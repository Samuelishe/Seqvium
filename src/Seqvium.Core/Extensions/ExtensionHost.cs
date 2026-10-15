// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Core;

public sealed record ExtensionStatus(ExtensionPackage Package, ExtensionAvailability Availability,
    int Uses, ImmutableArray<ExtensionDiagnostic> Diagnostics);
public sealed record ExtensionProviderKey(string Package, string Version, string Capability);

/// <summary>Host-registered trusted factories only; manifests never name an assembly, type or executable.</summary>
public sealed class ExtensionHost : IAsyncDisposable
{
    internal sealed class Entry(ExtensionPackage package)
    {
        internal readonly ExtensionPackage Package = package;
        internal ExtensionAvailability State = InitialState(package);
        internal ImmutableArray<ExtensionDiagnostic> Diagnostics = package.Diagnostics;
        internal readonly Dictionary<string, ISampleGenerator> Generators = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, SemaphoreSlim> WorkGates = new(StringComparer.Ordinal);
        internal readonly CancellationTokenSource Stop = new();
        internal CancellationTokenSource? ActivationCancellation;
        internal Task? Activation;
        internal Task? Retirement;
        internal bool Transition;
        internal int Uses;
        internal TaskCompletionSource Drained = Completed();
        private static TaskCompletionSource Completed() { var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); result.SetResult(); return result; }
    }

    private readonly object _sync = new();
    private readonly Dictionary<ExtensionProviderKey, Func<CancellationToken, Task<ISampleGenerator>>> _factories;
    private readonly List<Entry> _entries = [];
    private bool _closed;
    private Task? _shutdown;
    public ExtensionHost(IEnumerable<KeyValuePair<ExtensionProviderKey, Func<CancellationToken, Task<ISampleGenerator>>>>? providers = null)
        => _factories = providers?.ToDictionary() ?? [];

    public ImmutableArray<ExtensionStatus> Status
    {
        get { lock (_sync) return _entries.Select(e => new ExtensionStatus(e.Package, e.State, e.Uses, e.Diagnostics)).ToImmutableArray(); }
    }

    private static ExtensionAvailability InitialState(ExtensionPackage package) => package.Manifest is null ||
        package.Diagnostics.Any(d => d.Code is "Extension.DuplicateIdentity" or "Extension.MissingFile") ? ExtensionAvailability.Damaged :
        package.Diagnostics.Any(d => d.Code == "Extension.RequiredContract") ? ExtensionAvailability.Incompatible : ExtensionAvailability.Disabled;

    public async Task DiscoverAsync(string root, CancellationToken cancellation = default)
    {
        var packages = await ExtensionDiscovery.DiscoverAsync(root, cancellation).ConfigureAwait(false);
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            // Refresh cannot retire a loaded provider or a live borrower. Disable first.
            if (_entries.Any(e => e.Transition || e.Uses != 0 || e.Generators.Count != 0 || e.State is ExtensionAvailability.Available or ExtensionAvailability.Activating))
                throw new InvalidOperationException("Extension.Busy");
            foreach (var entry in _entries) { entry.Stop.Dispose(); entry.ActivationCancellation?.Dispose(); }
            _entries.Clear();
            _entries.AddRange(packages.Select(p => new Entry(p)));
        }
    }

    public async Task<bool> EnableAsync(string package, CancellationToken cancellation = default, TimeSpan? activationLimit = null)
    {
        Entry entry;
        Task activation;
        CancellationTokenSource activationCancellation;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            entry = Find(package);
            if (entry.State == ExtensionAvailability.Available) return true;
            if (entry.Transition || entry.State is not (ExtensionAvailability.Disabled or ExtensionAvailability.Failed) || entry.Generators.Count != 0)
                return false;
            if (entry.Package.Manifest!.Capabilities.Any(c => c.Category == ExtensionCategory.SampleGenerator &&
                !_factories.ContainsKey(new(package, entry.Package.Manifest.Version, c.Id))))
            {
                entry.State = ExtensionAvailability.Incompatible;
                entry.Diagnostics = entry.Package.Diagnostics.Add(new("Extension.ProviderUnavailable"));
                return false;
            }
            entry.State = ExtensionAvailability.Activating;
            entry.Transition = true;
            entry.ActivationCancellation?.Dispose();
            entry.ActivationCancellation = activationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation, entry.Stop.Token);
            entry.Activation = activation = Task.Run(() => ActivateAsync(entry));
        }
        try { await activation.WaitAsync(activationLimit ?? TimeSpan.FromSeconds(5), cancellation).ConfigureAwait(false); }
        catch (Exception error) when (error is TimeoutException or OperationCanceledException)
        {
            lock (_sync)
                if (entry.Activation == activation)
                {
                    activationCancellation.Cancel();
                    entry.Diagnostics = entry.Diagnostics.Add(new("Extension.ActivationCancelled"));
                    if (!_closed) entry.Retirement = Task.Run(() => RejectActivationAsync(entry, activation));
                }
            // Activation stays supervised and unavailable until its actual exit; no unload underneath code.
            return false;
        }
        lock (_sync)
        {
            // Readiness is not activation authority. Publish only after the caller's deadline/cancel gate wins.
            if (_closed) return false; // Shutdown owns the already-supervised activation and retirement.
            if (entry.State == ExtensionAvailability.Activating)
            {
                if (activationCancellation.IsCancellationRequested)
                {
                    entry.Retirement = Task.Run(() => RejectActivationAsync(entry, activation));
                    return false;
                }
                entry.State = ExtensionAvailability.Available;
            }
            entry.Transition = false;
            return entry.State == ExtensionAvailability.Available;
        }
    }

    private async Task RejectActivationAsync(Entry entry, Task activation)
    {
        await activation.ConfigureAwait(false);
        await RetireAsync(entry, ExtensionAvailability.Failed, new("Extension.ActivationCancelled")).ConfigureAwait(false);
    }

    private async Task ActivateAsync(Entry entry)
    {
        var created = new Dictionary<string, ISampleGenerator>(StringComparer.Ordinal);
        var cancellation = entry.ActivationCancellation!.Token;
        try
        {
            foreach (var capability in entry.Package.Manifest!.Capabilities)
            {
                cancellation.ThrowIfCancellationRequested();
                foreach (var content in capability.Content) ExtensionDiscovery.ResolveContent(entry.Package.Directory, content.File);
                if (capability.Category == ExtensionCategory.SampleGenerator)
                    created.Add(capability.Id, await _factories[new(entry.Package.Identity, entry.Package.Manifest.Version, capability.Id)](cancellation).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("Provider returned no instance."));
            }
            cancellation.ThrowIfCancellationRequested();
            lock (_sync)
            {
                if (_closed) throw new OperationCanceledException(cancellation);
                cancellation.ThrowIfCancellationRequested();
                foreach (var pair in created)
                { entry.Generators.Add(pair.Key, pair.Value); entry.WorkGates.Add(pair.Key, new(1)); }
                created.Clear();
                entry.Diagnostics = entry.Package.Diagnostics;
            }
        }
        catch (Exception error)
        {
            var failures = new List<ExtensionDiagnostic> { new("Extension.ActivationFailed", error.Message) };
            foreach (var pair in created)
            {
                try { await pair.Value.DisposeAsync().ConfigureAwait(false); }
                catch (Exception disposeError)
                {
                    failures.Add(new("Extension.ShutdownFailed", disposeError.Message));
                    lock (_sync) entry.Generators.TryAdd(pair.Key, pair.Value);
                }
            }
            lock (_sync) { entry.State = ExtensionAvailability.Failed; entry.Diagnostics = entry.Package.Diagnostics.AddRange(failures); }
        }
    }

    public async Task<bool> DisableAsync(string package)
    {
        Entry entry;
        Task retirement;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            entry = Find(package);
            if (entry.Uses != 0 || entry.Transition)
            { entry.Diagnostics = entry.Package.Diagnostics.Add(new("Extension.Busy")); return false; }
            if (entry.State is ExtensionAvailability.Damaged or ExtensionAvailability.Incompatible or ExtensionAvailability.Disabled) return true;
            entry.Transition = true;
            entry.State = ExtensionAvailability.Deactivating; // Refuse new leases before awaiting disposal.
            entry.Retirement = retirement = Task.Run(() => RetireAsync(entry));
        }
        await retirement.ConfigureAwait(false);
        lock (_sync) return entry.State == ExtensionAvailability.Disabled;
    }

    private async Task RetireAsync(Entry entry, ExtensionAvailability success = ExtensionAvailability.Disabled,
        ExtensionDiagnostic? reason = null)
    {
        var failures = new List<ExtensionDiagnostic>();
        foreach (var pair in entry.Generators.ToArray())
        {
            try
            {
                await pair.Value.DisposeAsync().ConfigureAwait(false);
                lock (_sync)
                {
                    entry.Generators.Remove(pair.Key);
                    if (entry.WorkGates.Remove(pair.Key, out var gate)) gate.Dispose();
                }
            }
            catch (Exception error) { failures.Add(new("Extension.ShutdownFailed", error.Message)); }
        }
        lock (_sync)
        {
            entry.State = failures.Count == 0 ? success : ExtensionAvailability.Failed;
            entry.Diagnostics = entry.Package.Diagnostics.AddRange(failures);
            if (reason is not null) entry.Diagnostics = entry.Diagnostics.Add(reason);
            entry.Transition = false;
        }
    }

    public ExtensionUse Acquire(string package, string capability)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            var entry = Find(package);
            if (entry.State != ExtensionAvailability.Available || entry.Transition ||
                !entry.Package.Manifest!.Capabilities.Any(c => c.Id == capability)) throw new InvalidOperationException("Extension.Unavailable");
            if (entry.Uses++ == 0) entry.Drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return new(this, entry, capability);
        }
    }

    private Entry Find(string package)
    {
        var matches = _entries.Where(e => e.Package.Identity == package).Take(2).ToArray();
        return matches.Length switch { 1 => matches[0], 0 => throw new InvalidOperationException("Extension.Missing"),
            _ => throw new InvalidOperationException("Extension.DuplicateIdentity") };
    }

    public sealed class ExtensionUse : IDisposable
    {
        private readonly ExtensionHost _host;
        private Entry? _entry;
        private readonly string _capability;
        internal ExtensionUse(ExtensionHost host, Entry entry, string capability) { _host = host; _entry = entry; _capability = capability; }
        public CancellationToken Cancellation { get { lock (_host._sync) return (_entry ?? throw new ObjectDisposedException(nameof(ExtensionUse))).Stop.Token; } }
        internal ISampleGenerator Generator
        {
            get { lock (_host._sync) return (_entry ?? throw new ObjectDisposedException(nameof(ExtensionUse))).Generators.GetValueOrDefault(_capability)
                ?? throw new InvalidOperationException("Extension.ProviderUnavailable"); }
        }
        internal async Task<SampleCandidate> GenerateAsync(SampleGenerationRequest request, CancellationToken cancellation)
        {
            ISampleGenerator generator;
            SemaphoreSlim gate;
            lock (_host._sync)
            {
                generator = Generator;
                gate = _entry!.WorkGates[_capability];
            }
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try { cancellation.ThrowIfCancellationRequested(); return await generator.GenerateAsync(request, cancellation).ConfigureAwait(false); }
            finally { gate.Release(); }
        }
        public string ContentPath(string content)
        {
            lock (_host._sync)
            {
                var entry = _entry ?? throw new ObjectDisposedException(nameof(ExtensionUse));
                var item = entry.Package.Manifest!.Capabilities.Single(c => c.Id == _capability).Content.SingleOrDefault(c => c.Id == content)
                    ?? throw new InvalidOperationException("Extension.MissingFile");
                return ExtensionDiscovery.ResolveContent(entry.Package.Directory, item.File);
            }
        }
        public void Dispose()
        {
            lock (_host._sync)
            {
                if (_entry is not { } entry) return;
                _entry = null;
                if (--entry.Uses == 0) entry.Drained.TrySetResult();
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_sync) { _closed = true; return new(_shutdown ??= Task.Run(ShutdownAsync)); }
    }
    private async Task ShutdownAsync()
    {
        // Close dependent host sessions/editors first. Their finally blocks release known borrowers.
        foreach (var entry in _entries) entry.Stop.Cancel();
        await Task.WhenAll(_entries.Where(e => e.Activation is not null).Select(e => e.Activation!)).ConfigureAwait(false);
        await Task.WhenAll(_entries.Where(e => e.Retirement is not null).Select(e => e.Retirement!)).ConfigureAwait(false);
        await Task.WhenAll(_entries.Select(e => e.Drained.Task)).ConfigureAwait(false);
        foreach (var entry in _entries)
        {
            await RetireAsync(entry).ConfigureAwait(false);
            foreach (var gate in entry.WorkGates.Values) gate.Dispose();
            entry.ActivationCancellation?.Dispose(); entry.Stop.Dispose();
        }
    }
}
