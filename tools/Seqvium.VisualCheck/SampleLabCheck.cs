// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Extensions;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task SampleLabAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        await Task.Delay(180);
        main.WindowState = WindowState.Normal; main.Width = 1920; main.Height = 1080;
        main.UpdateLayout(); await Task.Delay(250);
        workspace.Open(WorkspaceState.SampleLabId);
        var pane = main.GetVisualDescendants().OfType<SampleLabPaneContent>().Single();
        await pane.Pending;
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == id);
        var input = new NativePointer.DrumInput(main);
        if (native) await input.ActivateVisibleWindow();
        async Task Click(string id)
        {
            workspace.Open(WorkspaceState.SampleLabId); main.UpdateLayout();
            var button = (Button)Id(id);
            button.BringIntoView(); await Task.Delay(35);
            Require(button.IsEnabled, id + " disabled.");
            if (native) await input.Click(button); else button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await pane.Pending; await Task.Delay(40);
        }
        Require(workflow.Extensions.Status.Single(p => p.Package.Identity == PercussionGenerator.Package).Availability == ExtensionAvailability.Available,
            "Lab package did not activate without opening Browser.");
        await Click("Lab.Random"); var a = workflow.State.Lab!.Selected!.Value;
        await Click("Lab.Random"); var b = workflow.State.Lab!.Selected!.Value;
        var list = (ListBox)Id("Lab.History");
        list.SelectedItem = list.Items.OfType<LabEntry>().Single(e => e.Id == a); await pane.Pending;
        await Click("Lab.UseReference");
        Field<Expander>(pane, "_shape").IsExpanded = true;
        workspace.Open(WorkspaceState.SampleLabId);
        main.UpdateLayout(); await Task.Delay(80);
        Field<Expander>(pane, "_shape").BringIntoView(); await Task.Delay(100);
        await CaptureAsync(main, output, "sample-lab-locks");
        ((CheckBox)Id("Lab.Lock.decay")).IsChecked = true; await pane.Pending;
        decimal decay = workflow.State.Lab!.Locks["decay"];
        await Click("Lab.Similar"); var c = workflow.State.Lab!.Selected!.Value;
        Require(workflow.State.Lab.Entries.Single(e => e.Id == c).Recipe.Values["decay"] == decay &&
            workflow.State.Lab.Entries.Single(e => e.Id == c).Recipe.Reference == a, "Variation ignored the reference or decay lock.");
        list.SelectedItem = list.Items.OfType<LabEntry>().Single(e => e.Id == b); await pane.Pending;
        Require(workflow.State.Lab!.Reference == a && workflow.State.UndoCount == 0 && !workflow.State.IsDirty, "History selection changed reference or music.");
        await Click("Lab.Keep"); await Click("Lab.Rack");
        workspace.Open(WorkspaceState.SampleLabId);
        var target = workflow.State.Selected!.Value;
        var part = workflow.State.Snapshot.State.Patterns.Single().Parts.Single();
        Require(await workflow.SetLabTargetAsync(target, part.Id), "PCM context could not be captured.");
        list.SelectedItem = list.Items.OfType<LabEntry>().Single(e => e.Id == c); await pane.Pending;
        await Click("Lab.Playlist");
        Require(workflow.State.Snapshot.State.Resources.Length == 3 && workflow.State.UndoCount == 3,
            "Independent acceptance did not create one managed transaction per action.");
        Require(await workflow.UndoAsync() && await workflow.UndoAsync(true), "Lab acceptance Undo/Redo failed.");
        workspace.Open(WorkspaceState.SampleLabId);
        var alignment = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                main.Width = 1920; main.Height = 1080;
                workspace.State.SetBounds(WorkspaceState.SampleLabId, new(35, 25, 1060, 850));
                workspace.Open(WorkspaceState.SampleLabId); workspace.Commit(); await Task.Delay(100);
                var value = Id("Lab.Value.decay"); value.BringIntoView(); await Task.Delay(40);
                var label = ((Grid)value.Parent!).Children.OfType<TextBlock>().First();
                alignment.Add(FieldAlignment.Verify(value, label, main, language + "/" + theme + "/Lab decay"));
                Field<ScrollViewer>(pane, "_sound").Offset = default;
                await CaptureAsync(main, output, language + "-" + theme + "-sample-lab");
                main.Width = 1100; main.Height = 760;
                workspace.State.SetBounds(WorkspaceState.SampleLabId, new(10, 10, 470, 560)); workspace.Commit(); await Task.Delay(100);
                await CaptureAsync(main, output, language + "-" + theme + "-sample-lab-reduced");
            }
        }
        main.Width = 1400; main.Height = 900;
        workspace.State.SetBounds(WorkspaceState.SampleLabId, new(30, 20, 1000, 760)); workspace.Commit();
        object physical = new { Result = "not-run" };
        if (audio)
        {
            try
            {
                await Click("Lab.Raw"); await Task.Delay(650);
                await workflow.StopAsync();
                Require(await workflow.PlayAsync(), workflow.State.Detail ?? "Play failed.");
                await Task.Delay(200);
                await Click("Lab.Context"); await Task.Delay(2300);
                Require(workflow.State.Lab!.Contextual, "Context audition not active.");
                await Click("Lab.Original"); await workflow.StopAsync();
                physical = new { Result = "passed", Device = workflow.State.Device, SubjectiveListening = false };
            }
            catch (Exception error)
            { await workflow.StopAsync(true); physical = new { Result = "failed", Reason = error.Message }; }
        }
        async Task ToggleGenerator()
        {
            workspace.Open(WorkspaceState.BrowserId);
            main.UpdateLayout(); await Task.Delay(80);
            ((Expander)Id("Extension.Disclosure")).IsExpanded = true;
            main.UpdateLayout(); await Task.Delay(80);
            var browser = main.GetVisualDescendants().OfType<BrowserPaneContent>().Single();
            await browser.PendingCommand;
            var extensions = main.GetVisualDescendants().OfType<ExtensionBrowserContent>().Single();
            await extensions.Pending;
            var packages = (ComboBox)Id("Extension.Packages");
            packages.SelectedItem = packages.Items.Cast<object>().Single(item =>
                ((ExtensionStatus)item.GetType().GetProperty("Status")!.GetValue(item)!).Package.Identity == PercussionGenerator.Package);
            var toggle = (Button)Id("Extension.Toggle");
            toggle.BringIntoView(); main.UpdateLayout(); await Task.Delay(60);
            if (native) await input.Click(toggle); else toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await extensions.Pending;
            workspace.Open(WorkspaceState.SampleLabId); await Task.Delay(80);
        }
        int retained = workflow.State.Lab!.Entries.Length;
        await ToggleGenerator();
        Require(!Id("Lab.Random").IsEnabled && Id("Lab.Keep").IsEnabled, "Disabled generator left stale Lab actions.");
        await ToggleGenerator();
        Require(Id("Lab.Random").IsEnabled && workflow.State.Lab!.Entries.Length == retained, "Re-enable did not refresh Lab or lost history.");
        await ToggleGenerator();
        await Task.Delay(80);
        await CaptureAsync(main, output, "sample-lab-generator-disabled");
        // Existing candidate audio can still be explicitly accepted with no provider.
        await Click("Lab.Keep");
        string project = Path.Combine(output, "sample-lab.seqvium");
        Require(await workflow.SaveAsync(project), "Save failed.");
        Require(await workflow.SaveAsync(Path.Combine(output, "sample-lab-portable.seqvium")), "Save As failed.");
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Disabled-generator Open failed.");
        var rendered = await PatternWaveExport.ExportAsync(workflow.State.Document, workflow.State.Selected!.Value,
            Path.Combine(output, "sample-lab-pattern.wav"));
        Require(rendered.Frames > 0 && rendered.ClippedSamples == 0, "Accepted PCM export is empty or clipped.");
        Json(output, "sample-lab", new { NativeInput = native, RoutedButtons = !native, Physical = physical,
            Families = new[] { "Kick", "Clap" }, A = a, B = b, C = c, HeldDecay = decay,
            ReferenceStayedA = true, IndependentManagedAcceptances = 4, DisabledGeneratorReopenAndExport = true,
            BrowserDisableReenableUpdatesLab = true,
            FieldAlignment = alignment, AcousticJudgement = false });
    }
}
