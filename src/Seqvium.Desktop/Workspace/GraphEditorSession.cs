// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed record GraphEditBasis(Guid Lifecycle, long Generation, Id<GraphAttachment> Attachment);
internal sealed record GraphMoveProposal(GraphEditBasis Basis, Id<GraphNode> Node, GraphPosition Before,
    GraphPosition After);

/// <summary>Explicit editing identity and transient interaction only; never stores an editable graph copy.</summary>
internal sealed class GraphEditorSession
{
    private Guid? _lifecycle;
    private WorkflowState? _observation;
    public Id<GraphAttachment>? Target { get; private set; }
    public Id<GraphNode>? SelectedNode { get; private set; }
    public Id<GraphConnection>? SelectedConnection { get; private set; }
    public GraphMoveProposal? Move { get; private set; }
    public ProjectState? State => _observation?.Snapshot.State;
    public GraphIntentReport? Report => Graph is { } graph && State is { } state
        ? GraphDiagnostics.Inspect(state).Single(r => r.GraphId == graph.Id) : null;
    public GraphViewport Viewport { get; } = new();
    public GraphAttachment? Attachment => _lifecycle == _observation?.Document.LifecycleId
        ? _observation?.Snapshot.State.GraphAttachments.SingleOrDefault(a => a.Id == Target) : null;
    public GraphDefinition? Graph => _observation?.Snapshot.State.Graphs.SingleOrDefault(g => g.Id == Attachment?.GraphId);
    public GraphEditBasis? Basis => Attachment is { } attachment && _observation is { } state
        ? new(state.Document.LifecycleId, state.Generation, attachment.Id) : null;
    public bool Observe(WorkflowState state)
    {
        bool changed = _observation?.Document.LifecycleId != state.Document.LifecycleId ||
            _observation?.Generation != state.Generation;
        _observation = state;
        if (changed) Cancel();
        if (_lifecycle != state.Document.LifecycleId || state.IsClosed)
        { Target = null; _lifecycle = null; SelectedNode = null; SelectedConnection = null; }
        if (Graph is not { } graph || !graph.Nodes.Any(n => n.Id == SelectedNode)) SelectedNode = null;
        if (Graph is not { } current || !current.Connections.Any(c => c.Id == SelectedConnection)) SelectedConnection = null;
        return changed;
    }
    public bool Open(WorkflowState state, Id<GraphAttachment> target)
    {
        Observe(state);
        if (state.IsClosed || !state.Snapshot.State.GraphAttachments.Any(a => a.Id == target)) return false;
        if (_lifecycle == state.Document.LifecycleId && Target == target) return true;
        Cancel(); _lifecycle = state.Document.LifecycleId; Target = target;
        SelectedNode = null; SelectedConnection = null;
        return true;
    }
    public void Select(GraphHit hit)
    {
        SelectedNode = hit.Node; SelectedConnection = hit.Connection;
    }
    public bool BeginMove(Id<GraphNode> node)
    {
        if (Basis is not { } basis || Graph?.Nodes.SingleOrDefault(n => n.Id == node) is not { } value) return false;
        Move = new(basis, node, value.Position, value.Position); return true;
    }
    public void UpdateMove(GraphPoint delta)
    {
        if (Move is not { } move) return;
        Move = move with { After = new(Math.Clamp(move.Before.X + delta.X, -1_000_000, 1_000_000),
            Math.Clamp(move.Before.Y + delta.Y, -1_000_000, 1_000_000)) };
    }
    public GraphMoveProposal? CompleteMove()
    {
        var result = Move; Move = null;
        return result is not null && result.Before != result.After && result.Basis == Basis ? result : null;
    }
    public void Cancel() => Move = null;
}

internal static class GraphConnectionRules
{
    public static string? Refusal(GraphDefinition graph, Id<GraphNode> firstNode, Id<GraphPort> firstPort,
        Id<GraphNode> secondNode, Id<GraphPort> secondPort)
    {
        var first = graph.Nodes.SingleOrDefault(n => n.Id == firstNode);
        var second = graph.Nodes.SingleOrDefault(n => n.Id == secondNode);
        var a = first?.Ports.SingleOrDefault(p => p.Id == firstPort);
        var b = second?.Ports.SingleOrDefault(p => p.Id == secondPort);
        if (first is null || second is null || a is null || b is null) return GraphReasons.MissingEndpoint;
        if (!graph.AdditionalData.IsEmpty) return GraphReasons.UnsupportedDependency;
        if ((CapabilityRefusal(first) ?? CapabilityRefusal(second)) is { } capability) return capability;
        if (a.Direction == GraphPortDirection.Input) (a, b) = (b, a);
        if (!GraphBuiltIns.Compatible(a, b)) return GraphReasons.IncompatiblePorts;
        if (graph.Connections.Any(c => c.ToPortId == b.Id)) return GraphReasons.InputCardinality;
        return null; // Cycles/incomplete topology remain savable intent, diagnosed by Core.
    }
    public static bool Supported(GraphNode node) => CapabilityRefusal(node) is null;
    private static string? CapabilityRefusal(GraphNode node)
    {
        var descriptor = GraphBuiltIns.Descriptors.SingleOrDefault(d => d.Type == node.Type);
        if (descriptor is null || descriptor.StateVersion != node.StateVersion || node.Extension is not null)
            return GraphReasons.MissingCapability;
        if (!node.AdditionalData.IsEmpty || node.Ports.Any(p => !p.AdditionalData.IsEmpty)) return GraphReasons.UnsupportedDependency;
        var inputs = node.Ports.Where(p => p.Direction == GraphPortDirection.Input).ToArray();
        var outputs = node.Ports.Where(p => p.Direction == GraphPortDirection.Output).ToArray();
        if (inputs.Length < descriptor.MinimumInputs || inputs.Length > descriptor.MaximumInputs ||
            outputs.Length != descriptor.Outputs || node.Ports.Select(p => p.Layout).Distinct().Count() != 1 ||
            inputs.Any(p => !p.Required || p.Role != (node.Type == GraphBuiltIns.Mix ? "slot" : "in")) || outputs.Any(p => p.Role != "out"))
            return GraphReasons.InvalidDeclaration;
        return null;
    }
}
