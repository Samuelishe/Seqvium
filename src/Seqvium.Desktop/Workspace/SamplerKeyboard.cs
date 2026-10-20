// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Pitch overview and captured selected-zone handles. All proposals remain local until pointer release.</summary>
internal sealed class SamplerKeyboard : Control
{
    private readonly ShellSession _shell;
    private ImmutableArray<SamplerZone> _zones = [];
    internal ImmutableArray<SamplerZone> Zones
    {
        get => _zones;
        set { _zones = value; Height = KeyTop + 50; }
    }
    internal Id<SamplerZone>? Selected { get; set; }
    internal IReadOnlyDictionary<Id<SamplerZone>, string> SourceNames { get; set; } =
        ImmutableDictionary<Id<SamplerZone>, string>.Empty;
    internal Func<bool>? Begin { get; set; }
    internal event Action<Id<SamplerZone>>? Selection;
    internal event Action<decimal>? NotePressed;
    internal decimal PreviewVelocity { get; set; } = .8m;
    private bool Layered => Zones.Any(z => z.LowVelocity != 0 || z.HighVelocity != 1);
    private double MapBottom => Layered ? 94 : 42;
    private double KeyTop => MapBottom + 18;

    internal Rect ZoneBounds(SamplerZone z) => new(X(z.LowKey),
        14 + (double)(1 - z.HighVelocity) * (MapBottom - 14),
        (z.HighKey - z.LowKey + 1) * Bounds.Width / _count,
        (double)(z.HighVelocity - z.LowVelocity) * (MapBottom - 14));
    internal event Action<ImmutableArray<SamplerZone>>? Preview, Completed;
    internal event Action? Cancelled;
    private IPointer? _pointer;
    internal bool HasGesture => _pointer is not null;
    private ImmutableArray<SamplerZone> _before;
    private int _handle;
    private double _first = 48, _count = 36;
    private double _zoom = 1;
    internal double FirstKey => _first;
    internal double VisibleKeys => _count;
    internal event Action? ViewChanged;

    internal SamplerKeyboard(ShellSession shell)
    {
        _shell = shell;
        Height = 110;
        Focusable = true;
        ClipToBounds = true;
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
        SizeChanged += (_, _) => Adapt();
    }

    private double X(double pitch) => (pitch - _first) / _count * Bounds.Width;
    internal double PitchX(double pitch) => X(pitch);
    private int KeyAt(double x) => Math.Clamp((int)Math.Floor(_first + x / Math.Max(1, Bounds.Width) * _count), 0, 127);

    internal void Fit()
    {
        _zoom = 1;
        Adapt();
        Reveal(Zones.SingleOrDefault(z => z.Id == Selected)?.RootPitch ?? 60, center: true);
    }

    private double DefaultCount =>
        Math.Clamp(Math.Floor(Bounds.Width / HostVisual.Metric(this, "Control.Height") / 6) * 6, 12, 48);

    private void Adapt()
    {
        if (Bounds.Width <= 0 || HasGesture) return;
        double center = _first + _count / 2;
        var selected = Zones.SingleOrDefault(z => z.Id == Selected);
        bool visible = selected is not null && selected.RootPitch >= (decimal)_first &&
                       selected.RootPitch < (decimal)(_first + _count);
        _count = Math.Clamp(Math.Round(DefaultCount * _zoom), 12,
            Math.Max(12, Math.Min(60, Math.Floor(Bounds.Width / 18))));
        Pan(center - _count / 2);
        if (visible) Reveal(selected!.RootPitch);
    }

    internal void Pan(double first)
    {
        _first = Math.Clamp(Math.Round(first), 0, 128 - _count);
        ViewChanged?.Invoke();
        InvalidateVisual();
    }

    internal void Reveal(decimal pitch, bool center = false)
    {
        if (center || pitch < (decimal)_first || pitch >= (decimal)(_first + _count))
            Pan(Math.Floor((double)pitch - _count / 2));
    }

    internal void Zoom(double factor, double anchor = .5)
    {
        double count = Math.Clamp(Math.Round(_count * factor), 12,
            Math.Max(12, Math.Min(60, Math.Floor(Bounds.Width / 18))));
        double first = _first + (_count - count) * anchor;
        _count = count;
        _zoom = count / DefaultCount;
        Pan(first);
    }

    internal void Cancel()
    {
        if (_pointer is null) return;
        Zones = _before;
        var p = _pointer;
        _pointer = null;
        p.Capture(null);
        Cancelled?.Invoke();
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || Begin?.Invoke() != true) return;
        Focus();
        var at = e.GetPosition(this);
        if (at.Y < 12)
        {
            _handle = 3;
            _before = Zones;
            _pointer = e.Pointer;
            e.Pointer.Capture(this);
            Pan(at.X / Math.Max(1, Bounds.Width) * 128 - _count / 2);
            e.Handled = true;
            return;
        }

        var selected = Zones.SingleOrDefault(z => z.Id == Selected);
        _handle = selected is null ? -1
            : at.Y is >= 14 and < 30 && Math.Abs(at.X - X(selected.LowKey)) <= 8 ? 0
            : at.Y >= MapBottom - 12 && at.Y < MapBottom + 2 && Math.Abs(at.X - X(selected.HighKey + 1)) <= 8 ? 1
            : at.Y >= MapBottom + 2 && at.Y < KeyTop && Math.Abs(at.X - X((double)selected.RootPitch + .5)) <= 9 ? 2 : -1;
        if (_handle >= 0)
        {
            _before = Zones;
            _pointer = e.Pointer;
            e.Pointer.Capture(this);
        }
        else
        {
            decimal velocity = at.Y >= KeyTop ? PreviewVelocity :
                Math.Clamp((decimal)(1 - (at.Y - 14) / (MapBottom - 14)), 0, 1);
            if (at.Y < KeyTop && Zones.SingleOrDefault(z => KeyAt(at.X) >= z.LowKey && KeyAt(at.X) <= z.HighKey &&
                    SamplerKeymap.ContainsVelocity(z, velocity)) is { } zone)
            {
                Selected = zone.Id;
                Selection?.Invoke(zone.Id);
            }
            if (at.Y >= KeyTop) NotePressed?.Invoke(KeyAt(at.X));
        }

        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointer != e.Pointer) return;
        if (_handle == 3)
        {
            Pan(e.GetPosition(this).X / Math.Max(1, Bounds.Width) * 128 - _count / 2);
            e.Handled = true;
            return;
        }

        var z = _before.Single(z => z.Id == Selected);
        int key = KeyAt(e.GetPosition(this).X);
        int low = _before.Where(n => n.Id != z.Id && SamplerKeymap.VelocityOverlaps(z, n) && n.HighKey < z.LowKey).Select(n => n.HighKey + 1).DefaultIfEmpty(0)
            .Max();
        int high = _before.Where(n => n.Id != z.Id && SamplerKeymap.VelocityOverlaps(z, n) && n.LowKey > z.HighKey).Select(n => n.LowKey - 1)
            .DefaultIfEmpty(127).Min();
        var changed = _handle switch
        {
            0 => z with { LowKey = Math.Clamp(key, low, z.HighKey) },
            1 => z with { HighKey = Math.Clamp(key, z.LowKey, high) },
            _ => z with { RootPitch = key }
        };
        Zones = [.. _before.Select(n => n.Id == z.Id ? changed : n)];
        Preview?.Invoke(Zones);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointer != e.Pointer) return;
        var p = _pointer;
        _pointer = null;
        p.Capture(null);
        if (_handle != 3 && !_before.SequenceEqual(Zones)) Completed?.Invoke(Zones);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        Cancel();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
        }

        if (_pointer is null && e.Key is Key.Left or Key.Right)
        {
            var ordered = Zones.OrderBy(z => z.LowKey).ToArray();
            int i = Array.FindIndex(ordered, z => z.Id == Selected);
            if (ordered.Length > 0)
                Selection?.Invoke(ordered[Math.Clamp(i + (e.Key == Key.Left ? -1 : 1), 0, ordered.Length - 1)].Id);
            e.Handled = true;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_pointer is not null) return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) Pan(_first - e.Delta.Y * 6 - e.Delta.X * 6);
        else Zoom(e.Delta.Y > 0 ? .75 : 1.333333333, e.GetPosition(this).X / Math.Max(1, Bounds.Width));
        e.Handled = true;
        InvalidateVisual();
    }

    public override void Render(DrawingContext c)
    {
        base.Render(c);
        IBrush B(string role) => HostVisual.Brush(this, _shell.Preferences.Theme, role);
        c.FillRectangle(B("Surface.Workspace"), new(Bounds.Size));
        foreach (var z in Zones.OrderBy(z => z.LowKey))
            c.DrawRectangle(B(z.Id == Selected ? "Accent.Action" : "Control.Track"), new Pen(B("Border.Default"), 1),
                new(z.LowKey / 128d * Bounds.Width, 1 + (double)(1 - z.HighVelocity) * 9,
                    (z.HighKey - z.LowKey + 1) / 128d * Bounds.Width, (double)(z.HighVelocity - z.LowVelocity) * 9));
        c.DrawRectangle(null, new Pen(B("Text.Secondary"), 1),
            new(_first / 128 * Bounds.Width, 0, _count / 128 * Bounds.Width, 11));
        var selectedZone = Zones.SingleOrDefault(z => z.Id == Selected);

        bool SelectedKey(int key) =>
            selectedZone is not null && key >= selectedZone.LowKey && key <= selectedZone.HighKey;

        bool RootKey(int key) => selectedZone is not null && key == (int)selectedZone.RootPitch;
        bool Black(int key) => key % 12 is 1 or 3 or 6 or 8 or 10;
        for (int key = 0; key < 128; key++)
        {
            if (Black(key)) continue;
            double left = key > 0 && Black(key - 1) ? key - .5 : key;
            double right = key < 127 && Black(key + 1) ? key + 1.5 : key + 1;
            var keyRect = new Rect(X(left), KeyTop, (right - left) * Bounds.Width / _count, 50);
            c.DrawRectangle(
                B(RootKey(key) ? "Accent.Action" : SelectedKey(key) ? "Surface.Selected" : "Piano.WhiteKey"),
                new Pen(B("Border.Default"), 1), keyRect);
            c.DrawRectangle(B("Material.Bevel"), null, keyRect.Deflate(1));
            c.DrawLine(new Pen(B("Material.Shade"), 2), new(keyRect.Left + 1, KeyTop + 48), new(keyRect.Right - 1, KeyTop + 48));
        }

        for (int key = 0; key < 128; key++)
        {
            if (Black(key))
            {
                var black = new Rect(X(key + .1), KeyTop, Bounds.Width / _count * .8, 30);
                c.DrawRectangle(B("Material.Shade"), null, black.Translate(new(1, 2)));
                c.DrawRectangle(B(RootKey(key) ? "Accent.Action" : "Piano.BlackKey"),
                    new Pen(B("Border.Default")), black, 1, 1);
                c.DrawRectangle(B("Material.Bevel"), null, black.Deflate(1), 1, 1);
            }

            if (key % 12 != 0) continue;
            c.DrawText(new FormattedText(PianoNotes.Name(key), System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, HostVisual.Typeface(this), HostVisual.Metric(this, "Type.Caption"),
                B(SelectedKey(key) && !RootKey(key) ? "Text.Primary" : "Piano.KeyInk")), new(X(key) + 1, KeyTop + 33));
        }

        int i = 0;
        foreach (var z in Zones.OrderBy(z => z.LowKey))
        {
            bool active = z.Id == Selected;
            var rect = ZoneBounds(z);
            c.DrawRectangle(B(active ? "Surface.Selected" : $"Rack.Step{i++ % 8}"),
                new Pen(B(active ? "Accent.Action" : "Border.Default"), active ? 2 : 1), rect);
            if (rect.Height > 4) c.DrawRectangle(B("Material.Bevel"), null, rect.Deflate(2));
            double captionX = Math.Max(0, rect.X) + 4;
            if (Math.Min(Bounds.Width, rect.Right) - captionX > 35 && rect.Height >= 16)
            {
                string title = Layered && SourceNames.TryGetValue(z.Id, out var name) ? name : PianoNotes.Name(z.RootPitch);
                using (c.PushClip(rect))
                    HostVisual.Caption(this, _shell, c, title + (Layered
                        ? $" · {z.LowVelocity * 100:0.##}–{z.HighVelocity * 100:0.##}%" : ""), new(captionX, rect.Y + 3));
            }
            if (!active) continue;
            c.FillRectangle(B("Accent.Action"), new(X(z.LowKey), 14, 6, 14));
            c.FillRectangle(B("Accent.Action"), new(X(z.HighKey + 1) - 6, MapBottom - 12, 6, 12));
            double root = X((double)z.RootPitch + .5);
            c.DrawLine(new Pen(B("Accent.Action"), 2), new(root, MapBottom + 2), new(root, KeyTop));
            c.DrawEllipse(B("Accent.Action"), null, new(root, MapBottom + 8), 5, 5);
        }
        if (Layered)
        {
            double y = 14 + (double)(1 - PreviewVelocity) * (MapBottom - 14);
            c.DrawLine(new Pen(B("Text.Secondary"), 1, DashStyle.Dash), new(0, y), new(Bounds.Width, y));
        }
    }
}
