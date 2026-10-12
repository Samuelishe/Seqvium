// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Seqvium.Tools.ProjectStats;

internal static class GitReader
{
    internal static GitMetadata Read(string root, string executable = "git")
    {
        try
        {
            string? head = Run(root, executable, "rev-parse", "--verify", "HEAD");
            string? branch = Run(root, executable, "branch", "--show-current");
            string? status = Run(root, executable, "status", "--porcelain=v1", "--untracked-files=normal");
            if (head is null || branch is null || status is null ||
                !Regex.IsMatch(head.Trim(), @"\A[0-9a-f]{40,64}\z", RegexOptions.CultureInvariant))
                return Unavailable;
            return new GitMetadata(true, SanitizeBranch(branch.Trim()), head.Trim(), status.Length != 0);
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        { return Unavailable; }
    }

    internal static GitMetadata Unavailable => new(false, null, null, null);

    internal static string SanitizeBranch(string branch)
    {
        if (branch.Length == 0) return "Detached";
        // Arbitrary branch names can contain personal identifiers; expose only repository stage/common names.
        return branch is "master" or "main" or "develop" ||
            Regex.IsMatch(branch, @"\ASEQ-(?:R[0-9]+(?:-F[0-9]+(?:-[AB])?)?|PROJECT-STATS-R[0-9]+|VERSIONING-R[0-9]+)\z", RegexOptions.CultureInvariant)
            ? branch : "[redacted]";
    }

    private static string? Run(string root, string executable, params string[] arguments)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.ArgumentList.Add("--no-optional-locks");
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using Process? process = Process.Start(start);
        if (process is null) return null;
        // Drain both pipes concurrently; stderr is never forwarded to reports or the console.
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(5000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            return null;
        }
        // This is a synchronous CLI boundary, not a synchronization-context/UI operation.
        string result = stdout.GetAwaiter().GetResult();
        _ = stderr.GetAwaiter().GetResult();
        return process.ExitCode == 0 ? result : null;
    }
}
