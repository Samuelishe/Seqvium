# Desktop visual and lifecycle verification

## Unfold Pattern repeats

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll unfold-repeats "$PWD/.artifacts/unfold-repeats/review" --timeout-seconds=240
```

The isolated synth study uses a non-bar Pattern length, nonzero Song start and 7/8. It checks actual
context menus, dismissal, single-cycle availability, individual selection/move/clipboard/variation,
editor targeting, exact Undo/Redo, Busy/hide/selection/stale ABA invalidation and Save. RU/EN ×
Dark/Light at 1080/520 DIP covers populated and empty Playlist. Default input is explicitly routed.
`native` uses guarded Windows input and waits at most 45 seconds for deliberate foreground after
coverage refusal; it never clicks through another app. `audio` separately checks short physical
Song output and live Unfold position/epoch. Canonical numerical PCM belongs to UnfoldRepeatsTests.
Screenshots/projects/JSON stay ignored locally. Physical delivery is not subjective listening.

## Piano relative Transform and pitch captions

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-transform "$PWD/.artifacts/piano-transform/review" audio --timeout-seconds=180
```

The isolated Synth/chord study checks exact independent fields, default/no-op, Apply/Cancel/Escape/
close, history, Free Snap, stale/Busy/Part ABA/hide, retained Popup/focus and real numeric presenter
alignment. RU/EN × Dark/Light at 1080/520/360 DIP includes populated and empty Parts. A separate
chromatic C4–D5 matrix checks actual pitch-glyph ink containment above the velocity stripe at
1080/360 DIP. Default input is explicitly routed; `native` uses guarded Windows input, including
Unicode exact numeric entry. Setup, Snap, Part/theme/layout and label-fixture changes use APIs.
`audio` separately checks physical Pattern/Song Apply/Undo/Redo and 1600-ms open-Popup stability;
canonical numerical PCM and portable history live in PianoTransformTests. Delivery, native/routed
input, rendered review and subjective listening remain separate evidence classes. `quick` skips
the layout/label matrix for addressed input and physical checks. Loopable physical fixture gates
are separate from the three-tick note's exact 3→5-tick GUI/mathematical check.

## Piano Humanize

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-humanize "$PWD/.artifacts/piano-humanize/review" audio --timeout-seconds=300
```

Creates an isolated Synth chord/phrase and shared repeats. Guarded native mouse/keyboard selects
the chord, chooses Free Snap, adjusts both sliders and exact percentages, cancels/escapes, applies
zero and nonzero settings and checks exact Undo/Redo. RU/EN × Dark/Light at 1080/520/360 DIP includes
real flyouts, actual field alignment and empty Parts. External generation and pane hide cancel drafts.
`quick` keeps all languages/themes at 360 DIP. `audio` deliberately plays Pattern/Song through the
current physical output, retains an open flyout/focus under observations and uses native Apply/Undo/
Redo without transport reset. Save/Open/export and fixture/theme/layout changes use production APIs,
separately from native musical input. Device delivery is not subjective acoustic acceptance.

## Piano Quantize and Scale

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-tools "$PWD/.artifacts/piano-tools/review" audio --timeout-seconds=300
```

Creates an isolated four-note Synth fragment and shared Playlist repeats. Guarded Windows input
boxes three notes, chooses Snap, Quantizes, changes Snap before exact Undo/Redo, selects C Major/A
Natural Minor and switches Parts. RU/EN Dark/Light at 1080/520/360 DIP includes empty/populated
renders, ghost/view/zoom/scroll/hide retention and no document/history edits. The existing repeat
menu and Piano editor verify local Quantize; a one-tick end note verifies whole-operation refusal.
`audio` deliberately plays the current physical Pattern/Song output and holds the scale Popup under
transport observations; no system volume changes. `quick` repeats English Dark/Light at 520 DIP.
Fixture/project setup and theme/layout changes are API-driven; musical input is native. A foreground
refusal waits up to 60 seconds for deliberate activation and never sends input through another app.
Canonical PCM tests, physical delivery, screen review and subjective listening are separate evidence.

## Rack channels and Pattern Gain

For live Gain acceptance, `rack-gain <output> native` deliberately plays a small isolated sustained
Synth fixture on the current physical output. Guarded native held fader trajectories must change
actual DSP meters before release without canonical history or execution/voice reset; Escape restores,
release accepts one Undo and native Undo/Redo restore sound. Playing reorder/cancel/Ctrl-copy and
routed Graph/Editor/overflow/capture/Stop cancellation are recorded separately in `live-gain.json`.
This is physical delivery and numerical DSP evidence, not subjective listening. No system volume changes.

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll rack-workflow "$PWD/.artifacts/rack-workflow/review" native audio --timeout-seconds=180
```

An isolated three-hit Snare fixture exercises menu Duplicate, name reorder, Ctrl-drag insertion,
history, editor double-click, fader/exact input and Save/New/Open. `native` uses guarded Windows input
for these gestures; explicit routed micro/capture-loss/outside/stale/ABA probes remain separate.
RU/EN Dark/Light at 1400/1000/640 DIP includes long names, overflow, field alignment and empty states.
A legacy invalid Gain fixture verifies exact display/repair/history and fader keyboard endpoints.
`audio` deliberately uses the real current output and checks live Gain/reorder without transport reset
or device failure. Device delivery is not subjective listening. JSON/captures and failed/refused runs
remain ignored under existing rolling retention. No composition or system-volume change is made.

## Drum play-along

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll drum-playalong "$PWD/.artifacts/drum-playalong/review" native --timeout-seconds=180
```

Deliberately plays physical audio. Uses a small isolated API-created Kick fragment; guarded Windows
input selects Snare in the real Rack menu, repeatedly plays over Pattern/Song, shapes/Undo/replays,
uses local Stop/global Stop/Panic and hides the editor. Checks unchanged transport/output, no
preview history/backlog/Busy availability pulses, localized theme/compact renders and joined
document replacement with an active preview. Geometry, language/theme, Song placement and New
are fixture setup, not native musical authoring. Omit `native` for explicitly routed fallback.
No system volume/settings change or subjective listening claim. Submitted-PCM measurements use
the separate [DeviceCheck](../Seqvium.DeviceCheck/README.md#drum-play-along) route.

## Playlist parameter automation

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll automation "$PWD/.artifacts/automation/review" native audio --timeout-seconds=120
```

Creates a four-bar held synth chord and two repeated Song uses through the production workflow.
Actual Playlist controls create Cutoff/Volume lanes; captured point move/add/cancel/capture-loss,
exact properties, invalid input, history and portable Save/New/Open are exercised. RU/EN × Dark/Light
uses 1400×900, 900×700 and 640×480 windows with 1020×650, 440×480 and locally scrolled minimum
pane frames. Real field presenters are
measured; captures include the native popup screen and transparent popup content separately.
The compact target/power/Position/Value/transition surface uses shared controls and icon actions.

Exports `cutoff-sweep.wav`, `mixer-fade.wav` and `combined.wav`, each 16.25 seconds at 48 kHz PCM16,
plus independent media-free projects. `native` separately attempts guarded Windows point dragging;
`audio` plays the combined Song on the current output, edits a live curve and checks exact natural
Ended/zero voices/unchanged epoch. Neither option turns routed setup/history into full native
authoring or establishes subjective listening. No personal reference/sample library is required.

## Polyphonic Synth Source Editor

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll synth "$PWD/.artifacts/synth-f1/review" native audio --timeout-seconds=360
```

Creates an isolated synth channel via Rack, writes a chord through Piano, opens the same Source
from the graph, and checks real parameters, invalid/draft/preset handling, locale changes, ADSR
history/capture/cancel/wheel conflict, all factory snapshots and portable Save As/New/Open/export.
All eight exact parameters reject malformed/below/above-range input without losing drafts or
changing canonical state. Each shows its own range immediately below the field, with localized
units and tooltip/accessibility help; compact error images cover each parameter in both themes/languages.
RU/EN × Dark/Light uses 1920×1080, 1280×800 and 900×700 windows with 760×660, 640×630 and 360×520
pane frames. It captures short/long envelopes, focus/pressed/error, actual held/released output and
canonical long names. Separate 4× Release control renders and font-outline/ink metrics supplement
visual review; they do not establish native high-DPI support. Field axes/centers, preview geometry,
wave hit targets and overflow are checked. Four short PCM16 WAVs remain local listening fixtures.

`native` waits for the real Windows foreground event, then retains foreground/hit-test guards.
With `audio`, the compact RU/EN × Dark/Light matrix also exercises all eight dials and the ADSR
curve on a held voice: draft isolation, one commit, Undo and Escape, stable epoch/voice/output and
no neighbouring availability pulse. `native` repeats those trajectories through Windows input;
repeated Hold must not toggle workspace control availability. Per-trajectory JSON and captures stay
in the same ignored run. These checks establish physical delivery, not subjective sound quality.
Activate the visible check window when needed; no input is sent through another application.
In reduced and compact panes, each Saw → native Sine changes SoundDefinition/display, native Undo/
Redo round-trips, and Save/Open retains the choices. Note selection, held state, live release tail,
natural completion and joined Stop are checked. Native failure fails the scenario after receipts
are written. The original Cutoff/waveform protocol is replayed once diagnostically, not retried to
obtain a pass. `audio` adds physical preview and the playing/released matrix. No subjective listening
or owner aesthetic acceptance is claimed. Images/audio and comparative study remain ignored.

## PCM Sampler Source Editor

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sampler-editor "$PWD/.artifacts/sampler-editor-f1/review" native audio
```

Uses an authored stereo WAV, the existing Rack Sound Editor and bound Source double-click routes.
Checks exact range/loop/crossfade fields, short-loop refusal, one drag/Undo, Escape/capture loss, zoom/pan/Fit,
RU/EN Dark/Light ordinary/reduced windows and 40 field alignments. Save/New/Open after deleting the
external WAV preserves intent; production Pattern export and finite R8 run on reopened media.
The same production kernel writes percussion/sustain/melodic F1/zero/short/long WAV comparisons;
zero is byte-identical to F1, and acoustic preference remains unassessed. Optional native exact
Start/crossfade/Apply and optional physical Hold past source EOF/Release/Stop are recorded
separately in input-audio.json; these do not establish subjective loop quality or pressure acceptance.
Outputs remain ignored and governed by existing retention. The
[report](../../docs/experiments/SEQ-SAMPLER-EDITOR-F1_REPORT.md) records actual outcomes and limitations.

## Multisample Key Mapping

Velocity Layers has a separate library-independent route:

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sampler-velocity "$PWD/.artifacts/sampler-velocity/review" native audio
```

It generates three short stereo signals, drives the actual import review and repeated-root Split
layers action, then checks one-edit import/history, 2D source selection, exact Velocity, invalid
overlap, Cancel, Save/Open, RU/EN Dark/Light ordinary/reduced fields and a dense map. `native` checks
guarded Windows Velocity entry with routed setup; `audio` checks short Instrument previews at
20/50/90% and selected-WAV Hold/Release/Stop on the real output. Device description, advancing
callback position and failures are recorded separately in `velocity-results.json`. Neither option
establishes subjective listening, full native authoring or sustained-load acceptance. Run the
existing `sampler-editor` route for waveform/range/loop/crossfade and empty-state regressions.

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sampler-keymap "$PWD/.artifacts/sampler-keymap" 'C:/path/to/Seqvium Sample Library' 'C:/path/to/local-comparisons' native audio
```

The optional local library must contain verified piano catalog roots. No personal WAV is a build/test
dependency or distributed fixture. This route exercises batch import/history, mapped source selection,
range isolation, key boundary capture/cancel, 26-zone density, adaptive key widths/reveal/free pan,
RU/EN Dark/Light ordinary/reduced fields and source selector states, unknown-root refusal and portable
Save/Open. The same short acceptance fragment is exported to single/mapped PCM16 WAVs in the supplied
delivery directory. It is not a new song. Optional native Root/boundary input and physical preview/
Pattern output have independent results. Run `sampler-editor` for the legacy waveform/loop/crossfade
surface and empty-state matrix. [Report](../../docs/experiments/SEQ-SAMPLER-KEYMAP-F1_REPORT.md) owns
actual results/limits; ignored GUI evidence uses the existing retention policy.

## Musical Reverb

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll reverb "$PWD/.artifacts/reverb-f1/review" native audio
```

Uses a quiet authored Kick rhythm in the production Mixer Inspector. Routed Enabled/Decay/Size/
Damping/Mix edits, invalid/cancelled Decay, Undo/Redo, Save/New/Open and finite full Song WAV are checked.
The actual-window matrix covers RU/EN × Dark/Light, ordinary/reduced empty/populated Mixer and
32 field alignments. Optional `native` guards Windows Decay/bypass/enable; setup/selection stay routed.
Optional `audio` checks the real default output, live Mix, exact natural Ended/position, zero voices,
unchanged epoch and absence of device faults. No subjective listening or stress acceptance follows.
`reverb-study.json` and twelve PCM16 `study-*.wav` files compare authored percussion, four-note phrase
and sustained harmonics through dry/default/small-bright/large-dark direct DSP. The numerical summary
records peak/tail energy/stereo difference and offline elapsed time, not callback deadline performance.
Artifacts use existing ignored retention; the [report](../../docs/experiments/SEQ-R11-REVERB-F1_REPORT.md)
owns observed outcomes and limits.

## Musical Delay

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll delay "$PWD/.artifacts/delay-f1/review" native audio
```

Uses an authored quiet Kick rhythm through the production Mixer Inspector. Routed Enabled/Time/
Feedback/Mix, invalid feedback, Undo/Redo, managed Save/Open and full Song WAV late echoes are checked.
Real-window captures cover RU/EN × Dark/Light at ordinary/reduced sizes, empty/populated Mixer and
24 rendered field alignments. Inspector rebuilds can defer field materialization; the harness waits
for the visible field before input. Optional `native` separately guards Windows Time/bypass/enable;
a covered/foreground-refused window is not a pass. Optional `audio` uses the actual output, changes
Mix during Song and requires fault-free natural Ended, exact final position, zero voices and unchanged
epoch. No subjective listening, pressure, full native authoring or platform/DPI acceptance follows.
The [report](../../docs/experiments/SEQ-R11-DELAY-F1_REPORT.md) owns actual evidence and limits.

## Audio Clip time stretch

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll stretch "$PWD/.artifacts/stretch-f1/review" native audio
```

An authored two-second stereo melodic phrase enters the production Playlist import path. Actual
properties Apply 200%, verify one Undo/Redo, reject 201%, dismiss an uncommitted edit, export four
seconds and reopen managed media after removing the external WAV. The real-window matrix covers
RU/EN × Dark/Light at 1400×900/800×600, empty startup/populated Playlist and all six parameter rows. Capture
the popup TopLevel so inherited backgrounds and compositor transforms are included. Native input
separately checks opening properties, exact percentage entry and Apply; selection/setup remain routed.
Optional `audio` polls the real default output through natural EOF and refuses any device fault or
missing execution. `stretch.json` separates native, physical and routed evidence. Unit tests own
numerical pitch/PCM, deterministic cancellation, budgets and concurrent-edit publication evidence.
No subjective listening, stress or other-platform acceptance follows. Outputs use existing ignored
rolling retention; the [report](../../docs/experiments/SEQ-R10-STRETCH-F1_REPORT.md) owns observations.

## Pattern resampling

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll resampling "$PWD/.artifacts/r8/review" native audio
```

Uses isolated production Desktop/preferences and an authored Kick/Clap/Open Hat Pattern. Routed
context action, stable Browser reveal after deferred search, exact accepted/local PCM, one Undo/Redo,
Rack/Playlist resource reuse and Save As/New/Open are checked. Captures cover actual context menus and
populated Browser at 1400×900/800×600 in RU/EN × Dark/Light. Empty startup is also captured. Fixture
creation uses workflow commands; it is not native musical authoring. Optional `native` separately
records guarded Windows right-click/popup-action/managed acceptance, with refusal/failure retained.
Optional `audio` separately plays the source and Browser sample through real default output without
changing device settings; physical delivery does not establish subjective listening or pressure
acceptance. `resampling.json`, `reveal-stability.json` and captures remain ignored retention-managed
evidence. Unit tests own controlled cancellation/stale/lifecycle interleavings and relocation after
removing original media, not timing-sensitive GUI sleeps.

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

## Piano Roll selectors and Rack time geometry

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-rack "$PWD/.artifacts/piano-rack" --timeout-seconds=300
```

This route always requires guarded native Windows input and physical Pattern playback. It creates
only two isolated Synth Parts. RU/EN × Dark/Light ordinary/compact Piano checks hold each real Popup
over dozens of workflow observations, count ItemsSource changes, compare realized row identity and
screen coordinates, select by mouse and verify canvas values/history/Part switching. Constrained
Popups additionally require native wheel scrolling and mouse selection. Rack checks measure actual
pad/ruler/thumbnail coordinates for 1/2/4/8/32 bars at three pane widths, native horizontal scrolling,
far-end alignment and crossing notes. Screen captures and rendered images remain local; native
thumbnail opening, note draw/move/Undo/Redo, captured step paint/cancel, native resize/layout restore
and short physical Song with native selectors have separate receipts. `geometry` skips the already
verified selector matrix for focused Rack/gesture reruns. `baseline` records the old
selector/width failure without asserting it passes; it is useful only on affected sources.

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

## Repeat variation

`repeat-variation <output-directory> [audio]` creates a small isolated synth fixture and uses guarded
Windows right-click/popup input for RU/EN × Dark/Light, ordinary/narrow floating Playlist, zoom/scroll,
exact repeat boundaries, Escape, first/middle/last detachment and history. Existing editor navigation
opens the variation for a native Piano pitch edit while preserving the original Pattern. Optional
`audio` runs short physical Pattern/Song output; it is not pressure or acoustic acceptance. Keep
the check foreground while it runs. Generated projects and captures stay in its ignored run directory.
`repeat-variation-audio <output-directory> <saved-study>` separately checks short physical Pattern/Song
from the saved variation without native input or editing the supplied project.
`repeat-variation-empty <output-directory>` renders localized/theme empty menus and verifies the new action is absent; it is
explicitly routed evidence.

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
