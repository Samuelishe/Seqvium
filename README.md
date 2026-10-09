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

**A runnable desktop foundation now exists; music editors are not implemented.** SEQ-R1 provides a .NET 10 canonical
musical
model, document edits/Undo/Redo and versioned JSON Save/reopen, with a managed test suite.
SEQ-R0 remains **partially evidenced** and selects no permanent backend. R2-F1 provides managed WAV
preparation/offline sampling; R2-F2 has locally accepted-ready bounded realtime playback through a
Windows WASAPI output adapter. R2-F3 adds bounded source discovery, transient raw WAV preview and
explicit reuse of accepted media without copying WAV bytes. R2-F4 adds independent logical input/output
endpoint discovery/selection and joined output replacement. **Overall R2 is complete / local accepted-ready**
for this bounded foundation. **R3 is complete / local accepted-ready**: one Avalonia main window,
actual unnamed project summary, RU/EN and Dark/Light preferences, two retained internal panes,
bounded floating/docking, keyboard access and independent user layout persistence. The owner accepts
F1/F2 within this scope; wider platform/accessibility and delivered-release evidence remain open.
Opening the desktop never starts audio. Musical editors and input streaming/recording are absent.
See the [F2 device/performance evidence](docs/experiments/SEQ-R2-F2_REPORT.md) for its measured scope.
The [F4 endpoint evidence and R2 audit](docs/experiments/SEQ-R2-F4_REPORT.md) records the selection boundary.
The [roadmap](docs/ROADMAP.md) describes intended stages, not available features or release promises.

## Platforms

Windows, Linux, and macOS are the intended targets. Windows is the primary early development environment;
The managed solution has local Windows build/test evidence; F2 physically validates one Windows 11
44.1 kHz stereo float32 output endpoint. Other endpoint/platform and supported-distribution acceptance
is not claimed.

## Development

Start with the [documentation index](docs/INDEX.md). Repository agents should read [AGENTS.md](AGENTS.md).
See [development](docs/DEVELOPMENT.md) and [test execution](docs/TEST_EXECUTION.md) for reproducible
CLI commands. `Seqvium.sln` contains portable `Seqvium.Core`, `Seqvium.Tests`, narrow
`Seqvium.Audio.Windows`, the explicit physical `Seqvium.DeviceCheck` harness and `Seqvium.Desktop`.
After building, launch the silent desktop with:

```text
dotnet run --project src/Seqvium.Desktop/Seqvium.Desktop.csproj -c Release --no-build --no-restore
```

## License

Seqvium-authored work is licensed under the [Apache License 2.0](LICENSE) (`Apache-2.0`).
Third-party components and assets retain their respective licenses and terms; see
[third-party provenance and evaluation](docs/THIRD_PARTY.md).
