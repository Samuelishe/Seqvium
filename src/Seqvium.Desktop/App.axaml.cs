// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Lifecycle;
using Avalonia.Threading;

namespace Seqvium.Desktop;

public sealed partial class App : Application
{
    private readonly DesktopLaunch _launch;
    private readonly TaskCompletionSource<MainWindow> _mainWindowReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public App() : this(DesktopLaunch.Production) { }
    internal App(DesktopLaunch launch) => _launch = launch;
    internal event Action<MainWindow>? MainWindowCreated;
    internal string? IsolatedMediaDirectory => _launch.IsolatedMediaDirectory;
    internal string ConfigurationDirectory => _launch.ConfigurationDirectory;
    private HostTheme? _appliedTheme;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        foreach (var (role, value) in HostMetrics.Resources) Resources[role] = value;
        ApplyTheme(HostTheme.Dark);
        _ = new HostFluentResources(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        var store = new PreferenceStore(Path.Combine(_launch.ConfigurationDirectory, "preferences.json"));
        var controlStore = new ControlPreferencesStore(Path.Combine(_launch.ConfigurationDirectory, "controls.json"));
        var session = new ShellSession(store, await store.LoadAsync(), controlStore: controlStore,
            controls: await controlStore.LoadAsync());
        var layoutStore = new WorkspaceLayoutStore(Path.Combine(_launch.ConfigurationDirectory, "workspace-layout.json"));
        var layout = await layoutStore.LoadAsync();
        ApplyTheme(session.Preferences.Theme);
        desktop.MainWindow = new MainWindow(session, this, layoutStore, layout);
        desktop.MainWindow.Show();
        _mainWindowReady.TrySetResult((MainWindow)desktop.MainWindow);
        MainWindowCreated?.Invoke((MainWindow)desktop.MainWindow);
    }

    internal async Task ActivateExistingAsync(CancellationToken cancellation)
    {
        var window = await _mainWindowReady.Task.WaitAsync(cancellation).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!cancellation.IsCancellationRequested) window.ActivateExisting();
        });
    }

    internal void ApplyTheme(HostTheme theme)
    {
        if (_appliedTheme == theme) return;
        _appliedTheme = theme;
        RequestedThemeVariant = theme == HostTheme.Light ? ThemeVariant.Light : ThemeVariant.Dark;
        foreach (var (role, value) in HostPalette.For(theme)) Resources[role] = new SolidColorBrush(Color.Parse(value));
        foreach (var (role, value) in HostMaterials.For(theme)) Resources[role] = value;
    }
}
