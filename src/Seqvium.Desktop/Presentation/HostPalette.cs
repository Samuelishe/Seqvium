// SPDX-License-Identifier: Apache-2.0
using System.Collections.Frozen;

namespace Seqvium.Desktop.Presentation;

/// <summary>Host semantic colors shared by shell, retained panes, commands and first-party dialogs.</summary>
public static class HostPalette
{
    public static IReadOnlyDictionary<string, string> For(HostTheme theme) => theme == HostTheme.Light ? Light : Dark;

    private static readonly FrozenDictionary<string, string> Dark = new Dictionary<string, string>
    {
        ["Surface.Background"] = "#17191E", ["Surface.Raised"] = "#20232A", ["Surface.Workspace"] = "#1B1E24",
        ["Text.Primary"] = "#F0F1F5", ["Text.Secondary"] = "#ABB1C1", ["Border.Default"] = "#393E4A",
        ["Focus.Active"] = "#9C9AFF", ["Accent.Action"] = "#AAA8FF", ["State.Unavailable"] = "#737C8C",
        ["Status.Warning"] = "#EAC17B", ["Status.Critical"] = "#EF9A9A", ["Status.Playing"] = "#7DCEB5",
        ["Surface.Hover"] = "#2D3340", ["Surface.Pressed"] = "#353C4B", ["Surface.Selected"] = "#343650",
        ["Surface.Playing"] = "#263E38", ["Surface.Critical"] = "#4B3035", ["Surface.Accent"] = "#3C3C62"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> Light = new Dictionary<string, string>
    {
        ["Surface.Background"] = "#EDF0F5", ["Surface.Raised"] = "#FAFBFD", ["Surface.Workspace"] = "#F4F6FA",
        ["Text.Primary"] = "#242B38", ["Text.Secondary"] = "#566172", ["Border.Default"] = "#CBD2DF",
        ["Focus.Active"] = "#5E5AC6", ["Accent.Action"] = "#514AA7", ["State.Unavailable"] = "#778293",
        ["Status.Warning"] = "#805300", ["Status.Critical"] = "#AC3947", ["Status.Playing"] = "#17634F",
        ["Surface.Hover"] = "#E7EBF3", ["Surface.Pressed"] = "#DBE1ED", ["Surface.Selected"] = "#E4E2F6",
        ["Surface.Playing"] = "#DCEFE7", ["Surface.Critical"] = "#F8E3E6", ["Surface.Accent"] = "#E5E3F8"
    }.ToFrozenDictionary(StringComparer.Ordinal);
}
