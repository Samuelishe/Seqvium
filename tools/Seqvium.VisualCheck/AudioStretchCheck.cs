// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
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
    private static async Task AudioStretchAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        await Task.Delay(150);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900;
        workspace.Open(WorkspaceState.ArrangementId); main.UpdateLayout();
        var pane = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        var canvas = pane.Canvas;
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Concat(
            pane.AudioPropertiesContent is Control content ? content.GetVisualDescendants().OfType<Control>().Prepend(content) : [])
            .Single(c => c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);
        void Click(string id) => ((Button)Id(id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        using var pointer = new Pointer(100105, PointerType.Mouse, true);
        void Select()
        {
            var rect = canvas.AudioRect(workflow.State.Snapshot.State.AudioClips.First());
            var point = new Point(rect.X + 20, rect.Y + 8);
            var root = canvas.TranslatePoint(point, main)!.Value;
            canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, main, root, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
            canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, main, root, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        }
        void CloseProperties() => Field<Flyout>(pane, "_audioFlyout").Hide();
        async Task Refresh() { await pane.PendingCommand; await workflow.ObserveAsync(); Invoke(pane, "Refresh"); main.UpdateLayout(); await Task.Delay(50); }
        async Task Drag(Point from, Point to)
        {
            Point Root(Point at) => canvas.TranslatePoint(at, main)!.Value;
            canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, main, Root(from), 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
            canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, canvas, pointer, main, Root(to), 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
            canvas.RaiseEvent(new PointerReleasedEventArgs(canvas, pointer, main, Root(to), 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            await Refresh();
        }

        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                await CaptureAsync(main, output, $"empty-{language}-{theme}");
            }
        }
        string source = Path.Combine(output, "Monophonic phrase.wav");
        WriteStretchPhrase(source);
        await pane.ImportAudioAsync([source]);
        workspace.Open(WorkspaceState.ArrangementId); main.UpdateLayout(); await Task.Delay(100);
        pane = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single(); canvas = pane.Canvas;
        await Refresh(); Select();
        var original = workflow.State.Snapshot.State.AudioClips.Single();
        int undo = workflow.State.UndoCount;
        Click("Arrangement.AudioProperties"); await Task.Delay(80);
        ((TextBox)Id("AudioClip.Stretch")).Text = "200";
        Click("AudioClip.Apply"); await Refresh();
        var stretched = workflow.State.Snapshot.State.AudioClips.Single();
        Require(stretched.StretchRatio == 2 && stretched.LengthFrames == 96000 && workflow.State.UndoCount == undo + 1,
            "Stretch field did not make one local non-destructive edit.");
        Require(AudioClipSource.LengthTicks(workflow.State.Snapshot.State, stretched) ==
            2 * AudioClipSource.LengthTicks(workflow.State.Snapshot.State, original), "Stretch did not double musical width.");
        Require(await workflow.UndoAsync(), "Stretch Undo failed.");
        Require(workflow.State.Snapshot.State.AudioClips.Single() == original, "Undo did not restore clip intent.");
        Require(await workflow.UndoAsync(redo: true), "Stretch Redo failed."); await Refresh();

        // Trim both edges on an explicitly stretched clip at Song zero. The edge gesture must
        // map timeline delta back to source frames and must never become implicit stretching.
        var clipRect = canvas.AudioRect(stretched);
        double barPixels = workflow.State.Snapshot.State.Settings.Meter.TicksPerBar * canvas.TickPixels;
        var rightEdge = new Point(clipRect.Right - 3, clipRect.Bottom - 5);
        await Drag(rightEdge, rightEdge - new Vector(barPixels, 0));
        var rightTrim = workflow.State.Snapshot.State.AudioClips.Single();
        Require(rightTrim.LengthFrames == 48000 && rightTrim.StretchRatio == 2,
            "Right trim at Song zero did not halve source range while retaining stretch.");
        Require(await workflow.UndoAsync(), "Right trim Undo failed."); await Refresh();
        clipRect = canvas.AudioRect(workflow.State.Snapshot.State.AudioClips.Single());
        var leftEdge = new Point(clipRect.X + 3, clipRect.Bottom - 5);
        await Drag(leftEdge, leftEdge + new Vector(barPixels, 0));
        var leftTrim = workflow.State.Snapshot.State.AudioClips.Single();
        Require(leftTrim.OffsetFrames == 48000 && leftTrim.LengthFrames == 48000 &&
            leftTrim.Position.Ticks == 3840000 && leftTrim.StretchRatio == 2, "Left trim lost source/time mapping.");
        Require(await workflow.UndoAsync(), "Left trim Undo failed."); await Refresh();

        var alignments = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width; main.Height = width == 1400 ? 900 : 600; main.UpdateLayout(); await Task.Delay(70);
                    Select(); Click("Arrangement.AudioProperties"); await Task.Delay(100);
                    foreach (string key in new[] { "Offset", "Length", "Stretch", "FadeIn", "FadeOut", "Gain" })
                    {
                        var field = Id("AudioClip." + key); var row = (Grid)field.Parent!;
                        alignments.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().First(), row,
                            $"{language}-{theme}-{width}-{key}"));
                    }
                    await CapturePopupAsync(TopLevel.GetTopLevel(pane.AudioPropertiesContent!)!, output,
                        $"properties-{language}-{theme}-{width}");
                    CloseProperties();
                    await CaptureAsync(main, output, $"playlist-{language}-{theme}-{width}");
                }
            }
        }
        Json(output, "field-alignment", alignments);
        Select(); Click("Arrangement.AudioProperties"); await Task.Delay(80);
        var before = workflow.State.Snapshot;
        ((TextBox)Id("AudioClip.Stretch")).Text = "201"; Click("AudioClip.Apply"); await Refresh();
        Require(workflow.State.Snapshot == before && pane.AudioPropertiesContent!.IsVisible, "Invalid stretch altered the document.");
        await CapturePopupAsync(TopLevel.GetTopLevel(pane.AudioPropertiesContent!)!, output, "invalid-stretch"); CloseProperties();
        Select(); Click("Arrangement.AudioProperties"); await Task.Delay(80);
        ((TextBox)Id("AudioClip.Stretch")).Text = "50"; CloseProperties();
        Require(workflow.State.Snapshot == before, "Dismissed stretch committed.");

        string project = Path.Combine(output, "portable.seqvium"), wave = Path.Combine(output, "stretched-song.wav");
        Require(await workflow.SaveAsync(project), "Save As failed.");
        Require(await workflow.ExportSongAsync(ArrangementEditBasis.From(workflow.State), wave), "Song export failed.");
        using (var pcm = WavDecoder.Decode(File.ReadAllBytes(wave))) Require(pcm.Frames == 192000, "Export is not four seconds.");
        File.Delete(source);
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed.");
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Portable Open failed.");
        Require(workflow.State.Snapshot.State.AudioClips.Single().StretchRatio == 2 &&
            workflow.State.Document.SavedMediaDiagnostics.IsEmpty, "Portable stretch/media was lost.");
        workspace.Open(WorkspaceState.ArrangementId); main.Width = 1400; main.Height = 900; main.UpdateLayout(); await Refresh();

        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                var input = new NativePointer.DrumInput(main); await input.ActivateVisibleWindow();
                Select(); await input.Click(Id("Arrangement.AudioProperties")); await Task.Delay(100);
                await input.Exact(Id("AudioClip.Stretch"), "150"); await input.Click(Id("AudioClip.Apply")); await Refresh();
                Require(workflow.State.Snapshot.State.AudioClips.Single().StretchRatio == 1.5m, "Native stretch did not apply.");
                nativeResult = new { Result = "passed", Checks = "Native property action, numeric input and Apply; clip selection routed" };
                Require(await workflow.UndoAsync(), "Native edit Undo failed.");
            }
            catch (Exception error) { nativeResult = new { Result = "refused-or-failed", Reason = error.Message }; }
        }
        object physical = new { Result = "not-run" };
        if (audio)
        {
            try
            {
                var watch = Stopwatch.StartNew();
                Require(await workflow.PlayAsync(), workflow.State.Detail ?? "Physical playback failed.");
                string? device = workflow.State.Device;
                for (int i = 0; i < 300 && workflow.State.Execution?.Execution.Playing != true; i++)
                { await Task.Delay(20); await workflow.ObserveAsync(); }
                Require(workflow.State.Execution?.Execution.Playing == true, workflow.State.Detail ?? "Playback never started.");
                for (int i = 0; i < 240; i++)
                {
                    await Task.Delay(20); await workflow.ObserveAsync();
                    Require(workflow.State.StatusKey != "Workflow.DeviceFault" && workflow.State.Execution is not null,
                        workflow.State.StatusKey + ": " + workflow.State.Detail);
                    if (workflow.State.Execution?.Execution.Playing != true) break;
                }
                Require(workflow.State.Execution?.Execution.Playing == false && device is not null,
                    workflow.State.StatusKey + ": " + (workflow.State.Detail ?? "Physical output did not finish."));
                physical = new { Result = "passed", Device = device, ElapsedSeconds = watch.Elapsed.TotalSeconds,
                    Status = workflow.State.StatusKey, Execution = workflow.State.Execution, SubjectiveListening = false };
                await workflow.StopAsync();
            }
            catch (Exception error) { await workflow.StopAsync(true); physical = new { Result = "failed", Reason = error.Message }; }
        }
        Json(output, "stretch", new { RoutedImportProperties = true, SourceSeconds = 2, PlaybackSeconds = 4,
            LocalHistory = true, BothEdgeTrims = true, SaveAsOpen = true, ExportFrames = 192000, InvalidAndDismissed = true,
            Native = nativeResult, Physical = physical, SubjectiveListening = false });
    }

    private static void WriteStretchPhrase(string path)
    {
        const int rate = 48000, frames = rate * 2;
        using var file = File.Create(path); using var writer = new BinaryWriter(file);
        writer.Write("RIFF"u8); writer.Write(36 + frames * 8); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((ushort)3); writer.Write((ushort)2); writer.Write(rate); writer.Write(rate * 8);
        writer.Write((ushort)8); writer.Write((ushort)32); writer.Write("data"u8); writer.Write(frames * 8);
        double phase = 0; double[] pitches = [220, 261.625565, 329.627557, 440];
        for (int i = 0; i < frames; i++)
        {
            phase += Math.Tau * pitches[i * 4 / frames] / rate;
            double age = (i % (rate / 2)) / (double)rate;
            double envelope = Math.Min(1, age / .008) * Math.Exp(-2 * age);
            float sample = (float)(.12 * envelope * (Math.Sin(phase) + .2 * Math.Sin(phase * 2)));
            writer.Write(sample); writer.Write(sample * .8f);
        }
    }
}
