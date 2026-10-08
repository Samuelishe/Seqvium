# Audit history

Role: Historical audits and past validation narratives.
Read when: Explicit history, provenance, rationale, supersession, or reconstruction is needed.
Authoritative for: Historical records and traceability only.
Not authoritative for: Current behavior, policy, plans, implementation state, or open questions.

This is a cold, non-canonical archive excluded from normal current context. Active owner documents
win any conflict; historical wording records its stage, not a competing current specification.

Historical findings below describe the R7 checkpoint. Their ranking/context is preserved for
reconstruction. Still-open entries are maintained in [current questions](../KNOWN_PROBLEMS.md);
Q-013 and Q-060 are now resolved in [resolved questions](RESOLVED_QUESTIONS.md). Historical Q-ID
mentions/tables here are checkpoint observations, not additional live question definitions.

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
| Q-058 | Autosave/crash recovery has no accepted workflow; a generic R12 recovery line does not define recoverable music or protect recorded media. Missing workflow / data-loss risk | What is recovery state versus explicit Save; how are corruption-safe behavior, bounded retention, recorded media and crash-restart user choice handled? Define required protection before real projects, without selecting autosave cadence, journal or storage scheme | [Project format](../PROJECT_FORMAT.md), [UX](../UX_CONTRACT.md); R1/R2 foundations, R12 integrity, later recording |
| Q-059 | Self-contained used media has no complete unsaved-project, Save As/collect/relocate or failure transaction. Missing workflow / integrity risk | When does imported/accepted/captured audio become durable, and what happens on disk-full, interrupted copy/save, moved projects or missing external media? Discuss integrity checks, retention with undo/recovery, relinking and explicit cleanup separately from Q-009 encoding; do not choose storage or automatic deletion | [Project format](../PROJECT_FORMAT.md), [sample workflow](../SAMPLE_WORKFLOW.md); R1/R2/R7/R8/R12, recording |
| Q-060 | Invalid/unpublished editable graph can differ from audible last-valid execution; save/reopen/export revision selection is unspecified. Missing decision / reproducibility risk | What may Save and render do while graphs diverge, and what must reopening restore or diagnose? Define intended revision/validation/user choice without persisting live execution objects, silently discarding edits, or selecting an automatic fallback | [Node graph](../NODE_GRAPH.md), [project format](../PROJECT_FORMAT.md), [audio](../AUDIO_ENGINE.md), [UX](../UX_CONTRACT.md); R4/R8/R12 |
| Q-061 | First Track Release is a goal without explicit minimum workflow/dependency closure. Roadmap dependency / delivery risk | Which small sampler/sample-based track must R12 finish end-to-end, and which prior stage supplies each prerequisite? Review R7/R8 bounded local processing before R11 Mixer, R10 basic stretch versus R14+ richness (Q-051), render latency (Q-021), recovery/media (Q-058/Q-059) and host UI services (Q-067). Recording, automation and CLAP/VST3 are later and must not be silently assumed prerequisites; owner decides any scope/order change | [Roadmap](../ROADMAP.md), [architecture](../ARCHITECTURE.md), [audio](../AUDIO_ENGINE.md), [sample workflow](../SAMPLE_WORKFLOW.md); R2–R12/R14+ |

### Significant

**Q-021** remains more than future low-latency UX: latency-bearing branches can already misalign
mixing or rendered material before advanced compensation is scheduled. **Q-010/Q-016/Q-024** separate
package management, runtime hosting and compatibility; a generator-only R6 contract is not proof of
realtime plugin lifetime/state/editor correctness. **Q-029/Q-048/Q-049** retain explicit shared-edit,
event-conversion and baked-replacement choices; these must not be solved by unlinking everything.

| ID | Concrete problem / type and impact | Next discussion / evidence, not an accepted answer | Owners / affected stages |
| --- | --- | --- | --- |
| Q-056 | Manual export range can intersect a valid tail. Missing product decision | Does the selected range default to a hard render boundary, or should an explicit include-tails-like option extend it? Preserve project cuts/tails under D-047 whichever range rule is later chosen | [Audio](../AUDIO_ENGINE.md), [UX](../UX_CONTRACT.md); R12 export, related R8 range capture |
| Q-057 | Accepted loop/seek/Stop intentions do not establish stateful DSP transitions or finite tail completion. Technical risk | How are destination warm-up, hard-cut reset, loop tail ownership and fast bounded playback settling implemented without duplicated state or stale sound? Compare realtime/offline entry-state cases, long/nondecaying tails and cancellation. Feedback topology is Q-020; Record Stop timing is Q-027; no algorithm or fade duration chosen | [Audio](../AUDIO_ENGINE.md), [node graph](../NODE_GRAPH.md); R4/R5/R8/R10/R12, later processors |
| Q-062 | Device loss/change and startup/project-open audio failure have no user recovery flow. Missing workflow / lifetime risk | What happens to transport, active voices, capture and the still-editable project when a device disappears or cannot initialize; how does a user retry/select a device? Distinguish failure of audio execution from file/plugin open policy Q-013; validate teardown/republication/rate changes without blocking callbacks | [Audio](../AUDIO_ENGINE.md), [UX](../UX_CONTRACT.md), [settings](../SETTINGS.md); R2 onward, later recording |
| Q-063 | Undo exists as a foundation but transaction boundaries across shared edits, asynchronous acceptance and plugin edits are undefined. Missing decision / integrity risk | Which gestures/actions form one document edit, when does asynchronous work commit, and how do undo/deletion/project close invalidate pending results? Include variation, event conversion, rendered replacement and retained media; workspace preference undo is a separate scope choice | [Architecture](../ARCHITECTURE.md), [UX](../UX_CONTRACT.md), [sample workflow](../SAMPLE_WORKFLOW.md); R1/R4/R5/R7/R8/R10, hosting |
| Q-064 | Mouse-first custom chrome, overlapping panes and graph feedback have no accessibility/input acceptance scope. Missing workflow / platform risk | What keyboard/focus paths, accessible window controls, non-color status cues and discoverable actions are required? Validate resize/move/maximize/close, focus return from external editors, pane reachability, DPI and usable minimums on actual platforms before shell acceptance; no concrete gestures or dimensions selected | [Workspace](../WORKSPACE.md), [UX](../UX_CONTRACT.md), [UI design](../UI_DESIGN.md), [portability](../PORTABILITY.md); R3 and later UI |
| Q-065 | Discovery/reuse is central but Browser and personal sample/preset library have no bounded workflow or stage owner commitment. Missing workflow | What minimum browse/audition/import and accepted-sample reuse path is needed for R7/R12? Decide whether personal-library publication/preset saving is required later, with project-copy versus library identity and reset/removal safety. Do not assume a catalog/index/store feature | [Sample workflow](../SAMPLE_WORKFLOW.md), [UX](../UX_CONTRACT.md), [project format](../PROJECT_FORMAT.md), [roadmap](../ROADMAP.md); R2/R7/R12 review |
| Q-066 | Cross-context sidechain/control routes may cross the two local processing scopes; exposure mechanics alone do not settle their lifetime or scheduling. Missing boundary / technical risk | Which cross-scope connections are permitted when needed, and how do moving/deleting a target, shared placements, rate/latency and offline capture affect them? Use concrete signal/control cases; preserve bounded local UI reasoning without silently accepting arbitrary feedback or a third local layer | [Node graph](../NODE_GRAPH.md), [architecture](../ARCHITECTURE.md), [audio](../AUDIO_ENGINE.md); R4 foundations, R10/R11/deeper routing and R14+ control |
| Q-067 | Localization/theme contracts are accepted but minimum host service introduction and contribution lifetime/versioning are not assigned. Roadmap dependency / API risk | Which bounded RU/EN and semantic resource services must exist for R3 shell and R6/R7 first-party surfaces? Resolve resource identity/fallback, contributions and unload with Q-054/Q-055 before exposing contracts; theme packaging remains open and independent external editor visuals exempt | [Architecture](../ARCHITECTURE.md), [UI design](../UI_DESIGN.md), [extensions](../EXTENSIONS.md), [roadmap](../ROADMAP.md); R3/R6/R7 |
| Q-068 | Linux/macOS architectural targets and hosted checks do not define platform release scope. Missing planning decision | Which desktop/device acceptance is required for each intended release, distinct from RIDs/prerequisites Q-041? Keep R14+ additional-platform delivery distinct from early portable architecture/CI; no delivery dates or parity selected | [Portability](../PORTABILITY.md), [roadmap](../ROADMAP.md), [test execution](../TEST_EXECUTION.md); R0 distribution evidence through R12/R14+ release review |
| Q-069 | Project-owned audio intent and negotiated device rate/channels/buffers can differ. Missing boundary / reproducibility risk | Which values are durable project intentions versus runtime device facts, and what supported adaptation/diagnostic is needed when reopening on another device? Honor D-045 without silently rewriting the project or pretending hardware is portable; project/offline sound settings must not live-link to user defaults | [Settings](../SETTINGS.md), [audio](../AUDIO_ENGINE.md), [project format](../PROJECT_FORMAT.md); R1/R2/R12 |
| Q-071 | Candidate similarity/locks/history are bundled with contextual substitution but have distinct limits and user choices. Missing interaction decision | What constitutes a nearby variant or lock, and what bounded candidate history/comparison is useful for the one/two R7 families? Separate disposable exploration retention from explicit accepted audio; choose with concrete examples, not a universal similarity metric | [Sample workflow](../SAMPLE_WORKFLOW.md), [UI design](../UI_DESIGN.md); R7 |

### Minor / cleanup

| ID | Concrete problem / type | Next discussion / action when authorized | Owner |
| --- | --- | --- | --- |
| Q-070 | `.editorconfig` applies LF to every file while `.gitattributes` checks out `.bat`/`.cmd` as CRLF. Objective passive-policy contradiction; no batch scripts currently exist | Align the editor exceptions with the accepted Git batch-script policy in a later authorized config change. R7 reports the discrepancy without changing configuration or introducing tooling | [Development](../DEVELOPMENT.md), [.editorconfig](../../.editorconfig), [.gitattributes](../../.gitattributes) |

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


## Pre-R8 retrieval evolution policy

Superseded stage-based policy text retained for reconstruction; the active governance now owns rolling
state-based maintenance and separate current/history retrieval classes.

### Earlier knowledge evolution

The document ownership model stays stable while retrieval mechanisms evolve:

1. **Stage 0 — established:** AGENTS, compact current state, index/routing, canonical owner docs,
   affected files, and ordinary repository search. No generated context infrastructure.
2. **Stage 1 — policy portion established by SEQ-KB-R3:** coding, development, verification,
   portability, CI, and ideas owners now exist before source, with passive `.editorconfig` and
   `.gitattributes`. This does not start implementation. `docs/FILE_INDEX.md` remains deferred until
   meaningful source topology exists. Add small baseline tooling or one local workflow skill only
   for an observed need; skills are not mandatory infrastructure. SEQ-KB-R4 adds an accepted future
   [ProjectStats contract](../PROJECT_STATS.md), independent from retrieval evolution; executable tooling
   still waits for explicit authorization.
3. **Stage 2 — measurable context-selection problems:** consider a generated repository map and
   bounded context planner. Generated outputs remain disposable retrieval artifacts, not canonical
   truth. Do not create retrieval manifests or budgets now; structural
   ProjectStats introduction follows its separate owner and is not a prerequisite for retrieval tooling.
4. **Stage 3 — exact routing/search demonstrably insufficient:** consider a local semantic index,
   hybrid RAG, and possibly MCP exposure. Retrieval must retain source provenance and never silently
   replace Markdown, source, tests, or Git history as authority.

Promote retrieval infrastructure only in response to an observed problem. Report a broken retrieval
tool and fall back to direct owners/search; a planner or index must not narrow the authorized task or
silently redefine the product. No AgentContext/planner/RAG infrastructure is introduced by SEQ-KB-R3.

Prefer cheap deterministic compiler/editor/build/CI enforcement over agent memory when justified.
[DEVELOPMENT](../DEVELOPMENT.md#machine-enforcement-and-text-consistency) maps newline/diagnostic policy
to existing passive configuration and nullable/warnings/SDK/formatting/portability rules to future
enforcement points. Do not introduce tools simply because enforcement is possible.


## Pre-R8 project-state checkpoint

Historical handoff as recorded at R7; it does not describe the current checkpoint.

## Current checkpoint

SEQ-KB-R7 complete (2026-10-07): accepted project/default/instance ownership, compatibility metadata,
contract-based plugin compatibility, transport/render and main-window/responsive UI semantics integrated;
then the entire updated canonical knowledge base audited. No implementation stage started.

## Implemented capability

Documentation/legal/policy foundation, root [LICENSE](../../LICENSE), and passive `.editorconfig` /
`.gitattributes`. No application/audio/experiment implementation, dependencies, test projects,
executable tooling, or CI exists; the pre-existing `Seqvium.sln` contains no projects.

## Current focus

Preserve the accepted product and repository foundation. Executable work requires explicit owner
authorization. [SEQ-R0](../ROADMAP.md#seq-r0--audio-architecture-probe) remains **pending / not started**;
[ProjectStats](../PROJECT_STATS.md) is a future contract, not a runnable tool.

## Validation baseline

R7 privacy searches, Markdown links/anchors, unique decision/question IDs, owner/routing, scope,
LF/whitespace and Git preservation checks passed; see the bounded
[work-log entry](WORK_LOG.md#2026-10-07--seq-kb-r7). The audit records a passive batch-newline policy
discrepancy (Q-070); no build/test/runtime/platform acceptance is claimed.

## Active blockers / evidence gaps

The completed knowledge stage does not close implementation risks. High-impact open choices include
missing-plugin project opening (Q-013), independent overlapping execution (Q-047), recovery/media
integrity (Q-058/Q-059), divergent graph save/render (Q-060), and milestone dependency closure (Q-061).
Audio architecture, toolchain/test choices and later workflow mechanisms remain unvalidated in the
[global audit register](AUDITS.md#seq-kb-r7-global-audit-findings). Compact handoff policy belongs to
[documentation governance](../DOCUMENTATION_GOVERNANCE.md#compact-current-state-contract).

## SEQ-KB-R20 — Cross-platform release scope audit

Historical conceptual audit, 2026-10-08; no build, runtime, hardware or distributed artifact was tested.
Current policy belongs to [Portability](../PORTABILITY.md#evidence-led-release-policy), not this record.

| Case | Conclusion within the documentation audit |
| --- | --- |
| A — Hosted Windows | Restore/Release build/tests establish only the checked runner contracts, not actual Windows desktop or physical audio-device reliability |
| B — Hosted Linux | Ubuntu build/offline tests establish bounded build/software evidence, not all distributions, desktops, audio servers or hardware |
| C — Hosted macOS | Managed/native compilation does not establish working desktop/audio, delivered dependencies or applicable signing/notarization readiness; no tools/architectures selected |
| D — Windows primary | Windows 11 development/manual tests are legitimate early priority; domain, audio/plugin and storage boundaries remain portable for all three OS targets |
| E — WSLg | Separately report Linux process/runtime, WSLg GUI and bridged audio. Success is useful only for that environment; native Linux desktop/device acceptance remains separate |
| F — Native Linux | Check implemented window/chrome, input/focus, scaling, output/device selection, filesystem and native dependencies in named actual environments; no X11/Wayland or ALSA/PipeWire choice |
| G — GUI without audio | Keep safely interpretable document access and editing; explicitly separate playback, recording and device readiness. An editor opening does not establish full DAW support |
| H — Different devices | One interface/backend/configuration does not validate another. Declare actual rate/channel/buffer/workload scope; no universal platform claim or numeric performance threshold |
| I — Native distribution | If native is chosen, substantiate build -> materialization -> loading -> interop -> bounded execution with the delivered component; undeclared developer libraries cannot prove deployment |
| J — CPU architecture | Architectural portability does not promise every CPU; exact OS/CPU/RID/prerequisites remain Q-041, and delivery requirements may differ |
| K — Install and launch | Source build, development-output launch, packaging, clean delivered installation/materialization, launch/dependencies and applicable update/removal are separate evidence; no installer/updater selected |
| L — Filesystem case | Preserve identity/references/resource paths on sensitive and insensitive filesystems; prevent or honestly detect case collisions before unsafe materialization, without choosing a format |
| M — Project across OS | Preserve canonical meaning/relationships and managed audio independently of old device/path preferences. Missing executable plugins retain identity/opaque state and scoped degraded behavior; no universal plugin portability |
| N — Save/recovery | R19 failure/durability contracts remain. Validate claimed OS/filesystem/volume guarantees; Windows success or one rename cannot establish cross-platform crash consistency |
| O — Preferences/workspace | Environment language/theme/layout/device preferences must not become musical dependencies; old monitor/device/platform paths cannot be required merely to edit moved work |
| P — First-party UI | Actual implemented window/chrome, keyboard/focus, DPI/pointer, panes/overlays/docking and claimed accessibility need bounded desktop evidence; external editor checks follow hosting implementation |
| Q — Extensions/plugins | Metadata does not prove platform executable compatibility. Preserve stable identity/state and existing missing-capability behavior; no early CLAP/VST3 or arbitrary external-plugin portability |
| R — Offline rendering | Device-independent validation can establish musical/scheduling/DSP/render correctness with declared numerical assumptions; no physical-device claim or arbitrary cross-processor/platform bit identity |
| S — Conditional release | Windows with sufficient declared software/desktop/audio/delivery evidence may be supported; Linux hosted-only and macOS build-only retain those labels. All three remain architectural targets |
| T — Later evidence | Add supported environments/backends/devices/architectures as evidence grows, using existing portable boundaries rather than redesigning the domain; no dates/order/parity promised |

Working release-policy directions 1–10 accepted with bounded scope: Windows remains the early primary
environment, all three OS families retain architecture and early hosted ownership, and release claims
require actual capability evidence. Linux/macOS dates are not tied to Windows; ready targets may ship
earlier than R14+. R12 declares accepted environments; later coverage expands without abandoning
portable document meaning. Missing/unverified intended audio forbids full DAW support claims.
Deferred distribution does not make architecture/CI optional. Conceptual labels are neither a final
public vocabulary nor a readiness percentage, and hardware cannot replace software/packaging evidence.
The actual milestone target subset is still a later Q-041/Q-061 choice: bounded Windows-first delivery
or inclusion of adequately evidenced Linux/macOS environments. No user preference is needed to accept
these claim limits now; no target subset, delivery date, native/backend/toolchain choice or full R12
scenario is invented. Q-068 is policy-resolved; related technical questions remain open.
