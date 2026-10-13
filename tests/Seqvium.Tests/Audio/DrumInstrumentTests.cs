// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class DrumInstrumentTests
{
    public static IEnumerable<object[]> Voices => DrumInstruments.All.SelectMany(i => new[] { 44100, 48000 }
        .Select(rate => new object[] { i.Key, rate }));

    public static IEnumerable<object[]> Controls => DrumInstruments.All.Skip(1).SelectMany(i =>
        i.Parameters.Where(p => p.Key != "choke").Select(p => new object[] { i.Key, p.Key }));

    private static SoundDefinition Sound(DrumInstruments.Instrument i) =>
        new(default, i.Key, i.Algorithm, i.Defaults, [], null);

    [Theory, MemberData(nameof(Voices))]
    public void PresetsRenderDistinctDeterministicBoundedSoundsAtBothRates(string key, int rate)
    {
        var i = DrumInstruments.All.Single(i => i.Key == key);
        var fingerprints = new List<double>();
        foreach (var preset in i.Presets)
        {
            var sound = Sound(i) with { Parameters = preset.Parameters };
            Assert.True(BuiltInSound.Supports(sound));
            using var pcm = DrumVoice.Render(sound, rate, cancellation: TestContext.Current.CancellationToken);
            using var same = DrumVoice.Render(sound, rate, cancellation: TestContext.Current.CancellationToken);
            double energy = 0, dc = 0;
            Assert.Equal((int)decimal.Ceiling(preset.Parameters["decay"] * rate / 1000), pcm.Frames);
            for (int frame = 0; frame < pcm.Frames; frame++)
            {
                float sample = pcm.Sample(frame, 0);
                Assert.Equal(sample, same.Sample(frame, 0));
                Assert.InRange(sample, -1, 1);
                energy += sample * sample;
                dc += sample;
            }

            Assert.True(energy > 1, key);
            Assert.InRange(Math.Abs(dc / pcm.Frames), 0, .03);
            Assert.Equal(0, pcm.Sample(0, 0));
            Assert.Equal(0, pcm.Sample(pcm.Frames - 1, 0));
            fingerprints.Add(energy);
            using var transposed = DrumVoice.Render(sound, rate, 48, TestContext.Current.CancellationToken);
            Assert.Equal(pcm.Frames, transposed.Frames);
        }

        Assert.Equal(3, fingerprints.Distinct().Count());
    }

    [Theory, MemberData(nameof(Controls))]
    public void EachSoundControlChangesItsActualSignal(string key, string control)
    {
        var instrument = DrumInstruments.All.Single(i => i.Key == key);
        var sound = Sound(instrument);
        var p = instrument.Parameters.Single(p => p.Key == control);
        decimal value = p.Default == p.Minimum ? p.Maximum : p.Minimum;
        using var a = DrumVoice.Render(sound, 48000, cancellation: TestContext.Current.CancellationToken);
        using var b = DrumVoice.Render(sound with { Parameters = sound.Parameters.SetItem(control, value) }, 48000,
            cancellation: TestContext.Current.CancellationToken);
        double difference = 0;
        for (int f = 0; f < Math.Min(a.Frames, b.Frames); f++) difference += Math.Abs(a.Sample(f, 0) - b.Sample(f, 0));
        Assert.True(difference > .01, key + "/" + control);
    }

    [Theory, MemberData(nameof(Voices))]
    public void ExtremesAndCancellationStayFiniteAndUnsupportedDataIsRefused(string key, int rate)
    {
        var i = DrumInstruments.All.Single(i => i.Key == key);
        var sound = Sound(i);
        foreach (decimal pitch in new decimal[] { 0, 127 })
        {
            var values = i.Parameters.ToImmutableDictionary(p => p.Key, p => p.Maximum).Add("rootPitch", 36);
            using var pcm = DrumVoice.Render(sound with { Parameters = values }, rate, pitch,
                TestContext.Current.CancellationToken);
            for (int f = 0; f < pcm.Frames; f++) Assert.InRange(pcm.Sample(f, 0), -1, 1);
        }

        Assert.False(BuiltInSound.Supports(sound with { Parameters = sound.Parameters.Add("future", 1) }));
        Assert.Throws<OperationCanceledException>(() => DrumVoice.Render(sound, rate, cancellation: new(true)));
    }

    [Theory, MemberData(nameof(Voices))]
    public async Task AllVoicesUseRackPianoHistoryMediaFreePersistenceAndBothExports(string key, int rate)
    {
        await using var f = new Fixture();
        await f.Create(key);
        var part = f.Pattern.Parts.Single();
        Assert.True(await f.Workflow.ToggleBeatStepAsync(f.Basis, part.Id, 15));
        part = f.Pattern.Parts.Single();
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, part.Id,
            [part.Notes.Single() with { Pitch = 43, Intensity = .6m, Duration = new(1000) }]));
        var session = new DrumEditorSession();
        session.Observe(f.Workflow.State, f.Target(part.Id));
        Assert.True(session.Begin());
        session.Preset(DrumInstruments.All.Single(i => i.Key == key).Presets[1].Parameters);
        Assert.True(await f.Workflow.SetDrumParametersAsync(session.Complete()!));
        var shaped = f.Workflow.State.Snapshot;
        Assert.True(await f.Workflow.UndoAsync());
        Assert.True(await f.Workflow.UndoAsync(true));
        Assert.Equal(shaped.Revision, f.Workflow.State.Snapshot.Revision);
        using var plan = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, rate, 2, 256,
            cancellationToken: TestContext.Current.CancellationToken);
        using var rendered = DrumVoice.Render(f.State.Sounds.Single(), rate, 43, TestContext.Current.CancellationToken);
        Assert.Equal(Math.Max(
            ConstantTempoConversion.ToFrames(new(f.Pattern.Length.Ticks), f.State.Settings.Tempo, rate),
            plan.Notes.Single().StartFrame + rendered.Frames), plan.EndFrame);
        Assert.True(await f.Workflow.AddArrangementClipAsync(ArrangementEditBasis.From(f.Workflow.State),
            f.Basis.Attachment, default, 0));
        string project = f.Directory.File("drums.seqvium");
        Assert.True(await f.Workflow.SaveAsync(project));
        var saved = f.State;
        Assert.True(await f.Workflow.OpenAsync(project, UnsavedDecision.Discard));
        Assert.True(ProjectDocument.HasSameContent(saved, f.State));
        Assert.Empty(f.State.Resources);
        Assert.Null(f.Workflow.State.Execution);
        Assert.True(await f.Workflow.ExportWaveAsync(f.Basis, f.Directory.File("pattern.wav"),
            TestContext.Current.CancellationToken));
        Assert.True(await f.Workflow.ExportSongAsync(ArrangementEditBasis.From(f.Workflow.State),
            f.Directory.File("song.wav"), TestContext.Current.CancellationToken));
        Assert.Equal(File.ReadAllBytes(f.Directory.File("pattern.wav")),
            File.ReadAllBytes(f.Directory.File("song.wav")));
    }

    [Fact]
    public async Task HatChokeCrossesChannelsAndClipsHasClosedPriorityAndSeekPartitionParity()
    {
        await using var f = new Fixture();
        await f.Create("OpenHat");
        var open = f.Pattern.Parts.Single();
        await f.Workflow.ToggleBeatStepAsync(f.Basis, open.Id, 0);
        await f.Workflow.AddDrumVoiceAsync(f.Basis, "Closed",
            DrumInstruments.All.Single(i => i.Key == "ClosedHat").Algorithm);
        var closed = f.Pattern.Parts.Last();
        await f.Workflow.ToggleBeatStepAsync(f.Basis, closed.Id, 1);
        await f.Workflow.ToggleBeatStepAsync(f.Basis, closed.Id, 0);
        using var pattern = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 256,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, pattern.Notes.Single(n => n.Occurrence.PartId == open.Id).ChokeFrames);
        using var a = pattern.CreateExecution();
        using var b = pattern.CreateExecution();
        var full = Render(a, pattern.EndFrame, 256);
        Assert.Equal(full, Render(b, pattern.EndFrame, 19));
        using var seek = pattern.CreateExecution();
        seek.Seek(4000);
        var block = new float[512];
        seek.Process(block);
        Assert.Equal(full.AsSpan(8000, 512).ToArray(), block);
        // Two independent source patterns choke globally in song, while muted clips cannot choke.
        await f.Workflow.DeleteBeatChannelAsync(f.Basis, closed.Id);
        var openPlacement = f.Basis.Attachment;
        await f.Create("ClosedHat");
        await f.Workflow.ToggleBeatStepAsync(f.Basis, f.Pattern.Parts.Single().Id, 0);
        var closedPlacement = f.Basis.Attachment;
        await f.Workflow.AddArrangementClipAsync(ArrangementEditBasis.From(f.Workflow.State), openPlacement, default,
            0);
        await f.Workflow.AddArrangementClipAsync(ArrangementEditBasis.From(f.Workflow.State), closedPlacement,
            new(240000), 1);
        using var song =
            ArrangementPreparation.Prepare(f.Document, 48000, 2, 256, TestContext.Current.CancellationToken);
        Assert.Equal(6000, song.Notes.Single(n => n.Occurrence.PartId == open.Id).ChokeFrames);
        using var songExecution = song.CreateExecution();
        var pcm = Render(songExecution, song.EndFrame, 31);
        using var sought = song.CreateExecution();
        sought.Seek(7000);
        sought.Process(block);
        Assert.Equal(pcm.AsSpan(14000, 512).ToArray(), block);
    }

    [Fact]
    public async Task HatLoopChokesAtFractionalWrapWithoutCallbackAllocationAndCanBeDisabled()
    {
        await using var f = new Fixture();
        await f.Create("OpenHat");
        var part = f.Pattern.Parts.Single();
        await f.Workflow.ToggleBeatStepAsync(f.Basis, part.Id, 15);
        await f.Workflow.ToggleBeatStepAsync(f.Basis, part.Id, 0);
        f.Document.Edit("Fractional tempo",
            edit => edit.SetSettings(f.State.Settings with { Tempo = new Tempo(137) }));
        using var plan = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 44100, 2, 256, voiceCapacity: 1,
            cancellationToken: TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution();
        execution.SetLooping(true);
        var block = new float[512];
        execution.Process(block);
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 3000; i++) execution.Process(block);
        Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread());
        f.Document.Edit("Free hats", edit => edit.SetSoundParameters(f.State.Sounds.Single().Id,
            f.State.Sounds.Single().Parameters.SetItem("choke", 0)));
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(f.Document, f.Basis.Attachment,
            44100, 2, 256, voiceCapacity: 1, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void PortablePresetsValidateInstrumentVersionAndExactParameters()
    {
        foreach (var i in DrumInstruments.All)
        {
            var preset = new DrumPresetFile(1, i.Algorithm, i.Presets[1].Parameters.SetItem("tune", 2));
            var restored = DrumPresetFile.Decode(preset.Encode(), i.Algorithm);
            Assert.Equal(preset.Parameters.OrderBy(p => p.Key), restored.Parameters.OrderBy(p => p.Key));
            Assert.Throws<NotSupportedException>(() => DrumPresetFile.Decode(preset.Encode(), "future"));
            Assert.Throws<NotSupportedException>(() => (preset with { Version = 2 }).Encode());
        }

        Assert.Throws<NotSupportedException>(() => DrumPresetFile.Decode(new byte[32769], DrumVoice.Algorithm));
        var hat = Sound(DrumInstruments.All.Single(i => i.Key == "OpenHat"));
        Assert.False(DrumVoice.Supports(hat with { Parameters = hat.Parameters.SetItem("choke", .5m) }));
    }

    private static float[] Render(OfflineSampler execution, long frames, int packet)
    {
        var result = new float[checked((int)frames * 2)];
        while (execution.Position < frames)
            execution.Process(result.AsSpan((int)execution.Position * 2,
                (int)Math.Min(packet, frames - execution.Position) * 2));
        return result;
    }

    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }

        public void Close()
        {
            Joined = true;
        }

        public void Dispose()
        {
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;

        internal Fixture() => Workflow = new(ProjectDocument.Create(),
            () => new(new Borrower(), new RealtimeSampler(48000, 2, 256), "Test", () => null), Directory.File("media"));

        internal ProjectDocument Document => Workflow.State.Document;
        internal ProjectState State => Workflow.State.Snapshot.State;
        internal BeatEditBasis Basis => BeatSteps.Resolve(Workflow.State).Basis!;
        internal Pattern Pattern => State.Patterns.Single(p => p.Id == Basis.Pattern);

        internal NodeEditorTarget Target(Id<MusicalPart> part) => new(Document.LifecycleId, Basis.Attachment,
            State.GraphAttachments.Single(a => a.Id == Basis.Attachment).GraphId,
            State.GraphAttachments.Single(a => a.Id == Basis.Attachment).Sources.Single(s => s.PartId == part).NodeId);

        internal async Task Create(string key)
        {
            Assert.True(await Workflow.CreateBeatAsync(Document.LifecycleId, Workflow.State.Generation, key));
            Assert.True(await Workflow.AddDrumVoiceAsync(Basis, key,
                DrumInstruments.All.Single(i => i.Key == key).Algorithm));
        }

        public async ValueTask DisposeAsync()
        {
            await Workflow.DisposeAsync();
            Directory.Dispose();
        }
    }
}
