# Product versioning

Role: Canonical product identity and checkpoint policy.
Read when: Changing production/test/product resource files, choosing a version checkpoint, or displaying runtime identity.
Authoritative for: Product syntax, component authority, checkpoint gates, MSBuild source, runtime display, and version ledger.
Not authoritative for: Roadmap acceptance, project format/schema, SDK/dependencies, ABI, devices, packaging, or release publication.

## Syntax and representations

The product version is `MAJOR.MINOR.PATCH.BUILD`. Display BUILD is exactly four ASCII digits,
`0000..9999`; other components are canonical nonnegative decimal integers without leading zeros.
Numeric CLR metadata uses the same four components without padding: `0.1.0.0027` maps to `0.1.0.27`.
MAJOR/MINOR/PATCH must fit CLR metadata (`0..65534`); BUILD has the stricter product limit.
Overflow requires a separate owner decision, with no automatic rollover/wrapping. Neither prerelease
labels nor `+SHA` are part of human-readable product identity.

## Authority and change gates

| Component | Authority | Meaning and evidence |
| --- | --- | --- |
| MAJOR | Project owner only | A major mature product generation or explicit release milestone. Never automatically assign `1.0`. |
| MINOR | Project owner only | A major product/roadmap line with a substantially new capability set. Codex may propose, never independently apply. |
| PATCH | Owner; Codex under the gates below | A completed, verified substantial standalone user capability or major functional block, judged by product capability rather than diff size or commits. |
| BUILD | Codex under the gates below | One completed, verified checkpoint with substantial changes to compiled production/test files or product resources within the current capability line. |

PATCH requires a serious demonstrated capability change: for example, a first complete user editor,
a finished Pattern Workspace, or another independently significant music-creation workflow.
Applicable deterministic and GUI/device checks must be complete. Explicitly record the capability,
evidence, reset, and decision in this ledger and the closing report. A dialog, button, cosmetic
improvement, several fixes, or unfinished stage does not qualify. Roadmap stage IDs are independent
of PATCH. If the scale is debatable, retain the current line and propose the change to the owner.
Delegated PATCH authority grants neither MAJOR/MINOR authority nor permission to start later stages.

BUILD may cover bug fixes, interface refinements, meaningful technical improvements, and new compiled
regressions. It is never a compile or Git commit counter. Do not increment for intermediate edits,
failed/incomplete attempts, ordinary `dotnet build`, pure documentation/research, experiments that do
not change the product, or report corrections. Classify the final checkpoint, not each file.
Version edits alone do not create a checkpoint. Commit/tag/release authorization is separate.

One completed checkpoint consumes at most one version change. On MAJOR increase, reset subordinate
components; on MINOR increase, reset PATCH and BUILD; on PATCH increase, reset BUILD to `0000`.
Do not add another BUILD bump in that task. Do not reuse an earlier accepted identity.
Acceptance/evidence gaps cannot be resolved merely by assigning a version.

Future tasks inspect the current source/ledger, classify the completed change, and retain the version
if no gate applies. Otherwise apply the authorized component/reset once, validate, and append one
ledger entry with checkpoint ID, previous/new identity, capability or correction, evidence and limits.
If verification fails, finish the checkpoint before claiming its new identity as accepted.

## Storage and runtime display

Root [Directory.Build.props](../Directory.Build.props) defines exactly four numeric components:
`SeqviumVersionMajor`, `SeqviumVersionMinor`, `SeqviumVersionPatch`, and `SeqviumVersionBuild`.
`SeqviumDisplayVersion` pads BUILD automatically; `SeqviumNumericVersion` derives its numeric equivalent.
Change components only under the gates above. Verification-only MSBuild global-property overrides
do not change the canonical source or ledger.

Production identity applies explicitly to `src/Seqvium.Core`, `src/Seqvium.Audio.Windows`, and
`src/Seqvium.Desktop`, matching both project name and location. Tests, DeviceCheck, unrelated artifacts,
and the independent R0 probe retain their SDK/default identity. Future production assemblies require
an explicit scope update; a tool does not acquire product identity merely by its project name.

| MSBuild property | Derived value |
| --- | --- |
| `SeqviumDisplayVersion`, `InformationalVersion` | Padded display representation |
| `SeqviumNumericVersion`, `Version`, `AssemblyVersion`, `FileVersion` | Numeric four-component representation |
| `VersionPrefix` | Numeric MAJOR.MINOR.PATCH |
| `IncludeSourceRevisionInInformationalVersion` | `false` for production |

SDK/NuGet `Version` uses numeric representation because package tooling may normalize leading zeros.
CLR attributes are assigned explicitly rather than inferred from `Version`; informational identity
preserves padding. This convention does not declare SemVer compatibility or introduce a package/
release system. No versioning dependency, Git-derived counter, bump-on-build automation, packaging,
or release workflow is added. The validation target rejects invalid components and independent
metadata overrides before assembly generation/compilation.

The existing About flyout binds to
[ShellSession.Version](../src/Seqvium.Desktop/Presentation/ShellSession.cs), which reads the running
Desktop assembly's `AssemblyInformationalVersionAttribute`. It stores no second version and refuses
missing metadata rather than inventing an identity. RU/EN and Dark/Light do not affect the value.
Toolbar/dialogs gain no permanent version labels. Future diagnostics/distribution identity must
derive from this source or runtime metadata rather than independent constants.

## Initial alpha basis and historical audit

The owner explicitly selects **`0.1.0.0000`** as the first alpha baseline. R1–R3 provide a working
canonical musical model, WAV import, durable project lifecycle, offline/realtime sampler, and
first-party Avalonia Desktop. Current R4-F1/F2/F3-A/F3-B add real editable Source/Gain/Mix/Output
graphs, independent two-source Mix, Undo/Redo, Save/reopen, and bounded GUI/audio smoke evidence.
This is a substantial implemented foundation for musical work, with further roadmap capability ahead.

R4 remains in progress/partial. R4-F2 physical realtime acceptance remains partial with unresolved
starvation evidence. F3-A is conditionally accepted for continued development; F3-B owner canvas/
interaction visual acceptance remains pending. Alpha identity claims neither complete R4, stable ABI,
public/production release, nor resolved starvation. [Current state](PROJECT_STATE.md) owns status.

Read-only Git audit baseline: `master`, `ba4b98808495d19ee520852573484a88ca7ba86e`.
The following unique Git revisions describe actual development, not retrospectively numbered versions.
Classification uses changed paths as well as commit subjects. Accompanying tests/evidence do not
create separate milestones. Bounded local accepted-ready scope differs from full acceptance.

| Exact Git references / period | Development and acceptance distinction |
| --- | --- |
| KB-R0–R21: `ccba50f` through `edad470` (2026-10-06–08) | Documentation/KB, architecture/UX contracts, roadmap, governance and repository policy; no compiled production capability or numbered product checkpoint. |
| R0: `aa8d7dc` | Isolated managed/native realtime experiment and evidence; narrow/partial, no permanent production engine/ABI selection. |
| R1: `43d613a` | Core canonical music/documents/time/history/persistence, deterministic tests and build foundation; bounded local accepted-ready R1. |
| R2-PRE: `61d93d5` | Documentation/evidence review of architecture boundaries, not a new product capability. |
| R2-F1: `0ab5513`; organization `c7b912a`; decode fix `4b709df` | Durable WAV/media and offline Core sampler plus tests; source organization and verified redundant-decode correction remain within that foundation. |
| R2-F2/F3/F4: `f9c04bb`, `9c48322`, `95888ae` | Core realtime execution, Windows adapter/harness, transient audition/reuse and independent endpoint intent; tests and distinct physical evidence. R2 is bounded local accepted-ready, not universal realtime/platform acceptance. |
| R3-F1/F2: `88f5389`, `db51a22`; fixes `f20b44f`, `6eafa71`; closure `6d29ebc` | Desktop, RU/EN/themes, workspace/layout and regressions. Documentation-only closure records owner acceptance of bounded R3, not new compiled functionality. |
| R4-PRE: `c7b056f` | Accepted bounded graph implementation strategy in documentation, not implementation or completed R4. |
| R4-F1: `5b34d5b`; guard `95cf3b9` | Core graph intent/diagnostics/edits and graph-aware JSON gates with tests; bounded local accepted-ready F1. |
| R4-F2: `e6e258d`; guard/investigation `5dcd10b`; evidence `021f839` | Core graph PCM/convergence, adapter/tests/harness; final scheduler evidence commit is documentation-only. Functional implementation exists, physical acceptance remains partial. |
| R4-F3-A: `b4ac44a`; startup fix `836fcfc` | Desktop import/transport/document workflow and tested partial-startup ownership correction; conditional owner acceptance for continued development. |
| R4-F3-B: `f8ce9bd`; crossing fix `ba4b988` | Editable Desktop graph/two-source workflow, small canonical boundary operation, tests and GUI/audio evidence; nearest-cable correction brings the prior suite to 732. Visual owner acceptance is pending. |

Historical R0–R4 commits do not acquire fictional `0.0.0.0001` and later identities. The former
Desktop-only `0.0.1-dev` was temporary metadata, not an accepted product version ledger.
Git owns source history; detailed work/decisions remain in the [cold archive](archive/INDEX.md),
retrieved only for explicit historical tasks.

## Version ledger

| Version | Checkpoint | Decision, evidence and limits |
| --- | --- | --- |
| `0.1.0.0000` | SEQ-VERSIONING-R1 | Explicit owner-selected initial alpha after the implemented foundations audited above; replaces temporary Desktop identity. Canonical/evaluated MSBuild, Release binary, About and regression checks are owned by [test execution](TEST_EXECUTION.md#product-version-verification). Does not close R4/F2/F3-B acceptance or announce a release. |
| `0.1.0.0001` | SEQ-R4-F3-B-UX-F1 | One completed Desktop behavior repair: context-request menu lifecycle, capture ownership, usable button-anchored node creation and stale-action guards. Locked restore, full Release build (zero warnings/errors), 867 passing tests preserving all 838 prior cases, and guarded Windows native RU/EN × Dark/Light menu/creation/Undo/keyboard evidence under the [F3-B report](experiments/SEQ-R4-F3-B_REPORT.md#context-menu-lifecycle-and-node-creation-repair). BUILD only; PATCH stays 0, no new musical capability, F3-B owner acceptance pending, F2 partial, no release/commit/tag. |
| `0.1.0.0002` | SEQ-NODE-EDITOR-IMPL-F1 | One verified Desktop interface/architecture checkpoint within the existing Source/Gain/Mix/Output capability line: reusable retained node editor, canonical Gain gestures, lifecycle guards, structural details and status economy. BUILD is selected for this bounded extension of existing attenuation/routing, rather than claiming a completed independent musical editor/processing capability for PATCH. Locked restore, zero-warning Release build and 908 passing tests preserve all 867 baseline cases. Actual Windows RU/EN × Dark/Light ordinary/reduced render/lifecycle and empty-state checks, guarded native opening/menu/drag checks, and separately labelled routed Gain/capture tests are recorded in the [report](experiments/SEQ-NODE-EDITOR-IMPL-F1_REPORT.md). Final native Gain input was refused on foreground loss; physical audio, full native matrix and broader platform acceptance are not claimed. PATCH remains 0; F2 partial, F3-B pending owner review, Q-032 open. No release/commit/tag. |
| `0.1.0.0003` | SEQ-DESKTOP-FOUNDATIONS-F1 | One BUILD for verified Workspace input/activation, Design System v1/field alignment, Desktop Single Instance and reusable visual/process checks within existing musical capabilities. Locked restore, zero-warning ordinary/isolated Release builds, all 922 tests (908 baseline plus 14), actual RU/EN × Dark/Light renders/alignment/resource propagation and independent Windows activation/crash/timeout evidence are in the [report](experiments/SEQ-DESKTOP-FOUNDATIONS-F1_REPORT.md). A user-held apphost required temporary isolated output; the user session was preserved and normal output later built cleanly. Forty guarded Windows screen captures and the early five-check native subset pass; final `native-complete/matrix.json` confirms eleven native mouse/keyboard checks. The full native matrix remains incomplete after earlier guard refusals, and routed events are not native input. PATCH remains 0; F2 partial, F3-B/visual owner acceptance pending, Q-032/Q-055 open. No intermediate bump, release, commit, push or tag. |
| `0.1.0.0004` | SEQ-R5-F1 | Verified finite Channel Rack/16-step musical editor, safe existing Mix composition and Save/Open. BUILD decision/evidence/limits are recorded in the [checkpoint](#seq-r5-f1-checkpoint) below; R5 remains partial. |

### SEQ-R5-F1 checkpoint

Product identity advances once from **`0.1.0.0003` to `0.1.0.0004`**. BUILD classifies a verified
finite musical editor checkpoint: empty Beat/independent WAV channels/canonical 16-step edits, safe
existing Mix composition, one-bar Play and durable Save/Open. This is a real music-creation workflow,
but it does not finish Pattern Workspace: continuous loop/live continuity remain F2/F3. The scale
and remaining native-input/picker/acoustic evidence make a delegated PATCH claim debatable; retain
PATCH 0 rather than interpreting the new grid as full R5 acceptance. MAJOR/MINOR stay owner-selected.

Affected Release builds pass with zero warnings/errors; 380 focused L1/L2 cases and four changed/new
boundary cases pass (383 distinct), including built metadata/About identity checks. Eight actual
RU/EN × Dark/Light normal/reduced renders and controlled actions pass. Native click smoke refuses on
foreground; no physical listening/device acceptance, full suite/native matrix or release is claimed.
[Test execution](TEST_EXECUTION.md#r5-f1-step-sequencer-verification) and
[completed work](archive/WORK_LOG.md#seq-r5-f1--2026-10-13) own scope/timings. Format/reader markers are
unchanged. L3 cadence advances to two completed tasks; version does not reset it. No staging, commit,
push, tag or intermediate version bump occurred.

Initialization stayed at BUILD `0000`, including its versioning implementation/tests. The separate
verified Desktop repair consumes `.0001` and the bounded editor checkpoint consumes `.0002`;
intermediate edits/builds consume no increments.

### SEQ-PROJECT-STATS-R1 tooling checkpoint

Product identity remains **`0.1.0.0000`** under the owner's explicit tooling-only task direction.
ProjectStats is an autonomous developer tool, not a new musical Seqvium capability. Production
assemblies/resources are unchanged; the CLI reports its own SDK-default assembly version, without
another version source. Locked Release verification and all prior/new tests are recorded in
[test execution](TEST_EXECUTION.md#projectstats-verification). No PATCH or disputed BUILD is assigned.

The current BUILD gate includes substantial compiled test changes, so tooling-only regressions in
the main Tests project create a policy ambiguity. **Proposed clarification, pending owner review:**
"A checkpoint confined to autonomous developer tooling and tests solely for that tooling does not
consume product BUILD when production code and product resources are unchanged." This proposal is
not a blanket accepted amendment to the gate; the retained identity above follows this task's scope.

## Compatibility boundaries

Product version does not determine `.seqvium` format acceptance, graph schema/node state versions,
extension ABI, .NET SDK/runtime, dependencies/packages, device IDs, or audio format.
[Project format](PROJECT_FORMAT.md) retains independent major/minor/minimum-reader gates and unknown
data preservation. The codec writer marker `0.2.0-r2-f1` identifies its implementation lineage,
not the current product. This checkpoint leaves writer metadata, JSON shape, reader 1.1 requirements,
canonical/document identity and persistence semantics unchanged. Future codec changes require an
independent compatibility decision; product PATCH/BUILD are never reader gates.

### SEQ-DESKTOP-FOUNDATIONS-F1-FINALIZE

Identity remains **`0.1.0.0003`**. The original F1 evidence is synchronized from retained JSON/TRX;
this checkpoint changes only documentation, developer verification/retention and its tests. Production
code/resources are unchanged. The owner's explicit tooling-only direction excludes an automatic
BUILD for these regressions. Reliability/cleanup verification is recorded in the
[foundation report](experiments/SEQ-DESKTOP-FOUNDATIONS-F1_REPORT.md#completion-and-reliability-audit).
R4-F2 remains partial; F3-B/visual owner acceptance, full native matrix and physical audio remain separate.

### SEQ-TEST-STRATEGY-R1

Identity remains **`0.1.0.0003`** under the owner's explicit instruction against an automatic version
increase. This is a developer tooling/test-policy checkpoint: VisualCheck retention deadlines and
cleanup-lock contention plus three focused regressions; production assemblies/resources, DSP and
project format are unchanged. It adds no standalone user capability for PATCH and does not qualify
for MAJOR/MINOR. Compiled regression changes are within the existing BUILD wording, but this task's
explicit version constraint overrides delegated automatic BUILD authority; it does not adopt the
general tooling exemption proposed above. Focused verification and limits are recorded in
[completed work](archive/WORK_LOG.md#seq-test-strategy-r1--2026-10-13). Version changes do not trigger
L3 independently of the risk/cadence policy in TEST_EXECUTION.
