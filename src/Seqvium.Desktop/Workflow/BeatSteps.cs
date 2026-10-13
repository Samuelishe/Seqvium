// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

/// <summary>Frozen command authority, not persisted music or a second step model.</summary>
internal sealed record BeatEditBasis(Guid Lifecycle, long Generation, Id<GraphAttachment> Attachment, Id<Pattern> Pattern);

internal static class BeatSteps
{
    internal const int Count = 16;
    internal const int MaximumBars = 32;
    internal static int StepCount(Pattern pattern) => checked((int)(pattern.Length.Ticks / StepTicks));
    internal const long StepTicks = MusicalPosition.TicksPerQuarter / 4;
    internal const long BarTicks = MusicalPosition.TicksPerQuarter * 4;
    internal static MusicalPosition Position(int step) => step is >= 0 and < Count * MaximumBars
        ? new(step * StepTicks) : throw new ArgumentOutOfRangeException(nameof(step));
    // Ordinary finite gates, never an infinite one-shot tail. EOF may finish earlier.
    internal static MusicalDuration Duration(int step, long length = BarTicks) => new(Math.Min(MusicalPosition.TicksPerQuarter,
        length - Position(step).Ticks));
    internal static int? Step(NoteEvent note) => note.Position.Ticks % StepTicks == 0 &&
        note.Position.Ticks < BarTicks * MaximumBars ? (int)(note.Position.Ticks / StepTicks) : null;

    internal static string? PatternRefusal(ProjectState state, Pattern? pattern)
    {
        if (state.Settings.Meter is not { Numerator: 4, Denominator: 4 }) return "Beat.MeterUnsupported";
        if (pattern is null) return "Beat.NoTarget";
        if (pattern.Length.Ticks % BarTicks != 0 || pattern.Length.Ticks > BarTicks * MaximumBars) return "Beat.LengthUnsupported";
        if (!pattern.AdditionalData.IsEmpty) return "Beat.NotesUnsupported";
        return null;
    }

    internal static string? PartRefusal(ProjectState state, MusicalPart part, long length = BarTicks)
    {
        var sound = state.Sounds.SingleOrDefault(s => s.Id == part.SoundId);
        if (!part.AdditionalData.IsEmpty || sound is null || sound.Algorithm != PcmSampler.Algorithm ||
            sound.Extension is not null || !sound.AdditionalData.IsEmpty || sound.ResourceIds.Length != 1 ||
            sound.Parameters.Count != 2 || !sound.Parameters.TryGetValue("rootPitch", out var root) ||
            root is < 0 or > 127 || !sound.Parameters.TryGetValue("releaseMilliseconds", out var release) ||
            release is < 0 or > 1000) return "Beat.SoundUnsupported";
        if (part.Notes.Any(note => Step(note) is not { } step || note.Position.Ticks >= length || note.Pitch != root || note.Intensity != 1m ||
                note.Duration != Duration(step, length) || !note.AdditionalData.IsEmpty) ||
            part.Notes.GroupBy(note => note.Position).Any(group => group.Count() != 1)) return "Beat.NotesUnsupported";
        return null;
    }

    internal static (Pattern? Pattern, BeatEditBasis? Basis) Resolve(WorkflowState observation)
    {
        var state = observation.Snapshot.State;
        var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == observation.Selected);
        var placement = state.Placements.SingleOrDefault(p => p.ItemContextId == attachment?.ContextId);
        var pattern = state.Patterns.SingleOrDefault(p => p.Id == placement?.PatternId);
        return (pattern, pattern is null || attachment is null ? null : new(observation.Document.LifecycleId,
            observation.Generation, attachment.Id, pattern.Id));
    }
}
