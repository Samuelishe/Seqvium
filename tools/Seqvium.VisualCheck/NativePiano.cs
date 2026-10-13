// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class NativePointer
{
    internal static async Task<bool> ComposePianoAsync(MainWindow window, PianoRollPaneContent pane,
        DesktopWorkflow workflow, string output)
    {
        if (!OperatingSystem.IsWindows()) return false;
        nint handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        bool down = false;
        var held = new HashSet<ushort>();
        var passed = new List<string>();
        var canvas = pane.Canvas;

        void Progress(string result, string? reason = null) => Program.Json(output, "native-piano",
            new { Result = result, ForegroundGuard = true, Passed = passed, Reason = reason });

        void Guard()
        {
            if (handle == 0 || GetForegroundWindow() != handle) throw new ForegroundLostException();
        }

        void Move(Control control, Point point)
        {
            Guard();
            var screen = control.PointToScreen(point);
            Program.Require(SetCursorPos(screen.X, screen.Y), "Piano SetCursorPos failed.");
        }

        void Button(bool press)
        {
            Guard();
            Program.Require(
                SendInput(1, [new Input { Type = 0, Data = new() { Mouse = new() { Flags = press ? 2u : 4u } } }],
                    Marshal.SizeOf<Input>()) == 1, "Piano mouse input failed.");
            down = press;
        }

        void Key(ushort key, bool press)
        {
            Guard();
            Program.Require(
                SendInput(1,
                    [new Input { Type = 1, Data = new() { Keyboard = new() { Key = key, Flags = press ? 0u : 2u } } }],
                    Marshal.SizeOf<Input>()) == 1, "Piano key input failed.");
            if (press) held.Add(key);
            else held.Remove(key);
        }

        async Task Drag(Point from, Point to)
        {
            Move(canvas, from);
            Button(true);
            await Task.Delay(35);
            Move(canvas, to);
            await Task.Delay(55);
            Button(false);
            await Task.Delay(80);
            await pane.PendingCommand;
            pane.Refresh();
            window.UpdateLayout();
            Guard();
        }

        async Task Draw(decimal quarter, int pitch, decimal duration)
        {
            if (canvas.Y(pitch) < canvas.RulerHeight + 4)
                canvas.SetView(canvas.OffsetX, canvas.OffsetY - (canvas.RulerHeight + 12 - canvas.Y(pitch)));
            if (canvas.Y(pitch) + canvas.RowHeight > canvas.NoteBottom)
                canvas.SetView(canvas.OffsetX, canvas.OffsetY + canvas.Y(pitch) + canvas.RowHeight - canvas.NoteBottom);
            long at = (long)(quarter * MusicalPosition.TicksPerQuarter),
                length = (long)(duration * MusicalPosition.TicksPerQuarter);
            int undo = workflow.State.UndoCount;
            await Drag(new(canvas.X(at) + 2, canvas.Y(pitch) + canvas.RowHeight / 2),
                new(canvas.X(at + length), canvas.Y(pitch) + canvas.RowHeight / 2));
            Program.Require(
                workflow.State.UndoCount == undo + 1 && canvas.Part!.Notes.Any(n =>
                    n.Position.Ticks == at && n.Pitch == pitch && n.Duration.Ticks == length),
                $"Native note draw failed at {quarter}/{pitch}/{duration}: {workflow.State.Detail}");
        }

        try
        {
            Guard();
            foreach (int pitch in new[] { 60, 64, 67 }) await Draw(0, pitch, 2);
            foreach (int pitch in new[] { 57, 60, 64 }) await Draw(4, pitch, 2);
            foreach (int pitch in new[] { 55, 59, 62 }) await Draw(8, pitch, 2);
            foreach (var note in new[]
                     {
                         (2m, 72, .5m), (2.5m, 74, .5m), (3m, 76, 1m), (6m, 72, .5m), (6.5m, 71, .5m), (7m, 69, 1m),
                         (10m, 67, 1m), (11m, 71, 1m), (12m, 72, 2m), (14m, 67, 2m)
                     })
                await Draw(note.Item1, note.Item2, note.Item3);
            passed.Add("19 native notes: three chords and a melody with mixed lengths");
            Progress("running");
            // Edit each chord voice independently, through its actual time-aligned handle.
            for (int i = 0; i < 3; i++)
            {
                decimal value = new[] { .45m, .64m, .72m }[i];
                double x = canvas.X(0) + 5 + i * 8;
                var destination = new Point(x, canvas.Bounds.Height - 8 - (double)value * (canvas.VelocityHeight - 28));
                // The native pointer is quantized to screen pixels; use its actual client coordinate
                // as the independent value oracle, rather than a subpixel requested percentage.
                var resolved = canvas.PointToClient(canvas.PointToScreen(destination));
                decimal expected =
                    decimal.Round(
                        Math.Clamp((decimal)((canvas.Bounds.Height - 8 - resolved.Y) / (canvas.VelocityHeight - 28)), 0,
                            1), 2);
                await Drag(new(x, canvas.Bounds.Height - 8 - .8 * (canvas.VelocityHeight - 28)),
                    destination);
                Program.Require(
                    canvas.Part!.Notes.Where(n => n.Position.Ticks == 0).OrderBy(n => n.Pitch).ElementAt(i).Intensity ==
                    expected, "Native independent velocity failed.");
            }

            passed.Add("native chord velocities: three independent values resolved at screen pixels");
            Progress("running");
            var original = workflow.State.Snapshot;
            var first = canvas.Part!.Notes.First();
            var rect = canvas.NoteRect(first);
            var at = new Point(rect.X + 12, rect.Y + 7);
            await Drag(at, at + new Vector(canvas.QuarterWidth / 4, -canvas.RowHeight));
            Program.Require(canvas.Part!.Notes.Single(n => n.Id == first.Id).Pitch == first.Pitch + 1,
                "Native note move failed.");
            Program.Require(await workflow.UndoAsync(), "Native move Undo failed.");
            pane.Refresh();
            Key(0x11, true);
            await Drag(at, at + new Vector(canvas.QuarterWidth, 0));
            Key(0x11, false);
            Program.Require(canvas.Part!.Notes.Length == 20, "Native Ctrl-copy failed.");
            Program.Require(await workflow.UndoAsync(), "Native copy Undo failed.");
            pane.Refresh();
            var edge = new Point(rect.Right - 3, rect.Y + 7);
            await Drag(edge, edge + new Vector(canvas.QuarterWidth / 2, 0));
            Program.Require(
                canvas.Part!.Notes.Single(n => n.Id == first.Id).Duration.Ticks == first.Duration.Ticks + 480000,
                "Native resize failed.");
            Program.Require(await workflow.UndoAsync(), "Native resize Undo failed.");
            pane.Refresh();
            Move(canvas, at);
            Button(true);
            Move(canvas, at + new Vector(canvas.QuarterWidth, 0));
            await Task.Delay(40);
            Key(0x1B, true);
            Key(0x1B, false);
            await Task.Delay(40);
            Button(false);
            await Task.Delay(80);
            Program.Require(workflow.State.Snapshot == original, "Native Escape changed notes.");
            passed.Add("native move, Ctrl-copy, end resize and Escape; exact Undo restoration");
            Progress("running");
            var partPicker = Program.Field<ComboBox>(pane, "_part");
            Move(partPicker, new(partPicker.Bounds.Width / 2, partPicker.Bounds.Height / 2));
            Button(true);
            Button(false);
            await Task.Delay(80);
            Key(0x28, true);
            Key(0x28, false);
            Key(0x0D, true);
            Key(0x0D, false);
            await Task.Delay(100);
            pane.Refresh();
            canvas.Fit();
            Program.Require(canvas.Part?.Notes.IsEmpty == true, "Native channel selection failed.");
            foreach (var note in new[] { (0m, 48), (4m, 45), (8m, 43), (12m, 48) })
                await Draw(note.Item1, note.Item2, 4);
            passed.Add("native channel choice and four lower-register notes");
            Progress("passed");
            return true;
        }
        catch (ForegroundLostException)
        {
            Progress("partial", "Foreground guard refused further input");
            return false;
        }
        catch (Exception error)
        {
            Progress("failure", error.ToString());
            throw;
        }
        finally
        {
            if (handle != 0 && GetForegroundWindow() == handle)
            {
                var releases = new List<Input>();
                if (down) releases.Add(new() { Type = 0, Data = new() { Mouse = new() { Flags = 4 } } });
                foreach (ushort key in held)
                    releases.Add(new() { Type = 1, Data = new() { Keyboard = new() { Key = key, Flags = 2 } } });
                if (releases.Count > 0) SendInput((uint)releases.Count, releases.ToArray(), Marshal.SizeOf<Input>());
            }
        }
    }
}
