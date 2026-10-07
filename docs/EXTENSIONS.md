# Extensions

Role: Optional-capability and extension lifecycle contract.
Read when: Designing extension boundaries, packages, removal, or missing-capability behavior.
Authoritative for: Categories, base-versus-optional policy, compatibility/fallback, lifecycle/removal safety, degraded availability and dependency blockers.
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

Seqvium Platform defines the current host rules and contracts; plugins are guests. Each plugin version
must declare and satisfy the host contracts/capabilities it requires. New plugin development should
target the current Seqvium platform/SDK contract available at development time, rather than evolve
independently of host semantics. This does not select final manifest fields, API, or version-range mechanics.

Plugins consume the host processing environment, including sample rate, frame/block count, channels,
tempo, transport/musical position, and supported capabilities where relevant. General realtime
processing must adapt to supported rates/configurations rather than assume `44.1 kHz`; exact supported
ranges remain open. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns this device-independent contract. Ordinary
processing plugins must not bind directly to miniaudio, WASAPI, ASIO, ALSA, PipeWire, or CoreAudio.

Compatibility must be resolved deliberately before activation using API/contract version, required and
optional host capabilities, processing format/range support, and compatible state/schema range where relevant.
Conceptual graded outcomes include compatible, compatible with fallback/limited capability,
compatibility warning, incompatible and missing; these are semantic examples, not final public labels.
Fallback must be explicitly supported and diagnostics must identify meaningful limitations. `Deprecated`
means actual deprecation/retirement intent, not merely old but working. An older plugin's lack of a newer
optional capability must not itself fail the application or whole project.

A plugin is not incompatible merely because it was built for an older Seqvium version. The current
platform must be able to satisfy its declared required contracts/capabilities and state requirements;
age or version ordering alone is not a rejection criterion. Prefer lightweight metadata/manifest/
capability checks where possible, without expensive mandatory runtime self-tests of every plugin at
every application/project startup. Real activation/materialization failure may also disable/reject a
plugin with diagnostics. Required activation/materialization must be bounded, not an indefinite wait.
Final negotiation mechanics remain open.

A known incompatible plugin must not be activated. Installation may be rejected if incompatibility
is known beforehand. An already installed plugin may remain present but unavailable/incompatible;
losing support must not automatically uninstall or delete it. The user decides whether to remove it.
Preserve project/plugin identity, state, and relationships where applicable under the missing-extension
rules below. The final resolver, manifest/schema, and compatibility paths are not selected.

## Independent execution capability

Future instrument hosting must honor the [execution-domain contract](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains).
A source exposing only one aggregate output per execution instance cannot provide independently
processable overlapping A/B through that output. Divergent required routes may need multiple execution
instances initialized from the same durable sound definition, or another genuine separation capability
supplied by the source. Copying/splitting the aggregate is insufficient. Separable outputs must also
preserve the requested performance interaction; multiple mono/legato instances do not automatically
reproduce one interacting performance. Shared preset/state intent is not shared live voice state.

Do not mandate one plugin instance per placement. Determine whether the available source can realize
the requested performance-state and output independence, including shared-definition edits and safe
occurrence retirement. Exact declarations, capability negotiation and state exchange remain Q-024/Q-047;
no plugin API, instance pool, host implementation or source-support claim is selected.

If capability, activation or resource limits prevent correct realization, retain the document and
identify the affected uses and unmet execution requirement under
[degraded operation](#degraded-project-opening-and-operation-blockers). Block affected playback/render
as required by dependency scope; healthy paths remain available only where semantics permit. A supported
fallback may preserve semantics; changing performance interaction, routes or source needs an explicit
informed choice. Never silently collapse routes, discard preserved sound state or report incorrect audio
as successful execution. Final hosting/fallback UX remains future design.

## Plugin metadata and localization direction

Leave room for stable plugin identity, package/version, plugin state/schema version, required host
contract/capability ranges, optional capabilities, supported localization/languages, fallback/default
language, and optional last-tested/last-updated Seqvium platform information. Platform-age metadata
helps diagnosis; `older than host -> reject` is not the rule. No manifest format, exact fields or API/ABI
is selected; Q-024 retains negotiation representation and state compatibility schema.

Missing localization must use a usable common fallback rather than disable working processing.
Seqvium-authored / Seqvium-native first-party contributions use English as the baseline fallback;
Russian/English are initial platform directions with room for other languages. A plugin supporting
RU/EN uses English when host Spanish is unavailable without changing host language. Independent
third-party native editors remain outside host-rendered localization control where applicable.
[ARCHITECTURE](ARCHITECTURE.md#host-localization-and-ui-resources) owns the platform principle;
Q-054 retains resource format, contribution mechanism and fallback schema/details.

## Plugin sound responsibility

A third-party/optional plugin owns the sound produced by its algorithm/version. Seqvium does not
promise to emulate older versions of arbitrary plugin algorithms. Preserving identity/state or
structural compatibility does not guarantee exact historical sonic identity. The platform likewise
does not promise indefinite historical engine emulation under
[AUDIO_ENGINE](AUDIO_ENGINE.md#sound-compatibility-boundary); specific future breaking changes need
deliberate policy when they occur.

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

First-party UI/extensions consume host localization contracts and Seqvium-native plugin surfaces
consume centralized semantic UI resources under [ARCHITECTURE](ARCHITECTURE.md#host-localization-and-ui-resources)
and [UI_DESIGN](UI_DESIGN.md#themes-and-semantic-resources). Independently rendered external native
editors are not required to adopt Seqvium themes.

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

Discovery, activation, errors, deactivation, and removal need explicit ownership. Installation or
removal must not unnecessarily destroy music; instance edits and global package removal follow
different safety contracts below. Exact package manager mechanics remain open in Q-010.

- **Accepted generated sample:** rendered audio remains usable after generator removal. Recipe editing
  or regeneration may be unavailable until a compatible generator returns.
- **Used content-pack sample:** normally becomes managed project media by default; later pack removal
  must not lose already managed used audio. Explicit external references are an alternative governed by
  [PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary), not the normal durability path.
- **Realtime instrument/effect:** when the algorithm is needed to reproduce sound, preserve stable
  extension identity, serialized state, musical relationships, and sufficient opaque/unknown data.
  The UI explicitly represents the unavailable extension; affected execution is blocked where its
  dependency is required, rather than silently omitted with reported success.
- **Contributed node type:** preserve node identity/state and graph relationships if its plugin is
  missing, disabled, or rejected. Missing processing and safe execution behavior need explicit handling;
  deleting unknown nodes/connections on save is not a compatibility strategy.

Saving must not silently discard unknown extension state. Missing code cannot reproduce its algorithm
merely because state is preserved; these are distinct concerns. Reinstallation/rebinding compatibility
needs evidence. [PROJECT_FORMAT](PROJECT_FORMAT.md) owns on-disk preservation and compatibility.

## Degraded project opening and operation blockers

Inability to reproduce all audio does not normally prevent document access. A missing, disabled or
incompatible ordinary plugin, or recoverable activation/materialization failure, normally opens a
project degraded when its document remains safely understandable. Preserve plugin/node identity,
serialized/opaque compatible state, relationships and musical references. Edit the available document
model; missing processing is not itself a document-format failure.
[PROJECT_FORMAT](PROJECT_FORMAT.md#opening-and-migration) owns critical schema/corruption/unsafe-migration
refusal and behavior-changing migration choice.

Block the operation requiring the broken dependency wherever possible, rather than the whole user/
project: document access and unrelated editing remain available; healthy paths may execute where
semantics permit; affected execution is unavailable; export/render is blocked when its frozen canonical
scope's dependency closure requires the broken capability. Never silently omit required musical/
processing dependencies and report success, or substitute stale realtime state for canonical render.

Diagnostics point at the actual affected object/node/instance and identify the required capability,
with restoration/removal/replacement direction. Conceptually a Pattern requiring missing `HardBass
Superbeater 7.0` explains that dependency; wording is unselected. Persistent blockers are current UI
state under [UX_CONTRACT](UX_CONTRACT.md#project-availability-and-dependency-blockers), supported by
logs rather than hidden in them. Reinstallation/reattachment still needs schema/lifecycle evidence.

## Instance removal and package uninstall

Removing an instance from a project is an ordinary document edit that should eventually be undoable
under normal project editing rules. Uninstalling/removing its package is a global capability operation.

Do not physically uninstall or unload a package with known active runtime use: active instances,
processing, open plugin editors, or currently open project dependencies requiring the loaded package.
Block or defer uninstall and explain the known dependency. Never unload beneath active realtime
execution. Check current application/project/library context, not every project file on the user's
filesystem. Exact deferred completion, restart/update/install failure, storage/location and broader
package lifecycle remain Q-010. This follows the general
[known-dependency destructive-operation rule](ARCHITECTURE.md#document-integrity-and-asynchronous-publication).

## Not selected yet

Plugin-global preferences are user configuration; sound/meaning-affecting plugin instance state belongs
with the project. [SETTINGS](SETTINGS.md) owns this separation and preference-reset boundaries;
[PROJECT_FORMAT](PROJECT_FORMAT.md) owns instance-state preservation. Executable installation/discovery
locations are separate from preferences and remain open.

Identity namespace, version compatibility, manifest and package format, executable trust/isolation,
loading mechanism, API/ABI, update policy, and realtime contracts are open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
SEQ-R6 should implement only what the first optional content/generator modules require; later realtime
capabilities must meet [AUDIO_ENGINE](AUDIO_ENGINE.md), not inherit an asynchronous generator contract.
