# SEQ-R4-F2 execution and Windows graph evidence

Role: Compact numerical/workload/device evidence for the implemented F2 slice.
Authoritative for: Observed measurements and acceptance limits, not product contracts.
Owners: [Audio](../AUDIO_ENGINE.md#r4-f2-independent-contributions-and-realtime-convergence),
[Node graph](../NODE_GRAPH.md#implemented-r4-f2-prepared-item-local-execution),
[verification](../TEST_EXECUTION.md#r4-f2-execution-verification).

## Outcome and scope

The execution-identity continuation below passes 609 tests and retains further physical failures;
F2 remains partial. The original checkpoint and measurements in the following sections are preserved.

The original functional checkpoint passed all 600 tests, preserving 558 prior cases; locked restore and full
Release build have zero warnings/errors. F2 remains **partial**, because successful repeated Windows
pressure runs coexist with two non-injected starvations. R4 remains in progress / partial; F3 is not started.
No project schema/F1 identity, dependency/package/assembly, GUI or permanent backend selection changed.
Final checks also confirm new source/test Compile participation, 971 relative links/anchors across
38 active Markdown files, changed archival links and a clean whitespace diff. Staging and baseline
HEAD remain unchanged; all untracked task files are permanent source/tests or this one report.

The hypothesis was that extending the existing sampler before its sum would preserve independent
placement/part paths with one scheduler/decoder/ownership protocol. Independent upstream arithmetic,
fan-out, same-resource release and realtime parity support it. A graph after premixing cannot implement
branch-specific Gain; a second renderer would duplicate existing R2 semantics and was not introduced.
Contracts, including finite transition and joined shutdown, remain in the owners above.

## Reproduction and measurement

Baseline was clean `master` at `95cf3b9b091c06c8c79310a5b531b7bcdc85cec5`.
Windows x64 10.0.26300, .NET SDK 10.0.401/runtime 10.0.12, workstation GC, Release, Stopwatch 10 MHz.
The workstation has an AMD Ryzen 3 5400U, four cores/eight logical processors; Windows reports
16,541,458,432 bytes of physical memory. This is one workstation's evidence, not a minimum specification.
The silent `endpoints` command identified the active default as High Definition Audio Device speakers;
this is not the historical UR12 endpoint. Actual opened ID:
`{0.0.0.00000000}.{e2359e23-53eb-42ca-a231-510fbf25ee3f}`.

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-measure 60
```

The existing DeviceCheck warns before output; only quiet mathematical WAV material is authored/imported
in a private UUID fixture. System volume/rate/default endpoint/preferences remain untouched. A full run
opens baseline and pressure for 60 seconds each, then three fresh injected-fault lifetimes. A short
3-second startup run also passed but does not substitute for the long run. Raw JSON is retained only
in ignored `.artifacts/SEQ-R4-F2/` for owner review, including the failed run; no binary/WAV/capture is
permanent documentation. Fixture material is deleted after preparation completion and joined release.

The physical topology is the largest allowed node count: 8 Source, 8 independent branch Gain,
8-input Mix, 14 downstream Gain and Output; 32 nodes, 31 connections, 8 bound sources and 8 voices.
One shared mono 44.1 kHz float WAV contains a 220 Hz sine plus ramp, bounded by amplitude 0.015.
Parts have separate 0–14.6 ms offsets, fractional pitches, intensity 0.2, finite duration and 20 ms
release. Branch coefficients are 0.25–0.60, final gain 0.20. Repeats use absolute quarter boundaries.
This is the heaviest actually verified node/voice count, not every possible 64-connection graph.

WASAPI negotiated 48 kHz stereo float32, 10 ms period, 1056-frame capacity (22 ms), MMCSS enabled,
clock frequency 384000. The driver's reported stream-latency field was zero; it is not measured DAC
latency. Service packets were normally 480 frames; pressure also produced zero-work wakes and 960-frame
packets. Initial 1056-frame prefill is included in PCM capture, excluded from service timing percentiles.

Diagnostics are explicitly enabled and bounded: 1,905,604 bytes of capture/partition/observation payload.
176,400 interleaved captured samples cover 1.8375 seconds at this negotiated format. Baseline is compared
both to offline processing at actual capture partitions and to independently written source interpolation,
absolute note/release arithmetic and port-UUID-ordered branch/Mix calculation. Expected PCM never calls
the production graph executor. Pressure changes canonical source/coefficients, so its steady-state oracle/
parity fields are null; deadline, finite-output, allocation and lifetime observations still apply.

Pressure adds one CPU worker, a 128-by-32 KiB ring with sixteen allocations per control cycle, forced
compacting Gen2 collection every hundred cycles, Gain edits every ten and source replacements every fifty.
The serialized owner context pumps the actual coordinator's posted completions/progress. This measures
CPU/GC/control interference, not a fake device clock or independent graph runtime.

## Functional and budget evidence

The 42 new cases cover independent Kick/Snare and `m*(k*K+S)` at Source/Gain/Mix/Output, rate/layout
adaptation, stereo asymmetry, note/release/EOF/repeat/hard Stop and variable partitions. They exercise
branch swap, fan-out without extra performance, shared resource/independent release, silent dependencies,
unity Gain, geometry/node order and exact nonassociative Mix port ordering. No existing assertion weakened.

The maximum deterministic packet uses 32 nodes, 8 voices, 22 Gain, Mix and 65,536 stereo frames.
Scratch accounting is 16,262,640 bytes, below the 16 MiB cap. Five distinct 32,000,000-byte decoded
PCM16 dependencies exceed the 128 MiB plan cap and refuse wholly even when all parts are silent.
Maximum event/window/voice/packet refusal retains R2 limits. Work accounting is conservative sample/event
units, not CPU instructions: the maximum tested packet uses 40,763,456 units within the 67,108,864 cap.

Physical scratch is 271,600 bytes and shared PCM 176,400 bytes per plan; published active payload is
448,000 bytes. Maximum packet work is 656,896 units, maximum packet-window event count eight.
Candidate/active/pending/retired reservation is bounded to 576 MiB PCM/scratch plus a separate
102,401,024-byte conservative prepared-table allowance. These are retained payload/allowance bounds,
not managed process RSS, canonical history or temporary decode/diagnostics totals.

Gated tests prove cancellation/ABA/latest-request/capacity retry, immediate invalid intent status while
a worker is occupied, independent Undo edits, repair convergence, no runtime resurrection after reopen,
target/Close, hard Stop/Panic, topology epoch restart and a finite 44/48-frame new-state fade even at
in-packet EOF. Gain-only edits retain an already releasing voice. Geometry provenance retains origin
prepared revision. Failed join retains borrowed state, then confirmed retry releases it. PCM becomes
collectible after all leases end. Tests use synchronization gates, not timing sleeps or physical audio.

Warm graph allocation measurement excludes fixture/arrays/control setup and measures Process/status
entry after separate warm-up: zero bytes. The unchanged preview allocation case passed five isolated
consecutive runs, plus full-suite runs; an initially incorrect method filter selected zero tests and
was corrected before these five runs. There was no failing allocation assertion to dismiss as warm-up.

## Repeated physical observations

The table preserves a passing final-status series and the subsequent failed repeat. Quantiles are
median/p95/p99/worst microseconds. Further audit results are recorded below, without erasing this failure.

| Observation | Passing baseline | Passing pressure | Failed pressure repeat |
| --- | ---: | ---: | ---: |
| Duration seconds | 60.0349 | 60.0313 | 22.8603 |
| Service observations | 6004 | 6004 | 2285 |
| Processor µs | 65.4/92.7/117.2/238.4 | 56.6/91.4/109.8/6265.7 | 61.4/89.1/105.2/197.8 |
| Service µs | 80.3/116.3/144.7/286.6 | 67.1/108.6/131.6/6288.7 | 74.3/106.0/126.2/6935.2 |
| Processor/service/packet deadline misses | 0/0/0 | 0/0/0 | 0/0/0 |
| Padding exhaustion | 0 | 0 | 1 |
| Processor/service/worker allocation bytes | 0/0/0 | 0/0/0 | 0/0/0 |
| Gen0/1/2 collections | 1/1/1 | 556/396/118 | 194/136/40 |
| Process allocation bytes (harness/control included) | 1,384,088 | 3,089,024,152 | 1,083,176,928 |
| Canonical edits | 0 | 476 | 168 |
| Prepared created/released | 1/1 | 80/80 | 28/28 |
| Maximum sample magnitude | 0.000601156 | 0.000688205 | 0.000688205 |
| Maximum adjacent/boundary delta | 0.000330998 | 0.000609823 | 0.000433711 |
| Stop/Panic acknowledged | yes/yes | yes/yes | no/no, terminated first |
| Joined/live states after release | yes/0 | yes/0 | yes/0 |

All successful baseline captures had zero offline difference and independent oracle error
`5.820766091346741e-11`, finite samples and eight observed voices. Earlier full 60-second series also
passed baseline/pressure; the original pressure worst processor/service was 4947/4968 µs. Subsequent
diagnostic repeats passed with variable packet distribution. None establishes universal deadline safety.

The non-injected repeat failed at padding exhaustion, not an over-budget graph or callback allocation.
The worker correctly terminated, joined, released every prepared state and preserved the unacknowledged
Stop/Panic counters. Callback time alone excludes wait/dispatch/GC pauses before service; the exact cause
is unresolved. Additional wake and cumulative GC-pause diagnostics were added for the final audit.
No budget, device setting, failure guard or workload was relaxed to make a repeat pass.

The final wake/GC audit repeats the failure at **39.5703 seconds**: 3956 observations, one padding
exhaustion, zero processor/service/packet deadline counters and zero processor/service/worker allocations.
Processor quantiles were 62.2/102.4/120.1/2428.4 microseconds; service 72.2/119.1/139.8/2453.2;
wake intervals 9999.1/10058.6/13103.0/**30182.9**. The worst interval exceeds the 22 ms device capacity.
Cumulative process-wide GC pause was **1070.679 ms**, with 371/262/77 collections and 2,088,462,408
process allocation bytes. This establishes a missed service opportunity outside measured DSP/service
work. It does not correlate the individual missed wake with a specific GC pause or OS scheduling event;
that causal attribution remains unresolved. No instrumentation was allocated in the measured worker.
The 318 accepted edits produced 54 states, all released after confirmed join; 53 retirements, zero
live states/voices, and no fabricated Stop/Panic acknowledgment after termination.

That audit's baseline passed for 60.0423 seconds/6005 observations: processor worst 241.9 microseconds,
service worst 318.9, wake worst 10668.5, cumulative GC pause 0.885 ms, zero deadline/exhaustion/allocation
counters, zero offline difference and the same independent oracle error. Stop/Panic acknowledged and
the single state was released after join. Raw audit and earlier successful/failed JSON remain ignored
local evidence. Selecting only the passing runs would misrepresent the acceptance result.

Every series' injected invalidations reported native `0x88890004`; injected 30 ms stalls reported
starvation. Each confirmed join, zero active voices/live prepared states and one terminal boundary;
Stop acknowledgment stayed zero. These are injected failures, not physical unplug evidence.

## Limits and next acceptance gate

Keep F2 partial until the repeated pressure starvation is explained/resolved or a justified measured
supported workload policy is established without weakening required musical execution. The implementation
and functional evidence remain reviewable; full F2 acceptance cannot be inferred by selecting passing runs.
Wake/GC attribution and wider scheduling evidence belong to Q-047/Q-057 and the audio owner.

Acoustic perception/click assessment remains separate owner review. Render-buffer capture establishes
the submitted PCM path, not that the owner heard speakers, microphone loopback or DAC latency.
Only this endpoint's 48 kHz stereo format was physically measured; mono/44.1 kHz are deterministic
execution evidence. No multi-hour stability, all graph distributions, native engine, other platform,
cross-context, plugin/tail, normal GUI transport or F3 readiness acceptance is claimed.

## Execution identity correction and starvation attribution (2026-10-09)

This bounded continuation started from clean `master` at
`e6e258dbc0709d59b6a15e4769dd68d704eaad55`. No existing work, staging or history was changed.
The two published failures above remain unsuccessful acceptance results. F2 stays **partial** and
F3 stays unstarted. This section extends the same report; raw logs/traces stay ignored locally.

### Deterministic production error and correction

Before changing production, all 600 existing tests passed. A real coordinator regression creates both
valid item-local attachments before execution, so A and pending B share canonical revision R. It starts
A, selects/publishes B without consumer handoff, invalidates B's Gain, rejects/retirements B at a consumer
boundary, then repairs B with different valid coefficients. With A's two Gain and B's 1/2/3 Gain,
all three cases fail in the original code: B=1 throws `IndexOutOfRangeException` from graph PCM processing;
B=2/3 change A's PCM using B coefficients. Publication authority invalidates pending B correctly, but
revision-only recognition incorrectly treats active A as the accepted B basis. Equal array lengths do
not protect against it. An initial fixture omitted a required second source and was corrected before
these production failures; that setup timeout is not evidence of the execution collision.

The minimal identity reuses each prepared state's existing monotonic publication authority, scoped to
one sampler, plus its attachment. This is `PreparedExecutionId` in coherent status, never canonical JSON.
Coordinator fast path, update publication and consumer validation all require it. The plan's original
prepared revision remains frozen, independently of current audio/equivalent provenance. Explicit rejected
handoff observation identifies the rejected prepared execution and clears that basis before normal retry.
It does not infer rejection from a status that may still precede an in-progress handoff. This avoids
repeated preparation while waiting for consumer observation. Retry never issues Start, and deliberate
Start refuses an already observed rejected publication.

Nine new deterministic cases cover B=1/2/3 both playing and stopped, PCM/provenance, automatic convergence,
same-attachment reprepare, Undo/Redo/ABA at the original UUID, pending target changes, retry after explicit
rejection without a new canonical edit, finite progress, and both publication/consumer identity gates.
They use owner completion gates and actual sampler packets, no device or timing sleeps. Locked restore,
full Release solution build and the exact requested full test command pass: **609/609**, no failures/skips,
zero warnings/errors. All 600 old cases/assertions remain intact.

### Measurement protocol and limits

Environment remains Windows 10.0.26300 x64, SDK 10.0.401/runtime 10.0.12, Ryzen 3 5400U (4 cores/8 logical),
16,541,458,432 bytes RAM, workstation GC / Interactive, QPC 10 MHz. Silent discovery confirms the same
High Definition Audio speakers ID printed above; diagnostic variants select that exact ID. Every open
reports 48 kHz stereo float32, 1056-frame/22 ms capacity, 10 ms period, clock frequency 384000, MMCSS on.
No system volume/defaults, GC mode, priorities or device sizing changed. Acoustic review remains separate.

Before the series, the local protocol declares three requested 60-second runs for each of full, natural,
fixed forced and legacy, with all results retained. `graph-diagnose` uses the existing GraphCheck fixture,
load cadence and sampler/output path; its guide documents exact commands and factor switches. Full uses
the original CPU worker, allocation ring, Gain/source edits and blocking compacting Gen2 every 100 cycles.
Natural removes only the explicit collection. Fixed forced keeps the graph unchanged, removes the other
external loads and retains the same collection cadence. Legacy uses identical WAV bytes/events/voices/
tempo/rate/packets and external load; it directly sums voices without graph DSP/fade/scratch, with uniform
gain 0.1 rather than branch Gains and final 0.2. A separate mirrored source edit publishes legacy replacements.
This is a controlled current-device comparison with a declared DSP/control difference, not historical UR12.

All twelve initial diagnostic runs enable the same bounded native runtime listener. Audio observations
use preallocated arrays and QPC timestamps for wait entry/return, service/query, PCM and submission,
packet sequence/frames/padding/device clock. At exhaustion PCM/submission timestamps stay zero, and
submitted frames stop increasing: no backlog processing occurs. `WakeTicks` is still service-start
interval, not an OS event-to-dispatch measurement.
Wait entry/exit bracket the managed P/Invoke wrapper, including runtime transitions/resumption; they
do not isolate time inside the kernel wait. Process/GC/load counters end before Stop/Panic,
joined cleanup, JSON and offline/oracle calculations; output/lifetime totals explicitly include Stop/Panic
and joined shutdown. Workload timing records are filtered by the declared end. Fault detection can lag
the last audio observation, so workload duration and precise audio failure timestamp remain separate.

Available tools were checked before tracing: no global dotnet-trace/PerfView; built-in WPR/tracerpt are
available. Harness-only BCL EventListener records runtime GCStart/End, SuspendBegin/End and RestartBegin/End
at Informational GC keyword 1, up to 20,000 records. [Runtime event definitions](https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-garbage-collection-events)
distinguish suspension preparation, completed suspension and restart; collection call duration is not
automatically suspension duration. Event timestamps, asynchronous receipt timestamps and bracketed
UTC/QPC start/end anchors are retained separately. No listener, file output, allocation or control wait
is added to the audio path. Collector overflow is zero in the initial series. Native timestamp conversion
and clock mapping have uncertainty; comparisons below use millisecond resolution, not microsecond OS
dispatch claims. Listener delivery has overhead and may change collection timing; untraced follow-ups
therefore retain the original acceptance workload too. Nothing is installed globally or added as a dependency.

The initial full series overlaps one failed test-project build during development; all its runs remain
recorded with that confound. The initial matrix also precedes the final exact-rejection retry refinement;
natural's first run includes three extra prepared states. These observations are diagnostic evidence,
not a selected final acceptance series. Final original-workload runs are declared separately below.

### Initial complete diagnostic matrix

Every requested run is 60 seconds. Durations below are the control workload boundary; failed audio
timestamps are specified separately. GC is Gen0/1/2; allocations count authored 32 KiB arrays; edits include
source edits (shown after slash). Quantities exclude JSON/oracle/cleanup process work.

| Variant / repeat | Result / seconds | Worst service-start interval ms | GC 0/1/2 | Explicit GC | Allocations | Edits / source |
| --- | --- | ---: | --- | ---: | ---: | --- |
| full 1 | pass / 60.0185 | 18.7780 | 525/375/112 | 37 | 60032 | 450/75 |
| full 2 | pass / 60.0128 | 21.4880 | 567/403/120 | 40 | 64832 | 486/81 |
| full 3 | pass / 60.0082 | 23.4095 | 560/399/119 | 40 | 64096 | 480/80 |
| natural 1 | pass / 60.0102 | 19.7007 | 560/396/80 | 0 | 64416 | 482/80 |
| natural 2 | starvation / 41.0876 | 52.1069 | 376/268/54 | 0 | 43216 | 324/54 |
| natural 3 | starvation / 8.3000 | 35.2587 | 71/50/11 | 0 | 8352 | 62/10 |
| fixed forced 1 | pass / 60.0133 | 14.6483 | 38/38/38 | 38 | 0 | 0/0 |
| fixed forced 2 | pass / 60.0048 | 17.2718 | 38/38/38 | 38 | 0 | 0/0 |
| fixed forced 3 | pass / 60.0024 | 12.8141 | 37/37/37 | 37 | 0 | 0/0 |
| legacy 1 | pass / 60.0488 | 24.3297 | 457/305/77 | 37 | 60768 | 454/75 |
| legacy 2 | pass / 60.0096 | 20.7894 | 454/303/76 | 37 | 60400 | 452/75 |
| legacy 3 | starvation / 40.3169 | 48.4835 | 302/202/52 | 25 | 40016 | 300/50 |

All twelve runs have zero processor/service/packet deadline counters and zero measured processor/service/
worker allocations. Every run joins and releases all prepared states. Failures terminate before Stop/Panic
acknowledgment; shutdown does not fabricate acknowledgment. Initial graph/legacy/fixed states created and
released are respectively 76/82/81, 84/54/11, 1/1/1, 76/76/51. Passing intervals above 22 ms can still have
nonzero padding because preceding zero-work/partial packets change buffer phase; interval alone is not
the starvation assertion. Padding exhaustion remains the guard.

### One established runtime interval and two additional windows

Natural repeat 2 has no explicit collection. Relative to WASAPI PlaybackStart:

| Time ms | Observation |
| ---: | --- |
| 41000.1334 | Audio sequence 4101 begins; padding 576, packet 480 frames |
| 41000.2093 | Submission complete; 1,969,536 total frames submitted; PCM took 58.4 µs |
| 41000.2096 | Audio enters next native wait |
| 41008.1159 | Runtime SuspendBegin for GC |
| 41008.1931 | Runtime SuspendEnd: execution engine suspended |
| 41008.2504 | GCStart 945, Gen1, allocation trigger (reason 0), blocking type 0 |
| 41051.8303 | GCEnd 945 |
| 41051.8330 | RestartBegin |
| 41051.8587 | RestartEnd |
| 41052.2401 | Audio wait returns after 52.0305 ms |
| 41052.2403 | Sequence 4102 begins; padding zero, 1056 frames available, submitted count unchanged |

Completed suspension to RestartBegin is **43.6399 ms**, exceeding 22 ms device capacity; including
restart through RestartEnd gives 43.6656 ms. The SuspendBegin-to-RestartEnd envelope is 43.7428 ms.
Audio resumes about 0.38 ms after RestartEnd.
No PCM is processed/submitted at sequence 4102; clock advances from 15,739,936 to 15,756,288 units while
submission stays frozen. This establishes a runtime suspension aligned with an allocation-triggered
collection that is sufficient to exhaust this buffer. It does not establish whether the collecting
thread spent 43 ms computing GC or was itself descheduled, nor does it retrospectively explain the two
historical failures. Event receipt is delayed by up to about 60 ms here; receipt time is not suspension time.

Natural repeat 3 faults at audio time **8.294468 s**, with a 35.1512 ms wait. Background Gen2 starts at
8.25318 s, an inner blocking Gen1 ends at 8.25786 s, then GC-preparation SuspendBegin (reason 6) occurs at
8.25951 s. SuspendEnd is only at 8.29399 s; restart completes at 8.29579 s. Most of this envelope is
suspension preparation, not proven time when the whole runtime was suspended. That distinction remains
an attribution limit and a reason to examine scheduling if finer diagnosis is required.

Legacy repeat 3 faults at audio time **40.2686628 s**. An explicit compacting Gen2 call at cycle 2500 spans
40.22571–40.26867 s (about 42.96 ms); native SuspendBegin-to-RestartEnd spans about 42.94 ms, with completed
suspension near 40.24669 s and Gen2 GCStart/GCEnd at 40.24676/40.26789 s, reason 10 (induced compacting).
The mapped native start precedes the harness call bracket by about 0.74 ms: these independently converted
timestamps cannot support finer absolute alignment without calibration. Audio's native wait spans
48.4114 ms, padding becomes zero and submitted frames stay 1,932,096. It proves the same failure exists
without graph DSP and aligns with runtime GC; it does not claim equivalent DSP cost or a driver fault.

Initial comparisons localize individual failures to runtime suspension on the shared managed path:
explicit GC removal is insufficient, fixed-graph collections pass this series, and legacy also fails.
CPU-only/edits-only switches are available but are not expanded into a redundant matrix after this
localization. An allocations-only switch also isolates the ring without CPU, edits or explicit GC.
They remain useful for the narrower question of which allocation/control/CPU combination
creates the longest runtime intervals. Scheduler/driver causality is unproven. WPR CPU profiles were
inspected (CSwitch/ReadyThread exist); their default 788 × 1 MiB system buffers would materially perturb
this workstation's memory load, so no broad system recording is added merely to explain an already
identified fully suspended interval. A smaller bounded scheduling trace is the next attribution step.

### Final-code original-workload series and natural follow-up

After exact-rejection retry refinement, the entire solution passes locked restore/zero-warning Release
build/609 tests. Before further playback, the follow-up protocol declares three complete `graph-measure 60`
invocations (each baseline + original pressure + three injected fault lifetimes), then three `natural 60`
runs with GC tracing. No builds/tests run during these configurations. Each opened endpoint/fact matches
the initial matrix. No pressure criterion, forced collection, guard or system setting is changed.

| Final-code configuration / repeat | Result / workload seconds | Worst service-start interval ms | GC 0/1/2 | Explicit GC | Allocations | Edits / source |
| --- | --- | ---: | --- | ---: | ---: | --- |
| original baseline 1 | pass / 60.0104 | 10.8599 | 0/0/0 | 0 | 0 | 0/0 |
| original baseline 2 | pass / 60.0058 | 10.7431 | 0/0/0 | 0 | 0 | 0/0 |
| original baseline 3 | pass / 60.0161 | 15.2930 | 0/0/0 | 0 | 0 | 0/0 |
| original full pressure 1 | pass / 60.0029 | 19.3997 | 531/378/112 | 38 | 60912 | 456/76 |
| original full pressure 2 | starvation / 53.5031 | 30.5289 | 471/336/99 | 33 | 53920 | 404/67 |
| original full pressure 3 | starvation / 24.9010 | 44.6614 | 222/158/46 | 16 | 25600 | 192/32 |
| natural traced 1 | pass / 60.0190 | 21.1851 | 531/378/77 | 0 | 61120 | 458/76 |
| natural traced 2 | pass / 60.0033 | 18.7852 | 518/368/76 | 0 | 59824 | 447/74 |
| natural traced 3 | pass / 60.0070 | 17.8604 | 517/367/74 | 0 | 60160 | 451/75 |

The original pressure series has no runtime listener, so its two individual pauses remain unattributed;
they establish that listener overhead is not required for starvation. Their aggregate process GC pause
totals are 1418.352/734.273 ms, not proof of the specific missed intervals. Output observations exist
in fixed storage during these runs, but this harness revision serialized problem windows only for explicit
diagnostic variants. That reporting omission is corrected for subsequent `graph-measure` calls; it does
not reconstruct unavailable raw windows or change playback/acceptance behavior.

All nine final-code configurations have zero processor/service/packet deadline and processor/service/
worker allocation counters, confirmed join, no live prepared states and finite PCM. Original baselines
have zero offline PCM difference and independent oracle error within 2e-6. Prepared states created/released
are 1/1/1 for baselines, 77/68/32 for pressure and 77/75/76 for natural. All nine injected fault lifetimes
pass joined termination/zero-state-release checks without fabricated Stop acknowledgment. Two-second full
and legacy startup/trace smoke runs also pass; they are retained separately and do not replace long runs.

Natural's final 3/3 does not erase its initial 1/3 or change acceptance policy. Original forced-GC workload
is still 1/3 in this final series, in addition to the two historical failures. The initial full traced 3/3
is retained with its build overlap and interim retry limit; none of these successes is selected as proof
of uniform stability. Across the two declared long series, all 21 configurations remain recorded (16 pass,
five starvation failures), alongside every prior published result.

Raw evidence is retained in the exclusively created ignored
`.artifacts/SEQ-R4-F2/identity-starvation-bc354f8025ba4716b647907181f1c59e/` directory: declared plans,
all smoke/series JSON, native GC records and compact derived summaries. Five pre-existing JSON logs in
the parent directory remain untouched. Evidence is kept for owner review of failed intervals and
reproducibility; no WAV, GUI capture, profiler installation or new permanent report collection is retained.

### Interpretation and minimal next step

- **Established production error:** revision collisions admit wrong-attachment coefficients; execution/
  attachment identity fixes it, with PCM/provenance, finite retry and Stop regression evidence.
- **Established specific runtime delay:** one allocation-triggered blocking Gen1 has a 43.64 ms
  completed-suspension-to-restart interval overlapping starvation. Other native windows identify induced
  compaction and a long suspension-preparation envelope; they are distinguished from collection-call duration.
- **Not established:** driver/OS signal-to-dispatch latency, whether a collecting/suspending thread is
  descheduled inside the longest runtime intervals, causal attribution of the historical/untraced failures,
  all-device guarantees, acoustic perception or an accepted replacement engine/workload policy.

The next narrow diagnostic is a 60-second same-endpoint collection with a smaller WPR profile enabling
CSwitch/ReadyThread/process-thread events and bounded buffers, correlated with the retained audio/GC
identities. It should distinguish collecting-thread computation from scheduler delay and the long GC-prep
handshake. Avoid the inspected default 788 MiB profile and declare observer cost before recording.
This does not select another buffer, GC mode or native renderer. A separately approved buffer experiment
would need margin above the observed 52.03 ms wait (for example around 60 ms), repeat the unchanged original
workload and quantify latency; it cannot promise safety from these finite measurements or silently redefine
supported behavior. F2 remains **partial**, F3 remains unstarted and owner listening remains separate.

The final reporting-only correction is checked by one additional declared two-second `graph-measure`
smoke, with no repeat to seek a pass: baseline passes (2.0092 s), but full pressure starves at the workload
boundary 1.7049 s / audio lifetime 1.7020 s. Its worst service-start interval is 43.0000 ms and wait wrapper
is 42.8913 ms; sequence 168 sees zero padding, unchanged 81,216 submitted frames and no PCM/submission.
There are 107 cycles, 1712 authored allocations, 12 edits, 16/9/3 collections and one explicit collection
at cycle 100 lasting 4.5982 ms. That explicit call precedes the fault interval and cannot be assigned as
its cause; no native runtime trace was enabled for this smoke. Callback deadline/allocation counters
remain zero, all three states are released after join, and all three injected fault checks pass.
This short failure is additional retained evidence outside the 21 long configurations, and confirms
that `graph-measure` now serializes its bounded failure window. In total the continuation retains six
new non-injected starvations, alongside the original two; successful short full/legacy smoke is not
selected instead of this result.

That attribution session's process token was not elevated (`WindowsPrincipal.IsInRole(Administrator)` was false).
WPR status confirms no recording. Microsoft's [WPR recording guide](https://learn.microsoft.com/en-us/windows/win32/tracelogging/tracelogging-record-and-display-tracelogging-events)
uses an elevated command prompt; only read-only WPR availability/profile/status inspection was done here.
No system trace, token elevation or system permission change is attempted in this session. The bounded
scheduling follow-up therefore requires an appropriately permitted context; GC listener evidence needs
no such elevation. This is an explicit evidence/permission limit, not an automatic approval rejection.

Final verification repeats the exact requested locked restore/full Release build/test commands after
the last Start rejection guard, reporting correction and optional allocations-only switch: zero warnings/
errors and 609/609 tests pass. These refinements add no DSP, device or existing-load change to the measured
paths; allocations-only is an available, unrun follow-up. MSBuild Compile items include all affected Core,
Windows, test and harness sources, including the new GcTrace. All 1129 relative links/anchors checked
across 40 Markdown files pass (cold archive excluded except changed WORK_LOG); `git diff --check` passes.
Final Git is the original `master`/HEAD with empty staging, 14 modified tracked files and one new permanent
harness source, GcTrace.cs. No user work existed at entry or was removed. Task-only analysis/check scripts
and path-pointer files are removed after verification; all 16 raw/protocol/derived-evidence files are kept
in the UUID evidence directory (4,848,904 bytes) for review, alongside the untouched five prior logs.
The quoted natural failure's clock-anchor brackets are 1.3/1.4 µs, with final-versus-initial drift -0.35 µs;
this bounds anchor sampling/drift, not the separately observed native timestamp-conversion bias.

## Elevated bounded scheduler evidence (2026-10-09)

Baseline is clean `master` at `5dcd10bc61ab7c7ebf921d7cc6e95142e0086b80`. The owner explicitly
authorized one autonomous bounded WPR investigation after restarting Rider as administrator.
The ACP terminal's `WindowsPrincipal.IsInRole(Administrator)` returned **true** and its integrity
label was High (`S-1-16-12288`). The observed launch chain is Rider → node → cmd → Codex → PowerShell;
no UAC bypass or security/system configuration change was used. WPR is the existing System32 binary,
file version **10.0.26100.9549**, banner 10.0.26100 CoreSystem. Default and task-owned WPR instances
were idle before recording; unrelated system ETW sessions were preserved.

### Profile startup and retained trace

The prepared smoke script was read before execution. It only checks elevation/status, creates unique
local logs, starts its own named profile, holds about two seconds and saves/stops that instance.
Corrected `Full` starts and saves successfully: start/stop/final-status exits **0/0/0**, ETL
**11,534,336 bytes (11 MiB)**, 107,100 events and zero lost events/buffers. Both expected collectors
are active. KernelMinimal/Kernel fallbacks were unnecessary and were not run.

The earlier owner-reported `0xc5580612` describes an event session without providers, corresponding
to the runtime event-collector side of this profile. It did not recur with executable scoping removed
and strict runtime enablement. The corrected combination's success is established; isolating the
original failure to `ProcessExeFilter` rather than another simultaneous repair is **not** established.
No attempt to recreate the failing configuration was needed.

The sole hardware trace uses `SeqviumF2Scheduler.Verbose.Memory`, named instance
`SeqviumF2Diag89fed0a9`, with ProcessThread/CSwitch/ReadyThread/Loader/DPC/Interrupt and runtime
provider `e13c0d23-ccbc-4e12-931b-d9cc2eee27e4`, level 4, keyword 0x10001 and the prepared event-ID filter.
Active status reports **496 × 256 KiB kernel + 32 × 128 KiB runtime = 128 MiB** of buffer capacity.
This is observed active buffer storage, not measured total OS/process memory or tracing overhead.
The saved ETL is **93,585,408 bytes (89.25 MiB)**. Both live collector loss counters and the merged
ETL header's EventsLost/BuffersLost are **zero**. Offline ProcessTrace succeeds, reads all **357**
buffers and **2,121,229** events, and reports no callback or scheduler-state continuity errors.

ETL header UTC range is **10:42:33.9395505–10:43:25.0016843**. Actual raw-QPC event span is
**51.0612068 s**; CSwitch coverage is **50.7025563 s**, QPC 201045281682–201552307245.
It includes process/thread starts, the entire measurement (201060029432–201544150933), and the failure
window, rather than only final rundown. Zero-loss counters alone would not prove absence of circular
overwrite; the retained earliest/latest scheduler and target runtime events establish this coverage.

### Sole declared hardware workload

Silent discovery exit 0 confirms the exact previous active High Definition Audio speakers ID
`{0.0.0.00000000}.{e2359e23-53eb-42ca-a231-510fbf25ee3f}`. The current default is Samsung USB C Earphones;
the command uses the explicit original ID, with no fallback or default change:

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-diagnose full 60 1 "{0.0.0.00000000}.{e2359e23-53eb-42ca-a231-510fbf25ee3f}" gc-trace
```

Opened facts remain 48 kHz stereo float32, 1056-frame/22 ms capacity, 10 ms period and MMCSS enabled.
The unchanged real 32-node/eight-source graph, one CPU worker, allocation ring, canonical Gain/source
edits and forced compacting collections run once; no concurrent build/test or added local load runs.
It **fails with Starvation**, exit 1, at **48.4046957 s** of playback / **48.4121501 s** of measurement.
Before failure: 3008 load cycles, 48,128 payload allocations, 360 edits including 60 source edits,
30 explicit collections, and generation collection counters 420/299/89. Process allocation is
2,349,278,872 bytes; this is cumulative allocation, not retained or peak RAM.

Sequence 4835 discovers zero padding and unchanged 2,321,376 submitted frames. Its wait wrapper is
**74.3001 ms**, service-start interval **74.3841 ms**; no PCM or submission occurs for the failed packet.
Processor/service/packet misses and processor/service/worker allocated-byte counters remain zero.
Failure evidence is captured in existing fixed storage, serialized after joined cleanup/listener drain,
and copied at process exit; the controller enters WPR stop about **0.713 s** after measurement end.
This failure was retained without another workload run.

### Monotonic scheduler/runtime attribution

PID **16548**, audio TID **11052** (`Seqvium WASAPI output`), collecting/suspending TID **11120**
(`.NET TP Worker`) are confirmed by harness output, kernel lifecycle/name events and runtime emitters.
The separate background GC TID **2752** (`.NET BGC`) remains waiting throughout the relevant interval.
Analysis uses native ETL QPC directly at 10 MHz, via installed Windows OpenTrace/ProcessTrace with
[raw timestamps](https://learn.microsoft.com/en-us/windows/win32/api/evntrace/ns-evntrace-event_trace_logfilew).
It does not substitute asynchronous BCL receipt times. All 2520 workload BCL GC records have nearby
matching native ID/TID/count events; native-minus-BCL timestamp differences range +63.9 to +386.0 µs,
and initial/final BCL clock-anchor drift is 88.15 µs. Native ETL owns the phase durations below.

| Phase | Native QPC interval | Duration | Observed execution |
| --- | --- | --- | --- |
| SuspendBegin → SuspendEnd | 201543459659–201544133480 | 67.3821 ms | Initiator: 4.9249 ms scheduled, 62.4504 ms Ready, 0.0068 ms Standby, no Waiting |
| Longest initiator Ready interval | 201543465254–201544077449 | 61.2195 ms | CSwitch out with OldThreadState=Ready, priority 8; next switch in after the interval |
| SuspendEnd → RestartBegin | 201544133480–201544150537 | 1.7057 ms | Audio waiting; initiator scheduled throughout |
| GCStart → GCEnd | 201544134095–201544150493 | 1.6398 ms | Collection 425, natural Gen0, reason 0/type 0; initiator scheduled throughout |
| RestartBegin → RestartEnd | 201544150537–201544150705 | 0.0168 ms | Audio becomes Ready near the end |

The failing suspension is a **natural Gen0 inside the original full forced-GC workload**, not the
last explicitly induced Gen2. The initiator's scheduled handshake time includes only 13.5 µs of
overlapping ISR/DPC; no ISR/DPC overlaps its GCStart–GCEnd scheduled interval. Scheduled time minus
ISR/DPC is not an assertion that every remaining instruction belongs to a particular GC algorithm.

Audio enters its native WASAPI wait at QPC 201543407771, switches out at 201543407830, becomes Ready
at 201543506851 and runs at 201543506936: **8.5 µs** Ready-to-running. It immediately switches out
again at 201543507065, remains waiting for **64.3540 ms**, then becomes Ready at 201544150605 and runs
at 201544150718: **11.3 µs** Ready-to-running. WaitExit is 201544150772; padding query ends at
201544150866. Maximum reconstructed audio Ready-to-running in the whole workload is 239.9 µs.
The long worker wait lies inside the runtime suspension envelope; interpretation as a blocked
managed return during suspension is supported by these events and the existing native-wait source.
No wait stack or hardware-event signal timestamp was captured, so the wait's exact internal function
cannot be proved from the generic Windows UserRequest wait reason alone.

During the initiator's 61.2195 ms Ready interval, all eight CPU timelines are retained (489.756 summed
scheduled ms). Kernel lifecycle/process events identify **rider64.exe PID 13832** occupying
**460.773 scheduled ms / 94.082%** across those CPUs, largely eight threads at priorities **10–11**,
against the initiator's priority 8. The first preempting thread is Rider TID 19328, priority 11.
Other processes occupy the remainder; idle time is only 9.9 µs summed across CPUs. These are scheduled
occupancy totals, including interrupt overlap, not stack-attributed useful application computation.
The responsible Rider operation (for example indexing or JVM GC) is not identified by this profile.

**Established for this failure:** external CPU competition and a Ready-but-not-running suspension
initiator materially extend the runtime suspension handshake while the managed audio worker waits.
This is scheduler evidence inside runtime suspension, not a 74 ms audio dispatch delay or 67 ms
of GC computation. It does not establish a Windows scheduler defect, driver fault, the causes of
historical failures, or an accepted permanent native/backend/buffer/GC/priority policy.

### Tools, cleanup and next engineering scope

Installed tracerpt decodes the smoke trace but lacks a CSwitch v5 schema. A task-local BCL/PInvoke
reader uses installed SDK layouts and Windows APIs to decode the relevant payload prefixes and raw
QPC; its smoke event counts exactly match tracerpt (20,372 CSwitch, 11,874 ReadyThread, 107,100 total).
No WPA/xperf/TraceEvent binary was found in the inspected PATH, standard WPT locations or NuGet cache;
no global or local third-party software was installed. Existing facilities suffice for the reported
intervals; stacks/function attribution would require a separately scoped capture, not merely an ETL viewer.

Minimal follow-up is to identify the competing Rider background activity from local IDE/JVM evidence,
then, if needed, declare one unchanged 60-second workload with the IDE idle to test that environment
hypothesis. Do not change system priorities/services/security, audio sizing, GC mode or acceptance rules.
The successful startup and one fully attributed failure leave **R4-F2 partial; R4-F3 not started**.

WPR stop exit 0, default/named final status idle, no task WPR collectors remain; DeviceCheck exited.
Output joins; **61 created / 61 released / zero live prepared states**; the owned temporary fixture is
absent. Stop/Panic acknowledgments are false after fault termination, as expected for that distinct
lifetime path. Retain ETL/JSON/logs and the bounded local decoding/controller scripts in ignored
`.artifacts/SEQ-R4-F2/elevated-20261009-89fed0a9637942c98c45e9af8a885f81/`, plus the smoke capture under
the previous scheduler preparation directory, for offline owner review and reproducibility. No upload,
commit/push/index mutation or production/harness code change. The 609-case test baseline is inherited;
tests/build were not rerun for these documentation-only permanent changes.
