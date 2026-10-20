# Multisample Velocity Layers F1

Date: 2026-10-20. Base: `master`, product `0.1.16.0010`, clean worktree.
Result: **0.1.16.0011**, one BUILD within the existing multisample instrument line.

## Implemented workflow

The existing PCM Source Editor can build two/three-recording instruments with the same confirmed
Root. Add WAVs opens one compact review containing existing and new recordings. Split layers groups
equal roots, distributes keys between distinct roots and divides each group's Velocity in source
order. Root/key/Velocity fields remain editable before one atomic Add WAVs transaction. Unknown
roots, ambiguous rectangles and invalid bounds block acceptance. Cancel accepts nothing. Ordinary
pitch-only imports retain automatic nearest-root distribution; Auto map refuses velocity-bearing
maps without changing their settings.

The same compact keyboard draws pitch horizontally and Velocity vertically, with recording names
and percentages. A scrolling selector exposes every zone, including rectangles too narrow to label.
Exact selected-zone Velocity fields join the existing root/key fields. Source range, Loop and
Crossfade remain independent per zone. Selected WAV previews that recording at its own root;
Instrument and working keyboard keys select by pitch and preview Velocity. Unavailable combinations
report a localized error without playing another recording. Preview changes do not edit music.
Undo/Redo and portable Save/Open/Save As retain accepted map identity/settings and managed media.

## Execution and compatibility

SamplerZone stores decimal LowVelocity/HighVelocity, default 0/1. Intervals include the lower bound
and exclude the upper bound, except 1 is included. Inclusive key ranges may overlap only when
velocity does not. Validation rejects any ambiguous pair independently of array order; gaps schedule
no voice. Fractional notes select by their lower integer key and retain exact pitch for transposition.

Preparation compiles one resource/step/range into each PreparedNote using NoteEvent.Intensity.
Existing amplitude scaling stays unchanged. Pattern/Song, Piano audition, Source preview, WAV
export and R8 retain the existing sampler/Graph/Mixer execution. No callback selection, file decode,
UI access, additional voice or DSP engine is introduced. Compatible publication retains clock and
unchanged voice age; changed resource/step/playback voices retire without replaying past NoteOns.

Reader 1.14 gates only non-default Velocity bounds. Omitted defaults retain legacy single-source
and pitch-only representation/gates/PCM. Underdeclared intent is refused, saved floors remain
monotonic, and unknown zone data survives while unsupported execution/destructive edits are blocked.
The import owner reuses hash-matching, integrity-validated managed WAVs from the document/batch,
retaining independent zone identities and playback parameters. Unique decoded PCM, preparation,
generation/lifecycle/cancellation, rollback and atomic Save owners remain unchanged.

## Numerical and automated evidence

Tests use short authored ramp, opposite-polarity ramp and periodic stereo WAVs. No personal library
or third-party samples are needed. The Velocity partial adds **32 expanded cases** covering:

- Two/three actual recordings across 44.1/48 kHz and mono/stereo, 0/100%, exact boundaries and fractional
  pitch, with independent interpolated signal/amplitude oracles and the prepared resource identity.
- Reversed arrays, two-dimensional ambiguity, gaps, malformed ranges and all 128 zone slots.
- Different roots, independent source range/loop/crossfade, complete isolated-source comparison,
  multi-instrument Song/realtime parity, production PCM16 quantization and R8 Mixer exclusion.
- No-op/history, reader omission/gating, opaque preservation/blocking, portable Save/Open/Save As,
  deduplicated import and cancelled/stale/ABA/closed/missing-media refusal.
- Actual selected-zone versus instrument audition, finite Release/Stop, Pattern/Song live clock
  retention and affected-source retirement, 32 simultaneous mixed-velocity notes and 33-note refusal.

The 32-note preparation retains two shared 256-frame stereo arrays (**4096 decoded bytes**). One
primed kernel Process interval measures **0 allocated bytes**. This is a bounded observation, not
a CPU-pressure, scheduler, maximum-map throughput or general realtime allocation acceptance claim.
The 32-voice limit is unchanged. GUI WAV/R8 retain the existing 48-kHz stereo graph-port contract;
mono/rate parity is checked through prepared execution APIs.

Executed commands use SDK **10.0.402**, runtime **10.0.13**, xUnit v3/MTP, one test thread. Restore
inputs/assets were unchanged; no restore was needed.

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class '*SamplerKeymapTests' --filter-class '*SamplerEditorTests' --filter-class '*PersistenceTests' --filter-class '*ManagedMediaTests' --filter-class '*SampleReplacementTests' --filter-class '*PatternResamplingTests' --filter-class '*PianoRollTests' --filter-class '*ArrangementTests' --max-threads 1
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1 --report-xunit-trx --results-directory .artifacts/sampler-velocity/l3-checkpoint
```

L1/L2: **370/370** pass, zero skips, **8.486 s CLI**. After the reader fixture correction and final
import/preview guards, the targeted Sampler/Reader/ArtifactRetention run passes **100/100**, zero
skips, **47.791 s CLI**. The checkpoint full Release build has zero warnings/errors (**25.77 s**;
final incremental build **15.32 s**). Final L3 at product **0.1.16.0011** passes **2026/2026**,
zero skips, one thread, **96.105 s CLI**. All 32 new expanded cases are included. The green final
run preserves the earlier failure evidence and does not attribute or close intermittent tooling,
general allocation or realtime-pressure gaps. Relative targets in all 14 affected current Markdown files
resolve, and the final whitespace/diff check passes.

## Actual GUI, native input and physical output

`sampler-velocity <output> native audio` uses three generated 0.5-second stereo recordings. Picked
paths/setup are routed; the actual modal review, Split layers, source/2D selection, exact Velocity,
invalid overlap, Cancel, one import Undo/Redo, percentage Undo/Redo, unavailable preview and Save/New/
Open run through production controls/workflow. The final review additionally verifies changing a
confirmed root while another field is invalid, then rebuilding a valid draft. Native file-picker
authoring is not claimed.

Eight real-window populated configurations cover RU/EN × Dark/Light at 1400×900 / 800×700, with
900×710 / 480×590 Source panes. All **48 measured field/label centers differ by 0 DIP**. A 40-zone
map permits selecting its last source in a reduced pane. Actual screenshots were inspected for
source identity, layer labels, clipped density, readable controls and waveform space. Shared tokens,
standard controls and the existing keyboard are retained. The unchanged `sampler-editor` route also
passes its range/loop/crossfade, localized empty/populated/520-DIP-height, portable-media and finite
R8 checks (**99840 frames**). There is no shared-style/template change requiring a new catalog run.

Evidence tiers remain separate:

| Tier | Observation |
| --- | --- |
| Numerical PCM | Independent source/amplitude oracles and prepared/realtime/offline comparisons above. |
| Routed GUI | Actual review/editor controls, history, Save/Open and the full matrix pass. |
| Native Windows | An earlier guarded exact Velocity entry passes with routed source setup. Final and checkpoint repeats refuse startup because another window covers the target; those attempts are not passes. |
| Physical output | Final/checkpoint Instrument previews at 20/50/90% and selected-WAV Hold/Release/Stop pass on Samsung USB C Earphones, 48000 Hz stereo, with advancing callback position and no reported device fault. These are short output checks. |
| Subjective listening | Not performed; no timbral preference, acoustic quality or owner aesthetic acceptance is inferred. |

Review screenshots and raw JSON/TRX stay ignored under `.artifacts/sampler-velocity/`. The final
checkpoint render set is `checkpoint/20261020T053952186-e79e55e3e12045f48b3f333c2e2cd250` (ten PNGs),
with earlier successful native entry in `run-03/20261020T052658565-e88b285d441548eeb0a2567a11573403`.
They are retained for local review, not distributed application assets or permanent Markdown links.
Automatic artifact cleanup was held off with the existing exclusive retention lock during later
GUI checks, preserving earlier evidence. Reusable checks live in VisualCheck and regular tests.

## Failed attempts and limits

Initial builds caught missing UI imports/locals and required test cancellation arguments. Early
fixture assertions incorrectly expected a changed Root when it was already 60, assumed integer
percentage formatting retained no trailing zeros, counted media outside the document's existing
managed root, used default instead of explicit 32-voice admission, and attempted stereo-only GUI
export from mono graph ports. Those harness errors were corrected without changing audio limits.

The first L3 reports **2018/2022**, four failures, zero skips, **256.165 s CLI**. Two product metadata
cases still expected Reader 13; their reader constant expectations are updated while legacy document
floor assertions remain unchanged. Two existing ArtifactRetention cases receive no usable Windows
process snapshot and conservatively protect every directory; the log explicitly says `Process
ownership snapshot unavailable; all runs protected`. Their unchanged isolated rerun passes. No
retention/foreground guard is weakened and the green rerun does not establish a tooling fix.
The largest first-run costs are official demo export (**32.286/11.885 s**) and MSBuild metadata
subprocesses (**11.226/10.965 s**), followed by snapshot-dependent cleanup. This is the measured
overrun of the ordinary 120-second focused target, not new sampler callback work.

No round-robin, velocity crossfade, random selection, articulation, MIDI, new synthesis/effects,
global normalization, streaming or polyphony increase is included. General pressure, long physical
sessions, high-DPI/other-platform and subjective acceptance remain open. Official Neon Orchard is
unchanged. Git stage/commit/push/tag/history/remote operations were not performed.

Suggested commit: `SEQ-SAMPLER-VELOCITY-LAYERS-F1 Add multisample velocity layers`
