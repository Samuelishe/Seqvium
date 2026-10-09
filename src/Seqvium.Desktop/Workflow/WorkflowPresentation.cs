// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.Desktop.Workflow;

internal static class WorkflowPresentation
{
    internal static string Shortcut(string action) => action switch
    {
        "Import" => "Ctrl+I", "Open" => "Ctrl+O", "Save" => "Ctrl+S", "SaveAs" => "Ctrl+Shift+S",
        "Undo" => "Ctrl+Z", "Redo" => "Ctrl+Y", "Panic" => "Ctrl+Esc", "Play" => "F5", "Stop" => "Shift+F5", _ => ""
    };
    internal static bool IsError(WorkflowState state) => state.StatusKey.EndsWith("Failed", StringComparison.Ordinal) ||
        state.StatusKey == "Workflow.DeviceFault";
    internal static string CompactStatus(WorkflowState state) => state.StatusKey switch
    {
        "Workflow.Empty" or "Workflow.Opened" or "Workflow.Ready" => "Status.Ready",
        "Workflow.Playing" => "Status.Playing", "Workflow.Stopped" => "Status.Stopped",
        "Workflow.Ended" => "Status.Ended", "Workflow.Preparing" => "Status.Preparing",
        "Workflow.Importing" => "Status.Importing", "Workflow.Saved" => "Status.Saved",
        "Workflow.Cancelled" => "Status.Cancelled", "Workflow.Blocked" => "Status.Blocked",
        "Workflow.Degraded" => "Status.Degraded", "Workflow.SaveFailed" => "Status.SaveFailed",
        "Workflow.OpenFailed" => "Status.OpenFailed", "Workflow.ImportFailed" => "Status.ImportFailed",
        "Workflow.DeviceFault" => "Status.DeviceFault", _ => "Status.ActionFailed"
    };
    internal static string DisabledReason(string key, WorkflowState state) => state.Busy ? "Workflow.Busy" : key switch
    {
        "Undo" => "Workflow.NoUndo", "Redo" => "Workflow.NoRedo", "Stop" or "Panic" => "Workflow.NoTransport",
        "Play" when state.Selected is null => "Workflow.NoSelection", "Play" => "Workflow.Blocked", _ => "Workflow.Busy"
    };
}
