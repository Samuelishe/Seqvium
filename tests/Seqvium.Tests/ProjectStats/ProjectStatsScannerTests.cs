// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Text;
using Seqvium.Tools.ProjectStats;
using Xunit;

namespace Seqvium.Tests.ProjectStats;

public sealed class ProjectStatsScannerTests
{
    [Fact]
    public void HostSettingsSourceIsCountedWithoutOpeningOtherSettingsOrNestedPrivateTrees()
    {
        using var repo = new StatsRepository();
        repo.Write("src/Seqvium.Desktop/Settings/SettingsWindow.cs", "source\n");
        repo.Write("src/Other/Settings/Preferences.cs", "private\n");
        repo.Write("tools/Settings/Preferences.cs", "private\n");
        repo.Write("src/Seqvium.Desktop/Settings/private/Preferences.cs", "private\n");
        var report = repo.Collect(scanner: new FileScanner(path =>
        {
            Assert.Equal("SettingsWindow.cs", Path.GetFileName(path));
            return new StringReader("source\n");
        }));
        Assert.Equal(1, report.Totals.CSharpFiles);
        Assert.Equal(1, report.Ownership.Single(group => group.Subject == "Production").Totals.CSharpFiles);
    }

    [Fact]
    public void AuthoredAudioSubsystemsRemainInsideProductionAndTestsCorpus()
    {
        using var repo = new StatsRepository();
        repo.Write("src/App/Audio/Engine.cs", "source\n");
        repo.Write("tests/App.Tests/Audio/Tests.cs", "[Fact]\n");
        repo.Write("tools/audio/Private.cs", "private\n");
        repo.Write("experiments/Probe/native/device.c", "authored native\n");
        ProjectStatsReport report = repo.Collect();
        Assert.Equal(2, report.Totals.CSharpFiles);
        Assert.Equal(1, report.Totals.Facts);
        Assert.Equal(1, report.Ownership.Single(g => g.Subject == "Production").Totals.CSharpFiles);
        Assert.Equal(1, report.Kinds.Single(g => g.Subject == "C").Totals.Files);
    }

    [Fact]
    public void CountsFormatsAndKeepsOwnershipIsolated()
    {
        using var repo = new StatsRepository();
        repo.Write("src/App/Main.cs", "[Fact]\nclass App {}\n");
        repo.Write("tests/App.Tests/Tests.cs", "[Fact]\r\n[Theory]\r\n");
        repo.Write("tools/Tool/Tests/Fake.cs", "[Fact]\n");
        repo.Write("experiments/Probe/tests/Fake.cs", "[Theory]\n");
        repo.Write("eng/run.ps1", "Write-Output 'test'\n");
        repo.Write("src/App/View.axaml", "<Window />\n");
        repo.Write("src/App/View.xaml", "<Window />\n");
        repo.Write("docs/Guide.md", "# Guide\n");
        repo.Write("experiments/Probe/native.c", "void run() {}\n");
        repo.Write("experiments/Probe/native.cpp", "void cpp() {}\n");
        repo.Write("experiments/Probe/native.hpp", "void cpp();\n");
        repo.Write("src/App/image.png", "binary");
        ProjectStatsReport report = repo.Collect();
        Assert.Equal(12, report.Totals.Files);
        Assert.Equal(11, report.Totals.TextFiles);
        Assert.Equal(4, report.Totals.CSharpFiles);
        Assert.Equal(6, report.Totals.CSharpLines);
        Assert.Equal(1, report.Totals.Facts);
        Assert.Equal(1, report.Totals.Theories);
        Assert.Equal(1, report.Ownership.Single(g => g.Subject == "Production").Totals.CSharpFiles);
        Assert.Equal(1, report.Ownership.Single(g => g.Subject == "Tests").Totals.CSharpFiles);
        Assert.Equal(1, report.Ownership.Single(g => g.Subject == "Tooling").Totals.CSharpFiles);
        Assert.Equal(1, report.Ownership.Single(g => g.Subject == "Experiments").Totals.CSharpFiles);
        foreach (string kind in new[] { "Axaml", "Xaml", "Markdown", "BuildConfig", "C", "Cpp", "Header", "Other" })
            Assert.Equal(1, report.Kinds.Single(g => g.Subject == kind).Totals.Files);
    }

    [Theory]
    [InlineData("", 0, 0)]
    [InlineData("a", 1, 1)]
    [InlineData("a\n", 1, 2)]
    [InlineData("a\r\n", 1, 3)]
    [InlineData("a\r", 1, 2)]
    [InlineData("\n\n", 2, 2)]
    [InlineData("a\r\nb\rc\nlast", 4, 11)]
    [InlineData("Привет\n😀", 2, 9)]
    public void CountsUtf16CharactersAndLogicalLines(string content, long lines, long characters)
    {
        using var repo = new StatsRepository();
        repo.Write("src/App/Test.cs", content, bom: true);
        FileStats file = Assert.Single(new FileScanner().Scan(repo.Root).Files);
        Assert.Equal(lines, file.Lines);
        Assert.Equal(characters, file.Characters);
    }

    [Fact]
    public void TestAttributesAreAnExplicitLexicalEstimate()
    {
        using var repo = new StatsRepository();
        repo.Write("tests/App.Tests/Attributes.cs",
            "[Fact] [Theory()] [Xunit.FactAttribute]\n[global::Xunit.TheoryAttribute]\n[\n Fact ]\n" +
            "// [Fact]\nvar example = \"[Theory]\";\n[Factory] [TheoryOther] [InlineData(1)]\n");
        FileStats file = Assert.Single(new FileScanner().Scan(repo.Root).Files);
        Assert.Equal(4, file.Facts);
        Assert.Equal(3, file.Theories);
    }

    [Fact]
    public void ProjectsUseNearestContainingFolderAndOrdinalTies()
    {
        using var repo = new StatsRepository();
        repo.Write("Seqvium.sln", "solution\n");
        repo.Write("src/App/A.csproj", "<Project />\n");
        repo.Write("src/App/Z.csproj", "<Project />\n");
        repo.Write("src/App/A.cs", "outer\n");
        repo.Write("src/App/Nested/N.csproj", "<Project />\n");
        repo.Write("src/App/Nested/N.cs", "nested\n");
        repo.Write("tools/Orphan/File.cs", "orphan\n");
        ProjectStatsReport report = repo.Collect();
        Assert.Equal(3, report.Projects.Single(p => p.Subject == "src/App/A.csproj").Totals.Files);
        Assert.Equal(0, report.Projects.Single(p => p.Subject == "src/App/Z.csproj").Totals.Files);
        Assert.Equal(2, report.Projects.Single(p => p.Subject == "src/App/Nested/N.csproj").Totals.Files);
        Assert.Equal(2, report.Projects.Single(p => p.Subject == "[unassigned]").Totals.Files);
        Assert.Equal(["Seqvium.sln"], report.Solutions);
        Assert.Equal(3, report.Directories.Single(d => d.Subject == "src/App").DirectFiles);
        Assert.Equal(5, report.Directories.Single(d => d.Subject == "src/App").SubtreeFiles);
    }

    [Fact]
    public void SortingAndTopListsAreIndependentOfCreationOrder()
    {
        using var first = new StatsRepository();
        using var second = new StatsRepository();
        string[] paths = ["src/App/Z.cs", "src/App/A.cs", "tests/T/Z.cs", "docs/Z.md", "docs/A.md"];
        foreach (string path in paths) first.Write(path, "same\n");
        foreach (string path in paths.Reverse()) second.Write(path, "same\n");
        ProjectStatsReport a = first.Collect(1), b = second.Collect(1);
        Assert.Equal(new FileScanner().Scan(first.Root).Files, new FileScanner().Scan(second.Root).Files);
        Assert.Equal(a.Extensions, b.Extensions);
        Assert.Equal(a.Projects, b.Projects);
        Assert.Equal(a.Directories, b.Directories);
        Assert.Equal(a.Density, b.Density);
        Assert.All(a.Largest, g => Assert.InRange(g.Files.Count, 0, 1));
        Assert.Equal("src/App/A.cs", Assert.Single(a.Largest.Single(g => g.Category == "CSharp/Production").Files).Path);
        Assert.Equal("docs/A.md", Assert.Single(a.Largest.Single(g => g.Category == "Markdown").Files).Path);
    }

    [Theory]
    [InlineData(".git")]
    [InlineData(".idea")]
    [InlineData(".vs")]
    [InlineData(".vscode")]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData("packages")]
    [InlineData("publish")]
    [InlineData(".artifacts")]
    [InlineData(".scratch")]
    [InlineData("TestResults")]
    [InlineData("samples")]
    [InlineData("audio")]
    [InlineData("private")]
    [InlineData("tmp")]
    [InlineData("cmake-build-debug")]
    public void ExcludedTreesArePrunedBeforeOpeningFiles(string excluded)
    {
        using var repo = new StatsRepository();
        repo.Write("src/" + excluded + "/confidential.cs", "DO NOT READ");
        repo.Write("src/App/Public.cs", "public\n");
        var scanner = new FileScanner(path =>
        {
            Assert.Equal("Public.cs", Path.GetFileName(path));
            return new StringReader("public\n");
        });
        ScanResult scan = scanner.Scan(repo.Root);
        Assert.Single(scan.Files);
        Assert.Equal(1, scan.SkippedEntries);
    }

    [Fact]
    public void PrivacyExcludesLocalFilesAndNeverDisclosesPathsOrContent()
    {
        using var repo = new StatsRepository();
        string[] privatePaths = ["src/App/.env", "src/App/.env.production", "src/App/credentials.json",
            "src/App/device-ids.json", "src/App/appsettings.Development.json", "src/App/account.user",
            "src/App/Personal-song.seqvium", "src/App/song.wav", "src/App/secrets/key.pem",
            "private-owner/Confidential.cs", "docs/archive/Old.md", "src/App/token-123.cs",
            "src/App/user-preferences.json", "src/App/weird`name.cs", "src/App/a.generated.cs",
            "docs/experiments/personal-capture.png", "experiments/Probe/native/probe.dll"];
        foreach (string path in privatePaths) repo.Write(path, "TOKEN_AND_DEVICE_ID_CONTENT");
        repo.Write("src/App/Public.cs", "public\n");
        repo.Write("docs/PROJECT_STATS.md", "canonical\n");
        int opened = 0;
        var report = repo.Collect(scanner: new FileScanner(path =>
        { opened++; Assert.DoesNotContain("TOKEN", File.ReadAllText(path)); return new StringReader("public\n"); }));
        Assert.Equal(2, opened);
        Assert.Equal(2, report.Totals.Files);
        foreach (ReportFormat format in Enum.GetValues<ReportFormat>())
        {
            string output = ReportRenderer.Render(report, format);
            Assert.DoesNotContain(repo.Root, output);
            Assert.DoesNotContain("TOKEN_AND_DEVICE_ID_CONTENT", output);
            foreach (string path in privatePaths) Assert.DoesNotContain(path, output);
        }
    }

    [Fact]
    public void ReadFailuresCountFilesButExposeOnlyAggregateDiagnostics()
    {
        using var repo = new StatsRepository();
        repo.Write("src/App/A.cs", "a\n");
        ProjectStatsReport report = repo.Collect(scanner: new FileScanner(_ =>
            throw new IOException("PRIVATE_PATH_AND_TOKEN")));
        Assert.Equal(1, report.Totals.Files);
        Assert.Equal(1, report.UnreadableFiles);
        Assert.Equal(0, report.Totals.Characters);
        Diagnostic diagnostic = Assert.Single(report.Diagnostics);
        Assert.Equal("PST004", diagnostic.Code);
        Assert.Null(diagnostic.Subject);
        Assert.DoesNotContain("PRIVATE_PATH_AND_TOKEN", ReportRenderer.Render(report, ReportFormat.Json));
    }

    [Fact]
    public void InvalidUtf8IsUnreadable()
    {
        using var repo = new StatsRepository();
        string path = repo.Write("src/App/A.cs", "");
        File.WriteAllBytes(path, [0xff, 0xff, 0xff]);
        Assert.Equal(1, repo.Collect().UnreadableFiles);
    }

    [Fact]
    public void OversizedTextHasABoundedReadPolicy()
    {
        using var repo = new StatsRepository();
        string path = repo.Write("src/App/Huge.cs", "");
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write)) stream.SetLength(16 * 1024 * 1024 + 1);
        ProjectStatsReport report = repo.Collect(scanner: new FileScanner(_ => throw new InvalidOperationException("Must not open.")));
        Assert.Equal(0, report.Totals.Files);
        Assert.Equal("TextSizeLimit", Assert.Single(report.Skips).Reason);
        Assert.Equal("PST005", Assert.Single(report.Diagnostics).Code);
    }

    [Fact]
    public void JunctionOrSymlinkTreesNeverEscapeOrRecurse()
    {
        using var repo = new StatsRepository();
        using var outside = new StatsRepository();
        outside.Write("Confidential.cs", "PRIVATE_EXTERNAL_CONTENT");
        repo.Write("src/App/Public.cs", "public\n");
        string link = Path.Combine(repo.Root, "src", "Escape");
        if (OperatingSystem.IsWindows())
        {
            var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in new[] { "/c", "mklink", "/J", link, outside.Root }) start.ArgumentList.Add(argument);
            using Process process = Process.Start(start)!;
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }
        else Directory.CreateSymbolicLink(link, outside.Root);
        try
        {
            ScanResult scan = new FileScanner().Scan(repo.Root);
            Assert.Equal("src/App/Public.cs", Assert.Single(scan.Files).Path);
            Assert.Equal("ReparsePoint", Assert.Single(scan.Skips).Reason);
            Assert.Equal(1, scan.SkippedEntries);
            Assert.Equal(2, ProjectStatsApplication.Run([link], TextWriter.Null, TextWriter.Null));
            Assert.Equal(1, ProjectStatsApplication.Run([repo.Root, "--output", Path.Combine(link, "PROJECT_STATS.md")],
                TextWriter.Null, TextWriter.Null));
        }
        finally { Directory.Delete(link); }
        Assert.True(File.Exists(Path.Combine(outside.Root, "Confidential.cs")));
    }

    [Fact]
    public void ExclusiveFileLockIsCountedAsUnreadableOnWindows()
    {
        if (!OperatingSystem.IsWindows()) return; // Unix sharing semantics do not enforce a Windows exclusive open.
        using var repo = new StatsRepository();
        string path = repo.Write("src/App/A.cs", "a\n");
        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.Equal(1, repo.Collect().UnreadableFiles);
    }

    [Fact]
    public void UnixFileSymlinksIncludingDanglingLinksAreExcludedBeforeRead()
    {
        if (OperatingSystem.IsWindows()) return; // The Windows suite covers real junctions without requiring symlink privileges.
        using var repo = new StatsRepository();
        using var outside = new StatsRepository();
        string privateFile = outside.Write("Private.cs", "PRIVATE_CONTENT");
        Directory.CreateDirectory(Path.Combine(repo.Root, "src", "App"));
        string linked = Path.Combine(repo.Root, "src", "App", "Linked.cs");
        string dangling = Path.Combine(repo.Root, "src", "App", "Dangling.cs");
        File.CreateSymbolicLink(linked, privateFile);
        File.CreateSymbolicLink(dangling, Path.Combine(outside.Root, "Missing.cs"));
        ScanResult scan = new FileScanner(_ => throw new InvalidOperationException("Must not read links.")).Scan(repo.Root);
        Assert.Empty(scan.Files);
        Assert.Equal(2, scan.SkippedEntries);
        Assert.Equal("ReparsePoint", Assert.Single(scan.Skips).Reason);
    }

    [Fact]
    public void AdvisoryThresholdsAreStableAndNotAFailureGate()
    {
        using var repo = new StatsRepository();
        repo.Write("src/App/Large.cs", string.Concat(Enumerable.Repeat("code\n", 1000)));
        repo.Write("tests/App.Tests/Large.cs", string.Concat(Enumerable.Repeat("[Fact]\n", 2000)));
        ProjectStatsReport report = repo.Collect();
        Assert.Equal(["PST001", "PST002", "PST003"], report.Diagnostics.Select(d => d.Code));
        Assert.Equal(2000, report.Totals.Facts);
        Assert.Equal(0, ProjectStatsApplication.Run([repo.Root], TextWriter.Null, TextWriter.Null,
            readGit: _ => GitReader.Unavailable));
    }
}
