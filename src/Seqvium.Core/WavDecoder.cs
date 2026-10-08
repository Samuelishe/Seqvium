// SPDX-License-Identifier: Apache-2.0
using System.Buffers.Binary;

namespace Seqvium.Core;

public sealed class WavFormatException(string message) : IOException(message);

/// <summary>Disposable decoded cache. Leases keep immutable PCM alive independently of this owner.</summary>
public sealed class DecodedPcm : IDisposable
{
    private float[]? _samples;
    public int SampleRate { get; }
    public int Channels { get; }
    public int Frames { get; }
    internal DecodedPcm(int rate, int channels, float[] samples)
    {
        SampleRate = rate; Channels = channels; Frames = samples.Length / channels; _samples = samples;
    }
    public float Sample(int frame, int channel)
    {
        if (frame < 0 || frame >= Frames || channel < 0 || channel >= Channels) throw new ArgumentOutOfRangeException(nameof(frame));
        return Samples[frame * Channels + channel];
    }
    private float[] Samples => _samples ?? throw new ObjectDisposedException(nameof(DecodedPcm));
    internal PcmLease Lease() => new(SampleRate, Channels, Samples);
    public void Dispose() => _samples = null;
}

internal sealed class PcmLease(int rate, int channels, float[] samples) : IDisposable
{
    private float[]? _samples = samples;
    internal int Rate { get; } = rate;
    internal int Channels { get; } = channels;
    internal int Frames => Samples.Length / Channels;
    internal int Bytes => checked(Samples.Length * sizeof(float));
    internal float[] Samples => _samples ?? throw new ObjectDisposedException(nameof(PcmLease));
    internal PcmLease Lease() => new(Rate, Channels, Samples);
    public void Dispose() => _samples = null;
}

/// <summary>Bounded little-endian RIFF/WAVE decoder; never call from a realtime callback.</summary>
public static class WavDecoder
{
    public const int MaximumSourceBytes = 16 * 1024 * 1024;

    public static async Task<byte[]> ReadBytesAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await source.ReadAsync(chunk.AsMemory(0,
            (int)Math.Min(chunk.Length, MaximumSourceBytes + 1L - buffer.Length)), cancellationToken).ConfigureAwait(false)) > 0)
        {
            buffer.Write(chunk, 0, count);
            if (buffer.Length > MaximumSourceBytes) throw new WavFormatException("WAV exceeds the 16 MiB source limit.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        return buffer.ToArray();
    }

    public static DecodedPcm Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 12 or > MaximumSourceBytes || !bytes[..4].SequenceEqual("RIFF"u8) ||
            !bytes.Slice(8, 4).SequenceEqual("WAVE"u8) || U32(bytes, 4) != bytes.Length - 8)
            throw new WavFormatException("Invalid or unsupported RIFF/WAVE length/header.");
        int formatOffset = -1, formatLength = 0, dataOffset = -1, dataLength = 0;
        for (int offset = 12; offset < bytes.Length;)
        {
            if (bytes.Length - offset < 8) throw new WavFormatException("Truncated chunk header.");
            long length = U32(bytes, offset + 4);
            long end = offset + 8L + length + (length & 1);
            if (end > bytes.Length) throw new WavFormatException("Truncated chunk or padding.");
            var id = bytes.Slice(offset, 4);
            if (id.SequenceEqual("fmt "u8))
            {
                if (formatOffset >= 0 || dataOffset >= 0) throw new WavFormatException("Duplicate or misplaced format chunk.");
                formatOffset = offset + 8; formatLength = (int)length;
            }
            else if (id.SequenceEqual("data"u8))
            {
                if (dataOffset >= 0 || formatOffset < 0) throw new WavFormatException("Duplicate data or missing prior format.");
                dataOffset = offset + 8; dataLength = (int)length;
            }
            offset = (int)end;
        }
        if (formatOffset < 0 || formatLength < 16 || dataOffset < 0)
            throw new WavFormatException("Required fmt/data chunks are missing or invalid.");
        var format = bytes.Slice(formatOffset, formatLength);
        int tag = U16(format, 0), channels = U16(format, 2), bits = U16(format, 14);
        uint rate = U32(format, 4);
        if (formatLength != 16)
        {
            if (formatLength < 18 || U16(format, 16) != formatLength - 18)
                throw new WavFormatException("Invalid format extension length.");
        }
        if (tag == 0xfffe)
        {
            if (formatLength != 40 || U16(format, 16) != 22 || U16(format, 18) != bits)
                throw new WavFormatException("Unsupported extensible valid-bit/container configuration.");
            uint mask = U32(format, 20);
            if (!(mask == 0 || channels == 1 && mask == 4 || channels == 2 && mask == 3))
                throw new WavFormatException("Unsupported speaker ordering.");
            var subformat = new Guid(format.Slice(24, 16));
            tag = subformat == new Guid("00000001-0000-0010-8000-00aa00389b71") ? 1 :
                subformat == new Guid("00000003-0000-0010-8000-00aa00389b71") ? 3 : 0;
        }
        else if (formatLength > 18) throw new WavFormatException("Unsupported format extension.");
        if (channels is not (1 or 2) || rate is not (44100 or 48000) ||
            !(tag == 1 && bits == 16 || tag == 3 && bits == 32))
            throw new WavFormatException("Supported WAV: PCM16/float32, mono/stereo, 44100/48000 Hz.");
        int alignment = channels * (bits / 8);
        if (U16(format, 12) != alignment || U32(format, 8) != rate * alignment || dataLength % alignment != 0 || dataLength == 0)
            throw new WavFormatException("Invalid rate, frame alignment or empty data.");
        var data = bytes.Slice(dataOffset, dataLength);
        var samples = new float[dataLength / (bits / 8)]; // At most 32 MiB, bounded by actual validated source bytes.
        for (int i = 0; i < samples.Length; i++)
        {
            float value = tag == 1 ? BinaryPrimitives.ReadInt16LittleEndian(data.Slice(i * 2, 2)) / 32768f :
                BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.Slice(i * 4, 4)));
            if (!float.IsFinite(value)) throw new WavFormatException("Nonfinite float PCM is unsupported.");
            samples[i] = Math.Clamp(value, -1f, 1f);
        }
        return new((int)rate, channels, samples);
    }

    private static ushort U16(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(offset, 2));
    private static uint U32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, 4));
}
