// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Seqvium.Core;

/// <summary>Atomic multi-WAV acceptance, borrowing the existing durable WAV preparation and rollback owner.</summary>
public sealed class SamplerMapImport : IDisposable
{
    private readonly ProjectDocument _document;
    private readonly SoundDefinition _sound;
    private readonly long _generation;
    private readonly List<SamplerZone> _candidates = [];
    private readonly Dictionary<Id<ResourceDescriptor>, WavImport> _imports = [];
    private readonly Dictionary<string, Id<ResourceDescriptor>> _hashes = [];
    private readonly HashSet<Id<ResourceDescriptor>> _existingUsed = [];
    private readonly string? _mediaDirectory;
    private bool _ended, _failed;
    private long _decodedBytes;
    private bool _countedExisting;
    private CancellationToken _cancellation;
    private readonly List<CancellationToken> _tokens = [];

    public ImmutableArray<SamplerZone> DraftZones => [.. SamplerKeymap.Zones(_sound), .. _candidates];

    public SamplerMapImport(ProjectDocument document, Id<SoundDefinition> sound, string? ownedMediaDirectory = null)
    {
        document.CheckAvailable();
        _document = document;
        _generation = document.Generation;
        _sound = document.Current.State.Sounds.Single(s => s.Id == sound);
        if (!SamplerPlayback.Supports(_sound)) throw new ArgumentException("Unsupported key map target.");
        _mediaDirectory = ownedMediaDirectory;
        foreach (var resource in document.Current.State.Resources.Where(ProjectMedia.IsManagedWav))
            if (resource.ManagedLocator is { Length: >= 68 } locator)
                _hashes.TryAdd(locator[^68..^4], resource.Id);
    }

    public async Task PrepareAsync(string path, decimal confirmedRoot, CancellationToken cancellation = default)
    {
        if (_ended || _failed) throw new InvalidOperationException("Import ended.");
        PcmSampler.ValidateConfiguration(confirmedRoot, 0);
        if (_candidates.Count + SamplerKeymap.Zones(_sound).Length >= SamplerKeymap.MaximumZones)
            throw new ArgumentException("Key map capacity exceeded.");
        _cancellation = cancellation;
        WavImport? candidate = null;
        try
        {
            if (!_countedExisting)
            {
                _decodedBytes = await Task.Run(() =>
                {
                    long total = 0;
                    foreach (var resource in _sound.ResourceIds.Distinct())
                    {
                        cancellation.ThrowIfCancellationRequested();
                        using var existing = ProjectMedia.Decode(_document, resource);
                        total += (long)existing.Frames * existing.Channels * sizeof(float);
                        _existingUsed.Add(resource);
                        var descriptor = _document.Current.State.Resources.Single(r => r.Id == resource);
                        string locator = descriptor.ManagedLocator!;
                        _hashes.TryAdd(locator[^68..^4], resource);
                    }

                    return total;
                }, cancellation).ConfigureAwait(false);
                if (_decodedBytes > SamplerPreparation.MaximumDecodedBytes)
                    throw new ArgumentException("Key map PCM budget exceeded.");
                _countedExisting = true;
            }

            await using var file = File.OpenRead(path);
            var bytes = await WavDecoder.ReadBytesAsync(file, cancellation).ConfigureAwait(false);
            string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (_hashes.TryGetValue(hash, out var reused) && !_imports.ContainsKey(reused) && _existingUsed.Add(reused))
            {
                using var existing = await Task.Run(() => ProjectMedia.Decode(_document, reused), cancellation)
                    .ConfigureAwait(false);
                _decodedBytes += (long)existing.Frames * existing.Channels * sizeof(float);
                if (_decodedBytes > SamplerPreparation.MaximumDecodedBytes)
                    throw new ArgumentException("Key map PCM budget exceeded.");
            }
            if (!_hashes.TryGetValue(hash, out var resource))
            {
                using var pcm = await Task.Run(() => WavDecoder.Decode(bytes), cancellation).ConfigureAwait(false);
                _decodedBytes += (long)pcm.Frames * pcm.Channels * sizeof(float);
                if (_decodedBytes > SamplerPreparation.MaximumDecodedBytes)
                    throw new ArgumentException("Key map PCM budget exceeded.");
                candidate = ProjectMedia.BeginImport(_document, Path.GetFileNameWithoutExtension(path), confirmedRoot,
                    ownedMediaDirectory: _mediaDirectory);
                using var stream = new MemoryStream(bytes, false);
                await candidate.PrepareAsync(stream, cancellation).ConfigureAwait(false);
                resource = Id<ResourceDescriptor>.New(); // Draft identity replaced only inside acceptance.
                _imports.Add(resource, candidate);
                _hashes.Add(hash, resource);
            }
            _candidates.Add(new(Id<SamplerZone>.New(), resource, confirmedRoot, 0, 127,
                ImmutableDictionary<string, decimal>.Empty));
            _tokens.Add(cancellation);
        }
        catch
        {
            _failed = true;
            candidate?.Dispose();
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
            foreach (var token in _tokens) token.ThrowIfCancellationRequested();
            cancellation.ThrowIfCancellationRequested();
            Accept(SamplerKeymap.Distribute(DraftZones), cancellation);
        }
        catch (OperationCanceledException)
        {
            _ended = true;
            throw;
        }
    }

    /// <summary>Accept the complete reviewed map in one edit, preserving existing and candidate zone identities.</summary>
    public void Accept(ImmutableArray<SamplerZone> zones, CancellationToken cancellation = default)
    {
        try
        {
            if (_ended || _failed || _candidates.Count == 0)
                throw new InvalidOperationException("Import is not prepared.");
            _cancellation.ThrowIfCancellationRequested();
            foreach (var token in _tokens) token.ThrowIfCancellationRequested();
            cancellation.ThrowIfCancellationRequested();
            _document.CheckAvailable();
            if (_document.Generation != _generation) throw new InvalidOperationException("Stale key map import.");
            var draft = DraftZones;
            if (zones.IsDefault || zones.Length != draft.Length ||
                !zones.Select(z => z.Id).ToHashSet().SetEquals(draft.Select(z => z.Id)) ||
                zones.Any(z => draft.Single(d => d.Id == z.Id) is var original &&
                    (z.ResourceId != original.ResourceId || !z.Parameters.SequenceEqual(original.Parameters) ||
                     !z.AdditionalData.IsEmpty)))
                throw new ArgumentException("Reviewed map must retain the prepared sources and zone identities.");
            SamplerKeymap.Validate(_sound with
            {
                SamplerZones = zones, ResourceIds = [.. zones.Select(z => z.ResourceId).Distinct()],
                Parameters = _sound.Parameters.RemoveRange(SamplerPlayback.Keys)
            });
            foreach (var c in _imports.Values) c.ValidateCandidate(cancellation);
            foreach (var resource in _existingUsed)
                using (ProjectMedia.Decode(_document, resource)) { }
            _document.Edit("Add sampler sources and mapping", edit =>
            {
                var resources = new Dictionary<Id<ResourceDescriptor>, Id<ResourceDescriptor>>();
                foreach (var (id, c) in _imports)
                {
                    c.CheckCandidateCancellation(cancellation);
                    resources.Add(id, c.AddCandidate(edit));
                }

                edit.SetSamplerZones(_sound.Id, [.. zones.Select(z => resources.TryGetValue(z.ResourceId, out var id)
                    ? z with { ResourceId = id } : z)]);
                _cancellation.ThrowIfCancellationRequested();
                foreach (var token in _tokens) token.ThrowIfCancellationRequested();
                cancellation.ThrowIfCancellationRequested();
                foreach (var c in _imports.Values) c.CheckCandidateCancellation(cancellation);
            });
            foreach (var c in _imports.Values) c.CompleteCandidate();
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
        foreach (var c in _imports.Values) c.Dispose();
        _ended = true;
    }
}
