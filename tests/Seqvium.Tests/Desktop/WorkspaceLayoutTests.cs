// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class WorkspaceLayoutTests
{
    [Fact]
    public async Task MissingFileLeavesUserStorageUntouched()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("config/workspace-layout.json");
        Assert.Equal(new WorkspaceLayoutLoad(null, false), await new WorkspaceLayoutStore(path).LoadAsync());
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)));
    }

    [Fact]
    public async Task SaveReopenRetainsIdentityVisibilityGeometryDockPermissionAndOrder()
    {
        using var directory = new TemporaryDirectory();
        using var workspace = new WorkspaceState();
        workspace.Reflow(1100, 640);
        workspace.Open(WorkspaceState.InspectorId);
        workspace.Dock(WorkspaceState.InspectorId, PaneDock.Right);
        workspace.Collapse(WorkspaceState.InspectorId);
        workspace.Open(WorkspaceState.AppearanceId);
        workspace.SetBounds(WorkspaceState.AppearanceId, new(140, 110, 380, 210));
        workspace.SetDockingAllowed(WorkspaceState.AppearanceId, false);
        var layout = workspace.Capture();
        var path = directory.File("workspace-layout.json");
        var store = new WorkspaceLayoutStore(path);
        Assert.True(await store.SaveAsync(layout));
        var load = await new WorkspaceLayoutStore(path).LoadAsync();
        Assert.False(load.UsedFallback);
        Assert.NotNull(load.Layout);
        using var reopened = new WorkspaceState(load.Layout);
        reopened.Reflow(1100, 640);
        Assert.Equal(layout.Panes, reopened.Panes);
        Assert.Equal(layout.ActivePaneId, reopened.ActivePaneId);
        var text = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        Assert.Contains("\"version\":1", text);
        Assert.DoesNotContain("ProjectDocument", text);
        Assert.DoesNotContain("Appearance", text);
        Assert.DoesNotContain("revision", text);
        Assert.True(await store.SaveAsync(reopened.Capture()));
        Assert.Equal(text, await File.ReadAllTextAsync(path + ".previous", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"version\":0,\"panes\":[]}")]
    [InlineData("{\"version\":99,\"panes\":[]}")]
    [InlineData("{\"version\":\"1\",\"panes\":[]}")]
    [InlineData("{\"version\":1,\"panes\":null}")]
    public async Task CorruptStaleUnsupportedFilesAreNotOverwrittenEvenAfterActionsAndShutdown(string original)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("layout.json");
        await File.WriteAllTextAsync(path, original, TestContext.Current.CancellationToken);
        var store = new WorkspaceLayoutStore(path);
        var load = await store.LoadAsync();
        Assert.True(load.UsedFallback);
        using var workspace = new WorkspaceState(load.Layout);
        workspace.Reflow(640, 390);
        workspace.Open(WorkspaceState.InspectorId);
        var persistence = new WorkspaceLayoutPersistence(store.SaveAsync);
        persistence.Request(workspace.Capture());
        await persistence.ShutdownAsync();
        Assert.False(persistence.LastWriteSucceeded);
        Assert.Equal(original, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Single(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task IndependentValidEntriesSurviveInvalidFieldsUnknownInstancesAndDuplicates()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("layout.json");
        var text = """
                   {"version":1,"width":1920,"height":930,"activePaneId":"missing",
                    "panes":[
                     {"instanceId":"appearance.main","typeId":"appearance","visibility":"visible",
                      "dock":"right","allowDocking":false,"x":1700,"y":850,"width":340,"height":210},
                     {"instanceId":"project-inspector.main","typeId":"project-inspector","visibility":"collapsed",
                      "dock":"alien","allowDocking":true,"x":"bad","y":10,"width":1e100,"height":-10},
                     {"instanceId":"appearance.main","typeId":"appearance"},
                     {"instanceId":"unknown","typeId":"project-inspector"},null]}
                   """;
        await File.WriteAllTextAsync(path, text, TestContext.Current.CancellationToken);
        var store = new WorkspaceLayoutStore(path);
        var load = await store.LoadAsync();
        Assert.True(load.UsedFallback);
        Assert.NotNull(load.Layout);
        using var workspace = new WorkspaceState(load.Layout);
        workspace.Reflow(640, 390);
        Assert.Equal(2, workspace.Panes.Length);
        var appearance = workspace.Get(WorkspaceState.AppearanceId);
        Assert.Equal(PaneVisibility.Visible, appearance.Visibility);
        Assert.False(appearance.AllowDocking);
        Assert.Equal(PaneDock.Floating, appearance.Dock);
        Assert.Equal(PaneVisibility.Collapsed, workspace.Get(WorkspaceState.InspectorId).Visibility);
        Assert.Equal(WorkspaceState.AppearanceId, workspace.ActivePaneId);
        Assert.InRange(workspace.Bounds(WorkspaceState.AppearanceId).X, 0, 640 - 340);
        Assert.False(await store.SaveAsync(workspace.Capture()));
        Assert.Equal(text, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConflictingSavedDocksAreResolvedWithoutDeletingInstances()
    {
        using var directory = new TemporaryDirectory();
        using var workspace = new WorkspaceState();
        workspace.Reflow(1100, 640);
        workspace.Open(WorkspaceState.InspectorId);
        workspace.Open(WorkspaceState.AppearanceId);
        var layout = workspace.Capture();
        layout = layout with { Panes = [.. layout.Panes.Select(pane => pane with { Dock = PaneDock.Left })] };
        var store = new WorkspaceLayoutStore(directory.File("layout.json"));
        Assert.True(await store.SaveAsync(layout));
        var load = await store.LoadAsync();
        using var reopened = new WorkspaceState(load.Layout);
        reopened.Reflow(640, 390);
        Assert.Single(reopened.Panes.Where(pane => pane.Dock == PaneDock.Left));
        Assert.Single(reopened.Panes.Where(pane => pane.Dock == PaneDock.Floating));
        Assert.All(reopened.Panes, pane => Assert.Equal(PaneVisibility.Visible, pane.Visibility));
    }

    [Fact]
    public async Task OversizeAndExcessEntriesCannotAllocateOrOverwriteUnboundedData()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("layout.json");
        var original = new string('x', WorkspaceLayoutStore.MaximumBytes + 1);
        await File.WriteAllTextAsync(path, original, TestContext.Current.CancellationToken);
        var store = new WorkspaceLayoutStore(path);
        Assert.True((await store.LoadAsync()).UsedFallback);
        using var workspace = new WorkspaceState();
        Assert.False(await store.SaveAsync(workspace.Capture()));
        Assert.Equal(original, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        var many = JsonSerializer.Serialize(new { version = 1, panes = Enumerable.Repeat(new { }, 17) });
        var other = directory.File("many.json");
        await File.WriteAllTextAsync(other, many, TestContext.Current.CancellationToken);
        Assert.True((await new WorkspaceLayoutStore(other).LoadAsync()).UsedFallback);
        var deep = "{\"version\":1,\"panes\":[],\"extra\":" + new string('[', 20) + "0" + new string(']', 20) + "}";
        var nested = directory.File("deep.json");
        await File.WriteAllTextAsync(nested, deep, TestContext.Current.CancellationToken);
        Assert.True((await new WorkspaceLayoutStore(nested).LoadAsync()).UsedFallback);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task BackupFailurePreservesLastLayoutAndUnrelatedProjectsMediaRecoveryPreferences()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("layout.json");
        var store = new WorkspaceLayoutStore(path);
        using var workspace = new WorkspaceState();
        workspace.Reflow(1100, 640);
        Assert.True(await store.SaveAsync(workspace.Capture()));
        var before = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        Directory.CreateDirectory(path + ".previous");
        foreach (var name in new[] { "project.json", "managed.wav", "recovery.snapshot", "preferences.json" })
            await File.WriteAllTextAsync(directory.File(name), "User work", TestContext.Current.CancellationToken);
        workspace.Open(WorkspaceState.InspectorId);
        Assert.False(await store.SaveAsync(workspace.Capture()));
        Assert.Equal(before, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        foreach (var name in new[] { "project.json", "managed.wav", "recovery.snapshot", "preferences.json" })
            Assert.Equal("User work",
                await File.ReadAllTextAsync(directory.File(name), TestContext.Current.CancellationToken));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task PendingWritesAreCoalescedAndShutdownWaitsForLatestBeforeDisposal()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var written = new List<WorkspaceLayout>();
        var persistence = new WorkspaceLayoutPersistence(async layout =>
        {
            written.Add(layout);
            if (written.Count == 1)
            {
                entered.SetResult();
                await release.Task;
            }

            return true;
        });
        using var workspace = new WorkspaceState();
        workspace.Reflow(1100, 640);
        persistence.Request(workspace.Capture());
        await entered.Task;
        workspace.Open(WorkspaceState.InspectorId);
        for (var index = 0; index < 100; index++) persistence.Request(workspace.Capture());
        workspace.Open(WorkspaceState.AppearanceId);
        var last = workspace.Capture();
        persistence.Request(last);
        workspace.Dispose(); // Immutable captured writes hold no controls/content/document authority.
        var shutdown = persistence.ShutdownAsync();
        Assert.False(shutdown.IsCompleted);
        Assert.Throws<ObjectDisposedException>(() => persistence.Request(last));
        release.SetResult();
        await shutdown;
        Assert.Equal(2, written.Count);
        Assert.Equal(last, written[^1]);
        Assert.True(persistence.LastWriteSucceeded);
    }

    [Fact]
    public async Task FailedPendingWriteIsObservedAndLaterRequestCanSucceed()
    {
        var attempts = 0;
        var completions = 0;
        var persistence = new WorkspaceLayoutPersistence(_ => Task.FromResult(++attempts > 1));
        persistence.Completed += () => completions++;
        using var workspace = new WorkspaceState();
        persistence.Request(workspace.Capture());
        Assert.False(persistence.LastWriteSucceeded);
        persistence.Request(workspace.Capture());
        await persistence.ShutdownAsync();
        Assert.True(persistence.LastWriteSucceeded);
        Assert.Equal(2, completions);
    }
}
