# SEQ-R5-MUSIC — continuous rhythm workflow

Role: Bounded implementation/verification evidence for the owner-authorized musical follow-up.
Read when: Reviewing loop/live-edit, long-pattern, deletion and selected-Pattern export evidence.
Authoritative for: Methods, observations, failures, timing and evidence limits.
Not authoritative for: Product contracts, complete R5/R4 acceptance, release or engine/backend selection.

## Outcome and scope

The existing Core scheduler, immutable document/history, WAV media, graph compiler and WASAPI adapter
support this step without new dependencies. Avalonia's retained controls and shared design resources
support the editor additions. A second engine, timing queue or document would create conflicting
ownership; none was introduced. Loop work precedes Arrangement and processors because it removes
the immediate obstacle to developing a musical idea and supplies their execution prerequisite.

Implemented: continuous selected-Pattern Play, renderer-derived position/current column, compatible
live note/row edits including Undo, 1–32 whole 4/4 bars, atomic channel/Pattern deletion with Undo,
navigation collapse and one selected occurrence's graph-processed WAV export. Canonical IDs/media,
unknown-state refusal and silent Open remain intact. No arrangement timeline, concurrent Patterns,
EQ/delay/reverb, new instrument, velocity editor or song export is delivered. See
[UX](../UX_CONTRACT.md#implemented-r5-f1-step-sequencing),
[audio](../AUDIO_ENGINE.md#r5-f2f3-continuous-pattern-playback-and-wav-export) and
[graph](../NODE_GRAPH.md#r5-f1-channel-composition) for exact contracts.

## Automated verification

Windows 11 x64; SDK 10.0.401/runtime 10.0.12. Unchanged locked restore inputs/assets were reused.
The first focused build/run passes 210 L1/L2 cases (3.300 s CLI summary), selecting LoopPlayback,
StepSequencer, GraphRealtime, GraphLifecycle, RealtimeSampler, GraphExecution, DesktopWorkflow,
Document, DesignSystem and HostPresentation classes.

Early L3 is justified by changes to the shared callback/event/publication path. The final commands are:

```text
dotnet build Seqvium.sln -c Release --no-restore -p:BaseOutputPath=C:/Users/filed/RiderProjects/Seqvium/.artifacts/seq-r5-music/release/
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore -p:BaseOutputPath=C:/Users/filed/RiderProjects/Seqvium/.artifacts/seq-r5-music/release/ --report-xunit-trx --results-directory .artifacts/seq-r5-music/full-complete
```

Final isolated build: 7.65 s, zero warnings/errors. Full run: **1009/1009**, zero failures/skips,
19.233 s CLI summary; retained TRX runner start/finish interval 18.1513080 s. This covers finite
offline/default realtime/audition, graph convergence/lifetime, persistence and built version metadata
alongside the new musical cases. A final boundary review added two regressions ensuring sub-frame
or ambiguously rounded event content retains finite execution instead of an incorrect cycle.
A user-launched Desktop (PID 18748) then locked its normal Core output; its session was preserved.
Final complete executables are retained in `.artifacts/seq-r5-music/release/Release/net10.0/`. Cadence resets to zero; this is not a release checkpoint.

New evidence includes 4000-cycle independent absolute tick/PCM oracles at 48 kHz/137 and 1000 BPM,
44.1 kHz/137.123456 BPM, many cycle boundaries inside one packet, unchanged ramp-voice age across
publication, past-step restoration on the next loop, pending supersession/capacity/Undo, sticky Stop
and zero warmed callback allocation. Workflow cases cover 2/3/32-bar gates/identities, occupied
shortening refusal, first/middle/last channel Mix repair, exact Undo, silent Save/Open, Pattern removal,
actual exported graph PCM, cancelled overwrite preservation and refusal to overwrite managed media.

Initial failures are excluded from successful evidence; retained failed-run artifacts remain local: a first export compiler
error treated ProjectDocument as IDisposable; six new Save/Open assertions used reference equality
for immutable arrays rather than the established content comparison. These were corrected. An initial
full invocation ran old Tests output after a new test's cancellation-analyzer build error, producing
seven stale-metadata failures. The analyzer issue was fixed, a successful full build completed, and
the final full run above passed. No stale output is counted as current verification.

## Actual windows and physical output

VisualCheck `steps`, `catalog` and `matrix` pass with actual production templates/resources/windows.
Steps covers RU/EN × Dark/Light at ordinary/reduced size, prior Save/Open/painting/menu/dial/BPM
actions, 128 dense pads, capacity refusal and navigation collapse/restore. Four guarded native Rack
checks passed in the pre-guard-change run: click/one Undo, focus, captured skipped-cell paint and
Escape cancellation. The final isolated `steps-complete` rerun passes rendered/routed musical checks;
its native attempt refuses on the foreground guard. Earlier native evidence applies to unchanged
input code; the refusal is not counted as a native pass. New length,
channel deletion and Pattern deletion are actual routed control actions, not native picker evidence.

An independent Rack uses the real workflow/coordinator/sampler with only its physical borrower
substituted. It renders 96 long-pattern pads, actual phase 2:1:2/current column 17, clears markers on
Stop, and verifies length/deletion/Undo and WAV export. Dark/Light live and Russian reduced images
were inspected. The catalog verifies shared resource propagation; the full field/window matrix passes.
An initial window failed on an absent ExportWave icon alias; the overflow icon was corrected and
the subsequent windows pass.

Commands (output roots are local/ignored):

```text
dotnet .artifacts/seq-r5-music/release/Release/net10.0/Seqvium.VisualCheck.dll steps .artifacts/seq-r5-music/steps-complete
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll catalog .artifacts/seq-r5-music/catalog
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll matrix .artifacts/seq-r5-music/matrix
dotnet .artifacts/seq-r5-music/release/Release/net10.0/Seqvium.DeviceCheck.dll loop-smoke
```

Steps run interval: 15.36 s; catalog process: 6.84 s; matrix interval: 19.55 s. Physical loop smoke
passes for **6.026 s**, 48 kHz stereo float32, 1056-frame negotiated capacity, MMCSS. It plays a quiet
authored sine, makes a live canonical note edit and checks continued position/unchanged epoch.
Processor/service allocated bytes, processor/packet misses and padding exhaustions are all zero;
peak amplitude 0.00625. Stop is acknowledged and output/preparation join. This tests actual WASAPI
submission; it is neither acoustic listening/DAC loopback nor the failing R4-F2 pressure protocol.
The earlier pressure failures, other-device/high-DPI/platform and native export-picker gaps remain.

## Identity and retained evidence

One BUILD advances 0.1.0.0005 → **0.1.0.0006** within the alpha capability line. R5 still lacks its
richer controls; Arrangement and processor stages remain ahead. No PATCH/full-workspace acceptance,
format/reader/writer change, dependency update, staging, commit, push, tag or release occurs.

Generated WAV fixtures/projects, PNG/JSON/TRX, failed-run evidence and the final usable build stay under the ignored
`.artifacts/seq-r5-music/` tree for review/reproduction under normal rolling retention. Earlier successful renders may rotate out;
failed runs and the final executable remain for diagnosis and owner use. Reusable
LoopPlayback tests, LoopCheck and expanded VisualCheck are permanent source. No generated product
assets or task temporary source files remain untracked. Startup Git status was clean; the final
working changes belong to this task and the staging index remains untouched.
