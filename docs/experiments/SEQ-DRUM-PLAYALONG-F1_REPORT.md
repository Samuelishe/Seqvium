# SEQ-DRUM-PLAYALONG-F1 — Drum Play-along and Source Levels

Role: Implementation decisions, measurements and bounded verification evidence.
Read when: Reviewing Drum play-along, preview levels, output ownership or reproducing this checkpoint.
Authoritative for: Executed checks, numerical observations, failed attempts and evidence limits.
Not authoritative for: Product/audio contracts, subjective listening or general realtime qualification.

## Outcome and scope

Product **0.1.16.0002**, one BUILD from 0.1.16.0001. Work started on clean `master` at the expected
`4968165a35cd831ec60d35a6dac536086debb1aa`. Drum editor Play now produces overlapping source hits
over Pattern/Song through the existing RealtimeSampler and physical output. Hits do not restart the
musical clock, replace its prepared plan, create musical content/resources or enter Undo. Local Stop
and editor hide clear preview; global Stop/Panic and project replacement include it. Accepted edits
remain immediately available for the next explicit hit. Auto-audition after a parameter commit
remains stopped-transport only, so shaping over music adds no unsolicited hit.

No independent engine, competing play-along WASAPI stream, persistent gain, project field/reader
gate, dependency, new instrument, track or composition is introduced. Verification uses isolated
short musical fixtures. The toolbar numeric Pattern Gain remains unchanged. Existing shared
controls/materials/composition are retained; only Drum Play availability changes.

Current contracts belong to [Audio](../AUDIO_ENGINE.md#drum-play-along-and-source-preview-levels-seq-drum-playalong-f1),
[Architecture](../ARCHITECTURE.md#built-in-instrument-ownership) and
[UX](../UX_CONTRACT.md#drum-voice-sound-design).

## Established signal path and level policy

Source inspection confirmed both a UI availability refusal and a workflow return while musical
transport played. The old Drum solo path additionally called `SetGain(.12f)`; Synth called `.7f`.
These factors came after synthesis/Velocity and bypassed the project's graph. They did not reflect
the selected local Gain, Mixer settings or instrument headroom.

Drum preparation renders the same versioned deterministic DSP at the negotiated rate/pitch.
Velocity is a single linear multiplier. Kick includes its existing 0.8 kernel scaling; the other
family algorithms retain their 0.85 saturation scale. Synth retains its existing 0.18 DSP headroom.
Neither algorithm nor canonical sound has been made louder. A scheduled note then follows local
Source/Gain/Mix, assigned Mixer strip/buses and Master; the device receives float stereo. OS and
interface volume are external physical stages and were not changed or measured as acoustic gain.

Source preview now has unity gain for Drum/Synth, including those instruments in Piano audition.
PCM Piano/Source preview remains 0.12, Browser 0.05, and raw Lab retains its existing two factors (0.25 output and 0.05
raw audition). Browser/Piano/Synth Hold remain refused during musical playback;
Lab contextual replacement still follows the actual source graph/Mixer. These distinct workflows
are not silently converted to independent play-along.

Removing 0.12 eliminates **18.416 dB** of arbitrary Drum attenuation against unity Pattern; even the
new Pattern's default 0.25 graph gain previously sounded **6.375 dB** above that preview at equal
Velocity. Removing Synth's 0.7 eliminates **3.098 dB**. Unity does not normalize different timbres to
equal loudness. A Pattern at Gain 0.25 remains 12.041 dB below its dry source preview before any
other processing; a note at Velocity 1 differs from preview Velocity 0.8 for the same explicit reason.

Measured Classic drums, MIDI 36, Velocity 0.8, 44.1 kHz; RMS covers the full finite envelope:

| Instrument | New source/unity Pattern peak | Source RMS | Former Drum preview peak |
|------------|------------------------------:|-----------:|-------------------------:|
| Kick       |                      0.541257 |   0.158298 |                 0.064951 |
| Snare      |                      0.446467 |   0.070742 |                 0.053576 |
| Closed Hat |                      0.229264 |   0.034317 |                 0.027512 |
| Open Hat   |                      0.264094 |   0.039385 |                 0.031691 |
| Clap       |                      0.508070 |   0.044233 |                 0.060968 |
| Tom        |                      0.414502 |   0.103251 |                 0.049740 |
| Rim        |                      0.360891 |   0.062900 |                 0.043307 |

All 21 factory Drum sources are measured at 44.1/48 kHz in local `levels.json`. Core checks compare
every source's entire PCM to the existing note kernel at Velocity 0.8; workflow checks compare all
seven editors to actual unity Pattern graph output at both rates. Synth Warm Bass, MIDI 60,
Velocity 0.8, has measured scheduled/held kernel peak **0.084093** at 44.1 kHz and **0.084129** at
48 kHz. Sustain RMS over 0.75–1.00 s is **0.0448803 / 0.0448805** respectively, with held/scheduled
ratio **1.0** at both rates. Separate workflow tests retain a 0.1% settled-level bound. Held live
envelope smoothing and ordinary realtime boundary smoothing can affect attacks/edges; identical
attack PCM across these modes is not promised. These are linear/sample-peak measurements, not LUFS,
true-peak, perceptual loudness or comparative acoustic approval.

## Mixing, bounds and lifetime decisions

Preview mixes **after musical Master and its meters**, before the existing output. It is dry source
audition: local Gain/Mix multiplicity, channel/Master faders, Mute/Solo, effects and Song automation
do not process it. In particular Master Mute does not mute preview. This explicit boundary avoids
rebuilding/rerouting the current musical graph or exciting/resetting its shared effects. It never
enters offline export or R8; source/editor intent remains temporary. Mixer Delay/Reverb/taps are
tested bit-identically against uninterrupted music, including after local preview Stop.

Linked preview-only protection uses a -1 dBFS sample ceiling (0.89125094), instantaneous attenuation
and 50-ms recovery. Remaining headroom is the ceiling minus the largest absolute musical L/R sample.
Only preview is attenuated; the beat is never ducked. Solo source preview uses the same ceiling.
The guard does not change saved Master protection or repair an already clipping project. Dense
music can suppress a preview transient; this is the deliberate safety tradeoff. There is no makeup,
limiter on the project, loudness normalization, true-peak guarantee or acoustic click-free claim.

Eight fixed preview cursors and eight ring commands are separate from the existing 32 musical
slots. Ring overflow refuses the newest command without retry; full voice capacity replaces the
oldest preview tail immediately. Independent source hats do not choke scheduled hats. Native input
produced up to five overlapping preview voices with zero overflow/stealing; deterministic tests
exercise both capacity boundaries, partition invariance and protection at eight simultaneous hits.
Very dense stealing/Stop remains a hard cut, consistent with explicit bounded preview ownership.

Preparation runs on a worker, caching one source/pitch across EOF/local Stop. Velocity does not
re-render PCM. Cold/changed requests retain one active worker and only the latest pending intent;
supersession/cancellation cannot leave a delayed series of hits. Target, lifecycle, generation,
transport and serial are checked before enqueue. Source removal withdraws active/queued preview;
Undo does not resurrect it. The maximum family buffer is 2.4 s mono at 48 kHz: eight active sources,
eight queued sources, cache and candidate conservatively total **8,294,400 PCM bytes** outside the
unchanged musical plan budgets. Work is a fixed maximum of eight cursor reads/additions per frame
plus linked protection. Callback tests measure zero allocation, with no locks/I/O/disposal/document
access. Joined termination clears all queue/voice references.

If musical output exists, Drum borrows its sampler even while stopped. Otherwise the existing solo
owner opens one output; musical Play joins that solo lifetime before opening its musical owner.
No seamless transition from solo to starting transport is claimed. Local Stop/hide/retarget/removal
withdraw preview independently; global Stop/Panic, faults and New/Open/Close retain cancellation,
sticky transport and joined borrower ownership. Failed preparation cannot close the playing project.

## Executed verification

Windows x64, SDK **10.0.402**, runtime **10.0.13**, existing locked assets, no dependency/restore
change. Full Release solution build: **zero warnings/errors, 9.96 s**. Full one-thread MTP suite:
**1801/1801**, zero skips, **65.656 s runner / 65.797 s CLI summary**. Forty-one added cases cover
source/Pattern levels, overlap/partition/capacity, stale/cancelled intent, local/global Stop,
source removal/history, no Busy, musical identity and Mixer state. Existing Synth, PCM/Piano,
Browser/Lab, Song/automation, export/R8, media/portability and reader checks participate in L3.
Earlier focused new/audio/workflow checks passed 77/77 in 2.953 s. Later verifier-only follow-ups
build without warnings/errors (5.34 s GUI, 1.55 s DeviceCheck); production remains the L3 source state.

```text
dotnet build Seqvium.sln -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1 --no-progress --report-xunit-trx --results-directory .artifacts/drum-playalong/regression
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll drum-playalong <ignored-output> native --timeout-seconds=180
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll synth <ignored-output> native audio --timeout-seconds=360
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll drum-playalong <ignored-output>
```

Final native Drum route passes in **12.60 s** on **Line (Steinberg UR12), 44100 Hz stereo**.
Guarded Windows input selects actual Snare, plays eight fast hits over Pattern, shapes Tune,
replays, uses actual toolbar Undo, local Stop, global Stop, eight Song hits, Panic and editor hide.
Twenty-four timed burst observations measure **46.02–58.95 ms** request/check intervals, including
the driver's 40-ms button hold; these are not DAC/acoustic latency. The eight localized matrix hits
and two lifecycle hits are additional. No preview creates history/resources, musical epoch/plan
stays stable across hits, no second output is present and availability records **zero transitions**.
Editor hide cuts preview while Song keeps playing; explicit fixture New with an active preview
confirms joined/terminated output and zero preview voices. API setup creates only a four-hit Kick
bar and repeated test placement; geometry/theme/language/New are identified fixtures, not native
composition authoring.

RU/EN × Dark/Light actual renders cover 1920×1080 and 1000×760 windows, requested 760×740 and
360×520 pane frames, scale 1. All eight compositions were inspected; Play/Stop remains accessible,
compact parameters scroll locally and shared typography/materials are retained. Local reference
images were inspected before changing availability; no private comparative material is published.
The separate existing Synth native/audio route passes in **75.29 s**: all dials/ADSR/live drafts,
Undo/Escape, stable held voice/output, repeated Hold, Release, Stop, localized matrix and portable
Save/Open. Its physical receipt has null output failure; listening is not claimed.

DeviceCheck final physical capture contains **102,841 frames**, eight simultaneous low-velocity
preview hits plus the Pattern, peak **0.676571667**, exact **0.0** float error against the actual
partition mirror, **zero** nonfinite samples, callback/service allocations, processor/service/packet
misses and padding exhaustions. Output reports no failure and joins. This validates submitted PCM
through the UR12 output path, not acoustic output from a microphone, DAC level or human judgement.

## Failed attempts, retention and limits

Early new tests exposed fixture mistakes: processing beyond the finite OfflineSampler end, treating
float rounding as an exact real-number interval, and comparing Synth's smoothed attack samplewise.
The fixtures now respect finite spans, float precision and settled-envelope comparison. Existing
Drum tests previously used musical epoch/Playing to observe a solo transient; they now observe the
independent preview hit/voice counters while also asserting unchanged musical identity. No safety,
allocation or musical regression assertion was weakened.

The first native attempt refused a covered startup window. Ordinary window activation, as in the
existing native driver, made it available. A later attempt hit the harness's non-null reflection
helper while deliberately checking absent `_noteOutput`; the nullable reflection check was corrected.
The expanded lifecycle run refused an unsettled hit target immediately after reopening the pane.
It now uses the existing rendered-readiness click driver at that boundary. The OS foreground/hit
guards were retained; these attempts are failures, not substituted routed passes.

Final local evidence is under `.artifacts/drum-playalong/`:

- `native-lifecycle-final/20261016T195424801-b0f56e3a698644968f99533ebd5ee4f9/`: input, matrix and joined replacement.
- `native-final/20261016T195308250-4309fe24d483478cb2ac6a0386c5c4b4/`: retained target-readiness failure.
- `synth-regression/20261016T194727187-3b9ab22c92d04b58a6f7873c3b191a81/`: continuous Synth regression.
- `physical-final/` and `regression/`: level/physical receipts and full TRX.

These ignored diagnostics remain for owner review, then follow normal retention. Automatic existing
retention removed older unprotected preview runs and superseded task failure output (about 48 MB);
they are not recoverable through Git. Protected/unknown evidence and user projects/libraries/reference
material were preserved; the soft-budget warning remains. No manual broad cleanup was performed.

Subjective sound/aesthetic acceptance, acoustic latency, arbitrary clipping mixes, cold-preparation
latency bounds, maximum combined-load deadline qualification, multi-hour/other-device/platform/DPI
coverage remain open. Existing R4 pressure failures are not closed. New/Open/Close, Stop/Panic,
history/Save and R8/export keep their existing contracts. No Git staging, commit, push, tag, reset
or history/index mutation was performed. ProjectStats is refreshed separately as structural evidence.
