// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class AudioTimeStretchTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal Id<AudioClip> Id;
        internal AudioClip Clip => Document.Current.State.AudioClips.Single(c => c.Id == Id);
        internal int Rate;
        internal float[] Samples = [];
        internal static async Task<Fixture> Create(int rate = 48000, int channels = 2)
        {
            var f = new Fixture { Rate = rate };
            try
            {
                f.Samples = Signal(rate, channels, rate * 2);
                string path = f.Directory.File("phrase.wav");
                File.WriteAllBytes(path, WavFixtures.Create(f.Samples, rate, channels));
                string normalized = f.Directory.File("normalized.wav");
                AudioFileDecode.Normalize(path, normalized, TestContext.Current.CancellationToken);
                using var request = await AudioMedia.PrepareImportAsync(f.Document, normalized, "Mono lead",
                    "Authored verification signal", f.Directory.File("owned"), TestContext.Current.CancellationToken);
                f.Id = request.Accept(default, 0);
                return f;
            }
            catch { f.Dispose(); throw; }
        }
        internal void Set(AudioClip clip) => Document.Edit("Clip intent", e => e.UpdateAudioClip(clip));
        public void Dispose() { if (!Document.IsClosed) Document.Close(); Directory.Dispose(); }
    }

    private static float[] Signal(int rate, int channels, int frames) => Enumerable.Range(0, frames)
        .SelectMany(i => Enumerable.Range(0, channels).Select(c =>
            (float)((.2 * Math.Sin(2 * Math.PI * 220 * i / rate) + .04 * Math.Sin(2 * Math.PI * 440 * i / rate)) *
                    (c == 0 ? 1 : -.7)))).ToArray();

    private static float[] Render(ProjectDocument document, int rate = 48000, int packet = 251, bool smooth = false)
    {
        using var plan = ArrangementPreparation.Prepare(document, rate, 2, 1024, TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution(smooth);
        var pcm = new float[checked((int)plan.EndFrame * 2)];
        while (execution.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(packet, plan.EndFrame - execution.Position);
            execution.Process(pcm.AsSpan((int)execution.Position * 2, frames * 2));
        }
        return pcm;
    }

    private static double Frequency(float[] pcm, int rate, int channels, int channel, double start, double end)
    {
        int first = (int)(start * rate), last = (int)(end * rate), crossings = 0;
        for (int i = first + 1; i < last; i++)
            if (pcm[(i - 1) * channels + channel] <= 0 && pcm[i * channels + channel] > 0) crossings++;
        return crossings / (end - start);
    }

    [Theory]
    [InlineData(44100, 1, 50)] [InlineData(44100, 2, 150)] [InlineData(44100, 2, 200)]
    [InlineData(48000, 1, 200)] [InlineData(48000, 2, 50)] [InlineData(48000, 2, 125)]
    public async Task DurationChangesButFundamentalAndStereoRelationRemain(int rate, int channels, int percent)
    {
        using var f = await Fixture.Create(rate, channels);
        byte[] original = File.ReadAllBytes(f.Directory.File("phrase.wav"));
        f.Set(f.Clip with { StretchRatio = percent / 100m });
        var pcm = Render(f.Document);
        double duration = 2d * percent / 100;
        Assert.Equal((int)(48000 * duration) * 2, pcm.Length);
        Assert.InRange(Frequency(pcm, 48000, 2, 0, .1, duration - .1), 218, 222);
        Assert.All(pcm, sample => Assert.True(float.IsFinite(sample)));
        Assert.InRange(Math.Sqrt(pcm.Select(x => (double)x * x).Average()), .09, .17);
        for (int i = 0; i < pcm.Length; i += 2)
            Assert.InRange(pcm[i + 1] - pcm[i] * (channels == 1 ? 1 : -.7), -1e-6, 1e-6);
        Assert.Equal(original, File.ReadAllBytes(f.Directory.File("phrase.wav")));
        Assert.Equal(pcm, Render(f.Document, packet: 61));
    }

    [Theory]
    [InlineData(44100, "0.73137", 48000)] [InlineData(48000, "1.33337", 44100)]
    [InlineData(44100, "2", 44100)] [InlineData(48000, "0.5", 48000)]
    public async Task SplitCrossingFadesPreservesExactPcmPhaseAndDerivedSource(int sourceRate, string ratio, int rate)
    {
        using var f = await Fixture.Create(sourceRate);
        f.Set(f.Clip with { Position = new(12345), OffsetFrames = 93, LengthFrames = sourceRate + 317,
            FadeInFrames = sourceRate / 2, FadeOutFrames = sourceRate / 2,
            StretchRatio = decimal.Parse(ratio, CultureInfo.InvariantCulture) });
        var before = Render(f.Document, rate, smooth: true);
        long cutTicks = f.Clip.Position.Ticks + ConstantTempoConversion.FromFrames(
            AudioClipSource.StretchedFrames(sourceRate / 3 + 7, f.Clip.StretchRatio),
            f.Document.Current.State.Settings.Tempo, sourceRate).Ticks;
        f.Document.Edit("Split", e => e.SplitAudioClip(f.Id, new(cutTicks)));
        Assert.Equal(before, Render(f.Document, rate, smooth: true));
        using var plan = ArrangementPreparation.Prepare(f.Document, rate, 2, 1024, TestContext.Current.CancellationToken);
        Assert.Equal(2, plan.LeaseSources().Length); // Original plus one shared derived envelope, not per piece.
        Assert.True(f.Document.Undo()); Assert.Equal(before, Render(f.Document, rate, smooth: true));
        Assert.True(f.Document.Redo()); Assert.Equal(before, Render(f.Document, rate, smooth: true));
    }

    [Fact]
    public async Task TrimMoveRepeatAndBpmPreserveExplicitRatioCopiesAndAbsoluteBoundaryRounding()
    {
        using var f = await Fixture.Create(44100);
        Id<AudioClip> copy = default;
        f.Document.Edit("Independent copy", e => copy = e.CopyAudioClip(f.Id, new(9600000), 1));
        f.Set(f.Clip with { StretchRatio = 1.33337m, OffsetFrames = 997, LengthFrames = 22051,
            Repeats = 13, FadeInFrames = 113, FadeOutFrames = 257 });
        Assert.Equal(1, f.Document.Current.State.AudioClips.Single(c => c.Id == copy).StretchRatio);
        f.Document.Edit("Remove copy", e => e.DeleteAudioClip(copy));
        var before = Render(f.Document);
        long native = (long)decimal.Round(22051 * 1.33337m, 0, MidpointRounding.AwayFromZero);
        long end = (long)decimal.Round(native * 13m * 48000 / 44100, 0, MidpointRounding.AwayFromZero);
        Assert.Equal(end * 2, before.LongLength);
        using var plan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 1024, TestContext.Current.CancellationToken);
        Assert.Equal(13, plan.Events.Count(e => e.Kind == ExecutionEventKind.NoteOn));
        Assert.All(plan.Events.Where(e => e.Kind == ExecutionEventKind.NoteOn), e =>
            Assert.Equal((long)decimal.Round(native * (decimal)e.Occurrence!.Value.Iteration * 48000 / 44100,
                0, MidpointRounding.AwayFromZero), e.Frame));
        f.Document.Edit("Tempo", e => e.SetSettings(f.Document.Current.State.Settings with { Tempo = new(137.123456m) }));
        Assert.Equal(before, Render(f.Document));
        Assert.Equal(1.33337m, f.Clip.StretchRatio);
    }

    [Fact]
    public async Task SaveAsOpenIsPortableAndReaderGatePreventsSilentPlaybackAtOriginalSpeed()
    {
        using var f = await Fixture.Create();
        f.Set(f.Clip with { StretchRatio = 2 });
        var expected = Render(f.Document);
        string first = f.Directory.File("first.seqvium"), second = f.Directory.File("relocated.seqvium");
        ProjectPersistence.Save(f.Document, first); ProjectPersistence.Save(f.Document, second);
        File.Delete(f.Directory.File("phrase.wav"));
        Directory.Delete(first + ".media", true); File.Delete(first);
        f.Document.Close();
        var opened = ProjectPersistence.Open(second);
        try
        {
            Assert.False(opened.IsDegraded); Assert.Equal(expected, Render(opened.Document));
            var json = GraphFixture.Json(opened.Document);
            Assert.Equal(6, json["minimumReaderMinor"]!.GetValue<int>());
            json["minimumReaderMinor"] = 5;
            Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        }
        finally { opened.Document.Close(); }
    }

    [Fact]
    public async Task OldAudioProjectDefaultsToUnityWithoutRaisingItsReaderGate()
    {
        using var f = await Fixture.Create();
        var json = GraphFixture.Json(f.Document);
        Assert.Null(json["state"]!["audioClips"]![0]!["stretchRatio"]);
        Assert.Equal(4, json["minimumReaderMinor"]!.GetValue<int>());
        var loaded = GraphFixture.Read(json).Document;
        Assert.Equal(1, loaded.Current.State.AudioClips.Single().StretchRatio); loaded.Close();
    }

    [Theory]
    [InlineData("0")] [InlineData("0.49999")] [InlineData("2.00001")]
    public async Task InvalidRatioRefusesAtomically(string value)
    {
        using var f = await Fixture.Create(); var before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Set(f.Clip with
            { StretchRatio = decimal.Parse(value, CultureInfo.InvariantCulture) }));
        Assert.Equal(before, f.Document.Current);
    }

    [Fact]
    public async Task BoundsAndCancellationFailBeforeDerivedAllocationOrPublication()
    {
        Assert.Throws<GraphPreparationException>(() => AudioTimeStretch.RequiredBytes(4799, 48000, 2, 2));
        Assert.Throws<GraphPreparationException>(() => AudioTimeStretch.RequiredBytes(48000L * 61, 48000, 2, 2));
        Assert.Throws<GraphPreparationException>(() => AudioTimeStretch.RequiredBytes(192000L * 60, 192000, 2, 2));
        using var f = await Fixture.Create(); f.Set(f.Clip with { StretchRatio = 2 });
        using var sampler = new RealtimeSampler(48000, 2, 1024);
        using var cancelled = sampler.BeginArrangementPreparation(f.Document);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.PrepareAsync(cancellation.Token));
        using var stale = sampler.BeginArrangementPreparation(f.Document);
        await stale.PrepareAsync(TestContext.Current.CancellationToken);
        f.Set(f.Clip with { StretchRatio = .5m });
        Assert.NotEqual(SamplerPublication.Accepted, stale.Publish(TestContext.Current.CancellationToken));
        using var late = sampler.BeginArrangementPreparation(f.Document);
        await late.PrepareAsync(TestContext.Current.CancellationToken); f.Document.Close();
        Assert.NotEqual(SamplerPublication.Accepted, late.Publish(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public async Task RealtimeEqualsOfflineThroughMixerAndLiveStretchKeepsClockWithoutCallbackAllocations(int rate)
    {
        using var f = await Fixture.Create(44100);
        f.Document.Edit("Mixer", e =>
        {
            e.InitializeMixer(); var bus = e.AddMixerBus("Lead"); e.AssignMixerSound(f.Clip.SoundId, bus);
            e.SetMixerStrip(bus, new() { LevelDb = -6, Pan = .25m });
        });
        f.Set(f.Clip with { StretchRatio = 2, FadeInFrames = 300, FadeOutFrames = 500 });
        using var sampler = new RealtimeSampler(rate, 2, 1024, smoothBoundaries: true);
        using (var request = sampler.BeginArrangementPreparation(f.Document))
        { await request.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); }
        sampler.Start();
        var offline = Render(f.Document, rate, smooth: true);
        var output = new float[offline.Length];
        for (int i = 0; i < output.Length; i += 512) sampler.Process(output.AsSpan(i, Math.Min(512, output.Length - i)));
        Assert.Equal(offline, output);
        sampler.Stop(); sampler.Process(new float[512]); sampler.Start();
        var packet = new float[512]; sampler.Process(packet);
        f.Set(f.Clip with { StretchRatio = .5m });
        using (var next = sampler.BeginArrangementPreparation(f.Document, preservePlayback: true))
        { await next.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, next.Publish(TestContext.Current.CancellationToken)); }
        sampler.Process(packet); Assert.Equal(512, sampler.ReadStatus().Position);
        var changed = Render(f.Document, rate, smooth: true);
        Assert.Equal(changed[608..1024], packet[96..]);
        sampler.SetSongLoop(1000, 1512, true); sampler.SeekSong(1000);
        sampler.Process(packet); sampler.Process(packet); sampler.Process(packet);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++) sampler.Process(packet);
        Assert.Equal(allocated, GC.GetAllocatedBytesForCurrentThread());
    }

    [Theory]
    [InlineData(44100, 50)] [InlineData(48000, 200)]
    public void MusicalPhraseRetainsNotesAndEndpointsWithoutSilenceOrResamplingPitchShift(int rate, int percent)
    {
        int frames = rate * 2;
        double[] pitches = [220, 261.625565, 329.627557, 440];
        var input = new float[frames * 2];
        double phase = 0;
        for (int i = 0; i < frames; i++)
        {
            phase += Math.Tau * pitches[i * 4 / frames] / rate;
            input[i * 2] = (float)(.2 * Math.Sin(phase) + .03 * Math.Sin(phase * 2));
            input[i * 2 + 1] = input[i * 2] * -.7f;
        }
        using var source = new PcmLease(rate, 2, input);
        using var stretched = AudioTimeStretch.Prepare(source, 0, frames, percent / 100m,
            TestContext.Current.CancellationToken);
        double duration = 2d * percent / 100;
        for (int note = 0; note < 4; note++)
            Assert.InRange(Frequency(stretched.Samples, rate, 2, 0, duration * (note + .2) / 4,
                duration * (note + .8) / 4), pitches[note] - 8, pitches[note] + 8);
        int hopSamples = rate * 20 / 1000 * 2;
        Assert.Equal(input[..hopSamples], stretched.Samples[..hopSamples]);
        Assert.Equal(input[^hopSamples..], stretched.Samples[^hopSamples..]);
    }

    [Fact]
    public async Task ProductionSongWaveExportHasTheSameDurationAndQuantizedPcmAsRealtime()
    {
        using var f = await Fixture.Create(); f.Set(f.Clip with { StretchRatio = 2 });
        string wave = f.Directory.File("export.wav");
        var result = await PatternWaveExport.ExportArrangementAsync(f.Document, wave, TestContext.Current.CancellationToken);
        Assert.Equal(192000, result.Frames); Assert.Equal(0, result.ClippedSamples);
        using var decoded = WavDecoder.Decode(File.ReadAllBytes(wave));
        var pcm = Render(f.Document, smooth: true);
        for (int i = 0; i < pcm.Length; i++)
            Assert.InRange(decoded.Sample(i / 2, i % 2) - pcm[i], -1d / 32768, 1d / 32768);
        Assert.Equal(0, pcm[^1]); Assert.Equal(0, pcm[^2]);
    }

    [Fact]
    public async Task LiveRangeEditReplacesChangedDerivedPcmWhileUnrelatedPatternContinues()
    {
        using var f = await Fixture.Create();
        using (var import = ProjectMedia.BeginImport(f.Document, "Unrelated sustained note",
                   ownedMediaDirectory: f.Directory.File("pattern-media")))
        {
            using var bytes = new MemoryStream(WavFixtures.Create(Signal(48000, 2, 48000), 48000, 2));
            await import.PrepareAsync(bytes, TestContext.Current.CancellationToken);
            import.Accept((edit, media) =>
            {
                var pattern = edit.AddPattern("Held phrase", new(3840000));
                var part = edit.AddPart(pattern, "Reference", media.SoundId);
                edit.AddNote(pattern, part, default, new(1920000), 60, .5m);
                var placement = edit.AddPlacement(pattern, default);
                var attachment = edit.CreateItemGraph(placement);
                var source = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(180, 0));
                edit.BindGraphSource(attachment.Id, source.Id, placement, part);
                GraphFixture.Connect(edit, attachment.GraphId, source, output);
                edit.SetGraphOutput(attachment.Id, output.Id);
                edit.AddArrangementClip(placement, default, 1);
            }, TestContext.Current.CancellationToken);
        }
        f.Set(f.Clip with { LengthFrames = 48000, StretchRatio = 1.5m });
        using var sampler = new RealtimeSampler(48000, 2, 1024);
        using (var first = sampler.BeginArrangementPreparation(f.Document))
        { await first.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, first.Publish(TestContext.Current.CancellationToken)); }
        sampler.Start(); var packet = new float[512];
        for (int i = 0; i < 8; i++) sampler.Process(packet);
        long position = sampler.ReadStatus().Position;
        f.Set(f.Clip with { OffsetFrames = 509 }); // Same output length/index/step, different underlying waveform.
        using (var next = sampler.BeginArrangementPreparation(f.Document, preservePlayback: true))
        { await next.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, next.Publish(TestContext.Current.CancellationToken)); }
        var expected = Render(f.Document);
        sampler.Process(packet);
        Assert.Equal(position + 256, sampler.ReadStatus().Position);
        Assert.Equal(expected[((int)position * 2 + 96)..((int)position * 2 + 512)], packet[96..]);
        Assert.Equal(2, sampler.ReadStatus().Voices);
    }
}
