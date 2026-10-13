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
}
