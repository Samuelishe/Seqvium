// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>Ordered retained sidebars outside the musical canvas; one shared boundary implementation.</summary>
internal sealed class LeftPanelZone : Grid, IDisposable
{
    private readonly Window _window;
    private readonly WorkspaceHost _workspace;
    private readonly WorkspaceRail _rail;
    private readonly Action<bool> _visibilityChanged;
    private BrowserPaneContent? _browser;
    private readonly Grid _browserBody;
    private readonly Action _unsubscribeTitle;
    private sealed record Panel(SidePanelHost Host, Func<bool> IsOpen);
    private readonly Panel[] _panels;
    private Dictionary<SidePanelHost, double>? _fixedWidths;
    private Dictionary<SidePanelHost, SidePanelPlacement>? _fixedPlacements;
    private SidePanelHost? _resizing;
    private SidePanelPlacement? _beforeResize;
    private double _beforeWidth;
    private SidePanelHost[] _lastOpen = [];
    private double _lastBudget;
    internal SidePanelHost Patterns { get; }
    internal SidePanelHost Browser { get; }
    internal SidePanelPlacement Placement => Patterns.Placement;
    internal bool HasGesture => Patterns.HasGesture || Browser.HasGesture;
    private bool _disposed, _browserVisible, _initialized;
    private bool? _patternsVisible;

    internal LeftPanelZone(Window window, ShellSession shell, WorkspaceHost workspace,
        WorkspaceRail rail, PatternNavigator patterns, WorkspaceLayout? saved, Action<bool> visibilityChanged)
    {
        _window = window; _workspace = workspace; _rail = rail; _visibilityChanged = visibilityChanged;
        var browserBody = new Grid { RowDefinitions = new("Auto,*") };
        _browserBody = browserBody;
        var title = new TextBlock { Classes = { "caption" },
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        var caption = new Border { Child = title, Margin = new(10, 12, 10, 0) };
        caption.Bind(HeightProperty, this.GetResourceObservable("Control.Height"));
        void Translate(object? sender, PropertyChangedEventArgs args) => title.Text = shell["Pane.Browser"].ToUpperInvariant();
        shell.PropertyChanged += Translate;
        _unsubscribeTitle = () => shell.PropertyChanged -= Translate;
        Translate(null, new(null));
        browserBody.Children.Add(caption);
        browserBody.AddHandler(PointerPressedEvent, (_, _) => workspace.Activate(WorkspaceState.BrowserId), RoutingStrategies.Tunnel);
        browserBody.AddHandler(GotFocusEvent, (_, _) => workspace.Activate(WorkspaceState.BrowserId));
        double oldBrowserWidth = saved?.Panes.FirstOrDefault(p => p.InstanceId == WorkspaceState.BrowserId) is { } old
            ? old.DockedWidth ?? old.FloatingBounds.Width : 320;
        Browser = new(window, shell, workspace, browserBody, rail.BrowserToggle, "Browser.Resize", 260,
            saved?.BrowserPanel ?? new(Math.Clamp(oldBrowserWidth, 260, 480)), Refresh, BeginResize,
            () => workspace.Action(WorkspaceState.BrowserId, () => workspace.State.Collapse(WorkspaceState.BrowserId), false));
        Patterns = new(window, shell, workspace, patterns, rail.NavigationToggle, "Navigator.Resize", 180,
            saved?.Navigation ?? new(), Refresh, BeginResize, () => TogglePatterns(null, new()));
        _panels = [new(Browser, () => _browserVisible), new(Patterns, () => !Patterns.Placement.Collapsed)];
        ColumnDefinitions.Add(new(GridLength.Auto));
        Children.Add(rail);
        foreach (var panel in _panels)
        {
            ColumnDefinitions.Add(new(GridLength.Auto));
            SetColumn(panel.Host, Children.Count); Children.Add(panel.Host);
        }
        patterns.Width = double.NaN;
        rail.NavigationToggle.Click += TogglePatterns;
        workspace.State.Changed += Adapt;
        workspace.FocusSidebar = _ => _browser?.FocusBrowser();
        window.SizeChanged += WindowResized;
        Adapt();
    }

    private void BeginResize(SidePanelHost panel)
    {
        Cancel(); _workspace.CancelGestures();
        _fixedWidths = _panels.ToDictionary(p => p.Host, p => Math.Max(0, p.Host.Width - p.Host.GripWidth));
        _fixedPlacements = _panels.ToDictionary(p => p.Host, p => p.Host.Placement);
        _resizing = panel; _beforeResize = panel.Placement; _beforeWidth = _fixedWidths[panel];
    }

    private void TogglePatterns(object? sender, RoutedEventArgs args)
    {
        Cancel(); Patterns.Toggle(); Refresh(); _workspace.Commit();
    }

    private void Adapt()
    {
        if (_disposed || _workspace.State.HasPendingLayout) return;
        bool visible = _workspace.State.Get(WorkspaceState.BrowserId).Visibility == PaneVisibility.Visible;
        if (!_initialized || _browserVisible != visible)
        {
            Cancel();
            if (visible)
            {
                if (_browser is null)
                {
                    _browser = (BrowserPaneContent)_workspace.CreateContent(WorkspaceState.Definition(WorkspaceState.BrowserId));
                    SetRow(_browser, 1); _browserBody.Children.Add(_browser);
                }
                Browser.Restore(_initialized); _browser.Resume();
            }
            else { _browser?.Suspend(); Browser.Hide(); }
            _browserVisible = visible;
        }
        _initialized = true;
        Refresh();
    }

    private void Refresh()
    {
        if (_disposed) return;
        double budget = Math.Max(0, _window.Bounds.Width - HostMetrics.Values["Rail.Width"] - 260);
        var open = _panels.Where(p => p.IsOpen()).ToArray();
        if (_lastBudget != budget || !_lastOpen.SequenceEqual(open.Select(p => p.Host)))
        {
            _fixedWidths = null; _resizing = null;
            _lastBudget = budget; _lastOpen = open.Select(p => p.Host).ToArray();
        }
        if (_fixedWidths is not null && _resizing is { } resizing)
        {
            if (resizing.HasGesture || resizing.Placement != _beforeResize)
                _fixedWidths[resizing] = resizing.RequestedWidth;
            else _fixedWidths[resizing] = _beforeWidth;
            if (!resizing.HasGesture) _resizing = null;
        }
        if (_fixedWidths is not null && _fixedPlacements is not null)
            foreach (var panel in _panels)
                if (_fixedPlacements[panel.Host] != panel.Host.Placement)
                {
                    _fixedWidths[panel.Host] = panel.Host.RequestedWidth;
                    _fixedPlacements[panel.Host] = panel.Host.Placement;
                }
        double Requested(Panel p) => _fixedWidths?.GetValueOrDefault(p.Host) ?? p.Host.RequestedWidth;
        double requested = open.Sum(Requested);
        // Auto-fitting under window pressure never commits a smaller preferred width.
        double grips = open.Sum(p => p.Host.GripWidth);
        double available = Math.Max(0, budget - grips);
        double scale = requested > available ? available / requested : 1;
        foreach (var panel in _panels)
        {
            double width = panel.IsOpen() ? Requested(panel) * scale : 0;
            panel.Host.Show(panel.IsOpen() || panel.Host.HasGesture, width,
                Math.Min(480, Math.Max(0, available - requested * scale + width)));
        }
        bool shown = Patterns.ContentVisible;
        if (_browser is not null)
        {
            if (Browser.ContentVisible) _browser.Resume();
            else _browser.Suspend();
        }
        if (_patternsVisible != shown) { _patternsVisible = shown; _visibilityChanged(shown); }
    }

    private void WindowResized(object? sender, SizeChangedEventArgs args)
    {
        Cancel();
        Dispatcher.UIThread.Post(() => { if (!_disposed) Refresh(); }, DispatcherPriority.Loaded);
    }

    internal void Cancel() { foreach (var panel in _panels) panel.Host.Cancel(); }

    public void Dispose()
    {
        Cancel(); _disposed = true;
        _window.SizeChanged -= WindowResized; _workspace.State.Changed -= Adapt;
        _workspace.FocusSidebar = null; _rail.NavigationToggle.Click -= TogglePatterns;
        _unsubscribeTitle(); foreach (var panel in _panels) panel.Host.Dispose(); _browser?.Dispose();
    }
}

/// <summary>Resize, clip, collapse and restore for a sidebar. Drafts stay outside saved layout.</summary>
internal sealed class SidePanelHost : Grid, IDisposable
{
    private readonly Window _window;
    private readonly ShellSession _shell;
    private readonly WorkspaceHost _workspace;
    private readonly Button _toggle;
    private readonly string _resizeKey;
    private readonly double _minimum;
    private readonly Action _changed, _collapse;
    private readonly Action<SidePanelHost> _begin;
    private readonly Border _viewport = new() { ClipToBounds = true };
    private readonly Grid _body = new() { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
    private readonly Border _grip = new() { Focusable = true, Background = Brushes.Transparent };
    private readonly Border _line = new() { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
    private readonly Cursor _cursor = new(StandardCursorType.SizeWestEast);
    private IPointer? _pointer;
    private Point _start;
    private double _startWidth, _draftWidth, _maximum = 480, _visibleWidth;
    private bool _hover, _moved;
    private string? _gripRole;
    internal SidePanelPlacement Placement { get; private set; }
    internal bool HasGesture => _pointer is not null;
    internal bool ContentVisible => _viewport.IsVisible;
    internal double RequestedWidth => HasGesture ? _draftWidth : Placement.VisibleWidth ?? Placement.Width;
    internal double GripWidth => HostMetrics.Values["Space.Medium"];

    internal SidePanelHost(Window window, ShellSession shell, WorkspaceHost workspace, Control content, Button toggle,
        string resizeKey, double minimum, SidePanelPlacement saved, Action changed, Action<SidePanelHost> begin, Action collapse)
    {
        _window = window; _shell = shell; _workspace = workspace; _toggle = toggle; _resizeKey = resizeKey;
        _minimum = minimum; _changed = changed; _begin = begin; _collapse = collapse;
        Placement = saved.IsValid ? saved with { Width = Math.Max(minimum, saved.Width) } : new(minimum);
        Width = 0; _viewport.IsVisible = _grip.IsVisible = false;
        ColumnDefinitions = new("*,Auto");
        _body.Children.Add(content); _viewport.Child = _body; Children.Add(_viewport);
        _grip.Cursor = _cursor;
        _grip.Bind(WidthProperty, this.GetResourceObservable("Space.Medium"));
        _line.Bind(WidthProperty, this.GetResourceObservable("Border.Width"));
        _grip.Child = _line; SetColumn(_grip, 1); Children.Add(_grip);
        AutomationProperties.SetAutomationId(_grip, resizeKey);
        _grip.PointerEntered += (_, _) => { _hover = true; RefreshGrip(); };
        _grip.PointerExited += (_, _) => { _hover = false; RefreshGrip(); };
        _grip.GotFocus += (_, _) => RefreshGrip(); _grip.LostFocus += (_, _) => RefreshGrip();
        _grip.PointerPressed += Pressed; _grip.PointerMoved += Moved; _grip.PointerReleased += Released;
        _grip.PointerCaptureLost += (_, _) => Cancel(); _grip.KeyDown += GripKeyDown;
        shell.PropertyChanged += PreferencesChanged;
        window.Deactivated += Deactivated;
        window.AddHandler(KeyDownEvent, WindowKeyDown, RoutingStrategies.Tunnel);
        PreferencesChanged(null, new(null));
    }

    internal void Restore(bool restoreWidth = true) => Placement = Placement with
        { Collapsed = false, VisibleWidth = restoreWidth ? null : Placement.VisibleWidth };
    internal void Hide()
    {
        ReturnFocus(); Placement = Placement with { Collapsed = true, VisibleWidth = null };
    }
    internal void Toggle()
    {
        if (Placement.Collapsed) Restore();
        else Hide();
    }

    internal void Show(bool open, double width, double maximum)
    {
        _maximum = maximum;
        _visibleWidth = open ? (HasGesture && _draftWidth <= SidePanelPlacement.CollapseWidth ? 0 : width) : 0;
        bool visible = open && _visibleWidth > 0;
        if (!visible) ReturnFocus();
        _viewport.IsVisible = visible;
        _body.Children[0].IsVisible = visible;
        _grip.IsVisible = open && (visible || HasGesture);
        Width = _visibleWidth + (_grip.IsVisible ? GripWidth : 0);
        _body.Width = Math.Max(_minimum, _visibleWidth);
        _toggle.Classes.Set("active", visible);
        RefreshGrip();
    }

    internal void ReturnFocus()
    {
        if (_viewport.IsKeyboardFocusWithin || _grip.IsKeyboardFocusWithin) _toggle.Focus();
    }

    private void PreferencesChanged(object? sender, PropertyChangedEventArgs args)
    {
        ToolTip.SetTip(_grip, _shell[_resizeKey]); AutomationProperties.SetName(_grip, _shell[_resizeKey]);
    }

    private void RefreshGrip()
    {
        string role = _hover || HasGesture || _grip.IsKeyboardFocusWithin ? "Accent.Action" : "Border.Default";
        if (_gripRole == role) return;
        _gripRole = role; _line.Bind(BackgroundProperty, this.GetResourceObservable(role));
    }

    private void Pressed(object? sender, PointerPressedEventArgs args)
    {
        if (HasGesture || !args.GetCurrentPoint(_grip).Properties.IsLeftButtonPressed) return;
        _begin(this);
        _start = args.GetPosition(_window); _startWidth = _draftWidth = _visibleWidth; _moved = false;
        _pointer = args.Pointer; args.Pointer.Capture(_grip); args.Handled = true; RefreshGrip();
    }

    private void Moved(object? sender, PointerEventArgs args)
    {
        if (args.Pointer != _pointer) return;
        if (!args.GetCurrentPoint(_grip).Properties.IsLeftButtonPressed) { Cancel(); return; }
        double next = Math.Clamp(_startWidth + args.GetPosition(_window).X - _start.X, 0, _maximum);
        if (next <= 0 || next >= _maximum) { _start = args.GetPosition(_window); _startWidth = next; }
        _moved |= next != _draftWidth;
        _draftWidth = next; _changed(); args.Handled = true;
    }

    private void Released(object? sender, PointerReleasedEventArgs args)
    {
        if (args.Pointer != _pointer || args.InitialPressMouseButton != MouseButton.Left) return;
        var pointer = _pointer; _pointer = null;
        if (_moved)
        {
            if (_draftWidth <= SidePanelPlacement.CollapseWidth) _collapse();
            else Placement = _draftWidth >= _minimum ? new(_draftWidth) : Placement with { VisibleWidth = _draftWidth };
        }
        pointer.Capture(null); args.Handled = true; _changed(); _workspace.Commit();
    }

    internal void Cancel()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null; pointer.Capture(null); _changed();
    }

    private void WindowKeyDown(object? sender, KeyEventArgs args)
    {
        if (HasGesture && args.Key == Key.Escape) { Cancel(); args.Handled = true; }
    }

    private void GripKeyDown(object? sender, KeyEventArgs args)
    {
        if (HasGesture || args.KeyModifiers != KeyModifiers.None || args.Key is not (Key.Left or Key.Right or Key.Home)) return;
        double next = Math.Clamp(_visibleWidth + (args.Key == Key.Left ? -16 : 16), 0, _maximum);
        if (args.Key == Key.Home || next <= SidePanelPlacement.CollapseWidth) _collapse();
        else Placement = next >= _minimum ? new(next) : Placement with { VisibleWidth = next };
        _changed(); _workspace.Commit(); args.Handled = true;
    }

    private void Deactivated(object? sender, EventArgs args) => Cancel();
    public void Dispose()
    {
        Cancel(); _shell.PropertyChanged -= PreferencesChanged; _window.Deactivated -= Deactivated;
        _window.RemoveHandler(KeyDownEvent, WindowKeyDown); _cursor.Dispose();
    }
}
