// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class AudioClipTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal float[] Samples = [];
        internal Id<AudioClip> Clip;
        internal AudioClip Current => Document.Current.State.AudioClips.Single(c => c.Id == Clip);

        internal static async Task<Fixture> Create(int rate = 48000, int channels = 2, int frames = 1024)
        {
            var f = new Fixture();
            try
            {
                f.Samples = Enumerable.Range(0, frames).SelectMany(i => Enumerable.Range(0, channels)
                    .Select(c => (float)(Math.Sin(i * .03) * (c == 0 ? .3 : -.2)))).ToArray();
                string source = f.Directory.File("source.wav"), normalized = f.Directory.File("normalized.wav");
                File.WriteAllBytes(source, WavFixtures.Create(f.Samples, rate, channels));
                AudioFileDecode.Normalize(source, normalized, TestContext.Current.CancellationToken);
                using var import = await AudioMedia.PrepareImportAsync(f.Document, normalized, "Recording",
                    "Authored fixture",
                    f.Directory.File("owned"), TestContext.Current.CancellationToken);
                f.Clip = import.Accept(new(0), 4);
                return f;
            }
            catch
            {
                f.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (!Document.IsClosed) Document.Close();
            Directory.Dispose();
        }
    }

    private static float[] Render(ProjectDocument document, int rate, int packet = 127, bool smooth = false)
    {
        using var plan = ArrangementPreparation.Prepare(document, rate, 2, 1024);
        using var execution = plan.CreateExecution(smoothBoundaries: smooth);
        var output = new float[plan.EndFrame * 2];
        while (execution.Position < plan.EndFrame)
        {
            int count = (int)Math.Min(packet, plan.EndFrame - execution.Position);
            execution.Process(output.AsSpan((int)execution.Position * 2, count * 2));
        }

        return output;
    }

    [Theory]
    [InlineData(48000, 48000, 2)]
    [InlineData(44100, 48000, 1)]
    [InlineData(8000, 44100, 2)]
    public async Task SourceRangeFadesAndFixedSpeedRepeatsMatchIndependentOracle(int sourceRate, int rate, int channels)
    {
        using var f = await Fixture.Create(sourceRate, channels);
        f.Document.Edit("Range", e => e.UpdateAudioClip(f.Current with
        {
            OffsetFrames = 100, LengthFrames = 600, FadeInFrames = 80, FadeOutFrames = 120, Repeats = 3,
            Position = new(2400), GainDb = -6
        }));
        var clip = f.Current;
        var actual = Render(f.Document, rate);
        Assert.Equal(actual, Render(f.Document, rate, 37));
        long start = (long)Math.Round(clip.Position.Ticks * 60d * rate / 960000 /
                                      (double)f.Document.Current.State.Settings.Tempo.BeatsPerMinute);
        for (int frame = 0; frame < actual.Length / 2; frame++)
        {
            double age = -1;
            for (int r = 0; r < 3; r++)
            {
                long on = start + (long)Math.Round(600d * r * rate / sourceRate, MidpointRounding.AwayFromZero);
                long off = start + (long)Math.Round(600d * (r + 1) * rate / sourceRate, MidpointRounding.AwayFromZero);
                if (frame >= on && frame < off) age = (frame - on) * (double)sourceRate / rate;
            }

            for (int c = 0; c < 2; c++)
            {
                double expected = 0;
                if (age is >= 0 and < 600)
                {
                    double cursor = 100 + age;
                    int a = (int)cursor, b = Math.Min(a + 1, 1023), sourceChannel = channels == 1 ? 0 : c;
                    double sample = f.Samples[a * channels + sourceChannel] * (1 - cursor + a) +
                                    f.Samples[b * channels + sourceChannel] * (cursor - a);
                    expected = sample * (float)Math.Pow(10, -.3) * Math.Min(1, age / 80) *
                               Math.Clamp((600 - age - (double)sourceRate / rate) / 120, 0, 1);
                }

                Assert.InRange(actual[frame * 2 + c], expected - 1e-6, expected + 1e-6);
            }
        }

        Assert.Empty(f.Document.Current.State.Patterns);
        Assert.Empty(f.Document.Current.State.Placements);
    }

    [Fact]
    public async Task SplitMoveCopyTrimAndHistoryPreserveSourceAndRoundTripThroughPortableSave()
    {
        using var f = await Fixture.Create();
        var original = Render(f.Document, 48000);
        Id<AudioClip> right = default;
        f.Document.Edit("Split", e => right = e.SplitAudioClip(f.Clip, new(10240)));
        Assert.Equal(original, Render(f.Document, 48000));
        Assert.Equal(2, f.Document.Current.State.AudioClips.Length);
        Assert.Single(f.Document.Current.State.Resources);
        Assert.Single(f.Document.Current.State.Sounds);
        f.Document.Edit("Copy", e => e.CopyAudioClip(right, new(960000), 8));
        Assert.True(f.Document.Undo());
        Assert.Equal(2, f.Document.Current.State.AudioClips.Length);
        Assert.True(f.Document.Redo());
        Assert.Equal(3, f.Document.Current.State.AudioClips.Length);
        string path = f.Directory.File("portable.seqvium");
        ProjectPersistence.Save(f.Document, path);
        Assert.False(f.Document.IsDirty);
        File.Delete(f.Directory.File("source.wav"));
        File.Delete(f.Directory.File("normalized.wav"));
        var reopened = ProjectPersistence.Open(path);
        Assert.False(reopened.IsDegraded);
        Assert.Equal(Render(f.Document, 48000), Render(reopened.Document, 48000));
        var json = GraphFixture.Json(reopened.Document);
        Assert.Equal(4, json["minimumReaderMinor"]!.GetValue<int>());
        json["minimumReaderMinor"] = 3;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        reopened.Document.Close();
    }

    [Theory]
    [InlineData(-1, 100, 0, 0)]
    [InlineData(1000, 100, 0, 0)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, 100, 101, 0)]
    [InlineData(0, 100, 0, -1)]
    public async Task InvalidRangesAreAtomic(long offset, long length, long fadeIn, long fadeOut)
    {
        using var f = await Fixture.Create();
        var snapshot = f.Document.Current;
        int history = f.Document.UndoCount;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Invalid", e => e.UpdateAudioClip(f.Current with
        {
            OffsetFrames = offset, LengthFrames = length, FadeInFrames = fadeIn, FadeOutFrames = fadeOut
        })));
        Assert.Equal(snapshot, f.Document.Current);
        Assert.Equal(history, f.Document.UndoCount);
    }

    [Fact]
    public async Task MixerProcessesSharedAudioSourceOnceAndPlaylistMuteDoesNotChangeRouting()
    {
        using var f = await Fixture.Create();
        Id<MixerChannel> channel = default;
        f.Document.Edit("Mix", e =>
        {
            e.InitializeMixer();
            channel = e.AddMixerBus("Recording");
            e.AssignMixerSound(f.Current.SoundId, channel);
            e.SetMixerStrip(channel, new() { LevelDb = -6, Pan = 1 });
            e.CopyAudioClip(f.Clip, default, 5);
        });
        var output = Render(f.Document, 48000);
        for (int i = 0; i < 1024; i++)
        {
            Assert.InRange(output[i * 2], -1e-7, 1e-7);
            Assert.InRange(output[i * 2 + 1], f.Samples[i * 2 + 1] * 2 * Math.Pow(10, -.3) - 1e-6,
                f.Samples[i * 2 + 1] * 2 * Math.Pow(10, -.3) + 1e-6);
        }

        f.Document.Edit("Lane gate", e => e.SetArrangementTrack(5, "Lane", 5, true, false));
        var gated = Render(f.Document, 48000);
        Assert.InRange(gated[101], output[101] / 2 - 1e-6, output[101] / 2 + 1e-6);
        Assert.Equal(channel,
            f.Document.Current.State.Mixer!.Channels.Single(c => c.SoundIds.Contains(f.Current.SoundId)).Id);
    }

    [Fact]
    public async Task RealtimeSeekLoopAndOfflineParityUseSourceOffsetWithoutCallbackAllocation()
    {
        using var f = await Fixture.Create(frames: 48000);
        f.Document.Edit("Trim", e => e.UpdateAudioClip(f.Current with { OffsetFrames = 1000, LengthFrames = 24000 }));
        var offline = Render(f.Document, 48000);
        using var sampler = new RealtimeSampler(48000, 2, 1024);
        using var request = sampler.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        sampler.Start();
        var packet = new float[512];
        sampler.Process(packet);
        Assert.Equal(offline[..512], packet);
        sampler.SeekSong(12000);
        sampler.Process(packet);
        // Explicit seek retains the existing 1 ms transport ramp, distinct from source fades.
        var seekOracle = offline[24000..24512];
        for (int frame = 0; frame < 48; frame++)
        {
            seekOracle[frame * 2] *= (frame + 1f) / 48;
            seekOracle[frame * 2 + 1] *= (frame + 1f) / 48;
        }

        Assert.Equal(seekOracle, packet);
        sampler.SetSongLoop(1000, 1512, true);
        sampler.SeekSong(1000);
        sampler.Process(packet);
        sampler.Process(packet);
        sampler.Process(packet);
        Assert.Equal(offline[2000..2512], packet);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++) sampler.Process(packet);
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public async Task WaveformKeepsStereoExtremaAndMissingOrDamagedManagedMediaBlocksRenderButRetainsIntent()
    {
        using var f = await Fixture.Create();
        var resource = f.Document.Current.State.Resources.Single();
        var wave = AudioMedia.Waveform(f.Document, resource.Id);
        Assert.Equal(2, wave.Channels);
        Assert.InRange(wave.Range(0, 1024, 0).Max, .299f, .301f);
        Assert.InRange(wave.Range(0, 1024, 1).Min, -.201f, -.199f);
        string project = f.Directory.File("song.seqvium");
        ProjectPersistence.Save(f.Document, project);
        string stored = Path.Combine(project + ".media",
            resource.ManagedLocator!.Replace('/', Path.DirectorySeparatorChar));
        File.Delete(stored);
        var missing = ProjectPersistence.Open(project);
        Assert.True(missing.IsDegraded);
        Assert.Single(missing.Document.Current.State.AudioClips);
        Assert.Throws<FileNotFoundException>(() =>
            ArrangementPreparation.Prepare(missing.Document, 48000, 2, 256, TestContext.Current.CancellationToken));
        string incomplete = f.Directory.File("missing-save.seqvium");
        Assert.True(ProjectPersistence.SaveWithReport(missing.Document, incomplete).IsDegraded);
        missing.Document.Close();
    }

    [Fact]
    public async Task LongWaveBeyondLegacy16MiBLimitSharesPcmAcrossCopiesAndPreparedRevisions()
    {
        using var f = await Fixture.Create(frames: 2_200_000);
        Assert.True(new FileInfo(f.Directory.File("source.wav")).Length > WavDecoder.MaximumSourceBytes);
        f.Document.Edit("Copies", e =>
        {
            for (int i = 1; i <= 24; i++) e.CopyAudioClip(f.Clip, new(i * 96000000L), i % 32);
        });
        using var plan =
            ArrangementPreparation.Prepare(f.Document, 48000, 2, 256, TestContext.Current.CancellationToken);
        Assert.Equal(2_200_000 * 2 * 4, plan.DecodedBytes);
        ProjectPersistence.Save(f.Document, f.Directory.File("saved-long.seqvium"));
        var frozen = ProjectDocument.Reopen(f.Document.Current);
        frozen.MediaRoots.AddRange(f.Document.MediaRoots);
        AudioMedia.ShareCache(f.Document, frozen);
        using var next = ArrangementPreparation.Prepare(frozen, 48000, 2, 256, TestContext.Current.CancellationToken);
        using var firstSource = plan.LeaseSources()[0];
        using var nextSource = next.LeaseSources()[0];
        Assert.Same(firstSource.Samples, nextSource.Samples);
        frozen.Close();
    }

    [Theory]
    [InlineData(44100, 48000)]
    [InlineData(192000, 44100)]
    public async Task BpmChangesLeaveNativeSpeedRangeFadesAndRepeatFrameCountUnchanged(int sourceRate, int outputRate)
    {
        using var f = await Fixture.Create(sourceRate);
        f.Document.Edit("Range",
            e => e.UpdateAudioClip(f.Current with
            {
                OffsetFrames = 91, LengthFrames = 700, Repeats = 5, FadeInFrames = 53, FadeOutFrames = 17
            }));
        var before = Render(f.Document, outputRate);
        var clip = f.Current;
        f.Document.Edit("BPM", e => e.SetSettings(f.Document.Current.State.Settings with { Tempo = new(137.123456m) }));
        Assert.Equal(clip, f.Current);
        Assert.Equal(before, Render(f.Document, outputRate));
        long frames = (long)decimal.Round(700m * 5 * outputRate / sourceRate, 0, MidpointRounding.AwayFromZero);
        Assert.Equal(frames * 2, before.LongLength);
        Assert.True(f.Document.Undo());
        Assert.Equal(before, Render(f.Document, outputRate));
    }

    [Fact]
    public async Task PreparedImportCancellationAndStaleGenerationCannotPublishAndDisposeRemovesOnlyCandidate()
    {
        using var f = await Fixture.Create();
        var snapshot = f.Document.Current;
        using var cancellation = new CancellationTokenSource();
        using (var request = await AudioMedia.PrepareImportAsync(f.Document, f.Directory.File("normalized.wav"),
                   "Withdrawn", "Authored fixture", f.Directory.File("owned"), cancellation.Token))
        {
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() =>
                request.Accept(default, 0, TestContext.Current.CancellationToken));
            Assert.Throws<OperationCanceledException>(() =>
                request.Accept(default, 0, TestContext.Current.CancellationToken));
            Assert.Equal(snapshot, f.Document.Current);
        }

        using (var stale = await AudioMedia.PrepareImportAsync(f.Document, f.Directory.File("normalized.wav"),
                   "Stale", "Authored fixture", f.Directory.File("owned"), TestContext.Current.CancellationToken))
        {
            f.Document.Edit("Intervening edit", e => e.RenameDocument("Changed"));
            Assert.Throws<InvalidOperationException>(() =>
                stale.Accept(default, 0, TestContext.Current.CancellationToken));
        }

        Assert.Single(Directory.EnumerateFiles(f.Document.UnnamedMediaRoot!, "*.wav", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(f.Document.UnnamedMediaRoot!, "*.pending", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(".ogg", "AudioClip.UnsupportedFormat")]
    [InlineData(".wav", "AudioClip.DecodeFailed")]
    public async Task CodecFailuresPreserveTheExistingDocumentAndReportUserFacingReason(string extension, string reason)
    {
        using var f = await Fixture.Create();
        string path = f.Directory.File("bad" + extension);
        File.WriteAllText(path, "Invalid authored source fixture");
        await using var workflow = new DesktopWorkflow(f.Document, mediaDirectory: f.Directory.File("owned"));
        await workflow.ObserveAsync();
        var snapshot = workflow.State.Snapshot;
        Assert.False(await workflow.ImportAudioClipAsync(ArrangementEditBasis.From(workflow.State), path, default, 0,
            TestContext.Current.CancellationToken));
        Assert.Equal(snapshot, workflow.State.Snapshot);
        Assert.Equal("Workflow.ImportFailed", workflow.State.StatusKey);
        Assert.Equal(reason, workflow.State.Detail);
    }

    [Fact]
    public async Task AudioEditDuringSongReconstructsDestinationSourceAgeInsteadOfGoingSilent()
    {
        using var f = await Fixture.Create(frames: 48000);
        using var sampler = new RealtimeSampler(48000, 2, 1024);
        using (var first = sampler.BeginArrangementPreparation(f.Document))
        {
            await first.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, first.Publish(TestContext.Current.CancellationToken));
        }

        sampler.Start();
        var packet = new float[512];
        sampler.Process(packet);
        f.Document.Edit("Live range/gain",
            e => e.UpdateAudioClip(f.Current with { OffsetFrames = 1000, LengthFrames = 24000, GainDb = -6 }));
        using (var next = sampler.BeginArrangementPreparation(f.Document, preservePlayback: true))
        {
            await next.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, next.Publish(TestContext.Current.CancellationToken));
        }

        sampler.Process(packet);
        var expected = Render(f.Document, 48000);
        Assert.Equal(expected[608..1024], packet[96..]); // Publication keeps the existing 1 ms transport ramp.
        Assert.True(sampler.ReadStatus().Playing);
        Assert.Equal(512, sampler.ReadStatus().Position);
    }

    [Theory]
    [InlineData(48000, false, 0)]
    [InlineData(48000, true, 0)]
    [InlineData(44100, false, 0)]
    [InlineData(44100, true, 0)]
    [InlineData(44100, true, 12345)]
    [InlineData(48000, true, 12345)]
    public async Task SplitPreservesResamplingPhaseAndCrossingFadesWithoutAnArtificialBoundaryRamp(int sourceRate,
        bool fades, long positionTicks)
    {
        using var f = await Fixture.Create(sourceRate);
        var placed = f.Current with { Position = new(positionTicks) };
        f.Document.Edit("Place", e => e.UpdateAudioClip(placed));
        if (fades)
            f.Document.Edit("Fades",
                e => e.UpdateAudioClip(f.Current with { FadeInFrames = 600, FadeOutFrames = 600 }));
        var before = Render(f.Document, 48000, smooth: true);
        var cut = new MusicalPosition(positionTicks + ConstantTempoConversion.FromFrames(257,
            f.Document.Current.State.Settings.Tempo, sourceRate).Ticks);
        f.Document.Edit("Split", e => e.SplitAudioClip(f.Clip, cut));
        Assert.Equal(before, Render(f.Document, 48000, smooth: true));
    }
}
