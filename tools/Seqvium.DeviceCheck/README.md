# Explicit Windows audio verification

## Musical polyphony

The opt-in `polyphony` route benchmarks production Core and the same Windows output adapter.
It uses short authored PCM and built-in instruments, with saved fixture projects so before/after
runs preserve note/graph identities, media and float sum order. No personal library, network,
global setting or user process is modified. Timing-dependent checks are outside functional tests.

```text
dotnet build tools/Seqvium.DeviceCheck/Seqvium.DeviceCheck.csproj -c Release --no-restore
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll polyphony cpu .artifacts/polyphony/fixtures .artifacts/polyphony/cpu 1,8,16,32,64 none
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll polyphony physical .artifacts/polyphony/fixtures .artifacts/polyphony/physical 64 none
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll polyphony physical .artifacts/polyphony/fixtures .artifacts/polyphony/pressure 64 background
```

An optional final comma-separated workload list selects `pcm,synth,release,mixed,dense,live`,
plus F2 `synth-expression` (filter ADSR/Velocity/filter LFO) and `synth-vibrato` (filter ADSR/Velocity/pitch LFO).
Low counts below 32 admit PCM and these three Synth cases. Expression fixtures use a 400-ms
amplitude release inside the existing one-second loop, avoiding accidental >64 tail overlap.
Baseline uses a preserved unmodified 32-slot Core/Windows binary
beside this harness; do not replace its Core with the candidate. The accepted executable refuses
65/96/127/128 rather than stealing notes. The [report](../../docs/experiments/SEQ-AUDIO-POLYPHONY-F1_REPORT.md)
records the separately retained 128-slot prototype and its failed pressure acceptance.

CPU uses 48-kHz stereo, 480-frame packets, 16-second Songs (22 seconds with long Release),
two seconds of production callback priming, a restarted measurement, and five warm preparation
observations separate from the cold preparation. Raw QPC ticks, strict current-thread byte deltas,
active/peak voices, actual/end frames, full PCM SHA256, event/work/scratch/decoded accounting,
candidate reservation, retained state and process memory accompany p50/p95/p99/max microseconds.
CPU `Passed` checks zero bytes and natural completion; it is not hardware deadline acceptance.
Preparation, fixture work, hashing, assertions and JSON stay outside callback counter windows.

Physical **plays quiet sound** on the actual default endpoint, without changing system volume or
audio settings. It records negotiated period/capacity/rate/channels/MMCSS and every bounded service
observation, including separate Process and full service durations, actual submitted frames,
misses/padding/starvation, strict byte counts, peak voices, natural EOF, Panic acknowledgment and
joined disposal. Ordinary PCM/Synth/Release/Mixed/Dense captures are compared exactly to the same
offline executor using the actual device partitions. Live/control-gain pressure captures are not
reported as an offline oracle; deterministic live/history tests and CPU hashes are separate evidence.

The criteria are fixed before comparison: p99 Process and service at most half the negotiated
period; maxima at most one period; no deadline/packet misses, padding starvation, invalid/nonfinite
samples or callback/service bytes; exact applicable PCM, natural completion and joined Stop.
Background load replaces a bounded 4-MiB ring with 512 KiB every 5 ms and requests nonblocking Gen2
collections every 20 iterations; the control owner also sends bounded gain bursts. The worker joins
before disposal. Neither GC suppression, excluded windows nor retries until green are used.
Every requested scenario writes JSON, including failures, and any refused/failed scenario returns 1.
Raw artifacts and frozen baseline/prototype binaries remain ignored local evidence, not product media.
Use a fresh output directory for each observation and preserve failures. One endpoint cannot
establish general pressure, acoustic or all-computer acceptance.

## Expressive Synth examples

Use fresh fixture/output directories:

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll polyphony cpu .artifacts/synth-f2/fixtures .artifacts/synth-f2/cpu 1,8,32,64 none synth,synth-expression,synth-vibrato
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll synth-examples .artifacts/synth-f2/examples
```

The latter writes five original short phrases as project/WAV pairs through production Seqvium export,
with sample format, peak/RMS, difference RMS and SHA256 in its local audit. It changes no official demo.

## Drum play-along

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll drum-playalong .artifacts/drum-playalong/physical
```

Measures all 21 factory Drum sounds at both rates, pitch 36/Velocity 0.8, recording full-envelope
peak/RMS and the former solo gain versus unity/default Pattern gain. Then deliberately plays a
small Kick Pattern with eight simultaneous low-velocity preview hits through one physical output.
Captures submitted PCM, replays the actual partitions through a control mirror and records peak,
error, allocation/miss counters and joined lifetime in `physical.json`. Source measurements are
`levels.json`; `synth-levels.json` compares Warm Bass scheduled/held kernels and settled Sustain
at both rates. These are local diagnostic files, not project media. No settings/system volume
change, acoustic microphone/DAC measurement, GUI/native input or long-pressure acceptance is claimed.
Use [VisualCheck](../Seqvium.VisualCheck/README.md#drum-play-along) separately for real input.

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
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll loop-smoke
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll song-smoke
```

`loop-smoke` plays a quiet authored continuous loop for six seconds, edits its notes through the
real coordinator, checks retained position/epoch, Stop acknowledgment, deadlines/allocation counters
and joined teardown. It does not replace acoustic review or the pressure protocol.

`mixer-project <project> <native-mixed-wave> <original-wave> <report-directory>` runs a saved Mixer
Song on the default physical stereo output without changing any supplied file. It captures submitted
float PCM only in bounded memory, compares same-rate/same-partition offline processing, then compares
the GUI-produced 48-kHz PCM16 WAV to its own 48-kHz float render within quantization tolerance.
It reports signal difference, duration/clipping, real meters, allocation/deadline counters and Stop/join
as JSON. The route never creates a musical project or WAV; native GUI authors/export remain separate.
Physical output is not acoustic listening. See [Mixer evidence](../../docs/experiments/SEQ-R11-MIXER_REPORT.md).

`song-smoke` plays two simultaneous quiet Pattern sources in a finite six-second arrangement,
publishes a future clip while playing, and verifies finite Ended, preserved epoch/current position,
simultaneous voices, zero callback allocation, no deadline/padding misses and acknowledged Stop/join.
It reuses production song preparation/renderer/WASAPI; it does not replace owner listening or pressure
acceptance. No system volume/device settings are changed.

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

| Variant     | CPU worker | 16 × 32 KiB allocations / cycle | Canonical edits                  | Forced compacting Gen2 / 100 cycles | PCM path                    |
|-------------|------------|---------------------------------|----------------------------------|-------------------------------------|-----------------------------|
| full        | 1          | yes                             | Gain / 10, source / 50 cycles    | yes                                 | original 32-node graph      |
| natural     | 1          | yes                             | same                             | no                                  | same graph                  |
| forced      | 0          | no                              | no                               | yes                                 | fixed same graph            |
| legacy      | 1          | yes                             | same external graph/source edits | yes                                 | graph-free existing sampler |
| cpu         | 1          | no                              | no                               | no                                  | fixed graph                 |
| allocations | 0          | yes                             | no                               | no                                  | fixed graph                 |
| edits       | 0          | no                              | same                             | no                                  | graph                       |

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

## Read-only sample library and music workflow checks

`sample-check <library-directory> <verification-output>` inspects at most 4096 explicitly supplied
files, decodes WAVs through production Core, reports FLAC separately and renders two piano/two bass
boundaries when available. It writes only to the output directory and opens no audio device.
The raw/smoothed gate deltas are numerical evidence, not acoustic listening or library indexing.

`song-smoke` now also exercises seek, an in-packet range wrap, empty-channel graph add/delete and
Undo/Redo through actual physical output with the production smoothing policy. The same epoch after
seek, convergence, allocation/deadline counters, finite end, Stop acknowledgment and joined teardown
remain asserted. This short quiet check does not replace pressure/acoustic acceptance.
