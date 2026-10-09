// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Core;

public static class GraphReasons
{
    public const string ProcessingLimit = "graph.processing-limit";
    public const string UnsupportedScope = "graph.unsupported-scope";
    public const string UnsupportedDependency = "graph.unsupported-dependency";
    public const string MissingCapability = "graph.missing-capability";
    public const string InvalidDeclaration = "graph.invalid-declaration";
    public const string UnsupportedPort = "graph.unsupported-port";
    public const string InvalidGain = "graph.invalid-gain";
    public const string MissingEndpoint = "graph.missing-endpoint";
    public const string IncompatiblePorts = "graph.incompatible-ports";
    public const string DuplicateConnection = "graph.duplicate-connection";
    public const string InputCardinality = "graph.input-cardinality";
    public const string RequiredInput = "graph.required-input";
    public const string Cycle = "graph.directed-cycle";
    public const string MissingOutput = "graph.missing-output";
    public const string MissingSource = "graph.missing-source";
    public const string UnresolvedSource = "graph.unresolved-source";
    public const string SourceCoverage = "graph.source-coverage";
    public const string UnreachableSource = "graph.unreachable-source";
    public const string Aggregation = "graph.aggregation";
}

/// <summary>All F1 diagnostics block the selected attachment; display text is supplied outside Core.</summary>
public sealed record GraphDiagnostic(string Reason, Id<GraphDefinition> GraphId, Id<GraphAttachment>? AttachmentId,
    Id<GraphNode>? NodeId = null, Id<GraphPort>? PortId = null, Id<GraphConnection>? ConnectionId = null,
    Id<GraphSourceBinding>? SourceBindingId = null);
public sealed record GraphIntentReport(Id<GraphDefinition> GraphId, Id<GraphAttachment>? AttachmentId,
    ImmutableArray<GraphDiagnostic> Diagnostics)
{
    /// <summary>Eligibility for later preparation only; does not establish media availability or execution.</summary>
    public bool IsEligibleForPreparation => Diagnostics.IsEmpty;
}

public static class GraphDiagnostics
{
    private sealed record References(Dictionary<Id<ProcessingContext>, ProcessingContext> Contexts,
        Dictionary<Id<ProcessingContext>, PatternPlacement> ItemOwners, Dictionary<Id<Pattern>, Pattern> Patterns,
        Dictionary<Id<SoundDefinition>, SoundDefinition> Sounds);
    // Intent limits are independent of the much larger canonical document limits. F2 must validate runtime budgets.
    public const int MaximumNodes = 32;
    public const int MaximumConnections = 64;
    public const int MaximumSources = 8;
    public const int MaximumMixInputs = 8;

    public static ImmutableArray<GraphIntentReport> Inspect(ProjectState state)
    {
        ProjectValidation.Validate(state);
        var attachments = state.GraphAttachments.ToDictionary(item => item.GraphId);
        var references = new References(state.Contexts.ToDictionary(item => item.Id),
            state.Placements.Where(item => item.ItemContextId.HasValue).ToDictionary(item => item.ItemContextId!.Value),
            state.Patterns.ToDictionary(item => item.Id), state.Sounds.ToDictionary(item => item.Id));
        return [.. state.Graphs.OrderBy(item => item.Id.Value).Select(graph =>
            InspectGraph(references, graph, attachments.GetValueOrDefault(graph.Id)))];
    }

    private static GraphIntentReport InspectGraph(References references, GraphDefinition graph, GraphAttachment? attachment)
    {
        var diagnostics = new List<GraphDiagnostic>();
        void Add(string reason, Id<GraphNode>? node = null, Id<GraphPort>? port = null,
            Id<GraphConnection>? connection = null, Id<GraphSourceBinding>? binding = null) =>
            diagnostics.Add(new(reason, graph.Id, attachment?.Id, node, port, connection, binding));
        GraphIntentReport Finish() => new(graph.Id, attachment?.Id, [.. diagnostics
            .OrderBy(item => item.Reason, StringComparer.Ordinal).ThenBy(item => item.NodeId?.Value)
            .ThenBy(item => item.PortId?.Value).ThenBy(item => item.ConnectionId?.Value)
            .ThenBy(item => item.SourceBindingId?.Value)]);

        var context = attachment is null ? null : references.Contexts[attachment.ContextId];
        var placement = attachment is null ? null : references.ItemOwners.GetValueOrDefault(attachment.ContextId);
        var pattern = placement is null ? null : references.Patterns[placement.PatternId];
        var parts = pattern?.Parts ?? [];
        if (graph.Nodes.Length > MaximumNodes || graph.Connections.Length > MaximumConnections ||
            attachment?.Sources.Length > MaximumSources || parts.Length > MaximumSources)
        {
            Add(GraphReasons.ProcessingLimit);
            return Finish();
        }
        var nodes = graph.Nodes.ToDictionary(item => item.Id);
        var ports = graph.Nodes.SelectMany(node => node.Ports.Select(port => (node, port)))
            .ToDictionary(item => item.port.Id);
        var incoming = graph.Connections.GroupBy(item => item.ToPortId).ToDictionary(group => group.Key, group => group.ToArray());
        var predecessors = graph.Nodes.ToDictionary(item => item.Id, _ => new List<Id<GraphNode>>());
        var successors = graph.Nodes.ToDictionary(item => item.Id, _ => new List<Id<GraphNode>>());
        foreach (var node in graph.Nodes)
        {
            if (!node.AdditionalData.IsEmpty || node.Ports.Any(port => !port.AdditionalData.IsEmpty))
                Add(GraphReasons.UnsupportedDependency, node.Id);
            var descriptor = GraphBuiltIns.Descriptors.SingleOrDefault(item => item.Type == node.Type);
            if (descriptor is null || descriptor.StateVersion != node.StateVersion || node.Extension is not null)
                Add(GraphReasons.MissingCapability, node.Id);
            if (descriptor is not null)
            {
                var inputs = node.Ports.Where(port => port.Direction == GraphPortDirection.Input).ToArray();
                var outputs = node.Ports.Where(port => port.Direction == GraphPortDirection.Output).ToArray();
                if (inputs.Length < descriptor.MinimumInputs || inputs.Length > descriptor.MaximumInputs ||
                    outputs.Length != descriptor.Outputs || inputs.Any(port => !port.Required || port.Role !=
                        (node.Type == GraphBuiltIns.Mix ? "slot" : "in")) || outputs.Any(port => port.Role != "out") ||
                    node.Ports.Select(port => port.Layout).Distinct().Count() != 1)
                    Add(GraphReasons.InvalidDeclaration, node.Id);
                if (node.Type == GraphBuiltIns.Gain)
                {
                    if (node.Parameters.Count != 1 || !node.Parameters.TryGetValue(GraphBuiltIns.GainAmplitude, out var value) ||
                        value.ValueKind != System.Text.Json.JsonValueKind.Number || !value.TryGetDecimal(out var amplitude) ||
                        amplitude < GraphBuiltIns.GainMinimum || amplitude > GraphBuiltIns.GainMaximum)
                        Add(GraphReasons.InvalidGain, node.Id);
                }
                else if (!node.Parameters.IsEmpty) Add(GraphReasons.InvalidDeclaration, node.Id);
            }
            foreach (var port in node.Ports)
            {
                if (!GraphBuiltIns.SupportedAudio(port)) Add(GraphReasons.UnsupportedPort, node.Id, port.Id);
                int count = incoming.GetValueOrDefault(port.Id)?.Length ?? 0;
                if (port.Direction == GraphPortDirection.Input && port.Required && count == 0)
                    Add(GraphReasons.RequiredInput, node.Id, port.Id);
                if (port.Direction == GraphPortDirection.Input && count > 1)
                    Add(GraphReasons.InputCardinality, node.Id, port.Id);
            }
        }
        var edgeKeys = new HashSet<(Id<GraphNode>, Id<GraphPort>, Id<GraphNode>, Id<GraphPort>)>();
        foreach (var edge in graph.Connections.OrderBy(item => item.Id.Value))
        {
            if (!edge.AdditionalData.IsEmpty) Add(GraphReasons.UnsupportedDependency, connection: edge.Id);
            if (!edgeKeys.Add((edge.FromNodeId, edge.FromPortId, edge.ToNodeId, edge.ToPortId)))
                Add(GraphReasons.DuplicateConnection, connection: edge.Id);
            if (!ports.TryGetValue(edge.FromPortId, out var from) || from.node.Id != edge.FromNodeId ||
                !ports.TryGetValue(edge.ToPortId, out var to) || to.node.Id != edge.ToNodeId)
                Add(GraphReasons.MissingEndpoint, connection: edge.Id);
            else if (!GraphBuiltIns.Compatible(from.port, to.port))
                Add(GraphReasons.IncompatiblePorts, connection: edge.Id);
            // Every interpretable node dependency participates, including unsupported signal classes.
            if (nodes.ContainsKey(edge.FromNodeId) && nodes.ContainsKey(edge.ToNodeId))
            {
                successors[edge.FromNodeId].Add(edge.ToNodeId);
                predecessors[edge.ToNodeId].Add(edge.FromNodeId);
            }
        }
        var degrees = predecessors.ToDictionary(pair => pair.Key, pair => pair.Value.Count);
        var ready = new Queue<Id<GraphNode>>(degrees.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        int visited = 0;
        while (ready.TryDequeue(out var current))
        {
            visited++;
            foreach (var next in successors[current]) if (--degrees[next] == 0) ready.Enqueue(next);
        }
        if (visited != nodes.Count)
        {
            // Kahn's residual also includes downstream nodes. Identify actual cycle members with
            // bounded iterative reachability (at most 32 nodes), without claiming a compiled schedule.
            foreach (var start in degrees.Where(pair => pair.Value > 0).Select(pair => pair.Key))
            {
                var seen = new HashSet<Id<GraphNode>>();
                var pending = new Stack<Id<GraphNode>>(successors[start]);
                while (pending.TryPop(out var next))
                {
                    if (next == start) { Add(GraphReasons.Cycle, start); break; }
                    if (seen.Add(next)) foreach (var after in successors[next]) pending.Push(after);
                }
            }
        }

        var outputsInGraph = graph.Nodes.Where(node => node.Type == GraphBuiltIns.Output).ToArray();
        if (attachment?.OutputNodeId is not { } output || outputsInGraph.Length != 1 || outputsInGraph[0].Id != output)
            Add(GraphReasons.MissingOutput);
        var reachesOutput = new HashSet<Id<GraphNode>>();
        var work = new Stack<Id<GraphNode>>();
        if (attachment?.OutputNodeId is { } selected && nodes.ContainsKey(selected)) work.Push(selected);
        while (work.TryPop(out var nodeId))
            if (reachesOutput.Add(nodeId)) foreach (var before in predecessors[nodeId]) work.Push(before);

        if (attachment is null) { Add(GraphReasons.UnsupportedScope); return Finish(); }
        ProjectValidation.Require(context is not null, "Attachment context is required.");
        if (context.Level != LocalProcessingLevel.Item || placement is null)
            Add(GraphReasons.UnsupportedScope);
        if (context.Extension is not null || placement?.ContainingContextId is not null ||
            placement?.PartRelationships.Any(item => item.RouteId is not null || item.SharedPerformanceKey is not null || !item.AdditionalData.IsEmpty) == true ||
            !context.AdditionalData.IsEmpty || !graph.AdditionalData.IsEmpty || !attachment.AdditionalData.IsEmpty ||
            placement?.AdditionalData.IsEmpty == false || pattern?.AdditionalData.IsEmpty == false)
            Add(GraphReasons.UnsupportedDependency);
        var sourceNodes = graph.Nodes.Where(node => node.Type == GraphBuiltIns.Source).ToArray();
        foreach (var node in sourceNodes)
        {
            if (attachment.Sources.Count(binding => binding.NodeId == node.Id) != 1) Add(GraphReasons.MissingSource, node.Id);
            if (!reachesOutput.Contains(node.Id)) Add(GraphReasons.UnreachableSource, node.Id);
        }
        foreach (var binding in attachment.Sources)
        {
            if (!binding.AdditionalData.IsEmpty) Add(GraphReasons.UnsupportedDependency, binding.NodeId, binding: binding.Id);
            var part = parts.SingleOrDefault(item => item.Id == binding.PartId);
            if (!nodes.TryGetValue(binding.NodeId, out var node) || node.Type != GraphBuiltIns.Source ||
                placement?.Id != binding.PlacementId || part is null || binding.Boundary != GraphBuiltIns.PreItem)
                Add(GraphReasons.UnresolvedSource, binding.NodeId, binding: binding.Id);
            if (part is not null)
            {
                var sound = references.Sounds[part.SoundId];
                if (sound.Algorithm != PcmSampler.Algorithm || sound.Extension is not null || sound.ResourceIds.Length != 1 ||
                    sound.Parameters.Count != 2 || !sound.Parameters.TryGetValue("rootPitch", out var root) || root is < 0 or > 127 ||
                    !sound.Parameters.TryGetValue("releaseMilliseconds", out var release) || release is < 0 or > 1000 ||
                    part.Notes.Any(note => Math.Abs(note.Pitch - root) > 12))
                    Add(GraphReasons.MissingCapability, binding.NodeId, binding: binding.Id);
                if (!sound.AdditionalData.IsEmpty || !part.AdditionalData.IsEmpty || part.Notes.Any(note => !note.AdditionalData.IsEmpty))
                    Add(GraphReasons.UnsupportedDependency, binding.NodeId, binding: binding.Id);
            }
        }
        if (parts.IsEmpty || attachment.Sources.Length != parts.Length ||
            parts.Any(part => attachment.Sources.Count(binding => binding.PlacementId == placement?.Id && binding.PartId == part.Id) != 1))
            Add(GraphReasons.SourceCoverage);
        bool hasMix = graph.Nodes.Any(node => node.Type == GraphBuiltIns.Mix);
        if (context.IntentionalMix != hasMix || parts.Length > 1 && !hasMix) Add(GraphReasons.Aggregation);
        return Finish();
    }
}
