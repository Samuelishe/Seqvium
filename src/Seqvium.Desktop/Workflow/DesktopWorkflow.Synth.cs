// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private SoundDefinition SynthSource(NodeEditorTarget target, long generation)
    {
        if (_document.LifecycleId != target.LifecycleId || _document.Generation != generation ||
            DrumEditorSession.Resolve(_document.Current.State, target) is not { } sound || !SynthVoice.Supports(sound))
            throw new InvalidOperationException("Workflow.EditStale");
        return sound;
    }

    internal Task<bool> SetSynthParametersAsync(NodeEditorTarget target, long generation,
        ImmutableDictionary<string, decimal> before, ImmutableDictionary<string, decimal> after)
        => Command("Workflow.EditFailed", async () =>
        {
            var sound = SynthSource(target, generation);
            if (!sound.Parameters.OrderBy(p => p.Key).SequenceEqual(before.OrderBy(p => p.Key)))
                throw new InvalidOperationException("Workflow.EditStale");
            SynthVoice.Validate(after);
            await CloseNoteAuditionAsync();
            SynthSource(target, generation);
            return _document.Edit("Shape Synth", edit => edit.SetSoundParameters(sound.Id, after));
        });

    internal Task<bool> HoldSynthAsync(NodeEditorTarget target, long generation, decimal pitch, decimal intensity)
        => Command("Workflow.ActionFailed", async () =>
        {
            var sound = SynthSource(target, generation);
            if (_coordinator?.ReadStatus().Execution.Playing == true)
                throw new InvalidOperationException("Browser.StopTransport");
            await CloseNoteAuditionAsync();
            long transport = _playGeneration;
            _noteOutput = _openOutput();
            try
            {
                _noteSession = new(); _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
                _noteOutput.Sampler.SetGain(.7f);
                using var request = new NoteAuditionRequest(_noteOutput.Sampler, _document, sound.Id, pitch, intensity, held: true);
                await request.PrepareAsync();
                SynthSource(target, generation);
                if (transport != _playGeneration || request.Publish() != SamplerPublication.Accepted)
                    throw new InvalidOperationException("Piano.AuditionUnavailable");
                _samplerAudition = (target, generation);
                return true;
            }
            catch { await CloseNoteAuditionAsync(); throw; }
        });
}
