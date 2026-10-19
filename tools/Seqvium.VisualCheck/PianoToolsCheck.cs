// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
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
    private static async Task PianoToolsAsync(MainWindow main, string output, bool audio, bool quick)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var document = Field<ProjectDocument>(workflow, "_document");
        var input = new NativePointer.DrumInput(main);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900; main.Position = new(0, 0);
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Pattern Pattern() => BeatSteps.Resolve(workflow.State).Pattern!;
        Require(await workflow.CreateBeatAsync(document.LifecycleId, document.Generation, "Piano tools"), "Fixture Pattern");
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Melody", SynthVoice.Algorithm), "Fixture source");
        var partId = Pattern().Parts[0].Id;
        NoteEvent[] notes =
        [
            new(Id<NoteEvent>.New(), new(110000), new(310000), 60.25m, .15m),
            new(Id<NoteEvent>.New(), new(370000), new(200000), 64, .21m),
            new(Id<NoteEvent>.New(), new(610000), new(170000), 67, .18m),
            new(Id<NoteEvent>.New(), new(1200000), new(410000), 61, .12m)
        ];
        Require(await workflow.EditPianoNotesAsync(Basis(), partId, notes), "Fixture notes");
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Empty", SynthVoice.Algorithm), "Empty Part");
        var emptyId = Pattern().Parts[1].Id;
        Id<ArrangementClip> repeated = default;
        document.Edit("Repeated fixture", e =>
        {
            var placement = e.State.Placements.Single();
            repeated = e.AddArrangementClip(placement.Id, default, 0, 4);
            e.AddArrangementClip(placement.Id, default, 1, 2);
        });
        await workflow.ObserveAsync();
        foreach (var pane in workspace.State.Panes.Where(p => p.Visibility == PaneVisibility.Visible).ToArray())
            workspace.State.Hide(pane.InstanceId);
        workspace.Open(WorkspaceState.PianoId);
        workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, 1080, 580)); workspace.Commit();
        main.UpdateLayout();
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        piano.OpenPart(partId); piano.Canvas.AuditionEdits = false;
        Control Id(string id) => piano.GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        var canvas = piano.Canvas;
        var snap = Field<ComboBox>(piano, "_snap");
        var kind = Field<ComboBox>(piano, "_scaleKind");
        var tonic = Field<ComboBox>(piano, "_scaleTonic");
        var scaleFlyout = Field<Flyout>(piano, "_scaleFlyout");
        async Task Settle()
        {
            await workflow.ObserveAsync(); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            main.UpdateLayout(); await Task.Delay(90);
        }
        async Task Capture(string name)
        {
            await Settle(); await CaptureAsync(main, output, name);
            Require(NativeScreen.Capture(main, Path.Combine(output, name + "-screen.png")).StartsWith("passed"), "Native screen refused");
        }
        await Settle(); main.Activate(); await CaptureAsync(main, output, "startup");
        try { await input.ActivateVisibleWindow(); }
        catch (Exception error) when (error.GetType().Name == "ForegroundLostException" ||
                                     error.Message.StartsWith("Native startup window is covered", StringComparison.Ordinal))
        {
            Json(output, "foreground-wait", new { error.Message, WaitingSeconds = 60 });
            await input.WaitForForegroundAsync(TimeSpan.FromSeconds(60));
        }
        await Settle();
        if (canvas.Part?.Id != partId) await input.Select(Field<ComboBox>(piano, "_part"), 0);
        await input.Click(Id("Piano.Fit"));
        await input.Click(Id("Piano.Select"));
        await input.Drag(canvas, new(canvas.X(20000), canvas.Y(68)), new(canvas.X(950000), canvas.Y(59)));
        await Settle();
        Json(output, "box-selection", new { Part = canvas.Part?.Id, ExpectedPart = partId,
            canvas.SelectMode, Selection = canvas.Selection, Expected = notes.Take(3).Select(n => n.Id),
            canvas.OffsetX, canvas.OffsetY, canvas.Bounds, canvas.QuarterWidth });
        if (!canvas.Selection.SetEquals(notes.Take(3).Select(n => n.Id))) await CaptureAsync(main, output, "box-failure");
        Require(canvas.Selection.SetEquals(notes.Take(3).Select(n => n.Id)), "Native box selected wrong notes");
        await input.Select(snap, 1); await input.Select(snap, 2);
        var before = document.Current; int undo = document.UndoCount;
        await input.Click(Id("Piano.Quantize")); await piano.PendingCommand; await Settle();
        var quantized = document.Current;
        Require(document.UndoCount == undo + 1, "Quantize must have one Undo");
        long[] expected = [0, 480000, 720000, 1200000];
        Require(canvas.Part!.Notes.SequenceEqual(notes.Select((n, i) => n with { Position = new(expected[i]) })), "Native exact positions/fields");
        await Capture("quantized");
        await input.Select(snap, 1);
        input.Key(0x5A, modifier: 0x11); await Settle(); Require(document.Current == before, "Native Undo exact ticks");
        input.Key(0x59, modifier: 0x11); await Settle(); Require(document.Current == quantized, "Native Redo exact ticks");
        await input.Click(snap); input.Key(0x23); input.Key(0x0D); await Settle();
        Require(snap.SelectedIndex == 5 && !Id("Piano.Quantize").IsEnabled, "Snap Off must disable Quantize");
        await input.Select(snap, 2);

        var rows = new List<object>();
        foreach (var language in quick ? new[] { HostLanguage.English } : new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (int width in quick ? new[] { 520 } : new[] { 1080, 520, 360 })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, width, 580)); workspace.Commit();
            await Settle(); await input.Click(Id("Piano.Fit"));
            var snapshot = document.Current; int history = document.UndoCount;
            await input.Click(Id("Piano.Scale"));
            await input.Select(kind, 1); await input.Select(tonic, 0);
            Require(canvas.Scale == new PianoScale(PianoScaleKind.Major, 0), "Native C Major");
            Require(canvas.Scale.Contains(60) && !canvas.Scale.Contains(61) && canvas.Scale.IsTonic(72), "C Major rows");
            scaleFlyout.Hide(); await Capture($"{language}-{theme}-{width}-C-major");
            await input.Click(Id("Piano.Scale")); await input.Select(kind, 2); await input.Select(tonic, 9);
            Require(canvas.Scale == new PianoScale(PianoScaleKind.NaturalMinor, 9), "Native A minor");
            Require(canvas.Scale.Contains(60) && !canvas.Scale.Contains(61) && canvas.Scale.IsTonic(69), "A minor rows");
            scaleFlyout.Hide(); await Capture($"{language}-{theme}-{width}-A-minor");
            // Real Part selector input, including empty-state rendering in both themes/languages.
            await input.Select(Field<ComboBox>(piano, "_part"), 1);
            Require(canvas.Part!.Id == emptyId && canvas.Scale.Tonic == 9, "Empty Part retained scale");
            await input.Click(Id("Piano.Ghosts"));
            await Capture($"{language}-{theme}-{width}-empty");
            await input.Click(Id("Piano.Ghosts"));
            await input.Select(Field<ComboBox>(piano, "_part"), 0);
            Require(canvas.Part!.Id == partId && canvas.Scale.Tonic == 9, "Return retained scale");
            await input.Wheel(canvas, new(canvas.KeyWidth + 60, canvas.RulerHeight + 60), 120);
            await input.Wheel(canvas, new(canvas.KeyWidth + 60, canvas.RulerHeight + 60), -120, zoom: true);
            await input.Click(Id("Piano.ZoomIn"));
            await input.Wheel(canvas, new(canvas.KeyWidth + 60, canvas.RulerHeight + 60), -120, horizontal: true);
            workspace.State.Hide(WorkspaceState.PianoId); workspace.Commit(); workspace.Open(WorkspaceState.PianoId); workspace.Commit(); await Settle();
            Require(canvas.Scale == new PianoScale(PianoScaleKind.NaturalMinor, 9), "Hide/restore retained scale");
            Require(document.Current == snapshot && document.UndoCount == history, "Scale/view changed document/history");
            rows.Add(new { Language = language.ToString(), Theme = theme.ToString(), Width = width, NativeScale = true, Empty = true, NoDocumentEdit = true });
            Json(output, "piano-tools-progress", rows);
        }
        object physical = new { Requested = audio, Run = false };
        if (audio)
        {
            workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, 1080, 580)); workspace.Commit(); await Settle();
            Require(await workflow.PlayAsync(), "Physical Pattern: " + workflow.State.Detail);
            var device = Field<WorkflowOutput>(workflow, "_output"); long epoch = device.Sampler.TransportEpoch;
            var snapshot = document.Current; int history = document.UndoCount;
            await input.Click(Id("Piano.Scale")); await input.Click(kind);
            var item = (Control)kind.ContainerFromIndex(1)!; var source = kind.ItemsSource; var origin = item.PointToScreen(default);
            var focused = TopLevel.GetTopLevel(kind)?.FocusManager?.GetFocusedElement();
            await input.Wheel(item, new(item.Bounds.Width / 2, item.Bounds.Height / 2), 0);
            await Task.Delay(2300);
            Require(kind.IsDropDownOpen && ReferenceEquals(source, kind.ItemsSource) && ReferenceEquals(item, kind.ContainerFromIndex(1)) && item.PointToScreen(default) == origin,
                "Transport rebuilt Scale Popup");
            Require(ReferenceEquals(focused, TopLevel.GetTopLevel(kind)?.FocusManager?.GetFocusedElement()), "Playing Popup lost focus");
            await input.Click(item); Require(kind.SelectedIndex == 1, "Hovered scale row did not select");
            scaleFlyout.Hide();
            Require(device.Sampler.TransportEpoch == epoch && device.ReadFailure() is null, "Scale interrupted Pattern");
            Require(document.Current == snapshot && document.UndoCount == history, "Playing scale changed document");
            // Quantize during real Pattern playback, using current canonical selection.
            await input.Click(Id("Piano.Fit")); await input.Click(canvas, new(canvas.X(3000000), canvas.Y(60) + 6));
            input.Key(0x41, modifier: 0x11); await Settle();
            await input.Select(snap, 0);
            await input.Click(Id("Piano.Quantize")); await piano.PendingCommand; await Settle();
            Require(document.Current != snapshot && document.UndoCount == history + 1, "Playing Quantize did not edit");
            input.Key(0x5A, modifier: 0x11); await Settle(); Require(document.Current == snapshot, "Playing native Undo");
            Require(device.Sampler.TransportEpoch == epoch && device.Sampler.ReadStatus().Playing && device.ReadFailure() is null, "Quantize stopped Pattern");
            string description = device.Description; await workflow.StopAsync();
            await workflow.SetSongModeAsync(true); Require(await workflow.PlayAsync(), "Physical Song: " + workflow.State.Detail);
            await Task.Delay(900); device = Field<WorkflowOutput>(workflow, "_output"); epoch = device.Sampler.TransportEpoch;
            // Off and on remain view-only under continuous Song observations.
            await input.Click(Id("Piano.Scale")); await input.Select(kind, 0); await input.Select(kind, 2); scaleFlyout.Hide();
            await input.Click(Id("Piano.Quantize")); await piano.PendingCommand; await Settle();
            Require(document.Current != snapshot && document.UndoCount == history + 1, "Song Quantize did not edit");
            input.Key(0x5A, modifier: 0x11); await Settle(); Require(document.Current == snapshot, "Song native Undo");
            Require(device.Sampler.TransportEpoch == epoch && device.ReadFailure() is null, "Scale interrupted Song");
            await workflow.StopAsync(); physical = new { Requested = true, Run = true, Device = description, StablePopupMs = 2300 };
        }
        Json(output, "physical", physical);
        await input.Select(snap, 2);

        // Restore off-grid source ticks, then use the real Playlist repeat command and editor navigation.
        Require(await workflow.EditPianoNotesAsync(Basis(), partId, notes), "Restore off-grid fixture");
        var original = Pattern();
        workspace.Open(WorkspaceState.ArrangementId); workspace.State.SetBounds(WorkspaceState.ArrangementId, new(5, 5, 1080, 490)); workspace.Commit(); await Settle();
        var playlist = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        var clip = document.Current.State.ArrangementClips.Single(c => c.Id == repeated);
        var at = new Point(playlist.Canvas.HeaderSize + original.Length.Ticks * 1.45 * playlist.Canvas.TickPixels, playlist.Canvas.ClipRect(clip).Y + 12);
        await input.RightClick(playlist.Canvas, at);
        var unique = playlist.Canvas.ContextMenu!.Items.OfType<MenuItem>().Single(i => AutomationProperties.GetAutomationId(i) == "Arrangement.Context.RepeatUnique");
        await input.Click(unique); await playlist.PendingCommand; await Settle();
        var independent = document.Current.State.ArrangementClips.Single(c => c.Id == playlist.Canvas.Selected);
        workspace.State.Hide(WorkspaceState.PianoId); workspace.Open(WorkspaceState.ArrangementId); workspace.Commit(); await Settle();
        at = new(playlist.Canvas.HeaderSize + (independent.Position.Ticks + original.Length.Ticks * .45) * playlist.Canvas.TickPixels, playlist.Canvas.ClipRect(independent).Y + 12);
        await input.Click(playlist.Canvas, at); await input.Click(playlist.Canvas, at); await playlist.PendingCommand; await Settle();
        workspace.Open(WorkspaceState.PianoId); workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, 1080, 580)); workspace.Commit(); await Settle();
        Require(canvas.Pattern!.VariationOf == original.Id && canvas.Part!.Id != partId, "Independent Piano target");
        await input.Click(Id("Piano.Fit"));
        await input.Click(canvas, new(canvas.KeyWidth + 12, canvas.RulerHeight + 12)); input.Key(0x41, modifier: 0x11); await Settle();
        await input.Click(Id("Piano.Quantize")); await piano.PendingCommand; await Settle();
        Require(document.Current.State.Patterns.Single(p => p.Id == original.Id) == original, "Quantize changed original");
        Require(canvas.Part!.Notes.Take(3).Select(n => n.Position.Ticks).SequenceEqual(new long[] { 0, 480000, 720000 }), "Variation Quantize positions");
        await Capture("independent-repeat-quantized");
        var tail = new NoteEvent(Id<NoteEvent>.New(), new(3839999), new(1), 72, .1m);
        Require(await workflow.EditPianoNotesAsync(Basis(), canvas.Part.Id, [.. canvas.Part.Notes, tail]), "Boundary fixture");
        await Settle();
        await input.Click(canvas, new(canvas.KeyWidth + 12, canvas.RulerHeight + 12)); input.Key(0x41, modifier: 0x11); await Settle();
        await input.Select(snap, 0); var boundary = document.Current; int boundaryUndo = document.UndoCount;
        await input.Click(Id("Piano.Quantize")); await piano.PendingCommand; await Settle();
        Require(document.Current == boundary && document.UndoCount == boundaryUndo && canvas.Feedback == shell["Piano.QuantizeBoundary"], "Native boundary refusal");
        await Capture("boundary-refusal");
        Json(output, "piano-tools", new { NativeSelectionQuantizeUndoRedo = true, ExactPositions = expected, Matrix = rows, IndependentRepeat = true, Physical = physical, SubjectiveListening = false });
    }
}
