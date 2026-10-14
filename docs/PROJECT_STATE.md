# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline and active evidence gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology or live Git status.

## Current checkpoint

The product alpha is **0.1.5.0001**, SEQ-NEON-ORCHARD, one BUILD for the complete official editable
demo and reusable independent-copy checks after the Mixer PATCH. Production identity is canonical under
[VERSIONING](VERSIONING.md). No release, staging, commit, push or tag is performed.

R1/R2/R3 are complete within their bounded foundations; R3 workspace acceptance is owner-confirmed.
R4 remains partial: F1 is local accepted-ready, F2 functionally implemented but physical pressure
acceptance unresolved, F3-A conditionally accepted for continued development, F3-B actual canvas/
interaction acceptance pending. The new Mixer does not close those gaps or choose a permanent engine ABI.
R5's Rack/music slice, R9's Piano Roll, R10's bounded Playlist and R11's musical Mixer are implemented;
wider stage/First Track and owner aesthetic/acoustic acceptance remain separate.
[Roadmap](ROADMAP.md) owns remaining plans; completed rationale belongs to the cold archive.

## Implemented capability

The retained Desktop has Channel Rack, Piano Roll, Playlist, Mixer, Node Graph/Editor and existing
information/appearance panes. English/Russian and Dark/Light share semantic resources and preference/
workspace persistence. Normal startup stays silent; user-wide Single Instance and explicit scoped
device/output lifetime remain unchanged.

Rack provides 1–32-bar continuous Patterns, real renderer phase, atomic painting/actions/deletion/
Undo and melodic previews. Piano Roll edits shared pitch/duration/velocity, chords, move/copy/resize,
clipboard/exact properties, snap/zoom/scroll/ghosts. Playlist provides shared/overlapping/repeated
Patterns, 32 organizational slots, groups, Mute/Solo, seek/range loop, overview and finite Song export.
Built-in Kick/Snare/Hats/Clap/Tom/Rim, 21 presets and portable custom sounds reuse shared sound state;
linked hats choke across audible channels/clips. WAV PCM sampler and local Source/Gain/Mix/Output
graphs remain working. Source Root Pitch, transient audition, managed media and Save/Open are retained.

Mixer adds explicit shared-sound assignments, channels/buses/Master, amplification/trim/stereo balance,
Mute/Solo, measured stereo peak/RMS/hold/reduction, acyclic output routes, pre/post sends, high-pass/
three-band EQ, linked compression and optional Master sample-peak protection. Song occurrences sum
by assigned sound before stateful global processing, preserving supported local graph transfer.
Playlist lanes are not Mixer channels. Canonical Mixer state uses the existing owner/history and
format reader minor 3; older projects stay unchanged until explicit creation. Prepared control updates
retain compatible voices/time, consume at packet boundaries, and do not decode media again.
[Audio](AUDIO_ENGINE.md#project-mixer-execution), [architecture](ARCHITECTURE.md#implemented-project-mixer-boundary),
[format](PROJECT_FORMAT.md#mixer-persistence) and [UX](UX_CONTRACT.md#implemented-mixer-workflow) own contracts.

The owner rejected oversized Mixer proportions. The accepted compact studio instrument direction now
uses fixed narrow strips, pinned Master, a side inspector, original metal-cap/knob/chassis materials
and thin colored identities. Shared chrome, rail, Pattern cards and Rack/Playlist rows are denser.
Light is cool steel/colored ink; Dark is slate/jade. Framework tab templates use host tokens.
[UI design](UI_DESIGN.md#compact-studio-instrument-direction) and
[design system](DESIGN_SYSTEM.md#mixer-and-chassis-components) own current norms; this is not a claim
that every editor's full radical redesign is complete.

## Demo and validation baseline

[Neon Orchard](../examples/Neon%20Orchard/README.md) is the official permanent developing demo:
112 BPM, 32 bars, 15 Patterns, 191 notes, nine Playlist tracks, 78 clips and the saved Mixer.
Six unique WAVs include four hash-verified upstream VSCO2-CE recordings under CC0 and two original
Apache-2.0 synthesized bass/FX replacements. MusicRadar audio is excluded. Canonical music, graphs,
instrument parameters and Mixer match the protected private source; only resource metadata/locators
and replacement sound names change. One main project plus publication-01 and a restoration script
support future creative development and cross-version comparison without duplicating media.

Independent Temp copies open through native production GUI, all 15 Patterns and 12 distinct
instrument/source editors are inspected, and full Song plays on UR12 at 44.1 kHz with real meters.
Native WAV is 48 kHz stereo PCM16, 3,291,429 frames / 68.5714375 s, peak .8588256836, zero saturated
frames. No acoustic or other-platform acceptance is claimed. The focused isolated Release build
has zero warnings/errors; **33/33 demo/metadata cases pass**, including both relocated current and
frozen projects, every Pattern, media integrity, Save As/reopen and full Song export. This is L0/L1/L2
plus requested native L4 evidence, not a full regression
pass. [Test execution](TEST_EXECUTION.md#official-demo-verification)
owns reproducible commands; [the musical checkpoint](../examples/Neon%20Orchard/checkpoints/README.md)
owns the reference render conditions.

The latest L3 remains the Mixer full Release solution build and **1196/1196 tests**, zero skips (9.123 s CLI summary).
Its native presentation/history and physical PCM parity evidence remains
scoped in [the Mixer report](experiments/SEQ-R11-MIXER_REPORT.md).

### Verification cadence

Last successful L3: **SEQ-R11-MIXER, 2026-10-14**, full Release build and 1196/1196 tests.
Completed repository tasks since that L3: **1**. Official-demo focused checks and native review
complement this baseline; they do not imply another full-suite run.

## Current focus and remaining gaps

Continue the owner-authorized coherent, reference-led compact instrument language across Piano/
Graph/Drum/source editors, preserving meaningful curves and musical interaction. Concrete follow-ups
are in [desktop design continuation](ROADMAP.md#desktop-design-continuation). Private design and WAV
references and the general sample library remain outside Git; only the reviewed official-demo subset
is distributed. No third-party bitmap/icon/texture or new dependency was introduced.

Mixer's processing order is fixed; delay/reverb, arbitrary FX racks, sidechain, automation, true-peak/
LUFS analysis and latency compensation are not implemented. Source projection covers actual linear
stateless graphs, not arbitrary future stateful nodes. Explicit seek/range wrap resets FX without
preroll, an actual [technical debt](TECH_DEBT.md#mixer-cold-seek-and-range-wrap). Maximum-channel
physical stress and subjective acoustic/aesthetic acceptance remain unverified.

R4-F2's earlier pressure failures remain unresolved; a passing full Song is not pressure closure.
Existing WPR evidence attributes one later failure to a long suspension handshake with Ready time
under competing Rider load, not all original starvations or a proven driver/scheduler defect.
[Known problems](KNOWN_PROBLEMS.md) and prior reports retain the actual evidence and open support/
GC/device/workload policy. F3-B owner review remains pending; no full R4 acceptance follows.

Browser/Personal Library UI, Sample Lab/extension platform, deeper Playlist/Piano functions, MIDI/
capture/recording and unnamed musical recovery remain future capabilities. Linux/macOS runtime,
high/mixed DPI, cross-monitor, screen readers and broad platform support remain unevidenced.
[Portability](PORTABILITY.md) owns claim limits; independent device clocks/backend/ABI, recovery,
async integrity and extension/state questions remain with their canonical owners.
