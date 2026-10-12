// SPDX-License-Identifier: Apache-2.0
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace Seqvium.Tools.ProjectStats;

internal sealed class FileScanner(Func<string, TextReader>? openText = null)
{
    // Lexical patterns intentionally do not parse comments, strings, aliases, or conditional compilation.
    private static readonly Regex Facts = new(@"\[\s*(?:global::)?(?:Xunit\.)?Fact(?:Attribute)?(?=\s|\(|\])",
        RegexOptions.CultureInvariant);
    private static readonly Regex Theories = new(@"\[\s*(?:global::)?(?:Xunit\.)?Theory(?:Attribute)?(?=\s|\(|\])",
        RegexOptions.CultureInvariant);
    private const long MaximumTextBytes = 16 * 1024 * 1024;

    internal ScanResult Scan(string root, string? output = null)
    {
        var files = new List<FileStats>();
        var skips = new SortedDictionary<string, long>(StringComparer.Ordinal);
        long unreadableFiles = 0, unreadableDirectories = 0;
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out string? directory))
        {
            string[] entries;
            try
            {
                // Recheck before enumeration as well as when scheduling a directory.
                if (!PathPolicy.HasNoReparseAncestors(directory)) { Skip("ReparsePoint"); continue; }
                entries = Directory.GetFileSystemEntries(directory);
            }
            catch (Exception error) when (IsReadError(error))
            {
                unreadableDirectories++;
                continue;
            }
            foreach (string entry in entries.Order(StringComparer.Ordinal))
            {
                string relative = Path.GetRelativePath(root, entry).Replace('\\', '/');
                FileAttributes attributes;
                try { attributes = File.GetAttributes(entry); }
                catch (Exception error) when (IsReadError(error))
                {
                    // The type/path is unknown; report only an aggregate count.
                    Skip("UnavailableEntry");
                    continue;
                }
                if ((attributes & FileAttributes.ReparsePoint) != 0) { Skip("ReparsePoint"); continue; }
                bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                string? reason = PathPolicy.Exclusion(relative, isDirectory);
                // The permanent contract has the same basename as the root generated Markdown report.
                if (reason == "GeneratedReport" && relative.Equals("docs/PROJECT_STATS.md", StringComparison.OrdinalIgnoreCase)) reason = null;
                if (reason is not null) { Skip(reason); continue; }
                if (isDirectory) { pending.Push(entry); continue; }
                if (output is not null && entry.Equals(output, OperatingSystem.IsWindows()
                        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                { Skip("OutputTarget"); continue; }

                string extension = Path.GetExtension(entry).ToLowerInvariant();
                if (extension.Length == 0) extension = "[none]";
                FileKind kind = PathPolicy.Kind(extension);
                bool text = kind != FileKind.Other || extension is ".txt" or ".svg" or ".resx" or ".fsh" or ".fs" ||
                    Path.GetFileName(entry) is "LICENSE" or "NOTICE" or "CMakeLists.txt";
                Ownership ownership = PathPolicy.Classify(relative);
                long lines = 0, characters = 0, facts = 0, theories = 0;
                bool readable = true;
                if (text)
                {
                    try
                    {
                        if (!PathPolicy.HasNoReparseAncestors(entry)) { Skip("ReparsePoint"); continue; }
                        if (new FileInfo(entry).Length > MaximumTextBytes) { Skip("TextSizeLimit"); continue; }
                        using TextReader reader = openText is null
                            ? new StreamReader(entry, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true)
                            : openText(entry);
                        string content = reader.ReadToEnd();
                        characters = content.Length;
                        lines = CountLines(content);
                        if (kind == FileKind.CSharp && ownership == Ownership.Tests)
                        { facts = Facts.Count(content); theories = Theories.Count(content); }
                    }
                    catch (Exception error) when (IsReadError(error) || error is DecoderFallbackException)
                    { readable = false; unreadableFiles++; }
                }
                files.Add(new FileStats(relative, extension, ownership, kind, text, readable,
                    lines, characters, facts, theories));
            }
        }
        return new ScanResult(files.OrderBy(f => f.Path, StringComparer.Ordinal).ToArray(),
            skips.Values.Sum(), unreadableFiles, unreadableDirectories,
            skips.Select(p => new SkipCount(p.Key, p.Value)).ToArray());

        void Skip(string reason) => skips[reason] = skips.GetValueOrDefault(reason) + 1;
    }

    internal static bool IsReadError(Exception error) => error is IOException or UnauthorizedAccessException or SecurityException;

    internal static long CountLines(string text)
    {
        if (text.Length == 0) return 0;
        long lines = 0;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\r') { lines++; if (i + 1 < text.Length && text[i + 1] == '\n') i++; }
            else if (text[i] == '\n') lines++;
        return text[^1] is '\n' or '\r' ? lines : lines + 1;
    }
}
