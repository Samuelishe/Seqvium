// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    // Actual production controls and Windows input; only the held Pattern selection's output is substituted.
    private static async Task CrossStageInputAsync(MainWindow main, string output, bool native)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        var input = new NativePointer.DrumInput(main);
        var passed = new List<string>();
        void Passed(string text) { passed.Add(text); Json(output, "cross-stage-input", new { NativeInput = native, PhysicalAudio = false, Passed = passed }); }
        async Task Wait(Func<bool> ready, string message)
        { for (int i = 0; i < 300 && !ready(); i++) await Task.Delay(10); Require(ready(), message); }
        async Task Drain(BrowserPaneContent pane)
        { for (int i = 0; i < 10; i++) { var task = pane.PendingCommand; await task; await Task.Delay(20); if (task == pane.PendingCommand) return; } throw new InvalidOperationException("Browser did not settle."); }
        void Key(Control target, Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            target.Focus();
            if (native) input.Key(key == Avalonia.Input.Key.F6 ? (ushort)0x75 : (ushort)0x52,
                modifier: modifiers == KeyModifiers.Control ? (ushort)0x11 : modifiers == KeyModifiers.Shift ? (ushort)0x10 : (ushort)0);
            else target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers });
        }
        await Task.Delay(180);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900;
        if (native) { main.Activate(); await input.ActivateVisibleWindow(); }
        workspace.Open(WorkspaceState.BrowserId);
        workspace.State.Dock(WorkspaceState.BrowserId, PaneDock.Left); workspace.Commit();
        var browser = main.GetVisualDescendants().OfType<BrowserPaneContent>().Single();
        await Drain(browser);
        string sources = Path.Combine(output, "sources"); Directory.CreateDirectory(sources);
        WriteBrowserWav(Path.Combine(sources, "Kick.wav"), 65, .3);
        WriteBrowserWav(Path.Combine(sources, "Snare.wav"), 170, .18);
        browser.Navigate(sources); await Drain(browser);
        var list = Field<ListBox>(browser, "_list");
        WriteBrowserWav(Path.Combine(sources, "Added.wav"), 700, .1);
        Key(list, Avalonia.Input.Key.F6); await Task.Delay(80); await Drain(browser);
        Require(workspace.State.ActivePaneId != WorkspaceState.BrowserId && list.ItemCount == 2,
            "F6 must switch pane without refreshing Browser.");
        workspace.Open(WorkspaceState.BrowserId);
        Key(list, Avalonia.Input.Key.R, KeyModifiers.Control); await Task.Delay(80); await Drain(browser);
        Require(list.ItemCount == 3 && workspace.State.ActivePaneId == WorkspaceState.BrowserId,
            "Ctrl+R must refresh Browser without switching pane.");
        Passed("F6 switches pane; Ctrl+R refreshes folder through the main-window route");
        if (native)
        {
            int Index(string name) => list.Items.Cast<BrowserSource>().ToList().FindIndex(s => s.Name == name);
            await input.Click(list.ContainerFromIndex(Index("Kick.wav"))!); await Drain(browser);
            workspace.Open(WorkspaceState.BeatId);
            double left = workspace.State.Bounds(WorkspaceState.BrowserId).Width;
            workspace.State.SetBounds(WorkspaceState.BeatId, new(left + 8, 0, workspace.State.Width - left - 8, 340));
            workspace.Commit(); await Task.Delay(100);
            var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
            var row = list.ContainerFromIndex(Index("Snare.wav"))!;
            var destination = new Point(80, 90);
            Require(main.InputHitTest(rack.TranslatePoint(destination, main)!.Value) is Visual hit &&
                hit.GetVisualAncestors().Prepend(hit).Contains(rack), "Rack drop target is occluded.");
            await input.Drag(row, new(20, row.Bounds.Height / 2), rack.TranslatePoint(destination, row)!.Value);
            await Wait(() => workflow.State.Snapshot.State.Patterns.Length == 1 && !workflow.State.Busy,
                "Native Browser drag did not create a Rack use.");
            await rack.PendingCommand;
            Require(workflow.State.Snapshot.State.Sounds.Single().Name == "Snare", "Drag used the previously selected file.");
            Passed("native drag starts on Snare while Kick was selected; Rack accepts Snare");
            await workflow.UndoAsync(); await Task.Delay(80);
            int undo = workflow.State.UndoCount;
            var blank = new Point(30, list.Bounds.Height - 15);
            Require(list.InputHitTest(blank) is Visual blankHit &&
                !blankHit.GetVisualAncestors().Prepend(blankHit).OfType<ListBoxItem>().Any(), "No list whitespace for drag check.");
            await input.Drag(list, blank, rack.TranslatePoint(destination, list)!.Value);
            await Task.Delay(100);
            Require(workflow.State.UndoCount == undo && workflow.State.Snapshot.State.Patterns.IsEmpty,
                "Whitespace drag reused an unrelated selected sound.");
            Passed("native whitespace drag does not import the previously selected file");
        }

        // Hold real preparation across both presses. Avoid relying on this laptop being slow enough.
        workspace.State.Hide(WorkspaceState.BrowserId); workspace.Commit();
        var navigationRegion = main.FindControl<Border>("NavigationRegion")!;
        var previousNavigation = navigationRegion.Child;
        TaskCompletionSource entered = new(), release = new(); bool hold = false;
        RealtimeSampler? sampler = null; int opens = 0, editors = 0;
        await using var controlled = new DesktopWorkflow(ProjectDocument.Create(), () =>
        {
            opens++; sampler = new(48000, 2, 256);
            return new(new AuditBorrower(), sampler, "Deterministic input-check borrower", () => null);
        }, Path.Combine(output, "controlled-media"), (doc, renderer, target, owner) =>
            new(doc, renderer, target, owner, 8, 1, async cancellation =>
            { if (hold) { entered.TrySetResult(); await release.Task.WaitAsync(cancellation); } }, true));
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
        var packet = new float[512]; timer.Tick += (_, _) => sampler?.Process(packet); timer.Start();
        using var navigator = new PatternNavigator(shell, controlled, workspace, async (target, _, _) =>
        {
            Require(await controlled.SelectAsync(target), "Editor selection failed.");
            editors++; workspace.Open(WorkspaceState.PianoId);
        });
        try
        {
            Require(await controlled.ImportAsync(Path.Combine(sources, "Kick.wav")), "First fixture import failed.");
            var first = controlled.State.Selected!.Value;
            Require(await controlled.ImportAsync(Path.Combine(sources, "Snare.wav")), "Second fixture import failed.");
            var second = controlled.State.Selected!.Value;
            navigationRegion.Child = navigator; await Task.Delay(80);
            foreach (bool stop in new[] { false, true })
            {
                hold = false; Require(await controlled.SelectAsync(first), "Initial selection failed.");
                Require(await controlled.PlayAsync(), "Controlled Play failed.");
                await Wait(() => sampler!.ReadStatus().Playing, "Controlled renderer did not start.");
                entered = new(); release = new(); hold = true;
                var button = navigator.GetVisualDescendants().OfType<Button>().Single(b =>
                    AutomationProperties.GetAutomationId(b) == "Navigator.Pattern." + second);
                if (native) await input.Click(button); else button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(3)); await Task.Delay(50);
                Require(button.IsEnabled && controlled.State.Busy, "Clicked Pattern disabled before the second press.");
                var content = button.Content;
                if (stop) Require(await controlled.StopAsync(panic: true), "Panic failed.");
                if (native)
                {
                    // Windows double-click time includes this deliberately held first selection.
                    await input.Click(button);
                }
                else button.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent,
                    new PointerEventArgs(InputElement.PointerReleasedEvent, button,
                        new Pointer(77, PointerType.Mouse, true), main, button.TranslatePoint(new(15, 15), main)!.Value,
                        0, new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None)));
                Require(ReferenceEquals(content, button.Content), "Busy/selection replaced the native gesture target.");
                release.TrySetResult(); await navigator.PendingCommand;
                await Wait(() => editors == (stop ? 2 : 1), "Second press did not open the editor.");
                Require(controlled.State.Selected == second && opens == 1, "Editor selection reopened output or chose another Pattern.");
                if (stop) Require(!sampler!.ReadStatus().Playing, "Opening editor undid Panic.");
                else await Wait(() => sampler!.ReadStatus().AttachmentId == second && sampler.ReadStatus().Playing,
                    "Playing selection did not reach the chosen Pattern.");
                Passed(stop ? "held playing selection + double click + Panic opens editor and stays silent" :
                    "held playing selection + double click opens chosen editor on the same output");
                await controlled.StopAsync();
            }
        }
        finally { release.TrySetResult(); timer.Stop(); navigationRegion.Child = previousNavigation; }
        await CaptureAsync(main, output, "cross-stage-final");
    }

    private sealed class AuditBorrower : IAudioOutputLifetime
    {
        public bool Joined { get; private set; }
        public void Close() => Joined = true;
        public void Dispose() => Require(Joined, "Input-check output disposed before join.");
    }
}
