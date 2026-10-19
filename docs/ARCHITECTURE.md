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

## Pattern resampling ownership

`PatternResampling` is a bounded Core operation over one captured item-local Pattern attachment.
`GraphPreparation.PrepareObject` prepares its source-time-zero local graph without compiling Mixer,
reusing existing prepared tables and OfflineSampler. Ordinary preparation and WAV export retain
their Mixer behavior. This explicit boundary avoids treating exported downstream audio as an object
sample, without creating a second renderer or general object hierarchy.

DesktopWorkflow owns one asynchronous request, cancellation/deadline/progress and final managed
acceptance. It does not hold the ordinary command semaphore during computation, so edits, transport,
Cancel and lifecycle commands remain responsive. A frozen document is closed in worker completion;
live canonical edits permanently withdraw acceptance authority. New/Open/Close cancel/join the work
while their command gate prevents a late commit; they never dispose its owner context first.
The same managed WavImport owns the durable candidate and rolls back unaccepted files. Its existing
generation/lifecycle/integrity gate and one Edit accept resource/sampler identity atomically. A narrow
accepting flag excludes that transaction's own post-edit notification from request invalidation.

Browser consumes the accepted ResourceId and exposes existing preview/reuse; selection is never a
commit target. No temporary render bytes enter Undo. Undo/Redo retain accepted media through existing
storage policy. Provenance records Pattern/attachment/revision and local tap; durable PCM is
authoritative rather than an instruction to regenerate. No project format or assembly is added.
[Sample workflow](SAMPLE_WORKFLOW.md#implemented-pattern-resampling-seq-r8) owns product bounds;
[audio](AUDIO_ENGINE.md#pattern-object-rendering-seq-r8) owns execution and tails.

## PCM sampler editor ownership

SEQ-SAMPLER-KEYMAP-F1 extends that same sound with identified per-source zones. SamplerKeymap selects
a source during preparation; immutable PreparedNote retains its resource identity, pitch step and
range. There is no second engine, graph node type or Mixer assignment per zone. SamplerMapImport
borrows WavImport's durable candidates and rollback, then accepts every descriptor and the map in
one generation-checked document transaction. Unknown roots are explicit Desktop proposals, never
inferred musical data. The Source pane owns selected-zone/view state independently of canonical map
intent; source inspection and captured edits retain target/generation checks.

SEQ-SAMPLER-EDITOR-F1 adds source-frame playback intent to the existing identified sampler sound,
interpreted by SamplerPlayback and compiled into PreparedNote. The immutable PCM/media owner remains
unchanged. Source editor waveform observations reuse AudioWaveform, with retained immutable PCM only
for bounded fine zoom. DesktopWorkflow serializes generation-checked edits and the existing solo
preview lifetime; the pane owns proposals/viewport/observation, never output or prepared voices.
Live compatible range/loop publication retires changed voices without retrigger, preserving time and
unrelated occurrences. [Audio](AUDIO_ENGINE.md#sampler-source-range-and-sustain-loop-seq-sampler-editor-f1)
and [format](PROJECT_FORMAT.md#sampler-range-and-sustain-loop-reader-19) own the exact bounds.

## Sample Lab ownership

The optional Desktop `Generators/PercussionGenerator` is reachable only through its trusted R6
factory/use. Core owns sealed exploration metadata and disposable bounded candidate history.
`DesktopWorkflow.SampleLab` serializes host history, target authority, generation completion and
existing managed acceptance; the retained pane owns presentation only. A generation session/use
ends after validated candidate transfer. Independent generation uses lifecycle relevance; the older
R6 strict-generation acceptance adapter remains available for its existing callers.

`SampleAuditionSource` transforms a frozen prepared PCM plan without editing a document or creating
a resource. The coordinator owns a source lease and each preparation borrows its own lease. Publication
uses existing sampler authority/retirement; continuation excludes the changed sound while preserving
unrelated compatible notes and Mixer state. Clear/invalidated context re-prepares actual current
canonical state. Saved projects/offline export never consume the temporary override. No second
transport, DSP, Undo, media cache or generic preview framework is introduced. Exact supported scope
belongs to [Sample workflow](SAMPLE_WORKFLOW.md#implemented-sample-lab-v1).

## Implemented R6 extension ownership

Core `Extensions/` owns bounded manifest discovery/contract checks, trusted registered factories,
package state and counted use/retirement. Package identity/version, contributed capability,
active session and accepted project resource are separate. No new assembly, dynamic DLL loader,
realtime plugin contract or dependency from the audio callback is introduced. The host platform
contracts and deployment/trust limits belong to [Extensions](EXTENSIONS.md#implemented-r6-local-foundation).

Desktop owns enablement preferences and the small host-rendered Browser disclosure. Content sources
carry package/capability/content identity and reacquire a lease through inspection/preview/import;
disabled captured sources cannot bypass availability. Successful uses call existing `ProjectMedia`
or `AudioMedia`, retaining one document transaction and ordinary managed-media/history authority.

`SampleGenerationSession` owns one exploration lifetime, serial worker execution, cancellation,
supersession and temporary candidate disposal. `DesktopWorkflow` owns its document sessions and joins
them before New/Open/Close retires that document; application Close then retires the package host.
The acceptance seam captures a Rack destination, validates lifecycle/generation and candidate identity,
and commits through the existing managed WAV import inside the final synchronous session gate.
The R6 adapter adds no UI or second save/Undo system. The Sample Lab surface and prepared audition
above retain those owners; musical semantics remain
with [Sample workflow](SAMPLE_WORKFLOW.md#r6-module-sources-and-candidate-boundary).

## Implemented Browser composition

`LeftPanelZone` composes an ordered collection of retained `SidePanelHost` controls between the rail
and musical workspace. The zone owns ordering, space allocation and reduced-window fitting; each host
owns one captured right-boundary draft, minimum-content clipping, near-edge collapse and usable-width
restoration. Following panels shift with layout; they do not share a resize target. New first-party
sidebars must reuse this path rather than implement independent drag/collapse policies. The bounded
collection is a concrete Browser/Patterns composition, not a general docking service.

Browser remains a workspace capability for visibility, activation, keyboard traversal and joined
content lifetime, marked `PaneDefinition.IsSidebar`. `WorkspaceHost` excludes sidebars from its canvas,
shelf and musical dock arbitration and delegates their focus to the zone. Old layout fields remain
readable; optional `navigation`/`browserPanel` placements retain widths outside music/Undo. Browser
content is created lazily and disposed by the zone after MainWindow's existing pending-work join.
Sidebar visibility survives empty/music layout transitions; document replacement still owns stale
Browser actions and audition cancellation. [Workspace](WORKSPACE.md#shared-left-panel-zone) owns gestures.

Desktop's retained Browser owns only navigation/list/selection and bounded worker directory cursors.
It does not decode entire folders or infer resource identity from paths/bytes. `DesktopWorkflow` captures
resource authority on its owner; workers inspect/decode one selected source. External normalization
reuses `AudioFileDecode`, then `AudioMedia.ReadTemporaryWave` validates transient float PCM without
project acceptance. `WavAudition` can borrow the existing solo renderer/output lifetime; Desktop retains
joined disposal authority. Preview refuses while transport plays and never edits canonical state.
Browser drag captures the file row at pointer press rather than borrowing later selection; whitespace
and folders cannot start a sample drag.

Rack acceptance composes the existing managed WAV import or `WavResourceReuse` with a musical-use
callback inside one document edit. `AudioResourceReuse` validates a captured accepted resource before
one independent Audio Clip use. `AudioMedia.Decode` dispatches legacy managed WAV to `ProjectMedia`
and normalized audio to its existing resolver, allowing reuse without conversion/copy. Existing
resource/sound/occurrence identities, media roots, integrity/history and Save As remain canonical.
Browser access preferences and waveform observations are outside project serialization. See
[Sample workflow](SAMPLE_WORKFLOW.md#implemented-seq-sample-browser) for bounded product behavior.

## Implemented standalone Audio Clip boundary

`AudioClip` is canonical Playlist intent, not a synthetic Pattern, Part or Rack item. Position/lane
are musical organization; offset, length and fades are native PCM frames. A shared SoundDefinition
identifies the imported source and its explicit Mixer assignment. Copies/split pieces share it;
Playlist lanes remain independent of Mixer channels. Gain/fades/range/repeats belong to occurrences.

Desktop's worker codec adapter normalizes supported sources to immutable managed float WAV. Core
owns portable metadata, validation, history, media integrity and prepared PCM/waveform derivations.
Windows Media Foundation is confined to compressed-source import, never the realtime callback.
Arrangement preparation leases each unique resource once, prepares bounded explicit local WSOLA
derivations when requested by AudioClip intent, and schedules unpitched PCM through the
existing sampler, and contributes it to the existing global Mixer. No parallel transport/mixing owner
or obligatory Channel Rack editor is introduced. Frozen preparation/export documents share the
document's weak PCM cache; prepared states own strong leases. Save As revalidates relocated bytes
before reusing those arrays. [Audio](AUDIO_ENGINE.md#standalone-audio-clips) owns limits/clocks;
[format](PROJECT_FORMAT.md#audio-clips-reader-14) owns persistence. A clip-local decimal ratio is
separate from resource, source sound/Mixer route and project tempo. Shared immutable derivations
inside one prepared plan do not make other clips inherit edits. Existing coordinator invalidation,
joined lifetime and frozen export remain the only execution owners; no parallel stretching player,
UI-thread DSP or callback preparation is introduced.

## Implemented project Mixer boundary

The project owns optional canonical `MixerState`, stable channels/buses, explicit SoundDefinition
assignments, validated output/send edges, strip parameters and Master settings. Mixer is independent
of Playlist track slots and Pattern placement: a sound can occur in several Patterns or simultaneous
clips yet feed one global channel. Initial explicit creation groups equal Part names as an editable
starting arrangement, never as hidden permanent identity. Unassigned sounds feed Master directly.

`MixerPreparation` projects the currently supported linear Source/Gain/Mix graphs into source
contributions while preserving their exact local transfer gains. Song combines audible occurrences
before compiling this projection. Each channel's stateful processing therefore executes once on its
aggregate, not once per clip. `MixerDsp` owns filter/compressor/Delay/Reverb state, routes, final protection and
bounded measured taps inside the existing sampler; this is not a second renderer or backend.
The existing graph operations also retain their node diagnostics. Stateful contributed graph nodes
will require an explicit source/processor boundary instead of assuming linear decomposition.

Canonical edits use the existing document owner, generation guards and Undo/Redo. Desktop owns only
gestures, context and immutable meter observations; prepared plans/runtime DSP/meters are not serialized.
Compatible control updates are owner-prepared, execution/authority-qualified and consumed at an audio
packet boundary without rebuilding media or voices. Topology changes use existing prepared publication
and retain compatible time/voice state. [Audio](AUDIO_ENGINE.md#project-mixer-execution) owns reset,
memory, bounded reachable Delay/Reverb tail extension, admitted Reverb banks and constant-time line ownership
transfer; [format](PROJECT_FORMAT.md#mixer-persistence) owns the reader gate.
Fast Mixer compilation refusals use the coordinator's same blocker/last-valid policy as worker
preparation. They never escape the already accepted document edit/Undo notification. Temporary
Sample Lab plan substitution preserves the original source completion and explicit hard-stop policy,
so a later Mixer publication cannot append its tail twice or turn a hard boundary into a natural one.

## R3-F1 desktop host and presentation ownership

`src/Seqvium.Desktop` is the one framework-dependent `net10.0` desktop executable. It explicitly adopts
Avalonia Desktop with Fluent base controls; its manifest/lock own versions. Core remains BCL-only and
does not reference presentation. Desktop references Core and, since R4-F3-A, the existing narrow
`Seqvium.Audio.Windows` adapter; never DeviceCheck or the R0 experiment. Launch creates no audio/device/
capture session and calls no playback service.

`App` owns framework startup and semantic resources; `MainWindow` owns bounded visual/chrome events.
The display-independent `Presentation/ShellSession` owns localized projection and preference commands;
its standalone document lifetime transfers to the integrated workflow below. It does not edit music.
`HostLocalizer` owns frozen
first-party stable dotted-key EN/RU catalogs and per-entry fallback. `HostPalette` owns Dark/Light roles;
`PreferenceStore` owns the independent versioned user JSON. No culture change, canonical discriminator,
document generation/history or media authority depends on translated strings or UI preference changes.
The owned SettingsWindow projects global preferences through ShellSession; it has a bounded page
composition, not another workspace pane or a document. HostCommands catalogs stable application actions,
defaults and reserved input policy; MainWindow resolves current bindings before invoking its existing
workflow commands. ControlPreferences contains an immutable override map and global behavior;
ControlPreferencesStore owns its separate bounded file, worker I/O and unsafe-original refusal.
ShellSession serializes changes and exposes joined persistence for application shutdown. ThemeCatalog
feeds both explicit Settings selection and the checked title-row menu. New global options and commands
must reuse these owners under [Settings](SETTINGS.md#unified-global-settings-center); no future
subsystem pages/providers or general keymap framework are preconstructed.

Language/theme apply immediately at completed button actions without replacing controls/window/document.
Numeric display is explicitly invariant; language is not a project parser or automatic name translator.

Windows chrome retains native window styles, resize/move roles and a small guarded OS system-menu
adapter; other platforms retain native decorations until tested. `WorkspaceRegion` hosts R3-F2's compact
`WorkspaceState` (pure placement/visibility/order), `WorkspaceHost`/`WorkspacePane` (retained controls,
pointer capture and local focus), and `FirstPartyPaneContent` (read-only document projection).
`WorkspaceLayoutStore` owns a separate user file and `WorkspaceLayoutPersistence`
joins/coalesces requested snapshots; `MainWindow` coordinates their shutdown with preference writes.
Pane instance/type, active pane, focused control, canonical selection and semantic edit target are
distinct. R3 panes add no native pane window, extension hierarchy or audio owner.
[Workspace](WORKSPACE.md), [settings](SETTINGS.md) and [UI design](UI_DESIGN.md) own exact behavior/resources;
[F1 report](experiments/SEQ-R3-F1_REPORT.md) owns observed bounds.

### Desktop process ownership

Desktop's [Lifecycle](../src/Seqvium.Desktop/Lifecycle/DesktopInstance.cs) boundary owns a user-scoped
named Mutex and local activation-only Named Pipe before framework/document initialization. The
entry-point thread holds/releases the lease; cancellation and joined listener teardown own IPC.
The user-specific identity and .NET current-user access controls isolate different users. Scope is
user-wide across logon/shell sessions because preferences/layout are user-wide; do not allow a separate
ordinary process in another session to write the same configuration independently.

Activation waits for the primary MainWindow and dispatches one ordinary Show/restore/Activate request
on the GUI thread. OS foreground restrictions remain authoritative. A bounded failure exits without
another GUI/document/audio owner. Kernel lease release/abandonment permits startup after a crash,
without lock-file cleanup. Protocol v1 accepts only activation, never project replacement or playback.
Future project-opening IPC must retain the existing Save/Discard/Cancel application lifecycle.
Single Instance provides no exclusion for external writers of project files.

Only the separate friend [VisualCheck](../tools/Seqvium.VisualCheck/README.md) host composes an
isolated identity and preferences/layout/media directory. The production entry point accepts no
environment/command-line bypass. Tests, DeviceCheck, ProjectStats and experiments do not invoke
Desktop ownership by referencing the assembly. Linux/macOS remain runtime-unevidenced under
[portability](PORTABILITY.md).

### R4-F3-A integrated document/audio owner

`Workflow/DesktopWorkflow` is the narrow host application/session boundary over the same canonical
document, R2 media/persistence and R4 coordinator/sampler. `WorkflowOwner` serializes mutations,
notifications, preparation completion and transport on one non-GUI synchronization context. Disk I/O,
decode/preparation and blocking physical joins stay off the UI thread. Immutable observations feed
ShellSession, retained workspace content and `WorkflowControls`; views own no PCM leases or devices.
`DesktopOutput` alone composes Windows endpoint discovery and WASAPI: an explicit Play resolves the
current default output, opens that exact endpoint and validates its negotiated format without fallback.
Only coordinator Start plays. Canonical state contains no endpoint/runtime/UI data.

`MainWindow.Workflow` owns native pickers, action/focus gating and unsaved decisions. Imported resource,
sound, musical use and item graph are accepted in one existing ProjectDocument Edit through R2's
optional acceptance callback. No second document, Undo, preview Gain or audio engine exists.
Open validates a candidate before touching the current session; Cancel/failed Save retain current work.
Replacement/close stops intent, cancels/joins preparation, joins physical consumers, retires/releases
prepared PCM through AudioDeviceSession, then closes/replaces the old document. Failed join retains
ownership for retry. Reopened documents have fresh lifecycle and no execution; explicit Play is required.
New uses the same unsaved-decision and joined release boundary, then creates a pristine canonical
ProjectDocument. Session PatternEditorNavigation owns editor/Part preferences per Pattern; neither
project persistence nor WorkspaceState owns these targets. MainWindow retains the last musical layout
while showing the empty-document Rack, so empty startup/New does not overwrite useful musical geometry.
Close withdraws pending Start intent before entering the command gate, but keeps existing playback
until Save succeeds. A failed Save keeps the document, output owner and Browser directory cursor.
[Sample workflow](SAMPLE_WORKFLOW.md#r4-f3-a-desktop-import-and-musical-use) owns initial musical policy;
[audio](AUDIO_ENGINE.md#r4-f3-a-desktop-execution-composition) owns the execution limits.

## R5-F1 musical editor composition

SEQ-RACK-WORKFLOW-F1 adds `ProjectEdit.DuplicatePartIndependent` and `MovePart`, invoked through
generation/lifecycle/selection-checked DesktopWorkflow channel commands. Duplication copies supported
immutable intent to fresh sound/Part/note/zone identities and wires the existing Rack tree in the same
Edit. Explicit Part routes retain their destination with independent performance keys; Mixer assignment
retains the original aggregate. Automation lanes keep their original stable targets. Managed media
and history lifetime stay with their existing owners. Unknown identity-bearing copied content refuses.

Parts array order is durable Rack presentation. Audio equivalence normalizes this order independently
of canonical equality, so reordering can acknowledge an equivalent revision without preparing a new
plan or resetting voices/time. Compatible worker continuation also ignores row order when other edits
are pending. The toolbar fader reuses GainLevelControl capture. DesktopWorkflow owns a uniquely
identified gesture with frozen lifecycle/generation/attachment/node and starting amplitude, independent
of the ordinary Busy command pulse. GraphExecutionCoordinator validates its active prepared execution;
RealtimeSampler receives immutable latest parameter tables qualified by execution, authority and Stop.
OfflineSampler temporarily overlays these on its accepted coefficients/Mixer parameters while retaining
voices and FX histories. Cancellation withdraws the overlay; completion uses one existing document edit.
Neither the control nor the overlay owns a document, history, device gain or alternate renderer.

`Workspace/BeatPaneContent` is a retained projection of the selected occurrence's shared Pattern,
parts and notes. `Workflow/BeatSteps` supplies exact grid projection/refusals and frozen lifecycle,
generation, attachment and Pattern authority; it stores no editable steps. `DesktopWorkflow.Beat`
serializes creation, channel acceptance, name and note edits on the existing owner. Core adds only
`ProjectEdit.RenamePattern`, preserving the same Pattern and unknown fields. Creation supplies a
real Placement/item context/attachment; channel import uses the existing media acceptance callback.
No musical schema, history, PCM store or dynamic pane registry is added. R5-F2/F3 now extends the
same prepared execution with continuous looping and compatible live note publication, rather than
adding another engine. The control owner computes continuation mappings; the consumer only borrows
prepared tables and fixed voices. Pattern length uses existing canonical duration; channel deletion
repairs known topology and references atomically. PatternWaveExport freezes state/media roots and
renders through the existing graph off-thread without a device or history operation.
[UX](UX_CONTRACT.md#implemented-r5-f1-step-sequencing) owns grid scope; [graph](NODE_GRAPH.md#r5-f1-channel-composition)
owns safe automatic wiring. Existing audio preparation/publication and joined lifetime remain unchanged.

SEQ-UI-REDESIGN adds retained rail/navigation projections, palette-derived HostMaterials and a captured
GainDialControl sharing the existing Node Editor draft owner. Pattern thumbnails read canonical note
positions; they retain no editable music. Rack row painting stores only an ephemeral preview/frozen
basis. SetBeatStepsAsync copies submitted cells, revalidates authority/unsupported data, preserves
unaffected note identities and commits one existing ProjectDocument transaction. SetTempoAsync edits
only canonical ProjectSettings, preserving musical-time positions and stopping the finite execution.
WAV drop goes through the existing import/media acceptance owner. No DSP, scheduling, schema,
serialization marker, executable graph, realtime thread or second document/history owner changes.

## Playlist composition

### Song structure and explicit variations

`ArrangementSection` is a stable named/color musical range, separate from processing containers,
Playlist lanes and Mixer channels. Sections cannot overlap. Moving a section swaps it with its
adjacent section, preserving the intervening gap; duplicating inserts shared clip uses immediately
after the range and shifts later music/labels by its length. Both operations inspect every Pattern
and Audio Clip boundary and refuse an unsplit crossing before publication. Label deletion removes
only organization. Core owns these transformations and their validation in `ProjectEdit.SongStructure`.

Pattern and Audio Clip selections use one transaction, common relative movement/clamping, clipboard
geometry and duplicate span. Audio copies retain source-frame ranges, envelope clocks, fades, gain
and shared Mixer assignments. A copied Pattern clip retains its placement reference and opaque data;
there are no hidden note copies. Clipboard/view state remains Desktop session data.
Section-only organizational edits publish an equivalent revision without preparing PCM or resetting
transport. Musical restructuring still rebuilds scheduling and maps only compatible voices.

Make variation is an explicit exception to shared placement reuse: it clones a supported placement's
item context and built-in graph with fresh context/node/port/connection/binding identities, then uses
the existing Pattern variation remapping and retargets only the selected clip. New parts/notes have
fresh identities; sound/resources, containing context, routes and performance intent remain shared.
Unknown identity-bearing Pattern/placement/graph/context intent refuses detachment without mutation.
`Pattern.VariationOf` records informational ancestry, without note inheritance or source-deletion
ownership. Deleting an ancestor leaves the independent variation valid. Reader 1.5 owns persistence;
the renderer continues to consume ordinary canonical Patterns, placements and clips.

`ProjectEdit.MakeArrangementRepeatUnique` decomposes a repeated clip at a zero-based cycle index
inside the same document transaction. It uses actual Pattern ticks and calls MakeArrangementVariation
for the one-repeat center. Empty remainders are omitted. The original Clip ID stays with the left
shared remainder, or with the right shared remainder when detaching cycle zero; the center always
gets a fresh Clip ID. Other remainder IDs are fresh. All pieces keep their lane and contiguous
absolute positions. Undo restores the exact original snapshot; Redo restores the accepted identities.
There is no new clip kind, musical-time owner or media copy.

The owner authorizes a bounded Arrangement workflow independently of closing intervening roadmap
stages. Core's ArrangementClip references an existing source PatternPlacement, rather than manufacturing
more Rack placements or invisible Pattern/Part/Note copies. Its source owns canonical music/graph/media;
the clip owns only song position, fixed spatial lane and repeat count. Optional identified
ArrangementTrack configuration names/colors/gates one of 32 fixed lane slots, with no permanent
instrument assignment, processing context or Mixer identity. Full dynamic containers remain future work.

ProjectEdit.Arrangement uses the same document validation, equality, Undo and persistence; source
deletion removes referring clips atomically. DesktopWorkflow.Arrangement freezes lifecycle/generation
and serializes all clip/track commands on the existing owner. Selection of a Rack source during Song
is separate from audio intent. Mode changes explicitly stop and select the coordinator's finite Song
or continuous Pattern path; Open restores intent silently. There is no second transport/history owner.

ArrangementPreparation derives absolute note occurrences and one combined program from eligible source
graphs, reusing the existing offline/realtime processor, PCM leases, publication/retirement and output
lifetime. Linear stateless programs may process summed independent voices of their shared source;
future stateful per-occurrence processors must derive distinct execution ownership deliberately.
Song Mute/Solo affects scheduling, not canonical notes or the Rack loop. Compatible publication maps
stable occurrences into new tables; no UI timer supplies musical time.

ArrangementPaneContent/ArrangementCanvas and ArrangementOverview are retained projections with local
gesture/view state. Source choice, clipping/note preview, stationary headings, track settings, snap,
fit and overview use shared host resources. WorkspacePane participates in refresh/focus/cancel/Dispose.
Song export freezes document/media roots and invokes the same program off-thread with no device/history
mutation. [UX](UX_CONTRACT.md#implemented-playlist-arrangement),
[audio](AUDIO_ENGINE.md#playlist-song-preparation-and-execution) and
[format](PROJECT_FORMAT.md#playlist-reader-12) own the exact boundaries.

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
separate. F2 derives execution/resources from a frozen eligible revision; F3-B supplies the canvas.

### R4-F3-B graph presentation and commands

Desktop's retained `Workspace/GraphPaneContent` and `GraphCanvas` draw/handle input on the GUI thread;
the pure `GraphEditorSession` owns explicit target, selection and move proposal, while `GraphGeometry`
owns common draw/hit geometry and viewport transforms. They project an immutable workflow observation,
never an independently editable graph. Refresh adopts one coherent document observation before resolving
graph/diagnostics, including shell-triggered refreshes arriving before queued pane notifications.
`DesktopWorkflow.Graph` validates lifecycle/generation/attachment on the existing owner and calls
existing ProjectEdit operations. All audio preparation/publication/resource lifetime remains F2-owned.
Native pickers/cancellation remain MainWindow-owned. Second-source durable acceptance creates independent
music and graph binding atomically, with shared-Pattern refusal
under [sample workflow](SAMPLE_WORKFLOW.md#r4-f3-b-add-wav-to-current-item).
The only new Core edit is validated Pattern length adjustment for a longer accepted source; no new
graph model/history, DSP, assembly, plugin registry or docking framework is introduced.

## Built-in instrument ownership

The polyphonic subtractive synth uses the same SoundDefinition, canonical notes, Source bindings,
prepared plans, fixed voice slots and transport. SynthVoice owns validated versioned parameters,
factory snapshots and worker-prepared coefficients; each OfflineSampler voice owns independent
phase/filter state, never serialized. SynthPaneContent edits an explicit bound Source through
generation-checked workflow commands in the retained Node Editor. Sharing, independent copies,
history and portable Save/Open retain existing
owners. [Audio](AUDIO_ENGINE.md#polyphonic-subtractive-synth-seq-synth-f1)
specifies execution, budgets and cold-seek limits. Continuous held Synth preview reuses this owner:
DesktopWorkflow validates captured target/generation, prepares immutable parameters and publishes
through RealtimeSampler's bounded audition mailbox. SynthPaneContent coalesces outstanding draft
updates and submits one canonical edit on completion. Drafts do not enter Save, recovery, history,
canonical graph preparation or R8. Source removal joins the withdrawn preview at command completion.
Short Synth commands retain the shared command gate without broadcasting a workspace Busy pulse.

The seven-voice Drum family is a built-in SoundDefinition capability, separate from the removable R7 creative generator.
Canonical decimal parameters and algorithm identity use existing document history/serialization.
`DrumVoice` retains legacy kick synthesis/envelopes; `DrumInstruments` owns family descriptors,
presets and new synthesis, and `DrumChoke` derives prepared hat lifetimes. `BuiltInSound` recognizes built-in sound
capabilities without introducing a public registry. Derived host-rate signals enter the existing
source/lease scheduler under [Audio](AUDIO_ENGINE.md#built-in-drum-voice).

Drum editor Play also owns a bounded transient source layer in that same RealtimeSampler callback,
after musical Master. Desktop borrows the existing musical output when present; otherwise its joined
solo owner opens one consumer. Immutable worker-rendered mono PCM, eight fixed preview cursors and
eight command slots do not borrow graph publication or musical transport authority. Source gain is
unity; linked preview protection uses remaining output headroom without altering music. Stop/hide/
retarget/removal cancels preview intent independently; global Stop and joined output/project teardown
include both layers. No preview note, resource, history, graph route or second device stream is created.
The [audio contract](AUDIO_ENGINE.md#drum-play-along-and-source-preview-levels-seq-drum-playalong-f1)
owns precise bounds, Master bypass and level semantics; contextual Sample Lab remains separate.

The bound Source node opens `DrumVoicePaneContent` in the retained Node Editor. `DrumEditorSession`
captures lifecycle/attachment/graph/node plus sound ID, generation and starting values; workflow commits
revalidate all of them. Controls edit the shared sound, not a graph-local duplicate or physical voice.
Any intervening generation cancels drafts, including ABA; exact deletion Undo can restore the target,
while project replacement retires its lifecycle. Preview curves/waveforms and audition controls are
session presentation, not canonical music. Rack and Piano retain the only note model.

## Node editor state and lifetime proposal

**SEQ-NODE-EDITOR-IMPL-F1 — implemented internal Desktop boundary accepted for the first slice.**
[Graph](NODE_GRAPH.md#full-node-editors-proposal) owns editor capability and
[Workspace](WORKSPACE.md#node-editor-placement-proposal) owns the reusable pane and future alternatives.

`NodeEditorSession` is separate from `GraphEditorSession`: the graph session keeps graph
selection/viewport/move intent; the node session holds an explicit target, immutable observation,
local view context and at most one transient edit proposal. `NodeEditorPaneContent` resolves actual
supported type/state-version UI or host-owned details; `MainWindow` owns open/activate requests.
Dispatch is explicit and built-in, without a universal parameter schema, registry, new assembly or
public factory ABI. The controls own pointer/text presentation; the workflow owns commits.

### Identity and observation

Use logical target `(document LifecycleId, attachmentId, graphDefinitionId, nodeId)` for the current
bounded graph. Resolve all IDs and attachment-to-definition ownership from one coherent immutable
`WorkflowState.Snapshot`; verify supported type/state version and parameter meaning before editing.
Names, array indices, coordinates, object references, selected occurrence, saved path and prepared
execution IDs cannot replace this identity. Graph ID detects attachment reassignment. Generation is
a **commit precondition**, not part of the stable editor target; Undo/Redo can retain the target while
advancing generation even when the same snapshot revision returns.

At draft start capture this target, current generation, supported capability and starting parameter
value. Submit a frozen command to the existing serialized `DesktopWorkflow` owner, which revalidates
before `ProjectDocument.Edit`. Initially use the conservative whole-generation gate already employed
by graph edits; any intervening canonical Edit/Undo/Redo invalidates a draft, including unrelated
changes. Dependency-specific reconciliation can come later with evidence; identity alone is not a
commit gate. Own command progress/acknowledgement separately from draft cancellation, so a Busy
observation or delayed GUI callback cannot resubmit or redirect an already captured command.

| Event                                                                                 | Implemented session behavior                                                                                                                                                                                         |
|---------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Parameter edit, Undo or Redo while target exists                                      | Re-resolve current node and refresh controls; cancel stale drafts/gestures. Stable target survives replacement immutable records; old commands remain invalid even after Undo/Redo ABA                               |
| Node/graph/attachment/owning placement deletion                                       | Keep target key as unavailable in the same lifecycle for possible Undo; disable mutation, cancel drafts and release node-specific active resources. Do not follow workflow's fallback playback selection             |
| Undo restores the exact target                                                        | Re-resolve ownership/capability and recreate presentation as needed; restore current values, never cancelled input, native handles or old pending commands. Redo deletion returns to unavailable                     |
| Graph pane target/global selection/playback changes                                   | Keep node target. Opening a different node explicitly cancels the old draft and replaces target; repeated same-target open only activates                                                                            |
| Attachment points to another definition, node type/state version becomes incompatible | Show unavailable/unsupported details; never redirect to a different same-name/type node or reinterpret old parameter input. Revalidate exact restoration normally                                                    |
| Save or Save As                                                                       | Preserve logical target; save canonical state only. No target/draft/native handle is serialized                                                                                                                      |
| Open cancelled, candidate load/Save fails                                             | Keep current document/session target; any cancelled draft stays cancelled. Existing dirty-decision and join behavior remains authoritative                                                                           |
| Successful document replacement or close                                              | Withdraw commit authority, cancel proposals/open requests, detach subscriptions and release old observations/node presentation. Clear target even when reopened IDs/path match; new lifecycle needs explicit opening |
| Hide/collapse or host shutdown                                                        | Cancel transient editing, retain only safe session view/target on hide; dispose subscriptions/content on shutdown. A queued GUI refresh checks disposal/current lifecycle before access                              |

Retained chrome may outlive node-specific content. Do not retain a deleted node or old document just
to preserve an editor; retain the key and safe presentation context. Deletion Undo may restore the
same key but cannot revive invalidated asynchronous work. These rules extend the existing coherent
refresh discipline, rather than reading mutable `ProjectDocument` fields across threads.

### Parameters, history and prepared execution

```text
Quick controls / detailed editor / future supported plugin input
    -> captured edit intention -> DesktopWorkflow owner -> ProjectDocument.Edit
    -> one canonical snapshot -> immutable observations -> all editor projections
                             -> existing validation/preparation -> prepared DSP
```

Canonical node parameters/project-owned state remain the only durable editable truth. UI drafts,
derived display units, analytical curves and tab/scroll state are presentation; existing document
history and Save remain authoritative. Reuse `SetGraphGainAsync` and Core validation for Gain. One
changed completed gesture/text commit produces one logical edit, with no-op/cancel/stale producing
none. Opening/closing/resize/focus creates no musical history. No editor-specific Undo or DSP copy.

The frozen `NodeGainProposal` includes the full logical target, captured generation and starting
amplitude. The `SetGraphGainAsync` overload checks definition ownership, supported Gain v1 semantics,
starting value and legal `[0,1]` result on the same workflow owner before the existing edit. The
level control captures its own pointer; release commits once, while loss/Escape cancels. Text uses
synchronous `TextProperty` notifications under the refresh guard: Avalonia's deferred `TextChanged`
event must not turn a canonical refresh into another draft.

Source details use existing bounded `SourceAccess` discovery: storage is captured on the workflow
owner, inspected off the GUI, and published only while lifecycle/target/generation/cancellation still
match. Hiding or retiring content cancels publication. The worker owns only frozen read context and
temporary decoded buffers, not live document authority, a processor or an editor-local mutable graph.

The shell's document observation also compares lifecycle, not only snapshot revision/path/dirty state:
reopening the identical saved snapshot must adopt the new document rather than retain its retired owner.

Use observations for every accepted change, including quick controls and Undo/Redo. Refresh idle
controls without invoking their user-change commands; guard against notification feedback loops.
Conflicting external changes cancel draft input and refresh canonical values with feedback. Status
updates without a new generation need not cancel it. Do not display an editor's target as executing
merely because another attachment is playing; show execution divergence only for matching scope.

Future automation/modulation must distinguish canonical base value, accepted automation intent and
derived effective runtime value. Runtime feedback can update a separate labelled read-only overlay
at a bounded UI rate; it must not write sampled effective values into the base parameter, Save or
Undo. Native user gestures eventually need explicit begin/end intention and owner validation, whereas
host-to-plugin echoes/runtime notifications are not new edits. This reserves responsibilities, not
automation composition/rates, smoothing, state exchange or an ABI. Q-033/Q-034/Q-063 remain open.
Editor views own neither prepared execution nor PCM/device lifetimes; all publication and retirement
remain under [Audio](AUDIO_ENGINE.md#r4-f2-independent-contributions-and-realtime-convergence).

## R4-F2 execution and application coordination ownership

[GraphPreparation](../src/Seqvium.Core/Audio/GraphPreparation.cs) derives bounded immutable operation,
contribution and coefficient tables from canonical attachment intent plus verified PCM. The existing
`PreparedSampler`/`OfflineSampler` hold plan/leases and one mutable consumer's voice/scratch state;
graph-free Pattern and transient audition retain their explicit guarded preparation paths. R2's
`RealtimeSampler` owns graph publication, revisions, borrowing and retirement too. There is no second
engine, graph document, runtime history, package/assembly or Windows-owned musical processing.

[GraphExecutionCoordinator](../src/Seqvium.Core/Audio/GraphExecutionCoordinator.cs) is the minimal
application owner: post-transaction document notifications, latest desired target, one worker/candidate,
supersession/cancellation, capacity retry and thread-safe divergence/status. Its supplied serialized
owner context must continue servicing completion/progress through shutdown. Preparation workers see
frozen snapshots/media roots only; callbacks see prepared arrays/voices and atomic control values only.
Geometry/Gain equivalence is conservative across canonical content/roots, not a general async rebase.

`AudioDeviceSession` can own an attached coordinator for one output. Its asynchronous shutdown joins
preparation before stopping/joining the device and releasing the borrowed sampler. The existing lifetime
interface and concrete WASAPI sampler seam remain sufficient. Failed join retains ownership; callers
must keep the owner context alive and retry. The bounded F3-A desktop composition is described above.
[Audio](AUDIO_ENGINE.md#r4-f2-independent-contributions-and-realtime-convergence) and
[Node graph](NODE_GRAPH.md#implemented-r4-f2-prepared-item-local-execution) own exact limits/semantics.

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

## SEQ-R9 shared note editor ownership

Core `ProjectEdit.SetPartNotes` atomically replaces one explicit Part's notes under normal document
validation/history; `RenamePart` and shared sound parameters retain their existing owners. Desktop
`PianoNotes` owns pure snap/group transforms and safe-edit eligibility, while `DesktopWorkflow.Piano`
owns serialized lifecycle/generation/Pattern/attachment authority and survivor-order normalization.
Canvas drafts, selection, viewport and clipboard are transient projections; accepted notes are never
an editor-private music store. Rack safely projects richer notes as a read-only melody; Playlist
continues referencing the same Pattern and rendering actual NoteEvents.

`PianoNotes.Quantize` uses the existing Snap over selected starts with an atomic Pattern-end check.
`DesktopWorkflow.QuantizePianoNotesAsync` validates the same basis/Part/sound authority and commits
one `SetPartNotes` edit in surviving order; no-op/history/live publication retain the existing owners.
`PianoNotes.Humanize` is a bounded pure transformation with an explicitly supplied Random for seeded
and endpoint tests. The serialized `HumanizePianoNotesAsync` command freezes selected IDs, validates
the existing basis/Part/sound and creates a fresh local Random only on explicit Apply. Equal selected
onsets share a boundary-clamped offset; intensity is sampled per note. One `SetPartNotes` edit owns
no-op equality, history, persistence and existing live publication. The retained flyout owns only
strength drafts and frozen target/selection, expires on invalidation and survives transport-only
observations. No seed, settings, random generator service, schema or alternate music owner is added.
`Presentation/PianoScale` is a pure session view guide held by the retained Piano canvas. It has no
canonical document, sound-root, serialization, export or execution dependency. Canvas row backgrounds
consume it before grid/ghosts/notes; lifecycle replacement resets the guide without an edit.

Optional `SampleRootMetadata` reads bounded, exact-path/hash-verified import metadata without writing
the library. Root remains an explicit shared SoundDefinition parameter. `NoteAuditionRequest` owns
frozen source preparation, using the existing PCM kernel; the workflow joins its transient renderer/
physical borrower before transport changes/close. No new playback engine, history or project format.
[UX](UX_CONTRACT.md#implemented-seq-r9-piano-roll), [audio](AUDIO_ENGINE.md#seq-r9-pitched-note-audition)
and [sample workflow](SAMPLE_WORKFLOW.md#seq-r9-source-root-pitch) own observable/runtime details.

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
and local clip stretch are distinct operations. Audio Clips implement fixed/source time with an
explicit 0.5–2 local duration multiplier and worker-prepared WSOLA; default 1 retains original PCM.
Follow project tempo remains a future capability with no dormant control. Broader warp/tempo-map
representations remain open. [UX_CONTRACT](UX_CONTRACT.md#audio-timeline-editing) owns distinct trim,
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

## Implemented Playlist automation ownership

The bounded Playlist automation owner is `ProjectState.AutomationLanes`, edited through the same
ProjectDocument transactions and frozen DesktopWorkflow authority as clips. A lane has one typed
SoundDefinition or MixerChannel identity and absolute Song-time points. Neither Playlist lane number,
name nor current routing can substitute for that target. Missing or incompatible targets retain their
envelope as unresolved; Undo can restore the original identity. No target search by equal name occurs.

Song preparation compiles bounded frame breakpoints/physical lookup tables into PreparedSampler.
The existing OfflineSampler applies cutoff to its fixed synth voices and volume to the identified
aggregate Mixer fader. Compatible publication transfers oscillator/filter/envelope ages and Mixer
state; the GUI supplies no control clock. Base settings, Pattern execution and local R8 sampling
remain independent. No generic modulation matrix, node connector, external processor automation or
second engine/history owner is introduced. [Audio](AUDIO_ENGINE.md#musical-parameter-automation)
and [format](PROJECT_FORMAT.md#parameter-automation-reader-112) own execution and persistence.

## Future parameter control

Parameter modelling must leave room for wider control rather than treating DSP parameters as
permanently fixed primitive values. Beyond F1, an effective parameter may combine base/user value,
timeline automation, modulation, and envelopes/LFO/control-graph sources. This requirement does not
select a general composition abstraction. Wider replace/add/multiply semantics, domains,
precedence, smoothing and control rates remain unresolved outside the bounded implementation above.
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
owns its supplied content and availability; labels do not establish ownership. General resource identifier syntax
and APIs remain open in Q-054/Q-055/Q-067. R6 implements only
bounded package/capability identity, duplicate-package rejection and host-rendered metadata above.

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
