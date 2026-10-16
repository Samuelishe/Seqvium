// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed partial class BeatPaneContent
{
    private sealed record ChannelDrag(
        BeatEditBasis Basis,
        Id<MusicalPart> Part,
        IPointer Pointer,
        Point Start,
        bool Active = false,
        bool Copy = false,
        int? Slot = null);

    private ChannelDrag? _channelDrag;
    private readonly Dictionary<Id<MusicalPart>, (Grid Row, Border Mark)> _channelRows = [];

    private void AddDuplicateAction(ContextMenu menu, MusicalPart part)
    {
        var action = new MenuItem { Header = _shell["Beat.DuplicateChannel"] };
        _menuLabels.Add((action, "Beat.DuplicateChannel"));
        AutomationProperties.SetAutomationId(action, $"Beat.DuplicateChannel.{part.Id}");
        ToolTip.SetShowOnDisabled(action, true);
        BeatEditBasis? basis = null;
        menu.Opened += (_, _) =>
        {
            basis = _basis;
            var state = _workflow.State.Snapshot.State;
            var pattern = state.Patterns.SingleOrDefault(p => p.Id == basis?.Pattern);
            var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == basis?.Attachment);
            var graph = state.Graphs.SingleOrDefault(g => g.Id == attachment?.GraphId);
            string? refusal = pattern is null || attachment is null || graph is null
                ? "Beat.Stale"
                : DesktopWorkflow.DuplicateChannelRefusal(state, pattern, attachment, graph, part.Id);
            action.IsEnabled = !_workflow.State.Busy && refusal is null;
            ToolTip.SetTip(action, _shell[refusal ?? "Beat.DuplicateChannel"]);
        };
        action.Click += async (_, _) =>
        {
            if (_disposed || !IsEffectivelyVisible || basis is not { } frozen) return;
            CancelGesture();
            PendingCommand = _workflow.DuplicateBeatChannelAsync(frozen, part.Id);
            await PendingCommand;
            Refresh();
        };
        menu.Items.Add(action);
    }

    private void ArmChannelDrag(Id<MusicalPart> part, Control handle, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed || e.ClickCount != 1 ||
            e.KeyModifiers is not (KeyModifiers.None or KeyModifiers.Control) ||
            _basis is not { } basis || _workflow.State.Busy || !PendingCommand.IsCompleted) return;
        CancelGesture();
        _channelDrag = new(basis, part, e.Pointer, e.GetPosition(_rows));
        handle.Focus();
        e.Pointer.Capture(handle);
        e.Handled = true;
    }

    private void MoveChannelDrag(PointerEventArgs e)
    {
        if (_channelDrag is not { } drag || drag.Pointer != e.Pointer) return;
        var point = e.GetPosition(_rows);
        if (!drag.Active && Math.Sqrt(Math.Pow(point.X - drag.Start.X, 2) + Math.Pow(point.Y - drag.Start.Y, 2)) <
            HostVisual.Metric(this, "Input.DragThreshold")) return;
        bool copy = drag.Active ? drag.Copy : e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var state = _workflow.State.Snapshot.State;
        var pattern = state.Patterns.SingleOrDefault(p => p.Id == drag.Basis.Pattern);
        var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == drag.Basis.Attachment);
        var graph = state.Graphs.SingleOrDefault(g => g.Id == attachment?.GraphId);
        var viewportPoint = e.GetPosition(_rackScroll);
        bool allowed = pattern is not null && _basis == drag.Basis &&
                       new Rect(_rackScroll.Bounds.Size).Contains(viewportPoint) && point.X >= 0 &&
                       point.X <= _rows.Bounds.Width &&
                       (!copy || attachment is not null && graph is not null &&
                           DesktopWorkflow.DuplicateChannelRefusal(state, pattern, attachment, graph,
                               drag.Part) is null);
        int? slot = null;
        if (allowed)
        {
            slot = _channels.Count;
            for (int i = 0; i < _channels.Count; i++)
            {
                var row = _channelRows[_channels[i].Part].Row;
                var origin = row.TranslatePoint(default, _rows) ?? default;
                if (point.Y < origin.Y + row.Bounds.Height / 2)
                {
                    slot = i;
                    break;
                }
            }
        }

        _channelDrag = drag with { Active = true, Copy = copy, Slot = slot };
        foreach (var entry in _channelRows.Values) entry.Mark.IsVisible = false;
        if (slot is { } insertion && _channels.Count > 0)
        {
            var mark = _channelRows[_channels[Math.Min(insertion, _channels.Count - 1)].Part].Mark;
            mark.VerticalAlignment = insertion == _channels.Count ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            mark.IsVisible = true;
        }

        e.Handled = true;
    }

    private async Task EndChannelDragAsync(PointerReleasedEventArgs e)
    {
        if (_channelDrag is not { } drag || drag.Pointer != e.Pointer ||
            e.InitialPressMouseButton != MouseButton.Left) return;
        MoveChannelDrag(e);
        drag = _channelDrag!;
        CancelChannelDrag();
        e.Handled = true;
        if (!drag.Active || drag.Slot is not { } slot || _basis != drag.Basis) return;
        int original = _channels.FindIndex(c => c.Part == drag.Part);
        if (drag.Copy) PendingCommand = _workflow.DuplicateBeatChannelAsync(drag.Basis, drag.Part, slot);
        else
        {
            int index = slot > original ? slot - 1 : slot;
            if (index == original) return;
            PendingCommand = _workflow.MoveBeatChannelAsync(drag.Basis, drag.Part, index);
        }

        await PendingCommand;
        Refresh();
    }

    private void CancelChannelDrag()
    {
        var pointer = _channelDrag?.Pointer;
        _channelDrag = null;
        pointer?.Capture(null);
        foreach (var entry in _channelRows.Values) entry.Mark.IsVisible = false;
    }
}
