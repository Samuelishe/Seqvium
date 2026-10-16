// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>ADSR schematic with separate logarithmic time segments and an untimed dashed sustain plateau.</summary>
internal sealed class SynthEnvelopePlot(ShellSession shell) : Control
{
    internal ImmutableDictionary<string, decimal> Values { get; set; } = SynthVoice.Defaults;
    internal Func<bool>? Begin { get; set; }
    internal event Action<string, decimal>? Preview;
    internal event Action? Completed;
    internal event Action? Cancelled;
    private IPointer? _pointer;
    private int _handle;
    private Rect Plot => new(20, 25, Math.Max(1, Bounds.Width - 40), Math.Max(1, Bounds.Height - 53));
    private static double Time(decimal value, double maximum) => Math.Log(1 + (double)value) / Math.Log(1 + maximum);
    internal Point[] Handles()
    {
        var r = Plot;
        Point At(double x, double y) => new(r.X + x * r.Width, r.Bottom - y * r.Height);
        return [At(.04 + .20 * Time(Values["attack"], 5000), 1),
            At(.29 + .20 * Time(Values["decay"], 5000), (double)Values["sustain"]),
            At(.70, (double)Values["sustain"]), At(.76 + .24 * Time(Values["release"], 8000), 0)];
    }
    protected override void OnInitialized()
    {
        base.OnInitialized(); Focusable = true;
        AutomationProperties.SetAutomationId(this, "Synth.Envelope");
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
        PointerPressed += (_, e) =>
        {
            if (_pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            var points = Handles(); var p = e.GetPosition(this);
            double Distance(Point point) => Math.Sqrt(Math.Pow(point.X - p.X, 2) + Math.Pow(point.Y - p.Y, 2));
            int index = Enumerable.Range(0, points.Length).MinBy(i => Distance(points[i]));
            if (Distance(points[index]) > 18 || Begin?.Invoke() != true) return;
            _handle = index; _pointer = e.Pointer; e.Pointer.Capture(this); Focus(); e.Handled = true;
        };
        PointerMoved += (_, e) =>
        {
            if (_pointer != e.Pointer) return;
            var r = Plot; var p = e.GetPosition(this);
            double x = (p.X - r.X) / r.Width;
            if (_handle == 2) Preview?.Invoke("sustain", decimal.Round((decimal)Math.Clamp((r.Bottom - p.Y) / r.Height, 0, 1), 3));
            else
            {
                string key = _handle == 0 ? "attack" : _handle == 1 ? "decay" : "release";
                double origin = _handle == 0 ? .04 : _handle == 1 ? .29 : .76;
                double width = _handle == 3 ? .24 : .20;
                double maximum = _handle == 3 ? 8000 : 5000;
                Preview?.Invoke(key, decimal.Round((decimal)(Math.Exp(Math.Clamp((x - origin) / width, 0, 1) * Math.Log(1 + maximum)) - 1), 1));
            }
            e.Handled = true;
        };
        PointerReleased += (_, e) =>
        {
            if (_pointer != e.Pointer || e.InitialPressMouseButton != MouseButton.Left) return;
            _pointer = null; e.Pointer.Capture(null); Completed?.Invoke(); e.Handled = true;
        };
        PointerCaptureLost += (_, _) => Cancel();
        PointerWheelChanged += (_, e) => { if (_pointer is not null) e.Handled = true; };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { Cancel(); e.Handled = true; } };
    }
    internal void Cancel()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null; pointer.Capture(null); Cancelled?.Invoke();
    }
    public override void Render(DrawingContext context)
    {
        IBrush B(string role) => HostVisual.Brush(this, shell.Preferences.Theme, role);
        var r = Plot; var points = Handles();
        context.DrawRectangle(B("Surface.Workspace"), new Pen(B("Border.Default"), 1), new Rect(Bounds.Size), 6, 6);
        for (int i = 0; i < 5; i++)
            context.DrawLine(new Pen(B("Graph.Grid"), 1), new(r.X, r.Y + r.Height * i / 4), new(r.Right, r.Y + r.Height * i / 4));
        HostVisual.Caption(this, shell, context, shell["Synth.Envelope"], new(12, 5));
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            path.BeginFigure(new(r.X, r.Bottom), false);
            path.LineTo(points[0]); path.LineTo(points[1]); path.EndFigure(false);
            path.BeginFigure(points[2], false); path.LineTo(points[3]); path.EndFigure(false);
        }
        context.DrawGeometry(null, new Pen(B("Accent.Action"), 2.5), geometry);
        context.DrawLine(new Pen(B("Accent.Action"), 2, DashStyle.Dash), points[1], points[2]);
        for (int i = 0; i < points.Length; i++)
        {
            context.DrawEllipse(B("Surface.Raised"), new Pen(B("Accent.Action"), 2), points[i], 6, 6);
            HostVisual.Caption(this, shell, context, "ADSR"[i].ToString(), new(points[i].X - 4, r.Bottom + 7));
        }
    }
}

/// <summary>Analytical oscillator period; never a measured output or animated scope.</summary>
internal sealed class SynthWaveform(ShellSession shell) : Control
{
    internal SynthVoice.Wave Wave { get; set; }
    internal string Accent { get; set; } = "Accent.Action";
    protected override void OnInitialized()
    {
        base.OnInitialized(); ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        IBrush B(string role) => HostVisual.Brush(this, shell.Preferences.Theme, role);
        double insetX = Bounds.Width <= 50 ? 3 : 12, insetY = Bounds.Height <= 32 ? 3 : 10;
        var r = new Rect(insetX, insetY, Math.Max(1, Bounds.Width - insetX * 2), Math.Max(1, Bounds.Height - insetY * 2));
        if (Bounds.Width > 50) context.DrawRectangle(B("Surface.Workspace"), null, new Rect(Bounds.Size), 4, 4);
        context.DrawLine(new Pen(B("Graph.Grid"), 1), new(r.X, r.Center.Y), new(r.Right, r.Center.Y));
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            for (int i = 0; i <= 240; i++)
            {
                double phase = i / 120d % 1;
                double value = Wave switch { SynthVoice.Wave.Sine => Math.Sin(phase * 2 * Math.PI),
                    SynthVoice.Wave.Triangle => 1 - 4 * Math.Abs(phase - .5), SynthVoice.Wave.Saw => phase * 2 - 1,
                    _ => phase < .5 ? 1 : -1 };
                var point = new Point(r.X + r.Width * i / 240, r.Center.Y - value * r.Height * .44);
                if (i == 0) path.BeginFigure(point, false); else path.LineTo(point);
            }
            path.EndFigure(false);
        }
        context.DrawGeometry(null, new Pen(B(Accent), 2.5), geometry);
    }
}
