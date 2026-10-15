// SPDX-License-Identifier: Apache-2.0

using Avalonia.Automation;
using Avalonia.Controls;
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
    private static async Task ExtensionFoundationAsync(MainWindow main, string output, bool native)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        var input = new NativePointer.DrumInput(main);
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        await Task.Delay(250);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 940;
        main.UpdateLayout(); await Task.Delay(120);
        workspace.Open(WorkspaceState.BrowserId);
        var browser = main.GetVisualDescendants().OfType<BrowserPaneContent>().Single();
        var disclosure = (Expander)Id("Extension.Disclosure"); disclosure.IsExpanded = true;
        await Task.Delay(200);
        var extension = main.GetVisualDescendants().OfType<ExtensionBrowserContent>().Single();
        async Task Drain()
        {
            await extension.Pending;
            for (int i = 0; i < 8; i++)
            {
                var pending = browser.PendingCommand; await pending; await Task.Delay(30);
                if (pending == browser.PendingCommand) return;
            }
            throw new InvalidOperationException("Extension Browser actions did not settle.");
        }
        async Task Click(string id)
        {
            if (native) await input.Click(Id(id));
            else ((Button)Id(id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Drain();
        }
        await Drain();
        Require(workflow.Extensions.Status.Single().Availability == ExtensionAvailability.Available, "Bundled sound pack not discovered/activated.");
        if (native) await input.ActivateVisibleWindow();
        await Click("Extension.Sounds");
        var list = (ListBox)Id("Browser.Files");
        Require(list.ItemCount == 3, "Sound pack is absent from the existing Browser.");
        list.SelectedIndex = 0; await Drain();
        Require(Field<BrowserWaveform>(browser, "_wave").Waveform is { Peak: > 0 }, "Pack source has no decoded waveform.");
        Require(!workflow.State.IsDirty && workflow.State.UndoCount == 0, "Pack discovery edited music.");
        await Click("Browser.UseRack");
        workspace.Open(WorkspaceState.BrowserId);
        await Click("Browser.UsePlaylist");
        Require(workflow.State.Snapshot.State.Resources.Length == 2 && workflow.State.UndoCount == 2, "Pack import bypassed managed musical transactions.");
        workspace.Open(WorkspaceState.BrowserId);
        await Click("Extension.Toggle");
        Require(workflow.Extensions.Status.Single().Availability == ExtensionAvailability.Disabled, "Pack did not disable.");
        Require(list.Items.Cast<BrowserSource>().All(s => s.Package is null), "Disabled contribution left active Browser sources.");
        Require(workflow.State.Snapshot.State.Resources.Length == 2, "Disabling erased imported audio.");
        await CaptureAsync(main, output, "extensions-disabled");
        await Click("Extension.Toggle"); await Click("Extension.Sounds");
        Require(list.ItemCount == 3, "Re-enable did not restore contribution.");
        var matrix = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                list.SelectedIndex = 0; await Drain(); main.UpdateLayout();
                Require(Id("Extension.Sounds").Bounds.Width > 30 && Id("Extension.Toggle").Bounds.Height >= 24, "Extension actions lost usable bounds.");
                Require(list.Bounds.Height >= 24, "Extension disclosure displaced the sound list.");
                await CaptureAsync(main, output, $"extensions-{language}-{theme}");
                matrix.Add(new { Language = language.ToString(), Theme = theme.ToString(), ListHeight = list.Bounds.Height });
            }
        }
        main.Width = 640; main.Height = 600; await Task.Delay(120);
        Require(list.Bounds.Height >= 24, "Expanded extension controls displaced the minimum sound list.");
        await CaptureAsync(main, output, "extensions-minimum-expanded");
        disclosure.IsExpanded = false; await Task.Delay(100);
        Require(list.Bounds.Height >= 24, "Minimum Browser lost musical content after closing disclosure.");
        await CaptureAsync(main, output, "extensions-minimum");
        main.Width = 1400; main.Height = 940;
        string save = Path.Combine(output, "Extension Music.seqvium");
        Require(await workflow.SaveAsync(save), "Pack music did not save.");
        Require(await workflow.Extensions.DisableAsync("seqvium.found-sounds"), "Pack disable before reopen failed.");
        Require(await workflow.OpenAsync(save, UnsavedDecision.Discard), "Pack music did not reopen while disabled.");
        foreach (var resource in workflow.State.Snapshot.State.Resources)
        { using var pcm = AudioMedia.Decode(workflow.State.Document, resource.Id); Require(pcm.Frames > 0, "Accepted pack audio is unavailable."); }
        Require(workflow.State.Document.SavedMediaDiagnostics.IsEmpty, "Pack music depends on the disabled package.");
        Json(output, "extension-foundation", new { NativeInput = native, RoutedInput = !native, PhysicalAudio = false,
            ManagedResources = 2, DisableReenable = true, ReopenDisabled = true, Matrix = matrix });
    }
}
