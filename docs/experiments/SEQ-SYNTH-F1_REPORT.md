# SEQ-SYNTH-F1 — polyphonic subtractive instrument

Role: Bounded implementation and verification report.
Read when: Reviewing or reproducing the first synth checkpoint.
Authoritative for: Executed checks, observed results, retained evidence and limitations.
Not authoritative for: Independent DSP/format/UI contracts, subjective sound quality or future scope.

## Outcome and ownership

The existing application can create a melodic Synth channel, choose/edit a factory sound, write
Piano notes/chords, save/reopen without external WAV and export audible PCM. Two polynomially corrected
oscillators, linear balance/second-oscillator detune, amplitude ADSR and a bounded low-pass execute in
the existing prepared note/voice/transport kernel. No second engine, scheduler, document, extension
platform, recorded MIDI, modulation matrix or demo track is introduced.

[SynthVoice](../../src/Seqvium.Core/Audio/SynthVoice.cs) owns versioned parameters, factory snapshots
and coefficients. [OfflineSampler](../../src/Seqvium.Core/Audio/OfflineSampler.cs) owns per-slot phases/
integrators and exact event/release lifetime. [Audio](../AUDIO_ENGINE.md#polyphonic-subtractive-synth-seq-synth-f1),
[format](../PROJECT_FORMAT.md#subtractive-synth-reader-111), [UX](../UX_CONTRACT.md#subtractive-synth-sound-design)
and [design](../UI_DESIGN.md#synth-f1-reference-assessment-and-direction) own current contracts.
The checkpoint is **0.1.14.0000**, one PATCH from 0.1.13.0001 with BUILD reset; reader 1.11 is selective.

## Architecture and DSP decisions

Frozen notes hold derived oscillator increments, envelope frames, blend and filter coefficients;
existing fixed voice slots hold mutable phases and two integrators. Preparation allocates no PCM for
synth notes, regardless of gate duration. Coefficients and canonical-state inspection stay off callback.
The existing graph preflight additionally charges 128 voice work units per frame for synth plans.
Voice/event/scratch/Mixer limits still refuse the whole unsupported plan rather than dropping notes.

Saw/Pulse step corrections and Triangle slope corrections reduce spectral folding. Triangle is
analytical rather than a drifting integrated square. The trapezoidal state-variable low-pass keeps
positive damping throughout the supported rate/cutoff/resonance bounds. Linear ADSR releases from the
actual NoteOff level. Velocity is applied once. The filter precedes amplitude, so no post-release tail
or latency needs another render owner. A fixed 0.18 gain provides per-voice headroom without a limiter.

Loop release overlap is preflighted and retained across boundaries; NoteOff identifies its iteration.
Unchanged voices copy phase/filter state through compatible publication; changed sounds retire without
retrigger. Cold seek derives phase/envelope age without replaying arbitrarily long notes. Stop/Panic
clear voices and existing FX state. Held preview has a 30-second safety gate and explicit Release/Stop.
R8 includes release through the local Output before Mixer and retains overload/cancel/managed-history
ownership. Unknown or invalid synth intent remains preserved and non-executable, never defaulted.

## Verification

Heavy builds/tests ran sequentially with one build worker, shared compilation disabled and no CLI
build servers retained by those builds. Rider, Backend, Codex and browser processes were not stopped.

```powershell
dotnet build Seqvium.sln -c Release --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1 --no-progress
```

The full Release build passes with **zero warnings/errors, 45.55 s**. Full suite: **1690/1690**,
zero skips, one test thread, **79.133 s CLI**. The focused `Seqvium.Tests.Synth*` group passes
**31/31, 4.503 s CLI** before L3. This full run was selected for shared voice/serialization risk.

| Boundary | Executed evidence |
| --- | --- |
| Pitch | Audible sine frequency at selected MIDI 36, 60, fractional 69.5, 96 and 120 across 44.1/48 kHz, within 0.03% of an independent frequency equation |
| Waveforms/aliasing | Coherent 4096-sample oscillator comparison, bin 487: corrected Saw/Pulse/Triangle residual energy outside valid below-Nyquist harmonics is under 35% of the corresponding naive waveform; nonzero energy guard |
| Envelope/filter | Independent A/D/S/current-level-release values; filter finite/bounded across four shapes, MIDI 127, both output rates, minimum/maximum cutoff and resonance extremes; low cutoff attenuates a real sine |
| Mix/velocity/polyphony | Independent linear blend oracle, exact velocity scaling, nontrivial detune change, sum of independent solo voices and 32 fixed slots; insufficient voice/loop capacity and excessive packet work refuse |
| Shared execution | Exact offline/realtime float equality at 44.1/48 kHz with different packets; finite natural completion leaves no voices; looping matches expanded finite notes through release overlap |
| Live/transport | Edited synth retires while unrelated synth continues; clock/epoch retained; cold Seek rebuilds active destination voices; Stop/Panic clears them; selected primed callbacks allocate zero measured bytes |
| R8/Song/export | Release beyond Pattern length, byte-identical R8 before/after Mixer/Delay/Reverb, resonant overload refusal without edits, Song through Delay/Reverb with exact same-rate realtime/offline equality and finite unclipped WAV |
| Intent/lifecycle | Undo/Redo, shared/independent sounds, portable Save/Open, selective reader 1.11 and falsely old envelope refusal, unknown/partial/invalid state refusal, stale/ABA edit rejection and controlled held-preview release/join |

Existing old-project fixtures and unrelated production behaviors participate in L3. This is
not an execution of an older binary against a synth project, exhaustive spectral characterization,
maximum real-device pressure or a general audio deadline guarantee.

## Corrective visual and ergonomic audit

The original implementation renders were rejected by the owner; successful tests did not constitute
visual acceptance. This correction remains part of **0.1.14.0000**, with no DSP, format or feature
expansion. The audit covers the actual Synth Editor and its shown Rack/Piano/Workspace neighbours.

### Defects found and corrected

| Observed defect | Final correction |
| --- | --- |
| Generic scope text, mixed-language fixture name, permanent signal-path sentence and repeated default identity | Removed the scope/path/default-name repetitions; preserved real custom names and useful musical labels/errors |
| Nested frames and excessive ordinary-pane scrolling | One unboxed instrument surface, paired oscillators, compact parameter groups and a pinned single-row preview |
| Empty space below ADSR in wide panes | The editable curve absorbs spare height; compact panes keep a usable minimum and local body scrolling |
| Release resembled Upload/Export and its icon/text group sat off-center | Original falling-envelope vector plus concise Release / Отпустить; one centered content group with symmetric padding and balanced icon ink |
| Unit suffix shifted each number away from its dial axis | Centered the value field independently; units occupy a reserved right column |
| Weak selected state and unclear preview availability | Shape and held Play gain stronger outlines as well as accent ink/surface; disabled Release text follows the button foreground; Stop remains available while the editor owns output |
| Preset selection could discard incomplete text or overlap a pending edit | Refuse selection with a short actionable message until the value is finished/cancelled; preserve text/history and disable the non-action Custom option |
| Feedback retained its previous language | Store the feedback key; translate it on observation while preserving the unfinished value |
| Invalid exact values reported a generic error far away under the preset | Every numeric parameter now shows its actual allowed range directly below its rejected field; draft text remains intact, localized range help also serves tooltip/accessibility |
| Scrollbars crowded or overlaid content, with ad hoc list compensation | Shared non-overlay scrolling and token-derived gutters on both axes; remove duplicate Pattern/Settings margins; Synth oscillator width follows the real viewport |
| Curve drag could compete with scrolling | Consume wheel input during the captured curve gesture; Escape/capture loss still cancel |
| Awkward Russian Pluck transliteration and English time/frequency units | Щипок, мс and Гц; conventional ADSR, pitch notation, percent and cents remain intentional |
| Piano Rack/Export buttons stranded on a new toolbar row; lengthy Rack creation label | Rack/Export move into the identity row; ordinary Piano tools remain together; concise Drums… / Ударные… labels and centered Rack button content |
| Narrow Rack creation row could push its position indicator outside the pane | Bounded flexible action column with intentional wrapping when needed; position keeps its own column inside the footer |

The general [UI design rules](../UI_DESIGN.md#required-reference-and-information-density-gate) and
[UX working-surface discipline](../UX_CONTRACT.md#working-surface-discipline) now prohibit service/
architecture explanations, obvious instructions and duplicate labels on ordinary musical surfaces.
They require recognizable icon-first actions, concise text when an icon is ambiguous, actual optical
review, independent RU/EN compositions and deliberate musical controls rather than generated forms.
Parameter names, musical values, errors and unavailable-state messages remain text. Optional guidance
belongs in short tooltips/contextual help/accessibility names. Comparative research stays private;
no third-party images, graphics or protected code entered the product and attribution remains intact.

### Before/after and retained images

Original text transport/framing/scope: local
`.artifacts/synth-f1/run-03/20261016T122051155-6b3c1490410f48058820d087e46ae171/`.
The intermediate Release alignment and value/unit axis are visible under
`.artifacts/synth-f1/ui-correction-08/20261016T130340344-d90fa6a1a431474c9205958914ea3e09/`.
The detached validation message before correction remains in the protected audit-12 compact-error
images. Final visual evidence is under
`.artifacts/synth-f1/ui-audit-15/20261016T135928679-73ae823b07ff4804b3bd00930ce1e019/`.
The inventory contains **114 actual window renders and eight 4× control inspection renders**:

- 36 Warm Bass / Soft Pad / Pluck states: RU/EN × Dark/Light × three geometries.
- 24 held/released preview states across the same language/theme/geometry matrix.
- Four compact ADSR depth views, 32 field-local invalid-value states and four canonical long-name states.
- Eight focused/pressed selected-wave states in reduced panes.
- Four native Sine selections, native/routed held preview and eight enabled/disabled Release details.

| Main window | Synth pane frame | Actual Synth content | Review |
| --- | --- | --- | --- |
| 1920×1080 | 760×660 DIP | 730×608 DIP | Rack/chord/Piano neighbours, all controls visible without scroll |
| 1280×800 | 640×630 DIP | 610×578 DIP | Whole editor, no body overflow, aligned footer |
| 900×700 | 360×520 DIP | 330×468 DIP | Paired oscillators, two-column parameters, all ADSR markers/values reachable by local scrolling |

Scale is 1.0. The eight enlarged button images render the actual control at 4× solely for optical
inspection; they are not evidence of native high-DPI platform support. Window captures show real
production controls, not drawn mockups. Long sound names come from a saved/reopened canonical fixture;
ellipsis and the full-name tooltip preserve their identity without crowding the preset/window actions.
The final image review receipt is retained with the run. Owner aesthetic acceptance remains separate.

### Optical Release result and geometry evidence

Release's icon/text stack is centered as a unit; the original icon has centered visible bounds and
uses the same button foreground, including disabled state. No negative margins or per-language nudges
are used. RU and EN enabled/disabled button images were inspected separately, alongside whole-window
composition. The result is visually balanced; its falling slope and short label distinguish it from
Export, while familiar Play and Stop remain icon-only.

Actual font-outline/icon-ink union center offset is **+0.626 DIP in EN**, **+0.460 DIP in RU**, and
approximately **0 DIP vertically** in both themes. Text/icon ink vertical differences are −0.566 DIP
and +0.706 DIP respectively, reflecting their different glyph shapes. These supplement visual review;
matching rectangles alone is not optical acceptance.

All 96 TextPresenter/unit vertical checks and 128 dial/value horizontal-axis checks have **0 DIP**
maximum difference. Sixteen composition checks include the long-name states. Minimum waveform target
width is **35 DIP**, height 34 DIP after the shared gutter reservation. Preview controls are 40 DIP high with center alignment within
0.5 DIP; ordinary/reduced content has no overflow and compact content has no horizontal overflow.
Focused/pressed states retain a stronger selected contour. Shared scrolling changes default style
setters, retaining the existing templates. The actual two-theme component catalog verifies an 8-DIP
content-to-bar gap on both axes; the populated RU/EN shared workspace matrix and field-alignment
checks also pass. These supplementary runs remain in local shared-scroll-catalog-02 and
shared-scroll-matrix directories.
The routed Settings and populated Browser/Patterns navigation scenarios also pass after the shared
scroll change, including reduced inspector reachability and side-panel resize/cancel/restoration.
The generic workspace matrix's optional native segment refused foreground access; it is not a native
pass. Native acceptance here comes from the separate strict Synth run.

Each of eight numeric parameters rejects malformed, below-minimum and above-maximum text without a
canonical edit: 24 explicit boundary/parse checks, plus 32 localized compact error renders. The range
sits 2 DIP below its own field. The compact Cutoff example retains `20000` and displays `20–18000 Гц`
there; it no longer adds a generic sentence below the preset. Escape clears the error/draft. Valid
commits use the existing parameter contract. Ordinary surfaces show no permanent range paragraphs.

## Routed, native and regression verification

The permanent [scenario](../../tools/Seqvium.VisualCheck/SynthEditorCheck.cs) and
[optical audit](../../tools/Seqvium.VisualCheck/SynthVisualAudit.cs) retain the reusable checks.
Routed input covers Rack creation and Source double-click, chord, waveform/balance/detune/filter,
exact/invalid/incomplete values, preset refusal without draft loss, language change with active
feedback, ADSR commit/Undo/Escape/capture loss, all four factory snapshots, portable Save As/New/Open
and WAV export. Compact gestures exercise every ADSR handle, wheel conflict and cancellation in each
language/theme. Preview states use the existing real output; no state is invented for a screenshot.

The final strict native run passes with real Windows input in **reduced and compact** panes. Each
oscillator starts at known Saw, receives a native Sine click, changes canonical SoundDefinition and
its displayed shape/selected contour, then returns to Saw by native Undo and Sine by native Redo.
The saved/reopened parameters retain both Sines. Receipts record OS foreground/hit root, expected
button, actual pointer source/position, editor/document generation and canonical/displayed values.
The native note selector and Hold/Release/Stop also pass. Release uses an 800-ms fixture: the voice
remains active after NoteOff, naturally completes, and Stop joins the retained output. A new held
note followed by Stop confirms hard teardown. Preview creates no canonical edit/history.
Physical Samsung USB C Earphones, 48-kHz stereo, separately passes routed Hold/Release/Stop.
The latest input/ready-condition receipts are retained under
`.artifacts/synth-f1/readiness-final-19/20261016T141127729-b59553aa8289423cb47a05e090e105a1/`;
the submitted Synth visual matrix remains audit-15, with identical Synth controls. The final narrow
Rack footer is reviewed separately in the local rack-final-review run. Native/physical preview now awaits observed
Playing/Ended conditions instead of fixed hold-completion delays.

The final affected production/test Release build has **zero warnings/errors, 21.99 s**. Focused Synth, DesignSystem,
PianoRoll and PatternEditorNavigation checks pass **52/52**, zero skips, **4.799 s CLI**. The original
full **1690/1690** Release run covers unchanged DSP/serialization. This UI/harness correction uses
focused L1/L2 and actual L4; no second full regression pass is claimed. Builds/tests ran sequentially
with one worker and disabled CLI build servers; unrelated applications were not stopped.

### Historical refusals, reproduction and harness corrections

The historical unconfirmed waveform click has insufficient original delivery diagnostics to establish
its cause. The old Cutoff → waveform protocol was replayed under tracing and **passed** (canonical
wave2=1). It is not honest to attribute that old observation retrospectively to a product bug or a
particular harness race. The new strict Saw/Sine/Undo/Redo/Save/Open cases pass independently.

Separate failures have concrete evidence and corrections:

- Foreground/hit-test receipts identified browser/Rider coverage even when Avalonia IsActive was
  cached true. Readiness now awaits the real Windows foreground event before the unchanged guarded
  activation/input. No clicks are retried or sent through another window. Required native failure
  makes the scenario fail; a successful render cannot mask it.
- Workflow completion precedes posted presenter reconciliation; callback Playing can appear later
  than accepted publication. The verifier now consumes the production Refresh at the current
  generation after draining posted reconciliation, rechecking pending teardown and actual owner
  idleness, and awaits observed canonical/preview conditions. Preview readiness timings are recorded;
  fixed delays no longer certify held/released output. Stale-state guards remain.
- A verification fixture incorrectly required an already-Saw no-op to return a changed edit. It now
  establishes/asserts canonical Saw and only edits when needed; no product behavior was weakened.
- Diagnostic handlers captured a mutable oscillator loop variable and remained registered for the
  next geometry. The retained audit-11 stack points to an invalid Oscillator3 lookup during native
  delivery. Iteration captures are now stable and handlers detach in finally. Final runs complete both
  sizes, canonical/visual history, preview and Save/Open. This later diagnostic regression is distinct
  from the historical unconfirmed click.

The expanded verification also exposed intermediate incomplete runs: an unconfirmed Redo timeout,
an early invalid-field assertion and a preview-ready timeout. Their original observations lack enough
state/delivery detail to assign definitive individual causes. They are failures, not acceptance runs.
Diagnostics now include history press/release/click recipients, OS target evidence, field draft/range/
generation state and preview timeout state. History input waits for the real host command owner and
enabled control; presenter readiness is checked after queued production reconciliation. No musical
action is retried and no occlusion/stale guard is disabled. The successful audit-15 and subsequent
readiness run are recorded separately from these intermediate attempts; later harness changes do
not change the product image matrix or DSP.

Earlier raw attempts follow the existing retention policy and are never relabelled as passes. The
pre-correction native diagnostic stack remains in the local audit-11 run. IDE debugger configuration
could not supply launch overrides; no user debugger settings/breakpoints were changed. Actual input
receipts and source-visible harness faults provide the declared evidence. Historical cutoff/occlusion
attempts and fixture/API compile corrections were resolved before their respective green builds.

## Listening samples and limits

The protected final local run retains four authored **48-kHz stereo PCM16** files for owner listening:
WarmBass.wav (2.10 s), SoftPad.wav (3.60 s), BrightLead.wav (2.22 s) and Pluck.wav (2.12 s). Bass uses
low notes, Pad a held chord, and Lead/Pluck short phrases. They come from the production exporter and
ordinary local graph gain; they are not loudness-normalized, third-party recordings or another demo
track. The run's listening-metadata.json records frames, nonzero peak/RMS and SHA-256. Preserve these
and final render receipts until owner listening/aesthetic review. Private study originals remain
outside Git; raw captures, verification projects and earlier runs remain ignored local evidence.

Numerical/physical correctness does not establish preferred timbre, anti-aliasing at every pitch,
click-free live parameter morphing, historical filter/FX seek state, MIDI input or external plugins.
Cold seek, fixed pulse width, linear ADSR, finite preview safety gate and explicit capacity refusal
are intentional bounds. Existing R4 pressure failures, broader platform/DPI/accessibility validation
and owner acoustic/aesthetic acceptance remain open. No third-party assets/code were distributed.
Required licensing remains intact; Git staging/history/remotes were not changed.
