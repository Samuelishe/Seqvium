# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

SEQ-KB-R13 complete (2026-10-08). Mouse-first, keyboard-efficient creation/editing is accepted under
[Vision](PROJECT_VISION.md#creation-musical-structures-and-organization) and
[UX](UX_CONTRACT.md#mouse-first-creation-and-complementary-input). Semantic actions are independent
of physical input under [Architecture](ARCHITECTURE.md#semantic-actions-and-input-boundary);
[Workspace](WORKSPACE.md#keyboard-access-and-focus-return) owns keyboard window/pane access and focus
return, [UI design](UI_DESIGN.md#feedback-and-motion) owns sufficient non-color feedback. Q-064 remains
open for concrete bindings/routing/navigation, accessibility implementation and platform evidence.
R12 logical Undo, bounded intent-based grouping, transient preview and async commit integrity remain
accepted; Q-063 retains concrete mechanisms/evidence and history persistence/limits. Established
canonical/derived execution, musical/signal ownership, recovery/media integrity and current/cold-history
boundaries remain in force. No implementation stage has started.

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

R13 input/command/accessibility cases A–L, compatibility with R12 Undo, repository-wide relative
Markdown links/anchors, question/decision ID integrity, owner/routing, current/archive separation,
accepted/open scope, UTF-8/LF/whitespace and Git preservation checks passed. Documentation-only validation;
no solution build, tests, runtime/audio or platform acceptance is claimed.

## Active blockers / evidence gaps

Concrete graph-scope references/edit ownership (Q-019) and Arrangement route/control assignment (Q-030)
remain open. High-impact evidence work includes domain grouping, voice allocation, source/plugin instancing,
definition synchronization and measured CPU/RAM/resource behavior (Q-047), exact recovery/media integrity
mechanisms (Q-058/Q-059), future concrete roadmap/complete-project milestone closure (Q-061), stateful DSP/
finite-tail mechanics (Q-057), graph publication/lifetime/failure handling
(Q-018), device timing/recovery (Q-062/Q-069), concrete undo/async commit mechanisms/evidence (Q-063)
and input/focus/accessibility implementation and platform validation (Q-064).
Format/migration, package/negotiation, localization and backend mechanics still require design/evidence.
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) contains only open questions. Passive batch-newline policy
contradiction Q-070 remains unresolved; no configuration change is included in this stage.
