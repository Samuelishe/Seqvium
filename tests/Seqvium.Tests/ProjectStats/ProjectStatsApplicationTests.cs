// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Seqvium.Tools.ProjectStats;
using Xunit;

namespace Seqvium.Tests.ProjectStats;

public sealed class ProjectStatsApplicationTests
{
    [Theory]
    [InlineData("Console")]
    [InlineData("Markdown")]
    [InlineData("Json")]
    public void EveryFormatContainsMetricsAndSanitizedMetadata(string formatName)
    {
        using var repo = new StatsRepository();
        repo.Write("src/App/A.cs", "code\n");
        repo.Write("tests/T/T.cs", "[Fact]\n[Theory]\n");
        ReportFormat format = Enum.Parse<ReportFormat>(formatName);
        var options = new Options(repo.Root, 25, format, null);
        ProjectStatsReport report = ReportCollector.Collect(options, readGit: _ => GitReader.Unavailable);
        string rendered = ReportRenderer.Render(report, format);
        Assert.DoesNotContain(repo.Root, rendered);
        Assert.Contains("Seqvium.Tools.ProjectStats", rendered);
        Assert.Equal(TimeSpan.Zero, report.Metadata.GeneratedUtc.Offset);
        Assert.Equal(typeof(ProjectStatsApplication).Assembly.GetName().Version!.ToString(), report.Metadata.Version);
        Assert.Equal(".", report.Metadata.Root);
        Assert.False(report.Metadata.Git.Available);
        Assert.Null(report.Metadata.Git.Dirty);
        Assert.Equal(25, report.Metadata.Invocation.Top);
        Assert.Equal(format, report.Metadata.Invocation.Format);
        Assert.True(report.Metadata.ScanMilliseconds >= 0);
        Assert.True(report.Metadata.ReportMilliseconds >= 0);
        if (format == ReportFormat.Json)
        {
            using JsonDocument json = JsonDocument.Parse(rendered);
            Assert.Equal(2, json.RootElement.GetProperty("Totals").GetProperty("Files").GetInt64());
            Assert.Equal(".", json.RootElement.GetProperty("Metadata").GetProperty("Root").GetString());
            Assert.Equal(formatName, json.RootElement.GetProperty("Metadata").GetProperty("Invocation").GetProperty("Format").GetString());
            Assert.True(json.RootElement.TryGetProperty("WarningSummary", out _));
        }
        else
        {
            Assert.Contains("lexical estimate, not executed tests", rendered);
            Assert.Contains("Ownership", rendered);
            Assert.Contains("Directory density", rendered);
            Assert.Contains("Largest text files", rendered);
            Assert.Contains("Skipped entries", rendered);
            Assert.Contains("Advisory warning summary", rendered);
            if (format == ReportFormat.Markdown) Assert.StartsWith("# Seqvium repository statistics\n\n", rendered);
            else Assert.DoesNotContain("| ---", rendered);
        }
    }

    [Fact]
    public void ConsoleWithoutOutputMakesNoFilesAndDefaultsToCurrentRoot()
    {
        using var repo = new StatsRepository();
        string source = repo.Write("src/App/A.cs", "unchanged\n");
        string[] before = Directory.GetFiles(repo.Root, "*", SearchOption.AllDirectories);
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        Assert.Equal(0, ProjectStatsApplication.Run([], stdout, stderr, repo.Root, readGit: _ => GitReader.Unavailable));
        Assert.Equal(before, Directory.GetFiles(repo.Root, "*", SearchOption.AllDirectories));
        Assert.Equal("unchanged\n", File.ReadAllText(source));
        Assert.Equal("", stderr.ToString());
        Assert.Contains("C# lines", stdout.ToString());
    }

    [Theory]
    [InlineData("--markdown", "PROJECT_STATS.md")]
    [InlineData("--json", "PROJECT_STATS.json")]
    public void RepeatedReportsNeverCountThemselvesOrChangeSource(string format, string name)
    {
        using var repo = new StatsRepository();
        string source = repo.Write("src/App/A.cs", "unchanged\n");
        repo.Write("docs/PROJECT_STATS.md", "canonical\n");
        long before = repo.Collect().Totals.Files;
        string[] args = [".", format, "--output", name];
        for (int i = 0; i < 2; i++)
        {
            using var stdout = new StringWriter();
            Assert.Equal(0, ProjectStatsApplication.Run(args, stdout, TextWriter.Null, repo.Root, readGit: _ => GitReader.Unavailable));
            Assert.DoesNotContain(repo.Root, stdout.ToString());
            Assert.Equal(before, repo.Collect().Totals.Files);
        }
        Assert.True(File.Exists(Path.Combine(repo.Root, name)));
        Assert.Equal("unchanged\n", File.ReadAllText(source));
        Assert.Equal("canonical\n", File.ReadAllText(Path.Combine(repo.Root, "docs", "PROJECT_STATS.md")));
    }

    [Fact]
    public void ExplicitCustomTargetIsExcludedBeforeReadingAndPathIsRedacted()
    {
        using var repo = new StatsRepository();
        repo.Write("src/App/A.cs", "source\n");
        string output = repo.Write("docs/Custom.md", "custom\n");
        var options = new Options(repo.Root, 10, ReportFormat.Json, output);
        ProjectStatsReport report = ReportCollector.Collect(options,
            new FileScanner(path => { Assert.NotEqual(output, path); return new StringReader("source\n"); }),
            _ => GitReader.Unavailable);
        Assert.Equal(1, report.Totals.Files);
        Assert.True(report.Metadata.Invocation.OutputToFile);
        Assert.DoesNotContain(output, ReportRenderer.Render(report, ReportFormat.Json));
        Assert.DoesNotContain("Custom.md", string.Join(' ', report.Metadata.Invocation.Parameters));
    }

    [Theory]
    [InlineData("--top")]
    [InlineData("--top", "0")]
    [InlineData("--top", "-1")]
    [InlineData("--top", "101")]
    [InlineData("--top", "999999999999999")]
    [InlineData("--top", "abc")]
    [InlineData("--top", "1.5")]
    [InlineData("--markdown", "--json")]
    [InlineData("--json", "--json")]
    [InlineData("--output")]
    [InlineData("--output", "--json")]
    [InlineData("--unknown")]
    [InlineData(".", ".")]
    [InlineData("--top", "2", "--top", "3")]
    public void InvalidArgumentsFailWithoutEchoingInput(params string[] arguments)
    {
        using var repo = new StatsRepository();
        using var stderr = new StringWriter();
        Assert.Equal(2, ProjectStatsApplication.Run(arguments, TextWriter.Null, stderr, repo.Root));
        Assert.Contains("Invalid arguments", stderr.ToString());
        Assert.DoesNotContain(repo.Root, stderr.ToString());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("25")]
    [InlineData("100")]
    public void TopHasPositiveFiniteBounds(string value)
    {
        using var repo = new StatsRepository();
        Assert.Equal(int.Parse(value, System.Globalization.CultureInfo.InvariantCulture), Options.Parse(["--top", value], repo.Root).Top);
    }

    [Fact]
    public void MissingRootAndPrivateInvalidArgumentsDoNotLeakInput()
    {
        using var repo = new StatsRepository();
        using var stderr = new StringWriter();
        string missing = Path.Combine(repo.Root, "private-token-value");
        Assert.Equal(2, ProjectStatsApplication.Run([missing], TextWriter.Null, stderr, repo.Root));
        Assert.Equal(2, ProjectStatsApplication.Run(["--private-token-value"], TextWriter.Null, stderr, repo.Root));
        Assert.DoesNotContain("private-token-value", stderr.ToString());
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void HelpDoesNotScan(string argument)
    {
        using var stdout = new StringWriter();
        Assert.Equal(0, ProjectStatsApplication.Run([argument], stdout, TextWriter.Null,
            scanner: new FileScanner(_ => throw new InvalidOperationException("Must not scan."))));
        Assert.Contains("Usage:", stdout.ToString());
    }

    [Fact]
    public void ExistingSourceCanonicalDocAndMissingOutputParentsAreProtected()
    {
        using var repo = new StatsRepository();
        string source = repo.Write("src/App/A.cs", "source\n");
        string canonical = repo.Write("docs/PROJECT_STATS.md", "canonical\n");
        foreach (string target in new[] { source, canonical, Path.Combine(repo.Root, "missing", "PROJECT_STATS.md"), repo.Root })
        {
            using var stderr = new StringWriter();
            Assert.Equal(1, ProjectStatsApplication.Run(["--output", target], TextWriter.Null, stderr, repo.Root));
            Assert.DoesNotContain(target, stderr.ToString());
        }
        Assert.Equal("source\n", File.ReadAllText(source));
        Assert.Equal("canonical\n", File.ReadAllText(canonical));
    }

    [Fact]
    public void WriteFailuresReturnNonzeroAndRedactExceptionContents()
    {
        using var repo = new StatsRepository();
        using var stderr = new StringWriter();
        Assert.Equal(1, ProjectStatsApplication.Run([], new FailingWriter(), stderr, repo.Root, readGit: _ => GitReader.Unavailable));
        Assert.DoesNotContain("PRIVATE_EXCEPTION", stderr.ToString());
        if (!OperatingSystem.IsWindows()) return;
        string path = repo.Write("PROJECT_STATS.md", "old\n");
        using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.Equal(1, ProjectStatsApplication.Run(["--output", "PROJECT_STATS.md"], TextWriter.Null, stderr, repo.Root,
            readGit: _ => GitReader.Unavailable));
    }

    [Fact]
    public void MissingGitExecutableAndNonRepositoryHaveExplicitFallback()
    {
        using var repo = new StatsRepository();
        repo.Write("src/App/A.cs", "source\n");
        Assert.Equal(GitReader.Unavailable, GitReader.Read(repo.Root, "no-such-git-" + Guid.NewGuid().ToString("N")));
        Assert.Equal(GitReader.Unavailable, GitReader.Read(repo.Root));
        using var stdout = new StringWriter();
        Assert.Equal(0, ProjectStatsApplication.Run(["--json"], stdout, TextWriter.Null, repo.Root,
            readGit: root => GitReader.Read(root, "no-such-git")));
        using JsonDocument json = JsonDocument.Parse(stdout.ToString());
        Assert.False(json.RootElement.GetProperty("Metadata").GetProperty("Git").GetProperty("Available").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("Totals").GetProperty("Files").GetInt64());
    }

    [Theory]
    [InlineData("master", "master")]
    [InlineData("main", "main")]
    [InlineData("SEQ-PROJECT-STATS-R1", "SEQ-PROJECT-STATS-R1")]
    [InlineData("", "Detached")]
    [InlineData("owner/private-token", "[redacted]")]
    [InlineData("/home/private-owner", "[redacted]")]
    [InlineData("SEQ-PRIVATE-TOKEN", "[redacted]")]
    public void GitBranchesCannotDisclosePrivateNames(string branch, string expected) =>
        Assert.Equal(expected, GitReader.SanitizeBranch(branch));

    [Fact]
    public void ToolHasNoProductOrAnalysisAssemblyDependencies()
    {
        var references = typeof(ProjectStatsApplication).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, a => a.Name!.StartsWith("Seqvium", StringComparison.Ordinal));
        Assert.DoesNotContain(references, a => a.Name!.Contains("Avalonia", StringComparison.Ordinal));
        Assert.DoesNotContain(references, a => a.Name!.Contains("CodeAnalysis", StringComparison.Ordinal));
    }

    private sealed class FailingWriter : StringWriter
    {
        public override void Write(string? value) => throw new IOException("PRIVATE_EXCEPTION");
    }
}
