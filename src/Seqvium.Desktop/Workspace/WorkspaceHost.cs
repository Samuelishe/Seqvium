// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>One internal canvas and a reachable strip. Controls live until the host is disposed.</summary>
public sealed class WorkspaceHost : Grid, IDisposable
{
    private readonly ShellSession _session;
    private readonly Canvas _canvas = new() { ClipToBounds = true };

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
        foreach (var view in _views.Values) view.CancelGesture();
    }

    private void PaneKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Handled) return;
        if (args.Key == Key.Escape && args.KeyModifiers == KeyModifiers.None)
        {
            CancelGestures();
            LeaveRequested?.Invoke();
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
            tab.Content = (pane.Visibility == PaneVisibility.Collapsed ? "▴ " : "") + _session[definition.TitleKey];
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
            view.ZIndex = order++;
            view.Refresh(pane, State.ActivePaneId == pane.InstanceId);
        }

        _shelf.IsVisible = State.Panes.Any(pane => pane.Visibility != PaneVisibility.Hidden);
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
