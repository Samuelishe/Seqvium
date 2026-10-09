// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;

namespace Seqvium.Core;

/// <summary>A stable reference with a compile-time entity kind. Empty/default IDs are invalid canonical data.</summary>
public readonly record struct Id<T>(Guid Value)
{
    public static Id<T> New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public abstract record CanonicalData
{
    // JsonElement is immutable. Load clones values out of parser-owned lifetimes.
    public ImmutableDictionary<string, JsonElement> AdditionalData
    {
        get;
        init => field = value.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.Clone());
    } = ImmutableDictionary<string, JsonElement>.Empty;
}

public sealed record Meter : CanonicalData
{
    public int Numerator { get; init; }
    public int Denominator { get; init; }

    public Meter(int numerator, int denominator)
    {
        if (numerator is < 1 or > 64 || denominator is < 1 or > 64 || (denominator & (denominator - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(denominator), "Meter requires numerator 1–64 and power-of-two denominator 1–64.");
        Numerator = numerator;
        Denominator = denominator;
    }

    public long TicksPerBeat => MusicalPosition.TicksPerQuarter * 4 / Denominator;
    public long TicksPerBar => checked(TicksPerBeat * Numerator);
    public MusicalPosition BarStart(long zeroBasedBar) => new(checked(zeroBasedBar * TicksPerBar));
}

public sealed record ProjectSettings(Tempo Tempo, Meter Meter) : CanonicalData;
public sealed record NoteEvent(Id<NoteEvent> Id, MusicalPosition Position, MusicalDuration Duration,
    decimal Pitch, decimal Intensity) : CanonicalData;
public sealed record MusicalPart(Id<MusicalPart> Id, string Name, Id<SoundDefinition> SoundId,
    ImmutableArray<NoteEvent> Notes) : CanonicalData;
public sealed record Pattern(Id<Pattern> Id, string Name, MusicalDuration Length,
    ImmutableArray<MusicalPart> Parts) : CanonicalData
{
    /// <summary>Stable equal-position ordering: part UUID then note UUID; not insertion order or pitch.</summary>
    public IEnumerable<(Id<MusicalPart> PartId, NoteEvent Note)> OrderedNotes() =>
        Parts.SelectMany(part => part.Notes.Select(note => (PartId: part.Id, Note: note)))
            .OrderBy(item => item.Note.Position.Ticks)
            .ThenBy(item => item.PartId.Value).ThenBy(item => item.Note.Id.Value);
}

public sealed record ExtensionState : CanonicalData
{
    public string ExtensionId { get; init; }
    public int StateVersion { get; init; }
    public JsonElement Payload { get; init => field = value.Clone(); }

    public ExtensionState(string extensionId, int stateVersion, JsonElement payload)
    {
        ExtensionId = extensionId;
        StateVersion = stateVersion;
        Payload = payload;
    }
}
public sealed record SoundDefinition(Id<SoundDefinition> Id, string Name, string Algorithm,
    ImmutableDictionary<string, decimal> Parameters, ImmutableArray<Id<ResourceDescriptor>> ResourceIds,
    ExtensionState? Extension) : CanonicalData;
public sealed record ResourceDescriptor(Id<ResourceDescriptor> Id, string Name, string? ManagedLocator,
    string? Origin) : CanonicalData;
public sealed record InstrumentGroup(Id<InstrumentGroup> Id, string Name,
    ImmutableArray<Id<SoundDefinition>> SoundIds) : CanonicalData;

public enum LocalProcessingLevel { Item, Containing }
public sealed record ProcessingContext(Id<ProcessingContext> Id, string Name, LocalProcessingLevel Level,
    bool IntentionalMix, ExtensionState? Extension) : CanonicalData;
public sealed record RouteIntent(Id<RouteIntent> Id, string Name) : CanonicalData;

/// <summary>Part-specific contribution path. A shared performance key requests interaction, not a runtime instance.</summary>
public sealed record PlacementPart(Id<MusicalPart> PartId, Guid? SharedPerformanceKey,
    Id<RouteIntent>? RouteId) : CanonicalData;
public sealed record PatternPlacement(Id<PatternPlacement> Id, Id<Pattern> PatternId, MusicalPosition Position,
    Id<ProcessingContext>? ItemContextId, Id<ProcessingContext>? ContainingContextId,
    Id<RouteIntent>? RouteId, ImmutableArray<PlacementPart> PartRelationships) : CanonicalData;

public sealed record ProjectState(Id<ProjectState> Id, string? Name, ProjectSettings Settings,
    ImmutableArray<Pattern> Patterns, ImmutableArray<SoundDefinition> Sounds,
    ImmutableArray<PatternPlacement> Placements, ImmutableArray<InstrumentGroup> Groups,
    ImmutableArray<ResourceDescriptor> Resources, ImmutableArray<ProcessingContext> Contexts,
    ImmutableArray<RouteIntent> Routes) : CanonicalData
{
    // Optional properties keep historical graph-free constructor contracts intact.
    public ImmutableArray<GraphDefinition> Graphs { get; init; } = [];
    public ImmutableArray<GraphAttachment> GraphAttachments { get; init; } = [];
}

public sealed record ProjectSnapshot(ProjectState State, Guid Revision);

// Envelope compatibility belongs to the document, without retaining an obsolete loaded musical snapshot.
internal sealed record ProjectCompatibility(int Minor, int MinimumReaderMinor) : CanonicalData;

public sealed class ProjectValidationException(string message) : Exception(message);
