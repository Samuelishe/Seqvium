# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

SEQ-KB-R21 planning checkpoint complete. SEQ-R0 bounded experimental work has run (2026-10-08) and is
**partially evidenced**, with recommendation **narrow**. The [report](experiments/SEQ-R0_REPORT.md)
records actual Windows device/managed/native/offline evidence and remaining acceptance gaps.
Both execution candidates meet the measured 10 ms shared-mode budget for the synthetic workload;
native has smaller measured timing tails under managed pressure. Stream-clock starvation needs a
separate elapsed-time recovery boundary. No permanent engine/language/backend/ABI or release target
is adopted. R1 has not started and needs separate authorization.

## Implemented capability

Standalone [experiment source/build/run guide](../experiments/seq-r0/README.md): equivalent C# and C
fixed scheduling/DSP; WASAPI shared event-driven adapter; bounded prepared publication/control;
monotonic sample clock, loops, release/restart/panic, skip recovery, timer-driven checks and offline
oracle/capture comparison. Generated triangle/event fixtures only. Uses installed .NET/MSYS2/Windows
APIs, without downloaded audio libraries or NuGet packages; [provenance](THIRD_PARTY.md) records tools
and linked-runtime obligations. No production application/UI/graph/plugin host/export/persistence,
test framework or CI exists; `Seqvium.sln` remains empty.

## Current focus

Review the bounded findings and remaining R0 scope before an R1 implementation decision. Lower actual
device periods, intended workload, robust clock/device-loss policy and clean distribution remain
unvalidated. Experiment code must not become production by implication. The
[First Track plan](ROADMAP.md#first-track-scenario-and-acceptance-boundary) and owning-stage order
remain unchanged. No additional KB stage or ProjectStats tool is introduced. History stays cold under
[governance](DOCUMENTATION_GOVERNANCE.md#rolling-current-knowledge-and-cold-history).

## Validation baseline

Release managed/native builds pass with zero warnings/errors. The assertion harness verifies 12
candidate/block cases, exact events, generated audio error 0, bounded controls/publication and lifetime.
Actual default Windows endpoint runs at 48 kHz/stereo float32, 480-frame period/1056-frame capacity;
baseline and GC/control pressure have no measured service-period overruns. Deliberate 30 ms stalls
exhaust padding, recover toward elapsed time and acknowledge panic before shutdown. Captured baseline
output matches offline; repeated initialization/shutdown and controlled timer evidence are in the
report. These are local bounded observations, not glitch-free/DAC latency, cross-platform, packaged
distribution or production architecture acceptance.

## Active blockers / evidence gaps

Q-001–Q-007 remain open with narrowed partial evidence in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
Q-028 needs final terminology/domain structure, compatibility/default targeting and bounded hierarchy
mechanics. Q-029 needs identity/reference/detachment/deletion mechanisms, shared-target/acceptance UI,
serialization coordination and concurrency/capability/performance evidence. One-event extraction and
source linkage/suppression remain Q-048. Graph attachments (Q-019), route/context assignment (Q-030),
Q-008/Q-009 identity/time/format, Q-051 stretch and Q-066 cross-context representation/execution/timing/
lifetime/render/capability/UI remain open. Q-011 retains contextual substitution/restoration; Q-071
retains generation/similarity, controls/locks, candidate/history limits/storage/UI and evidence.
High-impact evidence includes source/domain grouping/voice allocation, opaque-source instancing,
definition synchronization and CPU/RAM behavior (Q-047), concrete recovery replacement/candidate and
durable-media protocol/GC/reconciliation with platform/fault evidence (Q-058/Q-059), concrete milestone
dependency/evidence closure (Q-061), DSP/tails (Q-057), graph publication/lifetime (Q-018), device timing/
recovery (Q-062/Q-069), undo/async mechanisms (Q-063), input/accessibility/platform validation (Q-064),
Browser/Personal Library mechanisms (Q-065), host-resource lifecycle (Q-067), localization/locale (Q-054)
and theme APIs/packaging (Q-055). Package/negotiation and backend mechanics remain open.
Concrete supported platform/CPU/RID/device scopes, delivery prerequisites and actual desktop/audio/
distribution/storage evidence remain Q-041 and related subsystem questions; no release dates selected.
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) contains only open questions. Passive batch-newline policy
contradiction Q-070 remains unresolved; no configuration change is included in this stage.
