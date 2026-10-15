// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>Relative clockwise rotation with one committed captured gesture and no press-position jump.</summary>
internal sealed class GainDialControl(ShellSession shell) : Control
{
    private IPointer? _pointer;
    private Point? _previousPosition;
    private decimal _gestureValue;
    private const double Sweep = Math.PI * 1.5;
    public decimal? Amplitude { get; set; }
    public decimal DefaultAmplitude { get; set; } = 1;
    public string? CenterText { get; set; }
    public string AccentRole { get; set; } = "Control.Fill";
    public string TrackRole { get; set; } = "Control.Track";
    public bool HasRecessedWell { get; set; }
    public bool Compact { get; set; }
    public bool HasGesture => _pointer is not null;
    public Func<bool>? Begin { get; set; }
    public event Action<decimal>? Preview;
    public event Action? Completed;
    public event Action? Cancelled;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (string.IsNullOrEmpty(AutomationProperties.GetAutomationId(this)))
            AutomationProperties.SetAutomationId(this, "NodeEditor.Dial");
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
        PointerPressed += (_, e) =>
        {
            if (_pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || Begin?.Invoke() != true) return;
            Focus();
            e.Handled = true;
            if (e.ClickCount == 2)
            {
                Preview?.Invoke(DefaultAmplitude);
                Completed?.Invoke();
                return;
            }

            _pointer = e.Pointer;
            _previousPosition = OutsideCenter(e.GetPosition(this));
            _gestureValue = Amplitude ?? 1;
            e.Pointer.Capture(this);
            InvalidateVisual();
        };
        PointerMoved += (_, e) =>
        {
            if (_pointer != e.Pointer) return;
            Update(e.GetPosition(this), e.KeyModifiers);
            e.Handled = true;
        };
        PointerReleased += (_, e) =>
        {
            if (_pointer != e.Pointer || e.InitialPressMouseButton != MouseButton.Left) return;
            Update(e.GetPosition(this), e.KeyModifiers);
            _pointer = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            Completed?.Invoke();
            InvalidateVisual();
        };
        PointerCaptureLost += (_, e) =>
        {
            if (_pointer == e.Pointer) Cancel();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && HasGesture)
            {
                Cancel();
                e.Handled = true;
                return;
            }

            if (e.Key is not (Key.Left or Key.Down or Key.Right or Key.Up or Key.Home or Key.End) ||
                HasGesture || e.KeyModifiers is not (KeyModifiers.None or KeyModifiers.Shift) || Begin?.Invoke() != true) return;
            decimal delta = e.KeyModifiers == KeyModifiers.Shift ? .001m : .01m;
            decimal value = e.Key == Key.Home ? 0 :
                e.Key == Key.End ? 1 : Math.Clamp((Amplitude ?? 1) +
                                                  (e.Key is Key.Down or Key.Left ? -delta : delta), 0, 1);
            Preview?.Invoke(value);
            Completed?.Invoke();
            e.Handled = true;
        };
        GotFocus += (_, _) => InvalidateVisual();
        LostFocus += (_, _) => InvalidateVisual();
    }

    private Point? OutsideCenter(Point point)
    {
        var offset = point - new Point(Bounds.Width / 2, Bounds.Height / 2);
        double deadRadius = Math.Max(3, Math.Min(Bounds.Width, Bounds.Height) * .12);
        return offset.X * offset.X + offset.Y * offset.Y >= deadRadius * deadRadius ? point : null;
    }

    private void Update(Point point, KeyModifiers modifiers)
    {
        var current = OutsideCenter(point);
        var previous = _previousPosition;
        _previousPosition = current;
        if (current is not { } to || previous is not { } from || to == from) return;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var a = from - center;
        var b = to - center;
        // A chord crossing the center is ambiguous. Re-anchor instead of turning it into half a turn.
        var segment = b - a;
        double along = Math.Clamp(-(a.X * segment.X + a.Y * segment.Y) /
            (segment.X * segment.X + segment.Y * segment.Y), 0, 1);
        if (OutsideCenter(center + a + segment * along) is null) return;
        double delta = Math.Atan2(a.X * b.Y - a.Y * b.X, a.X * b.X + a.Y * b.Y);
        decimal next = Math.Clamp(_gestureValue + (decimal)(delta / Sweep) *
            (modifiers.HasFlag(KeyModifiers.Shift) ? .2m : 1m), 0, 1);
        // Clamp each increment: reversing at a limit responds immediately, without accumulated overshoot.
        if (next == _gestureValue) return;
        _gestureValue = next;
        Preview?.Invoke(next);
    }

    public void Cancel()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null;
        pointer.Capture(null);
        Cancelled?.Invoke();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        IBrush Brush(string role) => HostVisual.Brush(this, shell.Preferences.Theme, role);
        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));
        double radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - (Compact ? 3 : 10);
        if (radius < (Compact ? 9 : 12)) return;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        Point At(double angle, double r) => center + new Vector(Math.Cos(angle) * r, Math.Sin(angle) * r);
        const double start = Math.PI * .75, sweep = Math.PI * 1.5;
        int ticks = Compact ? 12 : 32;
        for (int tick = 0; tick <= ticks; tick++)
        {
            double angle = start + tick / (double)ticks * sweep;
            bool lit = IsEnabled && Amplitude is >= 0 and <= 1 && tick / (decimal)ticks <= Amplitude;
            context.DrawLine(new Pen(Brush(lit ? AccentRole : TrackRole), tick % 8 == 0 ? 2.5 : 1.5),
                At(angle, radius), At(angle, radius + (Compact ? 2 : tick % 8 == 0 ? 6 : 3)));
        }

        double knob = radius - (Compact ? 3 : 5);
        if (HasRecessedWell)
        {
            context.DrawEllipse(Brush("Material.Shade"), null, center + new Vector(0, 1), knob + 4, knob + 4);
            context.DrawEllipse(Brush("Surface.Pressed"), new Pen(Brush("Graph.NodeBorder"), 1.5), center, knob + 3,
                knob + 3);
        }

        context.DrawEllipse(Brush("Material.Shade"), null, center + new Vector(0, 3), knob + 1, knob + 1);
        context.DrawEllipse(Brush("Material.Dial"), new Pen(Brush("Graph.NodeBorder"), 1), center, knob, knob);
        context.DrawEllipse(null, new Pen(Brush("Material.Highlight"), 1), center, knob - 2, knob - 2);
        if (Amplitude is >= 0 and <= 1)
        {
            double angle = start + (double)Amplitude.Value * sweep;
            context.DrawLine(new Pen(Brush(IsEnabled ? AccentRole : "State.Unavailable"), 3, lineCap: PenLineCap.Round),
                At(angle, knob - 11), At(angle, knob - 4));
            var value = new FormattedText(
                CenterText ?? ((double)Amplitude.Value * 100).ToString("0", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight, HostVisual.Typeface(this),
                HostVisual.Metric(this, "Type.Body"), Brush(IsEnabled ? "Text.Primary" : "State.Unavailable"));
            if (!Compact && value.Width <= knob * 2 - 4)
                context.DrawText(value, new(center.X - value.Width / 2, center.Y - value.Height / 2));
        }

        if (IsFocused) context.DrawEllipse(null, new Pen(Brush("Focus.Active"), 2), center, radius + 9, radius + 9);
    }
}
