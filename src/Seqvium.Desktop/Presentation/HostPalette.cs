// SPDX-License-Identifier: Apache-2.0
using System.Collections.Frozen;

namespace Seqvium.Desktop.Presentation;

/// <summary>Host semantic colors shared by shell, retained panes, commands and first-party dialogs.</summary>
public static class HostPalette
{
    public static IReadOnlyDictionary<string, string> For(HostTheme theme) => theme == HostTheme.Light ? Light : Dark;

    private static readonly FrozenDictionary<string, string> Dark = new Dictionary<string, string>
    {
        ["Surface.Background"] = "#171D23", ["Surface.Raised"] = "#252D35", ["Surface.Workspace"] = "#11171D",
        ["Text.Primary"] = "#EDF2F5", ["Text.Secondary"] = "#A9B8C3", ["Border.Default"] = "#394650",
        ["Focus.Active"] = "#77D9CA", ["Accent.Action"] = "#77D9CA", ["State.Unavailable"] = "#788895",
        ["Status.Warning"] = "#EAC17B", ["Status.Critical"] = "#EF9A9A", ["Status.Playing"] = "#7DCEB5",
        ["Surface.Hover"] = "#34424D", ["Surface.Pressed"] = "#3D4E5A", ["Surface.Selected"] = "#2B454A",
        ["Surface.Playing"] = "#24443C", ["Surface.Critical"] = "#4B3035", ["Surface.Accent"] = "#24474B",
        ["Graph.Cable"] = "#93B1C4", ["Graph.Grid"] = "#202C35",
        ["Graph.Node"] = "#26323C", ["Graph.NodeBorder"] = "#6E899C",
        ["Control.Track"] = "#384651", ["Control.Fill"] = "#77D9CA",
        ["Control.Thumb"] = "#EDF2F5",
        ["Rack.Channel0"] = "#E49A83", ["Rack.Step0"] = "#3C302E",
        ["Rack.Channel1"] = "#D6B469", ["Rack.Step1"] = "#373429",
        ["Rack.Channel2"] = "#69C6C0", ["Rack.Step2"] = "#243A3D",
        ["Rack.Channel3"] = "#A59CE0", ["Rack.Step3"] = "#302F42",
        ["Rack.Channel4"] = "#90B985", ["Rack.Step4"] = "#2C382D",
        ["Rack.Channel5"] = "#D49ABC", ["Rack.Step5"] = "#3B2E39",
        ["Rack.Channel6"] = "#81B5DB", ["Rack.Step6"] = "#283643",
        ["Rack.Channel7"] = "#BABFCD", ["Rack.Step7"] = "#323741",
        ["Rack.Ruler"] = "#1C252D",
        ["Rack.StepInk"] = "#172127",
        ["Rack.StepBorder"] = "#4F616E",
        ["Material.Highlight"] = "#60FFFFFF", ["Material.Shade"] = "#90060C10",
        ["Material.Clear"] = "#00FFFFFF", ["Material.Signal"] = "#4077D9CA",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> Light = new Dictionary<string, string>
    {
        ["Surface.Background"] = "#E5EBEF", ["Surface.Raised"] = "#F8FAFB", ["Surface.Workspace"] = "#EFF3F5",
        ["Text.Primary"] = "#202F3B", ["Text.Secondary"] = "#526573", ["Border.Default"] = "#C6D1D8",
        ["Focus.Active"] = "#117B70", ["Accent.Action"] = "#096A60", ["State.Unavailable"] = "#738593",
        ["Status.Warning"] = "#805300", ["Status.Critical"] = "#AC3947", ["Status.Playing"] = "#17634F",
        ["Surface.Hover"] = "#DCE7EC", ["Surface.Pressed"] = "#CFDEE5", ["Surface.Selected"] = "#D9ECE8",
        ["Surface.Playing"] = "#DCEFE7", ["Surface.Critical"] = "#F8E3E6", ["Surface.Accent"] = "#D7EDE7",
        ["Graph.Cable"] = "#456677", ["Graph.Grid"] = "#DCE6EB",
        ["Graph.Node"] = "#FFFFFF", ["Graph.NodeBorder"] = "#738C9C",
        ["Control.Track"] = "#AFBFC9", ["Control.Fill"] = "#096A60",
        ["Control.Thumb"] = "#F8FAFB",
        ["Rack.Channel0"] = "#A84F32", ["Rack.Step0"] = "#EDE1DD",
        ["Rack.Channel1"] = "#8D6419", ["Rack.Step1"] = "#ECE5D5",
        ["Rack.Channel2"] = "#14756F", ["Rack.Step2"] = "#DCEDEB",
        ["Rack.Channel3"] = "#6D57AD", ["Rack.Step3"] = "#E7E2EF",
        ["Rack.Channel4"] = "#47743A", ["Rack.Step4"] = "#E2EADD",
        ["Rack.Channel5"] = "#995474", ["Rack.Step5"] = "#ECE0E7",
        ["Rack.Channel6"] = "#3B739D", ["Rack.Step6"] = "#DDE8F0",
        ["Rack.Channel7"] = "#626E83", ["Rack.Step7"] = "#E3E7EC",
        ["Rack.Ruler"] = "#E4ECF0",
        ["Rack.StepInk"] = "#FFFFFF",
        ["Rack.StepBorder"] = "#AABCC5",
        ["Material.Highlight"] = "#B0FFFFFF", ["Material.Shade"] = "#30334450",
        ["Material.Clear"] = "#00FFFFFF", ["Material.Signal"] = "#30117B70",
    }.ToFrozenDictionary(StringComparer.Ordinal);
}
