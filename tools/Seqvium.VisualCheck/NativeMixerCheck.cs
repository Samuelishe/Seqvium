// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    /// <summary>Native authoring on an explicitly supplied independent project. Musical state is a read-only oracle.
    /// All edits, transport, Save/Open and export use Windows input and the production controls/dialogs.</summary>
    private static async Task NativeMixerAsync(MainWindow main, string output, string project, string phase)
    {
        var c = new MixerMusician(main, output, Path.GetFullPath(project));
        try
        {
            await c.Open();
        }
        catch
        {
            await CaptureAsync(main, output, "mixer-open-failure");
            Json(output, "open-failure-state", Field<DesktopWorkflow>(main, "_workflow").State);
            throw;
        }

        if (phase == "mix") await c.Mix();
        else if (phase == "play") await c.PlayExport();
        else if (phase == "review") await c.Review();
        else throw new ArgumentException("native-mixer phase must be mix, review or play.");
        await c.Save();
        Json(output, "native-mixer", new
        {
            NativeInput = true, InternalMusicalMutation = false, Phase = phase,
            Project = project, Mixer = c.State.Mixer, SubjectiveListening = false
        });
    }

    private sealed class MixerMusician(MainWindow main, string output, string project)
    {
        private readonly NativePointer.DrumInput _input = new(main);
        private DesktopWorkflow Workflow => Field<DesktopWorkflow>(main, "_workflow");
        private WorkspaceHost Workspace => Field<WorkspaceHost>(main, "_workspace");
        private MixerPaneContent Pane => main.GetVisualDescendants().OfType<MixerPaneContent>().Single();
        internal ProjectState State => Workflow.State.Snapshot.State;
        private ProjectState _original = null!;

        private Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Single(c =>
            c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);

        private Task Click(string id, bool allowOwnedDialog = false) =>
            _input.Click(Id(id), allowOwnedDialog: allowOwnedDialog);

        private static async Task Wait(Func<bool> condition, string failure)
        {
            for (int i = 0; i < 200 && !condition(); i++) await Task.Delay(30);
            Require(condition(), failure);
        }

        internal async Task Open()
        {
            await Task.Delay(300);
            main.Activate();
            await Task.Delay(150);
            if (main.WindowState != WindowState.Maximized) await Click("Window.Maximize");
            if (Field<ShellSession>(main, "_session").Preferences.Language != HostLanguage.English)
                await Click("Preference.Language");
            await Click("Workflow.Open", allowOwnedDialog: true);
            await _input.FileDialog(project);
            await Wait(() => Workflow.State.SavedPath == project && !Workflow.State.Busy, "Native Mixer Open failed.");
            _original = State;
            Require(Workflow.State.Execution is null, "Native Open started audio.");
            await ShowMixer();
        }

        private async Task ShowMixer()
        {
            foreach (var pane in Workspace.State.Panes
                         .Where(p => p.InstanceId != WorkspaceState.MixerId && p.Visibility == PaneVisibility.Visible)
                         .ToArray())
            {
                await Click(pane.InstanceId + ".open");
                await Click(pane.InstanceId + ".hide");
            }

            await Click(WorkspaceState.MixerId + ".open");
            var frame = (WorkspacePane)Id(WorkspaceState.MixerId);
            var bounds = Workspace.State.Bounds(WorkspaceState.MixerId);
            double targetX = 24, targetY = Math.Max(0, Math.Min(420, Workspace.State.Height - 408));
            if (bounds.X != targetX || bounds.Y != targetY)
                await _input.Drag(Field<Border>(frame, "_header"), new(90, 12),
                    new(90 + targetX - bounds.X, 12 + targetY - bounds.Y));
            bounds = Workspace.State.Bounds(WorkspaceState.MixerId);
            double targetWidth = Math.Min(1008, Workspace.State.Width - 24),
                targetHeight = Math.Min(408, Workspace.State.Height);
            if (Math.Abs(bounds.Width - targetWidth) > 3 || Math.Abs(bounds.Height - targetHeight) > 3)
                await _input.Drag(Field<Border>(frame, "_corner"), new(12, 12),
                    new(12 + targetWidth - bounds.Width, 12 + targetHeight - bounds.Height));
            await Task.Delay(150);
        }

        internal async Task Save()
        {
            await Click("Workflow.Save");
            await Wait(() => !Workflow.State.IsDirty && !Workflow.State.Busy,
                "Native Mixer Save failed: " + Workflow.State.Detail);
            Require(_original with { Mixer = State.Mixer } == State,
                "Mixer authoring altered the composition or local graphs.");
        }

        private async Task Visible(Control control)
        {
            var scroll = control.GetVisualAncestors().OfType<ScrollViewer>()
                .LastOrDefault();
            if (scroll is null) return;
            for (int n = 0; n < 40; n++)
            {
                var p = control.TranslatePoint(new(control.Bounds.Width / 2, control.Bounds.Height / 2), scroll)!.Value;
                if (p.X >= 12 && p.X < scroll.Viewport.Width - 12 && p.Y >= 0 &&
                    p.Y < scroll.Viewport.Height - 8) return;
                if (p.X < 12 || p.X >= scroll.Viewport.Width - 12)
                    await _input.Wheel(scroll, new(scroll.Bounds.Width / 2, 80), p.X < 12 ? -480 : 480, true);
                else await _input.Wheel(scroll, new(scroll.Bounds.Width / 2, 80), p.Y < 0 ? 240 : -240);
            }

            throw new InvalidOperationException("Native Mixer target cannot be reached.");
        }

        private MixerChannel Channel(string name) => State.Mixer!.Channels.Single(c => c.Name == name);

        private async Task Value(string id, decimal value)
        {
            var control = Id(id);
            if (control is TextBox text && decimal.TryParse(text.Text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var current) && current == value) return;
            await Visible(control);
            await CaptureAsync(main, output, "mixer-before-value");
            await _input.Exact(control, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await Wait(() => !Workflow.State.Busy, "Mixer exact entry stayed busy.");
            Require(!Workflow.State.StatusKey.EndsWith("Failed", StringComparison.Ordinal),
                "Mixer exact entry failed: " + Workflow.State.Detail);
        }

        private async Task Select(string name)
        {
            var control = Id("Mixer." + (name == "Master" ? "Master" : Channel(name).Id.ToString()) + ".Select");
            await Visible(control);
            await _input.Click(control);
            await Task.Delay(100);
        }

        private async Task Tab(int index)
        {
            var tabs = (TabControl)Id("Mixer.Detail.Tabs");
            var item = tabs.ContainerFromIndex(index) ?? throw new InvalidOperationException("Mixer tab missing.");
            await Visible(item);
            await _input.Click(item);
            await Task.Delay(100);
        }

        internal async Task Review()
        {
            Require(State.Mixer is not null, "Review requires a saved Mixer.");
            // Native workspace-only arrangement demonstrates simultaneous desktop tools at FullHD.
            await ArrangePane(WorkspaceState.ArrangementId, new(0, 0, Workspace.State.Width, 404));
            if (Workspace.State.Width >= 1412)
                await ArrangePane(WorkspaceState.BeatId, new(1040, 420, Workspace.State.Width - 1040, 408));
            await Click(WorkspaceState.MixerId + ".open");
            var shell = Field<ShellSession>(main, "_session");
            for (int i = 0; i < 2; i++)
            {
                await CaptureAsync(main, output, "mixer-bank-" + shell.Preferences.Theme);
                Require(
                    NativeScreen.Capture(main,
                        Path.Combine(output, "native-mixer-" + shell.Preferences.Theme + ".png")) ==
                    "passed: actual Windows screen pixels",
                    "Native Mixer screen capture failed.");
                await Select("Keys");
                await Tab(0);
                var alignment = new List<object>();
                foreach (var field in Field<ContentControl>(Pane, "_detail").GetVisualDescendants()
                             .OfType<MixerNumberEntry>())
                {
                    await Visible(field);
                    var parent = field.GetVisualAncestors().OfType<Grid>().First();
                    var label = parent.Children.OfType<TextBlock>().SingleOrDefault(t => t.IsVisible);
                    if (label is not null)
                        alignment.Add(FieldAlignment.Verify(field, label, Pane,
                            AutomationProperties.GetAutomationId(field)!));
                }

                Json(output, "mixer-fields-" + shell.Preferences.Theme, alignment);
                await CaptureAsync(main, output, "mixer-eq-" + shell.Preferences.Theme);
                await Tab(1);
                await CaptureAsync(main, output, "mixer-compressor-" + shell.Preferences.Theme);
                var disclosure = Field<ToggleButton>(Pane, "_disclosure");
                if (disclosure.IsChecked == true) await _input.Click(disclosure, new(100, 12));
                await Click("Preference.Theme");
            }

            await ReviewCompact();
            // Real fader, pan and curve gestures must share one canonical transaction and exact Undo.
            var original = State;
            var fader = (MixerFader)Id("Mixer." + Channel("Keys").Id + ".Fader");
            await Visible(fader);
            int history = Workflow.State.UndoCount;
            await _input.Drag(fader, new(fader.Bounds.Width * .36, fader.Bounds.Height * .55),
                new(fader.Bounds.Width * .36, fader.Bounds.Height * .4));
            await Wait(() => Workflow.State.UndoCount == history + 1, "Native fader did not commit once.");
            await Click("Workflow.Undo");
            await Wait(() => State == original, "Native fader Undo failed.");
            var dial = Id("Mixer." + Channel("Keys").Id + ".PanDial");
            await Visible(dial);
            history = Workflow.State.UndoCount;
            await _input.Drag(dial, new(17, 17), new(17, 7));
            await Wait(() => Workflow.State.UndoCount == history + 1, "Native pan did not commit once.");
            await Click("Workflow.Undo");
            await Wait(() => State == original, "Native pan Undo failed.");
            await Select("Keys");
            await Tab(0);
            var plot = (MixerResponsePlot)Id("Mixer.Detail.EqPlot");
            await Visible(plot);
            history = Workflow.State.UndoCount;
            var handle = plot.Handle(2);
            await _input.Drag(plot, handle, new(handle.X + 4, handle.Y - 5));
            await Wait(() => Workflow.State.UndoCount == history + 1, "Native EQ curve did not commit once.");
            await Click("Workflow.Undo");
            await Wait(() => State == original, "Native EQ curve Undo failed.");
            await Click("Workflow.Redo");
            await Wait(() => State != original, "Native EQ curve Redo failed.");
            await Click("Workflow.Undo");
            await Wait(() => State == original, "Native final curve Undo failed.");
            await Save();
            Json(output, "mixer-native-gestures",
                new { Fader = true, Pan = true, EqCurve = true, UndoRedo = true, MusicalContentRestored = true });
        }

        private async Task ArrangePane(string id, PaneBounds target)
        {
            await Click(id + ".open");
            var frame = (WorkspacePane)Id(id);
            var bounds = Workspace.State.Bounds(id);
            await _input.Drag(Field<Border>(frame, "_corner"), new(12, 12),
                new(12 + target.Width - bounds.Width, 12 + target.Height - bounds.Height));
            bounds = Workspace.State.Bounds(id);
            await _input.Drag(Field<Border>(frame, "_header"), new(90, 12),
                new(90 + target.X - bounds.X, 12 + target.Y - bounds.Y));
            Json(output, "layout-" + id, new { Target = target, Actual = Workspace.State.Bounds(id) });
        }

        private async Task ReviewCompact()
        {
            var shell = Field<ShellSession>(main, "_session");
            var frame = (WorkspacePane)Id(WorkspaceState.MixerId);
            var bounds = Workspace.State.Bounds(WorkspaceState.MixerId);
            await _input.Drag(Field<Border>(frame, "_corner"), new(12, 12),
                new(12 + 420 - bounds.Width, 12 + 300 - bounds.Height));
            for (int language = 0; language < 2; language++)
            {
                for (int theme = 0; theme < 2; theme++)
                {
                    await Select("Master");
                    var fields = new List<object>();
                    for (int tab = 0; tab < 3; tab++)
                    {
                        await Tab(tab);
                        foreach (var field in Field<ContentControl>(Pane, "_detail").GetVisualDescendants()
                                     .OfType<MixerNumberEntry>())
                        {
                            await Visible(field);
                            var parent = field.GetVisualAncestors().OfType<Grid>().First();
                            var label = parent.Children.OfType<TextBlock>().SingleOrDefault(t => t.IsVisible);
                            if (label is not null)
                                fields.Add(FieldAlignment.Verify(field, label, Pane,
                                    AutomationProperties.GetAutomationId(field)!));
                        }

                        await CaptureAsync(main, output,
                            $"mixer-compact-{shell.Preferences.Language}-{shell.Preferences.Theme}-{tab}");
                    }

                    Json(output, $"mixer-compact-fields-{shell.Preferences.Language}-{shell.Preferences.Theme}",
                        fields);
                    await Click("Preference.Theme");
                }

                await Click("Preference.Language");
            }

            var disclosure = Field<ToggleButton>(Pane, "_disclosure");
            if (disclosure.IsChecked == true) await _input.Click(disclosure, new(100, 12));
            bounds = Workspace.State.Bounds(WorkspaceState.MixerId);
            await _input.Drag(Field<Border>(frame, "_corner"), new(12, 12),
                new(12 + 1008 - bounds.Width, 12 + 408 - bounds.Height));
        }

        internal async Task Mix()
        {
            if (State.Mixer is null)
            {
                await Click("Mixer.Initialize");
                await Wait(() => State.Mixer is not null, "Native Mixer creation failed.");
            }

            Require(State.Mixer!.Channels.Length is 12 or 13, "Unexpected Neon Orchard source groups.");
            await Save();
            // Keep the composition and local balances, then establish modest independent gain/pan differences.
            foreach (var (name, level, pan) in new (string, decimal, decimal)[]
                     {
                         ("Kick", 0, 0), ("Snare", -1, 0), ("Clap", -4, .12m), ("Closed Hat", -2, -.32m),
                         ("Open Hat", -3, .28m), ("Rim", -2, -.22m), ("Tom", -1, .2m), ("Bass", 1, 0),
                         ("Keys", -1, -.14m), ("Pizz forte", 4, .27m), ("Flute", 2, -.3m), ("FX", -3, .38m)
                     })
            {
                var id = Channel(name).Id;
                await Value("Mixer." + id + ".Level", level);
                await Value("Mixer." + id + ".Pan", pan);
                Require(Channel(name).Settings.LevelDb == level && Channel(name).Settings.Pan == pan,
                    "Native balance value did not commit.");
                await Save();
            }

            await Value("Mixer.Master.Level", 3);
            if (!State.Mixer.ProtectOutput) await Click("Mixer.Ceiling");
            // Use genuinely executed EQ and dynamics, with measurements to follow on real playback.
            foreach (var (name, hp, low, midHz, mid, high) in new (string, decimal, decimal, decimal, decimal, decimal)
                         []
                         {
                             ("Bass", 30, 1, 350, -2, -1), ("Keys", 140, -2, 900, -1.5m, 1),
                             ("Pizz forte", 180, -2, 2500, 2, 1), ("Flute", 180, -1, 1200, 1, 1),
                             ("Snare", 100, -1, 2000, 1, 0)
                         })
            {
                await Select(name);
                await Tab(0);
                if (!Channel(name).Settings.Eq.Enabled) await Click("Mixer.Detail.EqEnable");
                await Value("Mixer.Detail.HP Hz", hp);
                await Value("Mixer.Detail.Low dB", low);
                await Value("Mixer.Detail.Mid Hz", midHz);
                await Value("Mixer.Detail.Mid dB", mid);
                await Value("Mixer.Detail.High dB", high);
                Require(Channel(name).Settings.Eq.Enabled && Channel(name).Settings.Eq.HighPassHz == hp,
                    "Native EQ failed.");
                await Save();
            }

            await Select("Bass");
            await Tab(1);
            if (!Channel("Bass").Settings.Compressor.Enabled) await Click("Mixer.Detail.CompEnable");
            await Value("Mixer.Detail.Threshold", -24);
            await Value("Mixer.Detail.Ratio", 3);
            await Value("Mixer.Detail.Attack ms", 18);
            await Value("Mixer.Detail.Release ms", 160);
            await Value("Mixer.Detail.Makeup dB", 1);
            await Save();
            // A drum bus is an actual aggregate path; no Playlist lane becomes a mixer route.
            var bus = State.Mixer.Channels.SingleOrDefault(c => c.Name == "Drums");
            if (bus is null)
            {
                if (State.Mixer.Channels.Length == 12)
                {
                    await Click("Mixer.AddBus");
                    await Wait(() => State.Mixer.Channels.Length == 13, "Native bus creation failed.");
                    await Save();
                }

                bus = State.Mixer.Channels.Last();
                await Select(bus.Name);
                await Tab(2);
                await _input.Exact(Id("Mixer.Detail.Name"), "Drums");
                await Wait(() => State.Mixer.Channels.Any(c => c.Name == "Drums"),
                    "Native bus rename failed: " + Workflow.State.Detail);
            }

            foreach (string name in new[] { "Kick", "Snare", "Clap", "Closed Hat", "Open Hat", "Rim", "Tom" })
            {
                await Select(name);
                await Tab(2);
                await _input.Select((ComboBox)Id("Mixer.Detail.Output"),
                    State.Mixer.Channels.Where(c => c.Id != Channel(name).Id).Select(c => c.Name).ToList()
                        .IndexOf("Drums") + 1);
                await Wait(() => Channel(name).Output == Channel("Drums").Id, "Native bus output failed.");
                await Save();
            }

            await Select("Drums");
            await Tab(1);
            if (!Channel("Drums").Settings.Compressor.Enabled) await Click("Mixer.Detail.CompEnable");
            await Value("Mixer.Detail.Threshold", -22);
            await Value("Mixer.Detail.Ratio", 2);
            await Value("Mixer.Detail.Attack ms", 25);
            await Value("Mixer.Detail.Release ms", 130);
            await Value("Mixer.Detail.Makeup dB", 1);
            await Save();
            await Review();
        }

        internal async Task PlayExport()
        {
            Require(State.Mixer is not null, "Play requires a saved Mixer.");
            var reopened = State.Mixer;
            await ShowMixer();
            await Click(WorkspaceState.ArrangementId + ".open");
            await Click("Arrangement.Start");
            await Click("Arrangement.Play");
            await Wait(() => Workflow.State.Execution?.Execution.Playing == true,
                "Native mixed Song start failed: " + Workflow.State.Detail);
            await Click(WorkspaceState.MixerId + ".open");
            int voices = 0;
            float peak = 0, reduction = 0;
            bool measured = false;
            for (int i = 0;
                 i < 2800 && Workflow.State.Execution?.Execution.Transport != SamplerTransportState.Ended;
                 i++)
            {
                await Task.Delay(30);
                Require(Workflow.State.StatusKey != "Workflow.DeviceFault" && Workflow.State.Execution?.Blocker is null,
                    "Mixed Song physical failure: " + Workflow.State.Detail);
                voices = Math.Max(voices, Workflow.State.Execution?.Execution.Voices ?? 0);
                foreach (var meter in Workflow.State.MixerMeters.Values)
                {
                    peak = Math.Max(peak, meter.PeakLeft);
                    reduction = Math.Max(reduction, meter.ReductionDb);
                    measured |= meter.Frames > 0 && meter.PeakLeft > 0;
                }

                if (i == 450) await CaptureAsync(main, output, "mixer-playing");
            }

            Require(Workflow.State.Execution?.Execution.Transport == SamplerTransportState.Ended && measured,
                "Physical mixed Song did not finish or supply real meters.");
            Require(State.Mixer == reopened, "Playback changed Mixer intent.");
            main.Activate();
            await Task.Delay(150);
            await Click("Workflow.Stop");
            await Click(WorkspaceState.ArrangementId + ".open");
            string wave = Path.Combine(Path.GetDirectoryName(project)!, "Neon Orchard - Mixed.wav");
            await Click("Arrangement.Export", allowOwnedDialog: true);
            await _input.FileDialog(wave);
            await Wait(() => File.Exists(wave) && !Workflow.State.Busy,
                "Native mixed Song export failed: " + Workflow.State.Detail);
            using var pcm = WavDecoder.Decode(File.ReadAllBytes(wave));
            double energyL = 0, energyR = 0, cross = 0, actualPeak = 0;
            long clipped = 0;
            for (int i = 0; i < pcm.Frames; i++)
            {
                double l = pcm.Sample(i, 0), r = pcm.Sample(i, 1);
                energyL += l * l;
                energyR += r * r;
                cross += l * r;
                actualPeak = Math.Max(actualPeak, Math.Max(Math.Abs(l), Math.Abs(r)));
                if (Math.Abs(l) >= .9999 || Math.Abs(r) >= .9999) clipped++;
            }

            Require(pcm.Frames == 3291429 && clipped == 0 && actualPeak > .1,
                "Mixed WAV duration/clipping/signal failed.");
            Json(output, "mixed-song", new
            {
                NativeSaveOpenExport = true, PhysicalWholeSong = true, SubjectiveListening = false,
                Workflow.State.Device, PeakVoices = voices, ObservedPeak = peak, ObservedReductionDb = reduction,
                MeasuredMeters = measured,
                Frames = pcm.Frames, Seconds = pcm.Frames / (double)pcm.SampleRate, Peak = actualPeak,
                ClippedFrames = clipped,
                RmsLeft = Math.Sqrt(energyL / pcm.Frames), RmsRight = Math.Sqrt(energyR / pcm.Frames),
                Correlation = cross / Math.Sqrt(energyL * energyR), Project = project, Wave = wave,
                WaveSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(wave)))
            });
        }
    }
}
