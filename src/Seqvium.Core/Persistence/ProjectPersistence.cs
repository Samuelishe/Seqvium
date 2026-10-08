// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Seqvium.Core;

internal sealed record ProjectFile(string Format, int Major, int Minor, int MinimumReaderMinor,
    string WriterVersion, Guid Revision, ProjectState State) : CanonicalData;

public sealed class ProjectFormatException(string message, Exception? innerException = null) : Exception(message, innerException);
public sealed record ProjectLoadResult(ProjectDocument Document, ImmutableArray<string> Diagnostics)
{
    public bool IsDegraded => !Diagnostics.IsEmpty;
}
public sealed record ProjectSaveResult(ProjectSnapshot Snapshot, string Path, ImmutableArray<string> Diagnostics)
{
    public bool IsDegraded => !Diagnostics.IsEmpty;
}

/// <summary>Canonical encoding and bounded file/media publication; no crash-recovery guarantee.</summary>
public static class ProjectPersistence
{
    public const int MaximumBytes = 16 * 1024 * 1024;
    public const string WriterVersion = "0.2.0-r2-f1";
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static byte[] Encode(ProjectDocument document)
    {
        document.CheckAvailable();
        return Encode(document.Current, document.Compatibility);
    }

    private static byte[] Encode(ProjectSnapshot snapshot, ProjectCompatibility compatibility)
    {
        ProjectValidation.Validate(snapshot.State);
        var file = new ProjectFile("seqvium-project", 1, compatibility.Minor, compatibility.MinimumReaderMinor,
            WriterVersion, snapshot.Revision, snapshot.State) { AdditionalData = compatibility.AdditionalData };
        var node = JsonSerializer.SerializeToNode(file, Options)?.AsObject()
            ?? throw new ProjectFormatException("Unable to encode project structure.");
        TransformAdditionalData(node, typeof(ProjectFile), loading: false);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(node, Options);
        if (bytes.Length > MaximumBytes) throw new ProjectFormatException("Project exceeds the 16 MiB format limit.");
        return bytes;
    }

    public static void Write(ProjectDocument document, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Write(Encode(document));
    }

    public static ProjectLoadResult Read(Stream source, IReadOnlySet<string>? availableExtensions = null,
        IReadOnlySet<Id<ResourceDescriptor>>? availableResources = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = source.Read(chunk, 0, (int)Math.Min(chunk.Length, MaximumBytes + 1L - buffer.Length))) > 0)
        {
            buffer.Write(chunk, 0, count);
            if (buffer.Length > MaximumBytes) throw new ProjectFormatException("Project exceeds the 16 MiB format limit.");
        }
        try
        {
            using var parsed = JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 64 });
            RejectDuplicateProperties(parsed.RootElement);
            if (parsed.RootElement.ValueKind != JsonValueKind.Object)
                throw new ProjectFormatException("Project root must be an object.");
            var node = JsonNode.Parse(parsed.RootElement.GetRawText())!.AsObject();
            CheckVersion(node);
            TransformAdditionalData(node, typeof(ProjectFile), loading: true);
            var file = node.Deserialize<ProjectFile>(Options) ?? throw new ProjectFormatException("Missing project structure.");
            ProjectValidation.Validate(file.State);
            ProjectValidation.Require(file.Revision != Guid.Empty, "Revision cannot be empty.");
            ProjectValidation.Require(!string.IsNullOrWhiteSpace(file.WriterVersion) && file.WriterVersion.Length <= ProjectValidation.MaximumTextLength,
                "Writer version is required.");
            var document = ProjectDocument.Reopen(new(file.State, file.Revision));
            document.Compatibility = new(file.Minor, file.MinimumReaderMinor) { AdditionalData = file.AdditionalData };
            var diagnostics = ImmutableArray.CreateBuilder<string>();
            foreach (var resource in file.State.Resources)
                if (availableResources?.Contains(resource.Id) != true)
                    diagnostics.Add($"Resource {resource.Id} is unavailable or has not been validated.");
            var extensions = file.State.Sounds.Select(sound => sound.Extension)
                .Concat(file.State.Contexts.Select(context => context.Extension)).OfType<ExtensionState>();
            foreach (var extension in extensions.DistinctBy(item => item.ExtensionId))
                if (availableExtensions?.Contains(extension.ExtensionId) != true)
                    diagnostics.Add($"Extension '{extension.ExtensionId}' is unavailable; opaque state is retained.");
            return new(document, diagnostics.ToImmutable());
        }
        catch (Exception error) when (error is JsonException or ArgumentException or OverflowException or ProjectValidationException)
        {
            throw new ProjectFormatException($"Project structure cannot be safely interpreted: {error.Message}", error);
        }
    }

    public static ProjectLoadResult Open(string path, IReadOnlySet<string>? availableExtensions = null,
        IReadOnlySet<Id<ResourceDescriptor>>? availableResources = null)
    {
        var fullPath = Path.GetFullPath(path);
        using var stream = File.OpenRead(fullPath);
        var result = Read(stream, availableExtensions, availableResources);
        result.Document.MediaRoots.Add(fullPath + ".media");
        var diagnostics = result.Diagnostics.ToBuilder();
        foreach (var resource in result.Document.Current.State.Resources.Where(ProjectMedia.IsManagedWav))
        {
            diagnostics.Remove($"Resource {resource.Id} is unavailable or has not been validated.");
            try { using (ProjectMedia.Decode(result.Document, resource.Id)) { } }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add($"Managed WAV {resource.Id} is unavailable or corrupt: {error.Message}");
            }
        }
        result.Document.MarkSaved(result.Document.Current, fullPath);
        result.Document.SavedMediaDiagnostics = diagnostics.ToImmutable();
        return result with { Diagnostics = diagnostics.ToImmutable() };
    }

    public static void Save(ProjectDocument document, string path)
        => SaveWithReport(document, path);

    /// <summary>Reports incomplete media availability while preserving safely interpretable canonical edits.</summary>
    public static ProjectSaveResult SaveWithReport(ProjectDocument document, string path)
    {
        document.CheckAvailable();
        var snapshot = document.Current;
        var bytes = Encode(snapshot, document.Compatibility);
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination) ?? throw new ArgumentException("A destination directory is required.", nameof(path));
        var temporary = Path.Combine(directory, $".seqvium-{Guid.NewGuid():N}.tmp");
        bool ownsTemporary = false;
        try
        {
            var diagnostics = ProjectMedia.PrepareSave(document, snapshot, destination);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                ownsTemporary = true;
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, destination, overwrite: true);
            ownsTemporary = false;
            document.MarkSaved(snapshot, destination);
            document.SavedMediaDiagnostics = diagnostics;
            if (!document.MediaRoots.Contains(destination + ".media")) document.MediaRoots.Add(destination + ".media");
            return new(snapshot, destination, diagnostics);
        }
        finally
        {
            if (ownsTemporary) File.Delete(temporary);
        }
    }

    private static void CheckVersion(JsonObject node)
    {
        if (node["format"]?.GetValueKind() != JsonValueKind.String || node["format"]!.GetValue<string>() != "seqvium-project")
            throw new ProjectFormatException("Unrecognized project format.");
        int Integer(string key)
        {
            if (node[key] is not JsonValue value || !value.TryGetValue<int>(out var result))
                throw new ProjectFormatException($"Required integer '{key}' is missing or invalid.");
            return result;
        }
        var major = Integer("major");
        var minor = Integer("minor");
        var minimum = Integer("minimumReaderMinor");
        if (major != 1 || minor < 0 || minimum < 0 || minimum > minor || minimum > 0)
            throw new ProjectFormatException($"Unsupported document version {major}.{minor} (minimum reader minor {minimum}). Reader supports 1.0.");
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new ProjectFormatException($"Duplicate JSON property '{property.Name}'.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    // Preserve optional unknown fields at their original object boundary. Opaque payloads/maps are not interpreted.
    private static void TransformAdditionalData(JsonObject node, Type type, bool loading)
    {
        var properties = Options.GetTypeInfo(type).Properties.Where(property => property.Set is not null).ToArray();
        var known = properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        const string storageKey = "additionalData";
        known.Remove(storageKey);
        if (loading)
        {
            if (node.ContainsKey(storageKey)) throw new ProjectFormatException($"'{storageKey}' is a reserved internal field.");
            var extra = new JsonObject();
            foreach (var key in node.Select(pair => pair.Key).Where(key => !known.Contains(key)).ToArray())
            {
                extra[key] = node[key]?.DeepClone();
                node.Remove(key);
            }
            node[storageKey] = extra;
        }
        else
        {
            var extra = node[storageKey] as JsonObject;
            node.Remove(storageKey);
            if (extra is not null)
                foreach (var (key, value) in extra)
                {
                    if (known.Contains(key) || key == storageKey) throw new ProjectFormatException("Unknown data collides with a reserved field.");
                    node[key] = value?.DeepClone();
                }
        }
        foreach (var property in properties)
        {
            if (typeof(CanonicalData).IsAssignableFrom(property.PropertyType) && node[property.Name] is JsonObject child)
                TransformAdditionalData(child, property.PropertyType, loading);
            else if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            {
                var elementType = property.PropertyType.GetGenericArguments()[0];
                if (typeof(CanonicalData).IsAssignableFrom(elementType) && node[property.Name] is JsonArray array)
                    foreach (var item in array.OfType<JsonObject>()) TransformAdditionalData(item, elementType, loading);
            }
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            MaxDepth = 64,
            IgnoreReadOnlyProperties = true,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        options.Converters.Add(new IdentityConverterFactory());
        options.Converters.Add(new PositionConverter());
        options.Converters.Add(new DurationConverter());
        options.Converters.Add(new TempoConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private sealed class IdentityConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Id<>);
        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
            (JsonConverter)(Activator.CreateInstance(typeof(IdentityConverter<>).MakeGenericType(type.GetGenericArguments()[0]))
                ?? throw new InvalidOperationException("Unable to create identity codec."));
    }
    private sealed class IdentityConverter<T> : JsonConverter<Id<T>>
    {
        public override Id<T> Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.String && reader.TryGetGuid(out var value)
                ? new(value) : throw new JsonException("Identity must be a UUID string.");
        public override void Write(Utf8JsonWriter writer, Id<T> value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
    }
    private sealed class PositionConverter : JsonConverter<MusicalPosition>
    {
        public override MusicalPosition Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value)
                ? new(value) : throw new JsonException("Position must be an Int64 tick count.");
        public override void Write(Utf8JsonWriter writer, MusicalPosition value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Ticks);
    }
    private sealed class DurationConverter : JsonConverter<MusicalDuration>
    {
        public override MusicalDuration Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value)
                ? new(value) : throw new JsonException("Duration must be an Int64 tick count.");
        public override void Write(Utf8JsonWriter writer, MusicalDuration value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Ticks);
    }
    private sealed class TempoConverter : JsonConverter<Tempo>
    {
        public override Tempo Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var value)
                ? new(value) : throw new JsonException("Tempo must be a decimal BPM value.");
        public override void Write(Utf8JsonWriter writer, Tempo value, JsonSerializerOptions options) => writer.WriteNumberValue(value.BeatsPerMinute);
    }
}
