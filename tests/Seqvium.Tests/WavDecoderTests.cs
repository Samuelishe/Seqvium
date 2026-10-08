// SPDX-License-Identifier: Apache-2.0
using System.Buffers.Binary;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class WavDecoderTests
{
    [Theory]
    [InlineData(44100, 1, false, false)]
    [InlineData(48000, 1, false, true)]
    [InlineData(44100, 2, false, true)]
    [InlineData(48000, 2, false, false)]
    [InlineData(44100, 1, true, false)]
    [InlineData(48000, 1, true, true)]
    [InlineData(44100, 2, true, true)]
    [InlineData(48000, 2, true, false)]
    public void SupportedFormatsPreserveRateFramesAndChannelOrder(int rate, int channels, bool floating, bool extensible)
    {
        float[] samples = [-1f, 0.25f, 0.5f, -0.75f];
        using var pcm = WavDecoder.Decode(WavFixtures.Create(samples, rate, channels, floating, extensible, oddChunk: true));
        Assert.Equal(rate, pcm.SampleRate); Assert.Equal(channels, pcm.Channels); Assert.Equal(4 / channels, pcm.Frames);
        for (int i = 0; i < samples.Length; i++) Assert.Equal(samples[i], pcm.Sample(i / channels, i % channels));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WaveFormatExWithZeroExtensionAndUnknownPaddedChunksAreAccepted(bool floating)
    {
        using var pcm = WavDecoder.Decode(WavFixtures.Create([0.5f], floating: floating, extended: true, oddChunk: true));
        Assert.Equal(0.5f, pcm.Sample(0, 0));
    }

    [Fact]
    public void EveryTruncatedPrefixIsRejectedAndForgedLengthsNeverAllocateDeclaredSizes()
    {
        byte[] valid = WavFixtures.Create([0.1f, -0.2f], extensible: true, oddChunk: true);
        for (int count = 0; count < valid.Length; count++)
            Assert.Throws<WavFormatException>(() => WavDecoder.Decode(valid.AsSpan(0, count)));
        byte[] forged = WavFixtures.Create([0.5f]);
        BinaryPrimitives.WriteUInt32LittleEndian(forged.AsSpan(16, 4), uint.MaxValue);
        Assert.Throws<WavFormatException>(() => WavDecoder.Decode(forged));
    }

    [Theory]
    [InlineData("riff")]
    [InlineData("wave")]
    [InlineData("rate")]
    [InlineData("byte-rate")]
    [InlineData("align")]
    [InlineData("channels")]
    [InlineData("bits")]
    [InlineData("codec")]
    [InlineData("extension-length")]
    [InlineData("valid-bits")]
    [InlineData("mask")]
    [InlineData("subformat")]
    [InlineData("empty")]
    [InlineData("duplicate")]
    [InlineData("missing-data")]
    [InlineData("unaligned")]
    [InlineData("partial-chunk")]
    [InlineData("missing-padding")]
    public void MalformedOrUnsupportedRepresentationsAreExplicitlyRejected(string failure)
    {
        byte[] bytes = WavFixtures.Create([0.5f, 0.25f], floating: false, extensible: true);
        switch (failure)
        {
            case "riff": bytes[0] = 0; break;
            case "wave": bytes[8] = 0; break;
            case "rate": BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24), 96000); break;
            case "byte-rate": bytes[28] ^= 1; break;
            case "align": bytes[32] = 7; break;
            case "channels": bytes[22] = 3; break;
            case "bits": bytes[34] = 24; break;
            case "codec": bytes[20] = 7; bytes[21] = 0; break;
            case "extension-length": bytes[36] = 21; break;
            case "valid-bits": bytes[38] = 12; break;
            case "mask": bytes[40] = 1; break;
            case "subformat": bytes[44] = 7; break;
            case "empty": bytes = bytes[..68]; BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(64), 0); break;
            case "duplicate": bytes = [.. bytes, .. bytes.AsSpan(60, 12).ToArray()]; break;
            case "missing-data": bytes[60] = (byte)'X'; break;
            case "unaligned": BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(64), 3); break;
            case "partial-chunk": bytes = [.. bytes, 1, 2, 3]; break;
            case "missing-padding": bytes = [.. bytes, .. "JUNK"u8.ToArray(), 1, 0, 0, 0, 7]; break;
        }
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), bytes.Length - 8);
        Assert.Throws<WavFormatException>(() => WavDecoder.Decode(bytes));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonfiniteFloatIsRejected(float value) =>
        Assert.Throws<WavFormatException>(() => WavDecoder.Decode(WavFixtures.Create([value])));

    [Fact]
    public void FiniteFloatOverloadSaturatesAndDisposalReleasesCacheOwner()
    {
        var pcm = WavDecoder.Decode(WavFixtures.Create([2f, -2f]));
        Assert.Equal(1f, pcm.Sample(0, 0)); Assert.Equal(-1f, pcm.Sample(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => pcm.Sample(0, 1));
        pcm.Dispose(); pcm.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pcm.Sample(0, 0));
    }

    [Fact]
    public async Task SourceReadIsBoundedAndCancellationIsHonored()
    {
        using var source = new MemoryStream(new byte[WavDecoder.MaximumSourceBytes + 1]);
        await Assert.ThrowsAsync<WavFormatException>(() => WavDecoder.ReadBytesAsync(source, TestContext.Current.CancellationToken));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WavDecoder.ReadBytesAsync(source, cancellation.Token));
    }
}
