// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class SamplerEditorTests
{
    [Theory, MemberData(nameof(Formats))]
    public async Task CrossfadePreservesPitchPeriodAndInterpolatedPcmOracle(int sourceRate, int rate, int channels, decimal pitch)
    {
        using var f = await Fixture.Create(sourceRate, channels, pitch);
        foreach (long length in new[] { 1L, 24L, 48L })
        {
            f.Set(new(16, 224, true, 64, 192, length));
            using var plan = SamplerPreparation.PreparePattern(f.Document, f.Pattern, rate, channels);
            var pcm = Render(plan, [1, 7, 31]);
            Assert.Equal(pcm, Render(plan, [127, 3]));
            double step = sourceRate / (double)rate * Math.Pow(2, (double)(pitch - 60) / 12);
            int off = rate / 50;
            int release = (int)decimal.Round(rate / 1000m, 0, MidpointRounding.AwayFromZero);
            for (int age = 0; age < off + release; age++)
            {
                double unwrapped = 16 + age * step;
                double cursor = unwrapped < 192 ? unwrapped : 64 + (unwrapped - 64) % 128;
                double ramp = Math.Min(cursor, 191);
                double value = ramp / 512;
                if (cursor > 192 - length)
                {
                    // Independent Bernstein form of the complementary cubic fade.
                    double remaining = (192 - cursor) / length;
                    double outgoing = remaining * remaining * (3 - 2 * remaining);
                    value = value * outgoing + (cursor - 128) / 512 * (1 - outgoing);
                }
                double gain = .5 * (age < off ? 1 : 1 - (age - off) / (double)release);
                Near(value * gain, pcm[age * channels]);
                if (channels == 2) Near(-value * gain * .5, pcm[age * channels + 1]);
            }
        }
    }

    [Theory]
    [InlineData(0, 0)] [InlineData(2, 1)] [InlineData(8, 4)] [InlineData(8192, 4096)]
    public async Task CrossfadeBoundsAreExplicitAndZeroKeepsExactF1(long prefix, long fade)
    {
        var playback = new SamplerPlayback(0, prefix + 16384, true, prefix, prefix + 16384, fade);
        playback.Validate(prefix + 16384);
        Assert.Throws<ArgumentException>(() => (playback with { Crossfade = playback.CrossfadeLimit + 1 }).Validate(prefix + 16384));
        Assert.Throws<ArgumentException>(() => (playback with { Crossfade = -1 }).Validate(prefix + 16384));
        using var f = await Fixture.Create(); f.Set(new(16, 224, true, 64, 192));
        using var legacy = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1);
        var expected = Render(legacy, [13]);
        f.Document.Edit("Explicit zero", e => e.SetSoundParameters(f.Sound, f.Definition.Parameters.SetItem(SamplerPlayback.CrossfadeKey, 0)));
        using var explicitZero = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1);
        Assert.Equal(expected, Render(explicitZero, [29]));
        f.Set(SamplerPlayback.Read(f.Definition, 256));
        Assert.False(f.Definition.Parameters.ContainsKey(SamplerPlayback.CrossfadeKey));
    }

    [Theory]
    [InlineData(1, 48)] [InlineData(2, 48)] [InlineData(1, 60)] [InlineData(2, 60)]
    [InlineData(1, 127)] [InlineData(2, 127)]
    public async Task FourFrameLoopAndConstantSignalHaveNoGainDipOvershootOrAddedDc(int channels, decimal pitch)
    {
        using var f = await Fixture.Create(channels: channels, pitch: pitch);
        using var replacement = ProjectMedia.BeginImport(f.Document, "Constant", 60, 1, f.Sound);
        using var bytes = new MemoryStream(WavFixtures.Create(Enumerable.Repeat(.25f, 256 * channels).ToArray(), channels: channels));
        await replacement.PrepareAsync(bytes, TestContext.Current.CancellationToken); replacement.Accept(TestContext.Current.CancellationToken);
        f.Set(new(0, 256, true, 8, 12, 2));
        using var plan = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, channels);
        var pcm = Render(plan, [1, 31]);
        Assert.All(pcm.Take(960 * channels), v => Near(.125, v));
        Assert.All(pcm, v => Assert.InRange(v, 0, .125f));
    }

    [Fact]
    public async Task PeriodicToneRetainsItsWaveformAcrossHundredsOfSeamsWithoutDcOrDrift()
    {
        using var f = await Fixture.Create();
        var sine = Enumerable.Range(0, 256).Select(i => (float)(.25 * Math.Sin(2 * Math.PI * i / 16))).ToArray();
        using var replacement = ProjectMedia.BeginImport(f.Document, "Periodic", 60, 1, f.Sound);
        using var bytes = new MemoryStream(WavFixtures.Create(sine));
        await replacement.PrepareAsync(bytes, TestContext.Current.CancellationToken); replacement.Accept(TestContext.Current.CancellationToken);
        f.Document.Edit("Long hold", e =>
        {
            e.SetPatternLength(f.Pattern, new(3840000));
            e.SetPartNotes(f.Pattern, f.Part, [new(Id<NoteEvent>.New(), new(0), new(3840000), 60, .5m)]);
        });
        f.Set(new(0, 256, true, 64, 192, 32));
        using var plan = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1);
        var pcm = Render(plan, [31, 127]);
        // Stay clear of release and compare all source phases, including both sides of every seam.
        for (int i = 0; i < 96000; i++) Near(sine[i % 16] * .5, pcm[i]);
        Assert.InRange(Math.Abs(pcm.Take(96000).Average(x => (double)x)), 0, .000001);
    }

    [Theory]
    [InlineData(-1)] [InlineData(.5)] [InlineData(49)] [InlineData(4097)]
    public async Task InvalidCrossfadeRemainsRoundTrippableButUnavailable(decimal value)
    {
        using var f = await Fixture.Create(); f.Set(new(16, 224, true, 64, 192));
        f.Document.Edit("Invalid fade", e => e.SetSoundParameters(f.Sound, f.Definition.Parameters.SetItem(SamplerPlayback.CrossfadeKey, value)));
        Assert.False(BuiltInSound.Supports(f.Definition));
        Assert.Throws<NotSupportedException>(() => SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000));
        var loaded = GraphFixture.Reopen(f.Document).Document;
        Assert.Equal(value, loaded.Current.State.Sounds.Single().Parameters[SamplerPlayback.CrossfadeKey]); loaded.Close();
    }

    [Fact]
    public async Task CrossfadeHistoryPortableMediaAndReaderGateAreSelective()
    {
        using var f = await Fixture.Create(); f.Set(new(16, 224, true, 64, 192));
        Assert.Equal(9, GraphFixture.Json(f.Document)["minimumReaderMinor"]!.GetValue<int>());
        f.Set(new(16, 224, true, 64, 192, 24));
        var json = GraphFixture.Json(f.Document); Assert.Equal(10, json["minimumReaderMinor"]!.GetValue<int>());
        json["minimumReaderMinor"] = 9; Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        Assert.True(f.Document.Undo()); Assert.Equal(0, SamplerPlayback.Read(f.Definition, 256).Crossfade);
        Assert.True(f.Document.Redo()); Assert.Equal(24, SamplerPlayback.Read(f.Definition, 256).Crossfade);
        using var expectedPlan = SamplerPreparation.PreparePattern(f.Document, f.Pattern, 48000, 1);
        var expected = Render(expectedPlan, [9]);
        string path = f.Directory.File("crossfade.seqvium"); ProjectPersistence.Save(f.Document, path);
        using var destination = new TemporaryDirectory(); string moved = destination.File("moved.seqvium");
        File.Copy(path, moved); CopyDirectory(path + ".media", moved + ".media");
        var loaded = ProjectPersistence.Open(moved).Document;
        try
        {
            using var actual = SamplerPreparation.PreparePattern(loaded, f.Pattern, 48000, 1);
            Assert.Equal(expected, Render(actual, [127]));
            Assert.Equal(24, SamplerPlayback.Read(loaded.Current.State.Sounds.Single(), 256).Crossfade);
        }
        finally { loaded.Close(); }
        f.Set(new(16, 224, true, 64, 192));
        Assert.False(f.Definition.Parameters.ContainsKey(SamplerPlayback.CrossfadeKey));
        Assert.Equal(10, GraphFixture.Json(f.Document)["minimumReaderMinor"]!.GetValue<int>()); // saved compatibility floor
    }
}
