// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Seqvium.Audio.Windows;
using Seqvium.Core;

[SupportedOSPlatform("windows")]
internal static class EndpointCheck
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };
    private static void Print(object value) => Console.WriteLine(JsonSerializer.Serialize(value, Json));

    internal static int Enumerate()
    {
        Console.Error.WriteLine(
            "Endpoint snapshot only: no output/capture stream, no sound, no OS preference changes.");
        var snapshot = WindowsAudioEndpoints.Discover();
        Print(new
        {
            Mode = "endpoints", Environment = RuntimeInformation.OSDescription,
            Runtime = RuntimeInformation.FrameworkDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(), SnapshotUtc = DateTimeOffset.UtcNow,
            snapshot.Endpoints, snapshot.Defaults
        });
        foreach (var direction in Enum.GetValues<AudioEndpointDirection>())
        foreach (var role in Enum.GetValues<AudioEndpointRole>())
            Print(snapshot.Resolve(direction, AudioEndpointIntent.Default(role)));
        Print(new { Accepted = true, CaptureOpened = false, RecordingTested = false });
        return 0;
    }

    internal static async Task<int> Session(string endpointId)
    {
        Console.Error.WriteLine(
            "WARNING: endpoint-session explicitly plays quiet authored PCM on the supplied output ID; gain 0.05. No capture or OS setting changes.");
        using var owner = new AudioDeviceSession();
        owner.SelectOutput(AudioEndpointIntent.Explicit(endpointId));
        var document = ProjectDocument.Create();
        var snapshot = WindowsAudioEndpoints.Discover();
        var initialInput = snapshot.Resolve(AudioEndpointDirection.Input, AudioEndpointIntent.Default());
        string inputId = initialInput.Endpoint?.Id ?? "deliberately-missing-input";
        Print(new
        {
            Mode = "endpoint-session", Environment = RuntimeInformation.OSDescription,
            Runtime = RuntimeInformation.FrameworkDescription, OutputId = endpointId, InitialInput = initialInput
        });
        string root = Path.Combine(Path.GetTempPath(), "Seqvium-F4-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "authored.wav");
        try
        {
            using (var stream = File.Create(path))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write("RIFF"u8);
                writer.Write(36 + 44100 * 4);
                writer.Write("WAVEfmt "u8);
                writer.Write(16);
                writer.Write((ushort)3);
                writer.Write((ushort)1);
                writer.Write(44100);
                writer.Write(176400);
                writer.Write((ushort)4);
                writer.Write((ushort)32);
                writer.Write("data"u8);
                writer.Write(44100 * 4);
                for (int i = 0; i < 44100; i++) writer.Write((float)(0.02 * Math.Sin(i * 2 * Math.PI * 220 / 44100)));
            }

            Id<Pattern> pattern = default;
            using (var import = ProjectMedia.BeginImport(document, "F4 authored PCM", ownedMediaDirectory: root))
            {
                await import.PrepareAsync(path);
                var media = import.Accept();
                document.Edit("F4 physical fixture", edit =>
                {
                    pattern = edit.AddPattern("Fixture", new(960000));
                    var part = edit.AddPart(pattern, "PCM", media.SoundId);
                    edit.AddNote(pattern, part, new(0), new(960000), 60, 1);
                });
            }

            byte[] canonical = ProjectPersistence.Encode(document);
            long generation = document.Generation;
            int undo = document.UndoCount;
            bool accepted = true;
            for (int iteration = 0; iteration < 3; iteration++)
            {
                var open = WasapiSelection.OpenOutput(owner, diagnostics: true, captureSamples: 200000);
                if (!open.IsOpen || owner.OutputSampler is not { } sampler || open.Output is not { } output)
                {
                    Print(new
                    {
                        Run = "selected-open", Accepted = false, open.Resolution,
                        Failure = open.Output?.Failure, HResult = open.Output?.HResult
                    });
                    return 1;
                }

                sampler.SetGain(0.05f);
                using (var preparation = sampler.BeginPreparation(document, pattern))
                {
                    await preparation.PrepareAsync();
                    if (preparation.Publish() != SamplerPublication.Accepted) return 1;
                }

                sampler.Start();
                output.Start(sampler);
                await Task.Delay(150);
                long stop = sampler.Stop();
                var wait = Stopwatch.StartNew();
                while (sampler.StopAcknowledgment < stop && output.Failure == OutputFailure.None &&
                       wait.ElapsedMilliseconds < 2000)
                    await Task.Delay(5);
                long acknowledgment = sampler.StopAcknowledgment;
                owner.SelectInput(AudioEndpointIntent.Explicit(inputId));
                bool independent = !output.Joined && owner.OutputIntent.EndpointId == endpointId;
                if (iteration == 1)
                {
                    output.InjectDeviceInvalidation();
                    wait.Restart();
                    while (!sampler.IsTerminated && wait.ElapsedMilliseconds < 2000) await Task.Delay(5);
                }

                // An active output intent change must join and release; restoring intent never implicitly plays.
                owner.SelectOutput(AudioEndpointIntent.Default(AudioEndpointRole.Multimedia));
                bool switched = output.Joined && !open.IsOpen && owner.OutputSampler is null &&
                                sampler.LivePreparedStates == 0;
                owner.SelectOutput(AudioEndpointIntent.Explicit(endpointId));
                bool sameMusic = canonical.SequenceEqual(ProjectPersistence.Encode(document)) &&
                                 document.UndoCount == undo && document.Generation == generation;
                bool run = independent && switched && sameMusic && acknowledgment >= stop &&
                           sampler.StopAcknowledgment >= acknowledgment && output.Facts?.Endpoint == endpointId &&
                           owner.InputIntent.EndpointId == inputId &&
                           output.Failure == (iteration == 1 ? OutputFailure.Native : OutputFailure.None) &&
                           output.ProcessorAllocatedBytes == 0 && output.ServiceMisses == 0 &&
                           output.PacketMisses == 0 &&
                           output.PaddingExhaustions == 0 && output.NonFiniteSamples == 0;
                accepted &= run;
                Print(new
                {
                    Run = "selected-output-lifetime", Iteration = iteration, Accepted = run,
                    output.Facts, output.Failure, HResult = $"0x{output.HResult:X8}", output.Joined,
                    IndependentInput = independent, OutputChanged = switched, CanonicalUnchanged = sameMusic,
                    Stop = stop, Acknowledgment = acknowledgment, sampler.PreparedStatesCreated,
                    sampler.PreparedStatesReleased,
                    output.CallbackCount, output.ProcessorAllocatedBytes, output.ServiceAllocatedBytes,
                    output.ServiceMisses, output.PacketMisses, output.PaddingExhaustions,
                    Peak = output.Capture.ToArray().Select(Math.Abs).DefaultIfEmpty().Max()
                });
            }

            owner.SelectOutput(AudioEndpointIntent.Explicit("deliberately-missing-output"));
            var missing = WasapiSelection.OpenOutput(owner);
            bool refused = !missing.IsOpen && missing.Output is null &&
                           missing.Resolution.Availability == AudioEndpointAvailability.Missing;
            accepted &= refused;
            Print(new { Run = "missing-explicit", Accepted = refused, missing.Resolution });
            // Direct adapter refusals exercise worker-side validation independently of the earlier snapshot.
            foreach (var candidate in snapshot.Endpoints.Where(item =>
                             item.Direction == AudioEndpointDirection.Output && item.State != AudioEndpointState.Active)
                         .Take(1)
                         .Concat(snapshot.Endpoints.Where(item => item.Direction == AudioEndpointDirection.Input)
                             .Take(1)))
            {
                using var invalid = new WasapiOutput(endpointId: candidate.Id);
                invalid.Close();
                bool rejected = invalid.Facts is null && invalid.CallbackCount == 0 && invalid.Joined &&
                                invalid.Failure == (candidate.Direction == AudioEndpointDirection.Input
                                    ? OutputFailure.InvalidEndpoint
                                    : OutputFailure.EndpointUnavailable);
                accepted &= rejected;
                Print(new
                {
                    Run = "native-refusal", Accepted = rejected, Candidate = candidate, invalid.Failure, invalid.Joined
                });
            }

            foreach (var role in Enum.GetValues<AudioEndpointRole>())
            {
                owner.SelectOutput(AudioEndpointIntent.Default(role));
                var following = WasapiSelection.OpenOutput(owner);
                bool opened = following.IsOpen && owner.OutputIntent.FollowsDefault &&
                              following.Output?.CallbackCount == 0;
                owner.CloseOutput();
                accepted &= opened;
                Print(new
                {
                    Run = "default-role-open-without-start", Accepted = opened, Role = role,
                    following.Resolution, Facts = following.Output?.Facts, Joined = following.Output?.Joined
                });
            }

            Print(new
            {
                Accepted = accepted, CaptureOpened = false, RecordingTested = false,
                CompleteF2PerformanceProtocol = false
            });
            return accepted ? 0 : 1;
        }
        finally
        {
            owner.CloseOutput();
            document.Close();
            // Exclusively created UUID fixture; never a supplied source or user-media directory.
            Directory.Delete(root, true);
        }
    }
}
