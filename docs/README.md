# Documentation

Start here to try Seqvium or find the developer documentation. The [project overview](../README.md)
describes what is available today and what is still planned. This guide explains a first sample
workflow and points to the detailed sources for each subject.

## Create a drum pattern

After [building and launching](../README.md#getting-started), use **Channel Rack**. A pristine layout
opens it; the left Rack icon or **Panes** menu reopens it. You can begin without any sample files.

1. Click **New pattern**. It creates an empty bar with 16 sixteenth-note steps in the current
   4/4 project; it does not change BPM or add preset hits.
2. Click **Add Drum Voice** for a synthesized kick. Its editor opens automatically: Tune sets pitch,
   Decay sets length, Tone shapes harmonics and Noise adds a transient. Drag Pitch/Amplitude curve
   points or use the exact fields; one completed adjustment is one Undo. Audition plays a quiet hit
   at the selected note/velocity. Double-click its channel name or use **… → Open sound editor** to return. **Add
   sound** or a dropped WAV adds other sounds, such as Snare/Hi-Hat. Sources mix automatically.
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
   rail button hides/restores the Pattern navigation at wide window sizes.
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

## Arrange a song

1. Create the Patterns you need in Channel Rack. Open **Playlist** from its rail icon or **Panes**.
2. Choose a source Pattern in Playlist and click a track to place it. Clips share the original notes;
   double-click a clip to return to its Rack. Overlapping clips play together, including on the same track.
3. Drag a clip to move it. Ctrl-drag makes a shared copy; dragging the right edge changes the number
   of whole Pattern repeats. Shift-click adds/removes clips from a selection; the selection tool draws a box and Ctrl+A
   selects all.
   Move, Ctrl-copy, Duplicate/Ctrl+D and Delete operate on the group in one Undo while retaining spacing.
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

The current Playlist has 32 fixed track slots and an editable view up to 4096 bars. Slots organize
compatible musical material; they do not assign instruments or create Mixer channels. The engine
checks a 32-voice song budget and refuses overload instead of dropping notes. Whole-Pattern repeats
are available now; section markers, independent variations, clip trimming and automation remain planned. WAV channel
add/delete/Undo/Redo keeps the current clock and other voices for the supported source/mix tree.
Other incompatible topology changes retain their restart policy.
See [Playlist scope](UX_CONTRACT.md#implemented-playlist-arrangement).

## Try sample processing

First [build and launch the desktop](../README.md#getting-started) on Windows. Start with a short
mono or stereo WAV at **8–192 kHz**: 16/24/32-bit PCM or 32-bit floating point, up to 16 MiB.
Playback still outputs at 44.1/48 kHz, with linear source resampling; FLAC is not supported.
No samples are bundled.

1. Choose **Import WAV** in the toolbar, or press Ctrl+I, and select your file. Toolbar icons have
   tooltips; secondary actions may move into the overflow menu in a narrow window.
2. Open **Node Graph**, or press Ctrl+G. The imported item starts with Source → Gain → Output.
   Source supplies the sample, Gain adjusts its level, and Output receives the result.
3. Double-click Gain to open **Node Editor**. Drag its dial vertically or use the level slider;
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
