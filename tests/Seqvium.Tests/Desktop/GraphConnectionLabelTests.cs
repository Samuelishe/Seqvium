// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphConnectionLabelTests
{
    [Theory]
    [InlineData("In", "Out")]
    [InlineData("Вход", "Выход")]
    public void SameNodePairConnectionsNameTheirActualInputSlots(string input, string output)
    {
        var source = GraphBuiltIns.Create(GraphBuiltIns.Source, new(0, 0));
        var mix = GraphBuiltIns.Create(GraphBuiltIns.Mix, new(400, 0));
        var first = new GraphConnection(Id<GraphConnection>.New(), source.Id, source.Ports[0].Id, mix.Id, mix.Ports[0].Id);
        var second = first with { Id = Id<GraphConnection>.New(), ToPortId = mix.Ports[1].Id };
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [source, mix], [first, second]);
        string Label(GraphConnection edge) => GraphConnectionLabels.Label(graph, edge,
            n => n.Id == source.Id ? "Source" : "Mix", input, output);
        Assert.Equal("Source → Mix · " + input + " 1", Label(first));
        Assert.Equal("Source → Mix · " + input + " 2", Label(second));
        Assert.DoesNotContain(first.Id.ToString(), Label(first));
        mix = mix with { Ports = [mix.Ports[1], mix.Ports[0], mix.Ports[2]] };
        graph = graph with { Nodes = [source, mix] };
        Assert.Equal("Source → Mix · " + input + " 2", Label(first));
        Assert.Equal("Source → Mix · " + input + " 1", Label(second));
    }

    [Fact]
    public void RetainedMissingEndpointDoesNotPreventDisconnectLabel()
    {
        var source = GraphBuiltIns.Create(GraphBuiltIns.Source, new(0, 0));
        var edge = new GraphConnection(Id<GraphConnection>.New(), source.Id, source.Ports[0].Id,
            Id<GraphNode>.New(), Id<GraphPort>.New());
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [source], [edge]);
        Assert.Equal("Source → ? · ?", GraphConnectionLabels.Label(graph, edge, _ => "Source", "In", "Out"));
        graph = graph with { Nodes = [] };
        Assert.Equal("? → ? · ?", GraphConnectionLabels.Label(graph, edge, _ => "Source", "In", "Out"));
    }

    [Fact]
    public void MultipleOutputsAndUnknownPortRoleRemainDistinguishable()
    {
        var source = GraphBuiltIns.Create(GraphBuiltIns.Source, new(0, 0));
        source = source with { Ports = source.Ports.Add(source.Ports[0] with { Id = Id<GraphPort>.New() }) };
        var gain = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(400, 0));
        gain = gain with { Ports = [gain.Ports[0] with { Role = "detector" }, gain.Ports[1]] };
        var edge = new GraphConnection(Id<GraphConnection>.New(), source.Id, source.Ports[1].Id, gain.Id, gain.Ports[0].Id);
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [source, gain], [edge]);
        Assert.Equal("Source · Out 2 → Gain · detector 1", GraphConnectionLabels.Label(graph, edge,
            n => n.Id == source.Id ? "Source" : "Gain", "In", "Out"));
    }
}
