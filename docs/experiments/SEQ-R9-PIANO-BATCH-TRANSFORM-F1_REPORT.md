# SEQ-R9-PIANO-BATCH-TRANSFORM-F1 — Relative Note Transform

Date: 2026-10-19. Clean `master` baseline `36276d7`, product 0.1.16.0009.
Checkpoint: **0.1.16.0010**, one BUILD within the existing Piano editor. Windows x64,
.NET SDK 10.0.402/runtime 10.0.13, Avalonia 12.1.3, xUnit v3/MTP.
This report owns evidence; [UX](../UX_CONTRACT.md#implemented-seq-r9-piano-roll) owns behavior.

## Implementation and scope

Transform joins Quantize/Humanize in the existing Piano tool group. A retained shared Flyout has
three exact numeric rows with visible units: integer Transpose −24…+24 semitones, Duration and
Velocity 50…200%, initially 0/100/100. Percentages accept decimal values. Each opening starts neutral.
The compact composition uses standard parameterRow/NumericUpDown and Apply/Cancel without another
slider group, permanent parameter strip, window, command, preference or note store.

Only Apply submits frozen occurrence/generation/lifecycle/Part/selection authority. Numeric drafts
do not edit music; invalid or fractional transpose input disables acceptance. Cancel, Escape and
closure accept nothing. Safe observations preserve the actual Popup, draft controls and focus.
Busy/Closed, pane cancellation, changed selection/Part/target and unsupported musical intent retire
or refuse acceptance. Queued observation handling retains intervening target/Busy invalidation.

The existing PianoNotes projection scales each selected note independently, then the serialized
workflow validates its existing basis and accepts one SetPartNotes transaction. Decimal duration
arithmetic rounds midpoint away from zero and floors at one tick; bounds are checked before Int64
conversion or end addition. Fractional Pitch remains intact. Intensity multiplies its own original
value and clamps to [0,1]; saturation can reduce differences. An invalid pitch or Pattern end refuses
the complete group. Starts, IDs, canonical order, unrelated notes/Parts/Patterns, sources, processing
and existing relationships remain exact. Defaults and actual no-ops retain revision/history/Redo;
accepted Undo/Redo stores exact results. Repeated Apply acts on current accepted notes.

Shared uses receive the common Pattern edit; explicit whole-clip variations and independent repeats
retain their existing content ownership. There is no reader/schema, DSP, audio publication or transport
change. Root Pitch, Snap, Quantize, Humanize and Scale behavior remain separate.

The owner's additional pitch-caption defect is fixed in the same checkpoint. Fixed text origins
misplaced/clipped names containing the bundled sharp fallback, whose line metrics differ from the
ordinary font. Note and keyboard captions now center actual glyph ink, retaining shared typography
and leaving the note intensity stripe clear. Actual local reference screenshots were inspected;
no reference graphics or private research assets are distributed.

## Automated verification

```powershell
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class '*PianoTransformTests' --filter-class '*PianoHumanizeTests' --filter-class '*PianoToolsTests' --filter-class '*PianoRollTests' --filter-class '*RepeatVariationTests' --filter-class '*UnfoldRepeatsTests' --filter-class '*StepSequencerTests' --filter-class '*PatternEditorNavigationTests' --filter-class '*WorkflowPresentationTests' --filter-class '*HostPresentationTests' --filter-class '*ProductVersionTests' --filter-class '*DesignSystemTests' --max-threads 1 --report-xunit-trx --results-directory .artifacts/piano-transform/focused-final
```

The affected Release build passes with zero warnings/errors. The focused run passes **309/309**,
zero skips, **103.543 s CLI**, including **42** Transform cases. The associated build took 41.70 s;
the combined interval exceeded the 120-s target, so no automatic wider suite was added. After final
tooltip, stronger reopen assertions and harness-selection changes, the final affected build passes
in **15.54 s**; Transform/HostPresentation/ProductVersion rerun passes **93/93**, **17.936 s CLI**.
These reruns overlap the 309 cases and are not another 93 distinct tests.

Transform cases verify independent and combined exact values, unequal lengths/velocities, fractional
pitch/percentages, ±24/50/200 endpoints, midpoint rounding, one-tick gates and long.MaxValue arithmetic.
They cover complete boundary refusal, identity/order/locality, one Undo/exact Redo, no-op/generation/
Redo retention, repeated Apply, portable Save/Open, shared uses, independent whole-clip/repeat content,
stale/ABA/lifecycle/wrong Part/missing IDs/other occurrence, Busy/Closed and unknown Pattern/Part/note/sound.

Live Pattern/Song tests retain epoch and unchanged held music through Apply/Undo/Redo. The numerical
comparison spans 160 packets through the changed onset, gate and release, against the accepted new
canonical state. Complete finite stereo Pattern/Song PCM after portable reopen matches realtime at
44.1/48 kHz within **1e-6**, using different packet sizes. This intentionally does not require equality
between music before and after Transform. Neighbouring tests protect the existing publication,
repeat, Piano/Rack, presentation/design and built 0.1.16.0010 metadata contracts.

This is L0/L1/L2. No shared renderer/schema change or cadence trigger justifies automatic L3;
cadence advances from two to three tasks since Piano tools F1's full Release baseline.
Final static checks validate 773 local Markdown destinations/anchors in affected documents and a
clean `git diff --check`. Ownership/status/scope review keeps UX/design contracts, reader guidance,
current state, version ledger and historical rationale in their respective owners.

## Actual window, input and physical checks

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-transform .artifacts/piano-transform/acceptance audio --timeout-seconds=180
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll piano-transform .artifacts/piano-transform/selection-final quick audio --timeout-seconds=120
```

The full real-window run passes **12 combinations**: RU/EN × Dark/Light × 1080/520/360-DIP Piano
panes, including populated flyouts and empty Parts. Actual TextPresenter/label center differences
are **0 DIP** in every field, with 26-DIP fields and 16-DIP text. Toolbars wrap at reduced widths;
the compact Popup retains all three fields/actions. No instructional paragraphs or technical counters
were added. The separate C4–D5 chromatic label matrix passes eight language/theme/1080/360 combinations
and 120 glyph-containment measurements, including sharps. Captures were visually inspected in both
themes, including reduced Russian and ordinary English compositions.

Explicit routed actions on real controls check independent exact fields, unchanged drafts,
Apply/Cancel/Escape/closure/defaults, fractional/out-of-range rejection, one Undo/exact Redo, Free Snap,
stale generation, Busy save, Part ABA and hide. The final quick rerun additionally confirms that changing
the selected note set closes the old draft without editing music. The full run takes 75.06 s.

Physical Pattern and Song output through **Samsung USB C Earphones, 48000 Hz, stereo** passes with
routed Apply/Undo/Redo. The current Popup/focus survives a 1600-ms playing observation window.
Pattern remains playing/looping at epoch **2**, progressing **5280→123840** frames; Song remains
playing at epoch **5**, progressing **4320→122880** frames. Each intermediate Apply/history snapshot
retains the same epoch and no device failure. The final selection rerun also passes physical output.
This is backend/session delivery evidence, separate from deterministic PCM; no microphone/loopback
capture or subjective listening was performed.

Guarded Windows native input initially reached chord selection, exact integer fields, Cancel/Escape
and neutral Apply. That run failed at the harness's locale-dependent decimal key entry before
nonzero acceptance/history. The driver now uses its existing Unicode exact-entry helper. Subsequent
native runs were refused because another window covered the activation point; no activation/action
input was sent in those attempts. **A complete native nonzero Apply/history or native layout matrix
is not claimed.** Routed real-control and physical checks remain explicitly separate.

## Failed attempts and diagnosis

The first routed selection probe raised a ToggleButton Click without changing its checked state;
explicit routed fixture selection corrected the driver, without a product change. Two later valid-draft
assertions failed during exploratory runs; a debugger run at the same assertion observed Apply enabled,
matching basis generation 5, idle workflow and completed command. Successful full/final reruns are
recorded separately; those failed attempts are not counted as passes.

An early full-PCM test retained the pre-Open closed document; it now prepares and compares the actual
reopened workflow document and asserts exact accepted content. New test compilation also caught the
required cancellation-token analyzer and differing Prepare parameter names; these were corrected.

Initial physical runs ended before Apply. Rider debugger evidence at OfflineSampler.cs:430 shows
PreparePatternCore on a worker, **loopable=false**, **zero=1**, **last=100800**, sample rate 48000.
The authored three-tick note is below one output frame and selects the existing finite fallback.
Phase records confirm the initial plan was already non-looping and naturally ended before Apply;
subsequent publications of a stopped plan legitimately change epoch. The physical fixture now uses
a 120000-tick gate for that note, while exact 3→5-tick math/GUI checks remain. No production audio
code was changed. An exploratory debugger pause during physical preparation also produced Starvation;
paused audio is not device acceptance. Agent breakpoints/sessions were removed/stopped; user breakpoints
were left untouched. Configured run settings and user processes were not changed.

## Retention and remaining limits

Current requested review screenshots/JSON remain ignored locally under
`.artifacts/piano-transform/acceptance/20261019T133017864-b782025392734e3d9b76ac7df29abc4c/`.
The final selection/input/physical run is under `.artifacts/piano-transform/selection-final/`;
TRX and native-refusal evidence share the same task root. These are review artifacts, not application
assets or durable public image links, and follow rolling retention. Public source consists of the
implementation, regression tests, reusable VisualCheck scenario and documents.
Superseded task-only raw runs also remain ignored locally: automatic approval review rejected the
scoped recursive cleanup command with `blocked by policy`, without a more detailed reason. No
alternate deletion was attempted. The successful matrices/final checks are the current review set;
older failed raw runs are disposable under the ordinary ownership-aware rolling retention policy.

Full native acceptance, other OS/DPI, extended stress, subjective sound/aesthetic approval and earlier
R4 pressure gaps remain unclaimed. Sub-frame playback keeps its pre-existing finite limitations.
Velocity saturation and tick rounding/floor can reduce relative differences by design. There is no
Scale Locking, MIDI, controller lane or wider Piano redesign. The missing 0.1.16.0008 ledger row is
restored from its unchanged original checkpoint/report; only this checkpoint consumes BUILD 0010.
Startup Git was clean; all final task changes remain unstaged. No stage/commit/push/tag/history or
remote operation, user-file deletion or official composition modification was performed.
