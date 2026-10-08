# Audio engine

Role: Audio execution and realtime boundary contract.
Read when: Designing the audio probe, scheduling, nodes, device I/O, recording, or rendering boundaries.
Authoritative for: Realtime constraints, execution-state lifetime/resource integrity, processing context, backend/device boundary, recording, item-local hard boundaries, manual export ranges, finite tails, canonical offline render and finite preparation failure.
Not authoritative for: Final language/backend/ABI, musical serialization, extension packaging, or UI design.

## R2-F1 offline sampler foundation

The reviewed direction is an initial bounded C# scheduler/DSP implementation with replaceable
execution/device ownership. [F1 source](../src/Seqvium.Core/Audio/OfflineSampler.cs) implements resource/event
preparation and offline execution, without a production callback or device adapter. R0's native structs,
four-slot scheduler and 48 kHz endpoint are not reused. A permanent engine/ABI remains unselected;
F2 must provide intended real-WAV/device/managed-pressure evidence before broader claims.

### WAV and decoded ownership

[WavDecoder](../src/Seqvium.Core/Media/WavDecoder.cs) supports little-endian RIFF/WAVE PCM16 and IEEE float32,
mono/stereo, 44,100/48,000 Hz. Supported `fmt` representations are 16 bytes, 18 bytes with zero extension,
and 40-byte WAVE_FORMAT_EXTENSIBLE with full valid bits and PCM/float subtype GUIDs. Extensible masks
are unspecified (0), mono front-center (4), or stereo front-left/right (3), preserving L/R ordering;
other masks/container/valid-bit configurations are explicit refusals. These variants correspond to
ordinary [Microsoft extensible format definitions](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ksmedia/ns-ksmedia-waveformatextensible).
No compressed codecs, RF64/RIFX, PCM24/32, other rates/channel layouts, wavl/multiple data chunks,
sample loops or generic codec plugins are supported.

Require exact RIFF length, one prior `fmt` and one nonempty frame-aligned `data`, consistent byte rate/
block alignment and valid extension/chunk bounds including odd padding. Unknown bounded chunks,
including metadata/fact, are skipped; a fact chunk is not required for common float files. Do not use
declared chunk lengths to allocate. Source input is limited to 16 MiB, decoded interleaved floats to
at most 32 MiB per resource. PCM16 scales by 32768; finite float overload saturates to [-1,1]; NaN/Inf
refuse the resource. Decode/read/hash/storage happen outside execution. Durable source bytes are
separate under [project format](PROJECT_FORMAT.md#r2-f1-managed-wav-layout).

`DecodedPcm` owns an immutable private sample array; Dispose releases its cache reference. Prepared
and live execution hold independent leases to the same immutable array. Disposing a document/cache/
prepared owner cannot invalidate an already created execution; execution disposal retires its leases.
Owners are serialized; no concurrent lease/dispose or realtime reclamation protocol is claimed.

### Pitches, voices and output

`core.pcm-sampler.linear-v1` uses exactly one resource and explicit decimal `rootPitch` (0–127) and
`releaseMilliseconds` (0–1000) in R1's sound parameters. MIDI-like note pitch permits fractional
semitones within root ±12 and canonical [0,127]. Source cursor step is
`sourceRate / executionRate * 2^((notePitch - rootPitch) / 12)`; root playback adapts source rate.
Linear interpolation uses adjacent samples, interpolating the final sample toward zero. Cursor is
derived from integer voice age, avoiding partition-dependent incremental floating-point drift.
This bounded choice permits direct ramp oracles; it promises no anti-aliasing/high-quality stretching.

Gain is canonical intensity directly in [0,1]. Mono duplicates to stereo; stereo preserves L/R or
downmixes to mono with `(L+R)/2`. Independent voices sum into float output with headroom (no limiter,
pan law, Master or FX). Note Off targets one `(Pattern, part, note, iteration)` execution occurrence,
starts a linear release of `round(releaseMilliseconds * executionRate / 1000)` frames (ties later),
or immediately retires when zero. Release gains are 1, (N-1)/N, ..., 1/N over N frames. Natural EOF may
retire earlier. One note/iteration never resets another voice using the same sound/resource.

Plans explicitly support 1–8 voice slots, with tested 1/4/8 configurations; these are F1 acceptance
bounds, not a permanent universal ceiling or mono/legato/voice-stealing policy. Preflight considers
duration, release, EOF and stop; an over-capacity plan is rejected wholly before output. All sources,
including zero-frame note dependencies, must be understood and available. Unsupported algorithms,
opaque sound state, extra sound parameters, relevant unknown Pattern/part/note/sound data and required
placement processing/route/shared-performance relationships are refused, rather than silently omitted.
Full execution domains, separate contribution outputs and plugins remain Q-047.

### Timing, processing ranges and determinism

Preparation freezes one current canonical revision and one Pattern. It supports a nonnegative absolute
musical start and 1–1024 bounded repetitions; it does not render Arrangement placements. Every note
start/end and iteration boundary is converted independently from absolute ticks with R1's exact
constant-tempo mapper. No rounded duration/loop-period accumulation occurs. Events retain musical
positions/IDs separately from Int64 derived execution frames. Rate changes never rewrite musical intent.

Same-frame policy is Stop before Note Off before Note On; within a kind, absolute tick, part UUID,
note UUID and iteration give deterministic order consistent with R1 equal-position starts. Positive
notes whose rounded start/end coincide increment `ZeroFrameNotes` and emit neither On nor Off;
canonical duration remains positive. The final hard boundary excludes all starts/releases there and
retires every voice. An explicit musical Stop selects that boundary exactly. Without explicit Stop,
offline completion allows at most the largest configured release after the absolute final Pattern
boundary; the final Stop marker retains that musical boundary and its derived frame includes this
bounded technical tail allowance. Ordinary repeats let prior releases continue independently.

`OfflineSampler.Process` consumes caller-owned interleaved float spans over sequential [start,end)
blocks. A boundary event belongs to the next block; reaching the final end retires voices immediately.
Blocks cannot exceed the prepared range or split a frame. Stop clears technical execution state;
Restart clears voices/event cursor and starts the same frozen plan deterministically. This offline
hard stop does not choose future user-facing realtime de-click/settling or seek-state reconstruction.

Working state is fixed slots plus immutable events/sources: at most 100,001 events and 128 MiB retained
decoded PCM per plan. Preparation can temporarily hold one additional bounded resource/source buffer;
output memory belongs to the caller and may be streamed in bounded blocks. No filesystem, decode,
document traversal or output allocation occurs in Process. Same declared inputs are partition invariant
locally; sample oracles use absolute tolerance 2e-6. Finite output, 120/137 BPM and both execution rates
are tested. Cross-platform numerical parity, realtime deadlines/allocations and device output remain
unevidenced. This is not R8 resampling, final WAV export, full transport, graph or Mixer execution.

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

The [semantic execution domains](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains) in
ARCHITECTURE distinguish shared durable sound intent from performance-state interaction and required
contribution independence, including overlapping Bass uses and mono/legato/voice stealing.
Prepared execution must honor the contribution paths and intentional convergence boundaries in
[NODE_GRAPH](NODE_GRAPH.md#contributions-and-irreversible-mixing); sharing a definition or presenting
one context in Arrangement and Mixer cannot authorize an earlier sum or duplicate DSP. Item-local
hard boundaries apply to the selected occurrence's own result before shared downstream state,
including its intentional whole-placement submix when used. Concrete output separation, state lifetime
and external-host feasibility still require Q-047/Q-057 evidence; no instance count is selected.

Scheduling, node processing, and foundational mixing/routing belong to engine execution even before
the user-facing Mixer milestone. Feedback/cycles, node latency, channel negotiation, and changes during
playback remain implementation/validation questions in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).

## Execution-state lifetime and resource integrity

Ending/releasing Placement A must target its owned events/contributions and local processor state.
It must not reset Placement B's domain, voices or local Delay merely because both use the same sound
definition. In a shared performance domain, A's event release may have the intended mono/legato/voice
consequences, but it is not a global reset of all members. Retire the domain only when no continuing
member or permitted state/tail requires it; definition lifetime is a separate shared-resource concern.
If an occurrence's required independent hard cut or state retirement cannot be honored inside a
shared source, that grouping is invalid and needs semantic separation or an explicit blocker.

A's independent source state or local Delay may outlive its natural input end where tails are permitted.
Its explicit hard right boundary still ends its own local result under
[source boundaries](#source-boundaries-and-effect-tails). Neither action clears B nor arbitrary shared
downstream state after intentional mixing. Exact tail completion, reset, de-click, seek reconstruction
and retirement mechanics remain Q-057; publication/state transfer and safe resource lifetime remain
Q-018. No tail algorithm or unlimited retention is implied. Cross-context consumers cannot extend a
source occurrence or keep arbitrary execution alive forever merely because its canonical connection
still exists; required signal/history availability follows the selected boundary and timing scope.

Correct required audio has priority over hidden resource shortcuts. Never silently merge independent
performance domains/contributions, collapse required routes, replace requested sound with stale/wrong
audio or omit required processing to save CPU/RAM. Bounded execution/preparation/resource use and useful
diagnostics are legitimate requirements; there is no unlimited-duplication guarantee or numerical limit.

If the requested configuration cannot be realized within available resources/capabilities, fail its
preparation or make affected execution explicitly unavailable/degraded, preserving canonical intent.
Keep document access and healthy paths where semantics permit; block render whose canonical dependency
closure requires the unavailable execution. A supported semantics-preserving fallback is possible;
a behavior-changing alternative requires an explicit informed choice, never a hidden rewrite.
The existing visibly identified last-valid in-session revision may continue under
[NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution); it must not be reported as the requested
new configuration or used as canonical render. [UX](UX_CONTRACT.md#project-availability-and-dependency-blockers)
owns observable blocker feedback; final overload UX, thresholds and resource policy remain open.

### Remaining execution evidence

Q-047 requires bounded comparisons of compatible sharing versus independent domains, voice interaction,
separable source outputs, opaque-source instancing, shared-definition parameter synchronization and
CPU/RAM scaling, including pressure/failure and lifetime cases. Concrete grouping, allocation, pooling,
instance counts, limits and realtime publication remain open. Q-019/Q-029 own canonical references/edit
relationships; Q-063 owns undo/async commit, and Q-066 retains concrete cross-context control/sidechain
execution mechanisms under the [graph semantics](NODE_GRAPH.md#cross-context-signal-and-control-relationships).
These questions are coordinated, not fully resolved here.

Within its existing authorized scope, a future [SEQ-R0](ROADMAP.md#seq-r0--audio-architecture-probe) can
provide baseline sample/tone event ownership, prepared-state lifetime, bounded control/overload and
realtime/offline comparison evidence if explicitly scoped. It cannot establish arbitrary mono/legato
or plugin behavior, final domain grouping, host capability negotiation, plugin instance synchronization
or product-wide CPU/RAM bounds. A later separately authorized bounded overlapping-source/opaque-host
evaluation is needed for those claims; no experiment or implementation stage is inserted or started.

## Source boundaries and effect tails

A natural source end may allow stateful item-local delay/reverb or similar processing tails to continue
after source input stops:

```text
source audio ----|
                 |~~~~ effect tail ~~~~
```

A deliberately shortened/trimmed clip/item right boundary expresses that this item's audible result
ends here. It is a hard audible boundary for the item's own/object-local result, including its local
processing tail; do not extend the clip to the mathematical end of local reverb/delay.

```text
item-local source + processing ------|
                                    silence
```

A hard boundary may use a tiny de-click/ramp to avoid an avoidable discontinuity. This must not
substantially extend audible reverb/delay beyond the user's boundary. Exact ramp length/algorithm,
processor reset and state ownership remain open in Q-057.

The boundary applies before later shared containing-container, bus, Mixer and Master processing. It
does not automatically erase arbitrary downstream effects of the item's earlier signal after irreversible
mixing; those scopes retain their own signal/capture/render semantics. Absolutely silencing all downstream
consequences would need separate routing/state semantics, not an assumption about ordinary trim.
Preserve the two local processing levels under
[ARCHITECTURE](ARCHITECTURE.md#resources-placements-and-two-local-processing-levels).
[UX_CONTRACT](UX_CONTRACT.md#audio-timeline-editing) owns understandable editing intentions.

### Cross-context boundaries and timing

The selected [source tap](NODE_GRAPH.md#source-boundaries-and-dependency-scope) determines what a
sidechain can observe. Before-local audio stops when the occurrence's permitted source input ends;
after-local audio can include allowed local tails after a natural source end. An explicit item hard
boundary does not permit either tap to resurrect that occurrence's cut source/local tail. A tap after
intentional container mixing observes that aggregate, including other continuing contributors or
permitted shared state; it is not an isolated Kick tail. Independent control generators retain their
own declared lifetime, rather than inheriting arbitrary audio-item boundaries.

Ending/cutting a source signal is not deleting its canonical dependency, and valid no-signal intervals
are not missing-input errors. It must not automatically reset an unrelated Bass compressor, control
smoother or downstream shared DSP. Those processors own their evolving state; Q-057 retains transition,
de-click, tail and seek/warm-up mechanics without a new fallback/reset algorithm.

Cross-context preparation must establish a realizable dependency schedule and meaningful time
relationship between tapped source, detector/control input and affected audible/parameter processing.
Different source/target latencies, control rates, transport positions and changing processor latency
can change that relationship. Consumers must not silently receive whichever block/value is convenient,
stale, or from a different occurrence. Equal block indices or a sidechain label do not prove alignment.
Realtime execution honors the prepared causal relationships within bounded callback work; it cannot
wait on UI/editor state or perform unbounded graph repair. If supported execution cannot satisfy a
required relationship, expose the dependency-scoped blocker or explicit degraded handling rather than
claim equivalence. Temporary Live / Low-Latency differences remain visible under the existing contract.

Offline execution must preserve those intended signal/control and time relationships within its
declared supported scope, including necessary state/input history. Equivalent intent does not promise
bit-identical arbitrary-plugin output. Q-005/Q-017/Q-018/Q-020/Q-021/Q-057/Q-066 retain clock/rate,
validation/scheduling, feedback, compensation and state evidence; no delays, sample offsets, control
rates, thresholds, cycle-breaking algorithm or plugin latency API are selected.

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

R1 keeps persistent musical time and canonical identities independent of every backend and of R0's
sample-frame/native/publication layouts; see [architecture disposition](ARCHITECTURE.md#r1-canonical-foundation).
R0's 48 kHz / 10 ms environment is bounded evidence only. The reviewed disposition starts bounded
managed F1 above; subsequent realtime/device work must evaluate intended workload, lower periods,
elapsed-time recovery and clean distribution before wider engine/backend choices. Q-001–Q-007 stay open.

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

An explicitly selected export range is a hard render boundary by default: selecting Bars 1–64 produces
that requested range without silently extending the file for processor tails. Future export UX may offer
an explicit option conceptually like `Include effect tails`, allowing render beyond the range for naturally
continuing permitted tails. It must never resurrect an explicit clip hard boundary, hard-cut processing
boundary or other intentional project silence. This option extends the capture range, not editing decisions.

Export must remain finite. Tail inclusion is not rendering to mathematical zero forever: extremely slow
decay, near-unity feedback, oscillation or non-decaying processors need bounded completion. Exact option
label/UI, silence threshold, maximum extension, processor-tail reporting and non-decaying-tail handling
are unselected. Q-057 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) retains finite completion, reset/warm-up,
loop state ownership, de-click and realtime/offline parity mechanisms; latency/cancellation also need evidence.
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) owns resampling source and acceptance semantics. F1's offline
single-Pattern sampler is implemented above; product render/export and realtime execution remain future work.
