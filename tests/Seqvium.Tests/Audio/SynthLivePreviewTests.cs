// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class SynthLivePreviewTests
{
    [Theory]
    [InlineData("filterAttack", 5000)] [InlineData("filterDecay", 0)]
    [InlineData("filterSustain", 0)] [InlineData("filterAmount", -4)]
    [InlineData("velocityCutoff", 8)] [InlineData("lfoRate", 20)]
    [InlineData("lfoCutoff", 4)] [InlineData("lfoTarget", 0)] [InlineData("lfoPitch", 75)]
    public async Task ExpressionLiveDraftsAreAudibleWithoutNoteOnOrDeviceReplacement(string key, int value)
    {
        var values = SynthVoice.Defaults.SetItem("filterAttack", 100).SetItem("filterDecay", 900)
            .SetItem("filterSustain", .35m).SetItem("filterAmount", 3).SetItem("velocityCutoff", 1)
            .SetItem("lfoTarget", key == "lfoPitch" ? 1 : 2).SetItem("lfoCutoff", 1).SetItem("lfoPitch", 15);
        using var f = new SynthVoiceTests.Fixture(values);
        using var actual = new RealtimeSampler(48000, 1, 128); using var baseline = new RealtimeSampler(48000, 1, 128);
        foreach (var rt in new[] { actual, baseline })
        {
            using var request = new NoteAuditionRequest(rt, f.Document, f.Sound, 60.375m, .5m, held: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken); Assert.Equal(SamplerPublication.Accepted, request.Publish());
        }
        var a = new float[128]; var b = new float[128];
        for (int i = 0; i < 100; i++) { actual.Process(a); baseline.Process(b); }
        long epoch = actual.TransportEpoch; var snapshot = f.Document.Current;
        Assert.True(actual.UpdateSynthAudition(f.Definition with { Parameters = values.SetItem(key, value) }, 60.375m));
        for (int i = 0; i < 100; i++) { actual.Process(a); baseline.Process(b); }
        Assert.True(a.Zip(b).Sum(x => Math.Abs(x.First - x.Second)) > .001, "No live expression change: " + key);
        Assert.Equal(epoch, actual.TransportEpoch); Assert.Equal(1, actual.PreparedStatesCreated);
        Assert.Equal(1, actual.ActiveVoices); Assert.Equal(snapshot, f.Document.Current);
    }

    [Theory]
    [InlineData("wave1", 0)]
    [InlineData("wave2", 3)]
    [InlineData("mix", 1)]
    [InlineData("detune", 100)]
    [InlineData("attack", 5000)]
    [InlineData("decay", 5000)]
    [InlineData("sustain", 0)]
    [InlineData("cutoff", 40)]
    [InlineData("resonance", 1)]
    public async Task EveryHeldParameterChangesSignalBeforeAnyCanonicalCommit(string key, int value)
    {
        using var f = new SynthVoiceTests.Fixture();
        using var actual = new RealtimeSampler(48000, 1, 128);
        using var baseline = new RealtimeSampler(48000, 1, 128);
        foreach (var sampler in new[] { actual, baseline })
        {
            using var request = new NoteAuditionRequest(sampler, f.Document, f.Sound, 60, .8m, held: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish());
        }

        var a = new float[128];
        var b = new float[128];
        for (int i = 0; i < 100; i++)
        {
            actual.Process(a);
            baseline.Process(b);
        }

        var snapshot = f.Document.Current;
        long epoch = actual.TransportEpoch;
        Assert.True(
            actual.UpdateSynthAudition(f.Definition with { Parameters = f.Definition.Parameters.SetItem(key, value) },
                60));
        for (int i = 0; i < 100; i++)
        {
            actual.Process(a);
            baseline.Process(b);
        }

        Assert.True(a.Zip(b).Sum(pair => Math.Abs(pair.First - pair.Second)) > .001, "No audible live change: " + key);
        Assert.Equal(1, actual.ActiveVoices);
        Assert.Equal(epoch, actual.TransportEpoch);
        Assert.Equal(snapshot, f.Document.Current);
    }

    [Theory]
    [InlineData(44100, 0)]
    [InlineData(44100, 8000)]
    [InlineData(48000, 0)]
    [InlineData(48000, 8000)]
    public async Task PendingLatestValueAndNaturalSafetyGateHonorEditedReleaseExactly(int rate, int release)
    {
        using var f = new SynthVoiceTests.Fixture();
        using var sampler = new RealtimeSampler(rate, 1, 511);
        using var request = new NoteAuditionRequest(sampler, f.Document, f.Sound, 60, .8m, held: true);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish());
        Assert.True(sampler.UpdateSynthAudition(
            f.Definition with { Parameters = f.Definition.Parameters.SetItem("release", 8000 - release) }, 60));
        Assert.True(sampler.UpdateSynthAudition(
            f.Definition with { Parameters = f.Definition.Parameters.SetItem("release", release) }, 60));
        var packet = new float[511];
        sampler.Process(packet);
        while (sampler.ReadStatus().Playing) sampler.Process(packet);
        Assert.Equal(rate * (30L + release / 1000), sampler.RenderPosition);
        Assert.Equal(0, sampler.ActiveVoices);
        Assert.Equal(1, sampler.PreparedStatesCreated);
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public async Task DraftCutoffChangesHeldSignalAndCancellationConvergesWithoutRestartOrCallbackAllocation(int rate)
    {
        using var f = new SynthVoiceTests.Fixture(SynthVoice.Defaults.SetItem("attack", 0).SetItem("decay", 0)
            .SetItem("sustain", 1).SetItem("cutoff", 12000));
        using var actual = new RealtimeSampler(rate, 1, 128);
        using var baseline = new RealtimeSampler(rate, 1, 128);
        foreach (var sampler in new[] { actual, baseline })
        {
            using var request = new NoteAuditionRequest(sampler, f.Document, f.Sound, 60.5m, .8m, held: true);
            await request.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, request.Publish());
        }

        var a = new float[128];
        var b = new float[128];

        void Packet()
        {
            Assert.True(actual.Process(a));
            Assert.True(baseline.Process(b));
        }

        for (int i = 0; i < 100; i++) Packet();
        long epoch = actual.TransportEpoch, position = actual.RenderPosition;
        var snapshot = f.Document.Current;
        int history = f.Document.UndoCount;
        Assert.True(
            actual.UpdateSynthAudition(f.Definition with { Parameters = f.Definition.Parameters.SetItem("cutoff", 60) },
                60.5m));
        for (int i = 0; i < 100; i++) Packet();
        Assert.True(a.Sum(x => x * x) < b.Sum(x => x * x) * .01,
            "A held note must audibly close its filter before commit.");
        Assert.Equal(1, actual.ActiveVoices);
        Assert.Equal(epoch, actual.TransportEpoch);
        Assert.Equal(position + 12800, actual.RenderPosition);
        Assert.Equal(snapshot, f.Document.Current);
        Assert.Equal(history, f.Document.UndoCount);
        Assert.True(actual.UpdateSynthAudition(f.Definition, 60.5m));
        for (int i = 0; i < 150; i++) Packet();
        for (int i = 0; i < a.Length; i++) Assert.InRange(Math.Abs(a[i] - b[i]), 0, 2e-6);
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        actual.Process(a);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - bytes);
        Assert.Equal(1, actual.PreparedStatesCreated);
        actual.Stop();
        actual.Process(a);
        Assert.All(a, x => Assert.Equal(0, x));
        Assert.Equal(0, actual.ActiveVoices);
        Assert.False(actual.UpdateSynthAudition(f.Definition, 60));
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public async Task RapidAllParameterUpdatesStayFiniteAndReleaseUsesCurrentLevelAndFrozenDuration(int rate)
    {
        using var f = new SynthVoiceTests.Fixture();
        using var sampler = new RealtimeSampler(rate, 2, 128);
        using var request = new NoteAuditionRequest(sampler, f.Document, f.Sound, 60, .8m, held: true);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish());
        var packet = new float[256];
        sampler.Process(packet);
        long epoch = sampler.TransportEpoch;
        for (int i = 0; i < 160; i++)
        {
            var values = SynthVoice.Defaults.SetItem("wave1", i % 4).SetItem("wave2", (i + 1) % 4)
                .SetItem("mix", i % 2).SetItem("detune", i % 2 == 0 ? -100 : 100)
                .SetItem("cutoff", i % 2 == 0 ? 20 : 18000).SetItem("resonance", i % 2)
                .SetItem("attack", i % 2 == 0 ? 0 : 5000).SetItem("decay", i % 2 == 0 ? 5000 : 0)
                .SetItem("sustain", i % 2).SetItem("release", 8000);
            Assert.True(sampler.UpdateSynthAudition(f.Definition with { Parameters = values }, 60));
            sampler.Process(packet);
            Assert.Equal(1, sampler.ActiveVoices);
            Assert.Equal(epoch, sampler.TransportEpoch);
            Assert.All(packet, x => Assert.True(float.IsFinite(x) && Math.Abs(x) < 2));
        }

        sampler.ReleaseAudition();
        sampler.Process(packet);
        long releasedAt = sampler.RenderPosition - 128;
        // An edit after NoteOff must neither restart nor prolong a released voice.
        Assert.True(
            sampler.UpdateSynthAudition(
                f.Definition with { Parameters = f.Definition.Parameters.SetItem("release", 0) }, 60));
        sampler.ReleaseAudition();
        while (sampler.ReadStatus().Playing) sampler.Process(packet);
        Assert.Equal(releasedAt + rate * 8L, sampler.RenderPosition);
        Assert.Equal(0, sampler.ActiveVoices);
        Assert.Equal(epoch, sampler.TransportEpoch);
        Assert.Equal(1, sampler.PreparedStatesCreated);
    }

    [Fact]
    public void LiveEnvelopeNoteOffStartsAtActualSmoothedLevelAndWaveChangeDoesNotJumpPhase()
    {
        using var f =
            new SynthVoiceTests.Fixture(SynthVoice.Defaults.SetItem("attack", 0).SetItem("decay", 0)
                .SetItem("sustain", 1));
        var before = SynthVoice.Prepare(f.Definition, 48000, 60);
        var after = SynthVoice.Prepare(
            f.Definition with { Parameters = f.Definition.Parameters.SetItem("sustain", 0).SetItem("wave1", 3) }, 48000,
            60);
        var live = new SynthState();
        live.Reset(before);
        for (int age = 0; age < 2000; age++) live.NextLive(before, age, -1, 4800, 48000);
        var reference = live;
        double old = reference.NextLive(before, 2000, -1, 4800, 48000);
        double changed = live.NextLive(after, 2000, -1, 4800, 48000);
        Assert.InRange(Math.Abs(changed - old), 0, .002);
        // Release immediately after a new zero-Sustain target retains the actual positive envelope.
        double released = live.NextLive(after, 2001, 0, 4800, 48000);
        Assert.True(Math.Abs(released) > .001);
    }
}
