// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Core;

/// <summary>Bounded frame breakpoints and a worker-built physical-domain lookup. No callback preparation.</summary>
internal sealed class PreparedAutomation
{
    private readonly (long Frame, double Value, AutomationTransition Transition)[] _points;
    private const int Resolution = 4096;
    private readonly double[] _physical = new double[Resolution + 1];
    internal int StorageBytes => _physical.Length * sizeof(double) + _points.Length * 24;

    internal PreparedAutomation(AutomationLane lane, Tempo tempo, int rate)
    {
        // Several canonical ticks may round to one frame: the latest musical point owns that frame.
        _points = lane.Points.GroupBy(p => ConstantTempoConversion.ToFrames(p.Position, tempo, rate))
            .Select(g => (g.Key, (double)g.Last().Value, g.Last().Transition)).ToArray();
        for (int i = 0; i < _physical.Length; i++)
        {
            double value = ParameterAutomation.Physical(lane.Target.Parameter, (double)i / Resolution);
            _physical[i] = lane.Target.Parameter == AutomationParameter.SynthCutoff
                ? Math.Tan(Math.PI * value / rate)
                : i == 0
                    ? 0
                    : Math.Pow(10, value / 20);
        }
    }

    internal double At(long frame)
    {
        int low = 0, high = _points.Length - 1;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            if (_points[middle].Frame <= frame) low = middle;
            else high = middle - 1;
        }

        var a = _points[low];
        double value = a.Value;
        if (frame > a.Frame && low + 1 < _points.Length)
        {
            var b = _points[low + 1];
            double t = (double)(frame - a.Frame) / (b.Frame - a.Frame);
            if (a.Transition == AutomationTransition.Smooth) t = t * t * (3 - 2 * t);
            value += (b.Value - a.Value) * t;
        }

        double index = Math.Clamp(value * Resolution, 0, Resolution);
        int first = Math.Min(Resolution - 1, (int)index);
        return _physical[first] + (_physical[first + 1] - _physical[first]) * (index - first);
    }
}
