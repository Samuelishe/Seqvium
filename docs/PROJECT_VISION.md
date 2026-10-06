# Project vision

Role: Product identity and long-term product boundary.
Read when: Evaluating feature fit, audience needs, or creative tradeoffs.
Authoritative for: Identity, audience, creative philosophy, high-level goals and non-goals.
Not authoritative for: Exact interactions, technical design, implemented capability, or stage order.

## Identity and audience

Seqvium is intended to be a free and open-source desktop music workstation. Its governing principle is:

> Easy to start, deep enough to grow.

It serves people starting to make music and people who want to grow from short ideas into finished
tracks without abandoning an approachable tool. Simple defaults must not impose an artificially low
musical ceiling. FOSS intent is accepted; the final license is still open.

## Sound and music lead

The first path should approach: open Seqvium, discover or add a sound, write a rhythm, bass line, or
melody, and hear it immediately. Editing, arrangement, mixing, and sound design should become
available as the work requires them.

Seqvium should particularly reward experimentation, imagination, discovery, fast feedback, finding
one's own sounds, and transforming musical material. The primary object is the sound and music being
created. UI and architecture serve that object; they must not become the central creative task.

A defining direction is the loop from sound exploration to durable samples, music, resampling, and
further reuse. [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) owns its semantics. A default-installed,
removable creative generator should make both intentional and casual exploration useful.

## References and non-goals

FL Studio is the nearest workflow reference, but Seqvium must develop its own creative character.
Ableton Live, Bitwig, Reaper, LMMS, Ardour, Zrythm, Bespoke Synth, and grooveboxes may supply specific
ideas. None is a specification. Fovium supplies the analogy of allowing the primary creative object
to dominate; its photograph-specific zero-UI rules are not Seqvium requirements.

Seqvium must not become a toy beat maker that quickly reaches its ceiling, an FL Studio clone, or a
generic professional DAW that requires understanding extensive routing before making a sound.
Avoid feature-count competition. Prefer a smaller number of coherent creative workflows.

The long-term capability space includes richer editing, instruments/effects, automation, recording,
MIDI, plugin hosting, and pitch/time processing. These are candidates, not initial release promises;
[ROADMAP](ROADMAP.md) owns their staging.

## Feature-fit questions

- Does this shorten the path from an idea to hearing and developing it?
- Does it help discovery or turn existing material into useful new material?
- Can it be simple by default, discoverable, and deeper when needed?
- Does it help someone finish music without adding unrelated ceremony?

High quality interaction is a product requirement, not a luxury postponed because the project is FOSS.
[UX_CONTRACT](UX_CONTRACT.md) owns the observable principles.
