// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public enum AutomationParameter
{
    SynthCutoff,
    MixerVolume
}

public enum AutomationTransition
{
    Linear,
    Smooth
}

/// <summary>Exactly one typed, durable target. Missing targets remain unresolved, never matched by name or route.</summary>
public sealed record AutomationTarget(
    AutomationParameter Parameter,
    Id<SoundDefinition>? SoundId = null,
    Id<MixerChannel>? ChannelId = null) : CanonicalData;

/// <summary>Normalized value; the transition describes the segment leaving this point.</summary>
public sealed record AutomationPoint(
    Id<AutomationPoint> Id,
    MusicalPosition Position,
    decimal Value,
    AutomationTransition Transition = AutomationTransition.Linear) : CanonicalData;

/// <summary>Absolute Song-time envelope on a dedicated Playlist lane. Endpoints hold across the Song.</summary>
public sealed record AutomationLane(
    Id<AutomationLane> Id,
    int Lane,
    string TargetName,
    AutomationTarget Target,
    bool Enabled,
    ImmutableArray<AutomationPoint> Points) : CanonicalData;

public static class ParameterAutomation
{
    public const int MaximumLanes = 32, MaximumPoints = 1024;

    public static bool IsResolved(ProjectState state, AutomationTarget target) => target.Parameter switch
    {
        AutomationParameter.SynthCutoff => state.Sounds.Any(s => s.Id == target.SoundId && SynthVoice.Supports(s)),
        AutomationParameter.MixerVolume => state.Mixer?.Channels.Any(c => c.Id == target.ChannelId) == true,
        _ => false
    };

    public static double Physical(AutomationParameter parameter, double value) =>
        parameter == AutomationParameter.SynthCutoff
            ? 20 * Math.Pow(900, value)
            : -90 + 102 * value;

    public static decimal Normalize(AutomationParameter parameter, double value) => (decimal)Math.Clamp(
        parameter == AutomationParameter.SynthCutoff ? Math.Log(value / 20) / Math.Log(900) : (value + 90) / 102, 0, 1);

    public static double ValueAt(AutomationLane lane, long ticks)
    {
        var points = lane.Points;
        int low = 0, high = points.Length - 1;
        if (ticks <= points[0].Position.Ticks) return (double)points[0].Value;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            if (points[middle].Position.Ticks <= ticks) low = middle;
            else high = middle - 1;
        }

        if (low == points.Length - 1) return (double)points[low].Value;
        var a = points[low];
        var b = points[low + 1];
        double t = (double)(ticks - a.Position.Ticks) / (b.Position.Ticks - a.Position.Ticks);
        if (a.Transition == AutomationTransition.Smooth) t = t * t * (3 - 2 * t);
        return (double)a.Value + ((double)b.Value - (double)a.Value) * t;
    }
}
