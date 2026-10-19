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
