// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Seqvium.Core;

public sealed record MediaAcceptance(Id<ResourceDescriptor> ResourceId, Id<SoundDefinition> SoundId);

/// <summary>Single-use preparation. Begin/Accept/Dispose belong to the document owner; only preparation runs asynchronously.</summary>
public sealed class WavImport : IDisposable
{
    private readonly ProjectDocument _document;
    private readonly Guid _lifecycle;
    private readonly long _generation;
    private readonly Id<ProjectState> _project;
    private readonly Id<SoundDefinition>? _target;
    private readonly string _name;
    private readonly decimal _rootPitch, _releaseMilliseconds;
    private readonly string _root;
    private string? _locator;
    private bool _started, _finished, _accepted;
    private CancellationToken _preparationCancellation;

    internal WavImport(ProjectDocument document, string name, Id<SoundDefinition>? target,
        decimal rootPitch, decimal releaseMilliseconds, string root)
    {
        _document = document;
        _name = name;
        _target = target;
        _root = root;
        _rootPitch = rootPitch;
        _releaseMilliseconds = releaseMilliseconds;
        _lifecycle = document.LifecycleId;
        _generation = document.Generation;
        _project = document.Current.State.Id;
    }

    public async Task PrepareAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await PrepareAsync(source, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The caller owns source and must await completion before disposing or accepting the request.</summary>
    public async Task PrepareAsync(Stream source, CancellationToken cancellationToken = default)
    {
        if (_started || _finished) throw new InvalidOperationException("Import preparation is single-use.");
        _started = true;
        _preparationCancellation = cancellationToken;
        string? temporary = null;
        bool ownsTemporary = false;
        try
        {
            var bytes = await WavDecoder.ReadBytesAsync(source, cancellationToken).ConfigureAwait(false);
            await Task.Run(() =>
            {
                using (WavDecoder.Decode(bytes))
                {
                }
            }, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            string locator = $"wav/{Guid.NewGuid():N}-{Convert.ToHexStringLower(SHA256.HashData(bytes))}.wav";
            var path = ProjectMedia.PathFor(_root, locator);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            temporary = path + ".pending";
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             8192, FileOptions.Asynchronous))
            {
                ownsTemporary = true;
                await output.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                output.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path);
            ownsTemporary = false;
            temporary = null;
            _locator = locator;
        }
        finally
        {
            if (ownsTemporary && temporary is not null) File.Delete(temporary);
        }
    }

    /// <summary>Conservative generation gate rejects even unrelated intervening edits/Undo/Redo. No implicit rebase.</summary>
    public MediaAcceptance Accept(CancellationToken cancellationToken = default)
        => Accept(null, cancellationToken);

    /// <summary>Optional musical use is validated in the same owning transaction as resource acceptance.
    /// A failed callback accepts neither descriptors nor use; the request still owns the unaccepted file.</summary>
    public MediaAcceptance Accept(Action<ProjectEdit, MediaAcceptance>? createUse,
        CancellationToken cancellationToken = default)
    {
        if (_finished || _locator is null) throw new InvalidOperationException("Import is not prepared or has ended.");
        CheckCancellation(cancellationToken);
        _document.CheckAvailable();
        if (_document.LifecycleId != _lifecycle || _document.Generation != _generation ||
            _document.Current.State.Id != _project ||
            _target is { } target && !_document.Current.State.Sounds.Any(sound => sound.Id == target))
            throw new InvalidOperationException("Stale import; document lifecycle, target or dependencies changed.");
        // Storage may have changed after preparation. Validation precedes the owning edit, never follows it.
        using (ProjectMedia.DecodePath(ProjectMedia.PathFor(_root, _locator), _locator))
        {
        }

        CheckCancellation(cancellationToken);
        Id<ResourceDescriptor> resource = default;
        Id<SoundDefinition> sound = default;
        _document.Edit("Import project WAV", edit =>
        {
            CheckCancellation(cancellationToken);
            resource = edit.AddResource(_name, _locator, "Imported WAV");
            sound = _target ?? edit.AddSound(_name, PcmSampler.Algorithm, [resource]);
            edit.ConfigurePcmSampler(sound, resource, _rootPitch, _releaseMilliseconds);
            createUse?.Invoke(edit, new(resource, sound));
            CheckCancellation(cancellationToken);
        });
        _accepted = true;
        _finished = true;
        return new(resource, sound);
    }

    private void CheckCancellation(CancellationToken cancellationToken)
    {
        if (_preparationCancellation.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            _finished = true; // Withdrawing this operation is permanent, even if a later caller passes another token.
            _preparationCancellation.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public void Dispose()
    {
        if (!_accepted && _locator is { } locator)
        {
            File.Delete(ProjectMedia.PathFor(_root, locator));
            _locator = null;
        }

        _finished = true;
    }
}

/// <summary>Project-owned source files, separate from decoded caches and Personal Library. No automatic accepted-file GC.</summary>
public static partial class ProjectMedia
{
    public static WavResourceReuse BeginReuse(ProjectDocument document, Id<ResourceDescriptor> resource, string name,
        decimal rootPitch = 60m, decimal releaseMilliseconds = 5m) =>
        new(document, resource, name, rootPitch, releaseMilliseconds);

    [GeneratedRegex("\\Awav/[0-9a-f]{32}-([0-9a-f]{64})\\.wav\\z", RegexOptions.CultureInvariant)]
    private static partial Regex LocatorPattern();

    public static WavImport BeginImport(ProjectDocument document, string name, decimal rootPitch = 60m,
        decimal releaseMilliseconds = 5m, Id<SoundDefinition>? target = null, string? ownedMediaDirectory = null)
    {
        document.CheckAvailable();
        ProjectValidation.Require(
            !string.IsNullOrWhiteSpace(name) && name.Length <= ProjectValidation.MaximumTextLength,
            "Invalid media name.");
        PcmSampler.ValidateConfiguration(rootPitch, releaseMilliseconds);
        if (target is { } id)
            ProjectValidation.Require(document.Current.State.Sounds.Any(sound => sound.Id == id &&
                    sound.Algorithm == PcmSampler.Algorithm && sound.Extension is null),
                "Import target is not a supported sampler.");
        if (document.UnnamedMediaRoot is null)
        {
            var parent = ownedMediaDirectory ??
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                             "Seqvium", "ManagedMedia");
            document.UnnamedMediaRoot = Path.Combine(Path.GetFullPath(parent), document.LifecycleId.ToString("N"));
            document.MediaRoots.Add(document.UnnamedMediaRoot);
        }

        return new(document, name, target, rootPitch, releaseMilliseconds, document.UnnamedMediaRoot);
    }

    public static DecodedPcm Decode(ProjectDocument document, Id<ResourceDescriptor> resourceId)
    {
        document.CheckAvailable();
        var resource = document.Current.State.Resources.SingleOrDefault(item => item.Id == resourceId)
                       ?? throw new InvalidOperationException("Resource does not belong to the current document.");
        return DecodeStoredCopy(document, resource).Pcm;
    }

    internal static bool IsManagedWav(ResourceDescriptor resource) =>
        resource.ManagedLocator?.StartsWith("wav/", StringComparison.Ordinal) == true;

    internal static string PathFor(string root, string locator)
    {
        if (!LocatorPattern().IsMatch(locator)) throw new IOException("Unsupported managed WAV locator.");
        return Path.Combine(root, locator.Replace('/', Path.DirectorySeparatorChar));
    }

    internal static string Resolve(ProjectDocument document, ResourceDescriptor resource)
    {
        var (path, pcm) = DecodeStoredCopy(document, resource);
        using (pcm)
        {
        }

        return path;
    }

    private static (string Path, DecodedPcm Pcm) DecodeStoredCopy(ProjectDocument document, ResourceDescriptor resource)
    {
        if (!IsManagedWav(resource)) throw new IOException("Resource is not a validated managed WAV.");
        Exception? unavailable = null;
        foreach (var root in document.MediaRoots.AsEnumerable().Reverse())
        {
            var path = PathFor(root, resource.ManagedLocator!);
            if (!File.Exists(path)) continue;
            try
            {
                return (path, DecodePath(path, resource.ManagedLocator!));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                unavailable = error;
            }
        }

        if (unavailable is not null)
            throw new IOException($"Managed WAV {resource.Id} has no usable stored copy.", unavailable);
        throw new FileNotFoundException($"Managed WAV {resource.Id} is missing.");
    }

    internal static DecodedPcm DecodePath(string path, string locator)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > WavDecoder.MaximumSourceBytes)
            throw new WavFormatException("Managed WAV exceeds size limit.");
        var bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new WavFormatException("Managed WAV changed length during validation.");
        if (!LocatorPattern().IsMatch(locator) || !string.Equals(LocatorPattern().Match(locator).Groups[1].Value,
                Convert.ToHexStringLower(SHA256.HashData(bytes)), StringComparison.Ordinal))
            throw new WavFormatException("Managed WAV integrity mismatch.");
        return WavDecoder.Decode(bytes);
    }

    internal static ImmutableArray<string> PrepareSave(ProjectDocument document, ProjectSnapshot snapshot,
        string destination)
    {
        string root = destination + ".media";
        var diagnostics = ImmutableArray.CreateBuilder<string>();
        foreach (var resource in snapshot.State.Resources.Where(resource => !IsManagedWav(resource)))
            diagnostics.Add($"Resource {resource.Id} is unavailable or has not been validated.");
        foreach (var resource in snapshot.State.Resources.Where(IsManagedWav))
        {
            string source;
            try
            {
                source = Resolve(document, resource);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add($"Managed WAV {resource.Id} is unavailable or corrupt: {error.Message}");
                continue; // Already unavailable input is preserved as unresolved intent; destination write errors still fail.
            }

            string target = PathFor(root, resource.ManagedLocator!);
            if (File.Exists(target))
            {
                using (DecodePath(target, resource.ManagedLocator!))
                {
                }

                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            string temporary = target + $".{Guid.NewGuid():N}.pending";
            bool ownsTemporary = false;
            try
            {
                using (var input = File.OpenRead(source))
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    ownsTemporary = true;
                    input.CopyTo(output);
                    output.Flush(flushToDisk: true);
                }

                using (DecodePath(temporary, resource.ManagedLocator!))
                {
                }

                File.Move(temporary, target); // Unique immutable path; never overwrite existing media.
                ownsTemporary = false;
            }
            finally
            {
                if (ownsTemporary) File.Delete(temporary);
            }
        }

        return diagnostics.ToImmutable();
    }
}
