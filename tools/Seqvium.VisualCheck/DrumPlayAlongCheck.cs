// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task DrumPlayAlongAsync(MainWindow main, string output, bool native)
    {
        Console.WriteLine("Drum play-along deliberately plays physical audio; system levels are unchanged.");
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        main.WindowState = WindowState.Normal;
        main.Width = 1400;
        main.Height = 900;
        await Task.Delay(200);
        BeatEditBasis Basis() => BeatSteps.Resolve(workflow.State).Basis!;
        Require(await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
            "Play-along check"), "Fixture Pattern failed.");
        Require(await workflow.AddDrumVoiceAsync(Basis(), "Kick"), "Fixture Kick failed.");
        var kick = BeatSteps.Resolve(workflow.State).Pattern!.Parts.Single();
        foreach (int step in new[] { 0, 4, 8, 12 })
            Require(await workflow.ToggleBeatStepAsync(Basis(), kick.Id, step), "Fixture step failed.");
        Require(await workflow.SetGainAsync(.5m), "Fixture Gain failed.");
        workspace.Open(WorkspaceState.BeatId);
        workspace.State.SetBounds(WorkspaceState.BeatId, new(5, 5, 850, 350));
        main.UpdateLayout();
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        rack.Refresh();
        var driver = new ControlInputDriver(main, !native, output);
        if (native)
        {
            main.Activate();
            await Task.Delay(80);
        }

        await driver.ActivateVisibleWindow();

        Control Id(string id) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);

        await driver.Click(Id("Beat.AddDrum"));
        var menu = ((Button)Id("Beat.AddDrum")).ContextMenu!;
        await driver.Click(menu.Items.OfType<MenuItem>().Single(i =>
            AutomationProperties.GetAutomationId(i) == "Beat.AddDrum.Snare"));
        await rack.PendingCommand;
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(20, 5, 760, 740));
        workspace.Open(WorkspaceState.NodeEditorId);
        workspace.Commit();
        var pane = main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single();

        async Task Refresh()
        {
            await pane.PendingCommand;
            await workflow.ObserveAsync();
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single().Refresh();
            main.UpdateLayout();
        }

        async Task Until(Func<bool> predicate, string error)
        {
            var limit = DateTime.UtcNow.AddSeconds(5);
            while (!predicate() && DateTime.UtcNow < limit) await Task.Delay(5);
            Require(predicate(), error + " · " + workflow.State.StatusKey + " · " + workflow.State.Detail);
        }

        await Refresh();
        Require(((DrumEnvelopePlot)Id("Drum.Envelope")).Algorithm == DrumInstruments.All
            .Single(i => i.Key == "Snare").Algorithm, "Actual Rack menu did not open Snare.");
        var hits = new List<object>();
        var transitions = new List<string>();
        bool checkingAvailability = false;
        var controls = main.GetVisualDescendants().OfType<Control>().ToArray();

        void Availability(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (checkingAvailability && e.Property == Control.IsEnabledProperty)
                transitions.Add(AutomationProperties.GetAutomationId((Control)sender!) + ":" + e.NewValue);
        }

        foreach (var c in controls) c.PropertyChanged += Availability;
        try
        {
            await driver.Click(Id("Workflow.Play"));
            await Until(() => workflow.State.Execution?.Execution.Playing == true, "Pattern did not start");
            var device = Field<WorkflowOutput>(workflow, "_output");
            var sampler = device.Sampler;
            long epoch = sampler.TransportEpoch;
            long execution = sampler.ReadStatus().PreparedExecutionId;

            async Task Burst(int count, string phase)
            {
                var before = workflow.State.Snapshot;
                int undo = workflow.State.UndoCount;
                checkingAvailability = true;
                for (int i = 0; i < count; i++)
                {
                    long previous = sampler.DrumAuditionHits;
                    long position = sampler.RenderPosition;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    await driver.RapidClick(Id("Drum.Audition"));
                    await pane.PendingCommand;
                    await Until(() => sampler.DrumAuditionHits == previous + 1, "Hit did not reach output");
                    Require(sampler.TransportEpoch == epoch && sampler.RenderPosition > position &&
                            sampler.ReadStatus().Playing, "Drum interrupted musical transport.");
                    Require(ReferenceEquals(device, Field<WorkflowOutput>(workflow, "_output")) &&
                            typeof(DesktopWorkflow).GetField("_noteOutput", System.Reflection.BindingFlags.Instance |
                                                                            System.Reflection.BindingFlags.NonPublic)!
                                .GetValue(workflow) is null,
                        "Play-along opened another output.");
                    hits.Add(new
                    {
                        phase, i, Milliseconds = watch.Elapsed.TotalMilliseconds,
                        sampler.DrumAuditionHits, sampler.DrumAuditionVoices, sampler.RenderPosition,
                        sampler.TransportEpoch, sampler.DrumAuditionMinimumGain
                    });
                }

                checkingAvailability = false;
                Require(workflow.State.Snapshot == before && workflow.State.UndoCount == undo,
                    "Hits created canonical work or Undo.");
                long last = sampler.DrumAuditionHits;
                Json(output, "progress",
                    new { Hits = hits, AvailabilityTransitions = transitions, device.Description });
                await Task.Delay(300);
                Require(sampler.DrumAuditionHits == last, "A delayed hit backlog remained.");
                Require(device.ReadFailure() is null, "Physical output failed: " + device.ReadFailure());
            }

            await Burst(8, "Pattern");
            Require(sampler.ReadStatus().PreparedExecutionId == execution, "Hits replaced the musical plan.");
            await driver.Click(Id("Drum.StopAudition"));
            await Until(() => sampler.DrumAuditionVoices == 0, "Local Stop left a voice");
            Require(sampler.ReadStatus().Playing && sampler.TransportEpoch == epoch, "Local Stop cut the beat.");
            var dial = (GainDialControl)Id("Drum.Dial.tune");
            var center = new Point(dial.Bounds.Width / 2, dial.Bounds.Height / 2);
            long beforeEditHits = sampler.DrumAuditionHits;
            await driver.Drag(dial, center + new Vector(0, -23), center + new Vector(18, -13));
            await Refresh();
            Require(sampler.DrumAuditionHits == beforeEditHits, "A playing parameter commit added an unwanted hit.");
            Require(sampler.TransportEpoch == epoch, "Parameter edit restarted music.");
            await Burst(4, "Edited");
            await driver.Click(Id("Workflow.Undo"));
            await Refresh();
            await Burst(4, "Undo");
            var matrix = new List<object>();
            foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            foreach (bool reduced in new[] { false, true })
            {
                await shell.SetLanguageAsync(language);
                await shell.SetThemeAsync(theme);
                main.Width = reduced ? 1000 : 1920;
                main.Height = reduced ? 760 : 1080;
                main.UpdateLayout();
                workspace.State.SetBounds(WorkspaceState.NodeEditorId,
                    new(5, 5, reduced ? 360 : 760, reduced ? 520 : 740));
                workspace.Commit();
                await Refresh();
                await driver.Click(Id("Drum.Audition"));
                await pane.PendingCommand;
                Require(Id("Drum.Audition").IsEnabled && Id("Drum.StopAudition").IsEnabled,
                    "Playing compact editor lost its actions.");
                await CaptureAsync(main, output, $"playing-{language}-{theme}-{reduced}");
                matrix.Add(new
                    { language, theme, reduced, Window = main.Bounds, main.RenderScaling, Pane = pane.Bounds });
            }

            await driver.Click(Id("Workflow.Stop"));
            await Until(() => !sampler.ReadStatus().Playing && sampler.DrumAuditionVoices == 0,
                "Global Stop left musical or preview voices");
            await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State), Basis().Attachment,
                new(0), 0, 8);
            await workflow.SetSongModeAsync(true);
            await Refresh();
            await driver.Click(Id("Workflow.Play"));
            await Until(() => sampler.ReadStatus().Playing, "Song did not start");
            epoch = sampler.TransportEpoch;
            await Burst(8, "Song");
            await driver.RapidClick(Id("Drum.Audition"));
            await pane.PendingCommand;
            await Until(() => sampler.DrumAuditionVoices > 0, "Hide fixture had no preview voice");
            await driver.RapidClick(Id(WorkspaceState.NodeEditorId + ".hide"));
            await Until(() => sampler.DrumAuditionVoices == 0, "Editor hide left a preview voice");
            Require(sampler.ReadStatus().Playing && sampler.TransportEpoch == epoch,
                "Editor hide interrupted Song.");
            await driver.Click(Id("Workflow.Panic"));
            await Until(() => !sampler.ReadStatus().Playing && sampler.DrumAuditionVoices == 0,
                "Panic left a voice");
            Require(transitions.Count == 0, "Play blinked controls: " + string.Join(",", transitions));
            Require(device.ReadFailure() is null, "Physical output failed: " + device.ReadFailure());
            workspace.Open(WorkspaceState.NodeEditorId);
            await Refresh();
            await driver.Click(Id("Drum.Audition"));
            await pane.PendingCommand;
            await Until(() => sampler.DrumAuditionVoices > 0, "Close fixture had no preview voice");
            Require(await workflow.NewAsync(UnsavedDecision.Discard), "Isolated document replacement failed.");
            Require(device.Lifetime.Joined && sampler.IsTerminated && sampler.DrumAuditionVoices == 0,
                "Project replacement did not join/retire output and preview.");
            Json(output, "playalong", new
            {
                NativeWindowsInput = native, FixtureSetup = "isolated API Kick fragment",
                device.Description, Hits = hits, AvailabilityTransitions = transitions, Matrix = matrix,
                sampler.DrumAuditionOverflows, sampler.DrumAuditionSteals, sampler.DrumAuditionMinimumGain,
                PhysicalFailure = device.ReadFailure(), NativeEditorHide = true,
                ProjectReplacementJoined = true, SubjectiveListening = false
            });
        }
        finally
        {
            foreach (var c in controls) c.PropertyChanged -= Availability;
        }
    }
}
