// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    // Geometry/docking and text setup are explicit fixtures, separate from Windows input evidence.
    private static async Task ResponsiveDrumLayoutAsync(MainWindow main, DrumVoicePaneContent pane,
        string output, ControlInputDriver input)
    {
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        Control Id(string id) => pane.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == id);
        var plot = (DrumEnvelopePlot)Id("Drum.Envelope");
        var scroll = (ScrollViewer)Id("Drum.Scroll");
        var dial = (GainDialControl)Id("Drum.Dial.tune");
        var exact = (TextBox)Id("Drum.tune");
        var before = workflow.State.Snapshot;
        int history = workflow.State.UndoCount;
        var observations = new List<object>();
        async Task Size(double width, double height)
        {
            workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, width, height));
            main.UpdateLayout(); await Task.Delay(50);
            Require(scroll.Extent.Width <= scroll.Viewport.Width + 1, "Drum requires horizontal overflow.");
            var play = Id("Drum.Audition").TranslatePoint(default, pane)!.Value;
            var stop = Id("Drum.StopAudition").TranslatePoint(default, pane)!.Value;
            var note = Id("Drum.Note").TranslatePoint(default, pane)!.Value;
            var level = Id("Drum.Velocity").TranslatePoint(default, pane)!.Value;
            var footer = (Border)Id("Drum.Audition").Parent!.Parent!.Parent!;
            var footerAt = footer.TranslatePoint(default, pane)!.Value;
            double actionCenter = play.Y + 28, valueCenter = note.Y + 20;
            Require(Math.Abs(stop.Y + 28 - actionCenter) <= 1 && Math.Abs(level.Y + 20 - valueCenter) <= 1,
                "Audition controls do not share their row center.");
            double top = play.Y, bottom = Math.Max(stop.Y + 56, level.Y + 40);
            Require(Math.Abs((top + bottom) / 2 - (footerAt.Y + footer.Bounds.Height / 2)) <= 1,
                "Audition content is not vertically centered in its dock.");
            Require(play.Y >= 0 && play.Y + 56 <= pane.Bounds.Height && stop.X + 56 <= pane.Bounds.Width,
                "Responsive Drum clipped Play/Stop.");
            Require(dial.Width >= 64 && dial.Width <= 76 && exact.Bounds.Height >= 24 && exact.Width == 72,
                "Responsive Drum shrank an input or hit target below its readable size.");
            foreach (string groupName in new[] { "_primary", "_shape" })
            foreach (var row in Field<WrapPanel>(pane, groupName).Children.OfType<Grid>().Where(c => c.IsVisible)
                         .GroupBy(c => Math.Round(c.TranslatePoint(default, pane)!.Value.Y)))
            {
                var centers = row.Select(c => c.Children.OfType<GainDialControl>().Single())
                    .Select(c => c.TranslatePoint(default, pane)!.Value.Y + c.Bounds.Height / 2).ToArray();
                Require(centers.Max() - centers.Min() <= 1, "Wrapped labels displaced neighbouring knobs.");
            }
            observations.Add(new { width, height, Pane = pane.Bounds.Size, Dial = dial.Width, Graph = plot.Height,
                Columns = Field<int>(pane, "_parameterColumns"), scroll.Extent, scroll.Viewport, Play = play, Stop = stop });
        }
        double previousDial = double.MaxValue;
        int previousColumns = 5;
        foreach (double width in new[] { 760d, 700, 640, 580, 520, 420, 360 })
        {
            await Size(width, 760);
            int columns = Field<int>(pane, "_parameterColumns");
            Require(dial.Width <= previousDial && columns <= previousColumns,
                "Width-only shrink enlarged a knob or added columns.");
            previousDial = dial.Width; previousColumns = columns;
        }
        await Size(420, 760); await Size(520, 760);
        double widthOnlyDial = dial.Width;
        foreach (double height in new[] { 620d, 500, 400, 620 })
        {
            await Size(520, height);
            Require(dial.Width <= widthOnlyDial, "Height-only shrink enlarged a parameter knob.");
        }
        await CaptureAsync(main, output, "clap-intermediate-520x620");
        await Size(300, 210);
        await CaptureAsync(main, output, "clap-minimum-300x210");
        await Size(520, 620);

        // Compact presentation never writes rounded data back, and focusing restores exact precision.
        var currentSession = Field<DrumEditorSession>(pane, "_session");
        var precisionSetup = new DrumEditorSession();
        precisionSetup.Observe(workflow.State, currentSession.Target);
        Require(precisionSetup.Begin(), "Precision fixture could not begin.");
        precisionSetup.Set("decay", 1041.667m);
        Require(await workflow.SetDrumParametersAsync(precisionSetup.Complete()!), "Precision fixture commit failed.");
        for (int i = 0; i < 100 && Field<WorkflowState>(currentSession, "_observation").Generation != workflow.State.Generation; i++)
            await Task.Delay(10);
        var duration = (TextBox)Id("Drum.decay");
        duration.BringIntoView(); main.UpdateLayout(); await Task.Delay(60);
        Require(duration.Text == "1041.7", "Unfocused duration is not compact.");
        int precisionHistory = workflow.State.UndoCount;
        await input.Click(duration);
        Require(duration.Text == "1041.667", "Duration focus did not restore exact precision.");
        await input.Click(Id("Drum.StopAudition"));
        Require(duration.Text == "1041.7" && workflow.State.UndoCount == precisionHistory &&
                DrumEditorSession.Resolve(workflow.State.Snapshot.State, currentSession.Target!)!.Parameters["decay"] == 1041.667m,
            $"Compact/focus formatting mismatch: text={duration.Text}; history={precisionHistory}->{workflow.State.UndoCount}; " +
            $"stored={DrumEditorSession.Resolve(workflow.State.Snapshot.State, currentSession.Target!)!.Parameters["decay"]}; " +
            $"focus={duration.IsKeyboardFocusWithin}; draft={currentSession.Draft is not null}");
        input.Key(0x5A, modifier: 0x11);
        for (int i = 0; i < 200 && (workflow.State.Snapshot.Revision != before.Revision ||
             Field<WorkflowState>(currentSession, "_observation").Generation != workflow.State.Generation); i++) await Task.Delay(10);
        Require(workflow.State.Snapshot.Revision == before.Revision, "Precision fixture Undo failed.");

        // A typed draft and focus survive resizing, then Escape restores without music or history.
        scroll.Offset = default; main.UpdateLayout(); exact.BringIntoView(); await Task.Delay(60);
        await input.Click(exact);
        string original = exact.Text!;
        exact.Text = "2";
        Require(Field<DrumEditorSession>(pane, "_session").Draft is not null, "Exact resize fixture did not begin a draft.");
        double fieldHeight = exact.Bounds.Height;
        await Size(600, 680);
        await Size(480, 680);
        Require(exact.IsKeyboardFocusWithin && exact.Text == "2" && exact.Bounds.Height == fieldHeight,
            "Resize changed exact-edit focus/text or field size.");
        await input.Escape(pane);
        Require(exact.Text == original && workflow.State.UndoCount == history && workflow.State.Snapshot.Revision == before.Revision,
            "Resize/Escape changed exact input or history.");

        // Jitter inside the hysteresis band must not alternate column counts.
        await Size(600, 680);
        double offset = workspace.State.Bounds(WorkspaceState.NodeEditorId).Width - pane.Bounds.Width +
            HostMetrics.Values["Space.Medium"] * 2 + HostMetrics.Values["ScrollBar.Size"] + 2;
        foreach (int side in new[] { -1, 1 })
        {
            await Size(offset + 498 + side * 20, 680);
            int columns = Field<int>(pane, "_parameterColumns");
            foreach (int jitter in new[] { -3, 3, -2, 2, -1, 1 })
            {
                await Size(offset + 498 + jitter, 680);
                Require(Field<int>(pane, "_parameterColumns") == columns, "Breakpoint jitter alternated Drum columns.");
            }
        }
        workspace.Open(WorkspaceState.BrowserId);
        workspace.State.Dock(WorkspaceState.NodeEditorId, PaneDock.Right);
        await Size(520, 680);
        await CaptureAsync(main, output, "clap-docked-browser-patterns");
        Require(pane.Bounds.Width < main.Bounds.Width / 2 && dial.Width <= 76,
            "Docked Drum derived its size from the main window.");
        workspace.State.Dock(WorkspaceState.NodeEditorId, PaneDock.Floating);
        await Size(760, 760);
        scroll.Offset = default; main.UpdateLayout(); dial.BringIntoView(); await Task.Delay(60);
        var center = new Point(dial.Bounds.Width / 2, dial.Bounds.Height / 2);
        double radius = dial.Bounds.Width * .36;
        var path = Enumerable.Range(0, 25).Select(i => center + new Vector(Math.Cos(-Math.PI + i * Math.PI / 24),
            Math.Sin(-Math.PI + i * Math.PI / 24)) * radius).ToArray();
        var resizeGesture = await input.TraceDrag(dial, path, i =>
        {
            if (i == 12) { workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 600, 680)); main.UpdateLayout(); }
            Require(dial.HasGesture && workflow.State.Snapshot.Revision == before.Revision,
                "Resize interrupted the captured gesture or committed early.");
        }, fine: true);
        await pane.PendingCommand;
        Require(workflow.State.UndoCount == history + 1, "Resized gesture did not commit exactly once.");
        input.Key(0x5A, modifier: 0x11);
        for (int i = 0; i < 200 && workflow.State.Snapshot.Revision != before.Revision; i++) await Task.Delay(10);
        Require(workflow.State.Snapshot.Revision == before.Revision, "Undo failed after a resized gesture.");
        await Size(760, 760);
        Require(workflow.State.Snapshot.Revision == before.Revision && workflow.State.UndoCount == history,
            "Responsive layout changed musical history.");
        Json(output, "responsive-drum", new { Setup = "fixture geometry/docking/text; actual control focus and Escape", Observations = observations,
            ExactFocusAndEscape = true, StableBreakpointBands = true, LocalViewport = true,
            CompactDuration = "1041.7", FocusedDuration = "1041.667", ExactStoredDurationPreserved = true,
            ResizeCapturedGesture = resizeGesture, ResizeGestureInput = "uses selected native/routed driver", SingleCommitAndUndo = true });
    }
}
