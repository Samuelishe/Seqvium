# SEQ-R3-F2 — Internal workspace panes and layout persistence

Date: 2026-10-09. Current disposition: the owner accepts F1 and F2's current bounded usability and
interaction behavior. **R3 is complete / local accepted-ready** after SEQ-R3-CLOSE; see
[current state](../PROJECT_STATE.md) and
the [completion audit](../archive/AUDITS.md#seq-r3-close--workspace-shell-completion-audit).
F1/F2 are packages within R3, not numbered stages or R4 authority. The checkpoint sections below
retain original measurements, failures and then-pending acceptance statements for reconstruction;
those statements do not supersede this disposition or require another UI polishing cycle.
Original implementation baseline: clean `master`, HEAD `88f53891c3308c5ba5f956064213420fcbf7391f`, 308 passing tests;
R1/R2 complete / local accepted-ready, R3-F1 accepted desktop/presentation foundation.

## Resize hit targets and boundary arbitration

Baseline: clean HEAD `f20b44f8c2f7d4a79fc74b88c5d0916dff71ce06`, 396 tests. The owner confirms the earlier
geometry/chrome defects are corrected. The remaining question is whether thin borders can provide
usable pointer tolerance without competing handles taking neighbouring content or retargeting capture.

The correction keeps 1-DIP visible borders and existing content/header padding. `PaneGeometry` provides
6-DIP inward/outward edges (12 total) and corners extending 12 inward/6 outward (18x18). Pane-local
transparent resize overlays are removed. One narrow `WorkspaceHost` resolver uses arranged visible
rectangles, supported floating/dock operations and back-to-front order for hover and press. Control
sources/ancestors win; a visible surface excludes neighbouring outward targets; covered boundaries are
unavailable. Empty-gap candidates use boundary-point distance, then front order. Corners win within
their pane. A transient 1-DIP edge cue stays below higher panes, without activation or selection changes.
Press freezes identity/edges before activation and captures the existing anchored `PaneGesture`;
hover refreshes after stationary-pointer layout/order changes. Geometry cancellation, content focus,
retained instances and release-time layout persistence remain bounded by the existing lifecycle.

Touching independent panes and the current left/right docks resize only the chosen pane. Each visible
side owns its inward target; exact seams select the front pane. Nearby divider gaps use distance/front
order. Current half-workspace width limits remain independent. No shared splitter, coupled floating
resize, magnetic snapping, dock relationship, dock tree, project JSON, dependency or R1/R2 change is
introduced. [Workspace](../WORKSPACE.md#independent-aligned-and-shared-boundaries) distinguishes current
independence, future visual magnetic alignment and future explicit workspace-owned shared splitters.

Locked restore and full Release build succeed with **zero warnings/errors; 428 tests pass, zero
failures/skips**, preserving all 396 baseline cases. The 32 new cases cover DIP tolerance/corner priority,
nearest/tied boundaries in both orders, visible sides and occlusion, hidden/clipped regions, control
exclusion, inactive direct resize, identity stability through activation/reordering and independent dock
seams. Display-independent tests do not establish native pointer/cursor behavior.

Method: actual Windows 11 x64 Release application, one 1920x1080 monitor, window DPI 96, SDK 10.0.401 /
runtime 10.0.12 / Avalonia 12.1.3. UI Automation reads actual pane/control rectangles; user32 delivers
pointer/key input; `GetCursorInfo` is compared to native arrow, move, horizontal, vertical and diagonal
cursor handles. Reproduce after the locked Release commands below by opening the two optional panes,
testing offsets around each visible border, then touching/overlapping/gap layouts and left/right docks
at 1100x750 and 640x511. Repeat with each pane in front. Owner pointer-feel acceptance remains separate.

Completed correction runs pass **525 checks**: **113 tolerance**, **127 boundary/overlap/dock**, **262
control/language/theme/reduced-window**, **15 final lifecycle**, and **8 perimeter checks**.
The final complete tolerance run starts actual resize at offsets -6, -5, 0, +5 and +6 DIP on each
floating edge, with no press jump and exact anchored reversal. All four corners work 5 DIP outward
and 11 DIP inward; 7-DIP ordinary-edge offsets and 13-DIP title-corner offsets leave normal input.
Targets clipped at all four workspace boundaries remain usable; actual right/bottom corner resize
does not resize the OS window. Both initial front orders are exercised: touching floating
panes resize directly on either visible side, and gap ties choose the front pane without coupled resize.
Meeting and eight-DIP-separated dock dividers resolve and resize independently in both orders; actual
cursor matches capture. All eight floating directions repeat exact anchored forward/reverse cycles
at normal/reduced sizes across EN/RU and Dark/Light. Close/Collapse/Actions retain arrow cursors at
both tested button corners and their pointer presses invoke the actual actions. Current content is
read-only facts or preference buttons; future editable control classes have source-exclusion tests,
not an implemented editor GUI. Inspected local captures retain thin borders and readable compact chrome.
Final checks verify content identity/focus, Escape and actual-pointer hover after rollback, capture-loss /
deactivation/reflow cancellation, no preview file writes, no audio adapter load and joined normal shutdown.

Failures caught before final validation: the first pure occlusion rule excluded merely touching rear
edges; restricting covered-boundary exclusion to the higher rectangle's interior retains visible sides.
The actual exact dock seam initially displayed an arrow while one-DIP offsets displayed resize. A Rider
Release-process breakpoint at `WorkspaceHost.ResolveBoundary` observed `(320,100)`, `control=false`,
two 320-DIP dock rectangles, and a correctly resolved Inspector/Right target. `BoundaryMoved` showed
the actual source was Appearance (bounds `320,0,320,401`) with `Cursor=null`: Avalonia's half-open hit
route differs from inclusive deterministic seam arbitration. Applying the resolved direction cursor
through either routed pane fixes the mismatch without changing the chosen identity. Agent breakpoints
were removed; all eight existing user exception breakpoints and their enabled states were preserved.
Debugger evaluator limitations (optimized local/renderer-interface evaluation) were not treated as
successful evidence. Geometry setup also required saturating movement at zero to remove restored
fractional DIPs, and allowing floating-point roundoff in stored widths; live pixel anchor assertions stay
exact. Two later tolerance attempts failed a border-entry cursor and a press-geometry assertion; repeated
border entries passed, and a subsequent native read found the shared desktop pointer away from the
injected coordinate. Pointer contention is a possible automation limitation, not a proven cause of
those failures. A subsequent complete 113-check tolerance run and perimeter run pass. Failed runs
are excluded from completed totals and do not replace owner acceptance.

New driver/captures stay ignored at `.scratch/seq-r3-f2/resize-boundaries.ps1` and
`.artifacts/seq-r3-f2-boundaries/` for pending owner reproduction/review. These are local evidence, not
runtime resources or durable link targets; they may be removed after owner review. R3 remains **in progress / partial**
pending owner interactive
acceptance; no high/mixed DPI, other OS, screen-reader or full magnetic/shared-docking acceptance follows.

## Geometry and chrome correction

Correction baseline: clean HEAD `db51a2224ff0e0f845a69fa3780a86c1edb00fd6`, 346 passing tests.
The owner's reproducible resize/cursor defects invalidate any interpretation of the earlier smoke
as completed interactive acceptance. No attachment was available in this session; investigation used
the supplied reproduction descriptions, current source and actual Release Windows interaction.

Confirmed causes: `SetBounds` used the same rectangle clamp for placement and resize, shrinking the
available X/Y range as width/height grew and translating the opposite anchor. Dock resize wrote floating
width up to full workspace width while `Bounds` rendered only half, accumulating invisible size.
The entire bottom grip was diagonal, with no other floating edge/corner targets; the header's move
cursor inherited into action buttons. Absolute pointer overshoot also required backtracking before
the visible clamped edge could shrink. Close was presented as Hide despite ordinary × semantics.

`PaneGeometry` separates movement from anchored resize and `PaneGesture` retains original placement,
rectangle, pointer origin, selected edges and workspace bounds. Resize discards blocked pointer travel
on each axis; reversal changes the visible size immediately. Cancel restores the exact original
placement, including separate dock width, and ignores late events. Identical previews raise no state
change. Capture completion/cancel stays on the UI thread; layout commits on release, not movement.
Workspace reflow normalizes stored rectangles/widths rather than silently rendering different geometry.

`PanePlacement.DockedWidth` is independent of the whole floating rectangle, limited in state to half
workspace width and preserved in the optional version-1 user-file field. Legacy files initialize it
from floating width without fallback/write refusal. Dock/undock restores the last valid floating
rectangle, including width. No project serialization, canonical/Undo/audio or dependency changes.

At that checkpoint chrome had 5-DIP edge strips and 10x10-DIP corners with priority and matching cursors; the marker was
inside the bottom-right corner. Content/header/buttons are inset clear of resize regions. Docked panes
expose only the horizontal inner edge. Buttons explicitly own arrow cursors and source/ancestor exclusion
prevents capture from their visuals. Resize preserves existing content focus. Close tooltips/accessibility
are "Close pane: Project Inspector" and "Закрыть панель: Инспектор проекта"; Ctrl+W naming matches.
The existing `.hide` button automation ID, Hidden state and retained content identity are preserved.

Locked restore succeeds; final full Release solution build has **0 warnings / 0 errors**; **396 tests pass, 0 fail / 0
skip**, preserving all 346 baseline tests. The 50 added cases include 44
geometry/gesture/hit-region/localization cases, three actual Button/nested-visual source checks and
three independent/legacy dock-width storage cases. Anchors/reversal use exact rectangle assertions;
these do not claim actual OS cursors or capture behavior.

Actual Release GUI checks on Windows 11 x64 / 96 DPI: **359 passed** (85 geometry, 256 chrome/theme/
language/reduced-window, 18 final-binary focus/persistence/reflow/dock checks). UI Automation reads live
content rectangles during gestures; user32 delivers actual pointer/key input. `GetCursorInfo` handles
are compared to Windows arrow/move/horizontal/vertical/two-diagonal cursors at each corresponding area.
Checks include repeated separate and captured resize cycles at the right boundary, immediate reversal
after overshoot, left/right dock limits and undock retention, all eight resize directions, Escape,
deactivation/capture loss, OS-window independence and 640x511 reflow. RU/EN and Dark/Light retain exact
anchors/reversal; Close/Collapse/Actions have actual arrow cursors (including button corner points),
pointer/Enter/Space activation, visible RU/EN tooltips and accessibility names. Appearance content
identity/focus survives resize, Escape and close/reopen. Layout file timestamp stays unchanged during
pointer preview. Final shutdown joins writes; app exited with EN/Dark and optional panes hidden.

Automation limitations discovered and corrected: off-screen dock overshoot was clipped by Windows;
tooltip text is a descendant of the UIA ToolTip rather than its Name; pressed/focused button bounds
can differ by one pixel, so exact live pane rectangles are derived from the content surface instead;
main-window repositioning must use a stable client origin when comparing screen rectangles across
reflow. One attempted build during GUI execution failed because apphost was locked; after normal
GUI shutdown the final build succeeds without warnings. These failed attempts are not passed checks.

Inspected generated captures are local-only under ignored `.artifacts/seq-r3-f2-correction/`; the
ad hoc driver is `.scratch/seq-r3-f2/resize-correction.ps1`. They remain for owner reproduction/review,
not product assets or permanent tooling. Documentation targets/anchors, owner/status/scope and final
diff/whitespace are checked. No staging-index, commit or push action is performed.

R3 stays **in progress / partial**. The correction is not marked accepted-ready: the owner must
successfully repeat the previously failing interactions. Single-monitor/96-DPI automated GUI evidence
does not certify high/mixed DPI, cross-monitor, other OS, screen readers or subjective pointer feel.
R4 and broader docking/editor/domain work are outside this correction.

The sections below retain the original implementation's bounded observations; current correction
results and acceptance limits are those in the resize-target/boundary section above.

## Scope and ownership

Five new `Desktop/Workspace` files own pure state, retained host/chrome/content and user-file persistence.
App/MainWindow coordinate restore, preference/layout notices and joined shutdown; presentation catalogs/
styles retain the compact F1 direction. Two new `Tests/Desktop/Workspace*Tests.cs` files verify contracts.
No dependency, OS pane window, nested docking tree, musical editor, fake data or audio startup was added.
Core, audio, device, canonical project format and media semantics remain unchanged.

Implemented contracts are owned once in [Workspace](../WORKSPACE.md#implemented-r3-f2-internal-panes),
[Settings](../SETTINGS.md#implemented-r3-f2-workspace-layout-storage), [UX](../UX_CONTRACT.md) and
[UI design](../UI_DESIGN.md#implemented-r3-f2-pane-presentation). This report owns observations, not contracts.
Two optional singleton surfaces use stable instance/type IDs, independent of titles: actual read-only
Project Inspector and Appearance using existing preferences. Host-owned placement/visibility/front order
remain separate from content lifetime, focused control, canonical selection and musical command target.

## Environment and automated verification

Windows 11 x64 build 10.0.26300; one 1920x1080 display, actual window DPI 96. SDK 10.0.401,
runtime 10.0.12, Avalonia 12.1.3; framework-dependent Release executable. Existing xUnit v3 /
Microsoft.Testing.Platform setup is unchanged. No physical audio device was opened.

```powershell
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
```

Implementation and post-hygiene verification: locked restore succeeds; full Release build **zero
warnings/errors**; **346 passed, 0 failed, 0 skipped**, retaining all 308 baseline cases.
The 38 added cases cover stable identity/catalogs, 10,000 bounded activations/overlap, invalid/minimum/
oversize geometry and tiny/normal/large reflow, collapse/hide/traversal/disposal, deliberate docking/
permission/conflicts, restored collapsed dock intent and early-close snapshots. Store tests cover
roundtrip, corrupt/future/partial/bounded data, backup/write failures and pending-write coalescing/join.
Actual canonical Save, prepared WAV acceptance and preference changes verify document/media independence.

MSBuild compile-item inspection confirms inclusion of all five new production and two test files.
Source/test hashes before and after hygiene are unchanged; no test or reusable harness was deleted.
Affected Markdown relative targets/anchors, ownership/status and diffs are checked; `git diff --check`
passes. No raw test output is retained as repository evidence.

## Actual GUI observations and important failures

The real Release Windows application was launched, interacted with and restarted. Local UI Automation/
user32 input exercised actual controls, pointer capture and keyboard paths. Client-area screenshots
were visually inspected after layout settled; no opt-in arrangement became default product presentation.
Three completed runs and a final Release repeat passed **58 checks** (39 main, 6 restart, 13 additional):

- Exposed Inspector content activates above overlapping Appearance without losing content. Both headers
  drag and both floating grips resize independently; the main window does not resize with a pane.
- Collapse removes the working surface; strip restore returns the same control runtime identity and
  safe focus. Hide/reopen retains the actual Appearance theme-button identity, without duplicates.
- Explicit left/right dock, undock, occupied-edge displacement and right dock inner-edge resize work.
  Per-pane permission disables dock actions and undocks only that pane. No magnetic snapping exists.
- Main-window resize retains reachable controls at 1100x750, 640x511 and maximized 1920x1040.
  Restart restores floating/docked geometry, active entry and individual permission; close joins writes.
- RU/EN and Dark/Light retain layout, focused control and content. Space/Home/Enter invoke the pane menu;
  F6/Ctrl+Tab restore collapsed entries, Ctrl+arrows move, Ctrl+Shift+arrows resize, Ctrl+W hides and Escape/
  Tab leave safely. Hiding the last pane and About Escape return focus to available host controls.
- Escape and application deactivation cancel capture. Alt+Space native menu, Win+Up/Down, custom
  maximize/minimize/restore and Alt+F4 remain functional. No TopMost or Audio.Windows module is present.

Actual captures exposed a derived-Border style-key issue, fixed before completed runs. A runtime
breakpoint showed that initial synthetic resize input delivered Control rather than Control+Shift;
extended navigation keys corrected the driver, and resize passed. A final repeat also required an
80-ms cursor-settling pause before synthetic presses after an immediate-input resize assertion failed;
the full 39/6/13 sequence then passed with no production changes. These are automation-method limits,
not measured real-user latency requirements. The temporary breakpoint was removed and all eight
pre-existing exception breakpoint configurations preserved.

The app was closed after verification; EN/Dark restored and optional panes hidden. Normal user layout/
`.previous` files were created through real authorized operations; no musical project/recovery/media
was changed by the GUI exercise. High/mixed-DPI evidence was not fabricated.

## Visual observations and local review artifacts

All six captures are **local-only** under ignored `.artifacts/seq-r3-f2/`, retained solely for pending
owner visual review. They may be deleted after review and are neither product assets nor files promised
in every clone. The following inventory records observations; no Markdown link depends on a local PNG.

| Local filename             | Observed state / client dimensions                        |
|----------------------------|-----------------------------------------------------------|
| `dark-overlap.png`         | EN/Dark, two overlapping floating surfaces, 1100x750      |
| `dark-docked-floating.png` | EN/Dark, left Inspector and floating Appearance, 1100x750 |
| `collapsed.png`            | EN/Dark, Inspector reachable on strip, 1100x750           |
| `small.png`                | RU/Dark, reachable headers/actions, 640x511               |
| `light-multiple.png`       | RU/Light, real surfaces with unchanged layout, 1100x750   |
| `maximized.png`            | RU/Dark, 1920x1040 work area on a 1920x1080 display       |

Reduced height is 511, not exactly 500, due to existing minimum/native sizing. Typography was not globally
shrunk; only pane content scrolls locally. Thin 28-DIP headers, 1-DIP boundaries and conditional 28-DIP
strip preserve the compact frame. No third-party DAW image/assets were copied.

## Hygiene disposition and remaining limits

Authorized hygiene moved six untracked F2 PNGs out of docs and removed four tracked generated F1 PNGs (`dark.png`,
`light.png`, `maximized.png`, `small.png`) from the current tree, without rewriting history
or changing the index. F1 essential observations remain in its Markdown report. Two ad hoc local
verification scripts are preserved in ignored `.scratch/seq-r3-f2/`; the UI driver now writes captures
to the ignored artifact directory, not docs. It is not adopted as a permanent supported tool.

The existing R0/R2 protocols/reports remain: original realtime measurements retain unique reproducibility/
decision context, and F3/F4 have distinct source/device/lifetime acceptance evidence. F1 records the
desktop/framework/chrome/preference baseline; this concise F2 report supports pending review and current
state/persistence verification. No new standalone report was created for the hygiene iteration.
[Retention policy](README.md#evidence-retention) governs later consolidation/archiving, not a blind quota.

Evidence is Windows/single-display/96-DPI only: no changed scaling, mixed DPI, cross-monitor, Linux/macOS
runtime, screen-reader certification, crash/power-loss or delivered-package acceptance. Docking is two
edges and two singleton capabilities, not arbitrary instancing, drag-drop dock previews or a full IDE
framework. At theoretical tiny sizes available space takes precedence over preferred minimums.
R3 remains partial pending owner visual acceptance, full-stage audit and missing platform/accessibility/
delivered-shell evidence. R4, graph execution, musical editors, recording and plugin hosting are not started.
No package/lock, source/test, staging-index, branch/HEAD, commit or push change was made by hygiene.
