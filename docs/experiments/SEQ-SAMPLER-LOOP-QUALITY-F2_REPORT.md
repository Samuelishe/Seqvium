# SEQ-SAMPLER-LOOP-QUALITY-F2 — Musical Sustain Loop Crossfade

Status: implemented and verified within the bounded existing PCM instrument.
Date: 2026-10-16. Base: `dd3b59a`, `0.1.13.0000`. Result: **0.1.13.0001**, one BUILD.
This refines the existing capability; it does not add another sampler/editor or qualify for PATCH.

## Musical behavior and period

Positive Crossfade replaces the F1 seam gain dip with a complementary smoothstep overlap.
For explicit loop `[A,B)` and width `X`, `[B-X,B)` mixes with the original pre-loop
`[A-X,A)`. Both reads retain the existing pitch/rate step. Incoming weight is
`t²(3-2t)`, outgoing weight `1-incoming`, for `t=(cursor-(B-X))/X`.
Weights sum to one and have zero slope at their endpoints. This preserves constant/correlated
signals without the equal-power boost; arbitrary phase opposition can still cancel.

The existing absolute-age cursor wraps B to A, with period **B-A**, including the first wrap.
It never skips the already mixed head, shortens the loop to B-A-X, replays a shifted head,
accumulates a fractional cursor or moves a marker. The pre-loop handle is an explicit second
read of existing audio, shown on the waveform; the prefix still plays once on note entry.
Primary interpolation holds its final valid frame for positive crossfade, and the incoming
head interpolates through A. Neither reads outside the selected range or past Loop End.

Width is exact source frames: **0..min(4096, half loop length, Loop Start minus Sample Start)**.
4096 frames is about 85.33/92.88 ms at 48/44.1 kHz. This bounds processing and leaves at least
half the loop outside overlap. A loop beginning at Sample Start needs explicit pre-loop room
before positive crossfade is available; no hidden marker or width correction supplies it.
Four-frame loops allow up to two frames with sufficient handles. A one-frame overlap or a
large transposition can offer little audible smoothing. No anti-aliasing, phase alignment,
DC removal or universal seamlessness is promised.

**Zero/omission preserves exact F1 PCM**, including the short gain dip and its interpolation.
It does not select a newly reinterpreted raw seam. The listening study's “F1” and “off” files
are deliberately byte-identical. Disabled Loop ignores the validated retained crossfade.
NoteOff/release, root pitch, velocity, independent voices and Pattern time remain unchanged.

## State, editor and lifetime

The existing shared SoundDefinition owns optional integer `loopCrossfadeFrames`. Canonical zero
is omitted; presence requires reader **1.10**, including externally supplied zero/invalid values.
Documents without it retain previous gates. The complete source-range block is still required.
Unknown/partial/fractional/out-of-bounds state is preserved and unavailable, not silently repaired.
Ordinary Undo/Redo and portable managed Save/Open apply. Replace WAV removes crossfade together
with range/loop; Undo restores source and intent atomically. No resource/schema copy is added.

Sampler Source Editor adds one standard exact field and complementary ramps over both actual
overlap areas. Local scrolling/reflow and the existing shared colors/metrics remain in use.
Invalid widths receive an actionable error; pointer proposals cannot invalidate handles.
Existing Channel Rack and bound Source double-click routes target the same singleton pane.

PreparedNote carries immutable width; the existing OfflineSampler performs both reads with no
new callback allocation, source lease or executor. Contextual Sample Lab audition clears the
old source-specific width, and replacement uses the same reset keys. Compatible live changes
retire affected voices on publication, preserve clock and unrelated voices, and wait for the next
scheduled note. Continuous held-note morphing/retrigger and transport restarts are not added.
R8 remains frozen local Output before Mixer, bounded by musical NoteOff/release and existing tails.

## Deterministic and Release verification

SDK 10.0.401, MTP/xUnit v3, Release, one test thread:

```powershell
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests -c Release --no-build --no-restore --max-threads 1
dotnet test --project tests/Seqvium.Tests -c Release --no-build --no-restore --filter-class '*SamplerEditorTests' --max-threads 1
```

- L0/L1/L2 and risk-selected L3: final production Release solution build **36.06 s**, zero
  warnings/errors. Full suite **1655/1655**, zero skips, **75.180 s CLI**.
- Final test-only extension adds four crossfade/rate realtime/Seek cases; affected Release
  rebuild **3.04 s**, zero warnings/errors, followed by **134/134 sampler cases**, **3.896 s**.
  Current suite therefore contains 1659 cases; a second full suite after those four additions
  was not run. Production binaries are unchanged after the green L3.
- Final live-publication refinement isolates a **crossfade-width-only** change during a held note,
  preserving the same source/loop markers. Zero-warning test rebuild **7.20 s**, sampler
  **134/134** again (**3.819 s**), with no transport restart or held-note retrigger.
- Independent ramp/source-phase PCM oracles cover widths 1/24/48, source and output 44.1/48 kHz,
  mono/stereo, pitches 0/48/60/72/127, release and multiple wraps. Different packet partitions
  produce identical PCM. Four-frame constant signals at fractional/native/extreme step retain
  gain without new DC or overshoot. A matched periodic tone retains its source phases across
  hundreds of seams without drift or added DC.
- Shared production realtime/offline packet parity, warm zero-byte callback measurements,
  Pattern loop, Stop/Panic, Song Seek/release, stale publication, changed/unrelated voice age,
  independent polyphony/capacity and held-preview retirement pass for F1 and positive widths.
- Replacement/history, selective gate, malformed state preservation, portable Save/Open and
  finite R8 versus Song with serial Delay/Reverb pass. R8 excludes Mixer and WAV quantization
  retains the existing tolerance. No R4-F2 allocation-pressure or starvation gap is closed.

Recorded corrections: the first command used nonexistent `.slnx`, then the actual `.sln`.
First focused run was 125/126 because its new long note exceeded fixture Pattern length;
the fixture was corrected. First full run was 1654/1655 in **124.664 s**, just beyond the
120-s focused target: the old unsupported-version fixture still rejected newly supported reader
1.10. It now checks 1.11; the complete repeat is green. These were verification fixture failures,
not suppressed production failures. Builds/tests used ordinary workload, not synthetic saturation.

## Actual GUI and comparison WAVs

The reusable `sampler-editor` VisualCheck exercises the real window:

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sampler-editor "$PWD/.artifacts/sampler-loop-quality-f2/review-02" native audio
```

Both passes verify RU/EN × Dark/Light, populated/empty 1400×900 and 800×600 main windows,
760/480-wide retained editor, exact field entry/refusal, one edit per Apply, drag/cancellation,
zoom/pan/Fit, Undo/Redo, preview ownership and portable Open after deleting the original WAV.
**40 field alignments have zero DIP difference**. Actual rendered ordinary/reduced views were
inspected against the local sampler reference library; both overlap ramps and the fifth exact
field remain usable without instructional paragraphs. Native Windows exact Start/Crossfade/Apply
and marker drag pass, with routed setup clearly separated.

Final physical Samsung USB C Earphones **48-kHz stereo** Hold past source EOF, bounded Release
to zero voices and Stop/join pass without canonical edits. First GUI attempt's physical preview
failed under a busy workload with only a generic diagnostic; no cause was captured. The repeated
probe records renderer/fault detail and passes. This does not establish sustained physical
reliability or subjective loop quality, nor explain the inherited R4 pressure issue.

Portable reopened production Pattern export and R8 both preserve the two-second gate plus
80-ms release: **99,840 frames**. The production kernel also writes quiet authored percussion,
sustain and melodic source studies: F1, off, short **128** frames, long **4096** frames.
All twelve comparisons are finite stereo 48-kHz float WAVs; three original studies are retained.
No external recordings or subjective listening acceptance are claimed.

On the phase-matched sustained study, held PCM error against the unmodified periodic source
is at most **8.95e-9** with short/long overlap, versus **0.07516** for F1/zero. Mean over the
measured hold is effectively zero. F1's seam delta itself is zero because of its gain dip;
the crossfade's nonzero delta follows the actual waveform slope. Seam delta alone is not a
quality ranking. Long overlap increases the sustained energy of the decaying percussion study
and changes the melodic blend; manual listening and marker choice remain essential.

Final local evidence is under ignored
`.artifacts/sampler-loop-quality-f2/review-02/20261016T110913242-77bdf082681e4fc1be84a4211f7720ba/`:
sixteen GUI captures, alignment/input/audio/history results, portable project/media, export,
`loop-comparisons/` and numerical measurements. About **10.16 MB** of comparison/source WAVs is
deliberately retained for owner listening; generated evidence is not a product runtime resource.
The earlier attempt retains its physical failure under normal rolling retention. The final run is
explicitly preserved until owner listening/review so the requested WAVs survive ordinary cleanup;
there are no new global processes or settings. A separate local performance report and original
process snapshots remain ignored under `.artifacts/development-performance/session-20261016/`.

## Limits

Acceptance is bounded Windows routed/native/short physical evidence. Subjective listening,
arbitrary phase/content quality, alternative DPI/platforms and long stress runs remain open.
No hidden normalization, raw-seam mode, equal-power selector, multisampling, granular engine,
time stretching, additional import format or audio backend is introduced. No Git index/history/
remote operation, commit, tag or publication was performed.
