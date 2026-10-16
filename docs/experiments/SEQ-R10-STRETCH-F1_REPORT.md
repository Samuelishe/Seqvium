# SEQ-R10-STRETCH-F1 — local Audio Clip time stretch

Date: 2026-10-16. Base: local `master`, `dc003ff6` (SEQ-R8, 0.1.9.0000), initially clean.
No fetch/pull, branch/index mutation, stage, commit, push or tag was performed. This report records
implementation evidence and limits; current contracts belong to [Audio](../AUDIO_ENGINE.md#local-audio-clip-time-stretch),
[Architecture](../ARCHITECTURE.md#implemented-standalone-audio-clip-boundary),
[UX](../UX_CONTRACT.md#implemented-audio-clip-workflow) and
[format](../PROJECT_FORMAT.md#local-time-stretch-reader-16).

## Delivered musical scope

Import a WAV directly into Playlist, select its Audio Clip, open properties and Apply **Time stretch**
from **50–200%**. Two seconds becomes four at 200% while retaining the fundamental, or one at 50%.
The source length remains two seconds; playback duration and the `↔` percentage show the different
intent. Edges continue to trim; repeat count remains separate. Closing without Apply cancels.
Original managed audio, shared source routing and other uses are unchanged. One canonical clip edit
supports Undo/Redo, Save/Open and portable Save As. BPM never enables or changes the ratio.

This is bounded fixed-time local stretching for sustained/monophonic instrumental phrases. Source
envelopes span 0.1–60 s; derived PCM totals at most 128 MiB inside the existing 512 MiB Audio Clip
budget. Unity bypasses the algorithm and retains the previous long-source capability. Split pieces
retain their parent derivation and can be shorter than 0.1 s. Independent range/fade edits establish
a new envelope; a resulting range below 0.1 s is refused at non-unity stretch. There is no tempo
following, warp engine, transient marker, beat detector, streaming or parallel audio engine.

## Algorithm and ownership decision

Independent C# WSOLA uses 40-ms windows, 20-ms synthesis hops, ±10-ms normalized waveform search,
channel-linked alignment and cosine crossfades. A coarse search with sample refinement bounds worker
cost while keeping PCM reads at native pitch. First and last windows anchor source endpoints.
This was selected for the narrow harmonic/monophonic material class and a dependency-free resident
preparation boundary. The [Driedger/Müller review](https://www.mdpi.com/2076-3417/6/2/57) describes
WSOLA and its material-dependent artifacts; it informed algorithm selection, not a copied implementation.
No third-party code, library, asset or native processor is distributed by this change.

The canonical decimal multiplier is clip-owned; source-frame range/fades remain intact. Absolute
scaled envelope endpoints round once to native playback frames. Their differences define pieces;
repeats convert cumulative playback frames to the actual host rate. Split keeps the existing virtual
clock/interpolation phase. Identical resource/range/ratio derivations share one PCM within a plan.
The immutable prepared source goes through the existing sampler contribution and Mixer, including
Song export. Generated PCM is disposable, not written over the resource or added to project media.

Preparation runs on the existing worker with cancellation between hops. Existing frozen snapshots,
generation/lifecycle checks, late-publication refusal, retirement slots and joined teardown retain
authority. Publication checks derived-source identity as well as numeric bounds: changing an offset
can produce different PCM with otherwise identical PreparedNote fields. Changed voices reconstruct
their current age; unrelated Pattern voices and the transport clock continue. The callback executes
only prepared PCM reads, existing envelopes and Mixer DSP.

Non-unity stretch requires reader 1.6. Old fields default to unity; unity is omitted on Save so an
ordinary old project keeps its reader gate. Required new intent with a forged lower gate is refused.
Save never lowers a gate already loaded/saved. Product identity and format compatibility stay separate.

## Release verification

Platform: Windows x64, .NET SDK 10.0.401, xUnit v3/Microsoft.Testing.Platform. Existing restore assets
were reused; no dependency/package changes or restore were required.

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*AudioTimeStretchTests" --max-threads 1
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1
```

- Full Release solution build: **zero warnings/errors**, **2.69 s**.
- Focused new stretch cases: **23/23**, zero skips, **4.702 s** CLI summary.
- Related AudioClip/Persistence/ProductVersion plus then-current stretch coverage: **162/162**,
  zero skips, **22.012 s**; the additional live Pattern case is included in the final 23-case run/L3.
- Full suite: **1450/1450**, zero skips, one thread, **60.159 s** CLI summary.
  L3 is selected for DSP/publication and essential reader-gate risk, resetting completion cadence.
- Final source-identity/edge-trim refinement: affected Release test-project build **zero warnings/errors**,
  **6.56 s**, and **48/48** stretch/AudioClip cases, zero skips, **4.932 s**. The completed L3 is reused;
  this focused run and the final GUI scenario cover the subsequent narrow changes.

The deterministic checks establish:

| Boundary | Evidence |
| --- | --- |
| Real duration/pitch | Two-second harmonic sources become 1–4 s; measured fundamental remains within 218–222 Hz around 220 Hz, not the 110/440-Hz speed-change result. A changing four-note phrase also retains its note frequencies at 50/200%. |
| Channels/rates | Mono and stereo 44.1/48-kHz sources; 44.1/48-kHz output, including cross-rate conversion. Linked stereo maintains the authored negative channel relation without cancellation of the search signal. |
| Start/end | Original first/last synthesis-hop samples agree exactly in direct DSP tests; production smoothing finishes inside the permitted range with final zero samples and no appended tail/silence. |
| Split/fades | Nonzero source/musical placement, crossing fades and nontrivial ratios 0.73137/1.33337: PCM before/after split is exact with shared derivation, phase and envelope. Undo/Redo is exact. |
| Repeats/time | Thirteen repeats use an independent absolute-boundary rounding oracle and exactly thirteen NoteOns. BPM changes retain the PCM and ratio; copies retain independent stretch. |
| Realtime/export | Realtime and offline PCM are exactly equal with equivalent smoothing and Mixer level/balance, at both output rates. Production 48-kHz PCM16 export has 192000 frames and differs by at most one quantization step. |
| Live edits | Ratio edits and equal-output-length source-offset changes reconstruct at the current clock. A separately sustained Pattern remains active; the transport does not restart. |
| Callback work | Selected warmed seek/range-loop callback processing allocates zero bytes on the measured thread. This is a bounded regression check, not general pressure/deadline certification. |
| Safety/history | Invalid ratio and short/long/oversize derivations refuse; cancellation, stale generation and closed-document publication cannot accept prepared results. Portable Save As/Open reproduces PCM after original/first saved media removal. |

Initial test attempts corrected a fixture that bypassed normalized-float import, and a comparison
that enabled smoothing only on the offline side. Reader refusal tests deliberately advance their
unsupported version from 1.6 to 1.7. These are test setup/contract updates, not hidden passing claims.

## Actual GUI and physical output

```text
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll stretch <ignored-output> native audio --timeout-seconds=120
```

The reusable friend host opens the real production MainWindow with isolated preferences, workspace
and media. Fixture generation/import and clip selection are routed; property Apply is a real retained
control action. It checks 200%, one Undo/Redo, 201% refusal, dismiss without acceptance, four-second
production Song WAV and Save As/New/Open after removing the external original. This is not claimed
as complete native musical authoring or Explorer file-dialog/drop acceptance for the new feature.
The final route also trims both edges of the stretched clip through captured pointer events, checks
the exact native-frame offset/length and unchanged ratio, and undoes each edit. Review found the
existing right-edge delta was clamped against the clip start at Song zero; right trim now snaps
against the actual end, allowing shortening there. The new scenario verifies that regression.

Properties and populated Playlist are rendered in **RU/EN × Dark/Light**, **1400×900 and 800×600**.
All **48** label/actual TextPresenter center differences are **0 DIP**, with contained text and valid
hit sizes. The popup remains compact: one extra percentage row, one duration caption, existing
range/fade/gain/routing controls and ordinary shared tokens. Empty states are reviewed in both
languages/themes. No new shared style or template required a catalog/style exception.
Local actual FL Playlist/waveform-editor screenshots were inspected before UI edits; no graphics
were copied into application assets.

Guarded native Windows opening of properties, exact percentage entry and Apply **passes**; initial
clip selection remains routed. The physical production-output check **passes** on **Samsung USB C
Earphones, 48 kHz stereo**: normal `Workflow.Ended`, position **192000**, zero active voices, no
pending preparation or last-valid/degraded execution. This confirms physical delivery and duration,
not subjective listening or measured acoustic waveform quality.

An earlier physical attempt reached `Workflow.DeviceFault`. Its first tool summary incorrectly
accepted any status other than PlayFailed; that result is invalid. The checker now requires a live
execution throughout polling, rejects DeviceFault and requires natural EOF. The successful repeated
strict check supersedes that summary; the earlier fault is not explained or erased by a later pass.
Earlier captures of an inner popup child included blank/transparent compositor results. Capturing
the actual popup TopLevel corrected this, and the visible themed frames were re-inspected. The
first GUI fixture also reopened Playlist after the empty-to-musical workspace transition.

## Limits and completion

Transient attacks may smear/double, complex polyphony may chorus/flutter, and endpoint anchors can
make local timing nonuniform near short ranges. Numerical pitch evidence does not establish general
perceptual transparency or owner musical/acoustic acceptance. There was no subjective listening,
stress/maximum-media or full callback-deadline certification, high/mixed-DPI, accessibility audit,
Linux/macOS runtime or physical 44.1-kHz endpoint check. The original R4 pressure gaps remain open.
Repreparation is resident and bounded, but not promised instantaneous or click-free during edits.

One completed standalone user capability qualifies for **PATCH 0.1.9.0000 → 0.1.10.0000**, BUILD reset.
It does not claim all R10 or wider Q-051 work complete. Current owners/public guides/state, resolved
portions/history and the version ledger are updated. Ignored GUI screenshots, synthetic portable
projects and WAV exports stay under `.artifacts/stretch-f1-*` for review under existing rolling
retention. Permanent source/tests/reusable VisualCheck remain product/tooling work; no generated
capture or disposable scratch file is added to Git. Root ProjectStats reports are refreshed after
the final edits and inspected separately from executed test counts.
