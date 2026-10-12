// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workspace;

/// <summary>Human endpoint identity shared by selection feedback and connection actions.</summary>
internal static class GraphConnectionLabels
{
    public static string Label(GraphDefinition graph, GraphConnection edge, Func<GraphNode, string> nodeLabel,
        string inputLabel, string outputLabel)
    {
        var from = graph.Nodes.SingleOrDefault(n => n.Id == edge.FromNodeId);
        var to = graph.Nodes.SingleOrDefault(n => n.Id == edge.ToNodeId);
        string source = from is null ? "?" : nodeLabel(from);
        if (from is not null && from.Ports.Count(p => p.Direction == GraphPortDirection.Output) > 1)
            source += " · " + PortLabel(from, edge.FromPortId, inputLabel, outputLabel);
        return source + " → " + (to is null ? "?" : nodeLabel(to)) + " · " +
            (to is null ? "?" : PortLabel(to, edge.ToPortId, inputLabel, outputLabel));
    }

    private static string PortLabel(GraphNode node, Id<GraphPort> id, string inputLabel, string outputLabel)
    {
        var port = node.Ports.SingleOrDefault(p => p.Id == id);
        if (port is null) return "?";
        int slot = node.Ports.Where(p => p.Direction == port.Direction).ToList().FindIndex(p => p.Id == id) + 1;
        string role = port.Role switch { "in" or "slot" => inputLabel, "out" => outputLabel, _ => port.Role };
        return role + " " + slot;
    }
}
