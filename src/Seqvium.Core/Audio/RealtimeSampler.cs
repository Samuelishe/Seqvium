// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

public enum SamplerPublication
{
    Accepted,
    Stale,
    Cancelled,
    Capacity
}

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

        internal State(PreparedSampler plan, long authority, bool transient, CancellationToken cancellation, long stop)
        {
            Plan = plan;
            Authority = authority;
            Transient = transient;
            Cancellation = cancellation;
            StopAtPublication = stop;
            Execution = plan.CreateExecution();
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
    private long _acknowledgedStop, _epoch, _position;
    private int _voices;
    private Guid _revision;
    public int SampleRate { get; }
    public int Channels { get; }
    public int MaximumPacketFrames { get; }
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
                    Volatile.Write(ref _retired, pending);
                }
                else
                {
                    var old = _active;
                    _active = pending;
                    _revision = pending.Plan.Revision;
                    _epoch++;
                    if (old is not null) Volatile.Write(ref _retired, old);
                }
            }
        }

        long stop = Volatile.Read(ref _stop);
        long start = Volatile.Read(ref _start);
        if (stop != _acknowledgedStop)
        {
            _active?.Execution.Stop();
            _playing = false;
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
                _playing = _active is not null;
                _epoch++;
            }
        }

        var active = _active;
        bool cancelled = active?.Cancellation.IsCancellationRequested == true;
        if (cancelled)
        {
            active!.Execution.Stop();
            _playing = false;
        }

        if (_playing && active is not null)
        {
            int frames = (int)Math.Min(output.Length / Channels, active.Plan.EndFrame - active.Execution.Position);
            if (frames > 0) active.Execution.Process(output[..(frames * Channels)]);
            if (active.Execution.Position == active.Plan.EndFrame) _playing = false;
        }

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
    private PreparedSampler? _plan;
    private CancellationToken _cancellation;
    private bool _started, _ended, _reserved;

    internal SamplerPreparationRequest(RealtimeSampler consumer, ProjectDocument document, Id<Pattern> target,
        int voices, int repeats, MusicalPosition start, long authority)
    {
        _consumer = consumer;
        _document = document;
        _target = target;
        _voices = voices;
        _repeats = repeats;
        _start = start;
        _authority = authority;
        _lifecycle = document.LifecycleId;
        _generation = document.Generation;
        _revision = document.Current.Revision;
        _frozen = ProjectDocument.Reopen(document.Current);
        _frozen.MediaRoots.AddRange(document.MediaRoots);
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
            _plan = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var plan = SamplerPreparation.PreparePattern(_frozen, _target, _consumer.SampleRate,
                    _consumer.Channels, _voices, _start, _repeats);
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
                     _document.Current.State.Patterns.Any(pattern => pattern.Id == _target);
        var result = _consumer.Publish(_plan, _authority, valid,
            _cancellation.IsCancellationRequested || cancellationToken.IsCancellationRequested);
        if (result == SamplerPublication.Accepted) _plan = null;
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
