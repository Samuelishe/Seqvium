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
    private static async Task SampleBrowserAsync(MainWindow main, string output, bool native, bool physical, string? codecSources)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        var input = new NativePointer.DrumInput(main);
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        async Task Wait(Func<bool> ready, string message)
        { for (int i = 0; i < 300 && !ready(); i++) await Task.Delay(20); Require(ready(), message); }
        async Task Click(string id)
        {
            if (native) await input.Click(Id(id));
            else ((Button)Id(id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(80);
        }
        async Task Drain(BrowserPaneContent pane)
        {
            for (int i = 0; i < 8; i++) { var pending = pane.PendingCommand; await pending; await Task.Delay(30); if (pending == pane.PendingCommand) return; }
            throw new InvalidOperationException("Browser actions did not settle.");
        }
        await Task.Delay(350);
        main.WindowState = WindowState.Normal; main.Width = 1920; main.Height = 1040; main.UpdateLayout();
        if (native) await input.ActivateVisibleWindow();
        string samples = Path.Combine(output, "source-fixtures"); Directory.CreateDirectory(samples);
        Directory.CreateDirectory(Path.Combine(samples, "Drums"));
        WriteBrowserWav(Path.Combine(samples, "Kick.wav"), 65, .3);
        WriteBrowserWav(Path.Combine(samples, "Snare.wav"), 170, .18);
        WriteBrowserWav(Path.Combine(samples, "Hat.wav"), 7000, .06);
        WriteBrowserWav(Path.Combine(samples, "Drums", "Deep Kick.wav"), 43, .5);
        File.WriteAllText(Path.Combine(samples, "Broken.wav"), "corrupt source");
        if (native) { input.Key(0x42, modifier: 0x11); await Task.Delay(100); }
        else workspace.Open(WorkspaceState.BrowserId);
        var browser = main.GetVisualDescendants().OfType<BrowserPaneContent>().Single();
        await Drain(browser);
        if (native) await input.Exact(Id("Browser.Path"), samples); else browser.Navigate(samples);
        await Drain(browser);
        var list = (ListBox)Id("Browser.Files");
        Require(list.ItemCount == 5, "Browser did not show explicit folder/audio entries.");
        async Task Select(string name)
        {
            int index = list.Items.Cast<BrowserSource>().ToList().FindIndex(e => e.Name == name);
            Require(index >= 0, "Missing Browser source: " + name);
            list.ScrollIntoView(index); await Task.Delay(60);
            if (native) await input.Click(list.ContainerFromIndex(index)!); else list.SelectedIndex = index;
            await Drain(browser);
        }
        await Select("Broken.wav");
        Require(Field<string>(browser, "_message") == "AudioClip.DecodeFailed" && workflow.State.UndoCount == 0,
            "Corrupt Browser selection was not recoverable without editing music.");
        await Select("Drums");
        if (native) { list.Focus(); input.Key(0x0D); }
        else
        {
            Require(list.Focus(), "Browser list refused keyboard focus.");
            list.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        }
        await Drain(browser);
        Require(list.Items.Cast<BrowserSource>().Single().Name == "Deep Kick.wav", "Browser did not enter its selected subfolder.");
        await Click("Browser.Up"); await Drain(browser);
        var search = (TextBox)Id("Browser.Search");
        if (native) await input.Exact(search, "snare"); else search.Text = "snare";
        await Task.Delay(260); await Drain(browser);
        Require(list.Items.Cast<BrowserSource>().Single().Name == "Snare.wav", "Current-folder name search failed.");
        search.Text = ""; await Task.Delay(260); await Drain(browser);
        await Select("Kick.wav");
        Require(Field<BrowserWaveform>(browser, "_wave").Waveform is { Peak: > 0 }, "Selected WAV has no real waveform.");
        Require(!workflow.State.IsDirty && workflow.State.UndoCount == 0 && workflow.State.Execution is null,
            "Browser navigation/inspection edited music or opened transport.");
        var physicalResults = new List<object>();
        if (physical)
        {
            // Real output and production WavAudition, triggered through the actual button's routed action.
            await Click("Browser.Preview"); await Drain(browser);
            Require(workflow.State.Device is not null && workflow.State.StatusKey != "Browser.PreviewFailed",
                "Physical Browser preview failed: " + workflow.State.Detail);
            physicalResults.Add(new { Source = "WAV", workflow.State.Device, workflow.State.StatusKey });
            await Click("Browser.Stop"); await Drain(browser);
        }
        await Click("Browser.UseRack"); await Drain(browser);
        Require(workflow.State.Snapshot.State.Patterns is [{ Parts.Length: 1 }] && workflow.State.UndoCount == 1,
            "Browser initial Rack use is not atomic.");
        await Select("Snare.wav"); await Click("Browser.UseRack"); await Drain(browser);
        await Select("Hat.wav"); await Click("Browser.UseRack"); await Drain(browser);
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        if (!native)
        {
            var snare = list.Items.Cast<BrowserSource>().Single(s => s.Name == "Snare.wav");
            using var transfer = new DataTransfer(); transfer.Add(DataTransferItem.Create(BrowserPaneContent.DragFormat, snare));
            var over = new DragEventArgs(DragDrop.DragOverEvent, transfer, rack, new Point(80, 80), KeyModifiers.None);
            rack.RaiseEvent(over);
            Require(over.DragEffects == DragDropEffects.Copy, "Browser source drop was not admitted by Rack.");
            rack.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, transfer, rack, new Point(80, 80), KeyModifiers.None));
            await rack.PendingCommand;
            Require(workflow.State.Snapshot.State.Patterns.Single().Parts.Length == 4, "Browser Rack drop did not create a channel.");
            Require(await workflow.UndoAsync(), "Browser Rack drop Undo failed.");
        }
        rack.Refresh();
        var state = workflow.State;
        var pattern = state.Snapshot.State.Patterns.Single();
        // Routed mode verifies canonical musical commands; native mode uses the actual step pad buttons.
        for (int row = 0; row < 3; row++)
        {
            int[] steps = row == 0 ? [0, 8] : row == 1 ? [4, 12] : [2, 6, 10, 14];
            foreach (int step in steps)
            {
                if (native)
                {
                    var pad = rack.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Single(b =>
                        AutomationProperties.GetAutomationId(b) == $"Beat.Step.{pattern.Parts[row].Id}.{step}");
                    await input.Click(pad); await rack.PendingCommand;
                }
                else
                {
                    state = workflow.State;
                    Require(await workflow.ToggleBeatStepAsync(new(state.Document.LifecycleId, state.Generation,
                        state.Selected!.Value, pattern.Id), pattern.Parts[row].Id, step), "Browser composition step failed.");
                }
            }
        }
        // The actual Browser resource source reuses legacy WAV in independent Audio Clips.
        browser.Navigate(null); await Drain(browser);
        await Select("Kick");
        await Click("Browser.UsePlaylist"); await Drain(browser);
        Require(workflow.State.Snapshot.State.Resources.Length == 3 && workflow.State.Snapshot.State.AudioClips.Length == 1,
            "Project-resource use copied media or omitted the clip.");
        var arrangement = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        if (!native)
        {
            var source = list.Items.Cast<BrowserSource>().Single(s => s.Name == "Kick");
            using var transfer = new DataTransfer(); transfer.Add(DataTransferItem.Create(BrowserPaneContent.DragFormat, source));
            var canvas = arrangement.Canvas;
            canvas.RaiseEvent(new DragEventArgs(DragDrop.DropEvent, transfer, canvas, new Point(200, 180), KeyModifiers.None));
            await arrangement.PendingCommand;
            Require(workflow.State.Snapshot.State.AudioClips.Length == 2 && workflow.State.Snapshot.State.Resources.Length == 3,
                "Browser managed-resource drop did not reuse media in Playlist.");
            Require(await workflow.UndoAsync(), "Browser Playlist drop Undo failed.");
        }
        workspace.State.ArrangeMusic(); workspace.State.Open(WorkspaceState.BrowserId);
        // The sidebar zone has already reserved Browser's width outside the musical canvas.
        workspace.State.SetBounds(WorkspaceState.BeatId, new(0, 0, workspace.State.Width, 340));
        workspace.State.SetBounds(WorkspaceState.ArrangementId, new(0, 348, workspace.State.Width, workspace.State.Height - 348));
        await Task.Delay(150);
        var matrices = new List<object>();
        for (int theme = 0; theme < 2; theme++)
        {
            for (int language = 0; language < 2; language++)
            {
                await CaptureAsync(main, output, "browser-" + shell.Preferences.Language + "-" + shell.Preferences.Theme);
                Require(Id("Browser.UseRack").IsEffectivelyVisible && Id("Browser.UsePlaylist").Bounds.Width > 24,
                    "Browser commands became unreachable.");
                matrices.Add(new { shell.Preferences, Browser = browser.Bounds, List = list.Bounds,
                    Rack = workspace.State.Bounds(WorkspaceState.BeatId), Playlist = workspace.State.Bounds(WorkspaceState.ArrangementId) });
                await Click("Preference.Language");
            }
            await Click("Preference.Theme");
        }
        main.Width = 1400; main.Height = 900; await Task.Delay(160);
        await CaptureAsync(main, output, "browser-reduced");
        main.Width = 640; main.Height = 480; await Task.Delay(160);
        Require(list.Bounds.Height >= 24, "Minimum window lost the Browser file list.");
        await CaptureAsync(main, output, "browser-minimum");
        main.Width = 1920; main.Height = 1040; await Task.Delay(160);
        string save = Path.Combine(output, "Browser Session.seqvium");
        if (native)
        {
            await input.Click(Id("Workflow.Save"), allowOwnedDialog: true); await input.FileDialog(save);
            await Wait(() => workflow.State.SavedPath == save && !workflow.State.Busy, "Native Browser Save failed.");
        }
        else Require(await workflow.SaveAsync(save), "Browser project Save failed.");
        foreach (string file in Directory.EnumerateFiles(samples, "*.wav")) File.Delete(file);
        Require(await workflow.OpenAsync(save, UnsavedDecision.Discard), "Browser project reopen failed.");
        Require(workflow.State.Snapshot.State.Patterns.Single().Parts.Sum(p => p.Notes.Length) == 8 &&
            workflow.State.Snapshot.State.AudioClips.Length == 1 && !workflow.State.Document.SavedMediaDiagnostics.Any(),
            "Saved Browser composition lost notes/audio or depends on deleted originals.");
        browser.Navigate(null); await Drain(browser); await Select("Kick");
        if (physical && codecSources is not null)
        {
            foreach (string extension in new[] { "mp3", "flac" })
            {
                var source = new BrowserSource("Tone." + extension, Path.Combine(codecSources, "Tone." + extension));
                var info = await workflow.InspectBrowserSourceAsync(source, CancellationToken.None);
                Require(info.Waveform.Peak > .1 && info.Rate == 48000, extension + " metadata/decode failed.");
                Require(await workflow.PreviewBrowserSourceAsync(source), extension + " physical preview failed: " + workflow.State.Detail);
                await Wait(() => workflow.State.BrowserPreviewPlaying, extension + " did not reach actual output packets.");
                physicalResults.Add(new { Source = extension, workflow.State.Device, info.Rate, info.Channels, info.Frames });
                await workflow.StopBrowserPreviewAsync();
                int resources = workflow.State.Snapshot.State.Resources.Length;
                Require(await workflow.UseBrowserAudioAsync(ArrangementEditBasis.From(workflow.State), source, default, 2),
                    extension + " Playlist import failed.");
                Require(workflow.State.Snapshot.State.Resources.Length == resources + 1, "Codec import did not accept managed audio.");
                Require(await workflow.UndoAsync(), "Codec import Undo failed.");
            }
        }
        await CaptureAsync(main, output, "browser-reopened-without-originals");
        Json(output, "sample-browser", new { NativeInput = native, Matrices = matrices,
            MusicalNotes = 8, Resources = 3, AudioClips = 1, OriginalFilesDeleted = true, SaveReopen = true,
            PhysicalAudio = physical, PhysicalResults = physicalResults, RoutedSetup = !native });
    }

    private static void WriteBrowserWav(string path, double frequency, double seconds)
    {
        const int rate = 48000; int frames = (int)(rate * seconds);
        using var file = File.Create(path); using var writer = new BinaryWriter(file);
        writer.Write("RIFF"u8); writer.Write(36 + frames * 4); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((ushort)3); writer.Write((ushort)1); writer.Write(rate); writer.Write(rate * 4);
        writer.Write((ushort)4); writer.Write((ushort)32); writer.Write("data"u8); writer.Write(frames * 4);
        for (int f = 0; f < frames; f++) writer.Write((float)(.7 * Math.Sin(f * Math.Tau * frequency / rate) * Math.Exp(-8.0 * f / frames)));
    }
}
