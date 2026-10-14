# Standalone Playlist Audio Clips — bounded completion

Role: Evidence for the implemented audio-arrangement checkpoint.
Read when: Reviewing native import/editing, media clocks/memory, Mixer integration and verification scope.
Authoritative for: Observed local results and their limits.
Not authoritative for: Product contracts, wider R10 acceptance, other-platform or acoustic support.

## Result and scope

Audio Clips are standalone Playlist occurrences, never synthetic Patterns. WAV/MP3/FLAC import,
actual mono/stereo waveforms, move/copy/trim/split/fades/gain/repeats, existing Mixer routing,
Undo/Redo, managed Save/Open/Save As and mixed Song WAV are implemented. Position uses musical
ticks; PCM range/fades use native frames. Moving or changing BPM never implicitly pitches/stretches.
Core has no codec dependency; Desktop uses NAudio.Wasapi/Core 2.2.1 for worker normalization and
Windows Media Foundation compressed import. Provenance is in [THIRD_PARTY](../THIRD_PARTY.md#audio-clip-codec-adapter).

Product PATCH 0.1.5.0001 → 0.1.6.0000 resets BUILD for this substantial independent musical
workflow. Reader 1.4 preserves essential audio intent independently of product identity. No staging,
commit, push, tag, release, full R10 container/stretch or earlier R4 acceptance is performed/claimed.

## Independent musical exam

Windows 11 x64, installed SDK 10.0.401, actual Avalonia production controls/resources, ordinary
interactive-user integrity. The official Neon Orchard and associated media were copied verbatim to
an independent task directory outside Git. No musical JSON or application edit/import API authored
the exam. The retained read-only model assertions only inspect the result of native actions.

The working composition adds four source identities and five clips: system-encoded short MP3 and
FLAC pads, an original 180-second stereo 48-kHz evolving WAV pad, and a task-owned copy of a personal
piano recording. Import creates no new Pattern. Native controls move the long pad, exercise history,
set its range to 50 seconds and fades to 1/2 seconds with -3 dB gain, create an explicit Mixer channel,
duplicate/Undo and Slice it. Real Explorer imports the piano onto Playlist. Private audio is not
distributed. The official project's Patterns, placements, Pattern arrangement, tracks and settings
compare unchanged against the musical exam; only the independent copy contains added source/clip/Mixer intent.

Native check phases then verify:

- Cancellation of an original 1200-second stereo float WAV import before acceptance, with unchanged
  clip count and history. This is cancellation evidence, not accepted 20-minute playback evidence.
- Corrupt WAV, unsupported OGG and six-channel WAV produce the corresponding localized decode,
  format and mono/stereo-only errors, without canonical edits.
- Native BPM 112→140 preserves all Audio Clip source-frame/fade intent; Undo restores 112.
- Actual left/right edge trim and upper-corner fade drag each change the intended source-frame field;
  native Undo restores the saved clip exactly. Fit song brings the long clip's far edge into view.
- Twelve duplicates of the existing long source while playing, followed by twelve Undo operations.
- Native Save As into a new relocated directory copies ten unique managed WAVs. The task-owned
  external Sources directory is then renamed out of its original location. Reopening, real playback,
  seek/range looping and native Song WAV succeed without those original paths.
- The Audio Clip's source reaches its existing Mixer channel meter. Native channel Mute/Solo,
  Undo/Redo and restoration succeed; no separate mixer is created. Save/reopen retains assignments.
- Actual RU/EN × Dark/Light maximized windows and a restored 1400×900 window show Playlist and compact
  audio properties. All five actual TextPresenter/label pairs differ by 0 DIP. The attempted smaller
  corner resize did not reduce that restored size; no 1024×768/high-DPI acceptance is inferred.

The external working project is `C:/Temp/Seqvium-R10-Audio-Clips-20261014b/Relocated/Neon Orchard -
Portable Audio.seqvium`; keep its associated media folder. Its export is `Audio Clips verified
022818.wav` in that directory. These are local musical work, not repository/runtime assets.

## Clocks, source sharing and physical/offline evidence

Deterministic tests compare independent native-range/fade/repeat PCM oracles at 48/44.1/8 kHz,
mono/stereo, packet partitions, BPM changes, history/portable media and missing managed files.
Split tests compare output bit-for-bit before/after cutting inside fades, including 44.1→48 kHz
resampling and nonzero musical placement. Source-envelope lineage and a common source clock retain
interpolation phase; adjacent matching split pieces do not acquire a new internal de-click ramp.
Seek/range wrap and compatible live range/gain changes reconstruct source age using fixed voice slots.
Pristine audio-only workflow import/play/save/reopen succeeds with no Pattern or graph attachment.

Twenty-five uses of a >16 MiB source retain one decoded array in a prepared Song and the same array
reference across frozen preparation/Save As. The native long-source test uses 69,120,000 PCM bytes:
working set grows from 417,026,048 to 504,344,576 bytes after twelve aliases, a growth of 87,318,528
bytes (~83.3 MiB), not twelve additional 69-MB arrays. Working set includes UI, temporary/old states
and other Pattern media; it is not a PCM allocation counter or proof of arbitrary-duration stability.

Read-only DeviceCheck runs the **whole 68.571-second saved mixed Song** through the unchanged UR12
44.1-kHz stereo float output, captures submitted PCM, compares same-rate/same-packet offline execution,
then compares the GUI's 48-kHz PCM16 export against its 48-kHz float renderer:

| Observation                                                            | Result                                                |
|------------------------------------------------------------------------|-------------------------------------------------------|
| Finite physical end                                                    | 3,024,000 frames at 44.1 kHz; joined Stop/teardown    |
| Submitted PCM vs offline maximum error                                 | 0                                                     |
| Native PCM16 export vs float maximum error                             | 0.0000152587890625 (rounding tolerance)               |
| Export                                                                 | 3,291,429 stereo frames at 48 kHz; 68.5714375 seconds |
| Export peak / saturated samples                                        | 0.8709716796875 / 0                                   |
| Callback/service allocations                                           | 0 / 0 bytes                                           |
| Processor/service/packet misses; padding exhaustion; nonfinite samples | All 0                                                 |
| Maximum observed voices                                                | 14                                                    |

This is actual device/submitted-signal evidence, **not acoustic listening**, a long pressure protocol,
maximum-polyphony stress, codec quality judgement or multi-platform acceptance. The original-wave
comparison input is the earlier independent audio exam export, not publication-01; its difference
confirms the added piano is audible, not an official-demo quality comparison.

## Verification and reproducibility

Final locked isolated full Release solution build: **zero warnings/errors**. Full MTP/xUnit suite:
**1224/1224 pass, zero skips**, 19.046 s CLI summary. Focused Audio Clip cases: 25/25, 1.491 s;
the earlier audio/workflow group: 58/58 before the two additional placement cases. Existing demo,
reader/metadata, Mixer and graph regression coverage participates in the full pass.
Final handoff audio/workflow/product-metadata subset passes 91/91 (8.431 s); the final 21 changed/new
Markdown pages have 954 valid relative targets/anchors, and `git diff --check` passes.

Commands use `--artifacts-path .artifacts/audio-clips/build` on build/test to avoid user-process output
locks. [Test execution](../TEST_EXECUTION.md#audio-clip-verification) and the
[native harness guide](../../tools/Seqvium.VisualCheck/README.md#native-audio-clip-check) own commands.
No new persistent test/CI framework was introduced.

Failures were preserved rather than relabeled: the first fixture FLAC encoder selection was invalid
and fixed to a supported 16-bit type; localized Windows filename access keys required native geometry
discovery; audio properties occupy a separate popup root. Elevated verification initially produced **no DragOver
events**: Explorer's medium token cannot drop into a high-integrity app. Same-user normal
launch yields real Copy DragOver/Drop and accepted audio. No security setting changed. Later native
probes initially observed a prior error/focused toolbar and a completed fast import; posted-command
synchronization, canvas refocus and a larger cancellation fixture corrected the probes. A later
pointer attempt targeted the long clip's offscreen far edge; fitting the Song corrected
that probe without a product change. A locked
verification executable required normal native Undo/Save/close of that owned host before rebuilding.
Four genuine split PCM regressions exposed truncated crossing fades and an artificial internal ramp;
the production fix, not a relaxed oracle, made them pass.

## Honest limits and retained material

Resident normalized audio is bounded to 512 MiB per source and unique audible Audio Clip PCM per
prepared Song. At 48-kHz stereo this is ~23:18; at 192-kHz stereo ~5:50. Pattern PCM has a separate
128-MiB budget. Prepared old/new states, waveforms, codec buffers and history/media consume more
memory/disk; there is no project-wide disk quota or disk streaming. Repeated imports remain distinct
sources; copies/splits/repeats share their existing source. Codec cancellation is cooperative and
cannot forcibly abort a stalled OS codec. Surround, time stretch/warp and combined audio/Pattern
marquee selection are not implemented. RIFF export retains its 32-bit size limit. WAV source support
is PCM16/24/32 and float32; MP3/FLAC depend on Windows codecs, not a portable universal backend.

Reusable source/tests/native tools are retained in their normal source locations. Ignored
`.artifacts/audio-clips/retained` holds selected raw drag, memory/cancel/error, field matrix and Mixer
captures/JSON for owner review; `.artifacts/audio-clips/device-final` holds the physical report and
`tests-full-final` the TRX. These local paths are evidence locations, not clone-available Markdown
links. Retain them through review of this checkpoint; ordinary other runs follow normal retention.
External independent musical project/media/export remain available for further GUI review. No private
library, design reference, official musical project or publication-01 was modified or staged.
