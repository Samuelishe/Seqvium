// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task ControlInputAsync(MainWindow main, string output, bool reproduce, bool routed, bool gainMixer, bool velocityOnly)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        main.WindowState = WindowState.Maximized;
        await Task.Delay(250);
        var input = new ControlInputDriver(main, routed, output);
        if (!routed) { main.Activate(); await Task.Delay(80); }
        await input.ActivateVisibleWindow();
        var checks = new List<string>();
        var traces = new List<object>();
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == id);
        workspace.Open(WorkspaceState.BeatId);
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        workspace.State.SetBounds(WorkspaceState.BeatId, new(10, 10, 880, 420));
        main.UpdateLayout();
        await Task.Delay(180);
        await input.Click(Id("Beat.Create"));
        await rack.PendingCommand;
        rack.Refresh();
        workspace.Open(WorkspaceState.BeatId);
        workspace.State.SetBounds(WorkspaceState.BeatId, new(10, 10, 880, 420));
        main.UpdateLayout();
        await Task.Delay(120);
        await input.Click(Id("Beat.AddDrum"));
        var menu = ((Button)Id("Beat.AddDrum")).ContextMenu!;
        await input.Click(menu.Items.OfType<MenuItem>()
            .Single(i => AutomationProperties.GetAutomationId(i) == "Beat.AddDrum.Clap"));
        await rack.PendingCommand;
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(20, 5, 760, 760));
        workspace.Open(WorkspaceState.NodeEditorId);
        main.UpdateLayout();
        await Task.Delay(100);
        var pane = main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single();
        Require(pane.GetVisualDescendants().OfType<DrumEnvelopePlot>().Single().Algorithm ==
            DrumInstruments.All.Single(i => i.Key == "Clap").Algorithm, "Native menu did not create Clap.");
        await pane.PendingWaveform;
        await CaptureAsync(main, output, "clap-before-amplitude");
        var plot = (DrumEnvelopePlot)Id("Drum.Envelope");
        var handles = (Point[])typeof(DrumEnvelopePlot).GetMethod("Handles",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plot,
            [Math.Max(150, (double)plot.Values["decay"] * 1.12)])!;
        Json(output, "before-amplitude", new { reproduce, plot.Algorithm, plot.AmplitudeMode,
            Keys = plot.Values.Keys, ProcessId = Environment.ProcessId });
        // Allows attaching a debugger to this isolated process without touching user run configurations.
        if (reproduce) await Task.Delay(15000);
        await input.Drag(plot, handles[0], handles[0] + new Vector(12, 8));
        await pane.PendingCommand;
        var auditionOutput = Field<WorkflowOutput>(workflow, "_noteOutput");
        Require(auditionOutput is not null, "Changed amplitude commit did not start audition.");
        await Undo();
        checks.Add("native new Pattern / Clap / amplitude attack / Undo");
        SaveProgress();
        await RapidAudition();
        if (Environment.GetCommandLineArgs().Contains("audition-only"))
        {
            await ReviewDrum(); SaveProgress("passed"); return;
        }

        var onlyInstrument = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("instrument=", StringComparison.Ordinal))?[11..];
        foreach (var instrument in (gainMixer ? Enumerable.Empty<DrumInstruments.Instrument>() : DrumInstruments.All)
                     .Where(i => onlyInstrument is null || i.Key == onlyInstrument)
                     .OrderBy(i => i.Key == "Clap" ? 0 : 1))
        {
            if (instrument.Key != "Clap")
            {
                workspace.State.Hide(WorkspaceState.NodeEditorId);
                workspace.Open(WorkspaceState.BeatId);
                rack.Refresh();
                main.UpdateLayout();
                await input.Click(Id("Beat.AddDrum"));
                menu = ((Button)Id("Beat.AddDrum")).ContextMenu!;
                await input.Click(menu.Items.OfType<MenuItem>().Single(i =>
                    AutomationProperties.GetAutomationId(i) == "Beat.AddDrum." + instrument.Key));
                await rack.PendingCommand;
                workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(20, 5, 760, 760));
                workspace.Open(WorkspaceState.NodeEditorId);
                main.UpdateLayout();
                await Task.Delay(100);
            }
            pane = main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single();
            plot = (DrumEnvelopePlot)Id("Drum.Envelope");
            Require(plot.Algorithm == instrument.Algorithm, "Native menu created the wrong instrument: " + instrument.Key);
            var scroll = (ScrollViewer)Id("Drum.Scroll");
            foreach (bool amplitude in velocityOnly ? [] : instrument.HasPitchEnvelope ? new[] { false, true } : new[] { true })
            {
                scroll.Offset = default;
                main.UpdateLayout();
                if (instrument.HasPitchEnvelope)
                    await input.Click(Id(amplitude ? "Drum.Amplitude" : "Drum.Pitch"));
                plot.BringIntoView();
                main.UpdateLayout();
                await Task.Delay(40);
                for (int handleIndex = 0; handleIndex < 3; handleIndex++)
                {
                    var before = workflow.State.Snapshot;
                    int count = workflow.State.UndoCount;
                    handles = (Point[])typeof(DrumEnvelopePlot).GetMethod("Handles",
                        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plot,
                        [Math.Max(150, (double)plot.Values["decay"] * 1.12)])!;
                    var from = handles[handleIndex];
                    var path = Enumerable.Range(0, 13).Select(i => from + new Vector(
                        handleIndex == 1 ? Math.Sin(i * .4) * 8 : i * 1.5,
                        handleIndex == 2 ? Math.Sin(i * .4) * 5 : i)).ToArray();
                    traces.Add(new { Instrument = instrument.Key, amplitude, handleIndex,
                        Samples = await input.TraceDrag(plot, path) });
                    await pane.PendingCommand;
                    Require(workflow.State.UndoCount == count + 1,
                        $"{instrument.Key}/{amplitude}/{handleIndex}: envelope did not commit once; " +
                        $"history={count}->{workflow.State.UndoCount}; enabled={plot.IsEnabled}; draft={plot.IsDraft}; " +
                        $"from={from}; bounds={plot.Bounds}; status={workflow.State.StatusKey}; detail={workflow.State.Detail}; " +
                        $"before={string.Join(',', before.State.Sounds.Single(s => s.Algorithm == instrument.Algorithm).Parameters)}; " +
                        $"after={string.Join(',', plot.Values)}");
                    await Undo(before);
                    var cancelTrace = await input.TraceDrag(plot, path, escape: true);
                    Require(workflow.State.Snapshot.Revision == before.Revision && workflow.State.UndoCount == count,
                        "Envelope Escape committed a gesture.");
                    traces.Add(new { Cancel = instrument.Key, amplitude, handleIndex, Samples = cancelTrace });
                }
                checks.Add(instrument.Key + (amplitude ? " amplitude" : " pitch") + ": all handles / curves / Escape / native Undo");
                SaveProgress();
            }

            foreach (var parameter in instrument.Parameters.Where(p => !velocityOnly && p.Key != "choke"))
            {
                bool shape = parameter.Key is "attack" or "amplitudeCurve" or "pitchAmount" or "pitchCurve" or "pitchDecay";
                bool amplitude = parameter.Key is "attack" or "amplitudeCurve";
                if (shape && instrument.HasPitchEnvelope && plot.AmplitudeMode != amplitude)
                {
                    scroll.Offset = default;
                    main.UpdateLayout();
                    await input.Click(Id(amplitude ? "Drum.Amplitude" : "Drum.Pitch"));
                }
                var dial = (GainDialControl)Id("Drum.Dial." + parameter.Key);
                dial.BringIntoView();
                main.UpdateLayout();
                await Task.Delay(40);
                await CheckDial(dial, () => pane.PendingCommand, instrument.Key + "/" + parameter.Key);
            }
            scroll.Offset = default;
            main.UpdateLayout();
            var velocity = (GainLevelControl)Id("Drum.Velocity");
            double velocityInset = HostVisual.Metric(velocity, velocity.SizeRole + ".Inset");
            var thumbCenter = new Point(velocityInset + (velocity.Bounds.Width - 2 * velocityInset) *
                (double)velocity.Amplitude!.Value, velocity.Bounds.Height / 2);
            decimal last = velocity.Amplitude!.Value;
            int velocityDirection = last > .5m ? -1 : 1;
            var velocityValues = new List<decimal>();
            var velocityTrace = await input.TraceDrag(velocity,
                [thumbCenter, thumbCenter + new Vector(0, 70), thumbCenter + new Vector(velocityDirection * 8, 80),
                    thumbCenter + new Vector(velocityDirection * 16, -70), thumbCenter + new Vector(velocityDirection * 24, -80),
                    thumbCenter + new Vector(velocityDirection * 30, 0)],
                i => { velocityValues.Add(velocity.Amplitude!.Value);
                    Json(output, "velocity-current", new { instrument.Key, i, Previous = last, Current = velocity.Amplitude,
                        velocity.HasGesture, Direction = velocityDirection, Values = velocityValues });
                    // Press maps the real integer screen pixel to X before vertical motion starts.
                    if (i == 0) last = velocity.Amplitude!.Value;
                    Require(velocityDirection * (velocity.Amplitude!.Value - last) >= -.001m && velocity.HasGesture,
                    $"{instrument.Key}: horizontal velocity reversed, jumped or lost capture. Sample={i}; before={last}; value={velocity.Amplitude}; capture={velocity.HasGesture}"); last = velocity.Amplitude!.Value; });
            Require(velocityDirection * (velocityValues[^1] - velocityValues[0]) > .1m, "Horizontal velocity did not respond.");
            traces.Add(new { Velocity = instrument.Key, Values = velocityValues, Samples = velocityTrace });
            decimal velocityBeforeCancel = velocity.Amplitude!.Value;
            int historyBeforeVelocityCancel = workflow.State.UndoCount;
            await input.TraceDrag(velocity, [thumbCenter, thumbCenter + new Vector(5, 70)], escape: true);
            Require(velocity.Amplitude == velocityBeforeCancel && workflow.State.UndoCount == historyBeforeVelocityCancel,
                "Local velocity cancellation changed its value or project history.");
            checks.Add(instrument.Key + ": horizontal velocity follows X above/below track");
            SaveProgress();
            if (instrument.Key == "Clap" && !velocityOnly) await ReviewDrum();
        }

        if (velocityOnly) { SaveProgress("passed"); return; }

        // The shared control's other two production owners: graph Gain and compact Mixer balance.
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single(a => a.Id == workflow.State.Selected);
        var graph = workflow.State.Snapshot.State.Graphs.Single(g => g.Id == attachment.GraphId);
        var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        typeof(MainWindow).GetMethod("OpenNodeEditor", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(main,
            [new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, graph.Id, gain.Id)]);
        var nodePane = main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single();
        nodePane.Refresh(); main.UpdateLayout();
        var gainDial = (GainDialControl)Id("NodeEditor.Dial");
        await CheckDial(gainDial, () => nodePane.PendingCommand, "Gain");
        await CheckDialBoundaries(gainDial, nodePane);
        var level = (GainLevelControl)Id("NodeEditor.Level");
        var levelBefore = workflow.State.Snapshot;
        int levelCount = workflow.State.UndoCount;
        double inset = HostVisual.Metric(level, "Level.Inset");
        var levelPath = Enumerable.Range(0, 61).Select(i => new Point(
            inset + (level.Bounds.Width - 2 * inset) * (.15 + i * .01),
            level.Bounds.Height / 2 + Math.Sin(i * .6) * 90)).ToArray();
        traces.Add(new { Level = "Gain", Samples = await input.TraceDrag(level, levelPath, i =>
        {
            decimal expected = decimal.Round((decimal)(.15 + i * .01), 4);
            Require(Math.Abs(level.Amplitude!.Value - expected) < .004m && level.HasGesture,
                "Gain level stalled or lost capture outside its track.");
            Require(workflow.State.Snapshot.Revision == levelBefore.Revision, "Level preview changed music.");
        }, intervalMs: 8) });
        await nodePane.PendingCommand;
        Require(workflow.State.UndoCount == levelCount + 1, "Level did not commit once.");
        await Undo(levelBefore);
        await input.TraceDrag(level, levelPath, escape: true, intervalMs: 8);
        Require(workflow.State.Snapshot.Revision == levelBefore.Revision && workflow.State.UndoCount == levelCount,
            "Level Escape changed history.");
        checks.Add("Gain level: 120 curved off-track samples / capture / immediate values / Escape / native Undo");
        SaveProgress();

        workspace.State.Hide(WorkspaceState.NodeEditorId);
        workspace.Open(WorkspaceState.MixerId);
        workspace.State.SetBounds(WorkspaceState.MixerId, new(0, 0, 1050, 660));
        main.UpdateLayout();
        await Task.Delay(100);
        var mixer = main.GetVisualDescendants().OfType<MixerPaneContent>().Single();
        if (workflow.State.Snapshot.State.Mixer is null)
        {
            await input.Click(Id("Mixer.Initialize"));
            await mixer.PendingCommand;
            main.UpdateLayout();
            await Task.Delay(100);
        }
        var mixerDials = mixer.GetVisualDescendants().OfType<GainDialControl>().ToArray();
        Require(mixerDials.Length > 1, "Mixer fixture needs actual channel and Master dials.");
        foreach (var dial in mixerDials)
            await CheckDial(dial, () => mixer.PendingCommand, AutomationProperties.GetAutomationId(dial)!);
        checks.Add("all shared dial owners verified with native Windows input");
        SaveProgress("passed");

        async Task Undo(ProjectSnapshot? expected = null)
        {
            long generation = workflow.State.Generation;
            input.Key(0x5A, modifier: 0x11);
            for (int i = 0; i < 100 && workflow.State.Generation == generation; i++) await Task.Delay(10);
            Require(workflow.State.Generation != generation &&
                (expected is null || workflow.State.Snapshot.Revision == expected.Revision), "Native Ctrl+Z failed.");
            // Canonical completion precedes the asynchronously posted UI observation. Do not send
            // a synthetic press against the intentionally stale editor between those two events.
            var session = Field<DrumEditorSession>(pane, "_session");
            var nodeSession = Field<NodeEditorSession>(main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single(), "_editor");
            for (int i = 0; i < 100 &&
                 Field<WorkflowState>(nodeSession, "_observation").Generation != workflow.State.Generation; i++) await Task.Delay(10);
            for (int i = 0; i < 100 && pane.IsEffectivelyVisible &&
                 (Field<WorkflowState>(session, "_observation").Generation != workflow.State.Generation || !session.CanEdit); i++)
                await Task.Delay(10);
            await Task.Delay(40);
        }

        async Task CheckDial(GainDialControl dial, Func<Task> pending, string name)
        {
            var before = workflow.State.Snapshot;
            int count = workflow.State.UndoCount;
            decimal initial = dial.Amplitude!.Value;
            // Choose direction with headroom; positive/negative half-turns include both sides of the seam.
            int direction = initial > .5m ? -1 : 1;
            var center = new Point(dial.Bounds.Width / 2, dial.Bounds.Height / 2);
            double radius = Math.Min(dial.Bounds.Width, dial.Bounds.Height) * .36;
            Point At(double angle, int i) => center + new Vector(Math.Cos(angle), Math.Sin(angle)) *
                (radius * (1 + Math.Sin(i * .7) * .12));
            var path = Enumerable.Range(0, 41).Select(i => At(-Math.PI + direction * i * Math.PI / 40, i)).ToArray();
            decimal last = initial;
            var values = new List<decimal>();
            double? playY = pane.IsEffectivelyVisible ? Id("Drum.Audition").TranslatePoint(default, pane)!.Value.Y : null;
            traces.Add(new { Dial = name, Samples = await input.TraceDrag(dial, path, _ =>
            {
                decimal value = dial.Amplitude!.Value;
                Require(direction * (value - last) >= -.002m && dial.HasGesture,
                    $"{name}: rotation reversed or lost capture. Value={value}; previous={last}; direction={direction}; capture={dial.HasGesture}");
                Require(workflow.State.Snapshot.Revision == before.Revision, name + ": preview committed early.");
                if (playY is { } y) Require(Math.Abs(Id("Drum.Audition").TranslatePoint(default, pane)!.Value.Y - y) <= .5,
                    "Draft feedback moved Play during a gesture.");
                values.Add(value); last = value;
            }, fine: true), Values = values });
            await pending();
            Require(Math.Abs(last - initial) > .10m && workflow.State.UndoCount == count + 1,
                name + ": circular fine gesture did not commit once.");
            await Undo(before);
            await input.TraceDrag(dial, [path[0], path[0]]);
            await pending();
            Require(workflow.State.Snapshot.Revision == before.Revision && workflow.State.UndoCount == count,
                name + ": stationary click changed the parameter.");
            await input.TraceDrag(dial, path, escape: true);
            Require(workflow.State.Snapshot.Revision == before.Revision && workflow.State.UndoCount == count,
                name + ": Escape committed a dial.");
            checks.Add(name + ": curved half-turn / Shift / Escape / native Undo");
            SaveProgress();
        }

        async Task CheckDialBoundaries(GainDialControl dial, NodeEditorPaneContent editor)
        {
            var before = workflow.State.Snapshot;
            int count = workflow.State.UndoCount;
            var center = new Point(dial.Bounds.Width / 2, dial.Bounds.Height / 2);
            Point At(double degrees) => center + new Vector(Math.Cos(degrees * Math.PI / 180), Math.Sin(degrees * Math.PI / 180)) * 30;
            var arc = Enumerable.Range(0, 73).Select(i => At(-90 + i * 5))
                .Concat(Enumerable.Range(1, 18).Select(i => At(270 - i * 5))).ToArray();
            decimal last = dial.Amplitude!.Value;
            traces.Add(new { Boundary = "Gain full turn, seam, saturation and immediate reversal",
                Samples = await input.TraceDrag(dial, arc, i =>
                {
                    decimal value = dial.Amplitude!.Value;
                    Require(dial.HasGesture && (i <= 72 ? value >= last : value < last), "Dial failed at wrap/limit/reversal.");
                    if (i == 72) Require(value == 1, "Full rotation did not reach the limit.");
                    last = value;
                }) });
            await editor.PendingCommand;
            Require(workflow.State.UndoCount == count + 1, "Boundary gesture created wrong history.");
            await Undo(before);
            var crossing = new[] { At(-90), At(0), center, At(90), At(180) };
            last = dial.Amplitude!.Value;
            traces.Add(new { Boundary = "Gain center crossing and re-anchor",
                Samples = await input.TraceDrag(dial, crossing, i =>
                {
                    decimal value = dial.Amplitude!.Value;
                    Require(i is 0 or 2 or 3 ? value == last : value > last, "Center crossing jumped or reversed the dial.");
                    last = value;
                }) });
            await editor.PendingCommand;
            await Undo(before);
            // Deliberately induced application capture loss is kept separate from native Escape evidence.
            await new ControlInputDriver(main, true, output).TraceDrag(dial, crossing, captureLoss: true);
            Require(workflow.State.Snapshot.Revision == before.Revision && workflow.State.UndoCount == count && !dial.HasGesture,
                "Routed capture loss committed a draft.");
            checks.Add("Gain native full turn / limits / immediate reversal / center crossing / Undo; routed capture-loss cancellation");
            SaveProgress();
        }

        void SaveProgress(string result = "running") => Json(output, "control-input", new
            { Result = result, NativeWindowsInput = !routed,
                Checks = checks.Select(check => routed ? check.Replace("native Windows input", "routed Avalonia input", StringComparison.OrdinalIgnoreCase)
                    .Replace("native ", "routed ", StringComparison.OrdinalIgnoreCase) : check),
                Traces = traces });

        async Task ReviewDrum()
        {
            var shell = Field<ShellSession>(main, "_session");
            var alignment = new List<object>();
            foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(20, 5, 760, 760));
                main.UpdateLayout();
                foreach (var field in pane.GetVisualDescendants().OfType<TextBox>().Where(c => c.IsEffectivelyVisible))
                {
                    var unit = ((Grid)field.Parent!).Children.OfType<TextBlock>().Single();
                    alignment.Add(FieldAlignment.Verify(field, unit, main, language + "/" + theme));
                }
                await CaptureAsync(main, output, $"clap-{language}-{theme}");
                double wideDial = pane.GetVisualDescendants().OfType<GainDialControl>().First().Width;
                int wideColumns = Field<int>(pane, "_parameterColumns");
                double wideGraph = plot.Height;
                workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 360, 290));
                main.UpdateLayout();
                var compactDial = pane.GetVisualDescendants().OfType<GainDialControl>().First();
                Require(compactDial.Width >= 64 && compactDial.Width <= 84 &&
                        Field<int>(pane, "_parameterColumns") < wideColumns && plot.Height < wideGraph,
                    $"Narrow Drum pane did not scale controls: dial={wideDial}->{compactDial.Width}; graph={wideGraph}->{plot.Height}; " +
                    $"pane={pane.Bounds}; columns={Field<int>(pane, "_parameterColumns")}; plotCapture={plot.HasGesture}; " +
                    $"dialCapture={compactDial.HasGesture}; velocityCapture={Field<GainLevelControl>(pane, "_velocity").HasGesture}");
                var actions = Id("Drum.Audition").TranslatePoint(default, pane)!.Value;
                var stop = Id("Drum.StopAudition").TranslatePoint(default, pane)!.Value;
                Require(actions.Y >= 0 && stop.Y >= 0 && actions.Y + 56 <= pane.Bounds.Height && stop.X + 56 <= pane.Bounds.Width,
                    "Reduced Drum pane clipped Play/Stop.");
                await CaptureAsync(main, output, $"clap-reduced-{language}-{theme}");
            }
            workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(20, 5, 760, 760));
            main.UpdateLayout();
            Json(output, "field-alignment", alignment);
            await ResponsiveDrumLayoutAsync(main, pane, output, input);
        }

        async Task RapidAudition()
        {
            var button = Id("Drum.Audition");
            await input.Click(button);
            await pane.PendingCommand;
            var device = Field<WorkflowOutput>(workflow, "_noteOutput");
            var sampler = device.Sampler;
            int prepared = sampler.PreparedStatesCreated;
            var hits = new List<object>();
            for (int i = 0; i < 8; i++)
            {
                long before = sampler.DrumAuditionHits;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                await input.RapidClick(button);
                while (sampler.DrumAuditionHits == before && watch.ElapsedMilliseconds < 400) await Task.Delay(2);
                Require(sampler.DrumAuditionHits > before, "Rapid audition click did not reach the physical renderer.");
                hits.Add(new { Index = i, Hit = sampler.DrumAuditionHits, Milliseconds = watch.Elapsed.TotalMilliseconds });
                Require(ReferenceEquals(sampler, Field<WorkflowOutput>(workflow, "_noteOutput").Sampler),
                    "Rapid audition reopened the output.");
                await Task.Delay(80);
            }
            await pane.PendingCommand;
            long last = sampler.DrumAuditionHits;
            await Task.Delay(450);
            Require(sampler.DrumAuditionHits == last, "Delayed audition backlog remained after rapid clicks.");
            Require(sampler.PreparedStatesCreated == prepared, "Ready retriggers rendered new PCM.");
            string? physicalFailure = device.ReadFailure();
            Require(physicalFailure is null, "Physical audition output failed: " + physicalFailure);
            await input.RapidClick(button);
            await pane.PendingCommand;
            for (int i = 0; i < 100 && sampler.DrumAuditionVoices == 0; i++) await Task.Delay(2);
            Require(sampler.DrumAuditionVoices > 0, "Stop fixture had no active sample.");
            await input.RapidClick(Id("Drum.StopAudition"));
            for (int i = 0; i < 100 && sampler.DrumAuditionVoices > 0; i++) await Task.Delay(2);
            Require(sampler.DrumAuditionVoices == 0, "Local sample Stop did not reach the renderer.");
            long stoppedEpoch = sampler.DrumAuditionHits;
            await Task.Delay(200);
            Require(sampler.DrumAuditionHits == stoppedEpoch && sampler.DrumAuditionVoices == 0, "Stopped audition restarted late.");
            Json(output, "rapid-audition", new { NativeWindowsInput = !routed, device.Description,
                PreparedStates = prepared, Hits = hits, NoDelayedBacklog = true, LocalSampleStop = true });
            checks.Add("native eight rapid audition clicks / one output / cached PCM / no delayed backlog / local Stop");
            SaveProgress();
        }
    }
}
