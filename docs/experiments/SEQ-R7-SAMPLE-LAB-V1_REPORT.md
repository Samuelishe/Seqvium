# SEQ-R7-SAMPLE-LAB-V1

Date: 2026-10-15. Baseline HEAD: `840a8ad`, clean working tree. Outcome: implemented Sample Lab V1
and the owner's subsequent first-party UI density cleanup. Product checkpoint: **0.1.8.0000**,
one PATCH from 0.1.7.0007, BUILD reset. This is a working bounded capability, not owner acoustic/
aesthetic acceptance, an external SDK or a release. No Git staging/history/publication was performed.

## Delivered creative workflow

The default-enabled, independently disableable **Percussion Lab** package generates Kick and Clap
through the real R6 trusted factory/contract/use lifecycle. Its new required exploration contract
prevents older hosts ignoring variation intent. Kick uses a swept pitched body, harmonics, noise
transient, envelope and saturation. Clap uses spaced band-shaped noise bursts, tail and resonant
body. There are no borrowed samples, Drum Voice calls or trivial renamed test-provider oscillators.

The musician chooses a family, generates, hears raw audio while stopped, explicitly establishes a
candidate reference and requests a related variation. Family-local exact locks constrain both Random
and Variation. The host retains twelve disposable WAV candidates with waveform metadata and separate
selection/reference/audition identities. Generation runs outside GUI/audio callbacks and is independent
of unrelated document generations. Late cancelled/superseded results are disposed. Provider leases
end after generation/validation, so idle history does not prevent disable; preserved audio remains
usable without executable code.

PCM Rack entry captures a precise source/binding. During Pattern/Song Play, a candidate temporarily
replaces that shared source through the existing local processing and downstream Mixer. This is a
prepared-plan substitution, not a second WAV played over the composition. Existing pitch/root/release
apply. Changed target voices retire at publication; future hits use the new source while unrelated
compatible voices, time and Mixer DSP continue. Original/Stop/Panic/hide/invalidation/close withdraw
the override and prepare actual current canonical sound. Audition creates no document edit or Undo.

Keep, new Rack use, new Playlist clip and explicitly shared PCM replacement use existing managed WAV
storage and one guarded document transaction each. Several candidates can be accepted independently;
history remains. Save As/Open/export require no generator. Accepted badges follow current resources,
including Undo/Redo. Temporary recipes/history/locks are not project fields or saved work.

## Deliberate V1 boundary

Context supports existing PCM Rack sounds in Pattern/Song, including their shared uses. It does not
detach one channel or one occurrence, replace Drum Voice/Audio Clip targets, analyze arbitrary WAV
references or host unknown processors. These would need distinct ownership/execution mechanisms;
they are not necessary to make the two actual families useful. Raw audition is separate and stopped
transport only. Candidate playback after substitution begins at the next note trigger, not by
restarting the target or unrelated music. No seamless-transition/acoustic-similarity guarantee follows.

Desktop requests 48-kHz mono candidates up to two seconds. The provider handles supported requested
rates, bounds frames/resources, peak-limits output and checks cancellation. History stores WAV bytes,
not twelve resident decoded PCM arrays or temporary files. Locks preserve recipe controls, not every
perceptual characteristic; drive/colour can alter apparent decay. Accepted audio is authoritative;
durable regeneration recipes, more families, Personal Library and R8 resampling remain follow-ups.
Current contracts are in [Sample workflow](../SAMPLE_WORKFLOW.md#implemented-sample-lab-v1),
[Extensions](../EXTENSIONS.md#sample-lab-generator-package) and [Audio](../AUDIO_ENGINE.md#sample-lab-contextual-audition).

## UI review and cleanup

The owner explicitly expanded scope from Sample Lab to recent Browser/Extensions/Rack work and then
the whole first-party UI. Actual local screenshots from the private reference catalog were inspected,
including real Browser, sampler and percussion interfaces. They informed compact hierarchy and
disclosure; no proprietary screenshot/graphic was copied into the product or this repository.

Sample Lab now uses short candidate names, waveform-led listening, concise commands/icons, a separate
reference marker, disclosed Shape controls and pinned acceptance. UUIDs, seed/recipe dumps and
permanent tutorial/success paragraphs are absent from normal work. Browser hides empty preview and
normal status chatter; optional metadata stays on inspection. Extensions shows availability rather
than identity/category/contract/durability paragraphs. Rack/Playlist/Mixer empty states are concise.
Playlist's section commands use the existing recognizable vector icons with short action tooltips.
Mixer no longer prints idle peak/reduction counters on every strip; real meters, exact controls and
overload remain. Redundant Gain percent/linear/dB presentation, Source technical readouts, Piano MIDI
number duplication and permanent source/note/tempo/property explanations are reduced or disclosed.
Musical tempo/pitch/time/levels and actionable shared-edit/error scope remain available.

All ten retained panes were composed with a populated Neon Orchard document across RU/EN × Dark/Light,
plus reduced Lab. Source review also covered dialogs/properties and instrument disclosures. This is
not a claim to have exercised every gesture, processor setting, modal or accessibility mode.
`AGENTS.md` and [UI design](../UI_DESIGN.md#required-reference-and-information-density-gate) now require
actual relevant reference inspection before UI work, web reference inspection if local material is
missing, short actions and a mandatory text/numeric-density review. No shared palette/template changed.

## Verification

Commands, using installed SDK 10.0.401, MTP and xUnit v3:

```powershell
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sample-lab "$PWD/.artifacts/sample-lab/final-audio" audio
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll sample-lab "$PWD/.artifacts/sample-lab/final-routed"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll extensions "$PWD/.artifacts/sample-lab/extensions-final"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll ui-density "$PWD/.artifacts/sample-lab/ui-final" "$PWD/examples/Neon Orchard/Neon Orchard.seqvium"
```

Full Release solution build: **zero warnings/errors**, 9.60 s. Full suite: **1341/1341 pass**, zero
skips, one thread; **62.326 s CLI summary** (62.075 s module). Earlier focused runs cover the new
classes plus extension/media/workspace cases. After final UI and availability changes, the solution
build remains warning-free (7.71 s) and **189/189 focused cases** pass (5.665 s CLI), including the
extension host, Browser, Lab, processed audition, workspace and song structure. The original R6
real-window Extensions route also passes with both bundled packages present. The first full attempt exposed five stale nine-pane
assertions; expectations now cover ten panes and the full run passes. Early GUI fixtures opened Lab
before the deferred startup empty-layout action and routed hidden controls; the harness now waits for
settled layout and explicitly exposes the real pane before action checks. Those failed attempts are
not visual/input successes. Extending the fixture to cross-pane enable/disable initially searched
inside Lab for Browser controls; it now searches the actual main-window tree. Those fixture failures
do not establish input success. Final checks verify that Browser disable/re-enable updates Lab's
generation actions immediately while preserving candidate audio and acceptance.

| Evidence | Actual observation |
| --- | --- |
| Numerical synthesis | Four requested rates for both families; finite peak-bounded PCM with a real transient and declining tail. Fixed-fixture early energy exceeds terminal energy by over 20×. Kick/Clap high-frequency-difference ratio exceeds 5× for the stated fixture. Random and related PCM differ measurably; related recipes stay in their bounded neighborhood and locks remain exact. These are fixture assertions, not a universal perceptual metric. |
| History/lifecycle | Selection never changes reference; family clears incompatible locks/reference; twelve entries evict/dispose; cancellation/supersession/New dispose uncooperative late output. Active work blocks disable. Unrelated rename does not cancel standalone synthesis. |
| Processed deterministic PCM | Temporary Pattern/Song audition exactly matches explicit shared acceptance through local Gain and real Mixer level/balance; raw PCM differs. Unrelated sustained PCM voice and transport position/epoch survive a candidate publication. |
| Managed music | Independent candidates produce one Undo each; target selection/unrelated rename do not redirect shared acceptance; changed/deleted source stales context. Undo/Redo, Rack/Playlist Play, disabled-generator Save As/Open and nonzero unclipped canonical export pass. |
| Real GUI, routed | Production pane opens/activates its package without Browser; random/reference/decay lock/variation/history and independent acceptance pass. Four language/theme combinations plus reduced layout pass; actual field/label center differences are 0 DIP. UI-density captures cover all ten panes in populated groups. |
| Physical output | Current 0.1.8.0000 actual-window `audio` sequence passes solo, Pattern/context/original and Stop through **Samsung USB C Earphones, 48000 Hz, stereo**. The earlier uncompressed panel also passed the same route. Device delivery is distinct from deterministic PCM comparison. |
| Native Windows input | One guarded action-button attempt was refused before activation because another window covered the target. No forced foreground/click workaround or repeated attempt. Checkbox/list selection remain fixture setup even in native mode. |
| Acoustic/aesthetic acceptance | Not claimed. Waveforms, numeric signal checks and successful physical delivery do not establish how musically attractive the sounds are to the owner. |

No new stress/pressure, callback allocation guarantee, mixed/high DPI, accessibility or Linux/macOS
acceptance is inferred. Existing pressure/decoder debt remains with its owners. The official demo is
opened without writing its source and is musically unchanged.

## Evidence and repository boundary

Reusable sources, manifests, tests and VisualCheck routes are product/verification material and stay
in Git. Local GUI captures, generated fixture projects/media/WAVs and JSON are ignored under
`.artifacts/sample-lab/`, retained only for review under the existing rolling evidence policy. The
audio, routed, UI-density and native-refusal observations are separate; no raw capture is a public asset.
No task-owned disposable source files, recordings or arbitrary temporary WAVs remain in the tree.
All staged/unstaged/untracked task files are reviewed at handoff; the initial worktree was clean.
