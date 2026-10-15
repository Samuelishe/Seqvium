// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace Seqvium.Desktop.Presentation;

internal sealed record BrowserLocations(string[] Roots, string[] Recent, string? Last);

/// <summary>Access preferences only. Paths are remembered, never copied, scanned or owned.</summary>
internal sealed class BrowserLocationsStore(string path)
{
    private const int MaximumBytes = 32768;
    private bool _preserveOriginal;
    internal bool Failed { get; private set; }

    internal Task<BrowserLocations> LoadAsync() => Task.Run(() =>
    {
        try
        {
            using var file = File.OpenRead(path);
            if (file.Length > MaximumBytes) throw new IOException("Browser locations too large.");
            using var json = JsonDocument.Parse(file, new() { MaxDepth = 8 });
            if (json.RootElement.GetProperty("version").GetInt32() != 1) throw new JsonException();
            var result = json.RootElement.GetProperty("locations").Deserialize<BrowserLocations>() ?? throw new JsonException();
            if (result.Roots is null || result.Recent is null || result.Roots.Length > 16 || result.Recent.Length > 16 ||
                result.Roots.Concat(result.Recent).Append(result.Last ?? "").Any(p => p is null || p.Length > 2048) ||
                result.Roots.Concat(result.Recent).Any(p => !Path.IsPathFullyQualified(p)) ||
                result.Last is not null && !Path.IsPathFullyQualified(result.Last)) throw new JsonException();
            return result;
        }
        catch (FileNotFoundException) { return new BrowserLocations([], [], null); }
        catch (DirectoryNotFoundException) { return new BrowserLocations([], [], null); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or
            InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            _preserveOriginal = Failed = true;
            return new BrowserLocations([], [], null);
        }
    });

    // The pane serializes and joins writes; malformed originals remain untouched for this session.
    internal Task<bool> SaveAsync(BrowserLocations locations) => Task.Run(() =>
    {
        if (_preserveOriginal) return false;
        string temporary = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { version = 1, locations });
            if (bytes.Length > MaximumBytes) throw new IOException("Browser locations too large.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { output.Write(bytes); output.Flush(true); }
            if (File.Exists(path))
            {
                if (new FileInfo(path).Length > MaximumBytes) throw new IOException("Browser locations too large.");
                File.Copy(path, path + ".previous", true);
            }
            File.Move(temporary, path, true);
            Failed = false;
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { Failed = true; return false; }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    });
}
