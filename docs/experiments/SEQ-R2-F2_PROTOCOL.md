# SEQ-R2-F2 device measurement protocol

Role: Pre-measurement protocol for the bounded production F1 realtime integration.
Read when: Reproducing or interpreting F2 device evidence.
Authoritative for: Declared workload, instrumentation boundaries and acceptance thresholds of this series.
Not authoritative for: Permanent engine/backend, general product latency or architecture.

Declared before final performance collection, 2026-10-08. Initial clean master HEAD
`4b709df6f3def521e1130ac33150992cc8ca982e`. Windows 11 x64, SDK 10.0.401/runtime 10.0.12;
default console output endpoint, WASAPI shared event-driven, dedicated managed worker, MMCSS Pro Audio
where available. Read mix format, period, capacity and clock frequency from the actual endpoint.
Supported adapter scope: float32 mono/stereo 44.1/48 kHz with standard channel order,
capacity 1–65536 frames. Record actual facts; do not change system audio settings.

Selected before final collection: default Line (Steinberg UR12), endpoint
`{0.0.0.00000000}.{af417ee0-95bf-4faf-ac43-7739c2bb6304}`, Yamaha Steinberg USB driver
2.1.9.0 (2025-05-20). Actual 44100 Hz stereo float32, 441-frame/10 ms period,
970-frame capacity, IAudioClock frequency 352800 units/s. This differs from R0's built-in
48 kHz endpoint; no system setting was changed. All timing budgets follow these actual facts.

Use a project-authored one-second 44.1 kHz mono float WAV, imported into lifecycle-owned storage.
One Pattern, quarter-note repeats, pitches root 60 plus fractional increments within one semitone,
intensity 0.5, 70% quarter duration and 50 ms linear release. Starts stagger by 1234 ticks.
Run 1/4/8 independent voices, 120 BPM for four voices and 137 BPM otherwise; 1024 prepared repeats.
No musical event coalescing. A later revision restarts the frozen Pattern at a packet boundary in
a new transport epoch; it never claims continuous live voice transfer.

For each voice configuration: 60 s uninterrupted baseline and 60 s managed/control pressure.
Pressure: every approximately 10 ms allocate 16 x 32768-byte arrays into a fixed 128-entry ring,
issue 256 latest gain writes; every tenth cycle request full blocking compacting GC.
Every twentieth cycle prepare a new identified revision off-thread and publish; periodically exercise
cancelled/stale/superseded/capacity requests. This is deliberately bounded artificial desktop pressure.
Warm 2000 offline blocks before runs; warm the actual worker before prefill. Include residual JIT/runtime
effects in measurements. Ten fresh Open/Start/Stop/Restart/Close/Dispose lifetimes plus fresh reopen.
Separate 30 ms worker stall and injected AUDCLNT_E_DEVICE_INVALIDATED scenarios.

Acceptance for each baseline/pressure: zero observed processor/whole-service period misses,
zero packet-budget misses, zero padding exhaustion; all samples finite, no stuck voices/resources.
Budget is actual period; packet budget is actual frames/rate. Do not relax after results.
Stall may miss budget but must fault-stop the consumer and join without catch-up/backlog.
No seamless recovery or elapsed-time fallback. Invalid endpoint must report HRESULT without canonical
or managed media damage. Injected device invalidation is not physical hardware removal evidence.

Preallocate 20000 service observations and at most 1048576 float capture samples; stop with explicit
instrumentation failure if observations fill. Capture is intentionally a prefix including prefill,
before WASAPI output/conversion. Smoke captures are limited further to half the requested run duration
so final Stop cannot enter the uninterrupted oracle. Compare to offline F1 using identical packet partitions, absolute
tolerance 2e-6, and a separate direct source/interpolation/intensity/release sum from canonical notes
and exact rational boundary conversion. Unit tests additionally cover small ramp/envelope/EOF cases,
1/4/8 voices, 120/137 BPM, rates, zero-frame notes and partition/lifecycle boundaries.

Processor timing: timestamp immediately before Process to immediately after allocation counter read.
Processor allocations: GC.GetAllocatedBytesForCurrentThread surrounding Process only.
Service timing: after native event wait until after padding/clock/GetBuffer/Process/capture/ReleaseBuffer,
including finite/silence/voice observations and deliberate stall. Service allocation counter covers that whole region
and native call return
transitions; it excludes wake/initialization/fault cleanup. Worker allocation covers the running loop
including native waits and their transitions, excluding prefill/setup and final cleanup. Managed runtime
or OS costs not represented by managed allocated-byte counters are not proven absent.
Wake intervals are separate timestamps; clock observations preserve raw position, frequency and
correlated QPC in 100 ns units. Render musical frame and submitted stream frames remain separate.
Do not call stream latency query physical/acoustic latency. Percentiles: nearest rank, after join.
Record all misses, packet range, callback count/duration, process-wide allocations/GC, rejected/cancelled
publication counts, callback Stop acknowledgment, voice observation, retirement and release outcomes.

Reproduction commands and results belong to the [report](SEQ-R2-F2_REPORT.md).
No DAC, listening guarantee, hardware dropout counter, multi-hour, lower hardware-period, other-device,
Linux/macOS, packaging or permanent managed/native choice follows from this protocol.
