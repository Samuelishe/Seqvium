// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
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
    private static async Task ArrangementMatrixAsync(MainWindow main, string output)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        workspace.Open(WorkspaceState.ArrangementId); main.UpdateLayout();
        var pane = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        await CaptureAsync(main, output, "playlist-empty");
        var sources = new List<Id<GraphAttachment>>();
        foreach (var name in new[] { "Drums", "Bass", "Break" })
        {
            var observation = workflow.State;
            Require(await workflow.CreateBeatAsync(observation.Document.LifecycleId, observation.Generation, name), "Pattern creation failed.");
            string path = Path.Combine(output, name + ".wav"); WriteStepWav(path, .2f);
            Require(await workflow.AddBeatWavAsync(BeatSteps.Resolve(workflow.State).Basis!, path), "Channel creation failed.");
            var pattern = BeatSteps.Resolve(workflow.State).Pattern!;
            foreach (int step in name == "Bass" ? new[] { 0, 6, 10 } : new[] { 0, 4, 8, 12 })
                Require(await workflow.ToggleBeatStepAsync(BeatSteps.Resolve(workflow.State).Basis!, pattern.Parts[0].Id, step), "Rhythm creation failed.");
            sources.Add(workflow.State.Selected!.Value);
        }
        Require(await workflow.SelectAsync(sources[0]), "Source selection failed.");
        var canvas = pane.Canvas; main.UpdateLayout();
        using var pointer = new Pointer(100084, PointerType.Mouse, true);
        Point Root(Point at) => canvas.TranslatePoint(at, main)!.Value;
        void Press(Point at, int count = 1, KeyModifiers modifiers = KeyModifiers.None) => canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, main, Root(at), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), modifiers, count));
        void Move(Point at) => canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, canvas, pointer, main, Root(at), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
        async Task Release(Point at)
        {
            canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, main, Root(at), 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            await pane.PendingCommand; await workflow.ObserveAsync(); Invoke(pane, "Refresh"); main.UpdateLayout();
        }
        var before = workflow.State.Snapshot; int undo = workflow.State.UndoCount;
        var firstPoint = new Point(HostMetrics.Values["Playlist.HeaderWidth"] + 12, HostMetrics.Values["Playlist.RulerHeight"] + 30);
        Press(firstPoint); await Release(firstPoint);
        var clip = workflow.State.Snapshot.State.ArrangementClips.Single();
        Require(clip.Position.Ticks == 0 && clip.Lane == 0 && workflow.State.UndoCount == undo + 1, "Routed placement is not one canonical edit.");
        var rect = canvas.ClipRect(clip);
        Press(rect.Center); Move(rect.Center + new Vector(192, HostMetrics.Values["Playlist.LaneHeight"] * 2));
        await Release(rect.Center + new Vector(192, HostMetrics.Values["Playlist.LaneHeight"] * 2));
        clip = workflow.State.Snapshot.State.ArrangementClips.Single();
        Require(clip.Position.Ticks == 2 * 3840000L && clip.Lane == 2, "Captured clip drag did not move in musical time.");
        Require(await workflow.UndoAsync(), "Move Undo failed."); Invoke(pane, "Refresh");
        clip = workflow.State.Snapshot.State.ArrangementClips.Single(); rect = canvas.ClipRect(clip);
        Press(new(rect.Right - 2, rect.Center.Y)); Move(new(rect.Right + 96, rect.Center.Y)); await Release(new(rect.Right + 96, rect.Center.Y));
        Require(workflow.State.Snapshot.State.ArrangementClips.Single().Repeats == 2, "Right edge did not repeat Pattern.");
        Require(await workflow.UndoAsync(), "Repeat Undo failed."); Invoke(pane, "Refresh");
        var placed = workflow.State.Snapshot;
        Press(rect.Center, modifiers: KeyModifiers.Control); Move(rect.Center + new Vector(192, 72)); await Release(rect.Center + new Vector(192, 72));
        Require(workflow.State.Snapshot.State.ArrangementClips.Length == 2 && workflow.State.Snapshot.State.ArrangementClips[0] == clip,
            "Ctrl-drag did not preserve the original shared clip.");
        Require(await workflow.UndoAsync(), "Ctrl-drag Undo failed."); Invoke(pane, "Refresh");
        Press(rect.Center); Move(rect.Center + new Vector(96, 64)); canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        await Release(rect.Center + new Vector(96, 64)); Require(workflow.State.Snapshot == placed, "Escape committed a clip draft.");
        Press(rect.Center); Move(rect.Center + new Vector(96, 64)); pointer.Capture(null); await Release(rect.Center);
        Require(workflow.State.Snapshot == placed, "Capture loss committed a clip draft.");
        Press(rect.Center); Move(rect.Center + new Vector(96, 64)); workspace.CancelGestures(); await Release(rect.Center);
        Require(workflow.State.Snapshot == placed, "Workspace deactivation cancellation committed a clip draft.");
        Press(rect.Center); Move(rect.Center + new Vector(96, 64)); workspace.State.Hide(WorkspaceState.ArrangementId); main.UpdateLayout();
        await Release(rect.Center); Require(workflow.State.Snapshot == placed, "Hiding Playlist committed a clip draft.");
        workspace.Open(WorkspaceState.ArrangementId); Invoke(pane, "Refresh");
        Press(rect.Center); Move(rect.Center + new Vector(96, 64));
        Require(await workflow.DuplicateArrangementClipAsync(ArrangementEditBasis.From(workflow.State), clip.Id), "Intervening edit failed.");
        await Release(rect.Center + new Vector(96, 64));
        Require(workflow.State.Snapshot.State.ArrangementClips[0] == clip, "Stale captured drag committed.");
        Require(await workflow.UndoAsync(), "Duplicate Undo failed."); Invoke(pane, "Refresh");
        Press(rect.Center); await Release(rect.Center);
        var duplicate = pane.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Arrangement.Duplicate");
        duplicate.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await pane.PendingCommand; Invoke(pane, "Refresh");
        Require(workflow.State.Snapshot.State.ArrangementClips.Length == 2, "Duplicate button failed.");
        canvas.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Delete }); await pane.PendingCommand;
        Require(workflow.State.Snapshot.State.ArrangementClips.Length == 1, "Delete key failed.");
        Require(await workflow.UndoAsync(), "Delete Undo failed."); Require(await workflow.UndoAsync(), "Button duplicate Undo failed.");
        Require(await workflow.UndoAsync(), "Place Undo failed."); Require(workflow.State.Snapshot == before, "Placement Undo lost exact prior identity.");
        for (int i = 0; i < 8; i++)
            Require(await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State), sources[0], new(i * 3840000L), 0), "Drums arrangement failed.");
        Require(await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State), sources[1], new(2 * 3840000L), 1, 4), "Bass arrangement failed.");
        Require(await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State), sources[2], new(6 * 3840000L), 2, 2), "Break arrangement failed.");
        Invoke(pane, "Refresh");
        var snap = Field<ComboBox>(pane, "_snap"); snap.SelectedIndex = 2;
        Require(canvas.SnapTicks == 240000, "Snap selector did not select actual sixteenths."); snap.SelectedIndex = 0;
        Press(new(24, HostMetrics.Values["Playlist.RulerHeight"] + 44)); await Release(new(24, HostMetrics.Values["Playlist.RulerHeight"] + 44));
        Require(workflow.State.Snapshot.State.ArrangementTracks.Single(t => t.Lane == 0).Muted, "Track M control failed.");
        Require(await workflow.UndoAsync(), "Mute Undo failed."); Invoke(pane, "Refresh");
        var tracks = pane.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Arrangement.Tracks");
        tracks.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); main.UpdateLayout();
        Field<TextBox>(pane, "_trackName").Text = "Rhythm";
        var colors = Field<List<Button>>(pane, "_trackColors"); colors[3].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var trackPopup = (Control)Field<Flyout>(pane, "_trackFlyout").Content!;
        await CapturePopupAsync(trackPopup, output, "playlist-track-settings");
        Require(colors.All(color => color.Content is Border { Background: not null }), "Track color swatches did not resolve theme brushes.");
        var applyTrack = trackPopup.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Arrangement.ApplyTrack");
        applyTrack.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await pane.PendingCommand;
        Require(workflow.State.Snapshot.State.ArrangementTracks.Single(t => t.Lane == 0) is { Name: "Rhythm", ColorIndex: 3 }, "Track editor did not commit name and color.");
        Require(await workflow.ConfigureArrangementTrackAsync(ArrangementEditBasis.From(workflow.State), 1, "Bass", 6, false, false), "Bass track configuration failed.");
        Require(await workflow.ConfigureArrangementTrackAsync(ArrangementEditBasis.From(workflow.State), 2, "Break", 2, false, false), "Break track configuration failed.");
        Require(await workflow.SetSongModeAsync(true), "Song mode failed."); Invoke(pane, "Refresh");
        Press(canvas.ClipRect(workflow.State.Snapshot.State.ArrangementClips[0]).Center); await Release(canvas.ClipRect(workflow.State.Snapshot.State.ArrangementClips[0]).Center);
        var alignment = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            main.Width = 1280; main.Height = 800; await System.Threading.Tasks.Task.Delay(80);
            workspace.State.SetBounds(WorkspaceState.ArrangementId, new(24, 24, 1000, 490));
            await CaptureAsync(main, output, $"playlist-{language}-{theme}");
            if (language == HostLanguage.Russian && theme == HostTheme.Dark)
            {
                var overview = Field<ArrangementOverview>(pane, "_overview");
                var scroll = Field<ScrollViewer>(pane, "_scroll");
                var music = workflow.State.Snapshot; int history = workflow.State.UndoCount;
                Point overviewAt = overview.TranslatePoint(new(overview.Bounds.Width * .65, overview.Bounds.Height / 2), main)!.Value;
                overview.RaiseEvent(new PointerPressedEventArgs(overview, pointer, main, overviewAt, 0,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
                overview.RaiseEvent(new PointerReleasedEventArgs(overview, pointer, main, overviewAt, 0,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
                Require(scroll.Offset.X > 0 && workflow.State.Snapshot == music && workflow.State.UndoCount == history, "Overview navigation edited music or did not scroll.");
                await CaptureAsync(main, output, "playlist-sticky-headers");
                scroll.Offset = default;
                var fit = pane.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Arrangement.Fit");
                fit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Require(workflow.State.Snapshot == music, "Fit song edited music.");
            }
            alignment.Add(FieldAlignment.Verify(Field<NumericUpDown>(pane, "_repeats"), Field<TextBlock>(pane, "_repeatLabel"), main, $"playlist-{language}-{theme}"));
            main.Width = 640; main.Height = 520; await System.Threading.Tasks.Task.Delay(80);
            workspace.State.SetBounds(WorkspaceState.ArrangementId, new(0, 0, 450, 320));
            await CaptureAsync(main, output, $"playlist-{language}-{theme}-reduced");
        }
        string save = Path.Combine(output, "playlist.seqvium"); Require(await workflow.SaveAsync(save), "Song Save failed.");
        var saved = workflow.State.Snapshot;
        Require(await workflow.ExportSongAsync(ArrangementEditBasis.From(workflow.State), Path.Combine(output, "playlist.wav")), "Song WAV export failed.");
        Require(await workflow.OpenAsync(save, UnsavedDecision.Discard), "Song Open failed.");
        Require(workflow.State.Snapshot.Revision == saved.Revision &&
            workflow.State.Snapshot.State.ArrangementClips.Select(c => c.Id).SequenceEqual(saved.State.ArrangementClips.Select(c => c.Id)) &&
            workflow.State.SongMode && workflow.State.Execution is null, "Reopen lost song or started output.");
        workspace.State.Hide(WorkspaceState.ArrangementId); workspace.Open(WorkspaceState.ArrangementId); main.UpdateLayout();
        Require(ReferenceEquals(pane, main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single()), "Playlist content was recreated.");
        Json(output, "arrangement", new { Passed = true, RoutedInput = "place / captured move / repeat / Escape / capture loss / stale draft / Duplicate / Delete / Undo",
            Themes = "RU/EN Dark/Light ordinary/reduced", SaveOpen = true, ActualGraphWaveExport = true,
            RetainedContent = true, PhysicalAudio = false, NativeInput = false, Alignment = alignment });
        await ArrangementPlaybackControlsAsync(shell, output);
    }

    private static async Task ArrangementPlaybackControlsAsync(ShellSession shell, string output)
    {
        RealtimeSampler? sampler = null;
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), () => new(new MusicalBorrower(),
            sampler = new(44100, 2, 2048), "Simulated physical borrower", () => null), Path.Combine(output, "song-controls-media"));
        var sources = new List<Id<GraphAttachment>>();
        foreach (var name in new[] { "Drums", "Bass" })
        {
            Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, name), "Live fixture creation failed.");
            Require(await workflow.AddBeatWavAsync(BeatSteps.Resolve(workflow.State).Basis!, Path.Combine(output, name + ".wav")), "Live fixture import failed.");
            var pattern = BeatSteps.Resolve(workflow.State).Pattern!;
            Require(await workflow.ToggleBeatStepAsync(BeatSteps.Resolve(workflow.State).Basis!, pattern.Parts[0].Id, 0), "Live note failed.");
            sources.Add(workflow.State.Selected!.Value);
        }
        foreach (var source in sources)
            Require(await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State), source, default, sources.IndexOf(source), 4), "Live placement failed.");
        Require(await workflow.SetSongModeAsync(true), "Live Song mode failed.");
        using var pane = new ArrangementPaneContent(shell, workflow, _ => Task.CompletedTask, () => { });
        var window = new Window { Width = 1000, Height = 460, Content = pane, Title = "Seqvium song playback verification" };
        window.Show();
        try
        {
            var play = workflow.PlayAsync(); var packet = new float[4096];
            for (int i = 0; i < 10000 && !play.IsCompleted; i++)
            { sampler?.Process(Span<float>.Empty); await workflow.ObserveAsync(); await Task.Yield(); }
            Require(await play, "Song control Play failed.");
            sampler!.Process(packet); Require(packet.Max() > 0, "Actual song renderer produced no PCM.");
            long target = 110250; // 2.5 seconds at the actual 44.1 kHz execution format: bar 2, beat 2.
            while (sampler.RenderPosition < target)
                sampler.Process(packet.AsSpan(0, (int)Math.Min(2048, target - sampler.RenderPosition) * 2));
            await workflow.ObserveAsync(); Invoke(pane, "Refresh"); window.UpdateLayout();
            Require(pane.Canvas.PositionTicks == 4800000, "Timeline playhead is not derived from actual 44.1 kHz renderer time.");
            Require(Field<TextBlock>(pane, "_position").Text!.Trim() == "2:2", "Song bar/beat position is wrong.");
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                Invoke(pane, "Refresh"); await CaptureAsync(window, output, "playlist-live-" + theme);
            }
            Require(await workflow.StopAsync(), "Song Stop failed."); sampler.Process(packet);
            Require(!sampler.ReadStatus().Playing && packet.All(sample => sample == 0), "Song Stop was not silent.");
            Json(output, "arrangement-playback", new { Passed = true, SampleRate = 44100, Position = "2:2", TickPosition = pane.Canvas.PositionTicks,
                Playback = "real sampler/coordinator with substituted physical borrower", PhysicalAudio = false });
        }
        finally { window.Close(); }
    }
}
