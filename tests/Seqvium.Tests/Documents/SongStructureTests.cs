// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class SongStructureTests
{
    private sealed class Song : IDisposable
    {
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal Id<PatternPlacement> Source;
        internal Id<ArrangementClip> PatternClip;
        internal Id<AudioClip> AudioClip;
        internal Id<ArrangementSection> Intro, Chorus;
        internal Song()
        {
            Document.Edit("Song", e =>
            {
                var resource = e.AddResource("Audio");
                var sound = e.AddSound("Audio", AudioClipSource.Algorithm, [resource]);
                e.SetSoundParameters(sound, ImmutableDictionary<string, decimal>.Empty
                    .Add("sampleRate", 48000).Add("channels", 2).Add("frames", 96000));
                var pattern = e.AddPattern("Phrase", new(100));
                Source = e.AddPlacement(pattern, default);
                PatternClip = e.AddArrangementClip(Source, new(20), 2, 2);
                AudioClip = e.AddAudioClip(sound, new(420), 4, 20, 100);
                var audio = e.State.AudioClips.Single();
                e.UpdateAudioClip(audio with { GainDb = -3m, FadeInFrames = 17, FadeOutFrames = 23 });
                Intro = e.AddArrangementSection("Intro", new(0), new(300), 1);
                Chorus = e.AddArrangementSection("Chorus", new(400), new(30000), 2);
            });
        }
        public void Dispose() { if (!Document.IsClosed) Document.Close(); }
    }

    [Fact]
    public void SwapPreservesGapSharedSourcesAndAudioEnvelopeWithOneUndo()
    {
        using var f = new Song();
        var before = f.Document.Current;
        Assert.True(f.Document.Edit("Swap", e => e.MoveArrangementSection(f.Intro, true)));
        var state = f.Document.Current.State;
        Assert.Equal(30120, state.ArrangementClips.Single().Position.Ticks);
        Assert.Equal(20, state.AudioClips.Single().Position.Ticks);
        Assert.Equal(before.State.AudioClips.Single() with { Position = new(20) }, state.AudioClips.Single());
        Assert.Equal(30100, state.ArrangementSections.Single(s => s.Id == f.Intro).Position.Ticks);
        Assert.Equal(0, state.ArrangementSections.Single(s => s.Id == f.Chorus).Position.Ticks);
        Assert.Equal(before.State.Placements, state.Placements);
        Assert.True(f.Document.Undo()); Assert.Same(before, f.Document.Current);
        Assert.True(f.Document.Redo());
        Assert.True(f.Document.Edit("Swap back", e => e.MoveArrangementSection(f.Intro, false)));
        Assert.True(ProjectDocument.HasSameContent(before.State, f.Document.Current.State));
    }

    [Fact]
    public void DuplicateInsertsTimeAndFreshClipsWithoutDuplicatingMediaOrEditingSources()
    {
        using var f = new Song();
        var before = f.Document.Current;
        f.Document.Edit("Repeat", e => e.DuplicateArrangementSection(f.Intro));
        var state = f.Document.Current.State;
        Assert.Equal(new long[] { 20, 320 }, state.ArrangementClips.Select(c => c.Position.Ticks));
        Assert.Equal(720, state.AudioClips.Single().Position.Ticks);
        Assert.Equal(700, state.ArrangementSections.Single(s => s.Id == f.Chorus).Position.Ticks);
        Assert.All(state.ArrangementClips, c => Assert.Equal(f.Source, c.SourcePlacementId));
        Assert.Equal(before.State.Sounds, state.Sounds); Assert.Equal(before.State.Resources, state.Resources);
        Assert.True(f.Document.Undo()); Assert.Same(before, f.Document.Current);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CrossingPatternOrAudioRefusesWithoutPartialHistory(bool audio)
    {
        using var f = new Song();
        f.Document.Edit("Crossing", e =>
        {
            if (audio) e.UpdateAudioClip(e.State.AudioClips.Single() with { Position = new(250) });
            else e.UpdateArrangementClip(f.PatternClip, new(250), 2, 2);
        });
        var before = f.Document.Current; int undo = f.Document.UndoCount;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Swap", e => e.MoveArrangementSection(f.Intro, true)));
        Assert.Same(before, f.Document.Current); Assert.Equal(undo, f.Document.UndoCount);
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Repeat", e => e.DuplicateArrangementSection(f.Intro)));
        Assert.Same(before, f.Document.Current);
    }

    [Fact]
    public void MixedSelectionClampsTogetherDuplicatesTheFullSpanAndPastesFrozenCutContent()
    {
        using var f = new Song();
        var before = f.Document.Current;
        f.Document.Edit("Move", e => e.EditPlaylistSelection([f.PatternClip], [f.AudioClip], -500, -30));
        Assert.Equal(0, f.Document.Current.State.ArrangementClips.Single().Position.Ticks);
        Assert.Equal(400, f.Document.Current.State.AudioClips.Single().Position.Ticks);
        Assert.Equal(0, f.Document.Current.State.ArrangementClips.Single().Lane);
        Assert.Equal(2, f.Document.Current.State.AudioClips.Single().Lane);
        f.Document.Undo();
        long span = AudioClipSource.EndTicks(before.State, before.State.AudioClips.Single()) - 20;
        f.Document.Edit("Copy", e => e.EditPlaylistSelection([f.PatternClip], [f.AudioClip], duplicate: true));
        Assert.Equal(20 + span, f.Document.Current.State.ArrangementClips.Last().Position.Ticks);
        Assert.Equal(420 + span, f.Document.Current.State.AudioClips.Last().Position.Ticks);
        f.Document.Undo();
        f.Document.Edit("Cut", e => e.EditPlaylistSelection([f.PatternClip], [f.AudioClip], delete: true));
        f.Document.Edit("Paste", e => e.PastePlaylistClips(before.State.ArrangementClips, before.State.AudioClips, new(1000), 5));
        Assert.Equal(1000, f.Document.Current.State.ArrangementClips.Single().Position.Ticks);
        Assert.Equal(1400, f.Document.Current.State.AudioClips.Single().Position.Ticks);
        Assert.Equal(7, f.Document.Current.State.AudioClips.Single().Lane);
        var pasted = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Overflow", e =>
            e.PastePlaylistClips(before.State.ArrangementClips, before.State.AudioClips, new(0), 31)));
        Assert.Same(pasted, f.Document.Current);
    }

    [Fact]
    public void SectionFormatGateUnknownFieldsValidationAndNoOpsRemainLossless()
    {
        using var f = new Song();
        var json = GraphFixture.Json(f.Document);
        Assert.Equal(5, json["minimumReaderMinor"]!.GetValue<int>());
        json["state"]!["arrangementSections"]![0]!["futureLabel"] = "retain";
        var reopened = GraphFixture.Read(json).Document;
        try
        {
            var section = reopened.Current.State.ArrangementSections[0];
            Assert.False(reopened.Edit("Same", e => e.UpdateArrangementSection(section.Id, section.Name,
                section.Position, section.Length, section.ColorIndex)));
            Assert.False(reopened.Edit("First", e => e.MoveArrangementSection(section.Id, false)));
            var encoded = GraphFixture.Json(reopened);
            Assert.Equal("retain", encoded["state"]!["arrangementSections"]![0]!["futureLabel"]!.GetValue<string>());
        }
        finally { reopened.Close(); }
        json["minimumReaderMinor"] = 4;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        var before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Overlap", e => e.AddArrangementSection("Bad", new(100), new(400), 0)));
        Assert.Same(before, f.Document.Current);
    }

    [Fact]
    public void VariationDetachesOneClipRemapsGraphAndKeepsOtherUsesSoundsAndMixer()
    {
        var f = new GraphFixture();
        try
        {
            Id<ArrangementClip> first = default, second = default;
            f.Document.Edit("Shared uses", e => { first = e.AddArrangementClip(f.Placement, default); second = e.AddArrangementClip(f.Placement, new(4000000)); });
            var before = f.Document.Current;
            Id<PatternPlacement> variation = default;
            f.Document.Edit("Variation", e => variation = e.MakeArrangementVariation(second, "Fill"));
            var state = f.Document.Current.State;
            Assert.Equal(f.Placement, state.ArrangementClips.Single(c => c.Id == first).SourcePlacementId);
            Assert.Equal(variation, state.ArrangementClips.Single(c => c.Id == second).SourcePlacementId);
            Assert.Equal(before.State.Sounds, state.Sounds); Assert.Equal(before.State.Resources, state.Resources);
            Assert.Equal(before.State.Mixer, state.Mixer);
            var placement = state.Placements.Single(p => p.Id == variation);
            var pattern = state.Patterns.Single(p => p.Id == placement.PatternId);
            Assert.NotEqual(f.Pattern, pattern.Id);
            Assert.NotEqual(f.FirstPart, pattern.Parts[0].Id);
            var attachment = state.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId);
            Assert.All(attachment.Sources, b => { Assert.Equal(variation, b.PlacementId); Assert.Contains(pattern.Parts, p => p.Id == b.PartId); });
            var graph = state.Graphs.Single(g => g.Id == attachment.GraphId);
            Assert.Equal(f.Graph.Nodes.Select(n => n.Type), graph.Nodes.Select(n => n.Type));
            Assert.All(graph.Nodes, n => Assert.DoesNotContain(f.Graph.Nodes, old => old.Id == n.Id));
            Assert.True(f.Document.Undo()); Assert.Same(before, f.Document.Current);
            Assert.True(f.Document.Redo());
            var note = pattern.Parts[0].Notes[0];
            f.Document.Edit("Local phrase", e => e.UpdateNote(pattern.Id, pattern.Parts[0].Id, note.Id, note.Position, note.Duration, 67, note.Intensity));
            Assert.Equal(60, f.Document.Current.State.Patterns.Single(p => p.Id == f.Pattern).Parts[0].Notes[0].Pitch);
        }
        finally { f.Document.Close(); }
    }

    [Fact]
    public void UnknownIdentityStateRefusesVariationAtomically()
    {
        var f = new GraphFixture(false);
        try
        {
            Id<ArrangementClip> clip = default;
            f.Document.Edit("Clip", e => clip = e.AddArrangementClip(f.Placement, default));
            var json = GraphFixture.Json(f.Document);
            json["state"]!["graphs"]![0]!["future"] = JsonSerializer.SerializeToNode(new { identity = f.Gain.Id.Value });
            var reopened = GraphFixture.Read(json).Document;
            try
            {
                var before = reopened.Current;
                Assert.Throws<ProjectValidationException>(() => reopened.Edit("Variation", e => e.MakeArrangementVariation(clip, "Unsafe")));
                Assert.Same(before, reopened.Current);
            }
            finally { reopened.Close(); }
        }
        finally { f.Document.Close(); }
    }

    [Fact]
    public void AncestrySurvivesSourceDeletionWithoutOwningItsNotesOrLoweringTheReaderGate()
    {
        var f = new GraphFixture(false);
        try
        {
            Id<ArrangementClip> clip = default;
            f.Document.Edit("Clip", e => clip = e.AddArrangementClip(f.Placement, default));
            f.Document.Edit("Variation", e => e.MakeArrangementVariation(clip, "Fill"));
            var variation = f.Document.Current.State.Patterns.Single(p => p.Id != f.Pattern);
            Assert.Equal(f.Pattern, variation.VariationOf);
            f.Document.Edit("Delete ancestor", e =>
            {
                foreach (var placement in e.State.Placements.Where(p => p.PatternId == f.Pattern)) e.DeletePlacement(placement.Id);
                e.DeletePattern(f.Pattern);
            });
            Assert.Equal(variation, f.Document.Current.State.Patterns.Single());
            var json = GraphFixture.Json(f.Document);
            Assert.Equal(5, json["minimumReaderMinor"]!.GetValue<int>());
            var reopened = GraphFixture.Read(json).Document;
            try { Assert.Equal(f.Pattern, reopened.Current.State.Patterns.Single().VariationOf); }
            finally { reopened.Close(); }
        }
        finally { f.Document.Close(); }
    }

    [Fact]
    public async Task WorkflowRefusesCrossingsAndStaleAbaButClearsRecoveredEditFailure()
    {
        using var f = new Song();
        await using var workflow = new DesktopWorkflow(f.Document);
        var stale = ArrangementEditBasis.From(workflow.State);
        Assert.True(await workflow.UpdateArrangementClipAsync(stale, f.PatternClip, new(250), 2, 2));
        var crossing = workflow.State.Snapshot;
        Assert.False(await workflow.SectionCommandAsync(ArrangementEditBasis.From(workflow.State), f.Intro, "Later"));
        Assert.Same(crossing, workflow.State.Snapshot);
        Assert.Equal("Workflow.EditFailed", workflow.State.StatusKey);
        Assert.Contains("Phrase", workflow.State.Detail);
        Assert.True(await workflow.UndoAsync());
        Assert.NotEqual("Workflow.EditFailed", workflow.State.StatusKey);
        Assert.False(await workflow.SectionCommandAsync(stale, f.Intro, "Duplicate"));
        Assert.Equal(2, workflow.State.Snapshot.State.ArrangementSections.Length);
        Assert.True(await workflow.SectionCommandAsync(ArrangementEditBasis.From(workflow.State), f.Intro, "Duplicate"));
        Assert.NotEqual("Workflow.EditFailed", workflow.State.StatusKey);
        Assert.Equal(3, workflow.State.Snapshot.State.ArrangementSections.Length);
    }

    [Fact]
    public async Task VariationKeepsSongPcmAndMixerUntilItsNotesAreExplicitlyChanged()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        try
        {
            Id<ArrangementClip> target = default;
            using (var import = ProjectMedia.BeginImport(document, "Keys", releaseMilliseconds: 0, ownedMediaDirectory: directory.File("media")))
            {
                using var stream = new MemoryStream(WavFixtures.Create(Enumerable.Range(0, 2048).Select(i => i / 8192f).ToArray(), 48000, 1));
                await import.PrepareAsync(stream, TestContext.Current.CancellationToken);
                import.Accept((e, media) =>
                {
                    var pattern = e.AddPattern("Keys", new(MusicalPosition.TicksPerQuarter));
                    var part = e.AddPart(pattern, "Keys", media.SoundId);
                    e.AddNote(pattern, part, default, new(MusicalPosition.TicksPerQuarter), 60, .5m);
                    var source = e.AddPlacement(pattern, default);
                    var a = e.CreateItemGraph(source);
                    var input = e.AddGraphNode(a.GraphId, GraphBuiltIns.Source, new(0, 0));
                    var gain = e.AddGraphNode(a.GraphId, GraphBuiltIns.Gain, new(120, 0));
                    var output = e.AddGraphNode(a.GraphId, GraphBuiltIns.Output, new(240, 0));
                    e.BindGraphSource(a.Id, input.Id, source, part);
                    GraphFixture.Connect(e, a.GraphId, input, gain); GraphFixture.Connect(e, a.GraphId, gain, output);
                    e.SetGraphOutput(a.Id, output.Id);
                    e.SetGraphGain(a.GraphId, gain.Id, .7m);
                    e.AddArrangementClip(source, default);
                    target = e.AddArrangementClip(source, new(MusicalPosition.TicksPerQuarter));
                    e.InitializeMixer();
                }, TestContext.Current.CancellationToken);
            }
            var before = Render(document);
            var mixer = document.Current.State.Mixer;
            Id<PatternPlacement> placement = default;
            document.Edit("Variation", e => placement = e.MakeArrangementVariation(target, "Turnaround"));
            Assert.Equal(before, Render(document));
            Assert.Equal(mixer, document.Current.State.Mixer);
            var state = document.Current.State;
            var pattern = state.Patterns.Single(p => p.Id == state.Placements.Single(p => p.Id == placement).PatternId);
            var note = pattern.Parts[0].Notes[0];
            document.Edit("Phrase", e => e.UpdateNote(pattern.Id, pattern.Parts[0].Id, note.Id, note.Position, note.Duration, 72, note.Intensity));
            var changed = Render(document);
            Assert.Equal(before[..48000], changed[..48000]);
            Assert.False(before[48000..].SequenceEqual(changed[48000..]));
            string path = directory.File("study.seqvium");
            ProjectPersistence.Save(document, path);
            var reopened = ProjectPersistence.Open(path).Document;
            try { Assert.Equal(changed, Render(reopened)); }
            finally { reopened.Close(); }
        }
        finally { document.Close(); }

        static float[] Render(ProjectDocument document)
        {
            using var plan = ArrangementPreparation.Prepare(document, 48000, 2, 512);
            using var execution = plan.CreateExecution();
            var output = new float[checked((int)plan.EndFrame * 2)];
            for (int offset = 0; offset < output.Length; offset += 1024)
                execution.Process(output.AsSpan(offset, Math.Min(1024, output.Length - offset)));
            return output;
        }
    }
}
