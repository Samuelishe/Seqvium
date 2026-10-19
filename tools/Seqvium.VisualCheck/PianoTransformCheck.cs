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
    private static async Task PianoTransformAsync(MainWindow main, string output, bool native, bool audio, bool quick)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var document = Field<ProjectDocument>(workflow, "_document");
        var input = new NativePointer.DrumInput(main);
        var driver = new ControlInputDriver(main, !native, output);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900; main.Position = new(0, 0);
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Pattern Pattern() => BeatSteps.Resolve(workflow.State).Pattern!;
        Require(await workflow.CreateBeatAsync(document.LifecycleId, document.Generation, "Transform"), "Fixture Pattern");
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Chords", SynthVoice.Algorithm), "Fixture source");
        var partId = Pattern().Parts[0].Id;
        NoteEvent[] notes = [new(Id<NoteEvent>.New(), new(490000), new(240000), 60.25m, .3m),
            new(Id<NoteEvent>.New(), new(490000), new(480000), 64, .6m),
            new(Id<NoteEvent>.New(), new(490000), new(3), 67, .8m),
            new(Id<NoteEvent>.New(), new(1200000), new(410000), 61, .12m)];
        Require(await workflow.EditPianoNotesAsync(Basis(), partId, notes), "Fixture notes");
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Empty", SynthVoice.Algorithm), "Empty Part");
        var emptyId = Pattern().Parts[1].Id;
        document.Edit("Shared uses", e => e.AddArrangementClip(e.State.Placements.Single().Id, default, 0, 4));
        await workflow.ObserveAsync();
        foreach (var pane in workspace.State.Panes.Where(p => p.Visibility == PaneVisibility.Visible).ToArray()) workspace.State.Hide(pane.InstanceId);
        workspace.Open(WorkspaceState.PianoId);
        workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, 1080, 580)); workspace.Commit(); main.UpdateLayout();
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        piano.OpenPart(partId); piano.Canvas.AuditionEdits = false;
        var canvas = piano.Canvas;
        var flyout = Field<Flyout>(piano, "_transformFlyout"); var body = (Control)flyout.Content!;
        var apply = Field<Button>(piano, "_transformApply");
        var transpose = Field<NumericUpDown>(piano, "_transformTranspose");
        var duration = Field<NumericUpDown>(piano, "_transformDuration");
        var velocity = Field<NumericUpDown>(piano, "_transformVelocity");
        Control Id(string id) => piano.GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        async Task Settle()
        {
            await workflow.ObserveAsync(); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            main.UpdateLayout(); await Task.Delay(80);
        }
        async Task Open()
        {
            await driver.Click(Id("Piano.Transform")); await Settle(); Require(flyout.IsOpen, "Transform did not open");
            Require(transpose.Value == 0 && duration.Value == 100 && velocity.Value == 100, "Neutral initial values");
        }
        async Task Enter(NumericUpDown field, string value)
        {
            if (native)
            {
                await input.Exact(field, value); input.Key(0x09);
            }
            else
            {
                var text = field.GetVisualDescendants().OfType<TextBox>().Single(); text.Focus(); text.Text = value;
            }
            await Settle();
        }
        async Task Settings()
        {
            await Enter(transpose, "2"); Require(duration.Value == 100 && velocity.Value == 100, "Independent transpose");
            await Enter(duration, "150"); Require(transpose.Value == 2 && velocity.Value == 100, "Independent duration");
            await Enter(velocity, "120"); Require(transpose.Value == 2 && duration.Value == 150 && velocity.Value == 120, "Independent velocity");
            Require(apply.IsEnabled, "Valid draft disabled");
        }
        async Task History(bool redo)
        {
            if (native) input.Key(redo ? (ushort)0x59 : (ushort)0x5A, modifier: 0x11);
            else Require(await workflow.UndoAsync(redo), "Canonical history");
            await Settle();
        }
        await Settle(); main.Activate(); await CaptureAsync(main, output, "startup");
        if (native) await driver.ActivateVisibleWindow();
        await driver.Click(Id("Piano.Fit")); await driver.Click(Id("Piano.Select"));
        if (!native) ((ToggleButton)Id("Piano.Select")).IsChecked = true;
        await driver.Drag(canvas, new(canvas.X(20000), canvas.Y(68)), new(canvas.X(1000000), canvas.Y(59)));
        await Settle(); Require(canvas.Selection.SetEquals(notes.Take(3).Select(n => n.Id)), "Selected chord only");
        Field<ComboBox>(piano, "_snap").SelectedIndex = 5; await Settle();
        Require(canvas.SnapTicks == 1 && Id("Piano.Transform").IsEnabled, "Free Snap eligibility");
        var before = document.Current; int undo = document.UndoCount;
        await Open(); await Settings(); Require(document.Current == before, "Draft changed music");
        var cancel = body.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Piano.Cancel");
        await driver.Click(cancel); await Settle(); Require(!flyout.IsOpen && document.Current == before, "Cancel changed music");
        await Open(); await driver.Escape(body); await Settle(); Require(!flyout.IsOpen && document.Current == before, "Escape changed music");
        await Open(); flyout.Hide(); await Settle(); Require(document.Current == before, "Close changed music");
        await Open(); await driver.Click(apply); await piano.PendingCommand; await Settle();
        Require(document.Current == before && document.UndoCount == undo, "Neutral Apply created history");
        await Open(); await Enter(transpose, "1.5"); Require(!apply.IsEnabled && document.Current == before, "Fractional transpose accepted");
        await Enter(transpose, "2"); await Enter(duration, "201"); Require(!apply.IsEnabled, "Invalid duration accepted");
        await driver.Click(cancel); await Settle();
        await Open(); await Settings(); await driver.Click(apply); await piano.PendingCommand; await Settle();
        var accepted = document.Current;
        Require(document.UndoCount == undo + 1, "Apply must have one Undo");
        Require(canvas.Part!.Notes.SequenceEqual(new[] { notes[0] with { Pitch = 62.25m, Duration = new(360000), Intensity = .36m },
            notes[1] with { Pitch = 66, Duration = new(720000), Intensity = .72m }, notes[2] with { Pitch = 69, Duration = new(5), Intensity = .96m }, notes[3] }), "Exact transformed notes");
        await History(false); Require(document.Current == before, "Exact Undo");
        await History(true); Require(document.Current == accepted, "Exact Redo");
        await Open(); canvas.SelectAll(); await Settle(); Require(!flyout.IsOpen && document.Current == accepted, "Selection change did not cancel");

        var matrix = new List<object>();
        if (!quick)
        {
            foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            foreach (int width in new[] { 1080, 520, 360 })
            {
                if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, width, 580)); workspace.Commit(); await Settle();
                await Open(); await Settings(); transpose.Focus(); var focused = TopLevel.GetTopLevel(body)?.FocusManager?.GetFocusedElement();
                for (int i = 0; i < 5; i++) await Settle();
                Require(flyout.IsOpen && ReferenceEquals(focused, TopLevel.GetTopLevel(body)?.FocusManager?.GetFocusedElement()) && document.Current == accepted, "Observation lost Popup/focus/draft");
                var alignment = new[] { (transpose, "TransformTranspose"), (duration, "TransformDuration"), (velocity, "TransformVelocity") }
                    .Select(item => FieldAlignment.Verify(item.Item1, body.GetVisualDescendants().OfType<TextBlock>().Single(t =>
                        AutomationProperties.GetAutomationId(t) == "Piano." + item.Item2), body, item.Item2)).ToArray();
                string name = $"{language}-{theme}-{width}";
                await CaptureAsync(main, output, name + "-populated"); await CapturePopupAsync(body, output, name + "-flyout");
                await driver.Click(cancel); await Settle(); piano.OpenPart(emptyId); await Settle();
                Require(!Id("Piano.Transform").IsEnabled, "Empty Part allows Transform");
                await driver.Click(Id("Piano.Ghosts")); await CaptureAsync(main, output, name + "-empty"); await driver.Click(Id("Piano.Ghosts"));
                piano.OpenPart(partId); canvas.SelectAll(); await Settle();
                matrix.Add(new { Language = language.ToString(), Theme = theme.ToString(), Width = width, Native = native, Alignment = alignment });
                Json(output, "matrix", matrix);
            }
            await Open();
            // Replace the selection while the flyout is open, then return: the old draft must stay retired.
            piano.OpenPart(emptyId); piano.OpenPart(partId); canvas.SelectAll(); await Settle(); Require(!flyout.IsOpen, "Part ABA revived draft");
            await Open(); Require(await workflow.EditPianoNotesAsync(Basis(), partId, notes), "External edit"); await Settle(); Require(!flyout.IsOpen, "Stale draft remains open");
            await Open(); Require(await workflow.SaveAsync(Path.Combine(output, "busy.seqvium")), "Busy save"); await Settle(); Require(!flyout.IsOpen, "Busy did not retire draft");
            await Open(); workspace.State.Hide(WorkspaceState.PianoId); workspace.Commit(); await Settle(); Require(!flyout.IsOpen, "Hide did not cancel");
            workspace.Open(WorkspaceState.PianoId); workspace.Commit(); await Settle();

            var labels = new List<object>();
            var labelNotes = Enumerable.Range(60, 15).Select(pitch => new NoteEvent(Id<NoteEvent>.New(),
                new(240000), new(960000), pitch, .02m)).ToArray();
            Require(await workflow.EditPianoNotesAsync(Basis(), partId, labelNotes), "Pitch-label fixture");
            foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            foreach (int width in new[] { 1080, 360 })
            {
                if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, width, 700)); workspace.Commit();
                await Settle(); canvas.Fit(); await Settle();
                foreach (var note in labelNotes)
                {
                    var rect = canvas.NoteRect(note);
                    var label = canvas.PitchLabel(note.Pitch, new(rect.X + 5, rect.Y + 1, rect.Width - 12, rect.Height - 4), "Text.Primary");
                    double top = label.Origin.Y + label.Text.Height + label.Text.OverhangAfter - label.Text.Extent;
                    Require(top >= rect.Top + 1 && top + label.Text.Extent <= rect.Bottom - 3, "Pitch glyph clips or meets velocity stripe: " + note.Pitch);
                    labels.Add(new { Language = language.ToString(), Theme = theme.ToString(), Width = width, Pitch = note.Pitch,
                        label.Text.Height, label.Text.Extent, InkTop = top, Note = rect });
                }
                await CaptureAsync(main, output, $"{language}-{theme}-{width}-pitch-labels");
            }
            Json(output, "pitch-labels", labels);
            Require(await workflow.EditPianoNotesAsync(Basis(), partId, notes), "Restore physical fixture");
            await Settle(); canvas.SelectAll(); await Settle();
        }

        var physical = new List<object>();
        if (audio)
        {
            // Sub-frame gates deliberately retain finite execution. Physical continuity needs a loopable phrase.
            var audible = notes.Select((n, i) => i == 2 ? n with { Duration = new(120000) } : n).ToArray();
            Require(await workflow.EditPianoNotesAsync(Basis(), partId, audible), "Loopable physical fixture");
            await Settle(); canvas.SelectAll(); await Settle();
        }
        if (audio) foreach (bool song in new[] { false, true })
        {
            Require(await workflow.SetSongModeAsync(song) || workflow.State.SongMode == song, "Mode");
            Require(await workflow.PlayAsync(), "Physical Play: " + workflow.State.Detail); await Settle();
            var device = Field<WorkflowOutput>(workflow, "_output"); long epoch = device.Sampler.TransportEpoch;
            void Trace(string phase) => Json(output, $"physical-{song}-{phase}", new { device.Sampler.TransportEpoch,
                Status = device.Sampler.ReadStatus(), device.Sampler.RenderPosition, Failure = device.ReadFailure(),
                Workflow = workflow.State.StatusKey, workflow.State.Detail });
            Trace("started");
            await Open(); await Settings(); transpose.Focus(); var focused = TopLevel.GetTopLevel(body)?.FocusManager?.GetFocusedElement();
            await Task.Delay(1600); await Settle(); Require(flyout.IsOpen && ReferenceEquals(focused, TopLevel.GetTopLevel(body)?.FocusManager?.GetFocusedElement()), "Playing Popup/focus");
            var current = document.Current;
            Trace("draft");
            await driver.Click(apply); await piano.PendingCommand; await Settle(); Require(document.Current != current, "Playing Apply");
            Trace("applied");
            await History(false); Trace("undo"); Require(document.Current == current, "Playing Undo"); await History(true); Trace("redo");
            Json(output, song ? "physical-song" : "physical-pattern", new { ExpectedEpoch = epoch, device.Sampler.TransportEpoch,
                Status = device.Sampler.ReadStatus(), device.Sampler.RenderPosition, Failure = device.ReadFailure(),
                Workflow = workflow.State.StatusKey, workflow.State.Detail, Device = device.Description });
            Require(device.Sampler.TransportEpoch == epoch && device.Sampler.ReadStatus().Playing && device.ReadFailure() is null, "Playback interrupted");
            physical.Add(new { Song = song, Device = device.Description, Epoch = epoch, StablePopupMs = 1600, Native = native });
            await workflow.StopAsync(); await Settle();
        }
        Json(output, "transform", new { Native = native, Routed = !native, DraftCancelEscapeCloseDefaultsInvalidApplyUndoRedo = true,
            Free = true, StaleBusyHidePartAba = true, Matrix = matrix, Physical = physical, SubjectiveListening = false });
    }
}
