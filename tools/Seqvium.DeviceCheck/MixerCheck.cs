// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Seqvium.Audio.Windows;
using Seqvium.Core;

/// <summary>Read-only full-project physical output and submitted-float/offline/native-export comparison.
/// This diagnostic creates no project, media or WAV, and does not author the musical result.</summary>
[SupportedOSPlatform("windows")]
internal static class MixerCheck
{
    internal static async Task<int> Run(string project, string wave, string originalWave, string reportDirectory)
    {
        project = Path.GetFullPath(project);
        wave = Path.GetFullPath(wave);
        var before = SHA256.HashData(File.ReadAllBytes(project));
        var loaded = ProjectPersistence.Open(project);
        var document = loaded.Document;
        using var output = new WasapiOutput(captureSamples: 8_000_000, diagnostics: true);
        RealtimeSampler? sampler = null;
        try
        {
            if (!loaded.Diagnostics.IsEmpty || document.Current.State.Mixer is null)
                throw new InvalidOperationException("Mixer/media validation failed.");
            var facts = output.Facts ?? throw new IOException("Physical output unavailable: " + output.Failure);
            if (facts.Channels != 2)
                throw new NotSupportedException("This comparison requires stereo physical output.");
            sampler = new(facts.Rate, facts.Channels, facts.CapacityFrames, smoothBoundaries: true);
            using var request = sampler.BeginArrangementPreparation(document);
            await request.PrepareAsync();
            if (request.Publish() != SamplerPublication.Accepted) throw new IOException("Mixer preparation refused.");
            sampler.Start();
            output.Start(sampler);
            var watch = Stopwatch.StartNew();
            while (sampler.ReadStatus().Transport != SamplerTransportState.Ended &&
                   output.Failure == OutputFailure.None && watch.Elapsed.TotalSeconds < 100)
                await Task.Delay(10);
            var final = sampler.ReadStatus();
            var meters = document.Current.State.Mixer.Channels
                .Select(c => new { c.Name, Meter = sampler.ReadMixerMeter(c.Id) }).ToArray();
            long stop = sampler.Stop();
            while (sampler.StopAcknowledgment < stop && watch.Elapsed.TotalSeconds < 102) await Task.Delay(5);
            output.Close();
            var native = WavDecoder.Decode(File.ReadAllBytes(wave));
            using var original = WavDecoder.Decode(File.ReadAllBytes(originalWave));
            using (native)
            using (var plan = ArrangementPreparation.Prepare(document, facts.Rate, 2, facts.CapacityFrames))
            using (var execution = plan.CreateExecution(smoothBoundaries: true))
            {
                long expected = plan.EndFrame * 2;
                if (output.Capture.Length < expected) throw new IOException("Physical capture is incomplete.");
                var packet = new float[facts.CapacityFrames * 2];
                double floatError = 0, nativeError = 0, difference = 0, peak = 0, lEnergy = 0, rEnergy = 0, cross = 0;
                long clipped = 0;
                int captured = 0;
                foreach (int partition in output.CapturePartitions)
                {
                    int count = (int)Math.Min(partition, plan.EndFrame - execution.Position);
                    if (count <= 0) break;
                    execution.Process(packet.AsSpan(0, count * 2));
                    for (int i = 0; i < count * 2; i++)
                    {
                        int absolute = captured + i;
                        float actual = output.Capture[absolute];
                        floatError = Math.Max(floatError, Math.Abs(actual - packet[i]));
                    }

                    captured += count * 2;
                }

                using var exportPlan = ArrangementPreparation.Prepare(document, 48000, 2, facts.CapacityFrames);
                using var exportExecution = exportPlan.CreateExecution(smoothBoundaries: true);
                if (native.Frames != exportPlan.EndFrame || native.Frames != original.Frames)
                    throw new IOException("Native export duration differs from the original or prepared Song.");
                while (exportExecution.Position < exportPlan.EndFrame)
                {
                    int firstFrame = (int)exportExecution.Position;
                    int count = (int)Math.Min(facts.CapacityFrames, exportPlan.EndFrame - firstFrame);
                    exportExecution.Process(packet.AsSpan(0, count * 2));
                    for (int frame = 0; frame < count; frame++)
                    {
                        double left = native.Sample(firstFrame + frame, 0),
                            right = native.Sample(firstFrame + frame, 1);
                        cross += left * right;
                        for (int channel = 0; channel < 2; channel++)
                        {
                            double mixed = channel == 0 ? left : right;
                            nativeError = Math.Max(nativeError, Math.Abs(packet[frame * 2 + channel] - mixed));
                            difference += Math.Pow(mixed - original.Sample(firstFrame + frame, channel), 2);
                            peak = Math.Max(peak, Math.Abs(mixed));
                            if (Math.Abs(mixed) >= .9999) clipped++;
                            if (channel == 0) lEnergy += mixed * mixed;
                            else rEnergy += mixed * mixed;
                        }
                    }
                }

                bool passed = final.Transport == SamplerTransportState.Ended && final.Position == plan.EndFrame &&
                              output.Failure == OutputFailure.None && output.Joined &&
                              sampler.StopAcknowledgment >= stop &&
                              output.ProcessorAllocatedBytes == 0 && output.ServiceAllocatedBytes == 0 &&
                              output.NonFiniteSamples == 0 &&
                              output.ProcessorMisses == 0 && output.PacketMisses == 0 &&
                              output.PaddingExhaustions == 0 &&
                              captured == expected && native.Frames == original.Frames &&
                              Math.Abs(native.Frames / 48000d - plan.EndFrame / (double)facts.Rate) <=
                              1d / facts.Rate &&
                              floatError <= 1e-7 && nativeError <= 1 / 32768d + 1e-7 && clipped == 0 &&
                              difference > .001 &&
                              before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(project)));
                var report = new
                {
                    Passed = passed, PhysicalOutput = true, SubmittedPcmCapture = true, AcousticListening = false,
                    Project = project, Wave = wave, facts, final, output.ElapsedSeconds, output.Failure, output.Joined,
                    output.ProcessorAllocatedBytes, output.ServiceAllocatedBytes, output.WorkerAllocatedBytes,
                    output.ProcessorMisses, output.ServiceMisses, output.PacketMisses, output.PaddingExhaustions,
                    output.NonFiniteSamples, output.MaximumSampleMagnitude, output.MaximumObservedVoices,
                    FloatMaximumError = floatError, NativeWavMaximumError = nativeError,
                    PhysicalComparisonRate = facts.Rate, ExportComparisonRate = 48000, Frames = native.Frames,
                    Seconds = native.Frames / 48000d, ClippedSamples = clipped, Peak = peak,
                    RmsLeft = Math.Sqrt(lEnergy / native.Frames), RmsRight = Math.Sqrt(rEnergy / native.Frames),
                    Correlation = cross / Math.Sqrt(lEnergy * rEnergy),
                    OriginalDifferenceRms = Math.Sqrt(difference / (native.Frames * 2d)),
                    OriginalPeak = Enumerable.Range(0, original.Frames).Max(i =>
                        Math.Max(Math.Abs(original.Sample(i, 0)), Math.Abs(original.Sample(i, 1)))),
                    OriginalRms = Math.Sqrt(Enumerable.Range(0, original.Frames).Sum(i =>
                                                Math.Pow(original.Sample(i, 0), 2) +
                                                Math.Pow(original.Sample(i, 1), 2)) /
                                            (original.Frames * 2d)),
                    Meters = meters
                };
                Directory.CreateDirectory(reportDirectory);
                string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(reportDirectory, "mixer-physical.json"), json);
                Console.WriteLine(json);
                return passed ? 0 : 1;
            }
        }
        finally
        {
            output.Close();
            sampler?.TerminateExecution();
            sampler?.Dispose();
            document.Close();
        }
    }
}
