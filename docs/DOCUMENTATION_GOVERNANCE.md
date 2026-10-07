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
- [UI_DESIGN](UI_DESIGN.md) owns evolving visual principles, hierarchy, pane chrome, density, processing
  indicators, and restrained feedback. Route visual-system/design tasks there with UX and the affected
  behavior owner. It does not select final visuals or redefine workflow, pane, or graph semantics.
- Architecture owns cross-boundary responsibilities and intended musical relationships; audio owns
  execution/control/render constraints; extensions own optional-capability lifecycle; project format
  owns serialization and unknown-data preservation.
- [NODE_GRAPH](NODE_GRAPH.md) owns signal-graph semantics, node/port classes, and the editable/prepared
  boundary; audio owns realtime execution constraints and architecture owns separation from timeline
  and user organization. Cross-owner documents link these contracts rather than redefine them.
- [WORKSPACE](WORKSPACE.md) owns internal panes, activation/front behavior, docking, and user layout
  ownership; UX owns general input/feedback principles. Project format owns musical serialization,
  including editable graph data, rather than application/user pane preferences.
- [SETTINGS](SETTINGS.md) owns application/user configuration boundaries, preference-reset limits,
  and quiet bounded production diagnostics. Project format owns sound/meaning-affecting plugin instance
  persistence; workspace owns layout behavior; development owns developer diagnostic activation.
  Architecture owns host localization/service boundaries and UI design owns semantic theme resources;
  settings does not select their APIs, packaging, or exact paths/formats.
- [CODING_GUIDELINES](CODING_GUIDELINES.md) owns implementation conventions/invariants, language,
  lifetime/refactoring rules, and warning policy; subsystem owners retain architecture and behavior.
  [DEVELOPMENT](DEVELOPMENT.md) owns environment, SDK/tool version authority, entry points, and tool
  introduction. Actual package versions remain manifest-owned.
- [TEST_EXECUTION](TEST_EXECUTION.md) owns test topology/quality, verification, future commands, and evidence
  tiers; [PORTABILITY](PORTABILITY.md) owns platform targets, portable boundaries, and claim limits;
  [CI_CD](CI_CD.md) owns hosted automation evolution and trigger policy. None implies installed tools,
  executable commands, workflows, or validated platform parity.
- [PROJECT_STATS](PROJECT_STATS.md) owns the future structural diagnostics contract, including metrics,
  reports, advisory diagnostics, privacy/exclusions, and evolution limits. Development owns actual tool
  introduction/setup; test execution owns suite topology/evidence. This contract does not authorize code.
- Current state is a compact present-tense handoff; work log records completed facts. The planning
  registers below represent distinct states rather than interchangeable task lists.

The decisions log links current contract owners rather than becoming a second specification. An
experiment report owns its observations, not final production contracts. AGENTS owns operational
startup/routing and stays short; it must not accumulate product detail or history.

[THIRD_PARTY](THIRD_PARTY.md) is the canonical provenance ledger and candidate evaluation boundary,
including the Apache-2.0 project licensing boundary, musical content, other assets, repository
services/actions, and historical replacements. Root [LICENSE](../LICENSE) owns authoritative license text;
the ledger never substitutes for legally required bundled notices or relicenses third-party material.
Exact installed versions belong to build/package/native manifests or workflows once present; the
ledger links them instead of maintaining a second lock. Asset-local creation/derivation evidence may
be linked from the ledger. README summarizes the selected license and routes here.

## Compact current-state contract

[PROJECT_STATE](PROJECT_STATE.md) answers: "If a contributor/agent appears with no conversational
memory, where is the project right now?" Keep it a present-tense handoff, not a history log. Its normal
shape is **Current checkpoint**, **Implemented capability**, **Current focus**, **Validation baseline**,
and **Active blockers / evidence gaps**. At the no-code stage, validation may be minimal or omitted
when it would be empty/artificial.

Approximately **50–80 lines** should normally suffice; this is a soft readability signal, not a minimum,
hard limit, or CI gate. A smaller foundation state is preferable to padding. Once implementation exists,
validation summarizes latest meaningful local Release build/test, hosted platform, and bounded runtime/audio
evidence with detail links. Do not accumulate every historical test run or claim evidence absent from reports.
[TEST_EXECUTION](TEST_EXECUTION.md#evidence-tiers) owns optional evidence labels and their limits.

If current state grows substantially, move history to WORK_LOG, accepted choices to DECISIONS_LOG,
detailed validation to an experiment/validation report, plans to ROADMAP, unresolved questions to
KNOWN_PROBLEMS, source topology to future FILE_INDEX, and live Git state to Git. Do not create a line-count
checker or new evidence infrastructure merely to enforce this discipline.

## Planning-state separation

| Register | Meaning / update boundary |
| --- | --- |
| [ROADMAP](ROADMAP.md) | Agreed staged direction: we currently intend to pursue/evaluate an item in that stage |
| [DECISIONS_LOG](DECISIONS_LOG.md) | Accepted durable decisions, rationale, and explicit supersessions; current contracts stay in owners |
| [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) | Concrete unresolved risks/questions requiring design, evidence, experimentation, or a decision before/while related work proceeds |
| [IDEAS](IDEAS.md) | Speculative alternatives/inspirations/features; neither accepted nor scheduled nor required to resolve; may remain indefinitely |
| [TECH_DEBT](TECH_DEBT.md) | Compromises actually present in implementation; design uncertainty is not debt |

Do not mechanically move open owner questions into IDEAS. Promote an idea only through the relevant
owner and decision/roadmap/risk record, leaving a traceable reference. When moving material, preserve
its origin rather than silently deleting history. Avoid duplicate ownership and fake roadmap noise.

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
| Current checkpoint, capability, focus, meaningful validation baseline, blocker/evidence gap | PROJECT_STATE; avoid chronology, live Git status, and transient attempts |
| Product, interaction, visual design, responsibility, audio, graph, workspace, settings/diagnostics, sample, extension, or persistence contract | The corresponding owner in the same change |
| Durable accepted/reversed choice | DECISIONS_LOG plus affected owner; link evidence |
| Future scope or stage order | ROADMAP |
| New/narrowed/resolved concrete uncertainty | KNOWN_PROBLEMS; retain evidence of resolution |
| Speculative idea retained/explored/promoted/rejected | IDEAS; link the resulting owner/decision/plan/risk on promotion |
| Coding, development/tooling, verification, portability, or CI policy | The corresponding engineering owner; passive/build/CI configuration when actually justified |
| ProjectStats metrics/output/privacy/diagnostic/evolution contract | PROJECT_STATS; test execution/development only for their owned boundaries |
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

1. **Stage 0 — established:** AGENTS, compact current state, index/routing, canonical owner docs,
   affected files, and ordinary repository search. No generated context infrastructure.
2. **Stage 1 — policy portion established by SEQ-KB-R3:** coding, development, verification,
   portability, CI, and ideas owners now exist before source, with passive `.editorconfig` and
   `.gitattributes`. This does not start implementation. `docs/FILE_INDEX.md` remains deferred until
   meaningful source topology exists. Add small baseline tooling or one local workflow skill only
   for an observed need; skills are not mandatory infrastructure. SEQ-KB-R4 adds an accepted future
   [ProjectStats contract](PROJECT_STATS.md), independent from retrieval evolution; executable tooling
   still waits for explicit authorization.
3. **Stage 2 — measurable context-selection problems:** consider a generated repository map and
   bounded context planner inspired by MeasPilot. Generated outputs remain disposable retrieval
   artifacts, not canonical truth. Do not create retrieval manifests or budgets now; structural
   ProjectStats introduction follows its separate owner and is not a prerequisite for retrieval tooling.
4. **Stage 3 — exact routing/search demonstrably insufficient:** consider a local semantic index,
   hybrid RAG, and possibly MCP exposure. Retrieval must retain source provenance and never silently
   replace Markdown, source, tests, or Git history as authority.

Promote retrieval infrastructure only in response to an observed problem. Report a broken retrieval
tool and fall back to direct owners/search; a planner or index must not narrow the authorized task or
silently redefine the product. No AgentContext/planner/RAG infrastructure is introduced by SEQ-KB-R3.
Fovium/MeasPilot reference provenance is recorded in [THIRD_PARTY](THIRD_PARTY.md).

Prefer cheap deterministic compiler/editor/build/CI enforcement over agent memory when justified.
[DEVELOPMENT](DEVELOPMENT.md#machine-enforcement-and-text-consistency) maps newline/diagnostic policy
to existing passive configuration and nullable/warnings/SDK/formatting/portability rules to future
enforcement points. Do not introduce tools simply because enforcement is possible.

## Documentation integrity

Before completion, inspect repository-relative Markdown targets and anchors, metadata/ownership,
status terminology, and all new/changed content. Confirm that future work is not advertised as
implemented, proposals are not accepted by implication, current state stays compact, roadmap contains
forward scope, work log contains facts, IDEAS stays speculative, known problems require resolution,
debt contains actual compromises, and AGENTS remains a router.
Inspect final Git diff/status, including untracked additions that ordinary `git diff` does not show.
Documentation-only work requires no build/tests unless executable tooling or configuration changes
actually justify them. No persistent checker is required for this foundation.
