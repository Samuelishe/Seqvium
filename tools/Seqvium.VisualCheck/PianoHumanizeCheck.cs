// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    private static async Task PianoHumanizeAsync(MainWindow main, string output, bool audio, bool quick)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var document = Field<ProjectDocument>(workflow, "_document");
        var input = new NativePointer.DrumInput(main);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900; main.Position = new(0, 0);
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Pattern Pattern() => BeatSteps.Resolve(workflow.State).Pattern!;
        Require(await workflow.CreateBeatAsync(document.LifecycleId, document.Generation, "Humanize"), "Fixture Pattern");
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Chords", SynthVoice.Algorithm), "Fixture source");
        var partId = Pattern().Parts[0].Id;
        NoteEvent[] notes =
        [
            new(Id<NoteEvent>.New(), new(490000), new(350000), 60.25m, .15m),
            new(Id<NoteEvent>.New(), new(490000), new(400000), 64, .21m),
            new(Id<NoteEvent>.New(), new(490000), new(170000), 67, .18m),
            new(Id<NoteEvent>.New(), new(1200000), new(410000), 61, .12m)
        ];
        Require(await workflow.EditPianoNotesAsync(Basis(), partId, notes), "Fixture notes");
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Empty", SynthVoice.Algorithm), "Empty Part");
        var emptyId = Pattern().Parts[1].Id;
        document.Edit("Shared uses", e => e.AddArrangementClip(e.State.Placements.Single().Id, default, 0, 4));
        await workflow.ObserveAsync();
        foreach (var pane in workspace.State.Panes.Where(p => p.Visibility == PaneVisibility.Visible).ToArray())
            workspace.State.Hide(pane.InstanceId);
        workspace.Open(WorkspaceState.PianoId);
        workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, 1080, 580)); workspace.Commit(); main.UpdateLayout();
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        piano.OpenPart(partId); piano.Canvas.AuditionEdits = false;
        var canvas = piano.Canvas;
        var flyout = Field<Flyout>(piano, "_humanizeFlyout");
        var body = (Control)flyout.Content!;
        var apply = Field<Button>(piano, "_humanizeApply");
        var timing = Field<NumericUpDown>(piano, "_humanizeTiming");
        var velocity = Field<NumericUpDown>(piano, "_humanizeVelocity");
        Slider Slider(string key) => body.GetVisualDescendants().OfType<Slider>().Single(s => AutomationProperties.GetAutomationId(s) == key);
        Control Id(string id) => piano.GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        async Task Settle()
        {
            await workflow.ObserveAsync(); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            main.UpdateLayout(); await Task.Delay(80);
        }
        async Task Capture(string name)
        {
            await Settle(); await CaptureAsync(main, output, name);
            Require(NativeScreen.Capture(main, Path.Combine(output, name + "-screen.png")).StartsWith("passed"), "Native screen refused");
        }
        async Task Open()
        {
            await input.Click(Id("Piano.Humanize")); await Settle(); Require(flyout.IsOpen, "Humanize did not open");
        }
        async Task Strength(NumericUpDown field, int value)
        {
            await input.Click(field); input.Key(0x41, modifier: 0x11);
            foreach (char digit in value.ToString(System.Globalization.CultureInfo.InvariantCulture)) input.Key((ushort)digit);
            input.Key(0x09); await Settle(); Require(field.Value == value, "Native strength input");
        }
        await Settle(); main.Activate(); await CaptureAsync(main, output, "startup");
        try { await input.ActivateVisibleWindow(); }
        catch (Exception error) when (error.GetType().Name == "ForegroundLostException" ||
                                     error.Message.StartsWith("Native startup window is covered", StringComparison.Ordinal))
        {
            Json(output, "foreground-wait", new { error.Message, WaitingSeconds = 60 });
            await input.WaitForForegroundAsync(TimeSpan.FromSeconds(60));
        }
        await Settle(); await input.Click(Id("Piano.Fit")); await input.Click(Id("Piano.Select"));
        await input.Drag(canvas, new(canvas.X(20000), canvas.Y(68)), new(canvas.X(950000), canvas.Y(59)));
        await Settle(); Require(canvas.Selection.SetEquals(notes.Take(3).Select(n => n.Id)), "Native chord selection");
        var snap = Field<ComboBox>(piano, "_snap"); await input.Click(snap); input.Key(0x23); input.Key(0x0D); await Settle();
        Require(canvas.SnapTicks == 1 && Id("Piano.Humanize").IsEnabled, "Humanize must work with Free Snap");
        var before = document.Current; int undo = document.UndoCount;
        await Open();
        var timingSlider = Slider("Piano.HumanizeTiming");
        await input.Click(timingSlider, new(timingSlider.Bounds.Width / 2, timingSlider.Bounds.Height / 2));
        await Settle();
        Json(output, "slider-input", new { Timing = timing.Value, Velocity = velocity.Value, Slider = timingSlider.Value, timingSlider.Bounds, flyout.IsOpen });
        if (!(timing.Value > 20 && velocity.Value == 20)) await Capture("slider-input-failure");
        Require(timing.Value > 20 && velocity.Value == 20, "Native independent Timing slider");
        var velocitySlider = Slider("Piano.HumanizeVelocity");
        var timingDraft = timing.Value;
        await input.Click(velocitySlider, new(velocitySlider.Bounds.Width / 2, velocitySlider.Bounds.Height / 2));
        await Settle();
        Require(velocity.Value > 20 && timing.Value == timingDraft, "Native independent Velocity slider");
        await Strength(timing, 100); await Strength(velocity, 100);
        Require(document.Current == before && document.UndoCount == undo, "Draft changed music");
        var cancel = body.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Piano.Cancel");
        await Capture("draft"); await input.Click(cancel); await Settle();
        Require(!flyout.IsOpen && document.Current == before, "Cancel changed music");
        await Open(); input.Key(0x1B); await Settle(); Require(!flyout.IsOpen && document.Current == before, "Escape changed music");
        await Open();
        var dismissPoint = canvas.PointToScreen(new(canvas.KeyWidth + 12, canvas.RulerHeight + 12));
        var overlay = main.InputHitTest(main.PointToClient(dismissPoint)) as Control;
        Require(overlay?.GetType().Name == "LightDismissOverlayLayer", "Expected the current flyout dismissal overlay");
        await input.Click(overlay!, overlay!.PointToClient(dismissPoint)); await Settle();
        Require(!flyout.IsOpen && document.Current == before, "Light dismissal changed music");
        await Open(); await Strength(timing, 0); await Strength(velocity, 0); await input.Click(apply); await piano.PendingCommand; await Settle();
        Require(document.Current == before && document.UndoCount == undo, "Zero created history");
        await Open(); await Strength(timing, 100); await Strength(velocity, 100);
        await input.Click(apply); await piano.PendingCommand; await Settle();
        var humanized = document.Current;
        Require(humanized != before && document.UndoCount == undo + 1, "Apply must have one Undo");
        Require(canvas.Part!.Notes.Take(3).Select(n => n.Position).Distinct().Count() == 1, "Chord split");
        Require(canvas.Part.Notes[3] == notes[3], "Unselected note changed");
        for (int i = 0; i < 3; i++)
        {
            var n = canvas.Part.Notes[i]; Require(Math.Abs(n.Position.Ticks - notes[i].Position.Ticks) <= 30000, "Timing bound");
            Require(Math.Abs(n.Intensity - notes[i].Intensity) <= .1m, "Velocity bound");
            Require(n with { Position = notes[i].Position, Intensity = notes[i].Intensity } == notes[i], "Immutable fields");
        }
        await Capture("applied");
        input.Key(0x5A, modifier: 0x11); await Settle(); Require(document.Current == before, "Native exact Undo");
        input.Key(0x59, modifier: 0x11); await Settle(); Require(document.Current == humanized, "Native exact Redo");

        var matrix = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (int width in quick ? new[] { 360 } : new[] { 1080, 520, 360 })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, width, 580)); workspace.Commit(); await Settle();
            await Open();
            await input.Click(timing); input.Key(0x26); input.Key(0x09); await Settle();
            Require(flyout.IsOpen && document.Current == humanized, "Observation closed or applied draft");
            var alignment = new[] { (timing, "Piano.HumanizeTiming"), (velocity, "Piano.HumanizeVelocity") }
                .Select(item => FieldAlignment.Verify(item.Item1,
                    body.GetVisualDescendants().OfType<TextBlock>().Single(t => AutomationProperties.GetAutomationId(t) == item.Item2), body, item.Item2)).ToArray();
            await Capture($"{language}-{theme}-{width}-popup");
            // Flyouts live in a real native Popup: capture it explicitly as well as the owner.
            await CapturePopupAsync(body, output, $"{language}-{theme}-{width}-flyout");
            await input.Click(cancel); await Settle();
            await input.Select(Field<ComboBox>(piano, "_part"), 1); await Settle();
            Require(canvas.Part!.Id == emptyId && !Id("Piano.Humanize").IsEnabled, "Empty target allowed Humanize");
            await input.Click(Id("Piano.Ghosts")); await Capture($"{language}-{theme}-{width}-empty"); await input.Click(Id("Piano.Ghosts"));
            await input.Select(Field<ComboBox>(piano, "_part"), 0); await Settle();
            await input.Click(canvas, new(canvas.KeyWidth + 12, canvas.RulerHeight + 12)); input.Key(0x41, modifier: 0x11); await Settle();
            matrix.Add(new { Language = language.ToString(), Theme = theme.ToString(), Width = width, Native = true, Alignment = alignment });
            Json(output, "matrix", matrix);
        }
        // An external generation expires a pending panel without committing its retained settings.
        await Open(); Require(await workflow.EditPianoNotesAsync(Basis(), partId, notes), "External edit"); await Settle();
        Require(!flyout.IsOpen, "Stale generation left Humanize open");
        await Open(); workspace.State.Hide(WorkspaceState.PianoId); workspace.Commit(); await Settle();
        Require(!flyout.IsOpen, "Hide did not cancel"); workspace.Open(WorkspaceState.PianoId); workspace.Commit(); await Settle();

        object physical = new { Requested = audio, Run = false };
        if (audio)
        {
            var results = new List<object>();
            foreach (bool song in new[] { false, true })
            {
                Require(await workflow.SetSongModeAsync(song) || workflow.State.SongMode == song, "Mode");
                Require(await workflow.PlayAsync(), "Physical Play: " + workflow.State.Detail); await Settle();
                var device = Field<WorkflowOutput>(workflow, "_output"); long epoch = device.Sampler.TransportEpoch;
                await Open(); await input.Click(timing); var focused = TopLevel.GetTopLevel(body)?.FocusManager?.GetFocusedElement();
                await Task.Delay(1600); await Settle();
                Require(flyout.IsOpen && ReferenceEquals(focused, TopLevel.GetTopLevel(body)?.FocusManager?.GetFocusedElement()), "Playing observations lost Popup/focus");
                var accepted = document.Current; int history = document.UndoCount;
                await input.Click(apply); await piano.PendingCommand; await Settle();
                Require(document.Current != accepted && document.UndoCount == history + 1, "Playing Apply");
                input.Key(0x5A, modifier: 0x11); await Settle(); Require(document.Current == accepted, "Playing Undo");
                input.Key(0x59, modifier: 0x11); await Settle();
                Require(device.Sampler.TransportEpoch == epoch && device.Sampler.ReadStatus().Playing && device.ReadFailure() is null, "Humanize interrupted playback");
                results.Add(new { Song = song, Device = device.Description, Epoch = epoch, StablePopupMs = 1600, NativeApplyUndoRedo = true });
                await workflow.StopAsync();
            }
            physical = new { Requested = true, Run = true, Results = results };
        }
        Json(output, "physical", physical);
        var saved = document.Current.State; string path = Path.Combine(output, "humanized.seqvium");
        Require(await workflow.SaveAsync(path), "Save");
        Require(await workflow.ExportWaveAsync(Basis(), Path.Combine(output, "humanized.wav"), default), "Export");
        Require(ProjectDocument.HasSameContent(saved, document.Current.State), "Export changed Humanize");
        Require(await workflow.OpenAsync(path, UnsavedDecision.Discard), "Open"); await Settle();
        Require(ProjectDocument.HasSameContent(saved, document.Current.State), "Open regenerated offsets");
        Json(output, "humanize", new { NativeSelection = true, NativeDraftCancelEscapeZeroApplyUndoRedo = true, Free = true,
            StaleAndHideCancel = true, Matrix = matrix, Physical = physical, SaveOpenExport = true, SubjectiveListening = false });
    }
}
