// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public static class PcmSampler
{
    public const string Algorithm = "core.pcm-sampler.linear-v1";

    public static void ValidateConfiguration(decimal rootPitch, decimal releaseMilliseconds)
    {
        if (rootPitch is < 0 or > 127 || releaseMilliseconds is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(rootPitch), "Root must be 0–127; release must be 0–1000 ms.");
    }

    public static double Step(int sourceRate, int executionRate, decimal rootPitch, decimal notePitch)
    {
        ValidateConfiguration(rootPitch, 0);
        if (sourceRate is < 8000 or > 192000 || executionRate is not (44100 or 48000) ||
            notePitch is < 0 or > 127)
            throw new NotSupportedException(
                "Sampler supports 8–192 kHz sources, 44.1/48 kHz output and pitches within MIDI 0–127.");
        return (double)sourceRate / executionRate * Math.Pow(2, (double)(notePitch - rootPitch) / 12);
    }
}

public readonly record struct ExecutionOccurrence(
    Id<Pattern> PatternId,
    Id<MusicalPart> PartId,
    Id<NoteEvent> NoteId,
    int Iteration,
    Id<PatternPlacement>? PlacementId = null,
    Id<ArrangementClip>? ClipId = null);

public enum ExecutionEventKind
{
    Stop,
    NoteOff,
    NoteOn
}

public sealed record PreparedEvent(
    long Frame,
    MusicalPosition Position,
    ExecutionEventKind Kind,
    ExecutionOccurrence? Occurrence,
    int NoteIndex);

internal sealed record PreparedNote(
    ExecutionOccurrence Occurrence,
    int SourceIndex,
    double Step,
    float Gain,
    long StartFrame,
    long EndFrame,
    int ReleaseFrames,
    int ContributionIndex = 0);

/// <summary>Frozen single-Pattern execution, bounded repeats and source leases. Derived state, never persistent musical truth.</summary>
public sealed class PreparedSampler : IDisposable
{
    private PcmLease[]? _sources;
    internal ImmutableArray<PreparedNote> Notes { get; }
    public ImmutableArray<PreparedEvent> Events { get; }
    public Guid Revision { get; }
    public int SampleRate { get; }
    public int Channels { get; }
    public int VoiceCapacity { get; }
    public int ZeroFrameNotes { get; }
    public long StartFrame { get; }
    public long EndFrame { get; }
    internal PreparedGraph? Graph { get; }
    public int DecodedBytes { get; }
    public int ScratchBytes => Graph?.ScratchBytes ?? 0;
    public ImmutableArray<AudioContribution> Contributions => Graph?.Contributions ?? [];
    public Id<GraphAttachment>? GraphAttachmentId => Graph?.AttachmentId;
    public long PacketWork { get; internal set; }
    public int MaximumPacketEvents { get; internal set; }
    internal LoopClock? Loop { get; set; }
    internal ProjectState? CanonicalBasis { get; set; }
    internal bool IsArrangement { get; init; }

    internal PreparedSampler(Guid revision, int rate, int channels, int capacity, long start, long end, int zero,
        ImmutableArray<PreparedNote> notes, ImmutableArray<PreparedEvent> events, PcmLease[] sources,
        PreparedGraph? graph = null)
    {
        Revision = revision;
        SampleRate = rate;
        Channels = channels;
        VoiceCapacity = capacity;
        StartFrame = start;
        EndFrame = end;
        ZeroFrameNotes = zero;
        Notes = notes;
        Events = events;
        _sources = sources;
        Graph = graph;
        DecodedBytes = sources.Sum(source => source.Bytes);
    }

    internal PcmLease[] LeaseSources() => (_sources ?? throw new ObjectDisposedException(nameof(PreparedSampler)))
        .Select(source => source.Lease()).ToArray();

    public OfflineSampler CreateExecution(bool smoothBoundaries = false)
    {
        var execution = new OfflineSampler(this);
        execution.SetBoundarySmoothing(smoothBoundaries);
        return execution;
    }

    public void Dispose()
    {
        if (_sources is { } sources)
            foreach (var source in sources)
                source.Dispose();
        _sources = null;
    }
}

public static class SamplerPreparation
{
    /// <summary>Finite solo keyboard audition at the configured source pitch; no canonical events are created.</summary>
    public static PreparedSampler PrepareNoteAudition(DecodedPcm source, int sampleRate, int channels,
        decimal rootPitch, decimal pitch, decimal intensity)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (channels is not (1 or 2) || intensity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity));
        double step = PcmSampler.Step(source.SampleRate, sampleRate, rootPitch, pitch);
        long end = Math.Min((long)Math.Ceiling(source.Frames / step), sampleRate * 2L);
        return new(Guid.Empty, sampleRate, channels, 1, 0, end, 0,
            [new(default, 0, step, (float)intensity, 0, end, 0)],
            [
                new(0, default, ExecutionEventKind.NoteOn, default(ExecutionOccurrence), 0),
                new(end, default, ExecutionEventKind.Stop, null, -1)
            ], [source.Lease()]);
    }

    /// <summary>Ephemeral raw source at native pitch. Empty revision/occurrence denotes no canonical music;
    /// leases are independent of source. No document, resource, Pattern or Undo state is manufactured.</summary>
    public static PreparedSampler PrepareOneShot(DecodedPcm source, int sampleRate, int channels)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (sampleRate is not (44100 or 48000) || channels is not (1 or 2))
            throw new NotSupportedException("One-shot supports 44.1/48 kHz mono/stereo execution.");
        double step = PcmSampler.Step(source.SampleRate, sampleRate, 60, 60);
        long end = (long)Math.Ceiling(source.Frames / step);
        var lease = source.Lease();
        return new(Guid.Empty, sampleRate, channels, 1, 0, end, 0,
            [new(default, 0, step, 1, 0, end, 0)],
            [
                new(0, default, ExecutionEventKind.NoteOn, default(ExecutionOccurrence), 0),
                new(end, default, ExecutionEventKind.Stop, null, -1)
            ], [lease]);
    }

    public const int MaximumEvents = 100_001;
    public const int MaximumDecodedBytes = 128 * 1024 * 1024;

    /// <summary>Loop occurrences use absolute tick endpoints; no rounded loop-length accumulation. Stop is a hard transport boundary.</summary>
    public static PreparedSampler PreparePattern(ProjectDocument document, Id<Pattern> patternId, int sampleRate,
        int channels = 2, int voiceCapacity = 8, MusicalPosition start = default, int repeats = 1,
        MusicalPosition? stop = null) =>
        PreparePatternCore(document, patternId, sampleRate, channels, voiceCapacity, start, repeats, stop);

    internal static PreparedSampler PreparePatternCore(ProjectDocument document, Id<Pattern> patternId, int sampleRate,
        int channels, int voiceCapacity, MusicalPosition start, int repeats, MusicalPosition? stop,
        PreparedGraph? graph = null, CancellationToken cancellationToken = default)
    {
        document.CheckAvailable();
        if (sampleRate is not (44100 or 48000) || channels is not (1 or 2) || voiceCapacity < 1 ||
            voiceCapacity > (graph is null ? 8 : GraphPreparation.MaximumVoices) ||
            repeats is < 1 or > 1024)
            throw new NotSupportedException(
                "Sampler supports 44100/48000 Hz, mono/stereo, 1–8 raw or 1–32 graph voices, 1–1024 repeats.");
        var snapshot = document.Current;
        var state = snapshot.State;
        var pattern = state.Patterns.SingleOrDefault(item => item.Id == patternId)
                      ?? throw new InvalidOperationException("Pattern is unavailable.");
        long musicalEnd = checked(start.Ticks + checked(pattern.Length.Ticks * repeats));
        if (stop is { } stopPosition && (stopPosition.Ticks < start.Ticks || stopPosition.Ticks > musicalEnd))
            throw new ArgumentOutOfRangeException(nameof(stop));
        var ordered = pattern.OrderedNotes().ToArray();
        if ((long)ordered.Length * repeats * 2 + 1 > MaximumEvents)
            throw new NotSupportedException("Prepared event capacity exceeded.");
        var notes = ImmutableArray.CreateBuilder<PreparedNote>();
        var events = new List<PreparedEvent>();
        var sources = new List<PcmLease>();
        var resourceIndices = new Dictionary<Id<ResourceDescriptor>, int>();
        int decodedBytes = 0, zero = 0, maximumRelease = 0;
        try
        {
            foreach (var part in pattern.Parts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sound = state.Sounds.Single(item => item.Id == part.SoundId);
                if (sound.Algorithm != PcmSampler.Algorithm || sound.Extension is not null ||
                    sound.ResourceIds.Length != 1 ||
                    sound.Parameters.Count != 2 || !sound.Parameters.ContainsKey("rootPitch") ||
                    !sound.Parameters.ContainsKey("releaseMilliseconds") ||
                    !sound.AdditionalData.IsEmpty || !part.AdditionalData.IsEmpty)
                    throw new NotSupportedException(
                        "Offline sampler requires one core PCM resource, explicit root/release and understood sound/part dependencies.");
                PcmSampler.ValidateConfiguration(sound.Parameters["rootPitch"],
                    sound.Parameters["releaseMilliseconds"]);
                var resource = sound.ResourceIds[0];
                if (!resourceIndices.ContainsKey(resource))
                {
                    using var decoded = ProjectMedia.Decode(document, resource);
                    var lease = decoded.Lease();
                    if (decodedBytes > MaximumDecodedBytes - lease.Bytes)
                    {
                        lease.Dispose();
                        throw new NotSupportedException("Decoded resource budget exceeded.");
                    }

                    decodedBytes += lease.Bytes;
                    resourceIndices.Add(resource, sources.Count);
                    sources.Add(lease);
                }
            }

            if (!pattern.AdditionalData.IsEmpty || graph is null && state.Placements.Any(placement =>
                    placement.PatternId == patternId &&
                    (placement.ItemContextId is not null || placement.ContainingContextId is not null ||
                     placement.RouteId is not null ||
                     placement.PartRelationships.Any(relation =>
                         relation.RouteId is not null || relation.SharedPerformanceKey is not null))))
                throw new NotSupportedException(
                    "Processing/route/performance or unknown Pattern dependencies require later execution support.");
            for (int iteration = 0; iteration < repeats; iteration++)
                foreach (var item in ordered)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var part = pattern.Parts.Single(part => part.Id == item.PartId);
                    var sound = state.Sounds.Single(sound => sound.Id == part.SoundId);
                    var note = item.Note;
                    if (!note.AdditionalData.IsEmpty)
                        throw new NotSupportedException("Unknown note dependencies are not executable.");
                    int sourceIndex = resourceIndices[sound.ResourceIds[0]];
                    double step = PcmSampler.Step(sources[sourceIndex].Rate, sampleRate, sound.Parameters["rootPitch"],
                        note.Pitch);
                    var position = new MusicalPosition(checked(start.Ticks + checked(iteration * pattern.Length.Ticks) +
                                                               note.Position.Ticks));
                    var end = position.Add(note.Duration);
                    long on = ConstantTempoConversion.ToFrames(position, state.Settings.Tempo, sampleRate);
                    long off = ConstantTempoConversion.ToFrames(end, state.Settings.Tempo, sampleRate);
                    if (stop is { } hardStop && position.Ticks >= hardStop.Ticks) continue;
                    if (on == off)
                    {
                        zero++;
                        continue;
                    } // Positive canonical duration; explicitly inaudible, no orphan release.

                    int release = checked((int)decimal.Round(
                        sound.Parameters["releaseMilliseconds"] * sampleRate / 1000m, 0,
                        MidpointRounding.AwayFromZero));
                    maximumRelease = Math.Max(maximumRelease, release);
                    var occurrence =
                        new ExecutionOccurrence(patternId, part.Id, note.Id, iteration, graph?.PlacementId);
                    int index = notes.Count;
                    notes.Add(new(occurrence, sourceIndex, step, (float)note.Intensity, on, off, release,
                        graph?.SourceBuffer(part.Id) ?? 0));
                    events.Add(new(on, position, ExecutionEventKind.NoteOn, occurrence, index));
                    events.Add(new(off, end, ExecutionEventKind.NoteOff, occurrence, index));
                }

            long first = ConstantTempoConversion.ToFrames(start, state.Settings.Tempo, sampleRate);
            var stopTicks = stop ?? new MusicalPosition(musicalEnd);
            long last = ConstantTempoConversion.ToFrames(stopTicks, state.Settings.Tempo, sampleRate);
            if (stop is null) last = checked(last + maximumRelease);
            events.RemoveAll(item => item.Frame >= last);
            events.Add(new(last, stopTicks, ExecutionEventKind.Stop, null, -1));
            // Conflict precedence is semantic before identity: Stop, release old voices, start new voices.
            var sorted = events.OrderBy(item => item.Frame).ThenBy(item => item.Kind)
                .ThenBy(item => item.Position.Ticks)
                .ThenBy(item => item.Occurrence?.PartId.Value).ThenBy(item => item.Occurrence?.NoteId.Value)
                .ThenBy(item => item.Occurrence?.Iteration).ToImmutableArray();
            ValidateCapacity(sorted, notes, sources, voiceCapacity, last);
            var clock = new LoopClock(start.Ticks, pattern.Length.Ticks, state.Settings.Tempo, sampleRate);
            // Different tick boundaries must stay ordered after every fractional-frame loop shift.
            // Sub-frame content retains finite execution instead of silently losing/reordering events.
            var boundaries = sorted.Select(item => item.Position.Ticks).Distinct().Order().ToArray();
            bool loopable = repeats == 1 && zero == 0 && clock.SpansFrame(pattern.Length.Ticks) &&
                            boundaries.Zip(boundaries.Skip(1), (left, right) => clock.SpansFrame(right - left))
                                .All(distinct => distinct);
            return new(snapshot.Revision, sampleRate, channels, voiceCapacity, first, last, zero, notes.ToImmutable(),
                sorted, [.. sources], graph) { Loop = loopable ? clock : null, CanonicalBasis = state };
        }
        catch
        {
            foreach (var source in sources) source.Dispose();
            throw;
        }
    }

    internal static void ValidateCapacity(ImmutableArray<PreparedEvent> events,
        ImmutableArray<PreparedNote>.Builder notes,
        List<PcmLease> sources, int capacity, long stop)
    {
        // Conservative lifetime includes release and EOF. This preflight refuses the whole plan before any output.
        var ends = new List<long>();
        foreach (var item in events.Where(item => item.Kind == ExecutionEventKind.NoteOn))
        {
            ends.RemoveAll(end => end <= item.Frame);
            var note = notes[item.NoteIndex];
            long eof = checked(note.StartFrame + (long)Math.Ceiling(sources[note.SourceIndex].Frames / note.Step));
            long end = Math.Min(stop, Math.Min(eof, checked(note.EndFrame + note.ReleaseFrames)));
            if (end > item.Frame) ends.Add(end);
            if (ends.Count > capacity)
                throw new NotSupportedException("Voice capacity exceeded; no notes were dropped.");
        }
    }
}

/// <summary>Single-owner sequential [start,end) float PCM processing. Working memory is sources plus fixed voice slots.</summary>
public sealed class OfflineSampler : IDisposable
{
    private sealed class Voice
    {
        internal PreparedNote? Note;
        internal int NoteIndex;
        internal long Age;
        internal int ReleaseAge = -1;
    }

    private readonly PreparedSampler _plan;
    private readonly Voice[] _voices;
    private PcmLease[]? _sources;
    private int _eventIndex;
    private bool _stopped;
    private readonly float[][]? _signals;
    private float[]? _coefficients;
    private int _lastSamples;
    private bool _looping;
    private long _iteration;
    private bool _smoothBoundaries;
    private long _rangeBoundary = long.MaxValue;
    private int BoundaryFadeFrames => Math.Max(1, (_plan.SampleRate * 3 + 999) / 1000);
    internal void SetBoundarySmoothing(bool enabled) => _smoothBoundaries = enabled;
    internal void SetRangeBoundary(long end) => _rangeBoundary = end;
    public long Position { get; private set; }

    internal long LoopPositionTicks => _looping && _plan.Loop is { } clock
        ? Math.Clamp(clock.Ticks(Position) - clock.StartTicks - _iteration * clock.LengthTicks, 0,
            clock.LengthTicks - 1)
        : 0;

    public int ActiveVoices
    {
        get
        {
            int count = 0;
            foreach (var voice in _voices)
                if (voice.Note is not null)
                    count++;
            return count;
        }
    }

    internal OfflineSampler(PreparedSampler plan)
    {
        _plan = plan;
        _voices = Enumerable.Range(0, plan.VoiceCapacity).Select(_ => new Voice()).ToArray();
        Position = plan.StartFrame;
        if (plan.Graph is { } graph)
        {
            _signals = Enumerable.Range(0, graph.BufferCount)
                .Select(_ => new float[checked(graph.MaximumPacketFrames * plan.Channels)]).ToArray();
            _coefficients = graph.Coefficients;
        }

        _sources = plan.LeaseSources();
    }

    public void Restart()
    {
        CheckAvailable();
        Stop();
        _stopped = false;
        _eventIndex = 0;
        Position = _plan.StartFrame;
        _iteration = 0;
    }

    internal void SetLooping(bool looping) => _looping = looping && _plan.Loop is not null;

    internal void Finish()
    {
        Stop();
        Position = _plan.EndFrame;
    }

    /// <summary>Reconstruct stateless PCM destination context, including source age and release.
    /// The frozen note table and voice capacity bound work; no history or allocation enters rendering.</summary>
    internal void Seek(long frame)
    {
        Stop();
        _stopped = false;
        _iteration = 0;
        Position = Math.Clamp(frame, _plan.StartFrame, _plan.EndFrame);
        int first = 0, last = _plan.Events.Length;
        while (first < last)
        {
            int middle = first + (last - first) / 2;
            if (_plan.Events[middle].Frame < Position) first = middle + 1;
            else last = middle;
        }

        _eventIndex = first;
        int slot = 0;
        for (int index = 0; index < _plan.Notes.Length; index++)
        {
            var note = _plan.Notes[index];
            long age = Position - note.StartFrame, release = Position - note.EndFrame;
            if (age <= 0 || age * note.Step >= _sources![note.SourceIndex].Frames ||
                release >= note.ReleaseFrames) continue;
            var voice = _voices[slot++];
            voice.Note = note;
            voice.NoteIndex = index;
            voice.Age = age;
            voice.ReleaseAge = release < 0 ? -1 : (int)release;
        }
    }

    // Mapping is constructed by the control owner, never searched/allocated in the callback.
    internal void ContinueFrom(OfflineSampler old, int[] notes)
    {
        Position = old.Position;
        _iteration = old._iteration;
        _stopped = old._stopped;
        int first = 0, last = _plan.Events.Length;
        while (first < last)
        {
            int middle = first + (last - first) / 2;
            if (EventFrame(_plan.Events[middle]) < Position) first = middle + 1;
            else last = middle;
        }

        _eventIndex = first;
        for (int index = 0; index < old._voices.Length; index++)
        {
            var source = old._voices[index];
            if (source.Note is null || notes[source.NoteIndex] < 0) continue;
            var destination = _voices[index];
            destination.NoteIndex = notes[source.NoteIndex];
            destination.Note = _plan.Notes[destination.NoteIndex];
            destination.Age = source.Age;
            destination.ReleaseAge = source.ReleaseAge;
        }
    }

    private long EventFrame(PreparedEvent item) => _looping && _plan.Loop is { } clock
        ? clock.Frame(item.Position.Ticks + _iteration * clock.LengthTicks)
        : item.Frame;

    public void Stop()
    {
        CheckAvailable();
        foreach (var voice in _voices) voice.Note = null;
        _stopped = true;
    }

    public void Process(Span<float> interleavedOutput)
    {
        CheckAvailable();
        if (interleavedOutput.Length % _plan.Channels != 0)
            throw new ArgumentException("Output must contain complete frames.");
        int frames = interleavedOutput.Length / _plan.Channels;
        if (_plan.Graph is { } prepared && frames > prepared.MaximumPacketFrames)
            throw new ArgumentOutOfRangeException(nameof(interleavedOutput), "Graph packet exceeds prepared capacity.");
        if (!_looping && frames > _plan.EndFrame - Position)
            throw new ArgumentOutOfRangeException(nameof(interleavedOutput),
                "Block exceeds the prepared end boundary.");
        interleavedOutput.Clear();
        _lastSamples = interleavedOutput.Length;
        if (_signals is not null)
            foreach (var signal in _signals)
                signal.AsSpan(0, _lastSamples).Clear();
        for (int frame = 0; frame < frames; frame++)
        {
            if (_looping && _plan.Loop is { } clock &&
                Position >= clock.Frame(clock.StartTicks + (_iteration + 1) * clock.LengthTicks))
            {
                // Source gates end at the musical boundary; the next sample starts in this same packet.
                foreach (var voice in _voices) voice.Note = null;
                _iteration++;
                _eventIndex = 0;
            }

            ApplyEvents();
            foreach (var voice in _voices)
            {
                if (voice.Note is not { } note) continue;
                var source = _sources![note.SourceIndex];
                double cursor = voice.Age * note.Step; // Derive from age; block partitions cannot change accumulation.
                if (cursor >= source.Frames)
                {
                    voice.Note = null;
                    continue;
                }

                double gain = note.Gain *
                              (voice.ReleaseAge < 0 ? 1 : 1 - (double)voice.ReleaseAge / note.ReleaseFrames);
                if (_smoothBoundaries)
                {
                    // Finish inside the permitted boundary, preserving exact silence after it.
                    // PCM source EOF, zero-release gate and the Pattern/Song end each own a boundary.
                    double remaining = (source.Frames - cursor) / note.Step;
                    long gateAge = note.EndFrame - note.StartFrame;
                    if (note.ReleaseFrames == 0) remaining = Math.Min(remaining, gateAge - voice.Age);
                    long end = _looping && _plan.Loop is { } loopClock
                        ? loopClock.Frame(loopClock.StartTicks + (_iteration + 1) * loopClock.LengthTicks)
                        : _plan.EndFrame;
                    remaining = Math.Min(remaining, Math.Min(end, _rangeBoundary) - Position);
                    gain *= Math.Clamp((remaining - 1) / BoundaryFadeFrames, 0, 1);
                }

                double left = Interpolate(source, cursor, 0);
                double right = source.Channels == 1 ? left : Interpolate(source, cursor, 1);
                Span<float> contribution = _signals is null ? interleavedOutput : _signals[note.ContributionIndex];
                if (_plan.Channels == 1) contribution[frame] += (float)((left + right) * 0.5 * gain);
                else
                {
                    contribution[frame * 2] += (float)(left * gain);
                    contribution[frame * 2 + 1] += (float)(right * gain);
                }

                voice.Age++;
                if (voice.ReleaseAge >= 0 && ++voice.ReleaseAge >= note.ReleaseFrames ||
                    voice.Age * note.Step >= source.Frames)
                    voice.Note = null;
            }

            Position++;
        }

        if (_plan.Graph is { } graph)
        {
            foreach (var operation in graph.Operations)
            {
                if (operation.Kind == GraphOperationKind.Source) continue;
                var destination = _signals![operation.Buffer].AsSpan(0, _lastSamples);
                if (operation.Kind == GraphOperationKind.Gain)
                {
                    if (operation.Inputs.Length == 0) continue; // Empty Channel Rack is a silent contribution.
                    var input = _signals[operation.Inputs[0]];
                    float amplitude = _coefficients![operation.Coefficient];
                    for (int index = 0; index < destination.Length; index++)
                        destination[index] = input[index] * amplitude;
                }
                else
                    foreach (int input in operation.Inputs)
                        for (int index = 0; index < destination.Length; index++)
                            destination[index] += _signals[input][index];
            }

            _signals![graph.OutputBuffer].AsSpan(0, _lastSamples).CopyTo(interleavedOutput);
        }

        // Events at block end belong to the following block, except final retirement is immediate.
        if (!_looping && Position == _plan.EndFrame) Stop();
    }

    /// <summary>Single-owner bounded diagnostics for the most recently processed packet, before or after Mix.</summary>
    public ReadOnlySpan<float> Signal(Id<GraphNode> nodeId)
    {
        CheckAvailable();
        var graph = _plan.Graph ?? throw new InvalidOperationException("Execution has no graph.");
        return _signals![graph.NodeBuffers[nodeId]].AsSpan(0, _lastSamples);
    }

    internal void UpdateCoefficients(float[] coefficients) => _coefficients = coefficients;

    private void ApplyEvents()
    {
        while (_eventIndex < _plan.Events.Length && EventFrame(_plan.Events[_eventIndex]) <= Position)
        {
            var item = _plan.Events[_eventIndex++];
            if (item.Kind == ExecutionEventKind.Stop)
            {
                if (!_looping) Stop();
                continue;
            }

            if (_stopped) continue;
            if (item.Kind == ExecutionEventKind.NoteOff)
            {
                foreach (var voice in _voices)
                    if (voice.Note is { } active && active.Occurrence == item.Occurrence)
                    {
                        if (active.ReleaseFrames == 0) voice.Note = null;
                        else voice.ReleaseAge = 0;
                    }
            }
            else
            {
                Voice? voice = null;
                foreach (var candidate in _voices)
                    if (candidate.Note is null)
                    {
                        voice = candidate;
                        break;
                    }

                if (voice is null) throw new InvalidOperationException("Prepared voice capacity invariant failed.");
                voice.Note = _plan.Notes[item.NoteIndex];
                voice.NoteIndex = item.NoteIndex;
                voice.Age = 0;
                voice.ReleaseAge = -1;
            }
        }
    }

    private static double Interpolate(PcmLease source, double cursor, int channel)
    {
        int index = (int)cursor;
        double fraction = cursor - index;
        double first = source.Samples[index * source.Channels + channel];
        double second = index + 1 < source.Frames ? source.Samples[(index + 1) * source.Channels + channel] : 0;
        return first + (second - first) * fraction;
    }

    private void CheckAvailable() => ObjectDisposedException.ThrowIf(_sources is null, this);

    public void Dispose()
    {
        if (_sources is { } sources)
            foreach (var source in sources)
                source.Dispose();
        _sources = null;
        foreach (var voice in _voices) voice.Note = null;
    }
}
