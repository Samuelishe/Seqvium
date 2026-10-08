// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class WavAuditionTests
{
    [Theory]
    [InlineData(44100, 48000, 1)]
    [InlineData(48000, 44100, 2)]
    [InlineData(44100, 44100, 2)]
    [InlineData(48000, 48000, 1)]
    public void OneShotSharesInterpolationAndOwnsIndependentPcmLeases(int sourceRate, int outputRate, int channels)
    {
        float[] source = [0.2f, 0.4f, -0.6f, 0.8f];
        using var pcm = WavDecoder.Decode(WavFixtures.Create(source, sourceRate));
        using var plan = SamplerPreparation.PrepareOneShot(pcm, outputRate, channels);
        Assert.Equal(Guid.Empty, plan.Revision);
        Assert.Single(plan.Events.Where(item => item.Kind == ExecutionEventKind.NoteOn));
        using var execution = plan.CreateExecution();
        pcm.Dispose();
        plan.Dispose(); // Execution owns independent leases, not the disposed temporary owners.
        int frames = (int)Math.Ceiling(source.Length * (double)outputRate / sourceRate);
        var output = new float[frames * channels];
        for (int frame = 0; frame < frames; frame++)
        {
            execution.Process(output.AsSpan(frame * channels, channels));
            double cursor = (double)frame * sourceRate / outputRate;
            int index = (int)cursor;
            double expected = source[index] + ((index + 1 < source.Length ? source[index + 1] : 0) - source[index]) *
                (cursor - index);
            for (int channel = 0; channel < channels; channel++)
                Assert.InRange(Math.Abs(output[frame * channels + channel] - expected), 0, 2e-6);
        }

        Assert.Equal(0, execution.ActiveVoices);
    }

    [Fact]
    public async Task PreviewDoesNotImportEditSaveOrAllocateProjectStorageAndEndsAtEof()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var before = document.Current;
        byte[] canonical = ProjectPersistence.Encode(document);
        string sourcePath = directory.File("external.wav");
        File.WriteAllBytes(sourcePath, WavFixtures.Create([0.4f, -0.2f]));
        using var audition = new WavAudition(48000, 2, 16, document);
        using var request = audition.BeginExternal(sourcePath);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        File.Delete(sourcePath); // Already decoded temporary PCM is independent; no durable acceptance is implied.
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        request.Dispose();
        float[] output = new float[8];
        Assert.True(audition.Processor.Process(output));
        Assert.Equal(0.4f * WavAudition.InitialGain, output[0]);
        Assert.Equal(-0.2f * WavAudition.InitialGain, output[2]);
        Assert.All(output.Skip(4), sample => Assert.Equal(0, sample));
        audition.Processor.RetireCompleted();
        Assert.Equal(0, audition.Processor.LivePreparedStates);
        Assert.True(audition.Processor.Process(output));
        Assert.All(output, sample => Assert.Equal(0, sample));
        Assert.Same(before, document.Current);
        Assert.Equal(canonical, ProjectPersistence.Encode(document));
        Assert.Equal(0, document.UndoCount);
        Assert.Empty(document.Current.State.Resources);
        Assert.Empty(Directory.GetFileSystemEntries(directory.Path));
        Assert.Equal(SourceAvailability.Missing,
            (await SourceAccess.InspectExternalAsync(sourcePath, TestContext.Current.CancellationToken)).Availability);
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("cancel")]
    [InlineData("close")]
    public async Task ActivePreviewEndsAndRetiresAtConsumerBoundary(string action)
    {
        var document = ProjectDocument.Create();
        using var audition = new WavAudition(48000, 1, 16, document);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await Prepare(audition, 0.5f, cancellation.Token);
        float[] output = new float[16];
        audition.Processor.Process(output);
        Assert.Equal(1, audition.Processor.ActiveVoices);
        long stop = 0;
        if (action == "stop") stop = audition.Stop();
        if (action == "cancel") cancellation.Cancel();
        if (action == "close") document.Close();
        audition.Processor.Process(output);
        Assert.All(output, sample => Assert.Equal(0, sample));
        Assert.Equal(0, audition.Processor.ActiveVoices);
        if (stop != 0) Assert.Equal(stop, audition.Processor.StopAcknowledgment);
        audition.Processor.RetireCompleted();
        Assert.Equal(0, audition.Processor.LivePreparedStates);
        if (action == "close") Assert.Throws<InvalidOperationException>(() => audition.BeginExternal("next.wav"));
        else
        {
            await Prepare(audition, 0.25f, TestContext.Current.CancellationToken);
            audition.Processor.Process(output);
            Assert.Equal(0.25f * WavAudition.InitialGain, output[0]);
        }
    }

    [Fact]
    public async Task SupersededPendingAndRetirementBackpressureNeverResurrectTheWrongSource()
    {
        using var audition = new WavAudition(48000, 1, 16);
        await Prepare(audition, 0.5f, TestContext.Current.CancellationToken);
        float[] output = new float[16];
        audition.Processor.Process(output);
        await Prepare(audition, 0.25f, TestContext.Current.CancellationToken);
        audition.Processor.Process(output);
        Assert.Equal(0.25f * WavAudition.InitialGain, output[0]); // old state fills retirement
        await Prepare(audition, 0.75f, TestContext.Current.CancellationToken);
        audition.Processor.Process(output);
        Assert.All(output, sample => Assert.Equal(0, sample)); // no restart of 0.25
        audition.Processor.RetireCompleted();
        audition.Processor.Process(output);
        Assert.Equal(0.75f * WavAudition.InitialGain, output[0]);
        audition.Stop();
        audition.Processor.Process(output);
        audition.Processor.RetireCompleted();
        audition.Processor.Process(output);
        audition.Processor.RetireCompleted();
        Assert.Equal(0, audition.Processor.LivePreparedStates);
    }

    [Fact]
    public async Task PendingStopCancellationAndSupersessionRejectBeforeAnySound()
    {
        using var audition = new WavAudition(48000, 1, 16);
        using var first = audition.BeginExternal("first.wav");
        using var source = new MemoryStream(WavFixtures.Create([0.5f]));
        await first.PrepareAsync(source, TestContext.Current.CancellationToken);
        using var newer = audition.BeginExternal("newer.wav");
        Assert.Equal(SamplerPublication.Stale, first.Publish(TestContext.Current.CancellationToken));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var nextSource = new MemoryStream(WavFixtures.Create([0.75f]));
        await newer.PrepareAsync(nextSource, cancellation.Token);
        Assert.Equal(SamplerPublication.Accepted, newer.Publish(TestContext.Current.CancellationToken));
        cancellation.Cancel();
        float[] output = new float[16];
        audition.Processor.Process(output);
        Assert.All(output, sample => Assert.Equal(0, sample));
        audition.Processor.RetireCompleted();
        Assert.Equal(0, audition.Processor.LivePreparedStates);
        await Prepare(audition, 0.2f, TestContext.Current.CancellationToken);
        audition.Stop();
        audition.Processor.Process(output);
        Assert.All(output, sample => Assert.Equal(0, sample));
        audition.Processor.RetireCompleted();
        Assert.Equal(0, audition.Processor.LivePreparedStates);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InFlightCancellationOrSupersessionCannotAcceptStalePcm(bool cancel)
    {
        using var audition = new WavAudition(48000, 1, 16);
        using var request = audition.BeginExternal("external.wav");
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var source = new GateStream(WavFixtures.Create([0.5f]));
        var task = request.PrepareAsync(source, cancellation.Token);
        await source.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        using var newer = audition.BeginExternal("newer.wav");
        using var nextSource = new MemoryStream(WavFixtures.Create([0.75f]));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            newer.PrepareAsync(nextSource, TestContext.Current.CancellationToken));
        if (cancel)
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }
        else
        {
            source.Continue.SetResult();
            await task;
            Assert.Equal(SamplerPublication.Stale, request.Publish(TestContext.Current.CancellationToken));
        }

        request.Dispose();
        await Prepare(audition, 0.2f, TestContext.Current.CancellationToken);
        float[] output = new float[16];
        audition.Processor.Process(output);
        Assert.Equal(0.2f * WavAudition.InitialGain, output[0]);
    }

    [Fact]
    public async Task FailedReplacementStopsOldPreviewWithoutProjectChanges()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var before = document.Current;
        using var audition = new WavAudition(48000, 1, 16, document);
        await Prepare(audition, 0.5f, TestContext.Current.CancellationToken);
        float[] output = new float[16];
        audition.Processor.Process(output);
        using var missing = audition.BeginExternal(directory.File("missing.wav"));
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            missing.PrepareAsync(TestContext.Current.CancellationToken));
        audition.Processor.Process(output);
        audition.Processor.RetireCompleted();
        Assert.All(output, sample => Assert.Equal(0, sample));
        Assert.Equal(0, audition.Processor.LivePreparedStates);
        Assert.Same(before, document.Current);
        Assert.Empty(Directory.GetFileSystemEntries(directory.Path));
    }

    [Fact]
    public async Task AcceptedResourcePreviewRejectsStaleAuthorityAndDegradedMedia()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var accepted = await WavFixtures.Import(document, directory, [0.5f]);
        using var audition = new WavAudition(48000, 1, 16, document);
        using var request = audition.BeginResource(document, accepted.ResourceId);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        document.Edit("Intervening edit", edit => edit.RenameDocument("Keep"));
        document.Undo();
        Assert.Equal(SamplerPublication.Stale, request.Publish(TestContext.Current.CancellationToken));
        File.Delete(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories).Single());
        using var missing = audition.BeginResource(document, accepted.ResourceId);
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            missing.PrepareAsync(TestContext.Current.CancellationToken));
        Assert.Single(document.Current.State.Resources);
        Assert.Throws<ArgumentException>(() => audition.BeginResource(ProjectDocument.Create(), accepted.ResourceId));
    }

    [Fact]
    public async Task WarmPreviewProcessorAllocatesNothingAcrossVariablePackets()
    {
        using var audition = new WavAudition(48000, 2, 256);
        using var request = audition.BeginExternal("fixture.wav");
        using var source = new MemoryStream(WavFixtures.Create(new float[48000]));
        await request.PrepareAsync(source, TestContext.Current.CancellationToken);
        request.Publish(TestContext.Current.CancellationToken);
        float[] output = new float[512];
        audition.Processor.Process(output);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 200; i++) audition.Processor.Process(output.AsSpan(0, i % 2 == 0 ? 254 : 512));
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public async Task CancellationAtPublicationEndsThePreparedCandidateAndReleasesCapacity()
    {
        using var audition = new WavAudition(48000, 1, 16);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var request = audition.BeginExternal("fixture.wav");
        using var source = new MemoryStream(WavFixtures.Create([0.5f]));
        await request.PrepareAsync(source, cancellation.Token);
        using var blocked = audition.BeginExternal("blocked.wav");
        using var blockedSource = new MemoryStream(WavFixtures.Create([0.75f]));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            blocked.PrepareAsync(blockedSource, TestContext.Current.CancellationToken));
        cancellation.Cancel();
        Assert.Equal(SamplerPublication.Cancelled, request.Publish(TestContext.Current.CancellationToken));
        Assert.Equal(0, audition.Processor.LivePreparedStates);
        await Prepare(audition, 0.25f, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FaultTerminationCannotAcceptOrRestartAnotherPreviewAndCloseDoesNotInventAnAck()
    {
        var document = ProjectDocument.Create();
        using var audition = new WavAudition(48000, 1, 16, document);
        await Prepare(audition, 0.5f, TestContext.Current.CancellationToken);
        float[] output = new float[16];
        audition.Processor.Process(output);
        long acknowledged = audition.Processor.StopAcknowledgment;
        using var pending = audition.BeginExternal("next.wav");
        using var source = new MemoryStream(WavFixtures.Create([0.75f]));
        await pending.PrepareAsync(source, TestContext.Current.CancellationToken);
        audition.Processor.TerminateExecution();
        Assert.Equal(SamplerPublication.Stale, pending.Publish(TestContext.Current.CancellationToken));
        Assert.Throws<InvalidOperationException>(() => audition.BeginExternal("third.wav"));
        document.Close();
        Assert.Equal(acknowledged, audition.Processor.StopAcknowledgment);
        Assert.False(audition.Processor.Process(output));
        Assert.All(output, sample => Assert.Equal(0, sample));
        audition.Dispose();
        Assert.Equal(0, audition.Processor.LivePreparedStates);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("oversized")]
    [InlineData("unsupported")]
    public async Task PreviewPreparationFailuresNeverMutateCanonicalOrProjectStorage(string failure)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        byte[] before = ProjectPersistence.Encode(document);
        using var audition = new WavAudition(48000, 1, 16, document);
        using var request = audition.BeginExternal(directory.File("external.wav"));
        using var source = new MemoryStream(failure switch
        {
            "oversized" => new byte[WavDecoder.MaximumSourceBytes + 1],
            "unsupported" => WavFixtures.Create([0.5f], 96000),
            _ => [1, 2, 3]
        });
        await Assert.ThrowsAsync<WavFormatException>(() =>
            request.PrepareAsync(source, TestContext.Current.CancellationToken));
        Assert.Equal(before, ProjectPersistence.Encode(document));
        Assert.Equal(0, document.UndoCount);
        Assert.Empty(Directory.GetFileSystemEntries(directory.Path));
        Assert.Equal(0, audition.Processor.LivePreparedStates);
    }

    [Fact]
    public async Task ProjectCloseWithdrawsInFlightExternalPreparationWithoutAnEdit()
    {
        var document = ProjectDocument.Create();
        var before = document.Current;
        using var audition = new WavAudition(48000, 1, 16, document);
        using var request = audition.BeginExternal("external.wav");
        using var source = new GateStream(WavFixtures.Create([0.5f]));
        var task = request.PrepareAsync(source, TestContext.Current.CancellationToken);
        await source.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        document.Close();
        source.Continue.SetResult();
        await task;
        Assert.Equal(SamplerPublication.Stale, request.Publish(TestContext.Current.CancellationToken));
        Assert.Same(before, document.Current);
        Assert.Empty(document.Current.State.Resources);
        Assert.Equal(0, audition.Processor.LivePreparedStates);
    }

    [Fact]
    public async Task SeparatePreviewOwnersKeepIndependentCursorsAndStopLifetimes()
    {
        using var first = new WavAudition(48000, 1, 16);
        using var second = new WavAudition(48000, 1, 16);
        await Prepare(first, 0.5f, TestContext.Current.CancellationToken);
        await Prepare(second, -0.25f, TestContext.Current.CancellationToken);
        float[] output = new float[16];
        first.Processor.Process(output);
        second.Processor.Process(output);
        first.Stop();
        first.Processor.Process(output);
        first.Processor.RetireCompleted();
        Assert.Equal(0, first.Processor.LivePreparedStates);
        second.Processor.Process(output);
        Assert.Equal(-0.25f * WavAudition.InitialGain, output[0]);
        Assert.Equal(32, second.Processor.RenderPosition);
        Assert.Equal(1, second.Processor.ActiveVoices);
    }

    [Fact]
    public async Task HealthyAcceptedResourceAuditionsRawWithoutChangingItsConfiguredSound()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var accepted = await WavFixtures.Import(document, directory, [0.5f, 0.25f], root: 72);
        byte[] before = ProjectPersistence.Encode(document);
        int undo = document.UndoCount;
        using var audition = new WavAudition(48000, 1, 16, document);
        using var request = audition.BeginResource(document, accepted.ResourceId);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        float[] output = new float[16];
        audition.Processor.Process(output);
        Assert.Equal(0.5f * WavAudition.InitialGain, output[0]);
        Assert.Equal(0.25f * WavAudition.InitialGain, output[1]);
        audition.Processor.RetireCompleted();
        Assert.Equal(0, audition.Processor.LivePreparedStates);
        Assert.Equal(before, ProjectPersistence.Encode(document));
        Assert.Equal(undo, document.UndoCount);
        Assert.Single(Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories));
    }

    private static async Task Prepare(WavAudition audition, float value, CancellationToken token)
    {
        using var request = audition.BeginExternal("fixture.wav");
        using var source = new MemoryStream(WavFixtures.Create(Enumerable.Repeat(value, 256).ToArray()));
        await request.PrepareAsync(source, token);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(token));
    }

    private sealed class GateStream(byte[] bytes) : MemoryStream(bytes)
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Continue.Task.WaitAsync(cancellationToken);
            return await base.ReadAsync(buffer, cancellationToken);
        }
    }
}
