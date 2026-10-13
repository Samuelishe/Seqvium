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
            sampler = new(facts.Rate, facts.Channels, facts.CapacityFrames);
            async Task Publish(bool live)
            {
                using var request = sampler.BeginArrangementPreparation(document, live);
                await request.PrepareAsync();
                if (request.Publish() != SamplerPublication.Accepted) throw new InvalidOperationException("Song publication refused.");
            }
            await Publish(false); sampler.Start(); output.Start(sampler);
            long epoch = 0, beforeEdit = 0; bool edited = false, converged = false, overlap = false;
            var timeout = Stopwatch.StartNew();
            while (timeout.Elapsed < TimeSpan.FromSeconds(9) && output.Failure == OutputFailure.None)
            {
                sampler.RetireCompleted(); var status = sampler.ReadStatus();
                if (epoch == 0 && status.Playing) epoch = status.Epoch;
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
                overlap && converged && final.Epoch == epoch && final.Position == facts.Rate * 6L && final.Position > beforeEdit &&
                sampler.StopAcknowledgment >= stop && output.Joined && output.ProcessorAllocatedBytes == 0 &&
                output.NonFiniteSamples == 0 && output.ProcessorMisses == 0 && output.PacketMisses == 0;
            Console.WriteLine(JsonSerializer.Serialize(new { Mode = "song-smoke", Passed = passed, facts, final, SimultaneousVoices = overlap,
                LiveEditConverged = converged, SameEpoch = final.Epoch == epoch, output.ElapsedSeconds, output.Failure,
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
