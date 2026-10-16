// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

internal sealed partial class ArrangementCanvas
{
    internal Id<AutomationLane>? SelectedAutomation { get; private set; }
    internal Id<AutomationPoint>? SelectedAutomationPoint { get; private set; }
    internal event Action? AutomationPropertiesRequested;
    private AutomationLane? _automationDraft;
    private AutomationPoint? _automationPoint;

    internal void SelectAutomation(Id<AutomationLane> id, Id<AutomationPoint>? point = null)
    {
        Cancel();
        SelectedAutomation = id;
        SelectedAutomationPoint = point;
        Selected = null;
        SelectedAudio = null;
        SelectedSection = null;
        _selection.Clear();
        _audioSelection.Clear();
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    internal Point AutomationPosition(AutomationLane lane, AutomationPoint point) => new(
        HeaderWidth + point.Position.Ticks * TickPixels,
        RulerHeight + lane.Lane * LaneHeight + 7 + (1 - (double)point.Value) * (LaneHeight - 14));

    private AutomationLane? AutomationAt(Point point) => point.Y < RulerHeight
        ? null
        : _observation.Snapshot.State.AutomationLanes.SingleOrDefault(l =>
            l.Lane == (int)((point.Y - RulerHeight) / LaneHeight));

    private string AutomationValue(AutomationLane lane, decimal value)
    {
        double physical = ParameterAutomation.Physical(lane.Target.Parameter, (double)value);
        return lane.Target.Parameter == AutomationParameter.SynthCutoff
            ? physical.ToString("0", CultureInfo.InvariantCulture) + " Hz"
            : value == 0
                ? "−∞ dB"
                : physical.ToString("0.0", CultureInfo.InvariantCulture) + " dB";
    }

    private void DrawAutomation(DrawingContext context)
    {
        var state = _observation.Snapshot.State;
        double left = Math.Max(HeaderWidth, _viewport.Left),
            right = _viewport.Width > 0 ? _viewport.Right : Bounds.Width;
        // The stationary header owns its visible band; the curve remains in musical coordinates.
        foreach (var original in state.AutomationLanes)
        {
            var lane = _automationDraft?.Id == original.Id ? _automationDraft : original;
            double top = RulerHeight + lane.Lane * LaneHeight;
            if (_viewport.Height > 0 && (top + LaneHeight < _viewport.Top || top > _viewport.Bottom)) continue;
            bool resolved = ParameterAutomation.IsResolved(state, lane.Target);
            IBrush Brush(string key) => HostVisual.Brush(this, _shell.Preferences.Theme, key);
            using var opacity = context.PushOpacity(lane.Enabled && resolved ? 1 : .4);
            context.DrawRectangle(Brush(lane.Id == SelectedAutomation ? "Surface.Selected" : "Surface.Raised"), null,
                new Rect(left, top + 1, Math.Max(0, right - left), LaneHeight - 2));
            var pen = new Pen(Brush(resolved ? "Accent.Action" : "Status.Warning"), 1.5);

            Point At(double x)
            {
                long ticks = Math.Max(0, (long)((x - HeaderWidth) / TickPixels));
                return new(x, top + 7 + (1 - ParameterAutomation.ValueAt(lane, ticks)) * (LaneHeight - 14));
            }

            var previous = At(left);
            for (double x = left + 2; x < right + 2; x += 2)
            {
                var next = At(Math.Min(x, right));
                context.DrawLine(pen, previous, next);
                previous = next;
            }

            double labelRight = double.NegativeInfinity;
            foreach (var point in lane.Points)
            {
                var position = AutomationPosition(lane, point);
                if (position.X < left - 8 || position.X > right + 8) continue;
                bool selected = lane.Id == SelectedAutomation && point.Id == SelectedAutomationPoint;
                context.DrawEllipse(Brush(selected ? "Text.Primary" : "Surface.Raised"), pen, position,
                    selected ? 5 : 4, selected ? 5 : 4);
                if (selected || position.X >= labelRight + 12)
                {
                    HostVisual.Caption(this, _shell, context, AutomationValue(lane, point.Value),
                        new(Math.Min(position.X + 9, right - 75),
                            Math.Clamp(position.Y < top + 17 ? position.Y + 7 : position.Y - 18, top + 2, top + LaneHeight - 18)));
                    labelRight = position.X + 80;
                }
            }
        }
    }

    private bool DrawAutomationHeader(DrawingContext context, int slot, double y)
    {
        var state = _observation.Snapshot.State;
        if (state.AutomationLanes.SingleOrDefault(l => l.Lane == slot) is not { } lane) return false;
        bool resolved = ParameterAutomation.IsResolved(state, lane.Target);
        string name = lane.Target.Parameter == AutomationParameter.SynthCutoff
            ? state.Sounds.SingleOrDefault(s => s.Id == lane.Target.SoundId)?.Name ?? lane.TargetName
            : state.Mixer?.Channels.SingleOrDefault(c => c.Id == lane.Target.ChannelId)?.Name ?? lane.TargetName;
        using var clip = context.PushClip(new Rect(HeaderX + 9, y + 2, HeaderWidth - 14, LaneHeight - 4));
        if (SelectedAutomation == lane.Id)
            context.DrawRectangle(null, new Pen(HostVisual.Brush(this, _shell.Preferences.Theme, "Accent.Action"), 1),
                new Rect(HeaderX + 6, y + 2, HeaderWidth - 9, LaneHeight - 4));
        HostVisual.Caption(this, _shell, context, name, new(HeaderX + 12, y + 7));
        string parameter =
            _shell[
                lane.Target.Parameter == AutomationParameter.SynthCutoff ? "Automation.Cutoff" : "Automation.Volume"];
        HostVisual.Caption(this, _shell, context, (resolved ? lane.Enabled ? "◉ " : "○ " : "! ") + parameter,
            new(HeaderX + 12, y + 29));
        return true;
    }

    private bool AutomationPressed(PointerPressedEventArgs e)
    {
        var position = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsMiddleButtonPressed) return false;
        var lane = AutomationAt(position);
        if (lane is null)
        {
            if (position.Y >= RulerY + RulerHeight)
            {
                SelectedAutomation = null;
                SelectedAutomationPoint = null;
            }

            return false;
        }

        double Distance(AutomationPoint p)
        {
            var at = AutomationPosition(lane, p);
            return Math.Sqrt(Math.Pow(at.X - position.X, 2) + Math.Pow(at.Y - position.Y, 2));
        }

        var point = lane.Points.OrderBy(Distance).FirstOrDefault(p => Distance(p) <= 9);
        if (properties.IsLeftButtonPressed && point is null)
            point = lane.Points.SingleOrDefault(p => p.Position.Ticks == AutomationTick(position.X, e.KeyModifiers));
        if (position.X < HeaderX + HeaderWidth)
        {
            SelectAutomation(lane.Id);
            AutomationPropertiesRequested?.Invoke();
            e.Handled = true;
            return true;
        }

        SelectAutomation(lane.Id, point?.Id);
        Focus();
        if (properties.IsRightButtonPressed)
        {
            var edit = new MenuItem { Header = _shell["Automation.Properties"] };
            edit.Click += (_, _) => AutomationPropertiesRequested?.Invoke();
            var remove = new MenuItem
                { Header = _shell["Automation.DeletePoint"], IsEnabled = point is not null && lane.Points.Length > 1 };
            remove.Click += (_, _) => CommandSubmitted?.Invoke(DeleteAutomationPoint());
            ContextMenu = new ContextMenu { ItemsSource = new[] { edit, remove } };
            ContextMenu.Open(this);
            e.Handled = true;
            return true;
        }

        if (point is not null && e.ClickCount == 2)
        {
            AutomationPropertiesRequested?.Invoke();
            e.Handled = true;
            return true;
        }

        if (!properties.IsLeftButtonPressed) return true;
        _basis = Basis;
        _pressed = position;
        _automationDraft = lane;
        _automationPoint = point ?? new(Id<AutomationPoint>.New(), new(AutomationTick(position.X, e.KeyModifiers)),
            AutomationY(lane, position.Y));
        SelectedAutomationPoint = _automationPoint.Id;
        if (point is null) UpdateAutomationDraft(position, e.KeyModifiers);
        _pointer = e.Pointer;
        e.Pointer.Capture(this);
        SelectionChanged?.Invoke();
        InvalidateVisual();
        e.Handled = true;
        return true;
    }

    private long AutomationTick(double x, KeyModifiers modifiers)
    {
        long snap = modifiers.HasFlag(KeyModifiers.Alt)
            ? 1
            : modifiers.HasFlag(KeyModifiers.Shift)
                ? _observation.Snapshot.State.Settings.Meter.TicksPerBeat
                : SnapTicks;
        return (long)Math.Clamp(Math.Round((x - HeaderWidth) / TickPixels / snap), 0, 1000000d * BarTicks / snap) *
               snap;
    }

    private decimal AutomationY(AutomationLane lane, double y) =>
        (decimal)Math.Clamp(1 - (y - RulerHeight - lane.Lane * LaneHeight - 7) / (LaneHeight - 14), 0, 1);

    private void UpdateAutomationDraft(Point position, KeyModifiers modifiers)
    {
        if (_automationDraft is not { } lane || _automationPoint is not { } point) return;
        long ticks = AutomationTick(position.X, modifiers);
        var others = lane.Points.Where(p => p.Id != point.Id).ToArray();
        // Saturate against neighbours; dragging never silently overwrites another point.
        long lower = others.Where(p => p.Position.Ticks < point.Position.Ticks).Select(p => p.Position.Ticks + 1)
            .DefaultIfEmpty(0).Max();
        long upper = others.Where(p => p.Position.Ticks >= point.Position.Ticks).Select(p => p.Position.Ticks - 1)
            .DefaultIfEmpty(1000000L * BarTicks).Min();
        ticks = Math.Clamp(ticks, lower, Math.Max(lower, upper));
        var next = point with { Position = new(ticks), Value = AutomationY(lane, position.Y) };
        _automationDraft = lane with { Points = [.. others.Append(next).OrderBy(p => p.Position.Ticks)] };
    }

    private bool AutomationMoved(PointerEventArgs e)
    {
        if (_automationDraft is null || _pointer != e.Pointer)
        {
            if (_pointer is null && AutomationAt(e.GetPosition(this)) is { } hovered)
            {
                string parameter = _shell[hovered.Target.Parameter == AutomationParameter.SynthCutoff ? "Automation.Cutoff" : "Automation.Volume"];
                ToolTip.SetTip(this, hovered.TargetName + " · " + parameter + " · " +
                    (ParameterAutomation.IsResolved(_observation.Snapshot.State, hovered.Target)
                        ? AutomationValue(hovered, hovered.Points.Min(p => p.Value)) + " – " + AutomationValue(hovered, hovered.Points.Max(p => p.Value))
                        : _shell["Automation.Unavailable"]));
                Cursor = new Cursor(StandardCursorType.Cross);
                return true;
            }
            return false;
        }
        UpdateAutomationDraft(e.GetPosition(this), e.KeyModifiers);
        InvalidateVisual();
        e.Handled = true;
        return true;
    }

    private bool AutomationReleased(PointerReleasedEventArgs e)
    {
        if (_automationDraft is not { } lane || _pointer != e.Pointer) return false;
        var basis = _basis;
        Cancel();
        CommandSubmitted?.Invoke(_workflow.EditAutomationAsync(basis, lane));
        e.Handled = true;
        return true;
    }

    internal Task DeleteAutomationPoint()
    {
        var lane = _observation.Snapshot.State.AutomationLanes.SingleOrDefault(l => l.Id == SelectedAutomation);
        if (lane is null || lane.Points.Length <= 1 || SelectedAutomationPoint is null) return Task.CompletedTask;
        return _workflow.EditAutomationAsync(Basis,
            lane with { Points = [.. lane.Points.Where(p => p.Id != SelectedAutomationPoint)] });
    }

    private bool AutomationKey(KeyEventArgs e)
    {
        var lane = _observation.Snapshot.State.AutomationLanes.SingleOrDefault(l => l.Id == SelectedAutomation);
        if (lane is null) return false;
        if (e.Key is Key.A or Key.V && e.KeyModifiers == KeyModifiers.Control)
        { SelectedAutomation = null; SelectedAutomationPoint = null; return false; }
        if (e.Key == Key.Enter)
        {
            AutomationPropertiesRequested?.Invoke();
            e.Handled = true;
            return true;
        }

        if (e.Key is Key.Home or Key.End)
        {
            SelectedAutomationPoint = (e.Key == Key.Home ? lane.Points[0] : lane.Points[^1]).Id;
            SelectionChanged?.Invoke();
            InvalidateVisual();
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Delete)
        {
            CommandSubmitted?.Invoke(DeleteAutomationPoint());
            e.Handled = true;
            return true;
        }

        if (lane.Points.SingleOrDefault(p => p.Id == SelectedAutomationPoint) is not { } point) return true;
        if (e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down)) return false;
        long ticks = point.Position.Ticks;
        if (e.Key is Key.Left or Key.Right)
            ticks = Math.Clamp(ticks + (e.Key == Key.Left ? -SnapTicks : SnapTicks), 0, 1000000L * BarTicks);
        decimal value = Math.Clamp(point.Value + (e.Key == Key.Up ? .01m : e.Key == Key.Down ? -.01m : 0), 0, 1);
        if (!lane.Points.Any(p => p.Id != point.Id && p.Position.Ticks == ticks))
            CommandSubmitted?.Invoke(_workflow.EditAutomationAsync(Basis, lane with
            {
                Points =
                [
                    .. lane.Points
                        .Select(p => p.Id == point.Id ? p with { Position = new(ticks), Value = value } : p)
                        .OrderBy(p => p.Position.Ticks)
                ]
            }));
        e.Handled = true;
        return true;
    }
}
