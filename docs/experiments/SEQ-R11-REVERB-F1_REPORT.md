# SEQ-R11-REVERB-F1 — Musical Algorithmic Reverb

Date: 2026-10-16. Base: `9bcccd6`, product `0.1.11.0001` after the recent regression audit.
Completed capability: `0.1.12.0000` (PATCH; BUILD reset). This report distinguishes implemented
musical processing and bounded verification from subjective sound approval and long-pressure acceptance.
No Git stage/commit/push/tag or remote/history/index mutation is authorized or performed.

## Result and algorithm choice

The existing Mixer Inspector has a compact Reverb tab on channels, buses and Master. Instruments
and Audio Clips use the existing assignments, sends, history, preparation/publication and export.
The fixed processing order is trim → EQ → Compressor → Delay → Reverb → balance → fader → output/sends.
Pre-fader sends include both effects. R8 remains local before Mixer and includes neither.

An independently authored eight-line normalized Hadamard feedback delay network follows four
serial input allpasses on each stereo side. Distinct odd line lengths, cross-coupling, orthogonal
stereo readouts, nominal RT60 loss and per-line low-pass damping produce a dense decaying space.
This avoids presenting independent sparse Delay repetitions as a reverberator. A static FDN was
chosen over convolution (outside scope) and parallel uncoupled combs (weaker diffusion). General
mathematical context is [Smith's FDN chapter](https://www.dsprelated.com/freebooks/pasp/FDN_Reverberation.html);
no third-party implementation, dependency, graphics or audio is copied/distributed.

| Control | Range | Default | Meaning |
| --- | --- | --- | --- |
| Enabled | on/bypass | bypass | Smoothed return to dry; no transport restart |
| Decay | 0.2–8 s | 1.8 s | Nominal low-frequency RT60 |
| Size | 0–100% | 50% | 0.5–1.5 line/diffuser geometry scale |
| Damping | 0–100% | 50% | Feedback/wet low-pass cutoff 16000 down to 400 Hz |
| Mix | 0–100% | 20% | Linear dry/wet, no dry-path latency |

There is no independent Pre-delay, modulation, room-model guarantee, convolution/IR loader,
plugin host, automation, reorderable rack, local node or recording mechanism. The middle-size,
1.8-second, damped 20% default is intended as a restrained room/hall starting point. Static networks
can retain modal coloration on sparse/tonal/sustained input, especially at extreme long/bright
settings. A 30-ms Size crossfade replaces the old geometry rather than preserving its full old decay.
Numerical density and physical delivery do **not** establish perceived naturalness, absence of
metallic ringing, acoustic quality relative to another reverb, or owner listening approval.

## Musical signal evidence

The permanent tests use an authored impulse, stepped melodic sine phrase and sustained tone.
They establish dense late samples, stereo differences, decreasing energy, distinct spatial settings,
longer decay at larger Decay, lower high-frequency difference energy at larger Damping, exact dry/
wet scaling, stable extrema and exact termination. Both 44.1/48 kHz and mono/stereo are covered.
These are signal/algorithm observations, not subjective listening.

VisualCheck additionally writes twelve authored nine-second stereo PCM16 listening studies: percussion
(decaying noise/body hits), a four-note melodic phrase, and sustained harmonics, each in dry/default/
small-bright/large-dark variants. Sources stop at two seconds; the remainder shows their natural tails.
No normalization hides gain changes. All generated peaks are below 0.30, with finite PCM and distinct
nonzero wet tails; dry tails are zero. The summed squared samples after source end are:

| Source | Default | Small/bright | Large/dark |
| --- | ---: | ---: | ---: |
| Percussion | 0.00473724 | 0.00000626778 | 0.0662348 |
| Phrase | 0.0720268 | 0.0111674 | 0.147632 |
| Sustain | 0.988718 | 0.130873 | 1.70976 |

These unnormalized sample-energy values compare these fixtures only. Offline elapsed times are
recorded in the study JSON; they are not callback CPU/deadline measurements. The WAVs are supplied
for actual listening; no subjective listening was performed by the agent.

## Stateful lifetime, admission and completion

Geometry/coefficients are prepared outside rendering. Existing execution construction on the control
publication owner allocates exact-size lines; it is outside the audio callback, not proof of bounded
GUI latency. Fresh disabled strips allocate no Reverb line payload. Reverb reserves two maximum-size
float/tag banks per admitted identity: 687168 bytes at 44.1 kHz, 747912 bytes at 48 kHz. A separate
16 MiB per-state payload admission limit therefore admits at most 24/22 identities. Actual initial
storage is one exact-size bank; Size transition temporarily retains two. Source/graph/old Delay,
object/array overhead, decoder/cache/UI memory and deferred GC remain additional costs.

Surviving admitted identities remain in the continuation budget even while bypassed. Disabled target
strips move the old processor without constructing another large disabled bank. Identity removal or
execution retirement ends this admission lifetime; a new fresh execution starts fresh. Sequentially
enabling many identities can hit the bound while earlier bypassed identities remain admitted. This
is explicit refusal, not permission for unbounded retention or a claim that all 64 strips can have
Reverb simultaneously. Existing four-slot reserved accounting includes the 16 MiB allowance; active/
pending/retired/candidate tests cover backpressure, cancellation/stale refusal and eventual release.

Same-geometry continuation transfers references without copying line PCM. Size changes retain the
old main bank for a 30-ms crossfade into freshly prepared geometry. Fast edits smooth Mix/bypass at
5 ms and damping/feedback at 20 ms. The completed fading bank stays referenced until retirement;
generation tags reset large histories without clearing/reclaiming them in the callback. Packet
preflight adds 512 work units per admitted strip/frame to the existing Mixer/tap cost, inside the
64 Mi-unit work gate. These units are conservative admission, not a wall-clock CPU guarantee.

Each Reverb expires after `2*Decay + 1` seconds of no input (maximum 17 s), fades the final 5 ms and
invalidates history. The bound allows two nominal RT60s plus diffusion/transition margin; it is not
an absolute silence detector. The longest reachable source-to-Master path adds serial Delay/Reverb/
bus allowances and takes the maximum across parallel sends. Mute/Solo, zero local/trim gain, zero
send level, post-fader silence and Mix=0 do not seed new tail time; valid pre-fader sends survive a
zero fader. Existing permitted live completion is retained, even after bypass/muting. Live publication additionally carries an immutable per-processor history-budget view: previous/current
allowances merge by maximum on surviving identities, and possible already excited histories seed
the new routes even after canonical source removal/muting or bus bypass. The inherited source-end
basis is retained. This prevents an old bus tail exciting a newly enabled Master from being cut at
the old transport end. Repeated publications do not compound the total; the same 120-s limit is
checked before publication, retaining last-valid audio on refusal. Historical allowances can remain
conservative after actual bypass expiry and can refuse live continuation while a fresh export remains
valid. Fresh exports have no such histories. Sample-level silence/cancellation can still produce
conservative silence; this is not dynamic tail trimming.

Natural source completion and full Pattern/Song WAV include this finite allowance. Ordinary Pattern
loops retain histories. Stop/Panic, Restart, cold Seek, Song range wrap, explicit hard Pattern stop
and retirement reset them; no effect preroll is added. Original NoteOffs remain fixed. A new test
exposed finite Pattern worker publication restarting transport; Mixer-only compatible publications
now preserve position/voices/epoch, without widening unrelated finite note/topology behavior.

Shared tail reachability fixes the prior audit's unused/muted/dry Delay silent-extension cases as
well as new Reverb cases. Large storage on every bypassed **Delay** strip remains existing debt;
there is no unrelated Delay allocation redesign. New Reverb admission and inherited-state/work/tail
failures refuse before publication, retain last-valid playback and expose the coordinator blocker.
Canonical edits/Undo remain accepted; Undo repairs invalid intent without restarting audio.

## Canonical state and compatibility

Reader 1.8 gates all non-default Reverb intent, including bypassed custom parameters/unknown data.
Exact disabled defaults are omitted, so projects without Reverb keep their earlier JSON shape/gate.
Unknown nested fields round-trip but block execution/destructive overwrite. Invalid/null/underdeclared
state refuses; no essential effect is silently ignored. Existing Undo/Redo, portable Save As/Open,
managed media, frozen WAV export and cancellation remain the owners. Runtime buffers/admission/
filters/tails are derived and never serialized. Historical binaries were not rerun in this checkpoint;
current reader gate/underdeclaration/unknown-state cases are executable tests, not old Desktop playback.

## Verification and reproducible commands

Windows 11, repository-pinned .NET SDK 10.0.401, .NET runtime 10.0.12, xUnit v3/MTP, Release, one
test thread. The `run-tests` skill/platform inspection selected SDK-10 native MTP syntax.

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore --max-threads 1
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll reverb <repo>/.artifacts/reverb-final native audio
```

Final L0/L3: full solution Release build, **zero warnings/errors**, 3.43 s final build; **1525/1525 tests pass**, zero skips, one thread, **77.892 s CLI** (77.644 s module).
**39 added Reverb cases** participate in this pass. L3 is selected for DSP/stateful lifetime, finite
Pattern continuation and reader/tail boundaries, independently of the zero-task cadence baseline.

Focused progress passes: 45/45 Reverb/Delay (16.742 s CLI); 92/92 Reverb/Delay/Mixer/R8 (36.043 s);
97/97 Reverb/Delay/Mixer/GraphRealtime after inherited admission refinement (7.016 s); the two new
finite-enable/refusal cases pass separately (1.484 s) before inclusion in L3. Final added extreme, portable export, publication-slot, removed-source/downstream-history and
inherited-tail-refusal cases participate in the final L3. These are not independent
full regression claims. Packet-size tests compare 37/127-frame delivery exactly; production WAV
samples match the shared smoothed renderer's PCM16 quantization, including late tails.

Selected primed callback scenarios measure **zero current-thread allocated bytes** for repeated
active processing/range wraps and live coefficient publication. Measurement excludes preparation,
harness arrays/assertions, source media and UI. Unprimed/JIT/first-use allocation and deadline behavior
are not accepted by those checks; no general pressure/GC/starvation closure follows. No heavy
allocation/reclamation is deliberately performed on the callback. Full physical callback telemetry,
maximum admitted simultaneous-bank stress and long-running failure recovery remain open.

Earlier attempts: a new audio fixture initially passed PCM16 into the normalized-float importer and
was corrected to use the production normalization path; a damping oracle exposed initially unfiltered
wet readouts and the wet readout now uses the damped line sample. Compile-time fixture/API/analyzer
errors were corrected. The finite-Pattern enable test first failed on epoch change and passes after
the scoped continuation correction. The first full run was 1515/1518 (64.014 s), with three stale
reader-version fixture expectations; those expectations now use the supported 1.8/future 1.9 boundary.
Final review added removed-source/new-Master and inherited-tail-budget regressions. The first
new fixture hit the existing empty-Song refusal; it was corrected to retain a muted occurrence.
An initially unbounded verification wait at Ended was stopped in its task-owned process and replaced
with a bounded deterministic loop. The corrected cases pass 2/2 (2.648 s); the related Reverb/Delay
refinement set passes 65/65 (8.357 s) before the final inherited-budget case. No failed attempt is
counted as acceptance. Final static checks cover 1339 relative links/anchors
across affected Markdown, ownership/scope/decision status and `git diff --check`; all pass. An initial GUI command refused a path outside `.artifacts`;
subsequent runs use the required ignored retention root.

## Actual GUI, native input and physical output

Production windows cover RU/EN × Dark/Light at 1400×900 and 800×600, with Mixer 850×400 and 500×400,
empty/populated states, short localized labels and accessible parameter fields. All 32 field centers
align at 0 DIP. Rendered reduced views in both languages/themes and empty states are spot-reviewed.
The local actual Reason rack reference was inspected for contextual compact controls; shared Seqvium
row/token/template styling is reused, with no shared style/component API change or proprietary asset.

Real routed controls commit Enabled/Decay/Size/Damping/Mix, reject invalid Decay, cancel with Escape,
Undo/Redo, Save/New/Open and export a 259200-frame (5.4-second) Song with nonzero final wet tail.
Guarded native Windows Decay entry and bypass/enable pass in the initial and pre-history-refinement
runs; setup/selection are routed. The final history-refinement run's native attempt is foreground/
coverage-refused because another window covers the target; no activation input is sent. This refusal
is recorded separately and does not erase or expand the two earlier native passes. This is not
full native composition or proof of every field's native gesture on every desktop/DPI configuration.

Physical playback passes through Samsung USB C Earphones, 48-kHz stereo: fault-free natural Ended,
position 259200, zero voices, unchanged epoch under a live Mix edit. This establishes delivery and
finite lifecycle only, not a listening assessment, acoustic transition quality, absolute latency,
physical 44.1-kHz hardware acceptance or maximum-load performance.

Final retained local evidence:
`.artifacts/reverb-tail-final/20261016T093045520-935bad81982d4d418aa24f2820d617d1/`.
Its routed matrix/history/export and physical playback pass after the history-budget refinement;
its native field is the explicit foreground refusal described above.
It contains matrix captures, alignment JSON, native/physical result, isolated preferences/project/WAV,
`reverb-study.json` and twelve `study-*.wav` files for review. Earlier native-success evidence is retained at
`.artifacts/reverb-final/20261016T091723101-a8f6ef18aa63459788ff29bfe6465bce/`. The initial successful
GUI run remains under `.artifacts/reverb/20261016T090543571-82928af6cc1343c09583c859eda3ac7e/` for comparison; these are
ignored and governed by rolling retention. No runtime/product asset depends on them. Required new
source/tests/reusable GUI verification are permanent under `src`, `tests` and `tools`.

## Limits and decision

Accept the bounded implemented Reverb capability and its declared verification; retain Q-057 for
general stateful seek/warm-up/transition and subjective acoustic assessment. Static-network coloration,
no independent Pre-delay/modulation, 30-ms geometry replacement, bounded admission retained across
bypass and conservative source-level tail bounds are explicit V1 limits. Cold FX seek, bypassed Delay
storage, R4-F2 pressure/starvation, long maximum-bank stress, owner listening/aesthetic review,
other-platform runtime, high/mixed DPI and broader accessibility/native authoring remain unclosed.
PATCH `0.1.11.0001 → 0.1.12.0000` is justified by a complete independently useful musical processor,
shared real signal/export, canonical compatibility and actual GUI/device evidence, not by diff size.
