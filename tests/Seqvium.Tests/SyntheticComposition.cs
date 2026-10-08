// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Tests;

internal sealed class SyntheticComposition
{
    public ProjectDocument Document { get; } = ProjectDocument.Create();
    public Id<ResourceDescriptor> Resource { get; private set; }
    public Id<SoundDefinition> Kick { get; private set; }
    public Id<SoundDefinition> Bass { get; private set; }
    public Id<Pattern> Pattern { get; private set; }
    public Id<MusicalPart> KickPart { get; private set; }
    public Id<MusicalPart> BassPart { get; private set; }
    public Id<NoteEvent> KickNote { get; private set; }
    public Id<InstrumentGroup> Group { get; private set; }
    public Id<PatternPlacement>[] Placements { get; } = new Id<PatternPlacement>[3];
    public Id<ProcessingContext> ItemContext { get; private set; }
    public Id<ProcessingContext> ContainerContext { get; private set; }
    public Id<RouteIntent> Route { get; private set; }
    public static MusicalDuration Quarter => new(MusicalPosition.TicksPerQuarter);

    public SyntheticComposition()
    {
        Document.Edit("Create synthetic composition", edit =>
        {
            Resource = edit.AddResource("Synthetic resource", "media/synthetic.wav", "synthetic fixture; no audio bytes");
            Kick = edit.AddSound("Kick", "core.future-sampler", [Resource]);
            Bass = edit.AddSound("Bass", "core.future-sampler", [Resource]);
            Group = edit.AddGroup("Rhythm", Kick, Bass);
            Pattern = edit.AddPattern("Full Groove A", new(4 * MusicalPosition.TicksPerQuarter));
            KickPart = edit.AddPart(Pattern, "Kick", Kick);
            BassPart = edit.AddPart(Pattern, "Bass", Bass);
            KickNote = edit.AddNote(Pattern, KickPart, new(0), Quarter, 36m, 0.8m);
            edit.AddNote(Pattern, BassPart, new(MusicalPosition.TicksPerQuarter), Quarter, 48.5m, 0.6m);
            for (int index = 0; index < 3; index++)
                Placements[index] = edit.AddPlacement(Pattern, new(index * 4 * MusicalPosition.TicksPerQuarter));
            Route = edit.AddRoute("Independent route");
            ItemContext = edit.AddContext("B local processing", LocalProcessingLevel.Item);
            ContainerContext = edit.AddContext("Retained container paths", LocalProcessingLevel.Containing);
            edit.SetPlacementRelationships(Placements[1], ItemContext, ContainerContext, null,
                [new(KickPart, null, Route), new(BassPart, null, null)]);
        });
    }

    public Pattern SharedPattern => Document.Current.State.Patterns.Single(pattern => pattern.Id == Pattern);
    public PatternPlacement Placement(int index) => Document.Current.State.Placements.Single(item => item.Id == Placements[index]);
}

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "seqvium-r1-tests", Guid.NewGuid().ToString("N"));
    public TemporaryDirectory() => Directory.CreateDirectory(Path);
    public string File(string name) => System.IO.Path.Combine(Path, name);
    public void Dispose() => Directory.Delete(Path, recursive: true);
}
