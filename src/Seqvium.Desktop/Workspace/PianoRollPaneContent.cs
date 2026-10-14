// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Retained shared Pattern/Part editor. Fields and gestures submit to the existing workflow owner.</summary>
internal sealed class PianoRollPaneContent : Grid, IDisposable
{
    private sealed record PartChoice(Id<MusicalPart> Id, string Name)
    {
        public override string ToString() => Name;
    }

    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly ComboBox _pattern = new() { MinWidth = 110, MaxWidth = 230 };
    private readonly ComboBox _part = new() { MinWidth = 110, MaxWidth = 230 };
    private readonly ComboBox _snap = new() { Width = 90 };
    private readonly ComboBox _length = new() { Width = 90 };
    private readonly TextBlock _scope = new() { Classes = { "caption" }, VerticalAlignment = VerticalAlignment.Center };

    private readonly TextBlock _feedback = new()
        { Classes = { "caption" }, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };

    private readonly TextBlock _warning = new()
        { Classes = { "warning", "caption" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    private readonly ScrollBar _horizontal = new() { Orientation = Orientation.Horizontal };
    private readonly ScrollBar _vertical = new() { Orientation = Orientation.Vertical };
    private readonly ToggleButton _select = new() { Classes = { "command" } };
    private readonly ToggleButton _velocity = new() { Classes = { "command" }, IsChecked = true };
    private readonly ToggleButton _ghosts = new() { Classes = { "command" }, IsChecked = true };
    private readonly ToggleButton _audition = new() { Classes = { "command" }, IsChecked = true };
    private readonly Button _root = new() { Classes = { "compact" } };
    private readonly ComboBox _rootPitch = new() { MinWidth = 155 };
    private readonly TextBox _channelName = new() { MinWidth = 180, MaxWidth = 280 };
    private readonly TextBlock _rootLabel = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly Flyout _rootFlyout = new();
    private readonly Flyout _noteFlyout = new();

    private readonly NumericUpDown _notePitch = Field(0, 127, 1),
        _noteStart = Field(0, long.MaxValue / (decimal)MusicalPosition.TicksPerQuarter, 0.25m),
        _noteDuration = Field(0.000001m, long.MaxValue / (decimal)MusicalPosition.TicksPerQuarter, 0.25m),
        _noteVelocity = Field(0, 100, 1);

    private readonly TextBlock _rootHint = new()
        { Classes = { "caption", "secondary" }, MaxWidth = 280, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    private readonly List<(Control Control, string Key)> _labels = [];
    private readonly Dictionary<string, Button> _buttons = [];
    private BeatEditBasis? _rootBasis, _noteBasis;
    private Id<MusicalPart>? _rootPart, _notePart, _selectedPart;
    private NoteEvent[] _noteSelection = [];
    private (decimal Pitch, decimal Start, decimal Duration, decimal Velocity) _noteInitial;

    private readonly TextBlock _noteHint = new()
        { Classes = { "caption", "secondary" }, MaxWidth = 280, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    private Id<Pattern>? _lastPattern;
    private Guid _lastLifecycle;
    private bool _refreshing, _viewRefreshing, _disposed;
    private decimal _originalRoot = 60m;
    private HostPreferences _preferences;
    internal PianoRollCanvas Canvas { get; }
    internal Task PendingCommand { get; private set; } = Task.CompletedTask;
    internal event Action<Id<MusicalPart>>? PartOpened;
    private readonly Dictionary<Id<Pattern>, Id<MusicalPart>> _partMemory = [];

    private static NumericUpDown Field(decimal minimum, decimal maximum, decimal increment) => new()
    {
        Minimum = minimum, Maximum = maximum, Increment = increment, Width = 112, ShowButtonSpinner = false,
        FormatString = "G", NumberFormat = CultureInfo.InvariantCulture.NumberFormat
    };

    internal PianoRollPaneContent(ShellSession shell, DesktopWorkflow workflow, Func<string, Task> action,
        Action openRack)
    {
        _shell = shell;
        _preferences = shell.Preferences;
        _workflow = workflow;
        RowDefinitions = new("Auto,Auto,Auto,*,Auto");
        Canvas = new(shell, workflow);
        this.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Background"));
        var target = new WrapPanel { Margin = new(8, 6) };
        target.Children.Add(_pattern);
        target.Children.Add(_part);
        target.Children.Add(_scope);
        Children.Add(target);
        _pattern.Margin = _part.Margin = new(0, 0, 8, 0);
        _scope.Margin = new(8, 0);
        Identify(_pattern, "Piano.Pattern");
        Identify(_part, "Piano.Part");
        _pattern.SelectionChanged += async (_, _) =>
        {
            if (_refreshing || _pattern.SelectedItem is not OccurrenceChoice choice ||
                choice.Id == workflow.State.Selected) return;
            CancelGesture();
            PendingCommand = workflow.SelectAsync(choice.Id);
            await PendingCommand;
            Refresh();
        };
        _part.SelectionChanged += (_, _) =>
        {
            if (_refreshing || _part.SelectedItem is not PartChoice choice) return;
            CancelGesture();
            _selectedPart = choice.Id;
            Refresh();
            PartOpened?.Invoke(choice.Id);
        };
        var toolbar = new WrapPanel { Margin = new(8, 0, 8, 6) };
        SetRow(toolbar, 1);
        Children.Add(toolbar);
        AddButton(toolbar, "Draw", "Draw", () =>
        {
            _select.IsChecked = false;
            Canvas.Focus();
            return Task.CompletedTask;
        });
        _select.Content = HostIcons.Create("Selection", _select);
        toolbar.Children.Add(_select);
        Identify(_select, "Piano.Select");
        _select.IsCheckedChanged += (_, _) =>
        {
            Canvas.Cancel();
            Canvas.SelectMode = _select.IsChecked == true;
            Canvas.Focus();
        };
        _snap.Margin = _length.Margin = new(6, 0);
        toolbar.Children.Add(_snap);
        toolbar.Children.Add(_length);
        Identify(_snap, "Piano.Snap");
        Identify(_length, "Piano.Length");
        _snap.SelectionChanged += (_, _) =>
        {
            if (!_refreshing)
            {
                Canvas.Cancel();
                Canvas.SnapTicks = SnapValues[Math.Max(0, _snap.SelectedIndex)];
            }
        };
        _length.SelectionChanged += (_, _) =>
        {
            if (!_refreshing && _length.SelectedIndex >= 0)
                Canvas.DefaultDuration = LengthValues[_length.SelectedIndex];
        };
        AddButton(toolbar, "Duplicate", "Duplicate", () =>
        {
            Canvas.Duplicate();
            return Task.CompletedTask;
        });
        AddButton(toolbar, "Delete", "Delete", () =>
        {
            Canvas.Delete();
            return Task.CompletedTask;
        });
        AddButton(toolbar, "Properties", "Info", () =>
        {
            OpenNoteProperties();
            return Task.CompletedTask;
        });
        _velocity.Content = HostIcons.Create("Velocity", _velocity);
        toolbar.Children.Add(_velocity);
        Identify(_velocity, "Piano.Velocity");
        _velocity.IsCheckedChanged += (_, _) =>
        {
            if (!_refreshing) Canvas.VelocityVisible = _velocity.IsChecked == true;
        };
        _ghosts.Content = HostIcons.Create("Piano", _ghosts);
        toolbar.Children.Add(_ghosts);
        Identify(_ghosts, "Piano.Ghosts");
        _ghosts.IsCheckedChanged += (_, _) =>
        {
            Canvas.Ghosts = _ghosts.IsChecked == true;
            Canvas.InvalidateVisual();
        };
        _audition.Content = HostIcons.Create("Sound", _audition);
        toolbar.Children.Add(_audition);
        Identify(_audition, "Piano.Audition");
        _audition.IsCheckedChanged += (_, _) => Canvas.AuditionEdits = _audition.IsChecked == true;
        AddButton(toolbar, "ZoomOut", "Minimize", () =>
        {
            Canvas.Zoom(1 / 1.25, Canvas.Bounds.Width / 2);
            return Task.CompletedTask;
        });
        AddButton(toolbar, "ZoomIn", "Add", () =>
        {
            Canvas.Zoom(1.25, Canvas.Bounds.Width / 2);
            return Task.CompletedTask;
        });
        AddButton(toolbar, "Fit", "Fit", () =>
        {
            Canvas.Fit();
            return Task.CompletedTask;
        });
        toolbar.Children.Add(_root);
        Identify(_root, "Piano.Root");
        _root.Flyout = _rootFlyout;
        _root.Click += (_, _) =>
        {
            _rootBasis = Canvas.Basis;
            _rootPart = Canvas.Part?.Id;
            RefreshRoot();
        };
        BuildRootEditor();
        BuildNoteEditor();
        AddButton(toolbar, "Play", "Play", async () =>
        {
            await workflow.SetSongModeAsync(false);
            await workflow.PlayAsync();
        });
        AddButton(toolbar, "Stop", "Stop", async () => await workflow.StopAsync());
        AddButton(toolbar, "Rack", "Rack", () =>
        {
            openRack();
            return Task.CompletedTask;
        });
        AddButton(toolbar, "Export", "Export", () => action("ExportWave"));
        _warning.Margin = new(8, 0, 8, 6);
        SetRow(_warning, 2);
        Children.Add(_warning);
        var music = new Grid { RowDefinitions = new("*,Auto"), ColumnDefinitions = new("*,Auto") };
        var tray = new Border { Child = Canvas, ClipToBounds = true };
        tray.Bind(Border.BoxShadowProperty, this.GetResourceObservable("Material.Recess"));
        music.Children.Add(tray);
        SetColumn(_vertical, 1);
        music.Children.Add(_vertical);
        SetRow(_horizontal, 1);
        music.Children.Add(_horizontal);
        _horizontal.Margin = new(HostMetrics.Values["Piano.KeyWidth"], 0, 0, 0);
        _horizontal.ValueChanged += (_, _) =>
        {
            if (!_viewRefreshing) Canvas.SetView(_horizontal.Value, Canvas.OffsetY);
        };
        _vertical.ValueChanged += (_, _) =>
        {
            if (!_viewRefreshing) Canvas.SetView(Canvas.OffsetX, _vertical.Value);
        };
        SetRow(music, 3);
        Children.Add(music);
        _feedback.Margin = new(8, 4, 28, 4);
        SetRow(_feedback, 4);
        Children.Add(_feedback);
        Canvas.Changed += RefreshView;
        Canvas.CommandSubmitted += task => PendingCommand = ObserveCommandAsync(task);
        workflow.Changed += WorkflowChanged;
        shell.PropertyChanged += PreferencesChanged;
        Refresh();
    }

    private static readonly long[] SnapValues = [960000, 480000, 240000, 120000, 320000, 1];
    private static readonly long[] LengthValues = [240000, 480000, 960000, 1920000, 3840000];

    private void Identify(Control control, string key)
    {
        AutomationProperties.SetAutomationId(control, key);
        _labels.Add((control, key));
    }

    private void AddButton(Panel panel, string key, string icon, Func<Task> command)
    {
        var button = new Button { Classes = { "command" } };
        button.Content = HostIcons.Create(icon, button);
        panel.Children.Add(button);
        _buttons[key] = button;
        Identify(button, "Piano." + key);
        button.Click += async (_, _) =>
        {
            if (_disposed || !PendingCommand.IsCompleted) return;
            Task previous = PendingCommand;
            Task submitted = command();
            PendingCommand = ObserveCommandAsync(ReferenceEquals(previous, PendingCommand)
                ? submitted
                : Task.WhenAll(submitted, PendingCommand));
            await PendingCommand;
        };
    }

    private async Task ObserveCommandAsync(Task task)
    {
        await task;
        if (!_disposed) Refresh();
    }

    private void BuildRootEditor()
    {
        var body = new StackPanel { Margin = new(12), Classes = { "spaced" } };
        var nameLabel = new TextBlock();
        Identify(nameLabel, "Piano.ChannelName");
        body.Children.Add(nameLabel);
        body.Children.Add(_channelName);
        Identify(_channelName, "Piano.ChannelName");
        var row = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "parameterRow" } };
        Identify(_rootLabel, "Piano.Root");
        row.Children.Add(_rootLabel);
        row.Children.Add(_rootPitch);
        Identify(_rootPitch, "Piano.RootPitch");
        _rootPitch.ItemsSource = Enumerable.Range(0, 128).Select(i => $"{PianoNotes.Name(i)} · {i}").ToArray();
        body.Children.Add(row);
        body.Children.Add(_rootHint);
        var apply = new Button { Classes = { "primary" } };
        Identify(apply, "Piano.Apply");
        body.Children.Add(apply);
        apply.Click += async (_, _) =>
        {
            if (_rootBasis is not { } basis || _rootPart is not { } part || _rootPitch.SelectedIndex < 0 ||
                !PendingCommand.IsCompleted) return;
            PendingCommand =
                _workflow.ConfigurePianoSourceAsync(basis, part, _channelName.Text ?? "",
                    _rootPitch.SelectedIndex == (int)_originalRoot ? _originalRoot : _rootPitch.SelectedIndex);
            await PendingCommand;
            _rootFlyout.Hide();
            Refresh();
            Canvas.Focus();
        };
        _rootFlyout.Content = body;
    }

    private void RefreshRoot()
    {
        _channelName.Text = Canvas.Part?.Name;
        var sound = _workflow.State.Snapshot.State.Sounds.SingleOrDefault(s => s.Id == Canvas.Part?.SoundId);
        _originalRoot = sound?.Parameters.TryGetValue("rootPitch", out var root) == true ? root : 60m;
        _rootPitch.SelectedIndex = (int)_originalRoot;
        _rootPitch.IsEnabled = sound?.Algorithm == PcmSampler.Algorithm;
        _rootHint.Text = _shell["Piano.RootHint"];
    }

    private void BuildNoteEditor()
    {
        var body = new StackPanel { Margin = new(12), Classes = { "spaced" } };
        foreach (var (field, key) in new[]
                 {
                     (_notePitch, "Pitch"), (_noteStart, "Start"), (_noteDuration, "Duration"),
                     (_noteVelocity, "Velocity")
                 })
        {
            var row = new Grid { ColumnDefinitions = new("*,Auto"), ColumnSpacing = 12, Classes = { "parameterRow" } };
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            Identify(label, "Piano." + key);
            row.Children.Add(label);
            SetColumn(field, 1);
            row.Children.Add(field);
            body.Children.Add(row);
            AutomationProperties.SetAutomationId(field, "Piano.Note" + key);
        }

        body.Children.Add(_noteHint);

        var apply = new Button { Classes = { "primary" } };
        Identify(apply, "Piano.Apply");
        body.Children.Add(apply);
        apply.Click += async (_, _) =>
        {
            if (_noteBasis is not { } basis || Canvas.Part is not { } part || Canvas.Pattern is not { } pattern ||
                _notePart != part.Id || basis != Canvas.Basis ||
                _noteSelection.Length == 0 || !PendingCommand.IsCompleted ||
                _notePitch.Value is not { } pitch || _noteStart.Value is not { } start ||
                _noteDuration.Value is not { } duration || _noteVelocity.Value is not { } velocity) return;
            var moved = PianoNotes.Move(_noteSelection,
                (start == _noteInitial.Start
                    ? _noteSelection.Min(n => n.Position.Ticks)
                    : (long)decimal.Round(start * MusicalPosition.TicksPerQuarter)) -
                _noteSelection.Min(n => n.Position.Ticks),
                pitch - _noteSelection[0].Pitch, pattern.Length.Ticks);
            long length = Math.Max(1, (long)decimal.Round(duration * MusicalPosition.TicksPerQuarter));
            bool changeLength = duration != _noteInitial.Duration, changeVelocity = velocity != _noteInitial.Velocity;
            if (moved.Any(n => n.Position.Ticks + (changeLength ? length : n.Duration.Ticks) > pattern.Length.Ticks))
            {
                _noteDuration.Classes.Add("error");
                return;
            }

            var edited = moved.Select(n => n with
            {
                Duration = changeLength ? new(length) : n.Duration,
                Intensity = changeVelocity ? velocity / 100 : n.Intensity
            }).ToArray();
            Canvas.CommitNotes(basis, part.Id,
                [.. part.Notes.Where(n => !_noteSelection.Any(s => s.Id == n.Id)), .. edited]);
            await PendingCommand;
            _noteFlyout.Hide();
            Refresh();
            Canvas.RevealSelection();
            Canvas.Focus();
        };
        _noteFlyout.Content = body;
        _buttons["Properties"].Flyout = _noteFlyout;
    }

    private void OpenNoteProperties()
    {
        _noteBasis = Canvas.Basis;
        _notePart = Canvas.Part?.Id;
        _noteSelection = Canvas.Part?.Notes.Where(n => Canvas.Selection.Contains(n.Id)).ToArray() ?? [];
        if (_noteSelection.Length == 0)
        {
            _noteFlyout.Hide();
            return;
        }

        _notePitch.Value = _noteSelection[0].Pitch;
        _noteStart.Maximum = (Canvas.Pattern!.Length.Ticks - 1) / (decimal)MusicalPosition.TicksPerQuarter;
        _noteDuration.Maximum = Canvas.Pattern.Length.Ticks / (decimal)MusicalPosition.TicksPerQuarter;
        _noteStart.Value = _noteSelection.Min(n => n.Position.Ticks) / (decimal)MusicalPosition.TicksPerQuarter;
        _noteDuration.Value = _noteSelection[0].Duration.Ticks / (decimal)MusicalPosition.TicksPerQuarter;
        _noteVelocity.Value = _noteSelection[0].Intensity * 100;
        _noteInitial = (_notePitch.Value!.Value, _noteStart.Value!.Value, _noteDuration.Value!.Value,
            _noteVelocity.Value!.Value);
        _noteHint.IsVisible = _noteSelection.Length > 1;
        _noteHint.Text = _shell["Piano.GroupProperties"];
        _noteDuration.Classes.Remove("error");
    }

    internal void OpenPart(Id<MusicalPart> part)
    {
        CancelGesture();
        Refresh();
        _selectedPart = part;
        Refresh();
        if (Canvas.Part?.Id == part) PartOpened?.Invoke(part);
    }

    private void WorkflowChanged(WorkflowState _) => Dispatcher.UIThread.Post(() =>
    {
        if (!_disposed) Refresh();
    });

    private void PreferencesChanged(object? sender, PropertyChangedEventArgs args)
    {
        // Shell also announces document/status observations. They must not cancel a fresh draft
        // merely because a notification from the preceding commit arrives later on the GUI thread.
        if (_preferences != _shell.Preferences)
        {
            _preferences = _shell.Preferences;
            CancelGesture();
        }

        Refresh();
    }

    internal void Refresh()
    {
        if (_disposed) return;
        _refreshing = true;
        try
        {
            var observation = _workflow.State;
            var (pattern, _) = BeatSteps.Resolve(observation);
            if (_lastLifecycle != observation.Document.LifecycleId || _lastPattern != pattern?.Id)
            {
                if (_lastLifecycle != observation.Document.LifecycleId) _partMemory.Clear();
                _selectedPart = null;
                if (pattern is not null && _partMemory.TryGetValue(pattern.Id, out var remembered))
                    _selectedPart = remembered;
                _lastLifecycle = observation.Document.LifecycleId;
                _lastPattern = pattern?.Id;
            }

            if (pattern?.Parts.Any(p => p.Id == _selectedPart) != true)
                _selectedPart = pattern is null ? null :
                    PatternEditorNavigation.PreferredPart(observation.Snapshot.State, pattern)?.Id ??
                    pattern.Parts.FirstOrDefault()?.Id;
            if (pattern is not null && _selectedPart is { } selected) _partMemory[pattern.Id] = selected;
            if (!_pattern.Items.OfType<OccurrenceChoice>().SequenceEqual(observation.Occurrences))
                _pattern.ItemsSource = observation.Occurrences;
            _pattern.SelectedItem = observation.Occurrences.SingleOrDefault(o => o.Id == observation.Selected);
            var choices = pattern?.Parts.Select(p => new PartChoice(p.Id, p.Name)).ToArray() ?? [];
            if (!_part.Items.OfType<PartChoice>().SequenceEqual(choices)) _part.ItemsSource = choices;
            _part.SelectedItem = choices.SingleOrDefault(p => p.Id == _selectedPart);
            Canvas.Adopt(observation, _selectedPart);
            if (_rootBasis is not null && _rootBasis != Canvas.Basis)
            {
                _rootFlyout.Hide();
                _rootBasis = null;
            }

            if (_noteBasis is not null && (_noteBasis != Canvas.Basis || _notePart != Canvas.Part?.Id))
            {
                _noteFlyout.Hide();
                _noteBasis = null;
            }

            foreach (var (control, key) in _labels)
            {
                string text = _shell[key];
                AutomationProperties.SetName(control, text);
                ToolTip.SetTip(control, text);
                if (control is TextBlock label) label.Text = text;
                else if (control is Button button && key == "Piano.Apply") button.Content = text;
            }

            int snapIndex = Math.Max(0, Array.IndexOf(SnapValues, Canvas.SnapTicks));
            _snap.ItemsSource = new[] { "1/4", "1/8", "1/16", "1/32", "1/8 T", _shell["Arrangement.SnapOff"] };
            _snap.SelectedIndex = snapIndex;
            int lengthIndex = Array.IndexOf(LengthValues, Canvas.DefaultDuration);
            _length.ItemsSource = new[] { "1/16", "1/8", "1/4", "1/2", "1/1" };
            _length.SelectedIndex = lengthIndex;
            decimal? root = observation.Snapshot.State.Sounds.SingleOrDefault(s => s.Id == Canvas.Part?.SoundId)
                ?.Parameters.GetValueOrDefault("rootPitch", 60);
            _root.Content = _shell["Piano.Root"] + ": " + (root is { } midi ? PianoNotes.Name(midi) : "—");
            _root.IsEnabled = Canvas.CanEdit;
            _pattern.IsEnabled = _part.IsEnabled = !observation.Busy;
            int uses = pattern is null
                ? 0
                : observation.Snapshot.State.ArrangementClips.Count(c =>
                    observation.Snapshot.State.Placements.Any(p =>
                        p.Id == c.SourcePlacementId && p.PatternId == pattern.Id));
            _scope.Text = uses > 0 ? $"{_shell["Piano.Shared"]} · {uses}" : _shell["Piano.PatternScope"];
            string? refusal = PianoNotes.Refusal(observation.Snapshot.State, pattern, Canvas.Part);
            _warning.IsVisible = refusal is not null && refusal != "Piano.NoTarget";
            _warning.Text = refusal is null ? "" : _shell[refusal];
            _buttons["Play"].IsEnabled = observation.CanPlay;
            _buttons["Stop"].IsEnabled = observation.CanStop || Canvas.Part is not null;
            _buttons["Export"].IsEnabled = Canvas.Basis is not null && !observation.Busy;
            _buttons["Draw"].Classes.Set("primary", !_select.IsChecked.GetValueOrDefault());
            AutomationProperties.SetHelpText(Canvas, _shell["Piano.Help"]);
            AutomationProperties.SetName(Canvas,
                _shell["Pane.Piano"] + " · " + pattern?.Name + " · " + Canvas.Part?.Name);
            ToolTip.SetTip(Canvas, _shell["Piano.KeyboardSelection"]);
            RefreshView();
            Canvas.InvalidateVisual();
        }
        finally
        {
            _refreshing = false;
        }
    }


    private void RefreshView()
    {
        if (_disposed) return;
        _viewRefreshing = true;
        try
        {
            _horizontal.Maximum = Canvas.MaximumX;
            _horizontal.ViewportSize = Math.Max(1, Canvas.Bounds.Width - Canvas.KeyWidth);
            _horizontal.Value = Canvas.OffsetX;
            _vertical.Maximum = Canvas.MaximumY;
            _vertical.ViewportSize = Math.Max(1, Canvas.NoteBottom - Canvas.RulerHeight);
            _vertical.Value = Canvas.OffsetY;
            _horizontal.SmallChange = Canvas.QuarterWidth / 4;
            _vertical.SmallChange = Canvas.RowHeight;
            _feedback.Text = Canvas.Feedback.Length > 0 ? Canvas.Feedback :
                Canvas.Selection.Count > 0 ? $"{Canvas.Selection.Count} {_shell["Piano.Selected"]}" : "";
            foreach (string key in new[] { "Duplicate", "Delete", "Properties" })
                _buttons[key].IsEnabled = Canvas.CanEdit && Canvas.Selection.Count > 0;
        }
        finally
        {
            _viewRefreshing = false;
        }
    }

    public void FocusEditor() => Canvas.Focus();

    public void CancelGesture()
    {
        Canvas.Cancel();
        _rootFlyout.Hide();
        _noteFlyout.Hide();
        _rootBasis = _noteBasis = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        CancelGesture();
        _disposed = true;
        _workflow.Changed -= WorkflowChanged;
        _shell.PropertyChanged -= PreferencesChanged;
    }
}
