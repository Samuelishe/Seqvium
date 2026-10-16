// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Seqvium.Desktop;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Seqvium.Core;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task SynthLiveGesturesAsync(MainWindow main, string output, bool native,
        string phase, Func<Task> refresh)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var pane = main.GetVisualDescendants().OfType<SynthPaneContent>().Single();

        Control Id(string id) => pane.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == "Synth." + id);

        var device = Field<WorkflowOutput>(workflow, "_noteOutput");
        var driver = new ControlInputDriver(main, !native, output);
        var initial = device.Sampler.ReadStatus();
        var records = new List<object>();
        var transitions = new List<string>();
        var playTransitions = new List<string>();
        bool playingCommand = false;
        var neighbours = main.GetVisualDescendants().OfType<Control>()
            .Where(c => !string.IsNullOrEmpty(AutomationProperties.GetAutomationId(c))).ToArray();
        var stable = pane.GetVisualDescendants().OfType<Control>().Where(c =>
            AutomationProperties.GetAutomationId(c) is { } id &&
            (id.StartsWith("Synth.Dial.", StringComparison.Ordinal) ||
             id.StartsWith("Synth.Value.", StringComparison.Ordinal) ||
             id.StartsWith("Synth.Wave", StringComparison.Ordinal) || id is "Synth.Presets" or "Synth.Note")).ToArray();

        void Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == InputElement.IsEnabledProperty)
                transitions.Add(AutomationProperties.GetAutomationId((Control)sender!) + ":" + e.NewValue);
        }

        foreach (var control in stable) control.PropertyChanged += Changed;

        void NeighbourChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (playingCommand && e.Property == InputElement.IsEnabledProperty)
                playTransitions.Add(AutomationProperties.GetAutomationId((Control)sender!) + ":" + e.NewValue);
        }

        foreach (var control in neighbours) control.PropertyChanged += NeighbourChanged;
        try
        {
            playingCommand = true;
            for (int i = 0; i < 3; i++)
            {
                await driver.Click(Id("Hold"));
                await refresh();
            }

            playingCommand = false;
            Require(playTransitions.Count == 0,
                "Repeated Play blinked workspace availability: " + string.Join(",", playTransitions));
            CheckContinuous();
            foreach (string key in new[]
                         { "cutoff", "resonance", "mix", "detune", "attack", "decay", "sustain", "release" })
            {
                var control = Id("Dial." + key);
                control.BringIntoView();
                await refresh();
                var before = workflow.State.Snapshot;
                int undo = workflow.State.UndoCount;
                var center = new Point(control.Bounds.Width / 2, control.Bounds.Height / 2);
                var points = Enumerable.Range(0, 9).Select(i => center + new Vector(
                    Math.Cos(-Math.PI / 2 + i * .17) * 19, Math.Sin(-Math.PI / 2 + i * .17) * 19)).ToArray();
                var samples = await driver.TraceDrag(control, points, index =>
                {
                    Require(workflow.State.Snapshot == before && workflow.State.UndoCount == undo,
                        "An unfinished live gesture changed canonical state: " + key);
                    CheckContinuous();
                }, intervalMs: 30);
                await pane.PendingPreview;
                await refresh();
                Require(workflow.State.UndoCount == undo + 1, "Live gesture must commit once: " + key);
                CheckContinuous();
                await UndoAsync();
                CheckContinuous();
                Require(workflow.State.Snapshot == before, "Live Undo did not restore canonical sound.");
                await driver.TraceDrag(control, points, _ => CheckContinuous(), escape: true, intervalMs: 25);
                await pane.PendingPreview;
                await refresh();
                CheckContinuous();
                Require(workflow.State.Snapshot == before && workflow.State.UndoCount == undo,
                    "Escape changed canonical sound/history: " + key);
                records.Add(new
                {
                    Key = key, Samples = samples, Epoch = device.Sampler.TransportEpoch,
                    Position = device.Sampler.RenderPosition, Voice = device.Sampler.ActiveVoices,
                    Commit = "one", Undo = "restored", Escape = "restored"
                });
            }

            var envelope = pane.GetVisualDescendants().OfType<SynthEnvelopePlot>().Single();
            envelope.BringIntoView();
            await refresh();
            var envelopeBefore = workflow.State.Snapshot;
            int envelopeUndo = workflow.State.UndoCount;
            var handle = envelope.Handles()[2];
            var curvePoints = Enumerable.Range(0, 6).Select(i => handle + new Vector(0, -i * 2)).ToArray();
            await driver.TraceDrag(envelope, curvePoints, _ =>
            {
                CheckContinuous();
                Require(workflow.State.Snapshot == envelopeBefore, "Live envelope draft leaked into canonical state.");
            });
            await pane.PendingPreview;
            await refresh();
            CheckContinuous();
            Require(workflow.State.UndoCount == envelopeUndo + 1, "Live envelope gesture must commit once.");
            await UndoAsync();
            await driver.TraceDrag(envelope, curvePoints, _ => CheckContinuous(), escape: true);
            await pane.PendingPreview;
            await refresh();
            CheckContinuous();
            Require(workflow.State.Snapshot == envelopeBefore && workflow.State.UndoCount == envelopeUndo,
                "Live envelope Escape changed canonical state/history.");
            var scroll = (ScrollViewer)pane.Children[1];
            scroll.Offset = default;
            await refresh();
            var unchanged = workflow.State.Snapshot;
            await driver.Click(Id("Wave1.3"));
            await refresh();
            CheckContinuous();
            if (workflow.State.Snapshot != unchanged)
            {
                await UndoAsync();
            }

            CheckContinuous();
            Require(transitions.Count == 0,
                "Preview/edit blinked neighbouring control availability: " + string.Join(",", transitions));
            await CaptureAsync(main, output, "synth-live-" + phase);
            Json(output, "synth-live-" + phase, new
            {
                Result = "passed", NativeInput = native, device.Description,
                Initial = initial, Final = device.Sampler.ReadStatus(), AvailabilityTransitions = transitions,
                WorkspacePlayTransitions = playTransitions,
                Gestures = records,
                SubmittedPcmListening = false
            });
        }
        finally
        {
            foreach (var control in stable) control.PropertyChanged -= Changed;
            foreach (var control in neighbours) control.PropertyChanged -= NeighbourChanged;
        }

        void CheckContinuous()
        {
            var current = device.Sampler.ReadStatus();
            Require(ReferenceEquals(Field<WorkflowOutput>(workflow, "_noteOutput"), device) &&
                    !device.Lifetime.Joined &&
                    device.ReadFailure() is null && current.Playing && current.Voices == 1 &&
                    current.Epoch == initial.Epoch &&
                    current.PreparedExecutionId == initial.PreparedExecutionId && current.Position >= initial.Position,
                "Live preview interrupted, restarted or faulted: " + phase + " · " + workflow.State.Detail);
        }

        async Task UndoAsync()
        {
            long generation = workflow.State.Generation;
            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            void Observed(WorkflowState state)
            {
                if (!state.Busy && state.Generation != generation) completed.TrySetResult();
            }

            workflow.Changed += Observed;
            try
            {
                var undo = main.GetVisualDescendants().OfType<Button>().Single(b =>
                    AutomationProperties.GetAutomationId(b) == "Workflow.Undo");
                await driver.Click(undo);
                if (!workflow.State.Busy && workflow.State.Generation != generation) completed.TrySetResult();
                await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await refresh();
            }
            finally
            {
                workflow.Changed -= Observed;
            }
        }
    }
}