// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

internal sealed record GraphGainParameters(float[] Coefficients, PreparedMixer? Mixer);

internal sealed record GraphGainUpdate(long Execution, long Authority, Guid Revision, GraphGainParameters Parameters);

internal sealed record GraphGainPreview(long Execution, long Authority, long Stop, GraphGainParameters Parameters);

public sealed partial class RealtimeSampler
{
    private GraphGainUpdate? _gainUpdate;
    private GraphGainPreview? _gainPreview;

    internal void ClearGraphGainPreview() => Volatile.Write(ref _gainPreview, null);

    internal bool PreviewGraphGain(ProjectSnapshot snapshot, long execution, Id<GraphNode> node, decimal amplitude)
    {
        var active = Volatile.Read(ref _active);
        if (_disposed || IsTerminated || HasPending || active?.Authority != execution ||
            active.Plan.Graph is not { } graph || !graph.Operations.Any(o =>
                o.Kind == GraphOperationKind.Gain && o.NodeId == node)) return false;
        var parameters = GainParameters(active, snapshot.State, node, amplitude);
        active.MixerBudget = parameters.Mixer;
        Volatile.Write(ref _gainPreview, new(execution, Volatile.Read(ref _authority),
            Volatile.Read(ref _stop), parameters));
        return true;
    }

    internal bool PublishGainUpdate(ProjectSnapshot snapshot, long execution)
    {
        var active = Volatile.Read(ref _active);
        if (_disposed || IsTerminated || HasPending || active?.Authority != execution || active.Plan.Graph is null)
            return false;
        var parameters = GainParameters(active, snapshot.State);
        active.MixerBudget = parameters.Mixer;
        long authority = InvalidatePreparation();
        Volatile.Write(ref _gainUpdate, new(execution, authority, snapshot.Revision, parameters));
        return true;
    }

    // Control owner only. Reuse the prepared schedule, PCM and topology; build parameter tables only.
    private GraphGainParameters GainParameters(State active, ProjectState state,
        Id<GraphNode>? previewNode = null, decimal amplitude = 0)
    {
        var graph = active.Plan.Graph!;
        var nodes = state.Graphs.SelectMany(g => g.Nodes).ToDictionary(n => n.Id);
        var coefficients = new float[graph.Coefficients.Length];
        foreach (var op in graph.Operations)
            if (op.Kind == GraphOperationKind.Gain)
                coefficients[op.Coefficient] = op.NodeId == previewNode
                    ? (float)amplitude
                    : (float)nodes[op.NodeId].Parameters[GraphBuiltIns.GainAmplitude].GetDecimal();
        var projection = new PreparedGraph
        {
            AttachmentId = graph.AttachmentId, PlacementId = graph.PlacementId,
            Contributions = graph.Contributions, NodeBuffers = graph.NodeBuffers, Operations = graph.Operations,
            Coefficients = coefficients, OutputBuffer = graph.OutputBuffer, BufferCount = graph.BufferCount,
            MaximumPacketFrames = graph.MaximumPacketFrames, ScratchBytes = graph.ScratchBytes
        };
        var mixer = MixerPreparation.Compile(state, projection, SampleRate);
        if (mixer is not null) mixer = MixerPreparation.ReserveContinuation(mixer, active.MixerBudget);
        return new(coefficients, mixer);
    }
}
