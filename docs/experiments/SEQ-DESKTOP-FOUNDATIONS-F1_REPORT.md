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
| Native mouse subset | Passed | **Five** checks in the final RU/Light ordinary window: micro selection without Undo, graph background clear with retained target, pan, workspace deactivation/focus, pane reactivation |
| Full native input matrix / catalog screen attempts | Partial | Earlier matrix attempts and six catalog screen captures were refused; native double click/drag/keyboard/all-theme input coverage is not claimed |
| Markdown relative links / anchors, ownership and diff | Passed | Changed documentation; source names/owners and decision states reviewed |
| User app, settings and projects | Preserved | No user process terminated; isolated settings/layout/media; early harness-only media cleaned precisely |
| Physical audio, high/mixed DPI, Linux/macOS GUI/runtime, elevation/multiple-user/session matrix | Not run | No wider support, audio/visual owner acceptance or security certification inferred |

The root commands remain in [TEST_EXECUTION](../TEST_EXECUTION.md). Reusable GUI/process commands
are in [VisualCheck](../../tools/Seqvium.VisualCheck/README.md). When a user process holds the ordinary
apphost, an absolute `BaseOutputPath` under `.artifacts/desktop-f1/release/` permits a complete Release
build and the same full suite with that property; it does not change canonical version metadata.
The suite passed in that output and again in normal output after the user closed their app.

Local retained evidence is under `.artifacts/desktop-f1/`: final isolated `release/`,
`tests-complete/`, `catalog-complete/`, `matrix-complete/` and `processes-final/` (earlier isolated runs remain for failure provenance).
These are ignored technical artifacts, not public README images or clone requirements. Renders
come from actual running windows/popups; capture JSON distinguishes them from optional Windows screen
pixels. Earlier screen/pointer attempts were refused; the final matrix obtained foreground naturally and passed 40 screen captures plus five actual mouse checks. Catalog pseudo states and routed input remain synthetic, and the broader native input matrix is incomplete.

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

At verification: `master`, unchanged HEAD `8e3542ea16adbe8afcc2faf1aee409b85515cd44`, **41 modified tracked files, 21 new untracked source/test/tool/document files**, no staged changes and no pre-existing worktree changes. Every listed path belongs to this task. Generated evidence is ignored; source, locks and reusable checks must be retained. This is an observed snapshot, not a replacement for live Git status.

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
