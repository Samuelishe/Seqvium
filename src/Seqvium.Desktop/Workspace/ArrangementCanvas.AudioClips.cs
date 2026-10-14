// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed partial class ArrangementCanvas
{
    private AudioClip? _audioOriginal, _audioDraft;
    private readonly HashSet<Id<AudioClip>> _audioSelection = [];
    private AudioClip[] _audioGroup = [];
    private int _audioGesture; // 0 move, 1 left trim, 2 right trim, 3 fade in, 4 fade out
    private Point? _dropPoint;
    private CancellationTokenSource? _audioImportCancellation;
    internal Id<AudioClip>? SelectedAudio { get; private set; }
    internal Id<AudioClip>[] AudioSelection => [.. _audioSelection];
    internal bool SliceMode { get; set; }
    internal bool IsImportingAudio => _audioImportCancellation is not null;
    internal event Action? AudioPropertiesRequested;

    private void InitializeAudioClips()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            var files = e.DataTransfer.TryGetFiles()?.ToArray();
            var point = e.GetPosition(this);
            e.DragEffects = !_observation.Busy && point.X >= HeaderX + HeaderWidth && point.Y >= RulerY + RulerHeight &&
                            files is { Length: > 0 } && files.All(f =>
                                f.TryGetLocalPath() is { } path && AudioFileDecode.Supported(path))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            _dropPoint = e.DragEffects == DragDropEffects.Copy ? point : null;
            InvalidateVisual();
            e.Handled = true;
        });
        AddHandler(DragDrop.DragLeaveEvent, (_, _) =>
        {
            _dropPoint = null;
            InvalidateVisual();
        });
        AddHandler(DragDrop.DropEvent, async (_, e) =>
        {
            var files = e.DataTransfer.TryGetFiles()?.Select(f => f.TryGetLocalPath()).ToArray();
            var point = e.GetPosition(this);
            _dropPoint = null;
            e.Handled = true;
            if (_observation.Busy || files is null ||
                files.Any(p => p is null || !AudioFileDecode.Supported(p))) return;
            CommandSubmitted?.Invoke(ImportAudioFilesAsync(files.Select(p => p!).ToArray(), TickAt(point.X),
                Math.Clamp((int)((point.Y - RulerHeight) / LaneHeight), 0, 31)));
            await Task.CompletedTask;
        });
    }

    internal async Task ImportAudioFilesAsync(string[] files, long ticks, int lane)
    {
        using var cancellation = new CancellationTokenSource();
        _audioImportCancellation = cancellation;
        try
        {
            for (int i = 0; i < files.Length; i++)
            {
                var before = _workflow.State;
                if (!await _workflow.ImportAudioClipAsync(ArrangementEditBasis.From(before), files[i], new(ticks),
                        Math.Min(31, lane + i), cancellation.Token)) break;
                var clip = _workflow.State.Snapshot.State.AudioClips.Last();
                _audioSelection.Clear();
                _audioSelection.Add(clip.Id);
                SelectedAudio = clip.Id;
            }

            SelectionChanged?.Invoke();
            InvalidateVisual();
        }
        finally
        {
            _audioImportCancellation = null;
        }
    }

    internal void CancelAudioImport() => _audioImportCancellation?.Cancel();

    private void AdoptAudioClips(WorkflowState state)
    {
        _audioSelection.RemoveWhere(id => !state.Snapshot.State.AudioClips.Any(c => c.Id == id));
        if (!_audioSelection.Contains(SelectedAudio ?? default))
            SelectedAudio = _audioSelection.Select(id => (Id<AudioClip>?)id).FirstOrDefault();
    }

    internal Rect AudioRect(AudioClip clip) => new(HeaderWidth + clip.Position.Ticks * TickPixels + 2,
        RulerHeight + clip.Lane * LaneHeight + 5,
        Math.Max(8, AudioClipSource.LengthTicks(_observation.Snapshot.State, clip) * TickPixels - 4), LaneHeight - 10);

    private AudioClip? AudioHit(Point point) => _observation.Snapshot.State.AudioClips.Reverse()
        .FirstOrDefault(c => AudioRect(c).Contains(point));

    private void DrawAudioClips(DrawingContext context)
    {
        foreach (var clip in _observation.Snapshot.State.AudioClips)
        {
            var shown = _audioDraft?.Id == clip.Id && !_copy ? _audioDraft : clip;
            if (_audioGesture == 0 && !_resize && !_copy && _audioGroup.Any(c => c.Id == clip.Id))
                shown = clip with { Position = new(clip.Position.Ticks + _tickDelta), Lane = clip.Lane + _laneDelta };
            DrawAudioClip(context, shown, _audioSelection.Contains(clip.Id));
        }

        if (_copy)
            foreach (var clip in _audioGroup)
                DrawAudioClip(context,
                    clip with { Position = new(clip.Position.Ticks + _tickDelta), Lane = clip.Lane + _laneDelta },
                    true);
        if (_dropPoint is { } point)
        {
            double x = HeaderWidth + TickAt(point.X) * TickPixels;
            int lane = Math.Clamp((int)((point.Y - RulerHeight) / LaneHeight), 0, 31);
            context.DrawRectangle(null, new Pen(HostVisual.Brush(this, _shell.Preferences.Theme, "Accent.Action"), 2),
                new Rect(x, RulerHeight + lane * LaneHeight + 2, Math.Max(24, _barWidth), LaneHeight - 4));
        }
    }

    private void DrawAudioClip(DrawingContext context, AudioClip clip, bool selected)
    {
        var state = _observation.Snapshot.State;
        var sound = state.Sounds.Single(s => s.Id == clip.SoundId);
        var rect = AudioRect(clip);
        if (_viewport.Width > 0 && !rect.Intersects(_viewport)) return;
        var theme = _shell.Preferences.Theme;
        IBrush Brush(string key) => HostVisual.Brush(this, theme, key);
        int color = state.ArrangementTracks.SingleOrDefault(t => t.Lane == clip.Lane)?.ColorIndex ?? clip.Lane % 8;
        using var opacity = context.PushOpacity(ArrangementPreparation.IsAudible(state, clip.Lane) ? 1 : .35);
        context.DrawRectangle(Brush("Surface.Raised"),
            new Pen(Brush(selected ? "Accent.Action" : $"Rack.Channel{color}"), selected ? 2 : 1), rect,
            HostVisual.Metric(this, "Control.Corner"), HostVisual.Metric(this, "Control.Corner"));
        using var clipBounds = context.PushClip(rect.Deflate(2));
        using (context.PushOpacity(.22)) context.DrawRectangle(Brush($"Rack.Channel{color}"), null, rect);
        context.DrawRectangle(Brush("Material.Bevel"), null, new Rect(rect.X, rect.Y, rect.Width, 18));
        context.DrawRectangle(Brush($"Rack.Channel{color}"), null, new Rect(rect.X, rect.Y, 3, rect.Height));
        string name = "∿  " + sound.Name + (clip.Repeats > 1 ? $"  ×{clip.Repeats}" : "");
        context.DrawText(new FormattedText(name, System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                HostVisual.Typeface(this), HostVisual.Metric(this, "Type.Caption"), Brush("Text.Primary")),
            new(rect.X + 8, rect.Y + 2));
        if (!_observation.AudioWaveforms.TryGetValue(sound.Id, out var wave))
        {
            HostVisual.Caption(this, _shell, context, _shell["AudioClip.Missing"], new(rect.X + 8, rect.Y + 23));
            return;
        }

        double cycle = (rect.Width + 4) / clip.Repeats;
        int first = Math.Max(0, (int)Math.Floor((_viewport.Left - rect.X) / cycle));
        int last = Math.Min(clip.Repeats,
            (int)Math.Ceiling(((_viewport.Width > 0 ? _viewport.Right : rect.Right) - rect.X) / cycle));
        var pen = new Pen(Brush($"Rack.Channel{color}"), 1);
        double band = Math.Max(1, rect.Height - 22) / wave.Channels;
        double visualScale = 1 / Math.Max(.01, wave.Peak); // Visual amplitude fit; never an audio gain.
        for (int repeat = first; repeat < last; repeat++)
        {
            double left = rect.X + repeat * cycle;
            if (repeat > 0) context.DrawLine(pen, new(left, rect.Y + 19), new(left, rect.Bottom));
            double start = Math.Max(left, _viewport.Left),
                end = Math.Min(left + cycle, _viewport.Width > 0 ? _viewport.Right : rect.Right);
            for (double x = start; x < end; x += 1.5)
            {
                double relative = Math.Clamp((x - left) / cycle, 0, 1);
                long source = clip.OffsetFrames + (long)(relative * clip.LengthFrames);
                long next = Math.Min(clip.OffsetFrames + clip.LengthFrames,
                    source + Math.Max(1, (long)(clip.LengthFrames / cycle * 1.5)));
                double age = clip.EnvelopeOffsetFrames + relative * clip.LengthFrames;
                double fade =
                    (clip.FadeInFrames > 0 ? Math.Clamp(age / clip.FadeInFrames, 0, 1) : 1) *
                    (clip.FadeOutFrames > 0
                        ? Math.Clamp((AudioClipSource.EnvelopeLength(clip) - age) / clip.FadeOutFrames, 0, 1)
                        : 1);
                for (int c = 0; c < wave.Channels; c++)
                {
                    var range = wave.Range(source, next, c);
                    double center = rect.Y + 21 + band * (c + .5), height = band * .44;
                    context.DrawLine(pen, new(x, center - Math.Clamp(range.Max * fade * visualScale, -1, 1) * height),
                        new(x, center - Math.Clamp(range.Min * fade * visualScale, -1, 1) * height));
                }
            }

            if (selected)
            {
                var fadePen = new Pen(Brush("Text.Primary"), 1);
                double FadeY(double gain) => rect.Bottom - Math.Clamp(gain, 0, 1) * (rect.Height - 20);
                double inEnd = Math.Clamp(clip.FadeInFrames - clip.EnvelopeOffsetFrames, 0, clip.LengthFrames);
                double outStart =
                    Math.Clamp(AudioClipSource.EnvelopeLength(clip) - clip.FadeOutFrames - clip.EnvelopeOffsetFrames, 0,
                        clip.LengthFrames);
                if (clip.FadeInFrames > clip.EnvelopeOffsetFrames)
                    context.DrawLine(fadePen, new(left, FadeY((double)clip.EnvelopeOffsetFrames / clip.FadeInFrames)),
                        new(left + inEnd / clip.LengthFrames * cycle,
                            FadeY((clip.EnvelopeOffsetFrames + inEnd) / clip.FadeInFrames)));
                if (clip.FadeOutFrames > 0)
                    context.DrawLine(fadePen,
                        new(left + outStart / clip.LengthFrames * cycle,
                            FadeY((AudioClipSource.EnvelopeLength(clip) - clip.EnvelopeOffsetFrames - outStart) /
                                  clip.FadeOutFrames)),
                        new(left + cycle,
                            FadeY((double)(AudioClipSource.EnvelopeLength(clip) - clip.EnvelopeOffsetFrames -
                                           clip.LengthFrames) / clip.FadeOutFrames)));
            }
        }

        if (selected)
        {
            context.DrawRectangle(Brush("Accent.Action"), null, new Rect(rect.X + 3, rect.Y + 18, 6, 6));
            context.DrawRectangle(Brush("Accent.Action"), null, new Rect(rect.Right - 9, rect.Y + 18, 6, 6));
        }
    }

    private int AudioGestureAt(AudioClip clip, Point point)
    {
        var rect = AudioRect(clip);
        if (point.Y >= rect.Y + 15 && point.Y < rect.Y + 29)
        {
            if (point.X < rect.X + 14) return 3;
            if (point.X > rect.Right - 14) return 4;
        }

        if (clip.Repeats == 1 && point.X < rect.X + 9) return 1;
        if (clip.Repeats == 1 && point.X > rect.Right - 9) return 2;
        return 0;
    }

    private bool AudioPressed(PointerPressedEventArgs e, Point point)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || point.X < HeaderX + HeaderWidth ||
            point.Y < RulerY + RulerHeight) return false;
        var clip = AudioHit(point);
        if (clip is null)
        {
            return false;
        }

        e.Handled = true;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (!_audioSelection.Add(clip.Id)) _audioSelection.Remove(clip.Id);
            SelectedAudio = _audioSelection.Select(id => (Id<AudioClip>?)id).FirstOrDefault();
            SelectionChanged?.Invoke();
            InvalidateVisual();
            return true;
        }

        if (!_audioSelection.Contains(clip.Id))
        {
            _audioSelection.Clear();
            _selection.Clear();
            Selected = null;
            _audioSelection.Add(clip.Id);
        }

        SelectedAudio = clip.Id;
        SelectionChanged?.Invoke();
        if (SliceMode)
        {
            CommandSubmitted?.Invoke(_workflow.SplitAudioClipAsync(Basis, clip.Id, new(TickAt(point.X))));
            return true;
        }

        if (e.ClickCount == 2)
        {
            AudioPropertiesRequested?.Invoke();
            return true;
        }

        _audioOriginal = _audioDraft = clip;
        _basis = Basis;
        _pressed = point;
        _audioGesture = _audioSelection.Count + _selection.Count == 1 ? AudioGestureAt(clip, point) : 0;
        _group = _observation.Snapshot.State.ArrangementClips.Where(c => _selection.Contains(c.Id)).ToArray();
        _audioGroup = _observation.Snapshot.State.AudioClips.Where(c => _audioSelection.Contains(c.Id)).ToArray();
        _copy = _audioGesture == 0 && e.KeyModifiers.HasFlag(KeyModifiers.Control);
        _pointer = e.Pointer;
        e.Pointer.Capture(this);
        InvalidateVisual();
        return true;
    }

    private bool AudioMoved(PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        if (_pointer is null)
        {
            var hover = AudioHit(point);
            var route = hover is null
                ? null
                : _observation.Snapshot.State.Mixer?.Channels
                    .SingleOrDefault(c => c.SoundIds.Contains(hover.SoundId));
            ToolTip.SetTip(this,
                hover is null
                    ? _shell["Arrangement.Hint"]
                    : _shell["AudioClip.Hint"] + " · " + (route?.Name ?? _shell["Mixer.Master"]));
            Cursor = new Cursor(hover is null ? StandardCursorType.Arrow :
                SliceMode ? StandardCursorType.Cross :
                AudioGestureAt(hover, point) is 1 or 2 ? StandardCursorType.SizeWestEast :
                AudioGestureAt(hover, point) is 3 or 4 ? StandardCursorType.Cross : StandardCursorType.SizeAll);
            return false;
        }

        if (_audioOriginal is not { } clip || _pointer != e.Pointer) return false;
        e.Handled = true;
        if (Math.Abs(point.X - _pressed.X) + Math.Abs(point.Y - _pressed.Y) < 3) return true;
        var state = _observation.Snapshot.State;
        int rate = AudioClipSource.Rate(state.Sounds.Single(s => s.Id == clip.SoundId));
        long deltaTicks = (long)Math.Round((point.X - _pressed.X) / TickPixels);
        long position = e.KeyModifiers.HasFlag(KeyModifiers.Alt)
            ? Math.Max(0, clip.Position.Ticks + deltaTicks)
            : TickAt(HeaderWidth + (clip.Position.Ticks + deltaTicks) * TickPixels);
        double framesPerTick =
            rate * 60d / (double)state.Settings.Tempo.BeatsPerMinute / MusicalPosition.TicksPerQuarter;
        long frames = (long)Math.Round((position - clip.Position.Ticks) * framesPerTick);
        if (_audioGesture == 1)
        {
            frames = Math.Clamp(frames, -clip.OffsetFrames, clip.LengthFrames - 1);
            long shift = AudioClipSource.DurationTicks(Math.Abs(frames), rate, state.Settings.Tempo) *
                         Math.Sign(frames);
            if (clip.Position.Ticks + shift < 0) return true;
            _audioDraft = clip with
            {
                Position = new(clip.Position.Ticks + shift), OffsetFrames = clip.OffsetFrames + frames,
                EnvelopeOffsetFrames = 0, EnvelopeLengthFrames = 0,
                LengthFrames = clip.LengthFrames - frames,
                FadeInFrames = Math.Min(clip.FadeInFrames, clip.LengthFrames - frames),
                FadeOutFrames = Math.Min(clip.FadeOutFrames, clip.LengthFrames - frames)
            };
        }
        else if (_audioGesture == 2)
        {
            long length = Math.Clamp(clip.LengthFrames + frames, 1,
                AudioClipSource.Frames(state.Sounds.Single(s => s.Id == clip.SoundId)) - clip.OffsetFrames);
            _audioDraft = clip with
            {
                LengthFrames = length, EnvelopeOffsetFrames = 0, EnvelopeLengthFrames = 0,
                FadeInFrames = Math.Min(clip.FadeInFrames, length),
                FadeOutFrames = Math.Min(clip.FadeOutFrames, length)
            };
        }
        else if (_audioGesture is 3 or 4)
        {
            var rect = AudioRect(clip);
            long fade = (long)Math.Clamp(
                (_audioGesture == 3 ? point.X - rect.X : rect.Right - point.X) / (rect.Width + 4) * clip.LengthFrames *
                clip.Repeats, 0, clip.LengthFrames);
            var independent = clip with
            {
                EnvelopeOffsetFrames = 0, EnvelopeLengthFrames = 0,
                FadeInFrames = Math.Min(clip.FadeInFrames, clip.LengthFrames),
                FadeOutFrames = Math.Min(clip.FadeOutFrames, clip.LengthFrames)
            };
            _audioDraft = _audioGesture == 3
                ? independent with { FadeInFrames = fade }
                : independent with { FadeOutFrames = fade };
        }
        else
        {
            SetGroupDelta(position - clip.Position.Ticks, (int)Math.Round((point.Y - _pressed.Y) / LaneHeight));
            _audioDraft = clip with { Position = new(clip.Position.Ticks + _tickDelta), Lane = clip.Lane + _laneDelta };
        }

        InvalidateVisual();
        return true;
    }

    private bool AudioReleased(PointerReleasedEventArgs e)
    {
        if (_audioOriginal is not { } original || _pointer != e.Pointer) return false;
        var draft = _audioDraft!;
        var basis = _basis;
        var ids = AudioSelection;
        var patternIds = Selection;
        bool copy = _copy;
        int gesture = _audioGesture;
        long delta = _tickDelta;
        int lanes = _laneDelta;
        Cancel();
        e.Handled = true;
        if (draft != original)
            CommandSubmitted?.Invoke(gesture == 0
                ? CommitGroupAsync(basis, patternIds, ids, delta, lanes, copy)
                : _workflow.EditAudioClipAsync(basis, draft));
        return true;
    }

    private bool AudioKey(KeyEventArgs e)
    {
        if (SelectedAudio is not { } id) return false;
        Task? command = e.Key switch
        {
            Key.Delete => _workflow.EditAudioSelectionAsync(Basis, AudioSelection, delete: true),
            Key.D when e.KeyModifiers == KeyModifiers.Control => _workflow.EditAudioSelectionAsync(Basis,
                AudioSelection, duplicate: true),
            Key.S when e.KeyModifiers == KeyModifiers.Control => null,
            Key.Enter => null,
            Key.Left or Key.Right or Key.Up or Key.Down => _workflow.EditAudioSelectionAsync(Basis, AudioSelection,
                e.Key == Key.Left ? -SnapTicks : e.Key == Key.Right ? SnapTicks : 0,
                e.Key == Key.Up ? -1 : e.Key == Key.Down ? 1 : 0),
            _ => null
        };
        if (e.Key == Key.Enter)
        {
            AudioPropertiesRequested?.Invoke();
            e.Handled = true;
            return true;
        }

        if (command is null) return false;
        e.Handled = true;
        CommandSubmitted?.Invoke(command);
        return true;
    }

    internal Task SplitSelectedAudio() => SelectedAudio is { } id
        ? _workflow.SplitAudioClipAsync(Basis, id, new(PositionTicks))
        : Task.CompletedTask;

    private void CancelAudioGesture()
    {
        _audioOriginal = _audioDraft = null;
        _audioGroup = [];
        _audioGesture = 0;
    }
}
