// SPDX-License-Identifier: Apache-2.0
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Settings;

namespace Seqvium.Desktop;

internal sealed partial class MainWindow
{
    private SettingsWindow? _settings;
    private MenuFlyout? _themeMenu;

    private async void SettingsClicked(object? sender, RoutedEventArgs args) => await OpenSettingsAsync();

    internal async Task OpenSettingsAsync()
    {
        if (_closing) return;
        if (_settings is { } existing) { existing.Activate(); return; }
        var focus = FocusManager?.GetFocusedElement();
        _workspace.CancelGestures();
        var window = new SettingsWindow(_session, async preferences =>
        {
            _preferenceWork = ApplyAppearanceAsync(preferences);
            await _preferenceWork;
        }) { Width = Math.Min(720, Bounds.Width - 40), Height = Math.Min(570, Bounds.Height - 60) };
        _settings = window;
        try { await window.ShowDialog(this); }
        finally
        {
            _settings = null;
            if (!_closing && focus is Control { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } control)
                control.Focus();
        }
    }

    private Task ApplyAppearanceAsync(HostPreferences preferences)
    {
        // Each call changes one explicit field; serialized stores and joined shutdown own persistence.
        var language = _session.Preferences.Language != preferences.Language
            ? _session.SetLanguageAsync(preferences.Language) : Task.CompletedTask;
        var theme = _session.Preferences.Theme != preferences.Theme
            ? _session.SetThemeAsync(preferences.Theme) : Task.CompletedTask;
        return Task.WhenAll(language, theme);
    }

    private void ThemeClicked(object? sender, RoutedEventArgs args)
    {
        if (_closing) return;
        _themeMenu?.Hide();
        var menu = _themeMenu = new MenuFlyout();
        foreach (var theme in ThemeCatalog.All)
        {
            var item = new MenuItem { Header = _session[theme.LabelKey],
                Icon = HostIcons.Create(theme.Theme == _session.Preferences.Theme ? "Check" : theme.Icon, ThemeAction, 16) };
            item.Click += async (_, _) =>
            {
                _preferenceWork = _session.SetThemeAsync(theme.Theme);
                await _preferenceWork;
            };
            menu.Items.Add(item);
        }
        menu.ShowAt(ThemeAction);
    }
}
