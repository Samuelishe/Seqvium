# Architecture

Role: Logical responsibility and dependency boundary guide.
Read when: Structuring code, reviewing coupling, or evaluating architecture proposals.
Authoritative for: Foundation-first logical domain/application/adapter/presentation boundaries, project lifecycle, timeline/organization/graph separation, musical/resource identities, semantic execution domains, signal ownership and processing scopes, Arrangement/Mixer relationships, canonical/derived state, semantic-action/input boundary, logical undo transactions and async commit integrity, host UI services.
Not authoritative for: Exact project decomposition, audio internals, extension API, file format, or progress.

No production architecture is implemented. Accepted responsibilities and musical direction bind future
design; implementation shape, schemas, and native choices still need bounded design and evidence.

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

| Concern | Responsibility |
| --- | --- |
| Domain | Musical, resource and project concepts/invariants |
| Application / use cases | Operations coordinating document edits and workflows |
| Infrastructure / adapters | Persistence, filesystem, audio devices/backends and plugin loading |
| Presentation | Workspace interaction and UI, including Avalonia if selected |

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
format remain open under [PROJECT_FORMAT](PROJECT_FORMAT.md).

Early architecture deliberately establishes the rails later work depends on: document and musical-domain
ownership, versioned serialization, settings/configuration separation, diagnostics/logging, extension
hosting, resource ownership, transport/musical clock, audio/backend/device abstraction, realtime execution,
asynchronous operation integrity and undo/edit transactions. Host localization/theme services enter at
the appropriate stage. This does not require all subsystems, extra layers or interfaces before any UI;
[ROADMAP](ROADMAP.md) owns sequencing and the later complete-project acceptance exercise.

## Core platform responsibilities

Keep these logical responsibilities distinct even if some later share an assembly. This is long-term
ownership, not a requirement to implement every capability in the first stages:

| Responsibility | Boundary |
| --- | --- |
| Application shell / workspace panes | Owns main-window composition and user workspace state; [WORKSPACE](WORKSPACE.md) owns behavior |
| Project / document model | Owns musical relationships, edits, resource references, and document integrity |
| Transport / musical clock | Host-owned musical execution context; cannot be replaced by a plugin |
| Audio engine / execution | Owns scheduling and execution-time state; isolated from arbitrary mutable UI/project objects |
| Node graph engine | Core signal-graph ownership; [NODE_GRAPH](NODE_GRAPH.md) owns its contract |
| Audio device abstraction | Host-owned input/output boundary below engine processing contracts |
| MIDI device / input foundation | Host owns device selection and musical input integration |
| Audio / MIDI recording foundations | Host owns capture and timeline/resource integration; polished recording UX may arrive later |
| Mixer / bus foundations | Core mixing/routing primitives may precede the full Mixer workspace |
| Audio resource ownership | Controls availability and lifetime of project audio independently of optional UI |
| Undo / redo | Owns coherent document edits; optional surfaces cannot replace edit integrity |
| Serialization | Preserves versioned document and extension data under the format contract |
| Extension hosting / lifecycle | Hosts attached capabilities; fundamental platform ownership stays in the base |
| User configuration / diagnostics | Host owns application preferences and bounded production diagnostics; [SETTINGS](SETTINGS.md) owns policy |
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

| Concept | Describes | Canonical boundary |
| --- | --- | --- |
| Musical timeline | Patterns, notes/events, pattern/audio clips, later automation and recordings in musical time | Musical model below |
| User organization | Instrument/channel groups, names, and workspace organization | Group identity below; pane composition in WORKSPACE |
| Signal graph | Sources, processors, mixing/splitting, effects, buses/output where applicable | NODE_GRAPH |

Do not collapse these into one universal graph. Arrangement places music in time; the signal graph
describes audio/control flow and does not replace Arrangement. Organization may visually correspond
to either without becoming its storage identity or defining routing by itself.

## Proposed application and audio shape

C# / .NET 10 / Avalonia is the application-layer candidate, not an installed stack here.
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
ABI, control publication, backend strategy, and C# decomposition remain open; no final classes exist.

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

| Concept | Semantic responsibility |
| --- | --- |
| User intent / logical edit | The requested musical/document outcome and its target/scope; requesting long work does not itself mutate the document |
| Canonical document mutation | A change to the single editable project truth, validated against the state in which it is accepted |
| Undo transaction | One coherent accepted document intention, potentially changing several canonical relationships together |
| Transient interaction preview | Explicit temporary feedback/audition with a cancel/restore path; not accepted document state or history |
| Async computation/preparation | Produces a candidate/prepared result from identified inputs; completion alone is not an edit |
| Async result commit | Revalidates the operation and accepts its canonical effect through the normal transaction boundary |
| Derived realtime publication | Makes a valid prepared canonical revision executable under the graph/audio contract; not a second editable history |
| Durable resource creation | Establishes storage/ownership of produced material; a file's existence alone neither inserts it into the project nor authorizes an edit |

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
stack, event-sourcing framework or threading primitive is selected.

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
not selected token fields, UUIDs, revision stamps, schemas or cancellation APIs.

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
- A **musical part** contains notes/events for one instrument; no class or storage schema is selected.
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
equivalent intensity. A `bool[16]` foundation is insufficient. These requirements do not select a
schema, time representation, class hierarchy, or storage layout. [PROJECT_FORMAT](PROJECT_FORMAT.md)
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
and useful open/migration diagnostics without selecting a schema.

## Separate sharing identities

Pattern musical content, instrument/sound definition, placement state, and processing state are
distinct identities whose sharing or independence must be expressible separately. There is no single
universal linked/unlinked state. Editing shared Pattern content changes every placement using it;
changing placement-local position, processing, or future fades does not change all those placements.

An explicit action conceptually called `Make Pattern Variation` copies/detaches musical content.
It must not implicitly detach every associated sound, resource, or processing relationship. A separate
intention, conceptually `Make Sound Independent`, may copy/detach an instrument/sound definition.
These are conceptual names, not selected commands, UI, or internal copy mechanics. A hidden
unlink-everything operation must not be the only model. [UX_CONTRACT](UX_CONTRACT.md) owns making
meaningful sharing understandable; [PROJECT_FORMAT](PROJECT_FORMAT.md) owns relationship preservation.

## Processing granularity and shared definitions

An atomic note, trigger, or step does not automatically own an arbitrary full DSP graph. Events may
eventually carry lightweight expressive properties such as pitch, intensity/velocity, duration, note
expression, or equivalent bounded controls. An unrestricted local processing graph belongs to a
standalone musical item/clip/fragment/placement-like scope, not every event in a Pattern.

For one hit needing substantially independent processing, provide a low-ceremony path conceptually
like `select event -> process separately / make independent fragment -> standalone item -> local graph`.
Users should not need to understand internal decomposition to make that hit sound different. Exact
command, event-to-item transformation, domain names, and graph ownership remain open.

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

| Concept | Ownership / meaning |
| --- | --- |
| Shared instrument/sound definition | Durable reusable sound intent: source/algorithm identity, base parameters, preset/configuration, content/resource references and sound-defining opaque extension data where applicable |
| Runtime performance state | Active voices, note ownership, mono/legato/retrigger history, voice-stealing interaction, envelopes, sample cursors and evolving source DSP state derived during execution |
| Execution domain | The semantic boundary within which musical events intentionally interact through runtime performance state; it may serve several occurrences and produce one or several distinguishable contributions |
| Independently processable contribution | A required distinguishable audible result under [signal ownership](#signal-ownership-and-processing-contexts); neither a domain nor a voice/placement/instance by definition |
| Processing/routing context | Canonical relationships specifying processing membership, paths and intentional convergence; derived processor state follows those scopes rather than definition identity |

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

| Case | Accepted semantic result |
| --- | --- |
| A — same polyphonic definition, same context | Multiple overlapping placements/events may share a domain when all conditions above hold and their aggregate occurs at an intended convergence. Placement count alone does not require duplication; the same destination alone does not permit it |
| B — clean A versus Distortion/Delay B | Preserve A/B outputs before local processing. Independently performed A/B have independent performance domains using the same definition; never split an already mixed source output to obtain them |
| C — mono, legato/retrigger or voice stealing | An intended shared performance lets A/B notes interact; an independent performance gives each domain its own interaction history/voice competition. Splitting or combining those domains can change pitch selection, attack and stolen notes even with the same preset; that sonic consequence is part of the musical contract |
| D — separable sampler/source voices | Where source capabilities preserve the required semantics, separately routable voices/events can implement contributions efficiently, sharing immutable sample/definition data. Several independent semantic domains may be realized inside one capable source implementation; one domain can serve several placements/events |

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

| Scope / identity | Responsibility |
| --- | --- |
| Pattern | Reusable musical content and sound-definition references; no automatic submix |
| Instrument / sound definition | Sound-producing behavior/settings reused by musical occurrences; intrinsic synthesis/sampler processing does not create another nested placement-local level or authorize mixing all uses |
| Item / placement-local context | Independent processing of one placed musical occurrence, including distinguishable contribution paths or an intentional whole-placement submix |
| Containing musical-container context | Common processing of an explicit aggregate of contained item results; timeline containment alone does not require aggregation |
| Instrument/channel organizational group | Naming, membership and organization; no implied audio processing or bus |
| Mixer channel / bus context | Audio routing, deliberate aggregation and processing/control of routed results, separately from timeline identity |
| Master / Output | Global final aggregation/processing and output boundary |

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
does not imply it. Resource/reference/edit ownership and storage schemas remain unselected;
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
No classes or schemas are selected.

## Semi-free Arrangement

Arrangement uses user-named containers for useful visual/musical organization rather than requiring
permanent one-instrument ownership or a completely unstructured timeline. Conceptually:

```text
Drums       | Drums Main | Drums Main | Drums Fill |
Bass        | Bass A     | Bass A     | Bass B     |
Atmosphere  |          Long Texture               |
```

A container may have a preferred/default musical purpose or content relationship. That must not
automatically make it the permanent owner of one instrument or Mixer Channel. Compatible material
should be movable/reusable without arbitrary structural duplication. Compatibility, preferred-target
behavior, ownership, nesting, and audio-clip semantics remain open.

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
remain Q-030/Q-019; compatibility/terminology remain Q-028. No object model is selected.

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
