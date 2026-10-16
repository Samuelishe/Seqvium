// SPDX-License-Identifier: Apache-2.0

using System.Runtime.Versioning;
using System.Text.Json;
using Seqvium.Audio.Windows;
using Seqvium.Core;

[SupportedOSPlatform("windows")]
internal static class DrumPlayAlongCheck
{
    internal static async Task<int> Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var levels = new List<object>();
        foreach (int rate in new[] { 44100, 48000 })
        foreach (var instrument in DrumInstruments.All)
        foreach (var preset in instrument.Presets)
        {
            var sound = new SoundDefinition(default, instrument.Key, instrument.Algorithm, preset.Parameters, [], null);
            using var pcm = DrumVoice.Render(sound, rate);
            double peak = 0, energy = 0;
            for (int i = 0; i < pcm.Frames; i++)
            {
                double sample = pcm.Sample(i, 0) * .8f;
                peak = Math.Max(peak, Math.Abs(sample));
                energy += sample * sample;
            }

            levels.Add(new
            {
                rate, instrument.Key, preset.Name, Velocity = .8, pcm.Frames, Peak = peak,
                Rms = Math.Sqrt(energy / pcm.Frames), OldDrumPreviewPeak = peak * .12,
                UnityPatternPeak = peak, DefaultPatternGainPeak = peak * .25
            });
        }

        var synthLevels = new List<object>();
        foreach (int rate in new[] { 44100, 48000 })
        {
            var sound = new SoundDefinition(default, "Synth", SynthVoice.Algorithm,
                SynthVoice.Presets[0].Values, [], null);
            using var scheduledPlan = SamplerPreparation.PrepareSynthAudition(sound, rate, 1, 60, .8m);
            using var heldPlan = SamplerPreparation.PrepareSynthAudition(sound, rate, 1, 60, .8m, held: true);
            using var scheduled = scheduledPlan.CreateExecution();
            using var held = heldPlan.CreateExecution();
            var a = new float[256];
            var b = new float[256];
            double peak = 0, heldPeak = 0, energy = 0, heldEnergy = 0;
            int measured = 0;
            for (int frame = 0; frame < rate; frame += 256)
            {
                int count = Math.Min(256, rate - frame);
                scheduled.Process(a.AsSpan(0, count));
                held.Process(b.AsSpan(0, count));
                for (int i = 0; i < count; i++)
                {
                    peak = Math.Max(peak, Math.Abs(a[i]));
                    heldPeak = Math.Max(heldPeak, Math.Abs(b[i]));
                    if (frame + i < rate * 3 / 4) continue;
                    energy += a[i] * a[i];
                    heldEnergy += b[i] * b[i];
                    measured++;
                }
            }

            synthLevels.Add(new
            {
                rate, Name = "WarmBass", Pitch = 60, Velocity = .8,
                ScheduledPeak = peak, HeldPeak = heldPeak, ScheduledSustainRms = Math.Sqrt(energy / measured),
                HeldSustainRms = Math.Sqrt(heldEnergy / measured), SustainRatio = Math.Sqrt(heldEnergy / energy),
                PreviousPreviewGain = .7
            });
        }

        File.WriteAllText(Path.Combine(directory, "synth-levels.json"), JsonSerializer.Serialize(synthLevels,
            new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(directory, "levels.json"), JsonSerializer.Serialize(levels,
            new JsonSerializerOptions { WriteIndented = true }));
        var document = ProjectDocument.Create();
        Id<GraphAttachment> attachment = default;
        document.Edit("Isolated physical check", edit =>
        {
            var sound = edit.AddSound("Kick", DrumVoice.Algorithm);
            edit.SetSoundParameters(sound, DrumVoice.Defaults);
            var pattern = edit.AddPattern("Check", new(MusicalPosition.TicksPerQuarter * 4));
            var part = edit.AddPart(pattern, "Kick", sound);
            for (int i = 0; i < 4; i++)
                edit.AddNote(pattern, part,
                    new(i * MusicalPosition.TicksPerQuarter), new(1000), 36, .8m);
            var placement = edit.AddPlacement(pattern, new(0));
            var graph = edit.CreateItemGraph(placement);
            attachment = graph.Id;
            var source = edit.AddGraphNode(graph.GraphId, GraphBuiltIns.Source, new(0, 0));
            var gain = edit.AddGraphNode(graph.GraphId, GraphBuiltIns.Gain, new(100, 0));
            var outputNode = edit.AddGraphNode(graph.GraphId, GraphBuiltIns.Output, new(200, 0));
            edit.BindGraphSource(graph.Id, source.Id, placement, part);
            edit.SetGraphGain(graph.GraphId, gain.Id, .25m);
            edit.ConnectGraphPorts(graph.GraphId, source.Id, source.Ports.Single().Id, gain.Id,
                gain.Ports.Single(p => p.Direction == GraphPortDirection.Input).Id);
            edit.ConnectGraphPorts(graph.GraphId, gain.Id,
                gain.Ports.Single(p => p.Direction == GraphPortDirection.Output).Id, outputNode.Id,
                outputNode.Ports.Single().Id);
            edit.SetGraphOutput(graph.Id, outputNode.Id);
        });
        var kick = document.Current.State.Sounds.Single();
        using var output = new WasapiOutput(48000 * 2 * 4, diagnostics: true);
        if (output.Facts is not { } facts || output.Failure != OutputFailure.None)
        {
            output.Close();
            document.Close();
            Console.WriteLine(JsonSerializer.Serialize(new { Accepted = false, output.Failure, output.HResult }));
            return 1;
        }

        using var sampler = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames,
            smoothBoundaries: true);
        using var mirror = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames,
            smoothBoundaries: true);
        var preview = PreparedDrumAudition.Prepare(kick, facts.Rate, 36);
        foreach (var processor in new[] { sampler, mirror })
        {
            using var request = processor.BeginGraphPreparation(document, attachment);
            await request.PrepareAsync();
            if (request.Publish() != SamplerPublication.Accepted)
                throw new InvalidOperationException("Publication refused.");
            processor.SetLooping(true);
            processor.Start();
            // Bounded simultaneous low-velocity overlap; no system volume adjustment.
            for (int i = 0; i < 8; i++) processor.TriggerDrumAudition(preview, .1m);
        }

        long position;
        try
        {
            output.Start(sampler);
            await Task.Delay(2300);
            position = sampler.RenderPosition;
        }
        finally
        {
            output.Close();
        }

        mirror.Process(Span<float>.Empty); // The physical adapter's declared warm boundary.
        var block = new float[facts.CapacityFrames * facts.Channels];
        int offset = 0;
        double error = 0, peakCapture = 0;
        foreach (int frames in output.CapturePartitions)
        {
            mirror.Process(block.AsSpan(0, frames * facts.Channels));
            for (int sample = 0; sample < frames * facts.Channels; sample++)
            {
                error = Math.Max(error, Math.Abs(block[sample] - output.Capture[offset + sample]));
                peakCapture = Math.Max(peakCapture, Math.Abs(output.Capture[offset + sample]));
            }

            offset += frames * facts.Channels;
        }

        sampler.TerminateExecution();
        mirror.TerminateExecution();
        bool accepted = output.Failure == OutputFailure.None && output.Joined && position > facts.Rate * 2 &&
                        error == 0 && peakCapture is > 0 and < 1 && output.NonFiniteSamples == 0 &&
                        output.ProcessorAllocatedBytes == 0 && output.ProcessorMisses == 0 && output.PacketMisses == 0;
        var receipt = new
        {
            Accepted = accepted, facts, output.Failure, output.HResult, output.Joined,
            CaptureFrames = offset / facts.Channels, Peak = peakCapture, MirrorError = error,
            sampler.DrumAuditionHits, sampler.DrumAuditionMinimumGain, Position = position,
            output.ProcessorAllocatedBytes, output.ServiceAllocatedBytes, output.ProcessorMisses,
            output.ServiceMisses, output.PacketMisses, output.PaddingExhaustions, output.NonFiniteSamples,
            SubjectiveListening = false
        };
        File.WriteAllText(Path.Combine(directory, "physical.json"), JsonSerializer.Serialize(receipt,
            new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(receipt));
        document.Close();
        return accepted ? 0 : 1;
    }
}
