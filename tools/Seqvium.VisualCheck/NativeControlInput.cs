// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Seqvium.VisualCheck;

internal static partial class NativePointer
{
    internal sealed partial class DrumInput
    {
        internal sealed record PathSample(int Index, Point Requested, Point Delivered, double ElapsedMs,
            bool Captured, string? CaptureOwner);

        internal async Task<IReadOnlyList<PathSample>> TraceDrag(Control control, IReadOnlyList<Point> points,
            Action<int>? observe = null, bool escape = false, bool fine = false, int intervalMs = 16)
        {
            Program.Require(points.Count >= 2, "A native trajectory needs at least two points.");
            await Task.Delay(550); // Distinct gestures, outside the OS double-click interval.
            Guard();
            var start = control.PointToScreen(points[0]);
            var hit = window.InputHitTest(window.PointToClient(start)) as Visual;
            Program.Require(hit == control || hit?.GetVisualAncestors().Contains(control) == true,
                "Native path start is occluded: " + control);
            var samples = new List<PathSample>();
            Point delivered = default;
            IPointer? pointer = null;
            TaskCompletionSource? arrival = null;
            Point expected = default;
            void Moved(object? _, PointerEventArgs e)
            {
                delivered = e.GetPosition(control);
                pointer = e.Pointer;
                if (Near(delivered, expected)) arrival?.TrySetResult();
            }
            control.AddHandler(InputElement.PointerMovedEvent, Moved, RoutingStrategies.Bubble, true);
            bool down = false;
            try
            {
                Program.Require(SetCursorPos(start.X, start.Y), "Native path start failed.");
                await Task.Delay(35);
                Program.Require(GetCursorPos(out var actualStart) && actualStart.X == start.X && actualStart.Y == start.Y,
                    $"Native path cursor moved before press: expected={start}; actual={actualStart}. No press sent.");
                Program.Require(control.PointToScreen(points[0]) == start,
                    "Native control moved before press; no input sent.");
                if (fine) SendKey(0x10, true);
                Button(true);
                await Task.Delay(35);
                observe?.Invoke(0);
                for (int i = 1; i < points.Count; i++)
                {
                    Guard();
                    var screen = control.PointToScreen(points[i]);
                    expected = control.PointToClient(screen);
                    arrival = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    var watch = Stopwatch.StartNew();
                    Program.Require(SetCursorPos(screen.X, screen.Y), "Native curved movement failed.");
                    if (Near(delivered, expected)) arrival.TrySetResult();
                    try { await arrival.Task.WaitAsync(TimeSpan.FromMilliseconds(250)); }
                    catch (TimeoutException error)
                    {
                        GetCursorPos(out var actual);
                        throw new InvalidOperationException($"Native movement timeout: {control.GetType().Name}; sample={i}; " +
                            $"requested={points[i]}; expected={expected}; delivered={delivered}; capture={pointer?.Captured}; " +
                            $"screen={screen}; actualCursor={actual}; currentTarget={control.PointToScreen(points[i])}; " +
                            $"bounds={control.Bounds}; enabled={control.IsEnabled}", error);
                    }
                    samples.Add(new(i, points[i], delivered, watch.Elapsed.TotalMilliseconds,
                        pointer?.Captured is not null, pointer?.Captured?.GetType().Name));
                    Program.Require(Math.Abs(delivered.X - points[i].X) <= 2 && Math.Abs(delivered.Y - points[i].Y) <= 2,
                        $"Native movement not delivered at sample {i}: requested {points[i]}, observed {delivered}");
                    observe?.Invoke(i);
                    await Task.Delay(intervalMs);
                }
                if (escape) { SendKey(0x1B, true); SendKey(0x1B, false); await Task.Delay(30); }
            }
            finally
            {
                if (GetForegroundWindow() == Handle)
                {
                    if (down) Button(false);
                    if (fine) SendKey(0x10, false);
                }
                control.RemoveHandler(InputElement.PointerMovedEvent, Moved);
            }
            await Task.Delay(70);
            return samples;

            static bool Near(Point a, Point b) => Math.Abs(a.X - b.X) <= 1 && Math.Abs(a.Y - b.Y) <= 1;

            void Button(bool press)
            {
                Guard();
                Program.Require(SendInput(1, [new() { Type = 0, Data = new() { Mouse = new() { Flags = press ? 2u : 4u } } }],
                    Marshal.SizeOf<Input>()) == 1, "Native path button failed.");
                down = press;
            }
            void SendKey(ushort key, bool press)
            {
                Guard();
                Program.Require(SendInput(1, [new() { Type = 1, Data = new() { Keyboard = new() { Key = key, Flags = press ? 0u : 2u } } }],
                    Marshal.SizeOf<Input>()) == 1, "Native path key failed.");
            }
        }
    }
}
