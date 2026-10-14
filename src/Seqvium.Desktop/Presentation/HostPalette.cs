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
        ["Focus.Active"] = "#00E5D1", ["Accent.Action"] = "#00E5D1", ["State.Unavailable"] = "#788895",
        ["Status.Warning"] = "#EAC17B", ["Status.Critical"] = "#EF9A9A", ["Status.Playing"] = "#7DCEB5",
        ["Surface.Hover"] = "#34424D", ["Surface.Pressed"] = "#3D4E5A", ["Surface.Selected"] = "#2B454A",
        ["Surface.Playing"] = "#24443C", ["Surface.Critical"] = "#4B3035", ["Surface.Accent"] = "#24474B",
        ["Graph.Cable"] = "#93B1C4", ["Graph.Grid"] = "#202C35",
        ["Graph.Node"] = "#26323C", ["Graph.NodeBorder"] = "#6E899C",
        ["Piano.WhiteKey"] = "#BFCFD5", ["Piano.BlackKey"] = "#25323C", ["Piano.KeyInk"] = "#25323C",
        ["Control.Track"] = "#384651", ["Control.Fill"] = "#00E5D1",
        ["Control.Thumb"] = "#EDF2F5",
        ["Mixer.Channel"] = "#202930", ["Mixer.Well"] = "#0F171D", ["Mixer.Cap"] = "#BCCBD3",
        ["Mixer.CapShade"] = "#536978",
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
        ["Material.Clear"] = "#00FFFFFF", ["Material.Signal"] = "#4000E5D1",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> Light = new Dictionary<string, string>
    {
        ["Surface.Background"] = "#DEE7EB", ["Surface.Raised"] = "#FAFCFD", ["Surface.Workspace"] = "#EDF3F5",
        ["Text.Primary"] = "#202F3B", ["Text.Secondary"] = "#526573", ["Border.Default"] = "#B4C5CE",
        ["Focus.Active"] = "#006F68", ["Accent.Action"] = "#007A70", ["State.Unavailable"] = "#738593",
        ["Status.Warning"] = "#805300", ["Status.Critical"] = "#AC3947", ["Status.Playing"] = "#17634F",
        ["Surface.Hover"] = "#D2E3E9", ["Surface.Pressed"] = "#BDD4DE", ["Surface.Selected"] = "#BCE4D8",
        ["Surface.Playing"] = "#C5E9D8", ["Surface.Critical"] = "#F8E3E6", ["Surface.Accent"] = "#C0E8DB",
        ["Graph.Cable"] = "#456677", ["Graph.Grid"] = "#D3E0E5",
        ["Graph.Node"] = "#FFFFFF", ["Graph.NodeBorder"] = "#5D7B8C",
        ["Piano.WhiteKey"] = "#FAFCFD", ["Piano.BlackKey"] = "#344956", ["Piano.KeyInk"] = "#25323C",
        ["Control.Track"] = "#BECCD5", ["Control.Fill"] = "#007A70",
        ["Control.Thumb"] = "#F8FAFB",
        ["Mixer.Channel"] = "#D9E4E9", ["Mixer.Well"] = "#BDCCD5", ["Mixer.Cap"] = "#F8FBFC",
        ["Mixer.CapShade"] = "#A0B3BE",
        ["Rack.Channel0"] = "#BE4F24", ["Rack.Step0"] = "#F2C6B7",
        ["Rack.Channel1"] = "#9C6B08", ["Rack.Step1"] = "#EFDFAB",
        ["Rack.Channel2"] = "#008077", ["Rack.Step2"] = "#B4E4DD",
        ["Rack.Channel3"] = "#7850C1", ["Rack.Step3"] = "#D7C6F2",
        ["Rack.Channel4"] = "#477E2B", ["Rack.Step4"] = "#CDE0B8",
        ["Rack.Channel5"] = "#AC407B", ["Rack.Step5"] = "#EBC4D9",
        ["Rack.Channel6"] = "#2C79AD", ["Rack.Step6"] = "#BDDAEF",
        ["Rack.Channel7"] = "#596F89", ["Rack.Step7"] = "#CFD8E3",
        ["Rack.Ruler"] = "#E4ECF0",
        ["Rack.StepInk"] = "#FFFFFF",
        ["Rack.StepBorder"] = "#AABCC5",
        ["Material.Highlight"] = "#70FFFFFF", ["Material.Shade"] = "#40304758",
        ["Material.Clear"] = "#00FFFFFF", ["Material.Signal"] = "#40007A70",
    }.ToFrozenDictionary(StringComparer.Ordinal);
}
