// SPDX-License-Identifier: Apache-2.0
using System.Collections.Frozen;

namespace Seqvium.Desktop.Presentation;

/// <summary>Small shell-owned role catalog. Warning is used only for preference persistence failures.</summary>
public static class HostPalette
{
    public static IReadOnlyDictionary<string, string> For(HostTheme theme) => theme == HostTheme.Light ? Light : Dark;

    private static readonly FrozenDictionary<string, string> Dark = new Dictionary<string, string>
    {
        ["Surface.Background"] = "#17191E", ["Surface.Raised"] = "#20232A", ["Surface.Workspace"] = "#1B1E24",
        ["Text.Primary"] = "#F0F1F5", ["Text.Secondary"] = "#ABB1C1", ["Border.Default"] = "#393E4A",
        ["Focus.Active"] = "#B5ADFF", ["Accent.Action"] = "#B5ADFF", ["State.Unavailable"] = "#929AAA",
        ["Status.Warning"] = "#F5C878"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> Light = new Dictionary<string, string>
    {
        ["Surface.Background"] = "#F3F2F7", ["Surface.Raised"] = "#FFFFFF", ["Surface.Workspace"] = "#FAF9FC",
        ["Text.Primary"] = "#252632", ["Text.Secondary"] = "#595F70", ["Border.Default"] = "#CCCADA",
        ["Focus.Active"] = "#5B44B5", ["Accent.Action"] = "#5B44B5", ["State.Unavailable"] = "#676D7C",
        ["Status.Warning"] = "#805000"
    }.ToFrozenDictionary(StringComparer.Ordinal);
}
