// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

/// <summary>Finite song execution through the same PCM scheduler and item graphs as Rack playback.</summary>
public static class ArrangementPreparation
{
    public const int VoiceCapacity = GraphPreparation.MaximumVoices;

    public static bool IsAudible(ProjectState state, int lane)
    {
        var track = state.ArrangementTracks.SingleOrDefault(track => track.Lane == lane);
        return track?.Muted != true && (!state.ArrangementTracks.Any(t => t.Solo) || track?.Solo == true);
    }

    public static long EndTicks(ProjectState state) => state.ArrangementClips.Select(clip =>
            checked(clip.Position.Ticks + state.Patterns.Single(pattern => pattern.Id ==
                                                                           state.Placements.Single(p =>
                                                                                   p.Id == clip.SourcePlacementId)
                                                                               .PatternId).Length.Ticks * clip.Repeats))
        .DefaultIfEmpty(0).Max();

    public static string? Refusal(ProjectState state)
    {
        if (state.ArrangementClips.IsEmpty) return "Arrangement.Empty";
        if (state.ArrangementClips.Any(clip => !clip.AdditionalData.IsEmpty)) return "Arrangement.Unsupported";
        if (state.ArrangementTracks.Any(track => !track.AdditionalData.IsEmpty)) return "Arrangement.Unsupported";
        var reports = GraphDiagnostics.Inspect(state).ToDictionary(report => report.GraphId);
        foreach (var id in state.ArrangementClips.Select(clip => clip.SourcePlacementId).Distinct())
        {
            var placement = state.Placements.Single(p => p.Id == id);
            var attachment = state.GraphAttachments.SingleOrDefault(a => a.ContextId == placement.ItemContextId);
            if (attachment is null) return "Arrangement.Unsupported";
            if (!reports[attachment.GraphId].IsEligibleForPreparation &&
                !GraphPreparation.IsSilentChannelGraph(state, attachment))
                return reports[attachment.GraphId].Diagnostics[0].Reason;
        }

        return null;
    }

    public static PreparedSampler Prepare(ProjectDocument document, int sampleRate, int channels,
        int maximumPacketFrames, CancellationToken cancellation = default)
    {
        document.CheckAvailable();
        var state = document.Current.State;
        if (Refusal(state) is { } refusal) throw new GraphPreparationException(refusal);
        var plans = new Dictionary<Id<PatternPlacement>, PreparedSampler>();
        var sources = new List<PcmLease>();
        try
        {
            var notes = ImmutableArray.CreateBuilder<PreparedNote>();
            var events = new List<PreparedEvent>();
            var operations = new List<GraphOperation>();
            var coefficients = new List<float>();
            var buffers = new Dictionary<Id<GraphNode>, int>();
            var contributions = ImmutableArray.CreateBuilder<AudioContribution>();
            var offsets = new Dictionary<Id<PatternPlacement>, (int Source, int Buffer)>();
            var outputs = new List<int>();
            int bufferCount = 0, zero = 0, release = 0, decodedBytes = 0;
            foreach (var id in state.ArrangementClips.Select(c => c.SourcePlacementId).Distinct()
                         .OrderBy(id => id.Value))
            {
                cancellation.ThrowIfCancellationRequested();
                var placement = state.Placements.Single(p => p.Id == id);
                var attachment = state.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId);
                var plan = GraphPreparation.Prepare(document, attachment.Id, sampleRate, channels, maximumPacketFrames,
                    voiceCapacity: VoiceCapacity, cancellationToken: cancellation);
                plans.Add(id, plan);
                // Relocating sub-frame note boundaries can change audibility. Do not reuse an incomplete table.
                if (plan.ZeroFrameNotes != 0) throw new GraphPreparationException(GraphPreparationReasons.Source);
                decodedBytes = checked(decodedBytes + plan.DecodedBytes);
                if (decodedBytes > SamplerPreparation.MaximumDecodedBytes)
                    throw new GraphPreparationException(GraphPreparationReasons.Budget);
                offsets.Add(id, (sources.Count, bufferCount));
                sources.AddRange(plan.LeaseSources());
                var graph = plan.Graph!;
                foreach (var operation in graph.Operations)
                    operations.Add(operation with
                    {
                        Buffer = operation.Buffer + bufferCount,
                        Inputs = [.. operation.Inputs.Select(input => input + bufferCount)],
                        Coefficient = operation.Coefficient < 0 ? -1 : operation.Coefficient + coefficients.Count
                    });
                foreach (var pair in graph.NodeBuffers) buffers.TryAdd(pair.Key, pair.Value + bufferCount);
                coefficients.AddRange(graph.Coefficients);
                contributions.AddRange(graph.Contributions);
                outputs.Add(graph.OutputBuffer + bufferCount);
                bufferCount += graph.BufferCount;
            }

            long endTicks = EndTicks(state);
            foreach (var clip in state.ArrangementClips.OrderBy(c => c.Position.Ticks).ThenBy(c => c.Id.Value))
            {
                if (!IsAudible(state, clip.Lane)) continue;
                var placement = state.Placements.Single(p => p.Id == clip.SourcePlacementId);
                var pattern = state.Patterns.Single(p => p.Id == placement.PatternId);
                var plan = plans[placement.Id];
                if ((long)events.Count + (long)plan.Notes.Length * clip.Repeats * 2 + 1 >
                    SamplerPreparation.MaximumEvents)
                    throw new GraphPreparationException(GraphPreparationReasons.Budget);
                var offset = offsets[placement.Id];
                for (int iteration = 0; iteration < clip.Repeats; iteration++)
                for (int n = 0; n < plan.Notes.Length; n++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var original = plan.Notes[n];
                    var note = pattern.Parts.Single(p => p.Id == original.Occurrence.PartId).Notes
                        .Single(note => note.Id == original.Occurrence.NoteId);
                    long ticks = checked(clip.Position.Ticks + iteration * pattern.Length.Ticks + note.Position.Ticks);
                    var start = new MusicalPosition(ticks);
                    var end = start.Add(note.Duration);
                    long on = ConstantTempoConversion.ToFrames(start, state.Settings.Tempo, sampleRate);
                    long off = ConstantTempoConversion.ToFrames(end, state.Settings.Tempo, sampleRate);
                    if (on == off)
                    {
                        zero++;
                        continue;
                    }

                    var occurrence = original.Occurrence with { ClipId = clip.Id, Iteration = iteration };
                    int index = notes.Count;
                    notes.Add(original with
                    {
                        Occurrence = occurrence, StartFrame = on, EndFrame = off,
                        SourceIndex = original.SourceIndex + offset.Source,
                        ContributionIndex = original.ContributionIndex + offset.Buffer
                    });
                    events.Add(new(on, start, ExecutionEventKind.NoteOn, occurrence, index));
                    events.Add(new(off, end, ExecutionEventKind.NoteOff, occurrence, index));
                    release = Math.Max(release, original.ReleaseFrames);
                }
            }

            long last = checked(ConstantTempoConversion.ToFrames(new(endTicks), state.Settings.Tempo, sampleRate) +
                                release);
            if (notes.Any(n => n.OneShot))
                last = Math.Max(last,
                    notes.Where(n => n.OneShot).Max(n => checked(n.StartFrame + sources[n.SourceIndex].Frames)));
            events.RemoveAll(item => item.Frame >= last);
            events.Add(new(last, new(endTicks), ExecutionEventKind.Stop, null, -1));
            var sorted = events.OrderBy(e => e.Frame).ThenBy(e => e.Kind).ThenBy(e => e.Position.Ticks)
                .ThenBy(e => e.Occurrence?.ClipId?.Value).ThenBy(e => e.Occurrence?.PartId.Value)
                .ThenBy(e => e.Occurrence?.NoteId.Value).ThenBy(e => e.Occurrence?.Iteration).ToImmutableArray();
            DrumChoke.Prepare(notes, sorted, state.Settings.Tempo, sampleRate);
            SamplerPreparation.ValidateCapacity(sorted, notes, sources, VoiceCapacity, last);
            int outputBuffer = bufferCount++;
            operations.Add(new(default, GraphOperationKind.Mix, outputBuffer, [.. outputs], -1));
            int scratch = checked(bufferCount * maximumPacketFrames * channels * sizeof(float) +
                                  operations.Count * 256 + VoiceCapacity * 128);
            if (scratch > GraphPreparation.MaximumScratchBytes)
                throw new GraphPreparationException(GraphPreparationReasons.Budget);
            var combined = new PreparedGraph
            {
                AttachmentId = default, PlacementId = default, Contributions = contributions.ToImmutable(),
                NodeBuffers = buffers, Operations = [.. operations], Coefficients = [.. coefficients],
                OutputBuffer = outputBuffer,
                BufferCount = bufferCount, MaximumPacketFrames = maximumPacketFrames, ScratchBytes = scratch
            };
            combined.Mixer = MixerPreparation.Compile(state, combined, sampleRate);
            var result = new PreparedSampler(document.Current.Revision, sampleRate, channels, VoiceCapacity, 0, last,
                    zero, notes.ToImmutable(), sorted, [.. sources], combined)
                { CanonicalBasis = state, IsArrangement = true };
            sources.Clear();
            try
            {
                GraphPreparation.Preflight(result, maximumPacketFrames);
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }
        catch (Exception error) when (error is NotSupportedException or OverflowException)
        {
            throw new GraphPreparationException(GraphPreparationReasons.Budget, error);
        }
        finally
        {
            foreach (var source in sources) source.Dispose();
            foreach (var plan in plans.Values) plan.Dispose();
        }
    }
}
