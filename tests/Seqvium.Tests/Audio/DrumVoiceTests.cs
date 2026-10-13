// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class DrumVoiceTests
{
    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Assert.True(Joined);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;

        internal Fixture() => Workflow = new(ProjectDocument.Create(), () =>
                new(new Borrower(), new RealtimeSampler(48000, 2, 256), "Test output", () => null),
            Directory.File("media"));

        internal ProjectDocument Document => Workflow.State.Document;
        internal ProjectState State => Workflow.State.Snapshot.State;
        internal BeatEditBasis Basis => BeatSteps.Resolve(Workflow.State).Basis!;
        internal Pattern Pattern => State.Patterns.Single();
        internal SoundDefinition Sound => State.Sounds.Single();

        internal NodeEditorTarget Target => new(Document.LifecycleId, Basis.Attachment,
            State.GraphAttachments.Single().GraphId, State.GraphAttachments.Single().Sources.Single().NodeId);

        internal async Task Create()
        {
            Assert.True(await Workflow.CreateBeatAsync(Document.LifecycleId, Workflow.State.Generation, "Drums"));
            Assert.True(await Workflow.AddDrumVoiceAsync(Basis, "Kick"));
        }

        internal async Task Hit(int step) =>
            Assert.True(await Workflow.ToggleBeatStepAsync(Basis, Pattern.Parts.Single().Id, step));

        public async ValueTask DisposeAsync()
        {
            await Workflow.DisposeAsync();
            Directory.Dispose();
        }
    }

    [Theory]
    [InlineData("tune", "7")]
    [InlineData("decay", "900")]
    [InlineData("tone", "0.95")]
    [InlineData("noise", "0.8")]
    [InlineData("pitchAmount", "10")]
    [InlineData("pitchDecay", "140")]
    [InlineData("pitchCurve", "0.6")]
    [InlineData("attack", "15")]
    [InlineData("amplitudeCurve", "0.6")]
    public void EveryExposedParameterChangesActualFiniteSignal(string key, string value)
    {
        using var first =
            DrumVoice.Render(DrumVoice.Defaults, 48000, cancellation: TestContext.Current.CancellationToken);
        using var second = DrumVoice.Render(DrumVoice.Defaults.SetItem(key, decimal.Parse(value,
                System.Globalization.CultureInfo.InvariantCulture)), 48000,
            cancellation: TestContext.Current.CancellationToken);
        double difference = 0;
        for (int i = 0; i < Math.Min(first.Frames, second.Frames); i++)
            difference += Math.Abs(first.Sample(i, 0) - second.Sample(i, 0));
        Assert.True(difference > 1, key + " did not materially affect PCM.");
        Assert.Equal(0, second.Sample(0, 0));
        Assert.Equal(0, second.Sample(second.Frames - 1, 0));
        for (int i = 0; i < second.Frames; i++) Assert.InRange(second.Sample(i, 0), -1, 1);
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void PitchTracksNotesWithoutChangingEnvelopeTimeAndVelocityBelongsToRenderer(int rate)
    {
        var values = DrumVoice.Defaults.SetItem("pitchAmount", 0).SetItem("tone", 0).SetItem("noise", 0);
        using var low = DrumVoice.Render(values, rate, 36, TestContext.Current.CancellationToken);
        using var high = DrumVoice.Render(values, rate, 48, TestContext.Current.CancellationToken);
        Assert.Equal(low.Frames, high.Frames);
        Assert.Equal(2 * DrumVoice.Frequency(values, 36), DrumVoice.Frequency(values, 48), 8);
        using var plan = SamplerPreparation.PrepareNoteAudition(low, rate, 2, 36, 36, .4m);
        using var execution = plan.CreateExecution();
        var block = new float[256];
        execution.Process(block);
        for (int i = 0; i < 128; i++)
        {
            Assert.Equal(low.Sample(i, 0) * .4f, block[i * 2], 6);
            Assert.Equal(block[i * 2], block[i * 2 + 1]);
        }
    }

    [Fact]
    public async Task RackCreationSharedEditsHistoryStaleABAAndSaveOpenNeedNoMedia()
    {
        await using var f = new Fixture();
        await f.Create();
        await f.Hit(0);
        await f.Hit(15);
        Assert.Empty(f.State.Resources);
        Assert.Empty(f.Sound.ResourceIds);
        Assert.True(GraphDiagnostics.Inspect(f.State).Single().IsEligibleForPreparation);
        Assert.Null(PianoNotes.Refusal(f.State, f.Pattern, f.Pattern.Parts.Single()));
        Assert.Null(BeatSteps.PartRefusal(f.State, f.Pattern.Parts.Single()));
        var session = new DrumEditorSession();
        session.Observe(f.Workflow.State, f.Target);
        Assert.True(session.Begin());
        session.Set("decay", 900);
        var proposal = session.Complete()!;
        int undo = f.Workflow.State.UndoCount;
        Assert.True(await f.Workflow.SetDrumParametersAsync(proposal));
        Assert.Equal(undo + 1, f.Workflow.State.UndoCount);
        Assert.Equal(900, f.Sound.Parameters["decay"]);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Equal(480, f.Sound.Parameters["decay"]);
        Assert.False(await f.Workflow.SetDrumParametersAsync(proposal));
        Assert.True(await f.Workflow.UndoAsync(true));
        Assert.Equal(900, f.Sound.Parameters["decay"]);
        string path = f.Directory.File("drums.seqvium");
        Assert.True(await f.Workflow.SaveAsync(path));
        var saved = f.State;
        Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard));
        Assert.True(ProjectDocument.HasSameContent(saved, f.State));
        Assert.Empty(f.State.Resources);
        Assert.False(f.Workflow.State.CanStop);
        Assert.Null(DrumEditorSession.Resolve(f.State, proposal.Target) is { } sound &&
                    f.Document.LifecycleId == proposal.Target.LifecycleId
            ? sound
            : null);
    }

    [Fact]
    public async Task ShortNotesPlayFullPercussiveEnvelopeAndExportTailMatchesGraph()
    {
        await using var f = new Fixture();
        await f.Create();
        await f.Hit(15);
        var part = f.Pattern.Parts.Single();
        var note = part.Notes.Single();
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, part.Id,
            [note with { Duration = new(1000), Intensity = .65m }]));
        using var plan = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 256,
            cancellationToken: TestContext.Current.CancellationToken);
        long onset = ConstantTempoConversion.ToFrames(note.Position, f.State.Settings.Tempo, 48000);
        using var pcm =
            DrumVoice.Render(f.Sound.Parameters, 48000, cancellation: TestContext.Current.CancellationToken);
        Assert.Equal(onset + pcm.Frames, plan.EndFrame);
        using var execution = plan.CreateExecution();
        var rendered = Render(execution, plan.EndFrame);
        for (int i = 0; i < pcm.Frames; i++) Assert.Equal(pcm.Sample(i, 0) * .65f * .25f, rendered[(onset + i) * 2], 6);
        string wav = f.Directory.File("kick.wav");
        Assert.True(await f.Workflow.ExportWaveAsync(f.Basis, wav, TestContext.Current.CancellationToken));
        using var exported =
            WavDecoder.Decode(await File.ReadAllBytesAsync(wav, TestContext.Current.CancellationToken));
        Assert.Equal(plan.EndFrame, exported.Frames);
        Assert.True(exported.Sample((int)onset + 2000, 0) != 0);
    }

    [Fact]
    public async Task LoopTailSurvivesBarBoundaryAndParameterEditKeepsTimeEpochAndStopIsSticky()
    {
        await using var f = new Fixture();
        await f.Create();
        await f.Hit(0);
        await f.Hit(15);
        using var sampler = new RealtimeSampler(48000, 2, 256);

        async Task Publish(bool preserve)
        {
            using var request = sampler.BeginGraphPreparation(f.Document, f.Basis.Attachment);
            request.PreservePlayback = preserve;
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }

        await Publish(false);
        sampler.SetLooping(true);
        sampler.Start();
        var block = new float[512];
        while (sampler.RenderPosition < 96000)
            sampler.Process(block.AsSpan(0, (int)Math.Min(256, 96000 - sampler.RenderPosition) * 2));
        sampler.Process(block);
        using var pcm =
            DrumVoice.Render(f.Sound.Parameters, 48000, cancellation: TestContext.Current.CancellationToken);
        Assert.Equal(pcm.Sample(6000, 0) * .25f, block[0], 6);
        long epoch = sampler.TransportEpoch, position = sampler.RenderPosition;
        sampler.RetireCompleted();
        f.Document.Edit("Shape", e => e.SetSoundParameters(f.Sound.Id, f.Sound.Parameters.SetItem("tune", 2)));
        await Publish(true);
        sampler.Process(block);
        Assert.Equal(epoch, sampler.TransportEpoch);
        Assert.Equal(position + 256, sampler.RenderPosition);
        sampler.RetireCompleted();
        await Publish(true);
        var stop = sampler.Stop();
        sampler.Process(block);
        Assert.Equal(stop, sampler.StopAcknowledgment);
        Assert.All(block, v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task PartitionParityRetriggerAndConsumerAllocationRemainBounded()
    {
        await using var f = new Fixture();
        await f.Create();
        await f.Hit(0);
        await f.Hit(1);
        await f.Hit(2);
        using var plan = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 256,
            cancellationToken: TestContext.Current.CancellationToken);
        using var a = plan.CreateExecution();
        using var b = plan.CreateExecution();
        Assert.True(Render(a, plan.EndFrame, 256).AsSpan().SequenceEqual(Render(b, plan.EndFrame, 17)));
        using var sampler = new RealtimeSampler(48000, 2, 256);
        using var request = sampler.BeginGraphPreparation(f.Document, f.Basis.Attachment);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        sampler.SetLooping(true);
        sampler.Start();
        var buffer = new float[512];
        sampler.Process(buffer);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) sampler.Process(buffer);
        Assert.Equal(allocated, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void UnknownOrOutOfRangeStateRemainsPreservedButNeverExecutes()
    {
        var invalid = DrumVoice.Defaults.SetItem("decay", 2001);
        Assert.Throws<NotSupportedException>(() =>
            DrumVoice.Render(invalid, 48000, cancellation: TestContext.Current.CancellationToken));
        Assert.Throws<NotSupportedException>(() => DrumVoice.Render(DrumVoice.Defaults.Add("future", 1), 48000,
            cancellation: TestContext.Current.CancellationToken));
        Assert.Throws<OperationCanceledException>(() => DrumVoice.Render(DrumVoice.Defaults, 48000,
            cancellation: new CancellationToken(true)));
    }

    [Fact]
    public async Task DrumAndWavChannelsMixWithoutChangingExistingMediaOrPartIdentity()
    {
        await using var f = new Fixture();
        await f.Create();
        await f.Hit(0);
        var drum = f.Sound;
        var drumPart = f.Pattern.Parts.Single();
        string wav = f.Directory.File("tone.wav");
        var bytes = WavFixtures.Create(Enumerable.Repeat(.1f, 4000).ToArray());
        await File.WriteAllBytesAsync(wav, bytes, TestContext.Current.CancellationToken);
        Assert.True(await f.Workflow.AddBeatWavAsync(f.Basis, wav, TestContext.Current.CancellationToken));
        var pcmPart = f.Pattern.Parts.Single(p => p.Id != drumPart.Id);
        Assert.True(await f.Workflow.ToggleBeatStepAsync(f.Basis, pcmPart.Id, 0));
        Assert.Equal(drumPart, f.Pattern.Parts.Single(p => p.Id == drumPart.Id));
        using var plan = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 256,
            cancellationToken: TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution();
        var block = new float[512];
        execution.Process(block);
        using var synthesized =
            DrumVoice.Render(drum.Parameters, 48000, cancellation: TestContext.Current.CancellationToken);
        for (int i = 0; i < 256; i++) Assert.Equal((synthesized.Sample(i, 0) + .1f) * .25f, block[i * 2], 5);
        Assert.True(await f.Workflow.DeleteBeatChannelAsync(f.Basis, pcmPart.Id));
        Assert.Single(f.Pattern.Parts);
        Assert.Equal(drumPart.Id, f.Pattern.Parts.Single().Id);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Equal(2, f.Pattern.Parts.Length);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(wav, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SharedDefinitionDrivesIndependentOverlappingSongVoicesAndRetainsNaturalTailOnSeek()
    {
        await using var f = new Fixture();
        await f.Create();
        await f.Hit(15);
        Assert.True(await f.Workflow.AddArrangementClipAsync(ArrangementEditBasis.From(f.Workflow.State),
            f.Basis.Attachment, default, 0));
        Assert.True(await f.Workflow.AddArrangementClipAsync(ArrangementEditBasis.From(f.Workflow.State),
            f.Basis.Attachment, default, 1));
        using var song =
            ArrangementPreparation.Prepare(f.Document, 48000, 2, 256, TestContext.Current.CancellationToken);
        using var pattern = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 256,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(pattern.EndFrame, song.EndFrame);
        using var a = song.CreateExecution();
        using var b = pattern.CreateExecution();
        var songPcm = Render(a, song.EndFrame);
        var patternPcm = Render(b, pattern.EndFrame);
        for (int i = 0; i < songPcm.Length; i++) Assert.Equal(patternPcm[i] * 2, songPcm[i], 6);
        using var seeked = song.CreateExecution();
        seeked.Seek(100000);
        var block = new float[512];
        seeked.Process(block);
        for (int i = 0; i < block.Length; i++) Assert.Equal(songPcm[200000 + i], block[i], 6);
    }

    [Fact]
    public async Task DenseNaturalTailOverlapRefusesBeforeOutputAndSourceDeletionUndoRestoresTarget()
    {
        await using var f = new Fixture();
        await f.Create();
        for (int i = 0; i < 16; i++) await f.Hit(i);
        f.Document.Edit("Long drum", e => e.SetSoundParameters(f.Sound.Id, f.Sound.Parameters.SetItem("decay", 2000)));
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(f.Document, f.Basis.Attachment,
            48000, 2, 256, voiceCapacity: 2, cancellationToken: TestContext.Current.CancellationToken));
        var target = f.Target;
        var session = new DrumEditorSession();
        session.Observe(f.Workflow.State, target);
        Assert.True(session.Begin());
        session.Set("tone", .7m);
        Assert.True(await f.Workflow.DeleteBeatChannelAsync(f.Basis, f.Pattern.Parts.Single().Id));
        session.Observe(f.Workflow.State, target);
        Assert.Null(session.Draft);
        Assert.False(session.CanEdit);
        Assert.True(await f.Workflow.UndoAsync());
        session.Observe(f.Workflow.State, target);
        Assert.True(session.CanEdit);
        Assert.Equal(target, session.Target);
    }

    private static float[] Render(OfflineSampler execution, long frames, int packet = 256)
    {
        var result = new float[checked((int)frames * 2)];
        while (execution.Position < frames)
        {
            int count = (int)Math.Min(packet, frames - execution.Position);
            execution.Process(result.AsSpan((int)execution.Position * 2, count * 2));
        }

        return result;
    }
}
