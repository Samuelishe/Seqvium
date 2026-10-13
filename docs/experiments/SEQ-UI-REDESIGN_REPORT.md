# SEQ-UI-REDESIGN review

Owner-directed interface redesign, implemented and locally verified on 2026-10-13.
This report records design choices, observable behavior and evidence. Visual approval remains with
the owner; it does not close R4/R5 or establish a distributed release.

## Result and design choices

Seqvium now uses an instrument presentation: slate chassis, jade actions, layered panels, bevelled
controls, purposeful shadow/light and colored channel identities. Dark/Light share structure and
interaction while using separately legible colors. The owner's explicit correction supersedes the
flat-only direction: complex decoration and substantial controls are valid design tools.

All eleven supplied images were inspected, including both SONAR views, FL Studio, EON ARP, the blue
arpeggiator, both Logic views, omniGRID, FabFilter Pro-Q 4, Serum and Reason. Transferable decisions
and usability limitations are recorded in [UI_DESIGN](../UI_DESIGN.md#seq-ui-redesign-reference-assessment).
The useful common language is tactile rhythm controls, beat grouping, contextual depth, visual
musical overview and coherent instrument materials. Tiny hit targets, cryptic repeated captions and
unbound parameter inventories were not transferred. No layout or proprietary asset was copied.

[DESIGN_SYSTEM v2](../DESIGN_SYSTEM.md) catalogs HostPalette/HostMetrics/HostMaterials and shared
styles. Existing standard fields, menus, dialogs, graph, Inspector and Appearance inherit them.
Workspace panels and graph nodes gain depth; own vector icons supply the navigation rail.
The startup Rack, wide-window Pattern navigator and transport group expose music directly.
Saved user layout still controls restoration; startup creates no Pattern or sound.

## Functional changes and preservation

- Sixteen larger sculpted pads per channel have a common musical ruler, four beat groups, channel
  tint, checked indicator and independent focus frame. Short commands and contextual row actions
  replace repeated labels. Narrow panes scroll locally without microscopic scaling.
- Captured left dragging paints/erases a continuous row, including skipped cells. Only release
  commits, with one Undo; Escape/capture loss/deactivation/stale authority cancels. Fill every four/
  two, shift left/right and Clear use the same canonical row transaction. No-op changes add no history.
- Add sound and single-local-WAV drop use the existing durable import and safe graph composition.
  Drop highlights the actual target; multiple files are refused. Imported channel removal is undoable.
- Pattern navigator creates empty Patterns and selects real occurrences with canonical note previews.
  Selection does not rewrite music or add history. Below the wide-layout breakpoint it yields space
  to the workspace, with retained selection controls available elsewhere.
- BPM opens a committed transport popup and remains editable in narrow overflow. Decimal locale is
  frozen for the popup; invalid/out-of-range/unrepresentable values stay local. Lifecycle/generation
  gates refuse stale application. An accepted tempo edit preserves musical-time data and stops the
  finite execution; one edit creates one Undo.
- Gain's graduated physical dial shares the existing Node Editor draft and exact amplitude parameter.
  Relative vertical drag previews, Shift adjusts finely, release commits once; Escape/capture loss
  cancels, double-click restores unity and arrows/Home/End remain available. Slider/exact amplitude/
  dB and optional analytical Curve remain. Curve fill/light visualize the setting, not measured audio.

Project schema, independent reader/writer markers, graph execution, DSP, scheduler and audio lifetime
are unchanged. Unsupported notes/unknown data and unsafe/shared channel expansion remain preserved
and refused. Musical edits still have one ProjectDocument/history/import authority. Existing keyboard,
Save/Open, pane docking/collapse/hide/reopen and silent creation/replacement remain intact.

## Verification

Environment: Windows 11 x64, .NET SDK 10.0.401/runtime 10.0.12, Avalonia 12.1.3, xUnit v3 MTP.

| Check | Recorded result | Scope |
| --- | --- | --- |
| Affected ordinary Release builds | Passed, zero warnings/errors before output lock | Desktop, Tests and reusable VisualCheck references |
| Final isolated Release Tests/references build | Passed, zero warnings/errors; 5.27 s | Complete production apphost, test/tool outputs under the task output root |
| Final focused L1/L2 | 435/435; zero failures/skips; 19.705 s wall | 19 named Desktop presentation/interaction/history/metadata classes; not full suite |
| Final warning-copy/resource subset | 82/82; zero failures/skips; 3.885 s wall | HostPresentation, DesignSystem and StepSequencer after the last copy edits |
| Shared component catalog | Passed | Dark/Light controls, field centers, invalid/disabled states and central radius/fill propagation |
| Actual full-window matrix | 24 rows passed; common field alignment passed | RU/EN × Dark/Light normal/reduced/minimum/maximized and pane sizes, owned modal/details; latest isolated binaries |
| Rack renders | Eight ordinary/reduced variants passed | 48 accessible checked/off pads, aligned name/action, focus, readable hit geometry and local scrolling |
| Rack/control routed actions | Passed | Paint/erase/cancellation, row popup, dial/tempo/narrow overflow, navigator, WAV drop/Undo/multi-file refusal, retained lifecycle and Save/Open |
| Dense authored fixture | Passed in Dark and Light | Eight imported channels, 128 pads, canonical rhythms; ninth channel refused |
| Guarded native Rack input | Four checks passed | Real Windows click/focus, captured skipped-cell paint with one Undo and native Escape cancellation |
| Guarded native graph/Workspace | Eleven checks passed in the earlier matrix | Micro select, background deselect/pan, pane activation, body double-click, three zoom drag cases, Escape and arrows |
| Latest matrix native attempt | Refused before input on foreground guard | Render/routed results passed; refusal is not native evidence and does not invalidate the separately recorded earlier checks |

Nine new deterministic cases cover atomic row gestures/operations, note identity/no-op/Undo/ABA,
unsupported-row refusal and canonical tempo preservation/stale/range boundaries. Existing tests were
preserved. The 435 cases include metadata/About identity for BUILD `0.1.0.0005`. The final 82 cases are
a subset, not 82 additional unique cases. No full regression, acoustic or platform acceptance is inferred.
Focused scope follows [TEST_EXECUTION](../TEST_EXECUTION.md#required-verification-levels); last L3
remains the 933-case foundation finalize baseline. Cadence advances from two to three completed tasks.

### Reproduce builds and focused checks

Ordinary output can be used when the user's Desktop is closed. A user opened the normal executable
during refinement, locking its apphost and later a dependent DLL. Complete final output was therefore
built separately; no user process was terminated. The ordinary builds failing to copy locked files
are not compilation failures or green build receipts. A global UseAppHost=false attempt also failed
because xUnit v3 requires an apphost; it was replaced with this successful isolated-output command:

```powershell
$taskOutput = Join-Path (Get-Location) '.artifacts/seq-ui-redesign/release/'
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore "-p:BaseOutputPath=$taskOutput"
```

This derives the recorded absolute output root from the checkout. The final scoped test command uses the same
output root; xUnit tests are in `Seqvium.Tests`, not a namespace derived from their Desktop directory:

```powershell
$classes = @('StepSequencer', 'DesignSystem', 'HostPresentation', 'DesktopFoundationGesture', 'DesktopInstance', 'NodeEditor', 'DesktopWorkflow', 'WorkflowPresentation', 'WorkspaceState', 'WorkspaceLayout', 'WorkspaceBoundary', 'PaneGeometry', 'PaneChrome', 'GraphCanvasGeometry', 'GraphEditorWorkflow', 'GraphEditorSession', 'GraphMenu', 'GraphConnectionLabel', 'ProductVersion')
$filters = foreach ($class in $classes) { '--filter-class'; "*$($class)Tests" }
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore "-p:BaseOutputPath=$taskOutput" @filters --report-xunit-trx --report-xunit-trx-filename seq-ui-redesign-final.trx --results-directory .artifacts/seq-ui-redesign/tests
```

The initial invalid middle-wildcard filter and directory-derived namespace selected no tests; those
runs were corrected and are not passes. The xUnit runner's exact named-class scope above passed.
Reusable [VisualCheck](../../tools/Seqvium.VisualCheck/README.md) commands use the final isolated DLL:

```powershell
dotnet .artifacts/seq-ui-redesign/release/Release/net10.0/Seqvium.VisualCheck.dll catalog "$PWD/.artifacts/seq-ui-redesign/catalog"
dotnet .artifacts/seq-ui-redesign/release/Release/net10.0/Seqvium.VisualCheck.dll steps "$PWD/.artifacts/seq-ui-redesign/steps"
dotnet .artifacts/seq-ui-redesign/release/Release/net10.0/Seqvium.VisualCheck.dll matrix "$PWD/.artifacts/seq-ui-redesign/matrix"
```

These are isolated actual production controls and authored fixtures; no user project/layout/media is
edited. Native injection always checks the exact foreground handle. Synthetic routed events and
automation are labelled separately. Images were inspected in both themes and at reduced sizes.

### Evidence and iteration corrections

Raw outputs/TRX live under ignored `.artifacts/seq-ui-redesign/` with existing rolling retention.
Selected images and essential JSON receipts were copied to the owner's local reference `Review`
folder so this review survives automatic rotation. Reports retain observations independently of PNGs.
Useful receipts include `seq-ui-redesign-final.trx`, `seq-ui-redesign-copy-review.trx`, `steps.json`,
`redesign-controls.json`, `catalog.json`, `matrix.json` and `native-progress.json`.

Intermediate checks caught a gradient collection-constructor mismatch, obsolete metric expectations,
an ambiguous first-ScrollViewer lookup that selected the name input rather than the rack, and a wrong
MenuItem automation-provider assumption. These were fixed in implementation/harness. The narrow BPM
label-only path was replaced by reparenting the retained real controls and verified through its popup.
No check was dropped to hide a failure. Foreground guard refusals remain explicit. Protected legacy/
reparse evidence was reported by retention and preserved; unrelated user artifacts were not deleted.

All eleven original images were moved individually from Desktop/screens to
`%LOCALAPPDATA%/Seqvium/DesignReferences/SEQ-UI-REDESIGN` as requested. The eight Screenshot_N files
retain their names; three long JPG names become FabFilter-Pro-Q-4, Serum and Reason-Rack, with original
names/hashes in a local manifest. Reference/review bitmaps are outside Git, not embedded product assets.

## Remaining limits

The Rack still supports one finite 4/4 bar and eight WAV channels. Loop, actual moving play position,
seamless live edits, velocity lane, channel mute/solo/pan, browser/audition and real spectrum/EQ need
musical/runtime ownership; their dependencies are in
[ROADMAP](../ROADMAP.md#instrument-ui-follow-up-after-seq-ui-redesign). No unbound future controls
are presented as working instruments. Meter editing, longer Patterns and multi-file import remain
separate scope. The singleton Node Editor does not yet support simultaneous independent instruments.

This task exercises no physical audio/listening/native file picker or native Explorer drag/drop.
Existing deterministic finite PCM evidence is retained; historical pressure failures are unresolved.
High/mixed DPI, cross-monitor, screen reader and Linux/macOS runtime remain unevidenced. Window
minimum follows existing 640×480 client geometry; captured outer pixels may include native frame size.
The user-owned running Desktop retains its loaded binary; close it before launching the final isolated
executable, or rebuild ordinary output after closing it. Its projects and session were preserved.

One BUILD checkpoint advances to `0.1.0.0005`. No dependency, release, commit, push, staging or Git
history mutation was performed. Final visual judgement remains with the owner.

L0 final review resolves 1,015 relative links/anchors in changed Markdown; whitespace/diff checks
pass. Current owner/public-guide scope distinguishes shipped interactions from planned engine work.
Untracked files are four intentional production controls/material owners plus this evidence report;
reference/review PNGs, authored fixtures, TRX and isolated executable outputs remain outside Git.
