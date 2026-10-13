# Desktop visual and lifecycle verification

This developer-only host reuses the built Desktop, its actual resources, controls and workflows.
Catalog/matrix hosts also enter through the production Single Instance gate with an isolated identity.
It adds no application tab, audio startup, public Single Instance bypass or UI framework dependency.
The pinned SDK/Avalonia versions remain owned by the repository manifests/locks.

After the normal locked restore and Release solution build, run from the repository root:

```powershell
tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.exe catalog "$PWD/.artifacts/desktop-f1/catalog"
tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.exe matrix "$PWD/.artifacts/desktop-f1/matrix"
tools/Seqvium.VisualCheck/Check-Processes.ps1
```

On another OS invoke the corresponding built DLL through `dotnet`; Windows screen/input/process
checks are explicitly platform-specific. Linux/macOS GUI/runtime support is not established here.
The output argument names an artifact parent; the host creates and prints a unique child run directory
for each review, keeping failed and successful evidence distinguishable.
Output directories should be absolute and task-owned, inside ignored `.artifacts/`.
The `gestures` command runs a shorter matrix for focused input/alignment verification.

## Catalog and central-change demonstration

`catalog` opens real standard/primary/compact/icon/danger buttons, sliders, drawn Gain levels,
numeric fields, selection/toggle controls, disclosure, diagnostics, menus and tooltip in both themes.
Named fixture pseudo states expose hover/pressed/focus/disabled/error rendering; those columns are
**synthetic states**, not native Windows mouse evidence. Not every state applies to every component.

The host temporarily replaces `Control.Corner` and `Control.Fill`, verifies actual button corners,
Fluent slider decrease-track brushes and drawn Gain brushes, then restores accepted resources in
`finally`. Invalid slider error and disabled colors remain their deliberate semantic exceptions.
The matrix similarly renders a temporary `Graph.Node` change and checks GraphCanvas's live resolver.
No test color remains in production or changes project intent.

## Window matrix, field alignment and gestures

`matrix` creates a synthetic PCM16 WAV and one saved fixture with the real workflow; it never invokes
Play. Preferences, layout and unnamed media use its isolated output tree. It displays Inspector,
Appearance, Graph and the retained Gain Node Editor in RU/EN × Dark/Light at ordinary, reduced,
minimum and maximized window sizes, plus minimum/expanded pane compositions and owned confirmations.
Native pickers are not exercised by this tool. Pane overlap/canvas clipping are intentional composition;
inspect labels, controls, local scrolling and reachability separately from graph viewport clipping.

`FieldAlignment` compares the real TextPresenter and label centers for toolbar Gain, Graph quick Gain and Node Editor,
checks centered template content and text/hit-area containment. `matrix.json` records actual bounds
and differences. New parameter rows should extend this check instead of trusting outer Height.

`RoutedGestures` exercises actual GUI controls with explicitly synthetic routed events: micro/body
double clicks, zoom-independent drag and one Undo, capture loss, Escape, background deselection/pan.
`NativePointer` separately attempts Windows `SendInput` with an exact foreground-window guard.
If foreground is lost it records partial/refused input and stops; it never forces foreground or
routes synthetic input as a substitute for claimed native evidence.

PNG renders come from actual running Avalonia windows. Each capture also attempts guarded Windows
screen pixels, saving `.screen.png` only when this exact window owns foreground; its `-capture.json`
records success/refusal. Popup/tooltip content may live in another native surface and needs its own
screen review. Render capture and Windows screen/input evidence remain distinct.

## Focused Step Sequencer check

`steps` verifies the retained Channel Rack in an isolated production window without opening audio:

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll steps "$PWD/.artifacts/seq-r5-f1/steps"
```

It creates an empty Beat, durably accepts three authored WAV fixtures, invokes real ToggleButton
automation actions with synthetic pointer events, checks second-toggle/Undo/Redo, arrow/Space and
stale-held-input behavior, then Save/Open and retained dock/collapse/hide/reopen. Eight actual renders
cover RU/EN × Dark/Light at 1100×750 and 640×480. `steps.json` records 48 accessible checked/off cells,
shared pad hit geometry, Pattern name/action alignment and local horizontal scrolling. Additional
routed checks exercise captured continuous paint, erase cancellation, actual Fill popup, Gain dial
preview/fine movement/single commit/cancellation, BPM popup and navigator creation/selection/Undo.
A dense authored eight-channel fixture checks 128 pads in both themes and ninth-channel refusal.
R5 musical checks additionally invoke the actual length selector, channel deletion menu, Pattern
deletion button and navigation collapse/restore. An independent Rack window uses real workflow/
coordinator/PCM with a substituted physical borrower, checking 96 long-pattern pads, actual renderer
phase 2:1:2, current-column outline, Stop cleanup and graph WAV export in Dark/Light. It opens no
audio device; physical loop smoke belongs to DeviceCheck.
A separate native click/focus/drag/Escape smoke uses an exact foreground guard and may report refusal.
It requests ordinary activation only; it never bypasses the guard or exercises a native file picker. Its media/projects/PNG/JSON are ignored run artifacts,
subject to the same rolling retention. Device-independent musical PCM checks stay in Tests.

## Independent-process Single Instance checks

`Check-Processes.ps1` starts two independent hosts through the same production `RunDesktop` path,
with an internal isolated identity. It checks one document initialization, successful activation,
restoration of the existing minimized window, graceful shutdown, explicit crash and reacquisition.
Separate `noipc`/`stalled` hosts hold the real lease while a Desktop secondary must time out with a
diagnostic and no document. The script terminates only the Process objects it created, joins them with bounded waits and disposes
their handles. Child stdout/stderr and partial results survive failed runs.

The private `desktop`, `lease`, `noipc`, `stalled`, `activate` commands belong to this developer tool;
the production Desktop exposes none of them. Protocol tests require no GUI/audio/device. Different
users/logon sessions, elevated-versus-unelevated launches, high DPI and Linux/macOS need separate
runtime evidence. Current-user access and user-wide lease scope are implemented, not exhaustively
platform/security-certified by one Windows session.

## Evidence and maintenance

The `arrangement` route renders the actual Playlist in RU/EN and Dark/Light at ordinary/reduced sizes,
checks routed placement/move/Ctrl-copy/repeat/cancellation/stale gestures, track M/S/name/color,
snap/overview/stationary headings/fit, Duplicate/Delete/Undo, Save/Open and graph Song WAV. It measures
the actual repeat field/label centers and opens the real track settings flyout. A second window proves
Song cursor/bar-beat from the real 44.1 kHz renderer with a substituted physical borrower. No native
mouse, picker, acoustic/device or broad stress claim follows from this route.

```powershell
tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.exe arrangement "$PWD/.artifacts/playlist/gui"
```

Keep renders, screen captures, JSON, synthetic projects/media and TRX local/ignored under the
[retention policy](../../docs/experiments/README.md#evidence-retention). This source, its lock and scripts
are reusable verification material. The host uses narrow reflection to inspect existing retained
controls; if their private names change, a missing-field failure requires updating the harness.
Do not weaken failed assertions or overwrite failed evidence to claim a pass.

[Design System](../../docs/DESIGN_SYSTEM.md) owns component use;
[Test execution](../../docs/TEST_EXECUTION.md) owns evidence tiers and suite commands.

## Deadlines and diagnostics

Every isolated host has an independent 120-second watchdog covering startup, awaited commands,
GUI closure, IPC teardown and automatic artifact maintenance before/after the scenario. The run owns
its watchdog from before startup rotation until Dispose rotation finishes. Manual retention and
process-script begin/complete commands also have a deadline. Override with `--timeout-seconds=N`
(1..600). Timeout persists a
diagnostic and exits **124**; ordinary exceptions return **1**, and existing activation refusal codes
remain unchanged. Only the current test host exits; no ordinary Seqvium process is discovered/killed.
The process script has a 90-second scenario deadline plus bounded child cleanup. No daemon is used.
If startup rotation times out before a run directory exists, `failure.txt` is written to its supplied
artifact parent; later timeout diagnostics go to the run directory. A timeout does not wait for another
manifest write before exiting; a run interrupted before completion stays incomplete and protected.

The GUI scenario is an observed Task; its save/close errors are caught and diagnosed. Native checks
write `native-progress.json` after each passed check and on refusal/error; routed results are written
before native input begins. Synthetic capture is released in `finally`. Outstanding native releases
are attempted only while the exact test window is still foreground; diagnostics retain held-input
state if foreground is lost. This guard never injects into another application.

For a short real startup/joined-close check without captures or input:

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll smoke "$PWD/.artifacts/desktop-smoke" --timeout-seconds=15
```

## Artifact retention

Normal GUI/process invocations automatically apply
[rolling retention v1](../../docs/experiments/README.md#rolling-retention-v1) before and after the
run. The policy owner describes limits, reservations, manifests and legacy review. To inspect/apply
the same policy manually, after the normal Release build:

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll retention dry-run "$PWD/.artifacts"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll retention apply "$PWD/.artifacts"
```

Paths must remain inside the explicit `.artifacts` root. Unmarked trees, unfinished runs and
exceptional raw evidence remain protected. Deletion errors are reported individually and return a
nonzero CLI code; protected excess over the soft budget is reported without silent deletion.
Only the holder of the exclusive root `.retention.lock` may delete or update retention journals.
A sharing/lock conflict skips optional automatic rotation immediately with diagnostics, allowing an
independent verification to continue. Manual `apply` reports `CleanupSkipped: true` and exits **1**;
retry after the owner releases the lock. Skipped report byte counts are unavailable (serialized as zero),
not a measurement of an empty tree. Other I/O/access errors remain errors; dry-run is read-only.
The private `retention begin/complete` commands serve the process-script lifecycle. They do not
classify arbitrary legacy data. DeviceCheck's console measurements and ProjectStats' explicit report
files do not claim run ownership or silently adopt their destinations.
