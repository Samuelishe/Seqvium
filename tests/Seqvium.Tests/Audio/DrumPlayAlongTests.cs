// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class DrumPlayAlongTests
{
    internal static SoundDefinition Sound(string key) => new(default, key,
        DrumInstruments.All.Single(i => i.Key == key).Algorithm,
        DrumInstruments.All.Single(i => i.Key == key).Defaults, [], null);

    public static TheoryData<string, int> Instruments => new(
        from instrument in DrumInstruments.All
        from rate in new[] { 44100, 48000 }
        select (instrument.Key, rate));

    [Theory]
    [MemberData(nameof(Instruments))]
    public void SourcePreviewMatchesSharedNoteKernelAtSameVelocity(string key, int rate)
    {
        var sound = Sound(key);
        var source = PreparedDrumAudition.Prepare(sound, rate, 36, TestContext.Current.CancellationToken);
        using var pcm = DrumVoice.Render(sound, rate, cancellation: TestContext.Current.CancellationToken);
        using var plan = SamplerPreparation.PrepareNoteAudition(pcm, rate, 2, 36, 36, .8m);
        using var expected = plan.CreateExecution();
        using var actual = new RealtimeSampler(rate, 2, 511);
        Assert.True(actual.TriggerDrumAudition(source, .8m));
        var a = new float[1022];
        var b = new float[1022];
        for (int frame = 0; frame < source.Frames + 511; frame += 511)
        {
            actual.Process(a);
            Array.Clear(b);
            int frames = (int)Math.Min(511, expected.EndFrame - expected.Position);
            if (frames > 0) expected.Process(b.AsSpan(0, frames * 2));
            Assert.Equal(b, a);
        }

        Assert.Equal(0, actual.DrumAuditionVoices);
        Assert.Equal(1, actual.DrumAuditionHits);
        Assert.Equal(1, actual.DrumAuditionMinimumGain);
        Assert.False(actual.ReadStatus().Playing); // Preview never borrows musical transport identity.
    }

    [Theory]
    [InlineData(44100, 1)]
    [InlineData(48000, 2)]
    public void OverlapIsAdditivePartitionIndependentAndBoundedWithoutDelayedCommands(int rate, int channels)
    {
        var source = PreparedDrumAudition.Prepare(Sound("Snare"), rate, 36, TestContext.Current.CancellationToken);
        using var a = new RealtimeSampler(rate, channels, 256);
        using var b = new RealtimeSampler(rate, channels, 256);
        var whole = new float[256 * channels];
        var split = new float[256 * channels];
        for (int hit = 0; hit < 16; hit++)
        {
            Assert.True(a.TriggerDrumAudition(source, .1m));
            Assert.True(b.TriggerDrumAudition(source, .1m));
            a.Process(whole);
            b.Process(split.AsSpan(0, 17 * channels));
            b.Process(split.AsSpan(17 * channels));
            Assert.Equal(whole, split);
            Assert.InRange(a.DrumAuditionVoices, 1, 8);
        }

        Assert.Equal(8, a.DrumAuditionSteals);
        for (int hit = 0; hit < 8; hit++) Assert.True(a.TriggerDrumAudition(source, .1m));
        Assert.False(a.TriggerDrumAudition(source, .1m));
        a.Process(whole);
        long hits = a.DrumAuditionHits;
        for (int i = 0; i < 500; i++) a.Process(whole);
        Assert.Equal(hits, a.DrumAuditionHits);
        Assert.Equal(1, a.DrumAuditionOverflows);
        Assert.Equal(0, a.DrumAuditionVoices);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StopInvalidatesQueuedAndActiveHitsButAllowsDeliberateNextHit(bool global)
    {
        var source = PreparedDrumAudition.Prepare(Sound("Kick"), 48000, 36, TestContext.Current.CancellationToken);
        using var sampler = new RealtimeSampler(48000, 2, 256);
        var block = new float[512];
        Assert.True(sampler.TriggerDrumAudition(source, .8m));
        sampler.Process(block);
        Assert.True(sampler.TriggerDrumAudition(source, .8m));
        if (global) sampler.Panic();
        else sampler.StopDrumAudition();
        sampler.Process(block);
        Assert.All(block, s => Assert.Equal(0, s));
        Assert.Equal(1, sampler.DrumAuditionHits);
        Assert.True(sampler.TriggerDrumAudition(source, .8m));
        sampler.Process(block);
        Assert.Contains(block, s => s != 0);
        sampler.TerminateExecution();
        Assert.Equal(0, sampler.DrumAuditionVoices);
        Assert.False(sampler.TriggerDrumAudition(source, .8m));
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void ProtectionLimitsOnlyPreviewAndNeverChangesExistingMusic(int rate)
    {
        var source = PreparedDrumAudition.Prepare(Sound("Kick"), rate, 36, TestContext.Current.CancellationToken);
        var overlay = new DrumAudition(rate);
        for (int hit = 0; hit < 8; hit++) Assert.True(overlay.Trigger(source, 1));
        var block = new float[2048];
        Array.Fill(block, .6f);
        overlay.Process(block, 2);
        Assert.All(block, s => Assert.InRange(Math.Abs(s - .6f), 0,
            AuditionPeakProtection.Ceiling - .6f + .000001f));
        Assert.True(overlay.MinimumGain < 1);
        overlay.Stop();
        Array.Fill(block, .6f);
        overlay.Process(block, 2);
        Assert.All(block, s => Assert.Equal(.6f, s));
        Assert.True(overlay.Trigger(source, 1));
        Array.Fill(block, 1.2f); // A pre-existing unprotected overload is never disguised or amplified.
        overlay.Process(block, 2);
        Assert.All(block, s => Assert.Equal(1.2f, s));
    }

    [Fact]
    public void WarmedMaximumOverlapConsumesNoCallbackAllocation()
    {
        var source = PreparedDrumAudition.Prepare(Sound("OpenHat"), 48000, 36, TestContext.Current.CancellationToken);
        using var sampler = new RealtimeSampler(48000, 2, 256);
        var block = new float[512];
        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < 8; i++) sampler.TriggerDrumAudition(source, .8m);
            sampler.Process(block);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int round = 0; round < 100; round++)
        {
            sampler.TriggerDrumAudition(source, .8m);
            sampler.Process(block);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
