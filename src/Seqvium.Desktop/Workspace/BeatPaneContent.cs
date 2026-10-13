// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Retained, locally scrolling projection of canonical Pattern parts and notes.</summary>
internal sealed class BeatPaneContent : Grid, IDisposable
{
    private sealed record Channel(Id<MusicalPart> Part, TextBlock Label, TextBlock Warning, ToggleButton[] Steps);
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly Func<BeatEditBasis, Func<bool>, Task> _import;
    private readonly Button _create = new(), _add = new(), _apply = new() { Classes = { "command" } };
    private readonly ComboBox _target = new() { MinWidth = 160 };
    private readonly TextBox _name = new() { MinWidth = 160 };
    private readonly TextBlock _message = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { "secondary" } };
    private readonly StackPanel _rows = new() { Classes = { "tight" } };
    private readonly StackPanel _patternControls = new() { Classes = { "tight" } };
    private readonly List<Channel> _channels = [];
    private readonly Dictionary<ToggleButton, BeatEditBasis> _armed = [];
    private readonly HashSet<ToggleButton> _expired = [];
    private BeatEditBasis? _basis, _nameBasis;
    private Id<Pattern>? _patternId;
    private Guid _lifecycle;
    private long _generation = -1, _interaction;
    private bool _refreshing, _disposed;
    internal Task PendingCommand { get; private set; } = Task.CompletedTask;

    internal BeatPaneContent(ShellSession shell, DesktopWorkflow workflow, Func<BeatEditBasis, Func<bool>, Task> import)
    {
        _shell = shell; _workflow = workflow; _import = import;
        var body = new StackPanel { Classes = { "spaced" } };
        body.Bind(MarginProperty, this.GetResourceObservable("Padding.Content"));
        var commands = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "compact" } };
        commands.Children.Add(_create); commands.Children.Add(_add); body.Children.Add(commands);
        _patternControls.Children.Add(_target);
        var rename = new Grid { ColumnDefinitions = new("*,Auto"), Classes = { "parameterRow" } };
        rename.Children.Add(_name); SetColumn(_apply, 1); rename.Children.Add(_apply);
        _apply.Content = HostIcons.Create("Check", _apply, 16);
        _patternControls.Children.Add(rename); body.Children.Add(_patternControls);
        body.Children.Add(_message); body.Children.Add(_rows);
        Children.Add(new ScrollViewer { Content = body, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        AutomationProperties.SetAutomationId(_create, "Beat.Create"); AutomationProperties.SetAutomationId(_add, "Beat.AddWav");
        AutomationProperties.SetAutomationId(_target, "Beat.Target"); AutomationProperties.SetAutomationId(_name, "Beat.Name");
        AutomationProperties.SetAutomationId(_apply, "Beat.ApplyName");
        _create.Click += async (_, _) =>
        {
            CancelGesture(); var state = workflow.State;
            PendingCommand = workflow.CreateBeatAsync(state.Document.LifecycleId, state.Generation, shell["Beat.DefaultName"]);
            await PendingCommand; Refresh();
        };
        _add.Click += async (_, _) =>
        {
            if (_basis is not { } basis) return;
            CancelGesture(); long interaction = _interaction;
            PendingCommand = _import(basis, () => !_disposed && IsEffectivelyVisible && _interaction == interaction);
            await PendingCommand; Refresh();
        };
        _target.SelectionChanged += async (_, _) =>
        {
            if (_refreshing || _target.SelectedItem is not OccurrenceChoice choice || choice.Id == workflow.State.Selected) return;
            CancelGesture(); PendingCommand = workflow.SelectAsync(choice.Id); await PendingCommand; Refresh();
        };
        _name.GotFocus += (_, _) => _nameBasis = _basis;
        _apply.Click += async (_, _) => await CommitNameAsync();
        _name.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Enter) { args.Handled = true; await CommitNameAsync(); }
            else if (args.Key == Key.Escape) { CancelGesture(); _name.Text = SelectedPattern()?.Name; FocusEditor(); args.Handled = true; }
        };
        workflow.Changed += WorkflowChanged; shell.PropertyChanged += PreferencesChanged;
        Refresh();
    }

    private Pattern? SelectedPattern() => BeatSteps.Resolve(_workflow.State).Pattern;
    private void WorkflowChanged(WorkflowState _) => Dispatcher.UIThread.Post(() => { if (!_disposed) Refresh(); });
    private void PreferencesChanged(object? sender, PropertyChangedEventArgs _) => Refresh();
    private void Label(Control control, string key)
    {
        string text = _shell[key]; AutomationProperties.SetName(control, text); ToolTip.SetTip(control, text);
        if (control is Button button && button != _apply) button.Content = text;
    }
    internal void Refresh()
    {
        if (_disposed) return;
        var observation = _workflow.State; var state = observation.Snapshot.State;
        var (pattern, basis) = BeatSteps.Resolve(observation);
        if (_lifecycle != observation.Document.LifecycleId || _generation != observation.Generation || _basis?.Attachment != basis?.Attachment)
        {
            CancelGesture(); _interaction++; _name.Text = pattern?.Name; _nameBasis = basis;
            _lifecycle = observation.Document.LifecycleId; _generation = observation.Generation;
        }
        _basis = basis;
        _refreshing = true;
        try
        {
            Label(_create, "Beat.Create"); Label(_add, "Beat.AddWav"); Label(_name, "Beat.Name"); Label(_apply, "Beat.ApplyName"); Label(_target, "Beat.Target");
            bool available = !observation.Busy && !observation.IsClosed;
            _create.IsEnabled = available && state.Settings.Meter is { Numerator: 4, Denominator: 4 };
            string? refusal = BeatSteps.PatternRefusal(state, pattern);
            _message.Text = refusal is null ? "" : _shell[refusal]; _message.IsVisible = refusal is not null;
            _patternControls.IsVisible = pattern is not null;
            if (!_target.Items.OfType<OccurrenceChoice>().SequenceEqual(observation.Occurrences)) _target.ItemsSource = observation.Occurrences;
            _target.SelectedItem = observation.Occurrences.SingleOrDefault(c => c.Id == observation.Selected);
            _target.IsVisible = observation.Occurrences.Length > 1; _target.IsEnabled = available;
            _name.IsEnabled = _apply.IsEnabled = available && refusal is null;
            var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == observation.Selected);
            var graph = state.Graphs.SingleOrDefault(g => g.Id == attachment?.GraphId);
            string? channelRefusal = pattern is null || attachment is null || graph is null ? refusal :
                DesktopWorkflow.BeatChannelRefusal(state, pattern, attachment, graph);
            _add.IsEnabled = available && channelRefusal is null && basis is not null;
            _add.IsVisible = pattern is not null;
            ToolTip.SetShowOnDisabled(_add, true);
            if (channelRefusal is not null) ToolTip.SetTip(_add, _shell["Beat.AddWav"] + " · " + _shell[channelRefusal]);
            if (pattern is null || _patternId != pattern.Id || !_channels.Select(c => c.Part).SequenceEqual(pattern.Parts.Select(p => p.Id)))
            {
                _rows.Children.Clear(); _channels.Clear(); _expired.Clear(); _patternId = pattern?.Id;
                if (pattern is not null)
                    foreach (var part in pattern.Parts) AddRow(part);
            }
            foreach (var channel in _channels)
            {
                var part = pattern!.Parts.Single(p => p.Id == channel.Part);
                channel.Label.Text = part.Name; ToolTip.SetTip(channel.Label, part.Name);
                string? partRefusal = refusal ?? BeatSteps.PartRefusal(state, part);
                channel.Warning.Text = partRefusal is null ? "" : _shell[partRefusal];
                channel.Warning.IsVisible = partRefusal is not null;
                for (int step = 0; step < BeatSteps.Count; step++)
                {
                    var button = channel.Steps[step];
                    var notes = part.Notes.Where(n => BeatSteps.Step(n) == step).ToArray();
                    button.IsChecked = notes.Length > 0; button.IsEnabled = available && partRefusal is null;
                    string text = $"{part.Name} · {_shell["Beat.Step"]} {step + 1} · {_shell[notes.Length > 0 ? "Beat.On" : "Beat.Off"]}";
                    AutomationProperties.SetName(button, text);
                    ToolTip.SetTip(button, text + (partRefusal is null ? " · " + _shell["Beat.Keyboard"] : " · " + _shell[partRefusal]));
                }
            }
        }
        finally { _refreshing = false; }
    }

    private void AddRow(MusicalPart part)
    {
        var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Width = 120,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };
        var warning = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { "warning", "caption" }, MaxWidth = 660 };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "compact" } };
        row.Children.Add(label);
        var buttons = new ToggleButton[BeatSteps.Count];
        for (int beat = 0; beat < 4; beat++)
        {
            var group = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "tight" } };
            group.Bind(MarginProperty, this.GetResourceObservable("Padding.Control"));
            for (int offset = 0; offset < 4; offset++)
            {
                int step = beat * 4 + offset;
                var button = new ToggleButton { Width = 28, Content = (step + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) };
                AutomationProperties.SetAutomationId(button, $"Beat.Step.{part.Id}.{step}");
                ToolTip.SetShowOnDisabled(button, true);
                button.AddHandler(PointerPressedEvent, (_, _) => Arm(button), RoutingStrategies.Tunnel);
                button.AddHandler(KeyDownEvent, (_, args) =>
                {
                    if (args.Key is Key.Enter or Key.Space) Arm(button);
                    if (args.KeyModifiers != KeyModifiers.None) return;
                    int next = args.Key == Key.Left ? step - 1 : args.Key == Key.Right ? step + 1 : args.Key == Key.Home ? 0 : args.Key == Key.End ? 15 : step;
                    int currentRow = _channels.FindIndex(c => c.Part == part.Id);
                    int nextRow = args.Key == Key.Up ? currentRow - 1 : args.Key == Key.Down ? currentRow + 1 : currentRow;
                    if ((next != step || nextRow != currentRow) && next is >= 0 and < BeatSteps.Count && nextRow >= 0 && nextRow < _channels.Count)
                    { _channels[nextRow].Steps[next].Focus(); args.Handled = true; }
                }, RoutingStrategies.Tunnel);
                button.Click += async (_, _) =>
                {
                    if (_refreshing || _disposed || !button.IsEnabled || !button.IsEffectivelyVisible || _expired.Remove(button))
                    { Refresh(); return; }
                    if (!_channels.Any(c => c.Steps.Contains(button))) { Refresh(); return; }
                    // Accessibility invocation has no preceding pointer/key press. It resolves this
                    // live retained control; a cancelled held gesture may never use that fallback.
                    var basis = _armed.Remove(button, out var armed) ? armed : _basis;
                    if (basis is null) { Refresh(); return; }
                    PendingCommand = _workflow.ToggleBeatStepAsync(basis, part.Id, step);
                    await PendingCommand; Refresh();
                };
                buttons[step] = button; group.Children.Add(button);
            }
            row.Children.Add(group);
        }
        _channels.Add(new(part.Id, label, warning, buttons));
        var body = new StackPanel { Classes = { "tight" } }; body.Children.Add(row); body.Children.Add(warning); _rows.Children.Add(body);
    }
    private void Arm(ToggleButton button)
    {
        if (!_disposed && button.IsEnabled && _basis is { } basis) { _expired.Remove(button); _armed[button] = basis; }
    }
    private async Task CommitNameAsync()
    {
        if (_nameBasis is not { } basis) return;
        PendingCommand = _workflow.RenameBeatAsync(basis, _name.Text ?? ""); await PendingCommand; Refresh();
    }
    public void CancelGesture() { _expired.UnionWith(_armed.Keys); _armed.Clear(); _nameBasis = null; }
    internal void Suspend() { _interaction++; CancelGesture(); }
    public void FocusEditor()
    {
        if (_disposed || !IsEffectivelyVisible) return;
        if (_channels.FirstOrDefault()?.Steps.FirstOrDefault(b => b.IsEnabled) is { } step) step.Focus(); else _create.Focus();
    }
    public void Dispose()
    {
        if (_disposed) return;
        CancelGesture(); _disposed = true;
        _workflow.Changed -= WorkflowChanged; _shell.PropertyChanged -= PreferencesChanged;
    }
}
