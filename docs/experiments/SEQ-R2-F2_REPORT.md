# SEQ-R2-F2 realtime WAV/device report

Role: Reproducible local observations for bounded F1 production realtime integration.
Read when: Assessing F2 acceptance, reproducing device measurements or narrowing remaining R2 evidence.
Authoritative for: This series' actual environment, workload, measurements, failures and limits.
Not authoritative for: Permanent engine/backend/ABI, product latency guarantees or release support.

Date: 2026-10-08. F2 is **locally accepted-ready within its bounded scope**.
At the F2 checkpoint, overall **R2 remained in progress / partial**. Current R2 acceptance is in
[PROJECT_STATE](../PROJECT_STATE.md) and the [F4 audit](SEQ-R2-F4_REPORT.md#complete-r2-requirement-audit).
R0 remains partially evidenced / narrow; R1 and F1 remain
local accepted-ready. No permanent managed/native engine choice follows.

## Baseline and ownership

Initial clean `master`, HEAD `4b709df6f3def521e1130ac33150992cc8ca982e`; index, tracked working
changes and untracked content were empty. No commit, push, fetch, checkout or staging operation was used.

Portable Core preserves F1 SamplerPreparation, PreparedSampler and OfflineSampler DSP/scheduling.
Only hot-path LINQ free-slot selection/voice counting became bounded loops; F1 numerical semantics and
all existing tests remain. RealtimeSampler adds finite packets, commands, captured preparation authority
and active/pending/retired ownership. One candidate reserves preparation capacity until publication/
disposal; completed unpublished results cannot accumulate decoded resources. The frozen document/media
roots belong to preparation only. The processor never reads canonical mutable objects or source files.

One narrow Windows assembly owns COM, default endpoint, shared event-driven buffers, MMCSS, clock/wake
and shutdown. Private OS vtables avoid RCW allocation and HRESULT exception-driven normal flow.
Core has no Windows dependency. DeviceCheck is an explicit physical harness, not a workstation or engine
SDK. No native sampler, new audio package, graph/Mixer, GUI, R3, plugin host, Sample Lab or recording.

Publication checks open lifecycle, exact revision/transition generation and target, cancellation and
request authority. Pending capacity rejects explicitly; a full retired mailbox delays handoff. The
consumer stops borrowing old state before the control owner disposes leases. Replacement starts the new
prepared Pattern in a fresh epoch, without transferring old voices. Identified old execution can continue
while a newer canonical revision is preparing/rejected; it never grants stale async acceptance.

Gain is latest-wins; Start and sticky Stop/Panic retain command order independently. Unit tests exercise
Stop before/after Start and gain bursts, captured preparation after edits, removed/closed/stale targets,
Undo/Redo generation, cancellation, pending/retirement/candidate capacity and lease release.
A separate terminal consumer boundary on fault/Close clears execution but **does not acknowledge Stop**.
The owner must join output before sampler disposal.

## Selected environment and protocol

The [protocol](SEQ-R2-F2_PROTOCOL.md) fixed workloads/thresholds before collection. Actual selected
endpoint differs from R0, and the machine itself differs from its Ryzen laptop:

| Fact            | Actual observation                                                                                          |
|-----------------|-------------------------------------------------------------------------------------------------------------|
| OS/CPU/RAM      | Windows 11 Pro 10.0.26300 x64; Intel Core i7-13700KF, 16 cores/24 logical processors; 34163740672 bytes RAM |
| Managed         | SDK 10.0.401, .NET runtime 10.0.12, workstation GC / Interactive                                            |
| Device/driver   | Default Line (Steinberg UR12), USB VID_0499/PID_170A; Yamaha Steinberg USB driver 2.1.9.0, 2025-05-20       |
| Endpoint        | `{0.0.0.00000000}.{af417ee0-95bf-4faf-ac43-7739c2bb6304}`                                                   |
| Format/backend  | WASAPI shared event-driven, 44100 Hz stereo float32                                                         |
| Period/capacity | 441 frames / 10 ms; 970-frame capacity (about 22 ms), queried from the endpoint                             |
| Clock/QPC/MMCSS | IAudioClock 352800 units/s; Stopwatch/QPC 10000000 ticks/s; Pro Audio registration succeeded                |
| Latency query   | IAudioClient reports 0; this is not measured DAC/acoustic zero latency                                      |

Registry properties identify this endpoint's UR12/driver; CIM confirms OS, CPU, RAM and driver.
No audio/rate/volume/power setting was changed. No debugger was attached; ordinary desktop scheduling,
DPC, thermal and power behavior were not isolated.

Supported adapter configurations are float32 mono/stereo 44.1/48 kHz, standard/unspecified channel mask,
queried positive period and capacity 1–65536 frames. **Only the UR12 44.1 kHz stereo configuration was
physically validated here.** Other configurations are supported by code/deterministic PCM tests, not
device acceptance. Other rates, PCM device formats and multichannel layouts refuse; no ASIO/input/MIDI.
R0's 48 kHz/480/1056 observations and timer-only 256-frame case are not facts of this F2 endpoint.

The actual workload is an independently written one-second mono 44.1 kHz float WAV, mathematically
authored sine plus a small ramp in Program.cs, imported through production managed storage. Quarter
Pattern repeats, root/fractional pitches, intensity 0.5, staggered starts, 70% quarter durations and
50 ms release exercise independent overlap; 1/4/8 observed live voices, 137/120/137 BPM.
1024 bounded absolute repeats avoid rounded-loop accumulation. Source/staging fixtures are private
UUID directories removed only after joined/released execution.

Warm-up is 2000 offline 256-frame blocks and worker entry before prefill. Main series has three 60 s
baselines and three 60 s pressure runs. Pressure retains at most 128 x 32768-byte arrays, allocates
16 each cycle, writes 256 gains, forces full compacting GC every tenth cycle and prepares revisions
every twentieth cycle. Main-series revisions rename the document, preserving sound intent; acceptance
cancellation/supersession and extra cancelled workers are explicit. Supplements below add observed
in-flight cancellation/supersession and actual root sound revisions.

## Actual performance

Units: **microseconds**, median / p95 / p99 / worst, nearest rank after join. Processor includes
Process and allocation-counter instrumentation. Service includes padding/clock/native buffer calls,
Process, bounded capture and finite/silence/voice observations, plus deliberate stall when injected.
Wait/wake delay is separate. All recorded processing/service/packet misses are checked, not only p99.

| Run / voices | Processor                   | Full service               | Wake interval                         | Observed service frames | Seconds / invocations |
|--------------|-----------------------------|----------------------------|---------------------------------------|-------------------------|-----------------------|
| baseline 1   | 3.6 / 5.1 / 21.5 / 1153.8   | 7.8 / 17.4 / 34.3 / 1169.5 | 10173.6 / 10577.6 / 10896.8 / 11274   | 441–441                 | 60.0109769 / 6002     |
| pressure 1   | 3.4 / 4.3 / 14.4 / 56.6     | 7.2 / 12.9 / 31.4 / 1101.5 | 10169.8 / 10564.6 / 10778.3 / 11517   | 441–441                 | 60.0116345 / 6002     |
| baseline 4   | 10 / 13.9 / 24.9 / 64.8     | 14.2 / 24.1 / 37.1 / 85.7  | 10154.1 / 10577.1 / 10840.3 / 11297.8 | 0–441                   | 60.0152166 / 6002     |
| pressure 4   | 9.8 / 12.3 / 23.9 / 1522.8  | 13.7 / 22.1 / 38 / 1526.8  | 10171.6 / 10614.3 / 10906.1 / 12046   | 0–882                   | 60.0181654 / 6002     |
| baseline 8   | 18.4 / 20.8 / 38.2 / 111.8  | 22.5 / 29.2 / 49.6 / 122.4 | 10182.1 / 10570.3 / 10731.8 / 11169.3 | 441–441                 | 60.0123207 / 6002     |
| pressure 8   | 18.3 / 20.4 / 33.3 / 2173.2 | 22.2 / 28.3 / 46.3 / 2181  | 10173.3 / 10598.9 / 11172.2 / 14285.4 | 0–882                   | 60.0152771 / 6002     |

**All six baseline/pressure runs: 0 processor period misses, 0 service period misses, 0 packet-budget
misses and 0 exhausted-padding observations.** Period budget is 10000 us; packet budget is frames/rate.
A zero-frame observation is a wake with no writable packet, not discarded music. Prefill processed
970 frames; normal services processed 441, and pressure included actual 882-frame services.
Maximum wake interval 14.2854 ms is distinct from full service duration and physical dropout evidence.

Every main run recorded **0 processor, 0 full-service and 0 running-worker managed allocated bytes**.
Processor counter surrounds Process; service counter also includes native calls/return transitions and
capture; worker counter includes native waits. Initialization, prefill and terminal cleanup are excluded.
These counters do not measure unmanaged allocation or prove absence of every runtime/OS transition cost.
Measured service/processor/wake durations retain those costs rather than subtracting them.

| Pressure voices | Process allocated bytes | Gen0 / Gen1 / Gen2 | Gain writes | Accepted states incl. initial |
Preparation cancellations / cancelled publication / stale publication / stale handoff | Capacity rejects | Retired /
released states |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 3490747864 | 575 / 575 / 575 | 1473280 | 145 | 57 / 95 / 48 / 28 | 0 | 144 / 145 |
| 4 | 4479719920 | 567 / 567 / 567 | 1451776 | 143 | 56 / 94 / 47 / 28 | 0 | 142 / 143 |
| 8 | 5849861456 | 557 / 557 / 557 | 1427968 | 141 | 55 / 92 / 46 / 28 | 0 | 140 / 141 |

Baseline process allocations were 982304 / 977016 / 977184 bytes, with 0/0/0 collections each.
Process counters include the explicit harness control/Task.Delay and measurement setup, excluding later
oracle/report generation; they are not callback allocations. No per-callback GC causal attribution is
claimed. Smaller native tails in R0 do not establish a speed ratio on this different CPU/USB endpoint.

All six runs acknowledged requested Stop **2 before Close**, observed zero active voices and at least
one actually submitted silent packet after Stop. Each joined and reached **0 live prepared states after
Dispose**. Baselines created/released 1/1; pressure created/released 145/145, 143/143, 141/141.
Capacity rejects are zero in the physical series; deterministic tests explicitly fill pending/retired/
candidate capacity and observe refusal/backpressure. The consumer-stale counts reflect superseded
pending preparations, not dropped musical events from an active plan.

## Output oracle and lifetime

Separate observed in-flight eight-voice supplements (same period/packet acceptance):

| Supplement                      | Processor median / p95 / p99 / worst, us | Service median / p95 / p99 / worst, us | Wake median / p95 / p99 / worst, us | Seconds / invocations | Service frame range |
|---------------------------------|------------------------------------------|----------------------------------------|-------------------------------------|-----------------------|---------------------|
| Name revision, in-flight checks | 18.3 / 23.9 / 52.7 / 3228.3              | 22.3 / 36.1 / 64 / 3250.3              | 10172.7 / 10566.9 / 11029.1 / 13919 | 60.0191668 / 6002     | 0–882               |
| Root 60/60.5 sound revision     | 18.6 / 24.2 / 57 / 1830.2                | 22.7 / 35.9 / 68.8 / 1834.5            | 10174 / 10564.1 / 11116 / 13998.2   | 60.0203654 / 6003     | 0–882               |

Both passed with **zero processor/service/packet misses, zero padding exhaustion and zero processor/
service/worker allocated bytes**. Both observed eight voices, finite samples, Stop 2 acknowledged before
Close, a submitted silent Stop packet, one separate termination boundary, joined output and released
142/142 prepared states (141 retirements, zero live states).
Both counted **93 cancellations and 47 supersessions issued while preparation was incomplete**.
149 total preparation cancellations include the separate pre-cancelled admission checks; 47 stale
publication refusals, 28 stale consumer handoffs, zero cancelled publication and zero capacity rejects.
The candidate is awaited before disposal; no stale result activates or edits canonical music.

Name supplement: 4936980104 process allocated bytes, 563/563/563 collections, 5639 pressure cycles,
1443584 gain writes and 142 accepted states. Sound supplement: 4941391088 bytes, 562/562/562 collections,
5627 cycles, 1440512 gains and 142 states. Actual root changes are canonical owning edits; prepared
coefficients become audible only at boundary handoff. This does not claim live voice-state transfer.

Each uninterrupted baseline captured **1048576 float samples / 524288 stereo frames** (11.888617 s),
including 970-frame prefill, 441-frame services and an intentional final partial captured prefix.
1188 captured partitions replay through offline F1 with **maximum absolute error 0** for 1/4/8 voices.
Independent source/interpolation/intensity/release sums have maximum error
4.654501e-10 / 5.189642e-9 / 1.624332e-8, below the declared 2e-6. All captured/submitted samples were
finite. The independent oracle includes the next candidate loop to account for an absolute onset rounded
down one frame before the unrounded loop quotient changes.

The ordinary suite adds tiny independent ramp/envelope/EOF, rational onset and variable-packet cases,
zero-frame notes, 120/137 BPM, both execution rates, independent release and Start/Stop boundaries.
A sound-edit test proves old execution retains old coefficients until handoff and new root 72 yields
the independently expected half-speed ramp, without callback mutation of canonical serialized state.

Ten fresh physical lifetimes in the reviewed series all passed silence before musical Start,
Start/Stop/Restart/Panic, joined Close/Dispose and zero retained states. Services numbered 27 each,
except one lifetime with 28. Last packet Panic acknowledgment was **4**, and remains 4 after cleanup;
termination is separately counted as one boundary. Later tests opened fresh sessions again.

The PCM capture is **before hardware conversion/mixing/output**. No loopback, microphone or DAC
measurement exists; it is neither acoustic latency nor a glitch-free listening guarantee. Pressure
captures are not called uninterrupted parity because external revision/gain commands change execution.

## Faults, failed checks and evidence limits

Final separate fault run: injected 30 ms worker stall terminated with Starvation after 12 observations,
0.1403895 s elapsed. Full service worst **30003.8 us**, one service-period miss, one packet-budget miss
and one padding exhaustion. Available packet capacity reached 970; stale PCM was not replayed.
The worker stopped/reset/released OS state and joined; terminal execution count 1, active voices 0,
retained states 0. **Stop acknowledgment remains 0**: a terminal boundary is not a completed packet Stop.

Injected device invalidation reported **0x88890004**, terminated/joined with no live voices/states;
canonical JSON remained identical and managed WAV still decoded. A well-formed nonexistent endpoint
reported **0x80070490**, joined, and left canonical/media access intact. Earlier malformed-ID checks
reported **0x80070057**; that was argument refusal, not physical absence/removal.

The first measurement series failed independent-oracle acceptance for baseline 1/8 despite exact
offline PCM parity: the independent oracle omitted a rounded-early next-loop onset (error 0.0025).
Earlier short smoke captures also included Stop silence and one test note unexpectedly quantized to
one frame at a later loop. These were corrected in verification, without loosening tolerance/budgets
or changing production musical conversion. The complete reviewed series was rerun and passed.

The conservative fault policy does not synchronize elapsed transport through starvation: stop the
execution/stream, retain ownership through join, then require deliberate fresh initialization. Raw
device clock values are interpreted with queried frequency; the API's correlated QPC uses 100 ns units
([Microsoft IAudioClock documentation](https://learn.microsoft.com/en-us/windows/win32/api/audioclient/nf-audioclient-iaudioclock-getposition)).
Canonical ticks, prepared absolute frames, runtime render/epoch, submission, device clock and elapsed
QPC stay separate. Nothing adds device/QPC identities to R1 serialization.

Missing evidence: actual hardware removal/change, prolonged clock discontinuities, multi-hour stability,
lower device periods, DAC/acoustic latency, genuine hardware dropout counters, ETW attribution,
other endpoint/platform/runtime configurations and clean delivered packaging. Native error services
that exit before packet completion are diagnostics, not completed timing samples. Handle/private-byte
growth was not independently profiled; joined COM/event cleanup and prepared-state counters are scoped
ownership evidence. No seamless continuation, arbitrary graph/plugin lifetimes or release support.

## Reproduction, artifacts and stage disposition

From repository root:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll measure 60
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll pressure 60
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll faults
```

Locked restore and full managed Release solution build passed with **0 warnings/errors**.
Full xUnit/MTP suite passed **215 tests, 0 failures/skips**, preserving all previous 199.
The [harness guide](../../tools/Seqvium.DeviceCheck/README.md) separates explicit hardware checks.

Local ignored JSON-lines: `tools/Seqvium.DeviceCheck/bin/f2-final.jsonl` (first failed oracle),
`f2-reviewed.jsonl` (complete accepted series), `f2-inflight.jsonl` (in-flight supplement),
`f2-sound-pressure.jsonl` (sound revision supplement), `f2-faults-final.jsonl` (final faults).
No fabricated CI, native comparison or acoustic measurement is recorded.

Main reviewed artifact SHA-256:
Core `6C3717A8439FED3107F9524379459610253A9B6A2AC6AB329A09B5A5687E58AE`;
Windows adapter `6136C89AEB9D3D2EADAF3ECB1D1B35F18F723C2AF936450B3A94351C466E9FFA`;
harness `9A547ACEE9E3CB599AFF1DD492481E041601B88532C80ADACB430FF10C834284`.
In-flight/final-fault harness `1F2B9D519F6F11898B389ED983C7C36804DB0E7ECDF8C9B9F2281456204F0298`.
Only harness workload/verification changed between these phases; production kernel/adapter are identical.
Final sound-revision harness SHA-256:
`CF52A12DB888F4BEB26B8535237DBB63907F03F318F5F6F7FA5A934DD3E5816C`.
The final source harness changes sound intent instead of name in pressure, so a future full rerun must
identify that workload difference, rather than claim identical control series.

Q-001–Q-007 remain open but gain real managed PCM/device timing, bounded ownership/control, clocks/fault-stop
and shared offline output evidence. No managed failure under the declared workload justifies a native
sampler comparison here. Q-047 gains independent voice/publication/pressure evidence, not routed outputs,
opaque source domains or live state transfer. Q-062 gains startup/injected-fault/join/restart evidence,
not real hardware removal or automatic reselection. Q-069 gains queried adaptation and separated clocks,
not universal synchronization or cross-platform negotiation.

At the F2 checkpoint, R2 was partial: a user-facing minimal source discovery/preview/reuse path and remaining
input/MIDI/capture foundation planning/implementation evidence need a separately scoped follow-up.
F2 does not authorize R3 or close the full R2 roadmap merely because output passed.
Current contracts remain in [Audio](../AUDIO_ENGINE.md#r2-f2-realtime-wav-and-windows-output) and
[Architecture](../ARCHITECTURE.md#r2-f2-execution-and-platform-ownership); this report owns observations.
