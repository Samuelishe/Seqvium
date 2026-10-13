// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json;

namespace Seqvium.Core;

/// <summary>Portable sound snapshots with no notes, media or graph state.</summary>
public sealed record DrumPresetFile(int Version, string Algorithm, ImmutableDictionary<string, decimal> Parameters)
{
    public byte[] Encode()
    {
        DrumInstruments.Validate(Algorithm, Parameters);
        if (Version != 1) throw new NotSupportedException("Unsupported drum preset version.");
        return JsonSerializer.SerializeToUtf8Bytes(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public static DrumPresetFile Decode(byte[] bytes, string algorithm)
    {
        if (bytes.Length > 32768) throw new NotSupportedException("Drum preset size exceeded.");
        var preset = JsonSerializer.Deserialize<DrumPresetFile>(bytes) ?? throw new JsonException("Empty preset.");
        if (preset.Version != 1 || preset.Algorithm != algorithm || preset.Parameters is null)
            throw new NotSupportedException("Preset belongs to another instrument or version.");
        DrumInstruments.Validate(algorithm, preset.Parameters);
        return preset;
    }
}
