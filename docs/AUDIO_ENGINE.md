# Audio engine

Role: Audio execution and realtime boundary contract.
Read when: Designing the audio probe, scheduling, nodes, device I/O, recording, or rendering boundaries.
Authoritative for: Realtime constraints, processing context, backend/device boundary, recording, canonical offline render and finite preparation failure.
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

### Bounded overload and semantic recovery

Seqvium must not convert missed realtime deadlines into an ever-growing execution backlog or endlessly
increasing latency. Realtime queues/work must have bounded capacity/behavior. Recovery returns to
current realtime progress rather than accumulating seconds of obsolete audio work to catch up later.
A dropout/glitch is preferable to unbounded latency, memory growth, or progressively falling behind.
This is a realtime semantic constraint, not a choice of UDP or any other transport.

Different data needs different overload handling:

- Rapidly superseded control/UI values may coalesce or use latest-wins behavior where semantics permit.
- Obsolete graph-preparation generations may be abandoned when a newer requested generation supersedes them.
- Musical events require ordered/semantic handling; they must not be naively dropped like disposable UI updates.
- Critical musical/control state, including stop/release/panic semantics, needs explicit eventual recovery behavior.

Exact scheduler/overflow policies, ring buffers, queue sizes, lock-free structures, and recovery algorithms
remain open. SEQ-R0 must later provide bounded evidence; this direction neither selects mechanisms nor
requires the probe to solve the full musical scheduler.

## Graph execution boundary

[NODE_GRAPH](NODE_GRAPH.md) owns editable graph definitions and semantic connections. The application
may edit rich project/visual objects; the host must validate/prepare a bounded execution representation
before realtime use. The callback must not traverse UI nodes or mutable graph-editor state. No graph
compiler, traversal strategy, publication mechanism, or final execution layout is selected.

Execution snapshots derive from the single canonical editable project graph and identify its revision.
Last-valid execution may continue in the current session while canonical edits are invalid or preparing;
invalid revisions never publish. The latest valid prepared revision automatically replaces older
execution atomically with safe retirement; no manual Apply is required. Obsolete preparation may be
cancelled/coalesced safely, separately from undo history. Save/reopen preserves canonical edits under
[PROJECT_FORMAT](PROJECT_FORMAT.md#save-and-reopen), not a persistent last-valid runtime graph.
[NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution) owns this graph rule and the required
visible distinction between editable and executing state; publication/resource retirement remains open.

Shared instrument/sound definitions do not force shared execution state. Overlapping placements with
different required local/downstream processing must remain separable until that difference is honored;
audio mixed irreversibly beforehand cannot supply independent results afterward. The model requirement
belongs to [ARCHITECTURE](ARCHITECTURE.md#processing-granularity-and-shared-definitions).
Voice groups, instances, prepared routes, or another bounded strategy remain unselected. Separation
can cost CPU/memory; a third-party instrument may require explicit instance duplication or another
bounded strategy. Q-047 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks execution/performance evidence,
without requiring a particular instance count or free duplication.

Scheduling, node processing, and foundational mixing/routing belong to engine execution even before
the user-facing Mixer milestone. Feedback/cycles, node latency, channel negotiation, and changes during
playback remain implementation/validation questions in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).

## Source boundaries and effect tails

Ending or trimming source input does not necessarily destroy already-running effect state. Ordinary
source-end behavior may allow delay/reverb or similar processing tails to continue after new input stops:

```text
source audio ----|
                 |~~~~ effect tail ~~~~
```

Source/input boundary, continuing processing tail, and explicit hard cut are distinct intentions.
The user must also be able to request an explicit hard boundary where resulting sound after that point
is intentionally silenced or otherwise terminated. Exact hard-cut/node-state reset mechanics and
controls remain open. [UX_CONTRACT](UX_CONTRACT.md#audio-timeline-editing) owns understandable editing intentions.

### Loop, seek, and playback Stop

Ordinary looping may let an existing effect tail continue across the musical loop boundary while the
source starts its next iteration. The loop mechanism must not recursively feed previously rendered
tails back as new source or create unbounded duplicate processing state each iteration. Intentional
growth from an effect's own feedback/routing is a separate DSP matter; feedback safety and reset rules
remain open.

Seeking, scrubbing, or jumping to another timeline location must not leave transient playback/tail
state misleadingly sounding from the previous location. Users should hear destination context.
State reconstruction/warm-up for stateful DSP remains open; no seek algorithm is selected.

Normal playback Stop uses a fast bounded settling/fade/de-click direction where appropriate to avoid
an ugly instantaneous digital cut. It must not continue ordinary musical tails for seconds after Stop.
Exact duration/shape and processor transition mechanics remain open. This transport settling behavior
does not reinterpret an explicit hard cut in the project.

### Recording stop boundary

Record Stop is distinct from playback Stop. Recorded material ends at the intended recording boundary,
subject later to defined device/latency alignment. Playback tail/de-click behavior must not silently
extend the recorded resource. Recording is not playback Stop plus whatever tail policy runs;
capture timing/latency remains open under the recording direction below.

Q-057 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks stateful transport/reset/warm-up mechanisms;
Q-027 separately tracks capture alignment. These accepted intentions do not select processor algorithms.

## Sound compatibility boundary

Structural/data compatibility and exact historical sonic identity are separate promises. Seqvium
does not promise indefinite bit-identical sonic emulation of every historical platform/audio-engine
version. Avoid gratuitous changes to accepted semantics and preserve/migrate project data, while
allowing intentional platform fixes/evolution. There is no permanent old-engine compatibility-mode
requirement. Exact handling of any future genuinely breaking audio change remains a deliberate open
policy for that change, not permission to discard project data.

Third-party/optional plugin algorithms own their version-specific sound under
[EXTENSIONS](EXTENSIONS.md#plugin-sound-responsibility); the host does not promise to emulate them.

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

Ordinary users select logical audio input/output devices/endpoints, not backend libraries. Input and
output are separate selections where platform/device architecture supports it. An analog/condenser
microphone through an interface is represented by the interface input endpoint/channel (for example
`Steinberg UR12 — Input 1`); it is not necessarily a separate OS device. A USB microphone may be its
own input device. [SETTINGS](SETTINGS.md#audio-device-selection) owns ordinary selection UX.

Backend/API/driver integration is an internal platform responsibility. Exact backend selection,
ASIO/device APIs, clock domains, rate/channel/buffer negotiation and device-loss/recovery remain
technical evidence/design questions (Q-026/Q-027/Q-062/Q-069), including project intent versus runtime
facts. Device abstractions and capture ownership may precede polished recording UX; endpoint UX does
not solve monitoring, clock alignment, capture placement, latency compensation or safe device changes.

ASIO is desired for appropriate Windows professional/low-latency hardware in the future. It is not
required by SEQ-R0 and no ASIO SDK/library/backend is adopted. Core processing contracts should leave
room for another device adapter without leaking its API into plugins. Evaluate concrete ASIO licensing
and distribution only when an implementation approaches; [THIRD_PARTY](THIRD_PARTY.md) owns provenance.

## Live / Low-Latency direction

A future Live / Low-Latency behavior is accepted product/platform direction for realtime performance
and monitoring. Processors may introduce meaningful algorithmic latency; Seqvium should eventually
identify latency-heavy processing on a live path and provide an explicit low-latency operating mode.
In that mode such processing may be temporarily bypassed or otherwise handled under future engine policy.

This operating behavior must not silently rewrite the project/graph. Users can see that the live path
differs from full processing; returning to normal mode restores the intended full graph. Offline/final
rendering uses the intended full processing path, not temporary live bypass state.
[UX_CONTRACT](UX_CONTRACT.md#live-processing-feedback) owns the visible distinction.

Latency thresholds, processor reporting contract, compensation strategy, and bypass algorithm remain
open in Q-021 of [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). This is future direction, not an additional
SEQ-R0 implementation requirement.

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

## Bounded asynchronous preparation

User-visible preparation, including export/render preparation, must have bounded failure behavior:
cancellation, visible progress/state, detection of dead/stalled external work where technically possible,
and finite failure handling while keeping the UI responsive. Export must not wait forever for a stalled
worker/plugin. No arbitrary universal timeout is selected; watchdogs/timeouts require implementation
evidence (Q-018 and affected lifecycle questions).

Failure leaves canonical project state unchanged and reports the responsible preparation/dependency;
it must not corrupt the existing realtime snapshot. Before publishing asynchronous work, revalidate
project/target/context and ownership/revision preconditions under
[ARCHITECTURE](ARCHITECTURE.md#document-integrity-and-asynchronous-publication). Only a valid,
relevant prepared revision may reach the engine; exact mechanisms remain open.

## Offline rendering direction

Export/render operates from a frozen canonical project revision:

```text
freeze canonical revision -> validate -> prepare offline execution -> render
```

If required canonical state is invalid or its dependency closure has unresolved blockers, block export
and identify the affected objects/dependencies. Do not silently omit required music/processing or report
success from an older realtime snapshot. Healthy unrelated paths are usable where their semantics permit;
render scope/taps remain owned by the relevant workflow. No export-last-playable-version workflow is
accepted. Runtime snapshots are not creative versions/checkpoints.

Realtime playback and offline render should share musical scheduling, node processing, and DSP
semantics as much as practical. Offline rendering is a sibling execution target independent of the
device backend, not a route through an active playback device. It must eventually serve WAV export,
resampling, and deterministic DSP/audio tests.

Determinism must be scoped to declared algorithms, seeds, inputs, and numerical assumptions. Do not
promise bit-identical output across arbitrary platforms or plugins without evidence. The probe may
compare a bounded realtime schedule against an offline equivalent; it need not implement full export.

Export/render must reproduce the project's intended audible semantics without adding an aesthetic
interpretation. Preserve an explicit hard cut; preserve a permitted processing tail according to the
selected render scope. Do not restore a deliberately cut tail, remove intended audible tail behavior,
add effects, or otherwise "improve" the project. Temporary playback Stop settling is not an instruction
to append audio to exports or recordings.

**Manual export-range semantics remain open:** when a valid tail extends outside a selected bounded
range, decide whether that range defaults to a hard render boundary or an explicit include-tails-like
option is appropriate (Q-056 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md)). This stage chooses neither.
Exact effect latency, cancellation, reset/warm-up, and realtime/offline equivalence also need evidence
in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) owns resampling source and acceptance semantics; no engine or
renderer exists at this milestone.
