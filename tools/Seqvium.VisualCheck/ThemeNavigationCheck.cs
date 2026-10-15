// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task ThemeNavigationAsync(MainWindow main, string output, string project, bool native)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        await Task.Delay(250);
        main.WindowState = WindowState.Normal; main.Width = 1400; main.Height = 900;
        var input = new NativePointer.DrumInput(main);
        if (native) await input.ActivateVisibleWindow();
        Require(workflow.State.Occurrences.Length == 0 && !workflow.State.IsDirty, "Empty review did not start pristine.");
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                foreach (double width in new[] { 1400d, 900d })
                {
                    main.Width = width;
                    await CaptureAsync(main, output, $"empty-{language}-{theme}-{width}");
                }
            }
        }
        main.Width = 1400;
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Theme fixture open failed.");
        workspace.Open(WorkspaceState.BeatId);
        await Task.Delay(250);
        var snapshot = workflow.State.Snapshot;
        int undo = workflow.State.UndoCount;
        var navigator = main.GetVisualDescendants().OfType<PatternNavigator>().Single();
        var thumbnail = navigator.GetVisualDescendants().OfType<PatternThumbnail>().First();
        var navigationResults = await VerifyPatternNavigationAsync(main, workflow, workspace, navigator, input, output, native);
        var results = new List<object>();
        var reduced = new HashSet<(HostLanguage, HostTheme)>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light, HostTheme.Dark, HostTheme.Light })
            {
                if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
                await Task.Delay(180);
                string name = $"theme-{language}-{theme}-{results.Count}";
                // Read compositor pixels before an explicit RenderTargetBitmap render can refresh anything.
                string screen = NativeScreen.Capture(main, Path.Combine(output, name + "-before.screen.png"));
                if (native)
                {
                    Require(screen.StartsWith("passed:"), screen);
                    var at = thumbnail.TranslatePoint(new(10, 0), main)!.Value;
                    using var bitmap = new Bitmap(Path.Combine(output, name + "-before.screen.png"));
                    var pixel = ReadPixel(bitmap, (int)(at.X * main.RenderScaling), (int)(at.Y * main.RenderScaling));
                    Require(pixel == Color.Parse(HostPalette.For(theme)["Surface.Workspace"]),
                        $"Stale compositor thumbnail: {pixel} in {theme}.");
                }
                Require(ReferenceEquals(thumbnail, navigator.GetVisualDescendants().OfType<PatternThumbnail>().First()),
                    "Theme replaced the retained thumbnail.");
                await CaptureAsync(main, output, name);
                results.Add(new { language, theme, Screen = screen });
                if (reduced.Add((language, theme)))
                {
                    main.Width = 900; await Task.Delay(140);
                    if (!navigator.IsVisible)
                        main.GetVisualDescendants().OfType<Button>().Single(c =>
                            AutomationProperties.GetAutomationId(c) == "Navigator.Toggle")
                            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await CaptureAsync(main, output, $"populated-reduced-{language}-{theme}");
                    main.Width = 1400; await Task.Delay(180);
                }
            }
        }
        Require(workflow.State.Snapshot == snapshot && workflow.State.UndoCount == undo, "Presentation edited music.");
        Json(output, "theme-navigation", new { ActualWindow = true, NativeInput = native, Themes = results,
            RetainedThumbnails = true, MusicUnchanged = true, PhysicalAudio = false, Navigation = navigationResults });
    }

    private static async Task<object> VerifyPatternNavigationAsync(MainWindow main, DesktopWorkflow workflow,
        WorkspaceHost workspace, PatternNavigator navigator, NativePointer.DrumInput input, string output, bool native)
    {
        var host = main.GetVisualDescendants().OfType<PatternNavigationHost>().Single();
        var grip = main.GetVisualDescendants().OfType<Border>().Single(c => AutomationProperties.GetAutomationId(c) == "Navigator.Resize");
        var toggle = main.GetVisualDescendants().OfType<Button>().Single(c => AutomationProperties.GetAutomationId(c) == "Navigator.Toggle");
        var checks = new List<string>();
        var pointer = new Pointer(100178, PointerType.Mouse, true);
        var down = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
        var move = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other);
        var up = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);
        Point anchor = default;
        void Press()
        {
            anchor = grip.TranslatePoint(new(grip.Bounds.Width / 2, 70), main)!.Value;
            grip.RaiseEvent(new PointerPressedEventArgs(grip, pointer, main, anchor, 0, down, KeyModifiers.None));
            Require(host.HasGesture && pointer.Captured == grip, "Sidebar failed to capture its boundary.");
        }
        async Task Move(double delta)
        {
            grip.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, grip, pointer, main,
                anchor + new Vector(delta, 0), 0, move, KeyModifiers.None));
            await Task.Delay(60); main.UpdateLayout();
        }
        async Task Release(double delta)
        {
            grip.RaiseEvent(new PointerReleasedEventArgs(grip, pointer, main, anchor + new Vector(delta, 0),
                0, up, KeyModifiers.None, MouseButton.Left));
            await Task.Delay(100); main.UpdateLayout();
        }
        async Task Toggle()
        {
            if (native) await input.Click(toggle);
            else toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(100); main.UpdateLayout();
        }
        try
        {
            double start = host.Placement.Width;
            var first = navigator.GetVisualDescendants().OfType<PatternThumbnail>().First();
            double cardWidth = first.Bounds.Width;
            Press(); await Move(-25);
            Require(first.Bounds.Width < cardWidth && host.Placement.Width == start, "Sidebar did not shrink continuously without saving a draft.");
            double intermediate = first.Bounds.Width;
            await Move(-45);
            Require(first.Bounds.Width < intermediate && navigator.IsVisible, "Cards did not follow the boundary.");
            await Release(-45);
            Require(Math.Abs(host.Placement.Width - (start - 45)) < 1, "Sidebar release failed to remember width.");
            checks.Add("Continuous card/thumbnail resize and one stable placement on release");
            var normal = host.Placement;
            Press(); await Release(0);
            Require(host.Placement == normal, "A boundary click changed the preferred width.");
            Press(); await Move(PatternNavigationPlacement.MinimumWidth + 1 - normal.Width);
            Require(navigator.IsVisible, "Sidebar collapsed before its minimum.");
            await CaptureAsync(main, output, "navigation-minimum");
            await Move(PatternNavigationPlacement.CollapseWidth - 1 - normal.Width);
            Require(!navigator.IsVisible, "Sidebar stayed open below its minimum.");
            host.Cancel();
            Press(); await Move(-1000);
            Require(!navigator.IsVisible && host.HasGesture, "Below-threshold drag did not collapse while retaining capture.");
            // Reverse before release: the same gesture can recover from the collapsed preview.
            await Move(0); Require(navigator.IsVisible, "Cannot reverse an in-progress collapse.");
            await Move(-1000); await Release(-1000);
            Require(!navigator.IsVisible && host.Placement.Collapsed && host.Placement.Width == normal.Width,
                "Auto-collapse overwrote the last usable width.");
            await CaptureAsync(main, output, "navigation-collapsed");
            await Toggle();
            Require(navigator.IsVisible && host.Placement == normal, "Rail did not restore the last normal width.");
            checks.Add("Auto-collapse, in-gesture reversal, rail reopen at remembered width");
            Press(); await Move(80);
            main.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            Require(!host.HasGesture && host.Placement == normal, "Escape failed to roll back sidebar draft.");
            Press(); await Move(-30); pointer.Capture(null);
            Require(!host.HasGesture && host.Placement == normal, "Capture loss committed a draft.");
            Press(); await Move(40); main.Width = 1300; await Task.Delay(140);
            Require(!host.HasGesture && host.Placement == normal, "Window resize committed the sidebar draft.");
            main.Width = 1400; await Task.Delay(140);
            checks.Add("Escape and capture loss roll back without saving");
            Press(); await Move(2000); double capped = host.ColumnDefinitions[1].Width.Value;
            await Move(1990);
            Require(host.ColumnDefinitions[1].Width.Value < capped, "Resize reversal stuck at maximum.");
            host.Cancel();
            checks.Add("Maximum-width blocked-travel reversal");
            main.Width = 900; await Task.Delay(180);
            Require(!navigator.IsVisible && host.Placement == normal, "Compact auto-hide lost saved width.");
            await Toggle(); Require(navigator.IsVisible, "Rail cannot explicitly open navigation in a narrow window.");
            await CaptureAsync(main, output, "navigation-reduced");
            main.Width = 1400; await Task.Delay(180);
            workspace.Open(WorkspaceState.BrowserId); await Task.Delay(120);
            Require(!navigator.IsVisible, "Docked Browser did not reserve its space.");
            await Toggle();
            Require(navigator.IsVisible && workspace.State.Get(WorkspaceState.BrowserId).Visibility == PaneVisibility.Collapsed,
                "Explicit Pattern reopen did not release the Browser dock.");
            checks.Add("Compact auto-hide/explicit reopen and docked Browser handoff");
            if (native)
            {
                await input.Drag(grip, new(3, 70), new(83, 70));
                double wide = host.Placement.Width;
                Require(wide > normal.Width, "Native boundary drag did not expand navigation.");
                await CaptureAsync(main, output, "navigation-wide");
                await input.Drag(grip, new(3, 70), new(3 - wide, 70));
                Require(host.Placement.Collapsed && !navigator.IsVisible, "Native boundary drag did not auto-collapse.");
                await input.Click(toggle);
                Require(!host.Placement.Collapsed && host.Placement.Width == wide && navigator.IsVisible,
                    "Native rail click did not restore native resized width.");
                checks.Add("Windows mouse resize, auto-collapse and rail restoration");
            }
            grip.Focus();
            double keyboardWidth = host.Placement.Width;
            grip.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
            Require(host.Placement.Width == keyboardWidth + 16, "Keyboard resize failed.");
            grip.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Home });
            Require(host.Placement.Collapsed && toggle.IsKeyboardFocusWithin, "Keyboard collapse did not return focus.");
            await Task.Delay(400);
            string path = Path.Combine(Field<App>(main, "_app").ConfigurationDirectory, "workspace-layout.json");
            Require((await new WorkspaceLayoutStore(path).LoadAsync()).Layout?.Navigation == host.Placement,
                "Collapsed navigation preference was not persisted.");
            await Toggle();
            checks.Add("Keyboard width/collapse and focus return");
            await Task.Delay(400);
            var loaded = await new WorkspaceLayoutStore(path).LoadAsync();
            Require(!loaded.UsedFallback && loaded.Layout?.Navigation == host.Placement,
                "User layout did not persist the navigation placement.");
            checks.Add("Independent user-layout persistence");
            await CaptureAsync(main, output, "navigation-restored");
            return new { RoutedInput = true, NativeInput = native, Passed = checks, host.Placement };
        }
        finally { host.Cancel(); pointer.Capture(null); pointer.Dispose(); }
    }

    private static Color ReadPixel(Bitmap bitmap, int x, int y)
    {
        using var pixel = new WriteableBitmap(new(1, 1), new(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using var buffer = pixel.Lock();
        bitmap.CopyPixels(new PixelRect(x, y, 1, 1), buffer.Address, buffer.RowBytes, buffer.RowBytes);
        return Color.FromRgb(Marshal.ReadByte(buffer.Address, 2), Marshal.ReadByte(buffer.Address, 1), Marshal.ReadByte(buffer.Address));
    }
}
