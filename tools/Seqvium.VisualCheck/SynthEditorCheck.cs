// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task SynthEditorAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow"); var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900;
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "F1"), "Create Pattern failed.");
        workspace.Open(WorkspaceState.BeatId); main.UpdateLayout();
        var beat = main.GetVisualDescendants().OfType<BeatPaneContent>().Single(); beat.Refresh();
        var add = main.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Beat.AddSynth");
        add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await beat.PendingCommand;
        var pattern = BeatSteps.Resolve(workflow.State).Pattern!; var part = pattern.Parts.Single();
        Require(await workflow.ConfigurePianoSourceAsync(BeatSteps.Resolve(workflow.State).Basis!, part.Id, "F1", 60), "Fixture channel name failed.");
        Require(SynthVoice.Supports(workflow.State.Snapshot.State.Sounds.Single()), "Rack did not create a valid synth.");
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Require(await workflow.EditPianoNotesAsync(Basis(), part.Id,
            [new(Id<NoteEvent>.New(), new(0), new(1920000), 60, .7m),
             new(Id<NoteEvent>.New(), new(0), new(1920000), 64, .6m),
             new(Id<NoteEvent>.New(), new(0), new(1920000), 67.5m, .6m)]), "Chord entry failed.");
        SynthPaneContent Pane() => main.GetVisualDescendants().OfType<SynthPaneContent>().Single();
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c => c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == "Synth." + id);
        TextBlock FieldError(string key) => Pane().GetVisualDescendants().OfType<TextBlock>().Single(c => AutomationProperties.GetAutomationId(c) == "Synth.Error." + key);
        TextBlock Status() => Pane().GetVisualDescendants().OfType<TextBlock>().Single(c => AutomationProperties.GetAutomationId(c) == "Synth.Status");
        async Task Refresh()
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            // Drain posted production reconciliation before certifying readiness. It can discover
            // a new pending teardown; the pre-dispatch snapshot is not a completed input boundary.
            while (true)
            {
                await Pane().PendingCommand.WaitAsync(TimeSpan.FromSeconds(5));
                var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void Observed(WorkflowState state) { if (!state.Busy) idle.TrySetResult(); }
                workflow.Changed += Observed;
                try { if (!workflow.State.Busy) idle.TrySetResult(); await idle.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
                finally { workflow.Changed -= Observed; }
                await workflow.ObserveAsync();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
                main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single().Refresh(); main.UpdateLayout();
                if (Field<long>(Pane(), "_generation") == workflow.State.Generation && !workflow.State.Busy && Pane().PendingCommand.IsCompleted) return;
                Require(DateTime.UtcNow < deadline, "Synth presenter did not reach a completed current-generation input boundary.");
            }
        }
        await Refresh();
        var binding = workflow.State.Snapshot.State.GraphAttachments.Single().Sources.Single();
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId, binding.NodeId);
        Require(Field<NodeEditorSession>(main, "_nodeEditor").Target == target, "Rack did not open the Synth Source Editor.");
        Invoke(main, "OpenGraph"); main.UpdateLayout(); await Task.Delay(40);
        var canvas = main.GetVisualDescendants().OfType<GraphCanvas>().Single(); canvas.Fit();
        var editor = Field<GraphEditorSession>(main, "_graphEditor");
        var node = editor.Graph!.Nodes.Single(n => n.Id == binding.NodeId); var rect = GraphGeometry.Body(node);
        var point = editor.Viewport.ToScreen(new(rect.X + 50, rect.Y + 18));
        var rootPoint = canvas.TranslatePoint(new(point.X, point.Y), main)!.Value;
        using (var pointer = new Pointer(96000, PointerType.Mouse, true))
            for (int click = 1; click <= 2; click++)
            {
                canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, main, rootPoint, 0,
                    new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, click));
                canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, main, rootPoint, 0,
                    new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            }
        Require(Field<NodeEditorSession>(main, "_nodeEditor").Target == target, "Source double-click changed target.");
        workspace.State.Hide(WorkspaceState.GraphId); workspace.Open(WorkspaceState.NodeEditorId);
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 700, 670)); workspace.Commit(); await Refresh();
        void Click(string key) => ((Button)Id(key)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void Exact(string key, string text)
        {
            var field = (TextBox)Id("Value." + key); field.Text = text;
            field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        }
        SoundDefinition Sound() => workflow.State.Snapshot.State.Sounds.Single(s => s.Id == part.SoundId);
        var previewReadiness = new List<object>();
        async Task PreviewState(bool playing, string phase)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Changed(WorkflowState _) { if (workflow.SamplerPreviewPlaying == playing) completion.TrySetResult(); }
            workflow.Changed += Changed;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (workflow.SamplerPreviewPlaying == playing) completion.TrySetResult();
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(5)); await Refresh();
                previewReadiness.Add(new { Phase = phase, Playing = playing, watch.ElapsedMilliseconds, Result = "observed" });
            }
            catch (TimeoutException)
            {
                var device = typeof(DesktopWorkflow).GetField("_noteOutput", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(workflow) as WorkflowOutput;
                Json(output, "synth-preview-timeout", new { Phase = phase, ExpectedPlaying = playing, ActualPlaying = workflow.SamplerPreviewPlaying,
                    Held = Field<bool>(Pane(), "_held"), GateHeld = Field<bool>(Pane(), "_gateHeld"), Pending = Pane().PendingCommand.Status.ToString(),
                    UiGeneration = Field<long>(Pane(), "_generation"), workflow.State.Generation, workflow.State.Busy, workflow.State.StatusKey, workflow.State.Detail,
                    Feedback = Status().Text, HasOutput = device is not null, Voices = device?.Sampler.ActiveVoices,
                    Joined = device?.Lifetime.Joined, Failure = device?.ReadFailure(), Sampler = device?.Sampler.ReadStatus() });
                await CaptureAsync(main, output, "synth-preview-timeout"); throw;
            }
            finally { workflow.Changed -= Changed; Json(output, "synth-preview-readiness", previewReadiness); }
        }

        int undo = workflow.State.UndoCount;
        Exact("cutoff", "3200"); await Refresh(); Require(Sound().Parameters["cutoff"] == 3200 && workflow.State.UndoCount == undo + 1, "Cutoff exact/history failed.");
        Click("Wave1.3"); await Refresh(); Require(Sound().Parameters["wave1"] == 3, "Waveform selection failed.");
        Exact("mix", "40"); await Refresh(); Exact("detune", "13.5"); await Refresh();
        Require(Sound().Parameters["mix"] == .4m && Sound().Parameters["detune"] == 13.5m, "Oscillator controls failed.");
        var before = workflow.State.Snapshot;
        Exact("cutoff", "20000"); await Refresh(); Require(workflow.State.Snapshot == before, "Invalid cutoff changed state.");
        var validation = new List<object>();
        foreach (var parameter in SynthVoice.Parameters.Where(p => !p.Key.StartsWith("wave", StringComparison.Ordinal)))
        {
            var field = (TextBox)Id("Value." + parameter.Key); var error = FieldError(parameter.Key);
            string DisplayValue(decimal value) => (parameter.Unit == "%" ? value * 100 : value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (string invalidValue in new[] { "unfinished", DisplayValue(parameter.Minimum - 1), DisplayValue(parameter.Maximum + 1) })
            {
                Exact(parameter.Key, invalidValue); await Refresh();
                Json(output, "synth-field-validation-current", new { parameter.Key, ExpectedText = invalidValue, ActualText = field.Text,
                    SnapshotUnchanged = workflow.State.Snapshot == before, ErrorVisible = error.IsVisible, Warning = field.Classes.Contains("warning"),
                    HeaderVisible = Status().IsVisible, HeaderText = Status().Text, workflow.State.Busy,
                    UiGeneration = Field<long>(Pane(), "_generation"), DocumentGeneration = workflow.State.Generation, Pending = Pane().PendingCommand.Status.ToString() });
                Require(field.Text == invalidValue && workflow.State.Snapshot == before && error.IsVisible &&
                    field.Classes.Contains("warning") && !Status().IsVisible, "Field-local rejection lost draft/state or used header feedback: " + parameter.Key);
                Require(error.Text!.Contains(DisplayValue(parameter.Minimum), StringComparison.Ordinal) && error.Text.Contains(DisplayValue(parameter.Maximum), StringComparison.Ordinal), "Missing parameter range.");
                var delta = error.TranslatePoint(default, field)!.Value;
                Require(delta.Y >= field.Bounds.Height && delta.Y <= field.Bounds.Height + 8, "Range is detached from its field.");
                validation.Add(new { parameter.Key, InvalidValue = invalidValue, Range = error.Text, DistanceBelowField = delta.Y - field.Bounds.Height, DraftPreserved = true });
                field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape }); await Refresh();
                Require(!error.IsVisible && !field.Classes.Contains("warning"), "Escape retained a field error.");
            }
        }
        Json(output, "synth-field-validation", validation);
        var cutoff = (TextBox)Id("Value.cutoff"); cutoff.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        cutoff.Text = "draft"; await Refresh(); Require(cutoff.Text == "draft", "Observation lost numeric draft.");
        var presetAttempt = (ComboBox)Id("Presets");
        var unfinishedSnapshot = workflow.State.Snapshot;
        presetAttempt.SelectedIndex = 2; await Refresh();
        Require(cutoff.Text == "draft" && workflow.State.Snapshot == unfinishedSnapshot && presetAttempt.SelectedIndex != 2, "Preset discarded an unfinished value.");
        await shell.ToggleLanguageAsync(); await Refresh();
        Require(cutoff.Text == "draft" && ((TextBlock)Id("Status")).Text == shell["Synth.FinishEdit"], "Language switch lost draft or retained stale-language feedback.");
        await shell.ToggleLanguageAsync(); await Refresh();
        cutoff.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        var envelope = (SynthEnvelopePlot)Id("Envelope");
        using (var pointer = new Pointer(96001, PointerType.Mouse, true))
        {
            Point Root(Point p) => envelope.TranslatePoint(p, main)!.Value;
            var start = envelope.Handles()[2]; var end = start + new Vector(0, 24);
            void Press() => envelope.RaiseEvent(new PointerPressedEventArgs(envelope, pointer, main, Root(start), 0,
                new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
            void Move() => envelope.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, envelope, pointer, main, Root(end), 0,
                new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
            void Release() => envelope.RaiseEvent(new PointerReleasedEventArgs(envelope, pointer, main, Root(end), 0,
                new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            decimal sustain = Sound().Parameters["sustain"]; undo = workflow.State.UndoCount;
            Press(); Move(); Release(); await Refresh();
            Require(Sound().Parameters["sustain"] < sustain && workflow.State.UndoCount == undo + 1, "ADSR gesture did not commit once.");
            Require(await workflow.UndoAsync(), "Envelope Undo failed."); await Refresh();
            Press(); Move(); envelope.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape }); Release(); await Refresh();
            Require(Sound().Parameters["sustain"] == sustain, "Envelope Escape changed state.");
            Press(); Move(); pointer.Capture(null); await Refresh(); Require(Sound().Parameters["sustain"] == sustain, "Capture loss changed state.");
        }
        var presets = (ComboBox)Id("Presets");
        foreach (var preset in SynthVoice.Presets.Select((p, i) => (p, i)))
        {
            presets.SelectedIndex = preset.i + 1; await Refresh();
            Require(Sound().Parameters.OrderBy(p => p.Key).SequenceEqual(preset.p.Values.OrderBy(p => p.Key)), "Factory preset failed.");
            decimal[] pitches = preset.p.Key == "WarmBass" ? [36, 43, 36, 34] :
                preset.p.Key == "SoftPad" ? [48, 55, 60] : preset.p.Key == "BrightLead" ? [60, 64, 67, 72] : [60, 64, 67, 72];
            bool chord = preset.p.Key == "SoftPad";
            Require(await workflow.EditPianoNotesAsync(Basis(), part.Id,
                [.. pitches.Select((pitch, i) => new NoteEvent(Id<NoteEvent>.New(), new(chord ? 0 : i * 960000),
                    new(chord ? 3840000 : 720000), pitch, .7m))]), "Audition phrase failed.");
            string path = Path.Combine(output, preset.p.Key + ".wav");
            Require(await workflow.ExportWaveAsync(Basis(), path), "Factory WAV export failed.");
            await Refresh();
        }
        Require(await workflow.EditPianoNotesAsync(Basis(), part.Id,
            [new(Id<NoteEvent>.New(), new(0), new(1920000), 60, .7m),
             new(Id<NoteEvent>.New(), new(0), new(1920000), 64, .6m),
             new(Id<NoteEvent>.New(), new(0), new(1920000), 67.5m, .6m)]), "Restore chord failed.");
        await Refresh();
        workspace.Open(WorkspaceState.PianoId);
        main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single().OpenPart(part.Id);
        var matrix = new List<object>(); var alignment = new List<object>(); var composition = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (var size in new[] { "fullhd", "reduced", "compact" })
                {
                    main.Width = size == "fullhd" ? 1920 : size == "reduced" ? 1280 : 900;
                    main.Height = size == "fullhd" ? 1080 : size == "reduced" ? 800 : 700;
                    main.UpdateLayout(); await Task.Delay(50);
                    var canvasBounds = Field<Canvas>(workspace, "_canvas").Bounds;
                    workspace.State.Reflow(canvasBounds.Width, canvasBounds.Height);
                    workspace.State.SetBounds(WorkspaceState.NodeEditorId, size == "fullhd" ? new(740, 0, 760, 660) :
                        size == "reduced" ? new(0, 0, 640, 630) : new(0, 0, 360, 520));
                    if (size == "fullhd")
                    {
                        workspace.Open(WorkspaceState.BeatId); workspace.Open(WorkspaceState.PianoId);
                        workspace.State.SetBounds(WorkspaceState.BeatId, new(0, 0, 720, 300));
                        workspace.State.SetBounds(WorkspaceState.PianoId, new(0, 310, 720, 420));
                        workspace.State.Hide(WorkspaceState.ArrangementId);
                    }
                    workspace.Open(WorkspaceState.NodeEditorId);
                    workspace.Commit(); await Refresh();
                    foreach (int presetIndex in new[] { 1, 2, 4 })
                    {
                        presets.SelectedIndex = presetIndex; await Refresh();
                        ((ScrollViewer)Pane().Children[1]).Offset = default; await Refresh();
                        string name = $"synth-{language}-{theme}-{size}-{presetIndex}";
                        await CaptureAsync(main, output, name);
                        matrix.Add(new { Name = name, Window = main.Bounds, Pane = Pane().Bounds, main.RenderScaling });
                    }
                    composition.Add(SynthCompositionAudit(Pane(), $"{language}-{theme}-{size}"));
                    Json(output, "synth-composition-audit", composition);
                    if (size == "fullhd")
                    {
                        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
                        var tools = piano.GetVisualDescendants().OfType<Control>().Where(c => AutomationProperties.GetAutomationId(c) is "Piano.Draw" or "Piano.Stop" or "Piano.Root").ToArray();
                        var rows = tools.Select(c => c.TranslatePoint(default, piano)!.Value.Y).ToArray();
                        Require(rows.Max() - rows.Min() < 1, "Piano tools stranded on a second row.");
                    }
                    var previewControls = new[] { "Note", "Hold", "Release", "Stop" }.Select(Id).ToArray();
                    var previewCenters = previewControls.Select(c => c.TranslatePoint(new(c.Bounds.Width / 2, c.Bounds.Height / 2), main)!.Value.Y).ToArray();
                    Require(previewCenters.Max() - previewCenters.Min() < .5, "Preview controls are not aligned.");
                    Require(previewControls.All(c => c.Bounds.Height == 40), "Preview height mismatch.");
                    if (size != "compact") Require(((ScrollViewer)Pane().Children[1]).Extent.Height <= ((ScrollViewer)Pane().Children[1]).Viewport.Height + 1, "Ordinary editor unnecessarily scrolls.");
                    foreach (string key in new[] { "mix", "detune", "cutoff", "resonance", "attack", "decay", "sustain", "release" })
                    {
                        var field = Id("Value." + key); var row = (Grid)field.Parent!;
                        alignment.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().Single(), row, $"{language}-{theme}-{size}-{key}"));
                    }
                    ((ScrollViewer)Pane().Children[1]).ScrollToEnd(); await Refresh();
                    Require(Id("Hold").Bounds.Height >= 32, "Compact preview is unreachable.");
                    if (size == "compact")
                    {
                        await CaptureAsync(main, output, $"synth-{language}-{theme}-compact-envelope");
                        var compactScroll = (ScrollViewer)Pane().Children[1];
                        for (int handle = 0; handle < 4; handle++)
                        {
                            var basisSnapshot = workflow.State.Snapshot;
                            var handlePoint = envelope.Handles()[handle];
                            var viewportPoint = envelope.TranslatePoint(handlePoint, compactScroll)!.Value;
                            Require(viewportPoint.Y >= 0 && viewportPoint.Y <= compactScroll.Viewport.Height, "Compact ADSR handle is clipped.");
                            using var curvePointer = new Pointer(96021, PointerType.Mouse, true);
                            var compactRoot = envelope.TranslatePoint(handlePoint, main)!.Value;
                            envelope.RaiseEvent(new PointerPressedEventArgs(envelope, curvePointer, main, compactRoot, 0, new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
                            var moved = compactRoot + new Vector(handle == 2 ? 0 : 5, handle == 2 ? -10 : 0);
                            envelope.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, envelope, curvePointer, main, moved, 0, new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
                            var offset = compactScroll.Offset;
                            var wheel = new PointerWheelEventArgs(envelope, curvePointer, main, moved, 0, new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None, new(0, 1));
                            envelope.RaiseEvent(wheel); Require(wheel.Handled && compactScroll.Offset == offset, "Curve drag conflicts with scrolling.");
                            envelope.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
                            envelope.RaiseEvent(new PointerReleasedEventArgs(envelope, curvePointer, main, moved, 0, new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
                            await Refresh(); Require(workflow.State.Snapshot == basisSnapshot, "Compact ADSR cancel changed intent.");
                        }
                        ((ScrollViewer)Pane().Children[1]).Offset = default; await Refresh();
                        foreach (var parameter in SynthVoice.Parameters.Where(p => !p.Key.StartsWith("wave", StringComparison.Ordinal)))
                        {
                            string invalidValue = parameter.Key == "cutoff" ? "20000" :
                                ((parameter.Maximum + 1) * (parameter.Unit == "%" ? 100 : 1)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            var field = (TextBox)Id("Value." + parameter.Key); var error = FieldError(parameter.Key);
                            var unchanged = workflow.State.Snapshot;
                            Exact(parameter.Key, invalidValue); await Refresh();
                            ((Control)field.Parent!.Parent!).BringIntoView(); await Refresh();
                            Require(field.Text == invalidValue && error.IsVisible && !Status().IsVisible && workflow.State.Snapshot == unchanged, "Localized invalid draft was not retained beside its field.");
                            string suffix = parameter.Key == "cutoff" ? "error" : "error-" + parameter.Key;
                            await CaptureAsync(main, output, $"synth-{language}-{theme}-compact-{suffix}");
                            field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape }); await Refresh();
                        }
                        ((ScrollViewer)Pane().Children[1]).Offset = default; await Refresh();
                    }
                    if (audio)
                    {
                        presets.SelectedIndex = 2; await Refresh();
                        Click("Hold"); await Refresh(); await PreviewState(true, $"{language}-{theme}-{size}-held"); Require(workflow.SamplerPreviewPlaying && Id("Release").IsEnabled, "Matrix held preview unavailable.");
                        await CaptureAsync(main, output, $"synth-{language}-{theme}-{size}-held");
                        if (size == "reduced") CaptureSynthReleaseDetail((Button)Id("Release"), output, $"release-{language}-{theme}-enabled");
                        Click("Release"); await Refresh(); Require(!Id("Release").IsEnabled && Id("Stop").IsEnabled, "Matrix release state failed.");
                        await CaptureAsync(main, output, $"synth-{language}-{theme}-{size}-released");
                        if (size == "reduced") CaptureSynthReleaseDetail((Button)Id("Release"), output, $"release-{language}-{theme}-disabled");
                        Click("Stop"); await Refresh(); Require(!Id("Stop").IsEnabled, "Matrix Stop stayed active.");
                    }
                    if (size == "reduced")
                    {
                        var chosen = (Button)Id("Wave1." + (int)Sound().Parameters["wave1"]);
                        chosen.Focus(NavigationMethod.Tab); await CaptureAsync(main, output, $"synth-{language}-{theme}-reduced-focus");
                        using var pointer = new Pointer(96020, PointerType.Mouse, true);
                        var root = chosen.TranslatePoint(new(chosen.Bounds.Width / 2, chosen.Bounds.Height / 2), main)!.Value;
                        chosen.RaiseEvent(new PointerPressedEventArgs(chosen, pointer, main, root, 0, new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
                        Require(chosen.Classes.Contains(":pressed"), "Routed pressed state was not delivered.");
                        await CaptureAsync(main, output, $"synth-{language}-{theme}-reduced-pressed");
                        chosen.RaiseEvent(new PointerReleasedEventArgs(chosen, pointer, main, root, 0, new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left)); await Refresh();
                    }
                }
            }
        }
        Json(output, "synth-matrix", matrix); Json(output, "synth-field-alignment", alignment); Json(output, "synth-composition-audit", composition);
        main.Width = 1280; main.Height = 800; main.UpdateLayout();
        var nativeCanvasBounds = Field<Canvas>(workspace, "_canvas").Bounds;
        workspace.State.Reflow(nativeCanvasBounds.Width, nativeCanvasBounds.Height);
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 640, 630)); workspace.Commit(); await Refresh();
        workspace.Open(WorkspaceState.NodeEditorId); await Refresh();
        ((ScrollViewer)Pane().Children[1]).Offset = default; await Refresh();
        bool nativePassed = false;
        presets.SelectedIndex = 4; await Refresh();
        object nativeResult = new { Result = "not-run" };
        var nativeEvents = new List<object>();
        if (native)
        {
            try
            {
                var input = new NativePointer.DrumInput(main);
                Json(output, "synth-native-ready", new { WaitingFor = "Activate the visible Seqvium check window", NoInputSent = true });
                main.Activate();
                // Readiness observes the real Windows foreground event, not cached Avalonia IsActive.
                // No input, focus forcing, retry or occlusion bypass happens while waiting.
                await input.WaitForForegroundAsync(TimeSpan.FromMinutes(4));
                await input.ActivateVisibleWindow();
                async Task NativeChange(Func<Task> action, Func<bool> achieved, string description)
                {
                    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    void Changed(WorkflowState state) { if (!state.Busy && achieved()) completion.TrySetResult(); }
                    workflow.Changed += Changed;
                    try { await action(); await completion.Task.WaitAsync(TimeSpan.FromSeconds(5)); await Refresh(); }
                    catch (TimeoutException) { throw new InvalidOperationException("Native state transition not delivered: " + description); }
                    finally { workflow.Changed -= Changed; }
                }
                async Task NativeHistory(string command)
                {
                    var button = main.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Workflow." + command);
                    bool Ready() => button.IsEnabled && !workflow.State.Busy && !Field<bool>(main, "_workflowActionBusy");
                    var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    void Availability(object? sender, AvaloniaPropertyChangedEventArgs e) { if (Ready()) ready.TrySetResult(); }
                    button.PropertyChanged += Availability;
                    try { if (Ready()) ready.TrySetResult(); await ready.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
                    finally { button.PropertyChanged -= Availability; }
                    object Evidence(string kind, object? source = null) => new { Event = kind, Command = command,
                        Source = source, WindowBounds = main.Bounds, main.WindowState, workflow.State.Generation,
                        ActionBusy = Field<bool>(main, "_workflowActionBusy"), workflow.State.UndoCount, workflow.State.RedoCount,
                        Target = input.TargetEvidence(button) };
                    EventHandler<PointerPressedEventArgs> press = (_, e) => nativeEvents.Add(Evidence("history-pointer-press", e.Source?.GetType().Name));
                    EventHandler<PointerReleasedEventArgs> release = (_, e) => nativeEvents.Add(Evidence("history-pointer-release", e.Source?.GetType().Name));
                    EventHandler<RoutedEventArgs> click = (_, _) => nativeEvents.Add(Evidence("history-click"));
                    main.AddHandler(InputElement.PointerPressedEvent, press, RoutingStrategies.Tunnel);
                    main.AddHandler(InputElement.PointerReleasedEvent, release, RoutingStrategies.Tunnel); button.Click += click;
                    try { nativeEvents.Add(Evidence("history-ready")); await input.Click(button); nativeEvents.Add(Evidence("history-input-complete")); }
                    finally { main.RemoveHandler(InputElement.PointerPressedEvent, press); main.RemoveHandler(InputElement.PointerReleasedEvent, release); button.Click -= click; }
                }
                // Replay the original failing two-action protocol once, recording delivery before
                // replacing its delay-based completion assumption with observed state transitions.
                var legacyButton = (Button)Id("Wave2.1");
                object LegacyEvidence(string kind) => new { Event = kind, UiGeneration = Field<long>(Pane(), "_generation"),
                    DocumentGeneration = workflow.State.Generation, Pending = Pane().PendingCommand.Status.ToString(),
                    Canonical = Sound().Parameters["wave2"], Displayed = (int)((SynthWaveform)Id("Oscillator2")).Wave,
                    Target = input.TargetEvidence(legacyButton) };
                legacyButton.AddHandler(InputElement.PointerPressedEvent, (_, e) => nativeEvents.Add(new { Event = "legacy-press", Local = e.GetPosition(legacyButton).ToString(), Source = e.Source?.GetType().Name, Basis = LegacyEvidence("before-handler") }), RoutingStrategies.Tunnel);
                legacyButton.Click += (_, _) => nativeEvents.Add(LegacyEvidence("legacy-click-after-handler"));
                await input.Exact(Id("Value.cutoff"), "2800");
                await Pane().PendingCommand; await workflow.ObserveAsync(); main.UpdateLayout(); await Task.Delay(35);
                Require(Sound().Parameters["cutoff"] == 2800, "Legacy cutoff failed.");
                nativeEvents.Add(LegacyEvidence("legacy-before-native-wave"));
                await input.Click(legacyButton);
                await Pane().PendingCommand; await workflow.ObserveAsync(); main.UpdateLayout(); await Task.Delay(35);
                nativeEvents.Add(LegacyEvidence("legacy-result-check"));
                Json(output, "synth-native-legacy-replay", new { Result = Sound().Parameters["wave2"] == 1 ? "passed-on-replay" : "reproduced-mismatch", Canonical = Sound().Parameters["wave2"] });
                await Refresh();
                foreach (string nativeSize in new[] { "reduced", "compact" })
                {
                    workspace.State.SetBounds(WorkspaceState.NodeEditorId, nativeSize == "reduced" ? new(0, 0, 640, 630) : new(0, 0, 360, 520)); workspace.Commit(); await Refresh();
                    ((ScrollViewer)Pane().Children[1]).Offset = default; await Refresh();
                foreach (int osc in Enumerable.Range(1, 2))
                {
                    string key = "wave" + osc;
                    if (Sound().Parameters[key] != 2)
                        Require(await workflow.SetSynthParametersAsync(target, workflow.State.Generation, Sound().Parameters,
                            Sound().Parameters.SetItem(key, 2)), "Known Saw fixture failed.");
                    await Refresh(); Require(Sound().Parameters[key] == 2, "Known Saw fixture is not Saw.");
                    var waveButton = (Button)Id($"Wave{osc}.0");
                    var display = (SynthWaveform)Id("Oscillator" + osc);
                    object BasisEvidence(string kind) => new { Event = kind, Size = nativeSize, Oscillator = osc,
                        UiGeneration = Field<long>(Pane(), "_generation"), DocumentGeneration = workflow.State.Generation,
                        Pending = Pane().PendingCommand.Status.ToString(), Canonical = Sound().Parameters[key],
                        Displayed = (int)display.Wave, Target = input.TargetEvidence(waveButton) };
                    EventHandler<PointerPressedEventArgs> press = (_, e) => nativeEvents.Add(new { Event = "pointer-press", Source = e.Source?.GetType().Name, Local = e.GetPosition(waveButton).ToString(), e.ClickCount, Basis = BasisEvidence("before-handler") });
                    EventHandler<RoutedEventArgs> clicked = (_, _) => nativeEvents.Add(BasisEvidence("click-after-handler"));
                    waveButton.AddHandler(InputElement.PointerPressedEvent, press, RoutingStrategies.Tunnel);
                    waveButton.Click += clicked;
                    try
                    {
                    nativeEvents.Add(BasisEvidence("before-native-sine"));
                    Require(((SynthWaveform)Id("Oscillator" + osc)).Wave == SynthVoice.Wave.Saw && ((Button)Id($"Wave{osc}.2")).Classes.Contains("primary"), "Saw visual fixture failed.");
                    await NativeChange(() => input.Click(waveButton), () => Sound().Parameters[key] == 0, "Sine " + osc);
                    void VerifyVisual(SynthVoice.Wave wave)
                    {
                        Require(((SynthWaveform)Id("Oscillator" + osc)).Wave == wave &&
                            ((Button)Id($"Wave{osc}.{(int)wave}")).Classes.Contains("primary") &&
                            ((Button)Id($"Wave{osc}.{(int)wave}")).BorderThickness.Top == 2, "Native visual state mismatch.");
                    }
                    VerifyVisual(SynthVoice.Wave.Sine); nativeEvents.Add(BasisEvidence("canonical-and-visual-sine"));
                    await CaptureAsync(main, output, "synth-native-" + nativeSize + "-sine-" + osc);
                    await NativeChange(() => NativeHistory("Undo"), () => Sound().Parameters[key] == 2, "Undo Saw " + osc);
                    VerifyVisual(SynthVoice.Wave.Saw); nativeEvents.Add(BasisEvidence("undo-saw"));
                    await NativeChange(() => NativeHistory("Redo"), () => Sound().Parameters[key] == 0, "Redo Sine " + osc);
                    VerifyVisual(SynthVoice.Wave.Sine); nativeEvents.Add(BasisEvidence("redo-sine"));
                    Json(output, "synth-native-events", nativeEvents);
                    }
                    finally
                    {
                        waveButton.RemoveHandler(InputElement.PointerPressedEvent, press); waveButton.Click -= clicked;
                    }
                }
                }
                var heldParameters = Sound().Parameters.SetItem("attack", 8).SetItem("decay", 100).SetItem("sustain", .6m).SetItem("release", 800);
                Require(await workflow.SetSynthParametersAsync(target, workflow.State.Generation, Sound().Parameters, heldParameters), "Preview envelope fixture failed."); await Refresh();
                await input.Select((ComboBox)Id("Note"), 57); await Refresh();
                Require(((ComboBox)Id("Note")).SelectedIndex == 57, "Native note selection failed.");
                before = workflow.State.Snapshot;
                await input.Click(Id("Hold")); await Refresh(); await PreviewState(true, "native-held");
                Require(workflow.SamplerPreviewPlaying && ((Button)Id("Hold")).Classes.Contains("active"), "Native held preview/state failed.");
                await CaptureAsync(main, output, "synth-native-preview-held");
                var releaseDevice = Field<WorkflowOutput>(workflow, "_noteOutput");
                await input.Click(Id("Release")); await Refresh();
                Require(releaseDevice.Sampler.ActiveVoices > 0 && !releaseDevice.Lifetime.Joined, "Native NoteOff hard-stopped instead of rendering release.");
                nativeEvents.Add(new { Event = "release-tail-active", releaseDevice.Sampler.ActiveVoices, releaseDevice.Lifetime.Joined });
                Require(!((Button)Id("Hold")).Classes.Contains("active") && !Id("Release").IsEnabled, "Native NoteOff state failed.");
                await PreviewState(false, "native-release-complete");
                Require(!workflow.SamplerPreviewPlaying, "Native NoteOff did not finish.");
                await Refresh(); Require(Id("Stop").IsEnabled && !Id("Release").IsEnabled, "Completed preview lost its cleanup action.");
                await input.Click(Id("Stop")); await Refresh();
                Require(releaseDevice.Lifetime.Joined && !Id("Stop").IsEnabled, "Native released-output Stop did not join.");
                await input.Click(Id("Hold")); await Refresh(); await PreviewState(true, "native-hard-stop-held");
                var nativeDevice = Field<WorkflowOutput>(workflow, "_noteOutput");
                await input.Click(Id("Stop")); await Refresh();
                Require(nativeDevice.Lifetime.Joined && !workflow.SamplerPreviewPlaying && before == workflow.State.Snapshot, "Native Stop/join/isolation failed.");
                nativePassed = true;
                nativeResult = new { Result = "passed", Checks = "Native reduced and compact editors: Cutoff; Saw-to-Sine/canonical/visual/Undo/Redo for both oscillators; note selector, Hold active state, NoteOff natural completion and Stop/join; routed fixture setup" };
            }
            catch (Exception error) { await CaptureAsync(main, output, "synth-native-failure"); nativeResult = new { Result = "refused-or-failed", Reason = error.Message }; }
            Json(output, "synth-native-events", nativeEvents);
        }
        object physical = new { Result = "not-run" };
        if (audio)
        {
            try
            {
                before = workflow.State.Snapshot; Click("Hold"); await Refresh(); await PreviewState(true, "routed-held");
                var device = Field<WorkflowOutput>(workflow, "_noteOutput");
                Require(workflow.SamplerPreviewPlaying && device.ReadFailure() is null, "Physical held preview failed.");
                await CaptureAsync(main, output, "synth-routed-preview-held");
                Click("Release"); await Refresh();
                await PreviewState(false, "routed-release-complete");
                Require(!workflow.SamplerPreviewPlaying && device.Sampler.ActiveVoices == 0 && device.ReadFailure() is null, "Physical release failed.");
                Click("Hold"); await Refresh(); await PreviewState(true, "routed-hard-stop-held"); device = Field<WorkflowOutput>(workflow, "_noteOutput");
                Click("Stop"); await Refresh(); Require(device.Lifetime.Joined && before == workflow.State.Snapshot, "Preview Stop/join or isolation failed.");
                physical = new { Result = "passed", device.Description, SubjectiveListening = false };
            }
            catch (Exception error) { physical = new { Result = "failed", Reason = error.Message }; }
        }
        Json(output, "synth-input-audio", new { Native = nativeResult, Physical = physical });
        string saved = Path.Combine(output, "Synth F1.seqvium"); var parameters = Sound().Parameters;
        Require(await workflow.SaveAsync(saved), "Synth Save failed.");
        string relocated = Path.Combine(output, "portable"); Directory.CreateDirectory(relocated);
        Require(await workflow.SaveAsync(Path.Combine(relocated, "Synth F1.seqvium")), "Save As failed.");
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed.");
        Require(await workflow.OpenAsync(Path.Combine(relocated, "Synth F1.seqvium"), UnsavedDecision.Discard), "Portable Open failed.");
        Require(workflow.State.Snapshot.State.Sounds.Single().Parameters.OrderBy(p => p.Key).SequenceEqual(parameters.OrderBy(p => p.Key)), "Portable parameters changed.");
        if (nativePassed) Require(workflow.State.Snapshot.State.Sounds.Single().Parameters["wave1"] == 0 &&
            workflow.State.Snapshot.State.Sounds.Single().Parameters["wave2"] == 0, "Portable native-selected Sine changed.");
        Json(output, "synth-workflow", new { Result = "passed", RoutedInput = true, FactoryWaves = 4,
            Checks = "Rack creation/open, Source double-click, chord, waveform/mix/detune/filter, exact/invalid/draft, ADSR commit/cancel, presets, Save As/New/Open", NativeInput = nativeResult });
        // A canonical, locally saved fixture exercises real long-name layout without adding a rename feature.
        var longSnapshot = workflow.State.Snapshot with { State = workflow.State.Snapshot.State with {
            Sounds = [.. workflow.State.Snapshot.State.Sounds.Select(sound => sound with { Name = "Очень длинный щипковый звук для проверки отображения" })] } };
        var longDocument = ProjectDocument.Reopen(longSnapshot);
        string longPath = Path.Combine(output, "long-name.seqvium");
        try { ProjectPersistence.Save(longDocument, longPath); } finally { longDocument.Close(); }
        Require(await workflow.OpenAsync(longPath, UnsavedDecision.Discard), "Long-name canonical fixture failed.");
        var longAttachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var longSource = longAttachment.Sources.Single();
        Invoke(main, "OpenNodeEditor", new NodeEditorTarget(workflow.State.Document.LifecycleId, longAttachment.Id, longAttachment.GraphId, longSource.NodeId));
        main.Width = 900; main.Height = 700; main.UpdateLayout();
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 360, 520)); workspace.Open(WorkspaceState.NodeEditorId); workspace.Commit(); await Refresh();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync(); await Refresh();
                ((ScrollViewer)Pane().Children[1]).Offset = default; await Refresh();
                await CaptureAsync(main, output, $"synth-{language}-{theme}-compact-long-name");
                composition.Add(SynthCompositionAudit(Pane(), $"{language}-{theme}-compact-long-name"));
            }
        }
        Json(output, "synth-composition-audit", composition);
        Json(output, "synth-render-inventory", Directory.EnumerateFiles(output, "*.png").Where(path => !path.EndsWith(".screen.png", StringComparison.Ordinal)).Select(Path.GetFileName).Order().ToArray());
        Require(!native || nativePassed, "Required native Synth checks did not pass; see synth-input-audio.json and native events.");
    }
}
