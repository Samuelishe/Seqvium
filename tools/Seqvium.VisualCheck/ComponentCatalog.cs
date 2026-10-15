// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

/// <summary>Explicit fixture pseudo states are template coverage, never native mouse evidence.</summary>
internal static class ComponentCatalog
{
    private sealed class StateButton : Button
    {
        protected override Type StyleKeyOverride => typeof(Button);
        public void State(string state) => PseudoClasses.Set(state, true);
    }
    private sealed class StateSlider : Slider
    {
        protected override Type StyleKeyOverride => typeof(Slider);
        public void State(string state) => PseudoClasses.Set(state, true);
    }
    private sealed class StateTextBox : TextBox
    {
        protected override Type StyleKeyOverride => typeof(TextBox);
        public void State(string state) => PseudoClasses.Set(state, true);
    }
    public static async Task VerifyAsync(App app, MainWindow main, string output)
    {
        var shell = Program.Field<ShellSession>(main, "_session");
        var body = new StackPanel { Spacing = 12, Margin = new(12) };
        string[] states = ["normal", "hover", "pressed", "focus", "selected", "disabled", "invalid"];
        var labels = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        labels.Children.Add(new TextBlock { Text = "Component / fixture state", Width = 160 });
        foreach (var state in states) labels.Children.Add(new TextBlock { Text = state, Width = 112 });
        body.Children.Add(labels);
        var buttons = new List<Button>(); var sliders = new List<Slider>(); var inputs = new List<TextBox>();
        foreach (string variant in new[] { "standard", "primary", "compact", "command", "danger", "audition", "audition-stop" })
        {
            var row = Row(variant + " Button");
            foreach (string state in states)
            {
                var button = new StateButton { Content = "Action", Width = variant == "command" ? double.NaN : 112 };
                if (variant != "standard" && variant != "audition-stop") button.Classes.Add(variant);
                if (variant == "audition") { button.Classes.Add("primary"); button.Content = HostIcons.Create("Play", button, 32); }
                if (variant == "audition-stop")
                {
                    button.Classes.Add("audition"); button.Classes.Add("stop");
                    button.Content = HostIcons.Create("Stop", button, 24);
                }
                if (variant == "command") button.Content = HostIcons.Create("Play", button, 16);
                Set(button, state, button.State);
                row.Children.Add(new Border { Width = variant == "audition" ? 160 : 112, Child = button, HorizontalAlignment = HorizontalAlignment.Left }); buttons.Add(button);
            }
            body.Children.Add(row);
        }
        var sliderRow = Row("Slider"); var levelRow = Row("Gain level / drawn"); var textRow = Row("Numeric TextBox");
        var comboRow = Row("ComboBox"); var toggleRow = Row("ToggleButton"); var auditionLevelRow = Row("Audition level / drawn");
        foreach (string state in states)
        {
            var slider = new StateSlider { Width = 112, Minimum = 0, Maximum = 1, Value = .6 };
            Set(slider, state, slider.State); sliderRow.Children.Add(slider); sliders.Add(slider);
            var level = new GainLevelControl(shell) { Width = 112, Height = 30, Focusable = true, Amplitude = .6m, IsEnabled = state != "disabled" };
            levelRow.Children.Add(level);
            auditionLevelRow.Children.Add(new GainLevelControl(shell) { SizeRole = "AuditionLevel", Width = 128, Height = 40,
                Focusable = true, Amplitude = .6m, IsEnabled = state != "disabled" });
            var text = new StateTextBox { Text = "0.60", Width = 112, Classes = { "numeric" } };
            Set(text, state, text.State); if (state == "invalid") text.Classes.Add("invalid"); textRow.Children.Add(text); inputs.Add(text);
            comboRow.Children.Add(new ComboBox { Width = 112, ItemsSource = new[] { "Source", "Gain", "Output" }, SelectedIndex = 1, IsEnabled = state != "disabled" });
            toggleRow.Children.Add(new ToggleButton { Width = 112, Content = "Toggle", IsChecked = state == "selected", IsEnabled = state != "disabled" });
        }
        foreach (var row in new[] { sliderRow, levelRow, auditionLevelRow, textRow, comboRow, toggleRow }) body.Children.Add(row);
        var disclosures = new List<Expander>();
        var disclosureRow = Row("Expander");
        foreach (double width in new[] { 180d, 260d, 360d })
        {
            var disclosure = new Expander { Width = width, Header = "Disclosure / Curve",
                Content = new TextBlock { Text = "Content", Margin = new(4) }, IsExpanded = width == 260,
                VerticalAlignment = VerticalAlignment.Top };
            disclosures.Add(disclosure); disclosureRow.Children.Add(disclosure);
        }
        body.Children.Add(disclosureRow);
        var statuses = Row("Diagnostics");
        foreach (string state in new[] { "warning", "error", "success" }) statuses.Children.Add(new TextBlock { Text = state, Width = 112, Classes = { state } });
        var menuButton = new Button { Content = "Menu / Tooltip", Width = 150 };
        var tooltip = new ToolTip { Content = "Host tooltip; shared palette and padding." };
        ToolTip.SetTip(menuButton, tooltip);
        menuButton.ContextMenu = new ContextMenu { Items = { new MenuItem { Header = "Open editor" }, new MenuItem { Header = "Disabled", IsEnabled = false }, new MenuItem { Header = "Selected", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true } } };
        statuses.Children.Add(menuButton); body.Children.Add(statuses);
        var window = new Window { Title = "Seqvium component verification", Width = 1100, Height = 750,
            Content = new ScrollViewer { Content = body, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
        window.Show(main);
        var observations = new List<object>();
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync(); app.ApplyTheme(theme);
            await Program.CaptureAsync(window, output, theme + "-catalog");
            foreach (var disclosure in disclosures)
            {
                var header = disclosure.GetVisualDescendants().OfType<ToggleButton>().Single(c => c.Name == "ExpanderHeader");
                var chevron = header.GetVisualDescendants().OfType<Border>().Single(c => c.Name == "ExpandCollapseChevronBorder");
                var position = chevron.TranslatePoint(default, header)!.Value;
                Program.Require(position.X >= 0 && position.Y >= 0 &&
                    position.X + chevron.Bounds.Width <= header.Bounds.Width &&
                    position.Y + chevron.Bounds.Height <= header.Bounds.Height,
                    "Disclosure chevron paints outside its compact header.");
                Program.Require(chevron.Background is ISolidColorBrush clear && clear.Color.A == 0,
                    "Disclosure chevron has a separate raised tile.");
            }
            var acceptedCorner = app.Resources["Control.Corner"];
            var acceptedFill = app.Resources["Control.Fill"];
            var acceptedNode = app.Resources["Graph.Node"];
            var acceptedAuditionCorner = app.Resources["Audition.Corner"];
            try
            {
                app.Resources["Control.Corner"] = new CornerRadius(7);
                app.Resources["Control.Fill"] = new SolidColorBrush(Colors.Magenta);
                app.Resources["Graph.Node"] = new SolidColorBrush(Colors.Magenta);
                await Program.CaptureAsync(window, output, theme + "-controlled-change");
                Program.Require(buttons.Where(b => !b.Classes.Contains("audition")).All(b => b.CornerRadius == new CornerRadius(7)),
                    "Common button radius failed to propagate.");
                app.Resources["Audition.Corner"] = new CornerRadius(9);
                Program.Require(buttons.Where(b => b.Classes.Contains("audition")).All(b => b.CornerRadius == new CornerRadius(9)),
                    "Audition variant radius failed to propagate.");
                foreach (var slider in sliders.Where(s => s.IsEnabled && !s.Classes.Contains("invalid")))
                {
                    var fill = slider.GetVisualDescendants().OfType<RepeatButton>().Single(c => c.Name == "PART_DecreaseButton");
                    Program.Require(fill.Background is ISolidColorBrush b && b.Color == Colors.Magenta, "Common slider fill failed to propagate.");
                }
                var invalid = sliders.Single(s => s.Classes.Contains("invalid")).GetVisualDescendants().OfType<RepeatButton>().Single(c => c.Name == "PART_DecreaseButton");
                Program.Require(invalid.Background is ISolidColorBrush error && error.Color == Color.Parse(HostPalette.For(theme)["Status.Critical"]), "Invalid slider exception lost its semantic error role.");
                foreach (var level in levelRow.Children.Concat(auditionLevelRow.Children).OfType<GainLevelControl>())
                    Program.Require(HostVisual.Brush(level, theme, "Control.Fill") is ISolidColorBrush b && b.Color == Colors.Magenta, "Drawn level failed to consume live fill.");
            }
            finally { app.Resources["Control.Corner"] = acceptedCorner; app.Resources["Audition.Corner"] = acceptedAuditionCorner;
                app.Resources["Control.Fill"] = acceptedFill; app.Resources["Graph.Node"] = acceptedNode; }
            await Program.CaptureAsync(window, output, theme + "-restored");
            inputs[3].Focus();
            menuButton.ContextMenu.Open(menuButton);
            await Program.CapturePopupAsync(menuButton.ContextMenu, output, theme + "-menu"); menuButton.ContextMenu.Close();
            ToolTip.SetIsOpen(menuButton, true);
            await Program.CapturePopupAsync(tooltip, output, theme + "-tooltip"); ToolTip.SetIsOpen(menuButton, false);
            observations.Add(new { Theme = theme.ToString(), Buttons = buttons.Select(b => new { b.Bounds, b.CornerRadius, b.Classes }),
                Inputs = inputs.Select(i => new { i.Bounds, i.Padding }), ResourceMutationRestored = true,
                SliderAndDrawingPropagation = true, States = states });
        }
        Program.Json(output, "catalog", new { Observations = observations, PseudoStatesAreFixtures = true, NativeMouseClaim = false });
        window.Close();
    }
    private static StackPanel Row(string name)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock { Text = name, Width = 160, VerticalAlignment = VerticalAlignment.Center }); return row;
    }
    private static void Set(Control control, string state, Action<string> pseudo)
    {
        if (state == "invalid") control.Classes.Add("invalid");
        if (state == "disabled") control.IsEnabled = false;
        if (state is "hover" or "pressed" or "focus" or "selected" or "invalid")
            pseudo(state switch { "hover" => ":pointerover", "focus" => ":focus-visible", "invalid" => ":error", "selected" => ":checked", _ => ":pressed" });
    }
}
