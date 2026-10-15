// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed partial class DesktopWorkflow
{
    internal ExtensionHost Extensions { get; } = Seqvium.Desktop.Extensions.PercussionGenerator.CreateHost();
    private readonly List<SampleGenerationSession> _generationSessions = [];
    private Task? _extensionInitialization;
    internal Task InitializeExtensionsAsync(string configurationDirectory) => _extensionInitialization ??= InitializeExtensionsCoreAsync(configurationDirectory);
    private async Task InitializeExtensionsCoreAsync(string configurationDirectory)
    {
        var store = new Presentation.ExtensionPreferencesStore(Path.Combine(configurationDirectory, "extensions.json"));
        var disabled = await store.LoadAsync();
        await Extensions.DiscoverAsync(Path.Combine(AppContext.BaseDirectory, "Extensions"));
        if (!store.Failed)
            foreach (var package in Extensions.Status.Where(p => p.Availability == ExtensionAvailability.Disabled && !disabled.Contains(p.Package.Identity)))
                await Extensions.EnableAsync(package.Package.Identity);
    }

    internal Task<bool> SetExtensionEnabledAsync(string package, bool enabled) => _owner.Run(async () =>
    {
        try
        {
            bool changed = enabled ? await Extensions.EnableAsync(package) : await Extensions.DisableAsync(package);
            if (changed) await RefreshLabGeneratorAsync();
            return changed;
        }
        finally { Publish(); }
    });

    internal Task<SampleGenerationSession> OpenGeneratorAsync(string package, string capability) => _owner.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        var session = new SampleGenerationSession(Extensions, package, capability);
        _generationSessions.Add(session);
        return Task.FromResult(session);
    });

    internal Task<bool> AcceptGeneratedRackAsync(ArrangementEditBasis basis, Id<GraphAttachment>? target, SampleGenerationSession session,
        SampleGenerationContext context, CancellationToken cancellation = default) => Command("Workflow.ImportFailed", async () =>
    {
        ValidateArrangementBasis(basis);
        if (!_generationSessions.Contains(session) || context.Lifecycle != _document.LifecycleId || context.Generation != _document.Generation)
            throw new InvalidOperationException("Extension.StaleResult");
        var candidate = session.Current(context);
        // Use the exact established Rack transaction and WAV import, including one atomic Undo entry.
        // Temporary transport to that path is host-owned and is never retained by the project.
        using var stream = candidate.OpenWave();
        string temporary = Path.Combine(Path.GetTempPath(), $"seqvium-candidate-{Guid.NewGuid():N}.wav");
        try
        {
            await using (var file = File.Create(temporary)) await stream.CopyToAsync(file, cancellation);
            ValidateArrangementBasis(basis); session.Current(context);
            return await UseBrowserRackCoreAsync(basis, target,
                new(candidate.Name + ".wav", temporary, SamplerCompatible: true), cancellation, commit =>
                    session.UseCurrent(context, current =>
                    {
                        if (current.Id != candidate.Id) throw new InvalidOperationException("Extension.StaleResult");
                        commit();
                    }));
        }
        finally { File.Delete(temporary); }
    });

    private async Task CloseGeneratorsAsync()
    {
        await CloseLabAsync();
        foreach (var session in _generationSessions) await session.DisposeAsync();
        _generationSessions.Clear();
    }

    private ExtensionHost.ExtensionUse? BorrowBrowserSource(BrowserSource source)
    {
        if (source.Package is not { } package) return null;
        var use = Extensions.Acquire(package, source.Capability!);
        try
        {
            if (use.ContentPath(source.Content!) != source.Path) throw new InvalidOperationException("Extension.Unavailable");
            return use;
        }
        catch { use.Dispose(); throw; }
    }
}
