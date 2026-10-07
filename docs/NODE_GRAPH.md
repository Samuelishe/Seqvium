# Node graph

Role: Core signal-graph product and responsibility contract.
Read when: Designing graph editing, nodes, ports, connections, or prepared node execution.
Authoritative for: Graph semantics, conceptual port classes, core/contributed nodes, editable/prepared boundary.
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
execution API. [UX_CONTRACT](UX_CONTRACT.md) owns mouse-first input and progressive disclosure;
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
item-local and containing-container processing direction, with mixer/bus/master responsibilities
beyond it. These workflows motivate local graph scopes without selecting scope identities, all
possible graphs, nesting, or the final container term. Exact scope ownership remains open.

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

## Conceptual connection kinds

Connections need semantic distinctions; one untyped `object -> object` pipe is insufficient.

| Class | Examples / meaning |
| --- | --- |
| Audio | Mono, stereo, or multichannel signal flow |
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

## Editable graph and audio execution

The application/project side may own rich definitions, node identities/names, parameters, connections,
and editor layout. That editable graph must not be assumed to be the realtime execution object graph.

Before audio execution, the host should validate and prepare the graph into a bounded representation
suitable for the engine. Compilation is one possible approach, not an accepted algorithm. The callback
must not traverse arbitrary UI objects or mutable graph-editor state. [AUDIO_ENGINE](AUDIO_ENGINE.md)
owns callback constraints, scheduling, execution-state lifetime, and device-independent host context.

Live editing uses last-valid execution: the currently valid prepared graph keeps running while a newly
edited candidate is prepared and validated. An invalid/incomplete candidate is not published and must
not automatically destroy currently playing valid audio. Once valid and prepared, it may replace the
previous execution state through the eventual safe publication mechanism.

```text
last valid prepared graph -> continues audio
editable candidate graph  -> prepare / validate
                            -> valid: publish safely
                            -> invalid: do not publish
```

If the visual graph differs from current execution because the candidate is invalid or not yet
published, the UI must clearly communicate that difference. Continuing old audio must not imply that
the edited graph is already sounding. [UX_CONTRACT](UX_CONTRACT.md#graph-state-and-recoverable-failures)
owns concise recoverable feedback; [UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns visual treatment.

Graph preparation, publication, resource retirement, and state transition during playback need evidence. The
same scheduling/node semantics should serve realtime and device-independent offline rendering as
much as practical. Saved editable state and compatibility belong to [PROJECT_FORMAT](PROJECT_FORMAT.md).
Q-060 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) retains the separate save/reopen/render policy when
editable and executing graphs differ; last-valid playback alone does not choose a durable/render revision.

## Core nodes and plugin contributions

A usable installation must provide basic routing, level control, and common processing without
optional downloads. Candidate core/basic nodes include input/source boundaries, Output, Gain, Mix,
Split/routing primitives, basic EQ, and basic compressor. The exact list, DSP, and APIs remain open;
this is a baseline-usability principle, not a demand to implement every node in the first graph stage.

Plugins may contribute specialized generators, granular processors, unusual modulation, instruments,
or effects as node types. They consume host services and cannot own or replace the graph engine.
[EXTENSIONS](EXTENSIONS.md) owns contribution lifecycle, compatibility, and missing-node preservation.

## Open design questions

Graph-scope ownership, cycle/feedback handling, validation/preparation, port/channel rules,
latency propagation/compensation, and live-edit publication remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
Live / Low-Latency operating behavior must preserve the intended project graph and full offline render
path under [AUDIO_ENGINE](AUDIO_ENGINE.md#live--low-latency-direction); its mechanics are not selected.
Compact-chain representation, node settings, optional parameter/control exposure, and multiple graph
pane behavior also need later design. Do not invent all scopes or freeze a compiler to fill these gaps.
