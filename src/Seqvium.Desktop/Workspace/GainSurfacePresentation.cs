// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal readonly record struct GainSurfaceVisibility(bool HideToolbarGain, bool HideGraphGain);

/// <summary>Visual priority only for exactly the same canonical parameter; different scopes stay available.</summary>
internal static class GainSurfacePresentation
{
    public static GainSurfaceVisibility Resolve(WorkflowState state, GraphEditorSession graphSession,
        NodeEditorTarget? nodeTarget, WorkspaceState workspace)
    {
        var canonical = state.Snapshot.State;
        NodeEditorTarget? Key(Id<GraphAttachment>? attachmentId, Id<GraphNode>? nodeId)
        {
            var attachment = canonical.GraphAttachments.SingleOrDefault(a => a.Id == attachmentId);
            var graph = canonical.Graphs.SingleOrDefault(g => g.Id == attachment?.GraphId);
            return attachment is not null && graph is not null && graph.Nodes.Any(n => n.Id == nodeId)
                ? new(state.Document.LifecycleId, attachment.Id, graph.Id, nodeId!.Value) : null;
        }
        bool Detailed(NodeEditorTarget? key)
        {
            if (key is null || key != nodeTarget || workspace.Get(WorkspaceState.NodeEditorId).Visibility != PaneVisibility.Visible)
                return false;
            var node = canonical.Graphs.SingleOrDefault(g => g.Id == key.GraphDefinitionId)?.Nodes.SingleOrDefault(n => n.Id == key.NodeId);
            return NodeEditorSession.SupportsGain(node);
        }
        var graphKey = graphSession.Basis?.Lifecycle == state.Document.LifecycleId
            ? Key(graphSession.Target, graphSession.SelectedNode) : null;
        var selectedAttachment = canonical.GraphAttachments.SingleOrDefault(a => a.Id == state.Selected);
        var selectedGraph = canonical.Graphs.SingleOrDefault(g => g.Id == selectedAttachment?.GraphId);
        var gains = selectedGraph?.Nodes.Where(n => n.Type == GraphBuiltIns.Gain && n.StateVersion == 1 && n.Extension is null).ToArray() ?? [];
        var toolbarKey = state.Gain.HasValue && gains.Length == 1 ? Key(state.Selected, gains[0].Id) : null;
        bool graphEditsToolbar = toolbarKey is not null && graphKey == toolbarKey &&
            workspace.Get(WorkspaceState.GraphId).Visibility == PaneVisibility.Visible && GraphConnectionRules.Supported(gains[0]);
        return new(Detailed(toolbarKey) || graphEditsToolbar, Detailed(graphKey));
    }
}
