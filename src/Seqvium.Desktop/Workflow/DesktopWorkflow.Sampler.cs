// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private SoundDefinition SamplerSource(NodeEditorTarget target, long generation)
    {
        if (_document.LifecycleId != target.LifecycleId || _document.Generation != generation ||
            DrumEditorSession.Resolve(_document.Current.State, target) is not { } sound ||
            !SamplerPlayback.Supports(sound)) throw new InvalidOperationException("Workflow.EditStale");
        return sound;
    }

    internal async Task<BrowserAudioInfo> InspectSamplerAsync(NodeEditorTarget target, long generation,
        CancellationToken cancellation)
    {
        var pending = await _owner.Run(() => Task.FromResult(SourceAccess.ReadResourceAsync(_document,
            SamplerSource(target, generation).ResourceIds.Single(), cancellation)));
        using var pcm = await pending.ConfigureAwait(false);
        return await Task.Run(() => new BrowserAudioInfo(pcm.SampleRate, pcm.Channels, pcm.Frames,
            AudioWaveform.ForSampler(pcm, cancellation)), cancellation).ConfigureAwait(false);
    }

    internal Task<bool> SetSamplerPlaybackAsync(NodeEditorTarget target, long generation, SamplerPlayback playback)
        => Command("Workflow.EditFailed", async () =>
        {
            var sound = SamplerSource(target, generation);
            using var pcm = await SourceAccess.ReadResourceAsync(_document, sound.ResourceIds.Single());
            var parameters = playback.Apply(sound, pcm.Frames);
            SamplerSource(target, generation);
            await CloseNoteAuditionAsync();
            SamplerSource(target, generation);
            bool changed = _document.Edit("Set sampler range and sustain loop",
                edit => edit.SetSoundParameters(sound.Id, parameters));
            _status = "Workflow.Ready";
            return changed;
        });

    internal Task<bool> HoldSamplerAsync(NodeEditorTarget target, long generation) => Command("Workflow.ActionFailed", async () =>
    {
        var sound = SamplerSource(target, generation);
        if (_coordinator?.ReadStatus().Execution.Playing == true)
            throw new InvalidOperationException("Browser.StopTransport");
        await CloseNoteAuditionAsync();
        long transport = _playGeneration;
        _noteOutput = _openOutput();
        try
        {
            _noteSession = new();
            _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
            _noteOutput.Sampler.SetGain(.12f);
            using var request = new NoteAuditionRequest(_noteOutput.Sampler, _document, sound.Id,
                sound.Parameters["rootPitch"], .8m, held: true);
            await request.PrepareAsync();
            SamplerSource(target, generation);
            if (transport != _playGeneration || request.Publish() != SamplerPublication.Accepted)
                throw new InvalidOperationException("Piano.AuditionUnavailable");
            _samplerAudition = (target, generation);
            return true;
        }
        catch { await CloseNoteAuditionAsync(); throw; }
    });

    private (NodeEditorTarget Target, long Generation)? _samplerAudition;
    internal bool SamplerPreviewPlaying => _samplerAudition is not null && _noteOutput?.Sampler.ReadStatus().Playing == true;
    internal Task<bool> ReleaseSamplerAsync() => _owner.Run(() =>
    {
        if (_samplerAudition is null) return Task.FromResult(false);
        _noteOutput?.Sampler.ReleaseAudition();
        return Task.FromResult(true);
    });
    internal Task<bool> StopSamplerAsync() => _owner.Run(async () =>
    {
        if (_samplerAudition is null) return false;
        await CloseNoteAuditionAsync();
        return true;
    });
}
