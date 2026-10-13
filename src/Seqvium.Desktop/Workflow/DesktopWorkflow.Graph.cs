// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private (GraphAttachment Attachment, GraphDefinition Graph) ValidateGraphBasis(GraphEditBasis basis)
    {
        if (_document.LifecycleId != basis.Lifecycle || _document.Generation != basis.Generation)
            throw new InvalidOperationException("Graph.Stale");
        var attachment = _document.Current.State.GraphAttachments.SingleOrDefault(a => a.Id == basis.Attachment)
                         ?? throw new InvalidOperationException("Graph.TargetMissing");
        return (attachment, _document.Current.State.Graphs.Single(g => g.Id == attachment.GraphId));
    }

    internal Task<bool> EditGraphAsync(GraphEditBasis basis, string description,
        Action<ProjectEdit, GraphAttachment, GraphDefinition> operation) => Command("Workflow.EditFailed", () =>
    {
        var (attachment, graph) = ValidateGraphBasis(basis);
        bool edited = _document.Edit(description, edit => operation(edit, attachment, graph));
        _status = "Workflow.Ready";
        return Task.FromResult(edited);
    });

    internal Task<bool> MoveGraphNodeAsync(GraphMoveProposal move) => EditGraphAsync(move.Basis, "Move graph node",
        (edit, attachment, graph) =>
        {
            if (graph.Nodes.Single(n => n.Id == move.Node).Position != move.Before)
                throw new InvalidOperationException("Graph.Stale");
            edit.MoveGraphNode(attachment.GraphId, move.Node, move.After);
        });

    internal Task<bool> AddGraphNodeAsync(GraphEditBasis basis, string type, GraphPosition position,
        string layout = GraphBuiltIns.Stereo, int inputs = 2) => EditGraphAsync(basis, "Create graph node",
        (edit, attachment, graph) =>
        {
            if (GraphCreationRules.Refusal(graph, type) is { } reason) throw new InvalidOperationException(reason);
            var node = edit.AddGraphNode(graph.Id, type, position, layout, inputs);
            if (type == GraphBuiltIns.Output) edit.SetGraphOutput(attachment.Id, node.Id);
            if (type == GraphBuiltIns.Mix) edit.SetContextMix(attachment.ContextId, true);
        });

    internal Task<bool> DeleteGraphElementAsync(GraphEditBasis basis, Id<GraphNode>? node,
        Id<GraphConnection>? cable) => EditGraphAsync(basis, "Delete graph element", (edit, attachment, graph) =>
    {
        if (node is { } nodeId)
        {
            var removed = graph.Nodes.Single(n => n.Id == nodeId);
            edit.DeleteGraphNode(graph.Id, nodeId);
            if (removed.Type == GraphBuiltIns.Mix && graph.Nodes.Count(n => n.Type == GraphBuiltIns.Mix) == 1)
                edit.SetContextMix(attachment.ContextId, false);
        }
        else if (cable is { } cableId) edit.DisconnectGraphPorts(graph.Id, cableId);
    });

    internal Task<bool> ConnectGraphAsync(GraphEditBasis basis, Id<GraphNode> firstNode, Id<GraphPort> firstPort,
        Id<GraphNode> secondNode, Id<GraphPort> secondPort) => EditGraphAsync(basis, "Connect graph ports",
        (edit, _, graph) =>
        {
            if (GraphConnectionRules.Refusal(graph, firstNode, firstPort, secondNode, secondPort) is { } reason)
                throw new InvalidOperationException(reason);
            var a = graph.Nodes.Single(n => n.Id == firstNode).Ports.Single(p => p.Id == firstPort);
            if (a.Direction == GraphPortDirection.Input)
                edit.ConnectGraphPorts(graph.Id, secondNode, secondPort, firstNode, firstPort);
            else edit.ConnectGraphPorts(graph.Id, firstNode, firstPort, secondNode, secondPort);
        });

    internal Task<bool> SetGraphGainAsync(GraphEditBasis basis, Id<GraphNode> node, decimal amplitude)
        => EditGraphAsync(basis, "Change graph Gain", (edit, attachment, _) =>
        {
            if (amplitude is < 0m or > 1m) throw new InvalidOperationException(GraphReasons.InvalidGain);
            edit.SetGraphGain(attachment.GraphId, node, amplitude);
        });

    internal Task<bool> SetGraphGainAsync(NodeGainProposal proposal) =>
        EditGraphAsync(new(proposal.Target.LifecycleId, proposal.Generation, proposal.Target.AttachmentId),
            "Change graph Gain", (edit, attachment, graph) =>
            {
                var node = graph.Nodes.SingleOrDefault(n => n.Id == proposal.Target.NodeId);
                if (attachment.GraphId != proposal.Target.GraphDefinitionId || node is null ||
                    node.Type != GraphBuiltIns.Gain || !GraphConnectionRules.Supported(node) ||
                    node.Parameters.Keys.Any(k => k != GraphBuiltIns.GainAmplitude) ||
                    NodeEditorSession.ReadAmplitude(node) != proposal.Before)
                    throw new InvalidOperationException("Graph.Stale");
                if (proposal.After is < 0m or > 1m) throw new InvalidOperationException(GraphReasons.InvalidGain);
                edit.SetGraphGain(graph.Id, node.Id, proposal.After);
            });

    // Existing discovery freezes document/storage ownership before its bounded worker reads.
    internal async Task<SourceDiscovery?> InspectNodeSourcesAsync(NodeEditorTarget target, long generation,
        CancellationToken cancellation)
    {
        var pending = await _owner.Run(() =>
        {
            if (_closed || _document.LifecycleId != target.LifecycleId || _document.Generation != generation)
                return Task.FromResult<Task<SourceDiscovery>?>(null);
            var attachment = _document.Current.State.GraphAttachments.SingleOrDefault(a => a.Id == target.AttachmentId);
            var node = _document.Current.State.Graphs.SingleOrDefault(g => g.Id == attachment?.GraphId)?.Nodes
                .SingleOrDefault(n => n.Id == target.NodeId);
            return Task.FromResult(attachment?.GraphId != target.GraphDefinitionId ||
                                   node is not { Type: GraphBuiltIns.Source } ||
                                   !GraphConnectionRules.Supported(node)
                ? null
                : SourceAccess.DiscoverProjectAsync(_document, SourceAccess.MaximumEntries, cancellation));
        });
        return pending is null ? null : await pending.ConfigureAwait(false);
    }

    internal Task<bool> BindGraphSourceAsync(GraphEditBasis basis, Id<GraphNode> node, Id<MusicalPart> part)
        => EditGraphAsync(basis, "Bind graph source", (edit, attachment, _) =>
        {
            var placement = _document.Current.State.Placements.Single(p => p.ItemContextId == attachment.ContextId);
            var previous = attachment.Sources.SingleOrDefault(s => s.NodeId == node);
            if (previous is not null) edit.DeleteGraphSource(attachment.Id, previous.Id);
            edit.BindGraphSource(attachment.Id, node, placement.Id, part);
        });

    internal static PatternPlacement SourceDestination(ProjectState state, GraphAttachment attachment)
    {
        var placement = state.Placements.SingleOrDefault(p => p.ItemContextId == attachment.ContextId)
                        ?? throw new InvalidOperationException("Graph.TargetMissing");
        var pattern = state.Patterns.Single(p => p.Id == placement.PatternId);
        if (state.Placements.Count(p => p.PatternId == pattern.Id) != 1)
            throw new InvalidOperationException("Graph.SharedPattern");
        if (pattern.Parts.Length >= GraphDiagnostics.MaximumSources)
            throw new InvalidOperationException(GraphReasons.ProcessingLimit);
        var report = GraphDiagnostics.Inspect(state).Single(r => r.GraphId == attachment.GraphId);
        if (report.Diagnostics.Any(d =>
                d.Reason is GraphReasons.UnsupportedScope or GraphReasons.UnsupportedDependency))
            throw new InvalidOperationException(GraphReasons.UnsupportedDependency);
        return placement;
    }

    internal Task<bool> AddGraphWavAsync(GraphEditBasis basis, string path, GraphPosition position,
        CancellationToken cancellation = default) => Command("Workflow.ImportFailed", async () =>
    {
        var (attachment, graph) = ValidateGraphBasis(basis);
        if (graph.Nodes.Length >= GraphDiagnostics.MaximumNodes)
            throw new InvalidOperationException(GraphReasons.ProcessingLimit);
        var placement = SourceDestination(_document.Current.State, attachment);
        string name = Path.GetFileNameWithoutExtension(path);
        _status = "Workflow.Importing";
        Publish();
        var prepared = await Task.Run(async () =>
        {
            await using var file = File.OpenRead(path);
            var bytes = await WavDecoder.ReadBytesAsync(file, cancellation);
            using var pcm = WavDecoder.Decode(bytes);
            return (Bytes: bytes, pcm.Frames, pcm.SampleRate, Root: SampleRootMetadata.Read(path, bytes));
        }, cancellation);
        var duration = ImportedOccurrence.Duration(prepared.Frames, prepared.SampleRate,
            _document.Current.State.Settings.Tempo);
        using var stream = new MemoryStream(prepared.Bytes, writable: false);
        using var request = ProjectMedia.BeginImport(_document, name, rootPitch: prepared.Root,
            releaseMilliseconds: 0m, ownedMediaDirectory: _mediaDirectory);
        await request.PrepareAsync(stream, cancellation);
        ValidateGraphBasis(basis);
        request.Accept((edit, media) =>
        {
            var pattern = _document.Current.State.Patterns.Single(p => p.Id == placement.PatternId);
            if (duration.Ticks > pattern.Length.Ticks) edit.SetPatternLength(pattern.Id, duration);
            var part = edit.AddPart(pattern.Id, name, media.SoundId);
            edit.AddNote(pattern.Id, part, default, duration, prepared.Root, 1m);
            var source = edit.AddGraphNode(graph.Id, GraphBuiltIns.Source, position,
                graph.Nodes.FirstOrDefault(n => n.Type == GraphBuiltIns.Source)?.Ports.FirstOrDefault()?.Layout ??
                GraphBuiltIns.Stereo);
            edit.BindGraphSource(attachment.Id, source.Id, placement.Id, part);
            edit.SetContextMix(attachment.ContextId, true);
        }, cancellation);
        _status = "Workflow.Ready";
        return true;
    });
}
