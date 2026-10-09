// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Desktop.Workspace;

internal readonly record struct PaneBoundary(string InstanceId, PaneBounds Bounds, PaneDock Dock, bool Visible);

internal readonly record struct ResizeBoundary(string InstanceId, PaneEdges Edges);

/// <summary>Independent pane boundaries only. Entries describe actual visible rectangles, back to front.</summary>
internal static class WorkspaceBoundaryResolver
{
    public static ResizeBoundary? Resolve(IReadOnlyList<PaneBoundary> panes, double x, double y,
        double width, double height, bool control = false)
    {
        if (control || !double.IsFinite(x) || !double.IsFinite(y) ||
            x < 0 || y < 0 || x > width || y > height) return null;

        var surface = -1;
        for (var index = panes.Count - 1; index >= 0; index--)
            if (panes[index].Visible && Contains(panes[index].Bounds, x, y))
            {
                surface = index;
                break;
            }

        ResizeBoundary? chosen = null;
        var nearest = double.PositiveInfinity;
        for (var index = panes.Count - 1; index >= 0; index--)
        {
            var pane = panes[index];
            // A neighbour's outward tolerance never claims the visible working surface under the pointer.
            if (!pane.Visible || (surface >= 0 && surface != index)) continue;
            var bounds = pane.Bounds;
            var edges = PaneGeometry.HitTest(x - bounds.X, y - bounds.Y, bounds.Width, bounds.Height, pane.Dock);
            if (edges == PaneEdges.None) continue;
            var bx = edges.HasFlag(PaneEdges.Left) ? bounds.X :
                edges.HasFlag(PaneEdges.Right) ? bounds.X + bounds.Width :
                Math.Clamp(x, bounds.X, bounds.X + bounds.Width);
            var by = edges.HasFlag(PaneEdges.Top) ? bounds.Y :
                edges.HasFlag(PaneEdges.Bottom) ? bounds.Y + bounds.Height :
                Math.Clamp(y, bounds.Y, bounds.Y + bounds.Height);
            // Even in an empty gap, a tolerance projected from a covered boundary is unavailable.
            if (panes.Skip(index + 1).Any(front => front.Visible &&
                                                   bx > front.Bounds.X && bx < front.Bounds.X + front.Bounds.Width &&
                                                   by > front.Bounds.Y && by < front.Bounds.Y + front.Bounds.Height))
                continue;
            var distance = (x - bx) * (x - bx) + (y - by) * (y - by);
            if (distance >= nearest) continue; // Equal distance keeps the frontmost candidate.
            nearest = distance;
            chosen = new(pane.InstanceId, edges);
        }

        return chosen;
    }

    private static bool Contains(PaneBounds bounds, double x, double y) =>
        x >= bounds.X && x <= bounds.X + bounds.Width && y >= bounds.Y && y <= bounds.Y + bounds.Height;
}
