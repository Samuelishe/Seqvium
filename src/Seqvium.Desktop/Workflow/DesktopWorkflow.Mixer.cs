// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal readonly record struct MixerEditBasis(Guid Lifecycle, long Generation);

internal sealed partial class DesktopWorkflow
{
    internal Task<bool> ResetMixerMetersAsync() => _owner.Run(() =>
    {
        _output?.Sampler.ResetMixerMeters();
        return Task.FromResult(true);
    });

    internal Task<bool> EditMixerAsync(MixerEditBasis basis, string description, Action<ProjectEdit> edit) =>
        Command("Workflow.EditFailed", () =>
        {
            if (_document.LifecycleId != basis.Lifecycle || _document.Generation != basis.Generation)
                throw new InvalidOperationException("Workflow.EditStale");
            bool changed = _document.Edit(description, edit);
            _status = "Workflow.Ready";
            return Task.FromResult(changed);
        });

    internal Task<MixerMeter> ReadMixerAsync(Id<MixerChannel>? channel) =>
        _owner.Run(() => Task.FromResult(_output?.Sampler.ReadMixerMeter(channel) ?? default));
}
