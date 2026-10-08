// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>An isolated logical edit. Intermediate states are never published or placed in document history.</summary>
public sealed class ProjectEdit
{
    private bool _sealed;
    internal ProjectState State { get; private set; }
    internal ProjectEdit(ProjectState state) => State = state;
    internal void Seal() => _sealed = true;
    private void CheckActive() { if (_sealed) throw new InvalidOperationException("Transaction has ended."); }

    public void RenameDocument(string? name)
    {
        CheckActive();
        if (name != State.Name) State = State with { Name = name };
    }

    public void SetSettings(ProjectSettings settings)
    {
        CheckActive();
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settings.Meter);
        var existing = State.Settings;
        if (settings.Tempo == existing.Tempo && settings.Meter.Numerator == existing.Meter.Numerator &&
            settings.Meter.Denominator == existing.Meter.Denominator) return;
        // Update understood values without replacing optional future data on these same objects.
        State = State with { Settings = existing with
        {
            Tempo = settings.Tempo,
            Meter = existing.Meter with { Numerator = settings.Meter.Numerator, Denominator = settings.Meter.Denominator }
        } };
    }

    public Id<ResourceDescriptor> AddResource(string name, string? managedLocator = null, string? origin = null)
    {
        CheckActive();
        var resource = new ResourceDescriptor(Id<ResourceDescriptor>.New(), name, managedLocator, origin);
        State = State with { Resources = State.Resources.Add(resource) };
        return resource.Id;
    }

    public Id<SoundDefinition> AddSound(string name, string algorithm,
        ImmutableArray<Id<ResourceDescriptor>> resources = default, ExtensionState? extension = null)
    {
        CheckActive();
        var sound = new SoundDefinition(Id<SoundDefinition>.New(), name, algorithm,
            ImmutableDictionary<string, decimal>.Empty, resources.IsDefault ? [] : resources, extension);
        State = State with { Sounds = State.Sounds.Add(sound) };
        return sound.Id;
    }

    public void SetSoundParameters(Id<SoundDefinition> soundId, ImmutableDictionary<string, decimal> parameters)
    {
        CheckActive();
        State = State with { Sounds = Replace(State.Sounds, item => item.Id == soundId, sound =>
            sound.Parameters.Count == parameters.Count && sound.Parameters.All(pair => parameters.TryGetValue(pair.Key, out var value) && value == pair.Value)
                ? sound : sound with { Parameters = parameters }) };
    }

    public void ConfigurePcmSampler(Id<SoundDefinition> soundId, Id<ResourceDescriptor> resourceId,
        decimal rootPitch, decimal releaseMilliseconds)
    {
        CheckActive();
        PcmSampler.ValidateConfiguration(rootPitch, releaseMilliseconds);
        State = State with { Sounds = Replace(State.Sounds, item => item.Id == soundId, sound =>
        {
            ProjectValidation.Require(sound.Algorithm == PcmSampler.Algorithm && sound.Extension is null,
                "Only a first-party PCM sampler can accept this configuration.");
            return sound with { ResourceIds = [resourceId], Parameters = sound.Parameters
                .SetItem("rootPitch", rootPitch).SetItem("releaseMilliseconds", releaseMilliseconds) };
        }) };
    }

    public Id<InstrumentGroup> AddGroup(string name, params Id<SoundDefinition>[] soundIds)
    {
        CheckActive();
        var group = new InstrumentGroup(Id<InstrumentGroup>.New(), name, [.. soundIds]);
        State = State with { Groups = State.Groups.Add(group) };
        return group.Id;
    }

    public Id<Pattern> AddPattern(string name, MusicalDuration length)
    {
        CheckActive();
        var pattern = new Pattern(Id<Pattern>.New(), name, length, []);
        State = State with { Patterns = State.Patterns.Add(pattern) };
        return pattern.Id;
    }

    public Id<MusicalPart> AddPart(Id<Pattern> patternId, string name, Id<SoundDefinition> soundId)
    {
        CheckActive();
        var part = new MusicalPart(Id<MusicalPart>.New(), name, soundId, []);
        ChangePattern(patternId, pattern => pattern with { Parts = pattern.Parts.Add(part) });
        return part.Id;
    }

    public Id<NoteEvent> AddNote(Id<Pattern> patternId, Id<MusicalPart> partId, MusicalPosition position,
        MusicalDuration duration, decimal pitch, decimal intensity)
    {
        CheckActive();
        var note = new NoteEvent(Id<NoteEvent>.New(), position, duration, pitch, intensity);
        ChangePart(patternId, partId, part => part with { Notes = part.Notes.Add(note) });
        return note.Id;
    }

    public void UpdateNote(Id<Pattern> patternId, Id<MusicalPart> partId, Id<NoteEvent> noteId,
        MusicalPosition position, MusicalDuration duration, decimal pitch, decimal intensity)
    {
        CheckActive();
        ChangePart(patternId, partId, part => part with { Notes = Replace(part.Notes, item => item.Id == noteId,
            note => note with { Position = position, Duration = duration, Pitch = pitch, Intensity = intensity }) });
    }

    public void DeleteNote(Id<Pattern> patternId, Id<MusicalPart> partId, Id<NoteEvent> noteId)
    {
        CheckActive();
        ChangePart(patternId, partId, part => part with { Notes = Remove(part.Notes, item => item.Id == noteId) });
    }

    public Id<PatternPlacement> AddPlacement(Id<Pattern> patternId, MusicalPosition position)
    {
        CheckActive();
        var placement = new PatternPlacement(Id<PatternPlacement>.New(), patternId, position, null, null, null, []);
        State = State with { Placements = State.Placements.Add(placement) };
        return placement.Id;
    }

    public void MovePlacement(Id<PatternPlacement> placementId, MusicalPosition position)
    {
        CheckActive();
        ChangePlacement(placementId, placement => placement with { Position = position });
    }

    public Id<RouteIntent> AddRoute(string name)
    {
        CheckActive();
        var route = new RouteIntent(Id<RouteIntent>.New(), name);
        State = State with { Routes = State.Routes.Add(route) };
        return route.Id;
    }

    public Id<ProcessingContext> AddContext(string name, LocalProcessingLevel level, bool intentionalMix = false,
        ExtensionState? extension = null)
    {
        CheckActive();
        var context = new ProcessingContext(Id<ProcessingContext>.New(), name, level, intentionalMix, extension);
        State = State with { Contexts = State.Contexts.Add(context) };
        return context.Id;
    }

    public void SetPlacementRelationships(Id<PatternPlacement> placementId, Id<ProcessingContext>? itemContextId,
        Id<ProcessingContext>? containingContextId, Id<RouteIntent>? routeId,
        ImmutableArray<PlacementPart> partRelationships)
    {
        CheckActive();
        ProjectValidation.Require(!partRelationships.IsDefault, "Part relationship array is required.");
        ChangePlacement(placementId, placement =>
        {
            var existing = placement.PartRelationships.ToDictionary(item => item.PartId);
            var merged = partRelationships.Select(item => existing.TryGetValue(item.PartId, out var old)
                ? old with { SharedPerformanceKey = item.SharedPerformanceKey, RouteId = item.RouteId } : item).ToImmutableArray();
            return placement with
            {
                ItemContextId = itemContextId, ContainingContextId = containingContextId, RouteId = routeId,
                PartRelationships = placement.PartRelationships.SequenceEqual(merged) ? placement.PartRelationships : merged
            };
        });
    }

    public Id<Pattern> MakePatternVariation(Id<PatternPlacement> placementId, string name)
    {
        CheckActive();
        var placement = Find(State.Placements, item => item.Id == placementId);
        var source = Find(State.Patterns, item => item.Id == placement.PatternId);
        var partIds = source.Parts.ToDictionary(part => part.Id, _ => Id<MusicalPart>.New());
        var variation = source with
        {
            Id = Id<Pattern>.New(), Name = name,
            Parts = [.. source.Parts.Select(part => part with
            {
                Id = partIds[part.Id], Notes = [.. part.Notes.Select(note => note with { Id = Id<NoteEvent>.New() })]
            })]
        };
        State = State with { Patterns = State.Patterns.Add(variation) };
        ChangePlacement(placementId, item => item with
        {
            PatternId = variation.Id,
            PartRelationships = [.. item.PartRelationships.Select(part => part with { PartId = partIds[part.PartId] })]
        });
        return variation.Id;
    }

    /// <summary>Retargets one part in its Pattern definition; all placements of that Pattern see the new sound.</summary>
    public Id<SoundDefinition> MakeSoundIndependent(Id<Pattern> patternId, Id<MusicalPart> partId, string name)
    {
        CheckActive();
        var part = Find(Find(State.Patterns, item => item.Id == patternId).Parts, item => item.Id == partId);
        var source = Find(State.Sounds, item => item.Id == part.SoundId);
        var independent = source with { Id = Id<SoundDefinition>.New(), Name = name };
        State = State with { Sounds = State.Sounds.Add(independent) };
        ChangePart(patternId, partId, item => item with { SoundId = independent.Id });
        return independent.Id;
    }

    public void DeletePlacement(Id<PatternPlacement> placementId)
    {
        CheckActive();
        var placement = Find(State.Placements, item => item.Id == placementId);
        State = State with { Placements = Remove(State.Placements, item => item.Id == placementId) };
        if (placement.ItemContextId is { } contextId)
            State = State with { Contexts = Remove(State.Contexts, item => item.Id == contextId) };
    }

    // Definition/resource removal never cascades into unrelated musical content. Commit validates remaining uses.
    public void DeletePattern(Id<Pattern> id) { CheckActive(); State = State with { Patterns = Remove(State.Patterns, item => item.Id == id) }; }
    public void DeleteSound(Id<SoundDefinition> id) { CheckActive(); State = State with { Sounds = Remove(State.Sounds, item => item.Id == id) }; }
    public void DeleteResource(Id<ResourceDescriptor> id) { CheckActive(); State = State with { Resources = Remove(State.Resources, item => item.Id == id) }; }
    public void DeleteGroup(Id<InstrumentGroup> id) { CheckActive(); State = State with { Groups = Remove(State.Groups, item => item.Id == id) }; }
    public void DeleteContext(Id<ProcessingContext> id) { CheckActive(); State = State with { Contexts = Remove(State.Contexts, item => item.Id == id) }; }
    public void DeleteRoute(Id<RouteIntent> id) { CheckActive(); State = State with { Routes = Remove(State.Routes, item => item.Id == id) }; }

    private void ChangePattern(Id<Pattern> id, Func<Pattern, Pattern> change) =>
        State = State with { Patterns = Replace(State.Patterns, item => item.Id == id, change) };
    private void ChangePart(Id<Pattern> patternId, Id<MusicalPart> partId, Func<MusicalPart, MusicalPart> change) =>
        ChangePattern(patternId, pattern => pattern with { Parts = Replace(pattern.Parts, item => item.Id == partId, change) });
    private void ChangePlacement(Id<PatternPlacement> id, Func<PatternPlacement, PatternPlacement> change) =>
        State = State with { Placements = Replace(State.Placements, item => item.Id == id, change) };

    private static T Find<T>(ImmutableArray<T> items, Func<T, bool> predicate)
    {
        foreach (var item in items) if (predicate(item)) return item;
        throw new ProjectValidationException("Edit target does not exist.");
    }

    private static ImmutableArray<T> Replace<T>(ImmutableArray<T> items, Func<T, bool> predicate, Func<T, T> change)
    {
        var old = Find(items, predicate);
        var replacement = change(old);
        return EqualityComparer<T>.Default.Equals(old, replacement) ? items : items.SetItem(items.IndexOf(old), replacement);
    }

    private static ImmutableArray<T> Remove<T>(ImmutableArray<T> items, Func<T, bool> predicate) => items.Remove(Find(items, predicate));
}
