// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;

namespace Seqvium.Core;

public sealed partial class ProjectEdit
{
    /// <summary>Creates an independent empty graph, using or creating the placement's item context atomically.</summary>
    public GraphAttachment CreateItemGraph(Id<PatternPlacement> placementId, bool intentionalMix = false)
    {
        CheckActive();
        var placement = Find(State.Placements, item => item.Id == placementId);
        var contextId = placement.ItemContextId;
        if (contextId is null)
        {
            contextId = AddContext("Item processing", LocalProcessingLevel.Item, intentionalMix);
            ChangePlacement(placementId, item => item with { ItemContextId = contextId });
        }
        else
        {
            ProjectValidation.Require(!State.GraphAttachments.Any(item => item.ContextId == contextId), "Context already has a graph.");
            SetContextMix(contextId.Value, intentionalMix);
        }
        var graph = new GraphDefinition(Id<GraphDefinition>.New(), [], []);
        var attachment = new GraphAttachment(Id<GraphAttachment>.New(), graph.Id, contextId.Value, null, []);
        State = State with { Graphs = State.Graphs.Add(graph), GraphAttachments = State.GraphAttachments.Add(attachment) };
        return attachment;
    }

    public void SetContextMix(Id<ProcessingContext> contextId, bool intentionalMix)
    {
        CheckActive();
        State = State with { Contexts = Replace(State.Contexts, item => item.Id == contextId,
            item => item with { IntentionalMix = intentionalMix }) };
    }

    public GraphNode AddGraphNode(Id<GraphDefinition> graphId, string type, GraphPosition position,
        string layout = GraphBuiltIns.Stereo, int mixInputs = 2)
    {
        var node = GraphBuiltIns.Create(type, position, layout, mixInputs);
        AddGraphNode(graphId, node);
        return node;
    }

    /// <summary>Adds a declared node, including safely interpretable unavailable capability intent.</summary>
    public void AddGraphNode(Id<GraphDefinition> graphId, GraphNode node)
    {
        CheckActive();
        ArgumentNullException.ThrowIfNull(node);
        ChangeGraph(graphId, graph => graph with { Nodes = graph.Nodes.Add(node) });
    }

    public void SetGraphGain(Id<GraphDefinition> graphId, Id<GraphNode> nodeId, decimal amplitude)
    {
        CheckActive();
        ChangeGraphNode(graphId, nodeId, node =>
        {
            ProjectValidation.Require(node.Type == GraphBuiltIns.Gain && node.StateVersion == 1 && node.Extension is null,
                "Gain edit requires the built-in version 1 capability.");
            // Out-of-range intent remains savable, with a diagnostic rather than silent clamping.
            return node with { Parameters = node.Parameters.SetItem(GraphBuiltIns.GainAmplitude, JsonSerializer.SerializeToElement(amplitude)) };
        });
    }

    public void MoveGraphNode(Id<GraphDefinition> graphId, Id<GraphNode> nodeId, GraphPosition position)
    {
        CheckActive();
        ArgumentNullException.ThrowIfNull(position);
        GraphStructure.Position(position);
        ChangeGraphNode(graphId, nodeId, node => node with
        {
            Position = node.Position with { X = position.X, Y = position.Y }
        });
    }

    public Id<GraphConnection> ConnectGraphPorts(Id<GraphDefinition> graphId, Id<GraphNode> fromNodeId,
        Id<GraphPort> fromPortId, Id<GraphNode> toNodeId, Id<GraphPort> toPortId)
    {
        CheckActive();
        var graph = Find(State.Graphs, item => item.Id == graphId);
        var from = Find(Find(graph.Nodes, item => item.Id == fromNodeId).Ports, item => item.Id == fromPortId);
        var to = Find(Find(graph.Nodes, item => item.Id == toNodeId).Ports, item => item.Id == toPortId);
        ProjectValidation.Require(GraphBuiltIns.Compatible(from, to), "Graph port connection is incompatible.");
        ProjectValidation.Require(!graph.Connections.Any(item => item.ToPortId == toPortId), "Graph input already has a connection.");
        var connection = new GraphConnection(Id<GraphConnection>.New(), fromNodeId, fromPortId, toNodeId, toPortId);
        ChangeGraph(graphId, item => item with { Connections = item.Connections.Add(connection) });
        return connection.Id;
    }

    public void DisconnectGraphPorts(Id<GraphDefinition> graphId, Id<GraphConnection> connectionId)
    {
        CheckActive();
        ChangeGraph(graphId, graph => graph with { Connections = Remove(graph.Connections, item => item.Id == connectionId) });
    }

    public Id<GraphSourceBinding> BindGraphSource(Id<GraphAttachment> attachmentId, Id<GraphNode> nodeId,
        Id<PatternPlacement> placementId, Id<MusicalPart> partId)
    {
        CheckActive();
        var attachment = Find(State.GraphAttachments, item => item.Id == attachmentId);
        var node = Find(Find(State.Graphs, item => item.Id == attachment.GraphId).Nodes, item => item.Id == nodeId);
        var placement = Find(State.Placements, item => item.Id == placementId);
        ProjectValidation.Require(node.Type == GraphBuiltIns.Source && node.StateVersion == 1 &&
            placement.ItemContextId == attachment.ContextId, "Source requires the attachment's item placement.");
        _ = Find(Find(State.Patterns, item => item.Id == placement.PatternId).Parts, item => item.Id == partId);
        ProjectValidation.Require(!attachment.Sources.Any(item => item.NodeId == nodeId || item.PartId == partId),
            "Source node and part each require a unique binding.");
        var binding = new GraphSourceBinding(Id<GraphSourceBinding>.New(), nodeId, placementId, partId, GraphBuiltIns.PreItem);
        ChangeAttachment(attachmentId, item => item with { Sources = item.Sources.Add(binding) });
        return binding.Id;
    }

    public void DeleteGraphSource(Id<GraphAttachment> attachmentId, Id<GraphSourceBinding> bindingId)
    {
        CheckActive();
        ChangeAttachment(attachmentId, item => item with { Sources = Remove(item.Sources, binding => binding.Id == bindingId) });
    }

    public void SetGraphOutput(Id<GraphAttachment> attachmentId, Id<GraphNode>? nodeId)
    {
        CheckActive();
        var attachment = Find(State.GraphAttachments, item => item.Id == attachmentId);
        if (nodeId is { } id)
        {
            var node = Find(Find(State.Graphs, item => item.Id == attachment.GraphId).Nodes, item => item.Id == id);
            ProjectValidation.Require(node.Type == GraphBuiltIns.Output, "Output designation requires an Output node.");
        }
        ChangeAttachment(attachmentId, item => item with { OutputNodeId = nodeId });
    }

    public void DeleteGraphNode(Id<GraphDefinition> graphId, Id<GraphNode> nodeId)
    {
        CheckActive();
        ChangeGraph(graphId, graph => graph with
        {
            Nodes = Remove(graph.Nodes, item => item.Id == nodeId),
            Connections = [.. graph.Connections.Where(item => item.FromNodeId != nodeId && item.ToNodeId != nodeId)]
        });
        State = State with { GraphAttachments = [.. State.GraphAttachments.Select(item => item.GraphId == graphId ? item with
        {
            OutputNodeId = item.OutputNodeId == nodeId ? null : item.OutputNodeId,
            Sources = [.. item.Sources.Where(binding => binding.NodeId != nodeId)]
        } : item)] };
    }

    /// <summary>Deletes the graph and its attachment; the placement context and independent music remain.</summary>
    public void DeleteGraph(Id<GraphDefinition> graphId)
    {
        CheckActive();
        State = State with
        {
            Graphs = Remove(State.Graphs, item => item.Id == graphId),
            GraphAttachments = [.. State.GraphAttachments.Where(item => item.GraphId != graphId)]
        };
    }

    private void RemoveContextGraphs(Id<ProcessingContext> contextId)
    {
        var graphIds = State.GraphAttachments.Where(item => item.ContextId == contextId).Select(item => item.GraphId).ToHashSet();
        State = State with
        {
            GraphAttachments = [.. State.GraphAttachments.Where(item => item.ContextId != contextId)],
            Graphs = [.. State.Graphs.Where(item => !graphIds.Contains(item.Id))]
        };
    }
    private void ChangeGraph(Id<GraphDefinition> id, Func<GraphDefinition, GraphDefinition> change) =>
        State = State with { Graphs = Replace(State.Graphs, item => item.Id == id, change) };
    private void ChangeGraphNode(Id<GraphDefinition> graphId, Id<GraphNode> nodeId, Func<GraphNode, GraphNode> change) =>
        ChangeGraph(graphId, graph => graph with { Nodes = Replace(graph.Nodes, item => item.Id == nodeId, change) });
    private void ChangeAttachment(Id<GraphAttachment> id, Func<GraphAttachment, GraphAttachment> change) =>
        State = State with { GraphAttachments = Replace(State.GraphAttachments, item => item.Id == id, change) };
}
