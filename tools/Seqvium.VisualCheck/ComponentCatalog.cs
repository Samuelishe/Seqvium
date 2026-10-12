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
        foreach (string variant in new[] { "standard", "primary", "compact", "command", "danger" })
        {
            var row = Row(variant + " Button");
            foreach (string state in states)
            {
                var button = new StateButton { Content = "Action", Width = variant == "command" ? double.NaN : 112 };
                if (variant != "standard") button.Classes.Add(variant);
                if (variant == "command") button.Content = HostIcons.Create("Play", button, 16);
                Set(button, state, button.State);
                row.Children.Add(new Border { Width = 112, Child = button, HorizontalAlignment = HorizontalAlignment.Left }); buttons.Add(button);
            }
            body.Children.Add(row);
        }
        var sliderRow = Row("Slider"); var levelRow = Row("Gain level / drawn"); var textRow = Row("Numeric TextBox");
        var comboRow = Row("ComboBox"); var toggleRow = Row("ToggleButton");
        foreach (string state in states)
        {
            var slider = new StateSlider { Width = 112, Minimum = 0, Maximum = 1, Value = .6 };
            Set(slider, state, slider.State); sliderRow.Children.Add(slider); sliders.Add(slider);
            var level = new GainLevelControl(shell) { Width = 112, Height = 30, Focusable = true, Amplitude = .6m, IsEnabled = state != "disabled" };
            levelRow.Children.Add(level);
            var text = new StateTextBox { Text = "0.60", Width = 112, Classes = { "numeric" } };
            Set(text, state, text.State); if (state == "invalid") text.Classes.Add("invalid"); textRow.Children.Add(text); inputs.Add(text);
            comboRow.Children.Add(new ComboBox { Width = 112, ItemsSource = new[] { "Source", "Gain", "Output" }, SelectedIndex = 1, IsEnabled = state != "disabled" });
            toggleRow.Children.Add(new ToggleButton { Width = 112, Content = "Toggle", IsChecked = state == "selected", IsEnabled = state != "disabled" });
        }
        foreach (var row in new[] { sliderRow, levelRow, textRow, comboRow, toggleRow }) body.Children.Add(row);
        body.Children.Add(new Expander { Header = "Disclosure / Curve", Content = new TextBlock { Text = "Localized content retains control focus and input." }, IsExpanded = true });
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
            var acceptedCorner = app.Resources["Control.Corner"];
            var acceptedFill = app.Resources["Control.Fill"];
            var acceptedNode = app.Resources["Graph.Node"];
            try
            {
                app.Resources["Control.Corner"] = new CornerRadius(7);
                app.Resources["Control.Fill"] = new SolidColorBrush(Colors.Magenta);
                app.Resources["Graph.Node"] = new SolidColorBrush(Colors.Magenta);
                await Program.CaptureAsync(window, output, theme + "-controlled-change");
                Program.Require(buttons.All(b => b.CornerRadius == new CornerRadius(7)), "Common button radius failed to propagate.");
                foreach (var slider in sliders.Where(s => s.IsEnabled && !s.Classes.Contains("invalid")))
                {
                    var fill = slider.GetVisualDescendants().OfType<RepeatButton>().Single(c => c.Name == "PART_DecreaseButton");
                    Program.Require(fill.Background is ISolidColorBrush b && b.Color == Colors.Magenta, "Common slider fill failed to propagate.");
                }
                var invalid = sliders.Single(s => s.Classes.Contains("invalid")).GetVisualDescendants().OfType<RepeatButton>().Single(c => c.Name == "PART_DecreaseButton");
                Program.Require(invalid.Background is ISolidColorBrush error && error.Color == Color.Parse(HostPalette.For(theme)["Status.Critical"]), "Invalid slider exception lost its semantic error role.");
                foreach (var level in levelRow.Children.OfType<GainLevelControl>())
                    Program.Require(HostVisual.Brush(level, theme, "Control.Fill") is ISolidColorBrush b && b.Color == Colors.Magenta, "Drawn level failed to consume live fill.");
            }
            finally { app.Resources["Control.Corner"] = acceptedCorner; app.Resources["Control.Fill"] = acceptedFill; app.Resources["Graph.Node"] = acceptedNode; }
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
