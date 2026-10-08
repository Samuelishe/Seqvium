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
musical ceiling. Seqvium-authored work uses Apache-2.0; [THIRD_PARTY](THIRD_PARTY.md) owns the
licensing/provenance boundary and the root [LICENSE](../LICENSE) contains the authoritative text.

Seqvium is a universal music workstation, not genre-locked. Beats/electronic music, ambient,
experimental and sample-based work, instrument/plugin use, guitar or microphone recording through
an audio interface, MIDI recording, and mixing those sources belong to its long-term direction.
Audio/MIDI input and recording are core platform responsibilities, not an electronic-music ceiling.

## Sound and music lead

The first path should approach: open Seqvium, discover or add a sound, write a rhythm, bass line, or
melody, and hear it immediately. Editing, arrangement, mixing, and sound design should become
available as the work requires them.

Seqvium should particularly reward experimentation, imagination, discovery, fast feedback, finding
one's own sounds, and transforming musical material. The primary object is the sound and music being
created. UI and architecture serve that object; they must not become the central creative task.

Pleasure, ease, tactile manipulation, flexibility, and visual quality are part of the product. Seqvium
should feel like a creative instrument: low ceremony, clear hierarchy, good spatial behavior, and
expressive but restrained visuals that invite experimentation. Advanced depth appears when requested.
Coherent interaction quality, not decorative animation everywhere, is the design target;
[UI_DESIGN](UI_DESIGN.md) owns the evolving visual/interaction guide.

A defining direction is the loop from sound exploration to durable samples, music, resampling, and
further reuse. [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) owns its semantics. A default-installed,
removable creative generator should make both intentional and casual exploration useful. A core
node graph adds depth for sound/control transformation without forcing graph internals into the
first musical task; [NODE_GRAPH](NODE_GRAPH.md) owns that direction.

Sample Lab supports standalone discovery and exploration against selected musical context, with
temporary audition through the relevant existing processing before explicit acceptance. Ordinary
object sampling should include that object's own processing; broader audible-context capture is a
distinct intention. These creative principles and their unresolved technical boundaries belong to
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md).

Creation and sound exploration are the workflow specialization: fast transformation, sample exploration,
resampling, graph-based processing, and little ceremony between imagination and audible result.
Universality must not turn into feature-count competition over every historical studio recording workflow.

## Creation, musical structures, and organization

Seqvium is **mouse-first, keyboard-efficient**. Polished direct pointer manipulation leads creation
and editing; basic workflows must be approachable without memorized shortcuts. Keyboard editing is a
complementary path of equal interaction quality for precise and repetitive work, navigation, selection
and action invocation. It uses the same musical/document model. On-screen musical keyboard,
MIDI/realtime note input, and eventual audio recording complement creation; optional typing-keyboard
musical input does not define ordinary command behavior. [UX_CONTRACT](UX_CONTRACT.md) owns input semantics.

Named reusable multi-instrument patterns let users choose the size of a musical idea, from a drum
part to a full groove. User-defined instrument/channel groups provide flexible organization independently
of pattern identity. [ARCHITECTURE](ARCHITECTURE.md) owns those relationships. Major surfaces normally
compose as flexible internal panes in one main window, under [WORKSPACE](WORKSPACE.md).

The accepted creative direction combines non-destructive item-local processing and shared processing
of a containing musical container with a semi-free, user-named Arrangement. Global routing remains
available beyond those two local levels. [ARCHITECTURE](ARCHITECTURE.md) owns the relationships;
container terminology and its exact domain identity remain open.

## References and non-goals

FL Studio is the nearest workflow reference, but Seqvium must develop its own creative character.
Ableton Live, Bitwig, Reaper, LMMS, Ardour, Zrythm, Bespoke Synth, and grooveboxes may supply specific
ideas. None is a specification.

Seqvium must not become a toy beat maker that quickly reaches its ceiling, an FL Studio clone, or a
generic professional DAW that requires understanding extensive routing before making a sound.
Avoid feature-count competition. Prefer a smaller number of coherent creative workflows.

Core audio/MIDI/recording direction does not promise early delivery. Automation, richer instruments/effects,
external plugin hosting, and pitch/time processing remain later capability work; [ROADMAP](ROADMAP.md)
owns staging. Seqvium does not prioritize supporting every historical recording workflow over its
creative focus.

## Feature-fit questions

- Does this shorten the path from an idea to hearing and developing it?
- Does it help discovery or turn existing material into useful new material?
- Can it be simple by default, discoverable, and deeper when needed?
- Does it help someone finish music without adding unrelated ceremony?

High quality interaction is a product requirement, not a luxury postponed because the project is FOSS.
[UX_CONTRACT](UX_CONTRACT.md) owns the observable principles; [UI_DESIGN](UI_DESIGN.md) guides visual quality.
