// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Settings;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task SettingsAsync(MainWindow main, string output, string project, bool native)
    {
        var shell = Field<ShellSession>(main, "_session");
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        await Task.Delay(150);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900;
        if (native)
        {
            main.Activate(); await Task.Delay(200);
        }
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Settings project open failed.");
        var snapshot = workflow.State.Snapshot;
        var checks = new List<string>();
        Button Find(Control root, string id) => root.GetVisualDescendants().OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == id);
        async Task Click(Window window, string id)
        {
            var button = Find(window, id);
            button.BringIntoView(); await Task.Delay(60);
            if (native) await NativePointer.SettingsClickAsync(window, button);
            else button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(80);
        }
        async Task<SettingsWindow> Open()
        {
            Find(main, "Settings.Open").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(140);
            return Field<SettingsWindow>(main, "_settings");
        }
        var settings = await Open();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (bool reduced in new[] { false, true })
        {
            main.Width = reduced ? 640 : 1400; main.Height = reduced ? 480 : 900;
            settings.Width = reduced ? 500 : 720; settings.Height = reduced ? 340 : 570;
            var languageControl = Field<ComboBox>(settings, "_language");
            var themeControl = Field<ComboBox>(settings, "_theme");
            await Click(settings, "Settings.General");
            languageControl.SelectedIndex = (int)language; await settings.PendingChange;
            themeControl.SelectedIndex = ThemeCatalog.All.FindIndex(item => item.Theme == theme); await settings.PendingChange;
            Require(shell.Preferences == new HostPreferences(language, theme), "Appearance UI did not apply.");
            foreach (string page in new[] { "General", "Controls", "About" })
            {
                await Click(settings, "Settings." + page);
                await CaptureAsync(settings, output, $"settings-{language}-{theme}-{reduced}-{page}");
                Require(settings.Bounds.Width >= 500 && settings.Bounds.Height >= 340, "Unusable settings minimum.");
                Require(Find(settings, "Settings.Close").Bounds.Height > 0, "Close became unreachable.");
                if (page == "Controls" && reduced)
                {
                    var fixedKeys = Field<Expander>(settings, "_fixedKeys");
                    fixedKeys.IsExpanded = true;
                    await CaptureAsync(settings, output, $"minimum-details-{language}-{theme}");
                    await Click(settings, "Shortcut.New");
                    Require(!fixedKeys.IsExpanded, "Capture and details compete at minimum size.");
                    await CaptureAsync(settings, output, $"minimum-capture-{language}-{theme}");
                    var assign = Find(settings, "Settings.Assign");
                    var close = Find(settings, "Settings.Close");
                    Require(assign.TranslatePoint(new(0, assign.Bounds.Height), settings)!.Value.Y <=
                        close.TranslatePoint(default, settings)!.Value.Y, "Minimum capture action overlaps the footer.");
                    await Click(settings, "Settings.CancelCapture");
                }
            }
        }
        checks.Add("RU/EN x Dark/Light x ordinary/reduced: three pages, real windows, retained controls");
        settings.Width = 720; settings.Height = 570;
        await Click(settings, "Settings.Controls");
        Require(!settings.GetVisualDescendants().OfType<Button>().Any(button =>
            AutomationProperties.GetAutomationId(button)?.StartsWith("Shortcut.Reset.", StringComparison.Ordinal) == true),
            "Default bindings expose individual resets.");
        await Click(settings, "Shortcut.Browser");
        void Chord(Control target, Key key, KeyModifiers modifiers) => target.RaiseEvent(new KeyEventArgs
            { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers });
        void CaptureChord(Key key, ushort vk, KeyModifiers modifiers = KeyModifiers.Control)
        {
            if (native) NativePointer.SettingsKey(settings, vk, 0x11);
            else Chord(settings, key, modifiers);
        }
        CaptureChord(Key.H, 0x48);
        await Task.Delay(100); await Click(settings, "Settings.Assign"); await settings.PendingChange;
        Require(HostCommands.Resolve(shell.Controls, Key.H, KeyModifiers.Control, false) == "Browser", "Binding UI failed.");
        Require(Find(settings, "Shortcut.Reset.Browser").IsEffectivelyVisible, "Custom binding has no reset.");
        await CaptureAsync(settings, output, "custom-binding");
        await Click(settings, "Shortcut.Graph"); CaptureChord(Key.H, 0x48); await Task.Delay(80);
        Require(Find(settings, "Settings.Assign").Content?.ToString() == shell["Settings.Replace"], "Conflict missing.");
        await CaptureAsync(settings, output, "conflict");
        await Click(settings, "Settings.Assign"); await settings.PendingChange;
        Require(shell.Controls.Binding("Browser") is null, "Conflict did not release prior owner.");
        await Click(settings, "Shortcut.Reset.Graph"); await Click(settings, "Settings.Assign"); await settings.PendingChange;
        await Click(settings, "Shortcut.Reset.Browser"); await Click(settings, "Settings.Assign"); await settings.PendingChange;
        await Click(settings, "Shortcut.Browser"); CaptureChord(Key.H, 0x48);
        await Task.Delay(80); await Click(settings, "Settings.Assign"); await settings.PendingChange;
        // Clearing, reserved keys, cancel, filtering and scoped reset remain real UI actions.
        await Click(settings, "Shortcut.Import"); Chord(settings, Key.Delete, KeyModifiers.None);
        await Click(settings, "Settings.Assign"); await settings.PendingChange;
        Require(shell.Controls.Binding("Import") is null, "Clear did not remove the shortcut.");
        await Click(settings, "Shortcut.Open"); Chord(settings, Key.C, KeyModifiers.Control);
        Require(!Find(settings, "Settings.Assign").IsEnabled, "Reserved clipboard chord accepted.");
        Chord(settings, Key.Escape, KeyModifiers.None);
        Require(!Field<StackPanel>(settings, "_capture").IsVisible, "Escape failed to cancel capture.");
        var search = Field<TextBox>(settings, "_search"); search.Text = "impossible-command";
        await Task.Delay(100);
        Require(!settings.GetVisualDescendants().OfType<Button>().Any(b => AutomationProperties.GetAutomationId(b) == "Shortcut.Open"), "Filter failed.");
        search.Text = "";
        await Task.Delay(100);
        var auto = Field<CheckBox>(settings, "_auto"); auto.IsChecked = true; await settings.PendingChange;
        await Click(settings, "Settings.Close"); await Task.Delay(140);
        workspace.State.Hide(WorkspaceState.BrowserId);
        var old = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.B, KeyModifiers = KeyModifiers.Control };
        main.RaiseEvent(old);
        Require(!old.Handled && workspace.State.Get(WorkspaceState.BrowserId).Visibility == PaneVisibility.Hidden, "Old binding still active.");
        if (native) { NativePointer.SettingsKey(main, 0x48, 0x11); await Task.Delay(200); }
        else Chord(main, Key.H, KeyModifiers.Control);
        Require(workspace.State.Get(WorkspaceState.BrowserId).Visibility == PaneVisibility.Visible, "New chord did not dispatch Browser.");
        var browser = main.GetVisualDescendants().OfType<BrowserPaneContent>().Single();
        Require(Field<CheckBox>(browser, "_auto").IsChecked == true, "Browser did not consume global behavior.");
        var text = Field<TextBox>(browser, "_search"); text.Focus(); text.Text = "hello";
        var localUndo = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Z, KeyModifiers = KeyModifiers.Control };
        text.RaiseEvent(localUndo);
        Require(workflow.State.Snapshot == snapshot && workflow.State.UndoCount == 0, "Text Undo edited music.");
        checks.Add("capture, transfer conflict, clear, per-command default, reserved chord, cancel, filter, dispatch, old chord removal, Browser preference, text focus");
        await shell.PendingControls;
        var loaded = await new ControlPreferencesStore(Path.Combine(Field<App>(main, "_app").ConfigurationDirectory, "controls.json")).LoadAsync();
        Require(!loaded.UsedFallback && loaded.Preferences.AutoPreview &&
            HostCommands.Resolve(loaded.Preferences, Key.H, KeyModifiers.Control, false) == "Browser", "Reload failed.");
        settings = await Open(); await Click(settings, "Settings.Controls");
        await Click(settings, "Settings.ResetAll"); await settings.PendingChange;
        Require(shell.Controls.AutoPreview && shell.Controls.Overrides is null, "Keyboard reset escaped its scope.");
        Require(!settings.GetVisualDescendants().OfType<Button>().Any(button =>
            AutomationProperties.GetAutomationId(button)?.StartsWith("Shortcut.Reset.", StringComparison.Ordinal) == true),
            "Reset left customized indicators.");
        await Click(settings, "Settings.Close");
        Require(workflow.State.Snapshot == snapshot && !workflow.State.IsDirty, "Preferences changed the document.");
        checks.Add("file reload, reopen, keyboard-only reset, musical snapshot/history preserved");
        main.Width = 900; main.Height = 600;
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            await shell.SetLanguageAsync(language);
            var themeButton = main.FindControl<Button>("ThemeAction")!;
            Require(AutomationProperties.GetName(themeButton)?.StartsWith(shell["Settings.SelectTheme"], StringComparison.Ordinal) == true,
                "Theme action does not announce selection.");
            if (native) await NativePointer.SettingsClickAsync(main, themeButton);
            else themeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var menu = Field<MenuFlyout>(main, "_themeMenu");
            Require(menu.Items.Count == ThemeCatalog.All.Count, "Theme menu assumes a binary toggle.");
            var item = menu.Items.OfType<MenuItem>().Single(i => i.Header?.ToString() == shell[
                ThemeCatalog.All.Single(t => t.Theme == theme).LabelKey]);
            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.Hide(); await Field<Task>(main, "_preferenceWork");
            Require(shell.Preferences.Theme == theme, "Explicit theme selection failed.");
            await CaptureAsync(main, output, $"chrome-{language}-{theme}");
        }
        checks.Add("explicit catalog theme menu from title-row sun/moon; RU/EN Dark/Light reduced populated workspace");
        Json(output, "settings", new { NativeInput = native, Checks = checks, PhysicalAudio = false, Version = shell.Version });
    }
}

internal static partial class NativePointer
{
    internal static async Task SettingsClickAsync(Window window, Control control)
    {
        nint handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        Program.Require(handle != 0 && GetForegroundWindow() == handle, "Settings native foreground refused.");
        var at = control.PointToScreen(new(control.Bounds.Width / 2, control.Bounds.Height / 2));
        var hit = window.InputHitTest(window.PointToClient(at)) as Visual;
        Program.Require(hit is not null && (hit == control || hit.GetVisualAncestors().Contains(control)) &&
            GetAncestor(WindowFromPoint(new(at.X, at.Y)), 2) == handle, "Settings native target covered.");
        Program.Require(SetCursorPos(at.X, at.Y), "Settings cursor failed.");
        Program.Require(SendInput(2, [new() { Type = 0, Data = new() { Mouse = new() { Flags = 2 } } },
            new() { Type = 0, Data = new() { Mouse = new() { Flags = 4 } } }], System.Runtime.InteropServices.Marshal.SizeOf<Input>()) == 2,
            "Settings click failed.");
        await Task.Delay(120);
    }

    internal static void SettingsKey(Window window, ushort key, ushort modifier)
    {
        nint handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        Program.Require(handle != 0 && GetForegroundWindow() == handle, "Settings native keyboard foreground refused.");
        Input[] inputs = [new() { Type = 1, Data = new() { Keyboard = new() { Key = modifier } } },
            new() { Type = 1, Data = new() { Keyboard = new() { Key = key } } },
            new() { Type = 1, Data = new() { Keyboard = new() { Key = key, Flags = 2 } } },
            new() { Type = 1, Data = new() { Keyboard = new() { Key = modifier, Flags = 2 } } }];
        Program.Require(SendInput(4, inputs, System.Runtime.InteropServices.Marshal.SizeOf<Input>()) == 4, "Settings key failed.");
    }
}
