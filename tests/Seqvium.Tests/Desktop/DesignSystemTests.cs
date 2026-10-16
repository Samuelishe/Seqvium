// SPDX-License-Identifier: Apache-2.0

using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Xunit;

namespace Seqvium.Tests;

public sealed class DesignSystemTests
{
    [Theory]
    [InlineData(HostTheme.Dark)]
    [InlineData(HostTheme.Light)]
    public void FluentAliasesResolveAndCentralBrushChangesReachTemplatesAndDrawing(HostTheme theme)
    {
        var app = new App();
        foreach (var (key, value) in HostMetrics.Resources) app.Resources[key] = value;
        app.ApplyTheme(theme);
        _ = new HostFluentResources(app);
        Assert.Equal("Surface.Selected", HostFluentResources.Aliases["ComboBoxItemBackgroundSelected"]);
        Assert.Equal("Surface.Selected", HostFluentResources.Aliases["ComboBoxItemBackgroundSelectedPointerOver"]);
        Assert.Equal("Text.Primary", HostFluentResources.Aliases["ComboBoxItemForegroundSelected"]);
        foreach (var (alias, source) in HostFluentResources.Aliases)
        {
            Assert.True(app.Resources.TryGetResource(source, app.ActualThemeVariant, out var value), source);
            Assert.True(app.Resources.TryGetResource(alias, app.ActualThemeVariant, out var resolved), alias);
            if (alias.EndsWith("Color", StringComparison.Ordinal))
                Assert.Equal(((ISolidColorBrush)value!).Color, resolved);
            else Assert.Same(value, resolved);
        }

        var controlled = new SolidColorBrush(Colors.Magenta);
        var accepted = app.Resources["Control.Fill"];
        app.Resources["Control.Fill"] = controlled;
        Assert.Same(controlled, app.Resources["SliderTrackValueFill"]);
        Assert.Same(controlled, app.Resources["SliderTrackValueFillPointerOver"]);
        var canvas = new Control();
        canvas.Resources["Control.Fill"] = controlled;
        Assert.Same(controlled, HostVisual.Brush(canvas, theme, "Control.Fill"));
        app.Resources["Control.Fill"] = accepted;
        app.ApplyTheme(theme == HostTheme.Dark ? HostTheme.Light : HostTheme.Dark);
        Assert.Equal(
            Color.Parse(HostPalette.For(theme == HostTheme.Dark ? HostTheme.Light : HostTheme.Dark)["Control.Fill"]),
            ((ISolidColorBrush)app.Resources["SliderTrackValueFill"]!).Color);
    }

    [Fact]
    public void FirstPartyUiHasOneColorOwnerAndEveryHostStyleResourceExists()
    {
        string root = FindRoot();
        string desktop = Path.Combine(root, "src", "Seqvium.Desktop");
        var roles = HostPalette.For(HostTheme.Dark).Keys.Concat(HostMetrics.Resources.Keys)
            .Concat(HostMaterials.For(HostTheme.Dark).Keys).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(HostPalette.For(HostTheme.Dark).Keys.Order(), HostPalette.For(HostTheme.Light).Keys.Order());
        foreach (string file in Directory.EnumerateFiles(desktop, "*", SearchOption.AllDirectories)
                     .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                                 !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                                 Path.GetExtension(f) is ".cs" or ".axaml"))
        {
            string source = File.ReadAllText(file);
            if (Path.GetFileName(file) != "HostPalette.cs")
            {
                Assert.False(System.Text.RegularExpressions.Regex.IsMatch(source, "[\"']#[0-9A-Fa-f]{6,8}[\"']"), file);
                Assert.False(
                    System.Text.RegularExpressions.Regex.IsMatch(source,
                        @"\b(?:Colors\.(?!Transparent)|Color\.From(?:Rgb|Argb)|Brushes\.(?!Transparent))"), file);
            }

            if (Path.GetExtension(file) != ".axaml") continue;
            var xml = XDocument.Parse(source);
            foreach (var attribute in xml.Descendants().Attributes()
                         .Where(a => a.Value.StartsWith("{DynamicResource ", StringComparison.Ordinal)))
                Assert.Contains(attribute.Value[17..^1], roles);
        }

        Assert.All(HostFluentResources.Aliases.Values, role => Assert.Contains(role, roles));
    }

    [Theory]
    [InlineData(HostTheme.Dark)]
    [InlineData(HostTheme.Light)]
    public void RequiredDrawingAndInteractionRolesRemainVisibleWithoutSelection(HostTheme theme)
    {
        var palette = HostPalette.For(theme);
        foreach (string role in new[]
                 {
                     "Control.Track", "Control.Fill", "Control.Thumb", "Graph.Node", "Graph.NodeBorder", "Graph.Grid",
                     "Graph.Cable", "Surface.Hover", "Surface.Pressed", "Surface.Selected", "Focus.Active",
                     "State.Unavailable", "Status.Critical", "Status.Warning", "Status.Playing"
                 })
            Assert.Equal(255, Color.Parse(palette[role]).A);

        static double Luminance(Color color)
        {
            static double Linear(byte value)
            {
                double x = value / 255d;
                return x <= .04045 ? x / 12.92 : Math.Pow((x + .055) / 1.055, 2.4);
            }

            return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        }

        double Contrast(string first, string second)
        {
            double a = Luminance(Color.Parse(palette[first])), b = Luminance(Color.Parse(palette[second]));
            return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
        }

        Assert.True(Contrast("Graph.NodeBorder", "Surface.Workspace") >= 3);
        Assert.True(Contrast("Control.Fill", "Control.Track") >= 3);
        Assert.True(Contrast("Focus.Active", "Surface.Background") >= 3);
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Seqvium.sln")))
                return directory.FullName;
        throw new InvalidOperationException("Repository source required for resource audit.");
    }
}