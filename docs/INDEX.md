# Documentation index

Role: Navigation map of canonical document owners.
Read when: Selecting task context or locating a durable subject.
Authoritative for: Document discovery and subject-to-owner lookup.
Not authoritative for: The contracts, status, or evidence in linked documents.

Start with [AGENTS](../AGENTS.md) and [PROJECT_STATE](PROJECT_STATE.md), select the relevant owners,
then inspect affected files. Read other documents only to answer a concrete dependency or evidence
question. The [README](../README.md) is the public introduction.

| Owner | Subject / read when |
| --- | --- |
| [AGENTS](../AGENTS.md) | Operational startup, safety, task routing, and verification |
| [PROJECT_STATE](PROJECT_STATE.md) | Current checkpoint, focus, implemented capability, active blockers |
| [PROJECT_VISION](PROJECT_VISION.md) | Product identity, audience, creative philosophy, non-goals |
| [UX_CONTRACT](UX_CONTRACT.md) | Observable interaction principles and general workflow semantics |
| [UI_DESIGN](UI_DESIGN.md) | Evolving visual/interaction guide: hierarchy, chrome, density, indicators, restrained feedback |
| [ARCHITECTURE](ARCHITECTURE.md) | Core/plugin/backend boundaries, musical/resource identities, two local processing levels, semi-free timeline |
| [AUDIO_ENGINE](AUDIO_ENGINE.md) | Realtime state, control boundary, scheduling, offline audio semantics |
| [NODE_GRAPH](NODE_GRAPH.md) | Core graph, free canvas/topology, progressive graph interaction, ports, editable/prepared boundary |
| [WORKSPACE](WORKSPACE.md) | Internal panes, activation/front behavior, docking, user layout state |
| [EXTENSIONS](EXTENSIONS.md) | Optional capabilities, lifecycle, missing-extension preservation |
| [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) | Standalone/contextual Sample Lab, audition, acceptance, what-you-hear resampling |
| [PROJECT_FORMAT](PROJECT_FORMAT.md) | Serialization compatibility, media policy, unknown-data preservation |
| [CODING_GUIDELINES](CODING_GUIDELINES.md) | C# implementation/refactoring, English source language, async/lifetime, warning baseline |
| [DEVELOPMENT](DEVELOPMENT.md) | Developer environment, local tools, SDK/version authority, eventual entry points, text consistency |
| [TEST_EXECUTION](TEST_EXECUTION.md) | Proportional verification, test quality, future commands, Release gates, evidence tiers |
| [PORTABILITY](PORTABILITY.md) | Windows/Linux/macOS target, portable boundaries, conditional native distribution, claim limits |
| [CI_CD](CI_CD.md) | Simple initial managed matrix, docs validation, later feedback/acceptance split, workflow principles |
| [ROADMAP](ROADMAP.md) | Ordered future stages and their scope |
| [DECISIONS_LOG](DECISIONS_LOG.md) | Accepted durable decisions, rationale, explicit supersession |
| [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) | Concrete unresolved questions, risks, validation gaps requiring resolution |
| [IDEAS](IDEAS.md) | Speculative incubator with no acceptance, schedule, or required resolution |
| [TECH_DEBT](TECH_DEBT.md) | Compromises actually present in implemented work |
| [WORK_LOG](WORK_LOG.md) | Concise facts about meaningful completed work |
| [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md) | Ownership rules, conflicts, updates, selective reading, RAG evolution |
| [THIRD_PARTY](THIRD_PARTY.md) | Actual/candidate dependencies and external-resource provenance |
| [Experiment guide](experiments/README.md) | Evidence-report conventions and experiment discovery |

Engineering policy exists before source; it does not imply installed tools or executable test commands.
`docs/FILE_INDEX.md` remains deferred until meaningful source topology exists under
[knowledge evolution](DOCUMENTATION_GOVERNANCE.md#knowledge-evolution). No AgentContext/planner/RAG
infrastructure is present or required.
