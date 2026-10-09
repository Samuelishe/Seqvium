# Explicit Windows audio verification

This small Release harness exercises production Core PCM execution through `Seqvium.Audio.Windows`.
It plays quiet project-authored WAV material on the current default console output; it changes no
device/volume/rate settings. It is not the future workstation, an engine SDK or an ordinary unit test.

From the repository root:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll smoke 2
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll measure 60
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll lifetimes
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll faults
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll pressure 60
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll audition-smoke
```

`measure 60` runs baseline and GC/control pressure for each of 1/4/8 voices, ten fresh lifetimes,
then stall/invalidation/unavailable-endpoint checks. Exit 0 means the requested checks passed; exit 1
records a failed acceptance check. A shorter measure run cannot establish the full protocol.
`pressure 60` separately exercises eight voices and explicitly counts cancellation/supersession issued
while the preparation task is incomplete. It does not replace the full baseline/pressure/lifetime series.
Redirect JSON-lines output into ignored `bin/` for local evidence. Percentiles are median/p95/p99/worst
in microseconds. Failure enums are emitted as numeric values in order declared by `OutputFailure`.
The [protocol](../../docs/experiments/SEQ-R2-F2_PROTOCOL.md) fixes acceptance/instrumentation;
the [report](../../docs/experiments/SEQ-R2-F2_REPORT.md) records observations and limits.

Source material is authored mathematically in `Program.cs`, written as WAV through an independent
writer, imported into an exclusively owned temporary lifecycle directory and decoded by production
F1. Only that fixture directory is removed after joined output and released sampler/preparation owners.
No personal files, music samples or third-party audio are read. Binary/log/capture artifacts are local
ignored outputs; no audio asset, external codec, native sampler DLL or global tool is introduced.

Control/document mutation is caller-serialized. `BeginPreparation` captures the immutable revision and
media roots on that owner. Await preparation before Publish/Dispose. Only one candidate may own
decoded preparation resources, including a completed unpublished result. Invalidate preparation when
canonical authority/target changes; pending consumer generations then reject stale handoff. The
identified old active revision can continue. Reclaim retired state on the control owner.
Close the output and successfully join before disposing its sampler; a join timeout retains ownership.
Transport Stop/Start restarts within the stream; fault recovery uses a fresh output session and new epoch.

## R4-F2 prepared graph verification

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-measure 60
```

This extends the same harness with an actual canonical item graph: 8 independently bound parts/Source,
8 branch Gain, one 8-input Mix, 14 downstream Gain and Output (32 nodes, 31 connections). One quiet
authored mono 44.1 kHz WAV backs eight independent pitched/timed/released occurrences; their output
uses the exact negotiated host layout/rate. Source coefficients are 0.25–0.60; final gain is 0.20.
Output warns before playing and changes no system settings. Duration is bounded to 1–120 seconds
per configuration; the declared long-run command uses 60 baseline plus 60 pressure seconds.

Both configurations use bounded opt-in diagnostics. Baseline's 176,400 captured interleaved samples
are compared to independent source/schedule/envelope/port-ordered Mix arithmetic and offline execution
at the actual captured partitions. Pressure exercises the production coordinator through its serialized
owner event loop, accepted branch edits/source replacements, a CPU worker, a 4 MiB allocation ring and
forced collections. Its dynamically changed capture is not compared to the initial steady-state oracle;
those fields are null. Deadline/finite/allocation/retirement/Stop/Panic/join requirements still apply.

Three additional fresh fault lifetimes inject two native invalidations and one worker stall. These are
bounded device-worker tests, not actual hardware unplugging. Preparation joins before device Close;
borrowed resources are released only after confirmed worker join, including exceptional cleanup.
Amplitude, adjacent-sample and packet-boundary deltas are diagnostic observations, not subjective click
assessment, DAC latency or microphone capture. Timing percentiles exclude the prefill/cold setup.
The [R4-F2 report](../../docs/experiments/SEQ-R4-F2_REPORT.md) owns actual endpoints/durations/workload,
earlier runs, measurements and limits; ordinary tests initialize no physical audio.

## R4-F2 starvation attribution variants

These commands **play quiet authored sound** and warn on stderr. They change no system volume, endpoint
defaults, period/capacity, MMCSS policy or GC mode. After the Release build:

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-diagnose full 60 3 "<output ID>" gc-trace
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-diagnose natural 60 3 "<output ID>" gc-trace
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-diagnose forced 60 3 "<output ID>" gc-trace
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-diagnose legacy 60 3 "<output ID>" gc-trace
```

Duration is 1–120 seconds; repeat count is 1–3. The optional endpoint ID selects that exact output,
without fallback. Omit `gc-trace` for a comparison without native runtime event delivery. Declare the
series before running, retain every failure, and avoid concurrent builds/tests or other injected loads.

| Variant | CPU worker | 16 × 32 KiB allocations / cycle | Canonical edits | Forced compacting Gen2 / 100 cycles | PCM path |
| --- | --- | --- | --- | --- | --- |
| full | 1 | yes | Gain / 10, source / 50 cycles | yes | original 32-node graph |
| natural | 1 | yes | same | no | same graph |
| forced | 0 | no | no | yes | fixed same graph |
| legacy | 1 | yes | same external graph/source edits | yes | graph-free existing sampler |
| cpu | 1 | no | no | no | fixed graph |
| allocations | 0 | yes | no | no | fixed graph |
| edits | 0 | no | same | no | graph |

The original `graph-measure` baseline/pressure/fault workload and acceptance requirements remain in
force. `natural` removes only explicit collections; natural GC still runs. `cpu` and `edits` are secondary
diagnostics if the first four variants do not localize the problem. Successful diagnostics do not replace
the original forced-GC acceptance result. `allocations` additionally isolates the allocation ring without
the CPU worker, edits or explicit collections; it is available for narrower follow-up rather than run by default.

Legacy uses the identical WAV bytes, eight parts/voices, note offsets/pitches/intensities/releases,
tempo/repeats, endpoint/rate/layout/capacity and `RealtimeSampler`/WASAPI path. A fixture-local saved/reopened
projection removes placements/graphs for Pattern preparation. External canonical graph Gain edits still
occur; mirrored source edits publish legacy replacements. DSP differs: legacy sums voices directly,
has no 22 Gain operations/Mix/scratch or graph transition fade, and uses uniform output gain 0.1 instead
of branch gains 0.25–0.60 followed by 0.2. Its extra mirrored source transaction is reported as a control
comparison limit, not equivalent DSP or a historical UR12 measurement.

All audio observations stay in fixed preallocated storage. `ProblemWindow` retains up to eleven service
records around exhaustion or the largest service-start interval: wait entry/return, service/query,
PCM start/end, submission end, sequence, padding, clock and submitted frames on Stopwatch/QPC time.
`WakeTicks` continues to mean service-start interval; no OS signal timestamp is measured.
Wait timestamps bracket the P/Invoke on the managed worker; they include managed/native transitions
and possible runtime suspension, rather than isolating time spent inside the OS wait.
The control harness brackets each explicit `GC.Collect`. This call duration includes more than proven
runtime suspension. Optional harness-only BCL `EventListener` enables runtime GC keyword 1 at Informational
level, retaining at most 20,000 GCStart/End, SuspendBegin/End and RestartBegin/End records. Native event
UTC timestamps map to QPC using bracketed initial/final clock anchors; asynchronous receipt time is
retained separately. Check anchor drift, dropped events and observer overhead before attributing a gap.
No file output, listener call, allocation or control-thread wait is added to PCM processing.

Process/GC/load counters end at the declared workload boundary, before Stop/Panic, cleanup, oracle and
JSON reporting. Output/lifetime totals explicitly cover playback through Stop/Panic and joined shutdown;
timing records used for workload quantiles are limited by the workload end timestamp. Raw JSON/traces
belong only in ignored `.artifacts/SEQ-R4-F2/`; conclusions extend the existing R4-F2 report.

## Transient preview smoke and diagnostic mode

Audible commands warn on stderr that sound **will play**. `audition-smoke` authors an external one-second
220 Hz sine WAV (amplitude 0.02) in a private UUID fixture, with initial preview gain 0.05. It imports
nothing and checks two fresh sessions: normal diagnostics-off, then opt-in bounded capture/observations.
Checks include initial silence, source deletion after preparation, EOF, Stop, replacement, live cancel,
project Close, unchanged canonical state/resources/history and joined temporary-PCM release.
An independent raw-source interpolation oracle verifies the first complete captured preview and silence.
This is not acoustic latency or a full F2 performance series; details are in the
[F3 report](../../docs/experiments/SEQ-R2-F3_REPORT.md).

`WasapiOutput()` defaults to no capture/observation/partition payload or per-service allocation/finite/
silence instrumentation. Clock/padding/fault-stop and resource ownership remain enabled. Default-mode
zero timing/allocation fields mean **not measured**. All existing F2 harness paths explicitly pass
`diagnostics: true`, preserving reporting and instrumentation capacity failure. Supplying a nonzero
capture capacity without diagnostics is refused; arrays stay bounded when enabled. No system volume,
sample-rate, endpoint preference or other audio setting is changed.

## Endpoint snapshot and independent selection

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll endpoints
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll endpoint-session "<output endpoint ID>"
```

`endpoints` is silent: it enumerates all render/capture states and Console/Multimedia/Communications
defaults, without activating output/capture clients. Its names and opaque IDs describe only this machine.
Device-level capture entries are not channel/microphone or tested-recording capabilities.

`endpoint-session` requires a supplied output ID. It **plays sound**, warns explicitly and uses an
authored 220 Hz sine at amplitude 0.02 with sampler gain 0.05 (peak about 0.001). Invoke it only for a
known desired output. Three fresh sessions test selected output, independent real/default input intent,
Stop acknowledgment, active-session selection change/join/state release and injected invalidation/reopen.
Missing output and native inactive/wrong-direction IDs must refuse without fallback. All three role
defaults are additionally opened without Start (silent). No system volume/default/rate is changed.
No capture stream is opened; PCM diagnostics copy render-buffer samples before submission, never
microphone audio. It is not the full F2 performance protocol. Results/limits and complete R2 audit are
in the [F4 report](../../docs/experiments/SEQ-R2-F4_REPORT.md).
