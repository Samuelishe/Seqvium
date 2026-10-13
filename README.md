# Seqvium

Seqvium is a free and open-source desktop music workstation in early alpha development.
It aims to make it easy to start with a sound or a musical idea, then discover deeper editing,
processing and sound design as the music takes shape.

The workflow takes inspiration from FL Studio: direct manipulation, reusable musical ideas and
flexible working surfaces. Seqvium is developing its own approach, with sample exploration and
node-based sound processing at its center. See the [project vision](docs/PROJECT_VISION.md) for
the wider direction.

## What you can do today

The current desktop provides a small working environment for samples, synthesized drums and music. You can:

- Import WAV samples and play the selected sample item.
- Add built-in Kick, Snare, Closed/Open Hat, Clap, Tom and Rim without sample files. Each has its own
  synthesis, useful parameters and three starting presets. Shape envelopes, audition repeatedly,
  save/load your own sound presets, and link hats for natural choke. Rack and Piano notes trigger the
  same sounds in Pattern/Song, Undo/Redo, Save/Open and WAV export.
- Create a Pattern in Channel Rack, add or drop WAV sounds, and paint/erase sixteenth-note steps.
  Choose 1–32 bars; fill, shift or clear a row, and delete channels or Patterns with Undo.
  Play a continuous loop with a visible position, change steps while it plays, and export the selected
  Pattern to WAV. Save/reopen it with its samples and continue editing.
- Build a song in Playlist: place shared Patterns on tracks, move and repeat clips, or Ctrl-drag a copy.
  Name and color tracks, use Mute/Solo, seek or loop a ruler range, and move or duplicate groups.
  Navigate with the overview, pointer zoom, horizontal scrolling or middle-button pan.
  Song plays overlapping Patterns together with a moving playhead; export the whole composition to WAV.
- Compose melodies, chords and bass in Piano Roll: draw, select, move/copy, resize and delete notes,
  edit velocity or exact properties, and audition pitches. Snap, zoom, scrolling and ghost parts keep
  editing musical. Rack and Playlist use the same notes; richer Rack rows show a safe melody preview.
  Set a WAV's source Root Pitch explicitly; matching, hash-verified catalog metadata can initialize it.
- Build a basic processing graph with Source, Gain, Mix and Output nodes: connect ports, move nodes,
  and pan or zoom the canvas.
- Adjust sample levels and mix independent WAV sources within the same graph.
- Double-click a node to open its reusable editor: adjust Gain with a physical dial, slider, exact amplitude
  and dB display, shape a Drum Voice source, or inspect Source, Mix and Output details.
- Undo and redo edits, save a project with its imported audio, and reopen it.
- Arrange internal workspace panes by moving, resizing, docking or collapsing them. Restore main-window
  size/position and use the Rack + Playlist layout for composing.
- Use the interface in English or Russian, with dark and light themes.

The graph makes signal flow visible: a sample can pass through Gain to Output, or several sources
can meet at a Mix node. These are basic level and routing tools; richer effects are still ahead.

For a first rhythm, follow [Create a drum pattern](docs/README.md#create-a-drum-pattern).
For pitched music, follow [Compose a melody](docs/README.md#compose-a-melody).
The [sample processing guide](docs/README.md#try-sample-processing) covers graph editing.

## Current state and limitations

Seqvium is an **early alpha**, not yet a ready replacement for FL Studio or Ableton Live.
It supports rhythm creation and a first working song arrangement workflow. Many advanced editing,
instrument and processing capabilities remain unfinished.

Step sequencing supports 1–32 bars in 4/4, continuous looping and live step editing. Piano Roll edits
the shared pitch/duration/velocity data; a Mixer editor is still ahead. Playlist arranges whole Patterns and their
repeats; section markers, independent variations, audio trimming/stretching and automation remain ahead.
WAV export covers either a selected Pattern or the whole Song, including the current processing graphs.
WAV channel edits preserve the other voices in the supported Rack graph. Short boundary smoothing
reduces sample-end clicks. WAV import supports mono/stereo PCM16/24/32 and float32 at 8–192 kHz;
FLAC remains unavailable. EQ and other effects, third-party plugin hosting,
MIDI input, recording and crash recovery remain missing.
Built-in drums use finite mono hits and shared sound parameters. Linked hats share one choke group
across channels and audible Song clips; advanced modulation, user-defined groups, stereo layers and
per-channel mix controls remain ahead.
Sampler pitching uses linear resampling, changing sample playback speed and natural duration; it is
not time stretching or a multisample instrument. The shared graph has a 32-voice budget.
Playback can glitch under heavy system load.

Windows 11 is the current development and tested desktop platform. Linux and macOS are intended
targets, but their application UI and audio operation have not been validated. Build instructions
below are for trying the application from source on Windows.

## Getting started

Install a .NET 10 SDK that matches [global.json](global.json), then open a terminal at the root
of your local checkout:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet run --project src/Seqvium.Desktop/Seqvium.Desktop.csproj -c Release --no-build --no-restore
```

The application opens an empty project and stays silent until you press Play.
Channel Rack opens in a pristine layout: create a Pattern and add built-in drums or your own WAVs; no sample library is
bundled. Save
projects you want to keep.
Launching it again brings the existing window forward where the OS allows, instead of opening
another workspace. If the running application does not respond, the new launch reports this and exits.

The [development guide](docs/DEVELOPMENT.md#current-environment-and-entry-points) covers the SDK,
project layout and local tools. The [documentation guide](docs/README.md) provides a first-use
walkthrough and routes to more detailed material.

## Where the project is heading

The next musical editing work can build on continuous Pattern playback and live edits. The longer-term
direction includes advanced note tools, richer Playlist editing, mixing, sample generation and resampling, detailed
effect and instrument editors, and optional extensions. Recording and external plugin hosting
are later goals.

These are planned capabilities, not features in the current build. The
[engineering roadmap](docs/ROADMAP.md) describes their scope and dependencies; it is not a release
schedule.

## Contributing

Bug reports, usability feedback, documentation improvements and focused code contributions are
welcome. Reports about sample playback or graph editing are especially useful while those workflows
are taking shape.

Read [Contributing](CONTRIBUTING.md) for reporting and development guidance, or use the
[documentation guide](docs/README.md#understand-the-design) to explore the architecture.

## License

Seqvium is licensed under the [Apache License 2.0](LICENSE).
Third-party components and assets retain their own licenses; see the
[third-party notices and provenance](docs/THIRD_PARTY.md).
