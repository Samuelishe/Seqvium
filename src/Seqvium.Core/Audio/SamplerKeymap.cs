// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>One identified source. Keys are inclusive; velocity is [low, high), with 1 included at the top.</summary>
public sealed record SamplerZone(
    Id<SamplerZone> Id,
    Id<ResourceDescriptor> ResourceId,
    decimal RootPitch,
    int LowKey,
    int HighKey,
    ImmutableDictionary<string, decimal> Parameters) : CanonicalData
{
    public decimal LowVelocity { get; init; }
    public decimal HighVelocity { get; init; } = 1;
}

public static class SamplerKeymap
{
    public const int MaximumZones = 128;
    public static bool HasIntent(SoundDefinition sound) => !sound.SamplerZones.IsDefaultOrEmpty;
    public static bool HasVelocityIntent(SoundDefinition sound) => HasIntent(sound) &&
        sound.SamplerZones.Any(z => z.LowVelocity != 0 || z.HighVelocity != 1);

    public static bool ContainsVelocity(SamplerZone zone, decimal velocity) =>
        velocity >= zone.LowVelocity && (velocity < zone.HighVelocity || velocity == 1 && zone.HighVelocity == 1);

    public static bool VelocityOverlaps(SamplerZone a, SamplerZone b) =>
        a.LowVelocity < b.HighVelocity && b.LowVelocity < a.HighVelocity;

    public static ImmutableArray<SamplerZone> Zones(SoundDefinition sound) => HasIntent(sound)
        ? sound.SamplerZones
        :
        [
            new(LegacyId(sound), sound.ResourceIds.Single(), sound.Parameters["rootPitch"], 0, 127,
                sound.Parameters.Where(p => SamplerPlayback.Keys.Contains(p.Key)).ToImmutableDictionary())
        ];

    private static Id<SamplerZone> LegacyId(SoundDefinition sound)
    {
        var bytes = sound.Id.Value.ToByteArray();
        bytes[0] ^= 0x80;
        return new(new Guid(bytes));
    }

    public static SoundDefinition Source(SoundDefinition sound, SamplerZone zone) => sound with
    {
        SamplerZones = [], ResourceIds = [zone.ResourceId],
        Parameters = zone.Parameters.SetItem("rootPitch", zone.RootPitch)
            .SetItem("releaseMilliseconds", sound.Parameters["releaseMilliseconds"])
    };

    public static SoundDefinition? Select(SoundDefinition sound, decimal pitch)
    {
        if (HasVelocityIntent(sound)) throw new ArgumentException("Velocity is required for a layered map.");
        return Select(sound, pitch, 1);
    }

    public static SoundDefinition? Select(SoundDefinition sound, decimal pitch, decimal intensity)
    {
        if (pitch is < 0 or > 127 || intensity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity));
        if (!HasIntent(sound)) return sound;
        int key = (int)decimal.Floor(pitch);
        var zone = sound.SamplerZones.SingleOrDefault(z => key >= z.LowKey && key <= z.HighKey &&
            ContainsVelocity(z, intensity));
        return zone is null ? null : Source(sound, zone);
    }

    public static void Validate(SoundDefinition sound)
    {
        if (sound.SamplerZones.IsDefault) throw new ArgumentException("Missing key map.");
        if (!HasIntent(sound)) return;
        var zones = sound.SamplerZones;
        if (sound.Algorithm != PcmSampler.Algorithm || zones.Length > MaximumZones ||
            !sound.Parameters.TryGetValue("rootPitch", out var root) || root is < 0 or > 127 ||
            !sound.Parameters.TryGetValue("releaseMilliseconds", out var release) || release is < 0 or > 1000 ||
            zones.Any(z => z is null) ||
            zones.Select(z => z.Id).Distinct().Count() != zones.Length ||
            !sound.ResourceIds.ToHashSet().SetEquals(zones.Select(z => z.ResourceId)))
            throw new ArgumentException("Invalid key map resources or capacity.");
        foreach (var z in zones)
        {
            if (z.Id.Value == Guid.Empty || z.LowKey < 0 || z.HighKey > 127 || z.LowKey > z.HighKey ||
                z.LowVelocity < 0 || z.HighVelocity > 1 || z.LowVelocity >= z.HighVelocity ||
                z.RootPitch is < 0 or > 127 || z.Parameters is null)
                throw new ArgumentException("Zones require keys within MIDI 0–127 and a positive velocity range within [0,1].");
            SamplerPlayback.Read(Source(sound, z), int.MaxValue);
        }
        for (int i = 0; i < zones.Length; i++)
        for (int j = i + 1; j < zones.Length; j++)
            if (zones[i].LowKey <= zones[j].HighKey && zones[j].LowKey <= zones[i].HighKey &&
                VelocityOverlaps(zones[i], zones[j]))
                throw new ArgumentException("Zones overlap in both pitch and velocity.");
    }

    /// <summary>Nearest confirmed root, ties to the lower root; duplicate roots require explicit manual resolution.</summary>
    public static ImmutableArray<SamplerZone> Distribute(IEnumerable<SamplerZone> sources)
    {
        var ordered = sources.OrderBy(z => z.RootPitch).ThenBy(z => z.Id.Value).ToArray();
        if (ordered.Length is < 1 or > MaximumZones ||
            ordered.Any(z => z.LowVelocity != 0 || z.HighVelocity != 1) ||
            ordered.Select(z => z.RootPitch).Distinct().Count() != ordered.Length ||
            ordered.Any(z => z.RootPitch is < 0 or > 127))
            throw new ArgumentException("Distinct confirmed roots required.");
        int low = 0;
        return
        [
            .. ordered.Select((z, i) =>
            {
                int high = i == ordered.Length - 1
                    ? 127
                    : (int)decimal.Floor((z.RootPitch + ordered[i + 1].RootPitch) / 2);
                if (high < low) throw new ArgumentException("Roots are too close for distinct key ranges.");
                var result = z with { LowKey = low, HighKey = high };
                low = high + 1;
                return result;
            })
        ];
    }

    /// <summary>Explicit draft operation: distribute distinct roots, then split each root's recordings in supplied order.</summary>
    public static ImmutableArray<SamplerZone> Layer(IEnumerable<SamplerZone> sources)
    {
        var groups = sources.GroupBy(z => z.RootPitch).OrderBy(g => g.Key).ToArray();
        if (groups.Sum(g => g.Count()) > MaximumZones) throw new ArgumentException("Key map capacity exceeded.");
        var keys = Distribute(groups.Select(g => g.First() with { LowVelocity = 0, HighVelocity = 1 }));
        return [.. groups.SelectMany((g, index) => g.Select((z, layer) => z with
        {
            LowKey = keys[index].LowKey, HighKey = keys[index].HighKey,
            LowVelocity = decimal.Round(layer * 100m / g.Count(), 2) / 100,
            HighVelocity = decimal.Round((layer + 1) * 100m / g.Count(), 2) / 100
        }))];
    }
}
