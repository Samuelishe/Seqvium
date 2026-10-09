# Architecture

Role: Logical responsibility and dependency boundary guide.
Read when: Structuring code, reviewing coupling, or evaluating architecture proposals.
Authoritative for: Foundation-first logical domain/application/adapter/presentation boundaries, project lifecycle,
timeline/organization/graph separation, musical/resource identities, semantic execution domains, signal ownership and
processing scopes, Arrangement/Mixer relationships, canonical/derived state, semantic-action/input boundary, logical
undo transactions and async commit integrity, host UI services.
Not authoritative for: Exact project decomposition, audio internals, extension API, file format, or progress.

SEQ-R1 selects a bounded managed canonical foundation below. R2-F1 adds managed WAV resources and
offline PCM execution. R2-F2 reuses that execution behind a bounded realtime boundary and a narrow Windows
output adapter; graph, presentation and permanent engine choices remain separate.

## R3-F1 desktop host and presentation ownership

`src/Seqvium.Desktop` is the one framework-dependent `net10.0` desktop executable. It explicitly adopts
Avalonia Desktop with Fluent base controls; its manifest/lock own versions. Core remains BCL-only and
does not reference presentation. Desktop references Core only, not `Seqvium.Audio.Windows`, DeviceCheck
or the R0 experiment. Launch creates no audio/device/capture session and calls no playback service.

`App` owns framework startup and semantic resources; `MainWindow` owns bounded visual/chrome events.
The display-independent `Presentation/ShellSession` owns one `ProjectDocument.Create`/Close lifetime,
localized projection and preference commands; it does not edit the document. `HostLocalizer` owns frozen
first-party stable dotted-key EN/RU catalogs and per-entry fallback. `HostPalette` owns Dark/Light roles;
`PreferenceStore` owns the independent versioned user JSON. No culture change, canonical discriminator,
document generation/history or media authority depends on translated strings or UI preference changes.
Language/theme apply immediately at completed button actions without replacing controls/window/document.
Numeric display is explicitly invariant; language is not a project parser or automatic name translator.

Windows chrome retains native window styles, resize/move roles and a small guarded OS system-menu
adapter; other platforms retain native decorations until tested. `WorkspaceRegion` hosts R3-F2's compact
`WorkspaceState` (pure placement/visibility/order), `WorkspaceHost`/`WorkspacePane` (retained controls,
pointer capture and local focus), and `FirstPartyPaneContent` (read-only document projection or existing
preference actions). `WorkspaceLayoutStore` owns a separate user file and `WorkspaceLayoutPersistence`
joins/coalesces requested snapshots; `MainWindow` coordinates their shutdown with preference writes.
Pane instance/type, active pane, focused control, canonical selection and semantic edit target are
distinct. No document selection/target, native pane window, extension hierarchy or audio owner is added.
[Workspace](WORKSPACE.md), [settings](SETTINGS.md) and [UI design](UI_DESIGN.md) own exact behavior/resources;
[F1 report](experiments/SEQ-R3-F1_REPORT.md) owns observed bounds.

## R1 canonical foundation

The production foundation uses one `Seqvium.Core` library and one `Seqvium.Tests` project. Domain
values/validation, document operations/history and JSON/filesystem persistence are logical boundaries
within that library; no assembly-per-layer rule or service framework is needed.

Each document has a persistent project UUID and a fresh nonpersistent lifecycle UUID on creation/open.
An unnamed pristine document has no explicit saved snapshot/path and is unmodified; its first accepted
edit becomes dirty. A successful file Save establishes the separately identified saved snapshot.
An immutable canonical state owns named Patterns, nested identified parts/notes, sound definitions,
Pattern placements, flat Instrument Groups, resource descriptors and optional processing/route intent.
References use typed `Id<T>` UUIDs, never names or equal payloads. Pattern parts address sound definitions;
placements address Patterns. A variation copies parts/notes with fresh identities and retargets one
placement atomically while preserving sound/resource/processing references. Sound independence instead
copies durable sound configuration and retargets an explicitly selected part (shared Pattern scope).
Occurrence-only sound independence first requires a variation; no universal unlink operation is implied.

Processing contexts have distinct item-local or containing-local identities, with optional opaque
configuration; an item context has exactly one placement owner. A placement may name one containing
context and a downstream route. Per-part placement relationships can retain distinct contributions
and explicit performance-interaction UUIDs independently of sound definitions. Route identities are
intent only, not Mixer channels, executable graphs or automatically instantiated DSP. Organization
never assigns processing. An explicit aggregate cannot also claim divergent post-mix part routes;
validation rejects that contradiction. R4-F1 adds the bounded canonical graph boundary below;
expanded scope, preparation/publication, execution-domain derivation, capability negotiation and
Arrangement/Mixer assignment remain Q-019/Q-030/Q-047.

Musical positions/durations use nonnegative/positive Int64 ticks, **960,000 ticks per quarter note**.
This exactly covers conventional binary subdivisions, triplets and quintuplets with fine edit precision;
it is a bounded grid, not a promise of every rational subdivision. Positions span over nine trillion
quarters. Checked arithmetic rejects overflow. Tempo is decimal BPM in [1, 1000], at most six fractional
digits. Constant-tempo conversions use exact integer rational arithmetic and round to nearest, ties
toward the later frame/tick. Always convert absolute boundaries; an executed duration is rounded end
minus rounded start. Adding individually rounded durations can drift and is not scheduling semantics.
A positive musical duration can map to zero frames below frame resolution; it remains positive in
canonical state. F1 reports and omits zero-frame executions under
the [audio owner](AUDIO_ENGINE.md#r2-f1-offline-sampler-foundation), without changing the note.
Meters support numerator 1–64 and power-of-two denominator 1–64. Quarter-note tempo does not change
with meter. Future tempo maps and fixed-time audio mappings require their own bounded representation;
neither sample frames nor a concrete sample rate are persistent musical time.

One accepted operation builds an isolated immutable candidate, validates the complete state, then
publishes one revision and one history entry. Failed/no-op operations publish nothing. Undo/Redo retain
whole coherent state/revision pairs; no unrelated edit coalescing, UI commands or audio history exist.
The initial configurable history bound defaults to 256 entries; history is not persisted. A saved
snapshot remains separately identified, so Undo to the saved revision clears dirty state. Transition
generation advances even on Undo/Redo, and close ends lifecycle authority. These stamps offer a
conservative F1 import gate; dependency-specific rebasing and general async adapters remain future work.
Current/saved/history snapshots explicitly retain descriptors, without implementing physical media GC.
Mutation is serialized by the caller on one owning thread; concurrent mutation is outside R1 scope.

R0 demonstrates bounded managed/native execution feasibility only. Its native structs, four-slot
publication, frame clock and observed 48 kHz / 10 ms endpoint are not production domain contracts.
The reviewed pre-R2 disposition permits an initial bounded C# scheduler/DSP direction with a replaceable
execution/device boundary. F1 implements backend-independent resources/offline execution; F2 adds the
bounded realtime and platform ownership below.
Q-001–Q-007 remain open; intended-workload realtime, period/device, clock-recovery and distribution
evidence must inform subsequent choices. No permanent engine/native ABI follows from F1.

## R4-F1 canonical graph responsibility

Core extends the existing immutable `ProjectState` with graph definitions and separate context
attachments. Graph topology does not own music: sources address one placement/part pre-item boundary;
Pattern, sound, resource and processing context identities retain their R1 roles. Initial supported
attachments use independent definitions, one per item-local context. Output is the local processing
result, not a new routing owner or device destination. Containing/global/shared attachment execution
and cross-context dependencies remain later capabilities.

Safe structural validation belongs to canonical acceptance/persistence; graph intent diagnostics
separately establish eligibility for later preparation. Neither descriptors nor a clean report create
DSP/executable state. Source/Gain/Mix/Output and port/cardinality/coordinate rules are owned by
[Node graph](NODE_GRAPH.md#implemented-r4-f1-canonical-graph-intent); reader 1.1 and unknown preservation
by [Project format](PROJECT_FORMAT.md#r4-f1-graph-aware-json-format). No public plugin ABI is introduced.

All graph operations use `ProjectDocument.Edit` and existing bounded history/lifecycle ownership.
Variation remapping, owned placement/context deletion and incident node-use removal are atomic;
unrelated safely unresolved dependencies retain intended identity. Graph deep content equality
includes immutable collections and opaque JSON, so net-zero reconstruction publishes no revision.
Coordinates are canonical graph presentation; workspace geometry and transient gestures remain
separate. F2 must derive execution/resources from a frozen eligible revision; F3 will supply the canvas.

## R2-F1 managed media and offline execution

The existing library contains [WAV decoding](../src/Seqvium.Core/Media/WavDecoder.cs),
[media preparation/storage](../src/Seqvium.Core/Media/ProjectMedia.cs) and
[offline sampler preparation/execution](../src/Seqvium.Core/Audio/OfflineSampler.cs). No new assembly or codec
framework is introduced. Durable source bytes belong to the project; decoded immutable PCM is a
disposable cache with independent leases for prepared plans and live execution. Device facts and
execution frames remain derived and absent from the canonical musical model.

`ProjectMedia.BeginImport` captures project/lifecycle/generation and an explicit optional sound target.
`WavImport.PrepareAsync` owns an unattached candidate, reads bounded source bytes, validates WAV on a
worker and establishes flushed managed bytes. Completion grants no edit authority. The owner awaits
completion, then `Accept` rechecks captured and current cancellation, open lifecycle, generation, project/target
identity
and stored integrity before one `ProjectDocument.Edit` adds the descriptor and creates/configures the
sampler. Any intervening edit, Undo or Redo rejects the request even if content later looks identical.
Save alone does not invalidate unchanged inputs; acceptance after Save creates newer dirty work.
Cancellation observed at the acceptance gate permanently withdraws that request; cancellation after
an established edit does not undo it. No selection lookup or implicit rebase occurs. Dispose after awaited completion
ends an unaccepted
candidate; accepted bytes are retained. Prepare/Accept/Dispose of one request may not overlap, and
document mutations remain caller-serialized. There is no general concurrency/worker framework.

The [format owner](PROJECT_FORMAT.md#r2-f1-managed-wav-layout) defines storage/Save integrity;
[audio](AUDIO_ENGINE.md#r2-f1-offline-sampler-foundation) defines bounded pitches, events and voices.
Frozen single-Pattern execution rejects unsupported required processing/route/performance dependencies.
It is neither Arrangement rendering nor a graph engine, and leaves execution replaceable for F2 evidence.

## R2-F2 execution and platform ownership

Portable Core now adds [prepared realtime control](../src/Seqvium.Core/Audio/RealtimeSampler.cs) over the same
F1 execution. Control captures an immutable revision and media roots, prepares off-thread and checks
lifecycle/target/revision/generation/cancellation before publication. The callback owns execution only;
it cannot read the source document. One candidate and active/pending/retired states provide explicit
capacity and control-side retirement. A replacement resets the transport epoch, without live voice transfer.
[Audio](AUDIO_ENGINE.md#r2-f2-realtime-wav-and-windows-output) owns exact controls, budgets and failure policy.

`Seqvium.Audio.Windows` owns actual WASAPI COM/output/clock/wake/teardown in a justified platform assembly,
referencing Core in one direction. OS vtables are private adapter plumbing, not a musical or engine ABI.
`Seqvium.DeviceCheck` is an explicit physical-device verification executable, separate from deterministic
tests and from any future workstation. No additional framework or backend interface is required for
this concrete ownership. Native execution, arbitrary graph publication and platform release remain open.

Output unavailability/faults end execution safely without editing the document or managed source storage.
Restart uses a fresh stream/consumer epoch; no universal QPC/clock synchronization policy is adopted.
Input/MIDI/capture remain future distinct adapter responsibilities: input buffers/timestamps would belong
to their device lifetime, while deliberate recording acceptance and durable musical placement belong to
the project owner. F2 implements none of those workflows.

## R2-F3 source access and audition ownership

[SourceAccess](../src/Seqvium.Core/Media/SourceAccess.cs) separates explicit external-directory
observations from accepted ResourceIds. Discovery does not mutate/open a project; project discovery
captures an already open revision, then validates frozen media on a worker. Observed availability is
not acceptance authority, and equal paths/names/hashes are not semantic identities.

[WavAudition](../src/Seqvium.Core/Audio/WavAudition.cs) owns ephemeral raw/solo state over F1/F2, not
a fake canonical project. A narrow owner-thread Close event ends bound preview; output never reads
the document. Cancellation, handoff and retirement use bounded prepared-state ownership. The caller
joins the borrowing worker before final
disposal. [Sample workflow](SAMPLE_WORKFLOW.md#r2-f3-bounded-source-access-raw-preview-and-reuse)
owns workflow; [Audio](AUDIO_ENGINE.md#r2-f3-transient-one-shot-execution) owns execution/lifetime.

[WavResourceReuse](../src/Seqvium.Core/Media/WavResourceReuse.cs) validates captured/current integrity
and authority, then creates an independent sound over one existing ResourceId through Edit. No byte
copy or implicit shared-definition/part retarget. Canonical schema/time, history and Save owners stay
unchanged; no assembly, dependency, GUI, catalog or backend SDK is added. Separate logical input/output
selection and MIDI/capture preparation are scoped in Audio; output does not prove operational input.

## R2-F4 endpoint and session ownership

Portable [AudioEndpoints](../src/Seqvium.Core/Audio/AudioEndpoints.cs) owns environment observations,
independent input/output intent/resolution and one serialized `AudioDeviceSession`. It has no project
reference or preference persistence. `IAudioOutputLifetime` is the sole minimal join/release contract
for concrete borrowed PCM ownership; there is no device-management framework or backend hierarchy.
`Seqvium.Audio.Windows` implements OS enumeration, role queries, property/COM ownership and selected
WASAPI open with worker-side state/direction validation. Only the adapter interprets native error codes.
Core never references Windows; callbacks never discover devices or read the control session.

Changing output stops/joins the existing borrower before releasing its processor and before explicit
replacement initialization. Input selection changes only input intent and opens no capture stream.
Current facts, active endpoint and future availability remain separate from intended selection and
portable canonical music. [Audio](AUDIO_ENGINE.md#r2-f4-logical-endpoints-and-independent-selection)
owns lifecycle/failure behavior; [Settings](SETTINGS.md#audio-device-selection) owns configuration scope.

## Accepted constraints

Use simple architecture for the current product with extension points justified by known requirements.
An abstraction needs a concrete ownership or testability reason. Avoid enterprise layering,
speculative interfaces, and a disposable model that must be replaced for Piano Roll or Arrangement.

Seqvium prioritizes a durable, extensible architecture foundation over the fastest visible DAW demo.
Substantial foundation work before impressive UI or a musically complete prototype is acceptable.
Establish boundaries whose later replacement would be broadly expensive, without speculative enterprise
architecture. Dependency correctness guides sequencing; not every subsystem must be complete before UI.

## Domain, application, infrastructure and presentation

Keep these concerns logically distinct, without mandating Clean Architecture boilerplate or a
project/assembly per layer:

| Concern                   | Responsibility                                                     |
|---------------------------|--------------------------------------------------------------------|
| Domain                    | Musical, resource and project concepts/invariants                  |
| Application / use cases   | Operations coordinating document edits and workflows               |
| Infrastructure / adapters | Persistence, filesystem, audio devices/backends and plugin loading |
| Presentation              | Avalonia desktop shell and future workspace interaction/UI         |

Dependency direction keeps core musical/domain semantics independent from concrete presentation and
low-level infrastructure. Application operations coordinate domain work; adapters implement host/domain/
application requirements, and presentation invokes operations and presents state. This describes logical
responsibilities, not a final source tree or project count. Keep concrete decomposition as simple as justified.

Pattern/domain objects must not depend on Avalonia controls; project semantics must not depend on
WASAPI/miniaudio or another backend; tempo/musical time must not depend on a UI window. Persisted plugin
state must not depend on a button/control instance. UI is not canonical project truth, and audio/backend
adapters implement requirements rather than define musical semantics.

## Semantic actions and input boundary

A semantic action expresses application intent and target/scope; an input gesture/binding expresses
how the user requests it. Presentation translates pointer gestures, keyboard bindings, menus/context
menus and future accessibility/action surfaces into application operations:

```text
pointer gesture   keyboard binding   menu/context/accessibility action
        \                |                /
                 semantic action
                        |
          application/domain operation
```

`MoveSelection(...)`, `DeleteSelection`, `Duplicate`, `Undo`, `Redo`, `OpenProcessing`, `AcceptCandidate`
and `CancelOperation` illustrate intentions only, not selected names, classes or APIs. Not every action
mutates the document. A canonical edit uses the same validation and
[logical Undo boundary](#logical-undo-transactions-and-history-scope) regardless of input surface;
transient preview remains distinct from canonical commit. There is one canonical project truth and
no keyboard-specific musical/edit model. Domain objects and edit intent must not encode physical
keys, modifier events, Avalonia controls or an input framework.

[UX](UX_CONTRACT.md#semantic-actions-and-input-composition) owns input composition, discoverability,
binding flexibility and focus/command-target safety, including selection distinct from UI focus.
No global enum, command bus, service locator, command-pattern hierarchy, concrete C# command class,
Avalonia API or binding framework is selected or required by this boundary.

## Foundational ownership and project lifecycle

Creating a new project and the first-class project/document lifecycle are fundamental, not optional late
features. The project owns musical state, project settings, managed media, plugin instances/state,
processing relationships and later arrangement/automation/recording data. New/Open/Save UI and container
complete container remains open under [PROJECT_FORMAT](PROJECT_FORMAT.md); F1 selects a bounded JSON/media directory
layout.

Early architecture deliberately establishes the rails later work depends on: document and musical-domain
ownership, versioned serialization, settings/configuration separation, diagnostics/logging, extension
hosting, resource ownership, transport/musical clock, audio/backend/device abstraction, realtime execution,
asynchronous operation integrity and undo/edit transactions. Host localization/theme services enter at
the appropriate stage. This does not require all subsystems, extra layers or interfaces before any UI;
[ROADMAP](ROADMAP.md) owns sequencing and the later complete-project acceptance exercise.

## Core platform responsibilities

Keep these logical responsibilities distinct even if some later share an assembly. This is long-term
ownership, not a requirement to implement every capability in the first stages:

| Responsibility                       | Boundary                                                                                                    |
|--------------------------------------|-------------------------------------------------------------------------------------------------------------|
| Application shell / workspace panes  | Owns main-window composition and user workspace state; [WORKSPACE](WORKSPACE.md) owns behavior              |
| Project / document model             | Owns musical relationships, edits, resource references, and document integrity                              |
| Transport / musical clock            | Host-owned musical execution context; cannot be replaced by a plugin                                        |
| Audio engine / execution             | Owns scheduling and execution-time state; isolated from arbitrary mutable UI/project objects                |
| Node graph engine                    | Core signal-graph ownership; [NODE_GRAPH](NODE_GRAPH.md) owns its contract                                  |
| Audio device abstraction             | Host-owned input/output boundary below engine processing contracts                                          |
| MIDI device / input foundation       | Host owns device selection and musical input integration                                                    |
| Audio / MIDI recording foundations   | Host owns capture and timeline/resource integration; polished recording UX may arrive later                 |
| Mixer / bus foundations              | Core mixing/routing primitives may precede the full Mixer workspace                                         |
| Audio resource ownership             | Controls availability and lifetime of project audio independently of optional UI                            |
| Undo / redo                          | Owns coherent document edits; optional surfaces cannot replace edit integrity                               |
| Serialization                        | Preserves versioned document and extension data under the format contract                                   |
| Extension hosting / lifecycle        | Hosts attached capabilities; fundamental platform ownership stays in the base                               |
| User configuration / diagnostics     | Host owns application preferences and bounded production diagnostics; [SETTINGS](SETTINGS.md) owns policy   |
| Localization / semantic UI resources | Host owns shared localization and theme/style contracts for first-party UI and Seqvium-native contributions |

Basic musical editing, routing, level control, and common processing must be usable without optional
downloads. Specialized generators/nodes/instruments/effects/content may extend the platform;
[EXTENSIONS](EXTENSIONS.md) owns categories, compatibility, and lifecycle. These core responsibilities
do not require separate projects, interfaces, or a public plugin API now.

Application state may control audio through a prepared bounded boundary; the engine must not depend
on UI-thread progress. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns the realtime contract. Production runtime
must not depend on experiment hosts or repository retrieval tools.

[PORTABILITY](PORTABILITY.md) owns the Windows/Linux/macOS product target and portable engineering
rules. Windows is primary early development; Linux/macOS are architectural targets from the start,
without release-date or validated runtime-parity claims.

## Internal modules, backends, and plugins

- **Modular internal architecture:** core subsystems have clear contracts and justified replaceable/testable boundaries.
- **Replaceable backend:** an internal adapter may implement a core boundary, such as audio-device I/O.
  Replacing it is not automatically a user plugin API.
- **User extension/plugin:** an optional guest contributes capabilities through Seqvium host contracts;
  it does not replace project integrity, transport, graph engine, or audio-device ownership.

**Seqvium Platform defines the current host rules and contracts. Plugins are guests of that platform.**
[EXTENSIONS](EXTENSIONS.md#host-context-and-compatibility) owns required-contract declarations,
compatibility before activation, and retention of installed incompatible plugins. Preserving project
data does not promise exact historical sound; [AUDIO_ENGINE](AUDIO_ENGINE.md#sound-compatibility-boundary)
owns the platform boundary and extensions own plugin-algorithm responsibility.

Ordinary instrument/effect/generator plugins consume device-independent host services, not miniaudio,
WASAPI, ASIO, ALSA, PipeWire, or CoreAudio directly. Backend replacement must not require rewriting
ordinary processing plugins. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns the processing environment and
backend boundary; [EXTENSIONS](EXTENSIONS.md) owns capability compatibility and failure handling.

## Timeline, organization, and signal graph

| Concept           | Describes                                                                                    | Canonical boundary                                  |
|-------------------|----------------------------------------------------------------------------------------------|-----------------------------------------------------|
| Musical timeline  | Patterns, notes/events, pattern/audio clips, later automation and recordings in musical time | Musical model below                                 |
| User organization | Instrument/channel groups, names, and workspace organization                                 | Group identity below; pane composition in WORKSPACE |
| Signal graph      | Sources, processors, mixing/splitting, effects, buses/output where applicable                | NODE_GRAPH                                          |

Do not collapse these into one universal graph. Arrangement places music in time; the signal graph
describes audio/control flow and does not replace Arrangement. Organization may visually correspond
to either without becoming its storage identity or defining routing by itself.

## Proposed application and audio shape

C# / .NET 10 is adopted for the canonical foundation; Avalonia is adopted for the bounded R3-F1 host.
[DEVELOPMENT](DEVELOPMENT.md) owns the developer environment, SDK/tool version authority, and setup;
[CODING_GUIDELINES](CODING_GUIDELINES.md) owns future implementation conventions.

The accepted logical boundary direction is conceptual, not an implementation selection:

```text
Project / musical state
    -> prepared execution state
    -> Audio engine (scheduler, node execution, mixer/buses)
    -> Audio device abstraction
        -> possible WASAPI / ASIO / future platform backend

Shared scheduling / node semantics
    -> Offline renderer (no audio device required)
```

[SEQ-R0](ROADMAP.md#seq-r0--audio-architecture-probe) must test execution feasibility before major
production implementation. A C# application with a narrow native boundary and native realtime engine,
possibly C++ using miniaudio, remains a hypothesis. C++, miniaudio, WASAPI, and ASIO are not accepted
implementations through this diagram. ASIO is a desired future capability, not an R0 requirement.
ABI, control publication and backend strategy remain open. R1's managed decomposition does not select
an engine or backend.

The editable project graph is the single canonical project truth, including definitions, names,
parameters, layout and connections. Execution is a derived prepared revision/snapshot identified by
the canonical revision it represents; it is never independently edited or persisted as another project
model. The host prepares a bounded realtime-suitable representation; validation/compilation strategy is open.
The callback must not traverse mutable graph-editor or arbitrary UI state. [NODE_GRAPH](NODE_GRAPH.md)
owns that boundary's graph semantics; [AUDIO_ENGINE](AUDIO_ENGINE.md) owns execution constraints.

## Document integrity and asynchronous publication

The following is an accepted semantic contract for future implementation. Completion of asynchronous
work does not itself authorize a canonical project edit. A result for Kick #42 must never attach to
whichever object is selected later. Current selection, focus, name, visual position and completion order
are not commit authority. [Async commit gate](#async-commit-gate) and
[history and pending-work relevance](#history-and-pending-work-relevance) below own the common rules;
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md#contextual-generation-result-validity) specializes candidate handling.
[PROJECT_FORMAT](PROJECT_FORMAT.md#save-and-reopen) owns canonical Save/reopen;
[AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction) owns frozen canonical render preparation.

### Edit, preparation and resource boundaries

| Concept                       | Semantic responsibility                                                                                                                 |
|-------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| User intent / logical edit    | The requested musical/document outcome and its target/scope; requesting long work does not itself mutate the document                   |
| Canonical document mutation   | A change to the single editable project truth, validated against the state in which it is accepted                                      |
| Undo transaction              | One coherent accepted document intention, potentially changing several canonical relationships together                                 |
| Transient interaction preview | Explicit temporary feedback/audition with a cancel/restore path; not accepted document state or history                                 |
| Async computation/preparation | Produces a candidate/prepared result from identified inputs; completion alone is not an edit                                            |
| Async result commit           | Revalidates the operation and accepts its canonical effect through the normal transaction boundary                                      |
| Derived realtime publication  | Makes a valid prepared canonical revision executable under the graph/audio contract; not a second editable history                      |
| Durable resource creation     | Establishes storage/ownership of produced material; a file's existence alone neither inserts it into the project nor authorizes an edit |

```text
user requests operation -> capture intent and sufficient preconditions
    -> async computation/preparation -> result available
    -> required durable resource placement succeeds
    -> revalidate project/target/context/operation at commit
    -> commit one coherent canonical undo transaction
    -> derived execution catches up separately
```

Durable placement is required only when the workflow creates/accepts managed material. It is not a
document edit by itself, and later non-commit needs explicit resource ownership/cleanup. External
export may create a useful artifact without any project mutation or project Undo entry; its delivery
authority/lifetime follows its own requested workflow. No database/ACID guarantee is implied.

### Logical undo transactions and history scope

Undo/Redo operates on coherent canonical document intentions, not individual property setters,
pointer events, task completions or currently executing audio. An accepted transaction presents its
related canonical changes together from the user's perspective, with one Undo restoring the preceding
relationship/state and Redo reapplying the accepted edit. Examples include moving a clip and its required
placement relationship, accepting generated audio and its project resource/reference, making a Pattern
variation, and explicitly replacing an object with rendered audio. No internal command count, class,
stack or framework follows from this semantic contract; R1's bounded realization is defined above.

Canonical musical, graph, project-owned plugin/sound configuration and applicable project editor-state
edits belong to document history. Application/user workspace layout, global preferences and external
package installation normally do not; any associated project-owned canonical edit has its own document
transaction. Live voices/tails, prepared execution and transient auditions/previews are outside document
Undo. [UX](UX_CONTRACT.md#undo-grouping-and-interaction-preview) owns gesture/action grouping;
[PROJECT_FORMAT](PROJECT_FORMAT.md#save-and-reopen) owns persistence boundaries. Undo history limits and
persistence across Save/reopen/restart are open, independently of rolling recovery.

A note edit in a Pattern referenced by three placements changes the one shared Pattern content. Undo
restores that content once and all remaining references observe it; it must not revert three invented
placement copies. Making a Pattern variation instead creates independent musical content and changes
the intended reference relationship in one transaction, without implicitly detaching sound definitions.
Placement-local processing edits target that placement's state. Making/editing an independent sound
definition targets its definition and intended references, not Pattern content or live execution state.
[Separate sharing identities](#separate-sharing-identities) owns those boundaries; Q-029 retains concrete
reference/detachment, acceptance-scope and sharing-feedback design.

### Async commit gate

Pending work conceptually carries original document identity and lifecycle, operation intent and
authorization, target identities/scope, expected ownership relationships, relevant context, sufficient
source/settings/dependency preconditions, and relevance/cancellation state. Its relationship to the
history state that made it meaningful must be known where needed. These are semantic requirements,
not a universal token schema or cancellation API. F1's conservative import realization is defined above.

Before canonical commit, establish all applicable conditions together at the mutation boundary:

- The original document is open and still able to accept the intended mutation.
- Required targets exist with the captured logical identities and expected ownership relationships.
- The operation remains authorized and relevant, and has not been cancelled or invalidated.
- Relevant context and required source/settings/dependency preconditions still hold.
- Required durable material is successfully available under the intended ownership.

A prior successful check followed by a conflicting change is insufficient; the accepted mutation must
still satisfy the gate. Long preparation need not lock the whole document. The exact coordination
mechanism remains open. Automatic commit is allowed only for an already authorized workflow whose
full gate still holds. Sample Lab candidate generation authorizes exploration, not acceptance.
Explicit acceptance is a new commit decision and must validate its actual destination/preconditions;
it does not waive document lifetime, ownership or storage safety.

Object identity alone cannot validate work begun at R10 when relevant inputs/settings are R14. A stale
result must not silently overwrite newer intent. Conversely, unrelated document edits need not invalidate
an operation with a known smaller sufficient dependency set. Validate that set and all relevant target/
context/lifecycle relationships; neither exact whole-document revision equality nor a match of one
object alone is universally required. Unknown dependencies require conservative validation/recomputation,
not an assumption that identity is enough.

A particular operation may support revalidation or rebasing if it can establish a correct outcome
against current dependencies without losing intervening edits or changing the authorized intention.
Rebasing is not a universal capability. If changed inputs are essential to the requested current result,
recompute; a frozen old render may remain useful only as clearly identified old material. Where meaningful,
explicit acceptance/reuse may apply a stale candidate to a newly validated destination as a separate
user action, without pretending it satisfies the original operation. Otherwise discard/lifecycle-clean
or retain it unattached under the workflow's ownership. No universal stale-result retention policy is set.

### History and pending-work relevance

Undo/Redo changes canonical state; a history position is evidence of context, not sufficient commit
authority. Work whose enabling edit/intention was undone loses authority to silently restore that edit
or its consequences. Cancel/invalidate it or retain only safely owned unattached material where useful.
An unrelated Undo need not invalidate work whose sufficient preconditions and relevance still hold.
Redo can recreate a similar state, but does not automatically renew cancelled/invalidated authorization
or validate an old pending result. Re-evaluate actual dependencies, relationships and operation relevance;
explicit reuse or a new request is required where original authority was lost.

Deleting a required target makes targeted commit impossible. The workflow may cancel/invalidate its
request or explicitly keep it suspended as pending work/candidate material. Undoing deletion may restore
the same logical identity, unlike creating another object with the same name/position. That restoration
can permit revalidation only if relevant source/settings/context/ownership also match and the original
request remains authorized under that workflow. Identity restoration never revives an already cancelled
or invalidated operation; useful artifacts may instead be explicitly accepted anew. Restoring identity
does not guarantee restoration of every dependency. No identity/schema or suspension mechanism is chosen.

Switching active projects is distinct from closing one: an inactive open document may still accept its
authorized operation if its gate holds, but a result never moves to the newly active document. Workflows
requiring active audition/context may suspend or invalidate that part. Closing a document ends canonical
commit permission; late completion must not reopen/resurrect it, even if a project with the same path
is later opened. Reopening requires fresh lifecycle validation/authority. Cancellation may be requested
without instant termination; safe result/resource retirement remains required. Separately useful exports
or explicitly retained candidates may finish/survive only with an owner and justified delivery/reuse
workflow independent of mutation of the closed document. Application close must not wait indefinitely
for arbitrary work; shutdown/cancellation/retention mechanisms remain open.

### Failure, cancellation and non-commit

User cancellation before commit withdraws the pending operation; a late result cannot undo cancellation.
Stale inputs, invalid target/ownership, and project close fail different gate conditions and can require
different reuse/cleanup paths. Preparation failure creates no edit; storage failure prevents claiming
durable acceptance; canonical commit failure must leave no partial accepted transaction or successful
history entry. Each path needs an explicit resource owner and safe non-commit behavior, not one universal
exception handler/status enum. Cancellation after an accepted edit does not implicitly undo it; reverting
the edit uses normal document Undo or a new explicit edit.

Prefer preparation and necessary durable placement before canonical commit where practical, so failed
preparation cannot leave half of a replacement/acceptance applied. If a workflow intentionally commits
a meaningful canonical edit first and secondary work later fails, that earlier edit remains a real Undo
transaction, with the secondary failure reported separately; failure itself adds no Undo step. A derived
execution failure likewise does not silently roll back accepted canonical intent. General filesystem/
document/database atomicity is not claimed. Q-063 retains concrete transaction/history, dependency
validation, invalidation/cancellation and operation-specific reuse mechanisms/evidence in
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md); Q-058/Q-059 retain recovery/media mechanics.

### Known dependencies and resource integrity

Before a global destructive operation, check known active dependencies and avoid invalidating live
or project state underneath them. This direction can apply to plugin/content-pack removal, managed
resource deletion, device changes while recording and destructive migration. Each subsystem owns its
exact block/defer/choice behavior; this is not a whole-filesystem dependency crawler.
[EXTENSIONS](EXTENSIONS.md#instance-removal-and-package-uninstall) owns package safety.

Media acceptance may publish durable availability only after successful project-managed storage;
Save/Save As/collect/relocate must preserve prior coherent state on failure. Recovery is a rolling current
working snapshot distinct from explicit Save, including unnamed documents, and does not recreate media.
Retained Undo/recovery/pending states may still own apparently unused resources, so deletion cannot follow
visible-reference removal alone. [PROJECT_FORMAT](PROJECT_FORMAT.md#media-and-persistence-integrity) owns
these integrity contracts and [recovery state](PROJECT_FORMAT.md#recovery-state), without selecting storage mechanics.

## Intended musical model

The accepted product direction for SEQ-R1 is:

- An **instrument** produces sound.
- A **musical part** contains notes/events for one sound definition; R1 implements the bounded type above.
- A **Pattern** owns reusable named musical content: parts/events and their references to instrument/sound
  definitions. It may use multiple instruments; it does not own those definitions exclusively or imply an audio bus.
- A **pattern clip** places/references a pattern in the Playlist/Arrangement's musical time.
- Repeated pattern clips normally reference shared Pattern musical content; an explicit operation
  creates an independent musical-content variation without implicitly detaching sound definitions.
- Mixer channels describe audio processing/routing, separately from arrangement tracks.
- Step Sequencer and Piano Roll edit compatible underlying musical event data.
- Pointer editing, on-screen musical keyboard, realtime note input, and MIDI recording converge on
  compatible events rather than independent note models.

Users choose pattern granularity. `Drums — Main` may contain Kick, Snare, and Hat parts, while bass/lead
use separate patterns. `Full Groove A` may combine those drums, bass, and synth. Other useful names
include `Drums — Fill`, `Bass — Verse`, and `Theme A`. Pattern identity is not bound to one arrangement
track or mixer channel.

**User-defined instrument/channel groups** organize instruments/channels independently of Pattern
membership. A `Drums` group can organize Kick, Snare, and Hat with meaningful naming/collapse. Multiple
patterns may use that group, and a pattern may use instruments from different groups. A group organizes
entities; a pattern owns/references musical parts. Neither is the other's storage identity. Exact
hierarchy/nesting rules remain open; unlimited nesting is not assumed.

The event model must support musical position, pitch where applicable, duration, and velocity or
equivalent intensity. A `bool[16]` foundation is insufficient. Future event vocabulary and editor APIs
remain open. R1's initial schema/time are defined above; [PROJECT_FORMAT](PROJECT_FORMAT.md)
owns persistence compatibility; [UX_CONTRACT](UX_CONTRACT.md) owns observable editing behavior.

## Project tempo and audio time

The project-wide tempo governs musical time. Notes, Patterns, their musical placements, later
automation, and other musical-time entities remain positioned in musical time as BPM changes.
Audio material additionally needs an explicit relationship to that tempo. At minimum, the model
must express concepts equivalent to:

- **Follow project tempo:** audio stays aligned to a musical duration/beat structure as BPM changes.
- **Fixed/source time:** audio retains its physical playback duration unless explicitly stretched.

A clip also supports local stretch independently of changing global tempo. Global tempo changes
and local clip stretch are distinct operations. Public labels, defaults, time/stretch representation,
and algorithms remain open. [UX_CONTRACT](UX_CONTRACT.md#audio-timeline-editing) owns distinct trim,
loop/repeat, and stretch intentions and ordinary non-destructive timeline edits;
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns preserving those relationships, not a selected schema.

Project-affecting configuration is independently project-owned, including tempo/time signature and
applicable sound/timing/processing settings. New-project defaults are copied at creation, never
live-linked to user preferences under [SETTINGS](SETTINGS.md#user-configuration-and-project-state).
[PROJECT_FORMAT](PROJECT_FORMAT.md#required-direction) owns identifying/version compatibility metadata
and useful open/migration diagnostics with R1's bounded schema and future migration policy.

## Separate sharing identities

The following identities and relationships are independently meaningful and must remain separately
editable and persistable where applicable. They describe ownership, not a required class per row.

| Conceptual identity                  | Owns / relates to                                                                                                                               | Does not imply                                                                  |
|--------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------|
| Musical content definition           | Pattern's named reusable parts/events and references to instrument/sound definitions; each part addresses one instrument                        | Exclusive sound ownership, a placement, or an audio bus                         |
| Instrument / sound definition        | Reusable sound intent, durable settings/configuration and required content references                                                           | Pattern ownership, shared live performance state, or routing every use together |
| Musical occurrence / placement       | One use of identified content/resource at a timeline position, supported local timing/range and item-local relationships                        | A new musical definition, resource rewrite, or movement of other uses           |
| Durable audio resource               | Accepted source audio and resource availability/lifetime, separately from its occurrences                                                       | A clip's position, trim, effects or processing membership                       |
| Arrangement container                | User-named timeline organization and placement membership, preferred purpose where useful; may expose an explicit containing processing context | Exclusive instrument/sound ownership, Pattern identity or Mixer identity        |
| Instrument Group                     | Organizational membership/naming of instruments independently of Pattern use                                                                    | Timeline placement or audible aggregation                                       |
| Item / containing processing context | Actual contribution membership, local processing and continuation to routes; explicit aggregate where requested                                 | Musical-content ownership or aggregation from mere visual containment           |
| Mixer route / channel / bus          | Global routing, deliberate mixing and processing; may present an existing local context                                                         | One container per channel or another copy of that context's DSP                 |

Content references, sound-use references, placement timing, organizational membership, actual processing
membership and downstream routes must not be collapsed into one relationship. Names, equal data or
proximity do not establish shared identity. This does not require a separate user operation for every
relationship: one coherent action can deliberately change several, with its scope understandable.

### Shared content and independent variations

Repeated placements normally reference one Pattern. Opening its musical editor from Placement B and
editing a Snare note edits that shared definition, so A, B and C all reflect the change. Opening from a
placement does not silently create private notes. B's start position, supported local timing/visible
range, item-local processing and future placement-specific properties instead affect B's occurrence.
Such properties do not rewrite the referenced events or change A/C; exact inventories/time forms remain
open. Moving B between Arrangement containers moves B alone, not every reference or the Pattern.
Renaming/reorganizing the reusable definition is a different scope from moving an occurrence or
assigning processing; changing definition organization cannot silently relocate its placements.

An explicit action conceptually called `Make Pattern Variation` creates/detaches independent musical
content and retargets the intended placement to it in one logical Undo transaction. The original
Pattern and its other uses remain intact. Sound definitions, source media, placement-local processing,
containing context and routes remain as intended; the action does not implicitly duplicate them or
runtime plugin instances. Later musical edits to the variation no longer edit the original content.
Undo restores the original reference and coherent canonical relationships, without deleting material
still needed by another use or retained history. R1 implements `MakePatternVariation` at this boundary;
the final user-facing command name is not selected.

### Independent sound and local processing

`Make Sound Independent` instead establishes independently editable sound-defining settings and updates
the explicitly intended sound-use reference (s). Two Patterns can retain their different notes while one
uses an independent Bass Synth definition. The new definition no longer follows edits to the old one;
it may still reference the same immutable sample/content. Musical notes and resources need not be copied.
If the intended scope is one occurrence of shared musical content, changing a reference stored in that
shared content would affect its other uses: the action must establish the intended independent use,
or explain an unavailable scope, rather than silently edit the shared reference. Reference/detachment
mechanisms remain Q-029; no universal per-property override system is selected.

This is a coherent logical edit distinct from Pattern variation. Supported durable/opaque source state
must be handled without a promise that every plugin supports arbitrary duplication. Unsupported required
independence is an explicit capability blocker or informed alternative, under the
[execution-domain contract](#shared-sound-definitions-and-execution-domains), not a hidden preset change.
Definition independence and runtime performance independence are separate; neither determines a plugin
instance count. Q-047 retains capability, synchronization and resource/performance evidence.

Two placements can share the same sound definition and have different item-local effects without
detaching that definition. Their required contributions remain independent before intentional mixing,
and their performance interaction follows the existing execution-domain semantics. There is no single
universal linked/unlinked state or mandatory unlink-everything operation.
[UX_CONTRACT](UX_CONTRACT.md#edit-target-and-sharing-feedback) owns understandable target/sharing feedback;
[PROJECT_FORMAT](PROJECT_FORMAT.md#musical-content-and-workspace-state) owns relationship preservation.

## Processing granularity and shared definitions

An atomic note, trigger, or step does not automatically own an arbitrary full DSP graph. Events may
eventually carry lightweight expressive properties such as pitch, intensity/velocity, duration, note
expression, or equivalent bounded controls. An unrestricted local processing graph belongs to a
standalone musical item/clip/fragment/placement-like scope, not every event in a Pattern.

For one hit needing substantially independent processing, provide a low-ceremony path conceptually
like `select event -> process separately / make independent fragment -> standalone item -> local graph`.
Users should not need to understand internal decomposition to make that hit sound different. Exact
command, event-to-item transformation, domain names, and graph ownership remain open (Q-048).
The resulting fragment's source event/content, timing and sound use must remain understandable. A
request concerning one occurrence cannot silently change the event in every shared Pattern use or
leave an unintended second trigger. Whether/how conversion retains a link to the source, removes or
suppresses its original occurrence, establishes independent content and follows later source edits
needs investigation under Q-048/Q-029 beyond R1's event schema. This is not a graph-per-note default or an extra local
level.

Shared instrument settings/definition must not automatically imply shared execution state or
irreversible mixed audio. For overlapping uses of a shared Bass Synth definition:

```text
shared Bass Synth definition
    -> Placement A contribution -> clean local/context path -> clean Mixer route
    -> Placement B contribution -> distortion/delay local/context path -> different Mixer route
```

The arrows express uses of a definition, not splitting an already mixed plugin output. A and B must
remain independently processable from sound production through their different required paths, even
while overlapping. Mixing their source output first and then splitting copies cannot produce the
independent clean and distorted performances. Any later common mix follows the graph boundary in
[NODE_GRAPH](NODE_GRAPH.md#contributions-and-irreversible-mixing).

The [execution-domain contract](#shared-sound-definitions-and-execution-domains) below defines the
required performance-state separation. Voice groups, instances, prepared routes and other bounded
representations remain possible mechanisms, with no selected instance count or free duplication.
[AUDIO_ENGINE](AUDIO_ENGINE.md#execution-state-lifetime-and-resource-integrity) owns lifetime/resource
constraints; Q-047 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) retains mechanism/performance evidence.

## Shared sound definitions and execution domains

The following semantic distinctions are accepted future contracts, not implemented objects:

| Concept                                | Ownership / meaning                                                                                                                                                                                   |
|----------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Shared instrument/sound definition     | Durable reusable sound intent: source/algorithm identity, base parameters, preset/configuration, content/resource references and sound-defining opaque extension data where applicable                |
| Runtime performance state              | Active voices, note ownership, mono/legato/retrigger history, voice-stealing interaction, envelopes, sample cursors and evolving source DSP state derived during execution                            |
| Execution domain                       | The semantic boundary within which musical events intentionally interact through runtime performance state; it may serve several occurrences and produce one or several distinguishable contributions |
| Independently processable contribution | A required distinguishable audible result under [signal ownership](#signal-ownership-and-processing-contexts); neither a domain nor a voice/placement/instance by definition                          |
| Processing/routing context             | Canonical relationships specifying processing membership, paths and intentional convergence; derived processor state follows those scopes rather than definition identity                             |

A definition can feed several independent execution domains without becoming several independent
sound definitions. Domains are derived from musical/performance intent and processing relationships;
the term does not select a persisted class, engine object, graph, plugin instance or allocation unit.
An execution domain is not another user-facing local processing level. Immutable content/resources
may remain shared while performance and local processor state are independent.

### Conditions for sharing execution

Sharing runtime performance state is permissible only when the intended note/voice interaction,
effective sound controls, required contribution paths and occurrence release/hard-boundary ownership
can all be preserved. The same definition, identical effect settings, a common eventual Master, or
two placements alone establish neither shared performance nor independence. The complete required
paths matter: two separate local Delays with identical settings still have distinct histories/tails.

Within an intended shared performance, mono note priority, legato/retrigger and limited-polyphony
voice stealing apply across its member events according to the instrument's behavior. Independently
performed occurrences must not steal/retrigger/release each other's voices or share evolving source
state merely because they reference one definition. This performance relationship must be expressible
in canonical musical/context intent; the domain grouping and its runtime realization are derived.
Exact reference representation, defaults and editing controls remain Q-019/Q-029.

Route divergence requires independently addressable contributions from sound production onward.
For independently performed A/B, their performance state also requires independent domains. A source
may preserve an intentionally interacting performance across separately routed contributions only
if it can actually expose the required distinguishable outputs without violating that interaction.
Separate voice/event outputs alone do not prove this for arbitrary shared source DSP. An aggregate-only
source cannot satisfy divergent routes in one execution; it may require separate domains/instances.
Changing a shared mono/legato performance into independent performances changes sound and must not be
presented as an equivalent optimization. If the requested interaction and output independence cannot
both be realized, follow the explicit capability-blocking direction in
[EXTENSIONS](EXTENSIONS.md#independent-execution-capability).

### Overlapping performance cases

| Case                                         | Accepted semantic result                                                                                                                                                                                                                                                                                                       |
|----------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| A — same polyphonic definition, same context | Multiple overlapping placements/events may share a domain when all conditions above hold and their aggregate occurs at an intended convergence. Placement count alone does not require duplication; the same destination alone does not permit it                                                                              |
| B — clean A versus Distortion/Delay B        | Preserve A/B outputs before local processing. Independently performed A/B have independent performance domains using the same definition; never split an already mixed source output to obtain them                                                                                                                            |
| C — mono, legato/retrigger or voice stealing | An intended shared performance lets A/B notes interact; an independent performance gives each domain its own interaction history/voice competition. Splitting or combining those domains can change pitch selection, attack and stolen notes even with the same preset; that sonic consequence is part of the musical contract |
| D — separable sampler/source voices          | Where source capabilities preserve the required semantics, separately routable voices/events can implement contributions efficiently, sharing immutable sample/definition data. Several independent semantic domains may be realized inside one capable source implementation; one domain can serve several placements/events  |

For a last-note-priority mono Bass with legato enabled, A holds C2 when B starts E2. An intended shared
performance selects E2 with that instrument's legato behavior; independent domains can keep C2 and E2
sounding separately. Releasing B may return the shared performance to C2 under its note-priority rules;
releasing independent B cannot retarget A. The preset alone therefore cannot define the resulting sound.

Use separation as coarse as these semantics permit. Neither one global execution instance per
definition nor one instance per placement/note is the default architecture. Physical instance count,
voice allocation, grouping/pooling and source capability mapping require later evidence, not a class
model inferred from this table. Pattern remains musical content, not a bus, and the two ordinary
local processing levels and separate Arrangement/Mixer identities remain unchanged.

### Shared definition edits

For shared Bass `Cutoff = 40% -> 55%`, the canonical edit changes that one definition's durable base
value. All uses still referencing it are expected to observe the updated sound intent, including uses
in separate domains, subject to their applicable expressive/automation controls. It does not merge,
copy between domains or reset their active voices, envelopes, cursors or tails merely because the
definition is shared. A parameter can affect an ongoing voice according to source/control semantics;
this is distinct from replacing that voice's evolving state with another domain's state.

An explicitly independent sound definition owns its own durable settings and no longer follows edits
to the original definition; it may still reference the same immutable content. Runtime independence
alone does not detach a sound definition. Exact realtime publication/synchronization and any necessary
source-specific transitions remain Q-047/Q-018/Q-057; concrete reference/edit/history mechanics remain
Q-029/Q-063 under [logical undo transactions](#logical-undo-transactions-and-history-scope).
[PROJECT_FORMAT](PROJECT_FORMAT.md#musical-content-and-workspace-state) owns durable preservation.

## Signal ownership and processing contexts

An **independently processable audible contribution** is the audible result of a musical occurrence
from a source/part/output whose required processing or route must be distinguishable from another's.
It can cover multiple notes/events with the same required context; it is not automatically a voice,
note graph, plugin instance, buffer or persisted object. A multi-instrument Pattern placement may
produce Kick and Snare contributions; overlapping placements of one instrument may also require
distinct contributions. Instrument identity alone therefore cannot determine all signal ownership.

A **processing context** describes which contributions are subject to processing and how their
results continue to other routes. Its canonical project relationships determine scope; engine state
is derived. Graph scope does not itself require one mixed audio output. Scope identities below are
semantic responsibilities, not concrete types or a requirement to instantiate a graph for every item.

| Scope / identity                        | Responsibility                                                                                                                                                                            |
|-----------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Pattern                                 | Reusable musical content and sound-definition references; no automatic submix                                                                                                             |
| Instrument / sound definition           | Sound-producing behavior/settings reused by musical occurrences; intrinsic synthesis/sampler processing does not create another nested placement-local level or authorize mixing all uses |
| Item / placement-local context          | Independent processing of one placed musical occurrence, including distinguishable contribution paths or an intentional whole-placement submix                                            |
| Containing musical-container context    | Common processing of an explicit aggregate of contained item results; timeline containment alone does not require aggregation                                                             |
| Instrument/channel organizational group | Naming, membership and organization; no implied audio processing or bus                                                                                                                   |
| Mixer channel / bus context             | Audio routing, deliberate aggregation and processing/control of routed results, separately from timeline identity                                                                         |
| Master / Output                         | Global final aggregation/processing and output boundary                                                                                                                                   |

The two ordinary local levels remain item-local and containing-container. Source-definition behavior
and global channel/bus/Master processing do not add further nested local levels. A Pattern's musical
parts are not automatically extra DSP scopes. A need for arbitrary processing of one atomic event
still uses the independent-item direction above, not an implicit graph per note.

Whole-placement processing means intentionally treating that placement's audible contributions
as one local submix before common processing. Whole-container processing similarly aggregates its
contained results at the containing level. Neither introduces a third local level. Independent paths
remain available until an intentional mix; the graph owner defines exactly what that mix loses in
[NODE_GRAPH](NODE_GRAPH.md#contributions-and-irreversible-mixing). Source/content/resource identities
remain editable and non-destructive even though a mixed stream cannot recover its independent inputs.

### Cross-context ownership and identity

Explicit cross-context signal/control relationships connect existing processing contexts; they do not
create another musical owner, a third local processing level or one universal graph. The source owns
its intended contribution/aggregate and boundary; the receiving processor owns the affected processing
and detector/control role. [NODE_GRAPH](NODE_GRAPH.md#cross-context-signal-and-control-relationships)
owns relationship kinds, taps, validity and endpoint lifetime. A detector/control dependency from
outside a container does not make that signal part of its audible submix or turn its processor into a
broader audible bus. Actual audible aggregation of outside contributions still belongs to that broader
route. Arrangement/Mixer exposure does not duplicate either processing or dependencies.

Relationships distinguish occurrences/contributions from reusable sound or Pattern definitions.
Consumer fan-out alone does not request additional performances; different overlapping placements
retain the contribution paths and execution-domain intent above. Definitions and performance state
cannot be merged or duplicated merely to simplify dependency scheduling. Concrete reference/scope
ownership, grouping and source capability realization remain Q-019/Q-030/Q-047/Q-066.

Moving source or target preserves meaningful logical identity, not necessarily the signal or context.
An item-owned tap can remain attached across a context move when its boundary survives; a tap on a
particular containing aggregate still names that aggregate. Changing processing membership may change
an applicable tapped signal; purely visual/organizational moves cannot silently redirect dependencies.
Changed endpoints/boundaries/capabilities require revalidation or deliberate reassignment, never matching
by name, position or selection. The move and necessary relationship edits share one canonical Undo
transaction; preparation/publication remains derived under the document integrity contract.

## Resources, placements, and two local processing levels

A durable audio resource and a musical occurrence/placement of it must not be assumed to be one
mutable object. Several items may reference `kick_017.wav`; local processing of one item must not
silently rewrite the shared resource or every other use. The default creative model is non-destructive.
An explicitly destructive/edit-source operation may be justified later, but ordinary item processing
does not imply it. R1 implements descriptor/reference/edit identity; F1 implements bounded managed WAV
storage under [project format](PROJECT_FORMAT.md#r2-f1-managed-wav-layout). Wider resource mechanisms remain open.
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns persistence obligations.

Ordinary import/drag-and-drop creates durable project-managed media independent of its original arbitrary
external path; any deliberate external-reference workflow needs explicit separate justification
under [PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary). That owner defines covered material
and preservation obligations; this architecture boundary does not select storage mechanisms.

For ordinary project work, the accepted user-facing model has no more than two local processing levels:

1. **Item-local processing:** one material instance/placement has independent processing, such as
   EQ/Gain on a Kick item or Delay on a Click item.
2. **Containing musical-container processing:** an explicit shared processing context combines its
   contained audible item results and applies common processing to that submix. Mere musical
   containment or organization does not automatically enable this mix.

```text
Kick item -> local EQ/Gain --+
Click item -> local Delay ---+-> container mix -> Compressor -> output
Noise item -----------------+

Item result(s) -> explicit container submix/processing, if used -> global routes/buses -> Master -> Output
```

Without a containing submix, separate item/contribution outputs continue to their assigned routes.
The diagram's shared Compressor intentionally consumes the aggregate; independent item processing
happens before it. This bounds the ordinary creative mental model, not the engine's number of DSP stages.
Global mixer, buses, master, and output remain available responsibilities. Master is global output processing,
not a third nested local layer. Do not infer unlimited user-facing local nesting.
[NODE_GRAPH](NODE_GRAPH.md) owns how processing connections express signal dependencies.

A natural source end may allow item-local effect tails. An explicit user clip/item right boundary ends
that item's own audible result, including its local tail, before containing-container/bus/Mixer/Master
processing. It does not erase arbitrary shared downstream state after irreversible mixing or introduce
a third local processing level. [AUDIO_ENGINE](AUDIO_ENGINE.md#source-boundaries-and-effect-tails) owns
the processing-scope and hard-boundary contract; exact state/de-click mechanics remain open.

"Layer" is provisional terminology, not a final public/domain name. Track, Layer, Channel, Lane,
or Container may overlap future vocabulary. The accepted semantics are a musical timeline container
holding independent items, with an explicit context for processing their combined audible result when
requested. Its domain identity is not assumed identical to Mixer Channel, Instrument Group, or Pattern.
R1 implements distinct local context/route intent, without a complete Arrangement container schema.

## Semi-free Arrangement

Arrangement uses user-named containers for useful visual/musical organization rather than requiring
permanent one-instrument ownership or a completely unstructured timeline. Conceptually:

```text
Drums       | Drums Main | Drums Main | Drums Fill |
Bass        | Bass A     | Bass A     | Bass B     |
Atmosphere  |          Long Texture               |
```

A container may have a preferred/default musical purpose or content relationship. That must not
automatically make it the permanent or exclusive owner of one instrument, sound definition or Mixer
Channel. A `Drums` purpose can guide naming, initial targeting and useful compatible defaults. A Bass
Pattern is not incompatible merely because of that name/purpose: where the container supports the
musical occurrence and its required context, reuse is allowed without copying or reassigning its sounds.
Defaults cannot silently convert content, detach sharing or change established processing/routing.
An explicit processing-context placement can change sound, with the feedback below. Exact defaults,
compatibility tests and warnings remain Q-028/Q-030.

### Compatible material and bounded organization

The minimum compatibility boundary is a supported timeline occurrence with meaningful placement/time
relationships and supported intended processing/routing semantics. Multi-instrument Patterns, standalone
musical fragments, audio sample clips and later recorded-audio occurrences belong to the Arrangement direction
when supported. They retain their own content, musical/source-time, resource and processing relationships;
they need not share an identical internal shape. Recorded audio has a durable source resource separately
from its timeline use; recording/capture alignment is not resolved here.

Semi-free does not mean accepting every object everywhere. A sound definition, Instrument Group or
Mixer route alone is not a timed occurrence. An unsupported structural/content/time relationship can
block placement or require a deliberate supported transformation; do not silently render, destructively
convert or duplicate material to make it fit. Container purpose is a preference, while actual supported
relationships are constraints. Compatibility algorithms and the first supported type inventory remain
open; no final public Track/Layer/Lane/Container term is selected.

Document compatibility is distinct from current executability: safely representable material with
missing media/plugins or incomplete graphs may remain arranged/editable, with dependency-scoped execution
blockers. A capability failure cannot silently convert its content or change required sound to make it
playable. The existing degraded-access/canonical-save contracts remain binding.

Nested organization may be useful for naming/collapse, but does not by itself add processing. The
minimum ordinary model remains items in named containers with distinguishable organization and optional
explicit containing processing. An occurrence's ordinary local path has item-local processing and at
most one containing-container processing level. An organizational ancestor cannot automatically add
another aggregate/chain; nesting two serial processing containers after item-local processing would
violate that bound. Global buses remain explicit routing responsibilities, not an invisible relabeling
of extra nested local levels. Flat-only versus bounded organizational nesting, depth, presentation,
membership/assignment mechanics and handling of competing containing contexts remain Q-028; Instrument
Group hierarchy remains Q-023. No unlimited processing tree or universal overrides are accepted.

### Deletion scope and retained relationships

| Requested deletion                      | Semantic boundary                                                                                                                                                                                                                      |
|-----------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| One Pattern placement                   | Remove that occurrence and its intended local relationships; preserve the Pattern, other placements, shared sound definitions and media                                                                                                |
| A shared Pattern definition             | Check its known uses; block/defer or offer a deliberate dependency-resolving choice rather than silently removing placements or redirecting them to similar content                                                                    |
| An Arrangement container                | Distinguish organization removal from removing contained occurrences or its processing context. With known members/routes/dependencies, resolve their disposition deliberately; no hidden broad cascade or sound-changing reassignment |
| A sound definition still used elsewhere | Respect those uses; block/defer or deliberately replace/detach/remove affected references with understandable scope, never destroy unrelated musical content or shared media                                                           |

Deletion of a visible use does not establish that its definitions/resources are disposable. Undo restores
the intended canonical relationships coherently; it does not rewind DSP state or renew cancelled async
work. Known endpoint/dependency changes follow
[cross-context ownership](#cross-context-ownership-and-identity) and
[document integrity](#document-integrity-and-asynchronous-publication). Exact cascades, orphan handling,
reference storage/counting, confirmation and history/resource retention remain Q-029/Q-019/Q-063/Q-059.
Explicit future edit-source/destructive media operations need separately defined affected-use/lifetime
semantics; ordinary trim, movement and local processing do not rewrite source audio.

### Arrangement context and Mixer presentation

The timeline container answers where/when material is arranged; processing context answers which
signals receive which processing; channels/buses answer where results flow and combine. Mixer is
the presentation/control surface for audio contexts/routes, not an automatic extra DSP layer.
Arrangement and Mixer identities remain separate, without a required one-to-one correspondence.
Useful defaults can bind them; one universal Track combining music, organization, DSP and routing
is not the accepted model.

Arrangement and Mixer may expose/reference the **same underlying processing/routing context**.
For example, the Arrangement container's shared Compressor may also be controlled from a Mixer
channel showing that context. It runs once at the same semantic boundary; viewing it in Mixer does
not apply a second Compressor. Its local scope does not become global merely because Mixer exposes it.
Conversely, shared processing of an audio aggregate containing contributions from outside that
container belongs to the broader route; it cannot silently be advertised as exclusively that
container's own processing. An external detector/control dependency alone does not add such audible
contributions; [cross-context ownership](#cross-context-ownership-and-identity) preserves that distinction.

```text
item contributions -> item-local paths -> container submix -> Compressor context C
    -> global bus -> Master -> Output

Arrangement control of C <-> same context C <-> Mixer control of C
```

The last line describes presentation, not another audio connection. A separate downstream Mixer
channel/processor can exist when the project explicitly routes through that distinct context; it
is not required just because two UI surfaces exist. Global bus/Master processing remains outside
the two local levels. Graph topology defines order and aggregation, not the pane where a control
was edited. Exact route assignment/defaults, control bindings and context reference/edit mechanics
remain Q-030/Q-019; compatibility/terminology remain Q-028. R1's context/route identities are a bounded foundation only.

### Moving material between contexts

At unchanged musical time, moving Kick from a Compressor processing container to a Distortion/Delay
processing container changes its processing membership and hence its signal path:

```text
before: Kick -> unchanged item-local result -> old container submix -> Compressor -> downstream route
after:  Kick -> unchanged item-local result -> new container submix -> Distortion -> Delay -> downstream route
```

Kick leaves the old aggregate and joins the new one. The different processors/input aggregates cause
the sound change; other members may also sound different because a shared processor now receives a
different mix. A downstream route change, if part of the context assignment, is a further audible
consequence that must be understandable. The move does not inherently rewrite shared Pattern content,
the Kick sound definition or source media. Existing downstream effect state is not retroactively erased;
[AUDIO_ENGINE](AUDIO_ENGINE.md#source-boundaries-and-effect-tails) owns that distinction and Q-057 its mechanics.

Moving the same Kick between purely organizational groups at unchanged time retains its processing
membership and routing, so it does not change sound. Musical containment, organization and routing
are separate relationships: an operation changing processing membership is a context move, not a
sound-neutral regrouping. [UX_CONTRACT](UX_CONTRACT.md#processing-context-and-mix-feedback) owns making
that consequence visible without requiring graph expertise. A move's accepted placement/context changes
form one [logical undo transaction](#logical-undo-transactions-and-history-scope); exact assignment,
edit representation and execution-transition mechanics remain Q-030/Q-019/Q-063/Q-057.

### Small composition ownership example

`Drums Main` contains Kick, Snare and Hat parts and is placed repeatedly. One placement becomes
`Drums Fill` through musical-content variation; Main and Fill still reference the shared Kick/Snare
sound definitions. An independent Bass Pattern references Bass Synth. Two audio clips reuse one
managed sample. These definitions and occurrences remain distinct even if some labels or data match.

Two Arrangement containers expose explicit contexts: `Drums` has Compressor, `Textures` has
Distortion/Delay. Drum placements intentionally join the first aggregate; reused audio occurrences can
join different contexts. Bass can retain a separate contribution route or deliberately join a compatible
context. Outside explicit submixes, Kick/Snare/Hat outputs can retain their own Mixer routes; after an
intentional aggregate, its route carries the aggregate, not recovered individual instruments. The named
containers do not create Pattern buses or dictate one Mixer channel each. Ordinary global routes/buses
continue to Master; Mixer may expose either local context without applying it twice.

Editing a Main Snare note updates Main's repeated uses; Fill's notes remain independent. Editing the
shared Kick definition updates its intended Main/Fill uses, while local effects on one occurrence do
not. Moving one Fill occurrence to Textures changes its processing/aggregate membership and any assigned
downstream route, potentially changing both mixes, without moving Main or editing shared sounds/media.
Simultaneous occurrences in both contexts retain independently required contributions and R11
performance-domain intent; they cannot be recovered from one prematurely mixed source output.

Variation, sound independence, moves and accepted sample-use changes each commit their own coherent
logical Undo edit. Save/reopen preserves the resulting references, memberships and routes explicitly,
including incomplete canonical work; it cannot infer sharing from layout. Later object rendering follows
the selected owner: Main definition without a placement context, one placement through item-local
processing, or a container through its own processing. It preserves the source and does not automatically
bake unrelated downstream Mixer/Master processing, under
[object sampling](SAMPLE_WORKFLOW.md#create-sample-from-object). No new render/release scenario is defined.

The ordinary path needs no ownership questionnaire: arrange occurrences, edit named shared music,
change a selected occurrence locally, and request independence when needed. Target/sharing and explicit
mix consequences must be understandable. Exact representation/UI/capability evidence remains open; the
example establishes semantics rather than an implemented composition or the full R12 acceptance case.

## Future parameter control

Parameter modelling must leave room for future control rather than treating DSP parameters as
permanently fixed primitive values. An eventual effective parameter may combine base/user value,
timeline automation, modulation, and envelopes/LFO/control-graph sources. This requirement does not
select an abstraction or require an automation implementation now. Replace/add/multiply semantics,
normalized versus physical domains, precedence, smoothing, and control rate remain unresolved.
[NODE_GRAPH](NODE_GRAPH.md) owns the preferred direction for optional parameter connectors.

## Host localization and UI resources

Localization and semantic UI resources are host platform responsibilities. These are accepted logical
boundaries, not proposed services/classes/interfaces or implemented capabilities. Keep distinct stable
semantic resource identity, localized display content, current host language preference, contributor
language support/fallback, semantic style roles, current host theme and provider ownership/lifetime.
Lookup and rendering depend on those presentation concerns; musical-domain identity does not.
First-party Seqvium UI/extensions and Seqvium-native contributions consume the host foundation rather
than introduce parallel localization/style systems. [UI_DESIGN](UI_DESIGN.md#themes-and-semantic-resources)
owns semantic Dark/Light roles; [EXTENSIONS](EXTENSIONS.md#ui-resource-contribution-lifecycle) owns
contribution availability/removal, and [ROADMAP](ROADMAP.md#localization-and-theme-foundation-ownership)
owns minimum introduction timing. Independent third-party native editors retain their own presentation.

### Stable identity and provider ownership

Seqvium-authored display content uses stable localization identifiers independent of translated text.
Commands, parameters, statuses and actions can change labels without changing persisted command
identities, project property keys, node/port identities, extension capability identifiers or serialization
discriminators. Presentation-resource identity does not replace those domain identities. User-created
Pattern, sample and track names remain user content, not automatic host translations.

Contributed resources are scoped by stable contributor identity as well as resource identity. A similar
human-readable name/key from two providers must not collide or silently replace another provider's
resource, including host-owned fallback resources. The host owns resolution rules and the contributor
owns its supplied content and availability; labels do not establish ownership. Exact identifier syntax,
manifest fields, duplicate-registration handling and APIs remain open in Q-054/Q-055/Q-067.

### Language support and resource fallback

Russian and English are the initial first-party UI baseline, with room for additional languages.
English is the baseline fallback for Seqvium-authored/native first-party contributions. The host's
selected language and each contributor's supported languages/fallback are independent:

- Use a usable entry for the host language when the contributor supplies it. An English-only extension
  in a Russian host uses its English fallback; the capability remains available and other surfaces stay
  Russian. Likewise, future host Spanish can coexist with a RU/EN contributor using English.
- Resolve missing or unusable entries per resource, even when the contributor advertises the selected
  language. A missing Russian first-party label falls back to that resource's usable English entry,
  not a change to the entire host or contributor language. Invalid translation values take the same
  fallback direction; exact validation/parser and diagnostic representation are unselected.
- If neither the requested entry nor the supported contributor fallback is usable/available, provide
  understandable host-owned generic presentation appropriate to the action/status and available stable
  metadata. Do not require contributor code to supply an error label, expose an unexplained raw key as
  the sole essential label, substitute a translated label for identity or hide an unrelated capability.

Other native contributors may supply additional languages and a supported fallback without changing
the user preference. Localization availability is not executable capability/version compatibility.
Noncritical missing presentation resources cannot alone disable working processing, delete project data
or change compatibility outcomes. This does not promise recovery of every malformed third-party
resource or safe rendering of arbitrary broken UI; contain unavailable presentation and retain host
explanation under [Extensions](EXTENSIONS.md#ui-resource-contribution-lifecycle).

### Preference changes and locale-aware presentation

Language/theme are user/application preferences under [SETTINGS](SETTINGS.md#user-configuration-and-project-state).
Changing them with a project open must preserve musical content, routing, automation, sound settings,
saved resources, stable identities and in-progress editing intent. Observable application/defer behavior
belongs to [UX](UX_CONTRACT.md#language-and-theme-preference-changes); no instantaneous hot switch,
window reconstruction or mandatory restart policy is selected.

Localized number/musical-value/unit presentation and formatted diagnostic messages must remain distinct
from canonical numeric values and serialization. Neither translated strings nor operating-system
culture may determine persisted musical values or reinterpret unchanged project data. Units may have
localized display labels without changing the represented quantity. Future text entry must resolve
accepted input deliberately to the intended canonical value and handle ambiguity visibly; a preference
change must not silently reinterpret an in-progress numeric edit. Exact decimal/parsing/display rules,
formatting APIs and locale-selection policy remain Q-054, not a selected runtime culture model.

Resource formats, contribution/registration APIs, fallback representation, contract versioning,
theme packaging and platform evidence remain open in Q-054/Q-055/Q-067. No UI framework, resource files,
SDK or packaging model is selected. Later expansion does not commit to downloadable language packs,
an online translation platform, theme marketplace/SDK/compiler, live resource reload or customization
editor. Canonical repository/source language remains English under [CODING_GUIDELINES](CODING_GUIDELINES.md).

## Open architecture work

[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks feasibility, ownership/lifetime, control overload, scheduling,
and other validation gaps. Promote a supported choice in its current owner and preserve rationale in
the cold decision archive, with the experiment's limits intact. Do not treat a successful probe as
proof of an entire future workstation architecture.
