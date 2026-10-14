// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Seqvium.Desktop;

namespace Seqvium.VisualCheck;

internal static partial class NativePointer
{
    /// <summary>Native musical authoring. Input stops on foreground loss, including an unrelated window in this process.</summary>
    internal sealed class DrumInput(MainWindow window)
    {
        private nint Handle => window.TryGetPlatformHandle()?.Handle ?? 0;

        private void Guard(bool dialog = false)
        {
            nint foreground = GetForegroundWindow();
            if (Handle == 0 || (foreground != Handle && (!dialog || GetAncestor(foreground, 3) != Handle)))
                throw new ForegroundLostException();
        }

        internal async Task Click(Control control, Point? point = null, bool allowOwnedDialog = false)
        {
            await Task.Delay(80); // Input hit testing uses the rendered scene after a layout publication.
            Guard();
            Program.Require(control.IsEffectivelyVisible && control.IsEnabled, "Native target unavailable: " + control);
            var screen = control.PointToScreen(point ?? new(control.Bounds.Width / 2, control.Bounds.Height / 2));
            var client = window.PointToClient(screen);
            if (TopLevel.GetTopLevel(control) == window)
            {
                var hit = window.InputHitTest(client) as Visual;
                Program.Require(hit is not null && (hit == control || hit.GetVisualAncestors().Contains(control)),
                    "Native target occluded: " + Avalonia.Automation.AutomationProperties.GetAutomationId(control) +
                    " at " + client + " hit " + hit);
            }

            Program.Require(SetCursorPos(screen.X, screen.Y), "Native drum cursor failed.");
            Program.Require(
                SendInput(1, [new() { Type = 0, Data = new() { Mouse = new() { Flags = 2 } } }],
                    Marshal.SizeOf<Input>()) == 1,
                "Native drum press failed.");
            await Task.Delay(40);
            Guard(allowOwnedDialog);
            SendInput(1, [new() { Type = 0, Data = new() { Mouse = new() { Flags = 4 } } }], Marshal.SizeOf<Input>());
            await Task.Delay(150);
        }

        internal void Key(ushort key, bool dialog = false, ushort modifier = 0)
        {
            Guard(dialog);
            var inputs = new List<Input>();
            if (modifier != 0) inputs.Add(new() { Type = 1, Data = new() { Keyboard = new() { Key = modifier } } });
            inputs.Add(new() { Type = 1, Data = new() { Keyboard = new() { Key = key } } });
            inputs.Add(new() { Type = 1, Data = new() { Keyboard = new() { Key = key, Flags = 2 } } });
            if (modifier != 0)
                inputs.Add(new() { Type = 1, Data = new() { Keyboard = new() { Key = modifier, Flags = 2 } } });
            Program.Require(SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<Input>()) == inputs.Count,
                "Native drum key failed.");
        }

        private void Text(string value, bool dialog = false)
        {
            Guard(dialog);
            var inputs = value.SelectMany(c => new Input[]
            {
                new() { Type = 1, Data = new() { Keyboard = new() { Scan = c, Flags = 4 } } },
                new() { Type = 1, Data = new() { Keyboard = new() { Scan = c, Flags = 6 } } }
            }).ToArray();
            Program.Require(SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length,
                "Native drum text failed.");
        }

        internal async Task Exact(Control control, string text)
        {
            await Click(control);
            Key(0x41, modifier: 0x11);
            await Task.Delay(40);
            Text(text);
            await Task.Delay(40);
            Key(0x0D);
            await Task.Delay(80);
        }

        internal async Task Select(ComboBox combo, int index)
        {
            await Click(combo, new(combo.Bounds.Width - 16, combo.Bounds.Height / 2));
            if (combo.Items.Count > 12)
            {
                Key(0x24);
                await Task.Delay(60);
                for (int i = 0; i < index; i++)
                {
                    Key(0x28);
                    await Task.Delay(12);
                }

                Key(0x0D);
                await Task.Delay(100);
                Program.Require(combo.SelectedIndex == index, "Native long selector failed.");
                return;
            }

            for (int attempt = 0; attempt < 50 && combo.ContainerFromIndex(index) is null; attempt++)
                await Task.Delay(30);
            var item = combo.ContainerFromIndex(index) ??
                       throw new InvalidOperationException("Native combo did not open.");
            await Click(item);
            await Task.Delay(100);
        }

        internal async Task Drag(Control control, Point from, Point to)
        {
            Guard();
            var screen = control.PointToScreen(from);
            Program.Require(SetCursorPos(screen.X, screen.Y), "Native drum drag start failed.");
            SendInput(1, [new() { Type = 0, Data = new() { Mouse = new() { Flags = 2 } } }], Marshal.SizeOf<Input>());
            try
            {
                await Task.Delay(40);
                Guard();
                screen = control.PointToScreen(to);
                Program.Require(SetCursorPos(screen.X, screen.Y), "Native drum drag failed.");
                await Task.Delay(60);
            }
            finally
            {
                if (GetForegroundWindow() == Handle)
                    SendInput(1, [new() { Type = 0, Data = new() { Mouse = new() { Flags = 4 } } }],
                        Marshal.SizeOf<Input>());
            }

            await Task.Delay(80);
        }

        internal async Task Wheel(Control control, Point point, int delta, bool horizontal = false, bool zoom = false)
        {
            Guard();
            var screen = control.PointToScreen(point);
            Program.Require(SetCursorPos(screen.X, screen.Y), "Native wheel cursor failed.");
            if (zoom)
                SendInput(1, [new() { Type = 1, Data = new() { Keyboard = new() { Key = 0x11 } } }],
                    Marshal.SizeOf<Input>());
            SendInput(1,
            [
                new()
                {
                    Type = 0,
                    Data = new()
                        { Mouse = new() { Flags = horizontal ? 0x1000u : 0x800u, Data = unchecked((uint)delta) } }
                }
            ], Marshal.SizeOf<Input>());
            if (zoom)
                SendInput(1, [new() { Type = 1, Data = new() { Keyboard = new() { Key = 0x11, Flags = 2 } } }],
                    Marshal.SizeOf<Input>());
            await Task.Delay(100);
        }

        internal async Task FileDialog(string path)
        {
            path = Path.GetFullPath(path); // Native File name accepts Windows separators, unlike the managed APIs.
            for (int i = 0; i < 50 && GetForegroundWindow() == Handle; i++) await Task.Delay(40);
            Guard(true);
            Program.Require(GetForegroundWindow() != Handle, "Native file dialog did not open.");
            Key(0x4E, true, 0x12); // Common file dialog: focus File name.
            Key(0x41, true, 0x11);
            Text(path, true);
            Key(0x0D, true);
            for (int i = 0; i < 100 && GetForegroundWindow() != Handle; i++) await Task.Delay(40);
            Guard();
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint window, uint flags);
}
