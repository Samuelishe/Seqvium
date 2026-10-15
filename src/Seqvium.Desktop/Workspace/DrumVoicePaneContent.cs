// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Graph-led sound design over one shared SoundDefinition, using the host parameter/history boundary.</summary>
internal sealed class DrumVoicePaneContent : Grid, IDisposable
{
    private sealed record ParameterControl(
        DrumVoice.Parameter Parameter,
        Grid Body,
        TextBlock Label,
        GainDialControl Dial,
        TextBox Exact,
        TextBlock Unit);

    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly DrumEditorSession _session = new();
    private readonly List<ParameterControl> _parameters = [];
    private ParameterControl? _textDraft;
    private bool _pointerEdit;
    private readonly TextBlock _identity = new() { Classes = { "caption" } };

    private readonly TextBlock _status = new()
        { Classes = { "caption", "warning" }, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center };

    private readonly Button _pitch = new(), _amplitude = new(), _hit = new() { Classes = { "primary", "audition" } };
    private readonly Button _stop = new() { Classes = { "audition", "stop" } };
    private readonly ComboBox _note = new()
        { Width = HostMetrics.Values["Audition.NoteWidth"], Height = HostMetrics.Values["AuditionLevel.Height"] };
    private readonly ComboBox _presets = new() { MinWidth = 110 };

    private readonly Button _savePreset = new() { Classes = { "command" } },
        _loadPreset = new() { Classes = { "command" } };

    private readonly ToggleButton _choke = new();
    private readonly WrapPanel _primary = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private string? _instrumentAlgorithm;
    private int _parameterColumns;
    private readonly GainLevelControl _velocity;
    private readonly DrumEnvelopePlot _plot;
    private readonly ScrollViewer _scroll;
    private readonly Border _auditionDock;
    private readonly WrapPanel _shape = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly DispatcherTimer _notice = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _refreshing, _disposed;
    private bool _presetBusy;
    private Task _command = Task.CompletedTask;
    private Task _auditionWork = Task.CompletedTask;
    private CancellationTokenSource? _waveCancellation;
    private ImmutableDictionary<string, decimal>? _waveBasis;
    internal Task PendingCommand => Task.WhenAll(_command, _auditionWork);
    internal Task PendingWaveform { get; private set; } = Task.CompletedTask;

    public DrumVoicePaneContent(ShellSession shell, DesktopWorkflow workflow)
    {
        _shell = shell;
        _workflow = workflow;
        _velocity = new(shell) { SizeRole = "AuditionLevel", Width = HostMetrics.Values["Audition.LevelWidth"],
            Height = HostMetrics.Values["AuditionLevel.Height"], Focusable = true, Amplitude = (.8m - .05m) / .95m };
        decimal? velocityBefore = _velocity.Amplitude;
        _velocity.Begin = () => { velocityBefore = _velocity.Amplitude; _pointerEdit = true; return true; };
        _velocity.Preview += value => { _velocity.Amplitude = value; _velocity.InvalidateVisual(); };
        _velocity.Cancelled += () => { _pointerEdit = false; _velocity.Amplitude = velocityBefore; _velocity.InvalidateVisual(); };
        _velocity.Completed += async () =>
        {
            _pointerEdit = false;
            ApplyResponsiveLayout();
            if (_velocity.Amplitude != velocityBefore) await AuditionAsync();
        };
        RowDefinitions = new("*,Auto");
        this.Bind(RowSpacingProperty, this.GetResourceObservable("Space.Gap"));
        var auditionActions = new StackPanel { Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        auditionActions.Bind(StackPanel.SpacingProperty, this.GetResourceObservable("Space.Medium"));
        auditionActions.Children.Add(_hit);
        auditionActions.Children.Add(_stop);
        var audition = new Grid { ColumnDefinitions = new("Auto,Auto"), RowDefinitions = new("Auto"),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        audition.Bind(ColumnSpacingProperty, this.GetResourceObservable("Space.Section"));
        audition.Bind(RowSpacingProperty, this.GetResourceObservable("Space.Medium"));
        var auditionValues = new StackPanel { Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        auditionValues.Bind(StackPanel.SpacingProperty, this.GetResourceObservable("Space.Medium"));
        auditionValues.Children.Add(_note);
        auditionValues.Children.Add(_velocity);
        audition.Children.Add(auditionActions);
        audition.Children.Add(auditionValues);
        SetColumn(auditionValues, 1);
        var auditionDock = new Border { Child = audition };
        _auditionDock = auditionDock;
        auditionDock.Bind(Border.PaddingProperty, this.GetResourceObservable("Padding.Audition"));
        auditionDock.Bind(Border.BackgroundProperty, this.GetResourceObservable("Material.Header"));
        auditionDock.Bind(Border.BorderBrushProperty, this.GetResourceObservable("Border.Default"));
        auditionDock.Bind(Border.BorderThicknessProperty, this.GetResourceObservable("Border.Top"));
        bool compactAudition = false;
        auditionDock.SizeChanged += (_, _) =>
        {
            double width = auditionDock.Bounds.Width;
            bool compact = compactAudition ? width < 504 : width < 480;
            if (width <= 0 || compact == compactAudition) return;
            compactAudition = compact;
            audition.ColumnDefinitions = new(compact ? "Auto" : "Auto,Auto");
            audition.RowDefinitions = new(compact ? "Auto,Auto" : "Auto");
            SetRow(auditionValues, compact ? 1 : 0);
            SetColumn(auditionValues, compact ? 0 : 1);
        };
        _note.ItemsSource = Enumerable.Range(0, 128).Select(i => PianoNotes.Name(i)).ToArray();
        _note.SelectedIndex = (int)DrumVoice.RootPitch;
        var body = new StackPanel { Classes = { "tight" } };
        var identityRow = new Grid { ColumnDefinitions = new("*,Auto") };
        identityRow.Bind(HeightProperty, this.GetResourceObservable("Control.Height"));
        _identity.VerticalAlignment = VerticalAlignment.Center;
        identityRow.Children.Add(_identity);
        identityRow.Children.Add(_status);
        SetColumn(_status, 1);
        body.Children.Add(identityRow);
        var presets = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left };
        presets.Children.Add(_presets);
        presets.Children.Add(_savePreset);
        presets.Children.Add(_loadPreset);
        presets.Children.Add(_choke);
        body.Children.Add(presets);
        body.Children.Add(_primary);
        var chassis = new Border { Child = body };
        chassis.Bind(Border.PaddingProperty, this.GetResourceObservable("Padding.Content"));
        chassis.Bind(Border.BackgroundProperty, this.GetResourceObservable("Material.Panel"));
        chassis.Bind(Border.BorderBrushProperty, this.GetResourceObservable("Border.Default"));
        chassis.Bind(Border.BorderThicknessProperty, this.GetResourceObservable("Border.Control"));
        chassis.Bind(Border.CornerRadiusProperty, this.GetResourceObservable("Control.Corner"));
        chassis.Bind(Border.BoxShadowProperty, this.GetResourceObservable("Material.Recess"));
        var modes = new StackPanel
            { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        modes.Children.Add(_pitch);
        modes.Children.Add(_amplitude);
        body.Children.Add(modes);
        _plot = new(shell) { Height = HostMetrics.Values["Drum.GraphHeight"], MinWidth = HostMetrics.Values["Drum.GraphMinWidth"] };
        SizeChanged += (_, _) => ApplyResponsiveLayout();
        body.Children.Add(_plot);
        body.Children.Add(_shape);
        var scroll = new ScrollViewer
        {
            Content = chassis, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        _scroll = scroll;
        scroll.PropertyChanged += (_, change) =>
        {
            if (change.Property == ScrollViewer.ViewportProperty) ApplyResponsiveLayout();
        };
        Children.Add(scroll);
        Identify(scroll, "Scroll");
        SetRow(scroll, 0);
        Children.Add(auditionDock);
        SetRow(auditionDock, 1);
        Identify(_pitch, "Pitch");
        Identify(_amplitude, "Amplitude");
        Identify(_hit, "Audition");
        Identify(_stop, "StopAudition");
        Identify(_note, "Note");
        Identify(_velocity, "Velocity");
        Identify(_identity, "Identity");
        Identify(_status, "Status");
        Identify(_presets, "Preset");
        Identify(_savePreset, "SavePreset");
        Identify(_loadPreset, "LoadPreset");
        Identify(_choke, "Choke");
        AttachedToVisualTree += (_, _) =>
        {
            _hit.Content ??= HostIcons.Create("Play", _hit, 32);
            _stop.Content ??= HostIcons.Create("Stop", _stop, 24);
            _savePreset.Content ??= HostIcons.Create("Save", _savePreset, 16);
            _loadPreset.Content ??= HostIcons.Create("Open", _loadPreset, 16);
        };
        _savePreset.Click += async (_, _) => await PresetFileAsync(true);
        _loadPreset.Click += async (_, _) => await PresetFileAsync(false);
        _presets.SelectionChanged += async (_, _) =>
        {
            if (_refreshing || _presets.SelectedIndex < 0 || !Begin()) return;
            _session.Preset(_session.Instrument.Presets[_presets.SelectedIndex].Parameters);
            await CommitAsync();
        };
        _choke.Click += async (_, _) =>
        {
            if (_refreshing || !Begin()) return;
            _session.Set("choke", _choke.IsChecked == true ? 1 : 0);
            await CommitAsync();
        };
        _pitch.Click += (_, _) => Switch(false);
        _amplitude.Click += (_, _) => Switch(true);
        _hit.Click += async (_, _) => await AuditionAsync();
        _stop.Click += async (_, _) => await _workflow.CancelDrumAuditionAsync();
        _note.SelectionChanged += (_, _) =>
        {
            _waveBasis = null;
            if (!_refreshing) RefreshPreview();
        };
        _plot.Begin = () => Begin(pointer: true);
        _plot.Preview += Preview;
        _plot.Completed += async () => await CommitAsync();
        _plot.Cancelled += () =>
        {
            if (!_refreshing) Cancel();
        };
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Escape || _session.Draft is null) return;
            e.Handled = true;
            Cancel();
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        _notice.Tick += (_, _) =>
        {
            _notice.Stop();
            _session.Cancel();
            RefreshPreview();
        };
    }

    private static void Identify(Control control, string key) =>
        AutomationProperties.SetAutomationId(control, "Drum." + key);

    private ParameterControl CreateParameter(DrumVoice.Parameter parameter)
    {
        var panel = new Grid { RowDefinitions = new("Auto,Auto,Auto"), Width = HostMetrics.Values["Drum.ParameterWidth"], Margin = new(4, 4) };
        panel.RowDefinitions[0].Height = new GridLength(HostMetrics.Values["Drum.LabelHeight"]);
        var label = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            TextAlignment = Avalonia.Media.TextAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
            MaxLines = 2, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };
        var dial = new GainDialControl(_shell)
        {
            Width = HostMetrics.Values["Drum.DialSize"], Height = HostMetrics.Values["Drum.DialSize"], Focusable = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            DefaultAmplitude = Normalize(parameter, parameter.Default),
            AccentRole = parameter.Key is "attack" or "amplitudeCurve" ? "Rack.Channel1" : "Control.Fill"
        };
        var exact = new TextBox { Width = 72, Classes = { "numeric" } };
        var unit = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Classes = { "caption" } };
        var row = new Grid
        {
            ColumnDefinitions = new("Auto,Auto"), HorizontalAlignment = HorizontalAlignment.Center,
            Classes = { "parameterRow" }
        };
        row.Children.Add(exact);
        row.Children.Add(unit);
        SetColumn(unit, 1);
        panel.Children.Add(label);
        panel.Children.Add(dial);
        SetRow(dial, 1);
        panel.Children.Add(row);
        SetRow(row, 2);
        Identify(dial, "Dial." + parameter.Key);
        Identify(exact, parameter.Key);
        Identify(label, "Label." + parameter.Key);
        var result = new ParameterControl(parameter, panel, label, dial, exact, unit);
        _parameters.Add(result);
        dial.Begin = () => Begin(pointer: true);
        dial.Preview += value => Preview(parameter.Key, Denormalize(parameter, value));
        dial.Completed += async () => await CommitAsync();
        dial.Cancelled += () =>
        {
            if (!_refreshing) Cancel();
        };
        exact.PropertyChanged += (_, change) =>
        {
            if (_refreshing || change.Property != TextBox.TextProperty || !Begin()) return;
            _textDraft = result;
            if (Parse(result, out var value))
            {
                _session.Set(parameter.Key, value);
                RefreshPreview();
            }
            else
            {
                exact.Classes.Set("invalid", true);
                _status.Text = _shell["Graph.InvalidValue"];
                _status.IsVisible = true;
            }
        };
        exact.GotFocus += (_, _) => RefreshPreview(updateText: true);
        exact.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (Parse(result, out var value) && Begin())
            {
                _session.Set(parameter.Key, value);
                await CommitAsync();
            }
        };
        exact.LostFocus += async (_, _) =>
        {
            if (_textDraft != result) { RefreshPreview(updateText: true); return; }
            if (_session.Draft is null || _refreshing || !_command.IsCompleted) return;
            if (Parse(result, out var value))
            {
                _session.Set(parameter.Key, value);
                await CommitAsync();
            }
            else Cancel();
        };
        return result;
    }

    private static decimal Normalize(DrumVoice.Parameter p, decimal value) =>
        (value - p.Minimum) / (p.Maximum - p.Minimum);

    private static decimal Denormalize(DrumVoice.Parameter p, decimal value) =>
        decimal.Round(p.Minimum + value * (p.Maximum - p.Minimum), 3);

    private bool Parse(ParameterControl control, out decimal value)
    {
        bool valid =
            decimal.TryParse(control.Exact.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
            decimal.TryParse(control.Exact.Text, NumberStyles.Float, CultureInfo.GetCultureInfo(
                _shell.Preferences.Language == HostLanguage.Russian ? "ru-RU" : "en-US"), out value);
        if (control.Parameter.Unit == "%") value /= 100;
        return valid && value >= control.Parameter.Minimum && value <= control.Parameter.Maximum;
    }

    private bool Begin(bool pointer = false)
    {
        if (pointer && _textDraft is not null)
        {
            // Finish the text owner's transaction before accepting another parameter gesture.
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
            return false;
        }
        if (_session.Observe(_workflow.State, _session.Target))
        {
            Refresh();
            return false;
        }

        bool started = !_disposed && !_presetBusy && _command.IsCompleted && _session.Begin();
        if (started) { _pointerEdit = pointer; _notice.Stop(); }
        return started;
    }

    private void ApplyResponsiveLayout()
    {
        // Reflow in the space owned by this editor, independent of the main window or docking.
        // Keep control sizes stable until the current pointer edit ends. Text fields retain their
        // controls and dimensions across reflow, so a typed draft can keep its focus.
        if (_pointerEdit || _plot.HasGesture || _velocity.HasGesture || _parameters.Any(p => p.Dial.HasGesture) ||
            Bounds.Width <= 0 || Bounds.Height <= 0) return;
        double maximumDial = HostVisual.Metric(this, "Drum.DialSize");
        double minimumDial = HostVisual.Metric(this, "Drum.CompactDialSize");
        double available = Math.Max(1, Bounds.Width - HostMetrics.Values["Space.Medium"] * 2 -
            HostVisual.Metric(this, "ScrollBar.Size") - 2);
        double slotHeight = Math.Max(0, Bounds.Height - _auditionDock.Bounds.Height - HostMetrics.Values["Space.Gap"]);
        double viewportHeight = _scroll.Viewport.Height > 0 ? Math.Min(slotHeight, _scroll.Viewport.Height) : slotHeight;
        // Visual size follows only the allocated viewport, continuously and with a mouse-friendly
        // floor. Changing the number of columns can never enlarge a knob on a shrinking pane.
        double widthSize = minimumDial + (maximumDial - minimumDial) * (available - 492) / (704 - 492);
        double heightSize = minimumDial + (maximumDial - minimumDial) * (viewportHeight - 360) / (520 - 360);
        double dial = Math.Clamp(Math.Min(widthSize, heightSize), minimumDial, maximumDial);
        double sizeFraction = (dial - minimumDial) / (maximumDial - minimumDial);
        double cell = Math.Floor(90 + sizeFraction * (HostVisual.Metric(this, "Drum.ParameterWidth") - 90));
        double RowWidth(int columns) => columns * (cell + 8) + 2;
        if (_parameterColumns == 0) _parameterColumns = Math.Clamp((int)((available - 2) / (cell + 8)), 2, 5);
        // Remove a column only when it no longer fits; add it back with 12 DIP of spare room.
        while (_parameterColumns > 2 && RowWidth(_parameterColumns) > available) _parameterColumns--;
        while (_parameterColumns < 5 && RowWidth(_parameterColumns + 1) + 12 <= available) _parameterColumns++;
        foreach (var parameter in _parameters)
        {
            parameter.Body.Width = cell;
            parameter.Label.MaxWidth = parameter.Body.Width;
            parameter.Dial.Width = parameter.Dial.Height = dial;
            // Compact is the Mixer's different drawing style; it would enlarge the knob inside
            // a smaller hit area. Drum keeps one drawing model so its visible disc really scales.
            parameter.Dial.Compact = false;
            parameter.Dial.InvalidateVisual();
        }
        _primary.Width = Math.Min(available, Math.Min(_parameterColumns, _primary.Children.Count(c => c.IsVisible)) * (cell + 8) + 2);
        _shape.Width = Math.Min(available, Math.Min(_parameterColumns, _shape.Children.Count(c => c.IsVisible)) * (cell + 8) + 2);
        _status.MaxWidth = Math.Max(80, Bounds.Width * .55);
        _plot.Height = Math.Clamp(Math.Min(viewportHeight * .36, Bounds.Width * .4),
            HostVisual.Metric(this, "Drum.CompactGraphHeight"), HostVisual.Metric(this, "Drum.GraphHeight"));
    }

    private void Preview(string key, decimal value)
    {
        _session.Set(key, value);
        RefreshPreview(updateText: true);
    }

    private void Switch(bool amplitude)
    {
        Cancel();
        _plot.AmplitudeMode = amplitude;
        Refresh();
    }

    internal void Observe(NodeEditorTarget? target)
    {
        if (_session.Target != target) _ = _workflow.CancelDrumAuditionAsync();
        if (_session.Observe(_workflow.State, target))
        {
            _pointerEdit = false;
            _textDraft = null;
            _refreshing = true;
            _velocity.Cancel();
            foreach (var parameter in _parameters) parameter.Dial.Cancel();
            _plot.Cancel();
            _refreshing = false;
            if (_session.ChangedElsewhere)
            {
                _notice.Stop();
                _notice.Start();
            }
        }

        Refresh();
    }

    private void Refresh()
    {
        if (_disposed) return;
        _refreshing = true;
        try
        {
            var sound = _session.Sound;
            var instrument = _session.Instrument;
            if (_instrumentAlgorithm != instrument.Algorithm)
            {
                _primary.Children.Clear();
                _shape.Children.Clear();
                _parameters.Clear();
                foreach (var parameter in instrument.Parameters.Where(p => p.Key != "choke"))
                {
                    var control = CreateParameter(parameter);
                    (parameter.Key is "pitchAmount" or "pitchDecay" or "pitchCurve" or "attack" or "amplitudeCurve"
                        ? _shape
                        : _primary).Children.Add(control.Body);
                }

                _instrumentAlgorithm = instrument.Algorithm;
                _waveBasis = null;
                _plot.AmplitudeMode = !instrument.HasPitchEnvelope;
                _plot.Algorithm = instrument.Algorithm;
            }

            var presetLabels = instrument.Presets.Select(p => _shell["Drum.Preset." + p.Name]).ToArray();
            if (!_presets.Items.OfType<string>().SequenceEqual(presetLabels)) _presets.ItemsSource = presetLabels;
            _presets.SelectedIndex = Enumerable.Range(0, instrument.Presets.Length).FirstOrDefault(i =>
                    instrument.Presets[i].Parameters.OrderBy(v => v.Key)
                        .SequenceEqual(_session.Values.OrderBy(v => v.Key)),
                -1);
            _presets.PlaceholderText = _shell["Drum.Custom"];
            _presets.IsEnabled =
                _savePreset.IsEnabled =
                    _loadPreset.IsEnabled = _session.CanEdit && _command.IsCompleted && !_presetBusy;
            _choke.IsVisible = instrument.IsHat;
            _choke.IsEnabled = _session.CanEdit && _command.IsCompleted;
            _choke.IsChecked = instrument.IsHat && _session.Values["choke"] == 1;
            _choke.Content = _shell["Drum.Choke"];
            ToolTip.SetTip(_choke, _shell["Drum.ChokeHint"]);
            AutomationProperties.SetName(_presets, _shell["Drum.Presets"]);
            AutomationProperties.SetName(_savePreset, _shell["Drum.SavePreset"]);
            AutomationProperties.SetName(_loadPreset, _shell["Drum.LoadPreset"]);
            ToolTip.SetTip(_savePreset, _shell["Drum.SavePreset"]);
            ToolTip.SetTip(_loadPreset, _shell["Drum.LoadPreset"]);
            int uses = _workflow.State.Snapshot.State.Patterns.SelectMany(p => p.Parts)
                .Count(p => p.SoundId == sound?.Id);
            _identity.Text = sound?.Name + (uses > 1 ? " · " + _shell["Drum.Shared"] + " " + uses : "");
            _pitch.Content = _shell["Drum.Pitch"];
            _pitch.IsVisible = instrument.HasPitchEnvelope;
            _amplitude.Content = _shell["Drum.Amplitude"];
            _amplitude.IsVisible = instrument.HasPitchEnvelope;
            ToolTip.SetTip(_hit, _shell["Drum.Audition"]);
            ToolTip.SetTip(_stop, _shell["Workflow.Stop"]);
            AutomationProperties.SetName(_stop, _shell["Workflow.Stop"]);
            _pitch.Classes.Set("primary", !_plot.AmplitudeMode);
            _amplitude.Classes.Set("primary", _plot.AmplitudeMode);
            foreach (var p in _parameters)
            {
                p.Label.Text = _shell["Drum." + p.Parameter.Key];
                p.Unit.Text = p.Parameter.Unit;
                p.Exact.IsEnabled = p.Dial.IsEnabled = _session.CanEdit && _command.IsCompleted;
                AutomationProperties.SetName(p.Exact, p.Label.Text + " · " + p.Parameter.Unit);
                AutomationProperties.SetName(p.Dial, p.Label.Text);
                ToolTip.SetTip(p.Dial, _shell["Drum.DialHint"]);
                p.Body.IsVisible =
                    p.Parameter.Key is not ("pitchAmount" or "pitchDecay" or "pitchCurve" or "attack"
                        or "amplitudeCurve") ||
                    _plot.AmplitudeMode == (p.Parameter.Key is "attack" or "amplitudeCurve");
            }

            _hit.IsEnabled = _session.Sound is { } available && DrumVoice.Supports(available) && !_presetBusy &&
                             _workflow.State.Execution?.Execution.Playing != true;
            _stop.IsEnabled = _hit.IsEnabled;
            _plot.IsEnabled = _session.CanEdit && _command.IsCompleted;
            AutomationProperties.SetName(_hit, _shell["Drum.Audition"]);
            AutomationProperties.SetName(_note, _shell["Drum.Note"]);
            AutomationProperties.SetName(_velocity, _shell["Drum.Velocity"]);
            AutomationProperties.SetName(_plot, _shell[_plot.AmplitudeMode ? "Drum.Amplitude" : "Drum.Pitch"]);
            ToolTip.SetTip(_note, _shell["Drum.Note"]);
            ToolTip.SetTip(_velocity, _shell["Drum.Velocity"]);
            ToolTip.SetTip(_plot, _shell["Drum.EnvelopeHint"]);
            ApplyResponsiveLayout();
            RefreshPreview(updateText: _session.Draft is null);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void RefreshPreview(bool updateText = false)
    {
        if (_disposed) return;
        bool previous = _refreshing;
        _refreshing = true;
        var values = _session.Values;
        foreach (var p in _parameters)
        {
            decimal value = values[p.Parameter.Key];
            p.Dial.Amplitude = Normalize(p.Parameter, value);
            p.Dial.CenterText = p.Parameter.Key == "tune"
                ? DrumInstruments.Frequency(_session.Instrument.Algorithm, values).ToString("0.0")
                : "";
            p.Dial.TrackRole = _shell.Preferences.Theme == HostTheme.Light ? "Graph.NodeBorder" : "Control.Track";
            p.Dial.HasRecessedWell = _shell.Preferences.Theme == HostTheme.Light;
            p.Dial.InvalidateVisual();
            if (updateText)
            {
                bool precise = p.Exact.IsKeyboardFocusWithin;
                decimal shown = p.Parameter.Unit == "%" ? value * 100 : value;
                p.Exact.Text =
                    shown.ToString(precise ? "0.############################" : p.Parameter.Unit is "ms" or "%" ? "0.#" : "0.##",
                        CultureInfo.InvariantCulture);
                p.Exact.Classes.Remove("invalid");
            }
        }

        _plot.Values = values;
        _plot.BasePitch = _note.SelectedIndex >= 0 ? _note.SelectedIndex : DrumVoice.RootPitch;
        _plot.IsDraft = _session.Draft is not null;
        _plot.InvalidateVisual();
        _status.Text = _session.ChangedElsewhere ? _shell["NodeEditor.ChangedElsewhere"] : "";
        ToolTip.SetTip(_status, _status.Text);
        _status.IsVisible = _status.Text.Length > 0;
        _refreshing = previous;
        if (_session.Sound is { } sound && DrumVoice.Supports(sound) && IsVisible &&
            (_waveBasis is null || !_waveBasis.OrderBy(p => p.Key).SequenceEqual(values.OrderBy(p => p.Key))))
        {
            _waveCancellation?.Cancel();
            _waveCancellation?.Dispose();
            _waveCancellation = new();
            _waveBasis = values;
            _plot.Waveform = null;
            PendingWaveform = RenderWaveformAsync(sound with { Parameters = values },
                _note.SelectedIndex >= 0 ? _note.SelectedIndex : 36,
                _waveCancellation.Token, PendingWaveform);
        }
    }

    private async Task RenderWaveformAsync(SoundDefinition sound, decimal pitch,
        CancellationToken cancellation, Task previous)
    {
        try
        {
            // Join the cancelled calculation before starting another; bursts of native input must
            // not queue competing synthesis jobs. The analytical envelope/value stays immediate.
            await previous;
            await Task.Delay(40, cancellation);
            var envelope = await Task.Run(() =>
            {
                using var pcm = DrumVoice.Render(sound, 48000, pitch, cancellation);
                var result = new float[640];
                for (int bin = 0; bin < 320; bin++)
                {
                    float minimum = 0, maximum = 0;
                    for (int frame = bin * pcm.Frames / 320; frame < (bin + 1) * pcm.Frames / 320; frame++)
                    {
                        float sample = pcm.Sample(frame, 0);
                        minimum = Math.Min(minimum, sample);
                        maximum = Math.Max(maximum, sample);
                    }

                    result[bin * 2] = maximum;
                    result[bin * 2 + 1] = minimum;
                }

                return result;
            }, cancellation);
            if (_disposed || cancellation.IsCancellationRequested || !IsEffectivelyVisible) return;
            _plot.Waveform = envelope;
            _plot.InvalidateVisual();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
    }

    private async Task PresetFileAsync(bool save)
    {
        if (_presetBusy || !_command.IsCompleted || !_session.CanEdit ||
            TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage) return;
        Cancel();
        var target = _session.Target;
        var sound = _session.Sound!;
        long generation = _workflow.State.Generation;
        bool failed = false;
        _presetBusy = true;
        Refresh();
        try
        {
            var type = new FilePickerFileType("Seqvium drum preset") { Patterns = ["*.seqdrum"] };
            if (save)
            {
                var file = await storage.SaveFilePickerAsync(new()
                {
                    Title = _shell["Drum.SavePreset"], SuggestedFileName = sound.Name + ".seqdrum",
                    DefaultExtension = "seqdrum", FileTypeChoices = [type], ShowOverwritePrompt = true
                });
                if (file is null) return;
                using (file)
                {
                    var bytes = new DrumPresetFile(1, sound.Algorithm, sound.Parameters).Encode();
                    await using var stream = await file.OpenWriteAsync();
                    stream.SetLength(0);
                    await stream.WriteAsync(bytes);
                }
            }
            else
            {
                var files = await storage.OpenFilePickerAsync(new()
                    { Title = _shell["Drum.LoadPreset"], AllowMultiple = false, FileTypeFilter = [type] });
                if (files.Count == 0) return;
                using var file = files[0];
                await using var stream = await file.OpenReadAsync();
                using var bytes = new MemoryStream();
                var buffer = new byte[4096];
                int count;
                while ((count = await stream.ReadAsync(buffer)) > 0)
                {
                    if (bytes.Length + count > 32768) throw new NotSupportedException("Preset too large.");
                    bytes.Write(buffer, 0, count);
                }

                var preset = DrumPresetFile.Decode(bytes.ToArray(), sound.Algorithm);
                _presetBusy = false;
                if (_disposed || _session.Target != target || _workflow.State.Generation != generation ||
                    !Begin()) return;
                _session.Preset(preset.Parameters);
                await CommitAsync();
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException
                                          or System.Text.Json.JsonException)
        {
            failed = true;
        }
        finally
        {
            _presetBusy = false;
            if (!_disposed) Refresh();
            if (failed && !_disposed)
            {
                _status.Text = _shell["Drum.PresetFailed"];
                _status.IsVisible = true;
            }
        }
    }

    private async Task CommitAsync()
    {
        _pointerEdit = false;
        _textDraft = null;
        if (!_command.IsCompleted || _disposed) return;
        _session.Observe(_workflow.State, _session.Target);
        var proposal = _session.Complete();
        if (proposal is null)
        {
            Refresh();
            return;
        }

        _command = _workflow.ShapeAndAuditionDrumAsync(proposal,
            _note.SelectedIndex >= 0 ? _note.SelectedIndex : DrumVoice.RootPitch,
            .05m + .95m * _velocity.Amplitude!.Value);
        Refresh();
        await _command;
        if (!_disposed) Observe(_session.Target);
    }

    private async Task AuditionAsync()
    {
        if (_disposed || _presetBusy || _session.Target is not { } target) return;
        if (_command.IsCompleted) Cancel();
        var state = _workflow.State;
        var request = _workflow.AuditionDrumAsync(target, state.Generation,
            _note.SelectedIndex >= 0 ? _note.SelectedIndex : DrumVoice.RootPitch,
            .05m + .95m * _velocity.Amplitude!.Value, state.TransportGeneration);
        _auditionWork = _auditionWork.IsCompleted ? request : Task.WhenAll(_auditionWork, request);
        Refresh();
        await request;
        Refresh();
    }

    public void Cancel()
    {
        _pointerEdit = false;
        _textDraft = null;
        _session.Cancel();
        _velocity.Cancel();
        _notice.Stop();
        foreach (var p in _parameters) p.Dial.Cancel();
        _plot.Cancel();
        if (!_disposed) Refresh();
    }

    public void Suspend()
    {
        _ = _workflow.CancelDrumAuditionAsync();
        Cancel();
        _waveCancellation?.Cancel();
        _waveBasis = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Suspend();
        _waveCancellation?.Dispose();
    }
}
