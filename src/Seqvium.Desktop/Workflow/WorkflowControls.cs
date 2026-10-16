// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

/// <summary>One retained command row in the R3 frame. Diagnostics require an explicit details action.</summary>
internal sealed class WorkflowControls : Grid
{
    private const string GainFormat = "0.00##########################";
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly Func<string, Task> _action;
    private readonly Dictionary<string, Button> _buttons = [];
    private readonly List<(TextBlock Text, string Key)> _menuLabels = [];
    private readonly StackPanel _project = new() { Orientation = Orientation.Horizontal, Classes = { "joined" } };
    private readonly StackPanel _overflowBody = new() { Classes = { "tight" }, MinWidth = 180 };
    private readonly StackPanel _selectionPopup = new() { Spacing = 6 };

    private readonly StackPanel _musicalParameters = new()
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center, Classes = { "compact" }, Margin = new(4, 0)
    };

    private readonly TextBlock _tempo = new() { FontWeight = FontWeight.SemiBold };
    private readonly Button _tempoAction = new() { Classes = { "compact" } };
    private readonly TextBox _tempoEdit = new() { Width = 88, Classes = { "numeric" } };
    private (Guid Lifecycle, long Generation)? _tempoBasis;
    private System.Globalization.CultureInfo _tempoCulture = System.Globalization.CultureInfo.InvariantCulture;
    private readonly TextBlock _meter = new() { FontWeight = FontWeight.SemiBold, Margin = new(8, 0, 4, 0) };
    private readonly Border _parameterSlot = new();
    private readonly Border _overflowParameters = new() { Margin = new(6) };

    private readonly Border _selectionSlot = new()
    {
        Margin = new(4, 0), Width = 220,
        VerticalAlignment = VerticalAlignment.Center
    };

    private readonly ComboBox _occurrences = new()
    {
        Classes = { "workflow" }, MinWidth = 100,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private readonly NumericUpDown _gain = new()
    {
        Classes = { "workflow" }, Minimum = 0, Maximum = 1,
        Increment = 0.05m, FormatString = GainFormat, Width = 64, ShowButtonSpinner = false,
        NumberFormat = System.Globalization.CultureInfo.InvariantCulture.NumberFormat
    };

    private readonly TextBlock _gainLabel = new()
        { VerticalAlignment = VerticalAlignment.Center, Margin = new(4, 0, 4, 0) };

    private readonly GainLevelControl _gainLevel;

    private readonly StackPanel _gainGroup = new()
        { Orientation = Orientation.Horizontal, Classes = { "parameterRow" } };

    private readonly Border _gainSlot = new(), _overflowGain = new();
    private (GraphEditBasis Basis, Id<GraphNode> Node, decimal Value, Guid Id, long Transport)? _gainGesture;

    private readonly Button _information = new() { Classes = { "shell", "command" } };
    private readonly Button _overflow = new() { Classes = { "shell", "command" } };
    private readonly Button _mode = new() { Classes = { "compact" }, MinWidth = 62 };
    private readonly TextBlock _source = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _chain = new() { Classes = { "secondary" }, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _device = new() { Classes = { "secondary" }, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _detail = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _technical = new() { Classes = { "secondary" }, TextWrapping = TextWrapping.Wrap };
    private readonly Expander _technicalExpander = new();
    private readonly TextBlock _statusText = new() { TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly Flyout _details;
    private bool _refreshing, _compact, _openInOverflow, _blocked;
    private Seqvium.Core.Id<Seqvium.Core.GraphAttachment>? _selection;
    private (GraphEditBasis Basis, Id<GraphNode> Node)? _gainBasis;
    internal bool CanCancelImport { get; set; }
    internal bool GainShownElsewhere { get; set; }
    internal bool OccurrenceShownElsewhere { get; set; }
    internal Task PendingTempoCommand { get; private set; } = Task.CompletedTask;
    internal Task PendingGainCommand { get; private set; } = Task.CompletedTask;
    internal Task PendingGainPreview { get; private set; } = Task.CompletedTask;
    internal void CancelGainGesture() => _gainLevel.Cancel();

    internal Button StatusControl { get; } = new()
    {
        Classes = { "shell", "status" },
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    internal WorkflowControls(ShellSession shell, DesktopWorkflow workflow, Func<string, Task> action)
    {
        _shell = shell;
        _workflow = workflow;
        _action = action;
        _gainLevel = new(shell) { SizeRole = "Fader", Fader = true, Width = 112, Focusable = true };
        AutomationProperties.SetAutomationId(_gainLevel, "Workflow.GainLevel");
        _gainGroup.Children.Add(_gainLabel);
        _gainGroup.Children.Add(_gainLevel);
        _gainGroup.Children.Add(_gain);
        _gainSlot.Child = _gainGroup;
        _gainLevel.Begin = () =>
        {
            if (_gain.Classes.Contains("invalid") || !_gain.IsEnabled) return false;
            BeginGainEdit();
            if (_gainBasis is not { } basis || workflow.State.Gain is not { } value) return false;
            var id = Guid.NewGuid();
            _gainGesture = (basis.Basis, basis.Node, value, id, workflow.State.TransportGeneration);
            PendingGainPreview = workflow.BeginGainPreviewAsync(id, basis.Basis, basis.Node);
            _gainBasis = null;
            return true;
        };
        _gainLevel.Preview += value =>
        {
            if (_gainGesture is not { } gesture) return;
            _gainGesture = gesture with { Value = value };
            PendingGainPreview = workflow.PreviewGainAsync(gesture.Id, value);
            _refreshing = true;
            _gain.Value = value;
            _gain.Text = value.ToString(GainFormat, System.Globalization.CultureInfo.InvariantCulture);
            _refreshing = false;
            _gainLevel.Amplitude = value;
            _gainLevel.InvalidateVisual();
        };
        _gainLevel.Completed += async () =>
        {
            if (_gainGesture is not { } gesture) return;
            _gainGesture = null;
            PendingGainCommand = workflow.CompleteGainPreviewAsync(gesture.Id, gesture.Value);
            await PendingGainCommand;
            Refresh();
        };
        _gainLevel.Cancelled += () =>
        {
            if (_gainGesture is { } gesture)
                PendingGainPreview = workflow.CancelGainPreviewAsync(gesture.Id);
            _gainGesture = null;
            Refresh();
        };
        this.Bind(HeightProperty, this.GetResourceObservable("Toolbar.Height"));
        ColumnDefinitions = new("Auto,*,Auto");
        var commands = new StackPanel
        {
            Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Classes = { "joined" }
        };
        foreach (var key in new[] { "New", "Open", "Save", "Import" }) _project.Children.Add(Command(key));
        commands.Children.Add(_project);
        commands.Children.Add(Separator());
        foreach (var key in new[] { "Play", "Stop", "Panic" }) commands.Children.Add(Command(key));
        commands.Children.Add(_mode);
        AutomationProperties.SetAutomationId(_mode, "Workflow.Mode");
        _mode.Click += async (_, _) => await workflow.SetSongModeAsync(!workflow.State.SongMode);
        _tempo.Bind(TextBlock.FontSizeProperty, this.GetResourceObservable("Type.Transport"));
        _tempoAction.Content = _tempo;
        _musicalParameters.Children.Add(_tempoAction);
        var tempoPopup = new StackPanel { Classes = { "spaced" } };
        var tempoRow = new Grid { ColumnDefinitions = new("Auto,Auto,Auto"), Classes = { "parameterRow" } };
        tempoRow.Children.Add(_tempoEdit);
        var bpm = new TextBlock { Text = "BPM" };
        Grid.SetColumn(bpm, 1);
        tempoRow.Children.Add(bpm);
        var tempoApply = new Button { Classes = { "command" } };
        tempoApply.Content = HostIcons.Create("Check", tempoApply, 16);
        Grid.SetColumn(tempoApply, 2);
        tempoRow.Children.Add(tempoApply);
        tempoPopup.Children.Add(tempoRow);
        var tempoFlyout = new Flyout { Content = tempoPopup };
        _tempoAction.Flyout = tempoFlyout;
        AutomationProperties.SetAutomationId(_tempoAction, "Workflow.EditTempo");
        AutomationProperties.SetAutomationId(_tempoEdit, "Workflow.TempoValue");
        AutomationProperties.SetAutomationId(tempoApply, "Workflow.ApplyTempo");
        tempoFlyout.Opened += (_, _) =>
        {
            var state = workflow.State;
            _tempoBasis = (state.Document.LifecycleId, state.Generation);
            _tempoCulture =
                System.Globalization.CultureInfo.GetCultureInfo(_shell.Preferences.Language == HostLanguage.Russian
                    ? "ru-RU"
                    : "en-US");
            _tempoEdit.Text = _shell.Tempo;
            _tempoEdit.Classes.Remove("invalid");
            AutomationProperties.SetName(_tempoEdit, _shell["Project.Tempo"]);
            AutomationProperties.SetName(tempoApply, _shell["Workflow.ApplyTempo"]);
            _tempoEdit.Focus();
            _tempoEdit.SelectAll();
        };

        async Task CommitTempoAsync()
        {
            bool parsed = decimal.TryParse(_tempoEdit.Text, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out var value) ||
                          decimal.TryParse(_tempoEdit.Text, System.Globalization.NumberStyles.Float, _tempoCulture,
                              out value);
            if (_tempoBasis is not { } basis || !parsed || value is < 1 or > 1000 || decimal.Round(value, 6) != value)
            {
                _tempoEdit.Classes.Add("invalid");
                return;
            }

            _tempoBasis = null;
            PendingTempoCommand = workflow.SetTempoAsync(basis.Lifecycle, basis.Generation, value);
            await PendingTempoCommand;
            tempoFlyout.Hide();
        }

        tempoApply.Click += async (_, _) => await CommitTempoAsync();
        _tempoEdit.KeyDown += async (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                e.Handled = true;
                await CommitTempoAsync();
            }

            if (e.Key == Avalonia.Input.Key.Escape)
            {
                e.Handled = true;
                _tempoBasis = null;
                tempoFlyout.Hide();
            }
        };
        tempoFlyout.Closed += (_, _) => _tempoBasis = null;
        _musicalParameters.Children.Add(new TextBlock
        {
            Text = "BPM", Classes = { "caption" },
            VerticalAlignment = VerticalAlignment.Center
        });
        _musicalParameters.Children.Add(_meter);
        _parameterSlot.Child = _musicalParameters;
        commands.Children.Add(_parameterSlot);
        commands.Children.Add(Separator());
        commands.Children.Add(Command("Undo"));
        commands.Children.Add(Command("Redo"));
        Children.Add(commands);
        _selectionSlot.Child = _occurrences;
        var right = new StackPanel
        {
            Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center,
            Classes = { "parameterRow" }
        };
        right.Children.Add(_selectionSlot);
        right.Children.Add(_gainSlot);
        right.Children.Add(_information);
        right.Children.Add(_overflow);
        Grid.SetColumn(right, 1);
        Children.Add(right);
        _buttons["Play"].Classes.Add("play");
        _buttons["Panic"].Classes.Add("critical");
        _information.Content = HostIcons.Create("Info", _information);
        _overflow.Content = HostIcons.Create("More", _overflow);
        AutomationProperties.SetAutomationId(_information, "Workflow.SourceInfo");
        AutomationProperties.SetAutomationId(_overflow, "Workflow.More");
        AddOverflow("SaveAs");
        AddOverflow("ExportWave", "Export");
        AddOverflow("ExportSong", "Export");
        AddOverflow("Graph");
        AddOverflow("CancelImport", "Close");
        _overflowBody.Children.Add(_overflowParameters);
        _overflowBody.Children.Add(_overflowGain);
        _overflow.Flyout = new Flyout { Content = _overflowBody };
        var body = new StackPanel { Width = 330, Classes = { "spaced" } };
        body.Children.Add(_selectionPopup);
        body.Children.Add(_source);
        body.Children.Add(_chain);
        body.Children.Add(_detail);
        body.Children.Add(_device);
        _technicalExpander.Content = _technical;
        body.Children.Add(_technicalExpander);
        _details = new() { Content = body };
        _information.Click += (_, _) => _details.ShowAt(_information);
        StatusControl.Click += (_, _) => _details.ShowAt(StatusControl);
        StatusControl.Content = _statusText;
        AutomationProperties.SetAutomationId(StatusControl, "Workflow.Status");
        AutomationProperties.SetAutomationId(_source, "Workflow.Source");
        AutomationProperties.SetAutomationId(_device, "Workflow.Device");
        AutomationProperties.SetAutomationId(_technical, "Workflow.Execution");
        AutomationProperties.SetAutomationId(_occurrences, "Workflow.Occurrence");
        AutomationProperties.SetAutomationId(_gain, "Workflow.Gain");
        AutomationProperties.SetAutomationId(_tempo, "Workflow.Tempo");
        AutomationProperties.SetAutomationId(_meter, "Workflow.Meter");
        _occurrences.SelectionChanged += async (_, _) =>
        {
            if (!_refreshing && _occurrences.SelectedItem is OccurrenceChoice choice && choice.Id != _selection)
                await workflow.SelectAsync(choice.Id);
        };
        _gain.GotFocus += (_, _) => BeginGainEdit();
        _gain.LostFocus += async (_, _) =>
        {
            if (!_gain.IsKeyboardFocusWithin) await CommitGainAsync();
        };
        _gain.AddHandler(KeyDownEvent, async (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                e.Handled = true;
                await CommitGainAsync();
            }
            else if (e.Key == Avalonia.Input.Key.Escape)
            {
                e.Handled = true;
                _gainBasis = null;
                Refresh();
                BeginGainEdit();
            }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        SizeChanged += (_, _) => Adapt();
        Refresh();
    }

    private void BeginGainEdit()
    {
        if (_refreshing || _gainBasis is not null) return;
        var state = _workflow.State;
        if (state.Selected is not { } selected || !state.Gain.HasValue) return;
        var attachment = state.Snapshot.State.GraphAttachments.Single(a => a.Id == selected);
        var node = state.Snapshot.State.Graphs.Single(g => g.Id == attachment.GraphId).Nodes
            .Single(n => n.Type == GraphBuiltIns.Gain);
        _gainBasis = (new(state.Document.LifecycleId, state.Generation, selected), node.Id);
    }

    private async Task CommitGainAsync()
    {
        if (_gainBasis is not { } draft) return;
        if (!decimal.TryParse(_gain.Text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value) || value is < 0 or > 1)
        {
            _gain.Classes.Add("invalid");
            return;
        }

        bool refocus = _gain.IsKeyboardFocusWithin;
        _gainBasis = null;
        _gain.Classes.Remove("invalid");
        var current = _workflow.State;
        if (current.Document.LifecycleId == draft.Basis.Lifecycle && current.Generation == draft.Basis.Generation &&
            current.Selected == draft.Basis.Attachment && current.Gain == value)
        {
            Refresh();
            if (_gain.IsKeyboardFocusWithin) BeginGainEdit();
            return;
        }

        PendingGainCommand = _workflow.SetGraphGainAsync(draft.Basis, draft.Node, value);
        await PendingGainCommand;
        Refresh();
        if (refocus && _gain.IsEnabled && _gain.IsEffectivelyVisible)
            _gain.GetVisualDescendants().OfType<TextBox>().SingleOrDefault()?.Focus();
        if (_gain.IsKeyboardFocusWithin) BeginGainEdit();
    }

    private Button Command(string key, string? icon = null)
    {
        var button = new Button { Classes = { "shell", "command" } };
        button.Content = HostIcons.Create(icon ?? key, button);
        AutomationProperties.SetAutomationId(button, "Workflow." + key);
        ToolTip.SetShowOnDisabled(button, true);
        button.Click += async (_, _) =>
        {
            _overflow.Flyout?.Hide();
            await _action(key);
        };
        _buttons.Add(key, button);
        return button;
    }

    private static Border Separator()
    {
        var border = new Border
            { Width = 1, Height = 16, Margin = new(4, 0), VerticalAlignment = VerticalAlignment.Center };
        border.Bind(BackgroundProperty, border.GetResourceObservable("Border.Default"));
        return border;
    }

    private void AddOverflow(string key, string? icon = null)
    {
        var button = Command(key, icon);
        FormatMenuButton(button, key);
        _overflowBody.Children.Add(button);
    }

    private void FormatMenuButton(Button button, string key)
    {
        button.Classes.Remove("command");
        button.Width = double.NaN;
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        var row = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "spaced" } };
        row.Children.Add(HostIcons.Create(
            key == "CancelImport" ? "Close" : key is "ExportWave" or "ExportSong" ? "Export" : key, button, 16));
        var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        _menuLabels.RemoveAll(item => item.Key == key);
        _menuLabels.Add((text, key));
        row.Children.Add(text);
        button.Content = row;
    }

    private void Adapt()
    {
        // Reparent retained controls into explicit overflow, never add another command row.
        bool compact = Bounds.Width < 1050, moveOpen = Bounds.Width < 560;
        if (_compact != compact)
        {
            bool focused = _occurrences.IsKeyboardFocusWithin;
            if (compact)
            {
                _selectionSlot.Child = null;
                _selectionPopup.Children.Add(_occurrences);
            }
            else
            {
                _selectionPopup.Children.Remove(_occurrences);
                _selectionSlot.Child = _occurrences;
            }

            _compact = compact;
            _selectionSlot.IsVisible = !compact;
            if (focused) _information.Focus();
        }

        if (_openInOverflow != moveOpen)
        {
            var button = _buttons["Open"];
            bool focused = button.IsKeyboardFocusWithin;
            if (moveOpen)
            {
                _project.Children.Remove(button);
                FormatMenuButton(button, "Open");
                _overflowBody.Children.Insert(0, button);
            }
            else
            {
                _overflowBody.Children.Remove(button);
                button.Classes.Add("command");
                button.ClearValue(WidthProperty);
                button.Content = HostIcons.Create("Open", button);
                _project.Children.Insert(1, button);
            }

            _openInOverflow = moveOpen;
            if (focused) _overflow.Focus();
        }

        bool gainInOverflow = Bounds.Width < 880;
        if (gainInOverflow != (_overflowGain.Child is not null))
        {
            _gainLevel.Cancel();
            _gainGesture = null;
            if (gainInOverflow)
            {
                _gainSlot.Child = null;
                _overflowGain.Child = _gainGroup;
            }
            else
            {
                _overflowGain.Child = null;
                _gainSlot.Child = _gainGroup;
            }
        }

        _overflowGain.IsVisible = gainInOverflow;
        bool parametersInOverflow = Bounds.Width < 510;
        if (parametersInOverflow != (_overflowParameters.Child is not null))
        {
            bool focused = _musicalParameters.IsKeyboardFocusWithin;
            _tempoBasis = null;
            _tempoAction.Flyout?.Hide();
            if (parametersInOverflow)
            {
                _parameterSlot.Child = null;
                _overflowParameters.Child = _musicalParameters;
            }
            else
            {
                _overflowParameters.Child = null;
                _parameterSlot.Child = _musicalParameters;
            }

            if (focused) _overflow.Focus();
        }

        _overflowParameters.IsVisible = parametersInOverflow;
        Refresh(_blocked);
    }

    internal void Refresh(bool interactionBlocked = false)
    {
        _blocked = interactionBlocked;
        var state = _workflow.State;
        _refreshing = true;
        try
        {
            _tempo.Text = _shell.Tempo;
            _meter.Text = _shell.Meter;
            _mode.Content = _shell[state.SongMode ? "Arrangement.Song" : "Arrangement.Pattern"];
            _mode.Classes.Set("primary", state.SongMode);
            _mode.IsEnabled = !interactionBlocked && !state.Busy && !state.IsClosed;
            AutomationProperties.SetName(_mode, _shell["Arrangement.Mode"] + ": " + _mode.Content);
            ToolTip.SetTip(_mode, _shell["Arrangement.ModeHint"]);
            ToolTip.SetTip(_tempoAction, _shell["Project.Tempo"] + " · " + _shell["Workflow.TempoHint"]);
            AutomationProperties.SetName(_tempoAction, _shell["Project.Tempo"] + ": " + _shell.Tempo + " BPM");
            _tempoAction.IsEnabled = !interactionBlocked && !state.Busy && !state.IsClosed;
            ToolTip.SetTip(_meter, _shell["Project.Meter"] + " · " + _shell["Workflow.ReadOnly"]);
            AutomationProperties.SetName(_tempo, _shell["Project.Tempo"] + ": " + _shell.Tempo + " BPM");
            AutomationProperties.SetName(_meter, _shell["Project.Meter"] + ": " + _shell.Meter);
            foreach (var (key, button) in _buttons)
            {
                bool transport = key is "Stop" or "Panic";
                button.IsVisible = key != "CancelImport" || CanCancelImport;
                button.IsEnabled = key == "CancelImport" ? CanCancelImport :
                    transport ? state.CanStop || state.Busy :
                    !interactionBlocked && !state.Busy && key switch
                    {
                        "Play" => state.CanPlay, "ExportWave" => state.Selected is not null,
                        "ExportSong" => !state.Snapshot.State.ArrangementClips.IsEmpty, "Undo" => state.UndoCount > 0,
                        "Redo" => state.RedoCount > 0, _ => true
                    };
                var shortcut = HostCommands.All.Any(command => command.Id == key)
                    ? HostCommands.Display(_shell.Controls.Binding(key))
                    : WorkflowPresentation.Shortcut(key);
                var label = _shell["Workflow." + key];
                AutomationProperties.SetName(button, label);
                AutomationProperties.SetAcceleratorKey(button, shortcut);
                ToolTip.SetTip(button, label + (shortcut.Length == 0 ? "" : " · " + shortcut) +
                                       (button.IsEnabled
                                           ? ""
                                           : "\n" + _shell[WorkflowPresentation.DisabledReason(key, state)]));
            }

            foreach (var (text, key) in _menuLabels) text.Text = _shell["Workflow." + key];
            _buttons["Play"].Classes.Set("active", state.Execution?.Execution.Playing == true);
            if (!_occurrences.Items.OfType<OccurrenceChoice>().SequenceEqual(state.Occurrences))
                _occurrences.ItemsSource = state.Occurrences;
            _selection = state.Selected;
            _occurrences.SelectedItem = state.Occurrences.SingleOrDefault(item => item.Id == state.Selected);
            _occurrences.PlaceholderText = _shell["Workflow.NoSelection"];
            _occurrences.IsVisible = state.Occurrences.Length > 0;
            _selectionSlot.IsVisible = !_compact && state.Occurrences.Length > 1 && !OccurrenceShownElsewhere;
            _selectionPopup.IsVisible = _compact && state.Occurrences.Length > 0;
            _occurrences.IsEnabled = !interactionBlocked && !state.Busy && state.Occurrences.Length > 0;
            AutomationProperties.SetName(_occurrences, _shell["Workflow.Occurrence"]);
            ToolTip.SetTip(_occurrences,
                state.Occurrences.SingleOrDefault(item => item.Id == state.Selected)?.Label ??
                _shell["Workflow.NoSelection"]);
            _gainLabel.Text = _shell["Workflow.PatternGain"];
            AutomationProperties.SetName(_gain, _shell["Workflow.Gain"]);
            ToolTip.SetTip(_gain, _shell["Workflow.GainUnit"]);
            ToolTip.SetTip(_gainLevel, _shell["Workflow.GainUnit"]);
            AutomationProperties.SetName(_gainLevel, _shell["Workflow.Gain"]);
            _gain.Minimum = Math.Min(0m, state.Gain ?? 0m);
            _gain.Maximum = Math.Max(1m, state.Gain ?? 1m);
            if (_gainBasis is { } gainDraft && (gainDraft.Basis.Lifecycle != state.Document.LifecycleId ||
                                                gainDraft.Basis.Generation != state.Generation ||
                                                gainDraft.Basis.Attachment != state.Selected || interactionBlocked))
                _gainBasis = null;
            if (_gainGesture is { } gesture && (gesture.Basis.Lifecycle != state.Document.LifecycleId ||
                                                gesture.Basis.Generation != state.Generation ||
                                                gesture.Basis.Attachment != state.Selected ||
                                                gesture.Transport != state.TransportGeneration ||
                                                interactionBlocked || GainShownElsewhere || state.IsClosed))
            {
                _gainLevel.Cancel();
                if (_gainGesture is { } stale)
                    PendingGainPreview = _workflow.CancelGainPreviewAsync(stale.Id);
                _gainGesture = null;
            }

            if (_gainBasis is null && _gainGesture is null)
            {
                _gain.Value = state.Gain;
                _gain.Text = state.Gain?.ToString(state.Gain is < 0 or > 1 ? "G29" : GainFormat,
                    System.Globalization.CultureInfo.InvariantCulture);
                _gain.Classes.Remove("invalid");
            }

            _gain.IsEnabled = !interactionBlocked && !state.Busy && state.Gain.HasValue;
            _gainLevel.IsEnabled = _gain.IsEnabled;
            _gainLevel.Amplitude = _gainGesture?.Value ?? state.Gain;
            _gainLevel.InvalidateVisual();
            _gainGroup.IsVisible = state.Gain.HasValue && !GainShownElsewhere;
            _gain.Classes.Set("invalid", state.Gain is < 0 or > 1 && _gainBasis is null && _gainGesture is null ||
                                         _gain.Classes.Contains("invalid"));
            _source.Text = state.Occurrences.SingleOrDefault(item => item.Id == state.Selected)?.Label ??
                           _shell["Workflow.NoSelection"];
            _source.IsVisible = state.Selected is not null && !_compact;
            _chain.Text = state.Chain;
            _chain.IsVisible = _chain.Text.Length > 0;
            var detail = string.Join(" · ", (state.Detail ?? "").Split(" · ").Select(reason =>
                HostLocalizer.English.ContainsKey(reason) ? _shell[reason] : reason));
            _detail.Text = _shell[state.StatusKey] + (detail.Length == 0 ? "" : "\n" + detail);
            _device.Text = state.Device ?? _shell["Workflow.DeviceInactive"];
            _device.IsVisible = state.Selected is not null || state.Device is not null;
            _technicalExpander.Header = _shell["Workflow.Technical"];
            _technical.Text = state.Source + "\n" + (state.Detail ?? "");
            if (state.Execution is { } execution)
                _technical.Text +=
                    $"\nCanonical: {execution.CanonicalRevision}\nExecuting: {execution.Execution.ExecutingRevision}\n" +
                    $"Origin: {execution.Execution.OriginPreparedRevision}\nEquivalent: {execution.Execution.EquivalentCanonicalRevision}\n" +
                    $"Execution: {execution.Execution.PreparedExecutionId}\nAttachment: {execution.Execution.AttachmentId}";
            _technicalExpander.IsVisible = !string.IsNullOrWhiteSpace(_technical.Text);
            var statusKey = WorkflowPresentation.CompactStatus(state);
            var status = _shell[statusKey];
            StatusControl.IsVisible = statusKey != "Status.Ready" || state.Execution?.LastValidPlaying == true;
            if (state.Execution?.LastValidPlaying == true) status += " · " + _shell["Status.LastValid"];
            _statusText.Text = status;
            AutomationProperties.SetName(StatusControl, status);
            ToolTip.SetTip(StatusControl, _detail.Text + "\n" + _shell["Workflow.Details"]);
            StatusControl.Classes.Set("error", WorkflowPresentation.IsError(state));
            StatusControl.Classes.Set("warning", state.StatusKey is "Workflow.Blocked" or "Workflow.Degraded");
            StatusControl.Classes.Set("playing",
                !WorkflowPresentation.IsError(state) && state.Execution?.Execution.Playing == true);
            foreach (var button in new[] { _information, _overflow })
            {
                var label = _shell[button == _information ? "Workflow.Details" : "Workflow.More"];
                AutomationProperties.SetName(button, label);
                ToolTip.SetTip(button, label);
            }

            _information.IsEnabled = _overflow.IsEnabled = !interactionBlocked || state.Busy;
        }
        finally
        {
            _refreshing = false;
        }
    }
}
