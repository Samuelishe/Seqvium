# SEQ-R11-DELAY-F1 — musical Delay / stateful Mixer FX

Date: 2026-10-16. Base: published `bd808684`, 0.1.10.0000; evidence is the uncommitted working tree.
Audience: contributors assessing implementation, reproduction and the limits of this checkpoint.
Contracts remain in [Audio](../AUDIO_ENGINE.md#musical-delay-v1-seq-r11-delay-f1),
[UX](../UX_CONTRACT.md#implemented-mixer-workflow) and [format](../PROJECT_FORMAT.md#mixer-delay-reader-17).

## Outcome and scope

A musician can add Delay to an existing Mixer channel, bus or Master, change Time/Feedback/Mix
while playing, bypass it, Undo/Redo, save/reopen and export a finite processed WAV. The Inspector
uses compact existing tabs/parameter fields and localized Enabled/ms/percent controls. No separate
player, renderer, history, transport, resource, FX rack or local graph node was introduced.

Time is 10–1000 ms, Feedback 0–75%, Mix 0–100%; defaults are disabled, 250 ms, 35%, 25%.
These limits cover short rhythmic echoes with stable feedback and a bounded resident line. Stereo
channels repeat independently; mono repeats equally. Delay follows Compressor before balance,
fader and pre-fader sends. Existing source contribution transfer, aggregate strip processing,
Mute/Solo/sends/EQ/compression/levels/protection/meters remain authoritative.

## Implementation and ownership

[MixerDelayDsp](../../src/Seqvium.Core/Audio/MixerDelayDsp.cs) writes input plus delayed feedback
and mixes dry/wet. Preparation derives integer host-frame taps, double coefficients and finite tail
allowances. Five-ms exponential Feedback/Mix smoothing also handles bypass; Time crossfades fixed
taps over 20 ms, coalescing a later target after the active transition. Bypass clears history after
wet settles below 1e-8. No signal-dependent silence detector, ping-pong, sync, modulation or automation
is claimed. Extreme edits still need acoustic assessment.

Each strip reserves two float rings and Int64 generation tags outside rendering, including bypassed
strips. At 48 kHz this is 768016 bytes per strip; 65 strips use 49,921,040 bytes, inside a separate
64 MiB per-state allowance. Existing 16 MiB scratch and PCM budgets remain separate. Existing four
reserved publication slots include Delay payload in retained/reserved accounting. This is not an
OS working-set cap. Reset increments a generation instead of clearing a large ring. Continuation
transfers exclusive line ownership by stable strip ID; retired state receives the new empty line.
No ring copy, allocation, I/O, mutable document traversal or blocking lock is added to the callback.

Tail allowance counts periods through -80 dB relative feedback decay, an extra period and 50 ms
settling; zero feedback needs one period plus settling. Maximum per strip is 34.05 s. Output/send
DAG preparation sums the longest chain and refuses more than 120 s extension. No-input countdowns
also expire each line with a final five-ms fade. The conservative allowance can include silence.
Natural full Pattern/Song execution appends the allowance; compatible live updates can extend an
active deadline without shortening earlier permitted completion. Source NoteOffs stay at their
original boundaries, including a last note whose gate equals the old finite end.

Ordinary Pattern looping streams one line across iterations. Stop/Panic, Restart, cold Seek,
explicit Song range wrap and natural retirement reset it. Seek reconstructs sources without FX
preroll. Existing joined project replacement owns release. Explicit Pattern stop stays hard; no
range-export UI was added. R8 PrepareObject remains before Mixer and excludes Delay. General
stateful graph processors, latency compensation, plugin-tail reporting and transport acoustic
settling remain open under Q-057.

Non-default Delay intent, including custom bypassed parameters, requires reader 1.7. Exact default
Delay is omitted, retaining old JSON shape/gates. Unknown nested data round-trips but blocks
execution/overwrite; null/out-of-range and underdeclared intent refuse. Reader-1.6 refusal is a frozen
envelope predicate test, not an executable historical reader. Existing canonical document/history,
Save As and managed media own persistence; DSP history is never saved.

## Deterministic verification

Windows 11 x64, .NET SDK 10.0.401 / runtime 10.0.12, Release, nullable/warnings-as-errors;
xUnit v3 on Microsoft.Testing.Platform, one test thread. No ordinary test opens a physical device.
Fixtures use authored impulse PCM and built-in Kick synthesis, without private music/media.

`MixerDelayTests` adds 21 cases. Independent impulse arithmetic checks first-echo interval, repeated
feedback decay, zero feedback, dry-only and wet-only, mono/stereo and 44.1/48-kHz host rates through
production Mixer. Partition changes retain exact float PCM. Channel/bus/Master cases verify aggregate
rather than per-clip processing, two simultaneous Audio Clips, pre/post sends, Mute and Solo.
Production Song PCM16 export retains late audible echoes and agrees within one quantization step;
realtime and offline float PCM agree exactly in the same rate/smoothing context.

Transport cases verify hard stop, cold Seek, range wraps, Panic/restart, ordinary Pattern loop echoes,
before-Mixer object output, live enable/Time/Feedback/Mix/bypass, no resurrected bypass history,
unchanged epoch/voices and no repeated note on a live deadline extension. Topology publication keeps
an active echo. The maximum bank executes a real allocation measurement against its reserved payload;
serial-tail overload refuses preparation. History/portable media/reader/unknown-state checks are atomic.

Warm callback checks observe zero thread allocation across repeated range wraps and live publication.
Setup, arrays, preparation, assertions and explicit warm-up are outside measurement. An unprimed live
update observation showed 1440 bytes; another unprimed seek/range observation showed 3528 bytes. The
checks were corrected to prime the exercised branches before measuring, matching the existing warm
verification policy. These observations do not prove first-entry/JIT or stressed callbacks allocate
nothing. A separate existing Arrangement allocation check showed a 296-byte failure during a full run
concurrent with GUI/device verification; its assertions were preserved. No allocation or deadline
counter is used to close the previous R4-F2 service-wake/starvation failures.

Focused Mixer/Arrangement/AudioClip/R8 checks passed 112/112 (9.182 s CLI). Final source-gate/maximum
bank refinement passed 70/70 MixerDelay/OfflineSampler cases (2.767 s); the final 21-case Delay-only
pass took 2.659 s. Final full Release **1471/1471 pass**, zero skips, one thread, **78.127 s CLI**;
solution build has zero warnings/errors (6.60 s after the boundary refinement). That final full run
was executed separately from GUI/device work. Existing R4-F2 pressure findings remain open.

Reproduction:

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore --max-threads 1
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*MixerDelayTests" --max-threads 1
```

Earlier full runs exposed obsolete reader-1.6 expectations, corrected to the new current reader and
next unsupported minor. The original hard-stop event table regression caught unnecessary NoteOff
retention on an explicit stop; preservation is now restricted to Mixer boundaries that can extend.
One existing retention age test initially failed and passed unchanged in the focused rerun; no
cleanup assertion or unrelated production behavior was weakened.

## Real window, native input and physical output

```text
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll delay .artifacts/delay-f1/final native audio
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll catalog .artifacts/delay-f1/catalog
```

Final retention-managed GUI run: `.artifacts/delay-f1/final/20261016T080036972-7eea9f573d9c48a5bfd599b0a43534da`.
An authored quiet two-hit Kick Pattern uses production Mixer controls, then becomes a finite Song.
Routed Enabled/Time/Feedback/Mix produce four real edits, Undo/Redo restores values, invalid feedback
retains state, Save/New/Open preserves intent and the full WAV has 188400 frames (3.925 s), including
nonzero echoes after the two-second source range. Default output delivery never changes system volume.

Actual screenshots cover RU/EN × Dark/Light, populated/empty Mixer at 1400×900 and 800×600. All 24
parameter presenter/label alignments are 0 DIP. Compact tabs fit, reduced bank/Inspector scrolling
remains accessible, and empty state has only the existing Create Mixer action. The shared catalog
passes. Private local Mixer reference images were inspected as study material; none is distributed.

The first GUI attempt could not find a newly rebuilt field; deferred materialization was inferred
from the timing difference. Rider debugger inspection
confirmed SelectedIndex 3, visible Inspector and the real Time field; agent breakpoints were removed
and the session stopped, preserving existing user exception breakpoints. The harness now waits for
field visibility after Inspector rebuild. Earlier empty captures showed startup Rack instead of the
requested Mixer; the final harness explicitly opens/hides panes before each empty capture.

Final native Windows input passes exact Time, Bypass and Enabled on actual controls. Setup/channel/tab
selection remain routed. Two earlier attempts refused input because another window covered the target;
they are not native passes. An earlier physical harness check incorrectly required a mode-changing
command to return true when Song was already selected; it was corrected before playback acceptance.

Final physical output passes on Samsung USB C Earphones, 48 kHz stereo: natural Ended at frame 188400,
zero voices, unchanged epoch while Mix changes live, and no reported device fault. The same bounded
physical scenario also passed in the prior review run. This proves declared device delivery/status,
not electrical loopback, subjective listening, acoustic quality or stress. Deterministic float parity
is device-independent; it is not relabeled as physical capture parity.

## Classification and remaining limits

One PATCH: **0.1.10.0000 → 0.1.11.0000**, BUILD resets to 0000, for the first complete musical Delay
workflow with canonical persistence, state lifetime and finite export. No MAJOR/MINOR, release, Git
stage/commit/push/tag or history/index operation is authorized or performed. Q-057 is narrowed only
for this built-in Mixer effect. Reverb/racks/automation/sync/ping-pong, general FX preroll/tail APIs,
pressure/maximum-channel device stress, other platforms/DPI and owner aesthetic/acoustic acceptance
remain separate. Final Git status and regenerated ignored ProjectStats are reported in the handoff.
