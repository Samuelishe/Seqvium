// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workspace;

internal readonly record struct GraphPoint(double X, double Y)
{
    public static GraphPoint operator +(GraphPoint a, GraphPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static GraphPoint operator -(GraphPoint a, GraphPoint b) => new(a.X - b.X, a.Y - b.Y);
    public static GraphPoint operator *(GraphPoint a, double scale) => new(a.X * scale, a.Y * scale);
    public double Length => Math.Sqrt(X * X + Y * Y);
}

internal readonly record struct GraphRect(double X, double Y, double Width, double Height)
{
    public bool Contains(GraphPoint point) => point.X >= X && point.Y >= Y && point.X <= X + Width && point.Y <= Y + Height;
}

internal sealed class GraphViewport
{
    internal const double MinimumZoom = 0.35, MaximumZoom = 2;
    public double Zoom { get; private set; } = 1;
    public GraphPoint Offset { get; private set; } = new(30, 30);
    public GraphPoint ToScreen(GraphPoint point) => point * Zoom + Offset;
    public GraphPoint ToGraph(GraphPoint point) => (point - Offset) * (1 / Zoom);
    public void Pan(GraphPoint delta) => Offset += delta;
    public void ZoomAt(GraphPoint anchor, double factor)
    {
        if (!double.IsFinite(factor) || factor <= 0) return;
        var graphAnchor = ToGraph(anchor);
        Zoom = Math.Clamp(Zoom * factor, MinimumZoom, MaximumZoom);
        Offset = anchor - graphAnchor * Zoom;
    }
    public void Fit(GraphDefinition graph, double width, double height)
    {
        if (graph.Nodes.IsEmpty || width <= 0 || height <= 0) return;
        var bounds = graph.Nodes.Select(node => GraphGeometry.Body(node)).ToArray();
        double left = bounds.Min(b => b.X), top = bounds.Min(b => b.Y);
        double right = bounds.Max(b => b.X + b.Width), bottom = bounds.Max(b => b.Y + b.Height);
        Zoom = Math.Clamp(Math.Min(Math.Max(1, width - 56) / (right - left),
            Math.Max(1, height - 56) / (bottom - top)), MinimumZoom, 1);
        Offset = new((width - (right - left) * Zoom) / 2 - left * Zoom,
            (height - (bottom - top) * Zoom) / 2 - top * Zoom);
    }
}

internal enum GraphHitKind { Background, Node, Port, Parameter, Connection }
internal sealed record GraphHit(GraphHitKind Kind, Id<GraphNode>? Node = null,
    Id<GraphPort>? Port = null, Id<GraphConnection>? Connection = null);

/// <summary>Shared drawing/hit geometry. Canvas size never changes canonical node coordinates.</summary>
internal static class GraphGeometry
{
    internal const double NodeWidth = 156, PortStep = 22;
    public static GraphRect Body(GraphNode node, GraphPosition? preview = null)
    {
        var position = preview ?? node.Position;
        int rows = Math.Max(node.Ports.Count(p => p.Direction == GraphPortDirection.Input),
            node.Ports.Count(p => p.Direction == GraphPortDirection.Output));
        return new(position.X, position.Y, NodeWidth, 44 + Math.Max(1, rows) * PortStep + 28);
    }
    public static GraphPoint Port(GraphNode node, GraphPort port, GraphPosition? preview = null)
    {
        var body = Body(node, preview);
        int index = node.Ports.Where(p => p.Direction == port.Direction).ToList().FindIndex(p => p.Id == port.Id);
        return new(body.X + (port.Direction == GraphPortDirection.Output ? body.Width : 0), body.Y + 44 + index * PortStep);
    }
    public static GraphRect Parameter(GraphNode node, GraphPosition? preview = null)
    {
        var body = Body(node, preview);
        return new(body.X + 8, body.Y + body.Height - 27, body.Width - 16, 22);
    }
    public static GraphPoint Bezier(GraphPoint from, GraphPoint to, double t)
    {
        double reach = Math.Max(48, Math.Abs(to.X - from.X) * 0.5), u = 1 - t;
        return from * (u * u * u) + (from + new GraphPoint(reach, 0)) * (3 * u * u * t) +
            (to - new GraphPoint(reach, 0)) * (3 * u * t * t) + to * (t * t * t);
    }
    public static double CableDistance(GraphPoint point, GraphPoint from, GraphPoint to)
    {
        double distance = double.MaxValue;
        var previous = from;
        // Adaptive bounded subdivision also covers backward connections and dense crossings.
        int steps = Math.Clamp((int)((to - from).Length / 8) + 32, 32, 512);
        for (int i = 1; i <= steps; i++)
        {
            var next = Bezier(from, to, (double)i / steps);
            var segment = next - previous;
            var offset = point - previous;
            double squared = segment.X * segment.X + segment.Y * segment.Y;
            double t = squared == 0 ? 0 : Math.Clamp((offset.X * segment.X + offset.Y * segment.Y) / squared, 0, 1);
            distance = Math.Min(distance, (point - (previous + segment * t)).Length);
            previous = next;
        }
        return distance;
    }
    public static IEnumerable<GraphNode> DrawOrder(GraphDefinition graph, Id<GraphNode>? selected) =>
        graph.Nodes.OrderBy(node => node.Id == selected ? 1 : 0);

    public static GraphHit HitTest(GraphDefinition graph, GraphPoint point, double zoom,
        Id<GraphNode>? selected = null, Id<GraphNode>? moving = null, GraphPosition? preview = null,
        Id<GraphConnection>? selectedCable = null)
    {
        double portRadius = 8 / zoom;
        foreach (var node in DrawOrder(graph, selected).Reverse())
        {
            var proposed = node.Id == moving ? preview : null;
            foreach (var port in node.Ports)
                if ((point - Port(node, port, proposed)).Length <= portRadius)
                    return new(GraphHitKind.Port, node.Id, port.Id);
            if (!Body(node, proposed).Contains(point)) continue;
            return new(node.Type == GraphBuiltIns.Gain && Parameter(node, proposed).Contains(point)
                ? GraphHitKind.Parameter : GraphHitKind.Node, node.Id);
        }
        foreach (var edge in graph.Connections.OrderBy(c => c.Id == selectedCable ? 1 : 0).Reverse())
        {
            var from = graph.Nodes.SingleOrDefault(n => n.Id == edge.FromNodeId);
            var to = graph.Nodes.SingleOrDefault(n => n.Id == edge.ToNodeId);
            var fromPort = from?.Ports.SingleOrDefault(p => p.Id == edge.FromPortId);
            var toPort = to?.Ports.SingleOrDefault(p => p.Id == edge.ToPortId);
            if (from is null || to is null || fromPort is null || toPort is null) continue;
            if (CableDistance(point, Port(from, fromPort, from.Id == moving ? preview : null),
                    Port(to, toPort, to.Id == moving ? preview : null)) <= 6 / zoom)
                return new(GraphHitKind.Connection, Connection: edge.Id);
        }
        return new(GraphHitKind.Background);
    }
}
