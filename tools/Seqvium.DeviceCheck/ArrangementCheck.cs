// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Seqvium.Audio.Windows;
using Seqvium.Core;

[SupportedOSPlatform("windows")]
internal static class ArrangementCheck
{
    internal static async Task<int> Run()
    {
        Console.Error.WriteLine("Song smoke plays two quiet authored sine Patterns for six seconds; system volume is unchanged.");
        string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "Seqvium-song-" + Guid.NewGuid().ToString("N")));
        var document = ProjectDocument.Create();
        RealtimeSampler? sampler = null;
        using var output = new WasapiOutput(192000, diagnostics: true);
        try
        {
            if (output.Failure != OutputFailure.None || output.Facts is not { } facts)
            { Console.Error.WriteLine(JsonSerializer.Serialize(new { output.Failure, output.HResult })); return 1; }
            var placements = new List<Id<PatternPlacement>>();
            for (int tone = 0; tone < 2; tone++)
            {
                using var bytes = new MemoryStream();
                using (var writer = new BinaryWriter(bytes, System.Text.Encoding.ASCII, true))
                {
                    writer.Write("RIFF"u8); writer.Write(36 + 24000 * 4); writer.Write("WAVEfmt "u8);
                    writer.Write(16); writer.Write((ushort)3); writer.Write((ushort)1); writer.Write(48000); writer.Write(192000);
                    writer.Write((ushort)4); writer.Write((ushort)32); writer.Write("data"u8); writer.Write(24000 * 4);
                    for (int i = 0; i < 24000; i++) writer.Write((float)(.02 * Math.Sin(i * 2 * Math.PI * (tone == 0 ? 220 : 330) / 48000)));
                }
                bytes.Position = 0;
                using var import = ProjectMedia.BeginImport(document, "Quiet tone " + tone, releaseMilliseconds: 0, ownedMediaDirectory: root);
                await import.PrepareAsync(bytes);
                import.Accept((edit, media) =>
                {
                    var pattern = edit.AddPattern("Tone " + tone, new(960000));
                    var part = edit.AddPart(pattern, "Sine", media.SoundId);
                    edit.AddNote(pattern, part, default, new(960000), 60, 1);
                    var placement = edit.AddPlacement(pattern, default); placements.Add(placement);
                    var attachment = edit.CreateItemGraph(placement);
                    string layout = facts.Channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
                    var source = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Source, new(0, 0), layout);
                    var gain = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Gain, new(180, 0), layout);
                    var sink = edit.AddGraphNode(attachment.GraphId, GraphBuiltIns.Output, new(360, 0), layout);
                    edit.SetGraphGain(attachment.GraphId, gain.Id, .25m);
                    edit.BindGraphSource(attachment.Id, source.Id, placement, part);
                    edit.ConnectGraphPorts(attachment.GraphId, source.Id, source.Ports.Single().Id, gain.Id, gain.Ports.Single(p => p.Direction == GraphPortDirection.Input).Id);
                    edit.ConnectGraphPorts(attachment.GraphId, gain.Id, gain.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id, sink.Id, sink.Ports.Single().Id);
                    edit.SetGraphOutput(attachment.Id, sink.Id);
                });
            }
            document.Edit("Song", edit => { edit.AddArrangementClip(placements[0], default, 0, 12); edit.AddArrangementClip(placements[1], new(960000), 1, 8); });
            sampler = new(facts.Rate, facts.Channels, facts.CapacityFrames, smoothBoundaries: true);
            async Task Publish(bool live)
            {
                using var request = sampler.BeginArrangementPreparation(document, live);
                await request.PrepareAsync();
                if (request.Publish() != SamplerPublication.Accepted) throw new InvalidOperationException("Song publication refused.");
            }
            await Publish(false); sampler.Start(); output.Start(sampler);
            long epoch = 0, beforeEdit = 0, previousPosition = 0, seekEpoch = 0;
            bool edited = false, converged = false, overlap = false, sought = false, seekObserved = false, looped = false, looping = false;
            int channelChanges = 0, channelConvergences = 0;
            Id<MusicalPart> addedPart = default; Id<GraphNode> addedSource = default, addedMix = default;
            Guid lastChannelRevision = Guid.Empty;
            var firstPlacement = document.Current.State.Placements.Single(p => p.Id == placements[0]);
            var firstAttachment = document.Current.State.GraphAttachments.Single(a => a.ContextId == firstPlacement.ItemContextId);
            var firstGraph = document.Current.State.Graphs.Single(g => g.Id == firstAttachment.GraphId);
            var originalSource = firstGraph.Nodes.Single(n => n.Type == GraphBuiltIns.Source);
            var originalGain = firstGraph.Nodes.Single(n => n.Type == GraphBuiltIns.Gain);
            var originalCable = firstGraph.Connections.Single(c => c.FromNodeId == originalSource.Id);
            var timeout = Stopwatch.StartNew();
            while (timeout.Elapsed < TimeSpan.FromSeconds(9) && output.Failure == OutputFailure.None)
            {
                sampler.RetireCompleted(); var status = sampler.ReadStatus();
                if (epoch == 0 && status.Playing) epoch = status.Epoch;
                if (!sought && timeout.Elapsed.TotalSeconds >= .5)
                {
                    sampler.SeekSong(facts.Rate / 2); sampler.SetSongLoop(facts.Rate / 2, facts.Rate * 3 / 2, true);
                    sought = true; looping = true; seekEpoch = status.Epoch;
                }
                if (sought && !seekObserved && status.Epoch > seekEpoch) { epoch = status.Epoch; seekObserved = true; }
                if (looping && status.Position < previousPosition) looped = true;
                previousPosition = status.Position;
                if (looping && timeout.Elapsed.TotalSeconds >= 1.8) { sampler.SetSongLoop(0, 0, false); looping = false; }
                if (lastChannelRevision != Guid.Empty && status.ExecutingRevision == lastChannelRevision)
                { channelConvergences++; lastChannelRevision = Guid.Empty; }
                if (edited && status.Position >= facts.Rate * (2.5 + channelChanges * .5) && channelChanges < 4 && lastChannelRevision == Guid.Empty)
                {
                    if (channelChanges == 0)
                        document.Edit("Add empty live channel", edit =>
                        {
                            var pattern = document.Current.State.Patterns.Single(p => p.Id == firstPlacement.PatternId);
                            addedPart = edit.AddPart(pattern.Id, "Live channel", pattern.Parts[0].SoundId);
                            string layout = facts.Channels == 1 ? GraphBuiltIns.Mono : GraphBuiltIns.Stereo;
                            var source = edit.AddGraphNode(firstGraph.Id, GraphBuiltIns.Source, new(0, 120), layout); addedSource = source.Id;
                            var mix = edit.AddGraphNode(firstGraph.Id, GraphBuiltIns.Mix, new(100, 60), layout); addedMix = mix.Id;
                            edit.BindGraphSource(firstAttachment.Id, source.Id, firstPlacement.Id, addedPart);
                            edit.DisconnectGraphPorts(firstGraph.Id, originalCable.Id);
                            var inputs = mix.Ports.Where(p => p.Direction == GraphPortDirection.Input).ToArray();
                            edit.ConnectGraphPorts(firstGraph.Id, originalSource.Id, originalCable.FromPortId, mix.Id, inputs[0].Id);
                            edit.ConnectGraphPorts(firstGraph.Id, source.Id, source.Ports.Single().Id, mix.Id, inputs[1].Id);
                            edit.ConnectGraphPorts(firstGraph.Id, mix.Id, mix.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id, originalGain.Id, originalCable.ToPortId);
                            edit.SetContextMix(firstAttachment.ContextId, true);
                        });
                    else if (channelChanges == 1)
                        document.Edit("Delete live channel", edit =>
                        {
                            edit.DeleteGraphNode(firstGraph.Id, addedMix); edit.DeleteGraphNode(firstGraph.Id, addedSource);
                            edit.ConnectGraphPorts(firstGraph.Id, originalSource.Id, originalCable.FromPortId, originalGain.Id, originalCable.ToPortId);
                            edit.DeletePart(firstPlacement.PatternId, addedPart); edit.SetContextMix(firstAttachment.ContextId, false);
                        });
                    else if (channelChanges == 2) document.Undo();
                    else document.Redo();
                    lastChannelRevision = document.Current.Revision; await Publish(true); channelChanges++;
                }
                if (status.Voices >= 2) overlap = true;
                if (!edited && status.Position >= facts.Rate * 2L)
                {
                    beforeEdit = status.Position;
                    document.Edit("Future song clip", edit => edit.AddArrangementClip(placements[1], new(9600000), 1));
                    await Publish(true); edited = true;
                }
                if (edited && status.ExecutingRevision == document.Current.Revision) converged = true;
                if (status.Transport == SamplerTransportState.Ended) break;
                await Task.Delay(5);
            }
            var final = sampler.ReadStatus();
            long stop = sampler.Stop(); timeout.Restart();
            while (sampler.StopAcknowledgment < stop && timeout.Elapsed < TimeSpan.FromSeconds(1)) await Task.Delay(5);
            output.Close(); sampler.TerminateExecution(); sampler.Dispose();
            bool passed = output.Failure == OutputFailure.None && final.Transport == SamplerTransportState.Ended && !final.Looping &&
                overlap && converged && seekObserved && looped && channelConvergences == 4 && final.Epoch == epoch && final.Position == facts.Rate * 6L && final.Position > beforeEdit &&
                sampler.StopAcknowledgment >= stop && output.Joined && output.ProcessorAllocatedBytes == 0 &&
                output.NonFiniteSamples == 0 && output.ProcessorMisses == 0 && output.PacketMisses == 0;
            Console.WriteLine(JsonSerializer.Serialize(new { Mode = "song-smoke", Passed = passed, facts, final, SimultaneousVoices = overlap,
                LiveEditConverged = converged, SeekObserved = seekObserved, RangeWrapped = looped,
                ChannelChanges = channelChanges, ChannelConvergences = channelConvergences, SameEpoch = final.Epoch == epoch, output.ElapsedSeconds, output.Failure,
                output.ProcessorAllocatedBytes, output.ServiceAllocatedBytes, output.MaximumSampleMagnitude,
                output.ProcessorMisses, output.PacketMisses, output.PaddingExhaustions,
                StopAcknowledged = sampler.StopAcknowledgment >= stop, output.Joined,
                AcousticAssessment = "Short physical output evidence; owner listening and pressure acceptance remain separate." }));
            return passed ? 0 : 1;
        }
        finally
        {
            output.Close(); sampler?.TerminateExecution(); sampler?.Dispose(); document.Close();
            // The fixture directory is uniquely created by this invocation and removed only after confirmed join.
            if (Directory.Exists(root))
            {
                if (!string.Equals(Path.GetDirectoryName(root), Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
                    !Path.GetFileName(root).StartsWith("Seqvium-song-", StringComparison.Ordinal))
                    throw new IOException("Song fixture cleanup escaped its temporary boundary.");
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
