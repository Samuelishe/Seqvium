// SPDX-License-Identifier: Apache-2.0
using System.Text;
using Seqvium.Tools.ProjectStats;

namespace Seqvium.Tests.ProjectStats;

internal sealed class StatsRepository : IDisposable
{
    internal string Root { get; } = Path.Combine(Path.GetTempPath(), "Seqvium.ProjectStats", Guid.NewGuid().ToString("N"));

    internal StatsRepository() => Directory.CreateDirectory(Root);

    internal string Write(string relative, string content, bool bom = false)
    {
        string path = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(bom));
        return path;
    }

    internal ProjectStatsReport Collect(int top = 10, FileScanner? scanner = null) => ReportCollector.Collect(
        new Options(Root, top, ReportFormat.Console, null), scanner, _ => GitReader.Unavailable);

    public void Dispose()
    {
        Directory.Delete(Root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
