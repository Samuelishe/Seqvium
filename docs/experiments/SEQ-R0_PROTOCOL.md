# SEQ-R0 experimental protocol

Role: Pre-measurement protocol for the bounded audio architecture probe.
Read when: Reproducing or interpreting SEQ-R0.
Authoritative for: Probe hypotheses, configurations, measurement and acceptance scope.
Not authoritative for: Production architecture, product latency targets or platform support.

Declared 2026-10-08 before implementing or collecting probe results. See the
[report](SEQ-R0_REPORT.md) for observations, including unavailable evidence.

## Question and alternatives

H1: A preallocated C# scheduling/DSP callback can meet the negotiated shared-mode period on this
Windows machine during baseline and bounded managed pressure. H2: Keeping scheduling/DSP in native
code reduces sensitivity to managed GC sufficiently to justify investigating that boundary.
H3: Fixed prepared-state slots, latest-wins parameters and sticky critical-control generations keep
work/memory bounded, permit safe retirement, and recover without obsolete musical backlog.
H4: The same bounded execution semantics work independently of an audio device.

Candidates reviewed: managed processing through direct platform interop; native processing through
the same adapter; WASAPI shared event-driven output; legacy waveOut; miniaudio; PortAudio.
WASAPI is selected **only as this Windows experiment's adapter**, because installed MinGW-w64 headers
support it and it exposes negotiated format, period, padding and device clock without a new audio
dependency. C is sufficient for the fixed workload; C++ and a C ABI are not preselected product choices.
waveOut provides a weaker modern timing boundary. miniaudio and PortAudio are plausible portable
adapters but would introduce source/build/notice work without improving this experiment's isolation.
No dependency is downloaded, vendored or globally installed.

The smallest fair comparison uses one native event-driven device worker with either a rooted C#
reverse-interop callback or a native C processor. This compares **managed versus native execution**,
not two independently implemented device stacks. Both use the same prepared sequence, float buffers,
worker priority, timing instrumentation and device. Ordinary workstation GC is the initial managed
configuration; changing latency mode or forbidding GC would answer a different question.

## Environment and supported configurations

Observed before coding: Windows 11 Pro 10.0.26300 x64; AMD Ryzen 3 5400U, 4 cores/8 logical processors;
.NET SDK 10.0.400 and 10.0.401, selected 10.0.401, runtime 10.0.12; PowerShell 7.6.6;
MSYS2 UCRT64 GCC 16.2.0-3, MinGW-w64 headers/CRT 14.0.0.r302.gd7f3c5201-1.
CMake 4.4.4 and Ninja are available but unnecessary. No MSVC/Clang is on PATH.
Windows sound-device inventory reports High Definition Audio, AMD High Definition Audio and AMD
Streaming Audio. Inventory is **not** evidence of an accessible active endpoint.

Source lives only in `experiments/seq-r0/`, outside the empty production solution. An experiment-local
SDK pin and build script own version/build settings. No test platform/packages are introduced:
verification is an executable assertion harness, not `dotnet test`.

Device runs require an accessible default output endpoint with float32 shared mix format and 1–8
channels; report negotiated rate/channels/period/capacity, endpoint identity and failures. Unsupported
formats fail explicitly. Offline checks require no endpoint. This implementation's adapter is win-x64;
Linux/macOS/device-loss/exclusive-mode/ASIO/recording are untested, with no release claim.

## Workload and ownership

A generated triangle wavetable, eight fixed voices, a small immutable ordered event list and a 1024-frame
loop exercise starts, 32-frame releases, same-frame ordering, frame 255/256/257 and 1023/0 boundaries.
Logical musical positions are integer sample frames at the declared host rate. A declared gain change
is included. No BPM conversion/tempo model, effects, graph, UI, assets or file export is implemented.
Verify additional non-dividing block sizes and long-running integer clock behavior.

One control/preparation owner, one callback owner, four preallocated prepared-state slots; one pending
publication may supersede another. Only the control side writes unpublished slots or reclaims retired
slots. The callback owns voices/cursors, borrows immutable active state and retires previous state at a
block boundary. Device close joins the worker before delegate/buffers/state can be destroyed.
Gain coalesces; stop/release/panic generations persist independently of disposable values. Musical
events remain ordered in prepared state. On a skipped-time recovery, terminate old voices and advance
to current sample time rather than replay missed note starts. Explicitly count skipped events/time.
This intentionally loses sound during overload; it does not promise full seek-state reconstruction.

## Measurements and runs

Release builds, no debugger. Warm both processors before measurement. Fixed storage records QPC
callback times; percentile calculation/logging happens after joining. Measure full render servicing
separately from processing-only duration where possible. Compare processing/service durations against
the negotiated engine period and packet frames/rate; these are conservative declared budgets, not
proof of actual DAC deadline. Record median/p95/p99/max, counts, seconds, misses, frame range, padding
exhaustion observations, HRESULT failures, device clock versus submitted frame clock and skipped time.
Do not relabel padding exhaustion as a driver-provided dropout counter; unavailable underrun/audio
quality evidence stays unavailable. Measure empty QPC-pair overhead and state instrumentation limits.

Per candidate: baseline 10 s; pressure 10 s; deliberate overload/recovery 5 s where device access works.
Pressure uses bounded retained allocation with explicit full GC, rapidly superseded parameters and
transport requests, prepared-state replacement, and one slow cancellable preparation worker whose
obsolete requests coalesce. Injection is declared and bounded, not hidden in baseline.
Reverse candidate order on a second paired run where practical. Short lifecycle cycles and repeat
open/close supplement timing runs. Record GC collections, callback-thread allocation where observable,
publication/rejection/coalescing counts, final stopped voices and shutdown outcome.

Device-independent checks compare exact event tuples (absolute frame, type, voice, ordering) and
audio within maximum absolute error 1e-6 for both processors and block partitions; assert starts,
releases, loop transitions, cancellation/stale publication, pool capacity, sticky critical controls,
invalid input and recovery after skipped time. Physical callback traces/output, if available, are
compared against an offline replay of the actual packet partitions for an uninterrupted sequence.

## Bounded acceptance declared before results

- Functional checks: exact discrete schedule, finite output, audio error <= 1e-6; no stuck voice after
  stop/release/panic and skipped-time recovery; no unbounded queue, invalid lifetime use or deadlock.
- Baseline: processing/service p99 below the negotiated period and report **every** observed miss;
  zero misses is desirable but does not establish glitch-free playback. Stress may miss deadlines:
  state remains bounded and converges to stop/panic by the next completed block plus 32 release frames.
- Four slots and one pending preparation/publication remain bounded under bursts. Latest valid
  prepared state converges after pressure ends; obsolete preparation cannot publish. Rejected
  capacity requests are observable, not silent musical event disposal.
- Device lifetime: at least five initialize/start/stop/close cycles per candidate when available,
  finite joined shutdown, no callbacks after close, all owned resources retired/disposed. Record
  handle/private-memory trends as observations, not proof of absence of all leaks.
- Native/managed recommendation requires comparable device/controlled conditions. No universal latency
  target, final backend, permanent ABI or complete DAW architecture is adopted from success.
- Without usable device execution or essential comparison, complete controlled checks and mark R0
  **partially evidenced**, with recommendation **investigate further** or a clearly bounded **narrow**.

API/candidate evidence: [WASAPI rendering](https://learn.microsoft.com/en-us/windows/win32/coreaudio/rendering-a-stream),
[event initialization](https://learn.microsoft.com/en-us/windows/win32/api/audioclient/nf-audioclient-iaudioclient-initialize),
[device clock](https://learn.microsoft.com/en-us/windows/win32/api/audioclient/nf-audioclient-iaudioclock-getposition),
[.NET GC latency](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/latency),
[miniaudio license](https://github.com/mackron/miniaudio/blob/master/LICENSE),
[PortAudio license](https://portaudio.com/license.html).

## Recovery-method amendment before the second device measurement series

The initial measured 30 ms starvation runs showed that this endpoint's reported IAudioClock position
falls behind elapsed QPC time through starvation. It therefore cannot alone establish recovery toward
current elapsed transport time. Keep that initial failure in the report. Before new measurements,
declare a callback-thread QPC/sample anchor with a padding adjustment: on exhausted padding, skip
positive elapsed-time deficit once and release old voices. Record deficit before/after recovery and
skip counters. This is a bounded experimental fallback, not a permanent device synchronization policy.
Baseline/offline semantics and acceptance thresholds are unchanged. Supplemental controlled runs use
a native high-resolution waitable timer at 48 kHz/256 frames, explicitly **without** an audio device;
they separate elapsed-time recovery from driver-clock behavior and do not establish device latency.

The initial controlled timer run skipped tiny wake delays on every block (approximately 20,000 frames
over 10 s even in baseline). Before repeating it, distinguish wake jitter from overload: keep absolute
sample-grid deadlines and recover only once a whole block is missed. Do not infer device reliability
from good processing durations in that failed timer policy. Lifecycle checks extend to ten instances
per candidate with per-disposal handle/memory trends after initial whole-process handle growth was
observed. These refine measurement/recovery, not the original acceptance thresholds.
