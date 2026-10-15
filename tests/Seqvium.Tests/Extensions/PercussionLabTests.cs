// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Extensions;
using Xunit;

namespace Seqvium.Tests;

public sealed class PercussionLabTests
{
    private static SampleGenerationRequest Request(string family, int seed, SampleRecipe? reference = null,
        ImmutableDictionary<string, decimal>? locks = null, int rate = 48000) =>
        new(new(Guid.NewGuid(), 0, rate, rate * 2), ImmutableDictionary<string, decimal>.Empty)
        {
            Exploration = new(family, reference is null ? SampleExplorationKind.Random : SampleExplorationKind.Similar,
                seed, reference, reference is null ? null : Guid.NewGuid(), locks ?? ImmutableDictionary<string, decimal>.Empty)
        };
    private static async Task<DecodedPcm> Decode(SampleCandidate candidate)
    {
        using var stream = candidate.OpenWave();
        return WavDecoder.Decode(await WavDecoder.ReadBytesAsync(stream, TestContext.Current.CancellationToken));
    }
    private static double Energy(DecodedPcm pcm, double start, double end)
    {
        int first = (int)(pcm.Frames * start), last = Math.Max(first + 1, (int)(pcm.Frames * end));
        return Enumerable.Range(first, last - first).Average(i => Math.Pow(pcm.Sample(i, 0), 2));
    }
    [Theory]
    [InlineData("Kick", 8000)] [InlineData("Kick", 44100)] [InlineData("Kick", 48000)] [InlineData("Kick", 192000)]
    [InlineData("Clap", 8000)] [InlineData("Clap", 44100)] [InlineData("Clap", 48000)] [InlineData("Clap", 192000)]
    public async Task FamiliesProduceFiniteBoundedPercussionWithRealTransientAndDecay(string family, int rate)
    {
        await using var generator = new PercussionGenerator();
        using var candidate = await generator.GenerateAsync(Request(family, 42, rate: rate), TestContext.Current.CancellationToken);
        using var pcm = await Decode(candidate);
        Assert.Equal(rate, pcm.SampleRate); Assert.Equal(1, pcm.Channels);
        Assert.InRange(pcm.Frames, rate / 10, rate * 2);
        var samples = Enumerable.Range(0, pcm.Frames).Select(i => pcm.Sample(i, 0)).ToArray();
        Assert.All(samples, sample => Assert.True(float.IsFinite(sample)));
        Assert.InRange(samples.Max(Math.Abs), .84f, .851f);
        Assert.True(Energy(pcm, .05, .25) > Energy(pcm, .8, 1) * 20);
        Assert.Equal(0, samples[^1]);
        Assert.InRange(Math.Abs(samples.Average()), 0, .006);
    }

    [Theory] [InlineData("Kick")] [InlineData("Clap")]
    public async Task RandomSoundsDifferAndSimilarUsesRecipeWhileHonouringExactLocks(string family)
    {
        await using var generator = new PercussionGenerator();
        using var a = await generator.GenerateAsync(Request(family, 10), TestContext.Current.CancellationToken);
        using var b = await generator.GenerateAsync(Request(family, 20), TestContext.Current.CancellationToken);
        var locks = ImmutableDictionary<string, decimal>.Empty.Add("decay", a.Recipe!.Values["decay"]);
        using var c = await generator.GenerateAsync(Request(family, 30, a.Recipe, locks), TestContext.Current.CancellationToken);
        using var d = await generator.GenerateAsync(Request(family, 40, locks: locks), TestContext.Current.CancellationToken);
        Assert.Equal(a.Recipe.Values["decay"], c.Recipe!.Values["decay"]);
        Assert.Equal(a.Recipe.Values["decay"], d.Recipe!.Values["decay"]);
        foreach (var control in generator.Families.Single(f => f.Id == family).Controls)
            Assert.InRange(Math.Abs(c.Recipe.Values[control.Id] - a.Recipe.Values[control.Id]) /
                (control.Maximum - control.Minimum), 0, .181m);
        Assert.Contains(c.Recipe.Values, p => p.Value != a.Recipe.Values[p.Key]);
        Assert.Contains(b.Recipe!.Values, p => p.Value != a.Recipe.Values[p.Key]);
        using var pa = await Decode(a); using var pb = await Decode(b); using var pc = await Decode(c);
        double Difference(DecodedPcm other) => Enumerable.Range(0, Math.Min(pa.Frames, other.Frames))
            .Average(i => Math.Pow(pa.Sample(i, 0) - other.Sample(i, 0), 2));
        Assert.True(Difference(pb) > .001); Assert.True(Difference(pc) > .0001);
    }

    [Fact]
    public async Task KickAndClapHaveMeasurablyDifferentSpectralCharacter()
    {
        await using var generator = new PercussionGenerator();
        using var kick = await generator.GenerateAsync(Request("Kick", 42), TestContext.Current.CancellationToken);
        using var clap = await generator.GenerateAsync(Request("Clap", 42), TestContext.Current.CancellationToken);
        using var a = await Decode(kick); using var b = await Decode(clap);
        double HighFrequency(DecodedPcm pcm) => Enumerable.Range(1, pcm.Frames - 1)
            .Sum(i => Math.Pow(pcm.Sample(i, 0) - pcm.Sample(i - 1, 0), 2)) /
            Enumerable.Range(0, pcm.Frames).Sum(i => Math.Pow(pcm.Sample(i, 0), 2));
        Assert.True(HighFrequency(b) > HighFrequency(a) * 5);
    }

    [Fact]
    public async Task UnsupportedReferenceLocksAndFrameLimitsRefuseInsteadOfRandomFallback()
    {
        await using var generator = new PercussionGenerator();
        using var a = await generator.GenerateAsync(Request("Kick", 10), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync(Request("Clap", 1, a.Recipe), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync(Request("Kick", 1,
            locks: ImmutableDictionary<string, decimal>.Empty.Add("decay", 2000)), TestContext.Current.CancellationToken));
        var bounded = Request("Kick", 1);
        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync(bounded with
            { Context = bounded.Context with { MaximumFrames = 100 } }, TestContext.Current.CancellationToken));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => generator.GenerateAsync(Request("Clap", 1), cancel.Token));
    }

    [Fact]
    public async Task HistorySeparatesSelectionReferenceLocksAndDisposesEvictedAudio()
    {
        await using var generator = new PercussionGenerator();
        using var history = new SampleCandidateHistory(); history.SetFamily("Kick");
        var all = new List<SampleCandidate>();
        for (int i = 0; i < 18; i++)
        {
            var c = await generator.GenerateAsync(Request("Kick", i), TestContext.Current.CancellationToken);
            all.Add(c); history.Add(c, true);
            if (i == 0) history.SetReference(c.Id);
        }
        Assert.Equal(12, history.Items.Length); Assert.Equal(all[0].Id, history.Reference);
        Assert.False(all[1].IsAvailable); Assert.True(all[0].IsAvailable);
        history.Select(all[10].Id); Assert.Equal(all[0].Id, history.Reference);
        history.SetLock("decay", 123); history.SetReference(all[10].Id);
        Assert.Equal(123, history.Locks["decay"]);
        history.SetFamily("Clap"); Assert.Null(history.Reference); Assert.Empty(history.Locks);
        Assert.Throws<InvalidOperationException>(() => history.SetReference(all[10].Id));
        history.Dispose(); Assert.All(all, c => Assert.False(c.IsAvailable));
    }
}
