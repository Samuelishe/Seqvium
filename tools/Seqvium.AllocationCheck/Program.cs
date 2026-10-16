// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Seqvium.Core;

namespace Seqvium.AllocationCheck;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 4 || args[0] is not ("empty" or "spin" or "preview" or "mixer" or "positive") ||
            args[1] is not ("inline" or "thread") || args[2] is not ("none" or "allocate" or "background") ||
            !int.TryParse(args[3], out int count) || count is < 1 or > 128)
        {
            Console.Error.WriteLine("Usage: AllocationCheck <empty|spin|preview|mixer|positive> <inline|thread> <none|allocate|background> <1..128>");
            return 2;
        }

        var results = new Observation[count];
        Exception? failure = null;
        void Run()
        {
            try { Execute(args[0], args[2], results); }
            catch (Exception error) { failure = error; }
        }
        if (args[1] == "thread")
        {
            var consumer = new Thread(Run) { IsBackground = true, Name = "AllocationCheck consumer" };
            consumer.Start();
            if (!consumer.Join(TimeSpan.FromSeconds(60))) throw new TimeoutException("Consumer exceeded sixty seconds.");
        }
        else Run();
        if (failure is not null) throw new InvalidOperationException("Probe failed.", failure);

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            scenario = args[0], host = args[1], load = args[2], runtime = RuntimeInformation.FrameworkDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(), serverGc = System.Runtime.GCSettings.IsServerGC,
            latency = System.Runtime.GCSettings.LatencyMode.ToString(), debugger = Debugger.IsAttached,
            coreVersion = typeof(RealtimeSampler).Assembly.GetName().Version!.ToString(),
            observations = results, nonzero = results.Count(r => r.Bytes != 0),
            unexpected = results.Count(r => r.Bytes != (args[0] == "positive" ? 64 : 0))
        }));
        return results.Any(r => r.Bytes != (args[0] == "positive" ? 64 : 0)) ? 1 : 0;
    }

    private static void Execute(string scenario, string load, Observation[] results)
    {
        using var fixture = AudioFixture.Create(scenario);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var ready = new ManualResetEventSlim();
        Exception? workerFailure = null;
        Thread? worker = null;
        // Initialize counters, markers and measurement entry before concurrent load starts.
        _ = AllocationEvents.Log;
        _ = Stopwatch.GetTimestamp();
        _ = OperatingSystem.IsWindows() ? GetCurrentThreadId() : 0;
        fixture.Reset();
        _ = Measure(scenario, fixture);
        if (load != "none")
        {
            worker = new Thread(() =>
            {
                try
                {
                    // Fixed live bounds: 8 MiB large arrays plus 4 MiB linked small objects.
                    var ring = new byte[32][];
                    for (int i = 0; i < ring.Length; i++) ring[i] = new byte[256 * 1024];
                    Link? root = null;
                    for (int i = 0; i < 131072; i++) root = new(root);
                    ready.Set();
                    for (int i = 0; i < 1024 && !stop.IsCancellationRequested; i++)
                    {
                        ring[i % ring.Length] = new byte[256 * 1024];
                        if (load == "background") GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: false);
                        Thread.Sleep(2); // Only the diagnostic load producer sleeps, never the measured loop.
                    }
                    GC.KeepAlive(root);
                    GC.KeepAlive(ring);
                }
                catch (Exception error) { workerFailure = error; ready.Set(); }
            }) { IsBackground = true, Name = "AllocationCheck bounded load" };
            worker.Start();
        }
        try
        {
            if (worker is not null && !ready.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Load startup.");
            for (int i = 0; i < results.Length; i++)
            {
                stop.Token.ThrowIfCancellationRequested();
                fixture.Reset();
                // Give every interval a known real allocation context, outside its counter boundary.
                object anchor = Allocate(37 + i * 8);
                int thread = Environment.CurrentManagedThreadId;
                int nativeThread = OperatingSystem.IsWindows() ? (int)GetCurrentThreadId() : 0;
                int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
                long positionBefore = fixture.Position;
                long start = Stopwatch.GetTimestamp();
                AllocationEvents.Log.MeasurementBegin(i);
                long bytes = Measure(scenario, fixture);
                AllocationEvents.Log.MeasurementEnd(i, bytes);
                long end = Stopwatch.GetTimestamp();
                results[i] = new(i, bytes, thread, Environment.CurrentManagedThreadId, nativeThread,
                    gen0, GC.CollectionCount(0), gen1, GC.CollectionCount(1), gen2, GC.CollectionCount(2), start, end,
                    positionBefore, fixture.Position, fixture.Voices);
                fixture.CheckRenderedWindow();
                GC.KeepAlive(anchor);
            }
        }
        finally
        {
            stop.Cancel();
            if (worker is not null && !worker.Join(TimeSpan.FromSeconds(5))) throw new TimeoutException("Load shutdown.");
        }
        if (workerFailure is not null) throw new InvalidOperationException("Load failed.", workerFailure);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(string scenario, AudioFixture fixture) => scenario switch
    {
        "preview" => Preview(fixture.Audition!.Processor, fixture.Packet),
        "mixer" => Mixer(fixture.Execution!, fixture.Packet),
        "positive" => Positive(),
        "spin" => Spin(),
        _ => Empty()
    };

    private static uint _sink;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Empty()
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        uint value = 1;
        for (uint i = 0; i < 1000000; i++) value = (value ^ i) * 16777619;
        Volatile.Write(ref _sink, value);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Spin()
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        Thread.SpinWait(100000);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Positive()
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        object value = Allocate(37);
        uint checksum = 1;
        for (uint i = 0; i < 1000000; i++) checksum = (checksum ^ i) * 16777619;
        Volatile.Write(ref _sink, checksum);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(value);
        return bytes;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Allocate(int length) => new byte[length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Preview(RealtimeSampler processor, float[] packet)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 200; i++) processor.Process(packet.AsSpan(0, i % 2 == 0 ? 254 : 512));
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Mixer(OfflineSampler execution, float[] packet)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 50; i++) execution.Process(packet);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private sealed class Link(Link? next) { internal readonly Link? Next = next; }
    private readonly record struct Observation(int Index, long Bytes, int ThreadBefore, int ThreadAfter, int NativeThread,
        int Gen0Before, int Gen0After, int Gen1Before, int Gen1After, int Gen2Before, int Gen2After, long Start, long End,
        long PositionBefore, long PositionAfter, int VoicesAfter);
}

[EventSource(Name = "Seqvium-AllocationCheck")]
internal sealed class AllocationEvents : EventSource
{
    internal static readonly AllocationEvents Log = new();
    [Event(1)] public void MeasurementBegin(long index) { if (IsEnabled()) WriteEvent(1, index); }
    [Event(2)] public void MeasurementEnd(long index, long bytes) { if (IsEnabled()) WriteEvent(2, index, bytes); }
}
