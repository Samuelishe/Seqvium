// SPDX-License-Identifier: Apache-2.0

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
    private static async Task SamplerEditorAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        main.WindowState = WindowState.Normal;
        main.Width = 1400;
        main.Height = 900;
        string sourceFile = Path.Combine(output, "authored-source.wav");
        WriteSamplerStudy(sourceFile);
        Require(
            await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
                "Sustain study"), "Create failed.");
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Require(await workflow.AddBeatWavAsync(Basis(), sourceFile), "WAV import failed.");
        var pattern = BeatSteps.Resolve(workflow.State).Pattern!;
        var part = pattern.Parts.Single();
        Require(await workflow.EditPianoNotesAsync(Basis(), part.Id,
            [new(Id<NoteEvent>.New(), new(0), new(3840000), 60, .7m)]), "Long note failed.");
        Require(await workflow.EditGraphAsync(new(workflow.State.Document.LifecycleId, workflow.State.Generation,
                workflow.State.Selected!.Value), "Preview release", (e, _, _) => e.SetSoundParameters(part.SoundId,
                workflow.State.Snapshot.State.Sounds.Single(s => s.Id == part.SoundId).Parameters
                    .SetItem("releaseMilliseconds", 80))),
            "Release fixture failed.");
        // Exercise the existing Channel Rack event, including its normal target routing.
        workspace.Open(WorkspaceState.BeatId);
        main.UpdateLayout();
        await Task.Delay(80);
        var beat = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        beat.Refresh();
        main.UpdateLayout();
        var source = workflow.State.Snapshot.State.GraphAttachments.Single().Sources.Single();
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId,
            source.NodeId);
        var more = beat.GetVisualDescendants().OfType<Button>().Single(b => b.ContextMenu?.Items.OfType<MenuItem>()
            .Any(m =>
                AutomationProperties.GetAutomationId(m) == "Beat.Sound." + part.Id) == true);
        more.ContextMenu!.Open(more);
        main.UpdateLayout();
        more.ContextMenu.Items.OfType<MenuItem>()
            .Single(m => AutomationProperties.GetAutomationId(m) == "Beat.Sound." + part.Id)
            .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        more.ContextMenu.Close();
        foreach (var pane in workspace.State.Panes.Where(p => p.InstanceId != WorkspaceState.NodeEditorId).ToArray())
            workspace.State.Hide(pane.InstanceId);
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 760, 520));
        workspace.Commit();
        SamplerPaneContent Pane() => main.GetVisualDescendants().OfType<SamplerPaneContent>().Single();

        Control Id(string id)
        {
            if (id is "Start" or "End" or "LoopStart" or "LoopEnd" or "Apply")
            {
                Field<Expander>(Pane(), "_sourceDetails").IsExpanded = true;
                main.UpdateLayout();
            }

            return main.GetVisualDescendants().OfType<Control>().Single(c =>
                c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == "Sampler." + id);
        }

        async Task Refresh()
        {
            await Pane().PendingCommand;
            await workflow.ObserveAsync();
            await Pane().PendingInspection;
            main.UpdateLayout();
            await Task.Delay(60);
        }

        void Click(string key) => ((Button)Id(key)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void Text(string key, string value) => ((TextBox)Id(key)).Text = value;
        await Refresh();
        Require(Field<NodeEditorSession>(main, "_nodeEditor").Target == target,
            "Rack Sound Editor route lost source identity.");
        Invoke(main, "OpenGraph");
        main.UpdateLayout();
        await Task.Delay(60);
        var canvas = main.GetVisualDescendants().OfType<GraphCanvas>().Single();
        canvas.Fit();
        var graphEditor = Field<GraphEditorSession>(main, "_graphEditor");
        var node = graphEditor.Graph!.Nodes.Single(n => n.Id == source.NodeId);
        var body = GraphGeometry.Body(node);
        var graphPoint = graphEditor.Viewport.ToScreen(new(body.X + 50, body.Y + 18));
        var rootPoint = canvas.TranslatePoint(new(graphPoint.X, graphPoint.Y), main)!.Value;
        using (var doublePointer = new Pointer(95000, PointerType.Mouse, true))
            for (int click = 1; click <= 2; click++)
            {
                canvas.RaiseEvent(new PointerPressedEventArgs(canvas, doublePointer, main, rootPoint, 0,
                    new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None,
                    click));
                canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, doublePointer, main, rootPoint, 0,
                    new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None,
                    MouseButton.Left));
            }

        Require(Field<NodeEditorSession>(main, "_nodeEditor").Target == target,
            "Source double-click lost sampler identity.");
        workspace.State.Hide(WorkspaceState.GraphId);
        workspace.Open(WorkspaceState.NodeEditorId);
        await Refresh();

        SamplerPlayback Current() =>
            SamplerPlayback.Read(workflow.State.Snapshot.State.Sounds.Single(s => s.Id == part.SoundId), 24000);

        int undo = workflow.State.UndoCount;
        Text("Start", "2400");
        Text("End", "21600");
        Click("Apply");
        await Refresh();
        Require(Current().Start == 2400 && Current().End == 21600 && workflow.State.UndoCount == undo + 1,
            "Range commit/history failed.");
        ((Avalonia.Controls.Primitives.ToggleButton)Id("Loop")).IsChecked = true;
        await Refresh();
        Text("LoopStart", "7200");
        Text("LoopEnd", "16800");
        Click("Apply");
        await Refresh();
        Require(Current() == new SamplerPlayback(2400, 21600, true, 7200, 16800), "Loop fields failed.");
        var before = workflow.State.Snapshot;
        Text("LoopEnd", "7201");
        Click("Apply");
        await Refresh();
        Require(workflow.State.Snapshot == before, "Short loop mutated intent.");
        ((TextBox)Id("LoopEnd")).RaiseEvent(new KeyEventArgs
            { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        Text("Start", "abc");
        await Refresh();
        Require(((TextBox)Id("Start")).Text == "abc", "Observation overwrote an incomplete numeric draft.");
        Click("Apply");
        await Refresh();
        Require(workflow.State.Snapshot == before, "Invalid text changed music.");
        ((TextBox)Id("Start")).RaiseEvent(
            new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        Require(await workflow.UndoAsync(), "Undo failed.");
        Require(await workflow.UndoAsync(true), "Redo failed.");
        await Refresh();
        var wave = (SamplerWaveform)Id("Waveform");
        using (var pointer = new Pointer(95001, PointerType.Mouse, true))
        {
            var root = TopLevel.GetTopLevel(wave)!;
            Point Root(Point p) => wave.TranslatePoint(p, root)!.Value;
            var start = new Point(wave.Bounds.Width * Current().Start / 24000d + 2, 12);
            var end = start + new Vector(35, 0);

            void Press() => wave.RaiseEvent(new PointerPressedEventArgs(wave, pointer, root, Root(start), 0,
                new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));

            void Move() => wave.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, wave, pointer, root,
                Root(end), 0,
                new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));

            void Release() => wave.RaiseEvent(new PointerReleasedEventArgs(wave, pointer, root, Root(end), 0,
                new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None,
                MouseButton.Left));

            undo = workflow.State.UndoCount;
            Press();
            Move();
            Release();
            await Refresh();
            Require(Current().Start > 2400 && workflow.State.UndoCount == undo + 1,
                "Waveform drag did not commit exactly once.");
            Require(await workflow.UndoAsync(), "Drag Undo failed.");
            await Refresh();
            Press();
            Move();
            wave.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            Release();
            await Refresh();
            Require(Current().Start == 2400 && workflow.State.UndoCount == undo, "Cancelled drag edited music.");
            Press();
            Move();
            pointer.Capture(null);
            Release();
            await Refresh();
            Require(Current().Start == 2400 && workflow.State.UndoCount == undo, "Capture loss edited music.");
        }

        Click("ZoomIn");
        await Refresh();
        Require(wave.ViewLength == 12000, "Zoom failed.");
        ((Slider)Id("Pan")).Value = 3000;
        Require(wave.ViewStart == 3000, "Pan failed.");
        Click("Fit");
        await Refresh();
        Require(wave.ViewLength == 24000, "Fit failed.");
        before = workflow.State.Snapshot;
        undo = workflow.State.UndoCount;
        Text("Crossfade", "4801");
        Click("Apply");
        await Refresh();
        Require(workflow.State.Snapshot == before, "Unavailable pre-loop frames were silently adopted.");
        Text("Crossfade", "2048");
        Click("Apply");
        await Refresh();
        Require(Current().Crossfade == 2048 && workflow.State.UndoCount == undo + 1, "Crossfade did not commit once.");
        Require(await workflow.UndoAsync(), "Crossfade Undo failed.");
        await Refresh();
        Require(Current().Crossfade == 0, "Crossfade Undo lost F1.");
        Require(await workflow.UndoAsync(true), "Crossfade Redo failed.");
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
                    main.Height = width == 1400 ? 900 : 600;
                    await Refresh();
                    workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, width == 1400 ? 760 : 480, 520));
                    workspace.Commit();
                    await Refresh();
                    foreach (string key in new[] { "Start", "End", "LoopStart", "LoopEnd", "Crossfade" })
                    {
                        var field = Id(key);
                        var row = (Grid)field.Parent!;
                        alignments.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().Single(), row,
                            $"{language}-{theme}-{width}-{key}"));
                    }

                    await CaptureAsync(main, output, $"sampler-{language}-{theme}-{width}");
                }
            }
        }

        Json(output, "field-alignment", alignments);
        main.Width = 1400;
        main.Height = 900;
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 760, 520));
        workspace.Commit();
        await Refresh();
        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                var input = new NativePointer.DrumInput(main);
                await input.ActivateVisibleWindow();
                await input.Exact(Id("Start"), "2600");
                await input.Click(Id("Apply"));
                await Refresh();
                Require(Current().Start == 2600, "Native range commit failed.");
                Require(await workflow.UndoAsync(), "Native Undo failed.");
                await Refresh();
                var marker = new Point(wave.Bounds.Width * Current().Start / 24000d + 2, 12);
                await input.TraceDrag(wave, [marker, marker + new Vector(16, 0), marker + new Vector(32, 0)]);
                await Refresh();
                Require(Current().Start > 2400, "Native marker drag failed.");
                Require(await workflow.UndoAsync(), "Native marker Undo failed.");
                await Refresh();
                await input.Exact(Id("Crossfade"), "1024");
                await input.Click(Id("Apply"));
                await Refresh();
                Require(Current().Crossfade == 1024, "Native crossfade commit failed.");
                Require(await workflow.UndoAsync(), "Native crossfade Undo failed.");
                await Refresh();
                nativeResult = new
                {
                    Result = "passed",
                    Checks = "Native exact Start, crossfade/Apply and waveform marker drag; setup routed"
                };
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
                before = workflow.State.Snapshot;
                undo = workflow.State.UndoCount;
                Click("Hold");
                await Refresh();
                for (int i = 0; i < 100 && !workflow.SamplerPreviewPlaying; i++) await Task.Delay(20);
                Require(workflow.SamplerPreviewPlaying, workflow.State.Detail ?? "Held preview failed.");
                var previewOutput = Field<WorkflowOutput>(workflow, "_noteOutput");
                await Task.Delay(500);
                Require(workflow.SamplerPreviewPlaying && previewOutput.ReadFailure() is null,
                    $"Held preview stopped or faulted: {previewOutput.Sampler.ReadStatus()}; fault={previewOutput.ReadFailure() ?? "none"}.");
                Click("Hold");
                await Refresh();
                for (int i = 0; i < 100 && workflow.SamplerPreviewPlaying; i++) await Task.Delay(20);
                Require(
                    !workflow.SamplerPreviewPlaying && workflow.State.Snapshot == before &&
                    workflow.State.UndoCount == undo,
                    "Release did not finish or preview edited music.");
                var released = previewOutput.Sampler.ReadStatus();
                Require(released.Voices == 0 && previewOutput.ReadFailure() is null, "Release left voices or faulted.");
                Click("Hold");
                await Refresh();
                await Task.Delay(100);
                var stoppedOutput = Field<WorkflowOutput>(workflow, "_noteOutput");
                Click("Stop");
                await Refresh();
                Require(!workflow.SamplerPreviewPlaying && stoppedOutput.Lifetime.Joined, "Preview Stop/join failed.");
                physical = new
                {
                    Result = "passed", Device = previewOutput.Description, Released = released, JoinedStop = true,
                    Checks = "Default physical output Hold past EOF, bounded Release, Stop; no canonical edits",
                    SubjectiveListening = false
                };
            }
            catch (Exception error)
            {
                physical = new { Result = "failed", Reason = error.Message };
            }
        }

        Json(output, "input-audio", new { Native = nativeResult, Physical = physical });
        string saved = Path.Combine(output, "sampler.seqvium");
        var expected = Current();
        Require(await workflow.SaveAsync(saved), "Save failed.");
        File.Delete(sourceFile);
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed.");
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width;
                    main.Height = width == 1400 ? 900 : 600;
                    workspace.Open(WorkspaceState.NodeEditorId);
                    workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, width == 1400 ? 760 : 480, 520));
                    workspace.Commit();
                    main.UpdateLayout();
                    await Task.Delay(60);
                    await CaptureAsync(main, output, $"empty-{language}-{theme}-{width}");
                }
            }
        }

        main.Width = 1400;
        main.Height = 900;
        Require(await workflow.OpenAsync(saved, UnsavedDecision.Discard), "Open failed.");
        Require(Current() == expected, "Portable reopen lost range/loop.");
        var reopenedAttachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var reopenedTarget = new NodeEditorTarget(workflow.State.Document.LifecycleId, reopenedAttachment.Id,
            reopenedAttachment.GraphId,
            reopenedAttachment.Sources.Single().NodeId);
        Invoke(main, "OpenNodeEditor", reopenedTarget);
        await Refresh();
        Require(((TextBox)Id("Start")).Text == expected.Start.ToString(), "Reopened editor lost exact range.");
        Require(((TextBox)Id("Crossfade")).Text == expected.Crossfade.ToString(), "Reopened editor lost crossfade.");
        string export = Path.Combine(output, "sampler-pattern.wav");
        Require(await workflow.ExportWaveAsync(Basis(), export), "Pattern export failed.");
        var sample = await PatternResampling.RenderAsync(workflow.State.Document, reopenedAttachment.Id);
        Require(sample.Frames >= 96000 && sample.Frames < 200000, "R8 loop render not finite/musically gated.");
        Json(output, "sampler", new
        {
            Routed = "passed", Portable = true, Range = Current(), R8Frames = sample.Frames,
            Native = nativeResult, Physical = physical, SubjectiveLoopQuality = "not-assessed"
        });
        await SamplerLoopComparisonsAsync(output);
    }

    private static void WriteSamplerStudy(string path)
    {
        const int frames = 24000, rate = 48000;
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF"u8);
        writer.Write(36 + frames * 8);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((ushort)3);
        writer.Write((ushort)2);
        writer.Write(rate);
        writer.Write(rate * 8);
        writer.Write((ushort)8);
        writer.Write((ushort)32);
        writer.Write("data"u8);
        writer.Write(frames * 8);
        for (int frame = 0; frame < frames; frame++)
        {
            double amplitude = .3 * Math.Min(1, frame / 1400d) * Math.Exp(-frame / 36000d);
            writer.Write((float)(amplitude * Math.Sin(frame * Math.Tau * 220 / rate)));
            writer.Write((float)(amplitude * .8 * Math.Sin(frame * Math.Tau * 330 / rate)));
        }
    }
}