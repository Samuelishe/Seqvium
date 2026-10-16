// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class RackWorkflowTests
{
    private static async Task StartGainFixture(Fixture f, bool mixer, bool song, bool effects = false)
    {
        await f.Create(SynthVoice.Algorithm);
        var part = f.Pattern.Parts.Single();
        Assert.True(await f.Edit(e => e.SetPartNotes(f.Pattern.Id, part.Id,
            [new(Id<NoteEvent>.New(), new(0), new(2880000), 60, .2m)])));
        if (mixer) Assert.True(await f.Edit(e => e.InitializeMixer()));
        if (effects)
            Assert.True(await f.Edit(e => e.SetMixerStrip(null, new()
            {
                Delay = new() { Enabled = true, TimeMs = 10, Mix = .6m },
                Reverb = new() { Enabled = true, DecaySeconds = .3m, Mix = .3m }
            })));
        if (song)
        {
            Assert.True(await f.Edit(e => e.AddArrangementClip(f.State.Snapshot.State.Placements.Single().Id,
                new(0), 0, 2)));
            Assert.True(await f.Workflow.SetSongModeAsync(true));
        }

        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("draft.seqvium")));
        var play = f.Workflow.PlayAsync();
        for (int i = 0; i < 10000 && !play.IsCompleted; i++)
        {
            f.Sampler?.Process(Span<float>.Empty);
            await f.Workflow.ObserveAsync();
            await Task.Yield();
        }

        Assert.True(await play.WaitAsync(TestContext.Current.CancellationToken), f.State.Detail);
    }

    [Theory]
    [InlineData(44100, false, false)]
    [InlineData(48000, false, false)]
    [InlineData(44100, true, false)]
    [InlineData(48000, true, false)]
    [InlineData(44100, false, true)]
    [InlineData(48000, false, true)]
    [InlineData(44100, true, true)]
    [InlineData(48000, true, true)]
    public async Task GainDraftChangesPreparedPcmBeforeCommitAndCancelCommitHistoryRetainExecution(
        int rate, bool mixer, bool song)
    {
        await using var f = new Fixture(rate);
        await StartGainFixture(f, mixer, song);
        var sampler = f.Sampler!;
        using var plan = song
            ? ArrangementPreparation.Prepare(f.State.Document, rate, 2, 127, TestContext.Current.CancellationToken)
            : GraphPreparation.Prepare(f.State.Document, f.State.Selected!.Value, rate, 2, 127, voiceCapacity: 32,
                cancellationToken: TestContext.Current.CancellationToken);
        using var oracle = plan.CreateExecution();
        var actual = new float[254];
        var expected = new float[254];
        sampler.Process(actual);
        oracle.Process(expected);
        var initialPcm = expected.ToArray();
        var initial = sampler.ReadStatus();
        var snapshot = f.State.Snapshot;
        int history = f.State.UndoCount, preparations = f.State.Execution!.Preparations;
        var basis = new GraphEditBasis(f.State.Document.LifecycleId, f.State.Generation, f.State.Selected!.Value);
        var node = snapshot.State.Graphs.Single().Nodes.Single(n => n.Type == GraphBuiltIns.Gain).Id;
        var busy = new List<bool>();
        f.Workflow.Changed += state => busy.Add(state.Busy);

        void CheckPcm(decimal gain)
        {
            long position = sampler.RenderPosition;
            Assert.True(sampler.Process(actual));
            oracle.Process(expected);
            Assert.Contains(expected, v => v != 0);
            for (int i = 0; i < actual.Length; i++)
                Assert.InRange(Math.Abs(actual[i] - expected[i] * (float)(gain / .25m)), 0, 1e-7f);
            var status = sampler.ReadStatus();
            Assert.Equal(initial.Epoch, status.Epoch);
            Assert.Equal(initial.PreparedExecutionId, status.PreparedExecutionId);
            Assert.Equal(position + 127, status.Position);
            Assert.Equal(initial.Voices, status.Voices);
        }

        Guid id = Guid.NewGuid();
        Assert.True(await f.Workflow.BeginGainPreviewAsync(id, basis, node));
        foreach (decimal gain in new[] { .1m, .7955m, 0m, 1m, .1m })
        {
            Assert.True(await f.Workflow.PreviewGainAsync(id, gain));
            CheckPcm(gain);
            Assert.Same(snapshot, f.State.Snapshot);
            Assert.Equal(history, f.State.UndoCount);
        }

        // Save and independent frozen preparation can only see accepted intent.
        string path = f.Directory.File("draft.seqvium");
        Assert.True(await f.Workflow.SaveAsync(path));
        var loaded = ProjectPersistence.Open(path).Document;
        Assert.Equal(.25m, loaded.Current.State.Graphs.Single().Nodes.Single(n => n.Id == node)
            .Parameters[GraphBuiltIns.GainAmplitude].GetDecimal());
        loaded.Close();
        using (var frozen = song
                   ? ArrangementPreparation.Prepare(f.State.Document, rate, 2, 127,
                       TestContext.Current.CancellationToken)
                   : GraphPreparation.Prepare(f.State.Document, basis.Attachment, rate, 2, 127,
                       cancellationToken: TestContext.Current.CancellationToken))
        {
            using var renderer = frozen.CreateExecution();
            var canonicalPcm = new float[254];
            renderer.Process(canonicalPcm);
            Assert.Equal(initialPcm, canonicalPcm);
        }

        Assert.True(await f.Workflow.CancelGainPreviewAsync(id));
        CheckPcm(.25m);
        Assert.False(await f.Workflow.CompleteGainPreviewAsync(id, .1m));
        Assert.Equal(history, f.State.UndoCount);
        busy.Clear();

        id = Guid.NewGuid();
        Assert.True(await f.Workflow.BeginGainPreviewAsync(id, basis, node));
        Assert.True(await f.Workflow.PreviewGainAsync(id, .7955m));
        CheckPcm(.7955m);
        Assert.True(await f.Workflow.CompleteGainPreviewAsync(id, .7955m));
        CheckPcm(.7955m);
        Assert.Equal(history + 1, f.State.UndoCount);
        Assert.DoesNotContain(true, busy);
        Assert.True(await f.Workflow.UndoAsync());
        CheckPcm(.25m);
        Assert.True(await f.Workflow.UndoAsync(true));
        CheckPcm(.7955m);
        await f.Workflow.ObserveAsync();
        Assert.Equal(preparations, f.State.Execution!.Preparations);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("aba")]
    [InlineData("stop")]
    [InlineData("panic")]
    [InlineData("new")]
    public async Task PendingGainDraftCannotReturnAfterAuthorityWithdrawal(string action)
    {
        await using var f = new Fixture(48000);
        await StartGainFixture(f, true, false);
        var sampler = f.Sampler!;
        sampler.Process(new float[254]);
        var basis = new GraphEditBasis(f.State.Document.LifecycleId, f.State.Generation, f.State.Selected!.Value);
        var node = f.State.Snapshot.State.Graphs.Single().Nodes.Single(n => n.Type == GraphBuiltIns.Gain).Id;
        Guid id = Guid.NewGuid();
        Assert.True(await f.Workflow.BeginGainPreviewAsync(id, basis, node));
        Assert.True(await f.Workflow.PreviewGainAsync(id, .9m)); // deliberately unconsumed
        int history = f.State.UndoCount;
        if (action == "cancel") Assert.True(await f.Workflow.CancelGainPreviewAsync(id));
        if (action == "aba")
        {
            Assert.True(await f.Workflow.SetGainAsync(.4m));
            Assert.True(await f.Workflow.UndoAsync());
        }

        if (action is "stop" or "panic") Assert.True(await f.Workflow.StopAsync(action == "panic"));
        if (action == "new") Assert.True(await f.Workflow.NewAsync(UnsavedDecision.Discard));
        Assert.False(await f.Workflow.PreviewGainAsync(id, .7m));
        Assert.False(await f.Workflow.CompleteGainPreviewAsync(id, .7m));
        if (action == "new") return;
        Assert.Equal(history, f.State.UndoCount);
        var actual = new float[254];
        sampler.Process(actual);
        if (action is "stop" or "panic") Assert.All(actual, v => Assert.Equal(0, v));
        else
        {
            using var plan = GraphPreparation.Prepare(f.State.Document, basis.Attachment, 48000, 2, 127,
                cancellationToken: TestContext.Current.CancellationToken);
            using var oracle = plan.CreateExecution();
            var expected = new float[254];
            oracle.Process(expected);
            oracle.Process(expected);
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GainDraftIsLatestWinsWithNoCallbackAllocationsAndOldCancelCannotWithdrawNewGesture(bool mixer)
    {
        await using var f = new Fixture(48000);
        await StartGainFixture(f, mixer, false);
        var sampler = f.Sampler!;
        var packet = new float[254];
        sampler.Process(packet);
        var basis = new GraphEditBasis(f.State.Document.LifecycleId, f.State.Generation, f.State.Selected!.Value);
        var node = f.State.Snapshot.State.Graphs.Single().Nodes.Single(n => n.Type == GraphBuiltIns.Gain).Id;
        var old = Guid.NewGuid();
        Assert.True(await f.Workflow.BeginGainPreviewAsync(old, basis, node));
        Assert.True(await f.Workflow.PreviewGainAsync(old, 0));
        var next = Guid.NewGuid();
        Assert.True(await f.Workflow.BeginGainPreviewAsync(next, basis, node));
        Assert.False(await f.Workflow.CancelGainPreviewAsync(old));
        Assert.False(await f.Workflow.PreviewGainAsync(old, 0));
        Assert.True(await f.Workflow.PreviewGainAsync(next, 1));
        Assert.True(await f.Workflow.PreviewGainAsync(next, 0));
        sampler.Process(packet);
        Assert.All(packet, v => Assert.Equal(0, v));
        for (int i = 0; i < 100; i++) sampler.Process(packet);
        Assert.True(await f.Workflow.PreviewGainAsync(next, .5m));
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) sampler.Process(packet);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Contains(packet, v => v != 0);
        Assert.True(await f.Workflow.CancelGainPreviewAsync(next));
        before = GC.GetAllocatedBytesForCurrentThread();
        sampler.Process(packet);
        allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.False(await f.Workflow.CompleteGainPreviewAsync(next, .5m));
    }

    [Fact]
    public async Task GainDraftAndCancelPreserveDelayReverbHistoriesAgainstCanonicalPreparedOracle()
    {
        await using var f = new Fixture(48000);
        await StartGainFixture(f, true, true, effects: true);
        var sampler = f.Sampler!;
        var basis = new GraphEditBasis(f.State.Document.LifecycleId, f.State.Generation, f.State.Selected!.Value);
        var graph = f.State.Snapshot.State.Graphs.Single();
        var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        using var baseline = ArrangementPreparation.Prepare(f.State.Document, 48000, 2, 127,
            TestContext.Current.CancellationToken);
        using var oracle = baseline.CreateExecution();
        var actual = new float[254];
        var expected = new float[254];

        void Compare(int packets)
        {
            for (int p = 0; p < packets; p++)
            {
                sampler.Process(actual);
                oracle.Process(expected);
                Assert.Equal(expected, actual);
            }
        }

        Compare(16);
        var initial = sampler.ReadStatus();
        Guid id = Guid.NewGuid();
        Assert.True(await f.Workflow.BeginGainPreviewAsync(id, basis, gain.Id));
        foreach (decimal amplitude in new[] { .8m, 0m, .4m })
        {
            var fork = ProjectDocument.Reopen(f.State.Snapshot);
            fork.Edit("Independent accepted oracle", e => e.SetGraphGain(graph.Id, gain.Id, amplitude));
            using var accepted = ArrangementPreparation.Prepare(fork, 48000, 2, 127,
                TestContext.Current.CancellationToken);
            oracle.UpdateCoefficients(accepted.Graph!.Coefficients);
            oracle.UpdateMixer(accepted.Graph.Mixer!);
            Assert.True(await f.Workflow.PreviewGainAsync(id, amplitude));
            Compare(8);
            // Zero local input still leaves existing delay/reverb output, rather than clearing FX.
            Assert.Contains(actual, v => v != 0);
            fork.Close();
        }

        oracle.UpdateCoefficients(baseline.Graph!.Coefficients);
        oracle.UpdateMixer(baseline.Graph.Mixer!);
        Assert.True(await f.Workflow.CancelGainPreviewAsync(id));
        Compare(8);
        Assert.Equal(initial.Epoch, sampler.TransportEpoch);
        Assert.Equal(initial.PreparedExecutionId, sampler.ReadStatus().PreparedExecutionId);
        Assert.Equal(initial.Voices, sampler.ActiveVoices);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnconsumedAcceptedGainSurvivesFollowingGeometryAndMixerPublication(bool song)
    {
        await using var f = new Fixture(48000);
        await StartGainFixture(f, true, song);
        using var baseline = song
            ? ArrangementPreparation.Prepare(f.State.Document, 48000, 2, 127, TestContext.Current.CancellationToken)
            : GraphPreparation.Prepare(f.State.Document, f.State.Selected!.Value, 48000, 2, 127,
                cancellationToken: TestContext.Current.CancellationToken);
        using var oracle = baseline.CreateExecution();
        var actual = new float[254];
        var expected = new float[254];
        f.Sampler!.Process(actual);
        oracle.Process(expected);
        long identity = f.Sampler.ReadStatus().PreparedExecutionId;
        Assert.True(await f.Workflow.SetGainAsync(.8m));
        var graph = f.State.Snapshot.State.Graphs.Single();
        Assert.True(await f.Edit(e => e.MoveGraphNode(graph.Id, graph.Nodes[0].Id, new(800, 900))));
        f.Sampler.Process(actual);
        oracle.Process(expected);
        for (int i = 0; i < actual.Length; i++) Assert.InRange(Math.Abs(expected[i] * 3.2f - actual[i]), 0, 1e-7);
        Assert.Equal(identity, f.Sampler.ReadStatus().PreparedExecutionId);
    }

    [Fact]
    public async Task StoppedGainAndSelectionAbaCannotAcceptAnEarlierGesture()
    {
        await using var f = new Fixture();
        await f.Create(SynthVoice.Algorithm);
        var first = f.State.Selected!.Value;
        Assert.True(await f.Workflow.CreateBeatAsync(f.State.Document.LifecycleId, f.State.Generation, "Other"));
        var other = f.State.Selected!.Value;
        Assert.True(await f.Workflow.SelectAsync(first));
        var basis = new GraphEditBasis(f.State.Document.LifecycleId, f.State.Generation, first);
        var graph = f.State.Snapshot.State.Graphs.Single(g => g.Id == f.State.Snapshot.State.GraphAttachments
            .Single(a => a.Id == first).GraphId);
        var node = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain).Id;
        Guid old = Guid.NewGuid();
        Assert.True(await f.Workflow.BeginGainPreviewAsync(old, basis, node));
        Assert.False(await f.Workflow.PreviewGainAsync(old, .9m)); // no device/preparation created by a stopped edit
        Assert.True(await f.Workflow.SelectAsync(other));
        Assert.True(await f.Workflow.SelectAsync(first));
        Assert.Equal(basis.Generation, f.State.Generation);
        Assert.False(await f.Workflow.CompleteGainPreviewAsync(old, .9m));
        Guid next = Guid.NewGuid();
        int history = f.State.UndoCount;
        Assert.True(await f.Workflow.BeginGainPreviewAsync(next, basis, node));
        Assert.False(await f.Workflow.CompleteGainPreviewAsync(next, .25m));
        Assert.Equal(history, f.State.UndoCount);
        next = Guid.NewGuid();
        Assert.True(await f.Workflow.BeginGainPreviewAsync(next, basis, node));
        Assert.True(await f.Workflow.CompleteGainPreviewAsync(next, .7955m));
        Assert.Equal(.7955m, f.State.Gain);
        Assert.Null(f.Sampler);
    }
}
