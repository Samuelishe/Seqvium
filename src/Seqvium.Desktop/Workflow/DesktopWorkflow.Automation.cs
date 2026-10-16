// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    internal Task<bool> AddAutomationAsync(ArrangementEditBasis basis, AutomationTarget target)
        => Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            var state = _document.Current.State;
            if (!ParameterAutomation.IsResolved(state, target))
                throw new InvalidOperationException("Automation.Unavailable");
            if (state.AutomationLanes.Any(l => l.Target == target)) return Task.FromResult(false);
            var used = state.ArrangementClips.Select(c => c.Lane).Concat(state.AudioClips.Select(c => c.Lane))
                .Concat(state.AutomationLanes.Select(l => l.Lane))
                .Concat(state.ArrangementTracks.Where(t => t.Muted || t.Solo).Select(t => t.Lane)).ToHashSet();
            int lane = Enumerable.Range(0, 32).FirstOrDefault(n => !used.Contains(n), -1);
            if (lane < 0) throw new InvalidOperationException("Automation.NoLane");
            var sound = state.Sounds.SingleOrDefault(s => s.Id == target.SoundId);
            var channel = state.Mixer?.Channels.SingleOrDefault(c => c.Id == target.ChannelId);
            string name = sound?.Name ?? channel!.Name;
            double value = sound is not null ? (double)sound.Parameters["cutoff"] : (double)channel!.Settings.LevelDb;
            decimal normalized = ParameterAutomation.Normalize(target.Parameter, value);
            long end = Math.Max(state.Settings.Meter.TicksPerBar * 4, ArrangementPreparation.EndTicks(state));
            return Task.FromResult(_document.Edit("Create parameter automation", edit => edit.AddAutomationLane(lane,
                name,
                target,
                [
                    new(Id<AutomationPoint>.New(), new(0), normalized),
                    new(Id<AutomationPoint>.New(), new(end), normalized)
                ])));
        });

    internal Task<bool> EditAutomationAsync(ArrangementEditBasis basis, AutomationLane lane)
        => Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            return Task.FromResult(_document.Edit("Edit parameter automation",
                edit => edit.UpdateAutomationLane(lane)));
        });

    internal Task<bool> DeleteAutomationAsync(ArrangementEditBasis basis, Id<AutomationLane> lane)
        => Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            return Task.FromResult(_document.Edit("Delete parameter automation",
                edit => edit.DeleteAutomationLane(lane)));
        });
}
