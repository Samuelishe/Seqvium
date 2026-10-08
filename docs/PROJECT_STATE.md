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
R2 prerequisite for scoping R3. **R3 is not started** and requires separate authorization.
SEQ-KB-R21 remains the planning baseline; R0 remains **partially evidenced / narrow** under its
[report](experiments/SEQ-R0_REPORT.md), without a permanent engine/backend/ABI decision.
The initial bounded C# scheduler/DSP keeps execution and device ownership replaceable.
Completed rationale is cold history; current contracts stay in canonical owners.

## Implemented capability

The .NET 10 solution contains portable Core, one Tests project, a narrow Windows audio adapter and an
explicit physical-device harness; no workstation/UI executable. [Architecture](ARCHITECTURE.md) owns topology.
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

R2's bounded resource/device foundation is available for separately scoped R3 shell work; do not start
R3 implicitly. Preserve portable canonical intent and narrow adapters. MIDI/capture buffers, clocks,
lifetime and deliberate durable recording acceptance are planned ownership, not implemented streams.
Full catalog/contextual audition/Personal Library, graph/plugin/Mixer/export/recovery remain later work.
The ordered First Track scenario in [ROADMAP](ROADMAP.md) is unchanged.

## Validation baseline

Local Windows x64, SDK 10.0.401 / runtime 10.0.12: locked restore and full Release solution build, **zero
warnings/errors; 274 passing tests, zero failures/skips**, preserving all 251 prior cases.
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
multi-hour, hosted Linux/macOS, GUI, crash/power-loss or delivered-distribution acceptance is claimed.

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
No CI, global tool, new dependency or release-platform support is introduced.
