// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private ImmutableDictionary<Id<SoundDefinition>, AudioWaveform> _audioWaveforms =
        ImmutableDictionary<Id<SoundDefinition>, AudioWaveform>.Empty;

    internal Task<bool> ImportAudioClipAsync(ArrangementEditBasis basis, string path, MusicalPosition position,
        int lane, CancellationToken cancellation = default, BrowserSource? browserSource = null) => Command("Workflow.ImportFailed", async () =>
    {
        using var use = browserSource is null ? null : BorrowBrowserSource(browserSource);
        ValidateArrangementBasis(basis);
        _status = "Workflow.Importing";
        Publish();
        string temporary = Path.Combine(Path.GetTempPath(), $"seqvium-audio-{Guid.NewGuid():N}.wav");
        try
        {
            try
            {
                await Task.Run(() => AudioFileDecode.Normalize(path, temporary, cancellation), cancellation);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                if (error.Message.StartsWith("AudioClip.", StringComparison.Ordinal)) throw;
                throw new IOException("AudioClip.DecodeFailed", error);
            }

            ValidateArrangementBasis(basis);
            using var request = await AudioMedia.PrepareImportAsync(_document, temporary,
                Path.GetFileNameWithoutExtension(path),
                $"Imported {Path.GetExtension(path).TrimStart('.').ToUpperInvariant()}", _mediaDirectory, cancellation);
            ValidateArrangementBasis(basis);
            var id = request.Accept(position, lane, cancellation);
            var clip = _document.Current.State.AudioClips.Single(c => c.Id == id);
            _audioWaveforms = _audioWaveforms.SetItem(clip.SoundId, request.Waveform);
            if (_document.Current.State.Patterns.IsEmpty)
            {
                _songMode = true;
                _coordinator?.SelectArrangement(true);
            }

            _status = "Workflow.Ready";
            return true;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    });

    private async Task LoadAudioWaveformsAsync()
    {
        var document = _document;
        _audioWaveforms = await Task.Run(() =>
        {
            var waves = ImmutableDictionary.CreateBuilder<Id<SoundDefinition>, AudioWaveform>();
            foreach (var sound in document.Current.State.Sounds.Where(s => s.Algorithm == AudioClipSource.Algorithm))
            {
                try
                {
                    waves.Add(sound.Id, AudioMedia.Waveform(document, sound.ResourceIds[0]));
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    /* Open diagnostics own unavailable media. */
                }
            }

            return waves.ToImmutable();
        });
    }

    internal Task<bool> ConfigureAudioClipAsync(ArrangementEditBasis basis, AudioClip clip, Id<MixerChannel>? route)
        => Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            return Task.FromResult(_document.Edit("Configure Playlist audio", edit =>
            {
                edit.UpdateAudioClip(clip);
                if (_document.Current.State.Mixer is not null) edit.AssignMixerSound(clip.SoundId, route);
            }));
        });

    internal Task<bool> CreateAudioMixerChannelAsync(ArrangementEditBasis basis, Id<SoundDefinition> sound)
        => Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            string name = _document.Current.State.Sounds.Single(s => s.Id == sound).Name;
            return Task.FromResult(_document.Edit("Route Playlist audio to new Mixer channel", edit =>
            {
                edit.InitializeMixer();
                var channel = edit.AddMixerBus(name);
                edit.AssignMixerSound(sound, channel);
            }));
        });

    internal Task<bool> EditAudioClipAsync(ArrangementEditBasis basis, AudioClip clip) => Command("Workflow.EditFailed",
        () =>
        {
            ValidateArrangementBasis(basis);
            return Task.FromResult(_document.Edit("Edit Playlist audio", edit => edit.UpdateAudioClip(clip)));
        });

    internal Task<bool> EditAudioSelectionAsync(ArrangementEditBasis basis, IReadOnlyList<Id<AudioClip>> selection,
        long tickDelta = 0, int laneDelta = 0, bool copy = false, bool delete = false, bool duplicate = false)
        => EditPlaylistSelectionAsync(basis, [], selection, tickDelta, laneDelta, copy, delete, duplicate);

    internal Task<bool> SplitAudioClipAsync(ArrangementEditBasis basis, Id<AudioClip> id, MusicalPosition position)
        => Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            return Task.FromResult(_document.Edit("Split Playlist audio", edit => edit.SplitAudioClip(id, position)));
        });
}
