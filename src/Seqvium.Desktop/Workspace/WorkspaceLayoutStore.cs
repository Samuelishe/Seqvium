// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

public sealed record WorkspaceLayoutLoad(WorkspaceLayout? Layout, bool UsedFallback);

/// <summary>A bounded, independent user file. A damaged/future file is read-only for this store lifetime.</summary>
public sealed class WorkspaceLayoutStore(string path)
{
    public const int MaximumBytes = 32768;
    private readonly string _path = Path.GetFullPath(path);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _preserveOriginal;

    public static string DefaultPath() =>
        Path.Combine(Path.GetDirectoryName(PreferenceStore.DefaultPath())!, "workspace-layout.json");

    public async Task<WorkspaceLayoutLoad> LoadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var load = await Task.Run(Load);
            _preserveOriginal |= load.UsedFallback;
            return load;
        }
        finally
        {
            _gate.Release();
        }
    }

    private WorkspaceLayoutLoad Load()
    {
        try
        {
            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            // Read at most the budget + sentinel even if another writer grows the file.
            var bytes = new byte[MaximumBytes + 1];
            var count = 0;
            while (count < bytes.Length)
            {
                var read = stream.Read(bytes, count, bytes.Length - count);
                if (read == 0) break;
                count += read;
            }

            if (count > MaximumBytes) return new(null, true);
            using var json = JsonDocument.Parse(bytes.AsMemory(0, count), new() { MaxDepth = 16 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("version", out var version) ||
                version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number) || number != 1 ||
                !root.TryGetProperty("panes", out var panes) || panes.ValueKind != JsonValueKind.Array ||
                panes.GetArrayLength() > 16) return new(null, true);
            var fallback = false;
            var width = Number(root, "width", 1100, ref fallback);
            var height = Number(root, "height", 640, ref fallback);
            if (width <= 0 || height <= 0)
            {
                width = 1100;
                height = 640;
                fallback = true;
            }

            var entries = ImmutableArray.CreateBuilder<PanePlacement>();
            foreach (var element in panes.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    fallback = true;
                    continue;
                }

                var id = String(element, "instanceId");
                var type = String(element, "typeId");
                var definition =
                    WorkspaceState.Definitions.FirstOrDefault(item => item.InstanceId == id && item.TypeId == type);
                if (definition is null || entries.Any(item => item.InstanceId == id))
                {
                    fallback = true;
                    continue;
                }

                var visibility = String(element, "visibility") switch
                {
                    "visible" => PaneVisibility.Visible, "collapsed" => PaneVisibility.Collapsed,
                    "hidden" => PaneVisibility.Hidden, _ => InvalidVisibility(ref fallback)
                };
                var dock = String(element, "dock") switch
                {
                    "left" => PaneDock.Left, "right" => PaneDock.Right, "floating" => PaneDock.Floating,
                    _ => InvalidDock(ref fallback)
                };
                var allowed = true;
                if (element.TryGetProperty("allowDocking", out var permission) &&
                    permission.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    allowed = permission.GetBoolean();
                else fallback = true;
                var defaults = definition.DefaultBounds;
                var bounds = new PaneBounds(Number(element, "x", defaults.X, ref fallback),
                    Number(element, "y", defaults.Y, ref fallback),
                    Number(element, "width", defaults.Width, ref fallback),
                    Number(element, "height", defaults.Height, ref fallback));
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    bounds = defaults;
                    fallback = true;
                }

                entries.Add(new(definition.InstanceId, definition.TypeId, visibility, bounds, dock, allowed));
            }

            var active = String(root, "activePaneId");
            return new(new(width, height, active, entries.ToImmutable()), fallback);
        }
        catch (FileNotFoundException)
        {
            return new(null, false);
        }
        catch (DirectoryNotFoundException)
        {
            return new(null, false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return new(null, true);
        }
    }

    public async Task<bool> SaveAsync(WorkspaceLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        await _gate.WaitAsync();
        try
        {
            if (_preserveOriginal) return false;
            return await Task.Run(() => Save(layout));
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool Save(WorkspaceLayout layout)
    {
        var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            if (layout.Panes.IsDefault || layout.Panes.Length > WorkspaceState.Definitions.Length ||
                !double.IsFinite(layout.Width) || !double.IsFinite(layout.Height)) return false;
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                version = 1, width = layout.Width, height = layout.Height, activePaneId = layout.ActivePaneId,
                panes = layout.Panes.Select(pane => new
                {
                    instanceId = pane.InstanceId, typeId = pane.TypeId,
                    visibility = pane.Visibility.ToString().ToLowerInvariant(),
                    x = pane.FloatingBounds.X, y = pane.FloatingBounds.Y,
                    width = pane.FloatingBounds.Width, height = pane.FloatingBounds.Height,
                    dock = pane.Dock.ToString().ToLowerInvariant(), allowDocking = pane.AllowDocking
                })
            });
            if (bytes.Length > MaximumBytes) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_path))
            {
                if (new FileInfo(_path).Length > MaximumBytes) return false;
                File.Copy(_path, _path + ".previous", overwrite: true);
            }

            File.Move(temporary, _path, overwrite: true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
        finally
        {
            try
            {
                File.Delete(temporary);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                /* Owned temporary only. */
            }
        }
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double Number(JsonElement element, string name, double otherwise, ref bool fallback)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number &&
            value.TryGetDouble(out var number) && double.IsFinite(number) && Math.Abs(number) <= 100000) return number;
        fallback = true;
        return otherwise;
    }

    private static PaneVisibility InvalidVisibility(ref bool fallback)
    {
        fallback = true;
        return PaneVisibility.Hidden;
    }

    private static PaneDock InvalidDock(ref bool fallback)
    {
        fallback = true;
        return PaneDock.Floating;
    }
}

/// <summary>Owner-context latest-pending coalescing. Shutdown joins all requested stable snapshots.</summary>
public sealed class WorkspaceLayoutPersistence(Func<WorkspaceLayout, Task<bool>> write)
{
    private WorkspaceLayout? _pending;
    private bool _writing;
    private bool _closed;
    private Task _work = Task.CompletedTask;
    public bool LastWriteSucceeded { get; private set; } = true;
    public event Action? Completed;

    public void Request(WorkspaceLayout layout)
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        _pending = layout;
        if (_writing) return;
        _writing = true;
        _work = DrainAsync();
    }

    private async Task DrainAsync()
    {
        try
        {
            while (_pending is { } next)
            {
                _pending = null;
                LastWriteSucceeded = await write(next);
                Completed?.Invoke();
            }
        }
        finally
        {
            _writing = false;
        }
    }

    public async Task ShutdownAsync()
    {
        _closed = true;
        await _work;
        Completed = null;
    }
}
