# Test execution and verification

Role: Proportional verification, test quality, and evidence guide.
Read when: Planning, running, or interpreting verification and future tests.
Authoritative for: Test topology/quality, verification scope, future commands/platform discovery, Release gates, and
evidence tiers.
Not authoritative for: Product/subsystem contracts, SDK selection, CI triggers, supported release platforms, or current
progress.

## Sample Lab verification

`PercussionLabTests` measures finite bounded PCM, nontrivial transient/decay, different Random PCM,
family spectral distinction, normalized recipe variations, exact locks and bounded history disposal.
`SampleLabTests` covers host activation/use, selection/reference independence, standalone edits,
late/superseding/cancelled/New results, managed independent acceptance, captured/stale/deleted targets,
Undo/Redo, disabled-generator Save As/Open/export and Pattern/Song audition authority.
`SampleAuditionTests` compares contextual PCM exactly with accepted playback through local Gain and
Mixer balance/level, distinguishes raw PCM, and verifies unrelated sustained-voice/time continuation.
These checks establish numerical behavior, not acoustic preference or physical device quality.

Use focused `*PercussionLabTests`, `*SampleLabTests`, `*SampleAuditionTests` and affected extension,
workspace, media, transport and presentation cases. This stage selects L3 because prepared realtime
publication/lifetime changed. VisualCheck `sample-lab` and `ui-density` cover real retained windows;
[the tool guide](../tools/Seqvium.VisualCheck/README.md#sample-lab-and-ui-density) owns commands and input
modes. Native input, routed actions, deterministic processed PCM, physical output and subjective
listening remain distinct. See the [R7 report](experiments/SEQ-R7-SAMPLE-LAB-V1_REPORT.md).

## R6 extension verification

`ExtensionFoundationTests` exercises local manifest damage/version/path/missing/duplicate boundaries,
required-contract rejection without activation, old package versions with optional fallback,
manifest-file symlink rejection before metadata read (explicit skip if the platform denies symlink creation),
trusted factory registration, failed/timed-out/cancelled activation and retry/disposal failure. Explicit gates
cover active-use disable refusal, open-editor leases, serialized generation, cancellation, superseded
late outputs, invalid parameters/audio, changed document generations, New/application Close joins and
candidate disposal. Managed Rack/Playlist and generated acceptance cover one Undo, disable/source
removal, portable Save As/Open and nonzero export without the package. Existing persistence/graph
tests retain opaque missing-state and affected-execution blockers; no realtime plugin is implied.

Use focused `*ExtensionFoundationTests`, then affected `*SampleBrowserTests`, `*DesktopWorkflowTests`,
`*PersistenceTests`, `*ManagedMediaTests`, `*GraphDiagnosticTests` and `*WorkspaceStateTests` as needed.
The workspace regression covers opening the docked Browser before its first native arrange.
For this stage's lifecycle risk, L3 is additionally selected; see the
[R6 report](experiments/SEQ-R6-EXTENSION-FOUNDATION_REPORT.md).

VisualCheck `extensions <output> [native]` opens the real production Browser and shipped Found Sounds,
imports through existing buttons, disables/re-enables contributions and reopens with the pack disabled.
It renders RU/EN Dark/Light plus reduced disclosure/list/preview geometry. Routed controls, native
Windows input, decoded PCM and physical/acoustic evidence remain distinct. A failed native click
delivery is not a pass, even when the corresponding routed scenario succeeds.

## Pattern resampling

`PatternResamplingTests` checks an independent two-path signal/release oracle across mono/stereo and
44.1/48-kHz source rates, all seven synthesized one-shot tails, source-time-zero placement, frozen
state after edit/Close, packet cancellation, required dependencies, unavailable media, overload and
size refusal. `ResamplingWorkflowTests` covers real managed acceptance, source preservation, exact
PCM, one Undo/Redo, Rack/Playlist reuse, relocation with original media removed, durable write failure,
late uncooperative completion after Cancel/edit/delete/Undo-Redo/New/Open/Close and joined teardown.

```text
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*PatternResamplingTests" --filter-class "*ResamplingWorkflowTests" --max-threads 1
```

The R8 checkpoint requires full Release regression because it touches sampler preparation and media
acceptance. [VisualCheck resampling](../tools/Seqvium.VisualCheck/README.md#pattern-resampling) owns
real-window localized menus/Browser, native action input and physical source/sample output, with
each tier recorded separately. [Report](experiments/SEQ-R8_REPORT.md) owns actual results and limits.

## Global settings and command bindings

`ControlPreferencesTests` covers real resolver/default removal, explicit conflict transfer, reserved
system/editor/text keys, text-local Undo after remapping, separate file/previous bytes, unsafe-original
write refusal, language/theme/music independence and joined latest writes. Combine with
`PreferenceStoreTests`, `HostPresentationTests`, `WorkflowPresentationTests` and `*Workspace*` classes.
Use `--filter-class "*ControlPreferencesTests"` with SDK 10/xUnit MTP after the affected Release build.

[VisualCheck settings](../tools/Seqvium.VisualCheck/README.md#global-settings) owns real-window
RU/EN × Dark/Light, reduced settings/workspace, capture/conflicts/default/reset, file reload, current
command dispatch and title-row theme selection. GUI setup and combo fixture selection are distinct
from guarded native action input. Editor-local remapping and hardware/platform/DPI acceptance remain
outside this first stage. Current evidence belongs to PROJECT_STATE and the Settings report.

## Sample Browser verification

Cross-stage regressions additionally cover section-only Pattern/Song × Mixer publication without
preparation, actual section-swap scheduling, cold processed range loops at 44.1/48 kHz, failed-Save
Close playback, queued stale Browser acceptance after New and preview/history/portable-media leases.
The [audit](experiments/SEQ-CROSS-STAGE-AUDIT_REPORT.md) records the native/routed input split, inherited
allocation failure and process-snapshot isolation evidence. Use VisualCheck `cross-stage` for held
double-click/Panic and shortcut checks; `native` adds real row/whitespace drag. This substitutes the
held selection's output and editor-intent callback, and does not claim physical audio or exact Part choice.

`SampleBrowserTests` covers paged nonrecursive discovery/cancellation, transient preview and failed
replacement, playing-transport preservation, atomic empty Rack creation, managed WAV/audio reuse,
Save As/reopen without external sources, malformed location preservation, stale/cancelled/missing
reuse and callback rollback. Focus `*SampleBrowserTests` with affected `*WavAuditionTests`,
`*WavResourceReuseTests`, `*SourceAccessTests`, `*DesktopWorkflowTests`, `*AudioClipTests`,
`*StepSequencerTests` and workspace/design tests. Actual platform remains SDK 10/xUnit MTP.

VisualCheck `browser` renders RU/EN Dark/Light and reduced layouts, authors a small phrase and Audio
Clip through real routed controls, checks source drops and Undo, saves/reopens after removing originals.
`browser ... native` requires guarded Windows input; refusal is not a native pass. `browser ... audio
<codec-fixture-directory>` additionally exercises production physical output and Windows MP3/FLAC
decode/preview/import. [Tool guide](../tools/Seqvium.VisualCheck/README.md#sample-browser-check)
owns invocation; [report](experiments/SEQ-SAMPLE-BROWSER_REPORT.md) owns evidence and limits.

## Audio Clip verification

`SongStructureTests` covers non-overlap/reader gates and unknown fields, section swaps with gaps,
shared repeat insertion, crossing refusal, mixed clamping/clipboard geometry, and exact history.
Variation cases check declared graph identity remapping, source isolation, opaque-state refusal,
shared Mixer/resources, equal PCM before note changes and Save/Open after a local phrase change.
Allocation measurements exclude test assertions from the measured callback loop.
Use the focused `*SongStructureTests` plus affected `*ArrangementTests`, `*AudioClipTests`,
`*PersistenceTests`, `*GraphEditingTests`, `*DocumentTests` and `*DesktopWorkflowTests` classes;
`*ProductVersionTests` covers the distinct current reader/metadata gates.

The [VisualCheck song-structure route](../tools/Seqvium.VisualCheck/README.md#native-song-structure-check)
uses a caller-supplied independent composition and real audio source, production fields/pickers,
native pointer gestures, actual Rack/Piano targets and both-theme localized section alignment.
Foreground/hit-test refusal is a failed native attempt, never a passing mouse claim.

`AudioClipTests` covers independent native-frame/resampling/fade/repeat oracles, shared-source PCM
and Save As leases, range/history/reader/portable media, missing managed files, Mixer assignment,
source duration vs BPM, packet invariance, seek/loop allocation and compatible live-edit continuation.
Split regression cases include crossing fades, 44.1→48 kHz and nonzero musical placement.
`DesktopWorkflowTests` additionally verifies pristine audio-only import/play/save/reopen without
Patterns, Rack items or graph attachments. Codec error tests do not imply compressed decoding on
every Windows edition or another OS.

```text
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*AudioClipTests" --filter-class "*DesktopWorkflowTests"
```

Requested native musical evidence uses
the [VisualCheck Audio Clip routes](../tools/Seqvium.VisualCheck/README.md#native-audio-clip-check)
on independent demo copies and actual Explorer/pickers. Whole-Song submitted PCM/offline/native-WAV
comparison uses read-only `DeviceCheck mixer-project`; native GUI remains the author/export owner.
The [report](experiments/SEQ-R10-AUDIO-CLIPS_REPORT.md) separates deterministic, rendered, native,
physical and acoustic claim limits, including failed verification attempts and their fixes.

## Official demo verification

`OfficialDemoTests` uses build-copied repository assets, never a private library or Desktop folder.
It relocates the current project and publication-01 into fresh Temp directories with their required
media, verifies the inventory hashes/licenses, decodes all resources, renders every Pattern with
finite nonzero PCM, and checks canonical Save As/reopen plus full Song PCM16 export without clipping.
Frozen baseline counts/duration are checked only for the checkpoint; the main demo can grow.

```powershell
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*OfficialDemoTests" --filter-class "*ProductVersionTests"
```

If an open user Desktop locks the ordinary outputs, use `--artifacts-path .artifacts/neon-orchard/build`
on both commands; initially build with `-p:RestoreLockedMode=true` instead of `--no-restore` for
that new output tree. Never close/kill user work to make the build pass. The focused demo/metadata
run passes 33 cases in 18.025 s; an isolated incremental Release build takes 1.79 s, zero warnings/errors.

Native `VisualCheck native-mixer <output> <independent-project> inspect` opens through the real
picker, selects all Patterns, opens drum/Source and Piano editors, then checks Playlist and Mixer
with foreground-guarded Windows input and captures. `play` checks full finite physical Song, real
meters, native WAV export and Save. Both use the real production Desktop with an isolated user
configuration and only explicitly supplied
**copies**. [VisualCheck](../tools/Seqvium.VisualCheck/README.md#official-demo-inspection)
owns invocation details. A normal clone needs neither tool to open the demo.

The bounded Windows UR12/native evidence establishes relocation/execution/export; it does not
establish acoustic approval, other-platform support, heavy-load stability or a full regression pass.

## Mixer verification

`MixerTests` protects legacy unity, amplification/balance/Mute/Solo, aggregate channel compression,
actual EQ calibration, bus/send/cycle semantics, unsupported-state preservation/refusal, Save/Open,
Undo/Redo, partition equivalence, zero callback allocation, fast publication/Stop and restart after
updated coefficients. Use the installed xUnit MTP class filter `--filter-class "*MixerTests"`.
Related persistence, graph, arrangement, sampler, drum and Desktop classes protect the shared boundaries.
Reader/DSP/shared style changes justify L3 independently of the ordinary cadence.

Native musical edits/export and the separately read-only physical PCM diagnostic have different
purposes. Commands/scope are in [VisualCheck](../tools/Seqvium.VisualCheck/README.md#native-mixer-check).
Compare realtime/offline at the actual device rate; compare the native PCM16 export at its export rate.
Measured signal/parity and physical output do not establish subjective acoustic quality, true peaks,
long-pressure stability or another OS. [Report](experiments/SEQ-R11-MIXER_REPORT.md) records completed
checks, failures and limitations. Shared density/material changes require the existing catalog/matrix
plus actual populated Mixer/Playlist/Rack captures, not just geometry constants.

## Drum Voice verification

`rack-pads` verifies fixed square geometry/scroll reachability at three pane widths in RU/EN ×
Dark/Light and native row painting, Undo/Redo and real playing-column rendering. `native-track`
authors/reopens/balances/arranges/plays/exports the Neon Orchard score through native Windows input,
not project JSON or musical APIs. [Report](experiments/SEQ-CHANNEL-RACK-MUSIC_REPORT.md) owns its
measurements and the distinction from layout fixture setup and unrelated Graph native smoke.

`DrumInstrumentTests` adds family synthesis/presets/extremes, media-free musical persistence/export,
cross-channel/cross-clip hats, simultaneous priority, seek/partition parity and fractional-loop
allocation/capacity. `drum-kit` performs native Windows Rack/Piano/Playlist authoring, physical Song
and native Save/Open/export; `drum-kit-review` covers all seven editors and native custom presets.
Both are bounded friend-host routes documented in [VisualCheck](../tools/Seqvium.VisualCheck/README.md)
and the [family report](experiments/SEQ-DRUM-INSTRUMENTS_REPORT.md).

`DrumVoiceTests` covers actual parameter-to-signal differences, host rates/pitch/envelope time,
velocity, short-note one-shot tails, loop overlap/preflight, seek, partition/consumer allocation,
live publication/Stop, shared graph mixing, history/stale targets and media-free Save/Open/export.
Use the installed MTP route with `--filter-class "*DrumVoiceTests"`; related sampler/graph/Rack/Song
classes protect unchanged boundaries. The developer `VisualCheck drum <output> [delivery-directory]`
opens physical output deliberately: actual GUI edits, both themes/languages, compact field reachability,
guarded native dial/pitch/amp handles, quiet audition, looping/live edits, finite Song and WAV.
It labels routed controls, native input and physical output separately; it does not claim acoustic
listening or pressure acceptance. [Report](experiments/SEQ-FIRST-DRUM_REPORT.md) owns completed evidence.

## Required verification levels

Daily music interaction uses `DesktopWorkflowTests`, `PatternEditorNavigationTests`,
`WorkspaceStateTests`, `WorkspaceLayoutTests`, `PianoRollTests`, `StepSequencerTests` and relevant
presentation/metadata classes. New cases protect selected-target PCM and output reuse, same-target
position, stopped/Song selection, Stop/Panic during held preparation, New Save/Discard/Cancel and
failed Save, melodic Part choice, per-Pattern/lifecycle memory and reachable empty layout.
The [daily workflow route](../tools/Seqvium.VisualCheck/README.md#daily-musical-workflow-check)
distinguishes native input/physical output from actual-window routed/render checks. Use an independent
musical project copy; the GUI New/Save test intentionally saves that copy. Do not reinterpret foreground
refusal or device Starvation as a successful physical check.

Check enough to justify confidence in the change. Remove redundant work without weakening meaningful
assertions or scenario coverage. Select the scope before running it and report the level, commands,
results and remaining evidence gaps. These rules apply to ordinary tasks and handoffs.

| Level | Scope                                                                          | When                                                                                            |
|-------|--------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------|
| L0    | Source/diff checks and compilation of affected projects, zero warnings/errors  | During work and before completion                                                               |
| L1    | Focused tests for changed components and direct contract/lifetime dependencies | Every code task                                                                                 |
| L2    | Related integration and cross-subsystem scenarios                              | When interactions or shared contracts change                                                    |
| L3    | Full Release solution build and the entire automated suite                     | About every 10 completed repository tasks; earlier for substantial risk or a release checkpoint |
| L4    | Native GUI input, full visual matrix, physical audio and stress                | Specific affected risk or explicit request; independently selected                              |

An ordinary code task finishes with successful L0 + L1; add L2 for changed subsystem interactions or
contracts. Documentation-only tasks use static link/anchor, ownership, decision-status, consistency,
scope and diff checks; no application build or Markdown-existence tests are required. A focused test
project build already compiles its project references: do not also build those projects separately
without a distinct reason. Select classes from affected source/usages, rather than assuming folders
are namespaces. Local Core edits alone do not require L3.

L3 is not automatic after each substantial handoff, BUILD increment or commit. Early L3 can be
justified by project-format compatibility, a dangerous migration, DSP/realtime invariants, a broad
semantic refactor, platform dependency updates or a release checkpoint. Explain the concrete risk
before broadening. L4 does not follow L3 automatically; a full automated pass establishes neither
native input nor physical audio/visual acceptance. Never label L1/L2 as a full regression pass.

Use the existing [current checkpoint](PROJECT_STATE.md#verification-cadence) to record the last
successful L3 and the number of completed tasks since it. Count completed repository tasks, including
documentation/tooling tasks, once; builds, commits, intermediate edits and status messages do not count.
At approximately 10, plan L3 with the next completion; reset to zero after a successful full build
and full suite. Failed/incomplete L3 does not reset the count. This is a coarse cadence, not a task
management system or a reason to delay important checks.

### Time budget and reuse

Aim for **at most 120 seconds** for ordinary focused verification, including any required restore,
affected build and tests. This is an optimization target, never permission to omit required evidence.
Record restore, build, test-run and selected tooling durations separately with scope/environment.
If over budget, identify the expensive phase/test from existing logs or one bounded diagnostic run;
do not repeat the whole command blindly. Use incremental builds and precise class/method filters.

Restore with `--locked-mode` on setup or changed SDK/package/restore inputs or missing assets; do not
repeat restore for unchanged dependencies with valid assets. Build affected outputs once with
`--no-restore`, then test with `--no-build --no-restore` only when those outputs match the current
sources, configuration and framework. Reuse a sufficient successful check until a relevant new change,
failure or unresolved dependency invalidates it. A documentation edit does not invalidate compiled tests.
Record the covered source state so evidence remains reviewable.

The last retained 933-case full-suite TRX measures **17.5105 seconds** between runner start/finish (Windows x64, SDK
10.0.401/runtime 10.0.12, F1-FINALIZE). It excludes CLI startup, restore and build;
it is historical timing, not a full pass of later sources or a timing guarantee. Restore/full-build
timings are unavailable in that evidence. Obtain missing full-route timing at the next scheduled L3,
never run an expensive full suite solely to collect statistics. The bounded audit and current measured
tooling costs are recorded in [completed work](archive/WORK_LOG.md#seq-test-strategy-r1--2026-10-13).

### Fast commands for the installed runner

Root `global.json` selects .NET 10 Microsoft.Testing.Platform; Tests uses `xunit.v3.mtp-v1` 3.2.2 (locked MTP 1.9.1).
Inspect those manifests, `Directory.Build.props`, and any future
`Directory.Packages.props` after setup changes. No central package manifest currently exists.
From a restored checkout, this is one affected build followed by one L1 run:

```text
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*ArtifactRetentionTests" --filter-class "*DesktopInstanceTests"
```

Repeated `--filter-class` arguments are OR. Class and method filters use fully qualified names or
`*` at the beginning/end. For one method, use `--filter-method
"Seqvium.Tests.ArtifactRetentionTests.BusyCleanupLockSkipsAutomaticRotationButManualApplyFailsAndCanRetry"`.
Use `--list-tests` with the intended filter to inspect selection without executing it, and check the
reported count: zero selected tests is not successful verification. Use `--project`/`--solution`,
pass MTP arguments directly without `--`, and do not use VSTest `--filter`/`--logger` syntax.
The installed runner's `--help` is the local authority for optional switches. It provides
`--filter-trait` but current tests have no category traits; do not pretend `Category=Integration`
selects the integration cases. It does not expose `--filter-query` in this installed version.
Reports use existing `--report-xunit-trx --results-directory <owned-ignored-directory>`; no generic
TRX, coverage or dump extension is installed. No custom runner or route registry is required.

For planned L3, use the full commands in [current execution](#current-and-future-execution); omit
restore if its inputs/assets are unchanged. Select L4 commands separately from the owning harness guide.

## Current test routes and costs

The suite has one managed test project; categories below describe execution requirements, not
mutually exclusive counts or implemented trait groups. Contract and regression describe purpose and
can apply to unit, integration or GUI tests. Existing checks are retained.

| Kind        | Current examples / focused class patterns                                                                                 | Required boundary / cost                                                                                |
|-------------|---------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| Unit        | `*MusicalTimeTests`, `*PaneGeometryTests`, `*WorkspaceBoundaryTests`, `*GraphCanvasGeometryTests`                         | Pure logic/geometry; no OS window/device                                                                |
| Contract    | `*DocumentTests`, `*GraphDiagnosticTests`, `*GraphPersistenceTests`, `*ProductVersionTests`                               | Canonical intent/format/metadata; version evaluation starts SDK processes                               |
| Integration | `*ManagedMediaTests`, `*GraphLifecycleTests`, `*DesktopWorkflowTests`, `*DesktopInstanceTests`, `*ArtifactRetentionTests` | Real controlled storage, owner/coordinator or IPC; choose the affected boundary                         |
| Regression  | Nearest-cable cases, stale editor/menu guards, deferred text notification, retention lock/watchdog cases                  | Reproduce a named failure; geometry/logic stays below GUI when sufficient                               |
| GUI         | VisualCheck `smoke`, `catalog`, `gestures`, `matrix`                                                                      | Actual Avalonia templates, dispatcher/window/render; synthetic routed events are separate from OS input |
| Native      | Guarded VisualCheck pointer/screen checks and independent-process activation                                              | Real foreground/Windows input/processes; isolate test identities and stop on guard refusal              |
| Stress      | Explicit DeviceCheck/probe pressure commands; warmed allocation and bounded capacity cases also exist in managed tests    | Sustained pressure is L4; deterministic allocation/capacity checks are focused by affected contract     |
| Physical    | DeviceCheck and the independent R0 probe                                                                                  | Explicit real device/backend, separate from ordinary tests; never implicit physical sound               |

WAV parsing, document persistence envelopes and offline DSP can use memory/generated inputs; actual
Save/relocate/file-lock/access-failure contracts need owned temporary storage. Pure Desktop sessions,
geometry and control text/resource checks do not require GUI platform startup. Actual template geometry,
pixels, native pickers, foreground/input and rendered acceptance require their declared GUI/native route.
Named mutex/pipe, real MSBuild evaluation and retention process ownership checks require the OS, not
an active user window or audio device. Wall time is necessary for timeout/watchdog tests and explicit
stress/device measurements; ordinary musical time and concurrency use controlled values/gates.

## SEQ-R9 Piano Roll verification

`PianoRollTests` covers shared canonical chords/mixed notes, Rack refusal, atomic stale/unknown/invalid
edits, no-op order, group boundaries, rich length preservation, roots/metadata, Undo/Save/Open/export
and joined pitched preview. `OfflineSamplerTests` adds full MIDI-range preparation and independent
pitched PCM ramp oracles. Desktop presentation/layout tests cover the seventh retained capability.
After the affected Release build, OR `--filter-class "*PianoRollTests" --filter-class "*OfflineSamplerTests"`
with relevant host/layout classes under the installed MTP route, not VSTest syntax.

`VisualCheck piano` uses actual windows with routed musical edits, RU/EN Dark/Light ordinary/reduced
properties/root/Rack renders, field alignment and real containment. `piano-music` separately uses
guarded native note gestures and explicitly audible normal physical output with supplied local
catalogued WAVs. Save/Open/export use production workflow APIs, not native picker actions. Catalog and
gestures verify shared resource propagation/field alignment; native graph checks are labelled separately.
Commands, 1073-case full L3, timings, preserved user example and limits are in the
[R9 report](experiments/SEQ-R9_REPORT.md)
and [tool guide](../tools/Seqvium.VisualCheck/README.md#piano-roll-and-physical-musical-check).
Early L3 is justified by shared pitch preparation/graph and transient audio lifetime changes. It resets
cadence but neither closes R4 pressure acceptance nor supplies acoustic/high-DPI/all-platform evidence.

## Playlist music workflow verification

Arrangement/StepSequencer regressions cover seek PCM at 44.1/48 kHz, in-packet range wrapping,
zero allocation/sticky Stop, group transaction/clamping/Undo/stale authority, first/middle/last channel
removal and Undo/Redo, silent last-channel plans and recoverable preparation retry after BPM.
WavDecoder adds signed PCM24/32/rate cases and narrow exporter-padding validation. Actual production
windows verify musical groups/ruler/zoom, BPM popup Play availability, frame hit-test/captured resize,
maximized and normal rectangle restoration, retained panes, RU/EN Dark/Light, fields and Save/Open/WAV.
Routed input and guarded native captures remain distinct. DeviceCheck `sample-check` reads an explicit
library and measures raw/smoothed PCM boundaries without output; `song-smoke` independently opens a
quiet physical output and proves seek, range wrap, channel add/delete/Undo/Redo, finite end and join.
Commands, failures, timing and acceptance limits belong to the
[report](experiments/SEQ-PLAYLIST-MUSIC_REPORT.md). Reuse sufficient green checks under normal cadence.

## Playlist verification

ArrangementTests covers independently calculated absolute tick/frame PCM for pitched-independent
source graphs, gaps/overlaps/repeats and packet partitions at 44.1/48 kHz mono/stereo, explicit clip
occurrence identity, track Mute/Solo gates, live voice age/epoch/position, silent shortening behind
the playhead, zero warmed callback allocation, sticky Stop, voice/cancellation refusals, destination
protection and WAV/realtime equivalence. Document cases check legacy defaults, required reader 1.2,
unknown clip fields, net-zero history, source-deletion/Undo, track IDs/Save/Open, and stale/ABA commands.
The Desktop case uses the real workflow/coordinator/renderer with only its physical borrower substituted.

Use the affected build and MTP class filters `*ArrangementTests`, `*LoopPlaybackTests`,
`*GraphLifecycleTests`, `*GraphExecutionTests`, `*PersistenceTests`, `*GraphPersistenceTests`,
`*DesktopWorkflowTests` and affected Workspace/presentation/metadata classes. The checkpoint chooses
early L3 because project compatibility and shared realtime continuation change. Full build/suite and
scope/timing belong to the [report](experiments/SEQ-R10-PLAYLIST_REPORT.md), not a recurring per-task rule.

VisualCheck `arrangement` is an isolated actual-window RU/EN Dark/Light ordinary/reduced route:
placement/captured move/Ctrl-copy/right-edge repeats, cancellation/stale/Workspace withdrawal, source,
track M/S/name/color, snap, overview/stationary headings/fit, duplicate/Delete/Undo, Save/Open, retained
content and graph WAV. Actual editable repeat/label centers are measured. A second window uses the
real Song renderer at 44.1 kHz to prove its cursor/2:2 readout. These are routed controls, not native OS
input or physical sound. Images must be reviewed in both themes. DeviceCheck `song-smoke` separately
checks two simultaneous quiet Patterns, live future-clip publication, finite end, retained epoch,
allocation/deadline counters and acknowledged Stop/join on the physical output. It is short output
evidence, not acoustic listening, pressure, high-DPI or other-platform acceptance.

## R5-F1 Step Sequencer verification

`StepSequencerTests` covers exact bidirectional grid projection, finite gates, canonical toggle/name
edits/Undo/Redo, atomic empty channel acceptance and restored graph/media identities, shared Pattern
editing, concurrent/stale/ABA/closed targets, import cancellation/errors, unsupported shape/notes and
unknown-data Save preservation. Three independently authored WAVs exercise exact NoteOn/Off positions
and numerical Mix PCM at 90/120/240 BPM; Desktop Play uses the real coordinator/realtime sampler with
only the physical output borrower substituted and proves finite Ended plus sticky Stop. Long-source
cases separately establish ordinary NoteOff/bar clipping; voice/channel budget refusals are explicit.

Use the existing affected Release build, then select this feature and relevant boundaries:

```text
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*StepSequencerTests" --filter-class "*DesktopWorkflowTests" --filter-class "*ManagedMediaTests" --filter-class "*GraphExecutionTests"
```

Choose related Document/Persistence/GraphEditor/Workspace/NodeEditor/DesignSystem and metadata cases
when those boundaries change. The completed F1 ran 380 cases, then only four changed/new cases after
its final test-only strengthening, reusing unrelated green checks (383 distinct cases). Commands,
timings and limitations belong to [completed work](archive/WORK_LOG.md#seq-r5-f1--2026-10-13).
No schema/DSP/publication invariant changes justified early L3; cadence advances rather than resets.

VisualCheck's [focused steps route](../tools/Seqvium.VisualCheck/README.md#focused-step-sequencer-check)
renders RU/EN × Dark/Light at ordinary/reduced size, real controls/automation state, field/action
alignment, navigation, Save/Open and retained lifecycle. Synthetic pointer plus automation actions
are labelled separately from the guarded native click. The F1 native attempt refused on foreground;
native pickers and physical audio were not exercised. No full native matrix or stress follows.

### SEQ-UI-REDESIGN verification

Nine StepSequencer cases add atomic row operation/gesture Undo, unchanged note identities/no-op,
ABA and unsupported-data refusal, canonical tempo preservation/stale/range boundaries. Presentation
resource guards now include palette-derived HostMaterials; shared geometry assertions follow v2.
Final focused L1/L2 passes 435 cases in 19 named Desktop classes, including ProductVersionTests.
After final warning text, HostPresentation/DesignSystem/StepSequencer rerun passes 82 cases. These
are focused checks, not a full suite. Actual commands, build/output-lock workaround and timings are
recorded in the [report](experiments/SEQ-UI-REDESIGN_REPORT.md). L3 cadence advances to three.

The real GUI catalog/window matrix checks shared resources, parameter centers, themes/languages,
ordinary/reduced/minimum/maximized geometry and owned modal states. The extended `steps` host checks
captured row paint/erase cancellation, actual row menu, Gain dial draft/commit/fine/keyboard paths,
tempo popup and narrow overflow, navigator create/select/Undo, routed single-WAV drop/exact Undo/
multi-file refusal, dense eight-channel rendering and ninth-channel refusal. Native Windows Rack
click/focus/drag/Escape and graph/Workspace gestures use exact foreground guards; routed and native
results remain separate. Captures contain actual authored fixture data, never fake product tracks.
No new native picker/Explorer D&D, acoustic/device pressure, high-DPI or other-platform claim follows.

## R5 continuous music verification

`LoopPlaybackTests` verifies 4000 cycles against independent absolute tick/frame and PCM oracles at
48 kHz/137 and 1000 BPM, 44.1 kHz/137.123456 BPM, including several boundaries within a packet.
It checks live voice age/position/epoch, past-note insertion/erasure/Undo, superseded pending capacity,
sticky Stop and zero warmed callback allocation. `StepSequencerTests` adds 2/3/32-bar gates/IDs/Save,
occupied-shortening refusal, first/middle/last channel tree repair and exact Undo, selected Pattern
removal, actual graph WAV PCM, cancellation/destination/media protection and real Desktop loop convergence.
Default finite realtime/offline/audition, graph lifecycle, history and persistence remain covered.

The final SEQ-R5-MUSIC L3 passes 1009 cases with zero failures/skips after a zero-warning full Release
build. The initial focused L1/L2 passes 210 cases. Existing locked restore assets were reused because
SDK/dependency inputs are unchanged. Full L3 was justified by shared scheduler/voice-publication risk;
it resets cadence, without establishing native/physical acceptance. See the
[report](experiments/SEQ-R5-MUSIC_REPORT.md) for commands, timing and initial failed checks.

VisualCheck `steps` covers actual length/channel/Pattern/navigation controls and a simulated-output
Rack window driven by the real workflow/coordinator/sampler. Dark/Light live renders show 96 pads and
position 2:1:2/current column 17, followed by cleared markers on Stop; no physical audio opens there.
The existing RU/EN ordinary/reduced matrix, catalog, field alignment and four guarded native Rack
checks passed earlier; the final isolated native attempt refuses on foreground and is not counted
as a pass. Native export picker and acoustic listening remain unverified. DeviceCheck `loop-smoke`
separately plays quiet authored PCM for six seconds through WASAPI, proving live convergence with
unchanged epoch, continued position, Stop/join and zero measured allocation/deadline failures on that
path. It neither repeats nor closes the failing R4-F2 pressure protocol.

## Product version verification

[VERSIONING](VERSIONING.md) owns identity and checkpoint authority.
`Desktop/ProductVersionTests` adds 31 deterministic cases: actual production assembly/PE metadata,
four-digit display syntax and numeric equivalence, About's real `ShellSession.Version` accessor in
RU/EN and Dark/Light, BUILD 0/1/27/9999, invalid/overflow/unpadded-component refusal, independent metadata
override rejection, canonical source ownership, and production scope by project name plus location.
Real MSBuild evaluation covers Core, Windows adapter, Desktop, Tests, DeviceCheck, the independent R0
probe, and an unrelated artifact with a production-like name. It invokes no restore/build or network;
these tests require the checkout and pinned SDK, but initialize neither GUI nor physical audio.
Project JSON cases preserve the independent major/minor/minimum-reader gates, codec writer marker,
round trip and incompatible-reader refusal. Existing persistence/graph compatibility tests remain intact.

The focused command after an affected Release build is:

```text
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*ProductVersionTests"
dotnet msbuild src/Seqvium.Desktop/Seqvium.Desktop.csproj -nologo -property:Configuration=Release -target:ValidateSeqviumProductVersion,GetAssemblyVersion,AddSourceRevisionToInformationalVersion -getProperty:SeqviumDisplayVersion,SeqviumNumericVersion,Version,AssemblyVersion,FileVersion,InformationalVersion,IncludeSourceRevisionInInformationalVersion
```

SEQ-VERSIONING-R1 local verification (Windows 11 x64, SDK 10.0.401 / runtime 10.0.12): locked restore,
full Release solution build with zero warnings/errors, and **763 passing tests, zero failures/skips**
(all 732 previous cases plus 31 version cases). Evaluated properties, generated assembly attributes,
loaded Release assemblies and PE file/product metadata agree: Desktop/Core/Windows adapter display/
informational `0.1.0.0000`, assembly/file `0.1.0.0`, with no source-revision suffix. Tests and DeviceCheck
retain numeric assembly/file identity `1.0.0.0`; the independent probe retains its own props/defaults.

A separate real Win32/Avalonia host exercised the existing production MainWindow/About flyout using
the built Desktop and isolated preferences/layout. All four RU/EN × Dark/Light combinations rendered
`0.1.0.0000`, with the native window visible, flyout open and document pristine. The actual popup
rendering was captured and inspected; captures are local/ignored. Foreground screen capture was
obstructed and refused; no native pointer/foreground acceptance is inferred. Temporary host resource
ownership/initial theme synchronization defects were corrected before the accepted capture run;
they required no production UI change. This bounded About smoke does not accept F3-B visuals or
resolve F2 physical realtime evidence. No audio session or production release was created.

## Current and future execution

### Artifact retention and reliability checks

`ArtifactRetentionTests` uses small isolated temporary `.artifacts` fixtures for age/count/budget,
last success/failure reservations, idempotence, dry-run immutability, reviewed legacy deletion,
active owners/leases, nested/invalid ownership, links/external paths, partial file locks, synthetic
ACL denial, automatic lifecycle hooks, competing cleanup locks and dispatcher-independent deadlines
during startup/Dispose maintenance. Automatic rotation may skip a busy cleanup lock with diagnostics;
manual apply returns nonzero when cleanup was skipped. Every deletion still requires lock ownership.

```text
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*ArtifactRetentionTests" --filter-class "*DesktopInstanceTests"
```

The [VisualCheck guide](../tools/Seqvium.VisualCheck/README.md#deadlines-and-diagnostics) supplies
bounded real startup/close and process checks, with explicit timeout exit codes. Retention runs
at those tools' lifecycle boundaries under the [policy](experiments/README.md#rolling-retention-v1).
It does not silently change raw `dotnet test` output paths or claim ownership of unrelated reports.

### Desktop foundation verification

`DesktopFoundationGestureTests` covers nullable pane activation/layout restoration, screen-DIP click
tolerance/double clicks and one canonical move. `DesignSystemTests` checks required both-theme roles,
contrast, live resource/Fluent alias propagation, typed resources and the first-party literal-color/
AXAML resource guard. `DesktopInstanceTests` covers exclusive ownership, readiness, activation,
invalid/cancelled clients, timeout and joined shutdown without GUI/device initialization.

```text
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*DesktopFoundationGestureTests" --filter-class "*DesignSystemTests" --filter-class "*DesktopInstanceTests"
```

The separate [VisualCheck](../tools/Seqvium.VisualCheck/README.md) exercises actual window templates,
rendered parameter-row alignment, controlled central-style changes, RU/EN × Dark/Light geometry and
independent-process activation/crash/timeout. Routed gestures/fixture states remain labelled synthetic;
native screen/pointer attempts stop at foreground loss. Its isolated settings/layout/media preserve
ordinary user work, and it does not open a physical audio device. The
[foundation report](experiments/SEQ-DESKTOP-FOUNDATIONS-F1_REPORT.md) records the completed checkpoint's
specific results and limits. These checks do not close F2/F3-B acceptance, Q-032/Q-055 or broader platforms.

The R1 [Seqvium.Tests](../tests/Seqvium.Tests/Seqvium.Tests.csproj) project uses xUnit v3 3.2.2 with
explicit MTP v1 support; its lock file resolves Microsoft.Testing.Platform 1.9.1. This suitable setup
was available in the installed package cache and verified against SDK 10.0.401 / runtime 10.0.12;
no VSTest adapter, coverage tool or additional runner is introduced. SDK 10 MTP mode is selected in
[global.json](../global.json). Setup/planned L3 commands from the repository root:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
```

Use MTP arguments directly, without a `--` separator. xUnit filtering uses `--filter-class`,
`--filter-method` or `--filter-trait`, not VSTest's `--filter`. Optional built-in xUnit TRX output uses
`--report-xunit-trx --results-directory <directory>`; no generic TRX/coverage extension is installed.
The suite exercises document lifecycle, typed identities/sharing, variations, sound independence,
transaction rejection/deletion/history, processing relationships, exact time conversions, format
compatibility/unknown data (including known-field edits), net-zero history, degraded access and
normal-process save failures. Fixtures are synthetic
and files use owned temporary directories. The locked-file overwrite check is Windows-specific;
the directory-target failure and canonical checks are portable. Ordinary synchronization uses explicit
gates; infinite cancellable waits deliberately model stalled/cancelled work.
F1 adds `WavDecoderTests`, `ManagedMediaTests`, `OfflineSamplerTests` and the independent tiny
`WavFixtures` writer. They cover supported/extensible PCM/float WAV, all truncated prefixes/chunk bounds,
durable unnamed acceptance/source removal, Save As/reopen/relocation, actual integrity/degraded Save,
pending/stale/Undo/close/cancel gates, interrupted reads/storage blockers and partial transfer failures.
Async tests use explicit completion gates and cancellation, without sleeps. DSP tests use independent
linear-ramp/envelope/rational timing oracles (absolute float tolerance 2e-6), 1/4/8 voices, independent
release, equal-frame conflicts, zero-frame notes, partition/loop invariance, root/fractional pitch,
44.1/48 kHz and 120/137 BPM. Capacity/dependency failures must occur before output. This is local
functional evidence, not device timing, real disk-full/power-loss, process-crash or cross-platform proof.
Current local totals and evidence limits belong to [PROJECT_STATE](PROJECT_STATE.md).

F2 adds `RealtimeSamplerTests` for shared offline/packet PCM and independent ramp/release/rational oracles,
1/4/8 voices, Stop/Start command order, sticky Panic under gain bursts, stale/cancelled/closed/removed-target
acceptance, transition-generation rejection after Undo/Redo, pending/retired backpressure and preparation
candidate capacity, packet refusal, explicit termination and lease release. Per-thread allocation checks
surround warmed Process/control entry only; native/runtime-entry evidence is separate.
The [physical harness](../tools/Seqvium.DeviceCheck/README.md) runs explicitly in Release, never as an
ordinary test or a hardware-dependent skip. Its pre-output capture is compared to offline execution at
the same actual partitions and to an independent source/interpolation/envelope sum. Baseline and pressure
budgets/fault policy are predeclared in the [F2 protocol](experiments/SEQ-R2-F2_PROTOCOL.md);
results and exact allocation boundaries are in the [report](experiments/SEQ-R2-F2_REPORT.md).
No hardware removal, DAC measurement or hosted CI result follows from injected errors or PCM comparison.

F3 adds SourceAccessTests, WavAuditionTests and WavResourceReuseTests. Deterministic tests cover bounded
one-level discovery/truncation/diagnostics, Windows exclusive-file refusal, unavailable external versus
degraded accepted media, no canonical/storage preview side effects, independent temporary leases,
rate/interpolation/EOF, Stop/live cancellation/Close/fault/supersession, in-flight gates and capacity,
source-specific Start under retirement backpressure, explicit import/source deletion, one resource
with independent sound/part uses, Undo/Redo and Save As/reopen. No physical device opens during tests.
Warm preview allocation checks measure Process only, not general runtime/native costs.
The explicit `audition-smoke` Release command warns about sound and uses gain 0.05 with low-amplitude
authored PCM. It checks both diagnostics-off and opt-in capture, with a separate independent raw-source
oracle and joined release. [F3 evidence/audit](experiments/SEQ-R2-F3_REPORT.md) distinguishes short smoke,
post-change pressure, historical full F2 evidence and unmeasured acoustic/platform/device behavior.

F4 adds `AudioEndpointTests` with injected immutable discovery results: independent direction/intent,
duplicate names/distinct IDs, role-following versus fixed IDs, default disappearance/query failure,
missing/inactive/wrong-direction refusal, invalid selection, frozen observations and no project edit.
Real Core preparation/retirement is exercised with only a borrowing/join boundary stub, never fake WASAPI.
Changing output preserves processed Stop acknowledgments, does not invent unprocessed acknowledgments,
joins before state release and retains ownership after thrown or unconfirmed join. Fresh attachment
and input failure leave unrelated output valid. A discovery-call counter stays unchanged through
100 Process packets; source review additionally verifies native enumeration is outside the service loop.
`endpoints` is silent physical discovery. Explicit `endpoint-session <ID>` warns and uses gain 0.05;
it validates selected output/Stop/join/reopen and injected failure, not microphone capture. The
[F4 report and complete R2 audit](experiments/SEQ-R2-F4_REPORT.md) retains results and evidence limits.
The full F2 performance matrix is unnecessary for F4: scheduling/DSP/service processing did not change;
new enumeration is control-side and native validation precedes stream activation. Bounded selected
physical lifetimes and the full deterministic suite complement existing F2/F3 workload evidence.

The standalone [SEQ-R0 assertion harness](../experiments/seq-r0/README.md) remains independent:
`dotnet bin/Release/net10.0/Probe.dll verify` after `./build.ps1` in `experiments/seq-r0/`.
It is not the production test suite. Documentation verification checks links/anchors,
metadata, ownership/routing, decision status, scope, passive configuration, and Git diffs/status under
[AGENTS](../AGENTS.md) and [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md).

On setup changes, inspect SDK, platform, framework and manifests before changing commands.

SEQ-R0 device/timer/lifetime commands and exact scope are in the
[probe README](../experiments/seq-r0/README.md); measured results belong to the
[report](experiments/SEQ-R0_REPORT.md). Release builds treat warnings as errors. Its oracle checks
ordered absolute events, partition independence, generated float output, critical controls, skipped
time, prepared-state capacity/publication and ownership. Actual device callbacks and offline captures
are separate from timer-driven evidence. This does not establish plugin/graph/UI/recording or release
acceptance, and no hosted checks are introduced.

## R3-F1 host verification

`Desktop/HostPresentationTests` and `PreferenceStoreTests` add 34 cases to the 274-case R2 baseline.
They verify stable catalogs and per-entry/essential fallback, both semantic palettes and contrast,
canonical snapshot/generation/history/name/numeric integrity, acceptance of actual previously prepared
WAV work after preference changes, shell create/close, ordered preference commands, all four persisted
combinations, missing/corrupt/oversize/write-failed settings and preservation of unrelated user content.
They reference Desktop but never initialize the platform or open a physical window/audio stream.
After an affected build, `--filter-class "*HostPresentationTests"` selects host
presentation cases when needed. The [F1 report](experiments/SEQ-R3-F1_REPORT.md) separately records actual
Windows UI Automation/pointer/keyboard smoke and visual inspection. Compilation/pure tests are not GUI
acceptance; high/mixed DPI, screen readers and Linux/macOS runtime remain missing evidence.

## R4-F3-B graph editor verification

### Reusable node editor verification

`NodeEditorTests` adds 41 cases without GUI/device initialization: stable four-part target identity,
draft/generation/ABA gates, deletion/Undo/restoration, unsupported/opaque states, lifecycle replacement,
owner commits/one edit/no-op, numerical dB oracle, canonical Save/reopen, deferred text-echo refusal,
menu view-action expiry and single empty-state content hints. A deterministic production F2
coordinator/PCM case verifies coefficient convergence, preserved execution identity/epoch, sticky
Stop and joined release through the editor's captured command. Exact-parameter visual priority and
true intervening-draft notices have independent cases; shared palette checks also cover unselected
node surfaces/boundaries and readable text in both themes. The full baseline is **908 passing
tests**, retaining all 867 earlier cases. The existing fresh-Play helper consumes a final packet
after owner acknowledgment before asserting actual playback; no physical output is substituted as
GUI/audio evidence.

Select `--filter-class "*NodeEditorTests"` after the affected
build. The [editor report](experiments/SEQ-NODE-EDITOR-IMPL-F1_REPORT.md) separately records native
Windows opening/menu/drag, actual-window RU/EN × Dark/Light ordinary/reduced and empty-state smoke,
controlled routed Gain/capture interactions, previous failures and foreground refusal. Routed events
are not native input; no new physical audio/pressure acceptance is claimed.

`Desktop/GraphMenuTests` adds 29 deterministic regressions for the actual captured menu command path:
all 20 Source/Gain/Mix/Output layout/input-count choices in incomplete intent, exact node/position/ports,
unbound Source and designated Output, one-edit Undo restoration, per-type Output refusal with RU/EN
labels, processing limits, cancellation/generation/target/replacement/Closed safety and Busy/no-target
availability. Tests initialize no GUI platform or output device. Existing 838 cases are preserved.
Run after Release build with `--filter-class "*GraphMenuTests"` on the existing MTP/xUnit command.

SEQ-R4-F3-B-UX-F1 additionally reproduces the defect with guarded Windows mouse input and records
right press/release, nested hover, Stereo Gain creation, exact Undo, button-anchored Mono Gain creation,
Apps/Shift+F10, Escape/focus and outside dismissal in RU/EN × Dark/Light. The
[report](experiments/SEQ-R4-F3-B_REPORT.md#context-menu-lifecycle-and-node-creation-repair) distinguishes
this native sequence from controlled actual-window pane/replacement checks and no-platform regressions.
Locked restore, full Release build and all 867 tests pass with zero warnings/errors/failures/skips.
Final production metadata displays `0.1.0.0001` (CLR/file `0.1.0.1`); this repair does not accept F3-B
for the owner or resolve F2 starvation. Routed synthetic events alone are not native mouse evidence.

`Desktop/GraphCanvasGeometryTests`, `GraphEditorSessionTests` and `GraphEditorWorkflowTests` add
52 deterministic cases over the existing 652-test baseline. They cover transform/anchor/zoom/pan/frame,
node/port/parameter/cable hit precedence and preview geometry, Mix positions, typed refusal/fan-out,
explicit retained targets and coherent replacement observations, cancellation/no-op/one-drag history,
Undo/Redo ABA, source/Output creation/deletion/binding, atomic Mix intent and second-source acceptance,
shared-Pattern refusal/failure/cancellation, durable topology/coordinate roundtrip and numeric repair.
Independent two-source PCM checks verify pre-mix branch gains and natural EOF; fan-out retains one
contribution/NoteOn. Live tests verify prepared identity/position/epoch on Gain updates, invalid last-valid
and repair convergence, invalid Save/reopen, pending preparation replacement and Stop/Panic/joined release.
Pattern length checks reject note truncation and placement overflow atomically.

The F3-B cable correction preserves all 704 prior cases and adds 28 regressions. The crossing fixture
reproduces the original wrong connection ID at `t=0.515` before the fix, with both distances inside
tolerance. Geometry cases cover nearest selection despite a farther selected/top cable, exact ties,
near-coincident curves at minimum/normal/maximum zoom, global-minimum tie grouping, unchanged six-screen
pixel tolerance and node/port precedence. `GraphConnectionLabelTests` checks real input order, RU/EN
roles, multiple outputs and retained missing endpoints. Workflow cases exercise geometry → selected ID
→ shared indication label → actual `GraphCanvas.DeleteSelectedAsync` → existing disconnect edit →
saved canonical IDs → Undo, preserving neighbours and original topology. No GUI platform/device is
initialized by the deterministic suite. The report separately records routed pointer/key events in
an actual Windows window and the unsuccessful bounded native-input attempt.

Tests initialize no GUI platform/device and use owner barriers, completion gates and sequential PCM
packets, with no sleeps as timing oracles. The seven initial R3 test failures assumed exactly two panes;
their count/conflict assertions now include the added hidden graph pane without removing baseline cases.
For focused work after the affected build:
`dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*GraphEditor*"`.
Actual Windows canvas/picker/workspace/focus checks and quiet physical output are separate evidence in
[the focused report](experiments/SEQ-R4-F3-B_REPORT.md). Owner review remains mandatory; green tests
do not accept canvas usability or reclassify the inherited F2 starvation evidence.

## R3-F2 workspace verification

`Desktop/WorkspaceStateTests` and `WorkspaceLayoutTests` initially added 38 deterministic cases.
The geometry/chrome correction adds 50 cases (396 at that checkpoint). The resize-target/boundary
correction adds 32 `WorkspaceBoundaryTests` cases, for **428 total**, retaining all 396 baseline cases.
The initial tests cover stable identity/localization, bounded front order, overlap
activation, floating/docked geometry and reflow, collapse/hide/reopen, traversal/disposal, per-pane docking
permission/conflicts, restored dock intent, early-close snapshots, bounded/partial/corrupt/future files,
roundtrip/backup/write failures and latest-pending shutdown. Actual canonical Save, prepared WAV acceptance
and preference changes verify ownership independence. These tests initialize no platform or physical audio.

`PaneGeometryTests` asserts exact edge/corner anchors, repeated reversibility, one-pixel reversal after
boundary/minimum saturation, dock width limits and retained floating bounds, rollback and compact hit
regions. `PaneChromeTests` checks actual Button/nested-visual source exclusion before capture, without
platform initialization. Additional layout tests verify independent dock width and legacy version-1 reads.
Localized close labels and keyboard hints are checked separately from actual control accessibility.
`WorkspaceBoundaryTests` exercises inward/outward tolerance, corner priority, distance/front-order ties,
touching visible sides, covered/invisible boundary exclusion, control sources, inactive direct resizing,
gesture stability after reordering and current independent dock seams. These display-independent cases
do not initialize Avalonia's platform or claim native cursor/capture correctness.
Use an affected build and the focused classes below; broader levels follow the selected risk.
The [F2 correction](experiments/SEQ-R3-F2_REPORT.md#geometry-and-chrome-correction)
records 359 actual Windows input/cursor checks, retained content identity/focus and inspected local captures.
The original 58 checks missed the owner's reproducible resize/cursor defects; they do not establish
interactive acceptance. Pure tests do not establish control focus, pointer capture, visual or OS acceptance.
[Boundary correction evidence](experiments/SEQ-R3-F2_REPORT.md#resize-hit-targets-and-boundary-arbitration)
records real Windows Release tolerance, overlapping/meeting boundary, source-route cursor and focus checks.
High/mixed DPI, screen-reader certification, Linux/macOS and crash/power-loss guarantees remain untested.

## Intended test topology

Prefer **one main managed `Seqvium.Tests` project**, organized by domain/feature folders. Conceptual
examples are `Domain/`, `MusicalTime/`, `Patterns/`, `Serialization/`, `NodeGraph/`, `Resources/`,
`Extensions/`, `ProjectStats/`, and `Support/`. These are examples, not required empty folders;
the R1 suite uses cohesive feature files. Initial framework/platform selection Q-039 is resolved;
future materially different execution hosts need separate evidence.

Do not create a test project per production subsystem. Add a separate project only for a materially
different execution contract: native runtime/materialization, another target framework/platform, a UI
integration host, out-of-process plugin isolation, or materially different dependencies/lifecycle.
An architecture folder alone does not justify a project. This starting preference may evolve with evidence.

[ProjectStats](PROJECT_STATS.md#test-strategy-and-placement) tests belong in
`Seqvium.Tests/ProjectStats/`, using synthetic temporary repositories. Tool internals are visible to
the main Tests assembly for scanner/I/O/CLI checks; no separate tooling test project is needed.

## ProjectStats verification

`ProjectStatsScannerTests` and `ProjectStatsApplicationTests` add 75 cases to the unchanged 763-case
baseline. Synthetic repositories cover classification/nearest-project ownership/density, lexical
attributes, UTF-8/BOM/CRLF/CR/LF and UTF-16 counts, deterministic ordering/Top-N, excluded-before-read
privacy, report self-exclusion, source/output protection, read/write/encoding/size failures,
real Windows junctions and Unix file/directory/dangling symlinks, all formats, strict CLI, Git fallback,
branch sanitization, assembly identity and absence of product/analysis dependencies. Conditional
Windows exclusive-open and Unix symlink branches reflect filesystem semantics without test skips.
Tests create and delete only their owned temporary repositories.

After the affected Release build (the full solution command is reserved for planned L3):

```text
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "Seqvium.Tests.ProjectStats.*"
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
dotnet run --project tools/Seqvium.Tools.ProjectStats -c Release --no-build --no-restore -- . --markdown --output PROJECT_STATS.md
git check-ignore PROJECT_STATS.md PROJECT_STATS.json
```

Windows 11 x64, SDK 10.0.401 / runtime 10.0.12: full locked restore and Release solution build pass
with zero warnings/errors; **838 tests pass, zero failures/skips**, preserving all 763 prior cases.
Real console/Markdown/JSON reports are inspected for counts, sanitized metadata, exclusion of root
reports/cold history, relative paths and Git-ignore status. This accepts no musical/GUI/audio behavior.

The same portable Release binaries execute under Ubuntu 24.04 / WSL2 Linux x64, runtime 10.0.12.
The focused 75 cases pass there, including real Unix symlinks; Git/JSON scanning agrees with Windows
structural results on the same source files. The available Linux SDK is 10.0.112 rather than pinned
10.0.401: this is runtime/test evidence, not a separate Linux source build, hosted CI, GUI/audio
acceptance or product release support.

## Future test categories

Categories describe required kinds of evidence as their owning capabilities arrive, not installed suites.

| Category                    | Bounded scope / method                                                                                                                                   |
|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| Pure/domain                 | Musical time, events, shared patterns/variations, IDs, undo/redo, serialization model, graph validation                                                  |
| DSP/offline                 | Generated impulse, sine, fixed-seed noise, bounded synthetic buffers; numerical behavior with declared tolerances, no physical device                    |
| Scheduler/transport         | Virtual/offline time; block/loop boundaries, event ordering, note start/end, seek/start/stop, tempo transitions when introduced; avoid wall-clock sleeps |
| Graph contracts             | Valid topology, branches/merges, invalid type connections, missing nodes, prepared publication, state preservation                                       |
| Serialization compatibility | Load/edit/save/reload, migrations, stable identities, unknown/missing plugin state, graph relationships, shared resources versus placements              |
| Plugin compatibility        | Later host/plugin version combinations, missing optional capability, incompatible required capability, disabled plugin, opaque state preservation        |
| UI logic                    | Pure geometry/state independent from UI framework/runtime where practical                                                                                |
| Runtime/manual UI           | Pointer feel, DPI, docking, animation, real windowing; unit tests alone do not establish these                                                           |
| Physical audio-device       | Separate actual device/backend/sample-rate/buffer evidence; hosted/offline success does not establish device acceptance                                  |

Subsystem contracts remain in [AUDIO_ENGINE](AUDIO_ENGINE.md), [NODE_GRAPH](NODE_GRAPH.md),
[PROJECT_FORMAT](PROJECT_FORMAT.md), [EXTENSIONS](EXTENSIONS.md), and [WORKSPACE](WORKSPACE.md).

## Test quality

- Every new test must name and exercise a concrete contract, regression cause, important boundary
  or failure. Prefer observable behavior through the responsible public API; do not test private
  methods solely for convenience. Internal seams are appropriate for concrete ownership/testability
  boundaries, rather than exposing production state just to assert implementation details.
- Use Theory/parameterization for multiple inputs of one contract. Do not create near-identical cases
  to inflate counts. Before adding a case, inspect existing neighbouring coverage. Repeat a contract
  across levels only when a distinct boundary needs evidence, and explain that reason in the test
  or verification report (for example, offline oracle versus realtime publication versus editor command).
- Keep fixtures small and realistic. Reuse the independent tiny WAV/composition builders where suitable;
  avoid both oversized setup for pure logic and excessive mocks of behavior the test should exercise.
- Prefer generated audio, project-authored tiny fixtures, deterministic event streams, fixed seeds,
  and temporary directories. Explicitly own fixtures, tasks, resources, cleanup, and lifecycle.
  Do not depend on developer music libraries, personal samples, commercial songs, downloaded packs,
  or mutable machine configuration. Tracked audio/MIDI/image/font/test assets follow
  [THIRD_PARTY](THIRD_PARTY.md) provenance; document generated-fixture creation when useful.
- Tests must not depend on execution order, other tests' mutable state, personal files/settings,
  pre-existing processes or the active user window. Use unique test-owned identities/temporary resources
  for OS integration. Ordinary unit/integration tests must never open real audio devices. Own and
  release objects, leases, threads, timers, subscriptions and tasks even on assertion failure.
  Control clock, lifetime and cancellation; bound external waits and join test-owned work before cleanup.
- Async tests use `await`, meaningful cancellation, and deterministic synchronization; avoid
  `.Result`/`.Wait()`. Arbitrary `Thread.Sleep`, `Task.Delay`, polling delays, and timing luck are not
  the normal proof of concurrency. Prefer fake/virtual clocks, explicit schedulers,
  `TaskCompletionSource`, barriers/latches/channels, deterministic callback harnesses, or explicit
  publication signals. Tests of timing itself must declare tolerances/environment and must not
  masquerade as deterministic unit tests.
- Preserve failure semantics; do not weaken assertions for green output. Functional correctness and
  timing/platform acceptance are distinct claims. Never hide errors, widen numerical tolerances without
  a contract-based reason, or disable meaningful scenarios to meet a time target.
- Prefer device-independent offline tests for deterministic DSP/signal/scheduling logic. Test declared
  numerical tolerances rather than assume bit identity across arbitrary processors/platforms/plugins.
  Physical-device latency/crackle acceptance remains separate.
- Automate pure UI layout/state logic where useful. Actual pointer interaction, DPI, windowing,
  rendering integration, and audio-device feel may require declared runtime/manual evidence.
- For a defect fix, prefer a regression that reproduces the cause and detects its return; demonstrate
  failure under a representative fault when useful. A small visual change does not justify dozens of
  GUI tests. Keep pure logic/geometry, synthetic routed events, real OS input and visual acceptance
  separate in both selection and reporting.
- Fast unit/contract checks must stay fast. A new expensive check needs a concrete cost reason and
  a separate focused route documented here or in its harness guide. Avoid repeatedly starting a
  platform/process for cases whose contract needs only values; retain real boundary checks where needed.

For critical scheduling, serialization, compatibility, or lifetime regressions, optional negative-proof
evidence can deliberately inject a representative fault and confirm that the owning test fails.
The purpose is test sensitivity, not a mutation-score metric or universal gate. No mutation framework
is introduced now.

## Focused, full local, and hosted workflow

Follow [L0–L4](#required-verification-levels) for local scope, cadence and reuse. Existing class/method
filters provide focused routes; the deferred registry [I-002](IDEAS.md#i-002--named-test-routes) is
not required. After an authorized push, inspect only actually enabled hosted checks and their
revision/scope under [CI_CD](CI_CD.md); a push alone is not successful hosted evidence. No hosted
workflow currently exists. Runtime/manual evidence supplements the automated scope when selected.

## R4-F3-A desktop workflow verification

`Desktop/DesktopWorkflowTests` adds 34 deterministic cases over real R2 import/persistence and R4
preparation/coordinator/sampler. Only the physical borrower is substituted; no mock document, PCM or
independent preview Gain exists. Explicit owner barriers and worker completion gates cover exact
Source binding, full-source duration, canonical coefficient PCM/identity/provenance, atomic Undo/Redo,
durable Save/Save As/reopen, cancellation/rejection, dirty Cancel/failed Save, invalid/missing media,
pending preparation replacement/shutdown, sticky Stop/Panic, fault/join retention and final release.
Six startup cases reproduce rejected output attachment with a terminated sampler and coordinator
factory failure after successful attachment. Real prepared PCM survives until confirmed consumer join;
throwing/unconfirmed joins retain ownership, refuse replacement and allow explicit Play/close cleanup
retry. Successful rollback permits a fresh explicit Play without canonical edits or automatic playback.
`WorkflowPresentationTests` adds nine compact status/human label and immutable GUI observation cases,
including non-default real tempo/meter. Existing 609 cases remain intact. No GUI platform or physical
device is initialized by the ordinary suite. Actual totals belong to PROJECT_STATE.

Build affected projects and select `--filter-class "*DesktopWorkflowTests"` plus
`--filter-class "*WorkflowPresentationTests"` and affected integration neighbours. Actual Windows
review separately exercises native pickers, owned unsaved modal, retained panes, narrow overflow,
RU/EN, Dark/Light, Gain endpoints, focus/shortcuts and failed Save. Quiet explicit physical playback is
a bounded smoke only. [F3-A evidence](experiments/SEQ-R4-F3-A_REPORT.md) distinguishes deterministic,
rendered GUI, device and pending owner/listening evidence; passing tests alone do not accept visuals
or supersede F2 pressure failures.

## R4-F1 graph verification

`Graphs/GraphEditingTests`, `GraphDiagnosticTests`, `GraphPersistenceTests` and the synthetic
`Support/GraphFixture` exercise canonical Core APIs in the existing Tests assembly. Cases cover typed
global identity collisions, atomic context/graph creation, Source coverage including silent parts,
same-name/sound/resource independence, item placement identity, supported ports/cardinality/fan-out,
deterministic topology diagnostics, all directed cycle classes and incompatible/unknown declarations.
Variation remapping, explicit ownership deletion, retained unresolved foreign dependencies and R1
referenced-owner refusal are checked together with Undo/Redo, net-zero collection/JSON reconstruction,
ended/reentrant/deleted-target operations and failure immutability.

Placement-level downstream routing blocks preparation for otherwise eligible single-part and mixed
item graphs. Real edits, Save/reopen and Undo/Redo preserve same-name distinct route identities;
the graph without its own downstream route remains eligible even when another placement is routed.

Persistence cases remove graph fields to reproduce graph-free 1.0 shape, retain precise Int64 ticks,
round-trip every graph boundary/coordinate and unknown/opaque state, and Save incomplete/cyclic graphs.
Compatibility fixtures freeze the actual baseline 1.0 envelope refusal predicate, verify graph-required
minor 1 and underdeclared refusal, higher compatible minor retention and nondecreasing requirements.
This is an envelope contract fixture, not a separately built historical reader. Existing version-refusal
cases now target required minor 2; all baseline test cases remain. Malformed identities/fields/ownership,
document/port/processing bounds, JSON depth/lifetime and failed Save retaining prior file/state/history
are deterministic and use owned temporary directories.

After the affected Release build, focused
run:
`dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "Seqvium.Tests.Graph*"`.
These tests open no GUI, audio device or network and establish intent eligibility only. Media decode,
prepared graph resources, PCM separation/oracles, realtime budgets/publication and actual canvas/device
behavior require separately authorized F2/F3 evidence. Current totals belong to PROJECT_STATE.

## R4-F2 execution verification

`GraphExecutionTests`, `GraphRealtimeTests` and `GraphLifecycleTests` add 42 cases without modifying
the 558 prior assertions. Focused execution after a Release build:

```text
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*GraphExecutionTests" --filter-class "*GraphRealtimeTests" --filter-class "*GraphLifecycleTests"
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*WavAuditionTests" --filter-method "*WarmPreviewProcessorAllocatesNothingAcrossVariablePackets*"
```

The authored WAV oracle reads canonical note intervals, absolute rational frame times and original PCM;
it calls neither prepared events nor production execution to derive expected samples. Tests compare
independent Kick/Snare, branch Gain, Mix and Output, release/EOF/hard Stop/repeats and stereo asymmetry
across rates/layouts/packet partitions. Branch swap, fan-out, same-resource independence, silent-source
media, unity Gain and geometry/node order are covered. A nonassociative three-input example asserts
exact port-UUID accumulation rather than toleranced/commutative equality. The largest numerical workload
uses 32 nodes, 8 sources/voices, 22 Gain, 8-input Mix and a 65,536-frame stereo packet. Five unique large
PCM16 silent dependencies verify actual decoded-budget refusal before execution.

An injected serialized owner context and explicit completion/cancellation gates exercise latest Edit/
Undo/Redo, ABA, target/Close, retained capacity retry without reprepare, invalid canonical/repair,
no last-valid resurrection after Save/reopen, Gain/release cursor continuity, finite topology ramp
including in-packet EOF, Stop/Panic, failed join retention and eventual PCM collection. Normal tests
open no physical output and use no timing sleeps. Warm allocation measures Process/status entry only,
after separate warm-up, with harness arrays/fixture setup outside the measurement. The existing preview
allocation case passes five isolated consecutive runs as well as full-suite runs; assertions are unchanged.

Explicit Windows evidence extends the existing DeviceCheck:

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-measure 60
```

It warns before quiet authored output, uses the actual 32-node/8-source attachment and negotiated
format/capacity, bounded diagnostics/capture, independent arithmetic versus captured PCM and offline
packet parity, deadline/allocation/GC observations, owner convergence under pressure, hard Stop/Panic
and joined injected fault lifetimes. The [report](experiments/SEQ-R4-F2_REPORT.md) owns actual duration,
endpoint, packet distribution, limits and retained measurements. Pressure's dynamically edited capture
is not relabeled as steady-state oracle evidence; its oracle/parity fields are null. Diagnostic zeros
are evidence only when the corresponding diagnostics mode was enabled.
Full F2 pressure acceptance remains partial: passing runs coexist with the original two non-injected
starvation failures and further failures in the diagnostic continuation below. Callback allocation/
deadline counters alone do not cover missed service wakes.

Execution-identity correction adds nine deterministic cases while preserving all 600 existing tests:
two valid attachments prepared from one document revision, rejected pending target, repair and PCM/
provenance/convergence with 1/2/3 Gain versus A's two Gain, both playing and sticky stopped. Same-attachment
reprepare/Undo/Redo/ABA, pending target changes, explicit rejected-handoff retry without a canonical edit,
finite retry and both publication/consumer identity gates are covered. None uses a device or timing sleeps.
The [diagnostic variants](../tools/Seqvium.DeviceCheck/README.md#r4-f2-starvation-attribution-variants)
separate CPU, allocations, canonical edits, forced collections and graph-free legacy execution on the
same physical path. Their native GC/Suspend/Restart observations and bounded service windows explain
individual intervals; aggregate wake/GC totals alone remain insufficient for causal attribution.

## Evidence tiers

These are complementary evidence classes, not a ranking in which a higher-looking tier replaces lower
semantic tests. Record source revision, environment, input/configuration, result, and material limits.

| Evidence                 | Proves within its declared scope                                                                                                                            |
|--------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Unit / pure              | Bounded deterministic logic                                                                                                                                 |
| Integration              | Controlled subsystem interaction                                                                                                                            |
| Hosted cross-platform CI | Build/test/runtime boundaries on hosted Windows/Linux/macOS environments                                                                                    |
| Native interop           | Selected native build/materialization/loading and production interop for a declared platform/RID                                                            |
| Local/manual runtime     | Actual application interaction on a declared real desktop/audio environment                                                                                 |
| Hardware/audio-device    | Behavior with a declared real interface/device/backend and settings                                                                                         |
| Delivered distribution   | Identified packaged artifact installation/materialization, launch/dependency loading and applicable update/removal behavior in a declared clean environment |

Hosted CI can prove compilation, offline DSP, serialization, graph scheduling, native loading/interop,
and packaging contracts when those checks exist. It cannot alone prove stable real-device low latency,
ASIO quality, USB interface behavior, glitch-free playback under desktop load, microphone/input
monitoring quality, or user-perceived UI behavior on every desktop environment. Use actual runtime/device
evidence for those claims. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns audio constraints and probe acceptance;
[PORTABILITY](PORTABILITY.md) owns platform claim boundaries.

Interpret runner families separately: Windows restore/Release build/tests do not establish actual
Windows desktop/device operation; Ubuntu build/offline checks do not cover all Linux desktops/audio
environments; macOS managed/native compilation does not establish a release-ready desktop application.
Checks prove only behavior actually exercised. Record WSLg runtime/GUI/bridged-audio evidence separately
from native Linux acceptance under [Portability](PORTABILITY.md#desktop-and-delivered-environment-acceptance).
Release claims combine the required evidence dimensions under
[release policy](PORTABILITY.md#evidence-led-release-policy), rather than treating any one tier as sufficient.

Useful optional status labels are **local accepted-ready** (declared local checks passed), **hosted-confirmed** (hosted
checks actually succeeded at a stated revision/platform scope), **runtime/manual accepted** (bounded real application
interaction accepted), and **hardware/audio-device accepted** (bounded real device/backend/settings accepted).
Use them only with declared evidence and material limits, not as ceremony for trivial tasks.
`hosted-confirmed` never implies physical audio-device acceptance. Link detailed reports rather than
accumulating individual runs in [PROJECT_STATE](PROJECT_STATE.md).

## Native composing workflow verification

Shared sampler replacement uses `SampleReplacementTests` plus `PianoRollTests`, `ManagedMediaTests`,
`SongStructureTests` and built `ProductVersionTests`: the bounded 91-case Release pass covers shared
music/processing/Mixer preservation, tuning, history/media, cancellation/errors and stale authority.
The [replacement report](experiments/SEQ-SHARED-WAV-REPLACEMENT_REPORT.md) distinguishes native
musical observations, complete inspection and old harness failures from a full regression claim.

The [SEQ-TRACK-GUI report](experiments/SEQ-TRACK-GUI_REPORT.md) separates native authoring in ordinary
Desktop from the automated VisualCheck fixtures. The `gestures` route now checks committed toolbar
Gain input under observation, Escape/invalid/focus-loss/stale cases; `arrangement` checks TOP + LEFT
origin, viewport width coverage and all 32 lane slots. These routes use isolated fixture ownership.
A real saved track is reopened, played to Ended and exported through native dialogs; its WAV is
inspected read-only for duration and saturation. This UI scope does not trigger a repeated L3 suite.
