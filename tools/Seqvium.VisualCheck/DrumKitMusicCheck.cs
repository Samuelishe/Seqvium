// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task DrumKitMusicAsync(MainWindow main, string output, string deliveryRoot)
    {
        Console.WriteLine("Native drum composition with physical audio and native file dialogs.");
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        if (shell.Preferences.Language != HostLanguage.English) await shell.ToggleLanguageAsync();
        main.WindowState = WindowState.Maximized;
        await Task.Delay(350);
        main.Activate();
        await Task.Delay(200);
        var input = new NativePointer.DrumInput(main);
        workspace.Open(WorkspaceState.BeatId);
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();

        Control Id(string id) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == id);

        Pattern Pattern() => BeatSteps.Resolve(workflow.State).Pattern!;

        async Task Rack()
        {
            workspace.State.Hide(WorkspaceState.NodeEditorId);
            workspace.State.Hide(WorkspaceState.PianoId);
            workspace.Open(WorkspaceState.BeatId);
            workspace.State.SetBounds(WorkspaceState.BeatId, new(0, 0, workspace.State.Width, workspace.State.Height));
            rack.Refresh();
            main.UpdateLayout();
            await Task.Delay(50);
        }

        async Task Add(string key, int preset = 0)
        {
            await Rack();
            await input.Click(Id("Beat.AddDrum"));
            for (int attempt = 0; attempt < 50 && ((Button)Id("Beat.AddDrum")).ContextMenu is null; attempt++)
                await Task.Delay(30);
            var menu = ((Button)Id("Beat.AddDrum")).ContextMenu!;
            if (menu is null)
                throw new InvalidOperationException("Native drum chooser did not open: " + workflow.State.StatusKey);
            await input.Click(menu.Items.OfType<MenuItem>()
                .Single(i => AutomationProperties.GetAutomationId(i) == "Beat.AddDrum." + key));
            await rack.PendingCommand;
            main.UpdateLayout();
            var editor = main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single();
            editor.Refresh();
            var drum = main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single();
            Require(drum.IsEffectivelyVisible, "Added drum editor is hidden: " + key);
            workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(40, 10, 760, 760));
            workspace.Open(WorkspaceState.NodeEditorId);
            main.UpdateLayout();
            await Task.Delay(100);
            if (preset != 0)
            {
                await input.Select((ComboBox)Id("Drum.Preset"), preset);
                await drum.PendingCommand;
                Require(((ComboBox)Id("Drum.Preset")).SelectedIndex == preset, "Native preset did not select: " + key);
            }

            await drum.PendingWaveform;
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                editor.Refresh();
                main.UpdateLayout();
                await drum.PendingWaveform;
                foreach (var field in drum.GetVisualDescendants().OfType<TextBox>().Where(c => c.IsEffectivelyVisible))
                {
                    var unit = ((Grid)field.Parent!).Children.OfType<TextBlock>().Single();
                    FieldAlignment.Verify(field, unit, main, key + "/" + theme);
                }

                await CaptureAsync(main, output, "kit-" + key + "-" + theme);
            }

            await input.Click(Id("Drum.Audition"));
            await drum.PendingCommand;
            await Task.Delay(500);
            await workflow.StopAsync();
            editor.Refresh();
            Json(output, "kit-progress", new { Instrument = key, Pattern = Pattern().Name });
        }

        async Task New(string name)
        {
            await Rack();
            await CaptureAsync(main, output, "kit-before-create");
            await input.Click(Id("Beat.Create"));
            await rack.PendingCommand;
            rack.Refresh();
            main.UpdateLayout();
            await input.Exact(Id("Beat.Name"), name);
            await rack.PendingCommand;
            Require(Pattern().Name == name, "Native Pattern rename failed.");
        }

        async Task Steps(string key, params int[] steps)
        {
            await Rack();
            var part = Pattern().Parts.Single(p =>
                workflow.State.Snapshot.State.Sounds.Single(s => s.Id == p.SoundId).Algorithm ==
                DrumInstruments.All.Single(i => i.Key == key).Algorithm);
            foreach (int step in steps)
            {
                await input.Click(Id($"Beat.Step.{part.Id}.{step}"));
                await rack.PendingCommand;
                rack.Refresh();
                main.UpdateLayout();
            }
        }

        await New("Jade Circuit · A");
        foreach (string key in new[] { "Kick", "Snare", "ClosedHat", "OpenHat", "Clap", "Tom", "Rim" })
            await Add(key, key is "Kick" or "Snare" ? 2 : 0);
        await Steps("Kick", 0, 4, 8, 12, 14);
        await Steps("Snare", 4, 12);
        await Steps("ClosedHat", 0, 2, 4, 6, 8, 10, 12, 14);
        await Steps("OpenHat", 7, 15);
        await Steps("Clap", 12);
        await Steps("Rim", 3, 11);
        var sourceA = workflow.State.Selected!.Value;
        await CaptureAsync(main, output, "kit-rack-A");
        await New("Jade Circuit · B");
        foreach (string key in new[] { "Kick", "Snare", "ClosedHat", "OpenHat", "Clap", "Rim" })
            await Add(key, key == "Snare" ? 1 : 0);
        await Steps("Kick", 0, 6, 8, 10, 14);
        await Steps("Snare", 4, 12, 15);
        await Steps("ClosedHat", 0, 2, 4, 6, 8, 10, 12, 13, 14);
        await Steps("OpenHat", 3, 11);
        await Steps("Clap", 4, 12);
        await Steps("Rim", 1, 7, 9, 15);
        var sourceB = workflow.State.Selected!.Value;
        await New("Jade Circuit · Space");
        foreach (string key in new[] { "Kick", "ClosedHat", "Rim" }) await Add(key, 1);
        await Steps("Kick", 0, 8);
        await Steps("ClosedHat", 2, 6, 10, 14);
        await Steps("Rim", 3, 11, 15);
        var sourceSpace = workflow.State.Selected!.Value;
        await New("Jade Circuit · Turn");
        await Add("Snare", 1);
        await Add("Tom", 1);
        await Steps("Snare", 4, 12, 14, 15);
        // Native Piano Roll draws a descending pitched tom fill and changes its velocity lane.
        await Rack();
        var tom = Pattern().Parts.Last();
        await input.Click(Id("Beat.Actions." + tom.Id));
        var actions = ((Button)Id("Beat.Actions." + tom.Id)).ContextMenu!;
        await input.Click(actions.Items.OfType<MenuItem>()
            .Single(i => AutomationProperties.GetAutomationId(i) == "Beat.Piano." + tom.Id));
        workspace.State.SetBounds(WorkspaceState.PianoId, new(0, 0, workspace.State.Width, workspace.State.Height));
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        piano.Refresh();
        main.UpdateLayout();
        var canvas = piano.Canvas;
        canvas.Fit();
        foreach (var hit in new[] { (8, 43), (10, 41), (12, 38), (13, 36), (14, 34), (15, 31) })
        {
            if (canvas.Y(hit.Item2) + canvas.RowHeight > canvas.NoteBottom)
                canvas.SetView(canvas.OffsetX,
                    canvas.OffsetY + canvas.Y(hit.Item2) + canvas.RowHeight - canvas.NoteBottom);
            long ticks = hit.Item1 * MusicalPosition.TicksPerQuarter / 4;
            await input.Drag(canvas, new(canvas.X(ticks) + 2, canvas.Y(hit.Item2) + canvas.RowHeight / 2),
                new(canvas.X(ticks + MusicalPosition.TicksPerQuarter / 4), canvas.Y(hit.Item2) + canvas.RowHeight / 2));
            await piano.PendingCommand;
            piano.Refresh();
        }

        Require(canvas.Part!.Notes.Length == 6, "Tom fill lost Piano notes.");
        var tomNote = canvas.Part.Notes[0];
        await input.Drag(canvas,
            new(canvas.X(tomNote.Position.Ticks) + 5, canvas.Bounds.Height - 8 - .8 * (canvas.VelocityHeight - 28)),
            new(canvas.X(tomNote.Position.Ticks) + 5, canvas.Bounds.Height - 8 - .55 * (canvas.VelocityHeight - 28)));
        await piano.PendingCommand;
        await CaptureAsync(main, output, "kit-piano-turn");
        var sourceTurn = workflow.State.Selected!.Value;
        await workflow.StopAsync();
        await Rack();
        workspace.State.Hide(WorkspaceState.BeatId);
        workspace.Open(WorkspaceState.ArrangementId);
        workspace.State.SetBounds(WorkspaceState.ArrangementId,
            new(0, 0, workspace.State.Width, workspace.State.Height));
        var playlist = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        playlist.Refresh();
        main.UpdateLayout();
        var source = Field<ComboBox>(playlist, "_source");
        var arrangement = playlist.Canvas;
        foreach (var section in new[]
                 {
                     (sourceA, 0, 4, 0), (sourceB, 4, 4, 1), (sourceSpace, 8, 2, 2), (sourceTurn, 10, 1, 3),
                     (sourceB, 11, 4, 1), (sourceTurn, 15, 1, 3)
                 })
        {
            int index = workflow.State.Occurrences.Select(o => o.Id).ToList().IndexOf(section.Item1);
            await input.Select(source, index);
            await playlist.PendingCommand;
            playlist.Refresh();
            for (int bar = section.Item2; bar < section.Item2 + section.Item3; bar++)
            {
                var scroll = Field<ScrollViewer>(playlist, "_scroll");
                double x = arrangement.HeaderSize + (bar + .08) *
                    workflow.State.Snapshot.State.Settings.Meter.TicksPerBar * arrangement.TickPixels;
                if (x > scroll.Offset.X + scroll.Viewport.Width - 20)
                    scroll.Offset = new(Math.Max(0, x - scroll.Viewport.Width + 80), 0);
                await input.Click(arrangement, new(x, HostVisual.Metric(arrangement, "Playlist.RulerHeight") +
                                                      (section.Item4 + .5) * HostVisual.Metric(arrangement,
                                                          "Playlist.LaneHeight")));
                await playlist.PendingCommand;
                playlist.Refresh();
                main.UpdateLayout();
            }
        }

        Require(workflow.State.Snapshot.State.ArrangementClips.Length == 16, "Playlist native placement count failed.");
        await input.Click(Id("Arrangement.Fit"));
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            await CaptureAsync(main, output, "kit-song-" + theme);
        }

        // Native Save/Open and export use the production pickers; the delivery directory is unique and outside Git.
        string delivered = Path.Combine(Path.GetFullPath(deliveryRoot),
            "Jade Circuit-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(delivered);
        string project = Path.Combine(delivered, "Jade Circuit.seqvium"),
            wave = Path.Combine(delivered, "Jade Circuit.wav");
        await input.Click(Id("Workflow.Save"));
        await input.FileDialog(project);
        for (int i = 0; i < 100 && workflow.State.SavedPath != project; i++) await Task.Delay(30);
        Require(workflow.State.SavedPath == project && !workflow.State.IsDirty,
            "Native Save failed: " + workflow.State.Detail);
        var saved = workflow.State.Snapshot;
        var lifecycle = workflow.State.Document.LifecycleId;
        await input.Click(Id("Workflow.Open"));
        await input.FileDialog(project);
        for (int i = 0; i < 100 && workflow.State.Document.LifecycleId == lifecycle; i++)
            await Task.Delay(30);
        await Task.Delay(150);
        Require(
            workflow.State.Snapshot.Revision == saved.Revision && workflow.State.Document.LifecycleId != lifecycle &&
            workflow.State.Execution is null, "Reopen changed music or played.");
        await input.Click(Id("Arrangement.Play"));
        await playlist.PendingCommand;
        int voices = 0;
        for (int i = 0; i < 1000 && workflow.State.Execution?.Execution.Transport != SamplerTransportState.Ended; i++)
        {
            await Task.Delay(40);
            await workflow.ObserveAsync();
            voices = Math.Max(voices, workflow.State.Execution?.Execution.Voices ?? 0);
            Require(workflow.State.Execution?.Blocker is null && workflow.State.StatusKey != "Workflow.DeviceFault",
                "Physical Song blocked: " + workflow.State.Detail);
        }

        Require(workflow.State.Execution?.Execution.Transport == SamplerTransportState.Ended && voices >= 3,
            "Song did not finish layered drums.");
        await input.Click(Id("Arrangement.Stop"));
        await playlist.PendingCommand;
        await input.Click(Id("Arrangement.Export"));
        await input.FileDialog(wave);
        await playlist.PendingCommand;
        Require(File.Exists(wave), "Native Song export failed.");
        using var pcm = WavDecoder.Decode(File.ReadAllBytes(wave));
        double peak = 0, energy = 0;
        int saturated = 0;
        for (int frame = 0; frame < pcm.Frames; frame++)
        {
            double v = pcm.Sample(frame, 0);
            peak = Math.Max(peak, Math.Abs(v));
            energy += v * v;
            if (Math.Abs(v) >= .9999) saturated++;
        }

        Require(peak > .05 && energy > 1 && saturated == 0 && pcm.Frames >= 1536000,
            "Song WAV is clipped, silent or truncated.");
        Json(output, "drum-kit-music", new
        {
            NativeAuthoring = true, NativeFileDialogs = true, PhysicalOutput = true,
            SubjectiveListening = false, Patterns = workflow.State.Snapshot.State.Patterns.Length,
            Clips = 16, Instruments = DrumInstruments.All.Select(i => i.Key), Voices = voices,
            pcm.Frames, Peak = peak, Energy = energy, Saturated = saturated, Delivered = delivered
        });
        Console.WriteLine("Delivered: " + delivered);
    }
}
