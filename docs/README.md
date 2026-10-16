# Documentation

Start here to try Seqvium or find the developer documentation. The [project overview](../README.md)
describes what is available today and what is still planned. This guide explains a first sample
workflow and points to the detailed sources for each subject.

To start with a complete editable example, open [Neon Orchard](../examples/Neon%20Orchard/README.md).
It includes all required media and saved Mixer processing, with instructions for Song playback,
WAV export and personal Save As copies. The composition will grow with the application.

Use **New project** in the toolbar or **Ctrl+N** for a fresh document. If the current project has
changes, choose Save, Discard or Cancel. An empty document starts silently with one compact Channel Rack;
opening music restores your previous musical layout. In Pattern Mode, click navigator cards to switch
the playing phrase without pressing Play again. Double-click or Enter opens an appropriate editor;
right-click offers Channel Rack or Piano Roll for a named channel. Explicit choices are remembered
for each Pattern until you replace the document.
The top group on the left rail controls **Browser** (folder/sound) and **Patterns** (two phrase cards).
Both panels can stay open together, in that order. Drag a panel's right edge to resize it; pull it
nearly to its left edge to close it. At narrow widths its contents gradually slide behind the edge.
Click its rail icon to restore the last comfortable width. Each panel remembers its own width and
visibility with the workspace, independently of the music. Smaller windows fit both panels without
automatically closing either one.

## Application settings

Open **Settings** at the right end of the toolbar (default **Ctrl+,**). General selects language and
theme; Controls remembers Browser auto preview and lets you search, assign, clear or restore global
keyboard shortcuts. Reassign explicitly transfers an occupied chord. Editor and system keys remain
fixed. About shows the application version. The small sun/moon in the title row opens the theme menu.
These preferences belong to your application, independently of saved music.

## Create a sound

Open **Sample Lab** from the rail or Panes menu. Choose **Kick** or **Clap**, press **Generate**, and
use **Solo** to listen while transport is stopped. Select a recent candidate and press the reference
icon to base **Variation** on it. **Shape** reveals controls and locks; changing family clears those
constraints. The recent list is temporary and disappears on New/Open/close.

**Keep** saves reusable audio in the project. **+ Rack** adds a new sampler channel; an empty project
gets a playable first Pattern. **+ Playlist** places a clip at the cursor on lane 1. Several candidates
can be kept independently. Project Undo/Redo changes accepted music, not candidate navigation.

For a supported WAV/PCM channel, open Sample Lab from its Rack menu. Play that Pattern or Song and
choose **In music** to hear the candidate through its existing processing and Mixer. This temporarily
substitutes the shared sound's future hits, including its other Song uses. **Original** or Stop ends
it. **Replace shared** explicitly changes that sound across its Patterns; it is not a one-channel
unlink. Existing pitch/root and note release still apply. Drum Voice and Audio Clip context targets
are outside this first version.

Browser → **Extensions** can disable **Percussion Lab**. Accepted sounds remain ordinary project
media and play after saving/reopening without the generator. See the
[sample contract](SAMPLE_WORKFLOW.md#implemented-sample-lab-v1) for precise limits.

## Find and use sounds

Browser also includes **Extensions**. Choose the optional **Found Sounds** pack and
**Sounds** for three original short sounds, then preview/add them like other WAVs. **Disable**
hides the pack from new uses while imported audio stays in the project; **Enable** restores it.
The disclosure explains unavailable packages. There is no external plugin installer.

Open **Browser** from the left rail or press **Ctrl+B**. Use the folder button to add your sample
folder. Open subfolders with double-click/Enter, go up with the arrow/Backspace and return with
Back/Alt+Left. The address field accepts a folder path. Pinned folders and recent locations remain
available next time; forgetting a location does not delete its files.

Type a name such as `Kick` into search. Search covers the open folder; **Load next page** continues
large folders without scanning your whole library. Select a file to see format, duration, sample
rate, channels and its real waveform. Press Space/Enter or the preview triangle to listen. Optional
**Auto preview** plays each new selection; Stop/Escape ends it. If music is playing, stop transport
explicitly before solo preview. Browsing/listening never imports audio or consumes Undo.

Drag a WAV into **Channel Rack** to create an empty sampler channel, or choose **+ Rack** for the
selected Pattern. An empty project gets its first Pattern in the same Undo action; click steps to
make a rhythm. Rack retains its 16 MiB WAV limit. Drag WAV/MP3/FLAC into **Playlist** at the desired
lane/time, or choose **+ Playlist** to add at the Song cursor on an unused lane. MP3/FLAC use the
Windows codecs available on your machine.

Choose **Project resources** to find and reuse accepted audio after its external original is gone.
Managed WAV can become another Rack channel or Audio Clip; normalized Playlist resources remain
available as Audio Clips. Reuse keeps existing media and creates an independent sound/use, so it
does not change earlier instruments. Save and Save As retain project-owned media normally; keep
the project's `.seqvium.media` directory beside the project file.

## Create a sample from a Pattern

Right-click the Pattern's card and choose **Create sample**. Browser opens with the ready sample
selected in **Project resources**. Preview it, then choose **+ Rack**, **+ Playlist**, or drag it into
the music. Your original Pattern remains editable. Ctrl+Z/Y undoes/redoes accepting the sample;
later musical uses have their own Undo actions.

The sample contains that Pattern's instruments and local Source/Gain/Mix processing, before Mixer
channels, buses and Master. It includes the whole phrase and supported release/drum tails. Save As
and moving the project together with its `.seqvium.media` directory keep the result self-contained.

Rendering can be cancelled beside the Pattern list. Editing the project while it runs discards the
outdated result; run the action again after editing. Unsupported local processing/dependencies and
unavailable source audio produce an error. Output is 48 kHz mono/stereo PCM16 within the Rack's
16 MiB WAV limit (about 87 seconds stereo). A longer result is refused; overload asks you to lower
local Gain. Audible-mix capture, source replacement and VST are not available.

## Work with audio clips

Open **Playlist** from the left rail. Drop WAV, MP3 or FLAC from Explorer onto a lane, or use Playlist's **Import
audio** button. You do not need a Pattern or Channel Rack. Launch Seqvium without elevation;
Windows prevents Explorer from dropping files into an administrator process.

1. The clip shows the actual mono/stereo waveform. Drag its body to move it; Ctrl-drag copies it.
   Snap controls placement. Moving a clip or changing BPM does not change source pitch or speed.
2. Drag either edge to trim a single occurrence, or double-click/Enter to edit exact source offset,
   duration, fade times and gain. Drag the upper waveform-corner handles for fades. Escape cancels
   unfinished gestures; Ctrl+Z/Y undo/redo completed edits.
3. Use **Slice** and click inside a clip, or **Split at cursor**. Splitting preserves the original
   fade envelope and source continuity. Set repeats to one before splitting. **Repeats** repeats the
   selected source range; it is not tempo stretching. Ctrl+D duplicates the selected audio group.
4. Clip properties choose the shared source's Mixer route or create an explicit channel. Copies share
   this route. Playlist track Mute/Solo gates occurrences, while Mixer Mute/Solo affects routed audio.
5. Use Playlist Play, ruler seek/range loop and Song WAV export just as with Patterns. Mixed audio and
   Patterns pass through the existing Mixer. Save As creates an independent project and managed media;
   keep the associated `.seqvium.media` folder with it. Original imports can subsequently be unavailable.

Import runs off the UI/audio threads and can be cancelled from Playlist. Errors leave the project and
Undo history unchanged. WAV PCM16/24/32 and float32, MP3 and FLAC are accepted at 8–192 kHz, mono/stereo
only. MP3/FLAC require available Windows Media Foundation codecs. Audio is resident, not disk-streamed:
one source and a prepared Song's unique Audio Clip PCM each have a 512 MiB decoded limit (about
23:18 at 48 kHz stereo). Copies do not multiply source PCM; other preparation/UI/history memory remains.
Pattern media has a separate budget and all playback shares 32 voices. No time stretch/warp is available.
See [audio execution limits](AUDIO_ENGINE.md#standalone-audio-clips).

## Create a drum pattern

After [building and launching](../README.md#getting-started), use **Channel Rack**. A pristine layout
opens it; the left Rack icon or **Panes** menu reopens it. You can begin without any sample files.

1. Click **New pattern**. It creates an empty bar with 16 sixteenth-note steps in the current
   4/4 project; it does not change BPM or add preset hits.
2. Click **Add drum…** and choose Kick, Snare, Closed/Open Hat, Clap, Tom or Rim. The sound editor
   opens with a useful starting sound and three presets. Each instrument has its own controls:
   Snare blends body and snappy noise; Clap spreads bursts; Tom exposes pitch drop and resonance.
   Rotate dials clockwise to increase and counterclockwise to decrease; Shift adjusts finely.
   Drag envelope points or enter exact values; each completed adjustment is one Undo and plays a
   quiet hit when music is stopped. The large **▶** repeats the selected note/velocity immediately;
   **■** beside it cuts the sample. The small save/open buttons exchange custom
   `.seqdrum` sound presets. Linked hats cut each other; switch **Hat choke** off for independent tails.
   Double-click the channel name or use **… → Open sound editor** to return. **Add sound** or a dropped
   WAV adds a sample channel. Sources mix automatically; rhythm stays in Rack.
3. Enable Kick steps 1/9, Snare 5/13 and Hi-Hat 1/3/5/7/9/11/13/15. Click again to remove a hit.
   Drag across a row to paint or erase; release commits the whole gesture, Escape cancels. The row's **…** menu fills
   every four/two steps, shifts or clears. Arrows navigate; Space/Enter toggles;
   Ctrl+Z/Y undo/redo. Edit the Pattern name and press Enter/check. Click BPM to set tempo.
   At wider window sizes, the left Pattern list offers creation and rhythm previews. For balance, type
   a fractional toolbar Gain such as `0.22`, then Enter or leave the field; Escape cancels the draft.
4. Press **Play** (F5) for a continuous cycle; Stop ends it. The outlined column and bar:beat:sixteenth
   display follow the audio renderer. Change steps while it plays: future steps update after preparation,
   and already passed steps are heard next time around. Unchanged sounding notes keep their position.
   The synthesized kick plays its full finite envelope and its tails can overlap across the bar.
   Long WAV samples still cut at a quarter-note gate or the Pattern end, so short sampled drums work best.
5. Choose the number of bars in the Pattern header (1–32). Added bars start empty. Clear late steps
   before shortening; length and BPM changes stop playback. The row **…** menu deletes a channel,
   and the header × deletes the Pattern and its occurrences; Undo restores them. The top left **…**
   rail button hides/restores the Pattern navigation, including explicit opening in narrow windows.
6. Use toolbar **… → Export selected Pattern to WAV** for a 48 kHz stereo PCM16 file with its
   current graph processing. Lower Gain and export again if clipping is reported. This exports one
   cycle of the selected Pattern; use Playlist below for a complete arrangement.
7. Save the project. **Open** restores the channels/steps silently for further editing. Keep its
   `.seqvium.media` directory with the project. History starts fresh on Open.

The editor explains unsupported meter/Pattern shape or notes and preserves them. Adding channels to
an already shared Pattern or a custom graph can be refused safely; existing shared steps still edit
the shared musical content. Up to eight channels and 32 simultaneous voices are offered in Rack and
Song; excessive overlapping voices/media can block
Play. [Editing scope](UX_CONTRACT.md#implemented-r5-f1-step-sequencing) explains these boundaries.

## Compose a melody

1. Create a Pattern in Rack, choose its length, and add a melodic WAV. Double-click the channel name
   or choose **Piano Roll** from its row menu; the piano rail icon and **Panes** also open the editor.
2. Check **Root Pitch** before composing. This is the note actually recorded in the source WAV, not
   the note you want to play. C4 means MIDI 60. Set the documented source note; if it is unknown, the
   default C4 is only a configuration, not a detected pitch. A matching nearby `catalog.json` with
   `file`, `root_midi` and SHA-256 can supply a verified initial root. The source popup also names the channel.
3. Click the musical grid to draw, or drag an empty row for a longer note. Drag note bodies to move,
   Ctrl-drag to copy, and drag right edges to resize. Shift-click extends selection; Shift-drag empty
   space or the selection tool draws a box. Right-click/Delete removes notes. Escape cancels a held edit.
4. Drag the aligned velocity handles; staggered handles keep chord voices individually reachable.
   The properties button exposes exact pitch, start, duration and velocity. Group edits change only
   the fields you actually adjust; 100% velocity means intensity 1. Each finished gesture is one Undo.
5. Choose snap (including eighth-note triplets or free). Wheel scrolls pitch, Shift+wheel scrolls
   time, Ctrl+wheel zooms around the pointer, and middle-drag pans. Keyboard arrows nudge; Shift+left/right
   changes duration, Shift+up/down moves an octave. Ctrl+A/C/X/V/D selects/copies/cuts/pastes/duplicates;
   Alt+left/right chooses a note without moving it, Enter auditions. Ctrl+Z/Y undo/redo.
6. Keyboard clicks and the audition toggle provide quiet source-note previews while stopped. Play uses
   the real Pattern/Song transport and processing graphs. Place the Pattern in Playlist for a song;
   all placements share your edits. Save/Open and Pattern/Song WAV export preserve this musical data.

Returning to Rack does not quantize or erase richer notes. A melody preview replaces incompatible
step pads; open Piano Roll to edit it. Root changes affect every use of the shared sound, not the
stored note pitches or WAV bytes. Linear sample pitching changes natural duration; there is no
sustain loop, time stretching, MIDI recording or automatic pitch detector yet.

To try another recording for an existing instrument, choose **Replace WAV…** from its Rack row
menu. It updates every Pattern using that instrument while keeping notes, local processing and Mixer
routing. Undo restores the original sample. Verified library tuning is used when available; otherwise
Root Pitch stays unchanged. Check source tuning and Mixer trim when the new recording differs.

## Arrange a song

1. Create the Patterns you need in Channel Rack. Open **Playlist** from its rail icon or **Panes**.
2. Choose a source Pattern in Playlist and click a track to place it. Clips share the original notes;
   double-click a clip to open its Pattern editor. Overlapping clips play together, including on the same track.
3. Drag a clip to move it. Ctrl-drag makes a shared copy; dragging the right edge changes the number
   of whole Pattern repeats. Shift-click adds/removes clips from a selection; the selection tool draws a box and Ctrl+A
   selects all.
   Patterns and Audio Clips share this selection. Move, Ctrl-copy, Duplicate/Ctrl+D and Delete operate
   on the group in one Undo while retaining spacing and pauses. Ctrl+C/X/V copies/cuts/pastes at the
   Song cursor on the last targeted track. Clip properties set the group's earliest bar and track exactly.
   Duplicate appends the group after its last end. Delete removes only the selected clips.
   Ctrl+Z/Y restores edits. The snap selector offers bars, beats, sixteenths or free movement;
   Shift temporarily uses beats and Alt temporarily moves freely.
4. Click a track heading to name it or choose its color. **M** mutes that track; **S** solos it.
   Multiple solo tracks play together; Mute still silences a solo track. These are song gates,
   independent of the source Pattern's Rack loop, and are saved with the project.
5. Playlist's Play button starts at the chosen song cursor; click its ruler to seek, or drag the ruler
   to define and enable a loop. The loop button disables/re-enables the retained range; Start returns to zero.
   Cursor/range are session state and do not change Save, Undo or whole-Song export. The global **Pattern / Song**
   button
   chooses between the continuous Rack and finite Playlist; switching stops transport. Select/edit
   another Rack source during Song without switching its playback target. Compatible clip/note/Mute/Solo
   edits continue from the current time; passed new notes wait for a future occurrence.
6. Use Shift+wheel for horizontal scrolling, Ctrl+wheel to zoom around the pointer, or middle-drag to pan.
   Drag the overview viewport to navigate, its ends to zoom, or choose Fit song. Track headings remain visible when
   scrolling
   horizontally. Follow playback keeps the renderer's cursor in view and can be turned off.
7. Use Playlist's export button or **… → Export song to WAV** for the whole arrangement, including
   leading gaps and overlaps, as 48 kHz stereo PCM16. Save/Open preserves the clips, tracks and samples;
   Open stays silent. Keep the `.seqvium.media` directory with the project.

8. Add named sections using **+ Section**. The retained ruler range initializes the range; exact fields
   use one-based bars and lengths in bars. Click a section or choose its name to seek without playback;
   double-click edits it. Earlier/Later swaps adjacent sections; Repeat section inserts a copy of its
   musical material and shifts later music. The context menu selects its clips, loops it or removes
   only the label. A crossing clip blocks restructuring with its name/track and **Show crossing clip**;
   adjust the range or explicitly split audio, then retry. Every accepted edit is one Undo.
9. Shared clips show **≋**. Their Rack/Piano edits affect the shared Pattern. Select one clip and choose
   **Make variation** for a fill or altered phrase: it opens an independent Pattern target in its editor,
   with **↗** ancestry and its own notes/processing. Instruments and Mixer routes remain shared.
   Rename it and edit through Piano Roll; other uses retain their original notes. Copying that variation
   shares its new Pattern. A clip's internal repeats still use one Pattern; separate the desired
   occurrence into its own clip before making only the final repetition a fill.

The current Playlist has 32 fixed track slots and a view that expands for long audio, with timeline
presentation/exact bar entry up to one million bars. Slots organize
compatible musical material; they do not assign instruments or create Mixer channels. The engine
checks a 32-voice song budget and refuses overload instead of dropping notes. Whole-Pattern repeats
are available now; Audio Clips have separate range/trim/split/fade controls. Nested sections, flexible
cross-boundary rearrangement, dynamic track groups, stretch and automation remain planned. WAV channel
add/delete/Undo/Redo keeps the current clock and other voices for the supported source/mix tree.
Other incompatible topology changes retain their restart policy.
See [Playlist scope](UX_CONTRACT.md#implemented-playlist-arrangement).

## Mix your music

Open **Mixer** from the rail or **Panes**, or use a Rack sound's **Open Mixer** action. In an older
project click **Create Mixer**: it begins at unity, grouping equal sound-row names as an editable
starting point. It does not rewrite notes or replace the existing graph Gain.

1. Choose Song or Pattern playback. Drag a channel fader vertically, or enter its dB value; positive
   values amplify, -90 is silence. Shift gives fine adjustment; double-click restores unity. Use the
   compact dial or exact bipolar value to balance left/right. Real meters show peak/RMS; PK is held
   peak and GR is compressor reduction. Reset peaks clears measurements, not music.
2. Click a source name or processing summary to open its side inspector. EQ provides high-pass,
   low/high shelves and a tunable mid band; drag its response handles or enter exact values. Compressor
   has threshold, ratio, attack/release, knee and makeup. Enable processing explicitly. The plots are
   analytical curves, not a live spectrum or loudness meter.
3. In Routing, use Trim for extra source gain, choose Output, assign shared sounds, or add a send. **Add bus** creates
   an empty channel; route drum outputs to it for shared compression, for example.
   A pre-fader send is after processing/pan and before level; Mute cuts it too. Cycles are refused.
   Playlist tracks still organize clips: a sound can share one Mixer channel across several tracks.
4. Keep Master visible and watch overload indicators. Optional **-1 dB peak ceiling** protects sample
   peaks, not intersample true peaks. It cannot certify mastering quality. Save, reopen and use the
   existing Pattern/Song WAV export; the mix is included. Retain the `.seqvium.media` directory.

The bank scrolls horizontally instead of widening every strip. Close its inspector to regain space
in a small pane. Completed gestures/valid exact entry are one Undo; Escape cancels unfinished edits.
There is currently no delay/reverb, automation, plugin rack or sidechain. See the
[Mixer workflow](UX_CONTRACT.md#implemented-mixer-workflow) for detailed behavior.

## Try sample processing

First [build and launch the desktop](../README.md#getting-started) on Windows. Start with a short
mono or stereo WAV at **8–192 kHz**: 16/24/32-bit PCM or 32-bit floating point, up to 16 MiB.
Playback still outputs at 44.1/48 kHz, with linear source resampling. This Rack/graph import route is
WAV-only; [Playlist Audio Clips](#work-with-audio-clips) also accept MP3/FLAC. No general sample library
is bundled; the official demo includes its own reviewed media.

1. Choose **Import WAV** in the toolbar, or press Ctrl+I, and select your file. Toolbar icons have
   tooltips; secondary actions may move into the overflow menu in a narrow window.
2. Open **Node Graph**, or press Ctrl+G. The imported item starts with Source → Gain → Output.
   Source supplies the sample, Gain adjusts its level, and Output receives the result.
3. Double-click Gain to open **Node Editor**. Rotate its dial clockwise to increase the level,
   counterclockwise to decrease it, or use the horizontal level slider;
   release applies once, Shift adjusts finely and Escape cancels. Double-click the dial for unity,
   or enter an exact linear amplitude between 0 and 1 and press Enter or **Apply**.
   `1` preserves the level and `0` silences it; the editor also shows dB attenuation. Expand **Curve**
   to see the input/output relationship. Escape cancels unfinished input. When the editor is hidden
   or collapsed, the graph's quick Gain input is available; it uses a decimal point, for example
   `0.5`, in either interface language.
4. Press **Play** to hear the selected item through the Windows default output. Press **Stop** to
   stop it. Moving nodes changes the drawing; connections determine the processing path.
5. Use **Save As** to keep the project. If you move or copy `Song.seqvium`, keep its associated
   `Song.seqvium.media` directory with it. Reopening stays silent until you press Play again.

For several samples in one graph, use **Add WAV source to current item** inside Node Graph. The new
Source starts unconnected. Create a two-input Mix, replace the existing Gain → Output connection
with Gain → Mix → Output, and connect the new Source to the other Mix input. Use matching channel
layouts on connected nodes. Separate Gain nodes before Mix let you balance the sources. An incomplete
graph shows a playback blocker. During playback it may keep the previously working graph audible,
so read the status before assuming that an unfinished connection is being heard.

The [sample workflow](SAMPLE_WORKFLOW.md#r4-f3-b-add-wav-to-current-item) explains import and mixing
boundaries; the [project format](PROJECT_FORMAT.md#r2-f1-managed-wav-layout) explains audio
storage. These are detailed engineering documents, not additional steps required for a first listen.

You can also open a node through **Open Editor** in its context menu, or press Enter when the node
is selected and the graph canvas has focus. Source, Mix and Output show their bindings and signal
paths. Selecting another node leaves the editor on its current target until you explicitly open
the new node. Resize, dock, collapse or hide the editor like the other internal panes; use the panes
menu to reopen it. The transfer graph is analytical: it shows the setting, without measuring audio.

## Build and contribute

- [Development](DEVELOPMENT.md#current-environment-and-entry-points): SDK requirements, solution
  projects, building, launching and local tooling.
- [Contributing](../CONTRIBUTING.md): bug reports, feedback and preparing a focused contribution.
- [Testing](TEST_EXECUTION.md#current-and-future-execution): the actual test runner and commands.
- [Coding guidelines](CODING_GUIDELINES.md): code structure, naming, concurrency and resource ownership.

## Understand the design

Seqvium keeps musical edits in one project document. The interface displays that document, while
audio runs from prepared data suitable for playback. This lets editing and Undo remain coherent as
processing changes. The [architecture guide](ARCHITECTURE.md) explains those responsibilities and
the distinction between samples, their musical uses and processing contexts.

- [Project vision](PROJECT_VISION.md): the intended music-making experience.
- [Node graph](NODE_GRAPH.md): signal flow, mixing and the boundary between editing and execution.
- [Workspace](WORKSPACE.md): internal panes, activation, docking and layout.
- [Project format](PROJECT_FORMAT.md): saving, imported audio and compatibility.
- [Extensions](EXTENSIONS.md): future optional capabilities and their lifecycle constraints.

These documents distinguish implemented behavior, accepted design, proposals and open questions.
Their planned sections do not describe features already available in the desktop.

## Follow development

The [roadmap](ROADMAP.md) gives the development sequence and scope. [Known problems](KNOWN_PROBLEMS.md)
records unresolved engineering questions; [platform guidance](PORTABILITY.md) distinguishes intended
targets from tested application support. For exact implementation and verification status, see the
[engineering handoff](PROJECT_STATE.md).

The [engineering owner index](INDEX.md) maps subjects to their canonical technical documents.
The [documentation policy](DOCUMENTATION_GOVERNANCE.md#audiences-and-document-purpose) explains how
these pages and the canonical engineering sources stay consistent.
