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
    private static async Task ReverbAsync(MainWindow main, string output, bool native, bool audio)
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
                     c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == "Mixer.Detail.Reverb" + key); i++)
            { main.UpdateLayout(); await Task.Delay(20); }
            var field = (TextBox)Id("Mixer.Detail.Reverb" + key);
            Require(field.Focus(), "Reverb field could not receive focus.");
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
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation, "Reverb rhythm"), "Create failed.");
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
        ((TabControl)Id("Mixer.Detail.Tabs")).SelectedIndex = 4; main.UpdateLayout(); await Task.Delay(70);
        int undo = workflow.State.UndoCount;
        ((ToggleButton)Id("Mixer.Detail.ReverbEnable")).IsChecked = true;
        Click("Mixer.Detail.ReverbEnable"); await Refresh();
        await Value("Decay", "1.2"); await Value("Size", "65"); await Value("Damping", "60"); await Value("Mix", "40");
        MixerReverb Current() => workflow.State.Snapshot.State.Mixer!.Channels.Single(c => c.Id == channel).Settings.Reverb;
        Require(Current() == new MixerReverb { Enabled = true, DecaySeconds = 1.2m, Size = .65m, Damping = .6m, Mix = .4m } &&
            workflow.State.UndoCount == undo + 5, "Reverb controls did not commit five real controls.");
        Require(await workflow.UndoAsync() && Current().Mix == .2m, "Reverb Undo failed.");
        Require(await workflow.UndoAsync(true) && Current().Mix == .4m, "Reverb Redo failed."); await Refresh();
        var basis = workflow.State.Snapshot;
        await Value("Decay", "9"); Require(workflow.State.Snapshot == basis, "Invalid decay changed music.");
        ((TextBox)Id("Mixer.Detail.ReverbDecay")).RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        await Value("Decay", "7"); // a real edit, then restore through history
        Require(Current().DecaySeconds == 7, "Decay did not affect intent."); Require(await workflow.UndoAsync(), "Decay Undo failed."); await Refresh();
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
                    ((TabControl)Id("Mixer.Detail.Tabs")).SelectedIndex = 4; main.UpdateLayout();
                    foreach (string key in new[] { "Decay", "Size", "Damping", "Mix" })
                    {
                        var field = Id("Mixer.Detail.Reverb" + key); var row = (Grid)field.Parent!;
                        alignments.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().Single(), row,
                            $"{language}-{theme}-{width}-{key}"));
                    }
                    await CaptureAsync(main, output, $"reverb-{language}-{theme}-{width}");
                }
            }
        }
        Json(output, "field-alignment", alignments);
        main.Width = 1400; main.Height = 900;
        workspace.State.SetBounds(WorkspaceState.MixerId, new(0, 0, 850, 400)); workspace.Commit(); await Refresh();
        ((TabControl)Id("Mixer.Detail.Tabs")).SelectedIndex = 4; main.UpdateLayout();
        object nativeResult = new { Result = "not-run" };
        if (native)
        {
            try
            {
                main.Activate(); await Task.Delay(150);
                var input = new NativePointer.DrumInput(main); await input.ActivateVisibleWindow();
                await input.Exact(Id("Mixer.Detail.ReverbDecay"), "1.5"); await Refresh();
                Require(Current().DecaySeconds == 1.5m, "Native Decay entry failed.");
                await input.Click(Id("Mixer.Detail.ReverbEnable")); await Refresh(); Require(!Current().Enabled, "Native bypass failed.");
                await input.Click(Id("Mixer.Detail.ReverbEnable")); await Refresh(); Require(Current().Enabled, "Native enable failed.");
                nativeResult = new { Result = "passed", Scope = "Windows exact Decay, bypass and enable; setup/selection routed" };
                Require(await workflow.UndoAsync() && await workflow.UndoAsync() && await workflow.UndoAsync(), "Native edit restoration failed.");
                await Refresh();
            }
            catch (Exception error) { nativeResult = new { Result = "refused-or-failed", Reason = error.Message }; }
        }
        Require(await workflow.EditMixerAsync(new(workflow.State.Document.LifecycleId, workflow.State.Generation),
            "Song fixture", e => e.AddArrangementClip(workflow.State.Snapshot.State.Placements.Single().Id, new(0), 0, 1)), "Song fixture failed.");
        Require(await workflow.SetSongModeAsync(true), "Song mode failed.");
        string project = Path.Combine(output, "reverb.seqvium"), wave = Path.Combine(output, "reverb.wav");
        Require(await workflow.SaveAsync(project), "Reverb Save failed.");
        Require(await workflow.ExportSongAsync(ArrangementEditBasis.From(workflow.State), wave), "Reverb export failed.");
        long expectedEnd;
        using (var plan = ArrangementPreparation.Prepare(workflow.State.Document, 48000, 2, 1024))
        using (var pcm = WavDecoder.Decode(File.ReadAllBytes(wave)))
        {
            expectedEnd = plan.EndFrame;
            Require(pcm.Frames == expectedEnd && pcm.Frames > 96000, "Reverb export lost permitted tail.");
            double latePeak = 0;
            for (int i = 96000; i < pcm.Frames; i++) latePeak = Math.Max(latePeak, Math.Abs(pcm.Sample(i, 0)));
            Require(latePeak > .0001, "Reverb export has no late echo.");
        }
        Require(await workflow.NewAsync(UnsavedDecision.Discard), "New failed.");
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Open failed.");
        Require(Current().Enabled && Current().DecaySeconds == 1.2m && Current().Size == .65m && Current().Damping == .6m && Current().Mix == .4m,
            "Saved Reverb intent changed.");
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
                    "Live Reverb mix", e => e.SetMixerStrip(channel, workflow.State.Snapshot.State.Mixer!.Channels.Single().Settings with
                        { Reverb = Current() with { Mix = .6m } })), "Live Mix failed.");
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
        ReverbStudy(output);
        Json(output, "reverb", new { RoutedControls = true, History = true, SaveOpen = true, ExportFrames = expectedEnd,
            Native = nativeResult, Physical = physical, SubjectiveListening = false });
    }
    private static void ReverbStudy(string output)
    {
        const int rate = 48000, frames = rate * 9;
        var observations = new List<object>();
        double[] frequencies = [220, 261.6256, 329.6276, 293.6648];
        foreach (string source in new[] { "percussion", "phrase", "sustain" })
        foreach (string space in new[] { "dry", "default", "small-bright", "large-dark" })
        {
            var settings = space switch
            {
                "small-bright" => new MixerReverb { Enabled = true, Size = .1m, DecaySeconds = .6m, Damping = .1m, Mix = .3m },
                "large-dark" => new MixerReverb { Enabled = true, Size = .9m, DecaySeconds = 3, Damping = .8m, Mix = .3m },
                _ => new MixerReverb { Enabled = space != "dry" }
            };
            var prepared = MixerReverbDsp.Prepare(settings, rate);
            var dsp = new MixerReverbDsp(prepared, rate);
            var pcm = new float[frames * 2]; var random = new Random(9211);
            double peak = 0, early = 0, late = 0, stereoDifference = 0;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < frames; i++)
            {
                double time = (double)i / rate, input = 0;
                if (time < 2)
                {
                    double local = time % .5;
                    input = source switch
                    {
                        "percussion" => .18 * (random.NextDouble() * 2 - 1) * Math.Exp(-local * 60) +
                            .16 * Math.Sin(2 * Math.PI * 110 * local) * Math.Exp(-local * 32),
                        "phrase" => .14 * Math.Sin(2 * Math.PI * frequencies[(int)(time * 2)] * local) *
                            (1 - Math.Exp(-local * 200)) * Math.Exp(-local * 8),
                        _ => .1 * (Math.Sin(2 * Math.PI * 220 * time) + .3 * Math.Sin(2 * Math.PI * 330 * time)) *
                            Math.Min(1, time * 20) * Math.Min(1, (2 - time) * 20)
                    };
                }
                double left = input, right = input * .8;
                dsp.Process(ref left, ref right, prepared);
                Require(double.IsFinite(left) && double.IsFinite(right), "Study signal became nonfinite.");
                pcm[2 * i] = (float)left; pcm[2 * i + 1] = (float)right;
                peak = Math.Max(peak, Math.Max(Math.Abs(left), Math.Abs(right)));
                if (time < 2) early += left * left + right * right;
                else { late += left * left + right * right; stereoDifference += Math.Abs(left - right); }
            }
            timer.Stop(); Require(peak < 1, "Study signal overloaded.");
            string name = $"study-{source}-{space}.wav";
            using (var writer = new BinaryWriter(File.Create(Path.Combine(output, name))))
            {
                writer.Write("RIFF"u8); writer.Write((uint)(frames * 4 + 36)); writer.Write("WAVEfmt "u8);
                writer.Write(16u); writer.Write((ushort)1); writer.Write((ushort)2); writer.Write((uint)rate);
                writer.Write((uint)(rate * 4)); writer.Write((ushort)4); writer.Write((ushort)16);
                writer.Write("data"u8); writer.Write((uint)(frames * 4));
                foreach (float value in pcm) writer.Write((short)Math.Clamp((int)Math.Round(value * 32768), short.MinValue, short.MaxValue));
            }
            observations.Add(new { Source = source, Space = space, Settings = settings, File = name, Peak = peak,
                SourceEnergy = early, TailEnergy = late, TailStereoDifference = stereoDifference,
                RenderMilliseconds = timer.Elapsed.TotalMilliseconds, SubjectiveListening = false });
        }
        Json(output, "reverb-study", observations);
    }

}
