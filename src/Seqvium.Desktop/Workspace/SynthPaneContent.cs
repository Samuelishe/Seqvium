// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Retained shared-sound editor. Drafts carry target/generation authority; no DSP or musical event ownership.</summary>
internal sealed class SynthPaneContent : Grid, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;

    private readonly TextBlock _identity = new()
    {
        TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 220,
        VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new(0, 0, 12, 0)
    };

    private readonly TextBlock _releaseLabel = new();
    private readonly TextBlock _status = new() { Classes = { "warning", "caption" }, TextWrapping = TextWrapping.Wrap };
    private readonly ComboBox _presets = new() { MinWidth = 160 };
    private readonly ComboBox _note = new() { Width = 92, Height = 40, MaxDropDownHeight = 260 };
    private readonly SynthEnvelopePlot _envelope;
    private readonly List<(TextBlock Text, string Key)> _labels = [];

    private readonly Dictionary<string, (GainDialControl Dial, TextBox Field, SynthVoice.Parameter Parameter)>
        _parameters = [];

    private readonly Dictionary<string, TextBlock> _fieldErrors = [];
    private readonly List<(Button Button, string Key)> _actions = [];
    private readonly Border[] _oscillators = new Border[2];
    private readonly SynthWaveform[] _waves = new SynthWaveform[2];
    private readonly Button[,] _shapes = new Button[2, 4];
    private string? _feedbackKey;
    private SoundDefinition? _sound;
    private NodeEditorTarget? _target;
    private long _generation = -1;
    private ImmutableDictionary<string, decimal>? _draft;
    private bool _refreshing, _disposed, _held, _gateHeld, _closed;
    private HostPreferences? _displayedPreferences;
    private readonly HashSet<string> _dirtyFields = [];
    internal Task PendingCommand { get; private set; } = Task.CompletedTask;

    private sealed record Preview(
        NodeEditorTarget Target,
        long Generation,
        ImmutableDictionary<string, decimal>? Values);

    private Preview? _nextPreview;
    internal Task PendingPreview { get; private set; } = Task.CompletedTask;
    private ImmutableDictionary<string, decimal> Values => _draft ?? _sound?.Parameters ?? SynthVoice.Defaults;

    // Visual availability belongs to this complete observation. Reading a newer owner generation
    // midway through projection would briefly disable unrelated controls. Actions still revalidate.
    private bool Ready => !_disposed && !_closed && _target is not null && _sound is not null;

    internal SynthPaneContent(ShellSession shell, DesktopWorkflow workflow)
    {
        _shell = shell;
        _workflow = workflow;
        RowDefinitions = new("Auto,*,Auto");
        this.Bind(RowSpacingProperty, this.GetResourceObservable("Space.Gap"));
        var header = new Grid { ColumnDefinitions = new("*,Auto"), RowDefinitions = new("Auto,Auto") };
        header.Children.Add(_identity);
        header.Children.Add(_presets);
        SetColumn(_presets, 1);
        header.Children.Add(_status);
        SetRow(_status, 1);
        SetColumnSpan(_status, 2);
        Children.Add(header);
        Identify(_presets, "Presets");
        Identify(_identity, "Identity");
        Identify(_status, "Status");
        _presets.SelectionChanged += (_, _) =>
        {
            if (_refreshing || _presets.SelectedIndex <= 0) return;
            if (!Ready || !PendingCommand.IsCompleted || _dirtyFields.Count > 0)
            {
                Observe(_target);
                Feedback("Synth.FinishEdit");
                return;
            }

            _draft = SynthVoice.Presets[_presets.SelectedIndex - 1].Values;
            Run(CommitAsync);
        };
        var body = new Grid { RowDefinitions = new("Auto,Auto,*,Auto") };
        body.Bind(RowSpacingProperty, this.GetResourceObservable("Space.Section"));
        var oscillators = new WrapPanel { Orientation = Orientation.Horizontal };
        for (int oscillator = 0; oscillator < 2; oscillator++)
        {
            int index = oscillator;
            var card = new StackPanel { Classes = { "tight" } };
            card.Children.Add(Label("Osc" + (index + 1), "section"));
            var choices = new Grid { ColumnDefinitions = new("*,*,*,*"), ColumnSpacing = 2 };
            for (int wave = 0; wave < 4; wave++)
            {
                int shape = wave;
                var button = new Button
                {
                    Height = 34, Padding = new(2), HorizontalAlignment = HorizontalAlignment.Stretch,
                    Content = new SynthWaveform(shell) { Wave = (SynthVoice.Wave)wave, Width = 32, Height = 24 }
                };
                Identify(button, $"Wave{index + 1}.{wave}");
                _shapes[index, wave] = button;
                button.Click += (_, _) =>
                {
                    if (!Ready || _workflow.State.Busy || !PendingCommand.IsCompleted) return;
                    _draft = Values.SetItem("wave" + (index + 1), shape);
                    RefreshValues();
                    Run(CommitAsync);
                };
                choices.Children.Add(button);
                SetColumn(button, wave);
            }

            card.Children.Add(choices);
            _waves[index] = new(shell) { Height = 44, Accent = index == 0 ? "Accent.Action" : "Rack.Channel1" };
            Identify(_waves[index], "Oscillator" + (index + 1));
            card.Children.Add(_waves[index]);
            _oscillators[index] = Panel(card);
            _oscillators[index].Width = 240;
            oscillators.Children.Add(_oscillators[index]);
        }

        body.Children.Add(oscillators);
        var shaping = new WrapPanel
            { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        shaping.Children.Add(Parameter("mix"));
        shaping.Children.Add(Parameter("detune"));
        shaping.Children.Add(Parameter("cutoff"));
        shaping.Children.Add(Parameter("resonance"));
        SetRow(shaping, 1);
        body.Children.Add(shaping);
        _envelope = new(shell) { MinHeight = 112, VerticalAlignment = VerticalAlignment.Stretch };
        _envelope.Begin = Begin;
        _envelope.Preview += Set;
        _envelope.Completed += () => Run(CommitAsync);
        _envelope.Cancelled += Cancel;
        SetRow(_envelope, 2);
        body.Children.Add(_envelope);
        var adsr = new WrapPanel
            { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var key in new[] { "attack", "decay", "sustain", "release" }) adsr.Children.Add(Parameter(key));
        SetRow(adsr, 3);
        body.Children.Add(adsr);
        var scroll = new ScrollViewer
        {
            Content = body, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        scroll.PropertyChanged += (_, e) =>
        {
            if (e.Property != ScrollViewer.ViewportProperty) return;
            body.MinHeight = scroll.Viewport.Height;
            foreach (var card in _oscillators) card.Width = (Math.Max(284, scroll.Viewport.Width) - 16) / 2;
        };
        SetRow(scroll, 1);
        Children.Add(scroll);
        var audition = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _note.ItemsSource = Enumerable.Range(0, 128).Select(i => PianoNotes.Name(i)).ToArray();
        _note.SelectedIndex = 60;
        Identify(_note, "Note");
        audition.Children.Add(_note);
        _note.SelectionChanged += (_, _) =>
        {
            if (!_refreshing && _held && _gateHeld && _target is { } target)
                Run(async () => { await _workflow.HoldSynthAsync(target, _generation, _note.SelectedIndex, .8m); });
        };
        audition.Children.Add(Action("Hold", "Play", async () =>
        {
            if (_target is not { } target) return;
            _held = await _workflow.HoldSynthAsync(target, _generation, _note.SelectedIndex, .8m);
            _gateHeld = _held;
            if (!_held) Feedback(_workflow.State.Detail ?? "Piano.AuditionUnavailable");
        }));
        audition.Children.Add(Action("Release", "NoteOff", async () =>
        {
            await _workflow.ReleaseSamplerAsync();
            _gateHeld = false;
        }));
        audition.Children.Add(Action("Stop", "Stop", async () =>
        {
            await _workflow.StopSamplerAsync();
            _held = _gateHeld = false;
        }));
        var footer = new Border { Child = audition, Padding = new(0, 8, 0, 0), BorderThickness = new(0, 1, 0, 0) };
        footer.Bind(Border.BorderBrushProperty, this.GetResourceObservable("Border.Default"));
        SetRow(footer, 2);
        Children.Add(footer);
        SizeChanged += (_, _) =>
        {
            _envelope.Cancel();
            foreach (var p in _parameters.Values) p.Dial.Cancel();
        };
    }

    private static void Identify(Control c, string id) => AutomationProperties.SetAutomationId(c, "Synth." + id);

    private TextBlock Label(string key, string style)
    {
        var text = new TextBlock { Classes = { style } };
        _labels.Add((text, key));
        return text;
    }

    private Border Panel(Control child)
    {
        return new Border { Child = child, Padding = new(0), Margin = new(0, 0, 8, 0) };
    }

    private Button Action(string key, string icon, Func<Task> command)
    {
        var button = new Button
            { Width = 42, Height = 40, Padding = new(8), HorizontalContentAlignment = HorizontalAlignment.Center };
        if (key == "Release")
        {
            button.Width = 108;
            button.Padding = new(6);
            var content = new StackPanel
                { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            content.HorizontalAlignment = HorizontalAlignment.Center;
            _releaseLabel.VerticalAlignment = VerticalAlignment.Center;
            content.Children.Add(HostIcons.Create(icon, button, 16));
            content.Children.Add(_releaseLabel);
            _releaseLabel.Bind(TextBlock.ForegroundProperty, button.GetObservable(Button.ForegroundProperty));
            button.Content = content;
        }
        else button.Content = HostIcons.Create(icon, button, 20);

        if (key == "Hold") button.Classes.Add("play");
        Identify(button, key);
        _actions.Add((button, key));
        button.Click += (_, _) => Run(async () =>
        {
            await command();
            Observe(_target);
        });
        return button;
    }

    private Control Parameter(string key)
    {
        var parameter = SynthVoice.Parameters.Single(p => p.Key == key);
        var dial = new GainDialControl(_shell)
            { Width = 52, Height = 52, CenterText = "", HasRecessedWell = true, Focusable = true };
        var field = new TextBox
            { Width = 84, Classes = { "numeric" }, HorizontalContentAlignment = HorizontalAlignment.Center };
        Identify(dial, "Dial." + key);
        Identify(field, "Value." + key);
        var cell = new StackPanel
            { Width = 126, HorizontalAlignment = HorizontalAlignment.Center, Classes = { "tight" } };
        var label = Label(key, "caption");
        label.HorizontalAlignment = HorizontalAlignment.Center;
        var unit = new TextBlock
            { Text = parameter.Unit, Classes = { "caption" }, VerticalAlignment = VerticalAlignment.Center };
        if (parameter.Unit is "ms" or "Hz") _labels.Add((unit, "Unit." + parameter.Unit));
        var exact = new Grid
        {
            Width = 126, ColumnDefinitions = new("21,84,21"), ColumnSpacing = 0, Classes = { "parameterRow" },
            HorizontalAlignment = HorizontalAlignment.Center
        };
        exact.Children.Add(field);
        SetColumn(field, 1);
        unit.Margin = new(4, 0, 0, 0);
        exact.Children.Add(unit);
        SetColumn(unit, 2);
        cell.Children.Add(label);
        cell.Children.Add(dial);
        cell.Children.Add(exact);
        var error = new TextBlock
        {
            Classes = { "warning", "caption" }, IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 126
        };
        Identify(error, "Error." + key);
        _fieldErrors.Add(key, error);
        cell.Children.Add(error);
        dial.HorizontalAlignment = HorizontalAlignment.Center;
        dial.Begin = Begin;
        dial.Preview += value => Set(key, Denormalize(parameter, value));
        dial.Completed += () => Run(CommitAsync);
        dial.Cancelled += Cancel;
        field.PropertyChanged += (_, e) =>
        {
            if (!_refreshing && e.Property == TextBox.TextProperty) _dirtyFields.Add(key);
        };
        field.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Cancel();
                e.Handled = true;
            }

            if (e.Key == Key.Enter)
            {
                CommitField();
                e.Handled = true;
            }
        };
        field.LostFocus += (_, _) =>
        {
            if (!_refreshing && field.Text != Display(parameter, Values[key])) CommitField();
        };

        void CommitField()
        {
            if (!Ready) return;
            if (!decimal.TryParse(field.Text?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var value))
            {
                RejectField();
                return;
            }

            if (parameter.Unit == "%") value /= 100;
            if (value < parameter.Minimum || value > parameter.Maximum)
            {
                RejectField();
                return;
            }

            field.Classes.Remove("warning");
            error.IsVisible = false;
            _dirtyFields.Remove(key);
            _draft = Values.SetItem(key, value);
            Run(CommitAsync);
        }

        void RejectField()
        {
            field.Classes.Add("warning");
            error.IsVisible = true;
        }

        _parameters.Add(key, (dial, field, parameter));
        return cell;
    }

    private static decimal Normalize(SynthVoice.Parameter p, decimal value) =>
        p.Key is "cutoff" or "attack" or "decay" or "release"
            ? (decimal)(Math.Log(1 + (double)(value - p.Minimum)) / Math.Log(1 + (double)(p.Maximum - p.Minimum)))
            : (value - p.Minimum) / (p.Maximum - p.Minimum);

    private static decimal Denormalize(SynthVoice.Parameter p, decimal value) => decimal.Round(p.Minimum +
        (p.Key is "cutoff" or "attack" or "decay" or "release"
            ? (decimal)(Math.Exp((double)value * Math.Log(1 + (double)(p.Maximum - p.Minimum))) - 1)
            : value * (p.Maximum - p.Minimum)), 3);

    private static string Display(SynthVoice.Parameter p, decimal value) =>
        (p.Unit == "%" ? value * 100 : value).ToString("0.###", CultureInfo.InvariantCulture);

    private bool Begin()
    {
        if (!Ready || _workflow.State.Generation != _generation || _workflow.State.Busy ||
            !PendingCommand.IsCompleted || _dirtyFields.Count > 0) return false;
        _draft ??= Values;
        return true;
    }

    private void Set(string key, decimal value)
    {
        if (_draft is null) return;
        _draft = _draft.SetItem(key, value);
        QueuePreview(_draft);
        RefreshValues();
    }

    private void QueuePreview(ImmutableDictionary<string, decimal>? values)
    {
        if (_target is not { } target) return;
        _nextPreview = new(target, _generation, values);
        if (PendingPreview.IsCompleted) PendingPreview = PublishPreviewAsync();
    }

    private async Task PublishPreviewAsync()
    {
        while (_nextPreview is { } preview)
        {
            _nextPreview = null;
            await _workflow.PreviewSynthParametersAsync(preview.Target, preview.Generation, preview.Values);
        }
    }

    private void Cancel()
    {
        _dirtyFields.Clear();
        foreach (var p in _parameters.Values) p.Field.Classes.Remove("warning");
        foreach (var error in _fieldErrors.Values) error.IsVisible = false;
        _draft = null;
        QueuePreview(null);
        _feedbackKey = null;
        _status.Text = "";
        _status.IsVisible = false;
        RefreshValues();
    }

    private void Feedback(string key)
    {
        _feedbackKey = key;
        _status.Text = _shell[key];
        _status.IsVisible = true;
    }

    private void Invalid() => Feedback("Synth.Invalid");

    private async Task CommitAsync()
    {
        if (!Ready || _draft is not { } draft || _target is not { } target || _sound is not { } sound) return;
        _draft = null;
        _feedbackKey = null;
        _status.Text = "";
        _status.IsVisible = false;
        await PendingPreview;
        if (!await _workflow.SetSynthParametersAsync(target, _generation, sound.Parameters, draft)) QueuePreview(null);
        Observe(_target);
    }

    private void Run(Func<Task> command)
    {
        if (_disposed || !PendingCommand.IsCompleted) return;
        PendingCommand = ExecuteAsync(command);
    }

    private async Task ExecuteAsync(Func<Task> command)
    {
        try
        {
            await command();
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            Invalid();
        }
    }

    internal void Observe(NodeEditorTarget? target)
    {
        if (_disposed) return;
        var observation = _workflow.State;
        _closed = observation.IsClosed;
        bool retargeted = _target != target;
        bool changed = _target != target || _generation != observation.Generation;
        if (changed)
        {
            _envelope.Cancel();
            foreach (var p in _parameters.Values) p.Dial.Cancel();
            _draft = null;
            _dirtyFields.Clear();
            _feedbackKey = null;
            _status.Text = "";
            _status.IsVisible = false;
            foreach (var p in _parameters.Values) p.Field.Classes.Remove("warning");
            foreach (var error in _fieldErrors.Values) error.IsVisible = false;
            if (retargeted) StopOwnedPreview();
        }

        _target = target;
        _generation = observation.Generation;
        _sound = target is not null && target.LifecycleId == observation.Document.LifecycleId
            ? DrumEditorSession.Resolve(observation.Snapshot.State, target)
            : null;
        if (_sound is not null && !SynthVoice.Supports(_sound)) _sound = null;
        _held = target is not null && _workflow.SynthPreviewTarget == target;
        _gateHeld = _held && _workflow.SynthPreviewHeld;
        _refreshing = true;
        foreach (var (label, key) in _labels) label.Text = _shell["Synth." + key];
        _identity.Text = _sound?.Name is null or "Synth" or "Синтезатор" ? _shell["Synth.Title"] : _sound.Name;
        ToolTip.SetTip(_identity, _sound?.Name);
        _identity.IsVisible = _sound?.Name is not (null or "Synth" or "Синтезатор");
        var presetNames = new[] { _shell["Synth.Custom"] }
            .Concat(SynthVoice.Presets.Select(p => _shell["Synth." + p.Key])).ToArray();
        if (!_presets.Items.OfType<ComboBoxItem>().Select(i => i.Content as string).SequenceEqual(presetNames))
            _presets.ItemsSource = presetNames
                .Select((name, index) => new ComboBoxItem { Content = name, IsEnabled = index > 0 }).ToArray();
        _presets.SelectedIndex = 1 + SynthVoice.Presets.ToList()
            .FindIndex(p => p.Values.OrderBy(v => v.Key).SequenceEqual(Values.OrderBy(v => v.Key)));
        _presets.IsEnabled = Ready;
        foreach (var (button, key) in _actions)
        {
            button.IsEnabled = Ready &&
                               (key == "Hold" ||
                                _held && (key == "Stop" || _gateHeld && _workflow.SamplerPreviewPlaying));
            bool active = key == "Hold" && _gateHeld && _held && _workflow.SamplerPreviewPlaying;
            button.Classes.Set("active", active);
            button.Classes.Set("primary", active);
            button.BorderThickness = new(active ? 2 : 1);
            AutomationProperties.SetHelpText(button, active ? _shell["Synth.Holding"] : "");
            ToolTip.SetTip(button, _shell["Synth." + key]);
            AutomationProperties.SetName(button, _shell["Synth." + key]);
        }

        _releaseLabel.Text = _shell["Synth.ReleaseLabel"];
        _status.Text = _feedbackKey is null ? "" : _shell[_feedbackKey];
        _status.IsVisible = !string.IsNullOrEmpty(_status.Text);
        _note.IsEnabled = Ready;
        ToolTip.SetTip(_note, _shell["Synth.Note"]);
        AutomationProperties.SetName(_note, _shell["Synth.Note"]);
        AutomationProperties.SetName(_envelope, _shell["Synth.Envelope"]);
        for (int osc = 0; osc < 2; osc++)
        for (int wave = 0; wave < 4; wave++)
        {
            var button = _shapes[osc, wave];
            string name = _shell["Synth.Osc" + (osc + 1)] + " · " + _shell["Synth." + (SynthVoice.Wave)wave];
            AutomationProperties.SetName(button, name);
            ToolTip.SetTip(button, name);
            button.IsEnabled = Ready;
        }

        _refreshing = false;
        if (changed || _displayedPreferences != _shell.Preferences) RefreshValues();
    }

    private void RefreshValues()
    {
        _displayedPreferences = _shell.Preferences;
        _refreshing = true;
        foreach (var (key, p) in _parameters)
        {
            p.Dial.Amplitude = Normalize(p.Parameter, Values[key]);
            p.Dial.DefaultAmplitude = Normalize(p.Parameter, p.Parameter.Default);
            p.Dial.IsEnabled = p.Field.IsEnabled = Ready;
            if (!_dirtyFields.Contains(key)) p.Field.Text = Display(p.Parameter, Values[key]);
            p.Dial.InvalidateVisual();
            AutomationProperties.SetName(p.Field, _shell["Synth." + key]);
            AutomationProperties.SetName(p.Dial, _shell["Synth." + key]);
            string unit = p.Parameter.Unit is "ms" or "Hz"
                ? _shell["Synth.Unit." + p.Parameter.Unit]
                : p.Parameter.Unit;
            string range = Display(p.Parameter, p.Parameter.Minimum) + "–" + Display(p.Parameter, p.Parameter.Maximum) +
                           " " + unit;
            string help = _shell["Synth.Range"] + " " + range;
            var error = _fieldErrors[key];
            error.Text = range;
            AutomationProperties.SetName(error, _shell["Synth." + key] + ": " + help);
            AutomationProperties.SetHelpText(p.Field, help);
            ToolTip.SetTip(p.Field, help);
            ToolTip.SetTip(p.Dial, help);
        }

        for (int osc = 0; osc < 2; osc++)
        {
            _waves[osc].Wave = (SynthVoice.Wave)(int)Values["wave" + (osc + 1)];
            _waves[osc].InvalidateVisual();
            for (int shape = 0; shape < 4; shape++)
            {
                var button = _shapes[osc, shape];
                bool selected = shape == (int)_waves[osc].Wave;
                button.Classes.Set("primary", selected);
                button.BorderThickness = new(selected ? 2 : 1);
                var glyph = (SynthWaveform)button.Content!;
                glyph.Accent = selected ? "Accent.Action" : "Text.Secondary";
                glyph.InvalidateVisual();
                AutomationProperties.SetHelpText(button, selected ? _shell["Synth.Selected"] : "");
            }
        }

        _envelope.Values = Values;
        _envelope.IsEnabled = Ready;
        _envelope.InvalidateVisual();
        _refreshing = false;
    }

    internal void Suspend()
    {
        _envelope.Cancel();
        foreach (var p in _parameters.Values) p.Dial.Cancel();
        Cancel();
        StopOwnedPreview();
    }

    private void StopOwnedPreview()
    {
        var target = _target;
        var pending = PendingCommand;
        PendingCommand = StopAsync();

        async Task StopAsync()
        {
            await pending;
            if (target is not null && _workflow.SynthPreviewTarget == target) await _workflow.StopSamplerAsync();
        }

        _held = _gateHeld = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _envelope.Cancel();
        foreach (var p in _parameters.Values) p.Dial.Cancel();
        _disposed = true;
    }
}