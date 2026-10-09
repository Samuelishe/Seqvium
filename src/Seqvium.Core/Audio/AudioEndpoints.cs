// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public enum AudioEndpointDirection
{
    Output,
    Input
}

public enum AudioEndpointRole
{
    Console,
    Multimedia,
    Communications
}

[Flags]
public enum AudioEndpointState
{
    Unknown = 0,
    Active = 1,
    Disabled = 2,
    NotPresent = 4,
    Unplugged = 8
}

/// <summary>Environment intent, never a musical identity. Null ID deliberately follows the default role
/// at the next resolution/open; explicit opaque IDs never fall back. Names are presentation only.</summary>
public sealed record AudioEndpointIntent
{
    public string? EndpointId { get; }
    public AudioEndpointRole Role { get; }
    public bool FollowsDefault => EndpointId is null;

    private AudioEndpointIntent(string? endpointId, AudioEndpointRole role)
    {
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role));
        if (endpointId is not null && (string.IsNullOrWhiteSpace(endpointId) || endpointId.Contains('\0')))
            throw new ArgumentException("Endpoint identity must be nonempty and contain no NUL.", nameof(endpointId));
        EndpointId = endpointId;
        Role = role;
    }

    public static AudioEndpointIntent Default(AudioEndpointRole role = AudioEndpointRole.Console) => new(null, role);

    public static AudioEndpointIntent Explicit(string endpointId)
    {
        ArgumentNullException.ThrowIfNull(endpointId);
        return new(endpointId, AudioEndpointRole.Console);
    }
}

public sealed record AudioEndpoint(
    string Id,
    string? DisplayName,
    AudioEndpointDirection Direction,
    AudioEndpointState State,
    int NameErrorCode = 0);

/// <summary>A null identity explicitly represents no current default. Error codes are opaque adapter diagnostics;
/// the adapter distinguishes an unavailable default from a failed query.</summary>
public sealed record AudioEndpointDefault(
    AudioEndpointDirection Direction,
    AudioEndpointRole Role,
    string? EndpointId,
    int ErrorCode = 0,
    bool QueryFailed = false);

public enum AudioEndpointAvailability
{
    Available,
    Missing,
    Unavailable,
    DefaultUnavailable,
    DiscoveryFailed,
    WrongDirection
}

public sealed record AudioEndpointResolution(
    AudioEndpointIntent Intent,
    AudioEndpointDirection Direction,
    AudioEndpointAvailability Availability,
    AudioEndpoint? Endpoint,
    int ErrorCode = 0)
{
    public bool IsAvailable => Availability == AudioEndpointAvailability.Available;
}

/// <summary>Immutable environment observation, not an availability guarantee or a stream capability test.</summary>
public sealed class AudioEndpointSnapshot
{
    public ImmutableArray<AudioEndpoint> Endpoints { get; }
    public ImmutableArray<AudioEndpointDefault> Defaults { get; }

    public AudioEndpointSnapshot(IEnumerable<AudioEndpoint> endpoints, IEnumerable<AudioEndpointDefault> defaults)
    {
        Endpoints = endpoints.ToImmutableArray();
        Defaults = defaults.ToImmutableArray();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var endpoint in Endpoints)
        {
            _ = AudioEndpointIntent.Explicit(endpoint.Id);
            if (!Enum.IsDefined(endpoint.Direction) || !identities.Add(endpoint.Id))
                throw new ArgumentException("Snapshot must have valid directions and unique endpoint IDs.",
                    nameof(endpoints));
        }

        var roles = new HashSet<(AudioEndpointDirection, AudioEndpointRole)>();
        foreach (var item in Defaults)
        {
            if (!Enum.IsDefined(item.Direction) || !Enum.IsDefined(item.Role) ||
                !roles.Add((item.Direction, item.Role)))
                throw new ArgumentException("Snapshot must have unique valid default roles.", nameof(defaults));
            if (item.EndpointId is not null) _ = AudioEndpointIntent.Explicit(item.EndpointId);
        }
    }

    public AudioEndpointResolution Resolve(AudioEndpointDirection direction, AudioEndpointIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        string? id = intent.EndpointId;
        if (intent.FollowsDefault)
        {
            var current = Defaults.FirstOrDefault(item => item.Direction == direction && item.Role == intent.Role);
            if (current is { QueryFailed: true })
                return new(intent, direction, AudioEndpointAvailability.DiscoveryFailed, null, current.ErrorCode);
            id = current?.EndpointId;
            if (id is null)
                return new(intent, direction, AudioEndpointAvailability.DefaultUnavailable, null,
                    current?.ErrorCode ?? 0);
        }

        var endpoint = Endpoints.FirstOrDefault(item => StringComparer.Ordinal.Equals(item.Id, id));
        var status = endpoint is null ? AudioEndpointAvailability.Missing :
            endpoint.Direction != direction ? AudioEndpointAvailability.WrongDirection :
            endpoint.State != AudioEndpointState.Active ? AudioEndpointAvailability.Unavailable :
            AudioEndpointAvailability.Available;
        if (intent.FollowsDefault &&
            status is AudioEndpointAvailability.Missing or AudioEndpointAvailability.Unavailable)
            status = AudioEndpointAvailability.DefaultUnavailable;
        return new(intent, direction, status, endpoint);
    }
}

/// <summary>The sole portable device boundary needed by the session owner: stop/join borrowing, then release.
/// A failed join must retain resources and must not report Joined. This is not a backend processing API.</summary>
public interface IAudioOutputLifetime : IDisposable
{
    bool Joined { get; }
    void Close();
}

/// <summary>One serialized control owner. Holds independent nonpersistent input/output intent and owns
/// an attached output plus its sampler. Await outstanding preparation before changing/closing output.
/// Input selection opens no stream. Same output intent is a no-op; reopening is always deliberate.</summary>
public sealed class AudioDeviceSession : IDisposable, IAsyncDisposable
{
    private IAudioOutputLifetime? _output;
    private GraphExecutionCoordinator? _coordinator;
    private bool _disposed;
    public AudioEndpointIntent OutputIntent { get; private set; } = AudioEndpointIntent.Default();
    public AudioEndpointIntent InputIntent { get; private set; } = AudioEndpointIntent.Default();
    public RealtimeSampler? OutputSampler { get; private set; }

    public void SelectInput(AudioEndpointIntent intent)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(intent);
        InputIntent = intent;
    }

    public void SelectOutput(AudioEndpointIntent intent)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(intent);
        if (OutputIntent == intent) return;
        CloseOutput();
        OutputIntent = intent;
    }

    public AudioEndpointResolution ResolveOutput(Func<AudioEndpointSnapshot> discover)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return discover().Resolve(AudioEndpointDirection.Output, OutputIntent);
    }

    public AudioEndpointResolution ResolveInput(AudioEndpointSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return snapshot.Resolve(AudioEndpointDirection.Input, InputIntent);
    }

    /// <summary>Transfers ownership after successful open, before Start. Failed attachment transfers nothing.</summary>
    public void AttachOutput(IAudioOutputLifetime output, RealtimeSampler sampler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(sampler);
        if (_output is not null || output.Joined || sampler.IsTerminated)
            throw new InvalidOperationException("Output attachment requires a fresh unborrowed lifetime.");
        _output = output;
        OutputSampler = sampler;
    }

    public void CloseOutput()
    {
        if (_coordinator is not null)
            throw new InvalidOperationException("Graph output requires awaited CloseOutputAsync before replacement/disposal.");
        CloseOutputCore();
    }

    /// <summary>Transfers application coordination ownership for this output. Closing first joins preparation, then the device borrower.</summary>
    public void AttachCoordinator(GraphExecutionCoordinator coordinator)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_output is null || _coordinator is not null || coordinator.Sampler != OutputSampler)
            throw new InvalidOperationException("Coordinator must belong to the attached output sampler.");
        _coordinator = coordinator;
    }

    public async Task CloseOutputAsync()
    {
        if (_coordinator is { } coordinator) await coordinator.CloseAsync();
        CloseOutputCore(); // A failed join retains sampler/output ownership and the closed coordinator for retry.
        if (_coordinator is { } completed) await completed.DisposeAsync();
        _coordinator = null;
    }

    private void CloseOutputCore()
    {
        if (_output is null) return;
        var sampler = OutputSampler!;
        sampler.Stop();
        _output.Close();
        if (!_output.Joined)
            throw new InvalidOperationException("Output did not join; borrowed resources remain owned.");
        // Joined consumer can no longer touch active/pending/retired state. Cleanup preserves its real acknowledgment.
        sampler.TerminateExecution();
        _output.Dispose();
        sampler.Dispose();
        _output = null;
        OutputSampler = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        CloseOutput();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await CloseOutputAsync();
        _disposed = true;
    }
}
