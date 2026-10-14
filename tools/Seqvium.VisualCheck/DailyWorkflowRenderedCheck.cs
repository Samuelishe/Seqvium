// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task DailyWorkflowRenderedAsync(MainWindow main, string output, string project)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        Button Button(string id) => main.GetVisualDescendants().OfType<Button>().Single(c =>
            AutomationProperties.GetAutomationId(c) == id);
        async Task Click(string id)
        {
            Button(id).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            await Task.Delay(100);
            await Field<Task>(main, "_workflowWork");
        }
        async Task Wait(Func<bool> condition, string failure)
        {
            for (int i = 0; i < 250 && !condition(); i++) await Task.Delay(20);
            Require(condition(), failure);
        }

        await Task.Delay(350);
        main.WindowState = WindowState.Normal;
        main.Width = 1920;
        main.Height = 1040;
        main.UpdateLayout();
        Require(workspace.State.Panes.Count(p => p.Visibility == PaneVisibility.Visible) == 1 &&
            workspace.State.ActivePaneId == WorkspaceState.BeatId, "Pristine startup shows unsolicited panes.");
        var matrix = new List<object>();
        for (int theme = 0; theme < 2; theme++)
        {
            for (int language = 0; language < 2; language++)
            {
                await CaptureAsync(main, output, "empty-" + shell.Preferences.Language + "-" + shell.Preferences.Theme);
                matrix.Add(new { shell.Preferences, Bounds = workspace.State.Bounds(WorkspaceState.BeatId) });
                await Click("Preference.Language");
            }
            await Click("Preference.Theme");
        }
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Project fixture Open failed.");
        Invoke(main, "ApplyOpenedWorkspace");
        await Task.Delay(150);
        var original = workflow.State.Snapshot;
        if (workflow.State.SongMode) await workflow.SetSongModeAsync(false);
        var choices = workflow.State.Occurrences;
        var navigator = main.GetVisualDescendants().OfType<PatternNavigator>().Single();
        var editor = new PatternEditorNavigation();
        Id<GraphAttachment>? melodic = null;
        var switches = new List<object>();
        await Click("Navigator.Pattern." + choices[0].Id);
        await navigator.PendingCommand;
        foreach (var choice in choices.Skip(1))
        {
            await Click("Navigator.Pattern." + choice.Id);
            await navigator.PendingCommand;
            await Wait(() => workflow.State.Selected == choice.Id && !workflow.State.Busy,
                "Pattern selection failed: " + choice.Label);
            Require(workflow.State.Execution is null, "Stopped selection opened audio.");
            if (editor.Resolve(workflow.State).Pane == WorkspaceState.PianoId) melodic ??= choice.Id;
            switches.Add(new { choice.Label, Selected = workflow.State.Selected });
        }
        Require(workflow.State.Snapshot == original && workflow.State.UndoCount == 0, "Selection edited music.");
        Require(melodic is not null, "Fixture lacks a melodic Pattern.");
        await Click("Workflow.Stop");
        await Click("Navigator.Pattern." + melodic);
        await navigator.PendingCommand;
        Button("Navigator.Pattern." + melodic).RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent,
            new PointerEventArgs(InputElement.PointerReleasedEvent, Button("Navigator.Pattern." + melodic),
                new Pointer(1, PointerType.Mouse, true), main, new(0, 0), 0,
                new PointerPointProperties(), KeyModifiers.None)));
        await navigator.PendingCommand;
        await Task.Delay(100);
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        Require(workspace.State.ActivePaneId == WorkspaceState.PianoId &&
            piano.Canvas.Part?.Id == editor.Resolve(workflow.State).Part && !piano.Canvas.Part!.Notes.IsEmpty,
            "Double-click did not reveal the melodic channel.");
        await CaptureAsync(main, output, "direct-melodic-editor");
        for (int theme = 0; theme < 2; theme++)
        {
            for (int language = 0; language < 2; language++)
            {
                await CaptureAsync(main, output, "music-" + shell.Preferences.Language + "-" + shell.Preferences.Theme);
                await Click("Preference.Language");
            }
            await Click("Preference.Theme");
        }
        var card = Button("Navigator.Pattern." + melodic);
        card.RaiseEvent(new ContextRequestedEventArgs());
        await Task.Delay(100);
        Require(card.ContextMenu?.IsOpen == true, "Pattern context menu did not open.");
        await CapturePopupAsync(card.ContextMenu!, output, "pattern-editors-menu");
        var rack = card.ContextMenu!.Items.Cast<MenuItem>().ElementAt(1);
        rack.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        await navigator.PendingCommand;
        card.ContextMenu.Close();
        await Click("Navigator.Pattern." + choices[0].Id);
        await navigator.PendingCommand;
        await Click("Navigator.Pattern." + melodic);
        await navigator.PendingCommand;
        card.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        await navigator.PendingCommand;
        Require(workspace.State.ActivePaneId == WorkspaceState.BeatId, "Explicit Rack intent was not remembered.");
        await Click(WorkspaceState.PianoId + ".open");
        await Click("Navigator.Pattern." + choices[0].Id);
        await navigator.PendingCommand;
        await Click("Navigator.Pattern." + melodic);
        await navigator.PendingCommand;
        card.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        await navigator.PendingCommand;
        Require(workspace.State.ActivePaneId == WorkspaceState.PianoId, "Rail editor intent was not remembered.");
        await Click("Navigator.Create");
        await navigator.PendingCommand;
        var rackPane = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        var name = Field<TextBox>(rackPane, "_name");
        name.Text = "A long musical phrase name with an expressive second line";
        name.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        await rackPane.PendingCommand;
        await Task.Delay(100);
        var longCard = Button("Navigator.Pattern." + workflow.State.Selected);
        longCard.BringIntoView();
        await CaptureAsync(main, output, "long-pattern-name");
        Require(workflow.State.IsDirty, "Pattern creation did not dirty fixture.");
        var lifecycle = workflow.State.Document.LifecycleId;
        Button("Workflow.New").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        await Task.Delay(150);
        Require(main.OwnedWindows.OfType<UnsavedProjectDialog>().Any(), "New did not request unsaved decision.");
        var dialog = main.OwnedWindows.OfType<UnsavedProjectDialog>().Single();
        dialog.GetVisualDescendants().OfType<Button>().Single(b =>
            AutomationProperties.GetAutomationId(b) == "Unsaved.Cancel").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        await Field<Task>(main, "_workflowWork");
        Require(workflow.State.Document.LifecycleId == lifecycle && workflow.State.IsDirty, "New Cancel lost work.");
        Button("Workflow.New").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        await Task.Delay(150);
        dialog = main.OwnedWindows.OfType<UnsavedProjectDialog>().Single();
        dialog.GetVisualDescendants().OfType<Button>().Single(b =>
            AutomationProperties.GetAutomationId(b) == "Unsaved.Save").RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        await Field<Task>(main, "_workflowWork");
        Require(workflow.State.Document.LifecycleId != lifecycle && !workflow.State.IsDirty &&
            workflow.State.SavedPath is null && workflow.State.Execution is null, "New Save did not replace silently.");
        Require(workspace.State.Panes.Count(p => p.Visibility == PaneVisibility.Visible) == 1 &&
            workspace.State.ActivePaneId == WorkspaceState.BeatId, "New retained empty Piano or other panes.");
        await CaptureAsync(main, output, "new-after-save");
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Reopen fixture failed.");
        Invoke(main, "ApplyOpenedWorkspace");
        Require(workspace.State.Get(WorkspaceState.PianoId).Visibility == PaneVisibility.Visible,
            "New erased the remembered musical layout.");
        await CaptureAsync(main, output, "restored-musical-layout");
        Json(output, "daily-workflow", new { RoutedInput = true, NativeInput = false,
            PhysicalOutput = false, SubjectiveListening = false, NewCancelAndSave = true,
            DirectMelodyEditorAndRememberedRack = true, MusicalLayoutRestored = true,
            NavigatorWidth = navigator.Bounds.Width, Switches = switches, Matrix = matrix });
    }
}
