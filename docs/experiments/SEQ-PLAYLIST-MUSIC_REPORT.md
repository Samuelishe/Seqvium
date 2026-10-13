# Playlist music workflow evidence

Role: Evidence and limitations for SEQ-PLAYLIST-MUSIC.
Read when: Reviewing composition, transport, workspace and sample-usability verification.
Authoritative for: Observed checks, commands, failures, timing and evidence scope.
Not authoritative for: Musical/audio/workspace contracts, roadmap acceptance or release status.

## Outcome and checkpoint

BUILD advances 0.1.1.0000 to 0.1.1.0001 within the existing Song/Rack line. This adds ruler cursor/range
loop, atomic clip groups, pointer navigation and overview grips, restored main window/music layout,
usable pane frames and distinct export icons. Supported stateless Rack channel edits preserve time
and surviving voices. WAV decoding covers PCM16/24/32 and float32 at 8–192 kHz with bounded exporter
padding compatibility; Desktop/export share short inside-boundary smoothing. No project reader,
dependency, backend or stateful processor contract changes. No staging/commit/push/tag/release.

The FL/SONAR/Cubase timeline references and actual earlier Seqvium captures were inspected from the
owner's local DesignReferences. Existing vectors/palette/components remain source-owned. No borrowed
image or sample resource is distributed. Sections, independent variants, waveform editing, velocity,
Piano Roll and measured mixing remain future work.

## Automated and build evidence

SDK 10.0.401, xUnit v3 / Microsoft.Testing.Platform, Windows x64. Locked assets were reused because
no dependency/restore input changed. The user opened a normal Release session during early builds;
its files were left untouched and an ordinary build failed on that output lock (17.48 s). Complete
outputs were built independently with the command below. New compilation errors and failing new
assertions were corrected before green evidence; version changes are classified once at completion.

```text
dotnet build Seqvium.sln -c Release --no-restore -p:BaseOutputPath=<absolute task artifact directory>/release/
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore -p:BaseOutputPath=<same directory>/release/ --report-xunit-trx --results-directory <same directory>/full-tests-piano-final
```

Early L3 was chosen for shared realtime boundary/WAV decoding risk. Full Release builds have zero
warnings/errors (5.49 s initial full build; 3.80 s repaired build; later incremental builds about
1–5 s). The green full suite passes 1045/1045 (17.962 s module, 18.201 s CLI summary). Its first run
failed an outdated 96 kHz refusal input, a new source-EOF fixture whose note/Pattern ended before EOF,
and one pre-existing artifact-process snapshot timeout while GUI verification ran concurrently.
The refusal uses an actually unsupported 384 kHz file, the fixture now distinguishes source EOF from
gate end, and the independent rerun passes. No retention assertion/policy was weakened.

Focused checks pass 225 cases, then 132 cases including three new first/middle/last channel deletion
and Undo/Redo cases (3.720 s CLI summary). These extend the full baseline to 1048 distinct covered
cases; the later focused pass is not represented as another full suite. Final affected sources receive
a 205/205 pass (17.653 s CLI) for final window/frame/range/metadata sources. The ordinary
Desktop Release output was synchronized in a zero-warning/error 3.32 s build after the user session
closed. Ordinary verification remains focused under [policy](../TEST_EXECUTION.md).

Regressions cover exact destination PCM at 44.1/48 kHz, range wrap across variable packet partitions,
zero warmed allocation, sticky Stop, group spacing/clamping/Undo/stale authority, channel continuity/
silent plans and explicit preparation retry. Raw/export comparisons select the same smoothing policy.

## BPM investigation

A healthy isolated Rack performed 120 → 135 → 90 → 120 through real preparation and stayed playable.
Debugger stops in StepSequencerTests showed BPM 135/90/120, CanPlay=true and no preparation blocker.
The initial generated fixture did not reproduce the owner failure. A controlled preparation failure after
BPM produces the actual lock: at the assertion, Rider observed graph.preparation-fault and CanPlay=false.
That path prevented any explicit retry. The fix retains diagnostics but makes structurally eligible
Play available and requests fresh preparation. The repaired failure regression and actual BPM-popup
Play checks pass.

The owner then supplied Desktop/Piano001. Opening its unchanged project in the actual built window
with physical output reproduced graph.prepare-budget before audio started. Five piano channels contain
26 notes, with an independently counted peak of nine concurrent gates. Song's combined budget was 32,
but each source graph and Desktop Rack prepared only eight slots. That source preflight refused the
whole plan, even in Song; retry alone could not repair it. Graph preparation now accepts bounded 1–32
slots, and Desktop Rack, selected-Pattern export and Song source graphs request 32. Raw foundation
sampler and explicit low-level eight-slot callers retain their existing bounds/defaults. Event, decoded,
scratch and work preflight remains; 33 simultaneous voices refuse without dropping notes.

Four generated five-channel cases exercise 9/32 voices in Rack/Song across 120 → 135 → 90 → 120,
verify every summed voice in renderer/export PCM and preserve overload refusal. The final focused
voice subset passes 99/99 (3.824 s CLI). Final full Release build has zero warnings/errors (3.66 s),
and a sequential full-suite rerun passes **1052/1052** (18.435 s module, 18.668 s CLI). This supersedes
the earlier full baseline rather than representing the focused passes as full regression.

The actual Piano001 project passes six physical Play checks in a 19.374 s window run: Song original /
135 BPM / Undo-restored 120 BPM reaches its exact finite end (96000 / 85333 / 96000 frames); Pattern
at all three tempos continues looping. Observation captures nine simultaneous voices and no preparation
blocker through the unchanged Samsung USB C Earphones 48 kHz stereo output. The original snapshot is
restored with no Save; SHA256 comparison confirms the project and all five sibling WAVs are unchanged.
The ordinary Desktop Release output was synchronized again after this repair (2.80 s, zero warnings/errors).
These are short real execution checks, not an acoustic or pressure assessment.

Debugging used isolated test processes; the owner's Desktop/audio was never attached or suspended.
A code-location launch was unavailable, so the runner's --debug gate and managed attach supplied a
single filtered reproduction. All agent breakpoints were removed, sessions stopped and user exception
breakpoints left intact. Debugger output is evidence of control state, not physical output timing.

## Actual application and rendered checks

Successful manifests measure arrangement at 11.853 s, catalog at 5.803 s and fields/gestures at
15.459 s, excluding automatic artifact maintenance outside the scenario. Initial failed GUI fixture
iterations and the debugger investigation made cumulative verification exceed the ordinary 120 s
target; the final focused build/test/render/device checks fit within it. A later full suite rerun covers
the actual Piano001 voice-budget repair described above.

VisualCheck arrangement opens actual production controls/windows with isolated preferences/layout/media.
It verifies retained Playlist, group drag/duplicate/Undo, ruler seek/range, pointer zoom anchor, fit,
track controls, Song/Pattern, BPM popup Play, Save/Open and frozen Song WAV. A frame check starts at
an actual top-level hit-test element, captures the workspace boundary, resizes by 20 DIP without music
edits and restores geometry. Fresh MainWindow construction reads stored normal 1230×780 / 30,25 and
maximized state, restores panes and returns to the same normal rectangle. Rack + Playlist is visibly
non-overlapping in its adaptive composition. Ruler range fill stays under readable bar labels.

RU/EN × Dark/Light ordinary/reduced Playlist renders and real text/label centers pass. Catalog verifies
shared resource propagation and templates. The graph field/gesture route is separately adapted for the
new pristine music layout; its original free-workspace fixture assumption failed and was repaired.
Images in both themes were inspected. Synthetic routed input is labelled independently of guarded
Windows screen/native evidence. The separate field/graph route completes eleven guarded native
Graph/Workspace checks with no held input. No native Playlist gesture, picker, acoustic or mixed-DPI
acceptance is claimed.

## Real library and physical output

```text
Seqvium.DeviceCheck sample-check <explicit library> <task output>/samples-final
Seqvium.DeviceCheck song-smoke
Seqvium.VisualCheck project <task output>/piano001-complete <explicit project file>
```

The read-only library audit decodes all 271 WAVs, leaving 20 FLAC files unsupported. Initially 79 WAVs
were PCM24 and 11 used 22.05 kHz. Actual decoder checks also found 30 off-by-one outer RIFF sizes and
two terminal mono PCM24 padding bytes counted as data. Only these complete-chunk variants are allowed;
missing PCM bytes, arbitrary mismatch and unsafe format/channel/budget inputs still refuse. No source
library bytes were edited. Four owned offline fixtures quantify quarter-note gates at 120 BPM:

| Sample | Raw maximum adjacent delta | Smoothed delta | After gate |
| --- | --- | --- | --- |
| Piano A3vH | 0.14988537 | 0.010844469 | exact silence |
| Piano A4vH | 0.040120505 | 0.00546702 | exact silence |
| 808 bass-dist | 0.48646012 | 0.008202404 | exact silence |
| 808 bass-sub | 0.039103698 | 0.00039567426 | exact silence |

The seven-second physical Song check uses quiet generated sines and unchanged system volume at
48 kHz stereo / 1056 frames. Seek is observed, the range wraps, channel add/delete/Undo/Redo converge
four times, surviving transport keeps the epoch after seek and reaches finite 288000-frame end.
Processor/service allocated bytes, processor/packet misses and padding exhaustions are zero; Stop is
acknowledged and output joins. This proves bounded short output execution, not subjective click-free
listening, long pressure, all-device or other-platform acceptance. Prior R4 pressure limits remain.

## Local artifacts and scope

Current complete binaries, TRX, rendered PNG/JSON, synthetic projects/media, library audit and physical
JSON stay ignored under .artifacts/seq-playlist-music for review/reproduction. Rolling GUI retention
may remove superseded task runs; protected pre-existing trees are preserved. The canonical report
requires no local screenshot to exist in another clone. SampleCheck and window/input checks are
permanent reusable tool source; no raw reference/sample/screenshot is tracked. User work and staging
index are preserved. Final diff whitespace and 682 relative Markdown link/anchor checks pass; the index
remains empty. All changed/untracked source and report files belong to this task, with no pre-existing
user work changed. The report and canonical owners record current truth without claiming R10/R4
full acceptance.
