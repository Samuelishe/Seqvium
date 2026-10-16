// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Retained, locally scrolling projection of canonical Pattern parts and notes.</summary>
internal sealed class BeatPaneContent : Grid, IDisposable
{
    internal event Action<Id<MusicalPart>>? SampleLabRequested;
    private sealed record Channel(Id<MusicalPart> Part, TextBlock Label, TextBlock Warning, ToggleButton[] Steps);

    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly Func<BeatEditBasis, Func<bool>, Task> _import;
    private readonly Button _create = new(), _add = new(), _apply = new() { Classes = { "command" } };
    private readonly ComboBox _target = new() { MinWidth = 160 };
    private readonly TextBox _name = new() { MinWidth = 110, Classes = { "patternName" } };

    private readonly TextBlock _message = new()
        { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { "secondary" } };

    private readonly StackPanel _rows = new() { Classes = { "tight" } };
    private readonly StackPanel _patternControls = new() { Classes = { "tight" } };
    private readonly List<Channel> _channels = [];
    private readonly Dictionary<Id<MusicalPart>, (Control Steps, Button Preview)> _melodies = [];
    internal event Action<Id<MusicalPart>>? PianoRequested;
    internal event Action<Id<MusicalPart>>? SoundRequested;
    internal event Action<Id<SoundDefinition>>? MixerRequested;
    private readonly Button _drum = new(), _synth = new();
    private readonly List<(MenuItem Item, string Key)> _menuLabels = [];
    private readonly ComboBox _bars = new() { MinWidth = 90 };
    private readonly Button _delete = new() { Classes = { "command", "danger" } };

    private readonly TextBlock _position = new()
        { Classes = { "numeric", "success" }, VerticalAlignment = VerticalAlignment.Center };

    private readonly TextBlock _empty = new()
        { Classes = { "secondary" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new(16) };

    private readonly TextBlock _sounds = new()
        { Classes = { "caption" }, VerticalAlignment = VerticalAlignment.Center };

    private readonly Grid _ruler;
    private readonly Grid _rack;
    private readonly List<TextBlock> _rulerCells = [];
    private int _stepCount = BeatSteps.Count;
    private readonly ScrollViewer _rackScroll;
    private (Channel Row, BeatEditBasis Basis, bool[] Values, bool Value, int Last, IPointer Pointer)? _paint;
    private readonly Dictionary<ToggleButton, BeatEditBasis> _armed = [];
    private readonly HashSet<ToggleButton> _expired = [];
    private BeatEditBasis? _basis, _nameBasis;
    private Id<Pattern>? _patternId;
    private Guid _lifecycle;
    private long _generation = -1, _interaction;
    private bool _refreshing, _disposed;
    private WorkflowState? _lastRendered;
    private int _currentStep = -1;
    internal Task PendingCommand { get; private set; } = Task.CompletedTask;

    internal BeatPaneContent(ShellSession shell, DesktopWorkflow workflow, Func<BeatEditBasis, Func<bool>, Task> import)
    {
        _shell = shell;
        _workflow = workflow;
        _import = import;
        RowDefinitions = new("Auto,Auto,*,Auto");
        var toolbar = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new(12, 8, 12, 8) };
        _patternControls.Children.Add(_target);
        var rename = new Grid { ColumnDefinitions = new("*,Auto,Auto,Auto"), Classes = { "parameterRow" } };
        rename.Children.Add(_name);
        SetColumn(_apply, 1);
        rename.Children.Add(_apply);
        _apply.Content = HostIcons.Create("Check", _apply, 16);
        SetColumn(_bars, 2);
        _bars.Margin = new(8, 0);
        rename.Children.Add(_bars);
        SetColumn(_delete, 3);
        _delete.Content = HostIcons.Create("Close", _delete, 16);
        rename.Children.Add(_delete);
        _patternControls.Children.Add(rename);
        toolbar.Children.Add(_patternControls);
        SetColumn(_create, 1);
        toolbar.Children.Add(_create);
        Children.Add(toolbar);
        _message.Margin = new(12, 0, 12, 8);
        SetRow(_message, 1);
        Children.Add(_message);
        var rack = new Grid
            { RowDefinitions = new("Auto,Auto,Auto"), HorizontalAlignment = HorizontalAlignment.Stretch };
        _rack = rack;
        _ruler = RackRow();
        _ruler.Margin = new(12, 0);
        _ruler.Height = 48;
        _ruler.Bind(BackgroundProperty, this.GetResourceObservable("Rack.Ruler"));
        _sounds.Margin = new(12, 0);
        _ruler.Children.Add(_sounds);
        RebuildRuler();
        rack.Children.Add(_ruler);
        _rows.Margin = new(12, 4, 12, 8);
        SetRow(_rows, 1);
        rack.Children.Add(_rows);
        SetRow(_empty, 2);
        rack.Children.Add(_empty);
        _rackScroll = new()
        {
            Content = rack, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        var tray = new Border { Child = _rackScroll, BorderThickness = new(0, 1, 0, 1) };
        tray.Bind(Border.BackgroundProperty, this.GetResourceObservable("Surface.Workspace"));
        tray.Bind(Border.BorderBrushProperty, this.GetResourceObservable("Border.Default"));
        tray.Bind(Border.BoxShadowProperty, this.GetResourceObservable("Material.Recess"));
        SetRow(tray, 2);
        Children.Add(tray);
        var footer = new Grid
            { ColumnDefinitions = new("*,Auto"), Margin = new(12, 8, 12, 8), ColumnSpacing = 8 };
        _add.Classes.Add("primary");
        var addActions = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 4 };
        addActions.Children.Add(_synth);
        addActions.Children.Add(_drum);
        addActions.Children.Add(_add);
        footer.Children.Add(addActions);
        SetColumn(_position, 1);
        footer.Children.Add(_position);
        SetRow(footer, 3);
        Children.Add(footer);
        AddHandler(KeyDownEvent, (_, args) =>
        {
            if (args.Key != Key.Escape || _paint is null) return;
            CancelGesture();
            Refresh();
            args.Handled = true;
        }, RoutingStrategies.Tunnel);
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            if (e.DataTransfer.TryGetValue(BrowserPaneContent.DragFormat) is { } source)
            {
                e.DragEffects = source.SamplerCompatible && !_workflow.State.Busy && PendingCommand.IsCompleted
                    ? DragDropEffects.Copy : DragDropEffects.None;
                Classes.Set("dropReady", e.DragEffects == DragDropEffects.Copy);
                e.Handled = true;
                return;
            }
            var files = e.DataTransfer.TryGetFiles()?.ToArray();
            e.DragEffects = _add.IsEnabled && PendingCommand.IsCompleted && files is { Length: 1 } &&
                            files[0].TryGetLocalPath() is { } path && Path.GetExtension(path)
                                .Equals(".wav", StringComparison.OrdinalIgnoreCase)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            Classes.Set("dropReady", e.DragEffects == DragDropEffects.Copy);
            e.Handled = true;
        });
        AddHandler(DragDrop.DragLeaveEvent, (_, _) => Classes.Remove("dropReady"));
        AddHandler(DragDrop.DropEvent, async (_, e) =>
        {
            Classes.Remove("dropReady");
            if (e.DataTransfer.TryGetValue(BrowserPaneContent.DragFormat) is { } source)
            {
                e.Handled = true;
                if (_disposed || !source.SamplerCompatible || _workflow.State.Busy || !PendingCommand.IsCompleted) return;
                var before = _workflow.State;
                PendingCommand = _workflow.UseBrowserRackAsync(ArrangementEditBasis.From(before), before.Selected, source);
                await PendingCommand;
                Refresh();
                return;
            }
            var files = e.DataTransfer.TryGetFiles()?.ToArray();
            if (_disposed || !_add.IsEnabled || !PendingCommand.IsCompleted || _basis is not { } basis ||
                files is not { Length: 1 } || files[0].TryGetLocalPath() is not { } path ||
                !Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase)) return;
            CancelGesture();
            e.Handled = true;
            PendingCommand = _workflow.AddBeatWavAsync(basis, path);
            await PendingCommand;
            Refresh();
        });
        AutomationProperties.SetAutomationId(_create, "Beat.Create");
        AutomationProperties.SetAutomationId(_add, "Beat.AddWav");
        AutomationProperties.SetAutomationId(_synth, "Beat.AddSynth");
        _synth.Click += async (_, _) =>
        {
            if (_basis is not { } basis || !PendingCommand.IsCompleted) return;
            var before = BeatSteps.Resolve(_workflow.State).Pattern!.Parts.Select(p => p.Id).ToHashSet();
            PendingCommand = _workflow.AddDrumVoiceAsync(basis, _shell["Synth.Title"], SynthVoice.Algorithm);
            await PendingCommand; Refresh();
            var created = BeatSteps.Resolve(_workflow.State).Pattern?.Parts.SingleOrDefault(p => !before.Contains(p.Id));
            if (created is not null) SoundRequested?.Invoke(created.Id);
        };
        AutomationProperties.SetAutomationId(_drum, "Beat.AddDrum");
        _drum.Click += (_, _) =>
        {
            if (_basis is not { } basis || !PendingCommand.IsCompleted) return;
            var menu = new ContextMenu();
            foreach (var instrument in DrumInstruments.All)
            {
                var item = new MenuItem { Header = _shell["Drum." + instrument.Key] };
                AutomationProperties.SetAutomationId(item, "Beat.AddDrum." + instrument.Key);
                item.Click += async (_, _) =>
                {
                    menu.Close();
                    if (!PendingCommand.IsCompleted) return;
                    var before = _workflow.State.Snapshot.State.Patterns.SelectMany(p => p.Parts)
                        .Select(p => p.Id).ToHashSet();
                    PendingCommand =
                        _workflow.AddDrumVoiceAsync(basis, _shell["Drum." + instrument.Key], instrument.Algorithm);
                    await PendingCommand;
                    Refresh();
                    var created = _workflow.State.Snapshot.State.Patterns.SingleOrDefault(p => p.Id == basis.Pattern)
                        ?.Parts
                        .SingleOrDefault(p => !before.Contains(p.Id));
                    if (created is not null) SoundRequested?.Invoke(created.Id);
                };
                menu.Items.Add(item);
            }

            _drum.ContextMenu = menu;
            menu.Open(_drum);
        };
        AutomationProperties.SetAutomationId(_target, "Beat.Target");
        AutomationProperties.SetAutomationId(_name, "Beat.Name");
        AutomationProperties.SetAutomationId(_apply, "Beat.ApplyName");
        AutomationProperties.SetAutomationId(_bars, "Beat.Bars");
        AutomationProperties.SetAutomationId(_delete, "Beat.Delete");
        AutomationProperties.SetAutomationId(_position, "Beat.Position");
        _bars.SelectionChanged += async (_, _) =>
        {
            if (_refreshing || _basis is not { } basis || _bars.SelectedIndex < 0) return;
            CancelGesture();
            PendingCommand = workflow.ResizeBeatAsync(basis, _bars.SelectedIndex + 1);
            await PendingCommand;
            Refresh();
        };
        _delete.Click += async (_, _) =>
        {
            if (_basis is not { } basis) return;
            CancelGesture();
            PendingCommand = workflow.DeleteBeatAsync(basis);
            await PendingCommand;
            Refresh();
        };
        _create.Click += async (_, _) =>
        {
            CancelGesture();
            var state = workflow.State;
            PendingCommand =
                workflow.CreateBeatAsync(state.Document.LifecycleId, state.Generation, shell["Beat.DefaultName"]);
            await PendingCommand;
            Refresh();
        };
        _add.Click += async (_, _) =>
        {
            if (_basis is not { } basis) return;
            CancelGesture();
            long interaction = _interaction;
            PendingCommand = _import(basis, () => !_disposed && IsEffectivelyVisible && _interaction == interaction);
            await PendingCommand;
            Refresh();
        };
        _target.SelectionChanged += async (_, _) =>
        {
            if (_refreshing || _target.SelectedItem is not OccurrenceChoice choice ||
                choice.Id == workflow.State.Selected) return;
            CancelGesture();
            PendingCommand = workflow.SelectAsync(choice.Id);
            await PendingCommand;
            Refresh();
        };
        _name.GotFocus += (_, _) => _nameBasis = _basis;
        _apply.Click += async (_, _) => await CommitNameAsync();
        _name.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                args.Handled = true;
                await CommitNameAsync();
            }
            else if (args.Key == Key.Escape)
            {
                CancelGesture();
                _name.Text = SelectedPattern()?.Name;
                FocusEditor();
                args.Handled = true;
            }
        };
        SizeChanged += (_, _) => Refresh();
        workflow.Changed += WorkflowChanged;
        shell.PropertyChanged += PreferencesChanged;
        Refresh();
    }

    private Pattern? SelectedPattern() => BeatSteps.Resolve(_workflow.State).Pattern;

    private void WorkflowChanged(WorkflowState _) => Dispatcher.UIThread.Post(() =>
    {
        if (!_disposed) Refresh(false);
    });

    private void PreferencesChanged(object? sender, PropertyChangedEventArgs _) => Refresh();

    private void Label(Control control, string key)
    {
        string text = _shell[key];
        AutomationProperties.SetName(control, text);
        ToolTip.SetTip(control, text);
        if (control is Button button && button != _apply && button != _delete)
        {
            if (button == _create && Bounds.Width < 500)
            {
                button.Content = HostIcons.Create("Add", button, 16);
                return;
            }

            button.HorizontalContentAlignment = HorizontalAlignment.Center;
            var row = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "compact" } };
            row.Children.Add(HostIcons.Create(button == _add ? "Add" : "Rack", button, 16));
            row.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
            button.Content = row;
        }
    }

    internal void Refresh(bool force = true)
    {
        if (_disposed) return;
        var observation = _workflow.State;
        var state = observation.Snapshot.State;
        if (!force && _lastRendered is { } previous &&
            previous.Document.LifecycleId == observation.Document.LifecycleId &&
            previous.Generation == observation.Generation && previous.Selected == observation.Selected &&
            previous.Busy == observation.Busy && previous.IsClosed == observation.IsClosed)
        {
            RefreshPosition(observation);
            return;
        }

        _lastRendered = observation;
        var (pattern, basis) = BeatSteps.Resolve(observation);
        if (_lifecycle != observation.Document.LifecycleId || _generation != observation.Generation ||
            _basis?.Attachment != basis?.Attachment)
        {
            CancelGesture();
            _interaction++;
            _name.Text = pattern?.Name;
            _nameBasis = basis;
            _lifecycle = observation.Document.LifecycleId;
            _generation = observation.Generation;
        }

        _basis = basis;
        _refreshing = true;
        try
        {
            Label(_create, "Beat.Create");
            Label(_add, "Beat.AddWav");
            Label(_drum, "Drum.Add");
            Label(_synth, "Synth.Add");
            Label(_name, "Beat.Name");
            Label(_apply, "Beat.ApplyName");
            Label(_target, "Beat.Target");
            Label(_bars, "Beat.Bars");
            Label(_delete, "Beat.Delete");
            var barLabels = Enumerable.Range(1, BeatSteps.MaximumBars).Select(i => $"{i} {_shell["Beat.BarsUnit"]}")
                .ToArray();
            if (!_bars.Items.OfType<string>().SequenceEqual(barLabels)) _bars.ItemsSource = barLabels;
            _bars.SelectedIndex = pattern is null ? -1 : (int)(pattern.Length.Ticks / BeatSteps.BarTicks) - 1;
            bool available = !observation.Busy && !observation.IsClosed;
            _create.IsEnabled = available && state.Settings.Meter is { Numerator: 4, Denominator: 4 };
            string? refusal = BeatSteps.PatternRefusal(state, pattern);
            _message.Text = refusal is null ? "" : _shell[refusal];
            _message.IsVisible = refusal is not null && refusal != "Beat.NoTarget";
            _empty.IsVisible = pattern is null || pattern.Parts.IsEmpty;
            _empty.Text = _shell[pattern is null ? "Beat.StartHint" : "Beat.AddHint"];
            _sounds.Text = _shell["Beat.Sounds"];
            ToolTip.SetTip(_position, _shell["Beat.PaintHint"]);
            AutomationProperties.SetHelpText(_rack, _shell["Beat.PaintHint"]);
            _bars.IsEnabled = available && refusal is null;
            _delete.IsEnabled = available && basis is not null;
            int nextCount = refusal is null ? BeatSteps.StepCount(pattern!) : BeatSteps.Count;
            bool lengthChanged = nextCount != _stepCount;
            if (lengthChanged)
            {
                _stepCount = nextCount;
                RebuildRuler();
            }

            _create.Classes.Set("primary", pattern is null);
            _ruler.IsVisible = pattern is not null;
            foreach (var (item, key) in _menuLabels) item.Header = _shell[key];
            _patternControls.IsVisible = pattern is not null;
            if (!_target.Items.OfType<OccurrenceChoice>().SequenceEqual(observation.Occurrences))
                _target.ItemsSource = observation.Occurrences;
            _target.SelectedItem = observation.Occurrences.SingleOrDefault(c => c.Id == observation.Selected);
            _target.IsVisible = observation.Occurrences.Length > 1;
            _target.IsEnabled = available;
            _name.IsEnabled = _apply.IsEnabled = available && refusal is null;
            var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == observation.Selected);
            var graph = state.Graphs.SingleOrDefault(g => g.Id == attachment?.GraphId);
            string? channelRefusal = pattern is null || attachment is null || graph is null
                ? refusal
                : DesktopWorkflow.BeatChannelRefusal(state, pattern, attachment, graph);
            _add.IsEnabled = available && channelRefusal is null && basis is not null;
            _drum.IsEnabled = _add.IsEnabled;
            _synth.IsEnabled = _add.IsEnabled; _synth.IsVisible = pattern is not null;
            _drum.IsVisible = pattern is not null;
            _add.IsVisible = pattern is not null;
            ToolTip.SetShowOnDisabled(_add, true);
            if (channelRefusal is not null)
                ToolTip.SetTip(_add, _shell["Beat.AddWav"] + " · " + _shell[channelRefusal]);
            if (lengthChanged || pattern is null || _patternId != pattern.Id ||
                !_channels.Select(c => c.Part).SequenceEqual(pattern.Parts.Select(p => p.Id)))
            {
                _rows.Children.Clear();
                _channels.Clear();
                _melodies.Clear();
                _menuLabels.Clear();
                _expired.Clear();
                _patternId = pattern?.Id;
                if (pattern is not null)
                    foreach (var part in pattern.Parts)
                        AddRow(part);
            }

            foreach (var channel in _channels)
            {
                var part = pattern!.Parts.Single(p => p.Id == channel.Part);
                channel.Label.Text = part.Name;
                ToolTip.SetTip(channel.Label, part.Name);
                string? partRefusal = refusal ?? BeatSteps.PartRefusal(state, part, pattern.Length.Ticks);
                bool melody = partRefusal == "Beat.NotesUnsupported" &&
                              PianoNotes.Refusal(state, pattern, part) is null;
                var projection = _melodies[part.Id];
                projection.Steps.IsVisible = !melody;
                projection.Preview.IsVisible = melody;
                if (melody)
                    projection.Preview.Content =
                        new PartNoteThumbnail(_shell, pattern, part, pattern.Parts.IndexOf(part))
                            { Height = 34, HorizontalAlignment = HorizontalAlignment.Stretch };
                ToolTip.SetTip(projection.Preview, part.Name + " · " + _shell["Piano.Open"]);
                channel.Warning.Text = partRefusal is null ? "" : _shell[partRefusal];
                channel.Warning.IsVisible = partRefusal is not null && !melody;
                for (int step = 0; step < _stepCount; step++)
                {
                    var button = channel.Steps[step];
                    var notes = part.Notes.Where(n => BeatSteps.Step(n) == step).ToArray();
                    button.IsChecked = _paint is { } paint && paint.Row.Part == part.Id
                        ? paint.Values[step]
                        : notes.Length > 0;
                    button.IsEnabled = available && partRefusal is null;
                    string text =
                        $"{part.Name} · {_shell["Beat.Step"]} {step + 1} · {_shell[notes.Length > 0 ? "Beat.On" : "Beat.Off"]}";
                    AutomationProperties.SetName(button, text);
                    ToolTip.SetTip(button, text + (partRefusal is null
                        ? " · " + _shell["Beat.Keyboard"] + " · " + _shell["Beat.PaintHint"]
                        : " · " + _shell[partRefusal]));
                }
            }

            // Reset after rebuilding controls so the current column always receives the shared marker.
            foreach (var cell in _rulerCells) cell.Classes.Remove("success");
            foreach (var channel in _channels)
            foreach (var button in channel.Steps)
                button.Classes.Remove("current");
            _currentStep = -1;
            RefreshPosition(observation);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void RefreshPosition(WorkflowState observation)
    {
        var execution = observation.Execution?.Execution;
        bool playing = execution is { Playing: true, Looping: true } &&
                       execution.Value.AttachmentId == _basis?.Attachment;
        int current = playing ? (int)(execution!.Value.LoopPositionTicks / BeatSteps.StepTicks) : -1;
        _position.Text = playing ? $"{current / 16 + 1}:{current % 16 / 4 + 1}:{current % 4 + 1}" : "—";
        AutomationProperties.SetName(_position, _shell["Beat.Position"] + " " + _position.Text);
        if (current == _currentStep) return;
        foreach (int step in new[] { _currentStep, current })
        {
            if (step < 0 || step >= _stepCount) continue;
            _rulerCells[step].Classes.Set("success", step == current);
            foreach (var channel in _channels) channel.Steps[step].Classes.Set("current", step == current);
        }

        _currentStep = current;
    }

    private void AddRow(MusicalPart part)
    {
        int index = _channels.Count;
        var label = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        var warning = new TextBlock
            { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { "warning", "caption" }, MaxWidth = 760 };
        var row = RackRow();
        row.Bind(HeightProperty, this.GetResourceObservable("Rack.RowHeight"));
        row.Bind(BackgroundProperty,
            this.GetResourceObservable(index % 2 == 0 ? "Surface.Background" : "Surface.Raised"));
        var identity = new Grid
            { ColumnDefinitions = new("Auto,Auto,*,Auto"), ColumnSpacing = 10, Margin = new(8, 0, 8, 0) };
        var accent = new Border { Width = 3, Height = 28, CornerRadius = new(1) };
        accent.Bind(Border.BackgroundProperty, this.GetResourceObservable($"Rack.Channel{index % 8}"));
        identity.Children.Add(accent);
        var number = new TextBlock
        {
            Text = (index + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture),
            VerticalAlignment = VerticalAlignment.Center, Classes = { "caption", "numeric" }
        };
        SetColumn(number, 1);
        identity.Children.Add(number);
        SetColumn(label, 2);
        identity.Children.Add(label);
        var more = new Button { Classes = { "command" } };
        more.Content = HostIcons.Create("More", more, 16);
        SetColumn(more, 3);
        identity.Children.Add(more);
        var menu = new ContextMenu();
        more.ContextMenu = menu;
        more.Click += (_, _) => menu.Open(more);
        var pianoAction = new MenuItem { Header = _shell["Piano.Open"] };
        AutomationProperties.SetAutomationId(pianoAction, $"Beat.Piano.{part.Id}");
        _menuLabels.Add((pianoAction, "Piano.Open"));
        menu.Items.Add(pianoAction);
        var soundAction = new MenuItem { Header = _shell["Drum.Open"] };
        AutomationProperties.SetAutomationId(soundAction, $"Beat.Sound.{part.Id}");
        _menuLabels.Add((soundAction, "Drum.Open"));
        menu.Items.Add(soundAction);
        var replaceAction = new MenuItem { Header = _shell["Beat.ReplaceWav"] };
        AutomationProperties.SetAutomationId(replaceAction, $"Beat.ReplaceWav.{part.Id}");
        _menuLabels.Add((replaceAction, "Beat.ReplaceWav"));
        menu.Items.Add(replaceAction);
        var labAction = new MenuItem { Header = _shell["Pane.SampleLab"] };
        _menuLabels.Add((labAction, "Pane.SampleLab"));
        AutomationProperties.SetAutomationId(labAction, $"Beat.SampleLab.{part.Id}");
        labAction.Click += (_, _) => { CancelGesture(); SampleLabRequested?.Invoke(part.Id); };
        menu.Items.Add(labAction);
        BeatEditBasis? replaceBasis = null;
        menu.Opened += (_, _) =>
        {
            replaceBasis = _basis;
            var sound = _workflow.State.Snapshot.State.Sounds.SingleOrDefault(s => s.Id == part.SoundId);
            replaceAction.IsEnabled = !_workflow.State.Busy && replaceBasis is not null &&
                sound is { Algorithm: PcmSampler.Algorithm, Extension: null } && sound.AdditionalData.IsEmpty;
            labAction.IsEnabled = replaceAction.IsEnabled;
            ToolTip.SetTip(replaceAction, _shell["Beat.ReplaceWavHint"]);
        };
        replaceAction.Click += async (_, _) =>
        {
            if (_disposed || !IsEffectivelyVisible || replaceBasis is not { } basis ||
                TopLevel.GetTopLevel(this) is not { } top) return;
            CancelGesture();
            IReadOnlyList<IStorageFile> files = [];
            try
            {
                files = await top.StorageProvider.OpenFilePickerAsync(new()
                {
                    Title = _shell["Beat.ReplaceWav"], AllowMultiple = false,
                    FileTypeFilter = [new("WAV") { Patterns = ["*.wav"] }]
                });
                if (files.FirstOrDefault()?.TryGetLocalPath() is { } path && !_disposed)
                {
                    PendingCommand = _workflow.ReplaceBeatWavAsync(basis, part.Id, path);
                    await PendingCommand;
                }
            }
            catch (Exception error)
            {
                if (!_disposed) await _workflow.ReportFailureAsync(error.Message);
            }
            finally { foreach (var file in files) file.Dispose(); }
            Refresh();
        };
        var mixerAction = new MenuItem { Header = _shell["Pane.Mixer"] };
        _menuLabels.Add((mixerAction, "Pane.Mixer"));
        AutomationProperties.SetAutomationId(mixerAction, $"Beat.Mixer.{part.Id}");
        mixerAction.Click += (_, _) =>
        {
            CancelGesture();
            MixerRequested?.Invoke(part.SoundId);
        };
        menu.Items.Add(mixerAction);
        soundAction.Click += (_, _) =>
        {
            CancelGesture();
            SoundRequested?.Invoke(part.Id);
        };
        pianoAction.Click += (_, _) =>
        {
            CancelGesture();
            PianoRequested?.Invoke(part.Id);
        };
        label.PointerPressed += (_, e) =>
        {
            if (e.ClickCount == 2)
            {
                CancelGesture();
                var sound = _workflow.State.Snapshot.State.Sounds.Single(s => s.Id == part.SoundId);
                if (DrumVoice.Supports(sound) || SynthVoice.Supports(sound)) SoundRequested?.Invoke(part.Id);
                else PianoRequested?.Invoke(part.Id);
                e.Handled = true;
            }
        };
        ToolTip.SetTip(more, _shell["Beat.ChannelActions"]);
        AutomationProperties.SetName(more, _shell["Beat.ChannelActions"] + " · " + part.Name);
        AutomationProperties.SetAutomationId(more, $"Beat.Actions.{part.Id}");
        foreach (string key in new[] { "Fill4", "Fill2", "ShiftLeft", "ShiftRight", "Clear", "DeleteChannel" })
        {
            var item = new MenuItem { Header = _shell["Beat." + key] };
            AutomationProperties.SetAutomationId(item, $"Beat.{key}.{part.Id}");
            _menuLabels.Add((item, "Beat." + key));
            // Freeze edit authority when the menu opens; intervening edits must not retarget an action.
            BeatEditBasis? menuBasis = null;
            menu.Opened += (_, _) =>
            {
                menuBasis = _basis;
                item.IsEnabled = _basis is not null && (key == "DeleteChannel" ||
                                                        _channels.FirstOrDefault(c => c.Part == part.Id)?.Steps
                                                            .All(b => b.IsEnabled) == true);
            };
            item.Click += async (_, _) =>
            {
                if (_disposed || !IsEffectivelyVisible || menuBasis is not { } basis) return;
                var current = _channels.FirstOrDefault(c => c.Part == part.Id);
                if (current is null) return;
                bool[] before = current.Steps.Select(b => b.IsChecked == true).ToArray();
                if (key == "DeleteChannel")
                {
                    CancelGesture();
                    PendingCommand = _workflow.DeleteBeatChannelAsync(basis, part.Id);
                    await PendingCommand;
                    Refresh();
                    return;
                }

                bool[] desired = Enumerable.Range(0, before.Length).Select(i => key switch
                {
                    "Fill4" => i % 4 == 0, "Fill2" => i % 2 == 0,
                    "ShiftLeft" => before[(i + 1) % before.Length],
                    "ShiftRight" => before[(i + before.Length - 1) % before.Length], _ => false
                }).ToArray();
                CancelGesture();
                PendingCommand = _workflow.SetBeatStepsAsync(basis, part.Id, desired);
                await PendingCommand;
                Refresh();
            };
            menu.Items.Add(item);
        }

        row.Children.Add(identity);
        var buttons = new ToggleButton[_stepCount];
        var groups = StepGroups((step) =>
        {
            var button = new ToggleButton
                { Classes = { "step", $"channel{index % 8}" }, HorizontalAlignment = HorizontalAlignment.Stretch };
            AutomationProperties.SetAutomationId(button, $"Beat.Step.{part.Id}.{step}");
            ToolTip.SetShowOnDisabled(button, true);
            button.AddHandler(PointerPressedEvent, (_, _) => Arm(button), RoutingStrategies.Tunnel);
            button.AddHandler(KeyDownEvent, (_, args) =>
            {
                if (args.Key is Key.Enter or Key.Space) Arm(button);
                if (args.KeyModifiers != KeyModifiers.None) return;
                int next = args.Key == Key.Left ? step - 1 :
                    args.Key == Key.Right ? step + 1 :
                    args.Key == Key.Home ? 0 :
                    args.Key == Key.End ? _stepCount - 1 : step;
                int currentRow = _channels.FindIndex(c => c.Part == part.Id);
                int nextRow = args.Key == Key.Up ? currentRow - 1 : args.Key == Key.Down ? currentRow + 1 : currentRow;
                if ((next != step || nextRow != currentRow) && next >= 0 && next < _stepCount && nextRow >= 0 &&
                    nextRow < _channels.Count)
                {
                    _channels[nextRow].Steps[next].Focus();
                    args.Handled = true;
                }
            }, RoutingStrategies.Tunnel);
            button.Click += async (_, _) =>
            {
                if (_refreshing || _disposed || !button.IsEnabled || !button.IsEffectivelyVisible ||
                    _expired.Remove(button))
                {
                    Refresh();
                    return;
                }

                if (!_channels.Any(c => c.Steps.Contains(button)))
                {
                    Refresh();
                    return;
                }

                // Accessibility invocation has no preceding pointer/key press. It resolves this
                // live retained control; a cancelled held gesture may never use that fallback.
                var basis = _armed.Remove(button, out var armed) ? armed : _basis;
                if (basis is null)
                {
                    Refresh();
                    return;
                }

                PendingCommand = _workflow.ToggleBeatStepAsync(basis, part.Id, step);
                await PendingCommand;
                Refresh();
            };
            buttons[step] = button;
            return button;
        });
        SetColumn(groups, 1);
        row.Children.Add(groups);
        var preview = new Button
            { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new(0, 4, 8, 4), IsVisible = false };
        AutomationProperties.SetAutomationId(preview, $"Beat.Melody.{part.Id}");
        AutomationProperties.SetName(preview, part.Name + " · " + _shell["Piano.Open"]);
        preview.Click += (_, _) =>
        {
            CancelGesture();
            PianoRequested?.Invoke(part.Id);
        };
        SetColumn(preview, 1);
        row.Children.Add(preview);
        _melodies.Add(part.Id, (groups, preview));
        var channel = new Channel(part.Id, label, warning, buttons);
        _channels.Add(channel);
        row.AddHandler(PointerPressedEvent, (_, e) => BeginPaint(channel, row, e), RoutingStrategies.Tunnel);
        row.PointerMoved += (_, e) => MovePaint(e);
        row.PointerReleased += async (_, e) => await EndPaintAsync(e);
        row.PointerCaptureLost += (_, e) =>
        {
            if (_paint?.Pointer == e.Pointer)
            {
                CancelGesture();
                Refresh();
            }
        };
        var body = new StackPanel { Classes = { "tight" } };
        body.Children.Add(row);
        body.Children.Add(warning);
        _rows.Children.Add(body);
    }

    private static Grid RackRow() => new() { ColumnDefinitions = new("210,*") };

    private void RebuildRuler()
    {
        if (_ruler.Children.Count > 1) _ruler.Children.RemoveAt(1);
        _rulerCells.Clear();
        var cells = StepGroups(step =>
        {
            var text = new TextBlock
            {
                Text = step % 4 == 0 ? $"{step / 16 + 1}.{step % 16 / 4 + 1}" : new[] { "", "e", "&", "a" }[step % 4],
                Classes = { "caption" }, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = step % 4 == 0 ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal
            };
            text.Bind(WidthProperty, this.GetResourceObservable("Rack.StepHeight"));
            _rulerCells.Add(text);
            return text;
        });
        SetColumn(cells, 1);
        _ruler.Children.Add(cells);
    }

    private Grid StepGroups(Func<int, Control> create)
    {
        var groups = new Grid
        {
            ColumnDefinitions = new(string.Join(",", Enumerable.Repeat("Auto", _stepCount / 4))),
            Margin = new(0, 0, 8, 0), HorizontalAlignment = HorizontalAlignment.Left
        };
        groups.Bind(ColumnSpacingProperty, this.GetResourceObservable("Space.Large"));
        for (int beat = 0; beat < _stepCount / 4; beat++)
        {
            var group = new Grid { ColumnDefinitions = new("Auto,Auto,Auto,Auto") };
            group.Bind(ColumnSpacingProperty, this.GetResourceObservable("Space.Small"));
            if (beat % 4 == 3 && beat + 1 < _stepCount / 4)
                group.Bind(MarginProperty, this.GetResourceObservable("Rack.BarPadding"));
            SetColumn(group, beat);
            groups.Children.Add(group);
            for (int offset = 0; offset < 4; offset++)
            {
                var cell = create(beat * 4 + offset);
                SetColumn(cell, offset);
                group.Children.Add(cell);
            }
        }

        return groups;
    }

    private void BeginPaint(Channel channel, Control capture, PointerPressedEventArgs e)
    {
        if (_disposed || _basis is not { } basis || !PendingCommand.IsCompleted ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var button = (e.Source as Visual)?.GetVisualAncestors().Prepend(e.Source as Visual).OfType<ToggleButton>()
            .FirstOrDefault();
        int step = Array.IndexOf(channel.Steps, button);
        if (step < 0 || !channel.Steps[step].IsEnabled) return;
        CancelGesture();
        Arm(channel.Steps[step]);
        bool[] values = channel.Steps.Select(b => b.IsChecked == true).ToArray();
        bool value = !values[step];
        values[step] = value;
        _paint = (channel, basis, values, value, step, e.Pointer);
        channel.Steps[step].IsChecked = value;
        channel.Steps[step].Focus();
        e.Pointer.Capture(capture);
        e.Handled = true;
    }

    private void MovePaint(PointerEventArgs e)
    {
        if (_paint is not { } paint || paint.Pointer != e.Pointer) return;
        for (int step = 0; step < paint.Row.Steps.Length; step++)
        {
            var button = paint.Row.Steps[step];
            var p = e.GetPosition(button);
            if (!new Rect(button.Bounds.Size).Contains(p)) continue;
            for (int i = Math.Min(paint.Last, step); i <= Math.Max(paint.Last, step); i++)
            {
                paint.Values[i] = paint.Value;
                paint.Row.Steps[i].IsChecked = paint.Value;
            }

            _paint = (paint.Row, paint.Basis, paint.Values, paint.Value, step, paint.Pointer);
            break;
        }

        e.Handled = true;
    }

    private async Task EndPaintAsync(PointerReleasedEventArgs e)
    {
        if (_paint is not { } paint || paint.Pointer != e.Pointer ||
            e.InitialPressMouseButton != MouseButton.Left) return;
        _paint = null;
        _armed.Clear();
        paint.Pointer.Capture(null);
        e.Handled = true;
        PendingCommand = _workflow.SetBeatStepsAsync(paint.Basis, paint.Row.Part, paint.Values);
        await PendingCommand;
        Refresh();
    }

    private void Arm(ToggleButton button)
    {
        if (!_disposed && button.IsEnabled && _basis is { } basis)
        {
            _expired.Remove(button);
            _armed[button] = basis;
        }
    }

    private async Task CommitNameAsync()
    {
        if (_nameBasis is not { } basis) return;
        PendingCommand = _workflow.RenameBeatAsync(basis, _name.Text ?? "");
        await PendingCommand;
        Refresh();
    }

    public void CancelGesture()
    {
        var pointer = _paint?.Pointer;
        _paint = null;
        pointer?.Capture(null);
        _expired.UnionWith(_armed.Keys);
        _armed.Clear();
        _nameBasis = null;
    }

    internal void Suspend()
    {
        _interaction++;
        CancelGesture();
    }

    public void FocusEditor()
    {
        if (_disposed || !IsEffectivelyVisible) return;
        if (_channels.FirstOrDefault()?.Steps.FirstOrDefault(b => b.IsEnabled) is { } step) step.Focus();
        else _create.Focus();
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
