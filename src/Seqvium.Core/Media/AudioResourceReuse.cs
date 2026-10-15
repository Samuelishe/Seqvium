// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Validated independent Audio Clip use of accepted audio. No media copy or resource identity change.</summary>
public sealed class AudioResourceReuse : IDisposable
{
    private readonly ProjectDocument _document;
    private readonly long _generation;
    private readonly Id<ResourceDescriptor> _resource;
    private readonly CancellationToken _preparationCancellation;
    private bool _ended;
    public int SampleRate { get; }
    public int Channels { get; }
    public int Frames { get; }
    public AudioWaveform Waveform { get; }

    internal AudioResourceReuse(ProjectDocument document, long generation, Id<ResourceDescriptor> resource,
        DecodedPcm pcm, AudioWaveform waveform, CancellationToken cancellation)
    {
        _document = document;
        _generation = generation;
        _resource = resource;
        _preparationCancellation = cancellation;
        SampleRate = pcm.SampleRate;
        Channels = pcm.Channels;
        Frames = pcm.Frames;
        Waveform = waveform;
    }

    public Id<AudioClip> Accept(MusicalPosition position, int lane, CancellationToken cancellation = default)
    {
        _preparationCancellation.ThrowIfCancellationRequested();
        cancellation.ThrowIfCancellationRequested();
        _document.CheckAvailable();
        if (_ended || _document.Generation != _generation) throw new InvalidOperationException("Stale audio reuse.");
        using (AudioMedia.Decode(_document, _resource)) { }
        var resource = _document.Current.State.Resources.Single(r => r.Id == _resource);
        Id<AudioClip> clip = default;
        _document.Edit("Reuse project resource in Playlist", edit =>
        {
            var sound = edit.AddSound(resource.Name, AudioClipSource.Algorithm, [_resource]);
            edit.SetSoundParameters(sound, ImmutableDictionary<string, decimal>.Empty
                .Add("sampleRate", SampleRate).Add("frames", Frames).Add("channels", Channels));
            clip = edit.AddAudioClip(sound, position, lane, 0, Frames);
            _preparationCancellation.ThrowIfCancellationRequested();
            cancellation.ThrowIfCancellationRequested();
        });
        _ended = true;
        return clip;
    }

    public void Dispose() => _ended = true;
}
