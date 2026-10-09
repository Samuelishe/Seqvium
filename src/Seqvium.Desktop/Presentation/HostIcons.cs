// SPDX-License-Identifier: Apache-2.0
using System.Collections.Frozen;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace Seqvium.Desktop.Presentation;

/// <summary>Project-authored 24-unit vector family, drawn with one 1.7-unit rounded stroke.
/// Geometry is presentation only. Brushes follow the owning control's semantic foreground.</summary>
internal static class HostIcons
{
    internal static IReadOnlyDictionary<string, string> Paths { get; } = new Dictionary<string, string>
    {
        ["Import"] = "M4 14V20H20V14 M12 3V15 M7 10L12 15L17 10",
        ["Open"] = "M3 19V6H9L11 8H20V11 M3 19L6 11H22L19 19Z",
        ["Save"] = "M5 3H17L21 7V21H3V3H5Z M7 3V9H16V3 M7 21V14H17V21",
        ["SaveAs"] = "M5 3H17L21 7V11 M3 21V3H5 M7 3V9H16V3 M7 19V14H11 M13 21L14 17L20 11L23 14L17 20Z",
        ["Play"] = "M8 5L19 12L8 19Z",
        ["Stop"] = "M6 6H18V18H6Z",
        ["Panic"] = "M8 3H16L21 8V16L16 21H8L3 16V8Z M12 7V13 M12 17V17.1",
        ["Undo"] = "M8 5L3 10L8 15 M3 10H13C19 10 21 15 19 19",
        ["Redo"] = "M16 5L21 10L16 15 M21 10H11C5 10 3 15 5 19",
        ["Info"] = "M12 3A9 9 0 1 1 12 21A9 9 0 1 1 12 3 M12 11V17 M12 7V7.1",
        ["More"] = "M5 12V12.1 M12 12V12.1 M19 12V12.1",
        ["Close"] = "M6 6L18 18 M18 6L6 18",
        ["Minimize"] = "M5 13H19",
        ["Maximize"] = "M5 5H19V19H5Z",
        ["Restore"] = "M8 5V3H21V16H19 M3 8H16V21H3Z",
        ["Resize"] = "M9 20L20 9 M14 20L20 14 M19 20L20 19",
        ["Up"] = "M6 15L12 9L18 15",
        ["Warning"] = "M12 3L22 21H2Z M12 9V14 M12 18V18.1",
        ["Check"] = "M5 12L10 17L20 7",
        ["Theme"] = "M12 3A9 9 0 1 1 12 21A9 9 0 1 1 12 3 M12 3V21",
        ["Panes"] = "M3 4H21V20H3Z M10 4V20 M3 9H10"
    }.ToFrozenDictionary(StringComparer.Ordinal);

    internal static Control Create(string name, TemplatedControl owner, double size = 18)
    {
        var path = new Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse(Paths[name]), Width = 24, Height = 24, StrokeThickness = 1.7,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, IsHitTestVisible = false
        };
        path.Bind(Shape.StrokeProperty, owner.GetObservable(TemplatedControl.ForegroundProperty));
        if (name is "Play" or "Stop")
            path.Bind(Shape.FillProperty, owner.GetObservable(TemplatedControl.ForegroundProperty));
        return new Viewbox { Width = size, Height = size, Child = path, IsHitTestVisible = false };
    }
}
