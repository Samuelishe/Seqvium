# Sample workflow

Role: Seqvium's sound exploration and material-transformation contract.
Read when: Designing discovery, generation, audition, sample acceptance, or resampling.
Authoritative for: Creative loop, candidate interaction, accepted audio durability, source-preserving resampling.
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

The transition between exploration and musical context should be unusually cheap. A generated
candidate should be auditionable alone and in the selected instrument's current pattern, without
repeated manual export/import/reconfiguration. Detailed preview substitution, stop/restore behavior,
and handling of an absent selected pattern remain design questions; audition must not silently become
acceptance or rewrite committed musical material.

## Specialized generation in a shared exploration surface

Prefer meaningful specialized generators or modes inside one exploration surface over a gigantic
universal synthesizer displaying every algorithm simultaneously. Possible families include percussion,
clicks/impacts, noise/rhythmic noise, waves/wind/atmospheres, synthetic or physical-model-inspired
plucks, guitar/string-like sounds, and experimental textures. These are ideas, not V1 scope.

The first generator should be local/procedural and cover one or two bounded families.
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

## Resampling

Future candidate sources include an instrument playing a note, a pattern, an arrangement range,
multiple selected sources, and generated audio. Converting them to a sample should preserve the
source by default and make the resulting sample immediately reusable.

An explicit replace/disable-source action may be added later, but it must be undoable. Do not assume
that accepting a render deletes or mutates its source. Start with one bounded source in SEQ-R6,
rather than treating the full candidate list as required initial scope.

Musical bounds, tails, insert/send inclusion, routing capture, normalization, sample rate, channels,
latency compensation, and cancellation are open. [AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction)
owns execution/render semantics; [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) records validation gaps.
