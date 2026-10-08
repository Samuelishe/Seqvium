// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Seqvium.Core;
using static Seqvium.Audio.Windows.WasapiNative;

namespace Seqvium.Audio.Windows;

/// <summary>Blocking control-side snapshot. Does not activate IAudioClient, capture, or change OS preferences.
/// The calling apartment owns every COM reference/property allocation until this method returns.</summary>
[SupportedOSPlatform("windows")]
public static unsafe class WindowsAudioEndpoints
{
    public static AudioEndpointSnapshot Discover()
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("Windows x64 endpoint discovery only.");
        int initialization = CoInitializeEx(0, 0);
        // A caller already initialized as STA may read MMDevice in that apartment; do not uninitialize it.
        if (initialization < 0 && initialization != unchecked((int)0x80010106))
            Marshal.ThrowExceptionForHR(initialization);
        nint enumerator = 0;
        try
        {
            Marshal.ThrowExceptionForHR(CoCreateInstance(EnumeratorClass, 0, 23, EnumeratorInterface, out enumerator));
            var endpoints = new List<AudioEndpoint>();
            var defaults = new List<AudioEndpointDefault>();
            foreach (var direction in Enum.GetValues<AudioEndpointDirection>())
            {
                nint collection = 0;
                try
                {
                    Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, int, uint, nint*, int>)
                        Method(enumerator, 3))(enumerator, (int)direction, 15, &collection));
                    uint count = 0;
                    Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, uint*, int>)
                        Method(collection, 3))(collection, &count));
                    for (uint index = 0; index < count; index++)
                    {
                        nint device = 0;
                        try
                        {
                            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)
                                Method(collection, 4))(collection, index, &device));
                            uint state = 0;
                            Marshal.ThrowExceptionForHR(DeviceState(device, &state));
                            string id = ReadId(device);
                            var name = ReadName(device);
                            endpoints.Add(new(id, name.Value, direction, (AudioEndpointState)state, name.HResult));
                        }
                        finally
                        {
                            Release(ref device);
                        }
                    }
                }
                finally
                {
                    Release(ref collection);
                }

                foreach (var role in Enum.GetValues<AudioEndpointRole>())
                {
                    nint device = 0;
                    try
                    {
                        int hr = ((delegate* unmanaged[Stdcall]<nint, int, int, nint*, int>)Method(enumerator, 4))(
                            enumerator, (int)direction, (int)role, &device);
                        defaults.Add(new(direction, role, hr >= 0 ? ReadId(device) : null, hr,
                            QueryFailed: hr < 0 && hr != unchecked((int)0x80070490)));
                    }
                    finally
                    {
                        Release(ref device);
                    }
                }
            }

            return new(endpoints, defaults);
        }
        finally
        {
            Release(ref enumerator);
            if (initialization >= 0) CoUninitialize();
        }
    }

    private static string ReadId(nint device)
    {
        nint id = 0;
        try
        {
            Marshal.ThrowExceptionForHR(
                ((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(device, 5))(device, &id));
            return Marshal.PtrToStringUni(id) ?? throw new InvalidOperationException("Endpoint returned no ID.");
        }
        finally
        {
            if (id != 0) CoTaskMemFree(id);
        }
    }

    private static (string? Value, int HResult) ReadName(nint device)
    {
        nint properties = 0;
        PropertyVariant value = default;
        try
        {
            int hr = ((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Method(device, 4))(device, 0, &properties);
            if (hr < 0) return (null, hr);
            PropertyKey key = new() { Format = new("a45c254e-df1c-4efd-8020-67d146a850e0"), Id = 14 };
            hr = ((delegate* unmanaged[Stdcall]<nint, PropertyKey*, PropertyVariant*, int>)Method(properties, 5))(
                properties, &key, &value);
            return (hr >= 0 && value.Type == 31 ? Marshal.PtrToStringUni(value.Pointer) : null, hr);
        }
        finally
        {
            PropVariantClear(ref value);
            Release(ref properties);
        }
    }
}

[SupportedOSPlatform("windows")]
public sealed record SelectedOutputOpen(AudioEndpointResolution Resolution, WasapiOutput? Output)
{
    public bool IsOpen => Output is { Failure: OutputFailure.None, Facts: not null, Joined: false };
}

/// <summary>Control-side open/reopen only. Resolve fresh intent, then revalidate native state/direction
/// on the owning worker before activation. Opening never starts playback; prepare/start explicitly.</summary>
[SupportedOSPlatform("windows")]
public static class WasapiSelection
{
    public static SelectedOutputOpen OpenOutput(AudioDeviceSession owner, bool diagnostics = false,
        int captureSamples = 0, Func<AudioEndpointSnapshot>? discover = null)
    {
        owner.CloseOutput(); // Join/release the old lifetime before even resolving its replacement.
        var resolution = owner.ResolveOutput(discover ?? WindowsAudioEndpoints.Discover);
        if (!resolution.IsAvailable) return new(resolution, null);
        var output = new WasapiOutput(owner.OutputIntent, captureSamples, diagnostics);
        if (output.Failure != OutputFailure.None || output.Facts is not { } facts)
        {
            output.Dispose();
            return new(resolution, output); // Retain native failure facts, but no borrowing/resources.
        }

        var sampler = new RealtimeSampler(facts.Rate, facts.Channels, facts.CapacityFrames);
        try
        {
            owner.AttachOutput(output, sampler);
        }
        catch
        {
            output.Dispose();
            sampler.Dispose();
            throw;
        }

        return new(resolution, output);
    }
}
