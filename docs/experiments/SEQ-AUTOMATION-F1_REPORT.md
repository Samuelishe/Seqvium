# SEQ-AUTOMATION-F1 — Musical Parameter Automation

Role: Bounded implementation and verification evidence.
Read when: Reviewing Playlist automation, reproducing its musical checks or assessing remaining limits.
Authoritative for: Observations, methods, failed attempts and retained evidence of this checkpoint.
Not authoritative for: Runtime/format/UI contracts, future modulation scope or subjective acceptance.

## Outcome and scope

Product **0.1.15.0000**, one PATCH from 0.1.14.0000 with BUILD reset. Dedicated Playlist lanes
control subtractive-synth Cutoff and Mixer channel/bus Volume during Song playback/export. Points
support capture, snap/free timing, exact musical position/value, Linear/Smooth transition, bypass,
history and portable Save/Open. Base parameters remain unchanged. Stable typed target identity
survives deletion as unresolved intent, with no name/route-based replacement. Reader 1.12 gates
nonempty automation only. This is not general modulation, controller recording or plugin automation.

The home repository started clean on master at d63c14223933879a449c943ac68f77e2ce2b8a49. Authorized
fetch and fast-forward-only pull reached the expected published checkpoint
64ae82f05b6837eff8c87c6670f6cd87cd4c5e5a. No staging, commit, push, tag, reset, rebase or stash occurred.
Personal sample/reference roots were checked rather than inferred from another machine. Actual local
reference images informed the composition; private comparisons and images were not added to Git.

## Execution and musical scenarios

The canonical document owns absolute Song envelopes. Preparation publishes bounded frame points and
4097-entry physical lookups through PreparedSampler. OfflineSampler evaluates them sample by sample;
existing synth voice integrators/phases/ADSR and aggregate Mixer processing remain the execution owners.
Cutoff uses logarithmic Hz; Volume uses dB with an exact silence endpoint. Compatible curve edits
preserve voices, time and epoch. Existing scratch/work preflight accounts for automation tables/work.
Unknown essential execution state refuses preparation. Unresolved known targets remain visible/inactive.

The authored fixture is a four-bar held C/E/G chord at 120 BPM, repeated twice, plus 250-ms synth
release: **16.25 seconds / 780000 frames** in each 48-kHz stereo PCM16 export. It is a diagnostic
phrase, not a new track or change to the official demo.

| Example | Audible intention | Exported sample peak |
| --- | --- | --- |
| `cutoff-sweep.wav` | Filter opens from 90 Hz to 12000 Hz over four bars, then closes to 400 Hz; channel level stays at base | 0.06970215 |
| `mixer-fade.wav` | Channel rises from silence to unity, holds, then fades to silence; synth uses its base cutoff | 0.06820679 |
| `combined.wav` | Both envelopes execute on the same held/repeated phrase | 0.06970215 |

Independent tests establish spectral opening on a sustained chord and an independent per-frame
smoothed-fader oracle. Both 44.1/48 kHz pass packet/realtime/offline parity and cold destination
Seek/range-wrap comparisons, including explicit seek fade. Live point/enable/delete/Undo updates
retain three voices and epoch. Selected primed callbacks allocate zero measured bytes. Song PCM16
matches the same frozen renderer within one quantization step. Pattern/R8 use base parameters and
R8 output remains byte-identical when Song automation is added. Silence and a routed bus fader
have separate observable oracles. Missing sound/channel replacement, reader/unknown data, no-op
history, invalid/gated lanes and cancelled/stale preparation receive permanent regression cases.

## Verification

Windows x64, SDK **10.0.402**, runtime **10.0.13**. Locked solution restore succeeds; packages and
project manifests are unchanged. Final full Release build has **zero warnings/errors, 14.41 s**.
Full single-thread MTP suite: **1712/1712, zero skips, 67.684 s CLI**. This includes 22 new automation
cases and existing document/media/graph/sampler/synth/Mixer/desktop/metadata regressions.
Earlier focused routes pass **164/164 (3.772 s)** and **116/116 (14.107 s)** with affected builds.

```text
dotnet build Seqvium.sln -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1 --no-progress
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll automation <ignored-output> native audio --timeout-seconds=120
```

Actual production windows pass empty/populated RU/EN × Dark/Light at 1400×900, 900×700 and 640×480,
including narrow Playlist and locally scrolled minimum frames. The matrix covers both parameters:
**24 populated states, 48 actual field-presenter alignments, maximum center difference 0 DIP**.
Routed controls verify lane creation, point drag/add, one Undo, Escape/capture-loss cancellation,
exact value/history, rejected input and portable Save/New/Open/WAV. Windows point dragging passes
separately with foreground/hit-test guards; setup/history remain routed. The shared component catalog
passes. Popup native screen pixels are reviewed in addition to transparent content-only renders.
The final GUI follow-up also rejects `0:1:0` and commits `5:2:17` as tick 16320017 in the same
transaction as a value change, then verifies Undo/Redo. It repeats the native point drag without
replaying physical audio; the prior physical run remains separate evidence.

The owner-requested visual correction replaces the default accent Checkbox and four-row form with
a compact target/power header, `bar:beat:tick` and Value rows, transition and shared icon actions.
Common host materials, fields, typography and both-theme resources remain the authority; no private
palette/template or permanent explanatory paragraph is added. Actual minimum-window inspection keeps
the selected curve and contextual editing reachable. Named commercial references remain local.

Physical combined playback through **Steinberg UR12, 44100 Hz stereo** passes after reopen. A live
curve edit preserves epoch; natural Ended is at **716625 frames**, with zero voices. This checks
delivery and transport completion, not subjective listening or captured analog waveform identity.

## Failed attempts and corrections

Initial compilation caught test-driver/API mistakes and they were corrected before verification.
Initial new tests exposed fixture lifetime/empty-lane access and a loop shorter than the existing
10-ms minimum; corrected fixtures retain the production contracts. A strict bus oracle exposed lookup
precision at 2048 intervals; 4096 intervals pass its unchanged 1e-7 amplitude bound. No assertion was
relaxed. Early GUI setup was reflowed by Rack creation, leaving the intended drag outside its viewport;
the driver correctly refused it. The setup now restores/validates actual layout. Field discovery also
initially included an unrendered private ComboBox TextBox; alignment now selects the identified fields.

The first L3 has 1701/1711 passes: two stale reader-11 fixture assertions, seven empty Node Editor
tests requiring an eagerly constructed graphical Synth editor, and a preview allocation measurement
of 3984 bytes. Reader assertions are updated; Synth editor construction is deferred until a real
synth target exists, with normal lifetime/GUI preserved. The unchanged preview assertion passes in
isolation and subsequent full runs; its initial allocation cause is not established and no pressure
claim follows. The corrected 1711-case full run passes, then the final dedicated-lane Mute/Solo guard
and regression produce the 1712-case result above. Existing gated empty slots are preserved and skipped.

## Retention and limits

Current GUI/physical evidence is under ignored `.artifacts/automation-completion`; full TRX is under
`.artifacts/automation-regression-complete`, and the component catalog has its own ignored run.
The three WAVs and media-free projects are also retained outside rotation in the home Music directory
`Seqvium Automation F1` for owner listening. They need no original personal samples. Old failed/superseded
task captures are disposable under existing retention; the harness also rotated one older owned
Audio Clip capture and Mixer catalog runs. Protected/unknown artifacts remain despite the soft budget warning.

Curves are absolute Song controls: clip/section duplication does not clone or shift them. Endpoints
hold, automation does not extend Song duration, and cold seek/loop has no filter/FX preroll. Roughly
5-ms smoothing delays abrupt live changes slightly. There is no automation clip/range transform,
point marquee/clipboard, controller recording, external plugin target or wider parameter matrix.
No full native authoring, subjective acoustic/aesthetic approval, high-DPI/cross-monitor/other-platform,
maximum-load pressure, starvation or general R4-F2 closure is claimed. Current contracts remain in
[Audio](../AUDIO_ENGINE.md#musical-parameter-automation), [format](../PROJECT_FORMAT.md#parameter-automation-reader-112)
and [UX](../UX_CONTRACT.md#playlist-parameter-automation).
