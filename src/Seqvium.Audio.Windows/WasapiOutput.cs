// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Seqvium.Core;
using static Seqvium.Audio.Windows.WasapiNative;

namespace Seqvium.Audio.Windows;

public sealed record OutputFacts(
    string Endpoint,
    int Rate,
    int Channels,
    int Bits,
    int CapacityFrames,
    long Period100Nanoseconds,
    ulong ClockFrequency,
    long StreamLatency100Nanoseconds,
    bool Mmcss);

public readonly record struct ServiceObservation(
    long ProcessorTicks,
    long ServiceTicks,
    long WakeTicks,
    int Frames,
    int Padding,
    ulong ClockPosition,
    ulong ClockQpc100Nanoseconds,
    long SubmittedFrames);

public enum OutputFailure
{
    None,
    Native,
    Unsupported,
    Starvation,
    ClockDiscontinuity,
    WaitTimeout,
    InvalidPacket,
    InstrumentationFull,
    EndpointUnavailable,
    InvalidEndpoint
}

/// <summary>Explicit Windows shared/event-driven output lifetime. One dedicated managed worker owns all COM/resources.
/// Physical tests opt in. The caller retains the sampler until Close joins. No canonical data enters this adapter.</summary>
[SupportedOSPlatform("windows")]
public sealed unsafe class WasapiOutput : IAudioOutputLifetime
{
    private readonly Thread _worker;
    private readonly ManualResetEventSlim _ready = new(false), _started = new(false);
    private readonly nint _quit, _activate;
    private RealtimeSampler? _sampler;
    private bool _disposed, _startRequested;
    private int _injectStall, _injectFailure;
    private readonly float[] _capture;
    private readonly bool _diagnostics;
    private readonly ServiceObservation[] _observations;
    private int _count, _captureCount, _capturePackets;
    private readonly int[] _partitions;
    public bool DiagnosticsEnabled => _diagnostics;

    public int DiagnosticStorageBytes => _capture.Length * sizeof(float) + _partitions.Length * sizeof(int) +
                                         _observations.Length * sizeof(ServiceObservation);

    public OutputFacts? Facts { get; private set; }
    public OutputFailure Failure { get; private set; }
    public int HResult { get; private set; }
    public string? UnexpectedError { get; private set; }
    public bool Joined { get; private set; }
    public long ProcessorAllocatedBytes { get; private set; }
    public long ServiceAllocatedBytes { get; private set; }
    public long WorkerAllocatedBytes { get; private set; }
    public float MaximumSampleMagnitude { get; private set; }
    public float MaximumAdjacentSampleDelta { get; private set; }
    public float MaximumPacketBoundaryDelta { get; private set; }
    private float _previousLeft, _previousRight;
    public int ProcessorMisses { get; private set; }
    public int ServiceMisses { get; private set; }
    public int PacketMisses { get; private set; }
    public int PaddingExhaustions { get; private set; }
    public int SilentPackets { get; private set; }
    public int SilentPacketsAfterStop { get; private set; }
    public int NonFiniteSamples { get; private set; }
    public int MaximumObservedVoices { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public int CallbackCount => Volatile.Read(ref _count);
    public ReadOnlySpan<ServiceObservation> Observations => _observations.AsSpan(0, _diagnostics ? _count : 0);
    public ReadOnlySpan<float> Capture => _capture.AsSpan(0, _captureCount);
    public ReadOnlySpan<int> CapturePartitions => _partitions.AsSpan(0, _capturePackets);

    public WasapiOutput(int captureSamples = 0, string? endpointId = null, bool diagnostics = false)
        : this(endpointId is null ? AudioEndpointIntent.Default() : AudioEndpointIntent.Explicit(endpointId),
            captureSamples, diagnostics)
    {
    }

    public WasapiOutput(AudioEndpointIntent selection, int captureSamples = 0, bool diagnostics = false)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("Windows x64 output only.");
        if (captureSamples is < 0 or > 8388608) throw new ArgumentOutOfRangeException(nameof(captureSamples));
        if (!diagnostics && captureSamples != 0)
            throw new ArgumentException("PCM capture requires explicit diagnostics.", nameof(captureSamples));
        _diagnostics = diagnostics;
        _capture = new float[captureSamples];
        _observations = diagnostics ? new ServiceObservation[20000] : [];
        _partitions = captureSamples == 0 ? [] : new int[20001];
        _quit = CreateEventW(0, 1, 0, null);
        _activate = CreateEventW(0, 0, 0, null);
        if (_quit == 0 || _activate == 0)
        {
            if (_quit != 0) CloseHandle(_quit);
            if (_activate != 0) CloseHandle(_activate);
            throw new InvalidOperationException($"CreateEvent: {Marshal.GetLastPInvokeError()}");
        }

        _worker = new Thread(() => Run(selection)) { IsBackground = true, Name = "Seqvium WASAPI output" };
        _worker.Start();
        if (!_ready.Wait(TimeSpan.FromSeconds(5)))
        {
            Close();
            throw new TimeoutException("WASAPI Open timeout.");
        }
    }

    public void Start(RealtimeSampler sampler)
    {
        ObjectDisposedException.ThrowIf(_disposed || Joined, this);
        if (_startRequested || Failure != OutputFailure.None || Facts is not { } facts)
            throw new InvalidOperationException("Output is not ready for Start.");
        if (sampler.SampleRate != facts.Rate || sampler.Channels != facts.Channels ||
            sampler.MaximumPacketFrames < facts.CapacityFrames)
            throw new ArgumentException("Sampler must match negotiated facts.", nameof(sampler));
        _startRequested = true;
        Volatile.Write(ref _sampler, sampler);
        SetEvent(_activate);
        if (!_started.Wait(TimeSpan.FromSeconds(5)))
        {
            Close();
            throw new TimeoutException("WASAPI Start timeout.");
        }

        if (Failure != OutputFailure.None)
        {
            Close();
            throw new InvalidOperationException($"WASAPI Start: {Failure}, 0x{HResult:X8}");
        }
    }

    public void InjectWorkerStall() => Volatile.Write(ref _injectStall, 1);
    public void InjectDeviceInvalidation() => Volatile.Write(ref _injectFailure, 1);

    public void Close()
    {
        if (Joined) return;
        SetEvent(_quit);
        if (!_worker.Join(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("WASAPI worker has not joined; resources remain owned.");
        Joined = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Close();
        _disposed = true;
        CloseHandle(_activate);
        CloseHandle(_quit);
        _ready.Dispose();
        _started.Dispose();
    }

    private bool Check(int hr)
    {
        if (hr >= 0) return true;
        HResult = hr;
        Failure = OutputFailure.Native;
        return false;
    }

    private void Run(AudioEndpointIntent selection)
    {
        nint enumerator = 0,
            device = 0,
            client = 0,
            render = 0,
            clock = 0,
            format = 0,
            endpoint = 0,
            audioEvent = 0,
            priority = 0;
        bool initialized = false, streaming = false;
        long first = 0, workerAllocation = 0;
        try
        {
            if (!Check(CoInitializeEx(0, 0))) return;
            initialized = true;
            if (!Check(CoCreateInstance(EnumeratorClass, 0, 23, EnumeratorInterface, out enumerator))) return;
            if (selection.FollowsDefault)
            {
                if (!Check(((delegate* unmanaged[Stdcall]<nint, int, int, nint*, int>)Method(enumerator, 4))(enumerator,
                        0, (int)selection.Role, &device))) return;
            }
            else
                fixed (char* id = selection.EndpointId)
                    if (!Check(((delegate* unmanaged[Stdcall]<nint, char*, nint*, int>)Method(enumerator, 5))(
                            enumerator, id, &device)))
                        return;

            uint state = 0;
            int direction = -1;
            if (!Check(DeviceState(device, &state)) || !Check(DeviceDirection(device, &direction))) return;
            if (direction != 0)
            {
                Failure = OutputFailure.InvalidEndpoint;
                return;
            }

            if (state != 1)
            {
                Failure = OutputFailure.EndpointUnavailable;
                return;
            }

            if (!Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(device, 5))(device, &endpoint))) return;
            Guid clientId = ClientInterface;
            if (!Check(((delegate* unmanaged[Stdcall]<nint, Guid*, uint, nint, nint*, int>)Method(device, 3))(device,
                    &clientId, 23, 0, &client))) return;
            if (!Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(client, 8))(client, &format))) return;
            byte* f = (byte*)format;
            int tag = *(ushort*)f, channels = *(ushort*)(f + 2), rate = *(int*)(f + 4), bits = *(ushort*)(f + 14);
            bool floating = tag == 3 || tag == 0xfffe && *(ushort*)(f + 16) >= 22 && *(Guid*)(f + 24) == FloatSubtype;
            uint mask = tag == 0xfffe ? *(uint*)(f + 20) : 0;
            if (!floating || bits != 32 || channels is not (1 or 2) || rate is not (44100 or 48000) ||
                *(ushort*)(f + 12) != channels * 4 || tag == 0xfffe && (*(ushort*)(f + 18) != 32 ||
                                                                        !(mask == 0 || channels == 1 && mask == 4 ||
                                                                          channels == 2 && mask == 3)))
            {
                Failure = OutputFailure.Unsupported;
                HResult = unchecked((int)0x88890008);
                return;
            }

            long period = 0, minimum = 0, latency = 0;
            if (!Check(((delegate* unmanaged[Stdcall]<nint, long*, long*, int>)Method(client, 9))(client, &period,
                    &minimum))) return;
            if (!Check(((delegate* unmanaged[Stdcall]<nint, int, uint, long, long, nint, nint, int>)Method(client, 3))(
                    client, 0, 0x40000, 0, 0, format, 0))) return;
            uint capacity = 0;
            if (!Check(((delegate* unmanaged[Stdcall]<nint, uint*, int>)Method(client, 4))(client, &capacity))) return;
            if (capacity is < 1 or > 65536 || period <= 0)
            {
                Failure = OutputFailure.Unsupported;
                return;
            }

            Guid renderId = RenderInterface, clockId = ClockInterface;
            if (!Check(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Method(client, 14))(client, &renderId,
                    &render))) return;
            if (!Check(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Method(client, 14))(client, &clockId,
                    &clock))) return;
            ulong clockFrequency = 0;
            if (!Check(((delegate* unmanaged[Stdcall]<nint, ulong*, int>)Method(clock, 3))(clock, &clockFrequency)))
                return;
            if (clockFrequency == 0)
            {
                Failure = OutputFailure.ClockDiscontinuity;
                return;
            }

            if (!Check(((delegate* unmanaged[Stdcall]<nint, long*, int>)Method(client, 5))(client, &latency))) return;
            audioEvent = CreateEventW(0, 0, 0, null);
            if (audioEvent == 0)
            {
                Check(unchecked((int)(0x80070000 | Marshal.GetLastPInvokeError())));
                return;
            }

            if (!Check(((delegate* unmanaged[Stdcall]<nint, nint, int>)Method(client, 13))(client, audioEvent))) return;
            priority = AvSetMmThreadCharacteristicsW("Pro Audio", out _);
            Facts = new(Marshal.PtrToStringUni(endpoint) ?? "Unknown", rate, channels, bits, (int)capacity, period,
                clockFrequency, latency, priority != 0);
            _ready.Set();
            nint* waits = stackalloc nint[2];
            waits[0] = _quit;
            waits[1] = _activate;
            uint wait = WaitForMultipleObjects(2, waits, 0, 30000);
            if (wait == 0) return;
            if (wait != 1)
            {
                Failure = OutputFailure.WaitTimeout;
                return;
            }

            var sampler = Volatile.Read(ref _sampler)!;
            // Warm the same worker/runtime before the measurement boundary, without advancing music.
            sampler.Process(Span<float>.Empty);
            nint buffer = 0;
            if (!Check(GetBuffer(render, capacity, &buffer))) return;
            var prefill = new Span<float>((void*)buffer, (int)capacity * channels);
            if (!sampler.Process(prefill))
            {
                ReleaseBuffer(render, capacity, 2);
                Failure = OutputFailure.InvalidPacket;
                return;
            }

            CapturePacket(prefill, (int)capacity);
            if (_diagnostics) ObservePacket(prefill, sampler);
            if (!Check(ReleaseBuffer(render, capacity, 0))) return;
            if (!Check(Simple(client, 10))) return;
            streaming = true;
            first = Stopwatch.GetTimestamp();
            long previous = first;
            if (_diagnostics) workerAllocation = GC.GetAllocatedBytesForCurrentThread();
            _started.Set();
            waits[1] = audioEvent;
            ulong previousClock = 0;
            long lastClockAdvance = first;
            long submitted = capacity;
            double periodTicks = period / 10000000.0 * Stopwatch.Frequency;
            while (true)
            {
                wait = WaitForMultipleObjects(2, waits, 0, 2000);
                if (wait == 0) break;
                if (wait != 1)
                {
                    Failure = OutputFailure.WaitTimeout;
                    break;
                }

                long serviceStart = Stopwatch.GetTimestamp(),
                    serviceAllocated = _diagnostics ? GC.GetAllocatedBytesForCurrentThread() : 0;
                if (Interlocked.Exchange(ref _injectStall, 0) != 0)
                {
                    long until = serviceStart + Stopwatch.Frequency * 30 / 1000;
                    while (Stopwatch.GetTimestamp() < until) Thread.SpinWait(16);
                }

                if (Interlocked.Exchange(ref _injectFailure, 0) != 0)
                {
                    Check(unchecked((int)0x88890004));
                    break;
                }

                uint padding = 0;
                ulong position = 0, qpc = 0;
                if (!Check(Padding(client, &padding)) || !Check(Position(clock, &position, &qpc))) break;
                if (position < previousClock || Stopwatch.GetTimestamp() - lastClockAdvance > periodTicks * 5 &&
                    position == previousClock)
                {
                    Failure = OutputFailure.ClockDiscontinuity;
                    break;
                }

                if (position != previousClock) lastClockAdvance = Stopwatch.GetTimestamp();
                previousClock = position;
                if (padding > capacity)
                {
                    Failure = OutputFailure.InvalidPacket;
                    break;
                }

                int frames = (int)(capacity - padding);
                long processorTicks = 0;
                if (padding == 0)
                {
                    PaddingExhaustions++;
                    Failure = OutputFailure.Starvation;
                }

                if (Failure == OutputFailure.None && frames != 0)
                {
                    if (!Check(GetBuffer(render, (uint)frames, &buffer))) break;
                    var output = new Span<float>((void*)buffer, frames * channels);
                    long processorStart = _diagnostics ? Stopwatch.GetTimestamp() : 0,
                        allocated = _diagnostics ? GC.GetAllocatedBytesForCurrentThread() : 0;
                    bool valid = sampler.Process(output);
                    if (_diagnostics)
                    {
                        ProcessorAllocatedBytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
                        processorTicks = Stopwatch.GetTimestamp() - processorStart;
                    }

                    CapturePacket(output, frames);
                    if (_diagnostics) ObservePacket(output, sampler);
                    if (!Check(ReleaseBuffer(render, (uint)frames, valid ? 0u : 2u))) break;
                    if (!valid)
                    {
                        Failure = OutputFailure.InvalidPacket;
                        break;
                    }

                    submitted += frames;
                }

                long end = Stopwatch.GetTimestamp(), serviceTicks = end - serviceStart;
                int index = _count;
                if (_diagnostics)
                {
                    ServiceAllocatedBytes += GC.GetAllocatedBytesForCurrentThread() - serviceAllocated;
                    if (processorTicks > periodTicks) ProcessorMisses++;
                    if (serviceTicks > periodTicks) ServiceMisses++;
                    if (frames > 0 && serviceTicks > (double)frames / rate * Stopwatch.Frequency) PacketMisses++;
                    if (index == _observations.Length)
                    {
                        Failure = OutputFailure.InstrumentationFull;
                        break;
                    }

                    _observations[index] = new(processorTicks, serviceTicks, serviceStart - previous, frames,
                        (int)padding,
                        position, qpc, submitted);
                }

                Volatile.Write(ref _count, index == int.MaxValue ? index : index + 1);
                previous = serviceStart;
                if (Failure != OutputFailure.None) break;
            }

            if (_diagnostics) WorkerAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - workerAllocation;
        }
        catch (Exception error)
        {
            // Exceptional adapter/runtime faults only; HRESULTs use explicit paths above.
            HResult = error.HResult;
            Failure = OutputFailure.Native;
            UnexpectedError = error.ToString();
        }
        finally
        {
            if (first != 0) ElapsedSeconds = (Stopwatch.GetTimestamp() - first) / (double)Stopwatch.Frequency;
            // Never invent a packet acknowledgment during shutdown or fault cleanup.
            Volatile.Read(ref _sampler)?.TerminateExecution();
            if (streaming)
            {
                int hr = Simple(client, 11);
                if (hr < 0 && HResult >= 0) Check(hr);
                hr = Simple(client, 12);
                if (hr < 0 && HResult >= 0) Check(hr);
            }

            if (priority != 0) AvRevertMmThreadCharacteristics(priority);
            Release(ref clock);
            Release(ref render);
            Release(ref client);
            Release(ref device);
            Release(ref enumerator);
            if (format != 0) CoTaskMemFree(format);
            if (endpoint != 0) CoTaskMemFree(endpoint);
            if (audioEvent != 0) CloseHandle(audioEvent);
            if (initialized) CoUninitialize();
            _ready.Set();
            _started.Set();
        }
    }

    private void ObservePacket(ReadOnlySpan<float> output, RealtimeSampler sampler)
    {
        bool silent = true;
        for (int index = 0; index < output.Length; index++)
        {
            float sample = output[index];
            silent &= sample == 0;
            if (!float.IsFinite(sample)) NonFiniteSamples++;
            bool left = sampler.Channels == 1 || index % 2 == 0;
            float difference = Math.Abs(sample - (left ? _previousLeft : _previousRight));
            MaximumSampleMagnitude = Math.Max(MaximumSampleMagnitude, Math.Abs(sample));
            MaximumAdjacentSampleDelta = Math.Max(MaximumAdjacentSampleDelta, difference);
            if (index < sampler.Channels) MaximumPacketBoundaryDelta = Math.Max(MaximumPacketBoundaryDelta, difference);
            if (left) _previousLeft = sample; else _previousRight = sample;
        }

        MaximumObservedVoices = Math.Max(MaximumObservedVoices, sampler.ActiveVoices);
        if (silent)
        {
            SilentPackets++;
            if (sampler.StopAcknowledgment > 0) SilentPacketsAfterStop++;
        }
    }

    private void CapturePacket(ReadOnlySpan<float> output, int frames)
    {
        if (_captureCount == _capture.Length || _capturePackets == _partitions.Length) return;
        int samples = Math.Min(output.Length, _capture.Length - _captureCount);
        samples -= samples % (Facts?.Channels ?? 1);
        output[..samples].CopyTo(_capture.AsSpan(_captureCount));
        _captureCount += samples;
        _partitions[_capturePackets++] = samples / (Facts?.Channels ?? 1);
    }
}
