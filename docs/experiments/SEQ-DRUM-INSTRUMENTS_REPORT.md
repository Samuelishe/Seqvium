# Built-in drum instruments

Role: Evidence and limits for the bounded seven-instrument musical workflow.
Read when: Reproducing drum synthesis, editor or native composition checks.
Authoritative for: Observations, methods, failures and retained evidence.
Not authoritative for: Product contracts, subjective sound quality, release or platform acceptance.

## Result and scope

SEQ-DRUM-INSTRUMENTS extends the original kick with Snare, Closed/Open Hat, Clap, Tom and Rim.
Distinct synthesis and instrument-specific parameters follow the existing Source editor and shared
notes/history/graph boundaries. There are 21 factory starting sounds and explicit portable custom
presets. Linked hats choke across channels and audible song clips, with Closed priority at equal
onsets. Original kick parameters/PCM and existing project intent remain compatible.
Current audio, UX, format and design contracts remain in their respective owners.

The original DS-01–DS-03 previews were inspected from the private reference library. They informed
instrument anatomy, dominant envelopes, selected identity and economical controls; no reference
image, proprietary resource, external code, dependency or plugin host is distributed.

## Verification

Windows 11 x64, SDK 10.0.401/runtime 10.0.12, Release. Source/diff checks and full solution build
pass with zero warnings/errors (3.16 s final full-build CLI). Full MTP suite passes **1179/1179**,
zero skips (10.079 s CLI). The focused drum pass covers 106 cases, including actual signal changes
for each exposed sound parameter, presets at both host rates, pitch/time, extremes/cancellation,
Rack/Piano/history/media-free Save/Open, Pattern/Song WAV parity, cross-channel/cross-clip choke,
simultaneous priority, seek/partition parity and fractional loop/callback allocation.
After small editor note-label/vector-lifecycle follow-ups, the focused 183-case boundary pass also
passes (8.654 s CLI) with a zero-warning/error incremental build (1.45 s). This is not another full suite.

Reproduce using the manifests and commands under [test execution](../TEST_EXECUTION.md):

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore
tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.exe drum-kit <absolute-output> <delivery-directory> --timeout-seconds=300
tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.exe drum-kit-review <absolute-output> <saved-project> --timeout-seconds=180
```

`drum-kit` deliberately plays physical audio and authors music with foreground-guarded Windows
SendInput. It clicks the real Rack creation/voice chooser/presets/steps, draws pitched toms and
velocity in Piano Roll, and places clips in Playlist. It uses production Save/Open/export pickers.
Workspace setup uses the isolated developer host; musical content is not generated through internal
document APIs or direct JSON. Native input and physical output are distinct from rendered/routed checks.

`drum-kit-review` opens the saved project for inspection and retargets the editor without musical
edits. All seven voices pass EN/RU × Dark/Light ordinary and 300×210-DIP minimum pane checks,
including rendered field/unit alignment and local-scroll reachability. Native custom Rim preset
Save/Load restores parameters with exactly one history transaction, then passes Undo/Redo.
Actual screenshots in both themes were inspected. Shared controls are inherited, not recolored locally.

## Authored music

**Jade Circuit**: four source Patterns (A, B, Space, Turn), all seven drum families, 16 native Playlist
placements over 16 bars, descending pitched/velocity tom transition and an intentional final fill.
Every instrument was solo-auditioned; the reopened Song runs to Ended through physical output.
Save/Open preserves revision and starts silent. Native Song export is 48 kHz stereo PCM16, **1,561,200 frames / 32.525
s**, peak **0.342896**, energy 3871.267, **zero saturated samples**;
physical observation reaches six simultaneous voices.

Preserved outside rolling artifacts at
`C:/Users/filed/Music/Seqvium Examples/Jade Circuit-20261013-212407/`:
`Jade Circuit.seqvium` and `Jade Circuit.wav`. No media folder is required for these synthesized sounds.
Reusable source checks stay in `tools/Seqvium.VisualCheck`; transient PNG/JSON/fixtures stay ignored
under `.artifacts/drum-instruments/`. The successful native music run is
`music/20261013T212242068-6d3136f7d4504cbdb7b4f9168d025dad`; final review is
`review/20261013T213333629-d6333329c59d449d83e19ce7de9581cd`.

## Failures and repairs

- A one-slot fractional hat-loop regression exposed retirement after, rather than before, the next
  onset. Prepared choke ages now retire the voice at its exact boundary; the loop allocates no callback memory.
- GUI audition originally became disabled after its first output was opened. The editor now checks
  playing transport rather than output ownership, permitting repeated stopped audition.
- Preset observation initially replaced the ComboBox items on every refresh. Stable label lists
  retain popup/navigation state; applying snapshots remains atomic.
- Full tests exposed eager vector parsing in detached Node Editor construction. Preset vectors now
  initialize on visual attachment, matching the existing host pattern; the repaired full suite passes.
- Early native harness attempts lost foreground, used stale rendered hit regions or unreliable
  keyboard popup selection. They remain failures; rendered-scene waits, target occlusion checks and
  clicks on actual popup items supply the successful run. No forced foreground or synthetic-input pass is substituted.
- A preset-save assertion ran before the asynchronous picker/write continuation. Rider debugger
  observed a valid Rim file destination, 255 encoded bytes and successful FileStream writes at
  lines 518–525; the harness now waits for completion. Agent breakpoints were removed; user settings
  were untouched. This was a verification race, not a preset format or storage failure.

## Limits and checkpoint

Hits are deterministic mono, with one shared binary hat group. No stereo spread, modulation,
custom groups, per-channel Mixer, universal plugin host, user preset browser or live uncommitted
audio preview is added. Custom preset writes are ordinary explicit file writes, not crash-safe snapshots.
Subjective acoustic listening is unavailable to the agent; physical playback and PCM measurement
do not establish aesthetic sound quality, long pressure stability or Linux/macOS/high-DPI acceptance.
Earlier R4 pressure failures remain open.

One delegated PATCH advances **0.1.3.0000 → 0.1.4.0000** for a complete media-free percussion workflow;
BUILD resets to 0000. This is separate from roadmap/package stages and release acceptance.
Library, references and user projects are preserved. No staging, commit, push or tag was performed.
