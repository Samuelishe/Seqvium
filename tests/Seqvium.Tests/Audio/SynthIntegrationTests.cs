// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class SynthVoiceTests
{
    [Fact]
    public async Task R8RefusesResonantOverloadWithoutNormalizingOrEditingSound()
    {
        using var f = new Fixture(Sine.SetItem("cutoff", 440).SetItem("resonance", 1), pitch: 69, intensity: 1);
        var before = f.Document.Current;
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken));
        Assert.Equal("Resample.Overload", error.Message); Assert.Same(before, f.Document.Current);
    }

    [Fact]
    public void OscillatorMixHasAnIndependentLinearOracleAndVoicesHaveIndependentState()
    {
        using var f = new Fixture(Sine.SetItem("wave2", 2).SetItem("detune", 17));
        float[] Signal(decimal mix)
        {
            f.Set(f.Definition.Parameters.SetItem("mix", mix));
            using var plan = SamplerPreparation.PrepareSynthAudition(f.Definition, 48000, 1, 60, .5m);
            return Render(plan);
        }
        var left = Signal(0); var right = Signal(1); var blend = Signal(.3m);
        for (int i = 0; i < blend.Length; i++) Assert.InRange(Math.Abs(blend[i] - (.7 * left[i] + .3 * right[i])), 0, 3e-8);
        using var poly = new Fixture(Sine, voices: 3);
        using var combined = GraphPreparation.Prepare(poly.Document, poly.Attachment.Id, 48000, 2, 511, 3, cancellationToken: TestContext.Current.CancellationToken);
        var actual = Render(combined); var sum = new double[actual.Length];
        foreach (int pitch in new[] { 60, 64, 68 })
        {
            using var solo = new Fixture(Sine, pitch);
            using var plan = GraphPreparation.Prepare(solo.Document, solo.Attachment.Id, 48000, 2, 511, cancellationToken: TestContext.Current.CancellationToken);
            var pcm = Render(plan); for (int i = 0; i < sum.Length; i++) sum[i] += pcm[i];
        }
        for (int i = 0; i < actual.Length; i++) Assert.InRange(Math.Abs(actual[i] - sum[i]), 0, 4e-8);
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public void PatternLoopsRetainReleaseAndRefuseInsufficientOverlapCapacity(int rate)
    {
        using var f = new Fixture(Sine.SetItem("release", 1600), duration: 1920000);
        using var loop = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 511, 3, cancellationToken: TestContext.Current.CancellationToken);
        using var repeated = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 511, 3, repeats: 4, cancellationToken: TestContext.Current.CancellationToken);
        var expected = Render(repeated);
        using var execution = loop.CreateExecution(); execution.SetLooping(true); var packet = new float[1022];
        int samples = rate * 4 * 2;
        for (int offset = 0; offset < samples;)
        {
            int count = Math.Min(packet.Length, samples - offset); execution.Process(packet.AsSpan(0, count));
            for (int i = 0; i < count; i++) Assert.Equal(expected[offset + i], packet[i]);
            offset += count;
        }
        Assert.InRange(execution.ActiveVoices, 1, 3);
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 511, 2, cancellationToken: TestContext.Current.CancellationToken));
        execution.Stop(); Assert.Equal(0, execution.ActiveVoices);
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public async Task R8IncludesReleaseBeforeMixerAndSongWithDelayReverbHasExactRealtimeParity(int rate)
    {
        using var f = new Fixture(Sine.SetItem("release", 1600), voices: 3);
        var before = await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        Assert.Equal(100800, before.Frames); // 0.5-s gate + 1.6-s release, beyond the 1-s musical bar.
        f.Document.Edit("Song and effects", e =>
        {
            e.InitializeMixer(); e.SetMixerStrip(null, new()
            { Delay = new() { Enabled = true, TimeMs = 40, Feedback = .2m, Mix = .3m },
              Reverb = new() { Enabled = true, DecaySeconds = .2m, Mix = .2m } });
            e.AddArrangementClip(f.Document.Current.State.Placements.Single().Id, new(0), 0, 2);
        });
        var after = await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        Assert.Equal(before.Wave, after.Wave);
        using var song = ArrangementPreparation.Prepare(f.Document, rate, 2, 511, TestContext.Current.CancellationToken);
        Assert.True(song.EndFrame > rate * 3); var expected = Render(song);
        using var realtime = new RealtimeSampler(rate, 2, 511);
        using var request = realtime.BeginArrangementPreparation(f.Document); await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); realtime.Start();
        var buffer = new float[1022];
        for (int offset = 0; offset < expected.Length;)
        {
            int count = Math.Min(buffer.Length, expected.Length - offset); Assert.True(realtime.Process(buffer.AsSpan(0, count)));
            for (int i = 0; i < count; i++) Assert.Equal(expected[offset + i], buffer[i]);
            offset += count;
        }
        Assert.Equal(0, realtime.ActiveVoices);
        using var directory = new TemporaryDirectory();
        var exported = await PatternWaveExport.ExportArrangementAsync(f.Document, Path.Combine(directory.Path, "song.wav"), TestContext.Current.CancellationToken);
        Assert.True(exported.Frames > 0); Assert.Equal(0, exported.ClippedSamples);
    }

    [Fact]
    public async Task LiveChangeRetiresOnlyChangedSynthWhileStopPanicSeekAndPrimedCallbacksRemainBounded()
    {
        using var f = new Fixture(Sine, duration: 1920000);
        f.Document.Edit("Unrelated synth and Song", e =>
        {
            var sound = e.AddSound("Independent synth", SynthVoice.Algorithm); e.SetSoundParameters(sound, Sine);
            var part = e.AddPart(f.Pattern, "Independent", sound);
            e.AddNote(f.Pattern, part, new(0), new(1920000), 60, .5m);
            var state = f.Document.Current.State; var graph = state.Graphs.Single();
            var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
            var cable = graph.Connections.Single(c => c.ToNodeId == gain.Id);
            var source = e.AddGraphNode(graph.Id, GraphBuiltIns.Source, new(0, 120));
            e.BindGraphSource(f.Attachment.Id, source.Id, state.Placements.Single().Id, part);
            var mix = e.AddGraphNode(graph.Id, GraphBuiltIns.Mix, new(100, 120));
            e.DisconnectGraphPorts(graph.Id, cable.Id);
            GraphFixture.Connect(e, graph.Id, graph.Nodes.Single(n => n.Id == cable.FromNodeId), mix);
            GraphFixture.Connect(e, graph.Id, source, mix, 1); GraphFixture.Connect(e, graph.Id, mix, gain);
            e.SetContextMix(f.Attachment.ContextId, true);
            e.AddArrangementClip(state.Placements.Single().Id, new(0), 0, 2);
        });
        using var realtime = new RealtimeSampler(48000, 2, 128);
        async Task Publish(bool preserve)
        {
            using var request = realtime.BeginArrangementPreparation(f.Document, preserve);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(false); var packet = new float[256]; realtime.Start(); Assert.True(realtime.Process(packet));
        long epoch = realtime.TransportEpoch, position = realtime.RenderPosition;
        f.Set(Sine.SetItem("cutoff", 400)); await Publish(true); Assert.True(realtime.Process(packet));
        Assert.Equal(epoch, realtime.TransportEpoch); Assert.True(realtime.RenderPosition > position);
        Assert.Equal(1, realtime.ActiveVoices); // Only the changed voice retires; the other keeps its DSP state.
        realtime.SeekSong(12000); Assert.True(realtime.Process(packet)); Assert.Equal(2, realtime.ActiveVoices);
        for (int i = 0; i < 20; i++) realtime.Process(packet);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; i++) realtime.Process(packet);
        Assert.Equal(allocated, GC.GetAllocatedBytesForCurrentThread());
        realtime.Stop(); realtime.Process(packet); Assert.Equal(0, realtime.ActiveVoices); Assert.All(packet, x => Assert.Equal(0, x));
        realtime.Start(); realtime.Process(packet); realtime.Panic(); realtime.Process(packet); Assert.Equal(0, realtime.ActiveVoices);
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(f.Document, f.Attachment.Id, 48000, 2, 65536, 32, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SharedSoundsAndIndependentCopiesRetainSeparateIntentThroughHistoryAndReopen()
    {
        using var f = new Fixture(); Id<MusicalPart> shared = default; Id<SoundDefinition> independent = default;
        f.Document.Edit("Shared part", e => shared = e.AddPart(f.Pattern, "Shared", f.Sound));
        f.Set(SynthVoice.Presets[1].Values);
        Assert.All(f.Document.Current.State.Patterns.Single().Parts, part => Assert.Equal(f.Sound, part.SoundId));
        f.Document.Edit("Independent", e => independent = e.MakeSoundIndependent(f.Pattern, shared, "Independent"));
        f.Document.Edit("Separate sound", e => e.SetSoundParameters(independent, SynthVoice.Presets[3].Values));
        Assert.Equal(SynthVoice.Presets[1].Values, f.Definition.Parameters);
        Assert.True(f.Document.Undo()); Assert.True(f.Document.Redo());
        var reopened = GraphFixture.Reopen(f.Document).Document;
        Assert.Equal(2, reopened.Current.State.Sounds.Length);
        Assert.NotEqual(reopened.Current.State.Sounds[0].Parameters, reopened.Current.State.Sounds[1].Parameters);
        reopened.Close();
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public void MaximumPolyphonyUsesFixedSlotsAndHasNoDurationSizedPcm(int rate)
    {
        using var f = new Fixture(Sine, pitch: 0, voices: 32);
        using var plan = GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 128, 32,
            cancellationToken: TestContext.Current.CancellationToken);
        using var execution = plan.CreateExecution(); execution.Process(new float[256]);
        Assert.Equal(32, execution.ActiveVoices); Assert.Equal(0, plan.DecodedBytes);
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(f.Document, f.Attachment.Id, rate, 2, 128, 31,
            cancellationToken: TestContext.Current.CancellationToken));
        execution.Stop(); Assert.Equal(0, execution.ActiveVoices);
    }
}
