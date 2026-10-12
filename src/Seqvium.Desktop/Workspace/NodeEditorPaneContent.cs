// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Retained host surface; node-specific controls project the session's canonical observation.</summary>
internal sealed class NodeEditorPaneContent : Grid, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly NodeEditorSession _editor;
    private readonly TextBlock _scope = new() { Classes = { "caption" }, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _db = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _status = new() { Classes = { "caption" }, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _details = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _linear = new() { Classes = { "caption" } };
    private readonly TextBox _value = new() { Width = 80, Classes = { "numeric" } };
    private readonly GainLevelControl _level;
    private readonly GainTransferPlot _plot;
    private readonly Grid _gain = new() { RowDefinitions = new("Auto,Auto,Auto,Auto"), Classes = { "parameterRow" } };
    private readonly Expander _curve;
    private readonly DispatcherTimer _changeNotice = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly ScrollViewer _gainScroll, _detailsScroll;
    private readonly Button _commit, _unity, _silence;
    private bool _refreshing, _disposed;
    private string? _feedback;
    private CancellationTokenSource? _sourceCancellation;
    private (NodeEditorTarget Target, long Generation)? _sourceBasis;
    private SourceDiscovery? _sources;
    private NodeEditorTarget? _renderedTarget;
    private (Guid Lifecycle, long Generation)? _noticeBasis;
    private Task _command = Task.CompletedTask;
    internal Task PendingCommand => _command;
    internal Task PendingSourceInspection { get; private set; } = Task.CompletedTask;
    public string Title { get; private set; }
    public event Action? TitleChanged;
    public event Action<bool>? CurveExpansionChanged;

    public NodeEditorPaneContent(ShellSession shell, DesktopWorkflow workflow, NodeEditorSession editor)
    {
        _shell = shell; _workflow = workflow; _editor = editor;
        Title = shell["Pane.NodeEditor"];
        RowDefinitions = new("Auto,*,Auto");
        this.Bind(RowSpacingProperty, this.GetResourceObservable("Space.Gap"));
        this.Bind(MarginProperty, this.GetResourceObservable("Padding.Content"));
        Children.Add(_scope);
        _level = new(shell) { Focusable = true };
        _plot = new(shell) { Height = 116 };
        SetRow(_level, 1); _gain.Children.Add(_level);
        var exact = new Grid { ColumnDefinitions = new("*,Auto,Auto,Auto"), Classes = { "parameterRow" } };
        _commit = Button("Commit", async () => await CommitTextAsync());
        _commit.Classes.Add("command");
        _commit.AttachedToVisualTree += (_, _) => _commit.Content = HostIcons.Create("Check", _commit, 16);
        _unity = Button("Unity", async () => await SetAmplitudeAsync(1));
        _silence = Button("Silence", async () => await SetAmplitudeAsync(0));
        _linear.VerticalAlignment = VerticalAlignment.Center;
        exact.Children.Add(_linear); exact.Children.Add(_value); exact.Children.Add(_db); exact.Children.Add(_commit);
        SetColumn(_value, 1); SetColumn(_db, 2); SetColumn(_commit, 3); _gain.Children.Add(exact);
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(_unity); actions.Children.Add(_silence);
        SetRow(actions, 2); _gain.Children.Add(actions);
        _curve = new() { Content = _plot, IsExpanded = false, HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch, MinHeight = 0, Padding = new(0), Classes = { "nodeCurve" } };
        _curve.Bind(TemplatedControl.BackgroundProperty, this.GetResourceObservable("Surface.Background"));
        _curve.Bind(TemplatedControl.ForegroundProperty, this.GetResourceObservable("Text.Primary"));
        _curve.PropertyChanged += (_, change) =>
        { if (change.Property == Expander.IsExpandedProperty) CurveExpansionChanged?.Invoke(_curve.IsExpanded); };
        SetRow(_curve, 3); _gain.Children.Add(_curve);
        _gainScroll = new() { Content = _gain, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        _detailsScroll = new() { Content = _details };
        SetRow(_gainScroll, 1); SetRow(_detailsScroll, 1); Children.Add(_gainScroll); Children.Add(_detailsScroll);
        SetRow(_status, 2); Children.Add(_status);
        AutomationProperties.SetAutomationId(_value, "NodeEditor.Amplitude");
        AutomationProperties.SetAutomationId(_curve, "NodeEditor.Curve");
        AutomationProperties.SetAutomationId(_details, "NodeEditor.Details");
        AutomationProperties.SetAutomationId(_scope, "NodeEditor.Scope");
        AutomationProperties.SetAutomationId(_status, "NodeEditor.Status");
        _changeNotice.Tick += (_, _) =>
        { _changeNotice.Stop(); if (!_disposed) { _editor.DismissChangeNotice(); RefreshPreview(); } };
        // TextChanged is routed later by Avalonia; TextProperty changes synchronously under the
        // refresh guard, so a host-to-control echo cannot create a new user draft after a commit.
        _value.PropertyChanged += (_, change) =>
        {
            if (change.Property != TextBox.TextProperty || _refreshing || !_command.IsCompleted) return;
            if (_editor.Observe(_workflow.State)) { Refresh(); return; }
            if (_editor.BeginDraft())
            {
                if (Parse(_value.Text, out var amplitude)) _editor.UpdateDraft(amplitude);
                RefreshPreview();
            }
        };
        _value.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter) { e.Handled = true; await CommitTextAsync(); }
        };
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Escape || !_editor.HasDraft && !_level.HasGesture) return;
            e.Handled = true; CancelGesture(); Refresh();
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        _level.Begin = () =>
        {
            _editor.Observe(_workflow.State);
            return _command.IsCompleted && _editor.BeginDraft();
        };
        _level.Preview += value =>
        {
            _editor.UpdateDraft(value);
            _refreshing = true; _value.Text = value.ToString(CultureInfo.InvariantCulture); _refreshing = false;
            RefreshPreview();
        };
        _level.Completed += async () => await CommitDraftAsync();
        _level.Cancelled += () => { _editor.CancelDraft(); Refresh(); };
        workflow.Changed += WorkflowChanged; shell.PropertyChanged += PreferencesChanged;
        Refresh();
    }
    private Button Button(string key, Func<Task> action)
    {
        var button = new Button { Classes = { "shell" }, Margin = new(2, 0) };
        AutomationProperties.SetAutomationId(button, "NodeEditor." + key);
        button.Click += async (_, _) => { if (_command.IsCompleted && !_disposed) await action(); };
        return button;
    }
    private bool Parse(string? text, out decimal amplitude) =>
        (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out amplitude) ||
         decimal.TryParse(text, NumberStyles.Float, CultureInfo.GetCultureInfo(
             _shell.Preferences.Language == HostLanguage.Russian ? "ru-RU" : "en-US"), out amplitude)) && amplitude is >= 0 and <= 1;
    private void WorkflowChanged(WorkflowState _) => Dispatcher.UIThread.Post(() => { if (!_disposed) Refresh(); });
    private void PreferencesChanged(object? sender, PropertyChangedEventArgs _) => Refresh();
    public void Refresh()
    {
        if (_disposed) return;
        bool changed = _editor.Observe(_workflow.State);
        var noticeBasis = (_workflow.State.Document.LifecycleId, _workflow.State.Generation);
        if (_editor.ChangedElsewhere && _noticeBasis != noticeBasis)
        { _noticeBasis = noticeBasis; _changeNotice.Stop(); _changeNotice.Start(); }
        else if (!_editor.ChangedElsewhere) _changeNotice.Stop();
        if (changed || _renderedTarget != _editor.Target)
        {
            _level.Cancel(); CancelSources(); _feedback = null;
            _renderedTarget = _editor.Target;
        }
        _refreshing = true;
        try
        {
            var node = _editor.Node;
            string title = node is null ? _shell["Pane.NodeEditor"] : NodeName(node);
            if (title != Title) { Title = title; TitleChanged?.Invoke(); }
            _scope.Text = Scope();
            _scope.IsVisible = _scope.Text.Length > 0;
            AutomationProperties.SetName(this, Title + ". " + (node is null ?
                _shell[_editor.Target is null ? "NodeEditor.NoTarget" : "NodeEditor.TargetMissing"] : _scope.Text));
            var target = _editor.Target;
            AutomationProperties.SetHelpText(this, target?.NodeId.ToString() ?? "");
            bool gain = NodeEditorSession.SupportsGain(node);
            _gainScroll.IsVisible = gain; _detailsScroll.IsVisible = !gain;
            _linear.Text = _shell["NodeEditor.LevelLabel"];
            ToolTip.SetTip(_value, _shell["Workflow.GainUnit"]);
            _curve.Header = _shell["NodeEditor.Curve"];
            ToolTip.SetTip(_curve, _shell["NodeEditor.Transfer"]);
            foreach (var (button, key) in new[] { (_commit, "Commit"), (_unity, "Unity"), (_silence, "Silence") })
            {
                if (button != _commit) button.Content = _shell["NodeEditor." + key];
                AutomationProperties.SetName(button, _shell["NodeEditor." + key]);
                if (button == _commit) ToolTip.SetTip(button, _shell["NodeEditor.Commit"]);
                button.IsEnabled = _editor.CanEditGain && _command.IsCompleted;
            }
            _value.IsEnabled = _level.IsEnabled = _editor.CanEditGain && _command.IsCompleted;
            AutomationProperties.SetName(_value, _shell["Graph.Linear"]);
            AutomationProperties.SetName(_level, _shell["NodeEditor.Level"]);
            if (!_editor.HasDraft)
                _value.Text = node?.Parameters.TryGetValue(GraphBuiltIns.GainAmplitude, out var value) == true ? value.ToString() : "";
            if (!gain) _details.Text = Details();
            _details.Classes.Set("secondary", node is null && _editor.Target is null);
            _details.Classes.Set("warning", node is null && _editor.Target is not null);
            RefreshPreview();
            if (node is { Type: GraphBuiltIns.Source } && GraphConnectionRules.Supported(node) && IsEffectivelyVisible &&
                target is not null && _sourceBasis != (target, _workflow.State.Generation))
            {
                _sourceBasis = (target, _workflow.State.Generation);
                _sourceCancellation = new();
                PendingSourceInspection = InspectSourcesAsync(target, _workflow.State.Generation, _sourceCancellation.Token);
            }
        }
        finally { _refreshing = false; }
    }
    private void RefreshPreview()
    {
        bool valid = _editor.HasDraft ? Parse(_value.Text, out _) : _editor.Amplitude is >= 0 and <= 1;
        decimal? amplitude = valid ? _editor.HasDraft ? _editor.Draft?.After : _editor.Amplitude : null;
        _level.Amplitude = amplitude; _level.InvalidateVisual();
        _plot.Amplitude = amplitude; _plot.IsDraft = _editor.HasDraft; _plot.InvalidateVisual();
        _db.IsVisible = amplitude is not null;
        _curve.IsVisible = amplitude is not null;
        _db.Text = amplitude == 0 ? _shell["NodeEditor.SilentDb"] : amplitude is { } a
            ? NodeEditorSession.Decibels(a)?.ToString("0.00", CultureInfo.InvariantCulture) + " dB" : _shell["Graph.InvalidValue"];
        string status = _editor.Node is null ? "" : _feedback is not null ? _shell[_feedback] : _editor.ChangedElsewhere
            ? _shell["NodeEditor.ChangedElsewhere"] : _editor.HasDraft
                ? string.Join(" · ", new[] { _shell["NodeEditor.Draft"], valid ? "" : _shell["Graph.InvalidValue"] }.Where(s => s.Length > 0)) : "";
        bool warning = _editor.HasDraft || _editor.ChangedElsewhere || _feedback is not null;
        if (_editor.Node is { } node && _editor.Graph is { } graph && _editor.State is { } state)
        {
            var diagnostics = GraphDiagnostics.Inspect(state).Single(r => r.GraphId == graph.Id).Diagnostics;
            var reasons = diagnostics.Where(d => d.NodeId is null || d.NodeId == node.Id).Select(d => _shell[d.Reason]).Distinct();
            if (reasons.Any())
            { warning = true; status = string.Join(" · ", new[] { status }.Concat(reasons).Where(s => s.Length > 0)); }
            var execution = _workflow.State.Execution;
            if (execution is not null && execution.Target == _editor.Target?.AttachmentId && execution.LastValidPlaying)
            { warning = true; status = string.Join(" · ", new[] { status, _shell["Status.LastValid"] }.Where(s => s.Length > 0)); }
        }
        _status.Text = status;
        _status.IsVisible = status.Length > 0;
        _status.Classes.Set("warning", warning);
        _value.Classes.Set("invalid", !valid);
    }
    private string NodeName(GraphNode node) => _shell[node.Type switch
    {
        GraphBuiltIns.Source => "Graph.Source", GraphBuiltIns.Gain => "Graph.Gain", GraphBuiltIns.Mix => "Graph.Mix",
        GraphBuiltIns.Output => "Graph.Output", _ => "Graph.Unsupported"
    }] + " " + (_editor.Graph!.Nodes.Where(n => n.Type == node.Type).ToList().FindIndex(n => n.Id == node.Id) + 1);
    private string Scope()
    {
        if (_editor.Attachment is not { } attachment || _editor.State is not { } state) return "";
        var context = state.Contexts.SingleOrDefault(c => c.Id == attachment.ContextId);
        var placements = state.Placements.Where(p => p.ItemContextId == attachment.ContextId ||
            p.ContainingContextId == attachment.ContextId).ToArray();
        // A containing scope can own several placements; do not label it as one arbitrary source.
        var placement = placements.Length == 1 ? placements[0] : null;
        var pattern = state.Patterns.SingleOrDefault(p => p.Id == placement?.PatternId);
        var parts = new List<string>();
        if (pattern?.Name is { } name) parts.Add(name);
        if (context is not null && context.Name is not ("Item processing" or "Containing processing")) parts.Add(context.Name);
        parts.Add(_shell[context?.Level == LocalProcessingLevel.Item ? "NodeEditor.ItemScopeShort" : "NodeEditor.ContainingScopeShort"]);
        if (placement is not null && state.Placements.Count(p => p.PatternId == placement.PatternId) > 1)
            parts.Add(_shell["NodeEditor.Placement"] + " " + (state.Placements.IndexOf(placement) + 1));
        return string.Join(" · ", parts.Distinct());
    }
    private string Details()
    {
        if (_editor.Node is not { } node || _editor.State is not { } state || _editor.Attachment is not { } attachment ||
            _editor.Graph is not { } graph) return _shell[_editor.Target is null ? "NodeEditor.NoTarget" : "NodeEditor.TargetMissing"];
        var lines = new List<string>();
        if (!GraphConnectionRules.Supported(node) || node.Type == GraphBuiltIns.Gain)
        {
            lines.Add(_shell["NodeEditor.HostDetails"]); lines.Add(node.Type + " · v" + node.StateVersion);
            lines.Add(_shell["NodeEditor.NodeId"] + ": " + node.Id);
            foreach (var port in node.Ports) lines.Add(port.Direction + " · " + port.Role + " · " + port.SignalClass + " · " + port.Layout);
            return string.Join("\n", lines);
        }
        if (node.Type == GraphBuiltIns.Source)
        {
            var binding = attachment.Sources.SingleOrDefault(s => s.NodeId == node.Id);
            var placement = state.Placements.SingleOrDefault(p => p.Id == binding?.PlacementId);
            var pattern = state.Patterns.SingleOrDefault(p => p.Id == placement?.PatternId);
            var part = pattern?.Parts.SingleOrDefault(p => p.Id == binding?.PartId);
            var sound = state.Sounds.SingleOrDefault(s => s.Id == part?.SoundId);
            lines.Add(part is null ? _shell["Graph.Unbound"] : _shell["NodeEditor.Part"] + ": " + part.Name);
            if (sound is not null) lines.Add(_shell["NodeEditor.Sound"] + ": " + sound.Name);
            if (binding is not null)
            {
                lines.Add(_shell["NodeEditor.Placement"] + " " + (state.Placements.ToList().FindIndex(p => p.Id == binding.PlacementId) + 1) +
                    " · " + binding.Boundary);
                ToolTip.SetTip(_details, _shell["NodeEditor.Placement"] + ": " + binding.PlacementId + "\n" +
                    _shell["NodeEditor.Part"] + ": " + binding.PartId);
            }
            foreach (var resourceId in sound?.ResourceIds ?? [])
            {
                var resource = state.Resources.SingleOrDefault(r => r.Id == resourceId);
                var observed = _sources?.Sources.SingleOrDefault(r => r.ResourceId == resourceId);
                lines.Add((resource?.Name ?? resourceId.ToString()) + " · " + _shell[observed is null ? "NodeEditor.Checking" :
                    observed.Availability == SourceAvailability.Supported ? "NodeEditor.ResourceReady" : "NodeEditor.ResourceUnavailable"]);
                if (observed is { Availability: SourceAvailability.Supported }) lines.Add($"{observed.Rate} Hz · " +
                    (observed.Channels == 1 ? "Mono" : "Stereo L/R") + " · " +
                    ((double)observed.Frames!.Value / observed.Rate!.Value).ToString("0.###", CultureInfo.InvariantCulture) + " s");
            }
            if (sound is null || sound.ResourceIds.IsEmpty) lines.Add(_shell["NodeEditor.ResourceUnavailable"]);
        }
        else
        {
            lines.Add(_shell[node.Type == GraphBuiltIns.Mix ? "NodeEditor.Sum" : "NodeEditor.OutputPath"]);
            if (node.Type == GraphBuiltIns.Output) lines.Add(_shell[attachment.OutputNodeId == node.Id
                ? "NodeEditor.Designated" : "NodeEditor.NotDesignated"]);
            int index = 0;
            foreach (var port in node.Ports.Where(p => p.Direction == GraphPortDirection.Input))
            {
                index++;
                lines.Add(_shell["Graph.In"] + " " + index + " · " + port.Layout);
                var edges = graph.Connections.Where(c => c.ToNodeId == node.Id && c.ToPortId == port.Id).ToArray();
                if (edges.Length == 0) lines.Add("  " + _shell["NodeEditor.Disconnected"]);
                foreach (var edge in edges) lines.Add("  " + GraphConnectionLabels.Label(graph, edge, NodeName, _shell["Graph.In"], _shell["Graph.Out"]));
            }
            ToolTip.SetTip(_details, string.Join("\n", node.Ports.Select(p => p.Role + ": " + p.Id)));
        }
        return string.Join("\n", lines);
    }
    private async Task InspectSourcesAsync(NodeEditorTarget target, long generation, CancellationToken cancellation)
    {
        try
        {
            var result = await _workflow.InspectNodeSourcesAsync(target, generation, cancellation);
            if (_disposed || cancellation.IsCancellationRequested || _editor.Target != target ||
                _workflow.State.Generation != generation || _workflow.State.Document.LifecycleId != target.LifecycleId) return;
            _sources = result; _details.Text = Details();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
    }
    private void CancelSources()
    {
        _sourceCancellation?.Cancel(); _sourceCancellation?.Dispose(); _sourceCancellation = null;
        _sourceBasis = null; _sources = null;
    }
    private async Task CommitTextAsync()
    {
        if (_disposed || !_command.IsCompleted) return;
        bool changed = _editor.Observe(_workflow.State);
        if (changed) { Refresh(); return; }
        if (!Parse(_value.Text, out var amplitude)) { _feedback = GraphReasons.InvalidGain; RefreshPreview(); return; }
        if (!_editor.BeginDraft()) return;
        _editor.UpdateDraft(amplitude); await CommitDraftAsync();
    }
    private async Task SetAmplitudeAsync(decimal value)
    {
        _editor.Observe(_workflow.State); _level.Cancel(); _editor.CancelDraft();
        if (!_editor.BeginDraft()) return;
        _editor.UpdateDraft(value); await CommitDraftAsync();
    }
    private async Task CommitDraftAsync()
    {
        if (_disposed || !_command.IsCompleted) return;
        _editor.Observe(_workflow.State);
        var proposal = _editor.CompleteDraft();
        if (proposal is null) { Refresh(); return; }
        _feedback = null;
        _command = _workflow.SetGraphGainAsync(proposal);
        Refresh();
        bool success = await (Task<bool>)_command;
        if (_disposed) return;
        _feedback = _editor.Target != proposal.Target ? null : success ? null : _workflow.State.Detail;
        Refresh();
    }
    public void FocusEditor()
    {
        if (_disposed || !IsEffectivelyVisible) return;
        if (_value.IsVisible && _gainScroll.IsVisible && _value.IsEnabled) _value.Focus();
        else { Focusable = true; Focus(); }
    }
    public void CancelGesture()
    {
        _level.Cancel(); _editor.CancelDraft(); _editor.DismissChangeNotice(); _changeNotice.Stop(); CancelSources();
        if (!_disposed) Refresh();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _changeNotice.Stop(); _level.Cancel(); _editor.CancelDraft(); CancelSources();
        _workflow.Changed -= WorkflowChanged; _shell.PropertyChanged -= PreferencesChanged;
    }
}
