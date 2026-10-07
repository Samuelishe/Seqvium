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
- [WORKSPACE](WORKSPACE.md) owns main-window chrome/foreground behavior, internal panes, docking, and user layout
  ownership; UX owns general input/feedback principles. Project format owns musical serialization,
  including editable graph data, rather than application/user pane preferences.
- [SETTINGS](SETTINGS.md) owns application/user configuration boundaries, preference-reset limits,
  and quiet bounded production diagnostics. Project format owns project-affecting settings and plugin instance
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

The cold decision archive retains acceptance/rationale/supersession history; current owners state
accepted behavior directly without requiring historical D-records. An experiment report owns its observations, not final production contracts. AGENTS owns operational
startup/routing and stays short; it must not accumulate product detail or history.

[THIRD_PARTY](THIRD_PARTY.md) is the canonical provenance ledger and candidate evaluation boundary,
including the Apache-2.0 project licensing boundary, musical content, other assets, repository
services/actions, current candidates and current obligations. Removed/replaced history may be archived
only when current license/provenance obligations remain fully preserved. Root [LICENSE](../LICENSE) owns authoritative license text;
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

If current state grows substantially, move history to the cold archive, accepted current rules to
canonical owners, current evidence details to their reports, plans to ROADMAP, open questions to
KNOWN_PROBLEMS, source topology to future FILE_INDEX and live Git state to Git. Preserve past
validation narratives in archive when they no longer describe current evidence. Normal startup must
not link to completed work chronology. Do not create a line-count checker or evidence infrastructure
merely to enforce this discipline.

## Planning-state separation

| Register | Meaning / update boundary |
| --- | --- |
| [ROADMAP](ROADMAP.md) | Current stage if any, pending/future stages, and current sequence/scope constraints only |
| [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) | Concrete unresolved risks/questions requiring design, evidence, experimentation, or a decision before/while related work proceeds |
| [IDEAS](IDEAS.md) | Still-active speculative/exploring alternatives; neither accepted nor rejected/completed nor required to resolve |
| [TECH_DEBT](TECH_DEBT.md) | Compromises actually present in implementation; design uncertainty is not debt |

Do not mechanically move open owner questions into IDEAS. Promote an idea only through the relevant
owner and decision/roadmap/risk record, leaving a traceable reference. When moving material, preserve
its origin in archive rather than silently deleting history. Avoid duplicate ownership and fake roadmap noise.

## Rolling current knowledge and cold history

**Active `docs/` contains current truth, current policy, current plans, current unresolved questions,
and current implementation state. Historical material belongs in `docs/archive/`.** Maintain documents
by state rather than accumulate chronology indefinitely. A current rule stays active even if accepted
long ago; when/why it was chosen belongs in history. Do not over-archive current contracts.

History includes completed stages/work, resolved questions, superseded decisions and obsolete wording,
rejected alternatives, previous checkpoints, old audits/validation narratives, resolved debt and
removed/replaced components no longer relevant to current obligations. Rationale needed to understand
current constraints may stay concise in its owner; obsolete debate does not stay as a second contract.

The cold archive is non-canonical for current product behavior and loses any conflict with active owners.
It serves reconstruction/traceability only. Its initial flat files are INDEX, DECISIONS,
RESOLVED_QUESTIONS, ROADMAP, WORK_LOG and AUDITS; split by year/stage only when actual size/use justifies it.
There is no active work-log or decision-ledger copy. Future completed work goes directly to archive
WORK_LOG; current work/handoff belongs in current state or the appropriate current owner.

Archive retrieval is allowed only for explicit historical change, superseded-decision rationale,
resolved-question reconstruction, provenance, or previous implementation/validation evidence tasks.
Exclude `docs/archive/**` from normal startup, selective owner reading, ordinary task context,
generated current-context maps and future default RAG/index corpora. A future planner/index must treat
`docs/**` excluding `docs/archive/**` and `docs/archive/**` as **separate retrieval classes**. Default
current-knowledge retrieval excludes history; historical retrieval explicitly opts in. Do not duplicate
archive contents into active docs to force normal context visibility. No executable RAG infrastructure
is selected or introduced. [Archive index](archive/INDEX.md) is the single historical lookup route.

Before removing an active historical document, move any unique still-current information to its
canonical owner first. Preserve meaningful IDs, dates/stages, rationale/supersessions and reconstruction
context, repair relative links and remove duplicate authoritative copies. Active owners must state/link
the actual current rule, without forcing readers to load archived D-records. Stable archival D-IDs stay
useful; resolved questions retain Q-ID, question, status, accepted resolution, owner and related open
mechanisms. Partial answers move to archive as useful history while the active question narrows to the
unresolved mechanism; never falsely close it.

IDEAS retains only still-speculative/exploring records; promoted/rejected/completed outcomes leave the
active incubator and may be retained in AUDITS until a dedicated split is justified. TECH_DEBT contains
only existing unresolved implementation compromises; resolved history moves to archive. It is empty
while implementation is absent. THIRD_PARTY retains current introduced material, materially evaluated
candidates and current provenance/license obligations. Archiving old removals/replacements never
removes legally required current provenance/notices; licensing correctness takes priority over age.

## Selective reading

Follow `AGENTS -> PROJECT_STATE -> INDEX/routing -> selected owners -> affected files`.
Use ordinary `rg` search to find paths, symbols, and relevant sections. Expand context only for a
concrete unresolved boundary or evidence need. Roadmap is selective planning context, not routine
startup; decision/work history and the cold archive are never default current context.

Canonical documentation is English, with simple precise terminology. Durable knowledge must remain
usable by a fresh agent or contributor without the bootstrap prompt or conversation history.

## External repository research and public documentation

Information learned while inspecting other repositories is working/research context and must not
automatically be copied into Seqvium's public canonical documentation. Unless explicitly requested
or genuinely required as public third-party provenance/evidence, do not record private repository
names or URLs, commit SHAs, internal paths, workflow names, repository topology, confidential/private
implementation details, or research provenance irrelevant to Seqvium's public contract. Keep useful
Seqvium engineering conclusions without identifying their unrelated research sources.

Official public documentation for a dependency or standard may be cited when materially useful.
Actual third-party code, assets, and services introduced into Seqvium still require normal provenance
in [THIRD_PARTY](THIRD_PARTY.md). Research context is distinct from distributed-material provenance;
this rule does not remove license/notice obligations or rewrite Git history.

## Status and conflict resolution

Distinguish **accepted constraint/direction**, **proposal**, **open question**, and **implemented fact**.
Acceptance does not imply implementation. Candidate technologies are not actual dependencies.

Explicit task instructions determine the authorized change. Identify the subject's canonical owner;
a non-owner summary does not override it. Inspect source/tests and evidence when implementation
claims conflict. Repair stale summaries or the evidenced stale owner without silently relaxing an
accepted requirement. Runtime behavior that violates an accepted contract is a discrepancy, not
automatic supersession. Record uncertainty if evidence is incomplete.

When a durable decision changes, update its current owner in the same change and preserve the
previous/new decision relationship and rationale in the archive. If a material choice remains
unresolved, preserve the distinction and ask for needed input rather than invent acceptance. External references never override Seqvium owners.

## Update triggers

| Changed truth/state | Update in the same change |
| --- | --- |
| Current checkpoint, capability, focus, validation baseline or active gap | PROJECT_STATE; no chronology, live Git status or transient attempt log |
| Product/architecture/subsystem or engineering contract | Corresponding current owner; associated configuration only when actually justified |
| New accepted product/architecture decision | Current owner, remove/narrow affected open question, append rationale/decision record to archive DECISIONS |
| Decision superseded | Current owner gets new truth; archive retains old/new relationship, rationale and IDs; no stale competing active contract |
| Concrete uncertainty new/narrowed | KNOWN_PROBLEMS contains only the remaining open mechanism |
| Question resolved | Remove from KNOWN_PROBLEMS, append ID/question/resolution/current owners and related open mechanisms to archive RESOLVED_QUESTIONS |
| Current/future scope or stage order | Active ROADMAP; no accidental authorization or reordering |
| Roadmap stage completed | Remove when no longer useful to current planning; append/preserve stage in archive ROADMAP |
| Meaningful work completed | Update current state if its truth changed; one bounded factual record directly in archive WORK_LOG |
| Idea explored | Active IDEAS while still speculative/exploring |
| Idea promoted/rejected/completed | Update resulting current owner/plan/question if applicable, remove inactive idea, preserve useful outcome in archive |
| Existing implementation compromise introduced | TECH_DEBT with actual evidence, impact and exit condition |
| Debt resolved | Remove from TECH_DEBT; preserve useful evidence/history in archive |
| Component/service/asset evaluated/introduced/upgraded or current obligation changed | THIRD_PARTY retains current obligations and manifest version authority |
| Component removed/replaced | Current THIRD_PARTY reflects remaining obligations; archive history when legally safe |
| Owner added/moved or route changed | INDEX; AGENTS if operational routing changes |
| Experiment completed | Bounded report; update current owners for supported findings, archive decision/work/resolved-question history as appropriate |
| Audit/checkpoint becomes historical | Preserve ranking/narrative/checkpoint in archive AUDITS; retain still-open findings as ordinary current questions |

Do not update every file after every task or add duplicate handoff documents. History preservation is
permanent rolling policy, not a reason to keep chronology in active owners. Archive does not become a
second canonical specification.

## Knowledge evolution

The ownership model stays stable while retrieval mechanisms evolve:

1. **Established current baseline:** AGENTS, compact current state, INDEX, selected canonical owners,
   affected files and ordinary search. Engineering owners and passive text policies exist before source;
   no generated context infrastructure. FILE_INDEX remains deferred until meaningful topology exists.
   ProjectStats has a separate future diagnostics contract; executable tooling requires authorization.
2. **Observed context-selection cost:** consider a disposable generated repository map/bounded planner,
   with archive excluded from generated current-context maps. Do not introduce manifests/budgets now.
3. **Exact routing/search demonstrably insufficient:** consider local semantic index, hybrid RAG and
   possibly MCP, retaining provenance. Current corpus excludes archive; historical corpus is explicit opt-in.
   Retrieval never silently replaces Markdown, source, tests or Git as authority.

Promote infrastructure only for an observed problem. A broken retrieval tool falls back to direct
owners/search and cannot narrow authorized scope or redefine the product. No planner/RAG/tooling is
introduced by this policy. Prefer cheap deterministic compiler/editor/build/CI enforcement when
justified; [DEVELOPMENT](DEVELOPMENT.md#machine-enforcement-and-text-consistency) owns concrete
passive and future enforcement points.

## Documentation integrity

Before completion, inspect repository-relative Markdown targets and anchors, metadata/ownership,
status terminology, and all new/changed content. Confirm that future work is not advertised as
implemented, proposals are not accepted by implication, current state stays compact, roadmap contains
only current/forward scope, archive work log contains completed facts, IDEAS stays speculative-active,
known problems contain only unresolved issues, debt contains only existing unresolved compromises,
and AGENTS remains a router. Check archive non-authority/exclusion, ID traceability, migration links,
and that no required current truth exists only in archive.
Inspect final Git diff/status, including untracked additions that ordinary `git diff` does not show.
Documentation-only work requires no build/tests unless executable tooling or configuration changes
actually justify them. No persistent checker is required for this foundation.
