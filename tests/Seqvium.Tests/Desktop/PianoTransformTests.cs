// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class PianoTransformTests
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
        internal Task<bool> Apply(int transpose = 2, decimal duration = 150, decimal velocity = 120) =>
            Workflow.TransformPianoNotesAsync(Basis, Part.Id, Part.Notes.Select(n => n.Id).ToHashSet(), transpose, duration, velocity);
        public async ValueTask DisposeAsync() { await Workflow.DisposeAsync(); Directory.Dispose(); }
    }

    private static NoteEvent Note(long at, long duration = 123457, decimal pitch = 60.125m, decimal velocity = .37m)
        => new(Id<NoteEvent>.New(), new(at), new(duration), pitch, velocity);

    [Theory]
    [InlineData(2, 100, 100, 62.25, 240000, .3, 480000, .6)]
    [InlineData(0, 150, 100, 60.25, 360000, .3, 720000, .6)]
    [InlineData(0, 100, 120, 60.25, 240000, .36, 480000, .72)]
    [InlineData(2, 150, 120, 62.25, 360000, .36, 720000, .72)]
    [InlineData(-24, 50, 50, 36.25, 120000, .15, 240000, .3)]
    [InlineData(24, 200, 200, 84.25, 480000, .6, 960000, 1)]
    [InlineData(0, 62.5, 112.5, 60.25, 150000, .3375, 300000, .675)]
    public void ExactIndependentAndCombinedOracles(int transpose, decimal duration, decimal velocity,
        decimal pitch, long firstDuration, decimal firstVelocity, long secondDuration, decimal secondVelocity)
    {
        NoteEvent[] notes = [Note(490001, 240000, 60.25m, .3m), Note(0, 480000, 65.75m, .6m), Note(1)];
        var actual = PianoNotes.Transform(notes, notes.Take(2).Select(n => n.Id).ToHashSet(), transpose, duration, velocity, 3840000);
        Assert.Equal(notes[0] with { Pitch = pitch, Duration = new(firstDuration), Intensity = firstVelocity }, actual[0]);
        Assert.Equal(notes[1] with { Pitch = pitch + 5.5m, Duration = new(secondDuration), Intensity = secondVelocity }, actual[1]);
        Assert.Equal(notes[2], actual[2]);
    }

    [Theory]
    [InlineData(1, 50, 1)] [InlineData(1, 150, 2)] [InlineData(3, 50, 2)] [InlineData(5, 50, 3)]
    [InlineData(1, 200, 2)] [InlineData(2, 125, 3)]
    [InlineData(9223372036854775807, 100, 9223372036854775807)]
    [InlineData(9223372036854775807, 50, 4611686018427387904)]
    [InlineData(4611686018427387903, 200, 9223372036854775806)]
    public void ExactRoundingMinimumAndInt64Duration(long ticks, decimal scale, long expected)
    {
        var note = Note(0, ticks);
        var actual = PianoNotes.Transform([note], new HashSet<Id<NoteEvent>> { note.Id }, 0, scale, 100, long.MaxValue);
        Assert.Equal(note with { Duration = new(expected) }, actual.Single());
    }

    [Theory]
    [InlineData(0, -1, 100, "Piano.TransformPitchBoundary")]
    [InlineData(126.75, 1, 100, "Piano.TransformPitchBoundary")]
    [InlineData(60, 2, 200, "Piano.TransformTimeBoundary")]
    public async Task BoundaryRefusesEntireGroupIncludingEarlierValidNotes(decimal pitch, int transpose, decimal duration, string error)
    {
        await using var f = new Fixture(); await f.Create(Note(0), Note(3840000 - 3, 3, pitch));
        var before = f.Document.Current; long generation = f.Document.Generation; int undo = f.Document.UndoCount;
        Assert.False(await f.Apply(transpose, duration));
        Assert.Equal(error, f.Workflow.State.Detail);
        Assert.Same(before, f.Document.Current); Assert.Equal(generation, f.Document.Generation); Assert.Equal(undo, f.Document.UndoCount);
    }

    [Fact]
    public void HugeDurationRefusesBeforeInt64ConversionAndNearEndAcceptsExactly()
    {
        var note = Note(0, long.MaxValue);
        Assert.Equal("Piano.TransformTimeBoundary", Assert.Throws<InvalidOperationException>(() =>
            PianoNotes.Transform([note], new HashSet<Id<NoteEvent>> { note.Id }, 0, 200, 100, long.MaxValue)).Message);
        note = Note(long.MaxValue - 3, 2, 103, 1);
        Assert.Equal(note with { Pitch = 127, Duration = new(3) }, PianoNotes.Transform([note],
            new HashSet<Id<NoteEvent>> { note.Id }, 24, 150, 200, long.MaxValue).Single());
    }

    [Fact]
    public async Task DefaultsEmptyAndActualNoOpKeepRevisionGenerationHistoryAndRedo()
    {
        await using var f = new Fixture(); await f.Create(Note(1000, 1, 24, 0), Note(1000, 1, 127, 1));
        var before = f.Document.Current; long generation = f.Document.Generation; int undo = f.Document.UndoCount;
        Assert.False(await f.Apply(0, 100, 100));
        Assert.False(await f.Apply(0, 50, 200)); // one-tick floor and saturated 0/1 velocities
        Assert.False(await f.Workflow.TransformPianoNotesAsync(f.Basis, f.Part.Id, new HashSet<Id<NoteEvent>>(), 24, 200, 200));
        Assert.Same(before, f.Document.Current); Assert.Equal(generation, f.Document.Generation); Assert.Equal(undo, f.Document.UndoCount);
        Assert.True(await f.Apply(-1, 200, 50)); var after = f.Document.Current;
        Assert.True(await f.Workflow.UndoAsync());
        Assert.False(await f.Apply(0, 100, 100)); Assert.Equal(1, f.Workflow.State.RedoCount);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.Document.Current);
    }

    [Fact]
    public async Task SelectedPartOrderSharingOneUndoExactRedoRepeatApplyAndPersistence()
    {
        await using var f = new Fixture();
        NoteEvent[] notes = [Note(900000, 240000, 60.25m, .3m), Note(0, 480000, 65.75m, .6m), Note(800000, 3)];
        await f.Create(notes);
        Assert.True(await f.Workflow.AddDrumVoiceAsync(f.Basis, "Other", SynthVoice.Algorithm));
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, f.Pattern.Parts[1].Id, [Note(333333)]));
        f.Document.Edit("Shared uses", e =>
        {
            var placement = e.State.Placements.Single(); e.AddArrangementClip(placement.Id, new(1234567), 0, 2);
            e.AddArrangementClip(placement.Id, new(9999999), 1); e.InitializeMixer();
        });
        await f.Workflow.ObserveAsync(); var before = f.Document.Current; int undo = f.Document.UndoCount;
        var selected = notes.Take(2).Select(n => n.Id).ToHashSet();
        Assert.True(await f.Workflow.TransformPianoNotesAsync(f.Basis, f.Part.Id, selected, 2, 150, 120));
        var after = f.Document.Current;
        Assert.Equal(new[] { notes[0] with { Pitch = 62.25m, Duration = new(360000), Intensity = .36m },
            notes[1] with { Pitch = 67.75m, Duration = new(720000), Intensity = .72m }, notes[2] }, f.Part.Notes);
        Assert.Equal(before.State.Patterns[0].Parts[1], f.Pattern.Parts[1]);
        Assert.True(ProjectDocument.HasSameContent(before.State, after.State with { Patterns = before.State.Patterns }));
        Assert.Equal(undo + 1, f.Document.UndoCount);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.Document.Current);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.Document.Current);
        Assert.True(await f.Workflow.TransformPianoNotesAsync(f.Basis, f.Part.Id, selected, 2, 150, 120));
        Assert.Equal(64.25m, f.Part.Notes[0].Pitch); Assert.Equal(540000, f.Part.Notes[0].Duration.Ticks); Assert.Equal(.432m, f.Part.Notes[0].Intensity);
        var saved = f.Document.Current.State;
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("transformed.seqvium")));
        Assert.True(await f.Workflow.OpenAsync(f.Directory.File("transformed.seqvium"), UnsavedDecision.Discard));
        Assert.True(ProjectDocument.HasSameContent(saved, f.Workflow.State.Document.Current.State));
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

    [Theory]
    [InlineData(-25, 100, 100)] [InlineData(25, 100, 100)]
    [InlineData(0, 49.99, 100)] [InlineData(0, 200.01, 100)]
    [InlineData(0, 100, 49.99)] [InlineData(0, 100, 200.01)]
    public async Task InvalidParametersRefuseWithoutMutation(int transpose, decimal duration, decimal velocity)
    {
        await using var f = new Fixture(); await f.Create(Note(120000)); var before = f.Document.Current;
        Assert.False(await f.Apply(transpose, duration, velocity)); Assert.Same(before, f.Document.Current);
        Assert.Equal("Piano.TransformRange", f.Workflow.State.Detail);
    }

    [Fact]
    public async Task StaleGenerationAbaLifecycleWrongPartMissingSelectionAndOtherOccurrenceRefuse()
    {
        await using var f = new Fixture(); await f.Create(Note(120000));
        var basis = f.Basis; var part = f.Part.Id; var selected = f.Part.Notes.Select(n => n.Id).ToHashSet();
        Assert.True(await f.Apply()); Assert.True(await f.Workflow.UndoAsync()); var before = f.Document.Current;
        Assert.False(await f.Workflow.TransformPianoNotesAsync(basis, part, selected, 2, 150, 120));
        Assert.False(await f.Workflow.TransformPianoNotesAsync(f.Basis, Id<MusicalPart>.New(), selected, 2, 150, 120));
        Assert.False(await f.Workflow.TransformPianoNotesAsync(f.Basis, part, selected.Append(Id<NoteEvent>.New()).ToHashSet(), 2, 150, 120));
        Assert.Same(before, f.Document.Current);
        basis = f.Basis;
        Assert.True(await f.Workflow.CreateBeatAsync(f.Document.LifecycleId, f.Document.Generation, "Other"));
        before = f.Document.Current;
        Assert.False(await f.Workflow.TransformPianoNotesAsync(basis, part, selected, 2, 150, 120)); Assert.Same(before, f.Document.Current);
        Assert.True(await f.Workflow.SelectAsync(basis.Attachment)); basis = f.Basis;
        Assert.True(await f.Workflow.NewAsync(UnsavedDecision.Discard)); before = f.Document.Current;
        Assert.False(await f.Workflow.TransformPianoNotesAsync(basis, part, selected, 2, 150, 120)); Assert.Same(before, f.Document.Current);
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
        var (pattern, basis) = BeatSteps.Resolve(workflow.State); var before = document.Current;
        Assert.False(await workflow.TransformPianoNotesAsync(basis!, pattern!.Parts[0].Id, pattern.Parts[0].Notes.Select(n => n.Id).ToHashSet(), 2, 150, 120));
        Assert.Same(before, document.Current); Assert.Equal(0, document.UndoCount);
    }

    [Fact]
    public async Task BusyAndClosedRefuseWithoutMutation()
    {
        await using var f = new Fixture(); await f.Create(Note(120000));
        var basis = f.Basis; var part = f.Part.Id; var selected = f.Part.Notes.Select(n => n.Id).ToHashSet();
        var before = f.Document.Current; Task<bool>? declined = null;
        void DuringBusy(WorkflowState state)
        {
            if (state.Busy) declined = f.Workflow.TransformPianoNotesAsync(basis, part, selected, 2, 150, 120);
        }
        f.Workflow.Changed += DuringBusy;
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("busy.seqvium")));
        f.Workflow.Changed -= DuringBusy;
        Assert.NotNull(declined); Assert.False(await declined); Assert.Same(before, f.Document.Current);
        Assert.True(await f.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.False(await f.Workflow.TransformPianoNotesAsync(basis, part, selected, 2, 150, 120));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LiveTransformAndExactHistoryPublishWithoutTransportResetAndMatchCanonicalPcm(bool song)
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
        Assert.True(await f.Workflow.TransformPianoNotesAsync(f.Basis, f.Part.Id, new HashSet<Id<NoteEvent>> { late.Id }, 2, 150, 120));
        var after = f.Document.Current;
        async Task ConvergeAndCompare(int packets = 1)
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
            for (int i = 0; i < packets; i++)
            {
                canonical.Process(expected); sampler.Process(packet);
                Assert.Equal(position + (i + 1) * 256, sampler.RenderPosition);
                Assert.InRange(expected.Zip(packet, (a, b) => Math.Abs(a - b)).Max(), 0, .000001f);
            }
        }
        // Include the transformed onset, gate and release, rather than only the silent early packets.
        await ConvergeAndCompare(160);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.Document.Current); await ConvergeAndCompare();
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.Document.Current); await ConvergeAndCompare();
        Assert.True(await f.Workflow.StopAsync()); sampler.Process(packet); Assert.All(packet, v => Assert.Equal(0, v));
    }

    [Theory]
    [InlineData(44100, false)] [InlineData(48000, false)] [InlineData(44100, true)] [InlineData(48000, true)]
    public async Task CompleteTransformedCanonicalPcmMatchesRealtimeAfterPortableReopen(int rate, bool song)
    {
        await using var f = new Fixture();
        await f.Create(Note(0, 240000, 60.25m, .3m), Note(123457, 480000, 64, .6m), Note(960000, 360001, 67.75m, .8m));
        f.Document.Edit("Shared repeats", e => e.AddArrangementClip(e.State.Placements.Single().Id, new(100000), 0, 2));
        await f.Workflow.ObserveAsync(); Assert.True(await f.Apply());
        var accepted = f.Document.Current.State;
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("pcm.seqvium")));
        Assert.True(await f.Workflow.OpenAsync(f.Directory.File("pcm.seqvium"), UnsavedDecision.Discard));
        var reopened = f.Workflow.State.Document;
        Assert.True(ProjectDocument.HasSameContent(accepted, reopened.Current.State));
        using var plan = song ? ArrangementPreparation.Prepare(reopened, rate, 2, 256, cancellation: TestContext.Current.CancellationToken) :
            GraphPreparation.Prepare(reopened, f.Basis.Attachment, rate, 2, 256, cancellationToken: TestContext.Current.CancellationToken);
        using var canonical = plan.CreateExecution();
        using var sampler = new RealtimeSampler(rate, 2, 256);
        using var request = song ? sampler.BeginArrangementPreparation(reopened) : sampler.BeginGraphPreparation(reopened, f.Basis.Attachment);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); sampler.Start();
        var expected = new float[502]; var actual = new float[512];
        var offline = new float[checked((int)plan.EndFrame * 2)];
        for (int offset = 0; offset < offline.Length; offset += expected.Length)
        {
            int count = Math.Min(expected.Length, offline.Length - offset);
            canonical.Process(expected.AsSpan(0, count)); expected.AsSpan(0, count).CopyTo(offline.AsSpan(offset));
        }
        float maximum = 0, peak = 0;
        for (int offset = 0; offset < offline.Length; offset += actual.Length)
        {
            int count = Math.Min(actual.Length, offline.Length - offset);
            Assert.True(sampler.Process(actual.AsSpan(0, count)));
            for (int i = 0; i < count; i++)
            {
                maximum = Math.Max(maximum, Math.Abs(offline[offset + i] - actual[i]));
                peak = Math.Max(peak, Math.Abs(actual[i]));
            }
        }
        Assert.InRange(maximum, 0, .000001f); Assert.True(peak > .001f); Assert.Equal(0, sampler.ActiveVoices);
    }
}
