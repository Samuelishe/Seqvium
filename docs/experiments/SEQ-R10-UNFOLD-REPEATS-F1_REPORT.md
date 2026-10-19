# SEQ-R10-UNFOLD-REPEATS-F1 — Unfold Pattern repeats

## Scope and decision

Implemented on 2026-10-19 from a clean `master`, product `0.1.16.0008`. One BUILD selects
`0.1.16.0009`: this extends the existing Playlist editing capability, without a new musical model,
reader/schema, DSP, engine, dependency or settings. No stage/commit/push/tag/history mutation.

The action expands one pointed Pattern clip with 2–1024 repeats into contiguous ordinary clips.
It retains the first Clip ID, source placement, lane and exact integer cycle boundaries. Later IDs
are accepted once and retained by Redo. Array insertion retains surrounding order. An existing
variation and its ancestry remain shared. No Pattern, Part, note, sound, graph or resource is cloned.
The existing Make variation supplies independence afterwards; repeat uniqueness remains separate.

Reusing Make this repeat unique was rejected because that action intentionally clones musical
content. Unfold instead reuses canonical ArrangementClip/PatternPlacement, ProjectDocument.Edit,
history, persistence, Workflow command serialization and ArrangementEditBasis. Checked full-span
validation precedes bounded allocation. Opaque clip data refuses atomically; shared source data that
is neither split nor interpreted stays unchanged. No artificial lower repeat limit is introduced.

The context menu uses existing shared MenuItem styling and localization, with a short shared-note
tooltip. It captures the pointed clip independently of a possible wider selection. Acceptance selects
the first result. Dismissal, selection notifications, hide/cancellation, Busy and lifecycle/generation
changes invalidate the menu; stale saved MenuItems cannot edit after a subsequent reopen/Undo ABA.

## Environment and reproducible checks

Windows x64, .NET SDK **10.0.402** (repository global.json 10.0.401/latestPatch), runtime **10.0.13**,
Release, xUnit v3 / Microsoft.Testing.Platform. Existing restored dependencies were reused.
No full L3 was selected: this changes a bounded canonical edit and contextual UI, not DSP/schema;
the prior Piano tools L3 remains current, with cadence advancing from one completed task to two.

```powershell
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class '*UnfoldRepeatsTests' --filter-class '*RepeatVariationTests' --filter-class '*SongStructureTests' --filter-class '*ArrangementTests' --filter-class '*AudioClipTests' --filter-class '*HostPresentationTests' --filter-class '*ProductVersionTests' --max-threads 1
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll unfold-repeats .artifacts/unfold-repeats/final-routed --timeout-seconds=240
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll unfold-repeats .artifacts/unfold-repeats/native-review native --timeout-seconds=240
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll unfold-repeats .artifacts/unfold-repeats/physical-review audio --timeout-seconds=240
```

## Source, tests and numerical PCM

The affected Tests build includes Core, Windows audio, Desktop, VisualCheck and ProjectStats
references and has zero warnings/errors. The final versioned build took **22.95 s** (a preceding
pre-version build took **32.72 s**). Final serial L1/L2 is **172/172**, zero skips, **24.290 s CLI**;
the preceding serial pass was also 172/172 in 24.723 s, and the new-only pass is
**20/20**, **14.607 s CLI**. These are focused results, not a full regression suite.

The twenty new cases cover 2/5/1024 cycles, first/middle/last exact placement, 1,234,567-tick Pattern
length, 7/8, nonzero start and the exact `long.MaxValue` end boundary. They verify first/new IDs,
stable surrounding order, no source creation, normal/existing variations and their ancestry,
individual move/copy/variation/note locality/delete, one Undo, exact accepted Redo and canonical
reopen. Invalid counts/time/size, opaque occurrence data, missing target, stale generation/Undo ABA,
different lifecycle and unavailable closed editing refuse without a partial snapshot/history edit.
An unchanged opaque Pattern source is preserved rather than unnecessarily rejected or cloned.

Audio cases use authored PCM, overlapping chords and a held unrelated voice, coincident shared
Pattern clips and an overlapping Audio Clip. They include a source placement offset that must not
offset Song time, local Gain, 80-ms releases across cycle boundaries, Mixer routing and Delay tails.
Both 44.1/48 kHz and mono/stereo compare complete canonical offline Song before/after Unfold,
realtime packets against offline before/after, musical event frames/ticks, exact Redo and portable
Save/Open. Offline packets use 251 frames, realtime 256. Absolute sample error is asserted at
**≤ 2e-6**; lengths, on/off geometry and all unrelated canonical content remain equal.

Live owner publication uses existing PreservePlayback. Cycle-zero voices can retain the unchanged
clip/iteration identity; currently sounding later-cycle identities retire instead of replaying
their past onsets. Other chord/held voices retain their source ages, and position advances exactly
one packet without a new epoch. Undo uses the same publication path. This does not extend the
engine's continuation promise to remapped voices, arbitrary topology or stateful local processors.

The initial focused run had eight failures: six new-fixture issues (negative-position exception
type, missing mono graph layout, mismatched hard/smooth execution boundaries, and directly closing
a workflow-owned document) plus two existing strict Arrangement allocation measurements
(1664 and 32 bytes). Fixture corrections keep the PCM tolerance and production behavior unchanged;
the subsequent serial pass keeps the strict allocation assertions intact. Green repetition neither
attributes those initial observations nor closes the existing allocation/pressure audit. Early
compiler/analyzer failures involved a missing test namespace and cancellation-token call sites;
all were corrected before green builds. Restore/build/test costs were separate; verification
iterations and actual GUI/device routes are additional to the focused serial timing.

## Actual windows, input and physical output

Local FL Playlist and Ableton split-result reference images were inspected before editing. Only
the existing context-menu composition is extended; no shared style, field, token or toolbar is added.
Actual renders were reviewed for localized text, clipping, selection and consecutive clip geometry.

The final routed GUI scenario passes eight populated RU/EN × Dark/Light × 1080/520-DIP cases,
including actual open context menus, cancellation, acceptance, first-result/individual selection,
single-repeat disabled state and exact Undo/Redo. It also passes eight empty combinations, individual
drag Move, clipboard Copy/Paste, subsequent independent variation, existing preferred-editor target,
Busy Save invalidation, hide/reopen, selection retargeting and generation/Undo ABA refusal. API-driven
fixture/theme/layout/history setup is separate from routed mouse/menu input. The route's **29.71 s**
success is recorded in its manifest. The physical-review route repeats this matrix successfully.

Two guarded native Windows attempts were refused before action input because the test window was
covered. The second waited **45 seconds** for foreground and timed out. This is **not native mouse
acceptance**. No input was injected through a covering application. Routed input and rendered
popups do not establish native menu event ordering, full native authoring or accessibility acceptance.

The first optional physical attempt failed a combined variation Undo/Redo epoch/device assertion;
that attempt did not record enough fields to distinguish device failure from epoch change and is
not counted as a pass. The final addressed live **Unfold** check through **Samsung USB C Earphones,
48 kHz stereo** passes: epoch **3 → 3**, render position **43680 → 68640**, two active voices,
Playing, no termination/device failure or workflow detail. The executing revision differs from the
saved pre-Unfold study. This is short physical delivery/continuation, not full-song EOF, captured
DAC comparison, acoustic judgement or a replacement for canonical PCM tests.

Subjective listening was **not performed**. Other devices/platforms, high/mixed DPI, maximum-pressure
stress and general R4-F2/allocation acceptance remain outside this checkpoint. Full native input
remains an explicit evidence gap; no claim of all requested acceptance criteria being established.

## Retained evidence

Reusable source lives in UnfoldRepeatsTests and VisualCheck's `unfold-repeats` route. Generated
screenshots, isolated projects/config, JSON and failure records stay ignored under
`.artifacts/unfold-repeats/`, for local review under normal retention:

- `final-routed/20261019T115129185-b3d4618021a843b3a4d319a03b059ed7`: successful full menus,
  before/after/empty renders, history/interaction matrix and portable study.
- `physical-review/20261019T115220928-bc2bdfd60c284647a6ec7044cdbf835f`: successful repeated matrix,
  short device delivery and `physical-observation.json` with actual status/counters.
- `native-review/20261019T114932425-26b1dba83e2f465789633edf61dda8e3`: startup capture and bounded
  foreground refusal. Earlier ordinary outputs may rotate under the existing retention policy.

These paths are local evidence, not distributed assets or guaranteed clone contents. No user media
was imported or modified. Final untracked product/test/tool source is intentional, not disposable.
The ignored root ProjectStats Markdown/JSON reports are refreshed after final source/docs edits and
inspected separately; structural/lexical counts are not executed test totals.
