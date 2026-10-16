// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

/// <summary>Single-use project-bound pitched audition on an externally owned renderer/output.
/// Serialize control calls, await preparation, and join output before renderer disposal.</summary>
public sealed class NoteAuditionRequest : IDisposable
{
    private readonly RealtimeSampler _consumer;
    private readonly ProjectDocument _document, _frozen;
    private readonly Id<ResourceDescriptor> _resource;
    private readonly decimal _root, _pitch, _intensity;
    private readonly bool _held;
    private readonly Id<SoundDefinition>? _sound;
    private readonly Id<SamplerZone>? _previewZone;
    private readonly long _generation, _authority;
    private PreparedSampler? _plan;
    private bool _started, _ended, _reserved;
    private CancellationToken _cancellation;

    public NoteAuditionRequest(RealtimeSampler consumer, ProjectDocument document, Id<ResourceDescriptor> resource,
        decimal rootPitch, decimal pitch, decimal intensity)
    {
        _consumer = consumer;
        _document = document;
        _resource = resource;
        _root = rootPitch;
        _pitch = pitch;
        _intensity = intensity;
        _frozen = SourceAccess.Capture(document);
        _generation = document.Generation;
        consumer.Stop();
        _authority = consumer.InvalidatePreparation();
    }

    public async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_started || _ended) throw new InvalidOperationException("Audition preparation is single-use.");
        _started = true;
        _cancellation = cancellation;
        if (!_consumer.TryBeginWork()) throw new InvalidOperationException("Audition capacity is occupied.");
        _reserved = true;
        try
        {
            _plan = await Task.Run(() =>
            {
                cancellation.ThrowIfCancellationRequested();
                var sound = _sound is { } id ? _frozen.Current.State.Sounds.Single(s => s.Id == id) : null;
                if (sound is not null && _previewZone is { } zoneId)
                    sound = SamplerKeymap.Source(sound, SamplerKeymap.Zones(sound).Single(z => z.Id == zoneId));
                if (sound is not null && SynthVoice.Supports(sound))
                    return SamplerPreparation.PrepareSynthAudition(sound, _consumer.SampleRate, _consumer.Channels,
                        _pitch, _intensity, _held);
                if (sound?.Algorithm == PcmSampler.Algorithm)
                    sound = SamplerKeymap.Select(sound, _pitch) ??
                            throw new ArgumentException("No sample on this key.");
                using var pcm = sound is null ? ProjectMedia.Decode(_frozen, _resource)
                    : sound.Algorithm == PcmSampler.Algorithm ? ProjectMedia.Decode(_frozen, sound.ResourceIds.Single())
                    : DrumVoice.Render(sound, _consumer.SampleRate, _pitch, cancellation);
                if (sound?.Algorithm == PcmSampler.Algorithm)
                    return SamplerPreparation.PrepareSamplerAudition(pcm, sound, _consumer.SampleRate,
                        _consumer.Channels, _pitch, _intensity, _held);
                return SamplerPreparation.PrepareNoteAudition(pcm, _consumer.SampleRate, _consumer.Channels, _root,
                    sound is null ? _pitch : _root, _intensity);
            }, cancellation).ConfigureAwait(false);
        }
        catch
        {
            Release();
            throw;
        }
    }

    public NoteAuditionRequest(RealtimeSampler consumer, ProjectDocument document, Id<SoundDefinition> drumSound,
        decimal pitch, decimal intensity, bool held = false) : this(consumer, document, default(Id<ResourceDescriptor>),
        DrumVoice.RootPitch, pitch, intensity)
    {
        var sound = _frozen.Current.State.Sounds.Single(s => s.Id == drumSound);
        if (!BuiltInSound.Supports(sound)) throw new NotSupportedException("Unsupported sound audition.");
        _held = held;
        _sound = drumSound;
    }

    public SamplerPublication Publish()
    {
        if (_ended || _plan is null) throw new InvalidOperationException("No prepared note audition.");
        bool valid = !_document.IsClosed && _document.Generation == _generation &&
                     _document.Current.Revision == _frozen.Current.Revision;
        var result = _consumer.Publish(_plan, _authority, valid, _cancellation.IsCancellationRequested, true,
            _cancellation);
        if (result != SamplerPublication.Accepted) _plan.Dispose();
        _plan = null;
        _ended = true;
        Release();
        if (result == SamplerPublication.Accepted) _consumer.StartPrepared(_authority);
        return result;
    }

    public NoteAuditionRequest(RealtimeSampler consumer, ProjectDocument document, Id<SoundDefinition> soundId,
        Id<SamplerZone> zoneId, decimal intensity) : this(consumer, document, soundId,
        SamplerKeymap.Zones(document.Current.State.Sounds.Single(s => s.Id == soundId)).Single(z => z.Id == zoneId)
            .RootPitch,
        intensity, held: true) => _previewZone = zoneId;

    private void Release()
    {
        if (_reserved)
        {
            _reserved = false;
            _consumer.EndWork();
        }
    }

    public void Dispose()
    {
        _plan?.Dispose();
        _plan = null;
        _ended = true;
        Release();
    }
}