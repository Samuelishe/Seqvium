// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

/// <summary>Fixed-speed audio. Placement uses musical time; range and fades use native source frames.
/// Copies share a source sound and its Mixer assignment, never a Pattern or Playlist lane route.</summary>
public sealed record AudioClip(
    Id<AudioClip> Id,
    Id<SoundDefinition> SoundId,
    MusicalPosition Position,
    int Lane,
    long OffsetFrames,
    long LengthFrames,
    long FadeInFrames = 0,
    long FadeOutFrames = 0,
    decimal GainDb = 0,
    int Repeats = 1,
    long EnvelopeOffsetFrames = 0,
    long EnvelopeLengthFrames = 0) : CanonicalData;

public static class AudioClipSource
{
    public const string Algorithm = "core.audio-clip.fixed-v1";
    public static int Rate(SoundDefinition sound) => checked((int)sound.Parameters["sampleRate"]);
    public static long Frames(SoundDefinition sound) => checked((long)sound.Parameters["frames"]);

    public static long ConvertFrames(long frames, int sourceRate, int outputRate) =>
        checked((long)decimal.Round((decimal)frames * outputRate / sourceRate, 0, MidpointRounding.AwayFromZero));

    public static long EnvelopeLength(AudioClip clip) =>
        clip.EnvelopeLengthFrames == 0 ? clip.LengthFrames : clip.EnvelopeLengthFrames;

    // Split pieces retain the original source clock, including its resampling phase. The virtual
    // origin may precede zero when a later piece is moved to the beginning of the Song.
    public static (long Start, long End, long? Origin) ExecutionBounds(AudioClip clip, Tempo tempo,
        int sourceRate, int outputRate)
    {
        long start = ConstantTempoConversion.ToFrames(clip.Position, tempo, outputRate);
        if (clip.EnvelopeLengthFrames == 0 || clip.Repeats != 1)
            return (start, checked(start + ConvertFrames(clip.LengthFrames * clip.Repeats, sourceRate, outputRate)),
                null);
        long ticks = clip.Position.Ticks -
                     ConstantTempoConversion.FromFrames(clip.EnvelopeOffsetFrames, tempo, sourceRate).Ticks;
        long origin = Math.Sign(ticks) * ConstantTempoConversion.ToFrames(new(Math.Abs(ticks)), tempo, outputRate);
        return (checked(origin + ConvertFrames(clip.EnvelopeOffsetFrames, sourceRate, outputRate)),
            checked(origin + ConvertFrames(clip.EnvelopeOffsetFrames + clip.LengthFrames, sourceRate, outputRate)),
            origin);
    }

    public static long DurationTicks(long frames, int rate, Tempo tempo) =>
        Math.Max(1, ConstantTempoConversion.FromFrames(frames, tempo, rate).Ticks);

    public static long LengthTicks(ProjectState state, AudioClip clip) => DurationTicks(
        checked(clip.LengthFrames * clip.Repeats), Rate(state.Sounds.Single(s => s.Id == clip.SoundId)),
        state.Settings.Tempo);

    public static long EndTicks(ProjectState state, AudioClip clip) =>
        checked(clip.Position.Ticks + LengthTicks(state, clip));
}
