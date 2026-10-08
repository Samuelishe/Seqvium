# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

**SEQ-R1 is complete / local accepted-ready** for its bounded domain/document foundation.
The SEQ-KB-R21 planning checkpoint remains the planning baseline. SEQ-R0 is still **partially evidenced**,
recommendation **narrow**; its [report](experiments/SEQ-R0_REPORT.md) does not choose a permanent engine.
**SEQ-R2 is in progress / partial**. Separately authorized R2-F1 is locally accepted-ready for managed
WAV media and offline sampler foundations; it is a work package within R2, not a new numbered stage.
The reviewed initial bounded C# scheduler/DSP direction keeps execution/device replaceable; no permanent
engine is adopted. Completed facts/rationale are in the cold archive; current contracts stay in owners.

## Implemented capability

[Seqvium.sln](../Seqvium.sln) now contains the .NET 10 `Seqvium.Core` library and one `Seqvium.Tests`
project. R1 implements unnamed document lifecycle, immutable canonical state and typed stable UUIDs;
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
There is no workstation/UI, realtime callback/device F2, graph engine, plugin host, export, recording or
recovery engine. [SEQ-R0 source](../experiments/seq-r0/README.md) remains independent.

## Current focus

Review F1 before separately authorizing F2 realtime/device evidence with the real WAV workload.
Canonical musical state remains independent of WASAPI/native ABI, sample frames, periods and device
rates. Intended-workload, managed-pressure, period/device, elapsed-time recovery and clean-distribution
evidence remain necessary. First Track sequence/scenario remains unchanged under [ROADMAP](ROADMAP.md).

## Validation baseline

Local Windows x64, SDK 10.0.401 / runtime 10.0.12: locked restore, full Release solution build with
**zero warnings/errors**, and **192 passing tests, zero failures/skips**, preserving all 89 R1 tests. Commands are in
[DEVELOPMENT](DEVELOPMENT.md) and [TEST_EXECUTION](TEST_EXECUTION.md).
Tests use tiny generated WAV/synthetic data, owned temporary directories and deterministic async gates,
without sleeps. They cover R1 contracts plus malformed media, unnamed/source-removal/Save As ownership,
stale/cancelled/interrupted/storage-failed preparation, partial transfer/degraded access, pitch/channel/
intensity/EOF/release, independent 1/4/8 voices, 120/137 BPM and 44.1/48 kHz block/loop invariance.
No hosted Linux/macOS, production GUI/device, power-loss/crash consistency or distribution
acceptance is claimed. R0's bounded 48 kHz / 10 ms Windows observations remain experimental.

## Active blockers / evidence gaps

Q-008's bounded schema/time/identity and Q-039's initial managed test setup are resolved.
Q-009 retains broader packaging/migrations beyond F1; Q-019 retains graph attachment/execution;
Q-029 retains expanded detachment/deletion/acceptance UI and capability/concurrency evidence; Q-063
retains dependency-specific rebase, broader pending-work races/adapters and larger history policy.
Q-001–Q-007 remain open; Q-005/Q-006 gain bounded offline evidence without realtime parity claims.
Q-028/Q-030 retain full organization/context assignment; Q-047 execution-domain/source-capability and
performance evidence beyond F1 voices; Q-048 event extraction; Q-051 audio-time/stretch; Q-058/Q-059
recovery, repair/GC and wider crash/fault/race/platform-safe media ownership; Q-061 whole First Track.
Conservative accepted-file retention can grow disk use; no unnamed musical-state recovery is implemented.
Other future subsystem gaps remain in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md), which contains only unresolved questions.
No CI, ProjectStats, global tool installation or new release/platform support claim is introduced.
Q-070's passive batch-newline policy remains outside this stage.
