// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Media;

namespace Seqvium.Desktop.Presentation;

/// <summary>Instrument materials derived entirely from semantic theme colors.</summary>
internal static class HostMaterials
{
    internal static IReadOnlyDictionary<string, object> For(HostTheme theme)
    {
        var colors = HostPalette.For(theme);
        Color ColorOf(string role) => Color.Parse(colors[role]);
        LinearGradientBrush Gradient(params (string Role, double Offset)[] stops) => new()
        {
            StartPoint = new(0, 0, RelativeUnit.Relative), EndPoint = new(0, 1, RelativeUnit.Relative),
            GradientStops = [.. stops.Select(s => new GradientStop(ColorOf(s.Role), s.Offset))]
        };
        var materials = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Material.Panel"] = Gradient(("Surface.Raised", 0), ("Surface.Background", 1)),
            ["Material.Button"] = Gradient(("Surface.Hover", 0), ("Surface.Raised", .45), ("Surface.Background", 1)),
            ["Material.Bevel"] = Gradient(("Material.Highlight", 0), ("Material.Clear", .2), ("Material.Clear", .7), ("Material.Shade", 1)),
            ["Material.Dial"] = Gradient(("Surface.Hover", 0), ("Surface.Raised", .4), ("Surface.Background", 1)),
            ["Material.PanelShadow"] = new BoxShadows(new BoxShadow { OffsetY = 8, Blur = 24, Color = ColorOf("Material.Shade") }),
            ["Material.Recess"] = new BoxShadows(new BoxShadow { OffsetY = 2, Blur = 5, IsInset = true, Color = ColorOf("Material.Shade") }),
            ["Material.PadShadow"] = new BoxShadows(new BoxShadow { OffsetY = 2, Blur = 2, Color = ColorOf("Material.Shade") }),
            ["Material.CurveFill"] = Gradient(("Material.Signal", 0), ("Material.Clear", 1))
        };
        for (int i = 0; i < 8; i++)
        {
            var channel = ColorOf($"Rack.Channel{i}");
            materials[$"Material.Glow{i}"] = new BoxShadows(new BoxShadow { Blur = 7, Spread = 0, Color = new Color(90, channel.R, channel.G, channel.B) });
        }
        return materials;
    }
}
