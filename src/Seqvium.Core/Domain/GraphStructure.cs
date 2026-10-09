// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;

namespace Seqvium.Core;

/// <summary>Safe canonical shape. Unresolved processing endpoints are intentionally not ownership errors.</summary>
internal static class GraphStructure
{
    public const double MaximumCoordinate = 1_000_000;

    public static void Validate(ProjectState state, Action<Guid> identity)
    {
        Present(state.Graphs); Present(state.GraphAttachments);
        foreach (var graph in state.Graphs)
        {
            identity(graph.Id.Value); Present(graph.Nodes); Present(graph.Connections);
            foreach (var node in graph.Nodes)
            {
                identity(node.Id.Value); Text(node.Type);
                ProjectValidation.Require(node.StateVersion > 0, "Node state version must be positive.");
                ProjectValidation.Require(node.Position is not null, "Graph position is required.");
                Position(node.Position);
                ProjectValidation.Require(node.Parameters is not null && node.Parameters.Count <= ProjectValidation.MaximumEntities,
                    "Bounded node parameters are required.");
                foreach (var (key, value) in node.Parameters)
                {
                    Text(key);
                    ProjectValidation.Require(value.ValueKind != JsonValueKind.Undefined, "Node parameter JSON is required.");
                }
                if (node.Extension is { } extension)
                {
                    Text(extension.ExtensionId);
                    ProjectValidation.Require(extension.StateVersion > 0 && extension.Payload.ValueKind != JsonValueKind.Undefined,
                        "Node extension requires versioned opaque JSON.");
                }
                Present(node.Ports);
                ProjectValidation.Require(node.Ports.Length <= 256, "Node declaration port limit exceeded.");
                foreach (var port in node.Ports)
                {
                    identity(port.Id.Value); Text(port.Role); Text(port.SignalClass); Text(port.Use);
                    Text(port.Layout); Text(port.RatePolicy);
                    ProjectValidation.Require(Enum.IsDefined(port.Direction) && Enum.IsDefined(port.Cardinality) &&
                        (port.Direction == GraphPortDirection.Input
                            ? port.Cardinality == GraphPortCardinality.Single
                            : port.Cardinality == GraphPortCardinality.FanOut && !port.Required),
                        "Unsafe port direction/cardinality metadata.");
                }
            }
            foreach (var connection in graph.Connections)
            {
                identity(connection.Id.Value);
                Reference(connection.FromNodeId); Reference(connection.FromPortId);
                Reference(connection.ToNodeId); Reference(connection.ToPortId);
            }
        }
        var graphs = state.Graphs.Select(item => item.Id).ToHashSet();
        var contexts = state.Contexts.Select(item => item.Id).ToHashSet();
        var attachedGraphs = new HashSet<Id<GraphDefinition>>();
        var attachedContexts = new HashSet<Id<ProcessingContext>>();
        foreach (var attachment in state.GraphAttachments)
        {
            identity(attachment.Id.Value); Present(attachment.Sources);
            ProjectValidation.Require(graphs.Contains(attachment.GraphId) && contexts.Contains(attachment.ContextId),
                "Attachment requires existing graph and context owners.");
            ProjectValidation.Require(attachedGraphs.Add(attachment.GraphId) && attachedContexts.Add(attachment.ContextId),
                "F1 attachments require independent graphs and one attachment per context.");
            if (attachment.OutputNodeId is { } output) Reference(output);
            foreach (var binding in attachment.Sources)
            {
                identity(binding.Id.Value); Reference(binding.NodeId); Reference(binding.PlacementId);
                Reference(binding.PartId); Text(binding.Boundary);
            }
        }
    }

    internal static void Position(GraphPosition position) => ProjectValidation.Require(
        double.IsFinite(position.X) && double.IsFinite(position.Y) &&
        Math.Abs(position.X) <= MaximumCoordinate && Math.Abs(position.Y) <= MaximumCoordinate,
        "Graph coordinates must be finite and within +/- 1000000 graph units.");
    private static void Reference<T>(Id<T> id) => ProjectValidation.Require(id.Value != Guid.Empty, "Graph reference cannot be empty.");
    private static void Text(string text) => ProjectValidation.Require(!string.IsNullOrWhiteSpace(text) &&
        text.Length <= ProjectValidation.MaximumTextLength, "Invalid graph semantic identifier.");
    private static void Present<T>(ImmutableArray<T> values) => ProjectValidation.Require(!values.IsDefault &&
        values.Length <= ProjectValidation.MaximumEntities && values.All(value => value is not null), "Invalid graph collection.");
}
