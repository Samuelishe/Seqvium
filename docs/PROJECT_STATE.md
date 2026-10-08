# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

**SEQ-R1 is complete / local accepted-ready** for its bounded domain/document foundation.
The SEQ-KB-R21 planning checkpoint remains the planning baseline. SEQ-R0 is still **partially evidenced**,
recommendation **narrow**; its [report](experiments/SEQ-R0_REPORT.md) does not choose a permanent engine.
SEQ-R2 is pending and requires separate authorization plus a reviewed pre-production audio boundary.
Completed R1 facts/rationale are in the cold archive; current contracts stay in their owners.

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
[format](PROJECT_FORMAT.md#r1-canonical-json-format). R1 does not import media or instantiate processing.
There is no workstation executable, UI, production audio/backend/DSP/graph, plugin host, export,
recording or recovery engine. [SEQ-R0 source](../experiments/seq-r0/README.md) remains independent.

## Current focus

Review the completed R1 foundation and narrow R0 findings before separately authorizing R2.
Canonical musical state is independent of WASAPI/native ABI, callback periods and device rates.
Before production audio, review backend/ABI/DSP strategy and required intended-workload, period/device,
elapsed-time recovery and clean-distribution evidence. No permanent engine decision follows from
domain tests. First Track stage order/scenario remain unchanged under [ROADMAP](ROADMAP.md).

## Validation baseline

Local Windows x64, SDK 10.0.401 / runtime 10.0.12: locked restore, full Release solution build with
**zero warnings/errors**, and **89 passing tests, zero failures/skips**. Commands are in
[DEVELOPMENT](DEVELOPMENT.md) and [TEST_EXECUTION](TEST_EXECUTION.md).
Tests use synthetic data/owned temporary directories and no arbitrary sleeps. They exercise sharing,
variation/sound independence, local relationships, coherent history/rejection/deletion, time boundaries/
rounding/long ranges/drift, round-trip versions/unknown/opaque data and normal-process Save failures.
No hosted Linux/macOS, production GUI/device, media integrity, crash consistency or distribution
acceptance is claimed. R0's bounded 48 kHz / 10 ms Windows observations remain experimental.

## Active blockers / evidence gaps

Q-008's bounded schema/time/identity and Q-039's initial managed test setup are resolved.
Q-009 retains media-capable packaging and future migrations; Q-019 retains graph attachment/execution;
Q-029 retains expanded detachment/deletion/acceptance UI and capability/concurrency evidence; Q-063
retains dependency-specific async gates, real pending-work races/adapters and larger history policy.
These remaining gaps do not block bounded R1 acceptance. Q-001–Q-007 remain open before audio claims.
Q-028/Q-030 retain full organization/context assignment; Q-047 execution-domain/source-capability and
performance evidence; Q-048 event extraction; Q-051 audio-time/stretch; Q-058/Q-059 physical media,
recovery and fault/race-safe ownership; Q-061 whole First Track evidence. Other future subsystem gaps
remain in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md), which contains only unresolved questions.
No CI, ProjectStats, global tool installation or new release/platform support claim is introduced.
Q-070's passive batch-newline policy remains outside this stage.
