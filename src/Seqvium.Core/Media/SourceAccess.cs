// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public enum SourceAvailability
{
    Supported,
    Unsupported,
    Missing,
    Inaccessible,
    Degraded
}

/// <summary>External path and accepted ResourceId are different source roles, not interchangeable identities.</summary>
public sealed record WavSourceInfo(
    string Name,
    string? ExternalPath,
    Id<ResourceDescriptor>? ResourceId,
    SourceAvailability Availability,
    string Diagnostic,
    int? Rate = null,
    int? Channels = null,
    int? Frames = null);

public sealed record SourceDiscovery(
    ImmutableArray<WavSourceInfo> Sources,
    bool Truncated,
    string? Diagnostic,
    Guid? Lifecycle = null,
    Guid? Revision = null);

/// <summary>Explicit, one-level, bounded access only. No indexing, watcher, cache or project mutation.
/// Returned availability is an observation, not a guarantee for later preview/import.</summary>
public static class SourceAccess
{
    public const int MaximumEntries = 256;

    public static Task<SourceDiscovery> DiscoverDirectoryAsync(string directory, int limit = 64,
        CancellationToken cancellationToken = default)
    {
        CheckLimit(limit);
        string selected = Path.GetFullPath(directory);
        return Task.Run(async () =>
        {
            var result = ImmutableArray.CreateBuilder<WavSourceInfo>();
            int visited = 0;
            try
            {
                // Count all entries, including folders. Never traverse children or directory links.
                foreach (string entry in Directory.EnumerateFileSystemEntries(selected))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (++visited > limit)
                        return new SourceDiscovery(result.ToImmutable(), true,
                            "Selected directory entry limit reached.");
                    try
                    {
                        if ((File.GetAttributes(entry) & FileAttributes.Directory) != 0) continue;
                    }
                    catch (Exception error) when (IsSourceError(error))
                    {
                        result.Add(Failure(Path.GetFileName(entry), entry, null, error));
                        continue;
                    }

                    result.Add(await InspectExternalAsync(entry, cancellationToken).ConfigureAwait(false));
                }

                return new SourceDiscovery(result.ToImmutable(), false, null);
            }
            catch (Exception error) when (IsSourceError(error))
            {
                return new SourceDiscovery(result.ToImmutable(), false, $"Directory unavailable: {error.Message}");
            }
        }, cancellationToken);
    }

    public static async Task<WavSourceInfo> InspectExternalAsync(string path,
        CancellationToken cancellationToken = default)
    {
        string source = Path.GetFullPath(path);
        try
        {
            using var pcm = await DecodeExternalAsync(source, cancellationToken).ConfigureAwait(false);
            return Supported(Path.GetFileName(source), source, null, pcm);
        }
        catch (Exception error) when (IsSourceError(error))
        {
            return Failure(Path.GetFileName(source), source, null, error);
        }
    }

    /// <summary>Capture on the document owner, validate captured managed resources on a worker.
    /// Lifecycle/revision identify the observation; use/reuse must validate current authority separately.</summary>
    public static Task<SourceDiscovery> DiscoverProjectAsync(ProjectDocument document, int limit = 64,
        CancellationToken cancellationToken = default)
    {
        CheckLimit(limit);
        var frozen = Capture(document);
        Guid lifecycle = document.LifecycleId;
        return Task.Run(() =>
        {
            var result = ImmutableArray.CreateBuilder<WavSourceInfo>();
            foreach (var resource in frozen.Current.State.Resources.Take(limit))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var pcm = ProjectMedia.Decode(frozen, resource.Id);
                    result.Add(Supported(resource.Name, null, resource.Id, pcm));
                }
                catch (Exception error) when (IsSourceError(error))
                {
                    result.Add(Failure(resource.Name, null, resource.Id, error));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return new SourceDiscovery(result.ToImmutable(), frozen.Current.State.Resources.Length > limit, null,
                lifecycle, frozen.Current.Revision);
        }, cancellationToken);
    }

    internal static ProjectDocument Capture(ProjectDocument document)
    {
        document.CheckAvailable();
        var frozen = ProjectDocument.Reopen(document.Current);
        frozen.MediaRoots.AddRange(document.MediaRoots);
        return frozen;
    }

    internal static async Task<DecodedPcm> DecodeExternalAsync(string source, CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(source), ".wav", StringComparison.OrdinalIgnoreCase))
            throw new WavFormatException("Only explicitly selected WAV sources are supported.");
        await using var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read,
            8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var bytes = await WavDecoder.ReadBytesAsync(stream, cancellationToken).ConfigureAwait(false);
        return await Task.Run(() => WavDecoder.Decode(bytes), cancellationToken).ConfigureAwait(false);
    }

    private static void CheckLimit(int limit)
    {
        if (limit is < 1 or > MaximumEntries) throw new ArgumentOutOfRangeException(nameof(limit));
    }

    private static WavSourceInfo Supported(string name, string? path, Id<ResourceDescriptor>? resource,
        DecodedPcm pcm) =>
        new(name, path, resource, SourceAvailability.Supported, "Supported bounded WAV.", pcm.SampleRate, pcm.Channels,
            pcm.Frames);

    internal static bool IsSourceError(Exception error) => error is IOException or UnauthorizedAccessException;

    private static WavSourceInfo Failure(string name, string? path, Id<ResourceDescriptor>? resource,
        Exception error) =>
        new(name, path, resource, resource is not null
            ? SourceAvailability.Degraded
            : error switch
            {
                FileNotFoundException or DirectoryNotFoundException => SourceAvailability.Missing,
                WavFormatException => SourceAvailability.Unsupported,
                _ => SourceAvailability.Inaccessible
            }, error.Message);
}
