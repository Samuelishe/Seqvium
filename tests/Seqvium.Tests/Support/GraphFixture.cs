// SPDX-License-Identifier: Apache-2.0
using System.Text;
using System.Text.Json.Nodes;
using Seqvium.Core;

namespace Seqvium.Tests;

internal sealed class GraphFixture
{
    public ProjectDocument Document { get; } = ProjectDocument.Create();
    public Id<Pattern> Pattern { get; private set; }
    public Id<PatternPlacement> Placement { get; private set; }
    public Id<PatternPlacement> OtherPlacement { get; private set; }
    public Id<MusicalPart> FirstPart { get; private set; }
    public Id<MusicalPart> SecondPart { get; private set; }
    public Id<SoundDefinition> Sound { get; private set; }
    public Id<ResourceDescriptor> Resource { get; private set; }
    public GraphAttachment Attachment { get; private set; } = null!;
    public GraphNode Source { get; private set; } = null!;
    public GraphNode OtherSource { get; private set; } = null!;
    public GraphNode Gain { get; private set; } = null!;
    public GraphNode Mix { get; private set; } = null!;
    public GraphNode Output { get; private set; } = null!;
    public Id<GraphConnection> SourceEdge { get; private set; }
    public GraphDefinition Graph => Document.Current.State.Graphs.Single(item => item.Id == Attachment.GraphId);
    public GraphAttachment CurrentAttachment => Document.Current.State.GraphAttachments.Single(item => item.Id == Attachment.Id);
    public GraphIntentReport Report => GraphDiagnostics.Inspect(Document.Current.State).Single(item => item.GraphId == Attachment.GraphId);

    public GraphFixture(bool twoParts = true)
    {
        Document.Edit("Music", edit =>
        {
            Resource = edit.AddResource("Shared PCM");
            Sound = edit.AddSound("Same sound", PcmSampler.Algorithm, [Resource]);
            edit.ConfigurePcmSampler(Sound, Resource, 60m, 0m);
            Pattern = edit.AddPattern("Pattern", new(MusicalPosition.TicksPerQuarter));
            FirstPart = edit.AddPart(Pattern, "Same name", Sound);
            edit.AddNote(Pattern, FirstPart, new(0), new(MusicalPosition.TicksPerQuarter), 60m, 1m);
            // Silent parts are deliberately required sources too.
            if (twoParts) SecondPart = edit.AddPart(Pattern, "Same name", Sound);
            Placement = edit.AddPlacement(Pattern, new(0));
            OtherPlacement = edit.AddPlacement(Pattern, new(MusicalPosition.TicksPerQuarter));
        });
        Document.Edit("One logical graph", edit =>
        {
            Attachment = edit.CreateItemGraph(Placement, twoParts);
            Source = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Source, new(500, 200));
            Gain = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Gain, new(-50, 20));
            Output = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Output, new(-300, 400));
            edit.BindGraphSource(Attachment.Id, Source.Id, Placement, FirstPart);
            SourceEdge = Connect(edit, Attachment.GraphId, Source, Gain);
            if (twoParts)
            {
                OtherSource = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Source, new(0, 800));
                Mix = edit.AddGraphNode(Attachment.GraphId, GraphBuiltIns.Mix, new(-500, 800));
                edit.BindGraphSource(Attachment.Id, OtherSource.Id, Placement, SecondPart);
                Connect(edit, Attachment.GraphId, Gain, Mix, 0);
                Connect(edit, Attachment.GraphId, OtherSource, Mix, 1);
                Connect(edit, Attachment.GraphId, Mix, Output);
            }
            else Connect(edit, Attachment.GraphId, Gain, Output);
            edit.SetGraphOutput(Attachment.Id, Output.Id);
        });
    }

    public static Id<GraphConnection> Connect(ProjectEdit edit, Id<GraphDefinition> graph, GraphNode from,
        GraphNode to, int slot = 0) => edit.ConnectGraphPorts(graph, from.Id,
        from.Ports.Single(port => port.Direction == GraphPortDirection.Output).Id, to.Id,
        to.Ports.Where(port => port.Direction == GraphPortDirection.Input).ElementAt(slot).Id);
    public static JsonObject Json(ProjectDocument document) => JsonNode.Parse(ProjectPersistence.Encode(document))!.AsObject();
    public static ProjectLoadResult Read(JsonNode json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json.ToJsonString()));
        return ProjectPersistence.Read(stream);
    }
    public static ProjectLoadResult Reopen(ProjectDocument document) => Read(Json(document));
}
