// SPDX-License-Identifier: Apache-2.0
using System.Runtime.CompilerServices;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphLifecycleTests
{
    [Fact]
    public async Task CloseCancelsAndJoinsAnUnreleasedPreparationGateBeforeAnyDeviceRelease()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        var owner = new AudioOwnerContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var neverReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool exited = false;
        async Task Gate(CancellationToken cancellation)
        {
            entered.TrySetResult();
            try { await neverReleased.Task.WaitAsync(cancellation); }
            finally { exited = true; }
        }
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, 8, 3, Gate, false));
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        owner.Run(fixture.Document.Close);
        await owner.Join(coordinator.DisposeAsync);
        Assert.True(exited);
        Assert.False(sampler.HasPending);
        Assert.Equal(0, sampler.PreparedStatesCreated);
        Assert.Equal(0, sampler.ReservedPcmAndScratchBytes);
        Assert.True(coordinator.ReadStatus().Closed);
        Assert.Equal(0, sampler.StopAcknowledgment);
    }

    private sealed class Borrower(RealtimeSampler sampler) : IAudioOutputLifetime
    {
        internal bool AllowJoin;
        internal Func<bool> PreparationJoined = () => true;
        public bool Joined { get; private set; }
        public void Close()
        {
            Assert.True(PreparationJoined());
            Assert.True(sampler.LivePreparedStates > 0);
            if (!AllowJoin) throw new IOException("Injected unconfirmed join.");
            Joined = true;
        }
        public void Dispose() => Assert.True(Joined);
    }

    [Fact]
    public async Task SessionAwaitsPreparationAndFailedJoinRetainsBorrowedGraphUntilConfirmedRetry()
    {
        using var fixture = await AudioGraphFixture.Create();
        var sampler = new RealtimeSampler(48000, 2, 8);
        var session = new AudioDeviceSession();
        var borrower = new Borrower(sampler);
        session.AttachOutput(borrower, sampler);
        var owner = new AudioOwnerContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var never = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0; bool workerJoined = true;
        async Task Gate(CancellationToken cancellation)
        {
            if (++requests == 1) return;
            workerJoined = false; entered.TrySetResult();
            try { await never.Task.WaitAsync(cancellation); }
            finally { workerJoined = true; }
        }
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() =>
        {
            coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, 8, 3, Gate, false);
            session.AttachCoordinator(coordinator);
        });
        await owner.Until(() => sampler.HasPending);
        owner.Run(() => Assert.True(coordinator.Start()));
        sampler.Process(new float[16]);
        owner.Run(() => fixture.Document.Edit("New schedule", edit => edit.AddNote(fixture.Pattern, fixture.SnarePart, new(1700), new(50), 60, 0.1m)));
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        borrower.PreparationJoined = () => workerJoined;
        Task close = Task.CompletedTask;
        owner.Run(() => close = session.CloseOutputAsync());
        await owner.Until(() => close.IsCompleted);
        await Assert.ThrowsAsync<IOException>(() => close);
        Assert.False(borrower.Joined);
        Assert.Same(sampler, session.OutputSampler);
        Assert.Equal(1, sampler.LivePreparedStates);
        Assert.True(sampler.RetainedPcmAndScratchBytes > 0);
        Assert.Equal(0, sampler.StopAcknowledgment);
        sampler.Process(new float[16]);
        long acknowledged = sampler.StopAcknowledgment;
        Assert.True(acknowledged > 0);
        borrower.AllowJoin = true;
        await owner.Join(session.DisposeAsync);
        Assert.Equal(0, sampler.LivePreparedStates);
        Assert.Null(session.OutputSampler);
        Assert.Equal(acknowledged, sampler.StopAcknowledgment);
    }

    [Fact]
    public async Task InvalidSaveReopenCannotResurrectLastValidPlanAndTargetReplacementCancelsOldWork()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var first = new RealtimeSampler(48000, 2, 8);
        var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(fixture.Document, first, fixture.Attachment.Id, owner, 8, 3, null, false));
        await owner.Until(() => first.HasPending);
        owner.Run(() => Assert.True(coordinator.Start()));
        first.Process(new float[16]);
        owner.Run(() => fixture.Document.Edit("Invalid saved canonical", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 2)));
        Assert.True(coordinator.ReadStatus().LastValidPlaying);
        string path = fixture.Directory.File("invalid.seqvium");
        owner.Run(() => ProjectPersistence.Save(fixture.Document, path));
        await owner.Join(coordinator.DisposeAsync);
        first.TerminateExecution(); first.Dispose();
        var reopened = ProjectPersistence.Open(path).Document!;
        using var fresh = new RealtimeSampler(48000, 2, 8);
        GraphExecutionCoordinator next = null!;
        owner.Run(() => next = new(reopened, fresh, fixture.Attachment.Id, owner, 8, 3, null, false));
        Assert.Equal(GraphReasons.InvalidGain, next.ReadStatus().Blocker);
        Assert.False(next.ReadStatus().LastValidPlaying);
        Assert.Equal(Guid.Empty, next.ReadStatus().Execution.OriginPreparedRevision);
        owner.Run(() => Assert.False(next.Start()));
        owner.Run(() => reopened.Edit("Repair", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 0.5m)));
        owner.Run(() => next.SelectTarget(Id<GraphAttachment>.New()));
        await owner.Until(() => !next.ReadStatus().PreparationPending);
        Assert.Equal(GraphPreparationReasons.Target, next.ReadStatus().Blocker);
        Assert.False(fresh.HasPending);
        owner.Run(() => next.SelectTarget(fixture.Attachment.Id));
        await owner.Until(() => fresh.HasPending);
        owner.Run(() => Assert.True(next.Start()));
        fresh.Process(new float[16]);
        Assert.Equal(reopened.Current.Revision, fresh.ReadStatus().OriginPreparedRevision);
        await owner.Join(next.DisposeAsync);
        reopened.Close();
    }

    [Fact]
    public async Task CandidateActivePendingRetiredTogetherRemainInsideTotalReservation()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        async Task Publish()
        {
            using var request = sampler.BeginGraphPreparation(fixture.Document, fixture.Attachment.Id, repeats: 3);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); sampler.Start(); sampler.Process(new float[16]);
        await Publish(); sampler.Process(new float[16]); // old active is now retired
        await Publish(); // occupied retirement keeps this pending
        using var candidate = sampler.BeginGraphPreparation(fixture.Document, fixture.Attachment.Id, repeats: 3);
        await candidate.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, sampler.LivePreparedStates);
        Assert.Equal(SamplerPublication.Capacity, candidate.Publish(TestContext.Current.CancellationToken));
        Assert.True(sampler.ReservedPcmAndScratchBytes > sampler.RetainedPcmAndScratchBytes);
        Assert.InRange(sampler.ReservedPcmAndScratchBytes, 1, RealtimeSampler.MaximumRetainedPcmAndScratchBytes);
        candidate.Dispose();
        sampler.TerminateExecution(); sampler.Dispose();
        Assert.Equal(0, sampler.LivePreparedStates);
        Assert.Equal(0, sampler.ReservedPcmAndScratchBytes);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(256)]
    public async Task NewTopologyHasFiniteOneMillisecondFadeEvenWhenEofFallsInsidePacket(int packet)
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, packet);
        async Task Publish()
        {
            using var request = sampler.BeginGraphPreparation(fixture.Document, fixture.Attachment.Id, repeats: 3);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); sampler.Start(); sampler.Process(new float[16]);
        var epoch = sampler.TransportEpoch;
        fixture.Document.Edit("Schedule replacement", edit => edit.AddNote(fixture.Pattern, fixture.SnarePart, new(1500), new(80), 60, 0.2m));
        await Publish();
        using var plan = fixture.Prepare(packet: packet);
        float[] expected = GraphExecutionTests.Render(plan, [Math.Min(packet, 32)]);
        var actual = new float[expected.Length];
        int offset = 0;
        while (offset < actual.Length)
        {
            int samples = Math.Min(packet * 2, actual.Length - offset);
            sampler.Process(actual.AsSpan(offset, samples)); offset += samples;
        }
        Assert.True(sampler.TransportEpoch > epoch);
        for (int index = 0; index < actual.Length; index++)
            GraphExecutionTests.Near(expected[index] * Math.Min(1, (index / 2 + 1f) / sampler.GraphTransitionFrames), actual[index]);
        long panic = sampler.Panic(); float[] silent = new float[16]; sampler.Process(silent);
        Assert.Equal(panic, sampler.StopAcknowledgment);
        Assert.All(silent, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task GainOnlyUpdatePreservesAnAlreadyReleasingVoice()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 8);
        var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(fixture.Document, sampler, fixture.Attachment.Id, owner, 8, 3, null, false));
        await owner.Until(() => sampler.HasPending);
        owner.Run(() => Assert.True(coordinator.Start()));
        sampler.Process(new float[16]); sampler.Process(new float[8]);
        var before = sampler.ReadStatus();
        Assert.Equal(2, before.Voices);
        owner.Run(() => fixture.Document.Edit("Releasing Kick gain", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 0.75m)));
        float[] block = new float[8]; sampler.Process(block);
        Assert.Equal(before.Epoch, sampler.TransportEpoch);
        for (int frame = 0; frame < 4; frame++)
            for (int channel = 0; channel < 2; channel++)
                GraphExecutionTests.Near(0.5f * (0.75f * fixture.Oracle(fixture.KickPart, before.Position + frame, channel, 48000, 2, 3) +
                    fixture.Oracle(fixture.SnarePart, before.Position + frame, channel, 48000, 2, 3)), block[frame * 2 + channel]);
        await owner.Join(coordinator.DisposeAsync);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference DisposeOwners(PreparedSampler plan)
    {
        using var execution = plan.CreateExecution();
        var leases = plan.LeaseSources();
        var weak = new WeakReference(leases[0].Samples);
        foreach (var lease in leases) lease.Dispose();
        plan.Dispose();
        execution.Dispose();
        return weak;
    }

    [Fact]
    public async Task GraphPcmCanBeCollectedAfterEveryPreparedAndExecutionLeaseIsReleased()
    {
        using var fixture = await AudioGraphFixture.Create();
        var plan = fixture.Prepare();
        var weak = DisposeOwners(plan);
        GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(weak.IsAlive);
        Assert.Throws<ObjectDisposedException>(() => plan.CreateExecution());
    }
}
