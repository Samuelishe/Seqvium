// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class IntegrityTests
{
    [Theory]
    [InlineData("pitch")]
    [InlineData("intensity")]
    [InlineData("length")]
    [InlineData("missing-part")]
    [InlineData("missing-note")]
    [InlineData("missing-placement")]
    [InlineData("wrong-context-level")]
    [InlineData("unknown-route")]
    [InlineData("part-from-other-pattern")]
    [InlineData("shared-performance-different-sounds")]
    [InlineData("orphan-item-context")]
    public void RejectedOperationsLeaveSavedStateRevisionHistoryAndRelationshipsUnchanged(string failure)
    {
        using var directory = new TemporaryDirectory();
        var fixture = new SyntheticComposition();
        ProjectPersistence.Save(fixture.Document, directory.File("valid.json"));
        var current = fixture.Document.Current;
        var generation = fixture.Document.Generation;
        var bytes = ProjectPersistence.Encode(fixture.Document);
        Assert.Throws<ProjectValidationException>(() => fixture.Document.Edit("Rejected", edit =>
        {
            switch (failure)
            {
                case "pitch": edit.UpdateNote(fixture.Pattern, fixture.KickPart, fixture.KickNote, new(0), SyntheticComposition.Quarter, -1m, 1m); break;
                case "intensity": edit.UpdateNote(fixture.Pattern, fixture.KickPart, fixture.KickNote, new(0), SyntheticComposition.Quarter, 36m, 1.1m); break;
                case "length": edit.UpdateNote(fixture.Pattern, fixture.KickPart, fixture.KickNote, new(4 * MusicalPosition.TicksPerQuarter), SyntheticComposition.Quarter, 36m, 1m); break;
                case "missing-part": edit.AddNote(fixture.Pattern, Id<MusicalPart>.New(), new(0), SyntheticComposition.Quarter, 36m, 1m); break;
                case "missing-note": edit.DeleteNote(fixture.Pattern, fixture.KickPart, Id<NoteEvent>.New()); break;
                case "missing-placement": edit.MakePatternVariation(Id<PatternPlacement>.New(), "Variation"); break;
                case "wrong-context-level": edit.SetPlacementRelationships(fixture.Placements[0], fixture.ContainerContext, null, null, []); break;
                case "unknown-route": edit.SetPlacementRelationships(fixture.Placements[0], null, null, Id<RouteIntent>.New(), []); break;
                case "part-from-other-pattern":
                    var other = edit.AddPattern("Other", SyntheticComposition.Quarter);
                    var otherPart = edit.AddPart(other, "Other part", fixture.Kick);
                    edit.SetPlacementRelationships(fixture.Placements[0], null, null, null, [new(otherPart, null, null)]);
                    break;
                case "shared-performance-different-sounds":
                    var key = Guid.NewGuid();
                    edit.SetPlacementRelationships(fixture.Placements[0], null, null, null, [new(fixture.KickPart, key, null), new(fixture.BassPart, key, null)]);
                    break;
                case "orphan-item-context": edit.AddContext("Unowned", LocalProcessingLevel.Item); break;
                default: throw new InvalidOperationException("Unknown fixture mutation.");
            }
        }));
        Assert.Same(current, fixture.Document.Current);
        Assert.Same(current, fixture.Document.Saved);
        Assert.False(fixture.Document.IsDirty);
        Assert.Equal(1, fixture.Document.UndoCount);
        Assert.Equal(0, fixture.Document.RedoCount);
        Assert.Equal(generation, fixture.Document.Generation);
        Assert.Equal(bytes, ProjectPersistence.Encode(fixture.Document));
    }

    [Fact]
    public void NoteDeletionAndIndependentSoundHaveSeparateUndoBoundaries()
    {
        var fixture = new SyntheticComposition();
        fixture.Document.Edit("Remove one note", edit => edit.DeleteNote(fixture.Pattern, fixture.KickPart, fixture.KickNote));
        var deleted = fixture.Document.Current;
        Assert.Empty(fixture.SharedPattern.Parts[0].Notes);
        fixture.Document.Edit("Change sound scope", edit => edit.MakeSoundIndependent(fixture.Pattern, fixture.KickPart, "Other sound"));
        Assert.All(fixture.Document.Current.State.Placements, placement => Assert.Equal(fixture.Pattern, placement.PatternId));
        Assert.Empty(fixture.SharedPattern.Parts[0].Notes);
        fixture.Document.Undo();
        Assert.Same(deleted, fixture.Document.Current);
        fixture.Document.Undo();
        Assert.Equal(fixture.KickNote, fixture.SharedPattern.Parts[0].Notes.Single().Id);
        Assert.Equal(fixture.Kick, fixture.SharedPattern.Parts[0].SoundId);
    }

    [Fact]
    public void SoundResourceAndContributionIdentitiesRemainSeparateAcrossExplicitInteractionAndSerialization()
    {
        var fixture = new SyntheticComposition();
        var key = Guid.NewGuid();
        fixture.Document.Edit("Explicit performance interaction", edit =>
        {
            edit.SetPlacementRelationships(fixture.Placements[0], null, null, null, [new(fixture.KickPart, key, fixture.Route)]);
            edit.SetPlacementRelationships(fixture.Placements[2], null, null, null, [new(fixture.KickPart, key, fixture.Route)]);
        });
        using var stream = new MemoryStream(ProjectPersistence.Encode(fixture.Document));
        var state = ProjectPersistence.Read(stream).Document.Current.State;
        Assert.Equal(key, state.Placements[0].PartRelationships[0].SharedPerformanceKey);
        Assert.Equal(key, state.Placements[2].PartRelationships[0].SharedPerformanceKey);
        Assert.Null(state.Placements[1].PartRelationships[0].SharedPerformanceKey);
        Assert.Equal(2, state.Sounds.Length);
        Assert.Single(state.Resources);
        Assert.NotEqual(state.Placements[0].Id, state.Placements[2].Id);
        // Same intent key does not select physical instances, capabilities or a graph schedule.
    }

    [Fact]
    public void SavedSnapshotAndHistoryRetainResourcesAfterWorkingDeletion()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        Id<ResourceDescriptor> resource = default;
        document.Edit("Accept descriptor", edit => resource = edit.AddResource("Synthetic"));
        ProjectPersistence.Save(document, directory.File("saved.json"));
        var saved = document.Saved;
        document.Edit("Delete unreferenced descriptor", edit => edit.DeleteResource(resource));
        Assert.Empty(document.Current.State.Resources);
        Assert.Contains(document.RetainedSnapshots, snapshot => snapshot.State.Resources.Any(item => item.Id == resource));
        Assert.Same(saved, document.Saved);
        document.Undo();
        Assert.Equal(resource, document.Current.State.Resources.Single().Id);
        Assert.False(document.IsDirty);
        document.Redo();
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void EntityCountLimitAndDuplicateIdsAreEnforcedWithoutNameHeuristics()
    {
        var state = ProjectDocument.Create().Current.State;
        var resources = Enumerable.Range(0, ProjectValidation.MaximumEntities)
            .Select(_ => new ResourceDescriptor(Id<ResourceDescriptor>.New(), "Same name", null, null)).ToImmutableArray();
        Assert.Throws<ProjectValidationException>(() => ProjectValidation.Validate(state with { Resources = resources }));
        var first = resources[0];
        Assert.Throws<ProjectValidationException>(() => ProjectValidation.Validate(state with { Resources = [first, first] }));
        ProjectValidation.Validate(state with { Resources = [resources[0], resources[1]] });
        Assert.NotEqual(resources[0].Id, resources[1].Id);
    }

    [Fact]
    public void OversizedSerializationCannotOverwriteAnExistingSaveOrMarkNewWorkSaved()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var path = directory.File("existing.json");
        ProjectPersistence.Save(document, path);
        var originalBytes = File.ReadAllBytes(path);
        var saved = document.Saved;
        document.Edit("Oversized opaque data", edit => edit.AddSound("Large foreign state", "extension", extension:
            new("org.example.large", 1, JsonSerializer.SerializeToElement(new string('x', ProjectPersistence.MaximumBytes)))));
        Assert.Throws<ProjectFormatException>(() => ProjectPersistence.Save(document, path));
        Assert.Equal(originalBytes, File.ReadAllBytes(path));
        Assert.Same(saved, document.Saved);
        Assert.True(document.IsDirty);
        Assert.Equal(1, document.UndoCount);
        Assert.Empty(Directory.GetFiles(directory.Path, ".seqvium-*.tmp"));
    }
}
