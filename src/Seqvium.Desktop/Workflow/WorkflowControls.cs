// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workflow;

/// <summary>One retained command row in the R3 frame. Diagnostics require an explicit details action.</summary>
internal sealed class WorkflowControls : Grid
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly Func<string, Task> _action;
    private readonly Dictionary<string, Button> _buttons = [];
    private readonly List<(TextBlock Text, string Key)> _menuLabels = [];
    private readonly StackPanel _project = new() { Orientation = Orientation.Horizontal };
    private readonly StackPanel _overflowBody = new() { Spacing = 2, MinWidth = 180 };
    private readonly StackPanel _selectionPopup = new() { Spacing = 6 };
    private readonly StackPanel _musicalParameters = new() { Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center, Spacing = 4, Margin = new(4, 0) };
    private readonly TextBlock _tempo = new() { FontWeight = FontWeight.SemiBold };
    private readonly TextBlock _meter = new() { FontWeight = FontWeight.SemiBold, Margin = new(8, 0, 4, 0) };
    private readonly TextBlock _overflowParameters = new() { Margin = new(6), Classes = { "secondary" } };
    private readonly Border _selectionSlot = new() { Margin = new(4, 0), Width = 220,
        VerticalAlignment = VerticalAlignment.Center };
    private readonly ComboBox _occurrences = new() { Classes = { "workflow" }, MinWidth = 100,
        HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly NumericUpDown _gain = new() { Classes = { "workflow" }, Minimum = 0, Maximum = 1,
        Increment = 0.05m, FormatString = "0.00", Width = 64, ShowButtonSpinner = false,
        NumberFormat = System.Globalization.CultureInfo.InvariantCulture.NumberFormat };
    private readonly TextBlock _gainLabel = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new(4, 0, 4, 0) };
    private readonly Button _information = new() { Classes = { "shell", "command" } };
    private readonly Button _overflow = new() { Classes = { "shell", "command" } };
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
    internal bool CanCancelImport { get; set; }
    internal Button StatusControl { get; } = new() { Classes = { "shell", "status" },
        HorizontalAlignment = HorizontalAlignment.Stretch };

    internal WorkflowControls(ShellSession shell, DesktopWorkflow workflow, Func<string, Task> action)
    {
        _shell = shell; _workflow = workflow; _action = action;
        Height = 30; ColumnDefinitions = new("Auto,*,Auto");
        var commands = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (var key in new[] { "Import", "Open", "Save" }) _project.Children.Add(Command(key));
        commands.Children.Add(_project); commands.Children.Add(Separator());
        foreach (var key in new[] { "Play", "Stop", "Panic" }) commands.Children.Add(Command(key));
        _musicalParameters.Children.Add(_tempo);
        _musicalParameters.Children.Add(new TextBlock { Text = "BPM", Classes = { "caption" },
            VerticalAlignment = VerticalAlignment.Center });
        _musicalParameters.Children.Add(_meter);
        commands.Children.Add(_musicalParameters);
        commands.Children.Add(Separator());
        commands.Children.Add(Command("Undo")); commands.Children.Add(Command("Redo")); commands.Children.Add(Separator());
        Children.Add(commands);
        _selectionSlot.Child = _occurrences;
        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(_selectionSlot);
        right.Children.Add(_gainLabel); right.Children.Add(_gain); right.Children.Add(_information); right.Children.Add(_overflow);
        Grid.SetColumn(right, 1); Children.Add(right);
        _buttons["Play"].Classes.Add("play"); _buttons["Panic"].Classes.Add("critical");
        _information.Content = HostIcons.Create("Info", _information);
        _overflow.Content = HostIcons.Create("More", _overflow);
        AutomationProperties.SetAutomationId(_information, "Workflow.SourceInfo");
        AutomationProperties.SetAutomationId(_overflow, "Workflow.More");
        AddOverflow("SaveAs"); AddOverflow("CancelImport", "Close");
        _overflowBody.Children.Add(_overflowParameters);
        _overflow.Flyout = new Flyout { Content = _overflowBody };
        var body = new StackPanel { Width = 330, Spacing = 8 };
        body.Children.Add(_selectionPopup); body.Children.Add(_source); body.Children.Add(_chain);
        body.Children.Add(_detail); body.Children.Add(_device);
        _technicalExpander.Content = _technical; body.Children.Add(_technicalExpander);
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
        _gain.ValueChanged += async (_, _) =>
        { if (!_refreshing && _gain.Value is { } gain) await workflow.SetGainAsync(gain); };
        SizeChanged += (_, _) => Adapt();
        Refresh();
    }

    private Button Command(string key, string? icon = null)
    {
        var button = new Button { Classes = { "shell", "command" } };
        button.Content = HostIcons.Create(icon ?? key, button);
        AutomationProperties.SetAutomationId(button, "Workflow." + key);
        ToolTip.SetShowOnDisabled(button, true);
        button.Click += async (_, _) => { _overflow.Flyout?.Hide(); await _action(key); };
        _buttons.Add(key, button); return button;
    }

    private static Border Separator()
    {
        var border = new Border { Width = 1, Height = 16, Margin = new(4, 0), VerticalAlignment = VerticalAlignment.Center };
        border.Bind(BackgroundProperty, border.GetResourceObservable("Border.Default")); return border;
    }

    private void AddOverflow(string key, string? icon = null)
    {
        var button = Command(key, icon); FormatMenuButton(button, key); _overflowBody.Children.Add(button);
    }

    private void FormatMenuButton(Button button, string key)
    {
        button.Classes.Remove("command"); button.Width = double.NaN;
        button.HorizontalAlignment = HorizontalAlignment.Stretch; button.HorizontalContentAlignment = HorizontalAlignment.Left;
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(HostIcons.Create(key == "CancelImport" ? "Close" : key, button, 16));
        var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        _menuLabels.RemoveAll(item => item.Key == key);
        _menuLabels.Add((text, key)); row.Children.Add(text); button.Content = row;
    }

    private void Adapt()
    {
        // Reparent retained controls into explicit overflow, never add another command row.
        bool compact = Bounds.Width < 600, moveOpen = Bounds.Width < 470;
        if (_compact != compact)
        {
            bool focused = _occurrences.IsKeyboardFocusWithin;
            if (compact) { _selectionSlot.Child = null; _selectionPopup.Children.Add(_occurrences); }
            else { _selectionPopup.Children.Remove(_occurrences); _selectionSlot.Child = _occurrences; }
            _compact = compact; _selectionSlot.IsVisible = !compact;
            if (focused) _information.Focus();
        }
        if (_openInOverflow != moveOpen)
        {
            var button = _buttons["Open"]; bool focused = button.IsKeyboardFocusWithin;
            if (moveOpen) { _project.Children.Remove(button); FormatMenuButton(button, "Open"); _overflowBody.Children.Insert(0, button); }
            else
            {
                _overflowBody.Children.Remove(button); button.Classes.Add("command"); button.Width = 26;
                button.Content = HostIcons.Create("Open", button); _project.Children.Insert(1, button);
            }
            _openInOverflow = moveOpen;
            if (focused) _overflow.Focus();
        }
        _gainLabel.IsVisible = Bounds.Width >= 440;
        _musicalParameters.IsVisible = Bounds.Width >= 770;
        _overflowParameters.IsVisible = !_musicalParameters.IsVisible;
        Refresh(_blocked);
    }

    internal void Refresh(bool interactionBlocked = false)
    {
        _blocked = interactionBlocked;
        var state = _workflow.State; _refreshing = true;
        try
        {
            _tempo.Text = _shell.Tempo; _meter.Text = _shell.Meter;
            _overflowParameters.Text = _shell.Tempo + " BPM · " + _shell.Meter;
            ToolTip.SetTip(_tempo, _shell["Project.Tempo"] + " · " + _shell["Workflow.ReadOnly"]);
            ToolTip.SetTip(_meter, _shell["Project.Meter"] + " · " + _shell["Workflow.ReadOnly"]);
            AutomationProperties.SetName(_tempo, _shell["Project.Tempo"] + ": " + _shell.Tempo + " BPM");
            AutomationProperties.SetName(_meter, _shell["Project.Meter"] + ": " + _shell.Meter);
            foreach (var (key, button) in _buttons)
            {
                bool transport = key is "Stop" or "Panic";
                button.IsVisible = key != "CancelImport" || CanCancelImport;
                button.IsEnabled = key == "CancelImport" ? CanCancelImport : transport ? state.CanStop || state.Busy :
                    !interactionBlocked && !state.Busy && key switch
                    { "Play" => state.CanPlay, "Undo" => state.UndoCount > 0, "Redo" => state.RedoCount > 0, _ => true };
                var shortcut = WorkflowPresentation.Shortcut(key); var label = _shell["Workflow." + key];
                AutomationProperties.SetName(button, label); AutomationProperties.SetAcceleratorKey(button, shortcut);
                ToolTip.SetTip(button, label + (shortcut.Length == 0 ? "" : " · " + shortcut) +
                    (button.IsEnabled ? "" : "\n" + _shell[WorkflowPresentation.DisabledReason(key, state)]));
            }
            foreach (var (text, key) in _menuLabels) text.Text = _shell["Workflow." + key];
            _buttons["Play"].Classes.Set("active", state.Execution?.Execution.Playing == true);
            if (!_occurrences.Items.OfType<OccurrenceChoice>().SequenceEqual(state.Occurrences)) _occurrences.ItemsSource = state.Occurrences;
            _selection = state.Selected; _occurrences.SelectedItem = state.Occurrences.SingleOrDefault(item => item.Id == state.Selected);
            _occurrences.PlaceholderText = _shell["Workflow.NoSelection"];
            _occurrences.IsEnabled = !interactionBlocked && !state.Busy && state.Occurrences.Length > 0;
            AutomationProperties.SetName(_occurrences, _shell["Workflow.Occurrence"]);
            ToolTip.SetTip(_occurrences, state.Occurrences.SingleOrDefault(item => item.Id == state.Selected)?.Label ?? _shell["Workflow.NoSelection"]);
            _gainLabel.Text = "Gain"; AutomationProperties.SetName(_gain, _shell["Workflow.Gain"]);
            ToolTip.SetTip(_gain, _shell["Workflow.GainUnit"]);
            _gain.Minimum = Math.Min(0m, state.Gain ?? 0m); _gain.Maximum = Math.Max(1m, state.Gain ?? 1m);
            _gain.Value = state.Gain; _gain.IsEnabled = !interactionBlocked && !state.Busy && state.Gain.HasValue;
            _source.Text = state.Occurrences.SingleOrDefault(item => item.Id == state.Selected)?.Label ?? _shell["Workflow.NoSelection"];
            _chain.Text = state.Chain;
            var detail = string.Join(" · ", (state.Detail ?? "").Split(" · ").Select(reason =>
                HostLocalizer.English.ContainsKey(reason) ? _shell[reason] : reason));
            _detail.Text = _shell[state.StatusKey] + (detail.Length == 0 ? "" : "\n" + detail);
            _device.Text = state.Device ?? _shell["Workflow.DeviceInactive"];
            _technicalExpander.Header = _shell["Workflow.Technical"];
            _technical.Text = state.Source + "\n" + (state.Detail ?? "");
            if (state.Execution is { } execution)
                _technical.Text += $"\nCanonical: {execution.CanonicalRevision}\nExecuting: {execution.Execution.ExecutingRevision}\n" +
                    $"Origin: {execution.Execution.OriginPreparedRevision}\nEquivalent: {execution.Execution.EquivalentCanonicalRevision}\n" +
                    $"Execution: {execution.Execution.PreparedExecutionId}\nAttachment: {execution.Execution.AttachmentId}";
            var status = _shell[WorkflowPresentation.CompactStatus(state)];
            if (state.Execution?.LastValidPlaying == true) status += " · " + _shell["Status.LastValid"];
            _statusText.Text = status; AutomationProperties.SetName(StatusControl, status);
            ToolTip.SetTip(StatusControl, _detail.Text + "\n" + _shell["Workflow.Details"]);
            StatusControl.Classes.Set("error", WorkflowPresentation.IsError(state));
            StatusControl.Classes.Set("warning", state.StatusKey is "Workflow.Blocked" or "Workflow.Degraded");
            StatusControl.Classes.Set("playing", !WorkflowPresentation.IsError(state) && state.Execution?.Execution.Playing == true);
            foreach (var button in new[] { _information, _overflow })
            {
                var label = _shell[button == _information ? "Workflow.Details" : "Workflow.More"];
                AutomationProperties.SetName(button, label); ToolTip.SetTip(button, label);
            }
            _information.IsEnabled = _overflow.IsEnabled = !interactionBlocked || state.Busy;
        }
        finally { _refreshing = false; }
    }
}
