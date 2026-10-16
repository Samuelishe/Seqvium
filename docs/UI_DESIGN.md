# UI design

Role: Evolving visual and interaction design guide.
Read when: Designing visual hierarchy, pane chrome, controls, indicators, or graph presentation.
Authoritative for: Durable visual principles, usable responsive layout, interaction density, consistency, progressive
visual complexity, semantic theme/style resources.
Not authoritative for: Workflow semantics, pane lifecycle, graph execution, or implemented token values/component API.

## Design intent and scope

Seqvium's identity is **chaos into structure**: musical fragments acquire a sequence while retaining
their creative energy. Direction 09 supplies the cyan/teal/mint mark, intentional offsets and opposing
slopes; 01 and 07 are retained alternatives, not competing runtime themes. The [brand kit](../brand/README.md)
owns artwork, optical masters and reproduction; this guide owns their relationship to the instrument.

Title and About pair the 09 sign with Onest Black lettering. Labels, commands and dialogs use embedded
Onest Regular, headings SemiBold, exact numeric fields and technical fixed columns JetBrains Mono.
Musical symbols have an explicit bundled fallback. Heavy brand lettering does not justify bold body
text or enlarged controls. [DESIGN_SYSTEM](DESIGN_SYSTEM.md) owns the shared implementation roles.

Cyan actions and focus connect the slate/jade instrument to the brand. Light uses deeper teal ink
for contrast. Source/channel, meter, warning/error and transport colors retain their meanings.
Preserve tactile materials, FullHD density, both themes and RU/EN reachability. Review actual small
icons and populated editor pixels after typography changes.

Seqvium should feel like a creative instrument: substantial, tactile and enjoyable to manipulate.
The owner explicitly requests expressive instrument design, including volume, complex controls,
shadows, gradients, lighting and meaningful visualizations. Implementation difficulty is not a
reason to reject an otherwise useful design. The older flat-shell preference is superseded.
[DESIGN_SYSTEM](DESIGN_SYSTEM.md) catalogs the implemented visual API; source owns actual values.
Visual quality and musical correctness are complementary requirements.

## Compact studio instrument direction

### Required reference and information-density gate

Before every first-party UI change, inspect relevant actual screenshots from the local reference
library below. If suitable local images are unavailable, search for and inspect real product
interfaces on the web before editing. Reading a catalog or relying on memory does not satisfy this
gate. References inform composition and hierarchy; their graphics are not application assets.

Working surfaces must not become documentation. Never add permanent tutorials, explanatory paragraphs,
formulas, canonical/runtime terminology, UUIDs or redundant diagnostic counters to the musical flow.
Prefer familiar icons and one- or two-word actions. Show the selected sound, waveform, notes, controls
and actual musical state. Keep tempo, pitch, position and useful levels where they support a decision;
show exact source/recipe/engineering data only on deliberate inspection. Do not duplicate a value in
several formats or replace a usable control with an explanatory sentence.

Short tooltips name actions or essential scope; they are not a place to hide the same long tutorial.
Concise errors, unavailable state and consequential shared-edit scope remain understandable.
Review empty and populated panels together at normal and reduced sizes in RU/EN and Dark/Light.
Remove unnecessary words/numbers before adding help text to explain an overloaded composition.

Playlist now separates form from events: a quiet named/color band directly beneath the bar ruler
makes sections legible across a dense mixed arrangement. The section navigator gives precise access;
its small command row and item context menus expose actual structural editing rather than an
independent arrangement manager. Selected musical content retains the stronger outline. A shared
glyph and variation arrow make content relationships visible without inventing inherited note state.
Boundary refusal appears beside the editor with the real source/track and a direct reveal action.
Names, note thumbnails and stereo waveforms remain the main visual content. Track switches fit within
their lane, leaving grid rhythm and unselected clips readable in both themes.

Playlist Audio Clips use a thin source-colored outline, compact beveled name strip, readable actual
mono/stereo envelopes and restrained fade handles. Long sources retain detail through min/max levels;
waveform visual amplitude fitting is not source normalization. Editing tools and exact properties
remain contextual; no compulsory Rack panel or separate mixing surface is introduced.

The accepted direction is a dense desktop instrument, not a spacious settings application. The owner
authorizes substantial recomposition of the interface and its guidelines when reference study and
actual rendered usability justify it. Previous generous editor proportions are not immutable constraints.
Design for simultaneous musical tools on a FullHD display: music above, compact mixing below, contextual
processing beside its source. Do not scale a large form down uniformly or enlarge every channel when
its pane grows. Spend extra space on music, meters, meaningful curves and additional channels.

The implemented Mixer bank targets roughly one quarter of a FullHD desktop or less. Its narrow fixed
strips, pinned Master and side inspector replace the initial oversized composition. Rack rows, Playlist
lanes, Pattern cards, rail and chrome now use a denser common rhythm. These changes calibrate the
desktop system, not touch-screen proportions; ordinary exact fields remain larger than strip readouts.
Inspect actual pixels and mouse interaction at normal and reduced sizes, not isolated enlarged widgets.

The visual language is **slate / jade / machined instrument**: dark recessed wells, small sculpted metal
fader caps, graduated knobs, restrained chassis grain, thin colored source bands and quiet technical
readouts. Light has a cool steel chassis, saturated identity ink and distinct recess/cap depth, rather
than inverted Dark or default white Fluent forms. Color identifies source, selection and signal state;
do not make every inactive control a green selection surface. Quiet M/S switches and processing
summaries need no permanent button frames; selected/active/focus states remain independently legible.

Original code-drawn vectors, textures and materials are preferred where they render cleanly at different
sizes. Bitmap generation is appropriate only for a concrete visual need, not a prerequisite for rich
design. Never extract proprietary DAW graphics. Grain belongs to chassis, not note grids or plots.
Decorative depth cannot replace actual signal measurement or processing. EQ plots are explicitly
analytical responses; meters show real DSP taps. No simulated spectrum is permitted.

Reference review includes the private mixer/EQ/dynamics catalog and the owner's FullHD FL Studio and
Cubase workspace captures. Adopt successful density, stable channel identity, restrained chrome and
contextual processing; do not copy their assets or reproduce their layouts one-to-one. Private images
remain outside Git. [Roadmap](ROADMAP.md#desktop-design-continuation) owns staged continuation across
the other editors; rendered evidence is not owner aesthetic acceptance.

## Instrument materials and hierarchy

Use a slate chassis, illuminated jade actions, colored channel identities and sculpted controls.
Depth has a deliberate structure: workspace recess, raised instrument panel, bevelled control,
active light. Highlights, shadows, gradients and controlled glow create character and explain
manipulation. Detailed knobs and graphical editors are encouraged when they support a real parameter.
Avoid making every surface equally luminous or surrounding ordinary settings with elaborate frames.
Materials may be rich without obscuring labels, focus, grid rhythm or actual values.

Dark and Light are equal-quality working presentations. Dark keeps the slate studio chassis;
Light uses cool pale surfaces, saturated channel bodies and deeper colored ink, with the same
geometry, materials, states and interaction. Light must not become a desaturated fallback: review
musical color, depth and manipulation appeal as well as text contrast. Design it independently
rather than by inversion. A checked pad has both a lit body and a small
indicator; focus remains an independent outline. Channel hue supplements readable names and numbers.

`HostPalette` owns semantic colors, `HostMetrics` geometry/typography and `HostMaterials` derived
gradients/shadows. Standard controls and DrawingContext compositions use these shared resources.
Instrument-specific composition is encouraged; a general-purpose form is not the default music UI.
No borrowed graphics or screenshot textures are shipped.

### Theme parity and control economy

The current Light resources strengthen jade selection/action surfaces, colored Rack off bodies and
Playlist clip bodies, channel ink, boundaries and derived bevel/shadow depth. Dark color/material
values are preserved. Ordinary note-preview ink has at least 3:1 contrast against its channel body;
the checked-pad indicator has at least 4.5:1 against each checked body in both themes. These checks
cover those specific pairs, not accessibility certification or all composited hover/selection states.
Final perceived quality requires real dense/sparse content and owner review.

Rack no longer reserves footer space for a paint tutorial; localized step/position tooltips and
accessibility help retain that guidance. Playlist keeps its useful empty-state invitation, then
reclaims the instruction row when clips exist; the selection-tool tooltip and canvas accessibility
help expose gestures. Keep target names, musical time, values and persistent blockers visible.
Recognizable commands use original vectors with accessible names/tooltips; ambiguous operations
keep concise contextual menu labels. An icon does not replace a necessary musical identity or value.

## Reference library and next-editor direction

The home-machine library contains 34 inspected owner-supplied images: 19 earlier references, including
the AVIF, and 15 new synth/drum/sampler/browser/dynamics references. Originals
are catalogued outside Git under `%USERPROFILE%/Documents/Seqvium Resources/Design References`, with
stable PR/PL/MX/EQ/SY/DS/SA/SB/CP IDs, original paths/names, SHA-256, dimensions, identification confidence and
per-image observations/limits. The local `index.html` provides search and original-image access;
`catalog.json`/CSV, `DESIGN-BRIEF.md` and the private `MUSICAL-EDITORS.md` preserve provenance and
assessment. The new batch was copied, retaining inbox originals; its receipt records source/target
hashes, sizes and modification times, and prior metadata editions are retained. Earlier IDs/records
remain unchanged. Search includes bilingual tags, original names/paths, lessons and checksums, with
category filtering. Original publication URLs
and reuse rights were not supplied. These are private study references, never distributable assets.
[Development](DEVELOPMENT.md#local-development-resources) owns the local-resource entry points.
Earlier reference locations describe their originating machine; their absence on another machine
does not invalidate the design owners or authorize reconstruction of missing personal material.
The current work-machine catalog uses `WORK-`-prefixed IDs and local Previews. Inspect that actual
catalog/images rather than assuming the home-machine count or identifiers are present unchanged.

The following are selected directions, not claims about source-DAW gestures or sound. The preparation
implemented theme/control economy; SEQ-R9 now implements the bounded Piano surface below. Static
images establish visible composition, not performance/acoustic evidence. [UX](UX_CONTRACT.md) owns
Seqvium's implemented gestures; remaining reference-derived capabilities stay proposals.

| Working area             | Selected solution and comparison with Seqvium                                                                                                                                                                                                                                                    | Implementation boundary                                                                                                                                                                 |
|--------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Piano Roll               | PR-05 note bodies/pitch labels, PR-01 bracket, PR-03 aligned velocity and PR-02 dense-content review inform the implemented original SEQ-R9 composition below                                                                                                                                    | Shared NoteEvents/history, explicit Part, root/audition and Rack preservation are verified within the bounded report; PR-04 controller curves remain later                              |
| Channel Rack             | Existing bevelled pads/beat groups remain the rhythm surface. Identified Part labels/menus open Piano Roll; rich rows show real note previews                                                                                                                                                    | No flattening into step pads; per-channel mix controls remain planned                                                                                                                   |
| Playlist                 | PL-02's meaningful note/audio/automation distinction, PL-04's stable track/time composition and PL-03's saturated Light bodies with dark content ink. Keep the implemented fixed headings, overview, shared clips and scoped selection; improve existing compositions when musical work benefits | Current thumbnails show real notes. Waveforms, sections, automation and lower contextual editors require their actual owners; arrangement tracks do not imply Mixer channels            |
| Node Graph / Node Editor | Retain the implemented free canvas, typed ports, nearest-cable selection and target-aware editor. EQ references favor a large meaningful visualization with optional exact parameter depth; a processing summary opens the graph rather than putting every cable in the music editor             | Gain's transfer curve is analytical, not a measured signal. New processors/editors need actual parameter/provider and lifecycle ownership                                               |
| Synthesizers             | SY-02's oscillator cards and function-colored envelope/LFO/filter regions, SY-01's sculpted caps and meaningful waveform/modulation surfaces. Use graduated arcs, clear position and exact entry around supported musical functions                                                              | Instrument DSP, state, voices and modulation ownership first; visible path/granular controls do not prove algorithms or source gestures                                                 |
| Mixer                    | MX-03's tactile knobs/recessed faders, MX-04's readable signal ladders and MX-01's aligned channel identity. Preserve a useful strip minimum and horizontal bank navigation; keep Master locatable, M/S distinct and routing depth contextual                                                    | Real level/pan/mute/solo, measured taps, channel identities and routing differ from Playlist organization; do not fabricate meters or copy touch-screen proportions                     |
| Effects / EQ             | EQ-05's dominant curve/selected handles and EQ-03's selected-band Frequency/Gain/Q panel are strongest. EQ-01/EQ-02 link band colors to controls; EQ-04 keeps analysis subordinate to editable curves. Selected-band depth beats permanent all-band machinery                                    | Actual EQ/filter/analysis/automation capabilities and bounded measured data first; pitch tracking, dynamic EQ and transient/tonal separation remain processor work                      |
| Browser / Sample Lab     | SB-02's waveform/duration results and selected preview player, SB-01's category/source hierarchy and SB-03's generous selected waveform. Lead with local find/audition/explicit use; disclose source/root/BPM details as needed                                                                  | Browser images supply composition evidence, not Sample Lab generation or implemented discovery UI. Preview/use/publication stay under SAMPLE_WORKFLOW; cloud/store scope is not implied |

Physical character belongs mainly to controls and instrument panels. Grids and analytical plots need
quiet surfaces so notes, connections and curves stay legible. Light's strongest supplied evidence is
PL-03 (Pro Tools) and PL-05 (REAPER-style), with SB-01 supplying Light Browser structure;
Light knobs/EQ/synth composition must be designed and
reviewed independently, not claimed as observed in dark screenshots. Do not extract proprietary icons,
panel textures or screenshots. Existing original vectors/derived materials cover this preparation;
create new original assets only for a concrete editor need.

[R9 planning](ROADMAP.md#seq-r9--piano-roll) and the existing later-stage follow-ups own deferred work.
The [preparation report](experiments/SEQ-HOME-RESOURCES_REPORT.md) records actual verification limits.

### Synth F1 reference assessment and direction

**Accepted design requirement for SEQ-SYNTH-F1; implementation and visual acceptance are pending.**
Musical expression, ease of sound exploration and visual quality have equal priority with correct DSP.
This supplement defines the quality bar, not oscillator count, parameter ranges, voice architecture
or additional synthesis features. Those need the bounded F1 implementation brief and their technical owners.
No tonal Synth Editor exists in the inspected baseline; the implemented Drum/source editors are distinct.

#### Reference evidence and musical workflow

On 2026-10-16 the actual work-machine catalog contained 91 entries, rather than the home-machine
34-image collection. Inspected local images include WORK-SY-004 (Vital), WORK-SY-001 (Serum 1),
WORK-SY-003 (Serum 2) and WORK-DS-006 (KICK 2). Vital makes waveforms and envelopes visually dominant;
Serum separates oscillator anatomy from modulation and gives caps a readable pointer/rim;
KICK 2 gives a directly meaningful curve priority over secondary controls. These observations concern
visible composition only. The home SY-01/SY-02 IDs must not be substituted for work-machine records.
Private originals/catalogs remain outside Git and are not runtime assets.

Official public pages, current manuals and their actual interface images were also inspected below.
The latest Bitwig guide still labels its Polymer device image v3.3; the product-page animation is
labelled 4.4. These are manufacturer-published references, not evidence of a locally installed latest
build. Three sampled frames of the five-frame Polymer animation show stable module positions through
model changes. Linked videos were located, but no video playback, hands-on use or acoustic comparison
of these products was performed. The musical workflows below are derived from manuals, with Seqvium
design conclusions explicitly separated from observations.

| Reference and evidence | Musician's workflow described by the source | Selected principle and deliberate rejection for F1 |
| --- | --- | --- |
| [Vital](https://vital.audio/), plus local WORK-SY-004 | Inspect oscillator waves, adjust source controls, shape envelopes and inspect filter response; the product describes visual modulation previews before committing a connection. | Make wave choice and envelope shape immediate, meaningful feedback. Reject wavetable stacks, macro columns, multiple modulation tabs and decorative animated signal displays unless F1 actually implements the corresponding capability. |
| [Surge XT manual](https://surge-synthesizer.github.io/manual-xt/) and its full interface image | Select an oscillator, choose its algorithm, balance sources in the mixer, change cutoff/resonance and shape separate filter/amplitude envelopes. A muted oscillator's waveform dims. | Keep sound generation, balance and shaping visibly related; identify an envelope's destination. Reject dual scenes, modulation routing banks, multiple filter topologies and an always-visible effect rack for the smaller F1. |
| [Polymer guide](https://www.bitwig.com/userguide/latest/synth/) and [product demonstration page](https://www.bitwig.com/polymer/) | Choose an oscillator, filter and envelope in dedicated slots; adjust cutoff and envelope stages in the same compact panel. The manufacturer animation changes models without moving these main regions. | Use a short, stable instrument composition and emphasize frequently manipulated controls. Reject module swapping, conversion to Grid, sub/noise/PM/FX areas unless independently included in F1; the reference is not a feature checklist. |
| [Ableton Analog manual, sections 31.1.1–31.1.6](https://www.ableton.com/en/live-manual/12/live-instrument-reference/#analog), full device and envelope images | Choose wave and tuning/level in the shell, open the selected section's display for depth, then adjust filter frequency/resonance and ADSR. The display visualizes the selected section while primary controls remain available. | Preserve functional hierarchy and a readable source-to-filter-to-amplitude relationship. Reject its two parallel oscillator/filter/amplifier paths, dense global/MPE options and hiding the only amplitude envelope behind a section switch. |
| [FL Studio 3x OSC manual](https://www.image-line.com/fl-studio-learning/fl-studio-online-manual/html/plugins/3x%20Osc.htm) and actual interface image | Select shapes with waveform buttons, adjust coarse/fine tuning and mix sources; filter/envelope work belongs to the integrated channel/Sampler controls. Oscillator 3's mix reduces the combined contribution of 1 and 2. | Offer direct shape choices and repeatable oscillator anatomy. Reject the order-dependent balance model, decorative branding area and moving essential F1 shaping into another settings surface. A blend control must represent F1's actual mix law. |

#### Selected composition and interaction direction

Use one original Seqvium instrument chassis: a quiet sound/preset identity row; compact oscillator
regions with recognizable shape choices and an actual analytical wave; balance beside its sources;
filter controls along the signal path; and a prominent editable amplitude ADSR. Prefer simultaneous
access to these few musical functions over a long parameter form. The exact arrangement follows the
implemented F1 signal order; do not draw audio flowing through an envelope generator when it actually
controls an amplifier. Any filter envelope needs its own real DSP/state and a clear destination.

Spend space on the waveform and editable curve, comfortable tactile dials, readable pointers/scales
and the cutoff gesture. Keep a musical value beside its control without repeating it in captions.
Draw only implemented shapes; distinguish an analytical oscillator period, analytical filter response
and measured output. Do not show a fake scope/spectrum, animated voices or unsupported response curve.
ADSR must distinguish time-based A/D/R from the S level and held-note duration: a drawn sustain plateau
does not promise a fixed note length. Exact input and the curve must edit the same parameters.
Capture/cancel, focus, keyboard precision and one-gesture/one-Undo follow existing UX contracts;
live audible editing requires its own verified runtime boundary.

Retain slate/jade materials, original vectors, Onest/JetBrains Mono and shared HostPalette,
HostMetrics, HostMaterials, GainDialControl and standard field/button styles where their semantics fit.
These are composition requirements, not a new implemented component API. Light needs its own cool-steel
depth, strong curve/control ink and readable active state; it cannot be a washed-out inversion of Dark.
Labels/tooltips/accessibility names work in RU/EN. No local language/theme controls, dormant processor
panels, copied graphics, screenshot textures, permanent tutorials or technical counters belong here.

Narrow panes reflow functional groups in the same musical order and scroll editor content locally,
retaining useful grab targets and text size. Keep audition reachable while editing; reuse the existing
instrument interaction language where appropriate, with actual held/released-note semantics verified
for the synth. Comparing timbres can use repeatable audition, existing history and working presets;
this does not authorize an unimplemented A/B bank, MIDI keyboard or new global shortcut.

#### Required Synth Editor visual acceptance

Render the actual production Synth Editor, rather than a mockup or a Drum editor labelled Synth.
Review every RU/EN × Dark/Light combination in a FullHD workspace with neighbouring musical panes,
a reduced main window and the smallest supported Synth pane. Record actual window/pane DIP sizes,
display scale and screenshot paths; select final compact bounds from real implementation, not a
scaled screenshot. Initial review targets are a 1920×1080 desktop and a 1280×800 reduced desktop,
with compact pane geometry declared separately. These targets are not new host minimum dimensions.

At each configuration inspect initial/default and edited sounds, short/plucked and sustained/released
envelopes, long RU labels/preset names, selection/focus and any supported unavailable/error state.
Exercise shape choice, oscillator balance, ADSR handles/exact values, filter movement and repeated
timbre audition. Check that visible changes affect the sound, released notes follow Release, and
Undo/Redo and resize do not lose the selected sound or an input draft. Verify real field alignment,
pointer capture/Escape/reversal, local scrolling and reachability at compact sizes. Reuse existing
visual tooling; label routed versus native input evidence accurately.

The F1 completion report must show representative real renders and record the complete matrix,
then compare composition and manipulation against the inspected references: waveform/curve clarity
from Vital, functional grouping from Surge, compactness from Polymer, hierarchy from Analog and
shape selection from 3x OSC. Include deliberate rejections and reasons, defects found/fixed and any
remaining limits. Rendering/input evidence is separate from owner aesthetic acceptance and DSP/audio
evidence. This research pass supplies no Synth renders or completed acceptance claim.

### Implemented Drum Voice composition

The built-in drum family adopts DS-03's meaningful pitch/time surface, DS-01's oscillator/noise anatomy and
DS-02's selected-sound continuity. It uses original host drawing/materials; no private reference asset
is shipped. One shared chassis connects primary dials, Pitch/Amplitude modes, direct curve handles,
the actual derived waveform and envelope depth. These modes are functional, without a second rhythm
surface or effect-bank placeholders. Instrument-specific knobs make voice anatomy visible; three
factory presets and portable custom snapshots remain compact. Hats expose one functional choke
toggle. Pitch is offered for Kick/Tom; amplitude and actual waveforms serve the other voices.

Exact values stay below dials; only Tune also shows a distinct frequency. Light uses stronger
palette-owned rim/scale ink, recessed wells and a denser waveform stroke, preserving Dark's materials.
The frequency axis adapts to pitch-drop depth; note/Hz and ms context explain the sound. Compact
composition reflows the sound/audition row and parameter groups, and scrolls all working content locally.
Owner feedback on the initial actual screenshots favored this direction; final visual acceptance
remains distinct from the [render/input evidence](experiments/SEQ-FIRST-DRUM_REPORT.md).

### Musical-editor reference proposals

These are selected study directions for later design, not implemented controls, accepted input
bindings or authorization for new instruments. Static images establish visible composition only;
annotations do not prove gestures. Detailed per-image judgments and identification confidence stay
in the private catalog. The comparison uses existing R9 Dark/Light captures and the current
Node Editor owners; this preparation does not repeat Piano Roll verification.

| Area              | Strongest direction                                                                                                                                        | Seqvium comparison and capability gate                                                                                                                                                                                                                                |
|-------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Drum sound design | DS-03: dominant pitch envelope with note/Hz and time context; DS-01: oscillator/noise anatomy; DS-02: selected identity in one sound editor                | Seven built-in drum families now adapt controls, pitch/amplitude envelopes and previews to actual DSP, with factory/custom presets and linked hats. Layers, modulation and custom groups remain future work; DS-02 is a sample-pad reference, not synthesis evidence. |
| Sampler           | SA-04: stereo range/loop flags and overview navigation; SA-01: slice-to-key continuity; SA-02: compact waveform-first composition; SA-05: root-key context | Root Pitch, actual PCM waveform, source-frame range/sustain loop and held/released preview are implemented in the existing Source Editor. Mapping/slicing remain future work; Playlist trim uses a separate timing model                                                  |
| Dynamics          | CP-02: readable physical caps and threshold/knee curve; CP-01: selected-band controls beside reduction history                                             | Gain keeps its useful compact level/optional analytical Curve. Future compression needs real stateful DSP and measured taps; history, analytical transfer and waveform must have distinct meaning                                                                     |
| Control shape     | Raised cap, readable pointer, skirt/arc and recessed base; shape buttons for discrete waveform/filter choices                                              | Use the shared material/token family. Bipolar controls need meaningful center feedback, unipolar controls an endpoint, and exact values/units on demand. Comfortable targets beat miniature screenshot proportions                                                    |

For later rich Node Editors, let the meaningful graph own space and reveal exact selected-parameter
depth nearby. Keep the small Gain editor small. Colored modulation context may eventually connect
source, destination and depth, but must not impersonate typed signal cables or unsupported automation.
A compact linear effect-chain view remains a proposal for the same graph; branched topology must
remain explicit under [graph ownership](NODE_GRAPH.md#local-processing-scopes-and-alternate-views).

Candidate existing-tool improvements are a clearly targeted Sound editor opened from Rack identity,
optional visual root-key context in Piano/source properties, future named expression lanes, and
real waveform/range/overview affordances for audio clips in Playlist. These preserve current shared
notes, source ownership, navigation and explicit editor targeting. They are retained in
[planning](ROADMAP.md#musical-editor-reference-follow-ups), not started in this preparation.

Proposed musical gestures are envelope-point manipulation, forgiving source/loop handles, knob
fine adjustment/exact entry, selected-band threshold edits and explicit waveform audition/use.
Apply existing keyboard, capture/cancel, stale/ABA and one-Undo principles when a feature is designed;
continuous audible parameter preview requires its own runtime boundary. Current Gain commits on
release and is not a continuous audio-preview contract.

Dark and Light deserve independent expressive materials and review; improving Light must not flatten
or weaken Dark. Quiet graph grounds, readable colored curves, tactile controls and distinct selection/
focus/playing roles matter in both. Light Browser structure is not evidence for Light synth materials.
Prefer original self-contained icons for recognizable actions/shape choices, accessible names and
tooltips; retain necessary target, parameter and unit text without permanent tutorials or fake tabs.
No new token/component contract or production appearance changes follow from this assessment.

## Implemented SEQ-R9 musical surface

The five Piano originals and their private analytical notes were inspected before implementation.
PR-05 informs readable bevelled note bodies/names, PR-01 the Pattern-end bracket and layered axes,
PR-03 aligned velocity, and PR-02 dense-note review. PR-04 controller curves remain deferred.
These are composition principles, not copied assets, source-DAW input claims or distributed provenance.

Seqvium's original canvas has a fixed keyboard/ruler, quiet alternating pitch rows, distinct bar/
quarter/subdivision lines, channel-colored note materials and thin selection outlines. Names appear
only when they fit; intensity strips, edge cursors and captured previews convey edits without permanent
instructions. Chord velocity handles stagger while retaining onset stems. Muted ghost Parts are context,
never a second selection target. Compact Pattern/Part scope, source-root access and contextual exact
fields keep music dominant. Rich Rack rows show real pitch/duration/intensity, not misleading pads.

Dark retains its accepted roles and jade/slate material depth; Light has independently reviewed keys,
saturated bodies and readable ink. Both use the shared [Design System](DESIGN_SYSTEM.md), original
vectors and derived materials. Actual RU/EN ordinary/reduced renders, field alignment, window-coordinate
containment and native musical editing are in the [R9 report](experiments/SEQ-R9_REPORT.md).
This establishes implementation/evidence, not subjective owner aesthetic acceptance.

## Implemented R3-F1 visual baseline

The retained single-window shell now uses the v2 instrument materials, larger readable shared
typography and one transport row. Project actions, Play/Stop, BPM, meter and history have clear groups.
Version/capability details remain in About; technical information is disclosed deliberately.
The original F1 frame's lifecycle/OS behavior is retained; its dimensions and flat appearance are
superseded by the shared resources. Theme/language switching retains the window and controls.

## Implemented R3-F2 pane presentation

Panels have a substantial raised surface, bevelled header, readable title, shadow and distinct
active outline. Close, collapse, docking and resize retain existing behavior. Invisible resize
tolerance remains independent of the visual boundary under
[Workspace](WORKSPACE.md#resize-hit-targets-and-boundary-ownership).
Inspector/Appearance use shared controls; scrolling and localized tooltips preserve small-pane use.
The left navigation rail opens actual Rack, Graph, Editor, Inspector and Appearance surfaces.
Only implemented actions are offered. User-restored layout remains authoritative.

Browser and Patterns are adjacent static sidebars under the
[shared left-zone contract](WORKSPACE.md#shared-left-panel-zone), rather than movable pane windows.
Their folder/sound and phrase-card icons form the rail's top group, separated from musical-window
actions by space and a quiet shared boundary. Group order follows Browser then Patterns. Active
styling communicates independent visibility. Captions remain quiet; each exposed right edge is the
only resize affordance. Below usable content widths, clipping communicates pushing the panel inward
without crushing controls or closing early. The owner screenshots and inspected local Ableton Browser
and Bitwig references support this spatial hierarchy; no reference graphics are distributed.

## Implemented R5-F1 Channel Rack composition

A pristine layout opens Rack and Playlist in the adaptive music composition, without inserting music. At wider
window sizes, a project navigator lists actual playback occurrences with canonical rhythm thumbnails;
it creates/selects Patterns. Narrow windows reclaim that space and retain the occurrence selector.
The compact Pattern name/header, length selector, sixteenth-note ruler and fixed row geometry make
the rhythm readable as music. Whole-bar extension repeats four beat groups without shrinking pads.
Pads are compact fixed squares, not width-filling cells. Narrow panes scroll horizontally; larger
beat and bar gaps retain hierarchy. Inner caps, bevels, hover highlights and pressed shade/recess
provide tactile states in both themes. Rich-note previews continue to fill their row. Pattern cards
reserve a separate scrollbar gutter so their right edges remain intact.
The 16–512 sculpted pads share a channel tint, active indicator and glow. The actual current column
uses the shared playing outline independently of checked state and keyboard focus; the footer gives
bar:beat:sixteenth position. Pattern deletion and channel menu deletion use ordinary danger/action
styles with target-aware Undo; the navigation rail toggle reclaims space.
Larger hit targets, persistent names and short contextual commands replace repeated step captions.
The scrollable grid preserves readable geometry when the pane is narrow; its local header/footer
retain creation/import access. Empty and unsupported states reflect actual project content.

Captured dragging paints or erases a continuous row, previews locally and commits once on release.
Escape/capture loss cancels. Row actions fill every four/two steps, rotate or clear through one
canonical transaction. Single-WAV drop and Add sound share managed acceptance. Arrows/Space/Enter
retain accessible step editing. Existing unsupported data is preserved, not converted to fit the UI.
The transport BPM opens an exact committed editor; meter remains a value. A physical Gain dial
complements exact amplitude and the level slider in Node Editor. No second parameter/history exists.
The transfer curve visualizes the actual mathematical setting; it does not pretend to measure audio.

## SEQ-UI-REDESIGN reference assessment

All eleven owner-supplied images were inspected. They are retained outside the repository at
`%LOCALAPPDATA%/Seqvium/DesignReferences/SEQ-UI-REDESIGN`; no reference assets are distributed.
The [review report](experiments/SEQ-UI-REDESIGN_REPORT.md) records implementation/evidence/limits.

| Reference                            | Useful design decisions                                                                           | Adaptation / limitation                                                                              |
|--------------------------------------|---------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------|
| Screenshot 1, SONAR step instrument  | Bevelled drum pads, beat divisions, lit selected hits, substantial chassis                        | Keep tactile rhythm; enlarge tiny targets and remove repeated control labels                         |
| Screenshot 2, SONAR expanded channel | Parameter controls beside a channel and an expandable velocity lane                               | Contextual row commands and SEQ-R9 aligned velocity now cover the bounded shared-note scope          |
| Screenshot 3, FL Studio              | Fast Channel Rack, compact transport, colored steps, instrument identity, graphical note overview | Rail/navigator and real rhythm thumbnails; avoid copying layout or adding unavailable channel knobs  |
| Screenshot 4, EON ARP                | Strong purple identity, coherent illuminated matrix, graphical musical focus                      | Transfer rhythm clarity and restrained light; preserve Seqvium's own palette                         |
| Screenshot 5, blue arpeggiator       | Beat grouping and visually distinct parameter lanes                                               | Reserve lanes for real parameters; do not preallocate disabled lanes                                 |
| Screenshots 6–7, Logic               | Track colors repeated in the grid, clear time ruler, integrated editing hierarchy                 | Shared Rack colors, real Playlist and SEQ-R9 Piano Roll; deeper integration remains separate         |
| Screenshot 8, omniGRID               | Dense legible rhythm and contextual per-note depth                                                | Take direct editing and disclosure; avoid microscopic text and arbitrary knob inventory              |
| FabFilter Pro-Q 4 image              | Large purposeful analytical surface, curves, quiet tools around the signal                        | Actual Gain transfer fill/glow now; live spectrum/EQ require real processing and measurement         |
| Serum image                          | Layered instrument chassis, finely graduated knobs, shape/value feedback                          | Graduated captured Gain dial; future instruments inherit shared materials with their own composition |
| Reason rack image                    | Physical panel depth, coherent rack hardware, color/material character                            | Raised panels, bevels and light; decorative hardware must not displace musical work                  |

The references justify material richness. Their tiny controls, cryptic abbreviations and excessive
simultaneous options are not requirements. Seqvium keeps their musical character while improving
target size, naming, disclosure, cancellation and canonical editing safety. Final visual acceptance
belongs to the owner; screenshots and automated checks establish evidence, not aesthetic approval.

## Playlist reference assessment and visual contract

Local Audio Clip stretch reuses compact exact properties: source range, explicit percentage and
one playback duration. A non-unity `↔` percentage in the clip header distinguishes it from edge trim
and repeat count. The existing local FL Playlist and waveform-editor images were inspected for
this change; their identifiable audio bodies and compact contextual controls inform the composition.
No reference graphics are distributed. The property/error and empty/populated window review covers
RU/EN × Dark/Light at ordinary/reduced size; detailed results belong to the
[stretch report](experiments/SEQ-R10-STRETCH-F1_REPORT.md).

The owner-supplied five timeline images were inspected and moved from Desktop/screens to
`%LOCALAPPDATA%/Seqvium/DesignReferences/SEQ-R10-PLAYLIST`. Earlier Logic, Reason, Serum and FabFilter
references were also reviewed for hierarchy/materials. These are private local visual targets, not
distributed application assets; the implementation uses original vectors and shared host resources.

| Reference                                               | Strong transferable solution                                                                                | Implemented / planned boundary                                                                                                                                                      |
|---------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `playlist_main.png`, annotated FL Playlist              | Source picker distinct from tracks, musical ruler, overview/scroll, clip notes, clear contextual tools      | Source selection, shared clips, overview, snap, Ctrl-copy, repeats, M/S and retained pane are real; seek/range/group editing now work; markers/automation/audio clips are follow-up |
| `Screenshot-2024-10-04-at-1.47.57-PM.png.webp`, FL song | Track colors and repeated clip headers connect the overview to the song; named parts make structure legible | Named/color tracks, actual duration/pitch thumbnails, stationary headings and muted dimming; dynamic groups and section markers remain planned                                      |
| `1.-Color.jpg`, SONAR                                   | Strong colored track identity, substantial controls, visible editing tools and waveform/fade detail         | Adapt header/material depth and real gates; waveform trim/fades require supported audio-clip semantics, not decorative waveforms                                                    |
| `VJPS1X.jpg`, Cubase                                    | Stable inspector/track column, musical range, organized groups, precise source editor and measured Master   | Fixed headings, committed contextual track settings and scalable view now; musical range now works; groups/sample editor/Master measurement require their owners                    |
| `daw-software-editing.jpg`, stylized DAW illustration   | Rapid visual distinction of musical layers and a clear primary working area                                 | Adapt color hierarchy only; it is an illustration, not pixel/interaction authority or permission for simulated Mixer/meters                                                         |
| Earlier Logic / Reason / Serum / FabFilter              | Rhythmic clarity, purposeful graphical surfaces, layered instrument material and restrained light           | Continue shared tactile controls and data-derived previews; processors/spectrum/instruments must be backed by actual capability                                                     |

Playlist is a recessed time surface with substantial fixed track headers and a compact tool area.
Clip headers carry source names, bodies show real note position/duration/pitch, track tint connects the
header, clip and overview. Selection uses an independent jade boundary; Mute/non-Solo uses dimming
without erasing the content. A ruler separates bars from beats and the renderer cursor remains distinct.
The overview shows actual occupied time and a viewport frame; it is navigation, not waveform/audio level.
Panel/bevel materials come from HostMaterials; all colors, type and hit geometry come from the shared
system. Color chips render visible palette content inside normal buttons. Both themes and narrow layouts
must keep labels, real field text, targets and canonical content readable without whole-window scroll.

Keep unfamiliar processing/state detail behind deliberate contextual editors. Do not duplicate transport
owners, turn a spatial track into a compulsory instrument/Mixer channel, or show disabled imitations of
tools from mature DAWs. Prior claims that Arrangement is wholly absent are superseded by this bounded
Playlist; [UX](UX_CONTRACT.md#implemented-playlist-arrangement) owns behavior and
[R10 planning](ROADMAP.md#seq-r10--arrangement) separates the next capabilities. Owner visual acceptance
remains separate from verified rendering and functional evidence.

## Professional DAW Workspace Design Target

This is an accepted long-term visual direction, not authorization to implement future stages or a
pixel-perfect design system. It complements [project vision](PROJECT_VISION.md),
[workspace behavior](WORKSPACE.md) and [UX](UX_CONTRACT.md); their ownership and accessibility contracts
remain intact. Owner-supplied FL Studio, Cubase, SunVox and modern FL Studio images were inspected as
complementary references. They are research context only: no reference images or proprietary assets
are copied into the repository, and no application's palette, icons or exact layout is adopted.

## First-party Visual Design Rules

These rules apply to all subsequent Seqvium first-party surfaces unless an explicit, justified UX
contract requires an exception. They extend the accepted R3 frame and pane behavior; they do not
authorize future editors or choose their layouts.

- **Workspace first:** every persistent field, row and action must justify its space. Keep musical
  working regions dominant; no decorative cards, welcome dashboards or future-editor placeholders.
- **One main command row:** group Project, Transport, History and Selection/parameter actions with
  restrained separators. Horizontal shortage uses explicit overflow/menu and contextual disclosure;
  never automatically multiply permanent toolbar rows or shrink fonts. Import, Play/Stop and access
  to Save must remain reachable at 640x480 DIP. Undo/Redo remain compact icon-only actions.
- **Global musical parameters:** show actual tempo and meter compactly near transport when space
  permits. BPM opens its real committed editor; meter remains read-only. Overflow/Inspector retains
  access at reduced width. Future meter editing should preserve that familiar location.
- **Icons:** use icon-only for recognizable actions, with localized accessible names, tooltips and
  shortcut hints; explain disabled reasons. Ambiguous commands may have short menu labels. Reusable
  first-party vectors share geometry, stroke weight, rounded joins, 16–20 DIP readability and platform
  DPI scaling. No emoji, font-dependent action glyphs, mixed icon families or borrowed application assets.
- **Semantic hierarchy:** Dark uses graphite surfaces and readable light text; Light uses cool neutral
  backgrounds and readable dark text. Central resources distinguish selection, keyboard focus, hover,
  pressed, unavailable, active transport, warning and critical actions. Semantic action
  accents, green transport, amber warning and moderate red critical feedback have distinct purposes.
  Jade action accents and channel colors now identify Seqvium. Color supplements shape/text/state.
  Material depth, shadows, glow and gradients are supported expressive tools; signal displays must
  represent actual data or clearly labelled mathematical relationships.
  Unselected working elements must remain distinguishable from their surrounding surface in both
  themes. Use modest surface separation and readable boundaries; selection/focus is additional
  emphasis, not the only way to locate a node or control. Review the actual canvas composition.
- **First-party dialog chrome:** compact internal confirmations, errors, properties and tools follow
  Seqvium typography, semantic surfaces, borders, control states and spacing. Compact owned modals
  have no ordinary Windows titlebar and no duplicate decorative header. Size height from content,
  center on owner, preserve true modality, cyclic Tab, safe initial focus, Esc/Alt+F4 and focus return.
  Save/Discard/Cancel are visibly distinct; initial focus/default must be safe. Native system file
  pickers are the explicit exception. External independently rendered plugin editors retain the
  [workspace exception](WORKSPACE.md#one-main-application-window).
- **Human labels:** ordinary surfaces show musical/source names and concise actual chain labels.
  UUIDs, internal capability keys, execution provenance and technical device details require explicit
  details/Inspector/diagnostic disclosure. Persistent blockers and canonical/last-valid divergence
  remain visible and meaningful; diagnostics must not dominate the music workspace.
- **Status economy:** use one compact source in the existing status bar. Normal state needs little
  text; failures say what is blocked/retained and expose useful details on request. Avoid duplicate
  opened/ready/inactive explanations in permanent rows.
- **Keyboard/accessibility:** preserve shortcuts, automation identity, legible focus and useful hit
  targets. Overflow preserves actions and focus return; visual density never removes functional access.
- **Field alignment:** parameter labels, editable text, units and actions share one vertical center
  line and coherent typography. Verify the actual inner template/text presenter, rather than adding
  compensating top margins. Parent controls must forward the intended content alignment; new rows use
  the [common row rules and check](DESIGN_SYSTEM.md#field-and-row-alignment).
- **Real GUI review:** inspect rendered windows at 1100x750, 640x480 and maximized/1920x1080 with
  RU/EN, Dark/Light, normal/blocked actions, tooltips, focus, native picker return and owned modals.
  Exercise pane collapse/restore/dock/hide/reopen and real document actions. Unit tests alone are not
  visual acceptance; retain review screenshots locally under the existing evidence policy.

R4-F3-A implements `Presentation/HostIcons`, `Styles/HostControls.axaml` and shared palette roles
`Surface.Hover/Pressed/Selected/Playing/Critical/Accent`, `Status.Playing/Critical`, extending existing
Dark/Light resources. A single retained row presents real project/transport/history actions, selection
and linear Gain (0–1); BPM is editable and meter remains read-only, with narrow-window overflow access. Save As lives
in overflow; selection moves into details when space is limited. The content-sized owned unsaved modal
uses the same resources. The details flyout is a compact view of canonical intent, **not a node canvas**.
The owner conditionally accepts F3-A for continued development; final canvas/interaction acceptance
of F3-B remains pending. See the
[integration evidence](experiments/SEQ-R4-F3-A_REPORT.md).

### Transferable reference principles

- FL Studio: flexible internal composition, compact global controls and simultaneous specialized music
  editing surfaces; take coexistence and fast contextual access, not an exact window arrangement.
- Cubase: arrangement-first hierarchy, useful track density, detailed time structure and integrated lower
  working areas; take meaningful allocation between overview/editing/mixing, not mandatory fixed panes.
- SunVox: compact modular visualization with explicit meaningful node relationships, a distinct inspector
  and little decorative overhead. Its extreme tracker text density is not the application's default
  typography or an instruction to make a tracker/editor now.
- Modern FL Studio: coherent Browser/Arrangement/Mixer composition, shared visual language and useful
  horizontal/vertical space allocation. Do not copy promotional Browser artwork, proprietary assets,
  persistent developer counters or the particular control inventory.

No referenced feature must be implemented or visible by default. Purposeful color grouping, strong
hierarchy and progressive disclosure are shared principles; copying a populated screenshot is not.

### Workspace-first composition and functional density

The workspace is the primary product surface. Persistent chrome, navigation, status and preferences use
only a small justified fraction of available space. Musical content takes precedence over documentation,
development status, onboarding and configuration. Empty workspace is preferable to decorative placeholders.

Favor compact predictable controls, toolbars, tracks, inspectors and editors. Reject oversized cards,
large corner radii, excessive padding/whitespace, repeated explanations, giant ordinary-object headings,
persistent unavailable-future-feature prose and primary-screen framework/version/build details.
Density must preserve readable text, keyboard access, useful pointer targets and correct platform DPI.
Choose dimensions deliberately from hierarchy/content, never microscopic typography or indiscriminate
whole-application scaling. Accessibility and readable minimums are constraints, not density tradeoffs
to silently discard. Shared metrics evolve with verified composition; early shell dimensions are not immutable
constraints.

### Specialized musical surfaces and multi-pane composition

Pattern, Piano Roll, Arrangement, Mixer and Graph, plus future Browser and Sample Lab, share a coherent host
language while keeping task-specific interaction and useful density:

- Arrangement prioritizes tracks, clips, time structure and editing.
- Piano Roll prioritizes notes, pitch/time grids and velocity/parameter editing.
- Mixer prioritizes compact channels, levels and processing controls.
- Browser prioritizes navigable sources and fast audition.
- Graph prioritizes readable nodes, ports, meaningful connections and spatial navigation.
- Pattern and Sample Lab prioritize their actual musical/exploration actions and supported context.

Do not force every surface into identical cards, lists or generic forms. A pane is a substantial working
surface, not a decorative container. The long-term single-main-window workspace must accommodate useful
overlap/floating, clear activation/front behavior, independent sizing, collapse/restore and later
user-controlled docking/magnetism under [WORKSPACE](WORKSPACE.md#flexible-composition-and-activation).
Not every editor must remain visible. Layout preferences remain separate from canonical music.
The bounded F2 host above implements this direction for two optional host surfaces; it grants no
authorization for specialized musical editors or a general docking hierarchy/registry.

### Stable responsive frame

Design deliberately for ordinary desktop work, including one 1920x1080 display, smaller windows and
platform DPI changes within declared evidence. Prefer reflow of small command groups, collapsing
secondary panels, pane-local scrolling, resizable working regions, contextual tools and bounded usable
minimums. The primary application frame stays stable: no default whole-window scrolling, permanent
large headers, uniform scale transforms or unreachable offscreen work areas. Calibrate typography and
control geometry by function within the shared desktop density system. Actual timelines,
notes, channels and other editor content may scroll horizontally/vertically where musically appropriate.

### Visual hierarchy, color and control economy

Use deliberate instrument materials, substantial panels, bevelled controls, bounded light and shadow,
readable boundaries and consistent typography. Rich decoration is welcome when it reinforces the
instrument's character and preserves manipulation and visual hierarchy. Dark and Light both require
expressive musical color, material depth and sustained-working legibility. Distinguish active context,
selected material, keyboard focus and disabled/unavailable
states. Sparse accents and purposeful musical colors explain track/clip/pattern/context organization,
activity and relationships, rather than merely decorate. Essential information also needs non-color cues.

Keep frequent actions compact and close to the relevant musical context. Advanced actions may use
context menus, secondary panels or inspectors. Do not give every preference a large button and paragraph;
do not hide essential workflows solely to make screenshots clean. Progressive disclosure controls
complexity without making frequent work laborious or the interface a settings application.

### Honest functionality and GUI review

Never fabricate tracks, editable notes, transport, meters or processing controls ahead of implementation.
A sparse structurally correct R3-F1 host is acceptable; visual richness must arrive from actual useful
functionality. No musical domain change or reopening of R1/R2 follows from these references.

For each substantial GUI change, answer through actual rendered windows/screenshots and interaction:

1. What proportion of visible space is available for actual musical work?
2. Does persistent chrome justify the space it occupies?
3. Are common controls compact and immediately reachable?
4. Can relevant working surfaces coexist without excessive obstruction?
5. Does resizing preserve reachable content and useful minimum dimensions?
6. Is scrolling local to the appropriate musical surface?
7. Is layout understandable with RU/EN text and Dark/Light themes?
8. Are focus, selection, disabled state and active musical context distinguishable?
9. Are elements functional, rather than decoration or future-development descriptions?
10. Does this support sustained music production rather than occasional configuration?

When a substantial editor arrives, review representative dense and sparse content, not only its empty
default. Unit tests are necessary evidence for applicable invariants, not visual acceptance. Record actual
OS/DPI/size/input bounds and missing evidence. Owner visual review remains required; the recorded R3-F1
visual observations describe reserved workspace capacity, not musical work or multi-pane acceptance.
Generated screenshots remain local under [evidence retention](experiments/README.md#evidence-retention),
not product assets or a default Git deliverable.

## Hierarchy, chrome, and density

- Let musical material and the current action lead the hierarchy. Selection and active context should
  be clear without making infrastructure the main visual object.
- Pane chrome should make movement, activation, and available actions discoverable while remaining
  quiet enough for music to dominate. [WORKSPACE](WORKSPACE.md) owns pane behavior.
- Use compact controls for ordinary work and reveal expanded depth deliberately. Compactness must
  preserve readability and useful interaction targets, not compress everything into dense machinery.
- Keep selection, focus, parameter feedback, and pane conventions coherent between surfaces. Different
  creative tasks may need different density without looking like unrelated applications.
- Avoid engineer-tool aesthetics: walls of permanent connectors, diagnostics, and configuration
  fields must not define the ordinary musical workspace.

### Sample Lab interaction hierarchy

Lead with the sound and the next creative action: generation, audition, related variation and explicit
acceptance. Recent comparison/history helps return to useful alternatives without making a branching
tree or parameter console the primary surface. Reveal meaningful supported locks and deeper controls
on demand, with held constraints and their applicability legible. Reference, audible choice, current
project sound and acceptance scope need distinct understandable feedback; color alone cannot carry
lock, stale, pending or unavailable meaning. Temporary history must not look permanently saved.
[Sample workflow](SAMPLE_WORKFLOW.md#intentional-and-lazy-exploration) owns behavior and
[UX](UX_CONTRACT.md#sample-lab-exploration-feedback) owns action/discoverability requirements. Use the
host localization and semantic theme foundation; no final layout, control inventory, icons or gestures
are selected.

### Full node editor visual proposal

The implemented Drum editor continuously scales its dials from 76 to 64 DIP as its allocated width or
height shrinks, independently of column count. Cells contract before wrapping with hysteresis, so
reducing width cannot enlarge a knob. The analytical graph varies within 180–250 DIP. Numeric fields remain readable.
Spacious Play and sample Stop stay outside parameter scrolling. At very short pane heights vertical
overflow preserves access; resizing does not change geometry in the middle of a captured edit.

**SEQ-NODE-EDITOR-IMPL-F1 — bounded implementation of the accepted first slice.** The first-party editor
has a compact scope/identity row, a task-specific working region and restrained applicable
status. A real EQ curve, instrument controls or analyzer can occupy substantial space; generic cards
or identical generated forms must not limit those workflows. Tabs/presets belong only when functional
for the supported node. [Graph](NODE_GRAPH.md#full-node-editors-proposal) owns capability and the useful
Gain attenuation/transfer-graph slice; [Workspace](WORKSPACE.md#node-editor-placement-proposal) owns
placement and retained sizing.

Gain uses its node name once in the pane title, a short source/scope line, and a graduated physical
dial beside the level slider, exact linear value and derived dB. The dial supports relative clockwise
rotation to increase, counterclockwise to decrease, Shift fine adjustment, double-click unity,
arrows/Home/End and cancellation; all surfaces
share the same canonical parameter and one draft/commit owner. Unity/Silence sit below the level control. The labelled
analytical
transfer graph is a compact, initially collapsed Curve disclosure; its formula is available on
request. This deliberately refines DESIGN-R1 following owner review: primary musical controls take
precedence over permanently displayed teaching text and a large graph. The graph remains distinct
from live signal metering. Preserve semantic RU/EN and Dark/Light resources,
usable controls, independent focus/selection/target cues and local reflow/scrolling at reduced sizes.
Do not enlarge a single value into an empty major surface or show future-feature placeholders. Final
default/minimum dimensions require rendered layout and interaction evidence; an analytical graph is
not evidence for a live analyzer or a future EQ's usability.

Each empty/unavailable pane has at most one useful content hint. GraphCanvas owns its empty graph
message; an empty selector and footer are hidden. Node Editor hides absent identity/scope/status rows
and keeps one short content hint, with a distinct warning for a deleted target. Healthy Gain needs no
idle explanatory footer: reserve that region for drafts, changed-elsewhere feedback, diagnostics and
matching-scope last-valid state. The main toolbar hides occurrence/Gain controls when their data is
absent; an explicit compact selection in Details does not repeat a static selection heading.
Invalid amplitude remains visible in the exact input with a warning and repair path. Hide the
undefined dB/curve instead of repeating an invalid-value label in every control. A draft uses a short
label and a dashed curve; explanatory release/commit text need not occupy the working area.
Changed-elsewhere feedback appears only when an intervening accepted edit cancels a real draft,
and expires after three seconds. Initial loading, idle refresh and own commits do not create it.
For the same four-part target and parameter, the visible expanded Node Editor takes priority over
Graph quick Gain and the toolbar. Otherwise the visible Graph's selected Gain takes priority over
the toolbar's same Gain. Different processing targets retain their controls; hiding/collapsing the
editor restores quick access. Healthy canonical readiness and selected-node facts need no footer.

## Responsive layout and usable minimums

First-party Seqvium UI uses responsive/reflowing layout, not uniform graphical scaling of the
application. Prefer reflow, collapsing secondary content, pane-local scrolling/overflow, resizing flexible
regions, and preserving minimum usable sizes as available space changes.

Do not continuously shrink fonts, icons, mixer strips, knobs, click/touch targets, or other primary
controls below readable/usable minimums to fit more content. A mixer strip may need a minimum useful
width and horizontal scrolling rather than becoming an unreadable sliver. Exact dimensions and
breakpoints remain future visual-design work. Correct platform DPI scaling is still required and is
distinct from shrinking the UI to fit a window. Independently rendered third-party editors are outside
this visual contract; [WORKSPACE](WORKSPACE.md) owns main-window and pane behavior.

Validate content at its supported pane minimum as well as the main-window minimum in both languages
and themes. Check long/zero/invalid values and disclosed content, not only nominal values. Measure
actual template minimums and scroll extents: a compact idle tool must not acquire a scrollbar or
clip an action because invisible template space remains. Disclosures must stretch their working
content to usable width. Preserve deliberate manual resizing and provide scrolling when expanded
content exceeds a user-selected size, without shrinking labels or input targets.

## Processing and node-canvas presentation

F3-B implements a compact first-party canvas using 156-unit nodes, 22-unit port rows, bounded curved
cables with surface under-strokes at crossings and restrained 3-unit body rounding. Source, Gain,
Mix and Output have human type labels, directed input/output labels and source/parameter/layout detail.
Selection, keyboard focus, previews and invalid elements have distinct outlines/text/semantic colors;
an exclamation mark supplements error color. Hover/context feedback explains affected diagnostics.
`Graph.Cable`, `Graph.Grid`, `Graph.Node` and `Graph.NodeBorder` extend HostPalette equally for Dark/Light.
Unselected nodes use a slightly lighter Dark fill, white Light fill and defined boundaries so they
remain visible against the grid. The pane-local row contains explicit target, create/import/frame/delete/context
actions;
the main command row remains unchanged. Gain exposes one compact invariant text input on selection,
with linear ratio semantics, retained invalid intent and explicit commit. Technical IDs stay in the
existing requested details only. Pan/zoom affect canvas content, never global typography or document
coordinates; framing a dense graph is an overview, and ordinary zoom/pan retains access at narrow width.
[F3-B GUI evidence](experiments/SEQ-R4-F3-B_REPORT.md) records actual sparse/dense RU/EN Dark/Light
review, input and window-size bounds. Owner visual/manipulation acceptance remains pending.

Processing presence should have a compact, clearly interactive indicator on relevant musical items
and containers. Its exact icon, badge/count, color, label, typography, and motion remain unselected.
The indicator reveals the relevant graph pane under [UX_CONTRACT](UX_CONTRACT.md); full node maps
normally remain hidden until requested.

The canvas should support free two-dimensional node placement, readable connections, and clear
selection. Personal spatial organization is presentation state; [NODE_GRAPH](NODE_GRAPH.md) owns
topology-defined processing. Node settings should offer depth on demand. The compact-chain proposal,
settings/inspector placement, and optional parameter exposure are not final visual contracts.

## Feedback and motion

R6 keeps local packages inside Browser as a compact optional disclosure: sound access leads,
identity/version/purpose and availability support it, and Enable/Disable remain secondary. Imported
audio preservation and active-use refusal are explained in text. Diagnostics and reduced-height
scrolling use the existing visual system; no Marketplace, administrative dashboard or placeholder
Sample Lab surface is added. RU/EN and both themes share this composition.

Make keyboard focus, document selection, active pane and relevant command target sufficiently legible
without treating them as one visual state. Essential invalid/unavailable state and pending operations
need non-color/structural cues where applicable, such as shape/outline, labels, icons and concise
context explanation. Color and transient animation may supplement these cues but cannot be their
only carrier. Pane chrome, controls and shortcut hints should support discoverable keyboard access
under [UX](UX_CONTRACT.md#essential-accessibility-feedback) and
[WORKSPACE](WORKSPACE.md#keyboard-access-and-focus-return). Exact focus visuals, contrast/dimensions,
accessibility-tree representation and APIs need later design/evidence; no certification is claimed.

Invalid or unpublished graph edits must clearly differ from the graph currently executing under
[NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution). Use concise status/iconography,
affected node/connection highlighting, restrained color with non-color cues, and concise inline/context
explanations. Persistent missing-capability and execution blockers remain visible at affected elements;
optional global count/navigation may help reach them. Disappearing notifications are supplementary,
logs are diagnostic support, and the workspace must not become a diagnostics dashboard. Keep feedback
visible without interrupting normal editing with cascading modal
dialogs; [UX_CONTRACT](UX_CONTRACT.md#graph-state-and-recoverable-failures) owns exceptions
where a modal decision is genuinely required. Exact visuals remain open.

Use immediate visual/audio feedback to make manipulation understandable and invite experimentation.
Motion, where useful, should explain a change, continuity, or state with restraint. Coherent interaction
quality is the desired "vibe"; decorative animation and visual noise are not requirements.

[UX_CONTRACT](UX_CONTRACT.md) owns observable interaction and discoverability. Future visual work
should develop this guide from concrete workflows rather than fill in a complete theme spec prematurely.

## Global settings composition

The Settings center uses compact left navigation, ordinary shared controls, aligned preference rows
and local content scrolling. General, Controls and About are implemented; no empty future pages.
Keyboard rows lead with the action and assigned chord, with a small default action only for custom bindings. A dot/bold chord marks an effective custom
assignment; default rows have no reset icon. Search and a
short temporary capture/conflict area provide depth without permanent instructions. Fixed-key details
stay behind disclosure. About retains the brand mark/wordmark/tagline, runtime version and short scope.
Toolbar chrome offers Settings and Panes; the small title-row sun/moon opens explicit checked theme
selection, with a disclosure chevron and Choose theme tooltip. [Settings](SETTINGS.md#unified-global-settings-center) owns routing and behavior.

The owner's four Desktop screenshots and local WORK-EQ-001 (FabFilter), WORK-OT-001 (Reason) and
WORK-CR-001 (SONAR) images were inspected for this composition. Transfer compact action density,
quiet chrome, consistent rows and contextual depth; none of these private screenshots is a product asset.

## Themes and semantic resources

Dark and Light themes are baseline product directions, with room for additional restrained visual
themes later. A theme is primarily a coherent resource/style set, not a complete novelty UI redesign.
First-party Seqvium UI and Seqvium-native plugin surfaces consume centralized semantic UI resources
rather than hard-code concrete colors/fonts throughout individual plugins.

The R3 shell needs a small host-owned semantic foundation sufficient for the surfaces actually built:

- Surface background and readable primary/secondary text establish hierarchy.
- Selection, focus and outlines distinguish selected targets, keyboard focus and boundaries.
- Accent/action emphasis and warning/error, disabled/unavailable and pending-state treatment support
  the states exposed by the shell; later panes extend this vocabulary only where actual workflows need it.
- Common control sizing/spacing and typography roles preserve readable hierarchy and usable targets
  under the responsive-layout contract.

The implemented v2 vocabulary is cataloged in DESIGN_SYSTEM, with source-owned colors, dimensions,
typography and material recipes. These host keys are not a public extension API. Reusable icon/resource
roles may grow with real editors; no unused future parameter inventory is required in advance.

Dark/Light provide coherent alternative resources for the same UI semantics, not different document
models. Main workspace chrome, Browser/project-resource surfaces, node graph, Sample Lab and first-party
Seqvium-native extension UI share the host roles as those surfaces arrive. Extensions should not
independently recreate each Dark/Light style. Justified future plugin customization may coexist with
the shared foundation; it does not establish a parallel mandatory style system.

Theme changes preserve musical state
under [Architecture](ARCHITECTURE.md#preference-changes-and-locale-aware-presentation)
and observable preference-change safety under [UX](UX_CONTRACT.md#language-and-theme-preference-changes).
Selection, focus, warnings/errors, invalid graph state, disabled actions and pending work retain sufficient
structural/non-color cues under [feedback](#feedback-and-motion). Color alone cannot carry essential
meaning. No screen-reader compatibility, contrast certification or platform validation is claimed.

Host baseline roles must remain usable when a noncritical contributed style/icon is missing or invalid:
use a safe host role, simple host indicator or meaningful label rather than an unreadable/broken essential
control. A complete unavailable contributed theme falls back coherently to a usable host baseline;
the exact choice and preference-retention mechanism remain open. Contributor removal follows
[Extensions](EXTENSIONS.md#ui-resource-contribution-lifecycle); arbitrary malformed external UI is not
promised recoverable. Independently rendered third-party native editors may keep their own theme,
language and visual behavior; surrounding host UI still uses host resources.

Theme packaging as extensions/plugins/data packages, exact resource/API/token schema and visual choices
remain Q-055; additional restrained themes are possible without promising a marketplace, third-party
theme SDK, dynamic compiler, live reload or customization editor. Host localization ownership belongs
to [ARCHITECTURE](ARCHITECTURE.md#host-localization-and-ui-resources); staged introduction belongs to
[Roadmap](ROADMAP.md#localization-and-theme-foundation-ownership). No theme packages or localization
extension resources are introduced by this direction; R3-F1's first-party catalogs/palettes are implemented above.
