// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
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
    private static async Task SamplerVelocityAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900;
        string[] paths = [Path.Combine(output, "Soft.wav"), Path.Combine(output, "Medium.wav"), Path.Combine(output, "Hard.wav")];
        for (int layer = 0; layer < 3; layer++)
        {
            int harmonic = layer + 1;
            WriteLoopFloatWave(paths[layer], Enumerable.Range(0, 24000).SelectMany(i => new[]
            {
                (float)(.2 * Math.Sin(i * Math.Tau * 220 * harmonic / 48000)),
                (float)(.15 * Math.Sin(i * Math.Tau * 330 * harmonic / 48000))
            }).ToArray());
        }
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Velocity layers"), "Create failed");
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Require(await workflow.AddBeatWavAsync(Basis(), paths[0]), "Source failed");
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId, attachment.Sources.Single().NodeId);
        SoundDefinition Sound() => workflow.State.Snapshot.State.Sounds.Single();
        await workflow.EditGraphAsync(new(workflow.State.Document.LifecycleId, workflow.State.Generation, attachment.Id),
            "Root", (e, _, _) => e.SetSoundParameters(Sound().Id, Sound().Parameters.SetItem("rootPitch", 60)));
        Require(Sound().Parameters["rootPitch"] == 60, "Root failed");
        Invoke(main, "OpenNodeEditor", target);
        foreach (var pane in workspace.State.Panes.Where(p => p.InstanceId != WorkspaceState.NodeEditorId).ToArray()) workspace.State.Hide(pane.InstanceId);
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 900, 710)); workspace.Commit();
        SamplerPaneContent Pane() => main.GetVisualDescendants().OfType<SamplerPaneContent>().Single();
        async Task Refresh()
        {
            await Pane().PendingCommand; await workflow.ObserveAsync(); await Pane().PendingInspection;
            main.UpdateLayout(); await Task.Delay(45);
        }
        Control Id(string key) => main.GetVisualDescendants().OfType<Control>().Single(c => c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == "Sampler." + key);
        async Task Click(string key) { ((Button)Id(key)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Refresh(); }
        await Refresh();
        // Use the actual review dialog, including duplicate roots and its acceptance boundary.
        var roots = new decimal?[] { 60, 60 };
        var review = new SamplerImportWindow(shell, paths[1..], roots, Sound(), ["Soft"]);
        var accepted = review.ShowDialog<bool>(main);
        review.UpdateLayout(); await Task.Delay(60);
        Control ReviewId(string key) => review.GetVisualDescendants().OfType<Control>().Single(c => AutomationProperties.GetAutomationId(c) == "Sampler.Import" + key);
        Require(!((Button)ReviewId("Add")).IsEnabled, "Ambiguous roots accepted");
        ((ComboBox)ReviewId("Sources")).SelectedIndex = 1;
        ((TextBox)ReviewId("Low")).Text = "invalid";
        ((TextBox)ReviewId("Root")).Text = "72";
        ((Button)ReviewId("Layers")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(((TextBox)ReviewId("Root")).Text == "72" && ((TextBox)ReviewId("HighVelocity")).Text == "100",
            "Split ignored the newly confirmed root while replacing invalid ranges");
        ((TextBox)ReviewId("Root")).Text = "60";
        ((Button)ReviewId("Layers")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(((Button)ReviewId("Add")).IsEnabled, "Split did not produce a valid complete map");
        ((ComboBox)ReviewId("Sources")).SelectedIndex = 0;
        await CapturePopupAsync(review, output, "import-three-layers");
        ((ComboBox)ReviewId("Sources")).SelectedIndex = 1;
        Require(((TextBox)ReviewId("LowVelocity")).Text == "33.33", "Draft layer not visible");
        ((Button)ReviewId("Add")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(await accepted, "Review acceptance failed");
        int undo = workflow.State.UndoCount;
        Require(await workflow.AddSamplerSourcesAsync(target, workflow.State.Generation,
            paths[1..].Select(p => (p, 60m)).ToArray(), review.ReviewedZones), "Layer import failed");
        await Refresh();
        Require(Sound().SamplerZones.Length == 3 && workflow.State.UndoCount == undo + 1, "Import was not one edit");
        var original = Sound().SamplerZones;
        Require(await workflow.UndoAsync(), "Import Undo failed"); Require(!SamplerKeymap.HasIntent(Sound()), "Legacy Undo lost");
        Require(await workflow.UndoAsync(true), "Import Redo failed"); await Refresh();
        ((ComboBox)Id("Sources")).SelectedIndex = 1; await Refresh();
        var selected = Field<SamplerZone>(Pane(), "_zone");
        ((TextBox)Id("LowVelocity")).Text = "40"; await Click("ApplyKeys");
        Require(Sound().SamplerZones.Single(z => z.Id == selected.Id).LowVelocity == .4m, "Velocity edit failed");
        Require(await workflow.UndoAsync(), "Velocity Undo failed"); Require(await workflow.UndoAsync(true), "Velocity Redo failed");
        Require(await workflow.UndoAsync(), "Velocity restore failed"); await Refresh();
        var snapshot = workflow.State.Snapshot;
        ((TextBox)Id("LowVelocity")).Text = "20"; await Click("ApplyKeys");
        Require(workflow.State.Snapshot == snapshot, "Overlap changed music");
        Id("LowVelocity").RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape }); await Refresh();
        var keyboard = (SamplerKeyboard)Id("Keyboard");
        using (var pointer = new Pointer(98001, PointerType.Mouse, true))
        {
            var z = Sound().SamplerZones[0];
            var point = keyboard.ZoneBounds(z).Center;
            point = new(keyboard.PitchX(60.5), point.Y);
            var root = TopLevel.GetTopLevel(keyboard)!;
            keyboard.RaiseEvent(new PointerPressedEventArgs(keyboard, pointer, root, keyboard.TranslatePoint(point, root)!.Value, 0,
                new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
            Require(Field<SamplerZone>(Pane(), "_zone").Id == z.Id, "2D source selection failed");
        }
        // A repeated-root import can also be cancelled without canonical or media acceptance.
        snapshot = workflow.State.Snapshot;
        var cancelled = new SamplerImportWindow(shell, [paths[1]], [60], Sound(), ["Soft", "Medium", "Hard"]);
        var cancellation = cancelled.ShowDialog<bool>(main); cancelled.UpdateLayout();
        cancelled.GetVisualDescendants().OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Sampler.ImportCancel")
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(!await cancellation && workflow.State.Snapshot == snapshot, "Cancel changed music");
        var alignments = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width; main.Height = width == 1400 ? 900 : 700;
                    workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, width == 1400 ? 900 : 480, width == 1400 ? 710 : 590)); workspace.Commit();
                    await Refresh();
                    foreach (string key in new[] { "Root", "Low", "High", "LowVelocity", "HighVelocity", "PreviewVelocity" })
                    {
                        var control = Id(key); var row = (Grid)control.Parent!;
                        alignments.Add(FieldAlignment.Verify(control, row.Children.OfType<TextBlock>().Single(), row, $"{language}-{theme}-{width}-{key}"));
                    }
                    Require(Id("Waveform").Bounds.Height >= 88, "Waveform lost height");
                    await CaptureAsync(main, output, $"velocity-{language}-{theme}-{width}");
                    ((ComboBox)Id("Sources")).IsDropDownOpen = true; main.UpdateLayout(); await Task.Delay(40);
                    ((ComboBox)Id("Sources")).IsDropDownOpen = false;
                }
            }
        }
        Json(output, "field-alignment", alignments);
        // Dense maps retain an all-zone selector even when some velocity rectangles are only a pixel tall.
        var dense = SamplerKeymap.Layer(Enumerable.Range(0, 40).Select(i => original[i % 3] with
        {
            Id = Id<SamplerZone>.New(), RootPitch = 48 + i / 4 * 3
        }));
        Require(await workflow.SetSamplerMapAsync(target, workflow.State.Generation, dense), "Dense failed"); await Refresh();
        ((ComboBox)Id("Sources")).SelectedIndex = 39; await Refresh();
        Require(Field<SamplerZone>(Pane(), "_zone").Id == dense.OrderBy(z => z.LowKey).ElementAt(39).Id, "Dense last zone inaccessible");
        await CaptureAsync(main, output, "velocity-dense-reduced");
        Require(await workflow.UndoAsync(), "Dense Undo failed"); await Refresh();
        main.Width = 1400; main.Height = 900;
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 900, 710)); workspace.Commit(); await Refresh();
        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                var input = new NativePointer.DrumInput(main); await input.ActivateVisibleWindow();
                ((ComboBox)Id("Sources")).SelectedIndex = 0; await Refresh();
                await input.Exact(Id("HighVelocity"), "30"); await Refresh();
                Require(Field<SamplerZone>(Pane(), "_zone").HighVelocity == .3m, "Native velocity entry failed");
                Require(await workflow.UndoAsync(), "Native Undo failed"); await Refresh();
                nativeResult = new { Result = "passed", Scope = "Velocity entry; setup routed" };
            }
            catch (Exception e) { nativeResult = new { Result = "refused-or-failed", Reason = e.Message }; }
        }
        object physical = new { Result = "not-run" };
        Require(await workflow.SetSamplerMapAsync(target, workflow.State.Generation,
            [.. Sound().SamplerZones.Where(z => z.HighVelocity < 1)]), "Gap setup failed");
        await Refresh();
        ((ComboBox)Id("PreviewMode")).SelectedIndex = 1;
        ((TextBox)Id("PreviewVelocity")).Text = "100";
        snapshot = workflow.State.Snapshot;
        await Click("Hold");
        Require(!workflow.SamplerPreviewPlaying && workflow.State.Snapshot == snapshot &&
            Field<TextBlock>(Pane(), "_status").Text == shell["Sampler.RangeUnavailable"], "Gap did not report unavailable recording");
        Require(await workflow.UndoAsync(), "Gap restore failed"); await Refresh();
        if (audio)
        {
            try
            {
                ((ComboBox)Id("PreviewMode")).SelectedIndex = 1;
                var resourceResults = new List<string>();
                var devices = new List<string>();
                foreach (string velocity in new[] { "20", "50", "90" })
                {
                    ((TextBox)Id("PreviewVelocity")).Text = velocity; await Click("Hold");
                    Require(workflow.SamplerPreviewPlaying, "Preview did not start");
                    await Task.Delay(80);
                    var owner = Field<WorkflowOutput>(workflow, "_noteOutput");
                    Require(owner.ReadFailure() is null, "Physical output failed");
                    Require(owner.Sampler.ReadStatus().Position > 0, "Physical callback did not advance");
                    devices.Add(owner.Description);
                    resourceResults.Add(velocity); await Click("Hold"); await Click("Stop");
                }
                ((ComboBox)Id("PreviewMode")).SelectedIndex = 0; await Click("Hold");
                Require(workflow.SamplerPreviewPlaying, "Selected WAV preview failed"); await Click("Stop");
                physical = new { Result = "passed", Velocities = resourceResults, Devices = devices,
                    Scope = "Short Instrument and selected WAV previews; no subjective listening" };
            }
            catch (Exception e) { physical = new { Result = "failed", Reason = e.Message }; await workflow.StopSamplerAsync(); }
        }
        string saved = Path.Combine(output, "velocity.seqvium");
        Require(await workflow.SaveAsync(saved), "Save failed");
        var expectedMap = Sound().SamplerZones;
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed");
        Require(await workflow.OpenAsync(saved, UnsavedDecision.Discard), "Open failed");
        Require(Sound().SamplerZones.SequenceEqual(expectedMap), "Open lost layers");
        Json(output, "velocity-results", new { Routed = "passed", Matrix = "RU/EN Dark/Light ordinary/reduced", Native = nativeResult, Physical = physical, SubjectiveListening = "not-performed" });
    }
}
