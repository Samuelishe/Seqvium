# Product versioning

Role: Canonical product identity and checkpoint policy.
Read when: Changing production/test/product resource files, choosing a version checkpoint, or displaying runtime
identity.
Authoritative for: Product syntax, component authority, checkpoint gates, MSBuild source, runtime display, and version
ledger.
Not authoritative for: Roadmap acceptance, project format/schema, SDK/dependencies, ABI, devices, packaging, or release
publication.

## Syntax and representations

The product version is `MAJOR.MINOR.PATCH.BUILD`. Display BUILD is exactly four ASCII digits,
`0000..9999`; other components are canonical nonnegative decimal integers without leading zeros.
Numeric CLR metadata uses the same four components without padding: `0.1.0.0027` maps to `0.1.0.27`.
MAJOR/MINOR/PATCH must fit CLR metadata (`0..65534`); BUILD has the stricter product limit.
Overflow requires a separate owner decision, with no automatic rollover/wrapping. Neither prerelease
labels nor `+SHA` are part of human-readable product identity.

## Authority and change gates

| Component | Authority                          | Meaning and evidence                                                                                                                                   |
|-----------|------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------|
| MAJOR     | Project owner only                 | A major mature product generation or explicit release milestone. Never automatically assign `1.0`.                                                     |
| MINOR     | Project owner only                 | A major product/roadmap line with a substantially new capability set. Codex may propose, never independently apply.                                    |
| PATCH     | Owner; Codex under the gates below | A completed, verified substantial standalone user capability or major functional block, judged by product capability rather than diff size or commits. |
| BUILD     | Codex under the gates below        | One completed, verified checkpoint with substantial changes to compiled production/test files or product resources within the current capability line. |

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

| MSBuild property                                                     | Derived value                         |
|----------------------------------------------------------------------|---------------------------------------|
| `SeqviumDisplayVersion`, `InformationalVersion`                      | Padded display representation         |
| `SeqviumNumericVersion`, `Version`, `AssemblyVersion`, `FileVersion` | Numeric four-component representation |
| `VersionPrefix`                                                      | Numeric MAJOR.MINOR.PATCH             |
| `IncludeSourceRevisionInInformationalVersion`                        | `false` for production                |

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

| Exact Git references / period                                                 | Development and acceptance distinction                                                                                                                                                                                            |
|-------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| KB-R0–R21: `ccba50f` through `edad470` (2026-10-06–08)                        | Documentation/KB, architecture/UX contracts, roadmap, governance and repository policy; no compiled production capability or numbered product checkpoint.                                                                         |
| R0: `aa8d7dc`                                                                 | Isolated managed/native realtime experiment and evidence; narrow/partial, no permanent production engine/ABI selection.                                                                                                           |
| R1: `43d613a`                                                                 | Core canonical music/documents/time/history/persistence, deterministic tests and build foundation; bounded local accepted-ready R1.                                                                                               |
| R2-PRE: `61d93d5`                                                             | Documentation/evidence review of architecture boundaries, not a new product capability.                                                                                                                                           |
| R2-F1: `0ab5513`; organization `c7b912a`; decode fix `4b709df`                | Durable WAV/media and offline Core sampler plus tests; source organization and verified redundant-decode correction remain within that foundation.                                                                                |
| R2-F2/F3/F4: `f9c04bb`, `9c48322`, `95888ae`                                  | Core realtime execution, Windows adapter/harness, transient audition/reuse and independent endpoint intent; tests and distinct physical evidence. R2 is bounded local accepted-ready, not universal realtime/platform acceptance. |
| R3-F1/F2: `88f5389`, `db51a22`; fixes `f20b44f`, `6eafa71`; closure `6d29ebc` | Desktop, RU/EN/themes, workspace/layout and regressions. Documentation-only closure records owner acceptance of bounded R3, not new compiled functionality.                                                                       |
| R4-PRE: `c7b056f`                                                             | Accepted bounded graph implementation strategy in documentation, not implementation or completed R4.                                                                                                                              |
| R4-F1: `5b34d5b`; guard `95cf3b9`                                             | Core graph intent/diagnostics/edits and graph-aware JSON gates with tests; bounded local accepted-ready F1.                                                                                                                       |
| R4-F2: `e6e258d`; guard/investigation `5dcd10b`; evidence `021f839`           | Core graph PCM/convergence, adapter/tests/harness; final scheduler evidence commit is documentation-only. Functional implementation exists, physical acceptance remains partial.                                                  |
| R4-F3-A: `b4ac44a`; startup fix `836fcfc`                                     | Desktop import/transport/document workflow and tested partial-startup ownership correction; conditional owner acceptance for continued development.                                                                               |
| R4-F3-B: `f8ce9bd`; crossing fix `ba4b988`                                    | Editable Desktop graph/two-source workflow, small canonical boundary operation, tests and GUI/audio evidence; nearest-cable correction brings the prior suite to 732. Visual owner acceptance is pending.                         |

Historical R0–R4 commits do not acquire fictional `0.0.0.0001` and later identities. The former
Desktop-only `0.0.1-dev` was temporary metadata, not an accepted product version ledger.
Git owns source history; detailed work/decisions remain in the [cold archive](archive/INDEX.md),
retrieved only for explicit historical tasks.

## Version ledger

| Version      | Checkpoint                 | Decision, evidence and limits                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
|--------------|----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `0.1.0.0000` | SEQ-VERSIONING-R1          | Explicit owner-selected initial alpha after the implemented foundations audited above; replaces temporary Desktop identity. Canonical/evaluated MSBuild, Release binary, About and regression checks are owned by [test execution](TEST_EXECUTION.md#product-version-verification). Does not close R4/F2/F3-B acceptance or announce a release.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| `0.1.0.0001` | SEQ-R4-F3-B-UX-F1          | One completed Desktop behavior repair: context-request menu lifecycle, capture ownership, usable button-anchored node creation and stale-action guards. Locked restore, full Release build (zero warnings/errors), 867 passing tests preserving all 838 prior cases, and guarded Windows native RU/EN × Dark/Light menu/creation/Undo/keyboard evidence under the [F3-B report](experiments/SEQ-R4-F3-B_REPORT.md#context-menu-lifecycle-and-node-creation-repair). BUILD only; PATCH stays 0, no new musical capability, F3-B owner acceptance pending, F2 partial, no release/commit/tag.                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| `0.1.0.0002` | SEQ-NODE-EDITOR-IMPL-F1    | One verified Desktop interface/architecture checkpoint within the existing Source/Gain/Mix/Output capability line: reusable retained node editor, canonical Gain gestures, lifecycle guards, structural details and status economy. BUILD is selected for this bounded extension of existing attenuation/routing, rather than claiming a completed independent musical editor/processing capability for PATCH. Locked restore, zero-warning Release build and 908 passing tests preserve all 867 baseline cases. Actual Windows RU/EN × Dark/Light ordinary/reduced render/lifecycle and empty-state checks, guarded native opening/menu/drag checks, and separately labelled routed Gain/capture tests are recorded in the [report](experiments/SEQ-NODE-EDITOR-IMPL-F1_REPORT.md). Final native Gain input was refused on foreground loss; physical audio, full native matrix and broader platform acceptance are not claimed. PATCH remains 0; F2 partial, F3-B pending owner review, Q-032 open. No release/commit/tag.                              |
| `0.1.0.0003` | SEQ-DESKTOP-FOUNDATIONS-F1 | One BUILD for verified Workspace input/activation, Design System v1/field alignment, Desktop Single Instance and reusable visual/process checks within existing musical capabilities. Locked restore, zero-warning ordinary/isolated Release builds, all 922 tests (908 baseline plus 14), actual RU/EN × Dark/Light renders/alignment/resource propagation and independent Windows activation/crash/timeout evidence are in the [report](experiments/SEQ-DESKTOP-FOUNDATIONS-F1_REPORT.md). A user-held apphost required temporary isolated output; the user session was preserved and normal output later built cleanly. Forty guarded Windows screen captures and the early five-check native subset pass; final `native-complete/matrix.json` confirms eleven native mouse/keyboard checks. The full native matrix remains incomplete after earlier guard refusals, and routed events are not native input. PATCH remains 0; F2 partial, F3-B/visual owner acceptance pending, Q-032/Q-055 open. No intermediate bump, release, commit, push or tag. |
| `0.1.0.0004` | SEQ-R5-F1                  | Verified finite Channel Rack/16-step musical editor, safe existing Mix composition and Save/Open. BUILD decision/evidence/limits are recorded in the [checkpoint](#seq-r5-f1-checkpoint) below; R5 remains partial.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| `0.1.0.0005` | SEQ-UI-REDESIGN            | One verified BUILD for instrument UI v2, atomic row painting/actions, navigation/WAV drop, exact BPM and captured Gain dial; 435 focused tests plus final 82-case subset and actual Windows renders/guarded native checks. Existing finite musical capability, schema and DSP remain; R5 looping/live continuity and owner aesthetic acceptance stay separate. See the [report](experiments/SEQ-UI-REDESIGN_REPORT.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| `0.1.0.0006` | SEQ-R5-MUSIC               | Verified continuous Pattern loop/position/live notes, 1–32 bars, atomic deletion/navigation collapse and selected-Pattern WAV export; full 1009-case Release pass, actual-window checks and short physical loop evidence under the [checkpoint](#seq-r5-music-checkpoint). R5 and R4 pressure acceptance remain partial.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| `0.1.1.0000` | SEQ-R10-PLAYLIST           | One completed standalone Song composition capability: shared clips, simultaneous finite playback, real track Mute/Solo and whole-Song WAV, with compatible persistence and reference-informed editing/navigation. PATCH resets BUILD; zero-warning isolated full Release build, 1030 tests, actual-window RU/EN Dark/Light controls and short physical two-Pattern/live-edit output under the [report](experiments/SEQ-R10-PLAYLIST_REPORT.md). R5/R10 wider scope, owner visual acceptance and R4 pressure remain separate; no release/staging/commit/push.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| `0.1.1.0001` | SEQ-PLAYLIST-MUSIC         | One verified BUILD for composition navigation, range transport, group edits, workspace restoration and live channel/audio usability. Evidence and limits are in the [checkpoint](#seq-playlist-music-checkpoint) and [report](experiments/SEQ-PLAYLIST-MUSIC_REPORT.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| `0.1.1.0002` | SEQ-TRACK-GUI              | One verified BUILD for real composing workflow repairs, with native GUI track authoring and focused/rendered evidence under the [report](experiments/SEQ-TRACK-GUI_REPORT.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| `0.1.1.0003` | SEQ-HOME-RESOURCES         | One verified BUILD for Light musical color/material refinement and contextual Rack/Playlist help; private resource preparation consumes no separate version. Evidence and inherited harness limits are in the [report](experiments/SEQ-HOME-RESOURCES_REPORT.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| `0.1.2.0000` | SEQ-R9                     | One verified PATCH for a complete bounded shared Piano Roll, velocity/Root Pitch and real musical workflow. BUILD resets; full 1073-case Release pass, actual theme/field review, native composition and physical Pattern/Song/export evidence are scoped in the [checkpoint](#seq-r9-checkpoint) and [report](experiments/SEQ-R9_REPORT.md). No schema/dependency/release or prior pressure-acceptance claim.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| `0.1.3.0000` | SEQ-FIRST-DRUM             | One verified PATCH for the first built-in synthesized kick, full sound editor and Rack/Piano/Song/history/Save/Open/WAV workflow. BUILD resets; 1092-case full Release pass, actual Dark/Light/native/physical evidence and limits are in the [checkpoint](#seq-first-drum-checkpoint) and [report](experiments/SEQ-FIRST-DRUM_REPORT.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| `0.1.4.0000` | SEQ-DRUM-INSTRUMENTS       | One verified PATCH for seven distinct built-in drums, 21 presets, portable custom sounds, linked hats and native musical Save/Open/play/export. BUILD resets; full 1179-case Release pass and GUI/audio evidence are in the [report](experiments/SEQ-DRUM-INSTRUMENTS_REPORT.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| `0.1.4.0001` | SEQ-CHANNEL-RACK-MUSIC     | One BUILD for compact fixed Rack pads, preserved rich previews, navigator scrollbar gutter and unchanged Gain focus-loss repair. Native Neon Orchard authoring/reopen/full Song/WAV and bounded GUI/tests are in the [report](experiments/SEQ-CHANNEL-RACK-MUSIC_REPORT.md); no release or prior pressure acceptance follows.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| `0.1.5.0000` | SEQ-R11-MIXER              | One verified PATCH for canonical musical channels/buses/Master, amplification/balance, real meters, routes/sends, EQ/compression and compact studio instrument UI. BUILD resets; full 1196-case Release pass, native musical Save/Open/physical Song/WAV and same-rate PCM parity are scoped in the [checkpoint](#seq-r11-mixer-checkpoint) and [report](experiments/SEQ-R11-MIXER_REPORT.md). No release or prior pressure/platform/acoustic acceptance follows.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| `0.1.5.0001` | SEQ-NEON-ORCHARD           | One BUILD for the complete official editable demo, licensed managed media, frozen musical baseline and independent-copy verification. Zero-warning isolated Release build, 33 focused demo/metadata cases and native full Song/WAV pass; see the [checkpoint](#seq-neon-orchard-checkpoint). No engine stage, release, full regression or prior pressure/platform/acoustic acceptance claim.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| `0.1.6.0000` | SEQ-R10-AUDIO-CLIPS        | One PATCH for standalone fixed-speed Audio Clips with native import/editing, real waveforms, shared Mixer, portable history/media and Song playback/export. BUILD resets. Evidence and bounded resident/Windows-codec limits are in the [checkpoint](#seq-r10-audio-clips-checkpoint) and [report](experiments/SEQ-R10-AUDIO-CLIPS_REPORT.md); wider R10/pressure/platform/acoustic acceptance remains separate.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |

### SEQ-R10-AUDIO-CLIPS checkpoint

Identity advances once from **0.1.5.0001 to 0.1.6.0000** for the independently significant verified
standalone audio-arrangement workflow: native WAV/MP3/FLAC Playlist import, waveform/range/split/fades,
fixed-speed repeats, existing Mixer, history/portable media and mixed Song playback/export. PATCH
resets BUILD; MAJOR/MINOR remain owner-selected. Reader 1.4 is an independent essential-intent gate;
NAudio codec dependency and Windows-only compressed import are separately documented. Final Release,
native input/codec/cancel/portable presentation and full physical/offline/export evidence belong to
the [Audio Clip report](experiments/SEQ-R10-AUDIO-CLIPS_REPORT.md). This does not finish wider R10
stretch/container scope, streaming/surround support or prior pressure/acoustic/platform acceptance.
No release, staging, commit, push or tag occurs, and no intermediate BUILD is consumed.

### SEQ-NEON-ORCHARD checkpoint

Identity advances once from **0.1.5.0000 to 0.1.5.0001** for substantial new musical product
resources: a complete editable official demo with six redistributable managed WAVs, an immutable
publication snapshot and repeatable independent-copy verification. This is BUILD within the existing
instrument/Playlist/Mixer capability line, not a new engine block or PATCH. MAJOR/MINOR/PATCH and
project format remain unchanged; no new dependency or startup/demo-browser behavior is introduced.
The isolated zero-warning Release build and 33 passing demo/metadata cases establish L0/L1/L2.
Native independent-copy inspection and full physical Song/WAV establish the requested bounded L4;
the [test owner](TEST_EXECUTION.md#official-demo-verification) and
[musical checkpoint](../examples/Neon%20Orchard/checkpoints/README.md) record scope and limits.
L3 cadence advances to one completed task after Mixer; this version change does not trigger L3.
No staging, commit, push or tag is performed.

### SEQ-R11-MIXER checkpoint

Identity advances once from **0.1.4.0001 to 0.1.5.0000** under delegated PATCH authority for a
substantial standalone musical mixing capability: canonical source channels/buses/Master, amplification,
balance/Mute/Solo, real meters, routed pre/post sends, EQ/compression and shared realtime/offline DSP.
BUILD resets; MAJOR/MINOR retain owner-selected values. The [closing report](experiments/SEQ-R11-MIXER_REPORT.md)
records 1196 passing full-suite cases, zero-warning Release builds, compact RU/EN Dark/Light/native
history/field checks and GUI-authored Neon Orchard Save/Open/physical output/WAV. Submitted device PCM
matches same-rate offline exactly; native PCM16 export differs only by rounding. No acoustic, true-peak,
long-pressure, whole-interface aesthetic, other-platform or R4 acceptance is inferred. No release,
staging, commit, push or tag is authorized/performed. This completed checkpoint consumes no second BUILD.

### SEQ-CHANNEL-RACK-MUSIC checkpoint

Identity advances once from **0.1.4.0000 to 0.1.4.0001** for a bounded compiled UI/composing repair,
not a new editor subsystem or another substantial PATCH. All seven drums and five library samples
participate in a native-authored 68.571 s / 15-Pattern / nine-track composition. Reopen, full physical
Song and native WAV pass with zero saturated samples. The report distinguishes compact matrix and
field checks from early native failures and subjective/pressure limits; final Graph native smoke also passes.
L3 cadence advances to one completed task after the preceding full drum suite; BUILD does not reset it.

### SEQ-DRUM-INSTRUMENTS checkpoint

Identity advances once from **0.1.3.0000 to 0.1.4.0000** under delegated PATCH authority for a
complete media-free percussion workflow: six additional synthesized families, useful presets,
hat interaction and instrument-specific editing through shared Rack/Piano/Song/history/persistence.
BUILD resets; MAJOR/MINOR retain owner-selected values. Full Release passes 1179 cases;
the [report](experiments/SEQ-DRUM-INSTRUMENTS_REPORT.md) separates native musical authoring/pickers,
physical output and rendered matrix evidence from acoustic/pressure/platform limits.
No dependency/schema/plugin ABI or release acceptance change follows.

### SEQ-FIRST-DRUM checkpoint

Identity advances once from **0.1.2.0000 to 0.1.3.0000** under delegated PATCH authority for a
completed substantial standalone music-creation capability: built-in kick synthesis, directly editable
envelopes, meaningful parameters and existing note/Song/history/persistence/export integration.
BUILD resets to 0000; MAJOR/MINOR remain owner-selected. The final isolated Release solution build
has zero warnings/errors and all 1092 tests pass; actual RU/EN Dark/Light, guarded native dial/both
envelopes, physical audition/Pattern/Song and WAV evidence pass under the
[report](experiments/SEQ-FIRST-DRUM_REPORT.md). No package/ABI/dependency/schema change or release,
subjective-acoustic, long-pressure or prior R4 acceptance claim follows. No staging/commit/tag was done.

### SEQ-R9 checkpoint

Identity advances once from **`0.1.1.0003` to `0.1.2.0000`**. Delegated PATCH is justified by a
completed substantial standalone musical editor, not diff size: full note/pitch/duration/velocity
composition, shared Rack/Playlist integration, explicit source tuning, Undo/Save/Open and real
Pattern/Song playback/export. PATCH resets BUILD; MAJOR/MINOR remain owner-selected. Project schema/
reader/writer and dependencies are unchanged. Full Release build has zero warnings/errors and all
1073 tests pass. Actual RU/EN Dark/Light GUI, guarded native musical composition and short physical
execution with verified library roots are scoped in the [report](experiments/SEQ-R9_REPORT.md).
Advanced note/sampler features, aesthetic owner review, acoustic judgement and R4 pressure acceptance
remain separate. No release, staging, commit, push or tag; no intermediate BUILD increment.

### SEQ-HOME-RESOURCES checkpoint

Identity advances once from **`0.1.1.0002` to `0.1.1.0003`**. BUILD covers verified compiled
theme/control-economy refinement within existing editors. Private resources/documentation alone do
not qualify; no independent editor warrants PATCH. MAJOR/MINOR/PATCH, schema, DSP and dependencies
remain unchanged. Zero-warning affected Release build, 196 focused cases and actual component/
Playlist/field/editor review are scoped in the [report](experiments/SEQ-HOME-RESOURCES_REPORT.md).
The pre-existing Rack reduced-tempo harness failure and one retried Playlist input failure remain
reported, not weakened into passes. No full-suite rerun, complete Rack/native/audio acceptance,
release, staging, commit, push or tag.

### SEQ-TRACK-GUI checkpoint

Identity advances once from **`0.1.1.0001` to `0.1.1.0002`**. BUILD covers the verified GUI workflow
repairs found while authoring a real layered track: committed fractional Gain, anchored/filled Playlist
extent and all 32 lanes, usable corner targets and settled maximized pane restoration. No independent
editor, schema, dependency, DSP or product-line change warrants PATCH/MINOR. Focused checks and actual
native authoring, full finite playback and GUI export are scoped in the
[report](experiments/SEQ-TRACK-GUI_REPORT.md). No staging, commit, push, tag or release.

### SEQ-PLAYLIST-MUSIC checkpoint

Identity advances once from **`0.1.1.0000` to `0.1.1.0001`**. BUILD classifies a completed extension
of the existing Song/Rack workflow: seek/range loop, atomic clip groups, timeline navigation, restored
main window/music layout/resize access, continuous supported WAV channel changes, Play retry and WAV
compatibility/boundary smoothing. It does not declare a separate completed editor, R10 acceptance or
another product line. MAJOR/MINOR/PATCH, project reader markers and dependencies remain unchanged.

Early L3 is justified by shared realtime boundary and WAV decoding risk. The full 1045-case Release
pass, later focused regressions, actual production-window checks, read-only 271-WAV library audit and
short physical seek/range/channel/Undo/Redo output are recorded in the
[report](experiments/SEQ-PLAYLIST-MUSIC_REPORT.md). R4 pressure/acoustic/platform acceptance stays
separate. No staging, commit, push, tag or release occurs.

### SEQ-R5-MUSIC checkpoint

Identity advances once from **0.1.0.0005 to 0.1.0.0006**. BUILD classifies the verified continuous
Pattern/live-edit and editing/export extension within the owner-selected alpha line. R5 remains
partial without velocity/per-channel controls; Arrangement, simultaneous Patterns and processors
beyond Gain remain missing. Retain PATCH 0 rather than declaring a finished Pattern Workspace or
First Track checkpoint. MAJOR/MINOR and the independent format/reader/writer markers are unchanged.

Final full Release build has zero warnings/errors and all 1009 automated cases pass. Actual Windows
Rack/catalog/field matrix and bounded six-second WASAPI loop/live-edit/Stop/join pass under the
[report](experiments/SEQ-R5-MUSIC_REPORT.md). Acoustic listening, wider native picker/platform checks
and prior pressure acceptance remain separate. Early L3 is selected for shared audio-execution risk,
not the version increase; cadence resets to zero. No staging, commit, push, tag or release occurs.

### SEQ-UI-REDESIGN checkpoint

Identity advances once from **`0.1.0.0004` to `0.1.0.0005`**. BUILD classifies a substantial verified
interface/interaction improvement within the existing finite Pattern and Gain capability line.
It adds no independent completed R5 loop/live processing block for PATCH and does not settle visual
owner acceptance. No MAJOR/MINOR, format/reader/writer marker, dependency or DSP change occurs.
Final isolated Release builds pass with zero warnings/errors; built metadata/About identity is
included in 435 focused cases. Current report distinguishes native, routed, rendered and physical
evidence and records an output lock without terminating the user app. Cadence advances from two to
three; this BUILD does not trigger or replace L3. No intermediate bump, staging, commit, push or tag.

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
not the current product. The initial foundation retained reader 1.1 requirements; later Mixer and
Audio Clip checkpoints add independent essential-intent gates (currently reader 1.4). Writer lineage
does not substitute for those gates. Future codec changes require an
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
