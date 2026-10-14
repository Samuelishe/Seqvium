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

/// <summary>Retained song editor; controls submit frozen commands to the single document owner.</summary>
internal sealed partial class ArrangementPaneContent : Grid, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly ArrangementCanvas _canvas;
    private readonly ComboBox _source = new() { MinWidth = 140, MaxWidth = 240 };

    private readonly TextBlock _position = new()
        { Classes = { "numeric" }, VerticalAlignment = VerticalAlignment.Center };

    private readonly TextBlock _hint = new()
        { Classes = { "caption" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    private readonly NumericUpDown _repeats = new()
    {
        Minimum = 1, Maximum = 1024, Increment = 1,
        Width = 64, ShowButtonSpinner = false, FormatString = "0"
    };

    private readonly TextBlock _repeatLabel = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly ToggleButton _follow = new() { IsChecked = true, Classes = { "command" }, Margin = new(3, 0) };
    private readonly ToggleButton _loop = new() { Classes = { "command" }, Margin = new(3, 0) };
    private readonly ToggleButton _select = new() { Classes = { "command" }, Margin = new(3, 0) };
    private readonly ScrollViewer _scroll;
    private readonly ArrangementOverview _overview;
    private readonly ComboBox _snap = new() { Width = 106, Margin = new(6, 0) };
    private readonly ComboBox _trackPicker = new() { MinWidth = 160 };
    private readonly TextBox _trackName = new() { Width = 200 };
    private readonly CheckBox _trackMute = new(), _trackSolo = new();
    private readonly List<Button> _trackColors = [];
    private readonly Flyout _trackFlyout = new();
    private ArrangementEditBasis? _trackBasis;
    private int _trackLane, _trackColorIndex;
    private readonly Dictionary<string, Button> _buttons = [];
    private readonly ToggleButton _slice = new() { Classes = { "command" } };
    private bool _refreshing, _disposed;
    internal Task PendingCommand { get; private set; } = Task.CompletedTask;
    internal ArrangementCanvas Canvas => _canvas;

    internal ArrangementPaneContent(ShellSession shell, DesktopWorkflow workflow, Func<string, Task> action,
        Action openRack)
    {
        _shell = shell;
        _workflow = workflow;
        RowDefinitions = new("Auto,Auto,*,Auto");
        this.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Background"));
        var toolbar = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new(8) };
        Children.Add(toolbar);
        _canvas = new(shell, workflow)
            { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        _scroll = new ScrollViewer
        {
            Content = _canvas,
            HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        _canvas.SelectionChanged += Refresh;
        _canvas.CommandSubmitted += task => PendingCommand = task;
        _canvas.TrackRequested += (lane, command) =>
        {
            if (command == "Edit") OpenTrack(lane);
            else
            {
                var state = workflow.State.Snapshot.State;
                var track = state.ArrangementTracks.SingleOrDefault(track => track.Lane == lane);
                PendingCommand = workflow.ConfigureArrangementTrackAsync(_canvas.Basis, lane,
                    track?.Name ?? shell["Arrangement.Lane"] + " " + (lane + 1), track?.ColorIndex ?? lane % 8,
                    command == "Mute" ? track?.Muted != true : track?.Muted == true,
                    command == "Solo" ? track?.Solo != true : track?.Solo == true);
            }
        };

        async Task OpenRackAsync(Id<GraphAttachment> id)
        {
            await workflow.SelectAsync(id);
            if (!_disposed) openRack();
        }

        _canvas.RackRequested += id => PendingCommand = OpenRackAsync(id);
        _source.SelectionChanged += async (_, _) =>
        {
            if (!_refreshing && _source.SelectedItem is OccurrenceChoice choice)
            {
                PendingCommand = workflow.SelectAsync(choice.Id);
                await PendingCommand;
            }
        };
        toolbar.Children.Add(_source);
        AddButton(toolbar, "ImportAudio", "Import", () => action("ImportAudio"));
        AddButton(toolbar, "CancelAudio", "Close", () =>
        {
            _canvas.CancelAudioImport();
            return Task.CompletedTask;
        });
        AddButton(toolbar, "AudioProperties", "Sound", () =>
        {
            OpenAudioProperties();
            return Task.CompletedTask;
        });
        AddButton(toolbar, "Split", "Split", () => _canvas.SplitSelectedAudio());
        _canvas.AudioPropertiesRequested += OpenAudioProperties;
        _slice.Content = HostIcons.Create("Split", _slice);
        AutomationProperties.SetAutomationId(_slice, "Arrangement.Slice");
        _slice.IsCheckedChanged += (_, _) => _canvas.SliceMode = _slice.IsChecked == true;
        toolbar.Children.Add(_slice);
        AddButton(toolbar, "Play", "Play", async () =>
        {
            await workflow.SetSongModeAsync(true);
            await workflow.PlayAsync();
        });
        AddButton(toolbar, "Stop", "Stop", async () => await workflow.StopAsync());
        AddButton(toolbar, "Start", "Start", async () => await workflow.SeekSongAsync(default));
        AddButton(toolbar, "Duplicate", "Duplicate", () => SelectionCommand(duplicate: true));
        AddButton(toolbar, "Delete", "Delete", () => SelectionCommand(delete: true));
        _select.Content = HostIcons.Create("Selection", _select);
        toolbar.Children.Add(_select);
        _select.IsCheckedChanged += (_, _) => _canvas.SelectMode = _select.IsChecked == true;
        AutomationProperties.SetAutomationId(_select, "Arrangement.Select");
        _loop.Content = HostIcons.Create("Loop", _loop);
        toolbar.Children.Add(_loop);
        _loop.IsCheckedChanged += async (_, _) =>
        {
            if (_refreshing) return;
            var observation = workflow.State;
            PendingCommand = workflow.SetSongLoopAsync(new(observation.LoopStart), new(observation.LoopEnd),
                _loop.IsChecked == true);
            await PendingCommand;
        };
        AutomationProperties.SetAutomationId(_loop, "Arrangement.Loop");
        _snap.SelectionChanged += (_, _) =>
        {
            if (!_refreshing && _snap.SelectedIndex >= 0) _canvas.Snap = (ArrangementSnap)_snap.SelectedIndex;
        };
        toolbar.Children.Add(_snap);
        AddButton(toolbar, "Tracks", "Playlist", () =>
        {
            OpenTrack();
            return Task.CompletedTask;
        });
        BuildTrackEditor();
        var repeatRow = new StackPanel
            { Orientation = Orientation.Horizontal, Classes = { "parameterRow" }, Margin = new(8, 0) };
        repeatRow.Children.Add(_repeatLabel);
        repeatRow.Children.Add(_repeats);
        toolbar.Children.Add(repeatRow);
        _repeats.ValueChanged += async (_, _) =>
        {
            if (!_refreshing && _repeats.Value is { } value && decimal.Truncate(value) == value)
            {
                if (workflow.State.Snapshot.State.AudioClips.SingleOrDefault(c => c.Id == _canvas.SelectedAudio) is
                    { } audio)
                {
                    PendingCommand = workflow.EditAudioClipAsync(_canvas.Basis, audio with { Repeats = (int)value });
                    await PendingCommand;
                    return;
                }

                await SelectedCommand((basis, clip) =>
                    workflow.UpdateArrangementClipAsync(basis, clip.Id, clip.Position, clip.Lane, (int)value));
            }
        };
        AddButton(toolbar, "ZoomOut", "Minimize", () =>
        {
            ZoomAt(false, _scroll.Viewport.Width / 2);
            return Task.CompletedTask;
        });
        AddButton(toolbar, "ZoomIn", "Add", () =>
        {
            ZoomAt(true, _scroll.Viewport.Width / 2);
            return Task.CompletedTask;
        });
        AddButton(toolbar, "Fit", "Fit", () =>
        {
            FitSong();
            return Task.CompletedTask;
        });
        _follow.Content = HostIcons.Create("Follow", _follow, 18);
        toolbar.Children.Add(_follow);
        AutomationProperties.SetAutomationId(_follow, "Arrangement.Follow");
        AddButton(toolbar, "Export", "Export", () => action("ExportSong"));
        toolbar.Children.Add(_position);
        _overview = new(shell, ratio =>
        {
            _follow.IsChecked = false;
            _scroll.Offset =
                new(
                    Math.Max(0,
                        _canvas.HeaderSize + ratio * _canvas.TotalTicks * _canvas.TickPixels -
                        (_scroll.Viewport.Width + _canvas.HeaderSize) / 2), _scroll.Offset.Y);
        });
        _overview.ViewportRequested += (left, right) =>
        {
            _follow.IsChecked = false;
            double total = _canvas.TotalTicks;
            _canvas.ShowRange((right - left) * total, _scroll.Viewport.Width);
            _scroll.Offset = new(Math.Max(0, left * total * _canvas.TickPixels), _scroll.Offset.Y);
        };
        _overview.Margin = new(8, 0, 8, 4);
        SetRow(_overview, 1);
        Children.Add(_overview);
        _scroll.ScrollChanged += (_, _) =>
        {
            _canvas.SetViewportWidth(_scroll.Viewport.Width);
            RefreshOverview();
        };
        _canvas.PanRequested += delta =>
        {
            _follow.IsChecked = false;
            _scroll.Offset = new(Math.Max(0, _scroll.Offset.X - delta.X), Math.Max(0, _scroll.Offset.Y - delta.Y));
        };
        _scroll.AddHandler(PointerWheelChangedEvent, (_, e) =>
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                ZoomAt(e.Delta.Y > 0, e.GetPosition(_scroll).X);
            else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) || Math.Abs(e.Delta.X) > 0)
            {
                _follow.IsChecked = false;
                _scroll.Offset = new(Math.Max(0, _scroll.Offset.X - (e.Delta.X != 0 ? e.Delta.X : e.Delta.Y) * 80),
                    _scroll.Offset.Y);
            }
            else
            {
                _follow.IsChecked = false;
                return;
            }

            e.Handled = true;
        }, RoutingStrategies.Tunnel);
        SetRow(_scroll, 2);
        Children.Add(_scroll);
        _hint.Margin = new(12, 8);
        SetRow(_hint, 3);
        Children.Add(_hint);
        workflow.Changed += WorkflowChanged;
        shell.PropertyChanged += PreferencesChanged;
        Refresh();
    }

    private void AddButton(Panel parent, string key, string icon, Func<Task> action)
    {
        var button = new Button { Classes = { "command" }, Margin = new(3, 0) };
        button.Content = HostIcons.Create(icon, button, 18);
        AutomationProperties.SetAutomationId(button, "Arrangement." + key);
        button.Click += async (_, _) =>
        {
            PendingCommand = action();
            await PendingCommand;
        };
        _buttons.Add(key, button);
        parent.Children.Add(button);
    }

    private Task SelectedCommand(Func<ArrangementEditBasis, ArrangementClip, Task<bool>> command)
    {
        var state = _workflow.State;
        var clip = state.Snapshot.State.ArrangementClips.SingleOrDefault(c => c.Id == _canvas.Selected);
        if (clip is null) return Task.CompletedTask;
        PendingCommand = command(ArrangementEditBasis.From(state), clip);
        return PendingCommand;
    }

    private Task SelectionCommand(bool duplicate = false, bool delete = false)
    {
        PendingCommand = _canvas.AudioSelection.Length > 0
            ? _workflow.EditAudioSelectionAsync(_canvas.Basis, _canvas.AudioSelection, duplicate: duplicate,
                delete: delete)
            : _workflow.EditArrangementSelectionAsync(_canvas.Basis, _canvas.Selection, duplicate: duplicate,
                delete: delete);
        return PendingCommand;
    }

    private void ZoomAt(bool closer, double viewportX)
    {
        _follow.IsChecked = false;
        double anchor = Math.Max(_canvas.HeaderSize, viewportX);
        double tick = (_scroll.Offset.X + anchor - _canvas.HeaderSize) / _canvas.TickPixels;
        _canvas.Zoom(closer);
        _scroll.Offset = new(Math.Max(0, _canvas.HeaderSize + tick * _canvas.TickPixels - anchor), _scroll.Offset.Y);
    }

    private void WorkflowChanged(WorkflowState state) => Dispatcher.UIThread.Post(() =>
    {
        if (!_disposed) Refresh();
    });

    private void PreferencesChanged(object? sender, PropertyChangedEventArgs args) => Refresh();

    internal void Refresh()
    {
        if (_disposed) return;
        _refreshing = true;
        try
        {
            var observation = _workflow.State;
            var state = observation.Snapshot.State;
            _canvas.Adopt(observation);
            var snapLabels = new[]
            {
                _shell["Arrangement.SnapBar"], _shell["Arrangement.SnapBeat"], _shell["Arrangement.SnapSixteenth"],
                _shell["Arrangement.SnapOff"]
            };
            if (!_snap.Items.OfType<string>().SequenceEqual(snapLabels)) _snap.ItemsSource = snapLabels;
            _snap.SelectedIndex = (int)_canvas.Snap;
            AutomationProperties.SetName(_snap, _shell["Arrangement.Snap"]);
            ToolTip.SetTip(_snap, _shell["Arrangement.Snap"]);
            AutomationProperties.SetName(_follow, _shell["Arrangement.Follow"]);
            ToolTip.SetTip(_follow, _shell["Arrangement.Follow"]);
            AutomationProperties.SetName(_select, _shell["Arrangement.Select"]);
            ToolTip.SetTip(_select, _shell["Arrangement.Select"]);
            AutomationProperties.SetName(_loop, _shell["Arrangement.Loop"]);
            ToolTip.SetTip(_loop, _shell["Arrangement.Loop"]);
            _loop.IsChecked = observation.SongLoopEnabled;
            _loop.IsEnabled = !observation.Busy &&
                              observation.LoopEnd - observation.LoopStart >= MusicalPosition.TicksPerQuarter / 4;
            _select.IsEnabled = !observation.Busy;
            _slice.IsEnabled = !observation.Busy;
            ToolTip.SetTip(_slice, _shell["Arrangement.Slice"]);
            AutomationProperties.SetName(_slice, _shell["Arrangement.Slice"]);
            if (_follow.IsChecked == true && observation.SongMode && observation.Execution?.Execution.Playing == true &&
                !_canvas.HasGesture)
            {
                double x = _canvas.PlayheadX;
                if (x < _scroll.Offset.X || x > _scroll.Offset.X + _scroll.Viewport.Width - 24)
                    _scroll.Offset = new(Math.Max(0, x - _scroll.Viewport.Width * .25), _scroll.Offset.Y);
            }

            if (!_source.Items.OfType<OccurrenceChoice>().SequenceEqual(observation.Occurrences))
                _source.ItemsSource = observation.Occurrences;
            _source.SelectedItem = observation.Occurrences.SingleOrDefault(c => c.Id == observation.Selected);
            _source.PlaceholderText = _shell["Arrangement.Source"];
            _source.IsEnabled = !observation.Busy;
            AutomationProperties.SetName(_source, _shell["Arrangement.Source"]);
            var selected = state.ArrangementClips.SingleOrDefault(c => c.Id == _canvas.Selected);
            var audio = state.AudioClips.SingleOrDefault(c => c.Id == _canvas.SelectedAudio);
            _repeatLabel.Text = _shell["Arrangement.Repeats"];
            _repeats.Value = audio?.Repeats ?? selected?.Repeats ?? 1;
            _repeats.IsEnabled =
                (audio is not null && _canvas.AudioSelection.Length == 1 ||
                 selected is not null && _canvas.Selection.Length == 1) && !observation.Busy;
            AutomationProperties.SetAutomationId(_repeats, "Arrangement.Repeats");
            AutomationProperties.SetName(_repeats, _repeatLabel.Text);
            foreach (var (key, button) in _buttons)
            {
                string label = _shell["Arrangement." + key];
                ToolTip.SetTip(button, label);
                AutomationProperties.SetName(button, label);
                button.IsVisible = key != "CancelAudio" || _canvas.IsImportingAudio && observation.Busy;
                button.IsEnabled = key == "CancelAudio"
                    ? _canvas.IsImportingAudio
                    : key == "Stop"
                        ? observation.CanStop || observation.Busy
                        : !observation.Busy && key switch
                        {
                            "Play" => ArrangementPreparation.Refusal(state) is null,
                            "Export" => !state.ArrangementClips.IsEmpty || !state.AudioClips.IsEmpty,
                            "Duplicate" or "Delete" => _canvas.Selection.Length > 0 ||
                                                       _canvas.AudioSelection.Length > 0,
                            "AudioProperties" => audio is not null,
                            "Split" => audio is { Repeats: 1 } && _canvas.PositionTicks > audio.Position.Ticks &&
                                       _canvas.PositionTicks < AudioClipSource.EndTicks(state, audio),
                            _ => true
                        };
            }

            _buttons["Play"].Classes.Set("active",
                observation.SongMode && observation.Execution?.Execution.Playing == true);
            long ticks = _canvas.PositionTicks;
            _position.Text =
                $"  {ticks / state.Settings.Meter.TicksPerBar + 1}:{ticks % state.Settings.Meter.TicksPerBar / state.Settings.Meter.TicksPerBeat + 1}  ";
            _hint.Text =
                _shell[
                    state.ArrangementClips.IsEmpty && state.AudioClips.IsEmpty
                        ? "AudioClip.EmptyHint"
                        : "Arrangement.Hint"];
            _hint.IsVisible = state.ArrangementClips.IsEmpty && state.AudioClips.IsEmpty;
            ToolTip.SetTip(_select, _shell["Arrangement.Select"] + " · " + _shell["Arrangement.Hint"]);
            AutomationProperties.SetHelpText(_canvas, _shell["Arrangement.Hint"]);
            RefreshOverview();
        }
        finally
        {
            _refreshing = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _canvas.CancelAudioImport();
        CancelGesture();
        _workflow.Changed -= WorkflowChanged;
        _shell.PropertyChanged -= PreferencesChanged;
    }

    internal void CancelGesture()
    {
        _canvas.Cancel();
        _overview.Cancel();
        _trackBasis = null;
        _trackFlyout.Hide();
        _audioFlyout?.Hide();
    }

    internal void FocusEditor()
    {
        if (!_disposed && IsEffectivelyVisible) _canvas.Focus();
    }

    private void RefreshOverview() => _overview.Adopt(_workflow.State, _canvas.TotalTicks,
        _scroll.Offset.X / _canvas.TickPixels,
        (_scroll.Offset.X + Math.Max(0, _scroll.Viewport.Width - _canvas.HeaderSize)) / _canvas.TickPixels,
        _canvas.PositionTicks);

    private void FitSong()
    {
        _follow.IsChecked = false;
        _canvas.Fit(_scroll.Viewport.Width);
        _scroll.Offset = default;
    }

    private void BuildTrackEditor()
    {
        var body = new StackPanel { Classes = { "spaced" }, MinWidth = 260 };
        body.Children.Add(_trackPicker);
        body.Children.Add(_trackName);
        var colors = new WrapPanel();
        for (int i = 0; i < 8; i++)
        {
            int index = i;
            var swatch = new Border { Width = 20, Height = 20 };
            swatch.Bind(Border.BackgroundProperty, swatch.GetResourceObservable($"Rack.Channel{i}"));
            swatch.Bind(Border.CornerRadiusProperty, swatch.GetResourceObservable("Control.Corner"));
            var color = new Button { Classes = { "command" }, Margin = new(2), Content = swatch };
            color.Click += (_, _) =>
            {
                _trackColorIndex = index;
                foreach (var button in _trackColors) button.Classes.Set("primary", button == color);
            };
            AutomationProperties.SetAutomationId(color, "Arrangement.Color." + i);
            _trackColors.Add(color);
            colors.Children.Add(color);
        }

        body.Children.Add(colors);
        body.Children.Add(_trackMute);
        body.Children.Add(_trackSolo);
        var apply = new Button { Classes = { "primary" } };
        AutomationProperties.SetAutomationId(apply, "Arrangement.ApplyTrack");
        apply.Click += async (_, _) =>
        {
            if (_trackBasis is not { } basis) return;
            _trackBasis = null;
            PendingCommand = _workflow.ConfigureArrangementTrackAsync(basis, _trackLane, _trackName.Text ?? "",
                _trackColorIndex, _trackMute.IsChecked == true, _trackSolo.IsChecked == true);
            await PendingCommand;
            _trackFlyout.Hide();
        };
        body.Children.Add(apply);
        _trackFlyout.Content = body;
        _trackFlyout.Closed += (_, _) => _trackBasis = null;
        _trackFlyout.Opened += (_, _) =>
        {
            _trackMute.Content = _shell["Arrangement.Mute"];
            _trackSolo.Content = _shell["Arrangement.Solo"];
            apply.Content = _shell["Arrangement.Apply"];
            AutomationProperties.SetName(_trackName, _shell["Arrangement.TrackName"]);
            AutomationProperties.SetName(_trackPicker, _shell["Arrangement.Tracks"]);
            for (int i = 0; i < _trackColors.Count; i++)
                AutomationProperties.SetName(_trackColors[i], _shell["Arrangement.Color"] + " " + (i + 1));
        };
        _trackPicker.SelectionChanged += (_, _) =>
        {
            if (_trackPicker.SelectedIndex >= 0) LoadTrackDraft(_trackPicker.SelectedIndex);
        };
    }

    private void OpenTrack(int? lane = null)
    {
        var state = _workflow.State;
        var selected = state.Snapshot.State.ArrangementClips.SingleOrDefault(clip => clip.Id == _canvas.Selected);
        _trackPicker.ItemsSource = Enumerable.Range(1, 32).Select(i => _shell["Arrangement.Lane"] + " " + i).ToArray();
        _trackPicker.SelectedIndex = lane ?? selected?.Lane ?? 0;
        LoadTrackDraft(_trackPicker.SelectedIndex);
        _trackFlyout.ShowAt(_buttons["Tracks"]);
    }

    private void LoadTrackDraft(int lane)
    {
        var state = _workflow.State;
        var track = state.Snapshot.State.ArrangementTracks.SingleOrDefault(track => track.Lane == lane);
        _trackBasis = ArrangementEditBasis.From(state);
        _trackLane = lane;
        _trackName.Text = track?.Name ?? _shell["Arrangement.Lane"] + " " + (lane + 1);
        _trackColorIndex = track?.ColorIndex ?? lane % 8;
        _trackMute.IsChecked = track?.Muted == true;
        _trackSolo.IsChecked = track?.Solo == true;
        for (int i = 0; i < _trackColors.Count; i++) _trackColors[i].Classes.Set("primary", i == _trackColorIndex);
    }
}
