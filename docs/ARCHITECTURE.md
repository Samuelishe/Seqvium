# Architecture

Role: Logical responsibility and dependency boundary guide.
Read when: Structuring code, reviewing coupling, or evaluating architecture proposals.
Authoritative for: Core/plugin/backend boundaries, timeline/organization/graph separation, musical/resource identities, local processing.
Not authoritative for: Exact project decomposition, audio internals, extension API, file format, or progress.

No production architecture is implemented. Accepted responsibilities and musical direction bind future
design; implementation shape, schemas, and native choices still need bounded design and evidence.

## Accepted constraints

Use simple architecture for the current product with extension points justified by known requirements.
An abstraction needs a concrete ownership or testability reason. Avoid enterprise layering,
speculative interfaces, and a disposable model that must be replaced for Piano Roll or Arrangement.

## Core platform responsibilities

Keep these logical responsibilities distinct even if some later share an assembly. This is long-term
ownership, not a requirement to implement every capability in the first stages:

| Responsibility | Boundary |
| --- | --- |
| Application shell / workspace panes | Owns main-window composition and user workspace state; [WORKSPACE](WORKSPACE.md) owns behavior |
| Project / document model | Owns musical relationships, edits, resource references, and document integrity |
| Transport / musical clock | Host-owned musical execution context; cannot be replaced by a plugin |
| Audio engine / execution | Owns scheduling and execution-time state; isolated from arbitrary mutable UI/project objects |
| Node graph engine | Core signal-graph ownership; [NODE_GRAPH](NODE_GRAPH.md) owns its contract |
| Audio device abstraction | Host-owned input/output boundary below engine processing contracts |
| MIDI device / input foundation | Host owns device selection and musical input integration |
| Audio / MIDI recording foundations | Host owns capture and timeline/resource integration; polished recording UX may arrive later |
| Mixer / bus foundations | Core mixing/routing primitives may precede the full Mixer workspace |
| Audio resource ownership | Controls availability and lifetime of project audio independently of optional UI |
| Undo / redo | Owns coherent document edits; optional surfaces cannot replace edit integrity |
| Serialization | Preserves versioned document and extension data under the format contract |
| Extension hosting / lifecycle | Hosts attached capabilities; fundamental platform ownership stays in the base |

Basic musical editing, routing, level control, and common processing must be usable without optional
downloads. Specialized generators/nodes/instruments/effects/content may extend the platform;
[EXTENSIONS](EXTENSIONS.md) owns categories, compatibility, and lifecycle. These core responsibilities
do not require separate projects, interfaces, or a public plugin API now.

Application state may control audio through a prepared bounded boundary; the engine must not depend
on UI-thread progress. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns the realtime contract. Production runtime
must not depend on experiment hosts or repository retrieval tools.

Windows may be the first implementation target. Avoid deliberately preventing later Linux/macOS
adapters; this is not a commitment to release dates or validated platform support.

## Internal modules, backends, and plugins

- **Modular internal architecture:** core subsystems have clear contracts and justified replaceable/testable boundaries.
- **Replaceable backend:** an internal adapter may implement a core boundary, such as audio-device I/O.
  Replacing it is not automatically a user plugin API.
- **User extension/plugin:** an optional guest contributes capabilities through Seqvium host contracts;
  it does not replace project integrity, transport, graph engine, or audio-device ownership.

Ordinary instrument/effect/generator plugins consume device-independent host services, not miniaudio,
WASAPI, ASIO, ALSA, PipeWire, or CoreAudio directly. Backend replacement must not require rewriting
ordinary processing plugins. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns the processing environment and
backend boundary; [EXTENSIONS](EXTENSIONS.md) owns capability compatibility and failure handling.

## Timeline, organization, and signal graph

| Concept | Describes | Canonical boundary |
| --- | --- | --- |
| Musical timeline | Patterns, notes/events, pattern/audio clips, later automation and recordings in musical time | Musical model below |
| User organization | Instrument/channel groups, names, and workspace organization | Group identity below; pane composition in WORKSPACE |
| Signal graph | Sources, processors, mixing/splitting, effects, buses/output where applicable | NODE_GRAPH |

Do not collapse these into one universal graph. Arrangement places music in time; the signal graph
describes audio/control flow and does not replace Arrangement. Organization may visually correspond
to either without becoming its storage identity or defining routing by itself.

## Proposed application and audio shape

The primary development environment is Windows 11, Rider, C#, .NET 10, Avalonia, and Codex.
C# / .NET 10 / Avalonia is the application-layer candidate, not an installed stack here.

The accepted logical boundary direction is conceptual, not an implementation selection:

```text
Project / musical state
    -> prepared execution state
    -> Audio engine (scheduler, node execution, mixer/buses)
    -> Audio device abstraction
        -> possible WASAPI / ASIO / future platform backend

Shared scheduling / node semantics
    -> Offline renderer (no audio device required)
```

[SEQ-R0](ROADMAP.md#seq-r0--audio-architecture-probe) must test execution feasibility before major
production implementation. A C# application with a narrow native boundary and native realtime engine,
possibly C++ using miniaudio, remains a hypothesis. C++, miniaudio, WASAPI, and ASIO are not accepted
implementations through this diagram. ASIO is a desired future capability, not an R0 requirement.
ABI, control publication, backend strategy, and C# decomposition remain open; no final classes exist.

The editable project/visual graph may hold rich definitions, names, parameters, layout, and connections.
The host must prepare a bounded execution representation; validation/compilation strategy is open.
The callback must not traverse mutable graph-editor or arbitrary UI state. [NODE_GRAPH](NODE_GRAPH.md)
owns that boundary's graph semantics; [AUDIO_ENGINE](AUDIO_ENGINE.md) owns execution constraints.

## Intended musical model

The accepted product direction for SEQ-R1 is:

- An **instrument** produces sound.
- A **musical part** contains notes/events for one instrument; no class or storage schema is selected.
- A **Pattern** is a user-named reusable musical unit that may contain parts/events for multiple instruments.
- A **pattern clip** places/references a pattern in the Playlist/Arrangement's musical time.
- Repeated pattern clips normally reference shared data; an explicit operation creates an independent variation.
- Mixer channels describe audio processing/routing, separately from arrangement tracks.
- Step Sequencer and Piano Roll edit compatible underlying musical event data.
- Pointer editing, on-screen musical keyboard, realtime note input, and MIDI recording converge on
  compatible events rather than independent note models.

Users choose pattern granularity. `Drums — Main` may contain Kick, Snare, and Hat parts, while bass/lead
use separate patterns. `Full Groove A` may combine those drums, bass, and synth. Other useful names
include `Drums — Fill`, `Bass — Verse`, and `Theme A`. Pattern identity is not bound to one arrangement
track or mixer channel.

**User-defined instrument/channel groups** organize instruments/channels independently of Pattern
membership. A `Drums` group can organize Kick, Snare, and Hat with meaningful naming/collapse. Multiple
patterns may use that group, and a pattern may use instruments from different groups. A group organizes
entities; a pattern owns/references musical parts. Neither is the other's storage identity. Exact
hierarchy/nesting rules remain open; unlimited nesting is not assumed.

The event model must support musical position, pitch where applicable, duration, and velocity or
equivalent intensity. A `bool[16]` foundation is insufficient. These requirements do not select a
schema, time representation, class hierarchy, or storage layout. [PROJECT_FORMAT](PROJECT_FORMAT.md)
owns persistence compatibility; [UX_CONTRACT](UX_CONTRACT.md) owns observable editing behavior.

## Resources, placements, and two local processing levels

A durable audio resource and a musical occurrence/placement of it must not be assumed to be one
mutable object. Several items may reference `kick_017.wav`; local processing of one item must not
silently rewrite the shared resource or every other use. The default creative model is non-destructive.
An explicitly destructive/edit-source operation may be justified later, but ordinary item processing
does not imply it. Resource/reference/edit ownership and storage schemas remain unselected;
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns persistence obligations.

For ordinary project work, the accepted user-facing model has no more than two local processing levels:

1. **Item-local processing:** one material instance/placement has independent processing, such as
   EQ/Gain on a Kick item or Delay on a Click item.
2. **Containing musical-container processing:** a container combines its contained audible items
   and applies common processing to that result.

```text
Kick item -> local EQ/Gain --+
Click item -> local Delay ---+-> container mix -> Compressor -> output
Noise item -----------------+

Item processing -> Container processing -> Mixer / buses -> Master -> Output
```

This bounds the ordinary creative mental model, not the engine's number of DSP stages. Global mixer,
buses, master, and output remain available responsibilities. Master is global output processing,
not a third nested local layer. Do not infer unlimited user-facing local nesting.
[NODE_GRAPH](NODE_GRAPH.md) owns how processing connections express signal dependencies.

"Layer" is provisional terminology, not a final public/domain name. Track, Layer, Channel, Lane,
or Container may overlap future vocabulary. The accepted semantics are a musical timeline container
holding independent items and processing their combined audible result. Its domain identity is not
assumed identical to Mixer Channel, Instrument Group, or Pattern. No classes or schemas are selected.

## Semi-free Arrangement

Arrangement uses user-named containers for useful visual/musical organization rather than requiring
permanent one-instrument ownership or a completely unstructured timeline. Conceptually:

```text
Drums       | Drums Main | Drums Main | Drums Fill |
Bass        | Bass A     | Bass A     | Bass B     |
Atmosphere  |          Long Texture               |
```

A container may have a preferred/default musical purpose or content relationship. That must not
automatically make it the permanent owner of one instrument or Mixer Channel. Compatible material
should be movable/reusable without arbitrary structural duplication. Compatibility, preferred-target
behavior, ownership, nesting, and audio-clip semantics remain open.

The timeline container answers where/when material is arranged; mixer channels/buses answer where
audio flows and how it is processed/routed. Useful defaults may connect them without merging their
identities. How arrangement-container processing relates to a mixer channel or bus remains an explicit
architecture question; exposing shared container processing does not settle it.

## Future parameter control

Parameter modelling must leave room for future control rather than treating DSP parameters as
permanently fixed primitive values. An eventual effective parameter may combine base/user value,
timeline automation, modulation, and envelopes/LFO/control-graph sources. This requirement does not
select an abstraction or require an automation implementation now. Replace/add/multiply semantics,
normalized versus physical domains, precedence, smoothing, and control rate remain unresolved.
[NODE_GRAPH](NODE_GRAPH.md) owns the preferred direction for optional parameter connectors.

## Open architecture work

[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks feasibility, ownership/lifetime, control overload, scheduling,
and other validation gaps. Promote a supported choice through [DECISIONS_LOG](DECISIONS_LOG.md) and
the affected owner, with the experiment's limits intact. Do not treat a successful probe as proof of
an entire future workstation architecture.
