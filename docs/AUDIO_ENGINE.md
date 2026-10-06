# Audio engine

Role: Audio execution and realtime boundary contract.
Read when: Designing the audio probe, scheduling, nodes, device I/O, recording, or rendering boundaries.
Authoritative for: Realtime constraints, processing context, backend/device boundary, recording and offline direction.
Not authoritative for: Final language/backend/ABI, musical serialization, extension packaging, or UI design.

## Accepted realtime constraints

Realtime audio is separate from ordinary application work. The eventual execution path must prevent
blocking locks, filesystem/network I/O, UI interaction, unbounded work, uncontrolled allocation,
long/unpredictable callbacks, dependencies on timely UI execution, and avoidable runtime/GC interference.
Preparing resources, decoding files, generation, and persistence belong outside that path.

The engine should own active voices, sample cursors, envelopes, filters/DSP state, the audio clock,
sample-accurate event execution, prepared node execution, and mixer/bus execution state. Document edits
describe intended music; they are not arbitrary mutable objects for the callback to share.

Application control should use bounded commands and/or prepared immutable/snapshot-style state.
Resource publication and retirement need explicit lifetime ownership. The exact queues, snapshots,
capacity, overflow behavior, and shutdown protocol remain undecided.

## Graph execution boundary

[NODE_GRAPH](NODE_GRAPH.md) owns editable graph definitions and semantic connections. The application
may edit rich project/visual objects; the host must validate/prepare a bounded execution representation
before realtime use. The callback must not traverse UI nodes or mutable graph-editor state. No graph
compiler, traversal strategy, publication mechanism, or final execution layout is selected.

Scheduling, node processing, and foundational mixing/routing belong to engine execution even before
the user-facing Mixer milestone. Feedback/cycles, node latency, channel negotiation, and changes during
playback remain validation questions in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).

## Host processing context and backend independence

The host owns the processing environment: current sample rate, frame/block count, channel configuration,
tempo, transport/musical position, loop state, and supported capabilities, with time signature where
relevant. Plugins receive applicable context through Seqvium's processing contract; not every generator
or content pack needs the realtime contract.

Ordinary instrument/effect/generator processing must not depend directly on miniaudio, WASAPI, ASIO,
ALSA, PipeWire, or CoreAudio. Device/backend adapters belong below the host audio engine boundary.
Changing an internal device backend must not require rewriting ordinary processing plugins.

First-party processors must adapt to supported host rates/channel configurations. A general realtime
contract cannot assume fixed `44.1 kHz`; if `96 kHz` operation is supported later, hard-coded `44.1 kHz`
must not make those processors crash. This is an adaptation requirement, not a declaration that any
particular rate/channel range is already supported. Exact ranges and API/ABI remain open.
When processors exist, validation must cover first-party plugins/nodes at the supported host rates
and channel configurations; documenting adaptation alone is not execution evidence.

## Device input/output and recording direction

The core should eventually support selection of audio output, audio input, and MIDI input devices,
plus audio/MIDI recording, guitar or microphone capture through an audio interface, monitoring, and
latency-aware workflows. MIDI input/recording must converge on the musical model in
[ARCHITECTURE](ARCHITECTURE.md); captured audio integrates with host-owned timeline/resources.

Device abstractions and capture ownership may precede polished recording UX. Input monitoring,
clock alignment, capture placement, latency compensation, and device-change behavior remain open.

ASIO is desired for appropriate Windows professional/low-latency hardware in the future. It is not
required by SEQ-R0 and no ASIO SDK/library/backend is adopted. Core processing contracts should leave
room for another device adapter without leaking its API into plugins. Evaluate concrete ASIO licensing
and distribution only when an implementation approaches; [THIRD_PARTY](THIRD_PARTY.md) owns provenance.

## Proposed native direction

The candidate chain is described in [ARCHITECTURE](ARCHITECTURE.md#proposed-application-and-audio-shape).
A native realtime engine behind a narrow boundary, possibly C++ with miniaudio, is **proposed**.
Neither native code alone nor a library choice proves realtime suitability.

SEQ-R0 should examine initialization, callback work, audio clock, basic transport, scheduled sample/tone
events, loops, command/control exchange, and managed/native resource lifetimes. Measurements should
exercise UI/managed pressure and command stress, recording callback timing, overload/underrun evidence
where observable, and recovery/teardown. Numeric acceptance targets must be declared for the probe
environment, not fabricated as product guarantees.

No final device API, backend, sample format, channel layout, latency compensation strategy, or realtime
instrument/effect contract is selected. SEQ-R0 need not build the graph editor, full plugin host, ASIO,
or recording workspace to test a prepared execution/control boundary. [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md)
owns the open questions; [PROJECT_STATE](PROJECT_STATE.md) owns probe status.

## Offline rendering direction

Realtime playback and offline render should share musical scheduling, node processing, and DSP
semantics as much as practical. Offline rendering is a sibling execution target independent of the
device backend, not a route through an active playback device. It must eventually serve WAV export,
resampling, and deterministic DSP/audio tests.

Determinism must be scoped to declared algorithms, seeds, inputs, and numerical assumptions. Do not
promise bit-identical output across arbitrary platforms or plugins without evidence. The probe may
compare a bounded realtime schedule against an offline equivalent; it need not implement full export.

[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) owns resampling source and acceptance semantics. Tail handling,
render bounds, effect latency, cancellation, and exact equivalence remain open. No audio engine or
renderer exists at this milestone.
