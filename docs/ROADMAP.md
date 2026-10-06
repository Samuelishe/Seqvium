# Roadmap

Role: Ordered development direction.
Read when: Scoping the next stage or evaluating a future capability.
Authoritative for: Stage goals, sequence, and scope boundaries.
Not authoritative for: Completion status, factual history, accepted technical decisions, or existing debt.

This is a direction, not a promise to implement all capabilities in v0.1. Later ordering may change
with evidence and product need. Current status belongs to [PROJECT_STATE](PROJECT_STATE.md), completed
facts to [WORK_LOG](WORK_LOG.md), and technical choices to their owners and [DECISIONS_LOG](DECISIONS_LOG.md).

## SEQ-KB-R0 — Repository Knowledge Foundation

Define product direction, canonical owners, task routing, and documentation governance. Documentation
only: no production application, dependencies, engine, tooling framework, or tests for nonexistent code.

## SEQ-R0 — Audio Architecture Probe

Validate the riskiest architecture before application construction. Scope a minimal experimental host,
device initialization, realtime callback, audio clock, basic transport, scheduled sample/tone events,
loop boundaries, bounded command/control path, instrumentation, and stress behavior. Compare a
device-independent offline equivalent where useful.

Evaluate managed/native feasibility, ownership/lifetime, callback behavior under managed pressure,
and native binary distribution implications. Record setup, measurements, limitations, and a reasoned
accept/reject/narrow recommendation. A backend/language/ABI becomes accepted only through an explicit
evidence-backed decision. See [AUDIO_ENGINE](AUDIO_ENGINE.md) and the [experiment guide](experiments/README.md).

## SEQ-R1 — Domain and Project Foundation

Establish project/document ownership, musical-time primitives, instrument identity, events, parties,
patterns, clips, justified stable IDs, undo/redo, and a bounded versioned serialization foundation.
Avoid UI-heavy implementation. Preserve the intended model in [ARCHITECTURE](ARCHITECTURE.md) and
compatibility direction in [PROJECT_FORMAT](PROJECT_FORMAT.md).

## SEQ-R2 — Sample Foundation

Establish audio resources, WAV import, preview, managed project media, a simple sampler, note/pitch
playback, and resource lifetime. Do not expand immediately to every codec or sampler feature.

## SEQ-R3 — Pattern Workspace

Create the first genuinely musical editing workflow: Channel Rack, Step Sequencer, pattern looping,
several channels, velocity, and responsive live editing over extensible musical events.

## SEQ-R4 — Extension Foundation

Implement enough identity, manifest/package concepts, local discovery, content-pack boundaries,
sample-generator capability contract, and lifecycle/error/missing-state handling to make first-party
optional modules honest extensions. No store or marketplace. [EXTENSIONS](EXTENSIONS.md) owns the boundary.

## SEQ-R5 — Sample Lab / Generator V1

Ship one default-installed but removable generator package with one or two bounded families. Provide
random and nearby/similar variants, justified parameter locks, candidate history, solo and in-pattern
audition, and acceptance as durable audio. Follow [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md).

## SEQ-R6 — Resampling

Render a pattern or another bounded selected source to a reusable sample. Preserve the source by
default, define render/tail behavior, and enable immediate reuse. Any optional replace-with-sample
operation must be undoable. Full arrangement/multiple-source rendering need not arrive together.

## SEQ-R7 — Piano Roll

Add richer pitch, duration, and velocity editing over the same underlying musical event model as
the Step Sequencer.

## SEQ-R8 — Arrangement

Add Playlist/Arrangement with clips in musical time, normal shared pattern references, and explicit
independent variations. Determine a bounded audio-clip scope rather than assuming a full DAW timeline.

## SEQ-R9 — Mixer

Add channels, master, gain, pan, mute, solo, and the first bounded built-in DSP. Mixer channels remain
distinct from arrangement tracks. Deeper sends/routing should follow demonstrated need.

## SEQ-R10 — First Track Release

Reach a version in which a user can reasonably finish a small track: strengthened save/load,
arrangement, basic mixing/effects, WAV export, packaging, and recovery/error handling suitable for
real projects. This is a usability/integrity goal, not a feature-count target or assigned version number.

## SEQ-R11 — Extension Ecosystem

Add mature install/update/remove management and extension diagnostics after real modules establish
the lifecycle and compatibility requirements.

## SEQ-R12+ — Evidence-led expansion

Consider automation, MIDI, recording, richer synthesizers/effects, CLAP/VST3 hosting, pitch/time
processing and time stretching, deeper routing/sends, FLAC and other justified formats, and additional
platforms. Latency awareness and realtime safety constrain applicable earlier work; advanced
compensation is not presumed implemented. Later order and release schedules remain open.
