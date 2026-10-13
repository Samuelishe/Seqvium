# Project state

Role: Compact handoff of current repository truth for a contributor/agent without conversational memory.
Read when: Starting every nontrivial task.
Authoritative for: Current checkpoint, implemented capability, focus, validation baseline, active blockers/evidence
gaps.
Not authoritative for: Contracts, decisions, plans, history, source topology, or live Git status.

## Current checkpoint

The current product alpha is **`0.1.0.0006`**, the verified SEQ-R5-MUSIC BUILD checkpoint on the
owner-selected `0.1.0` line under
[VERSIONING](VERSIONING.md). Production assembly identity is canonical; About reads built Desktop
metadata. This identity does not close R4 acceptance or declare a production release.

**SEQ-R1, SEQ-R2 and SEQ-R3 are complete / local accepted-ready** within their bounded foundations.
R2-F1/F2/F3/F4 and R3-F1/F2 are working subdivisions, not new numbered stages. The owner accepts
R3-F1 and R3-F2's current bounded workspace usability/interaction behavior. SEQ-R3-CLOSE finds no
missing R3 prerequisite; no additional UI polishing package is required for this acceptance.
**R4 is in progress / partial; R4-F1 is complete / local accepted-ready.** R4-F2's functional execution
is implemented; full acceptance is blocked by mixed physical pressure evidence (two observed starvations).
F2 remains **partial**. **R4-F3-A is functionally implemented and conditionally accepted by the owner
for continued development. R4-F3-B is explicitly authorized and implemented, with actual canvas/
interaction acceptance pending owner review.** F3/R4 full acceptance remains incomplete.
SEQ-KB-R21 remains the planning baseline; R0 remains **partially evidenced / narrow** under its
[report](experiments/SEQ-R0_REPORT.md), without a permanent engine/backend/ABI decision.
Completed rationale is cold history; current contracts stay in canonical owners.

**R5 is in progress / partial. R5-F1 and the bounded F2/F3 musical slice are implemented:** empty
Beat creation, independent WAV channels, 1–32 bars, continuous Play, renderer position and compatible
live step edits, atomic channel/Pattern deletion, navigation collapse, selected-Pattern WAV export
and Save/Open. Velocity, simultaneous Patterns, Arrangement and richer processors remain future work.
The owner reports the visual redesign complete and now prioritizes music. Full automated, actual
window and short physical loop checks pass; long pressure/acoustic/platform acceptance stays separate.

## Implemented capability

SEQ-UI-REDESIGN implements the owner's substantial instrument direction: slate/jade identity,
layered panels, bevelled colored pads, shadows/glow, graduated Gain dial and coherent Dark/Light.
Shared HostPalette/HostMetrics/HostMaterials own colors, geometry and derived materials; original
vector rail icons and controls introduce no dependency or borrowed asset. A pristine layout opens
Rack; saved layout remains authoritative. Rail actions expose actual panes. A wide-window navigator
creates/selects real Patterns/occurrences with canonical rhythm thumbnails, without selection history.
Captured row paint/erase commits once; Escape/capture loss/stale basis cancels. Fill/rotate/clear and
single-WAV drop use the existing document/import owners. BPM commits settings once, stops playback
and remains editable from narrow overflow; musical-time content is preserved. Gain's dial shares
the existing editor draft/parameter/history. No schema/DSP/audio scheduling changes were made.
The eleven inspected references are outside Git at `%LOCALAPPDATA%/Seqvium/DesignReferences/SEQ-UI-REDESIGN`.
Current decisions are in UI_DESIGN/DESIGN_SYSTEM; validation/limits are in the
[redesign report](experiments/SEQ-UI-REDESIGN_REPORT.md). The owner reports this redesign complete.

Panes → Step Sequencer / Channel Rack now creates an empty one-bar 4/4 Pattern without changing BPM/
meter. Add WAV Channel durably accepts empty Parts/Sounds and exact placement Source bindings; three
channels feed existing binary Mix nodes → Gain → Output. Step toggles create/delete canonical notes
at exact sixteenths, with root pitch/unit intensity and finite quarter-note gates clipped to the Pattern end.
Name, notes, length and deletion use the one history; shared note edits change the same Pattern.
A derived absolute-tick LoopClock repeats in the same audio packet; compatible prepared note edits
retain position, epoch and unchanged voice ages. Export renders one frozen occurrence through its
current graph to 48 kHz stereo PCM16, preserving transport/history and reporting clipping.
Save/Open preserves IDs/media, with fresh silent runtime/history on Open. Unsupported shape/notes/
unknown data and unsafe/shared channel expansion refuse without rewriting content. Static retained
pane behavior, RU/EN, Dark/Light, local scrolling, keyboard and toggle accessibility reuse the host.
[UX](UX_CONTRACT.md#implemented-r5-f1-step-sequencing), [audio](AUDIO_ENGINE.md#r5-f1-finite-step-pattern-playback)
and [test execution](TEST_EXECUTION.md#r5-f1-step-sequencer-verification) own contracts/evidence.

Desktop now has user-wide Single Instance activation before document/audio initialization, independent
workspace deactivation and thresholded graph gestures. [Design System v2](DESIGN_SYSTEM.md) supplies
implicit standard controls, semantic template/drawing resources and verified parameter-row alignment.
The separate [VisualCheck](../tools/Seqvium.VisualCheck/README.md) host provides isolated component,
window, routed-input and process verification without a production developer surface.

The .NET 10 solution contains portable Core, one Tests project, a narrow Windows audio adapter,
an explicit physical-device harness and one Avalonia `Seqvium.Desktop` executable.
[Architecture](ARCHITECTURE.md) owns product topology.
[ProjectStats](PROJECT_STATS.md) is an independent BCL-only .NET 10 developer CLI in the solution,
with console/Markdown/JSON structural reports, explicit local output and privacy/advisory boundaries.
It adds no musical capability, device/GUI access or later-stage authorization.
The silent desktop opens one pristine unnamed
`ProjectDocument.Create` (120 BPM, 4/4); language/theme and layout persist as independent user files.
Windows custom chrome retains OS movement/resize/window actions; other desktops retain native chrome.
Optional Project Inspector and Appearance panes support activation/front order, floating, captured
movement/anchored resize, collapse/restore, reversible close/reopen and explicit left/right docking.
Expanded DIP hit targets use workspace boundary arbitration and frozen capture identity. Pane contents
remain alive; floating panes and the two docks resize independently. Keyboard traversal/actions,
Escape and safe focus return are implemented. [Workspace](WORKSPACE.md#implemented-r3-f2-internal-panes)
owns exact bounds; [Settings](SETTINGS.md#implemented-r3-f2-workspace-layout-storage) owns restoration,
corruption preservation, write failure and joined shutdown. RU/EN fallback and Dark/Light resources
apply without replacing controls or editing music. The single command row, instrument panel materials,
shared frame/pad geometry and local scrolling follow [UI design](UI_DESIGN.md) and source-owned metrics.
F3-B adds the bounded graph editor below. No general docking hierarchy, snapping, automatic audio startup or reset UI exists.

R1 provides immutable canonical documents, typed stable identities, shared multi-instrument Patterns,
pitch-aware notes, variations, independent sound/resource/organization/processing intent, validated
atomic edits, bounded Undo/Redo and precise ticks/constant-tempo conversion. Versioned JSON preserves
compatible unknown/opaque state with explicit degraded/refusal behavior.
R2 provides bounded PCM16/float32 WAV decoding, durable unnamed import, managed Save As/reopen integrity,
independent PCM leases, pitched offline/realtime execution, transport/epochs and prepared-state retirement.
External/project discovery, raw transient preview and explicit independent same-resource sound reuse
remain separate from import/Undo. Independent logical input/output intent supports default roles or
explicit opaque IDs, availability without silent fallback and confirmed join before output replacement.
Input discovery/selection opens no capture stream. [Audio](AUDIO_ENGINE.md) and
[Sample workflow](SAMPLE_WORKFLOW.md) own exact resource/execution/device contracts.

R4-F1 adds canonical item graph definitions/attachments, placement/part source bindings, version-1
Source/Gain/Mix/Output descriptors, typed ports, graph coordinates, structural versus intent diagnostics,
atomic editing/remapping/deletion and deep Undo/net-zero equality. Reader 1.1 preserves invalid/unknown
graph intent and requires minor 1 for nonempty graphs. [Node graph](NODE_GRAPH.md#implemented-r4-f1-canonical-graph-intent)
and [Project format](PROJECT_FORMAT.md#r4-f1-graph-aware-json-format) own exact contracts.
R4-F2 executes independent placement/part PCM through Source/Gain/Mix/Output, with checked preparation,
fixed scratch and shared R2 offline/realtime scheduling/leases. One owner coordinator automatically
converges after Edit/Undo/Redo/target changes, retains last-valid in-session audio on blockers, retries
capacity and preserves voices for Gain-only updates. Geometry provenance, atomic status, sticky hard
Stop/Panic, finite topology transition and preparation/device joined shutdown are implemented under
[Audio](AUDIO_ENGINE.md#r4-f2-independent-contributions-and-realtime-convergence). F3-B supplies its graph editor.

R4-F3-A connects these foundations in the actual Desktop: native durable WAV import creates an atomic
Sound/Pattern/Part/Note/Placement and exactly bound Source → Gain → Output graph. Selected Gain edits
canonical intent; explicit Play/Stop/Panic, Undo/Redo and Save/Save As/Open use existing Core APIs.
Dirty Save/Discard/Cancel, preparation/output joins and fresh silent reopen protect document/PCM lifetime.
A non-GUI host owner supplies immutable observations to the retained R3 shell/panes. One vector command
row, committed transport BPM editor/read-only meter, narrow overflow, compact status/details and owned modal follow
[permanent visual rules](UI_DESIGN.md#first-party-visual-design-rules). No bundled sample content is added.

R4-F3-B supplies one explicitly targeted retained Node Graph pane with real Source/Gain/Mix/Output,
typed cables, nearest-cable selection with deterministic crossing ties and port-specific connection
menus/selection feedback, free drag, pan/zoom/frame, context/keyboard editing and compact
committed Gain. Node preview commits once through the existing owner/Undo; stale/cancel/no-op gestures
publish nothing. A durable second WAV adds an independent Part/Note/Source binding in the same unshared
item, with shared-Pattern refusal and atomic boundary extension. Canonical topology/coordinates survive
Save/reopen; persistent element/status diagnostics distinguish invalid intent and last-valid playback.
F2 coordinator/DSP/backend remain unchanged. [Graph](NODE_GRAPH.md#implemented-r4-f3-b-first-party-editor),
[workspace](WORKSPACE.md#r4-f3-b-retained-graph-pane) and [evidence](experiments/SEQ-R4-F3-B_REPORT.md) own details.
Graph context menus open on Avalonia context requests, survive right-button release and support nested
pointer/keyboard actions. Toolbar creation opens beside its button; incomplete intent remains editable,
with per-action refusal explanations and dismissal/basis guards for retained menu actions.

## Current focus

SEQ-R5-MUSIC completes the owner-authorized continuous rhythm workflow and selected-Pattern export.
Users can develop a multi-bar beat while it plays and share a WAV. Next composition work needs
Arrangement placement, simultaneous Pattern execution and song export; EQ/delay/reverb need actual
processor/state/editor ownership. R5 still lacks richer velocity/per-channel controls. This checkpoint
does not close prior R4 pressure acceptance or declare a release.

The bounded workspace and canonical intent foundations are locally ready. F2's functional execution
passes verification, but its repeated physical pressure result is not uniformly successful.
F3-A is conditionally accepted for continued development. F3-B's implemented canvas/editor interaction
and multi-source workflow require owner inspection under [ROADMAP](ROADMAP.md).
Remaining [readiness recommendations](NODE_GRAPH.md#r4-implementation-readiness-recommendation) grant
no further authorization. R4 remains partial; F2 alone cannot deliver a workstation or select an engine ABI.
Preserve canonical intent, independent user configuration and narrow platform/device lifetimes.
Full Browser/Personal Library, expanded graph/editor/plugins/Mixer/song export/recovery and MIDI/capture/recording remain later work.

The owner accepts DESIGN-R1 for the bounded **SEQ-NODE-EDITOR-IMPL-F1** slice, now implemented:
one retained Node Editor with explicit lifecycle/attachment/definition/node identity, body double-click,
Open Editor and canvas Enter access. Gain provides one-transaction level gestures, exact amplitude,
derived dB/silence, Unity/Silence and a scalable analytical transfer graph behind Curve disclosure.
The compact composition uses one node title, short source/scope and grouped Level/value/dB. Identical
Gain parameters yield to the visible Node Editor, then Graph quick Gain, then toolbar; different
targets remain accessible. Real cancelled drafts get transient feedback, without own-commit echoes.
Minimum-size template/curve issues and unselected-node background contrast were corrected in both
themes; rendered review of draft/invalid/zero states complements state assertions. Source/Mix/Output provide
actual host information; unknown states are preserved without interpreting opaque parameters.
Drafts expire on any intervening generation, including ABA; exact deletion Undo restores the target,
whereas successful project replacement clears it even with identical saved IDs. Quick controls and
the editor use the same workflow/document/F2 boundary. Empty panes show one content hint; absent
selectors/headers/footer placeholders and empty toolbar Gain are hidden. Pristine startup leaves
the editor hidden; explicit/restored panes may have no target. Q-032 retains independent editors and
future composition. R4-F2 remains partial and F3-B remains pending owner review. Active Gain visual
acceptance remains with the owner; the earlier presentation was explicitly rejected.

## Validation baseline

SEQ-R5-MUSIC: final full Release solution build has zero warnings/errors; **1009/1009** tests pass
(19.233 s CLI test summary; isolated build 7.65 s, restore reused unchanged locked assets). Early
L3 was selected for shared realtime scheduling/continuation risk. Prior focused L1/L2 passes 210 cases.
Actual Windows steps/catalog/field matrix checks pass, including RU/EN Dark/Light ordinary/reduced
Rack, real length/deletion/navigation actions, 96 long pads, real-renderer current-column/2:1:2 position
with a substituted physical borrower. Four earlier guarded native Rack checks pass; the final native
attempt refuses on foreground, while rendered/routed checks pass. Dark/Light images were
inspected. Physical WASAPI loop smoke passes at 48 kHz stereo/1056 frames for 6.026 s: live convergence
with unchanged epoch, zero processor/service allocation, no processor/packet misses or padding
exhaustion, acknowledged Stop and joined teardown. This is short output evidence, not acoustic or
pressure acceptance. [Report](experiments/SEQ-R5-MUSIC_REPORT.md) owns commands, initial failures,
timings and retained local evidence. No dependencies, format marker or engine/backend decision changes.


SEQ-UI-REDESIGN: affected ordinary Release builds and final isolated Tests/reference builds pass
with zero warnings/errors. Final focused L1/L2 covers **435/435**, zero failures/skips, across
19 named Desktop interaction/presentation/metadata classes; after final warning-copy changes the
affected three-class subset passes **82/82**. Nine new cases protect atomic row gestures, retained
identities/Undo/ABA/refusals and exact canonical tempo editing. The full suite was not rerun.
Actual Windows production controls pass the Dark/Light component catalog, RU/EN ordinary/reduced
Rack renders and common window/field matrix. Checks include Save/Open, pane lifecycle, captured paint,
row popup, dial, tempo popup (including narrow overflow), navigator and routed WAV drop/Undo/refusal.
An authored eight-channel fixture checks 128 pads in each theme and ninth-channel refusal.
Four guarded native Rack click/focus/drag/Escape checks and eleven native graph/Workspace checks pass;
routed input is reported separately. No physical audio/device/picker/Explorer-drop acceptance follows.
Screenshots were visually inspected and copied for owner review outside rolling retention.
Normal output became locked by a user-launched Desktop during final refinement; the user session was
preserved. Complete final executables are in `.artifacts/seq-ui-redesign/release/Release/net10.0/`.
The [report](experiments/SEQ-UI-REDESIGN_REPORT.md) records actual scope, paths, failures and commands.

The current full baseline is SEQ-R5-MUSIC on Windows 11 x64, SDK 10.0.401/runtime 10.0.12.
Existing three-WAV 90/120/240 BPM finite PCM evidence remains in
[R5 verification](TEST_EXECUTION.md#r5-f1-step-sequencer-verification); prior physical pressure failures
and platform/owner acceptance remain separate below. Historical validation belongs to reports/archive.

### Verification cadence

Last successful L3: **SEQ-R5-MUSIC, 2026-10-13**, full Release solution build and 1009/1009 automated
tests (19.233 seconds CLI summary). Completed repository tasks since that L3: **0**.
Update once at task completion under [test policy](TEST_EXECUTION.md#required-verification-levels);
a focused pass does not reset it.

## Active blockers / evidence gaps

F3-B canvas/manipulation acceptance is pending owner review; tests and recorded GUI/audio smoke do not
constitute owner acceptance. The inherited nominal 640x480 client minimum produced
640x511-pixel visible Windows captures; exact 480-pixel outer-frame review remains unevidenced. No
full F3/R4 acceptance, high/mixed DPI, other-OS runtime or acoustic listening claim follows.
The cable correction reproduces and fixes draw-order selection of a farther cable. An actual Windows
window verifies routed pointer/key events, pre-Delete UI identity, saved remaining IDs, Undo topology
and port-specific menus. Its bounded native-input attempt stopped on foreground loss; native crossing
interaction and owner acceptance remain open under the [F3-B report](experiments/SEQ-R4-F3-B_REPORT.md).

F2 pressure acceptance is unresolved. The original starvations at 22.86 and 39.57 seconds remain failures;
new isolated diagnostics also fail with natural GC and graph-free legacy on the same 48 kHz stereo
High Definition Audio/22 ms-capacity path. Native runtime events align one natural Gen1 stopped-runtime
interval of 43.64 ms with a 52.03 ms audio wait and zero padding; legacy also fails near an induced compacting
Gen2 collection. A bounded elevated WPR full-workload failure at 48.40 s now confirms a 67.38 ms suspension
handshake: its initiator spends 62.45 ms Ready while higher-priority Rider threads dominate the CPUs;
the natural Gen0 collection itself takes 1.64 ms, and audio dispatch after Ready is only 8.5/11.3 µs.
This attributes that new failure, not the original two, a scheduler defect or driver fault. Callback allocation/deadline counters remain zero
at those failures; fault-stop, join and lease release work. Buffer/GC/support policy is unchanged.
Fast coefficient/provenance updates now require sampler-local prepared execution identity plus attachment;
revision collisions, rejected handoff, reprepare/ABA and sticky Stop are covered deterministically.
[R4-F2 evidence](experiments/SEQ-R4-F2_REPORT.md) preserves both outcomes and limits. Do not infer
uniform device deadline stability or full F2 acceptance from the passing runs.

No concrete prerequisite blocks bounded R3 closure. Q-053/Q-054/Q-055/Q-064/Q-067 retain broader
settings/reset/migration, contribution resources, future editor targets/native-editor focus and input/
accessibility mechanisms. High/mixed DPI, cross-monitor, screen readers and Linux/macOS runtime remain
unevidenced; Q-041 owns supported distribution targets/prerequisites. These are wider evidence gaps,
not unaccepted F2 behavior or full-platform claims. [Portability](PORTABILITY.md) owns claim limits.
Q-001–Q-007 retain engine/ABI/workload/clock/device/distribution questions; Q-026/Q-027/Q-062/Q-069
retain backends, hardware/default changes, notifications, recovery, input clocks and recording lifetime.
Q-009/Q-019/Q-029/Q-063 retain expanded format/graph/edit/async integrity; Q-028/Q-030/Q-047 retain
organization/routing/execution domains; Q-048/Q-051 audio time; Q-058/Q-059 recovery/repair/GC/storage
faults/races; Q-061 whole First Track closure. Conservative accepted-file retention can grow storage;
unnamed musical-state recovery is absent. Other questions, including Q-070, remain in
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md). No CI, global tool or release support is introduced.
