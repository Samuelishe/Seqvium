# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence
gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

**SEQ-R1, SEQ-R2 and SEQ-R3 are complete / local accepted-ready** within their bounded foundations.
R2-F1/F2/F3/F4 and R3-F1/F2 are working subdivisions, not new numbered stages. The owner accepts
R3-F1 and R3-F2's current bounded workspace usability/interaction behavior. SEQ-R3-CLOSE finds no
missing R3 prerequisite; no additional UI polishing package is required for this acceptance.
**R4 is in progress / partial; R4-F1 is complete / local accepted-ready.** R4-F2's functional execution
is implemented; full acceptance is blocked by mixed physical pressure evidence (two observed starvations).
F2 remains **partial**. F3 is not started and requires separate authorization.
SEQ-KB-R21 remains the planning baseline; R0 remains **partially evidenced / narrow** under its
[report](experiments/SEQ-R0_REPORT.md), without a permanent engine/backend/ABI decision.
Completed rationale is cold history; current contracts stay in canonical owners.

## Implemented capability

The .NET 10 solution contains portable Core, one Tests project, a narrow Windows audio adapter,
an explicit physical-device harness and one Avalonia `Seqvium.Desktop` executable.
[Architecture](ARCHITECTURE.md) owns topology. The silent desktop opens one pristine unnamed
`ProjectDocument.Create` (120 BPM, 4/4); language/theme and layout persist as independent user files.
Windows custom chrome retains OS movement/resize/window actions; other desktops retain native chrome.
Optional Project Inspector and Appearance panes support activation/front order, floating, captured
movement/anchored resize, collapse/restore, reversible close/reopen and explicit left/right docking.
Expanded DIP hit targets use workspace boundary arbitration and frozen capture identity. Pane contents
remain alive; floating panes and the two docks resize independently. Keyboard traversal/actions,
Escape and safe focus return are implemented. [Workspace](WORKSPACE.md#implemented-r3-f2-internal-panes)
owns exact bounds; [Settings](SETTINGS.md#implemented-r3-f2-workspace-layout-storage) owns restoration,
corruption preservation, write failure and joined shutdown. RU/EN fallback and Dark/Light resources
apply without replacing controls or editing music. The compact 30/30/22-DIP frame, 28-DIP pane chrome/
conditional strip and local content scrolling follow [UI design](UI_DESIGN.md).
No musical editor, general docking hierarchy, snapping, audio startup, file-dialog lifecycle or reset UI exists.

R1 provides immutable canonical documents, typed stable identities, shared multi-instrument Patterns,
pitch-aware notes, variations, independent sound/resource/organization/processing intent, validated
atomic edits, bounded Undo/Redo and precise ticks/constant-tempo conversion. Versioned JSON preserves
compatible unknown/opaque state with explicit degraded/refusal behavior.
R2 provides bounded PCM16/float32 WAV decoding, durable unnamed import, managed Save As/reopen integrity,
independent PCM leases, pitched offline/realtime execution, transport/epochs and prepared-state retirement.
External/project discovery, raw transient preview and explicit independent same-resource sound reuse
remain separate from import/Undo. Independent logical input/output intent supports default roles or
explicit opaque IDs, availability without silent fallback and confirmed join before output replacement.
Input discovery/selection opens no capture stream. [Audio](AUDIO_ENGINE.md) and
[Sample workflow](SAMPLE_WORKFLOW.md) own exact resource/execution/device contracts.

R4-F1 adds canonical item graph definitions/attachments, placement/part source bindings, version-1
Source/Gain/Mix/Output descriptors, typed ports, graph coordinates, structural versus intent diagnostics,
atomic editing/remapping/deletion and deep Undo/net-zero equality. Reader 1.1 preserves invalid/unknown
graph intent and requires minor 1 for nonempty graphs. [Node graph](NODE_GRAPH.md#implemented-r4-f1-canonical-graph-intent)
and [Project format](PROJECT_FORMAT.md#r4-f1-graph-aware-json-format) own exact contracts.
R4-F2 executes independent placement/part PCM through Source/Gain/Mix/Output, with checked preparation,
fixed scratch and shared R2 offline/realtime scheduling/leases. One owner coordinator automatically
converges after Edit/Undo/Redo/target changes, retains last-valid in-session audio on blockers, retries
capacity and preserves voices for Gain-only updates. Geometry provenance, atomic status, sticky hard
Stop/Panic, finite topology transition and preparation/device joined shutdown are implemented under
[Audio](AUDIO_ENGINE.md#r4-f2-independent-contributions-and-realtime-convergence). No graph editor exists.

## Current focus

The bounded workspace and canonical intent foundations are locally ready. F2's functional execution
passes verification, but its repeated physical pressure result is not uniformly successful.
F3 actual editor/source/Save flows remain proposed and unstarted under [ROADMAP](ROADMAP.md).
Remaining [readiness recommendations](NODE_GRAPH.md#r4-implementation-readiness-recommendation) grant
no further authorization. R4 remains partial; F2 alone cannot deliver a workstation or select an engine ABI.
Preserve canonical intent, independent user configuration and narrow platform/device lifetimes.
Full Browser/Personal Library, expanded graph/editor/plugins/Mixer/export/recovery and MIDI/capture/recording remain later work.

## Validation baseline

Local Windows 11 x64 (10.0.26300), SDK 10.0.401 / runtime 10.0.12: locked restore and full Release
solution build pass with **zero warnings/errors; all 600 tests pass, zero failures/skips**.
All 558 pre-F2 tests are preserved; 42 focused execution/lifecycle cases are added. [Test execution](TEST_EXECUTION.md#r4-f1-graph-verification)
owns the cases/commands; deterministic tests initialize neither the GUI platform nor physical audio.
[F1 evidence](experiments/SEQ-R3-F1_REPORT.md) records actual chrome/OS keyboard/preference behavior.
[F2 evidence](experiments/SEQ-R3-F2_REPORT.md#resize-hit-targets-and-boundary-arbitration) records 525
completed boundary/cursor/control checks at normal/reduced sizes across RU/EN, Dark/Light and both
front orders, with failed intermediate runs and automation limits separately retained.
The completion audit additionally passes 29 physical GUI checks at 1100x750/96 DPI for restored
geometry, capture/anchors/cancellation, retained instances/focus, keyboard, independent docks,
preference changes and joined shutdown. Four existing user configuration files are restored byte-for-byte.
The R3 audit retained no screenshots/raw logs. F2 raw device JSON remains ignored locally for measurement
review; its durable conclusions are in the compact report below. Desktop loads no Windows audio adapter.
RID-independent local build feasibility is not hosted or Linux/macOS runtime evidence.
[R2-F2](experiments/SEQ-R2-F2_REPORT.md), [F3](experiments/SEQ-R2-F3_REPORT.md) and
[F4](experiments/SEQ-R2-F4_REPORT.md) retain distinct workload/source/device evidence, including physical
UR12 44.1 kHz stereo float32 output/join checks. The independent
[R4-F2 report](experiments/SEQ-R4-F2_REPORT.md) records 32-node/8-source graph execution on the current
High Definition Audio output at 48 kHz stereo, baseline/CPU-GC-control pressure, oracle/parity and joined
fault lifetimes. No acoustic perception, DAC latency, multi-hour/all-device or additional platform claim follows.

## Active blockers / evidence gaps

F2 pressure acceptance is unresolved: successful 60-second runs coexist with padding-exhaustion
starvations at 22.86 and 39.57 seconds under CPU/allocation/forced-GC/control load. The final audit
measured a 30.18 ms service wake interval against 22 ms capacity, with zero callback allocation/deadline
counters; the exact GC/OS attribution remains unresolved. Execution fault-stop/join and lease release worked.
[R4-F2 evidence](experiments/SEQ-R4-F2_REPORT.md) preserves both outcomes and limits. Do not infer
uniform device deadline stability or full F2 acceptance from the passing runs.

No concrete prerequisite blocks bounded R3 closure. Q-053/Q-054/Q-055/Q-064/Q-067 retain broader
settings/reset/migration, contribution resources, future editor targets/native-editor focus and input/
accessibility mechanisms. High/mixed DPI, cross-monitor, screen readers and Linux/macOS runtime remain
unevidenced; Q-041 owns supported distribution targets/prerequisites. These are wider evidence gaps,
not unaccepted F2 behavior or full-platform claims. [Portability](PORTABILITY.md) owns claim limits.
Q-001–Q-007 retain engine/ABI/workload/clock/device/distribution questions; Q-026/Q-027/Q-062/Q-069
retain backends, hardware/default changes, notifications, recovery, input clocks and recording lifetime.
Q-009/Q-019/Q-029/Q-063 retain expanded format/graph/edit/async integrity; Q-028/Q-030/Q-047 retain
organization/routing/execution domains; Q-048/Q-051 audio time; Q-058/Q-059 recovery/repair/GC/storage
faults/races; Q-061 whole First Track closure. Conservative accepted-file retention can grow storage;
unnamed musical-state recovery is absent. Other questions, including Q-070, remain in
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). No CI, global tool or release support is introduced.
