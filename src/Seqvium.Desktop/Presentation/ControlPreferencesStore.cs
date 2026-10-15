// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;

namespace Seqvium.Desktop.Presentation;

public sealed record ControlPreferenceLoad(ControlPreferences Preferences, bool UsedFallback);

/// <summary>Separate compatible user file; unsafe originals remain read-only for this session.</summary>
public sealed class ControlPreferencesStore(string path)
{
    private const int MaximumBytes = 32768;
    private readonly string _path = Path.GetFullPath(path);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _preserveOriginal;

    public async Task<ControlPreferenceLoad> LoadAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var load = await Task.Run(Load);
            _preserveOriginal |= load.UsedFallback;
            return load;
        }
        finally { _gate.Release(); }
    }

    private ControlPreferenceLoad Load()
    {
        try
        {
            using var stream = File.OpenRead(_path);
            var bytes = new byte[MaximumBytes + 1];
            int count = 0, read;
            while (count < bytes.Length && (read = stream.Read(bytes, count, bytes.Length - count)) != 0) count += read;
            if (count > MaximumBytes) return new(new(), true);
            using var json = JsonDocument.Parse(bytes.AsMemory(0, count), new() { MaxDepth = 8 });
            var root = json.RootElement;
            if (root.GetProperty("version").GetInt32() != 1) return new(new(), true);
            var auto = root.TryGetProperty("autoPreview", out var preview) && preview.GetBoolean();
            var values = ImmutableDictionary.CreateBuilder<string, string?>();
            if (root.TryGetProperty("shortcuts", out var shortcuts))
            {
                foreach (var entry in shortcuts.EnumerateObject())
                {
                    if (!HostCommands.All.Any(command => command.Id == entry.Name) || values.ContainsKey(entry.Name))
                        return new(new(), true);
                    values.Add(entry.Name, HostCommands.Canonical(entry.Value.ValueKind == JsonValueKind.Null
                        ? null : entry.Value.GetString()));
                }
            }
            var preferences = new ControlPreferences(auto, values.ToImmutable());
            if (HostCommands.All.Any(command => preferences.Conflict(command.Id, preferences.Binding(command.Id)) is not null))
                return new(new(), true);
            return new(preferences, false);
        }
        catch (FileNotFoundException) { return new(new(), false); }
        catch (DirectoryNotFoundException) { return new(new(), false); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or
                                      InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException)
        { return new(new(), true); }
    }

    public async Task<bool> SaveAsync(ControlPreferences preferences)
    {
        await _gate.WaitAsync();
        try
        {
            if (_preserveOriginal) return false;
            return await Task.Run(() => Save(preferences));
        }
        finally { _gate.Release(); }
    }

    private bool Save(ControlPreferences preferences)
    {
        string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new { version = 1, autoPreview = preferences.AutoPreview,
                shortcuts = preferences.Overrides ?? ImmutableDictionary<string, string?>.Empty });
            if (bytes.Length > MaximumBytes) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes); stream.Flush(true); }
            if (File.Exists(_path))
            {
                if (new FileInfo(_path).Length > MaximumBytes) return false;
                File.Copy(_path, _path + ".previous", true);
            }
            File.Move(temporary, _path, true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }
}
