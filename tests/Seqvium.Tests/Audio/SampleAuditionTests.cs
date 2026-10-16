// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class SampleAuditionTests
{
    private static async Task<Id<SoundDefinition>> Import(ProjectDocument document, TemporaryDirectory directory,
        float[] samples, bool create)
    {
        using var request = ProjectMedia.BeginImport(document, "Percussion", releaseMilliseconds: 0, ownedMediaDirectory: directory.File("media"));
        using var wave = new MemoryStream(WavFixtures.Create(samples, 48000, floating: true));
        await request.PrepareAsync(wave, TestContext.Current.CancellationToken);
        return request.Accept(create ? (edit, media) => ImportedOccurrence.Create(edit, media, "Percussion", new(3840000)) : null,
            TestContext.Current.CancellationToken).SoundId;
    }
    private static float[] Render(PreparedSampler plan)
    {
        using var execution = plan.CreateExecution();
        var output = new float[4096]; execution.Process(output); return output;
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task AuditionUsesRealLocalGainMixerAndRoutingAndMatchesExplicitSharedAcceptance(bool song)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var sound = await Import(document, directory, Enumerable.Repeat(.3f, 48000).ToArray(), true);
        var attachment = document.Current.State.GraphAttachments.Single();
        document.Edit("Real processing", edit =>
        {
            edit.InitializeMixer();
            edit.SetMixerStrip(null, new() { LevelDb = -6, Pan = .5m });
            if (song) edit.AddArrangementClip(document.Current.State.Placements.Single().Id, default, 0, 1);
        });
        var replacement = WavFixtures.Create(Enumerable.Range(0, 48000).Select(i => .7f * MathF.Sin(i * .017f)).ToArray(), 48000, floating: true);
        using var pcm = WavDecoder.Decode(replacement);
        using var source = new SampleAuditionSource(sound, Guid.NewGuid(), pcm);
        var canonical = document.Current; int undo = document.UndoCount;
        using var before = song ? ArrangementPreparation.Prepare(document, 48000, 2, 4096, TestContext.Current.CancellationToken) : GraphPreparation.Prepare(document, attachment.Id, 48000, 2, 4096, cancellationToken: TestContext.Current.CancellationToken);
        using var audition = source.Apply(before);
        var heard = Render(audition);
        Assert.Equal(canonical, document.Current); Assert.Equal(undo, document.UndoCount);
        Assert.Contains(heard, v => Math.Abs(v) > .01);
        double left = heard.Where((_, i) => i % 2 == 0).Sum(v => (double)v * v);
        double right = heard.Where((_, i) => i % 2 != 0).Sum(v => (double)v * v);
        Assert.True(right > left * 2); // Real Master balance, absent from raw playback.
        using var raw = SamplerPreparation.PrepareOneShot(pcm, 48000, 2);
        Assert.True(Render(raw).Sum(v => (double)v * v) > heard.Sum(v => (double)v * v) * 10);
        using var accept = ProjectMedia.BeginImport(document, "Accepted", releaseMilliseconds: 0, target: sound, ownedMediaDirectory: directory.File("media"));
        using var wave = new MemoryStream(replacement);
        await accept.PrepareAsync(wave, TestContext.Current.CancellationToken); accept.Accept(TestContext.Current.CancellationToken);
        using var accepted = song ? ArrangementPreparation.Prepare(document, 48000, 2, 4096, TestContext.Current.CancellationToken) : GraphPreparation.Prepare(document, attachment.Id, 48000, 2, 4096, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(heard, Render(accepted));
        Assert.True(document.Undo());
        using var restored = song ? ArrangementPreparation.Prepare(document, 48000, 2, 4096, TestContext.Current.CancellationToken) : GraphPreparation.Prepare(document, attachment.Id, 48000, 2, 4096, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(Render(before), Render(restored));
        document.Close();
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)]
    public async Task ContextAuditionKeepsDelayDeadlineAndHardStopAfterLiveMixerUpdate(bool song, bool hardStop)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        try
        {
            var sound = await Import(document, directory, Enumerable.Repeat(.3f, 48000).ToArray(), true);
            var attachment = document.Current.State.GraphAttachments.Single();
            document.Edit("Delay", edit =>
            {
                edit.InitializeMixer();
                edit.SetMixerStrip(null, new() { Delay = new() { Enabled = true, TimeMs = 100, Feedback = .5m } });
                if (song) edit.AddArrangementClip(document.Current.State.Placements.Single().Id, default, 0, 1);
            });
            using var pcm = WavDecoder.Decode(WavFixtures.Create(Enumerable.Repeat(.1f, 48000).ToArray(), 48000));
            using var source = new SampleAuditionSource(sound, Guid.NewGuid(), pcm);
            using var canonical = song
                ? ArrangementPreparation.Prepare(document, 48000, 2, 127, TestContext.Current.CancellationToken)
                : GraphPreparation.Prepare(document, attachment.Id, 48000, 2, 127,
                    stop: hardStop ? new(960000) : null, cancellationToken: TestContext.Current.CancellationToken);
            using var audition = source.Apply(canonical);
            using var execution = audition.CreateExecution();
            execution.Process(new float[254]);
            execution.UpdateMixer(canonical.Graph!.Mixer!);
            Assert.Equal(canonical.EndFrame, execution.EndFrame);
            Assert.Equal(!hardStop, execution.HasMixerTail);
        }
        finally { document.Close(); }
    }

    [Fact]
    public async Task SwitchingAuditionRetainsUnrelatedVoiceAndTransportAtPacketBoundary()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var target = await Import(document, directory, Enumerable.Repeat(.4f, 48000).ToArray(), true);
        var other = await Import(document, directory, Enumerable.Repeat(.2f, 48000).ToArray(), false);
        var state = document.Current.State; var attachment = state.GraphAttachments.Single();
        var pattern = state.Patterns.Single(); var placement = state.Placements.Single(); var graph = state.Graphs.Single();
        document.Edit("Unrelated sustained voice", edit =>
        {
            var part = edit.AddPart(pattern.Id, "Sustain", other); edit.AddNote(pattern.Id, part, default, pattern.Length, 60, 1);
            var source = edit.AddGraphNode(graph.Id, GraphBuiltIns.Source, new(0, 0));
            edit.BindGraphSource(attachment.Id, source.Id, placement.Id, part);
            var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
            var cable = graph.Connections.Single(c => c.ToNodeId == gain.Id);
            var mix = edit.AddGraphNode(graph.Id, GraphBuiltIns.Mix, new(0, 0));
            var inputs = mix.Ports.Where(p => p.Direction == GraphPortDirection.Input).ToArray();
            edit.DisconnectGraphPorts(graph.Id, cable.Id);
            edit.ConnectGraphPorts(graph.Id, cable.FromNodeId, cable.FromPortId, mix.Id, inputs[0].Id);
            edit.ConnectGraphPorts(graph.Id, source.Id, source.Ports.Single().Id, mix.Id, inputs[1].Id);
            edit.ConnectGraphPorts(graph.Id, mix.Id, mix.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id, gain.Id,
                gain.Ports.Single(p => p.Direction == GraphPortDirection.Input).Id);
            edit.SetContextMix(attachment.ContextId, true);
        });
        using var sampler = new RealtimeSampler(48000, 2, 64);
        sampler.SetLooping(true);
        using (var prepare = sampler.BeginGraphPreparation(document, attachment.Id))
        {
            await prepare.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, prepare.Publish(TestContext.Current.CancellationToken));
        }
        sampler.Start(); sampler.Process(new float[128]);
        Assert.Equal(2, sampler.ReadStatus().Voices);
        long epoch = sampler.ReadStatus().Epoch, position = sampler.ReadStatus().Position;
        using var candidate = WavDecoder.Decode(WavFixtures.Create(Enumerable.Repeat(-.5f, 24000).ToArray(), 48000, floating: true));
        var id = Guid.NewGuid();
        using (var prepare = sampler.BeginGraphPreparation(document, attachment.Id))
        {
            prepare.PreservePlayback = true; prepare.Audition = new(target, id, candidate);
            await prepare.PrepareAsync(TestContext.Current.CancellationToken);
            Assert.Equal(SamplerPublication.Accepted, prepare.Publish(TestContext.Current.CancellationToken));
        }
        var heard = new float[128]; sampler.Process(heard);
        Assert.Equal(id, sampler.SampleAuditionCandidate);
        Assert.Equal(epoch, sampler.ReadStatus().Epoch); Assert.Equal(position + 64, sampler.ReadStatus().Position);
        Assert.Equal(1, sampler.ReadStatus().Voices);
        Assert.All(heard, v => Assert.InRange(v, .0499f, .0501f)); // Sustained .2 PCM through the existing .25 Gain.
        sampler.TerminateExecution(); document.Close();
    }
}
