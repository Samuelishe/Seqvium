// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Seqvium.Core;
using Seqvium.Audio.Windows;

if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
{
    Console.Error.WriteLine("Physical verification requires Windows x64.");
    return 2;
}

return await DeviceCheck.Run(args);

[SupportedOSPlatform("windows")]
internal static class DeviceCheck
{
    private sealed class Fixture : IDisposable
    {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "Seqvium-F2-" + Guid.NewGuid().ToString("N"));
        internal readonly ProjectDocument Document;
        internal readonly float[] Source;
        internal Id<Pattern> Pattern;

        internal Fixture(int voices)
        {
            Document = ProjectDocument.Create(new(new Tempo(voices == 4 ? 120 : 137), new Meter(4, 4)));
            Source = new float[44100];
            for (int i = 0; i < Source.Length; i++)
                Source[i] = (float)(0.025 * Math.Sin(i * 2 * Math.PI * 220 / 44100) + 0.005 * (1 - i / 44100.0));
        }

        internal async Task Initialize(int voices)
        {
            Directory.CreateDirectory(Root);
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true))
            {
                writer.Write("RIFF"u8);
                writer.Write(36 + Source.Length * 4);
                writer.Write("WAVEfmt "u8);
                writer.Write(16);
                writer.Write((ushort)3);
                writer.Write((ushort)1);
                writer.Write(44100);
                writer.Write(176400);
                writer.Write((ushort)4);
                writer.Write((ushort)32);
                writer.Write("data"u8);
                writer.Write(Source.Length * 4);
                foreach (float sample in Source) writer.Write(sample);
            }

            stream.Position = 0;
            using var import = ProjectMedia.BeginImport(Document, "F2 authored PCM", releaseMilliseconds: 50,
                ownedMediaDirectory: Root);
            await import.PrepareAsync(stream);
            var media = import.Accept();
            Document.Edit("F2 PCM workload", edit =>
            {
                Pattern = edit.AddPattern("F2 quarter", new(MusicalPosition.TicksPerQuarter));
                var part = edit.AddPart(Pattern, "Independent voices", media.SoundId);
                for (int i = 0; i < voices; i++)
                    edit.AddNote(Pattern, part, new(i * 1234),
                        new(MusicalPosition.TicksPerQuarter * 7 / 10), 60 + i * 0.1m, 0.5m);
            });
        }

        public void Dispose()
        {
            if (!Document.IsClosed) Document.Close();
            // Exclusively created UUID directory, never a supplied user path or source/library directory.
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }

    internal static async Task<int> Run(string[] args)
    {
        string mode = args.Length == 0 ? "smoke" : args[0];
        int seconds = args.Length > 1 ? int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) :
            mode == "measure" ? 60 : 2;
        if (mode is not ("smoke" or "measure" or "pressure" or "lifetimes" or "faults") || seconds is < 1 or > 120)
            throw new ArgumentException("smoke|measure|pressure [1..120 seconds]|lifetimes|faults");
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Environment = RuntimeInformation.OSDescription, Runtime = RuntimeInformation.FrameworkDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(), Stopwatch.Frequency,
            GCSettings.IsServerGC, GcLatency = GCSettings.LatencyMode.ToString(), Mode = mode, Seconds = seconds
        }));
        bool accepted = true;
        if (mode == "pressure") accepted &= await Configuration(8, seconds, true);
        if (mode is "smoke" or "measure")
        {
            foreach (int voices in new[] { 1, 4, 8 })
            {
                accepted &= await Configuration(voices, seconds, false);
                if (mode == "measure") accepted &= await Configuration(voices, seconds, true);
            }
        }

        if (mode is "measure" or "lifetimes")
            for (int i = 0; i < 10; i++)
                accepted &= await Lifetime(i);
        if (mode is "measure" or "faults")
        {
            accepted &= await Fault(false);
            accepted &= await Fault(true);
            accepted &= await Unavailable();
        }

        Console.WriteLine(JsonSerializer.Serialize(new
            { Accepted = accepted, CompleteProtocol = mode == "measure" && seconds == 60 }));
        return accepted ? 0 : 1;
    }

    private static async Task Prepare(RealtimeSampler sampler, Fixture fixture, int voices)
    {
        using var request = sampler.BeginPreparation(fixture.Document, fixture.Pattern, voices, repeats: 1024);
        await request.PrepareAsync();
        if (request.Publish() != SamplerPublication.Accepted)
            throw new InvalidOperationException("Initial publication rejected.");
    }

    private static async Task<bool> Configuration(int voices, int seconds, bool pressure)
    {
        using var fixture = new Fixture(voices);
        await fixture.Initialize(voices);
        using var output = new WasapiOutput(Math.Min(1048576, seconds * 44100));
        if (output.Facts is not { } facts)
        {
            output.Close();
            Console.WriteLine(JsonSerializer.Serialize(new
                { Run = "open", output.Failure, output.HResult, output.UnexpectedError }));
            return false;
        }

        using var sampler = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames);
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, facts.Rate,
            facts.Channels, voices, repeats: 1024);
        using (var warm = plan.CreateExecution())
        {
            float[] block = new float[256 * facts.Channels];
            for (int i = 0; i < 2000; i++) warm.Process(block);
        }

        await Prepare(sampler, fixture, voices);
        var ring = new byte[128][];
        int ringIndex = 0, cycles = 0, controls = 0, publications = 1, preparationCancellations = 0;
        int inFlightCancellations = 0, inFlightSupersessions = 0;
        long beforeAllocation = GC.GetTotalAllocatedBytes(true);
        int[] collections = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        sampler.Start();
        output.Start(sampler);
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed.TotalSeconds < seconds && output.Failure == OutputFailure.None)
        {
            await Task.Delay(10);
            if (!pressure) continue;
            for (int i = 0; i < 16; i++) ring[ringIndex++ % ring.Length] = new byte[32768];
            for (int i = 0; i < 256; i++)
            {
                sampler.SetGain(i % 2 == 0 ? 0.8f : 1);
                controls++;
            }

            cycles++;
            if (cycles % 10 == 0) GC.Collect(2, GCCollectionMode.Forced, true, true);
            sampler.RetireCompleted();
            if (cycles % 20 == 0)
            {
                var sound = fixture.Document.Current.State.Sounds.Single();
                fixture.Document.Edit("New prepared sound revision", edit =>
                    edit.ConfigurePcmSampler(sound.Id, sound.ResourceIds[0], 60m + cycles / 20 % 2 * 0.5m, 50));
                using var request = sampler.BeginPreparation(fixture.Document, fixture.Pattern, voices, repeats: 1024);
                using var cancellation = new CancellationTokenSource();
                var preparation = request.PrepareAsync(cancellation.Token);
                if (cycles % 60 == 0)
                {
                    if (!preparation.IsCompleted) inFlightCancellations++;
                    cancellation.Cancel();
                }
                else if (cycles % 80 == 0)
                {
                    if (!preparation.IsCompleted) inFlightSupersessions++;
                    sampler.InvalidatePreparation();
                }

                try
                {
                    await preparation;
                    var result = request.Publish(cancellation.Token);
                    if (result == SamplerPublication.Accepted) publications++;
                }
                catch (OperationCanceledException)
                {
                    preparationCancellations++;
                }

                if (cycles % 100 == 0)
                {
                    using var cancelled =
                        sampler.BeginPreparation(fixture.Document, fixture.Pattern, voices, repeats: 1024);
                    cancellation.Cancel();
                    try
                    {
                        await cancelled.PrepareAsync(cancellation.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        preparationCancellations++;
                    }
                }
            }
        }

        long stop = sampler.Stop();
        bool acknowledgment = await Acknowledge(sampler, stop);
        int voicesAtStop = sampler.ActiveVoices;
        long ackAtStop = sampler.StopAcknowledgment;
        output.Close();
        sampler.RetireCompleted();
        sampler.Dispose();
        long allocations = GC.GetTotalAllocatedBytes(true) - beforeAllocation;
        int[] gc =
        [
            GC.CollectionCount(0) - collections[0], GC.CollectionCount(1) - collections[1],
            GC.CollectionCount(2) - collections[2]
        ];
        var parity = pressure ? null : Compare(output, plan, fixture);
        bool accepted = output.Failure == OutputFailure.None && output.ProcessorMisses == 0 &&
                        output.ServiceMisses == 0 && output.PacketMisses == 0 &&
                        output.PaddingExhaustions == 0 && output.ProcessorAllocatedBytes == 0 && acknowledgment &&
                        voicesAtStop == 0 &&
                        output.ElapsedSeconds >= seconds &&
                        output.MaximumObservedVoices == voices && output.NonFiniteSamples == 0 &&
                        output.SilentPacketsAfterStop > 0 && sampler.LivePreparedStates == 0 &&
                        (pressure || parity!.Finite && parity.MaximumError <= 0.000002 &&
                            parity.OracleError <= 0.000002);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Run = pressure ? "pressure" : "baseline", Voices = voices, Accepted = accepted, facts,
            Timing = Timings(output), output.ElapsedSeconds, output.CallbackCount, output.Failure,
            HResult = $"0x{output.HResult:X8}", output.UnexpectedError,
            output.ProcessorMisses, output.ServiceMisses, output.PacketMisses, output.PaddingExhaustions,
            output.ProcessorAllocatedBytes, output.ServiceAllocatedBytes,
            output.WorkerAllocatedBytes, ProcessAllocatedBytes = allocations, Collections = gc, cycles, controls,
            publications, preparationCancellations,
            inFlightCancellations, inFlightSupersessions,
            sampler.StalePublications, sampler.CancelledPublications, sampler.CapacityRejections,
            sampler.ConsumerStaleRejections, sampler.Retirements,
            sampler.PreparedStatesCreated, sampler.PreparedStatesReleased, sampler.LivePreparedStates,
            sampler.TerminationBoundaries,
            output.MaximumObservedVoices, output.SilentPacketsAfterStop, output.NonFiniteSamples,
            StopRequested = stop, StopAckBeforeClose = ackAtStop, Acknowledged = acknowledgment,
            VoicesAtStop = voicesAtStop,
            VoicesAfterFaultBoundary = sampler.ActiveVoices, sampler.TransportEpoch, sampler.RenderPosition,
            sampler.ExecutingRevision,
            fixture.Document.Current.Revision, output.Joined, Parity = parity
        }));
        GC.KeepAlive(ring);
        return accepted;
    }

    private sealed record Parity(int Samples, int Packets, double MaximumError, double OracleError, bool Finite);

    private static Parity Compare(WasapiOutput output, PreparedSampler plan, Fixture fixture)
    {
        var actual = output.Capture;
        float[] expected = new float[actual.Length];
        using var offline = plan.CreateExecution();
        int offset = 0;
        foreach (int frames in output.CapturePartitions)
        {
            offline.Process(expected.AsSpan(offset, frames * plan.Channels));
            offset += frames * plan.Channels;
        }

        double maximum = 0, oracleError = 0;
        bool finite = true;
        var pattern = fixture.Document.Current.State.Patterns.Single(item => item.Id == fixture.Pattern);
        int bpm = (int)fixture.Document.Current.State.Settings.Tempo.BeatsPerMinute;
        int releaseFrames = plan.SampleRate / 20;
        for (int frame = 0; frame < actual.Length / plan.Channels; frame++)
        {
            double oracle = 0;
            // Direct independent note sum, no production event extraction, voice selection or sampler calls.
            int iteration = (int)((long)frame * bpm / (60L * plan.SampleRate));
            // An absolute boundary rounded down can begin the next loop before the unrounded quotient advances.
            for (int loop = Math.Max(0, iteration - 1); loop <= iteration + 1; loop++)
                foreach (var note in pattern.Parts[0].Notes)
                {
                    long on = Frames(loop * pattern.Length.Ticks + note.Position.Ticks, plan.SampleRate, bpm);
                    long off = Frames(loop * pattern.Length.Ticks + note.Position.Ticks + note.Duration.Ticks,
                        plan.SampleRate, bpm);
                    long age = frame - on;
                    if (age < 0 || frame >= off + releaseFrames) continue;
                    double cursor = age * 44100.0 / plan.SampleRate * Math.Pow(2, (double)(note.Pitch - 60) / 12);
                    if (cursor >= fixture.Source.Length) continue;
                    int index = (int)cursor;
                    double fraction = cursor - index;
                    double first = fixture.Source[index],
                        second = index + 1 < fixture.Source.Length ? fixture.Source[index + 1] : 0;
                    double envelope = frame < off ? 1 : 1 - (frame - off) / (double)releaseFrames;
                    oracle += (first + (second - first) * fraction) * (float)note.Intensity * envelope;
                }

            for (int channel = 0; channel < plan.Channels; channel++)
            {
                int index = frame * plan.Channels + channel;
                finite &= float.IsFinite(actual[index]);
                maximum = Math.Max(maximum, Math.Abs(actual[index] - expected[index]));
                oracleError = Math.Max(oracleError, Math.Abs(actual[index] - oracle));
            }
        }

        return new(actual.Length, output.CapturePartitions.Length, maximum, oracleError, finite);
    }

    private static long Frames(long ticks, int rate, int bpm)
    {
        BigInteger numerator = (BigInteger)ticks * rate * 60, denominator = (BigInteger)960000 * bpm;
        return (long)((2 * numerator + denominator) / (2 * denominator));
    }

    private static object Timings(WasapiOutput output)
    {
        var observations = output.Observations.ToArray();
        return new
        {
            ProcessorMicroseconds = Quantiles(observations.Select(item => item.ProcessorTicks)),
            ServiceMicroseconds = Quantiles(observations.Select(item => item.ServiceTicks)),
            WakeMicroseconds = Quantiles(observations.Select(item => item.WakeTicks)),
            MinimumPacket = observations.Length == 0 ? 0 : observations.Min(item => item.Frames),
            MaximumPacket = observations.Length == 0 ? 0 : observations.Max(item => item.Frames),
            Last = observations.LastOrDefault()
        };
    }

    private static double[] Quantiles(IEnumerable<long> ticks)
    {
        var sorted = ticks.Order().ToArray();
        if (sorted.Length == 0) return [0, 0, 0, 0];
        return new[] { 0.5, 0.95, 0.99, 1 }
            .Select(q => sorted[(int)Math.Ceiling(sorted.Length * q) - 1] * 1000000.0 / Stopwatch.Frequency).ToArray();
    }

    private static async Task<bool> Acknowledge(RealtimeSampler sampler, long stop)
    {
        var elapsed = Stopwatch.StartNew();
        while (sampler.StopAcknowledgment < stop && elapsed.Elapsed.TotalSeconds < 2) await Task.Delay(10);
        return sampler.StopAcknowledgment >= stop;
    }

    private static async Task<bool> Lifetime(int number)
    {
        using var fixture = new Fixture(8);
        await fixture.Initialize(8);
        using var output = new WasapiOutput(0);
        if (output.Facts is not { } facts)
        {
            output.Close();
            return false;
        }

        using var sampler = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames);
        await Prepare(sampler, fixture, 8);
        output.Start(sampler);
        await Task.Delay(80);
        bool silentBeforeStart = output.SilentPackets > 0 && output.MaximumObservedVoices == 0;
        sampler.Start();
        await Task.Delay(80);
        long stop = sampler.Stop();
        bool firstAck = await Acknowledge(sampler, stop);
        int firstVoices = sampler.ActiveVoices;
        long epoch = sampler.TransportEpoch;
        sampler.Start();
        await Task.Delay(80);
        bool restarted = sampler.TransportEpoch > epoch;
        long panic = sampler.Panic();
        bool panicAck = await Acknowledge(sampler, panic);
        int panicVoices = sampler.ActiveVoices;
        output.Close();
        sampler.RetireCompleted();
        sampler.Dispose();
        bool accepted = silentBeforeStart && firstAck && firstVoices == 0 && restarted && panicAck &&
                        panicVoices == 0 && output.Joined &&
                        output.Failure == OutputFailure.None && sampler.LivePreparedStates == 0;
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Run = "lifetime", number, Accepted = accepted, silentBeforeStart, firstAck, firstVoices, restarted,
            panicAck, panicVoices,
            output.Failure, output.Joined, output.CallbackCount, sampler.StopAcknowledgment, sampler.LivePreparedStates,
            sampler.TerminationBoundaries
        }));
        return accepted;
    }

    private static async Task<bool> Fault(bool invalidation)
    {
        using var fixture = new Fixture(8);
        await fixture.Initialize(8);
        byte[] before = ProjectPersistence.Encode(fixture.Document);
        using var output = new WasapiOutput(0);
        if (output.Facts is not { } facts)
        {
            output.Close();
            return false;
        }

        using var sampler = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames);
        await Prepare(sampler, fixture, 8);
        sampler.Start();
        output.Start(sampler);
        await Task.Delay(100);
        if (invalidation) output.InjectDeviceInvalidation();
        else output.InjectWorkerStall();
        await Task.Delay(150);
        output.Close();
        sampler.RetireCompleted();
        sampler.Dispose();
        bool unchanged = before.SequenceEqual(ProjectPersistence.Encode(fixture.Document));
        using var stillAvailable = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, facts.Rate);
        bool accepted = unchanged && output.Joined && sampler.ActiveVoices == 0 && sampler.TerminationBoundaries == 1 &&
                        sampler.LivePreparedStates == 0 &&
                        (invalidation
                            ? output.HResult == unchecked((int)0x88890004)
                            : output.Failure == OutputFailure.Starvation && output.ServiceMisses > 0);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Run = invalidation ? "injected-device-invalidation" : "injected-30ms-stall", Accepted = accepted,
            output.Failure, HResult = $"0x{output.HResult:X8}", output.Joined, output.CallbackCount,
            output.ElapsedSeconds, Timing = Timings(output),
            output.ServiceMisses, output.PacketMisses, output.PaddingExhaustions, sampler.StopAcknowledgment,
            sampler.ActiveVoices, sampler.LivePreparedStates, sampler.TerminationBoundaries,
            CanonicalUnchanged = unchanged
        }));
        return accepted;
    }

    private static async Task<bool> Unavailable()
    {
        using var fixture = new Fixture(1);
        await fixture.Initialize(1);
        byte[] before = ProjectPersistence.Encode(fixture.Document);
        using var output = new WasapiOutput(0, "{0.0.0.00000000}.{00000000-0000-0000-0000-000000000000}");
        output.Close();
        using var plan = SamplerPreparation.PreparePattern(fixture.Document, fixture.Pattern, 48000);
        bool unchanged = before.SequenceEqual(ProjectPersistence.Encode(fixture.Document));
        bool accepted = output.Failure == OutputFailure.Native && output.HResult < 0 && output.Joined && unchanged;
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Run = "unavailable-endpoint", Accepted = accepted, output.Failure,
            HResult = $"0x{output.HResult:X8}", output.Joined, CanonicalUnchanged = unchanged,
            MediaStillDecodes = plan.Events.Length > 0
        }));
        return accepted;
    }
}
