# SEQ-R9-PIANO-HUMANIZE-F1 — Piano Roll Humanization

Date: 2026-10-19. Windows x64, SDK 10.0.402/runtime 10.0.13, Avalonia 12.1.3, xUnit v3/MTP.
Baseline: clean `master`, `acea6de98cc31028697ee53cdd0743b3c7292ae0`, product 0.1.16.0007.
Checkpoint: **0.1.16.0008**, one BUILD within the existing Piano editor line. This report owns
observations; [UX](../UX_CONTRACT.md#implemented-seq-r9-piano-roll) owns the musical contract.

## Scope and choice

The question was whether a small explicit note tool could loosen timing/dynamics while retaining
canonical identity, chords, history, sharing and live playback. The implementation uses the existing
Piano note transforms, serialized workflow and one SetPartNotes transaction. A general random-event
framework, preview music store, seed serialization and new DSP/transport were unnecessary.

Full Timing strength draws integer offsets in [-30000,30000] ticks, 1/32 quarter or 15.625 ms at
120 BPM. Full Velocity draws offsets in [-0.10,0.10] intensity, with 0.00001 resolution. Strengths
scale these intervals linearly. Initial strengths are 20%, corresponding to 6000 ticks and 0.02.
These ranges give small phrase variation independently of Snap, without requiring prior Quantize.
Equal original selected onsets receive one common displacement. The longest selected duration
limits its upper boundary; zero limits its lower boundary. Clamping preserves complete notes and
may collapse an outward draw to no movement. Unselected chord members stay exact. Independent
velocity samples clamp to [0,1]. Repeated explicit Apply intentionally acts on the accepted result.

Only selected Position/Intensity change. IDs, canonical order, fractional pitch, duration, unrelated
Parts/Patterns, sources, routes and Playlist structure are exact. Shared uses observe the same edit;
Make variation and Make this repeat unique retain independent content with shared instruments.
No project-format/reader, dependency, settings, audio engine or publication contract changes.

## Automated verification

Commands used the restored checkout and Release binaries:

```powershell
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class '*PianoHumanizeTests' --filter-class '*PianoToolsTests' --filter-class '*PianoRollTests' --filter-class '*RepeatVariationTests' --filter-class '*RepeatVariationAudioTests' --filter-class '*StepSequencerTests' --filter-class '*PatternEditorNavigationTests' --filter-class '*WorkflowPresentationTests' --filter-class '*HostPresentationTests' --filter-class '*ProductVersionTests' --report-xunit-trx --results-directory .artifacts/piano-humanize/final-tests
```

The final affected build (including its production/VisualCheck references) passes with **zero
warnings/errors, 31.87 s**. The focused test run passes **242/242**, no skips, **28.348 s CLI**.
The RepeatVariation audio file contributes to the same partial RepeatVariationTests class; its
separate filename is not another test class. An earlier smaller run passed 99/99 in 2.996 s before
the Busy/Closed case was added. Later harness-only builds also pass with zero warnings/errors.

The 19 new Humanize cases cover:

- Seeded repeatability across 100 seeds and five independent-strength combinations, tick/velocity
  bounds, zero dimensions, note/field/order/selection locality and shared onset groups.
- Exact positive/negative endpoints, start/end clamping, mixed chord durations, velocity 0/1,
  full-length immovable notes, one-tick gates and positions near Int64.MaxValue.
- No-op revision/generation/history and Redo retention, one accepted Undo, exact snapshot Redo,
  and a counting random source proving no draws on zero/empty/refused/Redo/Save/Open/export/observation.
- Selected-Part edits with other notes/Parts, shared repeats and Mixer, exact non-musical-state
  equality, whole-clip/repeat variations, persistence and finite export.
- Stale generation/ABA/lifecycle, wrong Part/missing selected IDs, invalid strengths, Busy/Closed
  and unknown Pattern/Part/note/sound rejection before randomness or mutation.
- Controlled Pattern/Song publication through the production coordinator: executing revision,
  unchanged epoch/playing time, exact Undo/Redo and canonical PCM agreement within 0.000001.

The other 223 tests cover Piano/Quantize/Scale, repeat independence and PCM, Rack, navigation,
presentation and real built product metadata. This is L0/L1/L2, not a full regression pass.
No shared renderer or schema changed, and the preceding Piano-tools checkpoint supplied L3;
cadence advances to one task rather than automatically running the complete Release suite.

## Native, visual and physical evidence

Reusable scenario:

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-humanize .artifacts/piano-humanize/review audio --timeout-seconds=300
```

An isolated Synth chord/phrase and shared repeat fixture uses production APIs for setup. Guarded
Windows mouse/keyboard input selects three chord members while leaving another note untouched,
chooses Free Snap, opens Humanize, edits exact strengths, Cancel/Escape, applies zero and nonzero
settings, and performs exact Ctrl+Z/Ctrl+Y. No note changes before Apply. The accepted edit adds one
Undo and preserves common onset/other fields. UI observations never regenerate music.

The full first run passes **12 combinations**: RU/EN × Dark/Light × 1080/520/360-DIP Piano panes,
with populated flyouts and empty Parts. The narrow toolbar wraps using its existing layout; the
ordinary Popup fits its percentage fields and Apply/Cancel. No instructional paragraphs, extra
parameter strip, private palette or shared control-template change is introduced. Native Part
switching shows Humanize disabled on empty selection/Part. External generation and pane hide
close a pending flyout. Save/Open/export use production APIs rather than native file pickers.

Additional `quick` runs keep all four languages/themes at 360 DIP. They exercise each slider
independently and measure actual numeric TextPresenter/label centers: **0 DIP difference** for
both fields in every combination, 26-DIP fields with 16-DIP text and centered template alignment.
The final guarded light-dismiss check targets the actual flyout overlay, preserving the underlying
music. Cancel, Escape, light dismissal and zero Apply leave the canonical snapshot/history intact.

Short real Pattern and Song runs use **Samsung USB C Earphones, 48000 Hz, stereo**. The open
flyout retains focus through 1600-ms observation windows. Native Apply/Undo/Redo retains playing
state and transport epoch (Pattern 2, Song 5 in the recorded fixture), with no output failure.
These are physical output/session checks, separately from numerical canonical PCM comparisons.
There is no microphone/loopback recording or subjective acoustic judgement.

## Failed attempts and retained evidence

An early supplemental slider assertion read 20% before the queued native click was processed;
the immediate failure screenshot already showed 50%. Adding the scenario's existing settled
observation step before assertions corrected the harness without a product change. A later
light-dismiss probe correctly refused to click the underlying Canvas because Avalonia's current
LightDismissOverlayLayer owned the point. The probe now verifies and clicks that exact overlay;
foreground/OS hit guards remain intact. Intermediate harness compilation errors (popup capture
type, an unavailable slider property and a missing input extension import) were corrected.
Failed attempts are distinct from successful application checks.

Raw screenshots, TRX, synthetic project/WAV and isolated preferences/layout remain ignored under
`.artifacts/piano-humanize/`. Rolling retention removed the first full-matrix raw run after its
results were inspected; its durable findings are recorded above. The final successful four-theme
compact captures and JSON are retained locally for requested visual review. No raw reference images,
generated music or captures are product assets. No existing user work was present at startup, and
the Git index/history/remotes were not changed.

## Limits

Offsets are bounded independently per original onset group; neighbouring groups may meet or cross,
and there is no groove, ordering/separation rule, scale lock, MIDI input or melody generation.
Unselected notes never follow selected chord members. Boundary clamping is deliberately asymmetric
near edges. Strengths are session controls, without persisted presets or seed/re-roll controls.
Native tests use an authored fixture; repeat independence is verified canonically, not through a
new native Playlist unique-repeat authoring chain in this task. No native Save/Open picker,
other platform/high-DPI, extended pressure, acoustic/aesthetic owner or prior R4 acceptance is claimed.
