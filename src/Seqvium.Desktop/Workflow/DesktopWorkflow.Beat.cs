// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    internal Task<bool> SetTempoAsync(Guid lifecycle, long generation, decimal bpm) => Command("Workflow.EditFailed",
        () =>
        {
            if (_document.LifecycleId != lifecycle || _document.Generation != generation)
                throw new InvalidOperationException("Workflow.EditStale");
            var tempo = new Tempo(bpm);
            var settings = _document.Current.State.Settings;
            if (settings.Tempo == tempo) return Task.FromResult(false);
            _playGeneration++;
            _coordinator?.Stop();
            bool changed = _document.Edit("Change tempo", edit => edit.SetSettings(settings with { Tempo = tempo }));
            _status = "Workflow.Ready";
            return Task.FromResult(changed);
        });

    private (Pattern Pattern, PatternPlacement Placement, GraphAttachment Attachment, GraphDefinition Graph)
        ValidateBeatBasis(BeatEditBasis basis, bool requireGrid = true)
    {
        if (_closed || _document.LifecycleId != basis.Lifecycle || _document.Generation != basis.Generation ||
            _selected != basis.Attachment) throw new InvalidOperationException("Beat.Stale");
        var state = _document.Current.State;
        var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == basis.Attachment)
                         ?? throw new InvalidOperationException("Beat.Stale");
        var placement =
            state.Placements.SingleOrDefault(p =>
                p.ItemContextId == attachment.ContextId && p.PatternId == basis.Pattern)
            ?? throw new InvalidOperationException("Beat.Stale");
        var pattern = state.Patterns.Single(p => p.Id == basis.Pattern);
        if (requireGrid && BeatSteps.PatternRefusal(state, pattern) is { } refusal)
            throw new InvalidOperationException(refusal);
        return (pattern, placement, attachment, state.Graphs.Single(g => g.Id == attachment.GraphId));
    }

    internal Task<bool> CreateBeatAsync(Guid lifecycle, long generation, string name) => Command("Workflow.EditFailed",
        () =>
        {
            if (_document.LifecycleId != lifecycle || _document.Generation != generation)
                throw new InvalidOperationException("Beat.Stale");
            if (_document.Current.State.Settings.Meter is not { Numerator: 4, Denominator: 4 })
                throw new InvalidOperationException("Beat.MeterUnsupported");
            GraphAttachment? created = null;
            _document.Edit("Create Beat Pattern", edit =>
            {
                var pattern = edit.AddPattern(name, new(BeatSteps.BarTicks));
                var placement = edit.AddPlacement(pattern, default);
                created = edit.CreateItemGraph(placement);
                var gain = edit.AddGraphNode(created.GraphId, GraphBuiltIns.Gain, new(360, 0));
                var output = edit.AddGraphNode(created.GraphId, GraphBuiltIns.Output, new(540, 0));
                edit.SetGraphGain(created.GraphId, gain.Id, 0.25m);
                edit.ConnectGraphPorts(created.GraphId, gain.Id,
                    gain.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id,
                    output.Id, output.Ports.Single().Id);
                edit.SetGraphOutput(created.Id, output.Id);
            });
            _playGeneration++;
            _coordinator?.Stop();
            _selected = created!.Id;
            _coordinator?.SelectTarget(_selected.Value);
            _status = "Workflow.Ready";
            return Task.FromResult(true);
        });

    internal Task<bool> RenameBeatAsync(BeatEditBasis basis, string name) => Command("Workflow.EditFailed", () =>
    {
        var (pattern, _, _, _) = ValidateBeatBasis(basis);
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Beat.NameRequired");
        bool changed = _document.Edit("Rename Pattern", edit => edit.RenamePattern(pattern.Id, name));
        _status = "Workflow.Ready";
        return Task.FromResult(changed);
    });

    internal Task<bool> ToggleBeatStepAsync(BeatEditBasis basis, Id<MusicalPart> partId, int step)
        => Command("Workflow.EditFailed", () =>
        {
            var (pattern, _, _, _) = ValidateBeatBasis(basis);
            var part = pattern.Parts.SingleOrDefault(p => p.Id == partId) ??
                       throw new InvalidOperationException("Beat.Stale");
            if (BeatSteps.PartRefusal(_document.Current.State, part, pattern.Length.Ticks) is { } refusal)
                throw new InvalidOperationException(refusal);
            if (step < 0 || step >= BeatSteps.StepCount(pattern)) throw new ArgumentOutOfRangeException(nameof(step));
            var position = BeatSteps.Position(step);
            var existing = part.Notes.SingleOrDefault(n => n.Position == position);
            var root = _document.Current.State.Sounds.Single(s => s.Id == part.SoundId).Parameters
                .GetValueOrDefault("rootPitch", 60);
            bool changed = _document.Edit(existing is null ? "Enable beat step" : "Disable beat step", edit =>
            {
                if (existing is null)
                    edit.AddNote(pattern.Id, part.Id, position, BeatSteps.Duration(step, pattern.Length.Ticks), root,
                        1m);
                else edit.DeleteNote(pattern.Id, part.Id, existing.Id);
            });
            _status = "Workflow.Ready";
            return Task.FromResult(changed);
        });

    /// <summary>Publish one row gesture or rhythm operation as one canonical Undo transaction.</summary>
    internal Task<bool> SetBeatStepsAsync(BeatEditBasis basis, Id<MusicalPart> partId, IReadOnlyList<bool> steps)
    {
        // Own the submitted value before crossing to the workflow thread.
        var desired = steps.ToArray();
        return Command("Workflow.EditFailed", () =>
        {
            var (pattern, _, _, _) = ValidateBeatBasis(basis);
            if (desired.Length != BeatSteps.StepCount(pattern))
                throw new ArgumentException("Step count must match Pattern length.", nameof(steps));
            var part = pattern.Parts.SingleOrDefault(p => p.Id == partId) ??
                       throw new InvalidOperationException("Beat.Stale");
            if (BeatSteps.PartRefusal(_document.Current.State, part, pattern.Length.Ticks) is { } refusal)
                throw new InvalidOperationException(refusal);
            var root = _document.Current.State.Sounds.Single(s => s.Id == part.SoundId).Parameters
                .GetValueOrDefault("rootPitch", 60);
            bool differs = Enumerable.Range(0, desired.Length)
                .Any(i => desired[i] != part.Notes.Any(n => BeatSteps.Step(n) == i));
            if (!differs) return Task.FromResult(false);
            bool changed = _document.Edit("Edit channel rhythm", edit =>
            {
                for (int i = 0; i < desired.Length; i++)
                {
                    var existing = part.Notes.SingleOrDefault(n => BeatSteps.Step(n) == i);
                    if (desired[i] && existing is null)
                        edit.AddNote(pattern.Id, part.Id, BeatSteps.Position(i),
                            BeatSteps.Duration(i, pattern.Length.Ticks), root, 1m);
                    else if (!desired[i] && existing is not null) edit.DeleteNote(pattern.Id, part.Id, existing.Id);
                }
            });
            _status = "Workflow.Ready";
            return Task.FromResult(changed);
        });
    }

    /// <summary>Recognize a fully connected source/mix tree feeding the sole Gain → Output.
    /// No arbitrary graph is rebuilt; only its known final input cable is replaced.</summary>
    internal static string? BeatChannelRefusal(ProjectState state, Pattern pattern, GraphAttachment attachment,
        GraphDefinition graph, bool adding = true)
    {
        if (BeatSteps.PatternRefusal(state, pattern) is { } refusal) return refusal;
        if (state.Placements.Count(p => p.PatternId == pattern.Id) != 1) return "Beat.SharedChannels";
        int count = pattern.Parts.Length;
        if (adding && (count >= GraphDiagnostics.MaximumSources ||
                       graph.Nodes.Length + (count == 0 ? 1 : 2) > GraphDiagnostics.MaximumNodes))
            return "Beat.ChannelLimit";
        if (!graph.AdditionalData.IsEmpty || !attachment.AdditionalData.IsEmpty ||
            graph.Nodes.SelectMany(n => n.Ports).Any(p => p.Layout != GraphBuiltIns.Stereo) ||
            graph.Nodes.Any(n =>
                n.Type is not (GraphBuiltIns.Source or GraphBuiltIns.Mix or GraphBuiltIns.Gain
                    or GraphBuiltIns.Output)) ||
            graph.Nodes.Count(n => n.Type == GraphBuiltIns.Gain) != 1 ||
            graph.Nodes.Count(n => n.Type == GraphBuiltIns.Output) != 1 ||
            graph.Nodes.Count(n => n.Type == GraphBuiltIns.Source) != count ||
            graph.Nodes.Count(n => n.Type == GraphBuiltIns.Mix) != Math.Max(0, count - 1) ||
            graph.Nodes.Where(n => n.Type == GraphBuiltIns.Mix)
                .Any(n => n.Ports.Count(p => p.Direction == GraphPortDirection.Input) != 2) ||
            attachment.Sources.Length != count) return "Beat.GraphUnsupported";
        var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        var output = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Output);
        if (attachment.OutputNodeId != output.Id ||
            graph.Connections.Count(c => c.FromNodeId == gain.Id && c.ToNodeId == output.Id) != 1 ||
            graph.Nodes.Where(n => n.Type != GraphBuiltIns.Output).Any(n =>
                graph.Connections.Count(c => c.FromNodeId == n.Id) != 1))
            return "Beat.GraphUnsupported";
        var report = GraphDiagnostics.Inspect(state).Single(r => r.GraphId == graph.Id);
        if (count == 0)
        {
            if (graph.Nodes.Length != 2 || graph.Connections.Length != 1 ||
                report.Diagnostics.Any(d =>
                    d.Reason is not (GraphReasons.RequiredInput or GraphReasons.SourceCoverage)))
                return "Beat.GraphUnsupported";
        }
        else if (!report.IsEligibleForPreparation) return "Beat.GraphUnsupported";

        return null;
    }

    internal Task<bool> ResizeBeatAsync(BeatEditBasis basis, int bars) => Command("Workflow.EditFailed", () =>
    {
        var (pattern, _, _, _) = ValidateBeatBasis(basis);
        if (bars is < 1 or > BeatSteps.MaximumBars) throw new ArgumentOutOfRangeException(nameof(bars));
        long length = bars * BeatSteps.BarTicks;
        if (length == pattern.Length.Ticks) return Task.FromResult(false);
        if (pattern.Parts.Any(part => PianoNotes.Refusal(_document.Current.State, pattern, part) is not null))
            throw new InvalidOperationException("Beat.NotesUnsupported");
        var stepParts = pattern.Parts.Where(part =>
                BeatSteps.PartRefusal(_document.Current.State, part, pattern.Length.Ticks) is null)
            .Select(part => part.Id).ToHashSet();
        if (pattern.Parts.Any(part => part.Notes.Any(note => note.Position.Ticks >= length ||
                                                             !stepParts.Contains(part.Id) &&
                                                             note.Position.Ticks + note.Duration.Ticks > length)))
            throw new InvalidOperationException("Beat.ShortenOccupied");
        _playGeneration++;
        _coordinator?.Stop();
        return Task.FromResult(_document.Edit("Change Pattern length", edit =>
        {
            foreach (var part in pattern.Parts)
                if (stepParts.Contains(part.Id))
                    foreach (var note in part.Notes)
                        edit.UpdateNote(pattern.Id, part.Id, note.Id, note.Position,
                            BeatSteps.Duration(BeatSteps.Step(note)!.Value, length), note.Pitch, note.Intensity);
            edit.SetPatternLength(pattern.Id, new(length));
        }));
    });

    internal Task<bool> DeleteBeatAsync(BeatEditBasis basis) => Command("Workflow.EditFailed", () =>
    {
        var (pattern, _, _, _) = ValidateBeatBasis(basis, requireGrid: false);
        _playGeneration++;
        _coordinator?.Stop();
        return Task.FromResult(_document.Edit("Delete Pattern and occurrences", edit =>
        {
            foreach (var placement in _document.Current.State.Placements.Where(p => p.PatternId == pattern.Id))
                edit.DeletePlacement(placement.Id);
            edit.DeletePattern(pattern.Id);
        }));
    });

    internal Task<bool> DeleteBeatChannelAsync(BeatEditBasis basis, Id<MusicalPart> partId) => Command(
        "Workflow.EditFailed", () =>
        {
            var (pattern, _, attachment, graph) = ValidateBeatBasis(basis);
            if (BeatChannelRefusal(_document.Current.State, pattern, attachment, graph, adding: false) is { } refusal)
                throw new InvalidOperationException(refusal);
            var part = pattern.Parts.Single(p => p.Id == partId);
            var source = attachment.Sources.Single(binding => binding.PartId == partId);
            var cable = graph.Connections.Single(edge => edge.FromNodeId == source.NodeId);
            var destination = graph.Nodes.Single(node => node.Id == cable.ToNodeId);
            return Task.FromResult(_document.Edit("Delete WAV channel", edit =>
            {
                if (destination.Type == GraphBuiltIns.Mix)
                {
                    var sibling =
                        graph.Connections.Single(edge => edge.ToNodeId == destination.Id && edge.Id != cable.Id);
                    var onward = graph.Connections.Single(edge => edge.FromNodeId == destination.Id);
                    edit.DeleteGraphNode(graph.Id, destination.Id);
                    edit.ConnectGraphPorts(graph.Id, sibling.FromNodeId, sibling.FromPortId, onward.ToNodeId,
                        onward.ToPortId);
                }

                edit.DeleteGraphNode(graph.Id, source.NodeId);
                edit.DeletePart(pattern.Id, partId);
                edit.SetContextMix(attachment.ContextId, pattern.Parts.Length > 2);
                var state = _document.Current.State;
                if (!state.Patterns.SelectMany(p => p.Parts).Any(p => p.Id != partId && p.SoundId == part.SoundId) &&
                    !state.Groups.Any(group => group.SoundIds.Contains(part.SoundId))) edit.DeleteSound(part.SoundId);
            }));
        });

    internal Task<bool> AddBeatWavAsync(BeatEditBasis basis, string path, CancellationToken cancellation = default)
        => Command("Workflow.ImportFailed", async () =>
        {
            var (pattern, placement, attachment, graph) = ValidateBeatBasis(basis);
            if (BeatChannelRefusal(_document.Current.State, pattern, attachment, graph) is { } refusal)
                throw new InvalidOperationException(refusal);
            string name = Path.GetFileNameWithoutExtension(path);
            _status = "Workflow.Importing";
            Publish();
            var prepared = await Task.Run(async () =>
            {
                await using var file = File.OpenRead(path);
                var bytes = await WavDecoder.ReadBytesAsync(file, cancellation);
                return (Bytes: bytes, Root: SampleRootMetadata.Read(path, bytes));
            }, cancellation);
            ValidateBeatBasis(basis);
            using var request = ProjectMedia.BeginImport(_document, name, rootPitch: prepared.Root,
                releaseMilliseconds: 0m, ownedMediaDirectory: _mediaDirectory);
            // Durable acceptance already validates bytes, WAV integrity, cancellation and generation.
            using (var stream = new MemoryStream(prepared.Bytes, writable: false))
                await request.PrepareAsync(stream, cancellation);
            ValidateBeatBasis(basis);
            request.Accept(
                (edit, media) => { AttachBeatSound(edit, pattern, placement, attachment, graph, media.SoundId, name); },
                cancellation);
            _status = "Workflow.Ready";
            return true;
        });

    private static void AttachBeatSound(ProjectEdit edit, Pattern pattern, PatternPlacement placement,
        GraphAttachment attachment, GraphDefinition graph, Id<SoundDefinition> sound, string name)
    {
        var part = edit.AddPart(pattern.Id, name, sound);
        WireBeatPart(edit, pattern, placement, attachment, graph, part);
    }

    private static void WireBeatPart(ProjectEdit edit, Pattern pattern, PatternPlacement placement,
        GraphAttachment attachment, GraphDefinition graph, Id<MusicalPart> part)
    {
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
                mix.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id, gain.Id, input.Id);
            edit.SetContextMix(attachment.ContextId, true);
        }
    }
}
