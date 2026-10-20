// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using System.Collections.Immutable;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    internal async Task<BrowserAudioInfo> InspectSamplerZoneAsync(NodeEditorTarget target, long generation,
        Id<SamplerZone> zoneId, CancellationToken cancellation)
    {
        var pending = await _owner.Run(() =>
        {
            var sound = SamplerSource(target, generation);
            var zone = SamplerKeymap.Zones(sound).Single(z => z.Id == zoneId);
            return Task.FromResult(SourceAccess.ReadResourceAsync(_document, zone.ResourceId, cancellation));
        });
        using var pcm = await pending.ConfigureAwait(false);
        return await Task.Run(() => new BrowserAudioInfo(pcm.SampleRate, pcm.Channels, pcm.Frames,
            AudioWaveform.ForSampler(pcm, cancellation)), cancellation).ConfigureAwait(false);
    }

    internal Task<bool> SetSamplerZonePlaybackAsync(NodeEditorTarget target, long generation, Id<SamplerZone> zoneId,
        SamplerPlayback playback) => Command("Workflow.EditFailed", async () =>
    {
        var sound = SamplerSource(target, generation);
        var zone = SamplerKeymap.Zones(sound).Single(z => z.Id == zoneId);
        using var pcm = await SourceAccess.ReadResourceAsync(_document, zone.ResourceId);
        var parameters = playback.Apply(SamplerKeymap.Source(sound, zone), pcm.Frames);
        SamplerSource(target, generation);
        await CloseNoteAuditionAsync();
        SamplerSource(target, generation);
        return _document.Edit("Set sampler source playback", edit =>
        {
            if (!SamplerKeymap.HasIntent(sound)) edit.SetSoundParameters(sound.Id, parameters);
            else
                edit.SetSamplerZones(sound.Id, [
                    .. sound.SamplerZones.Select(z => z.Id == zoneId
                        ? z with
                        {
                            Parameters = parameters.Remove("rootPitch").Remove("releaseMilliseconds")
                        }
                        : z)
                ]);
        });
    });

    internal Task<bool> SetSamplerMapAsync(NodeEditorTarget target, long generation,
        System.Collections.Immutable.ImmutableArray<SamplerZone> zones) => Command("Workflow.EditFailed", async () =>
    {
        var sound = SamplerSource(target, generation);
        await CloseNoteAuditionAsync();
        SamplerSource(target, generation);
        return _document.Edit("Edit sampler key mapping", edit => edit.SetSamplerZones(sound.Id, zones));
    });

    internal Task<bool> AddSamplerSourcesAsync(NodeEditorTarget target, long generation,
        IReadOnlyList<(string Path, decimal Root)> sources, CancellationToken cancellation = default)
        => AddSamplerSourcesAsync(target, generation, sources, default, cancellation);

    internal Task<bool> AddSamplerSourcesAsync(NodeEditorTarget target, long generation,
        IReadOnlyList<(string Path, decimal Root)> sources, ImmutableArray<SamplerZone> reviewed,
        CancellationToken cancellation = default)
        => Command("Workflow.ImportFailed", async () =>
        {
            var sound = SamplerSource(target, generation);
            using var import = new SamplerMapImport(_document, sound.Id, _mediaDirectory);
            foreach (var source in sources) await import.PrepareAsync(source.Path, source.Root, cancellation);
            SamplerSource(target, generation);
            await CloseNoteAuditionAsync();
            SamplerSource(target, generation);
            if (reviewed.IsDefault) import.Accept(cancellation);
            else
            {
                var prepared = import.DraftZones;
                int existing = SamplerKeymap.Zones(sound).Length;
                if (reviewed.Length != prepared.Length ||
                    reviewed.Take(existing).Where((z, i) => z.Id != prepared[i].Id ||
                        z.ResourceId != prepared[i].ResourceId).Any())
                    throw new ArgumentException("Incomplete or reordered mapping review.");
                import.Accept([.. reviewed.Select((z, i) => z with
                {
                    Id = prepared[i].Id, ResourceId = prepared[i].ResourceId
                })], cancellation);
            }
            return true;
        });

    internal Task<bool> HoldSamplerZoneAsync(NodeEditorTarget target, long generation, Id<SamplerZone> zoneId)
        => HoldSamplerZoneAsync(target, generation, zoneId, .8m, false);

    internal Task<bool> HoldSamplerZoneAsync(NodeEditorTarget target, long generation, Id<SamplerZone> zoneId,
        decimal intensity, bool instrument, decimal? pitch = null)
        => Command("Workflow.ActionFailed", async () =>
        {
            var sound = SamplerSource(target, generation);
            if (_coordinator?.ReadStatus().Execution.Playing == true)
                throw new InvalidOperationException("Browser.StopTransport");
            await CloseNoteAuditionAsync();
            long transport = _playGeneration;
            if (instrument && SamplerKeymap.Select(sound,
                    pitch ?? SamplerKeymap.Zones(sound).Single(z => z.Id == zoneId).RootPitch, intensity) is null)
                throw new InvalidOperationException("Sampler.RangeUnavailable");
            _noteOutput = _openOutput();
            try
            {
                _noteSession = new();
                _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
                _noteOutput.Sampler.SetGain(.12f);
                using var request = instrument
                    ? new NoteAuditionRequest(_noteOutput.Sampler, _document, sound.Id,
                        pitch ?? SamplerKeymap.Zones(sound).Single(z => z.Id == zoneId).RootPitch, intensity, held: true)
                    : new NoteAuditionRequest(_noteOutput.Sampler, _document, sound.Id, zoneId, intensity);
                await request.PrepareAsync();
                SamplerSource(target, generation);
                if (transport != _playGeneration || request.Publish() != SamplerPublication.Accepted)
                    throw new InvalidOperationException("Piano.AuditionUnavailable");
                _samplerAudition = (target, generation);
                return true;
            }
            catch
            {
                await CloseNoteAuditionAsync();
                throw;
            }
        });

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

    internal Task<bool> HoldSamplerAsync(NodeEditorTarget target, long generation) => Command("Workflow.ActionFailed",
        async () =>
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
            catch
            {
                await CloseNoteAuditionAsync();
                throw;
            }
        });

    private (NodeEditorTarget Target, long Generation)? _samplerAudition;

    internal bool SamplerPreviewPlaying =>
        _samplerAudition is not null && _noteOutput?.Sampler.ReadStatus().Playing == true;

    internal Task<bool> ReleaseSamplerAsync() => _owner.Run(() =>
    {
        if (_samplerAudition is null) return Task.FromResult(false);
        if (_synthAudition is { } synth) Volatile.Write(ref _synthAudition, synth with { Held = false });
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
