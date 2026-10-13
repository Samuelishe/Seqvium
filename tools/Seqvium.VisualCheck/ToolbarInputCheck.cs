// SPDX-License-Identifier: Apache-2.0
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Seqvium.Desktop;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task ToolbarGainInputAsync(MainWindow main, WorkflowControls controls, DesktopWorkflow workflow, string output)
    {
        var field = Field<NumericUpDown>(controls, "_gain");
        var text = field.GetVisualDescendants().OfType<TextBox>().Single();
        var original = workflow.State.Snapshot; int history = workflow.State.UndoCount;
        void Key(Key key) => text.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
        Require(text.Focus(), "Toolbar Gain did not receive focus.");
        foreach (string draft in new[] { "", "0", "0.", "0.1", "0.13" })
        {
            text.Text = draft; await workflow.ObserveAsync(); controls.Refresh();
            Require(text.Text == draft && workflow.State.Snapshot == original && workflow.State.UndoCount == history,
                "Renderer refresh published or erased a fractional Gain draft.");
        }
        Key(Avalonia.Input.Key.Enter); await controls.PendingGainCommand;
        Require(workflow.State.Gain == .13m && workflow.State.UndoCount == history + 1, "Fractional Gain was not one committed edit.");
        text.Text = "0.02"; Key(Avalonia.Input.Key.Escape);
        Require(workflow.State.Gain == .13m && text.Text == "0.13", "Escape did not restore Gain.");
        text.Text = "invalid"; Key(Avalonia.Input.Key.Enter); await controls.PendingGainCommand;
        Require(workflow.State.Gain == .13m && field.Classes.Contains("invalid"), "Invalid Gain changed music.");
        Key(Avalonia.Input.Key.Escape); text.Text = "0.22";
        Field<Button>(controls, "_information").Focus(); await controls.PendingGainCommand;
        Require(workflow.State.Gain == .22m && workflow.State.UndoCount == history + 2, "Focus-loss Gain commit failed.");
        Require(await workflow.UndoAsync(), "Focus-loss Gain Undo failed.");
        Require(await workflow.UndoAsync(), "Fractional Gain Undo failed.");
        controls.Refresh(); main.UpdateLayout();
        Require(text.Focus(), "Gain draft refocus failed."); text.Text = "0.05";
        Require(await workflow.SetGainAsync(.31m), "External Gain fixture edit failed."); controls.Refresh();
        Key(Avalonia.Input.Key.Enter); await controls.PendingGainCommand;
        Require(workflow.State.Gain == .31m, "Expired Gain draft overwrote an intervening edit.");
        Field<Button>(controls, "_information").Focus();
        Require(await workflow.UndoAsync(), "External Gain fixture Undo failed.");
        Require(workflow.State.Snapshot == original, "Toolbar fixture did not restore music.");
        Json(output, "toolbar-gain-input", new { Passed = true, RoutedInput = true, NativeInput = false,
            FractionalDraftSurvivesObservation = true, SingleUndoCommit = true, FocusLossCommit = true,
            EscapeAndInvalidPreserveMusic = true, ExpiredDraftCannotOverwrite = true });
    }
}
