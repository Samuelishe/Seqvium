// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Prepared hat lifetimes across channels/clips. Closed wins simultaneous hits; other ties use event identity.
/// A three-ms fade ends inside the next onset. No mutable sound lookup enters the callback.</summary>
internal static class DrumChoke
{
    internal static void Prepare(ImmutableArray<PreparedNote>.Builder notes, ImmutableArray<PreparedEvent> events,
        Tempo tempo, int rate, LoopClock? loop = null)
    {
        var hats = events.Where(e => e.Kind == ExecutionEventKind.NoteOn && notes[e.NoteIndex].ChokeGroup != 0)
            .OrderBy(e => e.Frame).ThenBy(e => notes[e.NoteIndex].ChokePriority).ToArray();
        for (int i = 0; i < hats.Length; i++)
        {
            var current = hats[i];
            var note = notes[current.NoteIndex];
            long nextTicks = i + 1 < hats.Length ? hats[i + 1].Position.Ticks :
                loop is not null ? hats[0].Position.Ticks + loop.LengthTicks : long.MaxValue;
            long frames = i + 1 == hats.Length
                ? long.MaxValue
                : Math.Max(0, ConstantTempoConversion.ToFrames(new(nextTicks), tempo, rate) - note.StartFrame);
            notes[current.NoteIndex] = note with
            {
                ChokeFrames = frames, ChokeTicks = nextTicks, ChokeStartTicks = current.Position.Ticks
            };
        }
    }
}
