// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Seqvium.R0;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    internal static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] == "verify") { Verification.Run(); return 0; }
            if (args[0] is not ("device" or "controlled" or "lifetime"))
                throw new ArgumentException("Use verify | device [seconds] [reverse] | controlled [seconds] [reverse] | lifetime");
            Verification.Run();
            Console.WriteLine($"Runtime={Environment.Version}, GC={GCSettings.LatencyMode}, ServerGC={GCSettings.IsServerGC}, QPC={Stopwatch.Frequency}");
            Warm();
            foreach (bool managed in new[] { false, true })
            {
                using var boundary = new Probe(managed);
                Console.WriteLine($"EMPTY_CALLBACK: {(managed ? "managed" : "native")}, us/call={Native.r0_empty_calls(boundary.Handle, 1000000):R}, calls=1000000; includes managed allocation-counter instrumentation");
            }
            if (args[0] == "lifetime") { await Lifetime(); return 0; }
            int seconds = args.Length > 1 ? int.Parse(args[1]) : 10;
            bool reverse = args.Contains("reverse");
            foreach (bool managed in reverse ? new[] { true, false } : new[] { false, true })
                foreach (string mode in new[] { "baseline", "pressure", "overload" })
                    await Run(managed, args[0] == "controlled", mode, mode == "overload" ? 5 : seconds);
            Verification.Require(Native.r0_live_probes() == 0, "Native probe leak");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static void Warm()
    {
        foreach (bool managed in new[] { false, true })
        {
            using var p = new Probe(managed); p.Publish(1); p.Control(1); var buffer = new float[256];
            for (int i = 0; i < 2000; ++i) p.Render(buffer, 256);
        }
    }
    private static async Task Run(bool managed, bool controlled, string mode, int seconds)
    {
        using var p = new Probe(managed); p.Publish(1); p.Control(1);
        int[] gcBefore = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        long allocatedBefore = GC.GetTotalAllocatedBytes();
        int hr = controlled ? Native.r0_simulate(p.Handle, seconds, 256, mode == "overload" ? 1 : 0)
            : Native.r0_open(p.Handle, seconds, mode == "overload" ? 1 : 0);
        if (hr != 0)
        {
            Verification.Require(Native.r0_join(p.Handle, 1) == 0, "Failed-init join");
            Console.WriteLine(JsonSerializer.Serialize(new { candidate = managed ? "managed" : "native", mode,
                environment = controlled ? "controlled" : "device", initializationHResult = $"0x{hr:X8}" }, JsonOptions));
            return;
        }
        var join = Task.Run(() => Native.r0_join(p.Handle, 0));
        PressureResult? pressure = mode == "pressure" ? await Pressure(p, seconds) : null;
        if (mode == "overload")
        {
            await Task.Delay((seconds * 1000) - 500);
            p.Control(2); p.Control(3); p.Control(4);
        }
        Verification.Require(await join == 0, "Finite worker join failed");
        var s = p.Statistics;
        if (pressure is not null)
            Verification.Require(s.AcknowledgedState == 0 && s.AcknowledgedGeneration == pressure.PreparationRequests + 1,
                "Stop/panic or latest prepared-state convergence failed before shutdown");
        if (mode == "overload") Verification.Require(s.AcknowledgedState == 0, "Overload panic failed before shutdown");
        Verification.Require(s.Hr == 0 && s.ClockRegressions == 0 && s.Voices == 0 && s.Playing == 0 && s.LiveSlots <= 4,
            "Run failed or invalid final state");
        int[] gc = [GC.CollectionCount(0)-gcBefore[0], GC.CollectionCount(1)-gcBefore[1], GC.CollectionCount(2)-gcBefore[2]];
        object? parity = !controlled && mode == "baseline" && s.SkippedFrames == 0 ? Verification.CompareDeviceCapture(p, s.Channels) : null;
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            candidate = managed ? "managed" : "native", environment = controlled ? "controlled (no audio device)" : "WASAPI shared event",
            mode, endpoint = Marshal.PtrToStringUni(Native.r0_endpoint(p.Handle)),
            s.Rate, s.Channels, s.BufferFrames, s.PeriodFrames,
            periodBudgetUs = s.PeriodFrames * 1000000.0 / s.Rate,
            s.Callbacks, s.ElapsedSeconds, s.QpcPairUs, s.Mmcss,
            s.ClockFrequency, s.StreamLatencyMs, s.AcknowledgedState, s.AcknowledgedGeneration,
            s.MaximumWallDeficit, s.MaximumPostRecoveryDeficit, s.FinalWallDeficit, s.Recoveries,
            metrics = Metrics(p), s.PaddingEmpty, s.SkippedFrames, s.SkippedEvents, s.ClockRegressions,
            s.DeviceFrame, s.SubmittedFrame, leadFrames = (long)s.SubmittedFrame - (long)s.DeviceFrame,
            s.TimingFull, gcCollections = gc, applicationAllocatedBytes = GC.GetTotalAllocatedBytes() - allocatedBefore,
            callbackAllocatedBytes = managed ? (long?)p.CallbackAllocations : null, p.CallbackCalls,
            s.Publishes, s.Rejected, s.Superseded, s.Retired, s.ActiveGeneration, s.LiveSlots, s.Voices, s.Playing,
            pressure, offlineCaptureComparison = parity, shutdownHResult = $"0x{s.Hr:X8}"
        }, JsonOptions));
    }
    private static unsafe object Metrics(Probe p)
    {
        var s = p.Statistics; var timings = Native.r0_timings(p.Handle);
        int count = Math.Min(s.Callbacks, 65536);
        var processing = new double[count]; var service = new double[count]; var intervals = new double[count];
        int processingMisses = 0, serviceMisses = 0, packetMisses = 0;
        double budget = s.PeriodFrames * 1000000.0 / s.Rate;
        int minFrames = int.MaxValue, maxFrames = 0;
        for (int i = 0; i < count; ++i)
        {
            var t = timings[i]; processing[i] = t.ProcessingUs; service[i] = t.ServiceUs; intervals[i] = t.IntervalUs;
            if (t.ProcessingUs > budget) ++processingMisses;
            if (t.ServiceUs > budget) ++serviceMisses;
            if (t.ServiceUs > t.Frames * 1000000.0 / s.Rate) ++packetMisses;
            minFrames = Math.Min(minFrames, t.Frames); maxFrames = Math.Max(maxFrames, t.Frames);
        }
        return new { processingUs = Distribution(processing), serviceUs = Distribution(service), intervalUs = Distribution(intervals),
            processingMisses, serviceMisses, packetMisses, minFrames, maxFrames };
    }
    private static object Distribution(double[] values)
    {
        Array.Sort(values);
        double At(double percentile) => values.Length == 0 ? 0 : values[Math.Max(0, (int)Math.Ceiling(values.Length * percentile) - 1)];
        return new { median = At(0.5), p95 = At(0.95), p99 = At(0.99), worst = At(1) };
    }
    private sealed record PressureResult(int Iterations, int Controls, int PreparationRequests, int Published, int Obsolete, bool Cancelled);
    private static async Task<PressureResult> Pressure(Probe p, int seconds)
    {
        // One worker and one latest generation: slow preparation cannot build an unbounded work queue.
        int requested = 1, iterations = 0, controls = 0, requests = 0, published = 0, obsolete = 0;
        using var cancellation = new CancellationTokenSource();
        var preparation = Task.Run(async () =>
        {
            int seen = 1;
            try
            {
                while (true)
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    int generation = Volatile.Read(ref requested);
                    if (generation == seen) { await Task.Delay(1, cancellation.Token); continue; }
                    seen = generation;
                    await Task.Delay(15, cancellation.Token); // Deliberately slower than request production.
                    var table = (float[])Sequence.Table.Clone();
                    if (generation != Volatile.Read(ref requested)) { ++obsolete; continue; }
                    Native.r0_reclaim(p.Handle);
                    if (p.TryPublish(generation, Sequence.Events, table) == 0) ++published; else ++obsolete;
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        });
        var retained = new byte[128][];
        var timer = Stopwatch.StartNew();
        try
        {
            while (timer.Elapsed.TotalSeconds < Math.Max(0.2, seconds - 0.7))
            {
                for (int i = 0; i < 16; ++i) { var bytes = new byte[32768]; bytes[0] = (byte)i; retained[(iterations * 16 + i) & 127] = bytes; }
                for (int i = 0; i < 256; ++i) p.Control(5, (i & 1) * 0.5f);
                controls += 256;
                if (iterations % 7 == 0) { p.Control(2); p.Control(3); p.Control(4); p.Control(1); controls += 4; }
                int generation = ++requests + 1;
                Native.r0_request(p.Handle, generation); Volatile.Write(ref requested, generation);
                if (iterations % 10 == 0) GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                ++iterations; await Task.Delay(5);
            }
            // Give the single final request time to finish; synchronization/ack checks follow after join.
            await Task.Delay(100);
            p.Control(2); p.Control(3); p.Control(4); controls += 3;
        }
        finally { cancellation.Cancel(); await preparation; }
        GC.KeepAlive(retained);
        return new(iterations, controls, requests, published, obsolete, preparation.IsCompletedSuccessfully);
    }
    private static async Task Lifetime()
    {
        foreach (bool managed in new[] { false, true })
        {
            using var process = Process.GetCurrentProcess(); process.Refresh();
            int handlesBefore = process.HandleCount; long memoryBefore = process.PrivateMemorySize64;
            int success = 0, failures = 0;
            var handleTrend = new List<int>();
            var memoryTrend = new List<long>();
            for (int i = 0; i < 10; ++i)
            {
                using (var p = new Probe(managed))
                {
                    p.Publish(1); p.Control(1);
                    Verification.Require(Native.r0_open(p.Handle, 0, 0) == -1, "Invalid open accepted");
                    int hr = Native.r0_open(p.Handle, 1, 0);
                    if (hr == 0)
                    {
                        Verification.Require(Native.r0_open(p.Handle, 1, 0) == -1, "Concurrent open accepted");
                        p.Control(2); p.Control(1);
                        p.Publish(2); // Concurrent publication while callback owns active state.
                        await Task.Delay(80);
                        p.Control(3); p.Control(4);
                        Verification.Require(Native.r0_join(p.Handle, 1) == 0, "Cancel/close join");
                        long calls = p.CallbackCalls;
                        int callbacks = p.Statistics.Callbacks;
                        await Task.Delay(20);
                        Verification.Require(p.CallbackCalls == calls && p.Statistics.Callbacks == callbacks && p.Statistics.Voices == 0,
                            "Callback after close or stuck state");
                        Verification.Require(Native.r0_open(p.Handle, 1, 0) == -5, "Unsupported same-instance reopen accepted");
                        ++success;
                    }
                    else { Verification.Require(Native.r0_join(p.Handle, 1) == 0, "Failed initialization cleanup"); ++failures; }
                }
                process.Refresh(); handleTrend.Add(process.HandleCount); memoryTrend.Add(process.PrivateMemorySize64);
                Verification.Require(Native.r0_live_probes() == 0, "Native instance not disposed");
            }
            GC.Collect(); process.Refresh();
            Verification.Require(Native.r0_live_probes() == 0, "Native lifetime leak");
            Console.WriteLine(JsonSerializer.Serialize(new { candidate = managed ? "managed" : "native", lifecycleSuccessful = success,
                unavailable = failures, handlesBefore, handlesAfter = process.HandleCount, memoryBeforeBytes = memoryBefore,
                memoryAfterBytes = process.PrivateMemorySize64, handleTrend, memoryTrendBytes = memoryTrend }, JsonOptions));
        }
    }
}
