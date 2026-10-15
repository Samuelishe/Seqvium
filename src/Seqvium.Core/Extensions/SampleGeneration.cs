// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public sealed record SampleParameter(string Id, decimal Minimum, decimal Maximum, decimal Default);
public sealed record SampleGenerationContext(Guid Lifecycle, long Generation, int SampleRate, int MaximumFrames);
public sealed record SampleGenerationRequest(SampleGenerationContext Context, ImmutableDictionary<string, decimal> Parameters);

/// <summary>Trusted asynchronous provider. No document, device, graph or realtime callback access.</summary>
public interface ISampleGenerator : IAsyncDisposable
{
    ImmutableArray<SampleParameter> Parameters { get; }
    Task<SampleCandidate> GenerateAsync(SampleGenerationRequest request, CancellationToken cancellation);
}

/// <summary>Owned temporary WAV bytes, never a project resource. The receiving session disposes every outcome.</summary>
public sealed class SampleCandidate : IDisposable
{
    private byte[]? _wave;
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public bool IsAvailable => Volatile.Read(ref _wave) is not null;
    public SampleCandidate(string name, ReadOnlySpan<byte> wave)
    {
        if (!ExtensionDiscovery.Label(name) || wave.Length > 16 * 1024 * 1024) throw new ArgumentException("Invalid candidate.");
        Name = name; _wave = wave.ToArray();
    }
    public Stream OpenWave() => new MemoryStream(Volatile.Read(ref _wave) ?? throw new ObjectDisposedException(nameof(SampleCandidate)), false);
    internal void Validate(SampleGenerationContext context)
    {
        using var pcm = WavDecoder.Decode(_wave ?? throw new ObjectDisposedException(nameof(SampleCandidate)));
        if (pcm.SampleRate != context.SampleRate || pcm.Channels is < 1 or > 2 || pcm.Frames > context.MaximumFrames || pcm.Frames < 1)
            throw new InvalidDataException("Extension.InvalidResult");
    }
    public void Dispose() => Interlocked.Exchange(ref _wave, null);
}

/// <summary>One host exploration/editor lifetime. New requests supersede old ones; only explicit acceptance may edit music.</summary>
public sealed class SampleGenerationSession : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly ExtensionHost.ExtensionUse _use;
    private readonly ISampleGenerator _generator;
    private readonly List<Task> _pending = [];
    private CancellationTokenSource? _request;
    private SampleCandidate? _candidate;
    private SampleGenerationContext? _context;
    private long _sequence;
    private bool _closed;
    private Task? _shutdown;
    public ImmutableArray<SampleParameter> Parameters { get; }
    public string? Failure { get; private set; }

    public SampleGenerationSession(ExtensionHost host, string package, string capability)
    {
        _use = host.Acquire(package, capability);
        try
        {
            _generator = _use.Generator;
            Parameters = _generator.Parameters;
            if (Parameters.IsDefault || Parameters.Length > 32 || Parameters.Any(p => p is null || !ExtensionDiscovery.Label(p.Id) ||
                p.Minimum > p.Maximum || p.Default < p.Minimum || p.Default > p.Maximum) ||
                Parameters.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != Parameters.Length)
                throw new InvalidDataException("Extension.InvalidParameters");
        }
        catch { _use.Dispose(); throw; }
    }

    public Task<bool> GenerateAsync(SampleGenerationRequest request, CancellationToken cancellation = default)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            if (_pending.Count(t => !t.IsCompleted) >= 4) throw new InvalidOperationException("Extension.Busy");
            if (request.Context.Lifecycle == Guid.Empty || request.Context.SampleRate is < 8000 or > 192000 ||
                request.Context.MaximumFrames < 1 || request.Context.MaximumFrames > request.Context.SampleRate * 30 ||
                request.Parameters.Count != Parameters.Length || Parameters.Any(p => !request.Parameters.TryGetValue(p.Id, out var value) ||
                    value < p.Minimum || value > p.Maximum)) throw new ArgumentException("Extension.InvalidParameters");
            _request?.Cancel();
            _candidate?.Dispose(); _candidate = null; _context = null; Failure = null;
            var token = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _use.Cancellation);
            _request = token;
            long sequence = ++_sequence;
            // Task.Run is deliberate: even a provider's synchronous prefix cannot execute on GUI/audio owners.
            var task = Task.Run(() => RunAsync(request, sequence, token));
            _pending.RemoveAll(t => t.IsCompleted);
            _pending.Add(task);
            return task;
        }
    }

    private async Task<bool> RunAsync(SampleGenerationRequest request, long sequence, CancellationTokenSource cancellation)
    {
        SampleCandidate? result = null;
        try
        {
            result = await _use.GenerateAsync(request, cancellation.Token).ConfigureAwait(false)
                ?? throw new InvalidDataException("Extension.InvalidResult");
            cancellation.Token.ThrowIfCancellationRequested();
            result.Validate(request.Context);
            lock (_sync)
            {
                if (_closed || sequence != _sequence || cancellation.IsCancellationRequested) return false;
                _candidate = result; _context = request.Context; result = null;
                return true;
            }
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception error) { lock (_sync) if (sequence == _sequence) Failure = error.Message; return false; }
        finally
        {
            result?.Dispose();
            lock (_sync) { if (_request == cancellation) _request = null; cancellation.Dispose(); }
        }
    }

    public void Cancel()
    { lock (_sync) { _request?.Cancel(); ++_sequence; _candidate?.Dispose(); _candidate = null; _context = null; } }

    /// <summary>Call on the document owner immediately before preparation/commit; completion alone has no authority.</summary>
    public SampleCandidate Current(SampleGenerationContext current)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            if (_use.Cancellation.IsCancellationRequested || current != _context || _candidate is not { IsAvailable: true })
                throw new InvalidOperationException("Extension.StaleResult");
            return _candidate;
        }
    }

    /// <summary>Final synchronous acceptance gate on the document owner. Supersession/close cannot race this commit.</summary>
    public void UseCurrent(SampleGenerationContext current, Action<SampleCandidate> accept)
    { lock (_sync) accept(Current(current)); }

    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            _closed = true; Cancel();
            return new(_shutdown ??= JoinAsync());
        }
    }
    private async Task JoinAsync()
    { try { await Task.WhenAll(_pending).ConfigureAwait(false); } finally { _use.Dispose(); } }
}
