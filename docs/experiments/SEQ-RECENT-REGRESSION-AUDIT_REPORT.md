# SEQ-RECENT-REGRESSION-AUDIT

Date: 2026-10-16. Audience: contributors reviewing reliability across the five recent checkpoints.
Range: `ecc3396..b862b3c9`, initially clean working tree, alpha 0.1.11.0000.
Outcome: three local corrections, fifteen added regression cases, BUILD **0.1.11.0001**.
No index, commit, branch, remote or history mutation is performed.

## Scope and method

Read AGENTS/current state and the audio, architecture, sample, format, settings, UX, workspace,
coding, verification, versioning and documentation owners. Inspected actual changes in:

| Commit | Checkpoint | Principal boundaries inspected |
| --- | --- | --- |
| `11f0587` | Settings F1 | Binding resolution/capture/conflict/storage, text focus, modal lifetime and Browser auto preview |
| `f19cbd5` | CONTROL-INPUT | Drum amplitude capture, angular/captured controls, latest audition intent, Stop and joined lifetime |
| `dc003ff` | R8 | Frozen local render, source tails, cancellation/generation/acceptance, managed media, Browser reveal |
| `bd80868` | R10 Stretch F1 | WSOLA preparation, source/derived identities, split/envelope/repeat clocks, live continuation and reader 1.6 |
| `b862b3c9` | R11 Delay F1 | Stateful ownership/transfer, finite completion/note gates, routing/sends, realtime memory, reader 1.7 |

Read the five stage reports as evidence descriptions, then independently ran new failure cases,
combined numerical/media workflows, archived-reader executables, the current full suite and real
production-window scenarios. Successful earlier reports were not treated as defect exclusion.
No external-repository research or architecture redesign was needed.

## Confirmed defects and corrections

All three findings are **P2 / medium**: bounded reproducible correctness failures, without an
established process crash, corrupted saved audio or general data loss.

**A1 — R11 live tail refusal escapes an accepted document notification.**
Three serial maximum Delays allow 102.15 s; enabling a fourth requests 136.2 s and exceeds the
120-second gate. `GraphExecutionCoordinator.Progress` called `PublishMixerUpdate` synchronously
without the `GraphPreparationException` handling present in worker preparation. The new regression
failed through `MixerPreparation.Compile → PublishMixerUpdate → Progress → Refresh → ProjectDocument.Edit`.
The transaction/history was already accepted, but the caller received an exception and the
coordinator did not expose the invalid-canonical/last-valid state. Do not label this an atomic
edit refusal or an observed desktop crash.

The fast path now records the preparation blocker and retains the identified last-valid execution.
The regression checks accepted history, explicit budget reason, continuing revision, Undo repair,
unchanged epoch and no extra execution allocation. This preserves the existing finite-budget policy.

**A2 — R11 completion metadata omitted from Sample Lab plan substitution.**
`SampleAuditionSource.Apply` cloned the prepared plan but omitted the newly added `SourceEndFrame`
and `AllowMixerTail`. Its constructor consequently treated the already extended end as source EOF.
An unchanged live Mixer update extended the test Pattern from **170400 to 244800 frames** instead
of keeping its deadline. The same omission could discard an explicit hard-stop policy. This is a
cross-stage R11/Sample Lab integration regression, outside the earlier isolated Delay scenarios.

The clone now preserves both properties. Pattern, Song and explicit hard-stop cases check the
execution deadline after a real update; hard boundaries receive no extension. No PCM algorithm,
audition selection rule, persistent representation or musical note gate changes.

**A3 — inherited shared-resource metadata bypass exposed by Stretch.**
`ArrangementPreparation` validated decoded rate/frame/channel metadata only on the first resource
lease. Another audible SoundDefinition referring to the same resource skipped validation. A
48000-Hz resource advertised as 44100 Hz by the second definition was accepted, including with
Stretch=150%; scheduling then used the wrong clock/step instead of refusing contradictory media.
The new test initially failed because no exception was thrown. The same conditional check is
present in `ecc3396`; this is **pre-existing**, not attributed to one of the five commits.

Every audible definition is now validated against the shared PCM before scheduling/stretching.
Four cases cover conflicting rate at unity/non-unity, frames and channel count. They receive
`graph.prepare-media`. Valid resource sharing and PCM reuse are unchanged. No new reader gate,
canonical mutation, source rewrite or lossy migration is introduced.

## Measured compromises and remaining risks

**Disabled Delay storage is real, bounded and expensive.** At 48 kHz each strip allocates
`48001 * (4 + 4 + 8) = 768016` bytes of rings/tags, even when bypassed. A 64-channel bank plus
Master uses **49,921,040 bytes / 47.61 MiB** per execution. The added backpressure test admits
three executions (active/pending/retired), measures each synchronous publication allocating at
least that payload, and retains **149,763,120 bytes / 142.83 MiB** of line payload alone. A fourth
prepared candidate receives Capacity without adding another execution. PCM/scratch/objects and
GC-retained dead states add cost; four-slot reservation accounting is not measured working set.
Old projects with a Mixer pay this cost despite omitted/default Delay; projects without a Mixer
do not. Disabling a strip does not reclaim the reservation.

**Conservative tails can be pure silence.** New independent cases compare a dry source with an
unused return, a muted return and an enabled Mix=0 strip. Each maximum Delay appends exactly
**34.05 seconds of zero PCM**, preserving the preceding dry PCM. Four such serial allowances
also hit the 120-second refusal independently of useful source reachability. This agrees with
the explicitly accepted R11 conservative contract; it is a meaningful efficiency/admission
compromise rather than an unreported echo-loss defect.

Both follow-ups are registered together in [TECH_DEBT](../TECH_DEBT.md#bypassed-delay-storage-and-conservative-tail-reachability),
with bounded exit conditions for off-callback storage admission and safe audible-tail analysis.
No lazy allocation on the callback, silence trimming or global quota was introduced here.

R4-F2 pressure remains open under [Q-047](../KNOWN_PROBLEMS.md#musical-ownership-execution-and-edit-questions).
Large LOH reservations plausibly increase preparation/GC pressure, but this audit establishes
neither a starvation regression nor its attribution to Delay. Earlier unprimed 1440/3528-byte
observations, intermittent 296/4136/7576-byte assertions, scheduler evidence and the earlier physical
DeviceFault remain separately classified in their original reports/current measurement-gap owner.
No assertion was weakened and no earlier failure is declared fixed by this green run.

Cold seek/range-wrap FX reset remains the existing [compromise](../TECH_DEBT.md#mixer-cold-seek-and-range-wrap).
General stateful local processors, latency compensation, acoustic stretch/transition quality,
maximum-load deadline qualification and other platforms remain outside this audit's acceptance.

## Interactions tested without a reproduced defect

- Six sequential topology changes add/remove neighbouring strips, transfer the active Delay line
  and retire each predecessor. Entire resulting float PCM equals uninterrupted rendering; epoch
  stays fixed, each retirement leaves one live state and final disposal releases all states.
  Exclusive-ownership failure was not reproduced; this is not a race/sanitizer proof.
- R8 managed acceptance → Browser Audio Clip reuse → 1.33337 stretch with fades/repeats → channel
  and Master Delay agrees exactly between realtime/offline at **44100 and 48000 Hz**. Production
  48-kHz PCM16 export stays within one quantization step. Undo/Redo retains independent intent;
  two Saves, New, removal of original WAV copies and portable Open retain PCM and reader 1.7.
- R8 output remains byte-identical before and after configuring the downstream Mixer/Delay.
  Existing frozen/cancel/late/ABA/New/Open/Close tests pass; no new render owner or live-state tap.
- Current finite gate, loop, Stop/Panic/Seek, sends/Mute/Solo, live edits, unknown nested state and
  derived-source identity tests remain green. No claim of seamless FX preroll follows.

### Actual historical reader matrix

Built BCL-only Core readers from **`ecc3396` (1.5)** and **`bd80868` (1.6)** using `git archive`
into ignored task directories; the live checkout/index was not switched. The current 1.7 reader
uses this task's Core. Current writer fixtures contain audio/sections (1.5), explicit stretch (1.6)
and non-default Delay (1.7), plus an optional unknown clip field and, for 1.7, unknown nested Delay.

| Executed reader | 1.5 fixture | 1.6 fixture | 1.7 fixture |
| --- | --- | --- | --- |
| 1.5 | accepts, preserves unknown clip field | refuses version envelope | refuses version envelope |
| 1.6 | accepts, preserves unknown clip field | accepts, preserves unknown clip field | refuses version envelope |
| 1.7 | accepts, preserves unknown clip field | accepts, preserves unknown clip field | accepts, preserves both unknown fields |

Accepted fixtures were re-encoded and inspected. This is executable parsing/preservation evidence,
not historical Desktop playback; managed-media/numerical parity uses current production tests above.

## Executed verification and limits

- Windows 11 x64, SDK **10.0.401**, runtime **10.0.12**, xUnit v3/MTP, Release, one test thread.
- Full solution build: **zero warnings/errors**, **22.80 s**. Full suite: **1486/1486**, zero skips,
  **72.389 s CLI**. Fifteen new theory-expanded cases supplement the previous 1471-case baseline.
- Focused four-class correction/integration run: **70/70**, **23.093 s**; the subsequently added
  maximum-bank backpressure case passes separately, then participates in the full 1486-case run.
- Actual production-window `settings`, `resampling`, `stretch` and `delay` scenarios pass. Settings
  checks remap/conflict/old-key removal, text-local history, Browser preference and file reload;
  R8 checks stable reveal/selection, reuse/history/portable Open; Stretch checks fields/invalid
  input/both trims/history/Open and 192000-frame export; Delay checks live fields/history/Open and
  188400-frame late-echo export. RU/EN × Dark/Light ordinary/reduced matrices are generated.
  Spot-reviewed actual reduced Settings, populated Browser, Stretch properties and Mixer renders.
- GUI actions above are **routed Avalonia input**, not Windows SendInput. Native input, screen
  capture, numerical PCM, physical delivery and subjective listening are separate evidence tiers.
  No subjective listening or R4 stress certification is claimed.
- The separate routed `control-input` scenario passes **81 checks / 124 traces** across all seven
  drums, Gain and Mixer. Its rapid audition subscenario also uses the real Samsung USB C Earphones
  output at 48 kHz stereo: eight new epochs, one output, no delayed backlog and working local Stop.
  Input remains routed; these short physical observations are neither acoustic latency measurements
  nor sustained-pressure/device-fault closure.

Reproduction commands:

```text
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*MixerDelayTests" --filter-class "*SampleAuditionTests" --filter-class "*AudioTimeStretchTests" --filter-class "*ResamplingWorkflowTests" --max-threads 1
```

VisualCheck commands use the current Release executable, scenario name and an isolated ignored
output parent; settings additionally receives `examples/Neon Orchard/Neon Orchard.seqvium`.
No native flag is supplied. GUI fixtures/settings and reader probes remain local under
`.artifacts/recent-regression-audit/` for reproducibility, not distributed product resources.
VisualCheck applied its existing rolling retention to older generated verification runs and retained
protected manifests. No manual broad repository cleanup was performed.

Investigation failures are retained honestly: the three new reproductions failed before correction;
an initial portable test tried deleting an assumed import directory instead of the actual owned
fixture WAVs (corrected to remove original copies); two overlapping initial CLI invocations caused
an executable-copy retry; a missing archive path and a probe reference path were corrected. These
are harness/setup errors, not new audio failures. Final build/tests were sequential and clean.

Initial Git status was clean at `b862b3c9`. Final task changes remain unstaged: three Core files,
four regression test files, BUILD and their technical evidence/owners. No pre-existing user changes
were found. Root ProjectStats reports are regenerated separately as structural diagnostics, never
counted as executed tests. Product README/setup descriptions were reviewed and remain applicable.
