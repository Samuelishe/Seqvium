// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed class GraphPaneContent : Grid, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly GraphEditorSession _editor;
    private readonly Func<GraphEditBasis, GraphPosition, Task> _import;
    private readonly ComboBox _target = new() { Classes = { "workflow" }, MinWidth = 90,
        HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly GraphCanvas _canvas;
    private readonly TextBlock _status = new() { Classes = { "caption" }, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _parameterName = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBox _value = new() { Width = 74, MinHeight = 24, Padding = new(4, 2), FontSize = 12 };
    private readonly Button _commit;
    private readonly StackPanel _parameter = new() { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new(6, 2) };
    private readonly Dictionary<string, Button> _buttons = [];
    private readonly ContextMenu _menu = new()
    {
        Placement = PlacementMode.AnchorAndGravity, PlacementAnchor = PopupAnchor.TopLeft,
        PlacementGravity = PopupGravity.BottomRight
    };
    private bool _refreshing, _disposed;
    private string? _feedback;
    private Id<GraphNode>? _parameterNode;
    private GraphEditBasis? _parameterBasis;
    private long _generation = -1;

    public GraphPaneContent(ShellSession shell, DesktopWorkflow workflow, GraphEditorSession editor,
        Func<GraphEditBasis, GraphPosition, Task> import)
    {
        _shell = shell; _workflow = workflow; _editor = editor; _import = import;
        RowDefinitions = new("Auto,*,Auto,Auto");
        var toolbar = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new(4, 2), MinHeight = 28 };
        toolbar.Children.Add(_target);
        var commands = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new(4, 0, 0, 0) };
        Grid.SetColumn(commands, 1); toolbar.Children.Add(commands); Children.Add(toolbar);
        Button Action(string key, string icon, Action operation)
        {
            var button = new Button { Classes = { "shell", "command" } };
            button.Content = HostIcons.Create(icon, button, 16); button.Click += (_, _) => operation();
            AutomationProperties.SetAutomationId(button, "Graph." + key); _buttons.Add(key, button);
            commands.Children.Add(button); return button;
        }
        Action("Add", "Add", () => OpenMenu(Center(), additionsOnly: true));
        Action("AddWav", "Import", async () =>
        {
            CancelGesture();
            if (_editor.Basis is { } basis) await _import(basis, new(Center().X, Center().Y));
        });
        Action("Fit", "Fit", () => _canvas!.Fit());
        Action("Delete", "Delete", async () => await _canvas!.DeleteSelectedAsync());
        Action("Actions", "More", () => OpenMenu(Center()));
        _canvas = new(shell, editor, workflow);
        ToolTip.SetTip(_canvas, shell["Graph.KeyboardHint"]);
        Grid.SetRow(_canvas, 1); Children.Add(_canvas);
        _canvas.SelectionChanged += Refresh;
        _canvas.MenuRequested += point => OpenMenu(point);
        _canvas.Feedback += reason => { _feedback = reason; RefreshStatus(); };
        _parameter.Children.Add(_parameterName); _parameter.Children.Add(_value);
        _commit = new Button { Classes = { "shell", "command" }, Content = null };
        _commit.Content = HostIcons.Create("Check", _commit, 16);
        AutomationProperties.SetAutomationId(_value, "Graph.GainValue");
        AutomationProperties.SetAutomationId(_commit, "Graph.CommitGain");
        _commit.Click += async (_, _) => await CommitGainAsync();
        _value.GotFocus += (_, _) => _parameterBasis = _editor.Basis;
        _value.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter) { e.Handled = true; await CommitGainAsync(); }
            if (e.Key == Key.Escape) { _parameterNode = null; Refresh(); _canvas.Focus(); e.Handled = true; }
        };
        _parameter.Children.Add(_commit); Grid.SetRow(_parameter, 2); Children.Add(_parameter);
        var status = new Border { Padding = new(6, 3), Child = _status, BorderThickness = new(0, 1, 0, 0) };
        status.Bind(Border.BorderBrushProperty, this.GetResourceObservable("Border.Default"));
        Grid.SetRow(status, 3); Children.Add(status);
        _target.SelectionChanged += async (_, _) =>
        {
            if (_refreshing || _target.SelectedItem is not OccurrenceChoice choice) return;
            CancelGesture();
            if (_editor.Open(_workflow.State, choice.Id))
            { await workflow.SelectAsync(choice.Id); Refresh(); _canvas.Fit(); _canvas.Focus(); }
        };
        AutomationProperties.SetAutomationId(_target, "Graph.Target");
        workflow.Changed += WorkflowChanged; shell.PropertyChanged += PreferencesChanged;
        _editor.Observe(workflow.State); Refresh();
    }
    private GraphPoint Center() => _editor.Viewport.ToGraph(new(_canvas.Bounds.Width / 2, _canvas.Bounds.Height / 2));
    private void WorkflowChanged(WorkflowState _) => Dispatcher.UIThread.Post(() =>
    {
        if (_disposed) return;
        if (_editor.Observe(_workflow.State)) { _canvas.CancelGesture(); _feedback = null; }
        Refresh();
    });
    private void PreferencesChanged(object? sender, PropertyChangedEventArgs _) => Refresh();
    public void Refresh()
    {
        if (_disposed) return;
        var state = _workflow.State;
        // Shell preference/document notifications can arrive before this pane's queued workflow callback.
        // Adopt this exact immutable observation before resolving its graph or diagnostics.
        if (_editor.Observe(state)) { _canvas.CancelGesture(); _feedback = null; }
        _refreshing = true;
        try
        {
            if (!_target.Items.OfType<OccurrenceChoice>().SequenceEqual(state.Occurrences)) _target.ItemsSource = state.Occurrences;
            _target.SelectedItem = state.Occurrences.SingleOrDefault(c => c.Id == _editor.Target);
            _target.PlaceholderText = _shell[_editor.Target is null ? "Graph.NoTarget" : "Graph.TargetMissing"];
            ToolTip.SetTip(_canvas, _shell["Graph.KeyboardHint"]);
            AutomationProperties.SetName(_target, _shell["Graph.Target"]);
            _target.IsEnabled = !state.Busy && !state.IsClosed;
            _canvas.IsEnabled = !state.Busy && !state.IsClosed;
            foreach (var (key, button) in _buttons)
            {
                var label = _shell["Graph." + key]; AutomationProperties.SetName(button, label); ToolTip.SetTip(button, label);
                button.IsEnabled = _editor.Graph is not null && !state.Busy && !state.IsClosed;
            }
            _buttons["Delete"].IsEnabled &= _editor.SelectedNode is not null || _editor.SelectedConnection is not null;
            var report = _editor.Report;
            _canvas.Diagnostics = report?.Diagnostics.ToArray() ?? [];
            var selected = _editor.Graph?.Nodes.SingleOrDefault(n => n.Id == _editor.SelectedNode);
            _parameter.IsVisible = selected?.Type == GraphBuiltIns.Gain;
            _parameterName.Text = _shell["Graph.Gain"] + " · " + _shell["Graph.Linear"];
            ToolTip.SetTip(_value, _shell["Workflow.GainUnit"]); AutomationProperties.SetName(_value, _shell["Workflow.Gain"]);
            ToolTip.SetTip(_commit, _shell["Graph.CommitGain"]); AutomationProperties.SetName(_commit, _shell["Graph.CommitGain"]);
            if (selected is { Type: GraphBuiltIns.Gain } && (_parameterNode != selected.Id ||
                _generation != state.Generation && !_value.IsKeyboardFocusWithin))
            {
                _parameterNode = selected.Id; _parameterBasis = _editor.Basis;
                _value.Text = selected.Parameters.TryGetValue(GraphBuiltIns.GainAmplitude, out var value) ? value.ToString() : "";
            }
            _generation = state.Generation;
            _value.IsEnabled = _commit.IsEnabled = selected is not null && GraphConnectionRules.Supported(selected) && !state.Busy;
            RefreshStatus(); _canvas.InvalidateVisual();
        }
        finally { _refreshing = false; }
    }
    private void RefreshStatus()
    {
        if (_disposed) return;
        var state = _workflow.State;
        string text; string role = "Text.Secondary";
        if (_feedback is not null) { text = _shell[_feedback]; role = "Status.Warning"; }
        else if (_editor.Attachment is null) text = _shell[_editor.Target is null ? "Graph.NoTarget" : "Graph.TargetMissing"];
        else if (_canvas.Diagnostics.Count > 0)
        {
            text = _shell["Graph.Incomplete"] + " · " + _shell[_canvas.Diagnostics[0].Reason]; role = "Status.Warning";
            if (state.Execution is { LastValidPlaying: true } execution && execution.Target == _editor.Target)
                text += " · " + _shell["Status.LastValid"];
        }
        else if (state.Execution is { } execution && execution.Target == _editor.Target)
        {
            text = _shell[execution.Blocker is not null ? execution.Blocker : execution.PreparationPending ? "Status.Preparing" :
                execution.Execution.Playing ? "Graph.Executing" : "Graph.Canonical"];
            if (execution.PreparationPending && execution.Execution.Playing) text += " · " + _shell["Status.LastValid"];
            if (execution.Blocker is not null) role = "Status.Critical";
        }
        else text = _shell["Graph.Canonical"];
        string? selection = null;
        Id<GraphConnection>? selectedCable = null;
        if (_editor.Graph is { } graph)
        {
            var node = graph.Nodes.SingleOrDefault(n => n.Id == _editor.SelectedNode);
            var edge = graph.Connections.SingleOrDefault(c => c.Id == _editor.SelectedConnection);
            if (node is not null) selection = NodeLabel(node);
            else if (edge is not null && graph.Nodes.SingleOrDefault(n => n.Id == edge.FromNodeId) is { } from &&
                graph.Nodes.SingleOrDefault(n => n.Id == edge.ToNodeId) is { } to)
            {
                selection = ConnectionLabel(graph, edge); selectedCable = edge.Id;
            }
        }
        string selectedText = selection is null ? "" : _shell["Graph.Selected"] + ": " + selection;
        _status.Text = selectedText.Length == 0 ? text : selectedText + " · " + text;
        _status.Foreground = new SolidColorBrush(Color.Parse(HostPalette.For(_shell.Preferences.Theme)[role]));
        ToolTip.SetTip(_status, _status.Text + (_canvas.Diagnostics.Count == 0 ? "" : "\n" +
            string.Join("\n", _canvas.Diagnostics.Select(d => _shell[d.Reason]).Distinct())));
        // Machine-readable identity for bounded GUI verification; UUIDs stay off the visible surface.
        AutomationProperties.SetHelpText(_canvas, selectedCable?.ToString() ?? "");
        AutomationProperties.SetName(_canvas, _shell["Pane.Graph"] + ". " + text +
            (selection is null ? "" : ". " + _shell["Graph.Selected"] + ": " + selection));
    }
    private async Task CommitGainAsync()
    {
        if (_parameterNode is not { } node || _parameterBasis is not { } basis) return;
        if (!decimal.TryParse(_value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var amplitude) || amplitude is < 0 or > 1)
        { _feedback = GraphReasons.InvalidGain; RefreshStatus(); return; }
        _feedback = null;
        if (!await _workflow.SetGraphGainAsync(basis, node, amplitude)) _feedback = _workflow.State.Detail;
        _parameterNode = null; Refresh(); _canvas.Focus();
    }
    private string NodeLabel(GraphNode node)
    {
        string type = _shell[node.Type switch { GraphBuiltIns.Source => "Graph.Source", GraphBuiltIns.Gain => "Graph.Gain",
            GraphBuiltIns.Mix => "Graph.Mix", GraphBuiltIns.Output => "Graph.Output", _ => "Graph.Unsupported" }];
        var binding = _editor.Attachment?.Sources.SingleOrDefault(s => s.NodeId == node.Id);
        var part = _editor.State?.Patterns.SelectMany(p => p.Parts).SingleOrDefault(p => p.Id == binding?.PartId);
        return part is not null ? type + " · " + part.Name : type + " " +
            (_editor.Graph!.Nodes.Where(n => n.Type == node.Type).ToList().FindIndex(n => n.Id == node.Id) + 1);
    }
    private MenuItem Menu(string text, Func<Task>? action = null)
    {
        var item = new MenuItem { Header = text };
        if (action is not null) item.Click += async (_, _) => { _menu.Close(); await action(); if (!_disposed) _canvas.Focus(); };
        return item;
    }
    private string ConnectionLabel(GraphDefinition graph, GraphConnection edge) =>
        GraphConnectionLabels.Label(graph, edge, NodeLabel, _shell["Graph.In"], _shell["Graph.Out"]);
    private void OpenMenu(GraphPoint point, bool additionsOnly = false)
    {
        Refresh();
        CancelGesture(); _menu.Items.Clear();
        if (_editor.Basis is not { } basis || _editor.Graph is not { } graph) return;
        var add = Menu(_shell["Graph.Add"]);
        foreach (var type in new[] { GraphBuiltIns.Source, GraphBuiltIns.Gain, GraphBuiltIns.Mix, GraphBuiltIns.Output })
        {
            var typeMenu = Menu(_shell[type switch { GraphBuiltIns.Source => "Graph.Source", GraphBuiltIns.Gain => "Graph.Gain",
                GraphBuiltIns.Mix => "Graph.Mix", _ => "Graph.Output" }]);
            foreach (var layout in new[] { GraphBuiltIns.Stereo, GraphBuiltIns.Mono })
            {
                var layoutMenu = Menu(layout == GraphBuiltIns.Stereo ? "Stereo L/R" : "Mono", type == GraphBuiltIns.Mix ? null :
                    async () => await _workflow.AddGraphNodeAsync(basis, type, new(point.X, point.Y), layout));
                if (type == GraphBuiltIns.Mix)
                    for (int count = 2; count <= 8; count++)
                    {
                        int inputs = count;
                        layoutMenu.Items.Add(Menu(count + " " + _shell["Graph.Inputs"], async () =>
                            await _workflow.AddGraphNodeAsync(basis, type, new(point.X, point.Y), layout, inputs)));
                    }
                typeMenu.Items.Add(layoutMenu);
            }
            add.Items.Add(typeMenu);
        }
        _menu.Items.Add(add);
        _menu.Items.Add(Menu(_shell["Graph.AddWav"], () => _import(basis, new(point.X, point.Y))));
        if (!additionsOnly)
        {
            var select = Menu(_shell["Graph.SelectNode"]);
            foreach (var node in graph.Nodes) select.Items.Add(Menu(NodeLabel(node), () =>
            { _editor.Select(new(GraphHitKind.Node, node.Id)); Refresh(); return Task.CompletedTask; }));
            _menu.Items.Add(select);
            var connections = Menu(_shell["Graph.SelectConnection"]);
            foreach (var edge in graph.Connections)
            {
                var from = graph.Nodes.SingleOrDefault(n => n.Id == edge.FromNodeId);
                var to = graph.Nodes.SingleOrDefault(n => n.Id == edge.ToNodeId);
                if (from is null || to is null) continue;
                connections.Items.Add(Menu(ConnectionLabel(graph, edge), () =>
                { _editor.Select(new(GraphHitKind.Connection, Connection: edge.Id)); Refresh(); return Task.CompletedTask; }));
            }
            _menu.Items.Add(connections);
            if (graph.Nodes.SingleOrDefault(n => n.Id == _editor.SelectedNode) is { } selected)
            {
                if (selected.Type == GraphBuiltIns.Source && _editor.Attachment is { } attachment)
                {
                    var bind = Menu(_shell["Graph.BindSource"]);
                    var placement = _workflow.State.Snapshot.State.Placements.SingleOrDefault(p => p.ItemContextId == attachment.ContextId);
                    var parts = _workflow.State.Snapshot.State.Patterns.SingleOrDefault(p => p.Id == placement?.PatternId)?.Parts ?? [];
                    foreach (var part in parts.Where(p => !attachment.Sources.Any(s => s.PartId == p.Id)))
                        bind.Items.Add(Menu(part.Name, async () => await _workflow.BindGraphSourceAsync(basis, selected.Id, part.Id)));
                    _menu.Items.Add(bind);
                }
                var connect = Menu(_shell["Graph.Connect"]);
                foreach (var output in selected.Ports.Where(p => p.Direction == GraphPortDirection.Output))
                    foreach (var target in graph.Nodes)
                        foreach (var input in target.Ports.Where(p => p.Direction == GraphPortDirection.Input))
                        {
                            int slot = target.Ports.Where(p => p.Direction == GraphPortDirection.Input).ToList().IndexOf(input) + 1;
                            var item = Menu(NodeLabel(target) + " · " + _shell["Graph.In"] + " " + slot,
                                async () => await _workflow.ConnectGraphAsync(basis, selected.Id, output.Id, target.Id, input.Id));
                            var reason = GraphConnectionRules.Refusal(graph, selected.Id, output.Id, target.Id, input.Id);
                            item.IsEnabled = reason is null; if (reason is not null) ToolTip.SetTip(item, _shell[reason]);
                            connect.Items.Add(item);
                        }
                _menu.Items.Add(connect);
                var disconnect = Menu(_shell["Graph.Disconnect"]);
                foreach (var edge in graph.Connections.Where(c => c.FromNodeId == selected.Id || c.ToNodeId == selected.Id))
                    disconnect.Items.Add(Menu(ConnectionLabel(graph, edge), async () =>
                        await _workflow.DeleteGraphElementAsync(basis, null, edge.Id)));
                _menu.Items.Add(disconnect);
            }
            if (_editor.SelectedNode is not null || _editor.SelectedConnection is not null)
                _menu.Items.Add(Menu(_shell["Graph.Delete"], _canvas.DeleteSelectedAsync));
            _menu.Items.Add(Menu(_shell["Graph.Fit"], () => { _canvas.Fit(); return Task.CompletedTask; }));
        }
        var anchor = _editor.Viewport.ToScreen(point);
        _menu.PlacementRect = new Rect(Math.Clamp(anchor.X, 0, _canvas.Bounds.Width),
            Math.Clamp(anchor.Y, 0, _canvas.Bounds.Height), 1, 1);
        _canvas.ContextMenu = _menu; _menu.Open(_canvas);
    }
    public void FocusCanvas() { if (!_disposed) _canvas.Focus(); }
    public void Frame() => _canvas.Fit();
    public void CancelGesture() { _canvas.CancelGesture(); _menu.Close(); }
    public void Dispose()
    {
        if (_disposed) return;
        CancelGesture(); _disposed = true;
        _workflow.Changed -= WorkflowChanged; _shell.PropertyChanged -= PreferencesChanged;
    }
}
