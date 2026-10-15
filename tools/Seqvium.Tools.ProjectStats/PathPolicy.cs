// SPDX-License-Identifier: Apache-2.0
using System.Text.RegularExpressions;

namespace Seqvium.Tools.ProjectStats;

internal static class PathPolicy
{
    private static readonly HashSet<string> PublicRoots = new(StringComparer.OrdinalIgnoreCase)
        { "src", "tests", "tools", "eng", "experiments", "docs" };
    private static readonly HashSet<string> RootFiles = new(StringComparer.OrdinalIgnoreCase)
        { "AGENTS.md", "README.md", "LICENSE", "NOTICE", "global.json", "NuGet.config",
            "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props",
            ".gitignore", ".gitattributes", ".editorconfig" };
    private static readonly HashSet<string> BlockedDirectories = new(StringComparer.OrdinalIgnoreCase)
        { ".git", ".idea", ".vs", ".vscode", "bin", "obj", "packages", "publish", "artifacts",
            ".artifacts", ".scratch", "TestResults", "tmp", "temp", "scratch", "local", "private",
            "secrets", "credentials", "user", "users", "userdata", "settings", "samples", "audio",
            "corpora", "corpus", "recordings", "music", "personal", "projects", "build", "out",
            "dist", "node_modules", ".cache", ".codex-cache", "_ReSharper.Caches", "reports", "devices", "endpoints" };
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".user", ".suo", ".userosscache", ".docstates", ".iml", ".env", ".key", ".pem",
            ".pfx", ".p12", ".snk", ".tmp", ".temp", ".cache", ".log", ".bak", ".seqvium",
            ".wav", ".mp3", ".flac", ".ogg", ".aiff", ".aif", ".m4a", ".mid", ".midi",
            ".dll", ".exe", ".pdb", ".o", ".obj", ".a", ".so", ".dylib" };
    private static readonly Regex SensitiveName = new(
        @"(^|[._ -])(private|personal|secret|secrets|credentials|token|tokens|password|local|user|users)([._ -]|$)|^[.]env($|[.])|^appsettings[.]|[0-9a-f]{32}|[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex SafeName = new(@"^[A-Za-z0-9_. -]{1,160}$", RegexOptions.CultureInvariant);

    internal static string? Exclusion(string relative, bool directory)
    {
        string[] segments = relative.Split('/');
        if (segments.Any(s => !SafeName.IsMatch(s) || SensitiveName.IsMatch(s) ||
                s.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase)))
            return "PrivateOrUnsafeName";
        string name = segments[^1];
        if (directory)
        {
            bool sourceAudio = name.Equals("Audio", StringComparison.OrdinalIgnoreCase) && segments.Length == 3 &&
                (segments[0].Equals("src", StringComparison.OrdinalIgnoreCase) || segments[0].Equals("tests", StringComparison.OrdinalIgnoreCase));
            bool hostSettingsSource = relative.Equals("src/Seqvium.Desktop/Settings", StringComparison.OrdinalIgnoreCase);
            if ((!sourceAudio && !hostSettingsSource && BlockedDirectories.Contains(name)) || name.StartsWith('.') ||
                name.StartsWith("cmake-build-", StringComparison.OrdinalIgnoreCase))
                return "ExcludedDirectory";
            if (segments.Length == 1 && !PublicRoots.Contains(name))
                return "OutsidePublicCorpus";
            if (relative.Equals("docs/archive", StringComparison.OrdinalIgnoreCase))
                return "ColdArchive";
        }
        else
        {
            if (Path.GetExtension(name).ToLowerInvariant() is ".json" or ".xml" or ".config" or ".yaml" or ".yml" &&
                (name.Contains("device", StringComparison.OrdinalIgnoreCase) || name.Contains("endpoint", StringComparison.OrdinalIgnoreCase)))
                return "PrivateDeviceSettings";
            if (relative.StartsWith("docs/experiments/", StringComparison.OrdinalIgnoreCase) &&
                Path.GetExtension(name).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif")
                return "GeneratedCapture";
            if (IsGeneratedReport(name)) return "GeneratedReport";
            if (BlockedExtensions.Contains(Path.GetExtension(name)) || name.EndsWith('~') ||
                name.Contains(".g.", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
                return "PrivateOrGeneratedFile";
            if (segments.Length == 1 && !RootFiles.Contains(name) &&
                !name.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) &&
                !name.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                return "OutsidePublicCorpus";
        }
        return null;
    }

    internal static bool IsGeneratedReport(string name) =>
        name.Equals("PROJECT_STATS.md", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("PROJECT_STATS.json", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("project-stats.", StringComparison.OrdinalIgnoreCase);

    internal static bool HasNoReparseAncestors(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
        return true;
    }

    internal static Ownership Classify(string path) => path.Split('/')[0].ToLowerInvariant() switch
    {
        "src" => Ownership.Production,
        "tests" => Ownership.Tests,
        "tools" or "eng" => Ownership.Tooling,
        "experiments" => Ownership.Experiments,
        "docs" => Ownership.Documentation,
        _ => Ownership.Repository
    };

    internal static FileKind Kind(string extension) => extension switch
    {
        ".cs" => FileKind.CSharp,
        ".c" => FileKind.C,
        ".cc" or ".cpp" or ".cxx" => FileKind.Cpp,
        ".h" or ".hh" or ".hpp" or ".hxx" => FileKind.Header,
        ".axaml" => FileKind.Axaml,
        ".xaml" => FileKind.Xaml,
        ".md" => FileKind.Markdown,
        ".csproj" or ".fsproj" or ".vbproj" or ".sln" or ".slnx" or ".props" or ".targets" or
        ".json" or ".config" or ".xml" or ".manifest" or ".yml" or ".yaml" or ".ps1" or ".sh" or
            ".cmake" or ".toml" or ".editorconfig" or ".gitignore" or ".gitattributes" => FileKind.BuildConfig,
        _ => FileKind.Other
    };
}
