# Architecture

Role: Logical responsibility and dependency boundary guide.
Read when: Structuring code, reviewing coupling, or evaluating architecture proposals.
Authoritative for: Accepted boundary constraints, proposed application shape, intended musical model.
Not authoritative for: Exact project decomposition, audio internals, extension API, file format, or progress.

No production architecture is implemented. Accepted constraints below bind future design; proposed
shape and model direction still need bounded design and evidence.

## Accepted constraints

Use simple architecture for the current product with extension points justified by known requirements.
An abstraction needs a concrete ownership or testability reason. Avoid enterprise layering,
speculative interfaces, and a disposable model that must be replaced for Piano Roll or Arrangement.

Keep these logical responsibilities distinct even if some later share an assembly:

| Responsibility | Boundary |
| --- | --- |
| Application shell / UI | Presents and edits application state; does not own realtime execution |
| Project / document | Owns musical relationships, edits, resource references, and document integrity |
| Audio resource ownership | Controls availability and lifetime of project audio independently of optional UI |
| Audio execution | Owns execution-time state; isolated from arbitrary mutable application/UI objects |
| Extension discovery / lifecycle | Coordinates optional capabilities; fundamental project integrity stays in the base |
| Serialization | Preserves versioned document and extension data under the format contract |

The eventual base must provide the shell, project/document semantics, transport foundation, basic
musical editing, audio resource ownership, extension discovery/lifecycle foundation, and essential
playback/mixing infrastructure. Optional capabilities must not become owners of project integrity.
[EXTENSIONS](EXTENSIONS.md) owns category and lifecycle details.

Application state may control audio through a prepared bounded boundary; the engine must not depend
on UI-thread progress. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns the realtime contract. Production runtime
must not depend on experiment hosts or repository retrieval tools.

Windows may be the first implementation target. Avoid deliberately preventing later Linux/macOS
adapters; this is not a commitment to release dates or validated platform support.

## Proposed application and audio shape

The primary development environment is Windows 11, Rider, C#, .NET 10, Avalonia, and Codex.
C# / .NET 10 / Avalonia is the application-layer candidate, not an installed stack here.

```text
C# / Avalonia application
    -> application/domain state
    -> narrow native boundary
    -> realtime native audio engine
    -> miniaudio or comparable backend
    -> system audio API/device
```

[SEQ-R0](ROADMAP.md#seq-r0--audio-architecture-probe) must test this direction before major production
implementation. Native engine language (including C++), backend, ABI, queue/snapshot design, and
C# project decomposition remain open. This diagram defines a hypothesis, not final classes or projects.

## Intended musical model

The current direction for SEQ-R1 is:

- An **instrument** produces sound.
- A **musical party** contains notes/events for one instrument. The term is provisional.
- A **pattern** is a reusable fragment containing one or more instrument parties.
- A **pattern clip** places/references a pattern in the Playlist/Arrangement's musical time.
- Repeated pattern clips normally reference shared data; an explicit operation creates an independent variation.
- Mixer channels describe audio processing/routing, separately from arrangement tracks.
- Step Sequencer and Piano Roll edit compatible underlying musical event data.

The event model must support musical position, pitch where applicable, duration, and velocity or
equivalent intensity. A `bool[16]` foundation is insufficient. These requirements do not select a
schema, time representation, class hierarchy, or storage layout. [PROJECT_FORMAT](PROJECT_FORMAT.md)
owns persistence compatibility; [UX_CONTRACT](UX_CONTRACT.md) owns observable editing behavior.

## Open architecture work

[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks feasibility, ownership/lifetime, control overload, scheduling,
and other validation gaps. Promote a supported choice through [DECISIONS_LOG](DECISIONS_LOG.md) and
the affected owner, with the experiment's limits intact. Do not treat a successful probe as proof of
an entire future workstation architecture.
