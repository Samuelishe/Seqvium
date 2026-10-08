// SPDX-License-Identifier: Apache-2.0
using System.Text;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

internal static class WavFixtures
{
    // Tiny project-authored fixtures only; this writer is independent of the production parser.
    internal static byte[] Create(float[] samples, int rate = 48000, int channels = 1, bool floating = true,
        bool extensible = false, bool oddChunk = false, bool extended = false)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("RIFF"u8); writer.Write(0); writer.Write("WAVE"u8);
        if (oddChunk) { writer.Write("JUNK"u8); writer.Write(3); writer.Write(new byte[] { 7, 8, 9, 0 }); }
        int bits = floating ? 32 : 16;
        writer.Write("fmt "u8); writer.Write(extensible ? 40 : extended ? 18 : 16);
        writer.Write((ushort)(extensible ? 0xfffe : floating ? 3 : 1)); writer.Write((ushort)channels);
        writer.Write(rate); writer.Write(rate * channels * bits / 8);
        writer.Write((ushort)(channels * bits / 8)); writer.Write((ushort)bits);
        if (extensible)
        {
            writer.Write((ushort)22); writer.Write((ushort)bits); writer.Write(channels == 1 ? 4 : 3);
            writer.Write(new Guid(floating ? "00000003-0000-0010-8000-00aa00389b71" : "00000001-0000-0010-8000-00aa00389b71").ToByteArray());
        }
        else if (extended) writer.Write((ushort)0);
        if (floating) { writer.Write("fact"u8); writer.Write(4); writer.Write(samples.Length / channels); }
        writer.Write("data"u8); writer.Write(samples.Length * bits / 8);
        foreach (float sample in samples)
            if (floating) writer.Write(sample);
            else writer.Write((short)Math.Clamp((int)(sample * 32768), short.MinValue, short.MaxValue));
        writer.Flush();
        stream.Position = 4; writer.Write((int)stream.Length - 8); writer.Flush();
        return stream.ToArray();
    }

    internal static async Task<MediaAcceptance> Import(ProjectDocument document, TemporaryDirectory directory,
        float[] samples, int rate = 48000, int channels = 1, decimal root = 60m, decimal release = 0m)
    {
        using var import = ProjectMedia.BeginImport(document, "Owned fixture", root, release, ownedMediaDirectory: directory.File("owned"));
        using var source = new MemoryStream(Create(samples, rate, channels));
        await import.PrepareAsync(source, TestContext.Current.CancellationToken);
        return import.Accept(TestContext.Current.CancellationToken);
    }
}
