// SPDX-License-Identifier: Apache-2.0
using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed partial class ArrangementPaneContent
{
    private sealed record SectionChoice(Id<ArrangementSection> Id, string Label)
    { public override string ToString() => Label; }
    private readonly ComboBox _sections = new() { MinWidth = 170, MaxWidth = 230 };
    private readonly TextBlock _selectionInfo = new() { Classes = { "caption" }, VerticalAlignment = VerticalAlignment.Center };
    private readonly List<(string Key, Button Button)> _structureButtons = [];
    private readonly Flyout _sectionFlyout = new(), _clipFlyout = new();
    private Control? _sectionContent, _clipContent;
    private ArrangementEditBasis? _sectionBasis, _clipBasis;
    private PlaylistBoundaryConflict? _boundaryConflict;
    private ArrangementEditBasis? _conflictBasis;
    private readonly TextBlock _structureFeedback = new() { Classes = { "warning" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly Button _showConflict = new() { Classes = { "compact" } };
    private readonly StackPanel _feedbackRow = new() { IsVisible = false, Classes = { "tight" } };
    internal Control? SectionPropertiesContent => _sectionContent;
    internal Control? ClipPropertiesContent => _clipContent;

    private void BuildSongStructure()
    {
        _sectionFlyout.Closed += (_, _) => _sectionBasis = null;
        _clipFlyout.Closed += (_, _) => _clipBasis = null;
        var row = new WrapPanel { Margin = new(8, 0, 8, 4) };
        var structure = new StackPanel { Classes = { "tight" } };
        SetRow(structure, 1); Children.Add(structure); structure.Children.Add(row);
        _feedbackRow.Margin = new(8, 0, 8, 4);
        _feedbackRow.Children.Add(_structureFeedback); _feedbackRow.Children.Add(_showConflict);
        structure.Children.Add(_feedbackRow);
        AutomationProperties.SetAutomationId(_showConflict, "Arrangement.ShowConflict");
        _showConflict.Click += (_, _) =>
        {
            if (_boundaryConflict is not { } conflict || _conflictBasis != _canvas.Basis) return;
            _canvas.SelectBoundaryConflict(conflict);
            _follow.IsChecked = false;
            _scroll.Offset = new(Math.Max(0, conflict.Position * _canvas.TickPixels - 30),
                Math.Max(0, conflict.Lane * Seqvium.Desktop.Presentation.HostVisual.Metric(_canvas, "Playlist.LaneHeight") - 30));
        };
        AutomationProperties.SetAutomationId(_sections, "Arrangement.Sections");
        row.Children.Add(_sections);
        _sections.SelectionChanged += async (_, _) =>
        {
            if (_refreshing || _sections.SelectedItem is not SectionChoice choice) return;
            var s = _workflow.State.Snapshot.State.ArrangementSections.Single(s => s.Id == choice.Id);
            _canvas.SelectedSection = choice.Id;
            PendingCommand = Navigate(s);
            await PendingCommand;
        };
        async Task Navigate(ArrangementSection s)
        {
            await _workflow.SetSongLoopAsync(s.Position, new(s.Position.Ticks + s.Length.Ticks), false);
            await _workflow.SeekSongAsync(s.Position);
            _follow.IsChecked = false;
            _scroll.Offset = new(Math.Max(0, s.Position.Ticks * _canvas.TickPixels), _scroll.Offset.Y);
        }
        Add("SectionAdd", () => { OpenSection(null); return Task.CompletedTask; });
        Add("SectionEdit", () => { OpenSection(SectionId()); return Task.CompletedTask; });
        Add("SectionEarlier", () => SectionAction("Earlier"));
        Add("SectionLater", () => SectionAction("Later"));
        Add("SectionDuplicate", () => SectionAction("Duplicate"));
        Add("Properties", () => { OpenClipProperties(); return Task.CompletedTask; });
        Add("Variation", _canvas.MakeVariation);
        row.Children.Add(_selectionInfo);
        _canvas.SectionPropertiesRequested += OpenSection;
        _canvas.ClipPropertiesRequested += OpenClipProperties;
        _canvas.SectionCommandRequested = RunSectionAction;

        void Add(string key, Func<Task> action)
        {
            var button = new Button { Classes = { "compact" }, Margin = new(3, 0) };
            string? icon = key switch
            {
                "SectionAdd" => "Add", "SectionEdit" => "Draw", "SectionEarlier" => "Left",
                "SectionLater" => "Right", "SectionDuplicate" => "Duplicate", "Properties" => "Inspector", _ => null
            };
            if (icon is not null)
            {
                button.Classes.Remove("compact"); button.Classes.Add("command");
                button.Content = HostIcons.Create(icon, button);
            }
            AutomationProperties.SetAutomationId(button, "Arrangement." + key);
            button.Click += async (_, _) => { PendingCommand = action(); await PendingCommand; };
            row.Children.Add(button); _structureButtons.Add((key, button));
        }
    }

    private Id<ArrangementSection>? SectionId() => _canvas.SelectedSection ?? (_sections.SelectedItem as SectionChoice)?.Id;
    private Task SectionAction(string action) => SectionId() is { } id ? RunSectionAction(id, action) : Task.CompletedTask;

    private async Task RunSectionAction(Id<ArrangementSection> id, string action)
    {
        var state = _workflow.State.Snapshot.State;
        var ordered = state.ArrangementSections.OrderBy(s => s.Position.Ticks).ToArray();
        int index = Array.FindIndex(ordered, s => s.Id == id);
        if (index < 0) return;
        var section = ordered[index];
        long[] boundaries = [section.Position.Ticks, section.Position.Ticks + section.Length.Ticks];
        int adjacent = index + (action == "Later" ? 1 : -1);
        if (action != "Duplicate" && adjacent >= 0 && adjacent < ordered.Length)
            boundaries = [.. boundaries, ordered[adjacent].Position.Ticks, ordered[adjacent].Position.Ticks + ordered[adjacent].Length.Ticks];
        _boundaryConflict = PlaylistEditing.FindBoundaryConflict(state, boundaries);
        _conflictBasis = _canvas.Basis;
        bool accepted = await _workflow.SectionCommandAsync(_canvas.Basis, id, action);
        if (!accepted && _boundaryConflict is { } conflict && _conflictBasis == _canvas.Basis)
        {
            _structureFeedback.Text = _shell["Arrangement.CrossingSection"] + "\n" + conflict.Name + " · " +
                _shell["Arrangement.Lane"] + " " + (conflict.Lane + 1) + " · " + _shell["Arrangement.StartBar"] + " " +
                ((decimal)conflict.Position / state.Settings.Meter.TicksPerBar + 1).ToString("0.###", CultureInfo.InvariantCulture);
            _feedbackRow.IsVisible = true;
            _showConflict.Content = _shell["Arrangement.ShowConflict"];
        }
        else _feedbackRow.IsVisible = false;
    }

    private void RefreshSongStructure()
    {
        var state = _workflow.State.Snapshot.State;
        if (_conflictBasis != _canvas.Basis) { _boundaryConflict = null; _feedbackRow.IsVisible = false; }
        var selected = SectionId();
        var choices = state.ArrangementSections.OrderBy(s => s.Position.Ticks)
            .Select(s => new SectionChoice(s.Id, $"{s.Position.Ticks / state.Settings.Meter.TicksPerBar + 1:00}  {s.Name}")).ToArray();
        if (!_sections.Items.OfType<SectionChoice>().SequenceEqual(choices)) _sections.ItemsSource = choices;
        _sections.SelectedItem = choices.FirstOrDefault(s => s.Id == selected);
        _sections.PlaceholderText = _shell["Arrangement.Sections"];
        _sections.IsEnabled = !_workflow.State.Busy;
        AutomationProperties.SetName(_sections, _shell["Arrangement.Sections"]);
        foreach (var (key, button) in _structureButtons)
        {
            string label = _shell["Arrangement." + key];
            if (key == "Variation") button.Content = label;
            AutomationProperties.SetName(button, label);
            ToolTip.SetTip(button, label);
            button.IsEnabled = !_workflow.State.Busy && key switch
            {
                "SectionEdit" or "SectionDuplicate" => selected is not null && choices.Any(s => s.Id == selected),
                "SectionEarlier" => choices.Length > 0 && Array.FindIndex(choices, s => s.Id == selected) > 0,
                "SectionLater" => selected is not null && Array.FindIndex(choices, s => s.Id == selected) is var index && index >= 0 && index < choices.Length - 1,
                "Variation" => _canvas.Selection.Length == 1 && _canvas.AudioSelection.Length == 0,
                "Properties" => _canvas.SelectedAutomation is not null ||
                                _canvas.Selection.Length + _canvas.AudioSelection.Length > 0,
                _ => true
            };
        }
        int count = _canvas.Selection.Length + _canvas.AudioSelection.Length;
        var clip = state.ArrangementClips.SingleOrDefault(c => c.Id == _canvas.Selected);
        var pattern = clip is null ? null : state.Patterns.Single(p => p.Id == state.Placements.Single(p => p.Id == clip.SourcePlacementId).PatternId);
        _selectionInfo.Text = count == 1 && clip is not null
            ? pattern?.VariationOf is { } origin ? "  ↗ " + _shell["Arrangement.VariationFrom"] + " " +
                (state.Patterns.SingleOrDefault(p => p.Id == origin)?.Name ?? _shell["Arrangement.SourceUnavailable"])
            : "  ≋ " + _shell["Arrangement.Shared"] + " " + state.ArrangementClips.Count(c =>
                state.Placements.Single(p => p.Id == c.SourcePlacementId).PatternId ==
                state.Placements.Single(p => p.Id == clip.SourcePlacementId).PatternId)
            : count > 0 ? "  " + _shell["Arrangement.SelectedClips"] + " " + count : "";
        if (_sectionBasis is { } sb && sb != _canvas.Basis) { _sectionBasis = null; _sectionFlyout.Hide(); }
        if (_clipBasis is { } cb && cb != _canvas.Basis) { _clipBasis = null; _clipFlyout.Hide(); }
    }

    private StackPanel Form() => new() { MinWidth = 300, MaxWidth = 360, Classes = { "spaced" } };
    private TextBox Field(StackPanel body, string key, string value, bool numeric = true)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "parameterRow" } };
        var label = new TextBlock { Text = _shell["Arrangement." + key], Width = 120, VerticalAlignment = VerticalAlignment.Center };
        var field = new TextBox { Text = value, Width = 170 };
        if (numeric) field.Classes.Add("numeric");
        AutomationProperties.SetAutomationId(field, "Arrangement.Field." + key);
        AutomationProperties.SetName(field, label.Text);
        row.Children.Add(label); row.Children.Add(field); body.Children.Add(row);
        return field;
    }

    private void OpenSection(Id<ArrangementSection>? id)
    {
        var state = _workflow.State.Snapshot.State;
        var section = state.ArrangementSections.SingleOrDefault(s => s.Id == id);
        long bar = state.Settings.Meter.TicksPerBar;
        long start = section?.Position.Ticks ?? _workflow.State.LoopStart;
        long length = section?.Length.Ticks ?? Math.Max(bar, _workflow.State.LoopEnd - start);
        var basis = _canvas.Basis; _sectionBasis = basis;
        var body = Form(); _sectionContent = body;
        var name = Field(body, "SectionName", section?.Name ?? _shell["Arrangement.SectionDefault"], false);
        var position = Field(body, "StartBar", ((decimal)start / bar + 1).ToString(CultureInfo.InvariantCulture));
        var duration = Field(body, "LengthBars", ((decimal)length / bar).ToString(CultureInfo.InvariantCulture));
        var color = new ComboBox { SelectedIndex = section?.ColorIndex ?? state.ArrangementSections.Length % 8 };
        var colorChoices = new List<ComboBoxItem>();
        for (int i = 0; i < 8; i++)
        {
            var swatch = new Border { Width = 16, Height = 16 };
            swatch.Bind(Border.BackgroundProperty, swatch.GetResourceObservable($"Rack.Channel{i}"));
            swatch.Bind(Border.CornerRadiusProperty, swatch.GetResourceObservable("Control.Corner"));
            var choice = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "parameterRow" } };
            choice.Children.Add(swatch);
            choice.Children.Add(new TextBlock { Text = _shell["Arrangement.SectionColor"] + " " + (i + 1), VerticalAlignment = VerticalAlignment.Center });
            colorChoices.Add(new ComboBoxItem { Content = choice });
        }
        color.ItemsSource = colorChoices;
        color.SelectedIndex = section?.ColorIndex ?? state.ArrangementSections.Length % 8;
        AutomationProperties.SetAutomationId(color, "Arrangement.SectionColor"); body.Children.Add(color);
        var error = new TextBlock { IsVisible = false, Classes = { "warning" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 300 };
        body.Children.Add(error);
        var apply = new Button { Content = _shell["Arrangement.ApplyEdit"], Classes = { "primary" } };
        AutomationProperties.SetAutomationId(apply, "Arrangement.ApplySection"); body.Children.Add(apply);
        apply.Click += async (_, _) =>
        {
            if (_sectionBasis != basis) return;
            if (!Number(position, out var p) || !Number(duration, out var d) || p < 1 || d <= 0 || p > 1000000 || d > 1000000 || string.IsNullOrWhiteSpace(name.Text))
            { error.IsVisible = true; error.Text = _shell["Arrangement.InvalidRange"]; return; }
            if (section is not null && name.Text.Trim() == section.Name && (long)decimal.Round((p - 1) * bar) == start &&
                (long)decimal.Round(d * bar) == length && color.SelectedIndex == section.ColorIndex)
            { _sectionFlyout.Hide(); return; }
            PendingCommand = _workflow.EditSectionAsync(basis, id, name.Text.Trim(),
                (long)decimal.Round((p - 1) * bar), (long)decimal.Round(d * bar), color.SelectedIndex);
            await PendingCommand;
            if (_workflow.State.Generation != basis.Generation) _sectionFlyout.Hide();
            else { error.IsVisible = true; error.Text = _shell["Arrangement.SectionOverlap"]; }
        };
        if (section is not null)
        {
            var actions = new WrapPanel(); body.Children.Add(actions);
            foreach (string command in new[] { "Select", "Loop", "Delete" })
            {
                var button = new Button { Content = _shell["Arrangement.Section" + command], Classes = { "compact" } };
                AutomationProperties.SetAutomationId(button, "Arrangement.Section" + command);
                button.Click += async (_, _) =>
                {
                    if (_sectionBasis != basis) return;
                    _sectionFlyout.Hide();
                    if (command == "Select") _canvas.SelectSectionClips(section.Id);
                    else if (command == "Loop")
                        await _workflow.SetSongLoopAsync(section.Position, new(section.Position.Ticks + section.Length.Ticks), true);
                    else { PendingCommand = _workflow.SectionCommandAsync(basis, section.Id, command); await PendingCommand; }
                };
                actions.Children.Add(button);
            }
        }
        _sectionFlyout.Content = body;
        _sectionFlyout.ShowAt(_structureButtons[0].Button);
    }

    private void OpenClipProperties()
    {
        if (_canvas.SelectedAutomation is not null)
        {
            OpenAutomation();
            return;
        }

        var state = _workflow.State.Snapshot.State;
        var patternIds = _canvas.Selection; var audioIds = _canvas.AudioSelection;
        var starts = state.ArrangementClips.Where(c => patternIds.Contains(c.Id)).Select(c => c.Position.Ticks)
            .Concat(state.AudioClips.Where(c => audioIds.Contains(c.Id)).Select(c => c.Position.Ticks)).ToArray();
        var lanes = state.ArrangementClips.Where(c => patternIds.Contains(c.Id)).Select(c => c.Lane)
            .Concat(state.AudioClips.Where(c => audioIds.Contains(c.Id)).Select(c => c.Lane)).ToArray();
        if (starts.Length == 0) return;
        var basis = _canvas.Basis; _clipBasis = basis;
        var body = Form(); _clipContent = body;
        long bar = state.Settings.Meter.TicksPerBar;
        var position = Field(body, "StartBar", ((decimal)starts.Min() / bar + 1).ToString(CultureInfo.InvariantCulture));
        var lane = Field(body, "Lane", (lanes.Min() + 1).ToString(CultureInfo.InvariantCulture));
        var error = new TextBlock { IsVisible = false, Classes = { "warning" } }; body.Children.Add(error);
        var apply = new Button { Content = _shell["Arrangement.ApplyEdit"], Classes = { "primary" } };
        AutomationProperties.SetAutomationId(apply, "Arrangement.ApplyProperties"); body.Children.Add(apply);
        apply.Click += async (_, _) =>
        {
            if (_clipBasis != basis) return;
            if (!Number(position, out var p) || !Number(lane, out var l) || p < 1 || p > 1000000 ||
                l < 1 || l != decimal.Truncate(l) || l - 1 + lanes.Max() - lanes.Min() >= 32)
            { error.IsVisible = true; error.Text = _shell["Arrangement.InvalidRange"]; return; }
            PendingCommand = _workflow.EditPlaylistSelectionAsync(basis, patternIds, audioIds,
                (long)decimal.Round((p - 1) * bar) - starts.Min(), (int)l - 1 - lanes.Min());
            await PendingCommand; _clipFlyout.Hide();
        };
        _clipFlyout.Content = body; _clipFlyout.ShowAt(_structureButtons.Single(b => b.Key == "Properties").Button);
    }

    private static bool Number(TextBox field, out decimal value) => decimal.TryParse(field.Text,
        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
}
