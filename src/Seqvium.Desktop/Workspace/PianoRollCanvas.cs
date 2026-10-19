// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Viewport-sized music surface. All drafts are transient; only release publishes to the document owner.</summary>
internal sealed class PianoRollCanvas : Control
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private WorkflowState _observation;
    private Pattern? _pattern;
    private MusicalPart? _part;
    private BeatEditBasis? _basis;
    private readonly HashSet<Id<NoteEvent>> _selection = [];
    private NoteEvent[]? _preview;
    private Gesture? _gesture;
    private NoteEvent[] _clipboard = [];
    private double _offsetX, _offsetY, _quarterWidth = 104;
    private bool _velocityVisible = true;
    private bool _centerPending, _awaitingCommand;
    private long _lastDuration = MusicalPosition.TicksPerQuarter;
    private decimal _lastIntensity = 0.8m;
    private int _hoverPitch = -1;
    private Point? _hover;

    private sealed record Gesture(
        string Kind,
        Point Start,
        BeatEditBasis Basis,
        Id<MusicalPart> Part,
        NoteEvent[] Before,
        NoteEvent[] Selected,
        IPointer Pointer,
        bool Copy,
        long AnchorTicks,
        decimal AnchorPitch,
        double OffsetX,
        double OffsetY);

    internal bool SelectMode { get; set; }
    internal bool AuditionEdits { get; set; } = true;

    internal long DefaultDuration
    {
        get => _lastDuration;
        set => _lastDuration = Math.Max(1, value);
    }

    internal bool Ghosts { get; set; } = true;
    internal long SnapTicks { get; set; } = BeatSteps.StepTicks;
    internal PianoScale Scale { get; set; }
    internal bool CanQuantize => CanEdit && SnapTicks > 1 && _selection.Count > 0;

    internal bool VelocityVisible
    {
        get => _velocityVisible;
        set
        {
            Cancel();
            _velocityVisible = value;
            ClampView();
        }
    }

    internal bool CanEdit => _basis is not null && !_awaitingCommand && !_observation.IsClosed && !_observation.Busy &&
                             PianoNotes.Refusal(_observation.Snapshot.State, _pattern, _part) is null;

    internal IReadOnlySet<Id<NoteEvent>> Selection => _selection;
    internal MusicalPart? Part => _part;
    internal Pattern? Pattern => _pattern;
    internal BeatEditBasis? Basis => _basis;
    internal double OffsetX => _offsetX;
    internal double OffsetY => _offsetY;
    internal double QuarterWidth => _quarterWidth;
    internal double KeyWidth => HostVisual.Metric(this, "Piano.KeyWidth");
    internal double RowHeight => HostVisual.Metric(this, "Piano.RowHeight");
    internal double RulerHeight => HostVisual.Metric(this, "Piano.RulerHeight");

    internal double VelocityHeight => VelocityVisible
        ? Math.Min(HostVisual.Metric(this, "Piano.VelocityHeight"), Math.Max(48, Bounds.Height / 3))
        : 0;

    internal double NoteBottom => Math.Max(RulerHeight + RowHeight, Bounds.Height - VelocityHeight);

    internal double MaximumX => Math.Max(0,
        (_pattern?.Length.Ticks ?? BeatSteps.BarTicks) / (double)MusicalPosition.TicksPerQuarter * _quarterWidth -
        Math.Max(1, Bounds.Width - KeyWidth));

    internal double MaximumY => Math.Max(0, 128 * RowHeight - (NoteBottom - RulerHeight));
    internal event Action? Changed;
    internal event Action<Task>? CommandSubmitted;
    internal string Feedback { get; private set; } = "";
    internal bool HasGesture => _gesture is not null;

    internal PianoRollCanvas(ShellSession shell, DesktopWorkflow workflow)
    {
        _shell = shell;
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
        _workflow = workflow;
        _observation = workflow.State;
        Focusable = true;
        ClipToBounds = true;
        AutomationProperties.SetAutomationId(this, "Piano.Canvas");
        PointerCaptureLost += (_, e) =>
        {
            if (_gesture?.Pointer == e.Pointer) Cancel();
        };
        SizeChanged += (_, _) =>
        {
            if (_centerPending && Bounds.Height > RulerHeight + 40)
            {
                _centerPending = false;
                CenterPitch();
            }

            ClampView();
        };
    }

    internal void Adopt(WorkflowState observation, Id<MusicalPart>? partId)
    {
        var (pattern, basis) = BeatSteps.Resolve(observation);
        var part = pattern?.Parts.SingleOrDefault(p => p.Id == partId);
        if (observation.Document.LifecycleId != _observation.Document.LifecycleId) Scale = default;
        bool targetChanged = observation.Document.LifecycleId != _observation.Document.LifecycleId ||
                             basis?.Attachment != _basis?.Attachment || part?.Id != _part?.Id;
        if (_gesture is { } gesture && (targetChanged || gesture.Basis.Generation != observation.Generation)) Cancel();
        _observation = observation;
        _pattern = pattern;
        _part = part;
        _basis = basis;
        if (targetChanged)
        {
            _selection.Clear();
            _offsetX = 0;
            _centerPending = Bounds.Height <= RulerHeight + 40;
            CenterPitch();
        }

        if (part is not null && !observation.Busy) _selection.IntersectWith(part.Notes.Select(n => n.Id));
        ClampView();
    }

    private void CenterPitch()
    {
        decimal root = _observation.Snapshot.State.Sounds.SingleOrDefault(s => s.Id == _part?.SoundId)?.Parameters
            .GetValueOrDefault("rootPitch", 60) ?? 60;
        decimal center = _part is { Notes.IsEmpty: false }
            ? (_part.Notes.Min(n => n.Pitch) + _part.Notes.Max(n => n.Pitch)) / 2
            : root;
        _offsetY = (127 - (double)center) * RowHeight - Math.Max(0, (NoteBottom - RulerHeight) / 2 - RowHeight / 2);
    }

    internal void Fit()
    {
        Cancel();
        _offsetX = 0;
        _quarterWidth = Math.Clamp(Math.Max(1, Bounds.Width - KeyWidth - 12) * MusicalPosition.TicksPerQuarter /
                                   (double)(_pattern?.Length.Ticks ?? BeatSteps.BarTicks), 28, 320);
        CenterPitch();
        ClampView();
    }

    internal void SetView(double x, double y)
    {
        _offsetX = x;
        _offsetY = y;
        ClampView();
    }

    private void ClampView()
    {
        _offsetX = Math.Clamp(_offsetX, 0, MaximumX);
        _offsetY = Math.Clamp(_offsetY, 0, MaximumY);
        InvalidateVisual();
        Changed?.Invoke();
    }

    internal void Zoom(double factor, double x)
    {
        Cancel();
        double ticks = (Math.Max(KeyWidth, x) - KeyWidth + _offsetX) / _quarterWidth;
        _quarterWidth = Math.Clamp(_quarterWidth * factor, 28, 320);
        _offsetX = ticks * _quarterWidth - (Math.Max(KeyWidth, x) - KeyWidth);
        ClampView();
    }

    internal double X(long ticks) =>
        KeyWidth + ticks / (double)MusicalPosition.TicksPerQuarter * _quarterWidth - _offsetX;

    internal double Y(decimal pitch) => RulerHeight + (127 - (double)pitch) * RowHeight - _offsetY;

    internal Rect NoteRect(NoteEvent note) => new(X(note.Position.Ticks), Y(note.Pitch) + 2,
        Math.Max(4, note.Duration.Ticks / (double)MusicalPosition.TicksPerQuarter * _quarterWidth), RowHeight - 4);

    private long Ticks(Point point) =>
        (long)Math.Round((point.X - KeyWidth + _offsetX) / _quarterWidth * MusicalPosition.TicksPerQuarter);

    private decimal Pitch(Point point) =>
        Math.Clamp(127 - (int)Math.Floor((point.Y - RulerHeight + _offsetY) / RowHeight), 0, 127);

    private long Unit(KeyModifiers modifiers) => modifiers.HasFlag(KeyModifiers.Alt) ? 1 : SnapTicks;
    private NoteEvent[] Notes => _preview ?? _part?.Notes.ToArray() ?? [];

    private NoteEvent? Hit(Point point) => Notes.OrderBy(n => _selection.Contains(n.Id)).Reverse()
        .FirstOrDefault(n => NoteRect(n).Inflate(2).Contains(point));

    private static bool Black(int pitch) => pitch % 12 is 1 or 3 or 6 or 8 or 10;
    private IBrush Brush(string role) => HostVisual.Brush(this, _shell.Preferences.Theme, role);

    private void Text(DrawingContext context, string text, Point at, string role = "Text.Secondary") =>
        context.DrawText(
            new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, HostVisual.Typeface(this),
                HostVisual.Metric(this, "Type.Caption"), Brush(role)), at);

    internal (FormattedText Text, Point Origin) PitchLabel(decimal pitch, Rect area, string ink)
    {
        var text = new FormattedText(PianoNotes.Name(pitch), CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, HostVisual.Typeface(this), HostVisual.Metric(this, "Type.Caption"), Brush(ink));
        // Sharp-symbol fallback has different line metrics. Center visible ink, not a fixed line origin.
        double inkTop = text.Height + text.OverhangAfter - text.Extent;
        return (text, new(area.X, area.Center.Y - inkTop - text.Extent / 2));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Brush("Surface.Workspace"), new Rect(Bounds.Size));
        if (_pattern is null || _part is null)
        {
            Text(context, _shell["Piano.NoTarget"], new(KeyWidth + 16, RulerHeight + 24));
            return;
        }

        int color = _pattern.Parts.IndexOf(_part) % 8;
        double width = Bounds.Width;
        using (context.PushClip(new Rect(KeyWidth, RulerHeight, Math.Max(1, width - KeyWidth),
                   Math.Max(1, NoteBottom - RulerHeight))))
        {
            for (int pitch = 0; pitch <= 127; pitch++)
            {
                double y = Y(pitch);
                if (y + RowHeight < RulerHeight || y > NoteBottom) continue;
                context.FillRectangle(Brush(Black(pitch) ? "Surface.Background" : "Rack.Ruler"),
                    new(KeyWidth, y, width - KeyWidth, RowHeight));
                if (Scale.Contains(pitch))
                    using (context.PushOpacity(Scale.IsTonic(pitch) ? 0.13 : 0.055))
                        context.FillRectangle(Brush("Accent.Action"), new(KeyWidth, y, width - KeyWidth, RowHeight));
                if (pitch == _hoverPitch)
                    context.FillRectangle(Brush("Surface.Hover"), new(KeyWidth, y, width - KeyWidth, RowHeight));
                context.DrawLine(new Pen(Brush(pitch % 12 == 0 ? "Border.Default" : "Graph.Grid"), 1),
                    new(KeyWidth, y + RowHeight), new(width, y + RowHeight));
            }

            DrawTimeLines(context, NoteBottom);
            double end = X(_pattern.Length.Ticks);
            if (end < width)
            {
                context.FillRectangle(Brush("Surface.Background"),
                    new(Math.Max(KeyWidth, end), RulerHeight, width - Math.Max(KeyWidth, end),
                        NoteBottom - RulerHeight));
            }

            if (Ghosts)
                using (context.PushOpacity(0.22))
                    foreach (var note in _pattern.Parts.Where(p => p.Id != _part.Id).SelectMany(p => p.Notes))
                        context.DrawRectangle(Brush("Text.Secondary"), null, NoteRect(note), 2, 2);
            foreach (var note in Notes.OrderBy(n => _selection.Contains(n.Id))) DrawNote(context, note, color);
            if (_gesture is { Kind: "Select" } select && _hover is { } hover)
                context.DrawRectangle(Brush("Surface.Selected"), new Pen(Brush("Accent.Action"), 1),
                    new Rect(select.Start, hover).Normalize());
            DrawPlayhead(context, NoteBottom);
        }

        DrawKeys(context);
        if (VelocityVisible) DrawVelocity(context, color);
        context.FillRectangle(Brush("Material.Panel"), new(0, 0, width, RulerHeight));
        DrawRuler(context);
        if (IsFocused)
            context.DrawRectangle(null, new Pen(Brush("Focus.Active"), HostVisual.Metric(this, "Focus.Width")),
                new Rect(Bounds.Size).Deflate(1));
    }

    private void DrawTimeLines(DrawingContext context, double bottom)
    {
        long quarter = MusicalPosition.TicksPerQuarter, bar = _observation.Snapshot.State.Settings.Meter.TicksPerBar;
        long unit = SnapTicks > 1 ? SnapTicks : quarter / 4;
        while (unit / (double)quarter * _quarterWidth < 7) unit *= 2;
        long from = Math.Max(0, (long)(_offsetX / _quarterWidth * quarter) / unit * unit);
        long to = (long)((_offsetX + Math.Max(1, Bounds.Width - KeyWidth)) / _quarterWidth * quarter);
        for (long tick = from; tick <= to; tick += unit)
        {
            string role = tick % bar == 0 ? "Border.Default" : tick % quarter == 0 ? "Control.Track" : "Graph.Grid";
            context.DrawLine(new Pen(Brush(role), tick % bar == 0 ? 1.5 : 1), new(X(tick), RulerHeight),
                new(X(tick), bottom));
        }
    }

    private void DrawNote(DrawingContext context, NoteEvent note, int color)
    {
        var rect = NoteRect(note);
        if (rect.Right < KeyWidth || rect.Left > Bounds.Width || rect.Bottom < RulerHeight ||
            rect.Top > NoteBottom) return;
        bool selected = _selection.Contains(note.Id);
        string body = _shell.Preferences.Theme == HostTheme.Dark ? $"Rack.Channel{color}" : $"Rack.Step{color}";
        string ink = _shell.Preferences.Theme == HostTheme.Dark ? "Rack.StepInk" : $"Rack.Channel{color}";
        double corner = HostVisual.Metric(this, "Piano.NoteCorner");
        context.DrawRectangle(Brush("Material.Shade"), null, rect.Translate(new Vector(0, 2)), corner, corner);
        context.DrawRectangle(Brush(body),
            new Pen(Brush(selected ? "Focus.Active" : $"Rack.Channel{color}"), selected ? 2 : 1), rect, corner, corner);
        context.DrawRectangle(Brush("Material.Bevel"), null, rect.Deflate(1), corner, corner);
        context.DrawLine(new Pen(Brush("Material.Highlight"), 1), new(rect.Left + 3, rect.Top + 2),
            new(rect.Right - 3, rect.Top + 2));
        using (context.PushClip(rect))
        {
            if (rect.Width >= 40)
            {
                var label = PitchLabel(note.Pitch, new(rect.X + 5, rect.Y + 1, rect.Width - 12, rect.Height - 4), ink);
                context.DrawText(label.Text, label.Origin);
            }
            context.DrawLine(new Pen(Brush(ink), 2), new(rect.Left + 2, rect.Bottom - 2),
                new(rect.Left + Math.Max(2, (rect.Width - 4) * (double)note.Intensity), rect.Bottom - 2));
            if (rect.Width >= 12)
                context.DrawLine(new Pen(Brush(ink), 1), new(rect.Right - 4, rect.Y + 4),
                    new(rect.Right - 4, rect.Bottom - 4));
        }
    }

    private void DrawKeys(DrawingContext context)
    {
        using var clip = context.PushClip(new Rect(0, RulerHeight, KeyWidth, NoteBottom - RulerHeight));
        decimal root = _observation.Snapshot.State.Sounds.Single(s => s.Id == _part!.SoundId).Parameters
            .GetValueOrDefault("rootPitch", 60);
        for (int pitch = 0; pitch <= 127; pitch++)
        {
            double y = Y(pitch);
            if (y + RowHeight < RulerHeight || y > NoteBottom) continue;
            bool black = Black(pitch);
            context.DrawRectangle(Brush("Piano.WhiteKey"), new Pen(Brush("Border.Default"), 1),
                new(0, y, KeyWidth, RowHeight));
            if (black)
                context.DrawRectangle(Brush("Piano.BlackKey"), null, new(0, y + 1, KeyWidth * 0.62, RowHeight - 2), 0,
                    2);
            if (pitch == _hoverPitch)
                using (context.PushOpacity(0.45))
                    context.FillRectangle(Brush("Accent.Action"), new(0, y, KeyWidth, RowHeight));
            if (pitch % 12 == 0 || pitch == (int)root)
            {
                var label = PitchLabel(pitch, new(KeyWidth - 27, y, 23, RowHeight), "Piano.KeyInk");
                context.DrawText(label.Text, label.Origin);
            }
            if (pitch == (int)root)
                context.FillRectangle(Brush("Accent.Action"), new(KeyWidth - 4, y + 2, 3, RowHeight - 4));
        }
    }

    private void DrawRuler(DrawingContext context)
    {
        long bar = _observation.Snapshot.State.Settings.Meter.TicksPerBar;
        using var clip = context.PushClip(new Rect(KeyWidth, 0, Math.Max(1, Bounds.Width - KeyWidth), RulerHeight));
        long from = Math.Max(0, Ticks(new(KeyWidth, 0)) / bar);
        for (long i = from; X(i * bar) < Bounds.Width; i++)
        {
            double x = X(i * bar);
            Text(context, (i + 1).ToString(CultureInfo.InvariantCulture), new(x + 7, 10), "Text.Primary");
            context.DrawLine(new Pen(Brush("Border.Default"), 1), new(x, 8), new(x, RulerHeight));
        }

        if (_pattern is not null)
        {
            double start = X(0), end = X(_pattern.Length.Ticks);
            context.DrawLine(new Pen(Brush("Accent.Action"), 2), new(start, 4), new(end, 4));
            context.DrawLine(new Pen(Brush("Accent.Action"), 2), new(end, 4), new(end, 16));
        }

        DrawPlayhead(context, RulerHeight);
    }

    private IEnumerable<(NoteEvent Note, double X, double Y)> VelocityHandles()
    {
        foreach (var group in Notes.GroupBy(n => n.Position))
        {
            var notes = group.OrderBy(n => n.Pitch).ThenBy(n => n.Id.Value).ToArray();
            for (int i = 0; i < notes.Length; i++)
                yield return (notes[i], X(group.Key.Ticks) + 5 + i * 8,
                    Bounds.Height - 8 - (double)notes[i].Intensity * Math.Max(1, VelocityHeight - 28));
        }
    }

    private void DrawVelocity(DrawingContext context, int color)
    {
        context.FillRectangle(Brush("Surface.Background"), new(0, NoteBottom, Bounds.Width, VelocityHeight));
        context.DrawLine(new Pen(Brush("Border.Default"), 1), new(0, NoteBottom), new(Bounds.Width, NoteBottom));
        Text(context, _shell["Piano.Velocity"], new(8, NoteBottom + 8));
        Text(context, "100", new(8, NoteBottom + 29));
        Text(context, "0", new(8, Bounds.Height - 20));
        using var clip = context.PushClip(new Rect(KeyWidth, NoteBottom + 2, Math.Max(1, Bounds.Width - KeyWidth),
            VelocityHeight - 2));
        foreach (var handle in VelocityHandles().OrderBy(h => _selection.Contains(h.Note.Id)))
        {
            bool selected = _selection.Contains(handle.Note.Id);
            var brush = Brush(selected ? "Accent.Action" : $"Rack.Channel{color}");
            using var opacity = context.PushOpacity(_selection.Count == 0 || selected ? 1 : 0.4);
            context.DrawLine(new Pen(brush, selected ? 3 : 2), new(handle.X, Bounds.Height - 8),
                new(handle.X, handle.Y));
            context.DrawEllipse(brush, new Pen(Brush("Surface.Raised"), 1), new(handle.X, handle.Y), 3.5, 3.5);
        }

        DrawPlayhead(context, Bounds.Height);
    }

    private void DrawPlayhead(DrawingContext context, double bottom)
    {
        var execution = _observation.Execution?.Execution;
        if (execution is not { Playing: true } || _pattern is null) return;
        long ticks;
        if (!_observation.SongMode && execution.Value.AttachmentId == _basis?.Attachment)
            ticks = execution.Value.LoopPositionTicks;
        else if (_observation.SongMode)
        {
            long absolute = ConstantTempoConversion.FromFrames(execution.Value.Position,
                _observation.Snapshot.State.Settings.Tempo, _observation.ExecutionSampleRate).Ticks;
            var placement = _observation.Snapshot.State.Placements.SingleOrDefault(p =>
                p.ItemContextId == _observation.Snapshot.State.GraphAttachments
                    .SingleOrDefault(a => a.Id == _basis?.Attachment)?.ContextId);
            var clip = _observation.Snapshot.State.ArrangementClips.FirstOrDefault(c =>
                c.SourcePlacementId == placement?.Id && absolute >= c.Position.Ticks &&
                absolute < c.Position.Ticks + _pattern.Length.Ticks * c.Repeats);
            if (clip is null) return;
            ticks = (absolute - clip.Position.Ticks) % _pattern.Length.Ticks;
        }
        else return;

        double x = X(ticks);
        if (x < KeyWidth || x > Bounds.Width) return;
        context.DrawLine(new Pen(Brush("Status.Playing"), 2), new(x, 0), new(x, bottom));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_gesture is not null) return;
        Focus();
        var point = e.GetPosition(this);
        _hover = point;
        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsMiddleButtonPressed)
        {
            if (_basis is not { } viewBasis || _part is null) return;
            _gesture = new("Pan", point, viewBasis, _part.Id, [], [], e.Pointer, false, 0, 0, _offsetX, _offsetY);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!CanEdit || _basis is not { } basis || _part is null || _pattern is null) return;
        if (point.X < KeyWidth && point.Y >= RulerHeight && point.Y < NoteBottom)
        {
            decimal pitch = Pitch(point);
            _hoverPitch = (int)pitch;
            Feedback = PianoNotes.Name(pitch);
            Changed?.Invoke();
            InvalidateVisual();
            CommandSubmitted?.Invoke(_workflow.AuditionNoteAsync(basis, _part.Id, pitch));
            e.Handled = true;
            return;
        }

        if (point.X < KeyWidth || point.Y < RulerHeight) return;
        if (point.Y >= NoteBottom)
        {
            if (!VelocityVisible || !properties.IsLeftButtonPressed) return;
            var candidates = VelocityHandles().Where(h => Math.Abs(h.X - point.X) <= 6)
                .OrderBy(h => Math.Abs(h.Y - point.Y)).ToArray();
            if (candidates.Length == 0) return;
            var note = candidates[0].Note;
            if (!_selection.Contains(note.Id))
            {
                _selection.Clear();
                _selection.Add(note.Id);
            }

            Begin("Velocity", note.Position.Ticks, note.Pitch);
            UpdateDraft(point, e.KeyModifiers);
            e.Handled = true;
            return;
        }

        var hit = Hit(point);
        if (properties.IsRightButtonPressed)
        {
            if (hit is not null)
            {
                _selection.Clear();
                _selection.Add(hit.Id);
                Delete();
            }

            e.Handled = true;
            return;
        }

        if (!properties.IsLeftButtonPressed) return;
        if (hit is not null)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (!_selection.Add(hit.Id)) _selection.Remove(hit.Id);
                Changed?.Invoke();
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            if (!_selection.Contains(hit.Id))
            {
                _selection.Clear();
                _selection.Add(hit.Id);
            }

            _lastDuration = hit.Duration.Ticks;
            _lastIntensity = hit.Intensity;
            Begin(point.X >= NoteRect(hit).Right - Math.Min(8, NoteRect(hit).Width / 3) ? "Resize" : "Move",
                hit.Position.Ticks, hit.Pitch);
        }
        else if (SelectMode || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift)) _selection.Clear();
            Begin("Select", 0, 0);
        }
        else
        {
            long ticks = Math.Clamp(PianoNotes.Snap(Math.Max(0, Ticks(point)), Unit(e.KeyModifiers)), 0,
                _pattern.Length.Ticks - 1);
            if (Ticks(point) >= _pattern.Length.Ticks) return;
            var note = new NoteEvent(Id<NoteEvent>.New(), new(ticks),
                new(Math.Min(_lastDuration, _pattern.Length.Ticks - ticks)), Pitch(point), _lastIntensity);
            _selection.Clear();
            _selection.Add(note.Id);
            _preview = [.. _part.Notes, note];
            _gesture = new("Draw", point, basis, _part.Id, _part.Notes.ToArray(), [note], e.Pointer, false, ticks,
                note.Pitch, _offsetX, _offsetY);
            e.Pointer.Capture(this);
        }

        e.Handled = true;
        Changed?.Invoke();
        InvalidateVisual();

        void Begin(string kind, long ticks, decimal pitch)
        {
            _gesture = new(kind, point, basis, _part.Id, _part.Notes.ToArray(),
                _part.Notes.Where(n => _selection.Contains(n.Id)).ToArray(),
                e.Pointer, e.KeyModifiers.HasFlag(KeyModifiers.Control), ticks, pitch, _offsetX, _offsetY);
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        _hover = point;
        _hoverPitch = point.Y is > 0 && point.Y >= RulerHeight && point.Y < NoteBottom ? (int)Pitch(point) : -1;
        if (_gesture is not null) UpdateDraft(point, e.KeyModifiers);
        else
        {
            var hit = point.X >= KeyWidth && point.Y < NoteBottom ? Hit(point) : null;
            Cursor = new(hit is not null && point.X >= NoteRect(hit).Right - 8
                ? StandardCursorType.SizeWestEast
                : StandardCursorType.Arrow);
            Feedback = hit is null ? "" : Describe(hit);
            Changed?.Invoke();
            InvalidateVisual();
        }
    }

    private void UpdateDraft(Point point, KeyModifiers modifiers)
    {
        if (_gesture is not { } gesture || _pattern is null) return;
        if (gesture.Kind == "Pan")
        {
            SetView(gesture.OffsetX + gesture.Start.X - point.X, gesture.OffsetY + gesture.Start.Y - point.Y);
            return;
        }

        if (gesture.Kind == "Select")
        {
            var rect = new Rect(gesture.Start, point).Normalize();
            _selection.Clear();
            foreach (var note in gesture.Before.Where(n => rect.Intersects(NoteRect(n)))) _selection.Add(note.Id);
            Changed?.Invoke();
            InvalidateVisual();
            return;
        }

        long unit = Unit(modifiers);
        long delta =
            PianoNotes.Snap(
                (long)Math.Round((point.X - gesture.Start.X) / _quarterWidth * MusicalPosition.TicksPerQuarter), unit);
        NoteEvent[] changed;
        if (gesture.Kind == "Draw")
        {
            var note = gesture.Selected.Single();
            long duration = Math.Abs(point.X - gesture.Start.X) < 4
                ? note.Duration.Ticks
                : Math.Clamp(PianoNotes.Snap(Math.Max(1, Ticks(point) - note.Position.Ticks), unit),
                    Math.Min(Math.Max(1, unit), _pattern.Length.Ticks - note.Position.Ticks),
                    _pattern.Length.Ticks - note.Position.Ticks);
            changed = [note with { Duration = new(duration) }];
        }
        else if (gesture.Kind == "Resize")
            changed = PianoNotes.Resize(gesture.Selected, delta, _pattern.Length.Ticks, Math.Max(1, unit));
        else if (gesture.Kind == "Velocity")
        {
            decimal value = Math.Clamp((decimal)((Bounds.Height - 8 - point.Y) / Math.Max(1, VelocityHeight - 28)), 0,
                1);
            value = decimal.Round(value, 2);
            changed = gesture.Selected.Select(n => n with { Intensity = value }).ToArray();
        }
        else
            changed = PianoNotes.Move(gesture.Selected, delta, Pitch(point) - Pitch(gesture.Start),
                _pattern.Length.Ticks, false);

        if (gesture.Copy && gesture.Kind == "Move")
        {
            // Stable draft identities across pointer updates; copied notes acquire IDs only once.
            var ids = _preview?.Where(n => !gesture.Before.Any(b => b.Id == n.Id)).Select(n => n.Id).ToArray();
            changed = changed.Select((n, i) => n with { Id = ids is { Length: > 0 } ? ids[i] : Id<NoteEvent>.New() })
                .ToArray();
            _selection.Clear();
            _selection.UnionWith(changed.Select(n => n.Id));
        }

        _preview =
        [
            .. gesture.Before.Where(n =>
                gesture.Copy && gesture.Kind == "Move" || !gesture.Selected.Any(s => s.Id == n.Id)),
            .. changed
        ];
        Feedback = changed.Length == 1 ? Describe(changed[0]) : $"{changed.Length} · {Describe(changed[0])}";
        Changed?.Invoke();
        InvalidateVisual();
    }

    private string Describe(NoteEvent note) =>
        $"{PianoNotes.Name(note.Pitch)}  ·  {note.Position.Ticks / (decimal)MusicalPosition.TicksPerQuarter:0.###}  →  {note.Duration.Ticks / (decimal)MusicalPosition.TicksPerQuarter:0.###}  ·  {_shell["Piano.Velocity"]} {note.Intensity * 100:0}";

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_gesture is not { } gesture || gesture.Pointer != e.Pointer) return;
        var desired = _preview;
        _gesture = null;
        _preview = null;
        gesture.Pointer.Capture(null);
        if (desired is not null)
        {
            var edited = desired.FirstOrDefault(n => _selection.Contains(n.Id));
            if (edited is not null)
            {
                _lastDuration = edited.Duration.Ticks;
                _lastIntensity = edited.Intensity;
            }

            Submit(gesture.Basis, gesture.Part, desired);
        }

        Changed?.Invoke();
        InvalidateVisual();
        e.Handled = true;
    }

    private void Submit(BeatEditBasis basis, Id<MusicalPart> part, NoteEvent[] notes)
    {
        long transportGeneration = _workflow.State.TransportGeneration;
        _awaitingCommand = true;
        CommandSubmitted?.Invoke(CommitAsync());

        async Task CommitAsync()
        {
            try
            {
                bool changed = await _workflow.EditPianoNotesAsync(basis, part, notes);
                var observation = _workflow.State;
                if (changed && AuditionEdits && observation.TransportGeneration == transportGeneration &&
                    observation.Execution?.Execution.Playing != true &&
                    notes.FirstOrDefault(n => _selection.Contains(n.Id)) is { } note &&
                    BeatSteps.Resolve(observation).Basis is { } current && current.Attachment == basis.Attachment &&
                    current.Pattern == basis.Pattern)
                    await _workflow.AuditionNoteAsync(current, part, note.Pitch, note.Intensity);
            }
            finally
            {
                _awaitingCommand = false;
                Changed?.Invoke();
            }
        }
    }

    internal void CommitNotes(BeatEditBasis basis, Id<MusicalPart> part, NoteEvent[] notes) =>
        Submit(basis, part, notes);

    internal void Quantize()
    {
        if (!CanQuantize || _basis is not { } basis || _part is null) return;
        Cancel();
        _awaitingCommand = true;
        CommandSubmitted?.Invoke(CommitAsync());
        async Task CommitAsync()
        {
            try
            {
                await _workflow.QuantizePianoNotesAsync(basis, _part.Id, _selection, SnapTicks);
                if (_workflow.State.Detail is { } key && key is "Piano.QuantizeBoundary" or "Piano.QuantizeSnapOff")
                    Feedback = _shell[key];
            }
            finally
            {
                _awaitingCommand = false;
                Changed?.Invoke();
                InvalidateVisual();
            }
        }
    }

    internal void Delete()
    {
        if (!CanEdit || _basis is not { } basis || _part is null || _selection.Count == 0) return;
        Cancel();
        Submit(basis, _part.Id, _part.Notes.Where(n => !_selection.Contains(n.Id)).ToArray());
    }

    internal void Duplicate()
    {
        if (!CanEdit || _basis is not { } basis || _part is null || _pattern is null) return;
        var selected = _part.Notes.Where(n => _selection.Contains(n.Id)).ToArray();
        if (selected.Length == 0) return;
        long delta = selected.Max(n => n.Position.Ticks + n.Duration.Ticks) - selected.Min(n => n.Position.Ticks);
        var copies = PianoNotes.Move(selected, delta, 0, _pattern.Length.Ticks, true);
        _selection.Clear();
        _selection.UnionWith(copies.Select(n => n.Id));
        Submit(basis, _part.Id, [.. _part.Notes, .. copies]);
    }

    internal void SelectAll()
    {
        _selection.Clear();
        _selection.UnionWith(_part?.Notes.Select(n => n.Id) ?? []);
        Changed?.Invoke();
        InvalidateVisual();
    }

    internal void RevealSelection()
    {
        var note = _part?.Notes.FirstOrDefault(n => _selection.Contains(n.Id));
        if (note is null) return;
        var rect = NoteRect(note);
        SetView(_offsetX + (rect.Left < KeyWidth ? rect.Left - KeyWidth :
                rect.Right > Bounds.Width ? rect.Right - Bounds.Width : 0),
            _offsetY + (rect.Top < RulerHeight ? rect.Top - RulerHeight :
                rect.Bottom > NoteBottom ? rect.Bottom - NoteBottom : 0));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        bool control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) && e.Key is Key.Left or Key.Right &&
            _part is { Notes.IsEmpty: false })
        {
            var ordered = _part.Notes.OrderBy(n => n.Position.Ticks).ThenBy(n => n.Pitch).ThenBy(n => n.Id.Value)
                .ToArray();
            int index = Array.FindIndex(ordered, n => _selection.Contains(n.Id));
            index = index < 0 ? 0 : (index + (e.Key == Key.Right ? 1 : ordered.Length - 1)) % ordered.Length;
            var note = ordered[index];
            _selection.Clear();
            _selection.Add(note.Id);
            var rect = NoteRect(note);
            SetView(
                _offsetX + (rect.Left < KeyWidth ? rect.Left - KeyWidth :
                    rect.Right > Bounds.Width ? rect.Right - Bounds.Width : 0),
                _offsetY + (rect.Top < RulerHeight ? rect.Top - RulerHeight :
                    rect.Bottom > NoteBottom ? rect.Bottom - NoteBottom : 0));
            Feedback = Describe(note);
            Changed?.Invoke();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Cancel();
            _selection.Clear();
            Changed?.Invoke();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Home && !control)
        {
            Fit();
            e.Handled = true;
            return;
        }

        if (control && e.Key == Key.A)
        {
            SelectAll();
            e.Handled = true;
            return;
        }

        if (!CanEdit || _basis is not { } basis || _part is null || _pattern is null) return;
        if (e.Key is Key.Delete or Key.Back)
        {
            Delete();
            e.Handled = true;
            return;
        }

        if (control && e.Key == Key.D)
        {
            Duplicate();
            e.Handled = true;
            return;
        }

        var selected = _part.Notes.Where(n => _selection.Contains(n.Id)).ToArray();
        if (e.Key == Key.Enter && selected.Length > 0)
        {
            CommandSubmitted?.Invoke(_workflow.AuditionNoteAsync(basis, _part.Id, selected[0].Pitch,
                selected[0].Intensity));
            e.Handled = true;
            return;
        }

        if (control && e.Key is Key.C or Key.X)
        {
            _clipboard = selected;
            if (e.Key == Key.X) Delete();
            e.Handled = true;
            return;
        }

        if (control && e.Key == Key.V && _clipboard.Length > 0)
        {
            long start = Math.Max(0, PianoNotes.Snap(Ticks(new(KeyWidth + 16, 0)), SnapTicks));
            if (_clipboard.Max(n => n.Position.Ticks + n.Duration.Ticks) - _clipboard.Min(n => n.Position.Ticks) >
                _pattern.Length.Ticks) return;
            var copies = PianoNotes.Move(_clipboard, start - _clipboard.Min(n => n.Position.Ticks), 0,
                _pattern.Length.Ticks, true);
            _selection.Clear();
            _selection.UnionWith(copies.Select(n => n.Id));
            Submit(basis, _part.Id, [.. _part.Notes, .. copies]);
            e.Handled = true;
            return;
        }

        if (selected.Length == 0) return;
        long unit = Math.Max(1, SnapTicks);
        long ticks = e.Key == Key.Left ? -unit : e.Key == Key.Right ? unit : 0;
        decimal pitch = e.Key == Key.Up ? 1 : e.Key == Key.Down ? -1 : 0;
        if (ticks == 0 && pitch == 0) return;
        NoteEvent[] changed = e.KeyModifiers.HasFlag(KeyModifiers.Shift) && ticks != 0
            ? PianoNotes.Resize(selected, ticks, _pattern.Length.Ticks, unit)
            : PianoNotes.Move(selected, ticks, pitch * (e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 12 : 1),
                _pattern.Length.Ticks);
        Submit(basis, _part.Id, [.. _part.Notes.Where(n => !_selection.Contains(n.Id)), .. changed]);
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_gesture is not null) return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) Zoom(Math.Pow(1.15, e.Delta.Y), e.GetPosition(this).X);
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            SetView(_offsetX - e.Delta.Y * 64 - e.Delta.X * 64, _offsetY);
        else SetView(_offsetX - e.Delta.X * 64, _offsetY - e.Delta.Y * RowHeight * 3);
        e.Handled = true;
    }

    internal void Cancel()
    {
        var pointer = _gesture?.Pointer;
        _gesture = null;
        _preview = null;
        pointer?.Capture(null);
        Feedback = "";
        Changed?.Invoke();
        InvalidateVisual();
    }
}
