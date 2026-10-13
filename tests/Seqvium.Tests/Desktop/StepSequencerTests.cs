// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class StepSequencerTests
{
    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Assert.True(Joined);
    }
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;
        internal RealtimeSampler? Sampler;
        internal int Opens;
        internal Fixture(ProjectDocument? document = null) => Workflow = new(document ?? ProjectDocument.Create(), () =>
        {
            Opens++; Sampler = new(48000, 2, 32);
            return new(new Borrower(), Sampler, "Test borrower", () => null);
        }, Directory.File("owned"), (doc, sampler, target, owner) => new(doc, sampler, target, owner, 8, 1, null, true));
        internal WorkflowState State => Workflow.State;
        internal ProjectDocument Document => State.Document;
        internal Pattern Pattern => BeatSteps.Resolve(State).Pattern!;
        internal BeatEditBasis Basis => BeatSteps.Resolve(State).Basis!;
        internal async Task Create() => Assert.True(await Workflow.CreateBeatAsync(Document.LifecycleId, State.Generation, "Drums"));
        internal async Task<string> Channel(string name, float value = 0.4f, int frames = 1440)
        {
            string path = Directory.File(name + ".wav");
            await File.WriteAllBytesAsync(path, WavFixtures.Create(Enumerable.Repeat(value, frames).ToArray()), TestContext.Current.CancellationToken);
            Assert.True(await Workflow.AddBeatWavAsync(Basis, path, TestContext.Current.CancellationToken), State.Detail);
            return path;
        }
        internal async Task Toggle(int channel, int step) => Assert.True(await Workflow.ToggleBeatStepAsync(Basis, Pattern.Parts[channel].Id, step));
        internal Task<bool> Edit(Action<ProjectEdit> operation) => Workflow.EditGraphAsync(
            new(Document.LifecycleId, State.Generation, State.Selected!.Value), "External canonical edit", (edit, _, _) => operation(edit));
        public async ValueTask DisposeAsync() { await Workflow.DisposeAsync(); Directory.Dispose(); }
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)] [InlineData(15)]
    public void GridMappingIsExactAndFinite(int step)
    {
        var note = new NoteEvent(Id<NoteEvent>.New(), BeatSteps.Position(step), BeatSteps.Duration(step), 60m, 1m);
        Assert.Equal(step, BeatSteps.Step(note));
        Assert.Equal(step * 240000L, note.Position.Ticks);
        Assert.Equal(Math.Min(960000L, 3840000L - step * 240000L), note.Duration.Ticks);
        Assert.True(note.Position.Ticks + note.Duration.Ticks <= 3840000L);
        Assert.Null(BeatSteps.Step(note with { Position = new(note.Position.Ticks + 1) }));
    }

    [Fact]
    public async Task EmptyCreationAndChannelsAreAtomicAndOrdinaryImportKeepsItsLongNote()
    {
        await using var f = new Fixture(); await f.Create();
        Assert.Empty(f.Pattern.Parts); Assert.Single(f.State.Snapshot.State.Placements);
        Assert.Single(f.State.Snapshot.State.Contexts); Assert.Equal(1, f.State.UndoCount);
        Assert.Equal(new Tempo(120), f.State.Snapshot.State.Settings.Tempo);
        Assert.Equal(0, f.Opens);
        var path = await f.Channel("Kick", frames: 48000);
        Assert.Empty(f.Pattern.Parts.Single().Notes); Assert.Equal(2, f.State.UndoCount);
        Assert.True(GraphDiagnostics.Inspect(f.State.Snapshot.State).Single().IsEligibleForPreparation);
        Assert.True(await f.Workflow.ImportAsync(path, TestContext.Current.CancellationToken));
        Assert.Single(f.Pattern.Parts.Single().Notes);
        Assert.True(f.Pattern.Parts.Single().Notes.Single().Duration.Ticks > 960000L);
        Assert.Equal(0, f.Opens);
    }

    [Fact]
    public async Task ToggleUsesRootPitchOneHistoryEntryAndUndoRedoRestoresExactNoteIdentity()
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick");
        var part = f.Pattern.Parts.Single(); var sound = f.State.Snapshot.State.Sounds.Single();
        Assert.True(await f.Edit(edit => edit.ConfigurePcmSampler(sound.Id, sound.ResourceIds.Single(), 72, 0)));
        int undo = f.State.UndoCount;
        await f.Toggle(0, 15); var enabled = f.State.Snapshot;
        var note = f.Pattern.Parts.Single().Notes.Single(); Assert.Equal(72, note.Pitch); Assert.Equal(1, note.Intensity);
        Assert.Equal(undo + 1, f.State.UndoCount);
        await f.Toggle(0, 15); Assert.Empty(f.Pattern.Parts.Single().Notes); Assert.Equal(undo + 2, f.State.UndoCount);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(enabled, f.State.Snapshot);
        Assert.Equal(note.Id, f.Pattern.Parts.Single().Notes.Single().Id);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Empty(f.Pattern.Parts.Single().Notes);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Equal(note.Id, f.Pattern.Parts.Single().Notes.Single().Id);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Empty(f.Pattern.Parts.Single().Notes);
        Assert.True(await f.Workflow.RenameBeatAsync(f.Basis, "Named rhythm")); Assert.Equal("Named rhythm", f.Pattern.Name);
        Assert.Equal(part.Id, f.Pattern.Parts.Single().Id);
    }

    [Fact]
    public async Task ConcurrentStaleCommandsNeverDuplicateAndAbaOrSelectionChangeCannotRetarget()
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick");
        var basis = f.Basis; var part = f.Pattern.Parts.Single().Id;
        var results = await Task.WhenAll(f.Workflow.ToggleBeatStepAsync(basis, part, 0), f.Workflow.ToggleBeatStepAsync(basis, part, 0));
        Assert.Single(results, r => r); Assert.Single(f.Pattern.Parts.Single().Notes);
        Assert.True(await f.Workflow.UndoAsync()); Assert.False(await f.Workflow.ToggleBeatStepAsync(basis, part, 0));
        Assert.Equal("Beat.Stale", f.State.Detail);
        var fresh = f.Basis; await f.Create();
        Assert.False(await f.Workflow.ToggleBeatStepAsync(fresh, part, 0)); Assert.Empty(f.Pattern.Parts);
    }

    [Fact]
    public async Task RowGestureIsOneUndoPreservesUnchangedNotesAndDoesNotTouchOtherChannels()
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Channel("Snare");
        await f.Toggle(0, 0); await f.Toggle(1, 4);
        var note = f.Pattern.Parts[0].Notes.Single(); var other = f.Pattern.Parts[1]; var before = f.State.Snapshot;
        int undo = f.State.UndoCount;
        var desired = Enumerable.Range(0, 16).Select(i => i % 4 == 0).ToArray();
        Assert.True(await f.Workflow.SetBeatStepsAsync(f.Basis, f.Pattern.Parts[0].Id, desired));
        Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.Equal(new[] { 0, 4, 8, 12 }, f.Pattern.Parts[0].Notes.Select(n => BeatSteps.Step(n)!.Value).Order());
        Assert.Contains(f.Pattern.Parts[0].Notes, n => n.Id == note.Id);
        Assert.Equal(other, f.Pattern.Parts[1]); var after = f.State.Snapshot;
        Assert.False(await f.Workflow.SetBeatStepsAsync(f.Basis, f.Pattern.Parts[0].Id, desired));
        Assert.Same(after, f.State.Snapshot); Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(after, f.State.Snapshot);
        Assert.True(await f.Workflow.SetBeatStepsAsync(f.Basis, f.Pattern.Parts[0].Id, new bool[16]));
        Assert.Empty(f.Pattern.Parts[0].Notes); Assert.Equal(other, f.Pattern.Parts[1]);
        Assert.Equal(0, f.Opens);
    }

    [Fact]
    public async Task HeldRowBasisExpiresAfterUndoEvenWhenIdenticalTargetReturns()
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick");
        var basis = f.Basis; var part = f.Pattern.Parts[0].Id;
        await f.Toggle(0, 8); Assert.True(await f.Workflow.UndoAsync());
        var before = f.State.Snapshot; int undo = f.State.UndoCount;
        Assert.False(await f.Workflow.SetBeatStepsAsync(basis, part, Enumerable.Repeat(true, 16).ToArray()));
        Assert.Equal("Beat.Stale", f.State.Detail); Assert.Same(before, f.State.Snapshot); Assert.Equal(undo, f.State.UndoCount);
    }

    [Fact]
    public async Task TempoEditPreservesMusicIsOneUndoAndRejectsStaleDrafts()
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Toggle(0, 0);
        var before = f.State.Snapshot; var pattern = f.Pattern; int undo = f.State.UndoCount;
        var basis = f.Basis;
        Assert.True(await f.Workflow.SetTempoAsync(basis.Lifecycle, basis.Generation, 137.5m));
        Assert.Equal(137.5m, f.State.Snapshot.State.Settings.Tempo.BeatsPerMinute);
        Assert.Equal(before.State.Settings.Meter, f.State.Snapshot.State.Settings.Meter);
        Assert.Equal(pattern, f.Pattern); Assert.Equal(undo + 1, f.State.UndoCount);
        var after = f.State.Snapshot;
        Assert.False(await f.Workflow.SetTempoAsync(f.Basis.Lifecycle, f.Basis.Generation, 137.5m));
        Assert.Same(after, f.State.Snapshot); Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.State.Snapshot);
        Assert.False(await f.Workflow.SetTempoAsync(basis.Lifecycle, basis.Generation, 90));
        Assert.Equal("Workflow.EditStale", f.State.Detail); Assert.Same(before, f.State.Snapshot);
        Assert.Equal(0, f.Opens);
    }

    [Theory]
    [InlineData("0")] [InlineData("1001")] [InlineData("120.1234567")]
    public async Task TempoEditRefusesOutOfRangeOrUnrepresentableValues(string text)
    {
        await using var f = new Fixture(); var before = f.State.Snapshot;
        Assert.False(await f.Workflow.SetTempoAsync(f.State.Document.LifecycleId, f.State.Generation,
            decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Same(before, f.State.Snapshot); Assert.Equal(0, f.State.UndoCount);
    }

    [Theory]
    [InlineData("length")] [InlineData("off-grid")] [InlineData("pitch")]
    public async Task RowOperationsRefuseUnsupportedContentWithoutRewritingIt(string kind)
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Toggle(0, 0);
        var part = f.Pattern.Parts[0]; var note = part.Notes.Single(); var pattern = f.Pattern;
        Assert.True(await f.Edit(edit =>
        {
            if (kind == "length") edit.SetPatternLength(pattern.Id, new(BeatSteps.BarTicks + BeatSteps.StepTicks));
            else edit.UpdateNote(pattern.Id, part.Id, note.Id, new(kind == "off-grid" ? 1 : 0), note.Duration,
                kind == "pitch" ? note.Pitch + 1 : note.Pitch, note.Intensity);
        }));
        var before = f.State.Snapshot; int undo = f.State.UndoCount;
        Assert.False(await f.Workflow.SetBeatStepsAsync(f.Basis, part.Id, new bool[16]));
        Assert.Same(before, f.State.Snapshot); Assert.Equal(undo, f.State.UndoCount);
    }

    [Theory]
    [InlineData(90)] [InlineData(120)] [InlineData(240)]
    public async Task ThreeIndependentWavsProduceExactDrumEventsAndPcmThroughExistingGraph(int bpm)
    {
        await using var f = new Fixture(ProjectDocument.Create(new(new Tempo(bpm), new Meter(4, 4))));
        await f.Create(); await f.Channel("Kick", 0.4f); await f.Channel("Snare", 0.2f); await f.Channel("Hi-Hat", 0.1f);
        foreach (int step in new[] { 0, 8 }) await f.Toggle(0, step);
        foreach (int step in new[] { 4, 12 }) await f.Toggle(1, step);
        for (int step = 0; step < 16; step += 2) await f.Toggle(2, step);
        var state = f.State.Snapshot.State; var attachment = state.GraphAttachments.Single(); var graph = state.Graphs.Single();
        Assert.Equal(3, attachment.Sources.Length); Assert.Equal(3, state.Sounds.Select(s => s.Id).Distinct().Count());
        Assert.Equal(3, state.Resources.Select(r => r.Id).Distinct().Count()); Assert.Equal(2, graph.Nodes.Count(n => n.Type == GraphBuiltIns.Mix));
        Assert.All(attachment.Sources, binding => Assert.Equal(state.Placements.Single().Id, binding.PlacementId));
        using var plan = GraphPreparation.Prepare(f.Document, attachment.Id, 48000, 2, 32, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, plan.Contributions.Length);
        Assert.Equal(12, plan.Events.Count(e => e.Kind == ExecutionEventKind.NoteOn));
        Assert.Equal(11520000L / bpm, plan.EndFrame);
        long Frame(MusicalPosition position) => (long)decimal.Round((decimal)position.Ticks * 60 * 48000 / (960000m * bpm), 0, MidpointRounding.AwayFromZero);
        foreach (var e in plan.Events.Where(e => e.Occurrence is not null))
        {
            Assert.Equal(Frame(e.Position), e.Frame);
            Assert.Equal(f.Pattern.Id, e.Occurrence!.Value.PatternId);
            Assert.Equal(state.Placements.Single().Id, e.Occurrence.Value.PlacementId);
        }
        using var execution = plan.CreateExecution();
        var pcm = new float[checked((int)plan.EndFrame * 2)];
        for (int offset = 0; offset < pcm.Length; offset += 64) execution.Process(pcm.AsSpan(offset, Math.Min(64, pcm.Length - offset)));
        for (int step = 0; step < 16; step++)
        {
            long frame = Frame(new(step * 240000L));
            float expected = step % 8 == 0 ? 0.125f : step % 8 == 4 ? 0.075f : step % 2 == 0 ? 0.025f : 0;
            Assert.InRange(Math.Abs(pcm[checked((int)frame * 2)] - expected), 0, 1e-6f);
            Assert.InRange(Math.Abs(pcm[checked((int)frame * 2 + 1)] - expected), 0, 1e-6f);
        }
        Assert.Equal(plan.EndFrame, execution.Position);
    }

    [Fact]
    public async Task ExplicitDesktopPlayLoopsAndStepEditsContinueUntilHardStop()
    {
        await using var f = new Fixture(); await f.Create();
        await f.Channel("Kick", .4f); await f.Channel("Snare", .2f); await f.Channel("Hi-Hat", .1f);
        await f.Toggle(0, 0); await f.Toggle(1, 4); await f.Toggle(2, 0);
        var play = f.Workflow.PlayAsync();
        var packet = new float[64]; float peak = 0;
        for (int pass = 0; !play.IsCompleted && pass < 10000; pass++)
        { await f.Workflow.ObserveAsync(); f.Sampler?.Process(packet); peak = Math.Max(peak, packet.Max()); await Task.Yield(); }
        Assert.True(await play.WaitAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, f.Opens); Assert.NotNull(f.Sampler);
        for (int pass = 0; pass < 3010; pass++) { f.Sampler.Process(packet); peak = Math.Max(peak, packet.Max()); }
        Assert.InRange(peak, .12499f, .12501f);
        Assert.Equal(SamplerTransportState.Playing, f.Sampler.ReadStatus().Transport);
        Assert.True(f.Sampler.ReadStatus().Looping);
        long epoch = f.Sampler.TransportEpoch, position = f.Sampler.RenderPosition;
        await f.Toggle(0, 8);
        for (int pass = 0; pass < 10000 && f.Sampler.ReadStatus().ExecutingRevision != f.State.Snapshot.Revision; pass++)
        { await f.Workflow.ObserveAsync(); f.Sampler.Process(packet); await Task.Yield(); }
        Assert.Equal(f.State.Snapshot.Revision, f.Sampler.ReadStatus().ExecutingRevision);
        Assert.True(f.Sampler.ReadStatus().Playing); Assert.Equal(epoch, f.Sampler.TransportEpoch);
        Assert.True(f.Sampler.RenderPosition > position);
        Assert.True(await f.Workflow.StopAsync(true)); f.Sampler.Process(packet);
        Assert.All(packet, value => Assert.Equal(0, value)); Assert.False(f.Sampler.ReadStatus().Playing);
    }

    [Theory]
    [InlineData(0, 24000)] [InlineData(15, 6000)]
    public async Task LongSamplesEndAtTheOrdinaryFiniteGateRatherThanOneShotTail(int step, int gateFrames)
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Long percussion", frames: 48000); await f.Toggle(0, step);
        using var plan = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 32, cancellationToken: TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution(); var pcm = new float[192000];
        for (int offset = 0; offset < pcm.Length; offset += 64) execution.Process(pcm.AsSpan(offset, 64));
        int start = step * 6000;
        Assert.All(pcm.Take(start * 2), value => Assert.Equal(0, value));
        Assert.All(pcm.Skip(start * 2).Take(gateFrames * 2), value => Assert.Equal(.1f, value));
        Assert.All(pcm.Skip((start + gateFrames) * 2), value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task ChannelUndoRestoresThePreviousGraphAndRedoRestoresMediaAndExactBindings()
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Channel("Snare", .2f);
        var second = f.State.Snapshot; await f.Channel("Hi-Hat", .1f); var third = f.State.Snapshot;
        var channel = f.Pattern.Parts[2];
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(second, f.State.Snapshot);
        Assert.True(GraphDiagnostics.Inspect(f.State.Snapshot.State).Single().IsEligibleForPreparation);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Same(third, f.State.Snapshot);
        Assert.Equal(channel, f.Pattern.Parts[2]);
        using var pcm = ProjectMedia.Decode(f.Document, f.State.Snapshot.State.Sounds.Single(s => s.Id == channel.SoundId).ResourceIds.Single());
        Assert.Equal(.1f, pcm.Sample(0, 0));
    }

    [Fact]
    public async Task SaveOpenKeepsPatternPartNoteAndBindingsWithDurableMediaAndFreshHistory()
    {
        await using var f = new Fixture(); await f.Create();
        var paths = new[] { await f.Channel("Kick"), await f.Channel("Snare", .2f), await f.Channel("Hi-Hat", .1f) };
        await f.Toggle(0, 0); await f.Toggle(1, 4); await f.Toggle(2, 2);
        var before = f.State.Snapshot; var basis = f.Basis; var oldDocument = f.Document;
        string save = f.Directory.File("drums.seqvium"); Assert.True(await f.Workflow.SaveAsync(save));
        foreach (var path in paths) File.Delete(path);
        Assert.True(await f.Workflow.OpenAsync(save, UnsavedDecision.Discard)); Assert.True(oldDocument.IsClosed);
        Assert.Equal(before.Revision, f.State.Snapshot.Revision);
        Assert.Equal(before.State.Patterns.Single().Id, f.Pattern.Id);
        Assert.Equal(before.State.Patterns.Single().Parts.Select(p => (p.Id, p.SoundId, p.Notes.Single().Id)),
            f.Pattern.Parts.Select(p => (p.Id, p.SoundId, p.Notes.Single().Id)));
        Assert.Equal(before.State.GraphAttachments.Single().Sources.Select(s => (s.Id, s.NodeId, s.PartId)),
            f.State.Snapshot.State.GraphAttachments.Single().Sources.Select(s => (s.Id, s.NodeId, s.PartId)));
        Assert.Equal(0, f.State.UndoCount); Assert.Equal(0, f.State.RedoCount); Assert.Null(f.State.Execution); Assert.Equal(0, f.Opens);
        foreach (var resource in f.State.Snapshot.State.Resources)
        { using var pcm = ProjectMedia.Decode(f.Document, resource.Id); Assert.Equal(1440, pcm.Frames); }
        Assert.False(await f.Workflow.ToggleBeatStepAsync(basis, f.Pattern.Parts[0].Id, 8));
        await f.Toggle(0, 8); Assert.Equal(1, f.State.UndoCount); Assert.True(await f.Workflow.UndoAsync());
        Assert.Single(f.Pattern.Parts[0].Notes);
    }

    [Fact]
    public async Task SharedPatternEditingKeepsSharedIdentitiesWhileChannelAddRefuses()
    {
        await using var f = new Fixture(); await f.Create(); var path = await f.Channel("Kick");
        var pattern = f.Pattern.Id; var part = f.Pattern.Parts.Single().Id;
        Assert.True(await f.Edit(edit => edit.AddPlacement(pattern, new(BeatSteps.BarTicks))));
        await f.Toggle(0, 0);
        Assert.Single(f.State.Snapshot.State.Patterns); Assert.Equal(2, f.State.Snapshot.State.Placements.Count(p => p.PatternId == pattern));
        Assert.Equal(part, f.Pattern.Parts.Single().Id); Assert.Single(f.Pattern.Parts.Single().Notes);
        int undo = f.State.UndoCount;
        Assert.False(await f.Workflow.AddBeatWavAsync(f.Basis, path, TestContext.Current.CancellationToken)); Assert.Equal("Beat.SharedChannels", f.State.Detail);
        Assert.Equal(undo, f.State.UndoCount);
    }

    [Theory]
    [InlineData("meter")] [InlineData("length")] [InlineData("off-grid")] [InlineData("duplicate")]
    [InlineData("pitch")] [InlineData("intensity")] [InlineData("duration")] [InlineData("root-changed")]
    public async Task UnsupportedContentIsPreservedWithoutHistory(string kind)
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Toggle(0, 0);
        var pattern = f.Pattern; var part = pattern.Parts.Single(); var note = part.Notes.Single();
        Assert.True(await f.Edit(edit =>
        {
            if (kind == "meter") edit.SetSettings(new(new Tempo(120), new Meter(3, 4)));
            else if (kind == "length") edit.SetPatternLength(pattern.Id, new(BeatSteps.BarTicks + BeatSteps.StepTicks));
            else if (kind == "duplicate") edit.AddNote(pattern.Id, part.Id, note.Position, note.Duration, note.Pitch, note.Intensity);
            else if (kind == "root-changed") edit.ConfigurePcmSampler(part.SoundId, f.State.Snapshot.State.Resources.Single().Id, 61, 0);
            else edit.UpdateNote(pattern.Id, part.Id, note.Id, new(kind == "off-grid" ? 1 : 0),
                new(kind == "duration" ? 1 : note.Duration.Ticks), kind == "pitch" ? 61 : 60, kind == "intensity" ? .5m : 1m);
        }));
        var snapshot = f.State.Snapshot; int undo = f.State.UndoCount;
        Assert.False(await f.Workflow.ToggleBeatStepAsync(f.Basis, part.Id, 0)); Assert.Same(snapshot, f.State.Snapshot); Assert.Equal(undo, f.State.UndoCount);
    }

    [Theory]
    [InlineData("cancel")] [InlineData("invalid")] [InlineData("missing")] [InlineData("stale")]
    public async Task FailedImportLeavesNoChannelMediaOrHistory(string failure)
    {
        await using var f = new Fixture(); await f.Create();
        string path = f.Directory.File("bad.wav");
        await File.WriteAllBytesAsync(path, failure == "invalid" ? [1, 2, 3] : WavFixtures.Create([.3f]), TestContext.Current.CancellationToken);
        var basis = f.Basis;
        if (failure == "missing") File.Delete(path);
        if (failure == "stale") Assert.True(await f.Workflow.RenameBeatAsync(basis, "Changed"));
        using var cancellation = new CancellationTokenSource(); if (failure == "cancel") cancellation.Cancel();
        var snapshot = f.State.Snapshot; int undo = f.State.UndoCount;
        Assert.False(await f.Workflow.AddBeatWavAsync(basis, path, cancellation.Token));
        Assert.Same(snapshot, f.State.Snapshot); Assert.Equal(undo, f.State.UndoCount);
        Assert.Empty(f.Pattern.Parts); Assert.Empty(f.State.Snapshot.State.Resources);
        var owned = f.Directory.File("owned");
        Assert.True(!System.IO.Directory.Exists(owned) || System.IO.Directory.GetFiles(owned, "*.wav", SearchOption.AllDirectories).Length == 0);
    }

    [Theory]
    [InlineData("pattern")] [InlineData("part")] [InlineData("note")]
    public async Task UnknownMusicalDataSurvivesGridRefusalAndSave(string scope)
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Toggle(0, 0);
        string path = f.Directory.File("future.seqvium"); Assert.True(await f.Workflow.SaveAsync(path));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken))!;
        var pattern = json["state"]!["patterns"]![0]!;
        var part = pattern["parts"]![0]!;
        var target = scope == "pattern" ? pattern : scope == "part" ? part : part["notes"]![0]!;
        target["futureMusic"] = JsonNode.Parse("{\"opaque\":[1,true,\"unchanged\"]}");
        await File.WriteAllTextAsync(path, json.ToJsonString(), TestContext.Current.CancellationToken);
        Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard));
        var snapshot = f.State.Snapshot;
        Assert.False(await f.Workflow.ToggleBeatStepAsync(f.Basis, f.Pattern.Parts[0].Id, 0)); Assert.Same(snapshot, f.State.Snapshot);
        Assert.True(await f.Workflow.SaveAsync(path));
        var saved = JsonNode.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken))!;
        var savedPattern = saved["state"]!["patterns"]![0]!; var savedPart = savedPattern["parts"]![0]!;
        var savedTarget = scope == "pattern" ? savedPattern : scope == "part" ? savedPart : savedPart["notes"]![0]!;
        Assert.True(JsonNode.DeepEquals(target["futureMusic"], savedTarget["futureMusic"]));
    }

    [Fact]
    public async Task CancellationAfterBeginAndClosedTargetNeverAcceptAChannel()
    {
        await using var f = new Fixture(); await f.Create();
        string path = f.Directory.File("source.wav"); await File.WriteAllBytesAsync(path, WavFixtures.Create([.3f]), TestContext.Current.CancellationToken);
        var snapshot = f.State.Snapshot;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        void Withdraw(WorkflowState state) { if (state.StatusKey == "Workflow.Importing") cancellation.Cancel(); }
        f.Workflow.Changed += Withdraw;
        Assert.False(await f.Workflow.AddBeatWavAsync(f.Basis, path, cancellation.Token));
        f.Workflow.Changed -= Withdraw; Assert.Same(snapshot, f.State.Snapshot);
        var basis = f.Basis;
        Assert.True(await f.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.False(await f.Workflow.AddBeatWavAsync(basis, path, TestContext.Current.CancellationToken));
        Assert.Same(snapshot, f.State.Snapshot);
    }

    [Fact]
    public async Task UnsupportedMeterCannotCreateOrSilentlyChangeProjectSettings()
    {
        await using var f = new Fixture(ProjectDocument.Create(new(new Tempo(137.5m), new Meter(7, 8))));
        var snapshot = f.State.Snapshot;
        Assert.False(await f.Workflow.CreateBeatAsync(f.Document.LifecycleId, f.State.Generation, "Drums"));
        Assert.Equal("Beat.MeterUnsupported", f.State.Detail); Assert.Same(snapshot, f.State.Snapshot); Assert.Equal(0, f.State.UndoCount);
    }

    [Fact]
    public async Task ArbitraryGraphIsRefusedAndExistingNodesAndGainSurviveSafeComposition()
    {
        await using var f = new Fixture(); await f.Create(); var path = await f.Channel("Kick");
        var first = f.State.Snapshot.State.Graphs.Single();
        Assert.True(await f.Workflow.SetGainAsync(.6m));
        await f.Channel("Snare"); await f.Channel("Hi-Hat");
        var after = f.State.Snapshot.State.Graphs.Single();
        Assert.All(first.Nodes, original => Assert.Equal(original.Position, after.Nodes.Single(n => n.Id == original.Id).Position));
        Assert.Equal(.6m, f.State.Gain);
        Assert.True(await f.Edit(edit => edit.AddGraphNode(after.Id, GraphBuiltIns.Gain, new(50, 50))));
        var snapshot = f.State.Snapshot;
        Assert.False(await f.Workflow.AddBeatWavAsync(f.Basis, path, TestContext.Current.CancellationToken));
        Assert.Equal("Beat.GraphUnsupported", f.State.Detail); Assert.Same(snapshot, f.State.Snapshot);
    }

    [Fact]
    public async Task EighthChannelIsBoundedAndDenseLongSamplesUseExistingVoiceRefusal()
    {
        await using var f = new Fixture(); await f.Create();
        for (int channel = 0; channel < 8; channel++) { await f.Channel("Channel" + channel, frames: 48000); await f.Toggle(channel, 0); }
        var snapshot = f.State.Snapshot;
        Assert.False(await f.Workflow.AddBeatWavAsync(f.Basis, f.Directory.File("Channel0.wav"), TestContext.Current.CancellationToken));
        Assert.Equal("Beat.ChannelLimit", f.State.Detail); Assert.Same(snapshot, f.State.Snapshot);
        using var valid = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 32, cancellationToken: TestContext.Current.CancellationToken);
        await f.Toggle(0, 1);
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 32, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void BeatLabelsAndPaneLayoutHaveStableHostOwnership()
    {
        Assert.All(HostLocalizer.English.Keys.Where(k => k.StartsWith("Beat.") || k == "Pane.Beat"),
            key => Assert.True(HostLocalizer.Russian.ContainsKey(key), key));
        using var workspace = new WorkspaceState(); workspace.Reflow(640, 380); workspace.Open(WorkspaceState.BeatId);
        workspace.Dock(WorkspaceState.BeatId, PaneDock.Left); workspace.Collapse(WorkspaceState.BeatId); workspace.Open(WorkspaceState.BeatId);
        var saved = workspace.Capture(); using var reopened = new WorkspaceState(saved); reopened.Reflow(640, 380);
        Assert.Equal(workspace.Get(WorkspaceState.BeatId), reopened.Get(WorkspaceState.BeatId));
    }

    [Theory]
    [InlineData(2)] [InlineData(3)] [InlineData(32)]
    public async Task LongerPatternsPreserveNotesAndHistoryAndSurviveSaveOpen(int bars)
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Toggle(0, 15);
        var before = f.State.Snapshot; var note = f.Pattern.Parts[0].Notes.Single();
        Assert.True(await f.Workflow.ResizeBeatAsync(f.Basis, bars));
        Assert.Equal(bars * 16, BeatSteps.StepCount(f.Pattern));
        var extended = f.Pattern.Parts[0].Notes.Single(); Assert.Equal(note.Id, extended.Id);
        Assert.Equal(960000, extended.Duration.Ticks);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true));
        await f.Toggle(0, bars * 16 - 1);
        var after = f.State.Snapshot;
        Assert.False(await f.Workflow.ResizeBeatAsync(f.Basis, 1));
        Assert.Equal("Beat.ShortenOccupied", f.State.Detail); Assert.Same(after, f.State.Snapshot);
        string path = f.Directory.File("long.seqvium"); Assert.True(await f.Workflow.SaveAsync(path));
        Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard));
        Assert.True(ProjectDocument.HasSameContent(after.State, f.State.Snapshot.State));
        Assert.Equal(bars * 16 - 1, BeatSteps.Step(f.Pattern.Parts[0].Notes.Single(n => n.Id != note.Id)));
        Assert.Equal(0, f.Opens); Assert.Equal(0, f.State.UndoCount);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task DeleteAnyChannelRepairsMixTreeAndUndoRestoresAllIdentities(int index)
    {
        await using var f = new Fixture(); await f.Create();
        await f.Channel("Kick"); await f.Channel("Snare"); await f.Channel("Hat");
        await f.Toggle(0, 0); await f.Toggle(1, 0); await f.Toggle(2, 0);
        var before = f.State.Snapshot; var part = f.Pattern.Parts[index]; int undo = f.State.UndoCount;
        Assert.True(await f.Workflow.DeleteBeatChannelAsync(f.Basis, part.Id));
        Assert.Equal(undo + 1, f.State.UndoCount); Assert.Equal(2, f.Pattern.Parts.Length);
        Assert.DoesNotContain(f.State.Snapshot.State.Sounds, sound => sound.Id == part.SoundId);
        Assert.Equal(3, f.State.Snapshot.State.Resources.Length); // Saved/history media stays owned.
        Assert.True(GraphDiagnostics.Inspect(f.State.Snapshot.State).Single().IsEligibleForPreparation);
        using (var plan = GraphPreparation.Prepare(f.Document, f.Basis.Attachment, 48000, 2, 32, cancellationToken: TestContext.Current.CancellationToken))
        using (var execution = plan.CreateExecution())
        { var samples = new float[64]; execution.Process(samples); Assert.Equal(.2f, samples[0]); }
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true));
        foreach (var remaining in f.Pattern.Parts.ToArray())
            Assert.True(await f.Workflow.DeleteBeatChannelAsync(f.Basis, remaining.Id));
        Assert.Empty(f.Pattern.Parts); Assert.Empty(f.State.Snapshot.State.GraphAttachments.Single().Sources);
        await f.Channel("New"); Assert.True(GraphDiagnostics.Inspect(f.State.Snapshot.State).Single().IsEligibleForPreparation);
        string path = f.Directory.File("deleted.seqvium"); var state = f.State.Snapshot.State;
        Assert.True(await f.Workflow.SaveAsync(path)); Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard));
        Assert.True(ProjectDocument.HasSameContent(state, f.State.Snapshot.State));
    }

    [Fact]
    public async Task PatternDeletionRemovesItsOccurrencesAndGraphsAndUndoRestoresProject()
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Toggle(0, 0);
        var target = f.Basis; await f.Create(); await f.Channel("Other");
        var other = f.Pattern; Assert.True(await f.Workflow.SelectAsync(target.Attachment));
        var before = f.State.Snapshot;
        Assert.True(await f.Workflow.DeleteBeatAsync(f.Basis));
        Assert.Equal(other, Assert.Single(f.State.Snapshot.State.Patterns));
        Assert.Single(f.State.Snapshot.State.Placements); Assert.Single(f.State.Snapshot.State.Graphs);
        Assert.Single(f.State.Snapshot.State.Contexts); Assert.Equal(other.Id, f.Pattern.Id);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Same(before, f.State.Snapshot);
        Assert.False(await f.Workflow.DeleteBeatAsync(target)); Assert.Equal("Beat.Stale", f.State.Detail);
    }

    [Fact]
    public async Task ExportWritesActualGraphPcmAndLeavesMusicHistoryAndMediaIntact()
    {
        await using var f = new Fixture(); await f.Create(); await f.Channel("Kick"); await f.Channel("Snare", .2f);
        Assert.True(await f.Workflow.ResizeBeatAsync(f.Basis, 2)); await f.Toggle(0, 0); await f.Toggle(1, 31);
        var before = f.State.Snapshot; int undo = f.State.UndoCount;
        string path = f.Directory.File("beat.wav");
        Assert.True(await f.Workflow.ExportWaveAsync(f.Basis, path, TestContext.Current.CancellationToken), f.State.Detail);
        Assert.Equal("Workflow.Exported", f.State.StatusKey); Assert.Same(before, f.State.Snapshot);
        Assert.Equal(undo, f.State.UndoCount); Assert.Equal(0, f.Opens);
        using var decoded = WavDecoder.Decode(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(192000, decoded.Frames); Assert.Equal(2, decoded.Channels); Assert.Equal(48000, decoded.SampleRate);
        using var pcm = decoded.Lease(); Assert.InRange(pcm.Samples[0], .09998f, .10002f);
        Assert.InRange(pcm.Samples[186000 * 2], .04998f, .05002f); Assert.Equal(0, pcm.Samples[48000]);
        byte[] original = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.False(await f.Workflow.ExportWaveAsync(f.Basis, path, cancelled.Token));
        Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        string media = ProjectMedia.Resolve(f.Document, f.State.Snapshot.State.Resources[0]);
        Assert.False(await f.Workflow.ExportWaveAsync(f.Basis, media, TestContext.Current.CancellationToken));
        Assert.Empty(System.IO.Directory.GetFiles(f.Directory.Path, "*.pending", SearchOption.AllDirectories));
        Assert.Same(before, f.State.Snapshot);
    }
}
