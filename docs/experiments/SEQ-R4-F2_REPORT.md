# SEQ-R4-F2 execution and Windows graph evidence

Role: Compact numerical/workload/device evidence for the implemented F2 slice.
Authoritative for: Observed measurements and acceptance limits, not product contracts.
Owners: [Audio](../AUDIO_ENGINE.md#r4-f2-independent-contributions-and-realtime-convergence),
[Node graph](../NODE_GRAPH.md#implemented-r4-f2-prepared-item-local-execution),
[verification](../TEST_EXECUTION.md#r4-f2-execution-verification).

## Outcome and scope

Functional execution is ready: all 600 tests pass, preserving 558 prior cases; locked restore and full
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
