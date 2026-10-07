# Known problems and open questions

Role: Register of unresolved risks, questions, and validation gaps.
Read when: Planning experiments or checking whether an uncertain choice has evidence.
Authoritative for: Open uncertainty, impact, and intended resolution paths.
Not authoritative for: Accepted resolutions, existing implementation debt, progress, or stage order.

These are questions about future work, not bugs in a nonexistent application. Open tables contain
unresolved scope only; accepted behavior lives in current owners, resolved history in the cold archive.
Owners keep the detailed constraints; this register identifies what is not yet established.
Speculative possibilities without required resolution belong to [IDEAS](IDEAS.md); agreed staged
intent belongs to [ROADMAP](ROADMAP.md). No entry below is implementation debt.

## SEQ-R0 validation priorities

| ID | Question / risk | Evidence needed | Owner |
| --- | --- | --- | --- |
| Q-001 | Is a native realtime engine behind C# feasible and worth its complexity? | Bounded managed/native host, callback timing under managed/GC pressure, deployment/build implications; accept, reject, or narrow the candidate | [Architecture](ARCHITECTURE.md) |
| Q-002 | Which engine language/backend should be used? C++ and miniaudio are candidates | Official API/license/binary evaluation, Windows device behavior, limitations and viable alternatives; no library selected by assumption | [Audio](AUDIO_ENGINE.md), [provenance](THIRD_PARTY.md) |
| Q-003 | What narrow boundary safely owns buffers and prepared execution state? | Initialization, publication, reference/lifetime, failure, stop/restart and shutdown exercises; ABI and C# decomposition remain open; full graph preparation is Q-018 | [Architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md) |
| Q-004 | Commands, snapshots, or a combination; exact queue/overflow and recovery behavior? | The audio contract requires bounded capacity/work and recovery toward current realtime progress; test coalescing/latest-wins where semantics permit, obsolete preparation generations, ordering/cancellation/stale publication; no universal musical-event drop rule | [Audio](AUDIO_ENGINE.md) |
| Q-005 | How do musical time, the audio clock, transport, loops, and ordered musical events align under overload? | Scheduled events around loop/block boundaries and tempo/time-to-sample rounding; ordering and long-run drift evidence with declared rate/buffers; eventual stop/release/panic recovery, including note-off ownership after skipped time, without naive disposal like UI values or requiring the probe to solve the full scheduler | [Audio](AUDIO_ENGINE.md) |
| Q-006 | How much scheduling/DSP can realtime and offline share? | Device-independent bounded render and event/output comparison; state determinism, tails, and numerical limits identified | [Audio](AUDIO_ENGINE.md) |
| Q-007 | What latency, callback, and stress targets are achievable? | Declared probe environment/thresholds, callback duration, overload/recovery evidence without growing obsolete audio backlog/latency under the audio contract, responsiveness under control pressure; device loss/recovery limitations stated | [Audio](AUDIO_ENGINE.md) |

These priorities do not require SEQ-R0 to solve the final plugin ecosystem or workstation schema.
Unresolved findings must retain their evidence limits rather than become accepted choices by silence.

## Later design and validation gaps

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-008 | Exact musical-part/event schema and time/ID representation | Bounded SEQ-R1 model supporting named multi-instrument/shared patterns, compatible entry methods, undo, and later editors | [Architecture](ARCHITECTURE.md) |
| Q-009 | Project container/schema, compatibility metadata, migrations, and unsupported-version handling | SEQ-R1 bounded versioned foundation under the project format/settings contracts; useful open/migration diagnostics and round-trip unknown data. Recovery is Q-058; managed-media transactions/workflows are Q-059, not a second storage question here | [Project format](PROJECT_FORMAT.md) |
| Q-010 | Exact package discovery/install/update/remove, persistence/location, trust, reattachment and failure lifecycle | Resolve bounded R6 modules then R13 management; distinguish packages, saved instances and preferences. Active-use package uninstall must already be blocked/deferred under the extension contract; exact update/install rollback and broader lifecycle remain open. Negotiation is Q-024 | [Extensions](EXTENSIONS.md) |
| Q-011 | Contextual Sample Lab target/substitution, realtime publication, downstream audition boundaries, and stop/cancel/restoration | SEQ-R7 bounded design/evidence for standalone and contextual modes, preserving existing processing and temporary reversible audition until explicit acceptance; target preconditions must be revalidated before any commit; exact deletion/undo invalidation and restoration need cases. Exploration locks/similarity/history are Q-071 | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-012 | Concrete object-render/audible-selection capture taps, downstream mixer/send/Master inclusion, output rate/channels, normalization, and cancellation | Bounded SEQ-R8 and later examples under the sample/audio contracts; object-local and broader audible context stay distinct. Transport DSP mechanisms and manual export-range tails have separate entries below; do not silently bake unrelated downstream processing or assume whole-Master capture | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-014 | Future dependency/asset/codec/native compatibility and redistribution obligations | Use the current Apache-2.0 licensing boundary; evaluate concrete additions and their distributed-product obligations before distribution | [Third party](THIRD_PARTY.md) |
| Q-015 | Final visual language and detailed editing interaction | Deliberate gradual UI design stages; product-led interaction and DPI/input validation when UI exists | [UI design](UI_DESIGN.md), [UX](UX_CONTRACT.md) |
| Q-016 | CLAP/VST3 hosting scope and interoperability contract | Evidence-led SEQ-R14+ scope; lifetime, state/editor exchange, realtime constraints and rate/channel support before hosting. Platform delivery scope is separately Q-068; no hosting support commitment now | [Extensions](EXTENSIONS.md), [audio](AUDIO_ENGINE.md) |

## Graph, workspace, and host capability questions

Accepted subsystem ownership does not resolve these implementation choices or expand SEQ-R0 into
a full workstation probe. Later stages should answer only the questions needed for their bounded scope.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-017 | Exact node port types, conversions, event/control rates, and channel negotiation | Define a bounded semantic type model before accepting graph connections; no untyped universal pipe or final ABI by assumption | [Node graph](NODE_GRAPH.md) |
| Q-018 | Exact graph validation/preparation, revision publication, state transfer and retirement | Test bounded/coalesced preparation, generation ordering, atomic publication, lifetime, project close, cancellation and stalled external work. Latest valid canonical revision converges automatically; invalid candidates never publish. Save/reopen/render rules are fixed in project format/audio; no compiler, queue or watchdog chosen | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-019 | Exact identity/ownership of item/container graphs versus instrument/channel/bus/Master scopes | The architecture contract excludes arbitrary graph-per-event defaults; resolve two local levels without assumed nesting. A multi-instrument Pattern is musical content, not automatically one DSP container: define its bounded object-render composition before R8. Event conversion is Q-048, overlap Q-047 and container routing Q-030 | [Node graph](NODE_GRAPH.md), [architecture](ARCHITECTURE.md), [sample workflow](SAMPLE_WORKFLOW.md) |
| Q-020 | Cycles and feedback in graphs | Audit explicit feedback semantics, delay/state, or specialized nodes as candidates; validation/scheduling remain open and arbitrary zero-delay cycles are not accepted | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-021 | Latency reporting/propagation/compensation and exact Live / Low-Latency thresholds/bypass strategy | Before latency-bearing parallel paths/R8 render, identify the minimum needed reporting/alignment, including plugin latency changes and sidechains; full compensation/live UX remains later. Measure temporary handling/restoration under the audio contract, preserving project/full render; no thresholds or bypass algorithm selected | [Audio](AUDIO_ENGINE.md), [node graph](NODE_GRAPH.md) |
| Q-022 | Docking groups, per-pane preferences, and layout persistence/restoration | Bounded SEQ-R3 user-layout design with predictable escape and safe size/DPI adaptation; no final grouping/storage mechanism | [Workspace](WORKSPACE.md) |
| Q-023 | Instrument/channel organizational-group hierarchy | Use naming/collapse and cross-pattern examples to choose bounded hierarchy rules; group identity stays separate from patterns/routing | [Architecture](ARCHITECTURE.md) |
| Q-024 | Exact manifest format, API/ABI, capability/version negotiation representation and state-compatibility schema | Represent required/optional capabilities, processing configurations and compatible state/schema ranges. Graded outcomes and bounded activation are established; age alone is not incompatibility. Prefer lightweight declared checks without mandatory expensive startup self-tests; known incompatibility never activates or auto-deletes code | [Extensions](EXTENSIONS.md) |
| Q-025 | Whether stronger plugin crash/security isolation is worth its complexity for future external hosting | Conditional evidence-led evaluation only if real hosting justifies it; weigh containment value against lifecycle/IPC cost (I-005); The extension contract requires normal compatibility/lifecycle handling without hard isolation and distinguishes crash separation from a security sandbox | [Extensions](EXTENSIONS.md), [idea](IDEAS.md#i-005--out-of-process-external-plugin-crash-isolation) |
| Q-026 | Future ASIO and additional device-backend strategy | Preserve backend-independent engine/plugin contracts; evaluate concrete SDK/library terms and distribution later, not as an R0 requirement | [Audio](AUDIO_ENGINE.md), [third party](THIRD_PARTY.md) |
| Q-027 | Monitoring and audio/MIDI recording timing/latency | Define device/input clock alignment, capture placement, monitoring, and compensation for actual supported devices/workflows; The audio contract distinguishes Record Stop from playback settling and forbids silently extending capture with playback tails | [Audio](AUDIO_ENGINE.md) |

## Creative workflow model questions

Accepted human-facing contracts do not select final domain identities or interaction mechanisms.
These questions remain open and do not authorize implementation in this documentation stage.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-028 | Final Layer/Track/Channel/Lane/container term and domain identity; compatibility, preferred target, ownership, and nesting | Design a bounded semi-free model with processing contexts distinguishable from organizational groups; the architecture contract fixes move consequences, not names/classes; Pattern, Instrument Group, and Mixer Channel stay distinct; concrete audio time/edit mechanics are Q-051 | [Architecture](ARCHITECTURE.md) |
| Q-029 | Concrete reference/edit ownership and sharing indicators for Pattern content, sound definitions, placement/processing state, and audio resources; contextual acceptance scope | The architecture contract separates sharing identities and musical-content versus sound-definition independence; design non-destructive edits and explicit detachment/acceptance scope with shared-use examples, without a universal unlink-everything operation | [Architecture](ARCHITECTURE.md), [project format](PROJECT_FORMAT.md), [sample workflow](SAMPLE_WORKFLOW.md), [UX](UX_CONTRACT.md) |
| Q-030 | Arrangement-container processing relationship to mixer channels/buses and concrete signal consequences of moves | Trace local/container/global boundaries and useful defaults under the architecture contract: processing-context moves affect the result, organization alone does not; preserve separate timeline, organization, and routing identities | [Architecture](ARCHITECTURE.md), [node graph](NODE_GRAPH.md) |
| Q-031 | Compact-chain representation and transitions to/from a full graph | Evaluate simple effect reordering and custom topologies; if adopted, both views use the same canonical graph and processing system | [Node graph](NODE_GRAPH.md), [UI design](UI_DESIGN.md) |
| Q-032 | Node settings: inspector, workspace pane, overlay, inline controls, or combinations | Evaluate deeper settings on selection and optional useful detachment without uncontrolled windows; no final placement chosen | [Node graph](NODE_GRAPH.md), [workspace](WORKSPACE.md) |
| Q-033 | Effective parameter composition for automation/modulation | Preserve future base value, automation, modulation, envelopes/LFO/control sources; investigate replace/add/multiply, domains/units, precedence, smoothing, and rates without accepting a formula | [Architecture](ARCHITECTURE.md), [node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-034 | Explicit parameter exposure and control-port mechanics | Evaluate opt-in connectors and settings defaults for graph readability; define types, connection behavior, and persistence later | [Node graph](NODE_GRAPH.md), [project format](PROJECT_FORMAT.md) |
| Q-035 | Multiple graph panes, target retention/following, and cross-project context behavior | Prefer focus/update of an existing target surface; evaluate actual workflows before single/multiple-instance policy or context lifetime choices | [Workspace](WORKSPACE.md) |

## Engineering choices when implementation needs them

These choices require resolution with actual projects/tooling or release scope, not immediate work
or permission to start SEQ-R0. The policy baseline does not select their answers.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-036 | Exact .NET SDK pin/roll-forward and validated language policy | Decide with the first managed project; add `global.json` if justified for reproducibility | [Development](DEVELOPMENT.md) |
| Q-037 | Final UI framework adoption | Evaluate proposed Avalonia against actual product/platform/API/license needs before application implementation | [Architecture](ARCHITECTURE.md), [development](DEVELOPMENT.md), [provenance](THIRD_PARTY.md) |
| Q-038 | Native compiler/build system and compiler warning policy, if R0 selects native | Choose with evidence across Windows/Linux/macOS; define native warning/suppression rules for the selected language/toolchain | [Development](DEVELOPMENT.md), [portability](PORTABILITY.md), [audio](AUDIO_ENGINE.md) |
| Q-039 | Initial test framework, test platform, and exact package versions | Choose with the first managed test project, using manifest-owned versions; then document real commands | [Test execution](TEST_EXECUTION.md), [development](DEVELOPMENT.md) |
| Q-040 | Concrete CI path filters and required-check policy | Resolve together once workflows/checker exist so docs-only changes do not leave required checks pending | [CI/CD](CI_CD.md) |
| Q-041 | Supported release architecture/RID matrix and runtime prerequisites | Decide for actual distribution with build/interop/packaging and declared runtime/device evidence; no dates/parity implied | [Portability](PORTABILITY.md) |
| Q-042 | Formatter/analyzer policy beyond current targeted diagnostics | Evaluate repeated source mistakes and real style once source exists; introduce repository-scoped tools only for concrete benefit | [Coding guidelines](CODING_GUIDELINES.md), [development](DEVELOPMENT.md) |
| Q-043 | Which actual native/UI/plugin-host execution or dependency constraints warrant additional test projects? | Begin with one main managed suite; split only on demonstrated TFM/platform/runtime/host/lifecycle differences, not subsystem folders | [Test execution](TEST_EXECUTION.md) |
| Q-044 | Exact ProjectStats ownership-path classification after source topology exists | Define production/test/tooling/experiment categories and corpus/path rules against real topology; verify with synthetic repositories | [ProjectStats](PROJECT_STATS.md) |
| Q-045 | Useful ProjectStats diagnostics and thresholds after real repository sizes exist | Evaluate structural review signals on real sizes; keep stable codes/advisory semantics without arbitrary gates | [ProjectStats](PROJECT_STATS.md) |
| Q-046 | Is a dedicated pre-R0 repository-tooling implementation stage useful after code authorization? | Owner explicitly chooses sequencing/scope; ProjectStats may precede the probe but is not silently inserted into R0 or the roadmap | [Development](DEVELOPMENT.md), [ProjectStats](PROJECT_STATS.md), [roadmap](ROADMAP.md) |

## Musical ownership execution and edit questions

Accepted ownership contracts require later bounded evidence for these mechanisms. They do not select
instances/classes, expand SEQ-R0, or authorize implementation now.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-047 | How can shared instrument definitions preserve independent overlapping execution/local processing at bounded CPU/memory cost? | High-impact evidence before overlapping item/container execution and again before external hosting: compare bounded strategies, third-party instance duplication, shared parameter/state updates, tail lifetime and resource limits. Whole mixed plugin output cannot be separated afterward; no selected instance count or free duplication | [Audio](AUDIO_ENGINE.md), [architecture](ARCHITECTURE.md), [extensions](EXTENSIONS.md) |
| Q-048 | Exact conversion of an atomic event into an independently processed standalone fragment/item | Design a low-ceremony reversible edit preserving intended music and relationships; do not select command names or domain classes from the conceptual action | [Architecture](ARCHITECTURE.md), [UX](UX_CONTRACT.md) |
| Q-049 | Exact edit-state transition when replacing an object with rendered audio containing baked processing | Verify explicit undoable replacement avoids silently reapplying the exact baked chain; evaluate bypass/removal/history or another reversible transformation without selecting mechanics or deleting sources by default | [Sample workflow](SAMPLE_WORKFLOW.md), [UX](UX_CONTRACT.md) |

## Runtime and UI platform mechanisms

Current audio, settings, localization and theme contracts leave these exact mechanisms open.
Scheduling/publication, latency and negotiation remain Q-004/Q-005/Q-007/Q-018/Q-021/Q-024;
tails/transport remain Q-056/Q-057.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-050 | Exact historical sonic-compatibility policy for a future genuinely breaking platform/audio change | Evaluate deliberately when a concrete change occurs under the audio compatibility contract; preserve/migrate data without promising indefinite bit-identical historical engine emulation or a permanent old-engine mode | [Audio](AUDIO_ENGINE.md), [project format](PROJECT_FORMAT.md) |
| Q-051 | Concrete audio time/stretch representation/algorithms, labels/defaults, and trim/split/loop/stretch interaction | The architecture/UX contracts fix tempo-following/fixed time and independent local stretch. R10's bounded audible stretch still needs a working supported mapping/algorithm; distinguish that prerequisite from richer R14+ processing. Resolve musical/source positions and sample rounding before gestures, without accidental speed changes or destructive split | [Architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md), [UX](UX_CONTRACT.md), [project format](PROJECT_FORMAT.md) |
| Q-052 | Exact production-log file size, rotation/count, retention/lifetime, storage, and diagnostics controls | Implement quiet bounded production diagnostics under the settings contract; ordinary GUI exposes at most enable/disable where useful, developer Debug/Trace activation remains outside it; no numeric limits selected | [Settings](SETTINGS.md), [development](DEVELOPMENT.md) |
| Q-053 | Concrete user settings paths/formats and bounded application/plugin preference-reset mechanics | Use platform-appropriate user configuration under the settings contract; keep project-affecting plugin instance state with the project and never delete projects/managed media on reset; executable installation/discovery remains separately open in Q-010 | [Settings](SETTINGS.md), [project format](PROJECT_FORMAT.md), [UX](UX_CONTRACT.md) |
| Q-054 | Localization resource format, contribution mechanism and fallback schema/details | Use host resources with stable identities, initial RU/EN and later languages. English is the baseline fallback for Seqvium-authored/native first-party contributions; missing localization uses a usable common fallback without disabling capability or changing host language. Exact representation remains open | [Architecture](ARCHITECTURE.md), [extensions](EXTENSIONS.md) |
| Q-055 | Theme resource/API details and packaging as extensions/plugins/data packages | Develop coherent Dark/Light semantic colors, typography, sizing/style and justified icon/resource roles under the localization/theme contracts; keep packaging open and independently rendered external native editors exempt from theme adoption | [UI design](UI_DESIGN.md), [architecture](ARCHITECTURE.md), [extensions](EXTENSIONS.md) |

## Transport and render mechanisms

| ID | Question / risk | Intended resolution path | Owner / affected stages |
| --- | --- | --- | --- |
| Q-056 | Manual export range can intersect a valid tail. Missing product decision | Does the selected range default to a hard render boundary, or should an explicit include-tails-like option extend it? Preserve project cuts/tails under the audio contract whichever range rule is later chosen | [Audio](AUDIO_ENGINE.md), [UX](UX_CONTRACT.md); R12 export, related R8 range capture |
| Q-057 | Accepted loop/seek/Stop intentions do not establish stateful DSP transitions or finite tail completion. Technical risk | How are destination warm-up, hard-cut reset, loop tail ownership and fast bounded playback settling implemented without duplicated state or stale sound? Compare realtime/offline entry-state cases, long/nondecaying tails and cancellation. Feedback topology is Q-020; Record Stop timing is Q-027; no algorithm or fade duration chosen | [Audio](AUDIO_ENGINE.md), [node graph](NODE_GRAPH.md); R4/R5/R8/R10/R12, later processors |

## Project integrity and milestone dependencies

| ID | Question / risk | Intended resolution path | Owner / affected stages |
| --- | --- | --- | --- |
| Q-058 | Autosave/crash recovery has no accepted workflow; a generic R12 recovery line does not define recoverable music or protect recorded media. Missing workflow / data-loss risk | What is recovery state versus explicit Save; how are corruption-safe behavior, bounded retention, recorded media and crash-restart user choice handled? Define required protection before real projects, without selecting autosave cadence, journal or storage scheme | [Project format](PROJECT_FORMAT.md), [UX](UX_CONTRACT.md); R1/R2 foundations, R12 integrity, later recording |
| Q-059 | Self-contained used media has no complete unsaved-project, Save As/collect/relocate or failure transaction. Missing workflow / integrity risk | When does imported/accepted/captured audio become durable, and what happens on disk-full, interrupted copy/save, moved projects or missing external media? Discuss integrity checks, retention with undo/recovery, relinking and explicit cleanup separately from Q-009 encoding; do not choose storage or automatic deletion | [Project format](PROJECT_FORMAT.md), [sample workflow](SAMPLE_WORKFLOW.md); R1/R2/R7/R8/R12, recording |
| Q-061 | First Track Release is a goal without explicit minimum workflow/dependency closure. Roadmap dependency / delivery risk | Which small sampler/sample-based track must R12 finish end-to-end, and which prior stage supplies each prerequisite? Review R7/R8 bounded local processing before R11 Mixer, R10 basic stretch versus R14+ richness (Q-051), render latency (Q-021), recovery/media (Q-058/Q-059) and host UI services (Q-067). Recording, automation and CLAP/VST3 are later and must not be silently assumed prerequisites; owner decides any scope/order change | [Roadmap](ROADMAP.md), [architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md), [sample workflow](SAMPLE_WORKFLOW.md); R2–R12/R14+ |

## Devices and document transactions

| ID | Question / risk | Intended resolution path | Owner / affected stages |
| --- | --- | --- | --- |
| Q-062 | Exact device loss/change, startup audio failure and recovery workflow | Define transport/voice/capture behavior, retry and logical input/output endpoint reselection, teardown/republication and recovery. Normal UX chooses devices, not backend libraries; audio unavailability does not by itself make a safely understood document unavailable. Recording/device-change lifetime requires known-dependency checks | [Audio](AUDIO_ENGINE.md), [UX](UX_CONTRACT.md), [settings](SETTINGS.md); R2 onward, later recording |
| Q-063 | Precise undo transactions, async commit grouping, document history and pending-work invalidation | Define gestures/actions and shared/plugin edit boundaries; validate project/target identity, context, relevance/cancellation and ownership/revision preconditions before async commit. Completion never authorizes mutation of a removed/changed target or current selection. Exact undo/deletion/close invalidation, candidate retention and grouping remain open | [Architecture](ARCHITECTURE.md), [UX](UX_CONTRACT.md), [sample workflow](SAMPLE_WORKFLOW.md); R1/R4/R5/R7/R8/R10, hosting |
| Q-069 | Project audio intent versus runtime device facts; clock domains and rate/channel/buffer/backend constraints | Define durable project intent, negotiated facts and supported adaptation on another device without silent project rewrites. Input/output selections are logically separate where supported; endpoint UX is accepted, exact backend/API/driver, clock synchronization and negotiation remain open | [Settings](SETTINGS.md), [audio](AUDIO_ENGINE.md), [project format](PROJECT_FORMAT.md); R1/R2/R12 |

## Input, library, routing and host services

| ID | Question / risk | Intended resolution path | Owner / affected stages |
| --- | --- | --- | --- |
| Q-064 | Mouse-first custom chrome, overlapping panes and graph feedback have no accessibility/input acceptance scope. Missing workflow / platform risk | What keyboard/focus paths, accessible window controls, non-color status cues and discoverable actions are required? Validate resize/move/maximize/close, focus return from external editors, pane reachability, DPI and usable minimums on actual platforms before shell acceptance; no concrete gestures or dimensions selected | [Workspace](WORKSPACE.md), [UX](UX_CONTRACT.md), [UI design](UI_DESIGN.md), [portability](PORTABILITY.md); R3 and later UI |
| Q-065 | Discovery/reuse is central but Browser and personal sample/preset library have no bounded workflow or stage owner commitment. Missing workflow | What minimum browse/audition/import and accepted-sample reuse path is needed for R7/R12? Decide whether personal-library publication/preset saving is required later, with project-copy versus library identity and reset/removal safety. Do not assume a catalog/index/store feature | [Sample workflow](SAMPLE_WORKFLOW.md), [UX](UX_CONTRACT.md), [project format](PROJECT_FORMAT.md), [roadmap](ROADMAP.md); R2/R7/R12 review |
| Q-066 | Cross-context sidechain/control routes may cross the two local processing scopes; exposure mechanics alone do not settle their lifetime or scheduling. Missing boundary / technical risk | Which cross-scope connections are permitted when needed, and how do moving/deleting a target, shared placements, rate/latency and offline capture affect them? Use concrete signal/control cases; preserve bounded local UI reasoning without silently accepting arbitrary feedback or a third local layer | [Node graph](NODE_GRAPH.md), [architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md); R4 foundations, R10/R11/deeper routing and R14+ control |
| Q-067 | Localization/theme contracts are accepted but minimum host service introduction and contribution lifetime/versioning are not assigned. Roadmap dependency / API risk | Which bounded RU/EN and semantic resource services must exist for R3 shell and R6/R7 first-party surfaces? Resolve resource identity/fallback schema, contributions and unload with Q-054/Q-055 before exposing contracts; theme packaging remains open and independent external editor visuals exempt | [Architecture](ARCHITECTURE.md), [UI design](UI_DESIGN.md), [extensions](EXTENSIONS.md), [roadmap](ROADMAP.md); R3/R6/R7 |
| Q-068 | Linux/macOS architectural targets and hosted checks do not define platform release scope. Missing planning decision | Which desktop/device acceptance is required for each intended release, distinct from RIDs/prerequisites Q-041? Keep R14+ additional-platform delivery distinct from early portable architecture/CI; no delivery dates or parity selected | [Portability](PORTABILITY.md), [roadmap](ROADMAP.md), [test execution](TEST_EXECUTION.md); R0 distribution evidence through R12/R14+ release review |
| Q-071 | Candidate similarity/locks/history are bundled with contextual substitution but have distinct limits and user choices. Missing interaction decision | What constitutes a nearby variant or lock, and what bounded candidate history/comparison is useful for the one/two R7 families? Separate disposable exploration retention from explicit accepted audio; choose with concrete examples, not a universal similarity metric | [Sample workflow](SAMPLE_WORKFLOW.md), [UI design](UI_DESIGN.md); R7 |

## Passive repository policy gap

| ID | Question / risk | Intended resolution path | Owner / affected stages |
| --- | --- | --- | --- |
| Q-070 | `.editorconfig` applies LF to every file while `.gitattributes` checks out `.bat`/`.cmd` as CRLF. Objective passive-policy contradiction; no batch scripts currently exist | Align the editor exceptions with the accepted Git batch-script policy in a later authorized config change. No configuration change or tooling is authorized by this documentation stage | [Development](DEVELOPMENT.md), [.editorconfig](../.editorconfig), [.gitattributes](../.gitattributes) |
