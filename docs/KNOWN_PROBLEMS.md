# Known problems and open questions

Role: Register of unresolved risks, questions, and validation gaps.
Read when: Planning experiments or checking whether an uncertain choice has evidence.
Authoritative for: Open uncertainty, impact, and intended resolution paths.
Not authoritative for: Accepted resolutions, existing implementation debt, progress, or stage order.

These are questions about future work, not bugs in a nonexistent application. Open tables contain
unresolved scope; partial/resolved outcomes are retained below with links for traceability.
Owners keep the detailed constraints; this register identifies what is not yet established.
Speculative possibilities without required resolution belong to [IDEAS](IDEAS.md); agreed staged
intent belongs to [ROADMAP](ROADMAP.md). No entry below is implementation debt.

## SEQ-R0 validation priorities

| ID | Question / risk | Evidence needed | Owner |
| --- | --- | --- | --- |
| Q-001 | Is a native realtime engine behind C# feasible and worth its complexity? | Bounded managed/native host, callback timing under managed/GC pressure, deployment/build implications; accept, reject, or narrow the candidate | [Architecture](ARCHITECTURE.md) |
| Q-002 | Which engine language/backend should be used? C++ and miniaudio are candidates | Official API/license/binary evaluation, Windows device behavior, limitations and viable alternatives; no library selected by assumption | [Audio](AUDIO_ENGINE.md), [provenance](THIRD_PARTY.md) |
| Q-003 | What narrow boundary safely owns buffers and prepared execution state? | Initialization, publication, reference/lifetime, failure, stop/restart and shutdown exercises; ABI and C# decomposition remain open; full graph preparation is Q-018 | [Architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md) |
| Q-004 | Commands, snapshots, or a combination; exact queue/overflow and recovery behavior? | D-044 requires bounded capacity/work and recovery toward current realtime progress; test coalescing/latest-wins where semantics permit, obsolete preparation generations, ordering/cancellation/stale publication; no universal musical-event drop rule | [Audio](AUDIO_ENGINE.md) |
| Q-005 | How do musical time, the audio clock, transport, loops, and ordered musical events align under overload? | Scheduled events around loop/block boundaries and tempo/time-to-sample rounding; ordering and long-run drift evidence with declared rate/buffers; eventual stop/release/panic recovery, including note-off ownership after skipped time, without naive disposal like UI values or requiring the probe to solve the full scheduler | [Audio](AUDIO_ENGINE.md) |
| Q-006 | How much scheduling/DSP can realtime and offline share? | Device-independent bounded render and event/output comparison; state determinism, tails, and numerical limits identified | [Audio](AUDIO_ENGINE.md) |
| Q-007 | What latency, callback, and stress targets are achievable? | Declared probe environment/thresholds, callback duration, overload/recovery evidence without growing obsolete audio backlog/latency under D-044, responsiveness under control pressure; device loss/recovery limitations stated | [Audio](AUDIO_ENGINE.md) |

These priorities do not require SEQ-R0 to solve the final plugin ecosystem or workstation schema.
Unresolved findings must retain their evidence limits rather than become accepted choices by silence.

## Later design and validation gaps

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-008 | Exact musical-part/event schema and time/ID representation | Bounded SEQ-R1 model supporting named multi-instrument/shared patterns, compatible entry methods, undo, and later editors | [Architecture](ARCHITECTURE.md) |
| Q-009 | Project container/schema, compatibility metadata, migrations, and unsupported-version handling | SEQ-R1 bounded versioned foundation under D-045; useful open/migration diagnostics and round-trip unknown data. Recovery is Q-058; managed-media transactions/workflows are Q-059, not a second storage question here | [Project format](PROJECT_FORMAT.md) |
| Q-010 | Extension identity/package discovery and install/update/remove lifecycle, including loading/trust | Minimal SEQ-R6 content/generator requirements, then SEQ-R13 management; distinguish package revision, saved instance state and global preferences. Resolve active-use removal/update, failed installation and reattachment without data loss; compatibility negotiation is Q-024 and optional stronger isolation Q-025 | [Extensions](EXTENSIONS.md) |
| Q-011 | Contextual Sample Lab target/substitution, realtime publication, downstream audition boundaries, and stop/cancel/restoration | SEQ-R7 bounded design/evidence for standalone and contextual modes, preserving existing processing and temporary reversible audition until explicit acceptance; concurrent target deletion/undo and stale generation completion need cases. Exploration locks/similarity/history are Q-071 | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-012 | Concrete object-render/audible-selection capture taps, downstream mixer/send/Master inclusion, output rate/channels, normalization, and cancellation | Bounded SEQ-R8 and later examples under D-036/D-047; object-local and broader audible context stay distinct. Transport DSP mechanisms and manual export-range tails have separate entries below; do not silently bake unrelated downstream processing or assume whole-Master capture | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-013 | Does a project with a missing/incompatible required realtime plugin refuse to open, or open degraded with a missing/offline instance? | High-impact owner decision before realtime hosting/project-open integration. Neither model is selected. Define affected playback/render/edit/save behavior, concise diagnostics and compatible reattachment; preservation is required whichever policy is chosen and does not imply degraded opening | [Extensions](EXTENSIONS.md), [project format](PROJECT_FORMAT.md), [UX](UX_CONTRACT.md) |
| Q-014 | Future dependency/asset/codec/native compatibility and redistribution obligations; project-license selection resolved | Apache-2.0 selected by D-029; evaluate concrete additions and their distributed-product obligations before distribution | [Third party](THIRD_PARTY.md) |
| Q-015 | Final visual language and detailed editing interaction | Deliberate gradual UI design stages; product-led interaction and DPI/input validation when UI exists | [UI design](UI_DESIGN.md), [UX](UX_CONTRACT.md) |
| Q-016 | CLAP/VST3 hosting scope and interoperability contract | Evidence-led SEQ-R14+ scope; lifetime, state/editor exchange, realtime constraints and rate/channel support before hosting. Platform delivery scope is separately Q-068; no hosting support commitment now | [Extensions](EXTENSIONS.md), [audio](AUDIO_ENGINE.md) |

## Graph, workspace, and host capability questions

Accepted subsystem ownership does not resolve these implementation choices or expand SEQ-R0 into
a full workstation probe. Later stages should answer only the questions needed for their bounded scope.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-017 | Exact node port types, conversions, event/control rates, and channel negotiation | Define a bounded semantic type model before accepting graph connections; no untyped universal pipe or final ABI by assumption | [Node graph](NODE_GRAPH.md) |
| Q-018 | Graph validation/preparation, execution representation, and live-edit publication implementation | D-041 accepts last-valid execution; test bounded preparation, generation ordering, publication/state transfer, retired resource lifetime, project close and active plugin unload together. Invalid candidates never replace valid execution; persisted/rendered revision choice is Q-060, not solved by safe publication | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-019 | Exact identity/ownership of item/container graphs versus instrument/channel/bus/Master scopes | D-033 excludes arbitrary graph-per-event defaults; resolve two local levels without assumed nesting. A multi-instrument Pattern is musical content, not automatically one DSP container: define its bounded object-render composition before R8. Event conversion is Q-048, overlap Q-047 and container routing Q-030 | [Node graph](NODE_GRAPH.md), [architecture](ARCHITECTURE.md), [sample workflow](SAMPLE_WORKFLOW.md) |
| Q-020 | Cycles and feedback in graphs | Audit explicit feedback semantics, delay/state, or specialized nodes as candidates; validation/scheduling remain open and arbitrary zero-delay cycles are not accepted | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-021 | Latency reporting/propagation/compensation and exact Live / Low-Latency thresholds/bypass strategy | Before latency-bearing parallel paths/R8 render, identify the minimum needed reporting/alignment, including plugin latency changes and sidechains; full compensation/live UX remains later. Measure temporary handling/restoration under D-044, preserving project/full render; no thresholds or bypass algorithm selected | [Audio](AUDIO_ENGINE.md), [node graph](NODE_GRAPH.md) |
| Q-022 | Docking groups, per-pane preferences, and layout persistence/restoration | Bounded SEQ-R3 user-layout design with predictable escape and safe size/DPI adaptation; no final grouping/storage mechanism | [Workspace](WORKSPACE.md) |
| Q-023 | Instrument/channel organizational-group hierarchy | Use naming/collapse and cross-pattern examples to choose bounded hierarchy rules; group identity stays separate from patterns/routing | [Architecture](ARCHITECTURE.md) |
| Q-024 | Exact plugin capability/version/format/state negotiation, manifest/API, and version-range mechanics | D-039/D-046 require satisfied contracts before activation, not rejection by age alone; prefer lightweight declared checks, handle real activation/materialization failure, and preserve identity/state. No expensive every-plugin startup self-test requirement; known incompatibility never activates or automatically deletes installed code | [Extensions](EXTENSIONS.md) |
| Q-025 | Whether stronger plugin crash/security isolation is worth its complexity for future external hosting | Conditional evidence-led evaluation only if real hosting justifies it; weigh containment value against lifecycle/IPC cost (I-005); D-037 requires normal compatibility/lifecycle handling without hard isolation and distinguishes crash separation from a security sandbox | [Extensions](EXTENSIONS.md), [idea](IDEAS.md#i-005--out-of-process-external-plugin-crash-isolation) |
| Q-026 | Future ASIO and additional device-backend strategy | Preserve backend-independent engine/plugin contracts; evaluate concrete SDK/library terms and distribution later, not as an R0 requirement | [Audio](AUDIO_ENGINE.md), [third party](THIRD_PARTY.md) |
| Q-027 | Monitoring and audio/MIDI recording timing/latency | Define device/input clock alignment, capture placement, monitoring, and compensation for actual supported devices/workflows; D-047 distinguishes Record Stop from playback settling and forbids silently extending capture with playback tails | [Audio](AUDIO_ENGINE.md) |

## Creative workflow model questions

SEQ-KB-R2, SEQ-KB-R5, SEQ-KB-R6, and SEQ-KB-R7 accept human-facing direction, not final domain identities or interaction mechanisms.
These questions remain open and do not authorize implementation in this documentation stage.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-028 | Final Layer/Track/Channel/Lane/container term and domain identity; compatibility, preferred target, ownership, and nesting | Design a bounded semi-free model with processing contexts distinguishable from organizational groups; D-035 fixes move consequences, not names/classes; Pattern, Instrument Group, and Mixer Channel stay distinct; concrete audio time/edit mechanics are Q-051 | [Architecture](ARCHITECTURE.md) |
| Q-029 | Concrete reference/edit ownership and sharing indicators for Pattern content, sound definitions, placement/processing state, and audio resources; contextual acceptance scope | D-034 separates sharing identities and musical-content versus sound-definition independence; design non-destructive edits and explicit detachment/acceptance scope with shared-use examples, without a universal unlink-everything operation | [Architecture](ARCHITECTURE.md), [project format](PROJECT_FORMAT.md), [sample workflow](SAMPLE_WORKFLOW.md), [UX](UX_CONTRACT.md) |
| Q-030 | Arrangement-container processing relationship to mixer channels/buses and concrete signal consequences of moves | Trace local/container/global boundaries and useful defaults under D-035: processing-context moves affect the result, organization alone does not; preserve separate timeline, organization, and routing identities | [Architecture](ARCHITECTURE.md), [node graph](NODE_GRAPH.md) |
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

The accepted R5 contracts require later bounded evidence for these mechanisms. They do not select
instances/classes, expand SEQ-R0, or authorize implementation now.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-047 | How can shared instrument definitions preserve independent overlapping execution/local processing at bounded CPU/memory cost? | High-impact evidence before overlapping item/container execution and again before external hosting: compare bounded strategies, third-party instance duplication, shared parameter/state updates, tail lifetime and resource limits. Whole mixed plugin output cannot be separated afterward; no selected instance count or free duplication | [Audio](AUDIO_ENGINE.md), [architecture](ARCHITECTURE.md), [extensions](EXTENSIONS.md) |
| Q-048 | Exact conversion of an atomic event into an independently processed standalone fragment/item | Design a low-ceremony reversible edit preserving intended music and relationships; do not select command names or domain classes from the conceptual action | [Architecture](ARCHITECTURE.md), [UX](UX_CONTRACT.md) |
| Q-049 | Exact edit-state transition when replacing an object with rendered audio containing baked processing | Verify explicit undoable replacement avoids silently reapplying the exact baked chain; evaluate bypass/removal/history or another reversible transformation without selecting mechanics or deleting sources by default | [Sample workflow](SAMPLE_WORKFLOW.md), [UX](UX_CONTRACT.md) |

## Runtime and UI platform mechanisms

SEQ-KB-R6 accepts constraints/direction under D-039 through D-044, not their implementation. Existing
Q-004/Q-005/Q-007/Q-018/Q-021/Q-024 retain scheduling, publication, latency, and plugin negotiation
questions; R7 separates tails/transport into Q-056/Q-057 below. The following gaps cover distinct mechanisms.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-050 | Exact historical sonic-compatibility policy for a future genuinely breaking platform/audio change | Evaluate deliberately when a concrete change occurs under D-039; preserve/migrate data without promising indefinite bit-identical historical engine emulation or a permanent old-engine mode | [Audio](AUDIO_ENGINE.md), [project format](PROJECT_FORMAT.md) |
| Q-051 | Concrete audio time/stretch representation/algorithms, labels/defaults, and trim/split/loop/stretch interaction | D-040 fixes tempo-following/fixed time and independent local stretch. R10's bounded audible stretch still needs a working supported mapping/algorithm; distinguish that prerequisite from richer R14+ processing. Resolve musical/source positions and sample rounding before gestures, without accidental speed changes or destructive split | [Architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md), [UX](UX_CONTRACT.md), [project format](PROJECT_FORMAT.md) |
| Q-052 | Exact production-log file size, rotation/count, retention/lifetime, storage, and diagnostics controls | Implement quiet bounded production diagnostics under D-042; ordinary GUI exposes at most enable/disable where useful, developer Debug/Trace activation remains outside it; no numeric limits selected | [Settings](SETTINGS.md), [development](DEVELOPMENT.md) |
| Q-053 | Concrete user settings paths/formats and bounded application/plugin preference-reset mechanics | Use platform-appropriate user configuration under D-042; keep project-affecting plugin instance state with the project and never delete projects/managed media on reset; executable installation/discovery remains separately open in Q-010 | [Settings](SETTINGS.md), [project format](PROJECT_FORMAT.md), [UX](UX_CONTRACT.md) |
| Q-054 | Localization resource format, contribution workflow, and fallback rules | Define host resources/contracts for initial Russian/English and later languages under D-043; stable internal identity must not depend on translated display text; first-party reusable UI consumes host localization | [Architecture](ARCHITECTURE.md) |
| Q-055 | Theme resource/API details and packaging as extensions/plugins/data packages | Develop coherent Dark/Light semantic colors, typography, sizing/style and justified icon/resource roles under D-043; keep packaging open and independently rendered external native editors exempt from theme adoption | [UI design](UI_DESIGN.md), [architecture](ARCHITECTURE.md), [extensions](EXTENSIONS.md) |

## SEQ-KB-R7 global audit findings

The full canonical knowledge base was audited **after** integrating D-045 through D-049. These are
unresolved findings and recommended discussion/evidence paths, not newly accepted features, stage
reordering, or permission to implement. High impact denotes potential data loss, expensive redesign,
or an unexercisable workflow; significance is not a claim that implementation already fails.

### Critical / high-impact

Existing **Q-013** is the missing-plugin project-open decision, not just a playback fallback question.
**Q-047** is a feasibility/resource risk for independent overlapping instrument execution; neither
shared definitions nor an external plugin's mixed output prove separability. **Q-019/Q-030** require
item/Pattern/container/Mixer boundaries before scope-specific preparation, sampling, and Arrangement
can be designed safely. Preserve these distinct identities; do not choose a universal graph to remove
the uncertainty. **Q-003/Q-004/Q-005/Q-018** retain native lifetime, sample-accurate scheduling,
overload recovery and publication evidence needs; R0 cannot validate all later production mechanisms.

| ID | Concrete problem / type and impact | Next discussion / evidence, not an accepted answer | Owners / affected stages |
| --- | --- | --- | --- |
| Q-058 | Autosave/crash recovery has no accepted workflow; a generic R12 recovery line does not define recoverable music or protect recorded media. Missing workflow / data-loss risk | What is recovery state versus explicit Save; how are corruption-safe behavior, bounded retention, recorded media and crash-restart user choice handled? Define required protection before real projects, without selecting autosave cadence, journal or storage scheme | [Project format](PROJECT_FORMAT.md), [UX](UX_CONTRACT.md); R1/R2 foundations, R12 integrity, later recording |
| Q-059 | Self-contained used media has no complete unsaved-project, Save As/collect/relocate or failure transaction. Missing workflow / integrity risk | When does imported/accepted/captured audio become durable, and what happens on disk-full, interrupted copy/save, moved projects or missing external media? Discuss integrity checks, retention with undo/recovery, relinking and explicit cleanup separately from Q-009 encoding; do not choose storage or automatic deletion | [Project format](PROJECT_FORMAT.md), [sample workflow](SAMPLE_WORKFLOW.md); R1/R2/R7/R8/R12, recording |
| Q-060 | Invalid/unpublished editable graph can differ from audible last-valid execution; save/reopen/export revision selection is unspecified. Missing decision / reproducibility risk | What may Save and render do while graphs diverge, and what must reopening restore or diagnose? Define intended revision/validation/user choice without persisting live execution objects, silently discarding edits, or selecting an automatic fallback | [Node graph](NODE_GRAPH.md), [project format](PROJECT_FORMAT.md), [audio](AUDIO_ENGINE.md), [UX](UX_CONTRACT.md); R4/R8/R12 |
| Q-061 | First Track Release is a goal without explicit minimum workflow/dependency closure. Roadmap dependency / delivery risk | Which small sampler/sample-based track must R12 finish end-to-end, and which prior stage supplies each prerequisite? Review R7/R8 bounded local processing before R11 Mixer, R10 basic stretch versus R14+ richness (Q-051), render latency (Q-021), recovery/media (Q-058/Q-059) and host UI services (Q-067). Recording, automation and CLAP/VST3 are later and must not be silently assumed prerequisites; owner decides any scope/order change | [Roadmap](ROADMAP.md), [architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md), [sample workflow](SAMPLE_WORKFLOW.md); R2–R12/R14+ |

### Significant

**Q-021** remains more than future low-latency UX: latency-bearing branches can already misalign
mixing or rendered material before advanced compensation is scheduled. **Q-010/Q-016/Q-024** separate
package management, runtime hosting and compatibility; a generator-only R6 contract is not proof of
realtime plugin lifetime/state/editor correctness. **Q-029/Q-048/Q-049** retain explicit shared-edit,
event-conversion and baked-replacement choices; these must not be solved by unlinking everything.

| ID | Concrete problem / type and impact | Next discussion / evidence, not an accepted answer | Owners / affected stages |
| --- | --- | --- | --- |
| Q-056 | Manual export range can intersect a valid tail. Missing product decision | Does the selected range default to a hard render boundary, or should an explicit include-tails-like option extend it? Preserve project cuts/tails under D-047 whichever range rule is later chosen | [Audio](AUDIO_ENGINE.md), [UX](UX_CONTRACT.md); R12 export, related R8 range capture |
| Q-057 | Accepted loop/seek/Stop intentions do not establish stateful DSP transitions or finite tail completion. Technical risk | How are destination warm-up, hard-cut reset, loop tail ownership and fast bounded playback settling implemented without duplicated state or stale sound? Compare realtime/offline entry-state cases, long/nondecaying tails and cancellation. Feedback topology is Q-020; Record Stop timing is Q-027; no algorithm or fade duration chosen | [Audio](AUDIO_ENGINE.md), [node graph](NODE_GRAPH.md); R4/R5/R8/R10/R12, later processors |
| Q-062 | Device loss/change and startup/project-open audio failure have no user recovery flow. Missing workflow / lifetime risk | What happens to transport, active voices, capture and the still-editable project when a device disappears or cannot initialize; how does a user retry/select a device? Distinguish failure of audio execution from file/plugin open policy Q-013; validate teardown/republication/rate changes without blocking callbacks | [Audio](AUDIO_ENGINE.md), [UX](UX_CONTRACT.md), [settings](SETTINGS.md); R2 onward, later recording |
| Q-063 | Undo exists as a foundation but transaction boundaries across shared edits, asynchronous acceptance and plugin edits are undefined. Missing decision / integrity risk | Which gestures/actions form one document edit, when does asynchronous work commit, and how do undo/deletion/project close invalidate pending results? Include variation, event conversion, rendered replacement and retained media; workspace preference undo is a separate scope choice | [Architecture](ARCHITECTURE.md), [UX](UX_CONTRACT.md), [sample workflow](SAMPLE_WORKFLOW.md); R1/R4/R5/R7/R8/R10, hosting |
| Q-064 | Mouse-first custom chrome, overlapping panes and graph feedback have no accessibility/input acceptance scope. Missing workflow / platform risk | What keyboard/focus paths, accessible window controls, non-color status cues and discoverable actions are required? Validate resize/move/maximize/close, focus return from external editors, pane reachability, DPI and usable minimums on actual platforms before shell acceptance; no concrete gestures or dimensions selected | [Workspace](WORKSPACE.md), [UX](UX_CONTRACT.md), [UI design](UI_DESIGN.md), [portability](PORTABILITY.md); R3 and later UI |
| Q-065 | Discovery/reuse is central but Browser and personal sample/preset library have no bounded workflow or stage owner commitment. Missing workflow | What minimum browse/audition/import and accepted-sample reuse path is needed for R7/R12? Decide whether personal-library publication/preset saving is required later, with project-copy versus library identity and reset/removal safety. Do not assume a catalog/index/store feature | [Sample workflow](SAMPLE_WORKFLOW.md), [UX](UX_CONTRACT.md), [project format](PROJECT_FORMAT.md), [roadmap](ROADMAP.md); R2/R7/R12 review |
| Q-066 | Cross-context sidechain/control routes may cross the two local processing scopes; exposure mechanics alone do not settle their lifetime or scheduling. Missing boundary / technical risk | Which cross-scope connections are permitted when needed, and how do moving/deleting a target, shared placements, rate/latency and offline capture affect them? Use concrete signal/control cases; preserve bounded local UI reasoning without silently accepting arbitrary feedback or a third local layer | [Node graph](NODE_GRAPH.md), [architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md); R4 foundations, R10/R11/deeper routing and R14+ control |
| Q-067 | Localization/theme contracts are accepted but minimum host service introduction and contribution lifetime/versioning are not assigned. Roadmap dependency / API risk | Which bounded RU/EN and semantic resource services must exist for R3 shell and R6/R7 first-party surfaces? Resolve resource identity/fallback, contributions and unload with Q-054/Q-055 before exposing contracts; theme packaging remains open and independent external editor visuals exempt | [Architecture](ARCHITECTURE.md), [UI design](UI_DESIGN.md), [extensions](EXTENSIONS.md), [roadmap](ROADMAP.md); R3/R6/R7 |
| Q-068 | Linux/macOS architectural targets and hosted checks do not define platform release scope. Missing planning decision | Which desktop/device acceptance is required for each intended release, distinct from RIDs/prerequisites Q-041? Keep R14+ additional-platform delivery distinct from early portable architecture/CI; no delivery dates or parity selected | [Portability](PORTABILITY.md), [roadmap](ROADMAP.md), [test execution](TEST_EXECUTION.md); R0 distribution evidence through R12/R14+ release review |
| Q-069 | Project-owned audio intent and negotiated device rate/channels/buffers can differ. Missing boundary / reproducibility risk | Which values are durable project intentions versus runtime device facts, and what supported adaptation/diagnostic is needed when reopening on another device? Honor D-045 without silently rewriting the project or pretending hardware is portable; project/offline sound settings must not live-link to user defaults | [Settings](SETTINGS.md), [audio](AUDIO_ENGINE.md), [project format](PROJECT_FORMAT.md); R1/R2/R12 |
| Q-071 | Candidate similarity/locks/history are bundled with contextual substitution but have distinct limits and user choices. Missing interaction decision | What constitutes a nearby variant or lock, and what bounded candidate history/comparison is useful for the one/two R7 families? Separate disposable exploration retention from explicit accepted audio; choose with concrete examples, not a universal similarity metric | [Sample workflow](SAMPLE_WORKFLOW.md), [UI design](UI_DESIGN.md); R7 |

### Minor / cleanup

| ID | Concrete problem / type | Next discussion / action when authorized | Owner |
| --- | --- | --- | --- |
| Q-070 | `.editorconfig` applies LF to every file while `.gitattributes` checks out `.bat`/`.cmd` as CRLF. Objective passive-policy contradiction; no batch scripts currently exist | Align the editor exceptions with the accepted Git batch-script policy in a later authorized config change. R7 reports the discrepancy without changing configuration or introducing tooling | [Development](DEVELOPMENT.md), [.editorconfig](../.editorconfig), [.gitattributes](../.gitattributes) |

ProjectStats sequencing **Q-046** is still an owner choice, not a prerequisite for R0 or a reason to
delay music milestones. Its main-suite-first test placement is consistent with Q-043 and D-032;
neither framework nor test platform has been selected. R14+ bundles recording, automation, hosting,
time processing and platform delivery; each needs later bounded scope rather than treating the bucket
as a dependency-resolving implementation stage. No automatic reordering is justified by this audit.

Material duplication was reviewed by subject: extensions own compatibility, settings own preference
classification, audio owns transport/render, node graph owns publication semantics, portability owns
platform claims, architecture/UI design own localization/themes, and project format owns media
durability. Cross-owner summaries with links are useful boundary context, not competing specifications.
Architecture's repeated media-default inventory was reduced to the format owner; transport mechanisms
were removed from the sampling-tap question. No giant merged owner or extra audit-document authority
was introduced. Terminology remains provisional where explicitly marked, rather than a contradiction.

## Retained resolution trace

**Q-014 — Project-license selection resolved; third-party scope remains open.** The original question
was "Final FOSS license and dependency/codec/native redistribution obligations". SEQ-KB-R4 selected
Apache License 2.0 for Seqvium-authored work through
[D-029](DECISIONS_LOG.md#d-029--seqvium-uses-apache-license-20) and root [LICENSE](../LICENSE).
The project license is no longer an open choice. The narrowed Q-014 table entry retains only future
dependency/content compatibility and redistribution evaluation under [THIRD_PARTY](THIRD_PARTY.md).

**SEQ-KB-R5 partial resolutions.** D-033 through D-038 in [DECISIONS_LOG](DECISIONS_LOG.md) narrow
Q-009, Q-012, Q-019, Q-025, and Q-028 through Q-030. The media default is accepted, sampling/capture
operations are distinct, graph-per-event defaults are excluded, sharing identities are separated,
processing-context moves have consequences, and hard plugin isolation is optional future evaluation.
The table entries retain their unresolved mechanics; Q-047 through Q-049 capture specific execution
and edit gaps rather than reopening those accepted principles.

**SEQ-KB-R6 partial resolutions.** D-039 through D-044 in [DECISIONS_LOG](DECISIONS_LOG.md) narrow
Q-004/Q-005/Q-007/Q-012/Q-018/Q-021/Q-024/Q-028. Platform authority/retention, distinct edits/tails,
last-valid execution with visible editor differences, bounded overload, and future low-latency mode
are accepted direction. Their refined rows and Q-050 through Q-055 retain mechanisms/policies needing
later deliberate design/evidence; no scheduler, ABI, storage layout, algorithm, or packaging was selected.

**SEQ-KB-R7 integration and question refinement.** D-045 through D-049 refine accepted settings,
metadata, compatibility, transport/render, UI and public research policy. Q-009's original broad
format/recovery/media scope is retained as separate Q-009/Q-058/Q-059; Q-012's original render/tail/
transport scope is narrowed to taps/output with Q-056/Q-057 and recording alignment in Q-027.
Q-011's exploration semantics moved to Q-071; Q-016's platform delivery scope moved to Q-068.
Q-013 is clarified as a still-unresolved whole-project opening choice; preservation never selected
one opening model. Q-005/Q-010/Q-018/Q-019/Q-021/Q-047/Q-051 were sharpened with concrete boundary
cases. Q-056 through Q-071 record remaining choices/risks, not accepted answers or new roadmap stages.
No fully unresolved entry was declared resolved merely because a principle was accepted.

When evidence resolves an entry, record the result and link its report/decision; do not erase the
reasoning. A compromise actually introduced into implementation belongs in [TECH_DEBT](TECH_DEBT.md).
