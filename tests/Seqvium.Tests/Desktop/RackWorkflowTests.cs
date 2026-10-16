// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class RackWorkflowTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;
        internal RealtimeSampler? Sampler;
        internal Fixture() => Workflow = new(ProjectDocument.Create(), mediaDirectory: Directory.File("owned"));

        internal Fixture(int rate) => Workflow = new(ProjectDocument.Create(), () =>
        {
            Sampler = new(rate, 2, 127);
            return new(new Borrower(), Sampler, "Controlled Rack output", () => null);
        }, Directory.File("owned"));

        internal WorkflowState State => Workflow.State;
        internal Pattern Pattern => BeatSteps.Resolve(State).Pattern!;
        internal BeatEditBasis Basis => BeatSteps.Resolve(State).Basis!;

        internal async Task Create(string algorithm)
        {
            Assert.True(await Workflow.CreateBeatAsync(State.Document.LifecycleId, State.Generation, "Phrase"));
            if (algorithm == PcmSampler.Algorithm)
            {
                var path = Directory.File("source.wav");
                await File.WriteAllBytesAsync(path, WavFixtures.Create(Enumerable.Range(0, 24000)
                    .Select(i => .2f * (float)Math.Sin(i * .025)).ToArray()), TestContext.Current.CancellationToken);
                Assert.True(await Workflow.AddBeatWavAsync(Basis, path));
            }
            else Assert.True(await Workflow.AddDrumVoiceAsync(Basis, "Snare", algorithm));

            var part = Pattern.Parts.Single();
            Assert.True(await Edit(e =>
            {
                var sound = State.Snapshot.State.Sounds.Single();
                var parameters = algorithm == PcmSampler.Algorithm
                    ? sound.Parameters.SetItem("rootPitch", 63).SetItem("releaseMilliseconds", 30)
                    : algorithm == SynthVoice.Algorithm
                        ? SynthVoice.Presets[2].Values.SetItem("detune", 13).SetItem("cutoff", 2300)
                        : DrumInstruments.Find(algorithm)!.Presets[2].Parameters.SetItem("tune", 4)
                            .SetItem("brightness", .73m);
                e.SetSoundParameters(sound.Id, parameters);
                e.AddNote(Pattern.Id, part.Id, new(0), new(240000), 61.5m, .63m);
                e.AddNote(Pattern.Id, part.Id, new(960000), new(120000), 57, .21m);
            }));
        }

        internal Task<bool> Edit(Action<ProjectEdit> action) => Workflow.EditGraphAsync(
            new(State.Document.LifecycleId, State.Generation, State.Selected!.Value), "Fixture edit",
            (edit, _, _) => action(edit));

        internal float[] Render(int rate)
        {
            using var plan = GraphPreparation.Prepare(State.Document, State.Selected!.Value, rate, 2, 127,
                cancellationToken: TestContext.Current.CancellationToken);
            using var execution = plan.CreateExecution();
            var samples = new float[checked((int)plan.EndFrame * 2)];
            while (execution.Position < plan.EndFrame)
            {
                int frames = (int)Math.Min(127, plan.EndFrame - execution.Position);
                execution.Process(samples.AsSpan((int)execution.Position * 2, frames * 2));
            }

            return samples;
        }

        public async ValueTask DisposeAsync()
        {
            await Workflow.DisposeAsync();
            Directory.Dispose();
        }
    }

    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Assert.True(Joined);
    }

    [Theory]
    [InlineData(44100, false, false)]
    [InlineData(48000, false, false)]
    [InlineData(44100, true, false)]
    [InlineData(48000, true, false)]
    [InlineData(44100, true, true)]
    [InlineData(48000, true, true)]
    public async Task ReorderAndGainPreserveActivePlanClockAndVoices(int rate, bool mixer, bool song)
    {
        await using var f = new Fixture(rate);
        await f.Create(SynthVoice.Algorithm);
        var part = f.Pattern.Parts.Single();
        Assert.True(await f.Edit(e => e.SetPartNotes(f.Pattern.Id, part.Id,
            [new(Id<NoteEvent>.New(), new(0), new(2880000), 60, .2m)])));
        Assert.True(await f.Workflow.DuplicateBeatChannelAsync(f.Basis, part.Id));
        if (mixer) Assert.True(await f.Edit(e => e.InitializeMixer()));
        if (song)
        {
            Assert.True(await f.Edit(e =>
                e.AddArrangementClip(f.State.Snapshot.State.Placements.Single().Id, new(0), 0, 2)));
            Assert.True(await f.Workflow.SetSongModeAsync(true));
        }

        var play = f.Workflow.PlayAsync();
        for (int i = 0; i < 10000 && !play.IsCompleted; i++)
        {
            f.Sampler?.Process(Span<float>.Empty);
            await f.Workflow.ObserveAsync();
            await Task.Yield();
        }

        Assert.True(await play.WaitAsync(TestContext.Current.CancellationToken), f.State.Detail);
        var sampler = f.Sampler!;
        var packet = new float[254];
        sampler.Process(packet);
        long epoch = sampler.TransportEpoch, prepared = sampler.ReadStatus().PreparedExecutionId;
        Assert.Equal(2, sampler.ActiveVoices);
        var before = f.State.Snapshot;
        for (int i = 0; i < 3; i++)
        {
            long position = sampler.RenderPosition;
            Assert.True(await f.Workflow.MoveBeatChannelAsync(f.Basis, part.Id, i % 2 == 0 ? 1 : 0));
            await f.Workflow.ObserveAsync();
            sampler.Process(packet);
            Assert.Equal(epoch, sampler.TransportEpoch);
            Assert.Equal(prepared, sampler.ReadStatus().PreparedExecutionId);
            Assert.Equal(position + 127, sampler.RenderPosition);
            Assert.Equal(2, sampler.ActiveVoices);
        }

        Assert.True(await f.Workflow.SetGainAsync(.37m));
        for (int i = 0; i < 10000 && f.State.Execution?.PreparationPending == true; i++)
        {
            sampler.Process(Span<float>.Empty);
            await f.Workflow.ObserveAsync();
            await Task.Yield();
        }

        sampler.Process(packet);
        Assert.Equal(epoch, sampler.TransportEpoch);
        Assert.Equal(2, sampler.ActiveVoices);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.True(await f.Workflow.UndoAsync());
        sampler.Process(packet);
        Assert.Equal(epoch, sampler.TransportEpoch);
        Assert.Equal(2, sampler.ActiveVoices);
        Assert.Equal(before.State.Patterns.Single().Parts[0].Id, f.Pattern.Parts[0].Id);
    }

    [Theory]
    [InlineData("core.drum-voice.snare-v1", 44100)]
    [InlineData("core.drum-voice.snare-v1", 48000)]
    [InlineData(SynthVoice.Algorithm, 44100)]
    [InlineData(SynthVoice.Algorithm, 48000)]
    [InlineData(PcmSampler.Algorithm, 44100)]
    [InlineData(PcmSampler.Algorithm, 48000)]
    public async Task IndependentDuplicateBindsExactMusicAndReorderIsBitIdentical(string algorithm, int rate)
    {
        await using var f = new Fixture();
        await f.Create(algorithm);
        var before = f.State.Snapshot;
        var part = f.Pattern.Parts.Single();
        var sound = f.State.Snapshot.State.Sounds.Single();
        float[] baseline = f.Render(rate);
        int undo = f.State.UndoCount;
        Assert.True(await f.Workflow.DuplicateBeatChannelAsync(f.Basis, part.Id), f.State.Detail);
        var copy = f.Pattern.Parts[1];
        Assert.Equal(part.Name + " 2", copy.Name);
        Assert.NotEqual(part.Id, copy.Id);
        Assert.NotEqual(part.SoundId, copy.SoundId);
        Assert.Equal(part.Notes.Select(n => (n.Position, n.Duration, n.Pitch, n.Intensity)),
            copy.Notes.Select(n => (n.Position, n.Duration, n.Pitch, n.Intensity)));
        Assert.Empty(part.Notes.Select(n => n.Id).Intersect(copy.Notes.Select(n => n.Id)));
        var copySound = f.State.Snapshot.State.Sounds.Single(s => s.Id == copy.SoundId);
        Assert.Equal(sound.Parameters, copySound.Parameters);
        Assert.Equal(sound.ResourceIds, copySound.ResourceIds);
        Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.True(GraphDiagnostics.Inspect(f.State.Snapshot.State).Single().IsEligibleForPreparation);
        Assert.Equal(copy.Id,
            f.State.Snapshot.State.GraphAttachments.Single().Sources.Single(s => s.PartId == copy.Id).PartId);
        var duplicated = f.State.Snapshot;
        float[] doubled = f.Render(rate);
        Assert.Equal(baseline.Length, doubled.Length);
        for (int i = 0; i < baseline.Length; i++) Assert.InRange(Math.Abs(doubled[i] - baseline[i] * 2), 0, 1e-6);
        Assert.True(await f.Workflow.MoveBeatChannelAsync(f.Basis, copy.Id, 0));
        Assert.Equal(copy.Id, f.Pattern.Parts[0].Id);
        Assert.Equal(doubled, f.Render(rate));
        Assert.Equal(undo + 2, f.State.UndoCount);
        Assert.False(await f.Workflow.MoveBeatChannelAsync(f.Basis, copy.Id, 0));
        Assert.Equal(undo + 2, f.State.UndoCount);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Same(duplicated, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true));
        Assert.Same(duplicated, f.State.Snapshot);
        var parameters = copySound.Parameters.SetItem(algorithm == PcmSampler.Algorithm ? "rootPitch" :
            algorithm == SynthVoice.Algorithm ? "cutoff" : "tune", algorithm == SynthVoice.Algorithm ? 300 :
            algorithm == PcmSampler.Algorithm ? 72 : 7);
        Assert.True(await f.Edit(e => e.SetSoundParameters(copy.SoundId, parameters)));
        Assert.Equal(sound, f.State.Snapshot.State.Sounds.Single(s => s.Id == sound.Id));
        Assert.NotEqual(sound.Parameters, f.State.Snapshot.State.Sounds.Single(s => s.Id == copy.SoundId).Parameters);
        var changedCopy = f.State.Snapshot.State.Sounds.Single(s => s.Id == copy.SoundId);
        Assert.True(BuiltInSound.Supports(changedCopy));
        Assert.False(doubled.SequenceEqual(f.Render(rate)));
        Assert.True(await f.Edit(e => e.SetSoundParameters(sound.Id, parameters)));
        Assert.Equal(changedCopy, f.State.Snapshot.State.Sounds.Single(s => s.Id == copy.SoundId));
    }

    [Fact]
    public async Task MappedCopyHasFreshZoneIdsSharedMediaIndependentSettingsAndPortableHistory()
    {
        await using var f = new Fixture();
        await f.Create(PcmSampler.Algorithm);
        var sound = f.State.Snapshot.State.Sounds.Single();
        var original = f.Pattern.Parts.Single();
        var values = ImmutableDictionary<string, decimal>.Empty.Add("sampleStart", 100).Add("sampleEnd", 18000)
            .Add("loopEnabled", 1).Add("loopStart", 2000).Add("loopEnd", 10000).Add("loopCrossfadeFrames", 400);
        var zones = new[]
        {
            new SamplerZone(Id<SamplerZone>.New(), sound.ResourceIds.Single(), 60, 0, 63, values),
            new SamplerZone(Id<SamplerZone>.New(), sound.ResourceIds.Single(), 68, 64, 127, values)
        }.ToImmutableArray();
        Assert.True(await f.Edit(e =>
        {
            e.SetSamplerZones(sound.Id, zones);
            e.InitializeMixer();
        }));
        int undo = f.State.UndoCount;
        Assert.True(await f.Workflow.DuplicateBeatChannelAsync(f.Basis, original.Id, 0), f.State.Detail);
        var copy = f.Pattern.Parts[0];
        var copySound = f.State.Snapshot.State.Sounds.Single(s => s.Id == copy.SoundId);
        Assert.Empty(zones.Select(z => z.Id).Intersect(copySound.SamplerZones.Select(z => z.Id)));
        Assert.Equal(zones.Select(z => (z.ResourceId, z.RootPitch, z.LowKey, z.HighKey, z.Parameters)),
            copySound.SamplerZones.Select(z => (z.ResourceId, z.RootPitch, z.LowKey, z.HighKey, z.Parameters)));
        Assert.Single(f.State.Snapshot.State.Resources);
        Assert.Contains(copy.SoundId, f.State.Snapshot.State.Mixer!.Channels.Single().SoundIds);
        Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.True(await f.Edit(e => e.SetSamplerZones(copy.SoundId,
            copySound.SamplerZones.SetItem(0, copySound.SamplerZones[0] with { RootPitch = 62 }))));
        Assert.Equal(60, f.State.Snapshot.State.Sounds.Single(s => s.Id == sound.Id).SamplerZones[0].RootPitch);
        string path = f.Directory.File("portable.seqvium");
        Assert.True(await f.Workflow.SaveAsync(path), f.State.Detail);
        File.Delete(f.Directory.File("source.wav"));
        var canonical = ProjectPersistence.Encode(f.State.Document);
        Assert.True(await f.Workflow.NewAsync(UnsavedDecision.Discard));
        Assert.True(await f.Workflow.OpenAsync(path, UnsavedDecision.Discard), f.State.Detail);
        Assert.Equal(canonical, ProjectPersistence.Encode(f.State.Document));
        Assert.Equal(copy.Id, f.Pattern.Parts[0].Id);
        Assert.Contains(f.Render(48000), s => Math.Abs(s) > .001);
    }

    [Theory]
    [InlineData("sound")]
    [InlineData("mixer")]
    [InlineData("note")]
    [InlineData("graph")]
    [InlineData("shared")]
    [InlineData("capacity")]
    public async Task UnsafeDuplicateAndStaleOrAbaCommandsLeaveSnapshotAndHistoryUntouched(string boundary)
    {
        await using var f = new Fixture();
        await f.Create(SynthVoice.Algorithm);
        var part = f.Pattern.Parts.Single();
        var stale = f.Basis;
        using var json = JsonDocument.Parse("{\"future\":true}");
        var opaque = ImmutableDictionary<string, JsonElement>.Empty.Add("future", json.RootElement.Clone());
        if (boundary == "capacity")
            for (int i = 1; i < GraphDiagnostics.MaximumSources; i++)
                Assert.True(await f.Workflow.DuplicateBeatChannelAsync(f.Basis, part.Id));
        else
            Assert.True(await f.Edit(e =>
            {
                if (boundary == "sound")
                    e.SetSoundParameters(part.SoundId,
                        f.State.Snapshot.State.Sounds.Single().Parameters.Add("future", 1));
                if (boundary == "mixer")
                {
                    e.InitializeMixer();
                    e.SetMixer(new([], new()) { AdditionalData = opaque });
                }

                if (boundary == "note")
                    e.SetPartNotes(f.Pattern.Id, part.Id, [part.Notes[0] with { AdditionalData = opaque }]);
                if (boundary == "shared") e.AddPlacement(f.Pattern.Id, new(0));
                if (boundary == "graph")
                    e.AddGraphNode(f.State.Snapshot.State.Graphs.Single().Id, GraphBuiltIns.Gain, new(800, 0));
            }));
        var before = f.State.Snapshot;
        int undo = f.State.UndoCount;
        Assert.False(await f.Workflow.DuplicateBeatChannelAsync(f.Basis, part.Id));
        Assert.Same(before, f.State.Snapshot);
        Assert.Equal(undo, f.State.UndoCount);
        Assert.False(await f.Workflow.MoveBeatChannelAsync(stale, part.Id, 0));
        Assert.False(await f.Workflow.DuplicateBeatChannelAsync(stale, part.Id));
        Assert.Same(before, f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.True(await f.Workflow.UndoAsync(true));
        Assert.False(await f.Workflow.DuplicateBeatChannelAsync(stale, part.Id));
        Assert.Same(before, f.State.Snapshot);
    }
}
