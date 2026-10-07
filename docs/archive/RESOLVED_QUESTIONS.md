# Resolved questions

Role: Historical resolved questions and resolved portions.
Read when: Explicit history, provenance, rationale, supersession, or reconstruction is needed.
Authoritative for: Historical records and traceability only.
Not authoritative for: Current behavior, policy, plans, implementation state, or open questions.

This is a cold, non-canonical archive excluded from normal current context. Active owner documents
win any conflict; historical wording records its stage, not a competing current specification.

## Q-013 — Missing/incompatible required plugin during project open

Status: Resolved by SEQ-KB-R8, 2026-10-07.
Original question: Refuse whole-project opening or open degraded when a required realtime plugin is missing/incompatible?
Resolution: Normally open degraded when an ordinary plugin is missing, disabled, incompatible, or
has a recoverable activation/materialization failure, provided the document remains safely understandable.
Preserve plugin/node identity, state, relationships, musical references, and compatible opaque data.
Editing remains available to the degree of the document model; block affected execution/render/export
by its dependency closure and identify the responsible object. Hard open refusal is reserved for
critical document/schema incompatibility, unsafe migration, severe corruption, or fundamental inability
to understand the document. Missing audio capability alone is not a format failure.
Current owners: [Extensions](../EXTENSIONS.md#degraded-project-opening-and-operation-blockers),
[project format](../PROJECT_FORMAT.md#opening-and-migration), [UX](../UX_CONTRACT.md#project-availability-and-dependency-blockers).
Related open mechanisms: Q-010 package lifecycle/reattachment, Q-024 negotiation, Q-009 format/migration.

## Q-060 — Canonical graph versus last-valid execution in Save/reopen/render

Status: Resolved by SEQ-KB-R8, 2026-10-07.
Original question: Which revision does Save/reopen/export use while editable and playing graphs differ?
Resolution: One canonical editable project graph; prepared execution is a derived revisioned snapshot.
Save preserves canonical user state, including invalid/incomplete work, rather than older playing state.
Last-valid execution is current-session runtime only. Reopen restores canonical saved edits and shows
blockers; affected execution stays unavailable until repaired. No second permanent last-valid graph is
saved merely because the engine used it. Export freezes, validates and prepares canonical state,
blocking required invalid/unavailable dependencies instead of silently rendering stale realtime state.
Current owners: [Node graph](../NODE_GRAPH.md#editable-graph-and-audio-execution),
[project format](../PROJECT_FORMAT.md#save-and-reopen), [audio](../AUDIO_ENGINE.md#offline-rendering-direction),
[UX](../UX_CONTRACT.md#graph-state-and-recoverable-failures).
Related open mechanisms: Q-018 preparation/publication, Q-009 encoding, Q-058 recovery, Q-063 transactions.

## Earlier resolved portions and question-split provenance

The records below retain earlier stage context. Broad questions that remain open are narrowed in the
active register; these historical partial resolutions do not close their remaining mechanisms.

**Q-014 — Project-license selection resolved; third-party scope remains open.** The original question
was "Final FOSS license and dependency/codec/native redistribution obligations". SEQ-KB-R4 selected
Apache License 2.0 for Seqvium-authored work through
[D-029](DECISIONS.md#d-029--seqvium-uses-apache-license-20) and root [LICENSE](../../LICENSE).
The project license is no longer an open choice. The narrowed Q-014 table entry retains only future
dependency/content compatibility and redistribution evaluation under [THIRD_PARTY](../THIRD_PARTY.md).

**SEQ-KB-R5 partial resolutions.** D-033 through D-038 in [DECISIONS_LOG](DECISIONS.md) narrow
Q-009, Q-012, Q-019, Q-025, and Q-028 through Q-030. The media default is accepted, sampling/capture
operations are distinct, graph-per-event defaults are excluded, sharing identities are separated,
processing-context moves have consequences, and hard plugin isolation is optional future evaluation.
The table entries retain their unresolved mechanics; Q-047 through Q-049 capture specific execution
and edit gaps rather than reopening those accepted principles.

**SEQ-KB-R6 partial resolutions.** D-039 through D-044 in [DECISIONS_LOG](DECISIONS.md) narrow
Q-004/Q-005/Q-007/Q-012/Q-018/Q-021/Q-024/Q-028. Platform authority/retention, distinct edits/tails,
last-valid execution with visible editor differences, bounded overload, and future low-latency mode
are accepted direction. Their refined rows and Q-050 through Q-055 retain mechanisms/policies needing
later deliberate design/evidence; no scheduler, ABI, storage layout, algorithm, or packaging was selected.

**SEQ-KB-R7 integration and question refinement.** D-045 through D-049 refine accepted settings,
metadata, compatibility, transport/render, UI and public research policy. Q-009's original broad
format/recovery/media scope is retained as separate Q-009/Q-058/Q-059; Q-012's original render/tail/
transport scope is narrowed to taps/output with Q-056/Q-057 and recording alignment in Q-027.
Q-011's exploration semantics moved to Q-071; Q-016's platform delivery scope moved to Q-068.
Q-013 is clarified as a still-unresolved whole-project opening choice; preservation never selected
one opening model. Q-005/Q-010/Q-018/Q-019/Q-021/Q-047/Q-051 were sharpened with concrete boundary
cases. Q-056 through Q-071 record remaining choices/risks, not accepted answers or new roadmap stages.
No fully unresolved entry was declared resolved merely because a principle was accepted.

When evidence resolves an entry, record the result and link its report/decision; do not erase the
reasoning. A compromise actually introduced into implementation belongs in [TECH_DEBT](../TECH_DEBT.md).

## R8 resolved portions — remaining mechanisms stay open

Status: Partial resolutions accepted by SEQ-KB-R8, 2026-10-07. These are historical portions,
not additional full question definitions; the active register retains each original Q-ID.

| Question | Accepted portion | Current owners | Remaining open scope |
| --- | --- | --- | --- |
| Q-010 | Block/defer physical package uninstall/unload during known active instances/processing/editors/open-project loaded use; instance removal is an ordinary undoable document edit | [Extensions](../EXTENSIONS.md#instance-removal-and-package-uninstall) | Exact discovery/install/update/remove, persistence/location, failed install/update and reattachment lifecycle |
| Q-024 | Plugin age alone is not incompatibility; graded outcomes, required contracts/state/configuration and bounded activation govern compatibility; metadata supports diagnosis | [Extensions](../EXTENSIONS.md#host-context-and-compatibility) | Manifest format, API/ABI, negotiation representation/version ranges and state schema |
| Q-054 | Missing localization prefers usable common fallback; English baseline for Seqvium-authored/native first-party contributions, without changing host language | [Architecture](../ARCHITECTURE.md#host-localization-and-ui-resources), [extensions](../EXTENSIONS.md) | Resource format, contribution mechanism and fallback schema/details |
| Q-062 / Q-069 | Normal UX chooses logical input/output endpoints, separately where supported, not backend libraries; interface-connected analog microphones use interface channels, USB microphone may be a separate endpoint | [Settings](../SETTINGS.md#audio-device-selection), [audio](../AUDIO_ENGINE.md#device-inputoutput-and-recording-direction) | Loss/recovery, clocks, rate/channel/buffer/backend constraints, project intent versus runtime facts |
| Q-063 | Completion alone never authorizes project mutation; revalidate project/target/context, relevance/cancellation and revision/ownership preconditions; deleted-target work cannot attach to new selection | [Architecture](../ARCHITECTURE.md#document-integrity-and-asynchronous-publication), [UX](../UX_CONTRACT.md), [sample workflow](../SAMPLE_WORKFLOW.md) | Undo transactions, async commit grouping, edit history and exact pending-work invalidation |
| Q-018 / Q-011 / Q-012 | Latest valid canonical execution converges automatically; obsolete prep may coalesce/cancel; user-visible preparation fails finitely with cancellation/state rather than waiting forever | [Node graph](../NODE_GRAPH.md#editable-graph-and-audio-execution), [audio](../AUDIO_ENGINE.md#bounded-asynchronous-preparation), [sample workflow](../SAMPLE_WORKFLOW.md) | Generation tokens, queues, publication/retirement, state transfer, watchdogs and scope/transaction mechanics |

No additional question ID is required by this migration: remaining finite-failure and target-publication
mechanisms fit the narrowed Q-018/Q-011/Q-012/Q-063, and migration/removal mechanics fit Q-009/Q-010.

## Q-056 — Manual export range and permitted effect tails

Status: Fully resolved by SEQ-KB-R9, 2026-10-07.
Original question: Does a selected export range default to a hard render boundary, or extend for valid tails?
Resolution: An explicitly selected export range is hard by default. Future UX may offer an explicit
include-tails-like option extending capture for naturally continuing permitted tails. It never overrides
an explicit clip hard boundary, hard-cut processing boundary or other intentional project silence.
Export stays finite; neither the option nor processor feedback implies rendering to mathematical zero.
Current owners: [Audio render](../AUDIO_ENGINE.md#offline-rendering-direction),
[UX](../UX_CONTRACT.md#audio-timeline-editing).
Decision: [D-060](DECISIONS.md#d-060--explicit-clip-boundaries-and-hard-by-default-export-ranges).
Related open mechanisms: Q-057 bounded completion/tail reporting/thresholds/maximum extension and state
transitions; exact export label/UI remains open in the current owners. Q-012 retains resampling/capture
taps and scope. These mechanisms do not keep the manual export product choice unresolved.

## R9 resolved portions — remaining mechanics and roadmap closure stay open

Status: Partial resolutions accepted by SEQ-KB-R9, 2026-10-07. The active register retains the same Q-IDs
only for remaining uncertainty; no new question IDs or complete roadmap redesign were required.

| Question | Accepted portion | Current owners | Remaining open scope |
| --- | --- | --- | --- |
| Q-057 | Natural source end may tail; deliberate item/clip right boundary hard-cuts its own local audible result, including its tail, before shared downstream processing. Tiny de-click may avoid clicks without substantial tail extension. Manual export range is hard by default; explicit inclusion allows only naturally permitted tails, never intentional cuts | [Audio boundaries](../AUDIO_ENGINE.md#source-boundaries-and-effect-tails), [render](../AUDIO_ENGINE.md#offline-rendering-direction), [architecture](../ARCHITECTURE.md#resources-placements-and-two-local-processing-levels), [UX](../UX_CONTRACT.md#audio-timeline-editing); [D-060](DECISIONS.md#d-060--explicit-clip-boundaries-and-hard-by-default-export-ranges) | Seek warm-up/state reconstruction, loop-tail ownership, processor reset, de-click, non-decaying tails, finite completion and realtime/offline parity |
| Q-058 | Recovery is rolling current snapshot/state, separate from explicit Save, supports unnamed/unsaved work without a final path and is bounded without casually deleting sole recent unsaved recovery for arbitrary thresholds. Exact cadence is not selected; rapid interaction must not cause pathological writes. Document recovery does not reconstruct media | [Project format](../PROJECT_FORMAT.md#recovery-state), [UX](../UX_CONTRACT.md#project-lifecycle-and-durable-work), [settings](../SETTINGS.md#reset-boundary); [D-057](DECISIONS.md#d-057--rolling-recovery-snapshot-separate-from-explicit-save) | Crash-safe replacement, corruption detection, cadence/debounce, retention/cleanup/layout/location, recovery choice, explicit-Save interaction and recorded-media reconciliation |
| Q-059 | Ordinary import is project-managed durable media independent of original external paths; storage succeeds before durable availability is claimed. Acceptance and Save/Save As/collect/relocate fail safely. Individual missing/corrupt media normally degrades safely understandable projects with persistent visible state and repair/relink/replace/remove where meaningful. No eager deletion while Undo/recovery/pending/uncommitted work may need resources | [Project format media](../PROJECT_FORMAT.md#media-policy-boundary), [integrity](../PROJECT_FORMAT.md#media-and-persistence-integrity), [opening](../PROJECT_FORMAT.md#opening-and-migration), [sample workflow](../SAMPLE_WORKFLOW.md#acceptance-and-provenance), [UX](../UX_CONTRACT.md#project-availability-and-dependency-blockers); [D-058](DECISIONS.md#d-058--project-managed-ordinary-imported-media-and-degraded-resource-access) | Container/storage, staging/atomic commit, Save As/collect/relocate, checksums/integrity, repair workflows, cleanup/GC, disk-full/interrupted operations and separately justified advanced external references |
| Q-061 | Foundation-first strategy rejects MVP-at-any-cost and fastest-demo ordering; durable architecture rails and new-project/document lifecycle are foundational. Logical domain/application/infrastructure/presentation separation does not mandate layer projects. Meaningful end-to-end track is a later acceptance milestone requiring dedicated research/audit, not a finalized R9 scenario | [Roadmap](../ROADMAP.md#sequencing-philosophy), [architecture](../ARCHITECTURE.md#domain-application-infrastructure-and-presentation); [D-059](DECISIONS.md#d-059--foundation-first-architecture-and-logical-responsibility-separation) | After sufficient foundation decisions/evidence, concrete fuller implementation roadmap, safe stage dependencies, complete-project/MVP acceptance scenario, capability-to-stage mapping and Codex/architecture gap/cycle audit |

Q-012 wording was aligned to retain capture-specific taps/output/range scope and Q-057 mechanisms,
without reopening the resolved manual-export default. Q-065 Browser/personal-library scope was untouched.

## R10 resolved portions — signal semantics accepted, concrete bindings remain open

Status: Partial resolutions accepted by SEQ-KB-R10, 2026-10-07. Q-019 and Q-030 retain their canonical
IDs in the active register solely for concrete mechanisms; neither is reported as fully closed.

| Question / original scope | Accepted portion and rationale reference | Current owners | Remaining open scope |
| --- | --- | --- | --- |
| Q-019 — Exact identity/ownership of item/container graphs versus instrument/channel/bus/Master scopes | Musical ownership does not imply a bus; contributions preserve required independence; whole-placement/container processing is explicit convergence within two local levels; Pattern object rendering does not alter playback routing. [D-061](DECISIONS.md#d-061--musical-ownership-and-explicit-signal-convergence), [D-063](DECISIONS.md#d-063--object-render-convergence-follows-semantic-ownership) | [Architecture](../ARCHITECTURE.md#signal-ownership-and-processing-contexts), [node graph](../NODE_GRAPH.md#contributions-and-irreversible-mixing), [sample workflow](../SAMPLE_WORKFLOW.md#create-sample-from-object) | Concrete scope representation, graph attachment/reference sharing and edit/lifetime ownership; related Q-029/Q-018, execution Q-047, conversion Q-048 and render taps Q-012 |
| Q-030 — Arrangement-container processing relationship to mixer channels/buses and concrete signal consequences of moves | Separate timeline and Mixer identities can expose one context without duplicate DSP; context ownership determines local/global boundary; processing moves change path/aggregate membership, organization alone does not. [D-062](DECISIONS.md#d-062--separate-arrangement-identity-and-shared-mixer-context-presentation) | [Architecture](../ARCHITECTURE.md#arrangement-context-and-mixer-presentation), [moves](../ARCHITECTURE.md#moving-material-between-contexts), [UX](../UX_CONTRACT.md#processing-context-and-mix-feedback) | Concrete route/context assignment, defaults and shared control bindings; related compatibility Q-028, graph references Q-019, undo/commit Q-063 and state transitions Q-057 |

Q-047 remains open. R10 clarifies its required separable signals through the cases; it supplies no
CPU/memory, voice, instance, external-host or state-lifetime evidence and closes no execution mechanism.

## R11 resolved portion — Q-047 semantic contract, mechanisms remain open

Status: Partial resolution accepted by SEQ-KB-R11, 2026-10-07. Q-047 retains its canonical definition
in the active register; no full runtime/performance resolution is claimed.
Original question: How can shared instrument definitions preserve independent overlapping execution/
local processing at bounded CPU/memory cost?
Accepted portion: Durable shared sound intent and runtime performance state are distinct. Semantic
execution domains bound intended voice/state interaction, may serve multiple compatible occurrences
and may expose multiple contributions where supported. Independent performance and divergent required
paths must be preserved from sound production; mono/legato/voice-stealing consequences are explicit.
Separable sources may realize this without per-placement instances; opaque aggregate-only sources may
need multiple instances or another real separation capability. Shared-definition edits do not merge live
state; occurrence retirement cannot reset another use through definition identity. Impossible capability/
resource configurations fail/degrade explicitly, preserving canonical intent rather than silently mixing,
substituting stale/wrong audio or omitting required processing.
Rationale: [D-064](DECISIONS.md#d-064--shared-sound-definitions-and-semantic-execution-domains).
Current owners: [Architecture](../ARCHITECTURE.md#shared-sound-definitions-and-execution-domains),
[audio](../AUDIO_ENGINE.md#execution-state-lifetime-and-resource-integrity),
[extensions](../EXTENSIONS.md#independent-execution-capability),
[format](../PROJECT_FORMAT.md#musical-content-and-workspace-state),
[UX](../UX_CONTRACT.md#project-availability-and-dependency-blockers).
Remaining Q-047: Concrete grouping, voice allocation, source separation/opaque-host instancing,
definition synchronization/realtime publication, state/lifetime mechanisms, pooling and resource limits,
measured CPU/RAM scaling and pressure/failure evidence. Exact graph publication/references Q-018/Q-019,
capability negotiation Q-024, reference/edit Q-029, DSP tails/transitions Q-057, async/undo Q-063 and
cross-context routing Q-066 remain coordinated open questions. R0 scope/order is unchanged and cannot
establish arbitrary plugin behavior or resource bounds; later bounded evidence is separately authorized.
