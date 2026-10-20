// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class PolyphonyTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal Id<SoundDefinition> Sound;
        internal Id<Pattern> Pattern;
        internal Id<MusicalPart> Part;
        internal Id<PatternPlacement> Placement;
        internal GraphAttachment Attachment = null!;
        internal int Channels;
        internal string Kind = "pcm";
        internal const long Length = 192000, Gate = 9600;

        internal static float Sample(int frame, int channel, int layer = 0) =>
            (float)(Math.Sin(frame * .031) * .004 * (channel == 0 ? 1 : -.7) * (layer == 0 ? 1 : -.5));

        internal static async Task<Fixture> Create(int count, int channels = 2, string kind = "pcm", int sourceRate = 48000)
        {
            var f = new Fixture { Channels = channels, Kind = kind };
            try
            {
                var first = await WavFixtures.Import(f.Document, f.Directory,
                    [.. Enumerable.Range(0, 4096).SelectMany(i => Enumerable.Range(0, channels).Select(c => Sample(i, c)))],
                    sourceRate, channels, release: 3);
                var second = kind == "layers" ? await WavFixtures.Import(f.Document, f.Directory,
                    [.. Enumerable.Range(0, 4096).SelectMany(i => Enumerable.Range(0, channels).Select(c => Sample(i, c, 1)))],
                    sourceRate, channels, release: 3) : first;
                f.Document.Edit("Musical polyphony", e =>
                {
                    f.Sound = first.SoundId;
                    if (kind is "synth" or "drum" or "hat")
                    {
                        var hat = DrumInstruments.All.Single(d => d.Key == "ClosedHat");
                        f.Sound = e.AddSound(kind, kind == "synth" ? SynthVoice.Algorithm : kind == "hat" ? hat.Algorithm : DrumVoice.Algorithm);
                        e.SetSoundParameters(f.Sound, kind == "synth" ? SynthVoice.Defaults.SetItem("release", 3)
                            : kind == "hat" ? hat.Defaults : DrumVoice.Defaults.SetItem("decay", 60));
                    }
                    if (kind == "layers")
                        e.SetSamplerZones(f.Sound,
                        [
                            new(Id<SamplerZone>.New(), first.ResourceId, 60, 0, 127, ImmutableDictionary<string, decimal>.Empty)
                                { LowVelocity = 0, HighVelocity = .5m },
                            new(Id<SamplerZone>.New(), second.ResourceId, 60, 0, 127, ImmutableDictionary<string, decimal>.Empty)
                                { LowVelocity = .5m, HighVelocity = 1 }
                        ]);
                    f.Pattern = e.AddPattern("Chord", new(Length));
                    f.Part = e.AddPart(f.Pattern, kind, f.Sound);
                    e.SetPartNotes(f.Pattern, f.Part, Notes(count, kind));
                    f.Placement = e.AddPlacement(f.Pattern, default);
                    f.Attachment = e.CreateItemGraph(f.Placement);
                    string layout = channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
                    var source = e.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Source, new(0, 0), layout);
                    var output = e.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Output, new(180, 0), layout);
                    GraphFixture.Connect(e, f.Attachment.GraphId, source, output);
                    e.BindGraphSource(f.Attachment.Id, source.Id, f.Placement, f.Part);
                    e.SetGraphOutput(f.Attachment.Id, output.Id);
                });
                return f;
            }
            catch { f.Dispose(); throw; }
        }

        internal static ImmutableArray<NoteEvent> Notes(int count, string kind = "pcm", long start = 0, long duration = Gate) =>
            [.. Enumerable.Range(0, count).Select(i => new NoteEvent(Id<NoteEvent>.New(), new(start), new(duration),
                kind == "drum" ? 36 : 48 + i % 24 + i % 3 * .125m,
                kind == "layers" ? i % 2 == 0 ? .2m : .7m : .01m + i % 7 * .001m))];

        internal PreparedSampler Prepare(int rate = 48000, int capacity = GraphPreparation.MaximumVoices, int packet = 511) =>
            GraphPreparation.Prepare(Document, Attachment.Id, rate, Channels, packet, capacity,
                cancellationToken: TestContext.Current.CancellationToken);
        public void Dispose() { Document.Close(); Directory.Dispose(); }
    }

    public static TheoryData<int, int, int> Capacities
    {
        get
        {
            var data = new TheoryData<int, int, int>();
            foreach (int n in new[] { 1, 8, 16, 32, 64, 96, 127, 128 })
            foreach (int rate in new[] { 44100, 48000 })
            foreach (int channels in new[] { 1, 2 })
                if (n <= GraphPreparation.MaximumVoices) data.Add(n, rate, channels);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Capacities))]
    public async Task MusicalCapacityHasIndependentPcmOracleAndExactPacketRealtimeParity(int count, int rate, int channels)
    {
        using var f = await Fixture.Create(count, channels, sourceRate: rate == 44100 ? 48000 : 44100);
        using var plan = f.Prepare(rate);
        float[] expected = Render(plan, 1), partitioned = Render(plan, 511);
        Assert.Equal(expected, partitioned);
        var notes = f.Document.Current.State.Patterns.Single().Parts.Single().Notes.OrderBy(n => n.Id.Value).ToArray();
        int gate = (int)ConstantTempoConversion.ToFrames(new(Fixture.Gate), new(120), rate);
        int release = (int)Math.Round(rate * .003, MidpointRounding.AwayFromZero);
        int sourceRate = rate == 44100 ? 48000 : 44100;
        for (int frame = 0; frame < expected.Length / channels; frame++)
        for (int c = 0; c < channels; c++)
        {
            float oracle = 0;
            if (frame < gate + release)
                foreach (var note in notes)
                {
                    double cursor = frame * sourceRate / (double)rate * Math.Pow(2, (double)(note.Pitch - 60) / 12);
                    int whole = (int)cursor;
                    double value = Fixture.Sample(whole, c) +
                                   (Fixture.Sample(whole + 1, c) - Fixture.Sample(whole, c)) * (cursor - whole);
                    double gain = (float)note.Intensity * (frame < gate ? 1 : 1 - (frame - gate) / (double)release);
                    oracle += (float)(value * gain);
                }
            Assert.InRange(Math.Abs(expected[frame * channels + c] - oracle), 0, 2e-8);
        }
        using var rt = new RealtimeSampler(rate, channels, 511);
        using var request = rt.BeginGraphPreparation(f.Document, f.Attachment.Id, GraphPreparation.MaximumVoices);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); rt.Start();
        var actual = new float[expected.Length];
        for (int at = 0; at < actual.Length;)
        {
            int size = Math.Min(127 * channels, actual.Length - at);
            Assert.True(rt.Process(actual.AsSpan(at, size))); at += size;
        }
        Assert.Equal(expected, actual); Assert.Equal(count, rt.PeakVoices); Assert.Equal(0, rt.ActiveVoices);
        Assert.Equal(SamplerTransportState.Ended, rt.ReadStatus().Transport);
    }

    [Theory]
    [InlineData("synth", 44100, 1)] [InlineData("synth", 48000, 2)]
    [InlineData("drum", 44100, 2)] [InlineData("drum", 48000, 1)]
    [InlineData("layers", 44100, 1)] [InlineData("layers", 48000, 2)]
    public async Task InstrumentsUseEverySlotWithoutStealingAndRetireAtTheirOwnEnd(string kind, int rate, int channels)
    {
        using var f = await Fixture.Create(GraphPreparation.MaximumVoices, channels, kind);
        using var plan = f.Prepare(rate);
        float[] pcm = Render(plan, 1); Assert.Equal(pcm, Render(plan, 511));
        using var rt = new RealtimeSampler(rate, channels, 511);
        using var request = rt.BeginGraphPreparation(f.Document, f.Attachment.Id, GraphPreparation.MaximumVoices);
        await request.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        rt.Start(); var actual = new float[pcm.Length];
        for (int at = 0; at < actual.Length;)
        {
            int size = Math.Min(113 * channels, actual.Length - at); rt.Process(actual.AsSpan(at, size)); at += size;
        }
        Assert.Equal(pcm, actual); Assert.Equal(GraphPreparation.MaximumVoices, rt.PeakVoices);
        Assert.Equal(0, rt.ActiveVoices); Assert.Contains(pcm, x => Math.Abs(x) > .00001);
        if (kind == "layers")
        {
            // Independent layer selection/interpolation oracle, before the NoteOff.
            var notes = f.Document.Current.State.Patterns.Single().Parts.Single().Notes.OrderBy(n => n.Id.Value);
            float oracle = 0; const int frame = 37;
            foreach (var note in notes)
            {
                int layer = note.Intensity < .5m ? 0 : 1;
                double cursor = frame * 48000d / rate * Math.Pow(2, (double)(note.Pitch - 60) / 12);
                int whole = (int)cursor;
                double value = Fixture.Sample(whole, 0, layer) + (Fixture.Sample(whole + 1, 0, layer) -
                    Fixture.Sample(whole, 0, layer)) * (cursor - whole);
                oracle += (float)(value * (float)note.Intensity);
            }
            Assert.InRange(Math.Abs(pcm[frame * channels] - oracle), 0, 2e-8);
        }
    }

    [Theory]
    [InlineData(32, 0)] [InlineData(64, 0)] [InlineData(32, 3)]
    public async Task CoincidentOffOnKeepsOriginalSlotsReleaseAndBoundedEventWork(int chord, int releaseMs)
    {
        using var f = await Fixture.Create(chord);
        f.Document.Edit("Turnover", e =>
        {
            var sound = f.Document.Current.State.Sounds.Single(s => s.Id == f.Sound);
            e.SetSoundParameters(f.Sound, sound.Parameters.SetItem("releaseMilliseconds", releaseMs));
            var first = Fixture.Notes(chord);
            var second = Fixture.Notes(chord, start: Fixture.Gate);
            e.SetPartNotes(f.Pattern, f.Part, first.AddRange(second));
        });
        using var plan = f.Prepare();
        Assert.True(plan.MaximumPacketEvents >= chord * 2);
        Assert.InRange(plan.PacketWork, 1, GraphPreparation.MaximumPacketWork);
        using var x = plan.CreateExecution(); var block = new float[480]; x.Process(block); x.Process(block);
        Assert.Equal(releaseMs == 0 ? chord : chord * 2, x.PeakVoices);
        Assert.Equal(Render(plan, 1), Render(plan, 511));
    }

    [Fact]
    public async Task ActualOverlapAboveCapacityRefusesBeforePublicationAndRawLimitStaysEight()
    {
        using var f = await Fixture.Create(GraphPreparation.MaximumVoices + 1);
        Assert.Throws<GraphPreparationException>(() => f.Prepare());
        using var rt = new RealtimeSampler(48000, 2, 511);
        using (var request = rt.BeginGraphPreparation(f.Document, f.Attachment.Id, GraphPreparation.MaximumVoices))
            await Assert.ThrowsAsync<GraphPreparationException>(() => request.PrepareAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, rt.LivePreparedStates); Assert.Equal(0, rt.ReservedPcmAndScratchBytes);
        Assert.Throws<NotSupportedException>(() => SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, voiceCapacity: 9));
        f.Document.Edit("Release overlap", e => e.SetPartNotes(f.Pattern, f.Part,
            Fixture.Notes(GraphPreparation.MaximumVoices).AddRange(Fixture.Notes(1, start: Fixture.Gate))));
        Assert.Throws<GraphPreparationException>(() => f.Prepare());
    }

    [Theory]
    [InlineData(65)] [InlineData(96)] [InlineData(127)] [InlineData(128)]
    public async Task UnacceptedCapacitiesRefuseWithActualOverlapAndLimit(int voices)
    {
        using var f = await Fixture.Create(voices);
        var error = Assert.Throws<GraphPreparationException>(() => f.Prepare());
        Assert.Contains("Voice capacity 64 exceeded (65 overlapping voices", error.InnerException!.Message);
    }

    [Fact]
    public async Task DensityRefusesChokedEventFloodEvenWhenVoicesDoNotOverlap()
    {
        using var f = await Fixture.Create(GraphPreparation.MaximumEventsPerFrame + 1, kind: "hat");
        var error = Assert.Throws<GraphPreparationException>(() => f.Prepare());
        Assert.Equal(GraphPreparationReasons.Budget, error.Reason);
    }

    [Fact]
    public async Task PrimedMaximumVoiceCallbackAndDenseTurnoverAllocateStrictlyZeroBytes()
    {
        using var f = await Fixture.Create(GraphPreparation.MaximumVoices);
        f.Document.Edit("Long gates", e => e.SetPartNotes(f.Pattern, f.Part,
            Fixture.Notes(GraphPreparation.MaximumVoices, duration: Fixture.Length)));
        using var plan = f.Prepare(); using var x = plan.CreateExecution();
        var block = new float[64]; x.Process(block); x.Process(block);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++) x.Process(block);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, bytes); Assert.Equal(GraphPreparation.MaximumVoices, x.ActiveVoices);
        x.Stop(); Assert.Equal(0, x.ActiveVoices);
    }

    private static float[] Render(PreparedSampler plan, int packet, bool smooth = false)
    {
        using var x = plan.CreateExecution(smooth);
        var result = new float[checked((int)(plan.EndFrame - plan.StartFrame) * plan.Channels)];
        for (int at = 0; at < result.Length;)
        {
            int frames = Math.Min(packet, (result.Length - at) / plan.Channels);
            x.Process(result.AsSpan(at, frames * plan.Channels)); at += frames * plan.Channels;
        }
        Assert.Equal(0, x.ActiveVoices); return result;
    }
}
