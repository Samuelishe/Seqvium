// SPDX-License-Identifier: Apache-2.0
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Lifecycle;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length < 2) { Console.Error.WriteLine("catalog|matrix <absolute-output> / desktop|lease|noipc|stalled <identity> <absolute-output> / activate <identity>"); return 1; }
        if (args[0] == "activate")
        {
            using var instance = new DesktopInstance(DesktopLaunch.Isolated(args[1], Path.GetTempPath()).Identity);
            return instance.IsPrimary ? 4 : instance.RequestActivationAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult() ? 0 : 2;
        }
        if (args[0] is "lease" or "noipc" or "stalled")
        {
            string directory = Path.GetFullPath(args[2]); Directory.CreateDirectory(directory);
            using var instance = new DesktopInstance(DesktopLaunch.Isolated(args[1], directory).Identity);
            if (!instance.IsPrimary) return 4;
            if (args[0] != "noipc") instance.StartListening(args[0] == "stalled" ? ct => Task.Delay(Timeout.Infinite, ct) : _ =>
            { File.AppendAllText(Path.Combine(directory, "activation.txt"), "activate\n"); return Task.CompletedTask; });
            File.WriteAllText(Path.Combine(directory, "ready.txt"), Environment.ProcessId.ToString());
            using var stop = new ManualResetEventSlim();
            while (!File.Exists(Path.Combine(directory, "stop.txt"))) stop.Wait(50);
            return 0;
        }
        if (args[0] == "desktop")
        {
            string directory = Path.GetFullPath(args[2]); Directory.CreateDirectory(directory);
            return Seqvium.Desktop.Program.RunDesktop([], DesktopLaunch.Isolated(args[1], directory), app =>
            {
                app.MainWindowCreated += window =>
                {
                    File.AppendAllText(Path.Combine(directory, "documents.txt"), Environment.ProcessId + "\n");
                    window.Activated += (_, _) => File.AppendAllText(Path.Combine(directory, "activation.txt"), "activate\n");
                    window.WindowState = WindowState.Minimized;
                    File.WriteAllText(Path.Combine(directory, "ready.txt"), Environment.ProcessId.ToString());
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                    timer.Tick += (_, _) =>
                    {
                        File.WriteAllText(Path.Combine(directory, "window-state.txt"), window.WindowState.ToString());
                        if (!File.Exists(Path.Combine(directory, "stop.txt"))) return;
                        timer.Stop(); window.Close();
                    };
                    timer.Start();
                };
            }, key => { File.AppendAllText(Path.Combine(directory, "diagnostic.txt"), key + "\n"); });
        }
        string output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
        int result = 0;
        int desktopExit = Seqvium.Desktop.Program.RunDesktop([], DesktopLaunch.Isolated(Guid.NewGuid().ToString("N"), Path.Combine(output, "config")), app =>
        {
            app.MainWindowCreated += async main =>
            {
                try
                {
                    if (args[0] == "catalog") await ComponentCatalog.VerifyAsync(app, main, output);
                    else if (args[0] is "matrix" or "gestures") await MatrixAsync(app, main, output, args[0] == "matrix");
                    else throw new ArgumentException("Unknown scenario.");
                }
                catch (Exception error) { result = 1; File.WriteAllText(Path.Combine(output, "failure.txt"), error.ToString()); }
                finally
                {
                    var workflow = Field<DesktopWorkflow>(main, "_workflow");
                    if (workflow.State.IsDirty) await workflow.SaveAsync(Path.Combine(output, "final-fixture.seqvium"));
                    main.Close();
                }
            };
        }, key => { result = 1; File.WriteAllText(Path.Combine(output, "failure.txt"), key); });
        return desktopExit == 0 ? result : desktopExit;
    }

    internal static T Field<T>(object owner, string name) => (T)(owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner)
        ?? throw new InvalidOperationException("Missing harness field: " + name));
    private static void Invoke(object owner, string name, params object[] args) =>
        (owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException(name)).Invoke(owner, args);
    internal static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    internal static void Json(string output, string name, object value) => File.WriteAllText(Path.Combine(output, name + ".json"),
        JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));

    internal static async Task CaptureAsync(Window window, string output, string name)
    {
        window.UpdateLayout(); await Task.Delay(180);
        var size = new PixelSize((int)Math.Ceiling(window.Bounds.Width), (int)Math.Ceiling(window.Bounds.Height));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96)); bitmap.Render(window);
        bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
        Json(output, name + "-capture", new { Render = "actual Avalonia window", NativeScreen = NativeScreen.Capture(window, Path.Combine(output, name + ".screen.png")) });
    }

    internal static async Task CapturePopupAsync(Control control, string output, string name)
    {
        await Task.Delay(180); control.UpdateLayout();
        Require(control.Bounds.Width > 0 && control.Bounds.Height > 0, "Popup did not render: " + name);
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(control.Bounds.Width), (int)Math.Ceiling(control.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(control); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
        Json(output, name + "-capture", new { Render = "actual open Avalonia popup control", control.Bounds, NativeMouseClaim = false });
    }

    private static async Task MatrixAsync(App app, MainWindow main, string output, bool full)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var editor = Field<GraphEditorSession>(main, "_graphEditor");
        var nodeEditor = Field<NodeEditorSession>(main, "_nodeEditor");
        string source = Path.Combine(output, "fixture.wav"); WriteWav(source);
        Require(await workflow.ImportAsync(source), "Fixture import failed.");
        Require(await workflow.SaveAsync(Path.Combine(output, "fixture.seqvium")), "Fixture save failed.");
        Invoke(main, "OpenGraph");
        var attachment = workflow.State.Snapshot.State.GraphAttachments.Single();
        var graph = workflow.State.Snapshot.State.Graphs.Single();
        var gain = graph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
        var target = new NodeEditorTarget(workflow.State.Document.LifecycleId, attachment.Id, graph.Id, gain.Id);
        var alignment = new List<object>();
        var workflowControls = Field<WorkflowControls>(main, "_workflowControls");
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            editor.Select(new(GraphHitKind.Background));
            Field<GraphPaneContent>(main, "_graphPane").Refresh(); Invoke(main, "RefreshGainHierarchy");
            await CaptureAsync(main, output, $"{language}-{theme}-toolbar-gain");
            alignment.Add(FieldAlignment.Verify(Field<NumericUpDown>(workflowControls, "_gain"), Field<TextBlock>(workflowControls, "_gainLabel"), main, $"{language}-{theme}-toolbar"));
            editor.Select(new(GraphHitKind.Node, gain.Id));
            Field<GraphPaneContent>(main, "_graphPane").Refresh(); Invoke(main, "RefreshGainHierarchy");
            await CaptureAsync(main, output, $"{language}-{theme}-graph-quick-gain");
            var quickGain = Field<GraphPaneContent>(main, "_graphPane");
            alignment.Add(FieldAlignment.Verify(Field<TextBox>(quickGain, "_value"), Field<TextBlock>(quickGain, "_parameterName"), main, $"{language}-{theme}-graph-quick"));
        }
        editor.Select(new(GraphHitKind.Background)); Field<GraphPaneContent>(main, "_graphPane").Refresh();
        Invoke(main, "OpenNodeEditor", target);
        workspace.Open(WorkspaceState.InspectorId); workspace.Open(WorkspaceState.AppearanceId);
        var snapshot = workflow.State.Snapshot; int undo = workflow.State.Document.UndoCount;
        var rows = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            foreach (var geometry in full ? new[] { "normal", "reduced", "minimum", "maximized", "pane-minimum", "pane-expanded" } : new[] { "normal" })
            {
                main.WindowState = geometry == "maximized" ? WindowState.Maximized : WindowState.Normal;
                main.Width = geometry == "minimum" ? 640 : geometry == "reduced" ? 900 : 1100;
                main.Height = geometry == "minimum" ? 480 : geometry == "reduced" ? 600 : 750;
                await Task.Delay(180);
                var width = workspace.State.Width; var height = workspace.State.Height;
                workspace.State.SetBounds(WorkspaceState.GraphId, new(0, 0,
                    geometry == "pane-minimum" ? 300 : Math.Max(300, width * .63), geometry == "pane-minimum" ? 260 : Math.Max(260, height - 40)));
                workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(Math.Max(0, width - 360), 0,
                    geometry == "pane-minimum" ? 300 : 360, geometry == "pane-minimum" ? 210 : Math.Min(420, height)));
                workspace.State.SetBounds(WorkspaceState.InspectorId, new(Math.Max(0, width - 340), Math.Max(0, height - 245), geometry == "pane-minimum" ? 280 : 340, geometry == "pane-minimum" ? 210 : 245));
                workspace.State.SetBounds(WorkspaceState.AppearanceId, new(20, Math.Max(0, height - 190), 280, 170));
                var curve = main.GetVisualDescendants().OfType<Expander>().Single(c => Avalonia.Automation.AutomationProperties.GetAutomationId(c) == "NodeEditor.Curve");
                curve.IsExpanded = geometry == "pane-expanded";
                workspace.State.Activate(WorkspaceState.NodeEditorId);
                string name = $"{language}-{theme}-{geometry}";
                await CaptureAsync(main, output, name);
                var nodeContent = main.GetVisualDescendants().OfType<NodeEditorPaneContent>().Single();
                alignment.Add(FieldAlignment.Verify(Field<TextBox>(nodeContent, "_value"), Field<TextBlock>(nodeContent, "_linear"), main, name + "-node-editor"));
                Require(workflow.State.Snapshot == snapshot && workflow.State.Document.UndoCount == undo, "Theme/layout mutated music.");
                Require(nodeEditor.Target == target, "Theme/layout lost explicit node editor target.");
                rows.Add(new { name, main.Bounds, Panes = workspace.State.Capture(), MusicPreserved = true });
            }
            var dialog = new UnsavedProjectDialog(shell); var completion = dialog.ShowDialog<UnsavedDecision>(main);
            await CaptureAsync(dialog, output, $"{language}-{theme}-dialog"); dialog.Close(UnsavedDecision.Cancel); await completion;
        }
        main.WindowState = WindowState.Normal; main.Width = 1100; main.Height = 750;
        workspace.State.SetBounds(WorkspaceState.GraphId, new(10, 10, 650, 480));
        workspace.State.SetBounds(WorkspaceState.NodeEditorId, new(700, 10, 360, 320));
        workspace.State.Hide(WorkspaceState.InspectorId); workspace.State.Hide(WorkspaceState.AppearanceId);
        workspace.State.Activate(WorkspaceState.GraphId);
        await Task.Delay(250);
        var canvas = main.GetVisualDescendants().OfType<GraphCanvas>().Single();
        canvas.Fit(); await Task.Delay(180);
        var routed = await RoutedGestures.VerifyAsync(canvas, workspace, workflow, editor, nodeEditor, target);
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            app.ApplyTheme(theme);
            var accepted = app.Resources["Graph.Node"];
            var acceptedCorner = app.Resources["Control.Corner"];
            try
            {
                app.Resources["Graph.Node"] = new SolidColorBrush(Colors.Magenta);
                app.Resources["Control.Corner"] = new CornerRadius(7);
                await CaptureAsync(main, output, theme + "-graph-controlled");
                Require(HostVisual.Brush(canvas, theme, "Graph.Node") is ISolidColorBrush brush && brush.Color == Colors.Magenta,
                    "GraphCanvas did not resolve live semantic resources.");
                var realButtons = main.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("shell") && !b.Classes.Contains("chrome") && !b.Classes.Contains("paneChrome"));
                Require(realButtons.All(b => b.CornerRadius == new CornerRadius(7)), "Actual toolbar/pane buttons did not share the changed radius.");
                Require(main.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("chrome") || b.Classes.Contains("paneChrome")).All(b => b.CornerRadius == default), "Square chrome exception changed.");
            }
            finally { app.Resources["Graph.Node"] = accepted; app.Resources["Control.Corner"] = acceptedCorner; }
            await CaptureAsync(main, output, theme + "-graph-restored");
        }
        var native = await NativePointer.VerifyAsync(main, canvas, workspace, workflow, editor, nodeEditor, target);
        Json(output, "matrix", new { Rows = rows, FieldAlignment = alignment, RoutedGestures = routed, NativePointer = native, Screenshots = "Real Win32/Avalonia window renders at 96 DIP; native input separately guarded", AudioStarted = workflow.State.CanStop });
        Require(await workflow.SaveAsync(Path.Combine(output, "fixture.seqvium")), "Final isolated fixture save failed.");
    }

    private static void WriteWav(string path)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF"u8); writer.Write(36 + 128 * 2); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((short)1); writer.Write((short)1); writer.Write(48000); writer.Write(96000);
        writer.Write((short)2); writer.Write((short)16); writer.Write("data"u8); writer.Write(128 * 2);
        for (int i = 0; i < 128; i++) writer.Write((short)(Math.Sin(i * .1) * 100));
    }
}
