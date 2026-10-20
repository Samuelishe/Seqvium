// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class SamplerKeymapTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal Id<SoundDefinition> Sound;
        internal Id<Pattern> Pattern;
        internal Id<MusicalPart> Part;
        internal GraphAttachment Attachment = null!;
        internal SoundDefinition Definition => Document.Current.State.Sounds.Single(s => s.Id == Sound);
        internal SamplerZone[] Zones => SamplerKeymap.Zones(Definition).OrderBy(z => z.LowKey).ToArray();

        internal static async Task<Fixture> Create(int rate = 48000, int channels = 2, bool map = true)
        {
            var f = new Fixture();
            var first = await WavFixtures.Import(f.Document, f.Directory,
                Enumerable.Range(0, 256)
                    .SelectMany(i => Enumerable.Range(0, channels).Select(c => (c == 0 ? 1 : -.5f) * i / 512f))
                    .ToArray(),
                rate, channels, root: 48, release: 10);
            var second = await WavFixtures.Import(f.Document, f.Directory,
                Enumerable.Range(0, 256)
                    .SelectMany(i => Enumerable.Range(0, channels).Select(c => (c == 0 ? -1 : .25f) * i / 512f))
                    .ToArray(),
                rate, channels, root: 72, release: 10);
            f.Sound = first.SoundId;
            f.Document.Edit("Map and music", e =>
            {
                if (map)
                    e.SetSamplerZones(f.Sound, SamplerKeymap.Distribute([
                        new(Id<SamplerZone>.New(), first.ResourceId, 48, 0, 127,
                            ImmutableDictionary<string, decimal>.Empty),
                        new(Id<SamplerZone>.New(), second.ResourceId, 72, 0, 127,
                            ImmutableDictionary<string, decimal>.Empty)
                    ]));
                f.Pattern = e.AddPattern("Map test", new(38400));
                f.Part = e.AddPart(f.Pattern, "Mapped", f.Sound);
                e.AddNote(f.Pattern, f.Part, new(0), new(38400), 60, .5m);
                var placement = e.AddPlacement(f.Pattern, new(0));
                f.Attachment = e.CreateItemGraph(placement);
                var graph = f.Attachment.GraphId;
                var source = e.AddGraphNode(graph, GraphBuiltIns.Source, new(0, 0),
                    channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo);
                var output = e.AddGraphNode(graph, GraphBuiltIns.Output, new(180, 0),
                    channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo);
                GraphFixture.Connect(e, graph, source, output);
                e.SetGraphOutput(f.Attachment.Id, output.Id);
                e.BindGraphSource(f.Attachment.Id, source.Id, placement, f.Part);
            });
            return f;
        }

        internal void Notes(params decimal[] pitches) => Document.Edit("Notes", e =>
            e.SetPartNotes(Pattern, Part,
                [.. pitches.Select(p => new NoteEvent(Id<NoteEvent>.New(), new(0), new(38400), p, .1m))]));

        internal void Set(ImmutableArray<SamplerZone> zones) =>
            Document.Edit("Map", e => e.SetSamplerZones(Sound, zones));

        public void Dispose()
        {
            if (!Document.IsClosed) Document.Close();
            Directory.Dispose();
        }
    }

    [Theory]
    [InlineData(44100, 1)]
    [InlineData(44100, 2)]
    [InlineData(48000, 1)]
    [InlineData(48000, 2)]
    public async Task EveryKeyAndFractionalBoundaryUsesIndependentPitchAndSourceOracle(int rate, int channels)
    {
        using var f = await Fixture.Create(44100, channels);
        foreach (decimal pitch in Enumerable.Range(0, 128).Select(i => (decimal)i).Concat([60.999m, 61.001m]))
        {
            f.Notes(pitch);
            using var plan = Prepare(f, rate, channels);
            using var voice = plan.CreateExecution();
            var pcm = new float[32 * channels];
            voice.Process(pcm);
            double step = 44100d / rate * Math.Pow(2, (double)(pitch - (pitch < 61 ? 48 : 72)) / 12);
            for (int frame = 0; frame < 32; frame++)
            {
                double cursor = frame * step;
                if (cursor > 254) break;
                for (int c = 0; c < channels; c++)
                {
                    double slope = pitch < 61 ? c == 0 ? 1 : -.5 : c == 0 ? -1 : .25;
                    Assert.InRange(Math.Abs(pcm[frame * channels + c] - cursor / 512 * slope * .1), 0, .000002);
                }
            }
        }
    }

    [Theory]
    [InlineData(44100, 1)]
    [InlineData(48000, 2)]
    public async Task ChordsLoopsReleasePacketsRealtimeAndFiniteR8ShareTheSameKernel(int rate, int channels)
    {
        using var f = await Fixture.Create(rate, channels);
        f.Set([
            .. f.Zones.Select(z => z with
            {
                Parameters = new SamplerPlayback(16, 240, true, 64, 192, 16)
                    .Apply(SamplerKeymap.Source(f.Definition, z), 256).Remove("rootPitch").Remove("releaseMilliseconds")
            })
        ]);
        f.Notes(48, 55, 60, 61, 67, 72, 79, 84);
        using var plan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, channels, 128,
            cancellationToken: TestContext.Current.CancellationToken);
        var expected = Render(plan, 7);
        Assert.Equal(expected, Render(plan, 128));
        Assert.Contains(expected, v => Math.Abs(v) > .01);
        using var realtime = new RealtimeSampler(rate, channels, 128);
        using var request = realtime.BeginGraphPreparation(f.Document, f.Attachment.Id);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        realtime.Start();
        var actual = new float[expected.Length];
        var packet = new float[128 * channels];
        int offset = 0;
        while (offset < actual.Length)
        {
            int count = Math.Min(packet.Length, actual.Length - offset);
            realtime.Process(packet.AsSpan(0, count));
            packet.AsSpan(0, count).CopyTo(actual.AsSpan(offset));
            offset += count;
        }

        Assert.Equal(expected, actual);
        Assert.Equal(0, realtime.ReadStatus().Voices);
        realtime.Stop();
        realtime.Process(packet);
        Assert.All(packet, v => Assert.Equal(0, v));
        var resample =
            await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        Assert.InRange(resample.Frames, 1400, 1500);
        f.Document.Edit("Mixer", e =>
        {
            e.InitializeMixer();
            e.SetMixerStrip(null, new MixerStripSettings { LevelDb = -6 });
        });
        var afterMixer =
            await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        Assert.Equal(resample.Wave, afterMixer.Wave);
    }

    [Fact]
    public async Task LegacyRoundtripStaysIdenticalAndMappedMediaHistorySurvivesPortableSave()
    {
        using var f = await Fixture.Create(map: false);
        using var legacy = Prepare(f, 48000);
        var before = Render(legacy, 11);
        var json = GraphFixture.Json(f.Document);
        Assert.Null(json["state"]!["sounds"]![0]!["samplerZones"]);
        Assert.Equal(1, json["minimumReaderMinor"]!.GetValue<int>());
        var zones = SamplerKeymap.Zones(f.Definition);
        f.Set(zones);
        using var mapped = Prepare(f, 48000);
        Assert.Equal(before, Render(mapped, 17));
        Assert.Equal(13, GraphFixture.Json(f.Document)["minimumReaderMinor"]!.GetValue<int>());
        Assert.True(f.Document.Undo());
        Assert.False(SamplerKeymap.HasIntent(f.Definition));
        Assert.True(f.Document.Redo());
        string path = f.Directory.File("portable.seqvium");
        ProjectPersistence.Save(f.Document, path);
        var loaded = ProjectPersistence.Open(path).Document;
        try
        {
            using var reopened = GraphPreparation.Prepare(loaded, f.Attachment.Id, 48000, 2, 128,
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(before, Render(reopened, 23));
        }
        finally
        {
            loaded.Close();
        }

        json = GraphFixture.Json(f.Document);
        json["minimumReaderMinor"] = 12;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
    }

    [Theory]
    [InlineData("futureZone", false)]
    [InlineData("futureParameter", true)]
    public async Task UnknownZoneDataIsPreservedButNotExecutedOrOverwritten(string key, bool parameter)
    {
        using var f = await Fixture.Create();
        var json = GraphFixture.Json(f.Document);
        var zone = json["state"]!["sounds"]![0]!["samplerZones"]![0]!;
        if (parameter) zone["parameters"]![key] = 1;
        else zone[key] = new JsonObject { ["x"] = 1 };
        var loaded = GraphFixture.Read(json).Document;
        try
        {
            var sound = loaded.Current.State.Sounds.Single(s => s.Id == f.Sound);
            Assert.False(SamplerPlayback.Supports(sound));
            Assert.Throws<NotSupportedException>(() => SamplerPreparation.PreparePattern(loaded, f.Pattern, 48000));
            Assert.NotNull(
                GraphFixture.Json(loaded)["state"]!["sounds"]![0]!["samplerZones"]![0]![
                    parameter ? "parameters" : key]);
            Assert.Throws<ProjectValidationException>(() =>
                loaded.Edit("Unsafe", e => e.SetSamplerZones(f.Sound, sound.SamplerZones)));
        }
        finally
        {
            loaded.Close();
        }
    }

    [Fact]
    public async Task RangesRejectOverlapKeepManualGapsAndSourceSettingsRemainIndependent()
    {
        using var f = await Fixture.Create();
        var zones = f.Zones;
        f.Set([zones[0] with { HighKey = 59 }, zones[1] with { LowKey = 62 }]);
        f.Notes(60, 61);
        using var gap = Prepare(f, 48000);
        Assert.All(Render(gap, 127), v => Assert.Equal(0, v));
        var snapshot = f.Document.Current;
        Assert.Throws<ArgumentException>(() => f.Set([zones[0], zones[1] with { LowKey = 60 }]));
        Assert.Equal(snapshot, f.Document.Current);
        var parameters = new SamplerPlayback(16, 224, true, 64, 192, 24)
            .Apply(SamplerKeymap.Source(f.Definition, zones[0]), 256)
            .Remove("rootPitch").Remove("releaseMilliseconds");
        f.Set([zones[0] with { Parameters = parameters }, zones[1]]);
        Assert.Empty(f.Zones[1].Parameters);
        Assert.Equal(24, SamplerPlayback.Read(SamplerKeymap.Source(f.Definition, f.Zones[0]), 256).Crossfade);
        var other = f.Document.Current.State.Sounds.Single(s => s.Id != f.Sound);
        Assert.False(SamplerKeymap.HasIntent(other));
        Assert.False(SamplerPlayback.HasIntent(other));
    }

    [Fact]
    public async Task LiveUnrelatedZoneEditKeepsHeldSourceAgeAndChangedSourceCannotBeSubstituted()
    {
        using var f = await Fixture.Create();
        f.Notes(48);
        using var rt = new RealtimeSampler(48000, 2, 64);
        rt.SetLooping(true);

        async Task Publish()
        {
            using var request = rt.BeginGraphPreparation(f.Document, f.Attachment.Id);
            request.PreservePlayback = true;
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish());
        }

        await Publish();
        rt.Start();
        var packet = new float[128];
        rt.Process(packet);
        var status = rt.ReadStatus();
        f.Set([f.Zones[0], f.Zones[1] with { RootPitch = 73 }]);
        await Publish();
        rt.Process(packet);
        Assert.Equal(status.Epoch, rt.ReadStatus().Epoch);
        Assert.Equal(status.Position + 64, rt.ReadStatus().Position);
        Assert.InRange(packet[0], .01249f, .01251f); // source age 64, not a retrigger at zero
        rt.RetireCompleted();
        var zones = f.Zones;
        f.Set([zones[0] with { ResourceId = zones[1].ResourceId }, zones[1]]);
        await Publish();
        rt.Process(packet);
        Assert.Equal(0, rt.ReadStatus().Voices);
        Assert.All(packet, v => Assert.Equal(0, v));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchImportIsOneAtomicUndoAndCancelledOrStaleCandidatesCannotAccept(bool stale)
    {
        using var f = await Fixture.Create(map: false);
        string path = f.Directory.File("new.wav");
        await File.WriteAllBytesAsync(path, WavFixtures.Create([0, .2f, .1f, 0]),
            TestContext.Current.CancellationToken);
        var snapshot = f.Document.Current;
        int undo = f.Document.UndoCount;
        using (var batch = new SamplerMapImport(f.Document, f.Sound, f.Directory.File("batch")))
        {
            await batch.PrepareAsync(path, 72, TestContext.Current.CancellationToken);
            batch.Accept(TestContext.Current.CancellationToken);
            Assert.Equal(undo + 1, f.Document.UndoCount);
            Assert.Equal(2, f.Zones.Length);
            Assert.True(f.Document.Undo());
            Assert.Equal(snapshot.State, f.Document.Current.State);
            Assert.True(f.Document.Redo());
        }

        snapshot = f.Document.Current;
        using (var batch = new SamplerMapImport(f.Document, f.Sound, f.Directory.File("failed")))
        {
            await batch.PrepareAsync(path, 84, TestContext.Current.CancellationToken);
            if (stale)
            {
                f.Document.Edit("Other edit", e => e.RenameDocument("Stale"));
                Assert.Throws<InvalidOperationException>(() => batch.Accept(TestContext.Current.CancellationToken));
            }
            else
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel();
                Assert.Throws<OperationCanceledException>(() => batch.Accept(cts.Token));
                Assert.Throws<InvalidOperationException>(() => batch.Accept(TestContext.Current.CancellationToken));
                Assert.Equal(snapshot, f.Document.Current);
            }
        }

        Assert.Equal(3,
            System.IO.Directory.GetFiles(f.Document.UnnamedMediaRoot!, "*.wav", SearchOption.AllDirectories).Length);
    }

    [Theory]
    [InlineData("C4", 60)]
    [InlineData("C♯4", 61)]
    [InlineData("Db4", 61)]
    [InlineData("0", 0)]
    [InlineData("127", 127)]
    public void ExactPitchEntryUsesMusicalNames(string text, decimal expected)
    {
        Assert.True(SamplerPitchText.TryRead(text, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("C10")]
    [InlineData("H4")]
    [InlineData("")]
    [InlineData("128")]
    [InlineData("-1")]
    public void InvalidRootsAreNeverGuessed(string text) => Assert.False(SamplerPitchText.TryRead(text, out _));

    [Fact]
    public async Task TwentySixRootsDistributeWithoutGapsAndDuplicateRootsRefuse()
    {
        using var f = await Fixture.Create();
        var zones = SamplerKeymap.Distribute(Enumerable.Range(0, 26).Select(i => f.Zones[0] with
        {
            Id = Id<SamplerZone>.New(), RootPitch = 21 + i * 4
        }));
        Assert.Equal(0, zones[0].LowKey);
        Assert.Equal(127, zones[^1].HighKey);
        for (int i = 0; i < 25; i++) Assert.Equal(zones[i].HighKey + 1, zones[i + 1].LowKey);
        Assert.Throws<ArgumentException>(() =>
            SamplerKeymap.Distribute([zones[0], zones[1] with { RootPitch = zones[0].RootPitch }]));
    }

    [Fact]
    public async Task SharedThirtyTwoVoiceCeilingAndPrimedCallbacksRemainBounded()
    {
        using var f = await Fixture.Create();
        f.Set([
            .. f.Zones.Select(z => z with
            {
                Parameters = new SamplerPlayback(0, 256, true, 32, 224, 16)
                    .Apply(SamplerKeymap.Source(f.Definition, z), 256).Remove("rootPitch").Remove("releaseMilliseconds")
            })
        ]);
        f.Notes([.. Enumerable.Range(0, 32).Select(i => i % 2 == 0 ? 48m : 72m)]);
        using var plan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, 48000, 2, 64, 32,
            cancellationToken: TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution();
        var packet = new float[128];
        execution.Process(packet);
        Assert.Equal(32, execution.ActiveVoices);
        for (int i = 0; i < 3; i++) execution.Process(packet);
        long before = GC.GetAllocatedBytesForCurrentThread();
        execution.Process(packet);
        execution.Process(packet);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        f.Notes([.. Enumerable.Repeat(60m, 33)]);
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(f.Document, f.Attachment.Id, 48000, 2,
            64,
            32,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FrozenNotesDoNotChangeSourceWhenAnotherKeyStartsOrCanonicalMapChanges()
    {
        using var f = await Fixture.Create();
        f.Notes(48, 72);
        using var frozen = Prepare(f, 48000);
        var expected = Render(frozen, 9);
        var zones = f.Zones;
        f.Set([zones[0] with { HighKey = 48 }, zones[1] with { LowKey = 49 }]);
        Assert.Equal(expected, Render(frozen, 17));
        using var next = Prepare(f, 48000);
        Assert.Equal(expected, Render(next, 9));
        f.Notes(60);
        using var changed = Prepare(f, 48000);
        Assert.Contains(Render(changed, 7), v => v < -.001f);
    }

    [Fact]
    public async Task MissingMappedMediaBlocksPreparationAndReplacementResetsOnlyTheExplicitInstrument()
    {
        using var f = await Fixture.Create();
        var zones = f.Zones;
        var other = f.Document.Current.State.Sounds.Single(s => s.Id != f.Sound);
        string path = f.Directory.File("replacement.wav");
        await File.WriteAllBytesAsync(path, WavFixtures.Create([0, .1f, 0]),
            TestContext.Current.CancellationToken);
        using (var import = ProjectMedia.BeginImport(f.Document, "Replacement", target: f.Sound))
        {
            await import.PrepareAsync(path, TestContext.Current.CancellationToken);
            import.Accept(TestContext.Current.CancellationToken);
        }

        Assert.False(SamplerKeymap.HasIntent(f.Definition));
        Assert.Equal(other, f.Document.Current.State.Sounds.Single(s => s.Id == other.Id));
        Assert.True(f.Document.Undo());
        Assert.Equal(zones, f.Zones);
        var resource = f.Document.Current.State.Resources.Single(r => r.Id == zones[1].ResourceId);
        File.Delete(ProjectMedia.PathFor(f.Document.UnnamedMediaRoot!, resource.ManagedLocator!));
        Assert.Throws<GraphPreparationException>(() => Prepare(f, 48000));
    }

    private static float[] Render(PreparedSampler plan, int packet)
    {
        using var execution = plan.CreateExecution();
        var pcm = new float[checked((int)(plan.EndFrame - plan.StartFrame) * plan.Channels)];
        int offset = 0;
        while (execution.Position < plan.EndFrame)
        {
            int count = (int)Math.Min(packet, plan.EndFrame - execution.Position) * plan.Channels;
            execution.Process(pcm.AsSpan(offset, count));
            offset += count;
        }

        Assert.Equal(0, execution.ActiveVoices);
        return pcm;
    }

    private static PreparedSampler Prepare(Fixture f, int rate, int channels = 2) =>
        GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, channels, 128,
            cancellationToken: TestContext.Current.CancellationToken);
}
