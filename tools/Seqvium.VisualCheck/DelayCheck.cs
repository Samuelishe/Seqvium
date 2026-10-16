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
    private static async Task DelayAsync(MainWindow main, string output, bool native, bool audio)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900;
        workspace.Open(WorkspaceState.MixerId); main.UpdateLayout(); await Task.Delay(100);
        MixerPaneContent Pane() => main.GetVisualDescendants().OfType<MixerPaneContent>().Single();
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c =>
            c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);
        void Click(string id) => ((Button)Id(id)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        async Task Refresh() { await Pane().PendingCommand; await workflow.ObserveAsync();
            Invoke(Pane(), "Refresh"); main.UpdateLayout(); await Task.Delay(60); }
        async Task Value(string key, string value)
        {
            for (int i = 0; i < 30 && !main.GetVisualDescendants().OfType<Control>().Any(c =>
                     c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == "Mixer.Detail.Delay" + key); i++)
            { main.UpdateLayout(); await Task.Delay(20); }
            var field = (TextBox)Id("Mixer.Detail.Delay" + key);
            Require(field.Focus(), "Delay field could not receive focus.");
            field.Text = value;
            field.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            for (int i = 0; i < 50 && field.IsFocused && workflow.State.Busy; i++) await Task.Delay(20);
            await Refresh();
        }
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width; main.Height = width == 1400 ? 900 : 600;
                    workspace.Open(WorkspaceState.MixerId);
                    foreach (var placement in workspace.State.Panes.Where(p => p.InstanceId != WorkspaceState.MixerId).ToArray())
                        workspace.State.Hide(placement.InstanceId);
                    workspace.State.SetBounds(WorkspaceState.MixerId, new(0, 0, width == 1400 ? 850 : 500, 400));
                    workspace.Commit(); main.UpdateLayout(); await Task.Delay(70);
                    await CaptureAsync(main, output, $"empty-{language}-{theme}-{width}");
                }
            }
        }
        main.Width = 1400; main.Height = 900;
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Delay rhythm"), "Create failed.");
        var target = workflow.State.Selected!.Value; var pattern = workflow.State.Snapshot.State.Patterns.Single();
        BeatEditBasis Basis() => new(workflow.State.Document.LifecycleId, workflow.State.Generation, target, pattern.Id);
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Kick"), "Kick failed.");
        pattern = workflow.State.Snapshot.State.Patterns.Single();
        var steps = new bool[16]; steps[0] = steps[12] = true;
        Require(await workflow.SetBeatStepsAsync(Basis(), pattern.Parts.Single().Id, steps), "Steps failed.");
        Require(await workflow.SetGainAsync(.15m), "Quiet fixture Gain failed.");
        workspace.Open(WorkspaceState.MixerId);
        foreach (var placement in workspace.State.Panes.Where(p => p.InstanceId != WorkspaceState.MixerId).ToArray())
            workspace.State.Hide(placement.InstanceId);
        workspace.State.SetBounds(WorkspaceState.MixerId, new(0, 0, 850, 400)); workspace.Commit(); await Refresh();
        Click("Mixer.Initialize"); await Refresh();
        var channel = workflow.State.Snapshot.State.Mixer!.Channels.Single().Id;
        Click("Mixer." + channel + ".Select"); await Refresh();
        ((TabControl)Id("Mixer.Detail.Tabs")).SelectedIndex = 3; main.UpdateLayout(); await Task.Delay(70);
        int undo = workflow.State.UndoCount;
        ((ToggleButton)Id("Mixer.Detail.DelayEnable")).IsChecked = true;
        Click("Mixer.Detail.DelayEnable"); await Refresh();
        await Value("Time", "125"); await Value("Feedback", "50"); await Value("Mix", "40");
        MixerDelay Current() => workflow.State.Snapshot.State.Mixer!.Channels.Single(c => c.Id == channel).Settings.Delay;
        Require(Current() == new MixerDelay { Enabled = true, TimeMs = 125, Feedback = .5m, Mix = .4m } &&
            workflow.State.UndoCount == undo + 4, "Delay controls did not commit four real parameters.");
        Require(await workflow.UndoAsync() && Current().Mix == .25m, "Delay Undo failed.");
        Require(await workflow.UndoAsync(true) && Current().Mix == .4m, "Delay Redo failed."); await Refresh();
        var basis = workflow.State.Snapshot;
        await Value("Feedback", "76"); Require(workflow.State.Snapshot == basis, "Invalid feedback changed music.");
        ((TextBox)Id("Mixer.Detail.DelayFeedback")).RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        await Value("Time", "999"); // a real edit, then restore through history
        Require(Current().TimeMs == 999, "Time did not affect intent."); Require(await workflow.UndoAsync(), "Time Undo failed."); await Refresh();
        var alignments = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (int width in new[] { 1400, 800 })
                {
                    main.Width = width; main.Height = width == 1400 ? 900 : 600;
                    workspace.State.SetBounds(WorkspaceState.MixerId, new(0, 0, width == 1400 ? 850 : 500, 400));
                    workspace.Commit(); await Refresh();
                    ((TabControl)Id("Mixer.Detail.Tabs")).SelectedIndex = 3; main.UpdateLayout();
                    foreach (string key in new[] { "Time", "Feedback", "Mix" })
                    {
                        var field = Id("Mixer.Detail.Delay" + key); var row = (Grid)field.Parent!;
                        alignments.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().Single(), row,
                            $"{language}-{theme}-{width}-{key}"));
                    }
                    await CaptureAsync(main, output, $"delay-{language}-{theme}-{width}");
                }
            }
        }
        Json(output, "field-alignment", alignments);
        main.Width = 1400; main.Height = 900;
        workspace.State.SetBounds(WorkspaceState.MixerId, new(0, 0, 850, 400)); workspace.Commit(); await Refresh();
        ((TabControl)Id("Mixer.Detail.Tabs")).SelectedIndex = 3; main.UpdateLayout();
        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                main.Activate(); await Task.Delay(150);
                var input = new NativePointer.DrumInput(main); await input.ActivateVisibleWindow();
                await input.Exact(Id("Mixer.Detail.DelayTime"), "150"); await Refresh();
                Require(Current().TimeMs == 150, "Native Time entry failed.");
                await input.Click(Id("Mixer.Detail.DelayEnable")); await Refresh(); Require(!Current().Enabled, "Native bypass failed.");
                await input.Click(Id("Mixer.Detail.DelayEnable")); await Refresh(); Require(Current().Enabled, "Native enable failed.");
                nativeResult = new { Result = "passed", Scope = "Windows exact Time, bypass and enable; setup/selection routed" };
                Require(await workflow.UndoAsync() && await workflow.UndoAsync() && await workflow.UndoAsync(), "Native edit restoration failed.");
                await Refresh();
            }
            catch (Exception error) { nativeResult = new { Result = "refused-or-failed", Reason = error.Message }; }
        }
        Require(await workflow.EditMixerAsync(new(workflow.State.Document.LifecycleId, workflow.State.Generation),
            "Song fixture", e => e.AddArrangementClip(workflow.State.Snapshot.State.Placements.Single().Id, new(0), 0, 1)), "Song fixture failed.");
        Require(await workflow.SetSongModeAsync(true), "Song mode failed.");
        string project = Path.Combine(output, "delay.seqvium"), wave = Path.Combine(output, "delay.wav");
        Require(await workflow.SaveAsync(project), "Delay Save failed.");
        Require(await workflow.ExportSongAsync(ArrangementEditBasis.From(workflow.State), wave), "Delay export failed.");
        long expectedEnd;
        using (var plan = ArrangementPreparation.Prepare(workflow.State.Document, 48000, 2, 1024))
        using (var pcm = WavDecoder.Decode(File.ReadAllBytes(wave)))
        {
            expectedEnd = plan.EndFrame;
            Require(pcm.Frames == expectedEnd && pcm.Frames > 96000, "Delay export lost permitted tail.");
            double latePeak = 0;
            for (int i = 96000; i < pcm.Frames; i++) latePeak = Math.Max(latePeak, Math.Abs(pcm.Sample(i, 0)));
            Require(latePeak > .0001, "Delay export has no late echo.");
        }
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed.");
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Open failed.");
        Require(Current().Enabled && Current().TimeMs == 125 && Current().Feedback == .5m && Current().Mix == .4m,
            "Saved Delay intent changed.");
        object physical = new { Result = "not-run" };
        if (audio)
        {
            try
            {
                Require(workflow.State.SongMode || await workflow.SetSongModeAsync(true), "Song mode failed.");
                Require(await workflow.PlayAsync(), workflow.State.Detail ?? "Physical Play failed.");
                for (int i = 0; i < 300 && workflow.State.Execution?.Execution.Playing != true; i++)
                { await Task.Delay(20); await workflow.ObserveAsync(); }
                Require(workflow.State.Execution?.Execution.Playing == true, "Physical playback did not start.");
                string? device = workflow.State.Device;
                var epoch = workflow.State.Execution!.Execution.Epoch;
                Require(await workflow.EditMixerAsync(new(workflow.State.Document.LifecycleId, workflow.State.Generation),
                    "Live Delay mix", e => e.SetMixerStrip(channel, workflow.State.Snapshot.State.Mixer!.Channels.Single().Settings with
                        { Delay = Current() with { Mix = .6m } })), "Live Mix failed.");
                for (int i = 0; i < 500; i++)
                {
                    await Task.Delay(20); await workflow.ObserveAsync();
                    Require(workflow.State.StatusKey != "Workflow.DeviceFault" && workflow.State.Execution is not null,
                        workflow.State.StatusKey + ": " + workflow.State.Detail);
                    if (!workflow.State.Execution!.Execution.Playing) break;
                }
                var ended = workflow.State.Execution!.Execution;
                Require(!ended.Playing && ended.Transport == SamplerTransportState.Ended && ended.Position == expectedEnd &&
                    ended.Voices == 0 && ended.Epoch == epoch && device is not null, "Physical tail/transport did not finish correctly.");
                physical = new { Result = "passed", Device = device, Execution = ended, LiveMix = true, SubjectiveListening = false };
                await workflow.StopAsync(); Require(await workflow.UndoAsync(), "Live Mix Undo failed.");
            }
            catch (Exception error) { await workflow.StopAsync(true); physical = new { Result = "failed", Reason = error.Message }; }
        }
        Json(output, "delay", new { RoutedControls = true, History = true, SaveOpen = true, ExportFrames = expectedEnd,
            Native = nativeResult, Physical = physical, SubjectiveListening = false });
    }
}
