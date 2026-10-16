// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private WorkflowOutput? _noteOutput;
    private AudioDeviceSession? _noteSession;

    internal Task<bool> EditPianoNotesAsync(BeatEditBasis basis, Id<MusicalPart> partId, IReadOnlyList<NoteEvent> notes)
    {
        var desired = notes.ToArray();
        return Command("Workflow.EditFailed", () =>
        {
            var (pattern, _, _, _) = ValidateBeatBasis(basis, requireGrid: false);
            var part = pattern.Parts.SingleOrDefault(p => p.Id == partId) ??
                       throw new InvalidOperationException("Beat.Stale");
            if (PianoNotes.Refusal(_document.Current.State, pattern, part) is { } refusal)
                throw new InvalidOperationException(refusal);
            if (desired.Any(n => !n.AdditionalData.IsEmpty))
                throw new InvalidOperationException("Beat.NotesUnsupported");
            // Gestures can submit changed notes last. Keep surviving canonical order, including
            // saturated/zero moves, so a presentation-only permutation cannot create an Undo entry.
            var byId = desired.ToDictionary(n => n.Id);
            var existing = part.Notes.Select(n => n.Id).ToHashSet();
            var ordered = part.Notes.Where(n => byId.ContainsKey(n.Id)).Select(n => byId[n.Id])
                .Concat(desired.Where(n => !existing.Contains(n.Id))).ToArray();
            bool changed = _document.Edit("Edit Piano Roll notes",
                edit => edit.SetPartNotes(pattern.Id, part.Id, [.. ordered]));
            _status = "Workflow.Ready";
            return Task.FromResult(changed);
        });
    }

    internal Task<bool> SetRootPitchAsync(BeatEditBasis basis, Id<MusicalPart> partId, decimal root) => Command(
        "Workflow.EditFailed", () =>
        {
            var (pattern, _, _, _) = ValidateBeatBasis(basis, requireGrid: false);
            var part = pattern.Parts.Single(p => p.Id == partId);
            var sound = _document.Current.State.Sounds.Single(s => s.Id == part.SoundId);
            if (PianoNotes.SoundRefusal(sound) is { } refusal) throw new InvalidOperationException(refusal);
            if (sound.Algorithm != PcmSampler.Algorithm) throw new InvalidOperationException("Beat.SoundUnsupported");
            PcmSampler.ValidateConfiguration(root, sound.Parameters["releaseMilliseconds"]);
            bool changed = _document.Edit("Set sample root pitch",
                edit => edit.SetSoundParameters(sound.Id, sound.Parameters.SetItem("rootPitch", root)));
            _status = "Workflow.Ready";
            return Task.FromResult(changed);
        });

    /// <summary>Channel identity and shared sampler tuning commit once without rewriting notes.</summary>
    internal Task<bool> ConfigurePianoSourceAsync(BeatEditBasis basis, Id<MusicalPart> partId, string name,
        decimal root)
        => Command("Workflow.EditFailed", () =>
        {
            var (pattern, _, _, _) = ValidateBeatBasis(basis, requireGrid: false);
            var part = pattern.Parts.Single(p => p.Id == partId);
            var sound = _document.Current.State.Sounds.Single(s => s.Id == part.SoundId);
            if (PianoNotes.SoundRefusal(sound) is { } refusal) throw new InvalidOperationException(refusal);
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Piano.NameRequired");
            if (sound.Algorithm == PcmSampler.Algorithm)
                PcmSampler.ValidateConfiguration(root, sound.Parameters["releaseMilliseconds"]);
            else if (root != (SynthVoice.Supports(sound) ? 60 : DrumVoice.RootPitch))
                throw new InvalidOperationException("Beat.SoundUnsupported");
            bool changed = _document.Edit("Configure Piano Roll channel", edit =>
            {
                edit.RenamePart(pattern.Id, part.Id, name);
                if (sound.Algorithm == PcmSampler.Algorithm)
                    edit.SetSoundParameters(sound.Id, sound.Parameters.SetItem("rootPitch", root));
            });
            _status = "Workflow.Ready";
            return Task.FromResult(changed);
        });

    /// <summary>Solo audition uses the shared PCM renderer without editing or starting transport.</summary>
    internal Task<bool> AuditionNoteAsync(BeatEditBasis basis, Id<MusicalPart> partId, decimal pitch,
        decimal intensity = 0.8m)
        => Command("Workflow.ActionFailed", async () =>
        {
            var (pattern, _, _, _) = ValidateBeatBasis(basis, requireGrid: false);
            var part = pattern.Parts.Single(p => p.Id == partId);
            var sound = _document.Current.State.Sounds.Single(s => s.Id == part.SoundId);
            if (PianoNotes.SoundRefusal(sound) is { } refusal) throw new InvalidOperationException(refusal);
            if (_coordinator?.ReadStatus().Execution.Playing == true) return false;
            await CloseNoteAuditionAsync();
            long auditionGeneration = _playGeneration;
            _noteOutput = _openOutput();
            try
            {
                _noteSession = new();
                _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
                _noteOutput.Sampler.SetGain(0.12f);
                using var request = DrumVoice.Supports(sound)
                    ? new NoteAuditionRequest(_noteOutput.Sampler, _document, sound.Id, pitch, intensity)
                    : new NoteAuditionRequest(_noteOutput.Sampler, _document, sound.Id, pitch, intensity);
                await request.PrepareAsync();
                ValidateBeatBasis(basis, requireGrid: false);
                if (auditionGeneration != _playGeneration)
                {
                    await CloseNoteAuditionAsync();
                    return false;
                }

                if (request.Publish() != SamplerPublication.Accepted)
                    throw new InvalidOperationException("Piano.AuditionUnavailable");
                return true;
            }
            catch
            {
                await CloseNoteAuditionAsync();
                throw;
            }
        });

    private async Task CloseNoteAuditionAsync(bool preserveDrumRequests = false)
    {
        _samplerAudition = null;
        if (!preserveDrumRequests) InvalidateDrumAuditions();
        _drumAuditionOutput = false;
        _drumAuditionSource = null;
        if (!_labContextual) _labAudible = null;
        _browserAudition?.EndLifetime();
        if (_noteSession?.OutputSampler is not null) await _noteSession.DisposeAsync();
        else if (_noteOutput is { } output)
        {
            output.Sampler.Stop();
            output.Lifetime.Close();
            if (!output.Lifetime.Joined) throw new InvalidOperationException("Audition output did not join.");
            output.Sampler.TerminateExecution();
            output.Lifetime.Dispose();
            output.Sampler.Dispose();
        }

        _noteSession = null;
        _noteOutput = null;
        _browserAudition?.Dispose();
        _browserAudition = null;
    }
}
