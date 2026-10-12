// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphEditorSessionTests
{
    private static WorkflowState Observation(ProjectDocument document, Id<GraphAttachment>? selected = null) =>
        new(document, [], selected, null, "", "", "Workflow.Ready", null, null, false, true, false, null);

    [Fact]
    public void ExplicitTargetDoesNotFollowAnotherSelectionAndReopeningRetainsViewport()
    {
        var fixture = new GraphFixture(false); var editor = new GraphEditorSession();
        Assert.True(editor.Open(Observation(fixture.Document), fixture.Attachment.Id));
        editor.Viewport.Pan(new(100, -80)); var offset = editor.Viewport.Offset;
        GraphAttachment other = null!;
        fixture.Document.Edit("Other graph", edit => other = edit.CreateItemGraph(fixture.OtherPlacement));
        editor.Observe(Observation(fixture.Document, other.Id));
        Assert.Equal(fixture.Attachment.Id, editor.Target); Assert.Equal(fixture.Graph.Id, editor.Graph!.Id);
        Assert.True(editor.Open(Observation(fixture.Document), fixture.Attachment.Id));
        Assert.Equal(offset, editor.Viewport.Offset);
    }

    [Fact]
    public void HundredsOfMoveUpdatesProduceOneCanonicalEditAndExactUndoRedo()
    {
        var fixture = new GraphFixture(false); var editor = new GraphEditorSession();
        editor.Open(Observation(fixture.Document), fixture.Attachment.Id);
        var before = fixture.Document.Current; int history = fixture.Document.UndoCount;
        Assert.True(editor.BeginMove(fixture.Gain.Id));
        for (int i = 1; i <= 500; i++) editor.UpdateMove(new(i, -i / 2.0));
        Assert.Same(before, fixture.Document.Current); Assert.Equal(history, fixture.Document.UndoCount);
        var move = Assert.IsType<GraphMoveProposal>(editor.CompleteMove());
        fixture.Document.Edit("Move graph node", edit => edit.MoveGraphNode(fixture.Graph.Id, move.Node, move.After));
        var after = fixture.Document.Current;
        Assert.Equal(history + 1, fixture.Document.UndoCount);
        Assert.Equal(new GraphPosition(450, -230), fixture.Graph.Nodes.Single(n => n.Id == fixture.Gain.Id).Position);
        Assert.True(fixture.Document.Undo()); Assert.Same(before, fixture.Document.Current);
        Assert.True(fixture.Document.Redo()); Assert.Same(after, fixture.Document.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelAndNoOpDiscardPreviewWithoutHistory(bool cancel)
    {
        var fixture = new GraphFixture(false); var editor = new GraphEditorSession();
        editor.Open(Observation(fixture.Document), fixture.Attachment.Id); var before = fixture.Document.Current;
        int count = fixture.Document.UndoCount; editor.BeginMove(fixture.Gain.Id);
        editor.UpdateMove(new(40, 80));
        if (cancel) editor.Cancel(); else editor.UpdateMove(default);
        Assert.Null(editor.CompleteMove()); Assert.Same(before, fixture.Document.Current); Assert.Equal(count, fixture.Document.UndoCount);
    }

    [Fact]
    public void NewGenerationCancelsPreviewIncludingUndoRedoToIdenticalRevision()
    {
        var fixture = new GraphFixture(false); var editor = new GraphEditorSession();
        editor.Open(Observation(fixture.Document), fixture.Attachment.Id); editor.BeginMove(fixture.Gain.Id); editor.UpdateMove(new(20, 20));
        fixture.Document.Edit("Intervening change", edit => edit.SetGraphGain(fixture.Graph.Id, fixture.Gain.Id, 0.2m));
        fixture.Document.Undo(); Assert.True(editor.Observe(Observation(fixture.Document)));
        Assert.Null(editor.Move); Assert.Null(editor.CompleteMove());
    }

    [Fact]
    public void MissingAttachmentRetainsIdentityAndUndoRestoresTargetWithoutRevivingGesture()
    {
        var fixture = new GraphFixture(false); var editor = new GraphEditorSession();
        editor.Open(Observation(fixture.Document), fixture.Attachment.Id); editor.BeginMove(fixture.Gain.Id); editor.UpdateMove(new(20, 20));
        fixture.Document.Edit("Delete target", edit => edit.DeletePlacement(fixture.Placement));
        editor.Observe(Observation(fixture.Document)); Assert.Null(editor.Graph); Assert.Null(editor.Move);
        Assert.Equal(fixture.Attachment.Id, editor.Target);
        fixture.Document.Undo(); editor.Observe(Observation(fixture.Document));
        Assert.Equal(fixture.Attachment.Id, editor.Attachment!.Id); Assert.Null(editor.Move);
    }

    [Fact]
    public void ReopenSameProjectAndAttachmentRequiresFreshExplicitTarget()
    {
        var fixture = new GraphFixture(false); var editor = new GraphEditorSession();
        editor.Open(Observation(fixture.Document), fixture.Attachment.Id); editor.BeginMove(fixture.Gain.Id);
        var reopened = GraphFixture.Reopen(fixture.Document).Document;
        editor.Observe(Observation(reopened, fixture.Attachment.Id));
        Assert.Null(editor.Target); Assert.Null(editor.Graph); Assert.Null(editor.Move);
        Assert.True(editor.Open(Observation(reopened), fixture.Attachment.Id));
        Assert.Equal(fixture.Graph.Id, editor.Graph!.Id); reopened.Close();
    }

    [Fact]
    public async Task LanguageThemeAndWorkspaceOperationsPreserveTargetAndCanonicalCoordinates()
    {
        var fixture = new GraphFixture(false); var editor = new GraphEditorSession();
        editor.Open(Observation(fixture.Document), fixture.Attachment.Id);
        using var directory = new TemporaryDirectory();
        using var shell = new ShellSession(new(directory.File("prefs.json")), new(new(), false), fixture.Document);
        using var workspace = new WorkspaceState(); workspace.Reflow(1100, 640); workspace.Open(WorkspaceState.GraphId);
        var before = fixture.Document.Current;
        foreach (var dock in new[] { PaneDock.Left, PaneDock.Right, PaneDock.Floating })
        { workspace.Dock(WorkspaceState.GraphId, dock); workspace.Collapse(WorkspaceState.GraphId); workspace.Open(WorkspaceState.GraphId); }
        workspace.Hide(WorkspaceState.GraphId); workspace.Open(WorkspaceState.GraphId);
        await shell.ToggleLanguageAsync(); await shell.ToggleThemeAsync(); editor.Observe(Observation(fixture.Document));
        Assert.Equal(fixture.Attachment.Id, editor.Target); Assert.Same(before, fixture.Document.Current);
    }

    [Fact]
    public void OldTwoPaneLayoutRestoresAndNewGraphPaneStartsHidden()
    {
        using var old = new WorkspaceState(); old.Reflow(1100, 640); old.Open(WorkspaceState.InspectorId);
        var saved = old.Capture() with { Panes = [.. old.Panes.Where(p => p.InstanceId != WorkspaceState.GraphId)] };
        using var restored = new WorkspaceState(saved); restored.Reflow(1100, 640);
        Assert.Equal(old.Get(WorkspaceState.InspectorId), restored.Get(WorkspaceState.InspectorId));
        Assert.Equal(PaneVisibility.Hidden, restored.Get(WorkspaceState.GraphId).Visibility);
        Assert.Equal(WorkspaceState.InspectorId, restored.ActivePaneId);
    }

    [Fact]
    public void ReplacementObservationClearsOldGraphBeforeDiagnosticsCanReadNewDocument()
    {
        var first = new GraphFixture(false); var second = new GraphFixture(false); var editor = new GraphEditorSession();
        editor.Open(Observation(first.Document), first.Attachment.Id);
        Assert.Equal(first.Graph.Id, editor.Report!.GraphId);
        editor.Observe(Observation(second.Document, second.Attachment.Id));
        Assert.Same(second.Document.Current.State, editor.State); Assert.Null(editor.Graph); Assert.Null(editor.Report);
        editor.Open(Observation(second.Document), second.Attachment.Id);
        Assert.Equal(second.Graph.Id, editor.Report!.GraphId);
        Assert.DoesNotContain(editor.State!.Graphs, g => g.Id == first.Graph.Id);
    }

    [Fact]
    public void ClosedDocumentObservationRemovesCommandAuthorityAndCannotReopenTarget()
    {
        var f = new GraphFixture(false); var editor = new GraphEditorSession();
        editor.Open(Observation(f.Document), f.Attachment.Id); editor.BeginMove(f.Gain.Id); editor.UpdateMove(new(40, 20));
        f.Document.Close(); var closed = Observation(f.Document);
        Assert.True(closed.IsClosed); editor.Observe(closed);
        Assert.Null(editor.Target); Assert.Null(editor.Move); Assert.Null(editor.Basis); Assert.Null(editor.Report);
        Assert.False(editor.Open(closed, f.Attachment.Id));
    }
}
