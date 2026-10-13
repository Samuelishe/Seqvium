# SEQ-DESKTOP-FOUNDATIONS-F1 verification

## Scope and environment

Implemented four sequential packages: Workspace input/activation, Design System v1, Desktop Single
Instance, and reusable visual/process verification. Starting repository: clean `master`,
`8e3542ea16adbe8afcc2faf1aee409b85515cd44`, product `0.1.0.0002`. Windows 11 x64,
.NET SDK 10.0.401 / runtime 10.0.12, existing pinned Avalonia desktop dependencies. No Core schema,
DSP/backend, plugin ABI, R5/R6 or new musical feature is changed. No audio device is opened by these checks.

## A. Workspace UX

Free main Workspace left click clears only active pane identity and focuses its canvas. Pane placement,
front order, graph selection, explicit Node Editor target and playback target stay independent.
Nullable activation survives layout restoration and hiding another pane. Clicking pane content
reactivates it. Resize-boundary arbitration keeps its existing priority.

Graph background click clears Node/Connection selection on release; background drag pans without
clearing selection. Middle/Space pan remain separate. A screen-space DIP threshold absorbs micro
movement before node/wire drag or pan. Equality remains a click; crossing stays a drag until release.
The selected distance is below the existing port hit radius; zoom affects graph displacement only
after crossing. Body double click retains the same lifecycle/target/generation checks. Ordinary port
click no longer attempts an invalid self-connection. Drag commits once; micro/no-op/cancel create no
Undo. Exact arrow/Shift-arrow movement retains its own graph-unit steps.

Pure tests verify geometry/order/restoration and click/history behavior. Actual-window routed checks
cover micro/body double clicks, one-Undo drag at multiple zooms, capture loss, Escape, background pan,
port precedence/cancellation, middle/Space pan, exact keyboard movement, workspace focus/deactivation/
reactivation and dock/collapse/hide/reopen with retained target/geometry. These are explicitly
synthetic routed events on actual controls, not native Windows mouse input.

## B. Design System v1

[DESIGN_SYSTEM](../DESIGN_SYSTEM.md) catalogs source-owned colors, geometry, spacing, typography,
controls and exception rules. HostControls supplies implicit first-party defaults and meaningful
button/parameter/status/chrome variants. HostFluentResources aliases installed template states to
the same semantic roles; it defines no independent colors. HostVisual resolves live resources for
GraphCanvas and Gain drawings, with resource-change invalidation. Unselected node boundaries have
both-theme contrast checks independent of selection. Gain track/fill/thumb share Slider roles.

Toolbar Gain's former top-aligned value came from NumericUpDown forwarding its parent alignment to
the inner TextBox. The common style now centers that parent. Parameter rows center labels, editable
text, units and actions. The actual TextPresenter measurement has **0 DIP center difference for
toolbar Gain in RU/EN × Dark/Light**; Graph quick Gain and Node Editor stay within the one-DIP check.
No per-window top-offset compensation is used. Future UI routing/guidelines require this catalog
and rendered field check.

The catalog temporarily changes common button radius and Slider fill, verifies actual template parts
and drawn Gain consumers, then restores accepted values in `finally`. Disabled/invalid semantic colors
remain exceptions. The real main-window matrix changes Graph.Node plus ordinary button radius and
checks toolbar/pane consumers; square window/pane chrome remains square. Magenta is an artificial
**test-only resource marker**, rendered by the real controls, absent from the production palette/UI.
The checked-in catalog/matrix host owns the temporary override; there is no production UI toggle.

Specialized graph coordinates, content widths, hit-test geometry and pane composition remain local
where they have actual behavior ownership. Native pickers, independently rendered external editors
and the pre-framework ownership-failure diagnostic retain justified platform boundaries.

## C. Single Instance

Desktop Lifecycle obtains a current-user named Mutex before Avalonia/preferences/document/audio
initialization. Scope is user-wide across sessions because preferences/layout are user-wide; different
users have separate names/access. The entry-point thread owns/relinquishes the lease. OS abandonment
permits recovery after a killed primary without lock-file deletion.

Activation-only local Named Pipes restrict access to the current user. Protocol requests and client
connection/response waits are bounded. MainWindow readiness is awaited; activation dispatches ordinary
Show/restore/Activate once, respecting foreground restrictions. A missing/stalled primary response
ends the secondary with a diagnostic and no document/audio. Shutdown cancels and joins IPC before
releasing ownership. No forced termination or foreground capture is implemented in production.

Independent Windows processes through the same RunDesktop path verified one document initialization,
secondary success/exit, restoration of the primary's minimized window, test-owned crash/restart and
graceful joined exit. Separate no-IPC/stalled hosts verified bounded refusal and zero secondary
documents. Pure tests also cover readiness, cancelled/invalid clients and listener survival.

VisualCheck alone composes an internal isolated identity and settings/layout/media root. Production
has no public flag/environment bypass. Tests, DeviceCheck, ProjectStats and experiments do not obtain
ownership merely by loading Desktop. Project-opening IPC, external project-file writer protection,
different-user/elevation/session runtime coverage and Linux/macOS acceptance remain separate work.

## D. Validation matrix

| Check | Result | Scope / limitation |
| --- | --- | --- |
| Locked solution restore | Passed | All seven projects; existing dependencies, new tool lock |
| Full Release solution build | Passed | Zero warnings/errors; isolated output also verified while a user Desktop held the normal apphost |
| Complete suite | Passed | **922 tests, zero failures/skips**, preserving 908 baseline cases |
| New focused foundation cases | Passed | **14** gesture/resource/instance cases; version metadata cases remain in the full suite |
| Actual component catalog | Passed | Dark/Light states, controlled common-resource change/restoration, actual open menu/tooltip renders |
| Main-window render matrix | Passed | **24** RU/EN × Dark/Light window/pane compositions: ordinary, reduced, minimum, maximized, pane minimum and pane expanded |
| Rendered field alignment | Passed | **32** measurements: toolbar, Graph quick Gain and Node Editor; no difference above one DIP |
| Owned modal rendering | Passed | RU/EN × Dark/Light, content-sized shared styling; no native picker claim |
| Music/theme/layout isolation | Passed | Snapshot/Undo and explicit target unchanged by presentation changes; synthetic saved fixtures only |
| Actual-window routed gestures | Passed | Declared synthetic controls/capture tests above, distinct from OS input |
| Two independent Desktop processes | Passed | One document; activation/minimized restore; secondary exits |
| Primary crash/restart and joined shutdown | Passed | Only isolated test-owned processes killed; next primary starts normally |
| No listener / stalled activation | Passed | Bounded diagnostic/exit, no secondary document |
| Old layouts and inactive restoration | Passed | Existing layout suite plus nullable activation regression; no schema migration |
| Native screen captures | Passed | **40 actual Windows screen captures**, with exact foreground guard; all window/pane/field/modal/central-change compositions |
| Early native mouse subset | Passed | **Five** checks in `matrix-complete/matrix.json` (also `gestures-all`): micro selection without Undo, graph background clear with retained target, pan, workspace deactivation/focus, pane reactivation |
| Final bounded native mouse/keyboard run | Passed | **Eleven** checks in `native-complete/matrix.json`: those five, Source/Gain body double click, threshold/one Undo/exact displacement/restoration at zoom 0.5/1/2, Escape cancellation, exact arrow movement at zoom 2 with Undo |
| Full native input matrix / catalog screen attempts | Partial | Earlier matrix attempts and six catalog screen captures were refused; final bounded double click/drag/keyboard evidence exists; complete all-theme/size native input coverage is not claimed |
| Markdown relative links / anchors, ownership and diff | Passed | Changed documentation; source names/owners and decision states reviewed |
| User app, settings and projects | Preserved | No user process terminated; isolated settings/layout/media; early harness-only media cleaned precisely |
| Physical audio, high/mixed DPI, Linux/macOS GUI/runtime, elevation/multiple-user/session matrix | Not run | No wider support, audio/visual owner acceptance or security certification inferred |

The root commands remain in [TEST_EXECUTION](../TEST_EXECUTION.md). Reusable GUI/process commands
are in [VisualCheck](../../tools/Seqvium.VisualCheck/README.md). When a user process holds the ordinary
apphost, an absolute `BaseOutputPath` under `.artifacts/desktop-f1/release/` permits a complete Release
build and the same full suite with that property; it does not change canonical version metadata.
The suite passed in that output and again in normal output after the user closed their app.

Local retained evidence is under `.artifacts/desktop-f1/`: `tests-complete/`, `tests-final-normal/`,
`catalog-complete/`, `matrix-complete/`, `native-complete/` and `processes-final/`; original failed/
incomplete attempts remain for provenance. The temporary isolated `release/` build copy and redundant
completed catalog/TRX outputs were removed in the finalize checkpoint; normal source build outputs
remain reproducible. Retained raw matrix/native evidence has an explicit preservation reason.
These are ignored technical artifacts, not public README images or clone requirements. Renders
come from actual running windows/popups; capture JSON distinguishes them from optional Windows screen
pixels. Earlier screen/pointer attempts were refused; the final matrix obtained foreground naturally and passed 40 screen captures plus the early five-check mouse subset. The subsequent shorter
`native-complete` run confirms eleven mouse/keyboard checks, with `AudioStarted: false`. Catalog pseudo states and routed input remain synthetic, and the broader native input matrix is incomplete.

## Intermediate defects and corrections

- The first catalog assertion incorrectly included the deliberately red invalid Slider in the
  temporary fill override. The corrected check verifies both shared propagation and preserved error state.
- Early pointer fixtures used a coordinate that hit a fitted node, and routed constructors initially
  used the canvas instead of its actual visual root. The harness now validates hits/root transforms;
  the complete routed checks pass.
- A temporary graph-color demonstration changed App theme without its shell preference, so an ordinary
  refresh restored the preference's theme. The final host changes the actual preference consistently
  before overriding/restoring a resource.
- An early host closed the workflow before MainWindow's own joined close, preventing normal host exit.
  The final host saves its isolated fixture and lets the real MainWindow own shutdown.
- Early imports used the production unnamed-media default. The internal host composition now supplies
  isolated media too; the three generated files/empty directories were matched to task fixture locators
  and removed without touching user media. An automatic review rejected broad computed cleanup; explicit
  verified file/empty-directory operations completed safe cleanup.
- The ordinary apphost was locked by a separately launched user Desktop. That process was preserved;
  isolated-output build/tests passed, and normal output was rebuilt after it was closed by the user.

## Checkpoint and remaining acceptance

One BUILD checkpoint: **`0.1.0.0003`**. This is substantial Desktop infrastructure inside the existing
musical capability line, so PATCH remains 0 and MAJOR/MINOR are unchanged. Intermediate builds consumed
no increments. F2 remains partial; F3-B and active Gain/general visual acceptance remain with the owner.
Q-032, Q-055 and broad recovery/platform questions are not closed. No commit, staging, push or tag.

## File structure and final worktree snapshot

The main new owners are `Desktop/Lifecycle/` (process ownership), `Presentation/HostFluentResources`/`HostVisual` (resource integration), `Workspace/GraphDragThreshold` (screen input), the three focused Desktop test classes, `tools/Seqvium.VisualCheck/` (reusable host/scripts), and `docs/DESIGN_SYSTEM.md` (catalog). Existing shell/pane controls and canonical documents are updated in place.

Original F1 worktree snapshot (historical, before the finalize checkpoint): `master`, unchanged HEAD `8e3542ea16adbe8afcc2faf1aee409b85515cd44`, **41 modified tracked files, 21 new untracked source/test/tool/document files**, no staged changes and no pre-existing worktree changes. Every listed path belongs to this task. Generated evidence is ignored; source, locks and reusable checks must be retained. This is an observed snapshot, not a replacement for live Git status.

```text
 M AGENTS.md
 M Directory.Build.props
 M README.md
 M Seqvium.sln
 M docs/ARCHITECTURE.md
 M docs/CODING_GUIDELINES.md
 M docs/DEVELOPMENT.md
 M docs/DOCUMENTATION_GOVERNANCE.md
 M docs/INDEX.md
 M docs/PORTABILITY.md
 M docs/PROJECT_STATE.md
 M docs/SETTINGS.md
 M docs/TEST_EXECUTION.md
 M docs/UI_DESIGN.md
 M docs/UX_CONTRACT.md
 M docs/VERSIONING.md
 M docs/WORKSPACE.md
 M docs/archive/WORK_LOG.md
 M docs/experiments/README.md
 M src/Seqvium.Desktop/App.axaml
 M src/Seqvium.Desktop/App.axaml.cs
 M src/Seqvium.Desktop/MainWindow.Workflow.cs
 M src/Seqvium.Desktop/MainWindow.axaml
 M src/Seqvium.Desktop/MainWindow.axaml.cs
 M src/Seqvium.Desktop/Presentation/HostLocalizer.cs
 M src/Seqvium.Desktop/Presentation/HostMetrics.cs
 M src/Seqvium.Desktop/Presentation/HostPalette.cs
 M src/Seqvium.Desktop/Program.cs
 M src/Seqvium.Desktop/Seqvium.Desktop.csproj
 M src/Seqvium.Desktop/Styles/HostControls.axaml
 M src/Seqvium.Desktop/Workflow/UnsavedProjectDialog.cs
 M src/Seqvium.Desktop/Workflow/WorkflowControls.cs
 M src/Seqvium.Desktop/Workspace/FirstPartyPaneContent.cs
 M src/Seqvium.Desktop/Workspace/GainEditorControls.cs
 M src/Seqvium.Desktop/Workspace/GraphCanvas.cs
 M src/Seqvium.Desktop/Workspace/GraphPaneContent.cs
 M src/Seqvium.Desktop/Workspace/NodeEditorPaneContent.cs
 M src/Seqvium.Desktop/Workspace/WorkspaceHost.cs
 M src/Seqvium.Desktop/Workspace/WorkspacePane.cs
 M src/Seqvium.Desktop/Workspace/WorkspaceState.cs
 M tests/Seqvium.Tests/Desktop/HostPresentationTests.cs
?? docs/DESIGN_SYSTEM.md
?? docs/experiments/SEQ-DESKTOP-FOUNDATIONS-F1_REPORT.md
?? src/Seqvium.Desktop/Lifecycle/DesktopInstance.cs
?? src/Seqvium.Desktop/Lifecycle/DesktopLaunch.cs
?? src/Seqvium.Desktop/Lifecycle/DesktopStartupDiagnostic.cs
?? src/Seqvium.Desktop/Presentation/HostFluentResources.cs
?? src/Seqvium.Desktop/Presentation/HostVisual.cs
?? src/Seqvium.Desktop/Workspace/GraphDragThreshold.cs
?? tests/Seqvium.Tests/Desktop/DesignSystemTests.cs
?? tests/Seqvium.Tests/Desktop/DesktopFoundationGestureTests.cs
?? tests/Seqvium.Tests/Desktop/DesktopInstanceTests.cs
?? tools/Seqvium.VisualCheck/Check-Processes.ps1
?? tools/Seqvium.VisualCheck/ComponentCatalog.cs
?? tools/Seqvium.VisualCheck/FieldAlignment.cs
?? tools/Seqvium.VisualCheck/NativePointer.cs
?? tools/Seqvium.VisualCheck/NativeScreen.cs
?? tools/Seqvium.VisualCheck/Program.cs
?? tools/Seqvium.VisualCheck/README.md
?? tools/Seqvium.VisualCheck/RoutedGestures.cs
?? tools/Seqvium.VisualCheck/Seqvium.VisualCheck.csproj
?? tools/Seqvium.VisualCheck/packages.lock.json
```

## Completion and reliability audit

SEQ-DESKTOP-FOUNDATIONS-F1-FINALIZE, 2026-10-13: initial `master` HEAD
`b0a5c6dc765a66875f1422132e80b35a5f9bafd4`, clean worktree. Retained `tests-complete` and
`tests-final-normal` TRX each confirm 922/922, no failures/skips. `matrix-complete/matrix.json`
confirms the early five native checks; `native-complete/matrix.json` confirms all eleven listed
above, independently of the wider render matrix. These original outputs were inspected without
rerunning the full GUI/native matrix. State, version ledger and archived work now agree.

The developer host formerly lacked a scenario-wide deadline and could lose exceptions during
final save/close outside its handler. The independent watchdog now bounds startup, awaited commands,
GUI shutdown and listener disposal; timeout writes `failure.txt` and exits 124 in its own isolated
host. The GUI scenario is an observed Task, save failure is asserted, close exceptions are recorded
and premature GUI exit fails. Native progress is persisted after each check and on refusal/error;
routed evidence survives a later native failure. Synthetic capture is always released; native
release cleanup retains the exact foreground guard. Process verification now bounds crash/cleanup
joins, disposes Process handles and retains child stdout/stderr plus partial results on failure.

Source audit of DesktopInstance/DesktopLaunch/Program/App/MainWindow confirms existing ownership
before configuration/document/audio, current-user pipe access, bounded connection/readiness/request
waits, cancelled/joined IPC, entry-thread mutex release, abandonment recovery, isolated media and
normal workflow/layout/preference shutdown joins. Expected persistence failures are translated by
the existing stores. No production defect was reproduced requiring a lifecycle rewrite or product
change. Framework GUI callbacks remain under their existing ownership; this bounded audit does not
claim arbitrary disk/driver failures cannot delay user shutdown. Yesterday's ten-minute delay was
not reproduced or attributed. Production behavior/version is unchanged at `0.1.0.0003`.

The complete local inventory covered every artifact producer before deletion. Reviewed SDK build
copies/caches contained dependency/runtime manifests or NuGet assets and auto-generated `obj` source,
with no user projects/media and no active artifact owner. Dry-run listed exact paths/bytes/reasons;
apply removed `desktop-f1/release`, `node-editor-f1/bin/obj`,
`seq-r4-f3-b-crossing/GuiHost/bin/obj`, `seq-r4-f3-b-ux-f1/bin/obj`, and redundant
`desktop-f1/catalog-final`, `catalog-verified`, `tests`. Repeat apply deleted nothing.

| Inventory snapshot | Bytes | Files | Directories |
| --- | ---: | ---: | ---: |
| Before | 2,762,128,802 | 1,886 | 359 |
| After cleanup and validation outputs | 397,513,935 | 1,468 | 194 |

Net storage freed: **2.202 GiB**; ten deleted directory roots accounted for
2,365,983,239 bytes including adopted markers. Inventory snapshots and reviewed plans/logs
are local under `.artifacts/finalize-audit/`; metadata/report writes explain the small difference
between gross deletion and net storage change. R4-F2 raw pressure traces (323,538,249 bytes),
F3-A/F3-B project/media and About evidence retain their original file counts and bytes.
Owner review renders, distinct failures, temporary harness source and unknown provenance remain.
A synthetic junction fixture is intentionally left at
`finalize-audit/junction-check`: dry-run/apply preserved its external-for-fixture sentinel; automatic
approval review rejected unlink with `blocked by policy`. No alternative bypass was attempted;
the junction target stays inside the agreed artifact tree, and inventory does not follow it.

[Rolling retention v1](README.md#rolling-retention-v1) now executes automatically at VisualCheck
GUI/process lifecycle boundaries, with manifests, active-owner/lease protection, deterministic
reservations/rotation, explicit legacy review and a bounded local journal. Numeric policy settings
and manual command links stay in that owner. Unknown and exceptional evidence are retained explicitly.

Validation: locked restore; final full Release build with zero warnings/errors; **933/933** tests
(no failures/skips), preserving all 922 baseline cases; focused **16/16** (11 new retention/watchdog
and five existing instance cases). Small fixtures cover age/count/budget, reservations, dry-run,
legacy, repeated apply, active/incomplete/pinned owners, invalid/nested manifests, links/external
paths, file locks/partial access and synthetic ACL denial. Additional real Windows fixtures verified
junction protection and an active isolated host referenced by command line even with manifest PID 0;
it rotated only after graceful owner exit. Real short startup/close and four process checks pass,
and the watchdog produced exit 124 plus failure diagnostics. No captures/native input in the normal
smoke. Full GUI/native matrices, physical audio and other-OS acceptance were deliberately not rerun.
An initial slow module-enumeration approach was replaced with one bounded process snapshot; the
generic unsupported TRX flag was corrected to xUnit's `--report-xunit-trx` before the actual full run.

R4-F2 stays partial; R4-F3-B and active Gain/general visual acceptance stay with the owner. Full native
input and physical audio remain separate. Git HEAD/index/history are unchanged; this checkpoint
changes developer tooling/tests and documentation only.
