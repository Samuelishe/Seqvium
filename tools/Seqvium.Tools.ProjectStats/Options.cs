// SPDX-License-Identifier: Apache-2.0
using System.Globalization;

namespace Seqvium.Tools.ProjectStats;

internal sealed record Options(string Root, int Top, ReportFormat Format, string? Output)
{
    internal const string Usage = "Usage: ProjectStats [root] [--top 1..100] [--markdown | --json] [--output path]";

    internal static Options Parse(IReadOnlyList<string> arguments, string currentDirectory)
    {
        string? root = null;
        string? output = null;
        int top = 10;
        ReportFormat format = ReportFormat.Console;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < arguments.Count; i++)
        {
            string argument = arguments[i];
            if (argument.StartsWith('-') && !seen.Add(argument))
                throw new ArgumentException("Duplicate option.");
            switch (argument)
            {
                case "--top":
                    if (!int.TryParse(Value(arguments, ref i), NumberStyles.None,
                            CultureInfo.InvariantCulture, out top) || top is < 1 or > 100)
                        throw new ArgumentException("--top requires an integer in 1..100.");
                    break;
                case "--markdown":
                case "--json":
                    if (format != ReportFormat.Console)
                        throw new ArgumentException("--markdown and --json are mutually exclusive.");
                    format = argument == "--markdown" ? ReportFormat.Markdown : ReportFormat.Json;
                    break;
                case "--output":
                    output = Value(arguments, ref i);
                    break;
                default:
                    if (argument.StartsWith('-') || root is not null || string.IsNullOrWhiteSpace(argument))
                        throw new ArgumentException("Unknown option or extra root argument.");
                    root = argument;
                    break;
            }
        }
        string fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root ?? ".", currentDirectory));
        if (!Directory.Exists(fullRoot) || !PathPolicy.HasNoReparseAncestors(fullRoot))
            throw new ArgumentException("Root must be an existing directory without reparse ancestors.");
        // Output is root-relative, never echoed. Existing source files are protected by the application.
        return new Options(fullRoot, top, format, output is null ? null : Path.GetFullPath(output, fullRoot));
    }

    private static string Value(IReadOnlyList<string> args, ref int index)
    {
        if (++index >= args.Count || args[index].StartsWith('-') || string.IsNullOrWhiteSpace(args[index]))
            throw new ArgumentException("Option requires a value.");
        return args[index];
    }
}
