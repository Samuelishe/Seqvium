// SPDX-License-Identifier: Apache-2.0
using System.Text;

namespace Seqvium.Core;

public sealed record WaveExportResult(long Frames, long ClippedSamples);

/// <summary>One frozen occurrence, including its graph, as 48 kHz stereo PCM16 WAV. No transport/device access.</summary>
public static class PatternWaveExport
{
    public static async Task<WaveExportResult> ExportAsync(ProjectDocument document, Id<GraphAttachment> target,
        string path, CancellationToken cancellation = default)
        => await ExportCoreAsync(document, target, path, cancellation).ConfigureAwait(false);

    public static async Task<WaveExportResult> ExportArrangementAsync(ProjectDocument document,
        string path, CancellationToken cancellation = default)
        => await ExportCoreAsync(document, null, path, cancellation).ConfigureAwait(false);

    private static async Task<WaveExportResult> ExportCoreAsync(ProjectDocument document, Id<GraphAttachment>? target,
        string path, CancellationToken cancellation)
    {
        document.CheckAvailable();
        string destination = Path.GetFullPath(path);
        if (!Path.GetExtension(destination).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            throw new IOException("Export requires a .wav destination.");
        foreach (string root in document.MediaRoots)
            if (destination.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                throw new IOException("Export cannot overwrite project media.");
        var frozen = ProjectDocument.Reopen(document.Current);
        frozen.MediaRoots.AddRange(document.MediaRoots);
        try { return await Task.Run(() => Render(frozen, target, destination, cancellation), cancellation).ConfigureAwait(false); }
        finally { frozen.Close(); }
    }

    private static WaveExportResult Render(ProjectDocument frozen, Id<GraphAttachment>? target, string destination,
        CancellationToken cancellation)
    {
        const int framesPerPacket = 1024;
        using var plan = target is { } attachment
            ? GraphPreparation.Prepare(frozen, attachment, 48000, 2, framesPerPacket, cancellationToken: cancellation)
            : ArrangementPreparation.Prepare(frozen, 48000, 2, framesPerPacket, cancellation);
        using var execution = plan.CreateExecution();
        long frames = plan.EndFrame - plan.StartFrame;
        long dataBytes = checked(frames * 4);
        if (dataBytes > uint.MaxValue - 36) throw new NotSupportedException("WAV exceeds the RIFF size limit.");
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".pending";
        bool ownsTemporary = false;
        long clipped = 0;
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                ownsTemporary = true;
                using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
                writer.Write("RIFF"u8); writer.Write((uint)(dataBytes + 36)); writer.Write("WAVEfmt "u8);
                writer.Write(16u); writer.Write((ushort)1); writer.Write((ushort)2); writer.Write(48000u);
                writer.Write(192000u); writer.Write((ushort)4); writer.Write((ushort)16);
                writer.Write("data"u8); writer.Write((uint)dataBytes);
                var packet = new float[framesPerPacket * 2];
                var encoded = new byte[framesPerPacket * 4];
                while (execution.Position < plan.EndFrame)
                {
                    cancellation.ThrowIfCancellationRequested();
                    int count = (int)Math.Min(framesPerPacket, plan.EndFrame - execution.Position);
                    execution.Process(packet.AsSpan(0, count * 2));
                    for (int index = 0; index < count * 2; index++)
                    {
                        float value = packet[index];
                        if (value is < -1 or > 1) clipped++;
                        short sample = (short)Math.Clamp((int)Math.Round(Math.Clamp(value, -1, 1) * 32768), short.MinValue, short.MaxValue);
                        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(encoded.AsSpan(index * 2, 2), sample);
                    }
                    stream.Write(encoded.AsSpan(0, count * 4));
                }
                stream.Flush(flushToDisk: true);
            }
            cancellation.ThrowIfCancellationRequested();
            File.Move(temporary, destination, overwrite: true); ownsTemporary = false;
            return new(frames, clipped);
        }
        finally { if (ownsTemporary) File.Delete(temporary); }
    }
}
