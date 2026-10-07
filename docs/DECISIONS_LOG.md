# Decisions log

Role: Register of accepted durable decisions and their rationale.
Read when: Checking why a direction is binding or recording explicit supersession.
Authoritative for: Acceptance history, decision rationale, and supersession status.
Not authoritative for: Current implementation, detailed contracts, proposals, or future stage order.

D-001 through D-008 record the SEQ-KB-R0 mandate; D-009 through D-016 record SEQ-KB-R1 on 2026-10-06.
D-017 through D-022 record SEQ-KB-R2; D-023 through D-028 record SEQ-KB-R3 on 2026-10-06.
D-029 through D-032 record SEQ-KB-R4 on 2026-10-06.
D-033 through D-038 record SEQ-KB-R5 on 2026-10-07.
D-039 through D-044 record SEQ-KB-R6 on 2026-10-07.
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
Refinement: [D-037](#d-037--realistic-external-plugin-boundary) makes stronger isolation explicitly
optional future engineering and records the minimal interoperability/security boundary.
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

Refinement: [D-036](#d-036--object-render-and-audible-capture-are-distinct) separates object-local
sampling from broader audible-selection capture without selecting final taps or command names.

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

## D-023 — Zero-warning managed baseline

Status: Accepted engineering policy; shared compiler enforcement deferred until projects exist.
Basis: SEQ-KB-R3 mandate; [engineering reference study](THIRD_PARTY.md#seq-kb-r3-engineering-reference-study).
Rationale: Nullable production/test code starts from zero warnings and errors. Future inherited MSBuild
policy treats warnings as errors; only narrow reasoned suppressions are permitted, not broad `NoWarn`.
Owner: [CODING_GUIDELINES](CODING_GUIDELINES.md#zero-warning-baseline-and-enforcement).

## D-024 — Repository-owned development conventions

Status: Accepted engineering policy.
Basis: SEQ-KB-R3 mandate, refining D-007; reference study linked above.
Rationale: English implementation/documentation, explicit async/resource/thread ownership, pragmatic UI
boundaries, and mechanical/behavioral refactoring rules must be retrievable without chat memory.
Canonical owners and proportionate machine configuration preserve these obligations without tooling ceremony.
Owners: [CODING_GUIDELINES](CODING_GUIDELINES.md), [DEVELOPMENT](DEVELOPMENT.md).

## D-025 — Windows/Linux/macOS product target with bounded evidence

Status: Accepted portability direction; no runtime parity or release-date claim.
Basis: SEQ-KB-R3 portability mandate, strengthening the earlier Windows-first/later-adapter direction.
Rationale: Windows is primary early development/runtime; Linux/macOS are first-class architectural
targets from the start. Portable contracts and localized OS adapters prevent avoidable coupling.
Hosted build/test evidence does not establish real desktop or audio-device acceptance.
Owners: [PORTABILITY](PORTABILITY.md), evidence tiers in [TEST_EXECUTION](TEST_EXECUTION.md).

## D-026 — Simple initial CI, growth driven by measured cost

Status: Accepted future CI policy; no workflows introduced.
Basis: SEQ-KB-R3 mandate and Fovium/MeasPilot reference study linked above.
Rationale: Start with Windows/Ubuntu/macOS managed restore/Release-build/tests once code exists.
Separate docs validation when a checker exists; account for required-check semantics. Adopt fast/full
feedback and distribution gates only when actual cost warrants them, keeping physical acceptance separate.
Owner: [CI_CD](CI_CD.md); verification in [TEST_EXECUTION](TEST_EXECUTION.md).

## D-027 — Machine-enforced repository consistency where proportionate

Status: Accepted repository policy; passive text/diagnostic configuration introduced.
Basis: SEQ-KB-R3 mandate and MeasPilot's targeted diagnostic guards.
Rationale: LF-normalized text with explicit batch-script exceptions and editor correctness guards reduce
recurring drift/refactoring mistakes. Prefer cheap deterministic enforcement over agent memory; defer
shared MSBuild, SDK pins, analyzers, and CI until actual projects/problems justify them.
Owners: [DEVELOPMENT](DEVELOPMENT.md#machine-enforcement-and-text-consistency), diagnostic rationale
in [CODING_GUIDELINES](CODING_GUIDELINES.md); [.editorconfig](../.editorconfig), [.gitattributes](../.gitattributes).

## D-028 — Separate planning states and retain speculative ideas

Status: Accepted knowledge policy.
Basis: SEQ-KB-R3 planning-state mandate, refining D-007.
Rationale: Agreed staged intent, accepted decisions, required unresolved risks, speculative possibilities,
and actual implementation debt need distinct registers. Ideas may remain indefinitely without scheduling
or resolution; promotion leaves traceability. Design uncertainty is not debt.
Owner: [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md#planning-state-separation);
speculative records in [IDEAS](IDEAS.md).

## D-029 — Seqvium uses Apache License 2.0

Status: Accepted.
Basis: Explicit SEQ-KB-R4 licensing mandate;
[official Apache text](https://www.apache.org/licenses/LICENSE-2.0.txt) and
[OSI approval](https://opensource.org/license/apache-2.0).
Rationale: Seqvium's own work is permissively open source and may be used commercially, modified,
redistributed, and developed into derivative works broadly under the license terms. Apache-2.0 adds
an explicit contributor patent grant without requiring derivative Seqvium code to remain open.
Third-party license boundaries and distribution obligations remain intact.
Owners: [THIRD_PARTY](THIRD_PARTY.md#project-license-and-material-boundaries) for the licensing/provenance
boundary; root [LICENSE](../LICENSE) for authoritative license text.
Resolution: Project-license selection in Q-014 is resolved; future component compatibility remains open.
This completes D-001's earlier FOSS intent without superseding its product direction.

## D-030 — Future ProjectStats repository diagnostics

Status: Accepted future tooling direction; no executable project exists.
Basis: SEQ-KB-R4 repository-tooling mandate, informed by the Fovium structural-diagnostics pattern.
Rationale: A cross-platform, initially BCL-only structural CLI can provide useful repository diagnostics
without production/UI/audio dependencies or quality authority. Deterministic ordering, sanitized metadata,
safe exclusions, advisory diagnostics, and synthetic-repository tests bound its responsibility.
Owner: [PROJECT_STATS](PROJECT_STATS.md); introduction/setup in [DEVELOPMENT](DEVELOPMENT.md),
test placement/evidence in [TEST_EXECUTION](TEST_EXECUTION.md).
Boundary: Requires later explicit code authorization; not part of SEQ-R0 and not started by SEQ-KB-R4.

## D-031 — Compact current-state handoff with bounded evidence

Status: Accepted repository policy, refining D-007.
Basis: SEQ-KB-R4 handoff mandate and mature Fovium/MeasPilot current-state growth experience.
Rationale: Checkpoint, capability, focus, latest meaningful validation, and active gaps answer where the
project is now. A soft 50–80-line discipline and links to proper history/decision/evidence owners preserve
readability without a machine gate or chronological test-run accumulation.
Owner: [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md#compact-current-state-contract);
current facts in [PROJECT_STATE](PROJECT_STATE.md), evidence labels in [TEST_EXECUTION](TEST_EXECUTION.md).

## D-032 — One main managed test project first

Status: Accepted future test architecture policy; framework/platform unselected.
Basis: SEQ-KB-R4 test-topology and deterministic-evidence mandate.
Rationale: Domain/feature folders in one main suite avoid a project per subsystem. Separate projects
need different execution/dependency contracts. Deterministic concurrency, generated fixtures, offline
audio/scheduling, compatibility coverage, and separate manual/device evidence support meaningful checks.
Owner: [TEST_EXECUTION](TEST_EXECUTION.md); ProjectStats-specific cases in [PROJECT_STATS](PROJECT_STATS.md).

## D-033 — Processing granularity and independent placements

Status: Accepted architecture requirement, refining D-019.
Basis: Explicit SEQ-KB-R5 architecture-question review; no runtime evidence claimed.
Rationale: Full local DSP graphs belong to standalone item/clip/fragment-like scopes, not every atomic
note/trigger/step. Bounded event expression and a low-ceremony path to a separately processed item
preserve creative flexibility. Shared instrument definitions do not force shared execution state or
irreversible mixed audio; overlapping placements must retain required downstream independence.
Resource/performance strategy remains open, with no selected voice/instance mechanism or free duplication.
Owners: [ARCHITECTURE](ARCHITECTURE.md#processing-granularity-and-shared-definitions) for the model,
[AUDIO_ENGINE](AUDIO_ENGINE.md) for execution, [NODE_GRAPH](NODE_GRAPH.md) for graph scopes.
Open mechanics: Q-019, Q-047, Q-048 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).

## D-034 — Separate musical sharing identities

Status: Accepted model requirement, refining D-010 and D-019.
Basis: Explicit SEQ-KB-R5 sharing mandate.
Rationale: Pattern musical content, instrument/sound definition, placement state, and processing state
may be shared or independent separately. Shared Pattern edits affect its placements; placement-local
edits do not modify every use. Musical-content variation and sound-definition independence are separate
intentions, without implicit detachment of every relationship or a universal unlink-everything model.
Owners: [ARCHITECTURE](ARCHITECTURE.md#separate-sharing-identities) for identities,
[UX_CONTRACT](UX_CONTRACT.md) for understandable sharing, [PROJECT_FORMAT](PROJECT_FORMAT.md) for persistence.
Open mechanics: Q-029; conceptual action names do not select final UI or copy/storage mechanics.

## D-035 — Processing context follows containment

Status: Accepted model/UX requirement, refining D-011 and D-022.
Basis: Explicit SEQ-KB-R5 move-semantics mandate.
Rationale: Material moved into a context owning processing or other meaningful behavior is subject to
that context and may sound different. Pure organizational grouping must not silently change sound.
Organization, musical/timeline containment, and mixer routing remain distinct and meaningful processing
contexts must be distinguishable in the UI.
Owners: [ARCHITECTURE](ARCHITECTURE.md#semi-free-arrangement) for relationships,
[UX_CONTRACT](UX_CONTRACT.md) for observable behavior. Q-028/Q-030 retain terminology and routing mechanics.

## D-036 — Object render and audible capture are distinct

Status: Accepted workflow requirement, refining D-018.
Basis: Explicit SEQ-KB-R5 sampling and rendered-replacement mandate.
Rationale: Object sampling includes the selected object's own semantic/local processing boundary;
audible-selection capture serves a broader selected musical/time context. Unrelated downstream
container/bus/Master processing is not automatically baked into an object sample. Explicit rendered
replacement must be undoable and avoid silently reapplying exact baked processing; sources survive
by default. Names, capture boundaries/tails, and replacement mechanics remain unresolved.
Owner: [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md#resampling); execution in [AUDIO_ENGINE](AUDIO_ENGINE.md).
Open mechanics: Q-012/Q-049 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).

## D-037 — Realistic external plugin boundary

Status: Accepted extension constraint, refining D-016 and the scope of D-013.
Basis: Explicit SEQ-KB-R5 third-party responsibility/lifecycle mandate.
Rationale: Normal compatibility/lifecycle and declared/recoverable failures require deliberate host
handling and state preservation. Arbitrary native/in-process third-party faults may crash or corrupt
the host; hard isolation is not a baseline promise. Stronger process/sandbox isolation is optional
future engineering, and process separation alone is not security sandboxing or malicious-code containment.
Third-party editors may use top-level OS windows when internal panes are unsuitable; first-party
workspace direction remains intact. Seqvium owns its host contract; plugin authors own plugin-specific
behavior/UI. Compatibility does not make independent plugins Seqvium-authored or warrantied.
Owners: [EXTENSIONS](EXTENSIONS.md) for hosting/security limits, [WORKSPACE](WORKSPACE.md) for editor windows,
[THIRD_PARTY](THIRD_PARTY.md) for provenance/license boundary; root [LICENSE](../LICENSE) for legal terms.
Open work: ordinary lifecycle API/ABI in later hosting; Q-025 is conditional optional-isolation evaluation,
not a required initial boundary or an accepted process architecture.

## D-038 — Self-contained project media by default

Status: Accepted project durability requirement, strengthening D-005 and D-019.
Basis: Explicit SEQ-KB-R5 project-media mandate.
Rationale: Normally used audio becomes project-managed/durable rather than fragile links into arbitrary
folders. Imported samples, accepted Sample Lab audio, recordings, and used pack material travel with
the normal project path without copying unused whole libraries. Original pack/generator removal must
not lose accepted managed audio. External references remain an explicit distinguishable alternative.
Owners: [PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary) for persistence/defaults,
[ARCHITECTURE](ARCHITECTURE.md) for resource identity, [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) for acceptance,
[EXTENSIONS](EXTENSIONS.md) for source removal. Q-009 retains container/schema and media-management mechanics.

## D-039 — Platform-authoritative plugin compatibility

Status: Accepted platform/compatibility constraint, refining D-014 through D-016 and D-037.
Basis: Explicit SEQ-KB-R6 architecture clarification; no implementation/runtime evidence claimed.
Rationale: Plugins declare/satisfy required current host contracts and new development targets the
available platform/SDK contract. Deliberate compatibility before activation prevents known unsupported
execution; installed incompatibility does not authorize automatic deletion. Preserved data/identity and
exact historical sonic identity are separate promises: plugins own algorithm-version sound and the
platform need not emulate every historical engine indefinitely. Breaking audio changes require deliberate policy.
Owners: [EXTENSIONS](EXTENSIONS.md#host-context-and-compatibility) for plugin lifecycle/sound,
[ARCHITECTURE](ARCHITECTURE.md) for authority, [AUDIO_ENGINE](AUDIO_ENGINE.md#sound-compatibility-boundary)
for platform sound, [PROJECT_FORMAT](PROJECT_FORMAT.md) for data preservation.
Open mechanics: Q-024/Q-050 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md); no final manifest/API/version resolver.

## D-040 — Tempo-aware audio and non-destructive timeline editing

Status: Accepted musical/editing direction, refining D-019 and D-038.
Basis: Explicit SEQ-KB-R6 tempo, timeline-edit, and tail-boundary mandate.
Rationale: Project tempo governs musical entities; audio supports tempo-following and fixed/source time
with independent local stretch. Trim, loop/repeat, and stretch express distinct intentions. Ordinary
trim/split/rearrange preserves durable source audio. A source end may leave an effect tail running;
an explicit hard cut is a separate request, without selecting reset/render/loop behavior.
Owners: [ARCHITECTURE](ARCHITECTURE.md#project-tempo-and-audio-time) for model,
[UX_CONTRACT](UX_CONTRACT.md#audio-timeline-editing) for edits,
[PROJECT_FORMAT](PROJECT_FORMAT.md) for preservation,
[AUDIO_ENGINE](AUDIO_ENGINE.md#source-boundaries-and-effect-tails) for tails.
Open mechanics: Q-012/Q-028/Q-051; no final labels/defaults, gestures, time/stretch representation, or algorithm.

## D-041 — Last-valid execution with visible recoverable feedback

Status: Accepted runtime/UX constraint, refining D-012 and D-021.
Basis: Explicit SEQ-KB-R6 live graph-editing mandate.
Rationale: Invalid/incomplete candidates must not destroy valid playing audio. A valid prepared
candidate can replace execution safely; the UI must clearly show when visual edits differ from current
execution. Concise graphical/recoverable feedback supports editing without cascading modal errors.
Owners: [NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution) for graph rule,
[AUDIO_ENGINE](AUDIO_ENGINE.md) for runtime,
[UX_CONTRACT](UX_CONTRACT.md#graph-state-and-recoverable-failures) for interaction,
[UI_DESIGN](UI_DESIGN.md#feedback-and-motion) for visuals.
Open mechanics: Q-018/Q-015; no compiler/publication strategy or final visuals selected.

## D-042 — Bounded diagnostics and user/project settings separation

Status: Accepted application configuration/diagnostic policy.
Basis: Explicit SEQ-KB-R6 logging, storage, and reset mandate.
Rationale: Quiet meaningful production warnings/errors and bounded storage avoid permanent verbose
streams and unbounded logs. Ordinary GUI needs at most an enable/disable preference; detailed developer
logging is activated outside it. Per-user preferences and project-affecting plugin instance state have
different ownership, and configuration reset must not delete projects or managed media.
Owners: [SETTINGS](SETTINGS.md) for policy/reset,
[PROJECT_FORMAT](PROJECT_FORMAT.md#project-state-and-user-preferences) for instance persistence,
[WORKSPACE](WORKSPACE.md) for layout, [DEVELOPMENT](DEVELOPMENT.md#developer-diagnostics) for developer activation,
[UX_CONTRACT](UX_CONTRACT.md) for settings interaction.
Open mechanics: Q-052/Q-053; no exact log limits, paths, formats, or reset controls selected.

## D-043 — Host-owned localization and semantic theme resources

Status: Accepted platform/UI direction.
Basis: Explicit SEQ-KB-R6 localization and theme-resource mandate.
Rationale: Russian/English initially and additional languages later use host localization contracts
without translated display text becoming stable identity. Dark/Light themes and centralized semantic
style resources let first-party/native surfaces evolve coherently; independently rendered external
native editors need not adopt them. Example resource names do not establish API identifiers.
Owners: [ARCHITECTURE](ARCHITECTURE.md#host-localization-and-ui-resources) for localization/services,
[UI_DESIGN](UI_DESIGN.md#themes-and-semantic-resources) for themes/roles,
[EXTENSIONS](EXTENSIONS.md) for contribution boundaries.
Open mechanics: Q-054/Q-055; localization formats/fallback/workflow and theme packaging remain open.

## D-044 — Bounded realtime overload and future Live / Low-Latency mode

Status: Accepted realtime constraint and future product/platform direction, refining D-006.
Basis: Explicit SEQ-KB-R6 overload and live-processing mandate; no timing/latency evidence claimed.
Rationale: Deadline misses must recover toward current realtime progress rather than create unlimited
backlog/latency. Superseded controls/preparation and musical events need different overload semantics;
critical stop/release/panic recovery must be explicit later. Future latency-heavy live-path handling is
visible, temporary, and separate from project edits and the intended full final/offline render.
Owners: [AUDIO_ENGINE](AUDIO_ENGINE.md#bounded-overload-and-semantic-recovery) for overload and
[Live direction](AUDIO_ENGINE.md#live--low-latency-direction), [NODE_GRAPH](NODE_GRAPH.md) for graph preservation,
[UX_CONTRACT](UX_CONTRACT.md#live-processing-feedback) for visible behavior.
Open mechanics: Q-004/Q-005/Q-007/Q-021; no universal late-event drop policy, scheduler, thresholds,
reporting contract, compensation/bypass algorithm, or new SEQ-R0 scope selected.

Final application decomposition, engine language/backend/ABI, extension/package API, project format,
visual language, plugin-hosting/isolation strategy, graph compiler/port ABI, workspace layout
mechanism, platform release schedule, final Layer/Track terminology/schema, node-settings UX,
arbitrary feedback, automation/modulation formula, and compact-chain visuals are **not accepted decisions**.
SDK pin/roll-forward, final UI adoption, native toolchain/warning policy, release RIDs, CI actions/filters,
analyzer packages, and test framework/platform are also unselected, alongside settings paths/formats,
log limits, localization format/fallback, theme packaging,
time/stretch representation/algorithms, tail/reset rules, realtime overload/recovery, low-latency policy,
and change-specific historical sonic compatibility. Concrete required questions belong
to [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md); speculative possibilities belong to [IDEAS](IDEAS.md).
