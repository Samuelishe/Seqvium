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
        if (native)
        {
            main.Activate();
            await Task.Delay(180);
            await input.ActivateVisibleWindow();
        }
        Require(workflow.State.Occurrences.Length == 0 && !workflow.State.IsDirty, "Empty review did not start pristine.");
        workspace.Open(WorkspaceState.BrowserId);
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
        workspace.State.Collapse(WorkspaceState.BrowserId);
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Theme fixture open failed.");
        workspace.Open(WorkspaceState.BeatId);
        await Task.Delay(250);
        var snapshot = workflow.State.Snapshot;
        int undo = workflow.State.UndoCount;
        var navigator = main.GetVisualDescendants().OfType<PatternNavigator>().Single();
        var thumbnail = navigator.GetVisualDescendants().OfType<PatternThumbnail>().First();
        var navigationResults = await VerifyPatternNavigationAsync(main, workflow, workspace, navigator, input, output, native);
        var sidebarResults = await VerifyLeftPanelsAsync(main, workspace, navigator, input, output, native);
        Json(output, "left-panels", new { Navigation = navigationResults, Sidebars = sidebarResults,
            MusicUnchanged = workflow.State.Snapshot == snapshot && workflow.State.UndoCount == undo });
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
            RetainedThumbnails = true, MusicUnchanged = true, PhysicalAudio = false,
            Navigation = navigationResults, Sidebars = sidebarResults });
    }

    private static async Task<object> VerifyPatternNavigationAsync(MainWindow main, DesktopWorkflow workflow,
        WorkspaceHost workspace, PatternNavigator navigator, NativePointer.DrumInput input, string output, bool native)
    {
        var host = main.GetVisualDescendants().OfType<LeftPanelZone>().Single();
        var grip = main.GetVisualDescendants().OfType<Border>().Single(c => AutomationProperties.GetAutomationId(c) == "Navigator.Resize");
        var toggle = main.GetVisualDescendants().OfType<Button>().Single(c => AutomationProperties.GetAutomationId(c) == "Navigator.Toggle");
        int toggleClicks = 0;
        void CountToggle(object? sender, RoutedEventArgs args) => toggleClicks++;
        toggle.Click += CountToggle;
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
            int before = toggleClicks;
            if (native) await input.Click(toggle);
            else toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(100); main.UpdateLayout();
            Require(toggleClicks == before + 1, $"Toggle input delivered {toggleClicks - before} clicks instead of one.");
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
            Press(); await Move(SidePanelPlacement.MinimumWidth + 1 - normal.Width);
            Require(navigator.IsVisible, "Sidebar collapsed before its minimum.");
            await CaptureAsync(main, output, "navigation-minimum");
            await Move(SidePanelPlacement.CollapseWidth - 1 - normal.Width);
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
            Press(); await Move(90 - normal.Width);
            double clippedCard = first.Bounds.Width;
            Require(navigator.IsVisible && host.Patterns.Width < 110 && clippedCard > 100,
                "Pushed-in panel compressed its minimum-size content instead of clipping.");
            await CaptureAsync(main, output, "navigation-clipped");
            await Release(90 - normal.Width);
            Require(host.Placement.Width == normal.Width && host.Placement.VisibleWidth == 90,
                "Clipping overwrote the restore width.");
            await Toggle(); await Toggle();
            Require(host.Placement == normal, $"Reopening a partly clipped panel lost the usable width: {host.Placement}; expected {normal}.");
            if (native)
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    Press(); await Move(90 - normal.Width); await Release(90 - normal.Width);
                    await Toggle(); await Toggle();
                    Require(host.Placement == normal, $"Repeated clipped-panel reopening failed on attempt {attempt}: {host.Placement}.");
                }
            checks.Add("Minimum content width, clipped viewport, independent comfortable restore width");
            Press(); await Move(80);
            main.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
            Require(!host.HasGesture && host.Placement == normal, "Escape failed to roll back sidebar draft.");
            Press(); await Move(-30); pointer.Capture(null);
            Require(!host.HasGesture && host.Placement == normal, "Capture loss committed a draft.");
            Press(); await Move(40); main.Width = 1300; await Task.Delay(140);
            Require(!host.HasGesture && host.Placement == normal, "Window resize committed the sidebar draft.");
            main.Width = 1400; await Task.Delay(140);
            checks.Add("Escape and capture loss roll back without saving");
            Press(); await Move(-30);
            workspace.Open(WorkspaceState.BrowserId); await Task.Delay(100);
            Require(!host.HasGesture && host.Placement == normal,
                "Opening a left neighbour did not cancel the captured Pattern draft.");
            workspace.State.Collapse(WorkspaceState.BrowserId); await Task.Delay(100);
            checks.Add("Neighbour visibility change cancels anchored draft");
            Press(); await Move(2000); double capped = host.Patterns.Width;
            await Move(1990);
            Require(host.Patterns.Width < capped, "Resize reversal stuck at maximum.");
            host.Cancel();
            checks.Add("Maximum-width blocked-travel reversal");
            main.Width = 900; await Task.Delay(180);
            Require(navigator.IsVisible && host.Placement == normal, "Window pressure changed sidebar preference.");
            await CaptureAsync(main, output, "navigation-reduced");
            main.Width = 1400; await Task.Delay(180);
            workspace.Open(WorkspaceState.BrowserId); await Task.Delay(120);
            Require(navigator.IsVisible && host.Browser.ContentVisible, "Browser and Patterns cannot coexist.");
            await Toggle();
            Require(!navigator.IsVisible && workspace.State.Get(WorkspaceState.BrowserId).Visibility == PaneVisibility.Visible,
                "Pattern close changed Browser visibility.");
            await Toggle();
            workspace.State.Collapse(WorkspaceState.BrowserId); await Task.Delay(100);
            checks.Add("Compact auto-fit and independent Browser/Patterns coexistence");
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
            double keyboardRenderedWidth = host.Patterns.Width;
            grip.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right });
            Require(host.Placement.Width == keyboardWidth + 16 && host.Patterns.Width == keyboardRenderedWidth + 16,
                "Keyboard resize failed to update saved and rendered width.");
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
        finally { toggle.Click -= CountToggle; host.Cancel(); pointer.Capture(null); pointer.Dispose(); }
    }

    private static async Task<object> VerifyLeftPanelsAsync(MainWindow main, WorkspaceHost workspace,
        PatternNavigator navigator, NativePointer.DrumInput input, string output, bool native)
    {
        var zone = main.GetVisualDescendants().OfType<LeftPanelZone>().Single();
        Control Id(string id) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => AutomationProperties.GetAutomationId(c) == id);
        async Task Click(string id)
        {
            if (native) await input.Click(Id(id));
            else Id(id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(100); main.UpdateLayout();
        }
        async Task Resize(SidePanelHost panel, string id, double target)
        {
            var grip = Id(id);
            double delta = target - (panel.Width - panel.GripWidth);
            if (native) await input.Drag(grip, new(3, 80), new(3 + delta, 80));
            else
            {
                using var pointer = new Pointer(100179, PointerType.Mouse, true);
                var start = grip.TranslatePoint(new(3, 80), main)!.Value;
                grip.RaiseEvent(new PointerPressedEventArgs(grip, pointer, main, start, 0,
                    new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
                grip.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, grip, pointer, main,
                    start + new Vector(delta, 0), 0, new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
                grip.RaiseEvent(new PointerReleasedEventArgs(grip, pointer, main, start + new Vector(delta, 0), 0,
                    new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            }
            await Task.Delay(120); main.UpdateLayout();
        }
        void Ordered()
        {
            double browserLeft = zone.Browser.TranslatePoint(default, main)!.Value.X;
            double patternLeft = zone.Patterns.TranslatePoint(default, main)!.Value.X;
            double workspaceLeft = workspace.TranslatePoint(default, main)!.Value.X;
            Require(Math.Abs(patternLeft - browserLeft - zone.Browser.Bounds.Width) < 1 &&
                Math.Abs(workspaceLeft - patternLeft - zone.Patterns.Bounds.Width) < 1,
                "Sidebar ordering or workspace displacement has a gap/overlap.");
            Require(workspace.Bounds.Width >= 259, "Sidebar pressure consumed the musical workspace.");
        }
        await Click(WorkspaceState.BrowserId + ".open");
        Require(navigator.IsVisible && zone.Browser.ContentVisible, "Both panels failed to open.");
        var disclosure = (Expander)Id("Extension.Disclosure");
        var header = disclosure.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>()
            .Single(c => c.Name == "ExpanderHeader");
        if (native) await input.Click(header); else disclosure.IsExpanded = true;
        Require(disclosure.IsExpanded, "Browser disclosure did not open.");
        await CaptureAsync(main, output, "browser-disclosure-expanded");
        if (native) await input.Click(header); else disclosure.IsExpanded = false;
        Require(!disclosure.IsExpanded, "Browser disclosure did not close.");
        Ordered();
        double patternWidth = zone.Placement.Width;
        await Resize(zone.Browser, "Browser.Resize", 360);
        Require(zone.Browser.Placement.Width == 360 && zone.Placement.Width == patternWidth,
            "Browser resize changed Pattern preference.");
        Ordered();
        await Resize(zone.Browser, "Browser.Resize", 100);
        Require(zone.Browser.ContentVisible && zone.Browser.Placement.VisibleWidth == 100 &&
            zone.Browser.Placement.Width == 360 && navigator.IsVisible, "Browser clipping lost content/restore width.");
        Ordered(); await CaptureAsync(main, output, "browser-clipped-both-panels");
        await Resize(zone.Browser, "Browser.Resize", 0);
        Require(!zone.Browser.ContentVisible && navigator.IsVisible, "Browser collapse affected Patterns.");
        Ordered();
        await Click(WorkspaceState.BrowserId + ".open");
        Require(zone.Browser.Placement.Width == 360 && zone.Browser.Placement.VisibleWidth is null,
            "Browser icon did not restore comfortable width.");
        await Resize(zone.Patterns, "Navigator.Resize", 0);
        Require(!navigator.IsVisible && zone.Browser.ContentVisible, "Pattern collapse affected Browser.");
        Ordered(); await Click("Navigator.Toggle"); Ordered();
        foreach (double width in new[] { 900d, 640d, 1400d })
        {
            main.Width = width; main.Height = width == 640 ? 480 : 900; await Task.Delay(180);
            Require(navigator.IsVisible && zone.Browser.ContentVisible, "Small window hid an explicitly open panel.");
            Ordered();
            if (width == 900)
            {
                double neighbour = zone.Patterns.Width;
                double current = zone.Browser.Width - zone.Browser.GripWidth;
                await Resize(zone.Browser, "Browser.Resize", current - 30);
                Require(Math.Abs(zone.Patterns.Width - neighbour) < 1,
                    "Narrow-window boundary resize changed the right neighbour's width.");
                Ordered();
            }
            await CaptureAsync(main, output, "both-panels-" + width);
            await Click(WorkspaceState.BrowserId + ".open"); Ordered();
            Require(navigator.IsVisible && !zone.Browser.ContentVisible, "Browser rail close was not independent.");
            await Click(WorkspaceState.BrowserId + ".open"); Ordered();
        }
        await Task.Delay(400);
        var saved = await new WorkspaceLayoutStore(Path.Combine(Field<App>(main, "_app").ConfigurationDirectory,
            "workspace-layout.json")).LoadAsync();
        Require(!saved.UsedFallback && saved.Layout?.BrowserPanel == zone.Browser.Placement &&
            saved.Layout.Navigation == zone.Placement, "Both sidebar preferences did not persist independently.");
        return new { NativeInput = native, OrderedCoexistence = true, IndependentResizeCollapseRestore = true,
            SmallWindow = "640x480", SeparatePreferences = true };
    }

    private static Color ReadPixel(Bitmap bitmap, int x, int y)
    {
        using var pixel = new WriteableBitmap(new(1, 1), new(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        using var buffer = pixel.Lock();
        bitmap.CopyPixels(new PixelRect(x, y, 1, 1), buffer.Address, buffer.RowBytes, buffer.RowBytes);
        return Color.FromRgb(Marshal.ReadByte(buffer.Address, 2), Marshal.ReadByte(buffer.Address, 1), Marshal.ReadByte(buffer.Address));
    }
}
