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
        if (report.Diagnostics.Any(d => d.Reason is GraphReasons.UnsupportedScope or GraphReasons.UnsupportedDependency))
            throw new InvalidOperationException(GraphReasons.UnsupportedDependency);
        return placement;
    }

    internal Task<bool> AddGraphWavAsync(GraphEditBasis basis, string path, GraphPosition position,
        CancellationToken cancellation = default) => Command("Workflow.ImportFailed", async () =>
    {
        var (attachment, graph) = ValidateGraphBasis(basis);
        if (graph.Nodes.Length >= GraphDiagnostics.MaximumNodes) throw new InvalidOperationException(GraphReasons.ProcessingLimit);
        var placement = SourceDestination(_document.Current.State, attachment);
        string name = Path.GetFileNameWithoutExtension(path);
        using var request = ProjectMedia.BeginImport(_document, name, releaseMilliseconds: 0m, ownedMediaDirectory: _mediaDirectory);
        _status = "Workflow.Importing"; Publish();
        var prepared = await Task.Run(async () =>
        {
            await using var file = File.OpenRead(path);
            var bytes = await WavDecoder.ReadBytesAsync(file, cancellation);
            using var pcm = WavDecoder.Decode(bytes);
            return (Bytes: bytes, pcm.Frames, pcm.SampleRate);
        }, cancellation);
        var duration = ImportedOccurrence.Duration(prepared.Frames, prepared.SampleRate, _document.Current.State.Settings.Tempo);
        using var stream = new MemoryStream(prepared.Bytes, writable: false);
        await request.PrepareAsync(stream, cancellation);
        ValidateGraphBasis(basis);
        request.Accept((edit, media) =>
        {
            var pattern = _document.Current.State.Patterns.Single(p => p.Id == placement.PatternId);
            if (duration.Ticks > pattern.Length.Ticks) edit.SetPatternLength(pattern.Id, duration);
            var part = edit.AddPart(pattern.Id, name, media.SoundId);
            edit.AddNote(pattern.Id, part, default, duration, 60m, 1m);
            var source = edit.AddGraphNode(graph.Id, GraphBuiltIns.Source, position,
                graph.Nodes.FirstOrDefault(n => n.Type == GraphBuiltIns.Source)?.Ports.FirstOrDefault()?.Layout ?? GraphBuiltIns.Stereo);
            edit.BindGraphSource(attachment.Id, source.Id, placement.Id, part);
            edit.SetContextMix(attachment.ContextId, true);
        }, cancellation);
        _status = "Workflow.Ready";
        return true;
    });
}
