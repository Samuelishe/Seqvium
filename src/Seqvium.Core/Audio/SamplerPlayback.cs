// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Non-destructive source-frame intent owned by one sound. Endpoints are exclusive.</summary>
public readonly record struct SamplerPlayback(long Start, long End, bool Loop, long LoopStart, long LoopEnd)
{
    public static readonly ImmutableArray<string> Keys =
        ["sampleStart", "sampleEnd", "loopEnabled", "loopStart", "loopEnd"];

    public static bool HasIntent(SoundDefinition sound) => sound.Algorithm == PcmSampler.Algorithm &&
        Keys.Any(sound.Parameters.ContainsKey);

    public static bool Supports(SoundDefinition sound)
    {
        if (sound.Algorithm != PcmSampler.Algorithm || sound.Extension is not null || !sound.AdditionalData.IsEmpty ||
            sound.ResourceIds.Length != 1 ||
            !sound.Parameters.TryGetValue("rootPitch", out var root) || root is < 0 or > 127 ||
            !sound.Parameters.TryGetValue("releaseMilliseconds", out var release) || release is < 0 or > 1000 ||
            sound.Parameters.Keys.Any(k => k is not ("rootPitch" or "releaseMilliseconds") && !Keys.Contains(k)))
            return false;
        try { Read(sound, int.MaxValue); return true; }
        catch (ArgumentException) { return false; }
    }

    public static SamplerPlayback Read(SoundDefinition sound, long frames)
    {
        if (!HasIntent(sound)) return new(0, frames, false, 0, 0);
        long Frame(string key)
        {
            if (!sound.Parameters.TryGetValue(key, out var value) || value < 0 || value > int.MaxValue ||
                decimal.Truncate(value) != value) throw new ArgumentException("Invalid sampler frames.");
            return (long)value;
        }
        if (!sound.Parameters.TryGetValue("loopEnabled", out var enabled) || enabled is not (0 or 1))
            throw new ArgumentException("Invalid sampler loop state.");
        var playback = new SamplerPlayback(Frame("sampleStart"), Frame("sampleEnd"), enabled == 1,
            Frame("loopStart"), Frame("loopEnd"));
        playback.Validate(frames);
        return playback;
    }

    public void Validate(long frames)
    {
        if (Start < 0 || Start >= End || End > frames || frames > int.MaxValue ||
            (Loop || LoopStart != 0 || LoopEnd != 0) &&
            (LoopStart < Start || LoopEnd > End || LoopEnd - LoopStart < 4))
            throw new ArgumentException("Sampler range or loop is outside the source; loops need at least four frames.");
    }

    public ImmutableDictionary<string, decimal> Apply(SoundDefinition sound, long frames)
    {
        Validate(frames);
        var parameters = sound.Parameters.RemoveRange(Keys);
        if (this == new SamplerPlayback(0, frames, false, 0, 0)) return parameters;
        return parameters.SetItem("sampleStart", Start).SetItem("sampleEnd", End)
            .SetItem("loopEnabled", Loop ? 1 : 0).SetItem("loopStart", LoopStart).SetItem("loopEnd", LoopEnd);
    }

    internal PreparedNote Apply(PreparedNote note, int rate) => note with
    {
        SourceOffset = Start, SourceLength = End - Start, SamplerRange = true,
        SustainStart = Loop ? LoopStart : 0, SustainEnd = Loop ? LoopEnd : 0,
        SustainFade = Loop ? Math.Min(64, Math.Min(Math.Max(1, rate / 1000), (LoopEnd - LoopStart) / 4)) : 0
    };
}
