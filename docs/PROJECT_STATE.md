# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline and active evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology or live Git status.

## Current checkpoint

Product alpha **0.1.11.0000**, **SEQ-R11-DELAY-F1 — musical Delay / stateful Mixer FX**.
Mixer Inspector exposes Enabled, Time 10–1000 ms, Feedback 0–75% and Mix 0–100% on channels,
buses and Master. Delay follows Compressor before balance/fader/sends, using one consumer-owned
stereo line per summed strip. Compatible edits preserve time/voices/history, smooth coefficients
and crossfade Time taps. Natural Pattern/Song rendering includes a bounded late echo; ordinary
Pattern loops retain state, Stop/Panic/Seek/range wrap clear it without long settling. Original note
gates and R8's local before-Mixer tap remain intact. A 64 MiB per-state line budget, existing publication
slots/work gates and maximum 120-s serial tail keep preparation/render finite. Default disabled Delay
is omitted from JSON; non-default intent requires reader 1.7. Existing Undo/Redo/Save/Open/media own
persistence. [Audio](AUDIO_ENGINE.md#musical-delay-v1-seq-r11-delay-f1),
[UX](UX_CONTRACT.md#implemented-mixer-workflow), [format](PROJECT_FORMAT.md#mixer-delay-reader-17)
and the [report](experiments/SEQ-R11-DELAY-F1_REPORT.md) own details/evidence/limits.

Previous **0.1.10.0000**, **SEQ-R10-STRETCH-F1 — local Audio Clip time stretch**. Existing
Playlist clip properties expose an explicit 50–200% duration multiplier and playback duration, with
a distinct non-unity header cue. Worker-prepared stereo-linked WSOLA preserves local pitch for the
bounded sustained/monophonic phrase scope. Original native ranges/fades and immutable managed media
remain canonical; copies own independent stretch. Split preserves common derivation/phase, repeats
use cumulative playback frames, and BPM never changes physical duration or enables stretch.
Realtime/offline retain the same sampler/Mixer/publication/export owners. Reader 1.6 gates non-unity
intent; old projects default to unity without raising their gate. Source envelopes are 0.1–60 s and
unique derived PCM totals at most 128 MiB inside the existing audio aggregate. There is no Follow
Tempo, warp editor or guarantee for transients/dense polyphony. [Audio](AUDIO_ENGINE.md#local-audio-clip-time-stretch),
[UX](UX_CONTRACT.md#implemented-audio-clip-workflow), [format](PROJECT_FORMAT.md#local-time-stretch-reader-16)
and the [report](experiments/SEQ-R10-STRETCH-F1_REPORT.md) own contracts/evidence.

**SEQ-R8 — Resampling** remains implemented. Pattern navigator **Create sample** renders
the identified Pattern through its supported local graph, before Mixer/Master, into independently
managed reusable project audio. Browser opens with the result selected, ready for preview/Rack/
Playlist. Source content remains editable. The fixed 48-kHz mono/stereo PCM16 result includes musical
rests, release and one-shot tails; unsupported required local dependencies, overload and the 16 MiB
sampler limit are explicit refusals. A frozen request has progress/Cancel, a cooperative deadline,
strict generation/lifecycle invalidation and joined teardown. Durable placement and acceptance reuse
WavImport with one Undo edit; media survives Redo/Save As/relocation/Open without original sources.
No automatic replacement, Song/audible capture, VST or new roadmap stage is included.
[Sample workflow](SAMPLE_WORKFLOW.md#implemented-pattern-resampling-seq-r8) owns bounds;
[R8 report](experiments/SEQ-R8_REPORT.md) owns evidence and remaining limits.

The first real drum-editing crash is corrected from
its reproduced `KeyNotFoundException: pitchAmount` in amplitude pointer capture. Shared Drum/Gain/Mixer
dials follow relative circular motion; captured horizontal levels follow X off the track. The Drum
footer keeps spacious icon-only Play and local sample Stop accessible, auditions changed accepted
commits outside music and reuses resident ready PCM/output for retrigger. Dials shrink continuously to
a usable minimum before wrapping within the allocated pane; Play/Stop stays outside parameter scrolling.
Preparation keeps only the latest
pending request; cancellation/transport/lifecycle relevance prevents stale starts. Evidence and limits
belong to the [control input report](experiments/SEQ-CONTROL-INPUT_REPORT.md).

A unified compact Settings window owns General,
Controls and branded About/version. Language/theme duplication and the Appearance UI are retired;
the title-row sun/moon opens explicit checked catalog selection. Thirteen application commands have
real remapping, named conflict transfer, clear/default/reset and compatible independent persistence.
Browser auto preview now uses the same persistent global behavior preference. Old language/theme/layout
files remain compatible and musical project/Undo ownership is unchanged. New global options must use
this center and new application keys must use HostCommands. These rules are routed through AGENTS,
INDEX and [Settings](SETTINGS.md#unified-global-settings-center), the current Markdown knowledge base;
no executable RAG index is installed. Editor-local remapping/profiles/controllers and actual future
VST/audio I/O/MIDI pages remain separately scoped work.

An ordered shared left zone places rail, Browser,
Patterns and musical workspace side by side. Each right boundary resizes only its left panel;
content reflows, then clips, and collapses only near the edge. Independent rail toggles restore the
last usable width. Sidebar icons form a separate top group. Reduced windows fit both panels without
changing preferences, and New/Open retain sidebar visibility. Optional compatible workspace fields
store usable/clipped widths outside project/history. [Workspace](WORKSPACE.md#shared-left-panel-zone)
owns behavior. That interface refinement adds no musical capability or reader gate.
Theme changes continue to invalidate retained custom drawings without replacing their controls.

The optional default-enabled Percussion Lab
provides useful Kick/Clap generation, real recipe variations/locks and a twelve-candidate host history.
The retained Sample Lab supports standalone exploration, raw solo, bounded shared PCM Rack audition
through current local processing/Mixer, independent managed acceptance and explicit shared replacement.
Standalone work survives unrelated edits; captured context never follows selection. Accepted audio
survives generator disable and portable Save As/Open/export, without format migration or external SDK.
[Sample workflow](SAMPLE_WORKFLOW.md#implemented-sample-lab-v1)
owns scope; the [R7 report](experiments/SEQ-R7-SAMPLE-LAB-V1_REPORT.md) owns evidence and limits.

The owner's follow-up also removes permanent instructional/technical clutter across the first-party
working surfaces. Sample Lab/Browser/Extensions lead with short actions and sound; redundant Gain/Mixer
readouts and Piano/source-property explanations are reduced or disclosed. AGENTS/UI design require
actual reference-image inspection and a density gate before future UI edits. Reader 1.5 covers
sections/ancestry; reader 1.6 adds explicit local stretch and reader 1.7 gates non-default Delay. [Versioning](VERSIONING.md) owns identity.
No release, staging, commit, push or tag is performed. R1–R3 remain complete in their bounded scopes.
R4 remains partial: F1 accepted-ready, F2 pressure acceptance unresolved, F3-A conditionally accepted,
F3-B owner interaction/visual review pending. R5 Rack/music, R9 Piano, bounded R10 Playlist/audio and
R11 Mixer are implemented; wider stages and First Track/owner aesthetic/acoustic acceptance remain
separate. [Roadmap](ROADMAP.md) owns remaining scope.

## Implemented capability

R6 discovers bounded schema-1 manifests under the application deployment Extensions root, checks
required/optional host contracts before activation and never scans/executes arbitrary DLLs. Exact
trusted factory registration is needed for executable providers. Uses block disable until work and
dependent sessions finish; the strict R6 adapter cancels on document edits, while Lab uses its bounded
relevance rules above. New/Open/Close join sessions,
and application shutdown retires providers. Failed activation/disposal and missing/invalid/incompatible
packages retain host diagnostics. Metadata labels use host RU/EN fallback and shared Dark/Light controls.
A separate user file remembers disabled IDs; accepted media and its Undo history stay project-owned.

Desktop has Browser, Channel Rack, Piano Roll, Playlist, Mixer, Node Graph/Editor, Project Inspector
and the global Settings center. Compact studio-instrument resources, movable/resizable/docked panes, English/Russian and
Dark/Light share preferences and workspace persistence. Normal startup and project Open stay silent;
user-wide Single Instance and scoped device/output lifetime remain unchanged.

Browser opens with Ctrl+B/rail/Panes, supports pinned/recent folders, bounded one-level pages and
name search, and lists current project audio without reopening external originals. Selection shows
actual metadata/waveform; explicit/optional automatic preview stays outside music/Undo and refuses
while transport plays. WAV enters Rack through its existing bounded sampler; WAV/Windows MP3/FLAC
and managed resources enter Playlist. Reuse keeps ResourceIds/media without copying, with one Undo
per use and portable Save As/Open. [Sample workflow](SAMPLE_WORKFLOW.md#implemented-seq-sample-browser)
owns boundaries; [report](experiments/SEQ-SAMPLE-BROWSER_REPORT.md) owns verification and limits.

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
exact range/gain, explicit local stretch/repeats and shared-source Mixer routes use existing history,
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
linked compression, musical Delay and optional sample-peak protection. Audio Clip contributions use this same
processing, not a parallel mixer. Compatible prepared changes retain time/unrelated voices.
[Design system](DESIGN_SYSTEM.md) and [UI design](UI_DESIGN.md) own compact shared visual norms.

## Verification baseline

Latest L0/L1/L2/L3: **SEQ-R11-DELAY-F1, 2026-10-16**. Full Release solution build has zero
warnings/errors; **1471/1471 tests pass**, zero skips, one thread (**78.127 s CLI**). The 21 added
Delay cases cover actual impulse/timing/decay/mix, mono/stereo, channel/bus/Master/sends/Mute/Solo,
realtime/offline float and PCM16 export, late echoes, Stop/Seek/loop, live control/voice gates,
state transfer, memory/tail bounds and history/reader/unknown state. Focused related checks pass
112/112 (9.182 s), followed by 70/70 Delay/OfflineSampler boundary/memory cases (2.767 s).
Warm callback paths measure zero allocation; unprimed observations and earlier failed runs remain
explicit in the [report](experiments/SEQ-R11-DELAY-F1_REPORT.md), not hidden as pressure acceptance.

Production windows pass RU/EN × Dark/Light ordinary/reduced empty/populated Mixer, 24 zero-DIP
field alignments, real control edits/invalid input, history, Save/Open and 188400-frame WAV late echoes.
Final guarded Windows Time/bypass/enable pass, with routed setup/selection. Physical Samsung USB C
Earphones at 48 kHz stereo reaches natural Ended at frame 188400 with zero voices and unchanged epoch
under live Mix. Earlier foreground refusals and harness corrections are recorded. Catalog passes.
No subjective listening, full native authoring, stress/DPI/other-platform or R4-F2 closure follows.

Prior L0/L1/L2/L3: **SEQ-R10-STRETCH-F1, 2026-10-16**. Full Release solution build has zero
warnings/errors; **1450/1450 tests pass**, zero skips, one thread (**60.159 s** CLI summary). The
**23 new stretch cases pass** (4.702 s); the earlier 162-case related audio/persistence/metadata
subset passes (22.012 s), followed by the additional live Pattern-continuation case in L3.
Final source-identity and right-edge-trim refinement passes **48/48** focused stretch/AudioClip cases
(4.932 s), with a zero-warning affected Release build (6.56 s) and captured left/right trim GUI checks.
Tests establish duration/fundamental/stereo/endpoints, exact realtime/offline PCM, production PCM16
export, split/fades/repeats/tempo/copy independence, bounded allocation/cancel/stale/closed publication,
zero callback allocations in the selected seek/loop check, history and portable media.

Real production windows pass localized ordinary/reduced properties, empty startup/populated Playlist,
48 zero-DIP field alignments, rejected/dismissed edits, one Undo/Redo, portable Save/Open and four-second
export. Guarded Windows properties/percentage/Apply pass with routed clip selection. Physical output
passes through Samsung USB C Earphones, 48 kHz stereo, ending at 192000 frames with zero voices.
An earlier run reached DeviceFault and is not a physical pass; its initial overly permissive tool
summary is explicitly superseded by strict EOF/fault checks and the successful repeat. No subjective
listening, full native musical authoring, stress, DPI or other-platform acceptance follows.
The [report](experiments/SEQ-R10-STRETCH-F1_REPORT.md) retains methods, attempts and limitations.

Prior L0/L1/L2/L3: **SEQ-R8, 2026-10-16**. Full Release solution build has zero warnings/errors;
**1427/1427 tests pass**, zero skips, one thread (**70.769 s** final CLI summary). New focused checks pass
**27/27** (5.133 s): independent two-instrument local signal/release oracles, seven drum tails,
mono/stereo and 44.1/48-kHz source conversion, Mixer exclusion, frozen state, packet cancellation,
unsupported/missing dependencies, overload/size refusal, stale late completion, lifecycle joins,
one managed Undo/Redo, Rack/Playlist reuse and relocation after original media removal.

The real-window routed three-instrument workflow passes with exact accepted/local PCM, stable Browser
reveal, reuse, history and Save As/New/Open. Native Windows right-click, popup action and independent
managed acceptance pass separately. Physical source and Browser sample output pass on Samsung USB C
Earphones at 48 kHz stereo. RU/EN × Dark/Light ordinary/reduced empty windows, context menus and
populated Browser are reviewed. An active search is cleared and the result stays selected after
deferred notifications. [R8 report](experiments/SEQ-R8_REPORT.md) records evidence, corrected search/polling
selection races, final GUI refinement and limits. Physical delivery is not subjective listening,
pressure/DPI/platform/aesthetic acceptance. Existing R4 pressure gaps remain open.

Prior L0/L1/L2: **SEQ-CONTROL-INPUT, 2026-10-15**. Affected Release builds have zero warnings/errors;
**265/265 focused tests pass**, zero skips, one thread (**27.285 s**). This includes drum commit/Undo,
burst coalescing, Stop/Panic/close, ready retrigger authority, long-sample Stop/restart and Piano audition
ownership. Final aligned-label refinement additionally passes **97/97 focused presentation/editor tests**
(16.492 s), localized ordinary/reduced layout/precision fixtures with native focus/Escape/Undo and
the shared component catalog. Original Windows crash events and independent native reproduction establish the missing
pitch-only key read in amplitude capture. The [report](experiments/SEQ-CONTROL-INPUT_REPORT.md) separates
native mouse/output checks, routed all-owner coverage, responsive layout/precision fixtures and remaining
foreground/input-delivery limits. This is focused verification, not a new L3 or pressure/acoustic acceptance.

Prior L3: **SEQ-SETTINGS-F1, 2026-10-15**. Full Release solution build has zero warnings/errors;
**1384/1384 tests pass**, zero skips, one thread (**99.262 s** CLI summary). Final minimum-size/custom-reset/theme-affordance refinement reuses that L3 with **105/105 focused
tests** (17.642 s), zero skips and a zero-warning affected Release build. Real production windows pass RU/EN × Dark/Light at
720×570 and 500×340 settings, with main window down to 640×480, search/capture/conflict/default/reset,
file reload, real binding dispatch/old-key removal, text-local history, About and explicit quick-theme
selection. The shared component catalog passes. A narrow Settings-source corpus exception additionally passes
76/76 ProjectStats tests (1.964 s); other settings/private trees remain excluded. Native Windows input
attempts are foreground/coverage
refused; rendered/routed evidence is not relabeled as native input. No physical audio, other-platform,
DPI, accessibility certification or owner aesthetic acceptance follows. The
[Settings report](experiments/SEQ-SETTINGS-F1_REPORT.md) owns methods, failures and limits.

Prior L0/L1/L2: **SEQ-LEFT-PANELS, 2026-10-15**. Affected Release test-project build and its
production/tool references: zero warnings/errors; **260/260 focused tests** pass, zero skips,
one thread (19.270 s), including 75 ProjectStats cases. Eight added layout cases cover independent usable/clipped widths,
malformed-file
preservation and Browser separation from musical docks/empty layout. Actual Windows mouse checks
cover both boundaries, independent collapse/reopen, neighbour-width stability and ordered displacement
at 640×480/900/1400 DIP. Routed cancellation/reversal/keyboard/focus/layout checks, RU/EN Dark/Light
empty/populated review, retained-theme compositor checks, component catalog and field matrix pass.
Browser discovery/acceptance/history/portable Save/Open also passes in the real routed window.
Compact disclosure chevrons remain inside one header surface in both themes; current rendered
catalog fixtures check three widths and collapsed/expanded geometry. ProjectStats completion now
refreshes the ignored root reports and requires an inspected compact summary in agent handoffs.
This is focused verification, not L3, physical audio, other-platform/DPI or subjective acceptance.
Commands, observations and bounded native refusals belong to the
[work log](archive/WORK_LOG.md#seq-left-panels--2026-10-15).

Earlier L3: **SEQ-R7-SAMPLE-LAB-V1, 2026-10-15**. Full Release solution build: zero warnings/errors;
**1341/1341 tests pass**, zero skips, one test thread (**62.326 s** CLI summary). Numerical tests cover
finite/transient/decay/spectral differences, actual reference/lock behavior, exact processed-audition
versus accepted-PCM agreement and unrelated voice/time continuation. Host tests cover cancellation,
late results, lifecycle, captured/stale targets and managed history/portability.
Final UI/availability follow-up: zero-warning Release solution build and **189/189 focused tests**
(5.665 s); this reuses the L3 baseline rather than claiming a second full regression run.

Real-window routed Sample Lab creation/reference/lock/history/acceptance/disable/reopen/export passes,
with RU/EN × Dark/Light ordinary/reduced views and zero-DIP exact-field center differences. A populated
Neon Orchard review composes all ten first-party panes in four language/theme combinations. Physical
solo, Pattern/context/original output passes through Samsung USB C Earphones, 48 kHz stereo. Native
Windows action input is foreground-refused because another window covers the check. These establish
neither ordinary native acceptance nor subjective acoustic/aesthetic judgement, stress or other platforms.
The R7 report separates the tiers and retained evidence. Earlier R6 and cross-stage evidence remains
in its owning reports; inherited pressure/allocation issues are not closed by this capability.

### Verification cadence

Completed repository tasks since this L3: **0**. This risk-selected Delay L3 covers reader gates,
stateful lifetime/finite source and effect boundaries, realtime publication and existing production regressions. No broader platform, aesthetic, pressure
or acoustic closure follows.
Prior visual identity passes **60/60 focused tests**
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

Finish wider Playlist/container/warp and compact editor work only through their scoped owners.
Audio is resident, not streamed: 512 MiB per source/unique audible Audio Clip PCM, separate 128 MiB
Pattern PCM, shared 32 voices. Old prepared states, waveforms/codec/UI and managed-media/history disk
consume more; no global media quota. MP3/FLAC use Windows-only Media Foundation via NAudio, not a
portable universal decoder. Surround, tempo following and warp maps are absent; bounded local stretch
has material/range/budget limits documented above. Sections are organization,
not processing containers; crossing clips need explicit split/range adjustment. Variations do not
inherit future note edits. Dynamic tracks/groups/collapse and per-repeat detachment remain absent.
Browser inspection followed by preview decodes external sources twice; three-minute source latency
and allocation observations justify a bounded follow-up, not a new cache policy during this audit.

Reverb, automation, sidechain/plugin racks, true-peak/LUFS, latency compensation, Personal Library,
wider Sample Lab context/reference models, MIDI, recording and recovery remain future work. Cold seek/range wrap
resets Mixer FX without preroll. R4-F2 pressure failures, maximum-channel stress, aesthetic/acoustic
review, Linux/macOS runtime, high/mixed DPI, cross-monitor and accessibility remain unevidenced.
[Known problems](KNOWN_PROBLEMS.md), [debt](TECH_DEBT.md) and [portability](PORTABILITY.md) retain
their respective unresolved facts; a green full Song does not close them.
