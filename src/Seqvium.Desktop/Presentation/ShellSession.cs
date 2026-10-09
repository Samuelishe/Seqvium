// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Seqvium.Core;

namespace Seqvium.Desktop.Presentation;

/// <summary>GUI projection and preferences. Standalone shells own their document;
/// the integrated workflow owns mutations/lifetime. Never owns audio/device sessions or pane layout.</summary>
public sealed class ShellSession : INotifyPropertyChanged, IDisposable
{
    private readonly HostLocalizer _localizer;
    private readonly PreferenceStore _store;
    private string? _noticeKey;
    private string? _layoutNoticeKey;
    private string _audioStatusKey = "Audio.Idle";
    public string AudioStatus => this[_audioStatusKey];
    internal void ObserveAudio(string key)
    {
        if (_audioStatusKey == key) return;
        _audioStatusKey = key; Refresh();
    }

    public ProjectDocument Document { get; private set; }
    internal bool HasWorkflowOwner { get; set; }
    private ProjectSnapshot? _observedSnapshot;
    private string? _observedSavedPath;
    private bool _observedDirty, _observedHasSaved;
    internal ProjectSnapshot Snapshot => _observedSnapshot ?? Document.Current;
    private string? SavedPath => _observedSnapshot is null ? Document.SavedPath : _observedSavedPath;
    internal void ObserveDocument(ProjectDocument document, ProjectSnapshot snapshot, string? savedPath,
        bool dirty, bool hasSaved)
    {
        Document = document;
        _observedSnapshot = snapshot; _observedSavedPath = savedPath;
        _observedDirty = dirty; _observedHasSaved = hasSaved;
        Refresh();
    }
    public HostPreferences Preferences { get; private set; }
    public bool IsClosed { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public string this[string key] => _localizer.Get(key, Preferences.Language);
    public string ProjectName => Snapshot.State.Name ??
        (SavedPath is { } path ? Path.GetFileNameWithoutExtension(path) : this["Project.Unnamed"]);

    public string ProjectStatus =>
        this[(_observedSnapshot is null ? Document.IsDirty : _observedDirty) ? "Project.Modified" :
            (_observedSnapshot is null ? Document.Saved is not null : _observedHasSaved) ? "Project.Saved" : "Project.Pristine"];

    public string Tempo =>
        Snapshot.State.Settings.Tempo.BeatsPerMinute.ToString("0.######", CultureInfo.InvariantCulture);

    public string Meter =>
        FormattableString.Invariant(
            $"{Snapshot.State.Settings.Meter.Numerator}/{Snapshot.State.Settings.Meter.Denominator}");

    public string LanguageLabel =>
        $"{this["Preference.Language"]}: {(Preferences.Language == HostLanguage.Russian ? "Русский" : "English")}";

    public string ThemeLabel =>
        $"{this["Preference.Theme"]}: {this[Preferences.Theme == HostTheme.Dark ? "Preference.Dark" : "Preference.Light"]}";

    public string? Notice => string.Join(" · ", new[] { _noticeKey, _layoutNoticeKey }
        .Where(key => key is not null).Select(key => this[key!]));

    public bool HasNotice => _noticeKey is not null || _layoutNoticeKey is not null;

    public void SetLayoutNotice(string? key)
    {
        if (IsClosed) return;
        _layoutNoticeKey = key;
        Refresh();
    }

    public string LanguageCode => Preferences.Language == HostLanguage.Russian ? "RU" : "EN";
    public string ThemeName => this[Preferences.Theme == HostTheme.Dark ? "Preference.Dark" : "Preference.Light"];

    public string Version =>
        typeof(ShellSession).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "Development build";

    public ShellSession(PreferenceStore store, PreferenceLoad load, ProjectDocument? document = null,
        HostLocalizer? localizer = null)
    {
        _store = store;
        _localizer = localizer ?? new();
        Preferences = load.Preferences;
        Document = document ?? ProjectDocument.Create();
        _noticeKey = load.UsedFallback ? "Preference.LoadFallback" : null;
    }

    // Apply on the UI owner at a completed button action. Keep controls/window and document alive.
    public Task ToggleLanguageAsync() => ApplyAsync(Preferences with
    {
        Language = Preferences.Language == HostLanguage.English ? HostLanguage.Russian : HostLanguage.English
    });

    public Task ToggleThemeAsync() => ApplyAsync(Preferences with
    {
        Theme = Preferences.Theme == HostTheme.Dark ? HostTheme.Light : HostTheme.Dark
    });

    private async Task ApplyAsync(HostPreferences preferences)
    {
        ObjectDisposedException.ThrowIf(IsClosed, this);
        Preferences = preferences;
        Refresh();
        var persisted = await _store.SaveAsync(preferences);
        if (IsClosed) return;
        _noticeKey = persisted ? null : "Preference.SaveFailed";
        Refresh();
    }

    private void Refresh() => PropertyChanged?.Invoke(this, new(null));

    public void Dispose()
    {
        if (IsClosed) return;
        IsClosed = true;
        if (!HasWorkflowOwner) Document.Close();
    }
}
