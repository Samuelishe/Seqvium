// SPDX-License-Identifier: Apache-2.0
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seqvium.Tools.ProjectStats;

internal static class ReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static string Render(ProjectStatsReport report, ReportFormat format)
    {
        if (format == ReportFormat.Json) return JsonSerializer.Serialize(report, JsonOptions) + "\n";
        var text = new StringBuilder();
        bool markdown = format == ReportFormat.Markdown;
        ReportMetadata metadata = report.Metadata;
        Title("Seqvium repository statistics", 1);
        text.AppendLine("Structural diagnostics; advisory only. Test attributes are a lexical estimate, not executed tests.");
        text.AppendLine();
        Table(["Metadata", "Value"], [
            ["UTC", metadata.GeneratedUtc.ToString("O", CultureInfo.InvariantCulture)],
            ["Tool", metadata.Tool], ["Tool version (assembly)", metadata.Version], ["Root", metadata.Root],
            ["Git available", metadata.Git.Available.ToString()], ["Branch", metadata.Git.Branch ?? "Unavailable"],
            ["HEAD", metadata.Git.Head ?? "Unavailable"], ["Dirty", metadata.Git.Dirty?.ToString() ?? "Unavailable"],
            ["Scan ms", Number(metadata.ScanMilliseconds)], ["Report aggregation ms (includes Git)", Number(metadata.ReportMilliseconds)],
            ["Format", metadata.Invocation.Format.ToString()], ["Top-N", Number(metadata.Invocation.Top)],
            ["Output to file", metadata.Invocation.OutputToFile.ToString()],
            ["Normalized parameters", string.Join(' ', metadata.Invocation.Parameters)],
            ["Skipped entries", Number(report.SkippedEntries)], ["Unreadable files", Number(report.UnreadableFiles)],
            ["Unreadable directories", Number(report.UnreadableDirectories)]
        ]);
        Title("Totals");
        Groups([new GroupStats("All included files", report.Totals)]);
        Title("Extensions"); Groups(report.Extensions);
        Title("Ownership"); Groups(report.Ownership);
        Title("File kinds"); Groups(report.Kinds);
        Title(".NET projects (nearest containing folder; no MSBuild evaluation)"); Groups(report.Projects);
        Title("Solutions");
        foreach (string solution in report.Solutions) text.AppendLine(solution);
        text.AppendLine();
        Title("Directories"); Directories(report.Directories);
        Title("Directory density (direct files, Top-N)"); Directories(report.Density);
        Title("Largest text files (lines, characters, ordinal path; Top-N per category)");
        foreach (LargestFiles group in report.Largest.Where(g => g.Files.Count != 0))
        {
            Title(group.Category, 3);
            Table(["Path", "Lines", "Characters", "Facts (lexical)", "Theories (lexical)"], group.Files
                .Select(f => new[] { f.Path, Number(f.Lines), Number(f.Characters), Number(f.Facts), Number(f.Theories) }));
        }
        Title("Skipped entries by reason (no excluded paths disclosed)");
        Table(["Reason", "Entries"], report.Skips.Select(s => new[] { s.Reason, Number(s.Entries) }));
        Title("Advisory warning summary");
        Table(["Code", "Severity", "Count"], report.WarningSummary.Select(s => new[] { s.Code, s.Severity, Number(s.Count) }));
        text.AppendLine($"Total advisory diagnostics: {report.Diagnostics.Count.ToString(CultureInfo.InvariantCulture)}.");
        text.AppendLine();
        Table(["Code", "Severity", "Message", "Subject"], report.Diagnostics.Select(d =>
            new[] { d.Code, d.Severity, d.Message, d.Subject ?? "[aggregate]" }));
        text.AppendLine("Counts exclude private/generated/local trees and cold docs/archive history. UTF-16 character units; CRLF/CR/LF lines; final newline adds no empty line. No source text is reported.");
        return text.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);

        void Title(string label, int level = 2)
        {
            text.AppendLine(markdown ? new string('#', level) + " " + label : label);
            text.AppendLine();
        }
        void Table(string[] headers, IEnumerable<string[]> rows)
        {
            Row(headers);
            if (markdown) Row(headers.Select(_ => "---").ToArray());
            foreach (string[] row in rows) Row(row);
            text.AppendLine();
        }
        void Row(string[] cells) => text.AppendLine(markdown
            ? "| " + string.Join(" | ", cells.Select(c => c.Replace("|", "\\|", StringComparison.Ordinal))) + " |"
            : string.Join("\t", cells));
        void Groups(IEnumerable<GroupStats> groups) => Table(
            ["Subject", "Files", "Text", "Unreadable", "Lines", "Characters", "C# files", "C# lines", "C# characters", "Facts (lexical)", "Theories (lexical)"],
            groups.Select(g => new[] { g.Subject, Number(g.Totals.Files), Number(g.Totals.TextFiles),
                Number(g.Totals.UnreadableFiles), Number(g.Totals.Lines), Number(g.Totals.Characters),
                Number(g.Totals.CSharpFiles), Number(g.Totals.CSharpLines), Number(g.Totals.CSharpCharacters),
                Number(g.Totals.Facts), Number(g.Totals.Theories) }));
        void Directories(IEnumerable<DirectoryStats> directories) => Table(
            ["Directory", "Direct files", "Subtree files", "Subtree C# lines"],
            directories.Select(d => new[] { d.Subject, Number(d.DirectFiles), Number(d.SubtreeFiles), Number(d.CSharpLines) }));
    }

    private static string Number<T>(T number) where T : IFormattable => number.ToString(null, CultureInfo.InvariantCulture);
}
