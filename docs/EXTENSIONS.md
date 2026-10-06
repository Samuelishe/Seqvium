# Extensions

Role: Optional-capability and extension lifecycle contract.
Read when: Designing extension boundaries, packages, removal, or missing-capability behavior.
Authoritative for: Categories, base-versus-optional policy, lifecycle principles, missing-extension preservation.
Not authoritative for: A final public API, package format, project container, DSP implementation, or store.

## Base and optional capabilities

Most optional musical capabilities should eventually be installable/removable. The base responsibility
boundary is owned by [ARCHITECTURE](ARCHITECTURE.md#accepted-constraints): optional UI cannot own
fundamental document integrity or audio resources.

| Candidate category | Boundary direction |
| --- | --- |
| Sample/content packs | May contain data only, with no executable code |
| Presets | Data tied to a capability; compatibility needs identity/version handling |
| Creative sample generators | Bounded asynchronous work outside realtime audio |
| Instruments | Realtime sound production with a stricter processing/lifetime contract |
| Realtime effects | Realtime processing with a stricter processing/lifetime contract |
| Future external plugin bridges | Separate hosting/integration concern; CLAP/VST3 strategy remains open |

Categories need not share an identical runtime contract. Do not create a universal API to cover
unproven needs. Early extension infrastructure should support honest first-party optional modules:
identity, local discovery, lifecycle/error handling, and capability-specific boundaries. No store or
marketplace belongs in the initial foundation.

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

Saving must not silently discard unknown extension state. Missing code cannot reproduce its algorithm
merely because state is preserved; these are distinct concerns. Reinstallation/rebinding compatibility
needs evidence. [PROJECT_FORMAT](PROJECT_FORMAT.md) owns on-disk preservation and compatibility.

## Not selected yet

Identity namespace, version compatibility, manifest and package format, executable trust/isolation,
loading mechanism, API/ABI, update policy, and realtime contracts are open in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
SEQ-R4 should implement only what the first optional content/generator modules require; later realtime
capabilities must meet [AUDIO_ENGINE](AUDIO_ENGINE.md), not inherit an asynchronous generator contract.
