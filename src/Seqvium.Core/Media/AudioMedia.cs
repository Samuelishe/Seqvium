// SPDX-License-Identifier: Apache-2.0

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Seqvium.Core;

/// <summary>Immutable min/max pyramid. Overview queries retain transients without scanning source PCM on the UI.</summary>
public sealed class AudioWaveform
{
    public static AudioWaveform FromPcm(DecodedPcm pcm, CancellationToken cancellation = default)
    {
        using var lease = pcm.Lease();
        return new(lease.Samples, pcm.Channels, cancellation);
    }
    private readonly (float Min, float Max)[][] _levels;
    public int Channels { get; }
    public int Frames { get; }
    public float Peak { get; }

    internal AudioWaveform(float[] samples, int channels, CancellationToken cancellation = default)
    {
        Channels = channels;
        Frames = samples.Length / channels;
        var levels = new List<(float, float)[]>();
        int count = (Frames + 255) / 256;
        var first = new (float, float)[count * channels];
        for (int b = 0; b < count; b++)
        for (int c = 0; c < channels; c++)
        {
            cancellation.ThrowIfCancellationRequested();
            float min = 0, max = 0;
            for (int f = b * 256; f < Math.Min(Frames, (b + 1) * 256); f++)
            {
                float value = samples[f * channels + c];
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            first[b * channels + c] = (min, max);
        }

        levels.Add(first);
        Peak = first.Max(p => Math.Max(Math.Abs(p.Item1), Math.Abs(p.Item2)));
        while (count > 1)
        {
            int nextCount = (count + 1) / 2;
            var next = new (float, float)[nextCount * channels];
            for (int b = 0; b < nextCount; b++)
            for (int c = 0; c < channels; c++)
            {
                var a = first[b * 2 * channels + c];
                var other = b * 2 + 1 < count ? first[(b * 2 + 1) * channels + c] : a;
                next[b * channels + c] = (Math.Min(a.Item1, other.Item1), Math.Max(a.Item2, other.Item2));
            }

            levels.Add(next);
            first = next;
            count = nextCount;
        }

        _levels = [.. levels];
    }

    public (float Min, float Max) Range(long start, long end, int channel)
    {
        int level = 0;
        long block = 256;
        while (level + 1 < _levels.Length && block * 2 <= Math.Max(1, end - start))
        {
            level++;
            block *= 2;
        }

        int first = (int)Math.Clamp(start / block, 0, _levels[level].Length / Channels - 1);
        int last = (int)Math.Clamp((Math.Max(start + 1, end) - 1) / block, first, _levels[level].Length / Channels - 1);
        float min = 0, max = 0;
        for (int b = first; b <= last; b++)
        {
            var value = _levels[level][b * Channels + channel];
            min = Math.Min(min, value.Min);
            max = Math.Max(max, value.Max);
        }

        return (min, max);
    }
}

/// <summary>Prepared managed audio, kept outside canonical state until owner acceptance.</summary>
public sealed class AudioMediaImport : IDisposable
{
    private readonly ProjectDocument _document;
    private readonly Guid _lifecycle;
    private readonly long _generation;
    private readonly string _path, _locator, _name, _origin;
    private bool _accepted;
    private bool _ended;
    internal CancellationToken PreparationCancellation { get; init; }
    public int SampleRate { get; }
    public int Channels { get; }
    public int Frames { get; }
    public AudioWaveform Waveform { get; }

    internal AudioMediaImport(ProjectDocument document, long generation, string path, string locator,
        string name, string origin, DecodedPcm pcm, AudioWaveform waveform)
    {
        _document = document;
        _lifecycle = document.LifecycleId;
        _generation = generation;
        _path = path;
        _locator = locator;
        _name = name;
        _origin = origin;
        SampleRate = pcm.SampleRate;
        Channels = pcm.Channels;
        Frames = pcm.Frames;
        Waveform = waveform;
    }

    public Id<AudioClip> Accept(MusicalPosition position, int lane, CancellationToken cancellation = default)
    {
        if (PreparationCancellation.IsCancellationRequested || cancellation.IsCancellationRequested)
        {
            _ended = true;
            PreparationCancellation.ThrowIfCancellationRequested();
            cancellation.ThrowIfCancellationRequested();
        }

        _document.CheckAvailable();
        if (_ended || _accepted || _document.LifecycleId != _lifecycle || _document.Generation != _generation)
            throw new InvalidOperationException("Stale audio import.");
        AudioMedia.ValidateFile(_path, _locator);
        Id<AudioClip> clip = default;
        _document.Edit("Import Playlist audio", edit =>
        {
            var resource = edit.AddResource(_name, _locator, _origin);
            var sound = edit.AddSound(_name, AudioClipSource.Algorithm, [resource]);
            edit.SetSoundParameters(sound, ImmutableDictionary<string, decimal>.Empty
                .Add("sampleRate", SampleRate).Add("frames", Frames).Add("channels", Channels));
            clip = edit.AddAudioClip(sound, position, lane, 0, Frames);
            cancellation.ThrowIfCancellationRequested();
        });
        _accepted = true;
        return clip;
    }

    public void Dispose()
    {
        _ended = true;
        if (!_accepted) File.Delete(_path);
    }
}

/// <summary>Portable normalized float WAV. Decode/hash/copy happen on workers; callbacks borrow resident immutable PCM.</summary>
public static partial class AudioMedia
{
    public const int MaximumDecodedBytes = 512 * 1024 * 1024;

    private sealed record CacheEntry(
        long Length,
        long Modified,
        int Rate,
        int Channels,
        WeakReference<float[]> Samples)
    {
        internal string Path { get; init; } = "";
    }

    private static readonly ConditionalWeakTable<ProjectDocument, Dictionary<string, CacheEntry>> Caches = new();

    internal static void ShareCache(ProjectDocument source, ProjectDocument frozen) =>
        Caches.Add(frozen, Caches.GetOrCreateValue(source));

    public static AudioWaveform Waveform(ProjectDocument document, Id<ResourceDescriptor> id)
    {
        using var pcm = Decode(document, id);
        using var lease = pcm.Lease();
        return new(lease.Samples, pcm.Channels);
    }

    [GeneratedRegex("\\Aaudio/[0-9a-f]{32}-([0-9a-f]{64})\\.wav\\z", RegexOptions.CultureInvariant)]
    private static partial Regex LocatorPattern();

    internal static bool IsManaged(ResourceDescriptor resource) =>
        resource.ManagedLocator?.StartsWith("audio/", StringComparison.Ordinal) == true;

    internal static string PathFor(string root, string locator)
    {
        if (!LocatorPattern().IsMatch(locator)) throw new IOException("Unsupported managed audio locator.");
        return Path.Combine(root, locator.Replace('/', Path.DirectorySeparatorChar));
    }

    public static async Task<AudioMediaImport> PrepareImportAsync(ProjectDocument document, string normalizedWave,
        string name, string origin, string? ownedMediaDirectory = null, CancellationToken cancellation = default)
    {
        document.CheckAvailable();
        long generation = document.Generation;
        if (document.UnnamedMediaRoot is null)
        {
            string parent = ownedMediaDirectory ??
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "Seqvium", "ManagedMedia");
            document.UnnamedMediaRoot = Path.Combine(Path.GetFullPath(parent), document.LifecycleId.ToString("N"));
            document.MediaRoots.Add(document.UnnamedMediaRoot);
        }

        string root = document.UnnamedMediaRoot;
        return await Task.Run(() =>
        {
            cancellation.ThrowIfCancellationRequested();
            using var input = File.OpenRead(normalizedWave);
            _ = ReadHeader(input);
            input.Position = 0;
            var hash = Convert.ToHexStringLower(SHA256.HashData(input));
            input.Position = 0;
            string locator = $"audio/{Guid.NewGuid():N}-{hash}.wav";
            string path = PathFor(root, locator);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + ".pending";
            bool ownsTemporary = false;
            try
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    ownsTemporary = true;
                    var block = new byte[65536];
                    int read;
                    while ((read = input.Read(block)) > 0)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        output.Write(block, 0, read);
                    }

                    output.Flush(true);
                }

                cancellation.ThrowIfCancellationRequested();
                using var pcm = DecodeFile(temporary, locator);
                using var lease = pcm.Lease();
                var waveform = new AudioWaveform(lease.Samples, pcm.Channels);
                cancellation.ThrowIfCancellationRequested();
                File.Move(temporary, path);
                ownsTemporary = false;
                return new AudioMediaImport(document, generation, path, locator, name, origin, pcm, waveform)
                    { PreparationCancellation = cancellation };
            }
            finally
            {
                if (ownsTemporary) File.Delete(temporary);
            }
        }, cancellation).ConfigureAwait(false);
    }

    internal static string Resolve(ProjectDocument document, ResourceDescriptor resource)
    {
        foreach (string root in document.MediaRoots.AsEnumerable().Reverse())
        {
            string path = PathFor(root, resource.ManagedLocator!);
            if (!File.Exists(path)) continue;
            ValidateFile(path, resource.ManagedLocator!);
            return path;
        }

        throw new FileNotFoundException($"Managed audio {resource.Id} is missing.");
    }

    public static DecodedPcm Decode(ProjectDocument document, Id<ResourceDescriptor> id)
    {
        document.CheckAvailable();
        var resource = document.Current.State.Resources.Single(r => r.Id == id);
        if (ProjectMedia.IsManagedWav(resource)) return ProjectMedia.Decode(document, id);
        if (!IsManaged(resource)) throw new IOException("Resource is not managed audio.");
        var cache = Caches.GetOrCreateValue(document);
        lock (cache)
        {
            foreach (var root in document.MediaRoots.AsEnumerable().Reverse())
            {
                string path = PathFor(root, resource.ManagedLocator!);
                var info = new FileInfo(path);
                if (!info.Exists) continue;
                if (cache.TryGetValue(resource.ManagedLocator!, out var entry) &&
                    entry.Samples.TryGetTarget(out var samples))
                {
                    if (entry.Path != path || entry.Length != info.Length ||
                        entry.Modified != info.LastWriteTimeUtc.Ticks)
                    {
                        ValidateFile(path, resource.ManagedLocator!);
                        cache[resource.ManagedLocator!] = entry with
                        {
                            Path = path, Length = info.Length, Modified = info.LastWriteTimeUtc.Ticks
                        };
                    }

                    return new(entry.Rate, entry.Channels, samples);
                }

                var pcm = DecodeFile(path, resource.ManagedLocator!);
                using var lease = pcm.Lease();
                cache[resource.ManagedLocator!] = new(info.Length, info.LastWriteTimeUtc.Ticks, pcm.SampleRate,
                    pcm.Channels,
                    new(lease.Samples)) { Path = path };
                return pcm;
            }
        }

        throw new FileNotFoundException($"Managed audio {id} is missing.");
    }

    internal static void ValidateFile(string path, string locator)
    {
        using var input = File.OpenRead(path);
        if (input.Length > MaximumDecodedBytes + 44L || !LocatorPattern().IsMatch(locator) ||
            Convert.ToHexStringLower(SHA256.HashData(input)) != LocatorPattern().Match(locator).Groups[1].Value)
            throw new WavFormatException("Managed audio integrity mismatch or source budget exceeded.");
        input.Position = 0;
        _ = ReadHeader(input);
    }

    private static (int Rate, int Channels, int Samples) ReadHeader(Stream input)
    {
        Span<byte> header = stackalloc byte[44];
        input.ReadExactly(header);
        int channels = BinaryPrimitives.ReadUInt16LittleEndian(header[22..]);
        int rate = BinaryPrimitives.ReadInt32LittleEndian(header[24..]);
        int bytes = BinaryPrimitives.ReadInt32LittleEndian(header[40..]);
        if (!header[..4].SequenceEqual("RIFF"u8) || !header.Slice(8, 8).SequenceEqual("WAVEfmt "u8) ||
            BinaryPrimitives.ReadInt32LittleEndian(header[4..]) != input.Length - 8 ||
            BinaryPrimitives.ReadInt32LittleEndian(header[16..]) != 16 ||
            BinaryPrimitives.ReadUInt16LittleEndian(header[20..]) != 3 || channels is not (1 or 2) ||
            rate is < 8000 or > 192000 || BinaryPrimitives.ReadUInt16LittleEndian(header[34..]) != 32 ||
            BinaryPrimitives.ReadUInt16LittleEndian(header[32..]) != channels * 4 ||
            BinaryPrimitives.ReadInt32LittleEndian(header[28..]) != rate * channels * 4 ||
            !header.Slice(36, 4).SequenceEqual("data"u8) || bytes <= 0 || bytes % (channels * 4) != 0 ||
            bytes > MaximumDecodedBytes || input.Length != 44L + bytes)
            throw new WavFormatException("Invalid normalized float WAV.");
        return (rate, channels, bytes / 4);
    }

    internal static DecodedPcm DecodeFile(string path, string locator)
    {
        ValidateFile(path, locator);
        return ReadTemporaryWave(path);
    }

    /// <summary>Reads adapter-normalized transient float WAV without accepting project media.
    /// The caller owns the temporary file. Run outside UI/realtime; header, budget and PCM are validated.</summary>
    public static DecodedPcm ReadTemporaryWave(string path, CancellationToken cancellation = default)
    {
        using var input = File.OpenRead(path);
        var header = ReadHeader(input);
        var samples = new float[header.Samples];
        // Chunked I/O avoids simultaneously retaining compressed bytes and complete decoded PCM.
        for (int offset = 0; offset < samples.Length; offset += 16384)
        {
            cancellation.ThrowIfCancellationRequested();
            var target = samples.AsSpan(offset, Math.Min(16384, samples.Length - offset));
            input.ReadExactly(MemoryMarshal.AsBytes(target));
            foreach (float sample in target)
                if (!float.IsFinite(sample))
                    throw new WavFormatException("Audio contains nonfinite samples.");
        }

        return new(header.Rate, header.Channels, samples);
    }

    public static async Task<AudioResourceReuse> PrepareReuseAsync(ProjectDocument document,
        Id<ResourceDescriptor> resource, CancellationToken cancellation = default)
    {
        var frozen = SourceAccess.Capture(document);
        long generation = document.Generation;
        return await Task.Run(() =>
        {
            cancellation.ThrowIfCancellationRequested();
            using var pcm = Decode(frozen, resource);
            using var lease = pcm.Lease();
            var waveform = new AudioWaveform(lease.Samples, pcm.Channels);
            cancellation.ThrowIfCancellationRequested();
            return new AudioResourceReuse(document, generation, resource, pcm, waveform, cancellation);
        }, cancellation).ConfigureAwait(false);
    }

    internal static void CopyForSave(ProjectDocument document, ResourceDescriptor resource, string root)
    {
        string source = Resolve(document, resource), target = PathFor(root, resource.ManagedLocator!);
        if (File.Exists(target))
        {
            ValidateFile(target, resource.ManagedLocator!);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temporary = target + $".{Guid.NewGuid():N}.pending";
        bool created = false;
        try
        {
            using (var input = File.OpenRead(source))
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                input.CopyTo(output);
                output.Flush(true);
            }

            ValidateFile(temporary, resource.ManagedLocator!);
            File.Move(temporary, target);
        }
        finally
        {
            if (created && File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
