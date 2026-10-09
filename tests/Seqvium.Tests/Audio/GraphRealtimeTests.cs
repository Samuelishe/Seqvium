// SPDX-License-Identifier: Apache-2.0
using System.Threading.Channels;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

internal sealed class AudioOwnerContext : SynchronizationContext
{
    private readonly Channel<Action> _actions = Channel.CreateUnbounded<Action>();
    public override void Post(SendOrPostCallback callback, object? state) => _actions.Writer.TryWrite(() => callback(state));
    internal void Run(Action action)
    {
        var previous = Current;
        SetSynchronizationContext(this);
        try { action(); } finally { SetSynchronizationContext(previous); }
    }
    internal async Task Until(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        while (!condition()) Run(await _actions.Reader.ReadAsync(timeout.Token));
    }
    internal async Task Join(Func<ValueTask> close)
    {
        Task task = Task.CompletedTask;
        Run(() => task = close().AsTask());
        await Until(() => task.IsCompleted);
        await task;
    }
}

public sealed class GraphRealtimeTests
{
    [Theory]
    [InlineData(44100, 1)]
    [InlineData(44100, 2)]
    [InlineData(48000, 1)]
    [InlineData(48000, 2)]
    public async Task GraphUsesExistingRealtimeSamplerAndHasOfflineParity(int rate, int channels)
    {
        using var fixture = await AudioGraphFixture.Create(channels);
        using var offline = fixture.Prepare(rate, channels, packet: 31);
        using var sampler = new RealtimeSampler(rate, channels, 31);
        using var request = sampler.BeginGraphPreparation(fixture.Document, fixture.Attachment.Id, repeats: 3);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        sampler.Start();
        var expected = GraphExecutionTests.Render(offline, [31]);
        var actual = new float[expected.Length];
        for (int offset = 0; offset < actual.Length; offset += 31 * channels)
            Assert.True(sampler.Process(actual.AsSpan(offset, Math.Min(31 * channels, actual.Length - offset))));
        Assert.Equal(expected, actual);
        Assert.False(sampler.ReadStatus().Playing);
        Assert.Equal(SamplerTransportState.Ended, sampler.ReadStatus().Transport);
        Assert.Equal(0, sampler.ActiveVoices);
        Assert.Equal(fixture.Document.Current.Revision, sampler.ReadStatus().OriginPreparedRevision);
        Assert.False(sampler.Process(new float[32 * channels]));
        sampler.TerminateExecution(); sampler.Dispose();
        Assert.Equal(0, sampler.LivePreparedStates);
    }

    [Fact]
    public async Task WarmedGraphCallbackAllocatesNothingIncludingStatusAndControlBoundaries()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        using var request = sampler.BeginGraphPreparation(fixture.Document, fixture.Attachment.Id, repeats: 1024);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        float[] packet = new float[16];
        for (int warm = 0; warm < 4000; warm++)
        {
            if (warm % 1000 == 0) sampler.Start();
            sampler.Process(packet);
            _ = sampler.ReadStatus();
        }
        sampler.Start();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < 2000; iteration++)
        {
            sampler.Process(packet);
            _ = sampler.ReadStatus();
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.InRange(sampler.RetainedPcmAndScratchBytes, 1, RealtimeSampler.MaximumRetainedPcmAndScratchBytes);
    }

    [Fact]
    public async Task GainAndGeometryCoalesceAtPacketBoundaryWithoutRestartOrFalseOrigin()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, 8, 3, null, false));
        await owner.Until(() => !coordinator.ReadStatus().PreparationPending || sampler.HasPending);
        owner.Run(() => Assert.True(coordinator.Start()));
        float[] block = new float[16]; sampler.Process(block);
        var initial = sampler.ReadStatus();
        owner.Run(() =>
        {
            fixture.Document.Edit("Gain", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 0.75m));
            fixture.Document.Edit("Geometry after pending Gain", edit => edit.MoveGraphNode(fixture.Attachment.GraphId, fixture.Kick.Id, new(500, 200)));
        });
        var gainRevision = fixture.Document.RetainedSnapshots.Single(item => item.Revision != fixture.Document.Current.Revision &&
            item.State.Graphs.Any(graph => graph.Nodes.Any(node => node.Id == fixture.KickGain.Id &&
                node.Parameters[GraphBuiltIns.GainAmplitude].GetDecimal() == 0.75m))).Revision;
        sampler.Process(block);
        var updated = sampler.ReadStatus();
        Assert.Equal(initial.Epoch, updated.Epoch);
        Assert.Equal(initial.Position + 8, updated.Position);
        Assert.Equal(initial.OriginPreparedRevision, updated.OriginPreparedRevision);
        Assert.Equal(gainRevision, updated.ExecutingRevision);
        Assert.Equal(fixture.Document.Current.Revision, updated.EquivalentCanonicalRevision);
        for (int frame = 0; frame < 8; frame++)
            for (int channel = 0; channel < 2; channel++)
                GraphExecutionTests.Near(0.5f * (0.75f * fixture.Oracle(fixture.KickPart, initial.Position + frame, channel, 48000, 2, 3) +
                    fixture.Oracle(fixture.SnarePart, initial.Position + frame, channel, 48000, 2, 3)), block[frame * 2 + channel]);
        Assert.Equal(1, coordinator.ReadStatus().Preparations);
        await owner.Join(coordinator.DisposeAsync);
        sampler.Process(block);
        Assert.False(sampler.ReadStatus().Playing);
    }

    [Fact]
    public async Task EditUndoRedoSupersedesGatedPreparationIncludingAbaWithoutMergingHistory()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        var owner = new AudioOwnerContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int gates = 0;
        async Task Gate(CancellationToken cancellation)
        {
            if (Interlocked.Increment(ref gates) != 1) return;
            entered.SetResult();
            await release.Task.WaitAsync(cancellation);
        }
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, 8, 3, Gate, false));
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        var original = fixture.Document.Current.Revision;
        int history = fixture.Document.UndoCount;
        owner.Run(() =>
        {
            fixture.Document.Edit("Invalid intent while worker is occupied", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 2));
            Assert.True(coordinator.ReadStatus().InvalidCanonical);
            Assert.Equal(GraphReasons.InvalidGain, coordinator.ReadStatus().Blocker);
            Assert.True(fixture.Document.Undo());
            fixture.Document.Edit("Gain one", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 0.5m));
            fixture.Document.Edit("Gain two", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.MasterGain.Id, 0.75m));
            Assert.True(fixture.Document.Undo()); Assert.True(fixture.Document.Undo());
            Assert.Equal(original, fixture.Document.Current.Revision);
            Assert.True(fixture.Document.Redo());
        });
        release.TrySetResult();
        await owner.Until(() => sampler.HasPending);
        owner.Run(() => Assert.True(coordinator.Start()));
        sampler.Process(new float[16]);
        Assert.Equal(fixture.Document.Current.Revision, sampler.ReadStatus().OriginPreparedRevision);
        Assert.Equal(history + 1, fixture.Document.UndoCount);
        Assert.InRange(coordinator.ReadStatus().Preparations, 2, 3);
        Assert.True(coordinator.ReadStatus().Supersessions >= 4);
        await owner.Join(coordinator.DisposeAsync);
    }

    [Fact]
    public async Task InvalidCanonicalKeepsIdentifiedLastValidAndRepairConvergesAutomatically()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, 8, 3, null, false));
        await owner.Until(() => sampler.HasPending);
        owner.Run(() => Assert.True(coordinator.Start()));
        float[] block = new float[16]; sampler.Process(block);
        var valid = sampler.ReadStatus();
        owner.Run(() => fixture.Document.Edit("Invalid Gain", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 2)));
        sampler.Process(block);
        Assert.Equal(valid.ExecutingRevision, sampler.ReadStatus().ExecutingRevision);
        Assert.True(coordinator.ReadStatus().LastValidPlaying);
        Assert.Equal(GraphReasons.InvalidGain, coordinator.ReadStatus().Blocker);
        Assert.Throws<GraphPreparationException>(() => fixture.Prepare());
        owner.Run(() => fixture.Document.Edit("Repair", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 0.5m)));
        sampler.Process(block);
        Assert.Equal(fixture.Document.Current.Revision, sampler.ReadStatus().ExecutingRevision);
        Assert.False(coordinator.ReadStatus().InvalidCanonical);
        Assert.Equal(valid.Epoch, sampler.TransportEpoch);
        await owner.Join(coordinator.DisposeAsync);
    }

    [Fact]
    public async Task CapacityRetryRetainsOneCandidateAndLatestTopologyRestartsWithoutStarvation()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, 8, 3, null, false));
        await owner.Until(() => sampler.HasPending);
        // Keep the consumer blocked. A schedule edit must eventually replace this occupied pending slot.
        owner.Run(() => fixture.Document.Edit("New schedule", edit => edit.AddNote(fixture.Pattern, fixture.SnarePart, new(1501), new(101), 60, 0.2m)));
        await owner.Until(() => coordinator.ReadStatus().CapacityRetries > 0);
        Assert.InRange(sampler.LivePreparedStates, 1, 3);
        int preparations = coordinator.ReadStatus().Preparations;
        for (int attempt = 0; attempt < 20; attempt++) owner.Run(coordinator.Progress);
        Assert.Equal(preparations, coordinator.ReadStatus().Preparations);
        float[] block = new float[16]; sampler.Process(block); // stale pending retires without executing
        owner.Run(coordinator.Progress); // retirement releases capacity and retries the retained candidate
        owner.Run(() => Assert.True(coordinator.Start()));
        sampler.Process(block);
        Assert.Equal(fixture.Document.Current.Revision, sampler.ReadStatus().OriginPreparedRevision);
        var epoch = sampler.TransportEpoch;
        owner.Run(() => fixture.Document.Edit("Changed timing", edit => edit.AddNote(fixture.Pattern, fixture.SnarePart, new(1701), new(101), 60, 0.2m)));
        await owner.Until(() => sampler.HasPending);
        sampler.Process(block);
        Assert.True(sampler.TransportEpoch > epoch);
        Assert.Equal(fixture.Document.Current.Revision, sampler.ReadStatus().OriginPreparedRevision);
        using var current = fixture.Prepare();
        Assert.Equal(current.StartFrame + 8, sampler.RenderPosition);
        await owner.Join(coordinator.DisposeAsync);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAndPanicWinDuringPreparationOrPendingAndCloseJoinsWorker(bool pending)
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        var owner = new AudioOwnerContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Gate(CancellationToken cancellation)
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(cancellation);
        }
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, 8, 3, Gate, false));
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        if (pending)
        {
            release.TrySetResult(); await owner.Until(() => sampler.HasPending);
            owner.Run(() => Assert.True(coordinator.Start()));
        }
        long stop = sampler.Stop();
        release.TrySetResult();
        if (!pending) await owner.Until(() => sampler.HasPending);
        float[] block = new float[16]; sampler.Process(block);
        Assert.Equal(stop, sampler.StopAcknowledgment);
        Assert.All(block, value => Assert.Equal(0, value));
        var panic = sampler.Panic(); sampler.Process(block);
        Assert.Equal(panic, sampler.StopAcknowledgment);
        Assert.Equal(SamplerTransportState.Stopped, sampler.ReadStatus().Transport);
        owner.Run(() => fixture.Document.Close());
        await owner.Join(coordinator.DisposeAsync);
        sampler.TerminateExecution(); sampler.Dispose();
        Assert.Equal(0, sampler.LivePreparedStates);
        Assert.Equal(panic, sampler.StopAcknowledgment); // Close has no fabricated packet acknowledgment.
        Assert.True(coordinator.ReadStatus().Closed);
    }

    [Fact]
    public async Task DocumentNotificationsOccurAfterTransactionAndIgnoreRejectedOrNetZeroWork()
    {
        var document = ProjectDocument.Create();
        int notifications = 0;
        document.Changed += () =>
        {
            notifications++;
            using var decoded = WavDecoder.Decode(WavFixtures.Create([0.1f]));
            using var plan = SamplerPreparation.PrepareOneShot(decoded, 48000, 1);
            Assert.True(document.Generation > 0);
            Assert.False(document.Edit("Net zero observer", edit => edit.RenameDocument(document.Current.State.Name)));
        };
        Assert.True(document.Edit("Name", edit => edit.RenameDocument("Name")));
        Assert.False(document.Edit("Net zero", edit => edit.RenameDocument("Name")));
        Assert.Throws<InvalidOperationException>(() => document.Edit("Rejected", _ => throw new InvalidOperationException()));
        Assert.True(document.Undo()); Assert.True(document.Redo());
        Assert.Equal(3, notifications);
        document.Close();
        await Task.CompletedTask;
    }
}
