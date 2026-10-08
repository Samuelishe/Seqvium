// SPDX-License-Identifier: Apache-2.0

using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Xunit;

namespace Seqvium.Tests;

public sealed class HostPresentationTests
{
    [Fact]
    public void StableCatalogsHaveMatchingUsableKeysAndExplicitLanguages()
    {
        Assert.Equal(HostLocalizer.English.Keys.Order(), HostLocalizer.Russian.Keys.Order());
        var localizer = new HostLocalizer();
        foreach (var (key, value) in HostLocalizer.English)
        {
            Assert.Contains('.', key);
            Assert.Equal(value, localizer.Get(key, HostLanguage.English));
            Assert.Equal(HostLocalizer.Russian[key], localizer.Get(key, HostLanguage.Russian));
            Assert.False(string.IsNullOrWhiteSpace(value));
            Assert.False(string.IsNullOrWhiteSpace(HostLocalizer.Russian[key]));
        }

        Assert.Equal("Close", localizer.Get("Window.Close", HostLanguage.English));
        Assert.Equal("Закрыть", localizer.Get("Window.Close", HostLanguage.Russian));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Broken\nlabel")]
    public void MissingOrUnusableRussianEntryFallsBackPerResource(string? russian)
    {
        var ru = new Dictionary<string, string> { ["Window.Close"] = "Закрыть" };
        if (russian is not null) ru["Project.Tempo"] = russian;
        var localizer = new HostLocalizer(HostLocalizer.English, ru);
        Assert.Equal("Tempo", localizer.Get("Project.Tempo", HostLanguage.Russian));
        Assert.Equal("Закрыть", localizer.Get("Window.Close", HostLanguage.Russian));
    }

    [Theory]
    [InlineData("Window.Close", "Close")]
    [InlineData("Window.Minimize", "Minimize")]
    [InlineData("Window.Maximize", "Maximize")]
    [InlineData("Window.Restore", "Restore")]
    [InlineData("Project.Unnamed", "Untitled project")]
    [InlineData("Preference.Language", "Language")]
    [InlineData("Preference.Theme", "Theme")]
    [InlineData("Host.About", "About Seqvium")]
    public void EssentialHostFallbackDoesNotExposeUnexplainedKey(string key, string expected)
    {
        var localizer = new HostLocalizer(new Dictionary<string, string> { [key] = "\t" },
            new Dictionary<string, string>());
        Assert.Equal(expected, localizer.Get(key, HostLanguage.Russian));
        Assert.Equal("Текст недоступен", localizer.Get("Unknown.Label", HostLanguage.Russian));
    }

    [Theory]
    [InlineData(HostTheme.Dark)]
    [InlineData(HostTheme.Light)]
    public void SemanticPaletteProvidesReadableDistinctRoles(HostTheme theme)
    {
        var palette = HostPalette.For(theme);
        string[] required =
        [
            "Surface.Background", "Surface.Raised", "Surface.Workspace", "Text.Primary",
            "Text.Secondary", "Border.Default", "Focus.Active", "Accent.Action", "State.Unavailable", "Status.Warning"
        ];
        foreach (var role in required) Assert.Equal(255, Color.Parse(palette[role]).A);
        Assert.True(Contrast(palette["Text.Primary"], palette["Surface.Raised"]) >= 7);
        Assert.True(Contrast(palette["Text.Secondary"], palette["Surface.Workspace"]) >= 4.5);
        Assert.True(Contrast(palette["Status.Warning"], palette["Surface.Background"]) >= 4.5);
        Assert.NotEqual(palette["Surface.Background"], palette["Focus.Active"]);
        Assert.Equal(HostPalette.For(HostTheme.Dark).Keys.Order(), HostPalette.For(HostTheme.Light).Keys.Order());
    }

    [Fact]
    public void SharedMetricsKeepCompactReadableShellAndDevelopmentVersionIndependent()
    {
        Assert.All(HostMetrics.Values.Values, value => Assert.InRange(value, 2, 32));
        Assert.InRange(HostMetrics.Values["Type.Body"], 12, 14);
        Assert.Equal(30, HostMetrics.Values["Chrome.Height"]);
        Assert.Equal(30, HostMetrics.Values["Toolbar.Height"]);
        Assert.Equal(22, HostMetrics.Values["Status.Height"]);
        Assert.Equal(24, HostMetrics.Values["Control.Height"]);
        using var directory = new TemporaryDirectory();
        using var session = new ShellSession(new(directory.File("prefs.json")), new(new(), false));
        Assert.Equal("0.0.1-dev", session.Version);
        Assert.Equal("EN", session.LanguageCode);
    }

    [Fact]
    public async Task PreferencesAndPresentationPreserveEditedCanonicalAndPendingAuthority()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create(new(new Tempo(137.125m), new Meter(7, 8)));
        document.Edit("Name", edit => edit.RenameDocument("Моя melody"));
        using var session = new ShellSession(new(directory.File("prefs.json")), new(new(), false), document);
        var snapshot = document.Current;
        var lifecycle = document.LifecycleId;
        var generation = document.Generation;
        var notifications = 0;
        session.PropertyChanged += (_, _) => notifications++;
        await session.ToggleLanguageAsync();
        await session.ToggleThemeAsync();
        Assert.Equal(HostLanguage.Russian, session.Preferences.Language);
        Assert.Equal(HostTheme.Light, session.Preferences.Theme);
        Assert.Equal("Моя melody", session.ProjectName);
        Assert.Equal("137.125", session.Tempo);
        Assert.Equal("7/8", session.Meter);
        Assert.Same(snapshot, document.Current);
        Assert.Equal(lifecycle, document.LifecycleId);
        Assert.Equal(generation, document.Generation);
        Assert.Null(document.Saved);
        Assert.True(document.IsDirty);
        Assert.Equal(1, document.UndoCount);
        Assert.Equal(0, document.RedoCount);
        Assert.True(notifications > 0);
        Assert.False(session.HasNotice);
        await session.ToggleLanguageAsync();
        await session.ToggleThemeAsync();
        Assert.Equal(new HostPreferences(), session.Preferences);
    }

    [Fact]
    public async Task PreferenceChangesDoNotInvalidatePreparedMusicalWork()
    {
        using var directory = new TemporaryDirectory();
        using var session = new ShellSession(new(directory.File("prefs.json")), new(new(), false));
        using var import = ProjectMedia.BeginImport(session.Document, "Pending sound",
            ownedMediaDirectory: directory.File("owned"));
        using var source = new MemoryStream(WavFixtures.Create([0, 0.25f, 0]));
        await import.PrepareAsync(source, TestContext.Current.CancellationToken);
        var snapshot = session.Document.Current;
        await session.ToggleLanguageAsync();
        await session.ToggleThemeAsync();
        Assert.Same(snapshot, session.Document.Current);
        var accepted = import.Accept(TestContext.Current.CancellationToken);
        Assert.Contains(session.Document.Current.State.Resources, resource => resource.Id == accepted.ResourceId);
        Assert.Equal(1, session.Document.UndoCount);
    }

    [Fact]
    public async Task ShellCreatesAndClosesOnePristineUnnamedDocumentWithoutMedia()
    {
        using var directory = new TemporaryDirectory();
        var session = new ShellSession(new(directory.File("prefs.json")), new(new(), false));
        var document = session.Document;
        Assert.False(document.IsDirty);
        Assert.Null(document.Current.State.Name);
        Assert.Equal("120", session.Tempo);
        Assert.Equal("4/4", session.Meter);
        Assert.Equal("New · not saved", session.ProjectStatus);
        await session.ToggleLanguageAsync();
        Assert.Equal("Проект без названия", session.ProjectName);
        Assert.Null(document.Current.State.Name);
        Assert.Empty(document.Current.State.Resources);
        Assert.Empty(document.Current.State.Patterns);
        Assert.Equal(0, document.Generation);
        session.Dispose();
        session.Dispose();
        Assert.True(document.IsClosed);
        await Assert.ThrowsAsync<ObjectDisposedException>(session.ToggleThemeAsync);
    }

    [Fact]
    public async Task OverlappingCommandsPersistInOwnerOrderAndCloseEndsAuthority()
    {
        using var directory = new TemporaryDirectory();
        var store = new PreferenceStore(directory.File("prefs.json"));
        var session = new ShellSession(store, new(new(), false));
        var language = session.ToggleLanguageAsync();
        var theme = session.ToggleThemeAsync();
        session.Dispose();
        await Task.WhenAll(language, theme);
        Assert.Equal(new HostPreferences(HostLanguage.Russian, HostTheme.Light), (await store.LoadAsync()).Preferences);
        Assert.True(session.Document.IsClosed);
    }

    private static double Contrast(string first, string second)
    {
        static double Linear(byte component)
        {
            double c = component / 255d;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        static double Luminance(Color color) =>
            0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

        var a = Luminance(Color.Parse(first));
        var b = Luminance(Color.Parse(second));
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }
}
