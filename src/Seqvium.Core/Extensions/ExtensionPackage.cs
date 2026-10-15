// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Seqvium.Core;

public enum ExtensionCategory { ContentPack, SampleGenerator }
public enum ExtensionAvailability { Disabled, Activating, Available, Deactivating, Incompatible, Damaged, Failed, Missing }
public sealed record ExtensionRequirement(string Contract, int Minimum, int Maximum, bool Optional = false);
public sealed record ExtensionContent(string Id, string Name, string File);
public sealed record ExtensionCapability(string Id, ExtensionCategory Category,
    ImmutableArray<ExtensionRequirement> Requires, ImmutableArray<ExtensionContent> Content);
public sealed record ExtensionManifest(int Schema, string Id, string Version, string Name,
    string? RussianName, ImmutableArray<ExtensionCapability> Capabilities)
{
    public string DisplayName(bool russian) => russian && ExtensionDiscovery.Label(RussianName) ? RussianName! : Name;
}
public sealed record ExtensionDiagnostic(string Code, string? Detail = null);
public sealed record ExtensionPackage(string Directory, ExtensionManifest? Manifest,
    ImmutableArray<ExtensionDiagnostic> Diagnostics)
{
    public string Identity => Manifest?.Id ?? Path.GetFileName(Directory);
}

/// <summary>Metadata only. A deployment root is explicitly supplied by the host, never inferred from user folders.</summary>
public static partial class ExtensionDiscovery
{
    public const int MaximumPackages = 64, MaximumManifestBytes = 65536;
    public static ImmutableDictionary<string, int> PlatformContracts { get; } =
        new Dictionary<string, int> { ["seqvium.content.wav"] = 1, ["seqvium.sample-generator"] = 1 }
            .ToImmutableDictionary(StringComparer.Ordinal);

    public static Task<ImmutableArray<ExtensionPackage>> DiscoverAsync(string root, CancellationToken cancellation = default)
        => Task.Run(() =>
        {
            if (!System.IO.Directory.Exists(root)) return ImmutableArray<ExtensionPackage>.Empty;
            try
            {
                RejectLink(root);
                var directories = System.IO.Directory.EnumerateDirectories(root).Take(MaximumPackages + 1).ToArray();
                if (directories.Length > MaximumPackages)
                    return [new(root, null, [new("Extension.TooManyPackages")])];
                var packages = directories.Order(StringComparer.Ordinal).Select(directory => Read(directory, cancellation)).ToArray();
                var duplicates = packages.Where(p => p.Manifest is not null).GroupBy(p => p.Manifest!.Id, StringComparer.Ordinal)
                    .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);
                return packages.Select(p => duplicates.Contains(p.Identity)
                    ? p with { Diagnostics = p.Diagnostics.Add(new("Extension.DuplicateIdentity")) } : p).ToImmutableArray();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
            { return [new(root, null, [new("Extension.DiscoveryFailed", error.Message)])]; }
        }, cancellation);

    private static ExtensionPackage Read(string directory, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        try
        {
            RejectLink(directory);
            using var input = File.OpenRead(Path.Combine(directory, "extension.json"));
            if (input.Length > MaximumManifestBytes) throw new InvalidDataException("Manifest exceeds 64 KiB.");
            using var json = JsonDocument.Parse(input, new() { MaxDepth = 12 });
            if (!json.RootElement.TryGetProperty("schema", out var schema) || schema.GetInt32() != 1)
                return new(directory, null, [new("Extension.ManifestVersion")]);
            var manifest = json.Deserialize<ExtensionManifest>(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter<ExtensionCategory>() }
            }) ?? throw new JsonException();
            Validate(manifest);
            var diagnostics = ImmutableArray.CreateBuilder<ExtensionDiagnostic>();
            foreach (var capability in manifest.Capabilities)
            {
                foreach (var requirement in capability.Requires)
                    if (!PlatformContracts.TryGetValue(requirement.Contract, out int version) ||
                        version < requirement.Minimum || version > requirement.Maximum)
                        diagnostics.Add(new(requirement.Optional ? "Extension.OptionalFallback" : "Extension.RequiredContract",
                            $"{capability.Id}: {requirement.Contract} [{requirement.Minimum}, {requirement.Maximum}]"));
                foreach (var content in capability.Content)
                {
                    try { ResolveContent(directory, content.File); }
                    catch (FileNotFoundException) { diagnostics.Add(new("Extension.MissingFile", $"{capability.Id}: {content.File}")); }
                    catch (DirectoryNotFoundException) { diagnostics.Add(new("Extension.MissingFile", $"{capability.Id}: {content.File}")); }
                }
            }
            return new(directory, manifest, diagnostics.ToImmutable());
        }
        catch (FileNotFoundException error) { return new(directory, null, [new("Extension.MissingFile", error.Message)]); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or
            ArgumentException or InvalidOperationException or FormatException or OverflowException)
        { return new(directory, null, [new("Extension.DamagedManifest", error.Message)]); }
    }

    private static void Validate(ExtensionManifest manifest)
    {
        if (!ValidIdentity(manifest.Id) || !Version.TryParse(manifest.Version, out var version) ||
            version.Build < 0 || !Label(manifest.Name) || manifest.Capabilities.IsDefaultOrEmpty || manifest.Capabilities.Length > 16 ||
            manifest.Capabilities.Select(c => c?.Id).Distinct(StringComparer.Ordinal).Count() != manifest.Capabilities.Length)
            throw new InvalidDataException("Invalid package identity, version, name or capabilities.");
        foreach (var capability in manifest.Capabilities)
        {
            if (capability is null || !ValidIdentity(capability.Id) || !Enum.IsDefined(capability.Category) ||
                capability.Requires.IsDefaultOrEmpty || capability.Requires.Length > 32 || capability.Content.IsDefault ||
                capability.Content.Length > 256 || capability.Requires.Any(r => r is null || !ValidIdentity(r.Contract) ||
                    r.Minimum < 1 || r.Maximum < r.Minimum) ||
                capability.Content.Any(c => c is null || !ValidIdentity(c.Id) || !Label(c.Name)) ||
                capability.Content.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != capability.Content.Length ||
                capability.Category == ExtensionCategory.SampleGenerator && !capability.Content.IsEmpty)
                throw new InvalidDataException("Invalid capability contract or content declaration.");
            string contract = capability.Category == ExtensionCategory.ContentPack ? "seqvium.content.wav" : "seqvium.sample-generator";
            if (!capability.Requires.Any(r => r.Contract == contract && !r.Optional))
                throw new InvalidDataException("The category contract must be required.");
        }
    }

    internal static bool Label(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 160 && !value.Any(char.IsControl);
    private static bool ValidIdentity(string? value) => value is { Length: <= 160 } && Identity().IsMatch(value);

    public static string ResolveContent(string directory, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Length > 240 ||
            relative.Contains('\\') || relative.Contains(':') || relative.Split('/').Any(p => p is "" or "." or "..") ||
            !Path.GetExtension(relative).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Content must be a relative WAV path inside its package.");
        RejectLink(directory);
        string current = directory;
        foreach (string part in relative.Split('/')) { current = Path.Combine(current, part); RejectLink(current); }
        if (!File.Exists(current)) throw new FileNotFoundException("Package content is missing.");
        return current;
    }

    private static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Linked package paths are not supported.");
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(?:[.-][a-z0-9]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex Identity();
}
