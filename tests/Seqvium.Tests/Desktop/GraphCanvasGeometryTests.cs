// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphCanvasGeometryTests
{
    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(-1200, 450, 0.35)]
    [InlineData(1000000, -1000000, 2)]
    public void TransformsRoundTripAfterPanAndBoundedZoom(double x, double y, double scale)
    {
        var viewport = new GraphViewport(); viewport.Pan(new(-147, 291)); viewport.ZoomAt(new(80, 90), scale);
        var restored = viewport.ToGraph(viewport.ToScreen(new(x, y)));
        Assert.Equal(x, restored.X, 7); Assert.Equal(y, restored.Y, 7);
    }

    [Theory]
    [InlineData(0.0001, GraphViewport.MinimumZoom)]
    [InlineData(10000, GraphViewport.MaximumZoom)]
    [InlineData(1.15, 1.15)]
    public void ZoomPreservesPointerAnchorEvenAtLimits(double factor, double expected)
    {
        var viewport = new GraphViewport(); var pointer = new GraphPoint(217, 94);
        var before = viewport.ToGraph(pointer); viewport.ZoomAt(pointer, factor);
        Assert.Equal(expected, viewport.Zoom); var after = viewport.ToGraph(pointer);
        Assert.Equal(before.X, after.X, 8); Assert.Equal(before.Y, after.Y, 8);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidZoomDoesNotMoveViewport(double factor)
    {
        var viewport = new GraphViewport(); var before = viewport.Offset;
        viewport.ZoomAt(new(40, 50), factor); Assert.Equal(1, viewport.Zoom); Assert.Equal(before, viewport.Offset);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void MixPortsHaveDistinctStablePositionsAfterViewportReflow(int count)
    {
        var mix = GraphBuiltIns.Create(GraphBuiltIns.Mix, new(120, 40), mixInputs: count);
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [mix], []);
        var viewport = new GraphViewport(); viewport.Fit(graph, 640, 400); viewport.Fit(graph, 1920, 900);
        var inputs = mix.Ports.Where(p => p.Direction == GraphPortDirection.Input).ToArray();
        for (int i = 0; i < inputs.Length; i++)
        {
            var at = GraphGeometry.Port(mix, inputs[i]); Assert.Equal(120, at.X); Assert.Equal(84 + i * 22, at.Y);
            Assert.Equal(inputs[i].Id, GraphGeometry.HitTest(graph, viewport.ToGraph(viewport.ToScreen(at)), viewport.Zoom).Port);
        }
        Assert.Equal(new GraphPosition(120, 40), mix.Position);
    }

    [Fact]
    public void NodePortAndParameterHitsPrecedeCableAndBackground()
    {
        var fixture = new GraphFixture(false); var gain = fixture.Gain;
        var port = gain.Ports.First(); var at = GraphGeometry.Port(gain, port);
        Assert.Equal(GraphHitKind.Port, GraphGeometry.HitTest(fixture.Graph, at, 1).Kind);
        var body = GraphGeometry.Body(gain);
        Assert.Equal(GraphHitKind.Node, GraphGeometry.HitTest(fixture.Graph, new(body.X + 30, body.Y + 14), 1).Kind);
        Assert.Equal(GraphHitKind.Parameter, GraphGeometry.HitTest(fixture.Graph, new(body.X + 30, body.Y + body.Height - 15), 1).Kind);
        Assert.Equal(GraphHitKind.Background, GraphGeometry.HitTest(fixture.Graph, new(10000, 10000), 1).Kind);
    }

    [Fact]
    public void OverlapUsesDrawingOrderAndSelectedNodeIsInFront()
    {
        var first = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(0, 0));
        var second = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(0, 0));
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [first, second], []);
        Assert.Equal(second.Id, GraphGeometry.HitTest(graph, new(50, 10), 1).Node);
        Assert.Equal(first.Id, GraphGeometry.HitTest(graph, new(50, 10), 1, first.Id).Node);
        Assert.Equal(first.Ports[0].Id, GraphGeometry.HitTest(graph, new(0, 44), 1, first.Id).Port);
    }

    [Fact]
    public void CableHitUsesActualBezierAndFollowsTransientNodePosition()
    {
        var from = GraphBuiltIns.Create(GraphBuiltIns.Source, new(0, 0));
        var to = GraphBuiltIns.Create(GraphBuiltIns.Output, new(400, 150));
        var cable = new GraphConnection(Id<GraphConnection>.New(), from.Id, from.Ports[0].Id, to.Id, to.Ports[0].Id);
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [from, to], [cable]);
        var middle = GraphGeometry.Bezier(GraphGeometry.Port(from, from.Ports[0]), GraphGeometry.Port(to, to.Ports[0]), 0.5);
        Assert.Equal(cable.Id, GraphGeometry.HitTest(graph, middle, 1).Connection);
        var proposal = new GraphPosition(400, 350);
        var moved = GraphGeometry.Bezier(GraphGeometry.Port(from, from.Ports[0]), GraphGeometry.Port(to, to.Ports[0], proposal), 0.5);
        Assert.Equal(cable.Id, GraphGeometry.HitTest(graph, moved, 1, moving: to.Id, preview: proposal).Connection);
        Assert.Null(GraphGeometry.HitTest(graph, middle, 1, moving: to.Id, preview: proposal).Connection);
        Assert.Equal(new GraphPosition(400, 150), to.Position);
    }

    [Theory]
    [InlineData(640, 360)]
    [InlineData(1100, 600)]
    [InlineData(1920, 900)]
    public void FrameContainsOrdinaryGraphWithoutEditingCoordinates(double width, double height)
    {
        var nodes = new[] { GraphBuiltIns.Create(GraphBuiltIns.Source, new(-50, -30)),
            GraphBuiltIns.Create(GraphBuiltIns.Output, new(450, 180)) };
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [.. nodes], []);
        var viewport = new GraphViewport(); viewport.Fit(graph, width, height);
        foreach (var node in nodes)
        {
            var body = GraphGeometry.Body(node);
            var top = viewport.ToScreen(new(body.X, body.Y));
            var bottom = viewport.ToScreen(new(body.X + body.Width, body.Y + body.Height));
            Assert.InRange(top.X, 0, width); Assert.InRange(top.Y, 0, height);
            Assert.InRange(bottom.X, 0, width); Assert.InRange(bottom.Y, 0, height);
        }
        Assert.Equal(new GraphPosition(-50, -30), nodes[0].Position);
    }

    [Fact]
    public void TypedCompatibilityRejectsDirectionLayoutOccupiedInputAndUnknownCapability()
    {
        var source = GraphBuiltIns.Create(GraphBuiltIns.Source, new(0, 0));
        var gain = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(200, 0));
        var mono = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(200, 200), GraphBuiltIns.Mono);
        var unknown = gain with { Id = Id<GraphNode>.New(), Type = "unknown.processor",
            Ports = [.. gain.Ports.Select(p => p with { Id = Id<GraphPort>.New() })] };
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [source, gain, mono, unknown], []);
        string? Reason(GraphNode a, GraphPort p, GraphNode b, GraphPort q) => GraphConnectionRules.Refusal(graph, a.Id, p.Id, b.Id, q.Id);
        Assert.Null(Reason(source, source.Ports[0], gain, gain.Ports[0]));
        Assert.Null(Reason(gain, gain.Ports[0], source, source.Ports[0]));
        Assert.Equal(GraphReasons.IncompatiblePorts, Reason(source, source.Ports[0], gain, gain.Ports[1]));
        Assert.Equal(GraphReasons.IncompatiblePorts, Reason(source, source.Ports[0], mono, mono.Ports[0]));
        Assert.Equal(GraphReasons.MissingCapability, Reason(source, source.Ports[0], unknown, unknown.Ports[0]));
        graph = graph with { Connections = [new(Id<GraphConnection>.New(), source.Id, source.Ports[0].Id, gain.Id, gain.Ports[0].Id)] };
        Assert.Equal(GraphReasons.InputCardinality, Reason(source, source.Ports[0], gain, gain.Ports[0]));
        var branch = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(200, 300));
        graph = graph with { Nodes = graph.Nodes.Add(branch) };
        Assert.Null(Reason(source, source.Ports[0], branch, branch.Ports[0]));
    }

    [Fact]
    public void CrossingCableHitMatchesSelectedCableDrawingPriority()
    {
        var graph = CrossingGraph();
        var first = graph.Connections[0]; var last = graph.Connections[1];
        var crossing = CablePoint(graph, first, 0.5);
        Assert.Equal(last.Id, GraphGeometry.HitTest(graph, crossing, 1).Connection);
        Assert.Equal(first.Id, GraphGeometry.HitTest(graph, crossing, 1, selectedCable: first.Id).Connection);
    }

    [Theory]
    [InlineData(0.35, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0.35, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void NearCrossingSelectsNearestCableEvenWhenFartherCableIsOnTop(double zoom, bool fartherSelected)
    {
        var graph = CrossingGraph();
        var first = graph.Connections[0]; var last = graph.Connections[1];
        var point = CablePoint(graph, first, 0.5 + 0.015 / Math.Max(1, zoom));
        Assert.InRange(Distance(graph, first, point), 0, 0.01);
        Assert.InRange(Distance(graph, last, point) * zoom, 0.1, 6);
        Assert.Equal(first.Id, GraphGeometry.HitTest(graph, point, zoom,
            selectedCable: fartherSelected ? last.Id : null).Connection);
    }

    private static GraphPoint CablePoint(GraphDefinition graph, GraphConnection edge, double t)
    {
        var from = graph.Nodes.Single(n => n.Id == edge.FromNodeId);
        var to = graph.Nodes.Single(n => n.Id == edge.ToNodeId);
        return GraphGeometry.Bezier(GraphGeometry.Port(from, from.Ports.Single(p => p.Id == edge.FromPortId)),
            GraphGeometry.Port(to, to.Ports.Single(p => p.Id == edge.ToPortId)), t);
    }

    [Theory]
    [InlineData(0.35)]
    [InlineData(1)]
    [InlineData(2)]
    public void ExactCrossingTieUsesSelectionThenVisualOrder(double zoom)
    {
        var graph = CrossingGraph();
        var first = graph.Connections[0]; var last = graph.Connections[1];
        var point = CablePoint(graph, first, 0.5);
        Assert.InRange(Distance(graph, first, point) * zoom, 0, 0.01);
        Assert.InRange(Distance(graph, last, point) * zoom, 0, 0.01);
        Assert.Equal(last.Id, GraphGeometry.HitTest(graph, point, zoom).Connection);
        Assert.Equal(first.Id, GraphGeometry.HitTest(graph, point, zoom, selectedCable: first.Id).Connection);
        Assert.Equal(last.Id, GraphGeometry.HitTest(graph, point, zoom, selectedCable: last.Id).Connection);
        graph = graph with { Connections = [last, first] };
        Assert.Equal(first.Id, GraphGeometry.HitTest(graph, point, zoom).Connection);
    }

    [Theory]
    [InlineData(0.35, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0.35, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void NearlyCoincidentCableTieIsMeasuredInScreenPixels(double zoom, bool fartherSelected)
    {
        var graph = ParallelGraph(0.08);
        var first = graph.Connections[0]; var last = graph.Connections[1];
        var point = CablePoint(graph, first, 0.5);
        Assert.InRange(Distance(graph, last, point), 0.079, 0.081);
        Assert.Equal(zoom == 2 ? first.Id : last.Id, GraphGeometry.HitTest(graph, point, zoom,
            selectedCable: fartherSelected ? last.Id : null).Connection);
        Assert.Equal(first.Id, GraphGeometry.HitTest(graph, point, zoom, selectedCable: first.Id).Connection);
    }

    [Fact]
    public void CableTieCandidatesAreRelativeToGlobalMinimum()
    {
        var graph = ParallelGraph(0.08, 0.16);
        var point = CablePoint(graph, graph.Connections[0], 0.5);
        Assert.Equal(graph.Connections[1].Id, GraphGeometry.HitTest(graph, point, 1,
            selectedCable: graph.Connections[2].Id).Connection);
    }

    [Theory]
    [InlineData(0.35)]
    [InlineData(1)]
    [InlineData(2)]
    public void CableToleranceRemainsSixScreenPixels(double zoom)
    {
        var graph = ParallelGraph();
        var middle = CablePoint(graph, graph.Connections[0], 0.5);
        Assert.Equal(graph.Connections[0].Id, GraphGeometry.HitTest(graph, middle + new GraphPoint(0, 5.99 / zoom), zoom).Connection);
        Assert.Equal(GraphHitKind.Background, GraphGeometry.HitTest(graph, middle + new GraphPoint(0, 6.01 / zoom), zoom).Kind);
    }

    [Fact]
    public void NodeAndPortStillPrecedeOverlappingCables()
    {
        var graph = ParallelGraph(); var edge = graph.Connections[0];
        var source = graph.Nodes[0];
        var middle = CablePoint(graph, edge, 0.5);
        var cover = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(middle.X - 40, middle.Y - 15));
        graph = graph with { Nodes = graph.Nodes.Add(cover) };
        Assert.Equal(cover.Id, GraphGeometry.HitTest(graph, middle, 1, selectedCable: edge.Id).Node);
        Assert.Equal(source.Ports[0].Id, GraphGeometry.HitTest(graph,
            GraphGeometry.Port(source, source.Ports[0]), 1, selectedCable: edge.Id).Port);
    }

    private static GraphDefinition ParallelGraph(params double[] offsets)
    {
        var nodes = ImmutableArray.CreateBuilder<GraphNode>();
        var edges = ImmutableArray.CreateBuilder<GraphConnection>();
        foreach (double offset in new[] { 0d }.Concat(offsets))
        {
            var from = GraphBuiltIns.Create(GraphBuiltIns.Source, new(0, offset));
            var to = GraphBuiltIns.Create(GraphBuiltIns.Output, new(400, offset));
            nodes.Add(from); nodes.Add(to);
            edges.Add(new(Id<GraphConnection>.New(), from.Id, from.Ports[0].Id, to.Id, to.Ports[0].Id));
        }
        return new(Id<GraphDefinition>.New(), nodes.ToImmutable(), edges.ToImmutable());
    }

    private static double Distance(GraphDefinition graph, GraphConnection edge, GraphPoint point)
    {
        var from = graph.Nodes.Single(n => n.Id == edge.FromNodeId);
        var to = graph.Nodes.Single(n => n.Id == edge.ToNodeId);
        return GraphGeometry.CableDistance(point, GraphGeometry.Port(from, from.Ports.Single(p => p.Id == edge.FromPortId)),
            GraphGeometry.Port(to, to.Ports.Single(p => p.Id == edge.ToPortId)));
    }

    private static GraphDefinition CrossingGraph()
    {
        var a = GraphBuiltIns.Create(GraphBuiltIns.Source, new(0, 0));
        var b = GraphBuiltIns.Create(GraphBuiltIns.Output, new(400, 150));
        var c = GraphBuiltIns.Create(GraphBuiltIns.Source, new(0, 150));
        var d = GraphBuiltIns.Create(GraphBuiltIns.Output, new(400, 0));
        var first = new GraphConnection(Id<GraphConnection>.New(), a.Id, a.Ports[0].Id, b.Id, b.Ports[0].Id);
        var last = new GraphConnection(Id<GraphConnection>.New(), c.Id, c.Ports[0].Id, d.Id, d.Ports[0].Id);
        return new GraphDefinition(Id<GraphDefinition>.New(), [a, b, c, d], [first, last]);
    }
}
