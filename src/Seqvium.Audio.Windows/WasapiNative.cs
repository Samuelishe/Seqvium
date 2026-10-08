// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Seqvium.Audio.Windows;

/// <summary>OS COM vtables, not an engine ABI. Raw PreserveSig calls avoid RCWs and exception/interop allocations in service.</summary>
[SupportedOSPlatform("windows")]
internal static unsafe class WasapiNative
{
    internal static readonly Guid EnumeratorClass = new("bcde0395-e52f-467c-8e3d-c4579291692e");
    internal static readonly Guid EnumeratorInterface = new("a95664d2-9614-4f35-a746-de8db63617e6");
    internal static readonly Guid ClientInterface = new("1cb9ad4c-dbfa-4c32-b178-c2f568a703b2");
    internal static readonly Guid RenderInterface = new("f294acfc-3146-4483-a7bf-addca7c260e2");
    internal static readonly Guid ClockInterface = new("cd63314f-3fba-4a1b-812c-ef96358728e7");
    internal static readonly Guid EndpointInterface = new("1be09788-6894-4089-8586-9a2a6c265ac5");
    internal static readonly Guid FloatSubtype = new("00000003-0000-0010-8000-00aa00389b71");
    internal static void* Method(nint instance, int slot) => (void*)(*(nint**)instance)[slot];

    internal static void Release(ref nint instance)
    {
        if (instance == 0) return;
        ((delegate* unmanaged[Stdcall]<nint, uint>)Method(instance, 2))(instance);
        instance = 0;
    }

    internal static int Simple(nint instance, int slot) =>
        ((delegate* unmanaged[Stdcall]<nint, int>)Method(instance, slot))(instance);

    internal static int Padding(nint client, uint* padding) =>
        ((delegate* unmanaged[Stdcall]<nint, uint*, int>)Method(client, 6))(client, padding);

    internal static int Position(nint clock, ulong* position, ulong* qpc) =>
        ((delegate* unmanaged[Stdcall]<nint, ulong*, ulong*, int>)Method(clock, 4))(clock, position, qpc);

    internal static int GetBuffer(nint render, uint frames, nint* buffer) =>
        ((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)Method(render, 3))(render, frames, buffer);

    internal static int ReleaseBuffer(nint render, uint frames, uint flags) =>
        ((delegate* unmanaged[Stdcall]<nint, uint, uint, int>)Method(render, 4))(render, frames, flags);

    internal static int DeviceState(nint device, uint* state) =>
        ((delegate* unmanaged[Stdcall]<nint, uint*, int>)Method(device, 6))(device, state);

    internal static int DeviceDirection(nint device, int* direction)
    {
        nint endpoint = 0;
        Guid iid = EndpointInterface;
        try
        {
            int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Method(device, 0))(device, &iid,
                &endpoint);
            return hr < 0
                ? hr
                : ((delegate* unmanaged[Stdcall]<nint, int*, int>)Method(endpoint, 3))(endpoint, direction);
        }
        finally
        {
            Release(ref endpoint);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyKey
    {
        internal Guid Format;
        internal uint Id;
    }

    // Windows x64 PROPVARIANT: header at 0, union at 8, including its 16-byte counted-array arm.
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct PropertyVariant
    {
        [FieldOffset(0)] internal ushort Type;
        [FieldOffset(8)] internal nint Pointer;
    }

    [DllImport("ole32.dll")]
    internal static extern int PropVariantClear(ref PropertyVariant value);

    [DllImport("ole32.dll")]
    internal static extern int CoInitializeEx(nint reserved, uint mode);

    [DllImport("ole32.dll")]
    internal static extern void CoUninitialize();

    [DllImport("ole32.dll")]
    internal static extern int
        CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);

    [DllImport("ole32.dll")]
    internal static extern void CoTaskMemFree(nint memory);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern nint CreateEventW(nint attributes, int manual, int initial, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int SetEvent(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int CloseHandle(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint WaitForMultipleObjects(uint count, nint* handles, int all, uint milliseconds);

    [DllImport("avrt.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern nint AvSetMmThreadCharacteristicsW(string task, out uint index);

    [DllImport("avrt.dll")]
    internal static extern int AvRevertMmThreadCharacteristics(nint handle);
}
