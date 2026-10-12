// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;

namespace Seqvium.Tools.ProjectStats;

internal static class ReportCollector
{
    internal static ProjectStatsReport Collect(Options options, FileScanner? scanner = null,
        Func<string, GitMetadata>? readGit = null)
    {
        DateTimeOffset utc = DateTimeOffset.UtcNow;
        var clock = Stopwatch.StartNew();
        ScanResult scan = (scanner ?? new FileScanner()).Scan(options.Root, options.Output);
        double scanMilliseconds = clock.Elapsed.TotalMilliseconds;
        clock.Restart();
        GitMetadata git = (readGit ?? (root => GitReader.Read(root)))(options.Root);
        IReadOnlyList<FileStats> files = scan.Files;
        string[] projects = files.Where(f => f.Extension is ".csproj" or ".fsproj" or ".vbproj")
            .Select(f => f.Path).Order(StringComparer.Ordinal).ToArray();
        var assignments = projects.ToDictionary(p => p, _ => new List<FileStats>(), StringComparer.Ordinal);
        var unassigned = new List<FileStats>();
        foreach (FileStats file in files)
        {
            // Closest containing project folder wins; same-folder ties use the ordinal project path.
            string? owner = projects.Where(p => file.Path.StartsWith(Folder(p) + "/", StringComparison.Ordinal))
                .OrderByDescending(p => Folder(p).Length).ThenBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (owner is null) unassigned.Add(file); else assignments[owner].Add(file);
        }
        GroupStats[] projectStats = assignments.Select(p => new GroupStats(p.Key, Sum(p.Value)))
            .Append(new GroupStats("[unassigned]", Sum(unassigned)))
            .OrderBy(p => p.Subject, StringComparer.Ordinal).ToArray();
        string[] folders = files.SelectMany(f => Ancestors(f.Path)).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToArray();
        DirectoryStats[] directories = folders.Select(folder =>
        {
            FileStats[] subtree = files.Where(f => folder == "." || f.Path.StartsWith(folder + "/", StringComparison.Ordinal)).ToArray();
            return new DirectoryStats(folder, files.LongCount(f => Folder(f.Path) == folder),
                subtree.LongLength, subtree.Where(f => f.Kind == FileKind.CSharp).Sum(f => f.Lines));
        }).ToArray();
        var largest = Enum.GetValues<FileKind>().Select(k => new LargestFiles(k.ToString(), Top(files.Where(f => f.Kind == k), options.Top)))
            .Concat(Enum.GetValues<Ownership>().Select(o => new LargestFiles("CSharp/" + o,
                Top(files.Where(f => f.Kind == FileKind.CSharp && f.Ownership == o), options.Top))))
            .OrderBy(g => g.Category, StringComparer.Ordinal).ToArray();
        var diagnostics = new List<Diagnostic>();
        foreach (FileStats file in files.Where(f => f.Kind == FileKind.CSharp && f.Readable))
        {
            if (file.Ownership == Ownership.Production && file.Lines >= 1000)
                diagnostics.Add(new("PST001", "Warning", "Production C# file has at least 1000 lines; review its scope.", file.Path));
            if (file.Ownership == Ownership.Tests && file.Lines >= 2000)
                diagnostics.Add(new("PST002", "Warning", "Test C# file has at least 2000 lines; review navigation needs.", file.Path));
            if (file.Ownership == Ownership.Tests && file.Facts + file.Theories >= 50)
                diagnostics.Add(new("PST003", "Info", "Test file contains at least 50 lexical test attributes.", file.Path));
        }
        if (scan.UnreadableFiles + scan.UnreadableDirectories != 0)
            diagnostics.Add(new("PST004", "Warning", "Unreadable files/directories leave text metrics incomplete.", null));
        if (scan.Skips.Any(s => s.Reason == "TextSizeLimit"))
            diagnostics.Add(new("PST005", "Info", "Text files above the 16 MiB read limit were excluded.", null));
        Diagnostic[] sorted = diagnostics.OrderBy(d => d.Code, StringComparer.Ordinal)
            .ThenBy(d => d.Subject, StringComparer.Ordinal).ToArray();
        var parameters = new List<string> { ".", "--top", options.Top.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        if (options.Format != ReportFormat.Console) parameters.Add("--" + options.Format.ToString().ToLowerInvariant());
        if (options.Output is not null) parameters.AddRange(["--output", "[explicit output]"]);
        var assembly = typeof(ReportCollector).Assembly.GetName();
        return new ProjectStatsReport(
            new ReportMetadata(utc, assembly.Name ?? "Unavailable", assembly.Version?.ToString() ?? "Unavailable",
                ".", git, scanMilliseconds, clock.Elapsed.TotalMilliseconds,
                new Invocation(".", options.Format, options.Top, options.Output is not null, parameters)),
            Sum(files), Groups(files, f => f.Extension), Groups(files, f => f.Ownership.ToString()),
            Groups(files, f => f.Kind.ToString()), projectStats,
            files.Where(f => f.Extension is ".sln" or ".slnx").Select(f => f.Path).ToArray(),
            directories, directories.OrderByDescending(d => d.DirectFiles).ThenBy(d => d.Subject, StringComparer.Ordinal).Take(options.Top).ToArray(),
            largest, scan.SkippedEntries, scan.UnreadableFiles, scan.UnreadableDirectories, scan.Skips,
            sorted.GroupBy(d => (d.Code, d.Severity)).Select(g => new DiagnosticCount(g.Key.Code, g.Key.Severity, g.Count())).ToArray(), sorted);
    }

    private static IReadOnlyList<FileStats> Top(IEnumerable<FileStats> files, int top) => files
        .Where(f => f.Text && f.Readable).OrderByDescending(f => f.Lines).ThenByDescending(f => f.Characters)
        .ThenBy(f => f.Path, StringComparer.Ordinal).Take(top).ToArray();

    private static GroupStats[] Groups(IEnumerable<FileStats> files, Func<FileStats, string> key) => files.GroupBy(key)
        .OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => new GroupStats(g.Key, Sum(g))).ToArray();

    internal static Totals Sum(IEnumerable<FileStats> files)
    {
        FileStats[] all = files.ToArray();
        FileStats[] cs = all.Where(f => f.Kind == FileKind.CSharp).ToArray();
        return new Totals(all.LongLength, all.LongCount(f => f.Text), all.LongCount(f => !f.Readable),
            all.Sum(f => f.Lines), all.Sum(f => f.Characters), cs.LongLength, cs.Sum(f => f.Lines),
            cs.Sum(f => f.Characters), all.Sum(f => f.Facts), all.Sum(f => f.Theories));
    }

    private static string Folder(string path) => path.Contains('/') ? path[..path.LastIndexOf('/')] : ".";
    private static IEnumerable<string> Ancestors(string path)
    {
        yield return ".";
        for (int i = 0; i < path.Length; i++) if (path[i] == '/') yield return path[..i];
    }
}
