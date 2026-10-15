// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Extensions;

namespace Seqvium.Desktop.Workflow;

internal enum LabAcceptance { Resource, Rack, Playlist, SharedSound }
internal sealed record LabEntry(Guid Id, string Name, SampleRecipe Recipe, AudioWaveform Waveform, int Rate, int Frames, int Accepted = 0)
{
    internal ImmutableArray<Id<ResourceDescriptor>> AcceptedResources { get; init; } = [];
}
internal sealed record LabTarget(Guid Lifecycle, Id<GraphAttachment> Attachment, Id<Pattern> Pattern,
    Id<MusicalPart> Part, SoundDefinition Sound, GraphSourceBinding Binding, string Label);
internal sealed record SampleLabState(ImmutableArray<SampleFamily> Families, string Family, ImmutableArray<LabEntry> Entries,
    Guid? Selected, Guid? Reference, ImmutableDictionary<string, decimal> Locks, bool Busy, string Message,
    string? Target, bool TargetValid, int SharedUses, Guid? Audible, bool Contextual);

internal sealed partial class DesktopWorkflow
{
    private readonly SampleCandidateHistory _labHistory = new();
    private readonly Dictionary<Guid, LabEntry> _labMetadata = [];
    private ImmutableArray<SampleFamily> _labFamilies = [];
    private readonly List<Task> _labWork = [];
    private CancellationTokenSource? _labCancellation;
    private long _labSequence, _labSelectionVersion;
    private bool _labBusy, _labTargetStale;
    private string _labMessage = "Lab.Start";
    private LabTarget? _labTarget;
    private Guid? _labAudible;
    private bool _labContextual;
    private int _labCandidateNumber;
    private long _labAuditionSequence;

    private SampleLabState ObserveLab() => new(_labFamilies, _labHistory.Family ?? "Kick",
        [.. _labHistory.Items.Select(c => _labMetadata[c.Id] with
        {
            Accepted = _labMetadata[c.Id].AcceptedResources.Count(id => _document.Current.State.Resources.Any(r => r.Id == id))
        })], _labHistory.Selected, _labHistory.Reference,
        _labHistory.Locks, _labBusy, _labMessage, _labTarget?.Label, LabTargetValid(),
        _labTarget is { } target ? _document.Current.State.Patterns.Sum(p => p.Parts.Count(part => part.SoundId == target.Sound.Id)) : 0,
        _labAudible, _labContextual);

    internal Task<bool> RefreshLabGeneratorAsync() => _owner.Run(async () =>
    {
        if (_closed) return false;
        try
        {
            await using var session = new SampleGenerationSession(Extensions, PercussionGenerator.Package, PercussionGenerator.Capability);
            _labFamilies = session.Families;
            _labHistory.SetFamily(_labHistory.Family ?? _labFamilies[0].Id);
            if (_labMessage == "Lab.Unavailable") _labMessage = "Lab.Start";
            return true;
        }
        catch (InvalidOperationException) { _labMessage = "Lab.Unavailable"; return false; }
        finally { Publish(); }
    });

    internal Task<bool> ConfigureLabAsync(string? family = null, Guid? selection = null, Guid? reference = null,
        string? control = null, decimal? held = null, bool changeLock = false) => _owner.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        if (family is not null && family != _labHistory.Family)
        {
            if (!_labFamilies.Any(f => f.Id == family)) throw new InvalidOperationException("Lab.FamilyUnavailable");
            CancelLabRequest(); _labHistory.SetFamily(family); _labMessage = "Lab.FamilyChanged";
        }
        if (selection is { } chosen)
        {
            _labSelectionVersion++; ClearLabAudition(); _noteOutput?.Sampler.Stop(); _labHistory.Select(chosen);
        }
        if (reference is { } basis) { _labHistory.SetReference(basis); _labMessage = "Lab.ReferenceSet"; }
        if (changeLock && control is not null)
        {
            var descriptor = _labFamilies.Single(f => f.Id == _labHistory.Family).Controls.Single(c => c.Id == control);
            if (held is { } v && (v < descriptor.Minimum || v > descriptor.Maximum)) throw new ArgumentOutOfRangeException(nameof(held));
            _labHistory.SetLock(control, held);
        }
        Publish(); return Task.FromResult(true);
    });

    internal Task<bool> GenerateLabAsync(SampleExplorationKind kind, int? seed = null) => _owner.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        _labWork.RemoveAll(t => t.IsCompleted);
        if (_busy || _labWork.Count >= 4) return Task.FromResult(false);
        var task = GenerateLabCoreAsync(kind, seed ?? Random.Shared.Next());
        _labWork.Add(task);
        return task;
    });

    private async Task<bool> GenerateLabCoreAsync(SampleExplorationKind kind, int seed)
    {
        CancelLabRequest();
        long sequence = _labSequence, selectionVersion = _labSelectionVersion;
        Guid lifecycle = _document.LifecycleId;
        using var cancellation = new CancellationTokenSource();
        _labCancellation = cancellation;
        _labBusy = true; _labMessage = "Lab.Generating"; Publish();
        SampleCandidate? result = null;
        try
        {
            await using var session = new SampleGenerationSession(Extensions, PercussionGenerator.Package, PercussionGenerator.Capability);
            _labFamilies = session.Families;
            _labHistory.SetFamily(_labHistory.Family ?? "Kick");
            var reference = _labHistory.ReferenceCandidate;
            var exploration = new SampleExploration(_labHistory.Family!, kind, seed, reference?.Recipe, reference?.Id, _labHistory.Locks);
            var context = new SampleGenerationContext(lifecycle, 0, 48000, 48000 * 2);
            bool produced = await session.GenerateAsync(new(context, ImmutableDictionary<string, decimal>.Empty) { Exploration = exploration }, cancellation.Token);
            if (!produced)
            {
                if (session.Failure is { } failure) throw new InvalidOperationException(failure);
                return false;
            }
            result = session.TakeCurrent(context);
            var candidate = result;
            var metadata = await Task.Run(async () =>
            {
                using var wave = candidate.OpenWave();
                var bytes = await WavDecoder.ReadBytesAsync(wave, cancellation.Token);
                using var pcm = WavDecoder.Decode(bytes);
                return new LabEntry(candidate.Id, candidate.Name, candidate.Recipe ?? throw new InvalidDataException("Extension.InvalidResult"),
                    AudioWaveform.FromPcm(pcm, cancellation.Token), pcm.SampleRate, pcm.Frames);
            }, cancellation.Token);
            if (_closed || lifecycle != _document.LifecycleId || sequence != _labSequence || cancellation.IsCancellationRequested) return false;
            _labMetadata.Add(result.Id, metadata with { Name = metadata.Recipe.Family + " " + (++_labCandidateNumber).ToString("00", System.Globalization.CultureInfo.InvariantCulture) });
            _labHistory.Add(result, select: selectionVersion == _labSelectionVersion);
            result = null;
            var retained = _labHistory.Items.Select(c => c.Id).ToHashSet();
            foreach (var id in _labMetadata.Keys.Where(id => !retained.Contains(id)).ToArray()) _labMetadata.Remove(id);
            if (selectionVersion == _labSelectionVersion) { ClearLabAudition(); _noteOutput?.Sampler.Stop(); }
            _labMessage = "Lab.Ready";
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch (Exception error)
        {
            if (sequence == _labSequence) _labMessage = error.Message.StartsWith("Lab.", StringComparison.Ordinal) ||
                error.Message.StartsWith("Extension.", StringComparison.Ordinal) ? error.Message : "Lab.Failed";
            return false;
        }
        finally
        {
            result?.Dispose();
            if (sequence == _labSequence) { _labBusy = false; _labCancellation = null; Publish(); }
        }
    }

    private void CancelLabRequest()
    { _labSequence++; _labCancellation?.Cancel(); _labCancellation = null; _labBusy = false; }

    internal Task<bool> CancelLabAsync() => _owner.Run(() =>
    {
        CancelLabRequest(); ClearLabAudition(); _noteOutput?.Sampler.Stop(); _labMessage = "Workflow.Cancelled";
        Publish(); return Task.FromResult(true);
    });

    internal Task<bool> SetLabTargetAsync(Id<GraphAttachment>? attachment = null, Id<MusicalPart>? part = null) => _owner.Run(() =>
    {
        ClearLabAudition(); _labTarget = null; _labTargetStale = false;
        if (attachment is { } id && part is { } partId)
        {
            var state = _document.Current.State;
            var target = state.GraphAttachments.Single(a => a.Id == id);
            var placement = state.Placements.Single(p => p.ItemContextId == target.ContextId);
            var pattern = state.Patterns.Single(p => p.Id == placement.PatternId);
            var channel = pattern.Parts.Single(p => p.Id == partId);
            var sound = state.Sounds.Single(s => s.Id == channel.SoundId);
            if (sound.Algorithm != PcmSampler.Algorithm || !BuiltInSound.Supports(sound))
            { _labMessage = "Lab.ContextUnavailable"; Publish(); return Task.FromResult(false); }
            _labTarget = new(_document.LifecycleId, id, pattern.Id, partId, sound,
                target.Sources.Single(s => s.PartId == partId), pattern.Name == channel.Name ? channel.Name : pattern.Name + " / " + channel.Name);
        }
        _labMessage = "Lab.Start"; Publish(); return Task.FromResult(true);
    });

    private bool LabTargetValid()
    {
        if (_labTargetStale || _labTarget is not { } target || target.Lifecycle != _document.LifecycleId) return false;
        var state = _document.Current.State;
        return state.Sounds.Any(s => s == target.Sound) && state.Patterns.Any(p => p.Id == target.Pattern &&
            p.Parts.Any(part => part.Id == target.Part && part.SoundId == target.Sound.Id)) &&
            state.GraphAttachments.Any(a => a.Id == target.Attachment && a.Sources.Contains(target.Binding));
    }
    private void ClearLabAudition()
    {
        _labAuditionSequence++;
        if (_labContextual && _coordinator is not null) _coordinator.SetSampleAudition(default, default, null);
        _labAudible = null; _labContextual = false;
    }
    private void LabDocumentChanged()
    {
        if (_labTarget is not null && !LabTargetValid())
        { _labTargetStale = true; ClearLabAudition(); _labMessage = "Lab.TargetStale"; }
    }

    internal Task<bool> AuditionLabAsync(Guid candidateId, bool contextual) => Command("Lab.Failed", async () =>
    {
        var candidate = _labHistory.Items.Single(c => c.Id == candidateId && c.IsAvailable);
        ClearLabAudition();
        if (contextual)
        {
            if (!LabTargetValid() || _coordinator is null || _coordinator.ReadStatus().Execution.Playing != true ||
                !_songMode && _selected != _labTarget!.Attachment) throw new InvalidOperationException("Lab.PlayContext");
            await CloseNoteAuditionAsync();
        }
        else
        {
            if (_coordinator?.ReadStatus().Execution.Playing == true) throw new InvalidOperationException("Browser.StopTransport");
            await CloseNoteAuditionAsync();
        }
        long intent = _playGeneration;
        using var stream = candidate.OpenWave();
        var bytes = await WavDecoder.ReadBytesAsync(stream);
        using var pcm = await Task.Run(() => WavDecoder.Decode(bytes));
        if (intent != _playGeneration || !_labHistory.Items.Contains(candidate)) return false;
        if (contextual)
        {
            if (!LabTargetValid()) throw new InvalidOperationException("Lab.TargetStale");
            _coordinator!.SetSampleAudition(_labTarget!.Sound.Id, candidateId, pcm);
            _labContextual = true;
            long auditionSequence = _labAuditionSequence;
            try
            {
                for (int attempt = 0; attempt < 300; attempt++)
                {
                    if (intent != _playGeneration || !LabTargetValid() || auditionSequence != _labAuditionSequence)
                    { ClearLabAudition(); return false; }
                    if (_coordinator.ReadStatus().Blocker is { } blocker) throw new InvalidOperationException(blocker);
                    if (_output!.Sampler.SampleAuditionCandidate == candidateId) break;
                    if (attempt == 299) throw new InvalidOperationException("Lab.ContextUnavailable");
                    await Task.Delay(10);
                }
            }
            catch { ClearLabAudition(); throw; }
        }
        else
        {
            _noteOutput = _openOutput();
            try
            {
                _noteSession = new(); _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
                _browserAudition = new(_noteOutput.Sampler, _document);
                _noteOutput.Sampler.SetGain(.25f);
                using var request = _browserAudition.BeginExternal(Path.Combine(Path.GetTempPath(), "seqvium-lab.wav"));
                await request.PrepareAsync(pcm);
                if (intent != _playGeneration || request.Publish() != SamplerPublication.Accepted)
                { await CloseNoteAuditionAsync(); return false; }
            }
            catch { await CloseNoteAuditionAsync(); throw; }
        }
        _labAudible = candidateId; _labContextual = contextual; _labMessage = contextual ? "Lab.InContext" : "Lab.RawPlaying";
        return true;
    });

    internal Task<bool> RestoreLabSoundAsync() => _owner.Run(() =>
    { ClearLabAudition(); _noteOutput?.Sampler.Stop(); _labMessage = "Lab.Original"; Publish(); return Task.FromResult(true); });

    internal Task<bool> AcceptLabAsync(Guid lifecycle, Guid candidateId, LabAcceptance scope,
        Id<GraphAttachment>? rack = null) => Command("Lab.Failed", async () =>
    {
        if (lifecycle != _document.LifecycleId) throw new InvalidOperationException("Extension.StaleResult");
        var candidate = _labHistory.Items.Single(c => c.Id == candidateId && c.IsAvailable);
        var info = _labMetadata[candidateId];
        var basis = new ArrangementEditBasis(_document.LifecycleId, _document.Generation);
        var target = scope == LabAcceptance.SharedSound && LabTargetValid() ? _labTarget : null;
        if (scope == LabAcceptance.SharedSound && target is null) throw new InvalidOperationException("Lab.TargetStale");
        var state = _document.Current.State;
        var attachment = rack is { } rackId ? state.GraphAttachments.Single(a => a.Id == rackId) : null;
        var placement = attachment is null ? null : state.Placements.Single(p => p.ItemContextId == attachment.ContextId);
        var pattern = placement is null ? null : state.Patterns.Single(p => p.Id == placement.PatternId);
        if (scope == LabAcceptance.Rack && attachment is not null &&
            BeatChannelRefusal(state, pattern!, attachment, state.Graphs.Single(g => g.Id == attachment.GraphId)) is { } refusal)
            throw new InvalidOperationException(refusal);
        using var request = ProjectMedia.BeginImport(_document, info.Name,
            rootPitch: target?.Sound.Parameters["rootPitch"] ?? 60,
            releaseMilliseconds: target?.Sound.Parameters["releaseMilliseconds"] ?? 0,
            target: target?.Sound.Id, ownedMediaDirectory: _mediaDirectory);
        using var wave = candidate.OpenWave();
        await request.PrepareAsync(wave);
        ValidateArrangementBasis(basis);
        if (!_labHistory.Items.Contains(candidate) || target is not null && !LabTargetValid())
            throw new InvalidOperationException("Extension.StaleResult");
        GraphAttachment? created = null;
        Id<SoundDefinition>? playlistSound = null;
        var accepted = request.Accept((edit, media) =>
        {
            if (scope == LabAcceptance.Rack)
            {
                if (attachment is null)
                {
                    // A new Rack starts with a musical bar and one native-pitch hit, ready for Play.
                    created = ImportedOccurrence.Create(edit, media, info.Name,
                        new MusicalDuration(Math.Max(BeatSteps.BarTicks,
                            ImportedOccurrence.Duration(info.Frames, info.Rate, state.Settings.Tempo).Ticks)));
                }
                else AttachBeatSound(edit, pattern!, placement!, attachment,
                    state.Graphs.Single(g => g.Id == attachment.GraphId), media.SoundId, info.Name);
            }
            if (scope == LabAcceptance.Playlist)
            {
                var sound = edit.AddSound(info.Name, AudioClipSource.Algorithm, [media.ResourceId]);
                edit.SetSoundParameters(sound, ImmutableDictionary<string, decimal>.Empty
                    .Add("sampleRate", info.Rate).Add("frames", info.Frames).Add("channels", 1));
                edit.AddAudioClip(sound, new(_songCursor), 0, 0, info.Frames); playlistSound = sound;
            }
        });
        _labMetadata[candidateId] = info with { AcceptedResources = info.AcceptedResources.Add(accepted.ResourceId) };
        if (playlistSound is { } audio) _audioWaveforms = _audioWaveforms.SetItem(audio, info.Waveform);
        if (created is not null) { _selected = created.Id; _coordinator?.SelectTarget(created.Id); }
        if (scope == LabAcceptance.Playlist && state.Patterns.IsEmpty) { _songMode = true; _coordinator?.SelectArrangement(true); }
        _labMessage = "Lab.Accepted"; _status = "Workflow.Ready";
        return true;
    });

    private async Task CloseLabAsync()
    {
        CancelLabRequest(); ClearLabAudition();
        await Task.WhenAll(_labWork); _labWork.Clear();
        _labHistory.Dispose(); _labMetadata.Clear(); _labTarget = null; _labTargetStale = false; _labMessage = "Lab.Start";
    }
}
