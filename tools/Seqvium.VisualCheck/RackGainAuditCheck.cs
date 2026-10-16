// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task RackGainAuditAsync(MainWindow main, string output, bool native)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var controls = Field<WorkflowControls>(main, "_workflowControls");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var graphSession = Field<GraphEditorSession>(main, "_graphEditor");
        var editor = Field<NodeEditorSession>(main, "_nodeEditor");
        var driver = new ControlInputDriver(main, !native, output);
        var traces = new List<object>();
        var receipts = new List<string>();
        main.WindowState = WindowState.Normal;
        main.Width = 1400;
        main.Height = 900;
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "Gain fixture New failed.");
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
            "Live Gain check"), "Gain fixture Pattern failed.");
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Sustain", SynthVoice.Algorithm),
            "Gain fixture Synth failed.");
        var pattern = BeatSteps.Resolve(workflow.State).Pattern!;
        var part = pattern.Parts.Single();
        var attachment = workflow.State.Selected!.Value;
        GraphEditBasis GraphBasis() => new(workflow.State.Document.LifecycleId, workflow.State.Generation, attachment);
        Require(await workflow.EditGraphAsync(GraphBasis(), "Sustained verification fixture", (edit, _, _) =>
        {
            edit.SetPatternLength(pattern.Id, new(122880000));
            edit.SetPartNotes(pattern.Id, part.Id, [new(Id<NoteEvent>.New(), new(0), new(122880000), 60, .15m)]);
            edit.InitializeMixer();
        }), "Sustain fixture failed.");
        Require(await workflow.DuplicateBeatChannelAsync(Basis(), part.Id), "Second channel fixture failed.");
        workspace.State.Hide(WorkspaceState.GraphId);
        workspace.State.Hide(WorkspaceState.NodeEditorId);
        workspace.Open(WorkspaceState.BeatId);
        workspace.State.SetBounds(WorkspaceState.BeatId, new(5, 5, 850, 390));
        workspace.Commit();

        async Task Refresh()
        {
            await controls.PendingGainPreview;
            await controls.PendingGainCommand;
            await workflow.ObserveAsync();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            controls.Refresh();
            main.UpdateLayout();
            await Task.Delay(60);
        }

        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c =>
            c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);

        await Refresh();
        if (native) main.Activate();
        await driver.ActivateVisibleWindow();
        await driver.Click(Id("Workflow.Play"));
        for (int i = 0; i < 300 && workflow.State.Execution?.Execution.Playing != true; i++) await Task.Delay(10);
        Require(workflow.State.Execution?.Execution.Playing == true,
            "Physical Pattern did not start: " + workflow.State.Detail);
        await Task.Delay(250);
        var device = Field<WorkflowOutput>(workflow, "_output");
        var sampler = device.Sampler;
        var initial = sampler.ReadStatus();
        var level = (GainLevelControl)Id("Workflow.GainLevel");
        var snapshot = workflow.State.Snapshot;
        int history = workflow.State.UndoCount;
        float initialPeak = sampler.ReadMixerMeter().PeakLeft;
        Require(initialPeak > 0, "Prepared physical DSP has no signal.");
        var availability = new List<bool>();
        void Changed(WorkflowState state) => availability.Add(state.Busy);
        workflow.Changed += Changed;
        try
        {
            void Held(int i, bool silence)
            {
                if (i != 3) return;
                var status = sampler.ReadStatus();
                var meter = sampler.ReadMixerMeter();
                Require(level.HasGesture && workflow.State.Snapshot == snapshot && workflow.State.UndoCount == history,
                    "Held native drag changed history or lost capture.");
                Require(status.Epoch == initial.Epoch && status.PreparedExecutionId == initial.PreparedExecutionId &&
                        status.Position > initial.Position && status.Voices == initial.Voices,
                    "Held drag restarted transport/voices/execution.");
                Require(silence ? meter.PeakLeft == 0 : meter.PeakLeft > initialPeak * 1.5,
                    "Prepared DSP did not change amplitude before release.");
                traces.Add(new
                {
                    Phase = silence ? "held-silence" : "held-raised", status, meter,
                    level.HasGesture, CanonicalGain = workflow.State.Gain
                });
            }

            double y = level.Bounds.Height / 2;
            await driver.TraceDrag(level, [new(32, y), new(0, y), new(0, y), new(0, y)],
                i => Held(i, true), escape: true, intervalMs: 100);
            await Refresh();
            Require(workflow.State.Snapshot == snapshot && sampler.ReadMixerMeter().PeakLeft > 0 &&
                    level.Amplitude == .25m, "Escape did not restore sound and fader.");
            receipts.Add("physical-held-silence-before-release-escape-restores-no-history");
            await driver.TraceDrag(level, [new(32, y), new(86, y), new(86, y), new(86, y)],
                i => Held(i, false), intervalMs: 100);
            await Refresh();
            Require(workflow.State.Gain > .7m && workflow.State.UndoCount == history + 1,
                "Accepted native drag was not one Undo.");
            Require(!availability.Contains(true), "Gain gesture flashed Busy.");
            workflow.Changed -= Changed;
            await driver.Click(Id("Workflow.Undo"));
            await Refresh();
            Require(workflow.State.Gain == .25m && sampler.ReadMixerMeter().PeakLeft > 0 &&
                    sampler.ReadMixerMeter().PeakLeft < initialPeak * 1.3, "Undo did not restore the heard Gain.");
            await driver.Click(Id("Workflow.Redo"));
            await Refresh();
            Require(sampler.ReadMixerMeter().PeakLeft > initialPeak * 1.5, "Redo did not restore accepted sound.");
            receipts.Add("physical-commit-one-undo-redo-no-busy");

            var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
            var second = BeatSteps.Resolve(workflow.State).Pattern!.Parts[1];
            var handle = Id("Beat.Name." + second.Id);
            var firstHandle = Id("Beat.Name." + part.Id);
            var from = new Point(handle.Bounds.Width / 2, handle.Bounds.Height / 2);
            var to = firstHandle.TranslatePoint(new(20, -4), handle)!.Value;
            await driver.TraceDrag(handle, [from, to], escape: true);
            await rack.PendingCommand;
            await Refresh();
            Require(BeatSteps.Resolve(workflow.State).Pattern!.Parts[0].Id == part.Id,
                "Playing reorder cancellation changed order.");
            await driver.TraceDrag(handle, [from, to]);
            await rack.PendingCommand;
            await Refresh();
            Require(BeatSteps.Resolve(workflow.State).Pattern!.Parts[0].Id == second.Id &&
                    sampler.ReadStatus().PreparedExecutionId == initial.PreparedExecutionId &&
                    sampler.TransportEpoch == initial.Epoch && sampler.ActiveVoices == initial.Voices,
                "Playing reorder restarted the plan or changed voices.");
            handle = Id("Beat.Name." + second.Id);
            firstHandle = Id("Beat.Name." + part.Id);
            from = new(handle.Bounds.Width / 2, handle.Bounds.Height / 2);
            to = firstHandle.TranslatePoint(new(20, firstHandle.Bounds.Height + 4), handle)!.Value;
            if (native) await new NativePointer.DrumInput(main).CtrlDrag(handle, from, to);
            else await RoutedChannelDrag(main, handle, from, to, KeyModifiers.Control);
            await rack.PendingCommand;
            await Refresh();
            Require(BeatSteps.Resolve(workflow.State).Pattern!.Parts.Length == 3 &&
                    sampler.TransportEpoch == initial.Epoch && sampler.ActiveVoices == initial.Voices,
                "Playing Ctrl-copy restarted surviving voices or failed.");
            receipts.Add("playing-name-reorder-cancel-and-ctrl-copy-preserve-surviving-voices");

            // Surface handoff/resize are deliberately routed probes, separate from native trajectories above.
            var gain = workflow.State.Snapshot.State.Graphs.Single().Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
            foreach (string reason in new[] { "graph", "editor", "resize", "capture", "stop" })
            {
                snapshot = workflow.State.Snapshot;
                history = workflow.State.UndoCount;
                using var pointer = new Pointer(880019, PointerType.Mouse, true);
                var at = level.TranslatePoint(new(25, y), main)!.Value;
                level.RaiseEvent(new PointerPressedEventArgs(level, pointer, main, at, 0,
                    new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                    KeyModifiers.None, 1));
                await controls.PendingGainPreview;
                Require(level.HasGesture, "Routed cancellation fixture did not capture.");
                if (reason == "graph")
                {
                    graphSession.Open(workflow.State, attachment);
                    graphSession.Select(new(GraphHitKind.Node, gain.Id));
                    workspace.Open(WorkspaceState.GraphId);
                    workspace.Commit();
                }

                if (reason == "editor")
                {
                    editor.Open(workflow.State, new(workflow.State.Document.LifecycleId, attachment,
                        workflow.State.Snapshot.State.Graphs.Single().Id, gain.Id));
                    workspace.Open(WorkspaceState.NodeEditorId);
                    workspace.Commit();
                }

                if (reason == "resize")
                {
                    main.Width = 700;
                    main.UpdateLayout();
                    await Task.Delay(100);
                }

                if (reason == "capture") pointer.Capture(null);
                if (reason == "stop") await workflow.StopAsync();
                await Refresh();
                level.RaiseEvent(new PointerReleasedEventArgs(level, pointer, main, at, 0,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                    KeyModifiers.None, MouseButton.Left));
                await Refresh();
                Require(!level.HasGesture && workflow.State.Snapshot == snapshot &&
                        workflow.State.UndoCount == history && level.Amplitude == workflow.State.Gain,
                    reason + " handoff did not cancel the draft.");
                receipts.Add("routed-gain-cancel-" + reason);
                workspace.State.Hide(WorkspaceState.GraphId);
                workspace.State.Hide(WorkspaceState.NodeEditorId);
                workspace.Commit();
                main.Width = 1400;
                await Refresh();
            }

            Require(device.ReadFailure() is null, "Physical output fault: " + device.ReadFailure());
            Json(output, "live-gain", new
            {
                Passed = true, NativeInput = native, device.Description,
                sampler.SampleRate, sampler.Channels, traces, receipts, SubjectiveListening = false
            });
        }
        finally
        {
            workflow.Changed -= Changed;
            await workflow.StopAsync();
        }
    }
}
