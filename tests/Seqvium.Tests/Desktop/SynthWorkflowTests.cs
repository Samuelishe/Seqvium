// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class SynthWorkflowTests
{
    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Assert.True(Joined);
    }

    [Fact]
    public async Task CreationChoosesMelodicNavigationAndSharedEditsRejectStaleAbaAndMalformedParameters()
    {
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create());
        Assert.True(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Synth"));
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Assert.True(await workflow.AddDrumVoiceAsync(Basis(), "Synth", SynthVoice.Algorithm));
        var sound = workflow.State.Snapshot.State.Sounds.Single(); Assert.True(SynthVoice.Supports(sound));
        Assert.Equal(WorkspaceState.PianoId, new PatternEditorNavigation().Resolve(workflow.State).Pane);
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId, attachment.Sources.Single().NodeId);
        long generation = workflow.State.Generation; int undo = workflow.State.UndoCount;
        var next = sound.Parameters.SetItem("mix", .75m);
        Assert.True(await workflow.SetSynthParametersAsync(target, generation, sound.Parameters, next));
        Assert.Equal(undo + 1, workflow.State.UndoCount);
        Assert.True(await workflow.UndoAsync());
        Assert.False(await workflow.SetSynthParametersAsync(target, generation, sound.Parameters, next));
        Assert.Equal(sound.Parameters, workflow.State.Snapshot.State.Sounds.Single().Parameters);
        Assert.False(await workflow.SetSynthParametersAsync(target, workflow.State.Generation, sound.Parameters, next.Remove("attack")));
        Assert.Equal(sound.Parameters, workflow.State.Snapshot.State.Sounds.Single().Parameters);
    }

    [Fact]
    public async Task HeldPreviewUsesExistingBorrowerReleasesNaturallyAndStopJoinsWithoutCanonicalEdits()
    {
        Borrower? borrower = null; RealtimeSampler? sampler = null;
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), () =>
        {
            borrower = new(); sampler = new(48000, 2, 128);
            return new(borrower, sampler, "Controlled synth output", () => null);
        });
        Assert.True(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Synth"));
        Assert.True(await workflow.AddDrumVoiceAsync(BeatSteps.Resolve(workflow.State).Basis!, "Synth", SynthVoice.Algorithm));
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId, attachment.Sources.Single().NodeId);
        var before = workflow.State.Snapshot; int undo = workflow.State.UndoCount;
        Assert.True(await workflow.HoldSynthAsync(target, workflow.State.Generation, 60.5m, .8m));
        var packet = new float[256]; Assert.True(sampler!.Process(packet)); Assert.Equal(1, sampler.ActiveVoices);
        Assert.True(await workflow.ReleaseSamplerAsync());
        for (int i = 0; i < 100 && sampler.ReadStatus().Playing; i++) Assert.True(sampler.Process(packet));
        Assert.Equal(0, sampler.ActiveVoices); Assert.False(sampler.ReadStatus().Playing);
        Assert.True(await workflow.StopSamplerAsync()); Assert.True(borrower!.Joined);
        Assert.Equal(before, workflow.State.Snapshot); Assert.Equal(undo, workflow.State.UndoCount);
    }
}
