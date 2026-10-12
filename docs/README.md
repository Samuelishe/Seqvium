# Documentation

Start here to try Seqvium or find the developer documentation. The [project overview](../README.md)
describes what is available today and what is still planned. This guide explains a first sample
workflow and points to the detailed sources for each subject.

## Try sample processing

First [build and launch the desktop](../README.md#getting-started) on Windows. Start with a short
mono or stereo WAV: 16-bit PCM or 32-bit floating point, up to 16 MiB. No samples are bundled.

1. Choose **Import WAV** in the toolbar, or press Ctrl+I, and select your file. Toolbar icons have
   tooltips; secondary actions may move into the overflow menu in a narrow window.
2. Open **Node Graph**, or press Ctrl+G. The imported item starts with Source → Gain → Output.
   Source supplies the sample, Gain adjusts its level, and Output receives the result.
3. Select Gain and enter a linear amplitude between 0 and 1 in the graph's parameter row; press
   Enter or the check button to commit. `1` preserves the level and `0` silences it. The desktop's
   current numeric input uses a decimal point, for example `0.5`, in either interface language.
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
