// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
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
    private static async Task SamplerKeymapAsync(MainWindow main, string output, string library, string delivery,
        bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        using var catalog = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(library, "catalog.json")));
        var files = catalog.RootElement.EnumerateArray().Where(e =>
                e.GetProperty("category").GetString() == "Piano/Single-Notes" &&
                e.GetProperty("source_path").GetString()!.Contains("dyn1", StringComparison.OrdinalIgnoreCase))
            .Select(e => (Path: Path.Combine(library, e.GetProperty("file").GetString()!),
                Root: e.GetProperty("root_midi").GetDecimal()))
            .OrderBy(e => e.Root).ToArray();
        Require(files.Length >= 3, "At least three confirmed piano roots are required.");
        main.WindowState = WindowState.Normal;
        main.Width = 1400;
        main.Height = 900;
        Require(
            await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
                "Sampler comparison"), "Fixture failed.");
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        var initial = files.OrderBy(f => Math.Abs(f.Root - 60)).First();
        Require(await workflow.AddBeatWavAsync(Basis(), initial.Path), "Piano import failed.");
        Require(await workflow.ResizeBeatAsync(Basis(), 2), "Length failed.");
        var pattern = BeatSteps.Resolve(workflow.State).Pattern!;
        var part = pattern.Parts.Single();
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId,
            attachment.Sources.Single().NodeId);
        var notes = new List<NoteEvent>();
        // A bounded acceptance fragment, not a new composition: melody, two chords and one held bass.
        decimal[] melody = [45, 52, 57, 60, 61, 64, 69, 77, 81, 77, 72, 69];
        for (int i = 0; i < melody.Length; i++)
            notes.Add(new(Id<NoteEvent>.New(), new(i * 480000L), new(420000), melody[i], .65m));
        foreach (int key in new[] { 48, 55, 60, 64 })
            notes.Add(new(Id<NoteEvent>.New(), new(0), new(1800000), key, .45m));
        foreach (int key in new[] { 57, 61, 64, 69 })
            notes.Add(new(Id<NoteEvent>.New(), new(3840000), new(2400000), key, .45m));
        Require(await workflow.EditPianoNotesAsync(Basis(), part.Id, notes), "Notes failed.");
        Require(await workflow.EditGraphAsync(
                new(workflow.State.Document.LifecycleId, workflow.State.Generation, attachment.Id), "Release",
                (e, _, _) => e.SetSoundParameters(part.SoundId,
                    workflow.State.Snapshot.State.Sounds.Single().Parameters.SetItem("releaseMilliseconds", 80))),
            "Release failed.");
        Directory.CreateDirectory(delivery);
        string single = Path.Combine(delivery, "01-single-sample.wav"),
            multi = Path.Combine(delivery, "02-multisample.wav");
        Require(await workflow.ExportWaveAsync(Basis(), single), "Single export failed.");
        Invoke(main, "OpenNodeEditor", target);
        foreach (var pane in workspace.State.Panes.Where(p => p.InstanceId != WorkspaceState.NodeEditorId).ToArray())
            workspace.State.Hide(pane.InstanceId);
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 850, 680));
        workspace.Commit();
        SamplerPaneContent Pane() => main.GetVisualDescendants().OfType<SamplerPaneContent>().Single();

        Control Id(string id)
        {
            if (id is "Start" or "End" or "LoopStart" or "LoopEnd" or "Apply")
            {
                Field<Expander>(Pane(), "_sourceDetails").IsExpanded = true;
                main.UpdateLayout();
            }

            if (id == "RemoveSource") return Field<Button>(Pane(), "_removeSource");
            return main.GetVisualDescendants().OfType<Control>().Single(c =>
                c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == "Sampler." + id);
        }

        async Task Refresh()
        {
            await Pane().PendingCommand;
            await workflow.ObserveAsync();
            await Pane().PendingInspection;
            main.UpdateLayout();
            await Task.Delay(70);
        }

        async Task Click(string id)
        {
            ((Button)Id(id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Refresh();
        }

        await Refresh();
        int undo = workflow.State.UndoCount;
        Require(
            await workflow.AddSamplerSourcesAsync(target, workflow.State.Generation,
                files.Where(f => f.Path != initial.Path).ToArray()), "Map import failed.");
        await Refresh();
        SoundDefinition Sound() => workflow.State.Snapshot.State.Sounds.Single();
        Require(Sound().SamplerZones.Length == files.Length && workflow.State.UndoCount == undo + 1,
            "Batch history failed.");
        var originalMap = Sound().SamplerZones;
        Require(await workflow.UndoAsync(), "Batch Undo failed.");
        Require(!SamplerKeymap.HasIntent(Sound()), "Undo lost legacy.");
        Require(await workflow.UndoAsync(true), "Batch Redo failed.");
        await Refresh();
        Require(await workflow.ExportWaveAsync(Basis(), multi), "Multi export failed.");
        using var singlePcm = WavDecoder.Decode(File.ReadAllBytes(single));
        using var multiPcm = WavDecoder.Decode(File.ReadAllBytes(multi));
        Require(singlePcm.Frames == multiPcm.Frames, "Comparison length differs.");
        Require(Enumerable.Range(0, multiPcm.Frames).Any(i => singlePcm.Sample(i, 0) != multiPcm.Sample(i, 0)),
            "Comparison PCM did not change.");
        var selected = Field<SamplerZone>(Pane(), "_zone");
        ((TextBox)Id("Root")).Text = "C4";
        await Click("ApplyKeys");
        Require(Sound().SamplerZones.Single(z => z.Id == selected.Id).RootPitch == 60, "Root field failed.");
        Require(await workflow.UndoAsync(), "Root Undo failed.");
        await Refresh();
        var before = workflow.State.Snapshot;
        ((TextBox)Id("High")).Text = "127";
        await Click("ApplyKeys");
        Require(workflow.State.Snapshot == before, "Overlap changed music.");
        Id("High").RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        await Refresh();
        var keyboard = (SamplerKeyboard)Id("Keyboard");
        using (var pointer = new Pointer(97001, PointerType.Mouse, true))
        {
            var root = TopLevel.GetTopLevel(keyboard)!;
            Point Root(Point p) => keyboard.TranslatePoint(p, root)!.Value;
            var z = Field<SamplerZone>(Pane(), "_zone");
            keyboard.Reveal(z.HighKey);
            var start = new Point(keyboard.PitchX(z.HighKey + 1) - 2, 36);
            var end = start - new Vector(keyboard.Bounds.Width / keyboard.VisibleKeys * 2, 0);

            void Press() => keyboard.RaiseEvent(new PointerPressedEventArgs(keyboard, pointer, root, Root(start), 0,
                new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));

            void Move() => keyboard.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, keyboard, pointer,
                root, Root(end), 0,
                new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));

            void Release() => keyboard.RaiseEvent(new PointerReleasedEventArgs(keyboard, pointer, root, Root(end), 0,
                new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None,
                MouseButton.Left));

            undo = workflow.State.UndoCount;
            Press();
            Move();
            Release();
            await Refresh();
            Require(
                Sound().SamplerZones.Single(n => n.Id == z.Id).HighKey < z.HighKey &&
                workflow.State.UndoCount == undo + 1, "Key boundary drag failed.");
            Require(await workflow.UndoAsync(), "Drag Undo failed.");
            await Refresh();
            Press();
            Move();
            keyboard.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            Release();
            await Refresh();
            Require(Sound().SamplerZones.SequenceEqual(originalMap), "Escape committed map.");
            Press();
            Move();
            pointer.Capture(null);
            Release();
            await Refresh();
            Require(Sound().SamplerZones.SequenceEqual(originalMap), "Capture loss committed map.");
        }

        var chooser = (ComboBox)Id("Sources");
        chooser.SelectedIndex = 1;
        await Refresh();
        var second = Field<SamplerZone>(Pane(), "_zone");
        Require(second.Id != selected.Id, "Source selection failed.");
        ((TextBox)Id("Start")).Text = "100";
        await Click("Apply");
        Require(Sound().SamplerZones.Single(z => z.Id == second.Id).Parameters["sampleStart"] == 100 &&
                Sound().SamplerZones.Where(z => z.Id != second.Id).All(z => z.Parameters.IsEmpty),
            "Source ranges leaked.");
        Require(await workflow.UndoAsync(), "Range Undo failed.");
        await Refresh();
        await Click("RemoveSource");
        Require(Sound().SamplerZones.Length == files.Length - 1, "Remove failed.");
        Require(await workflow.UndoAsync(), "Remove Undo failed.");
        await Refresh();
        // Review dozens without increasing vertical content or a table.
        var dense = SamplerKeymap.Distribute(Enumerable.Range(0, 26).Select(i => originalMap[0] with
        {
            Id = Id<SamplerZone>.New(), RootPitch = 21 + i * 4
        }));
        Require(await workflow.SetSamplerMapAsync(target, workflow.State.Generation, dense), "Dense map failed.");
        await Refresh();
        ((ComboBox)Id("Sources")).SelectedIndex = 13;
        await Refresh();
        var alignments = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width;
                    main.Height = width == 1400 ? 900 : 640;
                    await Refresh();
                    workspace.State.SetBounds(WorkspaceState.NodeEditorId,
                        new(0, 0, width == 1400 ? 850 : 480, width == 1400 ? 680 : 540));
                    workspace.Commit();
                    await Refresh();
                    Require(width != 1400 || Pane().Bounds.Width >= 800,
                        "Ordinary pane was clamped against the previous compact window.");
                    foreach (string key in new[]
                                 { "Root", "Low", "High", "Start", "End", "LoopStart", "LoopEnd", "Crossfade" })
                    {
                        var field = Id(key);
                        var row = (Grid)field.Parent!;
                        alignments.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().Single(), row,
                            $"{language}-{theme}-{width}-{key}"));
                    }

                    var details = Field<Expander>(Pane(), "_sourceDetails");
                    var lastField = Id("LoopEnd");
                    var bottom = lastField.TranslatePoint(new(0, lastField.Bounds.Height), Pane())!.Value.Y;
                    Require(bottom <= Pane().Bounds.Height, "Expanded source fields escaped the pane.");
                    var waveControl = Id("Waveform");
                    var holdControl = Id("Hold");
                    var loopControl = Id("Loop");
                    double waveTop = waveControl.TranslatePoint(new(0, 0), Pane())!.Value.Y;
                    double holdBottom = holdControl.TranslatePoint(new(0, holdControl.Bounds.Height), Pane())!.Value.Y;
                    double loopTop = loopControl.TranslatePoint(new(0, 0), Pane())!.Value.Y;
                    Json(output, $"source-layout-{language}-{theme}-{width}",
                        new
                        {
                            Pane = Pane().Bounds, Wave = waveControl.Bounds, waveTop, holdBottom, loopTop,
                            FieldsBottom = bottom, Rows = Pane().RowDefinitions.Select(r => r.ActualHeight).ToArray()
                        });
                    Require(waveTop >= holdControl.TranslatePoint(new(0, holdControl.Bounds.Height), Pane())!.Value.Y &&
                            loopControl.TranslatePoint(new(0, 0), Pane())!.Value.Y >=
                            waveTop + waveControl.Bounds.Height,
                        $"Source groups overlap: wave {waveTop}..{waveTop + waveControl.Bounds.Height}, hold bottom {holdBottom}, loop top {loopTop}.");
                    await Task.Delay(80);
                    await CaptureAsync(main, output, $"keymap-fields-{language}-{theme}-{width}");
                    details.IsExpanded = false;
                    main.UpdateLayout();
                    await Task.Delay(80);
                    Require(keyboard.Bounds.Width / keyboard.VisibleKeys >= 24, "Adaptive keys are too small.");
                    var rootPitch = Field<SamplerZone>(Pane(), "_zone").RootPitch;
                    Require(
                        rootPitch >= (decimal)keyboard.FirstKey &&
                        rootPitch < (decimal)(keyboard.FirstKey + keyboard.VisibleKeys),
                        "Resize hid the selected root.");
                    Require(Id("Crossfade").Bounds.Height >= 24 && Id("Loop").IsEffectivelyVisible,
                        "Source controls lost.");
                    Require(Id("Waveform").Bounds.Height >= 100, "Waveform lost usable height.");
                    await CaptureAsync(main, output, $"keymap-26-{language}-{theme}-{width}");
                    var select = (ComboBox)Id("Sources");
                    var sourceItem = ((System.Collections.IEnumerable)select.ItemsSource!).Cast<TextBlock>().First();
                    Require(
                        sourceItem.Foreground is Avalonia.Media.ISolidColorBrush ink && ink.Color ==
                        Avalonia.Media.Color.Parse(HostPalette.For(theme)["Text.Primary"]),
                        "Source text lost theme contrast.");
                    if (width == 800)
                    {
                        select.IsEnabled = false;
                        await CaptureAsync(main, output, $"keymap-source-disabled-{language}-{theme}");
                        select.IsEnabled = true;
                        select.IsDropDownOpen = true;
                        main.UpdateLayout();
                        await Task.Delay(80);
                        var popupRoot = ((System.Collections.IEnumerable)select.ItemsSource!).Cast<TextBlock>()
                            .Select(TopLevel.GetTopLevel).FirstOrDefault(t => t is not null && t != main);
                        Require(popupRoot is not null, "Source dropdown did not expose its visible rows.");
                        await CapturePopupAsync(popupRoot!, output, $"keymap-source-list-{language}-{theme}");
                        select.IsDropDownOpen = false;
                        var snapshot = workflow.State.Snapshot;
                        keyboard.Pan(0);
                        await Refresh();
                        Require(keyboard.FirstKey == 0 && workflow.State.Snapshot == snapshot,
                            "Observation recentered a freely panned keyboard.");
                        ((Avalonia.Controls.Primitives.ScrollBar)Id("KeyPan")).Value = 90;
                        await Refresh();
                        Require(keyboard.FirstKey > 70, "Horizontal navigation failed.");
                        keyboard.Fit();
                        await Refresh();
                    }
                }
            }
        }

        Require(await workflow.UndoAsync(), "Dense Undo failed.");
        await Refresh();
        main.Width = 1400;
        main.Height = 900;
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 850, 680));
        workspace.Commit();
        await Refresh();
        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                var input = new NativePointer.DrumInput(main);
                await input.ActivateVisibleWindow();
                await input.Exact(Id("Root"), "62");
                await Refresh();
                Require(Field<SamplerZone>(Pane(), "_zone").RootPitch == 62, "Native root failed.");
                Require(await workflow.UndoAsync(), "Native Undo failed.");
                await Refresh();
                var z = Field<SamplerZone>(Pane(), "_zone");
                keyboard.Reveal(z.HighKey);
                var point = new Point(keyboard.PitchX(z.HighKey + 1) - 2, 36);
                double keyWidth = keyboard.Bounds.Width / keyboard.VisibleKeys;
                await input.TraceDrag(keyboard,
                    [point, point - new Vector(keyWidth, 0), point - new Vector(keyWidth * 2, 0)]);
                await Refresh();
                Require(Field<SamplerZone>(Pane(), "_zone").HighKey < z.HighKey, "Native boundary failed.");
                Require(await workflow.UndoAsync(), "Native drag Undo failed.");
                await Refresh();
                nativeResult = new { Result = "passed", Scope = "Root entry and zone boundary drag; setup routed" };
            }
            catch (Exception error)
            {
                nativeResult = new { Result = "refused-or-failed", Reason = error.Message };
            }
        }

        object physical = new { Result = "not-run" };
        if (audio)
        {
            try
            {
                await Click("Hold");
                await Task.Delay(350);
                await Refresh();
                var outputOwner = Field<WorkflowOutput>(workflow, "_noteOutput");
                Require(workflow.SamplerPreviewPlaying && outputOwner.ReadFailure() is null,
                    "Physical sample preview failed.");
                await Click("Hold");
                await Task.Delay(200);
                await Click("Stop");
                Require(outputOwner.Lifetime.Joined, "Sample preview not joined.");
                await workflow.SetSongModeAsync(false);
                Require(await workflow.PlayAsync(), "Physical Pattern failed: " + workflow.State.Detail);
                await Task.Delay(1800);
                await workflow.ObserveAsync();
                Require(workflow.State.Execution?.Execution is { Playing: true, Voices: > 0 },
                    "Physical mapped chord failed.");
                var status = workflow.State.Execution;
                await workflow.StopAsync();
                physical = new
                {
                    Result = "passed", Device = outputOwner.Description, Pattern = status, PreviewJoined = true,
                    SubjectiveListening = false
                };
            }
            catch (Exception error)
            {
                physical = new { Result = "failed", Reason = error.Message };
            }
        }

        string saved = Path.Combine(output, "mapped.seqvium");
        Require(await workflow.SaveAsync(saved), "Save failed.");
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed.");
        Require(await workflow.OpenAsync(saved, UnsavedDecision.Discard), "Portable Open failed.");
        Require(Sound().SamplerZones.SequenceEqual(originalMap), "Portable map mismatch.");
        var reopened = workflow.State.Snapshot.State.GraphAttachments.Single();
        Invoke(main, "OpenNodeEditor",
            new NodeEditorTarget(workflow.State.Document.LifecycleId, reopened.Id, reopened.GraphId,
                reopened.Sources.Single().NodeId));
        await Refresh();
        await CaptureAsync(main, output, "keymap-reopened-piano");
        Json(output, "keymap", new
        {
            Routed = "passed", Native = nativeResult, Physical = physical, Zones = files.Length,
            DenseZones = 26, Portable = true, CompareFrames = multiPcm.Frames, Single = single, Multi = multi,
            Source = "Local verified-catalog VSCO Upright Piano soft; FreePats absent", NewComposition = false
        });
        Json(output, "field-alignment", alignments);
        var roots = new decimal?[] { null, 72 };
        var review = new SamplerImportWindow(shell, ["Unknown source.wav", "Known source.wav"], roots);
        var reviewTask = review.ShowDialog<bool>(main);
        await Task.Delay(80);
        review.UpdateLayout();

        Control ReviewId(string id) => review.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == "Sampler." + id);

        Require(!((Button)ReviewId("ImportAdd")).IsEnabled && ((TextBox)ReviewId("ImportRoot")).Text == "",
            "Unknown root was guessed.");
        await CaptureAsync(review, output, "unknown-root-review");
        ((TextBox)ReviewId("ImportRoot")).Text = "C4";
        Require(((Button)ReviewId("ImportAdd")).IsEnabled, "Explicit root did not enable import.");
        ((Button)ReviewId("ImportCancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(!await reviewTask, "Review cancellation failed.");
    }
}