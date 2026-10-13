// SPDX-License-Identifier: Apache-2.0

using System.Collections.Frozen;
using Avalonia;
using Avalonia.Media;

namespace Seqvium.Desktop.Presentation;

/// <summary>Design System v2 geometry and typography; DIP values and typed Avalonia resources.</summary>
public static class HostMetrics
{
    public static IReadOnlyDictionary<string, double> Values { get; } = new Dictionary<string, double>
    {
        ["Type.Body"] = 13, ["Type.Caption"] = 11, ["Type.Transport"] = 18,
        ["Chrome.Height"] = 34, ["Toolbar.Height"] = 48, ["Status.Height"] = 24,
        ["Control.Height"] = 30, ["Control.Corner"] = 5, ["Space.Gap"] = 6,
        ["Control.InputHeight"] = 30, ["Control.ComfortableHeight"] = 34,
        ["Control.IconWidth"] = 32, ["Control.StatusHeight"] = 22,
        ["Pane.HeaderHeight"] = 32, ["Pane.Corner"] = 8, ["Chrome.ButtonWidth"] = 42,
        ["Border.Width"] = 1, ["Focus.Width"] = 2,
        ["Space.XSmall"] = 2, ["Space.Small"] = 4, ["Space.Medium"] = 8,
        ["Space.Large"] = 12, ["Space.Section"] = 16,
        ["Type.Section"] = 14, ["Type.PaneTitle"] = 13, ["Type.Numeric"] = 13,
        ["Rack.StepHeight"] = 26, ["Rack.RowHeight"] = 44, ["Rack.StepCorner"] = 4,
        ["Rail.Width"] = 52,
        ["Playlist.LaneHeight"] = 72, ["Playlist.RulerHeight"] = 32, ["Playlist.HeaderWidth"] = 156,
        ["Playlist.OverviewHeight"] = 28,
        ["Piano.KeyWidth"] = 76, ["Piano.RowHeight"] = 20, ["Piano.RulerHeight"] = 32,
        ["Piano.VelocityHeight"] = 92, ["Piano.NoteCorner"] = 3,
        ["Level.Height"] = 30, ["Level.TrackHeight"] = 8, ["Level.ThumbSize"] = 12,
        ["Level.Inset"] = 8, ["ScrollBar.Size"] = 12
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, object> Resources { get; } = BuildResources();

    private static FrozenDictionary<string, object> BuildResources()
    {
        var resources = Values.ToDictionary(x => x.Key, x => x.Key.EndsWith("Corner", StringComparison.Ordinal)
            ? (object)new CornerRadius(x.Value)
            : x.Value);
        resources["Border.Control"] = new Thickness(Values["Border.Width"]);
        resources["Border.Top"] = new Thickness(0, Values["Border.Width"], 0, 0);
        resources["Border.Bottom"] = new Thickness(0, 0, 0, Values["Border.Width"]);
        resources["Border.Focus"] = new Thickness(Values["Focus.Width"]);
        resources["Padding.Control"] = new Thickness(Values["Space.Gap"], 0);
        resources["Padding.Input"] = new Thickness(Values["Space.Small"], Values["Space.XSmall"]);
        resources["Padding.Content"] = new Thickness(Values["Space.Medium"], Values["Space.Small"]);
        resources["Padding.Popup"] = new Thickness(Values["Space.Large"]);
        resources["Padding.Dialog"] = new Thickness(Values["Space.Section"]);
        resources["Padding.Parameter"] = new Thickness(Values["Space.Gap"], Values["Space.XSmall"]);
        resources["Padding.ComboBox"] = new Thickness(Values["Space.Gap"], 0, Values["Control.Height"], 0);
        resources["Padding.Numeric"] = new Thickness(3, 0);
        resources["Control.NumericInnerHeight"] = Values["Control.Height"] - 2 * Values["Border.Width"];
        resources["Padding.Status"] = new Thickness(0, 0, Values["Space.Gap"], 0);
        resources["Padding.Icon"] = new Thickness(3);
        resources["Rack.BarPadding"] = new Thickness(0, 0, Values["Space.Medium"], 0);
        resources["Navigation.ScrollGutter"] = new Thickness(0, 0, Values["ScrollBar.Size"] + Values["Space.Small"], 0);
        resources["Type.FontFamily"] = FontFamily.Default;
        resources["Type.MonospaceFamily"] = new FontFamily("Cascadia Mono,Consolas,monospace");
        return resources.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
