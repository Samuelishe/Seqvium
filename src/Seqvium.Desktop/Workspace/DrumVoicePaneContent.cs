// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
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
    private readonly TextBlock _identity = new() { Classes = { "caption" } };

    private readonly TextBlock _status = new()
        { Classes = { "caption", "warning" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    private readonly Button _pitch = new(), _amplitude = new(), _hit = new() { Classes = { "primary" } };
    private readonly ComboBox _note = new() { Width = 84 };
    private readonly Slider _velocity = new() { Minimum = .05, Maximum = 1, Value = .8, Width = 84 };
    private readonly DrumEnvelopePlot _plot;
    private readonly WrapPanel _shape = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly DispatcherTimer _notice = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _refreshing, _disposed;
    private Task _command = Task.CompletedTask;
    private CancellationTokenSource? _waveCancellation;
    private ImmutableDictionary<string, decimal>? _waveBasis;
    internal Task PendingCommand => _command;
    internal Task PendingWaveform { get; private set; } = Task.CompletedTask;

    public DrumVoicePaneContent(ShellSession shell, DesktopWorkflow workflow)
    {
        _shell = shell;
        _workflow = workflow;
        RowDefinitions = new("*,Auto");
        this.Bind(RowSpacingProperty, this.GetResourceObservable("Space.Gap"));
        var header = new Grid { ColumnDefinitions = new("*,Auto"), RowDefinitions = new("Auto,Auto") };
        var audition = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        audition.Children.Add(_note);
        audition.Children.Add(_velocity);
        audition.Children.Add(_hit);
        foreach (var control in new Control[] { _note, _velocity, _hit }) control.Margin = new(4, 2);
        header.Children.Add(_identity);
        _identity.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(audition);
        SetColumn(audition, 1);
        SizeChanged += (_, _) =>
        {
            bool compact = Bounds.Width < 430;
            SetColumnSpan(_identity, compact ? 2 : 1);
            SetRow(audition, compact ? 1 : 0);
            SetColumn(audition, compact ? 0 : 1);
            SetColumnSpan(audition, compact ? 2 : 1);
            audition.HorizontalAlignment = compact ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        };
        _note.ItemsSource = Enumerable.Range(0, 128).Select(i => PianoNotes.Name(i)).ToArray();
        _note.SelectedIndex = (int)DrumVoice.RootPitch;
        var body = new StackPanel { Classes = { "tight" } };
        body.Children.Add(header);
        var primary = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var parameter in DrumVoice.Parameters.Take(4)) primary.Children.Add(CreateParameter(parameter).Body);
        body.Children.Add(primary);
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
        _plot = new(shell) { Height = 280, MinWidth = 245 };
        body.Children.Add(_plot);
        foreach (var parameter in DrumVoice.Parameters.Skip(4)) _shape.Children.Add(CreateParameter(parameter).Body);
        body.Children.Add(_shape);
        var scroll = new ScrollViewer
        {
            Content = chassis, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        Children.Add(scroll);
        Identify(scroll, "Scroll");
        SetRow(scroll, 0);
        Children.Add(_status);
        SetRow(_status, 1);
        Identify(_pitch, "Pitch");
        Identify(_amplitude, "Amplitude");
        Identify(_hit, "Audition");
        Identify(_note, "Note");
        Identify(_velocity, "Velocity");
        Identify(_identity, "Identity");
        Identify(_status, "Status");
        _pitch.Click += (_, _) => Switch(false);
        _amplitude.Click += (_, _) => Switch(true);
        _hit.Click += async (_, _) =>
        {
            if (_session.Target is not { } target || !_command.IsCompleted) return;
            Cancel();
            _command = workflow.AuditionDrumAsync(target, workflow.State.Generation,
                _note.SelectedIndex >= 0 ? _note.SelectedIndex : DrumVoice.RootPitch, (decimal)_velocity.Value);
            Refresh();
            await _command;
            Refresh();
        };
        _note.SelectionChanged += (_, _) =>
        {
            _waveBasis = null;
            if (!_refreshing) RefreshPreview();
        };
        _plot.Begin = Begin;
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
        var panel = new Grid { RowDefinitions = new("Auto,Auto,Auto"), Width = 132, Margin = new(4, 4) };
        var label = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center };
        var dial = new GainDialControl(_shell)
        {
            Width = 84, Height = 84, Focusable = true,
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
        dial.Begin = Begin;
        dial.Preview += value => Preview(parameter.Key, Denormalize(parameter, value));
        dial.Completed += async () => await CommitAsync();
        dial.Cancelled += () =>
        {
            if (!_refreshing) Cancel();
        };
        exact.PropertyChanged += (_, change) =>
        {
            if (_refreshing || change.Property != TextBox.TextProperty || !Begin()) return;
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

    private bool Begin()
    {
        if (_session.Observe(_workflow.State, _session.Target))
        {
            Refresh();
            return false;
        }

        bool started = !_disposed && _command.IsCompleted && _session.Begin();
        if (started) _notice.Stop();
        return started;
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
        if (_session.Observe(_workflow.State, target))
        {
            _refreshing = true;
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
            int uses = _workflow.State.Snapshot.State.Patterns.SelectMany(p => p.Parts)
                .Count(p => p.SoundId == sound?.Id);
            _identity.Text = sound?.Name + (uses > 1 ? " · " + _shell["Drum.Shared"] + " " + uses : "");
            _pitch.Content = _shell["Drum.Pitch"];
            _amplitude.Content = _shell["Drum.Amplitude"];
            _hit.Content = _shell["Drum.Audition"];
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
                p.Body.IsVisible = DrumVoice.Parameters.IndexOf(p.Parameter) < 4 ||
                                   _plot.AmplitudeMode == (p.Parameter.Key is "attack" or "amplitudeCurve");
            }

            _hit.IsEnabled = _session.CanEdit && _command.IsCompleted && !_workflow.State.CanStop;
            _plot.IsEnabled = _session.CanEdit && _command.IsCompleted;
            AutomationProperties.SetName(_hit, _shell["Drum.Audition"]);
            AutomationProperties.SetName(_note, _shell["Drum.Note"]);
            AutomationProperties.SetName(_velocity, _shell["Drum.Velocity"]);
            AutomationProperties.SetName(_plot, _shell[_plot.AmplitudeMode ? "Drum.Amplitude" : "Drum.Pitch"]);
            ToolTip.SetTip(_note, _shell["Drum.Note"]);
            ToolTip.SetTip(_velocity, _shell["Drum.Velocity"]);
            ToolTip.SetTip(_plot, _shell["Drum.EnvelopeHint"]);
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
                ? DrumVoice.Frequency(values).ToString("0.0")
                : "";
            p.Dial.TrackRole = _shell.Preferences.Theme == HostTheme.Light ? "Graph.NodeBorder" : "Control.Track";
            p.Dial.HasRecessedWell = _shell.Preferences.Theme == HostTheme.Light;
            p.Dial.InvalidateVisual();
            if (updateText)
            {
                p.Exact.Text =
                    (p.Parameter.Unit == "%" ? value * 100 : value).ToString("0.###", CultureInfo.InvariantCulture);
                p.Exact.Classes.Remove("invalid");
            }
        }

        _plot.Values = values;
        _plot.BasePitch = _note.SelectedIndex >= 0 ? _note.SelectedIndex : DrumVoice.RootPitch;
        _plot.IsDraft = _session.Draft is not null;
        _plot.InvalidateVisual();
        _status.Text = _session.ChangedElsewhere ? _shell["NodeEditor.ChangedElsewhere"] :
            _session.Draft is not null ? _shell["NodeEditor.Draft"] : "";
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
            PendingWaveform = RenderWaveformAsync(values, _note.SelectedIndex >= 0 ? _note.SelectedIndex : 36,
                _waveCancellation.Token);
        }
    }

    private async Task RenderWaveformAsync(ImmutableDictionary<string, decimal> values, decimal pitch,
        CancellationToken cancellation)
    {
        try
        {
            var envelope = await Task.Run(() =>
            {
                using var pcm = DrumVoice.Render(values, 48000, pitch, cancellation);
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

    private async Task CommitAsync()
    {
        if (!_command.IsCompleted || _disposed) return;
        _session.Observe(_workflow.State, _session.Target);
        var proposal = _session.Complete();
        if (proposal is null)
        {
            Refresh();
            return;
        }

        _command = _workflow.SetDrumParametersAsync(proposal);
        Refresh();
        await _command;
        if (!_disposed) Observe(_session.Target);
    }

    public void Cancel()
    {
        _session.Cancel();
        _notice.Stop();
        foreach (var p in _parameters) p.Dial.Cancel();
        _plot.Cancel();
        if (!_disposed) Refresh();
    }

    public void Suspend()
    {
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
