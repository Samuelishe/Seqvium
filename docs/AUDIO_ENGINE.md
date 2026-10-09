# Audio engine

Role: Audio execution and realtime boundary contract.
Read when: Designing the audio probe, scheduling, nodes, device I/O, recording, or rendering boundaries.
Authoritative for: Realtime constraints, execution-state lifetime/resource integrity, processing context, backend/device
boundary, recording, item-local hard boundaries, manual export ranges, finite tails, canonical offline render and finite
preparation failure.
Not authoritative for: Final language/backend/ABI, musical serialization, extension packaging, or UI design.

## R2-F1 offline sampler foundation

The reviewed direction is an initial bounded C# scheduler/DSP implementation with replaceable
execution/device ownership. [F1 source](../src/Seqvium.Core/Audio/OfflineSampler.cs) implements resource/event
preparation and the shared PCM execution used by offline and bounded F2 realtime targets. R0's native structs,
four-slot scheduler and 48 kHz endpoint are not reused. A permanent engine/ABI remains unselected;
F2's [report](experiments/SEQ-R2-F2_REPORT.md) provides scoped real-WAV/device evidence; broader claims remain open.

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
Lease creation/disposal remains owner-serialized; F2's handoff protocol below prevents disposal while borrowed.

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
R4-F2 adds independent item-local contributions below; wider execution domains and plugins remain Q-047.

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
are tested. F2 adds bounded realtime/device observations below; cross-platform numerical parity remains
unevidenced. This is not R8 resampling, final WAV export, full transport, graph or Mixer execution.

## R2-F2 realtime WAV and Windows output

[RealtimeSampler](../src/Seqvium.Core/Audio/RealtimeSampler.cs) wraps the same `OfflineSampler` scheduling,
interpolation, occurrence-owned voices and envelope code. F1's hot-path free-slot search and voice count
use bounded loops rather than LINQ. Musical events are never a disposable control queue. Realtime packets
may vary up to the caller-declared negotiated capacity (1–65536 frames); complete mono/stereo frames only.
The wrapper zero-fills before start, after Stop and beyond prepared completion. Invalid packets return false.
No musical capability, graph, Mixer, plugin ABI or permanent engine SDK is added.

The document owner captures a canonical snapshot, lifecycle, transition generation, target Pattern and
media-root list. One preparation candidate, including a completed unpublished result, reserves capacity.
The worker decodes/validates through F1 from an isolated frozen document; cancellation is checked before
and after bounded preparation and at publication. The owning publication gate rejects close, target/revision/
generation changes, cancellation and supersession. Owner-serialized calls must await preparation before
Publish/Dispose. No dependency-specific rebase is inferred. On relevant edits/target changes, the owner
invalidates authority or begins a new preparation; the callback checks that authority without document access.
An identified older active revision may continue; its revision is distinct from the canonical document.

There are one active, one pending and one retired execution, plus at most one preparation candidate.
Publishing into an occupied pending slot reports capacity rejection. At a packet boundary the consumer
can switch only when retirement capacity is free; it hands the old execution to the retired mailbox and
never borrows it again. The control owner reclaims its leases; lack of reclamation delays replacement
without growing storage. A superseded pending state retires without execution. Accepted replacements
restart at their prepared absolute start in a new epoch, terminate old voices and do not claim seamless
live-state transfer. Source leases remain immutable and independent; no canonical mutable data reaches Process.
Plans over 64 events at one frame are refused wholly before realtime publication.

Gain in [0,1] is latest-wins. Start and sticky Stop/Panic use separately retained monotonic command identities:
Start followed by Stop stays stopped, Stop followed by deliberate Start first clears old state then restarts.
Stop acknowledgment is published after that processing boundary's voice observation. A separate consumer
termination boundary stops a faulted/closed execution permanently; resource cleanup does not fabricate a
packet Stop/Panic acknowledgment. Dispose requires a successfully joined consumer. Preparation/disposal,
controls and document mutations have one serialized owner; Process has one consumer. This is bounded
single-Pattern execution, not general multi-producer event admission or graph publication.

[Windows output](../src/Seqvium.Audio.Windows/WasapiOutput.cs) owns COM, endpoint/mix-format discovery,
shared event-driven initialization, period/capacity/clock queries, MMCSS, native buffers and joined shutdown.
Its narrow assembly references portable Core; Core references no Windows API. Float32 mono/stereo
44.1/48 kHz with unspecified/standard mono or stereo channel masks is supported; unsupported facts yield
explicit unavailability. Rate, capacity and period are queried, never assumed from R0. Native PreserveSig
vtable calls avoid RCW allocation and exception-driven normal HRESULT handling. No native sampler DLL,
audio NuGet library or OS setting mutation is introduced.

Canonical ticks, prepared absolute frames, render position/epoch, submitted stream frames, raw device clock
units/frequency and QPC elapsed time stay separate and nonpersistent. On padding exhaustion, clock regression/
prolonged nonadvance, wait failure or native device error, the worker terminates execution, stops/resets the
stream and retires OS ownership. Deliberate restart requires a fresh output/consumer lifetime. There is no
elapsed-time fallback, old Note On replay, backlog catch-up or seamless dropout claim.

The explicit [Release harness](../tools/Seqvium.DeviceCheck/README.md), [protocol](experiments/SEQ-R2-F2_PROTOCOL.md)
and [report](experiments/SEQ-R2-F2_REPORT.md) own physical timing/capture observations and their limits.
Prepared numerical/voice/lifetime tests remain in the ordinary hardware-independent suite. Device output
does not establish measured DAC/acoustic latency, physical dropout counts, lower hardware periods,
multi-hour stability, all endpoints, platform parity or clean distribution. F2's hard Stop preserves F1;
the broader user-facing settling/de-click and stateful seek/tail mechanisms remain Q-057.

## R4-F2 independent contributions and realtime convergence

[GraphPreparation](../src/Seqvium.Core/Audio/GraphPreparation.cs) prepares one identified item-local
attachment/placement from current canonical intent. [Node graph](NODE_GRAPH.md#implemented-r4-f2-prepared-item-local-execution)
owns Source/Gain/Mix/Output arithmetic and placement/part separation. `PreparedSampler` and `OfflineSampler`
remain the sole musical plan/kernel for graph-free Patterns, transient audition and graph execution;
`RealtimeSampler` retains R2 authority, borrowing, packet handoff, retirement and sticky commands.
There is no parallel graph engine or Windows-owned topology, no changed project schema and no F3 UI.

### Preparation and bounded workload

Require eligible F1 intent, actual validated managed WAV/PCM, source configuration and exact negotiated
execution layout/rate. Source adaptation is explicit; cables never resample or convert channels.
Unknown/dependency/routing/media/format failures refuse the entire current request with a stable blocker.
Captured lifecycle, generation, revision, target and media roots are rechecked at publication. Cancellation
is checked between source decodes and note scheduling units, around worker execution and at publication;
one synchronous decode/hash is bounded by the existing 16 MiB WAV limit and is not instantly interruptible.

Retain 1–8 voices, 100,001 events, 128 MiB decoded PCM per plan, 64 events per frame, 1–1024 repeats,
1–65,536 maximum packet frames, 44.1/48 kHz and mono/stereo. F1 caps remain 32 nodes, 64 connections,
8 bound sources and 8 Mix inputs. Checked scratch accounting is producer buffers
`bufferCount * maximumPacketFrames * channels * 4`, plus conservative node/edge/voice storage
`nodes*256 + edges*16 + voices*128`; cap 16 MiB per state. Output aliases its input, so 32 nodes require
at most 31 producer buffers. The verified maximum stereo packet/topology needs 16,262,640 bytes,
below 16,777,216. This accounting includes table/voice allowance, not an exact managed heap/RSS claim.

Preflight computes the largest event window spanning a maximum packet, in addition to per-frame density.
Conservative work units are `frames*channels*voices*16`, plus
`frames*channels*sum(1 + 4*max(1, inputCount))`, plus `maximumWindowEvents*voices`; cap 67,108,864.
Units bound sample/read/compute/write and event-slot work; they are not CPU instructions or a universal
device deadline guarantee. All unsupported plans refuse before output; the callback never drops notes.

One candidate plus active/pending/retired ownership bounds total retained PCM/scratch to 576 MiB.
`ReservedPcmAndScratchBytes` includes the sole in-flight candidate's conservative 144 MiB reservation;
`RetainedPcmAndScratchBytes` observes the published slots. Prepared event/note/index objects have a
separate conservative four-plan allowance of 102,401,024 bytes. A worker may temporarily hold one
bounded source-read/decode buffer beyond retained PCM. Canonical history, caller output, diagnostics,
GC heap retention and OS/native memory are separate; the envelope is not a process working-set limit.
Plans/executions lease the same immutable PCM; a branch never decodes that resource again within a plan.

### Application convergence and revision observation

[GraphExecutionCoordinator](../src/Seqvium.Core/Audio/GraphExecutionCoordinator.cs) uses one supplied
serialized owner SynchronizationContext. `ProjectDocument.Changed` fires after an accepted Edit, Undo
or Redo finishes; rejected/net-zero work and Save do not emit edits. The coordinator immediately
invalidates authority, cancels obsolete work and retains only the latest desired request. At most one
worker or completed candidate is admitted. A coalesced 20 ms owner progress notification reclaims retired
state and retries occupied capacity; a completed graph candidate is retained rather than decoded again.
The owner context/event loop must remain alive through awaited shutdown. There is no manual Apply,
runtime history or merging of independent canonical Undo transactions. Generation prevents Undo ABA.

Gain-only changes on equivalent topology/schedule/media publish an immutable latest coefficient vector
at a packet boundary, preserving voice cursors, release and transport. Geometry-only changes need no
decode, topology construction or scratch rebuild; equivalent provenance advances separately. Coalesced
Gain then geometry carries the full current coefficients, so supersession cannot lose an unapplied Gain.
Topology/source/schedule replacement restarts the prepared absolute range in a new epoch, with no
live state-transfer claim. Preparation/convergence never issues Start; deliberate coordinator Start
requires the current validated publication authority. Stop/Panic remain sticky through completion/retry.

`RealtimeSampler.ReadStatus` returns a value snapshot protected by a single-consumer sequence counter;
it distinguishes origin prepared revision, executing audio revision, equivalent canonical revision,
executing attachment, transport/epoch/position/voices, real Stop acknowledgment and termination.
`PreparedExecutionId` is the sampler-local monotonic publication authority of that particular prepared
state, retained independently of later invalidation/update authority. It is derived and never serialized.
Coordinator equivalence, update publication and consumer acceptance require this identity and the exact
attachment. Document revision alone cannot identify an execution: two attachments, or repeated preparation
after Undo ABA, can share it. Origin prepared revision remains frozen while audio/equivalent provenance
advance. Pending publication is not consumer handoff; a rejected target requires normal preparation and
publication before it can execute. Automatic convergence never issues Start and cannot undo sticky Stop.
Deliberate Start refuses a prepared publication already observed as rejected by the consumer.
`GraphExecutionCoordinator.ReadStatus` adds canonical revision/target, preparation pending, stable
blockers, last-valid playing and closed state through an atomic immutable observation. UI must use
these APIs, not the legacy unsynchronized Guid property. Snapshots retain no UI, mutable graph or Undo.
Invalid current edits stay canonical/savable; current offline preparation refuses them. A fresh reopen
inherits no previous last-valid plan.

### Finite transitions and joined shutdown

An abrupt graph replacement while playing applies a new-state linear fade-in over `round(rate/1000)`
frames: 44 at 44.1 kHz, 48 at 48 kHz, gain `(frame+1)/N`, then unity. It is partition invariant,
finite even at in-packet EOF and isolated from steady-state/offline arithmetic. No old tail crosses the
boundary; no seamless handoff or acoustically click-free guarantee is claimed. Gain edits are immediate
at their boundary. Core Stop/Panic retain R2's immediate hard cut, with zero voices/output at the next
processed boundary; they do not emit a settling tail or reinterpret an explicit hard range. Future
user-facing ordinary transport settling remains Q-057, separate from these technical hard commands.
Gain/Mix have zero algorithmic latency/tails; sampler release retains R2 behavior.

Document Close/output replacement first prohibits new preparation and invalidates/cancels work.
Await `GraphExecutionCoordinator.CloseAsync`/`DisposeAsync` before joining the device borrower.
`AudioDeviceSession.AttachCoordinator` transfers that coordination lifetime; `CloseOutputAsync` and
`DisposeAsync` await preparation, Stop/join output, then release prepared/execution leases. Synchronous
session close refuses an attached coordinator until the asynchronous path completes. Failed/unconfirmed
join retains borrowed sampler/output ownership for retry. Fault termination remains distinct from a
processed Stop acknowledgment. The Windows adapter remains only the device worker; new amplitude/
adjacent/packet-boundary delta observations run exclusively in bounded opt-in diagnostics.

The [verification owner](TEST_EXECUTION.md#r4-f2-execution-verification) and
[report](experiments/SEQ-R4-F2_REPORT.md) declare numerical, maximum-workload and actual device limits.
Normal tests open no physical output. Evidence does not establish acoustic perception, DAC latency,
multi-hour/all-device stability, other platforms, cross-context routing or stateful processor behavior.
Full F2 acceptance remains partial: the original two pressure starvations remain failures and new
same-endpoint diagnostics also fail with natural GC and graph-free legacy. Native runtime suspension
aligned with a collection exceeds device capacity in specific new windows. A bounded full-workload WPR
failure additionally confirms 62.45 ms Ready time of the suspension initiator under higher-priority Rider
CPU competition, versus 1.64 ms of actual Gen0 collection and 8.5/11.3 µs audio dispatch after Ready.
This identifies scheduler interference inside that runtime suspension; historical attribution, a scheduler
defect and driver fault remain unproved. The service-start interval is not OS dispatch latency.
Confirmed fault termination/join releases resources, but does not establish deadline stability.

## R2-F3 transient one-shot execution

`SamplerPreparation.PrepareOneShot` derives one ephemeral native-pitch voice and natural-EOF frame
boundary from decoded PCM, leasing source independently. It reuses OfflineSampler interpolation,
channel mapping, cursor and EOF semantics, including supported 44.1/48 kHz rate conversion. Empty
revision/occurrence denotes no canonical music; no saved project/Pattern is fabricated. No second
sampler, looping, contextual substitution or processing is added.

`WavAudition` owns a dedicated RealtimeSampler, not a mix into unrelated project playback. Optional
document Close notification runs on the control owner; Process has no document reference. F2's
one-candidate and active/pending/retired capacities apply. Tagged Start waits for the intended source
handoff, never restarting an old source under retirement backpressure. Musical F2 Start/Stop stays
unchanged. A published transient state's cancellation token is checked without allocation/waiting
each packet. EOF/cancel/explicit Stop retires temporary state at a consumer boundary; control-side
RetireCompleted releases leases. Full retirement capacity delays handoff/release, not voice stopping.
Fault termination rejects later publication and requires a fresh consumer/output lifetime. Cleanup
never invents Stop acknowledgment; callers await preparation and join output before Dispose.

Default `WasapiOutput()` now has no capture, observation/partition array payload, per-packet finite/
silence scan or allocation instrumentation. Clock/padding/fault-stop and joined native ownership remain.
Detailed timing/miss/allocation fields are **not measured** in default mode: their zeros are not timing
or zero-allocation evidence. Explicit `diagnostics: true` retains F2's bounded observations/reporting
and instrumentation-full failure. Capture additionally requires a declared capacity. Physical harness
commands opt in, warn before sound and change no system volume/settings.
The [F3 report](experiments/SEQ-R2-F3_REPORT.md) records post-change smoke/pressure evidence, without
claiming a repeat of the full original [F2 series](experiments/SEQ-R2-F2_REPORT.md).

## R2-F4 logical endpoints and independent selection

[AudioEndpoints](../src/Seqvium.Core/Audio/AudioEndpoints.cs) provides portable immutable endpoint/default
observations, default-role versus explicit opaque-ID intent, resolution and a serialized session owner.
`AudioDeviceSession` holds independent nonpersistent input/output selections. Selecting input cannot
close/change output; selecting output cannot change input. Neither owner nor callback references a
document, canonical settings, musical identities or Undo. Windows IDs never enter project persistence.

[WindowsAudioEndpoints](../src/Seqvium.Audio.Windows/WindowsAudioEndpoints.cs) reads render and capture
MMDevice endpoints with all OS states: Active, Disabled, NotPresent and Unplugged. Names are optional
presentation, IDs are opaque OS identity. Console, Multimedia and Communications defaults are queried
separately in each direction. A missing default is explicit; other default-query errors retain a failed
query and opaque diagnostic code. Infrastructure/enumeration failures throw with native diagnostics;
there is no partial-success listing. Name-property failures retain the ID/state and a name error code.
Collections/devices/property stores are released, GetId storage is CoTaskMemFree'd, PROPVARIANT is
cleared and only this call's successful COM initialization is uninitialized. No capture client is activated.
Input endpoints are device-level observations; channels are not fabricated as separate microphones.

A snapshot is neither a future availability guarantee nor a format/recording test. Explicit absent,
inactive or wrong-direction IDs refuse resolution without default fallback. A role-following choice
resolves afresh on each deliberate open; an explicit choice remains fixed. Observation refresh does
not rewrite intent, stop another direction or migrate an active stream. There are no device notifications
or automatic live default switching. The active stream keeps its queried endpoint until Close/fault;
control observes refreshed availability and output Failure/HResult separately from intent.

`WasapiSelection.OpenOutput` first joins/releases the previous output, takes fresh discovery (a narrow
injectable result function), resolves intent, then opens a fresh worker. Before activation, the worker
requeries default/explicit device, actual state and direction; unplug/change races may still fail later
through ordinary HRESULT fault-stop. Discovery never enters the packet loop. Existing format refusal,
rate/channels/period/capacity/clock, PCM, Stop/Panic, native-buffer and fault ordering remain unchanged.
The open result contains earlier resolution plus actual native outcome; `OutputFacts.Endpoint` identifies
what was actually opened, including a default change between snapshot and worker query.

The session owns attached output and sampler. After awaited preparation, a different output intent
issues Stop, closes and joins the borrower, then disposes output and active/pending/retired sampler state.
An unsuccessful/unconfirmed join retains old intent/resources for retry and opens no replacement.
Cleanup preserves real Stop acknowledgment; termination never invents an unprocessed acknowledgment.
Opening alone starts no playback; the caller explicitly prepares/starts the fresh sampler. There is no
seamless cross-device state/voice migration. Same intent selection is a no-op; deliberate reopen refreshes
even a default role without changing its intent. The single lifetime interface expresses borrowing/join
ownership, not a processing/backend hierarchy. Legacy direct WasapiOutput callers still must join before
disposing their own borrowed processor.

The [F4 report/audit](experiments/SEQ-R2-F4_REPORT.md) records real endpoint facts and bounded physical
checks. Logical input discovery/selection is implemented; input streaming/monitoring/recording is not.

### Remaining MIDI and capture ownership

MIDI/capture preparation is an ownership plan, not recording implementation: MIDI adapter owns bounded
messages/timestamps/connection lifetime; the host deliberately translates accepted input into musical
ticks. Audio input owns buffers, channel/clock facts and device lifetime. Later recording acceptance
must distinguish unfinished capture from durable managed media and use preparation/revalidation plus
explicit canonical editing. Monitoring, clock alignment, compensation, hardware loss, ASIO and
recording UX remain Q-026/Q-027/Q-062/Q-069 and later evidence. No permanent SDK, input stream or MIDI
controller/editor is introduced merely to document these owners.

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
before realtime use. The callback must not traverse UI nodes or mutable graph-editor state. R4-F2's
bounded preparation/publication is implemented above; wider capabilities and a final ABI remain open.

Execution snapshots derive from the single canonical editable project graph and identify its revision.
Last-valid execution may continue in the current session while canonical edits are invalid or preparing;
invalid revisions never publish. The latest valid prepared revision automatically replaces older
execution atomically with safe retirement; no manual Apply is required. Obsolete preparation may be
cancelled/coalesced safely, separately from undo history. Save/reopen preserves canonical edits under
[PROJECT_FORMAT](PROJECT_FORMAT.md#save-and-reopen), not a persistent last-valid runtime graph.
[NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution) owns this graph rule and the required
visible distinction between editable and executing state; broader stateful publication remains open.

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
diagnostics are legitimate requirements. These principles imply neither unlimited duplication nor
universal numerical limits; each implemented slice declares its own bounds.

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
instance counts, limits and realtime publication beyond bounded R4-F2 remain open. Q-019/Q-029 own canonical references/edit
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
microphone through an interface is represented by its OS capture endpoint, with future stream channels
owned separately; do not invent `Input 1` endpoints from a device-level OS listing. A USB microphone may be its
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
single-Pattern sampler and F2 realtime output are implemented above; product render/export remains future work.
