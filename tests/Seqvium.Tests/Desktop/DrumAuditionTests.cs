// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class DrumAuditionTests
{
    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Assert.True(Joined);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly DesktopWorkflow Workflow;
        internal int Opens;
        internal bool FailOpen;
        internal Action? Opening;
        internal RealtimeSampler? Sampler;
        internal Borrower? Output;

        internal Fixture(int rate = 48000) => Workflow = new(ProjectDocument.Create(), () =>
        {
            Opens++;
            if (FailOpen) throw new IOException("Test output unavailable");
            Output = new();
            Sampler = new(rate, 2, 256);
            Opening?.Invoke();
            return new(Output, Sampler, "Controlled output", () => null);
        });

        internal BeatEditBasis Basis => BeatSteps.Resolve(Workflow.State).Basis!;

        internal NodeEditorTarget Target => new(Workflow.State.Document.LifecycleId, Basis.Attachment,
            Workflow.State.Snapshot.State.GraphAttachments.Single().GraphId,
            Workflow.State.Snapshot.State.GraphAttachments.Single().Sources.Single().NodeId);

        internal async Task Create(string key = "Clap")
        {
            Assert.True(await Workflow.CreateBeatAsync(Workflow.State.Document.LifecycleId, Workflow.State.Generation,
                "Drums"));
            Assert.True(await Workflow.AddDrumVoiceAsync(Basis, key,
                key == "Synth" ? SynthVoice.Algorithm : DrumInstruments.All.Single(i => i.Key == key).Algorithm));
        }

        internal DrumProposal Proposal(string parameter = "amplitudeCurve", decimal value = 3)
        {
            var session = new DrumEditorSession();
            session.Observe(Workflow.State, Target);
            Assert.True(session.Begin());
            session.Set(parameter, value);
            return session.Complete()!;
        }

        internal float[] Tick()
        {
            var buffer = new float[512];
            Assert.True(Sampler!.Process(buffer));
            Sampler.RetireCompleted();
            return buffer;
        }

        public ValueTask DisposeAsync() => Workflow.DisposeAsync();
    }

    [Theory]
    [InlineData("attack", 2)]
    [InlineData("amplitudeCurve", 3)]
    [InlineData("tune", 7)]
    public async Task OnlyAcceptedChangedCommitAuditionsAndUndoDoesNotRetrigger(string key, decimal value)
    {
        await using var f = new Fixture();
        await f.Create();
        int history = f.Workflow.State.UndoCount;
        var before = f.Workflow.State.Snapshot;
        var proposal = f.Proposal(key, value);
        Assert.Equal(0, f.Opens);
        Assert.True(await f.Workflow.ShapeAndAuditionDrumAsync(proposal, 36, .8m));
        Assert.Equal(history + 1, f.Workflow.State.UndoCount);
        Assert.Equal(value, f.Workflow.State.Snapshot.State.Sounds.Single().Parameters[key]);
        Assert.Contains(f.Tick(), sample => sample != 0);
        long epoch = f.Sampler!.TransportEpoch;
        Assert.Equal(1, f.Opens);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Equal(before.Revision, f.Workflow.State.Snapshot.Revision);
        f.Tick();
        Assert.Equal(epoch, f.Sampler.TransportEpoch);
        Assert.False(await f.Workflow.ShapeAndAuditionDrumAsync(proposal, 36, .8m));
        Assert.Equal(1, f.Opens);
        var unchanged = f.Proposal() with { After = f.Workflow.State.Snapshot.State.Sounds.Single().Parameters };
        Assert.False(await f.Workflow.ShapeAndAuditionDrumAsync(unchanged, 36, .8m));
        f.Tick();
        Assert.Equal(epoch, f.Sampler.TransportEpoch);
    }

    [Fact]
    public async Task PreparationKeepsLatestIntentAndReadyHitsOverlapOnOneOutput()
    {
        await using var f = new Fixture();
        await f.Create();
        var target = f.Target;
        long generation = f.Workflow.State.Generation;
        var burst = Enumerable.Range(0, 8).Select(_ => f.Workflow.AuditionDrumAsync(target, generation, 36, .8m))
            .ToArray();
        var results = await Task.WhenAll(burst);
        Assert.True(results[^1]);
        Assert.Equal(1, f.Opens);
        Assert.Contains(f.Tick(), sample => sample != 0);
        long created = f.Sampler!.PreparedStatesCreated;
        long epoch = f.Sampler.DrumAuditionHits;
        for (int i = 0; i < 8; i++)
        {
            Assert.True(await f.Workflow.AuditionDrumAsync(target, generation, 36, .8m));
            Assert.Contains(f.Tick(), sample => sample != 0);
            Assert.True(f.Sampler.DrumAuditionHits > epoch);
            epoch = f.Sampler.DrumAuditionHits;
        }

        Assert.Equal(1, f.Opens);
        Assert.Equal(created, f.Sampler.PreparedStatesCreated);
        Assert.False(f.Output!.Joined);
        long transport = f.Workflow.State.TransportGeneration;
        Assert.True(await f.Workflow.StopAsync());
        Assert.False(await f.Workflow.AuditionDrumAsync(target, generation, 36, .8m, transport));
        var output = new float[512];
        f.Sampler!.Process(output);
        Assert.All(output, sample => Assert.Equal(0, sample));
        Assert.True(await f.Workflow.NewAsync(UnsavedDecision.Discard));
        Assert.True(f.Output.Joined);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationInvalidatesPreparationAndPendingIntent(bool panic)
    {
        await using var f = new Fixture();
        await f.Create();
        Task<bool>? cancelled = null, pending = null;
        f.Opening = () =>
        {
            pending = f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 48, .8m);
            cancelled = panic ? f.Workflow.StopAsync(true) : f.Workflow.CancelDrumAuditionAsync();
        };
        Assert.False(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
        Assert.True(await cancelled!);
        Assert.False(await pending!);
        var output = new float[512];
        f.Sampler!.Process(output);
        Assert.All(output, sample => Assert.Equal(0, sample));
    }

    [Fact]
    public async Task ClicksDuringPreparationCoalesceToOneLatestSoundInsteadOfDelayedHits()
    {
        await using var f = new Fixture();
        await f.Create();
        Task<bool>[] pending = [];
        f.Opening = () => pending = Enumerable.Range(0, 16).Select(i =>
            f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36 + i, .8m)).ToArray();
        Assert.False(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
        var results = await Task.WhenAll(pending);
        Assert.All(results[..^1], result => Assert.False(result));
        Assert.True(results[^1]);
        Assert.Equal(1, f.Opens);
        Assert.Equal(0, f.Sampler!.PreparedStatesCreated);
        Assert.Contains(f.Tick(), sample => sample != 0);
        long epoch = f.Sampler.DrumAuditionHits;
        for (int i = 0; i < 12; i++) f.Tick();
        Assert.Equal(epoch, f.Sampler.DrumAuditionHits);
    }

    [Fact]
    public async Task ProjectCloseWithdrawsBothPreparingAndPendingDrumHits()
    {
        await using var f = new Fixture();
        await f.Create();
        Task<bool>? closed = null, pending = null;
        f.Opening = () =>
        {
            pending = f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 48, .8m);
            closed = f.Workflow.CloseAsync(UnsavedDecision.Discard);
        };
        Assert.False(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
        Assert.True(await closed!);
        Assert.False(await pending!);
        Assert.True(f.Output!.Joined);
        Assert.True(f.Sampler!.IsTerminated);
        Assert.False(await f.Workflow.CancelDrumAuditionAsync());
    }

    [Fact]
    public async Task PianoNoteAndDrumAuditionTransferOutputOwnershipAndCloseJoins()
    {
        await using var f = new Fixture();
        await f.Create();
        var part = BeatSteps.Resolve(f.Workflow.State).Pattern!.Parts.Single();
        Assert.True(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
        var drum = f.Sampler!;
        Assert.Contains(f.Tick(), sample => sample != 0);
        Assert.True(await f.Workflow.AuditionNoteAsync(f.Basis, part.Id, 48, .6m));
        Assert.True(drum.IsTerminated);
        Assert.Contains(f.Tick(), sample => sample != 0);
        var piano = f.Sampler!;
        Assert.True(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
        Assert.True(piano.IsTerminated);
        Assert.Contains(f.Tick(), sample => sample != 0);
        Assert.True(await f.Workflow.CloseAsync(UnsavedDecision.Discard));
        Assert.True(f.Output!.Joined);
        Assert.False(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
    }

    [Fact]
    public async Task FailedOutputPreservesAcceptedMusicalEditAndItsUndo()
    {
        await using var f = new Fixture();
        await f.Create();
        var before = f.Workflow.State.Snapshot;
        f.FailOpen = true;
        Assert.True(await f.Workflow.ShapeAndAuditionDrumAsync(f.Proposal(), 36, .8m));
        Assert.Equal(3, f.Workflow.State.Snapshot.State.Sounds.Single().Parameters["amplitudeCurve"]);
        Assert.Equal("Workflow.ActionFailed", f.Workflow.State.StatusKey);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Equal(before.Revision, f.Workflow.State.Snapshot.Revision);
    }

    [Fact]
    public async Task LocalStopCutsLongDrumImmediatelyAndPlayCanRestartInTheSameOutput()
    {
        await using var f = new Fixture();
        await f.Create();
        Assert.True(await f.Workflow.ShapeAndAuditionDrumAsync(f.Proposal("decay", 1200), 36, .8m));
        Assert.Contains(f.Tick(), sample => sample != 0);
        Assert.True(f.Sampler!.DrumAuditionVoices > 0);
        long epoch = f.Sampler.DrumAuditionHits;
        Assert.True(await f.Workflow.CancelDrumAuditionAsync());
        Assert.All(f.Tick(), sample => Assert.Equal(0, sample));
        Assert.Equal(0, f.Sampler.DrumAuditionVoices);
        Assert.True(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
        Assert.Contains(f.Tick(), sample => sample != 0);
        Assert.True(f.Sampler.DrumAuditionHits > epoch);
        Assert.Equal(1, f.Opens);
    }

    [Fact]
    public async Task StopDuringAutomaticAuditionPreparationPreventsLateSound()
    {
        await using var f = new Fixture();
        await f.Create();
        Task<bool>? stopped = null;
        f.Opening = () => stopped = f.Workflow.StopAsync();
        Assert.True(await f.Workflow.ShapeAndAuditionDrumAsync(f.Proposal(), 36, .8m));
        Assert.True(await stopped!);
        Assert.All(f.Tick(), sample => Assert.Equal(0, sample));
        Assert.Equal(0, f.Sampler!.DrumAuditionHits);
        Assert.False(f.Output!.Joined); // A healthy, silent output remains reusable until joined Close.
    }

    [Fact]
    public async Task ActiveMusicalPlaybackAcceptsEditWithoutStartingSoloAudition()
    {
        await using var f = new Fixture();
        await f.Create();
        var part = BeatSteps.Resolve(f.Workflow.State).Pattern!.Parts.Single();
        Assert.True(await f.Workflow.ToggleBeatStepAsync(f.Basis, part.Id, 0));
        var play = f.Workflow.PlayAsync();
        while (!play.IsCompleted)
        {
            f.Sampler?.Process(Span<float>.Empty);
            await f.Workflow.ObserveAsync();
            await Task.Yield();
        }

        Assert.True(await play);
        f.Tick();
        int opens = f.Opens;
        Assert.True(await f.Workflow.ShapeAndAuditionDrumAsync(f.Proposal(), 36, .8m));
        Assert.Equal(opens, f.Opens);
        Assert.True(f.Workflow.State.Execution!.Execution.Playing);
    }

    [Fact]
    public async Task SourceRemovalAndUndoCannotResurrectPreviewOrPendingHits()
    {
        await using var f = new Fixture();
        await f.Create();
        var target = f.Target;
        var part = BeatSteps.Resolve(f.Workflow.State).Pattern!.Parts.Single();
        Assert.True(await f.Workflow.AuditionDrumAsync(target, f.Workflow.State.Generation, 36, .8m));
        f.Tick();
        Assert.True(f.Sampler!.DrumAuditionVoices > 0);
        Assert.True(await f.Workflow.DeleteBeatChannelAsync(f.Basis, part.Id));
        Assert.All(f.Tick(), s => Assert.Equal(0, s));
        long hits = f.Sampler.DrumAuditionHits;
        Assert.True(await f.Workflow.UndoAsync());
        Assert.All(f.Tick(), s => Assert.Equal(0, s));
        Assert.Equal(hits, f.Sampler.DrumAuditionHits);
    }

    public static TheoryData<string, int> SourceLevels => new(
        from key in DrumInstruments.All.Select(i => i.Key).Append("Synth")
        from rate in new[] { 44100, 48000 }
        select (key, rate));

    [Theory]
    [MemberData(nameof(SourceLevels))]
    public async Task EditorPreviewAndUnityPatternHaveTheSameSourceLevel(string key, int rate)
    {
        await using var f = new Fixture(rate);
        await f.Create(key);
        var part = BeatSteps.Resolve(f.Workflow.State).Pattern!.Parts.Single();
        Assert.True(await f.Workflow.EditPianoNotesAsync(f.Basis, part.Id,
            [new(Id<NoteEvent>.New(), new(0), new(1920000), key == "Synth" ? 60 : 36, .8m)]));
        Assert.True(await f.Workflow.SetGainAsync(1));
        using var plan = GraphPreparation.Prepare(f.Workflow.State.Document, f.Basis.Attachment, rate, 2, 256,
            cancellationToken: TestContext.Current.CancellationToken);
        using var pattern = plan.CreateExecution();
        if (key == "Synth")
            Assert.True(await f.Workflow.HoldSynthAsync(f.Target, f.Workflow.State.Generation, 60, .8m));
        else Assert.True(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
        var expected = new float[512];
        double actualEnergy = 0, expectedEnergy = 0;
        for (int i = 0; i < (key == "Synth" ? 100 : 10); i++)
        {
            pattern.Process(expected);
            var actual = f.Tick();
            for (int sample = 0; sample < actual.Length; sample++)
                if (key != "Synth") Assert.Equal(expected[sample], actual[sample], 6);
                else if (i >= 90)
                {
                    actualEnergy += actual[sample] * actual[sample];
                    expectedEnergy += expected[sample] * expected[sample];
                }
        }

        // Held preview retains its existing 5-ms envelope smoothing. Compare settled sustain,
        // not an attack transient whose deliberate smoothing differs from a scheduled note.
        if (key == "Synth") Assert.InRange(Math.Sqrt(actualEnergy / expectedEnergy), .999, 1.001);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlayAlongPreservesMusicalExecutionAndLocalStopOnlyCutsPreview(bool song)
    {
        await using var f = new Fixture();
        await f.Create();
        var part = BeatSteps.Resolve(f.Workflow.State).Pattern!.Parts.Single();
        Assert.True(await f.Workflow.ToggleBeatStepAsync(f.Basis, part.Id, 0));
        Assert.True(await f.Workflow.SetGainAsync(1));
        if (song)
        {
            Assert.True(await f.Workflow.AddArrangementClipAsync(new(f.Workflow.State.Document.LifecycleId,
                f.Workflow.State.Generation), f.Basis.Attachment, new(0), 0, 4));
            Assert.True(await f.Workflow.SetSongModeAsync(true));
        }

        var play = f.Workflow.PlayAsync();
        for (int i = 0; i < 20000 && !play.IsCompleted; i++)
        {
            f.Sampler?.Process(Span<float>.Empty);
            await f.Workflow.ObserveAsync();
        }

        Assert.True(await play);
        f.Tick();
        var before = f.Sampler!.ReadStatus();
        var snapshot = f.Workflow.State.Snapshot;
        int history = f.Workflow.State.UndoCount;
        var busy = new List<bool>();
        f.Workflow.Changed += Observe;
        void Observe(WorkflowState s) => busy.Add(s.Busy);
        for (int hit = 0; hit < 6; hit++)
        {
            Assert.True(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .4m));
            f.Tick();
        }

        f.Workflow.Changed -= Observe;
        Assert.DoesNotContain(true, busy);
        Assert.Equal(6, f.Sampler.DrumAuditionHits);
        Assert.Equal(6, f.Sampler.DrumAuditionVoices);
        Assert.Equal(1, f.Opens);
        Assert.Equal(snapshot, f.Workflow.State.Snapshot);
        Assert.Equal(history, f.Workflow.State.UndoCount);
        Assert.True(await f.Workflow.CancelDrumAuditionAsync());
        f.Tick();
        var after = f.Sampler.ReadStatus();
        Assert.Equal(0, f.Sampler.DrumAuditionVoices);
        Assert.Equal(before.Epoch, after.Epoch);
        Assert.Equal(before.PreparedExecutionId, after.PreparedExecutionId);
        Assert.Equal(before.Position + 7 * 256, after.Position);
        Assert.True(after.Playing);
        Assert.True(await f.Workflow.StopAsync(panic: true));
        Assert.All(f.Tick(), s => Assert.Equal(0, s));
        Assert.False(f.Sampler.ReadStatus().Playing);
        Assert.Equal(0, f.Sampler.DrumAuditionVoices);
    }

    [Fact]
    public async Task PlayAlongLeavesMixerHistoryAndMeasuredTapsBitIdenticalToUninterruptedMusic()
    {
        await using var f = new Fixture();
        await f.Create();
        var part = BeatSteps.Resolve(f.Workflow.State).Pattern!.Parts.Single();
        Assert.True(await f.Workflow.ToggleBeatStepAsync(f.Basis, part.Id, 0));
        MixerEditBasis MixerBasis() => new(f.Workflow.State.Document.LifecycleId, f.Workflow.State.Generation);
        Assert.True(await f.Workflow.EditMixerAsync(MixerBasis(), "Mixer", e => e.InitializeMixer()));
        var channel = f.Workflow.State.Snapshot.State.Mixer!.Channels.Single();
        Assert.True(await f.Workflow.EditMixerAsync(MixerBasis(), "FX", e => e.SetMixerStrip(channel.Id,
            channel.Settings with
            {
                Delay = new() { Enabled = true, TimeMs = 20, Mix = .4m },
                Reverb = new() { Enabled = true, Mix = .2m }
            })));
        var play = f.Workflow.PlayAsync();
        while (!play.IsCompleted)
        {
            f.Sampler?.Process(Span<float>.Empty);
            await f.Workflow.ObserveAsync();
        }

        Assert.True(await play);
        using var baseline = new RealtimeSampler(48000, 2, 256);
        using (var request = baseline.BeginGraphPreparation(f.Workflow.State.Document, f.Basis.Attachment,
                   voiceCapacity: GraphPreparation.MaximumVoices))
        {
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }

        baseline.SetLooping(true);
        baseline.Start();
        baseline.Process(Span<float>.Empty);
        var expected = new float[512];
        for (int i = 0; i < 25; i++)
        {
            baseline.Process(expected);
            Assert.Equal(expected, f.Tick());
        }

        Assert.True(await f.Workflow.AuditionDrumAsync(f.Target, f.Workflow.State.Generation, 36, .8m));
        double difference = 0;
        for (int i = 0; i < 20; i++)
        {
            baseline.Process(expected);
            var actual = f.Tick();
            difference += actual.Zip(expected).Sum(p => Math.Abs(p.First - p.Second));
            Assert.Equal(baseline.ReadMixerMeter(), f.Sampler!.ReadMixerMeter());
            Assert.Equal(baseline.ReadMixerMeter(channel.Id), f.Sampler.ReadMixerMeter(channel.Id));
            Assert.Equal(baseline.ActiveVoices, f.Sampler.ActiveVoices);
        }

        Assert.True(difference > 1);
        Assert.True(await f.Workflow.CancelDrumAuditionAsync());
        for (int i = 0; i < 25; i++)
        {
            baseline.Process(expected);
            Assert.Equal(expected, f.Tick());
        }
    }

    [Fact]
    public async Task RetriggerWaitsForItsPreparedAuthorityUnderRetirementBackpressure()
    {
        await using var f = new Fixture();
        await f.Create();
        using var sampler = new RealtimeSampler(48000, 2, 256);
        var buffer = new float[512];
        var sound = f.Workflow.State.Snapshot.State.Sounds.Single();

        async Task Publish(decimal pitch)
        {
            using var request = new NoteAuditionRequest(sampler, f.Workflow.State.Document, sound.Id, pitch, .8m);
            await request.PrepareAsync();
            Assert.Equal(SamplerPublication.Accepted, request.Publish());
        }

        Assert.False(sampler.RetriggerAudition());
        await Publish(36);
        sampler.Process(buffer);
        await Publish(48);
        sampler.Process(buffer); // First state occupies the retirement slot.
        long epoch = sampler.TransportEpoch;
        await Publish(60);
        Assert.True(sampler.HasPending);
        Assert.True(sampler.RetriggerAudition());
        sampler.Process(buffer);
        Assert.Equal(epoch, sampler.TransportEpoch); // Cannot restart the older active voice.
        sampler.RetireCompleted();
        sampler.Process(buffer);
        Assert.True(sampler.TransportEpoch > epoch);
        Assert.False(sampler.HasPending);
        sampler.InvalidatePreparation();
        Assert.False(sampler.RetriggerAudition());
        sampler.TerminateExecution();
        Assert.False(sampler.RetriggerAudition());
    }
}
