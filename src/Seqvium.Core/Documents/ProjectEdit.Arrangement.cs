// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.Core;

public sealed partial class ProjectEdit
{
    public void SetArrangementTrack(int lane, string name, int colorIndex, bool muted, bool solo)
    {
        CheckActive();
        var track = State.ArrangementTracks.SingleOrDefault(track => track.Lane == lane);
        if (track is null) State = State with { ArrangementTracks = State.ArrangementTracks.Add(
            new(Id<ArrangementTrack>.New(), lane, name, colorIndex, muted, solo)) };
        else State = State with { ArrangementTracks = Replace(State.ArrangementTracks, item => item.Id == track.Id,
            item => item with { Name = name, ColorIndex = colorIndex, Muted = muted, Solo = solo }) };
    }

    public Id<ArrangementClip> AddArrangementClip(Id<PatternPlacement> source, MusicalPosition position,
        int lane = 0, int repeats = 1)
    {
        CheckActive();
        var clip = new ArrangementClip(Id<ArrangementClip>.New(), source, position, lane, repeats);
        State = State with { ArrangementClips = State.ArrangementClips.Add(clip) };
        return clip.Id;
    }

    public void UpdateArrangementClip(Id<ArrangementClip> id, MusicalPosition position, int lane, int repeats)
    {
        CheckActive();
        State = State with { ArrangementClips = Replace(State.ArrangementClips, clip => clip.Id == id,
            clip => clip with { Position = position, Lane = lane, Repeats = repeats }) };
    }

    public void DeleteArrangementClip(Id<ArrangementClip> id)
    {
        CheckActive();
        State = State with { ArrangementClips = Remove(State.ArrangementClips, clip => clip.Id == id) };
    }

    /// <summary>Expands shared cycles in place; the first keeps the clip ID. No musical source is cloned.</summary>
    public void UnfoldArrangementRepeats(Id<ArrangementClip> id)
    {
        CheckActive();
        var clip = State.ArrangementClips.SingleOrDefault(c => c.Id == id);
        ProjectValidation.Require(clip is not null, "Arrangement.InvalidRepeat");
        ProjectValidation.Require(clip.Repeats is > 1 and <= 1024, "Arrangement.InvalidRepeat");
        // Opaque occurrence data may carry identities or span semantics that cannot be split safely.
        ProjectValidation.Require(clip.AdditionalData.IsEmpty, "Arrangement.UnfoldUnsupported");
        var placement = State.Placements.Single(p => p.Id == clip.SourcePlacementId);
        long length = State.Patterns.Single(p => p.Id == placement.PatternId).Length.Ticks;
        ProjectValidation.Require(length > 0 && length <= long.MaxValue / clip.Repeats &&
            clip.Position.Ticks >= 0 && clip.Position.Ticks <= long.MaxValue - length * clip.Repeats,
            "Arrangement.InvalidRange");
        var pieces = new ArrangementClip[clip.Repeats];
        for (int i = 0; i < pieces.Length; i++)
            pieces[i] = clip with { Id = i == 0 ? clip.Id : Id<ArrangementClip>.New(),
                Position = new(checked(clip.Position.Ticks + length * i)), Repeats = 1 };
        State = State with { ArrangementClips = [.. State.ArrangementClips.SelectMany(c => c.Id == id ? pieces : [c])] };
    }
}
