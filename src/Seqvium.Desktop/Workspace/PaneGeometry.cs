// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Desktop.Workspace;

[Flags]
internal enum PaneEdges
{
    None = 0,
    Left = 1,
    Right = 2,
    Top = 4,
    Bottom = 8
}

/// <summary>Resize clamps the selected edge, never translates its opposite anchor.</summary>
internal static class PaneGeometry
{
    public const double EdgeThickness = 6;
    public const double CornerSize = 12;

    public static PaneBounds Resize(PaneBounds start, PaneEdges edges, double dx, double dy,
        double width, double height, PaneDefinition definition, PaneDock dock)
    {
        if (dock != PaneDock.Floating)
        {
            var size = Math.Clamp(start.Width + (dock == PaneDock.Right ? -dx : dx),
                Math.Min(definition.MinimumWidth, width / 2), width / 2);
            return new(dock == PaneDock.Right ? width - size : 0, 0, size, height);
        }

        var left = start.X;
        var top = start.Y;
        var right = start.X + start.Width;
        var bottom = start.Y + start.Height;
        // An anchor close to an edge can have less room than the preferred minimum after reflow.
        if (edges.HasFlag(PaneEdges.Left))
            left = Math.Clamp(left + dx, 0, right - Math.Min(definition.MinimumWidth, right));
        if (edges.HasFlag(PaneEdges.Right))
            right = Math.Clamp(right + dx, left + Math.Min(definition.MinimumWidth, width - left), width);
        if (edges.HasFlag(PaneEdges.Top))
            top = Math.Clamp(top + dy, 0, bottom - Math.Min(definition.MinimumHeight, bottom));
        if (edges.HasFlag(PaneEdges.Bottom))
            bottom = Math.Clamp(bottom + dy, top + Math.Min(definition.MinimumHeight, height - top), height);
        return new(left, top, right - left, bottom - top);
    }

    public static PaneBounds Move(PaneBounds start, double dx, double dy, double width, double height) =>
        start with
        {
            X = Math.Clamp(start.X + dx, 0, width - start.Width),
            Y = Math.Clamp(start.Y + dy, 0, height - start.Height)
        };

    public static PaneEdges HitTest(double x, double y, double width, double height, PaneDock dock,
        bool control = false)
    {
        // Six DIPs on either side of the visual boundary; corners extend twelve inward, six outward.
        if (control || !double.IsFinite(x) || !double.IsFinite(y) ||
            x < -EdgeThickness || y < -EdgeThickness ||
            x > width + EdgeThickness || y > height + EdgeThickness) return PaneEdges.None;
        if (dock != PaneDock.Floating && (y < 0 || y > height)) return PaneEdges.None;
        if (dock == PaneDock.Left) return x >= width - EdgeThickness ? PaneEdges.Right : PaneEdges.None;
        if (dock == PaneDock.Right) return x <= EdgeThickness ? PaneEdges.Left : PaneEdges.None;
        var horizontal = x <= EdgeThickness ? PaneEdges.Left :
            x >= width - EdgeThickness ? PaneEdges.Right : PaneEdges.None;
        var vertical = y <= EdgeThickness ? PaneEdges.Top :
            y >= height - EdgeThickness ? PaneEdges.Bottom : PaneEdges.None;
        if ((x <= CornerSize || x >= width - CornerSize) && (y <= CornerSize || y >= height - CornerSize))
            return (x <= CornerSize ? PaneEdges.Left : PaneEdges.Right) |
                   (y <= CornerSize ? PaneEdges.Top : PaneEdges.Bottom);
        return horizontal | vertical;
    }
}

/// <summary>One owner-thread gesture. Original placement and anchors stay stable through saturation/cancel.</summary>
internal sealed class PaneGesture
{
    private readonly WorkspaceState _state;
    private readonly PanePlacement _original;
    private readonly PaneBounds _start;
    private readonly PaneEdges _edges;
    private readonly double _width;
    private readonly double _height;
    private readonly double _originX;
    private readonly double _originY;
    private double _saturatedX;
    private double _saturatedY;
    private bool _finished;

    public PaneGesture(WorkspaceState state, string id, PaneEdges edges, double x, double y)
    {
        _state = state;
        _original = state.Get(id);
        _start = state.Bounds(id);
        _edges = edges;
        _width = state.Width;
        _height = state.Height;
        _originX = x;
        _originY = y;
    }

    public void Update(double x, double y)
    {
        if (_finished || !double.IsFinite(x) || !double.IsFinite(y)) return;
        var dx = x - _originX - _saturatedX;
        var dy = y - _originY - _saturatedY;
        var bounds = _edges == PaneEdges.None
            ? PaneGeometry.Move(_start, dx, dy, _width, _height)
            : PaneGeometry.Resize(_start, _edges, dx, dy, _width, _height,
                WorkspaceState.Definition(_original.InstanceId), _original.Dock);
        if (_edges != PaneEdges.None)
        {
            var actualX = _edges.HasFlag(PaneEdges.Left) ? bounds.X - _start.X : bounds.Width - _start.Width;
            var actualY = _edges.HasFlag(PaneEdges.Top) ? bounds.Y - _start.Y : bounds.Height - _start.Height;
            // Discard only blocked travel. A reversal acts on the visible edge on the next event.
            if ((_edges & (PaneEdges.Left | PaneEdges.Right)) != 0) _saturatedX += dx - actualX;
            if ((_edges & (PaneEdges.Top | PaneEdges.Bottom)) != 0) _saturatedY += dy - actualY;
        }

        _state.SetBounds(_original.InstanceId, bounds);
    }

    public void Complete() => _finished = true;

    public void Cancel()
    {
        if (_finished) return;
        _finished = true;
        if (!_state.IsDisposed) _state.RestorePlacement(_original);
    }
}
