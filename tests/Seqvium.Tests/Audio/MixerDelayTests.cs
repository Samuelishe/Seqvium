// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class MixerDelayTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal Id<AudioClip> Clip;
        internal Id<MixerChannel> Channel;
        internal MixerState Mixer => Document.Current.State.Mixer!;
        internal static async Task<Fixture> Create(int channels = 2, int rate = 48000, int frames = 512)
        {
            var f = new Fixture();
            try
            {
                var samples = new float[frames * channels];
                samples[0] = .5f;
                if (channels == 2) samples[1] = -.25f;
                string source = f.Directory.File("impulse.wav"), normalized = f.Directory.File("normalized.wav");
                File.WriteAllBytes(source, WavFixtures.Create(samples, rate, channels));
                AudioFileDecode.Normalize(source, normalized, TestContext.Current.CancellationToken);
                using var import = await AudioMedia.PrepareImportAsync(f.Document, normalized, "Impulse",
                    "Authored numerical fixture", f.Directory.File("owned"), TestContext.Current.CancellationToken);
                f.Clip = import.Accept(new(0), 0);
                f.Document.Edit("Mixer", e => { e.InitializeMixer(); f.Channel = e.AddMixerBus("Signal");
                    e.AssignMixerSound(f.Document.Current.State.AudioClips.Single().SoundId, f.Channel); });
                return f;
            }
            catch { f.Dispose(); throw; }
        }
        internal async Task<GraphAttachment> Pattern(long gate = 4800)
        {
            GraphAttachment attachment = null!;
            using var import = ProjectMedia.BeginImport(Document, "Pulse", releaseMilliseconds: 0, ownedMediaDirectory: Directory.File("pattern-media"));
            var impulse = new float[1024]; impulse[0] = .5f;
            using var bytes = new MemoryStream(WavFixtures.Create(impulse, 48000, 1));
            await import.PrepareAsync(bytes, TestContext.Current.CancellationToken);
            import.Accept((e, media) =>
            {
                var pattern = e.AddPattern("Short pulse", new(9600));
                var part = e.AddPart(pattern, "Pulse", media.SoundId);
                e.AddNote(pattern, part, new(0), new(gate), 60, 1);
                var placement = e.AddPlacement(pattern, new(0));
                attachment = e.CreateItemGraph(placement);
                var source = e.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                var output = e.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(100, 0));
                e.BindGraphSource(attachment.Id, source.Id, placement, part);
                GraphFixture.Connect(e, attachment.GraphId, source, output);
                e.SetGraphOutput(attachment.Id, output.Id);
                e.AssignMixerSound(media.SoundId, Channel);
            });
            return attachment;
        }
        internal void Delay(decimal time = 10, decimal feedback = .5m, decimal mix = .5m, bool enabled = true,
            Id<MixerChannel>? target = null, bool master = false) => Document.Edit("Delay", e =>
            e.SetMixerStrip(master ? null : target ?? Channel, new() { Delay = new()
                { Enabled = enabled, TimeMs = time, Feedback = feedback, Mix = mix } }));
        public void Dispose() { if (!Document.IsClosed) Document.Close(); Directory.Dispose(); }
    }

    private static float[] Render(ProjectDocument doc, int rate = 48000, int channels = 2, int packet = 127,
        bool smooth = false)
    {
        using var plan = ArrangementPreparation.Prepare(doc, rate, channels, 1024);
        using var run = plan.CreateExecution(smooth);
        var data = new float[plan.EndFrame * channels];
        while (run.Position < plan.EndFrame)
        {
            int count = (int)Math.Min(packet, plan.EndFrame - run.Position);
            run.Process(data.AsSpan((int)run.Position * channels, count * channels));
        }
        return data;
    }

    [Theory]
    [InlineData(48000, 2, .5, .5)]
    [InlineData(44100, 1, .75, 1)]
    [InlineData(48000, 1, 0, 1)]
    [InlineData(44100, 2, .35, 0)]
    public async Task RealMixerImpulseMatchesIndependentRepeatDecayAndDryWetOracle(int rate, int channels,
        double feedback, double mix)
    {
        using var f = await Fixture.Create(channels, rate);
        f.Delay(feedback: (decimal)feedback, mix: (decimal)mix);
        var actual = Render(f.Document, rate, channels);
        int delay = rate / 100;
        for (int frame = 0; frame < actual.Length / channels; frame++)
            for (int c = 0; c < channels; c++)
            {
                int repeat = frame / delay;
                double source = c == 0 ? .5 : -.25;
                double expected = frame == 0 ? source * (1 - mix) : frame % delay == 0 && repeat <= 5
                    ? source * mix * Math.Pow(feedback, repeat - 1) : 0;
                if (frame <= delay * 5)
                    Assert.InRange(Math.Abs(actual[frame * channels + c] - expected), 0, 1e-7);
            }
        Assert.All(actual.TakeLast(channels * 100), sample => Assert.Equal(0, sample));
        Assert.Equal(actual, Render(f.Document, rate, channels, 37));
    }

    [Theory]
    [InlineData("channel")]
    [InlineData("bus")]
    [InlineData("master")]
    public async Task ChannelBusAndMasterUseOneSummedLineAndPreserveSendMuteSolo(string location)
    {
        using var f = await Fixture.Create();
        Id<MixerChannel> bus = default;
        if (location == "bus")
        {
            f.Document.Edit("Bus", e => bus = e.AddMixerBus("Return"));
            f.Document.Edit("Route", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select(c =>
                c.Id == f.Channel ? c with { Output = bus } : c)] }));
        }
        f.Delay(target: location == "bus" ? bus : null, master: location == "master");
        var single = Render(f.Document);
        var clip = f.Document.Current.State.AudioClips.Single();
        f.Document.Edit("Overlap", e => e.AddAudioClip(clip.SoundId, new(0), 1, 0, clip.LengthFrames));
        var doubled = Render(f.Document);
        Assert.InRange(doubled[960], .499999, .500001);
        Assert.Equal(single[960] * 2, doubled[960]);
        f.Document.Edit("Mute", e => e.SetMixerStrip(f.Channel,
            f.Mixer.Channels.Single(c => c.Id == f.Channel).Settings with { Muted = true }));
        Assert.All(Render(f.Document), v => Assert.Equal(0, v));
        f.Document.Undo();
        f.Document.Edit("Solo", e => e.SetMixerStrip(f.Channel,
            f.Mixer.Channels.Single(c => c.Id == f.Channel).Settings with { Solo = true }));
        Assert.Equal(doubled, Render(f.Document));
        if (location != "bus") return;
        f.Document.Edit("Pre send", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select(c =>
            c.Id == f.Channel ? c with { Output = null, Sends = [new(bus, 0, true)],
                Settings = c.Settings with { LevelDb = -90 } } : c)] }));
        Assert.Equal(doubled, Render(f.Document));
        f.Document.Edit("Post send", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select(c =>
            c.Id == f.Channel ? c with { Sends = [new(bus, 0, false)] } : c)] }));
        Assert.All(Render(f.Document), v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task FiniteSongExportContainsLastAudibleEchoAndMatchesRealtimeFloatAndPcm16()
    {
        using var f = await Fixture.Create(frames: 2048);
        f.Delay(time: 20, feedback: .75m, mix: .6m);
        var expected = Render(f.Document, smooth: true);
        using var rt = new RealtimeSampler(48000, 2, 1024, smoothBoundaries: true);
        using var request = rt.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); rt.Start();
        var actual = new float[expected.Length];
        for (int offset = 0; offset < actual.Length;)
        {
            int samples = Math.Min(254, actual.Length - offset);
            rt.Process(actual.AsSpan(offset, samples)); offset += samples;
        }
        Assert.Equal(expected, actual);
        Assert.Equal(SamplerTransportState.Ended, rt.ReadStatus().Transport);
        string path = f.Directory.File("echo.wav");
        var result = await PatternWaveExport.ExportArrangementAsync(f.Document, path, TestContext.Current.CancellationToken);
        using var pcm = WavDecoder.Decode(File.ReadAllBytes(path));
        Assert.Equal(expected.Length / 2, result.Frames);
        Assert.True(pcm.Frames > 2048);
        // Smooth source start is intentionally absent; the impulse is real in the production export.
        Assert.Contains(Enumerable.Range(2048, pcm.Frames - 2048), i => Math.Abs(pcm.Sample(i, 0)) > .001);
        for (int i = 0; i < expected.Length; i++)
            Assert.InRange(Math.Abs(expected[i] - pcm.Sample(i / 2, i % 2)), 0, 1d / 32768);
    }

    [Fact]
    public async Task StopSeekRangeLoopAndRestartClearHistoryAndWarmRenderingAllocatesNothing()
    {
        using var f = await Fixture.Create(frames: 4096); f.Delay();
        using var rt = new RealtimeSampler(48000, 2, 127);
        using var request = rt.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        rt.Start(); var packet = new float[254]; rt.Process(packet);
        rt.SeekSong(2000); rt.Process(packet); Assert.All(packet, v => Assert.Equal(0, v));
        rt.Stop(); rt.Process(packet); Assert.False(rt.ReadStatus().Playing); Assert.All(packet, v => Assert.Equal(0, v));
        rt.SeekSong(0); rt.Start(); rt.Process(packet); Assert.Equal(.25f, packet[0]);
        rt.SetSongLoop(2000, 2600, true); rt.SeekSong(2000);
        // Prime cold seek and repeated range-wrap entries before measuring steady consumer work.
        for (int i = 0; i < 20; i++) rt.Process(packet);
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 50; i++) rt.Process(packet);
        Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread()); Assert.All(packet, v => Assert.Equal(0, v));
        rt.Panic(); rt.Process(packet); Assert.False(rt.ReadStatus().Playing);
    }

    [Fact]
    public async Task LiveUpdatesRetainEchoTransportVoicesAndExtendNaturalEndWithZeroConsumerAllocations()
    {
        using var f = await Fixture.Create(frames: 4096);
        using var rt = new RealtimeSampler(48000, 2, 127);
        using var request = rt.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        rt.Start(); var packet = new float[254]; rt.Process(packet); var before = rt.ReadStatus();
        f.Delay(time: 100, mix: 1);
        Assert.True(rt.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId));
        rt.Process(packet);
        var after = rt.ReadStatus(); Assert.Equal(before.Epoch, after.Epoch); Assert.Equal(before.Voices, after.Voices);
        f.Delay(time: 20, feedback: .75m, mix: .7m);
        Assert.True(rt.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId));
        // Warm the active update path before observing the subsequent consumer packets.
        rt.Process(packet);
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) rt.Process(packet);
        Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread());
        Assert.True(rt.ReadStatus().Playing); Assert.True(rt.ReadStatus().Position > 4096);
        Assert.Equal(1, rt.PreparedStatesCreated);
        Assert.InRange(rt.RetainedPcmAndScratchBytes, 1, RealtimeSampler.MaximumRetainedPcmAndScratchBytes);
    }

    [Fact]
    public async Task HistoryPortablePersistenceReaderGateAndUnknownNestedDelayAreConservative()
    {
        using var f = await Fixture.Create(); var initial = f.Document.Current;
        var legacy = GraphFixture.Json(f.Document);
        Assert.Equal(4, legacy["minimumReaderMinor"]!.GetValue<int>());
        Assert.Null(legacy["state"]!["mixer"]!["channels"]![0]!["settings"]!["delay"]);
        Assert.Null(legacy["state"]!["mixer"]!["master"]!["delay"]);
        f.Delay(time: 125, feedback: .6m, mix: .8m); var edited = f.Document.Current;
        f.Document.Undo(); Assert.Equal(initial, f.Document.Current); f.Document.Redo(); Assert.Equal(edited, f.Document.Current);
        string path = f.Directory.File("portable.seqvium"); ProjectPersistence.Save(f.Document, path);
        var json = GraphFixture.Json(f.Document); Assert.Equal(7, json["minimumReaderMinor"]!.GetValue<int>());
        // Frozen pre-Delay reader envelope predicate refuses before deserialization/DSP.
        Assert.True(json["minimumReaderMinor"]!.GetValue<int>() > 6);
        var reopened = ProjectPersistence.Open(path).Document;
        try { Assert.Equal(Render(f.Document), Render(reopened)); }
        finally { reopened.Close(); }
        json["minimumReaderMinor"] = 6; Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        json["minimumReaderMinor"] = 7;
        json["state"]!["mixer"]!["channels"]![0]!["settings"]!["delay"]!["future"] = 1;
        var unknown = GraphFixture.Read(json).Document; unknown.MediaRoots.AddRange(f.Document.MediaRoots);
        try
        {
            Assert.Equal(1, GraphFixture.Json(unknown)["state"]!["mixer"]!["channels"]![0]!["settings"]!["delay"]!["future"]!.GetValue<int>());
            Assert.Throws<GraphPreparationException>(() => ArrangementPreparation.Prepare(unknown, 48000, 2, 127, TestContext.Current.CancellationToken));
            Assert.Throws<ProjectValidationException>(() => unknown.Edit("Overwrite", e => e.SetMixerStrip(f.Channel, new())));
        }
        finally { unknown.Close(); }
    }

    [Theory]
    [InlineData(9, .5, .5)] [InlineData(1001, .5, .5)]
    [InlineData(10, -.1, .5)] [InlineData(10, .751, .5)] [InlineData(10, .5, 1.01)]
    public async Task InvalidDelayIsRefusedAtomically(int time, double feedback, double mix)
    {
        using var f = await Fixture.Create(); var before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Delay(time, (decimal)feedback, (decimal)mix));
        Assert.Equal(before, f.Document.Current);
    }
    [Fact]
    public async Task PatternLoopCarriesOneLineAcrossCyclesWhileExplicitStopAndObjectTapStayHard()
    {
        using var f = await Fixture.Create(); var attachment = await f.Pattern(); f.Delay();
        using (var hard = GraphPreparation.Prepare(f.Document, attachment.Id, 48000, 2, 127,
                   stop: new(9600), cancellationToken: TestContext.Current.CancellationToken))
        {
            Assert.Equal(240, hard.EndFrame);
            using var run = hard.CreateExecution(); var data = new float[254]; run.Process(data);
            Assert.Equal(.25f, data[0]); run.Process(data.AsSpan(0, 226));
        }
        using var local = GraphPreparation.PrepareObject(f.Document, attachment.Id, 48000, 2, 1024,
            TestContext.Current.CancellationToken);
        Assert.Equal(240, local.EndFrame); Assert.Equal(0, local.MixerDelayBytes);
        using (var run = local.CreateExecution())
        { var data = new float[480]; run.Process(data); Assert.Equal(.5f, data[0]); }
        using var rt = new RealtimeSampler(48000, 2, 127);
        rt.SetLooping(true);
        using var request = rt.BeginGraphPreparation(f.Document, attachment.Id);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); rt.Start();
        var pcm = new float[1440];
        for (int at = 0; at < pcm.Length;)
        { int count = Math.Min(254, pcm.Length - at); rt.Process(pcm.AsSpan(at, count)); at += count; }
        Assert.Equal(.25f, pcm[0]); Assert.Equal(.25f, pcm[480]); Assert.Equal(.5f, pcm[960]);
        Assert.True(rt.ReadStatus().Playing); rt.Panic(); rt.Process(pcm.AsSpan(0, 254));
        Assert.All(pcm.Take(254), v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task TopologyContinuationTransfersActiveEchoAndKeepsBoundedReservation()
    {
        using var f = await Fixture.Create(frames: 4096); f.Delay();
        using var rt = new RealtimeSampler(48000, 2, 127);
        using (var request = rt.BeginArrangementPreparation(f.Document))
        {
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        rt.Start(); var packet = new float[254]; rt.Process(packet); var epoch = rt.ReadStatus().Epoch;
        f.Document.Edit("Add neighbour", e => e.AddMixerBus("Neighbour"));
        using (var request = rt.BeginArrangementPreparation(f.Document, preservePlayback: true))
        {
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        // Callback ownership transfer and tail output, including the delayed impulse at frame 480.
        rt.Process(packet); rt.Process(packet); rt.Process(packet);
        Assert.Equal(.25f, packet[(480 - 381) * 2]); Assert.Equal(epoch, rt.ReadStatus().Epoch);
        rt.RetireCompleted();
        Assert.InRange(rt.RetainedPcmAndScratchBytes, 1, RealtimeSampler.MaximumRetainedPcmAndScratchBytes);
    }

    [Fact]
    public async Task MaximumChannelStorageAndSerialTailWorkAreExplicitlyBounded()
    {
        using var f = await Fixture.Create();
        f.Document.Edit("Full bank", e => { for (int i = 1; i < 64; i++) e.AddMixerBus("Bus " + i); });
        using (var plan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 127, TestContext.Current.CancellationToken))
        {
            Assert.Equal(65 * 48001 * 16, plan.MixerDelayBytes);
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            using var run = plan.CreateExecution();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Assert.InRange(allocated, plan.MixerDelayBytes, plan.MixerDelayBytes + plan.ScratchBytes + 65536L);
        }
        f.Document.Edit("Serial long tails", e => e.SetMixer(f.Mixer with { Channels =
            [.. f.Mixer.Channels.Select((c, i) => c with { Output = i < 63 ? f.Mixer.Channels[i + 1].Id : null,
                Settings = c.Settings with { Delay = new() { Enabled = true, TimeMs = 1000, Feedback = .75m } } })] }));
        var error = Assert.Throws<GraphPreparationException>(() => ArrangementPreparation.Prepare(f.Document,
            48000, 2, 127, TestContext.Current.CancellationToken));
        Assert.Equal(GraphPreparationReasons.Budget, error.Reason);
    }

    [Fact]
    public async Task LiveMixFeedbackAndBypassPreserveThenRetireActiveEchoWithoutRetrigger()
    {
        using var f = await Fixture.Create(frames: 4096); f.Delay();
        using var rt = new RealtimeSampler(48000, 2, 127);
        using var request = rt.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        rt.Start(); var packet = new float[254]; rt.Process(packet);
        long id = rt.ReadStatus().PreparedExecutionId, epoch = rt.ReadStatus().Epoch;
        Assert.True(rt.PublishMixerUpdate(f.Document.Current, id)); rt.Process(packet); // warm publication entry
        f.Delay(mix: .75m);
        Assert.True(rt.PublishMixerUpdate(f.Document.Current, id));
        long bytes = GC.GetAllocatedBytesForCurrentThread(); rt.Process(packet);
        Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread());
        rt.Process(packet);
        double mixAtEcho = .75 - .25 * Math.Exp(-(480 - 254 + 1d) / 240);
        Assert.InRange(Math.Abs(packet[(480 - 381) * 2] - .5 * mixAtEcho), 0, 1e-7);
        f.Delay(feedback: 0, mix: 1, enabled: false);
        Assert.True(rt.PublishMixerUpdate(f.Document.Current, id));
        for (int i = 0; i < 50; i++) rt.Process(packet);
        Assert.All(packet, v => Assert.Equal(0, v));
        f.Delay(mix: 1); Assert.True(rt.PublishMixerUpdate(f.Document.Current, id));
        for (int i = 0; i < 10; i++) { rt.Process(packet); Assert.All(packet, v => Assert.Equal(0, v)); }
        Assert.Equal(epoch, rt.ReadStatus().Epoch); Assert.Equal(1, rt.PreparedStatesCreated);
    }

    [Fact]
    public async Task BypassedBankRetainsThreeLineReservationsUnderPublicationBackpressure()
    {
        using var f = await Fixture.Create();
        f.Document.Edit("Full bypassed bank", e => { for (int i = 1; i < 64; i++) e.AddMixerBus("Bus " + i); });
        using var sampler = new RealtimeSampler(48000, 2, 127);
        for (int i = 0; i < 3; i++)
        {
            using var request = sampler.BeginArrangementPreparation(f.Document, preservePlayback: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            int payload = request.Plan!.MixerDelayBytes;
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Assert.InRange(allocated, payload, payload + 1024 * 1024L);
            if (i < 2) sampler.Process(new float[254]);
        }
        Assert.Equal(3, sampler.LivePreparedStates);
        Assert.True(sampler.HasPending);
        Assert.InRange(sampler.RetainedPcmAndScratchBytes, 3L * 65 * 48001 * 16,
            RealtimeSampler.MaximumRetainedPcmAndScratchBytes);
        using (var candidate = sampler.BeginArrangementPreparation(f.Document))
        {
            await candidate.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Capacity, candidate.Publish(TestContext.Current.CancellationToken));
            Assert.Equal(3, sampler.LivePreparedStates);
            Assert.InRange(sampler.ReservedPcmAndScratchBytes, sampler.RetainedPcmAndScratchBytes,
                RealtimeSampler.MaximumRetainedPcmAndScratchBytes);
        }
        sampler.Dispose();
        Assert.Equal(0, sampler.LivePreparedStates);
    }

    [Fact]
    public async Task SequentialTopologyPublicationsKeepOneEchoHistoryAfterEachRetirement()
    {
        using var f = await Fixture.Create(frames: 4096); f.Delay(time: 20);
        var expected = Render(f.Document);
        using var sampler = new RealtimeSampler(48000, 2, 127);
        async Task Publish()
        {
            using var request = sampler.BeginArrangementPreparation(f.Document, preservePlayback: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); sampler.Start();
        var actual = new float[expected.Length];
        sampler.Process(actual.AsSpan(0, 254));
        long epoch = sampler.ReadStatus().Epoch;
        for (int i = 1; i <= 6; i++)
        {
            f.Document.Edit("Change bank", e =>
            {
                if (i % 2 == 1) e.AddMixerBus("Empty neighbour");
                else e.SetMixer(f.Mixer with { Channels = [f.Mixer.Channels[0]] });
            });
            await Publish();
            sampler.Process(actual.AsSpan(i * 254, 254));
            sampler.RetireCompleted();
            Assert.Equal(1, sampler.LivePreparedStates);
            Assert.Equal(epoch, sampler.ReadStatus().Epoch);
        }
        for (int offset = 7 * 254; offset < actual.Length;)
        {
            int count = Math.Min(254, actual.Length - offset);
            sampler.Process(actual.AsSpan(offset, count)); offset += count;
        }
        Assert.Equal(expected, actual);
        Assert.Equal(SamplerTransportState.Ended, sampler.ReadStatus().Transport);
        sampler.Dispose();
        Assert.Equal(sampler.PreparedStatesCreated, sampler.PreparedStatesReleased);
        Assert.Equal(0, sampler.RetainedPcmAndScratchBytes);
    }

    [Theory]
    [InlineData("unused")] [InlineData("muted")] [InlineData("dry")]
    public async Task InaudibleDelayDoesNotAppendSilentExportTail(string route)
    {
        using var f = await Fixture.Create();
        var dry = Render(f.Document);
        Id<MixerChannel> target = f.Channel;
        if (route != "dry")
            f.Document.Edit("Unused return", e => target = e.AddMixerBus("Unused"));
        f.Delay(time: 1000, feedback: .75m, mix: route == "dry" ? 0 : .5m, target: target);
        if (route == "muted") f.Document.Edit("Mute return", e => e.SetMixerStrip(target,
            f.Mixer.Channels.Single(c => c.Id == target).Settings with { Muted = true }));
        var extended = Render(f.Document);
        Assert.Equal(dry.Length, extended.Length);
        Assert.Equal(dry, extended.Take(dry.Length));
        Assert.All(extended.Skip(dry.Length), v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task LiveTailBudgetRefusalKeepsAcceptedHistoryLastValidPlaybackAndUndoRepair()
    {
        using var f = await Fixture.Create();
        var attachment = await f.Pattern();
        f.Document.Edit("Serial bank", e =>
        {
            for (int i = 0; i < 3; i++) e.AddMixerBus("Return " + i);
        });
        f.Document.Edit("Three long delays", e => e.SetMixer(f.Mixer with { Channels =
            [.. f.Mixer.Channels.Select((c, i) => c with
            {
                Output = i < 3 ? f.Mixer.Channels[i + 1].Id : null,
                Settings = c.Settings with { Delay = new() { Enabled = i < 3, TimeMs = 1000, Feedback = .75m } }
            })] }));
        using var sampler = new RealtimeSampler(48000, 2, 127);
        sampler.SetLooping(true);
        var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(f.Document, sampler, attachment.Id, owner, 8, 1, null, false));
        try
        {
            await owner.Until(() => sampler.HasPending);
            owner.Run(() => Assert.True(coordinator.Start()));
            var packet = new float[254]; sampler.Process(packet);
            var before = sampler.ReadStatus();
            int history = f.Document.UndoCount;
            owner.Run(() => f.Document.Edit("Fourth long delay", e => e.SetMixerStrip(f.Mixer.Channels[3].Id,
                f.Mixer.Channels[3].Settings with { Delay = new() { Enabled = true, TimeMs = 1000, Feedback = .75m } })));
            Assert.Equal(history + 1, f.Document.UndoCount);
            Assert.Equal(GraphPreparationReasons.Budget, coordinator.ReadStatus().Blocker);
            sampler.Process(packet);
            Assert.True(coordinator.ReadStatus().LastValidPlaying);
            Assert.Equal(before.ExecutingRevision, sampler.ReadStatus().ExecutingRevision);
            owner.Run(() => Assert.True(f.Document.Undo()));
            sampler.Process(packet);
            Assert.False(coordinator.ReadStatus().InvalidCanonical);
            Assert.Equal(f.Document.Current.Revision, sampler.ReadStatus().ExecutingRevision);
            Assert.Equal(before.Epoch, sampler.ReadStatus().Epoch);
            Assert.Equal(1, sampler.PreparedStatesCreated);
        }
        finally { await owner.Join(coordinator.DisposeAsync); }
    }

    [Fact]
    public async Task EnablingDelayBeforeFinitePatternEndDoesNotExtendTheLastNoteGate()
    {
        using var f = await Fixture.Create(); var attachment = await f.Pattern(gate: 9600);
        using var rt = new RealtimeSampler(48000, 2, 127);
        using var request = rt.BeginGraphPreparation(f.Document, attachment.Id);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        rt.Start(); var packet = new float[254]; rt.Process(packet);
        Assert.Equal(1, rt.ReadStatus().Voices);
        f.Delay(); Assert.True(rt.PublishMixerUpdate(f.Document.Current, rt.ReadStatus().PreparedExecutionId));
        rt.Process(packet);
        Assert.Equal(254, rt.ReadStatus().Position); Assert.True(rt.ReadStatus().Playing);
        Assert.Equal(0, rt.ReadStatus().Voices);
    }

}
