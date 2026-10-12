// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Seqvium.Desktop;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Seqvium.Core;

namespace Seqvium.VisualCheck;

/// <summary>Native Windows pointer evidence is admitted only while this exact window owns foreground.</summary>
internal static class NativePointer
{
    public static async Task<object> VerifyAsync(MainWindow window, GraphCanvas canvas, WorkspaceHost workspace,
        DesktopWorkflow workflow, GraphEditorSession editor, NodeEditorSession nodeEditor, NodeEditorTarget target)
    {
        if (!OperatingSystem.IsWindows()) return new { Result = "not-run", Reason = "Windows only" };
        nint handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        var passed = new List<string>();
        try
        {
            void Guard() { if (handle == 0 || GetForegroundWindow() != handle) throw new ForegroundLostException(); }
            void Move(Control control, Point point)
            {
                Guard(); var screen = control.PointToScreen(point); Program.Require(SetCursorPos(screen.X, screen.Y), "SetCursorPos failed.");
            }
            void Button(bool down)
            {
                Guard(); var input = new Input { Type = 0, Data = new InputUnion { Mouse = new MouseInput { Flags = down ? 2u : 4u } } };
                Program.Require(SendInput(1, [input], Marshal.SizeOf<Input>()) == 1, "SendInput failed.");
            }
            void Key(ushort key, bool down)
            {
                Guard(); var input = new Input { Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput { Key = key, Flags = down ? 0u : 2u } } };
                Program.Require(SendInput(1, [input], Marshal.SizeOf<Input>()) == 1, "Keyboard SendInput failed.");
            }
            async Task Click(Control control, Point point, bool micro = false)
            {
                Move(control, point); Button(true); await Task.Delay(40);
                if (micro) Move(control, point + new Vector(1, 1));
                Button(false); await Task.Delay(120);
            }
            Guard();
            var graph = editor.Graph!; var gain = graph.Nodes.Single(n => n.Id == target.NodeId);
            var body = GraphGeometry.Body(gain);
            var at = editor.Viewport.ToScreen(new(body.X + 50, body.Y + 18));
            int undo = workflow.State.Document.UndoCount;
            await Click(canvas, new(at.X, at.Y), true);
            Program.Require(editor.SelectedNode == gain.Id && workflow.State.Document.UndoCount == undo, "Native micro click changed history/selection.");
            passed.Add("native micro click selects without Undo");
            Point Background()
            {
                for (double y = 20; y < canvas.Bounds.Height - 20; y += 25)
                for (double x = 20; x < canvas.Bounds.Width - 20; x += 25)
                    if (GraphGeometry.HitTest(editor.Graph!, editor.Viewport.ToGraph(new(x, y)), editor.Viewport.Zoom).Kind == GraphHitKind.Background)
                        return new(x, y);
                throw new InvalidOperationException("No actual graph background hit found.");
            }
            var background = Background();
            await Click(canvas, background);
            Program.Require(editor.SelectedNode is null && nodeEditor.Target == target, "Background click lost explicit editor target.");
            passed.Add("native graph background clears selection and retains editor");
            var before = editor.Viewport.Offset;
            Move(canvas, background); Button(true); Move(canvas, background + new Vector(25, 15)); await Task.Delay(80); Button(false); await Task.Delay(120);
            Program.Require(editor.Viewport.Offset != before && workflow.State.Document.UndoCount == undo, "Native background pan failed.");
            passed.Add("native background pan without Undo");
            var workspaceCanvas = Program.Field<Canvas>(workspace, "_canvas");
            var free = new Point(workspaceCanvas.Bounds.Width - 20, workspaceCanvas.Bounds.Height - 20);
            Program.Require(workspaceCanvas.InputHitTest(free) == workspaceCanvas, "Workspace test coordinate is not free background.");
            await Click(workspaceCanvas, free);
            Program.Require(workspace.State.ActivePaneId is null && nodeEditor.Target == target && workflow.State.Document.UndoCount == undo, "Native workspace deactivation failed.");
            passed.Add("native workspace click deactivates without musical mutation");
            Program.Require(workspaceCanvas.IsFocused, "Workspace background did not receive keyboard focus.");
            await Click(canvas, Background());
            Program.Require(workspace.State.ActivePaneId == WorkspaceState.GraphId, "Native pane reactivation failed.");
            passed.Add("native pane reactivation");
            Point Body(GraphNode node)
            {
                var rect = GraphGeometry.Body(node); var screen = editor.Viewport.ToScreen(new(rect.X + 50, rect.Y + 18));
                var point = new Point(screen.X, screen.Y);
                Program.Require(point.X > 0 && point.Y > 0 && point.X < canvas.Bounds.Width && point.Y < canvas.Bounds.Height, "Native body fixture is outside viewport.");
                return point;
            }
            async Task DoubleClick(Point point)
            {
                Move(canvas, point); Button(true); await Task.Delay(25); Button(false); await Task.Delay(25);
                Button(true); await Task.Delay(25); Button(false); await Task.Delay(160);
            }
            canvas.Fit(); await Task.Delay(80);
            var source = editor.Graph!.Nodes.Single(n => n.Type == GraphBuiltIns.Source);
            await DoubleClick(Body(source));
            Program.Require(nodeEditor.Target?.NodeId == source.Id && workflow.State.Document.UndoCount == undo, "Native Source body double click failed or edited music.");
            await DoubleClick(Body(editor.Graph!.Nodes.Single(n => n.Id == target.NodeId)));
            Program.Require(nodeEditor.Target == target && workflow.State.Document.UndoCount == undo, "Native Gain body double click failed or edited music.");
            passed.Add("native body double click opens Source then Gain without Undo");
            foreach (double zoom in new[] { .5, 1, 2 })
            {
                canvas.Fit(); editor.Viewport.ZoomAt(new(canvas.Bounds.Width / 2, canvas.Bounds.Height / 2), zoom / editor.Viewport.Zoom);
                var node = editor.Graph!.Nodes.Single(n => n.Id == target.NodeId); var start = Body(node);
                Move(canvas, start); Button(true); await Task.Delay(40); Move(canvas, start + new Vector(1, 1)); await Task.Delay(40);
                Program.Require(workflow.State.Document.UndoCount == undo && editor.Move?.After == node.Position, "Native micro movement preview changed a node.");
                Move(canvas, start + new Vector(12, 8)); await Task.Delay(60); Button(false); await Task.Delay(120); await canvas.PendingCommand;
                editor.Observe(workflow.State);
                var moved = editor.Graph!.Nodes.Single(n => n.Id == target.NodeId).Position;
                Program.Require(workflow.State.Document.UndoCount == undo + 1 && Math.Abs(moved.X - node.Position.X - 12 / zoom) < .0001 &&
                    Math.Abs(moved.Y - node.Position.Y - 8 / zoom) < .0001, "Native drag DIP/zoom/history failed.");
                Program.Require(await workflow.UndoAsync(), "Native drag Undo failed."); editor.Observe(workflow.State);
                passed.Add($"native drag at zoom {zoom}: micro threshold / one Undo / exact displacement / restoration");
            }
            var cancelStart = Body(editor.Graph!.Nodes.Single(n => n.Id == target.NodeId));
            Move(canvas, cancelStart); Button(true); await Task.Delay(40); Move(canvas, cancelStart + new Vector(20, 0)); await Task.Delay(40);
            Key(0x1B, true); Key(0x1B, false); await Task.Delay(60); Button(false); await Task.Delay(120);
            Program.Require(editor.Move is null && workflow.State.Document.UndoCount == undo, "Native Escape did not cancel movement.");
            passed.Add("native Escape cancels captured drag without Undo");
            Program.Require(canvas.IsFocused, "Native keyboard fixture lost canvas focus.");
            var precise = editor.Graph!.Nodes.Single(n => n.Id == target.NodeId).Position;
            Key(0x27, true); Key(0x27, false); await Task.Delay(120); await canvas.PendingCommand; editor.Observe(workflow.State);
            Program.Require(editor.Graph!.Nodes.Single(n => n.Id == target.NodeId).Position.X == precise.X + 1 && workflow.State.Document.UndoCount == undo + 1,
                "Native arrow movement was not exactly one graph unit / one Undo.");
            Program.Require(await workflow.UndoAsync(), "Native arrow Undo failed."); editor.Observe(workflow.State);
            passed.Add("native arrow moves one graph unit at zoom 2 and restores through Undo");
            return new { Result = "passed", Passed = passed, ForegroundGuard = true };
        }
        catch (ForegroundLostException) { return new { Result = "partial", Passed = passed, Reason = "Foreground guard refused further input", ForegroundGuard = true }; }
    }
    private sealed class ForegroundLostException : Exception;
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public nuint ExtraInfo; }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, Input[] inputs, int size);
}
