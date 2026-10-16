// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class ResamplingWorkflowTests
{
    private static Task<bool> Render(DesktopWorkflow w) => w.ResamplePatternAsync(w.State.Document.LifecycleId,
        w.State.Generation, w.State.Selected!.Value, TestContext.Current.CancellationToken);

    [Fact]
    public async Task RealRenderIsOneManagedUndoEditAndImmediatelyReusableInRackAndPlaylistAfterRelocation()
    {
        using var f = await ResamplingFixture.Create();
        await using var w = new DesktopWorkflow(f.Document, mediaDirectory: f.Directory.File("accepted"));
        await w.ObserveAsync();
        var before = w.State.Snapshot.State; int history = w.State.UndoCount;
        var expected = await f.Render(TestContext.Current.CancellationToken);
        Assert.True(await Render(w), w.State.Resampling?.Detail);
        var resource = w.State.Resampling!.Resource!.Value;
        Assert.Equal("Resample.Ready", w.State.Resampling.Message);
        Assert.Equal(history + 1, w.State.UndoCount);
        Assert.Equal(before.Patterns, w.State.Snapshot.State.Patterns);
        Assert.Equal(before.Placements, w.State.Snapshot.State.Placements);
        Assert.Equal(before.Graphs, w.State.Snapshot.State.Graphs);
        Assert.Equal(3, w.State.Snapshot.State.Resources.Length);
        using (var pcm = ProjectMedia.Decode(w.State.Document, resource))
        using (var oracle = WavDecoder.Decode(expected.Wave))
            for (int i = 0; i < pcm.Frames; i++)
                for (int ch = 0; ch < pcm.Channels; ch++) Assert.Equal(oracle.Sample(i, ch), pcm.Sample(i, ch));
        var accepted = w.State.Snapshot;
        Assert.True(await w.UndoAsync()); Assert.Equal(before, w.State.Snapshot.State);
        Assert.True(await w.UndoAsync(true)); Assert.Equal(accepted, w.State.Snapshot);
        Assert.Equal("Create sample from Pattern", w.State.Document.NextUndoDescription);
        var source = new BrowserSource("Two voices (Resampled)", Resource: resource,
            Lifecycle: w.State.Document.LifecycleId, SamplerCompatible: true);
        Assert.True(await w.UseBrowserRackAsync(ArrangementEditBasis.From(w.State), null, source, TestContext.Current.CancellationToken));
        Assert.Equal(2, w.State.Snapshot.State.Patterns.Length);
        Assert.True(await w.UseBrowserAudioAsync(ArrangementEditBasis.From(w.State), source, new(0), 0, TestContext.Current.CancellationToken));
        Assert.Equal(3, w.State.Snapshot.State.Resources.Length);
        Assert.True(await w.SaveAsync(f.Directory.File("first.seqvium")));
        string portable = f.Directory.File("portable"); System.IO.Directory.CreateDirectory(portable);
        Assert.True(await w.SaveAsync(Path.Combine(portable, "music.seqvium")));
        Assert.True(await w.NewAsync(UnsavedDecision.Discard));
        string moved = f.Directory.File("relocated"); System.IO.Directory.Move(portable, moved);
        // Only the relocated project and sidecar remain. Every original managed/external copy is unavailable.
        foreach (string wave in System.IO.Directory.GetFiles(f.Directory.Path, "*.wav", SearchOption.AllDirectories)
                     .Where(path => !path.StartsWith(moved + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))) File.Delete(wave);
        Assert.True(await w.OpenAsync(Path.Combine(moved, "music.seqvium"), UnsavedDecision.Discard));
        Assert.Empty(w.State.Document.SavedMediaDiagnostics);
        using var relocated = ProjectMedia.Decode(w.State.Document, resource);
        Assert.Equal(28800, relocated.Frames); Assert.Equal(2, relocated.Channels);
        using var rack = GraphPreparation.Prepare(w.State.Document, w.State.Selected!.Value, 48000, 2, 1024, cancellationToken: TestContext.Current.CancellationToken);
        using var song = ArrangementPreparation.Prepare(w.State.Document, 48000, 2, 1024, TestContext.Current.CancellationToken);
        Assert.Contains(GraphExecutionTests.Render(song, [1024]), value => value != 0);
    }

    [Theory]
    [InlineData("cancel")] [InlineData("edit")] [InlineData("delete")] [InlineData("undo-redo")]
    [InlineData("new")] [InlineData("open")] [InlineData("close")]
    public async Task LateCompletedRenderCannotCommitAfterCancellationEditsHistoryOrLifecycle(string action)
    {
        using var f = await ResamplingFixture.Create();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var w = new DesktopWorkflow(f.Document, mediaDirectory: f.Directory.File("accepted"),
            renderPattern: async (doc, target, token, progress) =>
            {
                var sample = await PatternResampling.RenderAsync(doc, target, token, progress);
                using var registration = token.Register(() => cancelled.TrySetResult());
                entered.TrySetResult();
                await release.Task.WaitAsync(TestContext.Current.CancellationToken); // Simulate late completion ignoring cancellation.
                return sample;
            });
        await w.ObserveAsync();
        if (action == "open") Assert.True(await w.SaveAsync(f.Directory.File("open.seqvium")));
        var pending = Render(w);
        try
        {
            await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.True(w.State.Resampling!.Busy);
            Assert.False(await Render(w)); // One owned render; completion order cannot choose a result.
            Task<bool>? lifecycle = null;
            switch (action)
            {
                case "cancel": await w.CancelResamplingAsync(); break;
                case "edit": Assert.True(await w.SetTempoAsync(w.State.Document.LifecycleId, w.State.Generation, 121)); break;
                case "delete":
                    Assert.True(await w.DeleteBeatAsync(new(w.State.Document.LifecycleId, w.State.Generation,
                        f.Attachment.Id, f.Pattern))); break;
                case "undo-redo": Assert.True(await w.UndoAsync()); Assert.True(await w.UndoAsync(true)); break;
                case "new": lifecycle = w.NewAsync(UnsavedDecision.Discard); break;
                case "open": lifecycle = w.OpenAsync(f.Directory.File("open.seqvium"), UnsavedDecision.Discard); break;
                case "close": lifecycle = w.CloseAsync(UnsavedDecision.Discard); break;
            }
            await cancelled.Task.WaitAsync(TestContext.Current.CancellationToken);
            if (lifecycle is not null) Assert.False(lifecycle.IsCompleted); // Lifecycle must join the withdrawn worker.
            int history = w.State.UndoCount;
            release.TrySetResult();
            Assert.False(await pending);
            if (lifecycle is not null) Assert.True(await lifecycle);
            else
            {
                Assert.Equal(history, w.State.UndoCount);
                Assert.Equal(action == "cancel" ? "Resample.Cancelled" : "Resample.Stale", w.State.Resampling!.Message);
            }
            Assert.DoesNotContain(w.State.Snapshot.State.Resources, r => r.Name.EndsWith("(Resampled)", StringComparison.Ordinal));
            Assert.Empty(System.IO.Directory.GetFiles(f.Directory.Path, "*.pending", SearchOption.AllDirectories));
            string owned = f.Directory.File("accepted");
            Assert.Empty(System.IO.Directory.Exists(owned) ? System.IO.Directory.GetFiles(owned, "*.wav", SearchOption.AllDirectories) : []);
        }
        finally { release.TrySetResult(); await pending; }
    }

    [Fact]
    public async Task DurableWriteFailureDoesNotCreateResourceHistoryOrAlterSource()
    {
        using var f = await ResamplingFixture.Create();
        // Keep original roots readable while a fresh reopened lifecycle owns the failed destination.
        var document = ProjectDocument.Reopen(f.Document.Current);
        document.MediaRoots.AddRange(f.Document.MediaRoots);
        string blocked = f.Directory.File("blocked"); await File.WriteAllTextAsync(blocked, "owned obstruction", TestContext.Current.CancellationToken);
        await using var w = new DesktopWorkflow(document, mediaDirectory: blocked); await w.ObserveAsync();
        var before = w.State.Snapshot; int history = w.State.UndoCount;
        Assert.False(await Render(w));
        Assert.Same(before, w.State.Snapshot); Assert.Equal(history, w.State.UndoCount);
        Assert.Equal("Resample.Failed", w.State.Resampling!.Message);
        Assert.Equal("owned obstruction", await File.ReadAllTextAsync(blocked, TestContext.Current.CancellationToken));
    }
}
