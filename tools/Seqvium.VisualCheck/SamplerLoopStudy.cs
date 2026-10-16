// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task SamplerLoopComparisonsAsync(string output)
    {
        string directory = Path.Combine(output, "loop-comparisons"); Directory.CreateDirectory(directory);
        var measurements = new List<object>();
        foreach (string kind in new[] { "percussion", "sustain", "melodic" })
        {
            const int rate = 48000, frames = 24000;
            var authored = new float[frames * 2]; uint noise = 751;
            for (int i = 0; i < frames; i++)
            {
                double time = i / (double)rate;
                noise = unchecked(noise * 1664525 + 1013904223);
                double random = noise / (double)uint.MaxValue * 2 - 1;
                for (int channel = 0; channel < 2; channel++)
                {
                    double phase = time * Math.Tau * (kind == "melodic" ? 261.6256 : 220);
                    double value = kind switch
                    {
                        "percussion" => Math.Exp(-time * 18) * (.65 * Math.Sin(time * Math.Tau * 80) + .35 * random),
                        "sustain" => .75 * Math.Sin(phase) + .2 * Math.Sin(phase * 2 + channel * .3),
                        _ => Math.Exp(-time * 1.2) * (.7 * Math.Sin(phase + .3 * Math.Sin(time * Math.Tau * 4)) +
                            .25 * Math.Sin(time * Math.Tau * 392 + channel * .2))
                    };
                    authored[i * 2 + channel] = (float)(value * .3 * Math.Min(1, i / 64d));
                }
            }
            string original = Path.Combine(directory, kind + "-source.wav"); WriteLoopFloatWave(original, authored);
            var document = ProjectDocument.Create();
            try
            {
                using var import = ProjectMedia.BeginImport(document, kind, 60, 80);
                using var input = File.OpenRead(original); await import.PrepareAsync(input); var accepted = import.Accept();
                Id<Pattern> pattern = default;
                document.Edit("Listening study", e =>
                {
                    pattern = e.AddPattern(kind, new(3840000)); var part = e.AddPart(pattern, kind, accepted.SoundId);
                    e.AddNote(pattern, part, new(0), new(3840000), 60, .7m);
                });
                byte[]? f1 = null;
                foreach (var (mode, length) in new[] { ("f1", 0), ("off", 0), ("short", 128), ("long", 4096) })
                {
                    var sound = document.Current.State.Sounds.Single();
                    document.Edit(mode, e => e.SetSoundParameters(sound.Id,
                        new SamplerPlayback(2400, 21600, true, 7200, 16800, length).Apply(sound, frames)));
                    using var plan = SamplerPreparation.PreparePattern(document, pattern, rate, 2);
                    using var execution = plan.CreateExecution(smoothBoundaries: true);
                    var pcm = new float[checked((int)plan.EndFrame * 2)]; int offset = 0;
                    while (execution.Position < plan.EndFrame)
                    {
                        int count = (int)Math.Min(127, plan.EndFrame - execution.Position);
                        execution.Process(pcm.AsSpan(offset, count * 2)); offset += count * 2;
                    }
                    Require(execution.ActiveVoices == 0 && pcm.All(float.IsFinite), "Listening render did not retire.");
                    string path = Path.Combine(directory, kind + "-" + mode + ".wav"); WriteLoopFloatWave(path, pcm);
                    if (mode == "f1") f1 = File.ReadAllBytes(path);
                    if (mode == "off") Require(f1!.SequenceEqual(File.ReadAllBytes(path)), "Crossfade zero changed F1 audio.");
                    double dc = 0, energy = 0, seam = 0;
                    for (int i = 24000; i < 96000; i++) { double v = pcm[i * 2]; dc += v; energy += v * v; }
                    for (int i = 14400; i < 96000; i += 9600) seam = Math.Max(seam, Math.Abs(pcm[i * 2] - pcm[(i - 1) * 2]));
                    measurements.Add(new { Source = kind, Mode = mode, CrossfadeFrames = length, plan.EndFrame,
                        Mean = dc / 72000, Rms = Math.Sqrt(energy / 72000), SeamDelta = seam });
                }
            }
            finally { document.Close(); }
        }
        Json(directory, "measurements", new { Measurements = measurements, SubjectiveListening = false,
            ZeroMode = "Exact F1 gain dip, byte-identical comparisons", PeriodSourceFrames = 9600,
            Crossfade = "Complementary smoothstep; tail [B-X,B) with pre-loop [A-X,A); fixed B-A period" });
    }

    private static void WriteLoopFloatWave(string path, float[] samples)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF"u8); writer.Write(36 + samples.Length * 4); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((ushort)3); writer.Write((ushort)2); writer.Write(48000); writer.Write(48000 * 8);
        writer.Write((ushort)8); writer.Write((ushort)32); writer.Write("data"u8); writer.Write(samples.Length * 4);
        foreach (float sample in samples) writer.Write(sample);
    }
}
