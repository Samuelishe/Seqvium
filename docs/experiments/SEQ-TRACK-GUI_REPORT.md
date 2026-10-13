# Native composition workflow evidence

Role: Evidence for SEQ-TRACK-GUI and the local Night Transit composition.
Read when: Reviewing the actual composing workflow and its bounded UI repairs.
Authoritative for: Observed authoring, verification, failures and limits.
Not authoritative for: Musical/editor contracts, roadmap acceptance, distributed assets or release status.

## Delivered music

Night Transit is saved outside Git in the owner's Desktop/Seqvium-Night-Transit folder, with its
`.seqvium.media` directory and a GUI-exported WAV. The source library stays unchanged; no library
content or musical project is bundled with the repository. The ordinary production Desktop was used,
with Windows native mouse/keyboard input and real open/save/import/export dialogs. UI Automation
located controls and read observations; it did not write canonical project data. JSON was read only
to inspect saved results. VisualCheck fixtures below are separate from authoring the delivered track.

The composition has seven Patterns, thirteen WAV channels, sixty-five authored notes and twenty-one
clips on seven named layers. Thirty-two bars at 124 BPM last 61.935479 seconds in the exported PCM.

| Layer | Musical role / arrangement |
| --- | --- |
| Drums | Four-on-the-floor kick and backbeat clap, bars 5–16 and 21–28 |
| Tops + Rim | Closed/open hats and syncopated rim, bars 3–16 and 21–30 |
| Midnight Bass | Two-bar C bass motif, bars 5–16 and 21–28 |
| Glass Keys | C5/A4/C4 piano phrase, introduction through the ending |
| Neon Chords | FM chord stabs, bars 9–28 including the break |
| Turnarounds | Snare/tom fills in bars 8, 16, 20 and 28 |
| Station Air | Cymbal accents in bars 1, 5, 9, 21 and 32 |

The forms contain an introduction, layered main section, four-bar break, return and short ending.
Original PCM24 chord/cymbal files and piano WAVs import through the existing decoder. Independent
Pattern Gain values balance the layers. Existing quarter-note gates, root sample pitch and stateless
Gain/mixing are used; no unimplemented Piano Roll, velocity, effects or automation is simulated.

The project was saved repeatedly between Patterns and arrangement passes. Reopening restored all
music silently. A native GUI run then played Song from the beginning through the actual Ended status:
33 read-only observations at roughly two-second intervals progressed to 32:4 and remained Ended.
The earlier pass also reached the end while the owner interacted with the application/export.
Foreground guards stopped native input outside Seqvium; ordinary title/taskbar activation resumed work.
Other user Desktop files and exports were left untouched.

GUI whole-Song export yields **2,972,903 frames**, 48 kHz stereo PCM16, 11,891,656 bytes, with peak
0.386138916 and **zero saturated samples**. RMS is 0.065105773. These are read-only file measurements,
not loudness mastering or an acoustic listening judgement. No crash or audio failure was observed in
the completed finite run; this does not close prior R4 pressure/all-device/platform acceptance.

## Obstacles repaired

- Fractional toolbar Gain input could publish the initial zero and erase the remaining draft.
  Input now stays local until Enter/focus loss, commits one existing guarded graph edit, restores on
  Escape and rejects expired authority. Enter restores text focus after the command's temporary busy
  state. Renderer observation cannot erase an unchanged draft.
- A short/zoomed-out Playlist centered its fixed-size canvas in a larger pane, leaving empty margins
  and shifting the timeline origin. Both canvas and scroll content now align at TOP + LEFT; derived
  extent covers the visible horizontal viewport with editable bars up to the existing 4096-bar bound.
  All 32 supported lanes are scrollable immediately, instead of requiring clips to expose lanes past 8.
- The visible 24-DIP corner grip had only a 12-DIP inner corner target. Hit regions now match the
  visible grip, while straight edges keep their six-DIP bands and ordinary controls retain priority.
- A transient normal-sized startup arrange permanently shrank panes saved in a maximized window.
  MainWindow replays the saved layout against its settled workspace. Actual ordinary Desktop reopening
  retained the authored large Playlist; the pure regression covers the transient clamp/replay boundary.

These are bounded existing workflow corrections. BUILD advances once from 0.1.1.0001 to 0.1.1.0002;
no project schema, dependency, DSP/backend, PATCH/MINOR decision or architectural rewrite is introduced.

## Verification

Windows x64, SDK 10.0.401, xUnit v3 / Microsoft.Testing.Platform. Unchanged locked assets were reused.
Focused commands used the actual installed runner syntax:

```text
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*PaneGeometryTests" --filter-class "*WorkspaceBoundaryTests" --filter-class "*WorkspaceLayoutTests" --filter-class "*WorkspaceStateTests" --filter-class "*GraphEditorWorkflowTests" --filter-class "*DesignSystemTests" --filter-class "*ProductVersionTests" --report-xunit-trx --results-directory <task artifacts>/focused-tests
Seqvium.VisualCheck gestures <task artifacts>/fields-green
Seqvium.VisualCheck arrangement <task artifacts>/playlist-final
```

Affected Release builds have zero warnings/errors. The final focused build took 23.11 s and
**183/183** focused cases passed (15.935 s module, 16.255 s CLI). The later Enter-focus correction
was built in 6.74 s and verified by the final actual-window toolbar regression, including observation,
fractional Enter, Escape, invalid input, focus loss and expired authority. A final tool-only rebuild
took 1.58 s. L1/L2 is focused; the earlier SEQ-PLAYLIST-MUSIC 1052-case full suite is not represented
as a repeated full pass of this UI task. Cadence advances to one completed task since that L3.

The final field/gesture route succeeds in 15.021 s, checks RU/EN Dark/Light alignment, and also passes
eleven separately guarded native Graph/Workspace checks. The Playlist route succeeds in 11.385 s;
its extent check observes origin (0,0), viewport width 1038 DIP, content width 1044 DIP and height
2336 DIP for all 32 lanes. Group/range/zoom, persistence and existing musical controls remain covered.
Actual native scrolling in ordinary Desktop also exposes lane 32. Relevant screenshots were reviewed.
There is no shared-style/palette/template change requiring another component-catalog pass.

Initial verification exposed two corrections: Enter's transient busy state lost the numeric field
focus, and the fixture attempted refocus before refreshing its controls after Undo. Production now
restores Enter focus; the fixture refreshes the latest observation before requesting focus. An ordinary
rebuild also encountered the still-open Desktop's Windows output lock after foreground-guard refusal;
the project was saved and the window closed normally before the successful rebuild. No process kill,
assertion weakening, user-file cleanup or staging/history mutation was used.

Native composition and repeated real application checks intentionally exceed the ordinary 120-second
focused-verification target. Repaired build/test/render checks themselves remain bounded; no unchanged
full regression suite was repeated. Physical output confirms this one finite project, without claiming
zero packet misses/allocations or the separate long-pressure contract.

## Retained artifacts and scope

Ignored `.artifacts/seq-track-gui` retains rendered/native Seqvium screenshots, input journal/recipe,
read-only playback observations, export audit and focused TRX for review. VisualCheck runs retain their
existing rolling manifests; the task's native authoring evidence is local and not a public resource.
Cleanup of unrelated desktop/browser/IDE captures and disposable action/probe files was rejected by
automatic command review (`blocked by policy`), including a narrower literal-path attempt. These
task-owned files remain ignored locally; no user Desktop content or staging index was affected.
The permanent regression source lives with VisualCheck/tests. The delivered project, media and WAV
stay in the user's Desktop folder; no samples or generated song are tracked. Initial Git status was
clean; final tracked/untracked changes belong to this task. No staging, commit, push, tag or release.
