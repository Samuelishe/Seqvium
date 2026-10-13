// SPDX-License-Identifier: Apache-2.0
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class NodeEditorTests
{
    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static WorkflowState Observe(ProjectDocument document, bool busy = false) =>
        new(document, [], null, null, "", "", "Workflow.Ready", null, null, busy, false, false, null);
    private static NodeEditorTarget Target(GraphFixture f, GraphNode? node = null) =>
        new(f.Document.LifecycleId, f.Attachment.Id, f.Graph.Id, (node ?? f.Gain).Id);

    [Fact]
    public void ExplicitTargetSurvivesSelectionAndImmutableEditsWithoutFollowingPlayback()
    {
        var f = new GraphFixture(); using var editor = new NodeEditorSession();
        var before = f.Document.Current; int history = f.Document.UndoCount;
        Assert.True(editor.Open(Observe(f.Document), Target(f)));
        Assert.Same(before, f.Document.Current); Assert.Equal(history, f.Document.UndoCount);
        Assert.True(editor.BeginDraft()); editor.UpdateDraft(0.3m);
        Assert.True(editor.Open(Observe(f.Document), Target(f))); Assert.True(editor.HasDraft);
        editor.Observe(Observe(f.Document) with { Selected = Id<GraphAttachment>.New() });
        Assert.Equal(Target(f), editor.Target); Assert.True(editor.HasDraft);
        Assert.True(editor.Open(Observe(f.Document), Target(f, f.Source)));
        Assert.False(editor.HasDraft); Assert.False(editor.CanEditGain);
        Assert.Equal(f.Source.Id, editor.Node!.Id);
    }

    [Theory]
    [InlineData("node")]
    [InlineData("graph")]
    [InlineData("placement")]
    public void DeletionRetainsExactIdentityUndoRestoresOnlyCanonicalState(string deletion)
    {
        var f = new GraphFixture(false); using var editor = new NodeEditorSession();
        var key = Target(f); editor.Open(Observe(f.Document), key); editor.BeginDraft(); editor.UpdateDraft(0.2m);
        f.Document.Edit("Delete", edit =>
        {
            if (deletion == "node") edit.DeleteGraphNode(f.Graph.Id, f.Gain.Id);
            else if (deletion == "graph") edit.DeleteGraph(f.Graph.Id);
            else edit.DeletePlacement(f.Placement);
        });
        editor.Observe(Observe(f.Document)); Assert.Equal(key, editor.Target);
        Assert.Null(editor.Node); Assert.False(editor.HasDraft); Assert.False(editor.CanEditGain);
        Assert.True(f.Document.Undo()); editor.Observe(Observe(f.Document));
        Assert.Equal(key, editor.Target); Assert.Equal(1m, editor.Amplitude); Assert.True(editor.CanEditGain);
        Assert.Null(editor.CompleteDraft());
        Assert.True(f.Document.Redo()); editor.Observe(Observe(f.Document)); Assert.Null(editor.Node);
    }

    [Fact]
    public void AnyGenerationChangeCancelsDraftIncludingUndoRedoABAAndExternalEdits()
    {
        var f = new GraphFixture(false); using var editor = new NodeEditorSession();
        editor.Open(Observe(f.Document), Target(f)); editor.BeginDraft(); editor.UpdateDraft(0.2m);
        var original = f.Document.Current;
        f.Document.Edit("Unrelated geometry", edit => edit.MoveGraphNode(f.Graph.Id, f.Source.Id, new(40, 50)));
        Assert.True(f.Document.Undo()); Assert.Same(original, f.Document.Current);
        editor.Observe(Observe(f.Document)); Assert.True(editor.ChangedElsewhere);
        Assert.False(editor.HasDraft); Assert.Null(editor.CompleteDraft()); Assert.Equal(Target(f), editor.Target);
        f.Document.Edit("External Gain", edit => edit.SetGraphGain(f.Graph.Id, f.Gain.Id, 0.75m));
        editor.Observe(Observe(f.Document)); Assert.Equal(0.75m, editor.Amplitude);
    }

    [Fact]
    public void NewLifecycleWithIdenticalSavedIdsWithdrawsOldAuthorityAndDisposeIgnoresLateObservation()
    {
        var f = new GraphFixture(false); var editor = new NodeEditorSession();
        editor.Open(Observe(f.Document), Target(f)); editor.BeginDraft(); editor.UpdateDraft(0.3m);
        var loaded = GraphFixture.Reopen(f.Document).Document;
        editor.Observe(Observe(loaded)); Assert.Null(editor.Target); Assert.False(editor.HasDraft);
        Assert.False(editor.Open(Observe(loaded), Target(f)));
        var fresh = Target(f) with { LifecycleId = loaded.LifecycleId };
        Assert.True(editor.Open(Observe(loaded), fresh));
        editor.Dispose(); Assert.False(editor.Observe(Observe(loaded))); Assert.Null(editor.State);
        Assert.False(editor.Open(Observe(loaded), fresh)); loaded.Close();
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, 0.0)]
    [InlineData(0.5, -6.020599913279624)]
    [InlineData(2, null)]
    [InlineData(-1, null)]
    public void DerivedDecibelsHaveIndependentNumericalOracle(double amplitude, double? expected)
    {
        var actual = NodeEditorSession.Decibels((decimal)amplitude);
        if (expected is null) Assert.Null(actual);
        else Assert.Equal(expected.Value, actual!.Value, 10);
    }

    [Fact]
    public void InvalidSavedValueRemainsVisibleAndOnlyExplicitLegalDraftRepairsIt()
    {
        var f = new GraphFixture(false); f.Document.Edit("Invalid imported intent", e => e.SetGraphGain(f.Graph.Id, f.Gain.Id, 2m));
        using var editor = new NodeEditorSession(); editor.Open(Observe(f.Document), Target(f));
        Assert.Equal(2m, editor.Amplitude); Assert.True(editor.CanEditGain);
        editor.BeginDraft(); editor.UpdateDraft(1m);
        Assert.Equal(2m, editor.Amplitude); Assert.Equal(2m, editor.Draft!.Before);
        Assert.Equal(1m, editor.CompleteDraft()!.After); Assert.Equal(2m, editor.Amplitude);
    }

    [Theory]
    [InlineData("type")]
    [InlineData("version")]
    [InlineData("opaque")]
    [InlineData("parameters")]
    public void UnsupportedCapabilityCannotBeInterpretedAsEditableGain(string variant)
    {
        var f = new GraphFixture(false);
        var node = f.Gain with { Id = Id<GraphNode>.New() };
        node = variant switch
        {
            "type" => node with { Type = "unavailable.effect" },
            "version" => node with { StateVersion = 2 },
            "opaque" => node with { Extension = new("example", 1, JsonSerializer.SerializeToElement(new { secret = 1 })) },
            _ => node with { Parameters = node.Parameters.Add("unknown", JsonSerializer.SerializeToElement(0.2m)) }
        };
        // New port identities are required for canonical structural ownership.
        node = node with { Ports = [.. node.Ports.Select(p => p with { Id = Id<GraphPort>.New() })] };
        f.Document.Edit("Unsupported node", e => e.AddGraphNode(f.Graph.Id, node));
        using var editor = new NodeEditorSession(); Assert.True(editor.Open(Observe(f.Document), Target(f, node)));
        Assert.NotNull(editor.Node); Assert.False(editor.CanEditGain); Assert.False(editor.BeginDraft());
    }

    [Fact]
    public void WrongGraphOrAttachmentNeverResolvesByNodeIdAlone()
    {
        var f = new GraphFixture(false); using var editor = new NodeEditorSession();
        Assert.False(editor.Open(Observe(f.Document), Target(f) with { GraphDefinitionId = Id<GraphDefinition>.New() }));
        Assert.False(editor.Open(Observe(f.Document), Target(f) with { AttachmentId = Id<GraphAttachment>.New() }));
        Assert.False(editor.Open(Observe(f.Document, busy: true), Target(f)));
    }

    [Fact]
    public async Task FiveHundredPreviewUpdatesCommitOnceThroughOwnerAndUndoRedoRefreshQuickGain()
    {
        var f = new GraphFixture(false); int openedAudio = 0;
        await using var workflow = new DesktopWorkflow(f.Document, () => { openedAudio++; throw new InvalidOperationException(); });
        await workflow.SelectAsync(f.Attachment.Id);
        using var editor = new NodeEditorSession(); editor.Open(workflow.State, Target(f));
        var before = workflow.State.Snapshot; int history = workflow.State.UndoCount;
        editor.BeginDraft();
        for (int i = 1; i <= 500; i++) editor.UpdateDraft(i / 1000m);
        Assert.Same(before, workflow.State.Snapshot); Assert.Equal(history, workflow.State.UndoCount);
        var proposal = Assert.IsType<NodeGainProposal>(editor.CompleteDraft());
        Assert.True(await workflow.SetGraphGainAsync(proposal)); editor.Observe(workflow.State);
        Assert.Equal(history + 1, workflow.State.UndoCount); Assert.Equal(0.5m, workflow.State.Gain);
        Assert.Equal(0.5m, editor.Amplitude); Assert.Equal(0, openedAudio);
        Assert.True(await workflow.UndoAsync()); editor.Observe(workflow.State);
        Assert.Same(before, workflow.State.Snapshot); Assert.Equal(1m, editor.Amplitude);
        Assert.True(await workflow.UndoAsync(true)); editor.Observe(workflow.State); Assert.Equal(0.5m, editor.Amplitude);
        editor.BeginDraft(); editor.UpdateDraft(0.25m); var stale = editor.CompleteDraft()!;
        Assert.True(await workflow.UndoAsync()); Assert.True(await workflow.UndoAsync(true));
        before = workflow.State.Snapshot; history = workflow.State.UndoCount;
        Assert.False(await workflow.SetGraphGainAsync(stale));
        Assert.Same(before, workflow.State.Snapshot); Assert.Equal(history, workflow.State.UndoCount);
        Assert.False(await workflow.SetGraphGainAsync(stale with { Generation = workflow.State.Generation,
            Target = stale.Target with { GraphDefinitionId = Id<GraphDefinition>.New() } }));
        Assert.Equal(0, openedAudio);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelCaptureLossAndNoOpCannotProduceACommand(bool cancel)
    {
        var f = new GraphFixture(false); using var editor = new NodeEditorSession(); editor.Open(Observe(f.Document), Target(f));
        editor.BeginDraft(); editor.UpdateDraft(cancel ? 0.4m : 1m);
        if (cancel) editor.CancelDraft();
        Assert.Null(editor.CompleteDraft()); Assert.Equal(1m, editor.Amplitude);
    }

    [Fact]
    public async Task CanonicalGainSurvivesSaveReopenAndSourceObservationValidatesStoredWavWithoutAudio()
    {
        using var dir = new TemporaryDirectory(); string wav = dir.File("signal.wav");
        await File.WriteAllBytesAsync(wav, WavFixtures.Create(Enumerable.Repeat(0.2f, 480).ToArray()), TestContext.Current.CancellationToken);
        int audio = 0;
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), () =>
            { audio++; throw new InvalidOperationException(); }, dir.File("owned"));
        Assert.True(await workflow.ImportAsync(wav, TestContext.Current.CancellationToken));
        var state = workflow.State; var attachment = state.Snapshot.State.GraphAttachments.Single();
        var graph = state.Snapshot.State.Graphs.Single(); var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        var key = new NodeEditorTarget(state.Document.LifecycleId, attachment.Id, graph.Id, gain.Id);
        using var editor = new NodeEditorSession(); editor.Open(state, key); editor.BeginDraft(); editor.UpdateDraft(0.375m);
        Assert.True(await workflow.SetGraphGainAsync(editor.CompleteDraft()!));
        string path = dir.File("project.seqvium"); Assert.True(await workflow.SaveAsync(path));
        var sources = await workflow.InspectNodeSourcesAsync(key with { NodeId = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Source).Id },
            workflow.State.Generation, TestContext.Current.CancellationToken);
        Assert.Equal(SourceAvailability.Supported, Assert.Single(sources!.Sources).Availability);
        Assert.Equal(48000, sources.Sources[0].Rate); Assert.Equal(480, sources.Sources[0].Frames);
        Assert.True(await workflow.OpenAsync(path, UnsavedDecision.Discard)); editor.Observe(workflow.State);
        Assert.Null(editor.Target); Assert.Equal(0.375m, workflow.State.Gain); Assert.Null(workflow.State.Execution);
        Assert.Equal(0, audio);
    }

    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public bool Disposed { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() { Assert.True(Joined); Disposed = true; }
    }

    [Fact]
    public async Task NodeCommitUsesExistingF2CoefficientConvergencePreservesExecutionAndStickyStop()
    {
        using var dir = new TemporaryDirectory(); string path = dir.File("signal.wav");
        await File.WriteAllBytesAsync(path, WavFixtures.Create(Enumerable.Repeat(0.2f, 48000).ToArray()), TestContext.Current.CancellationToken);
        RealtimeSampler? sampler = null; var output = new Borrower();
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), () =>
        {
            sampler = new(48000, 2, 32); return new(output, sampler, "Deterministic borrower", () => null);
        }, dir.File("owned"), (doc, processor, target, owner) => new(doc, processor, target, owner, 8, 1, null, true));
        Assert.True(await workflow.ImportAsync(path, TestContext.Current.CancellationToken));
        var state = workflow.State; var attachment = state.Snapshot.State.GraphAttachments.Single(); var graph = state.Snapshot.State.Graphs.Single();
        using var editor = new NodeEditorSession(); editor.Open(state, new(state.Document.LifecycleId, attachment.Id, graph.Id,
            graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain).Id)); Assert.Null(sampler);
        var play = workflow.PlayAsync(); // Explicit deterministic execution; no physical device is opened.
        for (int i = 0; !play.IsCompleted && i < 10000; i++)
        { await workflow.ObserveAsync(); sampler?.Process(new float[64]); await Task.Yield(); }
        Assert.True(await play.WaitAsync(TestContext.Current.CancellationToken));
        sampler!.Process(new float[64]); var before = sampler.ReadStatus();
        editor.Observe(workflow.State); editor.BeginDraft(); editor.UpdateDraft(0.5m);
        Assert.True(await workflow.SetGraphGainAsync(editor.CompleteDraft()!));
        for (int i = 0; sampler.ReadStatus().ExecutingRevision != workflow.State.Snapshot.Revision && i < 10000; i++)
        { await workflow.ObserveAsync(); sampler.Process(new float[64]); await Task.Yield(); }
        var after = sampler.ReadStatus();
        Assert.Equal(workflow.State.Snapshot.Revision, after.ExecutingRevision);
        Assert.Equal(before.PreparedExecutionId, after.PreparedExecutionId); Assert.Equal(before.Epoch, after.Epoch);
        var packet = new float[64]; sampler.Process(packet);
        Assert.All(packet, value => Assert.InRange(Math.Abs(value - 0.1f), 0, 2e-6f));
        Assert.True(await workflow.StopAsync()); sampler.Process(packet); Assert.False(sampler.ReadStatus().Playing);
        editor.Observe(workflow.State); editor.BeginDraft(); editor.UpdateDraft(0.75m);
        Assert.True(await workflow.SetGraphGainAsync(editor.CompleteDraft()!)); sampler.Process(packet);
        Assert.False(sampler.ReadStatus().Playing); Assert.All(packet, value => Assert.Equal(0f, value));
        Assert.True(await workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.True(output.Joined); Assert.True(output.Disposed);
        Assert.Equal(0, sampler.LivePreparedStates); Assert.Equal(0, sampler.ReservedPcmAndScratchBytes);
    }

    [Fact]
    public async Task CancelledOpenFailedReplacementAndSavePreserveTargetSuccessfulReplacementClearsIt()
    {
        using var dir = new TemporaryDirectory(); var f = new GraphFixture(false);
        await using var workflow = new DesktopWorkflow(f.Document); await workflow.SelectAsync(f.Attachment.Id);
        using var editor = new NodeEditorSession(); editor.Open(workflow.State, Target(f)); var key = editor.Target;
        Assert.False(await workflow.OpenAsync(dir.File("missing.seqvium"), UnsavedDecision.Cancel));
        editor.Observe(workflow.State); Assert.Equal(key, editor.Target);
        Assert.False(await workflow.OpenAsync(dir.File("missing.seqvium"), UnsavedDecision.Discard));
        editor.Observe(workflow.State); Assert.Equal(key, editor.Target);
        string path = dir.File("saved.seqvium"); Assert.True(await workflow.SaveAsync(path));
        editor.Observe(workflow.State); Assert.Equal(key, editor.Target);
        Assert.True(await workflow.OpenAsync(path, UnsavedDecision.Discard)); editor.Observe(workflow.State);
        Assert.Null(editor.Target); Assert.Null(workflow.State.Execution);
    }

    [Theory]
    [InlineData("normal", true)]
    [InlineData("drag", false)]
    [InlineData("port", false)]
    [InlineData("other", false)]
    [InlineData("capture", false)]
    [InlineData("single", false)]
    public void OnlyTwoCompletedSameBodyClicksOpen(string sequence, bool expected)
    {
        var f = new GraphFixture(false); var key = Target(f); var clicks = new NodeBodyClick();
        clicks.Press(key, 1);
        if (sequence == "drag") clicks.Move();
        Assert.Null(clicks.Release(key));
        if (sequence == "capture") clicks.Cancel();
        var second = sequence == "port" ? null : sequence == "other" ? Target(f, f.Output) : key;
        clicks.Press(second, sequence == "single" ? 1 : 2);
        Assert.Equal(expected ? key : null, clicks.Release(second));
    }

    [Fact]
    public void OlderLayoutStartsEditorHiddenAndNewPaneRetainsGeometryWithoutMusicIdentity()
    {
        using var old = new WorkspaceState(); old.Reflow(1100, 640); old.Open(WorkspaceState.GraphId);
        var saved = old.Capture() with { Panes = [.. old.Panes.Where(p => p.InstanceId != WorkspaceState.NodeEditorId)] };
        using var restored = new WorkspaceState(saved); restored.Reflow(1100, 640);
        Assert.Equal(PaneVisibility.Hidden, restored.Get(WorkspaceState.NodeEditorId).Visibility);
        restored.Open(WorkspaceState.NodeEditorId); var bounds = restored.Bounds(WorkspaceState.NodeEditorId);
        restored.Collapse(WorkspaceState.NodeEditorId); restored.Open(WorkspaceState.NodeEditorId);
        restored.Hide(WorkspaceState.NodeEditorId); restored.Open(WorkspaceState.NodeEditorId);
        Assert.Equal(bounds, restored.Bounds(WorkspaceState.NodeEditorId));
        Assert.Equal(6, restored.Panes.Length);
    }

    [Fact]
    public void GenerationChangeBetweenBodyClicksCannotOpenEvenWhenLogicalTargetSurvives()
    {
        var f = new GraphFixture(false); var key = Target(f); var clicks = new NodeBodyClick();
        clicks.Press(key, 1, 10); Assert.Null(clicks.Release(key, 10));
        clicks.Press(key, 2, 12); Assert.Null(clicks.Release(key, 12));
    }

    [Fact]
    public async Task MenuOpeningIgnoresAnEarlierWorkflowFailureButStillRefusesExpiredActions()
    {
        var f = new GraphFixture(false); await using var workflow = new DesktopWorkflow(f.Document);
        await workflow.SelectAsync(f.Attachment.Id); await workflow.ReportFailureAsync("Earlier failure");
        var graph = new GraphEditorSession(); graph.Open(workflow.State, f.Attachment.Id);
        using var node = new NodeEditorSession(); var menu = new GraphMenuSession(graph, workflow);
        Assert.Null(menu.Begin());
        var open = menu.CaptureView(() => { Assert.True(node.Open(workflow.State, Target(f))); return Task.CompletedTask; });
        Assert.Null(await open()); Assert.Equal(Target(f), node.Target);
        var expired = menu.CaptureView(() => throw new InvalidOperationException("Expired callback ran"));
        menu.Cancel(); Assert.Equal("Graph.Stale", await expired());
    }

    [Fact]
    public async Task ProgrammaticTextRefreshNeverStartsUserDraftIncludingDeferredRoutedNotification()
    {
        using var dir = new TemporaryDirectory(); var f = new GraphFixture(false);
        await using var workflow = new DesktopWorkflow(f.Document);
        using var shell = new ShellSession(new(dir.File("preferences.json")), new(new(), false), f.Document) { HasWorkflowOwner = true };
        using var editor = new NodeEditorSession(); editor.Open(workflow.State, Target(f));
        using var content = new NodeEditorPaneContent(shell, workflow, editor);
        var value = (TextBox)typeof(NodeEditorPaneContent).GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(content)!;
        Assert.False(editor.HasDraft); Assert.Equal("1", value.Text);
        value.Text = "0.25"; Assert.True(editor.HasDraft); Assert.Equal(0.25m, editor.Draft!.After);
        editor.CancelDraft(); content.Refresh();
        Assert.False(editor.HasDraft); Assert.Equal("1", value.Text);
        // Reproduce the framework's later notification after the synchronous refresh guard has ended.
        value.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent));
        Assert.False(editor.HasDraft);
        int history = workflow.State.UndoCount;
        content.Refresh(); Assert.False(editor.HasDraft); Assert.Equal(history, workflow.State.UndoCount);
        content.Dispose(); Assert.False(editor.HasDraft);
    }

    [Theory]
    [InlineData(HostLanguage.English, HostTheme.Dark)]
    [InlineData(HostLanguage.English, HostTheme.Light)]
    [InlineData(HostLanguage.Russian, HostTheme.Dark)]
    [InlineData(HostLanguage.Russian, HostTheme.Light)]
    public async Task EmptyNodeEditorHasOneContentHintAndHidesPlaceholderHeadersAndStatus(HostLanguage language, HostTheme theme)
    {
        using var dir = new TemporaryDirectory(); var document = ProjectDocument.Create();
        await using var workflow = new DesktopWorkflow(document);
        using var shell = new ShellSession(new(dir.File("preferences.json")), new(new(language, theme), false), document) { HasWorkflowOwner = true };
        using var session = new NodeEditorSession(); using var content = new NodeEditorPaneContent(shell, workflow, session);
        Assert.Equal(shell["NodeEditor.NoTarget"], Field<TextBlock>(content, "_details").Text);
        Assert.Equal(shell["Pane.NodeEditor"], content.Title);
        Assert.False(Field<TextBlock>(content, "_scope").IsVisible);
        Assert.False(Field<TextBlock>(content, "_status").IsVisible);
        Assert.Equal("", Field<TextBlock>(content, "_status").Text);
        using var workspace = new WorkspaceState();
        Assert.Equal(PaneVisibility.Hidden, workspace.Get(WorkspaceState.NodeEditorId).Visibility);
    }

    [Fact]
    public async Task DeletedNodeKeepsOneMeaningfulContentWarningWithoutAnEmptyFooter()
    {
        using var dir = new TemporaryDirectory(); var f = new GraphFixture(false);
        using var session = new NodeEditorSession(); var key = Target(f); session.Open(Observe(f.Document), key);
        f.Document.Edit("Remove node", edit => edit.DeleteGraphNode(f.Graph.Id, f.Gain.Id));
        await using var workflow = new DesktopWorkflow(f.Document);
        using var shell = new ShellSession(new(dir.File("preferences.json")), new(new(), false), f.Document) { HasWorkflowOwner = true };
        using var content = new NodeEditorPaneContent(shell, workflow, session);
        Assert.Equal(key, session.Target); Assert.Null(session.Node);
        Assert.Equal(shell["NodeEditor.TargetMissing"], Field<TextBlock>(content, "_details").Text);
        Assert.Equal(shell["Pane.NodeEditor"], content.Title);
        Assert.False(Field<TextBlock>(content, "_status").IsVisible);
        Assert.False(Field<TextBox>(content, "_value").IsEnabled);
    }

    [Fact]
    public void GainVisualHierarchyOnlySuppressesTheSameFourPartParameterIdentity()
    {
        var f = new GraphFixture(false); var state = Observe(f.Document) with { Selected = f.Attachment.Id, Gain = 1m };
        var graph = new GraphEditorSession(); graph.Open(state, f.Attachment.Id); graph.Select(new(GraphHitKind.Node, f.Gain.Id));
        using var node = new NodeEditorSession(); node.Open(state, Target(f));
        using var workspace = new WorkspaceState(); workspace.Reflow(1100, 640);
        workspace.Open(WorkspaceState.GraphId); workspace.Open(WorkspaceState.NodeEditorId);
        Assert.Equal(new GainSurfaceVisibility(true, true), GainSurfacePresentation.Resolve(state, graph, node.Target, workspace));
        workspace.Hide(WorkspaceState.NodeEditorId);
        Assert.Equal(new GainSurfaceVisibility(true, false), GainSurfacePresentation.Resolve(state, graph, node.Target, workspace));
        graph.Select(new(GraphHitKind.Node, f.Source.Id));
        Assert.Equal(new GainSurfaceVisibility(false, false), GainSurfacePresentation.Resolve(state, graph, node.Target, workspace));
        workspace.Open(WorkspaceState.NodeEditorId);
        Assert.Equal(new GainSurfaceVisibility(true, false), GainSurfacePresentation.Resolve(state, graph, node.Target, workspace));
        GraphAttachment other = null!; GraphNode otherGain = null!;
        f.Document.Edit("Other scope", edit =>
        { other = edit.CreateItemGraph(f.OtherPlacement); otherGain = edit.AddGraphNode(other.GraphId, GraphBuiltIns.Gain, new(0, 0)); });
        state = Observe(f.Document) with { Selected = f.Attachment.Id, Gain = 1m };
        graph.Observe(state); graph.Select(new(GraphHitKind.Node, f.Gain.Id));
        node.Open(state, new(f.Document.LifecycleId, other.Id, other.GraphId, otherGain.Id));
        workspace.Hide(WorkspaceState.GraphId);
        Assert.Equal(new GainSurfaceVisibility(false, false), GainSurfacePresentation.Resolve(state, graph, node.Target, workspace));
        workspace.Open(WorkspaceState.GraphId);
        Assert.Equal(new GainSurfaceVisibility(true, false), GainSurfacePresentation.Resolve(state, graph, node.Target, workspace));
    }

    [Fact]
    public async Task SharedContainingScopeShowsItsOwnerWithoutClaimingOnePlacementOrThrowing()
    {
        var f = new GraphFixture(false); var json = GraphFixture.Json(f.Document);
        json["state"]!["contexts"]![0]!["level"] = "containing";
        json["state"]!["contexts"]![0]!["name"] = "Shared container";
        foreach (var placement in json["state"]!["placements"]!.AsArray())
        {
            placement!["itemContextId"] = null;
            placement["containingContextId"] = f.Attachment.ContextId.ToString();
        }
        var document = GraphFixture.Read(json).Document;
        using var dir = new TemporaryDirectory(); await using var workflow = new DesktopWorkflow(document);
        using var shell = new ShellSession(new(dir.File("prefs.json")), new(new(), false), document) { HasWorkflowOwner = true };
        using var session = new NodeEditorSession();
        Assert.True(session.Open(Observe(document), Target(f) with { LifecycleId = document.LifecycleId }));
        using var content = new NodeEditorPaneContent(shell, workflow, session);
        var scope = Field<TextBlock>(content, "_scope").Text;
        Assert.Contains("Shared container", scope); Assert.Contains("Container", scope);
        Assert.DoesNotContain("Pattern", scope); Assert.Equal(f.Gain.Id, session.Node!.Id);
    }

    [Fact]
    public void ChangeNoticeRequiresRealInterveningDraftAndCanBeDismissedWithoutEditing()
    {
        var f = new GraphFixture(false); using var editor = new NodeEditorSession(); editor.Open(Observe(f.Document), Target(f));
        Assert.False(editor.ChangedElsewhere);
        editor.BeginDraft(); editor.UpdateDraft(0.5m); var own = editor.CompleteDraft()!;
        f.Document.Edit("Own commit", edit => edit.SetGraphGain(f.Graph.Id, f.Gain.Id, own.After));
        editor.Observe(Observe(f.Document)); Assert.False(editor.ChangedElsewhere);
        editor.BeginDraft(); editor.UpdateDraft(0.75m);
        f.Document.Edit("External commit", edit => edit.SetGraphGain(f.Graph.Id, f.Gain.Id, 0.25m));
        editor.Observe(Observe(f.Document)); Assert.True(editor.ChangedElsewhere); Assert.False(editor.HasDraft);
        var before = f.Document.Current; int history = f.Document.UndoCount;
        editor.DismissChangeNotice(); Assert.False(editor.ChangedElsewhere);
        Assert.Same(before, f.Document.Current); Assert.Equal(history, f.Document.UndoCount);
    }
}
