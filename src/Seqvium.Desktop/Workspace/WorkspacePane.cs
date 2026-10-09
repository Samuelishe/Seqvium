// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>Internal pane chrome, captured pointer gestures and safe local keyboard operations.</summary>
internal sealed class WorkspacePane : Border, IDisposable
{
    protected override Type StyleKeyOverride => typeof(Border);
    private readonly WorkspaceHost _host;
    private readonly ShellSession _session;
    private readonly PaneDefinition _definition;

    private readonly TextBlock _title = new()
        { VerticalAlignment = VerticalAlignment.Center, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };

    private readonly Button _actions;
    private readonly Button _collapse;
    private readonly Button _hide;
    private readonly Border _header;
    private readonly Cursor _moveCursor = new(StandardCursorType.SizeAll);
    private readonly Cursor _arrowCursor = new(StandardCursorType.Arrow);
    private readonly Grid _surface = new();
    private readonly Border _corner;
    private readonly FirstPartyPaneContent _content;
    private readonly ContextMenu _menu = new();
    private readonly List<(MenuItem Item, string Key)> _items = [];
    private readonly MenuItem _left;
    private readonly MenuItem _right;
    private readonly MenuItem _permission;
    private IPointer? _pointer;
    private PaneGesture? _gesture;
    private bool _disposed;
    private string Id => _definition.InstanceId;

    public WorkspacePane(WorkspaceHost host, ShellSession session, PaneDefinition definition,
        Func<Task> language, Func<Task> theme)
    {
        _host = host;
        _session = session;
        _definition = definition;
        BorderThickness = new(1);
        CornerRadius = new(1);
        Classes.Add("workspacePane");
        this.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Background"));
        AutomationProperties.SetAutomationId(this, Id);
        var body = new Grid { RowDefinitions = new("28,*"), Margin = new(5, 10, 5, 10) };
        Child = _surface;
        _surface.Children.Add(body);
        _header = new() { Padding = new(6, 0, 2, 0) };
        _header.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Raised"));
        var header = new Grid { ColumnDefinitions = new("*,Auto,Auto,Auto") };
        _header.Child = header;
        body.Children.Add(_header);
        header.Children.Add(_title);
        _actions = ChromeButton("More", "Actions", header, 1);
        _collapse = ChromeButton("Minimize", "Collapse", header, 2);
        _hide = ChromeButton("Close", "Hide", header, 3);
        _collapse.Click += (_, _) => host.Action(Id, () => host.State.Collapse(Id));
        _hide.Click += (_, _) => host.Action(Id, () => host.State.Hide(Id));
        _left = AddAction("Pane.DockLeft", () => host.State.Dock(Id, PaneDock.Left));
        _right = AddAction("Pane.DockRight", () => host.State.Dock(Id, PaneDock.Right));
        AddAction("Pane.Float", () => host.State.Dock(Id, PaneDock.Floating));
        _permission = AddAction("Pane.AllowDocking",
            () => host.State.SetDockingAllowed(Id, !host.State.Get(Id).AllowDocking));
        _permission.ToggleType = MenuItemToggleType.CheckBox;
        AddAction("Pane.Collapse", () => host.State.Collapse(Id));
        AddAction("Pane.Close", () => host.State.Hide(Id));
        var hint = new MenuItem { IsEnabled = false };
        _items.Add((hint, "Pane.MoveHint"));
        _menu.Items.Add(hint);
        _actions.Click += (_, _) => _menu.Open(_actions);
        _menu.Closed += (_, _) =>
        {
            if (!_disposed && host.State.Get(Id).Visibility == PaneVisibility.Visible) FocusContext();
        };
        _actions.ContextMenu = _menu;
        _content = new(session, definition, language, theme);
        Grid.SetRow(_content, 1);
        body.Children.Add(_content);
        _corner = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
            Width = 10, Height = 10, IsHitTestVisible = false,
            Child = HostIcons.Create("Resize", _actions, 10)
        };
        _surface.Children.Add(_corner);
        _header.PointerPressed += (_, args) => Begin(args);
        AddHandler(PointerPressedEvent, (_, _) => host.Activate(Id), RoutingStrategies.Tunnel);
        AddHandler(GotFocusEvent, (_, _) => host.Activate(Id));
        PointerMoved += Move;
        PointerReleased += End;
        PointerCaptureLost += (_, _) => CancelGesture();
        // Consume local geometry chords before standard Shift+Arrow focus navigation.
        AddHandler(KeyDownEvent, PaneKeyDown, RoutingStrategies.Tunnel);
    }

    private Button ChromeButton(string glyph, string action, Grid header, int column)
    {
        var button = new Button
        {
            Width = 24, Cursor = new(StandardCursorType.Arrow), Classes = { "shell", "paneChrome" }
        };
        button.Content = HostIcons.Create(glyph, button, 16);
        AutomationProperties.SetAutomationId(button, Id + "." + action.ToLowerInvariant());
        Grid.SetColumn(button, column);
        header.Children.Add(button);
        return button;
    }

    private MenuItem AddAction(string key, Action action)
    {
        var item = new MenuItem();
        AutomationProperties.SetAutomationId(item, Id + "." + key);
        item.Click += (_, _) => _host.Action(Id, action);
        _items.Add((item, key));
        _menu.Items.Add(item);
        return item;
    }

    public void Refresh(PanePlacement pane, bool active)
    {
        if (_disposed) return;
        _title.Text = _session[_definition.TitleKey];
        ToolTip.SetTip(_title, _title.Text);
        AutomationProperties.SetName(this, _title.Text);
        Classes.Set("active", active);
        _title.FontWeight = active ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal;
        foreach (var (item, key) in _items)
        {
            item.Header = _session[key];
            AutomationProperties.SetName(item, _session[key]);
        }

        NameAction(_actions, "Pane.Actions");
        NameAction(_collapse, "Pane.Collapse");
        NameAction(_hide, "Pane.Close");
        _left.IsEnabled = _right.IsEnabled = pane.AllowDocking;
        _permission.IsChecked = pane.AllowDocking;
        _corner.IsVisible = pane.Dock == PaneDock.Floating;
        _content.Refresh();
    }

    private void NameAction(Button button, string key)
    {
        var label = _session[key] + ": " + _session[_definition.TitleKey];
        AutomationProperties.SetName(button, label);
        if (button == _hide) AutomationProperties.SetAcceleratorKey(button, "Ctrl+W");
        ToolTip.SetTip(button, label + (_actions == button ? " · " + _session["Pane.MoveHint"] : ""));
    }

    public void FocusContext()
    {
        if (!_disposed && IsVisible && TopLevel.GetTopLevel(this) is not null) _actions.Focus();
    }

    internal void SetResizeCursor(Cursor? cursor)
    {
        Cursor = cursor;
        _header.Cursor = cursor ?? (_host.State.Get(Id).Dock == PaneDock.Floating ? _moveCursor : _arrowCursor);
    }

    private void Begin(PointerPressedEventArgs args)
    {
        if (_disposed || args.Handled || !args.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (args.Source is Visual source && IsActionSource(source)) return;
        if (_host.State.Get(Id).Dock != PaneDock.Floating) return;
        _host.CancelGestures();
        _pointer = args.Pointer;
        var origin = args.GetPosition(_host);
        _gesture = new(_host.State, Id, PaneEdges.None, origin.X, origin.Y);
        args.Pointer.Capture(this);
        if (!IsKeyboardFocusWithin) FocusContext();
        args.Handled = true;
    }

    internal static bool IsActionSource(Visual source) =>
        source.GetVisualAncestors().Prepend(source).TakeWhile(item => item is not WorkspacePane)
            .Any(item => item is Button or TextBox or SelectingItemsControl or RangeBase or Thumb or MenuItem ||
                         item is Control { Focusable: true });

    private void Move(object? sender, PointerEventArgs args)
    {
        if (_pointer != args.Pointer) return;
        var position = args.GetPosition(_host);
        _gesture?.Update(position.X, position.Y);
        args.Handled = true;
    }

    private void End(object? sender, PointerReleasedEventArgs args)
    {
        if (_pointer != args.Pointer || args.InitialPressMouseButton != MouseButton.Left) return;
        _pointer = null;
        _gesture?.Complete();
        _gesture = null;
        args.Pointer.Capture(null);
        _host.Commit();
        args.Handled = true;
    }

    public void CancelGesture()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null;
        _gesture?.Cancel();
        _gesture = null;
        pointer.Capture(null);
    }

    private void PaneKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape && _pointer is not null)
        {
            CancelGesture();
            args.Handled = true;
            return;
        }

        if (!_actions.IsFocused ||
            args.KeyModifiers is not (KeyModifiers.Control or (KeyModifiers.Control | KeyModifiers.Shift))) return;
        var x = args.Key == Key.Left ? -10 : args.Key == Key.Right ? 10 : 0;
        var y = args.Key == Key.Up ? -10 : args.Key == Key.Down ? 10 : 0;
        if (x == 0 && y == 0) return;
        var dock = _host.State.Get(Id).Dock;
        var edges = args.KeyModifiers.HasFlag(KeyModifiers.Shift)
            ? dock == PaneDock.Right ? PaneEdges.Left : PaneEdges.Right | PaneEdges.Bottom
            : PaneEdges.None;
        if (edges != PaneEdges.None || dock == PaneDock.Floating)
        {
            var gesture = new PaneGesture(_host.State, Id, edges, 0, 0);
            gesture.Update(dock == PaneDock.Right ? -x : x, y);
            gesture.Complete();
        }

        _host.Commit();
        args.Handled = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        CancelGesture();
        _disposed = true;
        _menu.Close();
        _content.Dispose();
    }
}
