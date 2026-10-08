// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
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
    private readonly Border _grip;
    private readonly Border _dockGrip;
    private readonly FirstPartyPaneContent _content;
    private readonly ContextMenu _menu = new();
    private readonly List<(MenuItem Item, string Key)> _items = [];
    private readonly MenuItem _left;
    private readonly MenuItem _right;
    private readonly MenuItem _permission;
    private IPointer? _pointer;
    private Point _origin;
    private PaneBounds _start;
    private bool _resizing;
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
        var body = new Grid { RowDefinitions = new("28,*,12") };
        Child = body;
        _header = new() { Padding = new(6, 0, 2, 0) };
        _header.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Raised"));
        var header = new Grid { ColumnDefinitions = new("*,Auto,Auto,Auto") };
        _header.Child = header;
        body.Children.Add(_header);
        header.Children.Add(_title);
        _actions = ChromeButton("⋯", "Actions", header, 1);
        _collapse = ChromeButton("−", "Collapse", header, 2);
        _hide = ChromeButton("×", "Hide", header, 3);
        _collapse.Click += (_, _) => host.Action(Id, () => host.State.Collapse(Id));
        _hide.Click += (_, _) => host.Action(Id, () => host.State.Hide(Id));
        _left = AddAction("Pane.DockLeft", () => host.State.Dock(Id, PaneDock.Left));
        _right = AddAction("Pane.DockRight", () => host.State.Dock(Id, PaneDock.Right));
        AddAction("Pane.Float", () => host.State.Dock(Id, PaneDock.Floating));
        _permission = AddAction("Pane.AllowDocking",
            () => host.State.SetDockingAllowed(Id, !host.State.Get(Id).AllowDocking));
        _permission.ToggleType = MenuItemToggleType.CheckBox;
        AddAction("Pane.Collapse", () => host.State.Collapse(Id));
        AddAction("Pane.Hide", () => host.State.Hide(Id));
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
        _grip = new()
        {
            Height = 12, Background = Avalonia.Media.Brushes.Transparent,
            Cursor = new(StandardCursorType.BottomRightCorner)
        };
        var glyph = new TextBlock
        {
            Text = "◢", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right, Classes = { "caption" },
            Margin = new(0, 0, 3, 0)
        };
        _grip.Child = glyph;
        Grid.SetRow(_grip, 2);
        body.Children.Add(_grip);
        _dockGrip = new Border
        {
            Width = 7, Background = Avalonia.Media.Brushes.Transparent,
            Cursor = new(StandardCursorType.SizeWestEast)
        };
        Grid.SetRowSpan(_dockGrip, 3);
        body.Children.Add(_dockGrip);
        _dockGrip.PointerPressed += (_, args) => Begin(args, true);
        _header.PointerPressed += (_, args) => Begin(args, false);
        _grip.PointerPressed += (_, args) => Begin(args, true);
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
        var button = new Button { Content = glyph, Width = 24, Classes = { "shell", "paneChrome" } };
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
        NameAction(_hide, "Pane.Hide");
        _left.IsEnabled = _right.IsEnabled = pane.AllowDocking;
        _permission.IsChecked = pane.AllowDocking;
        _grip.IsVisible = pane.Dock == PaneDock.Floating;
        _dockGrip.IsVisible = pane.Dock != PaneDock.Floating;
        _dockGrip.HorizontalAlignment =
            pane.Dock == PaneDock.Right ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        _header.Cursor = new(pane.Dock == PaneDock.Floating ? StandardCursorType.SizeAll : StandardCursorType.Arrow);
        _grip.Cursor = new(pane.Dock == PaneDock.Floating
            ? StandardCursorType.BottomRightCorner
            : StandardCursorType.SizeWestEast);
        _content.Refresh();
    }

    private void NameAction(Button button, string key)
    {
        var label = _session[key] + ": " + _session[_definition.TitleKey];
        AutomationProperties.SetName(button, label);
        ToolTip.SetTip(button, label + (_actions == button ? " · " + _session["Pane.MoveHint"] : ""));
    }

    public void FocusContext()
    {
        if (!_disposed && IsVisible && TopLevel.GetTopLevel(this) is not null) _actions.Focus();
    }

    private void Begin(PointerPressedEventArgs args, bool resize)
    {
        if (_disposed || !args.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (args.Source is Visual source &&
            source.GetVisualAncestors().Prepend(source).Any(item => item is Button)) return;
        if (!resize && _host.State.Get(Id).Dock != PaneDock.Floating) return;
        _host.CancelGestures();
        _pointer = args.Pointer;
        _origin = args.GetPosition(_host);
        _start = _host.State.Bounds(Id);
        _resizing = resize;
        args.Pointer.Capture(this);
        FocusContext();
        args.Handled = true;
    }

    private void Move(object? sender, PointerEventArgs args)
    {
        if (_pointer != args.Pointer) return;
        var delta = args.GetPosition(_host) - _origin;
        var dock = _host.State.Get(Id).Dock;
        var bounds = _resizing
            ? _start with
            {
                Width = _start.Width + (dock == PaneDock.Right ? -delta.X : delta.X),
                Height = _start.Height + delta.Y
            }
            : _start with { X = _start.X + delta.X, Y = _start.Y + delta.Y };
        _host.State.SetBounds(Id, bounds);
        args.Handled = true;
    }

    private void End(object? sender, PointerReleasedEventArgs args)
    {
        if (_pointer != args.Pointer || args.InitialPressMouseButton != MouseButton.Left) return;
        _pointer = null;
        args.Pointer.Capture(null);
        _host.Commit();
        args.Handled = true;
    }

    public void CancelGesture()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null;
        if (!_host.State.IsDisposed) _host.State.SetBounds(Id, _start);
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
        var bounds = _host.State.Bounds(Id);
        if (args.KeyModifiers.HasFlag(KeyModifiers.Shift))
            bounds = bounds with { Width = bounds.Width + x, Height = bounds.Height + y };
        else if (_host.State.Get(Id).Dock == PaneDock.Floating)
            bounds = bounds with { X = bounds.X + x, Y = bounds.Y + y };
        _host.State.SetBounds(Id, bounds);
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
