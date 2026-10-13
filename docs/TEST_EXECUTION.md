# Test execution and verification

Role: Proportional verification, test quality, and evidence guide.
Read when: Planning, running, or interpreting verification and future tests.
Authoritative for: Test topology/quality, verification scope, future commands/platform discovery, Release gates, and
evidence tiers.
Not authoritative for: Product/subsystem contracts, SDK selection, CI triggers, supported release platforms, or current
progress.

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

The focused command after the normal Release gates is:

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
ACL denial, automatic lifecycle hooks and dispatcher-independent timeout diagnostics.

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
[global.json](../global.json). From the repository root:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --filter-class "*MusicalTimeTests"
```

Use MTP arguments directly, without a `--` separator. xUnit filtering uses `--filter-class`,
`--filter-method` or `--filter-trait`, not VSTest's `--filter`. Optional built-in xUnit TRX output uses
`--report-xunit-trx --results-directory <directory>`; no generic TRX/coverage extension is installed.
The suite exercises document lifecycle, typed identities/sharing, variations, sound independence,
transaction rejection/deletion/history, processing relationships, exact time conversions, format
compatibility/unknown data (including known-field edits), net-zero history, degraded access and
normal-process save failures. Fixtures are synthetic
and files use owned temporary directories. The locked-file overwrite check is Windows-specific;
the directory-target failure and canonical checks are portable. There are no timing sleeps.
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
Use the existing full locked Release commands; `--filter-class "*HostPresentationTests"` selects host
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

Use the full locked Release commands above, or select `--filter-class "*NodeEditorTests"` after the
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
Use the existing locked restore, full Release build and full solution commands above. For focused work:
`dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --filter-class "*GraphEditor*"`.
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
Use the same locked restore/full Release commands.
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

After locked restore and Release solution build:

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

- Prefer generated audio, project-authored tiny fixtures, deterministic event streams, fixed seeds,
  and temporary directories. Explicitly own fixtures, tasks, resources, cleanup, and lifecycle.
  Do not depend on developer music libraries, personal samples, commercial songs, downloaded packs,
  or mutable machine configuration. Tracked audio/MIDI/image/font/test assets follow
  [THIRD_PARTY](THIRD_PARTY.md) provenance; document generated-fixture creation when useful.
- Async tests use `await`, meaningful cancellation, and deterministic synchronization; avoid
  `.Result`/`.Wait()`. Arbitrary `Thread.Sleep`, `Task.Delay`, polling delays, and timing luck are not
  the normal proof of concurrency. Prefer fake/virtual clocks, explicit schedulers,
  `TaskCompletionSource`, barriers/latches/channels, deterministic callback harnesses, or explicit
  publication signals. Tests of timing itself must declare tolerances/environment and must not
  masquerade as deterministic unit tests.
- Preserve failure semantics; do not weaken assertions for green output. Functional correctness and
  timing/platform acceptance are distinct claims.
- Prefer device-independent offline tests for deterministic DSP/signal/scheduling logic. Test declared
  numerical tolerances rather than assume bit identity across arbitrary processors/platforms/plugins.
  Physical-device latency/crackle acceptance remains separate.
- Automate pure UI layout/state logic where useful. Actual pointer interaction, DPI, windowing,
  rendering integration, and audio-device feel may require declared runtime/manual evidence.

For critical scheduling, serialization, compatibility, or lifetime regressions, optional negative-proof
evidence can deliberately inject a representative fault and confirm that the owning test fails.
The purpose is test sensitivity, not a mutation-score metric or universal gate. No mutation framework
is introduced now.

## Focused, full local, and hosted workflow

- **Focused:** during implementation, run affected tests and direct contract/lifecycle neighbours.
  Broaden for shared contracts/consumers and risky publication, serialization, interop, or lifetime boundaries.
- **Full local:** before meaningful milestone/task handoff while the suite remains reasonably fast,
  run a Release build and the normal full suite with zero warnings/errors under
  [CODING_GUIDELINES](CODING_GUIDELINES.md). Enforcement begins with actual projects.
- **Hosted:** after an authorized push, inspect the actual enabled Windows/Ubuntu/macOS checks and
  their revision/scope under [CI_CD](CI_CD.md); a push alone is not successful hosted evidence.

Do not repeat sufficient green checks without a new change, failure, or unresolved concern. Runtime/manual
evidence supplements tests where needed. If the suite becomes materially expensive, justify named routes
and fast/full separation from measured cost, preserving full acceptance scope. A single route registry
is retained as deferred [I-002](IDEAS.md#i-002--named-test-routes); no filters/route infrastructure exists now.

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

Run the existing locked restore, full Release build and full MTP solution command. Actual Windows
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

Run the unchanged locked restore, Release build and full solution test commands above. Optional focused
run: `dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "Seqvium.Tests.Graph*"`.
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
