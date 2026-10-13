# Playlist checkpoint verification

Role: Evidence report for the bounded first Song workflow.
Read when: Reviewing Playlist capability, references, verification or its limitations.
Authoritative for: Observed commands/results/timing and retained local evidence.
Not authoritative for: Product contracts, roadmap acceptance, platform release or acoustic assessment.

## Outcome and scope

SEQ-R10-PLAYLIST implements an audible, editable finite song on the existing canonical document,
graph/PCM renderer and audio owner. Shared clips, absolute timing, repeats, overlaps, move/Ctrl-copy/
Duplicate/Delete, named/color fixed tracks with real Mute/Solo, snap, overview/fit/follow, stationary
headers, actual note duration/pitch previews, one Undo/Redo and compatible Save/Open are implemented.
Pattern retains the Rack loop. Song WAV uses the same frozen graph program, including leading gaps.
The source graphs are currently stateless linear programs; no stateful clip-processing claim follows.

The owner explicitly authorizes this finished composition slice ahead of wider roadmap completion.
It does not close R5/R10 or R4 physical-pressure acceptance. Seek/range looping, group editing,
sections/dynamic containers, explicit variants, real waveform trim/stretch, Mixer/measurement and
automation are planned in ROADMAP. WAV channel topology still stops, recorded as observed TECH_DEBT.
No dependency, engine/backend/ABI, bundled musical asset, staging or Git-history change is made.

Five owner-provided timeline images were inspected and moved into the explicitly named local folder
`%LOCALAPPDATA%/Seqvium/DesignReferences/SEQ-R10-PLAYLIST`. Older Logic/Reason/Serum/FabFilter images
were inspected too. UI_DESIGN distinguishes implemented adaptations from later capabilities and
illustration-only visual cues. Reference screenshots are not distributed assets.

## Environment and proportional verification

Windows 11 x64; SDK 10.0.401/runtime 10.0.12; unchanged locked dependencies/restore assets reused.
L0 plus L1/L2 target new song/format/document/presentation boundaries. Early L3 is justified by reader
compatibility and shared realtime continuation. L4 separately selects actual rendered/routed GUI and
a short physical Song output; no native mouse/picker, acoustic, broad stress or platform claim follows.

The owner launched normal Desktop during work (PID 13500), locking its ordinary output DLLs.
That user session was preserved; final solution/test/tool outputs use the isolated BaseOutputPath.
Ordinary copy attempts failed with MSB3026/3027; isolated builds have zero warnings/errors.
After the user session was no longer running at final inspection, the ordinary Desktop output was
synchronized with the verified source in a **7.87 s**, zero-warning/error focused build. No tests
were rerun for that output-location repair; source and dependencies were unchanged.

```text
dotnet build Seqvium.sln -c Release --no-restore -p:BaseOutputPath=C:/Users/filed/RiderProjects/Seqvium/.artifacts/seq-r10-playlist/release/
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore -p:BaseOutputPath=C:/Users/filed/RiderProjects/Seqvium/.artifacts/seq-r10-playlist/release/ --report-xunit-trx --results-directory .artifacts/seq-r10-playlist/full-complete
.artifacts/seq-r10-playlist/release/Release/net10.0/Seqvium.VisualCheck.exe arrangement C:/Users/filed/RiderProjects/Seqvium/.artifacts/seq-r10-playlist/gui
dotnet .artifacts/seq-r10-playlist/release/Release/net10.0/Seqvium.DeviceCheck.dll song-smoke
```

Final full Release solution build: **17.67 s**, zero warnings/errors. Final test-only rebuild after
correcting historical gates: **3.01 s**, zero warnings/errors. Full automated suite:
**1030/1030**, zero failures/skips, **17.466 s** CLI summary (17.241 s module).
Focused repaired passes: **55/55** (1.777 s), **30/30** feature/presentation (2.148 s),
**90/90** metadata/layout/node-editor (16.600 s). The metadata route starts real SDK processes;
its larger duration is reported separately. Full baseline is reset once at task completion.

Initial focused run: 146/152, 47.527 s. Its slow Desktop case had a substituted borrower that was not
pumping the pending consumer handoff; the harness now pumps a zero-length packet during readiness.
Other failures were stale five-pane/metric assertions and a test that expected disposal before actually
disposing the terminal sampler. The first GUI attempt found nonexistent Surface.Panel; it was corrected
to the actual shared role. Reference refinement found color content hidden by a material button template;
real palette-bound swatches now render as button content and have a brush-resolution guard.

The first full run found four historical registry/reader assertions (1026/1030); the next exposed the
remaining literal future-reader gate in that same metadata test (1028/1030). Registry census is now six,
reader is 1.2 and the unsupported gate is 1.3. The dependent 90 cases then pass; one final full run
confirms 1030 green. These failures changed verification contracts, not an audible DSP mismatch.
Green GUI/physical checks were reused until relevant resource/audio changes warranted a new run.

## Deterministic musical evidence

ArrangementTests independently calculates absolute frames and PCM for source rate/execution rate,
mono/stereo, leading/intermediate gaps, repeated/overlapping distinct Patterns, source gains and
variable packet partitions. Clip/repeat identity prevents equal notes from releasing other voices.
Mute/Solo is checked numerically, including multiple flags and silence without shortening the song.
Track IDs/settings/media round-trip; unknown clip fields survive known edits; underdeclared reader,
invalid slots/repeats/overflow and capacity/cancellation refuse without history/publication loss.

Live future placement keeps current position, voice age and epoch; stable occurrence mapping tolerates
changed source/scratch indices. Live Mute ends gated voices while preserving other voices; unmute does
not replay past onsets. Shortening behind the playhead ends without restarting. Warmed callback
allocation is zero; sticky Stop and retirement/disposal are tested. Whole-Song PCM16 WAV agrees with
realtime within quantization, preserves document/history and protects media/existing destinations.
Real Desktop Song/Pattern selection and Save/Open use the production owner/coordinator with only a
substituted physical borrower. Existing Rack, graph, document, media and lifecycle tests remain green.

## Actual window and physical evidence

Final actual-window run is under local ignored
`.artifacts/seq-r10-playlist/gui/20261013T113547452-6663b83c260846cea9bd4bd87eeb9137`.
RU/EN × Dark/Light ordinary/reduced controls render; repeat text/label center differences are zero DIP.
Routed input covers place/move/Ctrl-copy/right-edge repeat, Escape/capture/Workspace/hide cancellation,
stale draft, Duplicate/Delete/Undo, M heading, track name/color apply, snap, overview/fit, Save/Open,
retained content and graph Song export. Stationary headings remain visible after overview navigation.
Dark/Light and reduced images were inspected, including actual color swatches. A second window proves
the real 44.1 kHz renderer's 4800000 ticks / **2:2** cursor/readout. This is not native OS input.

Final physical `song-smoke` passes on 48 kHz stereo float32, **1056-frame** capacity:
two simultaneously active Pattern voices, live future-clip convergence with unchanged epoch,
finite Ended at **288000 frames**, and **5.9936409 s** output observation. Processor and service
allocation are zero; nonfinite samples, processor/packet misses and padding exhaustions are zero;
Stop is acknowledged and output joined. Maximum sample magnitude is 0.009529806. System volume and
device settings are unchanged. Receipt is `.artifacts/seq-r10-playlist/song-smoke-final.json`.
This short physical output is neither acoustic listening nor a resolution of inherited starvation.

## Checkpoint, retention and limits

PATCH advances **0.1.0.0006 → 0.1.1.0000**, resetting BUILD. The new independently usable Song editing/
simultaneous playback/export capability meets the delegated PATCH gate, with deterministic, GUI and
device evidence above. MAJOR/MINOR, stage acceptance and release/publication remain owner-controlled.
Reader 1.2 protects new song/track semantics while existing graph-only/empty files retain their older
minimums. No general migration, dynamic Track hierarchy or future DSP contract is claimed.

New untracked source/tests/harness files are permanent implementation/verification material.
Transient GUI/JSON/TRX/fixture data remains ignored under the task artifact root. Complete isolated
binaries are retained as the exact verification/tool outputs used during the user-session lock. Selected actual screenshots
and receipts are copied beside the local reference set for owner review, outside rolling retention.
Failed evidence remains distinguishable; no user source/session, product resource or reference image
is removed to make Git clean. New Playlist aesthetic acceptance, high/mixed DPI, screen readers,
Linux/macOS, native input and long pressure/acoustic evidence remain separate.
