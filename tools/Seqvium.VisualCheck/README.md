# Desktop visual and lifecycle verification

## Global Settings

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll settings "$PWD/.artifacts/settings/review" "$PWD/examples/Neon Orchard/Neon Orchard.seqvium" native
```

Omit `native` for routed input. Uses isolated real Desktop/preferences and a read-only project.
Reviews General/Controls/About in RU/EN × Dark/Light, ordinary and reduced windows. Exercises capture,
conflict transfer, clear/default/reset, reserved-key/cancel/search behavior, actual command dispatch
and old-key removal, Browser global behavior, file reload and unchanged music. Native mode guards exact
foreground and hit testing before action-button/key delivery. No physical audio or other-platform/DPI
acceptance follows. Identity review now opens About inside Settings.

## Theme switching and Pattern navigation

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll theme-navigation "$PWD/.artifacts/theme-navigation/review" "$PWD/examples/Neon Orchard/Neon Orchard.seqvium" native
```

Uses isolated preferences/layout and opens the supplied project without writing it. Empty RU/EN
Dark/Light ordinary/reduced captures precede the populated review. Routed pointer checks cover continuous
card/thumbnail resizing, collapse/reversal/restore, Escape/capture loss, upper-bound reversal, compact
fitting, independent Browser/Patterns coexistence, clipping, keyboard/focus and user-layout persistence.
Both right boundaries are checked for independent resize/collapse/restore and exact neighbouring
workspace displacement, including 640×480 and a 900-DIP neighbour-width regression. `native` adds guarded
Windows mouse resize/collapse/rail reopening for both panels and exact compositor thumbnail-background checks across
repeated theme switches. Screen pixels are sampled before explicit bitmap rendering can refresh a
stale scene. Omit `native` for routed/rendered evidence only. Retained control identity, canonical
snapshot and Undo remain unchanged; no physical output or cross-platform/DPI acceptance is claimed.
`left-panels.json` records completed sidebar checks before the separate repeated-theme matrix, so a
later foreground refusal does not erase their bounded evidence.

## Sample Lab and UI density

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sample-lab "$PWD/.artifacts/sample-lab/review"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sample-lab "$PWD/.artifacts/sample-lab/native" native audio
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll ui-density "$PWD/.artifacts/sample-lab/ui-density" "$PWD/examples/Neon Orchard/Neon Orchard.seqvium"
```

`sample-lab` opens the real production pane without Browser initialization, generates independent
and related Kick candidates, holds decay, revisits history and accepts independent project/Rack/
Playlist uses. It checks Undo/Redo, generator disable, retained-audio acceptance and portable reopen/
canonical export. RU/EN Dark/Light ordinary/reduced renders and actual numeric-field alignment are
included. Default buttons are routed; selection, lock checkbox and window layout are fixture setup.
`native` uses guarded Windows action-button input and refuses foreign-window coverage. `audio` adds
one real default-output solo/Pattern/context/original sequence; its result/device/failure is recorded
separately. Neither rendered waveform nor physical delivery establishes subjective acoustic quality.

`ui-density` opens the supplied project without writing its source, composes all ten first-party
panes in populated musical groups, and captures four language/theme combinations plus reduced Lab.
It uses fixture setup, no native input or physical output. Source/Gain/Drum/processor disclosures are
also reviewed through their existing focused routes when affected; a capture matrix is not every
possible gesture/state. All outputs remain ignored, retention-managed developer evidence.

## Local extension foundation

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll extensions "$PWD/.artifacts/r6-extensions/review"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll extensions "$PWD/.artifacts/r6-extensions/native" native
```

The real Browser discovers the build-copied original Found Sounds pack, shows a waveform, imports
through Rack/Playlist buttons, checks managed resources/Undo, disables/re-enables the contribution,
and reopens music with the package disabled. Four language/theme renders and reduced-window
disclosure/list/preview checks are included. The default route uses routed buttons and does not
establish native Windows input. `native` uses the guarded pointer helper for action buttons.
`extension-browse-input.json` records the first Browse click's pointer delivery, Click count,
resulting internal location and row count. Address Enter/Up/Back and editable-folder regressions use
routed keyboard events with focus on the address field in both modes.
Initial window/pane/disclosure setup and file selection remain fixture setup. It opens no physical
audio device. [R6 report](../../docs/experiments/SEQ-R6-EXTENSION-FOUNDATION_REPORT.md) records actual
success/failure and scope. The test-only generator provider is exercised by deterministic tests,
not presented as Sample Lab in this GUI.

## Cross-stage input and Browser cost

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll cross-stage "$PWD/.artifacts/cross-stage-audit/.artifacts/routed"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll cross-stage "$PWD/.artifacts/cross-stage-audit/.artifacts/native" native
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll browser-cost "$PWD/.artifacts/cross-stage-audit/cost" <authored-long-mp3> <authored-long-flac>
```

`cross-stage` verifies main-window F6/pane traversal and Ctrl+R/Browser refresh. A held production
preparation exercises Pattern selection, second-click editor intent and Panic with a deterministic
borrower/callback; it does not play physical audio or validate MainWindow's exact melodic Part choice.
`native` adds guarded real Windows clicks/motion, including a row different from previous selection
and list whitespace. Foreign-window coverage refuses input. An ordinary Shell shortcut launch may
be needed for foreground eligibility; guards must still pass. Sources, projects and configuration
are synthetic and local to the run. `browser-cost` observes full inspection then preview, process
allocations/peak working set and cooperative cancellation once per supplied source; it neither
measures UI dispatcher progress nor opens physical output. Supply authored/licensed files and keep
the run bounded. [Audit](../../docs/experiments/SEQ-CROSS-STAGE-AUDIT_REPORT.md) owns observations and limits.

## Sample Browser check

With an affected Release build (or equivalent isolated `--artifacts-path` executable):

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll browser "$PWD/.artifacts/sample-browser/gui"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll browser "$PWD/.artifacts/sample-browser/native" native
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll browser "$PWD/.artifacts/sample-browser/audio" audio <codec-fixture-directory>
```

The first route uses actual windows/controls and routed actions/drop events, not native mouse claims.
It creates authored synthetic WAV fixtures in its own run directory, browses/reuses them, makes eight
notes and one Audio Clip, checks drop/Undo, localized theme/reduced renders and Save/Open after removing
originals. `native` requires exact foreground and hit-testing guards before Windows input. A covered
window refuses input rather than clicking another application. `audio` adds actual WASAPI solo output
and optionally `Tone.mp3`/`Tone.flac` from the supplied independent fixture directory. These must be
caller-authored or licensed fixtures; no private library is required. This checks decoded metadata,
real preview execution and managed Playlist import/Undo, not subjective acoustic quality or pressure.
Physical observations and input mode are recorded separately in `sample-browser.json`.

## Visual identity review

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll identity "$PWD/.artifacts/identity/gui" "$PWD/examples/Neon Orchard/Neon Orchard.seqvium"
```

Uses isolated preferences/layout and opens the supplied project read-only, without Play. Compares
resolved font streams against embedded SHA-256, verifies static weights without simulations,
Russian/English letters and the musical-symbol fallback. Captures real Playlist/Rack/Mixer, Piano
and About in RU/EN × Dark/Light at FullHD and reduced size and measures toolbar field alignment.
Asserts unchanged music/history. Setup is programmatic; native screen captures have separate receipts.
The existing `catalog` and `matrix` routes cover wider shared-control/field boundaries.

`tools/branding/check-windows-icons.py <Desktop-exe> <ignored-output>` captures actual OS-extracted
icons, with optional `--window <HWND>` / `--shortcut <lnk>`. See the
[identity report](../../docs/experiments/SEQ-VISUAL-IDENTITY_REPORT.md) for observations and limits.

## Daily musical workflow check

Use an **independent project copy** with at least three Patterns and melodic notes. The scenario
creates a Pattern and exercises New → Save on that copy. Never pass the official demo or owner work.

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll daily-workflow "$PWD/.artifacts/daily-workflow/gui" "C:/Independent/music.seqvium"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll daily-workflow "$PWD/.artifacts/daily-workflow/rendered" "C:/Independent/music.seqvium" routed
```

The default route uses foreground-guarded native input, real pickers, Pattern playback on the default
output, direct/remembered editors, context menu and New Cancel/Save. Foreground refusal or device fault
fails the attempt. `routed` raises events on actual retained controls/windows and opens the supplied
fixture through the workflow; it verifies RU/EN × Dark/Light rendering, editor/rail/context choices,
long-name cards, New dialogs and musical-layout restoration, without native input or physical output.
Its JSON explicitly marks those limits. Audio transport/PCM invariants belong to focused production
coordinator/renderer tests. Unique captures/JSON/config and modified fixture copies stay local/ignored.

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
The `gestures` command runs a shorter matrix for focused input/alignment verification, including
committed toolbar Gain fractional drafts, Enter/Escape, invalid input, focus loss and stale authority.

## Native song structure check

`song-structure <output-root> <independent-project> <audio-source>` authors a bounded musical study
through guarded native Windows input. Supply an independent Neon Orchard copy with its managed media
outside `examples`, and a stereo WAV at least four seconds long. The published demo is never the target.
The scenario creates named ranges, swaps Pattern/Audio sections, inserts a repeated Groove, detaches
one keys clip, edits its octave phrase through Rack/Piano and tests mixed drag/Cut/Paste/Duplicate,
crossing refusal/reveal, exact history and native Save/Open/Play. Dark/Light EN/RU fields are measured
against their real text presenters. Source and routing assertions remain canonical and hardware
playback is separate from subjective musical/acoustic judgement.

Start with the check window visible and leave it active. A one-time normal Z-order raise and title
activation click must pass exact OS hit testing; it neither uses TopMost nor clicks through another
application. Every musical input retains the exact foreground guard. A covered window or lost focus
stops the scenario. Use `--timeout-seconds=240` for this longer authoring route. Native screen/render
captures and failed attempts remain ignored local evidence under the normal retention policy.

## Native Audio Clip check

`audio-fixtures <new-independent-directory>` copies the official Neon Orchard project and its media
outside the repository, then generates original 180-second/short pad WAVs and Windows-encoded MP3/FLAC
source fixtures. It never edits the official demo or authors musical JSON. Fixture encoding requires
the installed Windows codecs; codec failure is not a successful fixture set.

`native-audio <absolute-ignored-output> <independent-project> <author|drop|check|edit|review|mixer|play>`
uses guarded SendInput and production pickers/controls. `author` imports/edits/routes/splits/saves;
`drop` performs actual Explorer drag of `Sources/Library piano.wav` (supply only a task-owned copy).
`check` exercises cancellation/errors/BPM/12 source aliases/history and native portable Save As;
`edit` exercises actual left/right trim and corner-fade pointer gestures with Undo; `review` renders
RU/EN Dark/Light and exact field alignment in maximized/restored windows; `mixer`
checks the Audio Clip source's real Mixer meter, Mute/Solo and Undo/Redo; `play` exercises physical
seek/range loop and native whole-Song WAV. Internal musical observations are read-only, not authoring.
The check phase creates large original silence fixtures inside the independent Sources directory;
keep adequate temporary disk space. It never changes device/system volume settings.

Explorer is normally medium-integrity. If the verification terminal is elevated, use
`Start-NativeUserCheck.ps1 -Executable <built-VisualCheck.exe> -Arguments <quoted-native-command>
-WorkingDirectory <repo>` to launch the isolated host with the **same user's interactive Explorer
token**. This deliberately lowers verification privilege; it does not change security settings,
grant administrator access or bypass Windows' cross-integrity drop protection. Ordinary app users
should simply launch Seqvium without elevation. `Read-NativeElements.ps1` reads geometry only from
the specified native dialog/Explorer window; typing and drag remain actual native input.

## Native Mixer check

`native-mixer <absolute-output> <independent-project> <mix|review|play>` uses guarded Windows
mouse/keyboard input and the production controls/dialogs. Supply an explicit independent project **and** its associated
managed media; the route changes only that project. `mix` is the Neon Orchard
balance/EQ/compression/bus workflow. `review` checks normal and compact RU/EN Dark/Light fields,
coexisting Playlist/Rack layout and single-transaction fader/pan/EQ Undo/Redo, restoring musical state.
`play` deliberately runs the whole saved Song on physical output, native Save/Open and native WAV
export. No musical JSON/API authoring is used; internal observations are read-only assertions.
Foreground/occlusion refusal is a failed native attempt, never permission to send input elsewhere.
Use isolated evidence/config, bounded timeout and the existing failed-window rescue rules.

`DeviceCheck mixer-project <project> <native-wave> <original-wave> <report-directory>` is a separate
read-only physical diagnostic. It creates no project/media/WAV: submitted float PCM is captured only
in bounded memory, compared with offline execution at the device rate, then the native GUI WAV is
compared with a 48-kHz float render within PCM16 rounding. The JSON distinguishes physical output,
submitted PCM, acoustic listening and callback/service allocations/deadlines. See the
[report](../../docs/experiments/SEQ-R11-MIXER_REPORT.md) for actual measurements and limits.

## First Drum Voice musical check

`rack-pads <absolute-output>` reviews compact fixed pad geometry at three widths in RU/EN × Dark/Light,
scroll reachability, native row painting/history and physical playing-column rendering.
`native-track <absolute-output> <project> <library> <phase>` authors Neon Orchard exclusively by native
Windows input; phases are rhythm, melody, preview, arrange, balance, seeds, seeds-forte and play. Project edits,
pickers,
transport and exports use real controls, never musical APIs/JSON. It saves checkpoints and reopens between
phases; dirty failures retain the window for native rescue. Use only the explicit new composition target.
The play phase deliberately runs the entire Song on physical output. See the
[report](../../docs/experiments/SEQ-CHANNEL-RACK-MUSIC_REPORT.md) for scope/results and licensing boundaries.

`drum-kit <absolute-output> <delivery-directory> --timeout-seconds=300` authors four Patterns and
16 bars using guarded native Windows input in Rack, Piano Roll and Playlist, auditions all seven
drums and performs native Save/Open and Song WAV pickers with physical output. It creates a unique
example directory outside Git. `drum-kit-review <absolute-output> <project> --timeout-seconds=180`
inspects every editor in RU/EN Dark/Light ordinary/minimum panes and verifies native portable preset
Save/Load with single Undo. Review target/layout setup is separate from native musical authoring.

`drum <absolute-output> [delivery-directory]` creates a seven-hit synthesized kick through actual
production Rack controls, edits exact values and a captured fine dial, and verifies both editable
envelopes through foreground-guarded native input. RU/EN Dark/Light ordinary and compact renders
check real text/unit alignment and reachability of every parameter through local scrolling.
The same workflow verifies Undo/Redo, media-free Save/Open, quiet audition, continuous Pattern/live
sound changes, finite Song and both WAV exports. **This command plays sound on the current default
output.** Parameters are restored after input probes. It changes no system/user preferences and uses
isolated project/layout state; optional delivery preserves the authored example outside rolling artifacts.
No subjective listening, long pressure or native file-picker acceptance follows from this route.

## Captured musical controls

`control-input <absolute-output> --timeout-seconds=600` creates a new Pattern and Clap with guarded
Windows mouse input, then exercises all seven instruments, their dials and envelope handles, local
audition velocity, graph Gain and initialized Mixer channel/Master balance. Variable-radius curved
paths check direction and capture, Shift precision, stationary clicks, Escape and actual Ctrl+Z.
Horizontal paths leave the track above and below. Samples record requested/delivered coordinates,
capture and event delivery latency. Fixture pane placement and visual language/theme changes are
explicit setup, separate from native input. It plays quiet hits through the actual default output:
changed commit audition, eight rapid retriggers, stable output/PCM identity, absence of a delayed
backlog and a separate local sample Stop. It changes no user project or device/system volume.

`gain-mixer` restricts the scenario after its initial Clap regression to Gain/Mixer. `velocity-only`
restricts subsequent checks to the seven audition-level instances. `audition-only` stops after the
initial commit/Undo, rapid Play/Stop and localized ordinary/reduced visual review. Layout fixtures also
exercise intermediate width/height changes, docked neighbours, breakpoint jitter, precise focused text,
Escape and resizing a captured gesture with one commit/Undo. `instrument=Clap` limits instrument iteration
without relabeling other instruments as checked. `routed` uses synthetic routed
events in the real window and reports `NativeWindowsInput=false`; it is never a native pass.
`reproduce` adds a short attach window before the initial amplitude press for comparing the original
crash. Foreground, occlusion or input-delivery refusal remains failed/partial evidence. Launch this
interactive check with a visible window; hiding the native startup window can invalidate activation.
Screenshots, traces, private fixture projects and original crash logs stay in ignored `.artifacts/`.

## Catalog and central-change demonstration

`catalog` opens real standard/primary/compact/icon/danger buttons, sliders, drawn Gain levels,
numeric fields, selection/toggle controls, disclosure, diagnostics, menus and tooltip in both themes.
Named fixture pseudo states expose hover/pressed/focus/disabled/error rendering; those columns are **synthetic states**,
not native Windows mouse evidence. Not every state applies to every component.

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
It requests ordinary activation only; it never bypasses the guard or exercises a native file picker. Its
media/projects/PNG/JSON are ignored run artifacts,
subject to the same rolling retention. Device-independent musical PCM checks stay in Tests.

## Piano Roll and physical musical check

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano "$PWD/.artifacts/seq-r9/gui"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-music "$PWD/.artifacts/seq-r9/music" "C:/path/to/Seqvium Sample Library" "C:/path/to/preserved-examples"
```

`piano` uses authored PCM in an isolated production window, edit audition disabled, no audio device.
Routed checks compose chords/melody, move/copy/resize/cancel, edit separate chord velocity, keyboard
selection, exact group properties/Undo, root/source settings and late-notification guards. It checks
shared Rack previews, Save/Open and graph WAV. RU/EN × Dark/Light ordinary/reduced renders measure
actual pane containment and label/TextPresenter alignment. Routed events are not native input.

`piano-music` explicitly plays through the normal physical output at current system volume. It reads
the supplied private catalog, accepts verified VSCO single-note WAVs (MIDI roots 57/45), then composes
19 upper and four lower notes through guarded Windows SendInput: chords, different duration/velocity,
move/Ctrl-copy/resize/Escape/Undo. Real workflow APIs handle import, save/reopen, shared Playlist repeat
and Pattern/Song export, not native picker input. It checks pitched audition, continuous Pattern,
finite Song, live-edit/Undo convergence, library hashes and exported PCM frame counts/peak/saturation.
`piano-music.json` separates native input, physical execution and unavailable acoustic judgement.
No pressure, high-DPI, other-OS or all-device claim follows. The optional third directory receives a
unique preserved project/media, Pattern/Song WAV and theme renders outside temporary rolling retention;
source WAV/catalogs are never modified. Without it, output remains isolated/ignored and temporary.

## Independent-process Single Instance checks

`Check-Processes.ps1` starts two independent hosts through the same production `RunDesktop` path,
with an internal isolated identity. It checks one document initialization, successful activation,
restoration of the existing minimized window, graceful shutdown, explicit crash and reacquisition.
Separate `noipc`/`stalled` hosts hold the real lease while a Desktop secondary must time out with a
diagnostic and no document. The script terminates only the Process objects it created, joins them with bounded waits and
disposes
their handles. Child stdout/stderr and partial results survive failed runs.

The private `desktop`, `lease`, `noipc`, `stalled`, `activate` commands belong to this developer tool;
the production Desktop exposes none of them. Protocol tests require no GUI/audio/device. Different
users/logon sessions, elevated-versus-unelevated launches, high DPI and Linux/macOS need separate
runtime evidence. Current-user access and user-wide lease scope are implemented, not exhaustively
platform/security-certified by one Windows session.

## Official demo inspection

`native-mixer <output> <independent-project> inspect` uses the real Open picker and guarded Windows
input to select all saved Patterns, open the drum/Source and Piano editors, and inspect Playlist and
Mixer. It reads canonical state only as an oracle and requires unchanged music plus Play readiness.
The `play` phase plays the complete finite Song on the physical output, checks real meters and
exports `Neon Orchard - Mixed.wav` beside the supplied project through the native picker.
Both phases save through production GUI: always supply an independent project **with its media**,
never a protected original. The existing `play` phase is specific to the 32-bar publication baseline.

```powershell
& 'examples/Neon Orchard/Restore-Checkpoint.ps1' -Snapshot 'examples/Neon Orchard/checkpoints/publication-01.json' -Destination 'C:/Temp/Neon Orchard check'
tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.exe native-mixer "$PWD/.artifacts/neon-orchard/inspect" 'C:/Temp/Neon Orchard check/Neon Orchard.seqvium' inspect --timeout-seconds=180
tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.exe native-mixer "$PWD/.artifacts/neon-orchard/play" 'C:/Temp/Neon Orchard check/Neon Orchard.seqvium' play --timeout-seconds=150
```

Start with a fresh output WAV destination to avoid overwrite prompts. Full Song lasts 68.571 seconds;
the explicit deadline also covers native pickers, captures, Save and joined shutdown. Initial
inspect takes about 101 seconds and all 15 Patterns plus 12 distinct sound editors are captured at
FullHD. Evidence is native input plus production screen/render captures, not subjective listening.
These artifacts remain ignored and follow normal retention. Use the matching executable under a
custom `--artifacts-path` build when ordinary application DLLs are locked.

## Evidence and maintenance

The `arrangement` route renders the actual Playlist in RU/EN and Dark/Light at ordinary/reduced sizes,
checks routed placement/move/Ctrl-copy/repeat/cancellation/stale gestures, track M/S/name/color,
snap/overview/stationary headings/fit, group move/duplicate/Undo, ruler seek/range, pointer zoom,
BPM popup Play availability, actual pane-edge capture, fresh maximized/normal window restoration,
Save/Open and graph Song WAV. It measures
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

## Saved project playback

`project <output-directory> <project-file>` opens an explicitly supplied short saved composition through
the production workflow and real audio output. It verifies Song to its finite end and continuous Pattern,
each at the original tempo, changed tempo and Undo-restored tempo. The bounded fixture observes at most
about three seconds per Play and therefore suits short regression projects such as Piano001. It captures
renderer state/screens and restores the original snapshot without saving the supplied project. Preferences
and verification artifacts remain isolated. This route plays actual project audio at unchanged device/system
volume; it supplies execution evidence, not an acoustic listening judgement.

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
