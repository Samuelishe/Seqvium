// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>One internal canvas and a reachable strip. Controls live until the host is disposed.</summary>
public sealed class WorkspaceHost : Grid, IDisposable
{
    private readonly ShellSession _session;
    private readonly Canvas _canvas = new() { ClipToBounds = true, Background = Avalonia.Media.Brushes.Transparent };
    private readonly Border _boundaryCue = new() { IsHitTestVisible = false };

    private readonly StackPanel _strip = new()
        { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new(4, 2) };

    private readonly Border _shelf;
    private readonly Dictionary<string, WorkspacePane> _views = [];
    private readonly Dictionary<string, Button> _tabs = [];
    private readonly Func<Task> _language;
    private readonly Func<Task> _theme;
    private readonly DispatcherTimer _resizeSave;
    private bool _disposed;
    private bool _stopping;
    private bool _hasSize;
    private IPointer? _resizePointer;
    private PaneGesture? _resizeGesture;
    private ResizeBoundary? _resizeBoundary;
    private ResizeBoundary? _hoverBoundary;
    private Point? _hoverPosition;
    private readonly Dictionary<StandardCursorType, Cursor> _boundaryCursors = [];
    public WorkspaceState State { get; }
    public event Action? LayoutCommitted;
    public event Action? LeaveRequested;

    public WorkspaceHost(ShellSession session, WorkspaceState state, Func<Task> language, Func<Task> theme)
    {
        _session = session;
        State = state;
        _language = language;
        _theme = theme;
        RowDefinitions = new("*,Auto");
        Children.Add(_canvas);
        _boundaryCue.Bind(Border.BorderBrushProperty, this.GetResourceObservable("Accent.Action"));
        _canvas.Children.Add(_boundaryCue);
        _shelf = new() { Child = _strip, Height = 28, BorderThickness = new(0, 1, 0, 0) };
        _shelf.Bind(Border.BackgroundProperty, this.GetResourceObservable("Surface.Raised"));
        _shelf.Bind(Border.BorderBrushProperty, this.GetResourceObservable("Border.Default"));
        SetRow(_shelf, 1);
        Children.Add(_shelf);
        foreach (var definition in WorkspaceState.Definitions)
        {
            var tab = new Button { Classes = { "shell" }, MinWidth = 100 };
            AutomationProperties.SetAutomationId(tab, definition.InstanceId + ".restore");
            tab.Click += (_, _) => Open(definition.InstanceId);
            _tabs.Add(definition.InstanceId, tab);
            _strip.Children.Add(tab);
        }

        _resizeSave = new() { Interval = TimeSpan.FromMilliseconds(250) };
        _resizeSave.Tick += (_, _) =>
        {
            _resizeSave.Stop();
            Commit();
        };
        _canvas.SizeChanged += (_, _) =>
        {
            if (_disposed || _stopping || _canvas.Bounds.Width <= 0 || _canvas.Bounds.Height <= 0) return;
            CancelGestures();
            State.Reflow(_canvas.Bounds.Width, _canvas.Bounds.Height);
            if (_hasSize && State.Panes.Any(pane => pane.Visibility != PaneVisibility.Hidden))
            {
                _resizeSave.Stop();
                _resizeSave.Start();
            }

            _hasSize = true;
        };
        State.Changed += Refresh;
        session.PropertyChanged += SessionChanged;
        AddHandler(KeyDownEvent, PaneKeyDown);
        _canvas.AddHandler(PointerMovedEvent, BoundaryMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        _canvas.AddHandler(PointerPressedEvent, BoundaryPressed, RoutingStrategies.Tunnel);
        _canvas.AddHandler(PointerReleasedEvent, BoundaryReleased, RoutingStrategies.Tunnel);
        _canvas.PointerCaptureLost += (_, _) => CancelResize();
        _canvas.PointerExited += (_, _) =>
        {
            _hoverPosition = null;
            if (_resizePointer is null) ShowBoundary(null);
        };
        _canvas.LayoutUpdated += (_, _) =>
        {
            if (_disposed || _stopping) return;
            if (_resizeBoundary is { } captured) ShowBoundary(captured);
            else if (_hoverPosition is { } point)
                ShowBoundary(ResolveBoundary(point, _canvas.InputHitTest(point) is Visual source &&
                                                    WorkspacePane.IsActionSource(source)));
        };
        Refresh();
    }

    public void Open(string id)
    {
        if (_disposed || _stopping) return;
        CancelGestures();
        State.Open(id);
        Refresh();
        FocusPane(id);
        Commit();
    }

    internal void Action(string id, Action operation, bool focus = true)
    {
        if (_disposed || _stopping) return;
        CancelGestures();
        operation();
        Refresh();
        if (focus) FocusPane(State.ActivePaneId);
        Commit();
    }

    public void FocusPane(string? id)
    {
        if (_disposed || _stopping || State.IsDisposed) return;
        if (id is not null && State.Get(id).Visibility == PaneVisibility.Visible &&
            _views.TryGetValue(id, out var view))
            view.FocusContext();
        else LeaveRequested?.Invoke();
    }

    internal void Activate(string id)
    {
        if (_disposed || _stopping || State.ActivePaneId == id) return;
        State.Activate(id);
        Commit();
    }

    public void Commit()
    {
        if (!_disposed && !_stopping) LayoutCommitted?.Invoke();
    }

    public void PrepareShutdown()
    {
        _resizeSave.Stop();
        CancelGestures();
        _stopping = true;
        IsEnabled = false;
    }

    public void CancelGestures()
    {
        CancelResize();
        foreach (var view in _views.Values) view.CancelGesture();
    }

    private ResizeBoundary? ResolveBoundary(PointerEventArgs args)
    {
        var point = args.GetPosition(_canvas);
        _hoverPosition = point;
        return ResolveBoundary(point, args.Source is Visual source && WorkspacePane.IsActionSource(source));
    }

    private ResizeBoundary? ResolveBoundary(Point point, bool control)
    {
        var panes = State.Panes.Select(pane =>
        {
            if (!_views.TryGetValue(pane.InstanceId, out var view) || !view.IsVisible)
                return new PaneBoundary(pane.InstanceId, default, pane.Dock, false);
            var origin = view.TranslatePoint(default, _canvas) ?? default;
            return new PaneBoundary(pane.InstanceId,
                new(origin.X, origin.Y, view.Bounds.Width, view.Bounds.Height), pane.Dock, true);
        }).ToArray();
        return WorkspaceBoundaryResolver.Resolve(panes, point.X, point.Y, _canvas.Bounds.Width, _canvas.Bounds.Height,
            control);
    }

    private void ShowBoundary(ResizeBoundary? boundary)
    {
        _hoverBoundary = boundary;
        Cursor? cursor = null;
        if (boundary is { } target)
        {
            var type = target.Edges switch
            {
                PaneEdges.Left or PaneEdges.Right => StandardCursorType.SizeWestEast,
                PaneEdges.Top or PaneEdges.Bottom => StandardCursorType.SizeNorthSouth,
                PaneEdges.Left | PaneEdges.Top or PaneEdges.Right | PaneEdges.Bottom =>
                    StandardCursorType.TopLeftCorner,
                _ => StandardCursorType.TopRightCorner
            };
            if (!_boundaryCursors.TryGetValue(type, out cursor))
                _boundaryCursors.Add(type, cursor = new(type));
        }

        _canvas.Cursor = cursor;
        // Avalonia's half-open hit rectangles can route the exact seam through the other pane.
        // Present the resolved cursor there too; boundary identity and the cue remain workspace-owned.
        foreach (var view in _views.Values) view.SetResizeCursor(cursor);
        _boundaryCue.IsVisible = boundary is { } cue && _views[cue.InstanceId].IsVisible;
        if (boundary is not { } selected) return;
        var paneView = _views[selected.InstanceId];
        _boundaryCue.ZIndex = paneView.ZIndex + 1;
        var origin = paneView.TranslatePoint(default, _canvas) ?? default;
        Canvas.SetLeft(_boundaryCue, origin.X);
        Canvas.SetTop(_boundaryCue, origin.Y);
        _boundaryCue.Width = paneView.Bounds.Width;
        _boundaryCue.Height = paneView.Bounds.Height;
        _boundaryCue.BorderThickness = new(
            selected.Edges.HasFlag(PaneEdges.Left) ? 1 : 0,
            selected.Edges.HasFlag(PaneEdges.Top) ? 1 : 0,
            selected.Edges.HasFlag(PaneEdges.Right) ? 1 : 0,
            selected.Edges.HasFlag(PaneEdges.Bottom) ? 1 : 0);
    }

    private void BoundaryMoved(object? sender, PointerEventArgs args)
    {
        if (_disposed || _stopping) return;
        if (_resizePointer == args.Pointer)
        {
            var point = args.GetPosition(_canvas);
            _hoverPosition = point;
            _resizeGesture?.Update(point.X, point.Y);
            ShowBoundary(_resizeBoundary);
            args.Handled = true;
        }
        else if (args.Pointer.Captured is null) ShowBoundary(ResolveBoundary(args));
        else _hoverPosition = null;
    }

    private void BoundaryPressed(object? sender, PointerPressedEventArgs args)
    {
        if (_disposed || _stopping || !args.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed ||
            ResolveBoundary(args) is not { } boundary) return;
        CancelGestures();
        var point = args.GetPosition(_canvas);
        // Freeze identity and edges before activation reorders the visible panes.
        _resizeBoundary = boundary;
        _resizeGesture = new(State, boundary.InstanceId, boundary.Edges, point.X, point.Y);
        _resizePointer = args.Pointer;
        State.Activate(boundary.InstanceId);
        ShowBoundary(boundary);
        args.Pointer.Capture(_canvas);
        if (!_views[boundary.InstanceId].IsKeyboardFocusWithin) FocusPane(boundary.InstanceId);
        args.Handled = true;
    }

    private void BoundaryReleased(object? sender, PointerReleasedEventArgs args)
    {
        if (_resizePointer != args.Pointer || args.InitialPressMouseButton != MouseButton.Left) return;
        _resizePointer = null;
        _resizeGesture?.Complete();
        _resizeGesture = null;
        _resizeBoundary = null;
        args.Pointer.Capture(null);
        Commit();
        ShowBoundary(ResolveBoundary(args));
        args.Handled = true;
    }

    private void CancelResize()
    {
        if (_resizePointer is not { } pointer) return;
        _resizePointer = null;
        _resizeGesture?.Cancel();
        _resizeGesture = null;
        _resizeBoundary = null;
        pointer.Capture(null);
        ShowBoundary(null);
    }

    private void PaneKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Handled) return;
        if (args.Key == Key.Escape && args.KeyModifiers == KeyModifiers.None)
        {
            var resizing = _resizePointer is not null;
            CancelGestures();
            if (!resizing) LeaveRequested?.Invoke();
            args.Handled = true;
        }
    }

    private void SessionChanged(object? sender, PropertyChangedEventArgs args) => Refresh();

    private void Refresh()
    {
        if (_disposed) return;
        var order = 0;
        foreach (var pane in State.Panes)
        {
            var definition = WorkspaceState.Definition(pane.InstanceId);
            var tab = _tabs[pane.InstanceId];
            tab.IsVisible = pane.Visibility != PaneVisibility.Hidden;
            var tabContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            if (pane.Visibility == PaneVisibility.Collapsed)
                tabContent.Children.Add(HostIcons.Create("Up", tab, 16));
            tabContent.Children.Add(new TextBlock { Text = _session[definition.TitleKey] });
            tab.Content = tabContent;
            AutomationProperties.SetName(tab,
                _session[pane.Visibility == PaneVisibility.Collapsed ? "Pane.Restore" : "Pane.Activate"] +
                ": " + _session[definition.TitleKey]);
            ToolTip.SetTip(tab, AutomationProperties.GetName(tab));
            if (!_views.TryGetValue(pane.InstanceId, out var view) && pane.Visibility != PaneVisibility.Hidden)
            {
                view = new(this, _session, definition, _language, _theme);
                _views.Add(pane.InstanceId, view);
                _canvas.Children.Add(view);
            }

            if (view is null) continue;
            var bounds = State.Bounds(pane.InstanceId);
            view.IsVisible = pane.Visibility == PaneVisibility.Visible;
            Canvas.SetLeft(view, bounds.X);
            Canvas.SetTop(view, bounds.Y);
            view.Width = bounds.Width;
            view.Height = bounds.Height;
            view.ZIndex = order++ * 2;
            view.Refresh(pane, State.ActivePaneId == pane.InstanceId);
        }

        _shelf.IsVisible = State.Panes.Any(pane => pane.Visibility != PaneVisibility.Hidden);
        ShowBoundary(_resizeBoundary ?? _hoverBoundary);
    }

    public void Dispose()
    {
        if (_disposed) return;
        CancelGestures();
        _disposed = true;
        _resizeSave.Stop();
        State.Changed -= Refresh;
        _session.PropertyChanged -= SessionChanged;
        foreach (var view in _views.Values) view.Dispose();
        _views.Clear();
        _canvas.Children.Clear();
        LayoutCommitted = null;
        LeaveRequested = null;
        State.Dispose();
    }
}
