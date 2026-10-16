// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public sealed partial class ProjectEdit
{
    public Id<AutomationLane> AddAutomationLane(int lane, string targetName, AutomationTarget target,
        ImmutableArray<AutomationPoint> points)
    {
        CheckActive();
        var id = Id<AutomationLane>.New();
        State = State with
        {
            AutomationLanes = State.AutomationLanes.Add(new(id, lane, targetName, target, true, points))
        };
        return id;
    }

    public void UpdateAutomationLane(AutomationLane lane)
    {
        CheckActive();
        var old = State.AutomationLanes.Single(l => l.Id == lane.Id);
        if (old.Target != lane.Target)
            throw new InvalidOperationException("Automation targets cannot be implicitly replaced.");
        State = State with { AutomationLanes = Replace(State.AutomationLanes, l => l.Id == lane.Id, _ => lane) };
    }

    public void DeleteAutomationLane(Id<AutomationLane> id)
    {
        CheckActive();
        State = State with { AutomationLanes = Remove(State.AutomationLanes, l => l.Id == id) };
    }
}
