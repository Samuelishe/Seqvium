// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class SampleBrowserTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DirectoryPagesNeverDecodeRecurseOrScanBeyondTheRequestedBudget()
    {
        using var directory = new TemporaryDirectory();
        System.IO.Directory.CreateDirectory(directory.File("Kick"));
        await File.WriteAllTextAsync(directory.File("Kick/hidden.wav"), "not audio", Cancellation);
        for (int i = 0; i < 280; i++) await File.WriteAllTextAsync(directory.File($"kick-{i:000}.wav"), "not audio", Cancellation);
        using var browse = new BrowserDirectory(directory.Path, "kick");
        var first = await browse.NextAsync(Cancellation);
        Assert.Equal(BrowserDirectory.PageSize, first.Entries.Length);
        Assert.True(first.HasMore);
        Assert.InRange(first.Visited, 1, BrowserDirectory.VisitBudget);
        var second = await browse.NextAsync(Cancellation);
        Assert.False(second.HasMore);
        Assert.Equal(281, first.Entries.Length + second.Entries.Length);
        Assert.DoesNotContain(first.Entries.Concat(second.Entries), e => e.Name == "hidden.wav");
        Assert.All(first.Entries.Concat(second.Entries).Where(e => !e.Directory), e => Assert.True(e.SamplerCompatible));
    }

    [Fact]
    public async Task DirectoryErrorsAndCancellationRemainExplicit()
    {
        using var directory = new TemporaryDirectory();
        using var missing = new BrowserDirectory(directory.File("missing"), "");
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => missing.NextAsync(Cancellation));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        using var browse = new BrowserDirectory(directory.Path, "");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => browse.NextAsync(cancelled.Token));
    }

    [Fact]
    public async Task PreviewAndInspectionAreTransientAndReplacementFailureStopsOldAudio()
    {
        await using var f = new Fixture();
        var before = f.Workflow.State.Snapshot;
        var info = await f.Workflow.InspectBrowserSourceAsync(f.Source, Cancellation);
        Assert.Equal(48000, info.Rate); Assert.Equal(1, info.Channels);
        Assert.InRange(info.Waveform.Peak, .49f, .51f);
        Assert.True(await f.Workflow.PreviewBrowserSourceAsync(f.Source, Cancellation));
        var output = new float[64]; f.Sampler!.Process(output);
        Assert.Contains(output, s => s != 0);
        Assert.Same(before, f.Workflow.State.Snapshot);
        Assert.Equal(0, f.Workflow.State.UndoCount);
        Assert.False(f.Workflow.State.IsDirty);
        Assert.False(System.IO.Directory.Exists(f.Directory.File("owned")));
        File.Delete(f.Source.Path!);
        Assert.False(await f.Workflow.PreviewBrowserSourceAsync(f.Source, Cancellation));
        Assert.True(f.Outputs[0].Joined);
        Assert.Equal(0, f.Workflow.State.UndoCount);
    }

    [Fact]
    public async Task OneUseCreatesEmptyRackAndManagedMediaThenReuseSharesOnlyTheResource()
    {
        await using var f = new Fixture();
        Assert.True(await f.UseRack());
        var state = f.Workflow.State;
        var pattern = Assert.Single(state.Snapshot.State.Patterns);
        Assert.Empty(Assert.Single(pattern.Parts).Notes);
        var resource = Assert.Single(state.Snapshot.State.Resources);
        Assert.Equal(1, state.UndoCount);
        File.Delete(f.Source.Path!);
        var reuse = new BrowserSource(resource.Name, Resource: resource.Id, Lifecycle: state.Document.LifecycleId,
            SamplerCompatible: true);
        Assert.True(await f.UseRack(reuse));
        Assert.Single(f.Workflow.State.Snapshot.State.Resources);
        var parts = Assert.Single(f.Workflow.State.Snapshot.State.Patterns).Parts;
        Assert.Equal(2, parts.Length); Assert.NotEqual(parts[0].SoundId, parts[1].SoundId);
        Assert.True(await f.Workflow.UseBrowserAudioAsync(ArrangementEditBasis.From(f.Workflow.State), reuse,
            new(960), 1, Cancellation));
        Assert.Single(f.Workflow.State.Snapshot.State.Resources);
        Assert.Equal(3, f.Workflow.State.UndoCount);
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("first.seqvium")));
        Assert.True(await f.Workflow.SaveAsync(f.Directory.File("portable/second.seqvium")));
        Assert.True(await f.Workflow.OpenAsync(f.Directory.File("portable/second.seqvium"), UnsavedDecision.Discard));
        state = f.Workflow.State;
        Assert.Equal(2, Assert.Single(state.Snapshot.State.Patterns).Parts.Length);
        Assert.Single(state.Snapshot.State.AudioClips);
        using var pcm = AudioMedia.Decode(state.Document, resource.Id);
        Assert.Equal(4800, pcm.Frames);
        Assert.Empty(state.Document.SavedMediaDiagnostics);
    }

    [Fact]
    public async Task NormalizedAudioResourcesPreviewAndReuseInPlaylistWithoutASecondCopy()
    {
        await using var f = new Fixture();
        Assert.True(await f.Workflow.ImportAudioClipAsync(ArrangementEditBasis.From(f.Workflow.State), f.Source.Path!, default, 0, Cancellation));
        var state = f.Workflow.State;
        var resource = Assert.Single(state.Snapshot.State.Resources);
        var reuse = new BrowserSource(resource.Name, Resource: resource.Id, Lifecycle: state.Document.LifecycleId);
        File.Delete(f.Source.Path!);
        Assert.True(await f.Workflow.PreviewBrowserSourceAsync(reuse, Cancellation));
        Assert.True(await f.Workflow.UseBrowserAudioAsync(ArrangementEditBasis.From(f.Workflow.State), reuse, new(960), 1, Cancellation));
        Assert.Single(f.Workflow.State.Snapshot.State.Resources);
        Assert.Equal(2, f.Workflow.State.Snapshot.State.AudioClips.Length);
        Assert.False(await f.UseRack(reuse));
        Assert.Equal(2, f.Workflow.State.UndoCount);
        var discovery = await SourceAccess.DiscoverProjectAsync(state.Document, cancellationToken: Cancellation);
        Assert.Equal(SourceAvailability.Supported, Assert.Single(discovery.Sources).Availability);
        Assert.True(await f.Workflow.UndoAsync());
        Assert.Single(f.Workflow.State.Snapshot.State.AudioClips);
        Assert.True(await f.Workflow.UndoAsync(redo: true));
        Assert.Equal(2, f.Workflow.State.Snapshot.State.AudioClips.Length);
    }

    [Fact]
    public async Task RunningTransportRefusesPreviewWithoutStoppingOrOpeningAnotherDevice()
    {
        await using var f = new Fixture();
        Assert.True(await f.UseRack());
        var state = f.Workflow.State;
        var pattern = Assert.Single(state.Snapshot.State.Patterns);
        Assert.True(await f.Workflow.ToggleBeatStepAsync(new(state.Document.LifecycleId, state.Generation,
            state.Selected!.Value, pattern.Id), pattern.Parts[0].Id, 0));
        var play = f.Workflow.PlayAsync();
        for (int i = 0; !play.IsCompleted && i < 10000; i++)
        { await f.Workflow.ObserveAsync(); f.Sampler?.Process(new float[64]); await Task.Yield(); }
        Assert.True(await play.WaitAsync(Cancellation)); f.Sampler!.Process(new float[64]);
        int opens = f.Outputs.Count, undo = f.Workflow.State.UndoCount;
        Assert.False(await f.Workflow.PreviewBrowserSourceAsync(f.Source, Cancellation));
        f.Sampler.Process(new float[64]);
        Assert.True(f.Sampler.ReadStatus().Playing);
        Assert.Equal(opens, f.Outputs.Count); Assert.Equal(undo, f.Workflow.State.UndoCount);
    }

    [Theory]
    [InlineData("bad.wav")] [InlineData("missing.wav")] [InlineData("unsupported.mp3")]
    public async Task FailedOrCancelledRackUseAndStaleResourcesAcceptNothing(string name)
    {
        await using var f = new Fixture();
        var bad = new BrowserSource(name, f.Directory.File(name));
        if (name == "bad.wav") await File.WriteAllTextAsync(bad.Path!, "invalid", Cancellation);
        Assert.False(await f.UseRack(bad));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.False(await f.Workflow.UseBrowserRackAsync(ArrangementEditBasis.From(f.Workflow.State), null, f.Source, cancelled.Token));
        Assert.Empty(f.Workflow.State.Snapshot.State.Patterns); Assert.Equal(0, f.Workflow.State.UndoCount);
        Assert.True(await f.UseRack());
        var state = f.Workflow.State;
        var resource = Assert.Single(state.Snapshot.State.Resources);
        var stale = new BrowserSource(resource.Name, Resource: resource.Id, Lifecycle: Guid.NewGuid(), SamplerCompatible: true);
        Assert.False(await f.UseRack(stale)); Assert.Equal(1, f.Workflow.State.UndoCount);
    }

    [Fact]
    public async Task LocationPreferencesAreBoundedIndependentAndPreserveMalformedOriginals()
    {
        using var directory = new TemporaryDirectory();
        string path = directory.File("browser-locations.json");
        var store = new BrowserLocationsStore(path);
        Assert.Empty((await store.LoadAsync()).Roots);
        var saved = new BrowserLocations([directory.Path], [directory.File("Kick")], directory.File("Kick"));
        Assert.True(await store.SaveAsync(saved));
        var loaded = await new BrowserLocationsStore(path).LoadAsync();
        Assert.Equal(saved.Roots, loaded.Roots); Assert.Equal(saved.Last, loaded.Last);
        await File.WriteAllTextAsync(path, "broken original", Cancellation);
        store = new(path); Assert.Empty((await store.LoadAsync()).Roots);
        Assert.False(await store.SaveAsync(saved));
        Assert.Equal("broken original", await File.ReadAllTextAsync(path, Cancellation));
    }

    [Theory]
    [InlineData("cancel")] [InlineData("stale")] [InlineData("missing")]
    public async Task AudioReuseRevalidatesAtAcceptanceAndCannotLeavePartialUses(string failure)
    {
        await using var f = new Fixture();
        Assert.True(await f.UseRack());
        var document = f.Workflow.State.Document;
        var resource = Assert.Single(document.Current.State.Resources);
        using var request = await AudioMedia.PrepareReuseAsync(document, resource.Id, Cancellation);
        if (failure == "stale") document.Edit("Intervening edit", edit => edit.AddPattern("Other", new(3840)));
        else if (failure == "missing")
            foreach (string path in System.IO.Directory.EnumerateFiles(f.Directory.File("owned"), "*.wav", SearchOption.AllDirectories)) File.Delete(path);
        var before = document.Current;
        if (failure == "cancel")
        {
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            Assert.ThrowsAny<OperationCanceledException>(() => request.Accept(default, 0, cancelled.Token));
        }
        else if (failure == "stale") Assert.Throws<InvalidOperationException>(() => request.Accept(default, 0, Cancellation));
        else Assert.Throws<FileNotFoundException>(() => request.Accept(default, 0, Cancellation));
        Assert.Same(before, document.Current);
        Assert.Empty(document.Current.State.AudioClips);
    }

    [Fact]
    public async Task FailedMusicalReuseCallbackRollsBackTheIndependentSoundToo()
    {
        await using var f = new Fixture(); Assert.True(await f.UseRack());
        var document = f.Workflow.State.Document;
        using var request = ProjectMedia.BeginReuse(document, document.Current.State.Resources[0].Id, "Reuse");
        await request.PrepareAsync(Cancellation);
        var before = document.Current;
        Assert.Throws<InvalidOperationException>(() => request.Accept((_, _) => throw new InvalidOperationException("Rejected musical use"), Cancellation));
        Assert.Same(before, document.Current);
    }

    [Fact]
    public void BrowserLabelsHaveEnglishAndRussianAndItsDefaultIsAnOptionalLeftPane()
    {
        foreach (string key in HostLocalizer.English.Keys.Where(k => k.StartsWith("Browser.", StringComparison.Ordinal)))
            Assert.True(HostLocalizer.Russian.ContainsKey(key), key);
        using var workspace = new Seqvium.Desktop.Workspace.WorkspaceState();
        Assert.Equal(Seqvium.Desktop.Workspace.PaneDock.Left, workspace.Get(Seqvium.Desktop.Workspace.WorkspaceState.BrowserId).Dock);
    }

    private sealed class Borrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() { Assert.True(Joined); }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly TemporaryDirectory Directory = new();
        internal readonly DesktopWorkflow Workflow;
        internal readonly BrowserSource Source;
        internal readonly List<Borrower> Outputs = [];
        internal RealtimeSampler? Sampler;
        internal Fixture()
        {
            string path = Directory.File("Kick.wav");
            File.WriteAllBytes(path, WavFixtures.Create(Enumerable.Repeat(.5f, 4800).ToArray(), 48000, floating: true));
            Source = new("Kick.wav", path, SamplerCompatible: true);
            Workflow = new(ProjectDocument.Create(), () =>
            {
                Sampler = new(48000, 2, 32); var output = new Borrower(); Outputs.Add(output);
                return new(output, Sampler, "Deterministic browser output", () => null);
            }, Directory.File("owned"));
        }
        internal Task<bool> UseRack(BrowserSource? source = null) => Workflow.UseBrowserRackAsync(
            ArrangementEditBasis.From(Workflow.State), Workflow.State.Selected, source ?? Source, Cancellation);
        public async ValueTask DisposeAsync() { await Workflow.DisposeAsync(); Directory.Dispose(); }
    }
}
