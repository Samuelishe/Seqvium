// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task UnfoldRepeatsAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var document = Field<ProjectDocument>(workflow, "_document");
        var input = new NativePointer.DrumInput(main);
        var driver = new ControlInputDriver(main, !native, output);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900; main.Position = new(0, 0);
        Require(await workflow.CreateBeatAsync(document.LifecycleId, document.Generation, "Unfold study"), "Fixture Pattern");
        Require(await workflow.AddDrumVoiceAsync(BeatSteps.Resolve(workflow.State).Basis!, "Keys", SynthVoice.Algorithm), "Fixture synth");
        var source = BeatSteps.Resolve(workflow.State).Pattern!;
        Require(await workflow.EditPianoNotesAsync(BeatSteps.Resolve(workflow.State).Basis!, source.Parts[0].Id,
            [new(Id<NoteEvent>.New(), default, new(960000), 60, .1m), new(Id<NoteEvent>.New(), new(1920000), new(960000), 67, .1m)]), "Fixture notes");
        source = BeatSteps.Resolve(workflow.State).Pattern!;
        Id<ArrangementClip> target = default;
        document.Edit("Repeated uses", e =>
        {
            e.SetSettings(new(new(137.123456m), new(7, 8)));
            e.SetPatternLength(source.Id, new(3123457));
            var placement = e.State.Placements.Single();
            target = e.AddArrangementClip(placement.Id, new(123456), 0, 4);
            e.AddArrangementClip(placement.Id, new(123456), 1, 4);
        });
        foreach (var pane in workspace.State.Panes.Where(p => p.Visibility == PaneVisibility.Visible).ToArray())
            workspace.State.Hide(pane.InstanceId);
        workspace.Open(WorkspaceState.ArrangementId);
        var playlist = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        var canvas = playlist.Canvas;
        var scroll = Field<ScrollViewer>(playlist, "_scroll");
        canvas.Snap = ArrangementSnap.Off;
        async Task Settle()
        {
            await workflow.ObserveAsync(); main.UpdateLayout(); await Task.Delay(80);
        }
        async Task Size(int width)
        {
            workspace.Open(WorkspaceState.ArrangementId);
            workspace.State.SetBounds(WorkspaceState.ArrangementId, new(5, 5, width, 490)); workspace.Commit();
            await Settle(); scroll.Offset = default; canvas.Fit(width - 32); await Settle();
        }
        ArrangementClip Clip() => document.Current.State.ArrangementClips.Single(c => c.Id == target);
        Point At(ArrangementClip c, double cycle = .45) => new(canvas.HeaderSize +
            (c.Position.Ticks + 3123457 * cycle) * canvas.TickPixels, canvas.ClipRect(c).Y + 13);
        async Task Open(ArrangementClip c, double cycle = .45)
        {
            if (native) await input.RightClick(canvas, At(c, cycle));
            else Invoke(canvas, "OpenContextMenu", At(c, cycle));
            await Settle();
        }
        MenuItem Item(string key) => canvas.ContextMenu!.Items.OfType<MenuItem>().Single(i =>
            AutomationProperties.GetAutomationId(i) == "Arrangement.Context." + key);
        async Task Dismiss()
        {
            if (native) input.Key(0x1B, true); else canvas.ContextMenu!.Close();
            await Settle();
        }
        async Task ClickMenu(string key)
        {
            await driver.Click(Item(key)); await playlist.PendingCommand;
            canvas.ContextMenu!.Close(); await Settle();
        }
        async Task Select(ArrangementClip c, int count = 1)
        {
            if (native) await input.Click(canvas, At(c));
            else
            {
                using var pointer = new Pointer(918337, PointerType.Mouse, true);
                var at = canvas.TranslatePoint(At(c), main)!.Value;
                canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, main, at, 0,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, count));
                canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, main, at, 0,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            }
            await playlist.PendingCommand; await Settle();
            Require(canvas.Selection.SequenceEqual([c.Id]) && canvas.AudioSelection.Length == 0, "Individual selection");
        }
        await Size(1080); main.Activate(); await CaptureAsync(main, output, "startup");
        if (native)
        {
            try { await driver.ActivateVisibleWindow(); }
            catch (Exception error) when (error.GetType().Name == "ForegroundLostException" ||
                                         error.Message.StartsWith("Native startup window is covered", StringComparison.Ordinal))
            {
                Json(output, "foreground-wait", new { error.Message, WaitingSeconds = 45, InputSent = false });
                await input.WaitForForegroundAsync(TimeSpan.FromSeconds(45));
            }
        }
        var rows = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (int width in new[] { 1080, 520 })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            await Size(width);
            string name = $"{language}-{theme}-{width}";
            var before = document.Current; int undo = document.UndoCount;
            await CaptureAsync(main, output, "before-" + name);
            await Open(Clip(), 2.45);
            Require(Item("UnfoldRepeats").IsEnabled && Item("UnfoldRepeats").Header?.ToString() == shell["Arrangement.UnfoldRepeats"], "Localized action");
            await CapturePopupAsync(canvas.ContextMenu!, output, "menu-" + name);
            var dismissed = Item("UnfoldRepeats"); await Dismiss();
            dismissed.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); await playlist.PendingCommand; await Settle();
            Require(document.Current == before, "Dismissed action edited music");
            await Open(Clip(), 2.45); await ClickMenu("UnfoldRepeats");
            var after = document.Current;
            Require(after != before && document.UndoCount == undo + 1, "One unfold / one Undo");
            Require(after.State.ArrangementClips.Length == 5 && Clip().Repeats == 1 && canvas.Selected == target, "Unfold projection and first selection");
            Require(ProjectDocument.HasSameContent(before.State, after.State with { ArrangementClips = before.State.ArrangementClips }), "Sources changed");
            await CaptureAsync(main, output, "after-" + name);
            var middle = after.State.ArrangementClips[2]; await Select(middle);
            await Open(middle); Require(!Item("UnfoldRepeats").IsEnabled, "Single-repeat enabled"); await Dismiss();
            Require(await workflow.UndoAsync(), "Undo"); Require(document.Current == before, "Exact Undo");
            Require(await workflow.UndoAsync(true), "Redo"); Require(document.Current == after, "Exact Redo IDs");
            Require(await workflow.UndoAsync(), "Restore matrix"); await Settle();
            rows.Add(new { Language = language.ToString(), Theme = theme.ToString(), Width = width, NativeInput = native,
                Dismissed = true, IndividualSelection = true, OneUndoExactRedo = true });
            Json(output, "matrix-progress", rows);
        }
        await Size(1080);
        // A captured item must not follow selection changes or a generation/Undo ABA.
        await Open(Clip()); var obsolete = Item("UnfoldRepeats"); var snapshot = document.Current;
        canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.A, KeyModifiers = KeyModifiers.Control });
        obsolete.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); await playlist.PendingCommand; await Settle();
        Require(document.Current == snapshot, "Retargeted menu edited"); canvas.ContextMenu!.Close();
        await Open(Clip()); obsolete = Item("UnfoldRepeats");
        document.Edit("External edit", e => e.UpdateArrangementClip(target, new(123457), 0, 4));
        Require(await workflow.UndoAsync(), "ABA restore");
        obsolete.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); await playlist.PendingCommand; await Settle();
        Require(document.Current == snapshot, "Stale ABA menu edited"); canvas.ContextMenu!.Close();
        await Open(Clip()); obsolete = Item("UnfoldRepeats");
        Require(await workflow.SaveAsync(Path.Combine(output, "busy-study.seqvium")), "Busy command");
        obsolete.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); await playlist.PendingCommand; await Settle();
        Require(document.Current == snapshot, "Busy-invalidated menu edited");
        await Open(Clip()); obsolete = Item("UnfoldRepeats");
        workspace.State.Hide(WorkspaceState.ArrangementId); workspace.Commit(); await Settle();
        await Size(1080);
        obsolete.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); await playlist.PendingCommand; await Settle();
        Require(document.Current == snapshot, "Hidden menu edited after reopening");
        await Open(Clip()); await ClickMenu("UnfoldRepeats");
        var unfolded = document.Current; var phrase = unfolded.State.ArrangementClips[2];
        await Select(phrase);
        await driver.Drag(canvas, At(phrase), At(phrase) + new Vector(30, 40));
        await playlist.PendingCommand; await Settle();
        var moved = document.Current.State.ArrangementClips.Single(c => c.Id == phrase.Id);
        Require(moved.Position != phrase.Position && moved.Lane == 1, "Individual move");
        foreach (var c in unfolded.State.ArrangementClips.Where(c => c.Id != phrase.Id))
            Require(document.Current.State.ArrangementClips.Single(a => a.Id == c.Id) == c, "Move changed neighbour");
        await Open(moved); await ClickMenu("Copy"); await Open(moved); await ClickMenu("Paste");
        Require(document.Current.State.ArrangementClips.Length == unfolded.State.ArrangementClips.Length + 1 &&
            document.Current.State.ArrangementClips[^1].SourcePlacementId == phrase.SourcePlacementId, "Shared copy");
        Require(await workflow.UndoAsync() && await workflow.UndoAsync(), "Restore move/copy"); await Settle();
        await Select(phrase); await Open(phrase); await ClickMenu("Variation");
        var state = document.Current.State;
        var placement = state.Placements.Single(p => p.Id == state.ArrangementClips.Single(c => c.Id == phrase.Id).SourcePlacementId);
        var fill = state.Patterns.Single(p => p.Id == placement.PatternId);
        Require(fill.VariationOf == source.Id && state.ArrangementClips.Where(c => c.Id != phrase.Id)
            .All(c => c.SourcePlacementId == phrase.SourcePlacementId), "Individual variation");
        await Size(1080); await Select(state.ArrangementClips.Single(c => c.Id == phrase.Id));
        if (native)
        {
            var c = document.Current.State.ArrangementClips.Single(c => c.Id == phrase.Id);
            await input.Click(canvas, At(c)); await input.Click(canvas, At(c)); await playlist.PendingCommand; await Settle();
        }
        else
            await Select(document.Current.State.ArrangementClips.Single(c => c.Id == phrase.Id), 2);
        Require(workflow.State.Selected == state.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId).Id, "Existing editor target");
        ProjectPersistence.Save(document, Path.Combine(output, "unfold-study.seqvium"));
        object physical = new { Requested = audio, Run = false };
        if (audio)
        {
            await workflow.SetSongModeAsync(true); await workflow.SeekSongAsync(default);
            Require(await workflow.PlayAsync(), "Physical Song: " + workflow.State.Detail);
            await Task.Delay(900);
            var device = Field<WorkflowOutput>(workflow, "_output"); long epoch = device.Sampler.TransportEpoch;
            Require(device.ReadFailure() is null && device.Sampler.ReadStatus().Playing, "Physical output fault");
            var description = device.Description;
            var repeated = document.Current.State.ArrangementClips.Single(c => c.Repeats > 1);
            long position = device.Sampler.RenderPosition;
            Require(await workflow.UnfoldArrangementRepeatsAsync(ArrangementEditBasis.From(workflow.State), repeated.Id), "Playing unfold");
            await Task.Delay(500); await workflow.ObserveAsync();
            Json(output, "physical-observation", new { Device = description, BeforeEpoch = epoch,
                BeforePosition = position, AfterEpoch = device.Sampler.TransportEpoch, AfterPosition = device.Sampler.RenderPosition,
                Failure = device.ReadFailure(), Status = device.Sampler.ReadStatus(), workflow.State.Detail });
            Require(device.ReadFailure() is null && device.Sampler.TransportEpoch == epoch &&
                device.Sampler.RenderPosition > position, "Physical unfold continuation failed");
            await workflow.StopAsync(); physical = new { Requested = true, Run = true, Device = description, StableEpoch = true };
        }
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "Empty fixture");
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (int width in new[] { 1080, 520 })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            await Size(width); var empty = document.Current;
            var point = new Point(canvas.HeaderSize + 40, 100);
            if (native) await input.RightClick(canvas, point); else Invoke(canvas, "OpenContextMenu", point);
            await Settle();
            Require(!canvas.ContextMenu!.Items.OfType<MenuItem>().Any(i =>
                AutomationProperties.GetAutomationId(i) == "Arrangement.Context.UnfoldRepeats"), "Empty action present");
            await CaptureAsync(main, output, $"empty-{language}-{theme}-{width}");
            await Dismiss(); Require(document.Current == empty, "Empty dismissal edited");
        }
        Json(output, "unfold-repeats", new { Matrix = rows, NativeInput = native, Selection = true, Move = true, Copy = true,
            Variation = true, EditorTarget = true, StaleAba = true, Retargeted = true, Physical = physical, SubjectiveListening = false });
    }
}
