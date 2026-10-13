// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task StepMatrixAsync(MainWindow main, string output)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        workspace.Open(WorkspaceState.BeatId); main.UpdateLayout();
        var pane = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        await CaptureAsync(main, output, "empty");
        var create = Field<Button>(pane, "_create");
        create.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); await pane.PendingCommand;
        pane.Refresh(); Require(BeatSteps.Resolve(workflow.State).Pattern is { Parts.Length: 0 }, "Create button did not create empty canonical Pattern.");
        foreach (var (name, amplitude) in new[] { ("Kick", .4f), ("Snare", .2f), ("Hi-Hat", .1f) })
        {
            string path = Path.Combine(output, name + ".wav"); WriteStepWav(path, amplitude);
            Require(await workflow.AddBeatWavAsync(BeatSteps.Resolve(workflow.State).Basis!, path), "GUI fixture channel import failed: " + workflow.State.Detail);
        }
        pane.Refresh(); main.UpdateLayout();
        using var pointer = new Pointer(100042, PointerType.Mouse, true);
        async Task Click(ToggleButton button)
        {
            Require(button.IsEnabled, "Step button unavailable.");
            main.UpdateLayout();
            int beforeUndo = workflow.State.UndoCount;
            var point = button.TranslatePoint(new(button.Bounds.Width / 2, button.Bounds.Height / 2), main) ?? throw new InvalidOperationException("Detached step.");
            button.RaiseEvent(new PointerPressedEventArgs(button, pointer, main, point, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
            button.RaiseEvent(new PointerReleasedEventArgs(button, pointer, main, point, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            // Routed events do not update the platform pointer-over state used by Button's release
            // recognizer. Invoke the real automation action, separately from the native smoke below.
            if (workflow.State.UndoCount == beforeUndo && pane.PendingCommand.IsCompleted)
                ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(button)!).Toggle();
            await pane.PendingCommand; pane.Refresh();
            Require(workflow.State.UndoCount == beforeUndo + 1, $"Routed step did not commit: id={AutomationProperties.GetAutomationId(button)}, checked={button.IsChecked}, bounds={button.Bounds}, detail={workflow.State.Detail}.");
        }
        ToggleButton Step(int channel, int step) => pane.GetVisualDescendants().OfType<ToggleButton>().Single(b =>
            AutomationProperties.GetAutomationId(b) == $"Beat.Step.{BeatSteps.Resolve(workflow.State).Pattern!.Parts[channel].Id}.{step}");
        foreach (int step in new[] { 0, 8 }) await Click(Step(0, step));
        foreach (int step in new[] { 4, 12 }) await Click(Step(1, step));
        for (int step = 0; step < 16; step += 2) await Click(Step(2, step));
        var pattern = BeatSteps.Resolve(workflow.State).Pattern!;
        Require(pattern.Parts.Sum(p => p.Notes.Length) == 12, "Routed grid clicks lost canonical notes.");
        int undo = workflow.State.UndoCount;
        await Click(Step(0, 0)); Require(workflow.State.UndoCount == undo + 1 && Step(0, 0).IsChecked == false, "Second click did not turn step off once.");
        Require(await workflow.UndoAsync(), "Grid Undo failed."); pane.Refresh(); Require(Step(0, 0).IsChecked == true, "External Undo did not refresh.");
        Require(await workflow.UndoAsync(true), "Grid Redo failed."); pane.Refresh(); Require(Step(0, 0).IsChecked == false, "External Redo did not refresh.");
        Require(await workflow.UndoAsync(), "Grid restore failed."); pane.Refresh();
        Step(0, 0).Focus(); Step(0, 0).RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
        Require(Step(0, 1).IsFocused, "Arrow navigation lost grid focus.");
        Step(0, 1).RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Space });
        Step(0, 1).RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Space });
        await pane.PendingCommand; pane.Refresh(); Require(Step(0, 1).IsChecked == true, "Space did not toggle canonical step.");
        Require(await workflow.UndoAsync(), "Keyboard restore failed."); pane.Refresh();
        // A held mouse action must expire on external Undo even if the same logical IDs return.
        var staleButton = Step(0, 1); var stalePoint = staleButton.TranslatePoint(new(10, 10), main)!.Value;
        staleButton.RaiseEvent(new PointerPressedEventArgs(staleButton, pointer, main, stalePoint, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
        Require(await workflow.RenameBeatAsync(BeatSteps.Resolve(workflow.State).Basis!, "Drums"), "Rename fixture failed."); pane.Refresh();
        staleButton.RaiseEvent(new PointerReleasedEventArgs(staleButton, pointer, main, stalePoint, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        await pane.PendingCommand; pane.Refresh(); Require(Step(0, 1).IsChecked == false, "Stale pointer action committed.");

        string save = Path.Combine(output, "drums.seqvium"); Require(await workflow.SaveAsync(save), "GUI save failed.");
        Require(await workflow.OpenAsync(save, UnsavedDecision.Discard), "GUI Open failed."); pane.Refresh();
        Require(BeatSteps.Resolve(workflow.State).Pattern!.Id == pattern.Id && Step(0, 0).IsChecked == true, "Open lost grid identity/state.");
        var rows = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (bool reduced in new[] { false, true })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            main.Width = reduced ? 640 : 1100; main.Height = reduced ? 480 : 750;
            await Task.Delay(80); main.UpdateLayout();
            workspace.State.Dock(WorkspaceState.BeatId, PaneDock.Floating);
            workspace.State.SetBounds(WorkspaceState.BeatId, new(24, 24, reduced ? 560 : 780, 330));
            pane.Refresh(); Step(0, 0).Focus(); main.UpdateLayout();
            await CaptureAsync(main, output, $"{language}-{theme}-{(reduced ? "reduced" : "normal")}");
            var name = Field<TextBox>(pane, "_name"); var apply = Field<Button>(pane, "_apply");
            var text = name.GetVisualDescendants().OfType<TextPresenter>().Single();
            double Center(Control c) => c.TranslatePoint(default, main)!.Value.Y + c.Bounds.Height / 2;
            Require(Math.Abs(Center(text) - Center(apply)) <= 1, "Pattern name/action alignment failed.");
            var buttons = pane.GetVisualDescendants().OfType<ToggleButton>().ToArray();
            Require(buttons.Length == 48 && buttons.All(b => b.Bounds.Height >= 24 && !string.IsNullOrWhiteSpace(AutomationProperties.GetName(b))), "Grid hit area or accessible names failed.");
            var peer = ControlAutomationPeer.CreatePeerForElement(Step(0, 0));
            Require(peer is IToggleProvider { ToggleState: ToggleState.On }, "Checked step accessibility state failed.");
            var scroll = pane.GetVisualDescendants().OfType<ScrollViewer>().First();
            if (reduced) Require(scroll.Extent.Width > scroll.Viewport.Width, "Reduced pane lacks local horizontal scrolling.");
            rows.Add(new { language, theme, reduced, Steps = buttons.Length, Focus = Step(0, 0).IsFocused,
                NameAlignmentDip = Math.Abs(Center(text) - Center(apply)), scroll.Extent, scroll.Viewport });
        }
        workspace.State.Dock(WorkspaceState.BeatId, PaneDock.Left); workspace.State.Collapse(WorkspaceState.BeatId);
        workspace.Open(WorkspaceState.BeatId); workspace.State.Hide(WorkspaceState.BeatId); workspace.Open(WorkspaceState.BeatId);
        Require(main.GetVisualDescendants().OfType<BeatPaneContent>().Single() == pane, "Workspace replaced retained content.");
        workspace.State.Dock(WorkspaceState.BeatId, PaneDock.Floating);
        main.Width = 1100; main.Height = 750; workspace.State.SetBounds(WorkspaceState.BeatId, new(24, 24, 780, 330)); main.UpdateLayout();
        var native = await StepNativeInput.VerifyAsync(main, pane, workflow, Step(0, 1)); pane.Refresh();
        Json(output, "steps", new { Rows = rows, RoutedInput = "create, twelve step automation actions with synthetic pointer events, second toggle, external Undo/Redo, arrow/Space, stale held click",
            SaveOpen = "canonical identity and visible states restored", RetainedLifecycle = "dock/collapse/hide/reopen", NativeInput = native,
            AudioStarted = workflow.State.CanStop, PhysicalAudio = false });
    }
    private static void WriteStepWav(string path, float amplitude)
    {
        using var file = File.Create(path); using var writer = new BinaryWriter(file);
        writer.Write("RIFF"u8); writer.Write(36 + 1440 * 4); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((ushort)3); writer.Write((ushort)1); writer.Write(48000); writer.Write(192000); writer.Write((ushort)4); writer.Write((ushort)32);
        writer.Write("data"u8); writer.Write(1440 * 4); for (int i = 0; i < 1440; i++) writer.Write(amplitude);
    }
}

internal static class StepNativeInput
{
    internal static async Task<object> VerifyAsync(MainWindow window, BeatPaneContent pane, DesktopWorkflow workflow, ToggleButton button)
    {
        if (!OperatingSystem.IsWindows()) return new { Result = "not-run", Reason = "Windows only" };
        nint handle = window.TryGetPlatformHandle()?.Handle ?? 0;
        if (handle == 0 || GetForegroundWindow() != handle) return new { Result = "refused", Reason = "foreground guard" };
        bool held = false; var passed = new List<string>();
        bool Guard() => GetForegroundWindow() == handle;
        void Mouse(bool down)
        {
            Program.Require(Guard(), "Step native foreground changed.");
            var input = new Input { Type = 0, Data = new() { Mouse = new() { Flags = down ? 2u : 4u } } };
            Program.Require(SendInput(1, [input], Marshal.SizeOf<Input>()) == 1, "Step native mouse failed."); held = down;
        }
        try
        {
            var point = button.PointToScreen(new(button.Bounds.Width / 2, button.Bounds.Height / 2));
            Program.Require(SetCursorPos(point.X, point.Y), "Native cursor move failed.");
            int undo = workflow.State.UndoCount;
            Mouse(true); await Task.Delay(30); Mouse(false); await Task.Delay(120); await pane.PendingCommand; pane.Refresh();
            Program.Require(button.IsChecked == true && workflow.State.UndoCount == undo + 1, "Native step click failed.");
            passed.Add("native click → one canonical note / one Undo");
            Program.Require(button.IsFocused, "Native step click did not focus."); passed.Add("native grid focus");
            Program.Require(await workflow.UndoAsync(), "Native smoke restore failed."); pane.Refresh();
            return new { Result = "passed", Passed = passed, ForegroundGuard = true };
        }
        finally
        {
            if (held && Guard())
            {
                var input = new Input { Type = 0, Data = new() { Mouse = new() { Flags = 4 } } };
                SendInput(1, [input], Marshal.SizeOf<Input>());
            }
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public Union Data; }
    [StructLayout(LayoutKind.Explicit)] private struct Union { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public nuint Extra; }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, Input[] input, int size);
}
