// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

public sealed partial class ProjectEdit
{
    public static bool CanDuplicatePart(ProjectState state, Id<Pattern> patternId, Id<MusicalPart> partId)
    {
        var pattern = state.Patterns.SingleOrDefault(p => p.Id == patternId);
        var part = pattern?.Parts.SingleOrDefault(p => p.Id == partId);
        var sound = state.Sounds.SingleOrDefault(s => s.Id == part?.SoundId);
        return pattern is not null && part is not null && sound is not null &&
               pattern.AdditionalData.IsEmpty && part.AdditionalData.IsEmpty &&
               part.Notes.All(n => n.AdditionalData.IsEmpty) && BuiltInSound.Supports(sound) &&
               state.Placements.Where(p => p.PatternId == patternId).All(p => p.AdditionalData.IsEmpty &&
                                                                              p.PartRelationships
                                                                                  .Where(r => r.PartId == partId)
                                                                                  .All(r => r.AdditionalData
                                                                                      .IsEmpty)) &&
               (state.Mixer is null || MixerValidation.Supported(state.Mixer));
    }

    /// <summary>Reorders presentation only. Graph bindings and musical time retain their identities.</summary>
    public void MovePart(Id<Pattern> patternId, Id<MusicalPart> partId, int index)
    {
        CheckActive();
        ChangePattern(patternId, pattern =>
        {
            var part = pattern.Parts.Single(p => p.Id == partId);
            if (index < 0 || index >= pattern.Parts.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return pattern with { Parts = pattern.Parts.Remove(part).Insert(index, part) };
        });
    }

    /// <summary>Copies supported musical content and sound intent; immutable media and explicit routes stay shared.
    /// The caller must bind the new Part in its graph in this same transaction.</summary>
    public Id<MusicalPart> DuplicatePartIndependent(Id<Pattern> patternId, Id<MusicalPart> partId, string name,
        int index)
    {
        CheckActive();
        var pattern = State.Patterns.Single(p => p.Id == patternId);
        var part = pattern.Parts.Single(p => p.Id == partId);
        var sound = State.Sounds.Single(s => s.Id == part.SoundId);
        ProjectValidation.Require(CanDuplicatePart(State, patternId, partId),
            "Unsupported channel content cannot be duplicated safely.");
        ProjectValidation.Require(State.Mixer is null || MixerValidation.Supported(State.Mixer),
            "Unknown Mixer state cannot be duplicated safely.");
        if (index < 0 || index > pattern.Parts.Length) throw new ArgumentOutOfRangeException(nameof(index));
        var independent = sound with
        {
            Id = Id<SoundDefinition>.New(), Name = name,
            SamplerZones = [.. sound.SamplerZones.Select(z => z with { Id = Id<SamplerZone>.New() })]
        };
        var copy = part with
        {
            Id = Id<MusicalPart>.New(), Name = name, SoundId = independent.Id,
            Notes = [.. part.Notes.Select(n => n with { Id = Id<NoteEvent>.New() })]
        };
        State = State with { Sounds = State.Sounds.Add(independent) };
        ChangePattern(patternId, p => p with { Parts = p.Parts.Insert(index, copy) });
        foreach (var placement in State.Placements.Where(p => p.PatternId == patternId))
        {
            var relationship = placement.PartRelationships.SingleOrDefault(p => p.PartId == partId);
            if (relationship is null) continue;
            ProjectValidation.Require(relationship.AdditionalData.IsEmpty,
                "Unknown Part relationship cannot be duplicated safely.");
            ChangePlacement(placement.Id, p => p with
            {
                PartRelationships = p.PartRelationships.Add(relationship with
                {
                    PartId = copy.Id,
                    SharedPerformanceKey = relationship.SharedPerformanceKey.HasValue ? Guid.NewGuid() : null
                })
            });
        }

        if (State.Mixer is { } mixer)
            SetMixer(mixer with
            {
                Channels =
                [
                    .. mixer.Channels.Select(c => c.SoundIds.Contains(sound.Id)
                        ? c with { SoundIds = c.SoundIds.Add(independent.Id) }
                        : c)
                ]
            });
        return copy.Id;
    }
}
