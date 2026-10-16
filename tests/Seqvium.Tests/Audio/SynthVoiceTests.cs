// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class SynthVoiceTests
{
    internal sealed class Fixture : IDisposable
    {
        internal ProjectDocument Document = ProjectDocument.Create();
        internal Id<Pattern> Pattern;
        internal Id<MusicalPart> Part;
        internal Id<SoundDefinition> Sound;
        internal GraphAttachment Attachment = null!;
        internal Fixture(ImmutableDictionary<string, decimal>? values = null, decimal pitch = 60, int voices = 1,
            decimal intensity = .5m, long duration = 960000)
        {
            Document.Edit("Synth fixture", e =>
            {
                Sound = e.AddSound("Synth", SynthVoice.Algorithm); e.SetSoundParameters(Sound, values ?? SynthVoice.Defaults);
                Pattern = e.AddPattern("Phrase", new(1920000)); Part = e.AddPart(Pattern, "Synth", Sound);
                for (int i = 0; i < voices; i++) e.AddNote(Pattern, Part, new(0), new(duration), pitch + i * 4, intensity);
                var placement = e.AddPlacement(Pattern, new(0)); Attachment = e.CreateItemGraph(placement);
                var source = e.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                var gain = e.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Gain, new(160, 0));
                var output = e.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Output, new(320, 0));
                e.SetGraphGain(Attachment.GraphId, gain.Id, 1);
                e.BindGraphSource(Attachment.Id, source.Id, placement, Part);
                GraphFixture.Connect(e, Attachment.GraphId, source, gain); GraphFixture.Connect(e, Attachment.GraphId, gain, output);
                e.SetGraphOutput(Attachment.Id, output.Id);
            });
        }
        internal void Set(ImmutableDictionary<string, decimal> values) => Document.Edit("Shape synth", e => e.SetSoundParameters(Sound, values));
        internal SoundDefinition Definition => Document.Current.State.Sounds.Single(s => s.Id == Sound);
        public void Dispose() => Document.Close();
    }
    private static float[] Render(PreparedSampler plan, int packet = 127)
    {
        using var execution = plan.CreateExecution(); var result = new float[checked((int)plan.EndFrame * plan.Channels)];
        while (execution.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(packet, plan.EndFrame - execution.Position);
            execution.Process(result.AsSpan((int)execution.Position * plan.Channels, frames * plan.Channels));
        }
        Assert.Equal(0, execution.ActiveVoices); return result;
    }
    private static ImmutableDictionary<string, decimal> Sine => SynthVoice.Defaults.SetItem("wave1", 0)
        .SetItem("mix", 0).SetItem("attack", 0).SetItem("decay", 0).SetItem("sustain", 1).SetItem("cutoff", 18000).SetItem("resonance", 0);

    [Theory]
    [InlineData(44100, 36)] [InlineData(48000, 60)] [InlineData(44100, 69.5)] [InlineData(48000, 96)] [InlineData(48000, 120)]
    public void AudibleFundamentalFollowsFractionalMidiPitch(int rate, double midi)
    {
        using var f = new Fixture(Sine, (decimal)midi, duration: 1920000);
        using var plan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 511, cancellationToken: TestContext.Current.CancellationToken);
        var pcm = Render(plan); var crossings = new List<double>();
        for (int i = 500; i < rate / 2 - 1; i++) if (pcm[i * 2] <= 0 && pcm[(i + 1) * 2] > 0)
            crossings.Add(i - pcm[i * 2] / (double)(pcm[(i + 1) * 2] - pcm[i * 2]));
        double actual = rate * (crossings.Count - 1) / (crossings[^1] - crossings[0]);
        double expected = 440 * Math.Pow(2, (midi - 69) / 12);
        Assert.InRange(Math.Abs(actual / expected - 1), 0, .0003);
    }

    [Theory]
    [InlineData(SynthVoice.Wave.Saw)] [InlineData(SynthVoice.Wave.Pulse)] [InlineData(SynthVoice.Wave.Triangle)]
    public void PolynomialCorrectionReducesFoldedEnergyAgainstNaiveOscillator(SynthVoice.Wave wave)
    {
        const int n = 4096, bin = 487; double step = bin / (double)n;
        var corrected = new double[n]; var naive = new double[n];
        for (int i = 0; i < n; i++)
        {
            double phase = i * step % 1; corrected[i] = SynthVoice.Oscillator(wave, phase, step);
            naive[i] = wave switch { SynthVoice.Wave.Saw => 2 * phase - 1,
                SynthVoice.Wave.Triangle => 1 - 4 * Math.Abs(phase - .5), _ => phase < .5 ? 1 : -1 };
        }
        double Residual(double[] samples)
        {
            double energy = samples.Sum(x => x * x) / n, dc = samples.Average(); energy -= dc * dc;
            for (int h = 1; h * bin < n / 2; h++)
            {
                double real = 0, imaginary = 0;
                for (int i = 0; i < n; i++) { double angle = 2 * Math.PI * h * bin * i / n; real += samples[i] * Math.Cos(angle); imaginary += samples[i] * Math.Sin(angle); }
                energy -= 2 * (real * real + imaginary * imaginary) / (n * (double)n);
            }
            return Math.Max(0, energy);
        }
        Assert.True(Residual(corrected) < Residual(naive) * .35, $"Folded energy {Residual(corrected):G6} / {Residual(naive):G6}");
        Assert.InRange(corrected.Sum(x => x * x) / n, .05, 1);
    }

    [Fact]
    public void AdsrHasIndependentAttackDecaySustainAndReleaseFromActualNoteOffLevel()
    {
        using var f = new Fixture(Sine.SetItem("attack", 100).SetItem("decay", 200).SetItem("sustain", .25m).SetItem("release", 100));
        var p = SynthVoice.Prepare(f.Definition, 48000, 60);
        Assert.Equal(0, p.HeldEnvelope(0)); Assert.Equal(.5, p.HeldEnvelope(2400));
        Assert.Equal(1, p.HeldEnvelope(4800)); Assert.Equal(.625, p.HeldEnvelope(9600)); Assert.Equal(.25, p.HeldEnvelope(50000));
        Assert.Equal(.25, p.Envelope(4800, 2400)); // NoteOff halfway through Attack, not from Sustain.
        Assert.Equal(0, p.Envelope(7200, 4800));
    }

    [Theory]
    [InlineData(44100, 20, 0)] [InlineData(44100, 20, 1)] [InlineData(44100, 18000, 1)]
    [InlineData(48000, 20, 1)] [InlineData(48000, 18000, 0)] [InlineData(48000, 18000, 1)]
    public void FilterExtremesStayFiniteAndBounded(int rate, int cutoff, int resonance)
    {
        foreach (int wave in Enumerable.Range(0, 4))
        {
            using var f = new Fixture(Sine.SetItem("wave1", wave).SetItem("cutoff", cutoff).SetItem("resonance", resonance), pitch: 127, duration: 1920000);
            using var plan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 511, cancellationToken: TestContext.Current.CancellationToken); var pcm = Render(plan);
            Assert.All(pcm, sample => Assert.True(float.IsFinite(sample) && Math.Abs(sample) < 2));
        }
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public async Task PolyphonyReleasePacketsAndRealtimeMatchOfflineWithoutPcmPreparation(int rate)
    {
        using var f = new Fixture(voices: 4);
        using var plan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 511, voiceCapacity: 4, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, plan.DecodedBytes); var expected = Render(plan, 1); Assert.Equal(expected, Render(plan, 511));
        using var realtime = new RealtimeSampler(rate, 2, 511);
        using var request = realtime.BeginGraphPreparation(f.Document, f.Attachment.Id, 4); await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); realtime.Start();
        var actual = new float[expected.Length]; var buffer = new float[1022];
        for (int offset = 0; offset < actual.Length;)
        {
            int size = Math.Min(buffer.Length, actual.Length - offset); Assert.True(realtime.Process(buffer.AsSpan(0, size)));
            buffer.AsSpan(0, size).CopyTo(actual.AsSpan(offset)); offset += size;
        }
        Assert.Equal(expected, actual); Assert.Equal(0, realtime.ActiveVoices);
        Assert.Contains(actual, v => Math.Abs(v) > .01);
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 511, voiceCapacity: 3, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void VelocityFilterAndDetuneAffectRealSignalAndHeldPreviewReleases()
    {
        using var f = new Fixture(Sine);
        using var low = SamplerPreparation.PrepareSynthAudition(f.Definition, 48000, 1, 60, .25m);
        using var high = SamplerPreparation.PrepareSynthAudition(f.Definition, 48000, 1, 60, .5m);
        var a = Render(low); var b = Render(high); Assert.Equal(a.Select(x => x * 2), b);
        f.Set(Sine.SetItem("mix", 1).SetItem("wave2", 0).SetItem("detune", 100));
        using var detuned = SamplerPreparation.PrepareSynthAudition(f.Definition, 48000, 1, 60, .5m);
        Assert.NotEqual(b, Render(detuned));
        f.Set(Sine.SetItem("cutoff", 40));
        using var filtered = SamplerPreparation.PrepareSynthAudition(f.Definition, 48000, 1, 60, .5m);
        Assert.True(Render(filtered).Sum(x => (double)x * x) < b.Sum(x => (double)x * x) * .02);
        using var held = SamplerPreparation.PrepareSynthAudition(f.Definition, 48000, 1, 60, .5m, true);
        using var execution = held.CreateExecution(); var block = new float[100]; execution.Process(block);
        execution.ReleaseAudition(); while (execution.Position < execution.EndFrame) execution.Process(new float[(int)Math.Min(100, execution.EndFrame - execution.Position)]);
        Assert.Equal(0, execution.ActiveVoices); Assert.True(execution.Position < 48000);
    }

    [Fact]
    public void PortableHistoryAndSelectiveReaderGatePreserveIntentAndRejectInvalidState()
    {
        using var f = new Fixture(); using var directory = new TemporaryDirectory();
        var original = f.Definition.Parameters; f.Set(SynthVoice.Presets[1].Values); Assert.True(f.Document.Undo());
        Assert.Equal(original, f.Definition.Parameters); Assert.True(f.Document.Redo());
        string path = Path.Combine(directory.Path, "synth.seqvium"); ProjectPersistence.Save(f.Document, path);
        using var stream = File.OpenRead(path); var loaded = ProjectPersistence.Read(stream).Document;
        Assert.Equal(f.Definition.Parameters.OrderBy(p => p.Key), loaded.Current.State.Sounds.Single().Parameters.OrderBy(p => p.Key));
        var json = JsonNode.Parse(ProjectPersistence.Encode(f.Document))!; Assert.Equal(11, json["minimumReaderMinor"]!.GetValue<int>());
        json["minimumReaderMinor"] = 10;
        Assert.Throws<ProjectFormatException>(() => ProjectPersistence.Read(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()))));
        Assert.False(SynthVoice.Supports(f.Definition with { Parameters = original.SetItem("wave1", .5m) }));
        Assert.False(SynthVoice.Supports(f.Definition with { Parameters = original.Remove("attack") }));
        Assert.False(SynthVoice.Supports(f.Definition with { Parameters = original.Add("future", 1) }));
        loaded.Close();
    }
}
