// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    /// <summary>Read-only musical review with isolated preferences; real font resolution and GUI pixels.</summary>
    private static async Task IdentityAsync(MainWindow main, string output, string project)
    {
        await Task.Delay(150);
        var fonts = new List<object>();
        foreach (var (family, weight, file) in new[]
                 {
                     (HostMetrics.FontFamilyName, FontWeight.Normal, "Onest/Onest-Regular.ttf"),
                     (HostMetrics.FontFamilyName, FontWeight.SemiBold, "Onest/Onest-SemiBold.ttf"),
                     (HostMetrics.FontFamilyName, FontWeight.Black, "Onest/Onest-Black.ttf"),
                     (HostMetrics.MonospaceFamilyName, FontWeight.Normal, "JetBrainsMono/JetBrainsMono-Regular.ttf")
                 })
        {
            if (!FontManager.Current.TryGetGlyphTypeface(new Typeface(new FontFamily(family), weight: weight), out var glyph))
                throw new InvalidOperationException("Embedded font unavailable: " + file);
            Require(glyph.Weight == weight && glyph.FontSimulations == FontSimulations.None, "Synthetic or wrong font weight: " + file);
            foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя0123456789−")
                Require(glyph.CharacterToGlyphMap.TryGetGlyph(letter, out var id) && id != 0, file + ": missing glyph " + letter);
            using var asset = AssetLoader.Open(new Uri("avares://Seqvium.Desktop/Assets/Fonts/" + file));
            string embeddedHash = Convert.ToHexString(SHA256.HashData(asset));
            if (!glyph.PlatformTypeface.TryGetStream(out var actual))
                throw new InvalidOperationException("Resolved font stream unavailable: " + file);
            string actualHash;
            using (actual) actualHash = Convert.ToHexString(SHA256.HashData(actual));
            Require(actualHash == embeddedHash, "Resolved glyphs differ from the bundled font bytes: " + file);
            fonts.Add(new { Resource = family, glyph.FamilyName, Weight = (int)glyph.Weight,
                Simulations = glyph.FontSimulations.ToString(), EmbeddedSha256 = embeddedHash, ResolvedSha256 = actualHash });
        }
        Json(output, "fonts", fonts);
        Require(FontManager.Current.TryMatchCharacter('♯', FontStyle.Normal, FontWeight.Normal, FontStretch.Normal,
            new FontFamily(HostMetrics.FontFamilyName), null, out var music),
            "Sharp symbol fell back to a system font.");
        Require(music.GlyphTypeface.FamilyName == "Noto Sans Symbols", "Unexpected musical symbol fallback.");
        using (var bundledMusic = AssetLoader.Open(new Uri("avares://Seqvium.Desktop/Assets/Fonts/NotoSansSymbols/NotoSansSymbols-Music.ttf")))
        {
            if (!music.GlyphTypeface.PlatformTypeface.TryGetStream(out var resolvedMusic))
                throw new InvalidOperationException("Musical symbol stream unavailable.");
            using (resolvedMusic) Require(SHA256.HashData(bundledMusic).SequenceEqual(SHA256.HashData(resolvedMusic)),
                "Musical symbol bytes differ from bundled subset.");
        }
        Json(output, "music-symbols", new { music.GlyphTypeface.FamilyName, EmbeddedBytesMatch = true, Sharp = true });
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var shell = Field<ShellSession>(main, "_session");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Identity demo open failed.");
        Invoke(main, "ApplyOpenedWorkspace");
        var original = workflow.State.Snapshot;
        var melodic = workflow.State.Occurrences.First(c => c.Label.Contains("Orchard keys", StringComparison.Ordinal));
        Require(await workflow.SelectAsync(melodic.Id), "Melodic review target unavailable.");
        main.WindowState = WindowState.Normal;
        main.Width = 1920;
        main.Height = 1040;
        await Task.Delay(180);
        var observations = new List<object>();
        foreach (var language in new[] { HostLanguage.English, HostLanguage.Russian })
        foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
        {
            if (shell.Preferences.Language != language) await shell.ToggleLanguageAsync();
            if (shell.Preferences.Theme != theme) await shell.ToggleThemeAsync();
            foreach (var pane in workspace.State.Panes.Where(p => p.Visibility != PaneVisibility.Hidden).ToArray())
                workspace.State.Hide(pane.InstanceId);
            workspace.Open(WorkspaceState.ArrangementId);
            workspace.State.SetBounds(WorkspaceState.ArrangementId, new(0, 0, workspace.State.Width, 480));
            workspace.Open(WorkspaceState.BeatId);
            workspace.State.SetBounds(WorkspaceState.BeatId, new(0, 490, 980, Math.Max(320, workspace.State.Height - 490)));
            workspace.Open(WorkspaceState.MixerId);
            workspace.State.SetBounds(WorkspaceState.MixerId, new(990, 490, Math.Max(640, workspace.State.Width - 990), Math.Max(320, workspace.State.Height - 490)));
            await CaptureAsync(main, output, $"workspace-{language}-{theme}");
            var brand = main.FindControl<TextBlock>("BrandName")!;
            Require(brand.FontFamily.ToString().Contains("avares://", StringComparison.Ordinal) && brand.FontWeight == FontWeight.Black,
                "Chrome does not use embedded identity typography.");
            Require(main.Icon is not null, "Window icon missing.");
            var controls = Field<WorkflowControls>(main, "_workflowControls");
            observations.Add(FieldAlignment.Verify(Field<NumericUpDown>(controls, "_gain"), Field<TextBlock>(controls, "_gainLabel"), main, $"{language}-{theme}-toolbar"));
            var about = main.FindControl<Button>("AboutAction")!;
            about.Flyout!.ShowAt(about);
            await Task.Delay(100);
            var aboutBody = ((Flyout)about.Flyout).Content as Control ?? throw new InvalidOperationException("About content missing");
            await CapturePopupAsync(aboutBody.GetVisualAncestors().OfType<FlyoutPresenter>().Single(), output, $"about-{language}-{theme}");
            about.Flyout.Hide();
            workspace.Open(WorkspaceState.PianoId);
            workspace.State.SetBounds(WorkspaceState.PianoId, new(0, 0, workspace.State.Width, workspace.State.Height));
            var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
            piano.Refresh();
            piano.Canvas.Fit();
            await CaptureAsync(main, output, $"piano-{language}-{theme}");
            main.Width = 1400;
            main.Height = 900;
            await CaptureAsync(main, output, $"reduced-{language}-{theme}");
            main.Width = 1920;
            main.Height = 1040;
            await Task.Delay(100);
        }
        Require(workflow.State.Snapshot == original && workflow.State.UndoCount == 0 && !workflow.State.IsDirty && workflow.State.Execution is null,
            "Visual identity review changed music or started audio.");
        Json(output, "identity", new { Version = shell.Version, ReadOnlyProject = project, FontsEmbedded = true, NativeInput = false, PhysicalAudio = false,
            FullHd = true, FieldAlignment = observations, WindowHandle = main.TryGetPlatformHandle()?.Handle.ToInt64() });
    }
}
