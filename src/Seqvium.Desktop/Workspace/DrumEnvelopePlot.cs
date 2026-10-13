// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
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

/// <summary>Editable analytical envelopes and an explicitly derived waveform, with one captured gesture.</summary>
internal sealed class DrumEnvelopePlot(ShellSession shell) : Control
{
    private IPointer? _pointer;
    private int _handle;
    private double _horizon;
    private double _capturedScale;
    public decimal BasePitch { get; set; } = DrumVoice.RootPitch;
    public ImmutableDictionary<string, decimal> Values { get; set; } = DrumVoice.Defaults;
    public bool AmplitudeMode { get; set; }
    public bool IsDraft { get; set; }
    public float[]? Waveform { get; set; }
    public Func<bool>? Begin { get; set; }
    public event Action<string, decimal>? Preview;
    public event Action? Completed;
    public event Action? Cancelled;
    private Rect Plot => new(54, 28, Math.Max(1, Bounds.Width - 78), Math.Max(1, Bounds.Height - 126));
    private double Horizon => Math.Max(150, (double)Values["decay"] * 1.12);
    private double PitchScale => _pointer is null ? Math.Max(12, (double)Values["pitchAmount"] * 1.25) : _capturedScale;

    private Point At(double ms, double value, double horizon) => new(Plot.X + ms / horizon * Plot.Width,
        Plot.Bottom - Math.Clamp(value, 0, 1) * Plot.Height);

    private Point[] Handles(double horizon) => AmplitudeMode
        ?
        [
            At((double)Values["attack"], DrumVoice.AmplitudeEnvelope(Values, (double)Values["attack"]), horizon),
            At((double)Values["decay"] * .4, DrumVoice.AmplitudeEnvelope(Values, (double)Values["decay"] * .4),
                horizon),
            At((double)Values["decay"], 0, horizon)
        ]
        :
        [
            At(0, (double)Values["pitchAmount"] / PitchScale, horizon),
            At((double)Values["pitchDecay"] * .4,
                DrumVoice.PitchEnvelope(Values, (double)Values["pitchDecay"] * .4) / PitchScale, horizon),
            At((double)Values["pitchDecay"], 0, horizon)
        ];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Focusable = true;
        AutomationProperties.SetAutomationId(this, "Drum.Envelope");
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
        PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            var point = e.GetPosition(this);
            var handles = Handles(Horizon);
            double Distance(Point p) => Math.Sqrt(Math.Pow(p.X - point.X, 2) + Math.Pow(p.Y - point.Y, 2));
            int closest = Enumerable.Range(0, handles.Length).MinBy(i => Distance(handles[i]));
            if (Distance(handles[closest]) > 18 || Begin?.Invoke() != true) return;
            Focus();
            _handle = closest;
            _horizon = Horizon;
            _capturedScale = PitchScale;
            _pointer = e.Pointer;
            e.Pointer.Capture(this);
            e.Handled = true;
        };
        PointerMoved += (_, e) =>
        {
            if (_pointer != e.Pointer) return;
            var point = e.GetPosition(this);
            double time = Math.Max(0, (point.X - Plot.X) / Plot.Width * _horizon);
            double value = Math.Clamp((Plot.Bottom - point.Y) / Plot.Height, .0001, 1);
            string key;
            double proposed;
            if (AmplitudeMode)
            {
                key = _handle == 0 ? "attack" : _handle == 2 ? "decay" : "amplitudeCurve";
                proposed = _handle == 1 ? Math.Log(value) / Math.Log(.6) : time;
            }
            else
            {
                key = _handle == 0 ? "pitchAmount" : _handle == 2 ? "pitchDecay" : "pitchCurve";
                proposed = _handle == 0 ? value * PitchScale :
                    _handle == 2 ? time :
                    Math.Log(Math.Clamp(value * PitchScale / Math.Max(.01, (double)Values["pitchAmount"]), .0001, 1)) /
                    Math.Log(.6);
            }

            var parameter = DrumVoice.Parameters.Single(p => p.Key == key);
            Preview?.Invoke(key, Math.Clamp(decimal.Round((decimal)proposed, 2), parameter.Minimum, parameter.Maximum));
            e.Handled = true;
        };
        PointerReleased += (_, e) =>
        {
            if (_pointer != e.Pointer || e.InitialPressMouseButton != MouseButton.Left) return;
            _pointer = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            Completed?.Invoke();
        };
        PointerCaptureLost += (_, e) =>
        {
            if (_pointer == e.Pointer) Cancel();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && _pointer is not null)
            {
                Cancel();
                e.Handled = true;
            }
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
        IBrush Brush(string key) => HostVisual.Brush(this, shell.Preferences.Theme, key);

        void Text(string value, double x, double y, string role = "Text.Secondary", bool right = false)
        {
            var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                HostVisual.Typeface(this), HostVisual.Metric(this, "Type.Caption"), Brush(role));
            context.DrawText(text, new(x - (right ? text.Width : 0), y));
        }

        var plot = Plot;
        double horizon = _pointer is null ? Horizon : _horizon;
        string color = AmplitudeMode ? "Rack.Channel1" : "Accent.Action";
        context.DrawRectangle(Brush("Surface.Workspace"), new Pen(Brush("Border.Default"), 1), new Rect(Bounds.Size), 6,
            6);
        for (int line = 0; line <= 4; line++)
        {
            double y = plot.Y + plot.Height * line / 4;
            context.DrawLine(new Pen(Brush("Graph.Grid"), 1), new(plot.X, y), new(plot.Right, y));
            double level = 1 - line / 4d;
            Text(
                AmplitudeMode
                    ? (level * 100).ToString("0") + "%"
                    : (DrumVoice.Frequency(Values, BasePitch) * Math.Pow(2, level * PitchScale / 12)).ToString("0"), 6,
                y - 7);
            double x = plot.X + plot.Width * line / 4;
            context.DrawLine(new Pen(Brush("Graph.Grid"), 1), new(x, plot.Y), new(x, plot.Bottom));
            if (Bounds.Width >= 420 || line % 2 == 0)
                Text((horizon * line / 4).ToString("0") + " ms", line == 4 ? x : x - 10, plot.Bottom + 8,
                    right: line == 4);
        }

        Text(AmplitudeMode
            ? shell["Drum.Amplitude"]
            : shell["Drum.Pitch"] + " · " +
              PianoNotes.Name(decimal.Round(BasePitch + Values["tune"])) + " · " +
              DrumVoice.Frequency(Values, BasePitch).ToString("0.0") + " Hz", plot.X, 7, color);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            for (int i = 0; i <= 240; i++)
            {
                double ms = horizon * i / 240;
                var point = At(ms,
                    AmplitudeMode
                        ? DrumVoice.AmplitudeEnvelope(Values, ms)
                        : DrumVoice.PitchEnvelope(Values, ms) / PitchScale,
                    horizon);
                if (i == 0) path.BeginFigure(point, false);
                else path.LineTo(point);
            }

            path.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(Brush(color), 2.5, IsDraft ? DashStyle.Dash : null), geometry);
        foreach (var point in Handles(horizon))
        {
            context.DrawEllipse(Brush("Surface.Raised"), new Pen(Brush(color), 2), point, 6, 6);
            context.DrawEllipse(Brush(color), null, point, 2, 2);
        }

        double baseline = Bounds.Height - 34;
        context.DrawLine(new Pen(Brush("Graph.Grid"), 1), new(plot.X, baseline), new(plot.Right, baseline));
        if (Waveform is { } waveform)
        {
            bool light = shell.Preferences.Theme == HostTheme.Light;
            double height = light ? 32 : 24;
            for (int i = 0; i < waveform.Length / 2; i++)
            {
                double x = plot.X + i / (waveform.Length / 2d) * plot.Width;
                context.DrawLine(new Pen(Brush("Control.Fill"), light ? 1.7 : 1),
                    new(x, baseline - waveform[i * 2] * height),
                    new(x, baseline - waveform[i * 2 + 1] * height));
            }
        }

        Text(shell["Drum.Waveform"], plot.X, Bounds.Height - 15);
        if (IsFocused)
            context.DrawRectangle(null, new Pen(Brush("Focus.Active"), 2), new Rect(Bounds.Size).Deflate(2), 6, 6);
    }
}
