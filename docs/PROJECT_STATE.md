# Project state

Role: Compact handoff of current repository truth.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, focus, implemented capability, active blockers.
Not authoritative for: Contracts, decisions, plans, history, or live Git status.

- Checkpoint: SEQ-KB-R3 complete (2026-10-06); development/portability policy and passive configuration validated.
- Implemented capability: documentation/policy foundation with `.editorconfig` / `.gitattributes` and
  [engineering owners](INDEX.md). No application/audio implementation, dependencies, test projects,
  executable tooling, or CI; the pre-existing `Seqvium.sln` contains no projects.
- Current focus: preserve the accepted design and engineering baseline.
  [SEQ-R0](ROADMAP.md#seq-r0--audio-architecture-probe) remains pending / not started.
- Active blockers: none recorded for the policy foundation. Audio architecture remains unvalidated;
  see [open risks](KNOWN_PROBLEMS.md). The FOSS license is unselected.
