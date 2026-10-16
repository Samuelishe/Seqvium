// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>One source-frame viewport. Pointer proposals never edit canonical state until release.</summary>
internal sealed class SamplerWaveform : Control
{
    private readonly ShellSession _shell;
    private IPointer? _pointer;
    private int _marker = -1;
    private SamplerPlayback _before;
    internal AudioWaveform? Waveform { get; set; }
    internal SamplerPlayback Playback { get; set; }
    internal long ViewStart { get; private set; }
    internal long ViewLength { get; private set; }
    internal bool HasGesture => _pointer is not null;
    internal Func<bool>? Begin { get; set; }
    internal event Action<SamplerPlayback>? Preview;
    internal event Action? Completed, Cancelled, ViewChanged;
    internal SamplerWaveform(ShellSession shell)
    {
        _shell = shell; Focusable = true; ClipToBounds = true;
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
    }
    internal void Fit()
    {
        ViewStart = 0; ViewLength = Waveform?.Frames ?? 0;
        ViewChanged?.Invoke(); InvalidateVisual();
    }
    internal void Zoom(double factor, double anchor = .5)
    {
        if (Waveform is not { } wave) return;
        long old = Math.Max(1, ViewLength);
        ViewLength = Math.Clamp((long)(old * factor), Math.Min(16, wave.Frames), wave.Frames);
        ViewStart = Math.Clamp(ViewStart + (long)((old - ViewLength) * anchor), 0, wave.Frames - ViewLength);
        ViewChanged?.Invoke(); InvalidateVisual();
    }
    internal void Pan(long frame)
    {
        if (Waveform is not { } wave) return;
        ViewStart = Math.Clamp(frame, 0, wave.Frames - ViewLength);
        InvalidateVisual();
    }
    private double X(long frame) => (frame - ViewStart) / (double)Math.Max(1, ViewLength) * Bounds.Width;
    internal void Cancel()
    {
        if (_pointer is null) return;
        Playback = _before;
        var pointer = _pointer; _pointer = null; pointer.Capture(null);
        Cancelled?.Invoke(); InvalidateVisual();
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Waveform is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || Begin?.Invoke() != true) return;
        var point = e.GetPosition(this);
        long[] markers = [Playback.Start, Playback.End, Playback.LoopStart, Playback.LoopEnd];
        // Separate upper/lower handles remain reachable even when all endpoints coincide.
        int first = point.Y < Bounds.Height / 2 ? 0 : 2;
        if (first == 2 && Playback.LoopEnd == 0) return;
        _marker = Math.Abs(point.X - X(markers[first])) <= Math.Abs(point.X - X(markers[first + 1])) ? first : first + 1;
        if (Math.Abs(point.X - X(markers[_marker])) > 18) return;
        _before = Playback; _pointer = e.Pointer; e.Pointer.Capture(this); Focus(); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointer is null || Waveform is not { } wave || Bounds.Width <= 0) return;
        long frame = Math.Clamp(ViewStart + (long)Math.Round(e.GetPosition(this).X / Bounds.Width * ViewLength), 0, wave.Frames);
        var p = Playback;
        p = _marker switch
        {
            0 => p with { Start = Math.Min(frame, Math.Min(p.End - 1, p.Loop || p.LoopEnd > 0 ? p.LoopStart : p.End - 1)) },
            1 => p with { End = Math.Max(frame, Math.Max(p.Start + 1, p.Loop || p.LoopEnd > 0 ? p.LoopEnd : p.Start + 1)) },
            2 => p with { LoopStart = Math.Clamp(frame, p.Start, p.LoopEnd - 4) },
            _ => p with { LoopEnd = Math.Clamp(frame, p.LoopStart + 4, p.End) }
        };
        try { p.Validate(wave.Frames); }
        catch (ArgumentException) { return; }
        Playback = p; Preview?.Invoke(p); InvalidateVisual(); e.Handled = true;
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointer is null) return;
        var pointer = _pointer; _pointer = null; pointer.Capture(null);
        Completed?.Invoke(); e.Handled = true;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { base.OnPointerCaptureLost(e); Cancel(); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && HasGesture) { Cancel(); e.Handled = true; }
    }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        Zoom(e.Delta.Y > 0 ? .5 : 2, Math.Clamp(e.GetPosition(this).X / Math.Max(1, Bounds.Width), 0, 1));
        e.Handled = true;
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        IBrush Brush(string role) => HostVisual.Brush(this, _shell.Preferences.Theme, role);
        context.DrawRectangle(Brush("Surface.Workspace"), new Pen(Brush("Border.Default")), new Rect(Bounds.Size));
        if (Waveform is not { } wave || ViewLength <= 0 || Bounds.Width <= 0) return;
        var p = Playback;
        using (context.PushClip(new Rect(Bounds.Size)))
        {
            double left = Math.Clamp(X(p.Start), 0, Bounds.Width), right = Math.Clamp(X(p.Loop ? p.LoopEnd : p.End), 0, Bounds.Width);
            context.DrawRectangle(Brush("Surface.Selected"), null, new Rect(left, 24, Math.Max(0, right - left), Math.Max(0, Bounds.Height - 48)));
            if (p.Loop)
            {
                double a = Math.Clamp(X(p.LoopStart), 0, Bounds.Width), b = Math.Clamp(X(p.LoopEnd), 0, Bounds.Width);
                context.DrawRectangle(Brush("Surface.Accent"), null, new Rect(a, 24, Math.Max(0, b - a), Math.Max(0, Bounds.Height - 48)));
                if (p.Crossfade > 0)
                {
                    // Show both actual overlap regions; markers and the source remain untouched.
                    foreach (long end in new[] { p.LoopStart, p.LoopEnd })
                    {
                        double from = X(end - p.Crossfade), to = X(end);
                        context.DrawRectangle(null, new Pen(Brush("Status.Playing")),
                            new Rect(from, 24, Math.Max(0, to - from), Math.Max(0, Bounds.Height - 48)));
                        context.DrawLine(new Pen(Brush("Status.Playing"), 2),
                            new(from, end == p.LoopStart ? Bounds.Height - 24 : 24),
                            new(to, end == p.LoopStart ? 24 : Bounds.Height - 24));
                    }
                }
            }
            double channelHeight = Math.Max(1, Bounds.Height - 52) / wave.Channels;
            for (int c = 0; c < wave.Channels; c++)
            {
                double center = 26 + channelHeight * (c + .5);
                context.DrawLine(new Pen(Brush("Border.Default")), new(0, center), new(Bounds.Width, center));
                for (int x = 0; x < (int)Bounds.Width; x++)
                {
                    long start = ViewStart + (long)(x / Bounds.Width * ViewLength);
                    var (min, max) = wave.Range(start, ViewStart + (long)((x + 1) / Bounds.Width * ViewLength), c);
                    double scale = (channelHeight / 2 - 4) / Math.Max(.001f, wave.Peak);
                    context.DrawLine(new Pen(Brush(start >= p.Start && start < (p.Loop ? p.LoopEnd : p.End) ? "Accent.Action" : "Text.Secondary")),
                        new(x, center - max * scale), new(x, center - min * scale));
                }
            }
            void Marker(long frame, string label, bool end, bool loop)
            {
                double x = X(frame);
                if (x < 0 || x > Bounds.Width) return;
                var brush = Brush(loop ? p.Loop ? "Status.Playing" : "Text.Secondary" : "Accent.Action");
                context.DrawLine(new Pen(brush, 2), new(x, 22), new(x, Bounds.Height - 22));
                context.DrawRectangle(brush, null, new Rect(Math.Clamp(end ? x - 9 : x, 0, Math.Max(0, Bounds.Width - 9)), loop ? Bounds.Height - 22 : 0, 9, 22));
                var caption = new FormattedText(_shell["Sampler." + label], System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, HostVisual.Typeface(this), HostVisual.Metric(this, "Type.Caption"), Brush("Text.Secondary"));
                double gap = Math.Abs(X(loop ? p.LoopEnd : p.End) - X(loop ? p.LoopStart : p.Start));
                if (gap > 2 * caption.Width + 28 || _marker == (loop ? end ? 3 : 2 : end ? 1 : 0) && HasGesture)
                    context.DrawText(caption, new(Math.Clamp(end ? x - caption.Width - 12 : x + 12,
                        0, Math.Max(0, Bounds.Width - caption.Width)), loop ? Bounds.Height - 20 : 2));
            }
            Marker(p.Start, "Start", false, false); Marker(p.End, "End", true, false);
            if (p.LoopEnd > 0) { Marker(p.LoopStart, "LoopStart", false, true); Marker(p.LoopEnd, "LoopEnd", true, true); }
        }
    }
}
