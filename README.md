# Seqvium

Seqvium is a free and open-source desktop music workstation in early alpha development.
It aims to make it easy to start with a sound or a musical idea, then discover deeper editing,
processing and sound design as the music takes shape.

The workflow takes inspiration from FL Studio: direct manipulation, reusable musical ideas and
flexible working surfaces. Seqvium is developing its own approach, with sample exploration and
node-based sound processing at its center. See the [project vision](docs/PROJECT_VISION.md) for
the wider direction.

## What you can do today

The current desktop provides a small working environment for playing and processing samples. You can:

- Import WAV samples and play the selected sample item.
- Build a basic processing graph with Source, Gain, Mix and Output nodes: connect ports, move nodes,
  and pan or zoom the canvas.
- Adjust sample levels and mix independent WAV sources within the same graph.
- Double-click a node to open its reusable editor: adjust Gain with a level control, exact amplitude
  and dB display, or inspect Source, Mix and Output details.
- Undo and redo edits, save a project with its imported audio, and reopen it.
- Arrange internal workspace panes by moving, resizing, docking or collapsing them.
- Use the interface in English or Russian, with dark and light themes.

The graph makes signal flow visible: a sample can pass through Gain to Output, or several sources
can meet at a Mix node. These are basic level and routing tools; richer effects are still ahead.

For a first listening session, follow the [sample processing guide](docs/README.md#try-sample-processing).

## Current state and limitations

Seqvium is an **early alpha**, not yet a ready replacement for FL Studio or Ableton Live.
It is useful for trying sample playback and graph editing, but it does not yet offer a practical
workflow for composing complete tracks.

There is no step sequencer, Piano Roll, Arrangement or Mixer editor yet. Richer node editors,
EQ and other effects, third-party plugin hosting, MIDI input, audio recording, export and crash
recovery are also still missing. Playback can glitch under heavy system load.

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
Import your own WAV to begin; no sample library is bundled. Save projects you want to keep.

The [development guide](docs/DEVELOPMENT.md#current-environment-and-entry-points) covers the SDK,
project layout and local tools. The [documentation guide](docs/README.md) provides a first-use
walkthrough and routes to more detailed material.

## Where the project is heading

The next musical editing work focuses on reusable patterns and step sequencing. The longer-term
direction includes Piano Roll, Arrangement, mixing, sample generation and resampling, detailed
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
