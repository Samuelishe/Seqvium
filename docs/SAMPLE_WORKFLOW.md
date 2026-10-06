# Sample workflow

Role: Seqvium's sound exploration and material-transformation contract.
Read when: Designing discovery, generation, audition, sample acceptance, or resampling.
Authoritative for: Sample Lab context, candidate audition/acceptance, durable audio, what-you-hear resampling.
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

The accepted human-facing default is **what-you-hear sampling**: creating a sample from musical
material should correspond to what the user meaningfully hears from the selected source/context.
Processing should not unexpectedly fall off because returning dry/raw audio is easier internally.
Advanced alternatives may later offer audible/current result, before-local-processing, raw/source,
or other meaningful tap concepts. These are not final UI labels; ordinary use must not require a
professional routing questionnaire.

What-you-hear follows the selected semantic source, not necessarily the whole Master output:

| Selected source/context | Conceptual audible result |
| --- | --- |
| One musical item / sample-clip placement | Source through that item's local processing into a new durable sample |
| A containing musical container | Contained audible items through container processing into a new durable sample |
| Explicit Arrangement/time range or broader scope | Audible sources included by that selected scope |

These examples describe meaningful boundaries, not selected DSP taps. Human-semantic selection
comes first; mapping it to an execution tap comes later. Exact mixer/send/master inclusion, routing,
tails, latency, and render equivalence remain unresolved. Contextual audition may include relevant
downstream mix processing while item sampling ends at the item's semantic boundary; the two operations
need not capture the same signal scope.

Future candidate sources include an instrument playing a note, a pattern, an arrangement range,
multiple selected sources, and generated audio. Converting them to a sample should preserve the
source by default and make the resulting sample immediately reusable.

An explicit replace/disable-source action may be added later, but it must be undoable. Do not assume
that accepting a render deletes or mutates its source. Start with one bounded source in SEQ-R8,
rather than treating the full candidate list as required initial scope.

Musical bounds, tails, insert/send inclusion, routing capture, normalization, sample rate, channels,
latency compensation, and cancellation are open. [AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction)
owns execution/render semantics; [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) records validation gaps.
