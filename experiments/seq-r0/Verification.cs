// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.R0;

internal static unsafe class Verification
{
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    internal static void Run()
    {
        Require(Native.r0_size(0) == sizeof(Engine), "Engine ABI size");
        Require(Native.r0_size(1) == sizeof(Stats), "Stats ABI size");
        Require(Native.r0_size(2) == sizeof(Timing), "Timing ABI size");
        Require(Native.r0_size(3) == sizeof(MusicalEvent), "Event ABI size");
        float[]? reference = null;
        float maximumError = 0;
        int cases = 0;
        foreach (bool managed in new[] { false, true })
        {
            foreach (int block in new[] { 1, 127, 256, 257, 511, 1024 })
            {
                using var p = new Probe(managed);
                p.Publish(1); p.Control(1);
                var output = new float[32768]; var buffer = new float[block];
                for (int at = 0; at < output.Length; at += block)
                {
                    int count = Math.Min(block, output.Length - at);
                    p.Render(buffer, count); Array.Copy(buffer, 0, output, at, count);
                }
                var e = Native.r0_engine(p.Handle);
                Require(e->Clock == 32768 && e->Position == 0, "Sample clock/loop drift");
                Require(e->TraceCount == 32 * Sequence.Events.Length, "Event count");
                int ti = 0;
                // Oracle is the declared sequence repeated at absolute positions, independent of block traversal.
                for (int loop = 0; loop < 32; ++loop)
                    foreach (var ev in Sequence.Events)
                    {
                        var trace = e->Trace[ti++];
                        Require(trace.Frame == (ulong)(loop * 1024 + ev.Frame) && trace.Kind == ev.Kind && trace.Voice == ev.Voice,
                            "Absolute event position/order differs from sequence oracle");
                    }
                reference ??= output;
                for (int i = 0; i < output.Length; ++i)
                {
                    Require(float.IsFinite(output[i]), "Non-finite output");
                    maximumError = Math.Max(maximumError, Math.Abs(output[i] - reference[i]));
                }
                Require(maximumError <= 1e-6f, "Block/candidate audio mismatch");
                Require(p.CallbackAllocations == 0, "Processor allocated on callback thread");
                ++cases;
            }
            CheckControlsAndRecovery(managed);
            CheckPublication(managed);
        }
        Console.WriteLine($"VERIFY: {cases} schedule/audio cases, 32 loops each, max_abs_error={maximumError:R}; ABI/control/skip/publication checks PASS");
        Require(Native.r0_live_probes() == 0, "Controlled probe leak");
    }
    private static void CheckControlsAndRecovery(bool managed)
    {
        using var p = new Probe(managed);
        p.Publish(1); p.Control(1); var buffer = new float[4096];
        p.Render(buffer, 300);
        Native.r0_skip(p.Handle, 500);
        p.Render(buffer, 1);
        Require(p.Statistics.SkippedEvents == 4 && p.Statistics.Voices == 0, "Skipped-time release ownership");
        p.Control(2);
        for (int i = 0; i < 50000; ++i) p.Control(5, (i & 1) * 0.5f);
        p.Render(buffer, 64);
        Require(p.Statistics.Voices == 0 && p.Statistics.Playing == 0, "Stop lost under parameter burst");
        Require(buffer.Take(64).All(x => x == 0), "Stop did not settle");
        p.Control(1); p.Render(buffer, 100); p.Control(3); p.Render(buffer, 32);
        Require(p.Statistics.Voices == 0, "Release lost");
        p.Control(4); p.Control(1); p.Render(buffer, 1);
        Require(p.Statistics.Voices == 1, "Restart after panic failed");
        p.Control(4); p.Render(buffer, 1);
        Require(p.Statistics.Voices == 0 && p.Statistics.Playing == 0, "Panic failed");
        ulong before = Native.r0_engine(p.Handle)->Clock;
        Native.r0_skip(p.Handle, 10000000000UL); p.Render(buffer, 1);
        Require(Native.r0_engine(p.Handle)->Clock == before + 10000000001UL, "64-bit clock failed");
        // Independent expectation at a large musical position, including loop rollover and exact trace offsets.
        p.Control(1); p.Render(buffer, 1);
        Native.r0_skip(p.Handle, 10000000000UL);
        var e = Native.r0_engine(p.Handle);
        Require(e->Position == 1, "Long-run loop rounding drift");
        int oldTrace = e->TraceCount; ulong origin = e->Clock;
        p.Render(buffer, 1024);
        Require(e->Trace[oldTrace].Frame == origin + 254 && e->Trace[oldTrace].Kind == 2, "Long-run event drift");
    }
    private static void CheckPublication(bool managed)
    {
        using var p = new Probe(managed);
        p.Publish(1); p.Control(1); var buffer = new float[64]; p.Render(buffer, 64);
        for (int g = 2; g <= 4; ++g) p.Publish(g);
        Native.r0_request(p.Handle, 5);
        Require(p.TryPublish(5, Sequence.Events, Sequence.Table) == -3, "Slot capacity not bounded");
        Require(p.TryPublish(4, Sequence.Events, Sequence.Table) == -1, "Stale preparation accepted");
        p.Render(buffer, 64); // Pending generation 4 is now stale and must retire, not become active.
        Require(p.Statistics.ActiveGeneration == 1 && p.Statistics.LiveSlots == 4, "Stale publication/lifetime");
        Require(Native.r0_reclaim(p.Handle) == 3, "Retirement count");
        Require(p.TryPublish(5, Sequence.Events, Sequence.Table) == 0, "Valid preparation did not recover");
        p.Render(buffer, 64);
        Require(p.Statistics.ActiveGeneration == 5 && Native.r0_reclaim(p.Handle) == 1, "Latest-state convergence");
        var invalid = new[] { new MusicalEvent(0, 1, 99, 1) };
        Require(p.TryPublish(5, invalid, Sequence.Table) == -2, "Invalid voice accepted");
        var badTable = (float[])Sequence.Table.Clone(); badTable[0] = float.NaN;
        Require(p.TryPublish(5, Sequence.Events, badTable) == -2, "Invalid prepared resource accepted");
        // Published storage must own a copy, not borrow a subsequently mutable preparation array.
        var table = (float[])Sequence.Table.Clone(); p.Publish(6, table: table); Array.Fill(table, float.NaN);
        p.Control(2); p.Render(buffer, 64); p.Control(1); p.Render(buffer, 64);
        Require(buffer.All(float.IsFinite), "Mutable preparation leaked into callback");
    }
    internal static object CompareDeviceCapture(Probe actual, int channels)
    {
        // Only baseline with no skipped time qualifies. Capture starts at prefill and stores bounded output.
        var ae = Native.r0_engine(actual.Handle);
        using var offline = new Probe(false); offline.Publish(1); offline.Control(1);
        int frames = ae->CaptureCount / channels;
        var output = new float[frames * channels];
        int at = 0;
        fixed (float* op = output)
        {
            int prefill = Math.Min(actual.Statistics.BufferFrames, frames);
            Native.r0_process(offline.Handle, op, prefill, channels); at += prefill;
            var timing = Native.r0_timings(actual.Handle);
            for (int i = 0; at < frames && i < actual.Statistics.Callbacks; ++i)
            {
                int count = Math.Min(timing[i].Frames, frames - at);
                Native.r0_process(offline.Handle, op + at * channels, count, channels); at += count;
            }
        }
        Require(at == frames, "Captured packet partitions incomplete");
        float error = 0;
        for (int i = 0; i < output.Length; ++i) error = Math.Max(error, Math.Abs(output[i] - ae->Capture[i]));
        var oe = Native.r0_engine(offline.Handle); int events = 0;
        while (events < ae->TraceCount && ae->Trace[events].Frame < (ulong)frames)
        {
            Require(events < oe->TraceCount, "Device trace exceeds offline trace");
            var a = ae->Trace[events]; var b = oe->Trace[events];
            Require(a.Frame == b.Frame && a.Kind == b.Kind && a.Voice == b.Voice, "Device/offline event mismatch"); ++events;
        }
        Require(events == oe->TraceCount && error <= 1e-6f, "Device/offline mismatch");
        return new { frames, channels, events, maxAbsError = error };
    }
}
