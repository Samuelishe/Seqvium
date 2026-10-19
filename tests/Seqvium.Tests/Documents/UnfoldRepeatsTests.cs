// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class UnfoldRepeatsTests
{
    [Theory]
    [InlineData(2, false, false)] [InlineData(5, false, false)] [InlineData(1024, false, false)]
    [InlineData(5, true, false)] [InlineData(1024, true, true)] [InlineData(2, false, true)]
    public void ExactGeometrySourceIdentityOrderHistoryAndReopen(int repeats, bool variation, bool limit)
    {
        var f = new GraphFixture();
        try
        {
            const long length = 1234567;
            long start = limit ? long.MaxValue - length * repeats : 37;
            Id<ArrangementClip> id = default;
            f.Document.Edit("Uses", e =>
            {
                e.SetSettings(new(new(137.123456m), new(7, 8)));
                e.SetPatternLength(f.Pattern, new(length));
                e.AddArrangementClip(f.Placement, new(13), 4);
                id = e.AddArrangementClip(f.Placement, new(start), 3, repeats);
                if (variation) e.MakeArrangementVariation(id, "Existing fill");
                e.AddArrangementClip(f.Placement, new(41), 3, 2);
                e.SetArrangementTrack(3, "Phrase", 2, false, true);
                e.AddArrangementSection("Form", new(0), new(length * 8), 1);
                e.InitializeMixer();
            });
            var before = f.Document.Current; int undo = f.Document.UndoCount;
            var source = before.State.ArrangementClips.Single(c => c.Id == id).SourcePlacementId;
            Assert.True(f.Document.Edit("Unfold", e => e.UnfoldArrangementRepeats(id)));
            var after = f.Document.Current;
            var clips = after.State.ArrangementClips;
            Assert.Equal(repeats + 2, clips.Length);
            Assert.Equal(before.State.ArrangementClips[0], clips[0]);
            Assert.Equal(before.State.ArrangementClips[^1], clips[^1]);
            Assert.Equal(id, clips[1].Id);
            Assert.Equal(clips.Length, clips.Select(c => c.Id).Distinct().Count());
            for (int i = 0; i < repeats; i++)
            {
                var c = clips[i + 1];
                Assert.Equal(1, c.Repeats); Assert.Equal(3, c.Lane);
                Assert.Equal(start + length * i, c.Position.Ticks);
                Assert.Equal(source, c.SourcePlacementId);
                if (i > 0) Assert.DoesNotContain(before.State.ArrangementClips, old => old.Id == c.Id);
            }
            Assert.Equal(PlaylistEditing.EndTicks(before.State, before.State.ArrangementClips[1]),
                PlaylistEditing.EndTicks(after.State, clips[^2]));
            // Everything except clip occurrences must remain byte-for-byte canonical content.
            Assert.True(ProjectDocument.HasSameContent(before.State,
                after.State with { ArrangementClips = before.State.ArrangementClips }));
            Assert.Equal(undo + 1, f.Document.UndoCount);
            Assert.True(f.Document.Undo()); Assert.Same(before, f.Document.Current);
            Assert.True(f.Document.Redo()); Assert.Same(after, f.Document.Current);
            var reopened = GraphFixture.Reopen(f.Document).Document;
            try { Assert.True(ProjectDocument.HasSameContent(after.State, reopened.Current.State)); }
            finally { reopened.Close(); }
        }
        finally { f.Document.Close(); }
    }

    [Fact]
    public void IndividualMoveCopyVariationAndDeleteUseExistingEdits()
    {
        var f = new GraphFixture();
        try
        {
            Id<ArrangementClip> id = default;
            f.Document.Edit("Uses", e => id = e.AddArrangementClip(f.Placement, new(37), 3, 5));
            f.Document.Edit("Unfold", e => e.UnfoldArrangementRepeats(id));
            var unfolded = f.Document.Current;
            var middle = unfolded.State.ArrangementClips[2];
            f.Document.Edit("Move", e => e.EditPlaylistSelection([middle.Id], [], 960000, 1));
            Assert.Equal(middle.Position.Ticks + 960000, f.Document.Current.State.ArrangementClips[2].Position.Ticks);
            f.Document.Edit("Copy", e => e.EditPlaylistSelection([middle.Id], [], 1920000, 0, copy: true));
            var copy = f.Document.Current.State.ArrangementClips[^1];
            Assert.NotEqual(middle.Id, copy.Id); Assert.Equal(f.Placement, copy.SourcePlacementId);
            f.Document.Edit("Variation", e => e.MakeArrangementVariation(middle.Id, "Fill"));
            var state = f.Document.Current.State;
            var placement = state.Placements.Single(p => p.Id == state.ArrangementClips[2].SourcePlacementId);
            var fill = state.Patterns.Single(p => p.Id == placement.PatternId);
            var note = fill.Parts[0].Notes[0];
            f.Document.Edit("Local note", e => e.UpdateNote(fill.Id, fill.Parts[0].Id, note.Id,
                note.Position, note.Duration, 69, note.Intensity));
            Assert.Equal(unfolded.State.Patterns, f.Document.Current.State.Patterns.RemoveAt(1));
            foreach (var c in unfolded.State.ArrangementClips.Where(c => c.Id != middle.Id))
                Assert.Equal(c, f.Document.Current.State.ArrangementClips.Single(a => a.Id == c.Id));
            Assert.Equal(f.Placement, f.Document.Current.State.ArrangementClips.Single(c => c.Id == copy.Id).SourcePlacementId);
            f.Document.Edit("Delete", e => e.EditPlaylistSelection([middle.Id], [], delete: true));
            Assert.DoesNotContain(f.Document.Current.State.ArrangementClips, c => c.Id == middle.Id);
        }
        finally { f.Document.Close(); }
    }

    [Theory]
    [InlineData(1, 0, 1234567)] [InlineData(0, 0, 1234567)] [InlineData(1025, 0, 1234567)]
    [InlineData(int.MaxValue, 0, 1234567)] [InlineData(4, -1, 1234567)]
    [InlineData(4, long.MaxValue, 1234567)] [InlineData(4, 0, long.MaxValue)]
    public void InvalidOrExcessiveGeometryRefusesAtomically(int repeats, long position, long length)
    {
        var f = new GraphFixture();
        try
        {
            Id<ArrangementClip> id = default;
            f.Document.Edit("Use", e => id = e.AddArrangementClip(f.Placement, default, 0, 4));
            var before = f.Document.Current; int undo = f.Document.UndoCount;
            var error = Record.Exception(() => f.Document.Edit("Invalid unfold", e =>
            {
                e.SetPatternLength(f.Pattern, new(length));
                e.UpdateArrangementClip(id, new(position), 0, repeats);
                e.UnfoldArrangementRepeats(id);
            }));
            if (position < 0) Assert.IsType<ArgumentOutOfRangeException>(error);
            else Assert.IsType<ProjectValidationException>(error);
            Assert.Same(before, f.Document.Current); Assert.Equal(undo, f.Document.UndoCount);
        }
        finally { f.Document.Close(); }
    }

    [Fact]
    public void OpaqueClipRefusesButUnchangedOpaqueSourceIsPreserved()
    {
        var f = new GraphFixture();
        try
        {
            Id<ArrangementClip> id = default;
            f.Document.Edit("Use", e => id = e.AddArrangementClip(f.Placement, default, 0, 4));
            var json = GraphFixture.Json(f.Document);
            json["state"]!["arrangementClips"]![0]!["futureIdentity"] = "unknown occurrence semantics";
            var loaded = GraphFixture.Read(json).Document;
            try
            {
                var before = loaded.Current;
                Assert.Throws<ProjectValidationException>(() => loaded.Edit("Unfold", e => e.UnfoldArrangementRepeats(id)));
                Assert.Same(before, loaded.Current); Assert.Equal(0, loaded.UndoCount);
            }
            finally { loaded.Close(); }
            json["state"]!["arrangementClips"]![0]!.AsObject().Remove("futureIdentity");
            json["state"]!["patterns"]![0]!["futureIdentity"] = "unchanged shared source";
            loaded = GraphFixture.Read(json).Document;
            try
            {
                var before = loaded.Current;
                loaded.Edit("Unfold", e => e.UnfoldArrangementRepeats(id));
                Assert.True(ProjectDocument.HasSameContent(before.State,
                    loaded.Current.State with { ArrangementClips = before.State.ArrangementClips }));
            }
            finally { loaded.Close(); }
        }
        finally { f.Document.Close(); }
    }

    [Fact]
    public async Task StaleAbaLifecycleMissingTargetAndClosedWorkflowRefuse()
    {
        var f = new GraphFixture();
        await using var workflow = new DesktopWorkflow(f.Document);
        Id<ArrangementClip> id = default;
        f.Document.Edit("Use", e => id = e.AddArrangementClip(f.Placement, default, 0, 4));
        var stale = ArrangementEditBasis.From(workflow.State);
        var before = f.Document.Current;
        Assert.False(await workflow.UnfoldArrangementRepeatsAsync(stale with { Lifecycle = Guid.NewGuid() }, id));
        Assert.False(await workflow.UnfoldArrangementRepeatsAsync(stale, Id<ArrangementClip>.New()));
        Assert.True(await workflow.UpdateArrangementClipAsync(stale, id, new(123), 0, 4));
        Assert.True(await workflow.UndoAsync()); Assert.Same(before, f.Document.Current);
        Assert.False(await workflow.UnfoldArrangementRepeatsAsync(stale, id));
        Assert.Same(before, f.Document.Current);
        var valid = ArrangementEditBasis.From(workflow.State);
        Assert.True(await workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.False(await workflow.UnfoldArrangementRepeatsAsync(valid, id));
        Assert.Same(before, f.Document.Current);
    }
}
