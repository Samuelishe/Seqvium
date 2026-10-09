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
missing R3 prerequisite; no additional UI polishing package is required for this acceptance. **R4 is pending / not
started**, requiring separate authorization.
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

## Current focus

The bounded workspace foundation is accepted. Later graph/editor integration needs separate stage
authorization under [ROADMAP](ROADMAP.md); closure does not start R4 or select a permanent engine.
SEQ-R4-PRE supplies a source-audited [readiness recommendation](NODE_GRAPH.md#r4-implementation-readiness-recommendation)
and proposed F1/F2/F3 gates; concrete schema/runtime choices remain recommendations. R4 is still pending.
Preserve canonical intent, independent user configuration and narrow platform/device lifetimes.
Full Browser/Personal Library, graph/plugins/Mixer/export/recovery and MIDI/capture/recording remain later work.

## Validation baseline

Local Windows 11 x64 (10.0.26300), SDK 10.0.401 / runtime 10.0.12: locked restore and full Release
solution build pass with **zero warnings/errors; all 428 tests pass, zero failures/skips**.
All existing tests are preserved. [Test execution](TEST_EXECUTION.md#r3-f2-workspace-verification)
owns the cases/commands; deterministic tests initialize neither the GUI platform nor physical audio.
[F1 evidence](experiments/SEQ-R3-F1_REPORT.md) records actual chrome/OS keyboard/preference behavior.
[F2 evidence](experiments/SEQ-R3-F2_REPORT.md#resize-hit-targets-and-boundary-arbitration) records 525
completed boundary/cursor/control checks at normal/reduced sizes across RU/EN, Dark/Light and both
front orders, with failed intermediate runs and automation limits separately retained.
The completion audit additionally passes 29 physical GUI checks at 1100x750/96 DPI for restored
geometry, capture/anchors/cancellation, retained instances/focus, keyboard, independent docks,
preference changes and joined shutdown. Four existing user configuration files are restored byte-for-byte.
No screenshots/raw logs or new task artifacts are retained. Desktop loads no Windows audio adapter.
RID-independent local build feasibility is not hosted or Linux/macOS runtime evidence.
[R2-F2](experiments/SEQ-R2-F2_REPORT.md), [F3](experiments/SEQ-R2-F3_REPORT.md) and
[F4](experiments/SEQ-R2-F4_REPORT.md) retain distinct workload/source/device evidence, including physical
UR12 44.1 kHz stereo float32 output/join checks; no audio-device exercise is added by R3 closure.

## Active blockers / evidence gaps

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
