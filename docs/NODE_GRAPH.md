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

Nodes should offer a compact working surface and expanded settings when useful. Ordinary sound
creation must work before the user understands graph internals. A basic conceptual path can be:

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
[WORKSPACE](WORKSPACE.md) owns the internal graph pane's placement and activation.

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

## Editable graph and audio execution

The application/project side may own rich definitions, node identities/names, parameters, connections,
and editor layout. That editable graph must not be assumed to be the realtime execution object graph.

Before audio execution, the host should validate and prepare the graph into a bounded representation
suitable for the engine. Compilation is one possible approach, not an accepted algorithm. The callback
must not traverse arbitrary UI objects or mutable graph-editor state. [AUDIO_ENGINE](AUDIO_ENGINE.md)
owns callback constraints, scheduling, execution-state lifetime, and device-independent host context.

Graph preparation, publication, resource retirement, and edits during playback need evidence. The
same scheduling/node semantics should serve realtime and device-independent offline rendering as
much as practical. Saved editable state and compatibility belong to [PROJECT_FORMAT](PROJECT_FORMAT.md).

## Core nodes and plugin contributions

A usable installation must provide basic routing, level control, and common processing without
optional downloads. Candidate core/basic nodes include input/source boundaries, Output, Gain, Mix,
Split/routing primitives, basic EQ, and basic compressor. The exact list, DSP, and APIs remain open;
this is a baseline-usability principle, not a demand to implement every node in the first graph stage.

Plugins may contribute specialized generators, granular processors, unusual modulation, instruments,
or effects as node types. They consume host services and cannot own or replace the graph engine.
[EXTENSIONS](EXTENSIONS.md) owns contribution lifecycle, compatibility, and missing-node preservation.

## Open design questions

Graph scopes (instrument/channel/bus/master/clip), cycle/feedback handling, validation/preparation
strategy, port/channel rules, latency propagation/compensation, and live-edit publication remain open
in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). Do not invent all scopes or freeze a compiler to fill these gaps.
