// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class AutomationTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly SynthVoiceTests.Fixture Synth = new(SynthVoice.Defaults.SetItem("attack", 0)
            .SetItem("decay", 0).SetItem("sustain", 1).SetItem("release", 0).SetItem("mix", 0)
            .SetItem("cutoff", 18000).SetItem("resonance", 0));

        internal ProjectDocument Document => Synth.Document;
        internal AutomationLane Lane => Document.Current.State.AutomationLanes[0];
        internal Id<MixerChannel> Channel => Document.Current.State.Mixer!.Channels[0].Id;

        internal Fixture(bool volume = false, int voices = 3)
        {
            Document.Edit("Held four-bar chord", e =>
            {
                e.SetPatternLength(Synth.Pattern, new(15360000));
                e.SetPartNotes(Synth.Pattern, Synth.Part, [
                    .. Enumerable.Range(0, voices).Select(i =>
                        new NoteEvent(Id<NoteEvent>.New(), new(0), new(15360000), 48 + i * 4, .35m))
                ]);
                e.AddArrangementClip(Document.Current.State.Placements.Single().Id, new(0), 0, 2);
                if (volume) e.InitializeMixer();
            });
        }

        internal void Automate(bool volume, decimal start = 0, decimal end = 1, long ticks = 15360000,
            AutomationTransition transition = AutomationTransition.Linear)
        {
            Document.Edit("Draw envelope", e => e.AddAutomationLane(1, "Chord", volume
                    ? new(AutomationParameter.MixerVolume, ChannelId: Channel)
                    : new(AutomationParameter.SynthCutoff, Synth.Sound),
                [
                    new(Id<AutomationPoint>.New(), new(0), start, transition),
                    new(Id<AutomationPoint>.New(), new(ticks), end)
                ]));
        }

        internal PreparedSampler Prepare(int rate = 48000, int packet = 511) => ArrangementPreparation.Prepare(Document,
            rate, 2, packet, TestContext.Current.CancellationToken);

        public void Dispose()
        {
            if (!Document.IsClosed) Synth.Dispose();
        }
    }

    private static float[] Render(PreparedSampler plan, int packet = 127, bool smooth = false)
    {
        using var execution = plan.CreateExecution(smooth);
        var pcm = new float[checked((int)plan.EndFrame * plan.Channels)];
        while (execution.Position < execution.EndFrame)
        {
            int frames = (int)Math.Min(packet, execution.EndFrame - execution.Position);
            execution.Process(pcm.AsSpan((int)execution.Position * plan.Channels, frames * plan.Channels));
        }

        return pcm;
    }

    private static async Task Publish(Fixture f, RealtimeSampler sampler, bool preserve = false)
    {
        using var request = sampler.BeginArrangementPreparation(f.Document, preserve);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void HeldChordOpensSpectrallyWithoutNoteEventsOrBaseMutationAndRepeatsUseSongTime(int rate)
    {
        using var f = new Fixture();
        var sound = f.Synth.Definition;
        f.Automate(false, .15m, .95m, transition: AutomationTransition.Smooth);
        using var plan = f.Prepare(rate);
        var pcm = Render(plan);

        double Brightness(int at)
        {
            double power = 0, difference = 0;
            for (int i = at; i < at + rate / 4; i++)
            {
                double x = pcm[i * 2];
                power += x * x;
                difference += Math.Pow(x - pcm[(i - 1) * 2], 2);
            }

            return difference / power;
        }

        Assert.True(Brightness(rate * 7) > Brightness(rate) * 3);
        Assert.Equal(sound.Parameters, f.Synth.Definition.Parameters);
        Assert.Equal(6, plan.Events.Count(e => e.Kind == ExecutionEventKind.NoteOn));
        Assert.All(pcm, sample => Assert.True(float.IsFinite(sample) && Math.Abs(sample) < 1));
        Assert.Equal(pcm, Render(plan, 509)); // No packet-sized control steps.
        using var held = plan.CreateExecution();
        held.Process(new float[1022]);
        Assert.Equal(3, held.ActiveVoices);
        Assert.Contains(pcm.Skip(rate * 2).Take(rate), x => x != 0);
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void MixerVolumeMatchesIndependentSmoothedFaderOracleAndRespectsMute(int rate)
    {
        using var f = new Fixture(true, 1);
        using var dryPlan = f.Prepare(rate);
        var dry = Render(dryPlan);
        var settings = f.Document.Current.State.Mixer!;
        f.Automate(true, .2m, .85m);
        using var wetPlan = f.Prepare(rate);
        var wet = Render(wetPlan);
        double gain = Math.Pow(10, (-90 + .2 * 102) / 20), alpha = 1 - Math.Exp(-1 / (.005 * rate));
        double maximumError = 0;
        for (int frame = 0; frame < wet.Length / 2; frame++)
        {
            double norm = .2 + .65 * Math.Min(1, frame / (8d * rate));
            double target = Math.Pow(10, (-90 + norm * 102) / 20);
            if (frame > 0) gain += (target - gain) * alpha;
            maximumError = Math.Max(maximumError, Math.Abs(wet[frame * 2] - dry[frame * 2] * gain));
        }

        Assert.InRange(maximumError, 0, 1e-6);
        Assert.Equal(settings, f.Document.Current.State.Mixer);
        f.Document.Edit("Mute channel",
            e => e.SetMixerStrip(f.Channel, settings.Channels[0].Settings with { Muted = true }));
        using var muted = f.Prepare(rate);
        Assert.All(Render(muted), x => Assert.Equal(0, x));
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public async Task RealtimeOfflineSeekAndRangeWrapUseIdenticalColdDestinationValues(int rate)
    {
        using var f = new Fixture(true);
        f.Automate(true, .1m, .9m);
        f.Document.Edit("Filter too", e => e.AddAutomationLane(2, "Chord",
            new(AutomationParameter.SynthCutoff, f.Synth.Sound),
            [
                new(Id<AutomationPoint>.New(), new(0), .2m),
                new(Id<AutomationPoint>.New(), new(15360000), .9m, AutomationTransition.Smooth)
            ]));
        using var plan = f.Prepare(rate, 128);
        using var sampler = new RealtimeSampler(rate, 2, 128);
        await Publish(f, sampler);
        sampler.Start();
        var packet = new float[256];
        using var reference = plan.CreateExecution();
        for (int n = 0; n < 20; n++)
        {
            var expected = new float[256];
            reference.Process(expected);
            sampler.Process(packet);
            Assert.Equal(expected, packet);
        }

        long start = rate * 2, end = start + 128 * 4;
        sampler.SeekSong(start);
        sampler.SetSongLoop(start, end, true);
        sampler.Process(Span<float>.Empty); // Adopt seek and finish its explicit fade separately.
        reference.Seek(start);
        for (int n = 0; n < 12; n++)
        {
            if (reference.Position == end) reference.Seek(start);
            var expected = new float[256];
            reference.Process(expected);
            if (n == 0)
                for (int frame = 0; frame < sampler.GraphTransitionFrames; frame++)
                for (int channel = 0; channel < 2; channel++)
                    expected[frame * 2 + channel] *= (frame + 1f) / sampler.GraphTransitionFrames;
            sampler.Process(packet);
            Assert.Equal(expected, packet);
        }

        sampler.Stop();
        sampler.SeekSong(rate);
        sampler.Process(packet);
        Assert.False(sampler.ReadStatus().Playing);
        Assert.All(packet, x => Assert.Equal(0, x));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CurveEditEnableDeleteUndoRetainHeldVoicesClockAndPrimedCallbackAllocatesNothing(bool volume)
    {
        using var f = new Fixture(volume);
        f.Automate(volume, .4m, .7m);
        using var sampler = new RealtimeSampler(48000, 2, 128);
        await Publish(f, sampler);
        sampler.Start();
        var packet = new float[256];
        sampler.Process(packet);
        long epoch = sampler.TransportEpoch;
        for (int phase = 0; phase < 4; phase++)
        {
            if (phase == 3) Assert.True(f.Document.Undo());
            var lane = f.Lane;
            if (phase == 0)
                f.Document.Edit("Move point", e => e.UpdateAutomationLane(lane with
                {
                    Points = lane.Points.SetItem(1, lane.Points[1] with { Value = .9m })
                }));
            if (phase == 1) f.Document.Edit("Bypass", e => e.UpdateAutomationLane(lane with { Enabled = false }));
            if (phase == 2) f.Document.Edit("Remove", e => e.DeleteAutomationLane(lane.Id));
            await Publish(f, sampler, true);
            long position = sampler.RenderPosition;
            sampler.Process(packet);
            sampler.RetireCompleted();
            Assert.Equal(epoch, sampler.TransportEpoch);
            Assert.Equal(position + 128, sampler.RenderPosition);
            Assert.Equal(3, sampler.ActiveVoices);
            for (int n = 0; n < 30; n++) sampler.Process(packet);
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 30; n++) sampler.Process(packet);
            Assert.Equal(bytes, GC.GetAllocatedBytesForCurrentThread());
        }
    }

    [Fact]
    public async Task SongWavIsQuantizedSameExecutorAndPatternObjectRenderExcludesSongAutomation()
    {
        using var f = new Fixture(true, 1);
        // Keep R8's output budget small while exercising the same synth/local tap.
        f.Document.Edit("One bar", e =>
        {
            e.SetPartNotes(f.Synth.Pattern, f.Synth.Part,
                [new(Id<NoteEvent>.New(), new(0), new(1920000), 60, .35m)]);
            e.SetPatternLength(f.Synth.Pattern, new(1920000));
        });
        var before =
            await PatternResampling.RenderAsync(f.Document, f.Synth.Attachment.Id,
                TestContext.Current.CancellationToken);
        f.Automate(true, .3m, .85m, 1920000);
        var after = await PatternResampling.RenderAsync(f.Document, f.Synth.Attachment.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(before.Wave, after.Wave);
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "song.wav");
        var exported =
            await PatternWaveExport.ExportArrangementAsync(f.Document, path, TestContext.Current.CancellationToken);
        using var plan = f.Prepare(48000, 1024);
        var expected = Render(plan, 1024, true);
        var wav = File.ReadAllBytes(path);
        Assert.Equal(plan.EndFrame, exported.Frames);
        Assert.Equal(0, exported.ClippedSamples);
        for (int i = 0; i < expected.Length; i++)
            Assert.InRange(Math.Abs(BitConverter.ToInt16(wav, 44 + i * 2) / 32768d - expected[i]), 0, 1d / 32768);
    }

    [Fact]
    public void HistoryNoOpUnknownFieldsAndReaderGateRoundTripWithoutAffectingLegacyEnvelope()
    {
        using var f = new Fixture();
        var legacy = JsonNode.Parse(ProjectPersistence.Encode(f.Document))!;
        Assert.Null(legacy["state"]!["automationLanes"]);
        Assert.Equal(11, legacy["minimumReaderMinor"]!.GetValue<int>());
        f.Automate(false);
        var lane = f.Lane;
        int undo = f.Document.UndoCount;
        Assert.False(f.Document.Edit("No-op", e => e.UpdateAutomationLane(lane with { Points = [.. lane.Points] })));
        Assert.Equal(undo, f.Document.UndoCount);
        Assert.True(f.Document.Undo());
        Assert.Empty(f.Document.Current.State.AutomationLanes);
        Assert.True(f.Document.Redo());
        Assert.Equal(lane, f.Lane);
        var json = JsonNode.Parse(ProjectPersistence.Encode(f.Document))!;
        Assert.Equal(12, json["minimumReaderMinor"]!.GetValue<int>());
        json["minimumReaderMinor"] = 11;
        Assert.Throws<ProjectFormatException>(() =>
            ProjectPersistence.Read(new MemoryStream(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(json))));
        json["minimumReaderMinor"] = 12;
        var automation = json["state"]!["automationLanes"]![0]!;
        automation["future"] = "kept";
        automation["target"]!["future"] = 42;
        automation["points"]![0]!["future"] = true;
        var reopened = ProjectPersistence
            .Read(new MemoryStream(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(json))).Document;
        try
        {
            var encoded = JsonNode.Parse(ProjectPersistence.Encode(reopened))!["state"]!["automationLanes"]![0]!;
            Assert.Equal("kept", encoded["future"]!.GetValue<string>());
            Assert.Equal(42, encoded["target"]!["future"]!.GetValue<int>());
            Assert.True(encoded["points"]![0]!["future"]!.GetValue<bool>());
            Assert.Equal("Arrangement.Unsupported", ArrangementPreparation.Refusal(reopened.Current.State));
        }
        finally
        {
            reopened.Close();
        }
    }

    [Fact]
    public void MissingSoundAndMixerTargetsStayIdentifiedAndNeverFollowNamesOrReassignment()
    {
        using var f = new Fixture(true);
        f.Automate(true);
        var lane = f.Lane;
        var old = f.Channel;
        f.Document.Edit("Replace channel with same name", e =>
        {
            e.RemoveMixerChannel(old);
            var id = e.AddMixerBus("Synth");
            e.AssignMixerSound(f.Synth.Sound, id);
        });
        Assert.False(ParameterAutomation.IsResolved(f.Document.Current.State, lane.Target));
        Assert.Equal(old, f.Lane.Target.ChannelId);
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "portable.seqvium");
        ProjectPersistence.Save(f.Document, path);
        var loaded = ProjectPersistence.Open(path);
        Assert.Contains(loaded.Diagnostics, x => x.Contains("Automation target", StringComparison.Ordinal));
        Assert.Equal(old, loaded.Document.Current.State.AutomationLanes[0].Target.ChannelId);
        loaded.Document.Close();
        Assert.True(f.Document.Undo());
        Assert.True(ParameterAutomation.IsResolved(f.Document.Current.State, f.Lane.Target));
        f.Document.Edit("Cutoff", e =>
        {
            e.DeleteAutomationLane(lane.Id);
            e.AddAutomationLane(1, "Synth", new(AutomationParameter.SynthCutoff, f.Synth.Sound), lane.Points);
        });
        f.Document.Edit("Change source kind",
            e => e.SetSoundParameters(f.Synth.Sound, ImmutableDictionary<string, decimal>.Empty));
        Assert.False(ParameterAutomation.IsResolved(f.Document.Current.State, f.Lane.Target));
    }

    [Theory]
    [InlineData("time")]
    [InlineData("value")]
    [InlineData("lane")]
    [InlineData("target")]
    [InlineData("points")]
    [InlineData("gates")]
    public void InvalidEditsRefuseAtomically(string failure)
    {
        using var f = new Fixture();
        f.Automate(false);
        var lane = f.Lane;
        var before = f.Document.Current;
        Assert.ThrowsAny<Exception>(() => f.Document.Edit("Invalid envelope", e =>
        {
            if (failure == "gates") { e.SetArrangementTrack(1, "Track", 0, false, true); return; }
            if (failure == "target")
            {
                e.AddAutomationLane(2, "Duplicate", lane.Target,
                    [.. lane.Points.Select(p => p with { Id = Id<AutomationPoint>.New() })]);
                return;
            }

            e.UpdateAutomationLane(failure switch
            {
                "time" => lane with { Points = lane.Points.SetItem(1, lane.Points[1] with { Position = new(0) }) },
                "value" => lane with { Points = lane.Points.SetItem(0, lane.Points[0] with { Value = 1.1m }) },
                "lane" => lane with { Lane = 0 },
                _ => lane with { Points = [] }
            });
        }));
        Assert.Same(before, f.Document.Current);
    }

    [Fact]
    public async Task WorkflowCapturesAuthorityAndSingleLogicalHistoryEntry()
    {
        using var f = new Fixture();
        await using var workflow = new DesktopWorkflow(f.Document);
        ArrangementEditBasis Basis() => ArrangementEditBasis.From(workflow.State);
        Assert.True(await workflow.EditMixerAsync(new(workflow.State.Document.LifecycleId, workflow.State.Generation),
            "Keep existing muted empty slot", e => e.SetArrangementTrack(1, "Reserved", 1, true, false)));
        Assert.True(await workflow.AddAutomationAsync(Basis(), new(AutomationParameter.SynthCutoff, f.Synth.Sound)));
        Assert.Equal(2, workflow.State.Snapshot.State.AutomationLanes.Single().Lane);
        var stale = Basis();
        var lane = workflow.State.Snapshot.State.AutomationLanes.Single();
        int count = workflow.State.UndoCount;
        Assert.True(await workflow.EditAutomationAsync(stale, lane with { Enabled = false }));
        Assert.Equal(count + 1, workflow.State.UndoCount);
        Assert.True(await workflow.UndoAsync());
        Assert.False(await workflow.EditAutomationAsync(stale, lane with { Enabled = false }));
        Assert.True(workflow.State.Snapshot.State.AutomationLanes.Single().Enabled);
    }

    [Fact]
    public void EndpointHoldSmoothDomainAndSubFrameOrderingAreExplicit()
    {
        using var f = new Fixture(); f.Automate(false);
        var lane = f.Lane with { Points = [
            new(Id<AutomationPoint>.New(), new(960000), .2m, AutomationTransition.Smooth),
            new(Id<AutomationPoint>.New(), new(2880000), .8m)] };
        Assert.Equal(.2, ParameterAutomation.ValueAt(lane, 0));
        Assert.Equal(.29375, ParameterAutomation.ValueAt(lane, 1440000), 10);
        Assert.Equal(.8, ParameterAutomation.ValueAt(lane, long.MaxValue));
        Assert.InRange(ParameterAutomation.Physical(AutomationParameter.SynthCutoff, .5), 599.999, 600.001);
        Assert.Equal(-39, ParameterAutomation.Physical(AutomationParameter.MixerVolume, .5));
        lane = lane with { Points = [new(Id<AutomationPoint>.New(), new(0), .2m),
            new(Id<AutomationPoint>.New(), new(1), .8m), new(Id<AutomationPoint>.New(), new(960000), .8m)] };
        var prepared = new PreparedAutomation(lane, new(120), 48000);
        double expected = Math.Tan(Math.PI * ParameterAutomation.Physical(AutomationParameter.SynthCutoff, .8) / 48000);
        Assert.InRange(Math.Abs(prepared.At(0) - expected), 0, 2e-6);
    }

    [Fact]
    public void DeletedSynthTargetIsRetainedAcrossSaveAndSameNamedReplacementCannotCaptureIt()
    {
        var document = ProjectDocument.Create(); Id<SoundDefinition> sound = default;
        document.Edit("Unplaced synth and automation", e =>
        {
            sound = e.AddSound("Same name", SynthVoice.Algorithm); e.SetSoundParameters(sound, SynthVoice.Defaults);
            e.AddAutomationLane(0, "Same name", new(AutomationParameter.SynthCutoff, sound),
                [new(Id<AutomationPoint>.New(), new(0), .5m)]);
        });
        document.Edit("Replace sound", e =>
        { e.DeleteSound(sound); var replacement = e.AddSound("Same name", SynthVoice.Algorithm); e.SetSoundParameters(replacement, SynthVoice.Defaults); });
        var loaded = ProjectPersistence.Read(new MemoryStream(ProjectPersistence.Encode(document)));
        try
        {
            var state = loaded.Document.Current.State;
            Assert.Equal(sound, state.AutomationLanes[0].Target.SoundId);
            Assert.False(ParameterAutomation.IsResolved(state, state.AutomationLanes[0].Target));
            Assert.True(document.Undo()); Assert.True(ParameterAutomation.IsResolved(document.Current.State, document.Current.State.AutomationLanes[0].Target));
        }
        finally { loaded.Document.Close(); document.Close(); }
    }

    [Fact]
    public async Task StalePreparationCannotPublishAndCancelledPreparationDoesNotAcceptIntent()
    {
        using var f = new Fixture(); f.Automate(false);
        using var sampler = new RealtimeSampler(48000, 2, 128);
        using (var request = sampler.BeginArrangementPreparation(f.Document))
        {
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            var lane = f.Lane; f.Document.Edit("Change then Undo", e => e.UpdateAutomationLane(lane with { Enabled = false }));
            Assert.True(f.Document.Undo());
            Assert.NotEqual(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        var before = f.Document.Current;
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        using var next = sampler.BeginArrangementPreparation(f.Document);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next.PrepareAsync(cancelled.Token));
        Assert.Same(before, f.Document.Current); Assert.Equal(0, sampler.ActiveVoices);
    }

    [Fact]
    public void VolumeSilenceAndBusTargetRespectFaderTapRatherThanSoundAssignment()
    {
        using var f = new Fixture(true, 1); f.Automate(true, 0, 0);
        using (var silent = f.Prepare()) Assert.All(Render(silent), x => Assert.Equal(0, x));
        var old = f.Channel; var lane = f.Lane; Id<MixerChannel> bus = default;
        f.Document.Edit("Bus envelope", e =>
        {
            e.DeleteAutomationLane(lane.Id); bus = e.AddMixerBus("Bus");
            var mixer = f.Document.Current.State.Mixer!;
            // A new bus is part of this transaction; SetMixer uses the captured channel plus that bus.
            var channel = mixer.Channels[0] with { Output = bus };
            e.SetMixer(mixer with { Channels = [channel, new(bus, "Bus", 1, [], null, [], new())] });
            e.AddAutomationLane(1, "Bus", new(AutomationParameter.MixerVolume, ChannelId: bus),
                [new(Id<AutomationPoint>.New(), new(0), ParameterAutomation.Normalize(AutomationParameter.MixerVolume, -6))]);
        });
        using var wetPlan = f.Prepare(); var wet = Render(wetPlan);
        f.Document.Edit("Bypass bus envelope", e => e.UpdateAutomationLane(f.Lane with { Enabled = false }));
        using var dryPlan = f.Prepare(); var dry = Render(dryPlan);
        for (int i = 0; i < wet.Length; i++) Assert.InRange(Math.Abs(wet[i] - dry[i] * Math.Pow(10, -.3)), 0, 1e-7);
        Assert.Equal(old, f.Document.Current.State.Mixer!.Channels[0].Id);
    }
}
