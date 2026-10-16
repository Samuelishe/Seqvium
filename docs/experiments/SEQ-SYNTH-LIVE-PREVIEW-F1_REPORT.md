# SEQ-SYNTH-LIVE-PREVIEW-F1 — Continuous Synth Editing

Role: Bounded implementation and verification evidence.
Read when: Reviewing held Synth editing, UI stability, output recovery or reproducing this checkpoint.
Authoritative for: Observations, executed checks, failed attempts and retained evidence.
Not authoritative for: Runtime/format/UI contracts, subjective listening or wider pressure acceptance.

## Outcome and established causes

Product **0.1.16.0001**, one BUILD from 0.1.16.0000. Held preview hears knob and ADSR drafts during
capture, preserves its voice/output across accepted parameters, presets and history, and restores
canonical sound on Escape/capture loss. Repeated Hold keeps the voice; selected pitch retunes it.
Release freezes current level/duration; Stop joins output. No new instrument, track, dependency,
project field or reader gate is introduced. Work began on clean master at the expected published
`76081e3ae877ba42a4d2da00a3a0d0de3b95f41a`; no Git index/history/remote operation was performed.

Source inspection establishes the original interruption chain: SetSynthParametersAsync explicitly
closed note audition; DocumentChanged stopped generation-mismatched sampler preview; the editor
also stopped its held preview on every generation change. Pointer preview updated only drawings.
All three boundaries now distinguish a continuing supported Synth from an incompatible target.

The original visual refresh depended on global Busy and refreshed every parameter on transport
observations. A later strict native check also reproduced a distinct availability pulse: Note and
eight waveform buttons became disabled then enabled during editing. Visual Ready read the owner's
latest generation midway through an older UI observation. Availability now derives from one complete
captured observation; action admission and workflow commits still validate actual generation. Short
Synth commands keep the shared serialization gate without publishing a workspace Busy pulse.

The output-status investigation proves a recoverable stale-status defect: successful Synth Hold
did not replace a previous Workflow.DeviceFault. A controlled output invalidation is observed,
joined and remains visible; a deliberate successful retry now clears that status. This does **not**
identify the original screenshot's physical fault. Its generic message and absent retained device
code cannot distinguish device failure, starvation or another cause. Final UR12 checks report no
output failure. Real failures remain visible, with no automatic restart or hidden recovery.

## Execution and ownership

Desktop validates target/lifecycle/generation and sound parameters on the existing owner. A bounded
UI pump coalesces draft requests; RealtimeSampler publishes one immutable latest-wins parameter
record tied to transient execution authority and Stop identity. The existing single held Synth
voice consumes it at packet boundaries, retaining phases, integrators and age. Approximately 5-ms
smoothing covers continuous parameters and held envelope level; fixed waveform weights crossfade
at the current phases, including reversals. Voice scratch accounting increases to 256 bytes per slot.

Canonical Edit/Undo/Redo remain the sole durable truth. Save, recovery, Song automation, Pattern,
Mixer, offline WAV and frozen R8 never read audible drafts. Source removal stops and joins preview
before the serialized command completes; document replacement and existing shutdown remain joined.
The 30-second gate reserves up to eight seconds of edited release and ends at its actual frozen
duration. A released voice cannot be prolonged/restarted by subsequent edits or repeated Release.
Play after Release starts a new voice on the healthy resident output; Stop requires fresh output
on the next Play. Zero Sustain/Pluck can become silent while held. Repeated Hold is idempotent;
Release then Hold is the deliberate new attack.

Current contracts are owned by [Audio](../AUDIO_ENGINE.md#continuous-synth-preview-seq-synth-live-preview-f1),
[Architecture](../ARCHITECTURE.md#built-in-instrument-ownership),
[UX](../UX_CONTRACT.md#subtractive-synth-sound-design) and
[format](../PROJECT_FORMAT.md#subtractive-synth-reader-111).

## Verification and visual review

Windows x64, SDK **10.0.402**, runtime **10.0.13**. Existing locked restore assets were reused;
package manifests are unchanged. Full Release solution build: **zero warnings/errors, 15.13 s**.
Full one-thread MTP suite: **1760/1760**, zero skips, **66.298 s CLI**. Twenty-one new cases cover
each audible held parameter, Cutoff cancellation convergence against an uninterrupted voice,
preserved clock/epoch, exact edited release/safety boundaries at 44.1/48 kHz, frozen NoteOff duration,
primed zero callback allocation, resident-output history/presets/retrigger, stale generation,
Source removal/Undo, document replacement and injected actual-fault/explicit-retry status.
Existing Song/Automation/Pattern/Mixer/R8/media/reader regressions pass in this L3.

After the late visual-observation correction, the affected build passes with zero warnings/errors (10.72 s); final
Synth/DesignSystem/NodeEditor/Piano/navigation checks pass **114/114**, zero skips (4.480 s CLI). This focused follow-up
reuses the unchanged DSP/serialization L3; it is not a second
full-suite claim. No package restore or stress run was needed.

```text
dotnet build Seqvium.sln -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1 --no-progress --report-xunit-trx --results-directory .artifacts/synth-live-preview/regression
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll synth <ignored-output> native audio --timeout-seconds=360
```

The final actual-window route passes in **75.32 s**, including Windows mouse trajectories for all
eight dials and ADSR, unfinished-draft isolation, one commit, Undo and Escape, repeated Play and
waveform history. Undo between live gestures also uses the actual toolbar button through Windows
input; the localized routed matrix uses that same button through routed input. This final verifier
follow-up builds without warnings/errors (4.76 s), with production code unchanged from the 114-case
follow-up. Native note selection, natural Release, hard Stop/join and portable Save/Open pass.
Routed live gestures also run in each compact RU/EN × Dark/Light composition. All use the real **Steinberg UR12, 44100
Hz stereo**; neither OS audio settings nor another application's state is
changed. Native live trajectory retains one voice, execution ID 1 and epoch 2 while position advances
from **17199 to 978138 frames**. Synth availability and repeated-Play workspace availability each
record **zero transitions**. The separate physical Release receipt records null failure and zero
voices; explicit Stop joins. These observations establish delivery and lifetime, not human listening.

The matrix uses 1920×1080 / 1280×800 / 900×700 windows and 760×660 / 640×630 / 360×520 pane frames,
at render scale 1. It retains **127 inspection images**, including enlarged Release details,
short/long names/envelopes, held/released, focused/pressed and rejected exact values. Actual field
centers: **96 checks, maximum 0 DIP**; dial/value axes: **128 checks, maximum 0 DIP** across sixteen
composition audits. Actual ordinary/reduced/compact screenshots were inspected in both languages
and themes, including pinned transport, local scrolling, range errors and long names. No shared
style/template or visual composition was replaced. Relevant local reference images were inspected;
private comparative material is not distributed.

## Failed attempts, retention and limits

One new test initially queried its fixture's deleted target after New; retaining the old target
corrected the fixture. A verifier build initially missed its InputElement namespace. Neither was
accepted as a product pass. The first expanded native run passed; later final attempts are distinct:

- A native movement timed out with requested screen point (665, 544), actual cursor (846, 312),
  capture still owned by the enabled dial. Its physical segment passed. The input guard was retained;
  the origin of that cursor movement is unknown.
- The next attempt exposed the genuine visual availability pulse described above and also reported
  Physical release failed. That older physical receipt lacks the device code, so no cause is assigned.
  A new physical-release receipt records output failure/status/voice/parameter facts before assertion.
- After the visual projection correction, final-03 passes native, localized and physical checks with
  null output failure; native-history additionally passes real toolbar Undo during held editing.
  No input retry, foreground bypass or relaxed assertion occurs inside either run.

The final protected local evidence is under
`.artifacts/synth-live-preview/native-history/20261016T190924722-af301c9554294f00b817094268585667/`.
The retained availability/physical failure is under `final-02/20261016T190152584-d632ee9fede54eadb051c1c99cbd5137/`
in the same parent; full TRX is under `regression/`. Preserve these until owner review, then return
them to normal retention. Existing short factory WAVs and isolated projects are local fixtures,
not a new composition. Automatic VisualCheck retention removed an older legacy sampler-image run (12,874,009 bytes) and
the superseded cursor-failure image run (24,093,690 bytes); their original
screenshots were not restored. Protected/unknown user material remained despite the soft-budget
warning. No manual broad cleanup was performed.

Five-ms smoothing does not prove every preset/pitch transition acoustically click-free; zero Release
and hard Stop are deliberately immediate. The old generic failure and the earlier code-less physical
failure remain unclassified. Maximum-load pressure/starvation, subjective acoustic/aesthetic approval,
other-platform and mixed-DPI acceptance remain separate. Main Pattern/Song base sound edits retain
their existing changed-voice retirement; continuous draft morphing is scoped to Source preview.
