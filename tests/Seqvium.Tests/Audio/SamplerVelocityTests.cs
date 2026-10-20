// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class SamplerKeymapTests
{
    private static async Task<ImmutableArray<SamplerZone>> VelocityMap(Fixture f, int layers = 3)
    {
        var zones = f.Zones.Select(z => z with { RootPitch = 60, LowKey = 0, HighKey = 127 }).ToList();
        if (layers == 3)
        {
            var third = await WavFixtures.Import(f.Document, f.Directory,
                Enumerable.Range(0, 256).SelectMany(i => new[] { i % 16 < 8 ? .125f : -.125f, .0625f }).ToArray(),
                48000, 2, root: 60, release: 10);
            zones.Add(new(Id<SamplerZone>.New(), third.ResourceId, 60, 0, 127, ImmutableDictionary<string, decimal>.Empty));
        }
        return [.. zones.Select((z, i) => z with
        {
            LowVelocity = i == 0 ? 0 : layers == 2 ? .5m : i == 1 ? .33m : .66m,
            HighVelocity = i == layers - 1 ? 1 : layers == 2 ? .5m : i == 0 ? .33m : .66m
        })];
    }

    private static void VelocityNote(Fixture f, decimal intensity, decimal pitch = 60) =>
        f.Document.Edit("Velocity", e => e.SetPartNotes(f.Pattern, f.Part,
            [new(Id<NoteEvent>.New(), new(0), new(38400), pitch, intensity)]));

    [Theory]
    [InlineData(44100, 1, 2)] [InlineData(44100, 2, 3)]
    [InlineData(48000, 1, 3)] [InlineData(48000, 2, 2)]
    [InlineData(44100, 1, 3)] [InlineData(44100, 2, 2)]
    [InlineData(48000, 1, 2)] [InlineData(48000, 2, 3)]
    public async Task VelocityBoundariesChooseOneRealRecordingWithIndependentPcmOracle(int rate, int channels, int layers)
    {
        using var f = await Fixture.Create(48000, channels);
        var zones = await VelocityMap(f, layers);
        f.Set(zones);
        decimal[] velocities = layers == 2 ? [0, .499999m, .5m, 1] : [0, .329999m, .33m, .659999m, .66m, 1];
        foreach (decimal velocity in velocities)
        foreach (decimal pitch in new[] { 60m, 60.75m })
        {
            VelocityNote(f, velocity, pitch);
            using var plan = Prepare(f, rate, channels);
            int layer = layers == 2 ? velocity < .5m ? 0 : 1 : velocity < .33m ? 0 : velocity < .66m ? 1 : 2;
            Assert.Single(plan.Notes);
            Assert.Equal(zones[layer].ResourceId, plan.Notes[0].SamplerResource);
            using var execution = plan.CreateExecution();
            var actual = new float[32 * channels];
            execution.Process(actual);
            double step = 48000d / rate * Math.Pow(2, (double)(pitch - 60) / 12);
            float Sample(int frame, int channel) => layer switch
            {
                0 => frame / 512f * (channel == 0 ? 1 : -.5f),
                1 => frame / 512f * (channel == 0 ? -1 : .25f),
                _ => channel == 0 ? frame % 16 < 8 ? .125f : -.125f : .0625f
            };
            for (int i = 0; i < 32; i++)
            for (int c = 0; c < channels; c++)
            {
                double cursor = i * step;
                int whole = (int)cursor;
                float Native(int frame) => layer == 2 && channels == 1
                    ? (Sample(frame, 0) + Sample(frame, 1)) * .5f : Sample(frame, c);
                double expected = (Native(whole) + (Native(whole + 1) - Native(whole)) * (cursor - whole)) * (double)velocity;
                Assert.InRange(Math.Abs(actual[i * channels + c] - expected), 0, .000002);
            }
            using var rt = new RealtimeSampler(rate, channels, 128);
            using var request = rt.BeginGraphPreparation(f.Document, f.Attachment.Id);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
            rt.Start();
            var packet = new float[actual.Length];
            rt.Process(packet);
            Assert.Equal(actual, packet);
            Assert.InRange(rt.ReadStatus().Voices, 0, 1); // Velocity 0 still selects Soft, but is silent.
        }
        // Array order is never a musical tie breaker.
        f.Set([.. zones.Reverse()]);
        Assert.Equal(zones[^1].ResourceId, SamplerKeymap.Select(f.Definition, 60, 1)!.ResourceIds.Single());
    }

    [Fact]
    public async Task VelocityPitchGeometryRejectsAmbiguityAndBadBoundsButAllowsGapsAndFullCapacity()
    {
        using var f = await Fixture.Create();
        var zones = await VelocityMap(f, 2);
        f.Set(zones);
        var snapshot = f.Document.Current;
        foreach (var invalid in new[]
        {
            zones.SetItem(1, zones[1] with { LowVelocity = .49m }),
            zones.SetItem(0, zones[0] with { LowVelocity = -.01m }),
            zones.SetItem(1, zones[1] with { HighVelocity = 1.01m }),
            zones.SetItem(0, zones[0] with { HighVelocity = 0 }),
            zones.SetItem(0, zones[0] with { LowKey = 61, HighKey = 60 }),
            zones.SetItem(1, zones[1] with { LowVelocity = 1 })
        })
        {
            Assert.Throws<ArgumentException>(() => f.Set(invalid));
            Assert.Equal(snapshot, f.Document.Current);
        }
        f.Set([zones[0] with { HighKey = 60, HighVelocity = .4m }, zones[1] with { LowKey = 61, LowVelocity = 0 }]);
        Assert.Null(SamplerKeymap.Select(f.Definition, 60.99m, .4m));
        Assert.NotNull(SamplerKeymap.Select(f.Definition, 61, .4m));
        VelocityNote(f, .5m, 60.5m);
        using (var gap = Prepare(f, 48000)) Assert.Empty(gap.Notes);
        var many = SamplerKeymap.Layer(Enumerable.Range(0, 128).Select(_ => zones[0] with
        {
            Id = Id<SamplerZone>.New(), RootPitch = 60
        }));
        f.Set(many);
        foreach (var z in many) Assert.NotNull(SamplerKeymap.Select(f.Definition, 60, z.LowVelocity));
        Assert.Throws<ArgumentException>(() => SamplerKeymap.Layer(many.Add(many[0] with { Id = Id<SamplerZone>.New() })));
        Assert.Throws<ArgumentException>(() => f.Set(many.Add(many[0] with { Id = Id<SamplerZone>.New() })));
        Assert.Throws<ArgumentException>(() => SamplerKeymap.Distribute(many));
        Assert.Equal(many, f.Definition.SamplerZones);
    }

    [Theory]
    [InlineData(44100, 1)] [InlineData(44100, 2)] [InlineData(48000, 1)] [InlineData(48000, 2)]
    public async Task LayerSpecificRootsRangesLoopsAndCrossfadesMatchIsolatedSourceAndSongExport(int rate, int channels)
    {
        using var f = await Fixture.Create(48000, channels);
        var zones = await VelocityMap(f);
        f.Set([.. zones.Select((z, i) => z with
        {
            RootPitch = 48 + i * 12,
            Parameters = new SamplerPlayback(8 + i * 8, 240 - i * 8, true, 64 + i * 8, 192 - i * 8, 8 + i * 4)
                .Apply(SamplerKeymap.Source(f.Definition, z), 256).Remove("rootPitch").Remove("releaseMilliseconds")
        })]);
        var mapped = f.Definition;
        foreach (decimal velocity in new[] { .2m, .5m, 1m })
        {
            VelocityNote(f, velocity, 60.5m);
            using var plan = Prepare(f, rate, channels);
            var actual = Render(plan, 7);
            var selected = SamplerKeymap.Select(mapped, 60.5m, velocity)!;
            var resource = selected.ResourceIds.Single();
            // Compare complete execution against the existing single-source kernel, independently of map selection.
            f.Document.Edit("Isolated reference", e =>
            {
                e.ConfigurePcmSampler(f.Sound, resource, selected.Parameters["rootPitch"], 10);
                e.SetSoundParameters(f.Sound, selected.Parameters);
            });
            using (var isolated = Prepare(f, rate, channels)) Assert.Equal(actual, Render(isolated, 127));
            Assert.True(f.Document.Undo());
            Assert.Equal(mapped.SamplerZones, f.Definition.SamplerZones);
        }
        f.Document.Edit("Song", e =>
        {
            e.AddArrangementClip(f.Document.Current.State.Placements.Single().Id, new(0), 0, 2);
            var other = e.AddPattern("Independent instrument", new(38400));
            var part = e.AddPart(other, "Other", f.Document.Current.State.Sounds.First(s => s.Id != f.Sound).Id);
            e.AddNote(other, part, new(0), new(38400), 72, .15m);
            var placement = e.AddPlacement(other, new(0));
            var graph = e.CreateItemGraph(placement);
            string layout = channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
            var source = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Source, new(0, 0), layout);
            var output = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Output, new(180, 0), layout);
            GraphFixture.Connect(e, graph.GraphId, source, output);
            e.SetGraphOutput(graph.Id, output.Id);
            e.BindGraphSource(graph.Id, source.Id, placement, part);
            e.AddArrangementClip(placement, new(0), 1, 2);
            e.InitializeMixer();
            e.SetMixerStrip(null, new MixerStripSettings { LevelDb = -6 });
        });
        using var song = ArrangementPreparation.Prepare(f.Document, rate, channels, 128, TestContext.Current.CancellationToken);
        using var realtime = new RealtimeSampler(rate, channels, 128);
        using var request = realtime.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        realtime.Start();
        var expectedSong = Render(song, 128);
        var actualSong = new float[expectedSong.Length];
        for (int offset = 0; offset < actualSong.Length; offset += 128 * channels)
            realtime.Process(actualSong.AsSpan(offset, Math.Min(128 * channels, actualSong.Length - offset)));
        Assert.Equal(expectedSong, actualSong);
        if (channels == 1) return; // The existing GUI WAV/R8 format is stereo and requires stereo graph ports.
        string file = f.Directory.File("layer-song.wav");
        var export = await PatternWaveExport.ExportArrangementAsync(f.Document, file, TestContext.Current.CancellationToken);
        using var wave = WavDecoder.Decode(File.ReadAllBytes(file));
        Assert.Equal(export.Frames, wave.Frames);
        Assert.Contains(Enumerable.Range(0, wave.Frames), i => Math.Abs(wave.Sample(i, 0)) > .001f);
        using (var exportPlan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 128, TestContext.Current.CancellationToken))
        using (var exportVoice = exportPlan.CreateExecution(smoothBoundaries: true))
        {
            var packet = new float[256];
            while (exportVoice.Position < exportPlan.EndFrame)
            {
                int start = (int)exportVoice.Position;
                int count = (int)Math.Min(128, exportPlan.EndFrame - start);
                exportVoice.Process(packet.AsSpan(0, count * 2));
                for (int i = 0; i < count; i++)
                for (int c = 0; c < 2; c++)
                    Assert.InRange(Math.Abs(packet[i * 2 + c] - wave.Sample(start + i, c)), 0, 1.01 / 32768);
            }
        }
        var before = await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        f.Document.Edit("Mixer excluded", e => e.SetMixerStrip(null, new MixerStripSettings { LevelDb = -12 }));
        var after = await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        Assert.Equal(before.Wave, after.Wave);
    }

    [Fact]
    public async Task LayersRoundtripWithSelectiveReaderHistoryNoOpUnknownPreservationAndPortableSaveAs()
    {
        using var f = await Fixture.Create();
        var old = GraphFixture.Json(f.Document);
        Assert.Equal(13, old["minimumReaderMinor"]!.GetValue<int>());
        foreach (var z in old["state"]!["sounds"]![0]!["samplerZones"]!.AsArray())
        {
            Assert.Null(z!["lowVelocity"]); Assert.Null(z["highVelocity"]);
        }
        var zones = await VelocityMap(f);
        f.Set(zones);
        VelocityNote(f, .5m);
        using var plan = Prepare(f, 48000);
        var expected = Render(plan, 13);
        var snapshot = f.Document.Current;
        int history = f.Document.UndoCount;
        f.Set(zones);
        Assert.Equal(snapshot, f.Document.Current);
        Assert.Equal(history, f.Document.UndoCount);
        var json = GraphFixture.Json(f.Document);
        Assert.Equal(14, json["minimumReaderMinor"]!.GetValue<int>());
        var understated = json.DeepClone().AsObject();
        understated["minimumReaderMinor"] = 13;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(understated));
        string saved = f.Directory.File("layers.seqvium");
        ProjectPersistence.Save(f.Document, saved);
        string relocated = f.Directory.File("elsewhere/layers.seqvium");
        ProjectPersistence.Save(f.Document, relocated);
        var loaded = ProjectPersistence.Open(relocated).Document;
        try
        {
            using var reopened = GraphPreparation.Prepare(loaded, f.Attachment.Id, 48000, 2, 128,
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(expected, Render(reopened, 17));
            Assert.Equal(zones, loaded.Current.State.Sounds.Single(s => s.Id == f.Sound).SamplerZones);
            var nested = GraphFixture.Json(loaded);
            nested["state"]!["sounds"]![0]!["samplerZones"]![0]!["futureLayer"] = new JsonObject { ["mode"] = "unknown" };
            var opaque = GraphFixture.Read(nested).Document;
            try
            {
                Assert.False(SamplerPlayback.Supports(opaque.Current.State.Sounds.Single(s => s.Id == f.Sound)));
                Assert.NotNull(GraphFixture.Json(opaque)["state"]!["sounds"]![0]!["samplerZones"]![0]!["futureLayer"]);
                Assert.Throws<NotSupportedException>(() => SamplerPreparation.PreparePattern(opaque, f.Pattern, 48000));
            }
            finally { opaque.Close(); }
        }
        finally { loaded.Close(); }
        f.Set([.. zones.Select(z => z with { LowVelocity = 0, HighVelocity = 1, LowKey = (int)z.RootPitch, HighKey = (int)z.RootPitch })
            .Take(1)]);
        Assert.Equal(14, GraphFixture.Json(f.Document)["minimumReaderMinor"]!.GetValue<int>()); // Saved floor is monotonic.
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReviewedDuplicateRootImportIsAtomicAndReusesIdenticalMedia(bool cancel)
    {
        using var f = await Fixture.Create(map: false);
        var before = f.Document.Current;
        int undo = f.Document.UndoCount;
        string path = f.Directory.File("hard.wav");
        await File.WriteAllBytesAsync(path, WavFixtures.Create([0, -.2f, .1f, 0]), TestContext.Current.CancellationToken);
        string root = f.Directory.File("imports");
        int filesBefore = System.IO.Directory.GetFiles(f.Document.UnnamedMediaRoot!, "*.wav", SearchOption.AllDirectories).Length;
        using (var batch = new SamplerMapImport(f.Document, f.Sound, root))
        {
            await batch.PrepareAsync(path, 48, TestContext.Current.CancellationToken);
            await batch.PrepareAsync(path, 48, TestContext.Current.CancellationToken);
            var layers = SamplerKeymap.Layer(batch.DraftZones);
            if (cancel)
            {
                using var cts = new CancellationTokenSource(); cts.Cancel();
                Assert.Throws<OperationCanceledException>(() => batch.Accept(layers, cts.Token));
                Assert.Equal(before, f.Document.Current);
            }
            else
            {
                batch.Accept(layers, TestContext.Current.CancellationToken);
                Assert.Equal(3, f.Zones.Length);
                Assert.Equal(2, f.Definition.ResourceIds.Length);
                Assert.Equal(undo + 1, f.Document.UndoCount);
                Assert.True(f.Document.Undo()); Assert.Equal(before.State, f.Document.Current.State);
                Assert.True(f.Document.Redo());
                var ids = f.Zones.Select(z => z.Id).ToArray();
                Assert.Equal(layers.Select(z => z.Id), ids);
            }
        }
        Assert.Equal(filesBefore + (cancel ? 0 : 1), System.IO.Directory.GetFiles(f.Document.UnnamedMediaRoot!, "*.wav", SearchOption.AllDirectories).Length);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LiveLayerPublicationKeepsClockAndUnchangedVoicesAndRetiresChangedSelection(bool song)
    {
        using var f = await Fixture.Create();
        var zones = await VelocityMap(f, 2);
        f.Set([.. zones.Select(z => z with
        {
            Parameters = new SamplerPlayback(0, 256, true, 64, 224, 16).Apply(SamplerKeymap.Source(f.Definition, z), 256)
                .Remove("rootPitch").Remove("releaseMilliseconds")
        })]);
        VelocityNote(f, .2m);
        if (song) f.Document.Edit("Arrange", e => e.AddArrangementClip(f.Document.Current.State.Placements.Single().Id, new(0), 0, 3));
        using var rt = new RealtimeSampler(48000, 2, 64);
        rt.SetLooping(!song);
        async Task Publish()
        {
            using var request = song ? rt.BeginArrangementPreparation(f.Document) : rt.BeginGraphPreparation(f.Document, f.Attachment.Id);
            request.PreservePlayback = true;
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); rt.Start();
        var packet = new float[128]; rt.Process(packet);
        var status = rt.ReadStatus();
        var current = f.Definition.SamplerZones;
        f.Set(current.SetItem(1, current[1] with { RootPitch = 61 }));
        await Publish(); rt.Process(packet);
        Assert.Equal(status.Epoch, rt.ReadStatus().Epoch);
        Assert.Equal(status.Position + 64, rt.ReadStatus().Position);
        Assert.Equal(1, rt.ReadStatus().Voices);
        Assert.InRange(packet[0], .02499f, .02501f); // Original source age 64, unchanged amplitude .2.
        rt.RetireCompleted();
        current = f.Definition.SamplerZones;
        f.Set([current[0] with { HighVelocity = .1m }, current[1] with { LowVelocity = .1m }]);
        await Publish(); rt.Process(packet);
        Assert.Equal(status.Epoch, rt.ReadStatus().Epoch);
        Assert.Equal(0, rt.ReadStatus().Voices);
        Assert.All(packet, v => Assert.Equal(0, v));
    }

    [Theory]
    [InlineData("0", 0)] [InlineData("100%", 1)] [InlineData("33,5", .335)]
    public void VelocityPercentageEntryIsCanonical(string text, decimal expected)
    {
        Assert.True(SamplerVelocityText.TryRead(text, out var velocity)); Assert.Equal(expected, velocity);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AuditionChoosesActualVelocityOrExplicitZoneAndReleaseStopRemainFinite(bool explicitZone)
    {
        using var f = await Fixture.Create();
        var zones = await VelocityMap(f);
        f.Set(zones);
        foreach (decimal velocity in new[] { 0m, .33m, .66m, 1m })
        {
            int layer = explicitZone ? 0 : velocity < .33m ? 0 : velocity < .66m ? 1 : 2;
            using var rt = new RealtimeSampler(48000, 2, 64);
            using var request = explicitZone
                ? new NoteAuditionRequest(rt, f.Document, f.Sound, zones[0].Id, velocity)
                : new NoteAuditionRequest(rt, f.Document, f.Sound, 60, velocity, held: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish());
            using var pcm = ProjectMedia.Decode(f.Document, zones[layer].ResourceId);
            using var reference = SamplerPreparation.PrepareSamplerAudition(pcm,
                SamplerKeymap.Source(f.Definition, zones[layer]), 48000, 2, 60, velocity, held: true);
            using var execution = reference.CreateExecution();
            var expected = new float[128]; var actual = new float[128];
            execution.Process(expected); rt.Process(actual);
            Assert.Equal(expected, actual);
            rt.ReleaseAudition();
            for (int i = 0; i < 12; i++) rt.Process(actual);
            Assert.Equal(0, rt.ReadStatus().Voices);
            rt.Stop(); rt.Process(actual); Assert.All(actual, v => Assert.Equal(0, v));
        }
    }

    [Fact]
    public async Task ThirtyTwoLayeredNotesUseThirtyTwoVoicesAndCallbackDoesNotAllocate()
    {
        using var f = await Fixture.Create();
        var zones = await VelocityMap(f, 2);
        f.Set([.. zones.Select(z => z with
        {
            Parameters = new SamplerPlayback(0, 256, true, 64, 224, 16).Apply(SamplerKeymap.Source(f.Definition, z), 256)
                .Remove("rootPitch").Remove("releaseMilliseconds")
        })]);
        f.Document.Edit("Chord", e => e.SetPartNotes(f.Pattern, f.Part,
            [.. Enumerable.Range(0, 32).Select(i => new NoteEvent(Id<NoteEvent>.New(), new(0), new(38400), 60, i % 2 == 0 ? .2m : .8m))]));
        using var plan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, 48000, 2, 128, 32,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(32, plan.Notes.Length);
        Assert.Equal(4096, plan.DecodedBytes); // Two shared 256-frame stereo arrays, independent of note count.
        Assert.Equal(16, plan.Notes.Count(n => n.SamplerResource == zones[0].ResourceId));
        Assert.Equal(16, plan.Notes.Count(n => n.SamplerResource == zones[1].ResourceId));
        using var voice = plan.CreateExecution();
        var packet = new float[128];
        for (int i = 0; i < 3; i++) voice.Process(packet);
        Assert.Equal(32, voice.ActiveVoices);
        long before = GC.GetAllocatedBytesForCurrentThread();
        voice.Process(packet);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        f.Notes([.. Enumerable.Repeat(60m, 33)]);
        Assert.Throws<GraphPreparationException>(() => Prepare(f, 48000));
    }

    [Theory]
    [InlineData("stale")] [InlineData("closed")] [InlineData("missing")] [InlineData("cancel-reuse")]
    public async Task ReviewedLayersRejectStaleClosedMissingAndCancelledReusedSourceWithoutAcceptance(string failure)
    {
        using var f = await Fixture.Create(map: false);
        string path = f.Directory.File("candidate.wav");
        await File.WriteAllBytesAsync(path, WavFixtures.Create([0, .1f, -.1f, 0]), TestContext.Current.CancellationToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var import = new SamplerMapImport(f.Document, f.Sound);
        await import.PrepareAsync(path, 48, cancellation.Token);
        await import.PrepareAsync(path, 48, TestContext.Current.CancellationToken);
        var layers = SamplerKeymap.Layer(import.DraftZones);
        if (failure == "stale")
        {
            f.Document.Edit("Intervening edit", e => e.RenameDocument("Changed"));
            Assert.True(f.Document.Undo()); // ABA still invalidates generation.
        }
        if (failure == "missing")
        {
            var resource = f.Document.Current.State.Resources.Single(r => r.Id == f.Definition.ResourceIds.Single());
            File.Delete(ProjectMedia.PathFor(f.Document.UnnamedMediaRoot!, resource.ManagedLocator!));
        }
        if (failure == "cancel-reuse") cancellation.Cancel();
        if (failure == "closed") f.Document.Close();
        var snapshot = f.Document.Current;
        if (failure == "cancel-reuse")
            Assert.Throws<OperationCanceledException>(() => import.Accept(TestContext.Current.CancellationToken));
        else if (failure == "missing")
            Assert.ThrowsAny<IOException>(() => import.Accept(layers, TestContext.Current.CancellationToken));
        else Assert.ThrowsAny<InvalidOperationException>(() => import.Accept(layers, TestContext.Current.CancellationToken));
        Assert.Equal(snapshot, f.Document.Current);
    }

    [Theory]
    [InlineData("")] [InlineData("-1")] [InlineData("100.01")] [InlineData("NaN")]
    public void InvalidVelocityPercentageCannotEnterMusic(string text) => Assert.False(SamplerVelocityText.TryRead(text, out _));
}
