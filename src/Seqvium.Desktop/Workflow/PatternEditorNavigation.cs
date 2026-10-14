// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

/// <summary>Session-only editor intent for shared Patterns, never project or workspace geometry.</summary>
internal sealed class PatternEditorNavigation
{
    private readonly Dictionary<Id<Pattern>, (string Pane, Id<MusicalPart>? Part)> _remembered = [];
    private Guid _lifecycle;

    internal void Remember(WorkflowState state, string pane, Id<MusicalPart>? part = null)
    {
        Adopt(state);
        if (BeatSteps.Resolve(state).Pattern is { } pattern)
            _remembered[pattern.Id] = (pane, part);
    }

    internal (string Pane, Id<MusicalPart>? Part) Resolve(WorkflowState state)
    {
        Adopt(state);
        var pattern = BeatSteps.Resolve(state).Pattern;
        if (pattern is null) return (WorkspaceState.BeatId, null);
        if (_remembered.TryGetValue(pattern.Id, out var last) &&
            (last.Pane == WorkspaceState.BeatId || pattern.Parts.Any(p => p.Id == last.Part &&
                PianoNotes.Refusal(state.Snapshot.State, pattern, p) is null))) return last;
        var part = PreferredPart(state.Snapshot.State, pattern);
        return part is not null && IsMelodic(state.Snapshot.State, pattern, part)
            ? (WorkspaceState.PianoId, part.Id) : (WorkspaceState.BeatId, null);
    }

    internal static MusicalPart? PreferredPart(ProjectState state, Pattern pattern) => pattern.Parts
        .Where(p => PianoNotes.Refusal(state, pattern, p) is null)
        .OrderByDescending(p => IsMelodic(state, pattern, p))
        .ThenByDescending(p => !p.Notes.IsEmpty)
        .FirstOrDefault();

    private static bool IsMelodic(ProjectState state, Pattern pattern, MusicalPart part)
    {
        if (part.Notes.IsEmpty) return false;
        var sound = state.Sounds.Single(s => s.Id == part.SoundId);
        decimal root = sound.Parameters.GetValueOrDefault("rootPitch", 60);
        return part.Notes.Any(n => n.Pitch != root) ||
               (!DrumVoice.Supports(sound) && BeatSteps.PartRefusal(state, part, pattern.Length.Ticks) is not null);
    }

    private void Adopt(WorkflowState state)
    {
        if (_lifecycle == state.Document.LifecycleId) return;
        _lifecycle = state.Document.LifecycleId;
        _remembered.Clear();
    }
}
