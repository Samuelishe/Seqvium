// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class SamplerEditorTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal Id<SoundDefinition> Sound;
        internal Id<Pattern> Pattern;
        internal Id<MusicalPart> Part;
        internal GraphAttachment Attachment = null!;
        internal NodeEditorTarget Target => new(Document.LifecycleId, Attachment.Id, Attachment.GraphId,
            Document.Current.State.GraphAttachments.Single().Sources.Single().NodeId);
        internal SoundDefinition Definition => Document.Current.State.Sounds.Single(s => s.Id == Sound);
        internal static async Task<Fixture> Create(int rate = 48000, int channels = 1, decimal pitch = 60,
            decimal release = 1, bool graph = false)
        {
            var f = new Fixture();
            var accepted = await WavFixtures.Import(f.Document, f.Directory,
                Enumerable.Range(0, 256).SelectMany(i => channels == 1 ? new[] { i / 512f } : new[] { i / 512f, -i / 1024f }).ToArray(),
                rate, channels, release: release);
            f.Sound = accepted.SoundId;
            f.Document.Edit("Sampler music", e =>
            {
                f.Pattern = e.AddPattern("Sustain", new(38400)); // 20 ms at 120 BPM
                f.Part = e.AddPart(f.Pattern, "Sampler", f.Sound);
                e.AddNote(f.Pattern, f.Part, new(0), new(38400), pitch, .5m);
                if (!graph) return;
                var placement = e.AddPlacement(f.Pattern, new(0));
                f.Attachment = e.CreateItemGraph(placement);
                var source = e.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Source, new(0, 0), GraphBuiltIns.Stereo);
                var output = e.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Output, new(160, 0), GraphBuiltIns.Stereo);
                e.BindGraphSource(f.Attachment.Id, source.Id, placement, f.Part);
                GraphFixture.Connect(e, f.Attachment.GraphId, source, output);
                e.SetGraphOutput(f.Attachment.Id, output.Id);
            });
            return f;
        }
        internal void Set(SamplerPlayback p) => Document.Edit("Sample range", e =>
            e.SetSoundParameters(Sound, p.Apply(Definition, 256)));
        public void Dispose() { if (!Document.IsClosed) Document.Close(); Directory.Dispose(); }
    }

    public static IEnumerable<object[]> Formats => from source in new[] { 44100, 48000 }
        from rate in new[] { 44100, 48000 } from channels in new[] { 1, 2 }
        from pitch in new[] { 0m, 48m, 60m, 72m, 127m } select new object[] { source, rate, channels, pitch };

    [Theory, MemberData(nameof(Formats))]
    public async Task LoopHasIndependentSourceFramePitchSeamAndReleaseOracle(int sourceRate, int rate, int channels, decimal pitch)
    {
        using var f = await Fixture.Create(sourceRate, channels, pitch);
        f.Set(new(16, 224, true, 64, 192));
        using var plan = SamplerPreparation.PreparePattern(f.Document, f.Pattern, rate, channels);
        var pcm = Render(plan, [1, 7, 31]);
        Assert.Equal(pcm, Render(plan, [127, 3]));
        double step = (double)sourceRate / rate * Math.Pow(2, (double)(pitch - 60) / 12);
        long off = rate / 50;
        int release = (int)decimal.Round(rate / 1000m, 0, MidpointRounding.AwayFromZero);
        double fade = Math.Min(64, Math.Min(sourceRate / 1000, 32));
        for (int age = 0; age < off + release; age++)
        {
            double unwrapped = 16 + age * step;
            double cursor = unwrapped < 192 ? unwrapped : 64 + (unwrapped - 64) % 128;
            int index = (int)cursor;
            double value = (index * (1 - (cursor - index)) + (index + 1 < 192 ? index + 1 : 0) * (cursor - index)) / 512;
            double envelope = age < off ? 1 : 1 - (age - off) / (double)release;
            if (cursor >= 64)
            {
                envelope *= Math.Clamp((192 - cursor - 1) / fade, 0, 1);
                if (unwrapped >= 192) envelope = (age < off ? 1 : 1 - (age - off) / (double)release) *
                    Math.Min(Math.Clamp((192 - cursor - 1) / fade, 0, 1), Math.Clamp((cursor - 64) / fade, 0, 1));
            }
            Near(value * .5 * envelope, pcm[age * channels]);
            if (channels == 2) Near(-value * .25 * envelope, pcm[age * channels + 1]);
        }
        Assert.True(pcm.All(float.IsFinite));
    }

    [Theory]
    [InlineData(60, 1)] [InlineData(48, .5)] [InlineData(72, 2)]
    public async Task RangeExcludesNeighboursAndInterpolatesItsLastFrameToZero(int pitch, double step)
    {
        using var f = await Fixture.Create(pitch: pitch);
        f.Set(new(17, 23, false, 0, 0));
        using var plan = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1);
        var pcm = Render(plan, [1, 3]);
        for (int age = 0; age < pcm.Length; age++)
        {
            double cursor = 17 + age * step;
            double expected = cursor >= 23 ? 0 : cursor < 22 ? cursor / 1024 : 22 / 1024d * (23 - cursor);
            Near(expected, pcm[age]);
        }
    }

    [Theory]
    [InlineData(0, 0, false, 0, 0)] [InlineData(30, 10, false, 0, 0)]
    [InlineData(-1, 256, false, 0, 0)] [InlineData(0, 257, false, 0, 0)]
    [InlineData(0, 256, true, 0, 0)] [InlineData(0, 256, true, 3, 5)]
    [InlineData(8, 128, true, 0, 64)] [InlineData(0, 128, true, 32, 129)]
    [InlineData(0, 128, true, 64, 32)]
    public async Task InvalidRangesRefuseWithoutHistory(long start, long end, bool loop, long loopStart, long loopEnd)
    {
        using var f = await Fixture.Create(); var before = f.Document.Current;
        Assert.Throws<ArgumentException>(() => f.Set(new(start, end, loop, loopStart, loopEnd)));
        Assert.Equal(before, f.Document.Current);
    }

    [Fact]
    public async Task DisabledLoopIsExactlyLegacyPcmAndSharedResourceDefinitionsRemainIndependent()
    {
        using var f = await Fixture.Create();
        using var old = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1);
        var expected = Render(old, [13]);
        f.Set(new(0, 256, false, 32, 128));
        using var disabled = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1);
        Assert.Equal(expected, Render(disabled, [7]));
        Id<SoundDefinition> other = default;
        f.Document.Edit("Independent sound same media", e =>
        { other = e.AddSound("Independent", PcmSampler.Algorithm, f.Definition.ResourceIds); e.ConfigurePcmSampler(other, f.Definition.ResourceIds[0], 60, 1); });
        f.Set(new(16, 224, true, 64, 192));
        Assert.False(SamplerPlayback.HasIntent(f.Document.Current.State.Sounds.Single(s => s.Id == other)));
        Assert.True(f.Document.Undo()); Assert.False(SamplerPlayback.Read(f.Definition, 256).Loop);
        Assert.True(f.Document.Redo()); Assert.True(SamplerPlayback.Read(f.Definition, 256).Loop);
    }

    [Theory]
    [InlineData(0)] [InlineData(16)]
    public async Task PolyphonyRetiresEachOccurrenceAndCapacityUsesLoopLifetimeRatherThanSourceEof(long crossfade)
    {
        using var f = await Fixture.Create(); f.Set(new(0, 256, true, 32, 224, crossfade));
        f.Document.Edit("Overlap", e => e.AddNote(f.Pattern, f.Part, new(19200), new(19200), 72, .25m));
        Assert.Throws<NotSupportedException>(() => SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1, voiceCapacity: 1));
        using var poly = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1, voiceCapacity: 2);
        using var execution = poly.CreateExecution();
        var first = new float[500]; execution.Process(first); Assert.Equal(2, execution.ActiveVoices);
        var rest = new float[(int)(poly.EndFrame - execution.Position)]; execution.Process(rest); Assert.Equal(0, execution.ActiveVoices);
    }

    [Theory]
    [InlineData(44100, 0)] [InlineData(48000, 0)] [InlineData(44100, 16)] [InlineData(48000, 16)]
    public async Task RealtimeOfflinePacketParityStopPanicAndWarmCallbackAllocation(int rate, long crossfade)
    {
        using var f = await Fixture.Create(graph: true); f.Set(new(16, 224, true, 64, 192, crossfade));
        using var expectedPlan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 128, cancellationToken: TestContext.Current.CancellationToken);
        var expected = Render(expectedPlan, [128]);
        using var realtime = new RealtimeSampler(rate, 2, 128);
        var plan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 128, cancellationToken: TestContext.Current.CancellationToken);
        var authority = realtime.InvalidatePreparation();
        Assert.Equal(SamplerPublication.Accepted, realtime.Publish(plan, authority, true, false, lifetimeCancellation: TestContext.Current.CancellationToken));
        realtime.Start(); var actual = new float[expected.Length];
        for (int offset = 0; offset < actual.Length;)
        { int count = Math.Min(62, actual.Length - offset); Assert.True(realtime.Process(actual.AsSpan(offset, count))); offset += count; }
        Assert.Equal(expected, actual); Assert.Equal(0, realtime.ReadStatus().Voices);
        realtime.SetLooping(true); realtime.Start(); var packet = new float[256];
        for (int i = 0; i < 100; i++) realtime.Process(packet);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) realtime.Process(packet);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        realtime.Stop(); realtime.Process(packet); Assert.All(packet, value => Assert.Equal(0, value)); Assert.Equal(0, realtime.ReadStatus().Voices);
        realtime.Start(); realtime.Process(packet); Assert.True(realtime.ReadStatus().Playing);
        realtime.Panic(); realtime.Process(packet); Assert.All(packet, value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(0)] [InlineData(16)]
    public async Task HeldPreviewUsesReleaseAndCannotRestartOrOutliveFiniteSafetyGate(long crossfade)
    {
        using var f = await Fixture.Create(release: 3); f.Set(new(0, 256, true, 32, 224, crossfade));
        using var pcm = ProjectMedia.Decode(f.Document, f.Definition.ResourceIds[0]);
        var drySound = f.Definition with { Parameters = f.Definition.Parameters.RemoveRange(SamplerPlayback.Keys) };
        using var natural = SamplerPreparation.PrepareSamplerAudition(pcm, drySound, 48000, 1, 60, 1, held: true);
        Assert.Equal(256, natural.EndFrame);
        Assert.All(natural.Events, e => Assert.InRange(e.Frame, 0, natural.EndFrame));
        Assert.Equal(Enumerable.Range(0, 256).Select(i => i / 512f), Render(natural, [7]));
        using var realtime = new RealtimeSampler(48000, 1, 128);
        var plan = SamplerPreparation.PrepareSamplerAudition(pcm, f.Definition, 48000, 1, 60, 1, held: true);
        Assert.Equal(480144, plan.EndFrame);
        long authority = realtime.InvalidatePreparation(); Assert.Equal(SamplerPublication.Accepted, realtime.Publish(plan, authority, true, false, transient: true, lifetimeCancellation: TestContext.Current.CancellationToken));
        realtime.Start(); var packet = new float[128]; realtime.Process(packet); Assert.Equal(1, realtime.ReadStatus().Voices);
        realtime.ReleaseAudition(); realtime.Process(packet); realtime.Process(packet);
        Assert.Equal(0, realtime.ReadStatus().Voices); Assert.False(realtime.ReadStatus().Playing);
        realtime.Process(packet); Assert.All(packet, v => Assert.Equal(0, v));
        realtime.RetireCompleted(); Assert.Equal(0, realtime.LivePreparedStates);
        var second = SamplerPreparation.PrepareSamplerAudition(pcm, f.Definition, 48000, 1, 60, 1, held: true);
        authority = realtime.InvalidatePreparation();
        Assert.Equal(SamplerPublication.Accepted, realtime.Publish(second, authority, true, false, transient: true,
            lifetimeCancellation: TestContext.Current.CancellationToken));
        realtime.Start(); realtime.Process(packet); realtime.ReleaseAudition();
        long allocated = GC.GetAllocatedBytesForCurrentThread(); realtime.Process(packet); realtime.Process(packet);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - allocated);
        realtime.RetireCompleted(); Assert.Equal(0, realtime.LivePreparedStates);
    }

    [Theory]
    [InlineData(0)] [InlineData(16)]
    public async Task LiveLoopEditRetiresChangedVoiceWithoutRetriggerAndKeepsPatternClock(long crossfade)
    {
        using var f = await Fixture.Create(graph: true); f.Set(new(0, 256, true, 32, 224, crossfade));
        using var realtime = new RealtimeSampler(48000, 2, 64); realtime.SetLooping(true);
        async Task Publish()
        {
            using var request = realtime.BeginGraphPreparation(f.Document, f.Attachment.Id);
            request.PreservePlayback = true;
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); realtime.Start(); var packet = new float[128]; realtime.Process(packet);
        var before = realtime.ReadStatus(); Assert.Equal(1, before.Voices);
        // The positive case changes only width while its note is held.
        f.Set(crossfade == 0 ? new(16, 224, true, 64, 192) : new(0, 256, true, 32, 224, 24)); await Publish();
        realtime.Process(packet); var after = realtime.ReadStatus();
        Assert.Equal(before.Epoch, after.Epoch); Assert.Equal(before.Position + 64, after.Position);
        Assert.Equal(0, after.Voices); Assert.All(packet, v => Assert.Equal(0, v));
        realtime.RetireCompleted();
        for (int i = 0; i < 14; i++) realtime.Process(packet);
        Assert.True(realtime.ReadStatus().Playing); Assert.Equal(1, realtime.ReadStatus().Voices);
        using var stale = realtime.BeginGraphPreparation(f.Document, f.Attachment.Id);
        await stale.PrepareAsync(TestContext.Current.CancellationToken);
        f.Set(new(0, 256, false, 0, 0));
        Assert.Equal(SamplerPublication.Stale, stale.Publish(TestContext.Current.CancellationToken));
        realtime.TerminateExecution(); Assert.Equal(0, realtime.ReadStatus().Voices);
    }

    [Theory]
    [InlineData(0)] [InlineData(16)]
    public async Task LiveRangeEditKeepsUnrelatedLoopVoiceAndItsExactPcmAge(long crossfade)
    {
        using var f = await Fixture.Create(graph: true); f.Set(new(0, 256, true, 32, 224, crossfade));
        f.Document.Edit("Independent second sound on same PCM", e =>
        {
            var other = e.AddSound("Other", PcmSampler.Algorithm, f.Definition.ResourceIds);
            e.ConfigurePcmSampler(other, f.Definition.ResourceIds[0], 60, 1);
            var definition = f.Definition with { Id = other };
            e.SetSoundParameters(other, new SamplerPlayback(8, 240, true, 48, 208, crossfade).Apply(definition, 256));
            var part = e.AddPart(f.Pattern, "Other", other);
            e.AddNote(f.Pattern, part, new(0), new(38400), 48, .3m);
            var graph = f.Document.Current.State.Graphs.Single(); var source = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Source);
            var output = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Output);
            var second = e.AddGraphNode(graph.Id, GraphBuiltIns.Source, new(0, 120), GraphBuiltIns.Stereo);
            var mix = e.AddGraphNode(graph.Id, GraphBuiltIns.Mix, new(100, 50), GraphBuiltIns.Stereo);
            e.DisconnectGraphPorts(graph.Id, graph.Connections.Single().Id);
            e.BindGraphSource(f.Attachment.Id, second.Id, f.Document.Current.State.Placements.Single().Id, part);
            GraphFixture.Connect(e, graph.Id, source, mix); GraphFixture.Connect(e, graph.Id, second, mix, 1);
            GraphFixture.Connect(e, graph.Id, mix, output); e.SetContextMix(f.Attachment.ContextId, true);
        });
        var oracle = ProjectDocument.Reopen(f.Document.Current); oracle.MediaRoots.AddRange(f.Document.MediaRoots);
        oracle.Edit("Only unrelated voice", e => e.SetPartNotes(f.Pattern, f.Part, []));
        using var reference = GraphPreparation.Prepare(oracle, f.Attachment.Id, 48000, 2, 64,
            cancellationToken: TestContext.Current.CancellationToken);
        var expected = Render(reference, [64]);
        using var realtime = new RealtimeSampler(48000, 2, 64); realtime.SetLooping(true);
        async Task Publish()
        {
            using var request = realtime.BeginGraphPreparation(f.Document, f.Attachment.Id);
            request.PreservePlayback = true; await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(); realtime.Start(); var packet = new float[128]; realtime.Process(packet);
        Assert.Equal(2, realtime.ReadStatus().Voices); long epoch = realtime.ReadStatus().Epoch;
        f.Set(new(16, 224, true, 64, 192, crossfade == 0 ? 0 : 24)); await Publish(); realtime.Process(packet);
        Assert.Equal(epoch, realtime.ReadStatus().Epoch); Assert.Equal(1, realtime.ReadStatus().Voices);
        Assert.Equal(expected.Skip(128).Take(128), packet); oracle.Close();
    }

    [Theory]
    [InlineData(44100, 0)] [InlineData(48000, 0)] [InlineData(44100, 16)] [InlineData(48000, 16)]
    public async Task SongSeekReconstructsWrappedCursorAndReleaseWithoutStaleVoices(int rate, long crossfade)
    {
        using var f = await Fixture.Create(graph: true); f.Set(new(16, 224, true, 64, 192, crossfade));
        f.Document.Edit("Song", e => e.AddArrangementClip(f.Document.Current.State.Placements.Single().Id, new(0)));
        using var plan = ArrangementPreparation.Prepare(f.Document, rate, 2, 128, TestContext.Current.CancellationToken);
        var reference = Render(plan, [128]);
        using var execution = plan.CreateExecution(); execution.Seek(rate / 100);
        var seek = new float[256]; execution.Process(seek);
        Assert.Equal(reference.Skip(rate / 100 * 2).Take(256), seek);
        long off = rate / 50;
        execution.Seek(off + 3); execution.Process(new float[2]); Assert.Equal(1, execution.ActiveVoices);
        execution.Seek(plan.EndFrame); Assert.Equal(0, execution.ActiveVoices);
        using var realtime = new RealtimeSampler(rate, 2, 128);
        using var request = realtime.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        realtime.Start(); realtime.Process(seek); realtime.SeekSong(rate / 100); realtime.Process(seek);
        for (int i = realtime.GraphTransitionFrames; i < 128; i++)
        { Assert.Equal(reference[(rate / 100 + i) * 2], seek[i * 2]); Assert.Equal(reference[(rate / 100 + i) * 2 + 1], seek[i * 2 + 1]); }
        realtime.Stop(); realtime.Process(seek); Assert.Equal(0, realtime.ReadStatus().Voices);
    }

    [Theory]
    [InlineData("partial")] [InlineData("fractional")] [InlineData("unknown")]
    public async Task UnsafeOrUnknownSoundParametersRemainPreservedAndUnexecutable(string kind)
    {
        using var f = await Fixture.Create(); f.Set(new(0, 256, true, 32, 224));
        f.Document.Edit("Unknown state", e => e.SetSoundParameters(f.Sound, kind switch
        {
            "partial" => f.Definition.Parameters.Remove("loopEnd"),
            "fractional" => f.Definition.Parameters.SetItem("sampleStart", .5m),
            _ => f.Definition.Parameters.SetItem("futureLoopMode", 2)
        }));
        Assert.False(BuiltInSound.Supports(f.Definition));
        Assert.Throws<NotSupportedException>(() => SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000));
        var reopened = GraphFixture.Reopen(f.Document).Document;
        Assert.Equal(f.Definition.Parameters.OrderBy(p => p.Key), reopened.Current.State.Sounds.Single().Parameters.OrderBy(p => p.Key));
        reopened.Close();
    }

    [Theory]
    [InlineData(0)] [InlineData(16)]
    public async Task ReplacementResetsRangeAndLoopAtomicallyAndUndoRestoresOriginalSourceIntent(long crossfade)
    {
        using var f = await Fixture.Create(); f.Set(new(16, 224, true, 64, 192, crossfade == 0 ? 0 : 24));
        var before = f.Document.Current;
        using var replace = ProjectMedia.BeginImport(f.Document, "Shorter", 63, 17, f.Sound);
        using var bytes = new MemoryStream(WavFixtures.Create([.25f, -.25f]));
        await replace.PrepareAsync(bytes, TestContext.Current.CancellationToken); replace.Accept(TestContext.Current.CancellationToken);
        Assert.False(SamplerPlayback.HasIntent(f.Definition));
        Assert.Equal(63, f.Definition.Parameters["rootPitch"]); Assert.Equal(17, f.Definition.Parameters["releaseMilliseconds"]);
        Assert.True(f.Document.Undo()); Assert.Same(before, f.Document.Current);
        Assert.True(f.Document.Redo()); Assert.Equal(new SamplerPlayback(0, 2, false, 0, 0), SamplerPlayback.Read(f.Definition, 2));
    }

    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Assert.True(Joined);
    }

    [Fact]
    public async Task WorkflowEditsSharedSoundOnceRejectsStaleAndInvalidAndJoinsPreviewOnEditAndClose()
    {
        using var f = await Fixture.Create(graph: true);
        RealtimeSampler? output = null; Borrower? borrower = null;
        await using var workflow = new DesktopWorkflow(f.Document, () =>
        { output = new(48000, 2, 128); borrower = new(); return new(borrower, output, "Controlled", () => null); }, f.Directory.File("owned"));
        var target = f.Target; long generation = workflow.State.Generation;
        Assert.True(await workflow.SetSamplerPlaybackAsync(target, generation, new(16, 224, true, 64, 192)));
        Assert.False(await workflow.SetSamplerPlaybackAsync(target, generation, new(0, 256, false, 0, 0)));
        var before = workflow.State.Snapshot;
        Assert.False(await workflow.SetSamplerPlaybackAsync(target, workflow.State.Generation, new(0, 257, false, 0, 0)));
        Assert.Same(before, workflow.State.Snapshot);
        Assert.True(await workflow.HoldSamplerAsync(target, workflow.State.Generation));
        output!.Process(new float[256]); Assert.Equal(1, output.ReadStatus().Voices);
        Assert.True(await workflow.ReleaseSamplerAsync()); output.Process(new float[256]); Assert.Equal(0, output.ReadStatus().Voices);
        Assert.Same(before, workflow.State.Snapshot);
        Assert.True(await workflow.HoldSamplerAsync(target, workflow.State.Generation));
        Assert.True(borrower is not null); output!.Process(new float[256]);
        Assert.True(await workflow.SetSamplerPlaybackAsync(target, workflow.State.Generation, new(0, 256, false, 0, 0)));
        Assert.True(borrower!.Joined); Assert.True(output.IsTerminated);
        Assert.True(await workflow.UndoAsync()); Assert.Equal(before.State.Sounds, workflow.State.Snapshot.State.Sounds);
        Assert.True(await workflow.HoldSamplerAsync(target, workflow.State.Generation));
        await workflow.DisposeAsync(); Assert.True(borrower!.Joined); Assert.True(output.IsTerminated);
    }

    [Fact]
    public async Task ReaderGatePortableSaveUnknownAndInvalidIntentArePreservedOrRefused()
    {
        using var f = await Fixture.Create();
        Assert.Equal(0, GraphFixture.Json(f.Document)["minimumReaderMinor"]!.GetValue<int>());
        f.Set(new(16, 224, true, 64, 192));
        var json = GraphFixture.Json(f.Document); Assert.Equal(9, json["minimumReaderMinor"]!.GetValue<int>());
        json["minimumReaderMinor"] = 8; Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        string path = f.Directory.File("portable.seqvium"); ProjectPersistence.Save(f.Document, path);
        using var other = new TemporaryDirectory();
        string moved = other.File("moved.seqvium"); File.Copy(path, moved);
        CopyDirectory(path + ".media", moved + ".media");
        var loaded = ProjectPersistence.Open(moved).Document;
        try
        {
            using var plan = SamplerPreparation.PreparePattern(loaded, f.Pattern, 48000, 1);
            Assert.Contains(Render(plan, [9]).Skip(512), v => v != 0);
            var bad = GraphFixture.Json(loaded);
            bad["state"]!["sounds"]![0]!["parameters"]!["loopEnabled"] = 2;
            var degraded = GraphFixture.Read(bad).Document;
            Assert.False(BuiltInSound.Supports(degraded.Current.State.Sounds[0]));
            Assert.Throws<NotSupportedException>(() => SamplerPreparation.PreparePattern(degraded, f.Pattern, 48000));
            Assert.Equal(2, GraphFixture.Json(degraded)["state"]!["sounds"]![0]!["parameters"]!["loopEnabled"]!.GetValue<int>());
            degraded.Close();
            bad["state"]!["sounds"]![0]!["parameters"]!["loopEnabled"] = 1;
            bad["state"]!["sounds"]![0]!["parameters"]!["sampleEnd"] = 257;
            var outOfRange = GraphFixture.Read(bad).Document;
            outOfRange.MediaRoots.AddRange(loaded.MediaRoots);
            Assert.Throws<ArgumentException>(() => SamplerPreparation.PreparePattern(outOfRange, f.Pattern, 48000));
            outOfRange.Close();
        }
        finally { loaded.Close(); }
    }

    [Theory]
    [InlineData(0)] [InlineData(16)]
    public async Task R8IsFiniteThroughNoteOffReleaseAndExcludesMixerWhileSongUsesSameVoice(long crossfade)
    {
        using var f = await Fixture.Create(graph: true, release: 10); f.Set(new(16, 224, true, 64, 192, crossfade == 0 ? 0 : 24));
        var dry = await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1440, dry.Frames);
        using var decoded = WavDecoder.Decode(dry.Wave);
        Assert.Contains(Enumerable.Range(400, 500), i => Math.Abs(decoded.Sample(i, 0)) > .01);
        Assert.Equal(0, decoded.Sample(decoded.Frames - 1, 0));
        f.Document.Edit("Song and Mixer", e =>
        {
            e.AddArrangementClip(f.Document.Current.State.Placements.Single().Id, new(0), 0);
            e.InitializeMixer();
        });
        var channel = f.Document.Current.State.Mixer!.Channels.Single();
        f.Document.Edit("Serial Delay and Reverb", e => e.SetMixerStrip(channel.Id, channel.Settings with
        {
            Delay = new() { Enabled = true, TimeMs = 40, Feedback = .2m, Mix = .25m },
            Reverb = new() { Enabled = true, DecaySeconds = .4m, Mix = .2m }
        }));
        var sample = await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        Assert.Equal(dry.Wave, sample.Wave);
        string path = f.Directory.File("song.wav");
        var export = await PatternWaveExport.ExportArrangementAsync(f.Document, path, TestContext.Current.CancellationToken);
        Assert.InRange(export.Frames, 1441, 48000 * 10);
        using var song = WavDecoder.Decode(File.ReadAllBytes(path));
        Assert.Contains(Enumerable.Range(1440, song.Frames - 1440), i => Math.Abs(song.Sample(i, 0)) > .00001);
        using var songPlan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 128, TestContext.Current.CancellationToken);
        using var execution = songPlan.CreateExecution(smoothBoundaries: true);
        var packet = new float[256]; long offset = 0;
        while (execution.Position < songPlan.EndFrame)
        {
            int count = (int)Math.Min(128, songPlan.EndFrame - execution.Position); execution.Process(packet.AsSpan(0, count * 2));
            for (int i = 0; i < count; i++) NearPcm16(packet[i * 2], song.Sample((int)offset + i, 0));
            offset += count;
        }
    }

    private static float[] Render(PreparedSampler plan, int[] packets)
    {
        using var execution = plan.CreateExecution();
        var pcm = new float[checked((int)(plan.EndFrame - plan.StartFrame) * plan.Channels)];
        int i = 0, offset = 0;
        while (execution.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(packets[i++ % packets.Length], plan.EndFrame - execution.Position);
            execution.Process(pcm.AsSpan(offset, frames * plan.Channels)); offset += frames * plan.Channels;
        }
        Assert.Equal(0, execution.ActiveVoices); return pcm;
    }
    private static void NearPcm16(float expected, float actual) => Assert.InRange(Math.Abs(expected - actual), 0, 1f / 32768);
    private static void Near(double expected, float actual) => Assert.InRange(Math.Abs(expected - actual), 0, .000002);
    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (string directory in Directory.GetDirectories(source)) CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }
}
