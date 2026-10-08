// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        foreach (var (role, value) in HostMetrics.Values)
            Resources[role] = role == "Control.Corner" ? new CornerRadius(value) : value;
        ApplyTheme(HostTheme.Dark);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        var store = new PreferenceStore(PreferenceStore.DefaultPath());
        var session = new ShellSession(store, await store.LoadAsync());
        ApplyTheme(session.Preferences.Theme);
        desktop.MainWindow = new MainWindow(session, this);
        desktop.MainWindow.Show();
    }

    internal void ApplyTheme(HostTheme theme)
    {
        RequestedThemeVariant = theme == HostTheme.Light ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var (role, value) in HostPalette.For(theme)) Resources[role] = new SolidColorBrush(Color.Parse(value));
    }
}
