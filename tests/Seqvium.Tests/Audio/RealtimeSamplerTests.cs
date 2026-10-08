// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class RealtimeSamplerTests
{
    [Theory]
    [InlineData(1, 120, 44100)]
    [InlineData(4, 137, 48000)]
    [InlineData(8, 137, 44100)]
    public async Task RealtimePacketsMatchOfflineAndIndependentRampReleaseOracle(int voices, int bpm, int rate)
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory, voices, bpm, rate);
        using var realtime = new RealtimeSampler(rate, 2, 1056);
        using var request = realtime.BeginPreparation(fixture.Document, fixture.Pattern, voices, repeats: 8);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
        using var plan =
            SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, rate, 2, voices, repeats: 8);
        using var offline = plan.CreateExecution();
        float[] buffer = new float[2112], expected = new float[2112];
        Assert.True(realtime.Process(buffer));
        Assert.All(buffer, value => Assert.Equal(0, value));
        realtime.Start();
        int[] packets = [1, 127, 480, 1056, 3, 257];
        int packet = 0;
        while (offline.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(packets[packet++ % packets.Length], plan.EndFrame - offline.Position);
            long first = offline.Position;
            offline.Process(expected.AsSpan(0, frames * 2));
            Assert.True(realtime.Process(buffer.AsSpan(0, frames * 2)));
            for (int i = 0; i < frames * 2; i++) Assert.InRange(Math.Abs(expected[i] - buffer[i]), 0, 0.000002f);
            // Independent direct summation: tiny 64-frame ramp, fractional resampling and three-frame release.
            for (int i = 0; i < frames; i++)
            {
                long frame = first + i;
                double sum = 0;
                for (int iteration = 0; iteration < 8; iteration++)
                {
                    long onset = OracleFrames(iteration * 4000L, rate, bpm),
                        off = OracleFrames(iteration * 4000L + 800, rate, bpm);
                    for (int voice = 0; voice < voices; voice++)
                    {
                        long age = frame - onset;
                        if (age < 0 || frame >= off + 3) continue;
                        double step = 48000.0 / rate * Math.Pow(2, voice * 0.1 / 12), cursor = age * step;
                        if (cursor >= 64) continue;
                        int index = (int)cursor;
                        double fraction = cursor - index;
                        double sample = index / 64.0 +
                                        ((index + 1 < 64 ? (index + 1) / 64.0 : 0) - index / 64.0) * fraction;
                        double release = frame < off ? 1 : 1 - (frame - off) / 3.0;
                        sum += sample * 0.1f * release;
                    }
                }

                Assert.InRange(Math.Abs((float)sum - buffer[i * 2]), 0, 0.000002f);
            }
        }

        Assert.Equal(0, realtime.ActiveVoices);
        Assert.True(realtime.Process(buffer));
        Assert.All(buffer, value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task StopPanicSurviveGainBurstsAndStartStopOrderIsPreserved()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory);
        using var realtime = new RealtimeSampler(48000, 2, 1056);
        await Publish(realtime, fixture);
        float[] output = new float[2];
        realtime.Start();
        long stop = realtime.Stop();
        for (int i = 0; i < 10000; i++) realtime.SetGain(i % 2);
        Assert.True(realtime.Process(output));
        Assert.Equal(stop, realtime.StopAcknowledgment);
        Assert.Equal(0, realtime.ActiveVoices);
        Assert.All(output, value => Assert.Equal(0, value));
        realtime.Start();
        Assert.True(realtime.Process(output));
        Assert.Equal(1, realtime.ActiveVoices);
        long panic = realtime.Panic();
        realtime.Start();
        Assert.True(realtime.Process(output));
        Assert.Equal(panic, realtime.StopAcknowledgment);
        Assert.Equal(1, realtime.ActiveVoices);
        stop = realtime.Stop();
        Assert.True(realtime.Process(output));
        long epoch = realtime.TransportEpoch;
        realtime.Start();
        Assert.True(realtime.Process(output));
        Assert.True(realtime.TransportEpoch > epoch);
        Assert.Equal(stop, realtime.StopAcknowledgment);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("undo-redo")]
    [InlineData("close")]
    [InlineData("target")]
    [InlineData("supersede")]
    [InlineData("cancel")]
    public async Task LatePreparedResultsCannotGainStaleAuthority(string change)
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory);
        using var realtime = new RealtimeSampler(48000, 2, 1056);
        using var cancellation = new CancellationTokenSource();
        using var request = realtime.BeginPreparation(fixture.Document, fixture.Pattern);
        await request.PrepareAsync(cancellation.Token);
        switch (change)
        {
            case "edit": fixture.Document.Edit("Later", edit => edit.RenameDocument("New")); break;
            case "undo-redo":
                fixture.Document.Undo();
                fixture.Document.Redo();
                break;
            case "close": fixture.Document.Close(); break;
            case "target": fixture.Document.Edit("Delete target", edit => edit.DeletePattern(fixture.Pattern)); break;
            case "supersede": realtime.InvalidatePreparation(); break;
            case "cancel": cancellation.Cancel(); break;
        }

        Assert.Equal(change == "cancel" ? SamplerPublication.Cancelled : SamplerPublication.Stale,
            request.Publish(TestContext.Current.CancellationToken));
        Assert.True(realtime.Process(new float[2]));
        Assert.Equal(Guid.Empty, realtime.ExecutingRevision);
    }

    [Fact]
    public async Task CapturedPreparationSurvivesEditButPublicationFailsAndCancellationCanAbortBeforeDecode()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory);
        using var realtime = new RealtimeSampler(48000, 2, 1056);
        using var request = realtime.BeginPreparation(fixture.Document, fixture.Pattern);
        fixture.Document.Edit("Delete original", edit => edit.DeletePattern(fixture.Pattern));
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Stale, request.Publish(TestContext.Current.CancellationToken));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var next = realtime.BeginPreparation(fixture.Document, fixture.Pattern);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next.PrepareAsync(cancellation.Token));
    }

    [Fact]
    public async Task RetiredCapacityAppliesBackpressureAndNeverDisposesBorrowedExecution()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory);
        using var realtime = new RealtimeSampler(48000, 2, 1056);
        await Publish(realtime, fixture);
        realtime.Start();
        float[] output = new float[2];
        realtime.Process(output);
        Guid first = realtime.ExecutingRevision;
        fixture.Document.Edit("Revision two", edit => edit.RenameDocument("Two"));
        await Publish(realtime, fixture);
        realtime.Process(output);
        Assert.NotEqual(first, realtime.ExecutingRevision);
        Guid second = realtime.ExecutingRevision;
        fixture.Document.Edit("Revision three", edit => edit.RenameDocument("Three"));
        await Publish(realtime, fixture);
        realtime.Process(output);
        Assert.Equal(second, realtime.ExecutingRevision);
        using var excess = realtime.BeginPreparation(fixture.Document, fixture.Pattern);
        await excess.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Capacity, excess.Publish(TestContext.Current.CancellationToken));
        realtime.RetireCompleted();
        Assert.Equal(1, realtime.Retirements);
        realtime.Process(output);
        Assert.Equal(second, realtime.ExecutingRevision);
        Assert.Equal(1, realtime.ConsumerStaleRejections);
        realtime.RetireCompleted();
        await Publish(realtime, fixture);
        realtime.Process(output);
        Assert.Equal(fixture.Document.Current.Revision, realtime.ExecutingRevision);
        fixture.Document.Close();
        realtime.Start();
        realtime.Process(output);
        Assert.True(float.IsFinite(output[0]));
    }

    [Fact]
    public async Task SteadyStateEntryHasNoManagedAllocationAndInvalidPacketsFailExplicitly()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory);
        using var realtime = new RealtimeSampler(48000, 2, 1056);
        await Publish(realtime, fixture);
        float[] buffer = new float[64];
        realtime.Start();
        for (int i = 0; i < 1000; i++)
        {
            realtime.Start();
            realtime.Process(buffer);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            realtime.Start();
            realtime.Process(buffer);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.False(realtime.Process(new float[3]));
        Assert.False(realtime.Process(new float[2114]));
        long ack = realtime.StopAcknowledgment;
        realtime.Dispose();
        Assert.Equal(ack, realtime.StopAcknowledgment);
        Assert.False(realtime.Process(buffer));
    }

    [Fact]
    public async Task CompletedUnpublishedCandidateStillConsumesPreparationCapacity()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory);
        using var realtime = new RealtimeSampler(48000, 2, 1056);
        using var first = realtime.BeginPreparation(fixture.Document, fixture.Pattern);
        await first.PrepareAsync(TestContext.Current.CancellationToken);
        using var excess = realtime.BeginPreparation(fixture.Document, fixture.Pattern);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            excess.PrepareAsync(TestContext.Current.CancellationToken));
        Assert.Equal(SamplerPublication.Stale, first.Publish(TestContext.Current.CancellationToken));
        await Publish(realtime, fixture);
        realtime.Process(new float[2]);
        realtime.Dispose();
        Assert.Equal(realtime.PreparedStatesCreated, realtime.PreparedStatesReleased);
        Assert.Equal(0, realtime.LivePreparedStates);
    }

    private static async Task Publish(RealtimeSampler sampler, (ProjectDocument Document, Id<Pattern> Pattern) fixture)
    {
        using var request = sampler.BeginPreparation(fixture.Document, fixture.Pattern);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TerminalConsumerBoundaryDoesNotFabricateStopAcknowledgment()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory);
        using var realtime = new RealtimeSampler(48000, 2, 1056);
        await Publish(realtime, fixture);
        realtime.Start();
        realtime.Process(new float[2]);
        Assert.Equal(1, realtime.ActiveVoices);
        long requested = realtime.Stop();
        realtime.TerminateExecution();
        Assert.Equal(0, realtime.ActiveVoices);
        Assert.Equal(1, realtime.TerminationBoundaries);
        Assert.True(realtime.StopAcknowledgment < requested);
        Assert.False(realtime.Process(new float[2]));
        realtime.Dispose();
        Assert.Equal(0, realtime.StopAcknowledgment);
    }

    [Fact]
    public async Task SoundEditUsesNewPreparedCoefficientsOnlyAfterHandoff()
    {
        using var directory = new TemporaryDirectory();
        var fixture = await Create(directory);
        using var realtime = new RealtimeSampler(48000, 2, 1056);
        await Publish(realtime, fixture);
        realtime.Start();
        float[] output = new float[4];
        realtime.Process(output);
        Assert.Equal(0.0015625f, output[2]);
        Guid old = realtime.ExecutingRevision;
        var sound = fixture.Document.Current.State.Sounds.Single();
        fixture.Document.Edit("New root", edit => edit.ConfigurePcmSampler(sound.Id, sound.ResourceIds[0], 72, 5));
        realtime.Process(output);
        Assert.Equal(old, realtime.ExecutingRevision);
        Assert.Equal(0.003125f, output[0]);
        byte[] canonical = ProjectPersistence.Encode(fixture.Document);
        await Publish(realtime, fixture);
        realtime.Process(output);
        Assert.Equal(fixture.Document.Current.Revision, realtime.ExecutingRevision);
        Assert.Equal(0.00078125f, output[2]);
        Assert.Equal(canonical, ProjectPersistence.Encode(fixture.Document));
        realtime.RetireCompleted();
        Assert.Equal(1, realtime.Retirements);
    }

    private static async Task<(ProjectDocument Document, Id<Pattern> Pattern)> Create(TemporaryDirectory directory,
        int voices = 1, int bpm = 120, int rate = 48000)
    {
        var document = ProjectDocument.Create(new(new Tempo(bpm), new Meter(4, 4)));
        var media = await WavFixtures.Import(document, directory,
            Enumerable.Range(0, 64).Select(i => i / 64f).ToArray(),
            release: 3000m / rate);
        Id<Pattern> pattern = default;
        document.Edit("Realtime fixture", edit =>
        {
            pattern = edit.AddPattern("Pattern", new(4000));
            var part = edit.AddPart(pattern, "Part", media.SoundId);
            for (int i = 0; i < voices; i++) edit.AddNote(pattern, part, new(0), new(800), 60m + i * 0.1m, 0.1m);
            edit.AddNote(pattern, part, new(0), new(1), 60,
                1); // These absolute boundaries quantize to zero in the declared cases.
        });
        return (document, pattern);
    }

    private static long OracleFrames(long ticks, int rate, int bpm)
    {
        var numerator = (System.Numerics.BigInteger)ticks * 60 * rate;
        var denominator = (System.Numerics.BigInteger)960000 * bpm;
        return (long)((numerator * 2 + denominator) / (denominator * 2));
    }
}
