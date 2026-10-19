# SEQ-R10-REPEAT-VARIATION-F1

Date: 2026-10-19. Scope: one explicit Playlist command to detach the pointed repeat, using the
existing document, graph clone, history, renderer and editor navigation.

## Operation and boundary

For index `k` of `N` cycles, the document creates a shared left clip of `k` cycles, a one-cycle
independent variation and a shared right clip of `N-k-1` cycles, omitting empty pieces. Absolute
Pattern ticks determine every boundary. The original Clip ID belongs to the left remainder, or
the right remainder for `k=0`; the center and any second remainder have new IDs. One Edit owns all
cloning and splitting. Unknown identity-bearing content refuses the whole transaction. Undo returns
the original snapshot and Redo returns the accepted snapshot, including generated identities.

MakeArrangementVariation still owns Pattern/Part/note and local graph remapping. Instruments,
immutable PCM, resource IDs, containing processing and Mixer assignments remain shared. Existing
Make variation retains its whole-clip semantics. There is no new clip kind, reader gate, setting,
processor, transport or composition. Official Neon Orchard files remain unchanged.

The context menu captures its actual clip/cycle and document authority. Half-open absolute time
bounds exclude the right endpoint, independently of snap and minimum painted clip width. A binary
boundary search avoids quotient rounding at exact cycle edges. Selection/authority/visibility guards
prevent a retained menu from following another selection or retired document. Success selects only
the center and uses existing preferred-editor navigation and ancestry thumbnails.

## Deterministic evidence

`RepeatVariationTests` has **35 passing cases** (3.715 s CLI for the final focused run). Cases cover
first/interior/last cycles for 2/4/8/1024 repeats, non-bar length 1234567 ticks and nonzero start/lane,
contiguous positions/end, reference/identity ownership, shared-source edits, exact Undo/Redo and reopen.
Fourteen unknown-data/extension/node-version boundaries refuse without partial history or serialization
loss. Invalid/negative/overflowing indices and stale Edit/Undo ABA also refuse.

The audio cases author two Parts, a chord, an overlapping shared clip, a separate held note, non-4/4
meter and fractional tempo, Source/Gain/Mix/Output and optional Mixer. They compare the complete
48-kHz stereo offline Song before/after detachment with maximum sample difference bounded by **1e-6**.
Local source regrouping can change float addition order; bit identity is not claimed. Source and
variation Pattern PCM agree under the same bound. Release tails cross repeat boundaries without
being split or restarted by offline rendering. Canonical note gates extending beyond Pattern length
remain rejected; this task does not introduce cross-boundary notes.

Changing one variation note leaves the original Pattern identical. PCM before its onset and after
its gate plus 40-ms release remains equivalent; PCM inside that local interval changes. A frozen
request captured before that edit still exports the original accepted music. PCM16 Song WAV differs
from the frozen original by at most **one quantization unit**. Save, Save As, reopened canonical
identity/reference equality and reopened Song PCM pass through real managed PCM resources.

Live Song tests publish before the repeated clip and during its first cycle. They verify retained
Song epoch/time and source age for the unchanged left and overlapping clips and unrelated held note,
including the reverse projection on Undo. Remapped sounding identities retire; their past cloned
onsets do not replay. Ordinary off-thread preparation, existing budget/last-valid refusals and
packet-boundary voice mapping remain the only path. Continuity for the detached current note or
renumbered right-side occurrences is explicitly not promised; no global Stop is introduced.

The earlier neighbouring Playlist/Song Structure/Rack/Piano run passes **117/117** (20.473 s CLI).
An initial test-fixture length was shorter than its note and correctly refused validation; it was
corrected to a valid non-bar length. An initial neighbour filter used unsupported interior wildcards;
that run executed zero tests, then the valid filters above passed. Neither is a product regression.

## Actual GUI and physical evidence

The local FL Playlist reference image was inspected before UI edits. The command uses existing MenuItem
styles, materials, selection and editor surfaces; no shared style/field geometry changed.

Run `repeat-native-r5/20261019T052725129-98a6e84738044043a2baab5a8a6ed252` uses real Windows right-click,
popup clicks and Escape for **RU/EN × Dark/Light × 1080/520-DIP floating Playlist**. Every menu is
localized, the long Russian command fits, scroll/zoom select the intended cycle, exact neighbouring
half-open bounds are checked, and Escape changes no music. Actual OS captures were inspected,
including Russian Light at 520 DIP. First, middle and fourth-cycle commands each create one history
entry, select the independent center, open its actual preferred Piano target and pass exact Undo/Redo.
The run then failed in its later harness lookup, which assumed a Rack target after history navigation;
the command itself had already opened Piano. The harness now explicitly reopens the independent clip
using existing double-click navigation before continuing to Piano.

Other attempts remain failures: r1/r3/r4/r6 refused startup because another window covered the title;
r2 stopped at an occluded native context target after two Dark menus. No input was sent through a
failed target guard. These do not invalidate completed r5 checks or establish a complete native run.
Completed native pitch-edit and physical results follow separately below.

Run `repeat-native-r7/20261019T053912683-5da64872594148ab976326c5ed7983ec` then completes the eight-menu
matrix and all three detachment/history cases again, reopens the actual independent clip, enters
pitch **65/F4** through the real Piano exact field and verifies the original Pattern is unchanged.
The inspected OS capture shows that note and the saved `repeat-study.seqvium` retains it. Its later
physical assertion incorrectly treated an already-selected Pattern mode's no-op return as failure,
before calling Play. The harness now treats mode selection and Play as separate results.

The separate `repeat-variation-audio` run opens that native-authored study and passes **1.4 s Pattern**
and **1.8 s Song from the variation's position**, with active renderer/voices, no output failure,
unchanged document and joined Stop. Actual output is **Samsung USB C Earphones, 48 kHz stereo**.
The read-only endpoint snapshot contains no UR12, so no UR12 evidence is claimed. Physical setup in
this route uses workflow APIs; it is not additional native input or long-term audio-load validation.

The separate empty route also renders all four language/theme combinations at 520 DIP and verifies
that an empty Playlist has no repeat-unique command and accepts no musical/history edit. These menus
are routed/rendered checks, separately from the native populated matrix.

Raw screenshots, synthetic projects and logs stay ignored under `.artifacts/repeat-native-*` and
`.artifacts/repeat-*.log` for local review under normal retention. They are not product resources or
public screenshot assets. No acoustic preference, long stress, high-DPI or other-platform acceptance
is inferred from these bounded checks.

## Completion

BUILD **0.1.16.0006** extends the existing variation/Playlist workflow; this is not another standalone
editor or a PATCH milestone. Full Release solution build passes with zero warnings/errors (**49.34 s**).
The sequential full suite passes **1880/1880**, zero skips (**141.342 s CLI**); this full-suite overrun
is recorded rather than described as a focused check. No allocation failure occurs in this run, and
no DSP/threshold/audit-protocol change was made. The final incremental solution rebuild also passes
with zero warnings/errors (1.83 s); subsequent verification-host-only changes compile separately
(final test-project build 17.89 s). Relative links, owner/status/scope review and Git diff checks pass.
Ignored ProjectStats reports are refreshed from the current tool; structural metrics are not test counts.
No staging,
commit, push, tag, history mutation or official composition edit is authorized or performed.
