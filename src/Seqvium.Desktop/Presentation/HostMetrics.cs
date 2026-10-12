// SPDX-License-Identifier: Apache-2.0

using System.Collections.Frozen;
using Avalonia;
using Avalonia.Media;

namespace Seqvium.Desktop.Presentation;

/// <summary>Design System v1 geometry and typography; DIP values and typed Avalonia resources.</summary>
public static class HostMetrics
{
    public static IReadOnlyDictionary<string, double> Values { get; } = new Dictionary<string, double>
    {
        ["Type.Body"] = 12, ["Type.Caption"] = 11,
        ["Chrome.Height"] = 30, ["Toolbar.Height"] = 30, ["Status.Height"] = 22,
        ["Control.Height"] = 24, ["Control.Corner"] = 2, ["Space.Gap"] = 6,
        ["Control.InputHeight"] = 26, ["Control.ComfortableHeight"] = 28,
        ["Control.IconWidth"] = 26, ["Control.StatusHeight"] = 20,
        ["Pane.HeaderHeight"] = 28, ["Pane.Corner"] = 1, ["Chrome.ButtonWidth"] = 36,
        ["Border.Width"] = 1, ["Focus.Width"] = 2,
        ["Space.XSmall"] = 2, ["Space.Small"] = 4, ["Space.Medium"] = 8,
        ["Space.Large"] = 12, ["Space.Section"] = 16,
        ["Type.Section"] = 12, ["Type.PaneTitle"] = 12, ["Type.Numeric"] = 12,
        ["Level.Height"] = 30, ["Level.TrackHeight"] = 8, ["Level.ThumbSize"] = 12,
        ["Level.Inset"] = 8, ["ScrollBar.Size"] = 12
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, object> Resources { get; } = BuildResources();
    private static FrozenDictionary<string, object> BuildResources()
    {
        var resources = Values.ToDictionary(x => x.Key, x => x.Key.EndsWith("Corner", StringComparison.Ordinal)
            ? (object)new CornerRadius(x.Value) : x.Value);
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
        resources["Type.FontFamily"] = FontFamily.Default;
        resources["Type.MonospaceFamily"] = new FontFamily("Cascadia Mono,Consolas,monospace");
        return resources.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
