// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.Desktop.Workspace;

/// <summary>Screen DIP threshold shared by body clicks and graph drag/pan, independent of zoom.</summary>
internal sealed class GraphDragThreshold
{
    // Absorb small click jitter, staying below the existing 8-DIP port hit radius.
    // Equality remains a click; crossing is sticky until the next press.
    public const double Distance = 4;
    private GraphPoint _origin;
    public bool Started { get; private set; }
    public void Reset(GraphPoint origin) { _origin = origin; Started = false; }
    public bool Update(GraphPoint point)
    {
        var delta = point - _origin;
        Started |= delta.X * delta.X + delta.Y * delta.Y > Distance * Distance;
        return Started;
    }
}
