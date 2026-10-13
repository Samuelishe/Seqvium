# SEQ-R9 — Shared Piano Roll

Role: Bounded musical editor implementation and verification evidence.
Date: 2026-10-13.
Authoritative for: Methods, observations, failures and evidence limits of this checkpoint.
Not authoritative for: Product contracts, stage-wide acceptance, release or realtime pressure policy.

## Question and accepted scope

Can the existing canonical NoteEvent/PCM/history pipeline support usable melody, chord and bass
authoring without an editor-private music model or destructive Rack conversion? The implemented
answer is a retained shared Pattern/Part editor, not an independent synth, MIDI recorder or demo.
Direct group gestures and aligned velocity were preferred to permanent per-note forms; exact
properties remain contextual. Rich Rack data uses a truthful preview rather than quantizing into pads.
Explicit root metadata/configuration was selected over filename/FFT pitch guessing.

[UX](../UX_CONTRACT.md#implemented-seq-r9-piano-roll), [audio](../AUDIO_ENGINE.md#seq-r9-pitched-note-audition),
[samples](../SAMPLE_WORKFLOW.md#seq-r9-source-root-pitch), [format](../PROJECT_FORMAT.md#seq-r9-note-editing-compatibility)
and [UI](../UI_DESIGN.md#implemented-seq-r9-musical-surface) retain current contracts.

## Inputs and environment

Windows 11 x64 home machine, .NET SDK 10.0.401/runtime 10.0.12, Release, existing pinned Avalonia and
xunit.v3.mtp-v1 3.2.2/MTP 1.9.1. SDK, dependencies and format markers are unchanged. Tests use owned
generated WAVs and controlled output borrowers; they require no private files or audio device.
Actual-window checks use the production MainWindow/resources/workflow with isolated preferences/media.
Native SendInput requires the exact foreground test window, never forced activation or fallback routed
input. Physical playback uses the normal Windows default output at unchanged system volume, 44.1 kHz
stereo; offline export is 48 kHz stereo PCM16. This route records no new broad buffer/stress measurement.

Private references were studied first under `%USERPROFILE%/Documents/Seqvium Resources/Design References`:
catalog, analytical brief and all five Piano originals. PR-05 note bodies/names, PR-01 bracket/axes,
PR-03 aligned parameters and PR-02 density inform original compositions; PR-04 controller curves are
deferred. No proprietary images/textures/icons are shipped and no new third-party dependency is added.

The supplied read-only `%USERPROFILE%/Music/Seqvium Sample Library` provides two catalogued VSCO piano
single notes, A3/MIDI 57 dyn1 and A2/MIDI 45 dyn3. Exact path and SHA-256 match the bytes accepted by
production import. Other filename/chord labels are not treated as root calibration. The library and
references remain unchanged; accepted project media is a separate durable copy, not a library rewrite.

## Implementation observations

- Core atomic Part note replacement retains shared identity/history/validation. Desktop freezes
  lifecycle/generation/Pattern/attachment and normalizes surviving note order. Stale/ABA, invalid,
  duplicate IDs, unknown semantics and oversized clipboard spans refuse without partial edits.
- Canvas supplies draw/group select/move/Ctrl-copy/resize/delete, exact properties, velocity handles,
  clipboard/keyboard, snap/triplet/free, horizontal anchored zoom, both-axis scrolling/pan, ghost Parts
  and renderer-derived local playhead. Captured previews commit once; Escape/capture loss cancels.
- Same notes drive Rack melody previews, Playlist thumbnails/placements, Pattern/Song and export.
  Pattern extension preserves rich gates; shortening refuses occupied tails. No rich-row pad action
  can flatten chords, velocity, off-grid data or lengths.
- Explicit shared root/name settings preserve note pitches and WAV bytes. Optional bounded catalog
  lookup is path/hash verified, fallback 60 is an honest setting, and Open preserves stored roots.
  The previous root ±12 refusal was removed from both sampler preparation and graph diagnostics;
  independent pitched PCM ramp tests cover octave ratios. Linear speed-based resampling is retained.
- Quiet stopped-note audition freezes source data, uses the same PCM kernel and joins its dedicated
  transient output lifetime before Play/Stop/close. It enters neither music nor history and is not
  downstream graph monitoring or another transport.

Real GUI work found and repaired: queued native resize left visible pane coordinates inconsistent
with contained model bounds and a hidden navigator strip; late shell notifications expired a newly
opened properties/root draft; mixed group properties overwrote unedited fields; survivor permutations
could create no-op history. Final checks assert actual window bounds, field centers, exact mixed
values and unchanged history rather than weakening tolerances.

## Reproduction and automated evidence

Restore inputs/assets were unchanged; valid existing locked assets were reused. After source changes:

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --report-xunit-trx --results-directory .artifacts/seq-r9/tests-full-final
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano C:/path/to/repo/.artifacts/seq-r9/gui
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll catalog C:/path/to/repo/.artifacts/seq-r9/catalog
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll gestures C:/path/to/repo/.artifacts/seq-r9/gestures
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-music C:/path/to/repo/.artifacts/seq-r9/music "C:/path/to/Seqvium Sample Library" "C:/path/to/preserved-examples"
```

The [test skill/installed MTP route](../TEST_EXECUTION.md#fast-commands-for-the-installed-runner) was
used for class OR filters, direct runner arguments and valid incremental output. Focused L1/L2 passed
163 cases (3.191 s CLI). Final L3: full Release solution build **3.57 s, zero warnings/errors**; **1073/1073 tests, zero
failures/skips**, **9.074 s CLI / 8.937 s runner**. Early L3 is justified by
shared pitch preparation/graph and transient execution-lifetime risk, not the version bump.
Cadence resets to zero. The TRX is local under
`.artifacts/seq-r9/tests-full-final/filed_IRWYN_2026-10-13_18_48_42.445770.trx`.

Early failures remain distinct: an obsolete pitch-refusal oracle needed replacement by unsupported
sound validation and actual pitched-output tests; five full-suite literal six-pane expectations needed
the seventh pane, and the Russian pane title needed its own localized identity. One no-build run after
a failed compilation used old 147-case binaries and is **not** new verification. A native velocity
expectation of 72% landed at 73% due to integer screen pixels; the independent expected value now
uses the actual screen-to-client round trip, not a widened arbitrary tolerance. Earlier GUI authority/
group/containment failures motivated the production repairs above. The pre-existing reduced-tempo
Rack harness gap from the preparation task was not rerun or relabelled as resolved.

## Actual GUI and musical result

`piano` success `20261013T184345684-1fb98946985749b3be771625756e8b8b` (11.14 s scenario) checks actual
RU/EN × Dark/Light at 1100×750 and 640×480, 19-note chord/melody editing, independent chord velocity,
copy/move/resize/cancel/Undo, numeric properties and group changed-only preservation, root popup,
safe Rack preview, retained lifecycle, Save/Open and graph export. Four numeric rows per composition
measure label/TextPresenter centers within 1 DIP. It uses routed events and no physical audio.

`catalog` success `20261013T184842371-a9c47f4d5c474defa8a27da59054c28f` checks both themes and shared
resource propagation. `gestures` success `20261013T185613473-f27f13a09abb49119b12fabb97fe5c21`
(11.49 s scenario) checks shared fields/Graph in both languages/themes, routed cancellation/lifecycle
and **eleven separately guarded native mouse/keyboard checks**. No audio is started by these routes.

`piano-music` success `20261013T185151375-e3f17d5403074faa91f5075df986643b` (27.09 s scenario):

- **Jade Nocturne**, 120 BPM, four-bar Pattern, 19 upper chord/melody notes and four lower-register
  notes in two named Parts. Native mouse/keyboard creates all 23 notes with mixed lengths, distinct
  chord velocities, move/Ctrl-copy/end-resize/Escape and exact Undo restoration.
- Roots are 57/45; hashes before/after acceptance/execution/export are identical. A3 root marks the
  upper keyboard correctly; the second Part retains its own A2 root. No automatic pitch inference.
- Production Save/Open restores the exact canonical revision and remains silent. One shared Playlist
  clip repeats twice. Returning to Rack shows two truthful melody previews without changing notes.
- Physical source-note audition, continuous Pattern and sixteen-second finite Song execute through
  the normal output. Peak simultaneous voices: **4**. Live future velocity edit and Undo converge
  without restarting the Song epoch. Final Song is Ended, no playing voices/blocker.
- Pattern/Song export succeeds: **384000 / 768000 frames** at 48 kHz stereo PCM16 (8/16 s).
  Song peak **0.0399169921875**, **0 saturated samples**. Exports are nonzero audio, not UI fixtures.

Import, Save/Open and export in this actual window use production workflow calls, **not native picker
gestures**. Native proof covers the note editor/Part choice, not every command surface. Physical
execution proves the output path ran, not subjective listening quality: JSON explicitly records
`AcousticJudgement=false`. No unverifiable claim of hearing or tuning perception is made.

## Retention, recommendation and limits

The user-review example is intentionally preserved outside Git and rolling artifact retention:
`%USERPROFILE%/Documents/Seqvium Resources/Verification/SEQ-R9/20261013-185218-984ffa4d` contains
`Jade Nocturne.seqvium`, its `.media`, Pattern/Song WAV and Dark/Light/Rack/Playlist captures.
It is useful reproducible music and owner-review material, not a distributed sample bundle; preserve
the local library's source-license boundaries when sharing. Owner controls its later deletion.
Raw JSON/PNG/TRX in `.artifacts/seq-r9` remain ignored under normal rolling retention, not durable links.
Superseded task-owned eligible GUI runs may be rotated by that existing policy; originals/user data,
required runtime/build material and reusable verification sources are preserved. New untracked source,
tests and VisualCheck files are permanent task implementation, not disposable artifacts.

Recommendation: accept the bounded real-editor implementation as locally verified, with owner visual/
musical review still welcome. Version **0.1.2.0000** is one delegated PATCH for a complete standalone
music editor, resetting BUILD; no MAJOR/MINOR, schema, dependency, release or Git publication action.
The sampler still changes natural duration with pitch, uses linear resampling and a bounded graph
32-voice/eight-channel model. No sustain loop/multisample map, high-quality time stretching, automation,
scale/quantize/humanization, MIDI/QWERTY recording or MPE; advanced tools and fine tuning are in
[R9 follow-ups](../ROADMAP.md#piano-roll-follow-ups). Independent Piano targets, broader accessibility,
high/mixed DPI, other OS/audio devices, long stress and subjective acoustic acceptance are untested.
Prior R4 starvation/acceptance status remains unchanged. No staging, commit, push or tag.
