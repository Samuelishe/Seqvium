# SEQ-SYNTH-EXPRESSIVENESS-F2 — expressive subtractive Synth

This developer report records the bounded extension of the existing 64-voice instrument.
[Audio](../AUDIO_ENGINE.md#expressive-subtractive-synth-seq-synth-expressiveness-f2),
[format](../PROJECT_FORMAT.md#synth-expression-reader-115) and
[UX](../UX_CONTRACT.md#subtractive-synth-sound-design) own its contracts. Local evidence under
`.artifacts/synth-f2/` is ignored, disposable and not a distributed dependency.

## Scope and checkpoint

One BUILD, **0.1.16.0012 → 0.1.16.0013**, extends the existing polyBLEP Synth, Source editor,
immutable prepared parameters, OfflineSampler and fixed 64 voice slots. There is no new engine,
voice scheduler, stealing policy, musical event type, universal modulation matrix or automation lane.
FM, wavetable, unison, MPE/MIDI/VST and new Mixer effects remain outside this checkpoint.
Official Neon Orchard and its publication are unchanged.

Filter ADSR, sine Pitch/Cutoff LFO and Velocity brightness have actual PCM effects. Evolving Pad,
Expressive Lead and Velocity Pluck append complete parameter snapshots to the original four presets.
They do not introduce preset-name dependencies or replace existing sounds.

| Control | Musical range / interpretation |
| --- | --- |
| Filter Attack / Decay | 0–5000 ms each |
| Filter Sustain / Release | 0–100%; 0–8000 ms |
| Filter Amount | −8…+8 octaves; positive opens, negative closes |
| Velocity → filter | 0–8 octaves of reduction from full-intensity cutoff |
| LFO destination | Off, Pitch, Filter Cutoff; one active destination |
| LFO rate | 0.1–20 Hz, note-relative sine phase starting at zero |
| Pitch depth | 0–100 cents peak deviation |
| Cutoff depth | 0–4 octaves peak deviation |
| Effective Cutoff | 20–18000 Hz after base/automation and all modulations |

Amplitude ADSR alone ends voices. Filter release starts from the actual current level; live NoteOff
freezes its duration. Preview smooths depth/rate/destination changes and preserves phase/filter state.
Velocity is fixed at NoteOn and keeps the original amplitude multiplier. Soft/Medium/Strong preview
is transient and starts a fresh note only on explicit Hold with a changed velocity.

The callback uses bounded arithmetic/lookups. The existing oscillator sine calculation is unchanged;
the new LFO adds no audio-rate transcendental. Pitch uses a bounded sixth-order exponential.
Worker-derived six-harmonic discrete sums restore carrier phase on cold seek without replaying
elapsed audio; cold filter/FX preroll remains absent. Derived phase coefficients have value equality,
so an unchanged prepared note survives compatible publication. Shared table payload is 458784 bytes
per rate, 917568 process-wide for the two supported rates. Per-note expression reserves 256 extra
scratch bytes; graph and Song reserve 512 bytes per voice. Existing work/scratch ceilings remain.

## Compatibility and independent audio checks

The ten original required parameters and four factory snapshots remain unchanged. Ten optional
expression keys have neutral defaults without insertion on Open/Save. Any supplied expression key
requires Reader 1.15, including inactive/neutral intent. Underdeclared minimumReaderMinor is refused;
an accepted reader floor stays monotonic. Unknown parameters and opaque sound fields survive and
block execution/editor mutation, without preset substitution. Shared sounds, independent copies,
Undo/Redo and portable Save/Open retain their established owners.

An isolated probe used the preserved **Core 0.1.16.11 / actual Reader 1.14** binary, rather than
simulating a reader constant. It opened the neutral example, exported through the historical
PatternWaveExport and matched the new writer's entire **1,929,644-byte WAV** exactly:

`A1CA732279C8FE23BC4B21D236936B3EA3D19BAB883243774CEFE1309F6D9D85`

The same historical reader refused the F2 example with “minimum reader minor 15”. Modern tests also
reject falsely declared 1.11/1.14 expression files and retain the historical Synth-only 1.11 gate.

Numerical checks independently establish:

- Every original preset has exact neutral PCM at 44.1/48 kHz, mono/stereo and different packets.
  A separate frozen F1 phase/filter arithmetic oracle also matches every tested double sample.
- Signed filter ADSR changes normalized spectral brightness in the expected direction over time.
  NoteOff during Attack/Decay and late live ADSR edits preserve current release and amplitude lifetime.
- Crossing analysis follows ideal 4-Hz, ±50-cent vibrato within 0.8 cent, with measured peaks in
  49–51 and −51…−49 cents. Filter LFO repeats bright/dark movement; depth zero is neutral.
- Velocity changes normalized brightness by more than 3× in the selected fixture. The soft-note
  amplitude against an independently lowered static cutoff stays within 2e-6 of the old multiplier.
- A separate per-sample mathematical filter oracle composes automation's 600-Hz base, signed ADSR,
  Velocity and LFO with error below 2e-6. Modulation neither replaces nor writes the base automation.
- Realtime/offline float output and one-frame/511-frame packets match exactly. Song Mixer Delay,
  frozen R8 before Mixer, PCM16 WAV, extremes, fractional pitch, staggered velocities, loop releases,
  cold Seek and unchanged-voice publication are covered. Cold vibrato seek after 18.75 s converges
  within 2e-5 after the deliberately cold filter settles.
- Selected warmed actual callbacks allocate strictly zero; this is not attribution of older runtime
  accounting observations. Stop/Panic/retirement and stale/cancelled authority remain covered by the
  existing transport/workflow regressions and added expression cases.

Focused Synth/Automation passes **117/117**, zero skips (7.225 s CLI). Reader/product/persistence
neighbours pass **115/115** serially (17.203 s). The strengthened opaque-execution/independent-copy
case passes separately. Final full Release solution build has zero warnings/errors (2.86 s final
incremental CLI); **2119/2119 tests pass**, zero skips, `--max-threads 1` (**98.519 s CLI**).
This replaces the 2076-case L3 baseline. The initial full run's three stale reader expectations
and its parallel 130.613-s duration remain recorded; it was not a green L3.
The subsequent bounded UI refinement preserves a held note's velocity when its pitch changes
after selecting a next-note velocity; focused tests and actual GUI/audio check this binding,
reusing the unchanged DSP/Reader L3.
Its affected zero-warning build passes, followed by **148/148 focused Synth/Automation/metadata
tests**, zero skips, one thread (27.488 s CLI). Actual output verifies that next-note Velocity plus
pitch retune retains the current voice, while explicit Hold creates exactly one new note on the
resident device. This refinement adds no second version checkpoint.

## CPU measurements

Windows x64 build 26300, SDK 10.0.402/runtime 10.0.13, eight logical CPUs, workstation GC. Production
Core runs 48-kHz stereo, 480-frame packets, two seconds of priming, restarted 16-second Songs and
separate warm preparation observations. Fixture parameters are in PolyphonyFixture. Filter means
ADSR + Velocity + filter LFO; Vibrato means ADSR + Velocity + Pitch LFO. The following matrix uses
the final audio arithmetic; the subsequent prepared-value equality/accounting adjustment changes
neither its audio loop nor musical intent. JSON `Passed` denotes allocation/natural completion,
not CPU-simulation device acceptance.

| Active voices | Neutral p50 / p99, ms | Filter p50 / p99, ms | Vibrato p50 / p99, ms |
| --- | --- | --- | --- |
| 1 | .0152 / .0258 | .0295 / .0499 | .0280 / .0382 |
| 8 | .1007 / .1379 | .1976 / .2890 | .1958 / .2542 |
| 32 | .4212 / .5937 | .8495 / 1.2479 | .8368 / 1.6688 |
| 64 | .8713 / 1.4483 | 1.7477 / 3.3413 | 1.8380 / 4.0752 |

All twelve cases reach their requested peak and natural zero-voice end, with **0 measured callback
bytes**. At 64 voices CPU maxima are 1.766 / 14.5748 / 32.8148 ms for neutral/filter/vibrato.
Those scheduling/GC-sensitive outliers are not discarded. Earlier CPU observations were much worse:
neutral p99/max 35.5016/100.0441 ms and vibrato p99/max 79.2988/344.2595 ms. They prevent interpreting
an unscheduled CPU loop as a device guarantee or attributing every outlier to new modulation cost.

## Physical WASAPI

Actual endpoint: **Samsung USB C Earphones**, shared WASAPI, **48000 Hz stereo float32**, MMCSS,
device capacity 1056 frames, **actual period 100000 × 100 ns = 10 ms**. Capacity is not the period.
Each run uses 64 active voices, finite repeated notes/releases, natural completion, explicit Panic
acknowledgment and joined output. Background load uses the existing bounded ring/Gen2/control bursts.

Criteria remain p99 Process and full Service ≤5 ms, maxima ≤10 ms; strict zero callback/service bytes,
no processor/service/packet deadline misses, starvation, invalid samples or padding exhaustion.
Ordinary captures must match offline using actual device partitions. Gain-pressure captures are
explicitly not claimed as an offline PCM oracle.

| Final workload | Process p99 / max, ms | Service p99 / max, ms | Result |
| --- | --- | --- | --- |
| Filter, ordinary | 2.3384 / 3.0169 | 2.3729 / 3.0365 | pass |
| Vibrato, ordinary | 2.4150 / 2.5222 | 2.4382 / 2.5420 | pass |
| Filter, GC/control | 2.4396 / 3.0306 | 2.4660 / 3.0484 | pass |
| Vibrato, GC/control | 2.5687 / 3.6087 | 2.5908 / 3.6711 | pass |

All four have output Failure=None, peak 64, zero voices at natural Ended, stable epoch, Stop
acknowledgment and joined teardown; measured Process/Service bytes and all deadline counters are
zero. Ordinary PCM max error is exactly zero. Pressure is a bounded local pass, not general closure.

Earlier pressure on the pre-phase-refinement build **failed**: vibrato Process p99/max
37.7257/53.6853 ms, Service 37.8370/53.7813 ms, 309 Process/Service/packet misses, but zero measured
bytes and joined output. A neutral control under the same load also failed: Process p99/max
19.8705/37.0688 ms, Service 20.0119/37.2219 ms, 133 Process and 140 Service misses, ending with
Starvation. Filter passed that earlier pressure pair (Process 2.5611/4.5704 ms). No causal claim
that the arithmetic refinement fixed GC/runtime/OS scheduling follows. These failures, the rejected
128 prototype and historical R4-F2/allocation risks remain separate and open.

## Actual editor and examples

The production editor passes RU/EN × Dark/Light at 1920×1080 windows with a 760×820 pane and
900×700 windows with a **360×520 pane**, scaling 1. All 56 inspected new exact-field/unit pairs
have zero-DIP center difference. Original oscillators/ADSR remain locally scrollable, and note,
Velocity, Hold/Release/Stop stay in the pinned footer. Filter curve edits filter parameters only.
Exact negative values, invalid range/Escape, no-op/gesture Undo and cancellation pass.

Guarded native Windows exact Filter Amount **passes**. This does not establish full native
authoring of every new control. The final velocity-retune review's repeat native attempt is
foreground/occlusion-refused, with no activation input sent; it does not supersede the earlier
bounded native pass or count as a new native pass. Physical continuous preview passes Hold, exact Amount/rate edits,
Undo, frozen release and joined Stop without a new prepared voice/device. GUI interaction is bounded
independently of the CPU/device deadline measurements above. Early screenshots were occluded by
neighbouring panes; they were rejected for visual review and the editor was activated before the
final matrix. Local references informed hierarchy; no third-party graphics were copied.
The older `synth` scenario separately passes its original control/curve/exact/preset/export steps,
then fails an unrelated 720-DIP Piano toolbar single-row assertion in its workspace matrix. Piano
source/layout is unchanged by F2. This is a failed broader legacy scenario, not a claimed complete
legacy GUI pass; the new Source matrix and its independently checked compact controls pass.

Selected review captures and small native/physical outcome records are retained under
`.artifacts/synth-f2/review/`; redundant raw GUI runs are pruned after recording their findings.
The original comparison project/WAVs remain under `.artifacts/synth-f2/examples/`. Five
**10.05-second, 48000-Hz stereo PCM16** examples are exported by Seqvium PatternWaveExport:
neutral, filter envelope, vibrato, velocity brightness and filter LFO. Their authored phrase is
independent of Neon Orchard. All have 482400 frames, no clipping and distinct measured PCM.
Difference RMS from neutral is approximately .00584 / .01706 / .00942 / .00968 respectively;
peak magnitude stays below .078. These measurements establish differences and technical validity,
not subjective musical beauty. No acoustic listening or owner aesthetic acceptance is claimed.

## Corrections, evidence and remaining limits

The first full Release suite ran all 2119 cases and failed three stale test expectations: unsupported
future Reader 1.15 and two metadata cases still asserting Reader 1.14. They were updated to the new
supported boundary while retaining unsupported-reader refusal and selective legacy gates. Initial
fixture errors included a stereo graph requested as mono, a spectral window too short to average
carrier cycles, and a 500-ms release crossing a one-second 64-voice loop. Those fixtures were repaired,
not scheduler/capacity/deadline criteria. The independent oracle found and corrected a one-sample
filter NoteOff capture error. A premature test launch once locked a still-building EXE; subsequent
builds/tests are sequential. No relaxed allocation assertion or deadline was introduced.

The first GUI launch performed existing safe retention on five old diagnostic runs. The owner
subsequently confirmed that this cleanup is authorized and restoration is unnecessary. A temporary
cleanup lock during one review was released; final checks use normal retention. Automatic tool
review rejected manual recursive removal of a verified task-owned temporary GUI run with only
`blocked by policy`; no deletion occurred through that command. The safe normal retention route
succeeded and removed one redundant F2 GUI run (4651245 bytes). The remaining task evidence is
about 46 MB, including the five requested WAVs and bounded diagnostics, under ordinary retention.
No wide cleanup of unknown directories or user music/source/processes occurred.

Wider high-DPI/platform/accessibility certification, full native gesture coverage, arbitrary-duration
cold-seek filter preroll, subjective listening and general sustained GC/control acceptance remain
unevidenced. No runtime-allocation attribution, R4-F2 repair or 128-voice acceptance is claimed.

Suggested commit: `SEQ-SYNTH-EXPRESSIVENESS-F2: add filter envelope, LFO and velocity timbre`
