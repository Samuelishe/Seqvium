# Seqvium

Seqvium is a planned free and open-source desktop music workstation with its own creative workflow,
focused on turning musical ideas and sound discoveries into tracks.

> Easy to start, deep enough to grow.

## What is Seqvium?

The intention is an approachable music-making environment that grows with the user: start with a
rhythm or melody, then explore deeper editing, arrangement, processing, and sound design as needed.
Seqvium is intended for music across genres, with pointer editing complemented by future MIDI input
and audio recording.

## Creative direction

Sound and music should lead the experience. The design emphasizes quick feedback, experimentation,
and a short path from exploring a sound to hearing it in musical context and reusing it as new material.
FL Studio is the nearest workflow reference; Seqvium aims to develop its own identity.
See the [project vision](docs/PROJECT_VISION.md) for the product direction.

## Key ideas

These are planned capabilities and design directions:

- **Mouse-first creation:** edit notes, steps, clips, and sounds directly with the pointer.
- **Named reusable multi-instrument patterns:** choose the size of an idea, from one part to a full groove.
- **Sound and sample exploration:** use a contextual Sample Lab to audition variations in the music
  before accepting them.
- **What-you-hear resampling:** turn the audible result of a selected source or context into reusable material.
- **Progressive processing depth:** open node-based processing when deeper sound design is useful.
- **A flexible in-window workspace:** arrange working panes and organize instruments/channels under user control.
- **Semi-free Arrangement:** arrange compatible musical material in user-named containers, with flexible organization.

## Current status

**There is currently no runnable Seqvium workstation.** SEQ-R1 provides a .NET 10 canonical musical
model, document edits/Undo/Redo and versioned JSON Save/reopen, with a managed test suite.
SEQ-R0 has bounded experimental audio evidence and remains **partially evidenced**; it selects no
permanent backend. Production audio, GUI and later workflows remain future work.
The [roadmap](docs/ROADMAP.md) describes intended stages, not available features or release promises.

## Platforms

Windows, Linux, and macOS are the intended targets. Windows is the primary early development environment;
R1 currently has local Windows build/test evidence. R0 device observations are experimental;
no production desktop/audio or supported-distribution acceptance is claimed.

## Development

Start with the [documentation index](docs/INDEX.md). Repository agents should read [AGENTS.md](AGENTS.md).
See [development](docs/DEVELOPMENT.md) and [test execution](docs/TEST_EXECUTION.md) for reproducible
CLI commands. `Seqvium.sln` contains `Seqvium.Core` and `Seqvium.Tests`; there is no UI executable yet.

## License

Seqvium-authored work is licensed under the [Apache License 2.0](LICENSE) (`Apache-2.0`).
Third-party components and assets retain their respective licenses and terms; see
[third-party provenance and evaluation](docs/THIRD_PARTY.md).
