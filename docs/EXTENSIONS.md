# Extensions

Role: Optional-capability and extension lifecycle contract.
Read when: Designing extension boundaries, packages, removal, or missing-capability behavior.
Authoritative for: Categories, base-versus-optional policy, preset-source versus project-state boundaries,
compatibility/fallback, lifecycle/removal safety, degraded availability and dependency blockers.
Not authoritative for: A general public SDK/ABI, project container, DSP implementation, or store.

## Sample Lab generator package

R7 ships **seqvium.percussion-lab 1.0.0**, default enabled unless disabled in user preferences.
Its manifest requires `seqvium.sample-generator: 1` and `seqvium.sample-exploration: 1`; the latter
negotiates the bounded family/recipe/reference/lock declarations. Older hosts refuse the unmet
contract instead of ignoring variation intent. The exact package/version/capability factory creates
`PercussionGenerator`; no algorithm runs through a bypass when disabled. There is no assembly scan
or new general SDK. Browser and Sample Lab share one initialization/enablement owner and the existing
Extensions disclosure. The host retains copied sealed metadata and candidate WAVs, not provider UI/code.

`IExploringSampleGenerator` extends the async seam with bounded family-local numeric controls.
Requests capture intention, seed, reference recipe/identity and held values. The host validates output
audio and recipe family/ranges/seed/lineage/locks before transfer to its twelve-candidate history.
Provider use is counted through generation/validation, then released. Disable is refused while work
is active; after cancellation/join or ordinary completion it can retire the provider while idle
history remains. Accepted PCM owns no provider lease or generator dependency.
[Sample workflow](SAMPLE_WORKFLOW.md#implemented-sample-lab-v1) owns musical scope and acceptance.

## Implemented R6 local foundation

R6 implements a bounded local host in the existing Core/Desktop assemblies, with no public SDK/ABI
stability promise. `ExtensionPackage`, `ExtensionCapability`, a counted `ExtensionUse` and project-owned
`ResourceDescriptor` are separate identities/lifetimes. The supported categories are **ContentPack**
and **SampleGenerator**. Future realtime instruments/effects/nodes require their own concrete contracts;
they cannot execute through the asynchronous generator boundary.

Desktop discovers immediate directories under **`<application>/Extensions`** when Browser or Sample Lab first needs extensions.
This is a deployment-controlled root, not a scan of user folders or an installer. At most 64 packages,
64 KiB per `extension.json`, 16 capabilities, 32 requirements per capability and 256 content entries
per capability are admitted. Linked paths (including `extension.json` itself), absolute/escaping
content paths, duplicate package or capability identities, malformed metadata and unknown manifest
versions remain unavailable with host diagnostics. Valid metadata survives a missing declared file. Deployment updates/removal happen
while the app is closed; refresh of loaded/borrowed/failed-to-retire packages is refused.

Schema 1 declares `id`, independent three/four-component `version`, English `name`, optional
`russianName`, and `capabilities`. IDs are bounded lowercase dotted/hyphenated names. Each capability
declares `id`, `category`, `requires` and `content` (empty for a generator). Requirements contain a
`contract` plus inclusive integer `minimum`/`maximum` and optional `optional`. Content contains stable
`id`, `name`, and a package-relative WAV `file`. The shipped
[Found Sounds manifest](../src/Seqvium.Desktop/Extensions/seqvium.found-sounds/extension.json) is a complete example.

The current contracts are **`seqvium.content.wav: 1`**, **`seqvium.sample-generator: 1`** and
**`seqvium.sample-exploration: 1`**. Each
category must require its corresponding contract. All required contracts must match before any
package activation; one unmet required contract makes this bounded package unavailable atomically.
Optional contract failures produce a usable fallback diagnostic. Product-version age is never a gate.
An executable factory must additionally be registered by the host for the exact package ID, package
version and capability ID. No manifest assembly/type path, DLL scanning, reflection load, native
hosting, sandbox or external store exists. Placing DLLs beside a manifest does not execute them.

States distinguish Disabled, Activating, Available, Deactivating, Incompatible, Damaged and Failed;
missing identities receive an explicit missing diagnostic. Activation runs on a worker with a
five-second caller deadline. Cancellation/deadline withdraws use immediately; an uncooperative late
activation stays supervised/unavailable until it exits and its created providers are disposed. It is
never unloaded underneath running code. Ordinary activation failure allows retry after cleanup.
Failed disposal retains references, refuses discovery/re-enable, and allows a later disable retry.
The in-process trusted contract requires cooperative termination; R6 cannot kill arbitrary stuck code.

Disable is **blocked**, not queued, while generation, inspection/import or an open dependent session
holds a use. The host explains that work must finish/cancel and editors must close. Once drained,
disable withdraws acquisition before awaited provider disposal. Re-enable creates a fresh provider
through the registered factory. Application shutdown cancels borrowers, joins pending activation/work,
then disposes providers; document replacement closes and joins its generator sessions first. Accepted
media owns no package lease. There is no physical package deletion command.

### R6 generator and acceptance boundary

`ISampleGenerator` declares a bounded numeric parameter schema and accepts immutable parameters plus
document lifecycle/generation, requested sample rate and maximum output frames. Generation runs outside
UI/document/audio callbacks, serialized per provider across sessions. The host accepts supported mono/
stereo WAV candidates, up to 16 MiB and the requested rate/frame bound (context permits 8–192 kHz and
at most 30 seconds). No document, device, graph, transport or Undo object is passed to the provider.

`SampleGenerationSession` owns the editor/use lease, pending requests and one temporary candidate.
Supersession/cancellation/close invalidates prior requests and disposes late outputs even if the provider
ignores cancellation. Queued work is bounded; provider errors are recoverable. A `SampleCandidate` has
its own UUID, name, availability and disposable host-owned WAV bytes; it is not a durable ResourceId.
There is no candidate-history or recipe persistence in R6.

The Desktop acceptance seam captures an explicit Rack destination, checks the original document and
generation, prepares through existing managed WAV import, and rechecks candidate identity at the final
synchronous session/document commit gate. Acceptance creates media/sound/use with one ordinary Undo
entry. Temporary transport files are removed on every outcome. Cancelled, superseded, closed or stale
results cannot edit another project. Pack Rack/Playlist uses follow the same existing managed paths;
accepted audio remains playable/renderable after package disable/removal and portable Save As/Open.
Existing opaque unavailable sound/node state remains preserved and affected execution remains blocked;
R6 adds no realtime provider, project field or format-reader gate.

The test-only functional provider proves activation, generation, cancellation, stale/late disposal,
document/application close and durable acceptance. It tests the R6 seam independently; the R7 package and host surface above add real
families, reference/variation/locks, contextual audition and bounded history.
Those concrete request extensions must retain R6's ownership, cancellation and final acceptance gates;
R6 does not invent universal similarity or promise cross-version regeneration.

### R6 host presentation

Browser's **Extensions** disclosure lists package name/version and availability, with identity and
diagnostics on inspection and Sounds/Enable/Disable actions. Enabled pack sounds join the existing Browser
location/list/preview/drag and musical-use paths. Disabling removes contributed locations/selections;
old captured drag sources must reacquire availability and are refused after disable. Host-owned metadata
labels require no executable activation. Optional absent/invalid Russian names fall back to English;
there are no contributed dictionaries, styles, icons or editor factories to unload in this slice.
All actual controls use existing Onest/semantic resources and RU/EN Dark/Light. Broader contribution
registration/versioning remains future work, rather than a generic resource registry added for R6.
User enablement persistence belongs to [Settings](SETTINGS.md#extension-enablement).

The [R6 report](experiments/SEQ-R6-EXTENSION-FOUNDATION_REPORT.md) owns verification and native-input limits.

## First built-in instrument boundary

The first Drum Voice is a host-shipped, versioned sound algorithm with a first-party editor. It uses
existing canonical sound parameters and derived audio preparation, without package discovery, an
optional install/uninstall lifecycle or a plugin SDK. It is not the removable creative sample generator:
R6/R7 retain their identity/package/exploration/acceptance direction. A later instrument provider must
separately establish execution/state/lifetime compatibility; this bounded implementation does not
publish a universal instrument ABI. [Architecture](ARCHITECTURE.md#built-in-instrument-ownership)
and [Audio](AUDIO_ENGINE.md#built-in-drum-voice) own its actual boundaries.

## Base and optional capabilities

Optional contributed capabilities should eventually be installable/removable. Internal modularity
does not make fundamental workstation subsystems user-removable plugins. The core responsibility
boundary is owned by [ARCHITECTURE](ARCHITECTURE.md#core-platform-responsibilities): project/document,
transport/clock, audio execution, graph engine, device/input and audio/MIDI recording foundations,
mixing/buses, resources, undo/serialization, extension hosting, and main workspace infrastructure.

Plugins are attached guests of this platform. They consume host contracts/services without replacing
project integrity, transport, graph engine, or audio-device ownership. Core modules may have replaceable
internal implementations; an audio-device adapter is not automatically a public user plugin mechanism.

| Candidate category             | Boundary direction                                                                        |
|--------------------------------|-------------------------------------------------------------------------------------------|
| Sample/content packs           | May contain data only, with no executable code                                            |
| Presets                        | Data tied to a capability; compatibility needs identity/version handling                  |
| Creative sample generators     | Bounded asynchronous work outside realtime audio                                          |
| Instruments                    | Realtime sound production with a stricter processing/lifetime contract                    |
| Realtime effects               | Realtime processing with a stricter processing/lifetime contract                          |
| Specialized node types         | Contribute generators/processors/modulation to the core host graph; do not own its engine |
| Future external plugin bridges | Separate hosting/integration concern; CLAP/VST3 strategy remains open                     |

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
independently of host semantics. The bounded R6 representation is above; a general SDK/ABI remains open.

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
rules below. R6 selects only the local categories/contracts above; general processing/state negotiation remains open.

## Independent execution capability

Future instrument hosting must honor
the [execution-domain contract](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains).
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
is selected for general external hosting; the R6 local schema is above. Q-024 retains broader state compatibility.

Localization support/fallback metadata is independent of executable capability/version compatibility.
Missing localization does not reject otherwise compatible processing or change the host language.
[ARCHITECTURE](ARCHITECTURE.md#language-support-and-resource-fallback) owns per-resource fallback,
the initial first-party RU/EN baseline and English fallback; Q-054 retains concrete format/schema.
Human-readable metadata should be discoverable without requiring activation of arbitrary plugin code;
R6 implements that bounded metadata path above; a general resource registration API remains open. Independent third-party native editors
remain outside host-rendered localization control where applicable.

## UI resource contribution lifecycle

R6 extends the existing host foundation only for actual first-party/native extension surfaces; R3 shell
localization/styling must not wait for mature plugin hosting. Registration associates resources with a
stable contributor scope, supported languages/fallback and availability under
[Architecture](ARCHITECTURE.md#stable-identity-and-provider-ownership). Similar names from separate
providers cannot silently replace each other or the host baseline. Late contributions use the current
host language and semantic theme through host resolution, without resetting user preferences or
requiring executable activation solely to discover labels. Registration here is a semantic lifecycle
obligation, not a chosen load mechanism, manifest or public API.

When a contributor becomes unavailable, disabled or removed, the host must safely withdraw/invalidate
its active labels, icons, style contributions and native UI content. Close, detach or replace dependent
surfaces with host-owned presentation as appropriate; no dangling active UI references or stale callback
dependency on retired contributor code may remain. Do not unload beneath known active use: existing
[uninstall safety](#instance-removal-and-package-uninstall) applies to processing and open editors;
presentation dependencies also need safe retirement. Exact sequencing, caching, reference counting,
load/unload and deferred disable/uninstall mechanics remain Q-010/Q-067.

Retiring presentation must not erase project-owned capability/node/port identities, relationships,
saved resources or serialized/opaque state. Missing-capability explanation uses host-owned resources
and available stable metadata; it must work without keeping/activating the absent executable just to
display an error label. Known missing processing remains a scoped execution blocker under
[degraded operation](#degraded-project-opening-and-operation-blockers), not a translation failure.

Missing/invalid noncritical translation, icon or style contributions use host fallbacks under
[Architecture](ARCHITECTURE.md#language-support-and-resource-fallback) and
[UI design](UI_DESIGN.md#themes-and-semantic-resources). They must not silently hide unrelated
capabilities, remove data or change executable compatibility. If a particular contributed UI cannot
be rendered safely, contain that surface and explain its unavailability through host UI while retaining
processing where independently safe and compatible. No universal recovery from malformed third-party
resources/code is promised. Required executable/API incompatibility still follows Q-024; absence of a
translation alone is not such incompatibility. Resource contract evolution/versioning must be deliberate,
but its representation and compatibility rules remain Q-067 with Q-054/Q-055, not an assumed plugin ABI.

## Plugin sound responsibility

A third-party/optional plugin owns the sound produced by its algorithm/version. Seqvium does not
promise to emulate older versions of arbitrary plugin algorithms. Preserving identity/state or
structural compatibility does not guarantee exact historical sonic identity. The platform likewise
does not promise indefinite historical engine emulation under
[AUDIO_ENGINE](AUDIO_ENGINE.md#sound-compatibility-boundary); specific future breaking changes need
deliberate policy when they occur.

## Preset sources and project state

A reusable preset/template has identity/content separate from the project sound/plugin state it creates
or updates. Explicitly applying/accepting an installed or Personal Library preset is a project edit:

```text
library/installed preset source -> apply/accept -> project-owned sound/plugin state
```

The accepted project must retain sufficient compatible configuration, capability identity and state
to reproduce its intended sound under this document's compatibility/sound-responsibility contracts.
Later renaming, editing, overwriting or deleting the source preset must not silently rewrite existing
project state or make it dependent on that mutable preset. Provenance may be remembered separately.
Any audio accepted from a preset's source follows normal managed-media durability; retaining preset
state does not turn missing required executable processing into available audio.

Do not assume every plugin exposes host-readable parameters. Sound-defining opaque plugin state may
be project-owned; its identity/state/schema preservation and compatible restoration remain necessary
under [Project format](PROJECT_FORMAT.md#source-provenance-and-reusable-content). This does not promise
exact historical sonic identity across algorithm changes or select a parameter/state-exchange API.
Missing/incompatible capabilities retain their normal degraded access and dependency-scoped blockers.

Explicitly saving modified sound settings for future projects publishes reusable user content, separate
from ordinary project Save and from the project's own state edits. User-created presets belong to the
[Personal Library boundary](SAMPLE_WORKFLOW.md#personal-library-and-explicit-publication), not disposable
plugin-global preferences. Project Undo normally covers application/configuration edits to the project,
not independent preset publication. An intentionally combined edit/publication has distinct effects
and failure outcomes; no shared storage object or global-history UX is presumed.

Linked/live presets, versioning, overwrite/update workflow, plugin-defined formats, categories/tags
and portable preset packages remain open or require separate justification. Q-024 retains capability/
state compatibility and negotiation; Q-065 retains concrete reusable-preset/discovery mechanisms.

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
future evaluation in Q-025 of [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). R6 is a trusted local asynchronous/data host only.

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

### Native node editor lifecycle proposal

**SEQ-NODE-EDITOR-DESIGN-R1 — native hosting remains proposal pending owner acceptance.** This identifies future host
responsibilities only; it does not select CLAP/VST3, embedding, process isolation, platform handles,
public ABI or a realtime extension contract. SEQ-NODE-EDITOR-IMPL-F1 implements only the accepted
built-in Desktop slice in one internal Workspace pane; this grants no native-hosting authorization.

Keep logical project node/sound target, materialized processing instance, editor-open session and OS
window identity separate. A shared sound definition can require several execution instances under
[execution domains](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains); which instance's
editor may expose which shared state needs capability-specific resolution. Do not create an extra
processor merely to open a window, or bind an editor to whichever instance happens to be playing.

Propose one ordinary native editor-open session per resolved target/instance relationship: repeated
open activates an existing window; pending open is deduplicated and can be invalidated. Opening checks
live document target, compatible available provider and editor support. Materialization/activation
may be needed for a plugin UI but does not authorize transport or opening an audio device. Editor
absence/failure uses host details and explanation; independently safe processing may continue.

Window close retires editor callbacks/UI resources without deleting project state or implicitly
disposing a still-used processor. Target deletion/provider loss/document retirement withdraws editor
mutation authority, invalidates delayed open/parameter results and closes/detaches the window before
releasing its instance/provider references. Undo may permit a fresh open, never resurrection of an
old native handle. Cancelled/failed project replacement retains valid current-session ownership;
successful replacement must retire old editor use before document/provider disposal. Exact UI-thread,
owner-thread and audio join ordering needs bridge-specific evidence; do not block either indefinitely
or unload when a recoverable close fails. Retain ownership and report the failure for safe retry.

Closing the host must account for editor/native callbacks as well as existing prepared/audio joins.
Native focus/input belongs to that editor; return to a still-live host context without foreground
forcing. Package disable/uninstall follows known-active-use refusal/defer, including open editors and
presentation resources. Safe callback drainage, late notification rejection and resource detachment
remain Q-010/Q-024/Q-067 obligations; an OS window itself provides no crash containment.

Contributor-scoped editor lookup/availability, host resources/fallback and resource contract evolution
must wait for actual R6 modules and [UI contribution lifecycle](#ui-resource-contribution-lifecycle).
External-native hosting is **later than, and not a required deliverable of, R6 Extension Foundation**.
Its state exchange must use
the [single canonical state boundary](ARCHITECTURE.md#node-editor-state-and-lifetime-proposal),
not an independent plugin/editor Save or Undo model.

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

### Generator exploration capabilities

Each supported generator family must give an honest meaning to its random/related variation and any
exposed locks under [Sample workflow](SAMPLE_WORKFLOW.md#intentional-and-lazy-exploration). Related
variation requires supported reference inputs; a family need not analyze arbitrary audio or reproduce
another family's controls. Locks preserve declared supported constraints, with coupled/unsupported
properties represented honestly. Family/version compatibility cannot be inferred from matching control
labels, and unsupported variation cannot silently become random generation. No universal distance,
parameter/lock schema, capability manifest or mandatory complete recipe is selected; concrete declarations
and compatibility coordinate Q-024/Q-071.

Retained candidate audio is distinct from a recipe or historical metadata. Accepted project audio
survives generator removal; safely owned temporary audio may remain useful under bounded retention,
while generation/editing actions needing missing code become unavailable. Host-readable preserved
identity/provenance must not require keeping executable code alive merely to display candidate history.
Removal/disable must safely retire known active generation and contributed UI use rather than unload
beneath it; exact cancellation/defer/retention mechanisms remain Q-010/Q-067/Q-071. No persistence promise
for temporary candidates or cross-version bit-identical regeneration follows from metadata preservation.

### Material and capability availability

Discovery, activation, errors, deactivation, and removal need explicit ownership. Installation or
removal must not unnecessarily destroy music; instance edits and global package removal follow
different safety contracts below. Exact package manager mechanics remain open in Q-010.

- **Accepted generated sample:** rendered audio remains usable after generator removal. Recipe editing
  or regeneration may be unavailable until a compatible generator returns.
- **Used content-pack sample:** normally becomes managed project media by default; later pack removal
  must not lose already managed used audio. Explicit external references are an alternative governed by
  [PROJECT_FORMAT](PROJECT_FORMAT.md#media-policy-boundary), not the normal durability path.
- **Preset source:** removing its pack/library entry changes discovery availability, not already
  accepted project state. Required executable capability removal still follows the rules below.
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
locations are separate from preferences; R6's bounded deployment root is defined above.

General executable loading/isolation, public API/ABI, state evolution, update policy and realtime
contracts remain open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). R6 implements only local content/generator needs; later realtime
capabilities must meet [AUDIO_ENGINE](AUDIO_ENGINE.md), not inherit an asynchronous generator contract.
