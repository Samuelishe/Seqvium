// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Song overview and viewport navigation derived from canonical clips and renderer position.</summary>
internal sealed class ArrangementOverview : Control
{
    private readonly ShellSession _shell;
    private readonly Action<double> _navigate;
    private WorkflowState? _state;
    private double _totalTicks = 1, _left, _right, _position;
    private IPointer? _pointer;
    private double _grab;
    private int _edge;
    internal event Action<double, double>? ViewportRequested;
    internal ArrangementOverview(ShellSession shell, Action<double> navigate)
    {
        _shell = shell; _navigate = navigate;
        this.Bind(HeightProperty, this.GetResourceObservable("Playlist.OverviewHeight"));
        AutomationProperties.SetAutomationId(this, "Arrangement.Overview");
        PointerCaptureLost += (_, _) => _pointer = null;
    }
    internal void Adopt(WorkflowState state, double totalTicks, double left, double right, long position)
    {
        _state = state; _totalTicks = Math.Max(1, totalTicks);
        _left = Math.Clamp(left / _totalTicks, 0, 1); _right = Math.Clamp(right / _totalTicks, _left, 1);
        _position = Math.Clamp(position / _totalTicks, 0, 1);
        AutomationProperties.SetName(this, _shell["Arrangement.Overview"]); InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        var theme = _shell.Preferences.Theme;
        context.DrawRectangle(HostVisual.Brush(this, theme, "Surface.Workspace"), null, new Rect(Bounds.Size));
        if (_state is not { } observation) return;
        var state = observation.Snapshot.State;
        int lanes = Math.Max(4, state.ArrangementClips.Select(c => c.Lane + 1).DefaultIfEmpty(0).Max());
        double row = (Bounds.Height - 6) / lanes;
        foreach (var clip in state.ArrangementClips)
        {
            var placement = state.Placements.Single(p => p.Id == clip.SourcePlacementId);
            long length = state.Patterns.Single(p => p.Id == placement.PatternId).Length.Ticks * clip.Repeats;
            int color = state.ArrangementTracks.SingleOrDefault(t => t.Lane == clip.Lane)?.ColorIndex ?? clip.Lane % 8;
            using var opacity = context.PushOpacity(ArrangementPreparation.IsAudible(state, clip.Lane) ? 1 : .3);
            context.DrawRectangle(HostVisual.Brush(this, theme, $"Rack.Channel{color}"), null,
                new Rect(clip.Position.Ticks / _totalTicks * Bounds.Width, 3 + clip.Lane * row,
                    Math.Max(1, length / _totalTicks * Bounds.Width), Math.Max(1, row - 1)), 1, 1);
        }
        var accent = HostVisual.Brush(this, theme, "Accent.Action");
        context.DrawRectangle(null, new Pen(accent, 1), new Rect(_left * Bounds.Width, 1,
            Math.Max(2, (_right - _left) * Bounds.Width), Bounds.Height - 2), 2, 2);
        if (observation.SongMode) context.DrawLine(new Pen(accent, 2), new(_position * Bounds.Width, 0), new(_position * Bounds.Width, Bounds.Height));
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        double x = e.GetPosition(this).X, ratio = x / Math.Max(1, Bounds.Width);
        _edge = Math.Abs(x - _left * Bounds.Width) < 8 ? -1 : Math.Abs(x - _right * Bounds.Width) < 8 ? 1 : 0;
        _grab = ratio >= _left && ratio <= _right ? ratio - (_left + _right) / 2 : 0;
        _pointer = e.Pointer; e.Pointer.Capture(this); Navigate(e.GetPosition(this)); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    { base.OnPointerMoved(e); if (_pointer == e.Pointer) { Navigate(e.GetPosition(this)); e.Handled = true; } }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    { base.OnPointerReleased(e); if (_pointer == e.Pointer) { Cancel(); e.Handled = true; } }
    private void Navigate(Point point)
    {
        if (Bounds.Width <= 0) return;
        double ratio = Math.Clamp(point.X / Bounds.Width, 0, 1);
        if (_edge == 0) _navigate(Math.Clamp(ratio - _grab, 0, 1));
        else if (_edge < 0) ViewportRequested?.Invoke(Math.Min(ratio, _right - .005), _right);
        else ViewportRequested?.Invoke(_left, Math.Max(ratio, _left + .005));
    }
    internal void Cancel() { var pointer = _pointer; _pointer = null; pointer?.Capture(null); }
}
