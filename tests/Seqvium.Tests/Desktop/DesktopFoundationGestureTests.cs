// SPDX-License-Identifier: Apache-2.0
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class DesktopFoundationGestureTests
{
    [Fact]
    public void DeactivationPreservesOrderGeometryAndSurvivesLayoutRestore()
    {
        using var state = new WorkspaceState(); state.Reflow(1100, 650);
        state.Open(WorkspaceState.InspectorId); state.Open(WorkspaceState.AppearanceId);
        var before = state.Panes;
        state.Deactivate(); Assert.Null(state.ActivePaneId); Assert.Equal(before, state.Panes);
        state.Hide(WorkspaceState.InspectorId); Assert.Null(state.ActivePaneId);
        using var restored = new WorkspaceState(state.Capture()); restored.Reflow(1100, 650);
        Assert.Null(restored.ActivePaneId);
        restored.Activate(WorkspaceState.AppearanceId);
        Assert.Equal(WorkspaceState.AppearanceId, restored.ActivePaneId);
    }

    [Theory]
    [InlineData(0.25)] [InlineData(1)] [InlineData(4)]
    public void MicroMovementKeepsDoubleClickAndHistoryWhileRealDragCommitsOnce(double zoom)
    {
        var f = new GraphFixture(false); var editor = new GraphEditorSession();
        var state = new WorkflowState(f.Document, [], null, null, "", "", "Workflow.Ready", null, null, false, true, false, null);
        editor.Open(state, f.Attachment.Id);
        var target = new NodeEditorTarget(f.Document.LifecycleId, f.Attachment.Id, f.Graph.Id, f.Gain.Id);
        var clicks = new NodeBodyClick(); var drag = new GraphDragThreshold();
        int undo = f.Document.UndoCount;
        for (int count = 1; count <= 2; count++)
        {
            clicks.Press(target, count); drag.Reset(new(100, 100)); editor.BeginMove(f.Gain.Id);
            Assert.False(drag.Update(new(102, 101))); Assert.False(drag.Update(new(104, 100)));
            Assert.Null(editor.CompleteMove());
            Assert.Equal(count == 2 ? target : null, clicks.Release(target));
        }
        Assert.Equal(undo, f.Document.UndoCount);
        clicks.Press(target, 1); drag.Reset(default); editor.BeginMove(f.Gain.Id);
        Assert.True(drag.Update(new(5, 0))); clicks.Move(); editor.UpdateMove(new(5 / zoom, 0));
        Assert.True(drag.Update(default)); // Crossing the threshold is sticky until next press.
        var move = Assert.IsType<GraphMoveProposal>(editor.CompleteMove());
        Assert.Null(clicks.Release(target));
        f.Document.Edit("Move", edit => edit.MoveGraphNode(f.Graph.Id, move.Node, move.After));
        Assert.Equal(undo + 1, f.Document.UndoCount);
        drag.Reset(default); Assert.False(drag.Started);
    }
}
