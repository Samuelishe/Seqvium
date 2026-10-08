// SPDX-License-Identifier: Apache-2.0

using System.Collections.Frozen;

namespace Seqvium.Desktop.Presentation;

/// <summary>Shared compact-shell metrics in DIP, not a future pane API.</summary>
public static class HostMetrics
{
    public static IReadOnlyDictionary<string, double> Values { get; } = new Dictionary<string, double>
    {
        ["Type.Body"] = 12, ["Type.Caption"] = 11,
        ["Chrome.Height"] = 30, ["Toolbar.Height"] = 30, ["Status.Height"] = 22,
        ["Control.Height"] = 24, ["Control.Corner"] = 2, ["Space.Gap"] = 6
    }.ToFrozenDictionary(StringComparer.Ordinal);
}
