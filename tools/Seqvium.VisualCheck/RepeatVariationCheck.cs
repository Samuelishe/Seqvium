// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task RepeatVariationAsync(MainWindow main, string output, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var input = new NativePointer.DrumInput(main);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900; main.Position = new(0, 0);
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Repeat study"), "Create fixture");
        Require(await workflow.AddDrumVoiceAsync(BeatSteps.Resolve(workflow.State).Basis!, "Keys", SynthVoice.Algorithm), "Add synth");
        var source = BeatSteps.Resolve(workflow.State).Pattern!;
        Require(await workflow.EditPianoNotesAsync(BeatSteps.Resolve(workflow.State).Basis!, source.Parts[0].Id,
            [new(Id<NoteEvent>.New(), new(0), new(960000), 60, .1m), new(Id<NoteEvent>.New(), new(2880000), new(960000), 67, .1m)]), "Fixture notes");
        source = BeatSteps.Resolve(workflow.State).Pattern!;
        var document = Field<ProjectDocument>(workflow, "_document");
        Id<ArrangementClip> target = default;
        document.Edit("Repeated study", e =>
        {
            var placement = e.State.Placements.Single(p => p.PatternId == source.Id);
            target = e.AddArrangementClip(placement.Id, new(3840000), 0, 4);
            e.AddArrangementClip(placement.Id, new(3840000), 1, 4);
            e.AddArrangementSection("Phrase", default, new(6 * 3840000), 1);
        });
        await workflow.ObserveAsync();
        foreach (var pane in workspace.State.Panes.Where(p => p.Visibility == PaneVisibility.Visible).ToArray())
            workspace.State.Hide(pane.InstanceId);
        workspace.Open(WorkspaceState.ArrangementId);
        void Size(int width)
        {
            workspace.Open(WorkspaceState.ArrangementId);
            workspace.State.SetBounds(WorkspaceState.ArrangementId, new(5, 5, width, 490)); workspace.Commit();
            main.UpdateLayout();
        }
        Size(1080);
        var playlist = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        var canvas = playlist.Canvas;
        var scroll = Field<ScrollViewer>(playlist, "_scroll");
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c =>
            c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);
        async Task Settle()
        {
            await workflow.ObserveAsync(); main.UpdateLayout(); await Task.Delay(120);
        }
        ArrangementClip Clip() => workflow.State.Snapshot.State.ArrangementClips.Single(c => c.Id == target);
        Point PointAt(int index)
        {
            var clip = Clip();
            return new(canvas.HeaderSize + (clip.Position.Ticks + (index + .45) * source.Length.Ticks) * canvas.TickPixels,
                canvas.ClipRect(clip).Y + 12);
        }
        async Task Reveal(int index)
        {
            await Settle();
            double x = PointAt(index).X;
            scroll.Offset = new(Math.Max(0, x - canvas.HeaderSize - 80), 0);
            await Settle();
        }
        MenuItem Unique() => canvas.ContextMenu!.Items.OfType<MenuItem>().Single(i =>
            AutomationProperties.GetAutomationId(i) == "Arrangement.Context.RepeatUnique");
        await Settle(); main.Activate(); await Task.Delay(600);
        await CaptureAsync(main, output, "startup");
        try { await input.ActivateVisibleWindow(); }
        catch (InvalidOperationException error) when (error.Message.StartsWith("Native startup window is covered", StringComparison.Ordinal))
        {
            // A busy desktop may cover a normally raised window. Wait for deliberate user focus;
            // never force foreground again or inject through the covering application.
            Json(output, "foreground-wait", new { Reason = error.Message, WaitingSeconds = 45, InputSent = false });
            await input.WaitForForegroundAsync(TimeSpan.FromSeconds(45));
        }
        var results = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (int width in new[] { 1080, 520 })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            Size(width); canvas.Zoom(width == 520); await Reveal(2);
            var clip = Clip();
            // Test exact and neighbouring half-open boundaries at the actual zoom/scroll.
            for (int i = 0; i <= 4; i++)
            {
                double x = canvas.HeaderSize + (clip.Position.Ticks + i * source.Length.Ticks) * canvas.TickPixels;
                // A scrolled-off boundary belongs to the fixed header and is deliberately not a target.
                if (x < scroll.Offset.X + canvas.HeaderSize) continue;
                Require(canvas.RepeatAt(clip, new(x, PointAt(2).Y)) == (i == 4 ? null : i), "Exact boundary hit");
                if (i > 0) Require(canvas.RepeatAt(clip, new(x - .001, PointAt(2).Y)) == i - 1, "Left neighbour hit");
            }
            var before = document.Current;
            await CaptureAsync(main, output, $"before-{language}-{theme}-{width}");
            Json(output, $"target-{language}-{theme}-{width}", new { Point = PointAt(2), Offset = scroll.Offset,
                Canvas = canvas.Bounds, Screen = canvas.PointToScreen(PointAt(2)).ToString(),
                ClientHit = main.InputHitTest(main.PointToClient(canvas.PointToScreen(PointAt(2))))?.ToString() });
            await input.RightClick(canvas, PointAt(2));
            Require(Unique().Header?.ToString() == shell["Arrangement.RepeatUnique"], "Localized menu");
            await CaptureAsync(main, output, $"menu-{language}-{theme}-{width}");
            await CapturePopupAsync(Unique(), output, $"popup-{language}-{theme}-{width}");
            input.Key(0x1B, true); await Settle();
            Require(document.Current == before, "Escape edited music");
            results.Add(new { Language = language.ToString(), Theme = theme.ToString(), Width = width,
                Zoom = canvas.TickPixels, Scroll = scroll.Offset.X, NativeMenu = true, ExactBoundaries = true, Escape = true });
            Json(output, "repeat-progress", new { Phase = "menus", Checks = results.ToArray() });
        }
        if (shell.Preferences.Language != HostLanguage.English) await shell.ToggleLanguageAsync();
        if (shell.Preferences.Theme != HostTheme.Dark) await shell.ToggleThemeAsync();
        Size(1080);
        foreach (int index in new[] { 0, 1, 3 })
        {
            await Reveal(index); var before = document.Current; int undo = document.UndoCount;
            await input.RightClick(canvas, PointAt(index)); await input.Click(Unique());
            await playlist.PendingCommand; await Settle();
            Require(document.UndoCount == undo + 1, "One command must be one Undo");
            var unique = document.Current.State.ArrangementClips.Single(c => c.Id == canvas.Selected);
            Require(unique.Repeats == 1 && unique.Position.Ticks == Clip().Position.Ticks + (index == 0 ? -source.Length.Ticks : index * source.Length.Ticks), "Selected repeat position");
            var placement = document.Current.State.Placements.Single(p => p.Id == unique.SourcePlacementId);
            var variation = document.Current.State.Patterns.Single(p => p.Id == placement.PatternId);
            Require(variation.VariationOf == source.Id && workflow.State.Selected == document.Current.State.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId).Id,
                "Independent editor target");
            await CaptureAsync(main, output, "unique-" + index);
            Require(await workflow.UndoAsync(), "Undo"); Require(document.Current == before, "Exact native result Undo");
            Require(await workflow.UndoAsync(true), "Redo");
            if (index != 3) { Require(await workflow.UndoAsync(), "Restore fixture"); Size(1080); }
            results.Add(new { NativeUniqueIndex = index, SelectedCenter = true, OneUndo = true, ExactUndoRedo = true });
            Json(output, "repeat-progress", new { Phase = "detachment", Checks = results.ToArray() });
        }
        // Existing Rack/Piano route: edit one note through real Windows input.
        var selectedClip = document.Current.State.ArrangementClips.Single(c => document.Current.State.Patterns
            .Single(p => p.Id == document.Current.State.Placements.Single(p => p.Id == c.SourcePlacementId).PatternId).VariationOf == source.Id);
        Size(1080); await Settle();
        double centerX = canvas.HeaderSize + (selectedClip.Position.Ticks + source.Length.Ticks * .45) * canvas.TickPixels;
        scroll.Offset = new(Math.Max(0, centerX - canvas.HeaderSize - 80), 0); await Settle();
        var centerPoint = new Point(centerX, canvas.ClipRect(selectedClip).Y + 12);
        var beforeOpen = document.Current;
        await input.Click(canvas, centerPoint); await input.Click(canvas, centerPoint);
        await playlist.PendingCommand; await Settle();
        Require(document.Current == beforeOpen, "Opening a clip changed music");
        var selectedPlacement = document.Current.State.Placements.Single(p => p.Id == selectedClip.SourcePlacementId);
        var fill = document.Current.State.Patterns.Single(p => p.Id == selectedPlacement.PatternId);
        Require(workflow.State.Selected == document.Current.State.GraphAttachments.Single(a => a.ContextId == selectedPlacement.ItemContextId).Id,
            "Reopened independent target");
        workspace.Open(WorkspaceState.BeatId); workspace.State.SetBounds(WorkspaceState.BeatId, new(5, 5, 1080, 380)); workspace.Commit();
        await Settle();
        await input.Click(Id("Beat.Actions." + fill.Parts[0].Id));
        var actions = ((Button)Id("Beat.Actions." + fill.Parts[0].Id)).ContextMenu!;
        await input.Click(actions.Items.OfType<MenuItem>().Single(i => AutomationProperties.GetAutomationId(i) == "Beat.Piano." + fill.Parts[0].Id));
        workspace.Open(WorkspaceState.PianoId); workspace.State.SetBounds(WorkspaceState.PianoId, new(5, 5, 1080, 550)); workspace.Commit();
        await Settle(); await input.Click(Id("Piano.Fit"));
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        var note = piano.Canvas.Part!.Notes[0]; var rect = piano.Canvas.NoteRect(note);
        await input.Click(piano.Canvas, new(rect.X + 8, rect.Y + rect.Height / 2));
        await input.Click(Id("Piano.Properties"));
        var flyout = Field<Flyout>(piano, "_noteFlyout");
        Control Property(string id) => ((Control)flyout.Content!).GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        await input.Exact(Property("Piano.NotePitch"), "65"); await input.Click(Property("Piano.Apply"));
        await Settle();
        Require(document.Current.State.Patterns.Single(p => p.Id == fill.Id).Parts[0].Notes.Single(n => n.Id == note.Id).Pitch == 65,
            "Native Piano edit");
        Require(document.Current.State.Patterns.Single(p => p.Id == source.Id) == source, "Shared repeats changed");
        await CaptureAsync(main, output, "independent-piano");
        ProjectPersistence.Save(document, Path.Combine(output, "repeat-study.seqvium"));
        object physical = new { Requested = audio, Run = false };
        if (audio)
        {
            await workflow.SetSongModeAsync(false);
            Require(await workflow.PlayAsync(), "Physical Pattern: " + workflow.State.Detail);
            await Task.Delay(1400);
            var device = Field<WorkflowOutput>(workflow, "_output");
            Require(device.ReadFailure() is null && device.Sampler.ReadStatus().Playing, "Pattern output failure");
            var description = device.Description; await workflow.StopAsync();
            Require(await workflow.SeekSongAsync(selectedClip.Position), "Physical variation seek");
            await workflow.SetSongModeAsync(true);
            Require(await workflow.PlayAsync(), "Physical Song: " + workflow.State.Detail);
            await Task.Delay(1800); device = Field<WorkflowOutput>(workflow, "_output");
            Require(device.ReadFailure() is null && device.Sampler.ReadStatus().Playing, "Song output failure");
            await workflow.StopAsync();
            physical = new { Requested = true, Run = true, Device = description, PatternMs = 1400, SongMs = 1800 };
        }
        Json(output, "repeat-variation", new { Results = results, NativePianoLocalEdit = true,
            OriginalPatternUnchanged = true, Physical = physical, SubjectiveListening = false });
    }

    private static async Task RepeatVariationAudioAsync(MainWindow main, string output, string project)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        Require(await workflow.OpenAsync(Path.GetFullPath(project), UnsavedDecision.Discard), "Open native study");
        var snapshot = workflow.State.Snapshot;
        var state = snapshot.State;
        var variation = state.Patterns.Single(p => p.VariationOf is not null);
        var placement = state.Placements.Single(p => p.PatternId == variation.Id);
        var graph = state.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId);
        var clip = state.ArrangementClips.Single(c => c.SourcePlacementId == placement.Id);
        await workflow.SelectAsync(graph.Id);
        await workflow.SetSongModeAsync(false);
        Require(await workflow.PlayAsync(), "Physical Pattern: " + workflow.State.Detail);
        await Task.Delay(1400); await workflow.ObserveAsync();
        var device = Field<WorkflowOutput>(workflow, "_output");
        Require(device.ReadFailure() is null && device.Sampler.ReadStatus().Playing && device.Sampler.RenderPosition > 0,
            "Physical Pattern status");
        string description = device.Description;
        await workflow.StopAsync();
        await workflow.SetSongModeAsync(true); await workflow.SeekSongAsync(clip.Position);
        Require(await workflow.PlayAsync(), "Physical Song: " + workflow.State.Detail);
        await Task.Delay(1800); await workflow.ObserveAsync();
        device = Field<WorkflowOutput>(workflow, "_output");
        Require(device.ReadFailure() is null && device.Sampler.ReadStatus().Playing && device.Sampler.ActiveVoices > 0,
            "Physical Song status");
        await workflow.StopAsync();
        Require(workflow.State.Snapshot == snapshot && !workflow.State.IsDirty, "Physical check edited project");
        Json(output, "repeat-physical", new { Device = description, PatternMs = 1400, SongMs = 1800,
            SongStartedAtVariation = true, ProjectUnchanged = true, PhysicalAudio = true, NativeInput = false,
            SubjectiveListening = false });
    }

    private static async Task RepeatVariationEmptyAsync(MainWindow main, string output)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        main.Width = 1400; main.Height = 900;
        workspace.State.Hide(WorkspaceState.BeatId); workspace.Open(WorkspaceState.ArrangementId);
        workspace.State.SetBounds(WorkspaceState.ArrangementId, new(5, 5, 520, 490)); workspace.Commit();
        var playlist = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        var snapshot = workflow.State.Snapshot;
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            main.UpdateLayout(); await Task.Delay(120);
            Invoke(playlist.Canvas, "OpenContextMenu", new Point(playlist.Canvas.HeaderSize + 40, 100));
            Require(!playlist.Canvas.ContextMenu!.Items.OfType<MenuItem>().Any(i =>
                AutomationProperties.GetAutomationId(i) == "Arrangement.Context.RepeatUnique"), "Empty repeat command");
            await CaptureAsync(main, output, $"empty-{language}-{theme}");
            await CapturePopupAsync(playlist.Canvas.ContextMenu.Items.OfType<MenuItem>().First(), output,
                $"empty-popup-{language}-{theme}");
            playlist.Canvas.ContextMenu.Close();
        }
        Require(workflow.State.Snapshot == snapshot && workflow.State.UndoCount == 0, "Empty review edited music");
        Json(output, "repeat-empty", new { Cases = 4, Width = 520, RoutedMenu = true, NativeInput = false, EmptyActionAbsent = true });
    }
}
