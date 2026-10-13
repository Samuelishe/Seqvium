# Node graph

Role: Core signal-graph product and responsibility contract.
Read when: Designing graph editing, nodes, ports, connections, or prepared node execution.
Authoritative for: Graph semantics and irreversible mixing boundaries, conceptual port classes, core/contributed nodes, editable/prepared boundary.
Not authoritative for: Arrangement, group identity, final port ABI/compiler, DSP algorithms, or pane infrastructure.

## Accepted direction and scope

The node graph engine is core Seqvium platform functionality, not an optional editor plugin. It lets
users place nodes, connect compatible inputs/outputs, and explore audio/control processing. Useful
ideas may come from SunVox, LabVIEW, or Unity graph workflows; they are references, not specifications.
R4-F1 implements canonical graph intent, editing and diagnostics below. R4-F2 executes the bounded
item-local graph through the PCM sampler. F3-A adds a bounded desktop import/use/parameter workflow;
F3-B supplies the first-party editable canvas below; owner visual/interaction acceptance remains pending.

The signal graph describes sources, processors, mixing/splitting, effects, and buses/output where
applicable. It is separate from the musical timeline and user organization. It does not replace
Arrangement, and Arrangement need not expose DSP internals. [ARCHITECTURE](ARCHITECTURE.md) owns
that separation and the musical model.

## R5-F1 channel composition

Beat creation supplies a real empty item graph: Gain (0.25) → Output. Without parts/sources this is
intentionally unready for Play. First Add WAV Channel connects its bound Source to Gain; subsequent
channels insert a built-in two-input Mix before that same Gain, combining its preceding Source/Mix
tree with the new independent Source. Three channels therefore use two Mix nodes, one shared Gain
and one Output, executed by the existing graph compiler/DSP. A full graph remains editable in Node Graph.

Automatic expansion recognizes only a fully bound, acyclic stereo Source/binary-Mix tree feeding the
sole Gain → designated Output, with each upstream output used once and no extra nodes/unknown intent.
The pristine empty chain is separately checked. Existing node identities, ports, positions, Gain and
unrelated content remain intact; only the known Gain-input cable is replaced. Invalid/custom graphs,
other layouts/dependencies, shared Pattern channel expansion and capacity exhaustion refuse before
acceptance. At most eight parts/sources are offered, subject to existing graph/media/voice preparation
budgets. No new mixer, binding kind, graph schema or processor is added.

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
an execution schedule from a validated graph; R4-F2 implements the bounded deterministic schedule below.
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
it validates the two levels and explicit aggregate versus divergent-route contradiction. R4-F2 implements item-local DSP/preparation below; expanded scopes and final public
container terminology remain open. R4-F1 adds
canonical definitions, attachments, typed ports and eligibility diagnostics below; preparation and
expanded scopes stay Q-019 and later stages.

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

Detailed editor access uses body double-click alongside single-click selection/movement and compact
controls. The first slice uses one reusable Workspace pane. Q-032 retains independent editors and
future composition; the [editor boundary](#full-node-editors-proposal) defines the bounded capability.

## Full node editors proposal

**SEQ-NODE-EDITOR-IMPL-F1 — implemented bounded owner-authorized slice of DESIGN-R1.** One retained
internal editor provides Gain v1 editing and Source/Mix/Output information. Q-032 remains open for
independent editors and future composition. Core schema, DSP, backend and public extension API are unchanged;
F2 stays partial and F3-B interaction acceptance stays pending owner review.

The owner requirement is a detailed editor for every node, opened primarily by double-click on the
node body. Single-click retains selection and existing movement. Compact Gain-style parameters may
coexist. Editor capacity must allow a real EQ, interactive graphs, analyzers, tabs, presets and
specialized controls independently of node size or the graph's bottom parameter row. First-party
Workspace integration is preferred; native third-party OS windows are allowed. These requirements
do not select placement, instance count, DSP algorithms or plugin interoperability.

The host-owned `NodeEditorSession` projects a node-specific first-party presentation in
**one reusable internal Node Editor pane**. An explicit Open Editor request
changes its target; selection alone does not. A supported built-in gets its actual controls/details;
an unknown, missing or incompatible node gets host-owned identity/port/binding/diagnostic details
without executing contributor code or guessing at opaque parameters. Thus opening remains available
even for incomplete graphs. A full editor is an editing surface, not a promise that every structural
node has many editable parameters. [Workspace](WORKSPACE.md#node-editor-placement-proposal) owns the
alternatives and instance policy; [UX](UX_CONTRACT.md#detailed-node-editor-access) owns input behavior.

Future EQ/instrument/effect presentations can own a substantial resizable working region with
specialized interaction instead of being forced into a generated form. Editor choice is separate from
processing capability: UI absence may use host details while independently supported processing
continues; missing processing remains an execution blocker. Future instrument editors must resolve
whether controls address node-owned state or a referenced shared SoundDefinition and expose that
sharing scope. Node identity must not be assumed to identify one physical plugin execution instance
under [execution domains](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains).

### Gain v1 as a useful first editor

Gain v1 implements a compact attenuation workspace: a continuous level control, exact linear `[0,1]` input,
derived read-only dB attenuation (zero explicitly means silence), and Unity/Silence actions. Include
a scalable analytical transfer graph `output = linearAmplitude * input`, with labelled axes in an
initially collapsed Curve disclosure; the formula is available on request. This owner-directed
composition refinement keeps the musical control primary without removing the graph. A
draft during a gesture is visibly temporary. This illustrates attenuation and validates an
interactive specialized region without adding DSP or claiming a live waveform/analyzer. Do not pad
one numeric field to a graph-sized window, invent amplification/bypass/presets, or show disabled
future tabs. Unity and Silence edit the existing parameter; they are not new stored parameters.

One changed level release or Enter/Apply creates one existing document transaction; Escape, no-op,
capture loss or stale basis creates none. Numerical conversions/curve data are derived presentation,
not additional saved values. No audible gesture preview occurs in this first slice: canonical
commit converges through the existing F2 coordinator. Existing quick controls synchronize through
the [single-state boundary](ARCHITECTURE.md#node-editor-state-and-lifetime-proposal); duplicate controls
for the exact same parameter yield to the visible expanded Node Editor, then the visible Graph,
under [UX](UX_CONTRACT.md#detailed-node-editor-access). Different targets stay available.
Persisted invalid Gain intent stays visible with its diagnostic and an explicit legal repair path;
never silently clamp it to a slider value or portray an unsupported value as valid attenuation.

Source, Mix and Output initially receive purposeful read-only details: Source's actual placement/Part
binding and resource availability; Mix's identified input slots/connections and intentional aggregate;
Output's local result/designation and upstream connection. These views use existing metadata and
diagnostics, with repair in the graph; they do not imply a sampler instrument editor, Mixer/Master,
device selector or editable unknown JSON.

### Remaining owner decisions and risks

Q-032 retains reusable versus independently retained node panels and eventual placement choices;
Q-035 still owns multiple **graph** surfaces and cross-project behavior, which this single-node
proposal does not resolve. Q-067 retains contributed editor/resource registration and retirement.
Main risks are accidental selection-following, stale drafts after Undo/Redo, misleading canonical
versus audible feedback and a singleton becoming restrictive for comparisons. Use explicit targets,
fresh commit bases and scoped execution status; collect real comparison-workflow evidence before
expanding pane instances. Native editor lifecycle is only a
[future responsibility proposal](EXTENSIONS.md#native-node-editor-lifecycle-proposal).

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
wider live-edit publication remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). The bounded F1/F2 rules follow.
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
actual media, rate, workload and resource preparation use the separate F2 gates below. Mix presence must agree with
`IntentionalMix`; multiple parts require explicit Mix. Placement-level downstream routes, separate part
routes, shared-performance keys, containing processing, opaque context state and external/cross-context
references block this capability without being erased. Both placement and part routes report
`graph.unsupported-dependency` while retaining their canonical route identities. Output denotes the local
result, not Master or an OS endpoint.

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
incomplete safe intent can still Save. Eligibility alone creates no executable state; F2 preparation
and execution are described below. The editor remains F3 work.

Deep graph value comparison includes JSON maps/payloads and additional fields: collection reconstruction
or composed net-zero edits create no revision/history entry; real edits create one. Undo/Redo restores
all identities, wiring, parameters, bindings and coordinates under existing lifecycle/history rules.
[Format](PROJECT_FORMAT.md#r4-f1-graph-aware-json-format) owns the exact reader gate and serialized shape.

## Implemented R4-F2 prepared item-local execution

[GraphPreparation](../src/Seqvium.Core/Audio/GraphPreparation.cs) consumes the current canonical snapshot,
a concrete attachment and its owning placement/context. F1 eligibility is necessary; actual managed WAV
integrity/availability, PCM leases, source configuration, execution format, schedule and budgets are
additional gates. Stable `graph.prepare-*` reasons or existing `GraphReasons` refuse the entire plan;
preparation never edits intent or chooses another resource. All placement/part routes, shared-performance,
containing, opaque and cross-context dependencies remain blocked. JSON/schema/F1 identities are unchanged.

Prepared notes address distinct `(placement, part)` contribution buffers. Their event identity extends
R2 `(Pattern, part, note, iteration)` with placement identity. ResourceId selects shared decoded PCM only;
it does not merge voice/release ownership. Silent parts retain a binding and validated PCM dependency.
One Source accumulates that part's notes on its supported path exactly once. Fan-out reads its result;
it creates no musical performance or second decode. Source adaptation explicitly reuses R2 interpolation,
pitch, intensity, release and mono duplication/stereo downmix at the execution-rate boundary.

Preparation derives a Kahn topological order, selecting ready nodes by UUID. Source, Gain v1 `[0,1]`,
explicit 2–8-input Mix and the selected context's Output execute through `OfflineSampler`. Each producer
has a separate fixed buffer; Output aliases its one upstream result. Mix sums in ascending input-port
UUID order, independent of node/port/edge arrays, creation order and canvas geometry. Cables perform no
conversion. There is no normalization, clipping/limiting, effect, added algorithmic latency or effect tail.
`OfflineSampler.Signal` exposes the last packet's intermediate PCM only to its single diagnostic owner.

[Audio](AUDIO_ENGINE.md#r4-f2-independent-contributions-and-realtime-convergence) owns execution budgets,
packet publication, revisioned coefficient updates, status, finite transitions and joined shutdown.
[GraphExecutionCoordinator](../src/Seqvium.Core/Audio/GraphExecutionCoordinator.cs) observes post-transaction
Edit/Undo/Redo, target changes and Close on one serialized owner context. It retains one latest request,
cancels obsolete work and automatically retries capacity without rebuilding an already completed candidate.
Invalid canonical edits remain savable while an identified last-valid attachment/revision can keep playing.
Offline preparation always uses current canonical intent; a fresh reopen has no inherited runtime fallback.
Geometry changes advance proven-equivalent provenance; Gain changes update coefficients at a packet
boundary without resetting voices, releases or transport. Other changes restart in a new epoch.

The independent upstream and final PCM oracle, maximum topology, realtime/lifecycle tests and actual
bounded Windows output evidence are owned by [verification](TEST_EXECUTION.md#r4-f2-execution-verification)
and the [compact report](experiments/SEQ-R4-F2_REPORT.md). This is bounded Core readiness for F3;
F3-A now supplies bounded Desktop audio composition and canonical Gain controls, with exact new Source
bindings created during atomic import/use. Its compact chain/details view is not a graphical editor.
F3-B adds the canvas below. Browser, Pattern Workspace, Mixer and export remain absent.

## Implemented R4-F3-B first-party editor

One retained Desktop graph pane projects canonical Source/Gain v1/Mix/Output nodes and real typed ports.
Its explicitly chosen attachment is independent of unrelated musical selection. Workspace owns pane
geometry; graph coordinates remain canonical and Save/Undo-owned. A local viewport and selection own
no music or history. Rendering/hit testing share `Workspace/GraphGeometry`; curves follow canonical
or transient proposed node positions. [Workspace](WORKSPACE.md#r4-f3-b-retained-graph-pane) owns lifetime.

Ports and node bodies precede cables in hit testing. Cable selection compares every drawable candidate
within the unchanged six-screen-unit tolerance using `CableDistance * zoom`. Candidates within 0.1
screen unit of the global minimum are tied: the currently selected cable wins, then the upper cable
in visual order. These are the canvas's logical screen pixels (Avalonia DIPs); zoom does not change
the screen tolerance or tie threshold. A selected farther cable cannot override a distinct nearest
one. Bezier geometry, under-strokes, colors, port shapes and crossing rendering remain unchanged.
Selection feedback and connection/disconnect menus share human endpoint labels with actual port
roles and direction-local indices, such as `Source → Mix · In 2`; UUIDs stay off the visible surface.
Delete sends the selected connection ID through the existing disconnect edit, and Undo restores
canonical connection identities/topology without affecting neighbours.

Create/delete/move/connect/disconnect, Gain and source binding commands go through the existing
`DesktopWorkflow` owner and `ProjectDocument.Edit` / `ProjectEdit.Graph`. Source creation may remain
unbound until an existing unbound Part is explicitly chosen; deleting Source removes its binding and
incident edges, retaining independent music. Creating Output designates it; another Output requires
deleting the existing one. Creating/deleting the last Mix updates the owning context's mixing intent
in the same transaction. Mix creation supports 2–8 inputs and mono/stereo declarations; wires convert
nothing. Port preview/refusal checks direction, supported capability/declaration, signal/use/rate/layout,
single occupied inputs and fan-out. Cycles and disconnected intent may commit and remain diagnosed
and savable. Eligibility and persistent blockers come from `GraphDiagnostics`, not canvas heuristics.

Node creation is enabled by an available explicit editing target and an idle, open workflow, independently
of preparation eligibility. The creation menu offers Source/Gain/Mix/Output in Stereo L/R or Mono;
Mix leaves specify 2–8 inputs. A second Output and the node-count limit have per-action explanations,
without disabling otherwise available Source/Gain/Mix repair. Empty or unsupported contextual choices
are disabled with concise reasons. A successful edit that leaves invalid intent is not reported as
a refused action; canonical diagnostics continue to describe playback blockers.

Menu leaves capture lifecycle/generation/attachment plus a dismissal identity, including Delete's
selected element IDs. Dismissal, pane cancellation, a changed target, replacement, Close or an
intervening canonical edit invalidates the retained action. Execution rechecks the captured basis and
reports a localized refusal; it never redirects an old menu to the current selection/document.
[Workspace](WORKSPACE.md#r4-f3-b-retained-graph-pane) owns opening, placement and keyboard lifecycle.

A drag retains only a proposed position and lifecycle/generation/attachment basis. Hundreds of moves
publish nothing; release validates that basis and starting coordinate, then makes one Edit. Escape,
capture loss, competing pane operations, workspace reflow or changed observation cancel the proposal.
No-op release records nothing; each keyboard nudge is a separate Edit. The conservative generation
gate also rejects Undo/Redo ABA and unrelated intervening edits rather than overwriting newer work.
Gain's invariant text field displays retained numeric/invalid intent; Enter/check commits one amplitude
in `[0,1]`, and Escape discards input. There is no independent multiplier or slider history.

`Add WAV source to current item` supplies a real second Part/Note, Sound/resource and uniquely bound
Source under the existing atomic durable acceptance. It refuses shared Patterns before preparation,
never creates a hidden variation or extra performance on fan-out. Pattern boundary extension uses the
validated `SetPatternLength` operation in that same Edit. [Sample workflow](SAMPLE_WORKFLOW.md#r4-f3-b-add-wav-to-current-item)
owns this narrow musical policy. The user manually builds independent branches into explicit Mix.

Canonical, pending, executing and invalid/last-valid states have concise localized pane feedback and
element markers/context explanation. F2's coordinator remains unchanged: repair converges automatically,
Gain-only updates retain prepared identity/voices, and reopening restores only canonical intent/media.
The [F3-B evidence report](experiments/SEQ-R4-F3-B_REPORT.md) separates pure PCM, actual GUI, physical
output and missing owner acceptance; it does not close F2 pressure acceptance or all R4.

## R4 implementation readiness recommendation

**F1/F2 foundations are implemented above; full F2 acceptance remains blocked by mixed physical pressure evidence.**
F3-A is conditionally accepted by the owner for continued development. F3-B was explicitly authorized
and is implemented above, with owner visual/interaction review pending;
[ROADMAP](ROADMAP.md#seq-r4--node-graph-foundation) retains the complete editor/closure scope.
R4 stays in progress / partial; PRE is not an engine ABI or permission to implement later stages.
The existing immutable model/history, graph-aware reader 1.1, PCM scheduler/leases, realtime ownership
and narrow device session are the reuse boundary. F3 must compose them, rather than create an editor
model, second sampler, runtime Undo stack or Windows-owned graph. Remaining acceptance boundaries follow.

### First-party editor and remaining decisions

The bounded first-party editor is implemented above and uses the existing retained workspace, canonical
history, media and execution owners. Owner review must inspect actual sparse/dense canvas interaction,
not only green tests. No Browser, Arrangement, Mixer, export UI, R5 or automatic startup playback is implied.

| Decision window | Questions / bounded disposition |
| --- | --- |
| F1 dependency for F2 | Consume the implemented port/attachment/binding, ownership/remapping, cycle-blocker, transaction and reader-1.1 rules above; revalidate only justified corrections with preservation/compatibility tests. |
| Within R4, before F3 acceptance | Target retention/focus, minimal real source creation/import, document replacement/Save lifecycle, compact node/port/parameter/diagnostic interaction and real RU/EN Dark/Light GUI/audio evidence. |
| Preserve for later stages | Q-005: tempo maps, seek/drift/recovery; Q-017: event/control execution/converters; Q-019/Q-029: containing/global outputs and intentional shared graphs; Q-020: explicit delayed feedback; Q-021: latency-bearing compensation; Q-047: mono/legato/opaque domains; Q-057: state/tails/de-click beyond declared core; Q-063: dependency-specific rebase/large history; Q-066: sends/detectors/modulation, cross-context taps and combined causality. |

These broad questions remain open beyond bounded F1/F2. F2's numerical/device evidence establishes
its declared execution path, without full R4 acceptance or permission to start later stages.
