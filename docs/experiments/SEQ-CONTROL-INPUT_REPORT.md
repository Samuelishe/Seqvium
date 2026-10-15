# SEQ-CONTROL-INPUT — drum crash and captured controls

Role: Reproducible observations and verification limits for the blocking first-use control defects.
Date: 2026-10-15.
Scope: Existing drum editor, shared rotary/level controls and their Gain/Mixer owners. R8 is not started.
Contracts: [UX](../UX_CONTRACT.md#detailed-node-editor-access), [components](../DESIGN_SYSTEM.md#drum-instrument-components).

## Established crash cause

Windows Application events 1026/1000 record three production `Seqvium.Desktop.exe` crashes at
14:58:03, 14:59:01 and 15:04:28 local time, version 0.1.8.0003. All identify an unhandled
`System.Collections.Generic.KeyNotFoundException` for `pitchAmount`. The managed stack is:

```text
ImmutableDictionary.get_Item("pitchAmount")
DrumEnvelopePlot.get_PitchScale()                  original line 35
DrumEnvelopePlot.PointerPressed handler             original line 73
Avalonia.Input.MouseDevice.MouseDown / Win32 input dispatch
```

The owner's images show the first amplitude handle in the Clap editor. A new isolated real window
reproduced the same exception using Windows input: New Pattern, Add drum / Clap, drag amplitude
attack. The captured process state identifies `core.drum-voice.clap-v1`, `AmplitudeMode=true`, and
eight parameter keys without `pitchAmount`. This is a missing pitch-only key read during an amplitude
gesture, not an inferred audio-device fault. The debugger attach attempt timed out; no suspended
frame/value inspection is claimed. The original Windows exception and independently reproduced stack
establish the cause without debugger inference.

## Correction

Amplitude gestures capture their own scale and never read a pitch parameter. Existing pitched
envelope scaling remains intact. The common rotary control now integrates signed angular motion:
clockwise increases, counterclockwise decreases, without a press jump or seam wrap. An insensitive
center and center-crossing re-anchor prevent ambiguous half-turns. Clamping each increment allows
immediate reversal at either limit. Shift precision, keyboard actions, default reset and single
commit/cancellation ownership remain available across Drum, Gain and compact Mixer balance.

The drum audition level reuses the drawn captured Gain level control. A continuous track and outlined
thumb replace the visibly separated light cap in the short framework slider. Its 0.05–1 velocity
mapping is local to audition and has no document history. Both horizontal uses follow X while
captured outside the track vertically; repeated identical values avoid redundant feedback.

Waveform requests cancel superseded work, join the previous worker and wait 40 ms for input to
settle before synthesis. Values and analytical curves remain immediate. Gain preview caches graph
diagnostic reasons for the same immutable state/node instead of re-inspecting the project on every
mouse event. No pointer-move path starts audible audition. Mixer captures the actual starting balance
so a stationary dial click cannot apply a stale cached balance.

The audition footer centers Play/Stop, a 104×40 note selector and a 128×40 captured level as one row,
with symmetric padding and common row centers. At narrow widths it centers two rows. There is no
visible Play label; tooltip/accessibility names remain localized. Local sample Stop withdraws both
preparing and pending intent and cuts a ready voice at the next audio boundary. It leaves Play free
for repeated retrigger. Draft text no longer adds a bottom row: only actionable warnings use a fixed
identity-row slot, so feedback cannot move Play/Stop or the working area.

Accepted changed commits audition only the same lifecycle/source/generation outside musical playback.
Cancellation, Escape, Undo, no-op and rejected stale proposals do not create audition intent. Audio
failure does not revert the accepted edit. A healthy drum audition reuses one output/session and
prepared transient PCM while resident. EOF/Stop retain the existing transient retirement policy;
subsequent Play may prepare again in the same output. Ready requests use an authority-bound Start; retirement backpressure cannot
restart an older active source. During preparation one active operation and one replaceable latest
intent are retained, rather than a queue of delayed hits. Stop/Panic, target/suspension and project
close withdraw pending requests. Piano audition retains joined ownership transfer and source validation.

Responsive layout uses the Drum editor's allocated bounds inside the singleton Node Editor, with
separate enter/leave column and footer thresholds. Dials continuously shrink from 76 to 64 DIP as
width/height shrink, independently of column count; contracting cells then wrap. Graph height varies
within 180–250 DIP. Fonts/72-DIP exact fields do not shrink; envelope
grab tolerance remains 18 DIP. Very short viewports keep vertical overflow; horizontal scrolling is
disabled. Pointer edit ownership freezes sizing from Begin, including focus callbacks before actual
capture. Whole-DIP parameter cells reserve row slack to avoid a rounded final cell wrapping. Text
draft ownership prevents an unchanged field's LostFocus from completing a newly begun dial gesture.
Focused text controls survive reflow without replacement and retain full decimal precision. Unfocused
values use compact formatting (1041.667 ms displays as 1041.7); canonical values/history are unchanged.
Drum parameter labels now consistently select RU/EN, including Tune/Decay/Tone/Noise. A common two-line
label slot keeps neighbouring knobs/fields aligned when a translated label wraps.

## Verification method and observed limits

`VisualCheck control-input` uses an isolated production window and real Rack controls, `SetCursorPos`
and Windows `SendInput` with exact foreground and hit-test guards. Variable-radius curved dial paths,
curved envelope paths and horizontal paths displaced above/below the control inspect delivered
coordinates and capture. Project Undo uses actual Ctrl+Z. Layout placement, language/theme selection
and intentionally forced application capture loss are separately identified setup/routed evidence.
The `routed` fallback reports `NativeWindowsInput=false`, even though it uses real retained controls.

Earlier attempts failed/refused on a covered foreground window, unsettled rendered targets or
unattached menu items. They are not passing native evidence. An early horizontal assertion also
compared a pre-press fraction with the screen-pixel-quantized press: observed 0.3382 to 0.3235,
`capture=True`. The corrected driver samples the actual post-press value before assessing subsequent
direction. It waits for delivered pointer events rather than assuming that a fixed delay delivered
them. A routed attempt reached a local velocity limit inherited from its previous instrument;
the fixture now selects a direction with actual headroom rather than requiring movement below zero.

Affected Release builds pass with zero warnings/errors. Initial focused L1/L2: 222/222 tests,
zero skips, 92.515 s. After the shared audition-level change: 125/125 affected presentation, editor,
binding and version tests, zero skips, 53.892 s. The earlier audio/drum/Mixer checks remain applicable
to the unchanged synthesis and project contracts. These are focused checks, not a new L3 suite.

The final validation record below distinguishes native input, routed input, fixture layout/text setup
and physical renderer evidence. Short physical output is not subjective listening acceptance or the
existing unresolved heavy-pressure qualification. Other platforms and DPI remain outside this check.

One earlier 264-case run passed, and another observed a 7,576-byte warmed allocation assertion in
`MixerTests.EqAndCompressionArePartitionIndependentAndRenderingAllocatesNothing`. The isolated 19-case
Mixer rerun and a subsequent 264-case run passed (38.331 s). No allocation requirement was relaxed,
and no origin/fix for that intermittent observation is claimed. Earlier new-test failures also exposed
headless icon creation (moved to visual attachment), a controlled output that needed consumer pumping
for musical Start, and expected termination after cancellation; those fixtures/lifetimes were corrected.

During early GUI iterations, unjoined UI observations after Undo caused intentionally stale synthetic
presses; the driver now waits for the displayed editor generation. Native attempts also observed
unsettled menu targets, wrong instrument creation (now explicitly checked), coordinate-delivery timeouts
and one reported physical output failure. These attempts are not counted as passing native coverage.
The first shutdown attempt after adding editor cancellation exposed a call after owner disposal;
the closed/disposed guard prevents that call and later joined shutdowns pass. User breakpoints and
original screenshots remain untouched.

## Completed final checks

| Boundary | Executed evidence and result |
| --- | --- |
| Release L0/L1/L2 | Affected production/test/tool builds: zero warnings/errors. 265/265 focused MTP cases, zero skips, one thread, 27.285 s. Covers drum state/history, coalescing/preparation cancellation, Stop/Panic/close, long-sample Stop/restart, authority/backpressure and Piano audition transfer. Final label alignment refinement: 97/97 focused presentation/editor/design/version cases, 16.492 s. |
| All instrument/control owners | `control-routed-complete/20261015T141330028-bd72859bd9184e59b2b49addf56fa8a7`: 81 checks / 124 traces, all seven drums, envelope handles and local levels, graph Gain and initialized Mixer channels/Master. Real retained controls with routed Avalonia events; **not Windows input**. |
| Native user scenario | `control-native-accepted-final/20261015T141532231-ec2ad57dd70e4591bb0ef0bcd0817b0d`: 17 checks / 20 traces pass using Windows input. New Pattern/Clap, amplitude/Undo, all Clap parameters with variable-radius curves, fine movement, Escape/Undo, off-track audition level, Gain circular/level edge cases and Mixer channel/Master. Forced capture loss remains explicit routed setup. Other six drums have routed coverage; full native seven-instrument coverage is not claimed. |
| Rapid physical audition | Eight native Play clicks each advance the rendered epoch, one output, no new PCM while retriggering resident Clap, no trailing delayed epochs, local Stop passes. Samsung USB C Earphones, 48 kHz stereo. Observed request-to-epoch check times across the final native runs are 49.8–69.8 ms, including the driver's 40-ms button hold; these are not acoustic latency measurements. |
| Final layout/precision | `control-layout-native-final/20261015T141757879-3407270ffa74407b858af415d52f885b`: latest aligned-label layout passes RU/EN × Dark/Light ordinary/reduced renders, independent width/height changes, 300×210 minimum, intermediate sizes, docked Node Editor beside Browser/Patterns, jitter bands and neighbour-knob alignment. Geometry/docking/text assignment are fixtures; focus, blur, Escape, curved captured resize and Ctrl+Z use Windows input. Full decimal returns on focus, blur creates no edit/sound, resize preserves a typed draft, captured resize commits once and Undo restores. No horizontal overflow; Play/Stop remains visible. |
| Shared component catalog | `control-catalog-complete`: both themes and fixture normal/hover/pressed/focus/selected/disabled/invalid states pass, including audition buttons/level. Shared resource mutation/restoration and slider/drawn-control propagation pass. Rendered numeric/unit centers differ by 0 DIP in ordinary/reduced Drum reviews. These catalog pseudo states are fixtures, not mouse evidence. |
| Existing Rack/Gain GUI route | `control-steps-final` passes the existing `steps` scenario with angular Gain drag, fine adjustment, cancellation, keyboard and history checks. Its routed events remain separate from the native scenario above. |

Measured width-only shrink (fixture pane width 760/700/640/580/520 DIP) produces dial sizes
75.8/72.4/69.0/65.6/64 DIP. Column count cannot increase while width shrinks; it changes only after the
useful size floor. At truly short heights readable parameters remain vertically scrollable. The
300×210 minimum prioritizes Play/Stop and necessarily leaves very little parameter viewport.
The final successful native runs do not erase earlier foreground/delivery refusals. Wider native
instrument coverage, other DPI/platforms, sustained pressure and owner acoustic/aesthetic acceptance
remain outside these results.

## Reproduction and evidence retention

```text
dotnet build tools/Seqvium.VisualCheck/Seqvium.VisualCheck.csproj -c Release --no-restore
tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.exe control-input <ignored-output-parent> --timeout-seconds=600
```

Launch this explicitly interactive check visibly. `gain-mixer` and `velocity-only` provide focused
follow-ups; `routed` is a separate fallback. The original crash logs, individual input-attempt logs,
trace JSON and actual-window renders remain locally under `.artifacts/control-*`, subject to normal rolling retention.
They are diagnostics, not distributed runtime assets. The owner-provided Desktop images are untouched.
No Git staging, commit, push or tag is performed.
