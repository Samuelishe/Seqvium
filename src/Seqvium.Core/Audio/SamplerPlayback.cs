// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Non-destructive source-frame intent owned by one sound. Endpoints are exclusive.</summary>
public readonly record struct SamplerPlayback(long Start, long End, bool Loop, long LoopStart, long LoopEnd,
    long Crossfade = 0)
{
    public const string CrossfadeKey = "loopCrossfadeFrames";
    public const int MaximumCrossfade = 4096;
    public static readonly ImmutableArray<string> Keys =
        ["sampleStart", "sampleEnd", "loopEnabled", "loopStart", "loopEnd", CrossfadeKey];

    public static bool HasCrossfadeIntent(SoundDefinition sound) => sound.Algorithm == PcmSampler.Algorithm &&
        sound.Parameters.ContainsKey(CrossfadeKey);

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
            Frame("loopStart"), Frame("loopEnd"), sound.Parameters.ContainsKey(CrossfadeKey) ? Frame(CrossfadeKey) : 0);
        playback.Validate(frames);
        return playback;
    }

    public void Validate(long frames)
    {
        if (Start < 0 || Start >= End || End > frames || frames > int.MaxValue ||
            (Loop || LoopStart != 0 || LoopEnd != 0) &&
            (LoopStart < Start || LoopEnd > End || LoopEnd - LoopStart < 4))
            throw new ArgumentException("Sampler range or loop is outside the source; loops need at least four frames.");
        if (Crossfade < 0 || Crossfade > CrossfadeLimit)
            throw new ArgumentException("Crossfade requires source frames before Loop Start and at most half the loop (4096 frames maximum).");
    }

    public long CrossfadeLimit => Math.Max(0, Math.Min(MaximumCrossfade, Math.Min(LoopStart - Start, (LoopEnd - LoopStart) / 2)));

    public ImmutableDictionary<string, decimal> Apply(SoundDefinition sound, long frames)
    {
        Validate(frames);
        var parameters = sound.Parameters.RemoveRange(Keys);
        if (this == new SamplerPlayback(0, frames, false, 0, 0)) return parameters;
        parameters = parameters.SetItem("sampleStart", Start).SetItem("sampleEnd", End)
            .SetItem("loopEnabled", Loop ? 1 : 0).SetItem("loopStart", LoopStart).SetItem("loopEnd", LoopEnd);
        return Crossfade == 0 ? parameters : parameters.SetItem(CrossfadeKey, Crossfade);
    }

    internal PreparedNote Apply(PreparedNote note, int rate) => note with
    {
        SourceOffset = Start, SourceLength = End - Start, SamplerRange = true,
        SustainStart = Loop ? LoopStart : 0, SustainEnd = Loop ? LoopEnd : 0,
        SustainFade = Loop && Crossfade == 0 ? Math.Min(64, Math.Min(Math.Max(1, rate / 1000), (LoopEnd - LoopStart) / 4)) : 0,
        SustainCrossfade = Loop ? Crossfade : 0
    };
}
