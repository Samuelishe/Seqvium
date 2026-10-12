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
Use a new output directory for each review to keep failed and successful evidence distinguishable.
Output directories should be absolute and task-owned, normally under ignored `.artifacts/`.
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

## Independent-process Single Instance checks

`Check-Processes.ps1` starts two independent hosts through the same production `RunDesktop` path,
with an internal isolated identity. It checks one document initialization, successful activation,
restoration of the existing minimized window, graceful shutdown, explicit crash and reacquisition.
Separate `noipc`/`stalled` hosts hold the real lease while a Desktop secondary must time out with a
diagnostic and no document. The script terminates only the Process objects it created.

The private `desktop`, `lease`, `noipc`, `stalled`, `activate` commands belong to this developer tool;
the production Desktop exposes none of them. Protocol tests require no GUI/audio/device. Different
users/logon sessions, elevated-versus-unelevated launches, high DPI and Linux/macOS need separate
runtime evidence. Current-user access and user-wide lease scope are implemented, not exhaustively
platform/security-certified by one Windows session.

## Evidence and maintenance

Keep renders, screen captures, JSON, synthetic projects/media and TRX local/ignored under the
[retention policy](../../docs/experiments/README.md#evidence-retention). This source, its lock and scripts
are reusable verification material. The host uses narrow reflection to inspect existing retained
controls; if their private names change, a missing-field failure requires updating the harness.
Do not weaken failed assertions or overwrite failed evidence to claim a pass.

[Design System](../../docs/DESIGN_SYSTEM.md) owns component use;
[Test execution](../../docs/TEST_EXECUTION.md) owns evidence tiers and suite commands.
