// SPDX-License-Identifier: Apache-2.0

using System.Collections.Frozen;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Seqvium.Desktop.Presentation;

/// <summary>Compatibility aliases for the installed Fluent templates, never an independent palette.
/// Application resource changes synchronize aliases; templates keep their native input behavior.</summary>
internal sealed class HostFluentResources
{
    internal static IReadOnlyDictionary<string, string> Aliases { get; } = BuildAliases();
    private readonly Application _app;
    private bool _updating;

    public HostFluentResources(Application app)
    {
        _app = app;
        ((IResourceHost)app).ResourcesChanged += (_, _) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        if (_updating) return;
        _updating = true;
        try
        {
            foreach (var (alias, role) in Aliases)
            {
                if (!_app.Resources.TryGetResource(role, _app.ActualThemeVariant, out var value)) continue;
                // These two Fluent keys require Color rather than IBrush.
                if (alias is "ScrollBarThumbBackgroundColor" or "TextControlSelectionHighlightColor" &&
                    value is ISolidColorBrush brush)
                    value = brush.Color;
                if (!_app.Resources.TryGetResource(alias, _app.ActualThemeVariant, out var existing) ||
                    !Equals(existing, value))
                    _app.Resources[alias] = value;
            }
        }
        finally
        {
            _updating = false;
        }
    }

    private static FrozenDictionary<string, string> BuildAliases()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        void Add(string role, params string[] keys)
        {
            foreach (var key in keys) result.Add(key, role);
        }

        void States(string prefix, string normal, string hover, string pressed, string disabled)
        {
            Add(normal, prefix);
            Add(hover, prefix + "PointerOver");
            Add(pressed, prefix + "Pressed");
            Add(disabled, prefix + "Disabled");
        }

        foreach (var prefix in new[]
                 {
                     "Button", "ToggleButton", "ComboBox", "TextControlButton", "MenuFlyoutItem", "ScrollBarButton",
                     "ExpanderHeader", "ComboBoxItem"
                 })
        {
            States(prefix + "Background", "Surface.Raised", "Surface.Hover", "Surface.Pressed", "Surface.Background");
            States(prefix + "BorderBrush", "Border.Default", "Accent.Action", "Accent.Action", "Border.Default");
            States(prefix + "Foreground", "Text.Primary", "Text.Primary", "Text.Primary", "State.Unavailable");
        }

        // The chevron is part of the compact header, not a second raised button inside it.
        States("ExpanderChevronBackground", "Material.Clear", "Material.Clear", "Material.Clear", "Material.Clear");
        States("ExpanderChevronBorderBrush", "Material.Clear", "Material.Clear", "Material.Clear", "Material.Clear");
        States("ExpanderChevronForeground", "Text.Primary", "Text.Primary", "Text.Primary", "State.Unavailable");

        Add("Surface.Selected", "ToggleButtonBackgroundChecked", "ToggleButtonBackgroundCheckedPointerOver",
            "ToggleButtonBackgroundCheckedPressed");
        foreach (string suffix in new[] { "", "PointerOver", "Pressed", "Disabled" })
        {
            Add(suffix == "Disabled" ? "Surface.Background" : "Surface.Selected",
                "ComboBoxItemBackgroundSelected" + suffix);
            Add(suffix == "Disabled" ? "State.Unavailable" : "Text.Primary",
                "ComboBoxItemForegroundSelected" + suffix);
            Add(suffix == "Disabled" ? "Border.Default" : "Accent.Action",
                "ComboBoxItemBorderBrushSelected" + suffix);
        }

        Add("Padding.Control", "ComboBoxItemThemePadding");
        Add("Surface.Background", "ToggleButtonBackgroundCheckedDisabled", "TextControlBackground",
            "TextControlBackgroundFocused", "TextControlBackgroundDisabled", "ExpanderContentBackground");
        Add("Surface.Hover", "TextControlBackgroundPointerOver");
        Add("Text.Primary", "TextControlForeground", "TextControlForegroundFocused", "TextControlForegroundPointerOver",
            "ComboBoxForegroundFocused", "ComboBoxForegroundFocusedPressed", "ComboBoxDropDownGlyphForeground",
            "ComboBoxDropDownGlyphForegroundFocused", "ComboBoxDropDownGlyphForegroundFocusedPressed");
        Add("State.Unavailable", "TextControlForegroundDisabled", "ComboBoxDropDownGlyphForegroundDisabled",
            "ToggleButtonForegroundCheckedDisabled");
        Add("Text.Primary", "ToggleButtonForegroundChecked", "ToggleButtonForegroundCheckedPointerOver",
            "ToggleButtonForegroundCheckedPressed");
        Add("Accent.Action", "ToggleButtonBorderBrushChecked", "ToggleButtonBorderBrushCheckedPointerOver",
            "ToggleButtonBorderBrushCheckedPressed");
        Add("Border.Default", "ToggleButtonBorderBrushCheckedDisabled", "TextControlBorderBrush",
            "TextControlBorderBrushDisabled", "ComboBoxDropDownBorderBrush", "ExpanderContentBorderBrush",
            "MenuFlyoutPresenterBorderBrush", "ScrollBarBorderBrush");
        Add("Accent.Action", "TextControlBorderBrushPointerOver");
        Add("Accent.Action", "TabItemHeaderSelectedPipeFill", "TabItemHeaderForegroundSelected",
            "TabItemHeaderForegroundSelectedPointerOver", "TabItemHeaderForegroundSelectedPressed");
        Add("Text.Secondary", "TabItemHeaderForegroundUnselected");
        Add("Text.Primary", "TabItemHeaderForegroundUnselectedPointerOver", "TabItemHeaderForegroundUnselectedPressed");
        Add("Surface.Raised", "TabItemHeaderBackgroundUnselected", "TabItemHeaderBackgroundSelected");
        Add("Surface.Hover", "TabItemHeaderBackgroundUnselectedPointerOver",
            "TabItemHeaderBackgroundSelectedPointerOver");
        Add("Surface.Pressed", "TabItemHeaderBackgroundUnselectedPressed", "TabItemHeaderBackgroundSelectedPressed");
        Add("Surface.Background", "TabItemHeaderBackgroundDisabled");
        Add("State.Unavailable", "TabItemHeaderForegroundDisabled");
        Add("Type.Body", "TabItemHeaderFontSize");
        Add("Padding.Content", "TabItemHeaderMargin");
        Add("Control.Height", "TabItemMinHeight");
        Add("Focus.Active", "TextControlBorderBrushFocused", "ComboBoxBackgroundBorderBrushFocused",
            "TextControlSelectionHighlightColor");
        Add("Border.Default", "ComboBoxBackgroundBorderBrushUnfocused");
        Add("Surface.Raised", "ComboBoxDropDownBackground", "MenuFlyoutPresenterBackground",
            "ComboBoxBackgroundUnfocused");
        Add("Text.Secondary", "TextControlPlaceholderForeground", "ComboBoxPlaceHolderForeground",
            "ComboBoxPlaceHolderForegroundFocusedPressed", "MenuFlyoutItemKeyboardAcceleratorTextForeground",
            "MenuFlyoutSubItemChevron");
        Add("Status.Critical", "SystemControlErrorTextForegroundBrush");
        foreach (var suffix in new[] { "", "PointerOver", "Pressed", "Disabled" })
        {
            Add("Surface.Workspace", "SliderContainerBackground" + suffix);
            Add("Control.Track", "SliderTrackFill" + suffix);
            Add(suffix == "Disabled" ? "State.Unavailable" : "Control.Fill", "SliderTrackValueFill" + suffix);
            Add(suffix == "Disabled" ? "State.Unavailable" : suffix == "PointerOver" ? "Focus.Active" : "Control.Thumb",
                "SliderThumbBackground" + suffix);
        }

        Add("Border.Default", "SliderTickBarFill");
        Add("State.Unavailable", "SliderTickBarFillDisabled");
        Add("Control.Track", "ScrollBarForeground", "ScrollBarPanningThumbBackground", "ScrollBarThumbBackgroundColor",
            "ScrollBarThumbFillPointerOver");
        Add("Accent.Action", "ScrollBarThumbFillPressed");
        Add("State.Unavailable", "ScrollBarThumbFillDisabled");
        Add("Surface.Workspace", "ScrollBarBackground", "ScrollBarBackgroundPointerOver", "ScrollBarTrackFill",
            "ScrollBarTrackFillPointerOver", "ScrollBarTrackStroke", "ScrollBarTrackStrokePointerOver");
        Add("Control.Corner", "ControlCornerRadius", "OverlayCornerRadius");
        Add("Control.Height", "ComboBoxMinHeight", "MenuFlyoutThemeMinHeight");
        Add("Control.InputHeight", "TextControlThemeMinHeight");
        Add("Pane.HeaderHeight", "ExpanderMinHeight");
        Add("Control.NumericInnerHeight", "ExpanderChevronButtonSize");
        Add("Padding.Control", "ExpanderChevronMargin");
        Add("Level.Height", "SliderHorizontalHeight", "SliderVerticalWidth");
        Add("Level.TrackHeight", "SliderTrackThemeHeight");
        Add("Level.ThumbSize", "SliderHorizontalThumbWidth", "SliderHorizontalThumbHeight", "SliderVerticalThumbWidth",
            "SliderVerticalThumbHeight");
        Add("ScrollBar.Size", "ScrollBarSize");
        Add("Border.Control", "TextControlBorderThemeThickness", "ComboBoxBorderThemeThickness",
            "MenuFlyoutPresenterBorderThemeThickness", "ExpanderHeaderBorderThickness");
        Add("Border.Focus", "TextControlBorderThemeThicknessFocused");
        Add("Padding.Control", "ButtonPadding", "ComboBoxPadding", "ExpanderHeaderPadding");
        Add("Padding.Input", "TextControlThemePadding");
        Add("Padding.Content", "ExpanderContentPadding", "MenuFlyoutItemThemePaddingNarrow",
            "HorizontalMenuFlyoutItemThemePaddingNarrow");
        return result.ToFrozenDictionary(StringComparer.Ordinal);
    }
}