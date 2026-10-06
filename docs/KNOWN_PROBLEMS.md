# Known problems and open questions

Role: Register of unresolved risks, questions, and validation gaps.
Read when: Planning experiments or checking whether an uncertain choice has evidence.
Authoritative for: Open uncertainty, impact, and intended resolution paths.
Not authoritative for: Accepted resolutions, existing implementation debt, progress, or stage order.

These are questions about future work, not bugs in a nonexistent application. All entries are open.
Owners keep the detailed constraints; this register identifies what is not yet established.

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
| Q-011 | Candidate audition substitution and restoration, similarity, locks, and history scope | SEQ-R7 bounded UX/algorithm experiments with solo and in-pattern evidence | [Sample workflow](SAMPLE_WORKFLOW.md) |
| Q-012 | Resampling bounds, tails, effect/send inclusion, latency, channels/rate, normalization, and cancellation | SEQ-R8 explicit semantics for one bounded source and source-preserving reuse | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-013 | Missing realtime extension playback and compatible reattachment | Define explicit UI/fallback and verify preserved data on removal/save/reinstall | [Extensions](EXTENSIONS.md), [project format](PROJECT_FORMAT.md) |
| Q-014 | Final FOSS license and dependency/codec/native redistribution obligations | Select license explicitly and evaluate actual proposed additions before distribution | [Third party](THIRD_PARTY.md) |
| Q-015 | Final visual language and detailed editing interaction | Product-led workspace design and DPI/input validation when UI exists | [UX](UX_CONTRACT.md) |
| Q-016 | CLAP/VST3 hosting and Linux/macOS release schedule | Evidence-led later scope; no support or delivery commitment now | [Extensions](EXTENSIONS.md), [architecture](ARCHITECTURE.md) |

## Graph, workspace, and host capability questions

Accepted subsystem ownership does not resolve these implementation choices or expand SEQ-R0 into
a full workstation probe. Later stages should answer only the questions needed for their bounded scope.

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-017 | Exact node port types, conversions, event/control rates, and channel negotiation | Define a bounded semantic type model before accepting graph connections; no untyped universal pipe or final ABI by assumption | [Node graph](NODE_GRAPH.md) |
| Q-018 | Graph validation/preparation, execution representation, and live-edit publication | Test a bounded graph and safe publication/resource retirement under audio constraints; compilation is a candidate approach | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-019 | Which graph scopes are needed: instrument/channel/bus/master/clip? | Establish ownership from concrete workflows; do not instantiate every scope automatically or replace Arrangement | [Node graph](NODE_GRAPH.md), [architecture](ARCHITECTURE.md) |
| Q-020 | Cycles and feedback in graphs | Decide validation and safe execution rules before permitting cycles; feedback semantics and bounded scheduling remain unselected | [Node graph](NODE_GRAPH.md), [audio](AUDIO_ENGINE.md) |
| Q-021 | Latency propagation/compensation across graph nodes | Measure processing paths and define scheduling/render consequences; avoid assuming every node is zero-latency | [Audio](AUDIO_ENGINE.md), [node graph](NODE_GRAPH.md) |
| Q-022 | Docking groups, per-pane preferences, and layout persistence/restoration | Bounded SEQ-R3 user-layout design with predictable escape and safe size/DPI adaptation; no final grouping/storage mechanism | [Workspace](WORKSPACE.md) |
| Q-023 | Instrument/channel organizational-group hierarchy | Use naming/collapse and cross-pattern examples to choose bounded hierarchy rules; group identity stays separate from patterns/routing | [Architecture](ARCHITECTURE.md) |
| Q-024 | Plugin capability/version/format/state compatibility schema | Establish deliberate normal/limited/compatible-path/disabled outcomes with diagnostics and preserved state in SEQ-R6 and later contracts | [Extensions](EXTENSIONS.md) |
| Q-025 | Recoverable plugin failure versus hard native crash isolation | Assess in-process limits and stronger hosting boundaries when executable plugins justify them; no universal crash-containment promise | [Extensions](EXTENSIONS.md) |
| Q-026 | Future ASIO and additional device-backend strategy | Preserve backend-independent engine/plugin contracts; evaluate concrete SDK/library terms and distribution later, not as an R0 requirement | [Audio](AUDIO_ENGINE.md), [third party](THIRD_PARTY.md) |
| Q-027 | Monitoring and audio/MIDI recording timing/latency | Define device/input clock alignment, capture placement, monitoring, and compensation for actual supported devices/workflows | [Audio](AUDIO_ENGINE.md) |

When evidence resolves an entry, record the result and link its report/decision; do not erase the
reasoning. A compromise actually introduced into implementation belongs in [TECH_DEBT](TECH_DEBT.md).
