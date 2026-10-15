# SEQ-CROSS-STAGE-AUDIT

Role: Cross-stage regression and remaining-risk assessment.
Read when: Assessing the interaction of the Drum/Mixer/demo/Audio Clip/section/replacement/daily/identity/Browser checkpoints.
Authoritative for: Audit observations, reproduced defects, verification scope and follow-up priorities.
Not authoritative for: Product contracts, roadmap acceptance, a release or general audio/platform reliability.

## Basis and judgement

Audited on Windows x64, 2026-10-15, SDK 10.0.401/runtime 10.0.12. Starting HEAD is
`45e873bcf6adbf598e4bfda94e8b74e4b93ed79f`, published **0.1.7.0004**; the working tree was clean.
Read current owners and affected source/tests, then the retained stage reports and relevant historical
TRX/work-log evidence. No external repository research, new capability, backend or architecture rewrite.

The audit finds real integration defects, chiefly in playback equivalence and input/lifecycle handling.
It finds no demonstrated loss of accepted music, cross-document resource acceptance or unjoined output
after successful document replacement/close in the inspected and exercised paths. That bounded result
does not settle crash/power-loss recovery, maximum-memory behaviour or sustained physical output.
The highest remaining reliability risk is still real WASAPI starvation under pressure. Test isolation
and long-source Browser latency also remain open; a serial green suite does not close either one.

## Findings and priority

| Priority / suspicion | Classification | User impact and disposition |
|---|---|---|
| P1 / 1: section organization | **Confirmed defect, fixed** | Renaming/recoloring a label could restart Song and voices. Section-only edits now publish equivalence without PCM preparation; publication comparison also ignores sections. Add/rename/color/delete and Undo/Redo preserve time/epoch; actual section swaps still change the schedule. |
| P1 / related Song Mixer path | **Confirmed defect, fixed** | Song uses a combined graph with the default attachment ID, but coordinator ownership was compared with the selected Pattern attachment. This bypassed fast Mixer control publication and caused full preparation. Ownership now follows execution mode; production-coordinator tests protect the fast path. |
| P2 / 2: Browser F6 | **Confirmed shortcut conflict, fixed** | Window Tunnel consumed F6 before Browser. F6/Shift+F6 retain pane navigation; Browser refresh is Ctrl+R, shown in its tooltip and UX owner. Both native and routed checks pass. |
| P1 / 3: playing Pattern double click | **Confirmed input defect, fixed** | First Click starts async selection; Busy disabled the card and refresh replaced its child visuals. The clicked card/visual target now survive its own selection, and editor intent waits for that selection with lifecycle/generation validation. Other busy cards remain gated. Held-preparation native and routed checks cover second-click delivery and Panic; deterministic workflow tests cover selected-target PCM, same-output restart and Stop/Panic. |
| P2 / 4: drag source | **Source-boundary defect fixed; ordinary wrong-row failure not established** | ListBox can update selection before motion, but the old handler captured any ListBox press and later borrowed `_selected`, without requiring that row to be the gesture origin. Drag now captures the actual file row on press; directories/whitespace cannot start sample transfer. Post-fix real mouse motion from unselected Snare with Kick selected imports Snare; whitespace imports nothing. No pre-fix native wrong-row reproduction is claimed. |
| P2 / 5: long files | **Confirmed existing performance debt; attribution gap** | Inspection followed by preview decodes the entire external source twice. Measured cost is material, but latency varies and UI stalls were not measured. Retain this as a bounded follow-up, rather than introduce a persistent media cache during the audit. See the observation below. |
| P2 / 6: failed Save before Close | **Confirmed behaviour defect, fixed; no demonstrated music loss** | Stop ran before Save succeeded. Close now withdraws pending Start intent to escape the command gate, then saves before stopping/joining current audio. Failed Save preserves document/playback/epoch/output. Browser directory shutdown also waits for successful Close, preserving its pagination cursor after failed Save. |
| P2 / 6: resources and async replacement | **No defect found in exercised boundaries; existing limits** | Commands serialize acceptance/replacement; frozen lifecycle/generation checks reject stale/ABA actions. New with active preview joins output and rejects queued old-document use. Preview leases remain valid through Undo/Redo/Save As; portable media reopens without originals. No accepted-file garbage collection/global disk quota exists. |
| P2 / 7: processed seek/range wrap | **Admissible documented limitation** | Cold IIR/compressor/protection state differs from uninterrupted streaming. New extreme-setting 44.1/48-kHz checks establish finite, protected, packet-independent output matching the same cold offline seek at every wrap. No larger regression is evidenced; no preroll subsystem added. |
| P1 / 8: WASAPI pressure | **Confirmed inherited failure, still open** | Starvation stops playback. Existing CPU/runtime evidence explains one failure, not every historical case. No fresh stress campaign or unsupported backend/GC/priority/buffer change was made. |
| P2 / 8: intermittent test failures | **Confirmed verification weakness; allocation cause unresolved** | The audit reproduces a 4,136-byte preview allocation assertion under bounded parallel tests. Exact isolated current and retained baseline tests pass. Retention failures have explicit process-snapshot-unavailable evidence. Neither is dismissed by a later green run. |
| P3 / 9: format documentation | **Confirmed stale descriptions, corrected** | Source reader is 1.5. Besides “current reader 1.4”, the owner still said compatible minimum reader `<= 3` and described 1.1 as current in the graph section. Corrected without changing format or gates. Historical feature gates 1.1/1.3/1.4 remain valid. |

## Playback and lifecycle mechanisms

`GraphExecutionCoordinator.SameExecution` removed clips/tracks but retained `ArrangementSections`.
Consequently `RealtimeSampler.Publish` could not prepare continuation mappings for label edits.
The next consumer handoff began a new epoch. Seven new pre-fix cases failed: two direct Song
publication cases, four coordinator Pattern/Song × Mixer cases and failed-Save Close playback.
All seven pass after correction. The independent PCM oracle checks continuity, not merely flags.

Section equivalence deliberately excludes only organization in the coordinator fast path. A real
swap changes clip positions and prepares scheduling. Its regression compares newly scheduled PCM
with an offline destination oracle at the retained Song clock; it does not demand an epoch reset
when compatible continuation can retain time. Existing crossing refusals remain atomic.

For Close, removing the early Stop alone would make a pending Play wait for its preparation timeout
before Close could enter the command gate. The final implementation instead revokes pending Start
intent without stopping existing playback, then runs Save and joined shutdown. The existing pending-
preparation test covers worker cancellation before borrower join. Save errors preserve the canonical
snapshot and live ownership, allowing retry; no automatic resurrection after Stop/Panic is added.

Browser/Audio Clip imports normalize to unique temporary WAVs, validate captured destination authority,
and dispose rejected requests/partial files. Project Save publishes JSON after media transfer; saved
association changes after successful publication. Accepted media remain retained for saved/history
snapshots. Preview inspection is observation only; it cannot accept music into a replacement document.
The audit adds a queued stale-use/New case and preview/history/portable-media case, and reuses existing
cancel/error/stale/Save As/borrower tests. Missing/corrupt managed media remain a diagnosed degraded
state, not an invented replacement source.

Stop/EOF can leave the single solo preview output owner retained until the next audition, Play or
document close/replacement. Stop ends PCM; it is not immediate physical-device disposal. Successful
replacement/close joins and releases that owner. An active preview lease can outlive Undo of its
descriptor safely; it does not restore the descriptor or create history. This is scoped ownership,
not proof that every idle output handle is closed immediately. Accepted media/history disk retention
and resident PCM limits remain the documented existing constraints.

## Browser cost observation

One `browser-cost` run uses authored three-minute, 48-kHz stereo sine fixtures encoded locally with
soundfile, then the **application's Windows Media Foundation decoder**. Source PCM is about 69.1 MB.
Preview uses production preparation/renderer with a substituted output borrower, not WASAPI. No
physical sound, dispatcher-progress measurement or sustained pressure is claimed.

| Source | Inspection | Subsequent preview | Managed allocations: inspection / preview | Cancelled inspection, request after 25 ms |
|---|---:|---:|---:|---:|
| MP3, 8,640,624 decoded frames | 662.6 ms | 7,679.9 ms | 71,474,952 / 70,472,440 bytes | completed in 128.0 ms |
| FLAC, 8,640,000 decoded frames | 3,969.5 ms | 279.4 ms | 70,633,992 / 69,531,808 bytes | completed in 37.2 ms |

These are single observations in source order, not percentiles or codec comparisons. Allocations are
process cumulative per operation, including owner/poll/JIT activity; they are not retained PCM size.
Process peak working set reached 328,437,760 bytes across both sources. It does not establish a leak.
The disparate times do not attribute delay to decoding rather than runtime/GC/scheduling or prove UI
thread blocking. All normalization/read/waveform work is off the UI; UI selection cancellation is
cooperative and its queue waits for worker completion. Codec construction/read can delay cancellation.

No optimization is applied here: the evidence warrants a focused latency/resource follow-up, but not
a persistent cache with new invalidation/lease policy. First measure dispatcher progress and phase/GC
times. If warranted, share PCM within a single inspection/auto-preview operation, preserve changed-
source/selection/document cancellation, and measure peak memory. Keep the 512-MiB source bound and
project acceptance validation; do not infer process memory safety from a per-source bound.

## Physical pressure and test isolation

The [R4-F2 report](SEQ-R4-F2_REPORT.md#elevated-bounded-scheduler-evidence-2026-10-09) retains actual starvation,
including a full-workload failure at 48.40 s. That failure aligns with a 67.38-ms runtime suspension
handshake; the initiator was Ready but competing with higher-priority Rider threads. Measured DSP/
service allocation/deadline counters were zero. This establishes one scheduling/runtime mechanism,
not DSP failure, every historical cause, a driver/OS defect or a selected permanent backend policy.
Later successful whole-Song physical PCM/offline comparisons establish useful musical integration
at their declared device/rate/load, not pressure acceptance. No additional laptop stress is justified
by this audit alone. Follow-up should identify IDE activity, then decide whether one unchanged bounded
idle-IDE workload would resolve the remaining hypothesis.

The audit's initial 162-case/four-thread focused run passes 161 cases but fails
`WarmPreviewProcessorAllocatesNothingAcrossVariablePackets`: **4,136 bytes**, expected zero.
The unchanged test passes alone on both current and retained Browser assemblies. Its single warm-up
packet, runner/runtime activity and lazy initialization are investigation candidates, not attributed
causes. Keep the zero requirement; a future bounded allocation trace or isolated consumer-thread
measurement should locate the allocations and compare the same code/environment/concurrency.
Do not count a serial retry as a fix, suppress the case, raise tolerance or change DSP without evidence.

The retained Song Structure TRX identifies
`ArtifactRetentionTests.IndividualDenialDoesNotStopOtherCandidatesAndPartialRunCanRetry` failing with
**“Process ownership snapshot unavailable; all runs protected.”** `ActiveReferences` launches
PowerShell/CIM with a five-second deadline; snapshot failure safely protects candidates, invalidating
the test's expectation of deletion. GUID temporary roots isolate files but do not isolate this external
process/time/load dependency. Current retention-only verification passes 14 cases in 9.427 s; it does
not demonstrate hermetic parallel behaviour. A follow-up can inject process observations into algorithm
fixtures and retain a separate real OS snapshot/failure-safety check. Preserve fail-closed deletion.

The older 5,888-byte case caused by assertions inside a measured callback loop was already corrected
at Song Structure; it is separate from the unexplained 32/4,136/6,128/7,224-byte observations. Old pane-
count/reader expectations and the ancestry fixture's invalid source deletion were also already fixed.
This audit does not reopen those resolved fixture problems.

## Verification and reproducibility

L0/L1/L2 and bounded L4 input were selected. The Browser L3 remains the full-suite baseline;
this audit does not repeat the entire suite, native musical composition, acoustic judgement or pressure.
Restore used `--locked-mode`, isolated outputs and one MSBuild worker. Dependencies are unchanged.

```powershell
dotnet restore tests/Seqvium.Tests/Seqvium.Tests.csproj --locked-mode --artifacts-path .artifacts/cross-stage-audit/build
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore --artifacts-path .artifacts/cross-stage-audit/build -m:1
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --artifacts-path .artifacts/cross-stage-audit/build --filter-class '*ArrangementTests' --filter-class '*MixerTests' --filter-class '*DesktopWorkflowTests' --filter-class '*SampleBrowserTests' --max-threads 4
dotnet .artifacts/cross-stage-audit/build/bin/Seqvium.VisualCheck/release/Seqvium.VisualCheck.dll cross-stage "$PWD/.artifacts/cross-stage-audit/.artifacts/input" native --timeout-seconds=45
```

The first native launch refuses a covered window before input. Ordinary task-owned Shell shortcut
launch reaches foreground without interacting with foreign content. An initial Shell run finds the
Rack drop target occluded by its own unsettled layout; explicit side-by-side layout/settling fixes the
harness. Final Windows input passes F6/Ctrl+R, row-origin drag, whitespace refusal and held Pattern
selection/double click/Panic. Foreground and target guards remain enabled; native screen receipt passes.
The matching routed route passes independently.

For the held selection only, the harness mounts production `PatternNavigator` with a separate controlled
production workflow and an editor-intent callback, using a deterministic borrower. It proves native
gesture delivery, selection/output reuse and sticky Panic, not MainWindow's exact melodic Part choice
or physical audio packets. Existing daily routed/editor and deterministic selected-target PCM tests
provide those distinct integration boundaries. This audit sends no physical audio and makes no new
acoustic/device acceptance claim.

The initial restore takes 4.48 s wall time. Initial test compilation catches a missing test cancellation
argument; early harness compilation catches inaccessible test preparation and an event-constructor
mismatch. Those attempts select no successful tests. Subsequent zero-warning builds and the seven
expected pre-fix failures are retained separately. The initial wider run takes 50.794 s, including a
Close command waiting on Play; pending-intent withdrawal fixes that audit implementation issue. The
affected 108-case follow-up passes in 3.527 s.

Final affected Release build takes **9.20 s** MSBuild summary (9.83 s command wall), zero warnings/errors.
Final **183/183** cases pass, zero skips, **17.254 s** CLI summary, four test threads: Arrangement,
Mixer, DesktopWorkflow, SampleBrowser, ProductVersion, ManagedMedia, WavResourceReuse and SourceAccess.
This does not replace the recorded 161/162 result from the earlier wider selection containing the
unexplained allocation case. Retention-only passes 14/14 in 9.427 s; exact current/baseline allocation
checks pass individually in 1.453/1.327 s. No full suite rerun. The final native input scenario and
joined GUI shutdown pass on the final 0.1.7.0005 build (5.80 s marked run), without physical output.
Static inspection checks **865 repository-relative links/anchors** in affected docs and both reader
entry pages; ownership, decision status, unchanged public feature descriptions and Git whitespace/diff
checks pass. All code, permanent tests and reusable tool additions are retained; the temporary Shell
shortcut is removed after the final native run.

## Disposition and retained evidence

One BUILD checkpoint, **0.1.7.0004 → 0.1.7.0005**, covers bounded integration repairs and regression
protection; no new capability/PATCH, reader gate or acceptance closure. Current contracts stay in
their subsystem owners, existing Browser latency compromise in [technical debt](../TECH_DEBT.md),
and unresolved pressure/measurement evidence in [known problems](../KNOWN_PROBLEMS.md).

Ignored `.artifacts/cross-stage-audit/` retains isolated build outputs, red/focused TRX, compact codec
cost JSON, task-owned synthetic fixtures and guarded GUI receipts/captures for review. The reusable
input/cost routes and regression tests are source. Ordinary retention may rotate disposable GUI runs;
the report retains failed attempts and measurements independently of local files. No user project,
official demo/media or external application content is modified. No stage, commit, push, tag or
history/index mutation is performed.
