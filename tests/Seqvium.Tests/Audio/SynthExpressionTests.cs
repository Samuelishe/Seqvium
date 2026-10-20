// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class SynthExpressionTests
{
    private static readonly ImmutableDictionary<string, decimal> Held = SynthVoice.Defaults
        .SetItem("attack", 0).SetItem("decay", 0).SetItem("sustain", 1).SetItem("mix", 0)
        .SetItem("cutoff", 700).SetItem("resonance", 0);
    private static readonly ImmutableDictionary<string, decimal> Moving = Held.SetItem("filterAttack", 90)
        .SetItem("filterDecay", 170).SetItem("filterSustain", .3m).SetItem("filterRelease", 400)
        .SetItem("filterAmount", 3).SetItem("velocityCutoff", 2).SetItem("lfoTarget", 2)
        .SetItem("lfoRate", 4).SetItem("lfoCutoff", 1);

    private static float[] Render(PreparedSampler plan, int packet = 127)
    {
        using var execution = plan.CreateExecution();
        var result = new float[checked((int)plan.EndFrame * plan.Channels)];
        while (execution.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(packet, plan.EndFrame - execution.Position);
            execution.Process(result.AsSpan((int)execution.Position * plan.Channels, frames * plan.Channels));
        }
        Assert.Equal(0, execution.ActiveVoices);
        return result;
    }
    private static double Brightness(float[] pcm, int offset, int count, int channels = 1)
    {
        double power = 0, difference = 0;
        for (int i = offset; i < offset + count; i++)
        {
            double x = pcm[i * channels], previous = pcm[(i - 1) * channels];
            power += x * x; difference += (x - previous) * (x - previous);
        }
        return difference / power;
    }

    [Theory]
    [InlineData(44100, 1)] [InlineData(48000, 1)] [InlineData(44100, 2)] [InlineData(48000, 2)]
    public void NeutralExpressionPreservesEveryFactoryF1SampleAndTenParameterRepresentation(int rate, int channels)
    {
        foreach (var preset in SynthVoice.Presets.Take(4))
        {
            using var f = new SynthVoiceTests.Fixture(preset.Values);
            using var before = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, channels, 60.375m, .6m);
            var expected = Render(before);
            f.Set(preset.Values.SetItems(SynthVoice.ExpressionParameters.Select(p => KeyValuePair.Create(p.Key, p.Default)))
                .SetItem("lfoTarget", 1));
            using var neutral = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, channels, 60.375m, .6m);
            Assert.Equal(expected, Render(neutral, 511));
            Assert.True(SynthVoice.Supports(f.Definition));
        }
        using var old = new SynthVoiceTests.Fixture();
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "old.seqvium");
        ProjectPersistence.Save(old.Document, path);
        var loaded = ProjectPersistence.Open(path).Document;
        ProjectPersistence.Save(loaded, path);
        Assert.Equal(10, loaded.Current.State.Sounds.Single().Parameters.Count);
        Assert.Equal(11, JsonNode.Parse(File.ReadAllText(path))!["minimumReaderMinor"]!.GetValue<int>());
        loaded.Close();
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public void NeutralKernelMatchesIndependentFrozenF1FilterAndPhaseOracle(int rate)
    {
        // Frozen F1 arithmetic/order, independent of SynthState's expression/filter/lifetime implementation.
        using var f = new SynthVoiceTests.Fixture();
        var p = SynthVoice.Prepare(f.Definition, rate, 60.375m);
        var state = new SynthState(); state.Reset(p);
        double phase1 = 0, phase2 = 0, ic1 = 0, ic2 = 0;
        for (int age = 0; age < rate; age++)
        {
            int releaseAge = age < rate / 2 ? -1 : age - rate / 2;
            double envelope = p.Attack > 0 && age < p.Attack ? (double)age / p.Attack :
                p.Decay > 0 && age - p.Attack < p.Decay ? 1 + (p.Sustain - 1) * (age - p.Attack) / p.Decay : p.Sustain;
            if (releaseAge >= 0) envelope = p.Sustain * Math.Max(0, 1 - (double)releaseAge / p.Release);
            double input = (1 - p.Mix) * SynthVoice.Oscillator(p.Wave1, phase1, p.Step1) +
                p.Mix * SynthVoice.Oscillator(p.Wave2, phase2, p.Step2);
            phase1 += p.Step1; phase1 -= Math.Floor(phase1);
            phase2 += p.Step2; phase2 -= Math.Floor(phase2);
            double v3 = input - ic2, v1 = (ic1 + p.G * v3) * p.IntegratorGain, v2 = ic2 + p.G * v1;
            ic1 = 2 * v1 - ic1; ic2 = 2 * v2 - ic2;
            Assert.Equal(.18 * v2 * envelope, state.Next(p, age, releaseAge, p.G, rate));
        }
    }

    [Theory]
    [InlineData(44100, 4)] [InlineData(48000, 4)] [InlineData(44100, -4)] [InlineData(48000, -4)]
    public void FilterEnvelopeChangesNormalizedSpectralBrightnessInBothDirections(int rate, int amount)
    {
        using var f = new SynthVoiceTests.Fixture(Held.SetItem("filterAttack", 300).SetItem("filterDecay", 300)
            .SetItem("filterAmount", amount));
        using var plan = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, 1, 48, .5m);
        var pcm = Render(plan);
        double attack = Brightness(pcm, rate / 40, rate / 40), peak = Brightness(pcm, rate * 28 / 100, rate / 40);
        double sustain = Brightness(pcm, rate * 8 / 10, rate / 10);
        Assert.True(amount > 0 ? peak > attack * 2 && peak > sustain * 2 : peak < attack / 2 && peak < sustain / 2);
    }

    [Theory]
    [InlineData(44100, 1)] [InlineData(48000, 1)] [InlineData(44100, 2)] [InlineData(48000, 2)]
    public void LfoPitchHasIndependentCrossingFrequencyDepthAndCutoffHasPeriodicBrightness(int rate, int target)
    {
        var values = Held.SetItem("lfoTarget", target).SetItem("lfoRate", 4).SetItem("lfoPitch", 50).SetItem("lfoCutoff", 2);
        if (target == 1) values = values.SetItem("wave1", 0).SetItem("cutoff", 18000);
        using var f = new SynthVoiceTests.Fixture(values);
        using var plan = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, 1, target == 1 ? 69 : 48, .5m);
        var pcm = Render(plan);
        if (target == 1)
        {
            var crossings = new List<double>();
            for (int i = rate / 4; i < rate * 3 / 4; i++)
                if (pcm[i] <= 0 && pcm[i + 1] > 0) crossings.Add(i - pcm[i] / (double)(pcm[i + 1] - pcm[i]));
            var cents = new List<double>();
            for (int i = 1; i < crossings.Count; i++)
            {
                double time = (crossings[i] + crossings[i - 1]) / (2 * rate);
                double actual = 1200 * Math.Log2(rate / (crossings[i] - crossings[i - 1]) / 440);
                Assert.InRange(Math.Abs(actual - 50 * Math.Sin(2 * Math.PI * 4 * time)), 0, .8);
                cents.Add(actual);
            }
            Assert.InRange(cents.Max(), 49, 51); Assert.InRange(cents.Min(), -51, -49);
        }
        else
        {
            double open = Brightness(pcm, rate * 28 / 100, rate / 16), closed = Brightness(pcm, rate * 405 / 1000, rate / 16);
            double repeat = Brightness(pcm, rate * 53 / 100, rate / 16);
            Assert.True(open > closed * 3, $"Brightness open/closed: {open / closed}");
            Assert.InRange(repeat / open, .75, 1.25);
        }
        Assert.Equal(pcm, Render(plan, 1));
    }

    [Theory]
    [InlineData(44100)] [InlineData(48000)]
    public void VelocityChangesTimbreAndRetainsOriginalIntensityMultiplier(int rate)
    {
        using var f = new SynthVoiceTests.Fixture(Held.SetItem("cutoff", 6000).SetItem("velocityCutoff", 4));
        using var soft = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, 1, 48, .25m);
        using var strong = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, 1, 48, 1);
        var a = Render(soft); var b = Render(strong);
        double ratio = Brightness(b, rate / 4, rate / 4) / Brightness(a, rate / 4, rate / 4);
        Assert.True(ratio > 3, $"Brightness strong/soft: {ratio}");
        // At 25% intensity, four-octave sensitivity lowers cutoff by exactly three octaves.
        f.Set(Held.SetItem("cutoff", 750));
        using var oracle = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, 1, 48, 1);
        var reference = Render(oracle);
        for (int i = 0; i < a.Length; i++) Assert.InRange(Math.Abs(a[i] - reference[i] * .25), 0, 2e-6);
    }

    [Theory]
    [InlineData(44100, 2)] [InlineData(48000, 2)]
    public async Task CombinedSongAutomationMixerR8WavAndRealtimeUseSameExpression(int rate, int channels)
    {
        using var f = new SynthVoiceTests.Fixture(Moving, pitch: 48.375m, voices: 3, intensity: .2m);
        var local = await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        f.Document.Edit("Song with automation and FX", e =>
        {
            e.InitializeMixer(); e.SetMixerStrip(null, new() { Delay = new() { Enabled = true, TimeMs = 40, Mix = .2m } });
            e.AddArrangementClip(f.Document.Current.State.Placements.Single().Id, default, 0, 2);
            e.AddAutomationLane(1, "Synth", new(AutomationParameter.SynthCutoff, f.Sound),
                [new(Id<AutomationPoint>.New(), default, .3m), new(Id<AutomationPoint>.New(), new(1920000), .8m)]);
        });
        var isolated = await PatternResampling.RenderAsync(f.Document, f.Attachment.Id, TestContext.Current.CancellationToken);
        Assert.Equal(local.Wave, isolated.Wave);
        using var plan = ArrangementPreparation.Prepare(f.Document, rate, channels, 511, TestContext.Current.CancellationToken);
        var expected = Render(plan); Assert.Equal(expected, Render(plan, 1));
        using var realtime = new RealtimeSampler(rate, channels, 511);
        using var request = realtime.BeginArrangementPreparation(f.Document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        realtime.Start(); var actual = new float[expected.Length];
        for (int offset = 0; offset < actual.Length;)
        {
            int size = Math.Min(511 * channels, actual.Length - offset);
            Assert.True(realtime.Process(actual.AsSpan(offset, size))); offset += size;
        }
        Assert.Equal(expected, actual);
        using var directory = new TemporaryDirectory();
        var wave = await PatternWaveExport.ExportArrangementAsync(f.Document, Path.Combine(directory.Path, "expression.wav"), TestContext.Current.CancellationToken);
        Assert.Equal(0, wave.ClippedSamples);
    }

    [Theory]
    [InlineData(44100, 20, -8)] [InlineData(48000, 20, -8)]
    [InlineData(44100, 18000, 8)] [InlineData(48000, 18000, 8)]
    public void ExtremeCombinedModulationIsFiniteAndNoteOffStillEndsAtAmplitudeRelease(int rate, int cutoff, int amount)
    {
        using var f = new SynthVoiceTests.Fixture(Moving.SetItem("cutoff", cutoff).SetItem("resonance", 1)
            .SetItem("filterAmount", amount).SetItem("lfoCutoff", 4).SetItem("lfoRate", 20)
            .SetItem("velocityCutoff", 8).SetItem("filterRelease", 8000).SetItem("release", 50));
        using var plan = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, 1, 127, 1);
        Assert.Equal(rate + rate / 20, plan.EndFrame);
        Assert.All(Render(plan), x => Assert.True(float.IsFinite(x) && Math.Abs(x) < 10));
    }

    [Theory]
    [InlineData(2400)] [InlineData(7200)]
    public void FilterNoteOffDuringAttackOrDecayFreezesCurrentReleaseDespiteLateLiveEdit(int noteOff)
    {
        using var f = new SynthVoiceTests.Fixture(Moving.SetItem("filterAttack", 100).SetItem("filterDecay", 200)
            .SetItem("filterRelease", 100).SetItem("lfoTarget", 0).SetItem("velocityCutoff", 0));
        var p = SynthVoice.Prepare(f.Definition, 48000, 60);
        var a = new SynthState(); a.Reset(p);
        for (int age = 0; age <= noteOff; age++) a.NextLive(p, age, age == noteOff ? 0 : -1, 24000, 48000);
        var b = a;
        var edited = SynthVoice.Prepare(f.Definition with { Parameters = f.Definition.Parameters.SetItem("filterRelease", 8000)
            .SetItem("filterAttack", 5000).SetItem("filterSustain", 1) }, 48000, 60);
        for (int age = noteOff + 1; age < noteOff + 12000; age++)
            Assert.Equal(a.NextLive(p, age, age - noteOff, 24000, 48000), b.NextLive(edited, age, age - noteOff, 24000, 48000));
    }

    [Fact]
    public void CombinedCutoffMatchesIndependentPerSampleEnvelopeVelocityLfoAndAutomationOracle()
    {
        const int rate = 48000, noteOff = 7200;
        foreach (int sign in new[] { -1, 1 })
        {
            var values = Held.SetItem("filterAttack", 100).SetItem("filterDecay", 200).SetItem("filterSustain", .25m)
                .SetItem("filterRelease", 400).SetItem("filterAmount", 3 * sign).SetItem("velocityCutoff", 2)
                .SetItem("lfoTarget", 2).SetItem("lfoRate", 3).SetItem("lfoCutoff", 1.5m);
            using var f = new SynthVoiceTests.Fixture(values);
            var p = SynthVoice.Prepare(f.Definition, rate, 48.375m) with { Velocity = .4 };
            var state = new SynthState(); state.Reset(p);
            double phase = 0, ic1 = 0, ic2 = 0, smoothedG = double.NaN, frozenLevel = 0, error = 0;
            for (int age = 0; age < rate / 2; age++)
            {
                double level = age < 4800 ? age / 4800d : age < 14400 ? 1 - .75 * (age - 4800) / 9600 : .25;
                if (age == noteOff) frozenLevel = level;
                int releaseAge = age < noteOff ? -1 : age - noteOff;
                if (releaseAge >= 0) level = frozenLevel * Math.Max(0, 1 - releaseAge / 19200d);
                double octaves = 3 * sign * level + 2 * (.4 - 1) + 1.5 * Math.Sin(2 * Math.PI * 3 * age / rate);
                // Automation owns the base 600 Hz, rather than the sound's canonical 700 Hz.
                double g = Math.Tan(Math.PI * Math.Clamp(600 * Math.Pow(2, octaves), 20, 18000) / rate);
                smoothedG = double.IsNaN(smoothedG) ? g : smoothedG + (g - smoothedG) / (.005 * rate);
                double input = SynthVoice.Oscillator(p.Wave1, phase, p.Step1);
                phase += p.Step1; phase -= Math.Floor(phase);
                double v1 = (ic1 + smoothedG * (input - ic2)) / (1 + smoothedG * (smoothedG + p.K));
                double v2 = ic2 + smoothedG * v1; ic1 = 2 * v1 - ic1; ic2 = 2 * v2 - ic2;
                double amplitude = releaseAge < 0 ? 1 : Math.Max(0, 1 - releaseAge / (double)p.Release);
                double actual = state.Next(p, age, releaseAge, Math.Tan(Math.PI * 600 / rate), rate);
                error = Math.Max(error, Math.Abs(actual - .18 * v2 * amplitude));
            }
            Assert.InRange(error, 0, 2e-6);
        }
    }

    [Theory]
    [InlineData(44100, 1)] [InlineData(48000, 1)] [InlineData(44100, 2)] [InlineData(48000, 2)]
    public async Task ExpressionMonoStereoPreviewPacketsMatchRealtimeAndAllocateNoMeasuredCallbackBytes(int rate, int channels)
    {
        using var f = new SynthVoiceTests.Fixture(Moving.SetItem("lfoTarget", 1).SetItem("lfoPitch", 40));
        using var plan = SamplerPreparation.PrepareSynthAudition(f.Definition, rate, channels, 60.375m, .25m);
        var expected = Render(plan, 1); Assert.Equal(expected, Render(plan, 511));
        using (var parity = new RealtimeSampler(rate, channels, 511))
        {
            using var pending = new NoteAuditionRequest(parity, f.Document, f.Sound, 60.375m, .25m);
            await pending.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, pending.Publish());
            var actual = new float[expected.Length];
            for (int offset = 0; offset < actual.Length;)
            {
                int count = Math.Min(511 * channels, actual.Length - offset);
                Assert.True(parity.Process(actual.AsSpan(offset, count))); offset += count;
            }
            Assert.Equal(expected, actual);
        }
        using var rt = new RealtimeSampler(rate, channels, 511);
        using var request = new NoteAuditionRequest(rt, f.Document, f.Sound, 60.375m, .25m, held: true);
        await request.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, request.Publish());
        var block = new float[511 * channels];
        for (int i = 0; i < 100; i++) Assert.True(rt.Process(block));
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) rt.Process(block);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - bytes);
        rt.ReleaseAudition(); while (rt.ReadStatus().Playing) rt.Process(block);
        Assert.Equal(0, rt.ActiveVoices); rt.Stop(); rt.Process(block); Assert.All(block, x => Assert.Equal(0, x));
    }

    [Fact]
    public void StaggeredVelocityVoicesAndPatternLoopHaveIndependentExpressionAges()
    {
        using var poly = new SynthVoiceTests.Fixture(Moving, pitch: 48.375m, voices: 3);
        var notes = poly.Document.Current.State.Patterns.Single().Parts.Single().Notes;
        poly.Document.Edit("Stagger expression", e => e.SetPartNotes(poly.Pattern, poly.Part,
            [.. notes.Select((n, i) => n with { Position = new(i * 48000), Intensity = .2m + i * .3m })]));
        using var combined = GraphPreparation.Prepare(poly.Document, poly.Attachment.Id, 48000, 2, 511, 6,
            cancellationToken: TestContext.Current.CancellationToken);
        var actual = Render(combined); var sum = new double[actual.Length];
        for (int i = 0; i < 3; i++)
        {
            using var solo = new SynthVoiceTests.Fixture(Moving, pitch: 48.375m + i * 4, intensity: .2m + i * .3m);
            var n = solo.Document.Current.State.Patterns.Single().Parts.Single().Notes.Single();
            solo.Document.Edit("Stagger solo", e => e.SetPartNotes(solo.Pattern, solo.Part, [n with { Position = new(i * 48000) }]));
            using var plan = GraphPreparation.Prepare(solo.Document, solo.Attachment.Id, 48000, 2, 511,
                cancellationToken: TestContext.Current.CancellationToken);
            var pcm = Render(plan); for (int s = 0; s < sum.Length; s++) sum[s] += pcm[s];
        }
        for (int s = 0; s < sum.Length; s++) Assert.InRange(Math.Abs(actual[s] - sum[s]), 0, 4e-8);
        using var repeat = GraphPreparation.Prepare(poly.Document, poly.Attachment.Id, 48000, 2, 511, 6, repeats: 3,
            cancellationToken: TestContext.Current.CancellationToken);
        var expected = Render(repeat);
        using var loop = combined.CreateExecution(); loop.SetLooping(true); var block = new float[1022];
        for (int offset = 0; offset < 48000 * 2 * 2;)
        {
            int count = Math.Min(block.Length, 48000 * 2 * 2 - offset); loop.Process(block.AsSpan(0, count));
            Assert.Equal(expected.AsSpan(offset, count).ToArray(), block.AsSpan(0, count).ToArray()); offset += count;
        }
        loop.Stop(); Assert.Equal(0, loop.ActiveVoices);
    }

    [Fact]
    public async Task UnchangedExpressionVoiceSurvivesFutureNotePublicationWithExactPhaseAndFilterState()
    {
        using var f = new SynthVoiceTests.Fixture(Moving.SetItem("lfoTarget", 1).SetItem("lfoPitch", 35), duration: 1920000);
        f.Document.Edit("Long Song", e =>
        {
            e.SetPatternLength(f.Pattern, new(3840000));
            e.AddArrangementClip(f.Document.Current.State.Placements.Single().Id, default, 0, 2);
        });
        using var actual = new RealtimeSampler(48000, 2, 128); using var baseline = new RealtimeSampler(48000, 2, 128);
        async Task Publish(RealtimeSampler rt, bool preserve)
        {
            using var request = rt.BeginArrangementPreparation(f.Document, preserve);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        }
        await Publish(actual, false); await Publish(baseline, false); actual.Start(); baseline.Start();
        var a = new float[256]; var b = new float[256];
        for (int i = 0; i < 100; i++) { actual.Process(a); baseline.Process(b); }
        long epoch = actual.TransportEpoch;
        f.Document.Edit("Future note", e => e.AddNote(f.Pattern, f.Part, new(2880000), new(480000), 71, .3m));
        await Publish(actual, true);
        for (int i = 0; i < 100; i++)
        {
            actual.Process(a); baseline.Process(b); Assert.Equal(b, a); Assert.Equal(1, actual.ActiveVoices);
        }
        Assert.Equal(epoch, actual.TransportEpoch);
    }

    [Fact]
    public void ColdSeekReconstructsNoteRelativeVibratoPhaseWithoutElapsedFrameReplay()
    {
        using var f = new SynthVoiceTests.Fixture(Held.SetItem("wave1", 0).SetItem("cutoff", 18000)
            .SetItem("lfoTarget", 1).SetItem("lfoRate", 5.5m).SetItem("lfoPitch", 100));
        var p = SynthVoice.Prepare(f.Definition, 48000, 60.375m);
        const int age = 900000;
        var continuous = new SynthState(); continuous.Reset(p);
        for (int i = 0; i < age; i++) continuous.Next(p, i, -1, p.G, 48000);
        var sought = new SynthState(); sought.Reset(p, age);
        for (int i = age; i < age + 1200; i++)
        {
            double a = continuous.Next(p, i, -1, p.G, 48000), b = sought.Next(p, i, -1, p.G, 48000);
            if (i > age + 500) Assert.InRange(Math.Abs(a - b), 0, 2e-5); // Filter is deliberately cold.
        }
    }

    [Fact]
    public void ReaderGateUnknownDataHistoryIndependentCopyAndMonotonicSaveFloor()
    {
        using var f = new SynthVoiceTests.Fixture(Moving);
        var bytes = ProjectPersistence.Encode(f.Document); var json = JsonNode.Parse(bytes)!;
        Assert.Equal(15, json["minimumReaderMinor"]!.GetValue<int>());
        foreach (int floor in new[] { 11, 14 })
        {
            json["minimumReaderMinor"] = floor;
            Assert.Throws<ProjectFormatException>(() => ProjectPersistence.Read(new MemoryStream(Encoding.UTF8.GetBytes(json.ToJsonString()))));
        }
        json["minimumReaderMinor"] = 15;
        var parameters = json["state"]!["sounds"]![0]!["parameters"]!.AsObject(); parameters["futureLfo"] = 9;
        json["state"]!["sounds"]![0]!["futureOpaque"] = new JsonObject { ["meaning"] = "preserve" };
        var unknown = ProjectPersistence.Read(new MemoryStream(Encoding.UTF8.GetBytes(json.ToJsonString()))).Document;
        Assert.False(SynthVoice.Supports(unknown.Current.State.Sounds.Single()));
        Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(unknown,
            unknown.Current.State.GraphAttachments.Single().Id, 48000, 2, 128,
            cancellationToken: TestContext.Current.CancellationToken));
        var encoded = JsonNode.Parse(ProjectPersistence.Encode(unknown))!;
        Assert.Equal("preserve", encoded["state"]!["sounds"]![0]!["futureOpaque"]!["meaning"]!.GetValue<string>());
        unknown.Close();
        using var directory = new TemporaryDirectory(); string path = Path.Combine(directory.Path, "f2.seqvium");
        ProjectPersistence.Save(f.Document, path); f.Set(SynthVoice.Defaults); ProjectPersistence.Save(f.Document, path);
        Assert.Equal(15, JsonNode.Parse(File.ReadAllText(path))!["minimumReaderMinor"]!.GetValue<int>());
        Assert.True(f.Document.Undo()); Assert.Equal(Moving, f.Definition.Parameters);
        Assert.True(f.Document.Redo()); Assert.Equal(SynthVoice.Defaults, f.Definition.Parameters);
        f.Set(Moving);
        Id<MusicalPart> shared = default; Id<SoundDefinition> copy = default;
        f.Document.Edit("Share F2", e => shared = e.AddPart(f.Pattern, "Shared F2", f.Sound));
        Assert.All(f.Document.Current.State.Patterns.Single().Parts, p => Assert.Equal(f.Sound, p.SoundId));
        f.Document.Edit("Independent F2", e => copy = e.MakeSoundIndependent(f.Pattern, shared, "Independent F2"));
        f.Set(SynthVoice.Defaults);
        Assert.Equal(Moving, f.Document.Current.State.Sounds.Single(s => s.Id == copy).Parameters);
        ProjectPersistence.Save(f.Document, path);
        var reopened = ProjectPersistence.Open(path).Document;
        Assert.Equal(Moving.OrderBy(p => p.Key), reopened.Current.State.Sounds.Single(s => s.Id == copy).Parameters.OrderBy(p => p.Key));
        Assert.Equal(2, reopened.Current.State.Sounds.Length); reopened.Close();
    }
}
