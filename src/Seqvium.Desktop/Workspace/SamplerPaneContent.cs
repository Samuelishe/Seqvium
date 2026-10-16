// SPDX-License-Identifier: Apache-2.0
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

/// <summary>Specialized Source surface. Owns only disposable waveform observations and edit proposals.</summary>
internal sealed class SamplerPaneContent : ScrollViewer, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly SamplerWaveform _wave;
    private readonly TextBlock _scope = new() { TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _status = new() { Classes = { "caption", "warning" }, TextWrapping = TextWrapping.Wrap };
    private readonly Dictionary<string, TextBox> _fields = [];
    private readonly Dictionary<string, TextBlock> _labels = [];
    private readonly CheckBox _loop = new();
    private readonly Slider _pan = new() { Minimum = 0, Maximum = 0 };
    private readonly List<(Button Button, string Key)> _buttons = [];
    private readonly Button _apply, _hold;
    private NodeEditorTarget? _target;
    private SoundDefinition? _sound;
    private long _generation = -1;
    private Guid _lifecycle;
    private ResourceDescriptor? _resource;
    private BrowserAudioInfo? _audio;
    private CancellationTokenSource? _inspection;
    private SamplerPlayback? _draft;
    private bool _refreshing, _disposed;
    private bool _held, _inspectionError, _textDraft, _stopping;
    internal Task PendingInspection { get; private set; } = Task.CompletedTask;
    internal Task PendingCommand { get; private set; } = Task.CompletedTask;

    internal SamplerPaneContent(ShellSession shell, DesktopWorkflow workflow)
    {
        _shell = shell; _workflow = workflow;
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
        var body = new StackPanel();
        body.Bind(StackPanel.SpacingProperty, this.GetResourceObservable("Space.Gap"));
        _wave = new(shell) { Height = 240, MinHeight = 180 };
        Identify(_wave, "Waveform");
        _wave.Begin = () => Ready;
        _wave.Preview += p => { _textDraft = false; _draft = p; FillFields(p); };
        _wave.Completed += () => Run(CommitAsync);
        _wave.Cancelled += () => { _draft = null; _textDraft = false; Refresh(); };
        var toolbar = new WrapPanel { Orientation = Orientation.Horizontal };
        _hold = Button("Hold", async () =>
        {
            if (_held) { await _workflow.ReleaseSamplerAsync(); _held = false; }
            else if (_target is { } target)
            {
                _held = await _workflow.HoldSamplerAsync(target, _generation);
                Refresh();
                if (!_held) { _status.Text = _shell[_workflow.State.Detail ?? "Piano.AuditionUnavailable"]; _status.IsVisible = true; }
                return;
            }
            Refresh();
        });
        toolbar.Children.Add(_hold);
        toolbar.Children.Add(Button("Stop", async () => { await _workflow.StopSamplerAsync(); _held = false; Refresh(); }));
        toolbar.Children.Add(Button("ZoomIn", () => { _wave.Zoom(.5); return Task.CompletedTask; }));
        toolbar.Children.Add(Button("ZoomOut", () => { _wave.Zoom(2); return Task.CompletedTask; }));
        toolbar.Children.Add(Button("Fit", () => { _wave.Fit(); return Task.CompletedTask; }));
        _apply = Button("Apply", CommitAsync); toolbar.Children.Add(_apply);
        body.Children.Add(_scope); body.Children.Add(toolbar); body.Children.Add(_status); body.Children.Add(_wave);
        Identify(_pan, "Pan");
        _wave.ViewChanged += () =>
        {
            _refreshing = true;
            _pan.Maximum = Math.Max(0, (_audio?.Frames ?? 0) - _wave.ViewLength);
            _pan.Value = _wave.ViewStart; _pan.IsVisible = _pan.Maximum > 0;
            _refreshing = false;
        };
        _pan.PropertyChanged += (_, e) => { if (!_refreshing && e.Property == Slider.ValueProperty) _wave.Pan((long)_pan.Value); };
        body.Children.Add(_pan);
        Identify(_loop, "Loop");
        _loop.IsCheckedChanged += (_, _) =>
        {
            if (_refreshing || !Ready || !TryRead(out var value)) return;
            if (_loop.IsChecked == true && value.LoopEnd == 0) value = value with { LoopStart = value.Start, LoopEnd = value.End };
            _draft = value with { Loop = _loop.IsChecked == true };
            FillFields(_draft.Value); _wave.Playback = _draft.Value; _wave.InvalidateVisual();
            Run(CommitAsync);
        };
        body.Children.Add(_loop);
        var fields = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (string key in new[] { "Start", "End", "LoopStart", "LoopEnd" })
        {
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            var text = new TextBox { Width = 96, Classes = { "numeric" } };
            var row = new Grid { ColumnDefinitions = new("104,96"), Classes = { "parameterRow" } };
            row.Bind(MarginProperty, this.GetResourceObservable("Padding.Parameter"));
            row.Children.Add(label); row.Children.Add(text); Grid.SetColumn(text, 1);
            Identify(text, key); Identify(label, "Label." + key);
            _fields.Add(key, text); _labels.Add(key, label); fields.Children.Add(row);
            text.PropertyChanged += (_, e) =>
            {
                if (_refreshing || e.Property != TextBox.TextProperty || !Ready) return;
                _textDraft = true;
                if (TryRead(out var p))
                {
                    try { p.Validate(_audio!.Frames); _draft = p; _wave.Playback = p; _wave.InvalidateVisual(); }
                    catch (ArgumentException) { _status.Text = _shell["Sampler.Invalid"]; _status.IsVisible = true; }
                }
            };
            text.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter) { e.Handled = true; Run(CommitAsync); }
                if (e.Key == Key.Escape) { e.Handled = true; _draft = null; _textDraft = false; Refresh(); }
            };
        }
        body.Children.Add(fields);
        Content = body;
    }

    private bool Ready => !_disposed && _sound is not null && _audio is not null && _target is not null &&
        !_workflow.State.Busy && PendingCommand.IsCompleted && _workflow.State.Generation == _generation;
    private static void Identify(Control control, string id) => AutomationProperties.SetAutomationId(control, "Sampler." + id);
    private Button Button(string key, Func<Task> action)
    {
        var button = new Button(); Identify(button, key); _buttons.Add((button, key));
        button.Click += (_, _) => Run(action);
        return button;
    }
    private void Run(Func<Task> command)
    {
        if (_disposed || !PendingCommand.IsCompleted) return;
        PendingCommand = ExecuteAsync(command);
    }
    private async Task ExecuteAsync(Func<Task> command)
    {
        try { await command(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        { _status.Text = _shell["Sampler.Invalid"]; _status.IsVisible = true; }
    }

    internal void Observe(NodeEditorTarget? target)
    {
        if (_disposed) return;
        var state = _workflow.State;
        bool changed = _generation != state.Generation || _target != target || _lifecycle != state.Document.LifecycleId;
        if (changed)
        {
            _wave.Cancel(); _draft = null; _textDraft = false; _held = false;
            if (_target != target) Suspend();
        }
        _target = target; _generation = state.Generation; _lifecycle = state.Document.LifecycleId;
        _sound = target is not null ? DrumEditorSession.Resolve(state.Snapshot.State, target) : null;
        if (_sound is null || !SamplerPlayback.Supports(_sound))
        {
            _inspection?.Cancel(); _inspection?.Dispose(); _inspection = null;
            _audio = null; _resource = null; _wave.Waveform = null; _wave.InvalidateVisual(); return;
        }
        var resource = state.Snapshot.State.Resources.Single(r => r.Id == _sound.ResourceIds.Single());
        if (_resource != resource || _audio is null && changed)
        {
            _inspection?.Cancel(); _inspection?.Dispose(); _inspection = new();
            _resource = resource; _audio = null; _wave.Waveform = null; _inspectionError = false;
            PendingInspection = InspectAsync(target!, _generation, resource, _inspection.Token);
        }
        Refresh();
    }

    private async Task InspectAsync(NodeEditorTarget target, long generation, ResourceDescriptor resource, CancellationToken cancellation)
    {
        try
        {
            var info = await _workflow.InspectSamplerAsync(target, generation, cancellation);
            if (_disposed || cancellation.IsCancellationRequested || _target != target || _resource != resource ||
                _workflow.State.Generation != generation) return;
            SamplerPlayback.Read(_sound!, info.Frames);
            _audio = info; _wave.Waveform = info.Waveform; _wave.Fit(); Refresh();
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException)
        { if (!_disposed && !cancellation.IsCancellationRequested) { _inspectionError = true; Refresh(); } }
    }
    private bool TryRead(out SamplerPlayback p)
    {
        p = default;
        bool Frame(string key, out long value) => long.TryParse(_fields[key].Text,
            NumberStyles.None, CultureInfo.InvariantCulture, out value);
        if (!Frame("Start", out var start) || !Frame("End", out var end) ||
            !Frame("LoopStart", out var loopStart) || !Frame("LoopEnd", out var loopEnd)) return false;
        p = new(start, end, _loop.IsChecked == true, loopStart, loopEnd); return true;
    }
    private void FillFields(SamplerPlayback p)
    {
        _refreshing = true;
        _fields["Start"].Text = p.Start.ToString(CultureInfo.InvariantCulture);
        _fields["End"].Text = p.End.ToString(CultureInfo.InvariantCulture);
        _fields["LoopStart"].Text = p.LoopStart.ToString(CultureInfo.InvariantCulture);
        _fields["LoopEnd"].Text = p.LoopEnd.ToString(CultureInfo.InvariantCulture);
        _loop.IsChecked = p.Loop;
        _refreshing = false;
    }
    private async Task CommitAsync()
    {
        if (_target is null || _audio is null || !TryRead(out var p))
        { _status.Text = _shell["Sampler.Invalid"]; _status.IsVisible = true; return; }
        try { p.Validate(_audio.Frames); }
        catch (ArgumentException) { _status.Text = _shell["Sampler.Invalid"]; _status.IsVisible = true; return; }
        bool changed = await _workflow.SetSamplerPlaybackAsync(_target, _generation, p);
        _draft = null; _textDraft = false; Refresh();
        if (!changed && _workflow.State.Detail is not null) { _status.Text = _shell[_workflow.State.Detail]; _status.IsVisible = true; }
    }
    private void Refresh()
    {
        if (_disposed || _sound is null) return;
        foreach (var (button, key) in _buttons)
        {
            button.Content = _shell["Sampler." + (key == "Hold" && _held ? "Release" : key)];
            AutomationProperties.SetName(button, button.Content.ToString());
            button.IsEnabled = _audio is not null && !_workflow.State.Busy;
        }
        _scope.Text = _sound.Name + " · " + _shell["Sampler.Shared"];
        ToolTip.SetTip(_scope, _shell["Sampler.SharedTip"]);
        _loop.Content = _shell["Sampler.Loop"]; _loop.IsEnabled = _audio?.Frames >= 4 && !_workflow.State.Busy;
        foreach (var (key, text) in _fields)
        {
            _labels[key].Text = _shell["Sampler." + key];
            AutomationProperties.SetName(text, _shell["Sampler." + key]);
            ToolTip.SetTip(text, _shell["Sampler.Frames"]);
            text.IsEnabled = _audio is not null && !_workflow.State.Busy;
        }
        if (_audio is null) { _status.Text = _shell[_inspectionError ? "Sampler.Unavailable" : "Sampler.Loading"]; _status.IsVisible = true; return; }
        var playback = _draft ?? SamplerPlayback.Read(_sound, _audio.Frames);
        if (_draft is null && !_textDraft) FillFields(playback);
        _wave.Playback = playback; _wave.InvalidateVisual();
        _status.IsVisible = false;
        if (_textDraft)
        {
            bool valid = TryRead(out var entered);
            if (valid) try { entered.Validate(_audio.Frames); } catch (ArgumentException) { valid = false; }
            if (!valid) { _status.Text = _shell["Sampler.Invalid"]; _status.IsVisible = true; }
        }
    }
    internal void Suspend()
    {
        _wave.Cancel(); _draft = null; _textDraft = false;
        if (_stopping || !_held && PendingCommand.IsCompleted && !_workflow.SamplerPreviewPlaying) return;
        _stopping = true;
        PendingCommand = StopAfterAsync(PendingCommand);
    }
    private async Task StopAfterAsync(Task pending)
    {
        try { await pending; await _workflow.StopSamplerAsync(); _held = false; Refresh(); }
        finally { _stopping = false; }
    }
    public void Dispose()
    {
        if (_disposed) return;
        Suspend(); _disposed = true; _inspection?.Cancel(); _inspection?.Dispose();
        _audio = null; _wave.Waveform = null;
    }
}
