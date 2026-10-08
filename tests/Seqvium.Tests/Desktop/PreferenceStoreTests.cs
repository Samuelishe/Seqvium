// SPDX-License-Identifier: Apache-2.0

using Seqvium.Desktop.Presentation;
using Xunit;

namespace Seqvium.Tests;

public sealed class PreferenceStoreTests
{
    [Fact]
    public async Task MissingPreferencesUseDefaultsWithoutCreatingOrDeletingAnything()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("absent/prefs.json");
        Assert.Equal(new PreferenceLoad(new(), false), await new PreferenceStore(path).LoadAsync());
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)));
    }

    [Theory]
    [InlineData(HostLanguage.English, HostTheme.Dark)]
    [InlineData(HostLanguage.English, HostTheme.Light)]
    [InlineData(HostLanguage.Russian, HostTheme.Dark)]
    [InlineData(HostLanguage.Russian, HostTheme.Light)]
    public async Task RoundTripUsesStableVersionedValues(HostLanguage language, HostTheme theme)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("config/prefs.json");
        var preferences = new HostPreferences(language, theme);
        Assert.True(await new PreferenceStore(path).SaveAsync(preferences));
        Assert.Equal(new PreferenceLoad(preferences, false), await new PreferenceStore(path).LoadAsync());
        Assert.Contains("\"version\":1", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.DoesNotContain("Русский", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"version\":2}")]
    [InlineData("{\"version\":\"bad\"}")]
    [InlineData("{\"version\":1,\"language\":42,\"theme\":null}")]
    public async Task CorruptionFallsBackAndReadPreservesOriginalBytes(string bytes)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("prefs.json");
        await File.WriteAllTextAsync(path, bytes, TestContext.Current.CancellationToken);
        var store = new PreferenceStore(path);
        Assert.Equal(new PreferenceLoad(new(), true), await store.LoadAsync());
        Assert.Equal(bytes, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.True(await store.SaveAsync(new(HostLanguage.Russian, HostTheme.Light)));
        Assert.Equal(bytes, await File.ReadAllTextAsync(path + ".previous", TestContext.Current.CancellationToken));
        Assert.False((await store.LoadAsync()).UsedFallback);
    }

    [Fact]
    public async Task InvalidIndividualValuePreservesOtherValidPreference()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("prefs.json");
        await File.WriteAllTextAsync(path, "{\"version\":1,\"language\":\"ru\",\"theme\":\"unknown\"}",
            TestContext.Current.CancellationToken);
        Assert.Equal(new PreferenceLoad(new(HostLanguage.Russian, HostTheme.Dark), true),
            await new PreferenceStore(path).LoadAsync());
    }

    [Fact]
    public async Task OversizeFileIsPreservedAndSaveRefusedWithNoTemporaryLeak()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("prefs.json");
        var original = new string('x', 4097);
        await File.WriteAllTextAsync(path, original, TestContext.Current.CancellationToken);
        var store = new PreferenceStore(path);
        Assert.True((await store.LoadAsync()).UsedFallback);
        Assert.False(await store.SaveAsync(new()));
        Assert.Equal(original, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public async Task FailedWriteKeepsPreviousPreferencesAndUnrelatedUserContent()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("prefs.json");
        var store = new PreferenceStore(path);
        Assert.True(await store.SaveAsync(new()));
        Directory.CreateDirectory(path + ".previous"); // Deterministic backup obstruction, portable.
        var project = directory.File("project.seqvium");
        var media = directory.File("accepted.wav");
        var recovery = directory.File("recovery.snapshot");
        foreach (var file in new[] { project, media, recovery })
            await File.WriteAllTextAsync(file, "User work", TestContext.Current.CancellationToken);
        using var session = new ShellSession(store, await store.LoadAsync());
        await session.ToggleThemeAsync();
        Assert.Equal(HostTheme.Light, session.Preferences.Theme);
        Assert.True(session.HasNotice);
        Assert.Equal("Preference was applied for this session but could not be saved.", session.Notice);
        Assert.Equal(new HostPreferences(), (await store.LoadAsync()).Preferences);
        foreach (var file in new[] { project, media, recovery })
            Assert.Equal("User work", await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }
}
