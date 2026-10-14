// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>Physical level dial with relative vertical movement and one committed captured gesture.</summary>
internal sealed class GainDialControl(ShellSession shell) : Control
{
    private IPointer? _pointer;
    private Point _origin;
    private decimal _start;
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
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || Begin?.Invoke() != true) return;
            Focus();
            e.Handled = true;
            if (e.ClickCount == 2)
            {
                Preview?.Invoke(DefaultAmplitude);
                Completed?.Invoke();
                return;
            }

            _pointer = e.Pointer;
            _origin = e.GetPosition(this);
            _start = Amplitude ?? 1;
            e.Pointer.Capture(this);
            InvalidateVisual();
        };
        PointerMoved += (_, e) =>
        {
            if (_pointer != e.Pointer) return;
            decimal sensitivity = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? .001m : .005m;
            Preview?.Invoke(Math.Clamp(_start + (decimal)(_origin.Y - e.GetPosition(this).Y) * sensitivity, 0, 1));
            e.Handled = true;
        };
        PointerReleased += (_, e) =>
        {
            if (_pointer != e.Pointer || e.InitialPressMouseButton != MouseButton.Left) return;
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
                e.KeyModifiers is not (KeyModifiers.None or KeyModifiers.Shift) || Begin?.Invoke() != true) return;
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
            if (!Compact) context.DrawText(value, new(center.X - value.Width / 2, center.Y - value.Height / 2));
        }

        if (IsFocused) context.DrawEllipse(null, new Pen(Brush("Focus.Active"), 2), center, radius + 9, radius + 9);
    }
}
