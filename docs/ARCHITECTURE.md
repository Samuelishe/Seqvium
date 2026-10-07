# Architecture

Role: Logical responsibility and dependency boundary guide.
Read when: Structuring code, reviewing coupling, or evaluating architecture proposals.
Authoritative for: Core/plugin/backend boundaries, timeline/organization/graph separation, musical/resource identities, local processing, host UI services.
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
| User configuration / diagnostics | Host owns application preferences and bounded production diagnostics; [SETTINGS](SETTINGS.md) owns policy |
| Localization / semantic UI resources | Host owns shared localization and theme/style contracts for first-party UI and Seqvium-native contributions |

Basic musical editing, routing, level control, and common processing must be usable without optional
downloads. Specialized generators/nodes/instruments/effects/content may extend the platform;
[EXTENSIONS](EXTENSIONS.md) owns categories, compatibility, and lifecycle. These core responsibilities
do not require separate projects, interfaces, or a public plugin API now.

Application state may control audio through a prepared bounded boundary; the engine must not depend
on UI-thread progress. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns the realtime contract. Production runtime
must not depend on experiment hosts or repository retrieval tools.

[PORTABILITY](PORTABILITY.md) owns the Windows/Linux/macOS product target and portable engineering
rules. Windows is primary early development; Linux/macOS are architectural targets from the start,
without release-date or validated runtime-parity claims.

## Internal modules, backends, and plugins

- **Modular internal architecture:** core subsystems have clear contracts and justified replaceable/testable boundaries.
- **Replaceable backend:** an internal adapter may implement a core boundary, such as audio-device I/O.
  Replacing it is not automatically a user plugin API.
- **User extension/plugin:** an optional guest contributes capabilities through Seqvium host contracts;
  it does not replace project integrity, transport, graph engine, or audio-device ownership.

**Seqvium Platform defines the current host rules and contracts. Plugins are guests of that platform.**
[EXTENSIONS](EXTENSIONS.md#host-context-and-compatibility) owns required-contract declarations,
compatibility before activation, and retention of installed incompatible plugins. Preserving project
data does not promise exact historical sound; [AUDIO_ENGINE](AUDIO_ENGINE.md#sound-compatibility-boundary)
owns the platform boundary and extensions own plugin-algorithm responsibility.

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

C# / .NET 10 / Avalonia is the application-layer candidate, not an installed stack here.
[DEVELOPMENT](DEVELOPMENT.md) owns the developer environment, SDK/tool version authority, and setup;
[CODING_GUIDELINES](CODING_GUIDELINES.md) owns future implementation conventions.

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
- Repeated pattern clips normally reference shared Pattern musical content; an explicit operation
  creates an independent musical-content variation without implicitly detaching sound definitions.
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

## Project tempo and audio time

The project-wide tempo governs musical time. Notes, Patterns, their musical placements, later
automation, and other musical-time entities remain positioned in musical time as BPM changes.
Audio material additionally needs an explicit relationship to that tempo. At minimum, the model
must express concepts equivalent to:

- **Follow project tempo:** audio stays aligned to a musical duration/beat structure as BPM changes.
- **Fixed/source time:** audio retains its physical playback duration unless explicitly stretched.

A clip also supports local stretch independently of changing global tempo. Global tempo changes
and local clip stretch are distinct operations. Public labels, defaults, time/stretch representation,
and algorithms remain open. [UX_CONTRACT](UX_CONTRACT.md#audio-timeline-editing) owns distinct trim,
loop/repeat, and stretch intentions and ordinary non-destructive timeline edits;
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns preserving those relationships, not a selected schema.

Project-affecting configuration is independently project-owned, including tempo/time signature and
applicable sound/timing/processing settings. New-project defaults are copied at creation, never
live-linked to user preferences under [SETTINGS](SETTINGS.md#user-configuration-and-project-state).
[PROJECT_FORMAT](PROJECT_FORMAT.md#required-direction) owns identifying/version compatibility metadata
and useful open/migration diagnostics without selecting a schema.

## Separate sharing identities

Pattern musical content, instrument/sound definition, placement state, and processing state are
distinct identities whose sharing or independence must be expressible separately. There is no single
universal linked/unlinked state. Editing shared Pattern content changes every placement using it;
changing placement-local position, processing, or future fades does not change all those placements.

An explicit action conceptually called `Make Pattern Variation` copies/detaches musical content.
It must not implicitly detach every associated sound, resource, or processing relationship. A separate
intention, conceptually `Make Sound Independent`, may copy/detach an instrument/sound definition.
These are conceptual names, not selected commands, UI, or internal copy mechanics. A hidden
unlink-everything operation must not be the only model. [UX_CONTRACT](UX_CONTRACT.md) owns making
meaningful sharing understandable; [PROJECT_FORMAT](PROJECT_FORMAT.md) owns relationship preservation.

## Processing granularity and shared definitions

An atomic note, trigger, or step does not automatically own an arbitrary full DSP graph. Events may
eventually carry lightweight expressive properties such as pitch, intensity/velocity, duration, note
expression, or equivalent bounded controls. An unrestricted local processing graph belongs to a
standalone musical item/clip/fragment/placement-like scope, not every event in a Pattern.

For one hit needing substantially independent processing, provide a low-ceremony path conceptually
like `select event -> process separately / make independent fragment -> standalone item -> local graph`.
Users should not need to understand internal decomposition to make that hit sound different. Exact
command, event-to-item transformation, domain names, and graph ownership remain open.

Shared instrument settings/definition must not automatically imply shared execution state or
irreversible mixed audio. Placements A and B may use the same Bass Synth definition while A is clean
and B uses distortion. Even when they overlap, execution must preserve enough separation to honor
their different local/downstream processing before irreversible mixing.

Voice groups, execution instances, prepared routes, or other bounded representations are possible
later mechanisms; none is selected. There is no required instance count or promise of unlimited/free
duplication. Separation may cost CPU/memory. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns execution constraints;
Q-047 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) requires later resource/performance evidence, including
third-party instruments that may need explicit instance duplication or another bounded strategy.

## Resources, placements, and two local processing levels

A durable audio resource and a musical occurrence/placement of it must not be assumed to be one
mutable object. Several items may reference `kick_017.wav`; local processing of one item must not
silently rewrite the shared resource or every other use. The default creative model is non-destructive.
An explicitly destructive/edit-source operation may be justified later, but ordinary item processing
does not imply it. Resource/reference/edit ownership and storage schemas remain unselected;
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns persistence obligations.

Used audio is normally durable project-managed media, with explicit external-reference alternatives
under [PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary). That owner defines covered material
and preservation obligations; this architecture boundary does not select storage mechanisms.

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

Moving material into a context that owns processing or other meaningful behavior subjects it to that
context. Moving a Kick from a Drums container with Compressor into a Lo-Fi container with Distortion
and Delay changes its sound as expected. Pure organizational grouping must not silently change sound.
Organizational groups, musical/timeline containment with processing semantics, and mixer routing stay
distinct; the UI must make meaningful processing contexts distinguishable. This does not select final
Track/Layer/Container terminology or settle the container-to-mixer relationship.

## Future parameter control

Parameter modelling must leave room for future control rather than treating DSP parameters as
permanently fixed primitive values. An eventual effective parameter may combine base/user value,
timeline automation, modulation, and envelopes/LFO/control-graph sources. This requirement does not
select an abstraction or require an automation implementation now. Replace/add/multiply semantics,
normalized versus physical domains, precedence, smoothing, and control rate remain unresolved.
[NODE_GRAPH](NODE_GRAPH.md) owns the preferred direction for optional parameter connectors.

## Host localization and UI resources

Localization is a platform concern. Seqvium UI is intended to support Russian and English initially,
with additional languages later. Translated user-visible display text must not serve as stable internal
identity. First-party Seqvium UI/extensions consume host localization resources/contracts rather than
hard-code one language into reusable UI. Resource format, contribution workflow, and fallback rules
remain open; canonical repository/source language remains English under [CODING_GUIDELINES](CODING_GUIDELINES.md).

The host also owns centralized semantic theme/style resources for first-party UI and Seqvium-native
plugin surfaces. [UI_DESIGN](UI_DESIGN.md#themes-and-semantic-resources) owns Dark/Light baseline
direction and resource roles. Theme packaging as extensions, plugins, or data packages is undecided;
independently rendered third-party native editors need not adopt host themes. These service boundaries
do not select an SDK/API, UI framework, localization files, or theme packages.

## Open architecture work

[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks feasibility, ownership/lifetime, control overload, scheduling,
and other validation gaps. Promote a supported choice through [DECISIONS_LOG](DECISIONS_LOG.md) and
the affected owner, with the experiment's limits intact. Do not treat a successful probe as proof of
an entire future workstation architecture.
