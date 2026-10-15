// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Xunit;

namespace Seqvium.Tests;

public sealed class ExtensionFoundationTests
{
    private const string Package = "test.optional", Capability = "test.optional.sound";
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;
    private static byte[] Wave => WavFixtures.Create(Enumerable.Range(0, 480).Select(i => .2f * MathF.Sin(i * .1f)).ToArray(), 48000, floating: true);
    private static SampleGenerationContext Context => new(Guid.NewGuid(), 0, 48000, 4800);
    private static SampleGenerationRequest Request(SampleGenerationContext context, decimal value = .5m) =>
        new(context, ImmutableDictionary<string, decimal>.Empty.Add("amount", value));

    [Theory]
    [InlineData("json", "Extension.DamagedManifest")]
    [InlineData("schema", "Extension.ManifestVersion")]
    [InlineData("missing", "Extension.MissingFile")]
    [InlineData("path", "Extension.DamagedManifest")]
    [InlineData("absolute", "Extension.DamagedManifest")]
    [InlineData("version", "Extension.DamagedManifest")]
    [InlineData("null-capability", "Extension.DamagedManifest")]
    [InlineData("category", "Extension.DamagedManifest")]
    [InlineData("oversize", "Extension.DamagedManifest")]
    public async Task InvalidLocalPackagesRemainDiagnosableWithoutRunningCode(string failure, string diagnostic)
    {
        using var directory = new TemporaryDirectory();
        string package = CreatePackage(directory, false);
        string path = Path.Combine(package, "extension.json");
        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(path, Cancellation))!;
        switch (failure)
        {
            case "json": await File.WriteAllTextAsync(path, "broken", Cancellation); break;
            case "oversize": await File.WriteAllTextAsync(path, new string(' ', 65537), Cancellation); break;
            case "missing": File.Delete(Path.Combine(package, "Pulse.wav")); break;
            default:
                switch (failure)
                {
                    case "schema": manifest["schema"] = 9; break;
                    case "version": manifest["version"] = "yesterday"; break;
                    case "null-capability": manifest["capabilities"]![0] = null; break;
                    case "category": manifest["capabilities"]![0]!["category"] = "RealtimeEffect"; break;
                    case "path": manifest["capabilities"]![0]!["content"]![0]!["file"] = "../outside.wav"; break;
                    case "absolute": manifest["capabilities"]![0]!["content"]![0]!["file"] = directory.File("outside.wav"); break;
                }
                await File.WriteAllTextAsync(path, manifest.ToJsonString(), Cancellation); break;
        }
        await using var host = new ExtensionHost();
        await host.DiscoverAsync(directory.Path, Cancellation);
        Assert.Equal(ExtensionAvailability.Damaged, Assert.Single(host.Status).Availability);
        Assert.Contains(Assert.Single(host.Status).Diagnostics, d => d.Code == diagnostic);
        Assert.False(await host.EnableAsync(Path.GetFileName(package), Cancellation));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task CompatibilityUsesRequiredContractsAndOptionalFallbackInsteadOfProductAge()
    {
        using var directory = new TemporaryDirectory();
        string package = CreatePackage(directory, true, optional: true, version: "0.0.1");
        var provider = new Provider(); int activations = 0;
        await using var host = Host(_ => { activations++; return Task.FromResult<ISampleGenerator>(provider); }, "0.0.1");
        await host.DiscoverAsync(directory.Path, Cancellation);
        var manifest = Assert.Single(host.Status).Package.Manifest!;
        Assert.Equal("Optional sound", manifest.DisplayName(true));
        Assert.True(await host.EnableAsync(Package, Cancellation));
        Assert.Contains(Assert.Single(host.Status).Diagnostics, d => d.Code == "Extension.OptionalFallback");
        Assert.Equal(1, activations);
        Assert.True(await host.DisableAsync(Package)); Assert.Equal(1, provider.Disposals);
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(package, "extension.json")))!;
        json["capabilities"]![0]!["requires"]![0]!["minimum"] = 2;
        json["capabilities"]![0]!["requires"]![0]!["maximum"] = 4;
        File.WriteAllText(Path.Combine(package, "extension.json"), json.ToJsonString());
        await host.DiscoverAsync(directory.Path, Cancellation);
        Assert.Equal(ExtensionAvailability.Incompatible, Assert.Single(host.Status).Availability);
        Assert.False(await host.EnableAsync(Package, Cancellation)); Assert.Equal(1, activations);
    }

    [Fact]
    public async Task DuplicateIdentityAndUnregisteredExecutableNeverActivate()
    {
        using var directory = new TemporaryDirectory();
        string package = CreatePackage(directory, true);
        File.WriteAllText(Path.Combine(package, "Untrusted.dll"), "not executable code");
        await using var host = new ExtensionHost();
        await host.DiscoverAsync(directory.Path, Cancellation);
        Assert.False(await host.EnableAsync(Package, Cancellation));
        Assert.Contains(Assert.Single(host.Status).Diagnostics, d => d.Code == "Extension.ProviderUnavailable");
        Directory.CreateDirectory(directory.File("duplicate"));
        File.Copy(Path.Combine(package, "extension.json"), directory.File("duplicate/extension.json"));
        await host.DiscoverAsync(directory.Path, Cancellation);
        Assert.Equal(2, host.Status.Length);
        Assert.All(host.Status, p => Assert.Equal(ExtensionAvailability.Damaged, p.Availability));
        Assert.Throws<InvalidOperationException>(() => host.Acquire(Package, Capability));
    }

    [Fact]
    public async Task MissingRootIsOptionalAndMissingDeclaredDataRefusesReactivation()
    {
        using var directory = new TemporaryDirectory();
        await using var host = new ExtensionHost();
        await host.DiscoverAsync(directory.File("absent"), Cancellation); Assert.Empty(host.Status);
        Assert.Equal("Extension.Missing", Assert.Throws<InvalidOperationException>(() => host.Acquire(Package, Capability)).Message);
        string pack = CreatePackage(directory, false);
        await host.DiscoverAsync(directory.Path, Cancellation); Assert.True(await host.EnableAsync(Package, Cancellation));
        using (var use = host.Acquire(Package, Capability))
        {
            Assert.True(File.Exists(use.ContentPath("test.pulse")));
            Assert.False(await host.DisableAsync(Package));
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.DiscoverAsync(directory.Path, Cancellation));
        }
        Assert.True(await host.DisableAsync(Package));
        File.Delete(Path.Combine(pack, "Pulse.wav"));
        Assert.False(await host.EnableAsync(Package, Cancellation));
        Assert.Equal(ExtensionAvailability.Failed, Assert.Single(host.Status).Availability);
    }

    [Fact]
    public async Task ActivationFailureCanRetryAndDisposalFailureCannotBeMistakenForUnload()
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var provider = new Provider { FailDisposal = true }; int attempts = 0;
        await using var host = Host(_ => ++attempts == 1 ? throw new IOException("Provider initialization failed") : Task.FromResult<ISampleGenerator>(provider));
        await host.DiscoverAsync(directory.Path, Cancellation);
        Assert.False(await host.EnableAsync(Package, Cancellation));
        Assert.Equal(ExtensionAvailability.Failed, Assert.Single(host.Status).Availability);
        Assert.True(await host.EnableAsync(Package, Cancellation));
        Assert.False(await host.DisableAsync(Package));
        Assert.False(await host.EnableAsync(Package, Cancellation));
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.DiscoverAsync(directory.Path, Cancellation));
        Assert.Throws<InvalidOperationException>(() => host.Acquire(Package, Capability));
        provider.FailDisposal = false;
        Assert.True(await host.DisableAsync(Package));
        Assert.Equal(2, provider.Disposals);
    }

    [Fact]
    public async Task ActivationCancellationRefusesUseAndJoinsLateProvider()
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var entered = Signal(); var release = new TaskCompletionSource<ISampleGenerator>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Provider();
        await using var host = Host(_ => { entered.SetResult(); return release.Task; });
        await host.DiscoverAsync(directory.Path, Cancellation);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Cancellation);
        var enable = host.EnableAsync(Package, cancellation.Token);
        await entered.Task.WaitAsync(Cancellation);
        cancellation.Cancel(); Assert.False(await enable);
        Assert.Equal(ExtensionAvailability.Activating, Assert.Single(host.Status).Availability);
        Assert.False(await host.DisableAsync(Package));
        Assert.Throws<InvalidOperationException>(() => host.Acquire(Package, Capability));
        release.SetResult(provider);
        await host.DisposeAsync(); // Joins cancelled activation and disposes even its late result.
        Assert.Equal(1, provider.Disposals);
    }

    [Fact]
    public async Task ActivationDeadlineReturnsWithoutPublishingOrAbandoningTheTask()
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var release = new TaskCompletionSource<ISampleGenerator>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Provider();
        await using var host = Host(_ => release.Task);
        await host.DiscoverAsync(directory.Path, Cancellation);
        Assert.False(await host.EnableAsync(Package, Cancellation, TimeSpan.Zero));
        Assert.Throws<InvalidOperationException>(() => host.Acquire(Package, Capability));
        release.TrySetResult(provider);
        await host.DisposeAsync();
        Assert.NotEqual(ExtensionAvailability.Available, Assert.Single(host.Status).Availability);
    }

    [Fact]
    public async Task UnsupportedParameterSetsNeverRunProviderAndSessionsSerializeItsWork()
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var entered = Signal(); var release = Signal(); int calls = 0;
        var provider = new Provider { Work = async (_, _) =>
        { Interlocked.Increment(ref calls); entered.TrySetResult(); await release.Task; return new("Sample", Wave); } };
        await using var host = Host(_ => Task.FromResult<ISampleGenerator>(provider));
        await host.DiscoverAsync(directory.Path, Cancellation); Assert.True(await host.EnableAsync(Package, Cancellation));
        await using var first = new SampleGenerationSession(host, Package, Capability);
        await using var second = new SampleGenerationSession(host, Package, Capability);
        var context = Context;
        await Assert.ThrowsAsync<ArgumentException>(() => first.GenerateAsync(Request(context, 2), Cancellation));
        await Assert.ThrowsAsync<ArgumentException>(() => first.GenerateAsync(new(context, ImmutableDictionary<string, decimal>.Empty), Cancellation));
        Assert.Equal(0, calls);
        var a = first.GenerateAsync(Request(context), Cancellation); await entered.Task.WaitAsync(Cancellation);
        var b = second.GenerateAsync(Request(context), Cancellation);
        Assert.Equal(1, calls); release.SetResult();
        Assert.True(await a); Assert.True(await b); Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DocumentReplacementAndCloseCancelJoinPendingGenerationAndDisposeItsLateAudio(bool close)
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var entered = Signal(); var cancelled = Signal(); var release = Signal();
        var late = new SampleCandidate("Late", Wave);
        var provider = new Provider { Work = async (_, token) =>
        { entered.SetResult(); using var registration = token.Register(() => cancelled.TrySetResult()); await release.Task; return late; } };
        var host = Host(_ => Task.FromResult<ISampleGenerator>(provider));
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), mediaDirectory: directory.File("owned"), extensions: host);
        await host.DiscoverAsync(directory.Path, Cancellation); Assert.True(await host.EnableAsync(Package, Cancellation));
        var session = await workflow.OpenGeneratorAsync(Package, Capability);
        var state = workflow.State;
        var generation = session.GenerateAsync(Request(new(state.Document.LifecycleId, state.Generation, 48000, 4800)), Cancellation);
        await entered.Task.WaitAsync(Cancellation);
        var replacement = close ? workflow.CloseAsync(UnsavedDecision.Discard) : workflow.NewAsync(UnsavedDecision.Discard);
        await cancelled.Task.WaitAsync(Cancellation); Assert.False(replacement.IsCompleted);
        release.SetResult(); Assert.True(await replacement.WaitAsync(Cancellation));
        Assert.False(await generation); Assert.False(late.IsAvailable);
        Assert.True(state.Document.IsClosed); Assert.Empty(workflow.State.Snapshot.State.Resources);
        Assert.Equal(close ? 1 : 0, provider.Disposals);
    }

    [Fact]
    public async Task GenerationCancelKeepsEditorLeaseUntilCloseThenAllowsFreshActivation()
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var entered = Signal(); var cancelled = Signal();
        var provider = new Provider
        {
            Work = async (_, token) =>
            {
                entered.SetResult();
                using var registration = token.Register(() => cancelled.TrySetResult());
                await Task.Delay(Timeout.Infinite, token);
                throw new InvalidOperationException("Unreachable");
            }
        };
        await using var host = Host(_ => Task.FromResult<ISampleGenerator>(provider));
        await host.DiscoverAsync(directory.Path, Cancellation); Assert.True(await host.EnableAsync(Package, Cancellation));
        await using var session = new SampleGenerationSession(host, Package, Capability);
        var context = Context; var generate = session.GenerateAsync(Request(context), Cancellation);
        await entered.Task.WaitAsync(Cancellation);
        Assert.False(await host.DisableAsync(Package)); Assert.Equal(0, provider.Disposals);
        session.Cancel(); await cancelled.Task.WaitAsync(Cancellation);
        Assert.False(await generate); Assert.Null(session.Failure);
        Assert.False(await host.DisableAsync(Package)); // Open dependent editor still owns code.
        await session.DisposeAsync(); Assert.True(await host.DisableAsync(Package)); Assert.Equal(1, provider.Disposals);
        Assert.True(await host.EnableAsync(Package, Cancellation));
        await using var fresh = new SampleGenerationSession(host, Package, Capability);
        provider.Work = (_, _) => Task.FromResult(new SampleCandidate("Fresh", Wave));
        Assert.True(await fresh.GenerateAsync(Request(context), Cancellation));
        Assert.True(fresh.Current(context).IsAvailable);
    }

    [Fact]
    public async Task SupersededLateResultsAreDisposedAndStaleContextCannotBeAccepted()
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var entered = Signal(); var release = Signal(); var late = new SampleCandidate("Late", Wave);
        int calls = 0;
        var provider = new Provider { Work = async (_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1) { entered.SetResult(); await release.Task; return late; }
            return new("Current", Wave);
        } };
        await using var host = Host(_ => Task.FromResult<ISampleGenerator>(provider));
        await host.DiscoverAsync(directory.Path, Cancellation); Assert.True(await host.EnableAsync(Package, Cancellation));
        await using var session = new SampleGenerationSession(host, Package, Capability);
        var context = Context;
        var old = session.GenerateAsync(Request(context), Cancellation); await entered.Task.WaitAsync(Cancellation);
        var current = session.GenerateAsync(Request(context, .7m), Cancellation); release.SetResult();
        Assert.False(await old); Assert.True(await current); Assert.False(late.IsAvailable);
        var accepted = session.Current(context);
        Assert.Throws<InvalidOperationException>(() => session.Current(context with { Generation = 1 }));
        Assert.Throws<InvalidOperationException>(() => session.Current(context with { Lifecycle = Guid.NewGuid() }));
        session.Cancel(); Assert.False(accepted.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => session.Current(context));
    }

    [Theory]
    [InlineData("error")]
    [InlineData("frames")]
    [InlineData("rate")]
    [InlineData("corrupt")]
    public async Task FailedGeneratorOutputIsDisposedAndNextRequestCanRecover(string failure)
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        SampleCandidate? result = null;
        var provider = new Provider { Work = (_, _) =>
        {
            if (failure == "error") throw new IOException("Generation failed");
            result = new("Result", failure == "corrupt" ? [1, 2, 3] : failure == "rate"
                ? WavFixtures.Create([.1f, .2f], 44100, floating: true) : Wave);
            return Task.FromResult(result);
        } };
        await using var host = Host(_ => Task.FromResult<ISampleGenerator>(provider));
        await host.DiscoverAsync(directory.Path, Cancellation); Assert.True(await host.EnableAsync(Package, Cancellation));
        await using var session = new SampleGenerationSession(host, Package, Capability);
        var context = Context with { MaximumFrames = failure == "frames" ? 1 : 4800 };
        Assert.False(await session.GenerateAsync(Request(context), Cancellation));
        Assert.NotNull(session.Failure); if (result is not null) Assert.False(result.IsAvailable);
        provider.Work = (_, _) => Task.FromResult(new SampleCandidate("Recovered", Wave));
        Assert.True(await session.GenerateAsync(Request(Context), Cancellation)); Assert.Null(session.Failure);
    }

    [Fact]
    public async Task HostShutdownCancelsWorkAndWaitsForOpenEditorAndLateResult()
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var entered = Signal(); var cancelled = Signal(); var release = Signal();
        var candidate = new SampleCandidate("Late", Wave);
        var provider = new Provider { Work = async (_, token) =>
        { entered.SetResult(); using var registration = token.Register(() => cancelled.TrySetResult()); await release.Task; return candidate; } };
        await using var host = Host(_ => Task.FromResult<ISampleGenerator>(provider));
        await host.DiscoverAsync(directory.Path, Cancellation); Assert.True(await host.EnableAsync(Package, Cancellation));
        await using var session = new SampleGenerationSession(host, Package, Capability);
        var generate = session.GenerateAsync(Request(Context), Cancellation); await entered.Task.WaitAsync(Cancellation);
        var shutdown = host.DisposeAsync().AsTask(); await cancelled.Task.WaitAsync(Cancellation);
        Assert.False(shutdown.IsCompleted); Assert.Equal(0, provider.Disposals);
        var close = session.DisposeAsync().AsTask(); Assert.False(close.IsCompleted);
        release.SetResult(); await close.WaitAsync(Cancellation); await shutdown.WaitAsync(Cancellation);
        Assert.False(await generate); Assert.False(candidate.IsAvailable); Assert.Equal(1, provider.Disposals);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ImportedPackAudioSurvivesDisableRemovalUndoAndPortableReopen(bool playlist)
    {
        using var directory = new TemporaryDirectory(); string pack = CreatePackage(directory, false);
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), mediaDirectory: directory.File("owned"));
        await workflow.Extensions.DiscoverAsync(directory.Path, Cancellation); Assert.True(await workflow.Extensions.EnableAsync(Package, Cancellation));
        var source = new BrowserSource("Pulse.wav", Path.Combine(pack, "Pulse.wav"), SamplerCompatible: true,
            Package: Package, Capability: Capability, Content: "test.pulse");
        var before = workflow.State.Snapshot;
        Assert.Equal(480, (await workflow.InspectBrowserSourceAsync(source, Cancellation)).Frames);
        Assert.Same(before, workflow.State.Snapshot);
        Assert.True(playlist
            ? await workflow.UseBrowserAudioAsync(ArrangementEditBasis.From(workflow.State), source, default, 0, Cancellation)
            : await workflow.UseBrowserRackAsync(ArrangementEditBasis.From(workflow.State), null, source, Cancellation));
        var resource = Assert.Single(workflow.State.Snapshot.State.Resources);
        Assert.Equal(1, workflow.State.UndoCount);
        if (!playlist)
        {
            var state = workflow.State;
            var pattern = Assert.Single(state.Snapshot.State.Patterns);
            Assert.True(await workflow.ToggleBeatStepAsync(new(state.Document.LifecycleId, state.Generation,
                state.Selected!.Value, pattern.Id), pattern.Parts[0].Id, 0));
        }
        int musicalUndo = workflow.State.UndoCount;
        Assert.True(await workflow.Extensions.DisableAsync(Package));
        Assert.False(await workflow.UseBrowserRackAsync(ArrangementEditBasis.From(workflow.State), workflow.State.Selected, source, Cancellation));
        Assert.Equal(musicalUndo, workflow.State.UndoCount);
        File.Delete(Path.Combine(pack, "Pulse.wav")); File.Delete(Path.Combine(pack, "extension.json")); Directory.Delete(pack);
        Assert.True(await workflow.UndoAsync()); Assert.True(await workflow.UndoAsync(redo: true));
        Assert.True(await workflow.SaveAsync(directory.File("project.seqvium")));
        Assert.True(await workflow.SaveAsync(directory.File("portable/project.seqvium")));
        Assert.True(await workflow.OpenAsync(directory.File("portable/project.seqvium"), UnsavedDecision.Discard));
        using var pcm = AudioMedia.Decode(workflow.State.Document, resource.Id);
        Assert.Equal(480, pcm.Frames); Assert.NotEqual(0, pcm.Sample(20, 0));
        Assert.Empty(workflow.State.Document.SavedMediaDiagnostics);
        string rendered = directory.File("disabled-pack.wav");
        if (playlist) await PatternWaveExport.ExportArrangementAsync(workflow.State.Document, rendered, Cancellation);
        else await PatternWaveExport.ExportAsync(workflow.State.Document, workflow.State.Selected!.Value, rendered, Cancellation);
        using var render = WavDecoder.Decode(await File.ReadAllBytesAsync(rendered, Cancellation));
        Assert.Contains(Enumerable.Range(0, render.Frames), f => render.Sample(f, 0) != 0);
    }

    [Theory]
    [InlineData("accept")] [InlineData("stale")] [InlineData("new")] [InlineData("close")]
    public async Task GeneratorDocumentBoundaryAcceptsOnceOrDiscardsWithoutMutatingAnotherDocument(string action)
    {
        using var directory = new TemporaryDirectory(); CreatePackage(directory, true);
        var provider = new Provider();
        var host = Host(_ => Task.FromResult<ISampleGenerator>(provider));
        await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), mediaDirectory: directory.File("owned"), extensions: host);
        await host.DiscoverAsync(directory.Path, Cancellation); Assert.True(await host.EnableAsync(Package, Cancellation));
        var session = await workflow.OpenGeneratorAsync(Package, Capability);
        var state = workflow.State;
        var context = new SampleGenerationContext(state.Document.LifecycleId, state.Generation, 48000, 4800);
        Assert.True(await session.GenerateAsync(Request(context), Cancellation));
        var candidate = session.Current(context); Assert.Equal(0, state.UndoCount);
        switch (action)
        {
            case "new": Assert.True(await workflow.NewAsync(UnsavedDecision.Discard)); break;
            case "close": Assert.True(await workflow.CloseAsync(UnsavedDecision.Discard)); break;
            case "stale": Assert.True(await workflow.CreateBeatAsync(state.Document.LifecycleId, state.Generation, "Changed")); break;
            default:
                Assert.True(await workflow.AcceptGeneratedRackAsync(ArrangementEditBasis.From(state), state.Selected, session, context, Cancellation));
                Assert.Equal(1, workflow.State.UndoCount); Assert.Single(workflow.State.Snapshot.State.Resources);
                Assert.False(await workflow.AcceptGeneratedRackAsync(ArrangementEditBasis.From(workflow.State), workflow.State.Selected, session, context, Cancellation));
                await session.DisposeAsync(); Assert.True(await host.DisableAsync(Package));
                string path = directory.File("generated.seqvium"); Assert.True(await workflow.SaveAsync(path));
                Assert.True(await workflow.OpenAsync(path, UnsavedDecision.Discard));
                using (var pcm = AudioMedia.Decode(workflow.State.Document, workflow.State.Snapshot.State.Resources.Single().Id))
                    Assert.Equal(480, pcm.Frames);
                return;
        }
        Assert.False(candidate.IsAvailable);
        if (action != "close") Assert.False(await workflow.AcceptGeneratedRackAsync(ArrangementEditBasis.From(workflow.State), workflow.State.Selected, session, context, Cancellation));
        Assert.Empty(workflow.State.Snapshot.State.Resources);
    }

    [Fact]
    public async Task ExtensionPreferencesPreserveUnsupportedOriginalAndRetainMissingPackageChoices()
    {
        using var directory = new TemporaryDirectory(); string path = directory.File("extensions.json");
        var store = new ExtensionPreferencesStore(path); Assert.Empty(await store.LoadAsync());
        Assert.True(await store.SaveAsync([Package, "missing.generator"]));
        Assert.Equal(["missing.generator", Package], await new ExtensionPreferencesStore(path).LoadAsync());
        await File.WriteAllTextAsync(path, "{\"version\":2,\"disabled\":[]}", Cancellation);
        store = new(path); Assert.Empty(await store.LoadAsync()); Assert.True(store.Failed);
        Assert.False(await store.SaveAsync([])); Assert.Contains("\"version\":2", File.ReadAllText(path));
        foreach (string key in HostLocalizer.English.Keys.Where(k => k.StartsWith("Extension.", StringComparison.Ordinal)))
            Assert.True(HostLocalizer.Russian.ContainsKey(key), key);
    }

    private static ExtensionHost Host(Func<CancellationToken, Task<ISampleGenerator>> factory, string version = "1.0.0") => new(
        new Dictionary<ExtensionProviderKey, Func<CancellationToken, Task<ISampleGenerator>>> { [new(Package, version, Capability)] = factory });
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static string CreatePackage(TemporaryDirectory directory, bool generator, bool optional = false, string version = "1.0.0")
    {
        string path = directory.File(Package); Directory.CreateDirectory(path);
        var required = new List<object> { new { contract = generator ? "seqvium.sample-generator" : "seqvium.content.wav", minimum = 1, maximum = 1, optional = false } };
        if (optional) required.Add(new { contract = "optional.future-style", minimum = 1, maximum = 1, optional = true });
        File.WriteAllText(Path.Combine(path, "extension.json"), JsonSerializer.Serialize(new
        {
            schema = 1, id = Package, version, name = "Optional sound", russianName = (string?)null,
            capabilities = new[] { new { id = Capability, category = generator ? "SampleGenerator" : "ContentPack", requires = required,
                content = generator ? Array.Empty<object>() : [new { id = "test.pulse", name = "Pulse.wav", file = "Pulse.wav" }] } }
        }));
        if (!generator) File.WriteAllBytes(Path.Combine(path, "Pulse.wav"), Wave);
        return path;
    }
    private sealed class Provider : ISampleGenerator
    {
        internal int Disposals;
        internal bool FailDisposal;
        internal Func<SampleGenerationRequest, CancellationToken, Task<SampleCandidate>> Work = (_, _) => Task.FromResult(new SampleCandidate("Probe", Wave));
        public ImmutableArray<SampleParameter> Parameters => [new("amount", 0, 1, .5m)];
        public Task<SampleCandidate> GenerateAsync(SampleGenerationRequest request, CancellationToken cancellation) => Work(request, cancellation);
        public ValueTask DisposeAsync()
        { Disposals++; if (FailDisposal) throw new IOException("Shutdown refused"); return ValueTask.CompletedTask; }
    }
}
