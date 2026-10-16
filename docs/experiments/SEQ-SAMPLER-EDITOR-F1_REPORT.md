# SEQ-SAMPLER-EDITOR-F1 — Waveform, Source Range and Sustain Loop

Role: Implementation and bounded verification evidence for the existing PCM Sampler editor.
Read when: Reviewing this checkpoint's implementation, measurements or limits.
Authoritative for: Observed checks, implementation decisions and remaining evidence gaps.
Not authoritative for: Broader audio acceptance, product contracts or future sampler capabilities.

## Outcome and checkpoint

Baseline was clean `74f983d5`, product `0.1.12.0000`. The existing PCM SoundDefinition, prepared
voices, OfflineSampler and singleton Source Editor now support non-destructive source range and
optional sustain looping. No second document, media system, DSP engine or window mechanism exists.
One verified PATCH selects **0.1.13.0000**, BUILD reset. This is a complete instrument-editing
capability with numerical, integrated, actual-window and physical preview evidence. MAJOR/MINOR,
staging, commit, push and tags are unchanged.

Canonical owners: [audio](../AUDIO_ENGINE.md#sampler-source-range-and-sustain-loop-seq-sampler-editor-f1),
[UX](../UX_CONTRACT.md#sampler-source-editor),
[format](../PROJECT_FORMAT.md#sampler-range-and-sustain-loop-reader-19),
[graph](../NODE_GRAPH.md#implemented-pcm-source-editor),
[sample workflow](../SAMPLE_WORKFLOW.md#instrument-source-range-and-sustain-loop).

## Implementation decisions

- Five optional sound parameters carry integer source-frame endpoints and a 0/1 enabled state.
  Full-source/no-retained-loop defaults omit the block. Non-default/retained intent requires reader
  1.9; old documents keep their previous gates. Unsafe/partial/fractional/unknown state round-trips
  but cannot execute. Actual PCM length is checked per sound, including shared-resource cache hits.
- Half-open ranges and a minimum four-frame contained loop protect interpolation at all tested
  pitches/rates. The absolute-age cursor plays the prefix then wraps modulo without incremental
  floating-point drift or extra NoteOns. Release continues looping under the existing finite envelope
  and ends the voice; the post-loop region is not played. Disabled full-source looping is legacy PCM.
- A linked gain dip at the explicit seam uses at most 1 ms, 64 source frames and one quarter of the
  loop. It reaches zero before wrap and rises after wrap, without hidden marker movement or changed
  PCM/period. Very short loops can pulse. This bounded treatment is not arbitrary seamless looping,
  crossfade synthesis or improved anti-aliasing.
- Existing finite admission/R8 lifetime uses NoteOff + release for a loop, rather than treating it as
  source EOF. Existing local Output before Mixer, frozen request, budgets, generation gates, media
  acceptance/Undo and joined shutdown remain. Song/FX retain their existing bounded tails.
- Compatible live range/loop publication preserves clock and unrelated voices, retires audibly
  changed voices and waits for the next scheduled note. A held voice is never retriggered or silently
  reconstructed with new markers. Continuous loop morphing is outside F1.
- Replace WAV atomically resets the old source block, retaining existing tuning/release policy and
  musical/graph/Mixer identity. Undo restores source and block together. Sample Lab's full candidate
  audition clears original-source coordinates; replacement uses the same reset boundary.
- Source editor observations reuse AudioWaveform's min/max hierarchy; fine zoom retains the immutable
  PCM array only with the disposable observation. Views own no output/voice leases. Target, generation,
  cancellation and lifecycle protect delivery. Preview reuses NoteAuditionRequest and existing joined
  output: ten-second Hold safety gate, configured Release and Stop, no canonical edits. Piano PCM
  audition observes source intent with its existing two-second gate and now configured release.

## Deterministic and integrated verification

New `SamplerEditorTests`: **68/68** pass, zero skips, one thread (**2.363 s CLI** final main focused run).
Forty rate/channel/pitch combinations use independent ramp/interpolation/seam/envelope oracles:
44.1/48-kHz sources and execution, mono/stereo, MIDI 0/48/60/72/127. Additional cases cover exclusive
range endpoints at half/normal/double speed, several wraps, zero/positive release, polyphony/capacity,
disabled legacy PCM, same-byte independent sounds, and exact packet/realtime/offline agreement.

Live edit checks establish unchanged epoch/clock, no replacement NoteOn, next-cycle restart, exact
unrelated looping PCM age, stale publication refusal, Stop/Panic and terminal retirement. Song seek
reconstructs wrapped/releasing voices; post-transition realtime PCM agrees with the offline oracle.
Looping warm callbacks allocate **zero measured bytes** in the selected tests at both execution rates.
A final 68/68 focused follow-up (2.853 s CLI) additionally measures zero bytes in the primed
preview Release/retirement callback and confirms zero live prepared states after owner reclamation.

The host test rejects invalid/stale edits without history, verifies one shared sound edit, preview
release without musical changes, output join on editing and close, and Undo restoration. Replacement
with a two-frame WAV resets the block coherently. Portable Save As/copy/Open executes after relocation;
low reader gates refuse, malformed/unknown sound intent is preserved and blocked. Actual old reader
binaries were not re-executed for this checkpoint; this is not a new historical-reader matrix claim.

R8 independently renders a 20-ms looped note plus 10-ms release to **1440 frames**, with nonzero
late signal and a silent endpoint. Adding Mixer Delay/Reverb leaves R8 bytes identical. Production
Song export includes finite late FX, agrees with the same execution within PCM16 quantization and
ends with no voices. Existing Pattern/Piano/Mixer/Audio Clip/resampling/history/media regressions
participate in L3 rather than being represented as newly authored feature tests.

The final natural-EOF audition refinement omits a future NoteOff beyond an earlier source EOF,
keeping the public prepared schedule ordered and bounded without changing its PCM. A subsequent
**112/112** sampler/Piano/metadata run passes, zero skips, one thread (**17.903 s**), including the
new natural-source audition oracle and warmed Release/retirement checks. Its affected Release build
has zero warnings/errors (**40.05 s**, with build servers disabled after memory cleanup). The existing
full-suite evidence is reused with this focused follow-up; no second L3 is claimed.

Full Release solution build: **zero warnings/errors, 8.65 s**. L3 result: **1593/1593 tests pass**, zero skips, one thread (**70.629 s CLI**).
First L3 before reader-fixture correction: **1581/1584**, exactly three outdated reader expectations;
the old unsupported-next-minor fixture moved from 9 to 10 and metadata expectations from 8 to 9.
That attempt took **178.379 s CLI**, exceeding the usual focused budget; it was a full risk-selected
suite, not repeated focused testing or a callback timing measurement. Early test authoring builds
also corrected actual API/cancellation/analyzer expectations before the green focused run.

Commands use the inspected SDK 10.0.401, Microsoft.Testing.Platform and xUnit v3 setup:

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests -c Release --no-restore --filter-class '*SamplerEditorTests' --max-threads 1
dotnet test --project tests/Seqvium.Tests -c Release --no-build --no-restore --max-threads 1
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sampler-editor <ignored-output> native audio
```

## Actual-window, input and physical evidence

Private local Ableton/Bitwig sampler screenshots were inspected before editing. They informed the
dominant waveform and separated range/loop flags; no proprietary asset is distributed. The new
surface uses existing tokens, standard numeric/parameter rows and existing pane sizing/scrolling.
Apply stays with preview/zoom commands so reduced windows keep the action visible. Four exact fields
wrap into two rows at the checked 480-DIP pane width. Crowded marker captions yield to flags/fields.

Production-window `sampler-editor` passes the Rack Sound Editor menu and actual routed Source
double-click, exact range/loop edits, invalid/short-loop refusal, incomplete text retention across
observations, one mouse drag/Undo, Escape/capture loss, zoom/pan/Fit, Save/New/Open after deleting the
external original, Pattern export and R8. The final authored fixture has a two-second note, 80-ms
release and a half-second stereo source: R8 completes at **99,840 frames**. No musical note was added
by preview. Empty/populated RU/EN × Dark/Light at 1400×900 and 800×600 were captured; actual images
were inspected. All **32 measured field/TextPresenter alignments have zero-DIP difference**.

An earlier guarded Windows Start/Apply check passed with routed setup (`review-04`). Final native
attempts, expanded to include marker drag, were refused before input because another window covered
the target. No native marker-drag or full native musical workflow acceptance is claimed. Initial
harness attempts corrected a strict `> 96000` oracle (exactly two seconds with zero release is valid)
and deferred Rack/Graph target setup. These were harness failures, not silently counted passes.

Final physical preview passes on **Samsung USB C Earphones, 48 kHz stereo**: held output continues
past original source EOF, explicit Release leaves zero voices/Playing false without device fault,
another Hold followed by Stop joins the output, and snapshot/Undo remain unchanged. The transient
status can already be retired to Unprepared when polled; its zero position is not a musical EOF
measurement. Deterministic tests own exact release length. Physical delivery does not establish
subjective seam quality, arbitrary material suitability, acoustic approval or pressure acceptance.

Ignored `.artifacts/sampler-editor-f1/` retains current JSON, screenshots and authored portable media
for review under existing rolling retention. Reusable verification source is tracked in tools/tests;
none of those captures is a product resource or a required clone artifact.

## Boundaries and remaining limits

No multisampling/zones, slicing, granular DSP, pitch-preserving voice stretch, MIDI recording,
universal plugin editor, additional decoder or backend was added. Playlist trim/stretch is separate.
Cold seek/range FX reset, linear pitch resampling, source/plan resident budgets, maximum-load and
R4-F2 starvation/pressure gaps remain. Warm selected callback observations do not close unprimed/JIT,
stress, other-device/platform/DPI, accessibility or subjective quality evidence gaps.

Memory/process investigation requested during work is recorded separately below; process counts
and OS working sets are not callback allocation or managed-leak measurements.

## Requested memory/process follow-up

The owner reported near-full 16-GB RAM and many .NET Host processes during this work. Read-only
Win32 process/parent/command snapshots identified six retained CLI MSBuild nodes (`/nodemode:1
/nodeReuse:true`) and one SDK VBCSCompiler from this session's builds. They are reusable build caches,
not seven running Seqvium applications. Before cleanup their summed working sets were about 1.7 GiB;
this sum includes shared pages and is not unique system RAM or a managed heap measurement.

After the main builds/tests, `dotnet build-server shutdown --msbuild --vbcscompiler` gracefully stopped
those seven servers. No broad process-name kill, IDE shutdown, user browser closure or repository
configuration change was used. Immediate free physical memory rose from **3104 to 4604 MiB** (about
**1.46 GiB**); a later snapshot is retained locally because other applications also change memory.
Three remaining dotnet hosts are identified Rider children: ClrStack Worker, Roslyn Worker and the
IDE's design-time MSBuild project. Rider frontend/backend, those helpers and the Codex host remained
running/responding with the same PIDs. No Seqvium/VisualCheck/test application was left running.

The before snapshot also attributes roughly 2.4 GiB working set / 3.5 GiB private committed memory
to the main Rider frontend, about 1.0/0.8 GiB to its backend, and a separate group of Brave processes.
Working set, committed private memory, shared pages and OS compression are different quantities;
adding all private/working-set figures does not reconstruct physical RAM exactly. There is no
historical snapshot at the reported peak and no heap-growth trace proving a leak. The demonstrated
reclaimable cause is retained CLI build servers; this does not rule out unrelated long-session leaks
in Rider/browser or establish a leak in sampler PCM. No user application state was discarded.

Local `memory-before.json`/`memory-after.json` contain PID/parent/memory/time observations without
full command lines. Final ProjectStats execution uses the built DLL, so it does not restart a compiler
or MSBuild server. Future builds can recreate these caches; use the same graceful shutdown after
large completed validation runs when their retained memory is unwanted, keeping Rider-owned workers. The final follow-up build explicitly used `--disable-build-servers`;
its short-lived Avalonia collector exited and only the same three Rider dotnet helpers remained.

The owner clarified that the target is progressive laptop lag after 4–6 hours, rather than simply
lowering memory usage. A short sample at roughly 4.75 hours of IDE uptime shows 7–19% total CPU,
about 4 GiB available RAM, almost no disk queue and no sustained hard-page input. Read-only JVM
inspection reports approximately 0.83 GiB used heap against a 2.5-GiB maximum; this does not suggest
an exhausted Java heap at the sampled moment. Today's IDE log records 7.5/9.4-second startup freezes,
not an identified late-session freeze. These observations do not diagnose an earlier or future lag.

A local, hidden PowerShell sampler under ignored `.artifacts/development-performance/session-20261016/`
now records system/per-process CPU, available/committed memory, paging, disk queue and process/handle
growth every 15 seconds. It has a six-hour/32-MiB bound, exits when the current Rider session ends,
and accepts a `stop.request` file. It records no source contents, full command lines or network data.
The first access-limited process handling was corrected before the verified active sampling run;
unreadable protected processes are counted and skipped. One intentional diagnostic PowerShell process
remains for this capture. Correlation with the actual lag timestamp is still needed to distinguish
system pressure from an IDE-local stall; no chronic-lag root cause or leak closure is claimed.

