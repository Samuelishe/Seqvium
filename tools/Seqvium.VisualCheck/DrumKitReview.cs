// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task DrumKitReviewAsync(MainWindow main, string output, string project)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        main.WindowState = WindowState.Maximized;
        await Task.Delay(350);
        main.Activate();
        await Task.Delay(100);
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Review project unavailable.");
        var state = workflow.State.Snapshot.State;
        var first = state.Patterns.First(p => p.Parts.Length == 7);
        var placement = state.Placements.Single(p => p.PatternId == first.Id);
        var attachment = state.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId);
        var input = new NativePointer.DrumInput(main);

        Control Id(string key) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == key);

        var aligned = new List<object>();
        foreach (var part in first.Parts)
        {
            var sound = state.Sounds.Single(s => s.Id == part.SoundId);
            typeof(MainWindow).GetMethod("OpenNodeEditor", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(main,
            [
                new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId,
                    attachment.Sources.Single(s => s.PartId == part.Id).NodeId)
            ]);
            var editor = main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single();
            var drum = main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single();
            foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(40, 20, 760, 780));
                workspace.Open(WorkspaceState.NodeEditorId);
                editor.Refresh();
                main.UpdateLayout();
                await drum.PendingWaveform;
                foreach (var field in drum.GetVisualDescendants().OfType<TextBox>().Where(c => c.IsEffectivelyVisible))
                {
                    var unit = ((Grid)field.Parent!).Children.OfType<TextBlock>().Single();
                    aligned.Add(FieldAlignment.Verify(field, unit, main, part.Name + "/" + language + "/" + theme));
                }

                await CaptureAsync(main, output,
                    $"review-{DrumInstruments.Find(sound.Algorithm)!.Key}-{language}-{theme}");
                workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 300, 210));
                main.UpdateLayout();
                var scroll = (ScrollViewer)Id("Drum.Scroll");
                foreach (var field in drum.GetVisualDescendants().OfType<TextBox>().Where(c => c.IsEffectivelyVisible)
                             .ToArray())
                {
                    field.BringIntoView();
                    await Task.Delay(30);
                    main.UpdateLayout();
                    var at = field.TranslatePoint(default, scroll)!.Value;
                    Require(
                        at.X >= 0 && at.X + field.Bounds.Width <= scroll.Bounds.Width && at.Y >= -1 &&
                        at.Y + field.Bounds.Height <= scroll.Bounds.Height + 1,
                        "Compact drum parameter unreachable: " + AutomationProperties.GetAutomationId(field));
                }

                await CaptureAsync(main, output,
                    $"review-minimum-{DrumInstruments.Find(sound.Algorithm)!.Key}-{language}-{theme}");
                scroll.Offset = default;
            }
        }

        // Portable custom preset through actual native pickers, applied as one sound-history edit.
        if (shell.Preferences.Language != HostLanguage.English) await shell.ToggleLanguageAsync();
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(40, 20, 760, 780));
        workspace.Open(WorkspaceState.NodeEditorId);
        main.UpdateLayout();
        string presetPath = Path.Combine(output, "My Rim.seqdrum");
        main.Activate();
        await Task.Delay(150);
        var before = workflow.State.Snapshot;
        await input.Click(Id("Drum.SavePreset"));
        await input.FileDialog(presetPath);
        for (int wait = 0;
             wait < 100 && (!File.Exists(presetPath) ||
                            Field<bool>(main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single(),
                                "_presetBusy"));
             wait++) await Task.Delay(30);
        Require(File.Exists(presetPath), "Native custom preset save failed.");
        var current = main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single();
        await input.Exact(Id("Drum.tune"), "5");
        await current.PendingCommand;
        int undo = workflow.State.UndoCount;
        await input.Click(Id("Drum.LoadPreset"));
        await input.FileDialog(presetPath);
        for (int wait = 0; wait < 100 && workflow.State.UndoCount == undo; wait++) await Task.Delay(30);
        Require(
            workflow.State.UndoCount == undo + 1 && workflow.State.Snapshot.State.Sounds
                .Single(s => s.Id == first.Parts.Last().SoundId).Parameters["tune"] == 0,
            "Native preset restore did not commit once.");
        Require(await workflow.UndoAsync() && await workflow.UndoAsync(true), "Preset Undo/Redo failed.");
        Json(output, "drum-kit-review", new
        {
            Matrix = "7 voices × RU/EN × Dark/Light × ordinary/minimum", FieldAlignment = aligned,
            CustomPresetNativeSaveLoad = true, SingleUndo = true, NoLibraryWrites = true
        });
    }
}
