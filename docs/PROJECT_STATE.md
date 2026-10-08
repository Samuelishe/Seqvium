# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence
gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

**SEQ-R1 and SEQ-R2 are complete / local accepted-ready** within their bounded foundations.
R2-F1/F2/F3/F4 are accepted working subdivisions, not new numbered stages.
The [F4 complete R2 audit](experiments/SEQ-R2-F4_REPORT.md#complete-r2-requirement-audit) finds no missing
R2 prerequisite for scoping R3. **R3 is in progress / partial**: separately authorized R3-F1 implements
the accepted desktop/presentation foundation. R3-F2 implements bounded internal panes and independent
user layout persistence; local verification is complete, owner visual review of F2 remains pending.
SEQ-KB-R21 remains the planning baseline; R0 remains **partially evidenced / narrow** under its
[report](experiments/SEQ-R0_REPORT.md), without a permanent engine/backend/ABI decision.
The initial bounded C# scheduler/DSP keeps execution and device ownership replaceable.
Completed rationale is cold history; current contracts stay in canonical owners.

## Implemented capability

The .NET 10 solution contains portable Core, one Tests project, a narrow Windows audio adapter and an
explicit physical-device harness, plus one Avalonia `Seqvium.Desktop` executable.
[Architecture](ARCHITECTURE.md) owns topology. The host opens one silent main window, presents a real
pristine `ProjectDocument.Create` (120 BPM, 4/4), and persists immediately applied RU/EN and Dark/Light
preferences and workspace layout separately from music/media. Windows custom chrome has accessible actions and OS
movement/resize; other desktops retain native chrome until evidenced. No editor, file-dialog lifecycle,
audio startup or reset UI is implemented. Optional internal Project Inspector and Appearance panes support
floating, activation/front order, captured drag/resize, collapse/hide/reopen and explicit left/right docking.
[Workspace](WORKSPACE.md#implemented-r3-f2-internal-panes) owns
behavior; [Settings](SETTINGS.md#implemented-r3-f2-workspace-layout-storage)
owns the versioned bounded user file and failure safety. [F1 report](experiments/SEQ-R3-F1_REPORT.md)
records actual desktop evidence and limits. The compact 30/30/22-DIP frame has no global scrolling;
version/scope are in About only. [DAW visual target](UI_DESIGN.md#professional-daw-workspace-design-target)
owns design review; F1 is accepted,
while [F2 visual observations](experiments/SEQ-R3-F2_REPORT.md#visual-observations-and-local-review-artifacts)
support pending owner review. Its screenshots are ignored local review artifacts, not Git assets.
Neither package implements musical editors or a general docking framework.
R1 provides unnamed immutable canonical documents, typed stable IDs, shared multi-instrument Patterns,
pitch-aware notes, variations, independent sound/resource/organization/processing intent, validated edits,
bounded Undo/Redo and precise ticks/constant-tempo conversion. Versioned JSON preserves compatible unknown
and opaque state, sharing and project settings with explicit degraded/refusal behavior.

F1 adds bounded PCM16/float32 WAV decoding, mono/stereo 44.1/48 kHz, durable unnamed import, managed
Save As/reopen integrity, source independence and explicit preparation/acceptance gates.
Independent immutable PCM leases support pitched/offline voices and absolute note/release/loop execution.
F2 reuses that processor for bounded realtime packets, sticky Stop/Panic, epochs, captured-revision
preparation and active/pending/retired ownership. Windows output queries actual format/rate/channels/
period/capacity/clock and fault-stops/joins without editing music/media.
F3 adds one-level external/project discovery, transient raw one-shot preview with no import/edit/Undo,
and explicit independent sound reuse over existing ResourceId without byte copy or implicit retarget.
Detailed WASAPI diagnostics are opt-in.

F4 adds portable independent input/output session intent, default roles versus explicit opaque IDs,
immutable endpoint/status/default snapshots and availability without silent fallback.
Windows enumerates render/capture names/IDs/all states and six defaults without opening capture.
Selected output revalidates state/direction on its worker before activation; changing output joins old
ownership before processor release and deliberate fresh open. Input choice affects no output lifetime.
Endpoint selections/facts remain absent from canonical music/settings/Undo and portable JSON.
[Audio](AUDIO_ENGINE.md#r2-f4-logical-endpoints-and-independent-selection) owns exact semantics.

## Current focus

R3-F2 is implemented and locally verified within R3, not a new numbered stage or full R3 acceptance.
Review actual pane visuals/interaction; later shell/editor integration needs separate bounded authorization.
Do not infer authorization for further stages. Preserve portable canonical intent and narrow adapters. MIDI/capture
buffers, clocks,
lifetime and deliberate durable recording acceptance are planned ownership, not implemented streams.
Full catalog/contextual audition/Personal Library, graph/plugin/Mixer/export/recovery remain later work.
The ordered First Track scenario in [ROADMAP](ROADMAP.md) is unchanged.

## Validation baseline

Local Windows x64, SDK 10.0.401 / runtime 10.0.12: locked restore and full Release solution build, **zero
warnings/errors; 346 passing tests, zero failures/skips**, preserving all 308 pre-F2 cases.
34 host cases cover RU/EN fallback, complete Dark/Light roles, compact metrics/version, canonical/pending import
preservation,
preference roundtrip/corruption/write failure and pure host lifecycle without a display. Another 38 F2
cases cover pane transitions, bounded geometry/order, docking conflicts, user-file failures/roundtrip,
pending-write shutdown and independence from actual project Save/prepared WAV work.
Actual Windows 11 x64 desktop smoke verifies chrome movement/resize/minimize/maximize/restore,
Tab/Enter/Space, Alt+Space, Win+Up/Down and Alt+F4, live language/theme and focus retention at 96 DPI.
[F2 evidence](experiments/SEQ-R3-F2_REPORT.md) adds 58 actual control/pointer/keyboard/restart checks and
six inspected screenshots at 1100x750, 640x511 and maximized 1920x1040. No Windows audio adapter loads in
the desktop process; no playback/capture/DeviceCheck launch is requested.
RID-independent desktop build without apphost also passes locally; this is not hosted OS evidence.
Deterministic synthetic WAV/async-gate tests cover document/media/execution/source integrity plus 23 F4
intent/failure/join cases. No physical hardware opens during ordinary tests.
Existing [F2 workload evidence](experiments/SEQ-R2-F2_REPORT.md) and [F3 regressions](experiments/SEQ-R2-F3_REPORT.md)
remain applicable; the full 60-second F2 matrix was not rerun because F4 changes no callback/PCM processing.
[F4 evidence](experiments/SEQ-R2-F4_REPORT.md): 25 output/8 input endpoints, with Active/Disabled/Unplugged/
NotPresent observations; all six role defaults currently UR12. Three explicitly selected UR12 lifetimes
pass at 44.1 kHz stereo float32, 10 ms/970 frames, including Stop, input independence, join/release and
injected invalidation/fresh open. Each releases 1/1 prepared state; no canonical change, measured service/
packet miss or exhaustion. Three silent default-role opens and inactive/wrong-direction refusals pass.
No input stream, microphone recording, actual hardware removal, unfamiliar output playback, DAC latency,
multi-hour, hosted Linux/macOS, high/mixed-DPI, screen-reader certification, crash/power-loss or delivered-distribution
acceptance is claimed.

## Active blockers / evidence gaps

No concrete R2 prerequisite blocks separately authorized R3. Q-001–Q-007 remain open for permanent
engine/ABI and wider workload/period/device/clock/recovery/distribution evidence.
Q-062's logical selection gap is filled; hardware/default changes, notifications, automatic recovery,
input/recording lifetime remain. Q-069 retains negotiation/input clocks/synchronization and wider platforms;
Q-026/Q-027 retain ASIO/backends and capture/monitoring timing. Discovery proves no recording capability.
Q-009/Q-019/Q-029/Q-063 retain expanded format/graph/edit/async ownership; Q-028/Q-030/Q-047 retain
organization, graph/routing and broader execution domains. Q-048/Q-051 cover extraction/audio time;
Q-058/Q-059 recovery/repair/GC and wider storage/fault/race evidence; Q-061 whole First Track closure.
Conservative accepted-file retention may grow storage; unnamed musical-state recovery is absent.
Other open questions remain in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md); Q-070 remains outside this task.
Q-037 is resolved by bounded Avalonia adoption; Q-053/Q-054/Q-055/Q-064 are narrowed by shell evidence,
not closed for broader preferences, contributions, future editors or platform accessibility.
No CI, global tool or release-platform support is introduced. Desktop NuGet/native dependencies are
recorded in [THIRD_PARTY](THIRD_PARTY.md); no alternative rendering stack is added by Seqvium.
