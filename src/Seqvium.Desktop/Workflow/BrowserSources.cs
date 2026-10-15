// SPDX-License-Identifier: Apache-2.0

using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal sealed record BrowserSource(string Name, string? Path = null,
    Id<ResourceDescriptor>? Resource = null, Guid Lifecycle = default, bool Directory = false,
    bool SamplerCompatible = false)
{
    public override string ToString() => (Directory ? "▸  " : "∿  ") + Name;
}

internal sealed record BrowserAudioInfo(int Rate, int Channels, int Frames, AudioWaveform Waveform);
internal sealed record BrowserPage(BrowserSource[] Entries, bool HasMore, int Visited);

/// <summary>One explicitly opened directory, one bounded worker page at a time. No recursion or audio decode.</summary>
internal sealed class BrowserDirectory(string path, string query) : IDisposable
{
    internal const int PageSize = 256, VisitBudget = 4096;
    private IEnumerator<string>? _cursor;
    private bool _ended;

    // The caller serializes Next and Dispose, including cancelled worker completion.
    internal Task<BrowserPage> NextAsync(CancellationToken cancellation) => Task.Run(() =>
    {
        if (_ended) return new BrowserPage([], false, 0);
        _cursor ??= System.IO.Directory.EnumerateFileSystemEntries(path).GetEnumerator();
        var results = new List<BrowserSource>();
        int visited = 0;
        while (visited < VisitBudget && results.Count < PageSize)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!_cursor.MoveNext()) { _ended = true; break; }
            visited++;
            string entry = _cursor.Current;
            string name = System.IO.Path.GetFileName(entry);
            bool directory;
            try { directory = (File.GetAttributes(entry) & FileAttributes.Directory) != 0; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { continue; }
            if (!directory && !AudioFileDecode.Supported(entry)) continue;
            if (!name.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
            results.Add(new(name, entry, Directory: directory,
                SamplerCompatible: !directory && System.IO.Path.GetExtension(entry).Equals(".wav", StringComparison.OrdinalIgnoreCase)));
        }
        cancellation.ThrowIfCancellationRequested();
        return new BrowserPage(results.OrderByDescending(e => e.Directory)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToArray(), !_ended, visited);
    }, cancellation);

    public void Dispose() { _ended = true; _cursor?.Dispose(); _cursor = null; }
}
