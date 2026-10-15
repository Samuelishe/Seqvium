// SPDX-License-Identifier: Apache-2.0
using System.Reflection;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task UiDensityAsync(MainWindow main, string output, string project)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        await Task.Delay(300);
        main.WindowState = WindowState.Normal; main.Width = 1920; main.Height = 1080;
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Review fixture failed to open.");
        await Task.Delay(100);
        var first = workflow.State.Snapshot.State.GraphAttachments.First();
        await workflow.SelectAsync(first.Id);
        Require(await workflow.EditMixerAsync(new(workflow.State.Document.LifecycleId, workflow.State.Generation),
            "Review mixer", e => e.InitializeMixer()) || workflow.State.Snapshot.State.Mixer is not null, "Mixer fixture failed.");
        await workflow.InitializeExtensionsAsync(Path.Combine(output, "config"));
        await workflow.RefreshLabGeneratorAsync();
        await workflow.GenerateLabAsync(SampleExplorationKind.Random, 42);
        await workflow.GenerateLabAsync(SampleExplorationKind.Random, 52);
        var shots = new List<string>();
        void Open(string method, params object[] args) => typeof(MainWindow).GetMethod(method,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(main, args);
        async Task Capture(string name)
        { await Task.Delay(85); main.UpdateLayout(); await CaptureAsync(main, output, name); shots.Add(name); }
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                main.Width = 1920; main.Height = 1080; await Task.Delay(120);
                string prefix = language + "-" + theme;
                foreach (var placement in workspace.State.Panes) workspace.State.Hide(placement.InstanceId);
                foreach (var id in new[] { WorkspaceState.BeatId, WorkspaceState.ArrangementId, WorkspaceState.MixerId }) workspace.Open(id);
                workspace.State.SetBounds(WorkspaceState.BeatId, new(0, 0, 1020, 330));
                workspace.State.SetBounds(WorkspaceState.ArrangementId, new(0, 345, 1020, 480));
                workspace.State.SetBounds(WorkspaceState.MixerId, new(1030, 0, 500, 825)); workspace.Commit();
                await Capture(prefix + "-music");
                foreach (var placement in workspace.State.Panes) workspace.State.Hide(placement.InstanceId);
                workspace.Open(WorkspaceState.PianoId); workspace.State.SetBounds(WorkspaceState.PianoId, new(0, 0, 1050, 720));
                workspace.Open(WorkspaceState.InspectorId); workspace.State.SetBounds(WorkspaceState.InspectorId, new(1065, 0, 330, 300));
                workspace.Open(WorkspaceState.AppearanceId); workspace.State.SetBounds(WorkspaceState.AppearanceId, new(1065, 315, 330, 180));
                workspace.Commit(); await Capture(prefix + "-piano-info");
                foreach (var placement in workspace.State.Panes) workspace.State.Hide(placement.InstanceId);
                Open("OpenGraph");
                var gain = workflow.State.Snapshot.State.Graphs.Single(g => g.Id == first.GraphId).Nodes.First(n => n.Type == GraphBuiltIns.Gain);
                Open("OpenNodeEditor", new NodeEditorTarget(workflow.State.Document.LifecycleId, first.Id, first.GraphId, gain.Id));
                workspace.State.SetBounds(WorkspaceState.GraphId, new(0, 0, 950, 650));
                workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(970, 0, 400, 340)); workspace.Commit(); workspace.FrameGraph();
                await Capture(prefix + "-graph-gain");
                foreach (var placement in workspace.State.Panes) workspace.State.Hide(placement.InstanceId);
                workspace.Open(WorkspaceState.BrowserId); workspace.Open(WorkspaceState.SampleLabId);
                var browser = main.GetVisualDescendants().OfType<BrowserPaneContent>().Single(); await browser.PendingCommand;
                var lab = main.GetVisualDescendants().OfType<SampleLabPaneContent>().Single(); await lab.Pending;
                workspace.State.SetBounds(WorkspaceState.SampleLabId, new(350, 0, 750, 580)); workspace.Commit();
                await Capture(prefix + "-browser-lab");
                main.Width = 1100; main.Height = 760; await Task.Delay(120);
                workspace.State.Hide(WorkspaceState.BrowserId);
                workspace.State.SetBounds(WorkspaceState.SampleLabId, new(5, 5, 470, 540)); workspace.Commit();
                await Capture(prefix + "-lab-reduced");
            }
        }
        Json(output, "ui-density", new { Panes = WorkspaceState.Definitions.Select(d => d.TypeId),
            Captures = shots, ActualWindows = true, NativeInput = false, PhysicalAudio = false,
            ProjectSourceOpenedWithoutWriting = project });
    }
}
