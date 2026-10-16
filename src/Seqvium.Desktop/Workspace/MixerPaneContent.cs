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
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>One retained view over canonical Mixer state. It owns no audio or separate musical history.</summary>
internal sealed class MixerPaneContent : Grid, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private WorkflowState _state;
    private readonly StackPanel _bank = new() { Orientation = Orientation.Horizontal, Spacing = 0 };
    private readonly ContentControl _master = new();
    private readonly ContentControl _detail = new();

    private readonly Border _detailSide = new()
        { Width = 324, IsVisible = false, Margin = new(6, 0), BorderThickness = new(1, 0, 0, 0) };

    private readonly ToggleButton _disclosure = new()
    {
        IsChecked = false, HorizontalAlignment = HorizontalAlignment.Left,
        VerticalContentAlignment = VerticalAlignment.Center, Height = HostMetrics.Values["Pane.HeaderHeight"]
    };

    private readonly Button _initialize = new(), _bus = new(), _ceiling = new(), _reset = new();
    private readonly TextBlock _message = new() { Classes = { "secondary" }, Margin = new(12) };
    private readonly List<Action> _refreshers = [];
    private readonly List<MixerNumberEntry> _fields = [];
    private readonly List<(Id<MixerChannel>? Id, MixerFader Fader, TextBlock Peak)> _meters = [];
    private Guid _lifecycle;
    private Id<MixerChannel>? _selected;
    private Id<MixerChannel>[] _ids = [];
    private bool _disposed, _refreshing;
    private bool _hadMixer;
    private int _stripFields;
    private long _detailGeneration = -1;
    private HostLanguage _detailLanguage;
    internal Task PendingCommand { get; private set; } = Task.CompletedTask;

    internal void FocusEditor()
    {
        if (_initialize.IsVisible) _initialize.Focus();
        else _bus.Focus();
    }

    internal void CancelGesture()
    {
        foreach (var meter in _meters) meter.Fader.Cancel();
        foreach (var dial in this.GetVisualDescendants().OfType<GainDialControl>()) dial.Cancel();
        foreach (var plot in this.GetVisualDescendants().OfType<MixerResponsePlot>()) plot.Cancel();
        foreach (var field in _fields) field.Retire();
    }

    internal void OpenSound(Id<SoundDefinition> sound)
    {
        _selected = _state.Snapshot.State.Mixer?.Channels.SingleOrDefault(c => c.SoundIds.Contains(sound))?.Id;
        if (_state.Snapshot.State.Mixer is not null)
        {
            BuildDetail();
            _disclosure.IsChecked = true;
        }

        Refresh();
        RevealSelection();
    }

    internal MixerPaneContent(ShellSession shell, DesktopWorkflow workflow)
    {
        _shell = shell;
        _workflow = workflow;
        _state = workflow.State;
        RowDefinitions = new("Auto,*,Auto");
        var toolbar = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new(6, 4) };
        toolbar.Children.Add(_initialize);
        toolbar.Children.Add(_bus);
        toolbar.Children.Add(_ceiling);
        toolbar.Children.Add(_reset);
        _reset.Click += async (_, _) => await _workflow.ResetMixerMetersAsync();
        AutomationProperties.SetAutomationId(_reset, "Mixer.ResetMeters");
        Children.Add(toolbar);
        var body = new Grid { ColumnDefinitions = new("*,Auto,Auto"), Margin = new(5, 0) };
        var scroll = new ScrollViewer
        {
            Content = _bank, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        body.Children.Add(scroll);
        var masterScroll = new ScrollViewer
        {
            Content = _master, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        body.Children.Add(masterScroll);
        SetColumn(masterScroll, 1);
        Children.Add(body);
        SetRow(body, 1);
        _detailSide.Child = new ScrollViewer
        {
            Content = _detail,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        _detailSide.Bind(Border.BorderBrushProperty, _detailSide.GetResourceObservable("Border.Default"));
        body.Children.Add(_detailSide);
        SetColumn(_detailSide, 2);
        _disclosure.PropertyChanged += (_, e) =>
        {
            if (e.Property != ToggleButton.IsCheckedProperty) return;
            _detailSide.IsVisible = _disclosure.IsChecked == true;
            RevealSelection();
        };
        Children.Add(_disclosure);
        SetRow(_disclosure, 2);
        _initialize.Click += (_, _) => Queue(_state, "Initialize Mixer", edit => edit.InitializeMixer());
        _bus.Click += (_, _) => Queue(_state, "Add Mixer bus", edit => edit.AddMixerBus("Bus " + (_ids.Length + 1)));
        _ceiling.Click += (_, _) =>
        {
            var state = _state;
            var mixer = state.Snapshot.State.Mixer;
            if (mixer is not null)
                Queue(state, "Change Master peak ceiling",
                    edit => edit.SetMixer(mixer with { ProtectOutput = !mixer.ProtectOutput }));
        };
        AutomationProperties.SetAutomationId(_initialize, "Mixer.Initialize");
        AutomationProperties.SetAutomationId(_bus, "Mixer.AddBus");
        AutomationProperties.SetAutomationId(_ceiling, "Mixer.Ceiling");
        workflow.Changed += Changed;
        shell.PropertyChanged += PreferencesChanged;
        Refresh();
    }

    private void Changed(WorkflowState state) => Dispatcher.UIThread.Post(() =>
    {
        if (_disposed || state != _workflow.State) return;
        _state = state;
        Refresh();
    });

    private void PreferencesChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    private void RevealSelection() => Dispatcher.UIThread.Post(() =>
    {
        if (_disposed || !_detailSide.IsVisible || _selected is null) return;
        // Wait for the side inspector's layout before keeping its source strip in the reduced bank viewport.
        this.GetVisualDescendants().OfType<Button>().FirstOrDefault(b =>
            AutomationProperties.GetAutomationId(b) == "Mixer." + Key(_selected) + ".Select")?.BringIntoView();
    }, DispatcherPriority.Loaded);

    private MixerStripSettings Settings(WorkflowState state, Id<MixerChannel>? id) => id is { } channel
        ? state.Snapshot.State.Mixer!.Channels.Single(c => c.Id == channel).Settings
        : state.Snapshot.State.Mixer!.Master;

    private static string Key(Id<MixerChannel>? id) => id?.ToString() ?? "Master";
    private MixerEditBasis Basis(WorkflowState state) => new(state.Document.LifecycleId, state.Generation);

    private async void Queue(WorkflowState state, string description, Action<ProjectEdit> action)
    {
        if (_disposed) return;
        PendingCommand = _workflow.EditMixerAsync(Basis(state), description, action);
        await PendingCommand;
    }

    private void StripEdit(WorkflowState state, Id<MixerChannel>? id,
        Func<MixerStripSettings, MixerStripSettings> change)
    {
        var settings = change(Settings(state, id));
        Queue(state, "Change Mixer strip", edit => edit.SetMixerStrip(id, settings));
    }

    private void Refresh()
    {
        if (_disposed) return;
        _refreshing = true;
        try
        {
            var mixer = _state.Snapshot.State.Mixer;
            var ids = mixer?.Channels.Select(c => c.Id).ToArray() ?? [];
            if (_lifecycle != _state.Document.LifecycleId || !_ids.SequenceEqual(ids) ||
                _hadMixer != (mixer is not null))
            {
                _lifecycle = _state.Document.LifecycleId;
                _ids = ids;
                _hadMixer = mixer is not null;
                if (_selected is { } selected && !ids.Contains(selected)) _selected = null;
                _bank.Children.Clear();
                _master.Content = null;
                foreach (var field in _fields) field.Retire();
                _fields.Clear();
                _refreshers.Clear();
                _meters.Clear();
                if (mixer is null) _bank.Children.Add(_message);
                else
                {
                    foreach (var channel in mixer.Channels) _bank.Children.Add(Strip(channel.Id));
                    _master.Content = Strip(null);
                    _stripFields = _fields.Count;
                    BuildDetail();
                }
            }

            _initialize.Content = _shell["Mixer.Initialize"];
            _initialize.IsVisible = mixer is null;
            _message.Text = _shell["Mixer.Empty"];
            _bus.Content = _shell["Mixer.Bus"];
            _bus.IsVisible = mixer is not null;
            _bus.IsEnabled = !_state.Busy && ids.Length < MixerValidation.MaximumChannels;
            _ceiling.Content = _shell["Mixer.Ceiling"] + (mixer?.ProtectOutput == true ? " ●" : " ○");
            _ceiling.IsVisible = mixer is not null;
            _ceiling.IsEnabled = !_state.Busy;
            _reset.Content = _shell["Mixer.ResetMeters"];
            _reset.IsVisible = mixer is not null;
            _disclosure.IsVisible = mixer is not null;
            if (mixer is not null &&
                (_detailGeneration != _state.Generation || _detailLanguage != _shell.Preferences.Language))
                BuildDetail();
            _disclosure.Content = mixer is null
                ? ""
                : (_selected is { } selectedId ? mixer.Channels.Single(c => c.Id == selectedId).Name : "Master") +
                  " · " +
                  _shell["Mixer.Processing"];
            foreach (var refresh in _refreshers) refresh();
            foreach (var field in _fields) field.Refresh(_state);
            foreach (var (id, fader, peak) in _meters)
            {
                fader.Meter = _state.MixerMeters.GetValueOrDefault(id?.Value ?? Guid.Empty);
                fader.Playing = _state.Execution?.Execution.Playing == true;
                fader.InvalidateVisual();
                peak.IsVisible = fader.Meter.Overloads > 0 || fader.Meter.NonFiniteSamples > 0;
                peak.Text = "CLIP";
                ToolTip.SetTip(fader, FormattableString.Invariant(
                    $"Peak {(fader.Meter.HeldPeak > 0 ? 20 * Math.Log10(fader.Meter.HeldPeak) : -90):0.0} dBFS · GR {fader.Meter.ReductionDb:0.0} dB"));
                peak.Classes.Set("error", fader.Meter.Overloads > 0 || fader.Meter.NonFiniteSamples > 0);
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private Border Strip(Id<MixerChannel>? id)
    {
        string key = Key(id);
        var panel = new Grid
        {
            RowDefinitions = new("Auto,Auto,Auto,Auto,*,Auto,Auto"),
            Width = HostMetrics.Values[id is null ? "Mixer.MasterWidth" : "Mixer.ChannelWidth"],
            MinHeight = HostMetrics.Values["Mixer.StripHeight"], MaxHeight = HostMetrics.Values["Mixer.StripHeight"],
            VerticalAlignment = VerticalAlignment.Top, Margin = new(2), Classes = { "tight" }
        };
        var border = new Border
        {
            Child = panel, Margin = new(1), CornerRadius = new(2), BorderThickness = new(1),
            VerticalAlignment = VerticalAlignment.Top
        };
        border.Bind(BackgroundProperty, border.GetResourceObservable("Material.MixerChannel"));
        border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("Border.Default"));
        var nameLabel = new TextBlock
        {
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis, FontSize = HostMetrics.Values["Type.Caption"]
        };
        var name = new Button
            { Content = nameLabel, HorizontalAlignment = HorizontalAlignment.Stretch, Classes = { "mixerControl" } };
        AutomationProperties.SetAutomationId(name, "Mixer." + key + ".Select");
        name.Click += (_, _) =>
        {
            _selected = id;
            BuildDetail();
            _disclosure.IsChecked = true;
            Refresh();
        };
        var identity = new Border { Height = 2 };
        var identityGroup = new StackPanel { Spacing = 2 };
        identityGroup.Children.Add(identity);
        identityGroup.Children.Add(name);
        panel.Children.Add(identityGroup);
        var pan = new GainDialControl(_shell)
        {
            Width = HostMetrics.Values["Mixer.PanSize"], Height = HostMetrics.Values["Mixer.PanSize"], Focusable = true,
            DefaultAmplitude = .5m, Compact = true,
            HasRecessedWell = true, HorizontalAlignment = HorizontalAlignment.Center
        };
        AutomationProperties.SetAutomationId(pan, "Mixer." + key + ".PanDial");
        var panField = Number(id, "Pan", -1, 1, s => s.Pan, (s, v) => s with { Pan = v }, "Mixer." + key + ".Pan");
        var panGroup = new StackPanel { Spacing = 0 };
        panGroup.Children.Add(pan);
        panGroup.Children.Add(panField);
        panel.Children.Add(panGroup);
        SetRow(panGroup, 1);
        WorkflowState? panBasis = null;
        decimal panValue = 0;
        pan.Begin = () =>
        {
            if (_state.Busy) return false;
            panBasis = _state;
            panValue = Settings(_state, id).Pan;
            return true;
        };
        pan.Preview += v =>
        {
            panValue = decimal.Round(v * 2 - 1, 3);
            pan.Amplitude = v;
            pan.CenterText = panValue == 0 ? "C" : (panValue < 0 ? "L" : "R");
            pan.InvalidateVisual();
        };
        pan.Completed += () =>
        {
            if (panBasis is { } basis) StripEdit(basis, id, s => s with { Pan = panValue });
            panBasis = null;
        };
        pan.Cancelled += () =>
        {
            panBasis = null;
            Refresh();
        };
        var switches = new Grid { ColumnDefinitions = new("*,*"), Margin = new(0, 1), ColumnSpacing = 2 };
        var mute = new Button
            { Content = "M", HorizontalAlignment = HorizontalAlignment.Stretch, Classes = { "mixerControl" } };
        var solo = new Button
            { Content = "S", HorizontalAlignment = HorizontalAlignment.Stretch, Classes = { "mixerControl" } };
        switches.Children.Add(mute);
        switches.Children.Add(solo);
        SetColumn(solo, 1);
        AutomationProperties.SetAutomationId(mute, "Mixer." + key + ".Mute");
        AutomationProperties.SetAutomationId(solo, "Mixer." + key + ".Solo");
        mute.Click += (_, _) => StripEdit(_state, id, s => s with { Muted = !s.Muted });
        solo.Click += (_, _) => StripEdit(_state, id, s => s with { Solo = !s.Solo });
        solo.IsVisible = id is not null;
        panel.Children.Add(switches);
        SetRow(switches, 2);
        var effects = new Button { Classes = { "mixerControl" }, HorizontalAlignment = HorizontalAlignment.Stretch };
        effects.Click += (_, _) =>
        {
            _selected = id;
            BuildDetail();
            _disclosure.IsChecked = true;
            Refresh();
        };
        AutomationProperties.SetAutomationId(effects, "Mixer." + key + ".Effects");
        panel.Children.Add(effects);
        SetRow(effects, 3);
        var fader = new MixerFader(_shell)
            { MinHeight = HostMetrics.Values["Mixer.FaderHeight"], ShowScale = id is null };
        AutomationProperties.SetAutomationId(fader, "Mixer." + key + ".Fader");
        WorkflowState? faderBasis = null;
        decimal faderValue = 0;
        fader.Begin = () =>
        {
            if (_state.Busy) return false;
            faderBasis = _state;
            return true;
        };
        fader.Preview += v =>
        {
            faderValue = v;
            fader.LevelDb = v;
            fader.InvalidateVisual();
        };
        fader.Completed += () =>
        {
            if (faderBasis is { } basis) StripEdit(basis, id, s => s with { LevelDb = faderValue });
            faderBasis = null;
        };
        fader.Cancelled += () =>
        {
            faderBasis = null;
            Refresh();
        };
        panel.Children.Add(fader);
        SetRow(fader, 4);
        var level = Number(id, "dB", -90, 12, s => s.LevelDb, (s, v) => s with { LevelDb = v },
            "Mixer." + key + ".Level");
        panel.Children.Add(level);
        SetRow(level, 5);
        var peak = new TextBlock
        {
            Classes = { "caption", "numeric" }, FontSize = 10, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new(3, 4)
        };
        panel.Children.Add(peak);
        SetRow(peak, 6);
        _meters.Add((id, fader, peak));
        _refreshers.Add(() =>
        {
            var mixer = _state.Snapshot.State.Mixer!;
            var channel = id is { } cid ? mixer.Channels.Single(c => c.Id == cid) : null;
            var settings = Settings(_state, id);
            string role = channel is null ? "Accent.Action" : "Rack.Channel" + channel.ColorIndex;
            nameLabel.Text = channel?.Name ?? "Master";
            identity.Bind(Border.BackgroundProperty, identity.GetResourceObservable(role));
            name.Classes.Set("primary", id == _selected);
            ToolTip.SetTip(name,
                channel is null
                    ? _shell["Mixer.Master"]
                    : $"{channel.Name} · {channel.SoundIds.Length} · " + _shell["Mixer.Sources"]);
            AutomationProperties.SetName(mute, _shell["Mixer.Mute"]);
            AutomationProperties.SetName(solo, _shell["Mixer.Solo"]);
            mute.Classes.Set("primary", settings.Muted);
            solo.Classes.Set("primary", settings.Solo);
            if (panBasis is not null && panBasis.Generation != _state.Generation) pan.Cancel();
            if (!pan.HasGesture)
            {
                pan.Amplitude = (settings.Pan + 1) / 2;
                pan.CenterText = settings.Pan == 0 ? "C" : settings.Pan < 0 ? "L" : "R";
            }

            pan.AccentRole = role;
            pan.InvalidateVisual();
            if (faderBasis is not null && faderBasis.Generation != _state.Generation) fader.Cancel();
            if (!fader.HasGesture) fader.LevelDb = settings.LevelDb;
            fader.AccentRole = role;
            effects.Content = (settings.Eq.Enabled ? "EQ·" : "EQ") + " " + (settings.Compressor.Enabled ? "C·" : "C") +
                              (settings.Delay.Enabled ? " D·" : " D");
            ToolTip.SetTip(effects, _shell["Mixer.Processing"]);
            effects.Classes.Set("primary", settings.Eq.Enabled || settings.Compressor.Enabled || settings.Delay.Enabled);
            border.IsEnabled = !_state.Busy;
        });
        return border;
    }

    private Control Number(Id<MixerChannel>? id, string label, decimal min, decimal max,
        Func<MixerStripSettings, decimal> read, Func<MixerStripSettings, decimal, MixerStripSettings> change,
        string automation)
    {
        var field = new MixerNumberEntry(_workflow, state => read(Settings(state, id)),
            (state, value) => edit => edit.SetMixerStrip(id, change(Settings(state, id), value)), min, max);
        AutomationProperties.SetAutomationId(field, automation);
        AutomationProperties.SetName(field, label);
        ToolTip.SetTip(field, label + $" [{min}; {max}]");
        _fields.Add(field);
        var row = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new(3, 1), Classes = { "parameterRow" } };
        row.Children.Add(field);
        var text = new TextBlock
            { Text = label, Classes = { "caption" }, VerticalAlignment = VerticalAlignment.Center, Margin = new(4, 0) };
        row.Children.Add(text);
        if (label is "Pan" or "dB")
        {
            field.Classes.Add("mixerValue");
            row.Classes.Add("mixerValueRow");
            field.TextAlignment = Avalonia.Media.TextAlignment.Center;
            text.IsVisible = false;
        }

        SetColumn(text, 1);
        return row;
    }

    private void BuildDetail()
    {
        int tabIndex = (_detail.Content as TabControl)?.SelectedIndex ?? 0;
        foreach (var field in _fields.Skip(_stripFields)) field.Retire();
        if (_fields.Count > _stripFields) _fields.RemoveRange(_stripFields, _fields.Count - _stripFields);
        _detailGeneration = _state.Generation;
        _detailLanguage = _shell.Preferences.Language;
        // Strip refreshers are retained; contextual editors retire their own value sessions on target changes.
        var id = _selected;
        string key = Key(id);
        var settings = Settings(_state, id);
        var tabs = new TabControl { Margin = new(8) };
        var eq = new StackPanel { Spacing = 6 };
        var compressor = new StackPanel { Spacing = 6 };
        var eqPlot = new MixerResponsePlot(_shell)
            { Settings = settings, Height = 156, SampleRate = _state.ExecutionSampleRate };
        AutomationProperties.SetAutomationId(eqPlot, "Mixer.Detail.EqPlot");
        WorkflowState? eqBasis = null;
        MixerEq eqDraft = settings.Eq;
        eqPlot.Begin = () =>
        {
            if (_state.Busy) return false;
            eqBasis = _state;
            return true;
        };
        eqPlot.Preview += value => eqDraft = value;
        eqPlot.Completed += () =>
        {
            if (eqBasis is { } state) StripEdit(state, id, s => s with { Eq = eqDraft });
            eqBasis = null;
        };
        eqPlot.Cancelled += () => eqBasis = null;
        ToolTip.SetTip(eqPlot, _shell["Mixer.Response"]);
        var compPlot = new MixerResponsePlot(_shell) { Settings = settings, Compression = true, Height = 128 };
        eq.Children.Add(eqPlot);
        compressor.Children.Add(compPlot);
        var eqEnable = new Button { Content = _shell["Mixer.Eq"] + (settings.Eq.Enabled ? " ●" : " ○") };
        eqEnable.Click += (_, _) =>
        {
            StripEdit(_state, id, s => s with { Eq = s.Eq with { Enabled = !s.Eq.Enabled } });
        };
        AutomationProperties.SetAutomationId(eqEnable, "Mixer.Detail.EqEnable");
        eq.Children.Add(eqEnable);
        var eqFields = new WrapPanel { Orientation = Orientation.Horizontal };

        void EqValue(string label, decimal min, decimal max, Func<MixerEq, decimal> read,
            Func<MixerEq, decimal, MixerEq> change)
        {
            var control = Number(id, label, min, max, s => read(s.Eq), (s, v) => s with { Eq = change(s.Eq, v) },
                "Mixer.Detail." + label);
            control.Width = 120;
            eqFields.Children.Add(control);
        }

        EqValue("HP Hz", 0, 2000, e => e.HighPassHz, (e, v) => e with { HighPassHz = v == 0 ? 0 : Math.Max(20, v) });
        EqValue("Low dB", -18, 18, e => e.LowDb, (e, v) => e with { LowDb = v });
        EqValue("Mid Hz", 40, 16000, e => e.MidHz, (e, v) => e with { MidHz = v });
        EqValue("Mid dB", -18, 18, e => e.MidDb, (e, v) => e with { MidDb = v });
        EqValue("Q", .2m, 10, e => e.MidQ, (e, v) => e with { MidQ = v });
        EqValue("High dB", -18, 18, e => e.HighDb, (e, v) => e with { HighDb = v });
        eq.Children.Add(eqFields);
        var compEnable = new Button { Content = _shell["Mixer.Comp"] + (settings.Compressor.Enabled ? " ●" : " ○") };
        compEnable.Click += (_, _) => StripEdit(_state, id,
            s => s with { Compressor = s.Compressor with { Enabled = !s.Compressor.Enabled } });
        AutomationProperties.SetAutomationId(compEnable, "Mixer.Detail.CompEnable");
        compressor.Children.Add(compEnable);
        var compFields = new WrapPanel { Orientation = Orientation.Horizontal };

        void CompValue(string label, decimal min, decimal max, Func<MixerCompressor, decimal> read,
            Func<MixerCompressor, decimal, MixerCompressor> change)
        {
            var control = Number(id, label, min, max, s => read(s.Compressor),
                (s, v) => s with { Compressor = change(s.Compressor, v) }, "Mixer.Detail." + label);
            control.Width = 120;
            compFields.Children.Add(control);
        }

        CompValue("Threshold", -60, 0, c => c.ThresholdDb, (c, v) => c with { ThresholdDb = v });
        CompValue("Ratio", 1, 20, c => c.Ratio, (c, v) => c with { Ratio = v });
        CompValue("Attack ms", .1m, 200, c => c.AttackMs, (c, v) => c with { AttackMs = v });
        CompValue("Release ms", 10, 2000, c => c.ReleaseMs, (c, v) => c with { ReleaseMs = v });
        CompValue("Knee dB", 0, 24, c => c.KneeDb, (c, v) => c with { KneeDb = v });
        CompValue("Makeup dB", -12, 24, c => c.MakeupDb, (c, v) => c with { MakeupDb = v });
        compressor.Children.Add(compFields);
        var delay = new StackPanel { Spacing = HostMetrics.Values["Space.Small"] };
        var delayEnable = new ToggleButton
        {
            Content = _shell["Mixer.Enabled"], IsChecked = settings.Delay.Enabled,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        AutomationProperties.SetAutomationId(delayEnable, "Mixer.Detail.DelayEnable");
        delayEnable.Click += (_, _) => StripEdit(_state, id,
            s => s with { Delay = s.Delay with { Enabled = delayEnable.IsChecked == true } });
        delay.Children.Add(delayEnable);
        delay.Children.Add(Number(id, _shell["Mixer.DelayTime"], 10, 1000, s => s.Delay.TimeMs,
            (s, v) => s with { Delay = s.Delay with { TimeMs = v } }, "Mixer.Detail.DelayTime"));
        delay.Children.Add(Number(id, _shell["Mixer.DelayFeedback"], 0, 75, s => s.Delay.Feedback * 100,
            (s, v) => s with { Delay = s.Delay with { Feedback = v / 100 } }, "Mixer.Detail.DelayFeedback"));
        delay.Children.Add(Number(id, _shell["Mixer.DelayMix"], 0, 100, s => s.Delay.Mix * 100,
            (s, v) => s with { Delay = s.Delay with { Mix = v / 100 } }, "Mixer.Detail.DelayMix"));
        var routing = Routing(id);
        tabs.ItemsSource = new TabItem[]
        {
            new() { Header = _shell["Mixer.EqTab"], Content = eq },
            new() { Header = _shell["Mixer.CompTab"], Content = compressor },
            new() { Header = _shell["Mixer.Routing"], Content = routing },
            new() { Header = _shell["Mixer.Delay"], Content = delay }
        };
        AutomationProperties.SetAutomationId(tabs, "Mixer.Detail.Tabs");
        _detail.Content = tabs;
        tabs.SelectedIndex = tabIndex;
        // Plot/toggle refresh lives with the contextual content rather than subscriptions to retired tabs.
        eqPlot.Tag = key;
        compPlot.Tag = key;
    }

    private Control Routing(Id<MixerChannel>? id)
    {
        var panel = new StackPanel { Spacing = 8, Margin = new(8) };
        panel.Children.Add(Number(id, "Trim dB", -24, 36, s => s.TrimDb, (s, v) => s with { TrimDb = v },
            "Mixer.Detail.Trim"));
        if (id is not { } cid)
        {
            panel.Children.Add(new TextBlock { Text = _shell["Mixer.Master"] });
            return panel;
        }

        var mixer = _state.Snapshot.State.Mixer!;
        var channel = mixer.Channels.Single(c => c.Id == cid);
        var name = new TextBox { Text = channel.Name };
        AutomationProperties.SetAutomationId(name, "Mixer.Detail.Name");
        WorkflowState? renameBasis = null;
        name.GotFocus += (_, _) => renameBasis = _state;

        void Rename()
        {
            if (renameBasis is not { } state || name.Text == channel.Name) return;
            renameBasis = null;
            string newName = name.Text ?? "";
            var basisMixer = state.Snapshot.State.Mixer!;
            Queue(state, "Rename Mixer channel", edit => edit.SetMixer(basisMixer with
            {
                Channels = [.. basisMixer.Channels.Select(c => c.Id == cid ? c with { Name = newName } : c)]
            }));
        }

        name.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                Rename();
                e.Handled = true;
            }

            if (e.Key == Avalonia.Input.Key.Escape)
            {
                renameBasis = null;
                name.Text = channel.Name;
            }
        };
        name.LostFocus += (_, _) => Rename();
        panel.Children.Add(name);
        var choices = new[] { new RouteChoice(null, "Master") }
            .Concat(mixer.Channels.Where(c => c.Id != cid).Select(c => new RouteChoice(c.Id, c.Name))).ToArray();
        var output = new ComboBox
            { ItemsSource = choices, SelectedIndex = Array.FindIndex(choices, c => c.Id == channel.Output) };
        AutomationProperties.SetAutomationId(output, "Mixer.Detail.Output");
        panel.Children.Add(output);
        output.SelectionChanged += (_, _) =>
        {
            if (_refreshing || output.SelectedItem is not RouteChoice route) return;
            var state = _state;
            var m = state.Snapshot.State.Mixer!;
            if (m.Channels.Single(c => c.Id == cid).Output == route.Id) return;
            Queue(state, "Route Mixer output",
                edit => edit.SetMixer(m with
                {
                    Channels = [.. m.Channels.Select(c => c.Id == cid ? c with { Output = route.Id } : c)]
                }));
        };
        foreach (var send in channel.Sends)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            row.Children.Add(new TextBlock
            {
                Text = mixer.Channels.Single(c => c.Id == send.Target).Name, Width = 140,
                VerticalAlignment = VerticalAlignment.Center
            });
            var value = new MixerNumberEntry(_workflow,
                state => state.Snapshot.State.Mixer!.Channels.Single(c => c.Id == cid).Sends
                    .Single(s => s.Target == send.Target).LevelDb,
                (state, v) => edit =>
                {
                    var m = state.Snapshot.State.Mixer!;
                    edit.SetMixer(m with
                    {
                        Channels =
                        [
                            .. m.Channels.Select(c => c.Id == cid
                                ? c with
                                {
                                    Sends =
                                    [
                                        .. c.Sends.Select(s => s.Target == send.Target ? s with { LevelDb = v } : s)
                                    ]
                                }
                                : c)
                        ]
                    });
                }, -90, 12) { Width = 90 };
            AutomationProperties.SetAutomationId(value, "Mixer.Send." + send.Target);
            _fields.Add(value);
            row.Children.Add(value);
            var pre = new Button { Content = send.PreFader ? "Pre" : "Post" };
            pre.Click += (_, _) =>
            {
                var state = _state;
                var m = state.Snapshot.State.Mixer!;
                Queue(state, "Change Mixer send tap", edit => edit.SetMixer(m with
                {
                    Channels =
                    [
                        .. m.Channels.Select(c =>
                            c.Id == cid
                                ? c with
                                {
                                    Sends =
                                    [
                                        .. c.Sends.Select(s =>
                                            s.Target == send.Target ? s with { PreFader = !s.PreFader } : s)
                                    ]
                                }
                                : c)
                    ]
                }));
            };
            row.Children.Add(pre);
            var remove = new Button { Content = "×", Classes = { "danger" } };
            remove.Click += (_, _) =>
            {
                var state = _state;
                var m = state.Snapshot.State.Mixer!;
                Queue(state, "Remove Mixer send",
                    edit => edit.SetMixer(m with
                    {
                        Channels =
                        [
                            .. m.Channels.Select(c =>
                                c.Id == cid
                                    ? c with { Sends = [.. c.Sends.Where(s => s.Target != send.Target)] }
                                    : c)
                        ]
                    }));
            };
            row.Children.Add(remove);
            panel.Children.Add(row);
        }

        var sendTargets = mixer.Channels.Where(c => c.Id != cid && channel.Sends.All(s => s.Target != c.Id))
            .Select(c => new RouteChoice(c.Id, c.Name)).ToArray();
        var sendChoice = new ComboBox { ItemsSource = sendTargets, SelectedIndex = sendTargets.Length > 0 ? 0 : -1 };
        AutomationProperties.SetAutomationId(sendChoice, "Mixer.Detail.SendTarget");
        panel.Children.Add(sendChoice);
        var add = new Button
            { Content = _shell["Mixer.Send"], IsEnabled = channel.Sends.Length < 8 && sendTargets.Length > 0 };
        AutomationProperties.SetAutomationId(add, "Mixer.Detail.AddSend");
        add.Click += (_, _) =>
        {
            if (sendChoice.SelectedItem is not RouteChoice { Id: { } target }) return;
            var state = _state;
            var m = state.Snapshot.State.Mixer!;
            Queue(state, "Add Mixer send",
                edit => edit.SetMixer(m with
                {
                    Channels =
                    [
                        .. m.Channels.Select(c =>
                            c.Id == cid ? c with { Sends = c.Sends.Add(new(target, -18, false)) } : c)
                    ]
                }));
        };
        panel.Children.Add(add);
        panel.Children.Add(new TextBlock { Text = _shell["Mixer.Sources"], Classes = { "section" } });
        foreach (var sound in _state.Snapshot.State.Sounds)
        {
            var line = new Grid { ColumnDefinitions = new("*,180") };
            line.Children.Add(new TextBlock
            {
                Text = sound.Name, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            });
            var assignment = new ComboBox
            {
                ItemsSource = new[] { new RouteChoice(null, "Master") }
                    .Concat(mixer.Channels.Select(c => new RouteChoice(c.Id, c.Name))).ToArray()
            };
            var assigned = mixer.Channels.SingleOrDefault(c => c.SoundIds.Contains(sound.Id));
            assignment.SelectedIndex = assigned is null ? 0 : mixer.Channels.IndexOf(assigned) + 1;
            assignment.SelectionChanged += (_, _) =>
            {
                if (_refreshing || assignment.SelectedItem is not RouteChoice route) return;
                if (_state.Snapshot.State.Mixer!.Channels.SingleOrDefault(c => c.SoundIds.Contains(sound.Id))?.Id ==
                    route.Id) return;
                Queue(_state, "Assign Mixer source", edit => edit.AssignMixerSound(sound.Id, route.Id));
            };
            line.Children.Add(assignment);
            SetColumn(assignment, 1);
            panel.Children.Add(line);
        }

        var delete = new Button { Content = _shell["Mixer.Delete"], Classes = { "danger" } };
        delete.Click += (_, _) => Queue(_state, "Remove Mixer channel", edit => edit.RemoveMixerChannel(cid));
        panel.Children.Add(delete);
        return panel;
    }

    private sealed record RouteChoice(Id<MixerChannel>? Id, string Name)
    {
        public override string ToString() => Name;
    }

    public void Dispose()
    {
        _disposed = true;
        _workflow.Changed -= Changed;
        _shell.PropertyChanged -= PreferencesChanged;
    }
}

internal sealed class MixerNumberEntry : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    static MixerNumberEntry() => TextProperty.Changed.AddClassHandler<MixerNumberEntry>((control, _) =>
    {
        if (!control._refreshing && control.IsFocused) control._dirty = true;
    });

    internal void Retire()
    {
        _dirty = false;
        _basis = null;
    }

    private readonly DesktopWorkflow _workflow;
    private readonly Func<WorkflowState, decimal> _read;
    private readonly Func<WorkflowState, decimal, Action<ProjectEdit>> _change;
    private readonly decimal _min, _max;
    private WorkflowState? _basis;
    private bool _refreshing, _dirty;

    internal MixerNumberEntry(DesktopWorkflow workflow, Func<WorkflowState, decimal> read,
        Func<WorkflowState, decimal, Action<ProjectEdit>> change, decimal min, decimal max)
    {
        _workflow = workflow;
        _read = read;
        _change = change;
        _min = min;
        _max = max;
        Classes.Add("numeric");
        GotFocus += (_, _) => _basis = workflow.State;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Commit();
                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                _dirty = false;
                _basis = null;
                Refresh(workflow.State);
                e.Handled = true;
            }
        };
        LostFocus += (_, _) => Commit();
    }

    private async void Commit()
    {
        if (!_dirty || _basis is not { } state) return;
        if (!decimal.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < _min ||
            value > _max)
        {
            Classes.Add("error");
            return;
        }

        _dirty = false;
        _basis = null;
        Classes.Remove("error");
        await _workflow.EditMixerAsync(new(state.Document.LifecycleId, state.Generation), "Change Mixer parameter",
            _change(state, value));
    }

    internal void Refresh(WorkflowState state)
    {
        IsEnabled = !state.Busy;
        if (_dirty && _basis?.Document.LifecycleId == state.Document.LifecycleId &&
            _basis.Generation == state.Generation) return;
        _dirty = false;
        _basis = IsFocused ? state : null;
        _refreshing = true;
        try
        {
            Text = _read(state).ToString("0.###", CultureInfo.InvariantCulture);
            Classes.Remove("error");
        }
        finally
        {
            _refreshing = false;
        }
    }
}
