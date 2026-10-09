// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphDiagnosticTests
{
    [Fact]
    public void CycleReasonsIdentifyOnlyCycleMembersAndNotDownstreamOutput()
    {
        var f = new GraphFixture(false);
        f.Document.Edit("Self-loop", edit =>
        {
            edit.DisconnectGraphPorts(f.Attachment.GraphId, f.SourceEdge);
            GraphFixture.Connect(edit, f.Attachment.GraphId, f.Gain, f.Gain);
        });
        var cycle = Assert.Single(f.Report.Diagnostics.Where(item => item.Reason == GraphReasons.Cycle));
        Assert.Equal(f.Gain.Id, cycle.NodeId);
        Assert.Equal(f.Attachment.Id, cycle.AttachmentId);
    }

    [Fact]
    public void ContainingAttachmentAndUnattachedDefinitionAreSavableButUnsupported()
    {
        var f = new GraphFixture(false);
        Id<ProcessingContext> context = default;
        f.Document.Edit("Containing intent", edit => context = edit.AddContext("Containing", LocalProcessingLevel.Containing));
        var json = GraphFixture.Json(f.Document);
        json["state"]!["graphAttachments"]![0]!["contextId"] = context.ToString();
        var result = GraphFixture.Read(json);
        Assert.Contains(result.GraphReports.Single().Diagnostics, diagnostic => diagnostic.Reason == GraphReasons.UnsupportedScope);
        json["state"]!["graphAttachments"] = new System.Text.Json.Nodes.JsonArray();
        result = GraphFixture.Read(json);
        Assert.Null(result.GraphReports.Single().AttachmentId);
        Assert.Contains(result.GraphReports.Single().Diagnostics, diagnostic => diagnostic.Reason == GraphReasons.UnsupportedScope);
    }

    [Fact]
    public void MoreThanOneOutputAndSourceNodeCannotDeclareAdditionalInputsAsCapability()
    {
        var f = new GraphFixture(false);
        f.Document.Edit("Extra Output", edit => edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Output, new(0, 0)));
        Assert.Contains(f.Report.Diagnostics, diagnostic => diagnostic.Reason == GraphReasons.MissingOutput);
        var state = Change(f, graph => graph with { Nodes = graph.Nodes.SetItem(0, f.Source with
        { Ports = f.Source.Ports.Add(f.Gain.Ports[0] with { Id = Id<GraphPort>.New() }) }) });
        Assert.Contains(GraphDiagnostics.Inspect(state).Single().Diagnostics, diagnostic => diagnostic.Reason == GraphReasons.InvalidDeclaration && diagnostic.NodeId == f.Source.Id);
    }

    private static ProjectState Change(GraphFixture f, Func<GraphDefinition, GraphDefinition> operation) =>
        f.Document.Current.State with { Graphs = [operation(f.Graph)] };
    private static GraphConnection Edge(GraphNode from, GraphNode to) => new(Id<GraphConnection>.New(), from.Id,
        from.Ports.Single(port => port.Direction == GraphPortDirection.Output).Id, to.Id,
        to.Ports.First(port => port.Direction == GraphPortDirection.Input).Id);

    [Theory]
    [InlineData("required-input", GraphReasons.RequiredInput)]
    [InlineData("unknown-node", GraphReasons.MissingCapability)]
    [InlineData("unknown-version", GraphReasons.MissingCapability)]
    [InlineData("opaque-state", GraphReasons.MissingCapability)]
    [InlineData("detector", GraphReasons.UnsupportedPort)]
    [InlineData("musical", GraphReasons.UnsupportedPort)]
    [InlineData("control", GraphReasons.UnsupportedPort)]
    [InlineData("unknown-class", GraphReasons.UnsupportedPort)]
    [InlineData("unknown-layout", GraphReasons.UnsupportedPort)]
    [InlineData("unknown-rate", GraphReasons.UnsupportedPort)]
    [InlineData("incompatible-layout", GraphReasons.IncompatiblePorts)]
    [InlineData("invalid-declaration", GraphReasons.InvalidDeclaration)]
    [InlineData("gain-negative", GraphReasons.InvalidGain)]
    [InlineData("gain-above-unity", GraphReasons.InvalidGain)]
    [InlineData("gain-string", GraphReasons.InvalidGain)]
    [InlineData("gain-missing", GraphReasons.InvalidGain)]
    [InlineData("duplicate-edge", GraphReasons.DuplicateConnection)]
    [InlineData("multiple-input-edges", GraphReasons.InputCardinality)]
    [InlineData("missing-node", GraphReasons.MissingEndpoint)]
    [InlineData("missing-port", GraphReasons.MissingEndpoint)]
    public void SafeInvalidIntentHasStableAffectedIdentitiesAndNeverClaimsEligibility(string failure, string reason)
    {
        var f = new GraphFixture();
        var state = Change(f, graph =>
        {
            var gain = f.Gain;
            var port = gain.Ports[0];
            switch (failure)
            {
                case "required-input": return graph with { Connections = graph.Connections.RemoveAt(0) };
                case "unknown-node": gain = gain with { Type = "org.example.future" }; break;
                case "unknown-version": gain = gain with { StateVersion = 7 }; break;
                case "opaque-state": gain = gain with { Extension = new("org.example.fx", 1, JsonSerializer.SerializeToElement(17)) }; break;
                case "detector": port = port with { Use = GraphBuiltIns.Detector }; break;
                case "musical": port = port with { SignalClass = GraphBuiltIns.Musical }; break;
                case "control": port = port with { SignalClass = GraphBuiltIns.Control }; break;
                case "unknown-class": port = port with { SignalClass = "future.signal" }; break;
                case "unknown-layout": port = port with { Layout = "future.surround" }; break;
                case "unknown-rate": port = port with { RatePolicy = "future.rate" }; break;
                case "incompatible-layout": port = port with { Layout = GraphBuiltIns.Mono }; break;
                case "invalid-declaration": port = port with { Required = false, Role = "foreign" }; break;
                case "gain-negative": gain = gain with { Parameters = gain.Parameters.SetItem(GraphBuiltIns.GainAmplitude, JsonSerializer.SerializeToElement(-0.1m)) }; break;
                case "gain-above-unity": gain = gain with { Parameters = gain.Parameters.SetItem(GraphBuiltIns.GainAmplitude, JsonSerializer.SerializeToElement(1.1m)) }; break;
                case "gain-string": gain = gain with { Parameters = gain.Parameters.SetItem(GraphBuiltIns.GainAmplitude, JsonSerializer.SerializeToElement("half")) }; break;
                case "gain-missing": gain = gain with { Parameters = ImmutableDictionary<string, JsonElement>.Empty }; break;
                case "duplicate-edge": return graph with { Connections = graph.Connections.Add(graph.Connections[0] with { Id = Id<GraphConnection>.New() }) };
                case "multiple-input-edges": return graph with { Connections = graph.Connections.Add(Edge(f.OtherSource, f.Gain)) };
                case "missing-node": return graph with { Connections = graph.Connections.SetItem(0, graph.Connections[0] with { FromNodeId = Id<GraphNode>.New() }) };
                case "missing-port": return graph with { Connections = graph.Connections.SetItem(0, graph.Connections[0] with { FromPortId = Id<GraphPort>.New() }) };
                default: throw new InvalidOperationException();
            }
            gain = gain with { Ports = gain.Ports.SetItem(0, port) };
            return graph with { Nodes = graph.Nodes.SetItem(graph.Nodes.IndexOf(f.Gain), gain) };
        });
        ProjectValidation.Validate(state);
        var report = GraphDiagnostics.Inspect(state).Single();
        Assert.False(report.IsEligibleForPreparation);
        Assert.Contains(report.Diagnostics, diagnostic => diagnostic.Reason == reason);
        Assert.All(report.Diagnostics, diagnostic =>
        {
            Assert.Equal(f.Attachment.GraphId, diagnostic.GraphId);
            Assert.Equal(f.Attachment.Id, diagnostic.AttachmentId);
        });
        Assert.Equal(report.Diagnostics.ToArray(), GraphDiagnostics.Inspect(state).Single().Diagnostics.ToArray());
    }

    [Theory]
    [InlineData("self")]
    [InlineData("two-node")]
    [InlineData("long")]
    [InlineData("disconnected")]
    [InlineData("unsupported-port")]
    public void AllDirectedCycleClassesBlockEvenOutsideSelectedOutput(string cycle)
    {
        var f = new GraphFixture(false);
        f.Document.Edit("Cycles are editable intent", edit =>
        {
            if (cycle == "self")
            {
                edit.DisconnectGraphPorts(f.Attachment.GraphId, f.SourceEdge);
                GraphFixture.Connect(edit, f.Attachment.GraphId, f.Gain, f.Gain);
            }
            else
            {
                var first = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Gain, new(0, 0));
                var second = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Gain, new(0, 0));
                GraphFixture.Connect(edit, f.Attachment.GraphId, first, second);
                if (cycle == "long")
                {
                    var third = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Gain, new(0, 0));
                    GraphFixture.Connect(edit, f.Attachment.GraphId, second, third);
                    GraphFixture.Connect(edit, f.Attachment.GraphId, third, first);
                    edit.DisconnectGraphPorts(f.Attachment.GraphId, f.Graph.Connections.Single(edge => edge.ToNodeId == f.Output.Id).Id);
                    GraphFixture.Connect(edit, f.Attachment.GraphId, third, f.Output);
                }
                else
                {
                    GraphFixture.Connect(edit, f.Attachment.GraphId, second, first);
                    if (cycle == "two-node")
                    {
                        edit.DisconnectGraphPorts(f.Attachment.GraphId, f.Graph.Connections.Single(edge => edge.ToNodeId == f.Output.Id).Id);
                        GraphFixture.Connect(edit, f.Attachment.GraphId, second, f.Output);
                    }
                }
            }
        });
        var state = f.Document.Current.State;
        if (cycle == "unsupported-port") state = state with
        {
            Graphs = [f.Graph with { Nodes = [.. f.Graph.Nodes.Select(node => node with
            { Ports = [.. node.Ports.Select(port => port with { SignalClass = GraphBuiltIns.Control })] })] }]
        };
        Assert.Contains(GraphDiagnostics.Inspect(state).Single().Diagnostics, diagnostic => diagnostic.Reason == GraphReasons.Cycle);
        var loaded = GraphFixture.Reopen(f.Document);
        Assert.Contains(loaded.GraphReports.Single().Diagnostics, diagnostic => diagnostic.Reason == GraphReasons.Cycle);
    }

    [Fact]
    public void TopologyDiagnosticsIgnoreNodeAndConnectionOrderAndCanvasGeometry()
    {
        var f = new GraphFixture();
        Assert.True(f.Report.IsEligibleForPreparation);
        var state = Change(f, graph => graph with
        {
            Nodes = [.. graph.Nodes.Reverse().Select(node => node with { Position = new(-node.Position.X, -node.Position.Y) })],
            Connections = [.. graph.Connections.Reverse()]
        });
        Assert.True(GraphDiagnostics.Inspect(state).Single().IsEligibleForPreparation);
        state = state with { Graphs = [state.Graphs[0] with { Connections = [] }] };
        var diagnostics = GraphDiagnostics.Inspect(state).Single().Diagnostics;
        state = state with { Graphs = [state.Graphs[0] with { Nodes = [.. state.Graphs[0].Nodes.Reverse()] }] };
        Assert.Equal(diagnostics.ToArray(), GraphDiagnostics.Inspect(state).Single().Diagnostics.ToArray());
    }

    [Theory]
    [InlineData("missing-binding", GraphReasons.MissingSource)]
    [InlineData("silent-part-omitted", GraphReasons.SourceCoverage)]
    [InlineData("wrong-placement", GraphReasons.UnresolvedSource)]
    [InlineData("missing-part", GraphReasons.UnresolvedSource)]
    [InlineData("missing-source-node", GraphReasons.UnresolvedSource)]
    [InlineData("unsupported-boundary", GraphReasons.UnresolvedSource)]
    [InlineData("missing-output", GraphReasons.MissingOutput)]
    [InlineData("wrong-output", GraphReasons.MissingOutput)]
    [InlineData("aggregate-mismatch", GraphReasons.Aggregation)]
    [InlineData("unreachable-source", GraphReasons.UnreachableSource)]
    [InlineData("missing-source-capability", GraphReasons.MissingCapability)]
    [InlineData("invalid-source-config", GraphReasons.MissingCapability)]
    public void RequiredSourceCoverageAndAggregationRemainExplicit(string failure, string reason)
    {
        var f = new GraphFixture();
        var state = f.Document.Current.State;
        var attachment = f.CurrentAttachment;
        switch (failure)
        {
            case "missing-binding": attachment = attachment with { Sources = [] }; break;
            case "silent-part-omitted": attachment = attachment with { Sources = attachment.Sources.RemoveAt(1) }; break;
            case "wrong-placement": attachment = attachment with { Sources = attachment.Sources.SetItem(0, attachment.Sources[0] with { PlacementId = f.OtherPlacement }) }; break;
            case "missing-part": attachment = attachment with { Sources = attachment.Sources.SetItem(0, attachment.Sources[0] with { PartId = Id<MusicalPart>.New() }) }; break;
            case "missing-source-node": attachment = attachment with { Sources = attachment.Sources.SetItem(0, attachment.Sources[0] with { NodeId = Id<GraphNode>.New() }) }; break;
            case "unsupported-boundary": attachment = attachment with { Sources = attachment.Sources.SetItem(0, attachment.Sources[0] with { Boundary = "post-containing" }) }; break;
            case "missing-output": attachment = attachment with { OutputNodeId = null }; break;
            case "wrong-output": attachment = attachment with { OutputNodeId = f.Gain.Id }; break;
            case "aggregate-mismatch": state = state with { Contexts = [state.Contexts[0] with { IntentionalMix = false }] }; break;
            case "unreachable-source": state = Change(f, graph => graph with { Connections = [.. graph.Connections.Where(edge => edge.FromNodeId != f.OtherSource.Id)] }); break;
            case "missing-source-capability": state = state with { Sounds = [state.Sounds[0] with { Algorithm = "org.example.missing" }] }; break;
            case "invalid-source-config": state = state with { Sounds = [state.Sounds[0] with { Parameters = state.Sounds[0].Parameters.SetItem("rootPitch", 200m) }] }; break;
            default: throw new InvalidOperationException();
        }
        state = state with { GraphAttachments = [attachment] };
        ProjectValidation.Validate(state);
        Assert.Contains(GraphDiagnostics.Inspect(state).Single().Diagnostics, diagnostic => diagnostic.Reason == reason);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlacementRouteBlocksPreparationWithoutChangingIntentThroughSaveAndHistory(bool twoParts)
    {
        var f = new GraphFixture(twoParts);
        Assert.True(f.Report.IsEligibleForPreparation);
        Id<RouteIntent> routeId = default;
        Id<RouteIntent> otherRouteId = default;
        f.Document.Edit("Existing downstream routes", edit =>
        {
            routeId = edit.AddRoute("Downstream");
            otherRouteId = edit.AddRoute("Downstream");
            edit.SetPlacementRelationships(f.OtherPlacement, null, null, otherRouteId, []);
        });
        var beforeRouting = f.Document.Current;
        AssertIntent(f.Document, false);
        Assert.True(f.Document.Edit("Route local result", edit =>
            edit.SetPlacementRelationships(f.Placement, f.Attachment.ContextId, null, routeId, [])));
        var routed = f.Document.Current;
        AssertIntent(f.Document, true);
        Assert.Equal(beforeRouting.State.Graphs, routed.State.Graphs);
        Assert.Equal(beforeRouting.State.GraphAttachments, routed.State.GraphAttachments);

        using var directory = new TemporaryDirectory();
        var path = directory.File("placement-route.json");
        var json = GraphFixture.Json(f.Document);
        ProjectPersistence.Save(f.Document, path);
        var reopened = ProjectPersistence.Open(path);
        AssertIntent(reopened.Document, true);
        Assert.Equal(f.Report.Diagnostics.ToArray(), reopened.GraphReports.Single().Diagnostics.ToArray());
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(json, GraphFixture.Json(reopened.Document)));

        Assert.True(f.Document.Undo());
        Assert.Equal(beforeRouting, f.Document.Current);
        AssertIntent(f.Document, false);
        Assert.True(f.Document.Redo());
        Assert.Equal(routed, f.Document.Current);
        AssertIntent(f.Document, true);
        Assert.False(f.Document.IsDirty);
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(json, GraphFixture.Json(f.Document)));

        void AssertIntent(ProjectDocument document, bool routedPlacement)
        {
            var state = document.Current.State;
            ProjectValidation.Validate(state);
            Assert.Equal(new RouteIntent(routeId, "Downstream"), state.Routes.Single(route => route.Id == routeId));
            Assert.Equal(new RouteIntent(otherRouteId, "Downstream"), state.Routes.Single(route => route.Id == otherRouteId));
            Assert.Equal(2, state.Routes.Length);
            Assert.Equal(routedPlacement ? routeId : (Id<RouteIntent>?)null,
                state.Placements.Single(placement => placement.Id == f.Placement).RouteId);
            Assert.Equal(otherRouteId, state.Placements.Single(placement => placement.Id == f.OtherPlacement).RouteId);
            var report = GraphDiagnostics.Inspect(state).Single();
            Assert.Equal(!routedPlacement, report.IsEligibleForPreparation);
            if (routedPlacement)
                Assert.Equal(new GraphDiagnostic(GraphReasons.UnsupportedDependency, f.Attachment.GraphId, f.Attachment.Id),
                    Assert.Single(report.Diagnostics));
            else Assert.Empty(report.Diagnostics);
        }
    }

    [Theory]
    [InlineData("containing")]
    [InlineData("shared-performance")]
    [InlineData("part-route")]
    [InlineData("opaque-context")]
    public void UnsupportedExistingIntentIsPreservedAndBlocksFirstCapability(string intent)
    {
        var f = new GraphFixture(false);
        f.Document.Edit("Unsupported relationships", edit =>
        {
            var containing = intent == "containing" ? edit.AddContext("Container", LocalProcessingLevel.Containing) : (Id<ProcessingContext>?)null;
            var route = intent == "part-route" ? edit.AddRoute("Separate part route") : (Id<RouteIntent>?)null;
            edit.SetPlacementRelationships(f.Placement, f.Attachment.ContextId, containing, null,
                [new(f.FirstPart, intent == "shared-performance" ? Guid.NewGuid() : null, route)]);
        });
        var json = GraphFixture.Json(f.Document);
        if (intent == "opaque-context") json["state"]!["contexts"]![0]!["extension"] = new System.Text.Json.Nodes.JsonObject
        { ["extensionId"] = "org.example.context", ["stateVersion"] = 1, ["payload"] = new System.Text.Json.Nodes.JsonObject { ["foreign"] = true } };
        var result = GraphFixture.Read(json);
        Assert.Contains(result.GraphReports.Single().Diagnostics, diagnostic => diagnostic.Reason == GraphReasons.UnsupportedDependency);
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(json, GraphFixture.Json(result.Document)));
    }

    [Theory]
    [InlineData("nodes")]
    [InlineData("edges")]
    [InlineData("sources")]
    public void ProcessingLimitsBlockWithoutRejectingSafeDocumentSize(string limit)
    {
        var f = new GraphFixture();
        var state = f.Document.Current.State;
        if (limit == "nodes") state = Change(f, graph => graph with
        { Nodes = graph.Nodes.AddRange(Enumerable.Range(0, GraphDiagnostics.MaximumNodes).Select(_ => GraphBuiltIns.Create(GraphBuiltIns.Gain, new(0, 0)))) });
        if (limit == "edges") state = Change(f, graph => graph with
        { Connections = [.. Enumerable.Range(0, GraphDiagnostics.MaximumConnections + 1).Select(_ => graph.Connections[0] with { Id = Id<GraphConnection>.New() })] });
        if (limit == "sources") state = state with { GraphAttachments = [f.CurrentAttachment with
        { Sources = [.. Enumerable.Range(0, GraphDiagnostics.MaximumSources + 1).Select(_ => f.CurrentAttachment.Sources[0] with { Id = Id<GraphSourceBinding>.New() })] }] };
        ProjectValidation.Validate(state);
        Assert.Equal(GraphReasons.ProcessingLimit, GraphDiagnostics.Inspect(state).Single().Diagnostics.Single().Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void VersionOneGainUsesDecimalLinearAmplitudeAndAcceptsAttenuation(double amplitude)
    {
        var f = new GraphFixture(false);
        f.Document.Edit("Gain", edit => edit.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, (decimal)amplitude));
        Assert.True(f.Report.IsEligibleForPreparation);
        Assert.Equal((decimal)amplitude, f.Graph.Nodes.Single(node => node.Id == f.Gain.Id).Parameters[GraphBuiltIns.GainAmplitude].GetDecimal());
    }
}
