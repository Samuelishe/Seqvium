// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class PatternEditorNavigationTests
{
    [Fact]
    public async Task EmptyAndRhythmicPatternsOpenRackButMelodyFindsPartBeyondFirstRow()
    {
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create());
        var navigation = new PatternEditorNavigation();
        Assert.Equal(WorkspaceState.BeatId, navigation.Resolve(workflow.State).Pane);
        await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Groove");
        var basis = BeatSteps.Resolve(workflow.State).Basis!;
        await workflow.AddDrumVoiceAsync(basis, "Kick");
        basis = BeatSteps.Resolve(workflow.State).Basis!;
        var kick = BeatSteps.Resolve(workflow.State).Pattern!.Parts[0];
        await workflow.ToggleBeatStepAsync(basis, kick.Id, 0);
        Assert.Equal(WorkspaceState.BeatId, navigation.Resolve(workflow.State).Pane);
        await workflow.AddDrumVoiceAsync(BeatSteps.Resolve(workflow.State).Basis!, "Toms");
        var toms = BeatSteps.Resolve(workflow.State).Pattern!.Parts[1];
        var root = workflow.State.Snapshot.State.Sounds.Single(s => s.Id == toms.SoundId).Parameters["rootPitch"];
        await workflow.EditPianoNotesAsync(BeatSteps.Resolve(workflow.State).Basis!, toms.Id,
            [new(Id<NoteEvent>.New(), new(0), new(BeatSteps.StepTicks), root + 3, .8m)]);
        Assert.Equal((WorkspaceState.PianoId, (Id<MusicalPart>?)toms.Id), navigation.Resolve(workflow.State));
        Assert.Equal(toms.Id, PatternEditorNavigation.PreferredPart(workflow.State.Snapshot.State,
            BeatSteps.Resolve(workflow.State).Pattern!)!.Id);
    }

    [Fact]
    public async Task ExplicitEditorAndPartAreRememberedPerPatternAndExpiredByReplacement()
    {
        using var directory = new TemporaryDirectory();
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), mediaDirectory: directory.File("owned"));
        string wav = directory.File("line.wav");
        await File.WriteAllBytesAsync(wav, WavFixtures.Create(Enumerable.Repeat(.1f, 48000).ToArray()),
            TestContext.Current.CancellationToken);
        await workflow.ImportAsync(wav, TestContext.Current.CancellationToken);
        var navigation = new PatternEditorNavigation();
        var first = workflow.State.Selected!.Value;
        var part = BeatSteps.Resolve(workflow.State).Pattern!.Parts[0];
        Assert.Equal(WorkspaceState.PianoId, navigation.Resolve(workflow.State).Pane);
        navigation.Remember(workflow.State, WorkspaceState.BeatId);
        await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Empty");
        Assert.Equal(WorkspaceState.BeatId, navigation.Resolve(workflow.State).Pane);
        await workflow.SelectAsync(first);
        Assert.Equal(WorkspaceState.BeatId, navigation.Resolve(workflow.State).Pane);
        navigation.Remember(workflow.State, WorkspaceState.PianoId, part.Id);
        Assert.Equal((WorkspaceState.PianoId, (Id<MusicalPart>?)part.Id), navigation.Resolve(workflow.State));
        navigation.Remember(workflow.State, WorkspaceState.BeatId);
        string saved = directory.File("music.seqvium");
        await workflow.SaveAsync(saved);
        await workflow.OpenAsync(saved, UnsavedDecision.Discard);
        Assert.Equal(WorkspaceState.PianoId, navigation.Resolve(workflow.State).Pane);
    }

    [Fact]
    public async Task DeletedRememberedPartFallsBackToAvailableContent()
    {
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create());
        await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Beat");
        await workflow.AddDrumVoiceAsync(BeatSteps.Resolve(workflow.State).Basis!, "Kick");
        var part = BeatSteps.Resolve(workflow.State).Pattern!.Parts[0];
        var navigation = new PatternEditorNavigation();
        navigation.Remember(workflow.State, WorkspaceState.PianoId, part.Id);
        await workflow.DeleteBeatChannelAsync(BeatSteps.Resolve(workflow.State).Basis!, part.Id);
        Assert.Equal((WorkspaceState.BeatId, (Id<MusicalPart>?)null), navigation.Resolve(workflow.State));
    }
}
