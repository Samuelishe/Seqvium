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

Substitution mechanics, active-target identity, stop/cancel/restore behavior, realtime publication,
downstream boundaries, and handling unavailable context remain open. They require later bounded
design and evidence, not a presumed implementation here.

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

Accepted Sample Lab audio becomes project-managed media under the normal self-contained project path,
alongside used imported/pack audio and recordings. Durability does not depend on the original generator,
pack, or external folder remaining available. Explicit external-reference workflows are an alternative,
not the ordinary default; [PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary) owns storage policy.

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
automatically bake unrelated containing-container, bus, or Master processing merely because those
are heard downstream. For a selected container, its own boundary includes its contained audible items
and container processing; that does not automatically extend to unrelated downstream context.

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
