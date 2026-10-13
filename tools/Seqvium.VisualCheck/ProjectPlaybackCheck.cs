// SPDX-License-Identifier: Apache-2.0
using Avalonia.Controls;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task ProjectPlaybackAsync(MainWindow main, string output, string project)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        Require(await workflow.OpenAsync(Path.GetFullPath(project), UnsavedDecision.Discard), "Project open failed: " + workflow.State.Detail);
        await Task.Delay(100);
        var workspace = Field<WorkspaceHost>(main, "_workspace"); workspace.State.ArrangeMusic(); main.UpdateLayout();
        var original = workflow.State.Snapshot;
        async Task Play(string step)
        {
            bool started = await workflow.PlayAsync(); await workflow.ObserveAsync();
            Json(output, "project-" + step, new { Started = started, workflow.State.CanPlay, workflow.State.StatusKey,
                workflow.State.Detail, workflow.State.Device, workflow.State.Execution, workflow.State.SongMode,
                Tempo = workflow.State.Snapshot.State.Settings.Tempo.BeatsPerMinute, PhysicalAudio = true });
            Require(started, "Actual project Play failed: " + workflow.State.Detail);
            await Task.Delay(700); await workflow.ObserveAsync();
            Require(workflow.State.Execution?.Execution.Position > 0, "Actual project renderer did not advance.");
            Require(workflow.State.Execution?.Execution.Playing == true && workflow.State.Execution.Blocker is null,
                "Actual project stopped or retained a preparation blocker.");
            Json(output, "project-" + step + "-rendered", new { workflow.State.Execution, workflow.State.Device, PhysicalAudio = true });
            await CaptureAsync(main, output, "project-" + step);
            int peakVoices = workflow.State.Execution!.Execution.Voices;
            bool song = workflow.State.SongMode;
            long end = ConstantTempoConversion.ToFrames(new(ArrangementPreparation.EndTicks(workflow.State.Snapshot.State)),
                workflow.State.Snapshot.State.Settings.Tempo, workflow.State.ExecutionSampleRate);
            for (int pass = 0; pass < 24 && (!song || workflow.State.Execution.Execution.Playing); pass++)
            {
                await Task.Delay(100); await workflow.ObserveAsync();
                peakVoices = Math.Max(peakVoices, workflow.State.Execution!.Execution.Voices);
            }
            if (song) Require(!workflow.State.Execution!.Execution.Playing && workflow.State.Execution.Execution.Position == end,
                "Actual composition did not reach its finite end.");
            else Require(workflow.State.Execution!.Execution.Playing && workflow.State.Execution.Execution.Looping,
                "Actual Pattern did not continue looping.");
            Json(output, "project-" + step + "-completed", new { workflow.State.Execution, PeakVoices = peakVoices, PhysicalAudio = true });
            Require(await workflow.StopAsync(), "Project Stop failed.");
        }
        await Play("original");
        decimal bpm = original.State.Settings.Tempo.BeatsPerMinute;
        Require(await workflow.SetTempoAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, bpm == 135 ? 125 : 135), "Project tempo change failed.");
        await Play("changed-tempo");
        Require(await workflow.UndoAsync(), "Project tempo Undo failed."); await Play("restored-tempo");
        Require(await workflow.SetSongModeAsync(false), "Pattern mode failed."); await Play("pattern");
        Require(await workflow.SetTempoAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, bpm == 135 ? 125 : 135), "Pattern tempo change failed.");
        await Play("pattern-changed-tempo");
        Require(await workflow.UndoAsync(), "Pattern tempo Undo failed."); await Play("pattern-restored-tempo");
        Require(await workflow.SetSongModeAsync(true), "Song mode restore failed.");
        Require(workflow.State.Snapshot == original && !workflow.State.IsDirty, "Verification altered the saved project.");
        Json(output, "project-playback", new { Passed = true, PhysicalAudio = true, OriginalProjectSaved = false,
            OriginalRevisionRestored = true, OriginalTempo = bpm });
    }
}
