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
| Q-005 | How do musical time, the audio clock, transport, loops, and ordered musical events align under overload? | Scheduled events around loop/block boundaries; timing/ordering evidence with declared rate/buffers; eventual explicit stop/release/panic recovery, without naive disposal like UI values or requiring the probe to solve the full scheduler | [Audio](AUDIO_ENGINE.md) |
| Q-006 | How much scheduling/DSP can realtime and offline share? | Device-independent bounded render and event/output comparison; state determinism, tails, and numerical limits identified | [Audio](AUDIO_ENGINE.md) |
| Q-007 | What latency, callback, and stress targets are achievable? | Declared probe environment/thresholds, callback duration, overload/recovery evidence without growing obsolete audio backlog/latency under D-044, responsiveness under control pressure; device loss/recovery limitations stated | [Audio](AUDIO_ENGINE.md) |

These priorities do not require SEQ-R0 to solve the final plugin ecosystem or workstation schema.
Unresolved findings must retain their evidence limits rather than become accepted choices by silence.

## Later design and validation gaps

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-008 | Exact musical-part/event schema and time/ID representation | Bounded SEQ-R1 model supporting named multi-instrument/shared patterns, compatible entry methods, undo, and later editors | [Architecture](ARCHITECTURE.md) |
| Q-009 | Project container/schema, migrations, unsupported-version handling, recovery, and mechanics of managed media/external references | Self-contained used audio is the accepted default (D-038); SEQ-R1/R2 must resolve storage layout, embedding/copying, deduplication, collect/relocate, unused-media cleanup, and integrity without reopening that default; round-trip unknown state and durable audio | [Project format](PROJECT_FORMAT.md) |
| Q-010 | Extension identity, manifest/package format, loading/trust, updates, and live removal | Minimal SEQ-R6 content/generator requirements first; compatibility schema is Q-024; optional stronger future isolation is Q-025, not baseline correctness | [Extensions](EXTENSIONS.md) |
| Q-011 | Contextual Sample Lab target/substitution, realtime publication, downstream audition boundaries, stop/cancel/restoration, similarity, locks, and history | SEQ-R7 bounded design/evidence for standalone and contextual modes, preserving existing processing and temporary reversible audition until explicit acceptance | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-012 | Concrete object-render/audible-selection capture taps, downstream mixer/send/Master inclusion, bounds/tails, hard-cut/node-state reset, export/loop/seek tail behavior, latency, channels/rate, normalization, and cancellation | D-036 separates object-local rendering from broader audible context; D-040 distinguishes source boundary, continuing tail, and explicit hard cut. Bounded SEQ-R8 and later examples must resolve relevant reset/export/loop/seek rules without automatically destroying tails, silently baking unrelated downstream processing into object samples, or assuming whole-Master capture | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-013 | Missing realtime extension playback and compatible reattachment | Define explicit UI/fallback and verify preserved data on removal/save/reinstall | [Extensions](EXTENSIONS.md), [project format](PROJECT_FORMAT.md) |
| Q-014 | Future dependency/asset/codec/native compatibility and redistribution obligations; project-license selection resolved | Apache-2.0 selected by D-029; evaluate concrete additions and their distributed-product obligations before distribution | [Third party](THIRD_PARTY.md) |
| Q-015 | Final visual language and detailed editing interaction | Deliberate gradual UI design stages; product-led interaction and DPI/input validation when UI exists | [UI design](UI_DESIGN.md), [UX](UX_CONTRACT.md) |
| Q-016 | CLAP/VST3 hosting and Linux/macOS release schedule | Evidence-led later scope; no support or delivery commitment now | [Extensions](EXTENSIONS.md), [architecture](ARCHITECTURE.md) |

## Graph, workspace, and host capability questions

Accepted subsystem ownership does not resolve these implementation choices or expand SEQ-R0 into
a full workstation probe. Later stages should answer only the questions needed for their bounded scope.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-017 | Exact node port types, conversions, event/control rates, and channel negotiation | Define a bounded semantic type model before accepting graph connections; no untyped universal pipe or final ABI by assumption | [Node graph](NODE_GRAPH.md) |
| Q-018 | Graph validation/preparation, execution representation, and live-edit publication implementation | D-041 accepts last-valid execution and visible invalid/unpublished editor state; test bounded preparation, safe publication/state transition/resource retirement without replacing execution with an invalid candidate; compilation remains a candidate | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-019 | Exact identity/ownership of standalone item and containing-container graphs versus instrument/channel/bus/Master scopes | D-033 rules out arbitrary graph-per-event defaults; resolve scope relationships within the two local levels without assuming nesting or replacing Arrangement; event conversion is Q-048 and overlapping execution is Q-047 | [Node graph](NODE_GRAPH.md), [architecture](ARCHITECTURE.md) |
| Q-020 | Cycles and feedback in graphs | Audit explicit feedback semantics, delay/state, or specialized nodes as candidates; validation/scheduling remain open and arbitrary zero-delay cycles are not accepted | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-021 | Latency reporting/propagation/compensation and exact Live / Low-Latency thresholds/bypass strategy | Measure live paths and define processor reporting, compensation, temporary handling/restoration under D-044; preserve the project graph, visibly distinguish live behavior, and use full intended final/offline processing; no thresholds or bypass algorithm selected | [Audio](AUDIO_ENGINE.md), [node graph](NODE_GRAPH.md) |
| Q-022 | Docking groups, per-pane preferences, and layout persistence/restoration | Bounded SEQ-R3 user-layout design with predictable escape and safe size/DPI adaptation; no final grouping/storage mechanism | [Workspace](WORKSPACE.md) |
| Q-023 | Instrument/channel organizational-group hierarchy | Use naming/collapse and cross-pattern examples to choose bounded hierarchy rules; group identity stays separate from patterns/routing | [Architecture](ARCHITECTURE.md) |
| Q-024 | Exact plugin capability/version/format/state negotiation, manifest/API, and version-range mechanics | D-039 makes the current platform authoritative and requires deliberate resolution before activation; implement bounded normal/limited/compatible-path/disabled outcomes with diagnostics/preserved identity/state. Known incompatibility may reject installation, never activates, and never automatically deletes an installed plugin | [Extensions](EXTENSIONS.md) |
| Q-025 | Whether stronger plugin crash/security isolation is worth its complexity for future external hosting | Conditional evidence-led evaluation only if real hosting justifies it; weigh containment value against lifecycle/IPC cost (I-005); D-037 requires normal compatibility/lifecycle handling without hard isolation and distinguishes crash separation from a security sandbox | [Extensions](EXTENSIONS.md), [idea](IDEAS.md#i-005--out-of-process-external-plugin-crash-isolation) |
| Q-026 | Future ASIO and additional device-backend strategy | Preserve backend-independent engine/plugin contracts; evaluate concrete SDK/library terms and distribution later, not as an R0 requirement | [Audio](AUDIO_ENGINE.md), [third party](THIRD_PARTY.md) |
| Q-027 | Monitoring and audio/MIDI recording timing/latency | Define device/input clock alignment, capture placement, monitoring, and compensation for actual supported devices/workflows | [Audio](AUDIO_ENGINE.md) |

## Creative workflow model questions

SEQ-KB-R2, SEQ-KB-R5, and SEQ-KB-R6 accept human-facing direction, not final domain identities or interaction mechanisms.
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
| Q-047 | How can shared instrument definitions preserve independent overlapping execution/local processing at bounded CPU/memory cost? | Compare voice groups, execution instances, prepared routes, or other bounded strategies later; include third-party instruments needing explicit duplication; no selected instance count or free/unlimited separation | [Audio](AUDIO_ENGINE.md), [architecture](ARCHITECTURE.md), [extensions](EXTENSIONS.md) |
| Q-048 | Exact conversion of an atomic event into an independently processed standalone fragment/item | Design a low-ceremony reversible edit preserving intended music and relationships; do not select command names or domain classes from the conceptual action | [Architecture](ARCHITECTURE.md), [UX](UX_CONTRACT.md) |
| Q-049 | Exact edit-state transition when replacing an object with rendered audio containing baked processing | Verify explicit undoable replacement avoids silently reapplying the exact baked chain; evaluate bypass/removal/history or another reversible transformation without selecting mechanics or deleting sources by default | [Sample workflow](SAMPLE_WORKFLOW.md), [UX](UX_CONTRACT.md) |

## Runtime and UI platform mechanisms

SEQ-KB-R6 accepts constraints/direction under D-039 through D-044, not their implementation. Existing
Q-004/Q-005/Q-007/Q-012/Q-018/Q-021/Q-024 retain scheduling, tails, publication, latency, and plugin
negotiation questions. The following gaps cover distinct mechanisms not already owned by those entries.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-050 | Exact historical sonic-compatibility policy for a future genuinely breaking platform/audio change | Evaluate deliberately when a concrete change occurs under D-039; preserve/migrate data without promising indefinite bit-identical historical engine emulation or a permanent old-engine mode | [Audio](AUDIO_ENGINE.md), [project format](PROJECT_FORMAT.md) |
| Q-051 | Concrete audio time/stretch representation/algorithms, tempo-following versus fixed/source-time labels/defaults, and trim/split/loop/stretch interaction | D-040 accepts independent global tempo/local stretch and non-destructive source edits; design bounded time mappings and edit persistence, then gestures/modifiers/tools, without accidental trim-induced speed changes or destructive split | [Architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md), [UX](UX_CONTRACT.md), [project format](PROJECT_FORMAT.md) |
| Q-052 | Exact production-log file size, rotation/count, retention/lifetime, storage, and diagnostics controls | Implement quiet bounded production diagnostics under D-042; ordinary GUI exposes at most enable/disable where useful, developer Debug/Trace activation remains outside it; no numeric limits selected | [Settings](SETTINGS.md), [development](DEVELOPMENT.md) |
| Q-053 | Concrete user settings paths/formats and bounded application/plugin preference-reset mechanics | Use platform-appropriate user configuration under D-042; keep project-affecting plugin instance state with the project and never delete projects/managed media on reset; executable installation/discovery remains separately open in Q-010 | [Settings](SETTINGS.md), [project format](PROJECT_FORMAT.md), [UX](UX_CONTRACT.md) |
| Q-054 | Localization resource format, contribution workflow, and fallback rules | Define host resources/contracts for initial Russian/English and later languages under D-043; stable internal identity must not depend on translated display text; first-party reusable UI consumes host localization | [Architecture](ARCHITECTURE.md) |
| Q-055 | Theme resource/API details and packaging as extensions/plugins/data packages | Develop coherent Dark/Light semantic colors, typography, sizing/style and justified icon/resource roles under D-043; keep packaging open and independently rendered external native editors exempt from theme adoption | [UI design](UI_DESIGN.md), [architecture](ARCHITECTURE.md), [extensions](EXTENSIONS.md) |

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

When evidence resolves an entry, record the result and link its report/decision; do not erase the
reasoning. A compromise actually introduced into implementation belongs in [TECH_DEBT](TECH_DEBT.md).
