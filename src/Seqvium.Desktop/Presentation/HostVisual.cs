// SPDX-License-Identifier: Apache-2.0
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;
using System.Globalization;

namespace Seqvium.Desktop.Presentation;

/// <summary>DrawingContext consumers resolve the same live resources as standard controls.</summary>
internal static class HostVisual
{
    public static IBrush Brush(Control owner, HostTheme fallback, string role) =>
        owner.TryFindResource(role, out var value) && value is IBrush brush ? brush :
            HostMaterials.For(fallback).TryGetValue(role, out var material) && material is IBrush materialBrush ? materialBrush :
                new SolidColorBrush(Color.Parse(HostPalette.For(fallback)[role]));
    public static double Metric(Control owner, string role) =>
        owner.TryFindResource(role, out var value) ? value switch
        { double metric => metric, CornerRadius radius => radius.TopLeft, _ => HostMetrics.Values[role] } : HostMetrics.Values[role];
    public static Typeface Typeface(Control owner) => new(owner.TryFindResource("Type.FontFamily", out var value) && value is FontFamily family ? family : (FontFamily)HostMetrics.Resources["Type.FontFamily"]);
    public static void Caption(Control owner, ShellSession shell, DrawingContext context, string text, Point at) =>
        context.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            Typeface(owner), Metric(owner, "Type.Caption"), Brush(owner, shell.Preferences.Theme, "Text.Secondary")), at);
}
