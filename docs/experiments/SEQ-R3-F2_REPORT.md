# SEQ-R3-F2 — Internal workspace panes and layout persistence

Date: 2026-10-09. Status: implemented / locally verified; owner visual acceptance pending.
F2 is a bounded package within **R3 in progress / partial**, not another stage or R4 authority.
Baseline: clean `master`, HEAD `88f53891c3308c5ba5f956064213420fcbf7391f`, 308 passing tests;
R1/R2 complete / local accepted-ready, R3-F1 accepted desktop/presentation foundation.

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
