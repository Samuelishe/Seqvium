// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Seqvium.Desktop;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class NativePointer
{
    internal static async Task<object> VerifyDrumAsync(MainWindow window, GainDialControl dial,
        DrumEnvelopePlot plot, DrumVoicePaneContent pane, DesktopWorkflow workflow)
    {
        if (!OperatingSystem.IsWindows()) return new { Result = "not-run", Reason = "Windows only" };
        nint handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        var passed = new List<string>();
        bool down = false;

        void Guard()
        {
            if (handle == 0 || GetForegroundWindow() != handle) throw new ForegroundLostException();
        }

        void Move(Control control, Point point)
        {
            Guard();
            var screen = control.PointToScreen(point);
            Program.Require(SetCursorPos(screen.X, screen.Y), "Drum pointer move failed.");
        }

        void Button(bool press)
        {
            Guard();
            Program.Require(SendInput(1,
                [new Input { Type = 0, Data = new() { Mouse = new() { Flags = press ? 2u : 4u } } }],
                Marshal.SizeOf<Input>()) == 1, "Drum pointer input failed.");
            down = press;
        }

        try
        {
            window.Activate();
            await Task.Delay(120);
            Guard();
            var before = workflow.State.Snapshot;
            int undo = workflow.State.UndoCount;
            Move(dial, new(dial.Bounds.Width / 2, dial.Bounds.Height / 2));
            Button(true);
            await Task.Delay(50);
            Move(dial, new(dial.Bounds.Width / 2, dial.Bounds.Height / 2 - 10));
            await Task.Delay(50);
            Button(false);
            await Task.Delay(80);
            await pane.PendingCommand;
            Program.Require(workflow.State.UndoCount == undo + 1, "Native Drum dial did not commit once.");
            passed.Add("native dial capture and one Undo");
            Program.Require(await workflow.UndoAsync() && workflow.State.Snapshot.Revision == before.Revision,
                "Native Drum dial Undo failed.");
            await Task.Delay(50);
            before = workflow.State.Snapshot;
            undo = workflow.State.UndoCount;
            var point = (Point[])typeof(DrumEnvelopePlot).GetMethod("Handles",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(plot,
                [Math.Max(150, (double)plot.Values["decay"] * 1.12)])!;
            Move(plot, point[1]);
            Button(true);
            await Task.Delay(50);
            Move(plot, point[1] + new Vector(0, 12));
            await Task.Delay(50);
            Button(false);
            await Task.Delay(100);
            await pane.PendingCommand;
            Program.Require(workflow.State.UndoCount == undo + 1, "Native amplitude handle did not commit once.");
            passed.Add("native " + (plot.AmplitudeMode ? "amplitude" : "pitch") + " handle and one Undo");
            Program.Require(await workflow.UndoAsync() && workflow.State.Snapshot.Revision == before.Revision,
                "Native amplitude Undo failed.");
            return new { Result = "passed", ForegroundGuard = true, Passed = passed };
        }
        catch (ForegroundLostException)
        {
            return new
            {
                Result = "refused-or-partial", ForegroundGuard = true, Passed = passed, Reason = "Foreground lost"
            };
        }
        finally
        {
            if (down && GetForegroundWindow() == handle)
                SendInput(1, [new Input { Type = 0, Data = new() { Mouse = new() { Flags = 4u } } }],
                    Marshal.SizeOf<Input>());
        }
    }
}
