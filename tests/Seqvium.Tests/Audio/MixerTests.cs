// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class MixerTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal readonly List<GraphAttachment> Attachments = [];
        internal readonly List<GraphNode> Gains = [];

        internal static async Task<Fixture> Create(int count = 1)
        {
            var f = new Fixture();
            for (int n = 0; n < count; n++)
            {
                using var import = ProjectMedia.BeginImport(f.Document, "Signal", releaseMilliseconds: 0,
                    ownedMediaDirectory: f.Directory.File("media"));
                using var bytes = new MemoryStream(WavFixtures.Create(Enumerable.Range(0, 24000)
                    .SelectMany(i => new[] { .2f, .1f }).ToArray(), 48000, 2));
                await import.PrepareAsync(bytes, TestContext.Current.CancellationToken);
                import.Accept((edit, media) =>
                {
                    var pattern = edit.AddPattern("Pattern", new(960000));
                    var part = edit.AddPart(pattern, "Instrument", media.SoundId);
                    edit.AddNote(pattern, part, new(0), new(960000), 60, 1);
                    var placement = edit.AddPlacement(pattern, new(0));
                    var attachment = edit.CreateItemGraph(placement);
                    f.Attachments.Add(attachment);
                    var source = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                    var gain = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Gain, new(100, 0));
                    f.Gains.Add(gain);
                    var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(200, 0));
                    edit.SetGraphGain(attachment.GraphId, gain.Id, .5m);
                    edit.BindGraphSource(attachment.Id, source.Id, placement, part);
                    GraphFixture.Connect(edit, attachment.GraphId, source, gain);
                    GraphFixture.Connect(edit, attachment.GraphId, gain, output);
                    edit.SetGraphOutput(attachment.Id, output.Id);
                    edit.AddArrangementClip(placement, new(0), n, 1);
                });
            }

            return f;
        }

        internal MixerState Mixer => Document.Current.State.Mixer!;
        internal Id<MixerChannel> Channel => Mixer.Channels[0].Id;
        internal void Enable() => Document.Edit("Mixer", e => e.InitializeMixer());
        internal void Settings(MixerStripSettings s) => Document.Edit("Strip", e => e.SetMixerStrip(Channel, s));

        public void Dispose()
        {
            Document.Close();
            Directory.Dispose();
        }
    }

    private static float[] Render(ProjectDocument document, bool song = true, int packet = 127)
    {
        using var plan = song
            ? ArrangementPreparation.Prepare(document, 48000, 2, 1024)
            : GraphPreparation.Prepare(document, document.Current.State.GraphAttachments[0].Id, 48000, 2, 1024);
        using var execution = plan.CreateExecution();
        var output = new float[plan.EndFrame * 2];
        while (execution.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(packet, plan.EndFrame - execution.Position);
            execution.Process(output.AsSpan((int)execution.Position * 2, frames * 2));
        }

        return output;
    }

    [Fact]
    public async Task UnityOptInPreservesLegacyPatternAndSongAndGroupsAllOccurrences()
    {
        using var f = await Fixture.Create(2);
        var song = Render(f.Document);
        var pattern = Render(f.Document, false);
        f.Enable();
        Assert.Single(f.Mixer.Channels);
        Assert.Equal(2, f.Mixer.Channels[0].SoundIds.Length);
        Assert.Equal(song, Render(f.Document));
        Assert.Equal(pattern, Render(f.Document, false));
        Assert.InRange(Render(f.Document)[1000], .1999f, .2001f);
    }

    [Theory]
    [InlineData(6, 0, false)]
    [InlineData(-12, -.5, false)]
    [InlineData(0, 1, false)]
    [InlineData(0, -1, false)]
    [InlineData(-90, 0, false)]
    [InlineData(0, 0, true)]
    public async Task GainPanAndMuteHaveMeasuredMusicalEffects(int db, double pan, bool mute)
    {
        using var f = await Fixture.Create();
        f.Enable();
        f.Settings(new() { LevelDb = db, Pan = (decimal)pan, Muted = mute });
        var data = Render(f.Document);
        double gain = mute ? 0 : MixerDsp.Amplitude(db);
        double left = pan > 0 ? Math.Cos(pan * Math.PI / 2) : 1;
        double right = pan < 0 ? Math.Cos(pan * Math.PI / 2) : 1;
        Assert.InRange(Math.Abs(data[1000] - .1 * gain * left), 0, .00005);
        Assert.InRange(Math.Abs(data[1001] - .05 * gain * right), 0, .00005);
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void EqTransferIsCalibratedAtHostRateAndCompressorHasSoftKnee(int rate)
    {
        var identity = MixerDsp.Filters(new() { Enabled = true }, rate);
        foreach (int hz in new[] { 20, 120, 1000, 8000, 18000 })
            Assert.InRange(Math.Abs(identity.Aggregate(1d, (g, c) => g * c.Magnitude(hz, rate)) - 1), 0, 1e-10);
        var filters = MixerDsp.Filters(new() { Enabled = true, MidDb = 12, MidHz = 1000, HighPassHz = 80 }, rate);
        Assert.InRange(20 * Math.Log10(filters.Aggregate(1d, (g, c) => g * c.Magnitude(1000, rate))), 11.99, 12.01);
        Assert.True(filters[0].Magnitude(20, rate) < .07);
        var comp = new MixerCompressor { ThresholdDb = -20, Ratio = 4, KneeDb = 0 };
        Assert.Equal(15, MixerDsp.ReductionDb(0, comp));
        Assert.Equal(0, MixerDsp.ReductionDb(-25, comp));
        Assert.True(MixerDsp.ReductionDb(-20, comp with { KneeDb = 6 }) > 0);
    }

    [Fact]
    public async Task StereoCompressorProcessesTheAggregateRatherThanEachPatternAndMetersMeasureIt()
    {
        using var f = await Fixture.Create(2);
        f.Enable();
        f.Settings(new()
            { Compressor = new() { Enabled = true, ThresholdDb = -25, Ratio = 10, AttackMs = .1m, KneeDb = 0 } });
        using var plan =
            ArrangementPreparation.Prepare(f.Document, 48000, 2, 1024, TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution();
        var packet = new float[2048];
        for (int n = 0; n < 8; n++) execution.Process(packet);
        var meter = execution.ReadMixerMeter(f.Channel);
        Assert.True(meter.ReductionDb > 8);
        Assert.InRange(meter.PeakLeft, .05f, .08f);
        Assert.InRange(meter.PeakLeft / meter.PeakRight, 1.999f, 2.001f);
        Assert.InRange(meter.RmsLeft, meter.PeakLeft - .0001f, meter.PeakLeft);
        Assert.Equal(8192, meter.Frames);
        Assert.Equal(0, meter.NonFiniteSamples);
    }

    [Fact]
    public async Task BusSendSoloAndCycleRefusalPreserveAtomicHistory()
    {
        using var f = await Fixture.Create();
        f.Enable();
        Id<MixerChannel> bus = default;
        f.Document.Edit("Bus", e => bus = e.AddMixerBus("Drums"));
        var mixer = f.Mixer;
        f.Document.Edit("Route",
            e => e.SetMixer(mixer with
            {
                Channels =
                [
                    .. mixer.Channels.Select(c =>
                        c.Id == f.Channel
                            ? c with { Output = bus }
                            : c with { Settings = c.Settings with { LevelDb = -6, Solo = true } })
                ]
            }));
        Assert.InRange(Render(f.Document)[1000], .05f, .051f);
        var before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Cycle", e => e.SetMixer(f.Mixer with
        {
            Channels = [.. f.Mixer.Channels.Select(c => c.Id == bus ? c with { Output = f.Channel } : c)]
        })));
        Assert.Equal(before, f.Document.Current);
        f.Document.Edit("Parallel", e => e.SetMixer(f.Mixer with
        {
            Channels =
            [
                .. f.Mixer.Channels.Select(c => c.Id == f.Channel
                    ? c with
                    {
                        Output = null, Sends = [new(bus, 0, false)]
                    }
                    : c with { Settings = c.Settings with { Solo = false, LevelDb = 0 } })
            ]
        }));
        Assert.InRange(Render(f.Document)[1000], .1999f, .2001f);
        f.Document.Edit("Pre", e => e.SetMixer(f.Mixer with
        {
            Channels =
            [
                .. f.Mixer.Channels.Select(c => c.Id == f.Channel
                    ? c with
                    {
                        Settings = c.Settings with { LevelDb = -90 }, Sends = [new(bus, 0, true)]
                    }
                    : c)
            ]
        }));
        Assert.InRange(Render(f.Document)[1000], .0999f, .1001f);
    }

    [Fact]
    public async Task PersistenceGateUnknownDataAndUndoAreLossless()
    {
        using var f = await Fixture.Create();
        f.Enable();
        var initial = f.Document.Current;
        f.Settings(new()
        {
            TrimDb = 24, LevelDb = -4, Pan = .35m, Eq = new() { Enabled = true, LowDb = -4 },
            Compressor = new() { Enabled = true, Ratio = 6 }
        });
        var final = f.Document.Current;
        f.Document.Undo();
        Assert.Equal(initial, f.Document.Current);
        f.Document.Redo();
        Assert.Equal(final, f.Document.Current);
        string path = f.Directory.File("Mixer.seqvium");
        ProjectPersistence.Save(f.Document, path);
        var json = GraphFixture.Json(f.Document);
        Assert.Equal(3, json["minimumReaderMinor"]!.GetValue<int>());
        var reopened = ProjectPersistence.Open(path).Document;
        try
        {
            Assert.Equal(Render(f.Document), Render(reopened));
        }
        finally
        {
            reopened.Close();
        }

        json["state"]!["mixer"]!["futureOption"] = 7;
        var unknown = GraphFixture.Read(json).Document;
        unknown.MediaRoots.AddRange(f.Document.MediaRoots);
        try
        {
            Assert.Equal(7, GraphFixture.Json(unknown)["state"]!["mixer"]!["futureOption"]!.GetValue<int>());
            Assert.Throws<GraphPreparationException>(() =>
                ArrangementPreparation.Prepare(unknown, 48000, 2, 127, TestContext.Current.CancellationToken));
            Assert.Throws<ProjectValidationException>(() =>
                unknown.Edit("Overwrite", e => e.SetMixerStrip(f.Channel, new())));
        }
        finally
        {
            unknown.Close();
        }

        json["minimumReaderMinor"] = 2;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
    }

    [Fact]
    public async Task EqAndCompressionArePartitionIndependentAndRenderingAllocatesNothing()
    {
        using var f = await Fixture.Create();
        f.Enable();
        f.Settings(new()
        {
            TrimDb = 6, Eq = new() { Enabled = true, HighPassHz = 50, MidDb = 4, MidQ = 2, HighDb = -3 },
            Compressor = new() { Enabled = true, ThresholdDb = -24, Ratio = 4 }
        });
        Assert.Equal(Render(f.Document, packet: 1), Render(f.Document, packet: 1024));
        using var plan =
            ArrangementPreparation.Prepare(f.Document, 48000, 2, 127, TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution();
        var packet = new float[254];
        for (int n = 0; n < 20; n++) execution.Process(packet);
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        for (int n = 0; n < 50; n++) execution.Process(packet);
        Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public async Task FastMixerPublicationRetainsVoicesEpochAndStickyStopWithoutDecode()
    {
        using var f = await Fixture.Create();
        f.Enable();
        using var sampler = new RealtimeSampler(48000, 2, 127);
        using var request = sampler.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        sampler.Start();
        var packet = new float[254];
        sampler.Process(packet);
        var before = sampler.ReadStatus();
        f.Settings(new() { Pan = 1, LevelDb = 6 });
        Assert.True(sampler.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId));
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        sampler.Process(packet);
        Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread());
        var after = sampler.ReadStatus();
        Assert.Equal(before.Epoch, after.Epoch);
        Assert.Equal(before.Position + 127, after.Position);
        Assert.Equal(before.Voices, after.Voices);
        Assert.Equal(f.Document.Current.Revision, after.ExecutingRevision);
        Assert.Equal(1, sampler.PreparedStatesCreated);
        sampler.Stop();
        f.Settings(new() { Pan = -1 });
        Assert.True(sampler.PublishMixerUpdate(f.Document.Current, before.PreparedExecutionId));
        sampler.Process(packet);
        Assert.All(packet, v => Assert.Equal(0, v));
        Assert.False(sampler.ReadStatus().Playing);
    }

    [Fact]
    public async Task MasterPeakCeilingBoundsOverloadsWithoutHidingTheirMeasurement()
    {
        using var f = await Fixture.Create();
        f.Enable();
        f.Settings(new() { TrimDb = 36, LevelDb = 12 });
        f.Document.Edit("Ceiling", e => e.SetMixer(f.Mixer with { ProtectOutput = true }));
        var data = Render(f.Document);
        Assert.All(data, v => Assert.InRange(v, -.891251f, .891251f));
        using var plan =
            ArrangementPreparation.Prepare(f.Document, 48000, 2, 127, TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution();
        execution.Process(new float[254]);
        Assert.True(execution.ReadMixerMeter().Overloads > 0);
    }

    [Fact]
    public async Task SoloExcludesUnassignedDirectMasterSources()
    {
        using var f = await Fixture.Create(2);
        f.Enable();
        var sound = f.Mixer.Channels[0].SoundIds[1];
        f.Document.Edit("Direct Master", e => e.AssignMixerSound(sound, null));
        f.Settings(new() { Solo = true });
        Assert.InRange(Render(f.Document)[1000], .0999f, .1001f);
        f.Settings(new());
        Assert.InRange(Render(f.Document)[1000], .1999f, .2001f);
    }

    [Fact]
    public async Task RestartAfterFastEqUpdateMatchesFreshOfflineExecution()
    {
        using var f = await Fixture.Create();
        f.Enable();
        using var sampler = new RealtimeSampler(48000, 2, 127);
        using var request = sampler.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        sampler.Start();
        var packet = new float[254];
        sampler.Process(packet);
        var id = sampler.ReadStatus().PreparedExecutionId;
        f.Settings(new() { LevelDb = 6, Eq = new() { Enabled = true, HighPassHz = 140, MidDb = 6 } });
        Assert.False(sampler.PublishMixerUpdate(f.Document.Current, id + 1));
        Assert.True(sampler.PublishMixerUpdate(f.Document.Current, id));
        sampler.Process(packet);
        sampler.Stop();
        sampler.Process(packet);
        sampler.Start();
        sampler.Process(packet);
        Assert.Equal(Render(f.Document, packet: 127).Take(254), packet);
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public async Task ProcessedRangeLoopMatchesBoundedColdSeekAtEveryWrap(int rate)
    {
        using var f = await Fixture.Create(); f.Enable();
        f.Settings(new() { TrimDb = 12, Eq = new() { Enabled = true, HighPassHz = 500, MidDb = 12, MidQ = 4 },
            Compressor = new() { Enabled = true, ThresholdDb = -40, Ratio = 20 } });
        f.Document.Edit("Protect", e => e.SetMixer(f.Mixer with { ProtectOutput = true }));
        using var plan = ArrangementPreparation.Prepare(f.Document, rate, 2, 127, TestContext.Current.CancellationToken);
        using var cold = plan.CreateExecution(); using var streaming = plan.CreateExecution();
        int start = rate / 10, length = rate / 100;
        var segment = new float[length * 2]; cold.Seek(start);
        for (int offset = 0; offset < length; offset += 127)
            cold.Process(segment.AsSpan(offset * 2, Math.Min(127, length - offset) * 2));
        var scratch = new float[254];
        while (streaming.Position < start)
            streaming.Process(scratch.AsSpan(0, (int)Math.Min(127, start - streaming.Position) * 2));
        streaming.Process(scratch);
        Assert.Contains(segment.Take(254).Zip(scratch), p => Math.Abs(p.First - p.Second) > .000001f);
        foreach (int packetSize in new[] { 1, 127 })
        {
            using var sampler = new RealtimeSampler(rate, 2, 127);
            using var request = sampler.BeginArrangementPreparation(f.Document);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
            sampler.SeekSong(start); sampler.SetSongLoop(start, start + length, true); sampler.Start();
            for (int offset = 0; offset < length * 4;)
            {
                int count = Math.Min(packetSize, length * 4 - offset);
                sampler.Process(scratch.AsSpan(0, count * 2));
                for (int sample = 0; sample < count * 2; sample++)
                {
                    Assert.Equal(segment[(offset * 2 + sample) % segment.Length], scratch[sample]);
                    Assert.True(float.IsFinite(scratch[sample]));
                    Assert.InRange(Math.Abs(scratch[sample]), 0, .891252f);
                }
                offset += count;
            }
            Assert.True(sampler.ReadStatus().Playing);
            sampler.Panic(); sampler.Process(scratch);
            Assert.All(scratch, v => Assert.Equal(0, v));
        }
    }
}
