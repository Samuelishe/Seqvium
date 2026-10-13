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

internal enum ArrangementSnap { Bar, Beat, Sixteenth, Off }

internal sealed class ArrangementCanvas : Control
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private WorkflowState _observation;
    private ArrangementClip? _draft, _original;
    private ArrangementEditBasis _basis;
    private IPointer? _pointer;
    private Point _pressed;
    private bool _resize;
    private bool _copy;
    private double _barWidth = 96;
    private Rect _viewport;
    internal Id<ArrangementClip>? Selected { get; private set; }
    internal bool HasGesture => _pointer is not null;
    internal double PlayheadX => HeaderWidth + (double)PositionTicks / BarTicks * _barWidth;
    internal double TotalTicks => (Width - HeaderWidth) / _barWidth * BarTicks;
    internal double TickPixels => _barWidth / BarTicks;
    internal ArrangementSnap Snap { get; set; }
    internal long SnapTicks => Snap switch { ArrangementSnap.Beat => _observation.Snapshot.State.Settings.Meter.TicksPerBeat,
        ArrangementSnap.Sixteenth => MusicalPosition.TicksPerQuarter / 4, ArrangementSnap.Off => 1, _ => BarTicks };
    internal ArrangementEditBasis Basis => ArrangementEditBasis.From(_observation);
    internal long PositionTicks => _observation.SongMode && _observation.Execution is { } execution
        ? Math.Max(0, ConstantTempoConversion.FromFrames(execution.Execution.Position, _observation.Snapshot.State.Settings.Tempo,
            _observation.ExecutionSampleRate).Ticks) : 0;
    internal event Action? SelectionChanged;
    internal event Action<Task>? CommandSubmitted;
    internal event Action<Id<GraphAttachment>>? RackRequested;
    internal event Action<int, string>? TrackRequested;
    private double LaneHeight => HostVisual.Metric(this, "Playlist.LaneHeight");
    private double RulerHeight => HostVisual.Metric(this, "Playlist.RulerHeight");
    private double HeaderWidth => HostVisual.Metric(this, "Playlist.HeaderWidth");
    private long BarTicks => _observation.Snapshot.State.Settings.Meter.TicksPerBar;
    private double HeaderX => Math.Max(0, _viewport.Left);

    internal ArrangementCanvas(ShellSession shell, DesktopWorkflow workflow)
    {
        _shell = shell; _workflow = workflow; _observation = workflow.State;
        Focusable = true; ClipToBounds = true;
        AutomationProperties.SetAutomationId(this, "Arrangement.Timeline");
        PointerCaptureLost += (_, _) => Cancel();
        EffectiveViewportChanged += (_, e) => { _viewport = e.EffectiveViewport; InvalidateVisual(); };
    }

    internal void Adopt(WorkflowState observation)
    {
        if (observation.Document.LifecycleId != _observation.Document.LifecycleId) Selected = null;
        if (_draft is not null && (observation.Document.LifecycleId != _basis.Lifecycle || observation.Generation != _basis.Generation)) Cancel();
        _observation = observation;
        if (!observation.Snapshot.State.ArrangementClips.Any(c => c.Id == Selected)) Selected = null;
        int lanes = Math.Max(8, observation.Snapshot.State.ArrangementClips.Select(c => c.Lane + 2).DefaultIfEmpty(0).Max());
        long bars = Math.Min(4096, Math.Max(32, ArrangementPreparation.EndTicks(observation.Snapshot.State) / BarTicks + 8));
        Width = HeaderWidth + bars * _barWidth; Height = RulerHeight + Math.Min(32, lanes) * LaneHeight;
        AutomationProperties.SetName(this, _shell["Arrangement.Hint"]);
        InvalidateVisual();
    }

    internal void Zoom(bool closer)
    {
        Cancel(); _barWidth = Math.Clamp(_barWidth * (closer ? 2 : 0.5), 24, 192); Adopt(_observation);
    }

    internal void Fit(double width)
    {
        Cancel();
        double bars = Math.Max(4, (double)ArrangementPreparation.EndTicks(_observation.Snapshot.State) / BarTicks + 1);
        _barWidth = Math.Clamp((width - HeaderWidth) / bars, 8, 192); Adopt(_observation);
    }

    private Pattern PatternFor(ArrangementClip clip)
    {
        var state = _observation.Snapshot.State;
        return state.Patterns.Single(p => p.Id == state.Placements.Single(p => p.Id == clip.SourcePlacementId).PatternId);
    }

    internal Rect ClipRect(ArrangementClip clip)
        => new(HeaderWidth + (double)clip.Position.Ticks / BarTicks * _barWidth + 2,
            RulerHeight + clip.Lane * LaneHeight + 5,
            Math.Max(8, (double)PatternFor(clip).Length.Ticks * clip.Repeats / BarTicks * _barWidth - 4), LaneHeight - 10);

    private ArrangementClip? Hit(Point point) => _observation.Snapshot.State.ArrangementClips.Reverse()
        .FirstOrDefault(clip => ClipRect(clip).Contains(point));

    public override void Render(DrawingContext context)
    {
        var theme = _shell.Preferences.Theme;
        IBrush Brush(string key) => HostVisual.Brush(this, theme, key);
        context.DrawRectangle(Brush("Surface.Workspace"), null, new Rect(Bounds.Size));
        context.DrawRectangle(Brush("Surface.Raised"), null, new Rect(0, 0, Bounds.Width, RulerHeight));
        var grid = new Pen(Brush("Graph.Grid"), 1);
        double visibleLeft = Math.Max(HeaderWidth, _viewport.Left), visibleRight = _viewport.Width > 0 ? _viewport.Right : Bounds.Width;
        for (int bar = Math.Max(0, (int)((visibleLeft - HeaderWidth) / _barWidth)); HeaderWidth + bar * _barWidth < visibleRight; bar++)
        {
            double x = HeaderWidth + bar * _barWidth;
            context.DrawLine(new Pen(Brush("Border.Default"), 1), new(x, RulerHeight), new(x, Bounds.Height));
            if (_barWidth >= 48 || bar % 4 == 0) HostVisual.Caption(this, _shell, context, (bar + 1).ToString(), new(x + 6, 8));
            if (_barWidth >= 96)
                for (int beat = 1; beat < _observation.Snapshot.State.Settings.Meter.Numerator; beat++)
                    context.DrawLine(grid, new(x + beat * _barWidth / _observation.Snapshot.State.Settings.Meter.Numerator, RulerHeight),
                        new(x + beat * _barWidth / _observation.Snapshot.State.Settings.Meter.Numerator, Bounds.Height));
        }
        foreach (var clip in _observation.Snapshot.State.ArrangementClips)
            DrawClip(context, !_copy && _draft?.Id == clip.Id ? _draft : clip, clip.Id == Selected);
        if (_copy && _draft is not null) DrawClip(context, _draft, true);
        for (int lane = 0; RulerHeight + lane * LaneHeight < Bounds.Height; lane++)
        {
            double y = RulerHeight + lane * LaneHeight;
            var track = _observation.Snapshot.State.ArrangementTracks.SingleOrDefault(track => track.Lane == lane);
            int color = track?.ColorIndex ?? lane % 8;
            context.DrawRectangle(Brush("Material.Panel"), null, new Rect(HeaderX, y, HeaderWidth, LaneHeight));
            context.DrawRectangle(Brush($"Rack.Channel{color}"), null, new Rect(HeaderX, y + 1, 4, LaneHeight - 2));
            context.DrawLine(grid, new(HeaderX, y), new(Bounds.Width, y));
            using (context.PushClip(new Rect(HeaderX + 10, y + 5, HeaderWidth - 16, 23)))
                context.DrawText(new FormattedText(track?.Name ?? _shell["Arrangement.Lane"] + " " + (lane + 1),
                    System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, HostVisual.Typeface(this),
                    HostVisual.Metric(this, "Type.Body"), Brush("Text.Primary")), new(HeaderX + 12, y + 8));
            DrawTrackButton("M", track?.Muted == true, HeaderX + 12, y + 33);
            DrawTrackButton("S", track?.Solo == true, HeaderX + 48, y + 33);
            HostVisual.Caption(this, _shell, context, (lane + 1).ToString("00"), new(HeaderX + 110, y + 40));
        }
        context.DrawRectangle(Brush("Material.Panel"), null, new Rect(HeaderX, 0, HeaderWidth, RulerHeight));
        HostVisual.Caption(this, _shell, context, _shell["Arrangement.Tracks"], new(HeaderX + 12, 8));
        if (_observation.SongMode && _observation.Execution is not null)
        {
            double x = PlayheadX;
            var accent = Brush("Accent.Action");
            if (x >= HeaderX + HeaderWidth)
            {
                context.DrawLine(new Pen(accent, 2), new(x, 0), new(x, Bounds.Height));
                context.DrawRectangle(accent, null, new Rect(x - 4, 0, 8, 6), 2, 2);
            }
        }

        void DrawTrackButton(string text, bool active, double x, double y)
        {
            context.DrawRectangle(Brush(active ? "Surface.Selected" : "Material.Button"), new Pen(Brush(active ? "Accent.Action" : "Border.Default"), 1),
                new Rect(x, y, 30, 30), HostVisual.Metric(this, "Control.Corner"), HostVisual.Metric(this, "Control.Corner"));
            HostVisual.Caption(this, _shell, context, text, new(x + 10, y + 7));
        }
    }

    private void DrawClip(DrawingContext context, ArrangementClip clip, bool selected)
    {
        var pattern = PatternFor(clip); var rect = ClipRect(clip); var theme = _shell.Preferences.Theme;
        if (_viewport.Width > 0 && !rect.Intersects(_viewport)) return;
        var track = _observation.Snapshot.State.ArrangementTracks.SingleOrDefault(track => track.Lane == clip.Lane);
        int index = track?.ColorIndex ?? clip.Lane % 8;
        using var opacity = context.PushOpacity(ArrangementPreparation.IsAudible(_observation.Snapshot.State, clip.Lane) ? 1 : .35);
        var color = HostVisual.Brush(this, theme, $"Rack.Channel{index}");
        context.DrawRectangle(HostVisual.Brush(this, theme, $"Rack.Step{index}"), new Pen(selected
            ? HostVisual.Brush(this, theme, "Accent.Action") : color, selected ? 2 : 1), rect,
            HostVisual.Metric(this, "Control.Corner"), HostVisual.Metric(this, "Control.Corner"));
        using (context.PushClip(rect.Deflate(3)))
        {
            context.DrawRectangle(HostVisual.Brush(this, theme, "Material.Bevel"), null, new Rect(rect.X, rect.Y, rect.Width, 22));
            context.DrawRectangle(color, null, new Rect(rect.X, rect.Y, 4, rect.Height));
            context.DrawText(new FormattedText(pattern.Name + (clip.Repeats > 1 ? $"  ×{clip.Repeats}" : ""),
                System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, HostVisual.Typeface(this),
                HostVisual.Metric(this, "Type.Body"), HostVisual.Brush(this, theme, "Text.Primary")), new(rect.X + 10, rect.Y + 4));
            double cycle = (double)pattern.Length.Ticks / BarTicks * _barWidth;
            int first = _viewport.Width > 0 ? Math.Clamp((int)((_viewport.Left - rect.X) / cycle), 0, clip.Repeats - 1) : 0;
            int last = _viewport.Width > 0 ? Math.Clamp((int)Math.Ceiling((_viewport.Right - rect.X) / cycle), first + 1, clip.Repeats) : clip.Repeats;
            for (int repeat = first; repeat < last; repeat++)
            {
                double left = rect.X + repeat * cycle;
                if (repeat > 0) context.DrawLine(new Pen(color, 1), new(left, rect.Y + 20), new(left, rect.Bottom));
                decimal minimumPitch = pattern.Parts.SelectMany(p => p.Notes).Select(n => n.Pitch).DefaultIfEmpty(60).Min();
                decimal maximumPitch = pattern.Parts.SelectMany(p => p.Notes).Select(n => n.Pitch).DefaultIfEmpty(60).Max();
                for (int row = 0; row < pattern.Parts.Length; row++)
                    foreach (var note in pattern.Parts[row].Notes)
                    {
                        double x = left + (double)note.Position.Ticks / pattern.Length.Ticks * cycle;
                        double y = rect.Y + 27 + (maximumPitch > minimumPitch
                            ? (double)((maximumPitch - note.Pitch) / (maximumPitch - minimumPitch)) * (rect.Height - 34)
                            : row * (rect.Height - 32) / Math.Max(1, pattern.Parts.Length));
                        double duration = (double)note.Duration.Ticks / pattern.Length.Ticks * cycle;
                        context.DrawRectangle(color, null, new Rect(x + 2, y, Math.Max(2, duration - 3), 4), 1, 1);
                    }
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || _observation.Busy) return;
        Focus(); var point = e.GetPosition(this); var clip = Hit(point);
        e.Handled = true;
        if (point.X >= HeaderX && point.X < HeaderX + HeaderWidth && point.Y >= RulerHeight)
        {
            int lane = Math.Clamp((int)((point.Y - RulerHeight) / LaneHeight), 0, 31);
            double localY = (point.Y - RulerHeight) % LaneHeight;
            string command = localY >= 30 && point.X - HeaderX is >= 12 and < 42 ? "Mute" :
                localY >= 30 && point.X - HeaderX is >= 48 and < 78 ? "Solo" : "Edit";
            TrackRequested?.Invoke(lane, command); return;
        }
        Selected = clip?.Id; SelectionChanged?.Invoke();
        if (clip is null)
        {
            if (point.X < HeaderWidth || point.Y < RulerHeight || _observation.Selected is not { } source) return;
            long ticks = Math.Max(0, (long)Math.Floor((point.X - HeaderWidth) / _barWidth * BarTicks / SnapTicks) * SnapTicks);
            int lane = Math.Clamp((int)((point.Y - RulerHeight) / LaneHeight), 0, 31);
            CommandSubmitted?.Invoke(_workflow.AddArrangementClipAsync(ArrangementEditBasis.From(_observation), source,
                new(ticks), lane));
            return;
        }
        if (e.ClickCount == 2)
        {
            var placement = _observation.Snapshot.State.Placements.Single(p => p.Id == clip.SourcePlacementId);
            var attachment = _observation.Snapshot.State.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId);
            RackRequested?.Invoke(attachment.Id); return;
        }
        _original = _draft = clip; _basis = ArrangementEditBasis.From(_observation); _pressed = point;
        _resize = point.X >= ClipRect(clip).Right - 10;
        _copy = !_resize && e.KeyModifiers.HasFlag(KeyModifiers.Control);
        _pointer = e.Pointer; e.Pointer.Capture(this); InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_original is not { } original || _pointer != e.Pointer) return;
        var point = e.GetPosition(this);
        if (Math.Abs(point.X - _pressed.X) + Math.Abs(point.Y - _pressed.Y) < 4) return;
        if (_resize)
        {
            double length = (point.X - HeaderWidth - (double)original.Position.Ticks / BarTicks * _barWidth) / _barWidth * BarTicks;
            _draft = original with { Repeats = (int)Math.Clamp(Math.Round(length / PatternFor(original).Length.Ticks), 1, 1024) };
        }
        else
        {
            long snap = e.KeyModifiers.HasFlag(KeyModifiers.Alt) ? 1 :
                e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? _observation.Snapshot.State.Settings.Meter.TicksPerBeat : SnapTicks;
            long ticks = (long)Math.Clamp(Math.Round((original.Position.Ticks + (point.X - _pressed.X) / _barWidth * BarTicks) / snap), 0, 4095d * BarTicks / snap) * snap;
            int lane = Math.Clamp(original.Lane + (int)Math.Round((point.Y - _pressed.Y) / LaneHeight), 0, 31);
            _draft = original with { Position = new(ticks), Lane = lane };
        }
        InvalidateVisual(); e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointer != e.Pointer || _draft is not { } draft || _original is not { } original) return;
        var basis = _basis; bool copy = _copy; Cancel(); e.Handled = true;
        if (draft != original) CommandSubmitted?.Invoke(copy
            ? _workflow.CopyArrangementClipAsync(basis, draft.Id, draft.Position, draft.Lane)
            : _workflow.UpdateArrangementClipAsync(basis, draft.Id, draft.Position, draft.Lane, draft.Repeats));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape) { Cancel(); e.Handled = true; return; }
        if (_observation.Busy) return;
        if (e.Key is Key.Home or Key.End && !_observation.Snapshot.State.ArrangementClips.IsEmpty)
        {
            var ordered = _observation.Snapshot.State.ArrangementClips.OrderBy(c => c.Position.Ticks).ThenBy(c => c.Lane).ToArray();
            Selected = (e.Key == Key.Home ? ordered[0] : ordered[^1]).Id;
            SelectionChanged?.Invoke(); e.Handled = true; return;
        }
        var clip = _observation.Snapshot.State.ArrangementClips.SingleOrDefault(c => c.Id == Selected);
        if (clip is null) return;
        var basis = ArrangementEditBasis.From(_observation);
        if (e.Key == Key.Enter)
        {
            var placement = _observation.Snapshot.State.Placements.Single(p => p.Id == clip.SourcePlacementId);
            if (_observation.Snapshot.State.GraphAttachments.SingleOrDefault(a => a.ContextId == placement.ItemContextId) is { } attachment)
                RackRequested?.Invoke(attachment.Id);
            e.Handled = true; return;
        }
        Task? command = e.Key switch
        {
            Key.Delete => _workflow.DeleteArrangementClipAsync(basis, clip.Id),
            Key.D when e.KeyModifiers == KeyModifiers.Control => _workflow.DuplicateArrangementClipAsync(basis, clip.Id),
            Key.Left or Key.Right or Key.Up or Key.Down => _workflow.UpdateArrangementClipAsync(basis, clip.Id,
                new(e.Key == Key.Left ? Math.Max(0, clip.Position.Ticks - BarTicks) :
                    e.Key == Key.Right && clip.Position.Ticks <= long.MaxValue - BarTicks ? clip.Position.Ticks + BarTicks : clip.Position.Ticks),
                Math.Clamp(clip.Lane + (e.Key == Key.Up ? -1 : e.Key == Key.Down ? 1 : 0), 0, 31), clip.Repeats),
            _ => null
        };
        if (command is null) return;
        Cancel(); e.Handled = true; CommandSubmitted?.Invoke(command);
    }

    internal void Cancel()
    {
        var pointer = _pointer; _pointer = null; _draft = _original = null; _copy = false;
        pointer?.Capture(null); InvalidateVisual();
    }
}
