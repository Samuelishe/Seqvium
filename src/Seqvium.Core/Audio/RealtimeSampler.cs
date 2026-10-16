// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

public enum SamplerPublication
{
    Accepted,
    Stale,
    Cancelled,
    Capacity
}

public enum SamplerTransportState
{
    Unprepared,
    Ready,
    Playing,
    Stopped,
    Ended,
    Terminated
}

public readonly record struct SamplerExecutionStatus(
    Guid ExecutingRevision,
    Guid OriginPreparedRevision,
    Guid EquivalentCanonicalRevision,
    long Epoch,
    long Position,
    int Voices,
    bool Playing,
    bool Terminated,
    long StopAcknowledgment,
    SamplerTransportState Transport,
    Id<GraphAttachment>? AttachmentId,
    long PreparedExecutionId,
    bool Looping = false,
    long LoopPositionTicks = 0);

internal sealed record GraphRevisionUpdate(
    long PreparedExecutionId,
    Id<GraphAttachment> Attachment,
    Guid Revision,
    Guid EquivalentRevision,
    float[] Coefficients,
    long Authority);

internal sealed record AuditionRelease(long Authority, long Command);

internal sealed record SynthAuditionUpdate(long Authority, long Stop, PreparedSynth Parameters);

internal sealed record SongSeek(long Frame, long Command);

internal sealed record SongLoop(long Start, long End);

/// <summary>One serialized control owner, one sequential consumer. Dispose only after the consumer has joined.
/// Active, one pending and one retired execution are the only retained slots. Gain is latest-wins;
/// Stop/Panic generations survive a subsequent Start. Musical events remain in the complete F1 plan.</summary>
public sealed partial class RealtimeSampler : IDisposable
{
    internal sealed class State : IDisposable
    {
        internal readonly PreparedSampler Plan;
        internal readonly OfflineSampler Execution;
        internal readonly long Authority;
        internal readonly bool Transient;
        internal readonly CancellationToken Cancellation;
        internal readonly long StopAtPublication;
        internal Guid Revision, EquivalentRevision;
        internal long ContinuationAuthority;
        internal int[]? ContinuationNotes;
        internal bool MixerOnlyContinuation;
        internal PreparedMixer? MixerBudget;

        internal State(PreparedSampler plan, long authority, bool transient, CancellationToken cancellation, long stop)
        {
            Plan = plan;
            MixerBudget = plan.Graph?.Mixer;
            Authority = authority;
            Transient = transient;
            Cancellation = cancellation;
            StopAtPublication = stop;
            Execution = plan.CreateExecution();
            Revision = EquivalentRevision = plan.Revision;
        }

        public void Dispose()
        {
            Execution.Dispose();
            Plan.Dispose();
        }
    }

    private State? _pending, _active, _retired;
    private AuditionRelease? _auditionRelease;
    private long _seenAuditionRelease;
    private SynthAuditionUpdate? _synthAuditionUpdate;

    /// <summary>Control-owner, latest-wins update of an existing held synth audition only.
    /// Coefficients are prepared here; the consumer retains phase, envelope and filter state.</summary>
    public bool UpdateSynthAudition(SoundDefinition sound, decimal pitch)
    {
        if (_disposed || IsTerminated) return false;
        var state = Volatile.Read(ref _pending) ?? Volatile.Read(ref _active);
        if (state is not { Transient: true } || !state.Plan.HeldAudition ||
            state.Plan.Notes.Length != 1 || state.Plan.Notes[0].Synth is null ||
            state.Authority != Volatile.Read(ref _authority) || Volatile.Read(ref _stop) > state.StopAtPublication)
            return false;
        var parameters = SynthVoice.Prepare(sound, SampleRate, pitch);
        Volatile.Write(ref _synthAuditionUpdate, new(state.Authority, Volatile.Read(ref _stop), parameters));
        return true;
    }

    public void ReleaseAudition() => Volatile.Write(ref _auditionRelease,
        new(Volatile.Read(ref _authority), Interlocked.Increment(ref _command)));

    private long _authority, _stop, _start, _seenStart, _command, _startAuthority;
    private int _preparing;
    private float _gain = 1;
    private readonly DrumAudition _drumAudition;
    private readonly AuditionPeakProtection _sourceProtection;
    private bool _protectSourceAudition;
    public int DrumAuditionVoices => _drumAudition.ActiveVoices;
    public long DrumAuditionHits => _drumAudition.Hits;
    public long DrumAuditionOverflows => _drumAudition.Overflow;
    public long DrumAuditionSteals => _drumAudition.Stolen;
    public float DrumAuditionMinimumGain => _drumAudition.MinimumGain;

    /// <summary>Control-owner source hit, independent of graph publication/transport and final project Gain.</summary>
    public bool TriggerDrumAudition(PreparedDrumAudition source, decimal velocity)
    {
        if (_disposed || IsTerminated) return false;
        if (source.SampleRate != SampleRate) throw new ArgumentException("Audition rate differs from output.");
        return _drumAudition.Trigger(source, velocity);
    }

    public void StopDrumAudition() => _drumAudition.Stop();
    public void ProtectSourceAudition() => _protectSourceAudition = true;
    private bool _playing, _disposed, _terminated;
    private bool _stopBoundary;
    private int _transitionRemaining;
    private long _acknowledgedStop, _epoch, _position;
    private long _rejectedPreparedExecutionId;
    private int _voices;
    private bool _looping;
    private SongSeek? _seek;
    private SongLoop? _songLoop;
    private readonly bool _smoothBoundaries;
    private long _songStart, _seenSeek;
    private Guid _revision;
    private GraphRevisionUpdate? _update;
    private long _meterReset, _seenMeterReset;
    public void ResetMixerMeters() => Interlocked.Increment(ref _meterReset);

    public MixerMeter ReadMixerMeter(Id<MixerChannel>? channel = null) =>
        Volatile.Read(ref _active)?.Execution.ReadMixerMeter(channel) ?? default;

    internal bool PublishMixerUpdate(ProjectSnapshot snapshot, long executionId)
    {
        var active = Volatile.Read(ref _active);
        if (_disposed || IsTerminated || HasPending || active is null || active.Authority != executionId ||
            active.Plan.Graph is not { } graph || graph.Mixer is null) return false;
        var parameters = GainParameters(active, snapshot.State);
        var mixer = parameters.Mixer;
        if (mixer is null || mixer.Strips.Length != graph.Mixer.Strips.Length ||
            !mixer.Strips.Select(s => s.Id).SequenceEqual(graph.Mixer.Strips.Select(s => s.Id))) return false;
        if (mixer.Strips.Where((s, i) => s.Reverb.Enabled &&
                                         (!graph.Mixer.Strips[i].Reverb.Enabled ||
                                          s.Reverb.Size != graph.Mixer.Strips[i].Reverb.Size)).Any())
            return false;
        long work = active.Plan.PacketWork - (long)MaximumPacketFrames *
                    graph.Mixer.Strips.Count(s => s.Reverb.Admitted) * 512 +
                    (long)MaximumPacketFrames * mixer.Strips.Count(s => s.Reverb.Admitted) * 512;
        if (work > GraphPreparation.MaximumPacketWork)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        long authority = InvalidatePreparation();
        active.MixerBudget = mixer;
        Volatile.Write(ref _gainUpdate, new(executionId, authority, snapshot.Revision, parameters));
        return true;
    }

    private int _statusSequence;
    private SamplerExecutionStatus _status;
    public int SampleRate { get; }
    public int Channels { get; }
    public int MaximumPacketFrames { get; }
    public int GraphTransitionFrames => (SampleRate + 500) / 1000;
    public long StopAcknowledgment => Volatile.Read(ref _acknowledgedStop);
    public long TransportEpoch => Volatile.Read(ref _epoch);
    public long RenderPosition => Volatile.Read(ref _position);

    public int ActiveVoices => Volatile.Read(ref _voices);
    public bool IsTerminated => Volatile.Read(ref _terminated);

    // Read after a boundary synchronization or worker join; Guid is not an atomic status primitive.
    public Guid ExecutingRevision => _revision;
    public int StalePublications { get; private set; }
    public int CancelledPublications { get; private set; }
    public int CapacityRejections { get; private set; }
    public int Retirements { get; private set; }
    public int PreparedStatesCreated { get; private set; }
    public int PreparedStatesReleased { get; private set; }
    public int LivePreparedStates => PreparedStatesCreated - PreparedStatesReleased;
    public int ConsumerStaleRejections { get; private set; }

    public int TerminationBoundaries { get; private set; }

    // Four slots include the sole reserved candidate. Event/note object allowance is separate from DSP scratch.
    public const long MaximumRetainedPcmAndScratchBytes =
        4L * (SamplerPreparation.MaximumDecodedBytes + AudioMedia.MaximumDecodedBytes +
              GraphPreparation.MaximumScratchBytes + MixerDelayDsp.MaximumBytes + MixerReverbDsp.MaximumBytes);

    public const long MaximumPreparedTableAllowanceBytes = 4L * SamplerPreparation.MaximumEvents * 256;

    public long RetainedPcmAndScratchBytes => StateBytes(Volatile.Read(ref _active)) +
                                              StateBytes(Volatile.Read(ref _pending)) +
                                              StateBytes(Volatile.Read(ref _retired));

    /// <summary>Conservative total includes the worker's sole reserved candidate even while its decode is in progress.</summary>
    public long ReservedPcmAndScratchBytes => RetainedPcmAndScratchBytes + (Volatile.Read(ref _preparing) == 0
        ? 0
        : (long)SamplerPreparation.MaximumDecodedBytes + AudioMedia.MaximumDecodedBytes +
          GraphPreparation.MaximumScratchBytes + MixerDelayDsp.MaximumBytes + MixerReverbDsp.MaximumBytes);

    public bool HasPending => Volatile.Read(ref _pending) is not null;
    internal long RejectedPreparedExecutionId => Volatile.Read(ref _rejectedPreparedExecutionId);

    private static long StateBytes(State? state) =>
        state is null
            ? 0
            : (long)state.Plan.DecodedBytes + state.Plan.ScratchBytes + state.Plan.MixerDelayBytes +
              state.Plan.MixerReverbBytes;

    /// <summary>Thread-safe value observation. The reader may retry while the single consumer finishes its short status write.</summary>
    public SamplerExecutionStatus ReadStatus()
    {
        var spin = new SpinWait();
        while (true)
        {
            int sequence = Volatile.Read(ref _statusSequence);
            if ((sequence & 1) == 0)
            {
                var snapshot = _status;
                Thread.MemoryBarrier();
                if (sequence == Volatile.Read(ref _statusSequence)) return snapshot;
            }

            spin.SpinOnce();
        }
    }

    private void Observe()
    {
        Interlocked.Increment(ref _statusSequence);
        _status = new(_active?.Revision ?? _revision, _active?.Plan.Revision ?? _revision,
            _active?.EquivalentRevision ?? _revision, _epoch, _position, _voices, _playing, _terminated,
            _acknowledgedStop, _terminated ? SamplerTransportState.Terminated :
            _stopBoundary ? SamplerTransportState.Stopped :
            _playing ? SamplerTransportState.Playing :
            _active is null ? SamplerTransportState.Unprepared :
            _active.Execution.Position == _active.Execution.EndFrame ? SamplerTransportState.Ended :
            SamplerTransportState.Ready,
            _active?.Plan.GraphAttachmentId, _active?.Authority ?? 0, _looping && _active?.Plan.Loop is not null,
            _active?.Execution.LoopPositionTicks ?? 0);
        Interlocked.Increment(ref _statusSequence);
    }

    public RealtimeSampler(int sampleRate, int channels, int maximumPacketFrames, bool smoothBoundaries = false)
    {
        if (sampleRate is not (44100 or 48000) || channels is not (1 or 2) || maximumPacketFrames is < 1 or > 65536)
            throw new NotSupportedException(
                "Realtime PCM supports 44.1/48 kHz, mono/stereo and bounded packets up to 65536 frames.");
        SampleRate = sampleRate;
        Channels = channels;
        MaximumPacketFrames = maximumPacketFrames;
        _smoothBoundaries = smoothBoundaries;
        _drumAudition = new(sampleRate);
        _sourceProtection = new(sampleRate);
    }

    /// <summary>Invalidates pending acceptance. The identified active revision may continue. Control owner only.</summary>
    public long InvalidatePreparation()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Interlocked.Increment(ref _authority);
    }

    public SamplerPreparationRequest BeginPreparation(ProjectDocument document, Id<Pattern> target,
        int voiceCapacity = 8, int repeats = 1, MusicalPosition start = default)
    {
        document.CheckAvailable();
        return new(this, document, target, voiceCapacity, repeats, start, InvalidatePreparation());
    }

    public SamplerPreparationRequest BeginGraphPreparation(ProjectDocument document, Id<GraphAttachment> target,
        int voiceCapacity = 8, int repeats = 1)
    {
        document.CheckAvailable();
        return new(this, document, default, voiceCapacity, repeats, default, InvalidatePreparation(), target);
    }

    public SamplerPreparationRequest BeginArrangementPreparation(ProjectDocument document,
        bool preservePlayback = false)
    {
        document.CheckAvailable();
        return new(this, document, default, ArrangementPreparation.VoiceCapacity, 1, default,
            InvalidatePreparation(), arrangement: true) { PreservePlayback = preservePlayback };
    }

    internal bool PublishGraphUpdate(long preparedExecutionId, Id<GraphAttachment> attachment,
        Guid revision, Guid equivalentRevision, float[] coefficients,
        long authority)
    {
        if (_disposed || IsTerminated || authority != Volatile.Read(ref _authority) || HasPending) return false;
        var active = Volatile.Read(ref _active);
        if (active is null || active.Authority != preparedExecutionId ||
            active.Plan.GraphAttachmentId != attachment || active.Plan.Graph is null) return false;
        Volatile.Write(ref _update,
            new(preparedExecutionId, attachment, revision, equivalentRevision, coefficients, authority));
        return true;
    }

    internal SamplerPublication Publish(PreparedSampler plan, long authority, bool valid, bool cancelled,
        bool transient = false, CancellationToken lifetimeCancellation = default, bool preservePlayback = false)
    {
        if (cancelled)
        {
            CancelledPublications++;
            return SamplerPublication.Cancelled;
        }

        if (_disposed || IsTerminated || !valid || authority != Volatile.Read(ref _authority))
        {
            StalePublications++;
            return SamplerPublication.Stale;
        }

        if (plan.SampleRate != SampleRate || plan.Channels != Channels)
            throw new NotSupportedException("Prepared format does not match the consumer.");
        if (plan.Graph is { } graph && graph.MaximumPacketFrames < MaximumPacketFrames)
            throw new GraphPreparationException(GraphPreparationReasons.Format);
        if (plan.Graph is not null) GraphPreparation.Preflight(plan, MaximumPacketFrames);

        if (Volatile.Read(ref _pending) is not null)
        {
            CapacityRejections++;
            return SamplerPublication.Capacity;
        }

        // Reject dense simultaneous events before realtime entry; never omit notes for this budget.
        int count = 0;
        long frame = -1;
        foreach (var item in plan.Events)
        {
            count = item.Frame == frame ? count + 1 : 1;
            frame = item.Frame;
            if (count > 64) throw new NotSupportedException("Realtime event density exceeds 64 events per frame.");
        }

        var active = Volatile.Read(ref _active);
        if (preservePlayback && plan.Graph?.Mixer is { } nextMixer)
        {
            // Account inherited bypass/fading storage before the new execution is allocated/published.
            // Disabled fresh strips still allocate no line payload; the consumer moves the old owner.
            plan.Graph.Mixer = MixerPreparation.ReserveContinuation(nextMixer, active?.MixerBudget);
            GraphPreparation.Preflight(plan, MaximumPacketFrames);
        }

        var state = new State(plan, authority, transient, lifetimeCancellation, Volatile.Read(ref _stop));
        if (preservePlayback && active is not null &&
            (plan.IsArrangement && active.Plan.IsArrangement || plan.Loop is { } clock &&
                active.Plan.Loop is { } previous && clock.Compatible(previous)) &&
            plan.CanonicalBasis is { } nextBasis && active.Plan.CanonicalBasis is { } oldBasis &&
            (GraphExecutionCoordinator.SameExecution(oldBasis, nextBasis, ignoreGain: true, ignoreNotes: true) ||
             GraphExecutionCoordinator.SameChannelExecution(oldBasis, nextBasis)))
        {
            var indices = plan.Notes.Select((note, index) => (note, index)).ToDictionary(item => item.note.Occurrence);
            state.ContinuationNotes = active.Plan.Notes.Select(note =>
                indices.TryGetValue(note.Occurrence, out var next) &&
                SameAuditionSource(active.Plan, plan, note.Occurrence) &&
                SameAudioSource(oldBasis, nextBasis, note.Occurrence) &&
                next.note == note with
                {
                    SourceIndex = next.note.SourceIndex, ContributionIndex = next.note.ContributionIndex,
                    AutomationIndex = next.note.AutomationIndex
                } &&
                (!note.OneShot || SameDrumSound(oldBasis, nextBasis, note.Occurrence))
                    ? next.index
                    : -1).ToArray();
            state.ContinuationAuthority = active.Authority;
            state.MixerOnlyContinuation = plan.GraphAttachmentId == active.Plan.GraphAttachmentId &&
                                          ProjectDocument.HasSameContent(oldBasis with { Mixer = null },
                                              nextBasis with { Mixer = null });
        }

        Volatile.Write(ref _pending, state);
        PreparedStatesCreated++;
        return SamplerPublication.Accepted;
    }

    private static bool SameAudioSource(ProjectState left, ProjectState right, ExecutionOccurrence occurrence)
    {
        if (occurrence.AudioClipId is not { } id) return true;
        var before = left.AudioClips.Single(c => c.Id == id);
        var after = right.AudioClips.SingleOrDefault(c => c.Id == id);
        if (after is null) return false;
        return before.SoundId == after.SoundId && before.StretchRatio == after.StretchRatio &&
               (before.StretchRatio == 1 ||
                before.OffsetFrames - before.EnvelopeOffsetFrames == after.OffsetFrames - after.EnvelopeOffsetFrames &&
                AudioClipSource.EnvelopeLength(before) == AudioClipSource.EnvelopeLength(after));
    }

    private static bool SameAuditionSource(PreparedSampler left, PreparedSampler right, ExecutionOccurrence occurrence)
    {
        if (left.AuditionCandidate == right.AuditionCandidate) return true;
        var part = right.CanonicalBasis?.Patterns.SelectMany(p => p.Parts)
            .SingleOrDefault(p => p.Id == occurrence.PartId);
        return part is null || part.SoundId != left.AuditionSound && part.SoundId != right.AuditionSound;
    }

    public void SetGain(float gain)
    {
        if (!float.IsFinite(gain) || gain is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(gain));
        Volatile.Write(ref _gain, gain);
    }

    private static bool SameDrumSound(ProjectState left, ProjectState right, ExecutionOccurrence occurrence)
    {
        var oldPart = left.Patterns.Single(p => p.Id == occurrence.PatternId).Parts
            .Single(p => p.Id == occurrence.PartId);
        var newPart = right.Patterns.Single(p => p.Id == occurrence.PatternId).Parts
            .Single(p => p.Id == occurrence.PartId);
        if (oldPart.SoundId != newPart.SoundId) return false;
        var oldSound = left.Sounds.Single(s => s.Id == oldPart.SoundId);
        var newSound = right.Sounds.Single(s => s.Id == newPart.SoundId);
        return oldSound.Parameters.OrderBy(p => p.Key).SequenceEqual(newSound.Parameters.OrderBy(p => p.Key));
    }

    internal bool TryBeginWork() => Interlocked.CompareExchange(ref _preparing, 1, 0) == 0;
    internal void EndWork() => Volatile.Write(ref _preparing, 0);
    public void Start() => StartPrepared(0);

    /// <summary>Control-owner retrigger of the current transient audition, including a pending handoff.
    /// Bind Start to its preparation authority so retirement backpressure cannot restart an older source.</summary>
    public bool RetriggerAudition()
    {
        if (_disposed || IsTerminated) return false;
        long authority = Volatile.Read(ref _authority);
        var pending = Volatile.Read(ref _pending);
        var active = Volatile.Read(ref _active);
        if (pending is { Transient: true } && pending.Authority == authority ||
            active is { Transient: true } && active.Authority == authority)
        {
            StartPrepared(authority);
            return true;
        }

        return false;
    }

    internal void StartPrepared(long authority)
    {
        Volatile.Write(ref _startAuthority, authority);
        Volatile.Write(ref _start, Interlocked.Increment(ref _command));
    }

    public long Stop()
    {
        StopDrumAudition();
        long command = Interlocked.Increment(ref _command);
        Volatile.Write(ref _stop, command);
        return command;
    }

    public long Panic() => Stop();
    public Guid? SampleAuditionCandidate => Volatile.Read(ref _active)?.Plan.AuditionCandidate;
    public void SetLooping(bool looping) => Volatile.Write(ref _looping, looping);

    /// <summary>Control-owner song cursor. Seeking never starts playback or overrides Stop/Panic.</summary>
    public void SeekSong(long frame)
    {
        if (frame < 0) throw new ArgumentOutOfRangeException(nameof(frame));
        Volatile.Write(ref _songStart, frame);
        Volatile.Write(ref _seek, new(frame, Interlocked.Increment(ref _command)));
    }

    /// <summary>Session-only half-open song loop, in execution frames. Null disables it.</summary>
    public void SetSongLoop(long start, long end, bool enabled)
    {
        if (enabled && (start < 0 || end - start < SampleRate / 100))
            throw new ArgumentOutOfRangeException(nameof(end));
        Volatile.Write(ref _songLoop, enabled ? new(start, end) : null);
    }

    /// <summary>Single consumer. No allocation, I/O, locks, document access or reclamation. False means invalid packet.
    /// Compatible live note edits retain loop position and unchanged voices; other publications begin a new epoch.</summary>
    public bool Process(Span<float> output)
    {
        output.Clear();
        if (_disposed || _terminated || output.Length % Channels != 0 ||
            output.Length / Channels > MaximumPacketFrames) return false;
        var pending = Volatile.Read(ref _pending);
        if (pending is not null && Volatile.Read(ref _retired) is null)
        {
            pending = Interlocked.Exchange(ref _pending, null);
            if (pending is not null)
            {
                if (pending.Authority != Volatile.Read(ref _authority) || pending.Cancellation.IsCancellationRequested)
                {
                    ConsumerStaleRejections++;
                    Volatile.Write(ref _rejectedPreparedExecutionId, pending.Authority);
                    Volatile.Write(ref _retired, pending);
                }
                else
                {
                    var old = _active;
                    bool continuing = _playing && old is not null &&
                                      (_looping || (old.Plan.IsArrangement && pending.Plan.IsArrangement ||
                                                    pending.MixerOnlyContinuation) &&
                                          old.Execution.Position < (old.Execution.HasMixerTail
                                              ? Math.Max(old.Execution.EndFrame, pending.Execution.EndFrame)
                                              : pending.Execution.EndFrame)) &&
                                      pending.ContinuationAuthority == old.Authority &&
                                      pending.ContinuationNotes is { };
                    pending.Execution.SetLooping(_looping);
                    pending.Execution.SetBoundarySmoothing(_smoothBoundaries);
                    if (continuing) pending.Execution.ContinueFrom(old!.Execution, pending.ContinuationNotes!);
                    else if (_playing && old is not null &&
                             (old.Plan.IsArrangement && pending.Plan.IsArrangement || pending.MixerOnlyContinuation) &&
                             pending.ContinuationAuthority == old.Authority &&
                             old.Execution.Position >= pending.Execution.EndFrame)
                    {
                        pending.Execution.Finish();
                        _playing = false;
                    }
                    else if (_playing && old?.Plan.Graph is not null && pending.Plan.Graph is not null)
                        _transitionRemaining = GraphTransitionFrames;

                    _active = pending;
                    _revision = pending.Plan.Revision;
                    if (!continuing) _epoch++;
                    if (old is not null) Volatile.Write(ref _retired, old);
                }
            }
        }

        var update = Interlocked.Exchange(ref _update, null);
        // Withdraw drafts before applying any canonical tables. Authority and Stop are monotonic,
        // so even an unconsumed preview cannot return after Edit/Undo ABA or transport teardown.
        _active?.Execution.SetGraphGainPreview(null);
        if (update is not null && update.Authority == Volatile.Read(ref _authority) &&
            _active is { } updated && updated.Authority == update.PreparedExecutionId &&
            updated.Plan.GraphAttachmentId == update.Attachment)
        {
            _active.Execution.UpdateCoefficients(update.Coefficients);
            _active.Revision = update.Revision;
            _active.EquivalentRevision = update.EquivalentRevision;
            _revision = _active.Revision;
        }

        long reset = Volatile.Read(ref _meterReset);
        if (reset != _seenMeterReset)
        {
            _active?.Execution.ResetMixerMeters();
            _seenMeterReset = reset;
        }

        var gainUpdate = Interlocked.Exchange(ref _gainUpdate, null);
        if (gainUpdate is not null && gainUpdate.Authority == Volatile.Read(ref _authority) &&
            _active is { } gained && gained.Authority == gainUpdate.Execution)
        {
            gained.Execution.UpdateCoefficients(gainUpdate.Parameters.Coefficients);
            if (gainUpdate.Parameters.Mixer is { } mixer) gained.Execution.UpdateMixer(mixer);
            gained.Revision = gained.EquivalentRevision = gainUpdate.Revision;
            _revision = gained.Revision;
        }

        var preview = Volatile.Read(ref _gainPreview);
        if (preview is not null && preview.Authority == Volatile.Read(ref _authority) &&
            preview.Stop == Volatile.Read(ref _stop) && _active is { } previewed &&
            previewed.Authority == preview.Execution)
            previewed.Execution.SetGraphGainPreview(preview.Parameters);

        long stop = Volatile.Read(ref _stop);
        long start = Volatile.Read(ref _start);
        if (stop != _acknowledgedStop)
        {
            _active?.Execution.Stop();
            _playing = false;
            _stopBoundary = true;
            _transitionRemaining = 0;
            // A Start already seen cannot resurrect playback; a newer Start deliberately restarts.
        }

        if (start != _seenStart)
        {
            long startAuthority = Volatile.Read(ref _startAuthority);
            // Audition Start belongs to the requested source, not whichever older state remains active
            // during retirement backpressure. Retry at later boundaries until its handoff is possible.
            if (start <= stop) _seenStart = start;
            else if (startAuthority == 0 || _active?.Authority == startAuthority)
            {
                _seenStart = start;
                _active?.Execution.Restart();
                if (_active?.Plan.IsArrangement == true)
                {
                    _active.Execution.Seek(Volatile.Read(ref _songStart));
                    if (Volatile.Read(ref _seek) is { } initialSeek && initialSeek.Command <= start)
                        _seenSeek = initialSeek.Command;
                }

                _transitionRemaining = 0;
                _playing = _active is not null;
                _stopBoundary = false;
                _epoch++;
            }
        }

        var active = _active;
        var synthUpdate = Interlocked.Exchange(ref _synthAuditionUpdate, null);
        if (active is { Transient: true } && synthUpdate is not null &&
            synthUpdate.Authority == active.Authority && synthUpdate.Authority == Volatile.Read(ref _authority) &&
            synthUpdate.Stop == stop && _playing)
            active.Execution.UpdateSynthAudition(synthUpdate.Parameters);
        var seek = Volatile.Read(ref _seek);
        if (active?.Plan.IsArrangement == true && seek is not null && seek.Command != _seenSeek)
        {
            _seenSeek = seek.Command;
            active.Execution.Seek(seek.Frame);
            _transitionRemaining = _playing ? GraphTransitionFrames : 0;
            _epoch++;
        }

        bool cancelled = active?.Cancellation.IsCancellationRequested == true;
        if (cancelled)
        {
            active!.Execution.Stop();
            _playing = false;
            _transitionRemaining = 0;
        }

        var releaseAudition = Volatile.Read(ref _auditionRelease);
        if (_playing && active is { Transient: true } && releaseAudition is not null &&
            releaseAudition.Authority == active.Authority && releaseAudition.Command != _seenAuditionRelease)
        {
            active.Execution.ReleaseAudition();
            _seenAuditionRelease = releaseAudition.Command;
        }

        int renderedFrames = 0;
        if (_playing && active is not null)
        {
            active.Execution.SetLooping(_looping);
            var range = active.Plan.IsArrangement ? Volatile.Read(ref _songLoop) : null;
            long loopEnd = Math.Min(range?.End ?? 0, active.Execution.EndFrame);
            bool rangeLoop = range is not null && range.Start < loopEnd;
            int remaining = output.Length / Channels;
            while (remaining > 0)
            {
                active.Execution.SetRangeBoundary(rangeLoop ? loopEnd : long.MaxValue);
                if (rangeLoop && active.Execution.Position >= loopEnd) active.Execution.Seek(range!.Start);
                long end = rangeLoop ? loopEnd : active.Execution.EndFrame;
                int frames = _looping && active.Plan.Loop is not null
                    ? remaining
                    : (int)Math.Max(0, Math.Min(remaining, end - active.Execution.Position));
                if (frames == 0) break;
                active.Execution.Process(output.Slice(renderedFrames * Channels, frames * Channels));
                renderedFrames += frames;
                remaining -= frames;
            }

            if (rangeLoop && active.Execution.Position >= loopEnd) active.Execution.Seek(range!.Start);
            if (!rangeLoop && (!_looping || active.Plan.Loop is null) &&
                active.Execution.Position >= active.Execution.EndFrame)
            {
                active.Execution.Stop();
                _playing = false;
            }
        }

        // Finite new-topology fade-in only. Hard Stop/Panic never emit an old-state tail.
        if (_transitionRemaining > 0)
            for (int frame = 0; frame < renderedFrames && _transitionRemaining > 0; frame++)
            {
                float amplitude = (GraphTransitionFrames - _transitionRemaining + 1f) / GraphTransitionFrames;
                for (int channel = 0; channel < Channels; channel++) output[frame * Channels + channel] *= amplitude;
                _transitionRemaining--;
            }

        if (!_playing) _transitionRemaining = 0;
        float gain = Volatile.Read(ref _gain);
        for (int i = 0; i < output.Length; i++) output[i] *= gain;
        if (_protectSourceAudition && active is { Transient: true })
            for (int i = 0; i < output.Length; i += Channels)
            {
                float peak = Math.Abs(output[i]);
                if (Channels == 2) peak = Math.Max(peak, Math.Abs(output[i + 1]));
                float protectedGain = _sourceProtection.Gain(peak);
                for (int channel = 0; channel < Channels; channel++) output[i + channel] *= protectedGain;
            }

        _drumAudition.Process(output, Channels);
        Volatile.Write(ref _position, active?.Execution.Position ?? 0);
        Volatile.Write(ref _voices, active?.Execution.ActiveVoices ?? 0);
        Volatile.Write(ref _acknowledgedStop, stop);
        // Transient owners end at EOF/cancel/explicit Stop. Initial handoff may precede Start;
        // the Stop preceding publication must not prematurely retire that prepared source.
        if (active is { Transient: true } && !_playing && Volatile.Read(ref _retired) is null &&
            (cancelled || active.Execution.Position == active.Execution.EndFrame || stop > active.StopAtPublication))
        {
            _active = null;
            Volatile.Write(ref _retired, active);
        }

        Observe();

        return true;
    }

    /// <summary>Consumer-only terminal boundary. This is not a packet Stop/Panic acknowledgment.</summary>
    public void TerminateExecution()
    {
        if (_terminated) return;
        _active?.Execution.Stop();
        _drumAudition.Clear();
        _playing = false;
        Volatile.Write(ref _terminated, true);
        TerminationBoundaries++;
        Volatile.Write(ref _voices, 0);
        Observe();
    }

    /// <summary>Control owner reclamation. The consumer has already stopped borrowing this state.</summary>
    public void RetireCompleted()
    {
        var retired = Interlocked.Exchange(ref _retired, null);
        if (retired is not null)
        {
            retired.Dispose();
            PreparedStatesReleased++;
            Retirements++;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _drumAudition.Clear();
        var pending = Interlocked.Exchange(ref _pending, null);
        if (pending is not null)
        {
            pending.Dispose();
            PreparedStatesReleased++;
        }

        RetireCompleted();
        if (_active is not null)
        {
            _active.Dispose();
            PreparedStatesReleased++;
        }

        _active = null;
        // Cleanup deliberately does not modify StopAcknowledgment or the last callback voice observation.
    }
}

/// <summary>Captured immutable revision and media roots; PrepareAsync never reads the mutable source document.
/// Begin/Publish/Dispose are serialized by the document owner. Await before Dispose. One request is single-use.</summary>
public sealed class SamplerPreparationRequest : IDisposable
{
    private readonly RealtimeSampler _consumer;
    private readonly ProjectDocument _document, _frozen;
    private readonly Id<Pattern> _target;
    private readonly Guid _lifecycle, _revision;
    private readonly long _generation, _authority;
    private readonly int _voices, _repeats;
    private readonly MusicalPosition _start;
    private readonly Id<GraphAttachment>? _graphTarget;
    private readonly bool _arrangement;
    private readonly string[] _roots;
    private PreparedSampler? _plan;
    private CancellationToken _cancellation;
    private bool _started, _ended, _reserved;
    internal Func<CancellationToken, Task>? PreparationGate { get; set; }
    internal PreparedSampler? Plan => _plan;
    internal long Authority => _authority;
    internal bool PreservePlayback { get; set; }
    internal SampleAuditionSource? Audition { get; set; }

    internal SamplerPreparationRequest(RealtimeSampler consumer, ProjectDocument document, Id<Pattern> target,
        int voices, int repeats, MusicalPosition start, long authority, Id<GraphAttachment>? graphTarget = null,
        bool arrangement = false)
    {
        _consumer = consumer;
        _document = document;
        _target = target;
        _voices = voices;
        _repeats = repeats;
        _start = start;
        _authority = authority;
        _graphTarget = graphTarget;
        _arrangement = arrangement;
        _lifecycle = document.LifecycleId;
        _generation = document.Generation;
        _revision = document.Current.Revision;
        _frozen = ProjectDocument.Reopen(document.Current);
        AudioMedia.ShareCache(document, _frozen);
        _frozen.MediaRoots.AddRange(document.MediaRoots);
        _roots = [.. document.MediaRoots];
    }

    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        if (_started || _ended) throw new InvalidOperationException("Preparation is single-use.");
        _started = true;
        _cancellation = cancellationToken;
        if (!_consumer.TryBeginWork())
            throw new InvalidOperationException(
                "Only one preparation candidate is admitted; publish or dispose it before retrying.");
        _reserved = true;
        try
        {
            if (PreparationGate is { } gate) await gate(cancellationToken).ConfigureAwait(false);
            _plan = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var plan = _arrangement ? ArrangementPreparation.Prepare(_frozen, _consumer.SampleRate,
                        _consumer.Channels, _consumer.MaximumPacketFrames, cancellationToken) :
                    _graphTarget is { } graph ? GraphPreparation.Prepare(_frozen, graph, _consumer.SampleRate,
                        _consumer.Channels, _consumer.MaximumPacketFrames, _voices, _repeats,
                        cancellationToken: cancellationToken) : SamplerPreparation.PreparePattern(_frozen, _target,
                        _consumer.SampleRate, _consumer.Channels, _voices, _start, _repeats);
                if (cancellationToken.IsCancellationRequested)
                {
                    plan.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (Audition is not { } audition) return plan;
                using (plan) return audition.Apply(plan);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            ReleaseReservation();
            throw;
        }
    }

    public SamplerPublication Publish(CancellationToken cancellationToken = default)
    {
        if (_ended || _plan is null) throw new InvalidOperationException("Preparation has no publishable result.");
        bool valid = !_document.IsClosed && _document.LifecycleId == _lifecycle &&
                     _document.Generation == _generation && _document.Current.Revision == _revision &&
                     _document.MediaRoots.SequenceEqual(_roots) &&
                     (_arrangement || (_graphTarget is { } graph
                         ? _document.Current.State.GraphAttachments.Any(item => item.Id == graph)
                         : _document.Current.State.Patterns.Any(pattern => pattern.Id == _target)));
        var result = _consumer.Publish(_plan, _authority, valid,
            _cancellation.IsCancellationRequested || cancellationToken.IsCancellationRequested,
            preservePlayback: PreservePlayback);
        if (result == SamplerPublication.Accepted) _plan = null;
        else if (result == SamplerPublication.Capacity && (_graphTarget.HasValue || _arrangement)) return result;
        else
        {
            _plan.Dispose();
            _plan = null;
        }

        _ended = true;
        ReleaseReservation();
        return result;
    }

    public void Dispose()
    {
        Audition?.Dispose();
        Audition = null;
        _plan?.Dispose();
        _plan = null;
        _ended = true;
        ReleaseReservation();
    }

    private void ReleaseReservation()
    {
        if (!_reserved) return;
        _reserved = false;
        _consumer.EndWork();
    }
}
