# Roadmap

Role: Ordered development direction.
Read when: Scoping the next stage or evaluating a future capability.
Authoritative for: Stage goals, sequence, and scope boundaries.
Not authoritative for: Completion status, factual history, accepted technical decisions, or existing debt.

This is a direction, not a promise to implement all capabilities in v0.1. Later ordering may change
with evidence and product need. Current status belongs to [PROJECT_STATE](PROJECT_STATE.md), completed
facts to [WORK_LOG](WORK_LOG.md), and technical choices to their owners and [DECISIONS_LOG](DECISIONS_LOG.md).

## SEQ-KB-R0 — Repository Knowledge Foundation

Define product direction, canonical owners, task routing, and documentation governance. Documentation
only: no production application, dependencies, engine, tooling framework, or tests for nonexistent code.
Retained historical stage identity; completion is recorded in [WORK_LOG](WORK_LOG.md#2026-10-06--seq-kb-r0).

## SEQ-KB-R1 — Product Architecture Expansion

Incorporate accepted mouse-first/universal product direction, named patterns and independent groups,
core graph, internal workspace panes, and core/plugin/backend boundaries into canonical owners.
Documentation only; this stage does not start SEQ-R0 or introduce executable implementation.

## SEQ-KB-R2 — Creative Workflow Model

Incorporate contextual Sample Lab, what-you-hear sampling, non-destructive resource/placement
identity, two local processing levels, progressive graph visibility, free canvas/topology semantics,
semi-free Arrangement, and an evolving UI design owner. Completed documentation stage; see
[WORK_LOG](WORK_LOG.md#2026-10-06--seq-kb-r2). No implementation stage is started or renumbered.

## SEQ-KB-R3 — Development & Portability Policy

Establish canonical coding, development, verification, portability, CI, and ideas owners with minimal
passive `.editorconfig` / `.gitattributes`. This is a documentation/configuration stage before source:
no application/experiment projects, packages, executable tooling, tests, builds, or workflows.
No implementation stage is started or renumbered; completion belongs to PROJECT_STATE / WORK_LOG.

## SEQ-KB-R4 — Repository Foundation Finalization

Finalize Apache-2.0 licensing, README/public identity, test architecture policy, compact current-state /
evidence policy, and the future [ProjectStats contract](PROJECT_STATS.md). Documentation/legal/passive
repository work only; no executable application, experiment, tests, tooling, or CI is started.
Completion belongs to PROJECT_STATE / WORK_LOG; implementation stages are not renumbered.

ProjectStats is an early repository-tooling foundation for later explicit code authorization, separate
from the audio probe. It may precede the probe; whether a dedicated pre-R0 tooling stage is useful
remains Q-046 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md), not an inserted implementation commitment.

## SEQ-KB-R5 — Musical Ownership & Extension Boundary Clarification

Record accepted processing granularity, separately shared musical/sound/placement/processing identities,
independent overlapping execution, processing-context move consequences, object-render versus audible
capture semantics, realistic third-party plugin boundaries, and self-contained used project media.
Documentation only: no source, projects, dependencies, prototypes, executable tooling, tests, CI, or
passive configuration changes. Completion belongs to PROJECT_STATE / WORK_LOG. SEQ-R0 remains pending /
not started; implementation stages keep their existing IDs and external hosting stays later work.

## SEQ-KB-R6 — Runtime, Editing & UI Platform Semantics

Record accepted platform-authoritative compatibility and sound-responsibility boundaries; project
tempo/independent local stretch and non-destructive trim/split/loop semantics; source tails versus
hard cuts; last-valid graph execution with visible concise feedback; quiet bounded diagnostics and
user/project preference separation; host localization/theme resources; bounded realtime overload;
and future Live / Low-Latency direction. Keep exact mechanisms and packaging open in their owners.
Documentation only: no source, projects, tests, dependencies, scripts, CI, executable tooling, plugin
hosts, theme packages, localization files, or passive configuration changes. Completion belongs to
PROJECT_STATE / WORK_LOG. SEQ-R0 remains **pending / not started**; implementation stages retain IDs.

## SEQ-R0 — Audio Architecture Probe

SEQ-R0 remains pending / not started after the documentation stages.

Before future probe work, follow [CODING_GUIDELINES](CODING_GUIDELINES.md), [DEVELOPMENT](DEVELOPMENT.md),
[PORTABILITY](PORTABILITY.md), and [TEST_EXECUTION](TEST_EXECUTION.md). Policy preparation does not
authorize starting the probe.

Validate the riskiest architecture before application construction. Scope a minimal experimental host,
device initialization, realtime callback, audio clock, basic transport, scheduled sample/tone events,
loop boundaries, bounded command/control path, instrumentation, and stress behavior. Compare a
device-independent offline equivalent where useful.
Overload evidence must respect [AUDIO_ENGINE](AUDIO_ENGINE.md#bounded-overload-and-semantic-recovery):
missed deadlines cannot create unbounded obsolete audio backlog, and disposable updates and musical
events need different handling. Exact queue/scheduler/recovery mechanisms remain probe questions.

Evaluate managed/native feasibility, ownership/lifetime, callback behavior under managed pressure,
and native binary distribution implications. Record setup, measurements, limitations, and a reasoned
accept/reject/narrow recommendation. A backend/language/ABI becomes accepted only through an explicit
evidence-backed decision. See [AUDIO_ENGINE](AUDIO_ENGINE.md) and the [experiment guide](experiments/README.md).
Keep engine processing independent of the selected device adapter and compatible with a prepared
execution boundary. No graph editor, full plugin host, ASIO, or recording workspace is required here.

## SEQ-R1 — Domain and Musical Model Foundation

Establish project/document ownership, musical-time primitives, instrument identity, events/parts,
named multi-instrument patterns, clips, independent organizational groups, justified stable IDs,
undo/redo, and a bounded versioned serialization foundation.
Respect shared audio resources versus musical placements and the two local processing levels without
freezing the final container term/schema or merging Arrangement with mixer identity.
Avoid UI-heavy implementation. Preserve the intended model in [ARCHITECTURE](ARCHITECTURE.md) and
compatibility direction in [PROJECT_FORMAT](PROJECT_FORMAT.md).

## SEQ-R2 — Audio Resource / Device Foundation

Establish audio resources, WAV import, preview, managed project media, a simple sampler, note/pitch
playback, and resource lifetime. Do not expand immediately to every codec or sampler feature.
Establish a bounded backend-independent audio input/output/device foundation informed by R0. Plan
MIDI input and capture ownership without requiring polished recording UX or all device backends now.

## SEQ-R3 — Workspace Shell Foundation

Establish one main window and internal workspace-pane infrastructure: activation/front behavior,
movement/resizing, internal floating, bounded collapse/restore, user-controlled docking where justified,
and safe application/user layout persistence. Start with limited surfaces; no full visual system or
aggressive IDE docking framework. Follow [WORKSPACE](WORKSPACE.md) and [UX_CONTRACT](UX_CONTRACT.md).

## SEQ-R4 — Node Graph Foundation

Establish core node/connection concepts, semantic port validation, a bounded editable/prepared
execution boundary, and minimal graph interaction/structural nodes over the shell. Basic source/output,
gain and mix/routing may arrive here because execution needs them. Do not implement every candidate
processor or all graph scopes; [NODE_GRAPH](NODE_GRAPH.md) owns the constraints.
Respect item-local/containing-container processing direction, progressive graph visibility, free
spatial placement, and topology-defined dependencies without presuming a final "Layer" model.
Keep the last valid prepared graph executing during candidate validation; make invalid/unpublished
editor state visible. Publication/compiler strategy remains open; the UX rule does not select it.

## SEQ-R5 — Pattern Workspace

Create the first genuinely musical editing workflow: Channel Rack, Step Sequencer, pattern looping,
several channels, velocity, and responsive live editing over extensible musical events.

## SEQ-R6 — Extension Foundation

Implement enough identity, manifest/package concepts, local discovery, content-pack boundaries,
sample-generator capability contract, and lifecycle/error/missing-state handling to make first-party
optional modules honest extensions. No store or marketplace. [EXTENSIONS](EXTENSIONS.md) owns the boundary.
Include deliberate compatibility outcomes, diagnostics, and state preservation at the bounded level
needed by actual modules. The core graph/workspace/device infrastructure stays host-owned.
Resolve compatibility before activation under current platform contracts; installed incompatible
plugins may remain unavailable, with preserved identity/state, and must not be automatically deleted.

## SEQ-R7 — Sample Lab / Generator V1

Ship one default-installed but removable generator package with one or two bounded families. Provide
random and nearby/similar variants, justified parameter locks, candidate history, and a dedicated
workspace pane with standalone and contextual entry. Provide temporary contextual audition through
relevant existing downstream processing alongside solo audition, then explicit acceptance as durable
audio. Resolve bounded substitution/publication/restoration behavior. Follow [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md).

## SEQ-R8 — Resampling

Render a pattern or another bounded selected object to a reusable sample through its own semantic/local
processing boundary. Keep object sampling distinct from broader audible-selection capture under
[SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md#resampling); resolve the implemented scope's concrete boundary
and tails with evidence. Preserve the source and enable immediate reuse. Any optional replace-with-sample
operation must be undoable and avoid silently reapplying exact baked processing. Full audible-context,
arrangement, and multiple-source capture need not arrive together.

## SEQ-R9 — Piano Roll

Add richer pitch, duration, and velocity editing over the same underlying musical event model as
the Step Sequencer.

## SEQ-R10 — Arrangement

Add Playlist/Arrangement with clips in musical time, normal shared pattern references, and explicit
independent variations. Follow the semi-free direction: user-named structured containers, useful
default content relationships, and compatible material reuse without permanent one-instrument
ownership or mixer-channel identity. Resolve bounded compatibility/container-processing relationships
and audio-clip scope rather than assuming a final Track schema or full DAW timeline.
Respect project-tempo relationships and independent local stretch; ordinary audio trim/split/rearrange
preserves source resources and keeps trim, loop/repeat, and stretch distinct. Exact representation,
algorithms, gestures, and the implemented scope need bounded design rather than a complete stretch engine here.

## SEQ-R11 — Mixer / Core Processing

Add the user-facing Mixer workflow with channels, master, gain, pan, mute, solo, and bounded common
processing such as basic EQ/compression. Foundational engine mixing/routing and structural graph
nodes may already exist. Mixer channels remain distinct from arrangement tracks; deeper sends/routing
should follow demonstrated need. A usable base must not require optional processing downloads.

## SEQ-R12 — First Track Release

Reach a version in which a user can reasonably finish a small track: strengthened save/load,
arrangement, basic mixing/effects, WAV export, packaging, and recovery/error handling suitable for
real projects. This is a usability/integrity goal, not a feature-count target or assigned version number.

## SEQ-R13 — Extension Ecosystem

Add mature install/update/remove management and extension diagnostics after real modules establish
the lifecycle and compatibility requirements.

## SEQ-R14+ — Evidence-led expansion

Expand MIDI and audio recording/monitoring into complete user workflows, then consider automation,
richer synthesizers/effects, CLAP/VST3 hosting, additional specialized nodes, pitch/time
processing and time stretching, deeper routing/sends, FLAC and other justified formats, and additional
platforms. Latency awareness and realtime safety constrain applicable earlier work; advanced
compensation is not presumed implemented. Later order and release schedules remain open.
Future Live / Low-Latency mode for performance/monitoring must visibly distinguish temporary live
handling from the full graph without rewriting project state or altering intended final/offline render.
Latency reporting, thresholds, compensation, and bypass strategy remain open under [AUDIO_ENGINE](AUDIO_ENGINE.md).
External hosting must define ordinary compatibility/lifecycle semantics under [EXTENSIONS](EXTENSIONS.md).
Stronger process isolation may be evaluated later if evidence justifies it; it is not required baseline
hosting architecture or an early milestone added by SEQ-KB-R5.
Automation/modulation work must resolve the open effective-parameter model: base value and control
sources, composition, domains/units, precedence, smoothing, and rates. Earlier parameter modelling
must leave room for that control without selecting its formula now.
ASIO is desired future device capability, subject to concrete implementation and licensing/distribution
evaluation. Core recording/device responsibilities may be established before polished recording UX;
this placement does not turn them into removable plugins or impose an electronic-only product boundary.
