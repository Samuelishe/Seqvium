// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class SourceAccessTests
{
    [Fact]
    public async Task SelectedDirectoryIsOneLevelAndPreservesDistinctEqualSources()
    {
        using var directory = new TemporaryDirectory();
        byte[] bytes = WavFixtures.Create([0.2f, -0.4f]);
        File.WriteAllBytes(directory.File("a.wav"), bytes);
        File.WriteAllBytes(directory.File("b.WAV"), bytes);
        File.WriteAllBytes(directory.File("broken.wav"), [1, 2, 3]);
        File.WriteAllText(directory.File("other.mp3"), "Not supported");
        Directory.CreateDirectory(directory.File("child"));
        File.WriteAllBytes(directory.File("child/hidden.wav"), bytes);
        var result = await SourceAccess.DiscoverDirectoryAsync(directory.Path,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.Truncated);
        Assert.Null(result.Diagnostic);
        Assert.Null(result.Lifecycle);
        Assert.Equal(4, result.Sources.Length);
        var supported = result.Sources.Where(item => item.Availability == SourceAvailability.Supported).ToArray();
        Assert.Equal(2, supported.Length);
        Assert.NotEqual(supported[0].ExternalPath, supported[1].ExternalPath);
        Assert.All(supported, item =>
        {
            Assert.Null(item.ResourceId);
            Assert.Equal(48000, item.Rate);
            Assert.Equal(2, item.Frames);
        });
        Assert.Equal(2, result.Sources.Count(item => item.Availability == SourceAvailability.Unsupported));
        Assert.DoesNotContain(result.Sources, item => item.Name == "hidden.wav");
    }

    [Fact]
    public async Task DiscoveryStopsAtExplicitCapacityAndCancellation()
    {
        using var directory = new TemporaryDirectory();
        for (int i = 0; i < 20; i++) File.WriteAllBytes(directory.File($"{i}.wav"), WavFixtures.Create([0.1f]));
        var result =
            await SourceAccess.DiscoverDirectoryAsync(directory.Path, 3, TestContext.Current.CancellationToken);
        Assert.True(result.Truncated);
        Assert.Equal(3, result.Sources.Length);
        Assert.NotNull(result.Diagnostic);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => SourceAccess.DiscoverDirectoryAsync(directory.Path,
            257,
            TestContext.Current.CancellationToken));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            SourceAccess.DiscoverDirectoryAsync(directory.Path, cancellationToken: cancelled.Token));
    }

    [Fact]
    public async Task MissingAndInaccessibleSourcesHaveSourceDiagnosticsNotProjectBlockers()
    {
        using var directory = new TemporaryDirectory();
        var missing =
            await SourceAccess.InspectExternalAsync(directory.File("missing.wav"),
                TestContext.Current.CancellationToken);
        Assert.Equal(SourceAvailability.Missing, missing.Availability);
        Assert.Null(missing.ResourceId);
        var noDirectory = await SourceAccess.DiscoverDirectoryAsync(directory.File("absent"),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(noDirectory.Sources);
        Assert.NotNull(noDirectory.Diagnostic);
        string lockedPath = directory.File("locked.wav");
        File.WriteAllBytes(lockedPath, WavFixtures.Create([0.1f]));
        if (OperatingSystem.IsWindows())
        {
            using var locked = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            var unavailable =
                await SourceAccess.InspectExternalAsync(lockedPath, TestContext.Current.CancellationToken);
            Assert.Equal(SourceAvailability.Inaccessible, unavailable.Availability);
            Assert.NotEmpty(unavailable.Diagnostic);
        }

        File.Delete(lockedPath);
        Assert.Equal(SourceAvailability.Missing,
            (await SourceAccess.InspectExternalAsync(lockedPath, TestContext.Current.CancellationToken)).Availability);
    }

    [Theory]
    [InlineData("healthy")]
    [InlineData("missing")]
    [InlineData("corrupt")]
    public async Task ProjectDiscoveryIdentifiesAcceptedResourcesAndReportsDegradedMedia(string condition)
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        var accepted = await WavFixtures.Import(document, directory, [0.25f]);
        string path = Directory.GetFiles(directory.File("owned"), "*.wav", SearchOption.AllDirectories).Single();
        if (condition == "missing") File.Delete(path);
        if (condition == "corrupt") File.WriteAllBytes(path, WavFixtures.Create([0.75f]));
        var before = document.Current;
        int undo = document.UndoCount;
        var result =
            await SourceAccess.DiscoverProjectAsync(document, cancellationToken: TestContext.Current.CancellationToken);
        var item = Assert.Single(result.Sources);
        Assert.Equal(accepted.ResourceId, item.ResourceId);
        Assert.Null(item.ExternalPath);
        Assert.Equal(condition == "healthy" ? SourceAvailability.Supported : SourceAvailability.Degraded,
            item.Availability);
        Assert.Equal(document.LifecycleId, result.Lifecycle);
        Assert.Equal(before.Revision, result.Revision);
        Assert.Same(before, document.Current);
        Assert.Equal(undo, document.UndoCount);
    }
}
