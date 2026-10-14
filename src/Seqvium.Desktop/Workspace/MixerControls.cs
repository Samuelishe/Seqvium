// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>dB fader and measured stereo ladder. Pointer capture commits once; the meter has no input authority.</summary>
internal sealed class MixerFader(ShellSession shell) : Control
{
    public decimal LevelDb { get; set; }
    public MixerMeter Meter { get; set; }
    public bool Playing { get; set; }
    public bool ShowScale { get; set; }
    public string AccentRole { get; set; } = "Accent.Action";
    public Func<bool>? Begin { get; set; }
    public event Action<decimal>? Preview;
    public event Action? Completed;
    public event Action? Cancelled;
    private IPointer? _pointer;
    private double _startY;
    private decimal _start;
    public bool HasGesture => _pointer is not null;

    internal static double Unit(decimal db) =>
        db <= -90 ? 0 : db < -30 ? (double)(db + 90) / 60 * .28 : .28 + (double)(db + 30) / 42 * .72;

    internal static decimal Db(double unit) => decimal.Round(
        unit <= 0 ? -90 : unit < .28 ? (decimal)(unit / .28 * 60 - 90) : (decimal)((unit - .28) / .72 * 42 - 30), 1);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Focusable = true;
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
        PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.GetPosition(this).X > Bounds.Width * .62 ||
                Begin?.Invoke() != true) return;
            Focus();
            e.Handled = true;
            if (e.ClickCount == 2)
            {
                Preview?.Invoke(0);
                Completed?.Invoke();
                return;
            }

            _pointer = e.Pointer;
            _startY = e.GetPosition(this).Y;
            _start = LevelDb;
            e.Pointer.Capture(this);
        };
        PointerMoved += (_, e) =>
        {
            if (e.Pointer != _pointer) return;
            double fine = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? .15 : 1;
            Preview?.Invoke(Db(Math.Clamp(
                Unit(_start) + (_startY - e.GetPosition(this).Y) / Math.Max(1, Bounds.Height - 32) * fine, 0, 1)));
            e.Handled = true;
        };
        PointerReleased += (_, e) =>
        {
            if (e.Pointer != _pointer || e.InitialPressMouseButton != MouseButton.Left) return;
            _pointer = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            Completed?.Invoke();
        };
        PointerCaptureLost += (_, _) => Cancel();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Cancel();
                e.Handled = true;
                return;
            }

            if (e.Key is not (Key.Up or Key.Down or Key.Home or Key.End) || Begin?.Invoke() != true) return;
            Preview?.Invoke(e.Key == Key.Home ? -90 :
                e.Key == Key.End ? 0 : Math.Clamp(LevelDb +
                                                  (e.Key == Key.Up ? 1 : -1) *
                                                  (e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? .1m : 1), -90, 12));
            Completed?.Invoke();
            e.Handled = true;
        };
    }

    public void Cancel()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null;
        pointer.Capture(null);
        Cancelled?.Invoke();
    }

    public override void Render(DrawingContext context)
    {
        IBrush Brush(string role) => HostVisual.Brush(this, shell.Preferences.Theme, role);
        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));
        double top = 9, h = Math.Max(1, Bounds.Height - 18), rail = Bounds.Width * .36, meter = Bounds.Width * .72;
        double Y(decimal db) => top + h * (1 - Unit(db));
        context.DrawRectangle(Brush("Mixer.Well"), new Pen(Brush("Graph.NodeBorder")), new Rect(rail - 3, top, 6, h), 2,
            2);
        foreach (decimal db in new decimal[] { 12, 0, -12, -24, -60 })
        {
            double y = Y(db);
            context.DrawLine(new Pen(Brush(db == 0 ? AccentRole : "Border.Default")), new(rail - 16, y),
                new(rail - 7, y));
            if (ShowScale)
                HostVisual.Caption(this, shell, context, db.ToString(CultureInfo.InvariantCulture), new(0, y - 6));
        }

        double cap = Y(LevelDb);
        var thumb = new Rect(rail - 10, cap - 7, 20, 14);
        context.DrawRectangle(Brush("Material.Shade"), null, thumb.Translate(new(0, 2)), 4, 4);
        context.DrawRectangle(Brush("Material.FaderCap"), new Pen(Brush(IsFocused ? "Focus.Active" : "Mixer.CapShade")),
            thumb, 2, 2);
        context.DrawLine(new Pen(Brush(AccentRole), 2), new(thumb.X + 3, cap), new(thumb.Right - 3, cap));
        context.DrawLine(new Pen(Brush("Material.Highlight")), new(thumb.X + 2, cap - 5),
            new(thumb.Right - 2, cap - 5));
        float[] values = [Playing ? Meter.PeakLeft : 0, Playing ? Meter.PeakRight : 0];
        float[] rms = [Playing ? Meter.RmsLeft : 0, Playing ? Meter.RmsRight : 0];
        for (int side = 0; side < 2; side++)
        {
            double x = meter + side * 6;
            context.DrawRectangle(Brush("Mixer.Well"), new Pen(Brush("Border.Default")), new Rect(x, top, 4, h),
                2, 2);
            double db = 20 * Math.Log10(Math.Max(1e-6, values[side]));
            for (int segment = 0; segment < 28; segment++)
            {
                double threshold = -60 + segment * (60d / 28), y = top + h * (1 - (segment + 1) / 28d);
                if (db < threshold) continue;
                context.DrawRectangle(
                    Brush(threshold >= 0 ? "Status.Critical" : threshold > -6 ? "Status.Warning" : "Status.Playing"),
                    null,
                    new Rect(x + .5, y + .5, 3, Math.Max(1, h / 28 - 1)));
            }

            double rmsY = top + h * (1 - Math.Clamp((20 * Math.Log10(Math.Max(1e-6, rms[side])) + 60) / 60, 0, 1));
            context.DrawLine(new Pen(Brush("Text.Primary")), new(x, rmsY), new(x + 4, rmsY));
            if (Meter.Overloads > 0) context.DrawRectangle(Brush("Status.Critical"), null, new Rect(x, 1, 4, 4));
        }
    }
}

internal sealed class MixerResponsePlot(ShellSession shell) : Control
{
    public MixerStripSettings Settings { get; set; } = new();
    public bool Compression { get; set; }
    public int SampleRate { get; set; } = 48000;
    public Func<bool>? Begin { get; set; }
    public event Action<MixerEq>? Preview;
    public event Action? Completed;
    public event Action? Cancelled;
    private IPointer? _pointer;
    private int _band = -1;
    private MixerEq? _start;
    private Rect Plot => new(34, 12, Math.Max(1, Bounds.Width - 50), Math.Max(1, Bounds.Height - 40));
    private double X(double hz) => Plot.X + Math.Log(hz / 20) / Math.Log(1000) * Plot.Width;
    private double Y(double db) => Plot.Center.Y - db / 24 * Plot.Height / 2;

    internal Point Handle(int band) => band switch
    {
        0 => new(X((double)Math.Max(20, Settings.Eq.HighPassHz)), Y(-3)),
        1 => new(X(120), Y((double)Settings.Eq.LowDb)),
        2 => new(X((double)Settings.Eq.MidHz), Y((double)Settings.Eq.MidDb)),
        _ => new(X(8000), Y((double)Settings.Eq.HighDb))
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Focusable = true;
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
        PointerPressed += (_, e) =>
        {
            if (Compression || !Settings.Eq.Enabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            var point = e.GetPosition(this);
            double Distance(int i) => new Vector(Handle(i).X - point.X, Handle(i).Y - point.Y).Length;
            int band = Enumerable.Range(0, 4).MinBy(Distance);
            if (Distance(band) > 18 || Begin?.Invoke() != true) return;
            _band = band;
            _start = Settings.Eq;
            _pointer = e.Pointer;
            e.Pointer.Capture(this);
            Focus();
            e.Handled = true;
        };
        PointerMoved += (_, e) =>
        {
            if (_pointer != e.Pointer || _start is not { } start) return;
            var p = e.GetPosition(this);
            decimal hz = decimal.Round((decimal)(20 * Math.Pow(1000, Math.Clamp((p.X - Plot.X) / Plot.Width, 0, 1))));
            decimal db = decimal.Round((decimal)Math.Clamp((Plot.Center.Y - p.Y) / Plot.Height * 48, -18, 18), 1);
            var eq = _band switch
            {
                0 => start with { HighPassHz = Math.Clamp(hz, 20, 2000) },
                1 => start with { LowDb = db },
                2 => start with { MidDb = db, MidHz = Math.Clamp(hz, 40, 16000) },
                _ => start with { HighDb = db }
            };
            Settings = Settings with { Eq = eq };
            Preview?.Invoke(eq);
            InvalidateVisual();
            e.Handled = true;
        };
        PointerReleased += (_, e) =>
        {
            if (_pointer != e.Pointer || e.InitialPressMouseButton != MouseButton.Left) return;
            _pointer = null;
            e.Pointer.Capture(null);
            _start = null;
            Completed?.Invoke();
            e.Handled = true;
        };
        PointerCaptureLost += (_, _) => Cancel();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && _pointer is not null)
            {
                Cancel();
                e.Handled = true;
            }
        };
    }

    internal void Cancel()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null;
        pointer.Capture(null);
        if (_start is { } start) Settings = Settings with { Eq = start };
        _start = null;
        Cancelled?.Invoke();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width < 80 || Bounds.Height < 60) return;
        var rect = Plot;
        IBrush Brush(string role) => HostVisual.Brush(this, shell.Preferences.Theme, role);
        context.DrawRectangle(Brush("Surface.Workspace"), new Pen(Brush("Border.Default")), rect, 4, 4);
        var filters = MixerDsp.Filters(Settings.Eq, SampleRate);
        foreach (int db in Compression
                     ? new[] { -60, -48, -36, -24, -12, 0, 12, 24 }
                     : new[] { -18, -12, -6, 0, 6, 12, 18 })
        {
            double y = Compression ? rect.Bottom - (db + 60) / 84d * rect.Height : Y(db);
            context.DrawLine(new Pen(Brush("Graph.Grid")), new(rect.X, y), new(rect.Right, y));
            HostVisual.Caption(this, shell, context, db.ToString(CultureInfo.InvariantCulture), new(0, y - 7));
        }

        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            for (int i = 0; i <= 160; i++)
            {
                double x = i / 160d;
                double value = Compression ? -60 + x * 60 : 20 * Math.Pow(1000, x);
                double db = Compression
                    ? value + (Settings.Compressor.Enabled
                        ? (double)Settings.Compressor.MakeupDb - MixerDsp.ReductionDb(value, Settings.Compressor)
                        : 0)
                    : 20 * Math.Log10(Math.Max(1e-6,
                        filters.Aggregate(1d, (a, c) => a * c.Magnitude(value, SampleRate))));
                var point = new Point(rect.X + x * rect.Width,
                    Compression
                        ? rect.Bottom - (Math.Clamp(db, -60, 24) + 60) / 84 * rect.Height
                        : Y(Math.Clamp(db, -24, 24)));
                if (i == 0) path.BeginFigure(point, false);
                else path.LineTo(point);
            }

            path.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(Brush("Material.Signal"), 5), geometry);
        context.DrawGeometry(null, new Pen(Brush("Accent.Action"), 2), geometry);
        foreach (var tick in Compression
                     ? new double[] { -60, -48, -36, -24, -12, 0 }
                     : new double[] { 20, 100, 1000, 10000, 20000 })
        {
            double x = Compression ? rect.X + (tick + 60) / 60 * rect.Width : X(tick);
            context.DrawLine(new Pen(Brush("Graph.Grid")), new(x, rect.Y), new(x, rect.Bottom));
            string label = !Compression && tick >= 1000
                ? (tick / 1000).ToString(CultureInfo.InvariantCulture) + "k"
                : tick.ToString(CultureInfo.InvariantCulture);
            HostVisual.Caption(this, shell, context, label, new(Math.Min(rect.Right - 18, x - 8), rect.Bottom + 5));
        }

        if (!Compression && Settings.Eq.Enabled)
            for (int band = 0; band < 4; band++)
            {
                var point = Handle(band);
                string role = "Rack.Channel" + (band + 2);
                context.DrawEllipse(Brush("Surface.Raised"), new Pen(Brush(role), _band == band ? 3 : 2), point, 5, 5);
            }

        AutomationProperties.SetName(this, Compression ? shell["Mixer.Transfer"] : shell["Mixer.Response"]);
    }
}
