// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using System.Text.Json.Nodes;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class RepeatVariationTests
{
    public static TheoryData<int, int> Boundaries => new()
    {
        { 2, 0 }, { 2, 1 }, { 4, 0 }, { 4, 1 }, { 4, 3 },
        { 8, 0 }, { 8, 4 }, { 8, 7 }, { 1024, 0 }, { 1024, 512 }, { 1024, 1023 }
    };

    [Theory, MemberData(nameof(Boundaries))]
    public void ExactGeometryIdentitySharingAndHistory(int repeats, int index)
    {
        var f = new GraphFixture();
        try
        {
            Id<ArrangementClip> clip = default, other = default;
            f.Document.Edit("Uses", e =>
            {
                e.SetPatternLength(f.Pattern, new(1234567));
                clip = e.AddArrangementClip(f.Placement, new(37), 3, repeats);
                other = e.AddArrangementClip(f.Placement, new(123), 4);
                e.InitializeMixer();
            });
            var before = f.Document.Current; int undo = f.Document.UndoCount;
            Id<ArrangementClip> center = default;
            f.Document.Edit("Unique repeat", e => center = e.MakeArrangementRepeatUnique(clip, index, "Fill"));
            var after = f.Document.Current; var state = after.State;
            var pieces = state.ArrangementClips.Where(c => c.Id != other).OrderBy(c => c.Position.Ticks).ToArray();
            Assert.Equal(index > 0 && index < repeats - 1 ? 3 : 2, pieces.Length);
            Assert.Equal(37, pieces[0].Position.Ticks);
            Assert.Equal(37 + 1234567L * repeats, PlaylistEditing.EndTicks(state, pieces[^1]));
            Assert.Equal(repeats, pieces.Sum(c => c.Repeats));
            Assert.All(pieces, c => { Assert.Equal(3, c.Lane); Assert.True(c.Repeats > 0); });
            for (int i = 1; i < pieces.Length; i++)
                Assert.Equal(PlaylistEditing.EndTicks(state, pieces[i - 1]), pieces[i].Position.Ticks);
            var unique = pieces.Single(c => c.Id == center);
            Assert.NotEqual(clip, center); Assert.Equal(1, unique.Repeats);
            Assert.Equal(37 + 1234567L * index, unique.Position.Ticks);
            Assert.Equal(clip, (index == 0 ? pieces[^1] : pieces[0]).Id);
            Assert.All(pieces.Where(c => c.Id != center), c => Assert.Equal(f.Placement, c.SourcePlacementId));
            Assert.Equal(before.State.ArrangementClips.Single(c => c.Id == other), state.ArrangementClips.Single(c => c.Id == other));
            Assert.Equal(before.State.Sounds, state.Sounds); Assert.Equal(before.State.Resources, state.Resources);
            Assert.Equal(before.State.Mixer, state.Mixer);
            var placement = state.Placements.Single(p => p.Id == unique.SourcePlacementId);
            var pattern = state.Patterns.Single(p => p.Id == placement.PatternId);
            Assert.Equal(f.Pattern, pattern.VariationOf);
            Assert.All(pattern.Parts, p => Assert.DoesNotContain(before.State.Patterns.SelectMany(p => p.Parts), old => old.Id == p.Id));
            var attachment = state.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId);
            Assert.All(attachment.Sources, s => { Assert.Equal(placement.Id, s.PlacementId); Assert.Contains(pattern.Parts, p => p.Id == s.PartId); });
            Assert.Equal(undo + 1, f.Document.UndoCount);
            Assert.True(f.Document.Undo()); Assert.Same(before, f.Document.Current);
            Assert.True(f.Document.Redo()); Assert.Same(after, f.Document.Current);
            var reopened = GraphFixture.Reopen(f.Document).Document;
            try { Assert.True(ProjectDocument.HasSameContent(state, reopened.Current.State)); }
            finally { reopened.Close(); }
            var originalNote = before.State.Patterns.Single(p => p.Id == f.Pattern).Parts[0].Notes[0];
            f.Document.Edit("Shared phrase", e => e.UpdateNote(f.Pattern, f.FirstPart, originalNote.Id,
                originalNote.Position, originalNote.Duration, 65, originalNote.Intensity));
            Assert.Equal(pattern, f.Document.Current.State.Patterns.Single(p => p.Id == pattern.Id));
            Assert.All(pieces.Where(c => c.Id != center), c => Assert.Equal(f.Pattern,
                f.Document.Current.State.Placements.Single(p => p.Id == c.SourcePlacementId).PatternId));
        }
        finally { f.Document.Close(); }
    }

    [Theory]
    [InlineData(1, 0)] [InlineData(4, -1)] [InlineData(4, 4)] [InlineData(4, int.MaxValue)]
    public void InvalidIndexRefusesAtomically(int repeats, int index)
    {
        var f = new GraphFixture();
        try
        {
            Id<ArrangementClip> clip = default;
            f.Document.Edit("Uses", e => clip = e.AddArrangementClip(f.Placement, default, 0, repeats));
            var before = f.Document.Current; int undo = f.Document.UndoCount;
            Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Unique", e => e.MakeArrangementRepeatUnique(clip, index, "Fill")));
            Assert.Same(before, f.Document.Current); Assert.Equal(undo, f.Document.UndoCount);
        }
        finally { f.Document.Close(); }
    }

    [Fact]
    public async Task UnsupportedGraphAndStaleAbaRefuseWithoutPartialChanges()
    {
        var f = new GraphFixture();
        try
        {
            Id<ArrangementClip> clip = default;
            f.Document.Edit("Uses", e => clip = e.AddArrangementClip(f.Placement, default, 0, 4));
            var json = GraphFixture.Json(f.Document);
            json["state"]!["graphs"]![0]!["future"] = "unknown identity semantics";
            var reopened = GraphFixture.Read(json).Document;
            await using var workflow = new DesktopWorkflow(reopened);
            var before = reopened.Current; int undo = reopened.UndoCount;
            Assert.Null(await workflow.MakeArrangementRepeatUniqueAsync(ArrangementEditBasis.From(workflow.State), clip, 2, "Fill"));
            Assert.Same(before, reopened.Current); Assert.Equal(undo, reopened.UndoCount);
            var stale = ArrangementEditBasis.From(workflow.State);
            await workflow.UpdateArrangementClipAsync(stale, clip, new(123), 0, 4);
            await workflow.UndoAsync();
            Assert.Null(await workflow.MakeArrangementRepeatUniqueAsync(stale, clip, 2, "Fill"));
            Assert.Same(before, reopened.Current);
        }
        finally { f.Document.Close(); }
    }

    [Theory]
    [InlineData("clip")] [InlineData("pattern")] [InlineData("part")] [InlineData("note")]
    [InlineData("placement")] [InlineData("context")] [InlineData("attachment")]
    [InlineData("binding")] [InlineData("node")] [InlineData("port")] [InlineData("connection")]
    [InlineData("extension")] [InlineData("node-type")] [InlineData("node-version")]
    public void UnknownIdentityOrUnsupportedProcessingRefusesTheWholeSplit(string fault)
    {
        var f = new GraphFixture();
        try
        {
            Id<ArrangementClip> clip = default;
            f.Document.Edit("Uses", e => clip = e.AddArrangementClip(f.Placement, default, 0, 4));
            var json = GraphFixture.Json(f.Document); var state = json["state"]!;
            var node = state["graphs"]![0]!["nodes"]![0]!;
            JsonNode target = fault switch
            {
                "clip" => state["arrangementClips"]![0]!, "pattern" => state["patterns"]![0]!,
                "part" => state["patterns"]![0]!["parts"]![0]!, "note" => state["patterns"]![0]!["parts"]![0]!["notes"]![0]!,
                "placement" => state["placements"]![0]!, "context" => state["contexts"]![0]!,
                "attachment" => state["graphAttachments"]![0]!, "binding" => state["graphAttachments"]![0]!["sources"]![0]!,
                "port" => node["ports"]![0]!, "connection" => state["graphs"]![0]!["connections"]![0]!, _ => node
            };
            if (fault == "extension") target["extension"] = JsonNode.Parse("{\"extensionId\":\"org.example.fx\",\"stateVersion\":1,\"payload\":{}}");
            else if (fault == "node-type") target["type"] = "org.example.fx";
            else if (fault == "node-version") target["stateVersion"] = 99;
            else target["futureIdentity"] = "unknown";
            var loaded = GraphFixture.Read(json).Document;
            try
            {
                var before = loaded.Current; int undo = loaded.UndoCount;
                Assert.Throws<ProjectValidationException>(() => loaded.Edit("Unique", e => e.MakeArrangementRepeatUnique(clip, 1, "Fill")));
                Assert.Same(before, loaded.Current); Assert.Equal(undo, loaded.UndoCount);
                Assert.True(JsonNode.DeepEquals(json, GraphFixture.Json(loaded)));
            }
            finally { loaded.Close(); }
        }
        finally { f.Document.Close(); }
    }

    [Fact]
    public void NoteCrossingPatternBoundaryRemainsAnAtomicCanonicalRefusal()
    {
        var f = new GraphFixture();
        try
        {
            var before = f.Document.Current;
            Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Crossing note", e =>
                e.AddNote(f.Pattern, f.FirstPart, new(1), new(MusicalPosition.TicksPerQuarter), 60, .5m)));
            Assert.Same(before, f.Document.Current);
        }
        finally { f.Document.Close(); }
    }
}
