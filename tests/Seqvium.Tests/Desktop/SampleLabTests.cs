// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Seqvium.Core;
using Seqvium.Desktop.Extensions;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class SampleLabTests
{
    private sealed class Output : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Assert.True(Joined);
    }
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;
        internal readonly List<(RealtimeSampler Sampler, Output Output)> Outputs = [];
        internal RealtimeSampler? Sampler => Outputs.LastOrDefault().Sampler;
        internal WorkflowState State => Workflow.State;
        internal SampleLabState Lab => State.Lab!;
        internal Fixture(ExtensionHost? host = null)
        {
            Workflow = new(ProjectDocument.Create(), () =>
            {
                var sampler = new RealtimeSampler(48000, 2, 256); var output = new Output();
                Outputs.Add((sampler, output)); return new(output, sampler, "Deterministic Lab", () => null);
            }, Directory.File("media"), extensions: host);
        }
        internal async Task Initialize()
        {
            await Workflow.Extensions.DiscoverAsync(Path.Combine(AppContext.BaseDirectory, "Extensions"), TestContext.Current.CancellationToken);
            Assert.True(await Workflow.Extensions.EnableAsync(PercussionGenerator.Package), string.Join("; ", Workflow.Extensions.Status.SelectMany(p => p.Diagnostics).Select(d => d.Code + ": " + d.Detail)));
            Assert.True(await Workflow.RefreshLabGeneratorAsync());
        }
        internal async Task<Guid> Generate(int seed = 42)
        {
            Assert.True(await Workflow.GenerateLabAsync(SampleExplorationKind.Random, seed));
            return Lab.Selected!.Value;
        }
        internal async Task DriveUntil(Func<bool> done)
        {
            for (int i = 0; !done() && i < 20000; i++)
            {
                await Workflow.ObserveAsync();
                foreach (var (sampler, _) in Outputs.Where(o => !o.Output.Joined)) sampler.Process(new float[512]);
                await Task.Yield();
            }
            Assert.True(done());
        }
        internal async Task Drive(Task<bool> task)
        {
            await DriveUntil(() => task.IsCompleted);
            Assert.True(await task.WaitAsync(TestContext.Current.CancellationToken));
            Sampler?.Process(new float[512]);
        }
        public async ValueTask DisposeAsync()
        {
            await Workflow.DisposeAsync();
            Assert.All(Outputs, output => { Assert.True(output.Output.Joined); Assert.Equal(0, output.Sampler.LivePreparedStates); });
            Directory.Dispose();
        }
    }

    [Fact]
    public async Task StandaloneGenerationAndComparisonDoNotEditMusicAndAcceptSeveralIndependentCandidates()
    {
        await using var f = new Fixture(); await f.Initialize();
        var original = f.State.Snapshot;
        var a = await f.Generate(42); var b = await f.Generate(52);
        Assert.Equal(original, f.State.Snapshot); Assert.False(f.State.IsDirty); Assert.Equal(0, f.State.UndoCount);
        Assert.True(await f.Workflow.ConfigureLabAsync(selection: a, reference: a));
        decimal decay = f.Lab.Entries.Single(e => e.Id == a).Recipe.Values["decay"];
        await f.Workflow.ConfigureLabAsync(control: "decay", held: decay, changeLock: true);
        Assert.True(await f.Workflow.GenerateLabAsync(SampleExplorationKind.Similar, 62));
        var c = f.Lab.Selected!.Value;
        Assert.Equal(a, f.Lab.Reference);
        Assert.Equal(a, f.Lab.Entries.Single(e => e.Id == c).Recipe.Reference);
        Assert.Equal(decay, f.Lab.Entries.Single(e => e.Id == c).Recipe.Values["decay"]);
        await f.Workflow.ConfigureLabAsync(selection: b);
        Assert.Equal(a, f.Lab.Reference); Assert.Equal(0, f.State.UndoCount);
        await f.Drive(f.Workflow.AuditionLabAsync(a, false));
        Assert.False(f.State.IsDirty); Assert.Equal(original, f.State.Snapshot);
        Assert.Equal(a, f.Lab.Audible);
        Assert.True(await f.Workflow.AcceptLabAsync(f.State.Document.LifecycleId, a, LabAcceptance.Resource));
        Assert.True(await f.Workflow.AcceptLabAsync(f.State.Document.LifecycleId, b, LabAcceptance.Resource));
        Assert.Equal(2, f.State.Snapshot.State.Resources.Length); Assert.Empty(f.State.Snapshot.State.Patterns);
        Assert.Equal(2, f.State.UndoCount); Assert.Equal(3, f.Lab.Entries.Length);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Single(f.State.Snapshot.State.Resources);
        Assert.Equal(0, f.Lab.Entries.Single(e => e.Id == b).Accepted);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Equal(2, f.State.Snapshot.State.Resources.Length);
        await f.Workflow.ConfigureLabAsync(family: "Clap"); Assert.Empty(f.Lab.Locks); Assert.Null(f.Lab.Reference);
        Assert.Equal(3, f.Lab.Entries.Length);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task AcceptedRackAndPlaylistSurviveDisabledGeneratorUndoSaveAsOpenAndExport(bool playlist)
    {
        await using var f = new Fixture(); await f.Initialize();
        var a = await f.Generate();
        Assert.True(await f.Workflow.AcceptLabAsync(f.State.Document.LifecycleId, a,
            playlist ? LabAcceptance.Playlist : LabAcceptance.Rack));
        var accepted = f.State.Snapshot;
        Assert.Equal(1, f.State.UndoCount);
        Assert.True(await f.Workflow.UndoAsync()); Assert.Empty(f.State.Snapshot.State.Resources);
        Assert.True(await f.Workflow.UndoAsync(true)); Assert.Equal(accepted, f.State.Snapshot);
        Assert.True(await f.Workflow.SetExtensionEnabledAsync(PercussionGenerator.Package, false));
        Assert.Equal("Lab.Unavailable", f.Lab.Message);
        Assert.True(await f.Workflow.SetExtensionEnabledAsync(PercussionGenerator.Package, true));
        Assert.Equal(2, f.Lab.Families.Length);
        Assert.Single(f.Lab.Entries);
        Assert.True(await f.Workflow.SetExtensionEnabledAsync(PercussionGenerator.Package, false));
        Assert.False(await f.Workflow.GenerateLabAsync(SampleExplorationKind.Random));
        Assert.Single(f.Lab.Entries);
        await f.Drive(f.Workflow.PlayAsync()); Assert.True(f.Sampler!.ReadStatus().Playing);
        await f.Workflow.StopAsync();
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("one.seqvium")));
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("portable.seqvium")));
        Assert.True(await f.Workflow.NewAsync(UnsavedDecision.Discard)); Assert.Empty(f.Lab.Entries);
        Assert.True(await f.Workflow.OpenAsync(f.Directory.File("portable.seqvium"), UnsavedDecision.Discard));
        Assert.True(ProjectDocument.HasSameContent(accepted.State, f.State.Snapshot.State));
        using var pcm = ProjectMedia.Decode(f.State.Document, Assert.Single(f.State.Snapshot.State.Resources).Id);
        Assert.Contains(Enumerable.Range(0, pcm.Frames), i => Math.Abs(pcm.Sample(i, 0)) > .1);
        var export = playlist ? await PatternWaveExport.ExportArrangementAsync(f.State.Document, f.Directory.File("music.wav"), TestContext.Current.CancellationToken) :
            await PatternWaveExport.ExportAsync(f.State.Document, f.State.Selected!.Value, f.Directory.File("music.wav"), TestContext.Current.CancellationToken);
        Assert.True(export.Frames > 0); Assert.Equal(0, export.ClippedSamples);
        await f.Drive(f.Workflow.PlayAsync()); Assert.True(f.Sampler!.ReadStatus().Playing);
    }

    [Fact]
    public async Task CapturedContextIgnoresSelectionAndUnrelatedRenameButRefusesChangedSourceAndDeletion()
    {
        await using var f = new Fixture(); await f.Initialize();
        var a = await f.Generate();
        Assert.True(await f.Workflow.AcceptLabAsync(f.State.Document.LifecycleId, a, LabAcceptance.Rack));
        var first = f.State.Selected!.Value; var pattern = f.State.Snapshot.State.Patterns[0]; var part = pattern.Parts[0];
        Assert.True(await f.Workflow.SetLabTargetAsync(first, part.Id));
        var b = await f.Generate(32);
        Assert.True(await f.Workflow.AcceptLabAsync(f.State.Document.LifecycleId, b, LabAcceptance.Rack));
        var second = f.State.Selected!.Value;
        var secondPattern = f.State.Snapshot.State.Patterns[1];
        Assert.True(await f.Workflow.RenameBeatAsync(new(f.State.Document.LifecycleId, f.State.Generation, second, secondPattern.Id), "Other"));
        Assert.True(f.Lab.TargetValid);
        var otherSound = f.State.Snapshot.State.Sounds.Single(s => s.Id == secondPattern.Parts[0].SoundId);
        int undo = f.State.UndoCount;
        Assert.True(await f.Workflow.AcceptLabAsync(f.State.Document.LifecycleId, b, LabAcceptance.SharedSound));
        Assert.Equal(undo + 1, f.State.UndoCount);
        Assert.Equal(otherSound, f.State.Snapshot.State.Sounds.Single(s => s.Id == otherSound.Id));
        Assert.False(f.Lab.TargetValid); // A changed source needs a fresh explicit target, even after Undo.
        Assert.True(await f.Workflow.UndoAsync()); Assert.False(f.Lab.TargetValid);
        Assert.False(await f.Workflow.AcceptLabAsync(f.State.Document.LifecycleId, b, LabAcceptance.SharedSound));
        Assert.True(await f.Workflow.SetLabTargetAsync(first, part.Id));
        await f.Workflow.SelectAsync(first);
        Assert.True(await f.Workflow.DeleteBeatChannelAsync(new(f.State.Document.LifecycleId, f.State.Generation, first, pattern.Id), part.Id));
        Assert.False(f.Lab.TargetValid); Assert.Equal(2, f.Lab.Entries.Length);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task ContextAuditionPreservesCanonicalStateTimeAndMixerAndRestoresCurrentSound(bool song)
    {
        await using var f = new Fixture(); await f.Initialize();
        var a = await f.Generate();
        await f.Workflow.AcceptLabAsync(f.State.Document.LifecycleId, a, LabAcceptance.Rack);
        var target = f.State.Selected!.Value; var part = f.State.Snapshot.State.Patterns[0].Parts[0];
        await f.Workflow.SetLabTargetAsync(target, part.Id);
        var b = await f.Generate(99);
        await f.Workflow.EditMixerAsync(new(f.State.Document.LifecycleId, f.State.Generation), "Mixer for context", e => e.InitializeMixer());
        if (song)
        {
            await f.Workflow.AddArrangementClipAsync(ArrangementEditBasis.From(f.State), target, default, 0, 4);
            await f.Workflow.SetSongModeAsync(true);
        }
        await f.Drive(f.Workflow.PlayAsync());
        var canonical = f.State.Snapshot; int undo = f.State.UndoCount;
        long epoch = f.Sampler!.ReadStatus().Epoch;
        await f.Drive(f.Workflow.AuditionLabAsync(b, true));
        Assert.Equal(b, f.Sampler.SampleAuditionCandidate);
        Assert.Equal(epoch, f.Sampler.ReadStatus().Epoch); Assert.True(f.Sampler.ReadStatus().Playing);
        Assert.Equal(canonical, f.State.Snapshot); Assert.Equal(undo, f.State.UndoCount);
        Assert.Single(f.Outputs);
        Assert.True(await f.Workflow.SetGainAsync(.12m));
        var current = f.State.Snapshot;
        Assert.True(await f.Workflow.RestoreLabSoundAsync());
        await f.DriveUntil(() => f.Sampler.SampleAuditionCandidate is null && f.State.Execution?.PreparationPending == false);
        Assert.Equal(current, f.State.Snapshot); Assert.Equal(epoch, f.Sampler.ReadStatus().Epoch);
        await f.Drive(f.Workflow.StopAsync(true));
        Assert.False(f.Sampler.ReadStatus().Playing); Assert.Null(f.Lab.Audible);
    }

    private sealed class HeldGenerator : IExploringSampleGenerator
    {
        private readonly PercussionGenerator _inner = new();
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal SampleCandidate? Late;
        private int _calls;
        public ImmutableArray<SampleParameter> Parameters => [];
        public ImmutableArray<SampleFamily> Families => _inner.Families;
        public async Task<SampleCandidate> GenerateAsync(SampleGenerationRequest request, CancellationToken cancellation)
        {
            bool held = Interlocked.Increment(ref _calls) == 1;
            if (held) { Entered.SetResult(); await Release.Task; }
            var candidate = await _inner.GenerateAsync(request, CancellationToken.None);
            if (held) Late = candidate;
            return candidate;
        }
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
    private static ExtensionHost HeldHost(HeldGenerator provider) => new(new Dictionary<ExtensionProviderKey, Func<CancellationToken, Task<ISampleGenerator>>>
    { [new(PercussionGenerator.Package, PercussionGenerator.Version, PercussionGenerator.Capability)] = _ => Task.FromResult<ISampleGenerator>(provider) });

    [Fact]
    public async Task UnrelatedProjectEditsDoNotCancelStandaloneGeneration()
    {
        var provider = new HeldGenerator();
        await using var f = new Fixture(HeldHost(provider)); await f.Initialize();
        Assert.True(await f.Workflow.CreateBeatAsync(f.State.Document.LifecycleId, f.State.Generation, "Initial"));
        var pending = f.Workflow.GenerateLabAsync(SampleExplorationKind.Random, 10);
        await provider.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        var basis = new BeatEditBasis(f.State.Document.LifecycleId, f.State.Generation, f.State.Selected!.Value,
            f.State.Snapshot.State.Patterns.Single().Id);
        Assert.True(await f.Workflow.RenameBeatAsync(basis, "Renamed during synthesis"));
        int undo = f.State.UndoCount;
        provider.Release.SetResult(); Assert.True(await pending);
        Assert.Single(f.Lab.Entries); Assert.Equal(undo, f.State.UndoCount);
    }

    [Theory] [InlineData("supersede")] [InlineData("cancel")] [InlineData("new")]
    public async Task PendingGenerationBlocksDisableAndDisposesLateOutputAfterSupersessionCancelOrNew(string operation)
    {
        var provider = new HeldGenerator();
        await using var f = new Fixture(HeldHost(provider)); await f.Initialize();
        var pending = f.Workflow.GenerateLabAsync(SampleExplorationKind.Random, 10);
        await provider.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(await f.Workflow.SetExtensionEnabledAsync(PercussionGenerator.Package, false));
        Task<bool>? later = null;
        if (operation == "supersede") later = f.Workflow.GenerateLabAsync(SampleExplorationKind.Random, 20);
        else if (operation == "cancel") await f.Workflow.CancelLabAsync();
        else later = f.Workflow.NewAsync(UnsavedDecision.Discard);
        // Barrier lets the owner withdraw request authority before uncooperative code completes.
        await f.Workflow.ObserveAsync(); provider.Release.SetResult();
        Assert.False(await pending);
        if (later is not null) Assert.True(await later);
        Assert.NotNull(provider.Late); Assert.False(provider.Late.IsAvailable);
        Assert.Equal(operation == "supersede" ? 1 : 0, f.Lab.Entries.Length);
        Assert.True(await f.Workflow.Extensions.DisableAsync(PercussionGenerator.Package));
        Assert.False(f.State.IsDirty);
    }
}
