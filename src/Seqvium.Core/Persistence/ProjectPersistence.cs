// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Seqvium.Core;

internal sealed record ProjectFile(
    string Format,
    int Major,
    int Minor,
    int MinimumReaderMinor,
    string WriterVersion,
    Guid Revision,
    ProjectState State) : CanonicalData;

public sealed class ProjectFormatException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed record ProjectLoadResult(ProjectDocument Document, ImmutableArray<string> Diagnostics)
{
    public ImmutableArray<GraphIntentReport> GraphReports { get; init; } = [];
    public bool IsDegraded => !Diagnostics.IsEmpty || GraphReports.Any(report => !report.IsEligibleForPreparation);
}

public sealed record ProjectSaveResult(ProjectSnapshot Snapshot, string Path, ImmutableArray<string> Diagnostics)
{
    public ImmutableArray<GraphIntentReport> GraphReports { get; init; } = [];
    public bool IsDegraded => !Diagnostics.IsEmpty || GraphReports.Any(report => !report.IsEligibleForPreparation);
}

/// <summary>Canonical encoding and bounded file/media publication; no crash-recovery guarantee.</summary>
public static class ProjectPersistence
{
    public const int MaximumBytes = 16 * 1024 * 1024;
    public const string WriterVersion = "0.2.0-r2-f1";
    public const int ReaderMinor = 14;
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static bool HasReverb(ProjectState state) => state.Mixer is { } mixer &&
                                                         (mixer.Master.Reverb != new MixerReverb() ||
                                                          mixer.Channels.Any(c =>
                                                              c.Settings.Reverb != new MixerReverb()));

    private static bool HasDelay(ProjectState state) => state.Mixer is { } mixer &&
                                                        (mixer.Master.Delay != new MixerDelay() ||
                                                         mixer.Channels.Any(c => c.Settings.Delay != new MixerDelay()));

    private static bool HasAudio(ProjectState state) => !state.AudioClips.IsEmpty ||
                                                        state.Sounds.Any(s =>
                                                            s.Algorithm == AudioClipSource.Algorithm) ||
                                                        state.Resources.Any(AudioMedia.IsManaged);

    private static bool HasSongStructure(ProjectState state) => !state.ArrangementSections.IsEmpty ||
                                                                state.Patterns.Any(p => p.VariationOf is not null);

    public static byte[] Encode(ProjectDocument document)
    {
        document.CheckAvailable();
        return Encode(document.Current, document.Compatibility);
    }

    private static byte[] Encode(ProjectSnapshot snapshot, ProjectCompatibility compatibility)
    {
        ProjectValidation.Validate(snapshot.State);
        bool hasGraphs = !snapshot.State.Graphs.IsEmpty || !snapshot.State.GraphAttachments.IsEmpty;
        int required = snapshot.State.Sounds.Any(SamplerKeymap.HasVelocityIntent) ? 14 :
            snapshot.State.Sounds.Any(SamplerKeymap.HasIntent) ? 13 :
            !snapshot.State.AutomationLanes.IsEmpty ? 12 :
            snapshot.State.Sounds.Any(SynthVoice.HasIntent) ? 11 :
            snapshot.State.Sounds.Any(SamplerPlayback.HasCrossfadeIntent) ? 10 :
            snapshot.State.Sounds.Any(SamplerPlayback.HasIntent) ? 9 :
            HasReverb(snapshot.State) ? 8 :
            HasDelay(snapshot.State) ? 7 :
            snapshot.State.AudioClips.Any(c => c.StretchRatio != 1) ? 6 :
            HasSongStructure(snapshot.State) ? 5 :
            HasAudio(snapshot.State) ? 4 :
            snapshot.State.Mixer is not null ? 3 :
            !snapshot.State.ArrangementClips.IsEmpty || !snapshot.State.ArrangementTracks.IsEmpty ? 2 :
            hasGraphs ? 1 : 0;
        var file = new ProjectFile("seqvium-project", 1, Math.Max(compatibility.Minor, required),
            Math.Max(compatibility.MinimumReaderMinor, required),
            WriterVersion, snapshot.Revision, snapshot.State) { AdditionalData = compatibility.AdditionalData };
        var node = JsonSerializer.SerializeToNode(file, Options)?.AsObject()
                   ?? throw new ProjectFormatException("Unable to encode project structure.");
        TransformAdditionalData(node, typeof(ProjectFile), loading: false);
        if (snapshot.State.AutomationLanes.IsEmpty) node["state"]!.AsObject().Remove("automationLanes");
        // Older readers conservatively refuse unknown strip properties. Keep the original envelope/shape
        // for projects with no Delay intent; non-default (even bypassed) intent requires reader 1.7.
        if (snapshot.State.Mixer is { } mixer && node["state"]?["mixer"] is JsonObject mixerNode)
        {
            if (mixer.Master.Delay == new MixerDelay()) mixerNode["master"]!.AsObject().Remove("delay");
            for (int i = 0; i < mixer.Channels.Length; i++)
                if (mixer.Channels[i].Settings.Delay == new MixerDelay())
                    mixerNode["channels"]![i]!["settings"]!.AsObject().Remove("delay");
        }

        if (snapshot.State.Mixer is { } reverbMixer && node["state"]?["mixer"] is JsonObject reverbNode)
        {
            if (reverbMixer.Master.Reverb == new MixerReverb()) reverbNode["master"]!.AsObject().Remove("reverb");
            for (int i = 0; i < reverbMixer.Channels.Length; i++)
                if (reverbMixer.Channels[i].Settings.Reverb == new MixerReverb())
                    reverbNode["channels"]![i]!["settings"]!.AsObject().Remove("reverb");
        }

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
            if (buffer.Length > MaximumBytes)
                throw new ProjectFormatException("Project exceeds the 16 MiB format limit.");
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
            var file = node.Deserialize<ProjectFile>(Options) ??
                       throw new ProjectFormatException("Missing project structure.");
            ProjectValidation.Validate(file.State);
            if (file.State.Sounds.Any(SamplerKeymap.HasVelocityIntent) && file.MinimumReaderMinor < 14)
                throw new ProjectFormatException("Sampler velocity layers require minimumReaderMinor >= 14.");
            if (file.State.Sounds.Any(SamplerKeymap.HasIntent) && file.MinimumReaderMinor < 13)
                throw new ProjectFormatException("Sampler key mapping requires minimumReaderMinor >= 13.");
            if (!file.State.AutomationLanes.IsEmpty && file.MinimumReaderMinor < 12)
                throw new ProjectFormatException("Parameter automation requires minimumReaderMinor >= 12.");
            if (file.State.Sounds.Any(SynthVoice.HasIntent) && file.MinimumReaderMinor < 11)
                throw new ProjectFormatException("Subtractive synth requires minimumReaderMinor >= 11.");
            if (file.State.Sounds.Any(SamplerPlayback.HasCrossfadeIntent) && file.MinimumReaderMinor < 10)
                throw new ProjectFormatException("Sampler crossfade requires minimumReaderMinor >= 10.");
            if (file.State.Sounds.Any(SamplerPlayback.HasIntent) && file.MinimumReaderMinor < 9)
                throw new ProjectFormatException("Sampler range/loop requires minimumReaderMinor >= 9.");
            if (HasReverb(file.State) && file.MinimumReaderMinor < 8)
                throw new ProjectFormatException("Mixer reverb requires minimumReaderMinor >= 8.");
            if (HasDelay(file.State) && file.MinimumReaderMinor < 7)
                throw new ProjectFormatException("Mixer delay requires minimumReaderMinor >= 7.");
            if (file.State.AudioClips.Any(c => c.StretchRatio != 1) && file.MinimumReaderMinor < 6)
                throw new ProjectFormatException("Audio clip stretch requires minimumReaderMinor >= 6.");
            if (HasSongStructure(file.State) && file.MinimumReaderMinor < 5)
                throw new ProjectFormatException(
                    "Playlist sections and Pattern ancestry require minimumReaderMinor >= 5.");
            if (HasAudio(file.State) && file.MinimumReaderMinor < 4)
                throw new ProjectFormatException("Audio clips require minimumReaderMinor >= 4.");
            if (file.State.Mixer is not null && file.MinimumReaderMinor < 3)
                throw new ProjectFormatException("Mixer requires minimumReaderMinor >= 3.");
            if ((!file.State.Graphs.IsEmpty || !file.State.GraphAttachments.IsEmpty) && file.MinimumReaderMinor < 1)
                throw new ProjectFormatException("Graph intent requires minimumReaderMinor >= 1.");
            if ((!file.State.ArrangementClips.IsEmpty || !file.State.ArrangementTracks.IsEmpty) &&
                file.MinimumReaderMinor < 2)
                throw new ProjectFormatException("Arrangement requires minimumReaderMinor >= 2.");
            ProjectValidation.Require(file.Revision != Guid.Empty, "Revision cannot be empty.");
            ProjectValidation.Require(
                !string.IsNullOrWhiteSpace(file.WriterVersion) &&
                file.WriterVersion.Length <= ProjectValidation.MaximumTextLength,
                "Writer version is required.");
            var document = ProjectDocument.Reopen(new(file.State, file.Revision));
            document.Compatibility = new(file.Minor, file.MinimumReaderMinor) { AdditionalData = file.AdditionalData };
            var diagnostics = ImmutableArray.CreateBuilder<string>();
            foreach (var lane in file.State.AutomationLanes.Where(l =>
                         !ParameterAutomation.IsResolved(file.State, l.Target)))
                diagnostics.Add($"Automation target '{lane.TargetName}' is unavailable; its envelope is retained.");
            foreach (var resource in file.State.Resources)
                if (availableResources?.Contains(resource.Id) != true)
                    diagnostics.Add($"Resource {resource.Id} is unavailable or has not been validated.");
            var extensions = file.State.Sounds.Select(sound => sound.Extension)
                .Concat(file.State.Contexts.Select(context => context.Extension)).OfType<ExtensionState>();
            foreach (var extension in extensions.DistinctBy(item => item.ExtensionId))
                if (availableExtensions?.Contains(extension.ExtensionId) != true)
                    diagnostics.Add($"Extension '{extension.ExtensionId}' is unavailable; opaque state is retained.");
            return new(document, diagnostics.ToImmutable()) { GraphReports = GraphDiagnostics.Inspect(file.State) };
        }
        catch (Exception error) when (error is JsonException or ArgumentException or OverflowException
                                          or ProjectValidationException)
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
        foreach (var resource in result.Document.Current.State.Resources.Where(AudioMedia.IsManaged))
        {
            diagnostics.Remove($"Resource {resource.Id} is unavailable or has not been validated.");
            try
            {
                AudioMedia.Resolve(result.Document, resource);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add($"Managed audio {resource.Id} is unavailable or corrupt: {error.Message}");
            }
        }

        foreach (var resource in result.Document.Current.State.Resources.Where(ProjectMedia.IsManagedWav))
        {
            diagnostics.Remove($"Resource {resource.Id} is unavailable or has not been validated.");
            try
            {
                using (ProjectMedia.Decode(result.Document, resource.Id))
                {
                }
            }
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
        var graphReports = GraphDiagnostics.Inspect(snapshot.State);
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination) ??
                        throw new ArgumentException("A destination directory is required.", nameof(path));
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
            if (!snapshot.State.Graphs.IsEmpty || !snapshot.State.GraphAttachments.IsEmpty ||
                !snapshot.State.ArrangementClips.IsEmpty || !snapshot.State.ArrangementTracks.IsEmpty)
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(document.Compatibility.Minor,
                        snapshot.State.ArrangementClips.IsEmpty && snapshot.State.ArrangementTracks.IsEmpty ? 1 : 2),
                    MinimumReaderMinor = Math.Max(document.Compatibility.MinimumReaderMinor,
                        snapshot.State.ArrangementClips.IsEmpty && snapshot.State.ArrangementTracks.IsEmpty ? 1 : 2)
                };
            document.SavedMediaDiagnostics = diagnostics;
            if (snapshot.State.Sounds.Any(SamplerKeymap.HasVelocityIntent))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(14, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(14, document.Compatibility.MinimumReaderMinor)
                };
            if (snapshot.State.Sounds.Any(SamplerKeymap.HasIntent))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(13, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(13, document.Compatibility.MinimumReaderMinor)
                };
            if (!snapshot.State.AutomationLanes.IsEmpty)
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(12, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(12, document.Compatibility.MinimumReaderMinor)
                };
            if (snapshot.State.Sounds.Any(SynthVoice.HasIntent))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(11, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(11, document.Compatibility.MinimumReaderMinor)
                };
            if (snapshot.State.Sounds.Any(SamplerPlayback.HasCrossfadeIntent))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(10, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(10, document.Compatibility.MinimumReaderMinor)
                };
            if (snapshot.State.Sounds.Any(SamplerPlayback.HasIntent))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(9, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(9, document.Compatibility.MinimumReaderMinor)
                };
            if (HasReverb(snapshot.State))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(8, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(8, document.Compatibility.MinimumReaderMinor)
                };
            if (HasDelay(snapshot.State))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(7, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(7, document.Compatibility.MinimumReaderMinor)
                };
            if (snapshot.State.AudioClips.Any(c => c.StretchRatio != 1))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(6, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(6, document.Compatibility.MinimumReaderMinor)
                };
            if (HasSongStructure(snapshot.State))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(5, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(5, document.Compatibility.MinimumReaderMinor)
                };
            if (HasAudio(snapshot.State))
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(4, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(4, document.Compatibility.MinimumReaderMinor)
                };
            if (snapshot.State.Mixer is not null)
                document.Compatibility = document.Compatibility with
                {
                    Minor = Math.Max(3, document.Compatibility.Minor),
                    MinimumReaderMinor = Math.Max(3, document.Compatibility.MinimumReaderMinor)
                };
            if (!document.MediaRoots.Contains(destination + ".media")) document.MediaRoots.Add(destination + ".media");
            return new(snapshot, destination, diagnostics) { GraphReports = graphReports };
        }
        finally
        {
            if (ownsTemporary) File.Delete(temporary);
        }
    }

    private static void CheckVersion(JsonObject node)
    {
        if (node["format"]?.GetValueKind() != JsonValueKind.String ||
            node["format"]!.GetValue<string>() != "seqvium-project")
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
        if (major != 1 || minor < 0 || minimum < 0 || minimum > minor || minimum > ReaderMinor)
            throw new ProjectFormatException(
                $"Unsupported document version {major}.{minor} (minimum reader minor {minimum}). Reader supports 1.{ReaderMinor}.");
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new ProjectFormatException($"Duplicate JSON property '{property.Name}'.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                RejectDuplicateProperties(item);
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
            if (node.ContainsKey(storageKey))
                throw new ProjectFormatException($"'{storageKey}' is a reserved internal field.");
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
            if (type == typeof(SamplerZone))
            {
                if (node["lowVelocity"]?.GetValue<decimal>() == 0) node.Remove("lowVelocity");
                if (node["highVelocity"]?.GetValue<decimal>() == 1) node.Remove("highVelocity");
            }
            if (type == typeof(SoundDefinition) && node["samplerZones"] is JsonArray { Count: 0 })
                node.Remove("samplerZones");
            if (type == typeof(AudioClip) && node["stretchRatio"]?.GetValue<decimal>() == 1)
                node.Remove("stretchRatio");
            var extra = node[storageKey] as JsonObject;
            node.Remove(storageKey);
            if (extra is not null)
                foreach (var (key, value) in extra)
                {
                    if (known.Contains(key) || key == storageKey)
                        throw new ProjectFormatException("Unknown data collides with a reserved field.");
                    node[key] = value?.DeepClone();
                }
        }

        foreach (var property in properties)
        {
            if (typeof(CanonicalData).IsAssignableFrom(property.PropertyType) &&
                node[property.Name] is JsonObject child)
                TransformAdditionalData(child, property.PropertyType, loading);
            else if (property.PropertyType.IsGenericType &&
                     property.PropertyType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            {
                var elementType = property.PropertyType.GetGenericArguments()[0];
                if (typeof(CanonicalData).IsAssignableFrom(elementType) && node[property.Name] is JsonArray array)
                    foreach (var item in array.OfType<JsonObject>())
                        TransformAdditionalData(item, elementType, loading);
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
        public override bool CanConvert(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Id<>);

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
            (JsonConverter)(Activator.CreateInstance(
                                typeof(IdentityConverter<>).MakeGenericType(type.GetGenericArguments()[0]))
                            ?? throw new InvalidOperationException("Unable to create identity codec."));
    }

    private sealed class IdentityConverter<T> : JsonConverter<Id<T>>
    {
        public override Id<T> Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.String && reader.TryGetGuid(out var value)
                ? new(value)
                : throw new JsonException("Identity must be a UUID string.");

        public override void Write(Utf8JsonWriter writer, Id<T> value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }

    private sealed class PositionConverter : JsonConverter<MusicalPosition>
    {
        public override MusicalPosition Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value)
                ? new(value)
                : throw new JsonException("Position must be an Int64 tick count.");

        public override void Write(Utf8JsonWriter writer, MusicalPosition value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.Ticks);
    }

    private sealed class DurationConverter : JsonConverter<MusicalDuration>
    {
        public override MusicalDuration Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value)
                ? new(value)
                : throw new JsonException("Duration must be an Int64 tick count.");

        public override void Write(Utf8JsonWriter writer, MusicalDuration value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.Ticks);
    }

    private sealed class TempoConverter : JsonConverter<Tempo>
    {
        public override Tempo Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var value)
                ? new(value)
                : throw new JsonException("Tempo must be a decimal BPM value.");

        public override void Write(Utf8JsonWriter writer, Tempo value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.BeatsPerMinute);
    }
}
