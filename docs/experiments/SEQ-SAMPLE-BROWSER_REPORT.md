# SEQ-SAMPLE-BROWSER evidence

Role: Bounded Browser implementation and verification report.
Read when: Reviewing the Browser checkpoint, reproducing its checks or assessing evidence limits.
Authoritative for: Observed verification and failures at this checkpoint.
Not authoritative for: Product/resource contracts, future library scope or owner acceptance.

## Implemented boundary

The retained Browser adds local-folder navigation/pinned/recent access, current-folder name search,
project-resource discovery, selected-source metadata and real waveform, stopped-transport one-shot
preview and explicit Rack/Playlist use/drop. It composes existing resource, normalization, sampler,
history and Save owners. No library database, global scan, new project model or contextual preview.
[Sample workflow](../SAMPLE_WORKFLOW.md#implemented-seq-sample-browser), [workspace](../WORKSPACE.md#retained-sample-browser)
and [architecture](../ARCHITECTURE.md#implemented-browser-composition) own current contracts.

## Deterministic and build evidence

Windows x64, .NET SDK 10.0.401, Release, locked isolated outputs under `.artifacts/sample-browser/build`.
Affected build: zero warnings/errors. Focused run: **246/246 pass**, zero skips, **4.637 seconds** CLI
summary. Final expanded affected verification passes **318/318** in **17.850 seconds**, adding Node
Editor and product metadata boundaries. This includes 16 new Browser cases plus WAV audition/reuse/source, Desktop workflow, Audio
Clip, Rack, workspace and design guards. Output substitution is explicit: these tests use the real
preparation/renderer/document/managed-media implementation with a deterministic output borrower.
They do not claim physical device or native pointer evidence.

An initial full run found the ninth-pane expectation in NodeEditorTests and a transient protection
result in existing ArtifactRetentionTests; the first was corrected to the new capability count.
The isolated retention class then passed 14/14. A full one-thread retry passes **1270/1270**, zero
skips, **59.254 seconds** CLI summary. Final small footer/shutdown refinements receive affected
rebuild/focused tests and actual-window rechecks; the L3 is not a native or pressure acceptance claim.

## Actual-window and routed interaction evidence

VisualCheck `browser` uses an isolated real Desktop window and user configuration. It browses original
synthetic Kick/Snare/Hat WAVs, checks a real min/max envelope, adds three empty channels, writes eight
canonical notes, reuses managed Kick in one independent Audio Clip and saves/reopens after removing
all external top-level WAV originals. It checks actual Browser-source Rack and managed-resource
Playlist routed drops, their Undo and unchanged resource ownership. No user project or demo is edited.

RU/EN × Dark/Light FullHD renders, 1400×900 reduction and a 640×480 local-scrolling Browser pass;
folder Enter/up, local search and recovery after corrupt-source selection also pass routed checks.
The keyboard check first exposed the ListBox's default focus refusal; explicit focusability repairs
the production Browser and the same routed Enter case now passes. Inspected images show reachable footer
actions, real waveform, fixed compact Browser and simultaneous Rack/Playlist. Browser uses shared
tokens and existing field/control templates; no shared style change or new exact parameter row.
Long names use ellipsis/full-path tooltips; navigator yields width to the left Browser dock and the
existing occurrence selector returns. These are actual rendered/routed observations, not native input.

## Physical codec/preview evidence

VisualCheck `browser ... audio <codec-directory>` exercises the production output through Samsung
USB C Earphones at **48 kHz stereo**. WAV preview starts through the actual routed Browser Play action.
Separate authored one-second sine fixtures are encoded locally to MP3/FLAC with installed Python
soundfile; these generated fixtures are ignored evidence, not shipped runtime assets/dependencies.
The application's Windows Media Foundation decoder, not Python, reads both compressed sources.

MP3 yields **49,008 mono frames at 48 kHz**, including its codec padding; FLAC yields **48,000 mono
frames at 48 kHz**. Both produce real nonzero waveform, reach physical preview execution, import to
managed Playlist audio and Undo successfully. Normalization/previews leave the original project
resource count intact. No acoustic judgement, callback-allocation/timing measurement, stress/pressure
acceptance, other Windows codec installation or Linux/macOS runtime support is inferred.

## Native input attempt and limits

The separately invoked `browser ... native` attempt failed before any input: the exact native title
point belonged to another foreground window, so `ActivateVisibleWindow` refused activation/clicks.
No user application was closed, minimized or clicked through. Thus **native Browser mouse/keyboard,
folder-picker and OS drag/drop acceptance remain unestablished**. Routed controls and drop events are
reported separately; they are not a substitute claim for that failed attempt. High/mixed DPI,
cross-monitor/accessibility and owner aesthetic/acoustic acceptance also remain separate.

## Retained artifacts and checkpoint

Ignored `.artifacts/sample-browser/` retains the isolated build, latest successful rendered/physical
runs and small generated codec inputs for review/reproduction. The native refusal is retained in
this report; its raw run rotated under the existing retention policy. The existing retention
policy governs run evidence; no screenshots/binaries/media outputs are added to Git. Source, tests and
the reusable VisualCheck route are permanent repository changes. The official demo is unchanged.
Product classification is one BUILD **0.1.7.0003 → 0.1.7.0004**: implemented Browser integration with
bounded rendered/deterministic/physical evidence; native interaction acceptance remains open, so no
PATCH/native milestone acceptance is asserted. No staging, commit, push, tag or release occurred.
