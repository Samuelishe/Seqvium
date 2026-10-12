// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Seqvium.Desktop.Lifecycle;

namespace Seqvium.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) => RunDesktop(args, DesktopLaunch.Production);

    internal static int RunDesktop(string[] args, DesktopLaunch launch, Action<App>? configure = null, Action<string>? diagnostic = null)
    {
        try
        {
            using var instance = new DesktopInstance(launch.Identity);
            if (!instance.IsPrimary)
            {
                if (instance.RequestActivationAsync(DesktopInstance.ActivationTimeout).GetAwaiter().GetResult()) return 0;
                (diagnostic ?? DesktopStartupDiagnostic.Show)("Desktop.Instance.Unresponsive"); return 2;
            }
            var ready = new TaskCompletionSource<App>(TaskCreationOptions.RunContinuationsAsynchronously);
            instance.StartListening(async cancellation =>
            {
                var app = await ready.Task.WaitAsync(cancellation).ConfigureAwait(false);
                await app.ActivateExistingAsync(cancellation).ConfigureAwait(false);
            });
            if (instance.ListenerFailure is not null)
            { (diagnostic ?? DesktopStartupDiagnostic.Show)("Desktop.Instance.Unavailable"); return 3; }
            return AppBuilder.Configure(() => { var app = new App(launch); configure?.Invoke(app); ready.SetResult(app); return app; })
                .UsePlatformDetect().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or WaitHandleCannotBeOpenedException)
        { (diagnostic ?? DesktopStartupDiagnostic.Show)("Desktop.Instance.Unavailable"); return 3; }
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();
}
