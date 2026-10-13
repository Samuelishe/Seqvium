// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task DrumMusicAsync(MainWindow main, string output, string? deliveryRoot)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        main.WindowState = WindowState.Normal;
        main.Width = 1240;
        main.Height = 900;
        await Task.Delay(150);
        workspace.Open(WorkspaceState.BeatId);
        main.UpdateLayout();
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();

        Control Id(string id) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == id);

        void Click(string id) => ((Button)Id(id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Click("Beat.Create");
        await rack.PendingCommand;
        rack.Refresh();
        Click("Beat.AddDrum");
        await Task.Delay(50);
        var drumMenu = ((Button)Id("Beat.AddDrum")).ContextMenu!;
        ((MenuItem)drumMenu.Items[0]!).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        await rack.PendingCommand;
        rack.Refresh();
        await Task.Delay(100);
        main.UpdateLayout();
        var editor = main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single();
        editor.Refresh();
        var drum = main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single();
        Require(drum.IsEffectivelyVisible, "New Drum Voice did not open its full editor.");
        var part = BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single();
        Require(workflow.State.Snapshot.State.Resources.IsEmpty, "Drum Voice imported media.");
        workspace.State.Hide(WorkspaceState.NodeEditorId);
        workspace.Open(WorkspaceState.BeatId);
        rack.Refresh();
        main.UpdateLayout();
        foreach (int step in new[] { 0, 4, 7, 8, 12, 14, 15 })
        {
            var pad = (ToggleButton)Id($"Beat.Step.{part.Id}.{step}");
            ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(pad)!).Toggle();
            await rack.PendingCommand;
            rack.Refresh();
        }

        Require(BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single().Notes.Length == 7, "Rack rhythm lost hits.");
        workspace.Open(WorkspaceState.NodeEditorId);
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(100, 20, 740, 740));
        editor.Refresh();
        main.UpdateLayout();

        async Task Exact(string key, string value)
        {
            var field = (TextBox)Id("Drum." + key);
            int before = workflow.State.UndoCount;
            field.Focus();
            field.Text = value;
            field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await drum.PendingCommand;
            editor.Refresh();
            main.UpdateLayout();
            Require(workflow.State.UndoCount == before + 1,
                "Drum exact value did not commit once: " + key + " · " + workflow.State.Detail);
        }

        await Exact("decay", "620");
        await Exact("tone", "42");
        await Exact("noise", "8");
        await Exact("pitchAmount", "26");
        await Exact("pitchDecay", "90");
        using var pointer = new Pointer(91043, PointerType.Mouse, true);
        var dial = (GainDialControl)Id("Drum.Dial.tune");
        var point = dial.TranslatePoint(new(46, 46), main)!.Value;
        int beforeDial = workflow.State.UndoCount;
        dial.RaiseEvent(new PointerPressedEventArgs(dial, pointer, main, point, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None, 1));
        dial.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, dial, pointer, main,
            point + new Vector(0, -4), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
            KeyModifiers.Shift));
        dial.RaiseEvent(new PointerReleasedEventArgs(dial, pointer, main, point, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None,
            MouseButton.Left));
        await drum.PendingCommand;
        editor.Refresh();
        Require(workflow.State.UndoCount == beforeDial + 1, "Captured fine dial did not commit once.");
        Click("Drum.Amplitude");
        await Exact("attack", "1.4");
        await Exact("amplitudeCurve", "2.2");
        main.UpdateLayout();
        var native = await NativePointer.VerifyDrumAsync(main, (GainDialControl)Id("Drum.Dial.tone"),
            (DrumEnvelopePlot)Id("Drum.Envelope"), drum, workflow);
        Json(output, "native-drum", native);
        Click("Drum.Pitch");
        main.UpdateLayout();
        Json(output, "native-drum-pitch", await NativePointer.VerifyDrumAsync(main,
            (GainDialControl)Id("Drum.Dial.tone"),
            (DrumEnvelopePlot)Id("Drum.Envelope"), drum, workflow));
        Click("Drum.Amplitude");
        var aligned = new List<object>();
        foreach (var language in new[] { HostLanguage.Russian, HostLanguage.English })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            editor.Refresh();
            main.UpdateLayout();
            await drum.PendingWaveform;
            foreach (string key in new[] { "tune", "decay", "tone", "noise", "attack", "amplitudeCurve" })
            {
                var field = (TextBox)Id("Drum." + key);
                var unit = ((Grid)field.Parent!).Children.OfType<TextBlock>().Single();
                aligned.Add(FieldAlignment.Verify(field, unit, main, language + "/" + theme + "/" + key));
            }

            await CaptureAsync(main, output, $"drum-{language}-{theme}-amp");
            Click("Drum.Pitch");
            await drum.PendingWaveform;
            await CaptureAsync(main, output, $"drum-{language}-{theme}-pitch");
            main.Width = 640;
            main.Height = 480;
            main.UpdateLayout();
            workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(0, 0, 300, 210));
            await CaptureAsync(main, output, $"drum-{language}-{theme}-minimum");
            var localScroll = (ScrollViewer)Id("Drum.Scroll");
            foreach (string key in new[]
                         { "tune", "decay", "tone", "noise", "pitchAmount", "pitchDecay", "pitchCurve" })
            {
                var field = (TextBox)Id("Drum." + key);
                field.BringIntoView();
                await Task.Delay(20);
                main.UpdateLayout();
                var location = field.TranslatePoint(default, localScroll)!.Value;
                Require(location.X >= 0 && location.X + field.Bounds.Width <= localScroll.Bounds.Width &&
                        location.Y >= -1 && location.Y + field.Bounds.Height <= localScroll.Bounds.Height + 1,
                    "Compact Drum field is not reachable: " + key);
            }

            await CaptureAsync(main, output, $"drum-{language}-{theme}-minimum-depth");
            Click("Drum.Amplitude");
            foreach (string key in new[] { "attack", "amplitudeCurve" })
            {
                var field = (TextBox)Id("Drum." + key);
                field.BringIntoView();
                await Task.Delay(20);
                main.UpdateLayout();
                var location = field.TranslatePoint(default, localScroll)!.Value;
                Require(location.Y >= -1 && location.Y + field.Bounds.Height <= localScroll.Bounds.Height + 1,
                    "Compact amplitude field is not reachable: " + key);
            }

            localScroll.Offset = default;
            main.Width = 1240;
            main.Height = 900;
            await Task.Delay(120);
            main.UpdateLayout();
            workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(100, 20, 740, 740));
            Require(workspace.State.Bounds(WorkspaceState.NodeEditorId) is { Width: >= 700, Height: >= 700 },
                "Ordinary Drum geometry did not settle after minimum-window review.");
            Click("Drum.Amplitude");
        }

        // Actual waveform/parameters and shared musical state survive explicit history and project replacement.
        var savedValues = workflow.State.Snapshot.State.Sounds.Single().Parameters;
        Require(await workflow.UndoAsync() && await workflow.UndoAsync(true), "Drum Undo/Redo failed.");
        Require(await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State),
            workflow.State.Selected!.Value, default, 0, 2), "Drum song clip failed.");
        string project = Path.Combine(output, "Jade Pulse.seqvium");
        Require(await workflow.SaveAsync(project), "Drum Save failed.");
        var saved = workflow.State.Snapshot;
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Drum Open failed.");
        Require(workflow.State.Snapshot.Revision == saved.Revision && workflow.State.Execution is null &&
                savedValues.OrderBy(p => p.Key)
                    .SequenceEqual(workflow.State.Snapshot.State.Sounds.Single().Parameters.OrderBy(p => p.Key)),
            "Open changed Drum state or started output.");
        // Reopen the exact Source through the production editor action; target lifetime changes on Open.
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        typeof(MainWindow).GetMethod("OpenNodeEditor",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(main,
            [
                new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, attachment.GraphId,
                    attachment.Sources.Single().NodeId)
            ]);
        editor.Refresh();
        var beforeAudition = workflow.State.Snapshot;
        Click("Drum.Audition");
        await drum.PendingCommand;
        await Task.Delay(700);
        Require(workflow.State.Snapshot == beforeAudition, "Audition edited music.");
        await workflow.StopAsync();
        await workflow.SetSongModeAsync(false);
        Require(await workflow.PlayAsync(), "Drum Pattern playback failed: " + workflow.State.Detail);
        await Task.Delay(2400);
        await workflow.ObserveAsync();
        Require(
            workflow.State.Execution?.Execution is { Playing: true, Looping: true } &&
            workflow.State.Execution.Blocker is null,
            "Drum Pattern did not keep looping.");
        long epoch = (workflow.State.Execution ?? throw new InvalidOperationException("Missing Drum execution."))
            .Execution.Epoch;
        await Exact("tone", "58");
        for (int attempt = 0;
             attempt < 100 && workflow.State.Execution?.Execution.EquivalentCanonicalRevision !=
             workflow.State.Snapshot.Revision;
             attempt++)
        {
            await Task.Delay(20);
            await workflow.ObserveAsync();
        }

        Require(workflow.State.Execution?.Execution.Epoch == epoch, "Sound shaping restarted rhythm.");
        await workflow.StopAsync();
        await workflow.UndoAsync();
        Require(await workflow.SetSongModeAsync(true) && await workflow.PlayAsync(), "Drum Song failed.");
        int voices = 0;
        for (int attempt = 0;
             attempt < 160 && workflow.State.Execution?.Execution.Transport != SamplerTransportState.Ended;
             attempt++)
        {
            await Task.Delay(40);
            await workflow.ObserveAsync();
            voices = Math.Max(voices, workflow.State.Execution?.Execution.Voices ?? 0);
            Require(workflow.State.Execution?.Blocker is null, "Drum Song preparation blocked.");
        }

        Require(workflow.State.Execution?.Execution.Transport == SamplerTransportState.Ended && voices >= 2,
            "Finite drum Song or overlapping hits failed.");
        await workflow.StopAsync();
        string patternWave = Path.Combine(output, "Jade Pulse - Pattern.wav"),
            songWave = Path.Combine(output, "Jade Pulse - Song.wav");
        Require(await workflow.ExportWaveAsync(BeatSteps.Resolve(workflow.State).Basis!, patternWave),
            "Pattern export failed.");
        Require(await workflow.ExportSongAsync(ArrangementEditBasis.From(workflow.State), songWave),
            "Song export failed.");
        using var song = WavDecoder.Decode(File.ReadAllBytes(songWave));
        double peak = 0, energy = 0;
        int saturated = 0;
        for (int frame = 0; frame < song.Frames; frame++)
        {
            float value = song.Sample(frame, 0);
            peak = Math.Max(peak, Math.Abs(value));
            energy += value * value;
            if (Math.Abs(value) >= .9999) saturated++;
        }

        Require(peak > .05 && energy > 1 && saturated == 0 && song.Frames > 192000,
            "Export is silent, clipped or loses final drum tail.");
        string? delivered = null;
        if (deliveryRoot is not null)
        {
            delivered = Path.Combine(Path.GetFullPath(deliveryRoot),
                "Jade Pulse-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(delivered);
            Require(await workflow.SaveAsync(Path.Combine(delivered, "Jade Pulse.seqvium")),
                "Example delivery failed.");
            File.Copy(patternWave, Path.Combine(delivered, "Jade Pulse - Pattern.wav"));
            File.Copy(songWave, Path.Combine(delivered, "Jade Pulse - Song.wav"));
        }

        Json(output, "drum-music", new
        {
            RoutedGui = true, NativeInput = native, PhysicalOutput = true, SubjectiveListening = false,
            Parameters = savedValues, Hits = 7, SongFrames = song.Frames, Peak = peak, Energy = energy,
            Saturated = saturated,
            PeakVoices = voices, FieldAlignment = aligned, Delivered = delivered
        });
    }
}
