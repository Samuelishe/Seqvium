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
    Id<ArrangementClip>? ClipId = null,
    Id<AudioClip>? AudioClipId = null);

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
    int ContributionIndex = 0,
    bool OneShot = false,
    int ChokeGroup = 0,
    int ChokePriority = 0,
    long ChokeFrames = long.MaxValue,
    long ChokeTicks = long.MaxValue,
    long ChokeStartTicks = 0,
    long SourceOffset = 0,
    long SourceLength = long.MaxValue,
    long FadeIn = 0,
    long FadeOut = 0,
    long EnvelopeOffset = 0,
    long EnvelopeLength = 0,
    long? SourceClockStart = null,
    bool SmoothSourceEnd = true,
    long SustainStart = 0,
    long SustainEnd = 0,
    long SustainFade = 0,
    long SustainCrossfade = 0,
    bool SamplerRange = false,
    PreparedSynth? Synth = null,
    int AutomationIndex = -1,
    Id<ResourceDescriptor>? SamplerResource = null);

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
    internal long SourceEndFrame { get; init; }
    internal bool AllowMixerTail { get; init; } = true;
    public int MixerReverbBytes => Graph?.Mixer?.ReverbBytes ?? 0;
    public int MixerDelayBytes => Graph?.Mixer?.DelayBytes ?? 0;
    internal PreparedGraph? Graph { get; }
    public int DecodedBytes { get; }

    public int ScratchBytes => Automation.Sum(a => a.StorageBytes + sizeof(double)) + (Graph?.ScratchBytes ?? 0) +
                               (Graph?.Mixer is { } mixer ? mixer.Strips.Length * 2048 + mixer.Taps.Length * 64 : 0);

    public ImmutableArray<AudioContribution> Contributions => Graph?.Contributions ?? [];
    public Id<GraphAttachment>? GraphAttachmentId => Graph?.AttachmentId;
    public long PacketWork { get; internal set; }
    public int MaximumPacketEvents { get; internal set; }
    internal LoopClock? Loop { get; set; }
    internal ProjectState? CanonicalBasis { get; set; }
    internal bool IsArrangement { get; init; }
    internal PreparedAutomation[] Automation { get; init; } = [];
    internal PreparedAutomation?[] MixerAutomation { get; init; } = [];
    internal bool HeldAudition { get; init; }
    internal Id<SoundDefinition>? AuditionSound { get; init; }
    internal Guid? AuditionCandidate { get; init; }

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
        SourceEndFrame = end;
        ZeroFrameNotes = zero;
        Notes = notes;
        Events = events;
        _sources = sources;
        Graph = graph;
        DecodedBytes = sources.DistinctBy(source => source.Samples).Sum(source => source.Bytes);
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
    public static PreparedSampler PrepareSynthAudition(SoundDefinition sound, int sampleRate, int channels,
        decimal pitch, decimal intensity, bool held = false)
    {
        if (channels is not (1 or 2) || intensity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity));
        var synth = SynthVoice.Prepare(sound, sampleRate, pitch);
        // A held preview has a finite safety gate, without PCM proportional to its duration.
        long gate = sampleRate * (held ? 30L : 1L), end = gate + (held ? sampleRate * 8L : synth.Release);
        return new(Guid.Empty, sampleRate, channels, 1, 0, end, 0,
            [new(default, -1, 1, (float)intensity, 0, gate, synth.Release, Synth: synth)],
            [
                new(0, default, ExecutionEventKind.NoteOn, default(ExecutionOccurrence), 0),
                new(gate, default, ExecutionEventKind.NoteOff, default(ExecutionOccurrence), 0),
                new(end, default, ExecutionEventKind.Stop, null, -1)
            ], []) { HeldAudition = held };
    }

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

    public static PreparedSampler PrepareSamplerAudition(DecodedPcm source, SoundDefinition sound,
        int sampleRate, int channels, decimal pitch, decimal intensity, bool held = false)
    {
        sound = SamplerKeymap.Select(sound, pitch) ?? throw new ArgumentException("No sample on this key.");
        if (!SamplerPlayback.Supports(sound) || channels is not (1 or 2) || intensity is < 0 or > 1)
            throw new ArgumentException("Unsupported sampler audition.");
        var playback = SamplerPlayback.Read(sound, source.Frames);
        double step = PcmSampler.Step(source.SampleRate, sampleRate, sound.Parameters["rootPitch"], pitch);
        int release = (int)decimal.Round(sound.Parameters["releaseMilliseconds"] * sampleRate / 1000,
            0, MidpointRounding.AwayFromZero);
        long gate = sampleRate * (held ? 10L : 2L);
        var note = playback.Apply(new(default, 0, step, (float)intensity, 0, gate, release), source.SampleRate);
        long end = Math.Min(SourceEnd(note, source.Frames), gate + release);
        ImmutableArray<PreparedEvent> events =
        [
            new(0, default, ExecutionEventKind.NoteOn, default(ExecutionOccurrence), 0),
            new(end, default, ExecutionEventKind.Stop, null, -1)
        ];
        if (gate < end)
            events = events.Insert(1, new(gate, default, ExecutionEventKind.NoteOff, default(ExecutionOccurrence), 0));
        return new(Guid.Empty, sampleRate, channels, 1, 0, end, 0, [note], events,
            [source.Lease()]) { HeldAudition = held };
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
        PreparedGraph? graph = null, CancellationToken cancellationToken = default, bool objectBounds = false)
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
        var drumIndices = new Dictionary<(Id<SoundDefinition>, decimal), int>();
        int decodedBytes = 0, zero = 0, maximumRelease = 0;
        try
        {
            foreach (var part in pattern.Parts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sound = state.Sounds.Single(item => item.Id == part.SoundId);
                if (!BuiltInSound.Supports(sound) || !part.AdditionalData.IsEmpty)
                    throw new NotSupportedException(
                        "Offline sampler requires one core PCM resource, explicit root/release and understood sound/part dependencies.");
                if (SynthVoice.Supports(sound)) continue;
                if (DrumVoice.Supports(sound))
                {
                    foreach (decimal pitch in part.Notes.Select(n => n.Pitch).Distinct())
                    {
                        var key = (sound.Id, pitch);
                        if (drumIndices.ContainsKey(key)) continue;
                        int bytes = checked((int)decimal.Ceiling(sound.Parameters["decay"] * sampleRate / 1000) *
                                            sizeof(float));
                        if (decodedBytes > MaximumDecodedBytes - bytes)
                            throw new NotSupportedException("Synthesized source budget exceeded.");
                        using var synthesized =
                            DrumVoice.Render(sound, sampleRate, pitch, cancellationToken);
                        drumIndices.Add(key, sources.Count);
                        sources.Add(synthesized.Lease());
                        decodedBytes += bytes;
                    }

                    continue;
                }

                PcmSampler.ValidateConfiguration(sound.Parameters["rootPitch"],
                    sound.Parameters["releaseMilliseconds"]);
                foreach (var zone in SamplerKeymap.Zones(sound))
                {
                    var resource = zone.ResourceId;
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

                    SamplerPlayback.Read(SamplerKeymap.Source(sound, zone), sources[resourceIndices[resource]].Frames);
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
                    bool drum = DrumVoice.Supports(sound);
                    var synth = SynthVoice.Supports(sound) ? SynthVoice.Prepare(sound, sampleRate, note.Pitch) : null;
                    if (!drum && synth is null)
                    {
                        var selected = SamplerKeymap.Select(sound, note.Pitch);
                        if (selected is null) continue;
                        sound = selected;
                    }

                    int sourceIndex =
                        synth is not null ? -1 :
                        drum ? drumIndices[(sound.Id, note.Pitch)] : resourceIndices[sound.ResourceIds[0]];
                    double step = drum || synth is not null
                        ? 1
                        : PcmSampler.Step(sources[sourceIndex].Rate, sampleRate,
                            sound.Parameters["rootPitch"], note.Pitch);
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

                    int release = synth?.Release ?? (drum
                        ? 0
                        : checked((int)decimal.Round(
                            sound.Parameters["releaseMilliseconds"] * sampleRate / 1000m, 0,
                            MidpointRounding.AwayFromZero)));
                    maximumRelease = Math.Max(maximumRelease, release);
                    var occurrence =
                        new ExecutionOccurrence(patternId, part.Id, note.Id, iteration, graph?.PlacementId);
                    int index = notes.Count;
                    var prepared = new PreparedNote(occurrence, sourceIndex, step, (float)note.Intensity, on, off,
                        release,
                        graph?.SourceBuffer(part.Id) ?? 0, drum, drum ? DrumInstruments.ChokeGroup(sound) : 0,
                        DrumInstruments.Find(sound.Algorithm)?.Key == "ClosedHat" ? 1 : 0, Synth: synth,
                        SamplerResource: drum || synth is not null ? null : sound.ResourceIds[0]);
                    notes.Add(drum || synth is not null
                        ? prepared
                        : SamplerPlayback.Read(sound, sources[sourceIndex].Frames)
                            .Apply(prepared, sources[sourceIndex].Rate));
                    events.Add(new(on, position, ExecutionEventKind.NoteOn, occurrence, index));
                    events.Add(new(off, end, ExecutionEventKind.NoteOff, occurrence, index));
                }

            long first = ConstantTempoConversion.ToFrames(start, state.Settings.Tempo, sampleRate);
            var stopTicks = stop ?? new MusicalPosition(musicalEnd);
            long last = ConstantTempoConversion.ToFrames(stopTicks, state.Settings.Tempo, sampleRate);
            if (objectBounds)
            {
                // Preserve the musical bar (including rests), then only the actual supported source tails.
                // Source/Gain/Mix have no stateful tail; one-shots end at EOF, gated PCM at release or EOF.
                foreach (var note in notes)
                {
                    long eof = SourceEnd(note, SourceFrames(note, sources));
                    last = Math.Max(last,
                        note.OneShot ? eof : Math.Min(eof, checked(note.EndFrame + note.ReleaseFrames)));
                }
            }
            else if (stop is null) last = checked(last + maximumRelease);

            if (!objectBounds && stop is null && notes.Any(n => n.OneShot))
                last = Math.Max(last,
                    notes.Where(n => n.OneShot).Max(n => checked(n.StartFrame + sources[n.SourceIndex].Frames)));
            long sourceEnd = last;
            if (stop is null && graph?.Mixer is { } mixer) last = checked(last + mixer.TailFrames);
            events.RemoveAll(item => item.Frame > last ||
                                     item.Frame == last && (item.Kind != ExecutionEventKind.NoteOff ||
                                                            stop is not null || graph?.Mixer is null));
            events.Add(new(last, stopTicks, ExecutionEventKind.Stop, null, -1));
            // Conflict precedence is semantic before identity: Stop, release old voices, start new voices.
            var sorted = events.OrderBy(item => item.Frame).ThenBy(item => item.Kind)
                .ThenBy(item => item.Position.Ticks)
                .ThenBy(item => item.Occurrence?.PartId.Value).ThenBy(item => item.Occurrence?.NoteId.Value)
                .ThenBy(item => item.Occurrence?.Iteration).ToImmutableArray();
            var clock = new LoopClock(start.Ticks, pattern.Length.Ticks, state.Settings.Tempo, sampleRate);
            // Different tick boundaries must stay ordered after every fractional-frame loop shift.
            // Sub-frame content retains finite execution instead of silently losing/reordering events.
            var boundaries = sorted.Select(item => item.Position.Ticks).Distinct().Order().ToArray();
            bool loopable = !objectBounds && repeats == 1 && zero == 0 && clock.SpansFrame(pattern.Length.Ticks) &&
                            boundaries.Zip(boundaries.Skip(1), (left, right) => clock.SpansFrame(right - left))
                                .All(distinct => distinct);
            DrumChoke.Prepare(notes, sorted, state.Settings.Tempo, sampleRate, loopable ? clock : null);
            ValidateCapacity(sorted, notes, sources, voiceCapacity, last);
            if (loopable && notes.Any(n => n.OneShot || n.Synth is not null))
                ValidateDrumLoopCapacity(notes, sources, voiceCapacity, clock);
            return new(snapshot.Revision, sampleRate, channels, voiceCapacity, first, last, zero, notes.ToImmutable(),
                sorted, [.. sources], graph)
            {
                Loop = loopable ? clock : null, CanonicalBasis = state, SourceEndFrame = sourceEnd,
                AllowMixerTail = stop is null
            };
        }
        catch
        {
            foreach (var source in sources) source.Dispose();
            throw;
        }
    }

    internal static int SourceFrames(PreparedNote note, IReadOnlyList<PcmLease> sources) =>
        note.Synth is not null ? 0 : sources[note.SourceIndex].Frames;

    internal static long SourceEnd(PreparedNote note, long frames) => note.Synth is not null || note.SustainEnd > 0
        ? long.MaxValue
        : checked(note.StartFrame + (long)Math.Ceiling((note.SamplerRange
            ? Math.Min(frames - note.SourceOffset, note.SourceLength)
            : frames) / note.Step));

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
            long eof = SourceEnd(note, SourceFrames(note, sources));
            long end = Math.Min(stop, note.OneShot ? eof : Math.Min(eof, checked(note.EndFrame + note.ReleaseFrames)));
            end = Math.Min(end, note.ChokeFrames == long.MaxValue ? long.MaxValue : note.StartFrame + note.ChokeFrames);
            if (end > item.Frame) ends.Add(end);
            if (ends.Count > capacity)
                throw new NotSupportedException("Voice capacity exceeded; no notes were dropped.");
        }
    }

    private static void ValidateDrumLoopCapacity(ImmutableArray<PreparedNote>.Builder notes, List<PcmLease> sources,
        int capacity, LoopClock clock)
    {
        long cycle = clock.Frame(clock.StartTicks + clock.LengthTicks) - clock.Frame(clock.StartTicks);
        long longest = notes.Select(n => n.Synth is not null ? n.EndFrame - n.StartFrame + n.ReleaseFrames :
            n.OneShot ? (long)sources[n.SourceIndex].Frames : 0).Max();
        // Cover warm-up and the following complete cycle, using the shortest fractional cycle.
        long shortest = Math.Max(1, cycle - 1);
        long history = longest / shortest + 2;
        if (history * notes.Count > MaximumEvents)
            throw new NotSupportedException("Drum loop overlap budget exceeded.");
        var boundaries = new List<(long Frame, int Delta)>();
        for (long iteration = 0; iteration <= history; iteration++)
            foreach (var note in notes)
            {
                long shift = iteration * shortest;
                long end = note.Synth is not null
                    ? note.EndFrame + note.ReleaseFrames
                    : note.OneShot
                        ? note.StartFrame + sources[note.SourceIndex].Frames
                        : Math.Min(note.EndFrame + note.ReleaseFrames, clock.Frame(clock.StartTicks) + shortest);
                end = Math.Min(end,
                    note.ChokeTicks == long.MaxValue ? long.MaxValue : clock.Frame(note.ChokeTicks) - 1);
                if (end <= note.StartFrame) continue;
                boundaries.Add((note.StartFrame + shift, 1));
                boundaries.Add((end + shift, -1));
            }

        int active = 0;
        foreach (var boundary in boundaries.OrderBy(b => b.Frame).ThenBy(b => b.Delta))
            if ((active += boundary.Delta) > capacity)
                throw new NotSupportedException("Drum loop voice capacity exceeded; no hits were dropped.");
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
        internal long ChokeAge = long.MaxValue;
        internal long StartIteration;
        internal SynthState Synth;
        internal int AuditionReleaseFrames;
    }

    private readonly PreparedSampler _plan;
    private readonly double[] _automationValues;
    private readonly Voice[] _voices;
    private PcmLease[]? _sources;
    private int _eventIndex;
    private bool _stopped;
    private readonly float[][]? _signals;
    private float[]? _coefficients;
    private readonly MixerRuntime? _mixer;
    private long _tailSourceEnd;
    private PreparedSynth? _synthAudition;

    internal void UpdateSynthAudition(PreparedSynth parameters)
    {
        if (!_plan.HeldAudition || _plan.Notes.Length != 1 || _plan.Notes[0].Synth is null) return;
        _synthAudition = parameters;
    }

    public MixerMeter ReadMixerMeter(Id<MixerChannel>? channel = null) =>
        _mixer?.Read(channel?.Value ?? Guid.Empty) ?? default;

    internal void UpdateMixer(PreparedMixer mixer)
    {
        _mixer?.Update(mixer);
        if (_plan.AllowMixerTail) EndFrame = Math.Max(EndFrame, checked(_tailSourceEnd + mixer.TailFrames));
    }

    internal long EndFrame { get; private set; }
    internal bool HasMixerTail => _mixer is not null && EndFrame > _plan.SourceEndFrame;
    public void ResetMixerMeters() => _mixer?.ResetMeters();
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
        _automationValues = new double[plan.Automation.Length];
        _plan = plan;
        _synthAudition = plan.HeldAudition && plan.Notes.Length == 1 ? plan.Notes[0].Synth : null;
        EndFrame = plan.EndFrame;
        _tailSourceEnd = plan.SourceEndFrame;
        _voices = Enumerable.Range(0, plan.VoiceCapacity).Select(_ => new Voice()).ToArray();
        Position = plan.StartFrame;
        if (plan.Graph is { } graph)
        {
            _signals = Enumerable.Range(0, graph.BufferCount)
                .Select(_ => new float[checked(graph.MaximumPacketFrames * plan.Channels)]).ToArray();
            _coefficients = graph.Coefficients;
            if (graph.Mixer is { } mixer) _mixer = new(mixer);
        }

        _sources = plan.LeaseSources();
    }

    public void Restart()
    {
        _mixer?.Reset();
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
        Position = EndFrame;
    }

    /// <summary>Reconstruct stateless PCM destination context, including source age and release.
    /// The frozen note table and voice capacity bound work; no history or allocation enters rendering.</summary>
    internal void Seek(long frame)
    {
        _mixer?.Reset();
        Stop();
        _stopped = false;
        _iteration = 0;
        Position = Math.Clamp(frame, _plan.StartFrame, EndFrame);
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
            if (note.Synth is { } synth)
            {
                if (age <= 0 || release >= note.ReleaseFrames) continue;
                var synthVoice = _voices[slot++];
                synthVoice.Note = note;
                synthVoice.NoteIndex = index;
                synthVoice.Age = age;
                synthVoice.ReleaseAge = release < 0 ? -1 : (int)release;
                synthVoice.ChokeAge = long.MaxValue;
                synthVoice.StartIteration = 0;
                synthVoice.Synth.Reset(synth, age);
                continue; // Cold filter seek is bounded; no replay from note start in the callback.
            }

            double cursor = SourceCursor(note, age, Position);
            if (age <= 0 || cursor >= note.SourceOffset + (double)note.SourceLength ||
                cursor >= _sources![note.SourceIndex].Frames ||
                age >= note.ChokeFrames || !note.OneShot && release >= note.ReleaseFrames) continue;
            var voice = _voices[slot++];
            voice.Note = note;
            voice.NoteIndex = index;
            voice.Age = age;
            voice.ReleaseAge = note.OneShot || release < 0 ? -1 : (int)release;
            voice.ChokeAge = note.ChokeFrames;
            voice.StartIteration = 0;
        }
    }

    // Occurrence mapping is owner-prepared. Audio destination reconstruction scans the frozen table
    // once at publication, using existing fixed voice slots, just as an explicit Song seek does.
    internal void ContinueFrom(OfflineSampler old, int[] notes)
    {
        if (old._mixer is { } mixer) _mixer?.ContinueFrom(mixer);
        if (_plan.AllowMixerTail && _mixer is not null && old.HasMixerTail)
        {
            _tailSourceEnd = Math.Max(_tailSourceEnd, old._tailSourceEnd);
            EndFrame = Math.Max(Math.Max(EndFrame, old.EndFrame),
                checked(_tailSourceEnd + _plan.Graph!.Mixer!.TailFrames));
        }

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
            destination.Synth = source.Synth;
            destination.ReleaseAge = source.ReleaseAge;
            destination.StartIteration = source.StartIteration;
            destination.ChokeAge = ChokeAge(destination.Note, destination.StartIteration);
        }

        if (_plan.IsArrangement && !_stopped)
            for (int index = 0; index < _plan.Notes.Length; index++)
            {
                var note = _plan.Notes[index];
                if (note.Occurrence.AudioClipId is null) continue;
                long age = Position - note.StartFrame;
                double cursor = SourceCursor(note, age, Position);
                if (age <= 0 || Position >= note.EndFrame || cursor >= note.SourceOffset + (double)note.SourceLength ||
                    cursor >= _sources![note.SourceIndex].Frames) continue;
                bool retained = false;
                Voice? available = null;
                foreach (var voice in _voices)
                {
                    if (voice.Note is not null && voice.NoteIndex == index) retained = true;
                    if (voice.Note is null) available ??= voice;
                }

                if (retained || available is null) continue; // Complete preparation already proves capacity.
                available.Note = note;
                available.NoteIndex = index;
                available.Age = age;
                available.ReleaseAge = -1;
                available.ChokeAge = long.MaxValue;
                available.StartIteration = 0;
            }
    }

    private static double SourceCursor(PreparedNote note, long age, long position)
    {
        double cursor = note.SourceClockStart is { } origin
            ? note.SourceOffset - note.EnvelopeOffset + (position - origin) * note.Step
            : note.SourceOffset + age * note.Step;
        return note.SustainEnd > 0 && cursor >= note.SustainEnd
            ? note.SustainStart + (cursor - note.SustainStart) % (note.SustainEnd - note.SustainStart)
            : cursor;
    }

    private long EventFrame(PreparedEvent item) => _looping && _plan.Loop is { } clock
        ? clock.Frame(item.Position.Ticks + _iteration * clock.LengthTicks)
        : item.Frame;

    private long ChokeAge(PreparedNote note, long iteration) => _looping && _plan.Loop is { } clock &&
                                                                note.ChokeTicks != long.MaxValue
        ? clock.Frame(note.ChokeTicks + iteration * clock.LengthTicks) -
          clock.Frame(note.ChokeStartTicks + iteration * clock.LengthTicks)
        : note.ChokeFrames;

    public void Stop()
    {
        CheckAvailable();
        _mixer?.Reset();
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
        if (!_looping && frames > EndFrame - Position)
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
                foreach (var voice in _voices)
                    if (voice.Note is { Synth: not null } held)
                    {
                        if (held.ReleaseFrames == 0) voice.Note = null;
                        else if (voice.ReleaseAge < 0) voice.ReleaseAge = 0;
                    }
                    else if (voice.Note is not { OneShot: true }) voice.Note = null;

                _iteration++;
                _eventIndex = 0;
            }

            ApplyEvents();
            if (_synthAudition is not null && Position >= EndFrame) break;
            for (int i = 0; i < _automationValues.Length; i++) _automationValues[i] = _plan.Automation[i].At(Position);
            foreach (var voice in _voices)
            {
                if (voice.Note is not { } note) continue;
                if (note.Synth is { } synth)
                {
                    double sample = _synthAudition is { } live
                        ? voice.Synth.NextLive(live, voice.Age, voice.ReleaseAge, voice.AuditionReleaseFrames,
                            _plan.SampleRate) * note.Gain
                        : voice.Synth.Next(synth, voice.Age, voice.ReleaseAge,
                            note.AutomationIndex < 0 ? synth.G : _automationValues[note.AutomationIndex],
                            _plan.SampleRate) * note.Gain;
                    if (_smoothBoundaries)
                    {
                        long end = Math.Min(_looping ? long.MaxValue : EndFrame, _rangeBoundary);
                        sample *= Math.Clamp((end - Position - 1d) / BoundaryFadeFrames, 0, 1);
                    }

                    Span<float> target = _signals is null ? interleavedOutput : _signals[note.ContributionIndex];
                    target[frame * _plan.Channels] += (float)sample;
                    if (_plan.Channels == 2) target[frame * 2 + 1] += (float)sample;
                    voice.Age++;
                    if (voice.ReleaseAge >= 0 && ++voice.ReleaseAge >=
                        (_synthAudition is null ? note.ReleaseFrames : voice.AuditionReleaseFrames)) voice.Note = null;
                    continue;
                }

                var source = _sources![note.SourceIndex];
                double sourceAge = voice.Age * note.Step;
                double cursor = SourceCursor(note, voice.Age, Position);
                if (cursor >= source.Frames || cursor >= note.SourceOffset + (double)note.SourceLength ||
                    voice.Age >= voice.ChokeAge)
                {
                    voice.Note = null;
                    continue;
                }

                double gain = note.Gain *
                              (voice.ReleaseAge < 0 ? 1 : 1 - (double)voice.ReleaseAge / note.ReleaseFrames);
                double envelopeAge = note.SourceClockStart is { } origin
                    ? (Position - origin) * note.Step
                    : sourceAge + note.EnvelopeOffset;
                long envelopeLength = note.EnvelopeLength == 0 ? note.SourceLength : note.EnvelopeLength;
                if (note.FadeIn > 0) gain *= Math.Clamp(envelopeAge / note.FadeIn, 0, 1);
                if (note.FadeOut > 0)
                    gain *= Math.Clamp((envelopeLength - envelopeAge - note.Step) / note.FadeOut, 0, 1);
                if (voice.ChokeAge != long.MaxValue)
                    gain *= Math.Clamp((voice.ChokeAge - voice.Age - 1d) / BoundaryFadeFrames, 0, 1);
                if (_smoothBoundaries)
                {
                    // Finish inside the permitted boundary, preserving exact silence after it.
                    // PCM source EOF, zero-release gate and the Pattern/Song end each own a boundary.
                    double remaining = note.SustainEnd > 0
                        ? double.PositiveInfinity
                        : (source.Frames - cursor) / note.Step;
                    if (note.SmoothSourceEnd && note.SustainEnd == 0)
                        remaining = Math.Min(remaining,
                            (note.SourceOffset + (double)note.SourceLength - cursor) / note.Step);
                    long gateAge = note.EndFrame - note.StartFrame;
                    if (note.SmoothSourceEnd && !note.OneShot && note.ReleaseFrames == 0)
                        remaining = Math.Min(remaining, gateAge - voice.Age);
                    long end = _looping && note.OneShot
                        ? long.MaxValue
                        : _looping && _plan.Loop is { } loopClock
                            ? loopClock.Frame(loopClock.StartTicks + (_iteration + 1) * loopClock.LengthTicks)
                            : EndFrame;
                    remaining = Math.Min(remaining, Math.Min(end, _rangeBoundary) - Position);
                    gain *= Math.Clamp((remaining - 1) / BoundaryFadeFrames, 0, 1);
                }

                if (note.SustainFade > 0 && cursor >= note.SustainStart)
                {
                    // A bounded dip at the explicit seam. Never shifts markers or changes source PCM.
                    double fade = Math.Clamp((note.SustainEnd - cursor - 1) / note.SustainFade, 0, 1);
                    if (note.SourceOffset + sourceAge >= note.SustainEnd)
                        fade = Math.Min(fade, Math.Clamp((cursor - note.SustainStart) / note.SustainFade, 0, 1));
                    gain *= fade;
                }

                double left = ReadSample(source, note, cursor, 0);
                double right = source.Channels == 1 ? left : ReadSample(source, note, cursor, 1);
                if (note.SustainCrossfade > 0 && cursor >= note.SustainEnd - note.SustainCrossfade)
                {
                    // Phase-preserving pre-loop overlap: [B-X,B) -> [A-X,A).
                    // Both heads retain Step; wrapping still has period B-A, never B-A-X.
                    double t = (cursor - (note.SustainEnd - note.SustainCrossfade)) / note.SustainCrossfade;
                    double weight = t * t * (3 - 2 * t);
                    double incoming = cursor - (note.SustainEnd - note.SustainStart);
                    left += (ReadSample(source, note, incoming, 0) - left) * weight;
                    if (source.Channels == 1) right = left;
                    else right += (ReadSample(source, note, incoming, 1) - right) * weight;
                }

                Span<float> contribution = _signals is null ? interleavedOutput : _signals[note.ContributionIndex];
                if (_plan.Channels == 1) contribution[frame] += (float)((left + right) * 0.5 * gain);
                else
                {
                    contribution[frame * 2] += (float)(left * gain);
                    contribution[frame * 2 + 1] += (float)(right * gain);
                }

                voice.Age++;
                if (voice.ReleaseAge >= 0 && ++voice.ReleaseAge >= note.ReleaseFrames ||
                    note.SustainEnd == 0 && note.SourceOffset + voice.Age * note.Step >=
                    Math.Min(source.Frames, note.SourceOffset + (double)note.SourceLength) ||
                    voice.Age >= voice.ChokeAge)
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
            _mixer?.Process(_signals, interleavedOutput, _plan.Channels, Position - frames, _plan.MixerAutomation);
        }

        // Events at block end belong to the following block, except final retirement is immediate.
        if (!_looping && Position == EndFrame) Stop();
    }

    /// <summary>Single-owner bounded diagnostics for the most recently processed packet, before or after Mix.</summary>
    public ReadOnlySpan<float> Signal(Id<GraphNode> nodeId)
    {
        CheckAvailable();
        var graph = _plan.Graph ?? throw new InvalidOperationException("Execution has no graph.");
        return _signals![graph.NodeBuffers[nodeId]].AsSpan(0, _lastSamples);
    }

    internal void UpdateCoefficients(float[] coefficients) => _coefficients = coefficients;

    internal void ReleaseAudition()
    {
        if (!_plan.HeldAudition || _stopped) return;
        ApplyEvents();
        int release = 0;
        bool released = false;
        foreach (var voice in _voices)
            if (voice.Note is { } note && voice.ReleaseAge < 0)
            {
                int frames = _synthAudition?.Release ?? note.ReleaseFrames;
                released = true;
                voice.AuditionReleaseFrames = frames;
                release = Math.Max(release, frames);
                if (frames == 0) voice.Note = null;
                else voice.ReleaseAge = 0;
            }

        if (released) EndFrame = Math.Min(EndFrame, Position + release);
    }

    private void ApplyEvents()
    {
        while (_eventIndex < _plan.Events.Length && EventFrame(_plan.Events[_eventIndex]) <= Position)
        {
            var item = _plan.Events[_eventIndex++];
            if (item.Kind == ExecutionEventKind.Stop)
            {
                // Final retirement uses the execution boundary, which a live Mixer update can extend.
                continue;
            }

            if (_stopped) continue;
            if (item.Kind == ExecutionEventKind.NoteOff)
            {
                foreach (var voice in _voices)
                    if (voice.Note is { OneShot: false } active && voice.ReleaseAge < 0 &&
                        (!_looping || voice.StartIteration == _iteration) && active.Occurrence == item.Occurrence)
                    {
                        int frames = _synthAudition?.Release ?? active.ReleaseFrames;
                        voice.AuditionReleaseFrames = frames;
                        if (_synthAudition is not null) EndFrame = Math.Min(EndFrame, Position + frames);
                        if (frames == 0) voice.Note = null;
                        else voice.ReleaseAge = 0;
                    }
            }
            else
            {
                if (_plan.Notes[item.NoteIndex].ChokeFrames == 0) continue;
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
                voice.StartIteration = _iteration;
                voice.ChokeAge = ChokeAge(voice.Note, _iteration);
                if (voice.Note.Synth is { } synth) voice.Synth.Reset(synth);
            }
        }
    }

    private static double ReadSample(PcmLease source, PreparedNote note, double cursor, int channel)
    {
        if (!note.SamplerRange) return Interpolate(source, cursor, channel);
        int index = (int)cursor;
        double fraction = cursor - index;
        long end = note.SustainEnd > 0
            ? note.SustainEnd
            : Math.Min(source.Frames, note.SourceOffset + note.SourceLength);
        double second = index + 1 < end ? source.Samples[(index + 1) * source.Channels + channel]
            : note.SustainCrossfade > 0 ? source.Samples[index * source.Channels + channel] : 0;
        return source.Samples[index * source.Channels + channel] * (1 - fraction) + second * fraction;
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