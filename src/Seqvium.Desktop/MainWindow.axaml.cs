// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop;

internal sealed partial class MainWindow : Window
{
    private readonly ShellSession _session;
    private readonly App _app;
    private Task _preferenceWork = Task.CompletedTask;
    private bool _systemMenuRequested;
    private readonly WorkspaceHost _workspace;
    private readonly WorkspaceLayoutPersistence _layoutPersistence;
    private readonly bool _layoutFallback;
    private bool _closing;
    private bool _shutdownComplete;

    public MainWindow(ShellSession session, App app, WorkspaceLayoutStore layoutStore, WorkspaceLayoutLoad layout)
    {
        _session = session;
        _app = app;
        InitializeComponent();
        DataContext = session;
        InitializeWorkflow();
        _layoutFallback = layout.UsedFallback;
        _layoutPersistence = new(layoutStore.SaveAsync);
        _workspace = new(session, new(layout.Layout), ToggleLanguageAsync, ToggleThemeAsync);
        WorkspaceRegion.Child = _workspace;
        _workspace.LeaveRequested += () => PaneMenuAction.Focus();
        _workspace.LayoutCommitted += () => _layoutPersistence.Request(_workspace.State.Capture());
        _layoutPersistence.Completed += () => session.SetLayoutNotice(_layoutFallback ? "Workspace.LoadFallback" :
            _layoutPersistence.LastWriteSucceeded ? null : "Workspace.SaveFailed");
        if (_layoutFallback) session.SetLayoutNotice("Workspace.LoadFallback");
        AddHandler(KeyDownEvent, WindowKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, WindowKeyUp, RoutingStrategies.Tunnel);
        Deactivated += (_, _) =>
        {
            _systemMenuRequested = false;
            _workspace.CancelGestures();
        };
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
            _workspace.Dispose();
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
        MaximizeAction.Content = HostIcons.Create(WindowState == WindowState.Maximized ? "Restore" : "Maximize", MaximizeAction, 16);
        AutomationProperties.SetName(MaximizeAction, label);
        ToolTip.SetTip(MaximizeAction, label);
    }

    private void MinimizeClicked(object? sender, RoutedEventArgs args) => WindowState = WindowState.Minimized;

    private void MaximizeClicked(object? sender, RoutedEventArgs args) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseClicked(object? sender, RoutedEventArgs args) => Close();

    private void WindowKeyDown(object? sender, KeyEventArgs args)
    {
        if (!_closing && ((args.Key == Key.F6 && args.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift) ||
                          (args.Key == Key.Tab &&
                           args.KeyModifiers is KeyModifiers.Control or (KeyModifiers.Control | KeyModifiers.Shift))))
        {
            _workspace.CancelGestures();
            var id = _workspace.State.SwitchPane(args.KeyModifiers.HasFlag(KeyModifiers.Shift));
            _workspace.FocusPane(id);
            _workspace.Commit();
            args.Handled = true;
        }

        if (!_closing && args.Key == Key.W && args.KeyModifiers == KeyModifiers.Control &&
            _workspace.IsKeyboardFocusWithin && _workspace.State.ActivePaneId is { } active)
        {
            _workspace.Action(active, () => _workspace.State.Hide(active));
            args.Handled = true;
        }

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

    private Task ToggleLanguageAsync()
    {
        if (_closing) return Task.CompletedTask;
        _preferenceWork = _session.ToggleLanguageAsync();
        return _preferenceWork;
    }

    private Task ToggleThemeAsync()
    {
        if (_closing) return Task.CompletedTask;
        _preferenceWork = _session.ToggleThemeAsync();
        return _preferenceWork;
    }

    private async void LanguageClicked(object? sender, RoutedEventArgs args) => await ToggleLanguageAsync();
    private async void ThemeClicked(object? sender, RoutedEventArgs args) => await ToggleThemeAsync();

    private void InspectorClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.InspectorId);
    private void AppearanceClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.AppearanceId);

    private void OpenPane(string id)
    {
        if (_closing) return;
        PaneMenuAction.Flyout?.Hide();
        _workspace.Open(id);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_shutdownComplete) return;
        args.Cancel = true;
        if (_closing) return;
        _closing = true;
        _importCancellation?.Cancel();
        _workflowControls.Refresh(true);
        await _workflowWork;
        var decision = await ReplacementDecisionAsync();
        if (!await _workflow.CloseAsync(decision.Decision, decision.SavePath))
        {
            _closing = false;
            _workflowControls.Refresh();
            return;
        }
        _workflow.Changed -= WorkflowChanged;
        await _workflow.DisposeAsync();
        _workspace.PrepareShutdown();
        _layoutPersistence.Request(_workspace.State.Capture());
        await _layoutPersistence.ShutdownAsync();
        await _preferenceWork;
        _shutdownComplete = true;
        Close();
    }
}
