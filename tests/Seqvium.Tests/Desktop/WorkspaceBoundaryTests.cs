// SPDX-License-Identifier: Apache-2.0

using Avalonia.Controls;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class WorkspaceBoundaryTests
{
    private static PaneBoundary Floating(string id, double x, double y = 100) =>
        new(id, new(x, y, 340, 280), PaneDock.Floating, true);

    private static ResizeBoundary? Resolve(PaneBoundary[] panes, double x, double y, bool control = false) =>
        WorkspaceBoundaryResolver.Resolve(panes, x, y, 1000, 700, control);

    [Theory]
    [InlineData(94, 200, PaneEdges.Left)]
    [InlineData(106, 200, PaneEdges.Left)]
    [InlineData(446, 200, PaneEdges.Right)]
    [InlineData(434, 200, PaneEdges.Right)]
    [InlineData(250, 94, PaneEdges.Top)]
    [InlineData(250, 106, PaneEdges.Top)]
    [InlineData(250, 386, PaneEdges.Bottom)]
    [InlineData(250, 374, PaneEdges.Bottom)]
    [InlineData(94, 94, PaneEdges.Left | PaneEdges.Top)]
    [InlineData(111, 111, PaneEdges.Left | PaneEdges.Top)]
    [InlineData(429, 111, PaneEdges.Right | PaneEdges.Top)]
    [InlineData(111, 369, PaneEdges.Left | PaneEdges.Bottom)]
    [InlineData(429, 369, PaneEdges.Right | PaneEdges.Bottom)]
    internal void UsableToleranceIncludesBothSidesAndCornersOverrideEdges(double x, double y, PaneEdges edges) =>
        Assert.Equal(new ResizeBoundary("a", edges), Resolve([Floating("a", 100)], x, y));

    [Theory]
    [InlineData(93, 200)]
    [InlineData(107, 200)]
    [InlineData(447, 200)]
    [InlineData(250, 107)]
    [InlineData(250, 387)]
    [InlineData(113, 113)]
    [InlineData(double.NaN, 100)]
    public void ToleranceIsBoundedAndLeavesTheWorkingSurfaceAvailable(double x, double y) =>
        Assert.Null(Resolve([Floating("a", 100)], x, y));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GapUsesNearestBoundaryThenFrontOrderOnExactTie(bool reverse)
    {
        PaneBoundary[] panes = [Floating("a", 100), Floating("b", 448)];
        if (reverse) Array.Reverse(panes);
        Assert.Equal("a", Resolve(panes, 442, 200)!.Value.InstanceId);
        Assert.Equal("b", Resolve(panes, 446, 200)!.Value.InstanceId);
        Assert.Equal(reverse ? "a" : "b", Resolve(panes, 444, 200)!.Value.InstanceId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TouchingPanesOwnTheirVisibleSideAndTheFrontPaneOwnsTheExactSeam(bool reverse)
    {
        PaneBoundary[] panes = [Floating("a", 100), Floating("b", 440)];
        if (reverse) Array.Reverse(panes);
        Assert.Equal(new ResizeBoundary("a", PaneEdges.Right), Resolve(panes, 438, 200));
        Assert.Equal(new ResizeBoundary("b", PaneEdges.Left), Resolve(panes, 442, 200));
        Assert.Equal(reverse ? "a" : "b", Resolve(panes, 440, 200)!.Value.InstanceId);
    }

    [Fact]
    public void CoveredEdgesCannotInterceptFrontContentOrProjectIntoAnEmptyGap()
    {
        PaneBoundary[] panes = [Floating("rear", 100), new("front", new(400, 90, 100, 292), PaneDock.Right, true)];
        Assert.Null(Resolve(panes, 442, 200));
        // Pointer is below the front pane; the rear's corner itself is still obscured.
        Assert.Null(Resolve(panes, 442, 385));
        panes[1] = panes[1] with { Visible = false };
        Assert.Equal(new ResizeBoundary("rear", PaneEdges.Right), Resolve(panes, 442, 200));
        Assert.Equal(new ResizeBoundary("rear", PaneEdges.Right | PaneEdges.Bottom), Resolve(panes, 442, 385));
    }

    [Fact]
    public void InvisiblePanesAndOutOfWorkspaceToleranceNeverParticipate()
    {
        PaneBoundary[] panes = [Floating("a", 0), Floating("hidden", 0) with { Visible = false }];
        Assert.Equal(new ResizeBoundary("a", PaneEdges.Left), Resolve(panes, 0, 200));
        Assert.Equal(new ResizeBoundary("a", PaneEdges.Left), Resolve(panes, 6, 200));
        Assert.Null(Resolve(panes, -1, 200));
        Assert.Null(Resolve(panes, 1001, 200));
    }

    [Fact]
    public void ControlsExcludeEvenCornerAndCompetingRegions()
    {
        PaneBoundary[] panes = [Floating("a", 100), Floating("b", 448)];
        Assert.Null(Resolve(panes, 111, 111, true));
        Assert.Null(Resolve(panes, 444, 200, true));
        var textBox = new TextBox();
        var editable = new Border { Focusable = true, Child = new TextBlock() };
        Assert.True(WorkspacePane.IsActionSource(textBox));
        Assert.True(WorkspacePane.IsActionSource(editable.Child!));
        Assert.True(WorkspacePane.IsActionSource(new Slider()));
        Assert.True(WorkspacePane.IsActionSource(new ComboBox()));
        Assert.False(WorkspacePane.IsActionSource(new Border { Child = new TextBlock() }));
    }

    [Theory]
    [InlineData(WorkspaceState.InspectorId)]
    [InlineData(WorkspaceState.AppearanceId)]
    public void InactivePaneResizesDirectlyAndCaptureIdentitySurvivesActivationAndReordering(string id)
    {
        using var state = new WorkspaceState();
        state.Reflow(1000, 700);
        var other = id == WorkspaceState.InspectorId ? WorkspaceState.AppearanceId : WorkspaceState.InspectorId;
        state.Open(id);
        state.SetBounds(id, new(100, 100, 340, 280));
        state.Open(other);
        state.SetBounds(other, new(448, 100, 340, 280));

        PaneBoundary[] Surfaces() => state.Panes.Select(pane =>
            new PaneBoundary(pane.InstanceId, state.Bounds(pane.InstanceId), pane.Dock,
                pane.Visibility == PaneVisibility.Visible)).ToArray();

        var target = Resolve(Surfaces(), 442, 200)!.Value;
        Assert.Equal(id, target.InstanceId);
        Assert.Equal(other, state.ActivePaneId); // Hover has no activation side effect.
        var unchanged = state.Get(other);
        var gesture = new PaneGesture(state, target.InstanceId, target.Edges, 442, 200);
        state.Activate(target.InstanceId);
        gesture.Update(442, 200);
        Assert.Equal(new(100, 100, 340, 280), state.Bounds(id)); // No initial jump from outward tolerance.
        state.Activate(other);
        gesture.Update(462, 200);
        Assert.Equal(new(100, 100, 360, 280), state.Bounds(id));
        Assert.Equal(unchanged, state.Get(other));
        gesture.Cancel();
        Assert.Equal(new(100, 100, 340, 280), state.Bounds(id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MeetingDocksHaveIndependentSidesAndFrontOrderSeamOwnership(bool leftFront)
    {
        using var state = new WorkspaceState();
        state.Reflow(640, 390);
        state.Open(WorkspaceState.InspectorId);
        state.Dock(WorkspaceState.InspectorId, PaneDock.Left);
        state.Open(WorkspaceState.AppearanceId);
        state.Dock(WorkspaceState.AppearanceId, PaneDock.Right);
        if (leftFront) state.Activate(WorkspaceState.InspectorId);
        var panes = state.Panes.Select(pane => new PaneBoundary(pane.InstanceId, state.Bounds(pane.InstanceId),
            pane.Dock, true)).ToArray();
        Assert.Equal(WorkspaceState.InspectorId, Resolve(panes, 318, 200)!.Value.InstanceId);
        Assert.Equal(WorkspaceState.AppearanceId, Resolve(panes, 322, 200)!.Value.InstanceId);
        var target = Resolve(panes, 320, 200)!.Value;
        Assert.Equal(leftFront ? WorkspaceState.InspectorId : WorkspaceState.AppearanceId, target.InstanceId);
        var other = leftFront ? WorkspaceState.AppearanceId : WorkspaceState.InspectorId;
        var unchanged = state.Get(other);
        var gesture = new PaneGesture(state, target.InstanceId, target.Edges, 320, 200);
        gesture.Update(leftFront ? 300 : 340, 200);
        Assert.Equal(300, state.Bounds(target.InstanceId).Width);
        Assert.Equal(unchanged, state.Get(other)); // This is not coupled/shared-divider resize.
    }

    [Fact]
    public void DockGapUsesTheSameDistanceRuleAndHasNoVerticalOrCornerResize()
    {
        PaneBoundary[] panes =
        [
            new("left", new(0, 0, 316, 390), PaneDock.Left, true),
            new("right", new(324, 0, 316, 390), PaneDock.Right, true)
        ];
        Assert.Equal(new ResizeBoundary("left", PaneEdges.Right), Resolve(panes, 318, 0));
        Assert.Equal(new ResizeBoundary("right", PaneEdges.Left), Resolve(panes, 322, 390));
        Assert.Equal("right", Resolve(panes, 320, 200)!.Value.InstanceId);
        Assert.Null(Resolve(panes, 100, 0));
    }
}
