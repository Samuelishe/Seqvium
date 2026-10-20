# SEQ-AUDIO-POLYPHONY-F1 — scalable musical polyphony

This developer report records the musical-capacity change, measured prototype and acceptance limits.
The [audio contract](../AUDIO_ENGINE.md#scalable-musical-polyphony-seq-audio-polyphony-f1) owns current
behavior. Raw local evidence is retained under `.artifacts/polyphony/`; those ignored files are not
distributed project media or guaranteed to exist in another clone.

## Decision and scope

The product ceiling is **64 musical voices**, increased from 32. The 128-voice prototype is rejected
for this checkpoint: ordinary physical output passes, but its GC/control pressure measurements fail
the criteria fixed before comparison. This does not establish that voice count caused the historical
runtime/scheduler failures, nor that 64 closes general pressure acceptance.

Pattern, Song, overlapping Audio Clips, WAV and R8 use the same renderer and Desktop limit. The raw
graph-free sampler remains 1–8. Sources, nodes, Mix inputs, Mixer channels, decoded PCM, scratch,
retained-state slots and project format/reader limits do not increase. No voice stealing, new engine,
scheduler, settings panel, MIDI or native DSP is added. Official Neon Orchard is unchanged.

## Implementation and independent limits

Only `GraphPreparation.MaximumVoices` selected the former 32-slot musical ceiling. Arrangement
aliases it; Desktop's default coordinator, Pattern WAV and local object preparation request it.
Raw preparation/default low-level callers and the eight-voice Drum preview are independent.
The 32 graph-node, 32 Playlist-track, 32 automation-lane and 32 Pattern-bar limits are unrelated.
Likewise 64 graph connections/Mixer channels and eight Source/Mix inputs are not voice limits.

OfflineSampler now keeps one fixed 64-bit active mask. Ascending bit scans preserve the original
slot sum order exactly; the lowest clear bit selects the same free slot as the previous scan.
Activation/deactivation covers NoteOff, Release, EOF, choke, Pattern boundaries, Seek, continuation,
Stop and disposal. NoteOff compares the prepared note index and loop iteration. Live occurrence
mapping still supplies that index; surviving oscillator/filter/age/release state stays in its slot.
The constructor guards the mask's 64-slot bound. The researched prototype used two words for 128;
that extra capacity is absent from the product.
Final review also found that a smaller explicit prepared capacity could pass new-score preflight
while continuing voices still occupied old high slots. Live shrink now refuses before State
construction/publication, preserving last-valid playback. A 64→32 regression keeps sounding notes
in slots 32–63 and proves refusal, clock/epoch continuity, intact voices and Panic. Desktop remains
fixed at 64; this guard does not change any measured fixed-capacity callback path.

Song preparation builds a canonical note lookup once per clip before expanding repeats, replacing
repeated Part/note-array scans. Capacity lifetime validation remains the original bounded overlap
calculation, including release/EOF/choke: no unsupported general allocator or heap is introduced.
Interpolation, oscillator/filter/envelope arithmetic, event precedence and note identities are unchanged.

There were **two** independent 64-event density checks: Graph preflight and realtime publication.
Both graph paths now permit **129 events/frame**: 64 Off + 64 On + a transport boundary. The
100,001-event and 67,108,864 packet-work limits remain. Work charges still include capacity times
PCM/Synth sample work, maximum packet events, graph, automation, Mixer/FX and Song reconstruction.
Tests cover full turnover, positive-release overlap, choke-heavy event floods and explicit refusal.
`PacketWork` is an admission estimate, never a CPU duration. Raw realtime density stays 64.

Arrangement now reserves 256 bytes per slot, matching the existing Synth-aware graph allowance.
Active/peak execution diagnostics and an overlap-limit/frame refusal cause are available through
the existing APIs/tool. Offline peak resets on Restart; realtime peak spans the consumer lifetime
and includes voices that ended within a packet and retired states.

## Reproduction and fixed criteria

Environment: Windows x64 build 26300, .NET SDK 10.0.402/runtime 10.0.13, workstation GC, eight logical
processors, AMD64 Family 25 Model 80 Stepping 0. No system/device/volume/process settings are changed.
The default Samsung USB C Earphones endpoint opens float32 **48-kHz stereo**, **1056-frame capacity**,
**10-ms period**, MMCSS enabled. Capacity is not the actual per-service frame count: raw observations
retain every partition, submitted position, device clock and separate Process/full-service duration.

The [DeviceCheck guide](../../tools/Seqvium.DeviceCheck/README.md#musical-polyphony) owns current CLI
syntax. Fixtures are short authored float WAVs and built-in Synth/Kick, saved once under `fixtures`
with identities/media unchanged between arms. PCM uses a bounded sustain loop with fractional pitches;
Synth includes a six-second Release; mixed/live use three independently graphed instruments and Mixer;
dense uses eight chords/second and repeats. Songs contain 16 repeats (22 seconds including long Release).
Live adds a future note; CPU also publishes Undo/Redo while old/retired states exist.

CPU packets are 480 frames at 48 kHz, stereo. Preparation has one cold observation and five warm
observations, with allocations separate from rendering. After two seconds of production callback
priming, restart and measure the entire finite Song. Each block records raw QPC elapsed ticks,
strict current-thread byte delta and voice count; hashing, publication, assertions and reporting are
outside the window. Physical observations use the actual device partitions and independently record
Process, full service, misses, allocation, completion, Panic and joined resource release.

Before final comparisons the criteria were fixed: p99 Process **and** full service at most **50%**
of the negotiated period; maxima at most one period; zero processor/service/packet misses, starvation,
nonfinite samples and callback/service bytes; natural exact-frame completion, applicable exact PCM
and joined Stop. CPU `Passed` means completion/zero allocation, not physical deadline acceptance.
Pressure replaces a bounded 4-MiB ring with 512 KiB every 5 ms, requests nonblocking Gen2 every 20
iterations, and sends control gain bursts. GC is not disabled, and no measured windows are removed.

The original 32-slot Core/Windows binaries remain in `baseline-bin`; the 128 prototype and its
source checkpoint remain in `candidate128-bin`/`candidate128-source`. Keep a baseline-compatible
0.1.16.0011 harness beside the original Core: a harness referencing assembly 0.1.16.0012 cannot be
loaded against the older assembly. That loader failure is recorded separately from render results.

```powershell
dotnet .artifacts/polyphony/baseline-bin/Seqvium.DeviceCheck.dll polyphony cpu .artifacts/polyphony/fixtures .artifacts/polyphony/baseline-cpu-final 1,8,16,32 none
dotnet .artifacts/polyphony/candidate128-bin/Seqvium.DeviceCheck.dll polyphony physical .artifacts/polyphony/fixtures .artifacts/polyphony/candidate128-physical 128 none
dotnet .artifacts/polyphony/candidate128-bin/Seqvium.DeviceCheck.dll polyphony physical .artifacts/polyphony/fixtures .artifacts/polyphony/candidate128-physical-pressure 128 background
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll polyphony cpu .artifacts/polyphony/fixtures .artifacts/polyphony/final64-cpu 1,8,16,32,64 none
```

Use new output directory names when reproducing; retain earlier observations. `environment.json`,
each scenario JSON, bounded ABBA scripts/exits, TRX and build logs retain parameters and raw results.
No downloaded/user library or external research enters the fixtures.

## CPU comparison and preparation cost

A fixed **ABBA** comparison runs old/new/new/old, with four PCM loads per process, unchanged fixtures
and two-second priming in each case (`abba-v2-*`, script `abba.ps1`, four exit codes zero). This was
selected after the first final-64 matrix had unexplained early timing excursions: PCM 1/8/16 p50
98.9/671.1/1497.8 µs, with 16-voice p99 9280.8 µs. Those raw results remain; no causal attribution or
universal latency claim follows. The first ABBA attempt also retains an older-assembly loader
failure, corrected by restoring the baseline-compatible harness before the fixed four-arm series.

ABBA block durations, **microseconds**, old capacity 32 versus final capacity 64. p50 is the mean of
the two run medians; p95/p99 are the complete ranges across both observations; max is the worst
observed block. Every callback byte delta is zero and all unchanged PCM hashes are identical.

| Active voices | Engine | p50 | p95 range | p99 range | max |
| ---: | --- | ---: | ---: | ---: | ---: |
| 1 | old 32 | 18.30 | 22.9–42.6 | 31.8–43.8 | 132.0 |
| 1 | final 64 | 9.70 | 13.6–21.9 | 15.1–35.5 | 116.4 |
| 8 | old 32 | 63.60 | 115.2–132.8 | 156.4–160.1 | 274.4 |
| 8 | final 64 | 57.45 | 130.7–133.1 | 141.7–144.0 | 337.1 |
| 16 | old 32 | 121.05 | 212.2–240.7 | 277.7–301.2 | 664.7 |
| 16 | final 64 | 114.60 | 181.4–206.8 | 249.9–267.1 | 450.2 |
| 32 | old 32 | 265.25 | 391.1–398.0 | 493.6–557.6 | 745.3 |
| 32 | final 64 | 266.40 | 407.3–431.2 | 508.4–541.8 | 763.6 |

The active-mask change reduces the observed one-voice median **47.0%**, eight-voice **9.7%** and
16-voice **5.3%**, while full 32 is effectively neutral (+0.4%). It does not accelerate Synth's DSP
itself: the full matrix's 32-Synth p50 is 385.3 → 381.9 µs and long Release 388.6 → 402.4 µs.
Removing inactive traversal is useful when most capacity is idle; it does not make 128 free.

Final full-load **64** CPU matrix (480-frame blocks, microseconds):

| Workload | p50 | p95 | p99 | max | warm preparation median |
| --- | ---: | ---: | ---: | ---: | ---: |
| PCM | 557.8 | 769.2 | 919.9 | 1131.4 | 1467.6 |
| Synth | 760.0 | 861.7 | 1013.1 | 1175.4 | 2264.7 |
| Long Release | 782.0 | 913.5 | 1087.5 | 1310.7 | 436.7 |
| Mixed/Mixer | 560.0 | 917.5 | 1067.3 | 14985.3 | 3658.4 |
| Dense events | 593.3 | 1054.3 | 1103.0 | 14448.3 | 23051.1 |
| Live edit | 560.4 | 984.5 | 1120.4 | 1395.6 | 4428.9 |

The two >10-ms synthetic maxima remain reported; these CPU simulation observations alone are not
device acceptance. Each ordinary Song renders **769920 frames**, long Release **1056000**, all
ending with zero voices and zero measured callback bytes. Density peaks at 64 events in these
fixtures; separate musical regression cases exercise **128 simultaneous Off/On** at final capacity.
The PCM/Synth packet estimates are 1013824/7895104 work units, not measured CPU time.

For the same dense-32 Song the canonical note index reduces the five-observation warm preparation
median **13680.6 → 7096.1 µs (48.1%)**. Dense-64 preparation is 23.051 ms. The original prototype's
dense-128 warm preparation median is 74.006 ms, with about 25.37 MB allocated during preparation;
no decoded library size was raised to obtain these results. Cold timings and all five individual
values/bytes are retained rather than described as callback cost.

Whole 480-frame blocks containing a main chord start are also extracted from those same raw CPU
records, not timed in a second run. With 16 full-chord starts per Song (128 for Dense), p50/max µs:

| Workload | old 32 voices | final 64 voices | prototype 128 voices |
| --- | ---: | ---: | ---: |
| PCM | 396.0 / 420.6 | 497.9 / 556.8 | 1221.3 / 3587.9 |
| Synth | 621.3 / 655.5 | 743.4 / 777.9 | 1684.4 / 2677.6 |
| Mixed | 625.8 / 671.3 | 683.5 / 1179.8 | 1989.6 / 2224.9 |
| Dense | 225.6 / 587.3 | 452.4 / 968.4 | 1899.4 / 44069.1 |

These include the entire block's DSP, not isolated event CPU instructions. The prototype's dense
excursion remains visible. `event-costs.json` retains the selection count and quantiles.

## Final 64: ordinary physical acceptance

The complete six-scenario `final64-physical` series passes on the measured 10-ms endpoint.
Durations below are microseconds; the full per-service p50/p95/p99/max and raw observations remain
in each JSON. All six peak at 64 and end naturally at their exact prepared boundary with zero voices,
zero processor/service/packet misses, no padding starvation/native fault/nonfinite output, zero
processor/service bytes and acknowledged Panic/join. Created/released states match (1/1, live 2/2).

| Workload | Process p50 | Process p99 | Process max | Service p99 | Service max |
| --- | ---: | ---: | ---: | ---: | ---: |
| PCM | 780.8 | 1340.8 | 1479.0 | 1369.5 | 1496.6 |
| Synth | 793.0 | 1274.5 | 1467.9 | 1291.6 | 1486.9 |
| Long Release | 867.5 | 1361.6 | 1462.0 | 1377.6 | 1481.9 |
| Mixed/Mixer | 634.5 | 1090.8 | 1428.6 | 1107.2 | 1440.4 |
| Dense events | 815.7 | 1285.1 | 1419.0 | 1302.3 | 1438.0 |
| Live edit | 625.8 | 1088.8 | 1834.7 | 1105.3 | 1847.8 |

Worst service p99 uses 13.8% of the measured period and worst service max 18.5%, within the fixed
criteria. The five unchanged PCM captures have **exactly zero float error** versus actual-partition
offline. Live publication preserves its epoch and releases both states, with the changing capture
kept separate from a frozen offline comparison. This is bounded ordinary physical acceptance;
pressure and wider-device reliability remain separate.

The complete final **64-voice pressure series also passes**, with the same six workloads and fixed
criteria. Adapter callback/service byte totals and deadline/packet/padding/nonfinite counters are zero;
all scenarios reach natural EOF, peak 64, zero final voices and joined disposal. Microseconds:

| Workload | Process p99 | Process max | Service p99 | Service max |
| --- | ---: | ---: | ---: | ---: |
| PCM | 1359.6 | 5315.4 | 1377.5 | 5360.3 |
| Synth | 1303.1 | 2440.2 | 1321.9 | 2460.7 |
| Long Release | 1349.4 | 2208.7 | 1365.1 | 2229.1 |
| Mixed/Mixer | 1197.4 | 2591.1 | 1225.0 | 2611.8 |
| Dense events | 1270.7 | 2011.0 | 1288.8 | 2031.6 |
| Live edit | 1165.9 | 2059.1 | 1177.7 | 2088.0 |

Four matched original **32-voice pressure controls** pass with the same authored workload/harness
protocol. Process p99/max: Synth **670.1/1497.0**, Release **740.3/1373.6**, Mixed **651.1/912.8**,
Live **734.5/5290.9 µs**; worst service 5322.4 µs, zero bytes/misses, natural Ended and joined release.
These provide a comparison with the original engine; no general R4-F2 pressure closure follows.
Physical allocation fields are the adapter's aggregate counters; CPU JSON additionally retains every
strict per-block delta. Neither is an all-object allocation trace or attribution of historical bytes.

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll polyphony physical .artifacts/polyphony/fixtures .artifacts/polyphony/final64-physical 64 none
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll polyphony physical .artifacts/polyphony/fixtures .artifacts/polyphony/final64-physical-pressure 64 background
```

## 128 prototype: numerical success does not imply acceptance

The prototype passes **55/55** dedicated numerical/lifecycle cases, including 1/8/16/32/64/96/127/128
at 44.1/48 kHz mono/stereo, exact packet/realtime parity, independent PCM/layer/slot-reuse oracles,
release/EOF, Seek/range/Panic, history/publication/retirement, Audio Clip sharing and WAV/R8. The final
product suite tests supported capacities through 64 and explicit refusal at 65/96/127/128.

Ordinary physical 128 results, microseconds:

| Workload | Process p99 | Process max | Service max | Result |
| --- | ---: | ---: | ---: | --- |
| PCM | 2769.0 | 2896.5 | 2917.2 | pass |
| Synth | 2641.8 | 2820.2 | 2857.7 | pass |
| Long Release | 2588.1 | 2898.6 | 2966.4 | pass |
| Mixed/Mixer | 2002.0 | 2521.5 | 2543.9 | pass |
| Dense events | 2532.8 | 2653.0 | 2671.4 | pass |
| Live edit | 2082.6 | 2528.2 | 2548.1 | pass |

All six reach natural EOF, peak 128, zero final voices, zero callback bytes/misses and joined disposal.
All five unchanged captures have **zero maximum float error** versus actual-partition offline;
live epoch is preserved but its changing capture is not asserted against a frozen offline plan.

The same prototype under pressure, microseconds:

| Workload | Process p99 | Process max | Processor misses | Result |
| --- | ---: | ---: | ---: | --- |
| PCM | 2775.4 | 6638.4 | 0 | pass |
| Synth | 41850.5 | 50702.5 | 245 | fail |
| Long Release | 52322.1 | 261794.2 | 426 | fail |
| Mixed/Mixer | 35689.4 | 74378.6 | 280 | fail |
| Dense events | 42070.6 | 93616.2 | 316 | fail |
| Live edit | 37832.9 | 56670.2 | 279 | fail |

Callback byte deltas remain zero, and no starvation/native failure is reported in these observations;
deadline counters are nevertheless red. Service maxima reach 261901.2 µs. These are elapsed-duration
observations, not a GC/scheduler attribution. A separately observed synthetic dense-128 run also has
52894.8-µs p99 / 80108.2-µs max; it is retained, not substituted for the ordinary physical pass.
The five failed pressure runs also remain Playing at the finite timeout rather than reaching the
prepared end. Positions are Synth **653856/769920**, Release **693024/1056000**, Mixed
**687456/769920**, Dense **638880/769920**, Live **694752/769920** frames. Dense/live/mixed still have
sounding voices at that boundary. Panic is acknowledged and every state is released after join;
this is measured failure to keep up, not a claim that indefinitely stuck voices were reproduced.

## Signal, memory and limits

All nine unchanged <=32-voice benchmark Songs retain the baseline's exact SHA256, including looped
PCM, Synth Release, mixed routing and live Undo/Redo. Independent PCM numerical checks bound error
to 2e-8; the independent lowest-slot/reuse oracle is exact. Realtime/offline and packet partition
comparisons are exact. PCM16 export/R8 differences are bounded by one quantization step (1/32768).
No tolerance was relaxed for the mask change.

At 64 slots and 480 stereo frames, the PCM Song reserves 28672 scratch bytes and decodes 32768 bytes;
Synth scratch is also 28672 with zero decoded source bytes. The PCM retained state is 61440 bytes.
Mixed Song retains about 3.17 MB, and two live prepared states about 6.34 MB, including existing
bypassed Delay storage. Candidate reservation is a conservative 736 MiB plus published states,
not an allocated PCM arena; it does not claim that much observed resident memory. Raw records retain
managed/process peak memory; the process peak is cumulative across scenarios and includes diagnostics.
Prepared states are released only after joined output/termination and control disposal.

Cumulative process peak working sets in the CPU commands are **97193984 / 117284864 / 142856192
bytes** for baseline32 / final64 / prototype128. Their matrices contain 9 / 15 / 33 scenarios, so
this is not a controlled claim of per-voice heap cost. Maximum retained live payload in those runs
is **6315008 / 6339584 / 6372352 bytes**. Final physical 64 reaches **6404096 bytes** retained and
**143245312 bytes** process peak including capture/diagnostics; pressure process peak is 132169728.
The prototype's physical normal/pressure peaks are 122220544/134533120. `memory-summary.json` and
`binary-identities.json` retain scoped values and frozen baseline/prototype/current Core hashes.

Cold processed Seek still resets filters/FX without preroll. The existing R4-F2 GC/starvation/pressure
and allocation-attribution gaps remain. One endpoint, short authored Songs and bounded pressure do
not establish every computer, maximum 64-channel FX topology, long-duration reliability, acoustic
quality, actual GUI/native input, other platforms or a delivered release.

## Verification attempts and follow-up

Final L0/L3: `dotnet build Seqvium.sln -c Release --no-restore` has **zero warnings/errors**, 10.61 s.
`dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore
--max-threads 1 --report-xunit-trx --results-directory .artifacts/polyphony/l3-ownership-followup` passes
**2076/2076**, zero skips, **91.486 s CLI**. This includes all 2026 prior cases and 50 added cases.
Final focused Polyphony/StepSequencer/ArtifactRetention: **138/138**, 16.232 s; default production
Desktop coordinator maximum Pattern/Song: **1/1**, 2.064 s separately. Earlier existing sampler/
publication/keymap coverage: **145/145**, 17.523 s. No packages/restore inputs changed; valid locked
assets were reused. Full reruns follow source/test corrections and one declared follow-up of the
strict allocation failure. The earlier pre-guard L3 is separately green, 2075/2075 in 83.551 s.
Physical/CPU 64 observations precede only the final control-side live-shrink guard: their fixed
capacity paths and Process/DSP are unchanged, so those sufficient measurements are reused.

Core/integration and deterministic Desktop workflow evidence are green. No new real-window GUI,
native mouse/keyboard or separate native-DSP suite is run. Production Windows interop is exercised
by physical WASAPI; it is not a new native engine or acoustic measurement. ProjectStats is structural
repository evidence, separate from these executed tests. Full checks include ordinary Song/Pattern,
Loop/Seek/Stop, source/media, Mixer/automation, export/R8, history, publication and lifetime neighbours.
Static checks validate all 15 added relative links across 14 changed/new Markdown documents and the
final diff. Ignored root ProjectStats Markdown/JSON are refreshed with the current executable;
metadata/corpus, zero unreadable files/directories and the two size advisories are inspected.
No unrelated size-driven refactoring or test-coverage claim follows from structural counts.

Earlier harness observations remain retained: initial physical priming/long-Release timeout were
incomplete; 400-call priming and a later isolated series were followed by the fixed two-second
comparison protocol. They do not overwrite failures or change the deadline criteria.
One intermediate harness build reports two owned-DLL copy retries while its preceding run still
holds the file; final builds are warning-free and no user process is stopped. Early tests
caught the second realtime density gate, a too-short looping Drum fixture, owner-context/no-op test
setup and an oracle's float subtraction versus the renderer's double interpolation. The oracle was
corrected; the production PCM arithmetic and exact parity assertions were unchanged.

An early related run observes **5192 bytes** in the existing Arrangement callback allocation
assertion. Later green observations do not explain it. First L3: **2069/2073**, 155.543 s, with two
obsolete 32/33 Desktop refusal expectations and two existing ArtifactRetention process-snapshot
failures. The capacity expectations are corrected and 64-voice Desktop tempo/export cases added;
the fail-closed process ownership policy is unchanged. All attempts remain in local TRX/logs.
After the final live-shrink safety guard, the 68-case focused ownership/GraphRealtime run passes
(3.769 s), but its full 2076-case run records **224 bytes** in the unchanged Mixer zero-allocation
assertion: **2075/2076**, 83.630 s. The guard is a control-side publication refusal, outside that
OfflineSampler measurement. This observation remains unattributed; no assertion or warm-up is
changed in response. The exact focused check and one bounded complete follow-up are recorded
separately; a green follow-up cannot explain the measured bytes.
The unchanged exact Mixer follow-up passes 1/1 (1.534 s), then the declared complete follow-up passes
2076/2076. Both the 224-byte observation and earlier 5192-byte observation remain open.

Next 128 work needs bounded attribution of GC/runtime suspension versus render CPU and device service,
matched baseline/64/128 pressure, actual desktop interaction and longer FX/media/state pressure.
Reassess the same criteria on more endpoints/machines before promotion. Do not suppress GC, relax
strict bytes/deadlines, change sound quality or introduce a native engine to raise the number.

The version classification is one BUILD, **0.1.16.0011 → 0.1.16.0012**, for technical scaling of the
existing musical engine. No MAJOR/MINOR/PATCH or reader/schema change, Git index/history/remote
operation, release or tag is authorized by this checkpoint.
