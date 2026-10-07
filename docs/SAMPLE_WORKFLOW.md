# Sample workflow

Role: Seqvium's sound exploration and material-transformation contract.
Read when: Designing discovery, generation, audition, sample acceptance, or resampling.
Authoritative for: Sample Lab context, candidate audition/acceptance, durable audio, object sampling and audible capture.
Not authoritative for: Generator algorithms, DSP/render details, package API, or final resource file format.

## The creative loop

A sample is a first-class project resource. The intended loop is:

```text
discover/create sound
    -> audition variations
    -> accept as sample
    -> use in pattern/music
    -> transform/render musical material
    -> create a new sample
    -> reuse and transform again
```

The transition between exploration and musical context should be unusually cheap. Sample Lab is
a dedicated creative workspace pane, with behavior/layout owned by [WORKSPACE](WORKSPACE.md).
It is not isolated from the composition. No Sample Lab is implemented yet.

## Standalone and contextual Sample Lab

Two accepted conceptual entry modes serve the same exploration surface:

```text
Standalone: open Sample Lab -> discover / generate / mutate -> audition -> accept -> add/use material
Contextual: select Kick -> open Sample Lab with target/context -> generate / similar / mutate
           -> audition candidate in current music -> accept explicitly
```

Standalone exploration requires no selected target. Contextual exploration knows the active musical
sound/material and lets the user answer whether a variation improves the current music without
export/import, manual source replacement, rebuilding routing, or remembering previous processing.

Contextual candidates should normally be heard through the processing that would actually affect
the target in the current composition:

```text
candidate source -> existing local processing -> containing processing -> relevant downstream mix
                 -> audition
```

Solo/raw audition may also exist, but contextual audition is first-class. An attractive isolated
preview must not be the only way to judge a candidate that sounds substantially different after
acceptance in the real project. Audition remains temporary and reversible until explicit acceptance;
it must not silently rewrite accepted musical material or a shared audio resource.

Substitution mechanics, stop/cancel/restore, realtime publication, downstream boundaries and exact
target lifetime remain open. Async completion must revalidate project/target identity, compatible
context, relevance/cancellation and ownership/revision preconditions before publication or acceptance
under [ARCHITECTURE](ARCHITECTURE.md#document-integrity-and-asynchronous-publication). Generation for
a deleted target must not attach to current selection; unattached candidate, discard or explicit reuse
policy depends on the workflow. Q-011/Q-063 retain exact mechanics and undo/commit grouping.

## Specialized generation in a shared exploration surface

Prefer meaningful specialized generators or modes inside one exploration surface over a gigantic
universal synthesizer displaying every algorithm simultaneously. Possible families include percussion,
clicks/impacts, noise/rhythmic noise, waves/wind/atmospheres, synthetic or physical-model-inspired
plucks, guitar/string-like sounds, and experimental textures. These are ideas, not V1 scope.

The first generator should be local/procedural and cover one or two bounded families in SEQ-R7.
[EXTENSIONS](EXTENSIONS.md#default-generator) owns its default-installed/removable status.

## Intentional and lazy exploration

Random exploration must have structure rather than randomizing every parameter indiscriminately.
The long-term interaction should be capable of:

- Give me another sound, or something similar.
- Vary this more.
- Preserve the attack while changing the tail.
- Lock selected properties while exploring others.
- Return to an earlier candidate and compare several candidates.

These are semantic goals; distance metrics, locks, similarity algorithms, history retention, and
comparison controls are not specified yet. [UX_CONTRACT](UX_CONTRACT.md) owns general feedback and
discoverability principles. Candidate work must remain bounded and outside the realtime thread.

## Acceptance and provenance

Acceptance makes the rendered audio durable project material. The audio is the result; it must remain
available when its generator is removed. Where useful, additionally preserve generator identity,
version, parameters, random seed, and recipe metadata. A seed/recipe is not a substitute for audio.

Ordinary import/drag-and-drop and accepted Sample Lab output create project-managed durable media,
alongside recordings, used pack audio and other accepted material, including in an unnamed project.
Successful acceptance internalizes/manages the resource: moving/deleting the original arbitrary external
file must not break normal project use. Durability does not depend on the generator, pack or external
folder remaining available. Any advanced deliberate external-reference workflow needs explicit separate
justification and clear distinction from ordinary import;
[PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary) owns storage policy.

Project state may claim durable availability only after successful placement in managed durable storage.
Failed copy/conversion/write or interrupted acceptance must preserve the previously valid saved state.
The managed representation may differ from the source; no transcoding/storage format, source-retention
rule or codec library is chosen, and disposable cache is never the only durable copy. Document recovery
does not recreate produced audio; recording/generation must leave room for reconciliation with separately
managed durable material. Retained Undo/recovery/pending work can still need apparently unused media;
no eager deletion follows visible-reference removal. [PROJECT_FORMAT](PROJECT_FORMAT.md#media-and-persistence-integrity)
owns transaction/cleanup contracts and their open mechanisms.

Missing/corrupt individual media normally degrades its affected portions when the document is safely
understandable. Preserve visible broken-resource/item state and allow inspection, repair/relink,
replacement or removal where meaningful, with unaffected work available. Never silently substitute
unrelated audio; [UX_CONTRACT](UX_CONTRACT.md#project-availability-and-dependency-blockers) owns persistent feedback.

Opening an old project must not silently regenerate accepted audio using a newer algorithm and change
the composition. Regeneration with available code is a separate explicit creative action.
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns storage and compatibility, and [EXTENSIONS](EXTENSIONS.md)
owns missing-generator behavior.

A durable sample and its musical placements are distinct. For example, `kick_017.wav` may supply
items A, B, and C; ordinary editing/processing of A must not unexpectedly rewrite the resource or
change B and C. Non-destructive placement processing is the default. Explicit destructive/edit-source
operations may be considered later but are not implied by normal processing or contextual acceptance.
The exact acceptance edit for a target and shared-use behavior still need design.
[ARCHITECTURE](ARCHITECTURE.md) owns identity boundaries; [PROJECT_FORMAT](PROJECT_FORMAT.md) owns persistence.

## Asynchronous preparation and dependency availability

Generation/preparation/render work needed by user-visible operations requires cancellation, visible
state/progress and finite failure handling under [AUDIO_ENGINE](AUDIO_ENGINE.md#bounded-asynchronous-preparation).
Do not wait forever on stalled workers/plugins; failure leaves canonical state and existing realtime
execution intact. No universal timeout is selected.

Render/resampling freezes and validates/prepares the relevant canonical project revision under
[AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction). Required invalid state or unavailable
dependencies block the affected operation with object-level diagnostics; never silently sample older
playing state or omit required processing and report success. Semantic object boundaries are below;
concrete execution taps and broader capture scope remain open.

## Resampling

Two accepted operations have different meanings. `Create Sample from Object` and `Capture Audible
Selection` are conceptual names, not final labels, commands, or render taps. Ordinary use must not
require a routing questionnaire; advanced raw/source or before-processing alternatives remain open.

### Create Sample from Object

Create durable audio from the selected object's own semantic/local processing boundary:

```text
Kick source -> Kick local EQ -> Kick local Gain -> new durable sample
```

This answers "Turn this object, including its own processing, into reusable audio." It does not
automatically bake unrelated containing-container, Mixer channel/bus, or Master processing merely because those
are heard downstream. For a selected container, its own boundary includes its contained audible items
and container processing; that does not automatically extend to unrelated downstream context.

The selection's semantic owner determines the stop point, independently of where its controls appear:

| Selected object | Included result / stop point |
| --- | --- |
| Pattern musical content without a placement context | Its instrument/sound-definition uses executing its events; no arbitrary Arrangement placement or downstream Mixer context is inherited |
| Pattern placement or another standalone item | That occurrence's contributions and own item-local paths, including its intentional whole-placement submix/processing if present; before containing-context/global processing |
| Musical container with common processing | Its contained item results through its own explicit container submix/processing; before broader downstream channel/bus/Master processing |

For `Drums Main`, a reusable sample may require combining Kick and Snare into the rendered audio:

```text
Kick selected-object contribution  --+
Snare selected-object contribution -+-> object render output mix -> new durable sample
```

Honor the selected object's independent paths before this final render-output convergence. It creates
a combined audio artifact, not an implicit playback bus or a change to the source Pattern's routing.
The resulting mixed sample no longer exposes its constituent instruments independently; the source
remains preserved. If the selected placement already has a whole-placement submix/Compressor, render
its processed result instead of recovering independent instruments from it.

A container Compressor exposed in Mixer is still included when sampling that container, because it
is that container's own context, and is applied once. The same Compressor is excluded when sampling
an individual contained item. An unrelated global bus or Master is excluded in both cases. Shared
route processing of an aggregate containing contributions beyond the object cannot silently count
as exclusively object-owned. Cross-scope sidechain/control dependencies remain Q-066, not an automatic
extension of the object's audio aggregation boundary.
[ARCHITECTURE](ARCHITECTURE.md#arrangement-context-and-mixer-presentation) owns that relationship;
[NODE_GRAPH](NODE_GRAPH.md#contributions-and-irreversible-mixing) owns signal convergence. Exact taps,
dependency closure, musical range/tails, rate/channel negotiation and replacement edits remain open
under Q-012/Q-021/Q-047/Q-049/Q-057; no renderer or capture mechanism is selected.

### Capture Audible Selection

Capture the broader audible result of a selected musical/time context:

```text
selected sources -> containing processing -> relevant downstream audible context -> captured result
```

This answers "Record the sound I am hearing from this selected context/range." Exact downstream
boundaries, buses/sends/Master inclusion, range/tails, and execution taps require later bounded examples.
The operation is not automatically whole-Master capture. Contextual audition may include broader
downstream processing than an object render; hearing that context does not silently expand the object
sampling boundary.

### Source preservation and rendered replacement

Future candidate sources include an instrument playing a note, a pattern, an arrangement range,
multiple selected sources, and generated audio. Converting them to a sample should preserve the
source by default and make the resulting sample immediately reusable.

An explicit replace/disable-source action may be added later, but it must be undoable. A replacement
must not silently reapply the exact processing already baked into its audio:

```text
source -> EQ -> Gain -> rendered sample
```

must not naively become `rendered EQ/Gain audio -> old EQ -> old Gain`. The edit-state transition
remains open: bypass/removal of the baked local chain, retaining it in history, or another explicit
reversible transformation may be evaluated later. None is selected. Accepting a render does not delete
or mutate its source by default. Start with one bounded source in SEQ-R8 rather than treating the full
candidate list or both complete operations as required initial scope.

Musical bounds, tails, insert/send inclusion, routing capture, normalization, sample rate, channels,
latency compensation, and cancellation are open. [AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction)
owns execution/render semantics; [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) records validation gaps.
