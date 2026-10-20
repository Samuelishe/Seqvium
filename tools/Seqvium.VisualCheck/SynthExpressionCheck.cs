// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task SynthExpressionAsync(MainWindow main, string output, bool native, bool audio)
    {
        var w = Field<DesktopWorkflow>(main, "_workflow"); var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900;
        Require(await w.CreateBeatAsync(w.State.Document.LifecycleId, w.State.Generation, "Expression"), "Pattern failed.");
        Require(await w.AddDrumVoiceAsync(BeatSteps.Resolve(w.State).Basis!, "Synth", SynthVoice.Algorithm), "Synth failed.");
        var a = w.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(w.State.Document.LifecycleId, a.Id, a.GraphId, a.Sources.Single().NodeId);
        Invoke(main, "OpenNodeEditor", target); workspace.Open(WorkspaceState.NodeEditorId); workspace.Commit();
        SynthPaneContent Pane() => main.GetVisualDescendants().OfType<SynthPaneContent>().Single();
        Control Id(string id) => Pane().GetVisualDescendants().OfType<Control>().Single(c =>
            AutomationProperties.GetAutomationId(c) == "Synth." + id);
        SoundDefinition Sound() => w.State.Snapshot.State.Sounds.Single();
        async Task Refresh()
        {
            await Pane().PendingCommand; await Pane().PendingPreview; await w.ObserveAsync();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single().Refresh(); main.UpdateLayout();
        }
        await Refresh(); workspace.Activate(WorkspaceState.NodeEditorId); workspace.Commit(); await Refresh();
        var filter = (Expander)Id("FilterEnvelopeGroup"); var lfo = (Expander)Id("LfoGroup");
        Require(!filter.IsExpanded && !lfo.IsExpanded && Sound().Parameters.Count == 10, "Initial disclosure/legacy state failed.");
        var matrix = new List<object>(); var fields = new List<object>(); var checks = new List<string>();
        await CaptureAsync(main, output, "expression-neutral-collapsed");
        var presets = (ComboBox)Id("Presets"); presets.SelectedIndex = 5; await Refresh();
        Require(Sound().Parameters["filterAmount"] == 4, "Evolving Pad preset failed.");
        filter.IsExpanded = lfo.IsExpanded = true; await Refresh();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        foreach (bool compact in new[] { false, true })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            main.Width = compact ? 900 : 1920; main.Height = compact ? 700 : 1080; main.UpdateLayout();
            var bounds = Field<Canvas>(workspace, "_canvas").Bounds;
            workspace.State.Reflow(bounds.Width, bounds.Height);
            workspace.State.SetBounds(WorkspaceState.NodeEditorId, compact ? new(0, 0, 360, 520) : new(0, 0, 760, 820));
            workspace.Activate(WorkspaceState.NodeEditorId);
            workspace.Commit(); await Refresh();
            string name = $"expression-{language}-{theme}-{(compact ? "compact" : "ordinary")}";
            var scroll = (ScrollViewer)Pane().Children[1]; scroll.Offset = default; await Refresh();
            await CaptureAsync(main, output, name + "-oscillators");
            foreach (string group in new[] { "FilterEnvelope", "LfoTarget" })
            {
                Id(group).BringIntoView(); await Refresh(); await CaptureAsync(main, output, name + "-" + group);
            }
            foreach (string key in new[] { "filterAttack", "filterDecay", "filterSustain", "filterRelease", "filterAmount", "velocityCutoff", "lfoRate" })
            {
                var field = (TextBox)Id("Value." + key); field.BringIntoView(); await Refresh();
                var row = (Grid)field.Parent!; var unit = row.Children.OfType<TextBlock>().Single();
                fields.Add(FieldAlignment.Verify(field, unit, Pane(), name + ":" + key));
            }
            Require(scroll.Extent.Width <= scroll.Viewport.Width + 1, "Expression overflow.");
            foreach (string id in new[] { "Note", "Velocity", "Hold", "Release", "Stop" })
            {
                var c = Id(id); var p = c.TranslatePoint(default, Pane())!.Value;
                Require(c.IsEffectivelyVisible && p.Y >= 0 && p.Y + c.Bounds.Height <= Pane().Bounds.Height + 1,
                    "Preview footer clipped: " + id);
            }
            matrix.Add(new { name, Window = main.Bounds, Pane = Pane().Bounds, main.RenderScaling,
                scroll.Extent, scroll.Viewport });
        }
        var exact = (TextBox)Id("Value.filterAmount"); exact.BringIntoView(); await Refresh();
        async Task Exact(string key, string text)
        {
            var field = (TextBox)Id("Value." + key); field.BringIntoView(); await Refresh(); field.Focus();
            field.Text = text; field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await Refresh();
        }
        int undo = w.State.UndoCount; await Exact("filterAmount", "-2.5");
        Require(Sound().Parameters["filterAmount"] == -2.5m && w.State.UndoCount == undo + 1, "Exact negative amount failed.");
        await Exact("filterAmount", "-2.5"); Require(w.State.UndoCount == undo + 1, "No-op added history.");
        await Exact("filterAmount", "9"); Require(Sound().Parameters["filterAmount"] == -2.5m, "Invalid amount accepted.");
        exact.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape }); await Refresh();
        checks.Add("exact negative amount, no-op, invalid range, Escape");
        ((ComboBox)Id("LfoTarget")).SelectedIndex = 1; await Refresh();
        await Exact("lfoPitch", "25"); Require(Sound().Parameters["lfoPitch"] == 25, "Pitch depth failed.");
        await CaptureAsync(main, output, "expression-pitch-depth");
        ((ComboBox)Id("LfoTarget")).SelectedIndex = 2; await Refresh();
        await Exact("lfoCutoff", "1.5"); Require(Sound().Parameters["lfoCutoff"] == 1.5m, "Cutoff depth failed.");
        await CaptureAsync(main, output, "expression-cutoff-depth");
        checks.Add("LFO target and exact depths with distinct units");
        var driver = new ControlInputDriver(main, true, output);
        var dial = Id("Dial.velocityCutoff"); dial.BringIntoView(); await Refresh();
        Point[] path = [new(26, 7), new(36, 10), new(44, 20), new(44, 34)];
        var before = Sound().Parameters; undo = w.State.UndoCount;
        await driver.TraceDrag(dial, path); await Refresh(); Require(w.State.UndoCount == undo + 1, "Expression dial history failed.");
        Require(await w.UndoAsync(), "Undo failed."); await Refresh(); Require(Sound().Parameters == before, "Undo changed parameters.");
        await driver.TraceDrag(dial, path, escape: true); await Refresh();
        Require(w.State.UndoCount == undo && Sound().Parameters == before, "Cancelled dial changed sound.");
        checks.Add("dial gesture, one Undo, cancellation");
        var curve = (SynthEnvelopePlot)Id("FilterEnvelope"); curve.BringIntoView(); await Refresh();
        var handle = curve.Handles()[2]; before = Sound().Parameters; undo = w.State.UndoCount;
        await driver.TraceDrag(curve, [handle, handle + new Vector(0, -12)]); await Refresh();
        Require(w.State.UndoCount == undo + 1 && Sound().Parameters["sustain"] == before["sustain"], "Filter curve edited amplitude.");
        Require(await w.UndoAsync(), "Curve Undo failed."); await Refresh();
        checks.Add("independent editable filter curve");
        object physical = new { Result = "not-run" };
        if (audio)
        {
            try
            {
                await driver.Click(Id("Hold")); await Refresh();
                var device = Field<WorkflowOutput>(w, "_noteOutput");
                await Task.Delay(250); await w.ObserveAsync(); long epoch = device.Sampler.TransportEpoch;
                int states = device.Sampler.PreparedStatesCreated;
                ((ComboBox)Id("Velocity")).SelectedIndex = 2;
                ((ComboBox)Id("Note")).SelectedIndex = 61; await Refresh();
                Require(device.Sampler.PreparedStatesCreated == states && device.Sampler.TransportEpoch == epoch,
                    "Pending next-note Velocity retriggered a pitch retune.");
                await driver.Click(Id("Hold")); await Refresh();
                Require(device.Sampler.PreparedStatesCreated == states + 1 && device.Sampler.ActiveVoices == 1,
                    "Explicit changed-velocity Hold did not start exactly one new note.");
                states = device.Sampler.PreparedStatesCreated; epoch = device.Sampler.TransportEpoch;
                int history = w.State.UndoCount;
                await Exact("filterAmount", "2"); await Exact("lfoRate", "6");
                Require(await w.UndoAsync(), "Live Undo failed."); await Refresh();
                Require(device.Sampler.TransportEpoch == epoch && device.Sampler.PreparedStatesCreated == states &&
                    device.ReadFailure() is null && device.Sampler.ActiveVoices == 1, "Continuous F2 preview failed.");
                await driver.Click(Id("Release")); await Refresh(); await Task.Delay(50);
                await Exact("filterRelease", "0"); await driver.Click(Id("Stop")); await Refresh();
                Require(device.Lifetime.Joined, "F2 Stop did not join.");
                physical = new { Result = "passed", device.Description, HistoryBefore = history, Epoch = epoch,
                    SubjectiveListening = false };
            }
            catch (Exception error) { physical = new { Result = "failed", Reason = error.ToString() }; }
        }
        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                exact.BringIntoView(); await Refresh(); main.Activate();
                var input = new NativePointer.DrumInput(main); await input.ActivateVisibleWindow();
                await input.Exact(exact, "3.25"); await Refresh();
                Require(Sound().Parameters["filterAmount"] == 3.25m, "Native exact amount failed.");
                nativeResult = new { Result = "passed", Checks = "guarded Windows exact Filter Amount" };
            }
            catch (Exception error) { nativeResult = new { Result = "refused-or-failed", Reason = error.Message }; }
        }
        string saved = Path.Combine(output, "Expression.seqvium"); before = Sound().Parameters;
        Require(await w.SaveAsync(saved), "F2 Save failed.");
        Require(await w.OpenAsync(saved, UnsavedDecision.Discard), "F2 Open failed.");
        Require(Sound().Parameters.OrderBy(x => x.Key).SequenceEqual(before.OrderBy(x => x.Key)), "F2 portable parameters changed.");
        Json(output, "expression-matrix", matrix); Json(output, "expression-alignment", fields);
        Json(output, "expression-checks", new { checks, Physical = physical, Native = nativeResult });
    }
}
