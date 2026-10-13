# First Drum Voice — implementation and verification

Role: Evidence for the owner-authorized bounded built-in electronic kick and its musical editor.
Read when: Inspecting this checkpoint's implementation choices, actual verification and retained example.
Authoritative for: Observations, commands, failures, timing and evidence limits.
Not authoritative for: Product/audio/UI contracts, extension ABI, roadmap acceptance or release readiness.

## Outcome and boundary

SEQ-FIRST-DRUM implements one useful synthesized kick, triggered by existing Rack/Piano notes and
processed through existing Source/Gain/Mix/Output. Tune, Decay, Tone, Noise and pitch/amplitude shapes
affect real PCM. Shared sound parameters use ordinary history, Save/Open and Pattern/Song export.
No sample files, dependency, processor registry, package system, second sequencer or borrowed asset
was added. [Audio](../AUDIO_ENGINE.md#built-in-drum-voice),
[architecture](../ARCHITECTURE.md#built-in-instrument-ownership) and
[UX](../UX_CONTRACT.md#drum-voice-sound-design) own the implemented contracts.

The chosen minimum extension is a built-in versioned sound plus bounded off-thread synthesis into
temporary host-rate PCM. It reuses the proven immutable source leases and scheduler instead of
introducing stateful callback synthesis/seek machinery before this first instrument needs it.
The sound's identity/parameters remain durable; these signals are regenerable and never accepted
project samples. R6/R7 optional capability and removable creative-generator direction remains open.

## Reference and visual work

DS-01, DS-02, DS-03 originals and their catalog/brief/musical-editor notes were read from the owner's
local Design References before implementation. Selected lessons are oscillator/noise anatomy,
one selected sound editor and dominant pitch/time context. Static references were not treated as
evidence of input gestures or DSP. Original host controls/drawing implement the result; reference
images and third-party graphics remain outside Git.

Owner review of initial actual RU Light / EN Dark / compact images favored the instrument direction
and requested less duplicated numerical feedback, stronger Light expression, integrated composition,
compact reachability and editable curves. The final composition uses one chassis, values once below
dials (distinct Tune Hz retained), adaptive note/Hz/ms curves, palette-derived Light wells/scale ink
and a denser actual waveform. Dark materials remain. Both envelope modes have directly draggable
points plus exact controls; no decorative measurement or inactive future controls are present.

## Deterministic verification

Environment: Windows 11 x64, .NET SDK 10.0.401/runtime 10.0.12, repository-locked Avalonia/xUnit v3
MTP. Restore inputs/assets were unchanged, so valid locked assets were reused. Isolated output avoids
replacing a running user's binaries. Commands from the repository root:

```text
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore -p:OutputPath=C:/Users/filed/RiderProjects/Seqvium/.artifacts/first-drum/release/ -p:UseSharedCompilation=false
dotnet .artifacts/first-drum/release/Seqvium.Tests.dll --filter-class "*DrumVoiceTests" --filter-class "*GraphDiagnosticTests" --filter-class "*NodeEditorTests" --filter-class "*DesignSystemTests" --filter-class "*LoopPlaybackTests" --filter-class "*ArrangementTests" --filter-class "*StepSequencerTests"
dotnet build Seqvium.sln -c Release --no-restore -p:OutputPath=C:/Users/filed/RiderProjects/Seqvium/.artifacts/first-drum/release/ -p:UseSharedCompilation=false
dotnet .artifacts/first-drum/release/Seqvium.Tests.dll --report-xunit-trx --results-directory C:/Users/filed/RiderProjects/Seqvium/.artifacts/first-drum/tests
```

L0/L1/L2 pass: 225 focused cases, zero failures/skips, 2.312 s runner summary. The 19 new instrument
cases protect parameter effects, finite endpoints, host-rate pitch with unchanged envelope time,
velocity, retrigger/partition parity, zero consumer allocation, one-shot/loop tails, overload refusal,
live publication/Stop, seek after the original gate, independent overlapping Song contributions,
hybrid WAV/drum mixing, exact history/target ABA and media-free Save/Open/export. They use generated
fixtures rather than the owner's private samples.

Early L3 is justified by shared scheduler/seek/continuation changes, beyond a cosmetic editor task.
Final full Release solution build passes with zero warnings/errors in 6.72 s; all **1092/1092** tests
pass, no skips, 8.572 s runner summary (9.112 s CLI call). Ordinary Desktop outputs were synchronized
in a separate zero-warning/error 4.47 s build, with no user process replaced. These sufficient outputs were reused after
documentation changes. Product metadata checks validate **0.1.3.0000**, one delegated PATCH from
0.1.2.0000 with BUILD reset, for the completed standalone instrument workflow. Reader/writer markers,
project shape, dependencies and MAJOR/MINOR are unchanged. Cadence resets to zero completed tasks.

## Actual GUI and musical evidence

```text
dotnet .artifacts/first-drum/release/Seqvium.VisualCheck.dll drum C:/Users/filed/RiderProjects/Seqvium/.artifacts/first-drum/gui "C:/Users/filed/Documents/Seqvium Resources/Examples/First Drum Voice"
dotnet .artifacts/first-drum/release/Seqvium.VisualCheck.dll catalog C:/Users/filed/RiderProjects/Seqvium/.artifacts/first-drum/catalog
dotnet .artifacts/first-drum/release/Seqvium.VisualCheck.dll gestures C:/Users/filed/RiderProjects/Seqvium/.artifacts/first-drum/gestures
```

The successful final `drum` run is
`.artifacts/first-drum/gui/20261013T205329992-646b7939b180499c9b44852db64f03d0`, 16.51 s between run markers.
It opens a real production window at 1240×900, creates a kick through Rack controls, writes seven
hits (steps 0/4/7/8/12/14/15), sets parameters through real text fields and a captured fine dial,
and switches both curve modes. Routed controls/automation are explicitly distinct from native input.
Foreground-guarded Windows input passes physical dial capture and pitch/amplitude point drags, with
exact one-Undo restoration. RU/EN × Dark/Light 740×740 and actual 300×210 minimum pane renders check actual text/unit
centers (zero-DIP differences) and all nine fields reachable by local scrolling. Both themes and
compact-depth screenshots were visually inspected. Minimum review uses the 640×480 window, then
waits for native resizing to settle before restoring the ordinary instrument geometry. The entire
instrument, including audition, scrolls so fixed controls cannot consume the minimum working area.
The shared catalog and field/gesture route pass
separately; their artifacts remain under the corresponding ignored task directories.

The same window verifies history, Save/Open with stable canonical revision and silent fresh audio,
quiet solo audition, physical default-output Pattern looping, live Tone edits without epoch restart,
finite two-repeat Song and both GUI-workflow exports. Song observed four simultaneous voices.
The synthesized Song WAV is **215760 frames, 48 kHz stereo**, peak **0.3016052246**, energy **1248.5972** on the left
channel, **zero saturated samples**; the final kick tail extends past the
four-second musical clip end. Export opens no additional output device. No physical pressure or
subjectively judged acoustic quality is claimed by these short output/PCM observations.

## Failures and corrections

Initial compilation found a missing Core import, then new-test cancellation analyzer omissions;
these were corrected without suppression. The first focused run failed nine zero-first-sample
assertions because oversampling included a nonzero substep at frame zero, and the loop-tail oracle
included that new hit. Synthesis now explicitly starts/ends at zero; focused/full tests pass.
Initial GUI review exposed needless scrolling hiding the primary controls; spacing, sizing and
the unified chassis were corrected and re-rendered. Two early GUI checks failed because the harness
compared pre-reopen snapshot object identity and assumed Pattern mode after reopening a Song;
it now captures current audition authority and explicitly chooses the intended transport mode.
The actual 300×210 review then found an unreachable field because fixed audition rows exhausted
the working height. Moving audition into the common scrollable chassis repairs it without shrinking
inputs; all nine fields pass containment checks. The harness also waits for restored native window
geometry instead of capturing a pane still clamped to the previous small client. These failures were
not audio/visual successes. Subsequent complete musical/native/visual routes pass.

## Retention and remaining scope

The reusable owner example is preserved outside rolling evidence:
`C:/Users/filed/Documents/Seqvium Resources/Examples/First Drum Voice/Jade Pulse-20261013-205346/`.
It contains `Jade Pulse.seqvium` plus Pattern/Song WAVs, with no sample dependency. Earlier successful
example editions remain preserved. Isolated binaries, TRX, screenshots/JSON and failed run diagnostics
are ignored under `.artifacts/first-drum/` for review; normal tool retention can rotate disposable
captures, not the owner's projects/media or private references. Permanent synthesis, tests and
reusable GUI/native probes are source deliverables. Git staging/history were not modified.

Other drum families, preset browsing/storage, layered clicks, choke groups, advanced modulation,
live uncommitted audible preview and public instrument/package APIs remain future work. Compact
scrolling is verified at the stated size; high/mixed DPI, other platforms, native file pickers,
long stress/pressure and subjective listening remain separate evidence gaps. This checkpoint does
not close inherited R4-F2 pressure acceptance or declare a production release.
