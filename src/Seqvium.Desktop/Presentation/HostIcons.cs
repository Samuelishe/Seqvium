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
        ["Automation"] = "M3 19L10 6L21 13 M1 17H5V21H1Z M8 4H12V8H8Z M19 11H23V15H19Z",
        ["Power"] = "M12 2V11 M7 5A8 8 0 1 0 17 5",
        ["Split"] = "M12 3V21 M3 7H9V17H3Z M15 7H21V17H15Z",
        ["New"] = "M5 3H14L20 9V21H5Z M14 3V9H20 M9 15H16 M12.5 11.5V18.5",
        ["Open"] = "M3 19V6H9L11 8H20V11 M3 19L6 11H22L19 19Z",
        ["Browser"] = "M3 5H10L12 7H21V20H3Z M6 12H9 M6 16H9 M13 11V17 M16 10V18 M19 13V16",
        ["Patterns"] = "M3 4H21V11H3Z M3 14H21V21H3Z M6 8H9 M12 7H15 M6 18H10 M13 17H17",
        ["SampleLab"] = "M9 3H15 M10 3V10L4 20H20L14 10V3 M7 15H17 M9 18V18.1 M13 16V16.1",
        ["Reference"] = "M12 3V7 M12 17V21 M3 12H7 M17 12H21 M12 6A6 6 0 1 1 12 18A6 6 0 1 1 12 6 M12 10V14 M10 12H14",
        ["Refresh"] = "M20 10A8 8 0 1 0 19 18 M20 3V10H13",
        ["Save"] = "M5 3H17L21 7V21H3V3H5Z M7 3V9H16V3 M7 21V14H17V21",
        ["Export"] = "M4 15V21H20V15 M12 16V3 M7 8L12 3L17 8 M4 12L6 10L8 12L10 10",
        ["Loop"] = "M4 9C4 5 7 3 12 3H19 M15 0L19 3L15 6 M20 15C20 19 17 21 12 21H5 M9 18L5 21L9 24",
        ["Selection"] = "M3 8V3H8 M16 3H21V8 M21 16V21H16 M8 21H3V16 M7 7H17V17H7Z",
        ["Start"] = "M5 4V20 M19 5L8 12L19 19Z",
        ["SaveAs"] = "M5 3H17L21 7V11 M3 21V3H5 M7 3V9H16V3 M7 19V14H11 M13 21L14 17L20 11L23 14L17 20Z",
        ["Play"] = "M8 5L19 12L8 19Z",
        ["NoteOff"] = "M3 5H6 M10 7L17 17 M8 3A2 2 0 1 1 8 7A2 2 0 1 1 8 3 M19 17A2 2 0 1 1 19 21A2 2 0 1 1 19 17 M3 21H13",
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
        ["Moon"] = "M20 15A9 9 0 0 1 9 4A9 9 0 1 0 20 15Z",
        ["Sun"] = "M12 7A5 5 0 1 1 12 17A5 5 0 1 1 12 7 M12 1V4 M12 20V23 M1 12H4 M20 12H23 M4 4L6 6 M18 18L20 20 M4 20L6 18 M18 6L20 4",
        ["Settings"] = "M4 6H20 M4 12H20 M4 18H20 M8 3V9 M16 9V15 M10 15V21",
        ["Panes"] = "M3 4H21V20H3Z M10 4V20 M3 9H10",
        ["Graph"] = "M3 4H9V10H3Z M15 14H21V20H15Z M9 7H15V17 M3 17H15",
        ["Add"] = "M12 4V20 M4 12H20",
        ["Delete"] = "M4 6H20 M9 6V3H15V6 M6 6L7 21H17L18 6 M10 10V17 M14 10V17",
        ["Rack"] = "M3 5H7V9H3Z M10 5H14V9H10Z M17 5H21V9H17Z M3 15H7V19H3Z M10 15H14V19H10Z M17 15H21V19H17Z",
        ["Mixer"] = "M5 3V21 M12 3V21 M19 3V21 M2 8H8V12H2Z M9 14H15V18H9Z M16 5H22V9H16Z",
        ["Piano"] = "M3 4H21V20H3Z M7 4V14H10V4 M14 4V14H17V4 M8 14V20 M16 14V20",
        ["Draw"] = "M4 20L5 15L17 3L21 7L9 19Z M14 6L18 10 M4 20L9 19",
        ["Velocity"] = "M4 20V13 M9 20V6 M14 20V10 M19 20V3 M2 20H22",
        ["Playlist"] = "M3 4H21 M3 9H11V13H3Z M13 9H21V13H13Z M7 16H17V20H7Z",
        ["Duplicate"] = "M8 8H21V21H8Z M3 16V3H16",
        ["Follow"] = "M4 12H17 M12 7L17 12L12 17 M21 4V20",
        ["Sound"] = "M3 10H7L12 5V19L7 14H3Z M16 8C20 10 20 14 16 16 M19 5C25 9 25 15 19 19",
        ["Inspector"] = "M5 3H19V21H5Z M9 8H15 M9 12H15 M9 16H13",
        ["Left"] = "M15 5L8 12L15 19",
        ["Right"] = "M9 5L16 12L9 19",
        ["Fit"] = "M3 9V3H9 M15 3H21V9 M21 15V21H15 M9 21H3V15 M8 8H16V16H8Z"
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
