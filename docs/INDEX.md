# Documentation index

Role: Navigation map of canonical document owners.
Read when: Selecting task context or locating a durable subject.
Authoritative for: Document discovery and subject-to-owner lookup.
Not authoritative for: The contracts, status, or evidence in linked documents.

Start with [AGENTS](../AGENTS.md) and [PROJECT_STATE](PROJECT_STATE.md), select the relevant owners,
then inspect affected files. Read other documents only to answer a concrete dependency or evidence
question. The [README](../README.md) is the public introduction. Active owners contain current
knowledge; `docs/archive/**` is excluded from normal startup/owner context, generated current-context
maps and future default RAG/index corpora. Current and historical retrieval are separate classes;
historical retrieval explicitly opts in only for history/provenance/why/supersession/reconstruction.

| Owner | Subject / read when |
| --- | --- |
| [AGENTS](../AGENTS.md) | Operational startup, safety, task routing, and verification |
| [PROJECT_STATE](PROJECT_STATE.md) | Current checkpoint, implemented capability, focus, validation baseline, blockers/evidence gaps |
| [PROJECT_VISION](PROJECT_VISION.md) | Product identity, audience, creative philosophy, non-goals |
| [UX_CONTRACT](UX_CONTRACT.md) | Observable interaction principles and general workflow semantics |
| [UI_DESIGN](UI_DESIGN.md) | Evolving visual/interaction guide: hierarchy, usable responsive layout, chrome, density, indicators, restrained feedback |
| [ARCHITECTURE](ARCHITECTURE.md) | Foundation-first logical domain/application/adapter/presentation boundaries, project lifecycle, canonical/derived state, async integrity, musical/resource identities and processing scopes |
| [AUDIO_ENGINE](AUDIO_ENGINE.md) | Realtime state/publication, scheduling, devices, item-local hard boundaries, hard export ranges, finite tails and canonical render/preparation |
| [NODE_GRAPH](NODE_GRAPH.md) | Core graph, free canvas/topology, progressive graph interaction, ports, canonical graph and derived execution revisions |
| [WORKSPACE](WORKSPACE.md) | Main-window chrome/foreground behavior, internal panes, activation/front behavior, docking, user layout state |
| [SETTINGS](SETTINGS.md) | User/project separation, logical input/output devices, bounded reset and production diagnostics |
| [EXTENSIONS](EXTENSIONS.md) | Optional capabilities, graded compatibility/fallback, package lifecycle/removal safety, degraded access and dependency blockers |
| [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) | Standalone/contextual Sample Lab, audition, acceptance, what-you-hear resampling |
| [PROJECT_FORMAT](PROJECT_FORMAT.md) | Canonical Save/reopen, rolling recovery, failure-safe managed-media durability, degraded resource access, migration and unknown-data preservation |
| [CODING_GUIDELINES](CODING_GUIDELINES.md) | C# implementation/refactoring, English source language, async/lifetime, warning baseline |
| [DEVELOPMENT](DEVELOPMENT.md) | Developer environment, local tools, SDK/version authority, eventual entry points, text consistency |
| [TEST_EXECUTION](TEST_EXECUTION.md) | Test topology/quality, proportional verification, future commands, Release gates, evidence tiers |
| [PROJECT_STATS](PROJECT_STATS.md) | Future structural diagnostics contract: metrics/reports, advisory signals, privacy/exclusions, evolution boundary; no tool exists |
| [PORTABILITY](PORTABILITY.md) | Windows/Linux/macOS target, portable boundaries, conditional native distribution, claim limits |
| [CI_CD](CI_CD.md) | Simple initial managed matrix, docs validation, later feedback/acceptance split, workflow principles |
| [ROADMAP](ROADMAP.md) | Ordered future stages and their scope |
| [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) | Concrete unresolved questions, risks, validation gaps requiring resolution |
| [IDEAS](IDEAS.md) | Speculative incubator with no acceptance, schedule, or required resolution |
| [TECH_DEBT](TECH_DEBT.md) | Compromises actually present in implemented work |
| [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md) | Ownership rules, conflicts, updates, selective reading, RAG evolution |
| [THIRD_PARTY](THIRD_PARTY.md) | Apache-2.0 project licensing boundary, actual/candidate dependencies and external-resource provenance |
| [LICENSE](../LICENSE) | Complete authoritative Apache License 2.0 text for Seqvium-authored work |
| [Experiment guide](experiments/README.md) | Evidence-report conventions and experiment discovery |

Engineering policy exists before source; it does not imply installed tools or executable test commands.
`docs/FILE_INDEX.md` remains deferred until meaningful source topology exists under
[knowledge evolution](DOCUMENTATION_GOVERNANCE.md#knowledge-evolution). No AgentContext/planner/RAG
infrastructure is present or required.

## Explicit historical lookup

[Cold archive](archive/INDEX.md) contains historical decisions, resolved questions/portions, completed
roadmap/work, previous checkpoints and old audits. It is non-canonical; current owners win conflicts.
This is a historical lookup entry, never a default current-context owner route.
