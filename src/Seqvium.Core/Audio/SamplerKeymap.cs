// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>One identified source and its inclusive integer key range. Fractional notes use their lower key.</summary>
public sealed record SamplerZone(
    Id<SamplerZone> Id,
    Id<ResourceDescriptor> ResourceId,
    decimal RootPitch,
    int LowKey,
    int HighKey,
    ImmutableDictionary<string, decimal> Parameters) : CanonicalData;

public static class SamplerKeymap
{
    public const int MaximumZones = 128;
    public static bool HasIntent(SoundDefinition sound) => !sound.SamplerZones.IsDefaultOrEmpty;

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
        if (!HasIntent(sound)) return sound;
        int key = (int)decimal.Floor(pitch);
        var zone = sound.SamplerZones.SingleOrDefault(z => key >= z.LowKey && key <= z.HighKey);
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
        int last = -1;
        foreach (var z in zones.OrderBy(z => z.LowKey))
        {
            if (z.Id.Value == Guid.Empty || z.LowKey < 0 || z.HighKey > 127 || z.LowKey > z.HighKey ||
                z.LowKey <= last ||
                z.RootPitch is < 0 or > 127 || z.Parameters is null)
                throw new ArgumentException("Key zones must be ordered without overlap and within MIDI 0–127.");
            SamplerPlayback.Read(Source(sound, z), int.MaxValue);
            last = z.HighKey;
        }
    }

    /// <summary>Nearest confirmed root, ties to the lower root; duplicate roots require explicit manual resolution.</summary>
    public static ImmutableArray<SamplerZone> Distribute(IEnumerable<SamplerZone> sources)
    {
        var ordered = sources.OrderBy(z => z.RootPitch).ThenBy(z => z.Id.Value).ToArray();
        if (ordered.Length is < 1 or > MaximumZones ||
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
}