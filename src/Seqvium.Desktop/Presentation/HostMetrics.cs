// SPDX-License-Identifier: Apache-2.0

using System.Collections.Frozen;
using Avalonia;
using Avalonia.Media;

namespace Seqvium.Desktop.Presentation;

/// <summary>Design System v2 geometry and typography; DIP values and typed Avalonia resources.</summary>
public static class HostMetrics
{
    public const string FontFamilyName = "avares://Seqvium.Desktop/Assets/Fonts/Onest#Onest";
    public const string MonospaceFamilyName = "avares://Seqvium.Desktop/Assets/Fonts/JetBrainsMono#JetBrains Mono";
    public const string MusicSymbolFamilyName = "avares://Seqvium.Desktop/Assets/Fonts/NotoSansSymbols#Noto Sans Symbols";

    public static IReadOnlyDictionary<string, double> Values { get; } = new Dictionary<string, double>
    {
        ["Type.Body"] = 12, ["Type.Caption"] = 11, ["Type.Transport"] = 16,
        ["Type.Brand"] = 17, ["Type.BrandDisplay"] = 30, ["Brand.ChromeSize"] = 22, ["Brand.AboutSize"] = 52,
        ["Chrome.Height"] = 30, ["Toolbar.Height"] = 40, ["Status.Height"] = 20,
        ["Control.Height"] = 26, ["Control.Corner"] = 3, ["Space.Gap"] = 4,
        ["Control.InputHeight"] = 26, ["Control.ComfortableHeight"] = 30,
        ["Control.IconWidth"] = 28, ["Control.StatusHeight"] = 20,
        ["Pane.HeaderHeight"] = 26, ["Pane.Corner"] = 4, ["Chrome.ButtonWidth"] = 38,
        ["Border.Width"] = 1, ["Focus.Width"] = 2,
        ["Space.XSmall"] = 2, ["Space.Small"] = 4, ["Space.Medium"] = 8,
        ["Space.Large"] = 10, ["Space.Section"] = 12,
        ["Type.Section"] = 13, ["Type.PaneTitle"] = 12, ["Type.Numeric"] = 12,
        ["Rack.StepHeight"] = 24, ["Rack.RowHeight"] = 36, ["Rack.StepCorner"] = 3,
        ["Rail.Width"] = 44,
        ["Playlist.LaneHeight"] = 52, ["Playlist.RulerHeight"] = 26, ["Playlist.HeaderWidth"] = 128,
        ["Playlist.OverviewHeight"] = 22,
        ["Playlist.SectionHeight"] = 26,
        ["Mixer.ChannelWidth"] = 60, ["Mixer.MasterWidth"] = 72, ["Mixer.StripHeight"] = 292,
        ["Mixer.FaderHeight"] = 112, ["Mixer.PanSize"] = 34, ["Mixer.ControlHeight"] = 20,
        ["Piano.KeyWidth"] = 76, ["Piano.RowHeight"] = 20, ["Piano.RulerHeight"] = 32,
        ["Piano.VelocityHeight"] = 92, ["Piano.NoteCorner"] = 3,
        ["Level.Height"] = 26, ["Level.TrackHeight"] = 6, ["Level.ThumbSize"] = 10,
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
        resources["Type.FontFamily"] = new FontFamily(FontFamilyName);
        resources["Type.BrandFamily"] = resources["Type.FontFamily"];
        resources["Type.BrandTracking"] = -.4d;
        resources["Type.MonospaceFamily"] = new FontFamily(MonospaceFamilyName);
        return resources.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
