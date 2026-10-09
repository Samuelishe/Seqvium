# Node graph

Role: Core signal-graph product and responsibility contract.
Read when: Designing graph editing, nodes, ports, connections, or prepared node execution.
Authoritative for: Graph semantics and irreversible mixing boundaries, conceptual port classes, core/contributed nodes, editable/prepared boundary.
Not authoritative for: Arrangement, group identity, final port ABI/compiler, DSP algorithms, or pane infrastructure.

## Accepted direction and scope

The node graph engine is core Seqvium platform functionality, not an optional editor plugin. It lets
users place nodes, connect compatible inputs/outputs, and explore audio/control processing. Useful
ideas may come from SunVox, LabVIEW, or Unity graph workflows; they are references, not specifications.
R4-F1 implements canonical graph intent, editing and diagnostics below. No graph DSP or editor exists yet.

The signal graph describes sources, processors, mixing/splitting, effects, and buses/output where
applicable. It is separate from the musical timeline and user organization. It does not replace
Arrangement, and Arrangement need not expose DSP internals. [ARCHITECTURE](ARCHITECTURE.md) owns
that separation and the musical model.

## Progressive graph interaction

Graph complexity normally remains hidden until requested. Relevant musical items/containers visibly
indicate processing through a compact interactive control; opening it reveals the relevant Node Graph
workspace pane. Exact visuals are not selected. Nodes within the opened canvas should offer a compact
working surface and deeper settings on demand. Ordinary sound creation must work before the user
understands graph internals. A basic conceptual path can be:

```text
Sample / Instrument -> EQ -> Compressor -> Output
```

More advanced processing can branch and merge:

```text
Sample
  +-> Pitch -> Delay --------+
  +-> Filter -> Distortion --+-> Mix -> Output
```

These examples demonstrate creative flow, not mandatory default chains, adopted algorithms, or an
execution API. [UX_CONTRACT](UX_CONTRACT.md#focus-selection-and-command-targets) owns mouse-first,
keyboard-efficient input, common graph keyboard access and progressive disclosure;
[WORKSPACE](WORKSPACE.md) owns pane placement, activation, and target focus;
[UI_DESIGN](UI_DESIGN.md) owns progressive visual complexity and canvas presentation principles.

## Free spatial canvas and topology-defined processing

The visual canvas permits free two-dimensional node placement for readability and personal preference.
Screen coordinates are presentation state: left/right/above/below placement does not determine which
processor executes first. Connections/topology express signal dependencies and processing order.
For an acyclic path `Input -> EQ -> Compressor -> Delay -> Output`, that connected path defines order;
branches such as parallel Distortion/Delay feeding Mix express their dependency relationship.

Ordinary processing must not rely on hidden numeric effect priorities. The realtime engine may derive
an execution schedule from a validated graph; scheduling and compilation remain F2 work.
R4-F1 uses bounded iterative dependency/cycle checks; it does not compile a schedule. Arbitrary cycles
block eligibility for preparation. Feedback remains an audit/research question: explicit feedback
semantics, delay/state, or specialized nodes are possible future approaches, not accepted solutions
or permission for zero-delay cycles.

## Local processing scopes and alternate views

[ARCHITECTURE](ARCHITECTURE.md#resources-placements-and-two-local-processing-levels) owns the accepted
item-local and containing-container processing levels, and
[signal ownership](ARCHITECTURE.md#signal-ownership-and-processing-contexts) defines their semantic
responsibilities versus sound definitions and global routes. A scope can preserve multiple paths;
it does not imply one mixed output. R1's [canonical foundation](ARCHITECTURE.md#r1-canonical-foundation)
implements identified item/containing contexts, per-placement/per-part route intent and atomic edits;
it validates the two levels and explicit aggregate versus divergent-route contradiction. No graph
DSP, attachment/topology compiler or final public container terminology is implemented. R4-F1 adds
canonical definitions, attachments, typed ports and eligibility diagnostics below; preparation and
expanded scopes stay Q-019 and later packages.

Arrangement, Mixer and graph surfaces may expose the same canonical context under
[ARCHITECTURE](ARCHITECTURE.md#arrangement-context-and-mixer-presentation). Connections determine
where processing occurs; surfacing a context twice does not insert duplicate processors. Its actual
ownership/input boundary determines local versus global scope, independently of the surface.

An arbitrary full local graph belongs to a standalone item/clip/fragment/placement-like scope,
not automatically to each atomic note, trigger, or step. Bounded event expression remains possible.
A low-ceremony event-to-independent-item path should expose deeper processing when needed;
[ARCHITECTURE](ARCHITECTURE.md#processing-granularity-and-shared-definitions) owns that granularity
and the still-open transformation. Graph availability does not require users to understand object
decomposition merely to change one hit.

A **promising UX proposal**, not a hard contract, is a compact chain representation of a simple
linear graph, allowing ordinary effect reordering without opening the canvas. If adopted, it must
represent the same canonical graph, not a separate simple-chain DSP system alongside advanced-graph
DSP. Branching/custom topologies may show a custom-graph indication instead of pretending to be linear.
Exact visuals, editing rules, and chain/canvas transitions remain open.

Node-settings UX is also open: side inspector, independent workspace pane, overlay, inline controls,
or a combination are not selected. A promising direction is selecting a node to expose deeper settings
without uncontrolled windows, with useful settings/inspectors optionally detachable into workspace
panes. This is a bounded question rather than an accepted interaction.

## Contributions and irreversible mixing

The [execution-domain contract](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains) determines
when source performance state may interact across occurrences. A domain can supply several retained
contributions; it is not inherently a mix node, graph scope or plugin instance. Derived graph execution
must preserve both that performance intent and the signal independence defined here.

Preserve distinct audible contributions until every required independent processing/routing path
before their intended convergence has been honored. Musical membership, shared source definition,
visual grouping and graph scope alone are not permission to sum signals. Deliberate local submixes,
routed channel/bus mixes and final Master aggregation are permitted convergence boundaries.
An operation intentionally changing independent outputs into an aggregate must expose that change
under [UX_CONTRACT](UX_CONTRACT.md#processing-context-and-mix-feedback).

Once contributions are summed into a common mono/stereo/multichannel aggregate without retaining
separate source outputs, that result no longer promises independently addressable original sources.
Splitting/copying it afterward duplicates the aggregate;
it does not recover Kick/Snare or Placement A/B. Original content remains editable and can be executed
again; irreversibility here concerns that signal path, not destructive editing of project data.
Distinct routes retained explicitly before a mix still carry their own signals; they cannot be
inferred from the mixed output. Mono/stereo/channel layout does not substitute for source identity.
Downstream gain, effects and routing on that output address the aggregate; constituent-specific
controls need their independent upstream paths or source edits followed by re-execution.

For `Drums Main` containing Kick and Snare events placed in Arrangement:

```text
Kick events  -> Kick definition use  -> Kick contribution  -> required Kick processing/route --+
Snare events -> Snare definition use -> Snare contribution -> required Snare processing/route -+-> intended common bus -> Master -> Output
```

Pattern identity creates no implicit bus. Kick and Snare remain separable wherever different local
processing, channel controls or downstream routes require it. If Kick needs compression and Snare
needs delay, those paths precede their common mix. If their destinations differ, they stay separate
to those destinations and may converge at a later common bus/Master. Compatible contributions may
mix at an intended common destination once no promised independent output remains beyond it;
this does not mandate a separate processing graph for each event.

If the user instead intentionally processes this **whole Pattern placement**, its item-local boundary is:

```text
Kick contribution  --+
Snare contribution -+-> explicit placement submix -> whole-placement Compressor -> placement result
```

Any independent processing intended before this submix must occur before it. Downstream of that
aggregate, there is one placement result, not independently routable Kick and Snare recovered from it.
Different downstream instrument routes therefore require retained pre-mix paths or a changed routing
intention; whole-placement processing must not pretend to preserve those routes through the aggregate.
The same rule applies to shared containing-container processing at the second local level. Exact
branch/route UI remains open (Q-030); cross-context relationships follow the bounded semantics below,
with concrete representation/execution still open (Q-066).

## Conceptual connection kinds

Connections need semantic distinctions; one untyped `object -> object` pipe is insufficient.

| Class | Examples / meaning |
| --- | --- |
| Audio | Mono, stereo, or multichannel signals; audible paths/sends and detector inputs have distinct uses below |
| Musical/event input | Note on/off, musical events, trigger-like events |
| Control/modulation | Automation values, envelopes, LFO/modulation, parameter control |

Host context normally supplies sample rate, frame/block count, tempo, transport state, musical
position, loop state, and time signature where relevant. These should not require visible cables
everywhere. Host context is not an additional universal wire format.

Exact types, conversion rules, channel negotiation, event/control rates, and execution representation
are open. Conceptual classes do not select a binary layout, native ABI, or final public plugin API.

The preferred design direction is not to expose every configurable parameter as a permanently visible
connector. An EQ can show audio input/output while Frequency, Gain, and Q are available through settings.
Future modulation may make a parameter explicitly exposable/connectable when requested. This preserves
readability; exposure mechanics, control ports, and their persistence remain open.

Future parameter control must not be blocked by permanently fixed primitive parameter modelling;
[ARCHITECTURE](ARCHITECTURE.md#future-parameter-control) owns that extensibility requirement. Base value,
automation, modulation, envelopes/LFO/control sources may contribute to an effective value, but
composition, units, precedence, smoothing, and rates are not selected here.

## Cross-context signal and control relationships

Explicit dependencies may connect existing item-local, containing-container or global routing contexts
when their semantic boundaries and capabilities support the intended relationship. This accepted
direction does not promise every context/tap/target combination. It adds neither a third local
processing level nor a universal graph that replaces musical ownership or forces all wiring into one
user-facing canvas. [ARCHITECTURE](ARCHITECTURE.md#cross-context-ownership-and-identity) owns the
cross-boundary identity responsibilities; concrete references remain Q-019/Q-066.

| Relationship | Meaning / audible consequence |
| --- | --- |
| Ordinary audible audio route | Carries a contribution through its intended processing to an audible destination |
| Audio send | Deliberately branches audio toward another audible processing/mix path; convergence is intentional under the contribution contract, not implied by branching alone |
| Sidechain detector input | Supplies a processor's detector with a signal influencing its treatment of a separate audible input; it does not add that signal to the target's audible mix |
| Control/modulation input | Influences a parameter or other supported control behavior; it is not inherently audible audio or a detector input |

A sidechain can carry audio while having a different semantic role from the audible input. Its
detector use is not interchangeable with parameter modulation. Actual types, compatibility,
conversions and rates remain Q-017; effective-parameter composition/exposure remain Q-033/Q-034.
No replace/add/multiply rule or modulation framework is selected.

```text
Kick contribution -> Drums audible route
        |
        +-> external detector signal -> Bass Compressor detector

Bass contribution -> Bass Compressor audible input -> Bass route
```

The source is the intended Kick contribution at a specified signal boundary, not every use of a
definition named Kick. The target is the Bass processor's detector role in its own processing context.
Drums/Kick ownership and Bass processing ownership stay separate. Kick's ordinary output continues
independently unless an explicit audible-routing edit changes it; Bass contains its own processed
audio without automatically mixing Kick. Removing a detector destination cannot remove Kick's route.

One such Kick signal may serve its audible route and several compatible detector/control consumers.
Destination count alone neither duplicates the musical performance/sound definition nor permits
mixing independent source performances. All consumers refer to the intended signal occurrence and
timing; this does not mandate buffers, zero-copy fan-out or physical instance counts. Different taps
can require different paths, still governed by
[execution domains](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains).
Overlapping shared Pattern placements keep their intended source contributions, controls and domains;
a relationship to placement A must not silently consume A+B or placement B because definitions match.

### Source boundaries and dependency scope

The relationship must distinguish the logical source/contribution or intentional aggregate, its
signal boundary/tap, the target processor/control role, and the context needed to interpret them.
Required dependencies are part of the intended processing, not optional hints. These are semantic
obligations for canonical intent, not selected fields, identity schemas, port classes or buffer layouts.

Before item-local processing, after that processing and after intentional container mixing can be
different detector signals. For example, Kick EQ/Delay can change the post-local detector response;
a container aggregate can also contain Snare and common processing. Selecting that aggregate cannot
pretend to select isolated Kick after irreversible mixing. Separate pre-mix paths must actually exist
if independent Kick is required. No exhaustive supported-tap menu is committed.

Tap interpretation includes occurrence, processing/boundary ownership and relevant time scope.
Object name, screen position, current selection or whichever output is most convenient cannot supply
missing semantics. A source tap observes only the signal allowed at that boundary under
[audio hard-boundary/tail rules](AUDIO_ENGINE.md#cross-context-boundaries-and-timing).
An intentional silent/end interval is a valid signal condition, distinct from an unresolved source.

### Relationship lifetime and canonical edits

A cross-context relationship belongs to canonical project intent, separately from the derived runtime
connection/schedule. Create/change/remove is an ordinary logical document transaction under
[Undo](ARCHITECTURE.md#logical-undo-transactions-and-history-scope). A source/target move or deletion
and its necessary relationship changes form the same user-level edit, rather than runtime repair edits.

| Change | Required semantic behavior |
| --- | --- |
| Source moves | Retain a reference to the same logical source if its specified boundary and target remain meaningful/compatible. Revalidate changed processing, timing and scope; the signal may change even if identity survives. An old-container aggregate is still that aggregate, not an automatic reference to Kick's new container. An unavailable boundary needs an explicit unresolved state or deliberate reassignment |
| Source deleted/unavailable/replaced | Where safely representable, retain understandable unresolved intent: former logical endpoint, selected boundary and target relationship. Never bind another same-name/position object or assume a replacement is equivalent. Required unavailable input blocks affected execution; it is not valid silence. Explicit reattachment/reassignment requires compatibility and dependency validation |
| Target moved/changed/replaced | Retain its relationship only if the same logical processor and input/control meaning survive. Revalidate the new context/capability; a replacement is not an implicit compatible destination. Otherwise retain unresolved intent where safe, or explicitly remove/reassign it |
| Target deleted | Remove its active receiving use, without changing source audible paths or other consumers. Deliberate deletion can remove the incident relationship as part of that edit, retaining sufficient history for Undo; safely retained unresolved intent must not create a phantom receiver |
| Undo deletion/change | Restore prior logical endpoints/relationships as canonical state where history permits; revalidate/prepare normally. Restored identity does not guarantee an available plugin/tap or revive cancelled async requests, rewind DSP, or authorize stale prepared work |
| Safe removal | Deliberately remove the connection, or the endpoint and its known dependent relationships, as a coherent canonical edit. Expose effects on dependent targets; no hidden retargeting, unrelated source deletion or runtime-state reset |

An unavailable/missing processor or unsupported detector/control capability preserves canonical
identity, relationships and compatible opaque state where the document is understandable, under
[extension blockers](EXTENSIONS.md#degraded-project-opening-and-operation-blockers). Required missing
input/capability cannot be silently bypassed and called correct output. A supported semantics-preserving
fallback may be used; a behavior-changing alternative needs an explicit informed choice. Unrelated
editing and healthy paths remain usable only where their dependencies permit.

### Dependency validation and remaining scope

Preparation considers the actual dependency paths across audible routes, sends, detectors and controls,
including source/target resolution, tap meaning, capability/rate compatibility, causality, timing and
lifetime. Sidechain/control labels do not exempt a connection from cycle analysis: A controls B while
B controls A can require unavailable same-time values. Arbitrary zero-delay cycles remain disallowed;
explicit delay/state or specialized feedback handling is future Q-020 work, with no cycle-breaking
algorithm selected. Validation must account for combined paths, not only each local graph separately.

Invalid/unresolved edits remain canonical and savable when safely representable; they cannot publish
as correct execution. Last-valid in-session playback and visible revision divergence follow the
editable/prepared contract below. Undo changes canonical intent and triggers ordinary preparation;
there is no separate runtime Undo stack. Realtime/offline processing honors the same intended
relationships within declared supported constraints, not guaranteed universal routing or bit identity.

Q-066 remains open for concrete source/tap/target representation, port/rate compatibility, validation/
scheduling, feedback, timing/latency, state/lifetime and move/delete/reattachment mechanisms, offline
dependency capture, host/plugin capability support, detailed UI and platform evidence. Q-018/Q-019/
Q-020/Q-021/Q-030/Q-047/Q-057/Q-063 retain their specialized mechanisms. Ordinary creation needs no
manual external wiring; [UX](UX_CONTRACT.md#external-dependency-feedback) owns discoverable bounded
feedback, [sample workflow](SAMPLE_WORKFLOW.md#external-dependencies-in-object-rendering) owns object
render scope and [roadmap](ROADMAP.md#cross-context-routing-ownership) owns staged delivery.

## Editable graph and audio execution

The editable project graph is the single canonical project truth: definitions, node identities/names,
parameters, connections and editor layout. Realtime execution is a derived prepared revision/snapshot,
not a second independently editable or persistent project model.

```text
Canonical Project Graph (revision N)
    -> validate / prepare
    -> Prepared Execution Snapshot (represents N)
    -> Audio Engine
```

The snapshot is bounded/realtime-suitable and identifies its canonical revision. Compilation is one
possible approach, not a selected algorithm. The callback must not traverse arbitrary UI objects or
mutable graph-editor state. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns execution/lifetime constraints.

Normal editing automatically converges to the latest valid canonical revision; no manual `Apply graph`
workflow is required. While revision 184 plays, editing creates 185; after validation/preparation,
atomically publish 185 and safely retire 184. If 186–188 arrive meanwhile, obsolete preparation may be
cancelled/coalesced where safe; execution need not publish every intermediate edit. Document undo/history
and realtime publication history are separate concerns. Tokens, queues, state transfer and publication
mechanics remain open under the bounded-backlog audio contract.

An accepted canonical graph edit enters document Undo immediately according to its
[logical transaction](ARCHITECTURE.md#logical-undo-transactions-and-history-scope), even while derived
execution is preparing or using last-valid state. Undo restores canonical graph relationships/content
as a new current canonical revision; normal validation/preparation/publication applies again. It neither
rewinds the live DSP state nor selects whichever old graph happens to be playing. There is no second
runtime Undo stack. Obsolete prepared work must not publish merely because Undo/Redo returns to similar
content; it must still be relevant to the current canonical execution request under Q-018. Publication
coalescing does not merge independent canonical edits or erase their Undo boundaries.

If canonical revision 185 is invalid/incomplete or still unprepared, it remains the user's canonical
edit and is not published. Last-valid prepared revision 184 may continue during the current session.
The UI clearly marks current execution as behind editable state, conceptually `canonical 185 invalid /
playing 184 / not applied`. This is a temporary runtime relationship, not two permanent project graphs.
[UX_CONTRACT](UX_CONTRACT.md#graph-state-and-recoverable-failures) owns concise feedback;
[UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns visual treatment.

Save persists canonical work even when invalid; reopen restores those edits and shows blockers rather
than restoring a second last-valid project. Affected execution-dependent operations remain blocked
until canonical blockers are repaired, while current-session last-valid playback may continue as above.
[PROJECT_FORMAT](PROJECT_FORMAT.md#save-and-reopen) owns this persistence rule. Render freezes and
validates/prepares the canonical revision under [AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction),
never silently choosing the older playing snapshot. Healthy paths remain available only where semantics
permit under [EXTENSIONS](EXTENSIONS.md#degraded-project-opening-and-operation-blockers).

Preparation/publication, safe retirement and state transitions need evidence (Q-018). The same
scheduling/node semantics should serve realtime and device-independent offline render where practical.

## Core nodes and plugin contributions

A usable installation must provide basic routing, level control, and common processing without
optional downloads. Candidate core/basic nodes include input/source boundaries, Output, Gain, Mix,
Split/routing primitives, basic EQ, and basic compressor. The exact list, DSP, and APIs remain open;
this is a baseline-usability principle, not a demand to implement every node in the first graph stage.

Plugins may contribute specialized generators, granular processors, unusual modulation, instruments,
or effects as node types. They consume host services and cannot own or replace the graph engine.
[EXTENSIONS](EXTENSIONS.md) owns contribution lifecycle, compatibility, and missing-node preservation.

## Open design questions

Expanded graph scopes, feedback handling, preparation, channel negotiation, latency compensation and
live-edit publication remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). F1's bounded intent rules follow.
Live / Low-Latency operating behavior must preserve the intended project graph and full offline render
path under [AUDIO_ENGINE](AUDIO_ENGINE.md#live--low-latency-direction); its mechanics are not selected.
Compact-chain representation, node settings, optional parameter/control exposure, and multiple graph
pane behavior also need later design. Do not invent all scopes or freeze a compiler to fill these gaps.

## Implemented R4-F1 canonical graph intent

[GraphModel](../src/Seqvium.Core/Domain/GraphModel.cs) extends the immutable `ProjectState`, without a
second document or musical ownership system. Definitions contain identified nodes and connections;
attachments address one existing context and one independent definition. At most one attachment owns
each context/definition. Unattached definitions remain savable with an unsupported-scope blocker.
Graph, attachment, node, port, connection and source-binding UUIDs are typed and globally distinct.
Built-in type keys `core.source`, `core.gain`, `core.mix`, `core.output` use state version 1. Saved port
declarations do not confer capability: built-in descriptors validate their exact supported shape.

| Node | Version 1 ports |
| --- | --- |
| Source/Input | One `out`, no input; one identified source binding |
| Gain | One required `in`, one `out` |
| Mix | 2–8 individually identified required `slot` inputs, one `out` |
| Output | One required `in`, no output; exactly one designated Output per supported attachment |

Directions are `input`/`output`; cardinality is `single` for input and `fanOut` for output. Output ports
cannot be required. Connections identify both endpoint node and port UUIDs. Each input accepts one
connection; a compatible output may feed several inputs without another source binding/performance.
Audible audio uses signal class `audio`, use `audible`, rate policy `host`, and explicit `mono` or
`stereo-lr` layout. Connected layouts must match. No conversion or automatic Mix is inserted.
Signal class/use/layout/rate/role are extensible semantic strings: future `musical-event`,
`control-modulation`, audio `detector` and unknown keys survive persistence with blockers. Essential
direction/cardinality metadata remains closed and structurally checked.

Gain's only understood parameter is `linearAmplitude`: decimal linear amplitude ratio, default `1`,
range `[0,1]` for state version 1. Values outside that range remain savable invalid intent. This is the
first attenuation capability, not a permanent ceiling; later amplification needs an explicit versioned
capability/compatibility decision. JSON parameter maps retain future structured values without adding
automation/modulation composition or a second generic parameter framework.

Supported scope is one item-local context owned by one PatternPlacement. Each Source binding addresses
`(placementId, partId, boundary: "part.pre-item")`; names, SoundDefinition and ResourceId never select
the occurrence. Exactly one Source covers each placement part, including silent parts, and each reaches
the designated Output. The currently understood source declaration is R2's configured PCM sampler;
actual media, rate, workload and resource preparation remain F2 gates. Mix presence must agree with
`IntentionalMix`; multiple parts require explicit Mix. Separate part routes, shared-performance keys,
containing processing, opaque context state and external/cross-context references block this capability
without being erased. Output denotes the local result, not Master or an OS endpoint.

[ProjectEdit.Graph](../src/Seqvium.Core/Documents/ProjectEdit.Graph.cs) supplies context/graph creation,
node creation, Gain updates, source binding/removal, output designation, connect/disconnect, node/graph
deletion and movement through the existing `ProjectDocument.Edit`. Creation can adopt an existing item
context and its requested aggregate flag while retaining opaque state. Variation remaps only that
placement's known part IDs in the same edit; pre-existing unresolved IDs remain unchanged. Placement
deletion removes its context/attachment/definition; independent resources and other graphs remain.
Node deletion removes known incident connections/bindings and its output designation. `DeleteContext`
retains R1's referenced-owner refusal: explicitly detach musical owners in the same edit to delete it
and its graph. Referenced Pattern/Sound deletion still refuses without unrelated content removal.

[GraphStructure](../src/Seqvium.Core/Domain/GraphStructure.cs) checks safe shape/ownership and global
identity bounds. Missing graph/context owners refuse; nonempty unresolved node/port/placement/part
processing references remain representable. Distinct connection IDs with identical endpoints are
execution errors; duplicate entity IDs are structural errors. Graph positions are finite X/Y values
within +/-1,000,000 graph units, canonical and undoable; they do not affect dependency order. A completed
F3 drag will be one edit; transient gestures and workspace preferences stay outside this model.

[GraphDiagnostics](../src/Seqvium.Core/Domain/GraphDiagnostics.cs) returns stable `GraphReasons` keys and
affected graph/attachment/node/port/connection/binding identities, without localized strings. All F1
diagnostics block the selected attachment, including disconnected inputs, unknown capability/state,
invalid Gain, incompatible/unsupported ports, missing/unresolved Source/Output, coverage/aggregation
errors and every directed cycle. Unknown processing fields block conservatively while retaining data;
graph presentation fields remain outside signal dependencies. Iterative bounded checks identify actual cycle members, including
self-loops and disconnected cycles; array/coordinate order cannot change results. Eligibility means
eligibility for later preparation only, never an executable state. Intent limits are 32 nodes,
64 connections, 8 sources and 8 Mix inputs; document bounds are larger and separate. Over-limit or
incomplete safe intent can still Save. No compiler, buffers, DSP, realtime publication or editor exists.

Deep graph value comparison includes JSON maps/payloads and additional fields: collection reconstruction
or composed net-zero edits create no revision/history entry; real edits create one. Undo/Redo restores
all identities, wiring, parameters, bindings and coordinates under existing lifecycle/history rules.
[Format](PROJECT_FORMAT.md#r4-f1-graph-aware-json-format) owns the exact reader gate and serialized shape.

## R4 implementation readiness recommendation

**Status: F1 intent is implemented above; F2/F3 recommendations remain proposed and separately authorized.**
R4 is in progress / partial. PRE does not close broader Q-questions or select an engine ABI.
[Roadmap](ROADMAP.md#proposed-r4-delivery-packages) owns remaining delivery scope/gates; this section
retains actionable execution/editor recommendations rather than a second project model.

### Foundation audit and reuse boundary

| Actual foundation | Reuse / necessary extension |
| --- | --- |
| [ProjectModel](../src/Seqvium.Core/Domain/ProjectModel.cs) | Immutable musical ownership and typed UUIDs now include the F1 graph intent above. Reuse it; no graph document or generic Track/Layer. |
| [ProjectDocument](../src/Seqvium.Core/Documents/ProjectDocument.cs), [ProjectEdit](../src/Seqvium.Core/Documents/ProjectEdit.cs) | Atomic graph edits/deletion/remapping and deep equality reuse bounded history, revision/generation/lifecycle. A document-change notification driving audio convergence is still absent. |
| [ProjectValidation](../src/Seqvium.Core/Domain/ProjectValidation.cs) | Musical invariants and graph structural acceptance are separate from F1 graph eligibility diagnostics. F2 adds actual preparation/resource gates. |
| [ProjectPersistence](../src/Seqvium.Core/Persistence/ProjectPersistence.cs) | Graph-aware reader 1.1, historical empty defaults, load/save graph reports, unknown/opaque preservation and normal-process replacement. Reuse current intent for execution capture. |
| [OfflineSampler](../src/Seqvium.Core/Audio/OfflineSampler.cs) | Absolute tick scheduling, deterministic Stop/Off/On order, source-rate/pitch interpolation, intensity/release, occurrence-owned voices and PCM leases. `Process` currently adds **every voice into the same interleaved output**. `PreparePattern` refuses processing/routes/shared-performance and relevant unknown dependencies; it does not execute placements. |
| [RealtimeSampler](../src/Seqvium.Core/Audio/RealtimeSampler.cs) | Frozen preparation inputs, authority/generation/lifecycle checks, one candidate plus active/pending/retired ownership, callback handoff, control retirement, sticky Stop/Panic and actual acknowledgment. Publication currently restarts at prepared start; capacity rejection is terminal for that request, with no automatic retry/convergence coordinator. |
| [AudioEndpoints](../src/Seqvium.Core/Audio/AudioEndpoints.cs), [WasapiOutput](../src/Seqvium.Audio.Windows/WasapiOutput.cs) | Independent nonmusical device intent, explicit format refusal, packet/clock fault termination and confirmed join before release. Both session/output currently name `RealtimeSampler` concretely. Extend only their portable execution seam where needed; put no graph ownership in Windows types. |
| [ShellSession](../src/Seqvium.Desktop/Presentation/ShellSession.cs), [WorkspaceHost](../src/Seqvium.Desktop/Workspace/WorkspaceHost.cs), [WorkspacePane](../src/Seqvium.Desktop/Workspace/WorkspacePane.cs) | One document lifecycle, retained controls, activation/collapse/docking, RU/EN and semantic themes. Pane definitions/content are currently fixed to Inspector/Appearance; content construction needs a small first-party extension. Desktop currently references Core only and has no import, Save/Open, transport or audio-session integration. |

Existing sampler tests supply numerical and lifetime oracles, but cannot certify independent graph
contributions or automatic convergence. F1 tests cover intent only under [Test execution](TEST_EXECUTION.md#r4-f1-graph-verification).

### F1 boundary and remaining execution choices

The [implemented intent](#implemented-r4-f1-canonical-graph-intent) adopts the independent item graph,
placement/part source boundary, descriptor authority, typed audio ports, savable blockers and canonical
coordinates recommended by PRE. Three gates remain separate: structural acceptance, intent eligibility,
and F2 preparation with available media and bounded owned execution resources. F1 settles only the first
two; no eligible report authorizes audio output.

F2 should use the common execution rate (44.1/48 kHz) and explicit mono/stereo layout. Source adaptation
reuses R2 pitch/rate and mono duplication or `(L+R)/2` downmix; edges remain conversion-free. Recommended
Mix arithmetic sums in stable port-UUID order with float headroom, without clipping/normalization.
Fan-out should retain one producer result for all consumers, initially through separate bounded buffers
rather than an aliasing optimizer. These are execution recommendations awaiting F2 evidence, not F1 DSP.

### Independent contributions: alternatives and recommendation

| Approach | Correctness, cost and migration consequence |
| --- | --- |
| A — graph after existing mixed Pattern PCM | Smallest code/RT cost; correct only for an explicitly aggregated whole-Pattern result. Cannot provide independent Kick Gain/Snare paths or preserve divergent routes. Useful as a later explicit aggregate input, insufficient for R4 acceptance. |
| B — extend sampler before its sum | Retain sorted events, voice interpolation/release and shared immutable PCM; map prepared voices to distinct placement/part accumulators before Mix. Adds bounded contribution buffers/indices, not another decoder or note scheduler. Correct for separable first-party PCM voices; supports later item/containing continuation without retroactive source recovery. Recommended. |
| C — separate graph source renderer | Can be correct, but duplicates or forks scheduling, pitch/release, resource and publication semantics. Larger parity/migration/lifetime burden with no current opaque-source requirement justifying it. Revisit only for a concrete unsupported source capability; architectural neatness alone is insufficient. |

For B, extend `PreparedNote` with a derived contribution index and placement-aware event identity.
Refactor the existing voice kernel to accumulate once per voice into its assigned contribution, then
execute prepared Gain/Mix/Output operations. Keep legacy graph-free Pattern and transient one-shot
paths as explicit compatible aggregation adapters; do not just remove `PreparePattern`'s refusal checks.
Internal source accumulation is permissible only among notes sharing the same supported required path.
Same WAV or SoundDefinition can back several contribution buffers with independent cursor/release state;
PCM index is not contribution identity. Shared mono/legato/voice-stealing interaction remains unsupported.

Mandatory example: Kick -> Gain(k) and Snare feed separate Mix slots; Mix -> Gain(m) -> Output gives
`y[c,f] = m * (k * K[c,f] + S[c,f])`. An independently authored ramp/impulse plus note timing/envelope
oracle must prove both retained upstream signals, the exact topology and final PCM. Changing only k
must leave S unchanged. Swapping branches, premixing K+S before k, duplicating source execution on
fan-out and releasing the wrong occurrence must fail tests. A master-gain-only oracle is insufficient.

### Derived preparation, publication and persistence

Prepare a frozen validated attachment dependency closure off the realtime path: resolve resources,
reuse sampler scheduling, preflight capacities, derive operation/buffer indices and immutable coefficient
data, then allocate fixed execution workspaces. Plans own PCM leases; each live execution has independent
leases and mutable voice/work-buffer state. No prepared buffers, frames, pointers or DSP state enter Save.
Introduce only the small internal prepared/execution seam needed by legacy sampler, one-shot and graph
consumers; reuse the existing realtime owner and device session rather than a parallel graph backend.

Retain R2's 8-voice, 100,001-event, 128-MiB decoded-per-plan and 64-events-per-frame bounds initially.
Recommended additional starting caps are 32 nodes, 64 edges, 8 bound source contributions, 8 inputs per
Mix and 16 MiB execution scratch per state; these are **candidate acceptance limits**, to validate in F2.
Calculate scratch against negotiated maximum packet frames (up to 65,536), layout and live buffers with
checked arithmetic before publication. Include active/pending/retired plus one candidate in total memory
accounting; slot counts alone do not bound a graph's CPU/buffers. Refuse over-budget plans wholly. Realtime
work scales with prepared frame/voice/operation bounds, not document size or arbitrary graph traversal.
Preflight event work over a maximum packet as well as per-frame density; measured supported packet/work
bounds must refuse excessive plans before playback, never drop musical events inside the callback.

A serialized application coordinator must observe accepted Edit/Undo/Redo/target/lifetime transitions,
invalidate authority immediately, keep only the latest desired execution request and admit at most one
preparation. Reuse captured snapshot/media roots and lifecycle/generation/revision checks, adding
attachment/context/binding and output-session facts. Save alone changes no execution authority. Undo
returns an older revision UUID but advances generation; equal restored content cannot authorize old work.
Add cancellation checkpoints between bounded decode/scheduling/preparation units; current R2 checks only
before/after the full preparation. No unbounded worker queue or synchronous UI preparation.

At pending/retirement capacity pressure, retain the latest desired intent and automatically retry after
consumer/control progress. Current `Publish` disposes a capacity-rejected plan, so mere reuse of that API
does not ensure convergence. Coalesce obsolete requests; never erase canonical Undo steps. Continue the
identified last-valid active execution on invalid/preparation failure, with persistent divergence feedback.
Publish only a current valid result atomically at a packet boundary and retire after all consumer borrowing
ends; disposal belongs to control. Expose status through an immutable/atomic observation rather than read
the existing unsynchronized `Guid ExecutingRevision` from the GUI during callbacks.

For topology/source/schedule replacement, the minimal baseline is R2's declared restart/new epoch, with
no seamless voice/state transfer claim. Gain-only edits on an unchanged prepared schedule/topology should
use a revisioned immutable coefficient update at a boundary, preserving cursors/voices; a graph-wide
restart on every level adjustment is not useful editing. Coordinate coefficients, revision observation,
supersession and retirement under the same authority gate. Geometry-only revisions need no DSP rebuild:
revalidate the unchanged execution dependency set and advance its canonical provenance explicitly,
without pretending an older plan was prepared from changed audio intent.
Retain the original preparation revision separately from the current revision proven equivalent for
execution; the immutable plan's origin must not be relabeled. Unknown dependencies require recomputation.

Stop/Panic command identities and real packet acknowledgment remain sticky across publication; automatic
convergence never issues a Start that defeats Stop. Project close/output replacement cancels and joins
preparation, stops/joins the consumer, then releases candidate/pending/active/retired ownership. A failed
device join retains borrowed resources; fault termination is distinct from Stop acknowledgment. F2 must
settle and measure bounded click/transition handling before audio acceptance, without silently changing
offline hard boundaries or inventing tail support. Gain/Mix add zero algorithmic latency and no tails;
source releases retain R2 semantics. Stateful DSP, seek reconstruction and compensation are future work.

Under [canonical Save/reopen](PROJECT_FORMAT.md#save-and-reopen), persist the invalid current graph,
not the playing plan. Reopen has no previous-session last-valid snapshot; block affected execution until
repair. Offline preparation always freezes/validates current canonical intent and refuses required blockers.
Extend format/load diagnostics to graph capabilities as well as existing sound/context extensions.

F1 adopted graph-aware reader minor 1 and empty historical graph defaults under the
[format owner](PROJECT_FORMAT.md#r4-f1-graph-aware-json-format). F2 must use current canonical intent,
never treat an older playing plan as Save authority or revive previous-session playback on reopen.

### First-party editor and remaining decisions

Recommend one retained first-party graph pane with an explicit attachment target initially, focusing
that surface when its target is requested. Do not silently follow unrelated selection or duplicate panes.
Use the existing workspace geometry/focus/localization/theme services; extend concrete content creation,
not a graph UI framework/plugin registry. A local canvas owns rendering/hit testing, free placement,
typed cable compatibility previews and selection. A small presenter requests `ProjectEdit`; DSP and
validation live in Core/application owners. Show compact parameters, canonical versus executing/pending/
blocked status and persistent element diagnostics under the professional DAW design target.

Commit node drag/parameter gestures once through canonical Undo; Escape discards transient proposals.
Create/delete/connect/disconnect and Undo/Redo must affect actual processing and persistence. F3 also
needs minimal real WAV import/use, selected-occurrence preview/Stop/Panic and Save/Save As/Open/close
integration because R3 supplies none. An imported sound can create a bounded Pattern/part/note/placement
source use in one explicit logical action; this is not R5's Pattern editor. Prefer an existing usable
Pattern/placement when opened; never populate fake musical content. Require joined audio/preparation
shutdown and an actual unsaved-work decision before document replacement. No Browser, Arrangement,
Mixer, export UI or automatic playback at startup is implied.

| Decision window | Questions / bounded disposition |
| --- | --- |
| F1 dependency for F2 | Consume the implemented port/attachment/binding, ownership/remapping, cycle-blocker, transaction and reader-1.1 rules above; revalidate only justified corrections with preservation/compatibility tests. |
| Within R4, before F2 acceptance | Q-018/Q-063: coordinator convergence, cancellation, generation, publication/status and bounded teardown; Q-047: pre-mix source separation and resource scaling; Q-005: placement-qualified timing and restart/transport bounds; Q-057: Gain updates/transition/Stop evidence; Q-021: verify zero-latency parallel alignment. Set actual budgets from workload evidence. |
| Within R4, before F3 acceptance | Target retention/focus, minimal real source creation/import, document replacement/Save lifecycle, compact node/port/parameter/diagnostic interaction and real RU/EN Dark/Light GUI/audio evidence. |
| Preserve for later stages | Q-005: tempo maps, seek/drift/recovery; Q-017: event/control execution/converters; Q-019/Q-029: containing/global outputs and intentional shared graphs; Q-020: explicit delayed feedback; Q-021: latency-bearing compensation; Q-047: mono/legato/opaque domains; Q-057: state/tails/de-click beyond declared core; Q-063: dependency-specific rebase/large history; Q-066: sends/detectors/modulation, cross-context taps and combined causality. |

These broad questions remain open beyond F1's bounded intent. Remaining recommendations describe a
route to independently sourced audible graphs conditional on F2/F3 gates; F1's deterministic intent
evidence establishes neither graph DSP nor device/GUI acceptance.
