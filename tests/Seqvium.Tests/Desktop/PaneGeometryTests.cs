// SPDX-License-Identifier: Apache-2.0

using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class PaneGeometryTests
{
    private const string Id = WorkspaceState.InspectorId;
    private static readonly PaneBounds Initial = new(100, 100, 340, 280);

    private static WorkspaceState Create(PaneBounds? bounds = null)
    {
        var state = new WorkspaceState();
        state.Reflow(1000, 700);
        state.Open(Id);
        state.SetBounds(Id, bounds ?? Initial);
        return state;
    }

    [Fact]
    public void RightEdgeNearBoundaryNeverMovesLeftAnchorAndReversesImmediately()
    {
        using var state = Create(new(650, 100, 340, 280));
        var gesture = new PaneGesture(state, Id, PaneEdges.Right, 990, 200);
        gesture.Update(1000, 200);
        Assert.Equal(new(650, 100, 350, 280), state.Bounds(Id));
        gesture.Update(1100, 200);
        Assert.Equal(new(650, 100, 350, 280), state.Bounds(Id));
        gesture.Update(1099, 200);
        Assert.Equal(new(650, 100, 349, 280), state.Bounds(Id));
        gesture.Update(1090, 200);
        Assert.Equal(new(650, 100, 340, 280), state.Bounds(Id));
        gesture.Complete();
        // A second gesture starts from visible geometry, not an invisible requested width.
        new PaneGesture(state, Id, PaneEdges.Right, 990, 200).Update(989, 200);
        Assert.Equal(new(650, 100, 339, 280), state.Bounds(Id));
    }

    [Theory]
    [InlineData(PaneEdges.Left, -30, 0, 70, 100, 370, 280)]
    [InlineData(PaneEdges.Right, 30, 0, 100, 100, 370, 280)]
    [InlineData(PaneEdges.Top, 0, -30, 100, 70, 340, 310)]
    [InlineData(PaneEdges.Bottom, 0, 30, 100, 100, 340, 310)]
    [InlineData(PaneEdges.Left | PaneEdges.Top, -30, -30, 70, 70, 370, 310)]
    [InlineData(PaneEdges.Right | PaneEdges.Top, 30, -30, 100, 70, 370, 310)]
    [InlineData(PaneEdges.Left | PaneEdges.Bottom, -30, 30, 70, 100, 370, 310)]
    [InlineData(PaneEdges.Right | PaneEdges.Bottom, 30, 30, 100, 100, 370, 310)]
    internal void EveryEdgeAndCornerPreservesItsOppositeAnchorAndReturnsExactly(
        PaneEdges edges, double dx, double dy, double x, double y, double width, double height)
    {
        using var state = Create();
        var gesture = new PaneGesture(state, Id, edges, 17, 23);
        for (var cycle = 0; cycle < 20; cycle++)
        {
            gesture.Update(17 + dx, 23 + dy);
            Assert.Equal(new(x, y, width, height), state.Bounds(Id));
            gesture.Update(17, 23);
            Assert.Equal(Initial, state.Bounds(Id));
        }
    }

    [Theory]
    [InlineData(PaneEdges.Left | PaneEdges.Top, -1000, -1000, 0, 0, 440, 380, 1, 1, 1, 1, 439, 379)]
    [InlineData(PaneEdges.Right | PaneEdges.Top, 1000, -1000, 100, 0, 900, 380, -1, 1, 100, 1, 899, 379)]
    [InlineData(PaneEdges.Left | PaneEdges.Bottom, -1000, 1000, 0, 100, 440, 600, 1, -1, 1, 100, 439, 599)]
    [InlineData(PaneEdges.Right | PaneEdges.Bottom, 1000, 1000, 100, 100, 900, 600, -1, -1, 100, 100, 899, 599)]
    internal void CornerBoundarySaturationDiscardsOvershootOnBothAxes(
        PaneEdges edges, double dx, double dy, double x, double y, double width, double height,
        double reverseX, double reverseY, double rx, double ry, double rw, double rh)
    {
        using var state = Create();
        var gesture = new PaneGesture(state, Id, edges, 0, 0);
        gesture.Update(dx, dy);
        Assert.Equal(new(x, y, width, height), state.Bounds(Id));
        gesture.Update(dx + reverseX, dy + reverseY);
        Assert.Equal(new(rx, ry, rw, rh), state.Bounds(Id));
    }

    [Theory]
    [InlineData(PaneEdges.Left, 1000, 0, 160, 100, 280, 280, -1, 0, 159, 100, 281, 280)]
    [InlineData(PaneEdges.Right, -1000, 0, 100, 100, 280, 280, 1, 0, 100, 100, 281, 280)]
    [InlineData(PaneEdges.Top, 0, 1000, 100, 170, 340, 210, 0, -1, 100, 169, 340, 211)]
    [InlineData(PaneEdges.Bottom, 0, -1000, 100, 100, 340, 210, 0, 1, 100, 100, 340, 211)]
    internal void MinimumSizeReversalRespondsOnTheNextPixel(
        PaneEdges edges, double dx, double dy, double x, double y, double width, double height,
        double reverseX, double reverseY, double rx, double ry, double rw, double rh)
    {
        using var state = Create();
        var gesture = new PaneGesture(state, Id, edges, 0, 0);
        gesture.Update(dx, dy);
        Assert.Equal(new(x, y, width, height), state.Bounds(Id));
        gesture.Update(dx + reverseX, dy + reverseY);
        Assert.Equal(new(rx, ry, rw, rh), state.Bounds(Id));
    }

    [Theory]
    [InlineData(PaneDock.Right, PaneEdges.Left, -1000, 1, 500, 501)]
    [InlineData(PaneDock.Left, PaneEdges.Right, 1000, -1, 0, 0)]
    internal void DockInnerEdgeStoresVisibleWidthAndRetainsEntireFloatingRectangle(
        PaneDock dock, PaneEdges edge, double expand, double reverse, double limitedX, double reversedX)
    {
        using var state = Create();
        state.Dock(Id, dock);
        var gesture = new PaneGesture(state, Id, edge, 0, 0);
        gesture.Update(expand, 300);
        Assert.Equal(new(limitedX, 0, 500, 700), state.Bounds(Id));
        Assert.Equal(500, state.Get(Id).DockedWidth);
        Assert.Equal(Initial, state.Get(Id).FloatingBounds);
        gesture.Update(expand + reverse, 299);
        Assert.Equal(new(reversedX, 0, 499, 700), state.Bounds(Id));
        Assert.Equal(499, state.Get(Id).DockedWidth);
        gesture.Complete();
        state.Dock(Id, PaneDock.Floating);
        Assert.Equal(Initial, state.Bounds(Id));
    }

    [Theory]
    [InlineData(PaneDock.Left, PaneEdges.Right, 30)]
    [InlineData(PaneDock.Right, PaneEdges.Left, -30)]
    internal void DockRepeatedForwardReverseDeltasReturnExactly(PaneDock dock, PaneEdges edge, double dx)
    {
        using var state = Create();
        state.Dock(Id, dock);
        var initial = state.Bounds(Id);
        var gesture = new PaneGesture(state, Id, edge, 3, 5);
        for (var cycle = 0; cycle < 20; cycle++)
        {
            gesture.Update(3 + dx, 5);
            Assert.Equal(370, state.Bounds(Id).Width);
            gesture.Update(3, 5);
            Assert.Equal(initial, state.Bounds(Id));
        }
    }

    [Theory]
    [InlineData(PaneDock.Floating, PaneEdges.None)]
    [InlineData(PaneDock.Floating, PaneEdges.Left | PaneEdges.Top)]
    [InlineData(PaneDock.Floating, PaneEdges.Right | PaneEdges.Bottom)]
    [InlineData(PaneDock.Left, PaneEdges.Right)]
    [InlineData(PaneDock.Right, PaneEdges.Left)]
    internal void CancellationRestoresOriginalPlacementAndIgnoresLatePointerEvents(PaneDock dock, PaneEdges edges)
    {
        using var state = Create();
        state.Dock(Id, dock);
        var original = state.Get(Id);
        var gesture = new PaneGesture(state, Id, edges, 40, 50);
        gesture.Update(70, 80);
        Assert.NotEqual(original, state.Get(Id));
        // Escape, capture loss, deactivation and reflow all invoke this same cancellation operation.
        gesture.Cancel();
        gesture.Cancel();
        gesture.Update(90, 100);
        Assert.Equal(original, state.Get(Id));
    }

    [Fact]
    public void CompletionAndUnchangedPointerDoNotRollbackOrRaiseRedundantUpdates()
    {
        using var state = Create();
        var gesture = new PaneGesture(state, Id, PaneEdges.Right, 0, 0);
        var updates = 0;
        state.Changed += () => updates++;
        gesture.Update(30, 0);
        gesture.Update(30, 0);
        Assert.Equal(1, updates);
        gesture.Complete();
        gesture.Cancel();
        gesture.Update(0, 0);
        Assert.Equal(new(100, 100, 370, 280), state.Bounds(Id));
    }

    [Fact]
    public void ReflowLimitsStoredDockWidthAndNewGestureHasNoHiddenWidth()
    {
        using var state = Create();
        state.Dock(Id, PaneDock.Right);
        state.SetBounds(Id, new(0, 0, 10000, 700));
        state.Reflow(640, 390);
        Assert.Equal(320, state.Get(Id).DockedWidth);
        Assert.Equal(new(320, 0, 320, 390), state.Bounds(Id));
        new PaneGesture(state, Id, PaneEdges.Left, 320, 100).Update(321, 100);
        Assert.Equal(new(321, 0, 319, 390), state.Bounds(Id));
        Assert.Equal(319, state.Capture().Panes.Single(p => p.InstanceId == Id).DockedWidth);
    }

    [Theory]
    [InlineData(2, 140, PaneEdges.Left)]
    [InlineData(338, 140, PaneEdges.Right)]
    [InlineData(170, 2, PaneEdges.Top)]
    [InlineData(170, 278, PaneEdges.Bottom)]
    [InlineData(8, 8, PaneEdges.Left | PaneEdges.Top)]
    [InlineData(332, 8, PaneEdges.Right | PaneEdges.Top)]
    [InlineData(8, 272, PaneEdges.Left | PaneEdges.Bottom)]
    [InlineData(332, 272, PaneEdges.Right | PaneEdges.Bottom)]
    [InlineData(20, 20, PaneEdges.None)]
    [InlineData(170, 140, PaneEdges.None)]
    internal void HitRegionsGiveCornersPriorityAndLeaveHeaderAndContentAlone(double x, double y, PaneEdges expected)
        => Assert.Equal(expected, PaneGeometry.HitTest(x, y, 340, 280, PaneDock.Floating));

    [Theory]
    [InlineData(PaneDock.Right, 2, 0, PaneEdges.Left)]
    [InlineData(PaneDock.Right, 170, 2, PaneEdges.None)]
    [InlineData(PaneDock.Left, 338, 278, PaneEdges.Right)]
    [InlineData(PaneDock.Left, 170, 278, PaneEdges.None)]
    internal void DockHasOnlyItsHorizontalInnerDivider(PaneDock dock, double x, double y, PaneEdges expected)
        => Assert.Equal(expected, PaneGeometry.HitTest(x, y, 340, 280, dock));

    [Theory]
    [InlineData(HostLanguage.English, "Close pane: Project Inspector", "close")]
    [InlineData(HostLanguage.Russian, "Закрыть панель: Инспектор проекта", "закрыть")]
    public void CloseLabelAndKeyboardHintAreLocalizedAndDistinctFromCollapse(
        HostLanguage language, string expected, string hint)
    {
        var localizer = new HostLocalizer();
        Assert.Equal(expected,
            localizer.Get("Pane.Close", language) + ": " + localizer.Get("Pane.Inspector", language));
        Assert.NotEqual(localizer.Get("Pane.Close", language), localizer.Get("Pane.Collapse", language));
        Assert.Contains("Ctrl+W: " + hint, localizer.Get("Workspace.KeyboardHint", language));
    }
}
