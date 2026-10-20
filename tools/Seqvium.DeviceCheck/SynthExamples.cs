// SPDX-License-Identifier: Apache-2.0
using System.Security.Cryptography;
using System.Text.Json;
using Seqvium.Core;

/// <summary>Small original, reproducible musical comparisons exported by the production Seqvium writer.</summary>
internal static class SynthExamples
{
    internal static async Task<int> Run(string output)
    {
        Directory.CreateDirectory(output);
        var doc = ProjectDocument.Create(new(new Tempo(100), new Meter(4, 4)));
        try
        {
            Id<SoundDefinition> sound = default; GraphAttachment attachment = null!;
            var values = SynthVoice.Defaults.SetItem("cutoff", 900).SetItem("attack", 12)
                .SetItem("decay", 380).SetItem("sustain", .55m).SetItem("release", 450);
            doc.Edit("Original F2 study", e =>
            {
                sound = e.AddSound("Expression study", SynthVoice.Algorithm); e.SetSoundParameters(sound, values);
                var pattern = e.AddPattern("Opening and answer", new(15360000));
                var part = e.AddPart(pattern, "Expression", sound);
                // A minor ninth followed by a quiet/strong answer; no official-demo material or media.
                foreach (int pitch in new[] { 45, 52, 60, 71 })
                    e.AddNote(pattern, part, default, new(6720000), pitch, .24m);
                decimal[] answer = [64, 67, 71, 72, 71, 67, 64, 59];
                for (int i = 0; i < answer.Length; i++)
                    e.AddNote(pattern, part, new(7680000 + i * 960000), new(720000), answer[i], i % 2 == 0 ? .25m : .85m);
                var placement = e.AddPlacement(pattern, default); attachment = e.CreateItemGraph(placement);
                var source = e.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                var gain = e.AddGraphNode(attachment.GraphId, GraphBuiltIns.Gain, new(160, 0));
                var sink = e.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(320, 0));
                e.SetGraphGain(attachment.GraphId, gain.Id, .65m);
                e.BindGraphSource(attachment.Id, source.Id, placement, part);
                e.ConnectGraphPorts(attachment.GraphId, source.Id, source.Ports.Single().Id,
                    gain.Id, gain.Ports.Single(p => p.Direction == GraphPortDirection.Input).Id);
                e.ConnectGraphPorts(attachment.GraphId, gain.Id, gain.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id,
                    sink.Id, sink.Ports.Single().Id);
                e.SetGraphOutput(attachment.Id, sink.Id);
            });
            var variants = new[]
            {
                ("01-neutral", values),
                ("02-filter-envelope", values.SetItem("filterAttack", 1400).SetItem("filterDecay", 1700)
                    .SetItem("filterSustain", .15m).SetItem("filterRelease", 450).SetItem("filterAmount", 3.5m)),
                ("03-vibrato", values.SetItem("lfoTarget", 1).SetItem("lfoRate", 5.5m).SetItem("lfoPitch", 28)),
                ("04-velocity", values.SetItem("cutoff", 5000).SetItem("velocityCutoff", 4)),
                ("05-filter-lfo", values.SetItem("lfoTarget", 2).SetItem("lfoRate", 1.5m).SetItem("lfoCutoff", 2))
            };
            var reports = new List<object>(); float[]? neutral = null;
            foreach (var (name, parameters) in variants)
            {
                doc.Edit("Compare expression", e => e.SetSoundParameters(sound, parameters));
                ProjectPersistence.Save(doc, Path.Combine(output, name + ".seqvium"));
                string path = Path.Combine(output, name + ".wav");
                var exported = await PatternWaveExport.ExportAsync(doc, attachment.Id, path);
                byte[] bytes = await File.ReadAllBytesAsync(path);
                using var pcm = WavDecoder.Decode(bytes);
                var mono = Enumerable.Range(0, pcm.Frames).Select(i => pcm.Sample(i, 0)).ToArray();
                neutral ??= mono;
                double error = mono.Zip(neutral).Sum(x => Math.Pow(x.First - x.Second, 2)) / mono.Length;
                double peak = mono.Max(x => Math.Abs(x)), energy = mono.Sum(x => (double)x * x) / mono.Length;
                bool passed = exported.ClippedSamples == 0 && peak is > .001 and < 1 &&
                    pcm.SampleRate == 48000 && pcm.Channels == 2 && (name == "01-neutral" || error > 1e-6);
                reports.Add(new { name, Export = exported, pcm.SampleRate, pcm.Channels, pcm.Frames,
                    Peak = peak, Rms = Math.Sqrt(energy), DifferenceRms = Math.Sqrt(error),
                    Sha256 = Convert.ToHexString(SHA256.HashData(bytes)), Passed = passed });
                if (!passed) throw new InvalidOperationException("Musical example failed: " + name);
            }
            await File.WriteAllTextAsync(Path.Combine(output, "examples.json"),
                JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        finally { doc.Close(); }
    }
}
