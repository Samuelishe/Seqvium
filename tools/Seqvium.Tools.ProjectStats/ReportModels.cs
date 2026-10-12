// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.Tools.ProjectStats;

internal enum ReportFormat { Console, Markdown, Json }
internal enum Ownership { Production, Tests, Tooling, Experiments, Documentation, Repository }
internal enum FileKind { CSharp, C, Cpp, Header, Axaml, Xaml, Markdown, BuildConfig, Other }

internal sealed record FileStats(string Path, string Extension, Ownership Ownership, FileKind Kind,
    bool Text, bool Readable, long Lines, long Characters, long Facts, long Theories);

internal sealed record ScanResult(IReadOnlyList<FileStats> Files, long SkippedEntries,
    long UnreadableFiles, long UnreadableDirectories, IReadOnlyList<SkipCount> Skips);
internal sealed record SkipCount(string Reason, long Entries);
internal sealed record Totals(long Files, long TextFiles, long UnreadableFiles, long Lines,
    long Characters, long CSharpFiles, long CSharpLines, long CSharpCharacters, long Facts, long Theories);
internal sealed record GroupStats(string Subject, Totals Totals);
internal sealed record LargestFiles(string Category, IReadOnlyList<FileStats> Files);
internal sealed record DirectoryStats(string Subject, long DirectFiles, long SubtreeFiles, long CSharpLines);
internal sealed record Diagnostic(string Code, string Severity, string Message, string? Subject);
internal sealed record DiagnosticCount(string Code, string Severity, int Count);
internal sealed record GitMetadata(bool Available, string? Branch, string? Head, bool? Dirty);
internal sealed record Invocation(string Root, ReportFormat Format, int Top, bool OutputToFile,
    IReadOnlyList<string> Parameters);
internal sealed record ReportMetadata(DateTimeOffset GeneratedUtc, string Tool, string Version,
    string Root, GitMetadata Git, double ScanMilliseconds, double ReportMilliseconds, Invocation Invocation);
internal sealed record ProjectStatsReport(ReportMetadata Metadata, Totals Totals,
    IReadOnlyList<GroupStats> Extensions, IReadOnlyList<GroupStats> Ownership,
    IReadOnlyList<GroupStats> Kinds, IReadOnlyList<GroupStats> Projects, IReadOnlyList<string> Solutions,
    IReadOnlyList<DirectoryStats> Directories, IReadOnlyList<DirectoryStats> Density,
    IReadOnlyList<LargestFiles> Largest, long SkippedEntries, long UnreadableFiles,
    long UnreadableDirectories, IReadOnlyList<SkipCount> Skips, IReadOnlyList<DiagnosticCount> WarningSummary,
    IReadOnlyList<Diagnostic> Diagnostics);
