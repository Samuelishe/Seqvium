# SEQ-RACK-WORKFLOW-F1 — channels and Pattern Gain

Role: Completed checkpoint evidence and remaining verification limits.
Audience: Developers reviewing channel identity, graph/history safety and actual interaction evidence.
Status: Implemented bounded workflow, product **0.1.16.0003** (one BUILD).

## Outcome and boundaries

Rack actions offer independent Duplicate for supported Drum, Synth and PCM sounds, including key
mapping. Name drag reorders; Ctrl at drag activation copies into the marked gap. Each accepted
operation uses one existing document transaction. New sound/Part/note/zone/Source/port/binding/cable
identities are distinct. Immutable WAV resources and the explicit Mixer channel destination stay
shared. Sound automation remains targeted at the original identity; Mixer automation still addresses
the shared route. No processor bank, automation lane or global preference is manufactured.

Duplication retains the current safe unshared Rack tree and eight-Source limit. Unsupported graphs,
opaque copied intent and unavailable sound semantics refuse with a localized explanation. Reorder
only changes durable Parts order. Its audio-equivalence comparison ignores that presentation order,
while canonical equality, Undo and JSON retain it. Save/Save As/media retention use existing owners;
no format field, reader gate, recovery subsystem or second engine is introduced.

Pattern Gain combines the shared GainLevelControl's sculpted Fader variant with exact input and
Left/Right/Home/End. Linear 0–1 semantics are unchanged. Drafts are visual; release publishes through
existing live Gain. Escape/capture loss/stale authority cancels. Narrow toolbar overflow retains the
same controls, and visible Graph/detailed Gain surfaces keep parameter priority. Legacy `1.1251`
opens unchanged, shows a critical exact field and no legal fader thumb, and supports repair/Undo/Redo.

## Automated verification

Baseline was clean `master`, HEAD `404383af86021a0fb7057b8ecd986285c7138231`, version 0.1.16.0002.
Environment: Windows 11 x64, .NET SDK 10.0.402, net10.0, MTP/xUnit v3, one test thread.
Existing assets/locked dependencies were reused; no restore inputs changed.

```powershell
dotnet build Seqvium.sln -c Release --no-restore -m:1 -v:q
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1
```

Final full Release build: **6.06 s**, zero warnings/errors. Final full suite: **1820/1820**, zero
skips, **74.088 s CLI** (73.945 s runner). An earlier full pass was 1820/1820 in 66.178 s. Nineteen
new cases cover configured/preset Drum/Synth/PCM copies, original/copy editing independence in both
directions, changed audible samples, exact notes and fresh identities, doubled source signal and
bit-identical reorder at 44.1/48 kHz, mapped source/range/loop/crossfade settings, unknown/capacity/
shared/stale/ABA refusal, atomic history and portable media after external-source deletion. Active
Pattern/Song with/without Mixer protects clock, prepared plan and held voices through reorder and
compatible Gain. Existing DSP/Graph/Project/Mixer/Automation/R8 regressions remain in the full suite.

Focused Rack/DesignSystem/DesktopFoundationGesture/PatternEditorNavigation follow-up: **31/31**, **2.177 s CLI**, zero
skips. Test fixtures were strengthened to use altered presets/parameters and
legal copied timbre edits before the final full run. A final readout-only refinement preserves accepted
fractional precision instead of rounding the field to two places. It passes an affected Release build (8.72 s, zero
warnings/errors), those 31 focused checks and actual-window/native comparison of exact
text against canonical Gain. It changes no DSP. Subsequent tooling readiness/layout/assertion changes
rebuild in 3.22 s, zero warnings/errors; the full engine/Project regression above is reused.

## Actual windows, native input and physical output

VisualCheck `rack-workflow ... native audio --timeout-seconds=180` passes guarded Windows menu
Duplicate, name reorder, Ctrl-copy insertion, Undo/Redo, drag Escape, exact Source-editor double-click,
fader drag/cancel and exact numeric input. Explicit routed probes separately protect micro movement,
capture loss, outside drop, changed generation and Undo/Redo ABA. Creation, musical fixtures, pane
geometry, preferences, persistence and legacy/keyboard probes are programmatic/routed setup, not
claimed as fully native musical authoring.

The native run and final routed review render RU/EN × Dark/Light at 1400×900, 1000×700 and 640×700
DIP, scale **1.0**, with 850×390 or compact 440×390 requested Rack frames (clamped by workspace).
They include long names, populated/empty states, insertion line, overflow and invalid Gain. Actual
screenshots were inspected for contrast, cap depth, text containment, musical density and accessible
compact controls. Twelve toolbar field measurements have **0 DIP** center difference. Empty windows
show no fictitious Gain target. Final review settles the resized canvas before assigning pane bounds.

The shared catalog passes both themes, disabled/invalid faders and NumericUpDown's actual critical
inner-field border, plus resource propagation/restoration. The extended workspace/Graph/Gain matrix
passes 32 field checks (maximum **1 DIP**), retained hierarchy, routed gestures and native graph
double-click/movement/Escape/keyboard checks at zoom 0.5/1/2.

Actual physical output is **Line (Steinberg UR12), 44.1 kHz, stereo**. A short isolated three-hit Snare
Pattern plays while accepted Gain and row order change. Transport epoch stays **2**, render position
advances to **22,491** frames, and the output reports no fault. Stop and final New retain the existing
joined output lifetime.
This establishes bounded physical delivery/continuity, not subjective listening or stress acceptance.
No composition, original sample library modification or system-volume/device preference change occurs.

## Failed attempts, corrections and retained evidence

- The first physical reorder reset transport. Parts order was still participating in execution
  equality; normalization at the audio comparison fixes the reproduced cause. Six active-plan
  regressions and the later UR12 pass protect it.
- One native attempt missed menu acceptance without a captured original cause. The harness now
  acknowledges the actual menu Click before checking command completion. A subsequent attempt was
  safely refused because the cursor moved before press. A later covered-startup attempt also sent no
  input; the harness now explicitly activates its own window and awaits foreground readiness before
  retaining all hit-test guards. These refusals are not counted as passes; the final native run passes.
- The broader matrix initially failed its second graph double-click after a specialized Source
  editor expanded over that graph. Its layout fixture now hides the editor before further graph
  input; the final matrix passes. Production editor placement is unchanged.
- Screenshot review found missing visible invalid-number styling and a pane-size fixture race.
  Shared critical numeric styling and settled window layout correct them; final catalog/review pass.

Evidence remains ignored beneath `.artifacts/rack-workflow/`, notably `native-precision-ready`,
`precision-final`, `hierarchy-final`, `catalog-verified` and the latest retained failure. JSON distinguishes native,
routed, physical and rendering scope. Existing automatic retention rotates eligible generated captures;
protected/unknown evidence remains, with its soft-budget advisory. Product/test/tool source and user
resources are retained. Local reference images were inspected and remain outside public artifacts.
The one final native/UR12 run has an explicit preservation reason until owner interaction/visual
review, because later ordinary render runs must not rotate away its richer receipts. Superseded
captures were rotated under the existing policy and can be regenerated with the retained scenario.

Owner aesthetic/acoustic approval, high/mixed-DPI/other-platform execution, maximum-load pressure,
unknown extension cloning, arbitrary custom graph copying and shared-Pattern channel expansion are
not established. Existing R4 pressure gaps remain open. No Git index/history/publication operation
was performed. Current behavior belongs
to [UX](../UX_CONTRACT.md#channel-duplication-order-and-pattern-gain-seq-rack-workflow-f1),
[graph](../NODE_GRAPH.md#r5-f1-channel-composition), [architecture](../ARCHITECTURE.md#r5-f1-musical-editor-composition)
and [format](../PROJECT_FORMAT.md#channel-copies-and-row-order).
