# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline and active evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology or live Git status.

## Current checkpoint

Product alpha **0.1.6.0000**, SEQ-R10-AUDIO-CLIPS: one PATCH for standalone audio arrangement,
resetting BUILD. [Versioning](VERSIONING.md) owns identity; reader 1.4 is a separate format gate.
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

Mixer retains explicit sound assignments, channels/buses/Master, level/trim/stereo balance,
Mute/Solo, measured levels/reduction, acyclic output/pre/post sends, high-pass/three-band EQ,
linked compression and optional sample-peak protection. Audio Clip contributions use this same
processing, not a parallel mixer. Compatible prepared changes retain time/unrelated voices.
[Design system](DESIGN_SYSTEM.md) and [UI design](UI_DESIGN.md) own compact shared visual norms.

## Verification baseline

Latest L3: **SEQ-R10-AUDIO-CLIPS, 2026-10-14**. Locked isolated full Release solution build has
zero warnings/errors; **1224/1224 tests pass**, zero skips (19.046 s CLI summary).

### Verification cadence

Completed repository tasks since this L3: **0**. Focused Audio Clip cases pass 25/25, including exact
split/resampling/fade/BPM boundaries, 25 shared-source uses and Save As, cancellation/missing media,
Mixer and allocation-free seek/loop/live-edit. Pristine audio-only Desktop workflow also passes.

Independent Neon Orchard copies receive native imports/move/range/fades/split/history/Mixer edits,
real Explorer drop, cancellation/error tests, Save As/reopen without original sources, ruler
seek/range loops and native WAV. RU/EN Dark/Light actual windows/fields and restored 1400×900 review
pass. Full 68.571-second mixed Song through UR12 44.1 kHz has zero submitted-PCM/offline error,
zero callback allocations/misses and no saturation; GUI 48-kHz PCM16 differs only by rounding.
[Audio Clip report](experiments/SEQ-R10-AUDIO-CLIPS_REPORT.md) owns measurements, failed attempts,
retained evidence and limits. This does not imply acoustic judgement or general pressure closure.

Official [Neon Orchard](../examples/Neon%20Orchard/README.md) and publication-01 remain musically
unchanged: 112 BPM, 32 bars, 15 Patterns, 191 notes, nine tracks/78 Pattern clips and licensed media.
Only an independent outside-Git exam adds audio. Existing permanent demo tests participate in L3.

## Current focus and remaining gaps

Finish wider Playlist/container/stretch and compact editor work only through their scoped owners.
Audio is resident, not streamed: 512 MiB per source/unique audible Audio Clip PCM, separate 128 MiB
Pattern PCM, shared 32 voices. Old prepared states, waveforms/codec/UI and managed-media/history disk
consume more; no global media quota. MP3/FLAC use Windows-only Media Foundation via NAudio, not a
portable universal decoder. Surround, time stretch/warp and mixed audio/Pattern marquee are absent.

Delay/reverb, automation, sidechain/plugin racks, true-peak/LUFS, latency compensation, Browser/
Personal Library, Sample Lab, MIDI, recording and recovery remain future work. Cold seek/range wrap
resets Mixer FX without preroll. R4-F2 pressure failures, maximum-channel stress, aesthetic/acoustic
review, Linux/macOS runtime, high/mixed DPI, cross-monitor and accessibility remain unevidenced.
[Known problems](KNOWN_PROBLEMS.md), [debt](TECH_DEBT.md) and [portability](PORTABILITY.md) retain
their respective unresolved facts; a green full Song does not close them.
