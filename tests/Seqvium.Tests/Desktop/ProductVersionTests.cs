// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Seqvium.Audio.Windows;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Xunit;

namespace Seqvium.Tests;

public sealed class ProductVersionTests
{
    private static string RepositoryRoot
    {
        get
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
                 directory is not null;
                 directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "Seqvium.sln")))
                    return directory.FullName;
            throw new InvalidOperationException("Version source tests require the repository checkout.");
        }
    }

    private static (string Display, string Numeric) CanonicalVersion()
    {
        var source = XDocument.Load(Path.Combine(RepositoryRoot, "Directory.Build.props"));
        string Component(string name) => source.Descendants("SeqviumVersion" + name).Single().Value;
        var build = int.Parse(Component("Build"), CultureInfo.InvariantCulture);
        Assert.InRange(build, 0, 9999);
        var prefix = string.Join('.', Component("Major"), Component("Minor"), Component("Patch"));
        return ($"{prefix}.{build.ToString("D4", CultureInfo.InvariantCulture)}", $"{prefix}.{build}");
    }

    [Theory]
    [InlineData("Core")]
    [InlineData("Audio.Windows")]
    [InlineData("Desktop")]
    public void BuiltProductionMetadataMatchesCanonicalComponents(string name)
    {
        var assembly = name switch
        {
            "Core" => typeof(ProjectDocument).Assembly,
            "Audio.Windows" => typeof(OutputFacts).Assembly,
            _ => typeof(ShellSession).Assembly
        };
        var expected = CanonicalVersion();
        var display = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Matches(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.[0-9]{4}$", display);
        Assert.Equal(expected.Display, display);
        Assert.DoesNotContain('+', display);
        Assert.DoesNotContain("0.0.1-dev", display);
        Assert.Equal(expected.Numeric, assembly.GetName().Version!.ToString());
        Assert.Equal(expected.Numeric, assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version);
        var file = FileVersionInfo.GetVersionInfo(assembly.Location);
        Assert.Equal(expected.Numeric, file.FileVersion);
        Assert.Equal(expected.Display, file.ProductVersion);
    }

    [Theory]
    [InlineData(HostLanguage.English, HostTheme.Dark)]
    [InlineData(HostLanguage.English, HostTheme.Light)]
    [InlineData(HostLanguage.Russian, HostTheme.Dark)]
    [InlineData(HostLanguage.Russian, HostTheme.Light)]
    public void AboutAccessorReadsDesktopMetadataInEveryPresentation(HostLanguage language, HostTheme theme)
    {
        using var directory = new TemporaryDirectory();
        using var session = new ShellSession(new(directory.File("preferences.json")), new(new(language, theme), false));
        var snapshot = session.Document.Current;
        Assert.Equal(
            typeof(ShellSession).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion,
            session.Version);
        Assert.Equal(CanonicalVersion().Display, session.Version);
        Assert.Same(snapshot, session.Document.Current);
        Assert.False(session.Document.IsDirty);
        Assert.Equal(0, session.Document.UndoCount);
    }

    [Theory]
    [InlineData(0, "0000")]
    [InlineData(1, "0001")]
    [InlineData(27, "0027")]
    [InlineData(9999, "9999")]
    public async Task EvaluatedBuildPaddingAndNumericMetadataUseOneComponent(int build, string padded)
    {
        var result = await Evaluate("src/Seqvium.Desktop/Seqvium.Desktop.csproj",
            "-property:SeqviumVersionBuild=" + build);
        Assert.Equal(0, result.ExitCode);
        var values = Properties(result.Output);
        var prefix = values["VersionPrefix"];
        Assert.Equal($"{prefix}.{padded}", values["SeqviumDisplayVersion"]);
        Assert.Equal($"{prefix}.{build}", values["SeqviumNumericVersion"]);
        AssertMetadata(values);
    }

    [Theory]
    [InlineData("SeqviumVersionBuild=-1")]
    [InlineData("SeqviumVersionBuild=10000")]
    [InlineData("SeqviumVersionBuild=0027")]
    [InlineData("SeqviumVersionBuild=0000")]
    [InlineData("SeqviumVersionMajor=65535")]
    [InlineData("SeqviumVersionMinor=01")]
    [InlineData("SeqviumVersionPatch=-1")]
    [InlineData("InformationalVersion=0.1.0.0000+unwanted")]
    [InlineData("AssemblyVersion=1.0.0.0")]
    [InlineData("IncludeSourceRevisionInInformationalVersion=true")]
    public async Task InvalidComponentsAndIndependentMetadataOverridesAreRejected(string property)
    {
        var result = await Evaluate("src/Seqvium.Desktop/Seqvium.Desktop.csproj", "-property:" + property);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Seqvium", result.Output + result.Error);
    }

    [Theory]
    [InlineData("src/Seqvium.Core/Seqvium.Core.csproj", true)]
    [InlineData("src/Seqvium.Audio.Windows/Seqvium.Audio.Windows.csproj", true)]
    [InlineData("src/Seqvium.Desktop/Seqvium.Desktop.csproj", true)]
    [InlineData("tests/Seqvium.Tests/Seqvium.Tests.csproj", false)]
    [InlineData("tools/Seqvium.DeviceCheck/Seqvium.DeviceCheck.csproj", false)]
    [InlineData("experiments/seq-r0/Probe.csproj", false)]
    public async Task EvaluatedProductionScopeExcludesTestsToolsAndIndependentProbe(string project, bool production)
    {
        var result = await Evaluate(project);
        Assert.Equal(0, result.ExitCode);
        var values = Properties(result.Output);
        Assert.Equal(production, values["SeqviumProductAssembly"] == "true");
        if (production)
        {
            AssertMetadata(values);
            Assert.Equal(CanonicalVersion().Display, values["InformationalVersion"]);
        }
        else
        {
            Assert.Equal("1.0.0", values["Version"]);
            Assert.Equal("1.0.0.0", values["AssemblyVersion"]);
            Assert.Equal("1.0.0.0", values["FileVersion"]);
        }
    }

    [Fact]
    public async Task UnrelatedArtifactCannotAcquireProductIdentityByProjectName()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.File("Seqvium.Desktop.csproj");
        new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("Import",
                    new XAttribute("Project", Path.Combine(RepositoryRoot, "Directory.Build.props"))),
                new XElement("PropertyGroup", new XElement("TargetFramework", "net10.0"))))
            .Save(path);
        var result = await Evaluate(path);
        Assert.Equal(0, result.ExitCode);
        Assert.NotEqual("true", Properties(result.Output)["SeqviumProductAssembly"]);
        Assert.Equal("1.0.0.0", Properties(result.Output)["AssemblyVersion"]);
    }

    [Fact]
    public void RootPropsIsTheOnlyProductVersionDefinitionAndAboutUsesTheAccessor()
    {
        var root = RepositoryRoot;
        var props = XDocument.Load(Path.Combine(root, "Directory.Build.props"));
        foreach (var name in new[] { "Major", "Minor", "Patch", "Build" })
            Assert.Single(props.Descendants("SeqviumVersion" + name));
        foreach (var project in Directory.EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
                     .Where(path => Path.GetExtension(path) is ".csproj" or ".props" or ".targets" &&
                                    !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")))
        {
            var xml = XDocument.Load(project);
            Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is
                                                                    "Version" or "VersionPrefix" or "VersionSuffix"
                                                                    or "AssemblyVersion" or "FileVersion"
                                                                    or "InformationalVersion" ||
                                                                element.Name.LocalName.StartsWith("SeqviumVersion",
                                                                    StringComparison.Ordinal));
        }

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
                     .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj") &&
                                    Path.GetExtension(path) is ".cs" or ".axaml"))
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("0.0.1-dev", source);
            Assert.DoesNotMatch(@"\[assembly:\s*Assembly(InformationalVersion|Version|FileVersion)", source);
        }

        Assert.Contains("Text = _shell.Version",
            File.ReadAllText(Path.Combine(root, "src/Seqvium.Desktop/Settings/SettingsWindow.cs")));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public void ProductIdentityDoesNotChangeProjectEnvelopeOrReaderGates(bool graphs, int minor)
    {
        var document = graphs ? new GraphFixture().Document : ProjectDocument.Create();
        var json = GraphFixture.Json(document);
        Assert.Equal("seqvium-project", json["format"]!.GetValue<string>());
        Assert.Equal(1, json["major"]!.GetValue<int>());
        Assert.Equal(minor, json["minor"]!.GetValue<int>());
        Assert.Equal(minor, json["minimumReaderMinor"]!.GetValue<int>());
        Assert.Equal("0.2.0-r2-f1", json["writerVersion"]!.GetValue<string>());
        Assert.Equal(10, ProjectPersistence.ReaderMinor);
        Assert.DoesNotContain(CanonicalVersion().Display, json.ToJsonString());
        Assert.True(JsonNode.DeepEquals(json, GraphFixture.Json(GraphFixture.Read(json).Document)));
        json["major"] = 0;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        json["major"] = 1;
        json["minor"] = ProjectPersistence.ReaderMinor + 1;
        json["minimumReaderMinor"] = ProjectPersistence.ReaderMinor + 1;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
    }

    private static void AssertMetadata(Dictionary<string, string> values)
    {
        Assert.Equal(values["SeqviumNumericVersion"], values["Version"]);
        Assert.Equal(values["SeqviumNumericVersion"], values["AssemblyVersion"]);
        Assert.Equal(values["SeqviumNumericVersion"], values["FileVersion"]);
        Assert.Equal(values["SeqviumDisplayVersion"], values["InformationalVersion"]);
        Assert.Equal("false", values["IncludeSourceRevisionInInformationalVersion"]);
    }

    private static Dictionary<string, string> Properties(string output) =>
        JsonNode.Parse(output)!["Properties"]!.Deserialize<Dictionary<string, string>>()!;

    // Evaluation runs no restore/build, GUI, device or network. Global properties vary only this process.
    private static async Task<(int ExitCode, string Output, string Error)> Evaluate(string project,
        params string[] properties)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = RepositoryRoot, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        foreach (var argument in new[]
                 {
                     "msbuild", project, "-nologo", "-property:Configuration=Release",
                     "-target:ValidateSeqviumProductVersion,GetAssemblyVersion",
                     "-getProperty:SeqviumProductAssembly,SeqviumDisplayVersion,SeqviumNumericVersion,VersionPrefix,Version,AssemblyVersion,FileVersion,InformationalVersion,IncludeSourceRevisionInInformationalVersion"
                 }.Concat(properties))
            start.ArgumentList.Add(argument);
        // The independent probe has its own props and deliberately no product validation target.
        if (project == "experiments/seq-r0/Probe.csproj")
            start.ArgumentList[4] = "-target:GetAssemblyVersion";
        using var process = Process.Start(start) ??
                            throw new InvalidOperationException("Cannot start MSBuild evaluation.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await output, await error);
    }
}
