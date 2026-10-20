// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class ArrangementTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal readonly List<Id<PatternPlacement>> Sources = [];
        internal readonly List<GraphAttachment> Attachments = [];
        internal readonly List<GraphNode> Gains = [];
        internal static async Task<Fixture> Create(bool longPattern = false, int sourceRate = 48000)
        {
            var f = new Fixture();
            try
            {
                for (int index = 0; index < 2; index++)
                {
                    using var import = ProjectMedia.BeginImport(f.Document, "Sound " + index, releaseMilliseconds: 0,
                        ownedMediaDirectory: f.Directory.File("media"));
                    using var bytes = new MemoryStream(WavFixtures.Create(Enumerable.Range(0, longPattern ? 48000 : 512)
                        .SelectMany(frame => new[] { index == 0 ? frame / 48000f : .2f, index == 0 ? .4f : -.1f }).ToArray(), sourceRate, 2));
                    await import.PrepareAsync(bytes, TestContext.Current.CancellationToken);
                    import.Accept((edit, media) =>
                    {
                        edit.SetSettings(new(new Tempo(137.123456m), new(7, 8)));
                        long length = longPattern ? 3840000 : index == 0 ? 9600 : 14400;
                        var pattern = edit.AddPattern("Pattern " + index, new(length));
                        var part = edit.AddPart(pattern, "Channel", media.SoundId);
                        edit.AddNote(pattern, part, new(index == 0 ? 0 : 2400), new(longPattern ? 1920000 : index == 0 ? 4800 : 6000), 60, index == 0 ? 1 : .5m);
                        var placement = edit.AddPlacement(pattern, new(3840000 + index * 3840000L));
                        f.Sources.Add(placement);
                        var attachment = edit.CreateItemGraph(placement); f.Attachments.Add(attachment);
                        var source = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                        var gain = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Gain, new(180, 0)); f.Gains.Add(gain);
                        var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(360, 0));
                        edit.SetGraphGain(attachment.GraphId, gain.Id, index == 0 ? .5m : .25m);
                        edit.BindGraphSource(attachment.Id, source.Id, placement, part);
                        GraphFixture.Connect(edit, attachment.GraphId, source, gain); GraphFixture.Connect(edit, attachment.GraphId, gain, output);
                        edit.SetGraphOutput(attachment.Id, output.Id);
                    });
                }
                return f;
            }
            catch { f.Dispose(); throw; }
        }
        public void Dispose() { if (!Document.IsClosed) Document.Close(); Directory.Dispose(); }
    }

    private static long Frame(long ticks, decimal bpm, int rate) => (long)decimal.Round(ticks * 60m * rate / (960000m * bpm), 0, MidpointRounding.AwayFromZero);

    [Theory]
    [InlineData(44100, 48000, 1)] [InlineData(44100, 44100, 2)] [InlineData(48000, 48000, 2)]
    public async Task OverlapsRepeatsGapsGraphsAndAbsoluteBoundariesMatchIndependentPcmOracle(int rate, int sourceRate, int channels)
    {
        using var f = await Fixture.Create(sourceRate: sourceRate);
        // The graph layouts are explicit; mono execution uses the same authored topology with mono ports.
        if (channels == 1)
        {
            var json = GraphFixture.Json(f.Document);
            foreach (var node in json["state"]!["graphs"]!.AsArray().SelectMany(g => g!["nodes"]!.AsArray()))
                foreach (var port in node!["ports"]!.AsArray()) port!["layout"] = GraphBuiltIns.Mono;
            using var loaded = new CloseDocument(GraphFixture.Read(json).Document);
            loaded.Document.MediaRoots.AddRange(f.Document.MediaRoots);
            Verify(loaded.Document);
        }
        else Verify(f.Document);

        void Verify(ProjectDocument document)
        {
            document.Edit("Arrange", edit =>
            {
                edit.AddArrangementClip(f.Sources[0], new(137), 0, 7);
                edit.AddArrangementClip(f.Sources[1], new(4276), 1, 5);
                edit.AddArrangementClip(f.Sources[1], new(96000), 0, 2);
            });
            using var plan = ArrangementPreparation.Prepare(document, rate, channels, 127);
            using var execution = plan.CreateExecution();
            var state = document.Current.State; decimal bpm = state.Settings.Tempo.BeatsPerMinute;
            long expectedEnd = state.ArrangementClips.Max(c => c.Position.Ticks + state.Patterns.Single(p => p.Id == state.Placements.Single(p => p.Id == c.SourcePlacementId).PatternId).Length.Ticks * c.Repeats);
            Assert.Equal(Frame(expectedEnd, bpm, rate), plan.EndFrame); Assert.Equal(0, plan.StartFrame);
            int packet = 0; int[] sizes = [1, 47, 127, 3]; var output = new float[127 * channels];
            while (execution.Position < plan.EndFrame)
            {
                int count = (int)Math.Min(sizes[packet++ % sizes.Length], plan.EndFrame - execution.Position);
                long start = execution.Position; execution.Process(output.AsSpan(0, count * channels));
                for (int i = 0; i < count; i++)
                    for (int channel = 0; channel < channels; channel++)
                    {
                        float expected = 0;
                        foreach (var clip in state.ArrangementClips)
                        {
                            int source = f.Sources.IndexOf(clip.SourcePlacementId);
                            var pattern = state.Patterns.Single(p => p.Id == state.Placements.Single(p => p.Id == clip.SourcePlacementId).PatternId);
                            var note = pattern.Parts.Single().Notes.Single();
                            for (int repeat = 0; repeat < clip.Repeats; repeat++)
                            {
                                long on = Frame(clip.Position.Ticks + repeat * pattern.Length.Ticks + note.Position.Ticks, bpm, rate);
                                long off = Frame(clip.Position.Ticks + repeat * pattern.Length.Ticks + note.Position.Ticks + note.Duration.Ticks, bpm, rate);
                                long age = start + i - on;
                                double cursor = age * (double)sourceRate / rate;
                                if (age < 0 || start + i >= off || cursor >= 512) continue;
                                double left = source == 0 ? cursor / 48000 : .2f;
                                double right = source == 0 ? .4f : -.1f;
                                double value = channels == 1 ? (left + right) / 2 : channel == 0 ? left : right;
                                expected += (float)(value * (source == 0 ? .5 : .125));
                            }
                        }
                        Assert.InRange(Math.Abs(output[i * channels + channel] - expected), 0, .000001);
                    }
            }
            Assert.Equal(0, execution.ActiveVoices);
            Assert.All(plan.Events.Where(e => e.Kind != ExecutionEventKind.Stop), e => Assert.NotNull(e.Occurrence?.ClipId));
        }
    }

    private sealed class CloseDocument(ProjectDocument document) : IDisposable
    { internal ProjectDocument Document => document; public void Dispose() => document.Close(); }

    [Fact]
    public async Task ClipHistorySharingDeletionAndLegacyFormatRemainCoherent()
    {
        using var f = await Fixture.Create();
        var legacy = GraphFixture.Json(f.Document); legacy["state"]!.AsObject().Remove("arrangementClips");
        Assert.Empty(GraphFixture.Read(legacy).Document.Current.State.ArrangementClips);
        Assert.Equal(1, legacy["minimumReaderMinor"]!.GetValue<int>());
        Id<ArrangementClip> clip = default;
        f.Document.Edit("Place", edit => clip = edit.AddArrangementClip(f.Sources[0], new(137), 3, 4));
        var placed = f.Document.Current;
        f.Document.Edit("Move and repeat", edit => edit.UpdateArrangementClip(clip, new(30000), 4, 5));
        Assert.True(f.Document.Undo()); Assert.Equal(placed, f.Document.Current);
        Assert.True(f.Document.Redo()); var moved = f.Document.Current;
        Assert.False(f.Document.Edit("Net zero", edit => { edit.UpdateArrangementClip(clip, new(0), 0, 1); edit.UpdateArrangementClip(clip, new(30000), 4, 5); }));
        Assert.Equal(moved, f.Document.Current);
        var json = GraphFixture.Json(f.Document); Assert.Equal(2, json["minimumReaderMinor"]!.GetValue<int>());
        json["state"]!["arrangementClips"]![0]!["futureOptional"] = JsonNode.Parse("{\"keep\":[1,2]}");
        using var loaded = new CloseDocument(GraphFixture.Read(json).Document);
        loaded.Document.Edit("Known move", edit => edit.UpdateArrangementClip(clip, new(64000), 1, 2));
        Assert.NotNull(GraphFixture.Json(loaded.Document)["state"]!["arrangementClips"]![0]!["futureOptional"]);
        loaded.Document.Edit("Delete source", edit => edit.DeletePlacement(f.Sources[0]));
        Assert.Empty(loaded.Document.Current.State.ArrangementClips);
        Assert.True(loaded.Document.Undo()); Assert.Single(loaded.Document.Current.State.ArrangementClips);
        json["minimumReaderMinor"] = 1;
        Assert.Contains("Arrangement requires", Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json)).Message);
    }

    [Theory]
    [InlineData(-1, 1)] [InlineData(32, 1)] [InlineData(0, 0)] [InlineData(0, 1025)]
    public async Task InvalidClipEditsPublishNothing(int lane, int repeats)
    {
        using var f = await Fixture.Create(); var before = f.Document.Current; int history = f.Document.UndoCount;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Invalid clip", edit => edit.AddArrangementClip(f.Sources[0], default, lane, repeats)));
        Assert.Equal(before, f.Document.Current); Assert.Equal(history, f.Document.UndoCount);
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Overflow", edit => edit.AddArrangementClip(f.Sources[0], new(long.MaxValue), 0)));
    }

    [Fact]
    public async Task TooManyVoicesRefuseTheWholeSongAndCancellationLeavesNoPublication()
    {
        using var f = await Fixture.Create(longPattern: true);
        f.Document.Edit("Overlap", edit => { for (int i = 0; i <= ArrangementPreparation.VoiceCapacity; i++) edit.AddArrangementClip(f.Sources[0], default, i % 32); });
        Assert.Equal(GraphPreparationReasons.Budget, Assert.Throws<GraphPreparationException>(() => ArrangementPreparation.Prepare(f.Document, 48000, 2, 256, TestContext.Current.CancellationToken)).Reason);
        using var sampler = new RealtimeSampler(48000, 2, 256);
        using var request = sampler.BeginArrangementPreparation(f.Document);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request.PrepareAsync(cancelled.Token));
        Assert.Equal(0, sampler.LivePreparedStates); Assert.Equal(0, sampler.ReservedPcmAndScratchBytes);
    }

    private static async Task Publish(Fixture f, RealtimeSampler sampler, bool preserve = false)
    {
        using var request = sampler.BeginArrangementPreparation(f.Document); request.PreservePlayback = preserve;
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SectionLabelsPreserveSongPcmAndEpochAcrossPublicationAndHistory(bool mixer)
    {
        using var f = await Fixture.Create(longPattern: true);
        Id<ArrangementSection> section = default;
        f.Document.Edit("Song", e =>
        {
            e.AddArrangementClip(f.Sources[0], default, 0, 2);
            section = e.AddArrangementSection("Intro", default, new(3840000), 0);
            if (mixer) e.InitializeMixer();
        });
        using var referencePlan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 256, TestContext.Current.CancellationToken);
        using var reference = referencePlan.CreateExecution();
        using var sampler = new RealtimeSampler(48000, 2, 256);
        await Publish(f, sampler); sampler.Start();
        var packet = new float[512]; var expected = new float[512];
        sampler.Process(packet); reference.Process(expected);
        long epoch = sampler.TransportEpoch;
        f.Document.Edit("Label only", e => e.UpdateArrangementSection(section, "Verse", default, new(3840000), 3));
        for (int edit = 0; edit < 3; edit++)
        {
            long position = sampler.RenderPosition;
            await Publish(f, sampler, true);
            sampler.Process(packet); reference.Process(expected);
            Assert.Equal(epoch, sampler.TransportEpoch);
            Assert.Equal(position + 256, sampler.RenderPosition);
            Assert.Equal(expected, packet);
            Assert.Equal(1, sampler.ActiveVoices);
            sampler.RetireCompleted();
            if (edit == 0) Assert.True(f.Document.Undo());
            if (edit == 1) Assert.True(f.Document.Redo());
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task SectionOrganizationNeedsNoPreparationInPatternOrSong(bool song, bool mixer)
    {
        using var f = await Fixture.Create(longPattern: true);
        Id<ArrangementSection> section = default;
        f.Document.Edit("Song", e =>
        {
            e.AddArrangementClip(f.Sources[0], default, 0, 2);
            if (mixer) e.InitializeMixer();
        });
        using var sampler = new RealtimeSampler(48000, 2, 256);
        sampler.SetLooping(!song);
        var owner = new AudioOwnerContext();
        GraphExecutionCoordinator coordinator = null!;
        owner.Run(() =>
        {
            coordinator = new(f.Document, sampler, f.Attachments[0].Id, owner, 8, 1, null, false);
            coordinator.SelectArrangement(song);
        });
        try
        {
            await owner.Until(() => !coordinator.ReadStatus().PreparationPending || sampler.HasPending);
            owner.Run(() => Assert.True(coordinator.Start()));
            var packet = new float[512]; sampler.Process(packet);
            owner.Run(coordinator.Progress);
            int preparations = coordinator.ReadStatus().Preparations;
            long epoch = sampler.TransportEpoch;
            var commands = new Action[]
            {
                () => f.Document.Edit("Add section", e => section = e.AddArrangementSection("Intro", default, new(3840000), 0)),
                () => f.Document.Edit("Rename/color", e => e.UpdateArrangementSection(section, "Verse", default, new(3840000), 3)),
                () => Assert.True(f.Document.Undo()),
                () => Assert.True(f.Document.Redo()),
                () => f.Document.Edit("Remove label", e => e.DeleteArrangementSection(section))
            };
            foreach (var command in commands)
            {
                long position = sampler.RenderPosition;
                owner.Run(command);
                Assert.Equal(preparations, coordinator.ReadStatus().Preparations);
                sampler.Process(packet);
                Assert.Equal(epoch, sampler.TransportEpoch);
                Assert.Equal(position + 256, sampler.RenderPosition);
                Assert.True(sampler.ReadStatus().Playing);
                Assert.Equal(f.Document.Current.Revision, sampler.ReadStatus().EquivalentCanonicalRevision);
                owner.Run(coordinator.Progress);
            }
            if (mixer)
            {
                owner.Run(() => f.Document.Edit("Mixer control", e =>
                    e.SetMixerStrip(f.Document.Current.State.Mixer!.Channels[0].Id, new() { LevelDb = -6 })));
                Assert.Equal(preparations, coordinator.ReadStatus().Preparations);
                sampler.Process(packet);
                Assert.Equal(epoch, sampler.TransportEpoch);
                Assert.Equal(f.Document.Current.Revision, sampler.ReadStatus().ExecutingRevision);
            }
        }
        finally { await owner.Join(coordinator.DisposeAsync); }
    }

    [Fact]
    public async Task SectionSwapStillPublishesMovedMusicAtTheCurrentSongClock()
    {
        using var f = await Fixture.Create();
        Id<ArrangementSection> section = default;
        f.Document.Edit("Sections and music", e =>
        {
            e.AddArrangementClip(f.Sources[0], default, 0);
            e.AddArrangementClip(f.Sources[1], new(9600), 1);
            section = e.AddArrangementSection("First", default, new(9600), 0);
            e.AddArrangementSection("Second", new(9600), new(14400), 1);
        });
        using var sampler = new RealtimeSampler(48000, 2, 127);
        await Publish(f, sampler); sampler.Start(); sampler.Process(new float[2]);
        long epoch = sampler.TransportEpoch, position = sampler.RenderPosition;
        f.Document.Edit("Swap actual music", e => e.MoveArrangementSection(section, true));
        Assert.Equal(14400, f.Document.Current.State.ArrangementClips.Single(c => c.SourcePlacementId == f.Sources[0]).Position.Ticks);
        using var plan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 127, TestContext.Current.CancellationToken);
        using var reference = plan.CreateExecution(); reference.Seek(position);
        await Publish(f, sampler, true);
        var actual = new float[254]; var expected = new float[254];
        sampler.Process(actual); reference.Process(expected);
        Assert.Equal(epoch, sampler.TransportEpoch);
        Assert.Equal(position + 127, sampler.RenderPosition);
        Assert.Equal(expected, actual);
        Assert.Contains(actual, v => v != 0);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ProductionBoundarySmoothingEndsInsideGateAndSourceEofWithNoAddedTail(bool sourceEof)
    {
        using var f = await Fixture.Create(longPattern: !sourceEof);
        if (sourceEof)
            f.Document.Edit("Keep gate beyond short source EOF", edit =>
            {
                var pattern = f.Document.Current.State.Patterns[0]; var part = pattern.Parts[0]; var note = part.Notes[0];
                edit.SetPatternLength(pattern.Id, new(3840000));
                edit.UpdateNote(pattern.Id, part.Id, note.Id, note.Position, new(1920000), note.Pitch, note.Intensity);
            });
        f.Document.Edit("Song", edit => edit.AddArrangementClip(f.Sources[0], default, 0));
        using var plan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 256, TestContext.Current.CancellationToken);
        using var raw = plan.CreateExecution(); using var smooth = plan.CreateExecution(smoothBoundaries: true);
        long end = sourceEof ? 512 : plan.Events.First(e => e.Kind == ExecutionEventKind.NoteOff).Frame;
        var original = new float[512]; var softened = new float[512];
        while (raw.Position < end - 256)
        {
            int frames = (int)Math.Min(256, end - 256 - raw.Position);
            raw.Process(original.AsSpan(0, frames * 2)); smooth.Process(softened.AsSpan(0, frames * 2));
        }
        raw.Process(original); smooth.Process(softened);
        Assert.True(Math.Abs(original[^1]) > .01f);
        Assert.Equal(0, softened[^1]);
        float largest = Enumerable.Range(1, 255).Max(i => Math.Abs(softened[i * 2 + 1] - softened[(i - 1) * 2 + 1]));
        Assert.True(largest < Math.Abs(original[^1]) / 50);
        raw.Process(original.AsSpan(0, 32)); smooth.Process(softened.AsSpan(0, 32));
        Assert.All(softened.Take(32), value => Assert.Equal(0, value));
        Assert.Equal(raw.Position, smooth.Position);
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public async Task SeekRestoresDestinationPcmAndRangeLoopIsPacketInvariantAndStopStaysSticky(int rate)
    {
        using var f = await Fixture.Create(longPattern: true);
        f.Document.Edit("Song", edit => edit.AddArrangementClip(f.Sources[0], default, 0));
        using var sampler = new RealtimeSampler(rate, 2, 256);
        await Publish(f, sampler);
        long start = rate / 10, end = start + rate / 100;
        sampler.SeekSong(start); sampler.SetSongLoop(start, end, true); sampler.Start();
        var packet = new float[512];
        sampler.Process(Span<float>.Empty); // Observe deliberate seek/start before measuring steady PCM.
        long epoch = sampler.TransportEpoch, expectedFrame = start;
        int[] sizes = [1, 127, 256, 47];
        for (int block = 0; block < 100; block++)
        {
            int count = sizes[block % sizes.Length];
            sampler.Process(packet.AsSpan(0, count * 2));
            for (int sample = 0; sample < count; sample++)
            {
                if (expectedFrame == end) expectedFrame = start;
                float expected = (float)(expectedFrame * 48000d / rate / 48000 * .5);
                Assert.InRange(Math.Abs(packet[sample * 2] - expected), 0, .000001f);
                Assert.Equal(.2f, packet[sample * 2 + 1]); expectedFrame++;
            }
            if (expectedFrame == end) expectedFrame = start;
            Assert.Equal(expectedFrame, sampler.RenderPosition); Assert.Equal(epoch, sampler.TransportEpoch);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int block = 0; block < 100; block++) sampler.Process(packet);
        Assert.Equal(allocated, GC.GetAllocatedBytesForCurrentThread());
        sampler.SetSongLoop(0, 0, false); sampler.SeekSong(rate / 5); sampler.Process(Span<float>.Empty);
        sampler.Process(packet);
        Assert.InRange(Math.Abs(packet[0] - .1f / sampler.GraphTransitionFrames), 0, .000001f);
        var stop = sampler.Stop(); sampler.SeekSong(rate / 4); sampler.Process(packet);
        Assert.Equal(stop, sampler.StopAcknowledgment); Assert.False(sampler.ReadStatus().Playing);
        Assert.All(packet, sample => Assert.Equal(0, sample));
        sampler.Start(); sampler.Process(Span<float>.Empty); sampler.Process(packet);
        Assert.InRange(Math.Abs(packet[0] - .125f), 0, .000001f);
    }

    [Fact]
    public async Task SelectionCommandsAreAtomicPreserveSpacingAndRejectStaleAuthority()
    {
        using var f = await Fixture.Create();
        f.Document.Edit("Song", edit =>
        { edit.AddArrangementClip(f.Sources[0], new(100), 0); edit.AddArrangementClip(f.Sources[1], new(20000), 2); });
        await using var workflow = new DesktopWorkflow(f.Document, mediaDirectory: f.Directory.File("media"));
        var basis = ArrangementEditBasis.From(workflow.State);
        var clips = workflow.State.Snapshot.State.ArrangementClips;
        var ids = clips.Select(c => c.Id).ToArray();
        int undo = workflow.State.UndoCount;
        Assert.True(await workflow.EditArrangementSelectionAsync(basis, ids, -50000, -10));
        var moved = workflow.State.Snapshot.State.ArrangementClips;
        Assert.Equal(0, moved[0].Position.Ticks); Assert.Equal(19900, moved[1].Position.Ticks);
        Assert.Equal(2, moved[1].Lane); Assert.Equal(undo + 1, workflow.State.UndoCount);
        Assert.False(await workflow.EditArrangementSelectionAsync(basis, ids, delete: true));
        Assert.True(await workflow.UndoAsync()); Assert.Equal(clips, workflow.State.Snapshot.State.ArrangementClips);
        Assert.True(await workflow.EditArrangementSelectionAsync(ArrangementEditBasis.From(workflow.State), ids, duplicate: true));
        var copies = workflow.State.Snapshot.State.ArrangementClips.Where(c => !ids.Contains(c.Id)).OrderBy(c => c.Position.Ticks).ToArray();
        Assert.Equal(19900, copies[1].Position.Ticks - copies[0].Position.Ticks);
        Assert.Equal(34400, copies[0].Position.Ticks);
        Assert.True(await workflow.EditArrangementSelectionAsync(ArrangementEditBasis.From(workflow.State), ids, delete: true));
        Assert.Equal(2, workflow.State.Snapshot.State.ArrangementClips.Length);
        Assert.True(await workflow.UndoAsync()); Assert.Equal(4, workflow.State.Snapshot.State.ArrangementClips.Length);
    }

    [Fact]
    public async Task LiveFuturePlacementKeepsPositionVoiceAgeEpochAndStickyStopWithNoCallbackAllocation()
    {
        using var f = await Fixture.Create(longPattern: true);
        f.Document.Edit("Initial song", edit => edit.AddArrangementClip(f.Sources[0], default, 0, 2));
        using var sampler = new RealtimeSampler(48000, 2, 256); await Publish(f, sampler); sampler.Start();
        var packet = new float[512]; Assert.True(sampler.Process(packet));
        long epoch = sampler.TransportEpoch, position = sampler.RenderPosition;
        f.Document.Edit("Future clip", edit => edit.AddArrangementClip(f.Sources[1], new(3840000), 1));
        await Publish(f, sampler, true); Assert.True(sampler.Process(packet));
        Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(position + 256, sampler.RenderPosition);
        Assert.InRange(Math.Abs(packet[0] - 256 / 48000f * .5f), 0, .000001f); Assert.True(sampler.ReadStatus().Playing);
        sampler.RetireCompleted();
        for (int i = 0; i < 20; i++) sampler.Process(packet);
        // Keep assertion infrastructure outside the callback allocation measurement.
        int processed = 0;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++) if (sampler.Process(packet)) processed++;
        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(20, processed);
        Assert.Equal(before, after);
        long stop = sampler.Stop(); sampler.Process(packet); Assert.Equal(stop, sampler.StopAcknowledgment);
        await Publish(f, sampler, true); sampler.Process(packet);
        Assert.All(packet, value => Assert.Equal(0, value)); Assert.False(sampler.ReadStatus().Playing);
        sampler.TerminateExecution(); sampler.Dispose(); Assert.Equal(0, sampler.LivePreparedStates);
    }

    [Fact]
    public async Task SongExportMatchesRealtimePcmAndPreservesDocumentAndDestinationOnCancellation()
    {
        using var f = await Fixture.Create();
        f.Document.Edit("Song", edit => { edit.AddArrangementClip(f.Sources[0], new(137), 0, 3); edit.AddArrangementClip(f.Sources[1], new(1700), 1, 2); });
        var before = f.Document.Current; int history = f.Document.UndoCount;
        string path = f.Directory.File("song.wav"); var result = await PatternWaveExport.ExportArrangementAsync(f.Document, path, TestContext.Current.CancellationToken);
        using var decoded = WavDecoder.Decode(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        using var lease = decoded.Lease();
        using var sampler = new RealtimeSampler(48000, 2, 127, smoothBoundaries: true); await Publish(f, sampler); sampler.Start();
        var packet = new float[254]; long frame = 0;
        while (frame < result.Frames)
        {
            int frames = (int)Math.Min(127, result.Frames - frame); sampler.Process(packet.AsSpan(0, frames * 2));
            for (int i = 0; i < frames * 2; i++) Assert.InRange(Math.Abs(lease.Samples[(int)(frame * 2) + i] - packet[i]), 0, 1f / 32768);
            frame += frames;
        }
        Assert.Equal(SamplerTransportState.Ended, sampler.ReadStatus().Transport); Assert.Equal(before, f.Document.Current); Assert.Equal(history, f.Document.UndoCount);
        byte[] original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PatternWaveExport.ExportArrangementAsync(f.Document, path, cancelled.Token));
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        Assert.Empty(System.IO.Directory.GetFiles(f.Directory.Path, "*.pending"));
        await Assert.ThrowsAsync<IOException>(() => PatternWaveExport.ExportArrangementAsync(f.Document, Path.Combine(f.Document.MediaRoots[0], "overwrite.wav"), TestContext.Current.CancellationToken));
    }

    private sealed class Borrower : IAudioOutputLifetime
    { public bool Joined { get; private set; } public void Close() => Joined = true; public void Dispose() => Assert.True(Joined); }

    [Theory]
    [InlineData(true, false, false, false, -.0125f)]
    [InlineData(false, true, false, false, .2f)]
    [InlineData(false, false, false, true, -.0125f)]
    [InlineData(true, false, true, false, 0f)]
    public async Task TrackMuteSoloHaveRealPcmAndSaveUndoSemantics(bool muteFirst, bool soloFirst, bool muteSecond, bool soloSecond, float expectedRight)
    {
        using var f = await Fixture.Create(longPattern: true);
        f.Document.Edit("Two tracks", edit => { edit.AddArrangementClip(f.Sources[0], default, 0); edit.AddArrangementClip(f.Sources[1], default, 1); });
        var before = f.Document.Current;
        f.Document.Edit("Track settings", edit =>
        {
            edit.SetArrangementTrack(0, "Drums", 3, muteFirst, soloFirst);
            edit.SetArrangementTrack(1, "Bass", 4, muteSecond, soloSecond);
        });
        var configured = f.Document.Current;
        using var plan = ArrangementPreparation.Prepare(f.Document, 48000, 2, 256, TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution(); var packet = new float[512];
        execution.Process(packet); execution.Process(packet);
        Assert.InRange(Math.Abs(packet[1] - expectedRight), 0, .000001f);
        Assert.Equal(Frame(3840000, 137.123456m, 48000), plan.EndFrame);
        string path = f.Directory.File("tracks.seqvium"); ProjectPersistence.Save(f.Document, path);
        using var reopened = new CloseDocument(ProjectPersistence.Open(path).Document);
        Assert.Equal(configured.State.ArrangementTracks.Select(t => (t.Id, t.Name, t.ColorIndex, t.Muted, t.Solo)),
            reopened.Document.Current.State.ArrangementTracks.Select(t => (t.Id, t.Name, t.ColorIndex, t.Muted, t.Solo)));
        Assert.True(f.Document.Undo()); Assert.Equal(before, f.Document.Current);
        Assert.True(f.Document.Redo()); Assert.Equal(configured, f.Document.Current);
    }

    [Fact]
    public async Task MutingDuringPlaybackKeepsOtherTrackVoiceAgeAndUnmuteDoesNotReplayPastOnsets()
    {
        using var f = await Fixture.Create(longPattern: true);
        f.Document.Edit("Tracks", edit => { edit.AddArrangementClip(f.Sources[0], default, 0, 2); edit.AddArrangementClip(f.Sources[1], default, 1, 2); });
        using var sampler = new RealtimeSampler(48000, 2, 256); await Publish(f, sampler); sampler.Start();
        var packet = new float[512]; sampler.Process(packet); sampler.Process(packet); long position = sampler.RenderPosition, epoch = sampler.TransportEpoch;
        f.Document.Edit("Mute Bass", edit => edit.SetArrangementTrack(1, "Bass", 1, true, false));
        await Publish(f, sampler, true); sampler.Process(packet);
        Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(position + 256, sampler.RenderPosition);
        Assert.InRange(Math.Abs(packet[0] - position / 48000f * .5f), 0, .000001f); Assert.Equal(.2f, packet[1]);
        f.Document.Edit("Unmute Bass", edit => edit.SetArrangementTrack(1, "Bass", 1, false, false));
        await Publish(f, sampler, true); sampler.RetireCompleted(); sampler.Process(packet);
        Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(.2f, packet[1]);
    }

    [Fact]
    public async Task ShorteningPastThePlayheadEndsWithoutRestartingTheSong()
    {
        using var f = await Fixture.Create(longPattern: true);
        Id<ArrangementClip> clip = default;
        f.Document.Edit("Song", edit => clip = edit.AddArrangementClip(f.Sources[0], default, 0, 2));
        using var sampler = new RealtimeSampler(48000, 2, 256); await Publish(f, sampler); sampler.Start();
        var packet = new float[512];
        long shortenedEnd = Frame(3840000, 137.123456m, 48000);
        while (sampler.RenderPosition <= shortenedEnd) sampler.Process(packet);
        f.Document.Edit("Shorten song", edit => edit.UpdateArrangementClip(clip, default, 0, 1));
        await Publish(f, sampler, true); sampler.Process(packet);
        Assert.All(packet, value => Assert.Equal(0, value)); Assert.Equal(shortenedEnd, sampler.RenderPosition);
        Assert.Equal(SamplerTransportState.Ended, sampler.ReadStatus().Transport);
    }

    [Fact]
    public async Task DesktopSongCommandsSaveOpenStaleUndoAndRackModeUseOneOwnerAndRealRenderer()
    {
        using var f = await Fixture.Create(longPattern: true);
        RealtimeSampler? sampler = null; var borrower = new Borrower();
        await using var workflow = new DesktopWorkflow(f.Document, () => new(borrower,
            sampler = new(44100, 2, 256), "Test output", () => null));
        Assert.True(await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State), f.Attachments[0].Id, default, 0, 2));
        var placed = workflow.State.Snapshot;
        var stale = ArrangementEditBasis.From(workflow.State);
        Assert.True(await workflow.UndoAsync()); Assert.True(await workflow.UndoAsync(true));
        Assert.False(await workflow.AddArrangementClipAsync(stale, f.Attachments[1].Id, default, 1)); Assert.Equal(placed, workflow.State.Snapshot);
        string save = f.Directory.File("song.seqvium"); Assert.True(await workflow.SaveAsync(save));
        var packet = new float[512];
        async Task Play()
        {
            var play = workflow.PlayAsync();
            for (int i = 0; !play.IsCompleted && i < 10000; i++)
            { await workflow.ObserveAsync(); sampler?.Process(Span<float>.Empty); await Task.Yield(); }
            Assert.True(await play.WaitAsync(TestContext.Current.CancellationToken), workflow.State.Detail);
        }
        Assert.True(await workflow.SetSongModeAsync(true)); await Play();
        sampler!.Process(packet); Assert.Equal(.2f, packet[1]); Assert.False(sampler.ReadStatus().Looping);
        Assert.True(workflow.State.SongMode); long epoch = sampler.TransportEpoch;
        Assert.True(await workflow.SelectAsync(f.Attachments[1].Id)); sampler.Process(packet);
        Assert.Equal(epoch, sampler.TransportEpoch); Assert.True(sampler.ReadStatus().Playing);
        Assert.True(await workflow.SetSongModeAsync(false)); sampler.Process(packet); Assert.False(sampler.ReadStatus().Playing);
        await Play(); sampler.Process(packet); Assert.True(sampler.ReadStatus().Looping);
        Assert.True(await workflow.OpenAsync(save, UnsavedDecision.Discard)); Assert.True(borrower.Joined);
        Assert.True(workflow.State.SongMode); Assert.Null(workflow.State.Execution); Assert.Equal(0, workflow.State.UndoCount);
        Assert.Single(workflow.State.Snapshot.State.ArrangementClips);
    }
}
