// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private sealed record SynthAudition(NodeEditorTarget Target, decimal Pitch, decimal Intensity, bool Held);

    private SynthAudition? _synthAudition;
    private bool _retireSynthAudition;
    internal NodeEditorTarget? SynthPreviewTarget => Volatile.Read(ref _synthAudition)?.Target;
    internal bool SynthPreviewHeld => Volatile.Read(ref _synthAudition)?.Held == true;

    private SoundDefinition SynthSource(NodeEditorTarget target, long generation)
    {
        if (_document.LifecycleId != target.LifecycleId || _document.Generation != generation ||
            DrumEditorSession.Resolve(_document.Current.State, target) is not { } sound || !SynthVoice.Supports(sound))
            throw new InvalidOperationException("Workflow.EditStale");
        return sound;
    }

    internal Task<bool> SetSynthParametersAsync(NodeEditorTarget target, long generation,
        ImmutableDictionary<string, decimal> before, ImmutableDictionary<string, decimal> after)
        => Command("Workflow.EditFailed", false, () =>
        {
            var sound = SynthSource(target, generation);
            if (!sound.Parameters.OrderBy(p => p.Key).SequenceEqual(before.OrderBy(p => p.Key)))
                throw new InvalidOperationException("Workflow.EditStale");
            SynthVoice.Validate(after);
            return Task.FromResult(_document.Edit("Shape Synth", edit => edit.SetSoundParameters(sound.Id, after)));
        });

    internal Task<bool> PreviewSynthParametersAsync(NodeEditorTarget target, long generation,
        ImmutableDictionary<string, decimal>? values) => _owner.Run(() =>
    {
        // Draft updates never enter history, Save, graph preparation or another preview's scope.
        if (_closed || _document.LifecycleId != target.LifecycleId || _document.Generation != generation ||
            _synthAudition is not { } preview || preview.Target != target ||
            DrumEditorSession.Resolve(_document.Current.State, target) is not { } sound || !SynthVoice.Supports(sound))
            return Task.FromResult(false);
        SynthVoice.Validate(values ?? sound.Parameters);
        return Task.FromResult(
            _noteOutput?.Sampler.UpdateSynthAudition(sound with { Parameters = values ?? sound.Parameters },
                preview.Pitch) == true);
    });

    private bool RefreshSynthAudition()
    {
        if (_synthAudition is not { } preview || _document.LifecycleId != preview.Target.LifecycleId ||
            DrumEditorSession.Resolve(_document.Current.State, preview.Target) is not { } sound ||
            !SynthVoice.Supports(sound))
            return false;
        _noteOutput?.Sampler.UpdateSynthAudition(sound, preview.Pitch);
        _samplerAudition = (preview.Target, _document.Generation);
        return true;
    }

    internal Task<bool> HoldSynthAsync(NodeEditorTarget target, long generation, decimal pitch, decimal intensity)
        => Command("Workflow.ActionFailed", false, async () =>
        {
            var sound = SynthSource(target, generation);
            if (_coordinator?.ReadStatus().Execution.Playing == true)
                throw new InvalidOperationException("Browser.StopTransport");
            if (_synthAudition is { } preview && preview.Target == target && _noteOutput is { } resident &&
                !resident.Sampler.IsTerminated && resident.ReadFailure() is null)
            {
                resident.Sampler.RetireCompleted();
                if (preview.Held && (resident.Sampler.HasPending || resident.Sampler.ReadStatus().Playing))
                {
                    resident.Sampler.UpdateSynthAudition(sound, pitch);
                    Volatile.Write(ref _synthAudition, preview with { Pitch = pitch, Intensity = intensity });
                    _status = "Workflow.Ready";
                    return true;
                }
            }
            else await CloseNoteAuditionAsync();

            long transport = _playGeneration;
            _noteOutput ??= _openOutput();
            try
            {
                if (_noteSession is null)
                {
                    _noteSession = new();
                    _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
                }

                _noteOutput.Sampler.SetGain(1);
                _noteOutput.Sampler.ProtectSourceAudition();
                using var request = new NoteAuditionRequest(_noteOutput.Sampler, _document, sound.Id, pitch, intensity,
                    held: true);
                await request.PrepareAsync();
                SynthSource(target, generation);
                if (_noteOutput.ReadFailure() is { } failure) throw new IOException(failure);
                if (transport != _playGeneration || request.Publish() != SamplerPublication.Accepted)
                    throw new InvalidOperationException("Piano.AuditionUnavailable");
                _samplerAudition = (target, generation);
                Volatile.Write(ref _synthAudition, new(target, pitch, intensity, true));
                // A deliberate successful retry replaces a previously reported output fault.
                _status = "Workflow.Ready";
                return true;
            }
            catch
            {
                await CloseNoteAuditionAsync();
                throw;
            }
        });
}
