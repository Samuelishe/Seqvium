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

public sealed record AudioContribution(Id<PatternPlacement> PlacementId, Id<MusicalPart> PartId,
    Id<GraphSourceBinding> BindingId, Id<GraphNode> NodeId);

internal enum GraphOperationKind { Source, Gain, Mix }
internal sealed record GraphOperation(Id<GraphNode> NodeId, GraphOperationKind Kind, int Buffer, int[] Inputs, int Coefficient);

/// <summary>Derived tables only. Arrays are private to preparation/execution and never canonical state.</summary>
internal sealed class PreparedGraph
{
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
    internal int SourceBuffer(Id<MusicalPart> part) => NodeBuffers[Contributions.Single(item => item.PartId == part).NodeId];
}

public static class GraphPreparation
{
    public const int MaximumScratchBytes = 16 * 1024 * 1024;
    public const long MaximumPacketWork = 64 * 1024 * 1024;

    /// <summary>Prepares the current selected canonical attachment. Refusal never edits intent or substitutes last-valid music.
    /// Start is the placement's absolute position; repeats remain bounded sampler repeats, not Arrangement execution.</summary>
    public static PreparedSampler Prepare(ProjectDocument document, Id<GraphAttachment> attachmentId,
        int sampleRate, int channels, int maximumPacketFrames, int voiceCapacity = 8, int repeats = 1,
        MusicalPosition? stop = null, CancellationToken cancellationToken = default)
    {
        document.CheckAvailable();
        cancellationToken.ThrowIfCancellationRequested();
        var state = document.Current.State;
        var attachment = state.GraphAttachments.SingleOrDefault(item => item.Id == attachmentId)
            ?? throw new GraphPreparationException(GraphPreparationReasons.Target);
        var report = GraphDiagnostics.Inspect(state).Single(item => item.GraphId == attachment.GraphId);
        if (!report.IsEligibleForPreparation) throw new GraphPreparationException(report.Diagnostics[0].Reason);
        if (sampleRate is not (44100 or 48000) || channels is not (1 or 2) || maximumPacketFrames is < 1 or > 65536)
            throw new GraphPreparationException(GraphPreparationReasons.Format);
        if (voiceCapacity is < 1 or > 8 || repeats is < 1 or > 1024)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        var graph = state.Graphs.Single(item => item.Id == attachment.GraphId);
        if (graph.Nodes.SelectMany(item => item.Ports).Any(port => port.Layout !=
                (channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo)))
            throw new GraphPreparationException(GraphPreparationReasons.Format);
        var placement = state.Placements.Single(item => item.ItemContextId == attachment.ContextId);
        var program = Compile(graph, attachment, placement.Id, channels, maximumPacketFrames, voiceCapacity);
        PreparedSampler? plan = null;
        try
        {
            plan = SamplerPreparation.PreparePatternCore(document, placement.PatternId, sampleRate, channels,
                voiceCapacity, placement.Position, repeats, stop, program, cancellationToken);
            Preflight(plan, maximumPacketFrames);
            cancellationToken.ThrowIfCancellationRequested();
            return plan;
        }
        catch (Exception error)
        {
            plan?.Dispose();
            if (error is OperationCanceledException or GraphPreparationException) throw;
            throw new GraphPreparationException(error is IOException or UnauthorizedAccessException
                ? GraphPreparationReasons.Media : error is OverflowException or NotSupportedException
                    ? GraphPreparationReasons.Budget : GraphPreparationReasons.Source, error);
        }
    }

    private static PreparedGraph Compile(GraphDefinition graph, GraphAttachment attachment,
        Id<PatternPlacement> placement, int channels, int frames, int voices)
    {
        var remaining = graph.Nodes.ToDictionary(item => item.Id);
        var buffers = new Dictionary<Id<GraphNode>, int>();
        var operations = new List<GraphOperation>();
        var coefficients = new List<float>();
        int count = 0;
        while (remaining.Count > 0)
        {
            var node = remaining.Values.OrderBy(item => item.Id.Value).FirstOrDefault(item =>
                graph.Connections.Where(edge => edge.ToNodeId == item.Id).All(edge => buffers.ContainsKey(edge.FromNodeId)))
                ?? throw new GraphPreparationException(GraphReasons.Cycle);
            var inputs = node.Ports.Where(port => port.Direction == GraphPortDirection.Input)
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
            graph.Nodes.Length * 256 + graph.Connections.Length * 16 + voices * 128);
        if (scratch > MaximumScratchBytes)
            throw new GraphPreparationException(GraphPreparationReasons.Budget);
        return new()
        {
            AttachmentId = attachment.Id,
            PlacementId = placement, Contributions = [.. attachment.Sources.OrderBy(item => item.PartId.Value)
                .Select(item => new AudioContribution(item.PlacementId, item.PartId, item.Id, item.NodeId))],
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
        long work = checked((long)frames * plan.Channels * plan.VoiceCapacity * 16 +
            signalWork + (long)maximumWindow * plan.VoiceCapacity);
        if (work > MaximumPacketWork) throw new GraphPreparationException(GraphPreparationReasons.Budget);
        plan.MaximumPacketEvents = maximumWindow;
        plan.PacketWork = work;
    }
}
