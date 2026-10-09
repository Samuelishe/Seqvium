// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using System.Text.Json;
using Seqvium.Core;
using Xunit;

namespace Seqvium.Tests;

public sealed class GraphEditingTests
{
    [Fact]
    public void NodeParametersOwnTheirJsonLifetimeAndUnknownContentSurvivesHistory()
    {
        var f = new GraphFixture(false);
        GraphNode node;
        using (var json = JsonDocument.Parse("{\"foreign\":[1,{\"value\":true}]}"))
            node = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(0, 0)) with
            {
                Type = "org.example.opaque",
                Parameters = ImmutableDictionary<string, JsonElement>.Empty.Add("foreign", json.RootElement)
            };
        f.Document.Edit("Retain unavailable declaration", edit => edit.AddGraphNode(f.Attachment.GraphId, node));
        Assert.True(node.Parameters["foreign"].GetProperty("foreign")[1].GetProperty("value").GetBoolean());
        var bytes = ProjectPersistence.Encode(f.Document);
        f.Document.Undo();
        f.Document.Redo();
        Assert.Equal(bytes, ProjectPersistence.Encode(f.Document));
        Assert.False(f.Report.IsEligibleForPreparation);
    }

    [Fact]
    public void SeparateSoundsOverSameResourceRetainSpecificPartBindings()
    {
        var f = new GraphFixture();
        f.Document.Edit("Independent part sound", edit => edit.MakeSoundIndependent(f.Pattern, f.SecondPart, "Same sound"));
        Assert.Equal(2, f.Document.Current.State.Sounds.Length);
        Assert.All(f.Document.Current.State.Sounds, sound => Assert.Equal(f.Resource, sound.ResourceIds.Single()));
        Assert.Equal(new[] { f.FirstPart, f.SecondPart }, f.CurrentAttachment.Sources.Select(binding => binding.PartId));
        Assert.True(f.Report.IsEligibleForPreparation);
    }

    [Fact]
    public void DeletingOnePlacementRetainsOtherGraphAndItsUnresolvedOriginalDependency()
    {
        var f = new GraphFixture(false);
        GraphAttachment other = null!;
        f.Document.Edit("Other graph", edit =>
        {
            other = edit.CreateItemGraph(f.OtherPlacement);
            var source = edit.AddGraphNode(other.GraphId, GraphBuiltIns.Source, new(0, 0));
            edit.BindGraphSource(other.Id, source.Id, f.OtherPlacement, f.FirstPart);
        });
        var json = GraphFixture.Json(f.Document);
        json["state"]!["graphAttachments"]![1]!["sources"]![0]!["placementId"] = f.Placement.ToString();
        var loaded = GraphFixture.Read(json).Document;
        loaded.Edit("Delete owned placement", edit => edit.DeletePlacement(f.Placement));
        Assert.Equal(other.GraphId, loaded.Current.State.Graphs.Single().Id);
        Assert.Equal(f.Placement, loaded.Current.State.GraphAttachments.Single().Sources.Single().PlacementId);
        Assert.Contains(GraphDiagnostics.Inspect(loaded.Current.State).Single().Diagnostics, item => item.Reason == GraphReasons.UnresolvedSource);
        Assert.Single(loaded.Current.State.Sounds);
        Assert.Single(loaded.Current.State.Resources);
        var reopened = GraphFixture.Reopen(loaded).Document;
        Assert.Equal(f.Placement, reopened.Current.State.GraphAttachments.Single().Sources.Single().PlacementId);
        loaded.Undo();
        Assert.Equal(2, loaded.Current.State.Graphs.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void BuiltInMixCreationEnforcesBoundedExecutableSlotShape(int slots)
    {
        var f = new GraphFixture(false);
        var before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Unsupported Mix shape", edit =>
            edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Mix, new(0, 0), mixInputs: slots)));
        Assert.Same(before, f.Document.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OneEditCreatesOwnedContextAndGraphWithSeparatePlacementPartSources(bool twoParts)
    {
        var f = new GraphFixture(twoParts);
        Assert.Equal(2, f.Document.UndoCount);
        Assert.True(f.Report.IsEligibleForPreparation);
        Assert.Equal(f.Attachment.ContextId, f.Document.Current.State.Placements[0].ItemContextId);
        Assert.Null(f.Document.Current.State.Placements[1].ItemContextId);
        Assert.Equal(twoParts ? 2 : 1, f.CurrentAttachment.Sources.Length);
        Assert.All(f.CurrentAttachment.Sources, binding =>
        {
            Assert.Equal(f.Placement, binding.PlacementId);
            Assert.Equal(GraphBuiltIns.PreItem, binding.Boundary);
        });
        Assert.Single(f.Document.Current.State.Sounds);
        Assert.Single(f.Document.Current.State.Resources);
        var before = f.Document.Current;
        f.Document.Undo();
        Assert.Empty(f.Document.Current.State.Graphs);
        Assert.Empty(f.Document.Current.State.Contexts);
        Assert.Null(f.Document.Current.State.Placements[0].ItemContextId);
        f.Document.Redo();
        Assert.Same(before, f.Document.Current);
    }

    [Fact]
    public void TwoPlacementsSharingPatternHaveIndependentGraphBindings()
    {
        var f = new GraphFixture();
        GraphAttachment other = null!;
        f.Document.Edit("Other graph", edit =>
        {
            other = edit.CreateItemGraph(f.OtherPlacement, true);
            var node = edit.AddGraphNode(other.GraphId, GraphBuiltIns.Source, new(0, 0));
            edit.BindGraphSource(other.Id, node.Id, f.OtherPlacement, f.FirstPart);
        });
        Assert.NotEqual(f.Attachment.ContextId, other.ContextId);
        Assert.NotEqual(f.Attachment.GraphId, other.GraphId);
        var binding = f.Document.Current.State.GraphAttachments.Single(item => item.Id == other.Id).Sources.Single();
        Assert.Equal(f.FirstPart, binding.PartId);
        Assert.Equal(f.OtherPlacement, binding.PlacementId);
        Assert.DoesNotContain(f.CurrentAttachment.Sources, item => item.Id == binding.Id || item.NodeId == binding.NodeId);
        Assert.True(f.Report.IsEligibleForPreparation);
    }

    [Theory]
    [InlineData("duplicate-node")]
    [InlineData("duplicate-port")]
    [InlineData("cross-kind-id")]
    [InlineData("wrong-graph")]
    [InlineData("wrong-node")]
    [InlineData("wrong-port")]
    [InlineData("wrong-placement")]
    [InlineData("wrong-part")]
    [InlineData("duplicate-attachment")]
    [InlineData("duplicate-binding")]
    [InlineData("occupied-input")]
    [InlineData("incompatible-layout")]
    [InlineData("wrong-gain-type")]
    [InlineData("invalid-position")]
    public void FailedGraphEditsAreAtomicAndDoNotMutateHistory(string failure)
    {
        var f = new GraphFixture();
        var before = f.Document.Current;
        var generation = f.Document.Generation;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Rejected", edit =>
        {
            edit.RenameDocument("Must roll back");
            switch (failure)
            {
                case "duplicate-node": edit.AddGraphNode(f.Attachment.GraphId, f.Gain); break;
                case "duplicate-port": edit.AddGraphNode(f.Attachment.GraphId, f.Gain with { Id = Id<GraphNode>.New() }); break;
                case "cross-kind-id": edit.AddGraphNode(f.Attachment.GraphId, f.Gain with
                { Id = new Id<GraphNode>(f.Sound.Value), Ports = [.. f.Gain.Ports.Select(p => p with { Id = Id<GraphPort>.New() })] }); break;
                case "wrong-graph": edit.MoveGraphNode(Id<GraphDefinition>.New(), f.Gain.Id, new(0, 0)); break;
                case "wrong-node": edit.SetGraphGain(f.Attachment.GraphId, Id<GraphNode>.New(), 0.5m); break;
                case "wrong-port": edit.ConnectGraphPorts(f.Attachment.GraphId, f.Source.Id, f.Gain.Ports[1].Id, f.Gain.Id, f.Gain.Ports[0].Id); break;
                case "wrong-placement": edit.BindGraphSource(f.Attachment.Id, f.Source.Id, f.OtherPlacement, f.FirstPart); break;
                case "wrong-part": edit.BindGraphSource(f.Attachment.Id, f.Source.Id, f.Placement, Id<MusicalPart>.New()); break;
                case "duplicate-attachment": edit.CreateItemGraph(f.Placement); break;
                case "duplicate-binding": edit.BindGraphSource(f.Attachment.Id, f.Source.Id, f.Placement, f.FirstPart); break;
                case "occupied-input": GraphFixture.Connect(edit, f.Attachment.GraphId, f.OtherSource, f.Gain); break;
                case "incompatible-layout":
                    var mono = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Gain, new(0, 0), GraphBuiltIns.Mono);
                    GraphFixture.Connect(edit, f.Attachment.GraphId, f.Source, mono); break;
                case "wrong-gain-type": edit.SetGraphGain(f.Attachment.GraphId, f.Source.Id, 0.5m); break;
                case "invalid-position": edit.MoveGraphNode(f.Attachment.GraphId, f.Gain.Id, new(double.NaN, 0)); break;
                default: throw new InvalidOperationException();
            }
        }));
        Assert.Same(before, f.Document.Current);
        Assert.Equal(generation, f.Document.Generation);
        Assert.Equal(2, f.Document.UndoCount);
    }

    [Fact]
    public void FanOutUsesOneBindingAndMixHasStableIndividuallyIdentifiedSlots()
    {
        var f = new GraphFixture(false);
        f.Document.Edit("Explicit branches converge", edit =>
        {
            var branch = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Gain, new(10, 20));
            var mix = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Mix, new(20, 40));
            var oldOutputEdge = f.Graph.Connections.Single(edge => edge.ToNodeId == f.Output.Id);
            edit.DisconnectGraphPorts(f.Attachment.GraphId, oldOutputEdge.Id);
            GraphFixture.Connect(edit, f.Attachment.GraphId, f.Source, branch);
            GraphFixture.Connect(edit, f.Attachment.GraphId, f.Gain, mix, 0);
            GraphFixture.Connect(edit, f.Attachment.GraphId, branch, mix, 1);
            GraphFixture.Connect(edit, f.Attachment.GraphId, mix, f.Output);
            edit.SetContextMix(f.Attachment.ContextId, true);
        });
        Assert.True(f.Report.IsEligibleForPreparation);
        Assert.Single(f.CurrentAttachment.Sources);
        Assert.Equal(2, f.Graph.Connections.Count(edge => edge.FromNodeId == f.Source.Id));
        var slots = f.Graph.Nodes.Single(node => node.Type == GraphBuiltIns.Mix).Ports.Where(port => port.Direction == GraphPortDirection.Input).ToArray();
        Assert.NotEqual(slots[0].Id, slots[1].Id);
        Assert.All(slots, port => Assert.Equal(GraphPortCardinality.Single, port.Cardinality));
    }

    [Fact]
    public void VariationRemapsOnlyIdentifiedPlacementBindingsAndUndoRestoresAllIdentities()
    {
        var f = new GraphFixture();
        var before = f.Document.Current;
        Id<Pattern> variation = default;
        f.Document.Edit("Variation", edit => variation = edit.MakePatternVariation(f.Placement, "Variation"));
        var newParts = f.Document.Current.State.Patterns.Single(item => item.Id == variation).Parts;
        Assert.DoesNotContain(newParts, part => part.Id == f.FirstPart || part.Id == f.SecondPart);
        Assert.Equal(newParts.Select(part => part.Id), f.CurrentAttachment.Sources.Select(binding => binding.PartId));
        Assert.Equal(before.State.GraphAttachments[0].Sources.Select(binding => binding.Id), f.CurrentAttachment.Sources.Select(binding => binding.Id));
        Assert.Equal(f.Pattern, f.Document.Current.State.Placements[1].PatternId);
        Assert.True(f.Report.IsEligibleForPreparation);
        f.Document.Undo();
        Assert.Same(before, f.Document.Current);
        f.Document.Redo();
        Assert.True(f.Report.IsEligibleForPreparation);
    }

    [Theory]
    [InlineData("placement")]
    [InlineData("context")]
    [InlineData("graph")]
    [InlineData("source-node")]
    [InlineData("gain-node")]
    [InlineData("source-binding")]
    public void DeliberateDeletionRemovesOwnedGraphUsesAndPreservesReusableResources(string target)
    {
        var f = new GraphFixture();
        var before = f.Document.Current;
        f.Document.Edit("Delete", edit =>
        {
            switch (target)
            {
                case "placement": edit.DeletePlacement(f.Placement); break;
                case "context":
                    edit.SetPlacementRelationships(f.Placement, null, null, null, []);
                    edit.DeleteContext(f.Attachment.ContextId); break;
                case "graph": edit.DeleteGraph(f.Attachment.GraphId); break;
                case "source-node": edit.DeleteGraphNode(f.Attachment.GraphId, f.Source.Id); break;
                case "gain-node": edit.DeleteGraphNode(f.Attachment.GraphId, f.Gain.Id); break;
                case "source-binding": edit.DeleteGraphSource(f.Attachment.Id, f.CurrentAttachment.Sources[0].Id); break;
                default: throw new InvalidOperationException();
            }
        });
        Assert.Equal(f.Sound, f.Document.Current.State.Sounds.Single().Id);
        Assert.Equal(f.Resource, f.Document.Current.State.Resources.Single().Id);
        Assert.Contains(f.Document.Current.State.Placements, item => item.Id == f.OtherPlacement);
        if (target is "placement" or "context" or "graph")
        {
            Assert.Empty(f.Document.Current.State.Graphs);
            Assert.Empty(f.Document.Current.State.GraphAttachments);
        }
        else
        {
            Assert.False(f.Report.IsEligibleForPreparation);
            if (target.EndsWith("node", StringComparison.Ordinal))
                Assert.DoesNotContain(f.Graph.Connections, edge => edge.FromNodeId == (target == "source-node" ? f.Source.Id : f.Gain.Id) ||
                    edge.ToNodeId == (target == "source-node" ? f.Source.Id : f.Gain.Id));
        }
        f.Document.Undo();
        Assert.Same(before, f.Document.Current);
        Assert.True(f.Report.IsEligibleForPreparation);
        f.Document.Redo();
        ProjectValidation.Validate(f.Document.Current.State);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("pattern")]
    [InlineData("sound")]
    public void ReferencedMusicalOwnersStillRequireExplicitDetachment(string target)
    {
        var f = new GraphFixture();
        var before = f.Document.Current;
        Assert.Throws<ProjectValidationException>(() => f.Document.Edit("Rejected", edit =>
        {
            if (target == "context") edit.DeleteContext(f.Attachment.ContextId);
            else if (target == "pattern") edit.DeletePattern(f.Pattern);
            else edit.DeleteSound(f.Sound);
        }));
        Assert.Same(before, f.Document.Current);
    }

    [Fact]
    public void ParametersCoordinatesAndTopologyHaveOneUndoStepAndNetZeroPreservesRedo()
    {
        var f = new GraphFixture();
        var before = f.Document.Current;
        f.Document.Edit("Three graph changes", edit =>
        {
            edit.MoveGraphNode(f.Attachment.GraphId, f.Gain.Id, new(75, -20));
            edit.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, 0.25m);
            edit.DisconnectGraphPorts(f.Attachment.GraphId, f.SourceEdge);
        });
        var after = f.Document.Current;
        Assert.Equal(3, f.Document.UndoCount);
        f.Document.Undo();
        Assert.Same(before, f.Document.Current);
        var generation = f.Document.Generation;
        Assert.False(f.Document.Edit("Net zero", edit =>
        {
            edit.MoveGraphNode(f.Attachment.GraphId, f.Gain.Id, new(42, 50));
            edit.MoveGraphNode(f.Attachment.GraphId, f.Gain.Id, f.Gain.Position);
            edit.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, 0.2m);
            edit.SetGraphGain(f.Attachment.GraphId, f.Gain.Id, 1m);
            var temporary = edit.AddGraphNode(f.Attachment.GraphId, GraphBuiltIns.Gain, new(0, 0));
            edit.DeleteGraphNode(f.Attachment.GraphId, temporary.Id);
        }));
        Assert.Same(before, f.Document.Current);
        Assert.Equal(generation, f.Document.Generation);
        Assert.Equal(1, f.Document.RedoCount);
        f.Document.Redo();
        Assert.Same(after, f.Document.Current);
    }

    [Fact]
    public void RecreatedDeepGraphValuesAreEqualAndOpaqueDataIsRetained()
    {
        var f = new GraphFixture();
        var opaque = GraphBuiltIns.Create(GraphBuiltIns.Gain, new(0, 0)) with
        {
            Type = "org.example.unknown",
            Parameters = ImmutableDictionary<string, JsonElement>.Empty.Add("foreign", JsonSerializer.SerializeToElement(new { value = 1 })),
            Extension = new("org.example.provider", 4, JsonSerializer.SerializeToElement(new { bytes = "keep" })),
            AdditionalData = ImmutableDictionary<string, JsonElement>.Empty.Add("future", JsonSerializer.SerializeToElement(true))
        };
        f.Document.Edit("Opaque node", edit => edit.AddGraphNode(f.Attachment.GraphId, opaque));
        var before = f.Document.Current;
        Assert.False(f.Document.Edit("Reconstruct equal values", edit =>
        {
                edit.DeleteGraphNode(f.Attachment.GraphId, opaque.Id);
                edit.AddGraphNode(f.Attachment.GraphId, opaque with
                {
                    Parameters = opaque.Parameters.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.Clone()),
                    Ports = [.. opaque.Ports.Select(port => port with { AdditionalData = port.AdditionalData })],
                    Position = opaque.Position with { AdditionalData = opaque.Position.AdditionalData },
                    Extension = opaque.Extension! with { Payload = opaque.Extension.Payload.Clone() },
                    AdditionalData = opaque.AdditionalData
                });
        }));
        Assert.Same(before, f.Document.Current);
    }
}
