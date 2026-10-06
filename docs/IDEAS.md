# Ideas

Role: Low-pressure incubator for speculative product, UX, architecture, and repository-tooling ideas.
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

## I-002 — Named test routes

Status: Idea; future tooling only.
Origin / owner: Mature MeasPilot route-registry pattern; [TEST_EXECUTION](TEST_EXECUTION.md).
Value: A single registry could avoid duplicated long filters in documentation/CI. Conceptual names:
`Portable`, `Audio-Offline`, `NodeGraph`, `Serialization`, `CI-Fast`, and `CI-Full`.
Promotion condition: Measured suite growth makes ad-hoc filters costly to maintain; define fast/full
acceptance scope without hiding omitted checks. No route registry, filters, or infrastructure exists now.

## I-003 — Separate semantic repository analysis

Status: Idea.
Origin / owner: [PROJECT_STATS](PROJECT_STATS.md#evolution-boundary).
Value: If semantic architecture/dependency analysis is needed, a separate concern such as
`Seqvium.Tools.RepositoryAnalysis` could own it. ProjectStats might read its results while remaining
structural diagnostics rather than a Roslyn/ABI/plugin authority.
Promotion condition: A concrete semantic-analysis need warrants independent scope and dependencies.
No analyzer or project is selected or scheduled.

## I-004 — Optional ProjectStats trend comparison

Status: Idea.
Origin / owner: [PROJECT_STATS](PROJECT_STATS.md#reports-output-and-evidence-metadata).
Value: Compare deliberately saved reports if historical structural changes later help review.
Promotion condition: Actual use demonstrates value and defines bounded storage/privacy semantics.
No historical metrics database, committed snapshot stream, or trend infrastructure is introduced;
changing statistics do not belong in PROJECT_STATE or ordinary history.
