// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class WorkspaceStateTests
{
    private const string Inspector = WorkspaceState.InspectorId;
    private const string Appearance = WorkspaceState.AppearanceId;

    private static WorkspaceState Create(double width = 1100, double height = 640)
    {
        var workspace = new WorkspaceState();
        workspace.Reflow(width, height);
        return workspace;
    }

    [Fact]
    public void StableInstancesTypesAndLocalizedTitlesAreDistinctAndDefaultIsOptional()
    {
        using var workspace = Create();
        var localizer = new HostLocalizer();
        Assert.Equal(2, workspace.Panes.Select(pane => pane.InstanceId).Distinct().Count());
        Assert.Equal(2, workspace.Panes.Select(pane => pane.TypeId).Distinct().Count());
        foreach (var definition in WorkspaceState.Definitions)
        {
            Assert.NotEqual(localizer.Get(definition.TitleKey, HostLanguage.English), definition.InstanceId);
            Assert.NotEqual(localizer.Get(definition.TitleKey, HostLanguage.English),
                localizer.Get(definition.TitleKey, HostLanguage.Russian));
            Assert.Equal(PaneVisibility.Hidden, workspace.Get(definition.InstanceId).Visibility);
        }

        Assert.Null(workspace.ActivePaneId);
    }

    [Fact]
    public void ActivationOfOverlappedPanePreservesGeometryAndBoundsFrontOrder()
    {
        using var workspace = Create();
        workspace.Open(Inspector);
        workspace.Open(Appearance);
        workspace.SetBounds(Appearance, workspace.Bounds(Inspector) with { X = 60, Y = 60 });
        var before = workspace.Panes.ToDictionary(pane => pane.InstanceId, pane => pane.FloatingBounds);
        for (var index = 0; index < 10000; index++) workspace.Activate(index % 2 == 0 ? Inspector : Appearance);
        workspace.Activate(Inspector);
        Assert.Equal(Inspector, workspace.ActivePaneId);
        Assert.Equal(Inspector, workspace.Panes[^1].InstanceId);
        Assert.Equal(2, workspace.Panes.Length);
        Assert.All(workspace.Panes, pane => Assert.Equal(before[pane.InstanceId], pane.FloatingBounds));
        var events = 0;
        workspace.Changed += () => events++;
        workspace.Activate(Inspector);
        Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(-1000, -1000, 1, 1)]
    [InlineData(100000, 100000, 100000, 100000)]
    [InlineData(double.NaN, double.PositiveInfinity, double.NaN, double.NegativeInfinity)]
    [InlineData(80, 90, 350, 240)]
    public void MoveResizeClampsInvalidOversizeAndMinimumGeometry(double x, double y, double width, double height)
    {
        using var workspace = Create(640, 390);
        workspace.Open(Inspector);
        workspace.SetBounds(Inspector, new(x, y, width, height));
        AssertUsable(workspace, Inspector);
    }

    [Theory]
    [InlineData(640, 390)]
    [InlineData(1100, 640)]
    [InlineData(1920, 930)]
    [InlineData(100, 90)]
    public void ResizeReflowContainsFloatingAndDockedSurfaces(double width, double height)
    {
        using var workspace = Create(1920, 930);
        workspace.Open(Inspector);
        workspace.Open(Appearance);
        workspace.SetBounds(Appearance, new(1550, 650, 350, 240));
        workspace.Dock(Inspector, PaneDock.Left);
        workspace.Reflow(width, height);
        AssertUsable(workspace, Appearance);
        var dock = workspace.Bounds(Inspector);
        Assert.Equal(0, dock.X);
        Assert.Equal(height, dock.Height);
        Assert.InRange(dock.Width, 0, width / 2);
        workspace.Dock(Inspector, PaneDock.Floating);
        AssertUsable(workspace, Inspector);
    }

    [Fact]
    public void CollapseRestoreAndHideReopenPreserveInstanceAndRetainedPlacement()
    {
        using var workspace = Create();
        workspace.Open(Inspector);
        workspace.Open(Appearance);
        workspace.SetBounds(Inspector, new(60, 75, 360, 250));
        workspace.Dock(Inspector, PaneDock.Right);
        var before = workspace.Get(Inspector);
        workspace.Collapse(Inspector);
        Assert.Equal(PaneVisibility.Collapsed, workspace.Get(Inspector).Visibility);
        Assert.NotEqual(Inspector, workspace.ActivePaneId);
        workspace.Activate(Inspector);
        Assert.NotEqual(Inspector, workspace.ActivePaneId);
        workspace.Open(Inspector);
        Assert.Equal(before, workspace.Get(Inspector));
        workspace.Hide(Inspector);
        Assert.Equal(PaneVisibility.Hidden, workspace.Get(Inspector).Visibility);
        workspace.Open(Inspector);
        Assert.Equal(before, workspace.Get(Inspector));
        Assert.Equal(2, workspace.Panes.Length);
    }

    [Fact]
    public void DockingIsDeliberateReversibleAndPreferenceBelongsToOnePane()
    {
        using var workspace = Create();
        workspace.Open(Inspector);
        workspace.Open(Appearance);
        workspace.SetBounds(Inspector, new(0, 0, 340, 240));
        Assert.Equal(PaneDock.Floating, workspace.Get(Inspector).Dock);
        var floating = workspace.Bounds(Inspector);
        workspace.Dock(Inspector, PaneDock.Left);
        workspace.Dock(Inspector, PaneDock.Floating);
        Assert.Equal(floating, workspace.Bounds(Inspector));
        workspace.Dock(Inspector, PaneDock.Left);
        workspace.SetDockingAllowed(Inspector, false);
        Assert.Equal(PaneDock.Floating, workspace.Get(Inspector).Dock);
        workspace.Dock(Inspector, PaneDock.Right);
        Assert.Equal(PaneDock.Floating, workspace.Get(Inspector).Dock);
        workspace.Dock(Appearance, PaneDock.Right);
        Assert.Equal(PaneDock.Right, workspace.Get(Appearance).Dock);
        Assert.True(workspace.Get(Appearance).AllowDocking);
    }

    [Theory]
    [InlineData(PaneDock.Left)]
    [InlineData(PaneDock.Right)]
    public void OccupiedDockReturnsPreviousOccupantToReachableFloatingPlacement(PaneDock dock)
    {
        using var workspace = Create(640, 390);
        workspace.Open(Inspector);
        workspace.Open(Appearance);
        workspace.Dock(Inspector, dock);
        var retained = workspace.Get(Inspector).FloatingBounds;
        workspace.Dock(Appearance, dock);
        Assert.Equal(PaneDock.Floating, workspace.Get(Inspector).Dock);
        Assert.Equal(retained, workspace.Get(Inspector).FloatingBounds);
        Assert.Equal(PaneVisibility.Visible, workspace.Get(Inspector).Visibility);
        AssertUsable(workspace, Inspector);
        Assert.Equal(PaneDock.Floating, workspace.Panes.Single(pane => pane.InstanceId != Appearance).Dock);
    }

    [Fact]
    public void BothEdgesAndDockResizeKeepUsefulSizesAndSeparateFloatingCoordinates()
    {
        using var workspace = Create(640, 390);
        workspace.Open(Inspector);
        workspace.Open(Appearance);
        workspace.Dock(Inspector, PaneDock.Left);
        workspace.Dock(Appearance, PaneDock.Right);
        var previous = workspace.Get(Inspector).FloatingBounds;
        workspace.SetBounds(Inspector, new(0, 0, 10000, 900));
        Assert.Equal(previous.X, workspace.Get(Inspector).FloatingBounds.X);
        Assert.Equal(previous.Y, workspace.Get(Inspector).FloatingBounds.Y);
        Assert.Equal(previous.Height, workspace.Get(Inspector).FloatingBounds.Height);
        var left = workspace.Bounds(Inspector);
        var right = workspace.Bounds(Appearance);
        Assert.True(left.X + left.Width <= right.X);
        Assert.True(left.Width >= WorkspaceState.Definition(Inspector).MinimumWidth);
        Assert.True(right.Width >= WorkspaceState.Definition(Appearance).MinimumWidth);
    }

    [Fact]
    public void RestoringCollapsedDockAfterConflictDoesNotLoseEitherSurface()
    {
        using var workspace = Create();
        workspace.Open(Inspector);
        workspace.Dock(Inspector, PaneDock.Left);
        workspace.Collapse(Inspector);
        workspace.Open(Appearance);
        workspace.Dock(Appearance, PaneDock.Left);
        workspace.Open(Inspector);
        Assert.Equal(PaneDock.Left, workspace.Get(Inspector).Dock);
        Assert.Equal(PaneDock.Floating, workspace.Get(Appearance).Dock);
        Assert.Equal(PaneVisibility.Visible, workspace.Get(Appearance).Visibility);
    }

    [Fact]
    public void StableTraversalRestoresCollapsedButDoesNotResurrectHiddenPanes()
    {
        using var workspace = Create();
        Assert.Null(workspace.SwitchPane());
        workspace.Open(Inspector);
        workspace.Open(Appearance);
        workspace.Collapse(Inspector);
        Assert.Equal(Inspector, workspace.SwitchPane());
        Assert.Equal(PaneVisibility.Visible, workspace.Get(Inspector).Visibility);
        Assert.Equal(Appearance, workspace.SwitchPane(true));
        workspace.Hide(Inspector);
        Assert.Equal(Appearance, workspace.SwitchPane());
        Assert.Equal(PaneVisibility.Hidden, workspace.Get(Inspector).Visibility);
        workspace.Dispose();
        Assert.Null(workspace.ActivePaneId);
        Assert.Throws<ObjectDisposedException>(() => workspace.Open(Inspector));
        Assert.Throws<ObjectDisposedException>(() => workspace.SwitchPane());
        Assert.Throws<ObjectDisposedException>(() => workspace.Capture());
    }

    [Fact]
    public void CollapsedSavedDockRetainsIntentUntilExplicitRestorationResolvesConflict()
    {
        using var original = Create();
        original.Open(Inspector);
        original.Dock(Inspector, PaneDock.Left);
        original.Open(Appearance);
        original.Dock(Appearance, PaneDock.Left);
        original.Collapse(Appearance);
        original.Dock(Inspector, PaneDock.Left);
        using var restored = new WorkspaceState(original.Capture());
        restored.Reflow(640, 390);
        Assert.Equal(PaneDock.Left, restored.Get(Appearance).Dock);
        Assert.Equal(PaneVisibility.Collapsed, restored.Get(Appearance).Visibility);
        restored.Open(Appearance);
        Assert.Equal(PaneDock.Floating, restored.Get(Inspector).Dock);
        Assert.Equal(PaneDock.Left, restored.Get(Appearance).Dock);
    }

    [Fact]
    public void ClosingBeforeInitialArrangeCapturesLoadedLayoutInsteadOfHiddenStartupDefaults()
    {
        using var original = Create(1920, 930);
        original.Open(Inspector);
        original.Collapse(Inspector);
        var snapshot = original.Capture();
        using var notArranged = new WorkspaceState(snapshot);
        Assert.Same(snapshot, notArranged.Capture());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TraversalWithoutAnActivePaneHasPredictableForwardAndReverseEntry(bool backwards)
    {
        using var workspace = Create();
        workspace.Open(Inspector);
        workspace.Collapse(Inspector);
        workspace.Open(Appearance);
        workspace.Collapse(Appearance);
        Assert.Null(workspace.ActivePaneId);
        Assert.Equal(backwards ? Appearance : Inspector, workspace.SwitchPane(backwards));
    }

    [Fact]
    public void RestoredLargeLayoutAdaptsWithoutShrinkingTypographyOrUsingScreenCoordinates()
    {
        using var original = Create(1920, 930);
        original.Open(Appearance);
        original.SetBounds(Appearance, new(1510, 690, 380, 210));
        using var restored = new WorkspaceState(original.Capture());
        restored.Reflow(640, 390);
        AssertUsable(restored, Appearance);
        Assert.Equal(380, restored.Bounds(Appearance).Width);
        Assert.Equal(210, restored.Bounds(Appearance).Height);
        Assert.Equal(Appearance, restored.ActivePaneId);
    }

    [Fact]
    public async Task WorkspaceAndPreferencesPreserveCanonicalHistorySaveMediaAndPendingImportAuthority()
    {
        using var directory = new TemporaryDirectory();
        using var session = new ShellSession(new(directory.File("preferences.json")), new(new(), false));
        var document = session.Document;
        document.Edit("Name", edit => edit.RenameDocument("Real project"));
        ProjectPersistence.Save(document, directory.File("project.json"));
        using var import =
            ProjectMedia.BeginImport(document, "Pending sample", ownedMediaDirectory: directory.File("media"));
        using var source = new MemoryStream(WavFixtures.Create([0, 0.1f, 0]));
        await import.PrepareAsync(source, TestContext.Current.CancellationToken);
        var canonical = document.Current;
        var saved = document.Saved;
        var generation = document.Generation;
        var undo = document.UndoCount;
        var files = Directory.GetFiles(directory.File("media"), "*", SearchOption.AllDirectories);
        using var workspace = Create();
        workspace.Open(Inspector);
        workspace.Open(Appearance);
        workspace.Collapse(Inspector);
        workspace.Open(Inspector);
        workspace.Dock(Inspector, PaneDock.Left);
        workspace.Hide(Appearance);
        workspace.Open(Appearance);
        var layout = workspace.Capture();
        await session.ToggleLanguageAsync();
        await session.ToggleThemeAsync();
        Assert.Equal(layout.Panes, workspace.Capture().Panes);
        Assert.Same(canonical, document.Current);
        Assert.Same(saved, document.Saved);
        Assert.Equal(generation, document.Generation);
        Assert.Equal(undo, document.UndoCount);
        Assert.False(document.IsDirty);
        Assert.Equal(files, Directory.GetFiles(directory.File("media"), "*", SearchOption.AllDirectories));
        Assert.DoesNotContain("workspace",
            await File.ReadAllTextAsync(directory.File("project.json"), TestContext.Current.CancellationToken));
        import.Accept(TestContext.Current.CancellationToken);
        Assert.Equal(undo + 1, document.UndoCount);
    }

    private static void AssertUsable(WorkspaceState workspace, string id)
    {
        var bounds = workspace.Bounds(id);
        var definition = WorkspaceState.Definition(id);
        Assert.InRange(bounds.X, 0, workspace.Width - bounds.Width);
        Assert.InRange(bounds.Y, 0, workspace.Height - bounds.Height);
        Assert.InRange(bounds.Width, Math.Min(definition.MinimumWidth, workspace.Width), workspace.Width);
        Assert.InRange(bounds.Height, Math.Min(definition.MinimumHeight, workspace.Height), workspace.Height);
    }
}
