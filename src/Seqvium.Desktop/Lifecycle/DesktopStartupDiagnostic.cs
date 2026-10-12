// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Lifecycle;

internal static class DesktopStartupDiagnostic
{
    public static void Show(string key)
    {
        var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru"
            ? HostLanguage.Russian : HostLanguage.English;
        string message = new HostLocalizer().Get(key, language);
        Console.Error.WriteLine("Seqvium: " + message);
        if (OperatingSystem.IsWindows()) MessageBoxW(0, message, "Seqvium", 0x10);
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(nint window, string text, string caption, uint flags);
}
