// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.Core;

public sealed partial class ProjectEdit
{
    public Id<ArrangementSection> AddArrangementSection(string name, MusicalPosition position,
        MusicalDuration length, int colorIndex)
    {
        CheckActive();
        var section = new ArrangementSection(Id<ArrangementSection>.New(), name, position, length, colorIndex);
        State = State with { ArrangementSections = State.ArrangementSections.Add(section) };
        return section.Id;
    }

    public void UpdateArrangementSection(Id<ArrangementSection> id, string name, MusicalPosition position,
        MusicalDuration length, int colorIndex)
    {
        CheckActive();
        State = State with { ArrangementSections = Replace(State.ArrangementSections, s => s.Id == id,
            s => s with { Name = name, Position = position, Length = length, ColorIndex = colorIndex }) };
    }

    public void DeleteArrangementSection(Id<ArrangementSection> id)
    {
        CheckActive();
        State = State with { ArrangementSections = Remove(State.ArrangementSections, s => s.Id == id) };
    }

    /// <summary>Swaps adjacent named ranges, including any gap between them. Crossing clips refuse atomically.</summary>
    public void MoveArrangementSection(Id<ArrangementSection> id, bool later)
    {
        CheckActive();
        var ordered = State.ArrangementSections.OrderBy(s => s.Position.Ticks).ToArray();
        int index = Array.FindIndex(ordered, s => s.Id == id);
        ProjectValidation.Require(index >= 0, "Unknown Playlist section.");
        int other = index + (later ? 1 : -1);
        if (other < 0 || other >= ordered.Length) return;
        var left = ordered[Math.Min(index, other)];
        var right = ordered[Math.Max(index, other)];
        long a = left.Position.Ticks, b = a + left.Length.Ticks;
        long c = right.Position.Ticks, d = c + right.Length.Ticks;
        RequireNoCrossings(a, b, c, d);
        long Map(long tick) => tick < a || tick >= d ? tick :
            tick < b ? tick + d - b : tick < c ? tick + right.Length.Ticks - left.Length.Ticks : tick - (c - a);
        TransformSongPositions(Map);
        State = State with { ArrangementSections = [.. State.ArrangementSections.Select(s =>
            s with { Position = new(Map(s.Position.Ticks)) })] };
    }

    /// <summary>Inserts a shared-content copy immediately after the range, shifting later music by its length.</summary>
    public Id<ArrangementSection> DuplicateArrangementSection(Id<ArrangementSection> id)
    {
        CheckActive();
        var section = State.ArrangementSections.Single(s => s.Id == id);
        long start = section.Position.Ticks, length = section.Length.Ticks, end = checked(start + length);
        RequireNoCrossings(start, end);
        var patterns = State.ArrangementClips.Where(c => c.Position.Ticks >= start && c.Position.Ticks < end).ToArray();
        var audio = State.AudioClips.Where(c => c.Position.Ticks >= start && c.Position.Ticks < end).ToArray();
        TransformSongPositions(t => t >= end ? checked(t + length) : t);
        State = State with { ArrangementSections = [.. State.ArrangementSections.Select(s =>
            s.Position.Ticks >= end ? s with { Position = new(checked(s.Position.Ticks + length)) } : s)] };
        foreach (var clip in patterns) CopyArrangementClip(clip, new(checked(clip.Position.Ticks + length)), clip.Lane);
        foreach (var clip in audio) CopyAudioClip(clip.Id, new(checked(clip.Position.Ticks + length)), clip.Lane);
        var copy = section with { Id = Id<ArrangementSection>.New(), Name = section.Name + " 2", Position = new(end) };
        State = State with { ArrangementSections = State.ArrangementSections.Add(copy) };
        return copy.Id;
    }

    private void RequireNoCrossings(params long[] boundaries)
    {
        if (PlaylistEditing.FindBoundaryConflict(State, boundaries) is { } conflict)
            throw new ProjectValidationException("Arrangement.CrossingSection · " + conflict.Name +
                " · #" + (conflict.Lane + 1) + " · " +
                ((decimal)conflict.Position / State.Settings.Meter.TicksPerBar + 1).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
    }

    private void TransformSongPositions(Func<long, long> map)
    {
        foreach (var clip in State.ArrangementClips)
            UpdateArrangementClip(clip.Id, new(map(clip.Position.Ticks)), clip.Lane, clip.Repeats);
        foreach (var clip in State.AudioClips)
            UpdateAudioClip(clip with { Position = new(map(clip.Position.Ticks)) });
    }

    private Id<ArrangementClip> CopyArrangementClip(ArrangementClip clip, MusicalPosition position, int lane)
    {
        var copy = clip with { Id = Id<ArrangementClip>.New(), Position = position, Lane = lane };
        State = State with { ArrangementClips = State.ArrangementClips.Add(copy) };
        return copy.Id;
    }

    public void EditPlaylistSelection(IReadOnlyList<Id<ArrangementClip>> patternIds, IReadOnlyList<Id<AudioClip>> audioIds,
        long tickDelta = 0, int laneDelta = 0, bool copy = false, bool delete = false, bool duplicate = false)
    {
        CheckActive();
        var patterns = patternIds.Distinct().Select(id => State.ArrangementClips.Single(c => c.Id == id)).ToArray();
        var audio = audioIds.Distinct().Select(id => State.AudioClips.Single(c => c.Id == id)).ToArray();
        var positions = patterns.Select(c => c.Position.Ticks).Concat(audio.Select(c => c.Position.Ticks)).ToArray();
        if (positions.Length == 0) return;
        var lanes = patterns.Select(c => c.Lane).Concat(audio.Select(c => c.Lane)).ToArray();
        if (duplicate)
        {
            tickDelta = patterns.Select(c => PlaylistEditing.EndTicks(State, c)).Concat(audio.Select(c => AudioClipSource.EndTicks(State, c)))
                .Max() - positions.Min();
            copy = true;
        }
        long delta = Math.Max(tickDelta, -positions.Min());
        int lane = Math.Clamp(laneDelta, -lanes.Min(), 31 - lanes.Max());
        foreach (var clip in patterns)
            if (delete) DeleteArrangementClip(clip.Id);
            else if (copy) CopyArrangementClip(clip, new(checked(clip.Position.Ticks + delta)), clip.Lane + lane);
            else UpdateArrangementClip(clip.Id, new(checked(clip.Position.Ticks + delta)), clip.Lane + lane, clip.Repeats);
        foreach (var clip in audio)
            if (delete) DeleteAudioClip(clip.Id);
            else if (copy) CopyAudioClip(clip.Id, new(checked(clip.Position.Ticks + delta)), clip.Lane + lane);
            else UpdateAudioClip(clip with { Position = new(checked(clip.Position.Ticks + delta)), Lane = clip.Lane + lane });
    }

    public void PastePlaylistClips(IReadOnlyList<ArrangementClip> patterns, IReadOnlyList<AudioClip> audio,
        MusicalPosition position, int lane)
    {
        CheckActive();
        var starts = patterns.Select(c => c.Position.Ticks).Concat(audio.Select(c => c.Position.Ticks)).ToArray();
        if (starts.Length == 0) return;
        var lanes = patterns.Select(c => c.Lane).Concat(audio.Select(c => c.Lane)).ToArray();
        long delta = position.Ticks - starts.Min();
        int laneDelta = lane - lanes.Min();
        ProjectValidation.Require(lane >= 0 && lanes.Max() + laneDelta < 32, "Arrangement.ClipboardRange");
        foreach (var clip in patterns) CopyArrangementClip(clip, new(checked(clip.Position.Ticks + delta)), clip.Lane + laneDelta);
        foreach (var clip in audio)
            State = State with { AudioClips = State.AudioClips.Add(clip with
            {
                Id = Id<AudioClip>.New(), Position = new(checked(clip.Position.Ticks + delta)), Lane = clip.Lane + laneDelta
            }) };
    }

    /// <summary>Detaches only this song clip. Source sound, resources, routes and containing processing remain shared.</summary>
    public Id<PatternPlacement> MakeArrangementVariation(Id<ArrangementClip> clipId, string name)
    {
        CheckActive();
        var clip = State.ArrangementClips.Single(c => c.Id == clipId);
        var source = State.Placements.Single(p => p.Id == clip.SourcePlacementId);
        var pattern = State.Patterns.Single(p => p.Id == source.PatternId);
        // Unknown state may embed identity references beyond the declared remapping contract.
        ProjectValidation.Require(clip.AdditionalData.IsEmpty && pattern.AdditionalData.IsEmpty &&
            pattern.Parts.All(p => p.AdditionalData.IsEmpty && p.Notes.All(n => n.AdditionalData.IsEmpty)) &&
            source.AdditionalData.IsEmpty && source.PartRelationships.All(p => p.AdditionalData.IsEmpty),
            "Arrangement.VariationUnsupported");
        var context = State.Contexts.SingleOrDefault(c => c.Id == source.ItemContextId);
        var attachment = State.GraphAttachments.SingleOrDefault(a => a.ContextId == source.ItemContextId);
        var graph = attachment is null ? null : State.Graphs.Single(g => g.Id == attachment.GraphId);
        ProjectValidation.Require(context is null || context.Extension is null && context.AdditionalData.IsEmpty,
            "Arrangement.VariationUnsupported");
        ProjectValidation.Require(graph is null || graph.AdditionalData.IsEmpty && attachment!.AdditionalData.IsEmpty &&
            attachment.Sources.All(s => s.PlacementId == source.Id && s.AdditionalData.IsEmpty) &&
            graph.Nodes.All(n => n.Extension is null && n.AdditionalData.IsEmpty && n.Ports.All(p => p.AdditionalData.IsEmpty) &&
                GraphBuiltIns.Descriptors.Any(d => d.Type == n.Type && d.StateVersion == n.StateVersion)) &&
            graph.Connections.All(c => c.AdditionalData.IsEmpty), "Arrangement.VariationUnsupported");
        Id<ProcessingContext>? item = context is null ? null : Id<ProcessingContext>.New();
        if (context is not null) State = State with { Contexts = State.Contexts.Add(context with { Id = item!.Value }) };
        var placement = source with { Id = Id<PatternPlacement>.New(), ItemContextId = item };
        State = State with { Placements = State.Placements.Add(placement) };
        if (graph is not null && attachment is not null)
        {
            var nodes = graph.Nodes.ToDictionary(n => n.Id, _ => Id<GraphNode>.New());
            var ports = graph.Nodes.SelectMany(n => n.Ports).ToDictionary(p => p.Id, _ => Id<GraphPort>.New());
            var cloned = graph with
            {
                Id = Id<GraphDefinition>.New(),
                Nodes = [.. graph.Nodes.Select(n => n with { Id = nodes[n.Id], Ports = [.. n.Ports.Select(p => p with { Id = ports[p.Id] })] })],
                Connections = [.. graph.Connections.Select(c => c with { Id = Id<GraphConnection>.New(),
                    FromNodeId = nodes[c.FromNodeId], ToNodeId = nodes[c.ToNodeId], FromPortId = ports[c.FromPortId], ToPortId = ports[c.ToPortId] })]
            };
            State = State with { Graphs = State.Graphs.Add(cloned), GraphAttachments = State.GraphAttachments.Add(attachment with
            {
                Id = Id<GraphAttachment>.New(), GraphId = cloned.Id, ContextId = item!.Value,
                OutputNodeId = attachment.OutputNodeId is { } output ? nodes[output] : null,
                Sources = [.. attachment.Sources.Select(s => s with { Id = Id<GraphSourceBinding>.New(), NodeId = nodes[s.NodeId], PlacementId = placement.Id })]
            }) };
        }
        MakePatternVariation(placement.Id, name);
        State = State with { ArrangementClips = Replace(State.ArrangementClips, c => c.Id == clipId,
            c => c with { SourcePlacementId = placement.Id }) };
        return placement.Id;
    }
}

/// <summary>Musical geometry shared by the editor and canonical transactions.</summary>
public static class PlaylistEditing
{
    public static long EndTicks(ProjectState state, ArrangementClip clip) => checked(clip.Position.Ticks +
        state.Patterns.Single(p => p.Id == state.Placements.Single(s => s.Id == clip.SourcePlacementId).PatternId).Length.Ticks * clip.Repeats);

    public static PlaylistBoundaryConflict? FindBoundaryConflict(ProjectState state, IReadOnlyList<long> boundaries)
    {
        foreach (var c in state.ArrangementClips)
            if (boundaries.Any(b => c.Position.Ticks < b && EndTicks(state, c) > b))
                return new(c.Id, null, state.Patterns.Single(p => p.Id == state.Placements.Single(s => s.Id == c.SourcePlacementId).PatternId).Name,
                    c.Lane, c.Position.Ticks);
        foreach (var c in state.AudioClips)
            if (boundaries.Any(b => c.Position.Ticks < b && AudioClipSource.EndTicks(state, c) > b))
                return new(null, c.Id, state.Sounds.Single(s => s.Id == c.SoundId).Name, c.Lane, c.Position.Ticks);
        return null;
    }
}

public sealed record PlaylistBoundaryConflict(Id<ArrangementClip>? PatternClip, Id<AudioClip>? AudioClip,
    string Name, int Lane, long Position);
