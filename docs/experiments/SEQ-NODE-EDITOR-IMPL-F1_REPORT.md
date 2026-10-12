# SEQ-NODE-EDITOR-IMPL-F1 — reusable Node Editor evidence

Role: Bounded implementation and verification evidence for the first retained node editor.
Read when: Reviewing this editor's lifecycle, interaction checks or their limits.
Authoritative for: Observed checks, failures, local artifacts and checkpoint classification.
Not authoritative for: Product contracts, R4 acceptance, realtime pressure acceptance or plugin hosting.

## Scope and environment

The owner accepts DESIGN-R1 for one reusable internal editor, Gain v1 editing and informational
Source/Mix/Output views. Independent editors/further composition remain Q-032. F2 stays **partial**;
F3-B stays **pending owner review**. This does not authorize R5/R6 or native plugin hosting.

Baseline: `4e97e99c8274d9d3089b2ee08fddb1eff68608e2`, clean tracked/untracked Git status,
product `0.1.0.0001`. Local Windows 11 x64, SDK 10.0.401, runtime 10.0.12, Avalonia 12.1.3,
Release. Existing project-authored local Kick fixture supplies the actual-window sample; regular
tests author their own bounded WAV data and temporary directories. No third-party assets are added.

## Implemented boundary

`NodeEditorSession` retains `(LifecycleId, AttachmentId, GraphDefinitionId, NodeId)` and one transient
proposal over current immutable observations. Generation and starting amplitude gate commits, including
ABA. Deletion retains the key for exact Undo restoration; successful document replacement clears it
even when saved IDs match. Cancellation/failure of Open/Save preserves the current document target.
Hide/collapse cancels drafts and Source publication; disposed controls refuse queued refreshes.

`NodeEditorPaneContent` lives in the fixed `node-editor.main` Workspace pane. MainWindow owns semantic
open/activate requests. Explicit targets do not follow selection. The existing workflow validates a
frozen Gain proposal before the normal document edit; history, Save and F2 remain canonical.
Pointer capture belongs to the level control. The transfer plot and dB are derived presentation,
with no new parameter, DSP state, schema, backend, processor instance or public registry/API.

Opening uses platform click count and two completed same-body clicks, rejects movement/ports/wires/
pan/basis changes, and releases capture before activation. It also uses node context Open Editor and
canvas Enter. The existing ContextRequested open-on-release behavior is preserved.

## Deterministic and build verification

Final commands use the repository's SDK 10 MTP/xUnit v3 setup:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*NodeEditorTests"
```

Locked restore and full Release build pass with **zero warnings/errors**. **908 tests pass, zero
failures/skips**, retaining all 867 baseline cases and adding 41 editor cases. They cover explicit
target retention, wrong attachment/definition, unsupported type/state/opaque parameters, exact deletion
Undo/Redo, intervening edits/ABA, same-ID fresh lifecycle, cancellation/disposal, dB independent oracle,
invalid persisted Gain, 500 previews/one edit, no-op/cancel, captured menu expiry, delayed text echoes,
old layouts, canonical Save/reopen and stored WAV inspection. Empty-state checks cover all language/theme
combinations without initializing a GUI platform.
Final review also checks a saved containing context shared by several placements: the editor shows
the actual container owner/scope without choosing an arbitrary source or throwing on multiple owners.

One new F2 integration case explicitly uses the production preparation/coordinator/sampler with a
deterministic borrower, not a physical device. A committed amplitude 0.5 produces the independent
0.2 × 0.5 = 0.1 PCM oracle; execution ID/epoch are preserved. Stop remains sticky after another Gain
commit, and shutdown joins the borrower and releases all prepared states/PCM reservations. Core DSP,
coordinator and Windows adapter files are unchanged.

A retained prior three-pane Desktop binary reads a new layout with fallback, refuses a save and
preserves its original bytes exactly. Old layouts start the missing new pane hidden; target/draft
are never layout fields. Downgrade is safe preservation, not lossless layout editing.

## Actual Windows GUI checks

An isolated local host instantiates the production App resources, MainWindow, workspace and workflow
in a real Avalonia Windows window, with isolated settings/layout and task-owned output files.

### Owner-directed composition correction

The owner rejected the initial active Gain presentation after the empty-state repair. Its scope,
node name, amplitude explanation, formula and healthy statuses repeated information and gave the
analytical graph too much priority. This is not treated as visual acceptance of the earlier layout.
The resulting editor puts the node name in pane chrome, source/actual scope once below it, and
Level with adjacent exact value/dB over the level control and Unity/Silence actions. Curve is
initially collapsed and its formula is available on request. This deliberately refines DESIGN-R1.

The three Gain surfaces have different selection owners: toolbar follows the selected playback
attachment's single Gain; Graph follows its retained attachment/selected node; Node Editor follows
its explicit target. Complete lifecycle/attachment/definition/node equality establishes whether
they edit the same parameter. For matching targets a visible expanded editor takes priority, then
visible Graph quick Gain, then toolbar. Different targets retain access; hide/collapse restores
quick controls. No parameter/scopes are merged. Idle Ready/canonical-ready footer copies are hidden.

Real cancelled-draft feedback expires after three seconds. Initial load, refresh and own commits
do not produce it. Invalid values stay exact and diagnosed; their undefined dB/curve are hidden
instead of repeating invalid text across controls. Draft feedback is short and its curve dashed.

The curve initially collapsed to header width, and Fluent's hidden 48-DIP Expander minimum caused
small-pane scrolling/clipping. Explicit stretching and template-aware minimum/header styling fix
both. Its actual plot occupies the available width; opening a smaller floating pane provides
350 DIP height, sufficient for the curve and draft feedback. Collapse restores preceding height
unless manually resized. Unselected nodes now use slightly lighter Dark fills, white Light fills
and distinct boundaries. UI_DESIGN and CODING_GUIDELINES require background separation and rendered
minimum-size checks with localization, zero/invalid/draft and disclosed content.

Final-source renders also cover the maximized 1920×1040 client and a manually enlarged 700×380
editor in all four language/theme combinations. A manual resize while Curve is open survives its
collapse; the isolated GUI then saves task-owned canonical data and completes normal shutdown.

The repeated actual-window matrix covers RU/EN × Dark/Light, 1100×750, reduced 640×511 actual render,
300×210 pane minimum, compact normal view, zero/Unity, full-width curve and dashed draft, own/external
edits/notice expiry, invalid Undo/Redo, hide/collapse/reopen and same-snapshot replacement. Measured
idle/curve/draft extents fit their viewports at the exercised automatic sizes. User-chosen smaller
expanded sizes retain scrolling. The earlier state-only smoke passed while rendered inspection
still exposed invalid-label pressure and draft-axis clipping; those defects were corrected before
the repeated checks. Visual owner acceptance remains pending.

| Evidence | Actual scope and outcome |
| --- | --- |
| Guarded Windows mouse/key input | Successful single-click/body double-click, Enter opening, right press/release ContextRequested, pointer Open Editor and correct editor focus, body drag/one Undo, and port double-click exclusion. PID foreground is checked before each native event. The captured successful opening run is RU/Dark; it is not a full native matrix. |
| Ordinary/reduced render and lifecycle matrix | RU/EN × Dark/Light, 1100×750 and nominal 640×480 client settings. Actual current visual trees are rendered with RenderTargetBitmap and inspected. Source, Mix and Output show real resource/binding/input/output details; invalid Gain 2 stays visible and diagnosed. Target switch, hide/collapse/reopen, left/right docking/floating, deletion Undo/Redo, cancelled/failed Open, Save and same-ID replacement pass. |
| Owner-requested empty-state smoke | All four language/theme combinations exercise empty project, absent target, open Gain, deleted node and restored/reopened editor. Pristine startup leaves Node Editor hidden. Each empty pane has one content hint; selector/header/footer copies are hidden. Deleted-node warning remains once, while genuine graph diagnostics remain visible. Empty toolbar occurrence/Gain controls are hidden. |
| Controlled Gain events in the actual window | Whole-height hit area, temporary captured pointer proposal, one-release commit, capture-loss cancellation, exact input/Enter, no-op, keyboard Unity/Silence, Escape and external quick Gain reconciliation pass. Pointer/keyboard routed events here are explicitly **synthetic**, not native Windows input. |

The initial and final guarded native Gain attempts stopped on foreground loss. The final attempt
does not force foreground and stops before input. Therefore final native Gain/capture/keyboard
acceptance and the complete native RU/EN × Dark/Light matrix remain unevidenced. Controlled routes
and real visual rendering do not replace that evidence. No GUI run starts playback.

## Failed intermediate checks and corrections

- The first production build found one nullable execution-status access; it was corrected before
  successful Release gates. Four old Workspace assertions assumed three panes; their concrete
  count/conflict assertions now cover four without discarding baseline cases.
- One focused command used an unsupported interior wildcard in `--filter-class` and ran zero tests;
  the corrected beginning/end wildcard command and final full suite are recorded above.
- Native Gain work exposed a narrow drawn hit region and a deferred `TextChanged` echo that could
  recreate a draft after canonical refresh. The whole 30-DIP control now participates in hit-testing;
  synchronous `TextProperty` notifications run under the refresh guard. An explicit deferred-event
  regression and actual-window checks verify the correction.
- A new async control test initially crossed Avalonia's thread boundary; the test now performs its
  control assertions synchronously before disposal. Graph/toolbar controls requiring a render
  platform are verified in the actual-window host rather than initialized in the regular suite.
- One full run had three existing fresh-Play assertions before the consumer observed the final Start
  publication. Their helper now consumes the final packet after command acknowledgment. Assertions
  and production audio behavior are preserved; the final complete suite passes.
- Temporary GUI-harness iterations failed on file-sharing/publication races, stale replayed commands,
  initial helper compile/encoding/encoder setup, and a stale captured quick-control basis. The helper
  now publishes commands/results atomically with bounded file-sharing handling, and uses a fresh
  workflow basis. These are helper failures; accepted GUI results are the subsequent completed runs.
- Two repeated composition runs stopped at draft/disclosure assertions. The helper's reused
  acknowledgment was removed as a possible timing ambiguity; a subsequent runtime listener captured
  Windows `HandleDeactivated` → MainWindow → WorkspaceHost.CancelGestures → editor.CancelGesture,
  explaining a reproduced cancellation without a musical edit. This preserves the existing safe
  deactivation contract. Fifteen repeated disclosure cycles and the complete language/theme matrix
  subsequently pass. Untraced earlier interruptions are not assigned a stronger root-cause claim.
- Rider attached successfully, but could not bind source breakpoints to the temporary Release module.
  Agent breakpoints were removed and the debugger detached. No debugger value/root-cause proof is
  claimed; a second attach likewise could not bind the cancellation breakpoint. Actual-window
  event/property stack observations establish deactivation cancellation without editing production
  code to add debug logging. All agent breakpoints were removed and the debugger detached.

## Checkpoint and limits

`0.1.0.0002` is one BUILD checkpoint within the existing attenuation/routing capability line under
[VERSIONING](../VERSIONING.md). It recognizes the verified reusable editor architecture/interface,
without claiming a finished independent musical editor or new processing block for PATCH. No extra
version increment is assigned for intermediate builds, tests, documentation or the empty-state repair.

No physical audio/pressure, acoustic quality, high/mixed DPI, screen-reader certification, other OS,
full native matrix, owner acceptance of all editor visuals, public plugin ABI or release claim follows.
The inherited exact outer 480-pixel review gap remains separate; reduced actual images are 640×511
pixels under the current Windows frame/client minimum behavior.

Ignored local `.artifacts/node-editor-f1/` retains review captures, bounded helper source/scripts,
results, the compiled isolated review host and task-owned canonical project copies. They support owner review/reproduction and are not
runtime assets or durable clone dependencies. Earlier evidence/user artifacts are preserved.
Cleanup removes the stale task PID and empty output files. Automatic approval review rejected
recursive removal of the task-owned helper `obj` cache with `blocked by policy`, including a
literal-path attempt after verifying its location. That ignored build cache remains local with the
review host; no broader cleanup or user-file deletion was attempted.
No commit, staging, push, tag or Git history operation was performed.
