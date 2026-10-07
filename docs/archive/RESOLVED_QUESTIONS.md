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
