// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task RackPadReviewAsync(MainWindow main, string output)
    {
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        main.WindowState = WindowState.Maximized;
        await Task.Delay(350);
        main.Activate();
        await Task.Delay(150);
        workspace.Open(WorkspaceState.BeatId);
        workspace.State.Hide(WorkspaceState.ArrangementId);
        workspace.State.SetBounds(WorkspaceState.BeatId, new(0, 0, 1000, 550));
        main.UpdateLayout();

        Control Id(string id) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == id);

        var input = new NativePointer.DrumInput(main);
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        await input.Click(Id("Beat.Create"));
        await rack.PendingCommand;
        await input.Click(Id("Beat.AddDrum"));
        await input.Click(((Button)Id("Beat.AddDrum")).ContextMenu!.Items.OfType<MenuItem>().First());
        await rack.PendingCommand;
        await input.Click(Id(WorkspaceState.NodeEditorId + ".hide"));
        await input.Select((ComboBox)Id("Beat.Bars"), 1);
        await rack.PendingCommand;
        var part = BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single();
        await input.Click(Id($"Beat.Step.{part.Id}.0"));
        await rack.PendingCommand;
        var saved = workflow.State.Snapshot;
        var measurements = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (int width in new[] { 460, 1000, 1650 })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            workspace.State.SetBounds(WorkspaceState.BeatId, new(0, 0, width, 550));
            rack.Refresh();
            main.UpdateLayout();
            await Task.Delay(100);
            var pads = rack.GetVisualDescendants().OfType<ToggleButton>().Where(p => p.Classes.Contains("step"))
                .ToArray();
            Require(
                pads.Length == 32 && pads.All(p =>
                    Math.Abs(p.Bounds.Width - p.Bounds.Height) <= 1 &&
                    Math.Abs(p.Bounds.Width - HostMetrics.Values["Rack.StepHeight"]) <= 1),
                "Rack pads stretch at " + width + "/" + language + "/" + theme);
            var scroll = Field<ScrollViewer>(rack, "_rackScroll");
            Require(scroll.Extent.Width > scroll.Viewport.Width == (width < 1200),
                "Rack horizontal extent is incorrect.");
            var first = pads.Single(p => AutomationProperties.GetAutomationId(p) == $"Beat.Step.{part.Id}.0");
            var bar = pads.Single(p => AutomationProperties.GetAutomationId(p) == $"Beat.Step.{part.Id}.16");
            var last = pads.Single(p => AutomationProperties.GetAutomationId(p) == $"Beat.Step.{part.Id}.31");
            measurements.Add(new
            {
                language, theme, PaneWidth = width, Pad = first.Bounds, Extent = scroll.Extent,
                Viewport = scroll.Viewport, BarStart = bar.TranslatePoint(default, first)
            });
            await CaptureAsync(main, output, $"pads-{language}-{theme}-{width}");
            last.BringIntoView();
            await Task.Delay(80);
            main.UpdateLayout();
            var at = last.TranslatePoint(default, scroll)!.Value;
            Require(at.X >= 0 && at.X + last.Bounds.Width <= scroll.Bounds.Width, "Last step is not scroll-reachable.");
            scroll.Offset = default;
        }

        Require(workflow.State.Snapshot == saved, "Geometry/theme changes edited music.");
        workspace.State.SetBounds(WorkspaceState.BeatId, new(0, 0, 1000, 550));
        main.UpdateLayout();
        main.Activate();
        var firstPad = (ToggleButton)Id($"Beat.Step.{part.Id}.1");
        var fourth = (ToggleButton)Id($"Beat.Step.{part.Id}.3");
        var end = fourth.TranslatePoint(new(fourth.Bounds.Width / 2, fourth.Bounds.Height / 2), firstPad)!.Value;
        int beforeUndo = workflow.State.UndoCount;
        await input.Drag(firstPad, new(firstPad.Bounds.Width / 2, firstPad.Bounds.Height / 2), end);
        await rack.PendingCommand;
        Require(
            workflow.State.UndoCount == beforeUndo + 1 &&
            BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single().Notes.Length == 4,
            "Native square-pad paint failed to commit once.");
        input.Key(0x5A, modifier: 0x11);
        await Task.Delay(100);
        Require(workflow.State.Snapshot.Revision == saved.Revision, "Native pad paint Undo changed identity.");
        input.Key(0x59, modifier: 0x11);
        await Task.Delay(100);
        Require(BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single().Notes.Length == 4,
            "Native pad paint Redo failed.");
        await input.Click(Id("Workflow.Play"));
        await Task.Delay(600);
        await workflow.ObserveAsync();
        rack.Refresh();
        Require(rack.GetVisualDescendants().OfType<ToggleButton>().Any(p => p.Classes.Contains("current")),
            "Current step not rendered.");
        await CaptureAsync(main, output, "pads-playing");
        await input.Click(Id("Workflow.Stop"));
        Json(output, "rack-pads",
            new
            {
                SquareAndConstant = true, Matrix = measurements, NativePaintUndoRedo = true, PhysicalPlayhead = true
            });
    }
}
