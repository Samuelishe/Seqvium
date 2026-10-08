// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class DocumentTests
{
    [Fact]
    public void UnnamedLifecycleSeparatesIdentitySessionRevisionAndSavedState()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var initial = document.Current;
        Assert.Null(initial.State.Name);
        Assert.Null(document.Saved);
        Assert.Null(document.SavedPath);
        Assert.False(document.IsDirty);
        Assert.False(document.Undo());
        Assert.True(document.Edit("Name project", edit => edit.RenameDocument("First track")));
        Assert.True(document.IsDirty);
        Assert.Equal(initial.State.Id, document.Current.State.Id);
        Assert.NotEqual(initial.Revision, document.Current.Revision);
        ProjectPersistence.Save(document, directory.File("project.json"));
        Assert.False(document.IsDirty);
        Assert.Equal(document.Current, document.Saved);
        Assert.True(document.Edit("Rename", edit => edit.RenameDocument("New name")));
        Assert.True(document.IsDirty);
        Assert.True(document.Undo());
        Assert.False(document.IsDirty);
        Assert.True(document.Redo());
        Assert.True(document.IsDirty);
        var reopened = ProjectPersistence.Open(directory.File("project.json")).Document;
        Assert.Equal(initial.State.Id, reopened.Current.State.Id);
        Assert.NotEqual(document.LifecycleId, reopened.LifecycleId);
        Assert.Equal("First track", reopened.Current.State.Name);
        Assert.False(reopened.IsDirty);
        Assert.Equal(0, reopened.UndoCount);
        document.Close();
        Assert.True(document.IsClosed);
        Assert.Throws<InvalidOperationException>(() => document.Edit("Late edit", edit => edit.RenameDocument("Late")));
        Assert.Throws<InvalidOperationException>(() => document.Undo());
        Assert.Throws<InvalidOperationException>(() => ProjectPersistence.Save(document, directory.File("closed.json")));
    }

    [Fact]
    public void SharedMultiInstrumentPatternHasOneDefinitionAndDistinctIdentities()
    {
        var fixture = new SyntheticComposition();
        var state = fixture.Document.Current.State;
        Assert.Single(state.Patterns);
        Assert.Equal(2, fixture.SharedPattern.Parts.Length);
        Assert.Equal(3, state.Placements.Length);
        Assert.All(state.Placements, placement => Assert.Equal(fixture.Pattern, placement.PatternId));
        Assert.Equal(3, state.Placements.Select(item => item.Id).Distinct().Count());
        Assert.NotEqual(fixture.Pattern.Value, fixture.Group.Value);
        Assert.NotEqual(fixture.Kick.Value, fixture.Bass.Value);
        Assert.NotEqual(fixture.Resource.Value, fixture.Placements[0].Value);
        Assert.NotEqual(fixture.Route.Value, fixture.ContainerContext.Value);
        fixture.Document.Edit("Shared note", edit => edit.UpdateNote(fixture.Pattern, fixture.KickPart,
            fixture.KickNote, new(0), SyntheticComposition.Quarter, 38m, 0.9m));
        Assert.All(fixture.Document.Current.State.Placements, placement =>
        {
            var pattern = fixture.Document.Current.State.Patterns.Single(item => item.Id == placement.PatternId);
            Assert.Equal(38m, pattern.Parts.Single(part => part.Id == fixture.KickPart).Notes.Single().Pitch);
        });
        fixture.Document.Undo();
        Assert.Equal(36m, fixture.SharedPattern.Parts.Single(part => part.Id == fixture.KickPart).Notes.Single().Pitch);
    }

    [Fact]
    public void PlacementMoveAndLocalRelationshipsLeaveOtherOccurrencesAndOrganizationIntact()
    {
        var fixture = new SyntheticComposition();
        var before = fixture.Document.Current;
        fixture.Document.Edit("Move B", edit => edit.MovePlacement(fixture.Placements[1], new(13_000_000)));
        Assert.Equal(13_000_000, fixture.Placement(1).Position.Ticks);
        Assert.Equal(before.State.Placements[0], fixture.Placement(0));
        Assert.Equal(before.State.Placements[2], fixture.Placement(2));
        Assert.Equal(before.State.Patterns, fixture.Document.Current.State.Patterns);
        Assert.Equal(before.State.Groups, fixture.Document.Current.State.Groups);
        Assert.Equal(fixture.ItemContext, fixture.Placement(1).ItemContextId);
        fixture.Document.Undo();
        Assert.Same(before, fixture.Document.Current);
    }

    [Fact]
    public void ReplacingOneLocalContextIsAtomicAndLeavesSharedMusicAndOtherPlacementsIntact()
    {
        var fixture = new SyntheticComposition();
        var before = fixture.Document.Current;
        Id<ProcessingContext> replacement = default;
        fixture.Document.Edit("Replace B local context", edit =>
        {
            replacement = edit.AddContext("B new processing", LocalProcessingLevel.Item);
            edit.SetPlacementRelationships(fixture.Placements[1], replacement, fixture.ContainerContext, null,
                fixture.Placement(1).PartRelationships);
            edit.DeleteContext(fixture.ItemContext);
        });
        Assert.Equal(replacement, fixture.Placement(1).ItemContextId);
        Assert.Equal(before.State.Placements[0], fixture.Placement(0));
        Assert.Equal(before.State.Placements[2], fixture.Placement(2));
        Assert.Equal(before.State.Patterns, fixture.Document.Current.State.Patterns);
        Assert.Equal(before.State.Sounds, fixture.Document.Current.State.Sounds);
        Assert.Equal(before.State.Groups, fixture.Document.Current.State.Groups);
        fixture.Document.Undo();
        Assert.Same(before, fixture.Document.Current);
        fixture.Document.Redo();
        ProjectValidation.Validate(fixture.Document.Current.State);
    }

    [Fact]
    public void VariationIsOneCoherentEditWithFreshNotesAndPreservedSoundResourceAndProcessing()
    {
        var fixture = new SyntheticComposition();
        var before = fixture.Document.Current;
        Id<Pattern> variationId = default;
        fixture.Document.Edit("Make variation", edit => variationId = edit.MakePatternVariation(fixture.Placements[1], "Fill"));
        var after = fixture.Document.Current;
        var variation = after.State.Patterns.Single(item => item.Id == variationId);
        Assert.Equal(before.State.Sounds, after.State.Sounds);
        Assert.Equal(before.State.Resources, after.State.Resources);
        Assert.Equal(before.State.Contexts, after.State.Contexts);
        Assert.Equal(before.State.Groups, after.State.Groups);
        Assert.Equal(fixture.Pattern, fixture.Placement(0).PatternId);
        Assert.Equal(fixture.Pattern, fixture.Placement(2).PatternId);
        Assert.Equal(variationId, fixture.Placement(1).PatternId);
        for (int index = 0; index < variation.Parts.Length; index++)
        {
            Assert.NotEqual(fixture.SharedPattern.Parts[index].Id, variation.Parts[index].Id);
            Assert.NotEqual(fixture.SharedPattern.Parts[index].Notes[0].Id, variation.Parts[index].Notes[0].Id);
            Assert.Equal(fixture.SharedPattern.Parts[index].SoundId, variation.Parts[index].SoundId);
        }
        Assert.Equal(variation.Parts[0].Id, fixture.Placement(1).PartRelationships[0].PartId);
        Assert.Equal(fixture.Route, fixture.Placement(1).PartRelationships[0].RouteId);
        Assert.Equal(2, fixture.Document.UndoCount); // Setup and variation only.
        Assert.True(fixture.Document.Undo());
        Assert.Same(before, fixture.Document.Current);
        Assert.True(fixture.Document.Redo());
        Assert.Same(after, fixture.Document.Current);
        fixture.Document.Edit("Edit variation", edit => edit.UpdateNote(variationId, variation.Parts[0].Id,
            variation.Parts[0].Notes[0].Id, new(0), SyntheticComposition.Quarter, 40m, 0.5m));
        Assert.Equal(36m, fixture.SharedPattern.Parts[0].Notes[0].Pitch);
        ProjectValidation.Validate(fixture.Document.Current.State);
    }

    [Fact]
    public void IndependentSoundCopiesConfigurationButSharesResourcesAndDoesNotCopyNotes()
    {
        var fixture = new SyntheticComposition();
        Id<Pattern> variationId = default;
        fixture.Document.Edit("Variation", edit => variationId = edit.MakePatternVariation(fixture.Placements[1], "Fill"));
        var variation = fixture.Document.Current.State.Patterns.Single(item => item.Id == variationId);
        var before = fixture.Document.Current;
        Id<SoundDefinition> independentId = default;
        fixture.Document.Edit("Independent sound", edit =>
            independentId = edit.MakeSoundIndependent(variationId, variation.Parts[0].Id, "Independent Kick"));
        Assert.Equal(3, fixture.Document.Current.State.Sounds.Length);
        Assert.Equal(fixture.Kick, fixture.SharedPattern.Parts[0].SoundId);
        var independentPart = fixture.Document.Current.State.Patterns.Single(item => item.Id == variationId).Parts[0];
        Assert.Equal(independentId, independentPart.SoundId);
        Assert.Equal(variation.Parts[0].Notes, independentPart.Notes);
        Assert.Single(fixture.Document.Current.State.Resources);
        Assert.Equal(fixture.Resource, fixture.Document.Current.State.Sounds.Single(item => item.Id == independentId).ResourceIds.Single());
        fixture.Document.Undo();
        Assert.Same(before, fixture.Document.Current);
        fixture.Document.Redo();
        fixture.Document.Edit("Configure independent sound", edit =>
            edit.SetSoundParameters(independentId, ImmutableDictionary<string, decimal>.Empty.Add("gain", 0.25m)));
        Assert.Empty(fixture.Document.Current.State.Sounds.Single(item => item.Id == fixture.Kick).Parameters);
        Assert.Equal(0.25m, fixture.Document.Current.State.Sounds.Single(item => item.Id == independentId).Parameters["gain"]);
    }

    [Fact]
    public void InvalidOrThrowingMultiPartEditsPublishNothingAndPreserveRedo()
    {
        var fixture = new SyntheticComposition();
        fixture.Document.Edit("Move", edit => edit.MovePlacement(fixture.Placements[0], new(50)));
        fixture.Document.Undo();
        var before = fixture.Document.Current;
        var generation = fixture.Document.Generation;
        Assert.Throws<ProjectValidationException>(() => fixture.Document.Edit("Invalid multi edit", edit =>
        {
            edit.MakePatternVariation(fixture.Placements[1], "Candidate");
            edit.AddPlacement(Id<Pattern>.New(), new(0));
        }));
        Assert.Same(before, fixture.Document.Current);
        Assert.Equal(generation, fixture.Document.Generation);
        Assert.Equal(1, fixture.Document.UndoCount);
        Assert.Equal(1, fixture.Document.RedoCount);
        Assert.Throws<InvalidOperationException>(() => fixture.Document.Edit("Failure", edit =>
        {
            edit.RenameDocument("Partial");
            throw new InvalidOperationException("Synthetic failure");
        }));
        Assert.Same(before, fixture.Document.Current);
        Assert.False(fixture.Document.Edit("No-op", edit => edit.MovePlacement(fixture.Placements[0], fixture.Placement(0).Position)));
        Assert.Equal(1, fixture.Document.RedoCount);
        Assert.True(fixture.Document.Redo());
    }

    [Fact]
    public void DeletionRejectsReferencedDefinitionsAndRestoresOwnedLocalRelationships()
    {
        var fixture = new SyntheticComposition();
        var before = fixture.Document.Current;
        foreach (Action<ProjectEdit> deletion in new Action<ProjectEdit>[]
        {
            edit => edit.DeletePattern(fixture.Pattern), edit => edit.DeleteSound(fixture.Kick),
            edit => edit.DeleteResource(fixture.Resource), edit => edit.DeleteContext(fixture.ContainerContext),
            edit => edit.DeleteRoute(fixture.Route)
        })
        {
            Assert.Throws<ProjectValidationException>(() => fixture.Document.Edit("Reject referenced deletion", deletion));
            Assert.Same(before, fixture.Document.Current);
        }
        fixture.Document.Edit("Delete B", edit => edit.DeletePlacement(fixture.Placements[1]));
        Assert.Equal(2, fixture.Document.Current.State.Placements.Length);
        Assert.DoesNotContain(fixture.Document.Current.State.Contexts, item => item.Id == fixture.ItemContext);
        Assert.Single(fixture.Document.Current.State.Patterns);
        Assert.Single(fixture.Document.Current.State.Resources);
        Assert.Single(fixture.Document.Current.State.Groups);
        fixture.Document.Undo();
        Assert.Same(before, fixture.Document.Current);
        fixture.Document.Edit("Delete organization only", edit => edit.DeleteGroup(fixture.Group));
        Assert.Equal(before.State.Placements, fixture.Document.Current.State.Placements);
        Assert.Equal(before.State.Contexts, fixture.Document.Current.State.Contexts);
        fixture.Document.Edit("Delete all uses and definition coherently", edit =>
        {
            foreach (var placement in fixture.Placements) edit.DeletePlacement(placement);
            edit.DeletePattern(fixture.Pattern);
        });
        Assert.Empty(fixture.Document.Current.State.Patterns);
        Assert.Equal(2, fixture.Document.Current.State.Sounds.Length);
        Assert.Contains(fixture.Document.RetainedSnapshots, item => item.Revision == before.Revision);
        fixture.Document.Undo();
        ProjectValidation.Validate(fixture.Document.Current.State);
    }

    [Fact]
    public void ProjectDefaultsAreCopiedAndUserPreferenceReplacementCannotChangeAProject()
    {
        var preferences = new ProjectSettings(new Tempo(90m), new Meter(3, 4));
        var document = ProjectDocument.Create(preferences);
        preferences = preferences with { Tempo = new Tempo(150m), Meter = new Meter(7, 8) };
        Assert.Equal(90m, document.Current.State.Settings.Tempo.BeatsPerMinute);
        Assert.Equal(3, document.Current.State.Settings.Meter.Numerator);
        var other = ProjectDocument.Create(preferences);
        Assert.Equal(150m, other.Current.State.Settings.Tempo.BeatsPerMinute);
        Assert.NotEqual(document.Current.State.Id, other.Current.State.Id);
        using var stream = new MemoryStream(ProjectPersistence.Encode(document));
        var reopened = ProjectPersistence.Read(stream).Document;
        Assert.Equal(document.Current.State.Settings, reopened.Current.State.Settings);
        Assert.DoesNotContain("theme", System.Text.Encoding.UTF8.GetString(ProjectPersistence.Encode(document)), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReentrancyAndRetainedEditorsCannotMutateCanonicalHistory()
    {
        var document = ProjectDocument.Create();
        ProjectEdit? retained = null;
        var before = document.Current;
        Assert.Throws<InvalidOperationException>(() => document.Edit("Reentrant", edit =>
        {
            retained = edit;
            edit.RenameDocument("Partial");
            document.Edit("Nested", inner => inner.RenameDocument("Nested"));
        }));
        Assert.Same(before, document.Current);
        Assert.NotNull(retained);
        Assert.Throws<InvalidOperationException>(() => retained.RenameDocument("Late"));
        Assert.Equal(0, document.UndoCount);
    }

    [Fact]
    public void HistoryIsBoundedAndUnrelatedEditsNeverMerge()
    {
        var document = ProjectDocument.Create(historyLimit: 2);
        document.Edit("One", edit => edit.RenameDocument("One"));
        document.Edit("Two", edit => edit.RenameDocument("Two"));
        document.Edit("Three", edit => edit.RenameDocument("Three"));
        Assert.Equal(2, document.UndoCount);
        Assert.Equal("Three", document.NextUndoDescription);
        var generation = document.Generation;
        document.Undo();
        Assert.Equal("Two", document.Current.State.Name);
        document.Undo();
        Assert.Equal("One", document.Current.State.Name);
        Assert.False(document.Undo());
        Assert.Equal(generation + 2, document.Generation);
        document.Edit("Branch", edit => edit.RenameDocument("Branch"));
        Assert.Equal(0, document.RedoCount);
    }

    [Fact]
    public void EqualReplacementSettingsAndRelationshipsDoNotInventAcceptedEdits()
    {
        var fixture = new SyntheticComposition();
        var before = fixture.Document.Current;
        Assert.False(fixture.Document.Edit("Same settings", edit => edit.SetSettings(new(new Tempo(120m), new Meter(4, 4)))));
        Assert.False(fixture.Document.Edit("Same relationships", edit => edit.SetPlacementRelationships(
            fixture.Placements[1], fixture.ItemContext, fixture.ContainerContext, null,
            [new(fixture.KickPart, null, fixture.Route), new(fixture.BassPart, null, null)])));
        Assert.Same(before, fixture.Document.Current);
        Assert.Equal(1, fixture.Document.UndoCount);
    }

    [Fact]
    public void ComposedNetZeroTransactionDoesNotConsumeRevisionOrInvalidateRedo()
    {
        var fixture = new SyntheticComposition();
        fixture.Document.Edit("Rename", edit => edit.RenameDocument("Other"));
        fixture.Document.Undo();
        var before = fixture.Document.Current;
        var generation = fixture.Document.Generation;
        Assert.False(fixture.Document.Edit("Net zero", edit =>
        {
            edit.MovePlacement(fixture.Placements[0], new(100));
            edit.MovePlacement(fixture.Placements[0], new(0));
            edit.UpdateNote(fixture.Pattern, fixture.KickPart, fixture.KickNote, new(0), SyntheticComposition.Quarter, 40m, 0.8m);
            edit.UpdateNote(fixture.Pattern, fixture.KickPart, fixture.KickNote, new(0), SyntheticComposition.Quarter, 36m, 0.8m);
            edit.SetSoundParameters(fixture.Kick, ImmutableDictionary<string, decimal>.Empty.Add("gain", 0.5m));
            edit.SetSoundParameters(fixture.Kick, ImmutableDictionary<string, decimal>.Empty);
        }));
        Assert.Same(before, fixture.Document.Current);
        Assert.Equal(generation, fixture.Document.Generation);
        Assert.Equal(1, fixture.Document.UndoCount);
        Assert.Equal(1, fixture.Document.RedoCount);
    }

    [Fact]
    public void ProcessingRejectsSharedItemContextsAndDivergentRoutesAfterMix()
    {
        var fixture = new SyntheticComposition();
        var before = fixture.Document.Current;
        Assert.Throws<ProjectValidationException>(() => fixture.Document.Edit("Share item context", edit =>
            edit.SetPlacementRelationships(fixture.Placements[0], fixture.ItemContext, null, null, [])));
        Assert.Throws<ProjectValidationException>(() => fixture.Document.Edit("Contradictory mix", edit =>
        {
            var context = edit.AddContext("Explicit submix", LocalProcessingLevel.Containing, intentionalMix: true);
            edit.SetPlacementRelationships(fixture.Placements[0], null, context, null, [new(fixture.KickPart, null, fixture.Route)]);
        }));
        Assert.Same(before, fixture.Document.Current);
        fixture.Document.Edit("Explicit submix", edit =>
        {
            var context = edit.AddContext("Explicit submix", LocalProcessingLevel.Containing, intentionalMix: true);
            edit.SetPlacementRelationships(fixture.Placements[0], null, context, fixture.Route, []);
        });
        ProjectValidation.Validate(fixture.Document.Current.State);
    }
}
