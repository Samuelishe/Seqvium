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
| Q-004 | Commands, snapshots, or a combination? | Bounded capacity/work, overflow policy, ordering, rapid edits, cancellation and stale publication behavior under stress | [Audio](AUDIO_ENGINE.md) |
| Q-005 | How do musical time, the audio clock, transport, and loops align? | Scheduled events around loop/block boundaries; timing and ordering evidence with declared sample rate/buffer settings | [Audio](AUDIO_ENGINE.md) |
| Q-006 | How much scheduling/DSP can realtime and offline share? | Device-independent bounded render and event/output comparison; state determinism, tails, and numerical limits identified | [Audio](AUDIO_ENGINE.md) |
| Q-007 | What latency, callback, and stress targets are achievable? | Declared probe environment/thresholds, callback duration and overload evidence, responsiveness under control pressure; device loss/recovery limitations stated | [Audio](AUDIO_ENGINE.md) |

These priorities do not require SEQ-R0 to solve the final plugin ecosystem or workstation schema.
Unresolved findings must retain their evidence limits rather than become accepted choices by silence.

## Later design and validation gaps

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-008 | Exact musical-part/event schema and time/ID representation | Bounded SEQ-R1 model supporting named multi-instrument/shared patterns, compatible entry methods, undo, and later editors | [Architecture](ARCHITECTURE.md) |
| Q-009 | Project container, migrations, unsupported-version handling, media defaults, and recovery | SEQ-R1/R2 format/resource design; round-trip unknown extension state and independent managed audio | [Project format](PROJECT_FORMAT.md) |
| Q-010 | Extension identity, manifest/package format, loading/trust, updates, and live removal | Minimal SEQ-R6 content/generator requirements first; compatibility schema is Q-024 and hard isolation is Q-025 | [Extensions](EXTENSIONS.md) |
| Q-011 | Contextual Sample Lab target/substitution, realtime publication, downstream audition boundaries, stop/cancel/restoration, similarity, locks, and history | SEQ-R7 bounded design/evidence for standalone and contextual modes, preserving existing processing and temporary reversible audition until explicit acceptance | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-012 | Concrete what-you-hear render taps, mixer/send/master inclusion, bounds/tails, latency, channels/rate, normalization, and cancellation | SEQ-R8 map one semantic selection to an explicit render boundary; item, container, and range scopes must not silently collapse to dry or whole-Master capture | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
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
| Q-018 | Graph validation/preparation, execution representation, and live-edit publication | Test a bounded graph and safe publication/resource retirement under audio constraints; compilation is a candidate approach | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-019 | Exact ownership/identity of local item and containing-container graphs versus instrument/channel/bus/master scopes | Refine accepted two-level local direction from workflows; resolve multiple/local graph scope relationships without instantiating every scope, assuming nesting, or replacing Arrangement | [Node graph](NODE_GRAPH.md), [architecture](ARCHITECTURE.md) |
| Q-020 | Cycles and feedback in graphs | Audit explicit feedback semantics, delay/state, or specialized nodes as candidates; validation/scheduling remain open and arbitrary zero-delay cycles are not accepted | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-021 | Latency propagation/compensation across graph nodes | Measure processing paths and define scheduling/render consequences; avoid assuming every node is zero-latency | [Audio](AUDIO_ENGINE.md), [node graph](NODE_GRAPH.md) |
| Q-022 | Docking groups, per-pane preferences, and layout persistence/restoration | Bounded SEQ-R3 user-layout design with predictable escape and safe size/DPI adaptation; no final grouping/storage mechanism | [Workspace](WORKSPACE.md) |
| Q-023 | Instrument/channel organizational-group hierarchy | Use naming/collapse and cross-pattern examples to choose bounded hierarchy rules; group identity stays separate from patterns/routing | [Architecture](ARCHITECTURE.md) |
| Q-024 | Plugin capability/version/format/state compatibility schema | Establish deliberate normal/limited/compatible-path/disabled outcomes with diagnostics and preserved state in SEQ-R6 and later contracts | [Extensions](EXTENSIONS.md) |
| Q-025 | Recoverable plugin failure versus hard native crash isolation | Assess in-process limits and stronger hosting boundaries when executable plugins justify them; no universal crash-containment promise | [Extensions](EXTENSIONS.md) |
| Q-026 | Future ASIO and additional device-backend strategy | Preserve backend-independent engine/plugin contracts; evaluate concrete SDK/library terms and distribution later, not as an R0 requirement | [Audio](AUDIO_ENGINE.md), [third party](THIRD_PARTY.md) |
| Q-027 | Monitoring and audio/MIDI recording timing/latency | Define device/input clock alignment, capture placement, monitoring, and compensation for actual supported devices/workflows | [Audio](AUDIO_ENGINE.md) |

## Creative workflow model questions

SEQ-KB-R2 accepts human-facing direction, not final domain identities or interaction mechanisms.
These questions remain open and do not authorize implementation in this documentation stage.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-028 | Final Layer/Track/Channel/Lane/container term and domain identity; compatibility, preferred target, ownership, nesting, and audio-clip semantics | Design a bounded semi-free timeline model; keep Pattern, Instrument Group, and Mixer Channel distinct rather than choose classes from vocabulary | [Architecture](ARCHITECTURE.md) |
| Q-029 | Shared audio resource versus placement/reference/edit ownership, including contextual acceptance affecting existing uses | Define non-destructive local edits and explicit acceptance scope with shared-use examples; destructive source edits require separate semantics, not silent mutation | [Architecture](ARCHITECTURE.md), [project format](PROJECT_FORMAT.md), [sample workflow](SAMPLE_WORKFLOW.md) |
| Q-030 | Arrangement-container processing relationship to mixer channels and buses | Trace local/container/global signal boundaries and useful defaults without merging timeline and routing identities | [Architecture](ARCHITECTURE.md), [node graph](NODE_GRAPH.md) |
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

## Retained resolution trace

**Q-014 — Project-license selection resolved; third-party scope remains open.** The original question
was "Final FOSS license and dependency/codec/native redistribution obligations". SEQ-KB-R4 selected
Apache License 2.0 for Seqvium-authored work through
[D-029](DECISIONS_LOG.md#d-029--seqvium-uses-apache-license-20) and root [LICENSE](../LICENSE).
The project license is no longer an open choice. The narrowed Q-014 table entry retains only future
dependency/content compatibility and redistribution evaluation under [THIRD_PARTY](THIRD_PARTY.md).

When evidence resolves an entry, record the result and link its report/decision; do not erase the
reasoning. A compromise actually introduced into implementation belongs in [TECH_DEBT](TECH_DEBT.md).
