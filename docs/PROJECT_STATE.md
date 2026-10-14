# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline and active evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology or live Git status.

## Current checkpoint

Product alpha **0.1.7.0003**, SEQ-VISUAL-IDENTITY: one BUILD for the integrated 09 brand, embedded
typography, optical EXE/window icons and reproducible publication kit. The preceding daily workflow
adds playing Pattern navigation, direct/remembered editors, New and remembered musical layout.
The preceding shared WAV replacement preserves musical/processing/Mixer intent. The preceding
SEQ-R10-SONG-STRUCTURE PATCH introduced section arranging, variations and mixed clip editing.
[Versioning](VERSIONING.md) owns identity;
reader 1.5 is a separate format gate for sections/Pattern ancestry.
No release, staging, commit, push or tag is performed. R1–R3 remain complete in their bounded scopes.
R4 remains partial: F1 accepted-ready, F2 pressure acceptance unresolved, F3-A conditionally accepted,
F3-B owner interaction/visual review pending. R5 Rack/music, R9 Piano, bounded R10 Playlist/audio and
R11 Mixer are implemented; wider stages and First Track/owner aesthetic/acoustic acceptance remain
separate. [Roadmap](ROADMAP.md) owns remaining scope.

## Implemented capability

Desktop has Channel Rack, Piano Roll, Playlist, Mixer, Node Graph/Editor and information/appearance
panes. Compact studio-instrument resources, movable/resizable/docked panes, English/Russian and
Dark/Light share preferences and workspace persistence. Normal startup and project Open stay silent;
user-wide Single Instance and scoped device/output lifetime remain unchanged.

Navigator single click switches a playing Pattern to the new phrase at zero, with the same output
and sticky Stop/Panic. Double-click/Enter opens a supported melodic Part directly in Piano or rhythm
in Rack; explicit editor/Part choices persist per Pattern for this document session. Context menu
offers named Piano channels; wider two-line cards improve long-name access. Playlist uses the same
editor decision. New/Ctrl+N implements Save/Discard/Cancel and creates a pristine silent document.
Empty startup/New shows only a compact Rack; previous musical layout returns when music is opened.

The [brand kit](../brand/README.md) supplies primary 09, retained 01/07 alternatives, reconstructed
SVG/optical masters, exports and font/reference provenance. Title/About use the new identity;
Onest Regular/SemiBold/Black and JetBrains Mono load from application resources with a bundled music
symbol fallback. Both themes retain musical colors and density. README uses real demo captures;
Social Preview awaits the owner's GitHub settings upload. [UI design](UI_DESIGN.md) and
[Design System](DESIGN_SYSTEM.md) own the corresponding visual/technical rules.

Rack's **Replace WAV…** command updates a shared PCM sampler source across all Patterns with one
Undo entry; known catalog Root Pitch is adopted, otherwise previous tuning remains. Notes, release,
local graphs and Mixer routes/settings persist. [Sample workflow](SAMPLE_WORKFLOW.md#shared-wav-replacement)
owns cancellation, durable media and explicit tuning/level limits.

Rack/Piano provide shared pitch/duration/velocity, 1–32-bar Patterns, continuous playback/live edits,
steps, chords, move/copy/resize, exact properties, ghosts and explicit sample Root Pitch. Seven
synthesized drum families, 21 starting presets, editable envelopes, linked hat choke and portable
custom sound presets reuse shared sound intent. Legacy Rack/graph WAV import remains bounded to
16 MiB. Source/Gain/Mix/Output graphs, transient audition, managed media and history remain working.

Playlist mixes overlapping/repeated Patterns with independent Audio Clips. Explorer WAV/MP3/FLAC
drop and Playlist picker need no Pattern/Rack. Real mono/stereo waveform, move/copy/trim/split/fades,
exact range/gain, fixed-speed repeats and explicit shared-source Mixer routes use existing history,
managed Save/Open/portable Save As and realtime/offline Song export. BPM/placement do not imply
stretch/pitch. Musical ticks and native PCM frames remain distinct; split preserves envelope/phase.
32 organizational lanes, track Mute/Solo, snap/overview/seek/range loop remain independent of Mixer.
[Audio](AUDIO_ENGINE.md#standalone-audio-clips), [format](PROJECT_FORMAT.md#audio-clips-reader-14),
[architecture](ARCHITECTURE.md#implemented-standalone-audio-clip-boundary) and
[UX](UX_CONTRACT.md#implemented-audio-clip-workflow) own the actual contracts.

Named/color sections navigate, swap adjacent ranges and insert shared repeats with ripple movement.
Crossing clips refuse restructuring atomically with a named cause and direct reveal. Mixed Pattern/
Audio selection supports captured group move/copy, session clipboard, duplicate/delete and exact
placement with common history. One-clip variations receive independent notes/supported local graphs
and weak ancestry while keeping shared sounds/resources/Mixer routes. Shared-use/variation cues and
compact section forms reuse the visual system. [UX](UX_CONTRACT.md#implemented-playlist-song-structure)
and [architecture](ARCHITECTURE.md#song-structure-and-explicit-variations) own these boundaries.

Mixer retains explicit sound assignments, channels/buses/Master, level/trim/stereo balance,
Mute/Solo, measured levels/reduction, acyclic output/pre/post sends, high-pass/three-band EQ,
linked compression and optional sample-peak protection. Audio Clip contributions use this same
processing, not a parallel mixer. Compatible prepared changes retain time/unrelated voices.
[Design system](DESIGN_SYSTEM.md) and [UI design](UI_DESIGN.md) own compact shared visual norms.

## Verification baseline

Latest L3: **SEQ-R10-SONG-STRUCTURE, 2026-10-14**. Locked isolated full Release solution build has
zero warnings/errors; **1235/1235 tests pass**, zero skips (51.042 s CLI summary, four test threads).

### Verification cadence

Completed repository tasks since this L3: **3**. Visual identity passes **60/60 focused tests**
(15.741 s CLI), zero-warning affected Release builds, actual font-stream hashes, RU/EN Dark/Light
FullHD/reduced workspace/About/Piano, component catalog and broader field matrix. Real EXE, Shell
shortcut and live 16/24 px window icon extraction pass. Foreground guard refused taskbar/Alt+Tab
screen input; other-platform/high-DPI and owner aesthetic acceptance remain unestablished.
[Identity report](experiments/SEQ-VISUAL-IDENTITY_REPORT.md) owns the bounded observations/limits.

Daily workflow passes **353/353 focused tests**
(24.732 s CLI, one test thread) and affected Release builds with zero warnings/errors; this is L1/L2,
not a full suite. It adds transport/lifecycle/editor/layout regressions; real-window routed checks
on an independent demo cover New Cancel/Save, direct
Piano, remembered Rack, long names and restored layout. RU/EN × Dark/Light renders pass. Native input
was refused because Telegram covered the entire check window; a physical attempt failed with WASAPI
Starvation. Neither attempt establishes native/physical acceptance. Deterministic transport uses
the production coordinator/preparation/renderer with a substituted output borrower.
The follow-up shared WAV replacement passes 91
focused cases, including six new preservation/tuning/cancel/error/stale cases; it is not another L3.
Native shared sample/picker/history observations and complete private-project Rack/Piano/Playlist/
Mixer inspection are scoped in the [replacement report](experiments/SEQ-SHARED-WAV-REPLACEMENT_REPORT.md).
Focused section/variation/mixed-edit checks and the
component catalog pass. Native independent musical authoring verifies mouse groups/clipboard,
sections, isolated Rack/Piano variation editing, visible crossing refusal, history and Save/Open.

Independent Neon Orchard copies receive native imports/move/range/fades/split/history/Mixer edits,
real Explorer drop, cancellation/error tests, Save As/reopen without original sources, ruler
seek/range loops and native WAV. RU/EN Dark/Light actual windows/fields and restored 1400×900 review
pass. Full 68.571-second mixed Song through UR12 44.1 kHz has zero submitted-PCM/offline error,
zero callback allocations/misses and no saturation; GUI 48-kHz PCM16 differs only by rounding.
[Audio Clip report](experiments/SEQ-R10-AUDIO-CLIPS_REPORT.md) owns measurements, failed attempts,
retained evidence and limits. This does not imply acoustic judgement or general pressure closure.

Official [Neon Orchard](../examples/Neon%20Orchard/README.md) and publication-01 remain musically
unchanged: 112 BPM, 32 bars, 15 Patterns, 191 notes, nine tracks/78 Pattern clips and licensed media.
An independent outside-Git study has 40 bars, six sections, 16 Patterns/207 notes, 104 Pattern clips
and one private Audio Clip. Native full Song through Samsung USB C Earphones at 48 kHz and production
WAV export pass: 85.714292 seconds, 13 peak voices, zero clipped frames. Actual section fields pass
RU/EN Dark/Light alignment. [Song structure report](experiments/SEQ-R10-SONG-STRUCTURE_REPORT.md)
distinguishes these observations from subjective listening, callback/pressure and platform claims.
Existing permanent demo tests participate in L3.

## Current focus and remaining gaps

Finish wider Playlist/container/stretch and compact editor work only through their scoped owners.
Audio is resident, not streamed: 512 MiB per source/unique audible Audio Clip PCM, separate 128 MiB
Pattern PCM, shared 32 voices. Old prepared states, waveforms/codec/UI and managed-media/history disk
consume more; no global media quota. MP3/FLAC use Windows-only Media Foundation via NAudio, not a
portable universal decoder. Surround and time stretch/warp are absent. Sections are organization,
not processing containers; crossing clips need explicit split/range adjustment. Variations do not
inherit future note edits. Dynamic tracks/groups/collapse and per-repeat detachment remain absent.

Delay/reverb, automation, sidechain/plugin racks, true-peak/LUFS, latency compensation, Browser/
Personal Library, Sample Lab, MIDI, recording and recovery remain future work. Cold seek/range wrap
resets Mixer FX without preroll. R4-F2 pressure failures, maximum-channel stress, aesthetic/acoustic
review, Linux/macOS runtime, high/mixed DPI, cross-monitor and accessibility remain unevidenced.
[Known problems](KNOWN_PROBLEMS.md), [debt](TECH_DEBT.md) and [portability](PORTABILITY.md) retain
their respective unresolved facts; a green full Song does not close them.
