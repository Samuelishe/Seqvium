// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

public enum SamplerPublication
{
    Accepted,
    Stale,
    Cancelled,
    Capacity
}

public enum SamplerTransportState { Unprepared, Ready, Playing, Stopped, Ended, Terminated }

public readonly record struct SamplerExecutionStatus(Guid ExecutingRevision, Guid OriginPreparedRevision,
    Guid EquivalentCanonicalRevision, long Epoch, long Position, int Voices, bool Playing, bool Terminated,
    long StopAcknowledgment, SamplerTransportState Transport, Id<GraphAttachment>? AttachmentId,
    long PreparedExecutionId);

internal sealed record GraphRevisionUpdate(long PreparedExecutionId, Id<GraphAttachment> Attachment,
    Guid Revision, Guid EquivalentRevision,
    float[] Coefficients, long Authority);

/// <summary>One serialized control owner, one sequential consumer. Dispose only after the consumer has joined.
/// Active, one pending and one retired execution are the only retained slots. Gain is latest-wins;
/// Stop/Panic generations survive a subsequent Start. Musical events remain in the complete F1 plan.</summary>
public sealed class RealtimeSampler : IDisposable
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

        internal State(PreparedSampler plan, long authority, bool transient, CancellationToken cancellation, long stop)
        {
            Plan = plan;
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
    private long _authority, _stop, _start, _seenStart, _command, _startAuthority;
    private int _preparing;
    private float _gain = 1;
    private bool _playing, _disposed, _terminated;
    private bool _stopBoundary;
    private int _transitionRemaining;
    private long _acknowledgedStop, _epoch, _position;
    private long _rejectedPreparedExecutionId;
    private int _voices;
    private Guid _revision;
    private GraphRevisionUpdate? _update;
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
    public const long MaximumRetainedPcmAndScratchBytes = 4L * (SamplerPreparation.MaximumDecodedBytes + GraphPreparation.MaximumScratchBytes);
    public const long MaximumPreparedTableAllowanceBytes = 4L * SamplerPreparation.MaximumEvents * 256;
    public long RetainedPcmAndScratchBytes => StateBytes(Volatile.Read(ref _active)) +
        StateBytes(Volatile.Read(ref _pending)) + StateBytes(Volatile.Read(ref _retired));
    /// <summary>Conservative total includes the worker's sole reserved candidate even while its decode is in progress.</summary>
    public long ReservedPcmAndScratchBytes => RetainedPcmAndScratchBytes + (Volatile.Read(ref _preparing) == 0 ? 0 :
        (long)SamplerPreparation.MaximumDecodedBytes + GraphPreparation.MaximumScratchBytes);
    public bool HasPending => Volatile.Read(ref _pending) is not null;
    internal long RejectedPreparedExecutionId => Volatile.Read(ref _rejectedPreparedExecutionId);
    private static long StateBytes(State? state) => state is null ? 0 : (long)state.Plan.DecodedBytes + state.Plan.ScratchBytes;

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
                _stopBoundary ? SamplerTransportState.Stopped : _playing ? SamplerTransportState.Playing :
                    _active is null ? SamplerTransportState.Unprepared :
                        _active.Execution.Position == _active.Plan.EndFrame ? SamplerTransportState.Ended : SamplerTransportState.Ready,
            _active?.Plan.GraphAttachmentId, _active?.Authority ?? 0);
        Interlocked.Increment(ref _statusSequence);
    }

    public RealtimeSampler(int sampleRate, int channels, int maximumPacketFrames)
    {
        if (sampleRate is not (44100 or 48000) || channels is not (1 or 2) || maximumPacketFrames is < 1 or > 65536)
            throw new NotSupportedException(
                "Realtime PCM supports 44.1/48 kHz, mono/stereo and bounded packets up to 65536 frames.");
        SampleRate = sampleRate;
        Channels = channels;
        MaximumPacketFrames = maximumPacketFrames;
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

    internal bool PublishGraphUpdate(long preparedExecutionId, Id<GraphAttachment> attachment,
        Guid revision, Guid equivalentRevision, float[] coefficients,
        long authority)
    {
        if (_disposed || IsTerminated || authority != Volatile.Read(ref _authority) || HasPending) return false;
        var active = Volatile.Read(ref _active);
        if (active is null || active.Authority != preparedExecutionId ||
            active.Plan.GraphAttachmentId != attachment || active.Plan.Graph is null) return false;
        Volatile.Write(ref _update, new(preparedExecutionId, attachment, revision, equivalentRevision, coefficients, authority));
        return true;
    }

    internal SamplerPublication Publish(PreparedSampler plan, long authority, bool valid, bool cancelled,
        bool transient = false, CancellationToken lifetimeCancellation = default)
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

        Volatile.Write(ref _pending,
            new State(plan, authority, transient, lifetimeCancellation, Volatile.Read(ref _stop)));
        PreparedStatesCreated++;
        return SamplerPublication.Accepted;
    }

    public void SetGain(float gain)
    {
        if (!float.IsFinite(gain) || gain is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(gain));
        Volatile.Write(ref _gain, gain);
    }

    internal bool TryBeginWork() => Interlocked.CompareExchange(ref _preparing, 1, 0) == 0;
    internal void EndWork() => Volatile.Write(ref _preparing, 0);
    public void Start() => StartPrepared(0);

    internal void StartPrepared(long authority)
    {
        Volatile.Write(ref _startAuthority, authority);
        Volatile.Write(ref _start, Interlocked.Increment(ref _command));
    }

    public long Stop()
    {
        long command = Interlocked.Increment(ref _command);
        Volatile.Write(ref _stop, command);
        return command;
    }

    public long Panic() => Stop();

    /// <summary>Single consumer. No allocation, I/O, locks, document access or reclamation. False means invalid packet.
    /// A publication restarts at its prepared start in a new epoch; it does not transfer live voices.</summary>
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
                    if (_playing && old?.Plan.Graph is not null && pending.Plan.Graph is not null)
                        _transitionRemaining = GraphTransitionFrames;
                    _active = pending;
                    _revision = pending.Plan.Revision;
                    _epoch++;
                    if (old is not null) Volatile.Write(ref _retired, old);
                }
            }
        }

        var update = Interlocked.Exchange(ref _update, null);
        if (update is not null && update.Authority == Volatile.Read(ref _authority) &&
            _active is { } updated && updated.Authority == update.PreparedExecutionId &&
            updated.Plan.GraphAttachmentId == update.Attachment)
        {
            _active.Execution.UpdateCoefficients(update.Coefficients);
            _active.Revision = update.Revision;
            _active.EquivalentRevision = update.EquivalentRevision;
            _revision = _active.Revision;
        }

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
                _transitionRemaining = 0;
                _playing = _active is not null;
                _stopBoundary = false;
                _epoch++;
            }
        }

        var active = _active;
        bool cancelled = active?.Cancellation.IsCancellationRequested == true;
        if (cancelled)
        {
            active!.Execution.Stop();
            _playing = false;
            _transitionRemaining = 0;
        }

        int renderedFrames = 0;
        if (_playing && active is not null)
        {
            int frames = (int)Math.Min(output.Length / Channels, active.Plan.EndFrame - active.Execution.Position);
            if (frames > 0)
            {
                active.Execution.Process(output[..(frames * Channels)]);
                renderedFrames = frames;
            }
            if (active.Execution.Position == active.Plan.EndFrame) _playing = false;
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
        Volatile.Write(ref _position, active?.Execution.Position ?? 0);
        Volatile.Write(ref _voices, active?.Execution.ActiveVoices ?? 0);
        Volatile.Write(ref _acknowledgedStop, stop);
        // Transient owners end at EOF/cancel/explicit Stop. Initial handoff may precede Start;
        // the Stop preceding publication must not prematurely retire that prepared source.
        if (active is { Transient: true } && !_playing && Volatile.Read(ref _retired) is null &&
            (cancelled || active.Execution.Position == active.Plan.EndFrame || stop > active.StopAtPublication))
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
    private readonly string[] _roots;
    private PreparedSampler? _plan;
    private CancellationToken _cancellation;
    private bool _started, _ended, _reserved;
    internal Func<CancellationToken, Task>? PreparationGate { get; set; }
    internal PreparedSampler? Plan => _plan;
    internal long Authority => _authority;

    internal SamplerPreparationRequest(RealtimeSampler consumer, ProjectDocument document, Id<Pattern> target,
        int voices, int repeats, MusicalPosition start, long authority, Id<GraphAttachment>? graphTarget = null)
    {
        _consumer = consumer;
        _document = document;
        _target = target;
        _voices = voices;
        _repeats = repeats;
        _start = start;
        _authority = authority;
        _graphTarget = graphTarget;
        _lifecycle = document.LifecycleId;
        _generation = document.Generation;
        _revision = document.Current.Revision;
        _frozen = ProjectDocument.Reopen(document.Current);
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
                var plan = _graphTarget is { } graph ? GraphPreparation.Prepare(_frozen, graph, _consumer.SampleRate,
                    _consumer.Channels, _consumer.MaximumPacketFrames, _voices, _repeats,
                    cancellationToken: cancellationToken) : SamplerPreparation.PreparePattern(_frozen, _target,
                    _consumer.SampleRate, _consumer.Channels, _voices, _start, _repeats);
                if (cancellationToken.IsCancellationRequested)
                {
                    plan.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return plan;
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
                     (_graphTarget is { } graph ? _document.Current.State.GraphAttachments.Any(item => item.Id == graph) :
                         _document.Current.State.Patterns.Any(pattern => pattern.Id == _target));
        var result = _consumer.Publish(_plan, _authority, valid,
            _cancellation.IsCancellationRequested || cancellationToken.IsCancellationRequested);
        if (result == SamplerPublication.Accepted) _plan = null;
        else if (result == SamplerPublication.Capacity && _graphTarget.HasValue) return result;
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
