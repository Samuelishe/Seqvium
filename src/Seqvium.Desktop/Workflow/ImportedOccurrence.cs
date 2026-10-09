// SPDX-License-Identifier: Apache-2.0
using Seqvium.Core;

namespace Seqvium.Desktop.Workflow;

internal static class ImportedOccurrence
{
    // One native-pitch note. A frame at the lowest supported execution rate plus one tick protects
    // rounded musical ends against the source's ceiling EOF at both 44.1/48 kHz. EOF ends the sound;
    // the guard does not prolong PCM or change the existing local clip/release policy.
    internal static MusicalDuration Duration(int frames, int rate, Tempo tempo)
        => new(checked((long)decimal.Ceiling(((decimal)frames / rate + 1m / 44100m) *
            MusicalPosition.TicksPerQuarter * tempo.BeatsPerMinute / 60m) + 1));

    internal static GraphAttachment Create(ProjectEdit edit, MediaAcceptance media, string name,
        MusicalDuration duration)
    {
        var pattern = edit.AddPattern(name, duration);
        var part = edit.AddPart(pattern, name, media.SoundId);
        edit.AddNote(pattern, part, default, duration, 60m, 1m);
        var placement = edit.AddPlacement(pattern, default);
        var attachment = edit.CreateItemGraph(placement);
        var source = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0));
        var gain = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Gain, new(180, 0));
        var output = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(360, 0));
        edit.SetGraphGain(attachment.GraphId, gain.Id, 0.25m);
        edit.ConnectGraphPorts(attachment.GraphId, source.Id, source.Ports.Single().Id,
            gain.Id, gain.Ports.Single(port => port.Direction == GraphPortDirection.Input).Id);
        edit.ConnectGraphPorts(attachment.GraphId, gain.Id,
            gain.Ports.Single(port => port.Direction == GraphPortDirection.Output).Id, output.Id, output.Ports.Single().Id);
        edit.BindGraphSource(attachment.Id, source.Id, placement, part);
        edit.SetGraphOutput(attachment.Id, output.Id);
        return attachment;
    }
}
