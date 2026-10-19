// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class PianoHumanizeTests
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
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal readonly DesktopWorkflow Workflow;
        internal RealtimeSampler? Sampler;
        internal Fixture() => Workflow = new(Document, () =>
        {
            Sampler = new(48000, 2, 256);
            return new(new Borrower(), Sampler, "Controlled output", () => null);
        }, Directory.File("media"));
        internal BeatEditBasis Basis => BeatSteps.Resolve(Workflow.State).Basis!;
        internal Pattern Pattern => BeatSteps.Resolve(Workflow.State).Pattern!;
        internal MusicalPart Part => Pattern.Parts[0];
        internal async Task Create(params NoteEvent[] notes)
        {
            Assert.True(await Workflow.CreateBeatAsync(Document.LifecycleId, Document.Generation, "Phrase"));
            Assert.True(await Workflow.AddDrumVoiceAsync(Basis, "Lead", SynthVoice.Algorithm));
            Assert.True(await Workflow.EditPianoNotesAsync(Basis, Part.Id, notes));
        }
        internal Task<bool> Apply(int timing = 100, int velocity = 100, Random? random = null) =>
            Workflow.HumanizePianoNotesAsync(Basis, Part.Id, Part.Notes.Select(n => n.Id).ToHashSet(), timing, velocity, random ?? new Random(147));
        public async ValueTask DisposeAsync() { await Workflow.DisposeAsync(); Directory.Dispose(); }
    }

    private static NoteEvent Note(long at, long duration = 123457, decimal pitch = 60.125m, decimal velocity = .37m)
        => new(Id<NoteEvent>.New(), new(at), new(duration), pitch, velocity);

    private sealed class ExtremeRandom(bool positive) : Random
    {
        internal int Calls;
        public override long NextInt64(long minValue, long maxValue) { Calls++; return positive ? maxValue - 1 : minValue; }
        public override int Next(int minValue, int maxValue) { Calls++; return positive ? maxValue - 1 : minValue; }
    }

    [Theory]
    [InlineData(0, 100)] [InlineData(100, 0)] [InlineData(1, 1)] [InlineData(20, 20)] [InlineData(100, 100)]
    public void SeededBoundsChordLocalityAndAllOtherFields(int timing, int velocity)
    {
        NoteEvent[] notes = [Note(0), Note(0, 3840000, 67.75m, 1), Note(3839999, 1, 72, 0),
            Note(1234567, 300000, 61.5m), Note(1234567, 456789, 65), Note(1234567, 111)];
        var selected = notes.Take(5).Select(n => n.Id).ToHashSet();
        for (int seed = 0; seed < 100; seed++)
        {
            var actual = PianoNotes.Humanize(notes, selected, timing, velocity, 3840000, new Random(seed));
            Assert.Equal(actual, PianoNotes.Humanize(notes, selected, timing, velocity, 3840000, new Random(seed)));
            Assert.Equal(actual[0].Position, actual[1].Position);
            Assert.Equal(actual[3].Position, actual[4].Position);
            Assert.Equal(notes[5], actual[5]);
            for (int i = 0; i < notes.Length; i++)
            {
                var n = actual[i];
                Assert.Equal(notes[i], n with { Position = notes[i].Position, Intensity = notes[i].Intensity });
                Assert.InRange(n.Position.Ticks - notes[i].Position.Ticks, -300L * timing, 300L * timing);
                Assert.InRange(n.Position.Ticks, 0, 3840000 - n.Duration.Ticks);
                Assert.InRange(n.Intensity, Math.Max(0, notes[i].Intensity - velocity / 1000m), Math.Min(1, notes[i].Intensity + velocity / 1000m));
            }
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExactExtremesClampChordTogetherAndVelocitySeparately(bool positive)
    {
        NoteEvent[] notes = [Note(10, 3839980, velocity: .99m), Note(10, 2, velocity: .01m),
            Note(1000000), Note(long.MaxValue - 500000, 450001)];
        var actual = PianoNotes.Humanize(notes.Take(3).ToArray(), notes.Take(3).Select(n => n.Id).ToHashSet(), 100, 100, 3840000, new ExtremeRandom(positive));
        // Test the ordinary group separately from the Int64-scale pattern below.
        Assert.Equal(positive ? 20 : 0, actual[0].Position.Ticks);
        Assert.Equal(actual[0].Position, actual[1].Position);
        Assert.Equal(positive ? 1030000 : 970000, actual[2].Position.Ticks);
        Assert.Equal(positive ? 1m : .89m, actual[0].Intensity);
        Assert.Equal(positive ? .11m : 0m, actual[1].Intensity);
        var huge = PianoNotes.Humanize([notes[3]], new HashSet<Id<NoteEvent>> { notes[3].Id }, 100, 0, long.MaxValue, new ExtremeRandom(positive));
        Assert.Equal(notes[3].Position.Ticks + (positive ? 30000 : -30000), huge[0].Position.Ticks);
    }

    [Fact]
    public async Task NoOpKeepsRevisionGenerationUndoAndRedoAndDoesNotDrawRandomValues()
    {
        await using var f = new Fixture(); await f.Create(Note(0, 3840000));
        var random = new ExtremeRandom(false); var before = f.Document.Current;
        long generation = f.Document.Generation; int history = f.Document.UndoCount;
        Assert.False(await f.Apply(0, 0, random));
        Assert.False(await f.Workflow.HumanizePianoNotesAsync(f.Basis, f.Part.Id, new HashSet<Id<NoteEvent>>(), 100, 100, random));
        Assert.Equal(0, random.Calls);
        Assert.False(await f.Apply(100, 0, random));
        Assert.Same(before, f.Document.Current); Assert.Equal(generation, f.Document.Generation); Assert.Equal(history, f.Document.UndoCount);
        Assert.True(await f.Apply(0, 100)); var after = f.Document.Current;
        Assert.True(await f.Workflow.UndoAsync());
        Assert.False(await f.Apply(0, 0)); Assert.Equal(1, f.Workflow.State.RedoCount);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.Document.Current);
    }

    [Fact]
    public async Task SelectedPartOnlyAtomicHistoryPersistenceExportAndRepeatedObservation()
    {
        await using var f = new Fixture();
        NoteEvent[] notes = [Note(123456), Note(123456, 2111111, 64.75m, .91m), Note(319999, 777)];
        await f.Create(notes);
        Assert.True(await f.Workflow.AddDrumVoiceAsync(f.Basis, "Other", SynthVoice.Algorithm));
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Pattern.Parts[1].Id, [Note(333333)]));
        f.Document.Edit("Shared uses", e =>
        {
            var placement = e.State.Placements.Single(); e.AddArrangementClip(placement.Id, new(1234567), 0, 2);
            e.AddArrangementClip(placement.Id, new(9999999), 1); e.InitializeMixer();
        });
        await f.Workflow.ObserveAsync();
        var before = f.Document.Current; int undo = f.Document.UndoCount;
        var random = new ExtremeRandom(true);
        Assert.True(await f.Workflow.HumanizePianoNotesAsync(f.Basis, f.Part.Id, notes.Take(2).Select(n => n.Id).ToHashSet(), 100, 100, random));
        var after = f.Document.Current;
        Assert.Equal(notes.Take(2).Select(n => n with { Position = new(n.Position.Ticks + 30000), Intensity = Math.Min(1, n.Intensity + .1m) }).Append(notes[2]), f.Part.Notes);
        Assert.Equal(before.State.Patterns[0].Parts[1], f.Pattern.Parts[1]);
        Assert.True(ProjectDocument.HasSameContent(before.State, after.State with { Patterns = before.State.Patterns }));
        Assert.Equal(undo + 1, f.Document.UndoCount); Assert.Equal(3, random.Calls);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.Document.Current);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.Document.Current); Assert.Equal(3, random.Calls);
        for (int i = 0; i < 10; i++) await f.Workflow.ObserveAsync();
        Assert.True(await f.Workflow.ExportWaveAsync(f.Basis, f.Directory.File("humanized.wav"), TestContext.Current.CancellationToken));
        Assert.Same(after, f.Document.Current); Assert.Equal(undo + 1, f.Document.UndoCount); Assert.Equal(3, random.Calls);
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("humanized.seqvium")));
        Assert.True(await f.Workflow.OpenAsync(f.Directory.File("humanized.seqvium"), UnsavedDecision.Discard));
        Assert.True(ProjectDocument.HasSameContent(after.State, f.Document.Current.State)); Assert.Equal(3, random.Calls);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task IndependentWholeClipOrRepeatLeavesSharedOriginalAndSourcesExact(bool repeat)
    {
        await using var f = new Fixture(); await f.Create(Note(120000));
        Id<ArrangementClip> clip = default;
        f.Document.Edit("Shared repeat", e => clip = e.AddArrangementClip(e.State.Placements.Single().Id, new(999999), 0, 4));
        await f.Workflow.ObserveAsync(); var original = f.Pattern;
        if (repeat) clip = (await f.Workflow.MakeArrangementRepeatUniqueAsync(ArrangementEditBasis.From(f.Workflow.State), clip, 2, "Fill"))!.Value;
        else Assert.True(await f.Workflow.MakeArrangementVariationAsync(ArrangementEditBasis.From(f.Workflow.State), clip, "Fill"));
        var placement = f.Document.Current.State.Placements.Single(p => p.Id == f.Document.Current.State.ArrangementClips.Single(c => c.Id == clip).SourcePlacementId);
        Assert.True(await f.Workflow.SelectAsync(f.Document.Current.State.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId).Id));
        var before = f.Document.Current;
        Assert.True(await f.Apply());
        Assert.Equal(original, f.Document.Current.State.Patterns.Single(p => p.Id == original.Id));
        Assert.True(ProjectDocument.HasSameContent(before.State, f.Document.Current.State with { Patterns = before.State.Patterns }));
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.Document.Current);
    }

    [Fact]
    public async Task StaleAbaLifecycleSelectionAndInvalidAmountsRefuseBeforeRandomness()
    {
        await using var f = new Fixture(); await f.Create(Note(120000));
        var basis = f.Basis; var part = f.Part.Id; var selected = f.Part.Notes.Select(n => n.Id).ToHashSet();
        Assert.True(await f.Apply()); Assert.True(await f.Workflow.UndoAsync()); var before = f.Document.Current;
        var random = new ExtremeRandom(true);
        Assert.False(await f.Workflow.HumanizePianoNotesAsync(basis, part, selected, 100, 100, random));
        Assert.False(await f.Workflow.HumanizePianoNotesAsync(f.Basis, Id<MusicalPart>.New(), selected, 100, 100, random));
        Assert.False(await f.Workflow.HumanizePianoNotesAsync(f.Basis, part, selected.Append(Id<NoteEvent>.New()).ToHashSet(), 100, 100, random));
        foreach (var (timing, velocity) in new[] { (-1, 0), (101, 0), (0, -1), (0, 101) })
            Assert.False(await f.Workflow.HumanizePianoNotesAsync(f.Basis, part, selected, timing, velocity, random));
        Assert.Same(before, f.Document.Current); Assert.Equal(0, random.Calls);
        basis = f.Basis; Assert.True(await f.Workflow.NewAsync(UnsavedDecision.Discard)); before = f.Document.Current;
        Assert.False(await f.Workflow.HumanizePianoNotesAsync(basis, part, selected, 100, 100, random));
        Assert.Same(before, f.Document.Current); Assert.Equal(0, random.Calls);
    }

    [Theory]
    [InlineData("pattern")] [InlineData("part")] [InlineData("note")] [InlineData("sound")]
    public async Task UnknownIntentRefusesWholeAction(string kind)
    {
        await using var f = new Fixture(); await f.Create(Note(120000));
        var json = GraphFixture.Json(f.Document);
        if (kind == "sound") json["state"]!["sounds"]![0]!["algorithm"] = "future.sound";
        else
        {
            var target = json["state"]!["patterns"]![0]!;
            if (kind is "part" or "note") target = target["parts"]![0]!;
            if (kind == "note") target = target["notes"]![0]!;
            target["future"] = true;
        }
        var document = GraphFixture.Read(json).Document; await using var workflow = new DesktopWorkflow(document);
        var (pattern, basis) = BeatSteps.Resolve(workflow.State); var before = document.Current; var random = new ExtremeRandom(true);
        Assert.False(await workflow.HumanizePianoNotesAsync(basis!, pattern!.Parts[0].Id, pattern.Parts[0].Notes.Select(n => n.Id).ToHashSet(), 100, 100, random));
        Assert.Same(before, document.Current); Assert.Equal(0, document.UndoCount); Assert.Equal(0, random.Calls);
    }

    [Fact]
    public async Task BusyAndClosedRefuseWithoutDrawingRandomness()
    {
        await using var f = new Fixture(); await f.Create(Note(120000));
        var basis = f.Basis; var part = f.Part.Id; var selected = f.Part.Notes.Select(n => n.Id).ToHashSet();
        var before = f.Document.Current; var random = new ExtremeRandom(true); Task<bool>? declined = null;
        void DuringBusy(WorkflowState state)
        {
            if (state.Busy) declined = f.Workflow.HumanizePianoNotesAsync(basis, part, selected, 100, 100, random);
        }
        f.Workflow.Changed += DuringBusy;
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("busy.seqvium")));
        f.Workflow.Changed -= DuringBusy;
        Assert.NotNull(declined); Assert.False(await declined); Assert.Same(before, f.Document.Current);
        Assert.True(await f.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.False(await f.Workflow.HumanizePianoNotesAsync(basis, part, selected, 100, 100, random));
        Assert.Equal(0, random.Calls);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LiveHumanizeAndExactHistoryPublishWithoutTransportResetAndMatchCanonicalPcm(bool song)
    {
        await using var f = new Fixture();
        var held = Note(0, 3000000, 48, .12m); var late = Note(490000, 234567, 67.5m, .23m);
        await f.Create(held, late);
        if (song)
        {
            Assert.True(await f.Workflow.AddArrangementClipAsync(ArrangementEditBasis.From(f.Workflow.State), f.Basis.Attachment, default, 0, 2));
            Assert.True(await f.Workflow.SetSongModeAsync(true));
        }
        var play = f.Workflow.PlayAsync();
        for (int i = 0; i < 10000 && !play.IsCompleted; i++)
        { f.Sampler?.Process(Span<float>.Empty); await f.Workflow.ObserveAsync(); await Task.Yield(); }
        Assert.True(await play.WaitAsync(TestContext.Current.CancellationToken));
        var sampler = f.Sampler!; var packet = new float[512]; sampler.Process(packet);
        long epoch = sampler.TransportEpoch;
        var before = f.Document.Current;
        Assert.True(await f.Workflow.HumanizePianoNotesAsync(f.Basis, f.Part.Id, new HashSet<Id<NoteEvent>> { late.Id }, 100, 100, new Random(147)));
        var after = f.Document.Current;
        async Task ConvergeAndCompare()
        {
            for (int i = 0; i < 10000 && sampler.ReadStatus().ExecutingRevision != f.Document.Current.Revision; i++)
            { sampler.Process(Span<float>.Empty); await f.Workflow.ObserveAsync(); await Task.Yield(); }
            Assert.Equal(f.Document.Current.Revision, sampler.ReadStatus().ExecutingRevision);
            Assert.Equal(epoch, sampler.TransportEpoch); Assert.True(sampler.ReadStatus().Playing);
            long position = sampler.RenderPosition;
            using var plan = song
                ? ArrangementPreparation.Prepare(f.Document, 48000, 2, 256)
                : GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 256);
            using var canonical = plan.CreateExecution();
            var expected = new float[512];
            for (long frame = 0; frame < position; frame += 256) canonical.Process(expected);
            canonical.Process(expected); sampler.Process(packet);
            Assert.Equal(position + 256, sampler.RenderPosition);
            Assert.InRange(expected.Zip(packet, (a, b) => Math.Abs(a - b)).Max(), 0, .000001f);
        }
        await ConvergeAndCompare();
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.Document.Current); await ConvergeAndCompare();
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.Document.Current); await ConvergeAndCompare();
        Assert.True(await f.Workflow.StopAsync()); sampler.Process(packet); Assert.All(packet, v => Assert.Equal(0, v));
    }
}
