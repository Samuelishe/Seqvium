# SEQ-R4-F3-B first-party visual graph editor evidence

Role: Focused implementation and verification report.
Read when: Reviewing the bounded graph canvas and its acceptance evidence.
Authoritative for: Observed F3-B results, methods, failures, environment and evidence limits.
Not authoritative for: Graph/DSP/workspace contracts, roadmap authorization or owner acceptance.

## Scope and disposition

The owner explicitly authorized F3-B on 2026-10-12 from master
`836fcfce25d8035d97d30a560e20a0df9c531802`, with a clean initial working tree and 652 tests.
The first-party editor is implemented and ready for owner canvas/manipulation review; it is not
owner-accepted by this report. F3-A is conditionally accepted for continued development. F2 remains
partial with its existing physical starvation evidence unresolved; neither this editor nor its short
output smoke closes F2 or all R4. No commit/push, system settings, DSP/backend redesign or R5 work.

## Implementation boundary

One retained Node Graph pane uses the existing Workspace chrome/geometry/visibility/docking/focus.
The explicit attachment/lifecycle target is independent of global selection. Source/Gain v1/Mix
(2–8 inputs)/Output draw actual canonical ports/cables/parameters. Local renderer and pure shared
geometry supply hit testing, node/cable selection, transient drag, pointer-centred bounded zoom,
middle/Space/background pan, frame, context/keyboard create/delete/bind/connect/disconnect and Gain.

All accepted music/graph edits use the existing document owner and ProjectEdit/history. One drag
commits once; cancellation, no-op and stale lifecycle/generation create no edit. Gain text commits
once with Enter/check and retains saved invalid intent for repair. There is no editable graph copy,
second Undo, hidden converter/Source/Mix/performance, new major assembly or general editor framework.
Second WAV acceptance uses existing durable media plus a fresh Sound/Part/Note/Source binding in one
unshared item. Shared Patterns refuse before preparation. The narrow new Core `SetPatternLength`
allows a longer accepted source, with whole-state validation and Undo in the same import transaction.

Owners: [graph](../NODE_GRAPH.md#implemented-r4-f3-b-first-party-editor),
[workspace](../WORKSPACE.md#r4-f3-b-retained-graph-pane),
[sample workflow](../SAMPLE_WORKFLOW.md#r4-f3-b-add-wav-to-current-item),
[architecture](../ARCHITECTURE.md#r4-f3-b-graph-presentation-and-commands).

## Deterministic and build evidence

Windows 11 x64 10.0.26300, .NET SDK 10.0.401/runtime 10.0.12, Avalonia 12.1.3, Release.
Locked restore and full solution Release build pass with zero warnings/errors. All 704 tests pass,
zero failures/skips: 652 original cases plus 52 new editor cases. Existing workspace assertions were
adapted to the third hidden capability; no baseline case was deleted or weakened.
[Test execution](../TEST_EXECUTION.md#r4-f3-b-graph-editor-verification) owns commands and test inventory.

Independent PCM oracle: imported float mono Kick 0.2 for 512 frames and Snare 0.3 for 1024 frames,
48 kHz stereo output; independent branch ratios 0.25 and 0.5, then master 0.4. Expected output is
`((frame < 512 ? 0.2 * 0.25 : 0) + (frame < 1024 ? 0.3 * 0.5 : 0)) * 0.4`, both channels,
tolerance 2e-6. Muting each branch isolates its actual source; natural EOF leaves only the longer
source and then silence. Moving nodes preserves identical PCM. Fan-out has one contribution/NoteOn,
not duplicate scheduling. Gain-only realtime checks preserve prepared ID, origin, epoch and advancing
position; inherited nonconstant PCM/coordinator tests remain present. No physical device, platform
initialization or timing sleeps is required by pure tests.

Initial verification found seven old two-pane assumptions and two new test packet-size mistakes;
counts/conflict scope were corrected and oracle output was partitioned within prepared packet capacity.
A build attempted while the task's GUI process was running hit an apphost file lock; the task window
was closed normally before the clean rebuild. These intermediate failures are not final green results.

## Actual Windows GUI evidence

Actual Release windows were driven with Windows UI Automation, guarded native mouse/key input,
OS pickers and saved project JSON observations. Inputs were sent only to the task process; GUI settling
waits are automation mechanics, not pure timing oracles. SendKeys Ctrl combinations and a native Save
field automation pattern were unreliable; guarded native Ctrl/key events and the actual filename field
were used. Early automation failures/missed menus are retained separately from successful checks.

- Native WAV import and project Save/Save As/Open were exercised with task-authored quiet fixtures.
- Sparse Source → Gain → Output and a real seven-node Kick/Snare pre-mix graph were created/edited.
  Second import leaves one owning Pattern/placement with two distinct Parts/Sounds/resources/bindings.
- Mouse node drag changes saved coordinates; one Undo restores exact pre-drag revision, Redo restores
  the move. Escape and no-op preserve revision. Pane collapse during capture cancels without an edit.
- Actual compatible wire previews and pointer commits, selected cable deletion, occupied-input
  feedback and wire Escape were observed. Source deletion retains music; recreation explicitly binds
  the same Part and reconnects. Gain 0.5/0.1 values were committed through the pane.
- Keyboard nudge/Undo, Gain text input/local Undo without musical mutation, frame and project Ctrl+G
  were exercised. Pan/zoom leave saved revision unchanged. Picker cancellation retains the document.
- Global import selected a new occurrence while the graph retained the previous attachment; its human
  label changed from Kick to Kick · 1 as choices gained indices. Ctrl+G explicitly switched to Kick · 2.
- Collapse/restore, hide/reopen, dock left/float, resize, focus return, reduced overflow and replacement
  were exercised. Reopen clears the retained editing target and creates an inactive fresh audio session.
- RU/EN × Dark/Light were captured for sparse and dense graphs at ordinary/reduced/maximized sizes,
  including selection, incomplete feedback and dense wiring. Startup captures are 1100x750 pixels;
  native resize initially gave 1084x742 due to invisible frame extents, corrected captures measure
  1100x750 and 640x750. Maximized capture is 1920x1040 (1920x1080 display work area). Pane size remains
  independently retained after window resize, so large-window work also used actual pane enlargement.
  Dense framing at reduced width is an overview; ordinary zoom/pan provides readable editing access.

A real replacement crash was found: shell notification refreshed pane content against new workflow
state before the pane adopted that observation, so old graph ID diagnostics were looked up in a new
document. The Windows .NET Runtime log identified GraphPaneContent.Refresh. Refresh now adopts one
coherent immutable observation before graph/report resolution; renderer labels use that same snapshot.
A pure observation regression was added, and four consecutive different-project replacements with
the retained pane were rerun successfully, with empty final stderr. This failure is preserved as evidence,
not omitted in favour of the earlier passing tests.

Selected cable drawing/hit priority has a deterministic crossing regression. One additional native
crossing attempt removed a different cable than the script expected; its pre-click menu selection was
not captured, so that run does not establish the cause or a passing crossing result. A subsequent
automation retry encountered a UI Automation COM error; final input verification stopped when the
foreground belonged to the owner's browser. The guard sent no keys/mouse to that application. The
graph was restored through Undo and saved with all six original connections and original positions.
Exact dense crossing selection remains an owner-review/evidence item; ordinary cable selection,
deletion and actual pointer wiring passed independently. Accessible canvas names now expose the human
selected node/connection to make that final review unambiguous.

Local ignored evidence is `.artifacts/seq-r4-f3-b/`: screenshots, `gui-trace.jsonl`, replacement trace,
physical/reopen observations, TRX, scripts and generated WAV/project/media fixtures. These are review
and reproduction material, not product resources or files guaranteed to exist in another clone.
Representative views: `dense-1100-ru-dark.png`, `sparse-1100-en-light.png`, `compatible-preview.png`,
`physical-invalid-last-valid.png`, `invalid-reopen-stale-target.png` and reduced/maximized variants.

## Physical output evidence and limits

A bounded explicit Play/Stop smoke used the GUI-edited two-source graph on
**Samsung USB C Earphones, 48000 Hz, 2 channels**, through the existing Windows default output path.
Authored source sine amplitudes were 0.004 at 80/240 Hz, durations 8/10 seconds, branch ratios 0.25/0.5
and master 0.1. No system volume/device/priority/GC/buffer setting changed. Status reported Playing.
Deleting master → Output kept canonical incomplete state and reported Previous valid audio; Undo
repaired and automatically returned to Current graph playing. Explicit Stop reported Stopped.
Save retained the invalid canonical state. Fresh Open was blocked/silent with no inherited output;
explicit target and repair restored canonical eligibility. Successful replacement exercised the
production preparation/output joins; deterministic tests additionally assert zero retained resources
after Stop/Panic/close and cancellation of a gated pending preparation before replacement.

This is an actual native output/session smoke, separate from numerical PCM correctness. Default output
has no diagnostic capture/timing instrumentation; no physical packet-parity/deadline, DAC latency,
pressure, multi-hour/all-device or acoustic perception claim follows. The owner did not participate in
subjective listening or acceptance. Original F2 starvations remain unresolved under
[its report](SEQ-R4-F2_REPORT.md).

## Remaining acceptance

Owner must inspect/manipulate the actual canvas and judge sparse/dense readability, gestures and
first-party design. No high/mixed-DPI, cross-monitor, screen reader certification or other-OS runtime
claim follows. Exact minimum-height 480-pixel outer-frame evidence remains an inherited gap; F3-B used
640-DIP-width windows with usable height. Shared Pattern source expansion intentionally refuses;
general Pattern/Arrangement/Mixer/plugin/editor capabilities remain outside F3-B.

## Final repository state and retention

Branch master and HEAD remain the requested baseline. The initial tree was clean. All current changes
belong to this task: 28 modified tracked files and nine new product/test/document files; staging is
empty. No commit/push or Git history/index operation occurred. Ownership groups are:

- Core: `Documents/ProjectEdit.cs` (Pattern boundary edit).
- Desktop: `MainWindow` workflow/XAML/code, HostIcons/HostLocalizer/HostPalette, DesktopWorkflow and
  its new graph command partial, WorkflowControls/WorkflowPresentation, WorkspaceState/Host/Pane
  and new GraphGeometry/GraphEditorSession/GraphCanvas/GraphPaneContent.
- Tests: existing WorkspaceState/WorkspaceLayout assertions and new GraphCanvasGeometry,
  GraphEditorSession and GraphEditorWorkflow suites.
- Documentation: README; architecture, graph, project format/state, roadmap, sample workflow,
  settings, test execution, UI design, UX and workspace owners; this report and appended archive facts.

All nine untracked files are deliberate permanent source/tests/report, not disposable outputs.
Ignored artifacts are intentionally retained for owner review and reproducing verification: screenshots,
quiet WAVs, managed project/media fixtures, TRX/raw observations and task-specific GUI/link scripts.
They can be removed after owner review; accepted media/build/runtime material and prior task/user
artifacts were not cleaned. Relative file/anchor and duplicate-heading checks cover 14 changed Markdown
files and 653 links without errors; diff whitespace and ownership/status/scope checks pass.
