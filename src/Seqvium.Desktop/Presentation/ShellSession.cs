// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Seqvium.Core;

namespace Seqvium.Desktop.Presentation;

/// <summary>Owner-thread shell state. Owns one document lifetime, never an audio/device session or pane layout.</summary>
public sealed class ShellSession : INotifyPropertyChanged, IDisposable
{
    private readonly HostLocalizer _localizer;
    private readonly PreferenceStore _store;
    private string? _noticeKey;

    public ProjectDocument Document { get; }
    public HostPreferences Preferences { get; private set; }
    public bool IsClosed { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public string this[string key] => _localizer.Get(key, Preferences.Language);
    public string ProjectName => Document.Current.State.Name ?? this["Project.Unnamed"];

    public string ProjectStatus =>
        this[Document.IsDirty ? "Project.Modified" : Document.Saved is null ? "Project.Pristine" : "Project.Saved"];

    public string Tempo =>
        Document.Current.State.Settings.Tempo.BeatsPerMinute.ToString("0.######", CultureInfo.InvariantCulture);

    public string Meter =>
        FormattableString.Invariant(
            $"{Document.Current.State.Settings.Meter.Numerator}/{Document.Current.State.Settings.Meter.Denominator}");

    public string LanguageLabel =>
        $"{this["Preference.Language"]}: {(Preferences.Language == HostLanguage.Russian ? "Русский" : "English")}";

    public string ThemeLabel =>
        $"{this["Preference.Theme"]}: {this[Preferences.Theme == HostTheme.Dark ? "Preference.Dark" : "Preference.Light"]}";

    public string? Notice => _noticeKey is null ? null : this[_noticeKey];
    public bool HasNotice => _noticeKey is not null;
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
        Document.Close();
    }
}
