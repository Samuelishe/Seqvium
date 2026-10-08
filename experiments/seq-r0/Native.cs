// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;

namespace Seqvium.R0;

[StructLayout(LayoutKind.Sequential)]
internal struct MusicalEvent(int frame, int kind, int voice, float value)
{
    public int Frame = frame, Kind = kind, Voice = voice;
    public float Value = value;
}
[StructLayout(LayoutKind.Sequential)]
internal struct EventTrace
{
    public ulong Frame;
    public int Kind, Voice;
}
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Engine
{
    public ulong Clock;
    public int Position, EventIndex, Playing;
    public float Gain;
    public MusicalEvent* Events;
    public float* Table;
    public int EventCount, LoopFrames;
    public fixed int Phases[8], Steps[8], Releases[8];
    public fixed float Amplitudes[8];
    public EventTrace* Trace;
    public int TraceCount, TraceCapacity;
    public float* Capture;
    public int CaptureCount, CaptureCapacity;
}
[StructLayout(LayoutKind.Sequential)]
internal struct Timing
{
    public double ProcessingUs, ServiceUs, IntervalUs;
    public int Frames, Padding;
    public ulong Clock, DeviceFrame;
}
[StructLayout(LayoutKind.Sequential)]
internal struct Stats
{
    public int Rate, Channels, BufferFrames, PeriodFrames, Hr, Callbacks, PaddingEmpty, TimingFull;
    public ulong SkippedFrames, SkippedEvents, ClockRegressions, DeviceFrame, SubmittedFrame;
    public double ElapsedSeconds, QpcPairUs;
    public int Publishes, Rejected, Superseded, Retired, ActiveGeneration, LiveSlots;
    public int Voices, Playing, TraceCount, CaptureCount, Mmcss;
    public ulong ClockFrequency;
    public double StreamLatencyMs;
    public int AcknowledgedState, AcknowledgedGeneration;
    public long MaximumWallDeficit, MaximumPostRecoveryDeficit, FinalWallDeficit;
    public int Recoveries;
}
internal static unsafe class Native
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void Processor(Engine* engine, float* output, int frames, int channels);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern int r0_size(int which);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern int r0_live_probes();
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern double r0_empty_calls(nint probe, int count);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern nint r0_create(nint processor);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern void r0_request(nint probe, int generation);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern int r0_publish(nint probe, int generation, MusicalEvent* events, int count, int loop, float* table);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern int r0_reclaim(nint probe);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern void r0_control(nint probe, int kind, float value);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern void r0_process(nint probe, float* output, int frames, int channels);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern void r0_skip(nint probe, ulong frames);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern Engine* r0_engine(nint probe);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern Timing* r0_timings(nint probe);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern nint r0_endpoint(nint probe);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern void r0_stats(nint probe, out Stats stats);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern int r0_open(nint probe, int seconds, int inject);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern int r0_simulate(nint probe, int seconds, int frames, int inject);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern int r0_join(nint probe, int cancel);
    [DllImport("r0", CallingConvention = CallingConvention.Cdecl)] internal static extern int r0_destroy(nint probe);
}

internal sealed unsafe class Probe : IDisposable
{
    private readonly Native.Processor? processor;
    private GCHandle callbackRoot;
    internal nint Handle { get; private set; }
    internal long CallbackAllocations;
    internal long CallbackCalls;
    internal Probe(bool managed)
    {
        processor = managed ? Process : null;
        if (processor is not null) callbackRoot = GCHandle.Alloc(processor);
        Handle = Native.r0_create(processor is null ? 0 : Marshal.GetFunctionPointerForDelegate(processor));
        if (Handle == 0)
        {
            if (callbackRoot.IsAllocated) callbackRoot.Free();
            throw new OutOfMemoryException();
        }
    }
    private void Process(Engine* engine, float* output, int frames, int channels)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        ManagedProcessor.Render(engine, output, frames, channels);
        CallbackAllocations += GC.GetAllocatedBytesForCurrentThread() - before;
        ++CallbackCalls;
    }
    internal Stats Statistics { get { Native.r0_stats(Handle, out var s); return s; } }
    internal void Publish(int generation, MusicalEvent[]? events = null, float[]? table = null)
    {
        events ??= Sequence.Events; table ??= Sequence.Table;
        Native.r0_request(Handle, generation);
        if (TryPublish(generation, events, table) != 0) throw new InvalidOperationException("Publication failed");
    }
    internal int TryPublish(int generation, MusicalEvent[] events, float[] table)
    {
        fixed (MusicalEvent* ep = events)
        fixed (float* tp = table)
            return Native.r0_publish(Handle, generation, ep, events.Length, 1024, tp);
    }
    internal void Control(int kind, float value = 0) => Native.r0_control(Handle, kind, value);
    internal void Render(float[] output, int frames, int channels = 1)
    {
        if (frames < 0 || frames > output.Length / channels) throw new ArgumentOutOfRangeException(nameof(frames));
        fixed (float* op = output) Native.r0_process(Handle, op, frames, channels);
    }
    public void Dispose()
    {
        if (Handle == 0) return;
        if (Native.r0_destroy(Handle) != 0) throw new InvalidOperationException("Worker join failed; state retained");
        Handle = 0; GC.KeepAlive(processor);
        if (callbackRoot.IsAllocated) callbackRoot.Free();
    }
}
