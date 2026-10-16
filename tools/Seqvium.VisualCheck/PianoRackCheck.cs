// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    private static async Task PianoRackAsync(MainWindow main, string output, bool baseline, bool geometryOnly)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var input = new NativePointer.DrumInput(main);
        main.WindowState = WindowState.Normal;
        main.Width = 1400;
        main.Height = 900;
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
            "Piano Rack check"), "Fixture Pattern failed.");
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Pattern Pattern() => BeatSteps.Resolve(workflow.State).Pattern!;
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Steps", SynthVoice.Algorithm), "Step source failed.");
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Notes", SynthVoice.Algorithm), "Note source failed.");
        var stepsPart = Pattern().Parts[0].Id;
        var notesPart = Pattern().Parts[1].Id;
        Require(await workflow.ToggleBeatStepAsync(Basis(), stepsPart, 0), "Step fixture failed.");
        Require(await workflow.EditPianoNotesAsync(Basis(), notesPart,
        [
            new(Id<NoteEvent>.New(), new(0), new(960000), 60, .1m),
            new(Id<NoteEvent>.New(), new(3360000), new(480000), 67, .1m)
        ]), "Note fixture failed.");
        workspace.State.Hide(WorkspaceState.NodeEditorId);
        workspace.State.Hide(WorkspaceState.ArrangementId);
        workspace.Open(WorkspaceState.BeatId);
        workspace.State.SetBounds(WorkspaceState.BeatId, new(5, 5, 1100, 390));
        workspace.Commit();
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        workspace.Open(WorkspaceState.PianoId);
        workspace.State.SetBounds(WorkspaceState.PianoId, new(10, 10, 1080, 580));
        workspace.Commit();
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        piano.OpenPart(notesPart);
        var snap = Field<ComboBox>(piano, "_snap");
        var length = Field<ComboBox>(piano, "_length");
        var comboRows = new List<object>();
        var rackRows = new List<object>();
        int observations = 0;
        void Changed(WorkflowState _) => observations++;
        workflow.Changed += Changed;

        async Task Settle()
        {
            await workflow.ObserveAsync();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            main.UpdateLayout();
            await Task.Delay(80);
        }

        await Settle();
        main.Activate();
        await input.ActivateVisibleWindow();
        Require(await workflow.PlayAsync(), "Physical Pattern failed: " + workflow.State.Detail);
        await Task.Delay(300);
        var device = Field<WorkflowOutput>(workflow, "_output");
        var epoch = device.Sampler.TransportEpoch;
        try
        {
            foreach (var language in geometryOnly
                         ? Array.Empty<HostLanguage>()
                         : baseline
                             ? new[] { HostLanguage.English }
                             : new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in baseline ? new[] { HostTheme.Dark } : new[] { HostTheme.Dark, HostTheme.Light })
            foreach (int width in baseline ? new[] { 1080 } : new[] { 1080, 520 })
            {
                if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                workspace.State.SetBounds(WorkspaceState.PianoId, new(10, 10, width, 580));
                workspace.Commit();
                await Settle();
                foreach (var combo in new[] { snap, length })
                foreach (int index in baseline ? new[] { 1 } : new[] { 1, 3, 0 })
                {
                    var snapshot = workflow.State.Snapshot;
                    int undo = workflow.State.UndoCount;
                    int resets = 0, notifications = observations;

                    void Property(object? _, AvaloniaPropertyChangedEventArgs e)
                    {
                        if (e.Property == ItemsControl.ItemsSourceProperty) resets++;
                    }

                    combo.PropertyChanged += Property;
                    try
                    {
                        await input.Click(combo);
                        Require(combo.IsDropDownOpen, "Native ComboBox did not open.");
                        var item = combo.ContainerFromIndex(index) as ComboBoxItem ??
                                   throw new InvalidOperationException("Popup item was not realized.");
                        await input.Wheel(item, new(item.Bounds.Width / 2, item.Bounds.Height / 2), 0);
                        var origin = item.PointToScreen(default);
                        var source = combo.ItemsSource;
                        // Observe actual playback publications while the OS pointer stays over a row.
                        await Task.Delay(index == 1 ? 2200 : 200);
                        bool sameContainer = ReferenceEquals(item, combo.ContainerFromIndex(index));
                        bool stable = sameContainer && item.PointToScreen(default) == origin &&
                                      ReferenceEquals(source, combo.ItemsSource);
                        string name = $"{language}-{theme}-{width}-{(combo == snap ? "snap" : "length")}-{index}";
                        if (index == 1)
                            Require(
                                NativeScreen.Capture(main, Path.Combine(output, name + "-popup.png"))
                                    .StartsWith("passed"), "Native capture refused.");
                        if (!baseline)
                            Require(stable && resets == 0 && combo.IsDropDownOpen,
                                "Playing transport rebuilt/moved an open ComboBox.");
                        Json(output, "held-" + name, new
                        {
                            resets,
                            Notifications = observations - notifications,
                            stable,
                            sameContainer,
                            Open = combo.IsDropDownOpen
                        });
                        try
                        {
                            await input.Click((Control)combo.ContainerFromIndex(index)!);
                        }
                        catch (InvalidOperationException error) when (baseline)
                        {
                            Json(output, "baseline-click-refusal-" + name, new { error.Message });
                        }

                        bool accepted = !combo.IsDropDownOpen && combo.SelectedIndex == index;
                        comboRows.Add(new
                        {
                            language,
                            theme,
                            width,
                            Combo = combo == snap ? "snap" : "length",
                            index,
                            Resets = resets,
                            Notifications = observations - notifications,
                            stable,
                            sameContainer,
                            accepted
                        });
                        Json(output, "combo", comboRows);
                        if (!baseline)
                        {
                            Require(accepted, "Native selection was not accepted.");
                            long expected = combo == snap
                                ? new long[] { 960000, 480000, 240000, 120000, 320000, 1 }[index]
                                : new long[] { 240000, 480000, 960000, 1920000, 3840000 }[index];
                            Require((combo == snap ? piano.Canvas.SnapTicks : piano.Canvas.DefaultDuration) == expected,
                                "Native selector did not update the canvas.");
                            piano.OpenPart(stepsPart);
                            piano.OpenPart(notesPart);
                            await Settle();
                            Require(
                                combo.SelectedIndex == index && workflow.State.Snapshot == snapshot &&
                                workflow.State.UndoCount == undo,
                                "View selection changed music/history or was lost on observation/channel switching.");
                        }

                        if (combo.IsDropDownOpen)
                        {
                            input.Key(0x1B);
                            await Task.Delay(100);
                        }
                    }
                    finally
                    {
                        combo.PropertyChanged -= Property;
                    }
                }
            }

            Require(
                device.Sampler.TransportEpoch == epoch && device.Sampler.ReadStatus().Playing &&
                device.ReadFailure() is null,
                "Selectors interrupted Pattern playback.");
            if (!baseline && !geometryOnly)
            {
                foreach (var combo in new[] { snap, length })
                {
                    double height = combo.MaxDropDownHeight;
                    combo.MaxDropDownHeight = 70;
                    await input.Click(combo);
                    var top = (Control)combo.ContainerFromIndex(0)!;
                    await input.Wheel(top, new(10, top.Bounds.Height / 2), -360);
                    int last = combo.Items.Count - 1;
                    await input.Click((Control)combo.ContainerFromIndex(last)!);
                    Require(combo.SelectedIndex == last && !combo.IsDropDownOpen,
                        "Native scrolling Popup selection failed.");
                    Require(combo == snap ? piano.Canvas.SnapTicks == 1 : piano.Canvas.DefaultDuration == 3840000,
                        "Free snap/whole-note length did not reach the canvas.");
                    combo.MaxDropDownHeight = height;
                }

                Json(output, "native-popup-scroll",
                    new { SnapOff = true, WholeNote = true, NativeWheelAndMouse = true });
            }

            await workflow.StopAsync();
            workspace.State.Hide(WorkspaceState.PianoId);
            workspace.Open(WorkspaceState.BeatId);
            foreach (var language in baseline
                         ? new[] { HostLanguage.English }
                         : new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in baseline ? new[] { HostTheme.Dark } : new[] { HostTheme.Dark, HostTheme.Light })
            foreach (int bars in baseline ? new[] { 1 } : new[] { 1, 2, 4, 8, 32 })
            {
                if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                if (bars > 1)
                {
                    Require(await workflow.ResizeBeatAsync(Basis(), bars), "Resize fixture failed.");
                    Require(await workflow.EditPianoNotesAsync(Basis(), notesPart,
                        [
                            new(Id<NoteEvent>.New(), new(0), new(960000), 60, .1m),
                            new(Id<NoteEvent>.New(), new(3360000), new(960000), 67, .1m),
                            new(Id<NoteEvent>.New(), new(bars * BeatSteps.BarTicks - 240000), new(240000), 72, .1m)
                        ]),
                        "Cross-bar fixture failed.");
                }
                else if (Pattern().Length.Ticks != BeatSteps.BarTicks)
                {
                    Require(await workflow.EditPianoNotesAsync(Basis(), notesPart,
                        [new(Id<NoteEvent>.New(), new(0), new(960000), 60, .1m)]), "Shorten notes failed.");
                    Require(await workflow.ResizeBeatAsync(Basis(), bars), "Shorten fixture failed.");
                }

                foreach (int width in new[] { 1100, 650, 420 })
                {
                    workspace.State.SetBounds(WorkspaceState.BeatId, new(5, 5, width, 390));
                    workspace.Commit();
                    rack.Refresh();
                    await Settle();
                    var scroll = Field<ScrollViewer>(rack, "_rackScroll");
                    scroll.Offset = default;
                    main.UpdateLayout();
                    var buttons = rack.GetVisualDescendants().OfType<ToggleButton>()
                        .Where(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b)
                            ?.StartsWith($"Beat.Step.{stepsPart}.") == true).ToArray();
                    var thumb = rack.GetVisualDescendants().OfType<PartNoteThumbnail>().Single();
                    var start = thumb.TranslatePoint(default, rack)!.Value;
                    var xs = buttons.Select(b => b.TranslatePoint(default, rack)!.Value.X).ToArray();
                    double end = xs[^1] + buttons[^1].Bounds.Width;
                    rackRows.Add(new
                    {
                        language,
                        theme,
                        bars,
                        width,
                        Start = start.X,
                        StepStart = xs[0],
                        ThumbnailWidth = thumb.Bounds.Width,
                        StepWidth = end - xs[0],
                        QuarterStarts = xs.Where((_, i) => i % 4 == 0),
                        scroll.Extent,
                        scroll.Viewport
                    });
                    Json(output, "rack", rackRows);
                    if (!baseline)
                    {
                        Require(Math.Abs(start.X - xs[0]) < .1 && Math.Abs(start.X + thumb.Bounds.Width - end) < .1,
                            "Rendered Rack endpoints do not share one time scale.");
                        var ruler = Field<List<TextBlock>>(rack, "_rulerCells");
                        for (int i = 0; i < buttons.Length; i++)
                        {
                            double rulerX = ruler[i].TranslatePoint(default, rack)!.Value.X;
                            Require(Math.Abs(rulerX - xs[i]) < .1 &&
                                    Math.Abs(start.X + thumb.Timeline.X(i * BeatSteps.StepTicks) - xs[i]) < .1,
                                "Rendered step/ruler/thumbnail subdivision or bar boundary diverged.");
                        }

                        if (scroll.Extent.Width > scroll.Viewport.Width)
                        {
                            double before = scroll.Offset.X;
                            var bar = scroll.GetVisualDescendants().OfType<ScrollBar>().Single(b =>
                                b.IsEffectivelyVisible && b.Orientation == Avalonia.Layout.Orientation.Horizontal);
                            var handle = bar.GetVisualDescendants().OfType<Thumb>().Single();
                            var at = new Point(handle.Bounds.Width / 2, handle.Bounds.Height / 2);
                            await input.Drag(handle, at, at + new Vector(20, 0));
                            main.UpdateLayout();
                            // A queued shell refresh may replace this read-only drawing during native input.
                            thumb = rack.GetVisualDescendants().OfType<PartNoteThumbnail>().Single();
                            double delta = scroll.Offset.X - before;
                            Require(delta > 0, "Native shared Rack horizontal scroll failed.");
                            double moved = thumb.TranslatePoint(default, rack)!.Value.X;
                            Json(output, "scroll-progress", new
                            {
                                language,
                                theme,
                                bars,
                                width,
                                Before = start.X,
                                After = moved,
                                delta,
                                Step = buttons[0].TranslatePoint(default, rack)!.Value.X,
                                Ruler = ruler[0].TranslatePoint(default, rack)!.Value.X
                            });
                            Require(Math.Abs(moved - start.X + delta) <= 1 &&
                                    Math.Abs(buttons[0].TranslatePoint(default, rack)!.Value.X - moved) < .1 &&
                                    Math.Abs(ruler[0].TranslatePoint(default, rack)!.Value.X - moved) < .1,
                                "Horizontal scroll separated the real Rack rows/ruler.");
                            scroll.Offset = new(scroll.Extent.Width - scroll.Viewport.Width, 0);
                            main.UpdateLayout();
                            Require(Math.Abs(thumb.TranslatePoint(default, rack)!.Value.X + thumb.Bounds.Width -
                                             buttons[^1].TranslatePoint(default, rack)!.Value.X -
                                             buttons[^1].Bounds.Width) < .1,
                                "Rack endpoints diverged at the far scroll boundary.");
                            if (width == 420)
                                await CaptureAsync(main, output, $"rack-end-{language}-{theme}-{bars}");
                            scroll.Offset = default;
                            main.UpdateLayout();
                        }
                    }

                    await CaptureAsync(main, output, $"rack-{language}-{theme}-{bars}-{width}");
                    Require(
                        NativeScreen
                            .Capture(main, Path.Combine(output, $"screen-rack-{language}-{theme}-{bars}-{width}.png"))
                            .StartsWith("passed"), "Native Rack capture refused.");
                }
            }

            if (!baseline)
            {
                var snapshot = workflow.State.Snapshot;

                ToggleButton Step(int index) => rack.GetVisualDescendants().OfType<ToggleButton>().Single(b =>
                    Avalonia.Automation.AutomationProperties.GetAutomationId(b) == $"Beat.Step.{stepsPart}.{index}");

                var firstStep = Step(1);
                var paintStart = new Point(firstStep.Bounds.Width / 2, firstStep.Bounds.Height / 2);
                var paintEnd = Step(3).TranslatePoint(new(12, 12), firstStep)!.Value;
                int paintUndo = workflow.State.UndoCount;
                await input.Drag(firstStep, paintStart, paintEnd);
                await rack.PendingCommand;
                await Settle();
                Require(workflow.State.UndoCount == paintUndo + 1 &&
                        Enumerable.Range(1, 3).All(i => Step(i).IsChecked == true), "Native step painting failed.");
                Require(await workflow.UndoAsync(), "Native paint Undo failed.");
                await Settle();
                var erase = Step(0);
                var row = erase.GetVisualAncestors().OfType<Grid>().First(g => g.ColumnDefinitions.Count == 2);
                await new ControlInputDriver(main, false, output).TraceDrag(row,
                [
                    erase.TranslatePoint(new(12, 12), row)!.Value,
                    Step(3).TranslatePoint(new(12, 12), row)!.Value
                ], escape: true);
                await rack.PendingCommand;
                await Settle();
                Json(output, "native-step-smoke", new { PaintSkippedCells = true, OneUndo = true, Escape = true });
                Require(workflow.State.Snapshot == snapshot, "Native step smoke left musical edits.");
                var frame = rack.GetVisualAncestors().OfType<WorkspacePane>().Single();
                var corner = Field<Border>(frame, "_corner");
                var beforeResize = workspace.State.Bounds(WorkspaceState.BeatId);
                await input.Drag(corner, new(12, 12), new(112, 62));
                await Settle();
                var resized = workspace.State.Bounds(WorkspaceState.BeatId);
                Require(resized.Width > beforeResize.Width && resized.Height > beforeResize.Height,
                    "Native floating Rack resize failed.");
                var drawing = rack.GetVisualDescendants().OfType<PartNoteThumbnail>().Single();
                Require(Math.Abs(drawing.Bounds.Width - 15342) < .1, "Native resize stretched the 32-bar preview.");
                var layout = workspace.State.Capture();
                workspace.State.Hide(WorkspaceState.BeatId);
                workspace.State.RestoreLayout(layout);
                workspace.Commit();
                await Settle();
                Require(workspace.State.Bounds(WorkspaceState.BeatId) == resized &&
                        rack.IsEffectivelyVisible && workflow.State.Snapshot == snapshot,
                    "Floating Rack layout restore changed geometry/content.");
                Json(output, "native-resize-restore", new
                {
                    Before = beforeResize,
                    After = resized,
                    ThumbnailWidth = drawing.Bounds.Width,
                    Restored = true
                });
                var preview = rack.GetVisualDescendants().OfType<PartNoteThumbnail>().Single();
                await input.Click(preview, new(10, preview.Bounds.Height / 2));
                Require(piano.Canvas.Part?.Id == notesPart && workflow.State.Snapshot == snapshot &&
                        workspace.State.Get(WorkspaceState.PianoId).Visibility == PaneVisibility.Visible,
                    "Native read-only thumbnail did not open its exact Piano Part.");
                workspace.State.SetBounds(WorkspaceState.PianoId, new(10, 10, 1080, 580));
                workspace.Commit();
                Require(await workflow.EditPianoNotesAsync(Basis(), notesPart,
                    [new(Id<NoteEvent>.New(), new(0), new(960000), 60, .1m)]), "Note smoke fixture failed.");
                Require(await workflow.ResizeBeatAsync(Basis(), 1), "Note smoke length failed.");
                await Settle();
                Field<ToggleButton>(piano, "_audition").IsChecked = false;
                await input.Select(snap, 2);
                await input.Select(length, 0);
                piano.Canvas.Fit();
                main.UpdateLayout();
                await Task.Delay(80);
                var canvas = piano.Canvas;
                var at = new Point(canvas.X(960000) + 2, canvas.Y(64) + canvas.RowHeight / 2);
                int undo = workflow.State.UndoCount;
                await input.Click(canvas, at);
                await piano.PendingCommand;
                await Settle();
                var drawn = canvas.Part!.Notes.Single(n => n.Pitch == 64);
                Require(drawn.Position.Ticks == 960000 && drawn.Duration.Ticks == 240000 &&
                        workflow.State.UndoCount == undo + 1, "Native draw ignored Snap/Length.");
                var rect = canvas.NoteRect(drawn);
                var from = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                await input.Drag(canvas, from, from + new Vector(canvas.QuarterWidth / 4, -canvas.RowHeight));
                await piano.PendingCommand;
                await Settle();
                var moved = canvas.Part!.Notes.Single(n => n.Id == drawn.Id);
                Require(moved.Position.Ticks == 1200000 && moved.Pitch == 65, "Native note move lost Snap.");
                input.Key(0x5A, modifier: 0x11);
                await Task.Delay(150);
                await Settle();
                Require(canvas.Part!.Notes.Single(n => n.Id == drawn.Id) == drawn, "Native note Undo failed.");
                input.Key(0x59, modifier: 0x11);
                await Task.Delay(150);
                await Settle();
                Require(canvas.Part!.Notes.Single(n => n.Id == drawn.Id) == moved, "Native note Redo failed.");
                Json(output, "native-note-smoke", new
                {
                    Draw = true,
                    Move = true,
                    Snap = true,
                    Length = true,
                    Undo = true,
                    Redo = true,
                    ExactThumbnailTarget = true
                });
                Require(await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State),
                    workflow.State.Selected!.Value, new(0), 0, 4), "Short Song fixture failed.");
                Require(await workflow.SetSongModeAsync(true) && await workflow.PlayAsync(), "Physical Song failed.");
                await Task.Delay(300);
                var songOutput = Field<WorkflowOutput>(workflow, "_output");
                var songSampler = songOutput.Sampler;
                long songEpoch = songSampler.TransportEpoch, songPosition = songSampler.RenderPosition;
                var songSnapshot = workflow.State.Snapshot;
                int songUndo = workflow.State.UndoCount, songObservations = observations;
                await input.Click(snap);
                var songItem = (Control)snap.ContainerFromIndex(1)!;
                var songSource = snap.ItemsSource;
                await input.Wheel(songItem, new(10, songItem.Bounds.Height / 2), 0);
                await Task.Delay(2200);
                Require(ReferenceEquals(songSource, snap.ItemsSource) && snap.IsDropDownOpen,
                    "Song observations rebuilt the selector.");
                await input.Click((Control)snap.ContainerFromIndex(1)!);
                await input.Select(length, 4);
                Require(songSampler.TransportEpoch == songEpoch && songSampler.RenderPosition > songPosition &&
                        songSampler.ReadStatus().Playing && songOutput.ReadFailure() is null &&
                        workflow.State.Snapshot == songSnapshot && workflow.State.UndoCount == songUndo &&
                        piano.Canvas.SnapTicks == 480000 && piano.Canvas.DefaultDuration == 3840000,
                    "Native selectors interrupted Song or changed its musical document.");
                Json(output, "native-song", new
                {
                    songOutput.Description, EpochRetained = true,
                    Notifications = observations - songObservations, NativeSelectors = true, HistoryPreserved = true
                });
                await workflow.StopAsync();
            }

            Json(output, "result", new
            {
                Baseline = baseline,
                ComboChecks = comboRows.Count,
                RackChecks = rackRows.Count,
                NativeInput = true,
                PhysicalPattern = true,
                EpochRetained = true
            });
        }
        finally
        {
            workflow.Changed -= Changed;
            await workflow.StopAsync();
        }
    }
}
