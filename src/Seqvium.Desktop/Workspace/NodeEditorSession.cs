// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed record NodeEditorTarget(Guid LifecycleId, Id<GraphAttachment> AttachmentId,
    Id<GraphDefinition> GraphDefinitionId, Id<GraphNode> NodeId);
internal sealed record NodeGainProposal(NodeEditorTarget Target, long Generation, decimal? Before, decimal After);

/// <summary>Explicit logical target and one transient proposal over coherent canonical observations.</summary>
internal sealed class NodeEditorSession : IDisposable
{
    private WorkflowState? _observation;
    private bool _disposed;
    public NodeEditorTarget? Target { get; private set; }
    public NodeGainProposal? Draft { get; private set; }
    public bool HasDraft { get; private set; }
    public bool ChangedElsewhere { get; private set; }
    public ProjectState? State => _observation?.Snapshot.State;
    public GraphAttachment? Attachment => Target is { } target && _observation is { IsClosed: false } state &&
        state.Document.LifecycleId == target.LifecycleId
        ? State?.GraphAttachments.SingleOrDefault(a => a.Id == target.AttachmentId && a.GraphId == target.GraphDefinitionId) : null;
    public GraphDefinition? Graph => Attachment is { } attachment
        ? State?.Graphs.SingleOrDefault(g => g.Id == attachment.GraphId) : null;
    public GraphNode? Node => Graph?.Nodes.SingleOrDefault(n => n.Id == Target?.NodeId);
    public static bool SupportsGain(GraphNode? node) => node is { Type: GraphBuiltIns.Gain } &&
        GraphConnectionRules.Supported(node) && node.Parameters.Keys.All(k => k == GraphBuiltIns.GainAmplitude);
    public bool CanEditGain => !_disposed && _observation is { Busy: false, IsClosed: false } && SupportsGain(Node);
    public decimal? Amplitude => ReadAmplitude(Node);
    public static decimal? ReadAmplitude(GraphNode? node) => node is not null &&
        node.Parameters.TryGetValue(GraphBuiltIns.GainAmplitude, out var value) &&
        value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var amplitude) ? amplitude : null;
    public static double? Decibels(decimal amplitude) => amplitude is > 0 and <= 1
        ? 20 * Math.Log10((double)amplitude) : null;

    public bool Observe(WorkflowState state)
    {
        if (_disposed) return false;
        bool changed = _observation?.Document.LifecycleId != state.Document.LifecycleId ||
            _observation.Generation != state.Generation;
        if (changed)
        {
            ChangedElsewhere = HasDraft;
            CancelDraft();
        }
        _observation = state;
        if (state.IsClosed || Target is { } target && target.LifecycleId != state.Document.LifecycleId)
        { Target = null; ChangedElsewhere = false; }
        return changed;
    }
    public bool Open(WorkflowState state, NodeEditorTarget target)
    {
        if (_disposed) return false;
        Observe(state);
        if (state.IsClosed || state.Busy || target.LifecycleId != state.Document.LifecycleId) return false;
        var attachment = state.Snapshot.State.GraphAttachments.SingleOrDefault(a => a.Id == target.AttachmentId);
        if (attachment?.GraphId != target.GraphDefinitionId || !state.Snapshot.State.Graphs.Any(g =>
            g.Id == target.GraphDefinitionId && g.Nodes.Any(n => n.Id == target.NodeId))) return false;
        if (Target != target) { CancelDraft(); ChangedElsewhere = false; Target = target; }
        return true;
    }
    public bool BeginDraft()
    {
        if (!CanEditGain || Target is null || _observation is null) return false;
        if (HasDraft) return true;
        ChangedElsewhere = false; HasDraft = true;
        Draft = new(Target, _observation.Generation, Amplitude, Amplitude is >= 0 and <= 1 ? Amplitude.Value : 1m);
        return true;
    }
    public void UpdateDraft(decimal value)
    {
        if (HasDraft && Draft is { } draft && value is >= 0 and <= 1) Draft = draft with { After = value };
    }
    public NodeGainProposal? CompleteDraft()
    {
        var draft = Draft;
        CancelDraft();
        return CanEditGain && draft is not null && draft.Target == Target &&
            draft.Generation == _observation?.Generation && draft.Before != draft.After ? draft : null;
    }
    public void CancelDraft() { Draft = null; HasDraft = false; }
    public void DismissChangeNotice() => ChangedElsewhere = false;
    public void Dispose() { CancelDraft(); _disposed = true; Target = null; _observation = null; }
}

/// <summary>Avalonia supplies platform click count; only completed, unmoved body clicks can open.</summary>
internal sealed class NodeBodyClick
{
    private NodeEditorTarget? _first, _pressed;
    private long _firstGeneration, _pressedGeneration;
    private bool _second, _moved;
    public void Press(NodeEditorTarget? target, int clickCount, long generation = 0)
    {
        _second = clickCount == 2 && target is not null && target == _first && generation == _firstGeneration;
        _pressed = target; _pressedGeneration = generation; _moved = false;
        if (clickCount != 2) _first = null;
    }
    public void Move() => _moved = true;
    public NodeEditorTarget? Release(NodeEditorTarget? target, long generation = 0)
    {
        bool click = !_moved && target is not null && target == _pressed && generation == _pressedGeneration;
        var open = click && _second ? target : null;
        _first = click && !_second ? target : null;
        _firstGeneration = generation;
        _pressed = null; _second = false;
        return open;
    }
    public void Cancel() { _first = null; _pressed = null; _second = false; _moved = false; }
}
