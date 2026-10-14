// SPDX-License-Identifier: Apache-2.0

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using NAudio.Wave;
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

/// <summary>Worker-only codec adapter. No codec, file handle or NAudio type enters the audio engine.</summary>
internal static class AudioFileDecode
{
    internal static bool Supported(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".wav" or ".mp3" or ".flac";

    internal static (int Rate, int Channels, long Frames) Normalize(string source, string target,
        CancellationToken cancellation)
    {
        if (!Supported(source)) throw new NotSupportedException("AudioClip.UnsupportedFormat");
        using WaveStream reader = Path.GetExtension(source).Equals(".wav", StringComparison.OrdinalIgnoreCase)
            ? new WaveFileReader(source)
            : OperatingSystem.IsWindows()
                ? new MediaFoundationReader(source, new MediaFoundationReader.MediaFoundationReaderSettings
                    { RequestFloatOutput = true, SingleReaderObject = true })
                : throw new NotSupportedException("MP3/FLAC import requires Windows Media Foundation.");
        var provider = reader.ToSampleProvider();
        int rate = provider.WaveFormat.SampleRate, channels = provider.WaveFormat.Channels;
        if (rate is < 8000 or > 192000) throw new NotSupportedException("AudioClip.UnsupportedRate");
        // Preserve the first release's channel semantics explicitly: mono/stereo stay unchanged;
        // multichannel is rejected rather than silently dropping surround or inventing a matrix.
        if (channels is not (1 or 2)) throw new NotSupportedException("AudioClip.UnsupportedChannels");
        if (reader.Length > 0 && reader.Length / reader.WaveFormat.BlockAlign * channels * 4L >
            AudioMedia.MaximumDecodedBytes)
            throw new IOException("AudioClip.SizeLimit");
        using var output = new FileStream(target, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        output.Position = 44;
        var samples = new float[16384 * channels];
        long total = 0;
        int read;
        while ((read = provider.Read(samples, 0, samples.Length)) > 0)
        {
            cancellation.ThrowIfCancellationRequested();
            if (read % channels != 0 || (total + read) * 4 > AudioMedia.MaximumDecodedBytes)
                throw new IOException("Incomplete audio frame or decoded source budget exceeded.");
            foreach (float sample in samples.AsSpan(0, read))
                if (!float.IsFinite(sample))
                    throw new IOException("Audio contains nonfinite samples.");
            output.Write(MemoryMarshal.AsBytes(samples.AsSpan(0, read)));
            total += read;
        }

        cancellation.ThrowIfCancellationRequested();
        if (total == 0) throw new IOException("Audio source is empty.");
        Span<byte> header = stackalloc byte[44];
        header.Clear();
        "RIFF"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..], checked((int)(total * 4 + 36)));
        "WAVEfmt "u8.CopyTo(header[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(header[16..], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(header[20..], 3);
        BinaryPrimitives.WriteUInt16LittleEndian(header[22..], (ushort)channels);
        BinaryPrimitives.WriteInt32LittleEndian(header[24..], rate);
        BinaryPrimitives.WriteInt32LittleEndian(header[28..], rate * channels * 4);
        BinaryPrimitives.WriteUInt16LittleEndian(header[32..], (ushort)(channels * 4));
        BinaryPrimitives.WriteUInt16LittleEndian(header[34..], 32);
        "data"u8.CopyTo(header[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(header[40..], checked((int)(total * 4)));
        output.Position = 0;
        output.Write(header);
        output.Flush(true);
        return (rate, channels, total / channels);
    }
}
