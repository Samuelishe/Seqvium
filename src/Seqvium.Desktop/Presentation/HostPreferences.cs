// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;

namespace Seqvium.Desktop.Presentation;

public enum HostLanguage { English, Russian }
public enum HostTheme { Dark, Light }
public sealed record HostPreferences(HostLanguage Language = HostLanguage.English, HostTheme Theme = HostTheme.Dark);
public sealed record PreferenceLoad(HostPreferences Preferences, bool UsedFallback);

/// <summary>Owns only one small host preference file and its previous copy, never project or recovery storage.</summary>
public sealed class PreferenceStore(string path)
{
    private const int MaximumBytes = 4096;
    private readonly string _path = Path.GetFullPath(path);
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public static string DefaultPath()
    {
        string root;
        if (OperatingSystem.IsWindows())
            root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        else if (OperatingSystem.IsMacOS())
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        else
        {
            var configured = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            root = !string.IsNullOrWhiteSpace(configured) && Path.IsPathFullyQualified(configured)
                ? configured : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }
        return Path.Combine(root, "Seqvium", "preferences.json");
    }

    public async Task<PreferenceLoad> LoadAsync() => await Task.Run(() =>
    {
        try
        {
            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumBytes) return new(new(), true);
            using var json = JsonDocument.Parse(stream);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("version", out var version) ||
                !version.TryGetInt32(out var number) || number != 1) return new(new(), true);
            var language = ReadString(root, "language");
            var theme = ReadString(root, "theme");
            return new PreferenceLoad(new(language == "ru" ? HostLanguage.Russian : HostLanguage.English,
                theme == "light" ? HostTheme.Light : HostTheme.Dark),
                language is not ("en" or "ru") || theme is not ("dark" or "light"));
        }
        catch (FileNotFoundException) { return new PreferenceLoad(new(), false); }
        catch (DirectoryNotFoundException) { return new PreferenceLoad(new(), false); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        { return new PreferenceLoad(new(), true); }
    });

    public async Task<bool> SaveAsync(HostPreferences preferences)
    {
        if (!Enum.IsDefined(preferences.Language) || !Enum.IsDefined(preferences.Theme))
            throw new ArgumentOutOfRangeException(nameof(preferences));
        await _writeGate.WaitAsync();
        try
        {
            return await Task.Run(() => Save(preferences));
        }
        finally { _writeGate.Release(); }
    }

    private bool Save(HostPreferences preferences)
    {
        var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                version = 1,
                language = preferences.Language == HostLanguage.Russian ? "ru" : "en",
                theme = preferences.Theme == HostTheme.Light ? "light" : "dark"
            });
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            // Preserve even malformed bytes before an explicit preference change replaces them.
            // Refuse large/unreadable existing files instead of copying arbitrary amounts of data.
            if (File.Exists(_path))
            {
                if (new FileInfo(_path).Length > MaximumBytes) return false;
                File.Copy(_path, _path + ".previous", overwrite: true);
            }
            File.Move(temporary, _path, overwrite: true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { /* Owned temporary only. */ }
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
