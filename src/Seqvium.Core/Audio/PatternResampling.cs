// SPDX-License-Identifier: Apache-2.0
using System.Text;

namespace Seqvium.Core;

public sealed record PatternSample(byte[] Wave, long Frames, int SampleRate, int Channels);

/// <summary>Bounded Create Sample from one Pattern occurrence's local graph. No Mixer, Song or device capture.</summary>
public static class PatternResampling
{
    public const int SampleRate = 48000;

    // Capture on the document owner before yielding. The worker never traverses the live document.
    public static Task<PatternSample> RenderAsync(ProjectDocument document, Id<GraphAttachment> target,
        CancellationToken cancellation = default, Action<int>? progress = null)
    {
        document.CheckAvailable();
        cancellation.ThrowIfCancellationRequested();
        var frozen = ProjectDocument.Reopen(document.Current);
        frozen.MediaRoots.AddRange(document.MediaRoots);
        return RenderFrozenAsync(frozen, target, cancellation, progress);
    }

    private static async Task<PatternSample> RenderFrozenAsync(ProjectDocument frozen, Id<GraphAttachment> target,
        CancellationToken cancellation, Action<int>? progress)
    {
        try
        {
            return await Task.Run(() => Render(frozen, target, cancellation, progress), cancellation).ConfigureAwait(false);
        }
        finally { frozen.Close(); }
    }

    private static PatternSample Render(ProjectDocument frozen, Id<GraphAttachment> target,
        CancellationToken cancellation, Action<int>? progress)
    {
        const int packetFrames = 1024;
        var state = frozen.Current.State;
        var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == target)
            ?? throw new GraphPreparationException(GraphPreparationReasons.Target);
        var graph = state.Graphs.Single(g => g.Id == attachment.GraphId);
        var layout = graph.Nodes.FirstOrDefault(n => n.Id == attachment.OutputNodeId)?.Ports.FirstOrDefault()?.Layout;
        int channels = layout == GraphBuiltIns.Mono ? 1 : 2;
        using var plan = GraphPreparation.PrepareObject(frozen, target, SampleRate, channels, packetFrames, cancellation);
        long frames = plan.EndFrame - plan.StartFrame;
        long bytes = checked(frames * channels * sizeof(short));
        if (frames <= 0 || bytes > WavDecoder.MaximumSourceBytes - 44)
            throw new InvalidOperationException("Resample.TooLong");
        using var stream = new MemoryStream(checked((int)bytes + 44));
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("RIFF"u8); writer.Write((uint)(bytes + 36)); writer.Write("WAVEfmt "u8);
        writer.Write(16u); writer.Write((ushort)1); writer.Write((ushort)channels);
        writer.Write((uint)SampleRate); writer.Write((uint)(SampleRate * channels * 2));
        writer.Write((ushort)(channels * 2)); writer.Write((ushort)16); writer.Write("data"u8); writer.Write((uint)bytes);
        using var execution = plan.CreateExecution(smoothBoundaries: true);
        var packet = new float[packetFrames * channels];
        int reported = -1;
        while (execution.Position < plan.EndFrame)
        {
            cancellation.ThrowIfCancellationRequested();
            int count = (int)Math.Min(packetFrames, plan.EndFrame - execution.Position);
            execution.Process(packet.AsSpan(0, count * channels));
            for (int i = 0; i < count * channels; i++)
            {
                float value = packet[i];
                // Managed sampler WAV is bounded to unity. Never silently clip, normalize or change the object.
                if (!float.IsFinite(value) || value is < -1 or > 1)
                    throw new InvalidOperationException("Resample.Overload");
                writer.Write((short)Math.Clamp((int)Math.Round(value * 32768), short.MinValue, short.MaxValue));
            }
            int percent = (int)((execution.Position - plan.StartFrame) * 100 / frames);
            if (percent != reported) { reported = percent; progress?.Invoke(percent); }
        }
        cancellation.ThrowIfCancellationRequested();
        return new(stream.ToArray(), frames, SampleRate, channels);
    }
}
