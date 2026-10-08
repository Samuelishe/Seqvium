# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence
gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

**SEQ-R1 is complete / local accepted-ready** for its bounded domain/document foundation.
The SEQ-KB-R21 planning checkpoint remains the planning baseline. SEQ-R0 is still **partially evidenced**,
recommendation **narrow**; its [report](experiments/SEQ-R0_REPORT.md) does not choose a permanent engine. **SEQ-R2 is in
progress / partial**. Separately authorized R2-F1 is locally accepted-ready for managed
WAV media and offline sampler foundations; it is a work package within R2, not a new numbered stage.
Separately authorized **R2-F2 is locally accepted-ready** for shared managed WAV realtime execution,
bounded preparation/publication/retirement and Windows output under its [report](experiments/SEQ-R2-F2_REPORT.md).
**R2-F3 is locally accepted-ready** for bounded filesystem/project discovery, transient raw WAV audition
and explicit independent sound reuse under its [report](experiments/SEQ-R2-F3_REPORT.md).
The reviewed initial bounded C# scheduler/DSP direction keeps execution/device replaceable; no permanent
engine is adopted. Completed facts/rationale are in the cold archive; current contracts stay in owners.

## Implemented capability

[Seqvium.sln](../Seqvium.sln) now contains the .NET 10 `Seqvium.Core` library and one `Seqvium.Tests`
project, plus the narrow Windows output library and explicit physical verification harness.
R1 implements unnamed document lifecycle, immutable canonical state and typed stable UUIDs;
named multi-instrument Patterns, identified parts/pitch-aware notes, shared placements, musical
variations, independent durable sound configuration, distinct flat Instrument Groups/resource
descriptors, and bounded local processing/route/performance-interaction intent.
Validated logical edits, dependency-aware deletion rejection and bounded session Undo/Redo preserve
coherent relationships. Current/saved revisions, lifecycle and transition stamps stay distinct.

Musical time uses precise Int64 quarter-note ticks with exact constant-tempo conversion and deterministic
equal-position ordering under [architecture](ARCHITECTURE.md#r1-canonical-foundation).
Versioned JSON Save/Save As/reopen preserves sharing, project settings, compatible unknown fields and
opaque extension state; degraded diagnostics and structural/version refusal follow the
[format](PROJECT_FORMAT.md#r1-canonical-json-format).
F1 adds bounded PCM16/float32 WAV decoding (mono/stereo, 44.1/48 kHz), durable unnamed project import,
targeted sampler configuration through the owning edit/generation/cancellation gate, associated media
Save As and integrity-validated/degraded reopen/Save. Original source disappearance does not break
accepted audio. Immutable decoded leases support independent offline pitched voices, explicit root/
release, absolute prepared note events and deterministic block/loop execution under
[audio](AUDIO_ENGINE.md#r2-f1-offline-sampler-foundation).
F2 reuses F1 PCM/events/voices with finite variable packets, sticky Stop/Panic, new transport epochs,
captured revision gates, one preparation candidate and active/pending/retired ownership. WASAPI output
queries endpoint/rate/channels/period/capacity/clock, owns joined lifetimes and safely fault-stops without
editing canonical music/media. There is no workstation/UI, graph engine, plugin host, export, recording or
recovery engine. [SEQ-R0 source](../experiments/seq-r0/README.md) remains independent.
F3 separates transient sources from accepted resources: one-level bounded access, one-shot preview with
no project edit/storage, safe cancellation/Close/retirement, and fresh sound configuration over one
existing ResourceId without copying bytes or retargeting other uses. WASAPI diagnostics are opt-in.

## Current focus

Scope remaining R2 separate logical input/output endpoint discovery/selection foundation; do not start R3.
MIDI/capture ownership is planned, not streaming/recording/editor implementation. Source access is complete
for its bounded API scope; broader catalog/contextual audition/Personal Library remains later work.
Canonical musical state remains independent of WASAPI/native ABI, sample frames, periods and device
rates. Wider periods/workloads/devices, clock/recovery and clean distribution remain evidence gaps.
First Track sequence/scenario remains unchanged under [ROADMAP](ROADMAP.md).

## Validation baseline

Local Windows x64, SDK 10.0.401 / runtime 10.0.12: locked restore, full Release solution build with **zero
warnings/errors**, and **251 passing tests, zero failures/skips**, preserving all 215 prior tests (including 89 R1
tests). Commands are in
[DEVELOPMENT](DEVELOPMENT.md) and [TEST_EXECUTION](TEST_EXECUTION.md).
Tests use tiny generated WAV/synthetic data, owned temporary directories and deterministic async gates,
without sleeps. They cover R1 contracts plus malformed media, unnamed/source-removal/Save As ownership,
stale/cancelled/interrupted/storage-failed preparation, partial transfer/degraded access, pitch/channel/
intensity/EOF/release, independent 1/4/8 voices, 120/137 BPM and 44.1/48 kHz block/loop invariance.
F2 physically validates Steinberg UR12, 44.1 kHz stereo float32 / 10 ms / 970 frames: six 60 s baseline/
GC/control runs at 1/4/8 voices and two eight-voice supplements, with zero period/packet misses or padding
exhaustion. Capture/offline error 0; independent oracle below 2e-6. Ten fresh lifetimes and explicit
unavailable/invalidation/stall checks pass; 30 ms stall faults safely with one miss/exhaustion per run.
Timing/allocation boundaries and limits are in the report. No DAC latency/dropout, hardware removal,
multi-hour, hosted Linux/macOS, GUI, power-loss/crash or distribution acceptance is claimed.
F3's two fresh physical preview sessions pass EOF/Stop/replacement/cancel/Close with 6/6 state release
each; default diagnostic payload 0 bytes, capture oracle error 5.53e-11. Post-change F2 short 1/4/8-voice
smoke and eight-voice 60 s pressure pass with zero misses/exhaustion; the full F2 series was not repeated.

## Active blockers / evidence gaps

Q-008's bounded schema/time/identity and Q-039's initial managed test setup are resolved.
Q-009 retains broader packaging/migrations beyond F1; Q-019 retains graph attachment/execution;
Q-029 retains expanded detachment/deletion/acceptance UI and capability/concurrency evidence; Q-063
retains dependency-specific rebase, broader pending-work races/adapters and larger history policy.
Q-001–Q-007 remain open with bounded F2 workload/lifetime/clock/fault/parity evidence; Q-062/Q-069 retain
real hardware loss/change, broader recovery/negotiation/synchronization and platform evidence.
Separate input endpoint discovery/selection is a concrete remaining R2 gap; MIDI/capture timing and
broader device/graph/platform questions do not require implementing recording or a permanent SDK now.
Q-028/Q-030 retain full organization/context assignment; Q-047 execution-domain/source-capability and
performance evidence beyond F1/F2 PCM voices; Q-048 event extraction; Q-051 audio-time/stretch; Q-058/Q-059
recovery, repair/GC and wider crash/fault/race/platform-safe media ownership; Q-061 whole First Track.
Conservative accepted-file retention can grow disk use; no unnamed musical-state recovery is implemented.
Other future subsystem gaps remain in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md), which contains only unresolved questions.
No CI, ProjectStats, global tool installation or new release/platform support claim is introduced.
Q-070's passive batch-newline policy remains outside this stage.
