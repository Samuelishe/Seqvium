// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>One retained sidebar boundary. Draft geometry never enters the document or persisted layout.</summary>
internal sealed class PatternNavigationHost : Grid, IDisposable
{
    private readonly Window _window;
    private readonly ShellSession _shell;
    private readonly WorkspaceHost _workspace;
    private readonly WorkspaceRail _rail;
    private readonly PatternNavigator _patterns;
    private readonly Action<bool> _visibilityChanged;
    private readonly Border _grip = new() { Focusable = true, Background = Brushes.Transparent };
    private readonly Border _line = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Cursor _cursor = new(StandardCursorType.SizeWestEast);
    private PatternNavigationPlacement _layout;
    private IPointer? _pointer;
    private Point _start;
    private double _startWidth, _draftWidth;
    private bool _compactOpen, _wasCompact, _hover, _disposed, _moved;
    private bool? _shown;
    private string? _gripRole;
    internal PatternNavigationPlacement Placement => _layout;
    internal bool HasGesture => _pointer is not null;
    private double MaximumWidth => Math.Min(PatternNavigationPlacement.MaximumWidth,
        Math.Max(PatternNavigationPlacement.MinimumWidth,
            _window.Bounds.Width - HostMetrics.Values["Rail.Width"] - HostMetrics.Values["Space.Medium"] - 360));
    private bool BrowserVisible => _workspace.State.Get(WorkspaceState.BrowserId) is
        { Visibility: PaneVisibility.Visible, Dock: PaneDock.Left };

    internal PatternNavigationHost(Window window, ShellSession shell, WorkspaceHost workspace,
        WorkspaceRail rail, PatternNavigator patterns, PatternNavigationPlacement? saved, Action<bool> visibilityChanged)
    {
        _window = window; _shell = shell; _workspace = workspace; _rail = rail; _patterns = patterns;
        _visibilityChanged = visibilityChanged;
        _layout = saved is { IsValid: true } ? saved : new();
        _wasCompact = window.Bounds.Width < 1000;
        ColumnDefinitions = new("Auto,Auto,Auto");
        Children.Add(rail); SetColumn(patterns, 1); Children.Add(patterns);
        patterns.Width = double.NaN;
        _grip.Cursor = _cursor;
        _grip.Bind(WidthProperty, this.GetResourceObservable("Space.Medium"));
        _line.Bind(WidthProperty, this.GetResourceObservable("Border.Width"));
        _grip.Child = _line; SetColumn(_grip, 2); Children.Add(_grip);
        AutomationProperties.SetAutomationId(_grip, "Navigator.Resize");
        _grip.PointerEntered += (_, _) => { _hover = true; RefreshGrip(); };
        _grip.PointerExited += (_, _) => { _hover = false; RefreshGrip(); };
        _grip.GotFocus += (_, _) => RefreshGrip(); _grip.LostFocus += (_, _) => RefreshGrip();
        _grip.PointerPressed += Pressed; _grip.PointerMoved += Moved; _grip.PointerReleased += Released;
        _grip.PointerCaptureLost += (_, _) => Cancel();
        _grip.KeyDown += GripKeyDown;
        rail.NavigationToggle.Click += Toggle;
        workspace.State.Changed += Adapt;
        shell.PropertyChanged += PreferencesChanged;
        window.SizeChanged += WindowResized; window.Deactivated += WindowDeactivated;
        window.AddHandler(KeyDownEvent, WindowKeyDown, RoutingStrategies.Tunnel);
        PreferencesChanged(null, new(null));
        Adapt();
    }

    private void PreferencesChanged(object? sender, PropertyChangedEventArgs args)
    {
        ToolTip.SetTip(_grip, _shell["Navigator.Resize"]);
        AutomationProperties.SetName(_grip, _shell["Navigator.Resize"]);
    }

    private void RefreshGrip()
    {
        string role = _hover || HasGesture || _grip.IsKeyboardFocusWithin ? "Accent.Action" : "Border.Default";
        if (_gripRole == role) return;
        _gripRole = role;
        _line.Bind(BackgroundProperty, this.GetResourceObservable(role));
    }

    private void Adapt()
    {
        if (_disposed) return;
        bool compact = _window.Bounds.Width < 1000;
        if (compact && !_wasCompact) _compactOpen = false;
        _wasCompact = compact;
        bool available = (!compact || _compactOpen) && !BrowserVisible;
        if (HasGesture && !available) Cancel();
        double width = HasGesture ? _draftWidth : Math.Min(_layout.Width, MaximumWidth);
        bool visible = available && (HasGesture ? width > PatternNavigationPlacement.CollapseWidth : !_layout.Collapsed);
        if (!visible && (_patterns.IsKeyboardFocusWithin || _grip.IsKeyboardFocusWithin)) _rail.NavigationToggle.Focus();
        _patterns.IsVisible = visible;
        ColumnDefinitions[1].Width = new(visible ? width : 0);
        _grip.IsVisible = visible || available && HasGesture;
        _rail.NavigationToggle.Classes.Set("active", visible);
        if (_shown != visible) { _shown = visible; _visibilityChanged(visible); }
        RefreshGrip();
    }

    private void Toggle(object? sender, RoutedEventArgs args)
    {
        Cancel();
        bool opening = !_patterns.IsVisible;
        _layout = _layout with { Collapsed = !opening };
        _compactOpen = opening;
        if (opening && BrowserVisible) _workspace.State.Collapse(WorkspaceState.BrowserId);
        Adapt(); _workspace.Commit();
    }

    private void Pressed(object? sender, PointerPressedEventArgs args)
    {
        if (HasGesture || !args.GetCurrentPoint(_grip).Properties.IsLeftButtonPressed) return;
        _workspace.CancelGestures();
        _start = args.GetPosition(_window);
        _startWidth = _draftWidth = ColumnDefinitions[1].Width.Value;
        _moved = false;
        _pointer = args.Pointer;
        args.Pointer.Capture(_grip); args.Handled = true; RefreshGrip();
    }

    private void Moved(object? sender, PointerEventArgs args)
    {
        if (args.Pointer != _pointer) return;
        if (!args.GetCurrentPoint(_grip).Properties.IsLeftButtonPressed) { Cancel(); return; }
        double next = Math.Clamp(_startWidth + args.GetPosition(_window).X - _start.X, 0, MaximumWidth);
        // Rebase at the upper bound so reversal responds immediately after blocked pointer travel.
        if (next >= MaximumWidth) { _start = args.GetPosition(_window); _startWidth = next; }
        _moved |= next != _draftWidth;
        _draftWidth = next; Adapt(); args.Handled = true;
    }

    private void Released(object? sender, PointerReleasedEventArgs args)
    {
        if (args.Pointer != _pointer || args.InitialPressMouseButton != MouseButton.Left) return;
        var pointer = _pointer; _pointer = null;
        if (_moved)
            _layout = _draftWidth <= PatternNavigationPlacement.CollapseWidth
                ? _layout with { Collapsed = true }
                : new(Math.Clamp(_draftWidth, PatternNavigationPlacement.MinimumWidth, MaximumWidth));
        pointer.Capture(null); args.Handled = true;
        Adapt(); _workspace.Commit();
    }

    internal void Cancel()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null; pointer.Capture(null); Adapt();
    }

    private void WindowKeyDown(object? sender, KeyEventArgs args)
    {
        if (HasGesture && args.Key == Key.Escape) { Cancel(); args.Handled = true; }
    }

    private void GripKeyDown(object? sender, KeyEventArgs args)
    {
        if (HasGesture || args.KeyModifiers != KeyModifiers.None || args.Key is not (Key.Left or Key.Right or Key.Home)) return;
        double next = Math.Min(_layout.Width, MaximumWidth) + (args.Key == Key.Left ? -16 : 16);
        _layout = args.Key == Key.Home || next < PatternNavigationPlacement.MinimumWidth
            ? _layout with { Collapsed = true }
            : new(Math.Clamp(next, PatternNavigationPlacement.MinimumWidth, MaximumWidth));
        Adapt(); _workspace.Commit(); args.Handled = true;
    }

    private void WindowDeactivated(object? sender, EventArgs args) => Cancel();
    private void WindowResized(object? sender, SizeChangedEventArgs args)
    {
        Cancel();
        Dispatcher.UIThread.Post(() => { if (!_disposed) Adapt(); }, DispatcherPriority.Loaded);
    }

    public void Dispose()
    {
        Cancel(); _disposed = true;
        _rail.NavigationToggle.Click -= Toggle; _workspace.State.Changed -= Adapt;
        _shell.PropertyChanged -= PreferencesChanged;
        _window.SizeChanged -= WindowResized; _window.Deactivated -= WindowDeactivated;
        _window.RemoveHandler(KeyDownEvent, WindowKeyDown);
        _cursor.Dispose();
    }
}
