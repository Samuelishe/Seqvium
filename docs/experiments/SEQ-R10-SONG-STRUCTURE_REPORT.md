# Playlist song structure and variations

Role: Evidence for SEQ-R10-SONG-STRUCTURE, product alpha 0.1.7.0000.
Read when: Reviewing section/variation/mixed-selection behavior and the independent musical study.
Authoritative for: Observed local verification, failed attempts and evidence limits.
Not authoritative for: Product contracts, complete R10 acceptance or acoustic judgement.

## Completed scope

Named/color ranges, navigation, adjacent swaps and shared repeat insertion make composition form
editable. Mixed Pattern/Audio selection, captured move/copy, clipboard, duplicate/delete, exact
placement and context actions use one canonical transaction. Crossing clips refuse restructuring
with a visible named cause and direct reveal. Explicit one-clip variations clone supported item
processing and fresh musical identities while preserving sounds/resources/Mixer routes. Ancestry
and shared-use cues survive Save/Open. The actual contracts belong to [UX](../UX_CONTRACT.md#implemented-playlist-song-structure),
[architecture](../ARCHITECTURE.md#song-structure-and-explicit-variations),
[format](../PROJECT_FORMAT.md#playlist-sections-and-pattern-ancestry-reader-15) and
[design system](../DESIGN_SYSTEM.md).

Private arrangement references informed section hierarchy, source relationships, contextual actions,
compact musical grids and waveforms. Their images were studied, never distributed or extracted into
product assets. Existing shared colors/materials/components remain the visual authority.

## Independent Neon Orchard study

The official composition and publication checkpoint are unchanged; a before/after SHA-256 inventory
checks every file beneath the official example. Authoring uses a separate managed-media copy.
The study keeps 112 BPM/4/4, expands 32 to **40 bars**, and has **six sections, 16 Patterns,
207 notes, 104 Pattern clips and one Audio Clip**. The sections are Intro, Groove, Groove 2, Bridge,
Bloom and Coda. Repeat insertion adds eight bars without rewriting original Pattern definitions.
The final keys phrase in Groove 2 becomes `Orchard keys · Turnaround`, with its last upper voice raised
one octave. Its original Pattern notes and source processing remain intact. A private stereo Rhodes
recording contributes a quiet four-second, naturally timed texture in Bloom with explicit fades/gain.
This is an editable study, not a replacement publication or an acoustic approval of the added texture.

Guarded Windows mouse/keyboard and production pickers/controls demonstrate range creation,
Pattern/Audio section swap, exact Undo/Redo, repeat insertion, variation targeting in Rack/Piano,
mixed marquee/drag/context Copy/Cut/Paste/Duplicate and restoration, boundary refusal/reveal,
Save/Open and physical Play/Stop. Reopen checks a new document lifecycle and identical canonical
encoding. RU/EN Dark/Light section fields pass real TextPresenter alignment; native screen captures
include the open popups separately from main-window render captures. Exact Piano properties now
reveal an edited note that leaves its prior view.

A separate native whole-Song pass through the default **48 kHz stereo** output ends normally and
exports production PCM16 WAV. It measures **13 peak voices**, real Mixer activity, **4,114,286 frames
/ 85.714292 seconds**, peak **0.858826**, and **zero clipped frames**. The output file SHA-256 is
`488129af5e33483c81188e699d3a589f9c4539ff6f3a8307b7d0697a24fdd4b9`.
This route does not measure callback allocation/deadline counters or compare every submitted PCM
sample; those claims are not inferred from successful playback. Subjective listening is not performed.

## Deterministic and build verification

Windows x64, pinned SDK 10.0.401/runtime 10.0.12. Locked isolated full Release solution build passes
with **zero warnings/errors**. The final full suite passes **1235/1235, zero skips**, CLI summary
**51.042 seconds**, with `--max-threads 4`. Final product assemblies derive 0.1.7.0000 from the one
root source; project reader 1.5 remains a separate gate. Completed-task cadence resets to zero.

Focused verification initially passes 210 cases, then 256 cases with additional boundaries.
New cases cover gap-preserving swaps, ripple copies, cross-boundary Pattern/audio refusal, group
clamping and frozen clipboard geometry, opaque-state safety, graph remapping, weak ancestry after
source deletion, stale/ABA workflow authority, equal PCM before a variation edit, isolated changed
PCM afterward, shared Mixer intent and canonical Save/Open. Component catalog review passes.
Ordinary focused build/test phases stay below 120 seconds; the native authoring/full-Song routes are
separate requested L4 work. The final full build takes 9.05 seconds; the final incremental test build
takes 3.33 seconds, excluding CLI overhead.

## Failed attempts and corrections

- Initial indirect launches lose foreground or find the check window covered. Exact foreground/hit
  guards refuse input; normal shell launch reaches the native scenario. No unrelated application
  receives musical input, and no persistent TopMost/system preference is introduced.
- The authoring harness initially includes detached popup fields, misses menu items outside the
  main visual tree, targets an unfitted Piano note, and observes Open before a replacement lifecycle
  is ready. Attached-popup lookup, actual menu controls, Fit and lifecycle/enabled-state waits repair
  the harness. Final evidence uses native events, not replacement model mutations.
- A synthetic Shift+Up attempt produces a semitone, rather than the intended octave. The final
  musical edit uses the production exact-pitch properties. This attempt does not establish a physical
  keyboard regression. Image review also exposes the need to reveal the note after exact edits.
- One old allocation run observes 32 extra bytes and passes separately. Another existing test measures
  assertions inside its callback loop and reports 5888 extra bytes. Assertions are moved outside the
  measured interval, retaining the exact zero-allocation requirement; the full final suite passes.
- The first full suite fails a new ancestry fixture that deletes a definition without its placements;
  the fixture is repaired using the existing explicit deletion contract. An unrelated retention test
  encounters its bounded process-snapshot timeout and safely protects all outputs; it passes in
  isolation. Bounded suite concurrency gives the final green run without weakening retention safety.
- An additional old Audio Properties review stops on a transient dismissal overlay, then loses
  foreground in subsequent attempts. The strict guards send no input to the occluded targets.
  Temporary harness-only dismissal changes are removed. The new section field matrix is green;
  this additional reduced Audio Properties matrix is not claimed as completed.

## Retention and limits

The independent study/project-managed media and exported WAV are delivered outside Git under the
owner's Documents/Seqvium Projects directory. Ignored `.artifacts/song-structure/` retains the current
build, final test report, native screen/render evidence and compact raw observations for review.
These are local artifacts, not reproducibility links or distributed musical resources. Superseded
task-owned copies may be removed after delivery; reusable checks and permanent regressions stay in
`tools/` and `tests/`. No staging, commit, push or tag is performed.

Sections remain non-overlapping organization rather than processing containers/arranger chains.
Crossing sustained phrases require explicit split/range adjustment. Variations are independent notes
with shared sounds/routes, not inherited note overrides or automatic merge-back. Internal repeats
share one Pattern; per-repeat detachment requires separate clips. Tracks remain 32 fixed slots;
dynamic groups/collapse, stretch, automation and recording remain future work. Resident-media limits,
wider pressure/platform/high-DPI/accessibility evidence and owner visual/acoustic acceptance remain
in their existing owners. A green musical study does not close those gaps.
