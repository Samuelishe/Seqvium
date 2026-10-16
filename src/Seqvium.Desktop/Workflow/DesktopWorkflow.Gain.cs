// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private sealed record GainGesture(Guid Id, GraphEditBasis Basis, Id<GraphNode> Node, decimal Before);

    private GainGesture? _gainPreview;

    internal Task<bool> BeginGainPreviewAsync(Guid id, GraphEditBasis basis, Id<GraphNode> node) => _owner.Run(() =>
    {
        if (_closed || _busy || basis.Lifecycle != _document.LifecycleId || basis.Generation != _document.Generation ||
            basis.Attachment != _selected) return Task.FromResult(false);
        var (_, graph) = ValidateGraphBasis(basis);
        var target = graph.Nodes.SingleOrDefault(n => n.Id == node);
        if (target is null || !NodeEditorSession.SupportsGain(target) ||
            NodeEditorSession.ReadAmplitude(target) is not (>= 0 and <= 1)) return Task.FromResult(false);
        CancelGainPreview();
        _gainPreview = new(id, basis, node, NodeEditorSession.ReadAmplitude(target)!.Value);
        return Task.FromResult(true);
    });

    internal Task<bool> PreviewGainAsync(Guid id, decimal amplitude) => _owner.Run(() =>
    {
        if (_gainPreview is not { } gesture || gesture.Id != id || _closed ||
            gesture.Basis.Lifecycle != _document.LifecycleId || gesture.Basis.Generation != _document.Generation ||
            gesture.Basis.Attachment != _selected || amplitude is < 0 or > 1) return Task.FromResult(false);
        return Task.FromResult(_coordinator?.PreviewGain(gesture.Basis.Generation, gesture.Basis.Attachment,
            gesture.Node, amplitude) == true);
    });

    internal Task<bool> CancelGainPreviewAsync(Guid id) => _owner.Run(() =>
    {
        if (_gainPreview?.Id != id) return Task.FromResult(false);
        CancelGainPreview();
        return Task.FromResult(true);
    });

    internal Task<bool> CompleteGainPreviewAsync(Guid id, decimal amplitude) => Command("Workflow.EditFailed", false,
        () =>
        {
            if (_gainPreview is not { } gesture || gesture.Id != id) return Task.FromResult(false);
            CancelGainPreview();
            var (attachment, graph) = ValidateGraphBasis(gesture.Basis);
            var node = graph.Nodes.Single(n => n.Id == gesture.Node);
            if (NodeEditorSession.ReadAmplitude(node) != gesture.Before || amplitude is < 0 or > 1)
                return Task.FromResult(false);
            return Task.FromResult(_document.Edit("Change graph Gain", edit =>
                edit.SetGraphGain(attachment.GraphId, gesture.Node, amplitude)));
        });

    private void CancelGainPreview()
    {
        _gainPreview = null;
        _coordinator?.CancelGainPreview();
    }
}
