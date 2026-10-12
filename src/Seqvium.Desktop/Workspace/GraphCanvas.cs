// SPDX-License-Identifier: Apache-2.0
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>First-party retained canvas. Pointer previews own no canonical graph or audio state.</summary>
internal sealed class GraphCanvas : Control
{
    protected override AutomationPeer OnCreateAutomationPeer() => new CanvasPeer(this);
    private sealed class CanvasPeer(GraphCanvas owner) : ControlAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => "GraphCanvas";
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    }
    private readonly ShellSession _shell;
    private readonly GraphEditorSession _editor;
    private readonly DesktopWorkflow _workflow;
    private readonly Dictionary<string, IBrush> _brushes = [];
    private HostTheme? _theme;
    private IPointer? _pointer;
    private GraphPoint _origin, _last, _wireEnd;
    private GraphHit? _wire;
    private GraphEditBasis? _wireBasis;
    private bool _pan, _space;
    private readonly NodeBodyClick _bodyClick = new();
    private Task _command = Task.CompletedTask;
    public IReadOnlyList<GraphDiagnostic> Diagnostics { get; set; } = [];
    public event Action? SelectionChanged;
    public event Action<GraphPoint>? MenuRequested;
    public event Action<string?>? Feedback;
    public event Action<NodeEditorTarget>? EditorRequested;
    public Task PendingCommand => _command;

    public GraphCanvas(ShellSession shell, GraphEditorSession editor, DesktopWorkflow workflow)
    {
        _shell = shell; _editor = editor; _workflow = workflow;
        Focusable = true; ClipToBounds = true;
        AutomationProperties.SetAutomationId(this, "Graph.Canvas");
        PointerPressed += Pressed; PointerMoved += Moved; PointerReleased += Released;
        PointerCaptureLost += (_, e) => { if (_pointer == e.Pointer) CancelGesture(); };
        ContextRequested += (_, e) =>
        {
            e.Handled = true;
            if (!_command.IsCompleted) return;
            Focus();
            var point = e.TryGetPosition(this, out var position) ? Point(position) : new GraphPoint(Bounds.Width / 2, Bounds.Height / 2);
            MenuRequested?.Invoke(_editor.Viewport.ToGraph(point));
        };
        KeyDown += Keys; KeyUp += (_, e) => { if (e.Key == Key.Space) _space = false; };
        GotFocus += (_, _) => InvalidateVisual();
        LostFocus += (_, _) => { _space = false; InvalidateVisual(); };
        PointerWheelChanged += (_, e) =>
        {
            if (_pointer is not null) return;
            _editor.Viewport.ZoomAt(Point(e.GetPosition(this)), Math.Pow(1.15, e.Delta.Y));
            InvalidateVisual(); e.Handled = true;
        };
    }

    private static GraphPoint Point(Point p) => new(p.X, p.Y);
    private static Point Point(GraphPoint p) => new(p.X, p.Y);
    private GraphPoint GraphPointAt(PointerEventArgs e) => _editor.Viewport.ToGraph(Point(e.GetPosition(this)));
    private GraphHit Hit(GraphPoint point) => _editor.Graph is { } graph
        ? GraphGeometry.HitTest(graph, point, _editor.Viewport.Zoom, _editor.SelectedNode, _editor.Move?.Node,
            _editor.Move?.After, _editor.SelectedConnection)
        : new(GraphHitKind.Background);
    private NodeEditorTarget? BodyTarget(GraphHit hit) => hit is { Kind: GraphHitKind.Node, Node: { } node } &&
        _editor.Basis is { } basis && _editor.Graph is { } graph
        ? new(basis.Lifecycle, basis.Attachment, graph.Id, node) : null;
    public void OpenSelectedEditor()
    {
        var target = _editor.SelectedNode is { } node ? BodyTarget(new(GraphHitKind.Node, node)) : null;
        CancelGesture();
        if (target is not null) EditorRequested?.Invoke(target);
    }
    private IBrush Brush(string role)
    {
        if (_theme != _shell.Preferences.Theme) { _brushes.Clear(); _theme = _shell.Preferences.Theme; }
        if (!_brushes.TryGetValue(role, out var brush))
            _brushes.Add(role, brush = new SolidColorBrush(Color.Parse(HostPalette.For(_shell.Preferences.Theme)[role])));
        return brush;
    }
    private string Label(GraphNode node) => _shell[node.Type switch
    {
        GraphBuiltIns.Source => "Graph.Source", GraphBuiltIns.Gain => "Graph.Gain", GraphBuiltIns.Mix => "Graph.Mix",
        GraphBuiltIns.Output => "Graph.Output", _ => "Graph.Unsupported"
    }];
    private void Text(DrawingContext context, string value, GraphPoint at, string role = "Text.Primary", double size = 12)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, Brush(role));
        context.DrawText(text, Point(at));
    }
    private void Cable(DrawingContext context, GraphPoint from, GraphPoint to, string role, double width)
    {
        double reach = Math.Max(48, Math.Abs(to.X - from.X) * 0.5);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            path.BeginFigure(Point(from), false);
            path.CubicBezierTo(Point(from + new GraphPoint(reach, 0)), Point(to - new GraphPoint(reach, 0)), Point(to));
            path.EndFigure(false);
        }
        // A surface under-stroke keeps intersecting wires independently readable.
        context.DrawGeometry(null, new Pen(Brush("Surface.Workspace"), width + 3), geometry);
        context.DrawGeometry(null, new Pen(Brush(role), width), geometry);
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(Brush("Surface.Workspace"), null, new Rect(Bounds.Size));
        if (_editor.Graph is not { } graph)
        {
            Text(context, _shell[_editor.Target is null ? "Graph.NoTarget" : "Graph.TargetMissing"], new(18, 24));
            return;
        }
        var viewport = _editor.Viewport;
        using var transform = context.PushTransform(new Matrix(viewport.Zoom, 0, 0, viewport.Zoom, viewport.Offset.X, viewport.Offset.Y));
        var topLeft = viewport.ToGraph(default);
        var bottomRight = viewport.ToGraph(new(Bounds.Width, Bounds.Height));
        var gridPen = new Pen(Brush("Graph.Grid"), 1 / viewport.Zoom);
        // Bound grid work by visible extent, including a zoomed-out large canvas.
        for (double x = Math.Floor(topLeft.X / 32) * 32; x < bottomRight.X; x += 32)
            context.DrawLine(gridPen, new(x, topLeft.Y), new(x, bottomRight.Y));
        for (double y = Math.Floor(topLeft.Y / 32) * 32; y < bottomRight.Y; y += 32)
            context.DrawLine(gridPen, new(topLeft.X, y), new(bottomRight.X, y));
        GraphPosition? Preview(GraphNode node) => _editor.Move?.Node == node.Id ? _editor.Move.After : null;
        foreach (var edge in graph.Connections.OrderBy(c => c.Id == _editor.SelectedConnection ? 1 : 0))
        {
            var from = graph.Nodes.SingleOrDefault(n => n.Id == edge.FromNodeId);
            var to = graph.Nodes.SingleOrDefault(n => n.Id == edge.ToNodeId);
            var fromPort = from?.Ports.SingleOrDefault(p => p.Id == edge.FromPortId);
            var toPort = to?.Ports.SingleOrDefault(p => p.Id == edge.ToPortId);
            if (from is null || to is null || fromPort is null || toPort is null) continue;
            bool error = Diagnostics.Any(d => d.ConnectionId == edge.Id || d.Reason == GraphReasons.Cycle &&
                (d.NodeId == edge.FromNodeId || d.NodeId == edge.ToNodeId));
            Cable(context, GraphGeometry.Port(from, fromPort, Preview(from)), GraphGeometry.Port(to, toPort, Preview(to)),
                edge.Id == _editor.SelectedConnection ? "Accent.Action" : error ? "Status.Critical" : "Graph.Cable",
                (edge.Id == _editor.SelectedConnection ? 3 : 2) / viewport.Zoom);
        }
        foreach (var node in GraphGeometry.DrawOrder(graph, _editor.SelectedNode))
        {
            var body = GraphGeometry.Body(node, Preview(node));
            bool selected = node.Id == _editor.SelectedNode, error = Diagnostics.Any(d => d.NodeId == node.Id);
            var rectangle = new Rect(body.X, body.Y, body.Width, body.Height);
            context.DrawRectangle(Brush(selected ? "Surface.Selected" : "Graph.Node"),
                new Pen(Brush(selected ? "Accent.Action" : error ? "Status.Critical" : "Graph.NodeBorder"), selected ? 2 : 1), rectangle, 3, 3);
            string accent = node.Type switch { GraphBuiltIns.Source => "Status.Playing", GraphBuiltIns.Mix => "Status.Warning",
                GraphBuiltIns.Gain => "Accent.Action", _ => "Text.Secondary" };
            context.DrawRectangle(Brush(accent), null, new Rect(body.X + 1, body.Y + 8, 3, 16));
            Text(context, Label(node), new(body.X + 12, body.Y + 9));
            if (error) Text(context, "!", new(body.X + body.Width - 15, body.Y + 9), "Status.Critical");
            context.DrawLine(new Pen(Brush("Border.Default"), 1), new(body.X + 8, body.Y + 31), new(body.X + body.Width - 8, body.Y + 31));
            foreach (var port in node.Ports)
            {
                var at = GraphGeometry.Port(node, port, Preview(node));
                bool portError = Diagnostics.Any(d => d.PortId == port.Id);
                string role = portError ? "Status.Critical" : "Graph.Cable";
                if (_wire is { Node: { } wireNode, Port: { } wirePort } && node.Id != wireNode)
                    role = GraphConnectionRules.Refusal(graph, wireNode, wirePort, node.Id, port.Id) is null
                        ? "Status.Playing" : "State.Unavailable";
                context.DrawEllipse(Brush("Surface.Raised"), new Pen(Brush(role), 2), Point(at), 4.5, 4.5);
                int slot = node.Ports.Where(p => p.Direction == port.Direction).ToList().FindIndex(p => p.Id == port.Id) + 1;
                string portLabel = port.Direction == GraphPortDirection.Output ? _shell["Graph.Out"] :
                    _shell["Graph.In"] + (node.Type == GraphBuiltIns.Mix ? " " + slot : "");
                Text(context, portLabel, new(port.Direction == GraphPortDirection.Input ? at.X + 10 : at.X - 39, at.Y - 8), "Text.Secondary", 11);
            }
            string value;
            if (node.Type == GraphBuiltIns.Gain)
                value = node.Parameters.TryGetValue(GraphBuiltIns.GainAmplitude, out var gain)
                    ? "× " + gain.ToString() : _shell["Graph.InvalidValue"];
            else if (node.Type == GraphBuiltIns.Source)
            {
                var binding = _editor.Attachment?.Sources.SingleOrDefault(s => s.NodeId == node.Id);
                value = _editor.State?.Patterns.SelectMany(p => p.Parts).SingleOrDefault(p => p.Id == binding?.PartId)?.Name
                    ?? _shell["Graph.Unbound"];
            }
            else value = node.Ports.FirstOrDefault()?.Layout == GraphBuiltIns.Mono ? "Mono" : "Stereo L/R";
            if (value.Length > 20) value = value[..18] + "…";
            Text(context, value, new(body.X + 10, body.Y + body.Height - 23), "Text.Secondary", 11);
            if (selected && IsFocused)
                context.DrawRectangle(null, new Pen(Brush("Focus.Active"), 1, new DashStyle([2, 2], 0)), rectangle.Inflate(4), 3, 3);
        }
        if (_wire is { Node: { } originNode, Port: { } originPort })
        {
            var node = graph.Nodes.Single(n => n.Id == originNode);
            var port = node.Ports.Single(p => p.Id == originPort);
            var start = GraphGeometry.Port(node, port);
            var hit = Hit(_wireEnd);
            string? reason = hit is { Node: { } n, Port: { } p } ? GraphConnectionRules.Refusal(graph, originNode, originPort, n, p) : "Graph.ChoosePort";
            Cable(context, port.Direction == GraphPortDirection.Output ? start : _wireEnd,
                port.Direction == GraphPortDirection.Output ? _wireEnd : start,
                reason is null ? "Status.Playing" : "Status.Warning", 2 / viewport.Zoom);
        }
    }

    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_command.IsCompleted) return;
        Focus(); var point = Point(e.GetPosition(this)); var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsRightButtonPressed)
        {
            CancelGesture();
            _editor.Select(Hit(GraphPointAt(e))); SelectionChanged?.Invoke();
            InvalidateVisual(); e.Handled = true; return;
        }
        if (!properties.IsLeftButtonPressed && !properties.IsMiddleButtonPressed) return;
        // Preserve the previous completed body click across ordinary presses only.
        CancelGesture(resetClick: false); Feedback?.Invoke(null);
        _origin = _last = point;
        var hit = Hit(GraphPointAt(e));
        _pan = properties.IsMiddleButtonPressed || _space || hit.Kind == GraphHitKind.Background;
        _bodyClick.Press(!_pan && properties.IsLeftButtonPressed && e.KeyModifiers == KeyModifiers.None
            ? BodyTarget(hit) : null, e.ClickCount, _workflow.State.Generation);
        if (!_pan)
        {
            _editor.Select(hit); SelectionChanged?.Invoke();
            if (hit.Kind == GraphHitKind.Node && hit.Node is { } node) _editor.BeginMove(node);
            if (hit.Kind == GraphHitKind.Port) { _wire = hit; _wireBasis = _editor.Basis; _wireEnd = GraphPointAt(e); }
        }
        else if (!properties.IsMiddleButtonPressed && !_space) { _editor.Select(hit); SelectionChanged?.Invoke(); }
        _pointer = e.Pointer; e.Pointer.Capture(this); InvalidateVisual(); e.Handled = true;
    }
    private void Moved(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer)
        {
            var hit = Hit(GraphPointAt(e));
            var reasons = Diagnostics.Where(d => hit.Port is { } port ? d.PortId == port :
                hit.Node is { } node ? d.NodeId == node : hit.Connection is { } cable && d.ConnectionId == cable)
                .Select(d => _shell[d.Reason]).Distinct().ToArray();
            ToolTip.SetTip(this, reasons.Length == 0 ? _shell["Graph.KeyboardHint"] : string.Join("\n", reasons));
            return;
        }
        var point = Point(e.GetPosition(this));
        if (point != _origin) _bodyClick.Move();
        if (_pan) _editor.Viewport.Pan(point - _last);
        else if (_editor.Move is not null) _editor.UpdateMove((point - _origin) * (1 / _editor.Viewport.Zoom));
        else if (_wire is { Node: { } n, Port: { } p } && _editor.Graph is { } graph)
        {
            _wireEnd = GraphPointAt(e); var hit = Hit(_wireEnd);
            Feedback?.Invoke(hit is { Node: { } to, Port: { } port }
                ? GraphConnectionRules.Refusal(graph, n, p, to, port) ?? "Graph.Compatible" : "Graph.ChoosePort");
        }
        _last = point; InvalidateVisual(); e.Handled = true;
    }
    private async void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var move = _editor.CompleteMove(); var wire = _wire; var basis = _wireBasis; var hit = Hit(GraphPointAt(e));
        var open = _bodyClick.Release(BodyTarget(hit), _workflow.State.Generation);
        _pointer = null; _pan = false; _wire = null; _wireBasis = null; e.Pointer.Capture(null);
        e.Handled = true; InvalidateVisual();
        if (move is not null) await Execute(_workflow.MoveGraphNodeAsync(move));
        else if (wire is { Node: { } n, Port: { } p } && basis is not null && hit is { Node: { } to, Port: { } port })
            await Execute(_workflow.ConnectGraphAsync(basis, n, p, to, port));
        else Feedback?.Invoke(null);
        if (move is null && wire is null && open is not null) EditorRequested?.Invoke(open);
    }
    public void CancelGesture(bool resetClick = true)
    {
        if (resetClick) _bodyClick.Cancel();
        var pointer = _pointer; _pointer = null; _pan = false; _wire = null; _wireBasis = null;
        _editor.Cancel(); pointer?.Capture(null); Feedback?.Invoke(null); InvalidateVisual();
    }
    private async Task Execute(Task<bool> operation)
    {
        _command = operation;
        bool success = await operation;
        Feedback?.Invoke(success ? null : _workflow.State.Detail ?? "Graph.Stale");
        SelectionChanged?.Invoke(); InvalidateVisual();
    }
    public async Task DeleteSelectedAsync()
    {
        CancelGesture();
        if (_editor.Basis is { } basis) await Execute(_workflow.DeleteGraphElementAsync(basis, _editor.SelectedNode, _editor.SelectedConnection));
    }
    public void Fit()
    {
        CancelGesture();
        if (_editor.Graph is { } graph) _editor.Viewport.Fit(graph, Bounds.Width, Bounds.Height);
        InvalidateVisual();
    }
    private async void Keys(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        { CancelGesture(); e.Handled = true; return; }
        if (e.Key == Key.Space) { _space = true; e.Handled = true; return; }
        if (!_command.IsCompleted) return;
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None && IsFocused)
        { OpenSelectedEditor(); e.Handled = true; return; }
        if (e.Key == Key.Home) { Fit(); e.Handled = true; return; }
        if (e.Key is Key.Add or Key.OemPlus or Key.Subtract or Key.OemMinus)
        {
            _editor.Viewport.ZoomAt(new(Bounds.Width / 2, Bounds.Height / 2), e.Key is Key.Add or Key.OemPlus ? 1.15 : 1 / 1.15);
            InvalidateVisual(); e.Handled = true; return;
        }
        if (e.Key == Key.Delete) { await DeleteSelectedAsync(); e.Handled = true; return; }
        if (e.Key == Key.Tab && e.KeyModifiers == KeyModifiers.Alt && _editor.Graph is { } graph)
        {
            var nodes = graph.Nodes.ToArray(); int index = Array.FindIndex(nodes, n => n.Id == _editor.SelectedNode);
            if (nodes.Length > 0) _editor.Select(new(GraphHitKind.Node, nodes[(index + 1) % nodes.Length].Id));
            SelectionChanged?.Invoke(); InvalidateVisual(); e.Handled = true; return;
        }
        if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down && _editor.SelectedNode is { } selected &&
            e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift)
        {
            _editor.BeginMove(selected); double step = e.KeyModifiers == KeyModifiers.Shift ? 10 : 1;
            _editor.UpdateMove(new(e.Key == Key.Left ? -step : e.Key == Key.Right ? step : 0,
                e.Key == Key.Up ? -step : e.Key == Key.Down ? step : 0));
            if (_editor.CompleteMove() is { } move) await Execute(_workflow.MoveGraphNodeAsync(move));
            e.Handled = true;
        }
    }
}
