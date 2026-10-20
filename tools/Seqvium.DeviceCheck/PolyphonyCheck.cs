// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Seqvium.Audio.Windows;
using Seqvium.Core;

[SupportedOSPlatform("windows")]
internal static class PolyphonyCheck
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    internal static async Task<int> Run(string[] args)
    {
        if (args.Length is < 5 or > 6 || args[0] is not ("cpu" or "physical"))
            throw new ArgumentException("polyphony cpu|physical <fixture-directory> <output-directory> <comma-separated-voices> none|background [comma-separated-kinds]");
        string fixtures = Path.GetFullPath(args[1]), output = Path.GetFullPath(args[2]);
        int[] counts = args[3].Split(',').Select(int.Parse).ToArray();
        if (counts.Any(n => n is < 1 or > 129) || args[4] is not ("none" or "background"))
            throw new ArgumentException("Voices must be 1..129 and load none|background.");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "environment.json"), JsonSerializer.Serialize(new
        {
            Command = Environment.CommandLine, Date = DateTimeOffset.UtcNow, Environment.OSVersion,
            Runtime = RuntimeInformation.FrameworkDescription, RuntimeInformation.ProcessArchitecture,
            Environment.ProcessorCount, Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            CoreVersion = typeof(RealtimeSampler).Assembly.GetName().Version?.ToString(),
            Stopwatch.Frequency, GCSettings.IsServerGC, GCSettings.LatencyMode,
            Capacity = typeof(GraphPreparation).GetField(nameof(GraphPreparation.MaximumVoices))!.GetRawConstantValue(),
            Priming = "2 seconds of production Process calls, with finite restarts, outside measurement",
            Criteria = "normal/pressure: p99 Process and service <= 50% actual period; max <= period; no misses/starvation/nonfinite; strict zero callback bytes; full PCM parity, peak and natural completion required. One machine only; historical R4 gaps separate."
        }, Json));
        bool accepted = true;
        string[] kinds = args.Length == 6 ? args[5].Split(',') : ["pcm", "synth", "release", "mixed", "dense", "live"];
        if (kinds.Any(k => k is not ("pcm" or "synth" or "release" or "mixed" or "dense" or "live")))
            throw new ArgumentException("Unknown workload.");
        foreach (int count in counts)
        foreach (string kind in kinds)
        {
            // Keep the low-load matrix small, while retaining the explicit musical capacities.
            if (count < 32 && kind != "pcm") continue;
            string name = $"{kind}-{count}";
            Console.WriteLine($"{args[0]} {name} {args[4]}");
            try
            {
                using var fixture = await PolyphonyFixture.Open(fixtures, kind, count, 2);
                object result = args[0] == "cpu" ? await Cpu(fixture, kind, args[4] == "background")
                    : await Physical(fixture, kind, args[4] == "background");
                string json = JsonSerializer.Serialize(result, Json);
                using (var parsed = JsonDocument.Parse(json)) accepted &= parsed.RootElement.GetProperty("Passed").GetBoolean();
                await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), json);
            }
            catch (Exception error)
            {
                accepted = false;
                await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(new
                    { Refused = true, Error = error.ToString() }, Json));
                Console.WriteLine(error.Message);
            }
        }
        return accepted ? 0 : 1;
    }

    private static async Task Publish(RealtimeSampler rt, ProjectDocument doc, bool live = false, long[]? ownership = null)
    {
        using var request = rt.BeginArrangementPreparation(doc, live);
        var work = request.PrepareAsync();
        if (ownership is not null)
        {
            ownership[0] = Math.Max(ownership[0], rt.RetainedPcmAndScratchBytes);
            ownership[1] = Math.Max(ownership[1], rt.ReservedPcmAndScratchBytes);
        }
        await work;
        if (request.Publish() != SamplerPublication.Accepted) throw new InvalidOperationException("Publication refused.");
        if (ownership is not null)
            ownership[0] = Math.Max(ownership[0], rt.RetainedPcmAndScratchBytes);
    }

    private static async Task<object> Cpu(PolyphonyFixture f, string kind, bool background)
    {
        const int rate = 48000, channels = 2, frames = 480;
        long allocated = GC.GetTotalAllocatedBytes(true), before = Stopwatch.GetTimestamp();
        using var plan = ArrangementPreparation.Prepare(f.Document, rate, channels, frames);
        double preparationUs = Us(Stopwatch.GetTimestamp() - before);
        long preparationBytes = GC.GetTotalAllocatedBytes(true) - allocated;
        var warmPreparationUs = new double[5]; var warmPreparationBytes = new long[5];
        for (int i = 0; i < warmPreparationUs.Length; i++)
        {
            long b = GC.GetAllocatedBytesForCurrentThread(), t = Stopwatch.GetTimestamp();
            using var prepared = ArrangementPreparation.Prepare(f.Document, rate, channels, frames);
            warmPreparationUs[i] = Us(Stopwatch.GetTimestamp() - t);
            warmPreparationBytes[i] = GC.GetAllocatedBytesForCurrentThread() - b;
        }
        using var rt = new RealtimeSampler(rate, channels, frames);
        var ownership = new long[2];
        await Publish(rt, f.Document, ownership: ownership);
        rt.Start();
        var block = new float[frames * channels];
        Prime(rt, block, plan.EndFrame);
        int blocks = (int)((plan.EndFrame + frames - 1) / frames);
        var ticks = new long[blocks]; var bytes = new long[blocks]; var voices = new int[blocks];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var pressure = new Pressure(background);
        long retainedPeak = 0, reservedPeak = 0, end = 0;
        var preparations = new List<double>();
        for (int i = 0; i < blocks; i++)
        {
            int size = (int)Math.Min(frames, plan.EndFrame - (long)i * frames) * channels;
            long b = GC.GetAllocatedBytesForCurrentThread(), t = Stopwatch.GetTimestamp();
            bool valid = rt.Process(block.AsSpan(0, size));
            ticks[i] = Stopwatch.GetTimestamp() - t;
            bytes[i] = GC.GetAllocatedBytesForCurrentThread() - b;
            if (!valid) throw new InvalidOperationException("Invalid callback.");
            voices[i] = rt.ActiveVoices; end = rt.RenderPosition;
            hash.AppendData(MemoryMarshal.AsBytes(block.AsSpan(0, size)));
            retainedPeak = Math.Max(retainedPeak, rt.RetainedPcmAndScratchBytes);
            reservedPeak = Math.Max(reservedPeak, rt.ReservedPcmAndScratchBytes);
            if (kind == "live" && i is 100 or 200 or 300)
            {
                if (i == 100)
                {
                    var pattern = f.Document.Current.State.Patterns[0]; var part = pattern.Parts[0];
                    f.Document.Edit("Compatible future note", e =>
                        e.AddNote(pattern.Id, part.Id, new(1440000), new(96000), 73, .01m));
                }
                else if (i == 200) f.Document.Undo(); else f.Document.Redo();
                long start = Stopwatch.GetTimestamp();
                rt.RetireCompleted(); await Publish(rt, f.Document, true, ownership);
                preparations.Add(Us(Stopwatch.GetTimestamp() - start));
            }
            rt.RetireCompleted();
        }
        pressure.Stop();
        var final = rt.ReadStatus();
        rt.Panic(); rt.Process(block); rt.TerminateExecution(); rt.RetireCompleted(); rt.Dispose();
        return new
        {
            Mode = "CPU simulation, not a device deadline observation", background, rate, channels, frames,
            Capacity = plan.VoiceCapacity, PreparationUs = preparationUs, PreparationAllocatedBytes = preparationBytes,
            WarmPreparationUs = warmPreparationUs, WarmPreparationBytes = warmPreparationBytes,
            LivePreparationUs = preparations, ProcessUs = Quantiles(ticks),
            CallbackBytes = bytes.Sum(), NonzeroAllocationWindows = bytes.Count(b => b != 0),
            PeakVoices = ReadPeak(rt, voices.Max()), FinalVoices = final.Voices, FinalTransport = final.Transport.ToString(),
            ActualFrames = end, plan.EndFrame, plan.DecodedBytes, plan.ScratchBytes, plan.MaximumPacketEvents, plan.PacketWork,
            RetainedPeakBytes = Math.Max(retainedPeak, ownership[0]), ReservedPeakBytes = Math.Max(reservedPeak, ownership[1]),
            WorkingSet = Environment.WorkingSet, PeakWorkingSet = Process.GetCurrentProcess().PeakWorkingSet64,
            ManagedBytes = GC.GetTotalMemory(false), Sha256 = Convert.ToHexString(hash.GetHashAndReset()),
            RawTicks = ticks, RawCallbackBytes = bytes, RawActiveVoices = voices,
            pressure.Iterations, rt.PreparedStatesCreated, rt.PreparedStatesReleased, rt.Retirements,
            Passed = bytes.All(b => b == 0) && final.Voices == 0 && final.Transport == SamplerTransportState.Ended
        };
    }

    private static async Task<object> Physical(PolyphonyFixture f, string kind, bool background)
    {
        Console.Error.WriteLine("Physical polyphony check plays quiet authored material; system settings remain unchanged.");
        using var output = new WasapiOutput(2 * 48000 * 26, diagnostics: true);
        if (output.Facts is not { } facts || output.Failure != OutputFailure.None)
            throw new InvalidOperationException($"Open: {output.Failure}, {output.HResult:X8}");
        using var rt = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames);
        using var plan = ArrangementPreparation.Prepare(f.Document, facts.Rate, facts.Channels, facts.CapacityFrames);
        using (var warm = plan.CreateExecution())
        {
            var block = new float[facts.CapacityFrames * facts.Channels];
            for (int i = 0; i < 400 && warm.Position + facts.CapacityFrames < plan.EndFrame; i++) warm.Process(block);
        }
        var ownership = new long[2];
        await Publish(rt, f.Document, ownership: ownership); rt.Start();
        var prime = new float[facts.CapacityFrames * facts.Channels];
        Prime(rt, prime, plan.EndFrame);
        using var pressure = new Pressure(background);
        var elapsed = Stopwatch.StartNew();
        long retainedPeak = 0, reservedPeak = 0;
        bool edited = false; long epoch = 0;
        try
        {
            output.Start(rt);
            while (elapsed.Elapsed.TotalSeconds < (double)plan.EndFrame / facts.Rate + 3 && output.Failure == OutputFailure.None &&
                   rt.ReadStatus().Transport != SamplerTransportState.Ended)
            {
                await Task.Delay(10);
                if (epoch == 0 && rt.ReadStatus().Playing) epoch = rt.TransportEpoch;
                if (background) for (int n = 0; n < 32; n++) rt.SetGain(n % 2 == 0 ? .95f : 1);
                if (kind == "live" && !edited && elapsed.Elapsed.TotalSeconds > 2)
                {
                    var pattern = f.Document.Current.State.Patterns[0];
                    f.Document.Edit("Future edit", e => e.AddNote(pattern.Id, pattern.Parts[0].Id,
                        new(1440000), new(96000), 73, .01m));
                    rt.RetireCompleted(); await Publish(rt, f.Document, true, ownership); edited = true;
                }
                retainedPeak = Math.Max(retainedPeak, rt.RetainedPcmAndScratchBytes);
                reservedPeak = Math.Max(reservedPeak, rt.ReservedPcmAndScratchBytes);
                rt.RetireCompleted();
            }
            var final = rt.ReadStatus();
            pressure.Stop();
            long stop = rt.Panic();
            var stopWait = Stopwatch.StartNew();
            while (rt.StopAcknowledgment < stop && output.Failure == OutputFailure.None && stopWait.ElapsedMilliseconds < 500)
                await Task.Delay(5);
            output.Close(); rt.TerminateExecution(); rt.RetireCompleted(); rt.Dispose();
            var observations = output.Observations.ToArray();
            double error = 0;
            if (!background && kind != "live")
            {
                using var mirror = plan.CreateExecution();
                var expected = new float[facts.CapacityFrames * facts.Channels]; int offset = 0;
                foreach (int partition in output.CapturePartitions)
                {
                    expected.AsSpan().Clear();
                    int render = (int)Math.Min(partition, plan.EndFrame - mirror.Position);
                    if (render > 0) mirror.Process(expected.AsSpan(0, render * facts.Channels));
                    for (int s = 0; s < partition * facts.Channels; s++)
                        error = Math.Max(error, Math.Abs(expected[s] - output.Capture[offset + s]));
                    offset += partition * facts.Channels;
                }
            }
            double periodUs = facts.Period100Nanoseconds / 10d;
            var process = observations.Where(o => o.Frames > 0).Select(o => o.ProcessorTicks).ToArray();
            var service = observations.Select(o => o.ServiceTicks).ToArray();
            bool passed = output.Failure == OutputFailure.None && output.Joined &&
                output.ProcessorAllocatedBytes == 0 && output.ServiceAllocatedBytes == 0 && output.ProcessorMisses == 0 && output.ServiceMisses == 0 &&
                output.PaddingExhaustions == 0 && output.PacketMisses == 0 && output.NonFiniteSamples == 0 &&
                final.Transport == SamplerTransportState.Ended && final.Voices == 0 &&
                final.Position == plan.EndFrame && rt.StopAcknowledgment >= stop && error == 0 &&
                process.Length > 0 && Us(process.Order().ElementAt((int)((process.Length - 1) * .99))) <= periodUs * .5 &&
                service.Length > 0 && Us(service.Order().ElementAt((int)((service.Length - 1) * .99))) <= periodUs * .5;
            return new
            {
                Passed = passed, background, facts, Capacity = plan.VoiceCapacity, PeriodUs = periodUs,
                ProcessUs = Quantiles(process), ServiceUs = Quantiles(service), RawObservations = observations,
                Final = final, plan.EndFrame, PeakVoices = ReadPeak(rt, output.MaximumObservedVoices),
                output.MaximumObservedVoices, output.Failure, output.HResult,
                output.UnexpectedError, output.ProcessorAllocatedBytes, output.ServiceAllocatedBytes,
                output.ProcessorMisses, output.ServiceMisses, output.PacketMisses, output.PaddingExhaustions,
                output.NonFiniteSamples, output.MaximumSampleMagnitude, output.Joined,
                SubmittedFrames = observations.LastOrDefault().SubmittedFrames, MaxPcmError = error,
                PcmCompared = !background && kind != "live", EpochPreserved = final.Epoch == epoch,
                StopAcknowledged = rt.StopAcknowledgment >= stop, RetainedPeakBytes = Math.Max(retainedPeak, ownership[0]),
                ReservedPeakBytes = Math.Max(reservedPeak, ownership[1]), PeakWorkingSet = Process.GetCurrentProcess().PeakWorkingSet64,
                pressure.Iterations, rt.PreparedStatesCreated, rt.PreparedStatesReleased, rt.Retirements
            };
        }
        finally { output.Close(); pressure.Stop(); rt.TerminateExecution(); rt.RetireCompleted(); }
    }

    private static double Us(long ticks) => ticks * 1_000_000d / Stopwatch.Frequency;
    // The same harness can run beside the preserved baseline Core, which predates peak diagnostics.
    private static int ReadPeak(RealtimeSampler rt, int observed) =>
        (int?)typeof(RealtimeSampler).GetProperty("PeakVoices")?.GetValue(rt) ?? observed;
    private static void Prime(RealtimeSampler rt, float[] block, long end)
    {
        var warm = Stopwatch.StartNew();
        while (warm.Elapsed.TotalSeconds < 2)
        {
            if (rt.RenderPosition + block.Length / rt.Channels >= end)
            { rt.Stop(); rt.Process(block); rt.Start(); }
            rt.Process(block);
        }
        rt.Stop(); rt.Process(block); rt.Start();
    }
    private static object Quantiles(long[] ticks)
    {
        if (ticks.Length == 0) return new { Count = 0 };
        long[] sorted = ticks.Order().ToArray();
        return new { Count = ticks.Length, P50 = Us(sorted[(int)((sorted.Length - 1) * .5)]),
            P95 = Us(sorted[(int)((sorted.Length - 1) * .95)]), P99 = Us(sorted[(int)((sorted.Length - 1) * .99)]), Max = Us(sorted[^1]) };
    }

    private sealed class Pressure : IDisposable
    {
        private readonly CancellationTokenSource _stop = new();
        private readonly Thread? _thread;
        internal int Iterations;
        internal Pressure(bool enabled)
        {
            if (!enabled) return;
            _thread = new Thread(() =>
            {
                var ring = new byte[128][];
                while (!_stop.IsCancellationRequested && Iterations < 10000)
                {
                    for (int i = 0; i < 16; i++) ring[(Iterations * 16 + i) % ring.Length] = new byte[32768];
                    if (++Iterations % 20 == 0) GC.Collect(2, GCCollectionMode.Forced, blocking: false);
                    Thread.Sleep(5);
                }
                GC.KeepAlive(ring);
            }) { IsBackground = true, Name = "Polyphony bounded GC pressure" };
            _thread.Start();
        }
        internal void Stop()
        {
            _stop.Cancel();
            if (_thread is not null && !_thread.Join(5000)) throw new TimeoutException("Pressure did not join.");
        }
        public void Dispose() { Stop(); _stop.Dispose(); }
    }
}
