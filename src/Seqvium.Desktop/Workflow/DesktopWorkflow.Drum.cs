// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    private bool _drumAuditionOutput;
    private SoundDefinition? _drumAuditionSource;
    private decimal _drumAuditionPitch, _drumAuditionIntensity;
    private sealed record DrumAuditionIntent(NodeEditorTarget Target, long Generation, Id<SoundDefinition> Sound,
        decimal Pitch, decimal Intensity, long Transport, long Serial, TaskCompletionSource<bool> Completion);
    private DrumAuditionIntent? _pendingDrumAudition;
    private long _drumAuditionSerial;
    private Task _drumAuditionDrain = Task.CompletedTask;

    internal Task<bool> CancelDrumAuditionAsync() => _closed || _disposed ? Task.FromResult(false) : _owner.Run(() =>
    {
        InvalidateDrumAuditions();
        return Task.FromResult(true);
    });

    private void InvalidateDrumAuditions()
    {
        _drumAuditionSerial++;
        _pendingDrumAudition?.Completion.TrySetResult(false);
        _pendingDrumAudition = null;
        if (_drumAuditionOutput) _noteOutput?.Sampler.Stop();
    }

    private async Task DrainDrumAuditionsAsync()
    {
        while (_pendingDrumAudition is { } intent)
        {
            _pendingDrumAudition = null;
            try
            {
                intent.Completion.TrySetResult(await AuditionDrumSourceAsync(intent.Target, intent.Generation,
                    intent.Sound, intent.Pitch, intent.Intensity, intent.Transport, intent.Serial));
            }
            catch (Exception error) { intent.Completion.TrySetException(error); }
        }
    }
    internal Task<bool> AddDrumVoiceAsync(BeatEditBasis basis, string name) =>
        AddDrumVoiceAsync(basis, name, DrumVoice.Algorithm);

    internal Task<bool> AddDrumVoiceAsync(BeatEditBasis basis, string name, string algorithm) => Command(
        "Workflow.EditFailed", () =>
        {
            var (pattern, placement, attachment, graph) = ValidateBeatBasis(basis);
            if (BeatChannelRefusal(_document.Current.State, pattern, attachment, graph) is { } refusal)
                throw new InvalidOperationException(refusal);
            bool changed = _document.Edit(algorithm == SynthVoice.Algorithm ? "Add Synth" : "Add Drum Voice", edit =>
            {
                var defaults = algorithm == SynthVoice.Algorithm ? SynthVoice.Presets[0].Values :
                    (DrumInstruments.Find(algorithm) ?? throw new NotSupportedException("Unknown instrument.")).Defaults;
                var sound = edit.AddSound(name, algorithm);
                edit.SetSoundParameters(sound, defaults);
                var part = edit.AddPart(pattern.Id, name, sound);
                var source = edit.AddGraphNode(graph.Id, GraphBuiltIns.Source, new(0, pattern.Parts.Length * 120));
                edit.BindGraphSource(attachment.Id, source.Id, placement.Id, part);
                var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
                var input = gain.Ports.Single(p => p.Direction == GraphPortDirection.Input);
                if (pattern.Parts.IsEmpty)
                    edit.ConnectGraphPorts(graph.Id, source.Id, source.Ports.Single().Id, gain.Id, input.Id);
                else
                {
                    var cable = graph.Connections.Single(c => c.ToNodeId == gain.Id);
                    var mix = edit.AddGraphNode(graph.Id, GraphBuiltIns.Mix, new(180, pattern.Parts.Length * 120));
                    var slots = mix.Ports.Where(p => p.Direction == GraphPortDirection.Input).ToArray();
                    edit.DisconnectGraphPorts(graph.Id, cable.Id);
                    edit.ConnectGraphPorts(graph.Id, cable.FromNodeId, cable.FromPortId, mix.Id, slots[0].Id);
                    edit.ConnectGraphPorts(graph.Id, source.Id, source.Ports.Single().Id, mix.Id, slots[1].Id);
                    edit.ConnectGraphPorts(graph.Id, mix.Id,
                        mix.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id,
                        gain.Id, input.Id);
                    edit.SetContextMix(attachment.ContextId, true);
                }
            });
            _status = "Workflow.Ready";
            return Task.FromResult(changed);
        });

    internal Task<bool> SetDrumParametersAsync(DrumProposal proposal) => Command("Workflow.EditFailed", () =>
    {
        if (_document.LifecycleId != proposal.Target.LifecycleId || _document.Generation != proposal.Generation)
            throw new InvalidOperationException("Workflow.EditStale");
        var sound = DrumEditorSession.Resolve(_document.Current.State, proposal.Target);
        if (sound?.Id != proposal.SoundId || !DrumVoice.Supports(sound) ||
            !sound.Parameters.OrderBy(p => p.Key).SequenceEqual(proposal.Before.OrderBy(p => p.Key)))
            throw new InvalidOperationException("Workflow.EditStale");
        DrumInstruments.Validate(sound.Algorithm, proposal.After);
        bool changed = _document.Edit("Shape Drum Voice", edit => edit.SetSoundParameters(sound.Id, proposal.After));
        _status = "Workflow.Ready";
        return Task.FromResult(changed);
    });

    internal async Task<bool> ShapeAndAuditionDrumAsync(DrumProposal proposal, decimal pitch, decimal intensity)
    {
        long auditionSerial = Volatile.Read(ref _drumAuditionSerial);
        long transport = State.TransportGeneration;
        bool changed = await SetDrumParametersAsync(proposal);
        if (!changed) return false;
        var current = State;
        if (current.Generation == proposal.Generation + 1 && current.TransportGeneration == transport)
            await AuditionDrumAsync(proposal.Target, current.Generation, pitch, intensity, transport, auditionSerial);
        return true; // A failed/unavailable audition must not undo an accepted musical edit.
    }

    internal async Task<bool> AuditionDrumAsync(NodeEditorTarget target, long generation, decimal pitch, decimal intensity,
        long? expectedTransportGeneration = null, long? expectedAuditionSerial = null)
    {
        var observation = State;
        var state = observation.Snapshot.State;
        if (_disposed || _closed || observation.Document.LifecycleId != target.LifecycleId || observation.Generation != generation ||
            expectedTransportGeneration is { } expected && observation.TransportGeneration != expected ||
            DrumEditorSession.Resolve(state, target) is not { } sound || !DrumVoice.Supports(sound))
            return false;
        // Audition uses explicit source identity, even if another Pattern is selected for transport.
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _owner.Run(() =>
        {
            if (_closed || observation.TransportGeneration != _playGeneration ||
                expectedAuditionSerial is { } serial && serial != _drumAuditionSerial)
                completion.TrySetResult(false);
            else
            {
                _pendingDrumAudition?.Completion.TrySetResult(false);
                _pendingDrumAudition = new(target, generation, sound.Id, pitch, intensity,
                    observation.TransportGeneration, ++_drumAuditionSerial, completion);
                if (_drumAuditionDrain.IsCompleted) _drumAuditionDrain = DrainDrumAuditionsAsync();
            }
            return Task.FromResult(true);
        });
        return await completion.Task;
    }

    private Task<bool> AuditionDrumSourceAsync(NodeEditorTarget target, long generation,
        Id<SoundDefinition> sound, decimal pitch, decimal intensity, long expectedTransportGeneration, long serial) => Command("Workflow.ActionFailed", async () =>
    {
        if (serial != _drumAuditionSerial || expectedTransportGeneration != _playGeneration) return false;
        if (_document.LifecycleId != target.LifecycleId || _document.Generation != generation ||
            DrumEditorSession.Resolve(_document.Current.State, target)?.Id != sound)
            throw new InvalidOperationException("Workflow.EditStale");
        if (_coordinator?.ReadStatus().Execution.Playing == true) return false;
        long playGeneration = expectedTransportGeneration;
        try
        {
            if (!_drumAuditionOutput || _noteOutput is null || _noteOutput.Lifetime.Joined || _noteOutput.ReadFailure() is not null)
            {
                await CloseNoteAuditionAsync(preserveDrumRequests: true);
                _noteOutput = _openOutput();
                _noteSession = new();
                _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
                _noteOutput.Sampler.SetGain(.12f);
                _drumAuditionOutput = true;
            }
            var source = DrumEditorSession.Resolve(_document.Current.State, target)!;
            _noteOutput.Sampler.RetireCompleted();
            if (_drumAuditionSource is { } cached && cached.Id == source.Id && cached.Algorithm == source.Algorithm &&
                cached.Parameters.OrderBy(p => p.Key).SequenceEqual(source.Parameters.OrderBy(p => p.Key)) &&
                _drumAuditionPitch == pitch && _drumAuditionIntensity == intensity &&
                _noteOutput.Sampler.RetriggerAudition())
                return true;
            using var request = new NoteAuditionRequest(_noteOutput.Sampler, _document, sound, pitch, intensity);
            await request.PrepareAsync();
            // A slow preparation never turns later clicks into a backlog of old hits.
            if (serial != _drumAuditionSerial && _playGeneration == playGeneration &&
                _document.Generation == generation && _document.LifecycleId == target.LifecycleId)
                return false;
            if (_document.Generation != generation || _playGeneration != playGeneration ||
                _document.LifecycleId != target.LifecycleId || request.Publish() != SamplerPublication.Accepted)
            {
                await CloseNoteAuditionAsync();
                return false;
            }
            _drumAuditionSource = source;
            _drumAuditionPitch = pitch;
            _drumAuditionIntensity = intensity;
            return true;
        }
        catch
        {
            await CloseNoteAuditionAsync();
            throw;
        }
    });
}
