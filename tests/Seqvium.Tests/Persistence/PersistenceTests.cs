// SPDX-License-Identifier: Apache-2.0
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class PersistenceTests
{
    private static JsonObject Json(ProjectDocument document) => JsonNode.Parse(ProjectPersistence.Encode(document))!.AsObject();
    private static ProjectLoadResult Read(JsonNode node, IReadOnlySet<string>? extensions = null,
        IReadOnlySet<Id<ResourceDescriptor>>? resources = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(node.ToJsonString()));
        return ProjectPersistence.Read(stream, extensions, resources);
    }

    [Fact]
    public void SaveOpenRoundTripPreservesSharedVariationSoundResourceGroupsAndPaths()
    {
        using var directory = new TemporaryDirectory();
        var fixture = new SyntheticComposition();
        Id<Pattern> variationId = default;
        fixture.Document.Edit("Variation", edit => variationId = edit.MakePatternVariation(fixture.Placements[1], "Fill"));
        var part = fixture.Document.Current.State.Patterns.Single(item => item.Id == variationId).Parts[0];
        fixture.Document.Edit("Independent sound and project settings", edit =>
        {
            edit.MakeSoundIndependent(variationId, part.Id, "Independent Kick");
            edit.SetSettings(new(new Tempo(98.125m), new Meter(7, 8)));
        });
        var original = Json(fixture.Document);
        var path = directory.File("track.json");
        ProjectPersistence.Save(fixture.Document, path);
        var result = ProjectPersistence.Open(path);
        Assert.True(result.IsDegraded); // Descriptor exists, synthetic media bytes intentionally do not.
        Assert.True(JsonNode.DeepEquals(original, Json(result.Document)));
        var reopened = result.Document.Current.State;
        Assert.Equal(fixture.Pattern, reopened.Placements[0].PatternId);
        Assert.Equal(fixture.Pattern, reopened.Placements[2].PatternId);
        Assert.Equal(variationId, reopened.Placements[1].PatternId);
        Assert.Equal(3, reopened.Sounds.Length);
        Assert.Single(reopened.Resources);
        Assert.Single(reopened.Groups);
        Assert.Equal(fixture.ItemContext, reopened.Placements[1].ItemContextId);
        Assert.Equal(fixture.ContainerContext, reopened.Placements[1].ContainingContextId);
        Assert.Equal(fixture.Route, reopened.Placements[1].PartRelationships[0].RouteId);
        Assert.Equal(fixture.Document.Current.Revision, result.Document.Current.Revision);
        Assert.False(result.Document.IsDirty);
        ProjectValidation.Validate(reopened);
        var saveAs = directory.File("another.json");
        ProjectPersistence.Save(result.Document, saveAs);
        Assert.Equal(reopened.Id, ProjectPersistence.Open(saveAs).Document.Current.State.Id);
        Assert.True(File.Exists(path));
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 2, 2)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 0, -1)]
    [InlineData(1, 0, 1)]
    public void UnsupportedVersionsRefuseWithUsefulReason(int major, int minor, int minimum)
    {
        var node = Json(ProjectDocument.Create());
        node["major"] = major; node["minor"] = minor; node["minimumReaderMinor"] = minimum;
        var error = Assert.Throws<ProjectFormatException>(() => Read(node));
        Assert.Contains("Unsupported document version", error.Message);
        Assert.Contains("Reader supports 1.1", error.Message);
    }

    [Fact]
    public void CompatibleHigherMinorAndUnknownDataSurviveEveryCanonicalObjectBoundaryAndHistory()
    {
        var fixture = new SyntheticComposition();
        fixture.Document.Edit("Opaque dependency", edit =>
            edit.AddSound("Foreign", "extension", extension: new("org.example.future", 1, JsonSerializer.SerializeToElement(new { untouched = true }))));
        var original = Json(fixture.Document);
        original["minor"] = 3;
        original["minimumReaderMinor"] = 0;
        var objects = CanonicalObjects(original).ToArray();
        for (int index = 0; index < objects.Length; index++)
            objects[index]["futureOptional"] = JsonNode.Parse($"{{\"index\":{index},\"values\":[null,17,\"text\"]}}");
        var loaded = Read(original).Document;
        Assert.True(JsonNode.DeepEquals(original, Json(loaded)));
        loaded.Edit("Rename", edit => edit.RenameDocument("Edited"));
        var edited = Json(loaded);
        Assert.Equal(3, edited["minor"]!.GetValue<int>());
        var editedObjects = CanonicalObjects(edited).ToArray();
        Assert.Equal(objects.Length, editedObjects.Length);
        for (int index = 0; index < objects.Length; index++)
            Assert.True(JsonNode.DeepEquals(objects[index]["futureOptional"], editedObjects[index]["futureOptional"]));
        loaded.Undo();
        Assert.True(JsonNode.DeepEquals(original, Json(loaded)));
        loaded.Redo();
        Assert.True(JsonNode.DeepEquals(edited, Json(Read(edited).Document)));
    }

    [Fact]
    public void MissingExtensionRetainsOpaqueConfigurationAndAllowsUnrelatedEditsAndResave()
    {
        var document = ProjectDocument.Create();
        using (var opaque = JsonDocument.Parse("{\"preset\":7,\"foreign\":[1,2],\"state\":null}"))
        {
            var extension = new ExtensionState("org.example.instrument", 4, opaque.RootElement);
            document.Edit("Unavailable sound", edit => edit.AddSound("Opaque source", "extension", extension: extension));
        } // Domain owns a clone, so parser lifetime cannot invalidate state.
        var json = Json(document);
        var unavailable = Read(json);
        Assert.True(unavailable.IsDegraded);
        Assert.Single(unavailable.Diagnostics);
        Assert.Contains("org.example.instrument", unavailable.Diagnostics[0]);
        Assert.True(JsonNode.DeepEquals(json, Json(unavailable.Document)));
        unavailable.Document.Edit("Rename while degraded", edit => edit.RenameDocument("Still editable"));
        var reloaded = Read(Json(unavailable.Document), new HashSet<string> { "org.example.instrument" });
        Assert.False(reloaded.IsDegraded);
        Assert.Equal(4, reloaded.Document.Current.State.Sounds[0].Extension?.StateVersion);
        Assert.Equal(7, reloaded.Document.Current.State.Sounds[0].Extension?.Payload.GetProperty("preset").GetInt32());
        Id<SoundDefinition> independent = default;
        var patternId = default(Id<Pattern>);
        document.Edit("Part using opaque source", edit =>
        {
            patternId = edit.AddPattern("Music", SyntheticComposition.Quarter);
            edit.AddPart(patternId, "Part", document.Current.State.Sounds[0].Id);
        });
        var partId = document.Current.State.Patterns[0].Parts[0].Id;
        document.Edit("Independent durable opaque state", edit => independent = edit.MakeSoundIndependent(patternId, partId, "Independent"));
        Assert.NotEqual(document.Current.State.Sounds[0].Id, independent);
        Assert.Equal(document.Current.State.Sounds[0].Extension, document.Current.State.Sounds[1].Extension);
        // This proves data independence, not arbitrary plugin runtime duplication support.
    }

    [Fact]
    public void EditingKnownSettingsAndPartRoutesRetainsUnknownFieldsAtTheSameBoundary()
    {
        var fixture = new SyntheticComposition();
        var original = Json(fixture.Document);
        original["state"]!["settings"]!["futureSetting"] = 7;
        original["state"]!["settings"]!["meter"]!["futureMeter"] = "keep";
        original["state"]!["placements"]![1]!["partRelationships"]![0]!["futureRelation"] = true;
        var loaded = Read(original).Document;
        loaded.Edit("Edit supported settings and relationships", edit =>
        {
            edit.SetSettings(new(new Tempo(150m), new Meter(3, 4)));
            edit.SetPlacementRelationships(fixture.Placements[1], fixture.ItemContext, fixture.ContainerContext,
                fixture.Route, [new(fixture.KickPart, null, null), new(fixture.BassPart, null, fixture.Route)]);
        });
        var edited = Json(loaded);
        Assert.Equal(7, edited["state"]!["settings"]!["futureSetting"]?.GetValue<int>());
        Assert.Equal("keep", edited["state"]!["settings"]!["meter"]!["futureMeter"]?.GetValue<string>());
        Assert.True(edited["state"]!["placements"]![1]!["partRelationships"]![0]!["futureRelation"]?.GetValue<bool>());
        Assert.True(JsonNode.DeepEquals(edited, Json(Read(edited).Document)));
        loaded.Undo();
        Assert.True(JsonNode.DeepEquals(original, Json(loaded)));
    }

    [Fact]
    public void ValidMissingResourceDescriptorDegradesButDanglingIdentityRefuses()
    {
        var fixture = new SyntheticComposition();
        var original = Json(fixture.Document);
        Assert.True(Read(original).IsDegraded);
        Assert.False(Read(original, resources: new HashSet<Id<ResourceDescriptor>> { fixture.Resource }).IsDegraded);
        original["state"]!["resources"] = new JsonArray();
        Assert.Throws<ProjectFormatException>(() => Read(original));
    }

    [Theory]
    [InlineData("missing-state")]
    [InlineData("missing-settings")]
    [InlineData("missing-patterns")]
    [InlineData("missing-revision")]
    [InlineData("missing-name")]
    [InlineData("null-pattern")]
    [InlineData("null-settings")]
    [InlineData("empty-identity")]
    [InlineData("invalid-uuid")]
    [InlineData("non-string-uuid")]
    [InlineData("duplicate-identity")]
    [InlineData("bad-reference")]
    [InlineData("negative-position")]
    [InlineData("non-integer-position")]
    [InlineData("position-overflow")]
    [InlineData("zero-duration")]
    [InlineData("note-outside-pattern")]
    [InlineData("pitch-out-of-range")]
    [InlineData("intensity-out-of-range")]
    [InlineData("unsafe-locator")]
    [InlineData("reserved-extra")]
    [InlineData("unknown-essential-enum")]
    [InlineData("oversized-name")]
    [InlineData("non-number-tempo")]
    public void UnsafeEssentialStructureIsNeverGuessed(string failure)
    {
        var fixture = new SyntheticComposition();
        var node = Json(fixture.Document);
        var state = node["state"]!.AsObject();
        var pattern = state["patterns"]![0]!.AsObject();
        var note = pattern["parts"]![0]!["notes"]![0]!.AsObject();
        switch (failure)
        {
            case "missing-state": node.Remove("state"); break;
            case "missing-settings": state.Remove("settings"); break;
            case "missing-patterns": state.Remove("patterns"); break;
            case "missing-revision": node.Remove("revision"); break;
            case "missing-name": state.Remove("name"); break;
            case "null-pattern": state["patterns"]![0] = null; break;
            case "null-settings": state["settings"] = null; break;
            case "empty-identity": state["id"] = Guid.Empty.ToString(); break;
            case "invalid-uuid": state["id"] = "not-a-uuid"; break;
            case "non-string-uuid": state["id"] = 123; break;
            case "duplicate-identity": state["placements"]![1]!["id"] = state["placements"]![0]!["id"]!.DeepClone(); break;
            case "bad-reference": state["placements"]![0]!["patternId"] = Guid.NewGuid().ToString(); break;
            case "negative-position": note["position"] = -1; break;
            case "non-integer-position": note["position"] = "one-quarter"; break;
            case "position-overflow": note["position"] = decimal.MaxValue; break;
            case "zero-duration": note["duration"] = 0; break;
            case "note-outside-pattern": note["position"] = 4 * MusicalPosition.TicksPerQuarter; break;
            case "pitch-out-of-range": note["pitch"] = 128; break;
            case "intensity-out-of-range": note["intensity"] = -0.1m; break;
            case "unsafe-locator": state["resources"]![0]!["managedLocator"] = "../user.wav"; break;
            case "reserved-extra": state["additionalData"] = new JsonObject(); break;
            case "unknown-essential-enum": state["contexts"]![0]!["level"] = "third-local-level"; break;
            case "oversized-name": state["name"] = new string('x', ProjectValidation.MaximumTextLength + 1); break;
            case "non-number-tempo": state["settings"]!["tempo"] = "fast"; break;
            default: throw new InvalidOperationException("Unknown fixture mutation.");
        }
        Assert.Throws<ProjectFormatException>(() => Read(node));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{\"format\":\"seqvium-project\",\"major\":1,\"major\":2}")]
    [InlineData("{\"format\":\"foreign-project\"}")]
    public void CorruptionAndDuplicatePropertiesRefuse(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        Assert.Throws<ProjectFormatException>(() => ProjectPersistence.Read(stream));
    }

    [Fact]
    public void OversizeAndExcessDepthAreBoundedOnRead()
    {
        using var oversized = new MemoryStream(new byte[ProjectPersistence.MaximumBytes + 1]);
        Assert.Throws<ProjectFormatException>(() => ProjectPersistence.Read(oversized));
        using var nested = new MemoryStream(Encoding.UTF8.GetBytes(new string('[', 65) + "0" + new string(']', 65)));
        Assert.Throws<ProjectFormatException>(() => ProjectPersistence.Read(nested));
    }

    [Fact]
    public void FailedSavePreservesPriorFileSavedRevisionCurrentWorkAndCleansOnlyOwnedTemporary()
    {
        using var directory = new TemporaryDirectory();
        var document = ProjectDocument.Create();
        document.Edit("Original", edit => edit.RenameDocument("Original"));
        var path = directory.File("project.json");
        ProjectPersistence.Save(document, path);
        var bytes = File.ReadAllBytes(path);
        var saved = document.Saved;
        document.Edit("Unsaved", edit => edit.RenameDocument("Unsaved"));
        var current = document.Current;
        var history = document.UndoCount;
        // Existing directory forces failure after the temporary document has been written.
        var error = Record.Exception(() => ProjectPersistence.Save(document, directory.Path));
        Assert.True(error is IOException or UnauthorizedAccessException, $"Expected a filesystem failure; received {error?.GetType()}.");
        Assert.Equal(bytes, File.ReadAllBytes(path));
        Assert.Same(saved, document.Saved);
        Assert.Same(current, document.Current);
        Assert.Equal(history, document.UndoCount);
        Assert.Equal(path, document.SavedPath);
        Assert.True(document.IsDirty);
        Assert.Empty(Directory.GetFiles(directory.Path, ".seqvium-*.tmp"));
        // Locked destination forces failed overwrite of an existing project, not merely a new-path failure.
        if (OperatingSystem.IsWindows())
        {
            using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            var lockedError = Record.Exception(() => ProjectPersistence.Save(document, path));
            Assert.True(lockedError is IOException or UnauthorizedAccessException, $"Expected a filesystem failure; received {lockedError?.GetType()}.");
        }
        Assert.Equal(bytes, File.ReadAllBytes(path));
        Assert.Same(saved, document.Saved);
        Assert.Empty(Directory.GetFiles(directory.Path, ".seqvium-*.tmp"));
    }

    [Fact]
    public void StreamSerializationDoesNotPretendToBeAnExplicitSaveOrPersistSessionState()
    {
        var document = ProjectDocument.Create();
        document.Edit("Accepted", edit => edit.RenameDocument("Accepted"));
        using var stream = new MemoryStream();
        ProjectPersistence.Write(document, stream);
        Assert.True(document.IsDirty);
        Assert.Null(document.Saved);
        Assert.True(stream.CanWrite);
        stream.Position = 0;
        var reopened = ProjectPersistence.Read(stream).Document;
        Assert.Equal("Accepted", reopened.Current.State.Name);
        Assert.Equal(0, reopened.UndoCount);
        Assert.Equal(0, reopened.Generation);
        Assert.NotEqual(document.LifecycleId, reopened.LifecycleId);
        Assert.False(reopened.IsDirty);
    }

    private static IEnumerable<JsonObject> CanonicalObjects(JsonObject root)
    {
        yield return root;
        var state = root["state"]!.AsObject();
        yield return state;
        yield return state["settings"]!.AsObject();
        yield return state["settings"]!["meter"]!.AsObject();
        foreach (var key in new[] { "patterns", "sounds", "placements", "groups", "resources", "contexts", "routes" })
            foreach (var item in state[key]!.AsArray().OfType<JsonObject>())
            {
                yield return item;
                if (key == "patterns")
                    foreach (var part in item["parts"]!.AsArray().OfType<JsonObject>())
                    {
                        yield return part;
                        foreach (var note in part["notes"]!.AsArray().OfType<JsonObject>()) yield return note;
                    }
                if (key == "placements")
                    foreach (var relationship in item["partRelationships"]!.AsArray().OfType<JsonObject>()) yield return relationship;
                if (item["extension"] is JsonObject extension) yield return extension;
            }
    }
}
