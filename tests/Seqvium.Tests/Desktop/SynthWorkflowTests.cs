// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class SynthWorkflowTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly DesktopWorkflow Workflow;
        internal int Opens;
        internal string? Failure;
        internal RealtimeSampler Sampler = null!;
        internal Borrower Output = null!;

        internal Fixture() => Workflow = new(ProjectDocument.Create(), () =>
        {
            Opens++;
            Output = new();
            Sampler = new(44100, 2, 128);
            return new(Output, Sampler, "Controlled synth output", () => Failure);
        });

        internal NodeEditorTarget Target => new(Workflow.State.Document.LifecycleId,
            Workflow.State.Snapshot.State.GraphAttachments.Single().Id,
            Workflow.State.Snapshot.State.GraphAttachments.Single().GraphId,
            Workflow.State.Snapshot.State.GraphAttachments.Single().Sources.Single().NodeId);

        internal SoundDefinition Sound => Workflow.State.Snapshot.State.Sounds.Single();

        internal async Task Create()
        {
            Assert.True(await Workflow.CreateBeatAsync(Workflow.State.Document.LifecycleId, Workflow.State.Generation,
                "Synth"));
            Assert.True(await Workflow.AddDrumVoiceAsync(BeatSteps.Resolve(Workflow.State).Basis!, "Synth",
                SynthVoice.Algorithm));
        }

        internal void Tick()
        {
            Assert.True(Sampler.Process(new float[256]));
            Sampler.RetireCompleted();
        }

        public ValueTask DisposeAsync() => Workflow.DisposeAsync();
    }

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
        Assert.True(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
            "Synth"));
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Assert.True(await workflow.AddDrumVoiceAsync(Basis(), "Synth", SynthVoice.Algorithm));
        var sound = workflow.State.Snapshot.State.Sounds.Single();
        Assert.True(SynthVoice.Supports(sound));
        Assert.Equal(WorkspaceState.PianoId, new PatternEditorNavigation().Resolve(workflow.State).Pane);
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId,
            attachment.Sources.Single().NodeId);
        long generation = workflow.State.Generation;
        int undo = workflow.State.UndoCount;
        var next = sound.Parameters.SetItem("mix", .75m);
        Assert.True(await workflow.SetSynthParametersAsync(target, generation, sound.Parameters, next));
        Assert.Equal(undo + 1, workflow.State.UndoCount);
        Assert.True(await workflow.UndoAsync());
        Assert.False(await workflow.SetSynthParametersAsync(target, generation, sound.Parameters, next));
        Assert.Equal(sound.Parameters, workflow.State.Snapshot.State.Sounds.Single().Parameters);
        Assert.False(await workflow.SetSynthParametersAsync(target, workflow.State.Generation, sound.Parameters,
            next.Remove("attack")));
        Assert.Equal(sound.Parameters, workflow.State.Snapshot.State.Sounds.Single().Parameters);
    }

    [Fact]
    public async Task HeldPreviewUsesExistingBorrowerReleasesNaturallyAndStopJoinsWithoutCanonicalEdits()
    {
        Borrower? borrower = null;
        RealtimeSampler? sampler = null;
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), () =>
        {
            borrower = new();
            sampler = new(48000, 2, 128);
            return new(borrower, sampler, "Controlled synth output", () => null);
        });
        Assert.True(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
            "Synth"));
        Assert.True(await workflow.AddDrumVoiceAsync(BeatSteps.Resolve(workflow.State).Basis!, "Synth",
            SynthVoice.Algorithm));
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId,
            attachment.Sources.Single().NodeId);
        var before = workflow.State.Snapshot;
        int undo = workflow.State.UndoCount;
        Assert.True(await workflow.HoldSynthAsync(target, workflow.State.Generation, 60.5m, .8m));
        var packet = new float[256];
        Assert.True(sampler!.Process(packet));
        Assert.Equal(1, sampler.ActiveVoices);
        Assert.True(await workflow.ReleaseSamplerAsync());
        for (int i = 0; i < 100 && sampler.ReadStatus().Playing; i++) Assert.True(sampler.Process(packet));
        Assert.Equal(0, sampler.ActiveVoices);
        Assert.False(sampler.ReadStatus().Playing);
        Assert.True(await workflow.StopSamplerAsync());
        Assert.True(borrower!.Joined);
        Assert.Equal(before, workflow.State.Snapshot);
        Assert.Equal(undo, workflow.State.UndoCount);
    }

    [Fact]
    public async Task DraftCommitCancelHistoryPresetAndRepeatedPlayKeepOneVoiceAndOutput()
    {
        await using var f = new Fixture();
        await f.Create();
        var w = f.Workflow;
        Assert.True(await w.HoldSynthAsync(f.Target, w.State.Generation, 60, .8m));
        f.Tick();
        long epoch = f.Sampler.TransportEpoch, generation = w.State.Generation;
        var before = w.State.Snapshot;
        int undo = w.State.UndoCount;
        var draft = f.Sound.Parameters.SetItem("cutoff", 80);
        Assert.True(await w.PreviewSynthParametersAsync(f.Target, generation, draft));
        f.Tick();
        Assert.Equal(before, w.State.Snapshot);
        Assert.Equal(undo, w.State.UndoCount);
        Assert.True(await w.PreviewSynthParametersAsync(f.Target, generation, null));
        f.Tick();
        Assert.True(await w.SetSynthParametersAsync(f.Target, generation, f.Sound.Parameters, draft));
        f.Tick();
        Assert.Equal(undo + 1, w.State.UndoCount);
        Assert.False(await w.PreviewSynthParametersAsync(f.Target, generation, draft.SetItem("mix", 1)));
        Assert.True(await w.UndoAsync());
        f.Tick();
        Assert.Equal(before, w.State.Snapshot);
        Assert.True(await w.UndoAsync(redo: true));
        f.Tick();
        Assert.True(await w.SetSynthParametersAsync(f.Target, w.State.Generation, f.Sound.Parameters,
            SynthVoice.Presets[2].Values));
        f.Tick();
        Assert.True(await w.HoldSynthAsync(f.Target, w.State.Generation, 60, .8m));
        f.Tick();
        Assert.True(await w.HoldSynthAsync(f.Target, w.State.Generation, 67, .8m));
        f.Tick();
        Assert.Equal(epoch, f.Sampler.TransportEpoch);
        Assert.Equal(1, f.Sampler.ActiveVoices);
        Assert.Equal(1, f.Opens);
        Assert.Equal(1, f.Sampler.PreparedStatesCreated);
        Assert.False(f.Output.Joined);
        Assert.True(await w.ReleaseSamplerAsync());
        f.Tick();
        Assert.True(await w.HoldSynthAsync(f.Target, w.State.Generation, 62, .8m));
        f.Tick();
        Assert.Equal(1, f.Opens);
        Assert.Equal(1, f.Sampler.ActiveVoices);
        Assert.True(await w.StopSamplerAsync());
        Assert.True(f.Output.Joined);
        Assert.False(await w.PreviewSynthParametersAsync(f.Target, w.State.Generation, draft));
        Assert.True(await w.HoldSynthAsync(f.Target, w.State.Generation, 60, .8m));
        f.Tick();
        Assert.Equal(2, f.Opens);
        var retiredTarget = f.Target;
        Assert.True(await w.NewAsync(UnsavedDecision.Discard));
        Assert.True(f.Output.Joined);
        Assert.Null(w.SynthPreviewTarget);
        Assert.False(await w.PreviewSynthParametersAsync(retiredTarget, generation, draft));
    }

    [Fact]
    public async Task DeletedSourceJoinsPreviewAndUndoDoesNotResurrectIt()
    {
        await using var f = new Fixture();
        await f.Create();
        var w = f.Workflow;
        var target = f.Target;
        long generation = w.State.Generation;
        Assert.True(await w.HoldSynthAsync(target, generation, 60, .8m));
        f.Tick();
        Assert.True(await w.DeleteBeatChannelAsync(BeatSteps.Resolve(w.State).Basis!,
            w.State.Snapshot.State.Patterns.Single().Parts.Single().Id));
        Assert.True(f.Output.Joined);
        Assert.True(f.Sampler.IsTerminated);
        Assert.Null(w.SynthPreviewTarget);
        Assert.True(await w.UndoAsync());
        Assert.Null(w.SynthPreviewTarget);
        Assert.False(await w.PreviewSynthParametersAsync(target, generation, SynthVoice.Defaults));
    }

    [Fact]
    public async Task RealOutputFaultRemainsVisibleUntilSuccessfulExplicitSynthRetry()
    {
        await using var f = new Fixture();
        await f.Create();
        var w = f.Workflow;
        Assert.True(await w.HoldSynthAsync(f.Target, w.State.Generation, 60, .8m));
        f.Tick();
        var closedFault = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void Observed(WorkflowState state)
        {
            if (state.StatusKey == "Workflow.DeviceFault" && state.Device is null) closedFault.TrySetResult();
        }

        w.Changed += Observed;
        try
        {
            f.Failure = "Injected device invalidation (0x88890004)";
            await closedFault.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally
        {
            w.Changed -= Observed;
        }

        Assert.True(f.Output.Joined);
        Assert.Equal("Workflow.DeviceFault", w.State.StatusKey);
        Assert.Contains("0x88890004", w.State.Detail);
        f.Failure = null;
        Assert.True(await w.HoldSynthAsync(f.Target, w.State.Generation, 60, .8m));
        f.Tick();
        Assert.NotEqual("Workflow.DeviceFault", w.State.StatusKey);
        Assert.Null(w.State.Detail);
        Assert.Equal(2, f.Opens);
        Assert.Equal(1, f.Sampler.ActiveVoices);
    }
}