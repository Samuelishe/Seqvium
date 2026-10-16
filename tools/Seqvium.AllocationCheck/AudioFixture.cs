// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.AllocationCheck;

internal sealed class AudioFixture : IDisposable
{
    internal WavAudition? Audition;
    internal OfflineSampler? Execution;
    internal float[] Packet = [];
    private PreparedSampler? _plan;
    private ProjectDocument? _document;
    private string? _directory;

    internal static AudioFixture Create(string scenario)
    {
        var fixture = new AudioFixture();
        try
        {
            if (scenario == "preview")
            {
                fixture.Audition = new(48000, 2, 256);
                using var request = fixture.Audition.BeginExternal("fixture.wav");
                using var source = new MemoryStream(Wave(new float[48000], 1));
                // This console setup has no owner context; all async work is joined before measurement.
                request.PrepareAsync(source).GetAwaiter().GetResult();
                if (request.Publish() != SamplerPublication.Accepted) throw new InvalidOperationException("Preview publication.");
                fixture.Packet = new float[512];
            }
            if (scenario == "mixer")
            {
                fixture._directory = Path.Combine(Path.GetTempPath(), "Seqvium-allocation-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(fixture._directory);
                var document = fixture._document = ProjectDocument.Create();
                using var import = ProjectMedia.BeginImport(document, "Signal", releaseMilliseconds: 0,
                    ownedMediaDirectory: fixture._directory);
                var samples = new float[48000];
                for (int i = 0; i < samples.Length; i += 2) { samples[i] = .2f; samples[i + 1] = .1f; }
                using var source = new MemoryStream(Wave(samples, 2));
                import.PrepareAsync(source).GetAwaiter().GetResult();
                import.Accept((edit, media) =>
                {
                    var pattern = edit.AddPattern("Pattern", new(960000));
                    var part = edit.AddPart(pattern, "Instrument", media.SoundId);
                    edit.AddNote(pattern, part, new(0), new(960000), 60, 1);
                    var placement = edit.AddPlacement(pattern, new(0));
                    var attachment = edit.CreateItemGraph(placement);
                    var input = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                    var gain = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Gain, new(100, 0));
                    var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(200, 0));
                    edit.SetGraphGain(attachment.GraphId, gain.Id, .5m);
                    edit.BindGraphSource(attachment.Id, input.Id, placement, part);
                    edit.ConnectGraphPorts(attachment.GraphId, input.Id, input.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id,
                        gain.Id, gain.Ports.Single(p => p.Direction == GraphPortDirection.Input).Id);
                    edit.ConnectGraphPorts(attachment.GraphId, gain.Id, gain.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id,
                        output.Id, output.Ports.Single(p => p.Direction == GraphPortDirection.Input).Id);
                    edit.SetGraphOutput(attachment.Id, output.Id);
                    edit.AddArrangementClip(placement, new(0), 0, 1);
                });
                document.Edit("Mixer", e => e.InitializeMixer());
                document.Edit("Strip", e => e.SetMixerStrip(document.Current.State.Mixer!.Channels[0].Id, new()
                {
                    TrimDb = 6, Eq = new() { Enabled = true, HighPassHz = 50, MidDb = 4, MidQ = 2, HighDb = -3 },
                    Compressor = new() { Enabled = true, ThresholdDb = -24, Ratio = 4 }
                }));
                fixture._plan = ArrangementPreparation.Prepare(document, 48000, 2, 127);
                fixture.Execution = fixture._plan.CreateExecution();
                fixture.Packet = new float[254];
            }
            return fixture;
        }
        catch { fixture.Dispose(); throw; }
    }

    internal void Reset()
    {
        if (Audition is not null) { Audition.Processor.Start(); Audition.Processor.Process(Packet); }
        if (Execution is not null)
        {
            Execution.Restart();
            for (int i = 0; i < 20; i++) Execution.Process(Packet);
        }
    }

    internal long Position => Audition?.Processor.ReadStatus().Position ?? Execution?.Position ?? 0;
    internal int Voices => Audition?.Processor.ReadStatus().Voices ?? Execution?.ActiveVoices ?? 0;

    internal void CheckRenderedWindow()
    {
        if (Audition is not null && (Position != 38556 || Voices != 1 || !Audition.Processor.ReadStatus().Playing))
            throw new InvalidOperationException("Preview did not render the historical active-voice window.");
        if (Execution is not null && (Position != 8890 || Voices != 1))
            throw new InvalidOperationException("Mixer did not render the historical active-voice window.");
    }

    private static byte[] Wave(float[] samples, int channels)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8); writer.Write(36 + samples.Length * 4); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((ushort)3); writer.Write((ushort)channels); writer.Write(48000);
        writer.Write(48000 * channels * 4); writer.Write((ushort)(channels * 4)); writer.Write((ushort)32);
        writer.Write("data"u8); writer.Write(samples.Length * 4);
        foreach (float value in samples) writer.Write(value);
        writer.Flush();
        return stream.ToArray();
    }

    public void Dispose()
    {
        Audition?.Dispose(); Execution?.Dispose(); _plan?.Dispose(); _document?.Close();
        if (_directory is not null) Directory.Delete(_directory, recursive: true);
    }
}
