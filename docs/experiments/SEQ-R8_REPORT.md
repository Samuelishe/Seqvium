# SEQ-R8 — Pattern resampling

Role: Reproducible implementation evidence and limits for bounded Create Sample from Object.
Date: 2026-10-16.
Base: clean `master`, `f19cbd5` (SEQ-CONTROL-INPUT).
Contracts: [Sample workflow](../SAMPLE_WORKFLOW.md#implemented-pattern-resampling-seq-r8),
[audio](../AUDIO_ENGINE.md#pattern-object-rendering-seq-r8),
[architecture](../ARCHITECTURE.md#pattern-resampling-ownership).

## Product and engineering result

Pattern navigator **Create sample** renders the card's explicit local Pattern attachment through
all supported instrument paths and its own graph. The stop point is local Output, before every
Mixer channel/bus/send/Master. It does not choose an arbitrary Song occurrence or capture live audio.
The source stays editable; the result is an independent managed ResourceId/sampler sound, one Undo
transaction, automatically revealed in Browser for preview/Rack/Playlist reuse.

Existing PatternWaveExport could not supply the semantic tap: GraphPreparation normally compiles
Mixer too. `PrepareObject` adds an explicit preparation boundary, keeping the same prepared graph,
OfflineSampler and media ownership. Finite object bounds retain musical rests and actual source
release/EOF; they avoid unnecessary global-release padding and loop-only capacity refusal. No second
audio engine, arbitrary-object framework, new assembly, codec or project schema was introduced.

Output is fixed 48-kHz mono/stereo PCM16 within 16 MiB including header (about 87.38 seconds stereo).
Overload/nonfinite/oversize results fail before acceptance; there is no silent clipping, normalization
or truncation. Required unsupported local dependencies block. Source/Gain/Mix have zero latency and
no stateful tail, making the bounded tail/entry-state policy sufficient without sidechain capture,
preroll or universal latency compensation. A 30-second cooperative deadline and packet/source checks
bound host work; one bounded decode/hash remains synchronous.

Desktop freezes before yielding and owns request progress/cancellation. Any canonical change,
deletion or Undo/Redo expires acceptance; selection never retargets it. WavImport places flushed,
validated immutable bytes before revalidation/one document Edit, cleans exclusively owned unaccepted
files, and retains accepted bytes for history. New/Open/Close cancel and join before teardown, and
the ordinary command gate prevents a result from attaching while lifecycle replacement is in flight.

## Deterministic and Release verification

SDK 10.0.401, xUnit v3 on Microsoft.Testing.Platform; single test thread, Release, no skips.

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*PatternResamplingTests" --filter-class "*ResamplingWorkflowTests" --max-threads 1
```

The final full solution build passes with **zero warnings/errors**. The final full suite passes
**1427/1427** tests, zero skips (**70.769 s** CLI summary), after the Browser request-coalescing
correction. The earlier full run also passed in 81.767 s. New focused tests pass **27/27**, zero skips
(**5.133 s**); the media/Browser/metadata/R8 subset passed **105/105** (**19.380 s**) before the final
correction, which is covered again by the final full suite.

The tests establish:

- Independent two-instrument arithmetic through separate branch Gain and aggregate Mix/Gain,
  stereo polarity and mono downmix, 44.1/48-kHz input conversion and a measured release envelope.
  Local output agrees with the authored oracle within one PCM16 quantization step.
- Every one of the seven synthesized drum families renders its late one-shot through actual EOF;
  numeric samples agree with direct DrumVoice output. The full Pattern retains its musical bounds.
- Downstream Master mute/EQ/compression and arranged repeats cannot affect object PCM; ordinary
  preparation still observes the Mixer. No unrelated Song audio enters the sample.
- A real frozen render survives concurrent local source edits and closing the original document;
  packet-loop cancellation returns no partial sample. Unknown sound/external performance state,
  missing media, output-size and overload blockers preserve canonical state.
- Managed acceptance preserves source Pattern/placement/graph, adds exactly one Undo entry and
  roundtrips exact accepted PCM through Undo/Redo. Existing Browser Rack/Playlist reuse shares the
  resource. Save/Save As, moving the project/sidecar and removing every original media copy leave
  reopen/decode/arrangement execution valid.
- A deliberately late completed worker cannot commit after Cancel, tempo edit, deletion, Undo/Redo,
  New/Open/Close. Lifecycle operations wait for it to finish. Failed durable placement creates no
  descriptor/history/partial file and preserves the previous canonical state.

Initial fixture failures were corrected: the fractional-quarter audio fixture is outside the step
editor's whole-bar rename/Gain assumptions, so its concurrent edit now changes tempo; WavImport
correctly reused an already owned media root, so the write-failure fixture now uses a fresh reopened
lifecycle with original roots readable and an obstructed new destination. These were test premises,
not changes to musical editor/media contracts.

## Real-window GUI, native input and physical output

```powershell
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll resampling "$PWD/.artifacts/r8/final-stable" native audio
```

The isolated production window uses an authored Kick/Clap/Open Hat Pattern with eight notes and
local Gain 0.3. Fixture creation uses workflow commands; it is not claimed as native composition.
The real context-menu action produces **119760 frames / 2.495 s / 48-kHz stereo**, including the late
Open Hat beyond the two-second bar. Accepted PCM equals the independently requested frozen local
PCM16 result exactly. The source stays unchanged; one Undo/Redo, Browser waveform/preview, + Rack,
+ Playlist, shared media and Save As/New/Open pass.

Actual menu and empty/populated window captures cover **RU/EN × Dark/Light**, **1400×900 and 800×600**.
No new editor, preferences, permanent tutorial or shared component style is added. Local Ableton
project-Browser reference imagery was inspected before UI editing; no third-party graphic was copied.
The final scenario begins with a nonempty Browser query and checks that the created sample clears
the filter, appears selected and stays selected after deferred notifications.

Both the earlier verified and final nonempty-filter runs separately record:

- **Native Windows input passes:** guarded real right-click on the Pattern card, popup action click
  and an additional independent managed sample acceptance. The existing guard remains authoritative.
- **Physical audio passes:** original three-instrument Pattern and accepted Browser sample through
  **Samsung USB C Earphones, 48 kHz stereo**, using production output. No device setting is changed.
- **Routed/deterministic evidence remains separate:** fixture authoring, rendered matrices, controlled
  cancellation/interleavings, PCM parity and storage tests are not relabeled as Windows input or DAC
  measurements. Physical delivery does not establish subjective acoustic approval.

## GUI failures found and corrected

The first GUI run reached creation/reveal/history but lost Browser selection before Rack reuse.
A selection probe observed `Selected=null` 500 ms after successful reveal, with an empty query.
Programmatic TextBox changes could deliver TextChanged after the synchronous refresh guard ended,
triggering a delayed redundant search. Browser now records the requested query and cancels obsolete
search timers when navigation already covers it.

The nonempty-filter follow-up exposed a second ordering case: polling compared displayed entries
while the reveal's list rebuild was still pending, scheduling a second rebuild after its selection.
Browser now compares the already requested resource IDs and uses the latest immutable workflow
observation rather than a queued older one. The final scenario asserts persistent selection after
the original asynchronous boundary, not just an immediate setter value.

The final run is retained at `.artifacts/r8/final-stable/20261016T061415143-9f71b4ca1ea94b1c925620a340b97c86/`.
`resampling.json` records passing routed/native/physical results; `reveal-stability.json` records the
still-selected managed ResourceId and cleared query. All 24 empty/menu/populated language/theme/size
captures complete; representative full and reduced captures were visually inspected. The final
nonempty-filter path, native input and physical output pass with current 0.1.9.0000 binaries.

Rider debugger discovery found no active session and run configurations without dynamic launch
overrides; the CLI-only GUI scenario could not receive its arguments through those configurations.
No debugger frame/value evidence is claimed. No run configuration or user breakpoint was changed.
The fallback uses reproducible GUI artifacts and explicit selection/failure observations.

## Retained evidence and limits

Reusable regression tests and VisualCheck route are tracked source. GUI captures, JSON observations,
isolated preferences, fixture projects and managed WAVs remain ignored under `.artifacts/r8/`, owned
by the existing rolling retention harness. These are developer evidence, not distributable sample
assets. Reports remain readable without retaining every binary capture. Root ProjectStats reports
are refreshed after final source/docs edits and inspected separately; structural metrics are not
executed test counts.

R8 does not implement replacement/disable-source, audible mix/Song/Arrangement-container capture,
stateful local FX, external detectors, VST, larger sampler media, library publication, recovery or
an arbitrary render service. Future local processors must first supply executable dependency,
latency, entry-state and finite tail semantics. Other object scopes require their own explicit
semantic boundary and acceptance evidence. None is introduced as a new Roadmap stage here.
Stress, acoustic/aesthetic owner acceptance, high/mixed DPI, accessibility and other-platform runtime
remain unestablished; existing R4 pressure gaps are not closed. One PATCH advances to **0.1.9.0000**;
BUILD resets, reader compatibility remains unchanged. No stage/commit/push/tag or release is performed.
