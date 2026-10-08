# Roadmap

Role: Ordered current and future development direction.
Read when: Scoping the next stage or evaluating a future capability.
Authoritative for: Current/future stage goals, sequence, and scope boundaries.
Not authoritative for: Completion history, accepted technical contracts, or existing debt.

This is a direction, not a promise to implement all capabilities in v0.1. Later ordering may change
with evidence and product need. Current checkpoint belongs to [PROJECT_STATE](PROJECT_STATE.md);
contracts belong to their canonical owners. Completed stages are removed from current planning and
retained in the cold archive
under [rolling governance](DOCUMENTATION_GOVERNANCE.md#rolling-current-knowledge-and-cold-history).

SEQ-R0 has authorized bounded experimental evidence and remains **partially evidenced**; see its
[report](experiments/SEQ-R0_REPORT.md). R1 was separately authorized and its bounded foundation is
complete; see [PROJECT_STATE](PROJECT_STATE.md). This roadmap/probe result grants no authorization
for later stages. ProjectStats needs separate explicit code authorization;
whether a dedicated tooling stage is useful remains Q-046, not an inserted commitment.
[Open questions](KNOWN_PROBLEMS.md), including First Track Release dependency closure Q-061, do not
reorder stages or make recording, automation, external hosting or ProjectStats implicit prerequisites.

## Sequencing philosophy

Prioritize durable, extensible foundations over the fastest visible MVP/demo. Substantial document/domain,
serialization, resource, transport, audio/backend, realtime, plugin and edit/async integrity work may
legitimately precede impressive UI. Project creation and the document lifecycle are foundational ownership,
not optional late polish. Settings/diagnostics and host localization/theme rails enter where dependencies
need them under [ARCHITECTURE](ARCHITECTURE.md#foundational-ownership-and-project-lifecycle).
This is dependency correctness, not a requirement to finish every backend subsystem before any UI or
mandate extra architectural layers/projects.

A later meaningful end-to-end project/track remains a valuable acceptance milestone. Piano Roll playing
a pattern alone is insufficient as the final usable-product criterion, and useful closure does not require
approximating a complete FL Studio. Do not weaken/reorder foundations to reach an MVP at any cost.
The conceptual dependency audit supports the present R0–R12 order and the bounded scenario below.
No hard architectural cycle or additional document-level prerequisite to scoping R0 was identified.
This is planning-level closure only: Q-061 retains implementation-specific dependency checks, actual
capability/range choices and end-to-end evidence as stages arrive. No technical feasibility or release
acceptance follows from this conclusion; each implementation stage requires separate authorization.

## First Track scenario and acceptance boundary

The bounded R12 acceptance direction is a short composition with distinct sections, rhythm and pitched
music, rather than a tone or one looping Pattern. A representative case is 16 bars in 4/4 at a chosen
tempo: imported Kick/Snare/Hat WAVs, a known-pitch short tonal WAV played by the core sampler for bass
and melody, repeated shared drum/bass Patterns, one explicit independent fill/phrase variation, and
two occurrences of one directly arranged texture WAV, plus one deliberately independent sampler sound
configuration. Counts, tempo and genre are illustrative, not format limits, bundled-content commitments
or a fixed benchmark. No built-in synthesizer is required.

Create it in an unnamed project; find/import/audition resources; edit steps and pitched note durations/
velocities; arrange shared uses and a variation; move, trim/split, repeat and locally stretch supported
audio, distinguishing fixed-time from tempo-following behavior. Exercise item-local and explicit
containing processing with available core processors, distinct basic routes/channels and an intentional
submix, gain/pan/mute/solo and bounded EQ/compression. Save/Save As, close/reopen and render a coherent
WAV range from canonical state. Verify timing, sharing, sound/resource references, local edits, routing
and processing without duplicate DSP. Demonstrate degraded access plus deliberate repair/replacement/
removal for a missing managed sample, and recovery of a captured unnamed/unsaved revision after simulated
interruption. Recovery does not promise every edit or unpersisted audio survives.

R7 generation and R8 object resampling require their own bounded acceptance and integration into project
reuse by R12; using them in this composition is optional. The core sample path must finish the track
when the removable generator is absent, and accepted generated audio must remain usable without it.
Unavailable required processing still blocks its dependent render; removal is not permission for silent
bypass. An external effect/plugin host is unnecessary to exercise the recoverable-media failure case.

Validate each declared distribution target under [platform ownership](#platform-release-acceptance-ownership),
including the delivered application, actual GUI/audio and project integrity. Scope exact supported
audio/time operations, formats, rates/channels, render entry-state/tails and platforms with evidence
during their owning stages. R12 integrates proven foundations; it must not first invent their ownership.
Rich recording, external hosting, mature sends/sidechains/modulation/automation, Personal Library
publication and advanced pitch/time processing remain later work. This scenario assigns existing
requirements to a usable acceptance direction, not a complete R12 specification or implementation claim.

## Execution and timing acceptance ownership

Under [Audio](AUDIO_ENGINE.md), [Graph](NODE_GRAPH.md), [Architecture](ARCHITECTURE.md) and
[Test execution](TEST_EXECUTION.md), the minimum execution path develops incrementally:

- R1 establishes compatible event/time and persistent reference/edit foundations, including room for
  processing contexts/routes without implementing Arrangement or Mixer. R1's bounded schema resolves
  Q-008 and narrows Q-009/Q-029/Q-019;
  later graph/audio capabilities must validate their expanded relationships.
- R2 owns the core sampler's audible pitched-note, velocity, duration/release and overlapping-event
  behavior for its declared scope, plus basic transport/scheduling and output sufficient to exercise it.
  Short tonal samples can provide meaningful bass/melody; sustained loops, multisampling and a synthesizer
  are not implied. Resolve that minimum source/time behavior before R5/R9 rely on it. Subsystem/API and
  controlled device/offline exercises suffice before R3; no mandatory CLI product or polished Browser.
- R4 extends scheduling into prepared graph execution using bounded model-driven events; source/input,
  output, gain, convergence/routing and stable attachment ownership must be executable without R5 or
  optional modules. Compare the supported graph path offline as well as realtime. Reject unsupported
  topology/dependencies explicitly. Full scope UI and every processor remain later work.
- R5 proves musical looping/live edits over that same scheduler and event model; R9 extends editing,
  not sound production. R7 auditions only existing Pattern/sound and processing contexts; unsupported
  Arrangement targets cannot be offered as working contextual audition before R10.
- R8 owns production frozen-scope offline preparation/render for at least a Pattern-content scope with
  its actual sound uses, rate/channels, finite range/state/tails, cancellation and durable acceptance.
  Other object scopes require actual prior support; no R10 Arrangement or R11 Mixer prerequisite.
- R10 extends execution/offline scheduling to arranged shared/independent occurrences and audio time
  mappings, including bounded audible local stretch. R11 extends the same path with core EQ/compression
  and user-facing routes/controls; those processors must also work in supported local contexts.
- R4 and every later stage introducing latency-bearing paths own necessary timing/reporting/alignment
  for those paths, revisited before R8/R10/R11 completion. A deliberately narrower supported processor
  set can suffice; silently misaligned shipped paths cannot. Q-021 retains mechanisms, not a demand
  for universal plugin compensation or future Live mode. Q-005/Q-017/Q-018/Q-047/Q-057 retain scheduling,
  typing, publication, source-state and DSP/range evidence for each actual capability.
- R12 owns final export controls/WAV output delivery, packaging and whole-scenario evidence. Device-
  independent preparation, scheduling, DSP and media integrity must already serve prior stages.

R5 integrates simple on-screen musical audition over R2; R9 uses that source path for pitched editing.
No MIDI recording or global typing-keyboard piano mode is required for the scenario.
R3 owns initial focus/semantic-action targeting and ordinary keyboard-accessible window/pane controls;
R4/R5/R7/R9/R10/R11 extend them with each editor, using R1 transaction boundaries. R2 owns resource
retention for working/saved/history/pending/live owners as applicable from first import; future recovery
owners must fit that foundation. R12 completes recovery/repair experience, not fundamental lifetime.
These are bounded ownership/acceptance clarifications. They choose no engine, scheduler, storage protocol,
DSP algorithm, UI framework or public ABI; all stage IDs/order and R0 scope remain unchanged.

## Discovery and reuse ownership

Minimum discovery/reuse has bounded owners under
[Sample workflow](SAMPLE_WORKFLOW.md#browser-discovery-and-ownership): R2 establishes external WAV and
project-resource find/audition/import/reuse; R7 integrates accepted Sample Lab material; R8 returns
rendered samples to that same project-resource path; R12 requires usable external/installed-pack and
project-resource access sufficient for a small track. These are capability commitments, not a mature
Browser/catalog or a requirement to build the full workspace shell before R3.

Explicit cross-project Personal Library sample/preset publication belongs to **SEQ-R14+**, after R12,
with bounded delivery scope/order still to be designed there. It is not a First Track Release prerequisite.
Earlier project acceptance/reuse must work without it and must not silently accumulate global content.
No stage is inserted, renumbered or reordered; Q-061 retains broader release dependency closure.

## Localization and theme foundation ownership

Minimum host presentation dependencies have bounded stage owners under
[Architecture](ARCHITECTURE.md#host-localization-and-ui-resources),
[UI design](UI_DESIGN.md#themes-and-semantic-resources) and
[Extensions](EXTENSIONS.md#ui-resource-contribution-lifecycle):

| Stage                           | Minimum responsibility                                                                                                                                                                                                                                                                                                                                                                                                                |
|---------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| R3 — Workspace Shell Foundation | Host-owned stable localization lookup, initial first-party RU/EN with English/per-resource host fallback, semantic Dark/Light roles sufficient for the shell, and safe user language/theme preference behavior. The shell works without future extension hosting or mature dynamic registration.                                                                                                                                      |
| R6 — Extension Foundation       | Extend the R3 foundation with contributor-scoped identity, supported languages/fallback, bounded registration/availability and safe resource/UI retirement for actual native first-party modules. Late contributions use current host preferences; metadata discovery need not activate arbitrary executable code. Resource contract evolution must be handled deliberately, with exact versioning/registration mechanics still open. |
| R7 — Sample Lab / Generator V1  | The default-installed removable generator and its first-party UI consume R3/R6 host localization/semantic styles, including fallback and removal behavior; no separate translation/theme stack.                                                                                                                                                                                                                                       |
| R12 — First Track Release       | Within the bounded small-track scenario, require coherent RU/EN and Dark/Light presentation, safe preference changes, usable missing-resource/missing-capability host explanation and preservation of project values/state. Validate the surfaces actually shipped; missing presentation must not become apparent project corruption.                                                                                                 |

This assigns minimum capabilities, not a framework, resource format, token catalog, packaging model,
marketplace or arbitrary external-native-editor control. R6 does not require mature theme/plugin
ecosystem management. R12 does not require additional languages/themes, live resource reload, universal
third-party recovery or accessibility certification. Q-054/Q-055/Q-067 retain concrete mechanisms and
evidence; Q-061 retains the full dependency/release-scenario audit. Stage IDs/order and R0 scope stay unchanged.

## Cross-context routing ownership

The [graph contract](NODE_GRAPH.md#cross-context-signal-and-control-relationships) accepts bounded
semantic dependencies, not universal routing or an early full sidechain/modulation system.

| Stage                         | Bounded responsibility                                                                                                                                                                                                                                           |
|-------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| R4 — Node Graph Foundation    | Preserve semantic distinctions and dependency validation/prepared-execution foundations capable of later explicit cross-context relationships. Validate supported connections; full cross-context UI, all taps and mature scheduling/modulation are not required |
| R8 — Resampling               | Account for required external dependencies in the supported frozen object-render scope, or explicitly report unsupported/blocked cases. Never claim equivalent rendering after silently omitting them                                                            |
| R10/R11 — Arrangement / Mixer | Introduce only bounded routing/control relationships justified by those workflows, honoring existing contexts, meaningful endpoint/tap identity and independent contributions. No mandatory complete sends/sidechain/modulation framework                        |
| R12 — First Track Release     | Mature cross-context sidechains/modulation are not prerequisites unless indispensable to the later accepted small-track scenario. Any supported relationship must retain Save/Undo, validity and render integrity; Q-061 owns concrete scenario closure          |
| R14+ — Evidence-led expansion | Richer sidechains, sends, control routing and modulation may expand with evidence; representation, timing, lifecycle, capability support and detailed UI remain bounded design work                                                                              |

This narrows ownership without inserting, renumbering or reordering stages. Q-066 remains open for
mechanisms/evidence. R0's baseline control exchange is not cross-context routing acceptance; its probe
scope is unchanged and does not acquire a hidden advanced-routing prerequisite.

## Recovery and managed-media integrity ownership

[Project format](PROJECT_FORMAT.md#media-and-persistence-integrity) owns the failure/lifetime contract;
the following assigns minimum responsibility within existing stages, not a full storage feature set.

| Stage                           | Bounded responsibility                                                                                                                                                                                                                                               |
|---------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| R1 — Domain Foundation          | Document identity/lifecycle, coherent canonical versioned Save/reopen and basic edit/Undo boundaries; sufficient revision/resource/ownership relationships for future recovery/media. Full recovery UI, storage GC and every media workflow are not R1 prerequisites |
| R2 — Audio Resource Foundation  | Durable managed import/acceptance, partial-import failure safety, resource/source distinction and unnamed-project media lifetime; preview stays transient, accepted audio survives source disappearance                                                              |
| R7/R8 — Generation / Resampling | Owned async inputs/outputs, prepare/durable-store/revalidate/commit boundaries, cancellation/non-commit handling and Undo-retained audio; explicit fresh reuse where supported, no wrong-target attachment or automatic library publication                          |
| R12 — First Track Release       | Credible end-to-end explicit Save/Save As, project-media integrity, rolling recovery including unnamed work, bounded safe retention, degraded opening/repair direction and usable failure/ambiguous-completion reporting for shipped workflows                       |
| Later recording                 | Apply the same ownership contract to established durable portions, incomplete material and accepted edits; choose recording-specific reconciliation with storage/device evidence, without promising unpersisted samples                                              |

Concrete container/protocol, cadence/retention, GC, cross-platform guarantees and fault/race evidence remain
Q-058/Q-059; collect/relocate mechanics are not automatically early-stage features. Q-061 still owns
complete dependency/release closure. R1's bounded foundation is complete; later stages still require separate
authorization;
R0 scope is unchanged and acquires no storage/recovery prerequisite.

## Platform release acceptance ownership

[PORTABILITY](PORTABILITY.md#evidence-led-release-policy) owns platform targets and evidence-based
support claims. Windows-first development, early cross-platform hosted checks and actual supported
distribution are distinct. Minimum responsibility follows existing stages:

| Stage                                            | Bounded responsibility                                                                                                                                                                                                                                                                                            |
|--------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| R0 — Audio Architecture Probe                    | Investigate realtime architecture on its declared environment, retain backend-independent contracts, consider cross-platform managed/native feasibility and distribution consequences, and report accurate limits. No full three-platform GUI/device/product certification                                        |
| R1 — Domain Foundation                           | Keep musical/document/serialization contracts free of Windows-only assumptions, including portable identity and paths; exact format/storage evidence remains Q-009/Q-058/Q-059                                                                                                                                    |
| R2 — Audio Resource / Device Foundation          | Establish portable resources and logical device/backend boundaries. Validate implemented adapters honestly; portable interfaces do not require or prove every OS backend                                                                                                                                          |
| R3–R7 — Shell and first-party extension surfaces | Keep shell/resources/module contracts cross-platform; hosted checks exercise actual supported managed/native contracts when present. Identify real UI/extension issues at their platform boundary as capabilities arrive, without full R12 certification at R3                                                    |
| R12 — First Track Release                        | Identify intended distribution environments and require sufficient software/project-integrity, actual desktop, intended audio and delivered-artifact evidence for each declared supported target. Label hosted-only or experimental environments accurately and retain other OS families as architectural targets |
| R14+ — Evidence-led expansion                    | Expand platform delivery, backend/device and CPU architecture coverage as evidence supports it. Adequately evidenced Linux/macOS delivery may also occur earlier; no fixed release dates, order or simultaneous parity commitment                                                                                 |

Q-041 retains the exact OS/version, architecture/RID, device/backend coverage and delivery prerequisites;
Q-061 retains the complete scenario/dependency audit. Other owners retain device, storage, UI and extension
mechanisms. No stage is inserted, renumbered or reordered; R0 remains partially evidenced and R1 is complete, with later
implementation still separately authorized.

## SEQ-R0 — Audio Architecture Probe

Status: **partially evidenced**. The authorized bounded probe has run; see the
[protocol](experiments/SEQ-R0_PROTOCOL.md) and [report](experiments/SEQ-R0_REPORT.md).
Managed/native execution, Windows shared-mode callbacks and controlled/offline checks have evidence;
production selection, clean distribution and broader device/target evidence remain open. R0 did not
authorize R1; R1 was separately authorized and is complete under [PROJECT_STATE](PROJECT_STATE.md).

Before future probe work, follow [CODING_GUIDELINES](CODING_GUIDELINES.md), [DEVELOPMENT](DEVELOPMENT.md),
[PORTABILITY](PORTABILITY.md), and [TEST_EXECUTION](TEST_EXECUTION.md). Policy preparation does not
authorize starting the probe.

Validate the riskiest architecture before application construction. Scope a minimal experimental host,
device initialization, realtime callback, audio clock, basic transport, scheduled sample/tone events,
loop boundaries, bounded command/control path, instrumentation, and stress behavior. Compare a
device-independent offline equivalent where useful.
Overload evidence must respect [AUDIO_ENGINE](AUDIO_ENGINE.md#bounded-overload-and-semantic-recovery):
missed deadlines cannot create unbounded obsolete audio backlog, and disposable updates and musical
events need different handling. Exact queue/scheduler/recovery mechanisms remain probe questions.

Evaluate managed/native feasibility, ownership/lifetime, callback behavior under managed pressure,
and native binary distribution implications. Record setup, measurements, limitations, and a reasoned
accept/reject/narrow recommendation. A backend/language/ABI becomes accepted only through an explicit
evidence-backed decision. See [AUDIO_ENGINE](AUDIO_ENGINE.md) and the [experiment guide](experiments/README.md).
Keep engine processing independent of the selected device adapter and compatible with a prepared
execution boundary. No graph editor, full plugin host, ASIO, or recording workspace is required here.

R2's bounded resource/device foundation is complete / local accepted-ready under
[PROJECT_STATE](PROJECT_STATE.md) and
the [complete acceptance audit](experiments/SEQ-R2-F4_REPORT.md#complete-r2-requirement-audit).
Its completed scope/rationale is retained in the cold archive. The next numbered stage is R3, which
is not started and requires separate authorization; no extra milestone, permanent backend or recording
prerequisite is introduced. Current audio/source/device contracts remain in their canonical owners.

## SEQ-R3 — Workspace Shell Foundation

Establish one main window and internal workspace-pane infrastructure: activation/front behavior,
movement/resizing, internal floating, bounded collapse/restore, user-controlled docking where justified,
and safe application/user layout persistence. Start with limited surfaces; no full visual system or
aggressive IDE docking framework. Follow [WORKSPACE](WORKSPACE.md) and [UX_CONTRACT](UX_CONTRACT.md).
Respect custom unobtrusive main chrome, non-intrusive foreground behavior, and responsive layout with
usable minimums; platform/accessibility mechanics and concrete sizes still require bounded design.
Introduce the minimum host localization/semantic theme and preference rails assigned in
[foundation ownership](#localization-and-theme-foundation-ownership); extension hosting is not a shell prerequisite.

## SEQ-R4 — Node Graph Foundation

Establish core node/connection concepts, semantic port validation, a bounded editable/prepared
execution boundary, and minimal graph interaction/structural nodes over the shell. Basic source/output,
gain and mix/routing may arrive here because execution needs them. Do not implement every candidate
processor or all graph scopes; [NODE_GRAPH](NODE_GRAPH.md) owns the constraints.
Respect item-local/containing-container processing direction, progressive graph visibility, free
spatial placement, and topology-defined dependencies without presuming a final "Layer" model.
Derive revisioned execution from the single canonical graph; converge automatically to the latest valid
revision with safe atomic publication/retirement and coalescing of obsolete preparation. Last-valid
execution may continue in-session while invalid edits remain canonical and savable; show the divergence.
Publication/compiler/cancellation/failure strategy remains open; no manual Apply workflow is required.

## SEQ-R5 — Pattern Workspace

Create the first genuinely musical editing workflow: Channel Rack, Step Sequencer, pattern looping,
several channels, velocity, and responsive live editing over extensible musical events.
Edit the identified shared Pattern content so all its references reflect note edits, with understandable
target/sharing scope; do not introduce invisible musical copies when opened from a placement.

## SEQ-R6 — Extension Foundation

Implement enough identity, manifest/package concepts, local discovery, content-pack boundaries,
sample-generator capability contract, and lifecycle/error/missing-state handling to make first-party
optional modules honest extensions. No store or marketplace. [EXTENSIONS](EXTENSIONS.md) owns the boundary.
Include deliberate compatibility outcomes, diagnostics, and state preservation at the bounded level
needed by actual modules. The core graph/workspace/device infrastructure stays host-owned.
Resolve compatibility before activation under current platform contracts; installed incompatible
plugins may remain unavailable, with preserved identity/state, and must not be automatically deleted.
Compatibility checks answer satisfied required contracts/capabilities/state, not build age alone;
prefer lightweight declared checks and handle real activation failure without mandatory expensive
self-tests of every plugin at every startup. Support graded compatibility/localization fallback and
dependency-scoped blockers with normally degraded document access. Block/defer package uninstall under
known active use; exact package lifecycle and manifest/API negotiation remain open.
Extend existing host resources with bounded contribution identity, availability/fallback and safe
UI-resource retirement under [foundation ownership](#localization-and-theme-foundation-ownership).

## SEQ-R7 — Sample Lab / Generator V1

Ship one default-installed but removable generator package with one or two bounded families. Provide
random and nearby/similar variants, justified parameter locks, candidate history, and a dedicated
workspace pane with standalone and contextual entry. Provide temporary contextual audition through
relevant existing downstream processing alongside solo audition, then explicit acceptance as durable
audio. Resolve bounded substitution/publication/restoration behavior. Follow [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md).
Accepted/generated material joins the same discoverable project-resource audition/reuse path established
in R2; candidate exploration remains separate from acceptance. No hidden Sample Lab resource universe or
automatic global Personal Library publication. R7 does not require cross-project publication.
Acceptance identifies reusable-resource, specific-use or explicitly shared-definition intent under
[sample scope](SAMPLE_WORKFLOW.md#acceptance-scope-and-shared-uses), validates the actual target and
commits coherently. Implement only supported bounded scopes; no full Arrangement/event-conversion UI prerequisite.
Use host localization and semantic styles for the removable generator's first-party UI under
[foundation ownership](#localization-and-theme-foundation-ownership), rather than a separate resource system.

## SEQ-R8 — Resampling

Render a pattern or another bounded selected object to a reusable sample through its own semantic/local
processing boundary. Keep object sampling distinct from broader audible-selection capture under
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md#resampling); resolve the implemented scope's concrete boundary
and tails with evidence. Preserve the source and enable immediate reuse. Any optional replace-with-sample
operation must be undoable and avoid silently reapplying exact baked processing. Full audible-context,
arrangement, and multiple-source capture need not arrive together. Render the frozen canonical scope,
block required invalid/unavailable dependencies, handle preparation finitely and revalidate the target
before asynchronous acceptance; exact taps/transactions remain open.
Use the selected content/occurrence/container's semantic ownership rather than infer it from a label
or editor entry point; rendering one use cannot silently transform every shared reference.

## SEQ-R9 — Piano Roll

Add richer pitch, duration, and velocity editing over the same underlying musical event model as
the Step Sequencer.
The richer editor retains the same shared-content target semantics; opening from a placement does not
detach it or create another event model.

## SEQ-R10 — Arrangement

Add Playlist/Arrangement with clips in musical time, normal shared pattern references, and explicit
independent variations. Follow the semi-free direction: user-named structured containers, useful
default content relationships, and compatible material reuse without permanent one-instrument
ownership or mixer-channel identity. Resolve bounded compatibility/container-processing relationships
and audio-clip scope rather than assuming a final Track schema or full DAW timeline.
Provide bounded placement-local versus shared-content editing, explicit musical variation and
understandable organization/context moves and dependency-aware deletion. Apply the two local processing
levels to any supported hierarchy; do not add an arbitrary processing tree or universal overrides.
Respect project-tempo relationships and independent local stretch; ordinary audio trim/split/rearrange
preserves source resources and keeps trim, loop/repeat, and stretch distinct. Exact representation,
algorithms, gestures, and the implemented scope need bounded design rather than a complete stretch engine here.

## SEQ-R11 — Mixer / Core Processing

Add the user-facing Mixer workflow with channels, master, gain, pan, mute, solo, and bounded common
processing such as basic EQ/compression. Foundational engine mixing/routing and structural graph
nodes may already exist. Mixer channels remain distinct from arrangement tracks; deeper sends/routing
should follow demonstrated need. A usable base must not require optional processing downloads.

## SEQ-R12 — First Track Release

Reach a version in which a user can reasonably finish a small track: strengthened save/load,
arrangement, basic mixing/effects, WAV export, packaging, and recovery/error handling suitable for
real projects. This is a usability/integrity goal, not a feature-count target or assigned version number.
Save/reopen preserves canonical work even when invalid. Export validates/prepares a frozen canonical
revision, blocks required dependencies and never silently renders stale playback; preparation requires
responsive cancellation/state and finite failure handling. Export follows intended project cuts/tails
under [AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction).
Exercise the [bounded First Track scenario](#first-track-scenario-and-acceptance-boundary), including
meaningful shared/independent music and sound references, reused audio with local edits, context moves
and coherent Undo/reopen under the accepted ownership boundaries. Q-061 retains implementation-specific
scope/dependency and end-to-end evidence closure.
Selected export ranges are hard by default; explicit tail inclusion may extend naturally allowed tails
without undoing project cuts. Finite tail mechanics (Q-057), exact rolling-recovery workflow (Q-058),
managed-media integrity mechanics (Q-059) and concrete milestone closure (Q-061) remain open.
Require a usable find/audition/use workflow for current project samples and ordinary external/installed
pack material needed to finish a small track. Preview remains transient; accepted audio remains durable
after source/pack removal. Personal cross-project sample/preset publication is assigned to R14+;
advanced search/organization and a mature catalog are not R12 prerequisites.
Require coherent bounded language/theme and missing-presentation behavior on shipped surfaces under
[foundation ownership](#localization-and-theme-foundation-ownership), without altering project state.
Declare the environments intended for distribution and accept each supported target under
[platform ownership](#platform-release-acceptance-ownership): actual desktop/workspace interaction,
intended audio output, applicable native loading, clean delivered launch and project/media Save/reopen/
recovery evidence for shipped workflows. Capture/monitoring acceptance applies when shipped, not as an
implicit recording prerequisite. State limitations and hosted-only/experimental status elsewhere;
shared source or CI does not imply Windows/Linux/macOS release parity. The exact supported matrix and
delivery prerequisites remain Q-041; portable architecture remains required for other target OS families.
This is a bounded later acceptance direction, not permission to start implementation, weaken foundations
or claim a finalized/technically validated release specification.

## SEQ-R13 — Extension Ecosystem

Add mature install/update/remove management and extension diagnostics after real modules establish
the lifecycle and compatibility requirements.

## SEQ-R14+ — Evidence-led expansion

Own explicit cross-project Personal Library retention/publication of selected samples and user-created
presets under [Sample workflow](SAMPLE_WORKFLOW.md#personal-library-and-explicit-publication) and
[Extensions](EXTENSIONS.md#preset-sources-and-project-state). Concrete delivery scope, UI, formats,
organization and backup/import/export remain open; no catalog/store/cloud feature is committed.

Expand MIDI and audio recording/monitoring into complete user workflows, then consider automation,
richer synthesizers/effects, CLAP/VST3 hosting, additional specialized nodes, pitch/time
processing and time stretching, deeper routing/sends, FLAC and other justified formats, and additional
platform release coverage. Portable architecture and early hosted checks are already required direction,
not deferred platform ownership; adequately evidenced targets may ship earlier under
[release acceptance ownership](#platform-release-acceptance-ownership). Latency awareness and realtime
safety constrain applicable earlier work; advanced compensation is not presumed implemented. Later
order and release schedules remain open.
Future Live / Low-Latency mode for performance/monitoring must visibly distinguish temporary live
handling from the full graph without rewriting project state or altering intended final/offline render.
Latency reporting, thresholds, compensation, and bypass strategy remain open under [AUDIO_ENGINE](AUDIO_ENGINE.md).
External hosting must define ordinary compatibility/lifecycle semantics under [EXTENSIONS](EXTENSIONS.md).
Stronger process isolation may be evaluated later if evidence justifies it; it is not required baseline
hosting architecture or an early milestone.
Automation/modulation work must resolve the open effective-parameter model: base value and control
sources, composition, domains/units, precedence, smoothing, and rates. Earlier parameter modelling
must leave room for that control without selecting its formula now.
ASIO is desired future device capability, subject to concrete implementation and licensing/distribution
evaluation. Core recording/device responsibilities may be established before polished recording UX;
this placement does not turn them into removable plugins or impose an electronic-only product boundary.
