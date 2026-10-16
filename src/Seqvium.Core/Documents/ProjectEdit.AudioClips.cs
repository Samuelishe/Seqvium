// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

public sealed partial class ProjectEdit
{
    public Id<AudioClip> AddAudioClip(Id<SoundDefinition> sound, MusicalPosition position, int lane,
        long offset, long length)
    {
        CheckActive();
        var clip = new AudioClip(Id<AudioClip>.New(), sound, position, lane, offset, length);
        State = State with { AudioClips = State.AudioClips.Add(clip) };
        return clip.Id;
    }

    public void UpdateAudioClip(AudioClip clip)
    {
        CheckActive();
        State = State with
        {
            AudioClips = Replace(State.AudioClips, c => c.Id == clip.Id, c =>
                clip with { AdditionalData = c.AdditionalData })
        };
    }

    public Id<AudioClip> CopyAudioClip(Id<AudioClip> id, MusicalPosition position, int lane)
    {
        CheckActive();
        var copy = State.AudioClips.Single(c => c.Id == id) with
        {
            Id = Id<AudioClip>.New(), Position = position, Lane = lane
        };
        State = State with { AudioClips = State.AudioClips.Add(copy) };
        return copy.Id;
    }

    public void DeleteAudioClip(Id<AudioClip> id)
    {
        CheckActive();
        State = State with { AudioClips = Remove(State.AudioClips, c => c.Id == id) };
    }

    public Id<AudioClip> SplitAudioClip(Id<AudioClip> id, MusicalPosition position)
    {
        CheckActive();
        var clip = State.AudioClips.Single(c => c.Id == id);
        ProjectValidation.Require(clip.Repeats == 1, "Split a single audio occurrence; unfold repeats first.");
        int rate = AudioClipSource.Rate(State.Sounds.Single(s => s.Id == clip.SoundId));
        long elapsed = ConstantTempoConversion.ToFrames(new(position.Ticks - clip.Position.Ticks), State.Settings.Tempo,
            rate);
        long cut = checked((long)decimal.Round(elapsed / clip.StretchRatio, 0, MidpointRounding.AwayFromZero));
        ProjectValidation.Require(cut > 0 && cut < clip.LengthFrames, "Split must be inside the source range.");
        long remaining = clip.LengthFrames - cut;
        long envelope = AudioClipSource.EnvelopeLength(clip);
        long envelopeOffset = clip.EnvelopeOffsetFrames + cut;
        long origin = clip.Position.Ticks - ConstantTempoConversion.FromFrames(
            AudioClipSource.StretchedFrames(clip.EnvelopeOffsetFrames, clip.StretchRatio),
            State.Settings.Tempo, rate).Ticks;
        var right = clip with
        {
            Id = Id<AudioClip>.New(), Position = new(origin + ConstantTempoConversion.FromFrames(
                AudioClipSource.StretchedFrames(envelopeOffset, clip.StretchRatio),
                State.Settings.Tempo, rate).Ticks),
            OffsetFrames = clip.OffsetFrames + cut,
            LengthFrames = remaining, EnvelopeOffsetFrames = envelopeOffset, EnvelopeLengthFrames = envelope
        };
        UpdateAudioClip(clip with
        {
            LengthFrames = cut, EnvelopeLengthFrames = envelope
        });
        State = State with { AudioClips = State.AudioClips.Add(right) };
        return right.Id;
    }
}
