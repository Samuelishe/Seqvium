# Extensions

Role: Optional-capability and extension lifecycle contract.
Read when: Designing extension boundaries, packages, removal, or missing-capability behavior.
Authoritative for: Categories, base-versus-optional policy, lifecycle principles, missing-extension preservation.
Not authoritative for: A final public API, package format, project container, DSP implementation, or store.

## Base and optional capabilities

Optional contributed capabilities should eventually be installable/removable. Internal modularity
does not make fundamental workstation subsystems user-removable plugins. The core responsibility
boundary is owned by [ARCHITECTURE](ARCHITECTURE.md#core-platform-responsibilities): project/document,
transport/clock, audio execution, graph engine, device/input and audio/MIDI recording foundations,
mixing/buses, resources, undo/serialization, extension hosting, and main workspace infrastructure.

Plugins are attached guests of this platform. They consume host contracts/services without replacing
project integrity, transport, graph engine, or audio-device ownership. Core modules may have replaceable
internal implementations; an audio-device adapter is not automatically a public user plugin mechanism.

| Candidate category | Boundary direction |
| --- | --- |
| Sample/content packs | May contain data only, with no executable code |
| Presets | Data tied to a capability; compatibility needs identity/version handling |
| Creative sample generators | Bounded asynchronous work outside realtime audio |
| Instruments | Realtime sound production with a stricter processing/lifetime contract |
| Realtime effects | Realtime processing with a stricter processing/lifetime contract |
| Specialized node types | Contribute generators/processors/modulation to the core host graph; do not own its engine |
| Future external plugin bridges | Separate hosting/integration concern; CLAP/VST3 strategy remains open |

Categories need not share an identical runtime contract. Do not create a universal API to cover
unproven needs. Early extension infrastructure should support honest first-party optional modules:
identity, local discovery, lifecycle/error handling, and capability-specific boundaries. No store or
marketplace belongs in the initial foundation.

Basic routing, level control, and common processing must work without optional downloads;
[NODE_GRAPH](NODE_GRAPH.md) owns the core-node principle. Specialized nodes, generators, instruments,
effects, presets, and content may extend it. The removable default generator remains an optional
capability; this does not make the host graph or audio resources removable with it.

## Host context and compatibility

Plugins consume the host processing environment, including sample rate, frame/block count, channels,
tempo, transport/musical position, and supported capabilities where relevant. General realtime
processing must adapt to supported rates/configurations rather than assume `44.1 kHz`; exact supported
ranges remain open. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns this device-independent contract. Ordinary
processing plugins must not bind directly to miniaudio, WASAPI, ASIO, ALSA, PipeWire, or CoreAudio.

Compatibility must be resolved deliberately using API/contract version, required and optional host
capabilities, processing format/range support, and compatible state/schema range where relevant.
Possible outcomes are normal loading, unavailable optional capability, an explicitly supported
compatibility path, or disable/reject with a clear diagnostic. An older plugin's lack of a newer
optional capability must not itself fail the application or whole project.

If no safe compatibility path exists, disable the plugin rather than deliberately take down the host.
Preserve its state and relationships under the missing-extension rules below. The final resolver,
manifest/schema, and compatibility paths are not selected.

## Failure containment limits

The host should contain recoverable failures and disable incompatible/failing plugins where its
hosting model technically permits. Compatibility/error boundaries can handle declared incompatibility
and ordinary recoverable failures; they cannot guarantee arbitrary in-process native code will never
crash Seqvium. Native code may corrupt process memory or cause unrecoverable faults.

Hard crash isolation would require a stronger boundary, such as out-of-process hosting/sandboxing.
That remains a future design/risk question, not an adopted architecture. Declared incompatibility is
never permission to intentionally fail the entire host. [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks
this distinction; no plugin host is implemented yet.

## Default generator

The creative sample generator should ship enabled by default as an ordinary removable/installable
extension. It must not rely on privileged inseparable core behavior. The initial direction is procedural,
local generation; cloud AI is not a foundational dependency. The exploration surface and durable
acceptance semantics are owned by [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md).

## Lifecycle and missing capabilities

Discovery, activation, errors, deactivation, and removal will need explicit ownership. Installation or
removal must not unnecessarily destroy music. Removal during active processing must respect audio
resource lifetime; exact live-removal and restart behavior remains open.

- **Accepted generated sample:** rendered audio remains usable after generator removal. Recipe editing
  or regeneration may be unavailable until a compatible generator returns.
- **Used content-pack sample:** if copied/embedded into managed project resources, pack removal must
  not break the project. Externally referenced media needs an explicit policy rather than an implied guarantee.
- **Realtime instrument/effect:** when the algorithm is needed to reproduce sound, preserve stable
  extension identity, serialized state, musical relationships, and sufficient opaque/unknown data.
  The eventual UI must explicitly represent the missing extension; exact playback fallback is open.
- **Contributed node type:** preserve node identity/state and graph relationships if its plugin is
  missing, disabled, or rejected. Missing processing and safe execution behavior need explicit handling;
  deleting unknown nodes/connections on save is not a compatibility strategy.

Saving must not silently discard unknown extension state. Missing code cannot reproduce its algorithm
merely because state is preserved; these are distinct concerns. Reinstallation/rebinding compatibility
needs evidence. [PROJECT_FORMAT](PROJECT_FORMAT.md) owns on-disk preservation and compatibility.

## Not selected yet

Identity namespace, version compatibility, manifest and package format, executable trust/isolation,
loading mechanism, API/ABI, update policy, and realtime contracts are open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
SEQ-R6 should implement only what the first optional content/generator modules require; later realtime
capabilities must meet [AUDIO_ENGINE](AUDIO_ENGINE.md), not inherit an asynchronous generator contract.
