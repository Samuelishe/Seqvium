// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Text.Json;

namespace Seqvium.Desktop.Workflow;

/// <summary>Optional local catalog metadata, verified against the exact imported bytes.
/// No pitch detection, filename inference, library mutation or machine-specific dependency.</summary>
internal static class SampleRootMetadata
{
    internal static decimal Read(string sourcePath, byte[] bytes)
        => ReadKnown(sourcePath, bytes) ?? 60m;

    internal static decimal? ReadKnown(string sourcePath, byte[] bytes)
    {
        string fullPath = Path.GetFullPath(sourcePath);
        var directory = new DirectoryInfo(Path.GetDirectoryName(fullPath)!);
        for (int depth = 0; directory is not null && depth < 12; depth++, directory = directory.Parent)
        {
            string catalog = Path.Combine(directory.FullName, "catalog.json");
            try
            {
                if (!File.Exists(catalog) || new FileInfo(catalog).Length > 2 * 1024 * 1024) continue;
                using var json = JsonDocument.Parse(File.ReadAllBytes(catalog), new() { MaxDepth = 16 });
                if (json.RootElement.ValueKind != JsonValueKind.Array) continue;
                string? hash = null;
                foreach (var item in json.RootElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("file", out var file) ||
                        file.ValueKind != JsonValueKind.String || !item.TryGetProperty("root_midi", out var root) ||
                        root.ValueKind != JsonValueKind.Number || !root.TryGetDecimal(out var pitch) ||
                        pitch is < 0 or > 127 ||
                        !item.TryGetProperty("sha256", out var sha) || sha.ValueKind != JsonValueKind.String) continue;
                    string candidate = Path.GetFullPath(Path.Combine(directory.FullName, file.GetString()!));
                    if (!string.Equals(candidate, fullPath,
                            OperatingSystem.IsWindows()
                                ? StringComparison.OrdinalIgnoreCase
                                : StringComparison.Ordinal)) continue;
                    hash ??= Convert.ToHexStringLower(SHA256.HashData(bytes));
                    if (string.Equals(hash, sha.GetString(), StringComparison.OrdinalIgnoreCase)) return pitch;
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException
                                              or ArgumentException or NotSupportedException)
            {
                /* Optional malformed/unavailable metadata never changes import eligibility. */
            }
        }

        return null;
    }
}
