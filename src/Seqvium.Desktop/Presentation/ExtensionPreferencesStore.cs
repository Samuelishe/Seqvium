// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace Seqvium.Desktop.Presentation;

/// <summary>Only disabled package identities. Unsafe originals are preserved, including unknown versions.</summary>
internal sealed class ExtensionPreferencesStore(string path)
{
    private bool _preserve;
    internal bool Failed { get; private set; }
    internal Task<string[]> LoadAsync() => Task.Run(() =>
    {
        try
        {
            using var input = File.OpenRead(path);
            if (input.Length > 32768) throw new InvalidDataException();
            using var json = JsonDocument.Parse(input, new() { MaxDepth = 4 });
            if (json.RootElement.GetProperty("version").GetInt32() != 1) throw new JsonException();
            var disabled = json.RootElement.GetProperty("disabled").Deserialize<string[]>() ?? throw new JsonException();
            if (disabled.Length > 256 || disabled.Any(p => string.IsNullOrWhiteSpace(p) || p.Length > 160)) throw new JsonException();
            return disabled.Distinct(StringComparer.Ordinal).ToArray();
        }
        catch (FileNotFoundException) { return []; }
        catch (DirectoryNotFoundException) { return []; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or KeyNotFoundException or InvalidOperationException)
        { Failed = _preserve = true; return []; }
    });

    internal Task<bool> SaveAsync(IEnumerable<string> disabled)
    {
        string[] snapshot = disabled.Order(StringComparer.Ordinal).ToArray();
        return Task.Run(() =>
        {
            if (_preserve) return false;
            string temporary = path + $".{Guid.NewGuid():N}.tmp";
            try
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { version = 1, disabled = snapshot });
                if (snapshot.Length > 256 || bytes.Length > 32768) throw new InvalidDataException();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { output.Write(bytes); output.Flush(true); }
                if (File.Exists(path)) File.Copy(path, path + ".previous", true);
                File.Move(temporary, path, true); Failed = false; return true;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException) { Failed = true; return false; }
            finally
            {
                try { File.Delete(temporary); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
            }
        });
    }
}
