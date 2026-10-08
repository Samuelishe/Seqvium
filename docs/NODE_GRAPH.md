# Node graph

Role: Core signal-graph product and responsibility contract.
Read when: Designing graph editing, nodes, ports, connections, or prepared node execution.
Authoritative for: Graph semantics and irreversible mixing boundaries, conceptual port classes, core/contributed nodes, editable/prepared boundary.
Not authoritative for: Arrangement, group identity, final port ABI/compiler, DSP algorithms, or pane infrastructure.

## Accepted direction and scope

The node graph engine is core Seqvium platform functionality, not an optional editor plugin. It lets
users place nodes, connect compatible inputs/outputs, and explore audio/control processing. Useful
ideas may come from SunVox, LabVIEW, or Unity graph workflows; they are references, not specifications.
No graph engine or editor is implemented yet.

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
an execution schedule from a validated graph, but no scheduling/validation algorithm is selected.
Arbitrary cycles are not accepted. Feedback remains an audit/research question: explicit feedback
semantics, delay/state, or specialized nodes are possible future approaches, not accepted solutions
or permission for zero-delay cycles.

## Local processing scopes and alternate views

[ARCHITECTURE](ARCHITECTURE.md#resources-placements-and-two-local-processing-levels) owns the accepted
item-local and containing-container processing levels, and
[signal ownership](ARCHITECTURE.md#signal-ownership-and-processing-contexts) defines their semantic
responsibilities versus sound definitions and global routes. A scope can preserve multiple paths;
it does not imply one mixed output. Concrete scope references/edit ownership remain Q-019, not the
semantic separation. No final types, arbitrary nesting or container terminology are selected.

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

Concrete graph-scope references/edit ownership, cycle/feedback handling, validation/preparation, port/channel rules,
latency propagation/compensation, and live-edit publication remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
Live / Low-Latency operating behavior must preserve the intended project graph and full offline render
path under [AUDIO_ENGINE](AUDIO_ENGINE.md#live--low-latency-direction); its mechanics are not selected.
Compact-chain representation, node settings, optional parameter/control exposure, and multiple graph
pane behavior also need later design. Do not invent all scopes or freeze a compiler to fill these gaps.
