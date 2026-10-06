# Documentation governance

Role: Policy for a compact, coherent repository knowledge base.
Read when: Changing ownership/routing, adding documents, or resolving documentation conflicts.
Authoritative for: Ownership rules, update triggers, selective reading, conflicts, history, and RAG evolution.
Not authoritative for: Product/technical contracts, current state, or future product-stage scope.

## One owner per durable subject

[INDEX](INDEX.md) is the subject-to-owner map; each owner's opening metadata defines its boundary.
Use `Role`, `Read when`, `Authoritative for`, and `Not authoritative for` in new owner documents.
The public README may be a concise introduction without that metadata.

Other documents may summarize and link but must not create competing contracts. In particular:

- Vision owns why/for whom; UX owns general observable interaction; sample workflow owns specialized
  discovery/audition/acceptance/resampling semantics.
- Architecture owns cross-boundary responsibilities and intended musical relationships; audio owns
  execution/control/render constraints; extensions own optional-capability lifecycle; project format
  owns serialization and unknown-data preservation.
- [NODE_GRAPH](NODE_GRAPH.md) owns signal-graph semantics, node/port classes, and the editable/prepared
  boundary; audio owns realtime execution constraints and architecture owns separation from timeline
  and user organization. Cross-owner documents link these contracts rather than redefine them.
- [WORKSPACE](WORKSPACE.md) owns internal panes, activation/front behavior, docking, and user layout
  ownership; UX owns general input/feedback principles. Project format owns musical serialization,
  including editable graph data, rather than application/user pane preferences.
- Current state is a compact present-tense handoff. Roadmap defines future stages. Work log records
  completed facts. Decisions log records acceptance/supersession and rationale. Known problems records
  uncertainty. Technical debt records compromises actually present in implementation.

The decisions log links current contract owners rather than becoming a second specification. An
experiment report owns its observations, not final production contracts. AGENTS owns operational
startup/routing and stays short; it must not accumulate product detail or history.

[THIRD_PARTY](THIRD_PARTY.md) is the canonical provenance ledger and candidate evaluation boundary,
including musical content, other assets, repository services/actions, and historical replacements.
Exact installed versions belong to build/package/native manifests or workflows once present; the
ledger links them instead of maintaining a second lock. Asset-local creation/derivation evidence may
be linked from the ledger. README states licensing status and routes here; it does not select a license.

## Selective reading

Follow `AGENTS -> PROJECT_STATE -> INDEX/routing -> selected owners -> affected files`.
Use ordinary `rg` search to find paths, symbols, and relevant sections. Expand context only for a
concrete unresolved boundary or evidence need. Roadmap, work log, decision history, and eventual
archives are not routine startup context.

Canonical documentation is English, with simple precise terminology. Durable knowledge must remain
usable by a fresh agent or contributor without the bootstrap prompt or conversation history.

## Status and conflict resolution

Distinguish **accepted constraint/direction**, **proposal**, **open question**, and **implemented fact**.
Acceptance does not imply implementation. Candidate technologies are not actual dependencies.

Explicit task instructions determine the authorized change. Identify the subject's canonical owner;
a non-owner summary does not override it. Inspect source/tests and evidence when implementation
claims conflict. Repair stale summaries or the evidenced stale owner without silently relaxing an
accepted requirement. Runtime behavior that violates an accepted contract is a discrepancy, not
automatic supersession. Record uncertainty if evidence is incomplete.

When a durable decision changes, explicitly supersede it in the log with rationale and update its
current owner in the same change. If a material choice remains unresolved, preserve the distinction
and ask for needed input rather than invent acceptance. External references never override Seqvium owners.

## Update triggers

| Changed truth | Update |
| --- | --- |
| Current checkpoint, focus, capability, active blocker | PROJECT_STATE; avoid live Git status and transient attempts |
| Product, interaction, responsibility, audio, graph, workspace, sample, extension, or persistence contract | The corresponding owner in the same change |
| Durable accepted/reversed choice | DECISIONS_LOG plus affected owner; link evidence |
| Future scope or stage order | ROADMAP |
| New/narrowed/resolved uncertainty | KNOWN_PROBLEMS; retain evidence of resolution |
| Existing compromise introduced/resolved | TECH_DEBT, with actual evidence |
| Meaningful stage completed | One bounded factual WORK_LOG entry |
| Dependency, service/action, or asset evaluated/introduced/upgraded/replaced/removed; obligations changed | THIRD_PARTY; retain historical provenance and manifest version authority |
| Owner added/moved or route changed | INDEX; AGENTS if operational routing changes |
| Evidence-producing experiment completed | Its bounded report under `docs/experiments/`; update affected owners only for supported conclusions |

Do not update every file after every task. Avoid new progress/handoff documents that duplicate current
state. Preserve useful history; if real obsolete chronology later impedes reading, consider an indexed
archive in a separately justified change. Do not create an archive system before there is history to move.

## Knowledge evolution

The document ownership model stays stable while retrieval mechanisms evolve:

1. **Stage 0 — now:** AGENTS, compact current state, index/routing, canonical owner docs, affected
   files, and ordinary repository search. No generated context infrastructure.
2. **Stage 1 — meaningful source topology:** consider `docs/FILE_INDEX.md`, `docs/TEST_EXECUTION.md`,
   `docs/CODING_GUIDELINES.md`, and small repository baseline tooling. Add one small local workflow
   skill only if repeated agent behavior justifies it; skills are not mandatory infrastructure.
3. **Stage 2 — measurable context-selection problems:** consider a generated repository map and
   bounded context planner inspired by MeasPilot. Generated outputs remain disposable retrieval
   artifacts, not canonical truth. Do not create manifests, budgets, or ProjectStats tools now.
4. **Stage 3 — exact routing/search demonstrably insufficient:** consider a local semantic index,
   hybrid RAG, and possibly MCP exposure. Retrieval must retain source provenance and never silently
   replace Markdown, source, tests, or Git history as authority.

Promote retrieval infrastructure only in response to an observed problem. Report a broken retrieval
tool and fall back to direct owners/search; a planner or index must not narrow the authorized task or
silently redefine the product. Fovium/MeasPilot reference provenance is recorded in [THIRD_PARTY](THIRD_PARTY.md).

## Documentation integrity

Before completion, inspect repository-relative Markdown targets and anchors, metadata/ownership,
status terminology, and all new/changed content. Confirm that future work is not advertised as
implemented, proposals are not accepted by implication, current state stays compact, roadmap contains
forward scope, work log contains facts, debt contains actual compromises, and AGENTS remains a router.
Inspect final Git diff/status, including untracked additions that ordinary `git diff` does not show.
Documentation-only work requires no build/tests unless executable tooling or configuration changes
actually justify them. No persistent checker is required for this foundation.
