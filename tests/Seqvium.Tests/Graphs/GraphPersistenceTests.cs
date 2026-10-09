// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphPersistenceTests
{
    [Fact]
    public void GraphFreeHistoricalConstructorFieldsLoadSaveAndReopenWithEmptyDefaultsAndPreciseTicks()
    {
        var f = new SyntheticComposition();
        f.Document.Edit("Large precise time", edit => edit.MovePlacement(f.Placements[0], new(9_007_199_254_740_993)));
        var json = GraphFixture.Json(f.Document);
        var state = json["state"]!.AsObject();
        state.Remove("graphs"); state.Remove("graphAttachments");
        Assert.Equal(0, json["minor"]!.GetValue<int>());
        var loaded = GraphFixture.Read(json).Document;
        Assert.Empty(loaded.Current.State.Graphs);
        Assert.Empty(loaded.Current.State.GraphAttachments);
        Assert.Equal(9_007_199_254_740_993, loaded.Current.State.Placements[0].Position.Ticks);
        using var directory = new TemporaryDirectory();
        var path = directory.File("legacy.json");
        ProjectPersistence.Save(loaded, path);
        var reopened = ProjectPersistence.Open(path).Document;
        var encoded = GraphFixture.Json(reopened);
        encoded["state"]!.AsObject().Remove("graphs"); encoded["state"]!.AsObject().Remove("graphAttachments");
        Assert.True(JsonNode.DeepEquals(json, encoded));
        Assert.Equal(0, encoded["minimumReaderMinor"]!.GetValue<int>());
    }

    [Fact]
    public void GraphRoundTripPreservesEveryStableIdentityParameterAndCoordinate()
    {
        var f = new GraphFixture();
        f.Document.Edit("Gain and geometry", edit =>
        {
            edit.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, 0.1234567890123456789012345678m);
            edit.MoveGraphNode(f.Attachment.GraphId, f.Gain.Id, new(987.125, -432.5));
        });
        using var directory = new TemporaryDirectory();
        var original = GraphFixture.Json(f.Document);
        var path = directory.File("graph.json");
        ProjectPersistence.Save(f.Document, path);
        var result = ProjectPersistence.Open(path);
        Assert.True(JsonNode.DeepEquals(original, GraphFixture.Json(result.Document)));
        Assert.True(result.GraphReports.Single().IsEligibleForPreparation);
        Assert.Equal(1, original["minor"]!.GetValue<int>());
        Assert.Equal(1, original["minimumReaderMinor"]!.GetValue<int>());
        Assert.Equal(f.Document.Current.Revision, result.Document.Current.Revision);
        Assert.Equal(f.Document.Current.State.Id, result.Document.Current.State.Id);
        Assert.False(result.Document.IsDirty);
        Assert.Equal(0, result.Document.UndoCount);
    }

    // Frozen 1.0 envelope refusal contract from the actual baseline codec. It precedes decoding
    // unknown state fields, which is the critical defense against silently ignoring processing.
    private static bool BaselineReader10AcceptsEnvelope(JsonObject json) =>
        json["format"]!.GetValue<string>() == "seqvium-project" && json["major"]!.GetValue<int>() == 1 &&
        json["minor"]!.GetValue<int>() >= 0 && json["minimumReaderMinor"]!.GetValue<int>() >= 0 &&
        json["minimumReaderMinor"]!.GetValue<int>() <= json["minor"]!.GetValue<int>() &&
        json["minimumReaderMinor"]!.GetValue<int>() <= 0;

    [Fact]
    public void OldReaderRefusesEvenIncompleteMeaningfulGraphAndNewReaderRefusesUnderdeclaredRequirement()
    {
        var f = new GraphFixture(false);
        f.Document.Edit("Unfinished", edit => edit.DisconnectGraphPorts(f.Attachment.GraphId, f.SourceEdge));
        var json = GraphFixture.Json(f.Document);
        Assert.False(BaselineReader10AcceptsEnvelope(json));
        Assert.False(GraphFixture.Read(json).GraphReports.Single().IsEligibleForPreparation);
        json["minimumReaderMinor"] = 0;
        Assert.True(BaselineReader10AcceptsEnvelope(json));
        Assert.Contains("Graph intent requires", Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json)).Message);
        Assert.True(BaselineReader10AcceptsEnvelope(GraphFixture.Json(ProjectDocument.Create())));
    }

    [Fact]
    public void HigherCompatibleMinorAndExistingMinimumNeverDecreaseThroughGraphRemovalOrSave()
    {
        var f = new GraphFixture(false);
        var json = GraphFixture.Json(f.Document);
        json["minor"] = 5;
        json["futureEnvelope"] = "preserve";
        var loaded = GraphFixture.Read(json).Document;
        loaded.Edit("Remove graph", edit => edit.DeleteGraph(f.Attachment.GraphId));
        var encoded = GraphFixture.Json(loaded);
        Assert.Equal(5, encoded["minor"]!.GetValue<int>());
        Assert.Equal(1, encoded["minimumReaderMinor"]!.GetValue<int>());
        Assert.Equal("preserve", encoded["futureEnvelope"]!.GetValue<string>());
        using var directory = new TemporaryDirectory();
        ProjectPersistence.Save(f.Document, directory.File("original.json"));
        f.Document.Edit("Remove after save", edit => edit.DeleteGraph(f.Attachment.GraphId));
        ProjectPersistence.Save(f.Document, directory.File("without-graph.json"));
        Assert.Equal(1, GraphFixture.Json(f.Document)["minimumReaderMinor"]!.GetValue<int>());
        json["minimumReaderMinor"] = 2;
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
    }

    [Fact]
    public void UnknownGraphFieldsAndNodeParametersRemainAtTheirOriginalBoundariesThroughKnownEditsAndHistory()
    {
        var f = new GraphFixture();
        var json = GraphFixture.Json(f.Document);
        var state = json["state"]!;
        var graph = state["graphs"]![0]!;
        var attachment = state["graphAttachments"]![0]!;
        var boundaries = new List<JsonNode> { graph, attachment, attachment["sources"]![0]!, graph["connections"]![0]! };
        foreach (var node in graph["nodes"]!.AsArray().OfType<JsonObject>())
        {
            boundaries.Add(node); boundaries.Add(node["position"]!);
            boundaries.AddRange(node["ports"]!.AsArray().OfType<JsonObject>());
        }
        foreach (var boundary in boundaries) boundary["futureOptional"] = JsonNode.Parse("{\"nested\":[null,1,\"keep\"]}");
        var gain = graph["nodes"]!.AsArray().OfType<JsonObject>().Single(node => node["type"]!.GetValue<string>() == GraphBuiltIns.Gain);
        gain["type"] = "org.example.fx";
        gain["stateVersion"] = 12;
        gain["parameters"]!["foreign"] = JsonNode.Parse("{\"units\":\"future\",\"data\":[1,2]}");
        gain["extension"] = JsonNode.Parse("{\"extensionId\":\"org.example.fx\",\"stateVersion\":9,\"payload\":{\"blob\":\"keep\"},\"futureOptional\":true}");
        // New reader recognizes graph properties formerly unknown to reader 1.0; no duplicated shadow keys.
        var result = GraphFixture.Read(json);
        Assert.True(JsonNode.DeepEquals(json, GraphFixture.Json(result.Document)));
        Assert.Contains(result.GraphReports.Single().Diagnostics, diagnostic => diagnostic.Reason == GraphReasons.MissingCapability && diagnostic.NodeId == f.Gain.Id);
        var before = result.Document.Current;
        result.Document.Edit("Geometry only", edit => edit.MoveGraphNode(f.Attachment.GraphId, f.Gain.Id, new(123, 456)));
        json["revision"] = result.Document.Current.Revision.ToString();
        gain["position"]!["x"] = 123; gain["position"]!["y"] = 456;
        Assert.True(JsonNode.DeepEquals(json, GraphFixture.Json(result.Document)));
        result.Document.Undo();
        Assert.Same(before, result.Document.Current);
        result.Document.Redo();
        Assert.True(JsonNode.DeepEquals(json, GraphFixture.Json(GraphFixture.Reopen(result.Document).Document)));
    }

    [Theory]
    [InlineData("disconnected")]
    [InlineData("cycle")]
    [InlineData("unknown-node")]
    [InlineData("invalid-gain")]
    [InlineData("unresolved-binding")]
    [InlineData("unresolved-edge")]
    [InlineData("unsupported-port")]
    [InlineData("duplicate-edge")]
    public void SafeInvalidGraphsSaveAndReopenWithOriginalUnresolvedIdentity(string failure)
    {
        var f = new GraphFixture(false);
        var json = GraphFixture.Json(f.Document);
        var graph = json["state"]!["graphs"]![0]!;
        var nodes = graph["nodes"]!.AsArray();
        var gain = nodes.Single(node => node!["type"]!.GetValue<string>() == GraphBuiltIns.Gain)!;
        var edge = graph["connections"]![0]!;
        var unresolved = Guid.NewGuid().ToString();
        switch (failure)
        {
            case "disconnected": graph["connections"] = new JsonArray(); break;
            case "cycle": edge["fromNodeId"] = gain["id"]!.DeepClone(); edge["fromPortId"] = gain["ports"]![1]!["id"]!.DeepClone(); break;
            case "unknown-node": gain["type"] = "org.example.absent"; break;
            case "invalid-gain": gain["parameters"]![GraphBuiltIns.GainAmplitude] = 2; break;
            case "unresolved-binding": json["state"]!["graphAttachments"]![0]!["sources"]![0]!["partId"] = unresolved; break;
            case "unresolved-edge": edge["fromNodeId"] = unresolved; break;
            case "unsupported-port": gain["ports"]![0]!["signalClass"] = "future.signal"; break;
            case "duplicate-edge":
                var duplicate = edge.DeepClone(); duplicate["id"] = unresolved;
                graph["connections"]!.AsArray().Add(duplicate); break;
            default: throw new InvalidOperationException();
        }
        var loaded = GraphFixture.Read(json);
        Assert.True(loaded.IsDegraded);
        Assert.False(loaded.GraphReports.Single().IsEligibleForPreparation);
        using var directory = new TemporaryDirectory();
        var path = directory.File("unfinished.json");
        var saved = ProjectPersistence.SaveWithReport(loaded.Document, path);
        Assert.True(saved.IsDegraded);
        Assert.False(saved.GraphReports.Single().IsEligibleForPreparation);
        var reopened = ProjectPersistence.Open(path);
        Assert.False(reopened.GraphReports.Single().IsEligibleForPreparation);
        Assert.True(JsonNode.DeepEquals(json, GraphFixture.Json(reopened.Document)));
    }

    [Theory]
    [InlineData("empty-graph-id")]
    [InlineData("duplicate-graph-id")]
    [InlineData("duplicate-node-id")]
    [InlineData("duplicate-port-id")]
    [InlineData("duplicate-edge-id")]
    [InlineData("duplicate-attachment-id")]
    [InlineData("duplicate-binding-id")]
    [InlineData("cross-kind-id")]
    [InlineData("missing-node-type")]
    [InlineData("missing-port-role")]
    [InlineData("missing-graph-array")]
    [InlineData("null-node")]
    [InlineData("null-parameters")]
    [InlineData("null-position")]
    [InlineData("invalid-position")]
    [InlineData("oversized-position")]
    [InlineData("malformed-reference")]
    [InlineData("empty-reference")]
    [InlineData("unknown-direction")]
    [InlineData("unsafe-cardinality")]
    [InlineData("zero-version")]
    [InlineData("missing-context-owner")]
    [InlineData("missing-graph-owner")]
    [InlineData("shared-graph-ownership")]
    [InlineData("reserved-extra")]
    [InlineData("oversized-semantic-key")]
    public void EssentialUnsafeGraphShapeRefusesWithoutGuessing(string failure)
    {
        var f = new GraphFixture();
        var json = GraphFixture.Json(f.Document);
        var state = json["state"]!;
        var graph = state["graphs"]![0]!;
        var node = graph["nodes"]![0]!.AsObject();
        var port = node["ports"]![0]!.AsObject();
        var edge = graph["connections"]![0]!;
        var attachment = state["graphAttachments"]![0]!;
        switch (failure)
        {
            case "empty-graph-id": graph["id"] = Guid.Empty.ToString(); break;
            case "duplicate-graph-id": state["graphs"]!.AsArray().Add(graph.DeepClone()); break;
            case "duplicate-node-id": graph["nodes"]![1]!["id"] = node["id"]!.DeepClone(); break;
            case "duplicate-port-id": graph["nodes"]![1]!["ports"]![0]!["id"] = port["id"]!.DeepClone(); break;
            case "duplicate-edge-id": graph["connections"]![1]!["id"] = edge["id"]!.DeepClone(); break;
            case "duplicate-attachment-id": state["graphAttachments"]!.AsArray().Add(attachment.DeepClone()); break;
            case "duplicate-binding-id": attachment["sources"]![1]!["id"] = attachment["sources"]![0]!["id"]!.DeepClone(); break;
            case "cross-kind-id": port["id"] = state["sounds"]![0]!["id"]!.DeepClone(); break;
            case "missing-node-type": node.Remove("type"); break;
            case "missing-port-role": port.Remove("role"); break;
            case "missing-graph-array": graph.AsObject().Remove("nodes"); break;
            case "null-node": graph["nodes"]![0] = null; break;
            case "null-parameters": node["parameters"] = null; break;
            case "null-position": node["position"] = null; break;
            case "invalid-position": node["position"]!["x"] = "NaN"; break;
            case "oversized-position": node["position"]!["x"] = 1_000_001; break;
            case "malformed-reference": edge["fromPortId"] = "bad UUID"; break;
            case "empty-reference": edge["fromPortId"] = Guid.Empty.ToString(); break;
            case "unknown-direction": port["direction"] = "future-direction"; break;
            case "unsafe-cardinality": port["cardinality"] = "single"; break;
            case "zero-version": node["stateVersion"] = 0; break;
            case "missing-context-owner": attachment["contextId"] = Guid.NewGuid().ToString(); break;
            case "missing-graph-owner": attachment["graphId"] = Guid.NewGuid().ToString(); break;
            case "shared-graph-ownership":
                var extra = attachment.DeepClone(); extra["id"] = Guid.NewGuid().ToString();
                extra["sources"] = new JsonArray(); state["graphAttachments"]!.AsArray().Add(extra); break;
            case "reserved-extra": node["additionalData"] = new JsonObject(); break;
            case "oversized-semantic-key": port["signalClass"] = new string('x', ProjectValidation.MaximumTextLength + 1); break;
            default: throw new InvalidOperationException();
        }
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
    }

    [Fact]
    public void GraphPortAndEntityCountsAndOpaqueDepthRemainBounded()
    {
        var f = new GraphFixture(false);
        var state = f.Document.Current.State;
        var node = f.Gain with { Ports = [.. Enumerable.Range(0, 257).Select(_ => f.Gain.Ports[0] with { Id = Id<GraphPort>.New() })] };
        Assert.Throws<ProjectValidationException>(() => ProjectValidation.Validate(state with { Graphs = [f.Graph with { Nodes = [node] }] }));
        var graphs = Enumerable.Range(0, ProjectValidation.MaximumEntities).Select(_ => new GraphDefinition(Id<GraphDefinition>.New(), [], [])).ToImmutableArray();
        Assert.Throws<ProjectValidationException>(() => ProjectValidation.Validate(state with { Graphs = graphs, GraphAttachments = [] }));
        var json = GraphFixture.Json(f.Document);
        json["state"]!["graphs"]![0]!["nodes"]![0]!["parameters"]!["deep"] = JsonNode.Parse(new string('[', 60) + "0" + new string(']', 60));
        Assert.Throws<ProjectFormatException>(() => GraphFixture.Read(json));
        using var oversized = new MemoryStream(new byte[ProjectPersistence.MaximumBytes + 1]);
        Assert.Throws<ProjectFormatException>(() => ProjectPersistence.Read(oversized));
    }

    [Fact]
    public void FailedGraphSavePreservesCurrentSavedStateHistoryAndPreviousFile()
    {
        var f = new GraphFixture();
        using var directory = new TemporaryDirectory();
        var path = directory.File("saved.json");
        ProjectPersistence.Save(f.Document, path);
        var bytes = File.ReadAllBytes(path);
        var saved = f.Document.Saved;
        f.Document.Edit("Unsaved topology", edit => edit.DisconnectGraphPorts(f.Attachment.GraphId, f.SourceEdge));
        var current = f.Document.Current;
        var generation = f.Document.Generation;
        var history = f.Document.UndoCount;
        var error = Record.Exception(() => ProjectPersistence.Save(f.Document, directory.Path));
        Assert.True(error is IOException or UnauthorizedAccessException);
        Assert.Equal(bytes, File.ReadAllBytes(path));
        Assert.Same(saved, f.Document.Saved);
        Assert.Same(current, f.Document.Current);
        Assert.Equal(generation, f.Document.Generation);
        Assert.Equal(history, f.Document.UndoCount);
        Assert.Equal(path, f.Document.SavedPath);
        Assert.True(f.Document.IsDirty);
        Assert.Empty(Directory.GetFiles(directory.Path, ".seqvium-*.tmp"));
    }

    [Fact]
    public void EndedDeletedAndReentrantGraphOperationsCannotPublishStaleWork()
    {
        var f = new GraphFixture();
        ProjectEdit retained = null!;
        f.Document.Edit("Capture", edit => retained = edit);
        Assert.Throws<InvalidOperationException>(() => retained.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, 0.3m));
        var before = f.Document.Current;
        Assert.Throws<InvalidOperationException>(() => f.Document.Edit("Reentrant", edit =>
        {
            edit.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, 0.3m);
            f.Document.Undo();
        }));
        Assert.Same(before, f.Document.Current);
        f.Document.Edit("Delete graph", edit => edit.DeleteGraph(f.Attachment.GraphId));
        var removed = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Stale graph", edit => edit.MoveGraphNode(f.Attachment.GraphId, f.Gain.Id, new(0, 0))));
        Assert.Same(removed, f.Document.Current);
        f.Document.Close();
        Assert.Throws<InvalidOperationException>(() => f.Document.Edit("Closed", edit => edit.CreateItemGraph(f.Placement)));
    }
}
