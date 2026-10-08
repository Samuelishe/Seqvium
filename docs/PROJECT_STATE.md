# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

SEQ-KB-R19 complete (2026-10-08). [Project integrity](PROJECT_FORMAT.md#media-and-persistence-integrity)
distinguishes working, explicitly saved and recoverable revisions; accepted media, staging/cache,
Undo/Redo, recovery, pending and independent Personal Library owners. Unnamed work/media has lifetime
before first Save. Recovery choice does not Save; replacement protects a known valid candidate until
a valid successor exists. Failed Save/Save As/transfer preserves prior coherent state and unsaved work;
ambiguous completion requires evidence-based reconciliation. Degraded media/plugin access, coherent
repair, interrupted-recording honesty and owner/coverage-based cleanup are accepted contracts.
Q-058/Q-059 are partially answered, not closed: storage/protocol, candidate indexing/selection,
integrity validation, retention/cadence, GC, recording/transfer and platform/fault/race evidence remain.
Bounded R1/R2/R7/R8/R12 responsibilities are clarified without making complete recovery/storage an R1
prerequisite or defining Q-061's full release case. Arrangement/sharing, independent execution,
canonical/derived state, logical Undo/async integrity, disposable candidate history, discovery/library,
input/accessibility, localization/themes and current/cold-history boundaries remain in force.
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

R19 cases A–T, principles 1–15 and five operation failure boundaries checked conceptually against
Save/recovery, ownership, degraded opening/repair, async/Undo and durability evidence limits.
Repository-relative Markdown links/anchors, canonical Q/D identity/reference integrity, owner/routing,
active/cold separation, roadmap IDs/order and unchanged R0 scope, UTF-8/LF/whitespace and Git preservation
checks passed. Documentation-only; no builds/tests, fault injection, runtime/audio, concurrency,
performance, UI or platform durability evidence is claimed.

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
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) contains only open questions. Passive batch-newline policy
contradiction Q-070 remains unresolved; no configuration change is included in this stage.
