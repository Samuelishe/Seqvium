// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

internal sealed class AudioGraphFixture : IDisposable
{
    internal readonly TemporaryDirectory Directory = new();
    internal readonly ProjectDocument Document = ProjectDocument.Create();
    internal Id<Pattern> Pattern;
    internal Id<PatternPlacement> Placement;
    internal Id<MusicalPart> KickPart, SnarePart;
    internal GraphAttachment Attachment = null!;
    internal GraphNode Kick = null!, Snare = null!, KickGain = null!, Mix = null!, MasterGain = null!, Output = null!;
    internal float[] KickPcm = [], SnarePcm = [];
    internal int SourceRate, SourceChannels;
    internal const decimal Release = 0.125m;

    internal static async Task<AudioGraphFixture> Create(int channels = 2, int rate = 48000,
        int sourceChannels = 2, int sourceRate = 48000, bool shared = false, bool silent = false)
    {
        var fixture = new AudioGraphFixture { SourceRate = sourceRate, SourceChannels = sourceChannels };
        try
        {
            fixture.KickPcm = Enumerable.Range(0, 64).SelectMany(frame => Enumerable.Range(0, sourceChannels)
                .Select(channel => frame == 0 ? 0.4f : (frame + 1) / 256f * (channel == 0 ? 1 : -0.5f))).ToArray();
            fixture.SnarePcm = shared ? fixture.KickPcm : Enumerable.Range(0, 64)
                .SelectMany(frame => Enumerable.Range(0, sourceChannels)
                    .Select(channel => frame % 7 == 0 ? (channel == 0 ? -0.2f : 0.3f) : 0.025f)).ToArray();
            var kick = await WavFixtures.Import(fixture.Document, fixture.Directory, fixture.KickPcm,
                sourceRate, sourceChannels, release: Release);
            var snare = shared ? kick : await WavFixtures.Import(fixture.Document, fixture.Directory,
                fixture.SnarePcm, sourceRate, sourceChannels, release: Release);
            fixture.Document.Edit("Independent sources and graph", edit =>
            {
                fixture.Pattern = edit.AddPattern("Short nonintegral loop", new(1901));
                fixture.KickPart = edit.AddPart(fixture.Pattern, "Kick", kick.SoundId);
                fixture.SnarePart = edit.AddPart(fixture.Pattern, "Snare", snare.SoundId);
                edit.AddNote(fixture.Pattern, fixture.KickPart, new(0), new(411), 60.5m, 0.7m);
                edit.AddNote(fixture.Pattern, fixture.KickPart, new(891), new(504), 48m, 0.3m);
                if (!silent) edit.AddNote(fixture.Pattern, fixture.SnarePart, new(211), new(971), 60m, 0.6m);
                fixture.Placement = edit.AddPlacement(fixture.Pattern, new(137));
                fixture.Attachment = edit.CreateItemGraph(fixture.Placement, true);
                string layout = channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
                GraphNode Add(string type) => edit.AddGraphNode(fixture.Attachment.GraphId, type, new(0, 0), layout);
                fixture.Output = Add(GraphBuiltIns.Output);
                fixture.MasterGain = Add(GraphBuiltIns.Gain);
                fixture.Snare = Add(GraphBuiltIns.Source);
                fixture.Mix = Add(GraphBuiltIns.Mix);
                fixture.KickGain = Add(GraphBuiltIns.Gain);
                fixture.Kick = Add(GraphBuiltIns.Source);
                edit.BindGraphSource(fixture.Attachment.Id, fixture.Kick.Id, fixture.Placement, fixture.KickPart);
                edit.BindGraphSource(fixture.Attachment.Id, fixture.Snare.Id, fixture.Placement, fixture.SnarePart);
                GraphFixture.Connect(edit, fixture.Attachment.GraphId, fixture.Kick, fixture.KickGain);
                GraphFixture.Connect(edit, fixture.Attachment.GraphId, fixture.KickGain, fixture.Mix);
                GraphFixture.Connect(edit, fixture.Attachment.GraphId, fixture.Snare, fixture.Mix, 1);
                GraphFixture.Connect(edit, fixture.Attachment.GraphId, fixture.Mix, fixture.MasterGain);
                GraphFixture.Connect(edit, fixture.Attachment.GraphId, fixture.MasterGain, fixture.Output);
                edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 0.25m);
                edit.SetGraphGain(fixture.Attachment.GraphId, fixture.MasterGain.Id, 0.5m);
                edit.SetGraphOutput(fixture.Attachment.Id, fixture.Output.Id);
            });
            return fixture;
        }
        catch { fixture.Dispose(); throw; }
    }

    internal PreparedSampler Prepare(int rate = 48000, int channels = 2, int packet = 256, int repeats = 3,
        MusicalPosition? stop = null, int voices = 8) => GraphPreparation.Prepare(Document, Attachment.Id,
        rate, channels, packet, voices, repeats, stop);

    // Independent numeric oracle: canonical note intervals and authored PCM, no prepared events/executor.
    internal float Oracle(Id<MusicalPart> partId, long frame, int channel, int rate, int channels, int repeats)
    {
        var pattern = Document.Current.State.Patterns.Single(item => item.Id == Pattern);
        var notes = pattern.Parts.Single(item => item.Id == partId).Notes.OrderBy(item => item.Id.Value);
        var pcm = partId == KickPart ? KickPcm : SnarePcm;
        int release = (int)Math.Round((double)Release * rate / 1000, MidpointRounding.AwayFromZero);
        float sum = 0;
        long ToFrame(long ticks) => (long)decimal.Round(ticks * 60m * rate / (960000m * 120), 0,
            MidpointRounding.AwayFromZero);
        for (int iteration = 0; iteration < repeats; iteration++)
            foreach (var note in notes)
            {
                long on = ToFrame(137 + iteration * pattern.Length.Ticks + note.Position.Ticks);
                long off = ToFrame(137 + iteration * pattern.Length.Ticks + note.Position.Ticks + note.Duration.Ticks);
                long age = frame - on;
                if (on == off || age < 0 || frame >= off + release) continue;
                double cursor = age * (double)SourceRate / rate * Math.Pow(2, (double)(note.Pitch - 60) / 12);
                int index = (int)cursor;
                if (index >= pcm.Length / SourceChannels) continue;
                double Read(int sourceChannel)
                {
                    double first = pcm[index * SourceChannels + sourceChannel];
                    double second = index + 1 < pcm.Length / SourceChannels ? pcm[(index + 1) * SourceChannels + sourceChannel] : 0;
                    return first + (second - first) * (cursor - index);
                }
                double value = channels == 1 && SourceChannels == 2 ? (Read(0) + Read(1)) / 2 :
                    Read(SourceChannels == 1 ? 0 : channel);
                double envelope = frame < off ? 1 : 1 - (frame - off) / (double)release;
                sum += (float)(value * (float)note.Intensity * envelope);
            }
        return sum;
    }

    public void Dispose()
    {
        if (!Document.IsClosed) Document.Close();
        Directory.Dispose();
    }
}

public sealed class GraphExecutionTests
{
    [Theory]
    [InlineData(44100, 44100, 1, 1)]
    [InlineData(44100, 48000, 1, 2)]
    [InlineData(48000, 44100, 2, 1)]
    [InlineData(48000, 48000, 2, 2)]
    [InlineData(48000, 44100, 1, 1)]
    [InlineData(44100, 48000, 2, 2)]
    public async Task IndependentUpstreamAndMixMatchAuthoredOracleAcrossPartitions(int sourceRate, int rate,
        int sourceChannels, int channels)
    {
        using var fixture = await AudioGraphFixture.Create(channels, rate, sourceChannels, sourceRate);
        using var plan = fixture.Prepare(rate, channels, packet: 37);
        using var execution = plan.CreateExecution();
        Assert.Equal(2, plan.Contributions.Length);
        Assert.All(plan.Events.Where(item => item.Occurrence.HasValue), item =>
            Assert.Equal(fixture.Placement, item.Occurrence!.Value.PlacementId));
        int packet = 0;
        float[] output = new float[37 * channels];
        while (execution.Position < plan.EndFrame)
        {
            long position = execution.Position;
            int frames = (int)Math.Min(new[] { 1, 7, 37, 3 }[packet++ % 4], plan.EndFrame - position);
            execution.Process(output.AsSpan(0, frames * channels));
            for (int frame = 0; frame < frames; frame++)
                for (int channel = 0; channel < channels; channel++)
                {
                    int index = frame * channels + channel;
                    float kick = fixture.Oracle(fixture.KickPart, position + frame, channel, rate, channels, 3);
                    float snare = fixture.Oracle(fixture.SnarePart, position + frame, channel, rate, channels, 3);
                    Near(kick, execution.Signal(fixture.Kick.Id)[index]);
                    Near(snare, execution.Signal(fixture.Snare.Id)[index]);
                    Near(0.25f * kick, execution.Signal(fixture.KickGain.Id)[index]);
                    Near(0.25f * kick + snare, execution.Signal(fixture.Mix.Id)[index]);
                    Near(0.5f * (0.25f * kick + snare), output[index]);
                }
        }
        Assert.Equal(0, execution.ActiveVoices);
        Assert.Equal(Render(plan, [37]), Render(plan, [1, 7, 19, 3]));
    }

    [Fact]
    public async Task GainChangesOnlyItsBranchAndUnityIsEquivalentToSource()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var before = fixture.Prepare();
        using var first = before.CreateExecution();
        first.Process(new float[64]);
        var snare = first.Signal(fixture.Snare.Id).ToArray();
        fixture.Document.Edit("Unity Kick", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 1));
        using var after = fixture.Prepare();
        using var second = after.CreateExecution();
        second.Process(new float[64]);
        Assert.Equal(snare, second.Signal(fixture.Snare.Id).ToArray());
        Assert.Equal(second.Signal(fixture.Kick.Id).ToArray(), second.Signal(fixture.KickGain.Id).ToArray());
        Assert.NotEqual(Render(before, [11]), Render(after, [11]));
    }

    [Fact]
    public async Task SharedResourceRetainsSeparateOccurrenceReleaseAndSilentDependency()
    {
        using var fixture = await AudioGraphFixture.Create(shared: true);
        using var plan = fixture.Prepare();
        Assert.Equal(fixture.KickPcm.Length * sizeof(float), plan.DecodedBytes);
        using var execution = plan.CreateExecution();
        execution.Process(new float[64]);
        for (int frame = 0; frame < 32; frame++)
        {
            Near(fixture.Oracle(fixture.KickPart, plan.StartFrame + frame, 0, 48000, 2, 3), execution.Signal(fixture.Kick.Id)[frame * 2]);
            Near(fixture.Oracle(fixture.SnarePart, plan.StartFrame + frame, 0, 48000, 2, 3), execution.Signal(fixture.Snare.Id)[frame * 2]);
        }
        execution.Stop();
        execution.Process(new float[2]);
        Assert.Equal(0, execution.ActiveVoices);
        using var silent = await AudioGraphFixture.Create(silent: true);
        using var silentPlan = silent.Prepare();
        Assert.Equal(2, silentPlan.Contributions.Length);
        using var silentExecution = silentPlan.CreateExecution();
        silentExecution.Process(new float[64]);
        Assert.All(silentExecution.Signal(silent.Snare.Id).ToArray(), value => Assert.Equal(0, value));
        foreach (var path in System.IO.Directory.GetFiles(silent.Directory.File("owned"), "*.wav", SearchOption.AllDirectories)) File.Delete(path);
        Assert.Equal(GraphPreparationReasons.Media, Assert.Throws<GraphPreparationException>(() => silent.Prepare()).Reason);
    }

    [Fact]
    public async Task GeometryAndNodeArrayOrderDoNotDetermineDspAndFanOutDoesNotCreateVoices()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var original = fixture.Prepare();
        var expected = Render(original, [13]);
        fixture.Document.Edit("Geometry", edit => edit.MoveGraphNode(fixture.Attachment.GraphId, fixture.Kick.Id, new(900, -100)));
        using var moved = fixture.Prepare();
        Assert.Equal(expected, Render(moved, [7]));
        var json = GraphFixture.Json(fixture.Document);
        var nodes = json["state"]!["graphs"]![0]!["nodes"]!.AsArray();
        var reversed = nodes.Select(item => item!.DeepClone()).Reverse().ToArray();
        nodes.Clear(); foreach (var node in reversed) nodes.Add(node);
        var reorderedPath = fixture.Directory.File("reordered.seqvium");
        ProjectPersistence.Save(fixture.Document, reorderedPath);
        File.WriteAllText(reorderedPath, json.ToJsonString());
        var reopened = ProjectPersistence.Open(reorderedPath).Document!;
        using var reordered = GraphPreparation.Prepare(reopened, fixture.Attachment.Id, 48000, 2, 256,
            repeats: 3, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(expected, Render(reordered, [13]));
        reopened.Close();
    }

    [Fact]
    public async Task FanOutReadsOneSourcePerformanceAndExplicitBranchSwapChangesOnlyTheDeclaredPath()
    {
        using var fixture = await AudioGraphFixture.Create();
        using var original = fixture.Prepare();
        fixture.Document.Edit("Swap branches", edit =>
        {
            var graph = fixture.Document.Current.State.Graphs.Single(item => item.Id == fixture.Attachment.GraphId);
            foreach (var edge in graph.Connections.Where(edge => edge.FromNodeId == fixture.Kick.Id || edge.FromNodeId == fixture.Snare.Id))
                edit.DisconnectGraphPorts(graph.Id, edge.Id);
            GraphFixture.Connect(edit, graph.Id, fixture.Snare, fixture.KickGain);
            GraphFixture.Connect(edit, graph.Id, fixture.Kick, fixture.Mix, 1);
        });
        using var swapped = fixture.Prepare();
        var swappedPcm = Render(swapped, [11]);
        for (int frame = 0; frame < swappedPcm.Length / 2; frame++)
            for (int channel = 0; channel < 2; channel++)
                Near(0.5f * (fixture.Oracle(fixture.KickPart, swapped.StartFrame + frame, channel, 48000, 2, 3) +
                    0.25f * fixture.Oracle(fixture.SnarePart, swapped.StartFrame + frame, channel, 48000, 2, 3)), swappedPcm[frame * 2 + channel]);
        Assert.NotEqual(Render(original, [11]), swappedPcm);
        fixture.Document.Edit("One Source fan-out into explicit Mix", edit =>
        {
            var graph = fixture.Document.Current.State.Graphs.Single(item => item.Id == fixture.Attachment.GraphId);
            edit.DeleteGraphNode(graph.Id, fixture.Mix.Id);
            fixture.Mix = edit.AddGraphNode(graph.Id, GraphBuiltIns.Mix, new(0, 0), mixInputs: 3);
            GraphFixture.Connect(edit, graph.Id, fixture.KickGain, fixture.Mix);
            GraphFixture.Connect(edit, graph.Id, fixture.Kick, fixture.Mix, 1);
            GraphFixture.Connect(edit, graph.Id, fixture.Kick, fixture.Mix, 2);
            GraphFixture.Connect(edit, graph.Id, fixture.Mix, fixture.MasterGain);
        });
        using var fanout = fixture.Prepare();
        Assert.Equal(original.Events.Length, fanout.Events.Length);
        using var execution = fanout.CreateExecution();
        float[] block = new float[32]; execution.Process(block);
        Assert.Equal(2, execution.ActiveVoices);
        for (int frame = 0; frame < 16; frame++)
            for (int channel = 0; channel < 2; channel++)
                Near(0.5f * (2 * fixture.Oracle(fixture.KickPart, fanout.StartFrame + frame, channel, 48000, 2, 3) +
                    0.25f * fixture.Oracle(fixture.SnarePart, fanout.StartFrame + frame, channel, 48000, 2, 3)), block[frame * 2 + channel]);
    }

    [Fact]
    public async Task MaximumTopologyPacketAndScratchHaveBoundedEightVoiceWorkAndStableMixPortOrdering()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        float[] pcm = Enumerable.Range(0, 70000).SelectMany(index => new[] { 0.01f, -0.02f }).ToArray();
        var media = await WavFixtures.Import(document, directory, pcm, channels: 2);
        GraphAttachment attachment = null!; GraphNode mix = null!;
        Id<PatternPlacement> placement = default;
        document.Edit("Maximum bounded graph", edit =>
        {
            var pattern = edit.AddPattern("Long", new(3 * MusicalPosition.TicksPerQuarter));
            placement = edit.AddPlacement(pattern, new(0));
            attachment = edit.CreateItemGraph(placement, true);
            mix = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Mix, new(0, 0), mixInputs: 8);
            for (int index = 0; index < 8; index++)
            {
                var part = edit.AddPart(pattern, "Source " + index, media.SoundId);
                edit.AddNote(pattern, part, new(0), new(3 * MusicalPosition.TicksPerQuarter), 60, 0.1m * (index + 1));
                var source = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
                edit.BindGraphSource(attachment.Id, source.Id, placement, part);
                GraphFixture.Connect(edit, attachment.GraphId, source, mix, index);
            }
            var previous = mix;
            for (int index = 0; index < 22; index++)
            {
                var gain = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Gain, new(0, 0));
                GraphFixture.Connect(edit, attachment.GraphId, previous, gain);
                previous = gain;
            }
            var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(0, 0));
            GraphFixture.Connect(edit, attachment.GraphId, previous, output);
            edit.SetGraphOutput(attachment.Id, output.Id);
        });
        using var plan = GraphPreparation.Prepare(document, attachment.Id, 48000, 2, 65536,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(32, document.Current.State.Graphs.Single().Nodes.Length);
        Assert.InRange(plan.ScratchBytes, 15 * 1024 * 1024, GraphPreparation.MaximumScratchBytes);
        Assert.Equal(pcm.Length * sizeof(float), plan.DecodedBytes);
        using var execution = plan.CreateExecution();
        float[] block = new float[65536 * 2];
        execution.Process(block);
        Assert.Equal(8, execution.ActiveVoices);
        var graph = document.Current.State.Graphs.Single();
        float[] oracle = [0, 0];
        // Deliberately sum in stable input UUID order, independent of production operation tables.
        foreach (var port in mix.Ports.Where(port => port.Direction == GraphPortDirection.Input).OrderBy(port => port.Id.Value))
        {
            var edge = graph.Connections.Single(edge => edge.ToPortId == port.Id);
            var binding = document.Current.State.GraphAttachments.Single().Sources.Single(item => item.NodeId == edge.FromNodeId);
            float intensity = (float)document.Current.State.Patterns.Single().Parts.Single(item => item.Id == binding.PartId).Notes.Single().Intensity;
            oracle[0] += 0.01f * intensity; oracle[1] += -0.02f * intensity;
        }
        Assert.Equal(oracle[0], block[0]); Assert.Equal(oracle[1], block[1]);
        Assert.All(block.Where((_, index) => index % 2 == 0), value => Assert.Equal(oracle[0], value));
        execution.Stop();
        document.Close();
    }

    [Fact]
    public async Task NonAssociativeMixUsesPortIdentityOrderRatherThanPortArrayOrder()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var resources = new List<MediaAcceptance>();
        foreach (float value in new[] { 1f, 0.0000001f, -1f })
            resources.Add(await WavFixtures.Import(document, directory, Enumerable.Repeat(value, 64).ToArray()));
        GraphAttachment attachment = null!;
        document.Edit("Nonassociative Mix", edit =>
        {
            var pattern = edit.AddPattern("Mix arithmetic", new(240));
            var placement = edit.AddPlacement(pattern, new(0));
            attachment = edit.CreateItemGraph(placement, true);
            var declared = GraphBuiltIns.Create(GraphBuiltIns.Mix, new(0, 0), GraphBuiltIns.Mono, 3);
            var ordered = declared.Ports.Where(port => port.Direction == GraphPortDirection.Input).OrderBy(port => port.Id.Value).ToArray();
            var mix = declared with { Ports = [ordered[2], ordered[0], ordered[1], declared.Ports[^1]] };
            edit.AddGraphNode(attachment.GraphId, mix);
            for (int index = 0; index < 3; index++)
            {
                var part = edit.AddPart(pattern, "Part", resources[index].SoundId);
                edit.AddNote(pattern, part, new(0), new(240), 60, 1);
                var source = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0), GraphBuiltIns.Mono);
                edit.BindGraphSource(attachment.Id, source.Id, placement, part);
                edit.ConnectGraphPorts(attachment.GraphId, source.Id, source.Ports[0].Id, mix.Id, ordered[index].Id);
            }
            var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(0, 0), GraphBuiltIns.Mono);
            GraphFixture.Connect(edit, attachment.GraphId, mix, output);
            edit.SetGraphOutput(attachment.Id, output.Id);
        });
        using var plan = GraphPreparation.Prepare(document, attachment.Id, 48000, 1, 8,
            cancellationToken: TestContext.Current.CancellationToken);
        float intermediate = 1f + 0.0000001f;
        float expected = intermediate - 1f;
        Assert.NotEqual(0.0000001f, expected);
        Assert.All(Render(plan, [2]), value => Assert.Equal(expected, value));
        document.Close();
    }

    [Fact]
    public async Task DecodedBudgetRefusesFiveLargeUniqueDependenciesEvenWhenEveryPartIsSilent()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        float[] source = new float[8_000_000]; // PCM16 source <16 MiB, decoded payload 32,000,000 bytes.
        var sounds = new List<Id<SoundDefinition>>();
        for (int index = 0; index < 5; index++)
        {
            source[0] = (index + 1) / 16f;
            using var import = ProjectMedia.BeginImport(document, "Unique large dependency", ownedMediaDirectory: directory.File("owned"));
            using var bytes = new MemoryStream(WavFixtures.Create(source, floating: false));
            await import.PrepareAsync(bytes, TestContext.Current.CancellationToken);
            sounds.Add(import.Accept(TestContext.Current.CancellationToken).SoundId);
        }
        GraphAttachment attachment = null!;
        document.Edit("Silent dependencies still need bounded PCM", edit =>
        {
            var pattern = edit.AddPattern("Silent", new(240));
            var placement = edit.AddPlacement(pattern, new(0));
            attachment = edit.CreateItemGraph(placement, true);
            var mix = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Mix, new(0, 0), GraphBuiltIns.Mono, 5);
            for (int index = 0; index < sounds.Count; index++)
            {
                var part = edit.AddPart(pattern, "Silent source", sounds[index]);
                var node = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0), GraphBuiltIns.Mono);
                edit.BindGraphSource(attachment.Id, node.Id, placement, part);
                GraphFixture.Connect(edit, attachment.GraphId, node, mix, index);
            }
            var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(0, 0), GraphBuiltIns.Mono);
            GraphFixture.Connect(edit, attachment.GraphId, mix, output);
            edit.SetGraphOutput(attachment.Id, output.Id);
        });
        var before = document.Current;
        var error = Assert.Throws<GraphPreparationException>(() => GraphPreparation.Prepare(document,
            attachment.Id, 48000, 1, 8, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(GraphPreparationReasons.Budget, error.Reason);
        Assert.Same(before, document.Current);
        document.Close();
    }

    [Fact]
    public async Task ExplicitHardStopExcludesBoundaryAndExecutionLeasesOutlivePlan()
    {
        using var fixture = await AudioGraphFixture.Create();
        var plan = fixture.Prepare(stop: new(548));
        using var execution = plan.CreateExecution();
        plan.Dispose();
        float[] output = new float[(plan.EndFrame - plan.StartFrame) * 2];
        execution.Process(output);
        Assert.Equal(0, execution.ActiveVoices);
        Assert.Throws<ArgumentOutOfRangeException>(() => execution.Process(new float[2]));
        Assert.Throws<ObjectDisposedException>(() => plan.CreateExecution());
    }

    [Theory]
    [InlineData("placement-route")]
    [InlineData("part-route")]
    [InlineData("containing")]
    [InlineData("shared-performance")]
    [InlineData("gain")]
    [InlineData("layout")]
    [InlineData("voice")]
    [InlineData("packet")]
    public async Task UnsupportedPlansRefuseWhollyAndPreserveCanonicalIntent(string kind)
    {
        using var fixture = await AudioGraphFixture.Create();
        if (kind is "placement-route" or "part-route" or "containing" or "shared-performance")
            fixture.Document.Edit("Unsupported dependency", edit =>
            {
                var route = edit.AddRoute("Route");
                var context = edit.AddContext("Containing", LocalProcessingLevel.Containing);
                if (kind == "part-route") edit.SetContextMix(fixture.Attachment.ContextId, false);
                edit.SetPlacementRelationships(fixture.Placement, fixture.Attachment.ContextId,
                    kind == "containing" ? context : null, kind == "placement-route" ? route : null,
                    [new(fixture.KickPart, kind == "shared-performance" ? Guid.NewGuid() : null,
                        kind == "part-route" ? route : null)]);
            });
        if (kind == "gain") fixture.Document.Edit("Invalid Gain", edit => edit.SetGraphGain(fixture.Attachment.GraphId, fixture.KickGain.Id, 2));
        var snapshot = fixture.Document.Current;
        Assert.Throws<GraphPreparationException>(() => fixture.Prepare(channels: kind == "layout" ? 1 : 2,
            packet: kind == "packet" ? 65537 : 256, voices: kind == "voice" ? 1 : 8));
        Assert.Same(snapshot, fixture.Document.Current);
    }

    internal static float[] Render(PreparedSampler plan, int[] partitions)
    {
        using var execution = plan.CreateExecution();
        var output = new float[checked((int)(plan.EndFrame - plan.StartFrame) * plan.Channels)];
        int offset = 0, packet = 0;
        while (execution.Position < plan.EndFrame)
        {
            int frames = (int)Math.Min(partitions[packet++ % partitions.Length], plan.EndFrame - execution.Position);
            execution.Process(output.AsSpan(offset, frames * plan.Channels));
            offset += frames * plan.Channels;
        }
        return output;
    }
    internal static void Near(float expected, float actual) => Assert.InRange(Math.Abs(expected - actual), 0, 0.000002f);
}
