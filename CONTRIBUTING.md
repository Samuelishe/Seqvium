# Contributing to Seqvium

Seqvium is still a small alpha project. Reports from people trying sample playback and graph editing
help identify what works, what is confusing and what needs attention. Code, documentation and design
feedback are welcome too.

## Report a problem or suggest an improvement

Use the repository's issue tracker. For a bug, include:

- What you were trying to do, the steps to reproduce it, and what you expected instead.
- The version shown in About, or the source revision if you built it yourself.
- Your operating system; for audio problems, the output device and relevant settings. For Audio Clips,
  include format, sample rate, channels, duration, BPM, trim/fade/repeat settings and whether the issue
  occurs in playback, seek/loop or Song WAV export. Mention elevated application launch when reporting
  a crossed-out Explorer drop cursor.
- A small example project or sample, when useful and when you can share it.

Remove private paths and personal information from logs or screenshots. Share only audio and other
material you have permission to distribute. For usability feedback, describe the musical task and
where the interaction became difficult; a screenshot of the actual application can help.

For feature suggestions, check the [current capabilities](README.md#what-you-can-do-today) and
[roadmap](docs/ROADMAP.md) first. Explain the workflow the change would support. Discuss broad features,
architecture changes or new dependencies before investing in a large implementation.

## Prepare a code contribution

Follow [Development](docs/DEVELOPMENT.md#current-environment-and-entry-points) to set up the pinned
.NET SDK, build and launch. Rider is convenient, but the command-line workflow does not require it.
The [documentation guide](docs/README.md#understand-the-design) explains where to start reading;
the [owner index](docs/INDEX.md) locates the relevant technical contract.

Keep a change focused on one problem. Follow the [coding guidelines](docs/CODING_GUIDELINES.md),
route musical edits through the existing project document and Undo history, and explain why any new
abstraction or dependency is needed. Check the affected behavior using the
[test guide](docs/TEST_EXECUTION.md#current-and-future-execution).
For interface changes, also try the real application at ordinary and reduced window sizes, in both
languages and themes. Audio claims need evidence from the audio path actually exercised.

In a pull request, explain the problem, the resulting behavior, what you verified and any remaining
limitations. Screenshots can support UI review; test counts alone do not explain whether an
interaction is useful. Keep generated logs, captures and local build output out of the source tree's
tracked content; see [verification output guidance](docs/DEVELOPMENT.md#verification-output-organization).

## Improve the documentation

Write for the page's intended reader in clear English. Keep descriptions of available features
separate from plans, update the technical source when its contract changes, and check whether the
README or first-use guide also needs a correction. Verify relative links and anchors.
The [documentation policy](docs/DOCUMENTATION_GOVERNANCE.md#audiences-and-document-purpose)
defines the relationship between reader guides and canonical engineering knowledge.

Seqvium uses the [Apache License 2.0](LICENSE). If you add third-party code, assets or sample material,
record its provenance and applicable terms in [THIRD_PARTY](docs/THIRD_PARTY.md).
