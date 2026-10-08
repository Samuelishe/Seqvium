// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop;

internal sealed partial class MainWindow : Window
{
    private readonly ShellSession _session;
    private readonly App _app;
    private Task _preferenceWork = Task.CompletedTask;
    private bool _systemMenuRequested;

    public MainWindow(ShellSession session, App app)
    {
        _session = session;
        _app = app;
        InitializeComponent();
        DataContext = session;
        AddHandler(KeyDownEvent, WindowKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, WindowKeyUp, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => _systemMenuRequested = false;
        if (OperatingSystem.IsWindows())
        {
            // Retain WS_SYSMENU/caption semantics while extending content through the title bar.
            WindowDecorations = WindowDecorations.Full;
            WindowDecorationsTheme = (Avalonia.Styling.ControlTheme?)Resources["HostChrome"];
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = 0;
            WindowDecorationProperties.SetElementRole(TitleRegion, WindowDecorationsElementRole.TitleBar);
            WindowDecorationProperties.SetElementRole(MaximizeAction, WindowDecorationsElementRole.MaximizeButton);
        }
        else
        {
            // Keep native move/resize/keyboard behavior until each desktop's custom chrome is evidenced.
            WindowActions.IsVisible = false;
        }

        session.PropertyChanged += SessionChanged;
        RefreshChrome();
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            session.PropertyChanged -= SessionChanged;
            session.Dispose();
        };
    }

    private void SessionChanged(object? sender, PropertyChangedEventArgs args)
    {
        _app.ApplyTheme(_session.Preferences.Theme);
        RefreshChrome();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if ((change.Property == IsActiveProperty || change.Property == WindowStateProperty) &&
            DataContext is ShellSession)
            RefreshChrome();
    }

    private void RefreshChrome()
    {
        Title = $"{_session.ProjectName} — Seqvium";
        ToolTip.SetTip(TitleRegion, _session[IsActive ? "Window.Active" : "Window.Inactive"]);
        BrandName.Foreground = _app.Resources[IsActive ? "Text.Primary" : "Text.Secondary"] as Avalonia.Media.IBrush;
        Chrome.BorderBrush = _app.Resources[IsActive ? "Focus.Active" : "Border.Default"] as Avalonia.Media.IBrush;
        var label = _session[WindowState == WindowState.Maximized ? "Window.Restore" : "Window.Maximize"];
        MaximizeAction.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        AutomationProperties.SetName(MaximizeAction, label);
        ToolTip.SetTip(MaximizeAction, label);
    }

    private void MinimizeClicked(object? sender, RoutedEventArgs args) => WindowState = WindowState.Minimized;

    private void MaximizeClicked(object? sender, RoutedEventArgs args) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseClicked(object? sender, RoutedEventArgs args) => Close();

    private void WindowKeyDown(object? sender, KeyEventArgs args)
    {
        if (OperatingSystem.IsWindows() && args.Key == Key.Space && args.KeyModifiers == KeyModifiers.Alt)
        {
            args.Handled = true;
            _systemMenuRequested = true;
        }
    }

    private void WindowKeyUp(object? sender, KeyEventArgs args)
    {
        if (!_systemMenuRequested) return;
        if (args.Key == Key.Space) args.Handled = true;
        if (args.Key is not (Key.LeftAlt or Key.RightAlt)) return;
        _systemMenuRequested = false;
        args.Handled = true;
        // Open after Alt is released: its key-up otherwise dismisses the native popup immediately.
        WindowsWindowMenu.Show(this);
    }

    private async void LanguageClicked(object? sender, RoutedEventArgs args)
    {
        _preferenceWork = _session.ToggleLanguageAsync();
        await _preferenceWork;
    }

    private async void ThemeClicked(object? sender, RoutedEventArgs args)
    {
        _preferenceWork = _session.ToggleThemeAsync();
        await _preferenceWork;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_preferenceWork.IsCompleted) return;
        args.Cancel = true;
        await _preferenceWork;
        Close();
    }
}
