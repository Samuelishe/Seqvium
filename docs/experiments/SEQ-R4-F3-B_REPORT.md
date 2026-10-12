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
Original editor verification: locked restore and full solution Release build pass with zero warnings/errors. All 704 tests pass,
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

## Original implementation repository state and retention

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

## Cable selection and crossing correction — 2026-10-12

The owner authorized this correction from clean master at
`f8ce9bd4ea5fbf10bf53f36dd113de5ade1ea5b2`, preserving the 704-test baseline and current cable appearance.
F3-B implementation remains complete with owner acceptance pending; F2 remains partial. No Core DSP,
graph schema, audio backend, GC, workspace layout implementation or R5 change is included.

### Reproduction and minimal correction

Before changing production code, six nearest-selection cases failed on the existing crossing fixture
specifically at the final connection-ID assertion. At normal zoom, the point on the first Bezier at
`t=0.515` is within 0.01 graph unit of that cable; the second cable is about 4.2592 screen pixels away,
inside the unchanged six-pixel tolerance. Reverse draw order chooses the second cable, including when
it is already selected. Minimum zoom uses the same point; maximum zoom uses `t=0.5075` so both cables
remain inside tolerance. The initial maximum-zoom draft used `t=0.515` and failed its fixture premise
(second distance 8.5184 pixels); the corrected red run is the identity-only evidence.

`GraphGeometry.HitTest` now measures every drawable candidate in screen units, determines the global
minimum and resolves only candidates within 0.1 screen pixel of that minimum in renderer order:
selected cable first, then upper visual cable. Pairwise near-tie chaining cannot include a farther
candidate. Ports/bodies retain priority. No Bezier, under-stroke, color, port shape, crossing rendering
or six-pixel hit tolerance changed. Screen units are Avalonia DIPs, consistent with the renderer and
existing viewport; no high-DPI/native physical-pixel equivalence is inferred.

Selection feedback, Select Connection and contextual Disconnect share real endpoint/port labels.
Two same-node-pair cables identify `In 1` versus `In 2`; multi-output/unknown roles remain identifiable,
and missing retained endpoints do not crash disconnect labeling. The visible status begins with the
selected human endpoint label so long incomplete diagnostics do not precede deletion identity.
The canvas automation help text exposes the selected connection ID for verification; visible controls
and menus show no UUID. Delete continues through the existing workflow's selected ID to
`ProjectEdit.DisconnectGraphPorts`; Core editing/history was not changed.

### Verification and actual-window evidence

Locked restore and full Release solution build pass with zero warnings/errors; **all 732 tests pass,
zero failures/skips**. All 704 previous cases remain; 28 new cases cover nearest selection (including
a farther selected wire), exact-crossing ties, near-coincident curves at three zooms, global-minimum
ties, tolerance, node/port precedence, port-role labels and selected/deleted/saved/Undo identity.
Four workflow cases call the actual canvas delete command, compare every remaining connection record
and saved ID, and restore the same pre-delete snapshot, original connection IDs/topology and nodes.
Commands use the repository's MTP/xUnit setup, including `--report-xunit-trx`; an initial unsupported
generic TRX flag and a combined class/method filter that selected no tests are not passing evidence.
Temporary test attempts to instantiate full pane rendering without a GUI platform were discarded;
the permanent suite retains its no-platform/device boundary.

A single bounded native-input crossing attempt opened the graph in a real Windows window. Before
the crossing click, foreground belonged to the owner's browser; the guard stopped input. One focus
recovery attempt failed and native automation stopped. No crossing click, Delete or browser input
occurred, so this is not a passing native pointer result. The task window was closed normally.

A separate bounded actual-window check used the production Release MainWindow, GraphPaneContent,
GraphCanvas and workflow under a local launcher with isolated preference/layout stores. It opens a
task-authored canonical fixture derived from the prior quiet WAV project, with four connections and
the same crossing geometry; its deliberate unbound/mix incompleteness remains visible and no Play
is requested. Rooted Avalonia pointer-press/release and Delete key events exercise the actual handlers
on the GUI thread, without OS mouse/key injection. This is real-window routed-event evidence, not
native-input or subjective visual acceptance. The launcher has no selection/disconnect implementation.
An initial launcher event used canvas coordinates where a top-level coordinate was required; that
test-harness error was corrected before the passing run and is not attributed to product hit testing.

- Before Delete, editor selection, automation identity and visible status identify connection
  `b3ad9574-87e1-4212-876e-82ee7a9d8b1f`: `Source · Kick → Gain 1 · In 1`.
- Delete invokes the actual keyboard/canvas/workflow path. Saving and reading the canonical project
  leaves exactly `6d03a1a5-8940-4c13-b424-2c48803ca370`,
  `d4566491-6b3f-4e7f-a9ea-88e593934d4f` and `08d75795-22c2-4e4c-8c63-d5ede5ae8f59`.
  All remaining connection records retain their original endpoints.
- Undo and Save restore all four original IDs, complete original connection records and node positions.
- Actual Select Connection and Disconnect items distinguish `Source 2 → Mix 1 · In 1` and `In 2`.
  Invoking both selection items verifies their exact selected IDs and UI automation indications.
  The `In 2` contextual disconnect removes only its identified cable; Undo restores original topology.
  The final window closes normally with empty stderr and no active output/device session.

The deterministic reproduction confirms the proposed draw-order-versus-distance defect and the
actual-window routed check confirms deletion of the indicated nearest connection. **It does not
prove the cause of the original unsuccessful native GUI run**, whose pre-click selection was never
recorded. That original case remains intact above; native crossing interaction and owner acceptance
remain outstanding. F2's physical starvation evidence and acceptance status are unchanged.

### Correction repository state and retention

Master and HEAD remain the correction baseline; initial worktree/index were clean. Changes are limited
to Desktop geometry/connection presentation, permanent regression tests, graph/test/current-state
owners, this existing report and a concise archived work entry: nine modified tracked files and two
new files. The two new files are permanent Desktop
label code and its tests; staging is unchanged. No commit/push/history/index mutation occurred.
Ignored `.artifacts/seq-r4-f3-b-crossing/` retains red/final TRX, isolated fixtures/media, launcher/input
scripts and pre-Delete/after-Delete/after-Undo canonical observations for bounded reproduction/review.
These are local verification evidence, not runtime assets or files promised in other clones. Original
`.artifacts/seq-r4-f3-b/` evidence and user material remain untouched. Disposable intermediate launcher
build outputs can be removed after review; no required product build/runtime/test material was removed.
TRX name-inventory comparison confirms all 704 baseline cases are present among the final 732, with
28 additions and none missing. Independent reads of the saved GUI canonical files confirm the expected
remaining connection records, exact restored topology and unchanged nodes. Documentation checks cover
all five changed Markdown files and 272 relative links with no file/anchor/duplicate-heading errors;
ownership, decision status, scope and diff whitespace checks pass.
