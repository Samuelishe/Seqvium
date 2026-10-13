// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workspace;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    internal Task<bool> AddDrumVoiceAsync(BeatEditBasis basis, string name) =>
        AddDrumVoiceAsync(basis, name, DrumVoice.Algorithm);

    internal Task<bool> AddDrumVoiceAsync(BeatEditBasis basis, string name, string algorithm) => Command(
        "Workflow.EditFailed", () =>
        {
            var (pattern, placement, attachment, graph) = ValidateBeatBasis(basis);
            if (BeatChannelRefusal(_document.Current.State, pattern, attachment, graph) is { } refusal)
                throw new InvalidOperationException(refusal);
            bool changed = _document.Edit("Add Drum Voice", edit =>
            {
                var instrument = DrumInstruments.Find(algorithm) ?? throw new NotSupportedException("Unknown drum.");
                var sound = edit.AddSound(name, algorithm);
                edit.SetSoundParameters(sound, instrument.Defaults);
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

    internal Task<bool> AuditionDrumAsync(NodeEditorTarget target, long generation, decimal pitch, decimal intensity)
    {
        var observation = State;
        var state = observation.Snapshot.State;
        if (observation.Document.LifecycleId != target.LifecycleId || observation.Generation != generation ||
            DrumEditorSession.Resolve(state, target) is not { } sound || !DrumVoice.Supports(sound))
            return Task.FromResult(false);
        // Audition uses explicit source identity, even if another Pattern is selected for transport.
        return AuditionDrumSourceAsync(target, generation, sound.Id, pitch, intensity);
    }

    private Task<bool> AuditionDrumSourceAsync(NodeEditorTarget target, long generation,
        Id<SoundDefinition> sound, decimal pitch, decimal intensity) => Command("Workflow.ActionFailed", async () =>
    {
        if (_document.LifecycleId != target.LifecycleId || _document.Generation != generation ||
            DrumEditorSession.Resolve(_document.Current.State, target)?.Id != sound)
            throw new InvalidOperationException("Workflow.EditStale");
        if (_coordinator?.ReadStatus().Execution.Playing == true) return false;
        await CloseNoteAuditionAsync();
        long playGeneration = _playGeneration;
        _noteOutput = _openOutput();
        try
        {
            _noteSession = new();
            _noteSession.AttachOutput(_noteOutput.Lifetime, _noteOutput.Sampler);
            _noteOutput.Sampler.SetGain(.12f);
            using var request = new NoteAuditionRequest(_noteOutput.Sampler, _document, sound, pitch, intensity);
            await request.PrepareAsync();
            if (_document.Generation != generation || _playGeneration != playGeneration ||
                _document.LifecycleId != target.LifecycleId || request.Publish() != SamplerPublication.Accepted)
            {
                await CloseNoteAuditionAsync();
                return false;
            }

            return true;
        }
        catch
        {
            await CloseNoteAuditionAsync();
            throw;
        }
    });
}
