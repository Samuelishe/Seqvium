# SEQ-R0 audio architecture probe report

Role: Reproducible observations and bounded recommendation from the authorized audio probe.
Read when: Evaluating SEQ-R0, remaining Q-001–Q-007 evidence, or a later architecture decision.
Authoritative for: This experiment's environment, execution, measurements and limitations.
Not authoritative for: Production language/backend/ABI, product latency targets or release support.

Date: 2026-10-08. **Recommendation: narrow. Stage: partially evidenced; authorized probe work executed,
not blocked.** Both managed and native execution are viable for this tiny workload at the observed
Windows shared-mode period. Native has smaller measured processing tails; mandatory native execution
is not justified by this workload alone. Clock/recovery, intended workload/lower periods and clean
distribution need further evidence before a permanent architecture decision. R1 has not started.

## Baseline, hypotheses and scope

Initial Git: clean `master`, HEAD `edad4704309543bab17f83d7af2b7023895c8ac5`; index and tracked,
unstaged and untracked user-work sets were empty. Planning checkpoint SEQ-KB-R21. All source/report
changes here are uncommitted working-tree additions/edits, with no history/index operations.

The [protocol](SEQ-R0_PROTOCOL.md) was written before implementation/results. H1 (bounded C# callback),
H2 (native protection from managed runtime pressure), H3 (bounded semantic control/lifetime), and H4
(device-independent deterministic musical execution) were exercised. Both candidates use one native
WASAPI worker: either C scheduling/DSP or C# scheduling/DSP through a rooted reverse-interop delegate.
This isolates execution/runtime behavior; it does not compare two complete device stacks.

Direct managed platform interop is viable to probe with this shim; a fully managed WASAPI wrapper was
not implemented. WASAPI exposes negotiated period, padding and stream clock without another library.
waveOut was not selected for its weaker modern timing boundary. miniaudio and PortAudio remain portable
alternatives; their official licenses were reviewed, but neither source nor binaries were introduced.
C was enough for this fixed workload. No C++ engine, permanent C ABI, portable backend or package
architecture was adopted. [THIRD_PARTY](../THIRD_PARTY.md) owns actual provenance/obligations.

Excludes UI, persistence, graphs, sampler/synth product features, effects, plugins, recording, ASIO,
WAV export, CI and ProjectStats. No production project/solution was created.

## Environment and configuration

| Item | Actual observation |
| --- | --- |
| OS/CPU | Windows 11 Pro 10.0.26300 x64; AMD Ryzen 3 5400U, 4 cores/8 logical processors |
| Physical RAM | 16,541,458,432 bytes reported by Win32_ComputerSystem |
| Managed tools | SDKs 10.0.400/10.0.401 installed; experiment-local pin 10.0.401; runtime 10.0.12; workstation GC, Interactive latency |
| Native tools | Existing MSYS2 UCRT64 GCC package `mingw-w64-ucrt-x86_64-gcc 16.2.0-3`; headers/CRT `14.0.0.r302.gd7f3c5201-1` |
| Other tools | PowerShell 7.6.6; CMake 4.4.4/Ninja available but unused; no MSVC/Clang on PATH |
| Device | Default Speakers (`Динамики`), High Definition Audio Device; HDAUDIO VEN_14F1/DEV_1F87; Microsoft driver 10.0.26100.9549, 2026-09-17 |
| Endpoint identity | `{0.0.0.00000000}.{e2359e23-53eb-42ca-a231-510fbf25ee3f}` |
| Negotiated format | WASAPI shared event-driven, 48,000 Hz, 2 channels, float32 |
| Engine period/capacity | 480 frames = 10 ms; allocated buffer 1056 frames = 22 ms; ordinary packets 480 frames, starvation packets up to 1056 |
| Device clock | IAudioClock frequency 384,000 units/s; converted using negotiated rate, not assumed frame units |
| Priority/timing | MMCSS `Pro Audio` registration succeeded; QPC frequency 10,000,000 Hz |
| Stream latency query | IAudioClient returned 0 ms; this is an API result, **not** measured physical zero latency |
| Supplemental controlled run | No audio device: high-resolution native waitable timer, 48,000 Hz, mono, 256 frames/5.333333 ms |

Driver/endpoint identity came from actual WASAPI, registry and Win32_PnPSignedDriver reads, not merely
the sound-device inventory. Ordinary desktop processes/power/thermal/DPC activity were not isolated.
No debugger was attached and no system audio/volume/device/power settings were changed.

## Source, build and reproduction

[Source guide](../../experiments/seq-r0/README.md) documents layout and ownership. Native kernel and C#
processor are small independent implementations of one fixed contract. `native/device.c` owns Windows
COM/device handles; `probe.c` owns copied immutable state, fixed slots and lifetime; `controlled.c`
owns timer-only execution. Managed host owns preparation/reporting and the rooted callback delegate.
The production `Seqvium.sln` remains empty.

From `experiments/seq-r0/`, in PowerShell 7:

```powershell
dotnet --info
gcc --version
C:/msys64/usr/bin/pacman.exe -Q mingw-w64-ucrt-x86_64-gcc mingw-w64-ucrt-x86_64-headers mingw-w64-ucrt-x86_64-crt
./build.ps1
dotnet bin/Release/net10.0/Probe.dll verify
dotnet bin/Release/net10.0/Probe.dll device 10 > bin/device-final.log
dotnet bin/Release/net10.0/Probe.dll device 10 reverse > bin/device-repeat.log
dotnet bin/Release/net10.0/Probe.dll controlled 10 reverse > bin/controlled-grid.log
dotnet bin/Release/net10.0/Probe.dll lifetime > bin/lifetime-trend.log
C:/msys64/ucrt64/bin/objdump.exe -p bin/Release/net10.0/r0.dll
```

Use the installed toolchain's corresponding path if different. `global.json` owns SDK selection;
`build.ps1` owns compiler requirement and `-std=c11 -O2 -Wall -Wextra -Werror -ffp-contract=off -shared
-static-libgcc` plus OS import libraries. No audio/test packages, external source download or global
installation is needed. Both builds passed with zero warnings/errors. Verification uses the executable
harness, not a selected VSTest/MTP framework or `dotnet test`.

Local logs are ignored `bin/` artifacts, intentionally outside Git. This report preserves bounded
results; reruns naturally vary. The final measurement build's native DLL is 158,166 bytes; observed
SHA256 `9DAE1E36D69765C979BF53879575EC196F92E22EA988626BC14931450AD6E25C`, managed Probe.dll SHA256
`17BF61625497D3081B6F11EE30EEA321E9899266BB51461F9480DB46E7F3437C`. Earlier physical/timer series used
the same scheduling/DSP workload with the explicitly documented recovery-method amendments; final
changes to the lifecycle reporting did not alter DSP. This is developer build evidence, not packaging.
Final source review separated initialization status into an atomic value, avoiding a concurrent read
of mutable end-of-run Stats during Open. Release/ABI/oracle and ten device lifetimes per candidate
were revalidated (`lifetime-reviewed.log`); measured processing/control/recovery paths were unchanged.
That review rebuild has a different native binary identity from the full timing-series artifact above.

## Acceptance and measurement method

Predeclared: exact event positions/order; audio max absolute error <= 1e-6; finite output; bounded
slots/work/preparation; critical recovery within a completed block plus 32 release frames; no invalid
state use/deadlock/stuck voices. Baseline service/processing p99 must be below the negotiated period;
every observed miss is reported. Stress may glitch but cannot grow obsolete backlog. At least five
fresh device lifetimes per candidate; executed ten. No universal low-latency target was chosen.

Warm-up: 2000 blocks of 256 frames per processor, outside the actual run; each new worker also prefills
the endpoint before timing. New-thread/runtime effects can still remain. Fixed arrays hold up to
65,536 timings/traces and 524,288 floats; none filled the timing/trace limit. Capture truncation is
intentional. Timing/reporting/sorting occur after join; callback does no I/O/logging/allocation.

Processing duration includes native control/state work and processor dispatch; the managed branch
also includes reverse transition and two per-thread allocation-counter reads. Service duration includes
padding/clock queries, buffer access/release and deliberate stall where applicable. It excludes wake-up
delay; callback intervals are separately measured. Percentiles use nearest-rank order statistics.
QPC empty-pair measured approximately 0.020–0.027 microseconds; overhead is included, not subtracted.
Period and packet frames/rate are declared conservative service budgets, not measured DAC deadlines.
Render padding exhaustion is an observation; no genuine driver underrun/dropout counter was exposed.
Good callback statistics or successful submission do not prove glitch-free acoustical output.

## Actual device timing

Primary final forward run: baseline/pressure about 10.009 s and 1002 callbacks per candidate;
overload about 5.009 s, 491 native/490 managed callbacks. Units below are **microseconds**.

| Candidate/run | Processing median / p95 / p99 / worst | Service median / p95 / p99 / worst | Processing / service / packet-budget misses |
| --- | --- | --- | --- |
| Native baseline | 5.5 / 13.5 / 21.2 / 29.0 | 16.3 / 26.6 / 33.3 / 43.9 | 0 / 0 / 0 |
| Managed baseline | 8.3 / 28.3 / 44.3 / 1260.6 | 18.0 / 39.0 / 54.3 / 1267.6 | 0 / 0 / 0 |
| Native pressure | 5.3 / 14.7 / 22.4 / 27.8 | 15.5 / 28.6 / 39.4 / 124.0 | 0 / 0 / 0 |
| Managed pressure | 7.6 / 21.2 / 661.1 / 1408.3 | 16.3 / 33.0 / 671.6 / 1426.6 | 0 / 0 / 0 |
| Native injected overload | 5.6 / 15.4 / 24.4 / 27.5 | 14.6 / 28.8 / 42.2 / 30027.2 | 0 / 4 / 4 |
| Managed injected overload | 8.7 / 20.8 / 26.1 / 38.9 | 18.7 / 34.9 / 56.0 / 30032.0 | 0 / 4 / 4 |

Declared period budget: **10,000 microseconds**. There were no exhausted-padding observations in
baseline/pressure and four per overload candidate. That does not give a hardware dropout count.
Worker stalls of 30 ms were injected every 100 serviced packets, rather than hidden in baseline.
Overload p99 hides four rare 30 ms stalls; worst/counts are essential to interpretation.

| Candidate/run | Callback interval median / p95 / p99 / worst, microseconds |
| --- | --- |
| Native baseline | 10000.4 / 10036.3 / 10087.4 / 10367.9 |
| Managed baseline | 9999.1 / 10265.2 / 10420.7 / 10779.4 |
| Native pressure | 9999.9 / 10076.6 / 10271.9 / 10487.7 |
| Managed pressure | 9999.0 / 10237.5 / 10369.3 / 10813.7 |
| Native overload | 9999.2 / 10249.1 / 10689.3 / 40332.6 |
| Managed overload | 10000.9 / 10212.6 / 10508.9 / 40023.5 |

The reversed order also passed the same baseline/pressure scope. An earlier reversed recovery series
observed processing p99/worst 537.6/1362.3 microseconds for managed pressure versus 25.8/41.9 native;
no service-period misses. It independently supports a tail difference, not a fixed speed ratio.
Actual simultaneous workloads differ in produced requests/GC count, and there is no ETW causal trace
attributing individual slow callbacks to GC rather than JIT/OS scheduling. No universal GC threshold
or guaranteed native deadline follows.

The final-build reversed repeat (`device-repeat.log`) additionally observed:

| Candidate/run | Processing median / p95 / p99 / worst, microseconds | Service median / p95 / p99 / worst, microseconds | Seconds / callbacks / service misses |
| --- | --- | --- | --- |
| Managed baseline | 8.7 / 32.7 / 57.0 / 1013.5 | 17.9 / 42.5 / 70.9 / 1020.4 | 10.009363 / 1002 / 0 |
| Native baseline | 5.6 / 13.0 / 20.9 / 32.1 | 13.7 / 24.5 / 32.5 / 50.3 | 10.009380 / 1002 / 0 |
| Managed pressure | 8.4 / 20.9 / 796.5 / 1420.3 | 17.5 / 36.4 / 805.2 / 1434.1 | 10.009379 / 1002 / 0 |
| Native pressure | 5.5 / 12.7 / 20.9 / 102.3 | 12.8 / 24.1 / 34.1 / 106.4 | 10.009406 / 1002 / 0 |
| Managed overload | 8.6 / 20.3 / 29.0 / 34.0 | 16.5 / 33.3 / 50.3 / 30031.1 | 5.009258 / 490 / 4 |
| Native overload | 5.8 / 15.8 / 25.7 / 38.1 | 14.5 / 27.7 / 85.6 / 30019.9 | 5.009275 / 490 / 4 |

Both repeated pressure runs had 71 Gen2 collections, about 370.77/369.16 MB total allocation and
180,887/180,119 controls (managed/native). Final generation 706/703 converged; both acknowledged
stopped/zero voices. Repeated overload recovered four times, skipped 3418/3419 frames and 38/38 events,
with final elapsed-anchor deficit one frame, zero clock regressions and acknowledged panic.

## Managed pressure, preparation and overload semantics

Primary device pressure results:

| Metric | Native | Managed |
| --- | --- | --- |
| Total application allocated bytes during run/report setup | 335,560,384 | 416,894,112 |
| Gen0 / Gen1 / Gen2 collections | 65 / 65 / 64 | 81 / 81 / 80 |
| Control calls | 163,699 | 203,467 |
| Preparation requests | 638 | 793 |
| Worker publications / obsolete work | 83 / 374 | 32 / 422 |
| Native publication rejects / reclaimed retired slots | 82 / 82 | 30 / 31 |
| Final active prepared generation | 639 | 794 |
| Live slots after join | 2 of 4 | 2 of 4 |
| Callback-thread managed allocation bytes | Not applicable; no managed DSP call | 0 |
| Last callback acknowledged transport / voices | Stopped / 0 | Stopped / 0 |

Allocation ring retains at most 128 x 32,768 bytes = 4 MiB, with 16 fresh arrays per iteration and
explicit blocking compacting full GC every ten iterations. There is one preparation worker, one latest
request, an intentional 15 ms preparation delay and cancellation. Requests produced faster than that
delay are coalesced; obsolete work is abandoned. Counts are not one-for-one because some requests
are never started, some published states go stale before activation, and native rejection counters
also include stale callback-side activation. No growing work/task/event queue is used.
The per-thread allocation counter surrounds C# Render; allocations inside runtime entry before the
delegate body, unmanaged system services and other threads are outside that counter's scope.

Gain is latest-wins. Stop/release/panic generations persist separately from gain and desired play;
same-block rapid stop/restart clears prior state before applying current desired playback. These are
current-state controls, not a history-preserving command journal. Musical events stay in an immutable
ordered list, not a disposable UI queue. Final current prepared generation and critical stop/panic
were checked by the last callback's acknowledgment, before shutdown cleanup could hide stuck state.
Preparation cancellation completed and no outdated generation activated.

Controlled slot exhaustion fills four slots, returns explicit `-3`, and recovers after three retired
slots reclaim. Stale preparation returns `-1`; invalid prepared events/table return `-2`. Mutable
preparation arrays are copied: overwriting the caller's table with NaN after publication does not
alter finite callback output. Pool/queue policy is experimental, not a complete musical-event admission
or production graph compiler.

## Clocks, events, loops and recovery

Generated triangle table, eight fixed voice slots, 11 events per 1024-frame loop; starts/releases at
0, 255, 256, 257, 511, 512, 767, 768 and 1023, including three same-frame ordered events at 1023.
Release lasts 32 samples (0.667 ms at 48 kHz), declared only for this probe. Start resets musical position
while the 64-bit sample clock remains monotonic. Compatible snapshot replacement preserves voice state.

The independent sequence oracle passed both candidates at blocks 1/127/256/257/511/1024 for 32 loops,
352 expected event tuples per case. Event offsets/order matched exactly, including loop rollover.
Artificial advancement by 10,000,000,000 frames checked 64-bit progression, loop modulo and a next-event
offset of 254 frames. This is arithmetic evidence, not a multi-hour hardware run. No BPM/tempo/rational
musical-time conversion/rounding or seek-state reconstruction was implemented.

Initial overload failure: reported IAudioClock remained monotonic but progressed less than elapsed QPC
through starvation; four stalls yielded device position about 236,546 rather than about 240,000 frames
over 5 s. Device-clock-only recovery skipped zero frames and could accumulate musical delay on repeated
starvation. Reject that boundary as an elapsed-time recovery authority on this endpoint. The protocol
records the amendment before remeasurement.

Final bounded fallback anchors QPC to the submission clock plus current padding. Exhausted padding
causes one bounded skip to elapsed time; obsolete voices terminate and skipped notes are counted,
rather than replaying old starts/releases indefinitely. Primary overload results:

| Metric | Native | Managed |
| --- | --- | --- |
| Recovery operations | 4 | 4 |
| Skipped frames / musical events | 2936 / 31 | 3418 / 38 |
| Maximum pre-recovery deficit | 865 frames (18.021 ms) | 866 frames (18.042 ms) |
| Maximum positive deficit after recovery across services | 36 frames (0.750 ms) | 28 frames (0.583 ms) |
| Final elapsed-anchor deficit | 1 frame (0.0208 ms) | 1 frame (0.0208 ms) |
| Last callback acknowledgment after final Stop/Release/Panic | Stopped / 0 voices | Stopped / 0 voices |

No sample/device-clock regression occurred. In baseline, submitted clock was 482,016 at the last
packet, with device position about 479,972–479,973 (lead about 2043–2044 frames); this includes different
stream/submission pipeline domains and is not DAC latency. Under recovery the stream clock can lag
the logical QPC timeline, so that lead grows; do not interpret it as an accumulating queued backlog.
No software audio backlog exists, and elapsed-anchor deficit returns close to zero. Long-duration
oscillator drift, sleep/power changes, changing clock calibration, other endpoints and device loss
remain open. Skip recovery deliberately cuts old voices and omits missed sound; it cannot reconstruct
arbitrary held notes/effects/plugin state or promise musical/offline parity through a dropout.

## Controlled timer results

Initial timer policy treated every small late wake as a skip. Despite short callback processing it
discarded 21,010 native / 21,659 managed frames over 10 s baseline. This was a scheduler-policy failure,
not physical underrun evidence. A small absolute-grid refinement tolerates sub-period wake jitter and
skips only whole-block overload. The final controlled run has no baseline/pressure skipped frames:

| Candidate/run | Seconds / callbacks | Processing median / p95 / p99 / worst, microseconds | Service p99 / worst, microseconds | Service misses / skipped frames |
| --- | --- | --- | --- | --- |
| Native baseline | 10.000336 / 1875 | 3.2 / 10.2 / 25.3 / 78.5 | 27.6 / 78.6 | 0 / 0 |
| Managed baseline | 10.000121 / 1875 | 5.7 / 25.6 / 43.5 / 1382.0 | 43.8 / 1382.0 | 0 / 0 |
| Native pressure | 10.000148 / 1875 | 3.2 / 11.6 / 26.4 / 62.1 | 26.8 / 62.6 | 0 / 0 |
| Managed pressure | 10.000282 / 1875 | 5.7 / 14.9 / 48.0 / 1375.6 | 48.3 / 1375.7 | 0 / 0 |
| Native overload | 5.002831 / 898 | 3.0 / 9.9 / 20.8 / 66.0 | 66.6 / 30007.3 | 8 / 10240 |
| Managed overload | 5.002924 / 898 | 6.0 / 17.3 / 36.8 / 53.3 | 53.8 / 30012.1 | 8 / 10240 |

Budget 5333.333 microseconds, eight injected stalls per overload run, 110 skipped events; both stopped
before shutdown. Baseline clock ends exactly at 480,000 frames; overloaded clock follows the current
sample grid without backlog. Native pressure had 66 Gen2 collections and 168,315 controls; managed
72 Gen2 and 184,223 controls. Managed callback allocation remained zero. Wake-interval worst was
9.820 ms native / 10.093 ms managed baseline, despite no processing/service-budget miss: timer wake-up
reliability and processor duration are distinct. This does **not** establish a usable 256-frame device
period, hardware underrun count or low-latency audio output.

## Offline comparison

Device-independent verification passes 12 candidate/block cases with exact sequence-oracle events,
finite generated output and max absolute audio error **0** (allowed tolerance 1e-6). It does not
derive expected event offsets from the processor's own block traversal. A declared frame-768 gain
change is part of the sequence; start and per-voice release are ordered events.

Each actual uninterrupted device baseline captured the first 262,144 stereo frames (5.461333 s),
2816 discrete events. Offline replay of the actual prefill and packet partitions matched exact event
tuples and every captured float sample (error 0) for both candidates. Capture occurs before device
conversion/mixing/DAC; no loopback or microphone measurement was taken. Pressure/overload sequences
with external control races/skipped time are intentionally excluded from that deterministic comparison.
No effect tails, arbitrary DSP graphs, plugins, cross-platform floating-point or WAV-export parity is
claimed. The native backend is unnecessary for offline musical execution itself.

## Lifetime, shutdown and failures

Ten fresh initialize/start/replace/stop/restart/release/panic/cancel/join/dispose device instances per
candidate passed. No unavailable initialization, HRESULT failure, crash, stale callback or stuck voice
was observed in that series. Concurrent open and invalid duration reject explicitly; reopening one
closed Probe is unsupported (`-5`), while creating a fresh instance and transport restart work.
Active/pending/retired slots have one owner; all native Probe counts returned to zero after disposal.
Callbacks stop after joined close; delegates remain explicitly GC-rooted until successful destruction.
Join failure retains state/root instead of freeing it under a live worker.

Whole-process handles in final reviewed lifetime verification: native cold start 197 -> 267 after
first disposal -> 268 after second, then 268 through instance ten; managed 276 throughout. Private
bytes before/after final GC: native 11,534,336 -> 10,280,960; managed 11,292,672 -> 11,440,128.
Native per-cycle private bytes rose to about 12.28 MB before GC; managed varied about 11.03–11.37 MB.
Cold COM/runtime/reporting retention
is a plausible contributor, not an established attribution. No continuing per-instance handle growth
was observed after warm-up; this is not proof of absence of all system/native leaks or use-after-free.
No sanitizer, Application Verifier or device-removal fault run was available/performed.

Implementation failures kept as evidence: initial native link lacked materialized WASAPI/KS GUIDs;
local GUID definitions fixed the build without another dependency. The initial join timeout equaled
the nominal 10 s run and rejected a worker ending slightly beyond it; it now allows requested duration
plus a bounded 5 s margin (1–30 s runs). No resource was freed while that worker remained alive.
Device-clock-only recovery and overaggressive timer skipping failed as described above. These are
corrected experimental failures, not hidden green-only results.

## Interop, distribution and portability trade-offs

One million empty zero-frame processor calls measured about 0.0025–0.0036 microseconds native and
0.0136–0.0227 managed per call across runs. Includes the managed allocation-counter instrumentation;
it is not a pure uninstrumented ABI benchmark. This is much smaller than the observed millisecond-scale
managed tails, but does not identify their cause. Reverse callbacks need managed runtime entry and
delegate rooting; native processing avoids that per-block boundary. Native buffers/slots are owned
equally in both candidates, so this probe does not compare a managed-array resource scheme.

Native DLL import inspection: AVRT.dll, KERNEL32.dll, ole32.dll, and UCRT API sets for heap, private,
runtime, stdio and string. No libgcc, libstdc++, winpthread or MSYS DLL import is present. Build copies
installed complete MinGW-w64 runtime notices plus GCC exception/license files beside local output.
The private CRT import and actual runtime/materialization requirements need clean-system evaluation;
absence of MSYS imports is not distributability proof. No installer/self-contained package was built.

C# gives ordinary managed diagnostics, but this design still needs unsafe layout/interop checks and
native debugger/device knowledge. A native execution choice adds native compilation, per-RID artifacts,
native diagnostics and explicit lifetime/ABI ownership. Fixed shared structs are convenient research
plumbing, not an extensible production public API. C/C++/managed-AOT/portable-library options and
Linux/macOS actual builds/devices remain untested. The OS adapter is intentionally separate from the
device-independent processors, preserving the architectural portability direction.

## Recommendation and exact question disposition

**Narrow** the architectural recommendation: accept these observations as bounded experimental
evidence; keep preparation/cancellation/allocation/reporting on the control side and fixed scheduling,
voice state, sample clock and DSP on the execution side. Both C# and native are experimentally viable
at this device's 10 ms period. Investigate native execution if the intended workload/period requires
protection from the observed managed timing tails. Do not adopt mandatory native code, C++, miniaudio,
WASAPI, a C ABI or the four-slot policy as permanent architecture from this result.

**Reject** device stream clock alone as elapsed-transport overload authority on the tested endpoint,
and reject treating every tiny timer delay as lost musical time. The QPC fallback and sample-grid
timer recovery are useful bounded mechanisms; a production synchronization policy remains open.

| Question | Disposition and evidence | Exact remaining scope |
| --- | --- | --- |
| Q-001 | Open, partially evidenced: native/C# host feasible; both pass local budget; native tails smaller | Whether native complexity is justified at intended workload/period, clean deployment and wider runtime evidence |
| Q-002 | Open, narrowed: C/WASAPI works locally; alternatives/licenses reviewed | Final language/backend, portable audio-library comparison, supported device/OS/RID scope and actual packaging |
| Q-003 | Open, partially evidenced: copied fixed state, single callback owner, safe retirement/root/join in tested cases | Production ABI/decomposition, failure/race/device-loss/sanitizer evidence; arbitrary graph lifetime remains Q-018 |
| Q-004 | Open, partially evidenced: coalesced parameters/preparation, sticky controls, explicit capacity/stale rejects | Production event admission/overflow/recovery policies, multiple preparation producers and wider semantic loads |
| Q-005 | Open, partially evidenced: exact frame/loop/order/64-bit checks and bounded skip/panic recovery | BPM/tempo/rational conversion/rounding, long hardware drift, robust clock/device-loss policy and state reconstruction |
| Q-006 | Open, narrowed: exact synthetic scheduling/audio and captured baseline/offline parity | Effects/tails/graphs/plugins, wider numerical/platform and frozen-production-render semantics |
| Q-007 | Open, partially evidenced: one real 10 ms period plus separately identified timer evidence | Achievable lower device periods, DAC/acoustic latency/dropouts, longer runs, broader devices and intended product workload |

No question is closed and no permanent architecture decision record is added. R0 is **partially
evidenced**, not blocked: all authorized local phases were exercised, but broader architecture
acceptance cannot be inferred from one tiny workload/device, unvalidated lower periods, incomplete
fault/clock evidence and untested clean distribution. This is not a requirement for full three-platform
DAW certification at R0. Further work must be separately scoped; the probe is not permission to start R1.

Before an R1 implementation decision: agree on intended initial execution/device scope; resolve the
required musical-time conversion/clock semantics; decide whether additional period/workload and
clean-loading evidence is necessary for that scope; explicitly select or defer native/backend
boundaries with evidence. R1's domain objects must remain independent of this probe's types/adapter.
No full graph, plugin host or production scheduler is a prerequisite introduced by this report.

Canonical state, development/testing/provenance entries and Q-001–Q-007 now reflect partial evidence;
completed probe work belongs to cold WORK_LOG. Git history/index and pre-existing user work remain
preserved; build/log artifacts are ignored. Final whitespace/link/scope/diff checks supplement the
actual Release/functional/device/controlled/lifetime verification.
