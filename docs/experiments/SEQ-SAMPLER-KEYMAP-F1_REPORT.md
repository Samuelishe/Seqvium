# SEQ-SAMPLER-KEYMAP-F1 — Multisample Instruments

Date: 2026-10-16. Product checkpoint: **0.1.16.0000**, one PATCH after 0.1.15.0000.
This report owns bounded implementation/verification evidence, not product contracts or owner approval.

## Outcome and decisions

The existing PCM instrument supports identified WAV zones, confirmed Root Note and inclusive key
ranges. Import prepares managed media on workers and publishes all sources/distribution in one edit.
Unknown roots require explicit assignment; filenames are not a pitch detector. Nearest-root distribution
uses midpoint boundaries with ties assigned to the lower root. Overlap is refused; gaps are silent.
Each zone owns its source range, sustain loop and crossfade. Selecting a zone is observation, not an edit.

No parallel audio engine, extra graph node, Mixer route or new composition is introduced. Prepared
notes select their source once and use the existing OfflineSampler kernel in realtime/offline/R8.
Source identity joins step/playback identity when deciding whether a held live voice can continue.
Unrelated voices retain age and transport epoch; changed voices retire without triggering another WAV.
This is not continuous remapping of already-held voices. Reader 1.13 gates nonempty maps only; legacy
single-source PCM retains its format/PCM. Unknown nested intent is preserved but not silently executed.

The original all-keys/technical-form presentation was superseded during owner review. The retained
editor has a quiet full-map overview, adaptive large keyboard, free horizontal navigation and linked
root/zone/key emphasis. A selected root is revealed on source selection and remains visible through
resize, without overriding deliberate panning. Actual internet and local screenshots informed a
single recessed instrument deck, one/two-row source/zone header, bevelled keys and a labeled loop
toggle. Sources include root identity and concise names; full names stay inspectable. Source frame
fields remain in a disclosure while waveform, preview, loop and crossfade remain immediately reachable.
The selector text and popup selection use host theme roles, not framework colors. Comparative notes,
reference names and images remain private local working material; no borrowed graphics are distributed.

## Reproducible verification

SDK 10.0.402, .NET 10, xUnit v3 on Microsoft.Testing.Platform. Release commands:

```powershell
dotnet build Seqvium.sln -c Release --no-restore --disable-build-servers -m:1
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore --max-threads 1
```

The final solution build passes with zero warnings/errors (9.33 seconds). The full suite passes **1739/1739**, zero
skips, one thread (51.847 seconds CLI), 27 more than the preceding checkpoint.
An earlier complete run also passes 1739/1739 (62.703 seconds); intervening failures remain below.
Focused runs include 292/292, 252/252 and the corrected empty Node Editor boundary 41/41 (2.862 seconds).
Final focused mapping/Automation/design/Node Editor verification passes 95/95 (3.559 seconds).
Executed test cases are separate from ProjectStats lexical attribute counts.

- All 128 keys and fractional boundaries use independent source/rate/step signal oracles at 44.1/48 kHz,
  mono/stereo, alongside mapped chords, long loops, release and packet/realtime/offline parity.
- 32-voice success/33-voice refusal and a primed callback allocation check exercise existing budgets;
  all-zone decoded PCM remains bounded to the existing 128 MiB preparation limit.
- Source-specific state, missing media, independent instruments, frozen notes, compatible live updates,
  reader gating/underdeclaration/unknown data, portable Save/Open and replacement are covered.
- Batch import has one Undo/Redo, stale/cancelled refusal, permanent cancelled acceptance and managed
  rollback coverage. R8 retains its frozen local Output tap before Mixer and finite release behavior.

The reusable [visual check](../../tools/Seqvium.VisualCheck/README.md#multisample-key-mapping) opens real
production windows, prepares a five-source piano, exercises exact fields/invalid overlap, selection,
boundary drag, Escape/capture loss, history, removal and portable Save/New/Open. A 26-zone fixture
checks density; it deliberately reuses one recording and is not evidence of 26 independent samples.
RU/EN × Dark/Light ordinary/reduced windows cover collapsed/expanded source fields, selected-root
reveal, free pan, source popup and disabled text. Sixty-four actual field/label centers differ by no
more than 1 DIP. The working scale keeps at least 24 DIP per semitone in this matrix. Compact expanded
source controls remain contained with a usable waveform rather than overlapping preview or footer.
The shared component catalog passes in both themes after popup states are routed to host resources.
The legacy route independently exercises waveform/range/loop/crossfade, cancellation, zoom/pan,
history, empty-state translations, managed persistence and finite R8.

Guarded Windows Root entry and scaled boundary dragging pass in the revised composition. Later
attempts also record covered-window refusal without sending input; this is not native acceptance of
every gesture. Physical default UR12 output at 44.1 kHz stereo passes mapped Pattern playback,
selected-source Hold/Release and joined Stop. The legacy route holds beyond source EOF and joins Stop;
its latest native attempt is covered-window-refused. No subjective listening quality is inferred.

## Library and comparative audio

The supplied home library was inspected without modifying it. It contains 379 working WAVs plus
379 originals, but no FreePats piano. Its verified catalog supplies five soft piano roots (45, 57,
61, 69, 77), with local provenance/license records. No external sound is copied into the repository.
The unavailable requested 26-recording piano cannot be claimed as tested or reconstructed by guessing.

Two local WAVs under the owner's `Documents/Seqvium Resources/Verification/SEQ-SAMPLER-KEYMAP-F1`
remain outside Git: `01-single-sample.wav` and `02-multisample.wav`. Both use the same authored 20-note,
two-bar acceptance fragment through production Pattern rendering: 48 kHz stereo PCM16, 195840 frames,
4.08 seconds including release, 783404 bytes each. The first uses one root-61 source; the second uses
the five-source instrument. Their PCM differs. This short verification fixture is not a new track.
Owner listening/aesthetic approval remains pending.

## Failures, corrections and limits

Initial new tests incorrectly expected wrapper exception types for two preparation failures; assertions
now match the public thrown exceptions without changing production failure behavior. Early GUI checks
revealed root loss during compact resize, dark source text, waveform/footer overlap, an unscaled native
drag and a source popup capture that silently inspected an offscreen virtualized item. These are fixed;
popup capture now requires an attached visible item. The ordinary matrix also waits for resized window
layout before assigning pane bounds, preventing a misleading compact capture labeled as ordinary.

The labeled loop icon initially constructed graphics eagerly: a later full run passed 1732/1739 with
seven empty Node Editor failures. Icon construction is deferred until a real source is observed;
the unchanged 41-case Node Editor suite then passes. Another full run passes 1738/1739, with the existing
Automation volume callback allocation assertion measuring 1184 extra bytes. Its cause is not established;
the assertion is not relaxed and green repeats do not establish pressure/allocation guarantees.

No velocity layers, round-robin, articulations, source-pitch detector, slicing, external plugins,
new song, broad platform/high-DPI acceptance, worst-case starvation/pressure or general R4-F2 closure
is claimed. Raw screenshots/JSON/fixtures remain ignored under the sampler-keymap artifact families;
rotation removes superseded owned runs while preserving protected/unknown evidence. Private reference
study and listening WAVs remain local for owner review. No Git staging/history/remote operation occurs.

Contracts: [audio](../AUDIO_ENGINE.md#multisample-key-mapping-seq-sampler-keymap-f1),
[format](../PROJECT_FORMAT.md#sampler-key-mapping-reader-113),
[UX](../UX_CONTRACT.md#sampler-source-editor), [sample workflow](../SAMPLE_WORKFLOW.md).
