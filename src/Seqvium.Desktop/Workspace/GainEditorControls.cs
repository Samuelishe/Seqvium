// SPDX-License-Identifier: Apache-2.0
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>Captured attenuation gesture, with release as the only commit and loss/Escape as cancellation.</summary>
internal sealed class GainLevelControl(ShellSession shell) : Control
{
    private IPointer? _pointer;
    public decimal? Amplitude { get; set; }
    public bool HasGesture => _pointer is not null;
    public Func<bool>? Begin { get; set; }
    public event Action<decimal>? Preview;
    public event Action? Completed;
    public event Action? Cancelled;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        AutomationProperties.SetAutomationId(this, "NodeEditor.Level");
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
        this.Bind(MinHeightProperty, this.GetResourceObservable("Level.Height"));
        PointerEntered += (_, _) => InvalidateVisual(); PointerExited += (_, _) => InvalidateVisual();
        PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || Begin?.Invoke() != true) return;
            Focus(); _pointer = e.Pointer; e.Pointer.Capture(this);
            Update(e.GetPosition(this)); e.Handled = true;
        };
        PointerMoved += (_, e) => { if (_pointer == e.Pointer) { Update(e.GetPosition(this)); e.Handled = true; } };
        PointerReleased += (_, e) =>
        {
            if (_pointer != e.Pointer || e.InitialPressMouseButton != MouseButton.Left) return;
            Update(e.GetPosition(this)); _pointer = null; e.Pointer.Capture(null);
            e.Handled = true; Completed?.Invoke();
        };
        PointerCaptureLost += (_, e) => { if (_pointer == e.Pointer) Cancel(); };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && HasGesture) { Cancel(); e.Handled = true; return; }
            if (e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.Left or Key.Right or Key.Home or Key.End)) return;
            if (Begin?.Invoke() != true) return;
            decimal value = e.Key == Key.Home ? 0 : e.Key == Key.End ? 1 :
                Math.Clamp((Amplitude ?? 1) + (e.Key == Key.Left ? -0.01m : 0.01m), 0, 1);
            Preview?.Invoke(value); Completed?.Invoke(); e.Handled = true;
        };
        GotFocus += (_, _) => InvalidateVisual(); LostFocus += (_, _) => InvalidateVisual();
    }
    private void Update(Point point) => Preview?.Invoke(decimal.Round((decimal)Math.Clamp(
        (point.X - HostVisual.Metric(this, "Level.Inset")) / Math.Max(1, Bounds.Width - HostVisual.Metric(this, "Level.Inset") * 2), 0, 1), 4));
    public void Cancel()
    {
        if (_pointer is not { } pointer) return;
        _pointer = null; pointer.Capture(null); Cancelled?.Invoke();
    }
    public override void Render(DrawingContext context)
    {
        // The whole control is an input target, including the space around the drawn track.
        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));
        double inset = HostVisual.Metric(this, "Level.Inset"), height = HostVisual.Metric(this, "Level.TrackHeight");
        var track = new Rect(inset, Bounds.Height / 2 - height / 2, Math.Max(1, Bounds.Width - inset * 2), height);
        double corner = HostVisual.Metric(this, "Control.Corner");
        context.DrawRectangle(HostVisual.Brush(this, shell.Preferences.Theme, "Control.Track"), new Pen(HostVisual.Brush(this, shell.Preferences.Theme, "Border.Default")), track, corner, corner);
        if (Amplitude is >= 0 and <= 1)
        {
            double x = track.X + (double)Amplitude.Value * track.Width;
            context.DrawRectangle(HostVisual.Brush(this, shell.Preferences.Theme, IsEnabled ? "Control.Fill" : "State.Unavailable"), null, new Rect(track.X, track.Y, x - track.X, track.Height), corner, corner);
            string thumbRole = !IsEnabled ? "State.Unavailable" : HasGesture ? "Control.Fill" : IsPointerOver ? "Focus.Active" : "Control.Thumb";
            context.DrawEllipse(HostVisual.Brush(this, shell.Preferences.Theme, thumbRole), new Pen(HostVisual.Brush(this, shell.Preferences.Theme, IsEnabled ? "Control.Fill" : "State.Unavailable"), HostVisual.Metric(this, "Focus.Width")), new(x, track.Center.Y), HostVisual.Metric(this, "Level.ThumbSize") / 2, HostVisual.Metric(this, "Level.ThumbSize") / 2);
        }
        if (IsFocused) context.DrawRectangle(null, new Pen(HostVisual.Brush(this, shell.Preferences.Theme, "Focus.Active"), HostVisual.Metric(this, "Focus.Width")), new Rect(Bounds.Size).Deflate(1));
    }
}

/// <summary>Analytical normalized transfer function, never a live signal or independent DSP preview.</summary>
internal sealed class GainTransferPlot(ShellSession shell) : Control
{
    public decimal? Amplitude { get; set; }
    public bool IsDraft { get; set; }
    public override void Render(DrawingContext context)
    {
        AutomationProperties.SetName(this, shell["NodeEditor.Transfer"] + ". output = amplitude × input");
        if (Bounds.Width < 80 || Bounds.Height < 60) return;
        var plot = new Rect(32, 20, Bounds.Width - 46, Bounds.Height - 48);
        var grid = new Pen(HostVisual.Brush(this, shell.Preferences.Theme, "Border.Default"), 1);
        context.DrawRectangle(HostVisual.Brush(this, shell.Preferences.Theme, "Surface.Workspace"), grid, plot);
        Point At(double x, double y) => new(plot.X + (x + 1) * plot.Width / 2, plot.Y + (1 - y) * plot.Height / 2);
        context.DrawLine(grid, At(-1, 0), At(1, 0)); context.DrawLine(grid, At(0, -1), At(0, 1));
        HostVisual.Caption(this, shell, context, shell["NodeEditor.OutputAxis"], new(0, 0));
        HostVisual.Caption(this, shell, context, shell["NodeEditor.InputAxis"], new(plot.Center.X - 18, Bounds.Height - 15));
        HostVisual.Caption(this, shell, context, "1", new(18, plot.Y));
        HostVisual.Caption(this, shell, context, "−1", new(8, plot.Bottom - 13));
        HostVisual.Caption(this, shell, context, "−1", new(plot.X, plot.Bottom + 1));
        HostVisual.Caption(this, shell, context, "0", new(plot.Center.X - 3, plot.Bottom + 1));
        HostVisual.Caption(this, shell, context, "1", new(plot.Right - 7, plot.Bottom + 1));
        if (Amplitude is >= 0 and <= 1)
        {
            double a = (double)Amplitude.Value;
            var pen = new Pen(HostVisual.Brush(this, shell.Preferences.Theme, IsDraft ? "Status.Warning" : "Accent.Action"), 2,
                IsDraft ? new DashStyle([4, 3], 0) : null);
            var fill = new StreamGeometry();
            using (var shape = fill.Open()) { shape.BeginFigure(At(-1, 0), true); shape.LineTo(At(-1, -a));
                shape.LineTo(At(1, a)); shape.LineTo(At(1, 0)); shape.EndFigure(true); }
            context.DrawGeometry(HostVisual.Brush(this, shell.Preferences.Theme, "Material.CurveFill"), null, fill);
            context.DrawLine(new Pen(HostVisual.Brush(this, shell.Preferences.Theme, "Material.Signal"), 6), At(-1, -a), At(1, a));
            context.DrawLine(pen, At(-1, -a), At(1, a));
        }
    }
}
