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

## Context menu lifecycle and node creation repair

SEQ-R4-F3-B-UX-F1 starts from clean master HEAD
`25677832b908350c88dc51137034ad55f4982df4`, product `0.1.0.0000`, with 838 tests.
The owner reported an ordinary Desktop usability blocker, not an intentional musical limitation.
This working-tree repair is complete and verified locally; F3-B owner acceptance remains pending,
F2 remains partial, and R5/DSP/WASAPI/schema/media lifetime/geometry/colors are unchanged.

### Reproduction and confirmed event ordering

Windows 11 Pro x64 10.0.26300, 96 DPI, SDK 10.0.401/runtime 10.0.12, Avalonia 12.1.3,
Release production MainWindow/GraphPaneContent/GraphCanvas and actual workflow ran in a real Win32
window under a local launcher with isolated preferences/layout. The launcher opened the prior
project-authored quiet WAV fixture read-only; it implements neither menu routing nor graph commands,
never requests Play, and never saves over that fixture. It observes routed events/stacks and canonical
snapshots, while a separate guarded Windows `mouse_event`/`keybd_event` script supplies OS input.
UI Automation sets language/theme; menu navigation and final creation use native pointer input.

Before production edits, native right press opened the menu (`IsOpen=true`, three nodes, Undo=0).
Release made it disappear (`IsOpen=false`, same graph/history). The exact observed sequence was:

1. PointerPressed tunnel: right=true, menu=false.
2. GraphCanvas.Pressed → MenuRequested → GraphPaneContent.OpenMenu → ContextMenu.Open; menu=true.
3. PointerReleased tunnel, then Control.OnPointerReleased → ContextRequested; menu was already open.
4. MouseDevice.MouseUp → Pointer.CaptureLost → WorkspacePane's bubbled PointerCaptureLost handler
   → WorkspacePane.CancelGesture → GraphPaneContent.CancelGesture → ContextMenu.Close.

The stack proves that child implicit capture loss caused the close. It does **not** prove that
Avalonia's attached ContextRequested handler toggled the menu closed: its Open path saw an already
open menu. Opening on press nevertheless put one click through two context-opening paths.
The baseline toolbar failure was not independently traced; its repaired behavior is verified below.

Rider debugger launch was attempted first. Breakpoint binding returned "Didn't find the associated
module"; a manual pause returned an empty stack. Agent breakpoints/session were removed/stopped;
user breakpoints were preserved. The fallback collected real event ordering and close stacks in the
separate GUI host before any production behavior edit.

### Implemented repair and deterministic verification

Right press retains hit selection/focus and opens nothing. A single handled Avalonia ContextRequested
path opens the menu on right release or Apps/Shift+F10 key release. The canvas attaches its menu once,
with its own handler consuming requests before the framework handler. Canvas/pane capture-loss
handlers cancel only their own explicit pointer gestures. Escape closes the whole graph menu and
returns canvas focus; standard menu interaction handles nested hover, left action and outside dismissal.
Toolbar Add/Actions use their button as placement target; creation coordinates still use canvas centre.
No Win32-specific workaround was added to production.

Creation availability uses editing target, Busy and Closed, independently of preparation eligibility.
Source/Gain/Mix remain available in incomplete intent; Output alone explains the existing single-Output
refusal. Node limits, empty choices and unsupported connection/binding choices expose reasons.
Disabled toolbar tooltips remain accessible. Source creation stays unbound; no fake Source, automatic
Mix or hidden repair is introduced. The captured menu action validates lifecycle/generation/attachment
and dismissal identity before execution; Delete captures its element IDs instead of using later selection.
Successful edits retain canonical blockers rather than reporting those playback diagnostics as refusal.
All commands keep existing ProjectDocument transactions and Undo grouping.

`GraphMenuTests` adds 29 no-GUI/device cases: all 20 type/layout/input-count leaves, exact creation
and one-edit Undo, RU/EN Output refusal, processing limits, Busy/no-target/Closed and cancelled,
intervening-edit, same-generation target-switch and document-replacement actions. All prior test source
files are unchanged. Full MTP results contain 838 prior cases plus these 29, all passing.

An initial test draft instantiated full pane icons without a GUI render service and failed at test
setup. It was replaced with tests of the same captured command session used by real menu leaves,
keeping the normal suite's no-platform boundary. Standalone unrooted routed-pointer drafts were also
removed; they were not native evidence. A subsequent assertion exposed that WorkflowState.Detail
contains playback diagnostics after a successful incomplete edit; action feedback now distinguishes
actual workflow failure from those canonical blockers. These intermediate failures are not passing runs.

### Native smoke matrix and lifecycle checks

Every row below completed native right press/release, two levels of hover, left-click
Create node → Gain → Stereo L/R, visible new Gain, exact Undo restoration, a native click on `+`,
a menu beside that button, left-click Mono Gain creation, Undo, Apps, Shift+F10, Escape/focus and
outside-click dismissal. Opening alone leaves canonical graph/history unchanged.

| Language | Theme | Native menu/creation/Undo/toolbar/keyboard result |
| --- | --- | --- |
| EN | Dark | Passed |
| EN | Light | Passed |
| RU | Dark | Passed |
| RU | Light | Passed |

Screenshots show the actual created node, nested submenu and toolbar placement; selected Dark/Light
and RU captures were inspected. No synthetic routed events substitute for this mouse sequence.
On final `0.1.0.0001` binaries, an additional native node/port/connection right-click check verifies
correct selected IDs, menu survival and unchanged graph/history. A later optional full-sequence repeat
stopped before input when foreground belonged to another process; it sent no mouse/key input there.
The completed four-row matrix and final selection check remain distinct passing evidence.

Controlled actual-window checks additionally invoke real WorkspaceHost collapse/hide/reopen actions:
menus close, content reopens, target/canonical graph/history survive. A retained real MenuItem invoked
after an intervening Gain edit or same-ID project reopen changes neither the new snapshot nor Undo.
These lifecycle checks use controlled host actions/routed MenuItem clicks, not native user gestures;
the deterministic same-generation target-switch case separately checks target safety.
All native input checks verify the task process's foreground immediately before each event and stop
on failure. No audio output is opened, and no user configuration or source project is overwritten.

### Final gates, version and review limits

`dotnet restore Seqvium.sln --locked-mode`, `dotnet build Seqvium.sln -c Release --no-restore` and
`dotnet test --solution Seqvium.sln -c Release --no-build --no-restore` pass: zero warnings/errors,
**867 tests, zero failures/skips**. TRX confirms 838 existing and 29 new cases; all 732 earlier crossing
baseline names also remain. Evaluated and built production metadata use display/informational
`0.1.0.0001`, CLR/file `0.1.0.1`, without a source-revision suffix. One BUILD checkpoint is justified;
PATCH remains 0. This is no release or owner acceptance of F3-B/F2/R4.

Owner review: open an existing item's graph, right-click/release, hover Create node → Gain → Stereo
L/R and left-click; inspect the new node, Undo, then use `+`. Confirm Apps/Shift+F10 and Escape, and
repeat after collapsing/hiding/reopening or switching documents. Wider DPI/other OS, native crossing
Delete acceptance and F2 pressure remain outside this bounded menu repair.

Ignored `.artifacts/seq-r4-f3-b-ux-f1/` retains the local host/input/safety sources, event/close stacks,
compact JSON observations, final/failing TRX and screenshots for reproduction and owner review.
They are task-local verification material, not product resources or files promised in every clone;
prior artifacts/user work remain untouched. Automatic permission review rejected deletion of the
task-local launcher bin directory with `blocked by policy`; bin/obj are retained locally too.
Required product builds/runtime and permanent tests are retained. No commit/push/tag, staging
or Git-history mutation occurred.
