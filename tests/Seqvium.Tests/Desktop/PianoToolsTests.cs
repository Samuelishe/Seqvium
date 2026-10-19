// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class PianoToolsTests
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
        public async ValueTask DisposeAsync() { await Workflow.DisposeAsync(); Directory.Dispose(); }
    }

    private static NoteEvent Note(long at, long duration = 123457, decimal pitch = 60.125m, decimal velocity = .37m)
        => new(Id<NoteEvent>.New(), new(at), new(duration), pitch, velocity);

    [Theory]
    [InlineData(960000, 479999, 0)] [InlineData(960000, 480000, 960000)]
    [InlineData(480000, 239999, 0)] [InlineData(480000, 240000, 480000)]
    [InlineData(240000, 119999, 0)] [InlineData(240000, 120000, 240000)]
    [InlineData(120000, 59999, 0)] [InlineData(120000, 60000, 120000)]
    [InlineData(320000, 159999, 0)] [InlineData(320000, 160000, 320000)]
    [InlineData(240000, 239999, 240000)] [InlineData(240000, 240001, 240000)]
    [InlineData(320000, 479999, 320000)] [InlineData(320000, 480000, 640000)]
    public async Task ExactTickOraclePreservesEveryOtherNoteField(long unit, long start, long expected)
    {
        await using var f = new Fixture(); var note = Note(start);
        await f.Create(note);
        int undo = f.Document.UndoCount;
        Assert.True(await f.Workflow.QuantizePianoNotesAsync(f.Basis, f.Part.Id, new HashSet<Id<NoteEvent>> { note.Id }, unit));
        Assert.Equal(note with { Position = new(expected) }, f.Part.Notes.Single());
        Assert.Equal(undo + 1, f.Document.UndoCount);
    }

    [Fact]
    public async Task ChordSelectionOrderLocalityNoOpExactHistoryAndSaveOpen()
    {
        await using var f = new Fixture();
        NoteEvent[] notes = [Note(120000, 1), Note(120000, 2111111, 64.75m, .91m), Note(120000, 157, 67, .5m), Note(319999, 777)];
        await f.Create(notes);
        Assert.True(await f.Workflow.AddDrumVoiceAsync(f.Basis, "Other", SynthVoice.Algorithm));
        var other = f.Pattern.Parts[1];
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, other.Id, [Note(333333)]));
        f.Document.Edit("Shared uses", e =>
        {
            var placement = e.State.Placements.Single();
            e.AddArrangementClip(placement.Id, new(1234567), 0, 2);
            e.AddArrangementClip(placement.Id, new(9999999), 1);
            e.InitializeMixer();
        });
        await f.Workflow.ObserveAsync();
        var before = f.Document.Current; int undo = f.Document.UndoCount;
        var selected = notes.Take(3).Select(n => n.Id).ToHashSet();
        Assert.True(await f.Workflow.QuantizePianoNotesAsync(f.Basis, f.Part.Id, selected, 240000));
        var after = f.Document.Current;
        Assert.Equal(notes.Select((n, i) => i < 3 ? n with { Position = new(240000) } : n), f.Part.Notes);
        Assert.Equal(before.State.Patterns[0].Parts[1], f.Pattern.Parts[1]);
        Assert.Equal(before.State.Sounds, after.State.Sounds); Assert.Equal(before.State.Resources, after.State.Resources);
        Assert.Equal(before.State.Graphs, after.State.Graphs); Assert.Equal(before.State.GraphAttachments, after.State.GraphAttachments);
        Assert.Equal(before.State.Mixer, after.State.Mixer); Assert.Equal(before.State.Placements, after.State.Placements);
        Assert.Equal(before.State.ArrangementClips, after.State.ArrangementClips); Assert.Equal(before.State.Patterns[0].Length, f.Pattern.Length);
        Assert.Equal(undo + 1, f.Document.UndoCount);
        Assert.False(await f.Workflow.QuantizePianoNotesAsync(f.Basis, f.Part.Id, selected, 240000));
        Assert.False(await f.Workflow.QuantizePianoNotesAsync(f.Basis, f.Part.Id, new HashSet<Id<NoteEvent>>(), 240000));
        Assert.Same(after, f.Document.Current); Assert.Equal(undo + 1, f.Document.UndoCount);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.Document.Current);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.Document.Current);
        var path = f.Directory.File("quantized.seqvium");
        Assert.True(await f.Workflow.SaveAsync(path)); Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard));
        Assert.True(ProjectDocument.HasSameContent(after.State, f.Document.Current.State));
    }

    [Theory]
    [InlineData(960000, 3839999, 1, false)]
    [InlineData(240000, 3720000, 120000, false)]
    [InlineData(240000, 3599999, 240000, true)]
    [InlineData(1, 123456, 100, false)] [InlineData(0, 123456, 100, false)]
    public async Task BoundaryRefusesWholeOperationAndFreeIsUnavailable(long unit, long start, long duration, bool accepts)
    {
        await using var f = new Fixture(); var first = Note(99999); var tail = Note(start, duration);
        await f.Create(first, tail); var before = f.Document.Current; int undo = f.Document.UndoCount;
        Assert.Equal(accepts, await f.Workflow.QuantizePianoNotesAsync(f.Basis, f.Part.Id, new HashSet<Id<NoteEvent>> { first.Id, tail.Id }, unit));
        if (!accepts) { Assert.Same(before, f.Document.Current); Assert.Equal(undo, f.Document.UndoCount); }
        else Assert.Equal(3840000, f.Part.Notes[1].Position.Ticks + f.Part.Notes[1].Duration.Ticks);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task QuantizeIndependentWholeClipOrRepeatVariationLeavesOriginalAndSourcesExact(bool repeat)
    {
        await using var f = new Fixture(); await f.Create(Note(120000));
        Id<ArrangementClip> clip = default;
        f.Document.Edit("Shared repeat", e => clip = e.AddArrangementClip(e.State.Placements.Single().Id, new(999999), 0, 4));
        await f.Workflow.ObserveAsync(); var original = f.Pattern;
        if (repeat) clip = (await f.Workflow.MakeArrangementRepeatUniqueAsync(ArrangementEditBasis.From(f.Workflow.State), clip, 2, "Fill"))!.Value;
        else
        {
            Assert.True(await f.Workflow.MakeArrangementVariationAsync(ArrangementEditBasis.From(f.Workflow.State), clip, "Fill"));
        }
        var placement = f.Document.Current.State.Placements.Single(p => p.Id == f.Document.Current.State.ArrangementClips.Single(c => c.Id == clip).SourcePlacementId);
        Assert.True(await f.Workflow.SelectAsync(f.Document.Current.State.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId).Id));
        Assert.NotEqual(original.Id, f.Pattern.Id); var part = f.Part; var before = f.Document.Current;
        Assert.True(await f.Workflow.QuantizePianoNotesAsync(f.Basis, part.Id, part.Notes.Select(n => n.Id).ToHashSet(), 240000));
        Assert.Equal(original, f.Document.Current.State.Patterns.Single(p => p.Id == original.Id));
        Assert.Equal(part.SoundId, f.Part.SoundId); Assert.Equal(before.State.Sounds, f.Document.Current.State.Sounds);
        Assert.Equal(before.State.GraphAttachments, f.Document.Current.State.GraphAttachments);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.Document.Current);
        Assert.Equal(part.Id, f.Part.Id); Assert.True(await f.Workflow.UndoAsync(true)); Assert.Equal(part.Id, f.Part.Id);
    }

    [Fact]
    public async Task StaleGenerationLifecycleAndWrongPartRefuse()
    {
        await using var f = new Fixture(); var note = Note(120000); await f.Create(note);
        var basis = f.Basis; var part = f.Part.Id; var selected = new HashSet<Id<NoteEvent>> { note.Id };
        Assert.True(await f.Workflow.QuantizePianoNotesAsync(basis, part, selected, 240000));
        Assert.True(await f.Workflow.UndoAsync()); var before = f.Document.Current;
        Assert.False(await f.Workflow.QuantizePianoNotesAsync(basis, part, selected, 240000)); Assert.Same(before, f.Document.Current);
        Assert.False(await f.Workflow.QuantizePianoNotesAsync(f.Basis, Id<MusicalPart>.New(), selected, 240000)); Assert.Same(before, f.Document.Current);
        basis = f.Basis; Assert.True(await f.Workflow.NewAsync(UnsavedDecision.Discard)); before = f.Workflow.State.Snapshot;
        Assert.False(await f.Workflow.QuantizePianoNotesAsync(basis, part, selected, 240000)); Assert.Same(before, f.Workflow.State.Snapshot);
    }

    [Theory]
    [InlineData("pattern")] [InlineData("part")] [InlineData("note")] [InlineData("sound")]
    public async Task UnknownIntentAndUnsupportedSoundRefuseWithoutHistory(string kind)
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
        var document = GraphFixture.Read(json).Document;
        await using var workflow = new DesktopWorkflow(document);
        var (pattern, basis) = BeatSteps.Resolve(workflow.State); var before = document.Current;
        Assert.False(await workflow.QuantizePianoNotesAsync(basis!, pattern!.Parts[0].Id, pattern.Parts[0].Notes.Select(n => n.Id).ToHashSet(), 240000));
        Assert.Same(before, document.Current); Assert.Equal(0, document.UndoCount);
    }

    [Theory]
    [InlineData(1, "101011010101")]
    [InlineData(2, "101101011010")]
    [InlineData(0, "000000000000")]
    public void EveryTonicAndMidiOctaveUsesKnownPitchClasses(int mode, string expected)
    {
        var kind = (PianoScaleKind)mode;
        Assert.Equal(12, PianoScale.Tonics.Distinct().Count());
        for (int tonic = 0; tonic < 12; tonic++)
        for (int pitch = 0; pitch < 128; pitch++)
        {
            var scale = new PianoScale(kind, tonic);
            Assert.Equal(expected[(pitch - tonic + 132) % 12] == '1', scale.Contains(pitch));
            Assert.Equal(kind != PianoScaleKind.Off && pitch % 12 == tonic, scale.IsTonic(pitch));
        }
    }

    [Fact]
    public async Task ScaleViewHasNoDocumentHistoryOrSourceRootAuthorityAndResetsOnReplacement()
    {
        using var directory = new TemporaryDirectory(); var f = new GraphFixture();
        await using var workflow = new DesktopWorkflow(f.Document);
        using var shell = new ShellSession(new(directory.File("prefs.json")), new(new(), false), f.Document) { HasWorkflowOwner = true };
        var canvas = new PianoRollCanvas(shell, workflow);
        canvas.Adopt(workflow.State, f.FirstPart);
        Assert.Equal(default, canvas.Scale);
        var before = f.Document.Current; int undo = f.Document.UndoCount;
        canvas.Scale = new(PianoScaleKind.NaturalMinor, 9);
        canvas.Adopt(workflow.State, f.SecondPart); canvas.Adopt(workflow.State, f.FirstPart);
        canvas.Zoom(1.25, 100); canvas.SetView(10, 10);
        Assert.Equal(new(PianoScaleKind.NaturalMinor, 9), canvas.Scale);
        Assert.Same(before, f.Document.Current); Assert.Equal(undo, f.Document.UndoCount);
        Assert.Equal(60, f.Document.Current.State.Sounds.Single().Parameters["rootPitch"]);
        Assert.True(await workflow.SetRootPitchAsync(BeatSteps.Resolve(workflow.State).Basis!, f.FirstPart, 54.5m));
        canvas.Adopt(workflow.State, f.FirstPart);
        Assert.Equal(new(PianoScaleKind.NaturalMinor, 9), canvas.Scale);
        Assert.True(canvas.Scale.IsTonic(69)); Assert.False(canvas.Scale.Contains(61));
        before = f.Document.Current; undo = f.Document.UndoCount;
        canvas.Scale = new(PianoScaleKind.Major, 1); canvas.Scale = default;
        Assert.Same(before, f.Document.Current); Assert.Equal(undo, f.Document.UndoCount);
        canvas.Scale = new(PianoScaleKind.Major, 5);
        Assert.True(await workflow.NewAsync(UnsavedDecision.Discard)); canvas.Adopt(workflow.State, null);
        Assert.Equal(default, canvas.Scale);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LiveQuantizeAndExactHistoryPublishWithoutTransportResetAndMatchCanonicalPcm(bool song)
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
        Assert.True(await f.Workflow.QuantizePianoNotesAsync(f.Basis, f.Part.Id, new HashSet<Id<NoteEvent>> { late.Id }, 240000));
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
