// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal enum UnsavedDecision { Save, Discard, Cancel }
internal sealed record OccurrenceChoice(Id<GraphAttachment> Id, string Label)
{
    public override string ToString() => Label;
}
internal sealed record WorkflowState(ProjectDocument Document, OccurrenceChoice[] Occurrences,
    Id<GraphAttachment>? Selected, decimal? Gain, string Chain, string Source, string StatusKey,
    string? Detail, string? Device, bool Busy, bool CanPlay, bool CanStop, GraphExecutionStatus? Execution)
{
    // Captured on the document owner. GUI never reads mutable history/save fields across threads.
    internal ProjectSnapshot Snapshot { get; } = Document.Current;
    internal string? SavedPath { get; } = Document.SavedPath;
    internal bool IsDirty { get; } = Document.IsDirty;
    internal bool HasSaved { get; } = Document.Saved is not null;
    internal int UndoCount { get; } = Document.UndoCount;
    internal int RedoCount { get; } = Document.RedoCount;
    internal long Generation { get; } = Document.Generation;
    internal bool IsClosed { get; } = Document.IsClosed;
    internal bool SongMode { get; init; }
    internal int ExecutionSampleRate { get; init; } = 48000;
    internal long SongCursor { get; init; }
    internal long LoopStart { get; init; }
    internal long LoopEnd { get; init; }
    internal bool SongLoopEnabled { get; init; }
}

/// <summary>Host application/session owner over the existing document, import, persistence and F2 engine.
/// GUI consumes immutable observations. All document mutations and coordinator operations share one context.</summary>
internal sealed partial class DesktopWorkflow : IAsyncDisposable
{
    private readonly WorkflowOwner _owner = new();
    private readonly SemaphoreSlim _commands = new(1);
    private readonly Func<WorkflowOutput> _openOutput;
    private readonly Func<ProjectDocument, RealtimeSampler, Id<GraphAttachment>, SynchronizationContext,
        GraphExecutionCoordinator> _createCoordinator;
    private readonly string? _mediaDirectory;
    private readonly Timer _poll;
    private ProjectDocument _document;
    private AudioDeviceSession? _deviceSession;
    private WorkflowOutput? _output;
    private GraphExecutionCoordinator? _coordinator;
    private Id<GraphAttachment>? _selected;
    private bool _songMode;
    private string _status = "Workflow.Empty";
    private string? _detail;
    private bool _busy, _closed, _disposed, _audioReady;
    private long _playGeneration;
    private int _pollQueued;
    private WorkflowState _state;
    public WorkflowState State => Volatile.Read(ref _state);
    public event Action<WorkflowState>? Changed;

    internal DesktopWorkflow(ProjectDocument document, Func<WorkflowOutput>? openOutput = null,
        string? mediaDirectory = null,
        Func<ProjectDocument, RealtimeSampler, Id<GraphAttachment>, SynchronizationContext,
            GraphExecutionCoordinator>? createCoordinator = null)
    {
        _document = document;
        _openOutput = openOutput ?? DesktopOutput.Open;
        _createCoordinator = createCoordinator ?? ((doc, sampler, target, owner) => new(doc, sampler, target, owner, voiceCapacity: GraphPreparation.MaximumVoices));
        _mediaDirectory = mediaDirectory;
        _selected = document.Current.State.GraphAttachments.FirstOrDefault()?.Id;
        _songMode = !document.Current.State.ArrangementClips.IsEmpty;
        _state = new(document, [], _selected, null, "", "", _status, null, null, false, false, false, null);
        _document.Changed += DocumentChanged;
        _poll = new(_ =>
        {
            if (Interlocked.CompareExchange(ref _pollQueued, 1, 0) != 0) return;
            _owner.Post(_ =>
            {
                Volatile.Write(ref _pollQueued, 0);
                if (_closed) return;
                try
                {
                    if (_output?.ReadFailure() is { } fault)
                    {
                        _playGeneration++;
                        _coordinator?.Panic();
                        _status = "Workflow.DeviceFault"; _detail = fault;
                        // CloseAsync must keep this owner alive for preparation completion. Never restart here.
                        if (!_busy) _ = CloseFaultAsync();
                    }
                    Publish();
                }
                catch (Exception error) { _status = "Workflow.DeviceFault"; _detail = error.Message; Publish(); }
            }, null);
        }, null, 50, 50);
        _owner.Post(_ => Publish(), null);
    }

    private async Task CloseFaultAsync()
    {
        await _commands.WaitAsync();
        if (_closed || _output is null) { _commands.Release(); return; }
        _busy = true;
        try { await CloseAudioAsync(); }
        catch (Exception error) { _detail += " · " + error.Message; }
        finally { _busy = false; Publish(); _commands.Release(); }
    }

    private Task<bool> Command(string failureKey, Func<Task<bool>> operation) => _owner.Run(async () =>
    {
        await _commands.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            _busy = true; _detail = null; Publish();
            return await operation();
        }
        catch (OperationCanceledException) { _status = "Workflow.Cancelled"; return false; }
        catch (Exception error) { _status = failureKey; _detail = error.Message; return false; }
        finally { _busy = false; Publish(); _commands.Release(); }
    });

    public Task<bool> ImportAsync(string path, CancellationToken cancellation = default)
        => Command("Workflow.ImportFailed", async () =>
        {
            _status = "Workflow.Importing"; Publish();
            using var request = ProjectMedia.BeginImport(_document, Path.GetFileNameWithoutExtension(path),
                releaseMilliseconds: 0m, ownedMediaDirectory: _mediaDirectory);
            // Read one bounded source version; metadata and durable acceptance use the exact same bytes.
            var prepared = await Task.Run(async () =>
            {
                await using var file = File.OpenRead(path);
                var bytes = await WavDecoder.ReadBytesAsync(file, cancellation);
                using var pcm = WavDecoder.Decode(bytes);
                return (Bytes: bytes, pcm.Frames, pcm.SampleRate);
            }, cancellation);
            var duration = ImportedOccurrence.Duration(prepared.Frames, prepared.SampleRate,
                _document.Current.State.Settings.Tempo);
            using var source = new MemoryStream(prepared.Bytes, writable: false);
            await request.PrepareAsync(source, cancellation);
            GraphAttachment? attachment = null;
            request.Accept((edit, media) => attachment = ImportedOccurrence.Create(edit, media,
                Path.GetFileNameWithoutExtension(path), duration), cancellation);
            _playGeneration++;
            _coordinator?.Stop();
            _selected = attachment!.Id;
            _coordinator?.SelectTarget(_selected.Value);
            _status = "Workflow.Ready";
            return true;
        });

    public Task<bool> SetGainAsync(decimal gain) => Command("Workflow.EditFailed", () =>
    {
        if (gain is < 0m or > 1m) throw new ArgumentOutOfRangeException(nameof(gain));
        var attachment = SelectedAttachment() ?? throw new InvalidOperationException("No occurrence selected.");
        var node = _document.Current.State.Graphs.Single(item => item.Id == attachment.GraphId).Nodes
            .Single(item => item.Type == GraphBuiltIns.Gain);
        _document.Edit("Change occurrence Gain", edit => edit.SetGraphGain(attachment.GraphId, node.Id, gain));
        return Task.FromResult(true);
    });

    public Task<bool> SelectAsync(Id<GraphAttachment> target) => Command("Workflow.EditFailed", () =>
    {
        if (!_document.Current.State.GraphAttachments.Any(item => item.Id == target)) return Task.FromResult(false);
        if (!_songMode) { _playGeneration++; _coordinator?.Stop(); }
        _selected = target;
        if (!_songMode) _coordinator?.SelectTarget(target);
        return Task.FromResult(true);
    });

    public Task<bool> UndoAsync(bool redo = false) => Command("Workflow.EditFailed", () =>
        Task.FromResult(redo ? _document.Redo() : _document.Undo()));

    public Task<bool> SaveAsync(string path) => Command("Workflow.SaveFailed", () => Task.FromResult(Save(path)));
    internal Task<bool> ReportFailureAsync(string message) => Command("Workflow.ActionFailed", () =>
        throw new IOException(message));
    internal Task<WorkflowState> ObserveAsync() => _owner.Run(() =>
    { Publish(); return Task.FromResult(State); });

    private bool Save(string path)
    {
        var result = ProjectPersistence.SaveWithReport(_document, path);
        _status = result.IsDegraded ? "Workflow.Degraded" : "Workflow.Saved";
        _detail = string.Join(" · ", result.Diagnostics.Concat(result.GraphReports.SelectMany(r => r.Diagnostics)
            .Select(d => d.Reason)));
        return true;
    }

    private bool CanReplace(UnsavedDecision decision, string? savePath)
    {
        if (decision == UnsavedDecision.Cancel) return false;
        if (!_document.IsDirty || decision == UnsavedDecision.Discard) return true;
        if (string.IsNullOrWhiteSpace(savePath)) return false;
        try { return Save(savePath) && !_document.IsDirty; }
        catch (Exception error)
        {
            _status = "Workflow.SaveFailed"; _detail = error.Message;
            return false;
        }
    }

    public Task<bool> OpenAsync(string path, UnsavedDecision decision, string? savePath = null)
        => decision == UnsavedDecision.Cancel ? Task.FromResult(false) : Command("Workflow.OpenFailed", async () =>
        {
            if (!CanReplace(decision, savePath)) return false;
            // Read/validate candidate before touching current transport or document. Failed Open preserves both.
            var loaded = await Task.Run(() => ProjectPersistence.Open(path));
            try { await CloseAudioAsync(); }
            catch { loaded.Document.Close(); throw; }
            _document.Changed -= DocumentChanged;
            _document.Close();
            _document = loaded.Document;
            _document.Changed += DocumentChanged;
            _selected = _document.Current.State.GraphAttachments.FirstOrDefault()?.Id;
            _songMode = !_document.Current.State.ArrangementClips.IsEmpty;
            _songCursor = _loopStart = _loopEnd = 0; _songLoopEnabled = false;
            _status = loaded.IsDegraded ? "Workflow.Degraded" : "Workflow.Opened";
            _detail = string.Join(" · ", loaded.Diagnostics.Concat(loaded.GraphReports.SelectMany(r => r.Diagnostics)
                .Select(d => d.Reason)));
            return true;
        });

    public Task<bool> PlayAsync() => Command("Workflow.DeviceFault", async () =>
    {
        var attachment = SelectedAttachment();
        if (!_songMode && attachment is null) return false;
        var report = attachment is null ? null : GraphDiagnostics.Inspect(_document.Current.State).Single(item => item.GraphId == attachment.GraphId);
        string? refusal = _songMode ? ArrangementPreparation.Refusal(_document.Current.State) :
            report is { IsEligibleForPreparation: false } ? string.Join(" · ", report.Diagnostics.Select(item => item.Reason)) : null;
        if (refusal is not null)
        {
            _status = "Workflow.Blocked"; _detail = refusal;
            return false;
        }
        // A failed rollback still owns its borrower. Confirm cleanup before opening any replacement.
        if (_output is not null && !_audioReady) await CloseAudioAsync();
        long generation = ++_playGeneration;
        if (_output is null)
        {
            try
            {
                // Retain each acquired owner even if the next composition step fails.
                // Adapter performs all COM on its own worker; open/start stays off the GUI thread.
                _output = _openOutput();
                var session = new AudioDeviceSession();
                session.AttachOutput(_output.Lifetime, _output.Sampler);
                _deviceSession = session;
                _coordinator = _createCoordinator(_document, _output.Sampler, attachment?.Id ?? default, _owner);
                _coordinator.SelectArrangement(_songMode);
                _deviceSession.AttachCoordinator(_coordinator);
                _audioReady = true;
            }
            catch (Exception startup)
            {
                try { await CloseAudioAsync(); }
                catch (Exception rollback)
                { throw new AggregateException("Audio session startup and rollback failed.", startup, rollback); }
                throw;
            }
        }
        _status = "Workflow.Preparing"; Publish();
        _coordinator!.RetryBlockedPreparation();
        _output!.Sampler.SetLooping(!_songMode);
        ApplySongTransport(seek: _songMode);
        for (int wait = 0; wait < 1500 && generation == _playGeneration; wait++)
        {
            var status = _coordinator!.ReadStatus();
            if (_output.ReadFailure() is { } failure) throw new IOException(failure);
            if (status.Blocker is { } blocker)
            { _status = "Workflow.Blocked"; _detail = blocker; return false; }
            if (!status.PreparationPending && _coordinator.Start())
            { _status = "Workflow.Ready"; return true; }
            await Task.Delay(20);
        }
        if (generation == _playGeneration)
        { _status = "Workflow.Blocked"; _detail = "graph.preparation-timeout"; }
        return false;
    });

    // Emergency transport bypasses the async command gate: Stop during preparation cancels pending Start intent.
    public Task<bool> StopAsync(bool panic = false) => _owner.Run(() =>
    {
        _playGeneration++;
        if (panic) _coordinator?.Panic(); else _coordinator?.Stop();
        _status = "Workflow.Stopped"; Publish();
        return Task.FromResult(true);
    });

    private async Task CloseAudioAsync()
    {
        _playGeneration++;
        _audioReady = false;
        // Also join preparation if AttachCoordinator did not transfer ownership.
        if (_coordinator is not null) await _coordinator.CloseAsync();
        if (_deviceSession is not null) await _deviceSession.DisposeAsync();
        else if (_output is { } output)
        {
            // AttachOutput failed without taking ownership. The workflow must still join its borrower.
            output.Sampler.Stop();
            output.Lifetime.Close();
            if (!output.Lifetime.Joined)
                throw new InvalidOperationException("Output did not join; borrowed resources remain owned.");
            output.Sampler.TerminateExecution();
            output.Lifetime.Dispose();
            output.Sampler.Dispose();
        }
        if (_coordinator is not null) await _coordinator.DisposeAsync();
        _deviceSession = null; _coordinator = null; _output = null;
    }

    public async Task<bool> CloseAsync(UnsavedDecision decision, string? savePath = null)
    {
        if (decision == UnsavedDecision.Cancel) return false;
        await StopAsync();
        return await Command("Workflow.CloseFailed", async () =>
        {
            if (!CanReplace(decision, savePath)) return false;
            await CloseAudioAsync();
            _document.Changed -= DocumentChanged;
            _document.Close(); _closed = true;
            await _poll.DisposeAsync();
            return true;
        });
    }

    private GraphAttachment? SelectedAttachment() => _document.Current.State.GraphAttachments
        .SingleOrDefault(item => item.Id == _selected);

    private void DocumentChanged()
    {
        long end = ArrangementPreparation.EndTicks(_document.Current.State);
        _songCursor = Math.Min(_songCursor, end); _loopEnd = Math.Min(_loopEnd, end);
        if (_loopEnd - _loopStart < MusicalPosition.TicksPerQuarter / 4) _songLoopEnabled = false;
        ApplySongTransport();
        if (_songMode && _document.Current.State.ArrangementClips.IsEmpty) { _playGeneration++; _coordinator?.Stop(); }
        if (SelectedAttachment() is null)
        {
            _selected = _document.Current.State.GraphAttachments.FirstOrDefault()?.Id;
            if (!_songMode)
            {
                _coordinator?.Stop();
                if (_selected is { } selected) _coordinator?.SelectTarget(selected);
            }
        }
        Publish();
    }

    private void Publish()
    {
        var state = _document.Current.State;
        var choices = state.GraphAttachments.Select(attachment =>
        {
            var placement = state.Placements.SingleOrDefault(item => item.ItemContextId == attachment.ContextId);
            var pattern = state.Patterns.SingleOrDefault(item => item.Id == placement?.PatternId);
            int index = state.GraphAttachments.IndexOf(attachment) + 1;
            return new OccurrenceChoice(attachment.Id, (pattern?.Name ?? "Item") +
                (state.GraphAttachments.Length > 1 ? $" · {index}" : ""));
        }).ToArray();
        var selected = SelectedAttachment();
        var graph = state.Graphs.SingleOrDefault(item => item.Id == selected?.GraphId);
        var gains = graph?.Nodes.Where(item => item.Type == GraphBuiltIns.Gain && item.StateVersion == 1 &&
            item.Extension is null).ToArray() ?? [];
        decimal? gain = gains.Length == 1 && gains[0].Parameters.TryGetValue(GraphBuiltIns.GainAmplitude, out var value)
            && value.ValueKind == System.Text.Json.JsonValueKind.Number && value.TryGetDecimal(out var amplitude) ? amplitude : null;
        string source = selected is null ? "" : string.Join(" · ", selected.Sources.Select(binding =>
        {
            var part = state.Patterns.SelectMany(item => item.Parts).SingleOrDefault(item => item.Id == binding.PartId);
            var sound = state.Sounds.SingleOrDefault(item => item.Id == part?.SoundId);
            return $"{sound?.Name} · Placement {binding.PlacementId} · Part {binding.PartId}";
        }));
        string chain = graph is null ? "" : DescribeGraph(graph);
        var report = graph is null ? null : GraphDiagnostics.Inspect(state).Single(item => item.GraphId == graph.Id);
        string? songRefusal = _songMode ? ArrangementPreparation.Refusal(state) : null;
        var execution = _coordinator?.ReadStatus();
        string statusKey = _status;
        string? detail = _detail;
        bool actionFailure = _status.EndsWith("Failed", StringComparison.Ordinal) || _status == "Workflow.DeviceFault";
        // Import progress remains observable even while an empty Beat graph has no Source yet.
        bool exportResult = _status is "Workflow.Exporting" or "Workflow.Exported" or "Workflow.ExportClipped";
        if (!actionFailure && !exportResult && !(_busy && _status == "Workflow.Importing"))
        {
            if (execution?.Blocker is { } blocker) { statusKey = "Workflow.Blocked"; detail = blocker; }
            else if (execution?.PreparationPending == true) statusKey = "Workflow.Preparing";
            else if (execution?.Execution.Playing == true) statusKey = "Workflow.Playing";
            else if (execution?.Execution.Transport == SamplerTransportState.Ended) statusKey = "Workflow.Ended";
            if (_songMode && songRefusal is not null) { statusKey = "Workflow.Blocked"; detail = songRefusal; }
            else if (!_songMode && report is { IsEligibleForPreparation: false })
            { statusKey = "Workflow.Blocked"; detail = string.Join(" · ", report.Diagnostics.Select(item => item.Reason)); }
        }
        var observation = new WorkflowState(_document, choices, _selected, gain, chain, source, statusKey,
            detail, _output?.Description, _busy, !_busy && !_closed && (_songMode ? songRefusal is null : report?.IsEligibleForPreparation == true) &&
            !_document.SavedMediaDiagnostics.Any() && execution?.Closed != true,
            !_closed && _output is not null, execution) { SongMode = _songMode, ExecutionSampleRate = _output?.Sampler.SampleRate ?? 48000,
                SongCursor = _songCursor, LoopStart = _loopStart, LoopEnd = _loopEnd, SongLoopEnabled = _songLoopEnabled };
        Volatile.Write(ref _state, observation);
        Changed?.Invoke(observation);
    }

    private static string DescribeGraph(GraphDefinition graph)
    {
        string Label(GraphNode node) => node.Type switch
        {
            GraphBuiltIns.Source => "Source", GraphBuiltIns.Gain => "Gain",
            GraphBuiltIns.Mix => "Mix", GraphBuiltIns.Output => "Output", _ => node.Type
        };
        var sources = graph.Nodes.Where(node => node.Type == GraphBuiltIns.Source).ToArray();
        var source = sources.Length == 1 ? sources[0] : null;
        if (graph.Nodes.Length == 3 && graph.Connections.Length == 2 && source is not null)
        {
            var firstEdges = graph.Connections.Where(edge => edge.FromNodeId == source.Id).ToArray();
            var first = firstEdges.Length == 1 ? firstEdges[0] : null;
            var middle = graph.Nodes.SingleOrDefault(node => node.Id == first?.ToNodeId);
            var secondEdges = graph.Connections.Where(edge => edge.FromNodeId == middle?.Id).ToArray();
            var second = secondEdges.Length == 1 ? secondEdges[0] : null;
            var end = graph.Nodes.SingleOrDefault(node => node.Id == second?.ToNodeId);
            if (middle?.Type == GraphBuiltIns.Gain && end?.Type == GraphBuiltIns.Output)
                return "Source → Gain → Output";
        }
        // Wider imported intent is inspectable without pretending to provide a graphical editor.
        return string.Join(" · ", graph.Nodes.Select(Label));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (!_closed && !await CloseAsync(UnsavedDecision.Discard))
            throw new InvalidOperationException(State.Detail ?? "Workflow shutdown did not complete.");
        await _owner.DisposeAsync();
        _commands.Dispose();
        _disposed = true;
    }
}
