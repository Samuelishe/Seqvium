# SEQ-R9-PIANO-TOOLS-F1 — Quantize and Scale Highlighting

Date: 2026-10-19, Windows x64, SDK 10.0.402/runtime 10.0.13, MTP/xUnit v3.
Baseline: clean `master`, `fb17788f567167c2010af389c2f48418ea12108c`, product 0.1.16.0006.
Checkpoint: **0.1.16.0007**, one BUILD within the existing Piano editor capability.
This report owns observations; [UX](../UX_CONTRACT.md#implemented-seq-r9-piano-roll) owns behavior.

## Musical result and exact arithmetic

The explicit Quantize action changes only the selected active Part's `NoteEvent.Position`.
For nonnegative start `t` and Snap unit `u`, write `t = q*u + r`, `0 <= r < u`.
The result is `q*u` when `2*r < u`, otherwise `(q+1)*u`. Production uses the existing decimal/integer
`PianoNotes.Snap`, with midpoint away from zero (forward for valid nonnegative music).
Units are 960000, 480000, 240000, 120000 and 320000 ticks for 1/4, 1/8, 1/16, 1/32 and 1/8 Triplet.
The anchor is Pattern zero, independently of clip/song position. Free's gesture sentinel `1` is
explicitly refused by Quantize; it is never interpreted as a one-tick grid.

Before accepting any edit, every selected result must satisfy `start <= Pattern.Length - Duration`.
Equality is legal. One failure refuses all notes, with localized finer-Snap feedback. There is no
duration trim, previous-grid fallback, Pattern resize or partial acceptance. Identical starts stay
identical. IDs, fractional pitches, duration/intensity, Part/source/sound identity, graph, Mixer,
Playlist placement and order remain exact. Unselected notes/Parts/Patterns are untouched.

The serialized workflow validates lifecycle, generation, attachment, Pattern, explicit Part and
existing unknown-data/sound eligibility. One canonical `SetPartNotes` edit supplies one Undo; aligned
or empty selection adds no snapshot/revision/history. Undo/Redo restore saved snapshots instead of
consulting current Snap. Shared clips see the shared Pattern edit; both Make variation and Make this
repeat unique retain independent note content with shared sources and routes. No second transport,
preview processor, musical model or project-reader/schema change is introduced.

## View result

Scale's compact flyout supplies Off, Major and Natural Minor plus C–B sharp tonic names. Its pure
pitch-class guide repeats through MIDI 0–127 with the specified seven intervals and stronger tonic.
The retained canvas alone owns default Off/C and lifecycle reset. Part/navigation/view/pane and
transport observations retain it. Switching source root pitch is independent. Scale creates no
document revision/Undo and is absent from JSON, rendered PCM, export and R8.

Row tint uses live HostVisual `Accent.Action`, below hover, grid, ghosts, notes, selection and playhead.
No palette values, control templates, global shortcut/settings or permanent toolbar row are added.
The ordinary retained flyout selectors keep unchanged ItemsSource/selection during Refresh.
Quantize has selection/edit/Busy/Free gating and a tooltip even when disabled. Scale remains available
when musical editing is unavailable.

Local FL Piano overview and Logic editor reference images were inspected before composition changes.
Their quiet grid/readable note hierarchy informed the work; no private reference asset is distributed.

## Deterministic verification

`PianoToolsTests` adds **33** cases. Independent expected ticks cover each grid, both midpoint sides,
near-grid starts, a simultaneous chord, mixed long/one-tick/asymmetric gates, fractional pitches and
velocities. Tests assert entire NoteEvent equality except Position, stable order, other Part/source/
graph/Mixer/clip/length equality, atomic boundary/Free refusal, no-op history, exact Undo/Redo,
Save/Open, shared uses and both whole-clip/repeat variation locality. Stale generation/ABA/lifecycle,
wrong Part, unknown Pattern/Part/note intent and unsupported sound refuse without history.
All twelve tonics and both scale families are checked against literal independent pitch-class masks
for every MIDI row. A canvas test checks view-only history/root independence and replacement reset.

Controlled-output Pattern and Song tests use the production workflow/coordinator/sampler, perform
real Quantize and exact Undo/Redo during playback, await revision convergence and compare the next
PCM packet with a fresh canonical render. Epoch and advancing position remain exact. The physical
borrower alone is substituted; these are numerical execution checks, not device listening.

Focused L1/L2: **301/301**, zero skips, one thread, **40.236 s CLI**. It includes PianoTools/PianoRoll,
StepSequencer, RackWorkflow/RackTimeline, Arrangement/SongStructure/RepeatVariation, History,
PatternEditorNavigation, HostPresentation, DesignSystem and ProductVersion. Build + focused execution
stays within the ordinary 120-second target. Existing restored lock/assets remain valid; no dependency
changes required another restore.

Full Release commands:

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore --max-threads 1 --report-xunit-trx --results-directory .artifacts/piano-tools-release
```

The full build passes with **zero warnings/errors**, **19.73 s**; the final incremental solution
build after harness corrections also passes, **16.10 s**. The full suite passes **1913/1913**,
zero skips, one thread, **193.992 s CLI** (193.627 s runner). This requested L3 exceeds the ordinary
focused target; the existing demo export and MSBuild version subprocess tests dominate its largest
individual durations. Strict allocation assertions pass unchanged. The prior
[allocation audit](SEQ-RT-ALLOCATION-AUDIT-R1_REPORT.md) remains unresolved regardless of green repeats.

## Native input, screen review and limitations

VisualCheck's isolated `piano-tools` route uses Windows SendInput and existing foreground/hit-test
guards. Fixture creation, theme/language and pane geometry are API setup; selecting notes, Snap,
Quantize, Undo/Redo, scale/Part choices and musical editor input are native.

The r3 matrix completes **12** combinations: RU/EN × Dark/Light × 1080/520/360-DIP floating Piano,
inside a 1400×900-DIP window at 100% display scale. Three notes are boxed with the mouse, Snap 1/16
is selected and exact starts become **0/480000/720000**, with every other field unchanged. Native
Undo/Redo restore exact snapshots. C Major and A Natural Minor are selected, other/empty Part and
back retain the guide, and view/zoom/scroll/hide/restore leave document/history unchanged. Major and
relative minor share pitch classes, while the stronger tonic moves from C to A.

Actual screen pixels were inspected for populated/empty Parts, notes inside/outside the guide,
ghosts, selection, keyboard, grid and Pattern end. Notes remain primary; Light's tint is deliberately
quiet. At 360 DIP the existing toolbar wraps and the time viewport is narrow; all actions remain
reachable. No permanent extra toolbar row is reserved at ordinary width.

Selected local captures are retained under ignored `.artifacts/piano-tools-review/` for review,
separately from rolling run directories. They are actual Windows pixels, not product assets:

- [Dark C Major](../../.artifacts/piano-tools-review/English-Dark-1080-C-major-screen.png) and
  [A Natural Minor](../../.artifacts/piano-tools-review/English-Dark-1080-A-minor-screen.png).
- [Light C Major](../../.artifacts/piano-tools-review/English-Light-1080-C-major-screen.png) and
  [A Natural Minor](../../.artifacts/piano-tools-review/English-Light-1080-A-minor-screen.png).
- [Russian Dark reduced](../../.artifacts/piano-tools-review/Russian-Dark-520-C-major-screen.png) and
  [Russian Light minimum width](../../.artifacts/piano-tools-review/Russian-Light-360-C-major-screen.png).
- [Empty active Part with ghosts, Dark](../../.artifacts/piano-tools-review/English-Dark-360-empty-screen.png)
  and [Light](../../.artifacts/piano-tools-review/English-Light-1080-empty-screen.png).

- [Empty guide without ghosts, Dark](../../.artifacts/piano-tools-review/English-Dark-520-empty-screen.png)
  and [Light](../../.artifacts/piano-tools-review/English-Light-520-empty-screen.png).
- [Independent repeat after Quantize](../../.artifacts/piano-tools-review/independent-repeat-quantized-screen.png)
  and [whole-operation boundary refusal](../../.artifacts/piano-tools-review/boundary-refusal-screen.png).

Final **r7** (`piano-tools ... audio quick --timeout-seconds=240`) completes with exit zero on
0.1.16.0007. Native Quantize is undone/redone after changing Snap to 1/8, preserving exact snapshots.
The repeated English Dark/Light 520-DIP matrix includes truly empty rows with Ghosts disabled.
The native repeat menu creates an independent Pattern, Playlist reopening targets its remapped Part,
and Quantize gives the same exact first three starts while the original Pattern stays equal.
Adding a one-tick note at 3839999 then quantizing the whole selection to 1/4 refuses every change,
retains the snapshot/Undo count and displays the localized finer-Snap explanation.

Physical output is **Headset headphones (Samsung USB C Earphones), 48000 Hz, 2 channels**. Real
Pattern and Song playback each accept a native coarse-grid Quantize and native Undo without an
epoch reset, Stop or device failure. During Pattern playback the scale Popup stays open for **2300 ms**
with identical ItemsSource, realized hovered row, screen position and focused element, then accepts
the mouse choice. Scale observations change no music/history. Device identity and completion JSON
are retained with the review captures. This verifies delivery/continuity, not acoustic listening;
canonical PCM comparison is the separate controlled-output test above. No UR12 output is claimed.

No acoustic/aesthetic owner acceptance,
pressure/high-DPI/cross-monitor, other-OS or complete native Save/Open picker claim follows. Official
Neon Orchard is unchanged. Only short isolated verification fragments are used.

## Failed attempts and corrections

- Initial test builds caught a public test parameter exposing an internal enum, then an incorrect
  fixture constructor argument. The fixture uses the correct owner; no production API was broadened.
- First related run: 300/301. A new view test disposed a standalone ShellSession after workflow
  replacement had closed its original document. Marking the fixture shell as workflow-owned fixes
  that ownership error; the 301-case repeat passes.
- Native r1/r2/r4 fail foreground/coverage guards; no passing input is inferred. r5 waits for focus
  then fails its selection assertion. r6 records the correct box/Part IDs but its common ComboBox
  helper later holds a detached Popup item. Snap Off uses native End/Enter instead; the required
  1/16 selection remains mouse-operated. These are retained as failed attempts, not silent retries.
- r3 completes the matrix and playing-Popup/physical checks, then the harness attempts to click
  Playlist behind the Piano automatically opened by Make this repeat unique. The corrected route
  deliberately reveals Playlist before reopening its independent editor. No production behavior is
  changed to accommodate the harness.
- r7 runs after the full test process exits, keeping this native interaction separate from test
  subprocesses. It passes the corrected full musical chain. Earlier failures are not erased or
  attributed to a production defect without evidence.

All task changes remain unstaged. No stage/commit/push/tag/reset or history/remote mutation is used.
