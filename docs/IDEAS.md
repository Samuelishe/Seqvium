# Ideas

Role: Low-pressure incubator for speculative product, UX, and architecture ideas.
Read when: Retaining or exploring alternatives that are neither accepted nor scheduled nor required risks.
Authoritative for: Speculative idea records and their promotion/rejection traceability.
Not authoritative for: Accepted product contracts, roadmap commitments, required unresolved risks, implementation tasks, or debt.

```text
IDEAS
  != accepted product contract
  != roadmap commitment
  != unresolved required risk
  != implementation task list
```

An idea may remain here indefinitely without demanding resolution. [ROADMAP](ROADMAP.md) contains
agreed staged intent; [DECISIONS_LOG](DECISIONS_LOG.md) records durable acceptance and supersession;
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) contains concrete questions needing design/evidence/decision;
[TECH_DEBT](TECH_DEBT.md) contains compromises actually present in implementation. Design uncertainty
is not debt. [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md) owns this planning separation.

## Lightweight entries and promotion

Use stable IDs such as `I-001` when references are useful. A useful entry needs a title, status (normally
`Idea`, `Exploring`, `Promoted`, or `Rejected`), value, relevant owner/question, and promotion condition
where known; do not require ten fields for every thought. Promotion updates the appropriate owner,
decision/roadmap/risk record and leaves a link here. Rejection may retain a short reason. This is not a
second roadmap, and exploring does not authorize implementation.

## I-001 — Optional saved workspace arrangements

Status: Idea.
Origin / owner: [WORKSPACE](WORKSPACE.md#layout-ownership-and-restoration), which already mentioned
named/saved workspaces and optional project-specific layouts; they are not initial requirements.
Value: Convenient reuse of a deliberate pane arrangement if real usage demonstrates benefit.
Promotion condition: Concrete workflows justify saved presets and/or optional project-specific
snapshots, with explicit user choice and safe geometry restoration. Preserve normal user/application
layout ownership and resolve persistence with WORKSPACE / PROJECT_FORMAT before acceptance.

Required layout persistence/restoration questions remain Q-022 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
The existing container/resource, compact-chain, inspector, automation/modulation, feedback, parameter-port,
and multiple-graph-pane questions also stay there; they are design obligations, not speculative features.
