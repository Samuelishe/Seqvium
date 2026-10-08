// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop;

/// <summary>Bounded Windows chrome adapter. Avalonia suppresses SC_KEYMENU, so Alt+Space opens the real OS menu here.</summary>
internal static class WindowsWindowMenu
{
    public static void Show(Window window)
    {
        if (!OperatingSystem.IsWindows() || window.TryGetPlatformHandle() is not { Handle: var handle }) return;
        var menu = GetSystemMenu(handle, false);
        if (menu == IntPtr.Zero) return;
        // Reflect current state: the OS normally does this when it opens its own system menu.
        var maximized = window.WindowState == WindowState.Maximized;
        SetEnabled(menu, 0xF120, maximized); // Restore
        SetEnabled(menu, 0xF010, !maximized); // Move
        SetEnabled(menu, 0xF000, !maximized && window.CanResize); // Size
        SetEnabled(menu, 0xF020, window.CanMinimize);
        SetEnabled(menu, 0xF030, !maximized && window.CanMaximize);
        SetEnabled(menu, 0xF060, true); // Close remains available for this main window.
        var position = window.PointToScreen(new Point(6, HostMetrics.Values["Chrome.Height"]));
        var command = TrackPopupMenuEx(menu, 0x0100, position.X, position.Y, handle, IntPtr.Zero);
        if (command != 0) PostMessage(handle, 0x0112, (IntPtr)command, IntPtr.Zero);
    }

    private static void SetEnabled(IntPtr menu, uint command, bool enabled) =>
        EnableMenuItem(menu, command, enabled ? 0u : 1u);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool revert);

    [DllImport("user32.dll")]
    private static extern uint EnableMenuItem(IntPtr menu, uint item, uint flags);

    [DllImport("user32.dll")]
    private static extern uint
        TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr window, IntPtr parameters);

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
