// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed partial class RepeatVariationTests
{
    [Theory]
    [InlineData(0, false)] [InlineData(0, true)] [InlineData(1, true)] [InlineData(3, true)]
    public async Task FullSongFrozenPcmLocalEditPortableMediaAndLiveUnrelatedVoice(int index, bool mixer)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        try
        {
            Id<ArrangementClip> clip = default;
            Id<Pattern> original = default;
            Id<GraphAttachment> originalGraph = default;
            using (var import = ProjectMedia.BeginImport(document, "Keys", releaseMilliseconds: 40,
                ownedMediaDirectory: directory.File("media")))
            {
                using var bytes = new MemoryStream(WavFixtures.Create(Enumerable.Range(0, 96000)
                    .Select(i => .08f * MathF.Sin(i * .031f)).ToArray(), 48000, 1));
                await import.PrepareAsync(bytes, TestContext.Current.CancellationToken);
                import.Accept((e, media) =>
                {
                    e.SetSettings(new(new(137.123456m), new(7, 8)));
                    original = e.AddPattern("Phrase", new(96000));
                    var first = e.AddPart(original, "Chord", media.SoundId);
                    var second = e.AddPart(original, "Answer", media.SoundId);
                    e.AddNote(original, first, default, new(96000), 60, .4m);
                    e.AddNote(original, first, default, new(72000), 67, .3m);
                    e.AddNote(original, second, new(48000), new(48000), 72, .2m);
                    var source = e.AddPlacement(original, default);
                    var graph = e.CreateItemGraph(source, true); originalGraph = graph.Id;
                    var a = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Source, new(0, 0));
                    var b = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Source, new(0, 100));
                    var mix = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Mix, new(100, 0));
                    var gain = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Gain, new(200, 0));
                    var output = e.AddGraphNode(graph.GraphId, GraphBuiltIns.Output, new(300, 0));
                    e.BindGraphSource(graph.Id, a.Id, source, first); e.BindGraphSource(graph.Id, b.Id, source, second);
                    GraphFixture.Connect(e, graph.GraphId, a, mix); GraphFixture.Connect(e, graph.GraphId, b, mix, 1);
                    GraphFixture.Connect(e, graph.GraphId, mix, gain); GraphFixture.Connect(e, graph.GraphId, gain, output);
                    e.SetGraphOutput(graph.Id, output.Id); e.SetGraphGain(graph.GraphId, gain.Id, .7m);
                    clip = e.AddArrangementClip(source, new(192000), 2, 4);
                    e.AddArrangementClip(source, new(192000), 3, 4); // Overlap remains shared and audible.
                    var held = e.AddPattern("Held", new(1920000));
                    var heldPart = e.AddPart(held, "Held", media.SoundId);
                    e.AddNote(held, heldPart, default, new(1920000), 60, .1m);
                    var heldSource = e.AddPlacement(held, default);
                    var hg = e.CreateItemGraph(heldSource);
                    var hs = e.AddGraphNode(hg.GraphId, GraphBuiltIns.Source, new(0, 0));
                    var ho = e.AddGraphNode(hg.GraphId, GraphBuiltIns.Output, new(100, 0));
                    e.BindGraphSource(hg.Id, hs.Id, heldSource, heldPart);
                    GraphFixture.Connect(e, hg.GraphId, hs, ho); e.SetGraphOutput(hg.Id, ho.Id);
                    e.AddArrangementClip(heldSource, default, 1);
                    e.AddArrangementSection("Section", default, new(1920000), 1);
                    if (mixer) e.InitializeMixer();
                }, TestContext.Current.CancellationToken);
            }
            var before = document.Current;
            using var frozen = new FrozenDocument(ProjectDocument.Reopen(before));
            frozen.Document.MediaRoots.AddRange(document.MediaRoots);
            var pcm = SongPcm(document);
            using var sampler = new RealtimeSampler(48000, 2, 256);
            async Task Publish()
            {
                using var request = sampler.BeginArrangementPreparation(document);
                request.PreservePlayback = true;
                await request.PrepareAsync(TestContext.Current.CancellationToken);
                Assert.Equal(SamplerPublication.Accepted, request.Publish());
            }
            await Publish(); sampler.Start();
            var packet = new float[512];
            for (int i = 0; i < (mixer ? 20 : 1); i++) sampler.Process(packet);
            long epoch = sampler.TransportEpoch, position = sampler.RenderPosition;
            Id<ArrangementClip> center = default;
            document.Edit("Unique repeat", e => center = e.MakeArrangementRepeatUnique(clip, index, "Fill"));
            var accepted = document.Current;
            AssertPcm(pcm, SongPcm(document));
            Assert.Equal(before.State.Mixer, accepted.State.Mixer);
            Assert.Equal(before.State.ArrangementSections, accepted.State.ArrangementSections);
            await Publish(); sampler.Process(packet);
            Assert.Equal(epoch, sampler.TransportEpoch); Assert.Equal(position + 256, sampler.RenderPosition);
            // Remapped current onsets retire; past cloned onsets do not replay. The overlapping
            // original clip and the held unrelated voice retain their source ages in either case.
            Assert.Equal(!mixer ? 1 : index == 0 ? 5 : 7, sampler.ActiveVoices);
            if (!mixer || index > 0) AssertPcm(pcm.AsSpan((int)position * 2, 512).ToArray(), packet);
            sampler.RetireCompleted();
            var unique = accepted.State.ArrangementClips.Single(c => c.Id == center);
            var placement = accepted.State.Placements.Single(p => p.Id == unique.SourcePlacementId);
            var pattern = accepted.State.Patterns.Single(p => p.Id == placement.PatternId);
            var graph = accepted.State.GraphAttachments.Single(g => g.ContextId == placement.ItemContextId);
            using (var a = GraphPreparation.Prepare(document, originalGraph, 48000, 2, 256, cancellationToken: TestContext.Current.CancellationToken))
            using (var b = GraphPreparation.Prepare(document, graph.Id, 48000, 2, 256, cancellationToken: TestContext.Current.CancellationToken))
                AssertPcm(PlanPcm(a), PlanPcm(b));
            string wav = directory.File("frozen.wav");
            var export = PatternWaveExport.ExportArrangementAsync(document, wav, TestContext.Current.CancellationToken);
            var note = pattern.Parts[0].Notes[0];
            document.Edit("Local fill", e => e.UpdateNote(pattern.Id, pattern.Parts[0].Id, note.Id,
                note.Position, note.Duration, 65, note.Intensity));
            await export;
            Assert.Equal(before.State.Patterns.Single(p => p.Id == original), document.Current.State.Patterns.Single(p => p.Id == original));
            var changed = SongPcm(document);
            int start = checked((int)ConstantTempoConversion.ToFrames(unique.Position, document.Current.State.Settings.Tempo, 48000) * 2);
            int end = checked((int)ConstantTempoConversion.ToFrames(new(unique.Position.Ticks + pattern.Length.Ticks), document.Current.State.Settings.Tempo, 48000) * 2 + 3840);
            AssertPcm(pcm[..start], changed[..start]); AssertPcm(pcm[end..], changed[end..]);
            Assert.True(pcm[start..end].Zip(changed[start..end], (a, b) => Math.Abs(a - b)).Max() > .001f);
            AssertPcm(pcm, SongPcm(frozen.Document));
            string reference = directory.File("reference.wav");
            await PatternWaveExport.ExportArrangementAsync(frozen.Document, reference, TestContext.Current.CancellationToken);
            var wave = File.ReadAllBytes(wav); var referenceWave = File.ReadAllBytes(reference);
            Assert.Equal(referenceWave.Length, wave.Length);
            for (int i = 44; i < wave.Length; i += 2)
                Assert.InRange(Math.Abs(BitConverter.ToInt16(wave, i) - BitConverter.ToInt16(referenceWave, i)), 0, 1);
            ProjectPersistence.Save(document, directory.File("first.seqvium"));
            string portable = directory.File("portable/copy.seqvium"); Directory.CreateDirectory(Path.GetDirectoryName(portable)!);
            ProjectPersistence.Save(document, portable);
            var reopened = ProjectPersistence.Open(portable).Document;
            try { AssertPcm(changed, SongPcm(reopened)); Assert.True(ProjectDocument.HasSameContent(document.Current.State, reopened.Current.State)); }
            finally { reopened.Close(); }
            document.Undo(); Assert.Same(accepted, document.Current);
            document.Undo(); Assert.Same(before, document.Current);
            await Publish(); sampler.Process(packet);
            Assert.Equal(epoch, sampler.TransportEpoch); sampler.RetireCompleted();
        }
        finally { document.Close(); }
    }

    private static float[] SongPcm(ProjectDocument document)
    {
        using var plan = ArrangementPreparation.Prepare(document, 48000, 2, 256);
        return PlanPcm(plan);
    }

    private sealed class FrozenDocument(ProjectDocument document) : IDisposable
    {
        internal ProjectDocument Document { get; } = document;
        public void Dispose() => Document.Close();
    }

    private static float[] PlanPcm(PreparedSampler plan)
    {
        using var execution = plan.CreateExecution();
        var output = new float[checked((int)plan.EndFrame * 2)];
        for (int i = 0; i < output.Length; i += 512)
            execution.Process(output.AsSpan(i, Math.Min(512, output.Length - i)));
        return output;
    }

    private static void AssertPcm(float[] expected, float[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        Assert.InRange(expected.Zip(actual, (a, b) => Math.Abs(a - b)).DefaultIfEmpty().Max(), 0, .000001f);
    }
}
