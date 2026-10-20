// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;

// Persist each authored fixture once: baseline and candidate read identical identities/media/order.
internal sealed class PolyphonyFixture : IDisposable
{
    internal ProjectDocument Document { get; }
    internal GraphAttachment Attachment => Document.Current.State.GraphAttachments[0];
    internal PolyphonyFixture(ProjectDocument document) => Document = document;

    internal static async Task<PolyphonyFixture> Open(string directory, string kind, int voices, int channels)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"{kind}-{voices}-{channels}.seqvium");
        if (File.Exists(path)) return new(ProjectPersistence.Open(path).Document);
        var doc = ProjectDocument.Create(new(new Tempo(120), new Meter(4, 4)));
        try
        {
            Id<SoundDefinition> pcm;
            using (var wave = new MemoryStream())
            {
                using (var w = new BinaryWriter(wave, System.Text.Encoding.ASCII, true))
                {
                    const int frames = 4096, rate = 48000;
                    w.Write("RIFF"u8); w.Write(36 + frames * channels * 4); w.Write("WAVEfmt "u8);
                    w.Write(16); w.Write((ushort)3); w.Write((ushort)channels); w.Write(rate);
                    w.Write(rate * channels * 4); w.Write((ushort)(channels * 4)); w.Write((ushort)32);
                    w.Write("data"u8); w.Write(frames * channels * 4);
                    for (int i = 0; i < frames; i++)
                    for (int c = 0; c < channels; c++)
                        w.Write((float)(.03 * Math.Sin(2 * Math.PI * i / 128) * (c == 0 ? 1 : -.7)));
                }
                wave.Position = 0;
                using var import = ProjectMedia.BeginImport(doc, "Authored periodic PCM", releaseMilliseconds: 40,
                    ownedMediaDirectory: path + ".source");
                await import.PrepareAsync(wave);
                pcm = import.Accept().SoundId;
            }
            doc.Edit("Polyphony workload", e =>
            {
                var definition = doc.Current.State.Sounds.Single(s => s.Id == pcm);
                e.SetSoundParameters(pcm, definition.Parameters.SetItem("sampleStart", 0).SetItem("sampleEnd", 4096)
                    .SetItem("loopStart", 128).SetItem("loopEnd", 3968).SetItem("loopEnabled", 1));
                var synth = e.AddSound("Subtractive", SynthVoice.Algorithm);
                e.SetSoundParameters(synth, SynthVoice.Defaults.SetItem("release", kind == "release" ? 6000 : 40));
                if (kind is "synth-expression" or "synth-vibrato")
                    e.SetSoundParameters(synth, SynthVoice.Defaults.SetItem("release", 400)
                        .SetItem("filterAttack", 700).SetItem("filterDecay", 1100).SetItem("filterSustain", .3m)
                        .SetItem("filterRelease", 450).SetItem("filterAmount", 4).SetItem("velocityCutoff", 2)
                        .SetItem("lfoTarget", kind == "synth-vibrato" ? 1 : 2).SetItem("lfoRate", 5.5m)
                        .SetItem("lfoPitch", 35).SetItem("lfoCutoff", 2));
                var drum = e.AddSound("Kick", DrumVoice.Algorithm);
                e.SetSoundParameters(drum, DrumVoice.Defaults.SetItem("decay", 60));
                int parts = kind is "mixed" or "live" ? 3 : 1;
                for (int p = 0; p < parts; p++)
                {
                    bool dense = kind == "dense", longRelease = kind == "release";
                    var sound = parts == 3 ? p == 0 ? pcm : p == 1 ? synth : drum
                        : kind is "synth" or "release" or "synth-expression" or "synth-vibrato" ? synth : pcm;
                    long length = longRelease ? 30720000 : 1920000;
                    var pattern = e.AddPattern($"{kind} {p}", new(length));
                    var part = e.AddPart(pattern, $"Instrument {p}", sound);
                    int count = voices / parts + (p < voices % parts ? 1 : 0);
                    var notes = ImmutableArray.CreateBuilder<NoteEvent>();
                    for (int chord = 0; chord < (dense ? 8 : 1); chord++)
                    for (int n = 0; n < count; n++)
                        notes.Add(new(Id<NoteEvent>.New(), new(chord * 240000),
                            new(longRelease ? 15360000 : dense ? 96000 : 960000),
                            sound == drum ? 36 : 48 + n % 36 + n % 3 * .125m, .035m + n % 5 * .003m));
                    e.SetPartNotes(pattern, part, notes.ToImmutable());
                    var placement = e.AddPlacement(pattern, default);
                    var a = e.CreateItemGraph(placement);
                    string layout = channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
                    var source = e.AddGraphNode(a.GraphId, GraphBuiltIns.Source, new(0, 0), layout);
                    var gain = e.AddGraphNode(a.GraphId, GraphBuiltIns.Gain, new(160, 0), layout);
                    var output = e.AddGraphNode(a.GraphId, GraphBuiltIns.Output, new(320, 0), layout);
                    e.SetGraphGain(a.GraphId, gain.Id, 1);
                    e.BindGraphSource(a.Id, source.Id, placement, part);
                    Connect(e, a.GraphId, source, gain); Connect(e, a.GraphId, gain, output);
                    e.SetGraphOutput(a.Id, output.Id);
                    e.AddArrangementClip(placement, default, p, longRelease ? 1 : 16);
                }
                if (parts == 3) e.InitializeMixer();
            });
            ProjectPersistence.Save(doc, path);
            return new(doc);
        }
        catch { doc.Close(); throw; }
    }

    private static void Connect(ProjectEdit e, Id<GraphDefinition> graph, GraphNode from, GraphNode to) =>
        e.ConnectGraphPorts(graph, from.Id, from.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id,
            to.Id, to.Ports.Single(p => p.Direction == GraphPortDirection.Input).Id);
    public void Dispose() => Document.Close();
}
