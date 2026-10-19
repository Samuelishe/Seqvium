// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

/// <summary>Musical projections and group transforms, with no independent editable music.</summary>
internal static class PianoNotes
{
    internal static string Name(decimal pitch)
    {
        int midi = (int)decimal.Floor(pitch);
        string name = new[] { "C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B" }[midi % 12] +
                      (midi / 12 - 1);
        return pitch == midi ? name : name + $" +{(pitch - midi) * 100:0.#}¢";
    }

    internal static string? Refusal(ProjectState state, Pattern? pattern, MusicalPart? part)
    {
        if (pattern is null || part is null) return "Piano.NoTarget";
        if (!pattern.AdditionalData.IsEmpty || !part.AdditionalData.IsEmpty ||
            part.Notes.Any(n => !n.AdditionalData.IsEmpty))
            return "Beat.NotesUnsupported";
        return SoundRefusal(state.Sounds.SingleOrDefault(s => s.Id == part.SoundId));
    }

    internal static string? SoundRefusal(SoundDefinition? sound) => sound is null || !BuiltInSound.Supports(sound)
        ? "Beat.SoundUnsupported"
        : null;

    internal static long Snap(long ticks, long unit) => unit <= 0
        ? ticks
        : checked((long)Math.Round((decimal)ticks / unit, MidpointRounding.AwayFromZero) * unit);

    internal static NoteEvent[] Quantize(IReadOnlyList<NoteEvent> notes, IReadOnlySet<Id<NoteEvent>> selection,
        long unit, long length)
    {
        if (unit <= 1) throw new InvalidOperationException("Piano.QuantizeSnapOff");
        var result = notes.Select(n => selection.Contains(n.Id)
            ? n with { Position = new(Snap(n.Position.Ticks, unit)) }
            : n).ToArray();
        // Subtraction avoids overflow at the musical time limit. Refuse the whole group.
        if (result.Any(n => selection.Contains(n.Id) && n.Position.Ticks > length - n.Duration.Ticks))
            throw new InvalidOperationException("Piano.QuantizeBoundary");
        return result;
    }

    /// <summary>Bounded tick/velocity offsets; selected equal onsets share one clamped displacement.</summary>
    internal static NoteEvent[] Humanize(IReadOnlyList<NoteEvent> notes, IReadOnlySet<Id<NoteEvent>> selection,
        int timing, int velocity, long length, Random random)
    {
        if (timing is < 0 or > 100 || velocity is < 0 or > 100)
            throw new InvalidOperationException("Piano.HumanizeRange");
        if (!selection.IsSubsetOf(notes.Select(n => n.Id).ToHashSet()))
            throw new InvalidOperationException("Beat.Stale");
        if (timing == 0 && velocity == 0 || selection.Count == 0) return notes.ToArray();

        long maximum = MusicalPosition.TicksPerQuarter / 32 * timing / 100;
        var offsets = new Dictionary<long, long>();
        if (timing > 0)
            foreach (var chord in notes.Where(n => selection.Contains(n.Id)).GroupBy(n => n.Position.Ticks))
            {
                // The longest selected gate limits the entire chord. Never shorten or drop a note.
                long upper = Math.Min(maximum, length - chord.Key - chord.Max(n => n.Duration.Ticks));
                long lower = Math.Max(-maximum, -chord.Key);
                offsets.Add(chord.Key, Math.Clamp(random.NextInt64(-maximum, maximum + 1), lower, upper));
            }

        return notes.Select(n => !selection.Contains(n.Id) ? n : n with
        {
            Position = new(n.Position.Ticks + offsets.GetValueOrDefault(n.Position.Ticks)),
            Intensity = velocity == 0 ? n.Intensity :
                Math.Clamp(n.Intensity + random.Next(-velocity * 100, velocity * 100 + 1) / 100000m, 0, 1)
        }).ToArray();
    }

    /// <summary>Relative selected-note edits. Validate every result before the caller accepts the group.</summary>
    internal static NoteEvent[] Transform(IReadOnlyList<NoteEvent> notes, IReadOnlySet<Id<NoteEvent>> selection,
        int transpose, decimal durationPercent, decimal velocityPercent, long length)
    {
        if (transpose is < -24 or > 24 || durationPercent is < 50 or > 200 || velocityPercent is < 50 or > 200)
            throw new InvalidOperationException("Piano.TransformRange");
        if (!selection.IsSubsetOf(notes.Select(n => n.Id).ToHashSet()))
            throw new InvalidOperationException("Beat.Stale");
        return notes.Select(n =>
        {
            if (!selection.Contains(n.Id)) return n;
            decimal pitch = n.Pitch + transpose;
            if (pitch is < 0 or > 127) throw new InvalidOperationException("Piano.TransformPitchBoundary");
            // Decimal retains every Int64 tick; compare before conversion/addition to avoid overflow.
            decimal duration = Math.Max(1, decimal.Round(n.Duration.Ticks * (durationPercent / 100),
                0, MidpointRounding.AwayFromZero));
            if (duration > length - n.Position.Ticks)
                throw new InvalidOperationException("Piano.TransformTimeBoundary");
            return n with { Pitch = pitch, Duration = new((long)duration),
                Intensity = Math.Clamp(n.Intensity * (velocityPercent / 100), 0, 1) };
        }).ToArray();
    }

    internal static NoteEvent[] Move(IReadOnlyList<NoteEvent> notes, long ticks, decimal pitch, long length,
        bool copy = false)
    {
        if (notes.Count == 0) return [];
        ticks = Math.Clamp(ticks, -notes.Min(n => n.Position.Ticks),
            length - notes.Max(n => n.Position.Ticks + n.Duration.Ticks));
        pitch = Math.Clamp(pitch, -notes.Min(n => n.Pitch), 127 - notes.Max(n => n.Pitch));
        return notes.Select(n => n with
        {
            Id = copy ? Id<NoteEvent>.New() : n.Id,
            Position = new(n.Position.Ticks + ticks), Pitch = n.Pitch + pitch
        }).ToArray();
    }

    internal static NoteEvent[] Resize(IReadOnlyList<NoteEvent> notes, long delta, long length, long minimum)
    {
        if (notes.Count == 0) return [];
        minimum = Math.Min(minimum, notes.Min(n => n.Duration.Ticks));
        delta = Math.Clamp(delta, minimum - notes.Min(n => n.Duration.Ticks),
            notes.Min(n => length - n.Position.Ticks - n.Duration.Ticks));
        return notes.Select(n => n with { Duration = new(n.Duration.Ticks + delta) }).ToArray();
    }
}
