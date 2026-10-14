// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task DailyWorkflowAsync(MainWindow main, string output, string project)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        var input = new NativePointer.DrumInput(main);
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c =>
            AutomationProperties.GetAutomationId(c) == id);
        async Task Click(string id, bool modal = false)
        {
            var control = Id(id);
            var scroll = control.GetVisualAncestors().OfType<ScrollViewer>().LastOrDefault();
            if (scroll is not null)
                for (int n = 0; n < 30; n++)
                {
                    var point = control.TranslatePoint(new(control.Bounds.Width / 2, control.Bounds.Height / 2), scroll)!.Value;
                    if (point.Y >= 0 && point.Y < scroll.Viewport.Height - 8) break;
                    await input.Wheel(scroll, new(scroll.Bounds.Width / 2, 80), point.Y < 0 ? 240 : -240);
                }
            await input.Click(control, allowOwnedDialog: modal);
        }
        async Task Wait(Func<bool> condition, string failure)
        {
            for (int i = 0; i < 250 && !condition(); i++) await Task.Delay(20);
            Require(condition(), failure);
        }

        await Task.Delay(350);
        main.Activate();
        await Task.Delay(150);
        await input.ActivateVisibleWindow();
        Require(workspace.State.Panes.Count(p => p.Visibility == PaneVisibility.Visible) == 1 &&
                workspace.State.ActivePaneId == WorkspaceState.BeatId, "Empty startup has unsolicited panes.");
        Require(workflow.State.Execution is null && !workflow.State.IsDirty, "Startup is not pristine and silent.");
        var captures = new List<object>();
        for (int theme = 0; theme < 2; theme++)
        {
            for (int language = 0; language < 2; language++)
            {
                await CaptureAsync(main, output, "empty-" + shell.Preferences.Language + "-" + shell.Preferences.Theme);
                captures.Add(new { Empty = true, shell.Preferences, Bounds = workspace.State.Bounds(WorkspaceState.BeatId) });
                await Click("Preference.Language");
            }
            await Click("Preference.Theme");
        }
        await Click("Workflow.Open", true);
        await input.FileDialog(project);
        await Wait(() => workflow.State.SavedPath == project && !workflow.State.Busy, "Native project Open failed.");
        var original = workflow.State.Snapshot;
        var navigator = main.GetVisualDescendants().OfType<PatternNavigator>().Single();
        var navigation = new PatternEditorNavigation();
        var choices = workflow.State.Occurrences;
        Require(choices.Length >= 3, "Supply a musical project with at least three Patterns.");
        if (workflow.State.SongMode) await Click("Workflow.Mode");
        var switches = new List<object>();
        await Click("Navigator.Pattern." + choices[0].Id);
        await Click("Workflow.Play");
        await Wait(() => workflow.State.Execution?.Execution.Playing == true && !workflow.State.Busy, "Pattern Play failed.");
        string? melodic = null;
        for (int i = 1; i < choices.Length; i++)
        {
            var choice = choices[i];
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await Click("Navigator.Pattern." + choice.Id);
            await Wait(() => !workflow.State.Busy && workflow.State.Selected == choice.Id &&
                workflow.State.Execution?.Execution.AttachmentId == choice.Id &&
                workflow.State.Execution.Execution.Playing, "Selection stopped Pattern playback: " + choice.Label);
            Require(workspace.State.Get(WorkspaceState.PianoId).Visibility == PaneVisibility.Hidden,
                "Single click opened Piano Roll.");
            if (navigation.Resolve(workflow.State).Pane == WorkspaceState.PianoId) melodic ??= choice.Id.ToString();
            switches.Add(new { choice.Label, MillisecondsIncludingNativeInput = watch.ElapsedMilliseconds,
                workflow.State.Execution!.Execution.Position });
        }
        Require(workflow.State.Snapshot == original && workflow.State.UndoCount == 0, "Navigation edited music.");
        Require(melodic is not null, "Supply a project containing a melodic Pattern.");
        await Click("Workflow.Stop");
        await Click("Navigator.Pattern." + melodic);
        await Click("Navigator.Pattern." + melodic);
        await Wait(() => workspace.State.Get(WorkspaceState.PianoId).Visibility == PaneVisibility.Visible,
            "Native double-click did not open the melodic Piano Roll.");
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        var expected = navigation.Resolve(workflow.State).Part;
        Require(piano.Canvas.Part?.Id == expected && !piano.Canvas.Part!.Notes.IsEmpty,
            "Direct editor did not select the melodic channel.");
        await CaptureAsync(main, output, "melodic-direct-editor");
        var card = (Button)Id("Navigator.Pattern." + melodic);
        await input.RightClick(card, new(card.Bounds.Width / 2, card.Bounds.Height / 2));
        await Task.Delay(150);
        Require(card.ContextMenu?.IsOpen == true, "Native Pattern context menu failed.");
        await CapturePopupAsync(card.ContextMenu!, output, "pattern-editors-menu");
        var rackItem = card.ContextMenu!.Items.Cast<MenuItem>().ElementAt(1);
        await input.Click(rackItem);
        Require(workspace.State.ActivePaneId == WorkspaceState.BeatId, "Explicit Channel Rack command failed.");
        await Click("Navigator.Pattern." + choices[0].Id);
        await Click("Navigator.Pattern." + melodic);
        input.Key(0x0D);
        await Wait(() => workspace.State.ActivePaneId == WorkspaceState.BeatId && !workflow.State.Busy,
            "Remembered editor was lost on Pattern return.");

        await Click("Navigator.Create");
        await Wait(() => workflow.State.IsDirty && !workflow.State.Busy, "Native creation did not dirty the project.");
        var lifecycle = workflow.State.Document.LifecycleId;
        input.Key(0x4E, modifier: 0x11);
        await Task.Delay(250);
        input.Key(0x1B, dialog: true);
        await Task.Delay(180);
        Require(workflow.State.Document.LifecycleId == lifecycle && workflow.State.IsDirty, "New cancellation lost work.");
        await Click("Workflow.New", true);
        await Task.Delay(250);
        // Cancel owns initial focus; Shift+Tab twice reaches Save in this owned modal.
        input.Key(0x09, dialog: true, modifier: 0x10);
        input.Key(0x09, dialog: true, modifier: 0x10);
        input.Key(0x0D, dialog: true);
        await Wait(() => workflow.State.Document.LifecycleId != lifecycle && !workflow.State.Busy, "Native New/Save failed.");
        Require(!workflow.State.IsDirty && workflow.State.SavedPath is null && workflow.State.Occurrences.Length == 0 &&
            workflow.State.Execution is null, "New retained music, history, path or execution.");
        Require(workspace.State.Panes.Count(p => p.Visibility == PaneVisibility.Visible) == 1 &&
            workspace.State.ActivePaneId == WorkspaceState.BeatId, "New retained empty editors.");
        await CaptureAsync(main, output, "new-after-save");
        await Click("Workflow.Open", true);
        await input.FileDialog(project);
        await Wait(() => workflow.State.SavedPath == project && !workflow.State.Busy, "Reopen after New failed.");
        Require(workspace.State.Get(WorkspaceState.PianoId).Visibility == PaneVisibility.Visible,
            "New erased the previous musical layout.");
        await CaptureAsync(main, output, "reopened-musical-layout");
        Json(output, "daily-workflow", new { NativeInput = true, InternalMusicalMutation = false,
            StartupAndNewSilent = true, NewCancelAndSave = true, DirectPianoAndRememberedRack = true,
            MusicalLayoutRestored = true, NavigatorWidth = navigator.Bounds.Width, Switches = switches,
            Matrix = captures, SubjectiveListening = false });
    }
}
