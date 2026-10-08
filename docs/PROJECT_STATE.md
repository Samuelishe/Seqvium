# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

SEQ-KB-R18 complete (2026-10-08). [Arrangement](ARCHITECTURE.md#semi-free-arrangement) organizes
supported occurrences separately from reusable music, sound definitions, resources, Instrument Groups,
actual processing membership and Mixer routes. Preferred purpose is not exclusive sound ownership;
compatibility does not require identical content shapes or current executability. Bounded organization
cannot add a third ordinary local processing level. Shared Pattern edits affect its references;
placement-local edits and moves affect the occurrence. Musical variation and independent sound have
separate coherent Undo scopes. Resource/use/shared-definition Sample Lab acceptance explicitly identifies
its target; dependency-aware deletion and Save/reopen preserve intended relationships without heuristics.
Q-028/Q-029 are partially answered; concrete structures, hierarchy/compatibility/defaults, reference/
detachment/deletion, target UI, serialization and evidence remain open. Roadmap responsibility is narrowly
clarified for R1/R5/R7/R8/R9/R10/R12 without pulling Arrangement UI into R1 or defining the full release case.
Musical/signal ownership, independent execution, canonical/derived state, logical Undo/async integrity,
R17 candidate/history, non-destructive durable media, discovery/library, input/accessibility, host
localization/themes, recovery and current/cold-history boundaries remain in force.
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

R18 cases A–T and hypotheses 1–15 checked conceptually against distinct sharing/placement/container,
compatibility, two local levels, signal mixing/domain, deletion, durable-resource, render, Undo and async
contracts. Repository-relative Markdown links/anchors, canonical Q/D identity/reference integrity,
owner/routing, active/cold separation, roadmap IDs/order and unchanged R0 scope, UTF-8/LF/whitespace
and Git preservation checks passed. Documentation-only; no builds/tests, runtime/audio, concurrency,
performance, UI or platform acceptance evidence is claimed.

## Active blockers / evidence gaps

Q-028 needs final terminology/domain structure, compatibility/default targeting and bounded hierarchy
mechanics. Q-029 needs identity/reference/detachment/deletion mechanisms, shared-target/acceptance UI,
serialization coordination and concurrency/capability/performance evidence. One-event extraction and
source linkage/suppression remain Q-048. Graph attachments (Q-019), route/context assignment (Q-030),
Q-008/Q-009 identity/time/format, Q-051 stretch and Q-066 cross-context representation/execution/timing/
lifetime/render/capability/UI remain open. Q-011 retains contextual substitution/restoration; Q-071
retains generation/similarity, controls/locks, candidate/history limits/storage/UI and evidence.
High-impact evidence includes source/domain grouping/voice allocation, opaque-source instancing,
definition synchronization and CPU/RAM behavior (Q-047), recovery/media integrity (Q-058/Q-059), full
milestone dependency closure (Q-061), DSP/tails (Q-057), graph publication/lifetime (Q-018), device timing/
recovery (Q-062/Q-069), undo/async mechanisms (Q-063), input/accessibility/platform validation (Q-064),
Browser/Personal Library mechanisms (Q-065), host-resource lifecycle (Q-067), localization/locale (Q-054)
and theme APIs/packaging (Q-055). Package/negotiation and backend mechanics remain open.
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) contains only open questions. Passive batch-newline policy
contradiction Q-070 remains unresolved; no configuration change is included in this stage.
