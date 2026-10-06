# Decisions log

Role: Register of accepted durable decisions and their rationale.
Read when: Checking why a direction is binding or recording explicit supersession.
Authoritative for: Acceptance history, decision rationale, and supersession status.
Not authoritative for: Current implementation, detailed contracts, proposals, or future stage order.

D-001 through D-008 record the SEQ-KB-R0 mandate; D-009 through D-016 record SEQ-KB-R1 on 2026-10-06.
D-017 through D-022 record SEQ-KB-R2 on 2026-10-06.
They are accepted direction/constraints, not claims of implementation. Linked owners define the current
contract. No supersessions exist yet; the later KB stages refine direction without accepting technical proposals.
New decisions need an ID, status, basis/evidence, rationale, affected owner, and explicit supersession
link when replacing an earlier decision. Keep rejected or superseded reasoning available as history.

## D-001 — FOSS music workstation with its own character

Status: Accepted direction.
Basis: SEQ-KB-R0 product mandate.
Rationale: Approachable defaults must support growth into real tracks; feature-count competition and
copying a generic DAW workflow would weaken the creative focus. FOSS intent does not select a license.
Owner: [PROJECT_VISION](PROJECT_VISION.md).

## D-002 — Progressive complexity and immediate feedback

Status: Accepted direction.
Basis: SEQ-KB-R0 UX requirements.
Rationale: Sound and music should dominate, with minimum ceremony and discoverable depth.
Owner: [UX_CONTRACT](UX_CONTRACT.md).

## D-003 — Sound discovery and resampling define the creative loop

Status: Accepted direction.
Basis: SEQ-KB-R0 sample workflow requirements.
Rationale: Cheap movement between exploring sounds and hearing them in music gives Seqvium a
distinctive creative path. Source-preserving resampling supports continued experimentation.
Owner: [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md).

## D-004 — The default generator is an optional extension

Status: Accepted direction.
Basis: SEQ-KB-R0 modularity requirement.
Rationale: Default installation must not make a capability inseparable from core integrity. Initial
generation should be procedural/local, independent of a cloud AI service.
Owner: [EXTENSIONS](EXTENSIONS.md).

## D-005 — Preserve material when capabilities are missing

Status: Accepted direction.
Basis: SEQ-KB-R0 durability requirements.
Rationale: Accepted generated audio survives generator removal and must not change through implicit
regeneration. Missing extensions must retain identity, state, and relationships across save/load.
Owners by subject: [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) for acceptance,
[EXTENSIONS](EXTENSIONS.md) for lifecycle, [PROJECT_FORMAT](PROJECT_FORMAT.md) for persistence.

## D-006 — Isolate realtime execution from ordinary application behavior

Status: Accepted constraint.
Basis: SEQ-KB-R0 realtime requirements.
Rationale: Audio deadlines cannot depend on arbitrary UI/domain mutation, blocking work, or UI-thread
availability. This accepts isolation, not C++, miniaudio, a particular ABI, or a queue design.
Owner: [AUDIO_ENGINE](AUDIO_ENGINE.md); cross-boundary ownership in [ARCHITECTURE](ARCHITECTURE.md).

## D-007 — Durable repository knowledge with selective routing

Status: Accepted repository policy.
Basis: SEQ-KB-R0 AI-assisted development requirements.
Rationale: A fresh agent/contributor must navigate current truth without chat history. One owner per
durable subject and selective retrieval permit later tooling without replacing canonical Markdown.
Owner: [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md); operations in [AGENTS](../AGENTS.md).

## D-008 — Evidence before production audio architecture

Status: Accepted process boundary.
Basis: SEQ-KB-R0 scope and SEQ-R0 probe mandate.
Rationale: This milestone supplies documentation only; a bounded audio experiment must test the
candidate architecture before major production construction. Simple boundaries are preferable to
speculative production layers.
Owner: [ARCHITECTURE](ARCHITECTURE.md); stage scope in [ROADMAP](ROADMAP.md).

## D-009 — Mouse-first universal creation with complementary performance input

Status: Accepted product direction.
Basis: SEQ-KB-R1 input and product mandate.
Rationale: Direct pointer editing leads creation, while on-screen keyboard, MIDI/realtime note input,
and eventual audio/MIDI recording support music across genres. Note-entry paths converge on compatible
events; ordinary keyboard shortcuts remain primary and a QWERTY piano must be optional/explicit.
Owners: [UX_CONTRACT](UX_CONTRACT.md) for input, [PROJECT_VISION](PROJECT_VISION.md) for audience/specialization.

## D-010 — Named reusable multi-instrument patterns

Status: Accepted model direction.
Basis: SEQ-KB-R1 Pattern requirements.
Rationale: Users choose the granularity of a musical idea. Repeated placements normally share data;
independent variations are explicit. Pattern identity is not an arrangement-track or mixer-channel identity.
Owner: [ARCHITECTURE](ARCHITECTURE.md#intended-musical-model).

## D-011 — Organizational groups are independent of Pattern membership

Status: Accepted model direction.
Basis: SEQ-KB-R1 instrument/channel group requirements.
Rationale: Named/collapsible user organization must support multiple patterns and cross-group musical
ideas without becoming pattern storage identity or signal routing. Exact hierarchy remains open.
Owner: [ARCHITECTURE](ARCHITECTURE.md#intended-musical-model).

## D-012 — Core signal graph with a prepared execution boundary

Status: Accepted platform direction.
Basis: SEQ-KB-R1 node-graph requirements.
Rationale: Graph-based transformation is core creative depth, separate from timeline/organization.
Semantically distinct ports and prepared execution support coherent processing without making the
realtime callback traverse mutable editor state. Compiler, ABI, scopes, and exact core-node list are open.
Owners: [NODE_GRAPH](NODE_GRAPH.md) for graph semantics, [ARCHITECTURE](ARCHITECTURE.md) for separation,
[AUDIO_ENGINE](AUDIO_ENGINE.md) for execution constraints.

## D-013 — Flexible internal workspace panes in one main window

Status: Accepted workspace direction.
Basis: SEQ-KB-R1 workspace mandate.
Rationale: User composition, activation/front behavior, collapse/restore, and predictable per-pane
docking should serve music without aggressive layout capture. Layout is primarily user/application state;
project opening should normally preserve it. Exact docking/persistence/framework choices are open.
Owner: [WORKSPACE](WORKSPACE.md).

## D-014 — Distinguish modular core, replaceable backend, and optional plugin

Status: Accepted architecture constraint.
Basis: SEQ-KB-R1 core ownership requirements.
Rationale: Long-term project, transport, audio/graph/device, MIDI/recording, mixing/resource, edit,
serialization, extension-hosting, and workspace responsibilities remain core even when delivered later.
Internal adapters are not automatically user plugins; specialized contributions cannot own the platform.
Owner: [ARCHITECTURE](ARCHITECTURE.md); plugin lifecycle in [EXTENSIONS](EXTENSIONS.md).

## D-015 — Plugins consume host context independently of the device backend

Status: Accepted processing constraint.
Basis: SEQ-KB-R1 host processing and backend requirements.
Rationale: Host-owned rate/block/channel and musical context allow adaptable processing. Ordinary
plugins must not depend on a selected device backend or assume fixed `44.1 kHz` while claiming general
processing. No backend or supported rate/channel range is selected by this constraint.
Owner: [AUDIO_ENGINE](AUDIO_ENGINE.md); capability handling in [EXTENSIONS](EXTENSIONS.md).

## D-016 — Resolve compatibility deliberately and contain failures within technical limits

Status: Accepted extension constraint.
Basis: SEQ-KB-R1 compatibility and failure-containment requirements.
Rationale: Resolve versions/capabilities/formats/state deliberately; disable with diagnostics when no
safe path exists, preserving project/plugin state instead of intentionally failing the host. Arbitrary
in-process native faults cannot be promised contained; hard crash isolation remains a design question.
Owner: [EXTENSIONS](EXTENSIONS.md).

## D-017 — Contextual Sample Lab

Status: Accepted creative-workflow direction.
Basis: SEQ-KB-R2 product mandate.
Rationale: A dedicated Sample Lab pane supports standalone exploration and selected musical context.
Temporary, reversible candidates should be auditionable through relevant existing downstream processing
before explicit acceptance, avoiding export/import and routing reconstruction to judge a variation.
Substitution, publication, restoration, and exact downstream boundaries remain open.
Owner: [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md); pane integration in [WORKSPACE](WORKSPACE.md).

## D-018 — What-you-hear sampling follows semantic selection

Status: Accepted sampling direction.
Basis: SEQ-KB-R2 sampling mandate.
Rationale: Ordinary sample creation defaults to the audible semantic result of the selected musical
source/context rather than surprising dry/raw material. An item's result, a container's combined
result, and an explicit range are distinct scopes, not necessarily whole-Master capture. Advanced
source/tap alternatives remain optional future depth; concrete DSP boundaries remain unresolved.
Owner: [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md); execution constraints in [AUDIO_ENGINE](AUDIO_ENGINE.md).

## D-019 — Two local processing levels with non-destructive placements

Status: Accepted model direction.
Basis: SEQ-KB-R2 processing and resource-identity mandate.
Rationale: Independent item-local processing and common containing-container processing bound ordinary
creative reasoning. Local edits do not silently rewrite shared audio resources. Mixer/buses/master
remain separate global routing/output responsibilities, not unlimited nested local layers. "Layer"
terminology, container identity, graph scopes, and schemas are still open.
Owner: [ARCHITECTURE](ARCHITECTURE.md); persistence obligations in [PROJECT_FORMAT](PROJECT_FORMAT.md).

## D-020 — Graph topology drives processing, coordinates organize presentation

Status: Accepted graph semantic direction.
Basis: SEQ-KB-R2 graph mandate.
Rationale: Free two-dimensional placement supports readable personal layouts; connections express
signal dependencies and order rather than coordinates or hidden numeric priorities. Execution
scheduling remains unselected, and arbitrary feedback/cycles are not accepted.
Owner: [NODE_GRAPH](NODE_GRAPH.md).

## D-021 — Progressive graph visibility supports creative interaction quality

Status: Accepted UX/design direction.
Basis: SEQ-KB-R2 progressive-complexity and design-quality mandate.
Rationale: Compact interactive processing indications keep depth discoverable while graph maps
normally remain hidden until requested. Pleasant manipulation, hierarchy, spatial clarity, and
restrained feedback help Seqvium feel like a creative instrument. Exact visuals, compact-chain
representation, and node-settings UX are not selected.
Owners: [UX_CONTRACT](UX_CONTRACT.md) for visibility, [UI_DESIGN](UI_DESIGN.md) for visual principles,
[NODE_GRAPH](NODE_GRAPH.md) for graph interaction, [WORKSPACE](WORKSPACE.md) for target-pane focus.

## D-022 — Semi-free Arrangement with separate mixer identity

Status: Accepted musical-model direction.
Basis: SEQ-KB-R2 Arrangement mandate.
Rationale: User-named structured timeline containers organize reusable compatible material without
permanent one-instrument ownership or arbitrary duplication. Where/when material is arranged remains
distinct from audio routing, even when a container exposes common processing. Preferred targets,
compatibility, nesting, container-to-mixer relationships, and final Track schema remain open.
Owner: [ARCHITECTURE](ARCHITECTURE.md#semi-free-arrangement).

Final application decomposition, engine language/backend/ABI, extension/package API, project format,
license, visual language, plugin-hosting/isolation strategy, graph compiler/port ABI, workspace layout
mechanism, platform release schedule, final Layer/Track terminology/schema, node-settings UX,
arbitrary feedback, automation/modulation formula, and compact-chain visuals are **not accepted decisions**.
Their uncertainty belongs to [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
