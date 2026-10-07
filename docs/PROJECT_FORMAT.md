# Project format

Role: Serialization compatibility and resource-preservation contract.
Read when: Designing save/load, migration, managed media, or extension-state persistence.
Authoritative for: Compatibility direction, canonical Save/reopen, opening/migration, versioned persistence, media and unknown-data preservation.
Not authoritative for: A final container/schema, runtime domain classes, extension API, or recovery implementation.

No project format is implemented or selected. This document constrains future choices without
inventing field names, file extensions, or a container layout.

## Required direction

- Carry project/schema format version, the Seqvium version that saved the project, and relevant
  compatibility metadata for deciding supported loading/migration. Provide migration-aware loading
  as the model evolves; exact schema and metadata field names remain open.
- When opening/migration is unsupported or fails, give a concise useful reason rather than an
  unexplained failure. Distinguish document-level refusal from recoverable processing unavailability
  under the opening contract below.
- Use stable entity/resource IDs where references and edit identity justify them.
- Define reasonable forward/backward handling: distinguish supported loading, safe preservation, and
  inability to reproduce behavior. Do not silently rewrite unsupported data as if fully understood.
- Make normally used audio project-managed/self-contained by default; external references are explicit alternatives.
- Preserve extension identity, serialized state, and musical relationships, including currently unknown data.
- Preserve accepted generated audio; recipe metadata must not cause implicit regeneration on load.

[ARCHITECTURE](ARCHITECTURE.md#intended-musical-model) owns intended musical relationships;
[EXTENSIONS](EXTENSIONS.md#lifecycle-and-missing-capabilities) owns missing-capability behavior;
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md#acceptance-and-provenance) owns sample acceptance semantics.

## Opening and migration

Inability to reproduce all project audio does not normally mean inability to open its document. Missing,
disabled or incompatible ordinary plugins and recoverable activation/materialization failures normally
produce degraded opening, provided the document can still be safely understood. Preserve plugin/node
identity/state, graph relationships, musical references and compatible opaque/unknown data. The project
is editable to the degree its document model is available; affected operations use dependency-scoped
blockers under [EXTENSIONS](EXTENSIONS.md#degraded-project-opening-and-operation-blockers).

Hard whole-project refusal is reserved for document-level conditions: critically unsupported project/
schema format, migration unable to safely resolve required structure, severe corruption, or fundamental
architectural incompatibility preventing safe interpretation. Missing processing alone is not a format
failure. Give concise human-readable diagnostics; no final error codes/UI are chosen.

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
Autosave/crash recovery, crash-safe persistence and media transactions remain Q-058/Q-059; this Save
rule does not choose those mechanisms or the serialization schema.

## Musical content and workspace state

Serialization must respect the distinct identities of named multi-instrument patterns, their musical
parts/events and placements, organizational instrument/channel groups, and editable signal-graph
definitions/connections. Group membership must not become pattern storage identity or imply routing.
Exact schemas and graph scopes remain open; [ARCHITECTURE](ARCHITECTURE.md) and [NODE_GRAPH](NODE_GRAPH.md)
own the model and editable/prepared boundary. Saving project state must not require preserving live
UI objects or treating a prepared realtime representation as the editable document.

Persistence must separately preserve sharing/independence of Pattern musical content, instrument/sound
definition, placement state, and processing state. Musical-content variation does not implicitly detach
every sound/resource/processing relationship. This does not select a schema, copy mechanism, class
hierarchy, or serialized execution instances; [ARCHITECTURE](ARCHITECTURE.md#separate-sharing-identities)
owns the identities.

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

A normally saved Seqvium project should be self-contained with respect to audio material actually used
by the project. When external/imported audio becomes used material, the default is to place/copy/manage
the required audio in project-managed storage rather than retain fragile absolute links into arbitrary
sample folders. This covers imported samples, accepted Sample Lab audio, recordings, used content-pack
material, and other audio required to reproduce the project. Using one library file must not copy unused
portions of the entire library.

Once used pack/sample material is managed project media, removing the original pack must not lose that
audio. Accepted generated audio likewise remains durable if its generator disappears. Durability takes
precedence over continued availability of the original source; missing executable instruments/effects
remain subject to [EXTENSIONS](EXTENSIONS.md), not a promise that saved media replaces every algorithm.

Explicit external-reference workflows may later serve very large shared libraries, deliberate shared
media management, or advanced use. They must be distinguishable from the normal durable path. Users
must understand whether audio travels with the project, remains externally referenced, or is missing;
missing external media must be represented clearly. Final UI and reference mechanics remain open.

This default does not select a single archive/file, directory/bundle, manifest plus media directory,
or another versioned container. Exact embedding/copying policy, deduplication, garbage collection of
unused media, collect/relocate workflow, checksums/content addressing, and storage layout remain open.
Preserving user material does not authorize automatic deletion of unused resources.

## Unknown extension data

A missing, incompatible, or disabled algorithm may prevent playback, but must not cause save to discard
its identity, opaque state, musical connections, or contributed graph-node relationships. Future
preservation checks must cover round-trip absent-extension data and compatible reattachment. User
load/edit/save paths normally remain available under the degraded [opening contract](#opening-and-migration)
when the document is safely understandable; unavailable processing blocks its dependent operations.
Exact opaque encoding and compatibility claims are undecided.

## Open format choices

SEQ-R1 should establish only a bounded versioned foundation. Container versus directory, encoding,
ID/time representation, migration mechanism, unsupported-version behavior, crash-safe save/recovery,
resource integrity, and extension-state evolution remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
Q-058 separates explicit Save from unresolved autosave/crash recovery, including corruption safety,
bounded retention, recorded media and crash-restart user choice. Q-059 tracks managed-media integrity
and save/collect/relocate workflows. No recovery scheme is accepted by mentioning these obligations.
Exact derived preparation/publication mechanisms remain Q-018; canonical Save/reopen/render policy
is accepted above and in the audio owner.
WAV export is an audio deliverable, not a substitute for project serialization.
