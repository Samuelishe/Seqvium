// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task PianoMatrixAsync(MainWindow main, string output)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        main.WindowState = WindowState.Normal;
        main.Width = 1100;
        main.Height = 750;
        await Task.Delay(200);
        workspace.Open(WorkspaceState.BeatId);
        Require(
            await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
                "Jade Nocturne"), "Create Piano fixture failed.");
        string wav = Path.Combine(output, "authored-tone.wav");
        WriteStepWav(wav, 0.1f);
        Require(await workflow.AddBeatWavAsync(BeatSteps.Resolve(workflow.State).Basis!, wav),
            "Piano fixture import failed.");
        Require(await workflow.ResizeBeatAsync(BeatSteps.Resolve(workflow.State).Basis!, 4),
            "Piano fixture length failed.");
        workspace.Open(WorkspaceState.PianoId);
        workspace.State.SetBounds(WorkspaceState.PianoId, new(0, 0, workspace.State.Width, workspace.State.Height));
        main.UpdateLayout();
        var pane = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        var canvas = pane.Canvas;
        pane.Refresh();
        Field<ToggleButton>(pane, "_audition").IsChecked = false;
        canvas.Fit();
        using var pointer = new Pointer(91009, PointerType.Mouse, true);
        Point Root(Point p) => canvas.TranslatePoint(p, main)!.Value;

        void Press(Point p, KeyModifiers modifiers = KeyModifiers.None) => canvas.RaiseEvent(
            new PointerPressedEventArgs(canvas, pointer, main, Root(p), 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                modifiers, 1));

        void Move(Point p, KeyModifiers modifiers = KeyModifiers.None) => canvas.RaiseEvent(new PointerEventArgs(
            InputElement.PointerMovedEvent, canvas, pointer, main, Root(p), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), modifiers));

        async Task Release(Point p, KeyModifiers modifiers = KeyModifiers.None)
        {
            canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, main, Root(p), 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), modifiers,
                MouseButton.Left));
            await pane.PendingCommand;
            pane.Refresh();
            main.UpdateLayout();
        }

        async Task Draw(decimal quarter, int pitch, decimal duration)
        {
            if (canvas.Y(pitch) < canvas.RulerHeight + 4)
                canvas.SetView(canvas.OffsetX, canvas.OffsetY - (canvas.RulerHeight + 12 - canvas.Y(pitch)));
            if (canvas.Y(pitch) + canvas.RowHeight > canvas.NoteBottom)
                canvas.SetView(canvas.OffsetX, canvas.OffsetY + canvas.Y(pitch) + canvas.RowHeight - canvas.NoteBottom);
            canvas.DefaultDuration = (long)(duration * MusicalPosition.TicksPerQuarter);
            var p = new Point(canvas.X((long)(quarter * MusicalPosition.TicksPerQuarter)) + 2,
                canvas.Y(pitch) + canvas.RowHeight / 2);
            int undo = workflow.State.UndoCount;
            Press(p);
            await Release(p);
            Require(workflow.State.UndoCount == undo + 1,
                "Drawing did not create one canonical note: " + workflow.State.Detail);
        }

        foreach (int pitch in new[] { 60, 64, 67 }) await Draw(0, pitch, 2);
        foreach (int pitch in new[] { 57, 60, 64 }) await Draw(4, pitch, 2);
        foreach (int pitch in new[] { 55, 59, 62 }) await Draw(8, pitch, 2);
        foreach (var note in new[]
                 {
                     (2m, 72, .5m), (2.5m, 74, .5m), (3m, 76, 1m), (6m, 72, .5m), (6.5m, 71, .5m), (7m, 69, 1m),
                     (10m, 67, 1m), (11m, 71, 1m), (12m, 72, 2m), (14m, 67, 2m)
                 })
            await Draw(note.Item1, note.Item2, note.Item3);
        var pattern = BeatSteps.Resolve(workflow.State).Pattern!;
        var part = pattern.Parts.Single();
        Require(part.Notes.Length == 19 && part.Notes.Any(n => n.Duration.Ticks == 480000),
            "Phrase lost notes or mixed lengths.");
        var original = workflow.State.Snapshot;
        canvas.RaiseEvent(new KeyEventArgs
            { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right, KeyModifiers = KeyModifiers.Alt });
        Require(canvas.Selection.Count == 1 && workflow.State.Snapshot == original,
            "Keyboard note selection mutated music.");
        canvas.Fit();
        var first = part.Notes.First();
        var rect = canvas.NoteRect(first);
        var at = new Point(rect.X + 10, rect.Y + 7);
        Press(at);
        Move(at + new Vector(canvas.QuarterWidth / 4, -canvas.RowHeight));
        await Release(at + new Vector(canvas.QuarterWidth / 4, -canvas.RowHeight));
        Require(
            workflow.State.Snapshot.State.Patterns.Single().Parts.Single().Notes.Single(n => n.Id == first.Id).Pitch ==
            first.Pitch + 1, "Move pitch failed.");
        Require(await workflow.UndoAsync(), "Move Undo failed.");
        pane.Refresh();
        Require(workflow.State.Snapshot == original, "Move Undo lost exact snapshot.");
        Press(at, KeyModifiers.Control);
        Move(at + new Vector(canvas.QuarterWidth, 0), KeyModifiers.Control);
        await Release(at + new Vector(canvas.QuarterWidth, 0), KeyModifiers.Control);
        Require(BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single().Notes.Length == 20, "Ctrl-copy failed.");
        Require(await workflow.UndoAsync(), "Copy Undo failed.");
        pane.Refresh();
        var edge = new Point(rect.Right - 3, rect.Y + 7);
        Press(edge);
        Move(edge + new Vector(canvas.QuarterWidth / 2, 0));
        await Release(edge + new Vector(canvas.QuarterWidth / 2, 0));
        Require(
            BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single().Notes.Single(n => n.Id == first.Id).Duration
                .Ticks == first.Duration.Ticks + 480000, "Resize failed.");
        Require(await workflow.UndoAsync(), "Resize Undo failed.");
        pane.Refresh();
        Press(at);
        Move(at + new Vector(canvas.QuarterWidth, 0));
        canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        await Release(at);
        Require(workflow.State.Snapshot == original, "Escape committed a cancelled gesture.");
        Press(at);
        Move(at + new Vector(canvas.QuarterWidth, 0));
        pointer.Capture(null);
        await Release(at);
        Require(workflow.State.Snapshot == original, "Capture loss committed a cancelled gesture.");
        // A chord has separate handles at the same canonical onset. Drag the middle handle only.
        var velocityAt = new Point(canvas.X(0) + 13, canvas.Bounds.Height - 8 - .8 * (canvas.VelocityHeight - 28));
        Press(velocityAt);
        Move(new(velocityAt.X, canvas.Bounds.Height - 8 - .45 * (canvas.VelocityHeight - 28)));
        await Release(new(velocityAt.X, canvas.Bounds.Height - 8 - .45 * (canvas.VelocityHeight - 28)));
        var chord = BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single().Notes.Where(n => n.Position.Ticks == 0)
            .ToArray();
        Require(chord.Count(n => n.Intensity == .45m) == 1 && chord.Count(n => n.Intensity == .8m) == 2,
            "Overlapping chord velocity hit changed the wrong notes.");
        string save = Path.Combine(output, "piano.seqvium");
        Require(await workflow.SaveAsync(save), "Piano Save failed.");
        var saved = workflow.State.Snapshot;
        Require(await workflow.OpenAsync(save, UnsavedDecision.Discard), "Piano Open failed.");
        pane.Refresh();
        canvas.Fit();
        Require(
            workflow.State.Snapshot.Revision == saved.Revision && BeatSteps.Resolve(workflow.State).Pattern!.Parts
                .Single().Notes.SequenceEqual(saved.State.Patterns.Single().Parts.Single().Notes),
            "Piano reopen changed notes.");
        Require(
            await workflow.ExportWaveAsync(BeatSteps.Resolve(workflow.State).Basis!,
                Path.Combine(output, "pattern.wav")), "Piano export failed: " + workflow.State.Detail);
        var rows = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (var size in new[] { (1100, 750), (640, 480) })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            main.Width = size.Item1;
            main.Height = size.Item2;
            await Task.Delay(120);
            workspace.State.SetBounds(WorkspaceState.PianoId, new(0, 0, workspace.State.Width, workspace.State.Height));
            pane.Refresh();
            main.UpdateLayout();
            canvas.Fit();
            Require(canvas.Bounds.Height >= 120 && canvas.Bounds.Width >= 270,
                "Reduced Piano viewport lost usable music area.");
            rows.Add(new
            {
                language, theme, Width = size.Item1, Height = size.Item2, Canvas = canvas.Bounds,
                StateBounds = workspace.State.Bounds(WorkspaceState.PianoId),
                Origin = canvas.TranslatePoint(default, main), Notes = canvas.Part!.Notes.Length
            });
            await CaptureAsync(main, output, $"piano-{language}-{theme}-{size.Item1}");
            var origin = canvas.TranslatePoint(default, main)!.Value;
            Json(output, $"hierarchy-{language}-{theme}-{size.Item1}", canvas.GetVisualAncestors().OfType<Control>()
                .Prepend(canvas)
                .Select(c => new
                {
                    Type = c.GetType().Name, Id = AutomationProperties.GetAutomationId(c), Bounds = c.Bounds.ToString(),
                    Desired = c.DesiredSize.ToString(),
                    Margin = c.Margin.ToString(),
                    Origin = c.TranslatePoint(default, main)?.ToString(),
                    Left = Avalonia.Controls.Canvas.GetLeft(c).ToString(),
                    Top = Avalonia.Controls.Canvas.GetTop(c).ToString(), Transform = c.RenderTransform?.Value.ToString()
                }));
            Require(
                origin.X >= 0 && origin.Y >= 0 && origin.X + canvas.Bounds.Width <= main.Bounds.Width + 1 &&
                origin.Y + canvas.Bounds.Height <= main.Bounds.Height + 1,
                $"Piano canvas escaped window: {origin}, {canvas.Bounds}, {workspace.State.Bounds(WorkspaceState.PianoId)}.");
            canvas.SelectAll();
            var properties = Field<Dictionary<string, Button>>(pane, "_buttons")["Properties"];
            properties.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            properties.Flyout!.ShowAt(properties);
            await Task.Delay(100);
            var noteBody = (Control)Field<Flyout>(pane, "_noteFlyout").Content!;
            foreach (var field in noteBody.GetVisualDescendants().OfType<NumericUpDown>())
            {
                var row = (Grid)field.Parent!;
                rows.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().Single(), row,
                    $"{language}-{theme}-{size.Item1}-{AutomationProperties.GetAutomationId(field)}"));
            }

            await CapturePopupAsync(noteBody, output, $"properties-{language}-{theme}-{size.Item1}");
            var propertyBefore = workflow.State.Snapshot;
            Field<NumericUpDown>(pane, "_noteVelocity").Value = 50;
            noteBody.GetVisualDescendants().OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await pane.PendingCommand;
            pane.Refresh();
            var velocityNotes = canvas.Part!.Notes;
            Require(velocityNotes.All(n => n.Intensity == .5m) && velocityNotes
                    .Select(n => (n.Id, n.Position, n.Duration, n.Pitch))
                    .SequenceEqual(propertyBefore.State.Patterns.Single().Parts.Single().Notes
                        .Select(n => (n.Id, n.Position, n.Duration, n.Pitch))),
                "Group velocity properties flattened other musical fields.");
            Require(await workflow.UndoAsync(), "Group property Undo failed.");
            pane.Refresh();
            Require(workflow.State.Snapshot == propertyBefore, "Group property Undo lost snapshot.");
            properties.Flyout.Hide();
            var rootButton = Field<Button>(pane, "_root");
            rootButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            rootButton.Flyout!.ShowAt(rootButton);
            await Task.Delay(100);
            var rootBody = (Control)Field<Flyout>(pane, "_rootFlyout").Content!;
            await CapturePopupAsync(rootBody, output, $"root-{language}-{theme}-{size.Item1}");
            var rootBefore = workflow.State.Snapshot;
            Field<ComboBox>(pane, "_rootPitch").SelectedIndex = 57;
            Json(output, "root-authority", new
            {
                Basis = Field<BeatEditBasis?>(pane, "_rootBasis"), CanvasBasis = canvas.Basis,
                Part = Field<Id<MusicalPart>?>(pane, "_rootPart"), Name = Field<TextBox>(pane, "_channelName").Text,
                Pending = pane.PendingCommand.IsCompleted,
                Popup = rootButton.Flyout.IsOpen, Root = Field<ComboBox>(pane, "_rootPitch").SelectedIndex,
                CanEdit = canvas.CanEdit
            });
            rootBody.GetVisualDescendants().OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await pane.PendingCommand;
            pane.Refresh();
            Require(
                workflow.State.Snapshot.State.Sounds.Single().Parameters["rootPitch"] == 57 &&
                canvas.Part!.Notes.SequenceEqual(rootBefore.State.Patterns.Single().Parts.Single().Notes),
                "Root popup did not apply safely: " + workflow.State.Detail);
            Require(await workflow.UndoAsync(), "Root popup Undo failed.");
            pane.Refresh();
            Require(workflow.State.Snapshot == rootBefore, "Root popup Undo lost exact snapshot.");
        }

        workspace.Open(WorkspaceState.BeatId);
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        rack.Refresh();
        Require(rack.GetVisualDescendants().OfType<PartNoteThumbnail>().Any(), "Rack did not present melody preview.");
        Require(workflow.State.Snapshot.Revision == saved.Revision, "Returning to Rack changed melody.");
        await CaptureAsync(main, output, "rack-melody");
        workspace.Open(WorkspaceState.PianoId);
        workspace.Action(WorkspaceState.PianoId, () => workspace.State.Collapse(WorkspaceState.PianoId));
        workspace.Open(WorkspaceState.PianoId);
        Require(ReferenceEquals(pane, main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single()),
            "Piano pane was replaced on restore.");
        Json(output, "piano",
            new
            {
                Result = "passed", ActualWindow = true, RoutedInput = true, NativeInput = false, AudioStarted = false,
                Rows = rows
            });
    }
}
