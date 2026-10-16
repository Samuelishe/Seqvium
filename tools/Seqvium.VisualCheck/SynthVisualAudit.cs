// SPDX-License-Identifier: Apache-2.0
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    // Layout and actual font outlines supplement visual review; they do not certify optical quality.
    private static object SynthCompositionAudit(SynthPaneContent pane, string name)
    {
        Control Id(string id) => pane.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == "Synth." + id);
        Rect InPane(Control c) => new(c.TranslatePoint(default, pane)!.Value, c.Bounds.Size);
        var axes = new List<object>();
        foreach (string key in new[] { "mix", "detune", "cutoff", "resonance", "attack", "decay", "sustain", "release" })
        {
            double difference = Math.Abs(InPane(Id("Value." + key)).Center.X - InPane(Id("Dial." + key)).Center.X);
            Require(difference < .5, name + ": value is not on its dial axis: " + key);
            axes.Add(new { Parameter = key, DifferenceDip = difference });
        }
        var scroll = (ScrollViewer)pane.Children[1];
        Require(scroll.Extent.Width <= scroll.Viewport.Width + 1, name + ": horizontal overflow.");
        var waves = Enumerable.Range(1, 2).SelectMany(osc => Enumerable.Range(0, 4)
            .Select(wave => Id($"Wave{osc}.{wave}"))).ToArray();
        Require(waves.All(c => c.Bounds.Width >= 32 && c.Bounds.Height >= 32), name + ": small waveform target.");
        Require(Math.Abs(InPane(Id("Oscillator1")).Top - InPane(Id("Oscillator2")).Top) < .5,
            name + ": oscillator displays do not share a row.");
        var release = (Button)Id("Release"); var group = (StackPanel)release.Content!;
        var label = group.Children.OfType<TextBlock>().Single();
        var icon = group.Children.OfType<Viewbox>().Single();
        var groupOrigin = group.TranslatePoint(default, release)!.Value;
        double contentCenter = groupOrigin.X + group.Bounds.Width / 2;
        Require(Math.Abs(contentCenter - release.Bounds.Width / 2) < .5, name + ": Release content is not centered.");
        var text = new FormattedText(label.Text ?? "", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch), label.FontSize,
            label.Foreground!);
        var textInk = (text.BuildGeometry(default) ?? throw new InvalidOperationException("Release text has no glyph outlines.")).Bounds;
        var labelOrigin = label.TranslatePoint(default, release)!.Value;
        textInk = textInk.Translate(new(labelOrigin.X, labelOrigin.Y));
        var path = icon.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single();
        var ink = path.Data!.GetRenderBounds(new Pen(path.Stroke, path.StrokeThickness));
        var iconInk = new Rect(path.TranslatePoint(ink.TopLeft, release)!.Value,
            path.TranslatePoint(ink.BottomRight, release)!.Value);
        var combined = textInk.Union(iconInk);
        return new { Name = name, DialValueAxes = axes, MinimumWaveWidth = waves.Min(c => c.Bounds.Width),
            Release = new { Button = release.Bounds.Size, Group = new Rect(groupOrigin, group.Bounds.Size),
                Text = label.Text, TextInk = textInk, IconInk = iconInk,
                InkCenterOffset = combined.Center - new Point(release.Bounds.Width / 2, release.Bounds.Height / 2),
                InkVerticalDifference = textInk.Center.Y - iconInk.Center.Y },
            HorizontalOverflow = Math.Max(0, scroll.Extent.Width - scroll.Viewport.Width) };
    }
    private static void CaptureSynthReleaseDetail(Button button, string output, string name)
    {
        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(
            new PixelSize((int)(button.Bounds.Width * 4), (int)(button.Bounds.Height * 4)), new Vector(384, 384));
        bitmap.Render(button);
        bitmap.Save(Path.Combine(output, name + ".detail.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        Json(output, name + "-detail", new { Purpose = "Actual control render for optical inspection at 4x; not native/high-DPI platform evidence", button.IsEnabled });
    }

}
