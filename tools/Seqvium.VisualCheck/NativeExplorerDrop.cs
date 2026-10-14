// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;

namespace Seqvium.VisualCheck;

internal static partial class NativePointer
{
    /// <summary>Real Shell drag from a verified selected file in a task-owned Explorer folder.
    /// No clipboard, routed Drop event or application import API supplies the media.</summary>
    internal static async Task ExplorerDropAsync(string file, Control target, Point point, string output)
    {
        if (!OperatingSystem.IsWindows()) throw new NotSupportedException("Explorer verification requires Windows.");
        file = Path.GetFullPath(file);
        var top = TopLevel.GetTopLevel(target) ?? throw new InvalidOperationException("Drop target detached.");
        nint destination = top.TryGetPlatformHandle()?.Handle ?? 0;
        Process.Start(new ProcessStartInfo("explorer.exe", "/n,/select,\"" + file + "\"") { UseShellExecute = true });
        nint source = 0;
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!)!;
        try
        {
            for (int attempt = 0; attempt < 100 && source == 0; attempt++)
            {
                dynamic windows = shell.Windows();
                try
                {
                    for (int i = 0; i < windows.Count; i++)
                    {
                        dynamic browser = windows.Item(i);
                        try
                        {
                            if (!string.Equals((string)browser.LocationURL,
                                    new Uri(Path.GetDirectoryName(file)!).AbsoluteUri,
                                    StringComparison.OrdinalIgnoreCase)) continue;
                            dynamic document = browser.Document;
                            dynamic selected = document.SelectedItems();
                            try
                            {
                                if (selected.Count == 1)
                                {
                                    dynamic item = selected.Item(0);
                                    try
                                    {
                                        if (string.Equals((string)item.Path, file, StringComparison.OrdinalIgnoreCase))
                                            source = (nint)(long)browser.HWND;
                                    }
                                    finally
                                    {
                                        Marshal.ReleaseComObject(item);
                                    }
                                }
                            }
                            finally
                            {
                                Marshal.ReleaseComObject(selected);
                                Marshal.ReleaseComObject(document);
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(browser);
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(windows);
                }

                if (source == 0) await Task.Delay(40);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(shell);
        }

        Program.Require(source != 0, "Task Explorer did not select the expected source file.");
        var screen = target.PointToScreen(point);
        Program.Require(MoveWindow(source, Math.Max(0, screen.X + 350), 80, 700, 420, true),
            "Task Explorer placement failed.");
        await Task.Delay(150);
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in new[]
                 {
                     "-NoProfile", "-WindowStyle", "Hidden", "-ExecutionPolicy", "Bypass", "-File",
                     Path.GetFullPath("tools/Seqvium.VisualCheck/Read-NativeElements.ps1"), "-WindowHandle",
                     source.ToString()
                 }) start.ArgumentList.Add(argument);
        using var reader = Process.Start(start) ?? throw new IOException("Explorer UI reader did not start.");
        string text = await reader.StandardOutput.ReadToEndAsync();
        await reader.WaitForExitAsync();
        Program.Require(reader.ExitCode == 0,
            "Explorer UI reader failed: " + await reader.StandardError.ReadToEndAsync());
        using var elements = JsonDocument.Parse(text);
        var itemElement = elements.RootElement.EnumerateArray().Where(e =>
                e.GetProperty("Name").GetString() is { } name &&
                (name == Path.GetFileName(file) || name == Path.GetFileNameWithoutExtension(file)) &&
                e.GetProperty("Type").GetString() is "ControlType.ListItem" or "ControlType.DataItem")
            .OrderBy(e => e.GetProperty("Width").GetDouble()).FirstOrDefault();
        Program.Require(itemElement.ValueKind != JsonValueKind.Undefined,
            "Explorer source item rectangle unavailable.");
        int x = (int)itemElement.GetProperty("X").GetDouble() + 45;
        int y = (int)(itemElement.GetProperty("Y").GetDouble() + itemElement.GetProperty("Height").GetDouble() / 2);
        Program.Json(output, "explorer-drop-source",
            new { File = file, Source = source.ToInt64(), X = x, Y = y, Target = screen, SelectedPathVerified = true });
        Program.Require(GetForegroundWindow() == source, "Explorer drag foreground refused.");
        NativeScreen.CaptureVerifiedForeground(source, Path.Combine(output, "explorer-source.png"));
        Program.Require(SetCursorPos(x, y), "Explorer source pointer failed.");
        Program.Require(
            SendInput(1, [new() { Type = 0, Data = new() { Mouse = new() { Flags = 2 } } }], Marshal.SizeOf<Input>()) ==
            1, "Explorer press failed.");
        try
        {
            await Task.Delay(80);
            for (int i = 1; i <= 20; i++)
            {
                Program.Require(GetForegroundWindow() == source || GetForegroundWindow() == destination,
                    "Explorer drag lost an authorized surface.");
                SetCursorPos(x + (screen.X - x) * i / 20, y + (screen.Y - y) * i / 20);
                await Task.Delay(20);
            }

            await Task.Delay(150);
        }
        finally
        {
            if (GetForegroundWindow() == source || GetForegroundWindow() == destination)
                SendInput(1, [new() { Type = 0, Data = new() { Mouse = new() { Flags = 4 } } }],
                    Marshal.SizeOf<Input>());
        }

        await Task.Delay(200);
        if (GetForegroundWindow() == source) PostMessage(source, 0x0010, 0, 0);
        await Task.Delay(100);
    }

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(nint window, int x, int y, int width, int height, bool repaint);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint window, uint message, nint wparam, nint lparam);
}
