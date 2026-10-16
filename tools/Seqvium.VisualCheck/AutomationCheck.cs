// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    private static async Task AutomationAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        main.WindowState = WindowState.Normal;
        main.Width = 1400;
        main.Height = 900;
        main.UpdateLayout(); await Task.Delay(60); main.UpdateLayout();
        var initialBounds = Field<Canvas>(workspace, "_canvas").Bounds;
        workspace.State.Reflow(initialBounds.Width, initialBounds.Height);
        foreach (var id in workspace.State.Panes.Where(p => p.Visibility == PaneVisibility.Visible)
                     .Select(p => p.InstanceId).ToArray())
            workspace.State.Hide(id);
        workspace.Open(WorkspaceState.ArrangementId);
        workspace.State.SetBounds(WorkspaceState.ArrangementId, new(0, 0, 1020, 650));
        workspace.Commit();
        main.UpdateLayout();
        var pane = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        var canvas = pane.Canvas;
        ArrangementEditBasis Basis() => ArrangementEditBasis.From(workflow.State);

        async Task Refresh()
        {
            await pane.PendingCommand;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            pane.Refresh();
            main.UpdateLayout();
        }

        Button Button(string id) => pane.GetVisualDescendants().OfType<Button>()
            .Single(b => AutomationProperties.GetAutomationId(b) == id);

        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));

        Control Popup(string id) => pane.AutomationPropertiesContent!.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == id);

        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                await Refresh();
                Click(Button("Arrangement.Automation"));
                await Refresh();
                Require(!Popup("Automation.Add").IsEnabled, "Empty target list offers an invalid creation.");
                await CaptureAsync(main, output, $"empty-{language}-{theme}");
                await CapturePopupAsync(pane.AutomationPropertiesContent!, output, $"empty-targets-{language}-{theme}");
                pane.CancelGesture();
            }
        }

        Require(
            await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
                "Held chord"), "Pattern creation failed.");
        BeatEditBasis BeatBasis() => BeatSteps.Resolve(workflow.State).Basis!;
        Require(await workflow.AddDrumVoiceAsync(BeatBasis(), "Sweep", SynthVoice.Algorithm), "Synth creation failed.");
        Require(await workflow.ResizeBeatAsync(BeatBasis(), 4), "Pattern resize failed.");
        var part = BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single();
        Require(await workflow.EditPianoNotesAsync(BeatBasis(), part.Id, [
            .. new[] { 48, 52, 55 }.Select(p =>
                new NoteEvent(Id<NoteEvent>.New(), new(0), new(15360000), p, .55m))
        ]), "Chord entry failed.");
        Require(await workflow.EditMixerAsync(new(workflow.State.Document.LifecycleId, workflow.State.Generation),
            "Scenario setup", e =>
            {
                e.InitializeMixer();
                e.SetSoundParameters(part.SoundId, SynthVoice.Defaults.SetItem("attack", 50).SetItem("decay", 0)
                    .SetItem("sustain", 1).SetItem("release", 250).SetItem("mix", .1m).SetItem("cutoff", 6000));
                e.AddArrangementClip(workflow.State.Snapshot.State.Placements.Single().Id, new(0), 0, 2);
                e.SetArrangementTrack(0, "Held chord", 0, false, false);
            }), "Song setup failed.");
        workspace.State.Hide(WorkspaceState.NodeEditorId);
        workspace.State.Hide(WorkspaceState.BeatId);
        workspace.Open(WorkspaceState.ArrangementId);
        workspace.State.Dock(WorkspaceState.ArrangementId, PaneDock.Floating);
        workspace.State.SetBounds(WorkspaceState.ArrangementId, new(0, 0, 1020, 650));
        workspace.Commit();
        await Refresh();
        Invoke(pane, "FitSong");
        await Refresh();

        async Task AddLane(int selectedIndex)
        {
            Click(Button("Arrangement.Automation"));
            await Refresh();
            ((ComboBox)Popup("Automation.Target")).SelectedIndex = selectedIndex;
            int undo = workflow.State.UndoCount;
            Click((Button)Popup("Automation.Add"));
            await Refresh();
            Require(workflow.State.UndoCount == undo + 1, "Creation is not one Undo.");
        }

        await AddLane(0);
        await AddLane(0);

        AutomationLane Lane(AutomationParameter parameter) =>
            workflow.State.Snapshot.State.AutomationLanes.Single(l => l.Target.Parameter == parameter);

        var cutoff = Lane(AutomationParameter.SynthCutoff);
        var volume = Lane(AutomationParameter.MixerVolume);
        Require(await workflow.EditAutomationAsync(Basis(), cutoff with
        {
            Points =
            [
                new(cutoff.Points[0].Id, new(0), ParameterAutomation.Normalize(AutomationParameter.SynthCutoff, 90),
                    AutomationTransition.Smooth),
                new(Id<AutomationPoint>.New(), new(15360000),
                    ParameterAutomation.Normalize(AutomationParameter.SynthCutoff, 12000), AutomationTransition.Smooth),
                new(cutoff.Points[1].Id, new(30720000),
                    ParameterAutomation.Normalize(AutomationParameter.SynthCutoff, 400))
            ]
        }), "Cutoff shape failed.");
        Require(await workflow.EditAutomationAsync(Basis(), volume with
        {
            Points =
            [
                new(volume.Points[0].Id, new(0), 0, AutomationTransition.Smooth),
                new(Id<AutomationPoint>.New(), new(7680000),
                    ParameterAutomation.Normalize(AutomationParameter.MixerVolume, 0)),
                new(Id<AutomationPoint>.New(), new(23040000),
                    ParameterAutomation.Normalize(AutomationParameter.MixerVolume, 0), AutomationTransition.Smooth),
                new(volume.Points[1].Id, new(30720000), 0)
            ]
        }), "Volume shape failed.");
        await Refresh();

        Point Position(AutomationLane lane, AutomationPoint point) =>
            canvas.AutomationPosition(lane, point);

        var driver = new ControlInputDriver(main, true, output);
        cutoff = Lane(AutomationParameter.SynthCutoff);
        var middle = cutoff.Points[1];
        var from = Position(cutoff, middle);
        var to = from + new Vector(20, 5);
        int count = workflow.State.UndoCount;
        await driver.TraceDrag(canvas, [from, to]);
        await Refresh();
        Require(workflow.State.UndoCount == count + 1 && Lane(AutomationParameter.SynthCutoff).Points[1] != middle,
            "Point drag did not commit once.");
        Require(await workflow.UndoAsync(), "Drag Undo failed.");
        await Refresh();
        Require(Lane(AutomationParameter.SynthCutoff).Points[1] == middle, "Drag Undo lost exact values.");
        var snapshot = workflow.State.Snapshot;
        await driver.TraceDrag(canvas, [from, to], escape: true);
        await Refresh();
        Require(workflow.State.Snapshot == snapshot, "Escape accepted a point edit.");
        await driver.TraceDrag(canvas, [from, to], captureLoss: true);
        await Refresh();
        Require(workflow.State.Snapshot == snapshot, "Capture loss accepted a point edit.");
        // Empty space adds one point through the actual timeline gesture, then Undo restores the fixture.
        await driver.TraceDrag(canvas, [new(from.X - 80, from.Y + 12), new(from.X - 70, from.Y + 6)]);
        await Refresh();
        Require(Lane(AutomationParameter.SynthCutoff).Points.Length == 4, "Timeline point creation failed.");
        Require(await workflow.UndoAsync(), "New point Undo failed.");
        await Refresh();
        var alignment = new List<object>();
        var matrix = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int windowWidth in new[] { 1400, 900, 640 })
                {
                    bool narrow = windowWidth < 1400;
                    var navigation = Field<LeftPanelZone>(main, "_navigation");
                    if (windowWidth == 640) navigation.Patterns.Hide(); else navigation.Patterns.Restore();
                    main.Width = windowWidth;
                    main.Height = windowWidth == 640 ? 480 : narrow ? 700 : 900;
                    main.UpdateLayout();
                    await Task.Delay(40); main.UpdateLayout();
                    var available = Field<Canvas>(workspace, "_canvas").Bounds;
                    workspace.State.Reflow(available.Width, available.Height);
                    workspace.State.SetBounds(WorkspaceState.ArrangementId,
                        narrow ? new(0, 0, 440, 480) : new(0, 0, 1020, 650));
                    workspace.Commit();
                    await Refresh();
                    Invoke(pane, "FitSong");
                    await Refresh();
                    foreach (var parameter in new[]
                                 { AutomationParameter.SynthCutoff, AutomationParameter.MixerVolume })
                    {
                        var lane = Lane(parameter);
                        canvas.SelectAutomation(lane.Id, lane.Points[1].Id);
                        if (windowWidth == 640) Field<ScrollViewer>(pane, "_scroll").Offset = new(0, lane.Lane * HostVisual.Metric(canvas, "Playlist.LaneHeight"));
                        await Refresh();
                        canvas.RaiseEvent(new KeyEventArgs
                            { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                        await Refresh();
                        var body = pane.AutomationPropertiesContent!;
                        await Task.Delay(70);
                        body.UpdateLayout();
                        foreach (var field in body.GetVisualDescendants().OfType<TextBox>().Where(f => AutomationProperties.GetAutomationId(f)?.StartsWith("Automation.", StringComparison.Ordinal) == true))
                        {
                            var row = (Grid)field.Parent!;
                            alignment.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().First(), row,
                                $"{language}-{theme}-{narrow}-{parameter}-{AutomationProperties.GetAutomationId(field)}"));
                        }

                        string name = $"{language}-{theme}-{(windowWidth == 640 ? "minimum" : narrow ? "narrow" : "ordinary")}-{parameter}";
                        await CaptureAsync(main, output, name);
                        await CapturePopupAsync(body, output, name + "-properties");
                        snapshot = workflow.State.Snapshot;
                        ((TextBox)Popup("Automation.Value")).Text = "999999";
                        Click((Button)Popup("Automation.Apply"));
                        await Refresh();
                        Require(
                            workflow.State.Snapshot == snapshot &&
                            !string.IsNullOrEmpty(((TextBlock)Popup("Automation.Error")).Text),
                            "Invalid exact input changed music.");
                        pane.CancelGesture();
                        matrix.Add(new { Name = name, Pane = pane.Bounds, main.RenderScaling });
                    }
                }
            }
        }

        main.Width = 1400;
        main.Height = 900;
        main.UpdateLayout();
        workspace.State.SetBounds(WorkspaceState.ArrangementId, new(0, 0, 1020, 650));
        workspace.Commit();
        await Refresh();
        Invoke(pane, "FitSong");
        await Refresh();
        cutoff = Lane(AutomationParameter.SynthCutoff);
        canvas.SelectAutomation(cutoff.Id, cutoff.Points[1].Id);
        await Refresh();
        canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        await Refresh();
        ((TextBox)Popup("Automation.Value")).Text = "10000";
        var beforeExact = workflow.State.Snapshot;
        ((TextBox)Popup("Automation.Time")).Text = "0:1:0";
        Click((Button)Popup("Automation.Apply")); await Refresh();
        Require(workflow.State.Snapshot == beforeExact, "Invalid musical position changed the project.");
        ((TextBox)Popup("Automation.Time")).Text = "5:2:17";
        count = workflow.State.UndoCount;
        Click((Button)Popup("Automation.Apply"));
        await Refresh();
        Require(workflow.State.UndoCount == count + 1 && Math.Abs(ParameterAutomation.Physical(
                AutomationParameter.SynthCutoff,
                (double)Lane(AutomationParameter.SynthCutoff).Points[1].Value) - 10000) < .001,
            "Exact value/history failed.");
        Require(Lane(AutomationParameter.SynthCutoff).Points[1].Position.Ticks == 16320017,
            "Exact bar:beat:tick did not preserve ticks.");
        Require(await workflow.UndoAsync() && await workflow.UndoAsync(redo: true) && await workflow.UndoAsync(),
            "Exact history failed.");
        await Refresh();
        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                var input = new NativePointer.DrumInput(main);
                await input.ActivateVisibleWindow();
                cutoff = Lane(AutomationParameter.SynthCutoff);
                from = Position(cutoff, cutoff.Points[1]);
                count = workflow.State.UndoCount;
                await input.TraceDrag(canvas, [from, from + new Vector(10, 4)]);
                await Refresh();
                Require(workflow.State.UndoCount == count + 1, "Native point drag did not commit.");
                Require(await workflow.UndoAsync(), "Native Undo restoration failed.");
                await Refresh();
                nativeResult = new { Result = "passed", Scope = "Windows point drag; setup and history routed" };
            }
            catch (Exception error)
            {
                nativeResult = new { Result = "refused-or-failed", Reason = error.Message };
            }
        }

        var samples = new List<object>();
        foreach (string scenario in new[] { "cutoff-sweep", "mixer-fade", "combined" })
        {
            foreach (var lane in workflow.State.Snapshot.State.AutomationLanes)
                await workflow.EditAutomationAsync(Basis(), lane with
                {
                    Enabled = scenario == "combined" ||
                              (scenario == "cutoff-sweep") == (lane.Target.Parameter == AutomationParameter.SynthCutoff)
                });
            string project = Path.Combine(output, scenario + ".seqvium"),
                wave = Path.Combine(output, scenario + ".wav");
            Require(await workflow.SaveAsync(project), "Scenario Save failed.");
            Require(await workflow.ExportSongAsync(Basis(), wave), "Scenario export failed.");
            using var pcm = WavDecoder.Decode(File.ReadAllBytes(wave));
            double peak = 0;
            for (int frame = 0; frame < pcm.Frames; frame++) peak = Math.Max(peak, Math.Abs(pcm.Sample(frame, 0)));
            Require(peak is > .0001 and < 1, "Example is silent or overloaded.");
            samples.Add(new { Scenario = scenario, pcm.Frames, pcm.SampleRate, Peak = peak });
        }

        string saved = Path.Combine(output, "combined.seqvium");
        Require(
            await workflow.NewAsync(UnsavedDecision.Discard) &&
            await workflow.OpenAsync(saved, UnsavedDecision.Discard), "Portable reopen failed.");
        Require(
            workflow.State.Snapshot.State.AutomationLanes.Length == 2 &&
            workflow.State.Execution?.Execution.Playing != true, "Automation reopen was not intact/silent.");
        object physical = new { Result = "not-run" };
        if (audio)
        {
            try
            {
                if (!workflow.State.SongMode) Require(await workflow.SetSongModeAsync(true), "Song mode failed.");
                Require(await workflow.PlayAsync(), workflow.State.Detail ?? "Physical Play failed.");
                for (int i = 0; i < 250 && workflow.State.Execution?.Execution.Playing != true; i++)
                {
                    await Task.Delay(20);
                    await workflow.ObserveAsync();
                }

                Require(workflow.State.Execution?.Execution.Playing == true, "Physical playback did not start.");
                var epoch = workflow.State.Execution!.Execution.Epoch;
                string? device = workflow.State.Device;
                var lane = Lane(AutomationParameter.SynthCutoff);
                Require(
                    await workflow.EditAutomationAsync(Basis(),
                        lane with { Points = lane.Points.SetItem(1, lane.Points[1] with { Value = .9m }) }),
                    "Live curve edit failed.");
                for (int i = 0; i < 1000; i++)
                {
                    await Task.Delay(20);
                    await workflow.ObserveAsync();
                    Require(workflow.State.StatusKey != "Workflow.DeviceFault" && workflow.State.Execution is not null,
                        workflow.State.Detail ?? workflow.State.StatusKey);
                    if (!workflow.State.Execution!.Execution.Playing) break;
                }

                var ended = workflow.State.Execution!.Execution;
                using var expected = ArrangementPreparation.Prepare(workflow.State.Document,
                    workflow.State.ExecutionSampleRate, 2, 1024);
                Require(
                    ended.Transport == SamplerTransportState.Ended && ended.Position == expected.EndFrame &&
                    ended.Voices == 0 && ended.Epoch == epoch,
                    "Physical Song did not finish at its expected boundary.");
                physical = new
                {
                    Result = "passed", Device = device, Execution = ended, LiveCurveEdit = true,
                    SubjectiveListening = false
                };
                await workflow.StopAsync();
                Require(await workflow.UndoAsync(), "Live edit restoration failed.");
            }
            catch (Exception error)
            {
                await workflow.StopAsync(true);
                physical = new { Result = "failed", Reason = error.Message };
            }
        }

        Json(output, "automation", new
        {
            RoutedGestures = true, ExactHistory = true, PortableSaveOpen = true,
            Matrix = matrix, Alignment = alignment, Samples = samples, Native = nativeResult, Physical = physical
        });
    }
}
