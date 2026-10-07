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

Seqvium hosts compatible third-party plugins according to defined host contracts. Third-party code
may contain defects: arbitrary native/in-process faults may crash the application, corrupt host memory,
or cause unrecoverable failures. Seqvium does not guarantee containment of those faults. This technical
limit does not excuse careless host implementation: Seqvium owns correctness of its host contract;
plugin developers own correctness of plugin-specific behavior and UI.

The host must deliberately handle ordinary declared/recoverable cases: missing or incompatible plugins,
unsupported capabilities, normal initialization and load/unload errors, recoverable processing failures
where the contract permits, and plugins removed/unavailable on reopen. Disable/reject with diagnostics
where appropriate and preserve project/plugin state under the compatibility contract. A known
compatibility failure must not intentionally crash the application.

Stronger out-of-process hosting, crash containment, sandboxing, or per-plugin/vendor processes may be
evaluated later if practical value justifies their complexity. They are not initial extension-architecture
requirements or necessary to satisfy this baseline. No process/IPC architecture is selected; optional
containment is retained in [IDEAS](IDEAS.md#i-005--out-of-process-external-plugin-crash-isolation) and
future evaluation in Q-025 of [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). No plugin host exists yet.

## External plugin lifecycle and editors

Later executable/external hosting needs an explicit minimal interoperability contract covering, as
applicable, compatibility/version negotiation; initialization; activation/deactivation; processing
start/stop; editor open/close; parameter/state exchange; save/load state; shutdown/disposal; host/project
closing; plugin absence/incompatibility; and declared supported audio/configuration capabilities.
This defines ordinary cooperation, not handling every possible plugin defect; exact API/ABI remains open.

If future hosting uses separate processes, normal lifecycle must include detecting host/plugin process
termination, disconnect/cleanup, and avoiding indefinite waits on a dead peer. This conditional contract
does not select out-of-process hosting.

Third-party editors need not visually match Seqvium. If a native/plugin editor cannot safely or
practically participate in internal overlapping panes, it may use a normal top-level OS window.
Seqvium retains the host-owned plugin relationship and editor lifecycle; plugin design quality belongs
to the plugin developer. Do not require excessive heuristic validation of arbitrary plugin UI decisions.
[WORKSPACE](WORKSPACE.md#one-main-application-window) owns the accepted workspace exception; normal
major Seqvium-authored surfaces continue to follow the internal Workspace Pane model where appropriate.

## Security boundary

Process separation, if adopted, is not automatically a security sandbox. Seqvium does not currently
promise containment of malicious plugin code. An untrusted executable plugin may have whatever OS
permissions its process receives unless a future explicit sandbox model restricts them. Crash isolation
does not imply filesystem, privacy, or security isolation. Trust/signing/sandbox policy remains future
design; no security sandbox is promised or selected.

Third-party plugins are independently authored software; compatibility does not make them
Seqvium-authored or warrantied by the Seqvium project. [THIRD_PARTY](THIRD_PARTY.md) owns the
provenance/license boundary; root [LICENSE](../LICENSE) remains authoritative for Seqvium's legal terms.

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
- **Used content-pack sample:** normally becomes managed project media by default; later pack removal
  must not lose already managed used audio. Explicit external references are an alternative governed by
  [PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary), not the normal durability path.
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
