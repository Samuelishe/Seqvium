// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task RackWorkflowAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        var controls = Field<WorkflowControls>(main, "_workflowControls");
        var receipts = new List<string>();
        var matrix = new List<object>();
        main.WindowState = WindowState.Normal;
        main.Width = 1400;
        main.Height = 900;
        await Task.Delay(150);
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Pattern Pattern() => BeatSteps.Resolve(workflow.State).Pattern!;
        Require(
            await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
                "Rack workflow"), "Create failed.");
        Require(
            await workflow.AddDrumVoiceAsync(Basis(), "Snare",
                DrumInstruments.All.Single(i => i.Key == "Snare").Algorithm), "Snare failed.");
        var original = Pattern().Parts.Single();
        foreach (int step in new[] { 0, 4, 10 })
            Require(await workflow.ToggleBeatStepAsync(Basis(), original.Id, step), "Step failed.");
        workspace.State.SetBounds(WorkspaceState.BeatId, new(5, 5, 850, 390));
        workspace.Open(WorkspaceState.BeatId);
        workspace.Commit();
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        var driver = new ControlInputDriver(main, !native, output);
        var nativeDriver = new NativePointer.DrumInput(main);

        async Task Refresh()
        {
            await rack.PendingCommand;
            await controls.PendingGainCommand;
            await workflow.ObserveAsync();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            rack.Refresh();
            controls.Refresh();
            main.UpdateLayout();
            await Task.Delay(40);
        }

        Control Id(string id) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);

        Control Name(Id<MusicalPart> part) => Id("Beat.Name." + part);

        async Task MenuDuplicate(Id<MusicalPart> part)
        {
            await driver.Click(Id("Beat.Actions." + part));
            var menu = ((Button)Id("Beat.Actions." + part)).ContextMenu!;
            var action = menu.Items.OfType<MenuItem>().Single(i =>
                AutomationProperties.GetAutomationId(i) == "Beat.DuplicateChannel." + part);
            var clicked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            action.Click += (_, _) => clicked.TrySetResult();
            await driver.Click(action);
            await clicked.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Refresh();
        }

        async Task DragChannel(Id<MusicalPart> part, Id<MusicalPart> target, bool after, bool copy = false,
            bool escape = false)
        {
            var handle = Name(part);
            var other = Name(target);
            var start = new Point(handle.Bounds.Width / 2, handle.Bounds.Height / 2);
            var end = other.TranslatePoint(new(other.Bounds.Width / 2, after ? other.Bounds.Height + 5 : -5), handle)!
                .Value;
            if (native && copy) await nativeDriver.CtrlDrag(handle, start, end);
            else if (copy) await RoutedChannelDrag(main, handle, start, end, KeyModifiers.Control);
            else await driver.TraceDrag(handle, [start, end], escape: escape);
            await Refresh();
        }

        try
        {
            await Refresh();
            if (native)
            {
                main.Activate();
                await nativeDriver.WaitForForegroundAsync(TimeSpan.FromSeconds(10));
            }

            await driver.ActivateVisibleWindow();
            int history = workflow.State.UndoCount;
            await MenuDuplicate(original.Id);
            Require(Pattern().Parts.Length == 2 && workflow.State.UndoCount == history + 1, "Menu Duplicate failed.");
            var copy = Pattern().Parts[1];
            Require(copy.SoundId != original.SoundId && copy.Name == "Snare 2", "Duplicate was not independent.");
            receipts.Add("menu-independent-duplicate-one-undo");
            await DragChannel(copy.Id, original.Id, false);
            Require(Pattern().Parts[0].Id == copy.Id, "Name drag did not reorder.");
            receipts.Add("name-reorder");
            var beforeCancel = workflow.State.Snapshot;
            await DragChannel(copy.Id, original.Id, true, escape: true);
            Require(workflow.State.Snapshot == beforeCancel, "Escape committed channel drag.");
            receipts.Add("channel-escape");
            await DragChannel(original.Id, copy.Id, false, copy: true);
            Require(Pattern().Parts.Length == 3 && Pattern().Parts[0].SoundId != original.SoundId,
                "Ctrl drag did not duplicate at insertion.");
            receipts.Add("ctrl-drag-independent-copy");
            await driver.Click(Id("Workflow.Undo"));
            await Refresh();
            Require(Pattern().Parts.Length == 2, "Duplicate Undo failed.");
            await driver.Click(Id("Workflow.Redo"));
            await Refresh();
            Require(Pattern().Parts.Length == 3, "Duplicate Redo failed.");
            receipts.Add("native-history");
            foreach (string kind in new[] { "micro", "capture-loss", "outside", "stale", "aba" })
            {
                var handle = Name(original.Id);
                using var pointer = new Pointer(991019, PointerType.Mouse, true);
                var current = workflow.State.Snapshot;
                Point At(Point p) => handle.TranslatePoint(p, main)!.Value;
                var start = new Point(handle.Bounds.Width / 2, handle.Bounds.Height / 2);
                var end = kind == "micro" ? start + new Vector(1, 1) :
                    kind == "outside" ? new Point(-500, -500) : start + new Vector(0, -70);
                handle.RaiseEvent(new PointerPressedEventArgs(handle, pointer, main, At(start), 0,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                    KeyModifiers.None, 1));
                handle.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, handle, pointer, main, At(end),
                    0,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
                    KeyModifiers.None));
                if (kind == "capture-loss")
                {
                    await CaptureAsync(main, output, "insertion-line");
                    pointer.Capture(null);
                }

                if (kind is "stale" or "aba")
                {
                    Require(await workflow.SetGainAsync(.29m), "Stale fixture failed.");
                    if (kind == "aba") Require(await workflow.UndoAsync(), "ABA fixture failed.");
                    await Refresh();
                }

                var basis = workflow.State.Snapshot;
                handle.RaiseEvent(new PointerReleasedEventArgs(handle, pointer, main, At(end), 0,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                    KeyModifiers.None, MouseButton.Left));
                await Refresh();
                Require(workflow.State.Snapshot == basis, kind + " channel gesture committed.");
                if (kind == "stale")
                {
                    Require(await workflow.UndoAsync(), "Stale cleanup Undo failed.");
                    await Refresh();
                }

                Require(workflow.State.Snapshot == current, kind + " did not retain original snapshot.");
                receipts.Add("routed-channel-" + kind);
            }

            var level = (GainLevelControl)Id("Workflow.GainLevel");
            int gainHistory = workflow.State.UndoCount;
            await driver.TraceDrag(level, [new(25, 16), new(50, 16), new(82, 16)]);
            await Refresh();
            Require(workflow.State.Gain > .5m && workflow.State.UndoCount == gainHistory + 1,
                "Fader was not one Undo.");
            Require(
                decimal.Parse(Field<NumericUpDown>(controls, "_gain").Text!,
                    System.Globalization.CultureInfo.InvariantCulture) == workflow.State.Gain,
                "Fader exact readout rounded away accepted precision.");
            var accepted = workflow.State.Snapshot;
            await driver.TraceDrag(level, [new(50, 16), new(30, 16)], escape: true);
            await Refresh();
            Require(workflow.State.Snapshot == accepted, "Fader Escape committed.");
            receipts.Add("fader-drag-one-undo-and-escape");
            if (native)
            {
                await nativeDriver.Exact(Id("Workflow.Gain"), "0.37");
                await Refresh();
                Require(workflow.State.Gain == .37m, "Native exact Gain failed.");
                receipts.Add("native-exact-gain");
            }

            await ToolbarGainInputAsync(main, controls, workflow, output);
            await Refresh();
            await driver.TraceDrag(level, [new(40, 16), new(70, 16)], captureLoss: !native, escape: native);
            await Refresh();
            Require(workflow.State.Gain == .37m || !native, "Cancel changed Gain.");
            receipts.Add(native ? "native-second-cancel" : "routed-capture-loss");
            var nameHandle = Name(original.Id);
            if (native)
            {
                await nativeDriver.Click(nameHandle, beforeMs: 550, afterMs: 50);
                await nativeDriver.Click(nameHandle, beforeMs: 0, afterMs: 100);
            }
            else
            {
                using var doublePointer = new Pointer(991020, PointerType.Mouse, true);
                nameHandle.RaiseEvent(new PointerPressedEventArgs(nameHandle, doublePointer, main,
                    nameHandle.TranslatePoint(new(5, 5), main)!.Value, 0,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                    KeyModifiers.None, 2));
            }

            await Refresh();
            var editor = Field<NodeEditorSession>(main, "_nodeEditor");
            var sourceBinding = workflow.State.Snapshot.State.GraphAttachments.Single().Sources
                .Single(s => s.PartId == original.Id);
            Require(
                editor.Target?.NodeId == sourceBinding.NodeId &&
                workspace.State.Get(WorkspaceState.NodeEditorId).Visibility == PaneVisibility.Visible,
                "Name double click opened the wrong instrument.");
            workspace.State.Hide(WorkspaceState.NodeEditorId);
            workspace.Commit();
            await Refresh();
            receipts.Add("name-double-click-exact-source-editor");
            var fields = new List<object>();
            Require(await workflow.EditGraphAsync(
                new(workflow.State.Document.LifecycleId, workflow.State.Generation, workflow.State.Selected!.Value),
                "Long name fixture", (edit, _, _) => edit.RenamePart(Pattern().Id, original.Id,
                    "Snare · длинное имя независимого инструмента / long instrument name")), "Name fixture failed.");
            await Refresh();
            foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            foreach (int width in new[] { 1400, 1000, 640 })
            {
                await shell.SetLanguageAsync(language);
                await shell.SetThemeAsync(theme);
                main.Width = width;
                main.Height = width == 1400 ? 900 : 700;
                main.UpdateLayout();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                workspace.State.SetBounds(WorkspaceState.BeatId, new(5, 5, width == 1400 ? 850 : 440, 390));
                workspace.Commit();
                await Refresh();
                if (width < 880)
                {
                    ((Button)Id("Workflow.More")).Flyout!.ShowAt(Id("Workflow.More"));
                    await Task.Delay(100);
                    main.UpdateLayout();
                }

                fields.Add(FieldAlignment.Verify(Field<NumericUpDown>(controls, "_gain"),
                    Field<TextBlock>(controls, "_gainLabel"),
                    TopLevel.GetTopLevel(Field<NumericUpDown>(controls, "_gain"))!,
                    $"Gain-{language}-{theme}-{width}"));
                await CaptureAsync(main, output, $"rack-{language}-{theme}-{width}");
                if (width < 880)
                {
                    await CapturePopupAsync(Field<StackPanel>(controls, "_gainGroup"), output,
                        $"overflow-{language}-{theme}");
                    ((Button)Id("Workflow.More")).Flyout!.Hide();
                }

                matrix.Add(new
                    { language, theme, width, Window = main.Bounds, main.RenderScaling, Rack = rack.Bounds });
            }

            Json(output, "field-alignment", fields);
            main.Width = 1400;
            main.Height = 900;
            await Refresh();
            string save = Path.Combine(output, "independent.seqvium");
            var parts = Pattern().Parts.Select(p => (p.Id, p.SoundId)).ToArray();
            Require(await workflow.SaveAsync(save), "Save failed.");
            Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed.");
            Require(await workflow.OpenAsync(save, UnsavedDecision.Discard), "Open failed.");
            Require(parts.SequenceEqual(Pattern().Parts.Select(p => (p.Id, p.SoundId))),
                "Open changed independence/order.");
            receipts.Add("portable-save-new-open-order-and-identities");
            var legacy = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(save))!;
            foreach (var node in legacy["state"]!["graphs"]!.AsArray().SelectMany(g => g!["nodes"]!.AsArray()))
                if (node!["type"]!.GetValue<string>() == GraphBuiltIns.Gain)
                    node["parameters"]![GraphBuiltIns.GainAmplitude] = 1.1251m;
            string invalidPath = Path.Combine(output, "legacy-invalid.seqvium");
            await File.WriteAllTextAsync(invalidPath, legacy.ToJsonString());
            Require(await workflow.OpenAsync(invalidPath, UnsavedDecision.Discard), "Legacy invalid Gain Open failed.");
            await Refresh();
            var exact = Field<NumericUpDown>(controls, "_gain");
            Require(workflow.State.Gain == 1.1251m && exact.Value == 1.1251m && exact.Text == "1.1251" &&
                    exact.Classes.Contains("invalid") && !workflow.State.IsDirty, "Legacy Gain was clamped or hidden.");
            await CaptureAsync(main, output, "legacy-invalid-gain");
            var text = exact.GetVisualDescendants().OfType<TextBox>().Single();
            text.Focus();
            text.Text = "0.42";
            text.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await Refresh();
            Require(workflow.State.Gain == .42m && workflow.State.UndoCount == 1,
                "Explicit legacy Gain repair failed.");
            Require(await workflow.UndoAsync(), "Legacy repair Undo failed.");
            await Refresh();
            Require(workflow.State.Gain == 1.1251m && exact.Text == "1.1251", "Undo hid legacy Gain.");
            Require(await workflow.UndoAsync(true), "Legacy repair Redo failed.");
            await Refresh();
            var levelKeys = (GainLevelControl)Id("Workflow.GainLevel");
            levelKeys.Focus();
            foreach (var (key, expected) in new[] { (Key.Home, 0m), (Key.End, 1m), (Key.Left, .99m), (Key.Right, 1m) })
            {
                int prior = workflow.State.UndoCount;
                levelKeys.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
                await Refresh();
                Require(workflow.State.Gain == expected && workflow.State.UndoCount == prior + 1,
                    "Fader keyboard did not share exact Gain/history.");
            }

            receipts.Add("routed-legacy-invalid-display-repair-history-and-fader-keys");
            if (audio)
            {
                Require(await workflow.PlayAsync(), "Physical Play failed: " + workflow.State.Detail);
                await Task.Delay(150);
                var device = Field<WorkflowOutput>(workflow, "_output");
                var sampler = device.Sampler;
                long epoch = sampler.TransportEpoch, position = sampler.RenderPosition;
                Require(await workflow.SetGainAsync(.21m), "Playing Gain edit failed.");
                await Task.Delay(200);
                Require(
                    sampler.TransportEpoch == epoch && sampler.RenderPosition > position &&
                    device.ReadFailure() is null && sampler.ReadStatus().Playing,
                    "Live Gain reset/faulted transport.");
                Require(await workflow.MoveBeatChannelAsync(Basis(), Pattern().Parts[0].Id, 2),
                    "Playing reorder failed.");
                await Task.Delay(150);
                Require(sampler.TransportEpoch == epoch && device.ReadFailure() is null,
                    "Reorder reset/faulted transport.");
                Json(output, "physical",
                    new
                    {
                        device.Description, sampler.TransportEpoch, sampler.RenderPosition, SubjectiveListening = false
                    });
                await workflow.StopAsync(false);
                receipts.Add("physical-pattern-live-gain-and-reorder");
            }

            Require(await workflow.NewAsync(UnsavedDecision.Discard), "Empty fixture New failed.");
            foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            foreach (int width in new[] { 1400, 640 })
            {
                await shell.SetLanguageAsync(language);
                await shell.SetThemeAsync(theme);
                main.Width = width;
                main.Height = width == 1400 ? 900 : 700;
                await Refresh();
                Require(!Field<StackPanel>(controls, "_gainGroup").IsVisible,
                    "Empty project displays a fake Gain target.");
                await CaptureAsync(main, output, $"empty-{language}-{theme}-{width}");
            }

            Json(output, "rack-workflow",
                new { Passed = true, NativeInput = native, RoutedSetup = true, receipts, matrix });
        }
        catch (Exception error)
        {
            Json(output, "rack-workflow",
                new { Passed = false, NativeInput = native, receipts, matrix, Error = error.ToString() });
            await CaptureAsync(main, output, "failure");
            throw;
        }
    }

    private static async Task RoutedChannelDrag(MainWindow main, Control handle, Point from, Point to,
        KeyModifiers modifiers)
    {
        using var pointer = new Pointer(991012, PointerType.Mouse, true);
        Point At(Point p) => handle.TranslatePoint(p, main)!.Value;
        handle.RaiseEvent(new PointerPressedEventArgs(handle, pointer, main, At(from), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
            modifiers, 1));
        handle.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, handle, pointer, main, At(to), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), modifiers));
        handle.RaiseEvent(new PointerReleasedEventArgs(handle, pointer, main, At(to), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), modifiers,
            MouseButton.Left));
        await Task.Delay(30);
    }
}

internal static partial class NativePointer
{
    internal sealed partial class DrumInput
    {
        internal async Task CtrlDrag(Control control, Point from, Point to)
        {
            void Ctrl(bool down)
            {
                Guard();
                Program.Require(SendInput(1,
                    [new() { Type = 1, Data = new() { Keyboard = new() { Key = 0x11, Flags = down ? 0u : 2u } } }],
                    System.Runtime.InteropServices.Marshal.SizeOf<Input>()) == 1, "Native Ctrl failed.");
            }

            Ctrl(true);
            try
            {
                await TraceDrag(control, [from, to]);
            }
            finally
            {
                Ctrl(false);
            }
        }
    }
}
