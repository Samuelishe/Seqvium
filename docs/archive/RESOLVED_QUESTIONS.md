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

## R12 resolved portions — Q-063 semantics accepted, concrete integrity mechanisms remain open

Status: Partial resolution accepted by SEQ-KB-R12, 2026-10-07. Q-063 keeps its canonical active
definition; no full implementation/evidence resolution is claimed.
Original question: Precise undo transactions, async commit grouping, document history and pending-work
invalidation.
Accepted portion: Coherent canonical user intentions define Undo transactions; explicit preview and
async preparation/resource creation do not enter history by completion alone. Sufficient commit gates
validate original document/lifecycle, target/ownership, context/dependencies and operation relevance/
authorization. History, deletion/identity restoration and close never implicitly resurrect invalidated
work. Save/recovery and resource retention keep their distinct boundaries; derived graph publication
has no second Undo stack. Cases A–L were covered through current owners, including one-transaction
rendered replacement and failure before versus after an intentional canonical edit.
Rationale: [D-065](DECISIONS.md#d-065--logical-undo-transactions-and-async-commit-integrity).
Current owners: [Architecture](../ARCHITECTURE.md#document-integrity-and-asynchronous-publication),
[UX](../UX_CONTRACT.md#undo-grouping-and-interaction-preview),
[sample workflow](../SAMPLE_WORKFLOW.md#contextual-generation-result-validity),
[format](../PROJECT_FORMAT.md#save-and-reopen), [graph](../NODE_GRAPH.md#editable-graph-and-audio-execution).
Remaining Q-063: Transaction/command-stack shape and commit coordination; identity/lifecycle and
revision/dependency representation, cancellation/invalidation; concrete gesture/shared/plugin adapters,
operation-specific suspension/rebase/retention/reuse; Undo/Redo/delete/close races and preparation/storage/
commit failure evidence; history persistence across Save/reopen/restart, limits and storage.
Related narrowed portions: Q-011 contextual generation validity and no automatic acceptance are fixed,
but substitution/audition restoration/publication mechanisms remain open. Q-029 shared-content Undo is
fixed, but reference/detachment, acceptance scope and sharing UI remain open. Q-049 coherent replacement/
restoration grouping is fixed, but exact baked-chain transformation remains open. Q-018 canonical Undo
versus execution is coordinated without closing publication/state-transfer/retirement mechanisms.
Q-019/Q-030/Q-047/Q-057/Q-058/Q-059/Q-066 retain their concrete scopes. No question was fully closed.

## R13 resolved portion — Q-064 input semantics accepted, accessibility evidence remains open

Status: Partial resolution accepted by SEQ-KB-R13, 2026-10-08. Q-064 retains its canonical active
ID for concrete implementation and evidence; no complete accessibility resolution is claimed.
Original question: Required keyboard/focus paths, accessible custom-window actions, non-color status
cues and discoverable actions for mouse-first custom chrome, overlapping panes and graph feedback.
Accepted portion: Mouse-first is compatible with first-class keyboard-efficient editing without
memorized shortcuts for basic workflows. Semantic actions are independent of gestures/physical bindings
and converge on canonical operations/Undo. Precise/repeated/modifier input is legitimate; R12 discrete
versus bounded held-session grouping remains authoritative. Focus, selection, active pane and command
target differ; text/value/search/native-editor input is protected from unrelated host editing commands.
Important actions need discoverable paths, essential feedback cannot rely only on color/transient
animation, and custom chrome/workspace must preserve baseline keyboard reachability, escape and useful
focus return. QWERTY musical input stays optional/explicit; rebinding and platform-appropriate defaults
remain architecturally possible. Cases A–L were covered without choosing final bindings or UI mechanics.
Rationale: [D-066](DECISIONS.md#d-066--mouse-first-keyboard-efficient-semantic-actions-and-accessibility-baseline).
Current owners: [UX](../UX_CONTRACT.md#mouse-first-creation-and-complementary-input),
[architecture](../ARCHITECTURE.md#semantic-actions-and-input-boundary),
[workspace](../WORKSPACE.md#keyboard-access-and-focus-return), [UI feedback](../UI_DESIGN.md#feedback-and-motion);
[vision](../PROJECT_VISION.md#creation-musical-structures-and-organization) owns product fit.
Remaining Q-064: Exact bindings/defaults, modifier conflicts, steps/repeat; concrete focus/command
routing, detailed node/timeline navigation, pane/overlay escape and external-editor return; platform
accessibility APIs, screen-reader/accessibility-tree implementation; keybinding editor/storage/conflict
mechanics, import/export, profiles, chords/sequences; DPI/minimum-size and actual window/workspace/input
cross-platform validation. No certification, implementation or release parity is implied.
Related questions: Q-015 visual/detail, Q-022 docking/storage, Q-028 terminology/container identity,
Q-032 settings placement, Q-035 pane targets/following, Q-051 timeline mechanics, Q-063 history/grouping
mechanisms and Q-067 localization/theme services remain open and are not separately narrowed here.

## R14 resolved portion — Q-065 discovery and reusable ownership semantics

Status: Partial resolution accepted by SEQ-KB-R14, 2026-10-08. Q-065 retains its canonical active ID
for concrete workflow/mechanism design and evidence; no library/catalog implementation is claimed.
Original question: Discovery/reuse is central but Browser and personal sample/preset library have no
bounded workflow or stage owner commitment.
Accepted portion: Browser is a discovery/access surface across distinct source/ownership classes.
Preview/contextual temporary audition is transient and outside project Undo; explicit use accepts durable
project audio or configuration. Project resources support discoverable audition/reuse. Generation/render
acceptance creates project material only; intentional Personal Library publication is independent user
content protected from preference reset. Source/pack/library removal cannot break accepted project audio;
project cleanup cannot delete library originals. Applied preset/template state is project-owned, including
compatible opaque state where applicable; mutable source changes/removal cannot rewrite it. Provenance
does not become a live dependency, and equal bytes/name/path do not collapse ownership meanings.
R2 owns minimum external WAV/project-resource access, R7 accepted-generation integration, R8 project
render reuse and R12 small-track discovery usability. Explicit cross-project sample/preset publication
belongs to R14+ after R12. No implementation stage renumbered/reordered/started.
Rationale: [D-067](DECISIONS.md#d-067--browser-discovery-and-explicit-reusable-content-ownership).
Current owners: [Sample workflow](../SAMPLE_WORKFLOW.md#browser-discovery-and-ownership),
[UX](../UX_CONTRACT.md#discovery-audition-and-reusable-content),
[format](../PROJECT_FORMAT.md#source-provenance-and-reusable-content),
[settings](../SETTINGS.md#reset-boundary), [presets](../EXTENSIONS.md#preset-sources-and-project-state),
[roadmap](../ROADMAP.md#discovery-and-reuse-ownership).
Remaining Q-065: Exact Browser UI/layout, library filesystem/database format, scanning/indexing/watching,
metadata, search/tags/favorites, dedup/content addressing, waveform caching, preset format/versioning/
overwrite and plugin-defined formats, bindings, scaling/performance, cross-platform paths, backup/sync/
import/export. Linked/live presets and portable packages require separate justification.
Related questions: Q-009 schema/container, Q-010 package lifecycle, Q-011 contextual audition mechanics,
Q-024 preset/capability/state compatibility, Q-029 references/sharing/acceptance scope, Q-058/Q-059
recovery/storage, Q-063 history/commit and Q-071 candidate similarity/history remain open without separate
narrowing. Their accepted contracts are coordinated, not fully solved by this semantic boundary.

## R15 resolved portions — Q-054/Q-055/Q-067 host resource semantics and staging

Status: Partial resolution accepted by SEQ-KB-R15, 2026-10-08. All three Q IDs retain canonical active
definitions for concrete mechanisms/evidence; no question is fully closed.
Original questions: Q-054 localization format/contributions/fallback; Q-055 semantic themes/API/packaging;
Q-067 minimum host service staging and contribution lifetime/versioning.
Accepted portions: Stable resource and contributor scope, content, host language, contributor languages/
fallback, style roles, theme selection and lifetime are separated from project identity. Initial first-party
RU/EN uses English baseline; per-resource missing/invalid entries fall back locally, then to host-owned
generic explanation when necessary, without changing host language or executable compatibility. Shared
Dark/Light role categories and safe host baseline cover first-party/native surfaces with non-color meaning;
independent external editors are exempt. Contributor retirement invalidates active UI dependencies safely,
retains project identities/opaque state and does not keep executable code just to display missing labels.
Language/theme changes preserve musical state and ongoing editing intent; canonical values/serialization
are independent of translated presentation and OS culture. R3 owns host-only rails, R6 bounded real
contributions, R7 shared-resource generator UI and R12 coherent shipped-surface fallback/integrity within
the small-track scenario. Stage IDs/order preserved; SEQ-R0 remains pending / not started.
Rationale: [D-068](DECISIONS.md#d-068--minimum-host-localization-and-semantic-theme-foundation).
Current owners: [Architecture](../ARCHITECTURE.md#host-localization-and-ui-resources),
[UI design](../UI_DESIGN.md#themes-and-semantic-resources),
[Extensions](../EXTENSIONS.md#ui-resource-contribution-lifecycle),
[UX](../UX_CONTRACT.md#language-and-theme-preference-changes),
[Roadmap](../ROADMAP.md#localization-and-theme-foundation-ownership).
Remaining Q-054: Resource format/identifier syntax, supported-language/fallback schema, registration APIs,
translation-value validation/diagnostics, numeric/musical/unit/diagnostic formatting and text parsing,
locale selection and safe preference application with actual UI evidence.
Remaining Q-055: Resource API/token schema, colors/fonts/dimensions/icons, validation/fallback details,
theme preference retention/application, packaging and visual/platform evidence; no palette or package chosen.
Remaining Q-067: Registration/duplicate handling, availability/caching/invalidation, late contribution,
safe dependent-UI retirement, load/unload sequencing/reference ownership, resource contract evolution/
versioning representation and compatibility rules with bounded lifecycle evidence.
Related questions: Q-010 package removal/disable mechanisms, Q-024 executable negotiation, Q-015 final
visuals, Q-053 preference storage/reset and Q-064 accessibility/platform evidence remain open without
separate narrowing. Q-061 keeps the full later roadmap dependency audit; this stage resolves only the
minimum host-resource ownership slice. Missing localization is not a processing compatibility failure.

## R16 resolved portion — Q-066 bounded cross-context semantics

Status: Partial resolution accepted by SEQ-KB-R16, 2026-10-08. Q-066 retains its canonical active ID
for mechanisms/evidence; no question is fully closed.
Original question: Cross-context sidechain/control routes may cross two local processing scopes;
exposure alone does not settle lifetime or scheduling.
Accepted portion: Explicit audible routes/sends, detector sidechains and parameter controls have
distinct roles. Control-only influence does not mix source audio into target audio or change local
processing ownership. Fan-out of a compatible signal need not invent several performances; shared
placements retain contribution/domain independence. Specified source/tap/target/context intent is
canonical, with meaningful identity across moves, no name/position retargeting, safely retained unresolved
connections, scoped blockers and coherent endpoint/edit/Undo behavior. Preparation validates combined
dependencies, causality, capabilities and timing; arbitrary zero-delay cycles remain disallowed.
Tap-aware natural ends/hard boundaries do not reset unrelated target DSP. Object rendering freezes
required external influence without expanding its audible boundary or omitting unsupported dependencies.
Rationale: [D-069](DECISIONS.md#d-069--bounded-cross-context-routing-and-sidechain-semantics).
Current owners: [Node graph](../NODE_GRAPH.md#cross-context-signal-and-control-relationships),
[Architecture](../ARCHITECTURE.md#cross-context-ownership-and-identity),
[Audio](../AUDIO_ENGINE.md#cross-context-boundaries-and-timing),
[Sample workflow](../SAMPLE_WORKFLOW.md#external-dependencies-in-object-rendering),
[UX](../UX_CONTRACT.md#external-dependency-feedback),
[Roadmap](../ROADMAP.md#cross-context-routing-ownership).
Remaining Q-066: Concrete source/tap/target representation, port compatibility/rates, graph validation/
scheduling, feedback, latency/timing, state/lifetime and move/delete/replace/reattachment mechanisms,
offline dependency capture, host/plugin capability support, detailed UI and platform evidence.
Related Q-005/Q-012/Q-017/Q-018/Q-019/Q-020/Q-021/Q-028/Q-030/Q-033/Q-034/Q-047/Q-057/Q-063 remain
open without separate closure. R4 foundations, R8 supported render handling, bounded R10/R11 workflow
features and R14+ richer expansion preserve implementation order and avoid mandatory mature routing
in R0/R12. No engine mechanisms or final UI selected; SEQ-R0 remains pending / not started.

## R17 resolved portion — Q-071 bounded exploration semantics

Status: Partial resolution accepted by SEQ-KB-R17, 2026-10-08. Q-071 retains its canonical active ID
for material implementation mechanisms and evidence; it is not fully resolved.
Original question: Candidate similarity, parameter locks and exploration history have distinct limits
and user choices beyond contextual substitution.
Accepted portion: Random exploration and reference-based nearby variation are distinct intentions
within a supported family. Declared supported locks apply to future generation, preserve held constraints
across compatible reference changes and cannot silently transfer between unrelated families. Request
reference/constraints, audible comparison choice and acceptance destination remain distinct. Bounded
temporary candidate history supports revisiting/comparing available alternatives without canonical
edits, document Undo or implicit Save/recovery/library publication. Async operation identity, relevance
and current user choice survive out-of-order completion and source/target/project changes; no stale
overwrite, redirection or resurrection. Ending audition restores current canonical sound. Explicit
scope-validated acceptance creates durable project resources through normal Undo; multiple candidates
can be independently kept without twice replacing a target. Accepted audio survives generator removal;
temporary audio, metadata and recipe availability differ, with safe bounded cleanup and no arbitrary
cross-version bit-identical regeneration promise.
Rationale: [D-070](DECISIONS.md#d-070--bounded-sample-lab-exploration-and-candidate-semantics).
Current owners: [Sample workflow](../SAMPLE_WORKFLOW.md#intentional-and-lazy-exploration),
[UX](../UX_CONTRACT.md#sample-lab-exploration-feedback),
[UI design](../UI_DESIGN.md#sample-lab-interaction-hierarchy),
[Extensions](../EXTENSIONS.md#generator-exploration-capabilities).
Remaining Q-071: Concrete family algorithms/similarity calculations and reference inputs, parameter
schema/ranges and lock representation/coupling/compatibility, candidate history limits/storage/retention,
comparison/request-selection UI, ordering/cancellation, temporary ownership/removal lifecycle and
performance/usability evidence. No algorithm, schema, widgets, numeric limits or storage selected.
Related Q-011 audition/substitution/restoration, Q-029 sharing/acceptance scope, Q-063 async/Undo,
Q-065 discovery/publication, Q-010/Q-024 extension availability/compatibility, Q-058/Q-059 recovery/media
and Q-047/Q-057 execution/tails remain open without separate narrowing. R7 scope already suffices;
R14+ expansion needs demonstrated need. SEQ-R0 remains pending / not started.

## R18 resolved portions — Q-028/Q-029 container and shared-edit semantics

Status: Partial resolution accepted by SEQ-KB-R18, 2026-10-08. Q-028 and Q-029 each retain their
canonical active ID for concrete mechanisms/evidence; neither is fully resolved.
Original Q-028: Arrangement container term/domain identity, compatibility, preferred target, ownership
and nesting. Accepted portion: User-named containers organize compatible timed occurrences and may
expose explicit containing processing. Purpose is not exclusive sound/instrument ownership; supported
content/time/context relationships are constraints, with no identical-shape or live-availability rule.
Organization is distinct from processing membership and can be sound-neutral. Musical containment
creates no bus or one-to-one Pattern/Instrument Group/Mixer mapping. Organizational nesting cannot add
a third ordinary local processing level; at most one containing local context follows item-local
processing. Flat-only versus bounded organizational hierarchy remains undecided.
Remaining Q-028: Final public term/domain structure, compatibility/capability tests, default-target and
feedback algorithms, hierarchy/depth/assignment mechanics and evidence, coordinated with Q-023/Q-030/
Q-008/Q-051. No classes, exact nested tree or flat-only restriction selected.
Original Q-029: Reference/edit ownership, sharing and contextual acceptance across music, sound,
placements, processing and media. Accepted portion: Shared notes edit their one Pattern; local changes
and moves edit the occurrence. Musical variation detaches content for intended references, independently
of sound/resource/processing sharing. Independent sound establishes independent settings for intended
uses without mandatory note/media copies or instance counts. Local effects do not require detachment.
Resource reuse remains non-destructive. Delete occurrence, definition, container and used sound have
different dependency-aware scopes; coherent Undo restores canonical relationships without hidden cascades
or eager media deletion. Contextual acceptance distinguishes reusable resource, one use and explicitly
shared sound; target/dependency validation and durable storage precede coherent commit. Save/reopen
preserves actual relationships, never reconstructs them by labels/data/layout. Scope/sharing feedback
must be understandable without constant modal questionnaires or a universal unlink/override model.
Remaining Q-029: Concrete identity/reference storage and transitions, one-use detachment within shared
content, edit/copy and opaque-state capability handling, deletion/cascade/orphan/lifetime mechanisms,
sharing/target and acceptance UI, serialization/migration/graph coordination, concurrency/performance
evidence. Event conversion/source linkage stays Q-048; Q-008/Q-009/Q-019/Q-047/Q-049/Q-051/Q-059/
Q-063/Q-065/Q-066 retain specialized mechanisms. No schema, clone or cleanup algorithm selected.
Rationale: [D-071](DECISIONS.md#d-071--arrangement-occurrences-and-separate-shared-edit-ownership).
Current owners: [Architecture](../ARCHITECTURE.md#separate-sharing-identities),
[Arrangement](../ARCHITECTURE.md#semi-free-arrangement),
[UX](../UX_CONTRACT.md#edit-target-and-sharing-feedback),
[Project format](../PROJECT_FORMAT.md#musical-content-and-workspace-state),
[Sample workflow](../SAMPLE_WORKFLOW.md#acceptance-scope-and-shared-uses).
Bounded roadmap ownership preserves stage IDs/order and R0 scope; no full R12 scenario/dependency audit
is claimed, and Q-061 stays open. SEQ-R0 remains pending / not started.

## R19 resolved portions — Q-058/Q-059 recovery and media failure semantics

Status: Partial resolution accepted by SEQ-KB-R19, 2026-10-08. Each question retains its canonical
active ID for concrete mechanisms/evidence; neither is fully resolved.
Original Q-058: Rolling recovery implementation and recovery-choice workflow.
Accepted portion: Recovery belongs to an identified working document/lifecycle and captured revision,
including unnamed work before any final path. Recovery acceptance is unsaved working state, separate
from explicit Save. New recovery protects the older known valid candidate until a valid successor exists;
incomplete/corrupt candidates require honest validation/fallback. Successful Save permits retirement
only by document/revision/media coverage with other owners respected; later edits stay unsaved. Storage
pressure prioritizes disposable/unowned/redundant material, not arbitrary eviction of uniquely protective
unsaved work. Informed discard has identified scope; preference reset and opening old Save are not discard.
Remaining Q-058: Replacement/validation protocol, candidate indexing/selection metadata and diagnostics,
cadence/debounce, bounded retention/layout, exact recovery-choice realization, recording reconciliation
and cross-platform/process-crash/storage/fault/race evidence. No fixed counts/cadence/locations/UI chosen.
Original Q-059: Managed-media storage integrity, transactions, failure recovery and lifecycle-aware cleanup.
Accepted portion: Managed durable storage precedes gate-valid canonical resource/use acceptance, including
unnamed projects; original source/generator disappearance cannot invalidate accepted audio. Working,
saved, recovery, reusable resources, staging/cache, Undo/Redo, pending/live-use and independent library
ownership differ. Definite pre-commit failure preserves prior coherent state and unsaved edits; around-
commit uncertainty requires evidence-based reconciliation. Partial destination media/document is no
successful Save As/collect/relocate; source retirement is separate. Degraded opening/Save preserves
understandable references/unknown extension state without claiming repaired or fully collected audio.
Repair prepares replacement and validates intended target before one coherent edit; failed repair
preserves old unresolved intent. Recording recovery is bounded by actually stored interpretable audio
and captured edits. Cleanup respects every owner and cannot rely on visible uses, names or timestamps.
Remaining Q-059: Durable storage protocol/layout, integrity/corruption checks, repair/reference transitions,
operation/recording reconciliation, cross-volume Save As/collect/relocate consistency, owner tracking/GC
and platform/fault/race evidence. Atomic visibility, crash consistency and durability are distinct;
no algorithm, ACID/power-loss guarantee, hash/transcoding/dedup or filesystem mechanism selected.
Rationale: [D-072](DECISIONS.md#d-072--recovery-revisions-and-managed-media-failure-ownership).
Current owners: [Project format](../PROJECT_FORMAT.md#media-and-persistence-integrity),
[recovery](../PROJECT_FORMAT.md#recovery-state), [UX](../UX_CONTRACT.md#project-lifecycle-and-durable-work),
[sample workflow](../SAMPLE_WORKFLOW.md#asynchronous-preparation-and-dependency-availability).
Related Q-009 schema/migration, Q-011/Q-012 audition/render, Q-029 references/edit scope, Q-047 execution
lifetime, Q-063 Undo/async, Q-065 library, Q-071 temporary history and Q-010/Q-024 extension preservation
retain their mechanisms. Q-062 coordinates recording/device loss; Q-061 retains full roadmap closure.
R1/R2/R7/R8/R12 and later recording responsibility is narrowly clarified without renumbering/reordering
or expanding R0; SEQ-R0 remains pending / not started.
