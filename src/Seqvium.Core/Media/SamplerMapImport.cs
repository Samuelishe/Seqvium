// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Atomic multi-WAV acceptance, borrowing the existing durable WAV preparation and rollback owner.</summary>
public sealed class SamplerMapImport : IDisposable
{
    private readonly ProjectDocument _document;
    private readonly SoundDefinition _sound;
    private readonly long _generation;
    private readonly List<(WavImport Import, decimal Root)> _candidates = [];
    private readonly string? _mediaDirectory;
    private bool _ended, _failed;
    private long _decodedBytes;
    private bool _countedExisting;
    private CancellationToken _cancellation;

    public SamplerMapImport(ProjectDocument document, Id<SoundDefinition> sound, string? ownedMediaDirectory = null)
    {
        document.CheckAvailable();
        _document = document;
        _generation = document.Generation;
        _sound = document.Current.State.Sounds.Single(s => s.Id == sound);
        if (!SamplerPlayback.Supports(_sound)) throw new ArgumentException("Unsupported key map target.");
        _mediaDirectory = ownedMediaDirectory;
    }

    public async Task PrepareAsync(string path, decimal confirmedRoot, CancellationToken cancellation = default)
    {
        if (_ended || _failed) throw new InvalidOperationException("Import ended.");
        PcmSampler.ValidateConfiguration(confirmedRoot, 0);
        if (_candidates.Count + SamplerKeymap.Zones(_sound).Length >= SamplerKeymap.MaximumZones)
            throw new ArgumentException("Key map capacity exceeded.");
        _cancellation = cancellation;
        var candidate = ProjectMedia.BeginImport(_document, Path.GetFileNameWithoutExtension(path), confirmedRoot,
            ownedMediaDirectory: _mediaDirectory);
        try
        {
            if (!_countedExisting)
            {
                _decodedBytes = await Task.Run(() =>
                {
                    long total = 0;
                    foreach (var resource in _sound.ResourceIds)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        using var existing = ProjectMedia.Decode(_document, resource);
                        total += (long)existing.Frames * existing.Channels * sizeof(float);
                    }

                    return total;
                }, cancellation).ConfigureAwait(false);
                _countedExisting = true;
            }

            await using var file = File.OpenRead(path);
            var bytes = await WavDecoder.ReadBytesAsync(file, cancellation).ConfigureAwait(false);
            using var pcm = await Task.Run(() => WavDecoder.Decode(bytes), cancellation).ConfigureAwait(false);
            _decodedBytes += (long)pcm.Frames * pcm.Channels * sizeof(float);
            if (_decodedBytes > SamplerPreparation.MaximumDecodedBytes)
                throw new ArgumentException("Key map PCM budget exceeded.");
            using var stream = new MemoryStream(bytes, false);
            await candidate.PrepareAsync(stream, cancellation).ConfigureAwait(false);
            _candidates.Add((candidate, confirmedRoot));
        }
        catch
        {
            _failed = true;
            candidate.Dispose();
            throw;
        }
    }

    public void Accept(CancellationToken cancellation = default)
    {
        try
        {
            if (_ended || _failed || _candidates.Count == 0)
                throw new InvalidOperationException("Import is not prepared.");
            _cancellation.ThrowIfCancellationRequested();
            cancellation.ThrowIfCancellationRequested();
            _document.CheckAvailable();
            if (_document.Generation != _generation) throw new InvalidOperationException("Stale key map import.");
            foreach (var c in _candidates) c.Import.ValidateCandidate(cancellation);
            _document.Edit("Add sampler sources and distribute keys", edit =>
            {
                var zones = SamplerKeymap.Zones(_sound).ToBuilder();
                foreach (var c in _candidates)
                {
                    c.Import.CheckCandidateCancellation(cancellation);
                    zones.Add(new(Id<SamplerZone>.New(), c.Import.AddCandidate(edit), c.Root, 0, 127,
                        ImmutableDictionary<string, decimal>.Empty));
                }

                edit.SetSamplerZones(_sound.Id, SamplerKeymap.Distribute(zones));
                foreach (var c in _candidates) c.Import.CheckCandidateCancellation(cancellation);
            });
            foreach (var c in _candidates) c.Import.CompleteCandidate();
            _ended = true;
        }
        catch (OperationCanceledException)
        {
            _ended = true;
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var c in _candidates) c.Import.Dispose();
        _ended = true;
    }
}