# Bounded SEQ-R0 audio probe

This standalone experiment compares managed C# and native C scheduling/DSP through one Windows
WASAPI shared event-driven worker. It is outside `Seqvium.sln` and supplies no production assemblies.
See the [protocol](../../docs/experiments/SEQ-R0_PROTOCOL.md) and
[report](../../docs/experiments/SEQ-R0_REPORT.md). Seqvium-authored source and generated sequence/table
use the root Apache-2.0 license; no external audio/content is used.

## Build and run

From this directory, using PowerShell 7, .NET SDK from [global.json](global.json), and installed MSYS2
UCRT64 GCC 16.2.0 on PATH (including that installation's `share/licenses`):

```powershell
./build.ps1
dotnet bin/Release/net10.0/Probe.dll verify
dotnet bin/Release/net10.0/Probe.dll device 10
dotnet bin/Release/net10.0/Probe.dll device 10 reverse
dotnet bin/Release/net10.0/Probe.dll controlled 10
dotnet bin/Release/net10.0/Probe.dll lifetime
```

`build.ps1` builds Release C# and an optimized C DLL with warnings as errors, then copies installed
MinGW-w64 runtime/GCC license notices beside output. It does not install tools. Exact native options
and compiler requirement are script-owned; exact SDK is experiment-local. No NuGet audio/test package,
CMake, global SDK pin or production solution change is introduced. Build/run needs the native DLL
beside the managed output. `verify` loads that DLL but never opens an audio device.

`device` emits low-amplitude generated tones to the default output; `controlled` uses a native timer
and no output device. Both run baseline and pressure for the requested 1–30 seconds and overload for
5 seconds. Overload deliberately stalls the worker for 30 ms every 100 serviced packets. It ends with
Stop/Release/Panic and checks acknowledgment before device shutdown. Device failures print HRESULT;
an unavailable configuration is a result, not device acceptance. Logical assertion failures exit 1;
unavailable device runs may exit 0 after reporting their unavailable configurations—inspect the output.
No volume/device/system setting is changed.

`lifetime` exercises ten fresh initialize/start/replace/stop/cancel/close/dispose instances per
candidate. A Probe supports one device/timer lifetime; concurrent open and same-instance reopen are
explicitly rejected. Reinitialize by disposing and creating a fresh Probe. Playback transport can
stop/restart within an open device. Close joins before freeing native state and unrooting the managed
delegate; if joining fails, state and delegate root are retained rather than freed under a worker.

## Source and ownership

- `native/kernel.h`, `kernel.c`: device-independent fixed execution state and native scheduler/DSP.
- `ManagedProcessor.cs`: equivalent C# execution and project-authored triangle/sequence recipe.
- `native/probe.h`, `probe.c`: four immutable prepared slots, single publication producer, independent
  critical generations, coalesced gain, skip/release recovery, rooted callback/lifetime ABI and counters.
- `native/device.c`: Windows COM/WASAPI adapter, negotiated float32 format, period/padding/device clock,
  QPC recovery anchor, preallocated timing/capture storage, worker and shutdown.
- `native/controlled.c`: timer-driven comparison independent of device access.
- `Native.cs`: experiment-only interop layouts, ABI size checks, explicit native owner/delegate root.
- `Verification.cs`: sequence oracle, block/candidate/captured output comparisons and boundary assertions.
- `Program.cs`: non-realtime measurement reporting, bounded allocation/control/preparation pressure and
  lifetime checks. No callback logging/percentile calculation, mutable document/UI or resource allocation.

The preparation worker is the only concurrent publication producer. The control side may update
requested generation/transport/gain; it never reclaims a live active/pending slot. Publication copies
events and table into native-owned fixed storage. Callback-owned voices survive compatible table
replacement. Retired slots reclaim off callback. A stale prepared generation cannot activate.
Slot exhaustion rejects explicitly; no musical event FIFO is silently overflowing.

Counters and capture are bounded: 65,536 timing records and trace records, 524,288 float samples;
full capture stops recording, not musical execution. No WAV writer or asset files are created.
Stats and unsafe execution pointers are inspected after worker join, except single-thread controlled
verification. A callback publishes separate atomic state acknowledgments; shutdown cleanup cannot
masquerade as successful musical Stop/Panic convergence.

Keep generated output/logs in ignored `bin/`:

```powershell
dotnet bin/Release/net10.0/Probe.dll device 10 > bin/device.log
```

This is source/developer-runtime evidence, not an installer or supported product distribution. The
native adapter is Windows-only; cross-platform engine/backend decisions and packaging remain open.
