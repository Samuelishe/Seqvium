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
    private static async Task ResamplingAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        var navigator = main.GetVisualDescendants().OfType<PatternNavigator>().Single();
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        async Task SettleBrowser()
        {
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(30);
                var pane = main.GetVisualDescendants().OfType<BrowserPaneContent>().Single();
                var work = pane.PendingCommand; await work;
                if (work == pane.PendingCommand) return;
            }
            throw new InvalidOperationException("Browser did not settle.");
        }
        await Task.Delay(250);
        await shell.SetControlsAsync(shell.Controls with { AutoPreview = false });
        main.WindowState = WindowState.Normal;
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width; main.Height = width == 1400 ? 900 : 600; main.UpdateLayout(); await Task.Delay(60);
                    await CaptureAsync(main, output, $"empty-{language}-{theme}-{width}");
                }
            }
        }
        main.Width = 1400; main.Height = 900;
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Resample drums"), "Create failed.");
        var target = workflow.State.Selected!.Value;
        var patternId = workflow.State.Snapshot.State.Patterns.Single().Id;
        BeatEditBasis Basis() => new(workflow.State.Document.LifecycleId, workflow.State.Generation, target, patternId);
        foreach (string family in new[] { "Kick", "Clap", "OpenHat" })
            Require(await workflow.AddDrumVoiceAsync(Basis(), family, DrumInstruments.All.Single(i => i.Key == family).Algorithm), "Drum failed.");
        var pattern = workflow.State.Snapshot.State.Patterns.Single();
        for (int row = 0; row < 3; row++)
        {
            var steps = new bool[16];
            foreach (int step in row == 0 ? new[] { 0, 8 } : row == 1 ? new[] { 4, 12 } : new[] { 2, 6, 10, 15 }) steps[step] = true;
            Require(await workflow.SetBeatStepsAsync(Basis(), pattern.Parts[row].Id, steps), "Steps failed.");
        }
        Require(await workflow.SetGainAsync(.3m), "Local Gain failed.");
        pattern = workflow.State.Snapshot.State.Patterns.Single();
        await Task.Delay(150);
        Button Card() => (Button)Id("Navigator.Pattern." + target);
        MenuItem OpenMenu()
        {
            Card().RaiseEvent(new ContextRequestedEventArgs());
            Require(Card().ContextMenu?.IsOpen == true, "Pattern menu did not open.");
            return Card().ContextMenu!.Items.OfType<MenuItem>().Single(i => AutomationProperties.GetAutomationId(i) == "Resample.Create");
        }
        // Actual popup, retained controls and both language/theme catalogs at ordinary/reduced size.
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width; main.Height = width == 1400 ? 900 : 600; main.UpdateLayout(); await Task.Delay(60);
                    var menu = OpenMenu(); await Task.Delay(70);
                    Require(menu.IsEnabled && menu.Bounds.Width > 50, "Resample action is unavailable.");
                    await CapturePopupAsync(Card().ContextMenu!, output, $"menu-{language}-{theme}-{width}");
                    Card().ContextMenu!.Close();
                }
            }
        }
        main.Width = 1400; main.Height = 900; main.UpdateLayout();
        workspace.Open(WorkspaceState.BrowserId); await SettleBrowser();
        ((TextBox)Id("Browser.Search")).Text = "No matching resource";
        await Task.Delay(300); await SettleBrowser();
        Require(((ListBox)Id("Browser.Files")).ItemCount == 0, "Search fixture did not settle.");
        var before = workflow.State.Snapshot; int undo = workflow.State.UndoCount;
        var expected = await PatternResampling.RenderAsync(workflow.State.Document, target);
        var action = OpenMenu(); action.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Card().ContextMenu!.Close(); await navigator.PendingCommand;
        Require(workflow.State.Resampling is { Resource: not null, Message: "Resample.Ready" }, workflow.State.Resampling?.Detail ?? "Render failed.");
        var resource = workflow.State.Resampling!.Resource!.Value;
        Require(workflow.State.UndoCount == undo + 1 && workflow.State.Snapshot.State.Patterns.Single() == pattern,
            "Render changed source or created extra history.");
        await SettleBrowser();
        var list = (ListBox)Id("Browser.Files");
        Require(list.SelectedItem is BrowserSource { Resource: { } selected } && selected == resource,
            "New sample was not revealed and selected in Browser.");
        await Task.Delay(500); await SettleBrowser();
        Json(output, "reveal-stability", new { Selected = list.SelectedItem,
            Query = ((TextBox)Id("Browser.Search")).Text });
        Require(list.SelectedItem is BrowserSource { Resource: { } stable } && stable == resource,
            "A deferred Browser search cleared the revealed sample.");
        using (var pcm = ProjectMedia.Decode(workflow.State.Document, resource))
        using (var oracle = WavDecoder.Decode(expected.Wave))
        {
            Require(pcm.Frames > 96000 && pcm.Frames == oracle.Frames, "Late one-shot tail was truncated.");
            for (int frame = 0; frame < pcm.Frames; frame++)
                for (int channel = 0; channel < pcm.Channels; channel++)
                    Require(pcm.Sample(frame, channel) == oracle.Sample(frame, channel), "Accepted PCM differs from frozen local output.");
        }
        Require(await workflow.UndoAsync() && workflow.State.Snapshot == before, "Resample Undo failed.");
        Require(await workflow.UndoAsync(true), "Resample Redo failed.");
        await SettleBrowser();
        var source = list.Items.Cast<BrowserSource>().Single(s => s.Resource == resource); list.SelectedItem = source;
        await SettleBrowser();
        object physical = new { Result = "not-run" };
        if (audio)
        {
            try
            {
                Require(await workflow.PlayAsync(), workflow.State.Detail ?? "Source playback failed.");
                await Task.Delay(2400); Require(workflow.State.Execution?.Execution.Playing == true, "Source output stopped unexpectedly.");
                string? sourceDevice = workflow.State.Device; await workflow.StopAsync();
                ((Button)Id("Browser.Preview")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await SettleBrowser();
                string? sampleDevice = workflow.State.Device;
                Require(workflow.State.StatusKey != "Browser.PreviewFailed" && sampleDevice is not null, workflow.State.Detail ?? "Preview failed.");
                await Task.Delay(2800); await workflow.StopBrowserPreviewAsync();
                physical = new { Result = "passed", SourceDevice = sourceDevice, SampleDevice = sampleDevice, SubjectiveListening = false };
            }
            catch (Exception error) { await workflow.StopAsync(true); physical = new { Result = "failed", Reason = error.Message }; }
        }
        Json(output, "before-rack", new { Physical = physical, workflow.State.StatusKey, workflow.State.Detail,
            BrowserSelected = list.SelectedItem,
            BrowserMessage = Field<string>(main.GetVisualDescendants().OfType<BrowserPaneContent>().Single(), "_message"),
            Parts = workflow.State.Snapshot.State.Patterns.Single().Parts.Length });
        ((Button)Id("Browser.UseRack")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await SettleBrowser();
        Require(workflow.State.Snapshot.State.Patterns.Single().Parts.Length == 4 && workflow.State.Snapshot.State.Resources.Length == 1,
            "Browser Rack reuse failed: " + workflow.State.StatusKey + " / " + workflow.State.Detail + " / " +
            Field<string>(main.GetVisualDescendants().OfType<BrowserPaneContent>().Single(), "_message"));
        ((Button)Id("Browser.UsePlaylist")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await SettleBrowser();
        Require(workflow.State.Snapshot.State.AudioClips.Length == 1 && workflow.State.Snapshot.State.Resources.Length == 1,
            "Browser Playlist reuse copied media or omitted the clip.");
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width; main.Height = width == 1400 ? 900 : 600; main.UpdateLayout(); await Task.Delay(70);
                    await CaptureAsync(main, output, $"sample-{language}-{theme}-{width}");
                }
            }
        }
        main.Width = 1400; main.Height = 900; main.UpdateLayout();
        string save = Path.Combine(output, "resampled.seqvium");
        Require(await workflow.SaveAsync(save), "Save failed.");
        Require(await workflow.SaveAsync(Path.Combine(output, "portable.seqvium")), "Save As failed.");
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed.");
        Require(await workflow.OpenAsync(Path.Combine(output, "portable.seqvium"), UnsavedDecision.Discard), "Portable reopen failed.");
        Require(!workflow.State.Document.SavedMediaDiagnostics.Any() && workflow.State.Snapshot.State.Resources.Single().Id == resource,
            "Resample did not survive portable Save/Open.");
        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                var input = new NativePointer.DrumInput(main); await input.ActivateVisibleWindow();
                await Task.Delay(100); var card = Card();
                await input.RightClick(card, new(card.Bounds.Width / 2, card.Bounds.Height / 2));
                var item = card.ContextMenu!.Items.OfType<MenuItem>().Single(i => AutomationProperties.GetAutomationId(i) == "Resample.Create");
                await input.Click(item); await navigator.PendingCommand;
                Require(workflow.State.Snapshot.State.Resources.Length == 2, "Native render did not accept.");
                nativeResult = new { Result = "passed", Checks = "Windows right-click, popup click, managed acceptance" };
            }
            catch (Exception error) { nativeResult = new { Result = "refused-or-failed", Reason = error.Message }; }
        }
        Json(output, "resampling", new { RoutedAction = true, Native = nativeResult, Physical = physical,
            Instruments = 3, expected.Frames, expected.SampleRate, expected.Channels, LocalPcmAgreement = true,
            SourcePreserved = true, UndoRedo = true, BrowserRevealRackPlaylist = true, SaveAsOpen = true,
            SubjectiveListening = false });
    }
}
