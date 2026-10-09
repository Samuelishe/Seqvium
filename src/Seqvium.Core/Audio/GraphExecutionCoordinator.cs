// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Core;

public sealed record GraphExecutionStatus(Guid CanonicalRevision, bool PreparationPending,
    SamplerExecutionStatus Execution, bool InvalidCanonical, bool LastValidPlaying, bool Closed,
    string? Blocker, int Preparations, int Supersessions, int CapacityRetries, Id<GraphAttachment> Target);

/// <summary>Serialized application owner. All public operations/mutations run on the supplied owner context.
/// At most one worker/candidate is retained; timer progress is coalesced into one owner callback.
/// CloseAsync joins preparation before the caller joins its device; DisposeAsync never releases borrowed audio.
/// A fresh coordinator never inherits a previous document's last-valid plan.</summary>
public sealed class GraphExecutionCoordinator : IAsyncDisposable
{
    private readonly ProjectDocument _document;
    private readonly RealtimeSampler _sampler;
    private readonly SynchronizationContext _owner;
    private readonly Timer? _progress;
    private readonly int _voices, _repeats;
    private readonly Func<CancellationToken, Task>? _gate;
    private Id<GraphAttachment> _target;
    private CancellationTokenSource? _cancellation;
    private SamplerPreparationRequest? _request, _candidate;
    private Task _work = Task.CompletedTask;
    private ProjectSnapshot? _basis;
    private string[] _basisRoots = [], _desiredRoots = [];
    private Guid _origin;
    private Guid _executionRevision;
    private Id<GraphNode>[] _gainNodes = [];
    private Id<GraphNode>[] _candidateGainNodes = [];
    private long _desired, _startAuthority;
    private bool _busy, _dirty, _closed, _disposed;
    private int _queued, _preparations, _supersessions, _retries;
    private string? _blocker;
    private GraphIntentReport? _intent;
    private GraphExecutionStatus _status;

    public GraphExecutionCoordinator(ProjectDocument document, RealtimeSampler sampler,
        Id<GraphAttachment> target, SynchronizationContext owner, int voiceCapacity = 8, int repeats = 1)
        : this(document, sampler, target, owner, voiceCapacity, repeats, null, true) { }

    internal GraphExecutionCoordinator(ProjectDocument document, RealtimeSampler sampler,
        Id<GraphAttachment> target, SynchronizationContext owner, int voices, int repeats,
        Func<CancellationToken, Task>? gate, bool automaticProgress)
    {
        document.CheckAvailable();
        ArgumentNullException.ThrowIfNull(owner);
        if (voices is < 1 or > 8 || repeats is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(voices));
        _document = document; _sampler = sampler; _target = target; _owner = owner;
        _voices = voices; _repeats = repeats; _gate = gate;
        _status = new(document.Current.Revision, false, default, false, false, false, null, 0, 0, 0, target);
        CheckOwner();
        document.Changed += Refresh;
        document.Closed += BeginClose;
        if (automaticProgress) _progress = new Timer(_ => QueueProgress(), null, 20, 20);
        Refresh();
    }

    public GraphExecutionStatus ReadStatus()
    {
        var status = Volatile.Read(ref _status);
        var execution = _sampler.ReadStatus();
        return status with { Execution = execution, LastValidPlaying = status.InvalidCanonical && execution.Playing };
    }
    internal RealtimeSampler Sampler => _sampler;

    public void SelectTarget(Id<GraphAttachment> target)
    {
        CheckOwner();
        ObjectDisposedException.ThrowIf(_closed, this);
        if (_target == target) return;
        _target = target;
        _basis = null;
        Refresh();
    }

    /// <summary>Deliberate Start belongs to the current validated publication. Preparation alone never starts transport.</summary>
    public bool Start()
    {
        CheckOwner();
        if (_closed || _blocker is not null || _dirty || _busy || _candidate is not null || _basis is null ||
            _basis.Revision != _document.Current.Revision || _sampler.IsTerminated) return false;
        _sampler.StartPrepared(_startAuthority);
        return true;
    }

    public long Stop() { CheckOwner(); return _sampler.Stop(); }
    public long Panic() { CheckOwner(); return _sampler.Panic(); }

    private void Refresh()
    {
        CheckOwner();
        if (_closed) return;
        _desired++;
        _desiredRoots = [.. _document.MediaRoots];
        _sampler.InvalidatePreparation();
        if (_busy) _supersessions++;
        _cancellation?.Cancel();
        _candidate?.Dispose(); _candidate = null;
        var state = _document.Current.State;
        var attachment = state.GraphAttachments.SingleOrDefault(item => item.Id == _target);
        _intent = attachment is null ? null : GraphDiagnostics.Inspect(state).Single(item => item.GraphId == attachment.GraphId);
        _blocker = _intent is { IsEligibleForPreparation: true } ? null :
            _intent?.Diagnostics[0].Reason ?? GraphPreparationReasons.Target;
        _dirty = _blocker is null;
        Progress();
    }

    private void QueueProgress()
    {
        if (Interlocked.CompareExchange(ref _queued, 1, 0) != 0) return;
        _owner.Post(_ => { Volatile.Write(ref _queued, 0); if (!_disposed) Progress(); }, null);
    }

    internal void Progress()
    {
        CheckOwner();
        if (_closed) { Observe(); return; }
        _sampler.RetireCompleted();
        if (_sampler.IsTerminated)
        {
            _blocker = "graph.execution-terminated";
            _cancellation?.Cancel();
            _candidate?.Dispose(); _candidate = null;
            _dirty = false;
            Observe(); return;
        }
        if (!_document.MediaRoots.SequenceEqual(_desiredRoots)) { Refresh(); return; }
        if (_busy) { Observe(); return; }
        if (_candidate is { } candidate)
        {
            var result = candidate.Publish();
            if (result == SamplerPublication.Capacity) { _retries++; Observe(); return; }
            if (result == SamplerPublication.Accepted) Accepted(candidate, _document.Current);
            candidate.Dispose(); _candidate = null;
            if (result != SamplerPublication.Accepted) _dirty = true;
        }
        if (_dirty)
        {
            _dirty = false;
            var snapshot = _document.Current;
            var attachment = snapshot.State.GraphAttachments.SingleOrDefault(item => item.Id == _target);
            var report = _intent;
            if (report is null || !report.IsEligibleForPreparation)
                _blocker = report?.Diagnostics[0].Reason ?? GraphPreparationReasons.Target;
            else if (_basis is not null && _basisRoots.SequenceEqual(_document.MediaRoots) &&
                SameExecution(_basis.State, snapshot.State, ignoreGain: true) &&
                _sampler.ReadStatus().OriginPreparedRevision == _origin && !_sampler.HasPending)
            {
                bool geometry = SameExecution(_basis.State, snapshot.State, ignoreGain: false);
                var graph = snapshot.State.Graphs.Single(item => item.Id == attachment!.GraphId);
                // No source decoding, scratch allocation or topology construction for geometry/Gain edits.
                float[] coefficients = _gainNodes.Select(id =>
                    (float)graph.Nodes.Single(node => node.Id == id).Parameters[GraphBuiltIns.GainAmplitude].GetDecimal()).ToArray();
                long authority = _sampler.InvalidatePreparation();
                Guid executingRevision = geometry ? _executionRevision : snapshot.Revision;
                if (_sampler.PublishGraphUpdate(_origin, executingRevision, snapshot.Revision, coefficients, authority))
                {
                    _basis = snapshot;
                    _executionRevision = executingRevision;
                }
                else _dirty = true;
            }
            else
            {
                _cancellation?.Dispose();
                _cancellation = new();
                _request = _sampler.BeginGraphPreparation(_document, _target, _voices, _repeats);
                _request.PreparationGate = _gate;
                _busy = true; _preparations++;
                _work = PrepareAsync(_request, snapshot, _desired, _cancellation.Token);
            }
        }
        Observe();
    }

    private async Task PrepareAsync(SamplerPreparationRequest request, ProjectSnapshot snapshot, long desired,
        CancellationToken cancellation)
    {
        bool retained = false;
        try
        {
            await request.PrepareAsync(cancellation);
            if (!_closed && desired == _desired && !cancellation.IsCancellationRequested)
            {
                RememberGains(request);
                var result = request.Publish();
                if (result == SamplerPublication.Accepted) Accepted(request, snapshot);
                else if (result == SamplerPublication.Capacity)
                {
                    _candidate = request; retained = true; _retries++;
                }
                else _dirty = true;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (GraphPreparationException error)
        {
            if (!_closed && desired == _desired) _blocker = error.Reason;
        }
        catch (Exception)
        {
            if (!_closed && desired == _desired) _blocker = "graph.preparation-fault";
        }
        finally
        {
            if (!retained) request.Dispose();
            _request = null; _busy = false;
            Observe();
            QueueProgress();
        }
    }

    private void RememberGains(SamplerPreparationRequest request)
    {
        var graph = request.Plan!.Graph!;
        _candidateGainNodes = graph.Operations.Where(item => item.Kind == GraphOperationKind.Gain)
            .Select(item => item.NodeId).ToArray();
    }

    private void Accepted(SamplerPreparationRequest request, ProjectSnapshot snapshot)
    {
        _basis = snapshot; _basisRoots = [.. _document.MediaRoots];
        _gainNodes = _candidateGainNodes;
        _origin = _executionRevision = snapshot.Revision; _startAuthority = request.Authority; _blocker = null;
    }

    private static bool SameExecution(ProjectState left, ProjectState right, bool ignoreGain)
    {
        ProjectState Normalize(ProjectState state) => state with
        {
            Graphs = [.. state.Graphs.OrderBy(item => item.Id.Value).Select(graph => graph with
            {
                Nodes = [.. graph.Nodes.OrderBy(item => item.Id.Value).Select(node => node with
                {
                    Position = node.Position with { X = 0, Y = 0 },
                    Parameters = ignoreGain && node.Type == GraphBuiltIns.Gain
                        ? ImmutableDictionary<string, System.Text.Json.JsonElement>.Empty : node.Parameters,
                    Ports = [.. node.Ports.OrderBy(port => port.Id.Value)]
                })],
                Connections = [.. graph.Connections.OrderBy(edge => edge.Id.Value)]
            })],
            GraphAttachments = [.. state.GraphAttachments.OrderBy(item => item.Id.Value)
                .Select(item => item with { Sources = [.. item.Sources.OrderBy(binding => binding.Id.Value)] })]
        };
        return ProjectDocument.HasSameContent(Normalize(left), Normalize(right));
    }

    private void Observe() => Volatile.Write(ref _status, new(_document.Current.Revision,
        _busy || _dirty || _candidate is not null || _sampler.HasPending,
        default, _blocker is not null && _blocker != "graph.execution-terminated" && !_closed,
        false, _closed, _blocker, _preparations, _supersessions, _retries, _target));

    private void BeginClose()
    {
        CheckOwner();
        if (_closed) return;
        _closed = true; _dirty = false;
        _progress?.Dispose();
        _sampler.InvalidatePreparation();
        _cancellation?.Cancel();
        _sampler.Stop();
        _candidate?.Dispose(); _candidate = null;
        Observe();
    }

    /// <summary>First shutdown phase only: cancel and join preparations. Device owner must subsequently confirm consumer join.</summary>
    public async Task CloseAsync()
    {
        CheckOwner();
        BeginClose();
        await _work;
        _sampler.RetireCompleted();
        Observe();
    }

    public async ValueTask DisposeAsync()
    {
        CheckOwner();
        if (_disposed) return;
        await CloseAsync();
        if (_progress is not null) await _progress.DisposeAsync();
        _disposed = true;
        _document.Changed -= Refresh; _document.Closed -= BeginClose;
        _cancellation?.Dispose();
        _basis = null;
    }

    private void CheckOwner()
    {
        if (!ReferenceEquals(SynchronizationContext.Current, _owner))
            throw new InvalidOperationException("Graph coordinator requires its serialized owner context.");
    }
}
