// SPDX-License-Identifier: Apache-2.0
using System.Text;

namespace Seqvium.Tools.ProjectStats;

internal static class ProjectStatsApplication
{
    internal static int Run(IReadOnlyList<string> arguments, TextWriter stdout, TextWriter stderr,
        string? currentDirectory = null, FileScanner? scanner = null, Func<string, GitMetadata>? readGit = null)
    {
        if (arguments.Count == 1 && arguments[0] is "--help" or "-h")
        { stdout.WriteLine(Options.Usage); return 0; }
        Options options;
        try { options = Options.Parse(arguments, currentDirectory ?? Directory.GetCurrentDirectory()); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException || FileScanner.IsReadError(error))
        {
            stderr.WriteLine("Invalid arguments or unavailable root. " + Options.Usage);
            return 2;
        }
        try
        {
            if (options.Output is not null && !IsSafeOutput(options))
            {
                stderr.WriteLine("Cannot write output: use an existing directory, a regular report target, and no reparse ancestors. Existing source files are protected.");
                return 1;
            }
            ProjectStatsReport report = ReportCollector.Collect(options, scanner, readGit);
            string rendered = ReportRenderer.Render(report, options.Format);
            if (options.Output is null) stdout.Write(rendered);
            else
            {
                File.WriteAllText(options.Output, rendered, new UTF8Encoding(false));
                stdout.WriteLine("Wrote " + options.Format + " report to the explicit output target.");
            }
            return 0;
        }
        catch (Exception error) when (FileScanner.IsReadError(error) || error is ArgumentException or NotSupportedException)
        {
            stderr.WriteLine("Scan or output failed; no private path or exception content is disclosed.");
            return 1;
        }
    }

    private static bool IsSafeOutput(Options options)
    {
        string target = options.Output!;
        string? parent = Path.GetDirectoryName(target);
        if (parent is null || !Directory.Exists(parent) || Directory.Exists(target) ||
            !PathPolicy.HasNoReparseAncestors(target)) return false;
        if (target.Equals(Path.Combine(options.Root, "docs", "PROJECT_STATS.md"), OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) return false;
        // New custom targets are allowed; only conventional generated report names may be overwritten.
        return !File.Exists(target) || PathPolicy.IsGeneratedReport(Path.GetFileName(target));
    }
}
