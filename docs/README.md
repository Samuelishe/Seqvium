# Documentation

Start here to try Seqvium or find the developer documentation. The [project overview](../README.md)
describes what is available today and what is still planned. This guide explains a first sample
workflow and points to the detailed sources for each subject.

## Create a drum pattern

After [building and launching](../README.md#getting-started), open **Step Sequencer / Channel Rack**
from the **Panes** menu. Use short supported percussion WAVs (the formats below apply).

1. Click **New Beat Pattern**. It creates an empty bar with 16 sixteenth-note steps in the current
   4/4 project; it does not change BPM or add preset hits.
2. Click **Add WAV Channel** for your Kick, Snare and Hi-Hat. Each import adds a separate row;
   source mixing is connected automatically for this initial graph.
3. Enable Kick steps 1/9, Snare 5/13 and Hi-Hat 1/3/5/7/9/11/13/15. Click again to remove a hit.
   Arrows navigate; Space/Enter toggles; Ctrl+Z/Y undo/redo. Edit the Pattern name and press Enter/check.
4. Press the existing toolbar **Play** (F5). It plays one bar and stops. Step edits stop playback;
   press Play again to hear the current bar. Longer samples cut at a quarter-note gate or the bar end,
   so this is best suited to short drums. Continuous loop and seamless live editing are not available yet.
5. Save the project. **Open** restores the channels/steps silently for further editing. Keep its
   `.seqvium.media` directory with the project. History starts fresh on Open.

The editor explains unsupported meter/Pattern shape or notes and preserves them. Adding channels to
an already shared Pattern or a custom graph can be refused safely; existing shared steps still edit
the shared musical content. Up to eight channels are offered; excessive overlapping voices/media can
block Play. [Editing scope](UX_CONTRACT.md#implemented-r5-f1-step-sequencing) explains these boundaries.

## Try sample processing

First [build and launch the desktop](../README.md#getting-started) on Windows. Start with a short
mono or stereo WAV at **44.1 or 48 kHz**: 16-bit PCM or 32-bit floating point, up to 16 MiB.
No samples are bundled.

1. Choose **Import WAV** in the toolbar, or press Ctrl+I, and select your file. Toolbar icons have
   tooltips; secondary actions may move into the overflow menu in a narrow window.
2. Open **Node Graph**, or press Ctrl+G. The imported item starts with Source → Gain → Output.
   Source supplies the sample, Gain adjusts its level, and Output receives the result.
3. Double-click the body of Gain to open **Node Editor**. Drag the level control and release to
   apply, or enter an exact linear amplitude between 0 and 1 and press Enter or **Apply**.
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
