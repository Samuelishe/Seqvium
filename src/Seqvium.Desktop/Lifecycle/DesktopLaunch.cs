// SPDX-License-Identifier: Apache-2.0
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Lifecycle;

/// <summary>Internal host composition. Production has no command-line/environment bypass.</summary>
internal sealed record DesktopLaunch(string Identity, string ConfigurationDirectory)
{
    public string? IsolatedMediaDirectory => Identity.StartsWith("Seqvium.VisualCheck.", StringComparison.Ordinal)
        ? Path.Combine(ConfigurationDirectory, "media") : null;
    public static DesktopLaunch Production { get; } = new("Seqvium.Desktop.v1",
        Path.GetDirectoryName(PreferenceStore.DefaultPath()) ?? throw new InvalidOperationException("Missing configuration directory."));
    public static DesktopLaunch Isolated(string identity, string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        if (!Path.IsPathFullyQualified(directory)) throw new ArgumentException("An absolute isolated directory is required.", nameof(directory));
        return new("Seqvium.VisualCheck." + identity, directory);
    }
}
