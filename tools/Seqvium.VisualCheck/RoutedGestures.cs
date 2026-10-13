// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

/// <summary>Routed events exercise real controls/capture on the GUI thread, without claiming OS input.</summary>
internal static class RoutedGestures
{
    public static async Task<object> VerifyAsync(GraphCanvas canvas, WorkspaceHost workspace, DesktopWorkflow workflow,
        GraphEditorSession editor, NodeEditorSession nodeEditor, NodeEditorTarget target)
    {
        var pointer = new Pointer(100001, PointerType.Mouse, true);
        try
        {
        var down = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
        var move = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other);
        var up = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);
        int undo = workflow.State.Document.UndoCount; var before = workflow.State.Snapshot;
        var passed = new List<string>();
        var root = TopLevel.GetTopLevel(canvas) ?? throw new InvalidOperationException("Missing visual root.");
        Point Root(Point point) => canvas.TranslatePoint(point, root) ?? throw new InvalidOperationException("Missing root transform.");
        void Press(Point point, int count = 1) => canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, root, Root(point), 0, down, KeyModifiers.None, count));
        void Move(Point point) => canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, canvas, pointer, root, Root(point), 0, move, KeyModifiers.None));
        async Task Release(Point point)
        {
            canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, root, Root(point), 0, up, KeyModifiers.None, MouseButton.Left));
            await canvas.PendingCommand;
        }
        Point AtGain()
        {
            var node = editor.Graph!.Nodes.Single(n => n.Id == target.NodeId); var body = GraphGeometry.Body(node);
            var point = editor.Viewport.ToScreen(new(body.X + 50, body.Y + 18)); return new(point.X, point.Y);
        }
        foreach (double zoom in new[] { .5, 1, 2 })
        {
            canvas.Fit(); editor.Viewport.ZoomAt(default, zoom / editor.Viewport.Zoom);
            var origin = AtGain();
            Press(origin); Move(origin + new Vector(1, 1)); await Release(origin + new Vector(1, 1));
            Press(origin, 2); Move(origin + new Vector(1, 1)); await Release(origin + new Vector(1, 1));
            Program.Require(workflow.State.Document.UndoCount == undo && workflow.State.Snapshot == before, "Routed micro double click created history.");
            Program.Require(nodeEditor.Target == target, "Routed body double click lost target.");
            workspace.State.Activate(WorkspaceState.GraphId);
            origin = AtGain(); Press(origin); var pressedMove = editor.Move; Move(origin + new Vector(12, 8)); var moved = editor.Move; var captured = pointer.Captured?.GetType().Name;
            await Release(origin + new Vector(12, 8));
            Program.Require(workflow.State.Document.UndoCount == undo + 1, $"Routed drag did not commit exactly once: zoom={zoom}, origin={origin}, press={pressedMove}, move={moved}, capture={captured}, beforeUndo={undo}, undo={workflow.State.Document.UndoCount}, status={workflow.State.StatusKey}, detail={workflow.State.Detail}.");
            RequireObserved(editor, workflow);
            var after = editor.Graph!.Nodes.Single(n => n.Id == target.NodeId).Position;
            var old = before.State.Graphs.Single(g => g.Id == target.GraphDefinitionId).Nodes.Single(n => n.Id == target.NodeId).Position;
            Program.Require(Math.Abs(after.X - old.X - 12 / zoom) < .0001 && Math.Abs(after.Y - old.Y - 8 / zoom) < .0001, "Drag DIP/zoom conversion failed.");
            Program.Require(await workflow.UndoAsync(), "Gesture Undo failed."); RequireObserved(editor, workflow);
            undo = workflow.State.Document.UndoCount; before = workflow.State.Snapshot;
            origin = AtGain(); Press(origin); Move(origin + new Vector(20, 0)); pointer.Capture(null); await Release(origin + new Vector(20, 0));
            Program.Require(workflow.State.Document.UndoCount == undo, "Capture loss committed movement.");
            Press(origin); Move(origin + new Vector(20, 0));
            canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            await Release(origin + new Vector(20, 0));
            Program.Require(workflow.State.Document.UndoCount == undo, "Escape committed movement.");
            passed.Add($"micro/double click/drag/capture loss/Escape at zoom {zoom}");
        }
        canvas.Fit();
        var background = new Point(canvas.Bounds.Width / 2, canvas.Bounds.Height - 18);
        Program.Require(GraphGeometry.HitTest(editor.Graph!, editor.Viewport.ToGraph(new(background.X, background.Y)), editor.Viewport.Zoom).Kind == GraphHitKind.Background, "Routed background fixture is not empty.");
        Press(background); await Release(background);
        Program.Require(editor.SelectedNode is null && editor.SelectedConnection is null && nodeEditor.Target == target, "Routed background selection/target separation failed.");
        var offset = editor.Viewport.Offset;
        Press(background); Move(background + new Vector(1, 1)); await Release(background + new Vector(1, 1));
        Program.Require(editor.Viewport.Offset == offset, "Micro background movement panned.");
        Press(background); Move(background + new Vector(12, -8)); await Release(background + new Vector(12, -8));
        Program.Require(editor.Viewport.Offset != offset && workflow.State.Document.UndoCount == undo, "Pan created history or failed.");
        passed.Add("background click clears graph selection, drag pans, explicit editor survives");
        var node = editor.Graph!.Nodes.Single(n => n.Id == target.NodeId);
        var port = node.Ports[0]; var graphPort = GraphGeometry.Port(node, port);
        var screenPort = editor.Viewport.ToScreen(graphPort); var portPoint = new Point(screenPort.X, screenPort.Y);
        Program.Require(GraphGeometry.HitTest(editor.Graph, graphPort, editor.Viewport.Zoom).Kind == GraphHitKind.Port, "Port precedence failed.");
        Press(portPoint); Move(portPoint + new Vector(1, 0)); await Release(portPoint + new Vector(1, 0));
        Program.Require(workflow.State.Document.UndoCount == undo && workflow.State.Detail is null, "Ordinary port click attempted an edit/refusal.");
        Press(portPoint); Move(background); canvas.CancelGesture(); await Release(background);
        Program.Require(workflow.State.Document.UndoCount == undo && editor.Move is null, "Cancelled wire gesture moved a node.");
        passed.Add("port hit precedence, micro click without connection/refusal, cancelled wire without node movement");
        editor.Select(new(GraphHitKind.Node, node.Id));
        offset = editor.Viewport.Offset;
        var middleDown = new PointerPointProperties(RawInputModifiers.MiddleMouseButton, PointerUpdateKind.MiddleButtonPressed);
        canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, root, Root(background), 0, middleDown, KeyModifiers.None, 1));
        Move(background + new Vector(12, -8));
        canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, root, Root(background + new Vector(12, -8)), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.MiddleButtonReleased), KeyModifiers.None, MouseButton.Middle));
        Program.Require(editor.Viewport.Offset != offset && editor.SelectedNode == node.Id, "Middle pan changed selection or failed.");
        offset = editor.Viewport.Offset;
        canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Space });
        Press(background); Move(background + new Vector(-12, 8)); await Release(background + new Vector(-12, 8));
        canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Space });
        Program.Require(editor.Viewport.Offset != offset && editor.SelectedNode == node.Id && workflow.State.Document.UndoCount == undo, "Space pan changed music/selection or failed.");
        passed.Add("middle and Space pan preserve selected node and Undo");
        foreach (var modifiers in new[] { KeyModifiers.None, KeyModifiers.Shift })
        {
            var position = editor.Graph!.Nodes.Single(n => n.Id == node.Id).Position;
            canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right, KeyModifiers = modifiers });
            await canvas.PendingCommand; RequireObserved(editor, workflow);
            Program.Require(editor.Graph!.Nodes.Single(n => n.Id == node.Id).Position.X == position.X + (modifiers == KeyModifiers.Shift ? 10 : 1), "Exact keyboard movement failed.");
            Program.Require(workflow.State.Document.UndoCount == undo + 1, "Keyboard movement history failed.");
            Program.Require(await workflow.UndoAsync(), "Keyboard Undo failed."); RequireObserved(editor, workflow);
        }
        passed.Add("exact arrow and Shift-arrow movement / one Undo / restoration");
        var workspaceCanvas = Program.Field<Canvas>(workspace, "_canvas");
        var free = new Point(workspaceCanvas.Bounds.Width - 20, workspaceCanvas.Bounds.Height - 20);
                var freeHit = ((InputElement)root).InputHitTest(workspaceCanvas.TranslatePoint(free, (Visual)root)!.Value);
        Program.Require(freeHit == workspaceCanvas, "Workspace fixture hit=" + freeHit?.GetType().Name + "; enabled=" + workspaceCanvas.IsEffectivelyEnabled + "; bounds=" + workspaceCanvas.Bounds + "; panes=" + string.Join("|", workspace.State.Panes.Where(p => p.Visibility == PaneVisibility.Visible).Select(p => p.InstanceId + ":" + workspace.State.Bounds(p.InstanceId))));
        workspace.State.Activate(WorkspaceState.GraphId);
        var musical = workflow.State.Snapshot; var selected = editor.SelectedNode; var graphTarget = editor.Target;
        var freeRoot = workspaceCanvas.TranslatePoint(free, root) ?? throw new InvalidOperationException("Detached workspace.");
        workspaceCanvas.RaiseEvent(new PointerPressedEventArgs(workspaceCanvas, pointer, root, freeRoot, 0, down, KeyModifiers.None, 1));
        Program.Require(workspace.State.ActivePaneId is null && workspaceCanvas.IsFocused, "Workspace background did not deactivate and receive focus.");
        Program.Require(editor.SelectedNode == selected && editor.Target == graphTarget && nodeEditor.Target == target && workflow.State.Snapshot == musical,
            "Workspace background changed independent musical/editor targets.");
        Press(AtGain()); await Release(AtGain());
        Program.Require(workspace.State.ActivePaneId == WorkspaceState.GraphId, "Pane did not reactivate on content click.");
        var editorPane = workspace.State.Get(WorkspaceState.NodeEditorId); var bounds = editorPane.FloatingBounds;
        workspace.State.Dock(WorkspaceState.NodeEditorId, PaneDock.Right);
        workspace.State.Collapse(WorkspaceState.NodeEditorId); workspace.Open(WorkspaceState.NodeEditorId);
        workspace.State.Hide(WorkspaceState.NodeEditorId); workspace.Open(WorkspaceState.NodeEditorId);
        workspace.State.Dock(WorkspaceState.NodeEditorId, PaneDock.Floating);
        Program.Require(workspace.State.Get(WorkspaceState.NodeEditorId).FloatingBounds == bounds && nodeEditor.Target == target && workflow.State.Document.UndoCount == undo,
            "Dock/collapse/hide/reopen changed floating layout/target/history.");
        passed.Add("workspace deactivation/focus/reactivation and independent selection/target/history; dock/collapse/hide/reopen retain layout/editor");
        return new { Result = "passed", Input = "synthetic routed events, actual GUI controls", Passed = passed, NativeWindowsInput = false };
        }
        finally { pointer.Capture(null); canvas.CancelGesture(); }
    }
    private static void RequireObserved(GraphEditorSession editor, DesktopWorkflow workflow) => editor.Observe(workflow.State);
}
