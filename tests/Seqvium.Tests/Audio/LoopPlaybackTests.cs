// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class LoopPlaybackTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly ProjectDocument Document = ProjectDocument.Create();
        internal Id<Pattern> Pattern;
        internal Id<MusicalPart> Part;
        internal GraphAttachment Attachment = null!;
        internal static async Task<Fixture> Create(decimal bpm = 137, long length = 1000, bool ramp = false)
        {
            var f = new Fixture();
            try
            {
                using var import = ProjectMedia.BeginImport(f.Document, "Signal", releaseMilliseconds: 0,
                    ownedMediaDirectory: f.Directory.File("media"));
                using var bytes = new MemoryStream(WavFixtures.Create(Enumerable.Range(0, 48000)
                    .Select(i => ramp ? i / 48000f : .4f).ToArray()));
                await import.PrepareAsync(bytes, TestContext.Current.CancellationToken);
                import.Accept((edit, media) =>
                {
                    edit.SetSettings(new(new Tempo(bpm), new(4, 4)));
                    f.Pattern = edit.AddPattern("Loop", new(length));
                    f.Part = edit.AddPart(f.Pattern, "Signal", media.SoundId);
                    edit.AddNote(f.Pattern, f.Part, default, new(length / 2), 60, 1);
                    edit.AddNote(f.Pattern, f.Part, new(length / 2), new(length - length / 2), 60, .5m);
                    var placement = edit.AddPlacement(f.Pattern, default);
                    f.Attachment = edit.CreateItemGraph(placement);
                    var source = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                    var output = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Output, new(200, 0));
                    edit.BindGraphSource(f.Attachment.Id, source.Id, placement, f.Part);
                    GraphFixture.Connect(edit, f.Attachment.GraphId, source, output);
                    edit.SetGraphOutput(f.Attachment.Id, output.Id);
                });
                return f;
            }
            catch { f.Dispose(); throw; }
        }
        public void Dispose() { Document.Close(); Directory.Dispose(); }
    }

    private static async Task Publish(Fixture f, RealtimeSampler sampler, bool continuation = false)
    {
        using var request = sampler.BeginGraphPreparation(f.Document, f.Attachment.Id);
        request.PreservePlayback = continuation;
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish());
    }

    [Theory]
    [InlineData(48000, "137")]
    [InlineData(44100, "137.123456")]
    [InlineData(48000, "1000")]
    public async Task ThousandsOfLoopsMatchAbsoluteTickOracleAcrossEveryPacketBoundary(int rate, string tempo)
    {
        decimal bpm = decimal.Parse(tempo, System.Globalization.CultureInfo.InvariantCulture);
        using var f = await Fixture.Create(bpm);
        var settings = f.Document.Current.State.Settings;
        using var sampler = new RealtimeSampler(rate, 2, 256);
        await Publish(f, sampler); sampler.SetLooping(true); sampler.Start();
        long end = ConstantTempoConversion.ToFrames(new(4000 * 1000L), settings.Tempo, rate);
        var output = new float[512]; long frame = 0, iteration = 0;
        long epoch = 0;
        int[] packets = [1, 47, 256, 3, 127]; int packet = 0;
        while (frame < end)
        {
            int count = (int)Math.Min(packets[packet++ % packets.Length], end - frame);
            Assert.True(sampler.Process(output.AsSpan(0, count * 2)));
            if (epoch == 0) epoch = sampler.TransportEpoch;
            Assert.Equal(epoch, sampler.TransportEpoch); Assert.True(sampler.ReadStatus().Playing);
            for (int i = 0; i < count; i++, frame++)
            {
                while (frame >= ConstantTempoConversion.ToFrames(new((iteration + 1) * 1000), settings.Tempo, rate)) iteration++;
                long middle = ConstantTempoConversion.ToFrames(new(iteration * 1000 + 500), settings.Tempo, rate);
                float expected = frame < middle ? .4f : .2f;
                Assert.Equal(expected, output[i * 2]); Assert.Equal(expected, output[i * 2 + 1]);
            }
        }
        Assert.Equal(end, sampler.RenderPosition);
        var stop = sampler.Panic(); sampler.Process(output);
        Assert.Equal(stop, sampler.StopAcknowledgment); Assert.All(output, value => Assert.Equal(0, value));
        Assert.False(sampler.ReadStatus().Playing);
    }

    [Fact]
    public async Task LiveScheduleKeepsVoiceAgePositionAndEpochAndWaitsForNextLoopForPastSteps()
    {
        using var f = await Fixture.Create(120, 3840000, ramp: true);
        using var sampler = new RealtimeSampler(48000, 2, 256);
        await Publish(f, sampler); sampler.SetLooping(true); sampler.Start();
        var output = new float[512]; sampler.Process(output);
        long epoch = sampler.TransportEpoch, position = sampler.RenderPosition;
        f.Document.Edit("Add a future step", edit => edit.AddNote(f.Pattern, f.Part, new(240000), new(240000), 60, 1));
        await Publish(f, sampler, continuation: true);
        sampler.Process(output);
        Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(position + 256, sampler.RenderPosition);
        Assert.Equal(256 / 48000f, output[0]); // Existing voice did not start again or fade in.
        Assert.Equal(f.Document.Current.Revision, sampler.ReadStatus().ExecutingRevision);
        sampler.RetireCompleted();
        var first = f.Document.Current.State.Patterns.Single().Parts.Single().Notes.Single(n => n.Position.Ticks == 0);
        f.Document.Edit("Erase sounding step", edit => edit.DeleteNote(f.Pattern, f.Part, first.Id));
        await Publish(f, sampler, continuation: true); sampler.Process(output);
        Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(position + 512, sampler.RenderPosition);
        Assert.All(output, value => Assert.Equal(0, value));
        sampler.RetireCompleted();
        f.Document.Undo(); await Publish(f, sampler, continuation: true); sampler.Process(output);
        Assert.Equal(epoch, sampler.TransportEpoch); Assert.All(output, value => Assert.Equal(0, value));
        sampler.RetireCompleted();
        while (sampler.RenderPosition < 96000) sampler.Process(output.AsSpan(0, (int)Math.Min(256, 96000 - sampler.RenderPosition) * 2));
        sampler.Process(output);
        Assert.Equal(1 / 48000f, output[2]); // Restored past note begins on the next musical iteration.
    }

    [Fact]
    public async Task LoopConsumerAllocatesNothingAndStopWinsOverPendingLiveEdit()
    {
        using var f = await Fixture.Create();
        using var sampler = new RealtimeSampler(48000, 2, 32);
        await Publish(f, sampler); sampler.SetLooping(true); sampler.Start();
        var output = new float[64];
        for (int i = 0; i < 100; i++) sampler.Process(output);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) sampler.Process(output);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - allocated);
        f.Document.Edit("New rhythm", edit => edit.DeleteNote(f.Pattern, f.Part,
            f.Document.Current.State.Patterns.Single().Parts.Single().Notes[0].Id));
        await Publish(f, sampler, continuation: true);
        long stop = sampler.Stop(); sampler.Process(output);
        Assert.Equal(stop, sampler.StopAcknowledgment); Assert.False(sampler.ReadStatus().Playing);
        Assert.All(output, value => Assert.Equal(0, value));
        sampler.RetireCompleted(); sampler.Process(output); Assert.False(sampler.ReadStatus().Playing);
    }

    [Fact]
    public async Task SupersededPendingEditAndUndoKeepTheActualPlayingPosition()
    {
        using var f = await Fixture.Create(120, 3840000, ramp: true);
        using var sampler = new RealtimeSampler(48000, 2, 256);
        await Publish(f, sampler); sampler.SetLooping(true); sampler.Start();
        var packet = new float[512]; sampler.Process(packet);
        long epoch = sampler.TransportEpoch;
        f.Document.Edit("Pending step", edit => edit.AddNote(f.Pattern, f.Part, new(240000), new(240000), 60, 1));
        await Publish(f, sampler, continuation: true);
        f.Document.Undo();
        using var restored = sampler.BeginGraphPreparation(f.Document, f.Attachment.Id);
        restored.PreservePlayback = true;
        await restored.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Capacity, restored.Publish(TestContext.Current.CancellationToken));
        sampler.Process(packet); Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(512, sampler.RenderPosition);
        sampler.RetireCompleted();
        Assert.Equal(SamplerPublication.Accepted, restored.Publish(TestContext.Current.CancellationToken)); sampler.Process(packet);
        Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(768, sampler.RenderPosition);
        Assert.Equal(512 / 48000f, packet[0]); Assert.True(sampler.ReadStatus().Playing);
        Assert.Equal(f.Document.Current.Revision, sampler.ReadStatus().ExecutingRevision);
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public async Task SubFrameOrAmbiguousBoundariesKeepExistingFiniteExecution(int kind)
    {
        using var f = await Fixture.Create();
        f.Document.Edit("Sub-frame event", edit => edit.AddNote(f.Pattern, f.Part, new(kind == 1 ? 250 : 1), new(kind == 1 ? 1 : 500), 60, .1m));
        using var sampler = new RealtimeSampler(48000, 2, 256);
        await Publish(f, sampler); sampler.SetLooping(true); sampler.Start();
        var packet = new float[512]; Assert.True(sampler.Process(packet));
        Assert.False(sampler.ReadStatus().Looping); Assert.False(sampler.ReadStatus().Playing);
        Assert.Equal(SamplerTransportState.Ended, sampler.ReadStatus().Transport);
    }
}
