// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public static class GraphPreparationReasons
{
    public const string Target = "graph.prepare-target";
    public const string Media = "graph.prepare-media";
    public const string Format = "graph.prepare-format";
    public const string Budget = "graph.prepare-budget";
    public const string Source = "graph.prepare-source";
}

public sealed class GraphPreparationException(string reason, Exception? inner = null)
    : Exception(reason, inner)
{
    public string Reason { get; } = reason;
}

public sealed record AudioContribution(
    Id<PatternPlacement> PlacementId,
    Id<MusicalPart> PartId,
    Id<GraphSourceBinding> BindingId,
    Id<GraphNode> NodeId,
    Id<SoundDefinition>? AudioSoundId = null);

internal enum GraphOperationKind
{
    Source,
    Gain,
    Mix
}

internal sealed record GraphOperation(
    Id<GraphNode> NodeId,
    GraphOperationKind Kind,
    int Buffer,
    int[] Inputs,
    int Coefficient);

/// <summary>Derived tables only. Arrays are private to preparation/execution and never canonical state.</summary>
internal sealed class PreparedGraph
{
    internal PreparedMixer? Mixer { get; set; }
    internal required Id<GraphAttachment> AttachmentId { get; init; }
    internal required Id<PatternPlacement> PlacementId { get; init; }
    internal required ImmutableArray<AudioContribution> Contributions { get; init; }
    internal required Dictionary<Id<GraphNode>, int> NodeBuffers { get; init; }
    internal required GraphOperation[] Operations { get; init; }
    internal required float[] Coefficients { get; init; }
    internal required int OutputBuffer { get; init; }
    internal required int BufferCount { get; init; }
    internal required int MaximumPacketFrames { get; init; }
    internal required int ScratchBytes { get; init; }

    internal int SourceBuffer(Id<MusicalPart> part) =>
        NodeBuffers[Contributions.Single(item => item.PartId == part).NodeId];
}

public static class GraphPreparation
{
    public const int MaximumVoices = 32;
    public const int MaximumScratchBytes = 16 * 1024 * 1024;
    public const long MaximumPacketWork = 64 * 1024 * 1024;

    internal static bool IsSilentChannelGraph(ProjectState state, GraphAttachment attachment)
    {
        var placement = state.Placements.Single(p => p.ItemContextId == attachment.ContextId);
        var pattern = state.Patterns.Single(p => p.Id == placement.PatternId);
        var graph = state.Graphs.Single(g => g.Id == attachment.GraphId);
        if (!pattern.Parts.IsEmpty || graph.Nodes.Length != 2 ||
            graph.Nodes.Count(n => n.Type == GraphBuiltIns.Gain) != 1 ||
            graph.Nodes.Count(n => n.Type == GraphBuiltIns.Output) != 1) return false;
        var gain = graph.Nodes.SingleOrDefault(n => n.Type == GraphBuiltIns.Gain);
        var output = graph.Nodes.SingleOrDefault(n => n.Type == GraphBuiltIns.Output);
        return pattern.Parts.IsEmpty && attachment.Sources.IsEmpty && graph.Nodes.Length == 2 &&
               gain is not null && output is not null && attachment.OutputNodeId == output.Id &&
               graph.Connections is [var cable] && cable.FromNodeId == gain.Id && cable.ToNodeId == output.Id &&
               GraphDiagnostics.Inspect(state).Single(r => r.GraphId == graph.Id).Diagnostics.All(d =>
                   d.Reason is GraphReasons.RequiredInput or GraphReasons.SourceCoverage);
    }

    /// <summary>Prepares the current selected canonical attachment. Refusal never edits intent or substitutes last-valid music.
    /// Start is the placement's absolute position; repeats remain bounded sampler repeats, not Arrangement execution.</summary>
    public static PreparedSampler Prepare(ProjectDocument document, Id<GraphAttachment> attachmentId,
        int sampleRate, int channels, int maximumPacketFrames, int voiceCapacity = 8, int repeats = 1,
        MusicalPosition? stop = null, CancellationToken cancellationToken = default)
        => PrepareCore(document, attachmentId, sampleRate, channels, maximumPacketFrames, voiceCapacity,
            repeats, stop, false, cancellationToken);

    /// <summary>One item-local Pattern occurrence at source time zero, before all Mixer routing/processing.
    /// Required unknown local dependencies still block preparation. No transport or output-device state is used.</summary>
    public static PreparedSampler PrepareObject(ProjectDocument document, Id<GraphAttachment> attachmentId,
        int sampleRate, int channels, int maximumPacketFrames, CancellationToken cancellationToken = default)
        => PrepareCore(document, attachmentId, sampleRate, channels, maximumPacketFrames, MaximumVoices,
            1, null, true, cancellationToken);

    private static PreparedSampler PrepareCore(ProjectDocument document, Id<GraphAttachment> attachmentId,
        int sampleRate, int channels, int maximumPacketFrames, int voiceCapacity, int repeats,
        MusicalPosition? stop, bool objectRender, CancellationToken cancellationToken)
    {
        document.CheckAvailable();
        cancellationToken.ThrowIfCancellationRequested();
        var state = document.Current.State;
        var attachment = state.GraphAttachments.SingleOrDefault(item => item.Id == attachmentId)
                         ?? throw new GraphPreparationException(GraphPreparationReasons.Target);
        var report = GraphDiagnostics.Inspect(state).Single(item => item.GraphId == attachment.GraphId);
        bool silent = IsSilentChannelGraph(state, attachment);
        if (!report.IsEligibleForPreparation && !silent)
            throw new GraphPreparationException(report.Diagnostics[0].Reason);
        if (sampleRate is not (44100 or 48000) || channels is not (1 or 2) || maximumPacketFrames is < 1 or > 65536)
            throw new GraphPreparationException(GraphPreparationReasons.Format);
        if (voiceCapacity is < 1 or > MaximumVoices || repeats is < 1 or > 1024)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        var graph = state.Graphs.Single(item => item.Id == attachment.GraphId);
        if (graph.Nodes.SelectMany(item => item.Ports).Any(port => port.Layout !=
                                                                   (channels == 1
                                                                       ? GraphBuiltIns.Mono
                                                                       : GraphBuiltIns.Stereo)))
            throw new GraphPreparationException(GraphPreparationReasons.Format);
        var placement = state.Placements.Single(item => item.ItemContextId == attachment.ContextId);
        var program = Compile(graph, attachment, placement.Id, channels, maximumPacketFrames, voiceCapacity, silent);
        if (!objectRender) program.Mixer = MixerPreparation.Compile(state, program, sampleRate);
        PreparedSampler? plan = null;
        try
        {
            plan = SamplerPreparation.PreparePatternCore(document, placement.PatternId, sampleRate, channels,
                voiceCapacity, objectRender ? new(0) : placement.Position, repeats, stop, program, cancellationToken,
                objectRender);
            Preflight(plan, maximumPacketFrames);
            cancellationToken.ThrowIfCancellationRequested();
            return plan;
        }
        catch (Exception error)
        {
            plan?.Dispose();
            if (error is OperationCanceledException or GraphPreparationException) throw;
            throw new GraphPreparationException(error is IOException or UnauthorizedAccessException
                ? GraphPreparationReasons.Media
                : error is OverflowException or NotSupportedException
                    ? GraphPreparationReasons.Budget
                    : GraphPreparationReasons.Source, error);
        }
    }

    private static PreparedGraph Compile(GraphDefinition graph, GraphAttachment attachment,
        Id<PatternPlacement> placement, int channels, int frames, int voices, bool silent)
    {
        var remaining = graph.Nodes.ToDictionary(item => item.Id);
        var buffers = new Dictionary<Id<GraphNode>, int>();
        var operations = new List<GraphOperation>();
        var coefficients = new List<float>();
        int count = 0;
        while (remaining.Count > 0)
        {
            var node = remaining.Values.OrderBy(item => item.Id.Value).FirstOrDefault(item =>
                           graph.Connections.Where(edge => edge.ToNodeId == item.Id)
                               .All(edge => buffers.ContainsKey(edge.FromNodeId)))
                       ?? throw new GraphPreparationException(GraphReasons.Cycle);
            var inputs = node.Ports.Where(port => port.Direction == GraphPortDirection.Input &&
                                                  (!silent || node.Type != GraphBuiltIns.Gain))
                .OrderBy(port => port.Id.Value).Select(port =>
                    buffers[graph.Connections.Single(edge => edge.ToPortId == port.Id).FromNodeId]).ToArray();
            if (node.Type == GraphBuiltIns.Output) buffers.Add(node.Id, inputs[0]);
            else
            {
                int buffer = count++;
                buffers.Add(node.Id, buffer);
                int coefficient = -1;
                var kind = node.Type == GraphBuiltIns.Source ? GraphOperationKind.Source :
                    node.Type == GraphBuiltIns.Gain ? GraphOperationKind.Gain : GraphOperationKind.Mix;
                if (kind == GraphOperationKind.Gain)
                {
                    coefficient = coefficients.Count;
                    coefficients.Add((float)node.Parameters[GraphBuiltIns.GainAmplitude].GetDecimal());
                }

                operations.Add(new(node.Id, kind, buffer, inputs, coefficient));
            }

            remaining.Remove(node.Id);
        }

        // Includes buffer payload and conservative voice/operation/index storage, not only float arrays.
        int scratch = checked(count * checked(frames * channels * sizeof(float)) +
                              graph.Nodes.Length * 256 + graph.Connections.Length * 16 + voices * 256);
        if (scratch > MaximumScratchBytes)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        return new()
        {
            AttachmentId = attachment.Id,
            PlacementId = placement, Contributions =
            [
                .. attachment.Sources.OrderBy(item => item.PartId.Value)
                    .Select(item => new AudioContribution(item.PlacementId, item.PartId, item.Id, item.NodeId))
            ],
            NodeBuffers = buffers, Operations = [.. operations], Coefficients = [.. coefficients],
            OutputBuffer = buffers[attachment.OutputNodeId!.Value], BufferCount = count,
            MaximumPacketFrames = frames, ScratchBytes = scratch
        };
    }

    internal static void Preflight(PreparedSampler plan, int frames)
    {
        int first = 0, density = 0, maximumWindow = 0;
        long previous = -1;
        for (int index = 0; index < plan.Events.Length; index++)
        {
            var item = plan.Events[index];
            density = item.Frame == previous ? density + 1 : 1;
            previous = item.Frame;
            if (density > 64) throw new GraphPreparationException(GraphPreparationReasons.Budget);
            while (plan.Events[first].Frame <= item.Frame - frames) first++;
            maximumWindow = Math.Max(maximumWindow, index - first + 1);
        }

        // Conservative sample/event work units, not CPU instructions or a device deadline guarantee:
        // buffer clear plus read/compute/write per input; voice interpolation/envelope; bounded slot scans per event.
        long signalWork = plan.Graph?.Operations.Sum(operation => (long)frames * plan.Channels *
                                                                  (1 + 4 * Math.Max(1, operation.Inputs.Length))) ?? 0;
        if (plan.Loop is { } clock)
        {
            long shortestCycle = Math.Max(1, clock.Frame(clock.StartTicks + clock.LengthTicks) - plan.StartFrame - 1);
            maximumWindow = (int)Math.Min((long)frames * 64,
                Math.Max(maximumWindow, (frames / shortestCycle + 2) * plan.Events.Length));
        }

        long voiceWork = plan.Notes.Any(n => n.Synth is not null) ? 128 : 16;
        signalWork += (long)frames * plan.Automation.Length * 128;
        long work = checked((long)frames * plan.Channels * plan.VoiceCapacity * voiceWork +
                            signalWork + (long)maximumWindow * plan.VoiceCapacity);
        if (plan.IsArrangement)
            work = checked(work + (long)plan.Notes.Length * 4 * (frames / Math.Max(1, plan.SampleRate / 100) + 2));
        if (work > MaximumPacketWork) throw new GraphPreparationException(GraphPreparationReasons.Budget);
        if (plan.Graph?.Mixer is { } mixer)
            work = checked(work + (long)frames * (mixer.Strips.Length * 256 +
                                                  mixer.Strips.Count(s => s.Reverb.Admitted) * 512 +
                                                  mixer.Taps.Length * 8));
        if (work > MaximumPacketWork || plan.ScratchBytes > MaximumScratchBytes)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        plan.MaximumPacketEvents = maximumWindow;
        plan.PacketWork = work;
    }
}