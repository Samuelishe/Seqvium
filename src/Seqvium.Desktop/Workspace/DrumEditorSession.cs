// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed record DrumProposal(
    NodeEditorTarget Target,
    long Generation,
    Id<SoundDefinition> SoundId,
    ImmutableDictionary<string, decimal> Before,
    ImmutableDictionary<string, decimal> After);

/// <summary>A source editor addresses shared sound intent through an explicit bound node, never graph selection.</summary>
internal sealed class DrumEditorSession
{
    private WorkflowState? _observation;
    public NodeEditorTarget? Target { get; private set; }
    public DrumProposal? Draft { get; private set; }
    public bool ChangedElsewhere { get; private set; }

    public SoundDefinition? Sound => _observation is { IsClosed: false } state &&
                                     state.Document.LifecycleId == Target?.LifecycleId
        ? Resolve(state.Snapshot.State, Target!)
        : null;

    public bool CanEdit => _observation is { Busy: false, IsClosed: false } && Sound is { } sound &&
                           DrumVoice.Supports(sound);

    public ImmutableDictionary<string, decimal> Values => Draft?.After ?? Sound?.Parameters ?? DrumVoice.Defaults;

    public static SoundDefinition? Resolve(ProjectState state, NodeEditorTarget target)
    {
        var attachment =
            state.GraphAttachments.SingleOrDefault(a =>
                a.Id == target.AttachmentId && a.GraphId == target.GraphDefinitionId);
        var node = state.Graphs.SingleOrDefault(g => g.Id == attachment?.GraphId)?.Nodes
            .SingleOrDefault(n => n.Id == target.NodeId);
        if (node is not { Type: GraphBuiltIns.Source } || !GraphConnectionRules.Supported(node)) return null;
        var binding = attachment!.Sources.SingleOrDefault(s => s.NodeId == node.Id);
        var placement =
            state.Placements.SingleOrDefault(p =>
                p.Id == binding?.PlacementId && p.ItemContextId == attachment.ContextId);
        var part = state.Patterns.SingleOrDefault(p => p.Id == placement?.PatternId)?.Parts
            .SingleOrDefault(p => p.Id == binding?.PartId);
        return state.Sounds.SingleOrDefault(s => s.Id == part?.SoundId);
    }

    public bool Observe(WorkflowState observation, NodeEditorTarget? target)
    {
        bool changed = Target != target || _observation?.Document.LifecycleId != observation.Document.LifecycleId ||
                       _observation.Generation != observation.Generation;
        if (changed)
        {
            ChangedElsewhere = Draft is not null && Target == target;
            Draft = null;
        }

        _observation = observation;
        Target = observation.IsClosed || target?.LifecycleId != observation.Document.LifecycleId ? null : target;
        return changed;
    }

    public bool Begin()
    {
        if (!CanEdit || Target is null || _observation is null || Sound is not { } sound) return false;
        ChangedElsewhere = false;
        Draft ??= new(Target, _observation.Generation, sound.Id, sound.Parameters, sound.Parameters);
        return true;
    }

    public void Set(string key, decimal value)
    {
        var parameter = DrumVoice.Parameters.Single(p => p.Key == key);
        if (Draft is { } draft && value >= parameter.Minimum && value <= parameter.Maximum)
            Draft = draft with { After = draft.After.SetItem(key, value) };
    }

    public DrumProposal? Complete()
    {
        var proposal = Draft;
        Draft = null;
        return CanEdit && proposal is not null && proposal.Target == Target &&
               proposal.Generation == _observation?.Generation &&
               !proposal.Before.OrderBy(p => p.Key).SequenceEqual(proposal.After.OrderBy(p => p.Key))
            ? proposal
            : null;
    }

    public void Cancel()
    {
        Draft = null;
        ChangedElsewhere = false;
    }
}
