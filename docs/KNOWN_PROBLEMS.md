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
| Q-003 | What narrow boundary safely owns buffers and execution state? | Initialization, publication, reference/lifetime, failure, stop/restart and shutdown exercises; ABI and C# decomposition remain open | [Architecture](ARCHITECTURE.md), [audio](AUDIO_ENGINE.md) |
| Q-004 | Commands, snapshots, or a combination? | Bounded capacity/work, overflow policy, ordering, rapid edits, cancellation and stale publication behavior under stress | [Audio](AUDIO_ENGINE.md) |
| Q-005 | How do musical time, the audio clock, transport, and loops align? | Scheduled events around loop/block boundaries; timing and ordering evidence with declared sample rate/buffer settings | [Audio](AUDIO_ENGINE.md) |
| Q-006 | How much scheduling/DSP can realtime and offline share? | Device-independent bounded render and event/output comparison; state determinism, tails, and numerical limits identified | [Audio](AUDIO_ENGINE.md) |
| Q-007 | What latency, callback, and stress targets are achievable? | Declared probe environment/thresholds, callback duration and overload evidence, responsiveness under control pressure; device loss/recovery limitations stated | [Audio](AUDIO_ENGINE.md) |

These priorities do not require SEQ-R0 to solve the final plugin ecosystem or workstation schema.
Unresolved findings must retain their evidence limits rather than become accepted choices by silence.

## Later design and validation gaps

| ID | Question / risk | Intended resolution path | Owner |
| --- | --- | --- | --- |
| Q-008 | Exact musical schema, time/ID representation, and party terminology | Bounded SEQ-R1 model supporting shared patterns, notes, undo, and later editors | [Architecture](ARCHITECTURE.md) |
| Q-009 | Project container, migrations, unsupported-version handling, media defaults, and recovery | SEQ-R1/R2 format/resource design; round-trip unknown extension state and independent managed audio | [Project format](PROJECT_FORMAT.md) |
| Q-010 | Extension identity, manifest/package format, compatibility, loading/trust/isolation, updates, and live removal | Minimal SEQ-R4 content/generator requirements first; stricter realtime contracts when justified | [Extensions](EXTENSIONS.md) |
| Q-011 | Candidate audition substitution and restoration, similarity, locks, and history scope | SEQ-R5 bounded UX/algorithm experiments with solo and in-pattern evidence | [Sample workflow](SAMPLE_WORKFLOW.md) |
| Q-012 | Resampling bounds, tails, effect/send inclusion, latency, channels/rate, normalization, and cancellation | SEQ-R6 explicit semantics for one bounded source and source-preserving reuse | [Sample workflow](SAMPLE_WORKFLOW.md), [audio](AUDIO_ENGINE.md) |
| Q-013 | Missing realtime extension playback and compatible reattachment | Define explicit UI/fallback and verify preserved data on removal/save/reinstall | [Extensions](EXTENSIONS.md), [project format](PROJECT_FORMAT.md) |
| Q-014 | Final FOSS license and dependency/codec/native redistribution obligations | Select license explicitly and evaluate actual proposed additions before distribution | [Third party](THIRD_PARTY.md) |
| Q-015 | Final visual language and detailed editing interaction | Product-led workspace design and DPI/input validation when UI exists | [UX](UX_CONTRACT.md) |
| Q-016 | CLAP/VST3 hosting and Linux/macOS release schedule | Evidence-led later scope; no support or delivery commitment now | [Extensions](EXTENSIONS.md), [architecture](ARCHITECTURE.md) |

When evidence resolves an entry, record the result and link its report/decision; do not erase the
reasoning. A compromise actually introduced into implementation belongs in [TECH_DEBT](TECH_DEBT.md).
