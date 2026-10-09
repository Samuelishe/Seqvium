# Project format

Role: Serialization compatibility and resource-preservation contract.
Read when: Designing save/load, migration, managed media, or extension-state persistence.
Authoritative for: Compatibility direction, canonical Save/reopen, recovery-state and media integrity,
opening/migration, versioned persistence and unknown-data preservation.
Not authoritative for: A final container/schema, runtime domain classes, extension API, or recovery implementation.

SEQ-R1 selects the bounded canonical JSON format below; R2-F1 adds associated managed WAV directories.
R4-F1 adds graph-aware reader minor 1 and canonical graph intent without changing the product UI version.
Recovery, broader packaging and later migrations remain open; this is not a final complete-project container.

## R1 canonical JSON format

UTF-8 JSON has `format: "seqvium-project"`, integer `major: 1`, integer `minor: 0`,
`minimumReaderMinor: 0`, `writerVersion`, `revision` UUID and `state`. State contains project identity,
nullable name (unnamed), tempo/meter settings and explicit entity arrays. IDs are UUID strings; positions
and durations encode Int64 tick counts directly as JSON integers. Readers must retain Int64 precision,
not parse them through binary floating point. Tempo encodes decimal quarter-note BPM. Arrays preserve canonical
order; notes at equal positions order by part UUID then note UUID, independently of serialized order.
Saved revision is distinct from project identity. Save As preserves identity; explicit document-fork
work is deferred. Paths and saved association/history/lifecycle/generation are not serialized.

Known property names use camelCase. All constructor fields are required, including nullable fields
whose value may be JSON `null`; empty collections are explicit arrays/objects. The exact bounded
state shape follows [model records](../src/Seqvium.Core/Domain/ProjectModel.cs) and
[codec](../src/Seqvium.Core/Persistence/ProjectPersistence.cs):

| Object                      | Known fields                                                                                              |
|-----------------------------|-----------------------------------------------------------------------------------------------------------|
| State                       | `id`, `name`, `settings`, `patterns`, `sounds`, `placements`, `groups`, `resources`, `contexts`, `routes` |
| Settings / meter            | `tempo`, `meter` / `numerator`, `denominator`                                                             |
| Pattern                     | `id`, `name`, `length`, `parts`                                                                           |
| Part                        | `id`, `name`, `soundId`, `notes`                                                                          |
| Note                        | `id`, `position`, `duration`, `pitch`, `intensity`                                                        |
| Sound                       | `id`, `name`, `algorithm`, `parameters`, `resourceIds`, `extension`                                       |
| Resource                    | `id`, `name`, `managedLocator`, `origin`                                                                  |
| Instrument Group            | `id`, `name`, `soundIds`                                                                                  |
| Placement                   | `id`, `patternId`, `position`, `itemContextId`, `containingContextId`, `routeId`, `partRelationships`     |
| Placement part relationship | `partId`, `sharedPerformanceKey`, `routeId`                                                               |
| Context                     | `id`, `name`, `level` (`item` or `containing`), `intentionalMix`, `extension`                             |
| Route intent                | `id`, `name`                                                                                              |
| Extension state             | `extensionId`, `stateVersion`, `payload`                                                                  |

Pitch is decimal semitones in [0, 127], permitting fractional pitch; intensity is decimal [0, 1].
Sound parameters are a string-to-decimal map. Notes must fit their Pattern length; a placement end must
fit Int64 time. UUIDs are globally distinct across identified entities; part relationships refer only
to parts of that placement's Pattern. A non-null performance key requests interaction across the
identified sound uses without defining runtime instances. `additionalData` is a reserved codec-internal
name, never emitted as a format field. Extension payloads and optional unknown values are opaque JSON.

Only major 1 is understood. Higher compatible minor documents are retained at their original minor
when `minimumReaderMinor <= 1`; unsupported major/required minor is refused. Graph-free 1.0 remains
supported; the graph-aware gate below applies to meaningful graph intent. No legacy migration is
claimed. Unknown JSON properties at envelope, state, settings and every entity/nested part/note are
preserved semantically at the same object boundary, including through edits/Undo/Save. Reserved known
fields cannot be overwritten by extension data. Unknown properties are optional by version contract;
essential new semantics must raise the minimum reader or major. Opaque extension state retains an
extension identifier, positive state version and arbitrary JSON payload without activation. Missing
declared extensions or resource availability yields explicit degraded-access diagnostics while retaining
editable canonical state; no audio execution is provided by this reader.

Malformed JSON, duplicate properties/identities, missing required fields, invalid values/references or
contradictory essential relationships refuse the document rather than guess intent. A missing media
artifact differs from a dangling canonical resource ID: the descriptor remains interpretable in the
former case. Limits are 16 MiB UTF-8 JSON, depth 64, 100,000 total entities and 4,096-character names/
identifiers/locators. These initial bounds can evolve deliberately; they are not realtime capacities.
Resource descriptors carry an optional portable relative managed locator and provenance only; R1
alone neither imports bytes nor validates codec/media integrity; F1 supplies those boundaries below.
No external-reference import is adopted.

## R4-F1 graph-aware JSON format

The reader supports major 1, minor 1. `state.graphs` and `state.graphAttachments` are optional on read,
defaulting to empty arrays for historical R1 files; explicit null/invalid collections refuse. Current
writers emit both arrays. A graph-free legacy document keeps its 1.0 envelope and musical meaning.
Any nonempty graph/attachment array, including unsupported, unattached, incomplete or cyclic intent,
requires `minor >= 1` and `minimumReaderMinor >= 1`. Encode raises both as needed; Read refuses
underdeclared graph semantics. The actual baseline 1.0 envelope gate refuses minimum reader minor 1
before silently ignoring the new fields. Compatible higher minor/unknown fields remain preserved.
Loaded requirements never decrease; successful graph Save retains the promoted requirement in-session,
even after graph removal. Encode/failed Save cannot mutate compatibility, current or saved state.

Graph records follow [GraphModel](../src/Seqvium.Core/Domain/GraphModel.cs); constructor fields below are
required, including nullable `extension` and `outputNodeId`. All entity/reference identities are typed
UUID strings and entity IDs join the existing globally distinct identity set.

| Object | Known fields |
| --- | --- |
| Graph definition | `id`, `nodes`, `connections` |
| Graph node | `id`, `type`, `stateVersion`, `parameters`, `ports`, `position`, `extension` |
| Graph port | `id`, `role`, `direction`, `signalClass`, `use`, `layout`, `ratePolicy`, `cardinality`, `required` |
| Graph connection | `id`, `fromNodeId`, `fromPortId`, `toNodeId`, `toPortId` |
| Graph attachment | `id`, `graphId`, `contextId`, `outputNodeId`, `sources` |
| Source binding | `id`, `nodeId`, `placementId`, `partId`, `boundary` |
| Graph position | `x`, `y` |

`parameters` maps stable keys to retained JSON values. Gain v1's `linearAmplitude` is a decimal linear
amplitude ratio with default 1 and eligibility range `[0,1]`; future amplification is a versioned
capability decision, not prohibited. Positions are finite double values within +/-1,000,000 graph
units. `type`, role/signal/use/layout/rate/boundary are extensible strings; `direction` is `input`/`output`,
`cardinality` is `single`/`fanOut`. Supported capability/port/source rules belong to
[Node graph](NODE_GRAPH.md#implemented-r4-f1-canonical-graph-intent), not the saved declarations.

Unknown properties survive at definition/node/port/connection/attachment/binding/position and extension
boundaries, including known-field edits and Undo/Redo. Parameter values and extension payloads stay opaque
without reinterpretation; domain nodes own cloned JSON lifetimes. The codec recognizes formerly unknown
graph fields directly on read, avoiding a shadow unknown copy. Reserved known/unknown collisions refuse
encoding rather than emitting duplicate properties. Meaningful new essential semantics still require
a suitable minimum reader/major decision; unknown fields do not confer executable capability.

Duplicate entity IDs, missing required fields, malformed/empty UUIDs, unsafe ownership/cardinality,
invalid coordinates and excessive shape refuse structural acceptance. Attachments require existing
context and independent graph owners, at most one attachment per context/definition. Nonempty unresolved
processing endpoint/source/output references are retained with execution blockers. Distinct-ID duplicate
edges, incompatible ports, bad Gain, disconnected inputs, cycles and unavailable capabilities remain
savable. Load and Save reports supply machine-readable `GraphReports`; `IsDegraded` includes their blockers separately
from resource/extension diagnostics. Reports establish intent eligibility only; media availability and
actual execution preparation remain separate gates. Save persists current invalid intent, never a plan.

Existing 16 MiB/depth-64/text/global-100,000-entity limits remain; graphs, nodes, ports, edges, attachments
and bindings count as entities. A node may declare at most 256 ports. F1 processing eligibility limits
(32 nodes, 64 edges, 8 sources, 8 Mix inputs) are smaller than document limits and do not refuse Save.
Int64 musical time, media-reference semantics and normal-process file replacement guarantees remain
unchanged; no DSP state, buffers, device endpoints, workspace preferences or history are added to JSON.
Caller-supplied sets of validated available resource/extension identities can suppress the corresponding
load diagnostics; that is not executable compatibility negotiation or proof of playable audio.

The file adapter serializes a captured snapshot, writes an exclusively created sibling temporary
file, flushes it and replaces the destination before marking that snapshot saved. Serialization/write
failure leaves current history and saved association unchanged; owned temporary files are cleaned up.
This is bounded normal-process failure safety, not proven crash consistency/durability on every OS,
rolling recovery or general media transactions. F1 extends this adapter for bounded media Save As below.
Streams provide encoding/read
without implicitly marking saved. History is session-only. No runtime/UI/native/prepared state is stored.

## R2-F1 managed WAV layout

JSON remains major 1/minor 0, retaining compatible loaded higher minors and all R1 identities,
unknown properties and opaque extension data. F1 needs no structural schema migration: existing
`ResourceDescriptor.managedLocator` is `wav/<random UUID N>-<SHA-256 lowercase hex>.wav` for accepted
WAV. UUIDs distinguish imports even for equal bytes; the hash verifies integrity, not deduplication or
resource identity. `origin` is descriptive provenance and never a playback dependency. Sampler intent
uses existing sound algorithm/decimal parameter fields under
the [audio owner](AUDIO_ENGINE.md#r2-f1-offline-sampler-foundation).
R1 readers can preserve this JSON but cannot reproduce the new algorithm or establish media availability.
Legacy R1 descriptor-only resources remain interpretable/unvalidated; they are never promoted to
accepted WAV by filename or caller availability assertions. Only the F1 `wav/` namespace is decoded/copied.

Before first Save, the document's lifecycle owns a persistent
`LocalApplicationData/Seqvium/ManagedMedia/<lifecycle UUID N>/wav/` directory. A caller may supply an
owned parent for isolated tests/application integration. This is separate from temp decoded caches and
Personal Library. A request exclusively creates/flushed-writes a `.pending` file, then renames to its
unique immutable WAV path before acceptance. File existence alone is never canonical acceptance.
There is no persisted unnamed document manifest or crash reconstruction: stored bytes remain protected,
but recovery of musical edits still requires R12. Close/Undo/Redo/Save never delete accepted source bytes.

For a named project `song.json`, locators resolve below `song.json.media/`. Move/copy JSON and this
whole associated directory together. Save As preserves the project UUID and references. It validates
source length/hash/codec, copies available current resources to exclusive temporary destination files,
flushes and validates each copy, then installs immutable destination WAVs without overwriting existing
bytes. Only after this prepares successfully does the sibling-temp JSON replacement mark the captured
revision/path saved. A conflicting destination WAV or destination write/JSON failure preserves the
previous Save and current unsaved work. Valid partial destination WAVs may remain conservatively;
they do not establish a successful document Save and retries must validate them. Source directories and
pre-existing destination content are never removed. JSON is the final normal-process publication point;
this is not a cross-volume transaction, crash consistency or power-loss durability guarantee.

Open checks actual F1 file bytes even when callers supply available-resource IDs. Missing, malformed,
unsupported or hash-damaged media yields degraded diagnostics while references/unknown state remain.
Execution refuses unavailable dependencies. Already unavailable input media permits degraded Save
of safely interpretable edits: `SaveWithReport` returns diagnostics and `SavedMediaDiagnostics` also
exposes the last Save/Open report for existing `Save` callers. Degraded Save As preserves unresolved
locators and reports incomplete portable availability; it never substitutes bytes or claims repair.
Errors transferring an available source still fail Save. Stream Read has no filesystem ownership and
cannot validate WAV availability; stream Write/Encode neither collects media nor marks explicit Save.

Current/saved/Undo/Redo snapshots retain canonical descriptors; session source roots remain available
across Save As, pending requests own their candidate, and live decoded leases own independent PCM.
No accepted-file GC is implemented: even history eviction/Close retains bytes, favoring protection over
premature cleanup. Disk growth, orphan reconciliation, relink/repair, general owner tracking and recovery
remain Q-059/Q-058. Disposable unaccepted request files and exclusively owned write temporaries may be
removed after their operation ends. Each WAV is bounded to 16 MiB; total durable disk quota is unselected.

F3 adds no persisted fields/schema change. Discovery/preview are nonpersistent; preview creates no
accepted descriptor or project-owned source bytes. Explicit project reuse creates a fresh SoundId
over the same ResourceId/managed locator, with no duplicate WAV or semantic deduplication. Independently
imported equal files remain distinct resources. Save As transfers a shared resource once per destination
under the existing integrity/retention rules.

## Required direction

- Carry project/schema format version, the Seqvium version that saved the project, and relevant
  compatibility metadata for deciding supported loading/migration. Provide migration-aware loading
  as the model evolves; the bounded R1 fields are defined above, later migrations remain open.
- When opening/migration is unsupported or fails, give a concise useful reason rather than an
  unexplained failure. Distinguish document-level refusal from recoverable processing unavailability
  under the opening contract below.
- Use stable entity/resource IDs where references and edit identity justify them.
- Define reasonable forward/backward handling: distinguish supported loading, safe preservation, and
  inability to reproduce behavior. Do not silently rewrite unsupported data as if fully understood.
- Ordinary import/drag-and-drop accepts audio into project-managed durable storage; normal use must
  not depend on the original arbitrary external path remaining available.
- Preserve extension identity, serialized state, and musical relationships, including currently unknown data.
- Preserve accepted generated audio; recipe metadata must not cause implicit regeneration on load.

[ARCHITECTURE](ARCHITECTURE.md#intended-musical-model) owns intended musical relationships;
[EXTENSIONS](EXTENSIONS.md#lifecycle-and-missing-capabilities) owns missing-capability behavior;
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md#acceptance-and-provenance) owns sample acceptance semantics.

## Opening and migration

If Seqvium can safely understand the document structure, local capability/resource failures normally
degrade affected parts rather than prevent access to the whole project. This includes missing, disabled
or incompatible ordinary plugins, recoverable activation/materialization failures, unavailable ordinary
execution capabilities, and individual managed media that is missing, corrupt, fails integrity validation
or cannot be decoded. Preserve affected resource/item references and plugin/node
identity/state, graph relationships, musical references and compatible opaque/unknown data. The project
is editable to the degree its document model is available; affected operations use dependency-scoped
blockers. [EXTENSIONS](EXTENSIONS.md#degraded-project-opening-and-operation-blockers) owns plugin-specific
handling. Broken media remains visibly represented; where meaningful, allow inspection, replacement,
relink/repair or removal of the dependency while unaffected portions remain usable. Never silently
substitute unrelated media. Persistent project-visible feedback follows
[UX_CONTRACT](UX_CONTRACT.md#project-availability-and-dependency-blockers).

Hard whole-project refusal is reserved for document-level conditions: critically unsupported project/
schema format, migration unable to safely resolve required structure, severe corruption, or fundamental
architectural incompatibility preventing safe interpretation. Missing processing alone is not a format
failure, and neither is a local media failure in an otherwise safely understandable document. Give
concise human-readable diagnostics; no final error codes/UI are chosen.

Distinguish lossless/internal migration from behavior-affecting compatibility transformations. Before
applying forced fallback/default substitution, lost parameters or nontrivial routing conversion,
present a concise summary of affected behavior and require Continue/Cancel-like choice. Explain when
saving upgrades the format and may prevent reopening in older Seqvium versions. Do not silently pretend
nothing changed. Purely lossless/internal migration need not necessarily interrupt the user; exact
classification, migration mechanism and UI/text remain open (Q-009). Destructive migration also follows
known-dependency safety under [ARCHITECTURE](ARCHITECTURE.md#document-integrity-and-asynchronous-publication).

## Save and reopen

Save persists canonical user/project state, including temporarily invalid/incomplete editable graph
work. It neither requires current executability nor substitutes the last-valid playing revision for the
user's edits. The prepared execution snapshot is derived runtime state and is not the authoritative
saved model under [NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution).

Reopen restores the canonical saved editable graph, shows its blockers and does not pretend invalid
work is executable. Affected execution-dependent operations remain blocked until repaired. Do not
persist native pointers, active buffers or live runtime objects, or a second permanent last-valid
project graph solely because it played in the previous session. Explicit future creative version/
history/checkpoint features would require separate design.

Export/render freezes, validates and prepares canonical state under
[AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction); it never silently uses stale playback.
Explicit Save represents the user-confirmed saved state. A newer crash-recoverable working state is
separate under the recovery contract below; it does not silently redefine that saved version.
Crash-safe persistence and media transaction mechanisms remain Q-058/Q-059, not a selected schema.

Only accepted canonical state belongs to Save. Pending async results and transient gesture/audition
previews are not silently serialized as completed project edits. A late completion after Save cannot
rewrite that explicitly saved version: a subsequently accepted result is a new canonical edit and
requires another Save to become the explicitly saved state. Recovery may protect that newer working
state under the existing separate contract. [ARCHITECTURE](ARCHITECTURE.md#logical-undo-transactions-and-history-scope)
owns document history; whether Undo/Redo history survives Save, reopen or restart, and its limits/storage,
remain Q-063. Neither Save nor rolling recovery implicitly selects a persistent command/action history.

## Recovery state

Crash recovery maintains a rolling current recoverable project snapshot/state alongside the explicit
saved project, rather than an indefinitely growing journal of every user action. Its representation
may grow with the project itself; retained size/history must not grow proportionally to edit count or
hours worked merely because actions accumulate. A later bounded transient log may be an internal
mechanism, not the durable product model or an ever-growing user-action history.

### Document identity and recovery choice

A recovery candidate belongs to an identified working document/lifecycle and a captured canonical
revision, with its required media relationships. An unnamed document has this conceptual identity
before any final project path exists. Display names, filenames, timestamps and current selection alone
cannot establish ownership or revision correspondence. Candidate indexing/identity encoding is unselected.

After abnormal termination, offer usable newer working state separately from the last explicit Save.
For a project saved at R10 with recovery at R15, R10 remains the last explicitly saved state; accepting
R15 opens recovered working state and does not write over R10. Opening R10 instead does not by itself
authorize discarding R15. Recovery choice must distinguish candidate availability from acceptance and
from subsequent explicit Save. Do not infer that a candidate is newer or equivalent solely by timestamp.

Recovery of an unnamed document resumes recoverable canonical work and its managed media without
inventing a saved filename. Subsequent Save As establishes a chosen saved destination for a specific
revision; until that succeeds, the recovered work remains unsaved. For a named recovered document,
subsequent explicit Save may deliberately update its saved destination, or Save As may establish another;
make the destination and recovered-unsaved status understandable. Exact interaction/windows remain open.

### Replacement, fallback and retirement

Preparing a newer recovery state must not invalidate the older known valid candidate and its required
media before a valid recoverable successor is established. This also protects the sole known recent copy
of unnamed work. Incomplete replacement data is not a valid candidate merely because files exist.
On restart, validate interpretability and available dependencies; distinguish a usable document with
local media blockers from unsafe fundamental snapshot corruption. Reject the latter as working state,
report it concisely and offer an older valid candidate or independent explicit Save where available,
identifying the fallback and possible lost interval. Do not silently label fallback as the latest state.
A corrupt recovery artifact does not establish corruption of an independent valid saved project.
If no safe fallback exists, explain what is unavailable; do not fabricate recovered edits or audio.

A successful Save makes a recovery candidate redundant only when document identity, captured revision
coverage and required durable media establish that the saved result protects its work. Retain candidates
containing newer unsaved edits, candidates for other documents and resources still owned elsewhere.
Coverage must account for actual state lineage/content and dependencies; a higher revision number alone
does not prove inclusion of recovered work across Undo, branching or another document lifecycle.
Recovery acceptance, project opening, elapsed time or a Save-success notification alone is not proof
of safe retirement. Before deleting candidate media, apply the [owner rules](#resource-retirement).
Cleanup failure may leave redundant material; it must not invalidate the saved/recovered result or
turn a successful Save into a failed document commit. Exact retry/diagnostic mechanics remain open.

Recovery refresh may follow meaningful document transactions, a periodic schedule, debounced changes
or a hybrid. No exact cadence is accepted. High-frequency interaction such as parameter dragging must
not cause pathological disk writes; dirty marking followed by a bounded debounce/important transition
is a possible later policy, not a chosen algorithm.

Recovery storage must remain bounded, but cleanup must not casually delete the only known recent copy
of unsaved work merely to satisfy an arbitrary size/count threshold. Retention count/age/size, layout,
corruption detection, crash-safe replacement and cleanup algorithms remain open. Temporary/staging/
previous copies may support safe replacement without becoming accumulated action history.

Under storage pressure, prioritize disposable caches, safely unowned staging and demonstrably redundant
recovery before uniquely protective unsaved work. If a new recovery cannot be established safely, retain
the known good candidate, report that protection is stale/unavailable and leave canonical work unsaved;
do not pretend all recent edits are protected. An informed explicit discard/cleanup may release identified
unsaved recovery ownership, subject to other owners; ordinary preference reset, opening an older Save
or automatic age/count/size eviction cannot stand in for that choice. This is neither unlimited retention
nor a recovery-history browser/automatic multi-version backup feature. Numerical quotas remain unselected.

Document recovery does not reconstruct missing audio magically. Future recording/generation and
accepted imports must manage durable media independently enough for recovery to reconnect to already
produced material where possible, under [recording reconciliation](#interrupted-recording).

Recovery protects canonical working state, not acceptance of pending results or transient previews.
Separately owned pending/generated media may still require safe preservation/reconciliation; its retention
does not make it a saved or accepted project edit. History, recovery and pending-work resource ownership
follow [media integrity](#media-and-persistence-integrity), with exact mechanisms still open.

## Musical content and workspace state

Serialization must respect the distinct identities of named multi-instrument patterns, their musical
parts/events and placements, organizational instrument/channel groups, and editable signal-graph
definitions/connections. Group membership must not become pattern storage identity or imply routing.
The bounded R1 schema is defined above; later graph scopes remain open. [ARCHITECTURE](ARCHITECTURE.md)
and [NODE_GRAPH](NODE_GRAPH.md)
own the model and editable/prepared boundary. Saving project state must not require preserving live
UI objects or treating a prepared realtime representation as the editable document.

Persistence must separately preserve sharing/independence of Pattern musical content, instrument/sound
definition, placement state, and processing state. Musical-content variation does not implicitly detach
every sound/resource/processing relationship. R1 implements this bounded copy/reference mechanism;
it does not serialize execution instances. [ARCHITECTURE](ARCHITECTURE.md#separate-sharing-identities)
owns the identities.

Preserve intended content/sound-use/resource references, occurrence-local timing/ranges, Arrangement
organizational membership and any preferred-purpose configuration, actual item/containing processing
membership and downstream route relationships independently where applicable. The
[identity boundaries](ARCHITECTURE.md#separate-sharing-identities) are persistence obligations, not a
class or field inventory. Organization alone cannot be decoded as a bus or exclusive sound owner.
Different supported musical fragments, Pattern placements, audio clips and later recordings need not
be forced into one identical stored shape.

Saving only visible Arrangement layout and reconstructing sharing on reopen is insufficient. Equal
labels, events, presets or audio bytes do not prove one identity; different labels do not prove independence.
Round-trip must preserve a repeated Pattern's shared notes, a variation's independent notes with shared
sounds, an independently editable sound's resource references, and reused media with local clip edits.
Moves/deletion/Undo preserve or deliberately change the identified relationships under
[architecture](ARCHITECTURE.md#deletion-scope-and-retained-relationships), rather than rely on heuristics.
R1 domain IDs, musical time and basic copy/reference operations are implemented. Later migrations,
graph attachments, expanded edit workflows and audio-time mappings remain Q-009/Q-019/Q-029/Q-051;
no runtime instance history is selected.

Preserve shared sound-definition settings/content references and canonical relationships needed to
express intended performance interaction and contribution routing under
[ARCHITECTURE](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains). Derived execution-domain
grouping, live voices/envelopes/tails, physical instance counts and pools are not a second canonical
saved model. Persisted plugin/instrument state below means durable sound-defining configuration/opaque
data, not a mandate to serialize every runtime execution instance or its transient performance state.
R1 retains explicit part/occurrence performance-interaction and route intent. Runtime grouping,
capability realization and extension-state synchronization remain open.

Main-window pane layout is primarily application/user workspace state under [WORKSPACE](WORKSPACE.md).
Opening a project should not normally overwrite it. Project-side graph/editor layout is distinct from
the user's overall pane arrangement; no final storage policy or optional project-workspace format is chosen.

## Project state and user preferences

Persist project-owned values that affect sound, timing, musical meaning, or reproducible behavior,
including tempo/time signature, routing/processing, time/stretch behavior, applicable project audio
settings, and plugin/instrument instance state. Preserve instance state with identity/relationships
even when code is unavailable or incompatible. New-project/new-instance defaults are copied at creation,
not live-linked to preferences. [SETTINGS](SETTINGS.md#user-configuration-and-project-state) owns this
classification and the plugin-global presentation/default boundary; exact paths/formats remain open.
Configuration reset must not delete projects or project-managed audio/media.

Migration/preservation of structure and data does not promise exact historical sonic identity.
[AUDIO_ENGINE](AUDIO_ENGINE.md#sound-compatibility-boundary) and
[EXTENSIONS](EXTENSIONS.md#plugin-sound-responsibility) own platform/plugin sound responsibility;
there is no hidden permanent old-engine emulation requirement in this format contract.

## Media policy boundary

Persistence must preserve the distinction between durable audio resources and their musical
placements/references, including local processing state. Multiple items may use one resource;
ordinary local processing must not silently become a destructive rewrite of that shared media.
Separate reference/edit identity must be expressible without assuming final class names, copy-on-write
mechanics, resource deduplication, or a schema. [ARCHITECTURE](ARCHITECTURE.md) owns the model;
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) owns acceptance and source-preserving sample creation.

Preserve non-destructive trim ranges, split-piece references/placements, and distinct loop/repeat and
stretch intentions without rewriting the original durable audio resource through ordinary timeline
editing. Preserve project-wide tempo and audio tempo-following versus fixed/source-time relationships,
with independent local stretch, under [ARCHITECTURE](ARCHITECTURE.md#project-tempo-and-audio-time) and
[UX_CONTRACT](UX_CONTRACT.md#audio-timeline-editing). This does not select time/stretch encoding.
An explicit operation creating/committing a new cropped/consolidated/rendered resource is separate
from normal trim/split; edit-structure changes do not inherently modify source media destructively.

Ordinary media import/drag-and-drop creates a project-managed durable resource at successful acceptance,
including in an unnamed/unsaved working project. Normal continued use must not depend on the original
external file: moving/deleting `D:\Samples\kick.wav` after successful import must not break that resource.
This covers ordinary WAV/MP3 imports, FLAC if later supported, accepted Sample Lab output, recordings,
used content-pack material and other accepted audio. These examples do not select supported codecs;
format support remains roadmap/implementation work. Using one library file must not copy unused
portions of the entire library. A normally saved project is self-contained with respect to used audio.

Once used pack/sample material is managed project media, removing the original pack must not lose that
audio. Accepted generated audio likewise remains durable if its generator disappears. Durability takes
precedence over continued availability of the original source; missing executable instruments/effects
remain subject to [EXTENSIONS](EXTENSIONS.md), not a promise that saved media replaces every algorithm.

An advanced deliberate external-reference workflow, if retained or introduced, needs separate
justification and must be explicit and clearly distinguishable from ordinary import. It may later
serve very large shared libraries or deliberate shared media management. Users
must understand whether audio travels with the project, remains externally referenced, or is missing;
missing/corrupt media must be represented clearly under the degraded opening contract. Final UI and
reference mechanics remain open; ordinary import is not an external-reference workflow.

The managed representation may differ from the source format. A canonical durable representation,
optional source/origin metadata and disposable decoded/cache forms remain possible. No PCM-only rule,
sample format/bit depth, compression, original-file retention, dual source/canonical storage, content
addressing or codec library is selected. Runtime cache must never be the only durable copy of accepted media.

This default does not select a single archive/file, directory/bundle, manifest plus media directory,
or another versioned container. Exact embedding/copying policy, deduplication, garbage collection of
unused media and general collect/relocate remain open. F1 chooses only the bounded layout/integrity
hash above; wider content addressing, transcoding and cleanup remain open.
Preserving user material does not authorize automatic deletion of unused resources.

## Source provenance and reusable content

Browser availability and origin metadata are distinct from durable project ownership under
[Sample workflow](SAMPLE_WORKFLOW.md#browser-discovery-and-ownership). Ordinary use of a filesystem,
pack or Personal Library sample accepts a managed project resource through the same media boundary.
Only used material is required; using one item does not create a dependency on its whole library.
Moving/removing/offlining its source must not damage properly accepted self-contained project audio.
Provenance may preserve useful origin information without requiring that origin on reopen/playback.

Personal Library ownership is independent of project media: project cleanup cannot delete a retained
library original, and library removal cannot remove accepted project uses. A future shared physical
storage/deduplication strategy must preserve these separate ownership/lifetime guarantees. Equal bytes,
filenames or paths alone do not establish one semantic resource/source identity. F1 copies accepted
WAV into project ownership; no shared library store, deduplication or library metadata schema is selected.

Applying a preset/template retains sufficient project-owned sound configuration and required extension
identity/compatible state, including opaque data where applicable, under
[Extensions](EXTENSIONS.md#preset-sources-and-project-state). Renaming/editing/removing the source
preset does not silently change that accepted state. Saving a project does not publish its state as a
global preset or sample; explicit library publication belongs to a separate user-content workflow.
Library format/backup/import/export is outside the project container decision Q-009; Q-058/Q-059
remain open for project recovery and managed-media storage/integrity mechanisms.

## Media and persistence integrity

### Semantic states and owners

These are ownership/lifetime states, not required directories, tables, classes or physical copies.
One resource can have several owners; byte/path equality does not merge semantic identities.

| State / owner                                  | Preservation responsibility                                                                                      |
|------------------------------------------------|------------------------------------------------------------------------------------------------------------------|
| Current canonical working document             | Accepted editable intent and resource/use/definition relationships, including unsaved or locally degraded work   |
| Last successfully explicit-saved project       | Identified saved canonical revision and its managed-media dependencies, independently of later working edits     |
| Rolling recoverable working state              | Identified captured canonical revision and dependencies, independently of Save; not every edit since the capture |
| Project-managed durable media                  | Accepted reusable audio independent of source path/generator; may have no visible placement                      |
| Prepared/staged unaccepted media               | Owned preparation/output whose existence is not canonical acceptance; partial and complete output differ         |
| Transient disposable cache                     | Regenerable/dispensable preview or derived data; never the sole durable accepted-media copy                      |
| Undo/Redo-retained resources                   | Material needed to restore retained canonical relationships, even with zero current visible uses                 |
| Recovery-retained resources                    | Material needed by retained candidates, including revisions different from current working/saved state           |
| Pending async-owned resources                  | Inputs/output needed until completion, cancellation, delivery or safe retirement; no implicit project acceptance |
| Independently owned Personal Library resources | Explicitly retained user content outside project cleanup authority, even if physical storage is shared           |

Prepared execution is derived runtime state, outside Save/recovery authority. Active playback/render/
audition may additionally require safe live-use retirement under the audio owner. Disposable Sample Lab
candidate history, project Undo and recovery are different mechanisms with different retention promises.

Conceptually: prepare material/state -> establish necessary durable storage -> validate identities,
dependencies and authority -> commit the canonical edit -> persist Save or recovery independently ->
retire only safely unowned obsolete material. Canonical acceptance need not wait for explicit Save or
the next recovery capture; a crash may therefore lose accepted edits not yet captured while leaving
owned produced media. Honest reconciliation must not present that media as proof of a recovered edit.

### Acceptance and repair

From the user's perspective, acceptance is one coherent resource/reference/use edit after necessary
managed durable storage and commit validation succeed. Interrupted import/copy/conversion, disk full,
permission/write failure or unavailable new material cannot leave a complete accepted resource, partial
target replacement or successful Undo entry. Preserve existing canonical relationships and prior saved
state; retain identifiable staging under its owner for safe retry or eventual cleanup. Retrying validates
the source and intended destination anew; it cannot promote an arbitrary leftover file by name.

An unnamed project's accepted audio has project-managed lifetime from acceptance, before first Save.
It must survive original WAV disappearance and remain available to working state, Undo and recoverable
captures as applicable. Save As transfers/establishes the saved ownership only after a coherent result;
it cannot prematurely release the unnamed working/recovery media. Cache lifetime is insufficient.

Repair/relink/replace validates the identified damaged resource or intended use scope, prepares and
validates replacement audio, establishes managed storage and rechecks target/dependencies at commit.
Only then accept the intended reference/resource transition coherently, with normal Undo. Preserve the
old unresolved reference and affected-item identity on failure; retained history/recovery must still
describe their own prior relationships. Do not destroy old media needed by another owner, silently widen
repair to unrelated uses, or substitute a same-named file. Whether a repair preserves a resource identity
or deliberately establishes a replacement is operation-specific Q-029/Q-059 design, not filename inference.

### Save, Save As and relocation

Successful explicit Save identifies a coherent captured canonical revision with the required managed
media for complete normal use. Save does not require executable plugins or a valid playing graph.
For already missing/corrupt media, preserve the safely understandable canonical document, references
and unknown state through degraded Save/reopen; report unresolved dependencies and do not claim complete
media collection, repair or portable audio availability. This is distinct from accepting unavailable new
media or silently dropping damaged references to manufacture success.

Save failure before commit preserves the previous coherent saved project and its media; current
canonical edits remain unsaved and recovery-eligible. It does not silently roll back musical work or
declare it saved. Recovery may independently succeed or fail; neither proves explicit Save succeeded.
A Save that captures R10 while edits reach R15 saves R10 only, and must not clear R15's unsaved status.
Preserve prior saved dependencies during later edits/cleanup, not just the latest working references.

Save As from A to B prepares a coherent captured revision and required destination media. Until that
transition is established, A's prior Save, current edited work, destination association and recovery/media
ownership remain protected. Partial B is incomplete output, not a successful saved project or authority
to remove A. Retry/reconciliation must distinguish owned partial output from pre-existing destination
content; cleanup cannot delete unrelated user files. A successfully established B may become the chosen
saved destination without deleting A by implication. R1 Save As retains the project UUID;
explicit fork/media relocation policy remains Q-009/Q-029.

Collect/relocate has the same multi-resource obligation: no transfer, some media transferred, or a
document copied before required media are all insufficient for success. Preserve the coherent source
until destination state/media and completion are established; transfer may cross filesystems/volumes.
Even after destination success, source removal is a separate safe ownership/lifecycle step within the
requested move/cleanup scope. Protect other saved/recovery/history/pending/library owners. A collection
with known missing material must identify incompleteness rather than claim a complete portable project.

### Commit evidence and ambiguous completion

Atomic visibility means observers see a coherent transition rather than partial publication. Crash
consistency means interruption leaves an interpretable coherent result/fallback. Actual durable
persistence concerns what remains stored under stated failure conditions. None establishes the others
by assertion; process termination, storage errors and power loss require distinct evidence.

A crash after a durable transition but before UI confirmation differs from pre-commit failure. Where
completion is uncertain, report uncertainty and preserve potentially owned results. Reopening/retry must
establish which coherent prior/new state and required media are actually available using adequate
identity, operation/revision and integrity evidence, before duplicating, attaching or deleting anything.
An established committed result may be recognized despite missing acknowledgement; a partial artifact
must not be promoted to success. If evidence is insufficient, retain ambiguity and offer safe continuation,
not invented automatic reconstruction. Definitive failure must not be reported as success; uncertainty
must not be falsely resolved in either direction. A coherent committed result is the new saved state
even if acknowledgement was lost, not a rollback to the previous Save by assumption.

Storage adapters must eventually substantiate these requirements for their chosen representation and
supported operating systems. A single filesystem rename does not establish general cross-file durability;
no general ACID, cross-volume atomicity or power-loss guarantee is accepted. Layout/container/database,
journal/log, broader rename/fsync guarantees, deduplication/content addressing, transcoding and cleanup
remain unselected beyond F1's bounded adapter above. Q-058/Q-059 need wider race/fault-injection and
platform/storage evidence before concrete guarantees can be claimed.

### Failure-boundary matrix

Here, canonical acceptance, persistence commit and UI acknowledgement are distinct boundaries.
The matrix states obligations, not a selected transaction protocol or permanent test-case catalogue.

| Operation                    | Before                                                             | Prepared, not committed                                                               | Established result                                                                             | Before-commit failure                                                                          | Around-commit ambiguity                                                                                     | Retain / eventual cleanup                                                                                                                            |
|------------------------------|--------------------------------------------------------------------|---------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| Import / acceptance          | Existing working/saved references; external source                 | Owned partial/complete staging, then validated durable material; no accepted edit yet | Gate-valid resource/use edit and one Undo transaction; Save/recovery separately capture it     | No partial accepted resource/edit; old relationships intact; safe retry                        | Stored bytes alone prove no edit; reconcile storage and captured acceptance evidence separately             | Working/saved/history/recovery/pending owners retain; only safely unowned staging retires                                                            |
| Explicit Save                | Last coherent Save; possibly newer working/recovery state          | Captured revision and protected required media; previous Save retained                | Coherent saved revision/media, with degraded dependencies disclosed; later edits still unsaved | Previous Save/media and current work intact; recovery remains independent                      | Validate available old/new coherent result; do not infer success/failure from lost UI confirmation          | Saved dependencies retain; redundant recovery retires only by identity/revision coverage and owner checks                                            |
| Recovery update              | Valid candidate if one exists; current work may be newer           | Owned incomplete successor and dependency material                                    | Valid successor protects its captured revision; explicit Save unchanged                        | Older valid candidate remains; report uncaptured interval/protection failure                   | Validate candidate/fallback; incomplete/corrupt data is not latest working state                            | Unique unsaved protection prioritized; demonstrably superseded candidate ownership can retire                                                        |
| Save As / collect / relocate | Coherent source and edited work; any pre-existing destination      | Owned partial destination document/media; no final ownership switch                   | Identified coherent destination with necessary media; source retirement separately validated   | Source/current work protected; destination incomplete; retry/cleanup scoped to owned artifacts | Establish source/destination status before association switch, retry or deletion; no same-volume assumption | Protect source until destination established and other owners released; leave unrelated destination content intact                                   |
| Async render / acceptance    | Frozen scope, original lifecycle/target/authority and owned inputs | Pending computation and durable output; still no canonical edit                       | Revalidated intended resource/reference edit; execution and persistence follow independently   | No accepted edit; old state intact; useful output needs an explicit owner                      | A completed file cannot prove acceptance or target validity; reopen grants no old lifecycle authority       | Pending/unattached owner until deliberate fresh reuse or safe cleanup; accepted history/recovery owners thereafter; no automatic library publication |

### Interrupted recording

Distinguish safely stored/validated material already acknowledged as durable, recoverable incomplete
recording material, samples that may never have reached durable storage, and canonical recording edits
already accepted. Acknowledging durable material requires the actual storage guarantee; it is not merely
acknowledging capture in memory. Crash/device loss must preserve the established durable portion and any
accepted references available through Save/recovery. Reconcile incomplete material only to the extent
it can be safely identified and interpreted; show actual available extent and interruption, never a
fabricated complete take. Unsaved accepted edits beyond the last capture may be unavailable even when
audio bytes remain. Unattached material needs separate safe identification/reuse, not automatic placement.
No recovery of unpersisted samples is promised. Recording containers, streaming buffers, commit units
and reconciliation metadata/evidence remain later recording work coordinated with Q-058/Q-059/Q-062.

### Resource retirement

Zero visible placements is insufficient: an accepted reusable resource may still belong to the working
document, and the last explicit Save, retained Undo/Redo, recovery candidates, pending operations and
active prepared execution/render/audition may each prevent deletion. Personal Library ownership remains
independent. A saved R10 can retain media deleted from working R15; a recovery R15 can retain material
absent from saved R10. Physical sharing must honor every owner without requiring a particular counter/GC.

Save, Undo, Redo, cancellation or project close does not universally release every owner. Closing ends
canonical commit permission; it does not erase saved/recovery protection or prove pending tasks have
stopped using resources. Whether document history persists after close/restart remains Q-063; when
history is deliberately retired its ownership may end, while other owners remain. Explicit informed
discard can release the selected unsaved work/candidate, not independent saved/library content. Safe
cleanup needs validated ownership/lifecycle relevance at retirement, including races with acceptance,
recovery replacement and Save; age, path/name or timestamps alone are insufficient. Uncertain ownership
requires retention/reconciliation rather than destructive guessing. Q-059 owns tracking/GC mechanics.

### External damage and mixed-capability reopening

Seqvium must detect/report invalid required material where its validation can establish it, preserve
safely interpretable state/references/item identity and permit meaningful repair, replacement or removal.
External deletion, modification, damage or moves while closed do not justify silent substitution or
reconstruction from equal filenames. Missing Browser origins alone do not damage accepted managed media.
Protection from arbitrary external changes is not guaranteed, and universal filesystem monitoring is
not required. Integrity validation coverage/mechanisms remain Q-059 evidence work.

Missing executable plugin capability and damaged audio are separate dependency failures. Save/recovery
preserves plugin identity, compatible opaque/unknown state and musical/graph relationships; accepted
audio requires no original generator execution. Safely understandable documents open degraded under
[opening and migration](#opening-and-migration), with operation-scoped blockers. Fundamental unsafe
document interpretation alone justifies whole-document refusal; a bad local resource/candidate does
not prove another independent project artifact is corrupt.

## Unknown extension data

A missing, incompatible, or disabled algorithm may prevent playback, but must not cause save to discard
its identity, opaque state, musical connections, or contributed graph-node relationships. Future
preservation checks must cover round-trip absent-extension data and compatible reattachment. User
load/edit/save paths normally remain available under the degraded [opening contract](#opening-and-migration)
when the document is safely understandable; unavailable processing blocks its dependent operations.
R1 retains opaque JSON payload plus extension identity/state version as defined above. This proves
data preservation, not executable plugin compatibility or arbitrary runtime duplication.

## Open format choices

R1 establishes bounded JSON, identity/time encoding, version refusal and compatible unknown-data
preservation. F1 adds the bounded managed WAV layout above. Broader containers/migrations, crash-safe
save/recovery, wider resource integrity and executable extension-state evolution remain open in
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
Q-058 retains crash-safe snapshot replacement/validation, candidate indexing/selection, cadence/debounce,
bounded retention, recovery-choice realization and recording reconciliation. Q-059 retains durable-media
protocol beyond F1, repair, owner tracking/GC and broader Save As/collect/relocate consistency.
Cross-platform storage guarantees and wider race/fault-injection evidence remain
required; the identity/revision, failed/ambiguous completion and cleanup obligations are fixed above.
The recovery/media product contracts above are accepted; mechanisms beyond bounded F1 remain open.
Exact derived preparation/publication mechanisms remain Q-018; canonical Save/reopen/render policy
is accepted above and in the audio owner.
WAV export is an audio deliverable, not a substitute for project serialization.
