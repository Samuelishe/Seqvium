// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal readonly record struct ArrangementEditBasis(Guid Lifecycle, long Generation)
{
    internal static ArrangementEditBasis From(WorkflowState state) => new(state.Document.LifecycleId, state.Generation);
}

internal sealed partial class DesktopWorkflow
{
    private long _songCursor, _loopStart, _loopEnd;
    private bool _songLoopEnabled;

    internal Task<bool> SeekSongAsync(MusicalPosition position) => Command("Workflow.EditFailed", () =>
    {
        _songCursor = Math.Clamp(position.Ticks, 0, ArrangementPreparation.EndTicks(_document.Current.State));
        ApplySongTransport(seek: true);
        return Task.FromResult(true);
    });

    internal Task<bool> SetSongLoopAsync(MusicalPosition start, MusicalPosition end, bool enabled)
        => Command("Workflow.EditFailed", () =>
    {
        long songEnd = ArrangementPreparation.EndTicks(_document.Current.State);
        long minimum = MusicalPosition.TicksPerQuarter / 4;
        _loopStart = Math.Clamp(start.Ticks, 0, Math.Max(0, songEnd - minimum));
        _loopEnd = Math.Clamp(end.Ticks, _loopStart, songEnd);
        _songLoopEnabled = enabled && _loopEnd - _loopStart >= minimum;
        ApplySongTransport();
        return Task.FromResult(true);
    });

    private void ApplySongTransport(bool seek = false)
    {
        if (_output is null) return;
        var tempo = _document.Current.State.Settings.Tempo;
        long Frame(long ticks) => ConstantTempoConversion.ToFrames(new(ticks), tempo, _output.Sampler.SampleRate);
        _output.Sampler.SetSongLoop(Frame(_loopStart), Frame(_loopEnd), _songLoopEnabled);
        if (seek) _output.Sampler.SeekSong(Frame(_songCursor));
    }

    internal Task<bool> EditArrangementSelectionAsync(ArrangementEditBasis basis, IReadOnlyList<Id<ArrangementClip>> selection,
        long tickDelta = 0, int laneDelta = 0, bool copy = false, bool delete = false, bool duplicate = false)
    {
        var ids = selection.Distinct().ToArray();
        return Command("Workflow.EditFailed", () =>
        {
            ValidateArrangementBasis(basis);
            var state = _document.Current.State;
            var clips = ids.Select(id => state.ArrangementClips.Single(c => c.Id == id)).ToArray();
            if (clips.Length == 0) return Task.FromResult(false);
            if (duplicate)
            {
                long end = clips.Max(c => c.Position.Ticks + state.Patterns.Single(p => p.Id ==
                    state.Placements.Single(s => s.Id == c.SourcePlacementId).PatternId).Length.Ticks * c.Repeats);
                tickDelta = end - clips.Min(c => c.Position.Ticks); copy = true;
            }
            long delta = Math.Clamp(tickDelta, -clips.Min(c => c.Position.Ticks),
                4095L * state.Settings.Meter.TicksPerBar - clips.Max(c => c.Position.Ticks));
            int lanes = Math.Clamp(laneDelta, -clips.Min(c => c.Lane), 31 - clips.Max(c => c.Lane));
            return Task.FromResult(_document.Edit(delete ? "Delete song clips" : copy ? "Copy song clips" : "Move song clips", edit =>
            {
                foreach (var clip in clips)
                    if (delete) edit.DeleteArrangementClip(clip.Id);
                    else if (copy) edit.AddArrangementClip(clip.SourcePlacementId, new(clip.Position.Ticks + delta), clip.Lane + lanes, clip.Repeats);
                    else edit.UpdateArrangementClip(clip.Id, new(clip.Position.Ticks + delta), clip.Lane + lanes, clip.Repeats);
            }));
        });
    }

    private void ValidateArrangementBasis(ArrangementEditBasis basis)
    {
        if (_closed || _document.LifecycleId != basis.Lifecycle || _document.Generation != basis.Generation)
            throw new InvalidOperationException("Beat.Stale");
    }

    internal Task<bool> SetSongModeAsync(bool song) => Command("Workflow.EditFailed", () =>
    {
        if (_songMode == song) return Task.FromResult(false);
        _playGeneration++; _coordinator?.Stop();
        _songMode = song;
        if (!song && _selected is { } target) _coordinator?.SelectTarget(target);
        _coordinator?.SelectArrangement(song);
        _output?.Sampler.SetLooping(!song);
        _status = "Workflow.Ready";
        return Task.FromResult(true);
    });

    internal Task<bool> AddArrangementClipAsync(ArrangementEditBasis basis, Id<GraphAttachment> source,
        MusicalPosition position, int lane, int repeats = 1) => Command("Workflow.EditFailed", () =>
    {
        ValidateArrangementBasis(basis);
        var state = _document.Current.State;
        var attachment = state.GraphAttachments.Single(a => a.Id == source);
        var placement = state.Placements.Single(p => p.ItemContextId == attachment.ContextId);
        return Task.FromResult(_document.Edit("Place Pattern in song", edit =>
            edit.AddArrangementClip(placement.Id, position, lane, repeats)));
    });

    internal Task<bool> ConfigureArrangementTrackAsync(ArrangementEditBasis basis, int lane, string name,
        int colorIndex, bool muted, bool solo) => Command("Workflow.EditFailed", () =>
    {
        ValidateArrangementBasis(basis);
        return Task.FromResult(_document.Edit("Configure Playlist track", edit => edit.SetArrangementTrack(lane, name, colorIndex, muted, solo)));
    });

    internal Task<bool> CopyArrangementClipAsync(ArrangementEditBasis basis, Id<ArrangementClip> id,
        MusicalPosition position, int lane) => Command("Workflow.EditFailed", () =>
    {
        ValidateArrangementBasis(basis);
        var clip = _document.Current.State.ArrangementClips.Single(c => c.Id == id);
        return Task.FromResult(_document.Edit("Copy song clip", edit => edit.AddArrangementClip(clip.SourcePlacementId, position, lane, clip.Repeats)));
    });

    internal Task<bool> UpdateArrangementClipAsync(ArrangementEditBasis basis, Id<ArrangementClip> id,
        MusicalPosition position, int lane, int repeats) => Command("Workflow.EditFailed", () =>
    {
        ValidateArrangementBasis(basis);
        return Task.FromResult(_document.Edit("Edit song clip", edit => edit.UpdateArrangementClip(id, position, lane, repeats)));
    });

    internal Task<bool> DuplicateArrangementClipAsync(ArrangementEditBasis basis, Id<ArrangementClip> id)
        => Command("Workflow.EditFailed", () =>
    {
        ValidateArrangementBasis(basis);
        var state = _document.Current.State;
        var clip = state.ArrangementClips.Single(c => c.Id == id);
        var pattern = state.Patterns.Single(p => p.Id == state.Placements.Single(p => p.Id == clip.SourcePlacementId).PatternId);
        return Task.FromResult(_document.Edit("Duplicate song clip", edit => edit.AddArrangementClip(clip.SourcePlacementId,
            new(checked(clip.Position.Ticks + pattern.Length.Ticks * clip.Repeats)), clip.Lane, clip.Repeats)));
    });

    internal Task<bool> DeleteArrangementClipAsync(ArrangementEditBasis basis, Id<ArrangementClip> id)
        => Command("Workflow.EditFailed", () =>
    {
        ValidateArrangementBasis(basis);
        return Task.FromResult(_document.Edit("Delete song clip", edit => edit.DeleteArrangementClip(id)));
    });

    internal Task<bool> ExportSongAsync(ArrangementEditBasis basis, string path, CancellationToken cancellation = default)
        => Command("Workflow.ExportFailed", async () =>
    {
        ValidateArrangementBasis(basis);
        _status = "Workflow.Exporting"; Publish();
        var result = await PatternWaveExport.ExportArrangementAsync(_document, path, cancellation);
        _status = result.ClippedSamples > 0 ? "Workflow.ExportClipped" : "Workflow.Exported";
        return true;
    });
}
