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
using Avalonia.Platform.Storage;
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

        // Real captured paint controls: a skipped-cell move still paints a continuous row in one Undo.
        main.UpdateLayout();
        Point Root(ToggleButton b) => b.TranslatePoint(new(b.Bounds.Width / 2, b.Bounds.Height / 2), main)!.Value;
        void PressStep(ToggleButton b) => b.RaiseEvent(new PointerPressedEventArgs(b, pointer, main, Root(b), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
        void MoveStep(ToggleButton b) => (pointer.Captured as Control ?? b).RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent,
            pointer.Captured as Control ?? b, pointer, main, Root(b), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
        async Task ReleaseStep(ToggleButton b)
        {
            (pointer.Captured as Control ?? b).RaiseEvent(new PointerReleasedEventArgs(pointer.Captured as Control ?? b, pointer, main, Root(b), 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            await pane.PendingCommand; pane.Refresh();
        }
        var beforePaint = workflow.State.Snapshot; undo = workflow.State.UndoCount;
        PressStep(Step(0, 1)); MoveStep(Step(0, 3)); await ReleaseStep(Step(0, 3));
        Require(workflow.State.UndoCount == undo + 1 && Enumerable.Range(1, 3).All(i => Step(0, i).IsChecked == true), "Paint did not fill skipped cells in one transaction.");
        Require(await workflow.UndoAsync(), "Paint Undo failed."); pane.Refresh();
        Require(workflow.State.Snapshot == beforePaint, "Paint Undo did not restore exact identity.");
        PressStep(Step(0, 0)); MoveStep(Step(0, 3));
        pane.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        await ReleaseStep(Step(0, 3));
        Require(workflow.State.Snapshot == beforePaint, "Escape published an erase draft.");
        PressStep(Step(0, 1)); MoveStep(Step(0, 3)); pointer.Capture(null); await ReleaseStep(Step(0, 3));
        Require(workflow.State.Snapshot == beforePaint, "Capture loss published a paint draft.");
        // Open the actual row popup and invoke its menu action; verify canonical result/Undo.
        var more = pane.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == $"Beat.Actions.{pattern.Parts[0].Id}");
        more.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await CapturePopupAsync(more.ContextMenu!, output, "channel-menu");
        var fill = more.ContextMenu!.Items.OfType<MenuItem>().Single(m => AutomationProperties.GetAutomationId(m) == $"Beat.Fill4.{pattern.Parts[0].Id}");
        fill.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent)); await pane.PendingCommand; pane.Refresh();
        Require(Enumerable.Range(0, 16).All(i => Step(0, i).IsChecked == (i % 4 == 0)), "Fill menu did not edit canonical row.");
        Require(await workflow.UndoAsync(), "Menu Undo failed."); pane.Refresh(); more.ContextMenu.Close();
        Require(workflow.State.Snapshot == beforePaint, "Menu Undo did not restore exact identity.");

        string save = Path.Combine(output, "drums.seqvium"); Require(await workflow.SaveAsync(save), "GUI save failed.");
        Require(await workflow.OpenAsync(save, UnsavedDecision.Discard), "GUI Open failed."); pane.Refresh();
        Require(BeatSteps.Resolve(workflow.State).Pattern!.Id == pattern.Id && Step(0, 0).IsChecked == true, "Open lost grid identity/state.");
        await RedesignControlsAsync(main, pane, workflow, workspace, shell, output);
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
            workspace.State.SetBounds(WorkspaceState.BeatId, new(24, 24, reduced ? 560 : 960, 390));
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
            var scroll = Field<ScrollViewer>(pane, "_rackScroll");
            if (reduced) Require(scroll.Extent.Width > scroll.Viewport.Width, "Reduced pane lacks local horizontal scrolling.");
            if (reduced && language == HostLanguage.English && theme == HostTheme.Dark)
            {
                var controls = Field<WorkflowControls>(main, "_workflowControls");
                var overflow = Field<Button>(controls, "_overflow");
                ((Flyout)overflow.Flyout!).ShowAt(overflow); await Task.Delay(20); main.UpdateLayout();
                var tempo = Field<Button>(controls, "_tempoAction");
                Require(tempo.IsEffectivelyVisible && tempo.Bounds.Width > 0 &&
                    Field<Border>(controls, "_overflowParameters").Child is not null, "Reduced tempo action is unreachable.");
                ((Flyout)tempo.Flyout!).ShowAt(tempo); await Task.Delay(20);
                var field = Field<TextBox>(controls, "_tempoEdit"); var beforeTempo = workflow.State.Snapshot;
                field.Text = "129.25"; field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                await controls.PendingTempoCommand;
                Require(workflow.State.Snapshot.State.Settings.Tempo.BeatsPerMinute == 129.25m, "Reduced tempo popup failed.");
                Require(await workflow.UndoAsync() && workflow.State.Snapshot == beforeTempo, "Reduced tempo Undo failed.");
                overflow.Flyout.Hide(); pane.Refresh();
            }
            rows.Add(new { language, theme, reduced, Steps = buttons.Length, Focus = Step(0, 0).IsFocused,
                NameAlignmentDip = Math.Abs(Center(text) - Center(apply)), scroll.Extent, scroll.Viewport });
        }
        workspace.State.Dock(WorkspaceState.BeatId, PaneDock.Left); workspace.State.Collapse(WorkspaceState.BeatId);
        workspace.Open(WorkspaceState.BeatId); workspace.State.Hide(WorkspaceState.BeatId); workspace.Open(WorkspaceState.BeatId);
        Require(main.GetVisualDescendants().OfType<BeatPaneContent>().Single() == pane, "Workspace replaced retained content.");
        workspace.State.Dock(WorkspaceState.BeatId, PaneDock.Floating);
        main.Width = 1100; main.Height = 750; workspace.State.SetBounds(WorkspaceState.BeatId, new(24, 24, 960, 390)); main.UpdateLayout();
        await Task.Delay(80); main.Activate(); await Task.Delay(80); main.UpdateLayout();
        var native = await StepNativeInput.VerifyAsync(main, pane, workflow, Step(0, 1)); pane.Refresh();
        // Dense fixture uses real canonical imports/notes; it adds no bundled product content.
        foreach (var (name, rhythm) in new[] { ("Clap", 4), ("Low Tom", 7), ("Percussion", 3), ("Ride", 2), ("Texture", 5) })
        {
            string path = Path.Combine(output, name + ".wav"); WriteStepWav(path, .02f);
            Require(await workflow.AddBeatWavAsync(BeatSteps.Resolve(workflow.State).Basis!, path), "Dense fixture import failed.");
            var basis = BeatSteps.Resolve(workflow.State).Basis!; var part = BeatSteps.Resolve(workflow.State).Pattern!.Parts.Last();
            Require(await workflow.SetBeatStepsAsync(basis, part.Id, Enumerable.Range(0, 16).Select(i => i % rhythm == 0).ToArray()), "Dense fixture rhythm failed.");
        }
        pane.Refresh();
        main.WindowState = WindowState.Maximized; await Task.Delay(100);
        workspace.State.SetBounds(WorkspaceState.BeatId, new(20, 20, Math.Max(820, workspace.State.Width - 40), Math.Max(560, workspace.State.Height - 40)));
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            await CaptureAsync(main, output, $"dense-{theme}");
            Require(pane.GetVisualDescendants().OfType<ToggleButton>().Count() == 128, "Dense Rack lost channels/steps.");
        }
        Require(!await workflow.AddBeatWavAsync(BeatSteps.Resolve(workflow.State).Basis!, Path.Combine(output, "Kick.wav")), "Channel capacity incorrectly accepted a ninth channel.");
        pane.Refresh(); await CaptureAsync(main, output, "channel-limit");
        main.WindowState = WindowState.Normal; main.Width = 1100; main.Height = 750; main.UpdateLayout();
        var navigationToggle = main.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Navigator.Toggle");
        navigationToggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); main.UpdateLayout();
        Require(!main.GetVisualDescendants().OfType<PatternNavigator>().Single().IsVisible, "Navigator collapse failed.");
        await CaptureAsync(main, output, "navigation-collapsed");
        navigationToggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); main.UpdateLayout();
        Require(main.GetVisualDescendants().OfType<PatternNavigator>().Single().IsVisible, "Navigator restore failed.");
        await MusicalControlsAsync(shell, output);
        Json(output, "steps", new { Rows = rows, RoutedInput = "create, twelve step automation actions with synthetic pointer events, second toggle, external Undo/Redo, arrow/Space, stale held click, captured paint/skipped cells/one Undo, Escape/capture-loss erase cancellation, actual Fill popup",
            SaveOpen = "canonical identity and visible states restored", RetainedLifecycle = "dock/collapse/hide/reopen", NativeInput = native,
            AudioStarted = workflow.State.CanStop, PhysicalAudio = false });
    }

    private sealed class MusicalBorrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Require(Joined, "Simulated output must join.");
    }

    // Production Rack controls, real coordinator/sampler, substituted physical borrower; no sound/device.
    private static async Task MusicalControlsAsync(ShellSession shell, string output)
    {
        RealtimeSampler? sampler = null;
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), () =>
        {
            sampler = new(48000, 2, 2048);
            return new(new MusicalBorrower(), sampler, "Simulated physical borrower", () => null);
        }, Path.Combine(output, "musical-media"));
        await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Live rhythm");
        foreach (var name in new[] { "Kick", "Snare", "Hi-Hat" })
            Require(await workflow.AddBeatWavAsync(BeatSteps.Resolve(workflow.State).Basis!, Path.Combine(output, name + ".wav")), "Music fixture import failed.");
        using var pane = new BeatPaneContent(shell, workflow, (_, _) => Task.CompletedTask);
        var window = new Window { Width = 1000, Height = 460, Content = pane, Title = "Seqvium musical controls verification" };
        window.Show();
        try
        {
            window.UpdateLayout();
            var bars = Field<ComboBox>(pane, "_bars"); bars.SelectedIndex = 1; await pane.PendingCommand; pane.Refresh();
            var pattern = BeatSteps.Resolve(workflow.State).Pattern!;
            Require(pattern.Length.Ticks == BeatSteps.BarTicks * 2, "Length selector did not extend canonical Pattern.");
            Require(pane.GetVisualDescendants().OfType<ToggleButton>().Count() == 96, "Long Rack did not expose 32 steps per channel.");
            Require(await workflow.ToggleBeatStepAsync(BeatSteps.Resolve(workflow.State).Basis!, pattern.Parts[0].Id, 16), "Long step failed.");
            var play = workflow.PlayAsync(); var packet = new float[4096];
            for (int i = 0; i < 2000 && !play.IsCompleted; i++)
            { sampler?.Process(packet); await workflow.ObserveAsync(); await Task.Delay(1); }
            Require(await play, "Simulated Desktop Play failed.");
            long target = 102000;
            while (sampler!.RenderPosition < target)
                sampler.Process(packet.AsSpan(0, (int)Math.Min(2048, target - sampler.RenderPosition) * 2));
            await workflow.ObserveAsync(); pane.Refresh(); window.UpdateLayout();
            var position = Field<TextBlock>(pane, "_position");
            Require(position.Text == "2:1:2", "Rack position does not match actual sampler phase: " + position.Text);
            Require(pane.GetVisualDescendants().OfType<ToggleButton>().Count(b => b.Classes.Contains("current")) == 3, "Current step marker missing.");
            Field<ScrollViewer>(pane, "_rackScroll").Offset = new(670, 0);
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                pane.Refresh(); window.UpdateLayout(); await CaptureAsync(window, output, "live-long-" + theme);
                var nameText = Field<TextBox>(pane, "_name").GetVisualDescendants().OfType<TextPresenter>().Single();
                double Center(Control c) => c.TranslatePoint(default, window)!.Value.Y + c.Bounds.Height / 2;
                Require(Math.Abs(Center(nameText) - Center(bars)) <= 1, "Length selector/name alignment failed.");
            }
            await workflow.StopAsync(); sampler.Process(packet); await workflow.ObserveAsync(); pane.Refresh();
            Require(!pane.GetVisualDescendants().OfType<ToggleButton>().Any(b => b.Classes.Contains("current")), "Stop left a stale position marker.");
            var more = pane.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == $"Beat.Actions.{pattern.Parts[1].Id}");
            more.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            var delete = more.ContextMenu!.Items.OfType<MenuItem>().Single(m => AutomationProperties.GetAutomationId(m) == $"Beat.DeleteChannel.{pattern.Parts[1].Id}");
            delete.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent)); await pane.PendingCommand; more.ContextMenu.Close(); pane.Refresh();
            Require(BeatSteps.Resolve(workflow.State).Pattern!.Parts.Length == 2, "Channel menu deletion failed.");
            Require(await workflow.UndoAsync(), "Channel deletion Undo failed."); pane.Refresh();
            string exported = Path.Combine(output, "gui-pattern.wav");
            Require(await workflow.ExportWaveAsync(BeatSteps.Resolve(workflow.State).Basis!, exported), "GUI workflow export failed.");
            Field<Button>(pane, "_delete").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); await pane.PendingCommand; pane.Refresh();
            Require(workflow.State.Occurrences.Length == 0 && Field<Button>(pane, "_create").IsEnabled, "Pattern deletion left an unusable Rack.");
            Require(await workflow.UndoAsync(), "Pattern deletion Undo failed."); pane.Refresh();
            Json(output, "musical-controls", new { LongSteps = 96, Position = "2:1:2", CurrentColumn = 17,
                LengthSelector = "actual selection event", ChannelDelete = "actual menu action / exact Undo",
                PatternDelete = "actual button / Undo", Export = "graph PCM16 WAV", PhysicalAudio = false,
                Playback = "real sampler/coordinator with substituted physical borrower" });
        }
        finally { window.Close(); }
    }
    private static async Task RedesignControlsAsync(MainWindow main, BeatPaneContent pane, DesktopWorkflow workflow,
        WorkspaceHost workspace, ShellSession shell, string output)
    {
        var before = workflow.State.Snapshot; int undo = workflow.State.UndoCount;
        var attachment = workflow.State.Selected!.Value;
        var graph = before.State.Graphs.Single(g => g.Id == before.State.GraphAttachments.Single(a => a.Id == attachment).GraphId);
        var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        Invoke(main, "OpenNodeEditor", new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment, graph.Id, gain.Id));
        var editor = main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single();
        editor.Refresh(); main.UpdateLayout();
        var dial = Field<GainDialControl>(editor, "_dial");
        using var pointer = new Pointer(100043, PointerType.Mouse, true);
        var center = dial.TranslatePoint(new(dial.Bounds.Width / 2, dial.Bounds.Height / 2), main)!.Value;
        var origin = center + new Vector(0, -30);
        var destination = center + new Vector(Math.Sin(Math.PI * .3), -Math.Cos(Math.PI * .3)) * 30;
        void Press() => dial.RaiseEvent(new PointerPressedEventArgs(dial, pointer, main, origin, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
        void Move(KeyModifiers modifiers = KeyModifiers.None) => dial.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, dial,
            pointer, main, destination, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), modifiers));
        async Task Release()
        {
            dial.RaiseEvent(new PointerReleasedEventArgs(dial, pointer, main, destination, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            await editor.PendingCommand; editor.Refresh();
        }
        Press(); Move();
        Require(workflow.State.Snapshot == before && dial.Amplitude == .45m, "Dial preview published music or relative gesture failed.");
        await CaptureAsync(main, output, "gain-dial-draft");
        await Release(); Require(workflow.State.Gain == .45m && workflow.State.UndoCount == undo + 1, "Dial release did not commit once.");
        Require(await workflow.UndoAsync(), "Dial Undo failed."); editor.Refresh();
        Press(); Move(KeyModifiers.Shift); Require(dial.Amplitude == .29m, "Dial fine movement failed.");
        editor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape }); await Release();
        Require(workflow.State.Snapshot == before, "Dial Escape published a draft.");
        Press(); Move(); pointer.Capture(null); await Release();
        Require(workflow.State.Snapshot == before, "Dial capture loss published a draft.");
        dial.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.End }); await editor.PendingCommand; editor.Refresh();
        Require(workflow.State.Gain == 1 && workflow.State.UndoCount == undo + 1, "Dial keyboard End failed.");
        Require(await workflow.UndoAsync(), "Dial keyboard Undo failed."); editor.Refresh(); workspace.State.Hide(WorkspaceState.NodeEditorId);

        var controls = Field<WorkflowControls>(main, "_workflowControls");
        var tempo = Field<Button>(controls, "_tempoAction");
        ((Flyout)tempo.Flyout!).ShowAt(tempo); await Task.Delay(20); main.UpdateLayout();
        var field = Field<TextBox>(controls, "_tempoEdit"); field.Text = "137.5";
        field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        await controls.PendingTempoCommand;
        Require(workflow.State.Snapshot.State.Settings.Tempo.BeatsPerMinute == 137.5m && workflow.State.UndoCount == undo + 1, "Tempo popup did not commit once.");
        Require(await workflow.UndoAsync(), "Tempo Undo failed."); pane.Refresh();
        Require(workflow.State.Snapshot == before, "Tempo Undo rewrote Pattern content.");
        var navigator = main.GetVisualDescendants().OfType<PatternNavigator>().Single();
        var original = workflow.State.Selected!.Value;
        Field<Button>(navigator, "_create").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await navigator.PendingCommand; pane.Refresh(); main.UpdateLayout();
        Require(workflow.State.Snapshot.State.Patterns.Length == 2 && workflow.State.Selected != original &&
            BeatSteps.Resolve(workflow.State).Pattern is { Parts.Length: 0 }, "Navigator did not create/select an empty Pattern.");
        await CaptureAsync(main, output, "pattern-navigation");
        var first = navigator.GetVisualDescendants().OfType<Button>().Single(b =>
            AutomationProperties.GetAutomationId(b) == "Navigator.Pattern." + original);
        var twoPatterns = workflow.State.Snapshot;
        first.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); await navigator.PendingCommand; pane.Refresh();
        Require(workflow.State.Selected == original && workflow.State.Snapshot == twoPatterns && workflow.State.UndoCount == undo + 1,
            "Navigator selection rewrote music/history.");
        Require(await workflow.UndoAsync(), "Navigator create Undo failed."); pane.Refresh();
        Require(workflow.State.Snapshot == before, "Navigator create Undo changed existing Pattern identity.");
        string dropPath = Path.Combine(output, "Drop.wav"); WriteStepWav(dropPath, .02f);
        var file = await main.StorageProvider.TryGetFileFromPathAsync(dropPath) ?? throw new InvalidOperationException("Drop fixture unavailable.");
        using var transfer = new DataTransfer(); transfer.Add(DataTransferItem.CreateFile(file));
        pane.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, transfer, pane, new Point(10, 10), KeyModifiers.None));
        Require(pane.Classes.Contains("dropReady"), "Single WAV drop did not expose its target.");
        pane.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, transfer, pane, new Point(10, 10), KeyModifiers.None));
        await pane.PendingCommand; pane.Refresh();
        Require(BeatSteps.Resolve(workflow.State).Pattern is { Parts.Length: 4 } && workflow.State.UndoCount == undo + 1,
            "Routed WAV drop did not import one canonical channel.");
        Require(await workflow.UndoAsync(), "Drop Undo failed."); pane.Refresh();
        Require(workflow.State.Snapshot == before && !pane.Classes.Contains("dropReady"), "Drop Undo changed existing Pattern identity.");
        transfer.Add(DataTransferItem.CreateFile(file));
        pane.RaiseEvent(new DragEventArgs(DragDrop.DragOverEvent, transfer, pane, new Point(10, 10), KeyModifiers.None));
        Require(!pane.Classes.Contains("dropReady"), "Multi-file drop was presented as supported.");
        Json(output, "redesign-controls", new { Dial = "clockwise/fine preview, single commit/Undo, Escape, capture loss, keyboard End",
            Tempo = "actual popup Enter / one Undo / canonical restoration",
            Navigator = "actual create/select buttons, unchanged selection history, exact create Undo",
            Drop = "routed single-file import / exact Undo / multi-file refusal; not native Explorer input", PhysicalAudio = false });
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
            var beforePaint = workflow.State.Snapshot;
            var part = BeatSteps.Resolve(workflow.State).Pattern!.Parts[0].Id;
            ToggleButton Step(int i) => pane.GetVisualDescendants().OfType<ToggleButton>().Single(b =>
                AutomationProperties.GetAutomationId(b) == $"Beat.Step.{part}.{i}");
            void MoveTo(ToggleButton cell)
            {
                Program.Require(Guard(), "Native paint foreground changed.");
                var at = cell.PointToScreen(new(cell.Bounds.Width / 2, cell.Bounds.Height / 2));
                Program.Require(SetCursorPos(at.X, at.Y), "Native paint cursor move failed.");
            }
            undo = workflow.State.UndoCount;
            MoveTo(Step(1)); Mouse(true); await Task.Delay(30); MoveTo(Step(3)); await Task.Delay(30); Mouse(false);
            await Task.Delay(120); await pane.PendingCommand; pane.Refresh();
            Program.Require(workflow.State.UndoCount == undo + 1 && Enumerable.Range(1, 3).All(i => Step(i).IsChecked == true),
                "Native paint failed to fill skipped cells in one Undo.");
            passed.Add("native captured drag → continuous three-step paint / one Undo");
            Program.Require(await workflow.UndoAsync(), "Native paint Undo failed."); pane.Refresh();
            Program.Require(workflow.State.Snapshot == beforePaint, "Native paint Undo lost canonical identity.");
            MoveTo(Step(0)); Mouse(true); await Task.Delay(30); MoveTo(Step(3)); await Task.Delay(30);
            Program.Require(Guard(), "Native erase foreground changed.");
            var escape = new Input { Type = 1, Data = new() { Keyboard = new() { Key = 0x1B } } };
            var escapeUp = new Input { Type = 1, Data = new() { Keyboard = new() { Key = 0x1B, Flags = 2 } } };
            Program.Require(SendInput(2, [escape, escapeUp], Marshal.SizeOf<Input>()) == 2, "Native Escape failed.");
            await Task.Delay(30); Mouse(false); await Task.Delay(80); await pane.PendingCommand; pane.Refresh();
            Program.Require(workflow.State.Snapshot == beforePaint && workflow.State.UndoCount == undo, "Native Escape published erase draft.");
            passed.Add("native Escape cancels captured erase without musical mutation");
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
