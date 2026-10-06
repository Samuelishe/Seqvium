# Project format

Role: Serialization compatibility and resource-preservation contract.
Read when: Designing save/load, migration, managed media, or extension-state persistence.
Authoritative for: Compatibility direction, versioned persistence, media and unknown-data preservation.
Not authoritative for: A final container/schema, runtime domain classes, extension API, or recovery implementation.

No project format is implemented or selected. This document constrains future choices without
inventing field names, file extensions, or a container layout.

## Required direction

- Identify schema/version and provide migration-aware loading as the model evolves.
- Use stable entity/resource IDs where references and edit identity justify them.
- Define reasonable forward/backward handling: distinguish supported loading, safe preservation, and
  inability to reproduce behavior. Do not silently rewrite unsupported data as if fully understood.
- Define relative/managed media paths and an explicit embedded/copied/external resource policy.
- Preserve extension identity, serialized state, and musical relationships, including currently unknown data.
- Preserve accepted generated audio; recipe metadata must not cause implicit regeneration on load.

[ARCHITECTURE](ARCHITECTURE.md#intended-musical-model) owns intended musical relationships;
[EXTENSIONS](EXTENSIONS.md#lifecycle-and-missing-capabilities) owns missing-capability behavior;
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md#acceptance-and-provenance) owns sample acceptance semantics.

## Musical content and workspace state

Serialization must respect the distinct identities of named multi-instrument patterns, their musical
parts/events and placements, organizational instrument/channel groups, and editable signal-graph
definitions/connections. Group membership must not become pattern storage identity or imply routing.
Exact schemas and graph scopes remain open; [ARCHITECTURE](ARCHITECTURE.md) and [NODE_GRAPH](NODE_GRAPH.md)
own the model and editable/prepared boundary. Saving project state must not require preserving live
UI objects or treating a prepared realtime representation as the editable document.

Main-window pane layout is primarily application/user workspace state under [WORKSPACE](WORKSPACE.md).
Opening a project should not normally overwrite it. Project-side graph/editor layout is distinct from
the user's overall pane arrangement; no final storage policy or optional project-workspace format is chosen.

## Media policy boundary

Persistence must preserve the distinction between durable audio resources and their musical
placements/references, including local processing state. Multiple items may use one resource;
ordinary local processing must not silently become a destructive rewrite of that shared media.
Separate reference/edit identity must be expressible without assuming final class names, copy-on-write
mechanics, resource deduplication, or a schema. [ARCHITECTURE](ARCHITECTURE.md) owns the model;
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) owns acceptance and source-preserving sample creation.

Copying/embedding used pack audio into project-managed resources should make that project independent
of pack removal. External references cannot imply the same guarantee. Resource ownership and missing
media must be explicit enough for users to understand what travels with a project.

The default policy, deduplication, collection/relocation, unused-media cleanup, and whether projects
embed all accepted audio or manage it alongside the document remain open. Preserving user material
does not authorize automatic deletion of unused resources.

## Unknown extension data

A missing, incompatible, or disabled algorithm may prevent playback, but must not cause save to discard
its identity, opaque state, musical connections, or contributed graph-node relationships. Future tests
must cover load/edit/save while an extension is absent, then reinstallation of compatible code.
Exact opaque encoding and compatibility claims are undecided.

## Open format choices

SEQ-R1 should establish only a bounded versioned foundation. Container versus directory, encoding,
ID/time representation, migration mechanism, unsupported-version behavior, crash-safe save/recovery,
resource integrity, and extension-state evolution remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
WAV export is an audio deliverable, not a substitute for project serialization.
