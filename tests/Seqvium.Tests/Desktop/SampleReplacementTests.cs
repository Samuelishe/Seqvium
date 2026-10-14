// SPDX-License-Identifier: Apache-2.0
using System.Security.Cryptography;
using System.Text.Json;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class SampleReplacementTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;
        internal Fixture() => Workflow = new(ProjectDocument.Create(),
            () => throw new InvalidOperationException("Replacement must not open audio"), Directory.File("owned"));
        internal WorkflowState State => Workflow.State;
        internal BeatEditBasis Basis => BeatSteps.Resolve(State).Basis!;
        internal Pattern Pattern => BeatSteps.Resolve(State).Pattern!;
        internal MusicalPart Part => Pattern.Parts[0];
        internal async Task Create()
        {
            Assert.True(await Workflow.CreateBeatAsync(State.Document.LifecycleId, State.Generation, "Bass"));
            string file = Directory.File("original.wav");
            await File.WriteAllBytesAsync(file, WavFixtures.Create(Enumerable.Repeat(.1f, 4800).ToArray()), TestContext.Current.CancellationToken);
            Assert.True(await Workflow.AddBeatWavAsync(Basis, file));
            var part = Part;
            Assert.True(await Workflow.EditGraphAsync(new(State.Document.LifecycleId, State.Generation, State.Selected!.Value), "Musical source", (e,_,_) =>
            {
                e.SetSoundParameters(part.SoundId, State.Snapshot.State.Sounds.Single(s=>s.Id==part.SoundId).Parameters
                    .SetItem("rootPitch",45.5m).SetItem("releaseMilliseconds",85m));
                e.AddNote(Pattern.Id,part.Id,new(123456),new(700000),43,.4m);
                var placement=State.Snapshot.State.Placements.Single(p=>p.PatternId==Pattern.Id);
                e.AddArrangementClip(placement.Id,default);
                var second=e.AddArrangementClip(placement.Id,new(BeatSteps.BarTicks));
                e.MakeArrangementVariation(second,"Bass variation");
                e.InitializeMixer();
            }));
        }
        public async ValueTask DisposeAsync(){await Workflow.DisposeAsync();Directory.Dispose();}
    }

    [Theory]
    [InlineData(true,57)]
    [InlineData(false,45.5)]
    public async Task SharedReplacementRetainsMusicGraphsRoutesAndReleaseWithOneHistoryEntry(bool known,decimal root)
    {
        await using var f=new Fixture();await f.Create();
        var before=f.State.Snapshot;var part=f.Part;int undo=f.State.UndoCount;
        string file=f.Directory.File("candidate.wav");byte[] bytes=WavFixtures.Create(Enumerable.Repeat(.2f,4800).ToArray());
        await File.WriteAllBytesAsync(file,bytes,TestContext.Current.CancellationToken);
        if(known)await File.WriteAllTextAsync(f.Directory.File("catalog.json"),JsonSerializer.Serialize(new[]{new
        {file="candidate.wav",root_midi=57,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))}}),TestContext.Current.CancellationToken);
        Assert.True(await f.Workflow.ReplaceBeatWavAsync(f.Basis,part.Id,file,TestContext.Current.CancellationToken),f.State.Detail);
        var after=f.State.Snapshot;var a=after.State;var b=before.State;
        Assert.Equal(undo+1,f.State.UndoCount);
        Assert.Equal(b.Patterns,a.Patterns);Assert.Equal(b.Placements,a.Placements);Assert.Equal(b.ArrangementClips,a.ArrangementClips);
        Assert.Equal(b.Graphs,a.Graphs);Assert.Equal(b.GraphAttachments,a.GraphAttachments);Assert.Equal(b.Contexts,a.Contexts);
        Assert.Equal(b.Mixer,a.Mixer);Assert.Equal(b.Sounds.Length,a.Sounds.Length);
        var old=b.Sounds.Single(s=>s.Id==part.SoundId);var sound=a.Sounds.Single(s=>s.Id==part.SoundId);
        Assert.Equal(old.Name,sound.Name);Assert.NotEqual(old.ResourceIds,sound.ResourceIds);
        Assert.Equal(root,sound.Parameters["rootPitch"]);Assert.Equal(85,sound.Parameters["releaseMilliseconds"]);
        Assert.Equal(2,a.Patterns.SelectMany(p=>p.Parts).Count(p=>p.SoundId==sound.Id));
        Assert.True(await f.Workflow.UndoAsync());Assert.Same(before,f.State.Snapshot);
        Assert.True(await f.Workflow.UndoAsync(true));Assert.Same(after,f.State.Snapshot);
        string saved=f.Directory.File("bass.seqvium");Assert.True(await f.Workflow.SaveAsync(saved));
        var reopened=ProjectPersistence.Open(saved).Document;
        try
        {
            using var pcm=ProjectMedia.Decode(reopened,sound.ResourceIds.Single());
            Assert.Equal(.2f,pcm.Sample(0,0),4);
            Assert.True(MixerValidation.Same(a.Mixer,reopened.Current.State.Mixer));
        }
        finally {reopened.Close();}
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    [InlineData("cancelled")]
    [InlineData("stale")]
    public async Task RefusedReplacementLeavesSharedSourceAndMusicUnchanged(string failure)
    {
        await using var f=new Fixture();await f.Create();
        var basis=f.Basis;var part=f.Part;string path=f.Directory.File("candidate.wav");
        if(failure!="missing")await File.WriteAllBytesAsync(path,failure=="invalid"?[1,2,3]:WavFixtures.Create([.2f]),TestContext.Current.CancellationToken);
        if(failure=="stale") {Assert.True(await f.Workflow.UndoAsync());Assert.True(await f.Workflow.UndoAsync(true));}
        var before=f.State.Snapshot;int undo=f.State.UndoCount;
        using var cancel=new CancellationTokenSource();if(failure=="cancelled")cancel.Cancel();
        Assert.False(await f.Workflow.ReplaceBeatWavAsync(basis,part.Id,path,cancel.Token));
        Assert.Same(before,f.State.Snapshot);Assert.Equal(undo,f.State.UndoCount);
    }
}
