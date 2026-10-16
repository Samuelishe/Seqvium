// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class MixerReverbTests
{
    private static float[] Response(MixerReverb settings, int rate = 48000, int source = 0)
    {
        var prepared = MixerReverbDsp.Prepare(settings, rate);
        var dsp = new MixerReverbDsp(prepared, rate);
        var result = new float[rate * 20 * 2];
        for (int i = 0; i < result.Length / 2; i++)
        {
            double l = source switch
            {
                1 when i < rate => .1 * Math.Sin(2 * Math.PI * (220 + 55 * (i / (rate / 4))) * i / rate),
                2 when i < rate * 2 => .1 * Math.Sin(2 * Math.PI * 220 * i / rate),
                0 when i == 0 => .5,
                _ => 0
            };
            double r = source == 0 ? 0 : l * .7;
            dsp.Process(ref l, ref r, prepared);
            result[2 * i] = (float)l; result[2 * i + 1] = (float)r;
        }
        return result;
    }
    private static double Energy(float[] pcm, int start, int end) => pcm.Skip(start * 2).Take((end - start) * 2).Sum(x => (double)x * x);
    private static MixerReverb Wet(decimal decay = 1.8m, decimal size = .5m, decimal damping = .5m, decimal mix = 1) =>
        new() { Enabled = true, DecaySeconds = decay, Size = size, Damping = damping, Mix = mix };

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public void ImpulseIsDenseStereoDecayWithFiniteEndpoint(int rate)
    {
        var pcm = Response(Wet(), rate);
        Assert.Equal(0, pcm[0]);
        Assert.True(Energy(pcm, rate / 10, rate / 2) > .0001);
        Assert.True(Energy(pcm, rate / 10, rate / 2) > Energy(pcm, rate, rate * 2) * 20);
        Assert.True(pcm.Skip(rate / 2).Take(rate).Count(v => Math.Abs(v) > 1e-8) > rate * .8);
        Assert.True(Enumerable.Range(rate / 10, rate / 2).Sum(i => Math.Abs(pcm[i * 2] - pcm[i * 2 + 1])) > .1);
        Assert.All(pcm.Skip(rate * 10), v => Assert.Equal(0, v));
        Assert.All(pcm, v => Assert.True(float.IsFinite(v)));
    }

    [Fact]
    public void DecayDampingSizeAndMixHaveIndependentAudibleSignalEffects()
    {
        var shortTail = Response(Wet(.4m)); var longTail = Response(Wet(3));
        Assert.True(Energy(longTail, 48000, 96000) > Energy(shortTail, 48000, 96000) * 100);
        var bright = Response(Wet(damping: 0)); var dark = Response(Wet(damping: 1));
        static double Brightness(float[] a)
        {
            double diff = 0, energy = 0;
            for (int i = 9600; i < 96000; i += 2) { double d = a[i] - a[i - 2]; diff += d * d; energy += a[i] * a[i]; }
            return diff / energy;
        }
        Assert.True(Brightness(dark) < Brightness(bright) * .3);
        Assert.NotEqual(Response(Wet(size: 0)), Response(Wet(size: 1)));
        var wet = Response(Wet()); var half = Response(Wet(mix: .5m)); var dry = Response(Wet(mix: 0));
        Assert.Equal(.5f, dry[0]); Assert.Equal(.25f, half[0]);
        for (int i = 1; i < wet.Length; i++) Assert.Equal(wet[i] * .5f, half[i]);
        Assert.All(dry.Skip(1), v => Assert.Equal(0, v));
        Assert.Equal(dry, Response(new MixerReverb()));
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void PhraseAndSustainRemainStableAndDecayAfterSourceEnd(int source)
    {
        var pcm = Response(Wet(3, 1, .35m, .3m), source: source);
        Assert.All(pcm, v => Assert.InRange(v, -.5f, .5f));
        Assert.True(Energy(pcm, 96000, 144000) > .001);
        Assert.All(pcm.Skip(48000 * 18), v => Assert.Equal(0, v));
    }

    [Theory]
    [InlineData(44100, 0, 0)] [InlineData(48000, 1, 0)]
    [InlineData(44100, 1, 1)] [InlineData(48000, 0, 1)]
    public void MaximumDecayAndGeometryDampingExtremesAreStableAndExpire(int rate, int size, int damping)
    {
        var pcm = Response(Wet(8, size, damping), rate);
        Assert.All(pcm, value => Assert.InRange(value, -.5f, .5f));
        Assert.True(Energy(pcm, rate / 10, rate) > Energy(pcm, rate * 12, rate * 13));
        Assert.All(pcm.Skip(rate * 17 * 2), value => Assert.Equal(0, value));
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal Id<MixerChannel> Channel;
        internal MixerState Mixer => Document.Current.State.Mixer!;
        internal static async Task<Fixture> Create(int channels = 2, int rate = 48000)
        {
            var f = new Fixture();
            try
            {
                var data = new float[1024 * channels]; data[0] = .5f; if (channels == 2) data[1] = -.25f;
                string wave = f.Directory.File("impulse.wav"); File.WriteAllBytes(wave, WavFixtures.Create(data, rate, channels));
                string normalized = f.Directory.File("normalized.wav");
                AudioFileDecode.Normalize(wave, normalized, TestContext.Current.CancellationToken);
                using var import = await AudioMedia.PrepareImportAsync(f.Document, normalized, "Impulse", "Authored impulse",
                    f.Directory.File("owned"), TestContext.Current.CancellationToken);
                import.Accept(new(0), 0);
                f.Document.Edit("Mixer", e => { e.InitializeMixer(); f.Channel = e.AddMixerBus("Source");
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
        internal void Set(MixerReverb value, Id<MixerChannel>? target = null) => Document.Edit("Reverb", e =>
            e.SetMixerStrip(target, (target is {} id ? Mixer.Channels.Single(c => c.Id == id).Settings : Mixer.Master) with { Reverb = value }));
        public void Dispose() { if (!Document.IsClosed) Document.Close(); Directory.Dispose(); }
    }
    private static float[] Render(ProjectDocument document, int rate = 48000, int channels = 2, int packet = 127)
    {
        using var plan = ArrangementPreparation.Prepare(document, rate, channels, 1024);
        using var run = plan.CreateExecution();
        var data = new float[plan.EndFrame * channels];
        while (run.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(packet, plan.EndFrame - run.Position);
            run.Process(data.AsSpan((int)run.Position * channels, frames * channels));
        }
        return data;
    }

    [Theory]
    [InlineData(44100, 1)] [InlineData(44100, 2)] [InlineData(48000, 1)] [InlineData(48000, 2)]
    public async Task ProductionRealtimeOfflineAndPacketSizesHaveExactPcmParity(int rate, int channels)
    {
        using var f = await Fixture.Create(channels, rate); f.Set(Wet(.4m, mix: .4m), f.Channel);
        var expected = Render(f.Document, rate, channels);
        Assert.Equal(expected, Render(f.Document, rate, channels, 37));
        using var sampler = new RealtimeSampler(rate, channels, 127);
        using var request = sampler.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); sampler.Start();
        var actual = new float[expected.Length];
        for (int offset = 0; offset < actual.Length;)
        { int count = Math.Min(channels * 127, actual.Length - offset); sampler.Process(actual.AsSpan(offset, count)); offset += count; }
        Assert.Equal(expected, actual); Assert.Equal(SamplerTransportState.Ended, sampler.ReadStatus().Transport);
        Assert.Equal(0, sampler.ReadStatus().Voices);
        Assert.Contains(expected.Skip(rate * channels / 10), v => Math.Abs(v) > 1e-6);
    }

    [Theory]
    [InlineData("channel")] [InlineData("bus")] [InlineData("master")]
    public async Task StripsSendsMuteSoloAndSerialDelayUseSharedProcessing(string location)
    {
        using var f = await Fixture.Create(); Id<MixerChannel>? target = location == "channel" ? f.Channel : null;
        if (location == "bus")
        {
            f.Document.Edit("Return", e => target = e.AddMixerBus("Return"));
            f.Document.Edit("Pre send", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select(c =>
                c.Id == f.Channel ? c with { Settings = c.Settings with { LevelDb = -90 }, Sends = [new(target!.Value, 0, true)] } : c)] }));
        }
        f.Set(Wet(.4m, mix: .4m), target);
        var pcm = Render(f.Document); Assert.True(Energy(pcm, 4800, 24000) > .00001);
        f.Document.Edit("Solo", e => e.SetMixerStrip(f.Channel, f.Mixer.Channels[0].Settings with { Solo = true }));
        Assert.Equal(pcm, Render(f.Document));
        f.Document.Edit("Mute", e => e.SetMixerStrip(f.Channel, f.Mixer.Channels[0].Settings with { Muted = true }));
        Assert.All(Render(f.Document), v => Assert.Equal(0, v)); f.Document.Undo();
        f.Document.Edit("Delay and compression", e => e.SetMixerStrip(f.Channel, f.Mixer.Channels[0].Settings with
            { Delay = new() { Enabled = true, TimeMs = 10, Feedback = .4m }, Compressor = new() { Enabled = true } }));
        var serial = Render(f.Document); Assert.NotEqual(pcm, serial); Assert.All(serial, v => Assert.True(float.IsFinite(v)));
        if (location == "bus")
        {
            f.Document.Edit("Post send", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select(c =>
                c.Id == f.Channel ? c with { Sends = [new(target!.Value, 0, false)] } : c)] }));
            Assert.All(Render(f.Document), v => Assert.Equal(0, v));
        }
    }

    [Theory]
    [InlineData("unused")] [InlineData("mute")] [InlineData("dry")] [InlineData("zero-send")] [InlineData("fader")]
    public async Task InaudibleReverbAddsNoSilentExportTail(string kind)
    {
        using var f = await Fixture.Create(); var baseline = Render(f.Document);
        var target = f.Channel;
        if (kind == "unused" || kind == "zero-send") f.Document.Edit("Unused", e => target = e.AddMixerBus("Unused"));
        f.Set(Wet(8, mix: kind == "dry" ? 0 : .2m), target);
        if (kind == "mute" || kind == "fader") f.Document.Edit("Silence", e => e.SetMixerStrip(target,
            f.Mixer.Channels.Single(c => c.Id == target).Settings with { Muted = kind == "mute", LevelDb = kind == "fader" ? -90 : 0 }));
        if (kind == "zero-send") f.Document.Edit("Zero send", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select(c =>
            c.Id == f.Channel ? c with { Sends = [new(target, -90, true)] } : c)] }));
        Assert.Equal(baseline.Length, Render(f.Document).Length);
    }

    [Fact]
    public async Task CanonicalHistoryReaderGateOldShapeAndUnknownStateArePreserved()
    {
        using var f = await Fixture.Create(); var old = ProjectPersistence.Encode(f.Document);
        Assert.DoesNotContain("reverb", System.Text.Encoding.UTF8.GetString(old));
        f.Set(Wet(), f.Channel); Assert.True(f.Document.Undo()); Assert.Equal(old, ProjectPersistence.Encode(f.Document));
        Assert.True(f.Document.Redo()); var json = JsonNode.Parse(ProjectPersistence.Encode(f.Document))!.AsObject();
        Assert.Equal(8, json["minimumReaderMinor"]!.GetValue<int>());
        var opened = GraphFixture.Read(json).Document;
        Assert.Equal(Wet(), opened.Current.State.Mixer!.Channels[0].Settings.Reverb); opened.Close();
        json["minimumReaderMinor"] = 7;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        json["minimumReaderMinor"] = 8; json["state"]!["mixer"]!["channels"]![0]!["settings"]!["reverb"]!["futureRequired"] = 1;
        var unknown = GraphFixture.Read(json).Document;
        unknown.MediaRoots.AddRange(f.Document.MediaRoots);
        Assert.Throws<ProjectValidationException>(() => unknown.Edit("Overwrite", e => e.SetMixerStrip(f.Channel, new())));
        Assert.Contains("futureRequired", System.Text.Encoding.UTF8.GetString(ProjectPersistence.Encode(unknown)));
        Assert.Throws<GraphPreparationException>(() => ArrangementPreparation.Prepare(unknown, 48000, 2, 127, TestContext.Current.CancellationToken)); unknown.Close();
    }
    [Fact]
    public async Task ColdSeekRangeWrapStopAndPanicClearStateWithNoWarmCallbackAllocation()
    {
        using var f = await Fixture.Create(); f.Set(Wet(.5m, mix: .6m), f.Channel);
        using var rt = new RealtimeSampler(48000, 2, 127);
        using var request = rt.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); rt.Start();
        var packet = new float[254];
        for (int i = 0; i < 40; i++) rt.Process(packet);
        rt.SeekSong(7000); rt.Process(packet); Assert.All(packet, v => Assert.Equal(0, v));
        rt.Stop(); rt.Process(packet); Assert.False(rt.ReadStatus().Playing); Assert.All(packet, v => Assert.Equal(0, v));
        rt.SeekSong(0); rt.Start(); rt.Process(packet); Assert.InRange(packet[0], .199999f, .200001f);
        rt.SetSongLoop(0, 20000, true);
        for (int i = 0; i < 400; i++) rt.Process(packet);
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 400; i++) rt.Process(packet);
        Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread());
        rt.Panic(); rt.Process(packet); Assert.False(rt.ReadStatus().Playing); Assert.All(packet, v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task PatternLoopPreservesWetHistoryHardStopAndR8RemainBeforeMixer()
    {
        using var f = await Fixture.Create(); var attachment = await f.Pattern(); f.Set(Wet(.5m), f.Channel);
        using var hard = GraphPreparation.Prepare(f.Document, attachment.Id, 48000, 2, 127,
            stop: new(9600), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(240, hard.EndFrame);
        using var local = GraphPreparation.PrepareObject(f.Document, attachment.Id, 48000, 2, 1024, TestContext.Current.CancellationToken);
        Assert.Equal(0, local.MixerReverbBytes); Assert.Equal(240, local.EndFrame);
        using (var run = local.CreateExecution()) { var data = new float[480]; run.Process(data); Assert.Equal(.5f, data[0]); }
        using var rt = new RealtimeSampler(48000, 2, 127); rt.SetLooping(true);
        using var request = rt.BeginGraphPreparation(f.Document, attachment.Id);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); rt.Start();
        var pcm = new float[48000];
        for (int offset = 0; offset < pcm.Length;) { int n = Math.Min(254, pcm.Length - offset); rt.Process(pcm.AsSpan(offset, n)); offset += n; }
        Assert.Contains(pcm.Skip(12000), v => Math.Abs(v) > .0001);
        Assert.True(rt.ReadStatus().Playing); Assert.Equal(0, rt.ReadStatus().Voices);
    }

    [Fact]
    public async Task TopologyTransfersKeepExactTailAcrossSerialRetirementAndSizeChangesStayFinite()
    {
        using var f = await Fixture.Create(); f.Set(Wet(.5m), f.Channel); var expected = Render(f.Document);
        using var rt = new RealtimeSampler(48000, 2, 127);
        async Task Publish()
        {
            using var request = rt.BeginArrangementPreparation(f.Document, preservePlayback: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); rt.Start(); var actual = new float[expected.Length]; rt.Process(actual.AsSpan(0, 254));
        long epoch = rt.ReadStatus().Epoch;
        for (int i = 1; i <= 6; i++)
        {
            f.Document.Edit("Neighbour", e => { if (i % 2 == 1) e.AddMixerBus("Neighbour"); else e.RemoveMixerChannel(f.Mixer.Channels.Last().Id); });
            await Publish(); rt.Process(actual.AsSpan(i * 254, 254)); rt.RetireCompleted();
            Assert.Equal(1, rt.LivePreparedStates); Assert.Equal(epoch, rt.ReadStatus().Epoch);
        }
        for (int offset = 7 * 254; offset < actual.Length;) { int n = Math.Min(254, actual.Length - offset); rt.Process(actual.AsSpan(offset, n)); offset += n; }
        Assert.Equal(expected, actual);
        // Geometry switches while the impulse tail is already resident; voices/time remain under the same transport.
        rt.SeekSong(0); rt.Start(); var packet = new float[254];
        for (int i = 0; i < 40; i++) rt.Process(packet);
        epoch = rt.ReadStatus().Epoch; long position = rt.ReadStatus().Position;
        f.Set(Wet(.5m, size: 1), f.Channel); await Publish(); rt.Process(packet); rt.RetireCompleted();
        Assert.Equal(epoch, rt.ReadStatus().Epoch); Assert.Equal(position + 127, rt.ReadStatus().Position);
        double energy = 0;
        for (int i = 0; i < 50; i++) { rt.Process(packet); energy += packet.Sum(x => (double)x * x); }
        Assert.True(energy > 1e-9); Assert.All(packet, v => Assert.True(float.IsFinite(v)));
        rt.Dispose(); Assert.Equal(rt.PreparedStatesCreated, rt.PreparedStatesReleased);
    }

    [Fact]
    public async Task LiveCoefficientsAndBypassUseExistingBankWithoutRestartOrCallbackAllocation()
    {
        using var f = await Fixture.Create(); f.Set(Wet(.5m, mix: .4m), f.Channel);
        using var rt = new RealtimeSampler(48000, 2, 127);
        using var request = rt.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); rt.Start();
        var packet = new float[254];
        for (int i = 0; i < 40; i++) rt.Process(packet);
        var before = rt.ReadStatus();
        f.Set(Wet(2, damping: .9m, mix: .8m), f.Channel);
        Assert.True(rt.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId)); rt.Process(packet);
        f.Set(Wet(3, damping: .1m, mix: .2m), f.Channel);
        Assert.True(rt.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId));
        long bytes = GC.GetAllocatedBytesForCurrentThread(); rt.Process(packet);
        Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread());
        Assert.Equal(before.Epoch, rt.ReadStatus().Epoch); Assert.Equal(before.Position + 254, rt.ReadStatus().Position);
        f.Set(Wet(3) with { Enabled = false }, f.Channel);
        Assert.True(rt.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId)); rt.Process(packet);
        for (int i = 0; i < 100; i++) rt.Process(packet);
        Assert.All(packet, v => Assert.Equal(0, v)); Assert.Equal(1, rt.PreparedStatesCreated);
        f.Set(Wet(3), f.Channel); Assert.True(rt.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId));
        rt.Process(packet); Assert.All(packet, v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task DisabledBanksHaveNoReverbPayloadAndAdmissionRefusesBeforePublication()
    {
        using var f = await Fixture.Create();
        f.Document.Edit("Bank", e => { for (int i = 1; i < 64; i++) e.AddMixerBus("Return " + i); });
        using (var plan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 127, TestContext.Current.CancellationToken))
        { Assert.Equal(0, plan.MixerReverbBytes); Assert.Equal(65 * 48001 * 16, plan.MixerDelayBytes); }
        f.Set(Wet(.5m), f.Channel);
        using var rt = new RealtimeSampler(48000, 2, 127);
        using var request = rt.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); rt.Start(); var packet = new float[254]; rt.Process(packet);
        var before = rt.ReadStatus();
        f.Document.Edit("Over budget", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select(c =>
            c with { Settings = c.Settings with { Reverb = Wet(8, 1) } })] }));
        var error = Assert.Throws<GraphPreparationException>(() => rt.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId));
        Assert.Equal(GraphPreparationReasons.Budget, error.Reason); rt.Process(packet);
        Assert.Equal(before.ExecutingRevision, rt.ReadStatus().ExecutingRevision);
        Assert.Equal(before.Epoch, rt.ReadStatus().Epoch);
        Assert.InRange(rt.RetainedPcmAndScratchBytes, 1, RealtimeSampler.MaximumRetainedPcmAndScratchBytes);
    }

    [Theory]
    [InlineData(.19, .5, .5, .5)] [InlineData(8.01, .5, .5, .5)]
    [InlineData(1, -.01, .5, .5)] [InlineData(1, .5, 1.01, .5)] [InlineData(1, .5, .5, 1.01)]
    public async Task InvalidCanonicalParametersAreAtomic(double decay, double size, double damping, double mix)
    {
        using var f = await Fixture.Create(); var before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Set(Wet((decimal)decay, (decimal)size, (decimal)damping, (decimal)mix)));
        Assert.Equal(before, f.Document.Current);
    }

    [Fact]
    public async Task WorkerEnablePreservesTransportAndOriginalNoteGate()
    {
        using var f = await Fixture.Create(); var attachment = await f.Pattern(gate: 9600);
        using var rt = new RealtimeSampler(48000, 2, 127); var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(f.Document, rt, attachment.Id, owner, 8, 1, null, false));
        try
        {
            await owner.Until(() => rt.HasPending); owner.Run(() => Assert.True(coordinator.Start()));
            var packet = new float[254]; rt.Process(packet); var before = rt.ReadStatus(); Assert.Equal(1, before.Voices);
            owner.Run(() => f.Set(Wet(.5m), f.Channel));
            await owner.Until(() => rt.HasPending); rt.Process(packet); owner.Run(coordinator.Progress);
            Assert.Equal(before.Epoch, rt.ReadStatus().Epoch); Assert.Equal(254, rt.ReadStatus().Position);
            Assert.Equal(0, rt.ReadStatus().Voices); Assert.True(rt.ReadStatus().Playing);
            Assert.Equal(2, rt.PreparedStatesCreated); Assert.Equal(f.Document.Current.Revision, rt.ReadStatus().ExecutingRevision);
        }
        finally { await owner.Join(coordinator.DisposeAsync); }
    }

    [Fact]
    public async Task LiveTailRefusalShowsBlockerRetainsLastValidAndUndoRepairs()
    {
        using var f = await Fixture.Create(); var attachment = await f.Pattern();
        f.Document.Edit("Serial bank", e => { for (int i = 1; i < 8; i++) e.AddMixerBus("Return " + i); });
        f.Document.Edit("Reverbs", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select((c, i) =>
            c with { Output = i < 7 ? f.Mixer.Channels[i + 1].Id : null,
                Settings = c.Settings with { Reverb = Wet(i < 6 ? 8 : .2m) } })] }));
        using var rt = new RealtimeSampler(48000, 2, 127); rt.SetLooping(true); var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() => coordinator = new(f.Document, rt, attachment.Id, owner, 8, 1, null, false));
        try
        {
            await owner.Until(() => rt.HasPending); owner.Run(() => Assert.True(coordinator.Start()));
            var packet = new float[254]; rt.Process(packet); var before = rt.ReadStatus(); int undo = f.Document.UndoCount;
            owner.Run(() => f.Set(Wet(8), f.Mixer.Channels[6].Id));
            Assert.Equal(undo + 1, f.Document.UndoCount); Assert.Equal(GraphPreparationReasons.Budget, coordinator.ReadStatus().Blocker);
            rt.Process(packet); Assert.True(coordinator.ReadStatus().LastValidPlaying);
            Assert.Equal(before.ExecutingRevision, rt.ReadStatus().ExecutingRevision);
            owner.Run(() => Assert.True(f.Document.Undo())); rt.Process(packet);
            Assert.False(coordinator.ReadStatus().InvalidCanonical); Assert.Equal(before.Epoch, rt.ReadStatus().Epoch);
            Assert.Equal(f.Document.Current.Revision, rt.ReadStatus().ExecutingRevision);
        }
        finally { await owner.Join(coordinator.DisposeAsync); }
    }

    [Fact]
    public async Task PortableSaveAsAndPcm16ExportPreserveProcessedTailAndCancelledExportPreservesDestination()
    {
        using var f = await Fixture.Create(); f.Set(Wet(.6m, mix: .4m), f.Channel);
        f.Document.Edit("Delay", e => e.SetMixerStrip(f.Channel, f.Mixer.Channels[0].Settings with
            { Delay = new() { Enabled = true, TimeMs = 50, Feedback = .4m } }));
        string project = f.Directory.File("portable.seqvium"), wave = f.Directory.File("processed.wav");
        ProjectPersistence.Save(f.Document, project);
        var reopened = ProjectPersistence.Open(project).Document;
        try
        {
            Assert.Equal(Render(f.Document), Render(reopened));
            var result = await PatternWaveExport.ExportArrangementAsync(reopened, wave, TestContext.Current.CancellationToken);
            Assert.Equal(0, result.ClippedSamples);
            using var pcm = WavDecoder.Decode(File.ReadAllBytes(wave));
            using var plan = ArrangementPreparation.Prepare(reopened, 48000, 2, 1024, TestContext.Current.CancellationToken);
            Assert.Equal(plan.EndFrame, pcm.Frames); Assert.True(pcm.Frames > 48000);
            using var run = plan.CreateExecution(smoothBoundaries: true); var packet = new float[2048];
            double lateEnergy = 0;
            while (run.Position < plan.EndFrame)
            {
                long start = run.Position; int n = (int)Math.Min(1024, plan.EndFrame - start); run.Process(packet.AsSpan(0, n * 2));
                for (int i = 0; i < n; i++) for (int c = 0; c < 2; c++)
                {
                    short expected = (short)Math.Clamp((int)Math.Round(packet[2 * i + c] * 32768), short.MinValue, short.MaxValue);
                    Assert.Equal(expected / 32768f, pcm.Sample((int)start + i, c));
                    if (start + i > 4800) lateEnergy += packet[2 * i + c] * packet[2 * i + c];
                }
            }
            Assert.True(lateEnergy > .00001);
            var bytes = File.ReadAllBytes(wave); using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PatternWaveExport.ExportArrangementAsync(reopened, wave, cancelled.Token));
            Assert.Equal(bytes, File.ReadAllBytes(wave));
        }
        finally { reopened.Close(); }
    }

    [Fact]
    public async Task BypassHistoryTransfersThroughTopologyWithoutLostTailOrUnaccountedBuffers()
    {
        using var f = await Fixture.Create(); f.Set(Wet(.5m), f.Channel);
        using var rt = new RealtimeSampler(48000, 2, 127);
        async Task Publish()
        {
            using var request = rt.BeginArrangementPreparation(f.Document, preservePlayback: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); rt.Start(); var packet = new float[254];
        for (int i = 0; i < 40; i++) rt.Process(packet);
        var before = rt.ReadStatus(); long bytes = rt.RetainedPcmAndScratchBytes;
        f.Set(Wet(.5m) with { Enabled = false }, f.Channel);
        f.Document.Edit("Neighbour", e => e.AddMixerBus("Neighbour"));
        await Publish(); rt.Process(packet); rt.RetireCompleted();
        Assert.Equal(before.Epoch, rt.ReadStatus().Epoch); Assert.Equal(before.Position + 127, rt.ReadStatus().Position);
        Assert.Contains(packet, v => Math.Abs(v) > 1e-7);
        Assert.True(rt.RetainedPcmAndScratchBytes >= bytes);
        for (int i = 0; i < 100; i++) rt.Process(packet); Assert.All(packet, v => Assert.Equal(0, v));
        Assert.Equal(1, rt.LivePreparedStates);
    }

    [Fact]
    public async Task PublicationSlotsAccountAdmittedBanksAndRejectCancelledStaleOrFourthCandidates()
    {
        using var f = await Fixture.Create(); f.Set(Wet(.5m), f.Channel);
        using var rt = new RealtimeSampler(48000, 2, 127);
        async Task Publish()
        {
            using var request = rt.BeginArrangementPreparation(f.Document, preservePlayback: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); rt.Start(); var packet = new float[254]; rt.Process(packet); long one = rt.RetainedPcmAndScratchBytes;
        await Publish(); rt.Process(packet); // active plus deferred retired
        await Publish(); Assert.Equal(3, rt.LivePreparedStates); Assert.Equal(one * 3, rt.RetainedPcmAndScratchBytes);
        using (var fourth = rt.BeginArrangementPreparation(f.Document))
        {
            await fourth.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.InRange(rt.ReservedPcmAndScratchBytes, rt.RetainedPcmAndScratchBytes, RealtimeSampler.MaximumRetainedPcmAndScratchBytes);
            Assert.Equal(SamplerPublication.Capacity, fourth.Publish(TestContext.Current.CancellationToken));
        }
        using (var cancelled = rt.BeginArrangementPreparation(f.Document))
        {
            await cancelled.PrepareAsync(TestContext.Current.CancellationToken);
            using var token = new CancellationTokenSource(); token.Cancel();
            Assert.Equal(SamplerPublication.Cancelled, cancelled.Publish(token.Token));
        }
        using (var stale = rt.BeginArrangementPreparation(f.Document))
        {
            await stale.PrepareAsync(TestContext.Current.CancellationToken);
            f.Set(Wet(.6m), f.Channel);
            Assert.Equal(SamplerPublication.Stale, stale.Publish(TestContext.Current.CancellationToken));
        }
        Assert.Equal(3, rt.LivePreparedStates); rt.RetireCompleted(); rt.Process(packet); rt.RetireCompleted();
        Assert.Equal(1, rt.LivePreparedStates); rt.Dispose(); Assert.Equal(rt.PreparedStatesCreated, rt.PreparedStatesReleased);
        Assert.Equal(0, rt.RetainedPcmAndScratchBytes);
    }

    [Fact]
    public async Task RemovedSourceHistoryCanExciteNewMasterWithoutTruncatingItsTailOrGrowingOnRepeatedEdits()
    {
        using var f = await Fixture.Create(); f.Set(Wet(.5m), f.Channel);
        using var original = ArrangementPreparation.Prepare(f.Document, 48000, 2, 127, TestContext.Current.CancellationToken);
        long oldEnd = original.EndFrame;
        using var rt = new RealtimeSampler(48000, 2, 127);
        async Task Publish()
        {
            using var request = rt.BeginArrangementPreparation(f.Document, preservePlayback: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); rt.Start(); var packet = new float[254];
        for (int i = 0; i < 40; i++) rt.Process(packet);
        f.Document.Edit("Remove source", e =>
        {
            var clip = f.Document.Current.State.AudioClips.Single();
            e.CopyAudioClip(clip.Id, new(0), 1); e.DeleteAudioClip(clip.Id);
            e.SetArrangementTrack(1, "Muted", 1, true, false);
        });
        f.Set(Wet(8), null); await Publish(); rt.Process(packet); rt.RetireCompleted();
        long epoch = rt.ReadStatus().Epoch;
        // Canonical export is silent; live runtime still owns an excited channel FDN feeding the new Master.
        Assert.All(Render(f.Document), value => Assert.Equal(0, value));
        for (int i = 0; i < 2000 && rt.ReadStatus().Playing && rt.ReadStatus().Position <= oldEnd; i++) rt.Process(packet);
        Assert.True(rt.ReadStatus().Playing); Assert.Contains(packet, value => Math.Abs(value) > 1e-12);
        for (int i = 0; i < 4; i++)
        { f.Document.Edit("Neighbour", e => e.AddMixerBus("Unused " + i)); await Publish(); rt.Process(packet); rt.RetireCompleted(); }
        int packets = 0;
        while (rt.ReadStatus().Playing && packets++ < 9000) rt.Process(packet);
        Assert.Equal(SamplerTransportState.Ended, rt.ReadStatus().Transport); Assert.Equal(epoch, rt.ReadStatus().Epoch);
        Assert.Equal(1024 + (2 + 17) * 48000, rt.ReadStatus().Position);
    }

    [Fact]
    public async Task InheritedHistoryTailBudgetRefusesCanonicalDryChainBeforeReplacingLastValidAudio()
    {
        using var f = await Fixture.Create();
        f.Document.Edit("Serial bank", e => { for (int i = 1; i < 7; i++) e.AddMixerBus("Return " + i); });
        f.Document.Edit("Long histories", e => e.SetMixer(f.Mixer with { Channels = [.. f.Mixer.Channels.Select((c, i) =>
            c with { Output = i < 6 ? f.Mixer.Channels[i + 1].Id : null,
                Settings = c.Settings with { Reverb = Wet(8) } })] }));
        using var rt = new RealtimeSampler(48000, 2, 127);
        using (var initial = rt.BeginArrangementPreparation(f.Document))
        {
            await initial.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, initial.Publish(TestContext.Current.CancellationToken));
        }
        rt.Start(); var packet = new float[254]; rt.Process(packet); var before = rt.ReadStatus();
        f.Document.Edit("Bypass chain and enable Master", e => e.SetMixer(f.Mixer with
        {
            Channels = [.. f.Mixer.Channels.Select(c => c with { Settings = c.Settings with { Reverb = c.Settings.Reverb with { Enabled = false } } })],
            Master = f.Mixer.Master with { Reverb = Wet(8) }
        }));
        using (var fresh = ArrangementPreparation.Prepare(f.Document, 48000, 2, 127, TestContext.Current.CancellationToken))
            Assert.Equal(1024 + 17 * 48000, fresh.EndFrame);
        using var candidate = rt.BeginArrangementPreparation(f.Document, preservePlayback: true);
        await candidate.PrepareAsync(TestContext.Current.CancellationToken);
        var error = Assert.Throws<GraphPreparationException>(() => candidate.Publish(TestContext.Current.CancellationToken));
        Assert.Equal(GraphPreparationReasons.Budget, error.Reason); rt.Process(packet);
        Assert.True(rt.ReadStatus().Playing); Assert.Equal(before.ExecutingRevision, rt.ReadStatus().ExecutingRevision);
        Assert.Equal(before.Epoch, rt.ReadStatus().Epoch);
    }

}


