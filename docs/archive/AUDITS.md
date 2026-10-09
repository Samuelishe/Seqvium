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

Existing **Q-013** is the missing-plugin project-open decision, not just a playback fallback question. **Q-047** is a
feasibility/resource risk for independent overlapping instrument execution; neither
shared definitions nor an external plugin's mixed output prove separability. **Q-019/Q-030** require
item/Pattern/container/Mixer boundaries before scope-specific preparation, sampling, and Arrangement
can be designed safely. Preserve these distinct identities; do not choose a universal graph to remove
the uncertainty. **Q-003/Q-004/Q-005/Q-018** retain native lifetime, sample-accurate scheduling,
overload recovery and publication evidence needs; R0 cannot validate all later production mechanisms.

| ID    | Concrete problem / type and impact                                                                                                                                             | Next discussion / evidence, not an accepted answer                                                                                                                                                                                                                                                                                                                                                                                                 | Owners / affected stages                                                                                                                         |
|-------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| Q-058 | Autosave/crash recovery has no accepted workflow; a generic R12 recovery line does not define recoverable music or protect recorded media. Missing workflow / data-loss risk   | What is recovery state versus explicit Save; how are corruption-safe behavior, bounded retention, recorded media and crash-restart user choice handled? Define required protection before real projects, without selecting autosave cadence, journal or storage scheme                                                                                                                                                                             | [Project format](../PROJECT_FORMAT.md), [UX](../UX_CONTRACT.md); R1/R2 foundations, R12 integrity, later recording                               |
| Q-059 | Self-contained used media has no complete unsaved-project, Save As/collect/relocate or failure transaction. Missing workflow / integrity risk                                  | When does imported/accepted/captured audio become durable, and what happens on disk-full, interrupted copy/save, moved projects or missing external media? Discuss integrity checks, retention with undo/recovery, relinking and explicit cleanup separately from Q-009 encoding; do not choose storage or automatic deletion                                                                                                                      | [Project format](../PROJECT_FORMAT.md), [sample workflow](../SAMPLE_WORKFLOW.md); R1/R2/R7/R8/R12, recording                                     |
| Q-060 | Invalid/unpublished editable graph can differ from audible last-valid execution; save/reopen/export revision selection is unspecified. Missing decision / reproducibility risk | What may Save and render do while graphs diverge, and what must reopening restore or diagnose? Define intended revision/validation/user choice without persisting live execution objects, silently discarding edits, or selecting an automatic fallback                                                                                                                                                                                            | [Node graph](../NODE_GRAPH.md), [project format](../PROJECT_FORMAT.md), [audio](../AUDIO_ENGINE.md), [UX](../UX_CONTRACT.md); R4/R8/R12          |
| Q-061 | First Track Release is a goal without explicit minimum workflow/dependency closure. Roadmap dependency / delivery risk                                                         | Which small sampler/sample-based track must R12 finish end-to-end, and which prior stage supplies each prerequisite? Review R7/R8 bounded local processing before R11 Mixer, R10 basic stretch versus R14+ richness (Q-051), render latency (Q-021), recovery/media (Q-058/Q-059) and host UI services (Q-067). Recording, automation and CLAP/VST3 are later and must not be silently assumed prerequisites; owner decides any scope/order change | [Roadmap](../ROADMAP.md), [architecture](../ARCHITECTURE.md), [audio](../AUDIO_ENGINE.md), [sample workflow](../SAMPLE_WORKFLOW.md); R2–R12/R14+ |

### Significant

**Q-021** remains more than future low-latency UX: latency-bearing branches can already misalign
mixing or rendered material before advanced compensation is scheduled. **Q-010/Q-016/Q-024** separate
package management, runtime hosting and compatibility; a generator-only R6 contract is not proof of
realtime plugin lifetime/state/editor correctness. **Q-029/Q-048/Q-049** retain explicit shared-edit,
event-conversion and baked-replacement choices; these must not be solved by unlinking everything.

| ID    | Concrete problem / type and impact                                                                                                                                                       | Next discussion / evidence, not an accepted answer                                                                                                                                                                                                                                                                                          | Owners / affected stages                                                                                                                                     |
|-------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Q-056 | Manual export range can intersect a valid tail. Missing product decision                                                                                                                 | Does the selected range default to a hard render boundary, or should an explicit include-tails-like option extend it? Preserve project cuts/tails under D-047 whichever range rule is later chosen                                                                                                                                          | [Audio](../AUDIO_ENGINE.md), [UX](../UX_CONTRACT.md); R12 export, related R8 range capture                                                                   |
| Q-057 | Accepted loop/seek/Stop intentions do not establish stateful DSP transitions or finite tail completion. Technical risk                                                                   | How are destination warm-up, hard-cut reset, loop tail ownership and fast bounded playback settling implemented without duplicated state or stale sound? Compare realtime/offline entry-state cases, long/nondecaying tails and cancellation. Feedback topology is Q-020; Record Stop timing is Q-027; no algorithm or fade duration chosen | [Audio](../AUDIO_ENGINE.md), [node graph](../NODE_GRAPH.md); R4/R5/R8/R10/R12, later processors                                                              |
| Q-062 | Device loss/change and startup/project-open audio failure have no user recovery flow. Missing workflow / lifetime risk                                                                   | What happens to transport, active voices, capture and the still-editable project when a device disappears or cannot initialize; how does a user retry/select a device? Distinguish failure of audio execution from file/plugin open policy Q-013; validate teardown/republication/rate changes without blocking callbacks                   | [Audio](../AUDIO_ENGINE.md), [UX](../UX_CONTRACT.md), [settings](../SETTINGS.md); R2 onward, later recording                                                 |
| Q-063 | Undo exists as a foundation but transaction boundaries across shared edits, asynchronous acceptance and plugin edits are undefined. Missing decision / integrity risk                    | Which gestures/actions form one document edit, when does asynchronous work commit, and how do undo/deletion/project close invalidate pending results? Include variation, event conversion, rendered replacement and retained media; workspace preference undo is a separate scope choice                                                    | [Architecture](../ARCHITECTURE.md), [UX](../UX_CONTRACT.md), [sample workflow](../SAMPLE_WORKFLOW.md); R1/R4/R5/R7/R8/R10, hosting                           |
| Q-064 | Mouse-first custom chrome, overlapping panes and graph feedback have no accessibility/input acceptance scope. Missing workflow / platform risk                                           | What keyboard/focus paths, accessible window controls, non-color status cues and discoverable actions are required? Validate resize/move/maximize/close, focus return from external editors, pane reachability, DPI and usable minimums on actual platforms before shell acceptance; no concrete gestures or dimensions selected            | [Workspace](../WORKSPACE.md), [UX](../UX_CONTRACT.md), [UI design](../UI_DESIGN.md), [portability](../PORTABILITY.md); R3 and later UI                       |
| Q-065 | Discovery/reuse is central but Browser and personal sample/preset library have no bounded workflow or stage owner commitment. Missing workflow                                           | What minimum browse/audition/import and accepted-sample reuse path is needed for R7/R12? Decide whether personal-library publication/preset saving is required later, with project-copy versus library identity and reset/removal safety. Do not assume a catalog/index/store feature                                                       | [Sample workflow](../SAMPLE_WORKFLOW.md), [UX](../UX_CONTRACT.md), [project format](../PROJECT_FORMAT.md), [roadmap](../ROADMAP.md); R2/R7/R12 review        |
| Q-066 | Cross-context sidechain/control routes may cross the two local processing scopes; exposure mechanics alone do not settle their lifetime or scheduling. Missing boundary / technical risk | Which cross-scope connections are permitted when needed, and how do moving/deleting a target, shared placements, rate/latency and offline capture affect them? Use concrete signal/control cases; preserve bounded local UI reasoning without silently accepting arbitrary feedback or a third local layer                                  | [Node graph](../NODE_GRAPH.md), [architecture](../ARCHITECTURE.md), [audio](../AUDIO_ENGINE.md); R4 foundations, R10/R11/deeper routing and R14+ control     |
| Q-067 | Localization/theme contracts are accepted but minimum host service introduction and contribution lifetime/versioning are not assigned. Roadmap dependency / API risk                     | Which bounded RU/EN and semantic resource services must exist for R3 shell and R6/R7 first-party surfaces? Resolve resource identity/fallback, contributions and unload with Q-054/Q-055 before exposing contracts; theme packaging remains open and independent external editor visuals exempt                                             | [Architecture](../ARCHITECTURE.md), [UI design](../UI_DESIGN.md), [extensions](../EXTENSIONS.md), [roadmap](../ROADMAP.md); R3/R6/R7                         |
| Q-068 | Linux/macOS architectural targets and hosted checks do not define platform release scope. Missing planning decision                                                                      | Which desktop/device acceptance is required for each intended release, distinct from RIDs/prerequisites Q-041? Keep R14+ additional-platform delivery distinct from early portable architecture/CI; no delivery dates or parity selected                                                                                                    | [Portability](../PORTABILITY.md), [roadmap](../ROADMAP.md), [test execution](../TEST_EXECUTION.md); R0 distribution evidence through R12/R14+ release review |
| Q-069 | Project-owned audio intent and negotiated device rate/channels/buffers can differ. Missing boundary / reproducibility risk                                                               | Which values are durable project intentions versus runtime device facts, and what supported adaptation/diagnostic is needed when reopening on another device? Honor D-045 without silently rewriting the project or pretending hardware is portable; project/offline sound settings must not live-link to user defaults                     | [Settings](../SETTINGS.md), [audio](../AUDIO_ENGINE.md), [project format](../PROJECT_FORMAT.md); R1/R2/R12                                                   |
| Q-071 | Candidate similarity/locks/history are bundled with contextual substitution but have distinct limits and user choices. Missing interaction decision                                      | What constitutes a nearby variant or lock, and what bounded candidate history/comparison is useful for the one/two R7 families? Separate disposable exploration retention from explicit accepted audio; choose with concrete examples, not a universal similarity metric                                                                    | [Sample workflow](../SAMPLE_WORKFLOW.md), [UI design](../UI_DESIGN.md); R7                                                                                   |

### Minor / cleanup

| ID    | Concrete problem / type                                                                                                                                                    | Next discussion / action when authorized                                                                                                                                                    | Owner                                                                                                          |
|-------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------|
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

| Case                      | Conclusion within the documentation audit                                                                                                                                                                                      |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| A — Hosted Windows        | Restore/Release build/tests establish only the checked runner contracts, not actual Windows desktop or physical audio-device reliability                                                                                       |
| B — Hosted Linux          | Ubuntu build/offline tests establish bounded build/software evidence, not all distributions, desktops, audio servers or hardware                                                                                               |
| C — Hosted macOS          | Managed/native compilation does not establish working desktop/audio, delivered dependencies or applicable signing/notarization readiness; no tools/architectures selected                                                      |
| D — Windows primary       | Windows 11 development/manual tests are legitimate early priority; domain, audio/plugin and storage boundaries remain portable for all three OS targets                                                                        |
| E — WSLg                  | Separately report Linux process/runtime, WSLg GUI and bridged audio. Success is useful only for that environment; native Linux desktop/device acceptance remains separate                                                      |
| F — Native Linux          | Check implemented window/chrome, input/focus, scaling, output/device selection, filesystem and native dependencies in named actual environments; no X11/Wayland or ALSA/PipeWire choice                                        |
| G — GUI without audio     | Keep safely interpretable document access and editing; explicitly separate playback, recording and device readiness. An editor opening does not establish full DAW support                                                     |
| H — Different devices     | One interface/backend/configuration does not validate another. Declare actual rate/channel/buffer/workload scope; no universal platform claim or numeric performance threshold                                                 |
| I — Native distribution   | If native is chosen, substantiate build -> materialization -> loading -> interop -> bounded execution with the delivered component; undeclared developer libraries cannot prove deployment                                     |
| J — CPU architecture      | Architectural portability does not promise every CPU; exact OS/CPU/RID/prerequisites remain Q-041, and delivery requirements may differ                                                                                        |
| K — Install and launch    | Source build, development-output launch, packaging, clean delivered installation/materialization, launch/dependencies and applicable update/removal are separate evidence; no installer/updater selected                       |
| L — Filesystem case       | Preserve identity/references/resource paths on sensitive and insensitive filesystems; prevent or honestly detect case collisions before unsafe materialization, without choosing a format                                      |
| M — Project across OS     | Preserve canonical meaning/relationships and managed audio independently of old device/path preferences. Missing executable plugins retain identity/opaque state and scoped degraded behavior; no universal plugin portability |
| N — Save/recovery         | R19 failure/durability contracts remain. Validate claimed OS/filesystem/volume guarantees; Windows success or one rename cannot establish cross-platform crash consistency                                                     |
| O — Preferences/workspace | Environment language/theme/layout/device preferences must not become musical dependencies; old monitor/device/platform paths cannot be required merely to edit moved work                                                      |
| P — First-party UI        | Actual implemented window/chrome, keyboard/focus, DPI/pointer, panes/overlays/docking and claimed accessibility need bounded desktop evidence; external editor checks follow hosting implementation                            |
| Q — Extensions/plugins    | Metadata does not prove platform executable compatibility. Preserve stable identity/state and existing missing-capability behavior; no early CLAP/VST3 or arbitrary external-plugin portability                                |
| R — Offline rendering     | Device-independent validation can establish musical/scheduling/DSP/render correctness with declared numerical assumptions; no physical-device claim or arbitrary cross-processor/platform bit identity                         |
| S — Conditional release   | Windows with sufficient declared software/desktop/audio/delivery evidence may be supported; Linux hosted-only and macOS build-only retain those labels. All three remain architectural targets                                 |
| T — Later evidence        | Add supported environments/backends/devices/architectures as evidence grows, using existing portable boundaries rather than redesigning the domain; no dates/order/parity promised                                             |

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

## SEQ-KB-R21 — Roadmap dependency and First Track closure audit

Historical documentation/architecture audit, 2026-10-08, baseline clean `master` at
`45ebcf7b5678114c344f5d0d53b20e758ed6b53a`, checkpoint SEQ-KB-R20. The solution contains no projects;
no application, engine, tests, experiments or executable tooling exists. No external technical claims
were needed: conclusions below derive from repository contracts. No measured feasibility, realtime,
storage, DSP, accessibility, plugin or platform evidence is claimed.

Current obligations live in [Roadmap](../ROADMAP.md#first-track-scenario-and-acceptance-boundary) and
[execution ownership](../ROADMAP.md#execution-and-timing-acceptance-ownership); remaining uncertainty
lives in [Known problems](../KNOWN_PROBLEMS.md). This archive records the audit and partial Q-061 answer,
not a second authoritative requirement table. No new Q-ID or D-record is justified: identified gaps
have existing owners/questions, and corrections clarify existing capabilities rather than select
new product policy, schema, technology or implementation architecture.

### Evidence and dependency vocabulary

- **Accepted requirement:** an existing canonical owner constrains the eventual product. It is not
  evidence that the behavior works. Roadmap changes here only assign bounded responsibility/acceptance.
- **Conceptual conclusion:** a dependency follows from those requirements; reasons accompany hard
  prerequisites. No theoretical model proves runtime correctness.
- **Bounded design:** a concrete representation, UI or protocol must be chosen before its owning
  capability is implemented. Several choices remain possible; no universal implementation is inferred.
- **Measured evidence:** executable probe, lifecycle/fault exercise, DSP comparison or real desktop/
  device/distribution validation is required. A successful R0 baseline cannot close later cases.
- **Later capability:** valuable direction that can remain absent from the bounded R12 composition.

**H — Hard prerequisite:** without it the specified operation cannot execute or would violate semantics. **I —
Incremental foundation:** an earlier narrow capability suffices and grows later. **O — Optional enhancement:** useful
for the composition but unnecessary to finish it. **F — Future expansion:** deliberately beyond the R12 path. **E —
Evidence dependency:** a technical answer is gated by actual probe/implementation results.
These labels describe capabilities, not whole stages: adjacency in the roadmap does not make every
previous stage a hard technical prerequisite. An optional creative path still has its own mandatory
acceptance when its stage is shipped.

### Pass 1 — Stage-by-stage audit

Each row records consumption/foundation, introduced capability, independently exercisable completion,
deferral, later consumers, relevant questions and necessary evidence. All completion evidence is future.

| Stage                                    | Consumes and why                                                                                                                                                                               | Introduces / independently exercisable completion                                                                                                                                                                                        | Defers / later consumers                                                                                                                                                                                  | Open questions / completion evidence                                                                                                                                                                                            |
|------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| R0 — Audio Architecture Probe            | Accepted realtime/control/clock/lifetime and portable-adapter contracts; E feasibility before major production execution choices                                                               | Minimal experimental device host, sample/tone events, transport/loop/control stress and bounded offline comparison; inspect timing, overload recovery, managed/native pressure and stop/restart/retirement                               | No production engine, final schema, graph editor, external host, ASIO, recording or three-platform certification; informs R2/R4 and constrains R1 time/boundary coordination                              | Q-001–Q-007; declared setup/thresholds/measurements/limits and explicit accept/reject/narrow recommendation; backend/language/ABI require evidence-backed acceptance                                                            |
| R1 — Domain and Musical Model Foundation | H accepted musical/document/settings/format semantics: later edits require identifiable canonical targets; R0 E informs execution-facing assumptions, not pure model testability               | Unnamed lifecycle, content/sound/resource/placement/context/route identity relationships, position/pitch/duration/velocity, project settings, coherent transactions/Undo, bounded versioned Save/reopen including unresolved intent      | No full Arrangement/Mixer, recovery UI, final attachment inventory, storage GC or event extraction; supplies every later document consumer                                                                | Q-008/Q-009/Q-019/Q-023/Q-028/Q-029/Q-063/Q-069; model edits, shared-reference/variation/sound-independence examples, Undo/round-trip, unknown-state/degraded/version cases; no device required                                 |
| R2 — Audio Resource / Device Foundation  | H R1 document/resource/edit ownership for accepted import; E R0 disposition before production callback/backend commitment                                                                      | WAV access/preview/import/reuse, durable unnamed media, core pitched sampler, event duration/release/velocity and bounded overlap, transport/output/device foundation; exercise through subsystem API and controlled device/offline host | No complete Browser/shell, catalog, multisampler/synth, all codecs/backends, polished input/recording; supplies R4/R5/R7/R8/R9/R10                                                                        | Q-005/Q-006/Q-047/Q-057/Q-059/Q-062/Q-069/Q-065/Q-063; imported source removal, failed/partial import, owner retention, tonal pitch/note/release/order and device failure/configuration evidence; no mandatory CLI product      |
| R3 — Workspace Shell Foundation          | I R1 lifecycle/actions/settings boundary; host UI candidate needs Q-037 decision. R2 integration is convenient, not needed to prove pane geometry/focus                                        | Main window, limited internal panes, safe user layout, focus/target separation, discoverable pointer and ordinary keyboard window/pane actions, initial RU/EN and Dark/Light rails                                                       | No full musical panes, mature resource registry, pixel-perfect system or extension host; all subsequent first-party surfaces consume shell/actions/styles                                                 | Q-022/Q-037/Q-053/Q-054/Q-055/Q-064; actual limited desktop/chrome/focus/DPI/escape/restore and preference tests; UI logic alone does not prove native interaction                                                              |
| R4 — Node Graph Foundation               | H R1 canonical identity/transactions and R2 source/output/execution boundary: connections need targets and actual audio; H R3 for graph pane only, not pure graph validation                   | Typed supported connections, source/input/output, gain/mix/routing, stable attachments, prepared revision convergence/retirement; model-driven events produce audible and offline graph output before R5                                 | No full R10 scopes/UI, R11 effects suite, optional realtime host, universal feedback/sidechain/modulation; supplies musical execution, audition, render and later contexts                                | Q-005/Q-017/Q-018/Q-019/Q-020/Q-021/Q-029/Q-047/Q-057/Q-063/Q-066; topology/type/blocker, publication/obsolete revision/close/lifetime, contribution separation and supported realtime/offline/state evidence                   |
| R5 — Pattern Workspace                   | H R1 compatible events/shared content, R2 core sound, R4 execution and R3 interaction: step edits must be heard and target the same saved Pattern                                              | Channel Rack, Step Sequencer, multi-channel velocity/loop/live edits, basic sound audition integration and clear shared target; first musical editing workflow                                                                           | No full Piano Roll/Arrangement, universal per-note graphs, external plugins; R7 uses current Pattern/sound context, R8 renders its content and R9/R10 reuse events                                        | Q-008/Q-029/Q-047/Q-057/Q-063/Q-064; several instruments looping, live note/edit/stop/release, shared edits/Undo/reopen and pointer/keyboard target cases; one tone is insufficient                                             |
| R6 — Extension Foundation                | H existing host/resource/document boundaries and R3 UI rails for actual contributions; I R4 needed only for a module using graph execution                                                     | Local bounded first-party identity/discovery/package/capability lifecycle, missing-state and opaque-data preservation, contribution availability/retirement                                                                              | No marketplace, mature updater or CLAP/VST3 bridge; generator-specific async contract is enough for R7, future realtime guests need stricter contracts                                                    | Q-010/Q-024/Q-054/Q-055/Q-067; compatible/incompatible/missing activation, fallback, active-use block/defer, load/unload/close and unknown-state round-trip without external hosting                                            |
| R7 — Sample Lab / Generator V1           | H R6 for honest removable generator, R2 durable audio, R1 commit gate, R3 pane/resources; H R4/R5 for supported contextual Pattern audition                                                    | One/two procedural families, random/related variants, declared locks/history/comparison, standalone and existing sound/Pattern contextual listening, explicit durable resource/shared-sound acceptance                                   | No Arrangement context before R10, event-to-item conversion, arbitrary audio similarity or global library; R12 integrates resulting resources, but track creation does not require generator availability | Q-011/Q-024/Q-029/Q-047/Q-057/Q-059/Q-063/Q-067/Q-071; substitution/current-sound restoration, changed/deleted/closed target races, failed acceptance and generator removal; retained audio versus recipe/history distinguished |
| R8 — Resampling                          | H R1 frozen canonical/edit gate, R2 sound/media, R4 prepared graph and event schedule; Pattern scope can be model-tested, R5 supplies ordinary selection. External dependencies H only if used | At least bounded Pattern-content object render, actual source/local processing, finite range/state/tails, rate/channels, cancellation, storage/acceptance and immediate reuse with source intact                                         | No Arrangement capture before R10, complex sends/Master capture or R11 effects dependency; optional replacement needs Q-049 only if offered. R7 generator is not a hard rendering dependency              | Q-012/Q-018/Q-021/Q-047/Q-049/Q-057/Q-059/Q-063/Q-066; offline event/DSP output, boundary/dependency exclusion or blocker, finite failure, stale target, Undo/media retention; no silent last-valid render                      |
| R9 — Piano Roll                          | H R1/R5 same canonical events and targets, R2/R4 pitched sound/scheduling, R3 shell; no R7/R8 prerequisite for note editing                                                                    | Pitch/duration/velocity editing and audition of existing musical content with shared-reference semantics                                                                                                                                 | No new instrument/source engine, recording or timeline; R10/R12 consume completed drum and pitched phrases                                                                                                | Q-008/Q-029/Q-063/Q-064 plus Q-005/Q-047/Q-057 execution; Step/Piano round-trip, durations/pitches/Undo, no hidden copies, pointer/keyboard/focus evidence                                                                      |
| R10 — Arrangement                        | H canonical occurrence/sharing/time/context identity from R1/R4, actual samples/events from R2/R5/R9 and shell; I offline execution from R4/R8                                                 | Shared placements/explicit variations, named containers, direct audio, source-range/split/trim/move/repeat, fixed/follow-tempo and audible local stretch, bounded local/context assignment and arranged offline scheduling               | No universal nesting/override/time engine or rich sends; user-facing Mixer/effect suite follows R11. Core Gain/Mix suffices to exercise earlier local/context ownership                                   | Q-008/Q-019/Q-028/Q-029/Q-030/Q-047/Q-051/Q-057/Q-059/Q-063/Q-064/Q-066; overlap/separation, variation versus sound independence, trim/split/tempo/stretch audible round-trip and context-move tests before R12                 |
| R11 — Mixer / Core Processing            | H existing separate routes/contributions/contexts from R4/R10 and core sound; Mixer cannot reconstruct independent signals from a premature sum                                                | Channels/basic intentional bus/Master, gain/pan/mute/solo, core EQ/compression usable in applicable local/global scopes; shared controls address one DSP context                                                                         | No complete sends/sidechains, universal PDC, every processor or external downloads; extends arranged offline path for R12                                                                                 | Q-017/Q-018/Q-019/Q-021/Q-030/Q-047/Q-057/Q-064/Q-066; independent/aggregate controls, once-only processing, supported rate/channel behavior, state/latency and realtime/offline comparison                                     |
| R12 — First Track Release                | H earlier musical/source/time/graph/routing/media foundations for the scenario; R6/R7/R8 have stage acceptance even though creative use is O                                                   | Whole composition, final WAV range/settings/delivery/errors, Save As/repair/recovery usability, packaging and evidence per actual supported platform                                                                                     | No professional-workstation inventory or implicit recording/external host; does not originate scheduler, media lifetime, identities or renderer                                                           | Q-009/Q-012/Q-018/Q-021/Q-029/Q-041/Q-047/Q-051/Q-054/Q-055/Q-057/Q-058/Q-059/Q-061/Q-062/Q-063/Q-064/Q-065/Q-067/Q-069; integrated scenario plus negative/fault cases and actual distribution/desktop/device/storage evidence  |
| R13 — Extension Ecosystem                | H real R6/R7 lifecycle experience, compatibility and retained document state; E broader packaging/update evidence                                                                              | Mature install/update/remove and diagnostics within justified supported categories                                                                                                                                                       | No mandatory store or external host; no hard predecessor relationship to R12                                                                                                                              | Q-010/Q-024/Q-067/Q-014/Q-041; actual package rollback/dependencies/active use and retained project/UI behavior; bounded by categories shipped                                                                                  |
| R14+ — Evidence-led expansion            | H/I existing portable host, identity/time/media/execution contracts as each added feature needs them; E specific new capability evidence                                                       | Recording/monitoring, automation, richer instruments/DSP/stretch/routing, external hosting, Personal Library publication and added platform coverage where justified                                                                     | Order/scope/date open; these cannot become hidden dependencies of the audited composition                                                                                                                 | Q-016/Q-025/Q-026/Q-027/Q-033/Q-034/Q-041/Q-048/Q-050/Q-065 and extensions of Q-021/Q-047/Q-051/Q-057/Q-066; feature-specific evidence, no blanket early acceptance                                                             |

### Cross-stage dependency matrix

Summary of audit coverage; canonical detail remains in the linked owners. “Yes” means the invariant
must hold for shipped scenario capabilities, not that every possible inventory item is mandatory.

| Capability / invariant                                     | First required                                  | Implemented / extended by                                  | Required for R12?                            | Evidence / open Q                                              | Risk                                                           |
|------------------------------------------------------------|-------------------------------------------------|------------------------------------------------------------|----------------------------------------------|----------------------------------------------------------------|----------------------------------------------------------------|
| Feasible callback/control/lifetime boundary                | R0 probe; production R2                         | R0 informs R2/R4, later processors                         | Yes, for actual execution                    | Q-001–Q-007; measured declared environment                     | E; selecting engine/ABI without evidence                       |
| Canonical document/lifecycle/revision/settings             | R1                                              | R1; each stage adds owned fields; R12 integrity            | Yes                                          | Q-008/Q-009/Q-063/Q-069; round-trip/close/concurrent edits     | Late or filename-derived identity                              |
| Content/sound/resource/occurrence/context/route separation | R1 relationships, R4 concrete attachments       | R1/R4, R5/R9 shared edits, R10/R11 scopes                  | Yes                                          | Q-019/Q-028/Q-029/Q-030; serializable model examples           | Destructive redesign if one universal Track/source ID          |
| Canonical transactions/Undo and commit authority           | R1 edits; R2 async import                       | R1/R2/R4/R5/R7/R8/R9/R10/R11                               | Yes                                          | Q-063 with Q-059; gesture and deterministic race/failure cases | Wrong target, partial edit, resurrected work                   |
| Versioned Save/degraded unknown state                      | R1                                              | R1/R4/R6; R12 Save As/repair                               | Yes                                          | Q-009/Q-018/Q-024/Q-029; invalid/unknown round-trip            | Saving executable snapshot instead of intent                   |
| Managed WAV import and unnamed multi-owner media           | R2                                              | R2; R7/R8 output; R12 recovery/transfer                    | Yes                                          | Q-059/Q-063; source removal, storage failure/lifetime          | Owner too late if treated as R12 polish                        |
| Core pitched sampler and duration/release/velocity         | R2                                              | R2; R4 domains; R5/R9 use, R10 overlap                     | Yes                                          | Q-005/Q-047/Q-057; audible tonal notes/release                 | Trigger-only sampler insufficient for declared pitched editing |
| Transport/event scheduling/time-to-sample mapping          | R2 bounded events                               | R4 graph; R5 loops; R8 offline; R10 arrangement            | Yes                                          | Q-005/Q-006/Q-057/Q-069; block/loop/seek/rate tests            | Previously implicit production owner                           |
| Source/output/gain/mix and contribution-preserving routes  | R2 output; R4 graph                             | R4 bounded paths; R10 contexts; R11 controls               | Yes                                          | Q-017/Q-019/Q-030/Q-047; routed independent outputs            | Hidden dependency on Mixer or optional node                    |
| Graph validity, publication and temporary last-valid       | R4                                              | R4; R7 audition; R8/R10/R11 render/context changes         | Yes                                          | Q-018/Q-019/Q-063; obsolete/invalid/close/state retirement     | Stale audio exported as canonical                              |
| Timing/latency/DSP-state fidelity for supported paths      | R2 notes; R4 graph; before latency-bearing path | R4/R5/R8/R10/R11/R12 as capabilities arrive                | Yes, applicable subset                       | Q-005/Q-017/Q-018/Q-021/Q-047/Q-057                            | Uncompensated shipped parallel paths; universal PDC too early  |
| UI shell/focus/commands/keyboard/history grouping          | R3; R4 first graph editor                       | R3/R4/R5/R7/R9/R10/R11; R12 integration                    | Yes                                          | Q-037/Q-063/Q-064; desktop/input/DPI                           | Late focus patch or inaccessible custom chrome                 |
| Localization/theme and contribution lifecycle              | R3 host; R6 modules                             | R3/R6/R7; R12 shipped RU/EN, Dark/Light                    | Yes, bounded                                 | Q-054/Q-055/Q-067; missing resources/removal/preferences       | Shell depends on mature host/SDK                               |
| Optional modules/opaque missing state                      | R6                                              | R6/R7; R12 integration; R13 management                     | Yes for shipped modules; no for core sound   | Q-010/Q-024/Q-067; activation/unload/unknown data              | Removable generator becomes mandatory instrument               |
| Pattern/shared notes, Piano editing, explicit variation    | R1 model; R5 editing                            | R5/R9/R10                                                  | Yes                                          | Q-008/Q-029/Q-063; same events and shared-reference cases      | Step-specific boolean schema or invisible copies               |
| Sample Lab generation/context audition                     | R7                                              | R7; later R10 contexts; R12 integration                    | Stage acceptance yes; track use O            | Q-011/Q-029/Q-059/Q-063/Q-071                                  | Unsupported Arrangement context at R7                          |
| Frozen-scope offline object render/acceptance              | R4 comparison; production R8                    | R8 Pattern; R10 timeline; R11 effects; R12 export          | Stage acceptance yes; resampled ingredient O | Q-012/Q-018/Q-021/Q-057/Q-059/Q-063/Q-066                      | Renderer first appears during packaging                        |
| Direct audio timeline/musical and source mapping           | R1 leaves room; R10 audible use                 | R10; R12 workflow; R14+ richer DSP                         | Yes, bounded trim/split/repeat/stretch       | Q-051/Q-057; audible mapping and saved offsets                 | “Time stretching later” accidentally defers all basic stretch  |
| Local/container processing and intentional submix          | R4 references/primitives                        | R10 scope UI; R11 core processors and shared views         | Yes, supported two levels                    | Q-019/Q-030/Q-047/Q-057; context move/once-only DSP            | Premature aggregation, extra local tree or double DSP          |
| Mixer channels/basic buses/Master/EQ/compressor            | R4 routing; R11 common effects/UI               | R11; R12 complete mix                                      | Yes, bounded                                 | Q-017/Q-021/Q-030/Q-057; DSP/controls/render                   | Missing core effects or source separation                      |
| WAV range/render settings/output delivery                  | R8 internal rate/channels/ranges                | R10/R11 render extensions; R12 export UX/file delivery     | Yes                                          | Q-012/Q-057/Q-061/Q-069; no-device export/error/cancel         | Device recording or stale snapshot mistaken for export         |
| Rolling recovery, Save As and repair experience            | R1 identity, R2 media lifetime                  | R12 complete shipped integrity; earlier incremental checks | Yes                                          | Q-058/Q-059; named/unnamed fault/race/coverage cases           | Safe earlier media/Save postponed until release                |
| Actual supported-platform delivery                         | Portable boundaries from R0/R1                  | R2 device, R3+ GUI, R12 delivered acceptance               | Yes for each declared target                 | Q-041 plus subsystem evidence; GUI/audio/artifact/storage      | Hosted/build success advertised as support                     |
| Cross-context send/detector/control dependency             | R4 semantic room; first actual connection       | R8 checks if present; bounded R10/R11; R14+ expansion      | No mature system; integrity H if shipped     | Q-066/Q-021/Q-057; capture or explicit blocker                 | Silent omission or universal framework too early               |
| Recording/ASIO/CLAP/VST3/automation/library/marketplace    | Later feature-specific stage                    | R13 lifecycle; R14+ justified expansion                    | No                                           | Q-016/Q-025/Q-026/Q-027/Q-033/Q-034/Q-065                      | Optional future scope promoted into critical path              |

No mandatory invariant remains without a named stage after the bounded ownership clarifications.
Production scheduler/offline path and minimum sampler/timing acceptance were implicit or ambiguous,
not evidence of an absent accepted contract. No new implementation milestone is needed to own them.

### Pass 2 — A finished small-track walkthrough

Representative composition: 16 bars, 4/4, user-chosen 100 BPM; intro (1–4), fuller rhythm/bass/melody (5–12), a
fill/phrase variation and ending (13–16). Import Kick, Snare, Hat, a known-pitch short pluck
WAV and a texture WAV. Drums Main is a multi-instrument Pattern; Bass A and Melody A use pitched
sample notes. Repeated clips share their content. A deliberate Drums Fill/phrase variation has its own
notes while retaining intended shared sounds. Demonstrate a separately editable sampler sound
configuration without duplicating immutable media. Reuse the texture directly in Arrangement with
different local ranges/time/processing. Choose deliberate drum aggregation plus separate bass/melody/
texture routes into Master; preserve independent paths until that intended convergence.

These counts and BPM are an example, not new limits/defaults, required musical genre, supplied sample
pack or numerical performance promise. Short plucked bass/melody is musically sufficient for a bounded
sample-based composition. Sustained notes beyond available source audio are not promised; choose notes
within the declared source behavior rather than invent a looping sampler or synthesizer. The source's
actual pitch/duration/release/overlap behavior must be demonstrated by R2/R4 before Piano Roll acceptance.

| Candidate step                         | Contract support / bounded scenario treatment                                                                     | Stage path and eventual observable evidence                                                                                                                                                                       |
|----------------------------------------|-------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 — New unnamed project                | Accepted lifecycle and unnamed ownership                                                                          | R1, R3 UI, R2 media, R12 recovery; distinct working identity before path exists                                                                                                                                   |
| 2 — Tempo/musical settings             | Accepted project tempo/time signature and defaults copied once                                                    | R1, later editors; change user defaults without rewriting this project; notes retain musical positions                                                                                                            |
| 3 — Import/audition WAVs               | Accepted minimum source/project access; no full catalog                                                           | R2 subsystem, R3+ user integration, R12 usability; preview has no Undo/import, accepted source survives origin removal                                                                                            |
| 4 — Pitched sample instrument          | R2 explicitly owns simple sampler and note/pitch; minimum duration/release/velocity acceptance clarified          | R2/R4 core; audible known-pitch notes and note-end/source-end behavior without optional generator                                                                                                                 |
| 5 — Drum and pitched parts             | Accepted multi-instrument events with pitch/duration/intensity                                                    | R1/R2/R4/R5/R9; hear several distinct parts, not a tone demo                                                                                                                                                      |
| 6 — Step and Piano editors             | Accepted same event model and target                                                                              | R5/R9; edit phrase in both, preserve unsupported-by-current-view data, Undo/reopen                                                                                                                                |
| 7 — Repeated shared Patterns           | Accepted normal sharing                                                                                           | R1/R5/R10; edit Main and observe its references without changing independent Fill                                                                                                                                 |
| 8 — Independent musical variation      | Accepted separate from sound independence                                                                         | R1 model/R10 command; one coherent variation transaction, sound/media references retained; separately exercise sound-setting independence                                                                         |
| 9 — Direct Arrangement audio           | Accepted supported audio occurrences and non-destructive source use                                               | R2/R10; two clips reference one managed texture, local edits remain independent                                                                                                                                   |
| 10 — Move/trim/split/local time        | Accepted ordinary operations and distinct trim/repeat/stretch; algorithms/limits open                             | R10 Q-051; hear and save fixed/follow-tempo modes and local stretch; no accidental speed change on trim                                                                                                           |
| 11 — Local processing                  | Accepted item and one containing context, not every event graph                                                   | R4 primitives/R10 target/UI/R11 processors; item Gain/EQ and explicit container compression, own-result hard cuts                                                                                                 |
| 12 — Channels/bus routing              | Accepted routes distinct from timeline; no forced 1:1                                                             | R4/R10/R11; separate bass/texture and intentional drum submix to Master, no constituent recovery after sum                                                                                                        |
| 13 — Gain/pan/mute/solo/EQ/compression | R11 explicitly planned bounded common processing; usable core required                                            | R11; audible parameter/route changes, saved controls and offline DSP agree within declared tolerances                                                                                                             |
| 14 — Generate/resample an ingredient   | Accepted creative paths, optional in the composition                                                              | R6/R7/R8; separately accept candidate/render and reuse it, remove generator without losing accepted bytes                                                                                                         |
| 15 — Coherent WAV output               | Accepted First Track and canonical render/cuts                                                                    | R4 comparison/R8 renderer/R10 schedule/R11 DSP/R12 output; intended range, declared format/rate/channels, finite cancel/errors, no device dependency                                                              |
| 16 — Save/close/reopen                 | Accepted canonical/versioned Save and coherent captured revision                                                  | R1 incremental serialization, each stage's fields, R12 full UX; preserve named/unnamed Save As and old saved state on failure                                                                                     |
| 17 — Verify reopened relationships     | Accepted reference/musical/media/context invariants                                                               | All owning stages/R12; shared edits, independent variation/sound settings, timeline offsets, paths and once-only processors match saved canonical intent                                                          |
| 18 — Recoverable failure               | Accepted degraded media/capability access; one concrete case suffices for walkthrough, subsystem coverage broader | Remove one managed sample in controlled validation; R12 preserves broken reference, allows healthy editing, blocks dependent export, deliberate managed repair/removal resumes it. No external plugin is required |
| 19 — Interrupted unsaved work          | Accepted recovery, including unnamed project; mechanism not available yet                                         | R1/R2 foundations/R12; simulate interruption after an identified captured revision, offer unsaved recovery separately, preserve last explicit Save/media; state possibly uncaptured interval                      |
| 20 — Distributed platform validation   | Accepted R20 evidence-led support policy                                                                          | R12; run identified delivered artifact on every declared supported environment with GUI/audio/project/recovery evidence, not inferred parity                                                                      |

The walkthrough checks the candidate steps against owners, not automatically upgrades every suggestion
to an exhaustive release requirement. Basic buses mean intentional aggregation/routing, not arbitrary
sends. Missing media is the mandatory representative failure; arbitrary optional realtime processing
is not required to manufacture one. Tail inclusion UI, sustained-sample loops, advanced source analysis,
exact export formats/rates/channels and full accessibility APIs remain bounded design/evidence choices.
R12 must identify its supported choices and test them; it cannot defer all basic playable stretch or
pitched sound production while still advertising this scenario. R7/R8 optional use in the track does
not waive their own completion evidence or the accepted creative loop.

### Focused investigations A–R

**A — R0 feasibility versus production architecture.** R0 is an E gate for choosing production
callback/control/buffer-lifetime/backend/language/ABI assumptions before R2 and the R4 prepared graph.
R1's pure musical/document model can be designed/tested independently of device APIs; execution-facing
time/ID/publication assumptions must coordinate with the probe disposition. The current sequence keeps
R0 before major construction by policy, not because every R1 domain invariant needs audio hardware.
R0 need not prove final graph compilation, plugin compensation, storage recovery, arbitrary source
instancing or three-platform delivery. A negative/narrow result requires an explicit bounded response,
not silent commitment to the candidate or infinite workstation planning. Q-001–Q-007 remain E.

**B — R1 sufficiency.** Accepted owners already require lifecycle before paths, canonical revision/
transactions, justified stable identities, shared Pattern parts and sound-use references, resource
versus placement, organization versus processing/route, musical time, versioned incomplete Save/reopen
and project versus user defaults. Nothing genuinely missing calls for a new semantic identity. Concrete
Q-008/Q-009/Q-029 must coordinate with Q-019 before fixing references, to represent later attachments
and unresolved endpoints without retrofitting a universal Track ID. Model/round-trip examples suffice:
three Pattern references, one variation, independent sound settings, two resource uses and a processing
context/route reference. No actual R10 UI, R11 channel engine or full unknown-type plugin schema is
required in R1. Evolvable versioning is still necessary; “bounded” cannot mean disposable bool steps.

**C — R2 before R3.** Resource discovery/access is an application/subsystem operation. A controlled
host or later tests can enumerate user-chosen WAVs/project resources, preview, import, play pitched
events and inspect canonical/storage outcomes without a mature Browser. Device evidence uses a bounded
host just as R0 does; this is not a CLI product promise or early polished UI. R3+ integrates the same
operations into shipped surfaces, R12 proves usability. Storage/availability/failure evidence at R2
cannot be postponed until a final Browser exists. No hard R2→R3→R2 cycle.

**D — R4 before R5.** Model-driven scheduled events already exist through R1/R2. Feed a prepared
source→Gain→Mix/Output path, multiple inputs/branches and bounded invalid connections with known
events/resources. That checks ports, contribution convergence, publication and audio without a musical
editor. R5 consumes execution rather than being its compiler or scheduler. A minimal graph pane can
work over R3; pure validation/offline execution is independent of UI. No source plugin, mature route UI,
every DSP node or all graph scopes is necessary. Minimum graph primitives become completion obligations
instead of optional wording; full effects still arrive in R11.

**E — Core sound without extensions.** R2's simple sampler with declared pitched-note behavior can
play drums and short tonal bass/melody. Note duration/release/velocity and bounded overlapping events
need explicit acceptance; a one-shot trigger with a pitch knob alone cannot prove Piano Roll's behavior.
Q-047/Q-057 already own source/state evidence, so no new synth question is created. R4 Gain/Mix/routing
and R11 core EQ/compression require no optional downloads. R6 hosts optional generators, not mandatory
transport/sampler/graph. Generator absence removes exploration, not accepted audio or the core track.
General opaque instruments, mono/legato and universal multisampling remain later capability evidence.

**F — R7 contextual audition before Arrangement.** Valid contexts are existing Pattern parts/sampler
sound definitions with R4-supported processing and downstream output while R5 loops. Standalone is
always possible. R7 can explicitly update the shared sound definition or accept an unattached reusable
resource; one placed/event occurrence is offered only if its independent relationship already exists.
Do not fake an Arrangement target, a containing timeline context or Q-048 extraction. Contextual audition
must restore current canonical sound after intervening edits, not its launch snapshot. R10 extends the
available contexts later without weakening the full accepted contextual contract.

**G — R8 scopes before Arrangement/Mixer.** A Pattern musical-content definition has events and sound
uses; its render needs no Arrangement placement and inherits no arbitrary container/Mixer context.
Preserve individual required source paths before final render-output aggregation. R4 Gain/source
behavior suffices; later EQ/compression is not required just to render. A model-local item/context may
also be supported only if it already exists and is exercisable, not because every candidate scope is
listed in the owner. No full Arrangement range, complicated sends or whole-Master capture is implied.
Required external inputs must be frozen with correct history/time or explicitly blocked as unsupported,
never replaced by convenient silence/stale playback. R8 owns finite production offline render for its
subset; R10/R11 extend it as their scheduling/DSP arrives.

**H — Shared R5/R9/R10 musical model.** Position, pitch, duration and velocity are already required in
R1. Step Sequencer projects/edits that canonical data; it cannot replace it with a bool array or erase
richer data merely because its view is narrower. Piano Roll adds editing, Arrangement adds references/
timing, variation changes intended content reference in one transaction. Sound independence addresses
sound settings, local placement edits address one use. Neither the richer editor nor R10 may require
recreating phrases in a second event schema. Q-008/Q-029/Q-063 concrete model/commands remain R1 and
incremental stage design, not a separate planning blocker.

**I — R10 time/stretch.** Before first audio timeline gestures, design the bounded mapping between
project musical position, source sample/time range, tempo relationship and local stretch. Fixed-time
audio retains physical duration on BPM change; tempo-following stays aligned with its musical duration.
Notes/Pattern placements remain in musical time. Trim changes range without speed, split preserves
resource and appropriate offsets/mapping in the pieces, repeat reuses material, stretch changes time
mapping. Local stretch and project tempo are separate operations. Specify supported combinations and
rounding so split/move/reopen/offline output do not drift or destructively alter the source. A durable
flag without audible DSP is insufficient. Q-051 owns algorithm/quality/range and pitch interaction;
no algorithm or universal pitch-preservation claim is selected. Bounded audible local stretch and both
tempo relationships belong to R10/R12; rich independent pitch/time tools stay R14+.

**J — Mixer versus routing.** Basic output/voices in R2 and typed gain/convergence/routes in R4 precede
R11. R10 realizes item/containing context and assignment using existing primitives, without needing
the common-effects pane. R11 adds route/channel/bus controls and core EQ/compressor with the same
host processing boundary, accessible in supported local contexts as well. Context C seen in Arrangement
and Mixer runs once, retaining its scope; a separate deliberate downstream bus is a real additional
path. Multi-instrument Pattern is no implicit bus. Neither R4 nor R10 should hard-code all instruments
into one aggregate and expect the Mixer to split it later. No R11→R4 cycle is justified.

**K — Minimum latency/timing/state.** Q-005 owns ordered note time and block/loop/tempo/rate conversion;
Q-017 types/rates/channel negotiation; Q-018 validation/publication; Q-047 performance independence;
Q-057 seek/loop/stop/hard-cut/tails; Q-021 latency reporting/alignment. R2 handles source events/release,
R4 prepared scheduling/state for its paths, R5 live loops, R8 entry state and finite render, R10 timeline
cuts/overlap/stretch, R11 its actual processor delay and state. If parallel/externally dependent paths
have meaningful delay, provide required alignment/reporting or narrow/block the unsupported capability.
A measured low/zero algorithmic-latency core set may avoid a full compensation system, but must not
hide device/buffer latency or claim universal alignment. R8/R12 offline entry-state/history/cuts must
reproduce intended supported semantics, not merely matching buffer sizes. No bit identity for arbitrary
plugins; no full Live/Low-Latency mode, recording compensation or universal plugin PDC required early.

**L — Project integrity sequencing.** R1 Save/reopen protects coherent canonical revisions and
references; it needs bounded correct Save failure/version behavior before users rely on it, not the
complete rolling-recovery UI. R2 must establish durable accepted unnamed media before source removal,
separately retained saved/working/history/pending/live owners and prior-state protection on failed
acceptance. A conservative retention strategy can precede sophisticated GC; deleting by zero visible
uses is invalid. R7/R8 use common commit gates and storage, protect unaccepted owned output, and do not
attach stale results. R12 completes Save As/repair/recovery/reconciliation experience and verifies faults,
including ambiguous completion and sole unnamed protection. Recovery requires previous lifetime rails;
it cannot introduce resource ownership retroactively. Q-009/Q-058/Q-059/Q-063 already own this work.
Full collect/relocate, external-reference and recording protocols need not all ship by R12.

**M — Graph identity/publication.** R1 owns semantic target/reference foundations; R4 chooses bounded
attachment/endpoint representation for actual graphs with Q-019/Q-029, leaving room for local,
containing and global contexts and meaningful unresolved references. R10 extends attachments rather
than changes all IDs. R4 preserves invalid editable state, derives prepared revision, automatically
converges latest valid state and retires old execution safely (Q-018). Save persists invalid canonical
intent; reopen does not resurrect a second permanent last-valid graph. Offline freezes/prepares that
canonical dependency scope and blocks invalid state. No queue/compiler/refcount/thread primitive is
selected. A source/target deleted or moved revalidates relationships, never retargets by name/position.

**N — Extensions/host services.** R3 host RU/EN, semantic Dark/Light, preferences and fallback exist
without R6 dynamic hosting. R6 adds only actual first-party contribution registration/availability,
compatibility and safe retirement; R7 consumes it. Resource translation absence alone cannot fail
executable compatibility, and unload cannot leave UI callbacks to absent code or erase document state.
Q-010/Q-024/Q-054/Q-055/Q-067 are stage-local bounded mechanisms. No CLAP/VST3 bridge, external
editor embedding or full public realtime SDK is needed to implement an async generator package.
Missing opaque node/capability preservation can be checked with controlled model fixtures rather
than shipping an external host solely for the audit scenario.

**O — UI/keyboard.** R3 supplies focus/selection/action-target distinction, window/pane reachability,
escape and safe layout/preferences. R4 graph, R5 steps, R7 audition/acceptance, R9 notes, R10 clips and
R11 controls each own discoverable pointer and corresponding useful keyboard movement/action access.
Canonical transactions originate in R1; preview versus commit and held-key versus discrete Undo
grouping are tested with the actual editor, not deferred to R12. Text/numeric/search focus must not
trigger unrelated destructive commands. Accessible ordinary move/resize/minimize/maximize/close
actions accompany custom chrome from R3. Q-064 retains bindings/navigation/API/screen-reader/DPI/
platform evidence; no pixel-perfect framework or accessibility certification is selected here.
On-screen simple audition belongs to R5's first musical integration/R9 pitch editor over R2, not a
recording or global typing-keyboard prerequisite. Mature rebinding UI remains a later design choice.

**P — First Track export.** The path is canonical revision → required closure (events, occurrence/time
maps, media, processors and any supported external dependencies) → validation/preparation → scheduling/
DSP in a declared rate/channel context → intended project/local cuts and finite scoped tails → bounded
WAV delivery/error/cancel. R0 compares only a small execution equivalent; R4 graph comparison guards
the sibling offline path; R8 realizes production scope/ranges/media acceptance; R10 adds arrangement/
stretch; R11 adds common DSP/routes; R12 owns final whole-project/range controls/output delivery.
Output sample rate/channels follow declared rendering intent, not whichever device happens to be open.
Sample pitch-rate conversion and audio stretch must be valid for that context. File delivery failure
must not report a usable complete output; cancellation/partial output retains an explicit owner and
does not mutate the project or corrupt a pre-existing destination. Exact replacement UX is bounded
R12 design. Invalid canonical processing cannot export last-valid realtime audio; absent device alone
does not block offline processing. Hard selected range is default, permitted tails are finite and
cannot restore deliberate cuts; specific inclusion UX/thresholds stay Q-057/Q-012.

**Q — Platform-scoped release.** R20 remains binding: Windows/Linux/macOS architecture, early hosted
evidence and declared actual supported delivery are distinct. R0 validates its named environment,
R2 adapters/resources, R3+ GUI; R12 declares distribution targets and requires actual delivered launch/
native dependencies as applicable, GUI/input/DPI, intended audio and project/media/recovery evidence
for each. No simultaneous parity, exact CPU/RID/device/OS/toolchain matrix or arbitrary Linux/macOS
deferral. Q-041 resolves the actual claim with evidence; WSLg never substitutes for native Linux
desktop/device/distribution acceptance. Broader coverage may ship whenever justified.

**R — Optional/future scope.** Full MIDI/audio recording, monitoring, ASIO, CLAP/VST3, mature sidechain/
modulation, extensive automation, Personal Library/preset publication, marketplace, every codec,
exhaustive DSP/graph inventory, advanced monitoring and complete theme customization do not make this
small track possible in a way the core sample path cannot. They remain R13 lifecycle or R14+ expansion.
Early models leave room for them; no empty implementation layers or full frameworks are mandated.
Explicit core buses/output and necessary timing for shipped DSP are earlier requirements, distinct from
rich sends and universal PDC. Q-048 event extraction is useful future depth, not needed when the track
uses an existing standalone audio clip for local processing. Q-049 baked replacement is conditional
on offering that operation; source-preserving R8 output suffices. R7/R8 remain planned stages with
their own gates even when unused by the core composition.

### Critical path and alleged cycles

The hard capability chain is durable musical/document identity and edits → durable samples/core source
events → prepared contribution-preserving graph → musical editing/loop evidence → arranged references/
audio-time/processing contexts → basic Mixer/core DSP → integrated canonical export/Save/recovery/
delivery. R0 is the evidence gate before committing the production execution boundary. R3 supplies UI
rails alongside the audio path; R6/R7 form an optional creative branch; R8 supplies early production
offline/render-acceptance evidence; R9 supplies pitched editing over existing sound/model.

Current stage order is a coherent delivery order, not one serial hard prerequisite chain. After a
minimum R1 model, shell geometry and pure resource/model design can proceed independently; source/
graph and shell-specific tests need different evidence. R9 editor work need not technically wait for
generator/resampling algorithms. R6 async package work need not implement every graph processor.
This audit preserves stage IDs/order; these independence observations authorize no parallel work or
stage bypass. No fundamental reorder/split recommendation is warranted.

| Alleged cycle                            | Actual direction / reason                                                                                                               | Classification / closure                                                                             |
|------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------|
| R2 resources ↔ R3 Browser/shell          | Import/preview are subsystem operations; a Browser invokes them later                                                                   | I presentation integration, no hard cycle; bounded R2 host/API evidence                              |
| R4 graph ↔ R5 musical playback           | R1 events/R2 sampler can drive graph without R5 editor; R5 needs that executable path                                                   | H R4 execution for R5, no reverse edge; synthetic/model events before UI                             |
| R7 contextual audition ↔ R10 Arrangement | Existing Pattern/sound context supplies meaningful audition; Arrangement expands target inventory later                                 | H existing R4/R5 context, F later contexts at R7; no fabricated Arrangement support                  |
| R8 render ↔ R10/R11 processing           | R8 renders Pattern/source and actual current local paths; timeline/effects extend renderer later                                        | I incremental renderer; unsupported required dependency blocks, no full Mixer gate                   |
| R1 identity ↔ R4 attachments             | Semantic identities in R1 constrain concrete R4 references; cross-owner design coordination is not runtime consumption of future graphs | Bounded design before freezing R1 reference shapes; no need for R4 execution in R1                   |
| R11 Mixer ↔ earlier routing              | R2 output/R4 routes exist before controls; R11 exposes/extends them                                                                     | H early separation, I later UI/DSP; no reverse engine dependency on Mixer pane                       |
| R12 recovery ↔ earlier media lifetime    | R1 revision/ownership and R2 retained durable media enable recovery; R12 adds storage/workflow evidence                                 | H lifetime before import/async reliance, I recovery completion; late UI cannot repair lost resources |

Measured gates sit on the path as capabilities arrive: R0 timing/lifetime feasibility; R2 tonal sampler/
storage/device; R4 publication and independent outputs; R5 loops/live transitions; R8 canonical render/
state/tails/async; R10 audible stretch/overlap; R11 DSP/alignment; R12 integrity and per-target delivery.
None is replaced by this conceptual audit. Their scope is bounded to shipped features, not every
future engine/backend/plugin. Open stage-local design does not justify indefinite documentation delay.

### Findings and smallest corrective actions

| Finding / what matters                                                                                           | Earliest need / bounded owner                                                                              | Classification                                                                | Smallest correction / existing answer                                                                                                                |
|------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| Production scheduler and offline path had implicit ownership; R12 could become first full renderer               | R2 events, R4 prepared schedule/comparison, R8 production Pattern render, R10/R11 extensions, R12 delivery | Manageable acceptance/design gap; H capability at consumers, E implementation | Assign incremental owners/closure in Roadmap; Audio already requires shared scheduling/DSP and device independence; Q-005/Q-006/Q-012/Q-018/Q-057    |
| Simple sampler explicitly promises pitch but completion did not demonstrate duration/release/velocity or overlap | R2 source behavior, R4 domains, before R5/R9                                                               | Ambiguous acceptance; H musical sound, E source/state                         | Clarify minimal pitched-note acceptance and short-source scenario; Q-047/Q-057/Q-005 own evidence; no new synthesizer                                |
| R1/R4 reference design must coordinate before a collapsed identity is persisted                                  | R1 bounded model, R4 attachments                                                                           | Manageable schema design, high rewrite impact                                 | Existing separate-sharing contracts already answer semantics; add model examples and coordination, Q-008/Q-009/Q-019/Q-029                           |
| R7/R8 wording could assume future context/scopes                                                                 | R7 Pattern/sound audition, R8 Pattern-content render                                                       | Unsupported assumed capability if not bounded                                 | Name existing targets and explicit blockers; Sample workflow already distinguishes object scopes; Q-011/Q-012/Q-066                                  |
| R10 “not complete stretch engine” versus later “time stretching” may hide missing basic audible operation        | R10 before timeline completion, R12 supported scenario                                                     | Ambiguous boundary; H bounded audio-time behavior, E algorithm                | State basic audible stretch/fixed-follow modes remain R10; rich tools R14+; Q-051 already identifies gap                                             |
| Alignment/state correctness could be postponed with universal PDC/Live mode                                      | R4 first relevant path; R8/R10/R11 supported timing                                                        | E question; H correct shipped relationships                                   | Require minimum supported-path timing or explicit narrower support/blocker; Q-021/Q-047/Q-057 already own mechanisms, no universal compensation gate |
| Media lifetime/Save reliability could be interpreted as final-release-only polish                                | R1 Save/edit identity, R2 durable ownership; R7/R8 consumers; R12 recovery UX                              | H early integrity; manageable mechanism/E evidence                            | Clarify earliest applicable owners, allow conservative retention; Q-009/Q-058/Q-059/Q-063 and existing failure matrix answer semantics               |
| UI command/focus/history may be patched after panes exist                                                        | R3 rails; each R4/R5/R7/R9/R10/R11 editor                                                                  | Incremental foundation / stage-local design                                   | Explicit editor acceptance responsibility; existing UX/workspace/actions answer semantics, Q-063/Q-064 retain realization                            |
| Core effects and distinct routes must be available in local and Mixer workflows                                  | R4 separation, R10 local scopes, R11 EQ/compressor                                                         | H core capability, not optional plugin                                        | Clarify same processor/context path and core availability; existing graph/architecture/R11 already answer semantics, Q-019/Q-030/Q-047               |
| First Track acceptance lacked a named finished composition and failure/recovery/delivery proof path              | R12 direction; earlier component owners                                                                    | Planning gap partially resolved; E remains                                    | Add bounded scenario and ownership; Q-061 remains for concrete scope/evidence, Q-041/Q-058/Q-059 and subsystem questions unchanged                   |

No actual hard cycle, unowned required identity or fundamental sequence contradiction was found.
The material risks are bounded implementation design and evidence gaps, not a reason to manufacture
another KB stage. A failure discovered by later executable evidence can legitimately reopen ordering
or architecture review. Optional contexts/hosting/recording should not be promoted just to make the
roadmap look exhaustive. No new accepted technology decision or D-record follows from running an audit.

### Q-061 disposition, risks and next actions

Q-061 is **partially resolved at planning level**: current R0–R12 order is conceptually coherent,
the bounded finished-track scenario maps to named owners, no hard cycle/missing mandatory owner remains
after clarifications, and R13/R14+ expansion is not an implicit prerequisite. This accepted portion is
recorded here; the active question narrows to actual stage-specific choices/dependencies and cumulative
end-to-end evidence. It is not closed, moved wholly to resolved questions, or equated to full R12
specification/feasibility. Other Q-IDs retain their specialized mechanisms and measured scope.

Top architectural risks are: (1) R0 callback/control/lifetime feasibility and bounded overload; (2) identity/attachment
plus independently addressable overlapping source contributions/publication; (3) durable unnamed media, Save/recovery
ownership and ambiguous storage/async completion; (4) audible stretch/time rounding, DSP state/tails and minimum latency
alignment across realtime/offline; (5) actual usability and per-target delivered GUI/audio/integrity evidence. Their
impact is high, but
no numerical performance, data-loss or platform guarantee can be claimed before experiments.

Before corresponding implementation, select bounded representations where needed: R0 environment/
measurements and candidate boundary; R1 event/time/ID/schema/transaction scope; R2 managed-media/source
protocol and sampler behavior; R4 port/attachment/publication and supported topology; R3 framework/
focus/resources; R7 target/substitution/lifecycle; R8 render entry-state/range/dependencies; R10 time/
stretch mapping and R11 processor/alignment choices. These decisions occur at their actual need, not
all before R0. Q-046 ProjectStats sequencing and Q-070 passive newline discrepancy are not audio-probe
prerequisites; neither is changed by this documentation-only audit.

Recommended next actions, in dependency order:

1. **Documentation/planning:** separately approve the bounded R0 scope and choose its declared setup,
   comparisons/thresholds and reporting limits. No additional focused KB stage is presently justified.
2. **Technical probe, only after authorization:** execute R0 as already scoped, record Q-001–Q-007
   measurements and accept/reject/narrow the candidate. Do not treat a passing probe as production engine
   or three-platform certification; do not silently move experimental code into application ownership.
3. **Stage-local design:** before R1/R2 production commitments, reconcile Q-008/Q-009/Q-019/Q-029/Q-063
   reference/edit examples and Q-059 unnamed-media ownership with the probe disposition. Choose bounded
   shapes/protocols rather than a full R10/R11 schema/UI or speculative storage ecosystem.
4. **Bounded executable evidence at owning stages:** verify sampler overlap/release and prepared
   contribution/publication, then frozen Pattern render; assess Q-021/Q-047/Q-057 for actual DSP and
   Q-051 audible stretch before R10 completion. Add only focused probes needed by an undecidable choice.
5. **Cumulative release closure:** replay/extend the scenario as capabilities arrive; choose Q-041
   supported distribution environments and substantiate Save/repair/recovery/export/desktop/audio/
   delivered-artifact evidence before calling R12 complete. Keep Q-061 open until that closure is real.

Architecture is conceptually ready to scope the separately authorized R0 probe. This audit neither
authorizes/starts it nor establishes feasibility. No additional document-level blocker was identified.
All implementation IDs/order and the entire R0 section/scope are preserved. Detailed matrices remain
cold history; active Roadmap retains only current scenario/ownership and Known problems the unresolved
evidence. Validation for this checkpoint is documentation-only, as recorded in
[completed work](WORK_LOG.md#2026-10-08--seq-kb-r21).

## SEQ-R3-CLOSE — Workspace Shell Completion Audit

Date: 2026-10-09. Baseline: clean `master`, HEAD
`6eafa71b65248c048723fb4017f7334c566515eb`; R1/R2 complete locally, R3-F1 accepted.
The owner explicitly accepts F2's current bounded usability/interaction behavior and authorizes
completion audit rather than another UI polishing cycle. Decision: **R3 complete / local accepted-ready**.
No actual missing foundation prerequisite or blocking production defect is identified.

### Acceptance source and method

Audited the existing R3 stage text (retained unchanged in
[completed roadmap](ROADMAP.md#accepted-stage-requirements-retained-at-closure)), localization/theme
and initial focus/keyboard stage assignments, and active Workspace, UX, UI design, Architecture,
Settings, Project state, Portability and Test execution owners. Source and test inspection supplements
[F1 physical/visual evidence](../experiments/SEQ-R3-F1_REPORT.md) and
[F2 final correction evidence](../experiments/SEQ-R3-F2_REPORT.md#resize-hit-targets-and-boundary-arbitration).
Reports own observations, active owners contracts, and explicit owner acceptance the current F2
usability level. No absent future musical surface is represented as a functioning editor.

### Complete bounded requirement audit

| Requirement                                                   | Implementation and test evidence                                                                                                                                                                                                                                                                          | Physical evidence and bounded conclusion                                                                                                                                                                                                                  |
|---------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| One functional main window; safe OS behavior                  | Desktop App/MainWindow create one pristine document, preserve native chrome roles, use guarded Windows system menu; no TopMost/global hooks/foreground-forcing production code. Shell lifecycle tests                                                                                                     | F1 actual drag/border resize, minimize/maximize/restore, Alt+Space, Win+Up/Down, Alt+F4 and About return; completion checks retain no TopMost and working maximize/restore. Satisfied on evidenced Windows desktop                                        |
| Internal first-party panes, activation, focus and front order | WorkspaceState/Host/Pane distinguish stable type/instance IDs, active identity, focus and document targets; bounded permutation and retained views. WorkspaceStateTests cover overlap and 10,000 activations                                                                                              | F2 exposed-content activation, both front orders, reachable strip, content identity/focus. Satisfied for Inspector/Appearance, not a future registry                                                                                                      |
| Floating, movement and anchored resize                        | PaneGeometry/PaneGesture separate move, resize and reflow; four edges/four corners, fixed opposite anchors, saturation/reversal and cancellation. PaneGeometryTests use exact assertions                                                                                                                  | F2 final 525 checks include all directions, tolerance, normal/reduced sizes and cancellation. Completion adds direct floating/header/left/right/top anchor/reversal/rollback checks. Satisfied                                                            |
| Collapse, restore, hide and reopen retain content             | Host lazily creates controls until disposal; visibility changes do not replace them; state/traversal tests                                                                                                                                                                                                | F2 actual runtime identity and focus; completion repeats collapse/F6, Ctrl+W/final-pane focus, reopen/same instance. Satisfied within session; layout restores geometry across launches                                                                   |
| Bounded explicit docking; per-pane permission                 | Left/right single occupants, half-workspace limits, reversible floating placement, conflict displacement and independent allowed flag. State/layout tests                                                                                                                                                 | F2 both edges, conflict and permission actions; completion repeats left/right dock, independent permission/undock. Satisfied; no advanced groups or snapping required                                                                                     |
| Predictable hit targets; independent boundary ownership       | WorkspaceBoundaryResolver uses arranged visible geometry, control precedence, occlusion, distance/front ties; freezes identity before activation. 32 boundary cases plus chrome/geometry tests                                                                                                            | F2 final native cursor/tolerance/touching/gap/overlap/seam checks in both orders; completion verifies outward tolerance, dock cursor and unchanged neighbour. Satisfied; current docks are independent, not shared splitters                              |
| Keyboard access; safe focus return                            | Local F6/Ctrl+Tab, action-menu traversal, Ctrl+arrows/resize, Ctrl+W and Escape; live action/host fallback. State tests establish traversal; native control behavior separately evidenced                                                                                                                 | F1 Tab/Enter/Space/OS actions; F2 menus/movement/resize/restore/leave/About; completion verifies restore/close/reopen and live preference focus. Satisfied for shipped controls, not certification/native-editor hosting                                  |
| User layout persistence, restore and corruption safety        | Separate bounded version-1 store, adapted/clamped geometry, unknown/duplicate entry handling, original-byte preservation/write refusal on fallback, serialized/coalesced final writes. WorkspaceLayoutTests cover corrupt/future/partial/oversize/backup failure/shutdown; state tests cover early close  | F2 actual restart/reflow/permission restore and joined close; completion starts from existing persisted layout and observes real restored DIP bounds/no preview writes/normal close. Satisfied for normal-process policy, not crash/power-loss guarantees |
| RU/EN, Dark/Light and preference changes                      | Frozen host catalogs with per-resource/essential fallback, identical semantic roles/metrics, invariant numeric projection and independent preference file. HostPresentationTests/PreferenceStoreTests                                                                                                     | F1/F2 both themes/languages and retained controls; completion repeats RU/Light focus/runtime identity. Satisfied for shell; no contributor SDK implied                                                                                                    |
| Compact DAW-oriented workspace                                | 30/30/22-DIP frame, 28-DIP pane chrome/conditional shelf, thin semantic boundaries, local content scrolling, real Inspector/Appearance content; no fake music or dashboard                                                                                                                                | Recorded normal/reduced/maximized F1/F2 visual observations and explicit owner F1/F2 acceptance. Satisfied for current sparse shell; dense editor usability belongs to actual future surfaces                                                             |
| Separation from music, Undo, media and audio lifetimes        | Desktop references Core only; layout has no music/focus-control/device targets. WorkspaceAndPreferencesPreserveCanonicalHistorySaveMediaAndPendingImportAuthority performs real Save/prepared WAV acceptance and checks snapshots/generation/history/media; host tests separately check pending authority | F1/F2 and completion observe no Audio.Windows module. Source startup makes no audio/device/capture call. Satisfied; opening the shell is not physical audio acceptance                                                                                    |

### Completion verification and limits

Local Windows 11 x64 10.0.26300, one 1920x1080 display, actual window DPI 96, SDK 10.0.401 /
runtime 10.0.12 / Avalonia 12.1.3. Existing reports retain normal/reduced/maximized, RU/EN/Dark/Light
and both front-order evidence plus important failed intermediate runs; their GUI totals are historical
evidence, not counts newly rerun in this audit.

A further completion run passes **29 actual GUI checks** at 1100x750. Windows UI Automation reads real
control identity/focus/rectangles; user32 supplies pointer/keyboard input and native cursor/state reads.
Checks cover restored geometry, outward tolerance, anchored resize/reversal/cancellation, no preview
layout writes, movement, retained collapse/reopen instances, F6/Ctrl+W/focus return, live RU/Light,
independent dock resize/permission, maximize/restore, no TopMost/audio adapter and joined close.
An earlier attempt failed an inherited driver's normalization setup assertion when starting from the
existing saved arrangement. It is excluded from passed totals; it establishes no production defect.
The successful run uses actual restored rectangles and verifies transitions directly without requiring
that normalization setup. Source was unchanged. Four existing user configuration/previous files are
restored byte-for-byte after GUI exercises; no screenshot, raw log or new temporary artifact is retained.
Pre-existing ignored local drivers/captures are preserved, not adopted as permanent tooling or deleted.

After documentation changes, these commands pass:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
```

Zero warnings/errors; **428 passed, zero failures/skips**, all existing test source preserved.
Documentation checks cover relative targets/anchors, owner metadata, scope/status/decision separation,
roadmap/history placement and final whitespace/diff/index/untracked status.

### Remaining questions and disposition

No accepted requirement is edited to justify closure. The stage explicitly permits limited surfaces
and bounded docking and assigns minimum shell resource/focus rails; present implementation and physical
evidence cover them. The prior pending owner acceptance is now satisfied explicitly. A report's old
reference to remaining platform/accessibility/delivered-shell work does not redefine current stage
acceptance or turn every wider evidence tier into an R3 prerequisite.

Q-035 retains future editor targets/multiple panes; Q-053 broader settings/reset/migration;
Q-054/Q-055/Q-067 contributor resources/editor presentation; Q-064 broader input, native-editor return,
screen readers and high/mixed-DPI/platform evidence; Q-041 release environment/delivery; Q-061 cumulative
First Track closure. Advanced docking groups/shared splitters/magnetic alignment are future scoped
capabilities, not blocking defects in current independent docks. Linux/macOS remain architectural
targets without runtime acceptance; no accessibility certification or packaged release is claimed.
Musical editors, graph execution and R4 implementation remain pending separate authorization.

Current state is compacted with detail retained in owners/reports; stage completion and rationale enter
cold history. Unique F1/F2 failures, cursor/geometry findings and R0/R2 measurements are preserved.
Only documentation changes; no commit/push, staging-index change or production/test mutation.
