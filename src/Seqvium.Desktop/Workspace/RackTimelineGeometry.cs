// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>The Rack's existing fixed pads and inter-step/beat/bar spaces, in musical order.</summary>
internal readonly record struct RackTimelineGeometry(
    int StepCount,
    double StepWidth,
    double StepGap,
    double BeatGap,
    double BarGap)
{
    internal static RackTimelineGeometry For(Control owner, int stepCount) => new(stepCount,
        HostVisual.Metric(owner, "Rack.StepHeight"), HostVisual.Metric(owner, "Space.Small"),
        HostVisual.Metric(owner, "Space.Large"),
        owner.TryFindResource("Rack.BarPadding", out var padding) && padding is Thickness thickness
            ? thickness.Right
            : ((Thickness)HostMetrics.Resources["Rack.BarPadding"]).Right);

    internal double StepX(int step) => step * StepWidth + (step - step / 4) * StepGap +
                                       step / 4 * BeatGap + step / 16 * BarGap;

    internal double Width => StepX(StepCount - 1) + StepWidth;

    // Interpolate between actual cell starts, including their rhythmic gaps. The last cell ends
    // at its right edge; no imaginary trailing beat/bar space belongs to the Pattern.
    internal double X(long ticks)
    {
        double position = Math.Clamp(ticks / (double)BeatSteps.StepTicks, 0, StepCount);
        if (position >= StepCount) return Width;
        int step = (int)position;
        double start = StepX(step);
        double end = step + 1 == StepCount ? Width : StepX(step + 1);
        return start + (position - step) * (end - start);
    }
}
