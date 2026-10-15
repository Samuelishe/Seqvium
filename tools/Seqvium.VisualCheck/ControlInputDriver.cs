// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Seqvium.Desktop;

namespace Seqvium.VisualCheck;

/// <summary>The explicit routed mode is fallback evidence; it never reports Windows input or native latency.</summary>
internal sealed class ControlInputDriver(MainWindow window, bool routed, string output)
{
    private readonly NativePointer.DrumInput _native = new(window);
    public Task ActivateVisibleWindow() => routed ? Task.CompletedTask : _native.ActivateVisibleWindow();
    public Task RapidClick(Control control) => routed ? Click(control) : _native.Click(control, beforeMs: 0, afterMs: 0);

    public async Task Click(Control control)
    {
        if (!routed)
        {
            for (int i = 0; i < 100 && !Ready(); i++)
            {
                window.UpdateLayout();
                await Task.Delay(20);
            }
            if (!Ready()) await Program.CaptureAsync(window, output, "unready-native-target");
            await _native.Click(control);
            return;

            bool Ready()
            {
                var root = TopLevel.GetTopLevel(control);
                if (root is null || !control.IsEffectivelyVisible || !control.IsEnabled) return false;
                if (root != window) return control.Bounds.Width > 0 && control.Bounds.Height > 0;
                var at = control.TranslatePoint(new(control.Bounds.Width / 2, control.Bounds.Height / 2), window);
                var hit = at is { } point ? window.InputHitTest(point) as Visual : null;
                return hit == control || hit?.GetVisualAncestors().Contains(control) == true;
            }
        }
        if (control is TextBox) control.Focus();
        else if (control is Button button)
        {
            button.Focus();
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        else if (control is MenuItem item) item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        else throw new InvalidOperationException("Unsupported routed action: " + control);
        await Task.Delay(20);
        window.UpdateLayout();
    }

    public void Key(ushort key, ushort modifier = 0)
    {
        if (!routed) { _native.Key(key, modifier: modifier); return; }
        Program.Require(key == 0x5A && modifier == 0x11, "Only project Undo is a routed driver key.");
        window.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Z,
            KeyModifiers = KeyModifiers.Control });
    }
    public async Task Escape(Control target)
    {
        if (!routed) _native.Key(0x1B);
        else target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Escape });
        await Task.Delay(40);
    }

    public Task Drag(Control control, Point from, Point to) => routed
        ? TraceDrag(control, [from, to]) : _native.Drag(control, from, to);

    public async Task<IReadOnlyList<NativePointer.DrumInput.PathSample>> TraceDrag(Control control,
        IReadOnlyList<Point> points, Action<int>? observe = null, bool escape = false, bool fine = false, int intervalMs = 16,
        bool captureLoss = false)
    {
        Program.Require(routed || !captureLoss, "Forced capture loss is explicitly routed evidence.");
        if (!routed)
        {
            try { return await _native.TraceDrag(control, points, observe, escape, fine, intervalMs); }
            catch { await Program.CaptureAsync(window, output, "native-path-failure"); throw; }
        }
        await Task.Delay(40); // Publish scroll/layout changes to the real rendered hit-test scene.
        using var pointer = new Pointer(917345, PointerType.Mouse, true);
        var samples = new List<NativePointer.DrumInput.PathSample>();
        var from = control.TranslatePoint(points[0], window)!.Value;
        var source = window.InputHitTest(from) as Control ?? control;
        if (source != control && !source.GetVisualAncestors().Contains(control))
        {
            Program.Json(output, "occluded-control", new { control.IsVisible, control.IsEffectivelyVisible,
                control.Bounds, From = from, Hit = source.GetType().Name,
                Id = Avalonia.Automation.AutomationProperties.GetAutomationId(control),
                Ancestors = control.GetVisualAncestors().OfType<Control>().Select(c => new { Type = c.GetType().Name, c.Bounds }) });
            await Program.CaptureAsync(window, output, "occluded-control");
        }
        Program.Require(source == control || source.GetVisualAncestors().Contains(control),
            "Routed path start is occluded: " + control + " hit " + source + " at " + from);
        var modifiers = fine ? KeyModifiers.Shift : KeyModifiers.None;
        bool ending = false;
        int currentSample = 0;
        void Lost(object? _, PointerCaptureLostEventArgs e)
        {
            if (!ending && e.Pointer == pointer) Program.Json(output, "unexpected-capture-loss", new
            {
                Sample = currentSample, control.IsEnabled, Control = control.GetType().Name,
                State = Program.Field<Seqvium.Desktop.Workflow.DesktopWorkflow>(window, "_workflow").State.StatusKey,
                Stack = Environment.StackTrace
            });
        }
        control.PointerCaptureLost += Lost;
        source.RaiseEvent(new PointerPressedEventArgs(source, pointer, window, from, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), modifiers, 1));
        Program.Require(pointer.Captured == control, "Routed press was not accepted/captured: " + control + "; enabled=" + control.IsEnabled);
        observe?.Invoke(0);
        for (int i = 1; i < points.Count; i++)
        {
            currentSample = i;
            var target = pointer.Captured as Control ?? source;
            target.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, target, pointer, window,
                control.TranslatePoint(points[i], window)!.Value, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), modifiers));
            samples.Add(new(i, points[i], points[i], 0, pointer.Captured is not null, pointer.Captured?.GetType().Name));
            observe?.Invoke(i);
            await Task.Delay(2);
        }
        ending = true;
        if (escape) control.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Escape });
        if (captureLoss) pointer.Capture(null);
        (pointer.Captured as Control ?? source).RaiseEvent(new PointerReleasedEventArgs(source, pointer, window,
            control.TranslatePoint(points[^1], window)!.Value, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), modifiers, MouseButton.Left));
        await Task.Delay(20);
        control.PointerCaptureLost -= Lost;
        return samples;
    }
}
