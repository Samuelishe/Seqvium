# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

SEQ-KB-R20 complete (2026-10-08). [Portability](PORTABILITY.md#platform-evidence-and-support-scope)
separates architectural targets, source/build, hosted checks, native loading/interop, actual desktop,
audio-device, packaged distribution and public support scope. Windows is the primary early development/
validation environment; Windows/Linux/macOS remain first-class architectural targets. Q-068 is resolved
at product-policy level: each declared release needs evidence for its actual environment/capability scope;
neither common source, hosted checks nor an open editor establishes full DAW support. WSLg evidence is
bounded to Linux execution, graphical interaction and bridged audio there, not native Linux acceptance.
[Roadmap](ROADMAP.md#platform-release-acceptance-ownership) assigns bounded R0–R12/later responsibility;
R12 declares and validates intended distribution targets without mandatory simultaneous parity or
arbitrary Linux/macOS deferral. Q-041 retains exact target/OS/CPU/RID/device/backend and delivery choices;
Q-061 retains full release-scenario closure. Existing portable document/managed-media, degraded access,
Save/recovery/Undo/async ownership, musical relationships and presentation contracts remain in force.
No implementation stage has started.

## Implemented capability

Documentation/legal/policy foundation, root [LICENSE](../LICENSE), and passive `.editorconfig` /
`.gitattributes`. No application/audio/experiment implementation, dependencies, test projects,
executable tooling, prototypes, plugin hosts or CI exists; pre-existing `Seqvium.sln` contains no projects.
Accepted runtime/product requirements are future contracts, not running capabilities.

## Current focus

Preserve the accepted foundation and rolling state-based maintenance under
[governance](DOCUMENTATION_GOVERNANCE.md#rolling-current-knowledge-and-cold-history). History is excluded
from normal startup/default retrieval; current contracts remain in active owners. Executable work
requires explicit authorization. [SEQ-R0](ROADMAP.md#seq-r0--audio-architecture-probe) remains
**pending / not started**; [ProjectStats](PROJECT_STATS.md) is a future contract, not a runnable tool.

## Validation baseline

R20 cases A–T and support/release hypotheses checked conceptually against actual evidence scope,
portable projects, desktop/device/distribution acceptance and bounded stage ownership.
Repository-relative Markdown links/anchors, canonical Q/D identity/reference integrity, owner/routing,
active/cold separation, roadmap IDs/order and unchanged R0 scope, UTF-8/LF/whitespace and Git preservation
checks passed. Documentation-only; no builds/tests, runtime/audio/hardware, UI, packaged-distribution,
storage/fault/race or platform acceptance evidence is claimed.

## Active blockers / evidence gaps

Q-028 needs final terminology/domain structure, compatibility/default targeting and bounded hierarchy
mechanics. Q-029 needs identity/reference/detachment/deletion mechanisms, shared-target/acceptance UI,
serialization coordination and concurrency/capability/performance evidence. One-event extraction and
source linkage/suppression remain Q-048. Graph attachments (Q-019), route/context assignment (Q-030),
Q-008/Q-009 identity/time/format, Q-051 stretch and Q-066 cross-context representation/execution/timing/
lifetime/render/capability/UI remain open. Q-011 retains contextual substitution/restoration; Q-071
retains generation/similarity, controls/locks, candidate/history limits/storage/UI and evidence.
High-impact evidence includes source/domain grouping/voice allocation, opaque-source instancing,
definition synchronization and CPU/RAM behavior (Q-047), concrete recovery replacement/candidate and
durable-media protocol/GC/reconciliation with platform/fault evidence (Q-058/Q-059), full
milestone dependency closure (Q-061), DSP/tails (Q-057), graph publication/lifetime (Q-018), device timing/
recovery (Q-062/Q-069), undo/async mechanisms (Q-063), input/accessibility/platform validation (Q-064),
Browser/Personal Library mechanisms (Q-065), host-resource lifecycle (Q-067), localization/locale (Q-054)
and theme APIs/packaging (Q-055). Package/negotiation and backend mechanics remain open.
Concrete supported platform/CPU/RID/device scopes, delivery prerequisites and actual desktop/audio/
distribution/storage evidence remain Q-041 and related subsystem questions; no release dates selected.
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) contains only open questions. Passive batch-newline policy
contradiction Q-070 remains unresolved; no configuration change is included in this stage.
