// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphEditorWorkflowTests
{
    private sealed class Output : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public bool Disposed { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() { Assert.True(Joined); Disposed = true; }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;
        internal RealtimeSampler? Sampler;
        internal Output? Borrower;
        internal Fixture(Func<CancellationToken, Task>? gate = null)
        {
            Workflow = new(ProjectDocument.Create(), () =>
            {
                Sampler = new(48000, 2, 32); Borrower = new();
                return new(Borrower, Sampler, "Deterministic output", () => null);
            }, Directory.File("owned"), (document, sampler, target, owner) =>
                new(document, sampler, target, owner, 8, 1, gate, true));
        }
        internal WorkflowState State => Workflow.State;
        internal GraphAttachment Attachment => State.Snapshot.State.GraphAttachments.Single(a => a.Id == State.Selected);
        internal GraphDefinition Graph => State.Snapshot.State.Graphs.Single(g => g.Id == Attachment.GraphId);
        internal GraphEditBasis Basis => new(State.Document.LifecycleId, State.Generation, Attachment.Id);
        internal async Task<string> Source(string name, float amplitude = 0.2f, int frames = 512)
        {
            string path = Directory.File(name + ".wav");
            await File.WriteAllBytesAsync(path, WavFixtures.Create(Enumerable.Repeat(amplitude, frames).ToArray()),
                TestContext.Current.CancellationToken); return path;
        }
        internal async Task Import(int frames = 512) => Assert.True(await Workflow.ImportAsync(await Source("Kick", frames: frames)));
        internal async Task<GraphNode> Add(string type, GraphPosition? position = null, string layout = GraphBuiltIns.Stereo, int inputs = 2)
        {
            Assert.True(await Workflow.AddGraphNodeAsync(Basis, type, position ?? new(200, 200), layout, inputs));
            return Graph.Nodes.Last(n => n.Type == type);
        }
        internal async Task Connect(GraphNode from, GraphNode to, int slot = 0) =>
            Assert.True(await Workflow.ConnectGraphAsync(Basis, from.Id, from.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id,
                to.Id, to.Ports.Where(p => p.Direction == GraphPortDirection.Input).ElementAt(slot).Id));
        internal async Task DriveUntil(Func<bool> predicate)
        {
            // Sequential consumer and explicit owner barriers, no timing sleeps or device initialization.
            for (int pass = 0; pass < 20000 && !predicate(); pass++)
            { await Workflow.ObserveAsync(); Sampler?.Process(new float[64]); await Task.Yield(); }
            Assert.True(predicate());
        }
        internal async Task Play()
        {
            var play = Workflow.PlayAsync(); await DriveUntil(() => play.IsCompleted);
            Assert.True(await play); Sampler!.Process(new float[64]); await Workflow.ObserveAsync();
        }
        public async ValueTask DisposeAsync() { await Workflow.DisposeAsync(); Directory.Dispose(); }
    }

    [Fact]
    public async Task CanvasMoveCommitsOneEditAndNoOpDoesNotPublish()
    {
        await using var f = new Fixture(); await f.Import(); var editor = new GraphEditorSession();
        editor.Open(f.State, f.Attachment.Id); var gain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        var before = f.State.Snapshot; int history = f.State.UndoCount;
        editor.BeginMove(gain.Id); for (int i = 0; i <= 200; i++) editor.UpdateMove(new(i, -i));
        Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.MoveGraphNodeAsync(editor.CompleteMove()!));
        Assert.Equal(history + 1, f.State.UndoCount); var after = f.State.Snapshot;
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.State.Snapshot);
        var current = f.Graph.Nodes.Single(n => n.Id == gain.Id).Position;
        Assert.False(await f.Workflow.MoveGraphNodeAsync(new(f.Basis, gain.Id, current, current)));
        Assert.Same(after, f.State.Snapshot); Assert.Equal(history + 1, f.State.UndoCount);
    }

    [Theory]
    [InlineData(0.515, false)]
    [InlineData(0.515, true)]
    [InlineData(0.5, false)]
    [InlineData(0.5, true)]
    public async Task CrossingSelectionDeletesOnlyIndicatedConnectionAndUndoRestoresIdsAndTopology(double t, bool firstSelected)
    {
        await using var f = new Fixture(); await f.Import();
        var source = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Source);
        var gain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        var mix = await f.Add(GraphBuiltIns.Mix, new(400, -22));
        var second = await f.Add(GraphBuiltIns.Source, new(0, 150));
        Assert.True(await f.Workflow.EditGraphAsync(f.Basis, "Crossing fixture", (edit, _, graph) =>
        {
            edit.MoveGraphNode(graph.Id, source.Id, new(0, 0));
            edit.MoveGraphNode(graph.Id, gain.Id, new(400, 150));
            GraphFixture.Connect(edit, graph.Id, second, mix, 1);
        }));
        source = f.Graph.Nodes.Single(n => n.Id == source.Id); gain = f.Graph.Nodes.Single(n => n.Id == gain.Id);
        var firstEdge = f.Graph.Connections.Single(c => c.FromNodeId == source.Id);
        var secondEdge = f.Graph.Connections.Single(c => c.FromNodeId == second.Id);
        var editor = new GraphEditorSession(); editor.Open(f.State, f.Attachment.Id);
        if (firstSelected) editor.Select(new(GraphHitKind.Connection, Connection: firstEdge.Id));
        using var shell = new ShellSession(new(f.Directory.File("prefs.json")), new(new(), false), f.State.Document)
            { HasWorkflowOwner = true };
        var canvas = new GraphCanvas(shell, editor, f.Workflow);
        var point = GraphGeometry.Bezier(GraphGeometry.Port(source, source.Ports[0]), GraphGeometry.Port(gain, gain.Ports[0]), t);
        editor.Select(GraphGeometry.HitTest(editor.Graph!, point, editor.Viewport.Zoom, selectedCable: editor.SelectedConnection));
        var expected = t == 0.5 && !firstSelected ? secondEdge : firstEdge;
        Assert.Equal(expected.Id, editor.SelectedConnection); Assert.Null(editor.SelectedNode);
        string indication = GraphConnectionLabels.Label(f.Graph, expected, n => n.Type, shell["Graph.In"], shell["Graph.Out"]);
        Assert.EndsWith(expected == firstEdge ? "core.gain · In 1" : "core.mix · In 2", indication);
        var before = f.State.Snapshot; var topology = f.Graph.Connections.ToArray(); int history = f.State.UndoCount;
        await canvas.DeleteSelectedAsync();
        Assert.Equal(history + 1, f.State.UndoCount);
        Assert.Equal(topology.Where(c => c.Id != expected.Id), f.Graph.Connections);
        Assert.Equal(before.State.Graphs.Single().Nodes, f.Graph.Nodes);
        string path = f.Directory.File("crossing.seqvium"); Assert.True(await f.Workflow.SaveAsync(path));
        await using (var stream = File.OpenRead(path))
        {
            var reopened = ProjectPersistence.Read(stream).Document;
            Assert.Equal(f.Graph.Connections.ToArray(), reopened.Current.State.Graphs.Single().Connections.ToArray());
            reopened.Close();
        }
        editor.Observe(f.State); Assert.Null(editor.SelectedConnection);
        Assert.True(await f.Workflow.UndoAsync()); editor.Observe(f.State);
        Assert.Same(before, f.State.Snapshot); Assert.Equal(topology, f.Graph.Connections);
    }

    [Fact]
    public async Task StaleMoveAndUndoRedoAbaCannotOverwriteNewerWork()
    {
        await using var f = new Fixture(); await f.Import();
        var gain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain); var basis = f.Basis;
        var proposal = new GraphMoveProposal(basis, gain.Id, gain.Position, new(900, 800));
        Assert.True(await f.Workflow.SetGraphGainAsync(f.Basis, gain.Id, 0.4m));
        Assert.True(await f.Workflow.UndoAsync()); var before = f.State.Snapshot;
        Assert.False(await f.Workflow.MoveGraphNodeAsync(proposal)); Assert.Same(before, f.State.Snapshot);
        Assert.Equal("Graph.Stale", f.State.Detail);
    }

    [Fact]
    public async Task CreateDeleteOutputAndSourceRebindingUseCanonicalIdentityAndUndo()
    {
        await using var f = new Fixture(); await f.Import();
        var source = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Source);
        var part = f.Attachment.Sources.Single().PartId; var before = f.State.Snapshot;
        Assert.True(await f.Workflow.DeleteGraphElementAsync(f.Basis, source.Id, null));
        Assert.Empty(f.Attachment.Sources); Assert.Single(f.State.Snapshot.State.Patterns.Single().Parts);
        var fresh = await f.Add(GraphBuiltIns.Source);
        Assert.NotEqual(source.Id, fresh.Id);
        Assert.True(await f.Workflow.BindGraphSourceAsync(f.Basis, fresh.Id, part));
        var output = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Output);
        Assert.False(await f.Workflow.AddGraphNodeAsync(f.Basis, GraphBuiltIns.Output, new(0, 0)));
        Assert.True(await f.Workflow.DeleteGraphElementAsync(f.Basis, output.Id, null)); Assert.Null(f.Attachment.OutputNodeId);
        var newOutput = await f.Add(GraphBuiltIns.Output); Assert.Equal(newOutput.Id, f.Attachment.OutputNodeId);
        for (int i = 0; i < 5; i++) Assert.True(await f.Workflow.UndoAsync());
        Assert.Same(before, f.State.Snapshot);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    public async Task MixCountAndOwningIntentAreAtomicAndReversible(int count)
    {
        await using var f = new Fixture(); await f.Import(); var before = f.State.Snapshot;
        var mix = await f.Add(GraphBuiltIns.Mix, inputs: count);
        Assert.Equal(count, mix.Ports.Count(p => p.Direction == GraphPortDirection.Input));
        Assert.True(f.State.Snapshot.State.Contexts.Single().IntentionalMix);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true));
        Assert.True(await f.Workflow.DeleteGraphElementAsync(f.Basis, mix.Id, null));
        Assert.False(f.State.Snapshot.State.Contexts.Single().IntentionalMix);
    }

    [Fact]
    public async Task IncompatibleAndOccupiedConnectionsPreserveSnapshotAndHistory()
    {
        await using var f = new Fixture(); await f.Import(); var source = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Source);
        var gain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain); var mono = await f.Add(GraphBuiltIns.Gain, layout: GraphBuiltIns.Mono);
        var before = f.State.Snapshot; int history = f.State.UndoCount;
        Assert.False(await f.Workflow.ConnectGraphAsync(f.Basis, source.Id, source.Ports[0].Id, mono.Id, mono.Ports[0].Id));
        Assert.Equal(GraphReasons.IncompatiblePorts, f.State.Detail);
        Assert.False(await f.Workflow.ConnectGraphAsync(f.Basis, source.Id, source.Ports[0].Id, gain.Id, gain.Ports[0].Id));
        Assert.Equal(GraphReasons.InputCardinality, f.State.Detail);
        Assert.Same(before, f.State.Snapshot); Assert.Equal(history, f.State.UndoCount);
    }

    [Fact]
    public async Task CycleIsSavableInvalidIntentAndDisconnectRepairsAutomaticallyEligibleState()
    {
        await using var f = new Fixture(); await f.Import(); var gain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        var sourceEdge = f.Graph.Connections.Single(c => c.ToNodeId == gain.Id);
        Assert.True(await f.Workflow.DeleteGraphElementAsync(f.Basis, null, sourceEdge.Id));
        Assert.True(await f.Workflow.ConnectGraphAsync(f.Basis, gain.Id, gain.Ports[1].Id, gain.Id, gain.Ports[0].Id));
        Assert.Contains(GraphDiagnostics.Inspect(f.State.Snapshot.State).Single().Diagnostics, d => d.Reason == GraphReasons.Cycle);
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("cycle.seqvium")));
        var cycle = f.Graph.Connections.Single(c => c.ToNodeId == gain.Id);
        Assert.True(await f.Workflow.DeleteGraphElementAsync(f.Basis, null, cycle.Id));
        await f.Connect(f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Source), gain);
        Assert.True(GraphDiagnostics.Inspect(f.State.Snapshot.State).Single().IsEligibleForPreparation);
    }

    [Fact]
    public async Task SecondWavCreatesIndependentDurablePartSourceAndOneUndoEntry()
    {
        await using var f = new Fixture(); await f.Import(); var before = f.State.Snapshot; int history = f.State.UndoCount;
        string snare = await f.Source("Snare", 0.3f, 1024);
        Assert.True(await f.Workflow.AddGraphWavAsync(f.Basis, snare, new(0, 200), TestContext.Current.CancellationToken));
        var after = f.State.Snapshot; Assert.Equal(history + 1, f.State.UndoCount);
        Assert.Equal(2, f.Attachment.Sources.Length); Assert.Equal(2, after.State.Resources.Length); Assert.Equal(2, after.State.Sounds.Length);
        Assert.Single(after.State.Placements); Assert.Single(after.State.Patterns);
        Assert.Equal(2, after.State.Patterns.Single().Parts.Length);
        Assert.Equal(2, f.Attachment.Sources.Select(s => s.PartId).Distinct().Count());
        Assert.Equal(2, f.Attachment.Sources.Select(s => s.NodeId).Distinct().Count());
        Assert.True(after.State.Patterns.Single().Length.Ticks > before.State.Patterns.Single().Length.Ticks);
        File.Delete(snare);
        using (var pcm = ProjectMedia.Decode(f.State.Document, after.State.Resources.Last().Id)) Assert.Equal(1024, pcm.Frames);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.State.Snapshot);
        Assert.False(GraphDiagnostics.Inspect(after.State).Single().IsEligibleForPreparation);
    }

    [Fact]
    public async Task SharedPatternSecondImportRefusesBeforeCreatingMediaOrMusic()
    {
        await using var f = new Fixture(); await f.Import();
        Assert.True(await f.Workflow.EditGraphAsync(f.Basis, "Shared placement", (edit, _, _) =>
            edit.AddPlacement(f.State.Snapshot.State.Patterns.Single().Id, new(100000))));
        var before = f.State.Snapshot; int files = System.IO.Directory.GetFiles(f.Directory.File("owned"), "*.wav", SearchOption.AllDirectories).Length;
        Assert.False(await f.Workflow.AddGraphWavAsync(f.Basis, await f.Source("Snare"), new(0, 200), TestContext.Current.CancellationToken));
        Assert.Equal("Graph.SharedPattern", f.State.Detail); Assert.Same(before, f.State.Snapshot);
        Assert.Equal(files, System.IO.Directory.GetFiles(f.Directory.File("owned"), "*.wav", SearchOption.AllDirectories).Length);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelOrCorruptSecondImportLeavesNoAcceptedPartialState(bool cancel)
    {
        await using var f = new Fixture(); await f.Import(); var before = f.State.Snapshot;
        string path = await f.Source("Candidate");
        using var cancellation = new CancellationTokenSource();
        if (cancel) cancellation.Cancel(); else await File.WriteAllBytesAsync(path, [1, 2, 3], TestContext.Current.CancellationToken);
        Assert.False(await f.Workflow.AddGraphWavAsync(f.Basis, path, new(0, 200), cancellation.Token));
        Assert.Same(before, f.State.Snapshot);
        Assert.Single(System.IO.Directory.GetFiles(f.Directory.File("owned"), "*.wav", SearchOption.AllDirectories));
    }

    private static async Task<(GraphNode KickGain, GraphNode SnareGain, GraphNode Master)> BuildTwoSources(Fixture f, int frames = 512)
    {
        await f.Import(frames); var kickGain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        var output = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Output);
        Assert.True(await f.Workflow.AddGraphWavAsync(f.Basis, await f.Source("Snare", 0.3f, frames * 2), new(0, 200)));
        var snare = f.Graph.Nodes.Last(n => n.Type == GraphBuiltIns.Source);
        var snareGain = await f.Add(GraphBuiltIns.Gain, new(180, 200)); var mix = await f.Add(GraphBuiltIns.Mix, new(390, 100));
        var master = await f.Add(GraphBuiltIns.Gain, new(580, 100));
        Assert.True(await f.Workflow.SetGraphGainAsync(f.Basis, snareGain.Id, 0.5m));
        Assert.True(await f.Workflow.SetGraphGainAsync(f.Basis, master.Id, 0.4m));
        var old = f.Graph.Connections.Single(c => c.FromNodeId == kickGain.Id);
        Assert.True(await f.Workflow.DeleteGraphElementAsync(f.Basis, null, old.Id));
        await f.Connect(kickGain, mix); await f.Connect(snare, snareGain); await f.Connect(snareGain, mix, 1);
        await f.Connect(mix, master); await f.Connect(master, output);
        Assert.True(GraphDiagnostics.Inspect(f.State.Snapshot.State).Single().IsEligibleForPreparation);
        return (kickGain, snareGain, master);
    }

    private static float[] Render(Fixture f)
    {
        using var plan = GraphPreparation.Prepare(f.State.Document, f.Attachment.Id, 48000, 2, 32);
        using var execution = plan.CreateExecution();
        var pcm = new float[checked((int)plan.EndFrame * 2)];
        for (int offset = 0; offset < pcm.Length; offset += 64)
            execution.Process(pcm.AsSpan(offset, Math.Min(64, pcm.Length - offset)));
        return pcm;
    }

    [Fact]
    public async Task RealKickSnareBranchesMatchIndependentPcmOracleAndNaturalEof()
    {
        await using var f = new Fixture(); var (kick, snare, _) = await BuildTwoSources(f);
        var pcm = Render(f);
        for (int frame = 0; frame < pcm.Length / 2; frame++)
        {
            float expected = ((frame < 512 ? 0.2f * 0.25f : 0) + (frame < 1024 ? 0.3f * 0.5f : 0)) * 0.4f;
            Assert.InRange(Math.Abs(pcm[frame * 2] - expected), 0, 2e-6f);
            Assert.InRange(Math.Abs(pcm[frame * 2 + 1] - expected), 0, 2e-6f);
        }
        Assert.True(await f.Workflow.SetGraphGainAsync(f.Basis, kick.Id, 0));
        var snareOnly = Render(f); Assert.InRange(Math.Abs(snareOnly[0] - 0.06f), 0, 2e-6f);
        Assert.True(await f.Workflow.SetGraphGainAsync(f.Basis, snare.Id, 0)); Assert.All(Render(f), v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task SaveReopenKeepsAllTopologyCoordinatesAndMediaAndGeometryDoesNotChangePcm()
    {
        await using var f = new Fixture(); await BuildTwoSources(f); var expected = Render(f);
        foreach (var node in f.Graph.Nodes)
            Assert.True(await f.Workflow.MoveGraphNodeAsync(new(f.Basis, node.Id, node.Position,
                new(-node.Position.X - 100, -node.Position.Y - 40))));
        Assert.Equal(expected, Render(f)); var before = f.Graph;
        string path = f.Directory.File("two.seqvium"); Assert.True(await f.Workflow.SaveAsync(path));
        Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard));
        Assert.Null(f.State.Execution); Assert.False(f.State.CanStop);
        Assert.Equal(before.Nodes.Select(n => (n.Id, n.Position)), f.Graph.Nodes.Select(n => (n.Id, n.Position)));
        Assert.Equal(before.Connections, f.Graph.Connections); Assert.Equal(expected, Render(f));
    }

    [Fact]
    public async Task GainOnlyLiveChangePreservesPreparedIdentityCursorAndStopPanicRelease()
    {
        await using var f = new Fixture(); await f.Import(480000); await f.Play();
        var gain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain); var before = f.Sampler!.ReadStatus();
        Assert.True(await f.Workflow.SetGraphGainAsync(f.Basis, gain.Id, 0.5m));
        await f.DriveUntil(() => f.Sampler.ReadStatus().ExecutingRevision == f.State.Snapshot.Revision);
        var after = f.Sampler.ReadStatus(); Assert.Equal(before.PreparedExecutionId, after.PreparedExecutionId);
        Assert.Equal(before.OriginPreparedRevision, after.OriginPreparedRevision); Assert.True(after.Playing);
        Assert.True(after.Position > before.Position); Assert.Equal(before.Epoch, after.Epoch);
        var pcm = new float[64]; f.Sampler.Process(pcm); Assert.All(pcm, value => Assert.InRange(Math.Abs(value - 0.1f), 0, 2e-6f));
        Assert.True(await f.Workflow.StopAsync()); f.Sampler.Process(pcm); Assert.False(f.Sampler.ReadStatus().Playing);
        Assert.True(await f.Workflow.StopAsync(true)); f.Sampler.Process(pcm);
        Assert.True(await f.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.True(f.Borrower!.Joined); Assert.True(f.Borrower.Disposed); Assert.Equal(0, f.Sampler.LivePreparedStates);
    }

    [Fact]
    public async Task InvalidLastValidSaveReopenAndRepairKeepCanonicalIntentAndConverge()
    {
        await using var f = new Fixture(); await f.Import(480000); await f.Play();
        var edge = f.Graph.Connections.Last(); var executing = f.Sampler!.ReadStatus().PreparedExecutionId;
        Assert.True(await f.Workflow.DeleteGraphElementAsync(f.Basis, null, edge.Id));
        f.Sampler.Process(new float[64]); await f.Workflow.ObserveAsync();
        Assert.True(f.State.Execution!.LastValidPlaying); Assert.True(f.State.Execution.InvalidCanonical);
        Assert.Equal(executing, f.Sampler.ReadStatus().PreparedExecutionId);
        string path = f.Directory.File("invalid.seqvium"); Assert.True(await f.Workflow.SaveAsync(path)); var invalid = f.State.Snapshot;
        var from = f.Graph.Nodes.Single(n => n.Id == edge.FromNodeId); var to = f.Graph.Nodes.Single(n => n.Id == edge.ToNodeId);
        await f.Connect(from, to);
        await f.DriveUntil(() => f.State.Execution?.InvalidCanonical == false && f.Sampler.ReadStatus().EquivalentCanonicalRevision == f.State.Snapshot.Revision);
        Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard));
        Assert.Equal(invalid.Revision, f.State.Snapshot.Revision); Assert.Null(f.State.Execution); Assert.False(f.State.CanPlay);
        await f.Connect(f.Graph.Nodes.Single(n => n.Id == from.Id), f.Graph.Nodes.Single(n => n.Id == to.Id));
        Assert.True(f.State.CanPlay); Assert.Null(f.State.Execution); await f.Play();
        Assert.False(f.State.Execution!.InvalidCanonical); Assert.False(f.State.Execution.LastValidPlaying);
    }

    [Fact]
    public async Task OldTargetCommandCannotMutateReopenedDocumentWithSameIds()
    {
        await using var f = new Fixture(); await f.Import(); var basis = f.Basis;
        string path = f.Directory.File("project.seqvium"); Assert.True(await f.Workflow.SaveAsync(path));
        Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard)); var before = f.State.Snapshot;
        Assert.False(await f.Workflow.AddGraphNodeAsync(basis, GraphBuiltIns.Gain, new(0, 500)));
        Assert.Same(before, f.State.Snapshot); Assert.Equal("Graph.Stale", f.State.Detail);
    }

    [Fact]
    public async Task FanOutAddsSignalPathsWithoutAnotherMusicalSourceOrVoice()
    {
        await using var f = new Fixture(); await f.Import();
        var source = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Source);
        var gain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        var output = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Output);
        var branch = await f.Add(GraphBuiltIns.Gain); var mix = await f.Add(GraphBuiltIns.Mix);
        Assert.True(await f.Workflow.SetGraphGainAsync(f.Basis, branch.Id, 0.5m));
        Assert.True(await f.Workflow.DeleteGraphElementAsync(f.Basis, null, f.Graph.Connections.Single(c => c.FromNodeId == gain.Id).Id));
        await f.Connect(source, branch); await f.Connect(gain, mix); await f.Connect(branch, mix, 1); await f.Connect(mix, output);
        using var plan = GraphPreparation.Prepare(f.State.Document, f.Attachment.Id, 48000, 2, 32,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(plan.Contributions); Assert.Single(plan.Events.Where(e => e.Kind == ExecutionEventKind.NoteOn));
        using var execution = plan.CreateExecution(); var packet = new float[64]; execution.Process(packet);
        Assert.All(packet, value => Assert.InRange(Math.Abs(value - 0.15f), 0, 2e-6f));
        Assert.Single(f.Attachment.Sources); Assert.Single(f.State.Snapshot.State.Patterns.Single().Parts);
    }

    [Fact]
    public async Task PendingPreparationIsJoinedBeforeReplacementAndFreshDocumentHasNoExecution()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var never = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool exited = false;
        async Task Gate(CancellationToken token)
        {
            entered.TrySetResult();
            try { await never.Task.WaitAsync(token); } finally { exited = true; }
        }
        await using var f = new Fixture(Gate); await f.Import();
        string path = f.Directory.File("replacement.seqvium"); Assert.True(await f.Workflow.SaveAsync(path));
        var old = f.State.Document; var play = f.Workflow.PlayAsync();
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(await f.Workflow.StopAsync());
        var open = f.Workflow.OpenAsync(path, UnsavedDecision.Discard);
        Assert.False(await play.WaitAsync(TestContext.Current.CancellationToken));
        Assert.True(await open.WaitAsync(TestContext.Current.CancellationToken));
        Assert.True(exited); Assert.True(old.IsClosed); Assert.NotSame(old, f.State.Document);
        Assert.True(f.Borrower!.Joined); Assert.True(f.Borrower.Disposed); Assert.Null(f.State.Execution);
        Assert.Equal(0, f.Sampler!.LivePreparedStates); Assert.Equal(0, f.Sampler.ReservedPcmAndScratchBytes);
    }

    [Fact]
    public async Task SavedInvalidAmplitudeIsObservedExactlyAndCanBeRepairedWithUndo()
    {
        await using var f = new Fixture(); await f.Import(); var gain = f.Graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        Assert.True(await f.Workflow.EditGraphAsync(f.Basis, "Invalid imported numeric intent", (edit, attachment, _) =>
            edit.SetGraphGain(attachment.GraphId, gain.Id, 2m)));
        string path = f.Directory.File("invalid-gain.seqvium"); Assert.True(await f.Workflow.SaveAsync(path));
        Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard));
        Assert.Equal(2m, f.State.Gain); Assert.False(f.State.CanPlay);
        Assert.True(await f.Workflow.SetGraphGainAsync(f.Basis, gain.Id, 0.35m)); Assert.True(f.State.CanPlay);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Equal(2m, f.State.Gain); Assert.False(f.State.CanPlay);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Equal(0.35m, f.State.Gain);
    }

    [Fact]
    public void PatternLengthEditRejectsTruncationAndPlacementOverflowAtomically()
    {
        var f = new GraphFixture(false); var before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Truncate active note", edit => edit.SetPatternLength(f.Pattern, new(1))));
        Assert.Same(before, f.Document.Current);
        f.Document.Edit("Boundary placement", edit => edit.MovePlacement(f.OtherPlacement, new(long.MaxValue - MusicalPosition.TicksPerQuarter)));
        before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Overflow placement end", edit =>
            edit.SetPatternLength(f.Pattern, new(2 * MusicalPosition.TicksPerQuarter))));
        Assert.Same(before, f.Document.Current);
    }
}
