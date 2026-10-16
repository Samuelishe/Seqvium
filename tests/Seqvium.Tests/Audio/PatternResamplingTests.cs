// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

internal sealed class ResamplingFixture : IDisposable
{
    internal readonly TemporaryDirectory Directory = new();
    internal readonly ProjectDocument Document = ProjectDocument.Create();
    internal GraphAttachment Attachment = null!;
    internal Id<Pattern> Pattern;
    internal Id<MusicalPart> FirstPart;
    internal GraphNode Gain = null!;

    internal static async Task<ResamplingFixture> Create(int channels = 2, int sourceRate = 48000)
    {
        var f = new ResamplingFixture();
        var a = await WavFixtures.Import(f.Document, f.Directory,
            Enumerable.Range(0, sourceRate).SelectMany(_ => new[] { .4f, .2f }).ToArray(), sourceRate, 2, release: 100);
        var b = await WavFixtures.Import(f.Document, f.Directory,
            Enumerable.Range(0, sourceRate).SelectMany(_ => new[] { .2f, -.4f }).ToArray(), sourceRate, 2, release: 100);
        f.Document.Edit("Two independent musical paths", e =>
        {
            f.Pattern = e.AddPattern("Two voices", new(960000));
            f.FirstPart = e.AddPart(f.Pattern, "First", a.SoundId);
            var second = e.AddPart(f.Pattern, "Second", b.SoundId);
            e.AddNote(f.Pattern, f.FirstPart, new(0), new(960000), 60, 1);
            e.AddNote(f.Pattern, second, new(0), new(480000), 60, 1);
            var placement = e.AddPlacement(f.Pattern, new(2880000));
            f.Attachment = e.CreateItemGraph(placement, true);
            string layout = channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
            GraphNode Node(string type) => e.AddGraphNode(f.Attachment.GraphId, type, new(0, 0), layout);
            var sourceA = Node(GraphBuiltIns.Source); var sourceB = Node(GraphBuiltIns.Source);
            var localA = Node(GraphBuiltIns.Gain); var mix = Node(GraphBuiltIns.Mix);
            f.Gain = Node(GraphBuiltIns.Gain); var output = Node(GraphBuiltIns.Output);
            e.SetGraphGain(f.Attachment.GraphId, localA.Id, .25m);
            e.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, .5m);
            e.BindGraphSource(f.Attachment.Id, sourceA.Id, placement, f.FirstPart);
            e.BindGraphSource(f.Attachment.Id, sourceB.Id, placement, second);
            GraphFixture.Connect(e, f.Attachment.GraphId, sourceA, localA);
            GraphFixture.Connect(e, f.Attachment.GraphId, localA, mix);
            GraphFixture.Connect(e, f.Attachment.GraphId, sourceB, mix, 1);
            GraphFixture.Connect(e, f.Attachment.GraphId, mix, f.Gain);
            GraphFixture.Connect(e, f.Attachment.GraphId, f.Gain, output);
            e.SetGraphOutput(f.Attachment.Id, output.Id);
        });
        // Edits precede waveform calculation and a request captures the authoritative attachment by ID.
        f.Attachment = f.Document.Current.State.GraphAttachments.Single();
        return f;
    }

    internal Task<PatternSample> Render(CancellationToken cancellation = default, Action<int>? progress = null) =>
        PatternResampling.RenderAsync(Document, Attachment.Id, cancellation, progress);

    public void Dispose() { if (!Document.IsClosed) Document.Close(); Directory.Dispose(); }
}

public sealed class PatternResamplingTests
{
    public static IEnumerable<object[]> DrumFamilies => DrumInstruments.All.Select(i => new object[] { i.Key });

    [Theory, MemberData(nameof(DrumFamilies))]
    public async Task OneShotRendersPastPatternEndToItsRealEof(string family)
    {
        var document = ProjectDocument.Create(); GraphAttachment attachment = null!;
        var instrument = DrumInstruments.All.Single(i => i.Key == family);
        Id<SoundDefinition> soundId = default;
        document.Edit("Late drum hit", e =>
        {
            soundId = e.AddSound(family, instrument.Algorithm); e.SetSoundParameters(soundId, instrument.Defaults);
            var pattern = e.AddPattern("Late hit", new(480000)); // 250 ms; hit at 200 ms.
            var part = e.AddPart(pattern, family, soundId);
            e.AddNote(pattern, part, new(384000), new(96000), 36, 1);
            var placement = e.AddPlacement(pattern, new(960000));
            attachment = e.CreateItemGraph(placement);
            var source = e.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
            var output = e.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(100, 0));
            e.BindGraphSource(attachment.Id, source.Id, placement, part);
            GraphFixture.Connect(e, attachment.GraphId, source, output); e.SetGraphOutput(attachment.Id, output.Id);
        });
        try
        {
            using var source = DrumVoice.Render(document.Current.State.Sounds.Single(s => s.Id == soundId), 48000,
                cancellation: TestContext.Current.CancellationToken);
            var sample = await PatternResampling.RenderAsync(document, attachment.Id, TestContext.Current.CancellationToken);
            using var pcm = WavDecoder.Decode(sample.Wave);
            Assert.Equal(Math.Max(12000, 9600 + source.Frames), pcm.Frames);
            for (int frame = 0; frame < source.Frames; frame++)
                Assert.InRange(Math.Abs(pcm.Sample(9600 + frame, 0) - source.Sample(frame, 0)), 0, 1f / 32768);
            Assert.Equal(0, pcm.Sample(pcm.Frames - 1, 0));
        }
        finally { document.Close(); }
    }

    [Theory]
    [InlineData(1, 44100)] [InlineData(1, 48000)] [InlineData(2, 44100)] [InlineData(2, 48000)]
    public async Task LocalPathsAndReleaseMatchIndependentSignalOracleWithoutPlacementLeadIn(int channels, int sourceRate)
    {
        using var f = await ResamplingFixture.Create(channels, sourceRate);
        var basis = f.Document.Current; int undo = f.Document.UndoCount;
        var sample = await f.Render(TestContext.Current.CancellationToken);
        using var pcm = WavDecoder.Decode(sample.Wave);
        Assert.Equal(48000, pcm.SampleRate); Assert.Equal(channels, pcm.Channels);
        Assert.Equal(28800, pcm.Frames); Assert.Equal(pcm.Frames, sample.Frames);
        void Near(double value, int frame, int channel = 0) => Assert.InRange(Math.Abs(pcm.Sample(frame, channel) - value), 0, 1d / 32768);
        Near(channels == 1 ? -.0125 : .15, 1000);
        Near(channels == 1 ? .0375 : .05, 20000);
        Near((channels == 1 ? .0375 : .05) * (1 - 1000d / 4800), 25000);
        if (channels == 2) { Near(-.175, 1000, 1); Near(.025, 20000, 1); }
        Assert.Same(basis, f.Document.Current); Assert.Equal(undo, f.Document.UndoCount);
    }

    [Fact]
    public async Task DownstreamMixerMasterAndOtherSongItemsCannotEnterObjectSample()
    {
        using var f = await ResamplingFixture.Create();
        var before = await f.Render(TestContext.Current.CancellationToken);
        f.Document.Edit("Unrelated audible processing and arrangement", e =>
        {
            e.InitializeMixer();
            e.SetMixerStrip(null, new() { Muted = true, Eq = new() { Enabled = true, HighPassHz = 200 },
                Compressor = new() { Enabled = true, ThresholdDb = -30 } });
            e.AddArrangementClip(f.Document.Current.State.Placements[0].Id, new(0), 0, 3);
        });
        var after = await f.Render(TestContext.Current.CancellationToken);
        Assert.Equal(before.Wave, after.Wave);
        using var mixed = GraphPreparation.Prepare(f.Document, f.Attachment.Id, 48000, 2, 1024, cancellationToken: TestContext.Current.CancellationToken);
        Assert.All(GraphExecutionTests.Render(mixed, [1024]), value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task FrozenCanonicalStateAndMediaSurviveLiveEditsAndCloseDuringRender()
    {
        using var f = await ResamplingFixture.Create();
        var expected = await f.Render(TestContext.Current.CancellationToken);
        using var entered = new ManualResetEventSlim(); using var resume = new ManualResetEventSlim();
        var pending = f.Render(TestContext.Current.CancellationToken, _ =>
        { entered.Set(); Assert.True(resume.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)); });
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            f.Document.Edit("Changed local gain", e => e.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, 0));
            f.Document.Close();
        }
        finally { resume.Set(); }
        Assert.Equal(expected.Wave, (await pending).Wave);
    }

    [Fact]
    public async Task CancellationInsideActualPacketLoopReturnsNoPartialSample()
    {
        using var f = await ResamplingFixture.Create(); using var cancel = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Render(cancel.Token, _ => cancel.Cancel()));
        Assert.Equal(3, f.Document.UndoCount);
    }

    [Theory] [InlineData("opaque")] [InlineData("missing")] [InlineData("external-control")]
    public async Task RequiredUnsupportedOrUnavailableDependenciesBlockWholeObject(string kind)
    {
        using var f = await ResamplingFixture.Create();
        if (kind == "missing") File.Delete(System.IO.Directory.GetFiles(f.Directory.Path, "*.wav", SearchOption.AllDirectories)[0]);
        else if (kind == "external-control") f.Document.Edit("Required external performance", e =>
            e.SetPlacementRelationships(f.Document.Current.State.Placements[0].Id, f.Attachment.ContextId, null, null,
                [new(f.FirstPart, Guid.NewGuid(), null)]));
        else f.Document.Edit("Unknown sound state", e =>
        {
            var sound = f.Document.Current.State.Sounds[0];
            e.SetSoundParameters(sound.Id, sound.Parameters.Add("futureControl", 1));
        });
        var basis = f.Document.Current;
        await Assert.ThrowsAsync<GraphPreparationException>(() => f.Render(TestContext.Current.CancellationToken));
        Assert.Same(basis, f.Document.Current);
    }

    [Fact]
    public async Task TooLongAndOverloadedSourcesAreExplicitRefusals()
    {
        using var f = await ResamplingFixture.Create();
        f.Document.Edit("Overload", e =>
        {
            var sound = f.Document.Current.State.Sounds[0];
            for (int i = 0; i < 20; i++) e.AddNote(f.Pattern, f.FirstPart, new(0), new(960000), 60, 1);
        });
        Assert.Equal("Resample.Overload", (await Assert.ThrowsAsync<InvalidOperationException>(() => f.Render(TestContext.Current.CancellationToken))).Message);
        f.Document.Undo();
        f.Document.Edit("Too long", e => e.SetPatternLength(f.Pattern, new(960000 * 200L)));
        Assert.Equal("Resample.TooLong", (await Assert.ThrowsAsync<InvalidOperationException>(() => f.Render(TestContext.Current.CancellationToken))).Message);
    }
}
