// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class UnfoldRepeatsTests
{
    [Theory]
    [InlineData(44100, 1, false, 0)] [InlineData(44100, 2, true, 2)]
    [InlineData(48000, 1, true, 0)] [InlineData(48000, 2, false, 2)]
    public async Task SongPcmTailsOverlapPortableMediaAndExistingContinuation(int rate, int channels, bool variation, int currentCycle)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        try
        {
            const long length = 1234567, start = 123;
            Id<ArrangementClip> target = default;
            using (var import = ProjectMedia.BeginImport(document, "Keys", releaseMilliseconds: 80,
                ownedMediaDirectory: directory.File("media")))
            {
                using var bytes = new MemoryStream(WavFixtures.Create(Enumerable.Range(0, 192000)
                    .Select(i => .03f * MathF.Sin(i * .031f)).ToArray(), 48000, 1));
                await import.PrepareAsync(bytes, TestContext.Current.CancellationToken);
                import.Accept((e, media) =>
                {
                    e.SetSettings(new(new(137.123456m), new(7, 8)));
                    var pattern = e.AddPattern("Phrase", new(length));
                    var chord = e.AddPart(pattern, "Chord", media.SoundId);
                    e.AddNote(pattern, chord, default, new(length), 60, .4m);
                    e.AddNote(pattern, chord, default, new(length - 71), 67, .3m);
                    e.AddNote(pattern, chord, new(length / 2), new(length - length / 2), 72, .2m);
                    var source = e.AddPlacement(pattern, new(999)); // Not a Song offset.
                    Graph(e, source, chord, channels);
                    target = e.AddArrangementClip(source, new(start), 2, 4);
                    if (variation) e.MakeArrangementVariation(target, "Existing fill");
                    e.AddArrangementClip(source, new(start), 3, 4);
                    var held = e.AddPattern("Held", new(length * 5));
                    var hp = e.AddPart(held, "Held", media.SoundId);
                    e.AddNote(held, hp, default, new(length * 5), 60, .1m);
                    var hs = e.AddPlacement(held, default);
                    Graph(e, hs, hp, channels); e.AddArrangementClip(hs, default, 1);
                    e.AddArrangementSection("Form", default, new(length * 5), 1);
                    e.InitializeMixer();
                    var bus = e.AddMixerBus("Keys"); e.AssignMixerSound(media.SoundId, bus);
                    e.SetMixerStrip(bus, new() { Delay = new() { Enabled = true, TimeMs = 23, Feedback = .2m, Mix = .2m } });
                }, TestContext.Current.CancellationToken);
            }
            string audio = directory.File("overlap.wav"), normalized = directory.File("normalized.wav");
            File.WriteAllBytes(audio, WavFixtures.Create(Enumerable.Range(0, 16000)
                .Select(i => .02f * MathF.Sin(i * .057f)).ToArray(), 48000, 1));
            AudioFileDecode.Normalize(audio, normalized, TestContext.Current.CancellationToken);
            using (var import = await AudioMedia.PrepareImportAsync(document, normalized, "Audio overlap", "Authored fixture",
                directory.File("audio-owned"), TestContext.Current.CancellationToken))
                import.Accept(new(start + length - 100000), 2, TestContext.Current.CancellationToken);

            var before = document.Current;
            var pcm = Song(document, rate, channels);
            EqualPcm(pcm, await RealtimeSong(document, rate, channels, pcm.Length));
            using var sampler = new RealtimeSampler(rate, channels, 256);
            async Task Publish()
            {
                using var request = sampler.BeginArrangementPreparation(document);
                request.PreservePlayback = true;
                await request.PrepareAsync(TestContext.Current.CancellationToken);
                Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken));
            }
            await Publish(); sampler.Start();
            var block = new float[256 * channels];
            long position = ConstantTempoConversion.ToFrames(new(start + currentCycle * length + length * 3 / 4), before.State.Settings.Tempo, rate);
            while (sampler.RenderPosition < position) sampler.Process(block);
            position = sampler.RenderPosition;
            int voices = sampler.ActiveVoices; long epoch = sampler.TransportEpoch;
            var events = Events(document, rate, channels);
            document.Edit("Unfold", e => e.UnfoldArrangementRepeats(target));
            var after = document.Current;
            Assert.True(ProjectDocument.HasSameContent(before.State, after.State with { ArrangementClips = before.State.ArrangementClips }));
            Assert.Equal(events, Events(document, rate, channels));
            EqualPcm(pcm, Song(document, rate, channels));
            EqualPcm(pcm, await RealtimeSong(document, rate, channels, pcm.Length));
            await Publish(); sampler.Process(block);
            Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(position + 256, sampler.RenderPosition);
            Assert.Equal(currentCycle == 0 ? voices : voices - 3, sampler.ActiveVoices);
            if (currentCycle == 0) EqualPcm(pcm.AsSpan((int)position * channels, block.Length).ToArray(), block);
            sampler.RetireCompleted();
            // Remapped current onsets retire; they must not restart on publication.
            Assert.True(sampler.ActiveVoices >= 4); // Other clip chord + unrelated held voice.
            Assert.True(document.Undo()); Assert.Same(before, document.Current);
            await Publish(); sampler.Process(block); Assert.Equal(epoch, sampler.TransportEpoch); sampler.RetireCompleted();
            Assert.True(document.Redo()); Assert.Same(after, document.Current);
            EqualPcm(pcm, Song(document, rate, channels));
            string portable = directory.File("portable/study.seqvium"); Directory.CreateDirectory(Path.GetDirectoryName(portable)!);
            ProjectPersistence.Save(document, portable);
            var reopened = ProjectPersistence.Open(portable).Document;
            try
            {
                Assert.True(ProjectDocument.HasSameContent(after.State, reopened.Current.State));
                EqualPcm(pcm, Song(reopened, rate, channels));
            }
            finally { reopened.Close(); }
        }
        finally { document.Close(); }
    }

    private static void Graph(ProjectEdit e, Id<PatternPlacement> placement, Id<MusicalPart> part, int channels)
    {
        var graph = e.CreateItemGraph(placement);
        var layout = channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
        var source = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Source, new(0, 0), layout);
        var gain = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Gain, new(100, 0), layout);
        var output = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Output, new(200, 0), layout);
        e.BindGraphSource(graph.Id, source.Id, placement, part);
        GraphFixture.Connect(e, graph.GraphId, source, gain); GraphFixture.Connect(e, graph.GraphId, gain, output);
        e.SetGraphGain(graph.GraphId, gain.Id, .7m); e.SetGraphOutput(graph.Id, output.Id);
    }

    private static (long Frame, long Ticks, ExecutionEventKind Kind)[] Events(ProjectDocument document, int rate, int channels)
    {
        using var plan = ArrangementPreparation.Prepare(document, rate, channels, 256);
        return plan.Events.Select(e => (e.Frame, e.Position.Ticks, e.Kind)).OrderBy(e => e.Frame)
            .ThenBy(e => e.Ticks).ThenBy(e => e.Kind).ToArray();
    }

    private static float[] Song(ProjectDocument document, int rate, int channels)
    {
        using var plan = ArrangementPreparation.Prepare(document, rate, channels, 256);
        using var run = plan.CreateExecution();
        var output = new float[checked((int)plan.EndFrame * channels)];
        for (int i = 0; i < output.Length; i += 251 * channels)
            run.Process(output.AsSpan(i, Math.Min(251 * channels, output.Length - i)));
        return output;
    }

    private static async Task<float[]> RealtimeSong(ProjectDocument document, int rate, int channels, int samples)
    {
        using var realtime = new RealtimeSampler(rate, channels, 256);
        using var request = realtime.BeginArrangementPreparation(document);
        await request.PrepareAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SamplerPublication.Accepted, request.Publish(TestContext.Current.CancellationToken)); realtime.Start();
        var actual = new float[samples]; var packet = new float[256 * channels];
        for (int i = 0; i < actual.Length; i += packet.Length)
        {
            Assert.True(realtime.Process(packet));
            packet.AsSpan(0, Math.Min(packet.Length, actual.Length - i)).CopyTo(actual.AsSpan(i));
        }
        Assert.Equal(0, realtime.ActiveVoices);
        return actual;
    }

    private static void EqualPcm(float[] expected, float[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        float maximum = expected.Zip(actual, (a, b) => Math.Abs(a - b)).DefaultIfEmpty().Max();
        TestContext.Current.TestOutputHelper!.WriteLine($"Maximum PCM error: {maximum:R}");
        Assert.InRange(maximum, 0, .000002f);
    }
}
