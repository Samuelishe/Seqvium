# UX contract

Role: Product contract for observable interaction.
Read when: Designing editing, audition, workspace behavior, or user-facing actions.
Authoritative for: Progressive complexity, input/command and focus semantics, baseline accessibility expectations,
feedback, discoverability, general workflow semantics.
Not authoritative for: Pixel-perfect design, DSP behavior, serialization, or current implementation.

These are intended requirements except the explicitly implemented bounded R3 host, F3-A/F3-B workflows
and the implemented Rack, Playlist and Piano Roll sections below; broader capabilities remain future work.

## Working-surface discipline

Across Seqvium, ordinary working surfaces must not contain decorative explanations, service or
architecture descriptions, instructions for obvious actions or duplicate labels. Musical editors
must feel like finished instruments rather than automatically generated parameter forms. Use
icon-first controls for short actions only when the icon is recognizable; retain necessary parameter
names, musical values, actionable errors and unavailability messages as text. Put optional guidance
in short tooltips, contextual help and accessible names, not permanent workspace paragraphs. An
ambiguous icon needs a clearer metaphor or concise visible label, not forced icon-only presentation.
Icon and text must form one optically balanced control. Review actual glyphs, ink, baselines, spacing,
contrast and states in rendered images; matching bounding boxes is insufficient. Localization is part
of design: RU and EN each require real composition review, including long names and errors.

## Implemented Pattern resampling

A Pattern navigator card's **Create sample** context action captures that explicit local occurrence,
not the current transport mix. Temporary rendering progress and Cancel stay beside the Pattern list;
there is no separate render editor, file picker or routing questionnaire. Success opens Browser's
Project resources with the new sample selected, waveform/preview and existing Rack/Playlist use.
Delayed search notifications must not clear this reveal. The source remains editable; no replacement
or automatic placement is implied. Render refusal names a supported reason without accepting partial
audio. A changed project discards the old request and asks for a fresh render. Selection does not
retarget it. Errors/progress use shared localized controls and can be dismissed.
[Sample workflow](SAMPLE_WORKFLOW.md#implemented-pattern-resampling-seq-r8) owns exact bounds.

## Implemented Sample Lab V1

The rail/Panes opens standalone exploration; a PCM Rack channel menu captures contextual identity.
Generate and Variation are separate intentions. History selection, explicit reference, audition mode
and acceptance destination remain distinct. Shape/locks are disclosed; ordinary work presents short
commands, actual waveform and sound names. No permanent tutorial or recipe/debug dump occupies the
working surface. Keep/+Rack/+Playlist preserve other candidates; Replace shared names its real scope.
Solo requires stopped transport; In music requires the supported target Pattern/Song to be playing.
Original restores current canonical sound without accepting or editing anything. Unavailable/stale
targets remain understandable and never follow another channel's selection. Bounds and lifecycle
are owned by [Sample workflow](SAMPLE_WORKFLOW.md#implemented-sample-lab-v1).

## Implemented Sample Browser

The **Extensions** disclosure lists local package names/versions and availability, with specific
compatibility/file/lifecycle diagnostics on inspection. **Sounds** opens an enabled pack in this
same Browser. **Disable** hides new uses and explicitly preserves imported audio; an in-use module
refuses disable with finish/cancel/close-editor guidance. **Enable** restores a compatible contribution.
Missing optional translation uses English without changing the host language. Small windows retain
the sound list with local disclosure/preview scrolling; closing the disclosure returns musical space.

Rail/Panes or Ctrl+B opens Browser. Choose Project resources, a pinned folder or recent location;
Add folder uses the native folder picker. The address field opens an explicit path on Enter.
Double-click/Enter enters a folder; parent, Back/Alt+Left and Ctrl+R refresh complement the mouse actions.
F6/Shift+F6 retain workspace pane traversal, including while Browser has focus.
Ctrl+F focuses local name search; arrow keys select files. Search applies to the current folder only;
large directories expose a bounded **Load next page**, rather than silently omitting the remainder.

Selection reveals name, format, actual duration/rate/channels and a real waveform, with full names/paths
in tooltips. Space/Enter or Play previews; Auto preview repeats that action for subsequent selections.
Stop/Escape cancels temporary listening. Music already playing keeps playing; Browser explains why
solo preview is unavailable. Browsing/preview are never import, dirty or Undo actions. Corrupt/missing
sources and cancellation preserve the project and leave navigation usable.

Drag a sound into Rack or Playlist, or use the visible **+ Rack / + Playlist** buttons. Tooltips identify
the selected Pattern and the button's Song-cursor/unused-lane destination. Rack's bounded WAV support
and shared/custom-graph refusals stay explicit; Playlist also accepts supported Windows MP3/FLAC and
managed normalized audio. Successful uses are atomic history edits and survive loss of external
originals after Save/Open/Save As. Project-resource reuse refers to accepted media without another copy.
[Sample workflow](SAMPLE_WORKFLOW.md#implemented-seq-sample-browser) owns format/lifetime details.

## Implemented Mixer workflow

Mixer opens independently of Playlist. Existing projects start without it; explicit Create Mixer
creates unity strips grouped by equal Part names, with visible editable sound assignments. A shared
sound routes consistently across its Pattern/Playlist occurrences. Newly unassigned sounds feed Master;
creating a bus does not silently reroute anything. Rack's Open Mixer action targets its sound channel.
Output/send selectors expose real routes; cyclic intent fails without history or partial publication.

The bank offers dB faders (-90 silence through +12), bipolar stereo balance, Mute/Solo, measured stereo
levels/peak hold and a compact EQ/compression/Delay/Reverb summary. Master stays locatable. Contextual inspector
provides trim (-24 through +36), editable EQ response, exact filter/dynamics controls, output, pre/post
sends, name, assignments and channel deletion. Curve means analytical response, not live spectrum.
Peak/GR readouts derive from the audio engine; stopped meters are not animated. Reset peaks does not
alter music or consume Undo. Master sample-peak protection is explicit and saved.

The Delay inspector tab offers Enabled, Time (10–1000 ms), Feedback (0–75%) and Mix (0–100%).
Defaults are disabled, 250 ms, 35% and 25%. Each valid exact edit commits through the same history,
including during playback; Enter/focus loss accepts and Escape cancels. Invalid values retain intent.
Delay processes the selected channel/bus/Master after Compressor, before pan/level and pre-fader sends.
Feedback controls decaying repetitions; 0% gives one echo, Mix 0% gives dry, 100% gives wet. Bypass
returns to dry smoothly. Time transitions blend taps over 20 ms; other changes smooth over five ms.
Ordinary Pattern repeats keep echoes; Stop/Panic and Seek/range wrap clear them. Full Pattern/Song
WAV includes a finite final tail, while explicit hard boundaries remain hard. Save/Open and Undo/Redo
preserve the parameters. This is not tempo sync, ping-pong, automation or a reorderable rack. Exact
finite bounds and memory/refusal policy belong to [audio](AUDIO_ENGINE.md#musical-delay-v1-seq-r11-delay-f1).

The Reverb inspector tab offers Enabled, Decay (0.2–8 s), Size (0–100%), Damping (0–100%) and
Mix (0–100%); defaults are disabled, 1.8 s, 50%, 50%, 20%. Each accepted edit changes the real
channel/bus/Master processor and uses the same exact-field history/validation/cancel behavior.
Reverb follows Delay before balance/level/pre-fader sends. Size changes spatial density/onset,
Decay controls low-frequency decay and Damping darkens the tail. Mix 0% is dry, 100% wet; bypass
returns smoothly to dry. Size prepares/crossfades geometry off the callback without restarting
transport/notes. Natural playback/full WAV includes a finite tail, Pattern repeats retain it and
Stop/Panic/Seek/range wrap clear it. R8 remains local before Mixer. This compact V1 has no separate
Pre-delay or processor reordering. [Audio](AUDIO_ENGINE.md#musical-algorithmic-reverb-v1-seq-r11-reverb-f1)
owns finite bounds, transition detail, admission refusal and acoustic limitations.

Fader/dial/EQ handle drag previews visually and commits once on release. Fader/dial offer Shift fine adjustment;
double-click level/pan restores unity/center. Keyboard fader arrows/Home/End and exact numeric fields
provide precise access. Enter/focus loss commits a changed valid field, Escape cancels, invalid values
stay repairable and never reach DSP. Any intervening generation/lifecycle expires the gesture; retained
fields must not overwrite a newer project. Processing changes become audible after commit, not through
unowned mouse-move APIs. Undo/Redo uses the same canonical document as Rack, Piano and Playlist.

Use local bank/inspector scrolling at small sizes; do not enlarge strip controls to fill an empty pane.
Source names remain available in tooltips when truncated. Solo opens upstream/downstream bus paths;
Mute still wins, including pre-fader sends. Playlist Mute/Solo independently removes occurrences before
Mixer input. Original note/rhythm editing and quiet source audition retain their established behavior.

## Channel duplication, order and Pattern Gain (SEQ-RACK-WORKFLOW-F1)

The channel actions menu offers **Duplicate channel** for supported Drum, Synth and PCM instruments,
including key maps. It inserts an independently editable sound and copied notes immediately below
the source. Unknown content, shared-Pattern expansion, unsupported graphs and capacity limits disable
the action with a reason. Media bytes remain shared immutable resources; the copy keeps the source's
explicit Mixer channel assignment. Sound-targeted automation remains attached to its original identity.

Drag the channel name to reorder rows; Ctrl at drag activation copies independently into the indicated
gap. A movement threshold preserves ordinary clicks and instrument-editor double-click. The thin
insertion line uses the host focus material. Step painting, row actions and Browser drops retain their
own targets. Release accepts one Undo operation; Escape, capture loss, outside drop, hidden/retired
content or changed document authority accepts none. Reorder changes no musical time, graph binding
or playback clock. Row order survives Save/Open and history.

**Pattern Gain** in the toolbar controls the selected occurrence's existing graph Gain. Its shared
horizontal fader, exact field and Left/Right/Home/End keys edit linear 0–1: zero is silence, one is
unity. A completed pointer gesture is one Undo; Escape/capture loss or stale authority cancels it.
While music plays, the draft also updates the identified executing Gain at packet boundaries, including
its before-Mixer source transfers in Pattern/Song. Transport, voices and effects continue. Escape,
capture loss, surface handoff/reflow, conflicting edits, target/lifecycle changes and Stop/Panic withdraw
the draft; cancellation restores current accepted Gain without Undo. Release accepts one canonical edit;
Undo/Redo update the same continuing execution. Drafts never enter Save/recovery/offline rendering.
Narrow windows move the same retained controls into toolbar overflow. The same parameter
yields to a visible Graph quick control or detailed Gain Editor. Loaded invalid values remain exact
and visibly invalid, without a legal fader thumb; exact entry provides explicit repair and Undo.

## Implemented R5-F1 step sequencing

Step pads keep a compact fixed square geometry at every pane width; insufficient room scrolls
horizontally instead of stretching or shrinking musical cells. Beat/bar gaps remain distinct.
Rich melodic thumbnails share the pads' fixed time geometry, including beat/bar gaps, and never
become step pads. Their start, subdivisions and Pattern end align with the ruler and step rows;
extra pane width stays empty and narrow panes scroll all three together. The Pattern
navigator reserves a scrollbar gutter, keeping complete card edges outside its vertical scrollbar.

The Rack rail action (or Panes → Channel Rack) provides New pattern, Add sound and a committed
Pattern name field. Pristine layout exposes the Rack without inserting music. A wide-window project
navigator creates Patterns and selects actual occurrences with canonical rhythm thumbnails. Creation is empty, one bar
with 16 sixteenths, and requires current 4/4 without
changing meter or BPM. Rows are canonical MusicalParts. A click or Space/Enter creates/deletes one
NoteEvent and one Undo entry. Arrows move between steps/rows; Home/End reach row ends; Tab accesses
commands. Captured left dragging paints when begun on an off step and erases when begun on an on
step. Movement fills skipped cells in the same row. The draft changes no music until release; one
release produces one Undo entry. Escape, capture loss, deactivation or invalidated basis cancels.
Row context commands Fill every 4/2, shift left/right and Clear use the same atomic row transaction.
No-op operations add no history or playback stop. Add sound or a single local WAV drop uses the same
managed acceptance/refusal path. Toggle automation reports On/Off and accepts the same canonical action. Name commits
with
Enter/check; Escape cancels local text. Global transport/history retain their existing commands.
Unchanged committed toolbar Gain loses focus without publishing another command or interrupting Save;
changed/stale drafts retain the existing validation and history rules.

The editor follows the explicitly selected playback occurrence and labels its actual Pattern.
Notes and names edit shared Pattern content without a variation. New channels require an unshared
Pattern and a recognized safe graph; refusing shared channel expansion preserves all placements.
Unsupported meter/length is explained without conversion. Known PCM notes with richer pitch,
intensity, duration or timing show a read-only melodic thumbnail instead of destructive step controls.
Double-click the channel name, its preview or the row Piano Roll command opens the exact Part.
Unknown musical data still disables its affected row with a warning; all data stays canonical/savable. Pattern-level
unknown semantics disable grid operations. External Edit/Undo/Redo/Open refresh the same controls.

Commands freeze target and generation; stale/ABA/concurrent actions cannot duplicate notes or switch
objects. Selection, replacement, pane hide/collapse and shutdown withdraw local actions; native picker
deactivation alone does not invalidate its unchanged import target. Creation/Open never start audio.
Explicit Play continuously cycles the selected Pattern. Note toggles, captured row operations and
Undo/Redo of notes preserve position and unchanged sounding voices after bounded preparation.
Future notes take effect at publication; already passed new/restored notes wait until the next cycle.
Deleting a sounding step stops that voice. Stop/Panic remain sticky through preparation and retries.
Creation, length and BPM edits stop transport; Play deliberately restarts. Selecting another Pattern
while playing retains Play intent and starts the chosen phrase at its beginning after preparation;
the same target is a no-op. Stopped selection stays silent. Song selection leaves Song time/execution intact.
The current column outline and bar:beat:sixteenth display read renderer state, with up to 50 ms
observation delay; they do not claim an output-latency-corrected audible cursor.

The length selector offers 1–32 whole 4/4 bars (16–512 sixteenths). Extension adds empty time and
restores ordinary end-clipped step gates without changing note identities. Richer notes retain their
exact durations on extension; shortening refuses occupied tails as well as later steps. It never
silently discards notes. A channel menu can remove a channel from the recognized
unshared graph, repairing its Mix tree in one Undo. Shared/custom/unknown topology refuses safely.
Pattern deletion removes all its placements and their item graphs in one Undo, retaining independent
sounds/resources and unrelated music. Target/generation guards apply to every action.
The rail toggle hides/restores navigation independently of music and Browser. Both sidebars coexist
in rail/Browser/Patterns/workspace order. Each right boundary resizes only its left panel and shifts
everything following it. Content first reflows, then clips at its usable minimum, and collapses only
near its left edge. Reopening restores its last usable width; closing a neighbour never closes or
overlays the other panel. These preferences survive resizing and document replacement independently
of musical history. [Shared left zone](WORKSPACE.md#shared-left-panel-zone) owns exact geometry,
cancellation, keyboard paths and reduced-window fitting. Sidebar captions do not imply movement/docking.
WAV export is a toolbar overflow action for the selected occurrence through its current valid graph.
It captures one frozen revision, writes one finite cycle as 48 kHz stereo PCM16, reports clipping and
leaves transport, music and history intact. Failed/cancelled export preserves an existing destination.
Rack scope adds no effects beyond Gain or an in-pad velocity editor. Piano Roll and Playlist are below.
[Audio](AUDIO_ENGINE.md#r5-f1-finite-step-pattern-playback) owns gate/tail limits;
[Workspace](WORKSPACE.md#r5-f1-retained-step-sequencer) owns retained geometry/focus.

## Implemented SEQ-R9 Piano Roll

The Pattern navigator separates selection from editing. Single click selects the occurrence and,
in playing Pattern Mode, switches the phrase. Double-click or Enter opens its editor; Space selects.
Context actions expose Channel Rack and Piano Roll with named channels. First opening prefers a
supported populated melodic Part (pitched notes, or richer PCM note timing/duration/intensity);
empty/step-only rhythm opens Rack. No inferred instrument/pitch classification is promised.
Explicit editor/channel choices are remembered per shared Pattern for the document session, across
occurrences. Missing/unsupported remembered Parts fall back to current content; replacement clears memory.
Playlist's Pattern open gesture uses the same editor choice. Selection alone opens no pane.
Rack/export shortcuts belong with the Pattern/Part identity row; ordinary editing tools remain
grouped rather than stranding those secondary actions on a new toolbar row.

One retained editor targets the selected occurrence's shared Pattern and explicit Part. Target and
sharing are visible; opening it never detaches content. All three musical views, history, playback
and export read canonical NoteEvents. Unknown Pattern/Part/note or unsupported sound semantics are
refused without replacement. A recognized PCM Part supports overlapping notes and fractional loaded pitches.

Draw click uses remembered duration/velocity; dragging empty space defines a duration. Body drag
moves the selected group; Ctrl-drag creates new note identities. Right-edge drag resizes; right-click
or Delete deletes. Shift-click toggles membership, Shift-drag/selection tool boxes notes; Ctrl+A
selects all. Velocity stems align with time, with staggered chord handles. Exact properties offer
pitch/start/duration/velocity; group pitch/start moves relatively and only changed duration/intensity
fields apply. Boundary clamping preserves group spacing. Clipboard commands refuse an oversized span.
After exact note properties commit, the editor reveals the selected note within its retained view.
Ctrl+C/X/V/D copies/cuts/pastes/duplicates; arrows nudge snap/pitch, Shift horizontal arrows resize,
Shift vertical arrows transpose an octave, Alt horizontal arrows select individual notes, Enter auditions.

Captured drafts edit no document before release. One accepted gesture is one Undo. Escape, capture
loss, stale/ABA authority, hide/collapse, replacement and relevant preference changes cancel drafts.
No-op movement, selection or survivor-order permutations add no history. Late status notifications
must not cancel a still-authoritative properties/source draft. Group properties preserve mixed unedited fields.
Compatible note changes use existing live preparation/continuation, not a separate editor transport.

Fixed keyboard/ruler, bar/beat/subdivision hierarchy, Pattern-end bracket, selected outlines, edge
cursors, preview bodies and aligned velocity communicate editing. Snap offers quarter/eighth/sixteenth/
thirty-second, eighth-triplet and free; Alt temporarily bypasses snap. Wheel scrolls pitch, Shift-wheel
time, Ctrl-wheel anchors horizontal zoom, middle-drag pans. Snap and Note Length keep unchanged
choices alive during transport observations, including an open mouse-operated Popup. Language
changes refresh localized labels. Their selection updates the canvas and survives Part switching
within the session without changing musical content or Undo. View/selection/ghost/clipboard state is session-only.
Other Parts are noninteractive ghosts. Pattern/Song playhead comes from renderer state, not measured latency.

Root Pitch/source settings update shared SoundDefinition root and Part name in one validated transaction;
they do not transpose notes or edit source bytes. C4 is MIDI 60. The selector sets integer roots and
preserves a loaded fractional root when only renaming. No inferred pitch claim is made. Import initialization
belongs to [sample workflow](SAMPLE_WORKFLOW.md#seq-r9-source-root-pitch).
Keyboard and enabled edit audition use a quiet, bounded solo source voice while transport is stopped,
not downstream graph monitoring. Stop/Panic/Play/close withdraw and join that preview. Creation/Open stay silent.

**Quantize** aligns only selected starts to the current quarter/eighth/sixteenth/thirty-second or
eighth-triplet Snap, anchored at Pattern tick zero. Integer ticks use nearest rounding; exact
midpoints move forward. Free disables the command with a short hint, as do no selection, unavailable
Part and Busy. Ctrl+A selects the whole Part. IDs, fractional pitch, duration, velocity, source,
Pattern length and Playlist placement remain exact. If any rounded start plus its original duration
exceeds the Pattern, the whole action refuses and suggests finer Snap. One accepted command is one
Undo; an already aligned selection creates no revision/history. Undo/Redo restore exact snapshots
regardless of current Snap. Shared Pattern uses observe the same edit; explicit whole-clip/repeat
variations retain their independent content. Existing live publication preserves transport ownership.

**Humanize** opens a compact flyout beside Quantize with independent Timing and Velocity strengths,
each 0–100% (initially 20%). Sliders and exact percentage fields adjust only the draft; explicit
Apply accepts once, Cancel/Escape/light dismissal accepts nothing. The command requires selected
notes in the active editable Part, including with Free Snap and without preceding Quantize.
At 100%, Timing samples integer offsets within ±30,000 ticks (1/32 quarter; ±15.625 ms at 120 BPM),
and Velocity samples within ±0.10 canonical intensity (ten percentage points); strengths scale
linearly. Zero disables that dimension. Selected notes with equal original Position share one
offset, clamped together to zero and the Pattern end using their longest original duration.
Velocity offsets are independent per selected note, in 0.00001 intensity steps, clamped to [0,1].
Only Position/Intensity change, preserving canonical order, IDs, pitch, duration and every other
Part/Pattern/source/route. Unselected chord members stay untouched. Boundary clamping may reduce
the requested change; a full-length note cannot move. There is no quantization, order correction,
minimum separation or automatic detachment.

One Apply is one canonical Undo; unchanged output (including both zero) creates no revision/history
and preserves Redo. Redo restores the exact stored result without randomness. Only Apply draws random
offsets; observations, transport, reopening the pane, Save/Open and export only read accepted notes.
Shared uses receive the same edit; explicit independent variations/repeats retain their own content.
Busy, unsupported data, stale generation/lifecycle/occurrence/Part/selection and pane cancellation
invalidate pending drafts. Transport observations preserve a current open flyout and its fields/focus.
Accepted changes use the existing Pattern/Song publication mechanism without new transport ownership.

**Scale** opens a compact tonic/type flyout: Off, Major or Natural Minor, with C–B sharp names.
Major intervals are 0/2/4/5/7/9/11; Natural Minor 0/2/3/5/7/8/10. Rows repeat over MIDI 0–127,
with a slightly stronger tonic. This is only a background guide: outside notes remain readable and
drawable. It never edits sound/root pitch, notes, JSON, export/R8 or Undo. Default Off/C is retained
across Parts, navigation, zoom/scroll, hide/restore, transport and language/theme changes; project
replacement resets it. Controls remain retained during observations, including an open Popup.

Scale locking, Piano-local controller curves, MIDI/QWERTY recording, MPE and independent
simultaneous Piano editor targets remain [follow-ups](ROADMAP.md#seq-r9--piano-roll). Pitch-only
multisample mapping belongs to the shared Source Editor below; Playlist automation is separate.

## Playlist parameter automation

The Playlist Automation icon creates a dedicated lane for an existing synth Cutoff or Mixer
channel/bus Volume. Headers identify the named target and parameter; curves, real point values and
the shared ruler show its musical progression. Hover reveals its range. A power control bypasses
the envelope without changing base settings; unavailable targets retain a visible warning and points.

Click empty curve space to add a point; drag a point in time/value with existing snap, Shift beat
snap or Alt free timing. Neighbours cannot be overwritten by dragging. One release is one Undo;
Escape, capture loss, deactivation, replacement or changed generation cancels the draft. Double-click
a point, use its context menu/Enter or the properties action for exact `bar:beat:tick`, Hz/dB and
Linear/Smooth transition. The header opens lane settings. Invalid input stays editable with a concise
error. Arrows nudge the selected point, Home/End select endpoints, and Delete removes a point while
retaining at least one. Lane deletion is a separate explicit action. Global Undo/Redo and Save apply.

These envelopes use absolute Song time and control the shared sound or channel aggregate across
all its repeated/overlapping uses. They stay anchored when clips or sections move/duplicate; they
are not automation clips or Pattern-local curves. Endpoints hold outside the point range. They do
not lengthen Song playback, affect Pattern/preview/R8 or overwrite manual Synth/Mixer settings.
Song seek/loop and full WAV use the same values with the audio owner's cold-entry/smoothing limits.
Deleting/replacing a target never redirects by its name or route; Undo can restore the original.

## Playlist music workflow

Ruler click moves the session Song cursor without starting audio; Play begins at that cursor.
Ruler drag selects a snapped half-open range and enables looping. The loop toggle retains/disables
its endpoints; Start returns to zero. A minimum sixteenth-note range avoids pathological loops.
The renderer reconstructs destination PCM source age and release, and never leaves old-location voices.
Seek does not defeat Stop/Panic. Open clears cursor/range; Pattern/Song mode changes still stop.
Ranges and cursor do not enter document history, project JSON or whole-Song export.

Shift-click toggles membership, the selection tool or Shift-drag on empty timeline selects a box,
and Ctrl+A selects every clip. Captured group move/Ctrl-copy preserves relative positions/lanes;
boundary saturation clamps the group together. Duplicate/Ctrl+D appends after the group's last end.
Delete and arrows operate on the group; each accepted action is one Undo. Repeat resizing/field
editing applies only to a single clip. Cancellation and generation/lifecycle guards retain authority.

Shift+wheel scrolls horizontally, Ctrl+wheel anchors zoom to musical time under the pointer,
and middle-drag pans. Manual navigation suspends Follow; Fit returns to the song origin.
The overview viewport drags without a jump, its endpoints zoom, and ruler/headings remain stationary
on their scrolling axes. These operations never edit music.

Adding/deleting a WAV channel in the supported Rack source/mix tree preserves Song/Pattern time,
epoch and unchanged voices; deleting the final channel becomes silence while other Song sources play.
Undo restores intent without retriggering past notes. A failed preparation keeps diagnostics and
last-valid rules, while structurally eligible Play allows an explicit retry. BPM changes stop and
reprepare at the new tempo; returning to a prior BPM does not require reopening the project.
Rack and Song source graphs share a bounded 32-voice budget, separate from the eight-channel limit;
dense supported piano parts are not limited to one active voice per channel.

## Implemented Playlist Arrangement

Playlist is one retained editor of canonical ArrangementClips, with shared source-placement references,
absolute tick positions, a fixed lane slot and whole-Pattern repeat count. Notes/resources/graphs are
not copied by placement, duplication or Ctrl-drag. Double-click/Enter opens the source Pattern's preferred editor;
subsequent
Pattern edits affect every referring clip. A placement's source graph applies to all its song uses.
Same-lane overlaps are legal and audible; tracks are not instrument or Mixer ownership.

Left click places the chosen source; captured drag moves a clip, Ctrl-drag copies it, and the right
edge changes repeats. The explicit repeat field and Duplicate/Delete buttons provide alternatives.
Bar/beat/sixteenth/free snap is a session view choice; Shift selects beat snap and Alt overrides freely.
One release commits one Undo; Escape, capture loss, deactivation, hide/collapse, replacement or stale
generation cancels. Home/End select the first/last clip; arrows nudge time/lane, Delete removes it and
Ctrl+D appends a copy. The same global Undo/Redo and Save/Open apply. No-op edits publish nothing.

Tracks use 32 fixed spatial slots, with optional identified name/color/Mute/Solo configuration.
The heading opens a committed editor; M/S headings provide immediate canonical gates. Multiple Solo
tracks play together; Mute takes precedence. Their state and stable configuration IDs survive Save/Open
and Undo. They gate Song, not the independent Pattern loop, and introduce no per-track DSP/mixer route.
Clip color follows its track; actual note position, duration and pitch drive the thumbnail. Muted/non-solo
clips dim. A global overview navigates the real arrangement; fit/zoom/scroll/follow add no musical history.
Headings stay visible during horizontal scroll. Minimum layouts wrap tools and scroll locally.

Pattern mode continuously plays the selected Rack; Song mode finitely plays all arrangement clips
from the session cursor, with overlaps/gaps and a renderer-derived playhead/bar:beat display. Playlist Play
explicitly selects Song. Switching modes stops; source selection during Song does not retarget/stop it.
Compatible clip/note/Gain/track edits preserve time, epoch and unchanged voice ages after preparation;
new past onsets do not retrigger. Shortening behind the playhead ends rather than restarting. Removing
all clips stops. Incompatible topology/settings replacements retain the explicit existing interruption
policy. Supported WAV channel changes also retain time and surviving voices under the
[audio continuation contract](AUDIO_ENGINE.md#playlist-song-preparation-and-execution). Current cursor follows renderer
time, not measured acoustic latency.

Song export freezes the current canonical arrangement and its graph/media dependencies, includes
leading silence/overlaps and writes 48 kHz stereo PCM16 through the same executor. It never substitutes
last-valid audio for invalid intent, edits history or changes live transport. Saturation/clipping is
reported; cancellation/failure preserves an existing destination and cannot overwrite managed media.
Save/Open preserves all music/track state; transport, selection/view and history remain session-only.

This bounded editor offers whole-Pattern repeats and 32 lanes. The view expands for long Audio Clips;
timeline presentation and exact bar fields extend up to one million bars. Canonical checked Int64
time remains the persistence boundary. All 32 lanes
are available by vertical scrolling even before they contain clips. The timeline anchors at TOP + LEFT;
its horizontal extent fills an expanded viewport with editable bars up to the existing limit. It adds no
in-clip velocity editor. Global automation is above; sections and explicit variations are below.
Audio range/trim/split/fades are implemented below; stateful global processing belongs to Mixer.
Shared velocity edits are in Piano Roll.
Reference-informed follow-up lives
in [R10 planning](ROADMAP.md#seq-r10--arrangement), separate from implemented controls.

## Implemented shared WAV replacement

Channel Rack's row menu replaces the supported WAV sampler's source with one native file choice.
The action updates all uses of the shared instrument, including musical variations, with one Undo
transaction. Notes, placement and processing identities, release and Mixer routes/settings remain.
A contextual hint makes shared scope and tuning behavior explicit. Known hash-verified catalog Root
Pitch is adopted; otherwise existing tuning remains. Source range and sustain loop reset to the new
complete source; Undo restores old media and endpoints together. This does not normalize level or switch sampler
algorithms; Piano source tuning and Mixer trim remain explicit controls. Cancel/error/stale selection
accept no partial edit. [Sample workflow](SAMPLE_WORKFLOW.md#shared-wav-replacement) owns durability.

## Implemented Playlist song structure

A stationary named/color section band sits below the bar ruler. Click a section or choose it in the
section navigator to seek without starting playback; double-click edits its name, exact start/length
in bars and color. Add section starts with the retained musical range. Sections are non-overlapping
organization, independent of routing and clip ownership. Context actions select wholly contained
clips, loop the range or remove its label; removal leaves the music intact.
Changing section labels, colors or label bounds alone preserves playback position and sounding voices;
Earlier/Later and Repeat still publish their actual clip changes.

Earlier/Later swaps adjacent ranges and their musical content, preserving the intervening gap.
Repeat section inserts shared Pattern/Audio uses immediately after it and shifts all later music and
section labels by its length. Every accepted structure operation is one Undo. A clip crossing any
affected boundary refuses the whole operation, with a visible reason, source name, track/start and
Show crossing clip navigation. It never implicitly splits, stretches or truncates a sustained phrase.
The user can explicitly split audio or adjust section boundaries, then retry. This bounded operation
does not provide arranger chains, nested sections or automatic treatment of cross-section tails.

Pattern and Audio Clips participate in the same Shift selection, marquee and Ctrl+A. Captured move/
Ctrl-copy, arrows, Duplicate, Delete and Ctrl+C/X/V retain relative time, tracks, silence between items
and original source references. Boundary saturation moves the whole group together. Duplicate appends
after the full mixed span; paste starts at the Song cursor on the last targeted track. Clipboard is
local to this document lifecycle, retaining frozen clip intent after Cut; missing sources or an
oversized track span refuse paste atomically. Copies/pastes become selected for immediate development.
Clip properties give an exact earliest start in bars and track; mixed groups move relatively.
Repeats, audio trim and fades remain single-item actions. No-op, stale, Escape/capture/lifecycle
cancellation rules and canonical history apply equally to both clip kinds.

Shared Pattern uses show a shared symbol. Editing through Rack/Piano still changes that Pattern's
notes for all uses. Make variation is explicit for one Pattern clip: fresh notes/parts and supported
item processing belong to its new placement, while instruments/resources and Mixer routes stay
shared. An arrow and source ancestry distinguish the variation, including after rename/Save/Open;
the preferred editor opens its actual new target. Unknown identity-bearing processing/data refuses safe detachment.
A repeated clip becomes one independent Pattern whose internal repeats still share that variation;
individual repeat cycles are not automatically unfolded. Copying a variation shares the variation.

For a Pattern clip with more than one repeat, right-clicking a cycle also offers **Make this repeat
unique** (**Сделать этот повтор независимым**). It separates just the cycle under the pointer into
an ordinary one-repeat variation, with optional shared left/right clips. Time, lane and total span
stay unchanged; sounds/resources/Mixer stay shared. One command creates one Undo entry and selects
the independent clip through the existing editor navigation. Its tooltip explains the note/sound
boundary. The right musical endpoint is outside the clip. Snap, zoom and scroll do not choose a
different cycle. A dismissed, hidden, stale or retargeted menu accepts no edit. Single-repeat clips
retain Make variation; the whole-clip action above keeps its existing semantics.

**Unfold repeats** acts on the pointed Pattern clip, even when other clips are selected. It replaces
2–1024 cycles with contiguous ordinary one-repeat clips on the same lane, using exact Pattern ticks
without Snap. The first keeps the original Clip ID; later cycles receive fresh IDs in cycle order.
All clips share the same source placement, including an existing variation and its ancestry. Unfold
creates no musical content or independent variation; use Make variation afterwards for independence.
The first resulting clip is selected. Existing move/copy/delete/editor gestures apply to each clip.
One Undo restores the repeated clip, and Redo restores the accepted IDs. Single-repeat Unfold is
disabled; dismiss, selection retargeting, hide, Busy, stale generation and lifecycle changes invalidate
the captured action. Unknown clip data refuses atomically rather than being copied or discarded.

## Implemented Audio Clip workflow

Native Explorer WAV/MP3/FLAC drop directly targets Playlist time/lane with a snapped insertion ghost
and Copy effect. The Playlist import picker offers the same standalone route. Neither requires Rack
or creates a Pattern. Unsupported drops refuse the target; the picker reports localized format/channel/
rate/size/decode failures. A crossed-out cursor can also mean Windows elevation blocks the OS drop.
Import is worker-prepared, cancellable from Playlist, and one history edit per accepted file. Failure/
cancellation leaves that candidate absent; prior accepted files from a multi-drop stay undoable.

Actual mono/stereo min/max waveform envelopes distinguish audio from Pattern previews. Waveform
display fits source amplitude for readability, not audio normalization. Body drag moves, Ctrl-drag
copies; Shift selects audio groups. Edge drag trims a single occurrence; upper waveform corners edit
linear fades. Resize/fade cursors identify the active gesture. Snap/temporary modifiers and Escape/
capture/stale-basis cancellation retain existing owner semantics. Delete, Ctrl+D and arrow movement
operate on the common Pattern/Audio selection described above.

Slice clicks or Split at cursor split only a single-repeat range. Source range and original fade
envelope survive split without an artificial seam. Repeats loop that range at its explicit local ratio, not BPM.
Double-click/Enter opens compact exact offset/duration/fade/gain fields and the shared source's Mixer
route; creating a Mixer channel is explicit. Route/gain/range edit uses existing project/history, and
copies share route but own range/fades/gain/stretch. **Time stretch / Растяжение** in these same
properties is a 50–200% duration multiplier, default 100%; the compact playback duration updates
before Apply. At non-unity the clip header shows `↔` and its percentage, independently of repeat `×`.
An explicit Apply is one Undo transaction; dismissing the popup commits nothing. Invalid values
retain the document and show a short error. The existing edge gesture continues to trim, including
conversion through the current ratio; upper corners continue to edit source-frame fades, which scale
with stretch. Moving/BPM changes never silently stretch or transpose. No Follow Tempo control exists.

Audio and Patterns play/export together through the existing Mixer. Track Mute/Solo gates occurrences;
Mixer Mute/Solo/effects/routing operate on summed shared sources. Save/Open/Save As preserve media and
intent, with unavailable waveform/status for damaged managed media and no unsafe fallback export.
The external original is unnecessary after accepted managed import. Limits are in
[audio](AUDIO_ENGINE.md#local-audio-clip-time-stretch). Stretch targets sustained/monophonic phrases;
its 0.1–60-s source-envelope limit and transient/polyphonic artifacts remain explicit. No general warp,
surround downmix or recording is implied.

## Implemented R4-F3-A bounded workflow

Users explicitly import a supported WAV, select its real occurrence, edit canonical Gain, Play/Stop/
Panic and Save/Save As/Open. Empty launch adds no musical content or playback. Reopen restores durable
intent/media with no inherited execution and requires another explicit Play. Native pickers and
Save/Discard/Cancel guard dirty replacement/close; cancellation and failed Save retain current work.
Persistent blockers, degraded media, physical output faults and last-valid divergence remain visible
in compact status with deliberate details. Icon actions have localized tooltips/names and shortcuts;
Toolbar Gain keeps fractional typing local until Enter or focus loss, with one guarded history edit.
Escape restores the canonical value; observation refresh does not erase a draft, and intervening
generation/selection changes invalidate its authority. Enter retains the input focus for further edits.
BPM opens a committed numeric popup in its transport location; meter shows the actual read-only
setting. Tempo accepts 1–1000 BPM with at most six decimal places, invariant or popup-frozen locale
decimal parsing. Enter/check applies once; Escape cancels; invalid values remain local. The popup
freezes lifecycle/generation, refusing intervening edits instead of overwriting them. An accepted
tempo change stops playback, preserves musical-time positions and requires explicit Play; no-op
adds no history or stop. Narrow overflow and retained Inspector preserve access. The owned unsaved modal starts with
safe Cancel focus and returns focus.
[Workspace](WORKSPACE.md#r4-f3-a-commands-and-owned-modal-lifecycle) owns lifecycle/keys;
[UI design](UI_DESIGN.md#first-party-visual-design-rules) owns presentation. This is not graph canvas,
Pattern/Piano Roll, Arrangement, Mixer or Browser acceptance.

## Implemented R4-F3-B graph interaction

The main Workspace's background click deactivates pane chrome and moves keyboard focus to its canvas;
it does not clear musical/graph selection or explicit editor/playback targets. GraphCanvas's own empty
click clears Node/Connection selection while retaining the explicitly opened Node Editor. Background
drag pans; middle-button and Space+drag retain pan semantics. Node movement and pan begin only beyond
the screen-DIP threshold in [GraphDragThreshold](../src/Seqvium.Desktop/Workspace/GraphDragThreshold.cs),
independent of viewport zoom. Micro movement remains a click, preserves double-click recognition and
creates no canonical edit/Undo. Capture loss/Escape cancel; exact keyboard movement retains its own steps.

Users explicitly open the selected item's graph and manipulate real Source/Gain/Mix/Output, including
independent WAV branches in one unshared item. The retained target is visible and does not follow
unrelated selection. Missing targets cancel proposals and cannot redirect destructive commands.
Node drag previews commit once; cancellation/no-op leave canonical history untouched. Port preview
explains compatibility/refusal before acceptance; incomplete/cyclic intent is savable with persistent
element diagnostics. Gain text is one committed input session and can repair saved invalid amplitude.
Menus and local keyboard actions complement pointer creation, wiring, selection and navigation.

The compact pane distinguishes canonical readiness/incompleteness, preparation, currently playing
intent and retained previous-valid playback. Save preserves canonical invalid edits; fresh reopen
contains no previous runtime fallback. Repair converges automatically through the existing F2 owner.
[Workspace](WORKSPACE.md#r4-f3-b-retained-graph-pane), [graph](NODE_GRAPH.md#implemented-r4-f3-b-first-party-editor)
and [sample workflow](SAMPLE_WORKFLOW.md#r4-f3-b-add-wav-to-current-item) own exact bounds. Actual owner
canvas/interaction review remains required independently of pure tests and a quiet physical smoke.

## Subtractive Synth sound design

Rack's Add synth creates an empty melodic channel and opens its specialized Source editor. It adds
no notes or demo song. The channel Sound Editor action and Source-node double-click address that
same shared sound. New synth Patterns prefer Piano Roll; remembered explicit editor choices remain
authoritative. Piano supplies pitch, fractional transposition, duration, velocity and chords.

Two waveform selectors/analytical periods, balance/detune, cutoff/resonance and editable amplitude
ADSR expose actual DSP. Warm Bass, Soft Pad, Bright Lead and Pluck each apply one factory snapshot.
Dials, exact values and curve handles edit the canonical block with one Undo per gesture/commit.
Enter/focus loss accepts valid text; Escape restores it; invalid input stays local. Pointer Escape,
capture loss and intervening generations cancel drafts. Resize cancels gestures while preserving
unfinished text. Theme/language changes preserve target and music. Shared edits affect every use;
independent copies retain separate intent.
Preset selection preserves unfinished numeric input: finish or cancel the value first. Invalid text
stays in its field, including through language changes. A rejected exact value shows that parameter's
allowed range directly below the field, with localized units; range help is also available on the
field/dial. Escape or a valid commit clears the local error. Parameter errors do not use the preset
header's action-feedback slot.

Hold starts a local note at the selected pitch while transport is stopped; Release sends NoteOff,
and Stop joins the existing output owner. Preview creates no notes/history and has a finite safety
gate. Knobs and ADSR handles change the held sound during capture without a canonical edit. Escape,
capture loss or resize restores the canonical sound; release commits one Undo operation. Accepted
values, waveforms, presets and Undo/Redo preserve the held voice; conflicting generations cancel
drafts. Presets still require unfinished text to be resolved. Repeated Play while held keeps the
same note; changing its selected pitch retunes it. Play after Release starts a new note on the same
healthy output. Release freezes its current duration and actual envelope level, so later editing or
repeated Release does not prolong it. Stop joins the output, and subsequent Play opens it deliberately.
Play/short sound edits preserve neighbouring control availability and unfinished text. Source removal,
hide/retarget/shutdown withdraw preview. A fixed footer stays reachable
as controls reflow and scroll locally. Note selection, Play/hold, Release and Stop share one
aligned transport row. Release uses a falling-envelope icon and concise visible label, because an
upward-arrow metaphor is ambiguous with export. The editable curve uses spare ordinary-pane height
without adding decorative filler. Held state is visibly distinct from release; unavailable release/Stop are
disabled. Stop remains available while this editor owns the output, including after the release tail,
so it can explicitly join teardown. Shape selection has a stronger contour as well as color.
Omit the redundant generic scope,
textual signal-path sentence and repeated default instrument name. Preserved user names are document
content, not UI translations. [Audio](AUDIO_ENGINE.md#polyphonic-subtractive-synth-seq-synth-f1)
owns voice retirement and seek limits.

## Drum Voice sound design

Rack's Add drum chooser creates an empty Kick, Snare, Closed/Open Hat, Clap, Tom or Rim channel and
opens its sound editor with a useful default; it adds no preset rhythm. Channel identity double-click
and Open sound editor return to that explicit Source target;
Open Piano Roll remains a separate channel action. The editor shapes sound, Rack writes rhythm and
Piano writes pitch/duration/velocity. Shared Pattern/Song/history/Save/Open/export use the same intent.

Only the chosen instrument's working parameters have physical dials and convenient exact fields.
Each instrument offers three factory snapshots, a Custom state and explicit `.seqdrum` save/load
buttons. Applying a snapshot is one Undo, retaining rhythm and shared sound identity. Hat choke
links enabled hats across channels and audible Song clips; Closed wins simultaneous hits. Turning
it off permits independent tails. File loads retain lifecycle/target/generation authority across the picker.
Repeated numbers are omitted
inside dials; Tune retains useful Hz alongside its semitone field. Pitch and Amplitude modes expose
editable start/shape/end points and the applicable exact envelope parameters. Pitch mode exists for
Kick and Tom; other instruments expose amplitude and their actual waveform. Captured drag/fine
adjustment, arrows/Home/End, default reset, Enter/focus-loss commit and Escape/capture-loss cancellation
produce at most one logical edit. Uncommitted curves are dashed; intervening edits invalidate drafts.
Audition selects a note/velocity and plays an immediate source hit, including over Pattern or Song,
without notes, resources or history. Repeated Play overlaps bounded tails without restarting music
or pulsing workspace Busy. Local Stop cuts only preview; global Stop/Panic includes music. Hiding,
retargeting or removing the source withdraws preview. Parameter commits during playback affect
subsequent hits without an automatic extra hit or rhythm restart.

Drum/Synth source preview has the same base level as a corresponding unity-graph note at the same
Velocity, with preview-only peak protection. It bypasses local Gain and project Mixer/Master, even
Master Mute; project gain/effects/routing and dense preview overlap can therefore explain different
levels. This is source comparison, not equal-loudness normalization. The
[audio owner](AUDIO_ENGINE.md#drum-play-along-and-source-preview-levels-seq-drum-playalong-f1) defines
the mixing boundary and headroom policy. Browser/Piano and Synth Hold retain their existing stopped-
transport scope; Sample Lab contextual replacement retains its routed musical scope.

The time/frequency graph is analytical; its waveform is derived from the actual synthesized signal,
not a live meter. Compact panes reflow audition/knobs and scroll locally, keeping every exact parameter
reachable. [Workspace](WORKSPACE.md#drum-voice-editor-placement) owns retained sizing.

## Sampler Source Editor

Key Mapping adds WAVs to this same instrument in one batch/Undo action. A compact source selector,
full-map overview and adaptive working keyboard handle dozens of sources without a growing
table. The scrollbar/overview move left or right; wheel zooms, Shift-wheel pans, and the center action
returns to the selected root. Source selection reveals its root when outside the viewport. Resize
keeps a previously visible selected root in view; ordinary observation never undoes deliberate panning.
Compact panes show fewer large keys. A single instrument deck joins source identity and zone values
above the keyboard; wide panes put them in one row. The selector shows root and concise source name, with full name and
shared-edit scope on inspection. Zone management is grouped in its menu; preview belongs to waveform.
Black keys are shorter than white keys in the normal 2+3 pattern. The focused keyboard's arrows
select neighbouring zones; these are local controls, not new application shortcuts.

Click a zone or choose its source to inspect that WAV. Drag selected Low/High handles or the Root
marker; exact fields accept musical note names or MIDI numbers (C4 = 60). Bounds are inclusive,
cannot overlap and may leave silent gaps. Capture/Escape/stale generation cancels a proposal; a
release commits once. Auto map explicitly redistributes all ranges by confirmed Root Note.
Import reviews roots in a compact source selector; unknown roots are blank and block acceptance
until assigned. Distinct roots are required for auto map. No filename/audio pitch guessing occurs.

Selected WAV range/loop/crossfade edits belong only to that zone. Source range discloses exact
frame endpoints; Sustain Loop and Crossfade remain separately visible below the waveform.
Sustain Loop uses a labeled loop toggle, with distinct checked/focus states, rather than a detached checkbox.
Hold/release previews its source
at its own root, even if that root lies outside its manual key range. Removing a zone leaves the
other bounds unchanged; Auto map can close the resulting gap. At least one source remains.
The old Piano source-root field is disabled for maps; tune their roots in Sound Editor.
Whole-instrument Replace WAV (including explicit Sample Lab replacement) clears the map; Undo restores
it. Live edits preserve unaffected voices; changed source/step/range voices retire without switching
to another WAV or retriggering. Velocity layers, round-robin and articulation switching are absent.

Channel Rack's Sound Editor menu and a supported bound Source node's double-click/Open Editor target
the existing singleton Node Editor. A PCM sound opens the waveform editor; opening alone never edits
or auditions. Its named Shared sound cue means changes affect every Pattern using that identified
SoundDefinition. Independent sounds using the same WAV keep independent ranges/loops.

The real mono/stereo waveform shows the audible prefix/range and sustain region. Upper Start/End and
lower Loop Start/End flags have distinct positions and colors; disabled retained loop flags are muted.
Crowded flags omit colliding captions; the exact fields retain their labels. Dragging previews
one proposal and commits once on release; Escape/capture loss/target or generation change discards it.
Markers never cross each other or silently change another boundary. Wheel/+/- zoom around a viewport
anchor, pan scrolls the source-frame viewport, Fit shows the whole source. Zoom changes no musical data.
Fields accept exact integer source frames; Enter/Apply commits a valid block, invalid values leave the
project unchanged. Endpoints are exclusive. Loop toggle retains its explicit endpoints when disabled;
first enabling chooses the visible sample range. Short/outside/reversed loops are refused.

Crossfade accepts exact source frames. Zero preserves F1's seam fade; a positive value mixes the
loop tail with the equally sized area before Loop Start, shown by complementary ramps on both
waveform areas. Width is limited to 4096 frames, half the loop and available audio between Sample
Start and Loop Start. Out-of-range input is refused with an actionable error; no marker or width
is silently changed. Marker proposals must retain valid crossfade handles. Tooltips explain the
zero mode and limits without permanent technical paragraphs on the working surface. Edits use
one shared-sound transaction and the same voice-retirement policy as range changes.

Hold note previews the root note while stopped; Release starts configured release and Stop ends the
preview. A ten-second safety gate bounds forgotten holds. Preview bypasses downstream processing and
never edits notes or history. Editing/retargeting/hiding/Play/close ends the prior preview lifetime.
Source replacement explicitly resets range and loop to the complete replacement. Live playback edits
retire that sound's old voices on publication and take effect on its next scheduled notes, preserving
compatible transport/unrelated voices. Continuous held-note loop morphing is not promised.

The waveform uses an enlarged existing floating pane and local scrolling at reduced sizes. Exact
fields and shared-sound scope remain available in RU/EN and both themes. This is instrument source
editing, independent of Playlist Audio Clip trim/stretch and its musical placement model.

## Detailed node editor access

**SEQ-NODE-EDITOR-IMPL-F1 — implemented bounded access accepted for the first slice.**
Every node admits an editor through body double-click, preserving single-click selection/movement
and quick controls. One reusable internal pane follows
[Workspace](WORKSPACE.md#node-editor-placement-proposal); independent editors remain Q-032.

One semantic Open Node Editor action is also reachable through the selected node's context
menu and Enter while the graph canvas owns focus. Double-click resolves the actual body hit with
existing port precedence; ports, wires, background, pan modifiers and compact child controls retain
their own actions. Recognize a completed body double-click only when neither click became a drag,
both address the same logical target, and lifecycle/basis remains valid. Release any canvas capture
before opening. Use platform click timing/spatial tolerance rather than inventing a fixed interval.
Do not delay ordinary single-click selection or change existing movement semantics.
Double-click opening itself changes no document state/Undo/dirty flag and never starts audio.

The editor visibly identifies its node and processing scope independently of graph selection,
active pane and playback target. Opening the same target activates its surface; choosing another
requires explicit opening, not hover/selection-following. Missing nodes/capabilities show understandable
host-owned details with unavailable editing and no silent replacement. Every node has an opening
path, including structural nodes with read-only details.

For the first Gain slice, label amplitude and derived dB clearly; zero is silence. Keep the canonical
value distinct from temporary gesture/text input and any identified last-valid audible state. An
external accepted edit updates idle controls; if it invalidates a local draft, cancel that draft and
show concise changed-elsewhere feedback rather than overwrite it or silently commit stale input.
Programmatic refresh is not a user edit. Local text Undo/input takes precedence over project commands;
Enter in a numeric field commits it rather than opening another editor. Keyboard and pointer access
share the same target and document transactions.

The physical Gain dial and slider share the same editor draft. Relative circular dragging increases
clockwise and decreases counterclockwise, without snapping on press or wrapping values at the angular
seam. The center is insensitive; crossing it re-anchors the gesture. Limits respond immediately to
reversal. Release commits once, Shift gives fine rotation, double-click restores unity and arrows/Home/End
provide keyboard access. Escape/capture loss cancels. The exact amplitude field remains available.
The dial's ring displays its real value; the transfer plot is analytical, not a live signal meter.
Drum parameter dials and compact Mixer balance use this same rotation model. Horizontal Gain and drum
audition velocity use the shared captured level control: only X changes the value, including outside
the drawn track vertically. Audition velocity remains local and never enters project history.
The Drum editor keeps a large icon-only Play beside a separate sample Stop below its scrolling
parameters. Play can retrigger rapidly without waiting for an earlier hit to finish. Releasing a
changed accepted parameter auditions the current sound outside musical playback; cancellation,
Escape, Undo and unchanged or stale edits do not trigger sound. Pending preparation retains only the
latest intent, and Stop/Panic, target changes and project close prevent stale starts.
The editor continuously reduces dials to a usable minimum within its own floating/docked viewport,
then wraps with stable breakpoint bands. Graph size follows available width/height; text fields/fonts
remain readable. A very small viewport may scroll vertically.
Draft feedback stays on the edited controls; transient warning text has a fixed header slot and never
pushes Play/Stop or changes the working area during a gesture.

The node name belongs in the pane title; source and processing scope appear once below it. Gain's
short Level label groups exact amplitude and derived dB with the control. Curve is optional disclosure,
initially collapsed. Opening/closing it preserves a valid draft and creates no musical transaction.
The changed-elsewhere notice is transient and requires a real cancelled draft; ordinary refresh and
own commits never produce it. Errors, blockers and last-valid divergence remain available.

Quick Gain surfaces have different selection owners: toolbar follows the playback-selected attachment,
Graph follows its retained attachment and selected node, Node Editor follows its explicitly opened
target. Suppress duplicate controls only when their complete target and canonical parameter match:
visible expanded Node Editor, then visible Graph quick Gain, then toolbar. Hiding/collapsing a pane
restores the corresponding quick control; a different scope stays independently accessible.

Empty Graph and Node Editor surfaces show one useful hint in content. Empty selectors and redundant
headers/footers are hidden. An unavailable node keeps its logical key and one warning for exact Undo
restoration; diagnostics and last-valid feedback remain visible where applicable. Node Editor starts
hidden in a pristine layout and appears only on explicit open or restoration of a previously visible pane.

## Mouse-first creation and complementary input

**Mouse-first, keyboard-efficient** means direct pointer manipulation is a first-class, polished path
for creation and editing, and basic workflows do not require memorized shortcuts. Users must be able
to place, move, resize, and select notes/events; edit steps; move/resize clips; select waveform ranges
for trimming/processing; manipulate node graphs; adjust parameters; and organize the workspace directly
with the pointer. Drag/drop, selection, context operations and parameter manipulation remain excellent.

The ordinary computer keyboard is also a first-class path for precise/repetitive editing, navigation,
selection changes, action invocation, modifiers, Undo/Redo and focus/accessibility access. Its efficiency
is complementary, not secondary in quality or dependent on a separate musical/document model.
Exact gestures and bindings remain open; this direction does not make Seqvium keyboard-first or
shortcut-dependent.

An on-screen musical keyboard should support audition/simple playing. Future MIDI devices, MIDI
recording, and realtime note input complement pointer editing and converge on compatible musical
event data. Audio-interface/microphone recording is also long-term direction, with execution and
latency owned by [AUDIO_ENGINE](AUDIO_ENGINE.md).

A typing keyboard / QWERTY piano is not default global behavior and must not consume ordinary command
space by default. It may later be offered as an optional, explicitly activated musical-input mode,
with clear mode/focus boundaries and a discoverable exit. Entering, leaving or changing its context
must not make normal application commands unpredictably unavailable. Exact piano layout, mode-specific
command coexistence and routing remain open.

### Semantic actions and input composition

An action describes the requested outcome and intended target/scope, independently of how it is
invoked. Pointer gestures, keyboard bindings, menus/context menus and future accessibility/action
surfaces may request the same operation under the
[application boundary](ARCHITECTURE.md#semantic-actions-and-input-boundary).

For example, dragging a clip, a keyboard nudge and a context-menu Move action express the same
placement-edit semantics, including applicable placement/context relationships. Keyboard movement
must not invent a second edit model or bypass canonical validation/history. Input surfaces may differ
in preview or granularity; the accepted edit and its Undo meaning belong to the operation.

The following is an illustrative movement case, not accepted bindings or step sizes:

| Input example           | Requested intention                                     |
|-------------------------|---------------------------------------------------------|
| Left / Right            | Precise/small nudge of the intended clip/item selection |
| Modifier + Left / Right | Larger/coarser nudge of that same target                |

Separate intentional presses normally remain separate edits; a continuous held-key repeat may form
one bounded same-intent session under [Undo grouping](#undo-grouping-and-interaction-preview).
Changing target, an unrelated command or completing the interaction ends grouping; elapsed time alone
does not define intent. Input device alone must not produce radically inconsistent history for the
same canonical operation. No repeat delay/rate, grouping time constant or step size is selected.

Modifiers are legitimate gesture/command composition: they may adjust precision, magnitude or mode,
including constrained movement, temporary snapping or selection extension. Resolve conflicts
deliberately across relevant contexts, rather than assigning contradictory behavior ad hoc per control.
No global Shift/Ctrl/Alt meaning, exact modifier or final interaction mechanics is fixed here.

### Focus, selection and command targets

Keep these concepts distinct even where they correlate:

| Concept                    | Meaning                                                       |
|----------------------------|---------------------------------------------------------------|
| Keyboard focus             | The surface/editor currently receiving keyboard input         |
| Musical/document selection | Selected project material in the relevant editing context     |
| Active workspace pane      | The pane currently activated under the workspace contract     |
| Current command target     | The explicit target/scope to which a requested action applies |

Keyboard interaction should move focus or selection through relevant timeline items, notes/events,
graph elements, panes and menus/actions where useful. Every visual object need not be an individual
tab stop; contextual navigation and action access should avoid tab-stop noise. Common graph selection,
deletion, navigation, opening settings and command invocation must have room for keyboard access
without precision pointer work. Free spatial layout does not imply mouse-only interaction. Detailed
node/timeline navigation and a full keyboard graph editor are not defined here.

Routing must respect the current input context and intended command target. Typing/editing in a text
field, rename editor, numeric value editor or search/browser field must not also trigger unrelated
global project edits. Context-local input and explicitly applicable host actions need deliberate
precedence; an ambiguous, absent or unavailable target must not silently redirect a destructive edit
to another selection/pane. This is semantic routing, not a chosen focus manager or event API.

Third-party/native editors own their local input; typing there must not accidentally trigger unrelated
host edits. Re-entering the host must predictably restore a useful, still-valid host context rather
than use stale targets or steal focus. [WORKSPACE](WORKSPACE.md#keyboard-access-and-focus-return)
owns pane reachability, escape and detached/native-editor return behavior. Concrete routing, input
capture and platform integration remain open.

### Binding identity and platform conventions

Semantic action identity is independent of physical keys and default bindings. Preserve room for
user-configurable bindings without making domain code know physical keys or making later rebinding
require rewriting editing operations. Binding preferences follow the
[user configuration boundary](SETTINGS.md#user-configuration-and-project-state).

Actions remain portable across Windows, Linux and macOS, while default bindings may differ to respect
platform conventions and reserved/system shortcuts. Identical physical shortcuts on every OS are not required;
[PORTABILITY](PORTABILITY.md#product-target-and-current-evidence) owns target and evidence limits.
No complete binding editor, persistence format, conflict-resolution UI, import/export, profiles,
chords/sequences or exact platform defaults is selected.

## Progressive complexity and discoverability

Expose controls relevant to the current musical task. Advanced capability may be available without
permanently occupying the primary workspace. A simple default must allow deeper editing rather than
cap the user's work. Important actions must not exist only as undocumented shortcuts. Useful commands
need discoverable paths where appropriate through menus, context menus, visible/contextual controls,
a deliberately opened workspace or later command/search surfaces; shortcut hints can teach efficient
alternatives. Final menus and a command palette are not designed here.

Relevant musical items/containers should visibly indicate processing with a compact, clearly
interactive control. Graph complexity normally stays hidden; opening that indicator reveals the
relevant Node Graph workspace pane. No final icon, count, badge, color, label, or typography is chosen.
[UI_DESIGN](UI_DESIGN.md) owns presentation principles, and [NODE_GRAPH](NODE_GRAPH.md) owns graph semantics.

## Immediate feedback and minimum ceremony

Enabling a step, moving a note, adjusting a filter, or auditioning a candidate should reveal the
musical consequence quickly. Interaction should remain responsive while audio execution continues
independently; exact latency targets need evidence rather than invented numbers.

The ordinary add path should approach:

```text
+ Instrument -> choose instrument -> play/write music
```

Internal routing may exist, but ordinary defaults should work without creating and wiring unrelated
objects manually. A sound should be audible before a user has mastered the full workstation model.

## Editing semantics

Step Sequencer and Piano Roll provide compatible views of musical events in the bounded implementation above. Moving
between them must not require recreating a phrase. The intended model and shared pattern references
are owned by [ARCHITECTURE](ARCHITECTURE.md).

Patterns are user-named reusable units that may contain parts for multiple instruments. Users choose
granularity, such as separate bass/lead patterns or a combined groove. Organizational instrument/channel
groups are separate from pattern membership and identity; names/grouping must not force musical structure.

Repeated placements normally share Pattern musical content; editing it changes all those placements.
Musical-content independence and sound-definition independence are separate intentions. Conceptual
`Make Pattern Variation` detaches/copies musical content without implicitly detaching every sound,
resource, or processing relationship; conceptual `Make Sound Independent` addresses the sound definition.
Placement-local position, processing, or future fades do not modify every use of the Pattern/resource.
There must not be one ambiguous linked/unlinked state or only a hidden unlink-everything operation.
Make meaningful sharing understandable, for example by indicating multiple uses of a Pattern or sound
definition. Exact commands, indicators, and UI remain open. [ARCHITECTURE](ARCHITECTURE.md) owns identities.
Arrangement tracks and mixer channels have different purposes and must not force a misleading
one-to-one conceptual model.

Audio resources and musical placements also have distinct identities. Ordinary processing of one
item should be non-destructive and should not silently change every use of shared audio. Users
normally reason about item-local and containing-container processing, while global mixer/bus/master
responsibilities remain separate. [ARCHITECTURE](ARCHITECTURE.md) owns that bounded local model and
the semi-free Arrangement direction; no final "Layer" term or Track schema is accepted.

Atomic events do not automatically expose full local DSP graphs. A low-ceremony action should let a
user select one event and make an independently processed standalone fragment/item when needed,
without learning internal decomposition. Exact command and transformation remain open.

Sample Lab supports standalone and contextual exploration. Contextual audition is temporary and
reversible until explicit acceptance and should preserve relevant existing downstream processing.
Object sample creation includes the object's own semantic/local processing boundary; audible-selection
capture serves a broader selected musical/time context. The operations are distinct and their names
are conceptual. Replacing an object with audio containing baked processing must be undoable and must
not silently reapply that exact baked chain. Specialized audition, acceptance, candidate history,
and resampling semantics are owned by [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md). Missing capabilities should be
represented explicitly according to [EXTENSIONS](EXTENSIONS.md), with user work retained.

### Edit target and sharing feedback

Opening an editor from a placement must make its editing scope understandable. Musical editing names
the referenced Pattern and affects its shared musical content; it does not silently create a private
copy. Placement editing instead affects that occurrence's position, supported local range/timing and
item-local processing. Moving one occurrence does not move other uses or reorganize its definition.
Sound settings identify the sound definition/use; containing effects identify the processing context;
media-level work identifies the durable resource. Focus or selection alone cannot establish ownership.

Use stable contextual target/scope and meaningful sharing information so users can anticipate affected
uses, with deeper relationship detail available when needed. A persistent target indication, an
understandable account of other uses and explicit independence actions are useful directions, not
selected controls, icons, counts or editor composition. Ordinary edits should follow predictable
targets without constant modal questions; meaningful shared-state consequences must remain visible.
When scope is ambiguous or unavailable, resolve it before mutation rather than silently widen it.

For three uses of Pattern A, a Snare note edit affects all three. Conceptual `Make Pattern Variation`
for B gives B independent notes while A/C retain the original and sounds remain shared unless separately
changed. Conceptual `Make Sound Independent` instead gives the intended sound use independent settings;
notes and immutable samples can remain shared. Each independence action is one coherent Undo edit,
including its reference changes. Different effects on two placements require neither of these actions.
Exact reference/detachment and opaque-plugin support remain open under
[architecture](ARCHITECTURE.md#independent-sound-and-local-processing).

A `Drums` container's preferred purpose may guide convenient defaults without forbidding an otherwise
compatible Bass Pattern or claiming ownership of its synth. Genuine unsupported placement relationships
are understandable constraints; do not silently convert or duplicate content. Arrangement organization
and explicit processing assignment have different consequences under the
[bounded container model](ARCHITECTURE.md#compatible-material-and-bounded-organization).

Deleting a selected placement removes that use. Deleting a definition, container or resource is a
different scope with known dependencies. Explain affected uses and any block/defer or deliberate choice
needed to resolve them; do not silently cascade to unrelated content/media or redirect uses by name.
Predictable local deletion need not require a modal questionnaire. Undo restores the intended canonical
relationships, subject to ordinary validation; exact deletion/confirmation mechanics remain open under
[deletion scope](ARCHITECTURE.md#deletion-scope-and-retained-relationships).

### Undo grouping and interaction preview

Input source does not determine Undo semantics: a pointer drag is one logical Move transaction, a
single nudge normally one discrete edit, and a held nudge may be one bounded same-intent session.
All express canonical edit intentions; gesture updates or repeated input events are not independent
history policy. The contract below remains authoritative for interaction grouping and preview.

Undo steps follow understandable user intentions under
[ARCHITECTURE](ARCHITECTURE.md#logical-undo-transactions-and-history-scope). One continuous clip drag
is one editing interaction, even if hundreds of pointer updates occur over two seconds. During the
gesture, show the proposed position/relationship as an explicitly transient, model-bound preview of
the canonical target. The gesture itself leaves canonical state unchanged until final commit;
other independent accepted edits may still proceed. Preview is not an invisible alternative project
truth and must not leak into Save/recovery as an accepted move. Temporary audible feedback, if provided,
also remains preview under the graph/audio boundary.

Committing a changed final result validates the target and relevant preconditions and records one
coherent Undo transaction from the accepted starting state to the final placement/relationship. A
no-change result creates no Undo step. Cancellation/Escape discards the proposal and restores feedback
to current canonical state without a history entry; it must not roll back unrelated accepted edits.
If the target/context changed incompatibly during the interaction, cancel or require an explicit
reconciled intention rather than overwrite newer work. Event throttling and UI framework mechanics
remain open.

Five separate nudge commands normally produce five distinct edits; five unrelated parameter changes
remain distinct. Bounded coalescing can be justified only by an explicit, understandable same-intent
session, such as one continuous held-nudge interaction or one committed text/value editing session
on the same target/scope. A different target, unrelated command or completed interaction ends that
group. Do not merge unrelated actions merely because they happen close in time or merge later async
completion with intervening edits. Exact supported sessions/bounds remain workflow decisions; elapsed
milliseconds alone never define user intent. A multi-object command may deliberately form one Undo
transaction. Explicit async acceptance also creates one coherent edit regardless of preparation time.

Undo of shared Pattern content restores the one shared content, affecting all references consistently;
it does not imply independent placement copies. Variation, placement-local processing and independent
sound-definition edits have distinct targets under
[ARCHITECTURE](ARCHITECTURE.md#separate-sharing-identities); Q-029 retains exact sharing/detachment UI.
Document Undo concerns canonical project work. Workspace/user preferences, transient audition and
derived execution follow the [history scope](ARCHITECTURE.md#logical-undo-transactions-and-history-scope).
No keyboard shortcut, history size, storage implementation or persistence policy is selected here.

### Processing context and mix feedback

Shared versus independent performance can change audible mono/legato/retrigger and voice-stealing
behavior, even with one shared preset. Expose that meaningful consequence when an operation changes
performance interaction to provide independent processing; do not present it as a sound-neutral
implementation optimization. [ARCHITECTURE](ARCHITECTURE.md#shared-sound-definitions-and-execution-domains)
owns the rule; final labels, defaults and controls remain open.

Pattern membership must not misleadingly imply one mixed instrument output. A multi-instrument
placement can retain different instrument paths; shared sound definitions can serve overlapping
placements with different processing. The signal model belongs to
[ARCHITECTURE](ARCHITECTURE.md#signal-ownership-and-processing-contexts), and
[NODE_GRAPH](NODE_GRAPH.md#contributions-and-irreversible-mixing) owns convergence semantics.

Users must be able to distinguish processing of an independent contribution from intentional
processing of a whole placement/container submix. The latter's result cannot offer independent
instrument routes recovered from the aggregate. Do not hide this loss behind a control that appears
to preserve separate routing. Clear contextual scope/result feedback must make it understandable
without requiring the user to open a graph or answer a routing questionnaire. Exact controls remain open.

Arrangement and Mixer may show controls for the same processing context. Their target and shared
edits must be understandable; opening the other view must not look like adding a second effects chain.
An additional independent channel/bus stage, when explicitly routed, is a distinct target. Displaying
local processing in Mixer does not turn it into unrelated global processing or expand an object render.

Moving Kick from a Compressor context into Distortion/Delay changes processing membership and may
also change its assigned route; expose those meaningful consequences. Pure organizational regrouping
at unchanged musical time retains processing/routing and must not alter sound. Musical containment,
organization and audio context must be distinguishable with progressive disclosure; final names,
indicators, gestures and route-assignment workflow remain open. The architecture owner traces the
[move](ARCHITECTURE.md#moving-material-between-contexts), including possible effects on other submix members.

### External dependency feedback

Ordinary Kick/Bass creation and basic mixing must work without manually wiring every signal or opening
a full graph. Advanced dependencies are discoverable on demand and use the same mouse-first,
keyboard-efficient semantic actions. [NODE_GRAPH](NODE_GRAPH.md#cross-context-signal-and-control-relationships)
owns their meaning; no final wiring gestures, panels, menus, icons or global graph view are selected.

When users intentionally create an external dependency, they can understand which source/occurrence
and signal boundary influences which processor/control target, whether it is audible routing, a send,
detector input or parameter control, and whether it is valid. Kick driving Bass compression must not
look like Kick being added to Bass audio. Changed tap/processing membership can change detector behavior
even if logical source identity survives; a move, delete or reassignment must reveal meaningful affected
relationships without requiring a giant graph. Visual position/selection never silently retargets them.

Missing endpoints or unsupported required inputs remain understandable persistent dependency state
under [blocker feedback](#project-availability-and-dependency-blockers). Explain the affected processing/
operation, its unavailable requirement and meaningful repair/reattach/remove direction. A Bass object
render can require external Kick influence while excluding Kick audio; show why a required dependency
blocks that result. Last-valid playback is visibly distinct from canonical work awaiting valid execution.

### Audio timeline editing

Ordinary audio timeline editing must support trimming start/end, splitting, and moving/rearranging the
resulting pieces while preserving the original durable audio resource by default. A split primarily
changes edit structure; it must not inherently modify source audio destructively. A separate explicit
crop/consolidate/render operation may later create/commit a genuinely new shorter resource.

Keep these edit intentions distinct rather than overload one gesture ambiguously:

| Intention   | Meaning                                                             |
|-------------|---------------------------------------------------------------------|
| Trim        | Change audible/visible source range without changing playback speed |
| Loop/repeat | Extend material by repeating it                                     |
| Stretch     | Change playback duration/time mapping                               |

An ordinary trim must not accidentally change playback speed. Project-wide tempo changes and local
clip stretch are also distinct; [ARCHITECTURE](ARCHITECTURE.md#project-tempo-and-audio-time) owns the
tempo-following versus fixed/source-time model. The implemented Audio Clip properties choose explicit
50–200% WSOLA stretch; ordinary trim/move never enables it. Richer warp gestures and tempo following
remain future work, not implicit behavior of this control.

A natural source end may allow a local effect tail. Deliberately shortening/trimming the clip/item's
right boundary means its own audible result ends there, including item-local processing tails. A tiny
de-click may avoid a click without substantially extending that tail. This does not erase arbitrary
shared downstream container/bus/Mixer/Master state;
[AUDIO_ENGINE](AUDIO_ENGINE.md#source-boundaries-and-effect-tails) owns those scope distinctions and
loop/seek/playback Stop/Record Stop mechanics.

An explicitly selected export range is hard by default. A future explicit `Include effect tails`-like
option may extend it for naturally permitted tails, never restore project audio deliberately cut by
editing. Exact label/UI and finite tail-completion policy remain open under
[AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction).

## Discovery, audition and reusable content

Finding and auditioning material must make its scope understandable: temporary preview, accepted project
use and explicit Personal Library publication are distinct intentions under
[Sample workflow](SAMPLE_WORKFLOW.md#browser-discovery-and-ownership). Browser provides access to sources;
it does not turn all displayed material into project or global library content. A small discoverable
find/audition/use path, including reuse of current project samples, is sufficient initially.

Raw preview and contextual temporary audition do not enter project Undo or implicitly import/replace
accepted material. Explicit project use follows one coherent project edit where appropriate. Applying
a preset edits project-owned state; explicitly preserving a sample/preset across projects is a separate
user-content mutation. A combined action must expose its separate outcomes; project Undo normally
concerns the project effect. Exact controls, publication wording and global-history UX remain open.

An unavailable Browser source needs useful missing/unavailable state. Its provenance going offline must
not mark a healthy managed project resource or already applied preset state as broken. Missing actual
project media or required processing still follows the dependency-blocker contract below.

### Sample Lab exploration feedback

Keep ordinary exploration sound-first: generate, listen, request a related variant, revisit/compare
recent candidates and explicitly accept. Detailed parameters are optional; supported locks and deeper
controls appear progressively. These intentions need discoverable pointer paths and efficient keyboard
access through the same semantic actions, without choosing widgets, bindings or a final layout.

Make the generation family/reference, active held constraints, audible comparison choice/context and
acceptance destination distinguishable. Reference/family changes must explain retained or unavailable
locks; pending results retain their request identity rather than stealing a later selection. Explain
stale inputs and unavailable operations concisely. History is visibly temporary, not a permanent-save
claim. Acceptance into the project and independent Personal Library publication have separate outcomes.
Returning from audition restores current canonical sound, preserving intervening project edits; deleted
targets are not restored by preview. Specialized limits belong to
[Sample workflow](SAMPLE_WORKFLOW.md#intentional-and-lazy-exploration), with concrete Q-011/Q-029/Q-071
mechanisms/UI still open. Candidate navigation and lock changes do not enter document Undo/Redo.

## Project lifecycle and durable work

New project creation is a first-class document operation. Valuable unnamed/never-saved work must be
eligible for crash recovery without an existing final project path. Explicit Save is the user-confirmed
saved state; recovery maintains a separate rolling current working snapshot. After abnormal termination,
offer recovery rather than silently overwrite the last explicit saved version. Recovery existence or
acceptance alone must not replace the normal project file.
[PROJECT_FORMAT](PROJECT_FORMAT.md#recovery-state) owns recovery and integrity contracts.

Recovery feedback must identify the project or unnamed document, the captured work and its relationship
to the last explicit Save where one exists, and whether the candidate is usable, locally degraded or
unsafe/corrupt. A recovered R15 after saved R10 is recovered unsaved work; accepting it is not Save.
For unnamed recovered work, subsequent Save As chooses its first saved destination only on success.
For named recovered work, make the intended Save/Save As destination understandable before it can
replace the older saved version. Opening the older Save alone is not consent to discard newer recovery.
If an older valid candidate/Save is used as fallback, identify that fact and possible missing recent work.
No exact dialog, revision-number display, history browser or candidate-comparison UI is selected.

Save/storage failures must leave a concise persistent indication of unsaved work or unavailable/stale
recovery protection, with meaningful retry/continue/Save As direction where available. A failed Save
cannot look successful; an operation with uncertain completion must describe uncertainty until the
available coherent result can be established. Recovery success does not clear explicit-unsaved status.
Concurrent edits beyond a Save's captured revision remain unsaved. Missing material explains the actual
affected dependency and available repair/relink/replace/remove direction; do not imply that temporary
Browser source unavailability or missing generator code destroyed healthy accepted project audio.
Recording interruption identifies safely available material and gaps without promising unpersisted
samples. Keep feedback concise and primarily non-modal under
[recoverable failures](#graph-state-and-recoverable-failures); logs alone are insufficient. Exact windows,
status presentation, retry and reconciliation interactions remain open.

Ordinary media import/drag-and-drop accepts a durable project-managed resource. After successful
acceptance, moving/deleting the original arbitrary source file must not break normal project use.
Failed acceptance or Save/Save As/collect/relocate must preserve prior coherent project state under
[PROJECT_FORMAT](PROJECT_FORMAT.md#media-and-persistence-integrity). A deliberate advanced external
reference, if offered, needs separate justification and clear distinction from ordinary import.

## Project availability and dependency blockers

Distinguish document access, playback readiness, recording readiness and device/backend availability.
A safely understandable project remains accessible for available editing when suitable audio output
is absent; explain the unavailable operation and supported recovery direction as current UI state.
Input and output readiness may differ; an open editor must not imply that playback or recording works.
[PORTABILITY](PORTABILITY.md#portable-documents-and-environment-availability) owns platform claim limits;
Q-062/Q-069 retain exact device startup/loss/recovery and runtime-configuration mechanisms.

A safely understandable project normally opens degraded for local capability/resource failures:
missing/disabled/incompatible or recoverably failed ordinary plugins, missing/corrupt managed media,
failed media integrity validation/decoding and unavailable ordinary execution capabilities. Retain its
work and allow available document editing; hard project-open refusal belongs to critical document/schema/
corruption conditions preventing safe interpretation under [PROJECT_FORMAT](PROJECT_FORMAT.md#opening-and-migration).

Block operations whose dependency closure requires the broken capability, while unrelated editing
and healthy paths remain usable where semantics permit. Never silently omit required musical or
processing dependencies and report success. Explain the missing requirement concisely at the actual
affected object/node/instance/resource with restore/remove/replace direction. Broken media must remain
represented, with inspection, replacement/relink/repair or removal where meaningful; never silently
substitute unrelated audio. Exact wording/UI is unselected.

Persistent blockers must be represented as current UI state: affected-element marker, icon/badge,
restrained state color with non-color cues and concise inline/context explanation. Optional global
blocker count/navigation may help reach affected objects. A missing/corrupt media clip/item/resource
needs this persistent indication, potentially a placeholder and human-readable explanation, rather than
only a transient toast or log event. Disappearing notifications alone are
insufficient; logs remain diagnostic support. Keep the musical workspace clear rather than turning it
into a diagnostics dashboard. [UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns visual treatment.

This feedback also applies when available source capabilities or resources cannot realize the requested
independent execution. Identify affected uses and the unmet requirement, preserve editable intent and
explain supported recovery/fallback direction. Do not claim a silently merged, stale or substituted
result is correct. [AUDIO_ENGINE](AUDIO_ENGINE.md#execution-state-lifetime-and-resource-integrity) and
[EXTENSIONS](EXTENSIONS.md#independent-execution-capability) own execution/fallback constraints; final
overload and plugin-host UX remain open.

Behavior-affecting migration presents a concise summary and user choice before applying forced
fallback/default substitution or other nontrivial transformation, including older-version save
compatibility consequences under the format owner. Global destructive operations check known active
dependencies; package uninstall is blocked/deferred
under [EXTENSIONS](EXTENSIONS.md#instance-removal-and-package-uninstall).

Normal audio preferences select logical input/output devices/endpoints under
[SETTINGS](SETTINGS.md#audio-device-selection), with backend/driver integration internal to the platform.

## Graph state and recoverable failures

Contextual node creation must remain usable for repairing incomplete canonical intent. A normal right
click opens a persistent, navigable menu; users need not hold the button to reach nested actions.
Toolbar creation opens beside its initiating control. Explain each unavailable action without blocking
unrelated available creation. [Workspace](WORKSPACE.md#r4-f3-b-retained-graph-pane) owns the implemented
pointer/keyboard lifecycle; [Node graph](NODE_GRAPH.md#implemented-r4-f3-b-first-party-editor) owns target
safety and canonical command rules.

The editable graph is canonical; last-valid derived execution may continue in the current session
during candidate preparation/validation under
[NODE_GRAPH](NODE_GRAPH.md#editable-graph-and-audio-execution). When editable and executing graphs
differ because the candidate is invalid or not yet published, that state must be obvious to the user.
Prefer concise graphical feedback: status/iconography, affected node/connection highlighting,
restrained color and concise contextual explanation. Transient notification may supplement persistent
state, not replace it. Valid newer revisions converge automatically without manual Apply.

Save retains canonical work even when invalid; reopening restores it with blockers and affected
execution unavailable until fixed. Export freezes/validates/prepares canonical state and blocks required
invalid/missing dependencies, never silently exporting older playing state. User-visible preparation
requires cancellation, visible state/progress and finite failure handling while UI stays responsive,
without a universal timeout under [AUDIO_ENGINE](AUDIO_ENGINE.md#bounded-asynchronous-preparation).
Async completion revalidates its project/target/context before commit under
[ARCHITECTURE](ARCHITECTURE.md#document-integrity-and-asynchronous-publication); it cannot attach a
result for a deleted object to current selection. Accepted canonical edits enter document Undo even
while execution is preparing or using the visibly identified last-valid revision. Pending, stale,
failed or cancelled work must not appear as a successful edit; explicit reuse, where available, is a
separate choice. Concrete history/commit mechanisms remain Q-063.

User-facing failures should generally be concise and recoverable. Cascading modal `MessageBox`
dialogs must not be the normal error experience. Modal decisions remain available when genuinely
required, such as choices that cannot safely be inferred or destructive actions without a better
interaction. [UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns exact visual treatment.

## Live processing feedback

Future Live / Low-Latency mode must visibly communicate when the live processing path differs from
full intended processing. Switching it must not silently rewrite the project/graph; returning to normal
mode restores full processing. Final/offline render uses the intended full path rather than inheriting
temporary live bypass state. [AUDIO_ENGINE](AUDIO_ENGINE.md#live--low-latency-direction) owns direction
and unresolved latency/reporting/compensation/bypass policy, not a selected UI control.

## Application preferences and reset

The implemented global Settings center and its keyboard-only reset are owned by
[Settings](SETTINGS.md#unified-global-settings-center). New global options must use that center.
Broader future application/plugin-global preference resets follow
[SETTINGS](SETTINGS.md#reset-boundary). Configuration reset must not delete user projects,
project-managed audio/media, user-created samples/presets or Personal Library content. Exact button
layout and confirmation flow remain open.
Production diagnostics ordinarily need at most a simple enable/disable preference where useful;
developer logging levels belong outside ordinary GUI settings under [SETTINGS](SETTINGS.md#production-diagnostics).

### Language and theme preference changes

Settings applies explicit language/theme choices immediately. The title-row theme menu selects a
named theme with a current-item mark; it never cycles. Existing controls,
keyboard focus and document remain alive; no deferred restart is required in this bounded shell.
RU entries fall back individually to usable English; if an essential host entry is also unusable,
short independent English action/name labels remain available rather than exposing keys. Other missing
text gets a meaningful localized unavailable explanation. Blank, control-containing or overlong entries
are unusable. Dotted keys and first-party catalogs are host-owned, not an extension resource SDK.
Unnamed display is localized only while canonical name is null; user names and canonical numeric values
never translate. Numeric musical input retains its own explicit commit/cancel ownership.

Changing host language/theme with a project open is a presentation preference change, not a musical
edit. Preserve project content/relationships, ongoing editing intent, focus/selection and pending
operations without committing, discarding or reinterpreting user input merely to refresh presentation.
The current first-party host applies themes to retained controls and cached custom drawings, including
navigation thumbnails, without requiring a hover, resize, content edit or editor reopening to repaint.
UI behavior must be predictable: make clear when the new preference takes effect and any necessary
deferral. Applying safely at a defined boundary is permitted; instantaneous hot switching, rebuilding
every window or mandatory restart is not an accepted policy. Concrete behavior requires later UI evidence.

Missing translation/style resources use understandable host fallback without hiding working unrelated
capabilities. Different contributor language support may produce a local fallback without changing other
host surfaces. Independent native editors may retain their own language/theme; host-owned surrounding
UI follows the host preference. [Architecture](ARCHITECTURE.md#host-localization-and-ui-resources) owns
identity, per-resource fallback and locale/data separation; [UI design](UI_DESIGN.md#themes-and-semantic-resources)
owns semantic styles and non-color feedback. [Settings](SETTINGS.md#user-configuration-and-project-state)
retains preference ownership; exact storage/reset mechanics remain Q-053.

## In-window workspace and graph depth

R3-F2 implements the first two optional host panes with explicit open/activate, pointer movement/size,
collapse versus reversible Close (internally Hidden), bounded left/right docking and retained content. Pane activation
is not musical
selection or editing authority. The strip and Panes menu provide discoverable restoration/reopening;
F6/Ctrl+Tab, local pane actions, Escape and Ctrl+W supply complementary keyboard paths without global
hooks. Restore selects a valid live pane context, and hiding the last surface returns to the host action.
Language/theme refresh retain existing controls, working context and layout. Exact bindings and scope
are owned once in [Workspace](WORKSPACE.md#implemented-r3-f2-internal-panes); broader editor/native-editor
routing and accessibility remain open, not established by these two non-musical surfaces.

Major surfaces normally use internal workspace panes in one main window. Interaction activates a pane
and brings it ahead of overlapping panes, including when only part is visible. Moving/resizing, internal
floating, separation from docked groups, and collapse to an unobtrusive shelf/strip should preserve
working state on restoration. [WORKSPACE](WORKSPACE.md) owns the detailed pane contract and layout state.

This direction governs normal major Seqvium-authored surfaces. Third-party/native plugin editors may
use top-level OS windows when internal embedding is unsafe or impractical; their visual design need
not match Seqvium. [WORKSPACE](WORKSPACE.md) owns this exception and [EXTENSIONS](EXTENSIONS.md) owns
the host/editor lifecycle boundary.

Docking/magnetism is a per-pane user choice, predictable and easy to escape; automatic layout must not
fight free placement. Opening a project should normally retain the user's workspace preference.

When requested, the graph opens as a free-form spatial canvas with compact nodes and deeper settings
on demand. Coordinates do not define processing order; connections express dependencies. Ordinary
creation must not require learning graph internals; advanced branches/merges remain discoverable.
[NODE_GRAPH](NODE_GRAPH.md) owns graph semantics, ports, and the editing/execution boundary.
Compact-chain editing remains open; full node-editor presentation is a
[bounded implemented slice](NODE_GRAPH.md#full-node-editors-proposal).

## Essential accessibility feedback

Keyboard focus, selected targets and active context must be understandable. Important invalid state,
unavailable commands and pending operations need sufficient non-color/structural feedback where
applicable; color or transient animation alone is insufficient. Use persistent state and concise
contextual explanation appropriate to the action, without a second visual system or diagnostic-heavy
workspace. [UI_DESIGN](UI_DESIGN.md#feedback-and-motion) owns visual treatment; specific blocker and
preparation workflows above retain their specialized behavior.

Essential window/workspace actions must remain keyboard-accessible despite custom chrome under
[WORKSPACE](WORKSPACE.md#keyboard-access-and-focus-return). These are baseline future requirements,
not accessibility certification or implemented support. Screen-reader/accessibility-tree integration,
platform APIs, detailed navigation, DPI/minimum-size evidence and cross-platform acceptance remain
open under Q-064 in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).

## Interaction quality

Prioritize strong visual hierarchy, restrained intentional identity, readable typography, clear
selection/loading/error states, excellent drag-and-drop, useful context menus, sensible shortcuts,
and good note-editing interaction. Scaling and DPI correctness and responsive input belong in
verification when UI exists. Avoid an engineer-only interface.

First-party UI uses responsive/reflowing layout rather than uniformly scaling the application into
unreadability; [UI_DESIGN](UI_DESIGN.md#responsive-layout-and-usable-minimums) owns usable minimums.
The main window uses custom unobtrusive chrome without default TopMost or aggressive foreground/focus
activation under [WORKSPACE](WORKSPACE.md#main-window-chrome-and-foreground-behavior). These host
principles do not prescribe independently rendered third-party editor visuals.

Pleasant direct manipulation, spatial clarity, and restrained feedback should make experimentation
inviting. [UI_DESIGN](UI_DESIGN.md) owns gradual visual guidance and consistency between panes.

The implemented bounded Piano Roll gestures are specified above. Further bindings, onboarding and
editor integration remain evolvable; this contract does not freeze a final visual system.
