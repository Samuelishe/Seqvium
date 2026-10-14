// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public static class ProjectValidation
{
    public const int MaximumEntities = 100_000;
    public const int MaximumTextLength = 4096;

    public static void Validate(ProjectState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var identities = new HashSet<Guid>();

        void Identity<T>(Id<T> id)
        {
            Require(id.Value != Guid.Empty && identities.Add(id.Value),
                "Entity identities must be nonempty and globally distinct.");
            Require(identities.Count <= MaximumEntities, "Project entity limit exceeded.");
        }

        Identity(state.Id);
        OptionalText(state.Name);
        Require(state.Settings is not null && state.Settings.Meter is not null,
            "Project settings and meter are required.");
        _ = new Tempo(state.Settings.Tempo.BeatsPerMinute);
        var meter = state.Settings.Meter;
        Require(meter.Numerator is >= 1 and <= 64 && meter.Denominator is >= 1 and <= 64 &&
                (meter.Denominator & (meter.Denominator - 1)) == 0, "Unsupported meter.");
        Present(state.Patterns);
        Present(state.Sounds);
        Present(state.Placements);
        Present(state.Groups);
        Present(state.Resources);
        Present(state.Contexts);
        Present(state.Routes);

        foreach (var resource in state.Resources)
        {
            Identity(resource.Id);
            Text(resource.Name);
            OptionalText(resource.Origin);
            if (resource.ManagedLocator is { } locator)
            {
                Text(locator);
                Require(!locator.Contains('\\') && !locator.Contains(':') && !locator.StartsWith('/') &&
                        locator.Split('/').All(segment => segment.Length > 0 && segment is not "." and not ".."),
                    "Managed locators must be portable relative paths without traversal.");
            }
        }

        var resources = state.Resources.Select(item => item.Id).ToHashSet();
        foreach (var sound in state.Sounds)
        {
            Identity(sound.Id);
            Text(sound.Name);
            Text(sound.Algorithm);
            Present(sound.ResourceIds);
            Require(sound.Parameters is not null, "Sound parameters are required.");
            foreach (var key in sound.Parameters.Keys) Text(key);
            Unique(sound.ResourceIds);
            Require(sound.ResourceIds.All(resources.Contains), "Sound references an unknown resource.");
            Extension(sound.Extension);
        }

        var sounds = state.Sounds.ToDictionary(item => item.Id);
        MixerValidation.Validate(state, identities);
        foreach (var pattern in state.Patterns)
        {
            Identity(pattern.Id);
            Text(pattern.Name);
            Present(pattern.Parts);
            Require(pattern.Length.Ticks > 0, "Pattern length must be positive.");
            foreach (var part in pattern.Parts)
            {
                Identity(part.Id);
                Text(part.Name);
                Present(part.Notes);
                Require(sounds.ContainsKey(part.SoundId), "Part references an unknown sound definition.");
                foreach (var note in part.Notes)
                {
                    Identity(note.Id);
                    Require(note.Duration.Ticks > 0 && note.Position.Ticks >= 0 &&
                            note.Position.Ticks <= pattern.Length.Ticks - note.Duration.Ticks,
                        "Note must fit within the Pattern length.");
                    Require(note.Pitch is >= 0 and <= 127 && note.Intensity is >= 0 and <= 1,
                        "Pitch must be 0–127 semitones; intensity must be 0–1.");
                }
            }
        }

        var patterns = state.Patterns.ToDictionary(item => item.Id);
        foreach (var group in state.Groups)
        {
            Identity(group.Id);
            Text(group.Name);
            Present(group.SoundIds);
            Unique(group.SoundIds);
            Require(group.SoundIds.All(sounds.ContainsKey), "Group references an unknown sound definition.");
        }

        foreach (var context in state.Contexts)
        {
            Identity(context.Id);
            Text(context.Name);
            Extension(context.Extension);
            Require(Enum.IsDefined(context.Level), "Unknown local processing level.");
        }

        foreach (var route in state.Routes)
        {
            Identity(route.Id);
            Text(route.Name);
        }

        var contexts = state.Contexts.ToDictionary(item => item.Id);
        var routes = state.Routes.Select(item => item.Id).ToHashSet();
        var itemOwners = new HashSet<Id<ProcessingContext>>();
        var performanceSounds = new Dictionary<Guid, Id<SoundDefinition>>();
        foreach (var placement in state.Placements)
        {
            Identity(placement.Id);
            Present(placement.PartRelationships);
            Require(patterns.TryGetValue(placement.PatternId, out var pattern),
                "Placement references an unknown Pattern.");
            Require(placement.Position.Ticks >= 0 && placement.Position.Ticks <= long.MaxValue - pattern.Length.Ticks,
                "Placement end exceeds musical-time range.");
            CheckContext(placement.ItemContextId, LocalProcessingLevel.Item);
            CheckContext(placement.ContainingContextId, LocalProcessingLevel.Containing);
            if (placement.ItemContextId is { } item)
                Require(itemOwners.Add(item), "An item-local context cannot belong to multiple placements.");
            CheckRoute(placement.RouteId);
            Unique(placement.PartRelationships.Select(item => item.PartId));
            var parts = pattern.Parts.ToDictionary(part => part.Id);
            bool aggregate = new[] { placement.ItemContextId, placement.ContainingContextId }
                .Any(id => id is { } value && contexts[value].IntentionalMix);
            foreach (var relationship in placement.PartRelationships)
            {
                Require(parts.TryGetValue(relationship.PartId, out var part),
                    "Placement relationship references a part outside its Pattern.");
                CheckRoute(relationship.RouteId);
                Require(!aggregate || relationship.RouteId is null,
                    "Part routes cannot be retained after an intentional placement aggregate.");
                if (relationship.SharedPerformanceKey is { } key)
                {
                    Require(key != Guid.Empty, "Performance interaction key cannot be empty.");
                    Require(!performanceSounds.TryGetValue(key, out var existing) || existing == part.SoundId,
                        "A shared performance interaction must use one sound definition.");
                    performanceSounds[key] = part.SoundId;
                }
            }
        }

        Require(
            state.Contexts.Where(item => item.Level == LocalProcessingLevel.Item)
                .All(item => itemOwners.Contains(item.Id)),
            "An item-local context must have exactly one placement owner.");
        Present(state.ArrangementClips);
        Present(state.ArrangementTracks);
        Unique(state.ArrangementTracks.Select(track => track.Lane));
        foreach (var track in state.ArrangementTracks)
        {
            Identity(track.Id);
            Text(track.Name);
            Require(track.Lane is >= 0 and < 32 && track.ColorIndex is >= 0 and < 8,
                "Unsupported Playlist track or color.");
        }

        var placements = state.Placements.ToDictionary(item => item.Id);
        foreach (var clip in state.ArrangementClips)
        {
            Identity(clip.Id);
            Require(placements.TryGetValue(clip.SourcePlacementId, out var source),
                "Clip references an unknown source placement.");
            Require(clip.Lane is >= 0 and < 32 && clip.Repeats is >= 1 and <= 1024,
                "Unsupported clip lane or repeat count.");
            long length = patterns[source.PatternId].Length.Ticks;
            Require(length <= long.MaxValue / clip.Repeats && clip.Position.Ticks >= 0 &&
                    clip.Position.Ticks <= long.MaxValue - length * clip.Repeats,
                "Clip end exceeds musical-time range.");
        }

        GraphStructure.Validate(state, value => Identity(new Id<GraphDefinition>(value)));

        Present(state.AudioClips);
        foreach (var clip in state.AudioClips)
        {
            Identity(clip.Id);
            Require(sounds.TryGetValue(clip.SoundId, out var sound) && sound.Algorithm == AudioClipSource.Algorithm,
                "Audio clip references an unknown audio source.");
            Require(sound.ResourceIds.Length == 1 && sound.Parameters.TryGetValue("sampleRate", out var rate) &&
                    rate is >= 8000 and <= 192000 && decimal.Truncate(rate) == rate &&
                    sound.Parameters.TryGetValue("frames", out var frames) && frames > 0 && frames <= int.MaxValue &&
                    decimal.Truncate(frames) == frames && sound.Parameters.TryGetValue("channels", out var channels) &&
                    channels is 1 or 2, "Invalid audio source metadata.");
            Require(clip.Lane is >= 0 and < 32 && clip.Repeats is >= 1 and <= 1024 && clip.OffsetFrames >= 0 &&
                    clip.LengthFrames > 0 && clip.OffsetFrames <= AudioClipSource.Frames(sound) - clip.LengthFrames &&
                    clip.FadeInFrames >= 0 && clip.FadeOutFrames >= 0 &&
                    clip.EnvelopeOffsetFrames >= 0 && clip.EnvelopeLengthFrames >= 0 &&
                    clip.EnvelopeOffsetFrames <= clip.OffsetFrames &&
                    clip.EnvelopeOffsetFrames <= AudioClipSource.EnvelopeLength(clip) - clip.LengthFrames &&
                    AudioClipSource.EnvelopeLength(clip) <= AudioClipSource.Frames(sound) &&
                    clip.FadeInFrames <= AudioClipSource.EnvelopeLength(clip) &&
                    clip.FadeOutFrames <= AudioClipSource.EnvelopeLength(clip) &&
                    clip.GainDb is >= -90 and <= 24 && clip.Position.Ticks >= 0,
                "Invalid audio clip range, fade, level or lane.");
            try
            {
                _ = AudioClipSource.EndTicks(state, clip);
            }
            catch (OverflowException)
            {
                throw new ProjectValidationException("Audio clip end exceeds musical-time range.");
            }
        }

        void CheckContext(Id<ProcessingContext>? id, LocalProcessingLevel level)
        {
            if (id is { } value)
                Require(contexts.TryGetValue(value, out var context) && context.Level == level,
                    "Missing context or wrong local level.");
        }

        void CheckRoute(Id<RouteIntent>? id)
        {
            if (id is { } value) Require(routes.Contains(value), "Unknown route intent.");
        }
    }

    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition,
        string message)
    {
        if (!condition) throw new ProjectValidationException(message);
    }

    private static void Present<T>(ImmutableArray<T> items) => Require(
        !items.IsDefault && items.All(item => item is not null), "Required entity array is missing or contains null.");

    private static void Unique<T>(IEnumerable<T> items) =>
        Require(items.Distinct().Count() == items.Count(), "Duplicate reference.");

    private static void Text(string text) =>
        Require(!string.IsNullOrWhiteSpace(text) && text.Length <= MaximumTextLength, "Invalid or oversized text.");

    private static void OptionalText(string? text)
    {
        if (text is not null) Text(text);
    }

    private static void Extension(ExtensionState? extension)
    {
        if (extension is null) return;
        Text(extension.ExtensionId);
        Require(extension.StateVersion > 0 && extension.Payload.ValueKind != System.Text.Json.JsonValueKind.Undefined,
            "Extension state requires a positive version and defined opaque JSON.");
    }
}
