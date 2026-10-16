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
        .Concat(state.AudioClips.Select(clip => AudioClipSource.EndTicks(state, clip))).DefaultIfEmpty(0).Max();

    public static string? Refusal(ProjectState state)
    {
        if (state.ArrangementClips.IsEmpty && state.AudioClips.IsEmpty) return "Arrangement.Empty";
        if (state.AudioClips.Any(c => !c.AdditionalData.IsEmpty)) return "Arrangement.Unsupported";
        foreach (var clip in state.AudioClips)
        {
            var sound = state.Sounds.Single(s => s.Id == clip.SoundId);
            if (sound.Extension is not null || !sound.AdditionalData.IsEmpty ||
                sound.Parameters.Count != 3 ||
                !sound.Parameters.ContainsKey("channels")) return "Arrangement.Unsupported";
        }

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
        using var decodeBatch = ProjectMedia.BeginDecodeBatch();
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
                offsets.Add(id, (sources.Count, bufferCount));
                sources.AddRange(plan.LeaseSources());
                decodedBytes = sources.DistinctBy(source => source.Samples).Sum(source => source.Bytes);
                if (decodedBytes > SamplerPreparation.MaximumDecodedBytes)
                    throw new GraphPreparationException(GraphPreparationReasons.Budget);
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
            var audioSources = new Dictionary<Id<ResourceDescriptor>, int>();
            var stretches = new Dictionary<(Id<ResourceDescriptor>, long Offset, long Length, decimal Ratio), int>();
            long audioBytes = 0, stretchBytes = 0;
            foreach (var group in state.AudioClips.Where(c => IsAudible(state, c.Lane)).GroupBy(c => c.SoundId))
            {
                cancellation.ThrowIfCancellationRequested();
                var sound = state.Sounds.Single(s => s.Id == group.Key);
                int rate = AudioClipSource.Rate(sound);
                if (!audioSources.TryGetValue(sound.ResourceIds[0], out int sourceIndex))
                {
                    long expected = AudioClipSource.Frames(sound) * (long)sound.Parameters["channels"] * sizeof(float);
                    if (audioBytes + expected > AudioMedia.MaximumDecodedBytes)
                        throw new GraphPreparationException(GraphPreparationReasons.Budget);
                    using var decoded = AudioMedia.Decode(document, sound.ResourceIds[0]);
                    sourceIndex = sources.Count;
                    sources.Add(decoded.Lease());
                    audioSources.Add(sound.ResourceIds[0], sourceIndex);
                    audioBytes += expected;
                }
                // One immutable resource can serve several sound definitions. Validate each definition,
                // including cache hits, before using its clock/range to schedule or stretch shared PCM.
                var source = sources[sourceIndex];
                if (source.Rate != rate || source.Frames != AudioClipSource.Frames(sound) ||
                    source.Channels != sound.Parameters["channels"])
                    throw new GraphPreparationException(GraphPreparationReasons.Media);

                int buffer = bufferCount++;
                var node = new Id<GraphNode>(sound.Id.Value);
                buffers.Add(node, buffer);
                outputs.Add(buffer);
                operations.Add(new(node, GraphOperationKind.Source, buffer, [], -1));
                contributions.Add(new(default, default, default, node, sound.Id));
                var continuations = group.Where(c => c.Repeats == 1 && c.EnvelopeLengthFrames > 0)
                    .ToLookup(c => (c.OffsetFrames, c.EnvelopeOffsetFrames));
                foreach (var clip in group)
                {
                    cancellation.ThrowIfCancellationRequested();
                    int clipSource = sourceIndex;
                    long playbackOffset = clip.OffsetFrames, playbackLength = clip.LengthFrames;
                    long envelopeOffset = clip.EnvelopeOffsetFrames, envelopeLength = AudioClipSource.EnvelopeLength(clip);
                    long fadeIn = clip.FadeInFrames, fadeOut = clip.FadeOutFrames;
                    if (clip.StretchRatio != 1)
                    {
                        var key = (sound.ResourceIds[0], clip.OffsetFrames - clip.EnvelopeOffsetFrames,
                            AudioClipSource.EnvelopeLength(clip), clip.StretchRatio);
                        if (!stretches.TryGetValue(key, out clipSource))
                        {
                            long bytes = AudioTimeStretch.RequiredBytes(key.Item3, rate,
                                sources[sourceIndex].Channels, clip.StretchRatio);
                            if (stretchBytes + bytes > AudioTimeStretch.MaximumDerivedBytes ||
                                audioBytes + bytes > AudioMedia.MaximumDecodedBytes)
                                throw new GraphPreparationException(GraphPreparationReasons.Budget);
                            var stretched = AudioTimeStretch.Prepare(sources[sourceIndex], key.Item2, key.Item3,
                                clip.StretchRatio, cancellation);
                            clipSource = sources.Count;
                            sources.Add(stretched);
                            stretches.Add(key, clipSource);
                            stretchBytes += bytes;
                            audioBytes += bytes;
                        }
                        playbackOffset = AudioClipSource.StretchedFrames(clip.EnvelopeOffsetFrames, clip.StretchRatio);
                        playbackLength = AudioClipSource.PlaybackLength(clip);
                        envelopeOffset = playbackOffset;
                        envelopeLength = AudioClipSource.StretchedFrames(envelopeLength, clip.StretchRatio);
                        fadeIn = AudioClipSource.StretchedFrames(fadeIn, clip.StretchRatio);
                        fadeOut = AudioClipSource.StretchedFrames(fadeOut, clip.StretchRatio);
                    }
                    if (events.Count + clip.Repeats * 2L + 1 > SamplerPreparation.MaximumEvents)
                        throw new GraphPreparationException(GraphPreparationReasons.Budget);
                    var bounds = AudioClipSource.ExecutionBounds(clip, state.Settings.Tempo, rate, sampleRate);
                    long start = bounds.Start;
                    bool joined = clip.Repeats == 1 && clip.EnvelopeLengthFrames > 0 &&
                                  continuations[
                                          (clip.OffsetFrames + clip.LengthFrames,
                                              clip.EnvelopeOffsetFrames + clip.LengthFrames)]
                                      .Any(next =>
                                          next.GainDb == clip.GainDb && next.FadeInFrames == clip.FadeInFrames &&
                                          next.FadeOutFrames == clip.FadeOutFrames &&
                                          next.EnvelopeLengthFrames == clip.EnvelopeLengthFrames &&
                                          next.StretchRatio == clip.StretchRatio &&
                                          AudioClipSource.ExecutionBounds(next, state.Settings.Tempo, rate, sampleRate)
                                              .Start == bounds.End);
                    for (int repeat = 0; repeat < clip.Repeats; repeat++)
                    {
                        long on = start + AudioClipSource.ConvertFrames(playbackLength * repeat, rate, sampleRate);
                        long off = bounds.Origin is not null
                            ? bounds.End
                            : start +
                              AudioClipSource.ConvertFrames(playbackLength * (repeat + 1), rate, sampleRate);
                        if (on == off)
                        {
                            zero++;
                            continue;
                        }

                        int index = notes.Count;
                        var occurrence =
                            new ExecutionOccurrence(default, default, default, repeat, AudioClipId: clip.Id);
                        notes.Add(new(occurrence, clipSource, (double)rate / sampleRate,
                            clip.GainDb == -90 ? 0 : (float)Math.Pow(10, (double)clip.GainDb / 20), on, off, 0, buffer,
                            SourceOffset: playbackOffset, SourceLength: playbackLength,
                            FadeIn: fadeIn, FadeOut: fadeOut,
                            EnvelopeOffset: envelopeOffset,
                            EnvelopeLength: envelopeLength,
                            SourceClockStart: bounds.Origin, SmoothSourceEnd: !joined));
                        events.Add(new(on, ConstantTempoConversion.FromFrames(on, state.Settings.Tempo, sampleRate),
                            ExecutionEventKind.NoteOn, occurrence, index));
                        events.Add(new(off, ConstantTempoConversion.FromFrames(off, state.Settings.Tempo, sampleRate),
                            ExecutionEventKind.NoteOff, occurrence, index));
                    }
                }
            }

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

            long patternEnd = state.ArrangementClips.Select(c => checked(c.Position.Ticks +
                                                                         state.Patterns.Single(p =>
                                                                                 p.Id == state.Placements.Single(s =>
                                                                                         s.Id == c.SourcePlacementId)
                                                                                     .PatternId)
                                                                             .Length.Ticks * c.Repeats))
                .DefaultIfEmpty(0)
                .Max();
            long last = checked(ConstantTempoConversion.ToFrames(new(patternEnd), state.Settings.Tempo, sampleRate) +
                                release);
            last = Math.Max(last,
                state.AudioClips.Select(c =>
                        AudioClipSource.ExecutionBounds(c, state.Settings.Tempo,
                            AudioClipSource.Rate(state.Sounds.Single(s => s.Id == c.SoundId)), sampleRate).End)
                    .DefaultIfEmpty(0).Max());
            if (notes.Any(n => n.OneShot))
                last = Math.Max(last,
                    notes.Where(n => n.OneShot).Max(n => checked(n.StartFrame + sources[n.SourceIndex].Frames)));
            events.RemoveAll(item => item.Frame > last ||
                                     item.Frame == last && (item.Kind != ExecutionEventKind.NoteOff || state.Mixer is null));
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
            long sourceEnd = last;
            if (combined.Mixer is { } mixer)
            {
                last = checked(last + mixer.TailFrames);
                // Source NoteOffs retain their original boundaries; only the transport Stop moves.
                sorted = [.. sorted.Where(e => e.Kind != ExecutionEventKind.Stop),
                    new(last, new(endTicks), ExecutionEventKind.Stop, null, -1)];
            }
            var result = new PreparedSampler(document.Current.Revision, sampleRate, channels, VoiceCapacity, 0, last,
                    zero, notes.ToImmutable(), sorted, [.. sources], combined)
                { CanonicalBasis = state, IsArrangement = true, SourceEndFrame = sourceEnd };
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
