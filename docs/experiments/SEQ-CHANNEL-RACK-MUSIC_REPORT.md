# Channel Rack and native musical composition

Role: Evidence and limits for compact Rack geometry and the native Neon Orchard workflow.
Read when: Reproducing this checkpoint's GUI composition and measurements.
Authoritative for: Methods, observed results, failures and retained evidence.
Not authoritative for: Product contracts, subjective listening, release or platform acceptance.

## Result

The Rack's rhythmic cells are fixed 26×26 DIP, with shared bevel/cap, hover, pressed recess,
active indicator/glow, keyboard focus and independent playing outline. Rows are 44 DIP. Auto-sized
beat cells and separate bar gaps replace proportional columns; narrow content scrolls. Rich-note
previews retain the full row rather than collapsing with the fixed pads. The Pattern navigator
reserves a shared-metric scrollbar gutter so its cards' right edges remain visible.

Real composition exposed repeated unchanged Gain commits on focus loss after Enter, which could
consume the first Save click. Current matching unchanged values now withdraw the draft without
dispatching another command; stale/changed edits keep validation/history. Progress was saved through
native Ctrl+S before repairing, closing and reopening the project. The native balance scenario then
changes and immediately saves all 15 occurrence gains successfully.

## Neon Orchard

A warm A-minor electronic/organic-house score, **112 BPM, 32 bars, 68.5714375 s**, with 15 Patterns,
nine named Playlist tracks, 78 placements, 19 source Parts and 191 NoteEvents. Intro → first groove
→ airy break → stronger groove → return → quieter resolved coda. Variations change kick rhythm,
hat density, bass octave/syncopation and chord voicing; pitched velocity-controlled toms mark transitions.
All seven drums use factory snapshots and the linked Closed/Open Hat interaction. Piano Roll draws
the melodic notes/chords and applies exact velocity through the actual properties flyout.

Patterns: Pulse, Pulse variation, Backbeat, Hats, Hats variation, Wooden pulse, Falling toms,
Root motion, Root variation, Orchard keys, Open voicings, Glass seeds, Night air, Spark and Coda.
Tracks: Pulse, Backbeat, Cymbals, Wood, Transitions, Bass, Keys, Seeds and Air / sparks.
All bring audible scheduled material; no empty tracks are counted.

Five library originals are used read-only:

- MR `BassSynth_Moe-Dee_Multi_A1` for bass, explicit root A1.
- VSCO2-CE `Player_dyn1_rr1_018` for keys, catalog root A3.
- VSCO2-CE `LLVln_Pizz_A3_f_RR1` for pizzicato, catalog root A3.
- VSCO2-CE `LDFlute_expvib_A3_v1_1` for flute, explicit root A3.
- MR `DNB_FX_13` for the transition texture, unpitched use.

Explicit roots from filenames are user musical settings, not detected/calibrated pitch. The actual
application imports nine managed WAV resources (repeated imports and the replaced quiet trial have distinct IDs) beside
the project.
All six inspected original SHA-256 hashes, including the quiet pizzicato trial, are unchanged after native
authoring/reopen/export. Library licensing
notes were inspected; originals and reference resources were not edited or redistributed in Git.

Delivery outside Git: `C:/Users/filed/Desktop/Neon Orchard - Seqvium/Neon Orchard.seqvium`, its
`Neon Orchard.seqvium.media/`, and **`Neon Orchard.wav`**. `Comparisons/` retains first/second balance
Songs and isolated Glass Seeds before/after WAVs for the owner's loudness feedback. Keep the project and media folder
together.

At the owner's feedback, native toolbar occurrence gains raise keys/flute/pizzicato more than drums:
drums 0.30–0.32, bass 0.34, keys 0.45, final pizzicato 0.90, flute 0.48 and FX 0.36. No fictional master/Mixer
was added. Final native Song export is **48 kHz stereo PCM16, 3,291,429 frames**, peak **0.7347106934**,
energy **13307.5298**, zero saturated samples. First balance peak was 0.5769043, energy 7914.4497.
Full reopened Song reaches Ended on physical output with a maximum of 12 observed simultaneous voices.

Owner listening identified Glass Seeds as effectively masked. Read-only analysis measured the original
piano-dynamic violin pizzicato at peak 0.0029907 (about −50 dBFS); gain/velocity alone could not make
it useful. The forte recording peaks at 0.1133423. Native GUI imported it, redrew the same 16 notes,
applied velocity 85 and removed the quiet trial channel only after saving the replacement. Isolated
native Pattern exports improve from peak 0.0020142 / energy 0.0309861 to peak 0.0866699 / energy
19.763261: approximately 33 dB of peak improvement. Final reopened full Song/export retains the
same 191 notes/78 clips and no clipping. Originals remain untouched. The current attenuating Gain
range is not an automatic sample normalizer; sample-recording levels require musical source selection.

## Method and GUI evidence

`NativeTrackCheck` uses foreground-guarded Windows mouse/keyboard input: actual panes, Rack menus,
instrument presets, native source pickers, root properties, Piano Roll draw/velocity, Pattern selectors,
Playlist track editor/placement, Save/Open, Start/Play/Stop and native export picker. Model/control geometry
is read-only observation. It never calls musical edit, persistence, transport APIs or writes project JSON.
Each section saves a checkpoint; phases reopen the saved project in a fresh production Desktop friend host.
Shutdown uses native Alt+F4 after saving. A dirty failure keeps progress for native rescue instead of
silently saving via workflow APIs. Host identity/preferences remain isolated from ordinary Seqvium.

Reproduce after the Release build:

```text
Seqvium.VisualCheck.exe rack-pads <absolute-artifact-parent> --timeout-seconds=180
Seqvium.VisualCheck.exe native-track <absolute-artifact-parent> <project> <library> <phase> --timeout-seconds=600
```

Native-track phases: rhythm, melody, preview, arrange, balance, seeds, seeds-forte, play. Rhythm/melody/arrange are
resumable
through the GUI, with existing notes/placements checked before native input. Play deliberately opens
physical output for the whole Song and exports the final WAV through the application. Do not reuse
the creative fixture against unrelated owner projects. A fresh export destination avoids overwrite dialogs.

The final Rack route passes 460/1000/1650-DIP widths × RU/EN × Dark/Light, constant geometry,
horizontal reachability and unchanged music across layout/theme changes. Native drag painting commits
once, Ctrl+Z/Ctrl+Y preserve note identity, and physical Play renders the current pad. Both themes'
actual images were inspected. Layout fixture bounds/theme setup is separate from native musical input.
The saved rich Coda row's wide thumbnail opens the exact 16-note Piano Part without changing music.
Native balance review checks every Pattern card edge outside the scrollbar in both themes.

Final local runs under ignored `.artifacts/channel-rack/`:

- `track/20261013T222418972-acfaa8a277404c5ea2d5124ecf3a9cbb`: final reopened whole Song and native WAV, measured JSON.
- `track/20261013T222337847-ad13c12faf334d6d90d71dd09b70fcfc`: native forte replacement and measured Pattern exports.
- `pads/20261013T221553737-0a036c2374654ce7b61d2a11cac46681`: final compact matrix/native paint/history/playhead.
- `gestures/20261013T222540947-e3ac788bba884d8f91cf8dcdd990f785`: final rendered field matrix and routed/native Graph
  checks.

Screens/JSON are rolling local review evidence, not distributed product assets or durable clone links.
Music is preserved separately on the Desktop; reusable source checks remain under `tools/Seqvium.VisualCheck`.

## Failures and boundaries

Native picker automation initially used slash-separated Windows filenames, popup Apply selection included
spinner buttons, and an empty-grid focus click drew an extra note. The harness now normalizes picker paths,
targets actual Apply automation IDs and focuses via Select; extra notes were removed through the GUI.
An async picker/save observation race and native shutdown completion ordering were corrected in the harness.
Overlapping flute/FX clips refused correctly; FX moved to the existing Transitions lane through GUI placement.
Early native foreground refusals remain failures; no forced foreground or synthetic pass is substituted.
Startup observation now waits for renderer state instead of assuming an immediate first refresh.

Shared-style catalog and rendered field-alignment/routed checks are distinct from native Graph smoke.
An early unrelated Graph native micro-click assertion failed in the inherited gestures fixture;
the final unchanged Graph run passes all 11 native checks and routed input. The initial attempt remains
a recorded failure, not a diagnosed product repair. All 12 rendered field rows pass across RU/EN ×
Dark/Light; the shared-style catalog also passes. Native Rack/track evidence remains independent.

Final focused L0/L1/L2: zero-warning/error Release Tests/reference build (2.62 s CLI), **168/168**,
zero skips (8.733 s CLI), filtering StepSequencer, DesignSystem, HostPresentation, PianoRoll,
Arrangement and ProductVersion classes. Later tool-only source changes build without warnings/errors (1.69 s CLI) and
pass native musical scenarios. The preceding drum checkpoint's full L3 passes
1179/1179; no redundant full suite is claimed for this second UI task. Source/diff and changed-doc
relative-link checks pass. The `run-tests` skill selects SDK 10 MTP/xUnit v3 invocation/filter syntax;
this influences verification, not musical authoring or product architecture.

No automation, per-channel Mixer, FX processing, sample trim/loop mapping, FLAC import, stereo drum synthesis
or custom choke groups are introduced. Linked hats use one binary shared group. Subjective acoustic listening
is unavailable to the agent: physical output/PCM metrics cannot establish musical taste or long pressure
stability. Earlier R4 pressure failures, other OS/high-DPI acceptance and owner sound judgement remain open.

One BUILD checkpoint advances **0.1.4.0000 → 0.1.4.0001** for compact Rack and composing repairs;
it does not repeat the preceding drum PATCH or reset L3 cadence. No staging, commit, push or tag occurs.
