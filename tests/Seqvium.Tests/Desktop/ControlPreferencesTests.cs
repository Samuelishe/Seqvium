// SPDX-License-Identifier: Apache-2.0
using Avalonia.Input;
using Seqvium.Desktop.Presentation;
using Xunit;

namespace Seqvium.Tests;

public sealed class ControlPreferencesTests
{
    [Fact]
    public void OnlyEffectiveDifferencesFromDefaultsAreMarkedAsCustom()
    {
        var preferences = new ControlPreferences();
        Assert.All(HostCommands.All, command => Assert.False(preferences.IsCustomized(command.Id)));
        preferences = preferences.Assign("Browser", "Ctrl+H");
        Assert.True(preferences.IsCustomized("Browser"));
        preferences = preferences.Assign("Browser", "Ctrl+B");
        Assert.False(preferences.IsCustomized("Browser"));
        preferences = preferences.Assign("Browser", null);
        Assert.True(preferences.IsCustomized("Browser"));
    }

    [Fact]
    public void DefaultsResolveAndRemappingActuallyRemovesTheOldCommandBinding()
    {
        var preferences = new ControlPreferences();
        foreach (var command in HostCommands.All)
        {
            var gesture = KeyGesture.Parse(command.DefaultBinding);
            Assert.Equal(command.Id, HostCommands.Resolve(preferences, gesture.Key, gesture.KeyModifiers, false));
        }
        preferences = preferences.Assign("Browser", "Ctrl+H");
        Assert.Null(HostCommands.Resolve(preferences, Key.B, KeyModifiers.Control, false));
        Assert.Equal("Browser", HostCommands.Resolve(preferences, Key.H, KeyModifiers.Control, false));
    }

    [Fact]
    public void ConflictRequiresExplicitTransferAndResetMayAlsoConflict()
    {
        var preferences = new ControlPreferences();
        Assert.Equal("Save", preferences.Conflict("Open", "Ctrl+S"));
        Assert.Throws<InvalidOperationException>(() => preferences.Assign("Open", "Ctrl+S"));
        preferences = preferences.Assign("Open", "Ctrl+S", true);
        Assert.Null(preferences.Binding("Save"));
        Assert.Equal("Open", HostCommands.Resolve(preferences, Key.S, KeyModifiers.Control, false));
        Assert.Equal("Open", preferences.Conflict("Save", "Ctrl+S"));
        preferences = preferences.Assign("Save", "Ctrl+S", true);
        Assert.Null(preferences.Binding("Open"));
        Assert.Equal("Save", HostCommands.Resolve(preferences, Key.S, KeyModifiers.Control, false));
    }

    [Theory]
    [InlineData(Key.Space, KeyModifiers.Alt)]
    [InlineData(Key.F4, KeyModifiers.Alt)]
    [InlineData(Key.Tab, KeyModifiers.Control)]
    [InlineData(Key.F6, KeyModifiers.None)]
    [InlineData(Key.W, KeyModifiers.Control)]
    [InlineData(Key.C, KeyModifiers.Control)]
    [InlineData(Key.D, KeyModifiers.Control)]
    [InlineData(Key.R, KeyModifiers.Control)]
    [InlineData(Key.Left, KeyModifiers.Control)]
    [InlineData(Key.Space, KeyModifiers.None)]
    [InlineData(Key.A, KeyModifiers.None)]
    [InlineData(Key.Q, KeyModifiers.Control | KeyModifiers.Alt)]
    public void NativeTextAndEditorGesturesCannotBecomeGlobalBindings(Key key, KeyModifiers modifiers)
    {
        Assert.False(HostCommands.IsAssignable(key, modifiers));
        Assert.Throws<FormatException>(() => new ControlPreferences().Assign("Open", new KeyGesture(key, modifiers).ToString()));
    }

    [Theory]
    [InlineData("Ctrl+Z", Key.Z, KeyModifiers.Control)]
    [InlineData("Ctrl+Shift+Z", Key.Z, KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData("Ctrl+Shift+Y", Key.Y, KeyModifiers.Control | KeyModifiers.Shift)]
    public void TextEditingKeepsItsOwnUndoAndPlainKeysEvenWhenReassigned(string chord, Key key, KeyModifiers modifiers)
    {
        var preferences = new ControlPreferences().Assign("New", chord, true);
        Assert.Null(HostCommands.Resolve(preferences, key, modifiers, true));
        Assert.Equal("New", HostCommands.Resolve(preferences, key, modifiers, false));
    }

    [Fact]
    public void ReassignedDocumentUndoDoesNotStealTextHistoryAndTransportRemainsAvailable()
    {
        var preferences = new ControlPreferences().Assign("Undo", "Ctrl+H");
        Assert.Null(HostCommands.Resolve(preferences, Key.H, KeyModifiers.Control, true));
        Assert.Equal("Undo", HostCommands.Resolve(preferences, Key.H, KeyModifiers.Control, false));
        Assert.Equal("Play", HostCommands.Resolve(preferences, Key.F5, KeyModifiers.None, true));
    }

    [Fact]
    public async Task SeparateFileRoundTripsTransferClearAndBehaviorAndKeepsPreviousBytes()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("controls.json");
        var store = new ControlPreferencesStore(path);
        Assert.False((await store.LoadAsync()).UsedFallback);
        var controls = new ControlPreferences(true).Assign("Open", "Ctrl+S", true).Assign("Import", null);
        Assert.True(await store.SaveAsync(controls));
        string prior = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        Assert.True(await store.SaveAsync(controls with { AutoPreview = false }));
        Assert.Equal(prior, await File.ReadAllTextAsync(path + ".previous", TestContext.Current.CancellationToken));
        var load = await new ControlPreferencesStore(path).LoadAsync();
        Assert.False(load.UsedFallback); Assert.False(load.Preferences.AutoPreview);
        Assert.Null(load.Preferences.Binding("Save")); Assert.Null(load.Preferences.Binding("Import"));
        Assert.Equal("Open", HostCommands.Resolve(load.Preferences, Key.S, KeyModifiers.Control, false));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"version\":2}")]
    [InlineData("{\"version\":1,\"shortcuts\":{\"FutureCommand\":\"Ctrl+H\"}}")]
    [InlineData("{\"version\":1,\"shortcuts\":{\"Open\":\"Ctrl+S\"}}")]
    [InlineData("{\"version\":1,\"shortcuts\":{\"Open\":\"Alt+F4\"}}")]
    [InlineData("{\"version\":1,\"autoPreview\":12}")]
    public async Task UnsafeOriginalIsNeverOverwrittenEvenAfterSessionChanges(string bytes)
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("controls.json");
        await File.WriteAllTextAsync(path, bytes, TestContext.Current.CancellationToken);
        var store = new ControlPreferencesStore(path);
        Assert.True((await store.LoadAsync()).UsedFallback);
        Assert.False(await store.SaveAsync(new(true)));
        Assert.Equal(bytes, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExistingAppearanceAndMusicRemainIndependentAndLatestControlWriteIsJoined()
    {
        using var directory = new TemporaryDirectory();
        var appearance = new PreferenceStore(directory.File("preferences.json"));
        var original = new HostPreferences(HostLanguage.Russian, HostTheme.Light);
        Assert.True(await appearance.SaveAsync(original));
        using var session = new ShellSession(appearance, await appearance.LoadAsync());
        var snapshot = session.Document.Current;
        var first = session.SetControlsAsync(new(true));
        var last = session.SetControlsAsync(session.Controls.Assign("Browser", "Ctrl+H"));
        await session.PendingControls; await first; await last;
        var load = await new ControlPreferencesStore(directory.File("controls.json")).LoadAsync();
        Assert.True(load.Preferences.AutoPreview);
        Assert.Equal("Browser", HostCommands.Resolve(load.Preferences, Key.H, KeyModifiers.Control, false));
        Assert.Equal(original, (await appearance.LoadAsync()).Preferences);
        Assert.Same(snapshot, session.Document.Current); Assert.False(session.Document.IsDirty);
        Assert.Equal(0, session.Document.UndoCount);
    }
}
