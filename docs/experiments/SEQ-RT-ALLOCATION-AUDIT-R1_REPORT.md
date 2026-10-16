# SEQ-RT-ALLOCATION-AUDIT-R1

Date: 2026-10-17, Windows x64. Readers: developers investigating strict realtime allocation checks.
Baseline: clean `master`, `ccaeab545d56ba29b1f60c51e780e8ac8dbba1a4`, product **0.1.16.0005**.
This report owns observations and limits; audio contracts remain in [Audio](../AUDIO_ENGINE.md).

## Result and confidence

The unchanged Mixer assertion failed with **6672 bytes** in a serial 93-case run and again under
EventPipe. A standalone console probe also produces positive deltas in an arithmetic-only window,
without DSP, xUnit or MTP, when a separate worker requests background GC. Both production audio loops
show the same class of anomaly under that intervention, on the console thread and a dedicated thread.
This substantially narrows the investigation to a runtime-sensitive measurement boundary; runner
parallelism and production DSP are not necessary to reproduce positive counter deltas.

It does **not** identify the object/stack responsible for each historical delta or prove every byte
was phantom accounting. Sampled EventPipe is insufficient for that claim. The historical failures and
the new 6672-byte event remain unattributed individually. No production allocation has been confirmed;
no DSP, ordinary test, threshold, runtime configuration or assertion is changed. This is a diagnostic
outcome, not a claimed fix. Product version stays **0.1.16.0005**: only diagnostic tooling/docs change,
with no production/test/resource checkpoint under [Versioning](../VERSIONING.md#authority-and-change-gates).

## Historical reconstruction

Reports distinguish measured bytes from fixture corrections and passing repeats:

| Evidence                                                                                         | Failure                                           | Subsequent observation / disposition                                                                                                          |
|--------------------------------------------------------------------------------------------------|---------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| [R11 Mixer](SEQ-R11-MIXER_REPORT.md#verification-and-failures), 2026-10-14                       | Preview **6128** in a full run                    | Exact isolated and next 1196-case suite pass unchanged; no origin established. Stale UI/reader expectations are separate corrected fixtures.  |
| [Cross-stage](SEQ-CROSS-STAGE-AUDIT_REPORT.md#physical-pressure-and-test-isolation), 2026-10-15  | Preview **4136**, 161/162, four threads           | Exact current and retained Browser assemblies pass alone; not attributed.                                                                     |
| [Control Input](SEQ-CONTROL-INPUT_REPORT.md#verification-method-and-observed-limits), 2026-10-15 | Mixer EQ/compressor **7576** in a 264-case subset | 19-case Mixer and later 264-case subset pass unchanged; no origin established.                                                                |
| [Automation](SEQ-AUTOMATION-F1_REPORT.md#failed-attempts-and-corrections)                        | Preview **3984**, first L3 1701/1711              | Reader fixtures/eager headless Synth construction corrected separately; unchanged allocation assertion later passes alone and in full suites. |
| [Rack Gain](SEQ-RACK-GAIN-AUDIT-R1_REPORT.md#failed-attempts-and-limits)                         | Mixer EQ/compressor **6064**, 1838/1839           | Exact repeat and next 1839-case suite pass unchanged.                                                                                         |
| [Piano/Rack](SEQ-PIANO-RACK-UI-FIX-R1_REPORT.md#verification), 2026-10-17                        | Preview **3736**, 1844/1845                       | Exact repeat and next 1845-case suite pass unchanged.                                                                                         |

An earlier SEQ-R4-F1 intermediate full run on 2026-10-09 records **5736** in unchanged Preview;
focused/final serial repeats pass with no cause established. SEQ-DAILY-WORKFLOW on 2026-10-14 records **7224** in
Arrangement live publication during a concurrent 302-case run; its isolated 23-case class
and later serial 353-case subset pass. Both are in the historical [work log](../archive/WORK_LOG.md).

The [Song Structure report](SEQ-R10-SONG-STRUCTURE_REPORT.md#failed-attempts-and-corrections) records
an older **5888-byte** defect: assertions were inside a measured callback loop. Moving them outside
fixed that specific harness defect while preserving zero. Its separate **32-byte** observation remains
unexplained. Cross-stage explicitly separates these from **7224** in an Arrangement live-publication
check. No allocation stacks are retained in those historical narratives. Later
[Key Mapping](SEQ-SAMPLER-KEYMAP-F1_REPORT.md#failures-corrections-and-limits) records **1184** in an
Automation volume-update callback. These are evidence gaps, not proof of one shared cause.
ArtifactRetention's unavailable process snapshot is a separate established external dependency,
not an explanation of current-thread bytes. No retention or starvation work is included here.

## Measurement boundaries

[Preview](../../tests/Seqvium.Tests/Audio/WavAuditionTests.cs) prepares asynchronously before measuring,
publishes, allocates its 512-float output, then warms one 256-frame packet. Two
`GC.GetAllocatedBytesForCurrentThread` calls surround exactly 200 synchronous Process calls with
alternating 254/512-float spans. There is no await, assertion, array construction or logging between
them. The assertion evaluates the second counter before entering xUnit's Assert.Equal.
The window covers frames **256–38556**, below the 48000-frame source EOF. First publication/Start
has already been consumed. This fixture does not exercise EOF/retirement after warm-up.

[Mixer](../../tests/Seqvium.Tests/Audio/MixerTests.cs) awaits fixture import, enables EQ/compression and
first compares full renders at packet sizes 1/1024. It then creates a fresh prepared arrangement and
execution with a 254-float packet. Twenty 127-frame calls warm it; 50 measured calls advance **2540–8890** frames,
within the 24000-frame PCM voice. There are no measured control publications,
new voices, note-off/EOF/tail transitions, assertions or awaits. Coefficients/scratch/voice storage
are prepared earlier. The test uses OfflineSampler.Process directly, which is also the production
RealtimeSampler's execution kernel, rather than a physical device callback.

Original tests run on the runner's synchronous continuation thread after preparation. A synchronous
method does not migrate between managed threads mid-loop; scheduling on another CPU is not a managed
thread change. The console probe records matching before/after managed IDs and native consumer IDs.
GC counts and trace markers are outside the byte-counter boundary. Equal Gen2 counts do not exclude
a BGC that started earlier and retires contexts inside the interval.

## Controlled protocol and unprofiled results

[AllocationCheck](../../tools/Seqvium.AllocationCheck/README.md) is a reproducible Core-only host outside
the solution. It preserves the audio loops/configuration and validates active voices and exact frame
advance. Preparation, reset, anchor allocations, result arrays and observations are outside measurement.
Its own measurement entry is primed before load; it omits the Mixer's separate partition-parity render.
It does not claim to be byte-identical to runner setup. JSON retains every signed delta, with no
discarded/retried window or tolerance. The arithmetic control has no authored heap allocation or DSP.

Predeclared matrix: four scenarios × two hosts × three loads × 64 windows = **1536 windows**.
`none` has no worker; `allocate` has a bounded ring/linked-object producer; `background` additionally
requests nonblocking Gen2 from that same worker. No GC is disabled, and no OS/runtime settings change.
Producer limits, cancellation/join bounds and exact CLI are in the tool guide. This is a short
attribution experiment, not realtime pressure/deadline acceptance.

Final unprofiled matrix (nonzero anomaly counts out of 64; positive expects 64 bytes, others zero):

| Scenario / host               | No load | Allocation load | Background GC | Maximum under background GC |
|-------------------------------|--------:|----------------:|--------------:|----------------------------:|
| Arithmetic / inline           |       0 |               0 |            14 |                        8136 |
| Arithmetic / dedicated thread |       0 |               0 |            14 |                        8136 |
| Positive / inline             |       0 |               0 |            15 |            8200 (64 + 8136) |
| Positive / dedicated thread   |       0 |               0 |            14 |            8200 (64 + 8136) |
| Preview / inline              |       0 |               0 |            13 |                        8136 |
| Preview / dedicated thread    |       0 |               0 |            14 |                        8136 |
| Mixer / inline                |       0 |               0 |            14 |                        7696 |
| Mixer / dedicated thread      |       0 |               0 |             8 |                         504 |

All no-load/allocation-load positive controls are exactly **64**; every background positive delta
is at least 64. Final allocation-only cells encounter no Gen2 collections in their observation spans,
so their zeros do not exclude naturally triggered BGC. An earlier exploratory allocation-only Preview
cell did produce **2648** with equal Gen2 snapshots, before the final warm-entry/position diagnostics.
The measurement spans can overlap a collection already in progress. Exploratory SpinWait controls
also reproduced; they are retained separately and not pooled into the final arithmetic matrix.
The 24-process matrix took **6.34 s** before the positive control was lengthened to cover the same
arithmetic duration; its six replacement cells took under a second of execution, plus a 0.52-s build.
No repeat-until-failure search was used.

## Profiling and attribution limits

Local `dotnet-trace` **10.0.750501** captured `gc-verbose` plus markers outside both counter reads.
Final profiled dedicated-thread/background cells: arithmetic **12/64**, Preview **12/64**, Mixer **10/64** anomalous;
maxima **8136/8136/1280** bytes. Each trace contains 64 complete marker pairs and
zero reported lost events. No allocation sample falls on the consumer thread inside any anomalous
marker window. This is sampled evidence, not an all-object count: the initial short 64-byte positive
control also had no sample in its windows. Its capture preceded the later arithmetic-duration extension.

A concrete sampled allocation stack in the Preview trace is on native load thread **2100**, object
`System.Byte[]`, **262176 bytes**, at **70.0113 ms**:

```text
Seqvium.AllocationCheck.Program+<>c__DisplayClass1_0.<Execute>b__0()
System.Threading.Thread.StartCallback()
```

That trace has **57 BackgroundGC** starts and one nonconcurrent start. These are real producer
allocations, outside the measured consumer, and are not the consumer's unexplained bytes. Setup
Single[] allocations also have explicit AudioFixture.Create stacks before the windows.
The unmodified failing 93-case runner trace has **62 BackgroundGC**, one foreground and 33
nonconcurrent starts, zero reported lost events and 6192 allocation samples. No sampled stack enters
OfflineSampler.Process/MixerDsp or the failing method, but absent samples cannot exonerate small objects.

One additional temporary-marker 93-case trace passes: Preview **0** over 442.95–451.26 ms and Mixer **0** over
1989.86–1992.08 ms. The latter has a GC event inside its outer marker span: collection
overlap alone is not sufficient to predict a nonzero counter. These markers were outside the reads;
the temporary helper and source edits were removed and tests match HEAD exactly. It is not a fix.
An early marker payload shape failed to yield start events in the reader; those captures are excluded.
Final single-Int64/two-Int64 markers have validated complete pairs.

The official
runtime [counter API](https://learn.microsoft.com/en-us/dotnet/api/system.gc.getallocatedbytesforcurrentthread)
reports managed current-thread bytes, not native allocations. Runtime
[issue 134724](https://github.com/dotnet/runtime/issues/134724) and draft
[PR 134855](https://github.com/dotnet/runtime/pull/134855) describe BGC retiring an allocation context
without subtracting its unused bytes. Local arithmetic and 64+8136 controls are consistent with that
specific mechanism. This is strong supporting runtime evidence, **not** a locally verified patched-
runtime A/B or proof that every historical event has that cause. No runtime patch/code is distributed.

Rider exposes debugger APIs but discovers no test run points for this test file. The existing Tests
configuration launches the whole executable and stops on an unrelated expected AudioEndpoint test
exception; its frame/value evidence does not establish an allocation cause. The session was stopped,
the agent breakpoint removed and user breakpoints preserved. Installed dotMemory launchers fail
before profiling (missing Platform.Core / incomplete single-exe payload). Thus no all-object managed
allocation profiler result or exact allocation stack for the positive consumer deltas is claimed.

## Verification and disposition

SDK **10.0.402**, CoreCLR **10.0.13**, net10.0, xUnit v3 mtp-v1 **3.2.2**, locked MTP **1.9.1**.
Root global.json selects MTP; no Directory.Packages.props exists. The `run-tests` skill and actual
manifests/help determined SDK-10 named-project/direct-argument filters and `--report-xunit-trx`.

| Unprofiled original-source run                  | Result                 | Reported duration |
|-------------------------------------------------|------------------------|-------------------|
| Two exact methods, one thread                   | 2/2                    | 1.018 s           |
| Four classes, four threads                      | 93/93                  | 2.164 s           |
| Four classes, one thread                        | **92/93**, Mixer +6672 | 2.924 s           |
| Full Release suite, one thread                  | 1845/1845, zero skips  | 59.495 s          |
| Exact Preview / exact Mixer, separate processes | 1/1 each               | 1.053 / 1.019 s   |

The four classes are WavAuditionTests, MixerTests, GraphRealtimeTests and ArrangementTests.
The additional EventPipe run of these unchanged 93 tests fails the same Mixer assertion with +6672,
2.537 s runner duration. It is profiled evidence, outside the unprofiled table. No red run is replaced
by a green repeat. Full suite execution preceded the later red subset; the suite pass does not close
the failure. Affected Release build **19.46 s**, zero warnings/errors; full solution build **1.10 s**,
and restored-original source build **1.93 s**, zero warnings/errors. Tool builds are separate (about
0.5 s); package-free tool restore creates its lock, then locked restore is verified. No production
dependency changes. Full checks remain valid for unchanged production/tests; no GUI/device work applies.

Confirmed exclusions are narrow: xUnit/MTP, test parallelism, DSP, Thread.SpinWait and thread migration
are not necessary for the standalone arithmetic reproduction; assertions inside the two selected
loops are absent; those windows do not cross voice EOF. A dedicated thread alone does not prevent it.
No blanket exclusion of JIT, first-use runtime work, other audio paths or real tiny callback objects
is justified. Native allocations and realtime deadlines are outside this counter's contract.

The next discriminating step is an **all-object** allocation profiler with native consumer-thread
and exact-window attribution, or an independently validated runtime with the allocation-context fix
against this same strict probe, including the 64-byte positive control. Preserve the original failing
test/byte assertion until such evidence supports a narrowly scoped measurement correction. Do not
subtract presumed context slack, skip GC-overlapped windows, settle/retry until zero, or increase warm-up
as a claimed fix. The reproducible arithmetic intervention makes that next step bounded and testable.

Raw JSON, red/green TRX, EventPipe/ETLX and tool installation are retained locally under ignored
`.artifacts/rt-allocation-r1/`; local analysis source/dependency lookup stays in
`.scratch/rt-allocation-r1/`. They support follow-up but are not clone prerequisites. Permanent probe
source/guide/lock are retained in tools; temporary test instrumentation is removed. No user work,
media, system settings, index, history, remote or release state was changed.
