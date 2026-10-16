# SEQ-RACK-GAIN-AUDIT-R1 — live Pattern Gain and Rack audit

Role: Completed checkpoint evidence and explicit verification limits.
Audience: Developers reviewing realtime parameter ownership and Rack interaction safety.
Status: Implemented, product **0.1.16.0004**, one BUILD; final verification recorded below.

## Cause and correction

Baseline was clean `master`, HEAD `ca41eaccaf2dfe8eb34b9ae358839cd3db0c8fba`, product 0.1.16.0003.
The F1 fader's Preview handler updated only local numeric/drawn presentation. Completed invoked
SetGraphGainAsync; only that canonical edit notified GraphExecutionCoordinator. This explains why
the readout moved before sound changed. F1's report explicitly described visual-only drafts.

The toolbar now opens an identified gesture on DesktopWorkflow's serialized owner, freezes its
lifecycle/generation/attachment/node/starting value, and publishes temporary Gain parameters through
the current coordinator. RealtimeSampler consumes immutable latest coefficient/Mixer tables qualified
by prepared execution identity, authority and Stop stamp. The corresponding local Gain and linear
source transfers before Mixer change together. No global RealtimeSampler.SetGain, second engine,
device stream, graph preparation, schedule/media decode or scratch rebuild is used for movement.

The same sampler, voices, phase, clock and Mixer FX continue. The callback exchanges/reads prepared
tables; producer work and allocations remain off callback. Accepted parameter updates also carry
current Gain coefficients with Mixer parameters, including superseded/coalesced updates. Mixer/Song
Gain acceptance and history can therefore retain the prepared identity as well as voices/time.

Escape/capture loss, surface priority/reflow, deactivation, selection, canonical Edit/Undo/Redo,
Stop/Panic and project teardown withdraw the draft. An old gesture cannot cancel or commit a newer
one, and generation/selection ABA cannot revive it. Release uses one canonical Edit, no-op/cancel uses
none, and short Gain commands do not pulse Busy. Save, frozen offline rendering and R8 never read the
overlay. Existing effects may retain the tail excited by temporary input; cancellation restores the
accepted input without rewinding DSP history. Runtime tail allowance remains finite/conservative.

Graph/Node Editor gestures retain their existing commit behavior and complete-target priority. The
horizontal fader, cap material, exact readout, invalid legacy repair, keyboard and accessibility remain
unchanged. No schema/reader, new instrument, effect, setting, composition or redesign is introduced.

## Addressed audit

Actual/native Rack checks pass Duplicate, reorder, Ctrl-copy insertion, cancellation, Undo/Redo,
micro movement, outside drop, capture loss, stale generation/ABA, step separation and exact Source
double-click. The new sustained physical scenario additionally exercises reorder/cancel/Ctrl-copy
while music plays, retaining surviving voices/time. Routed held Gain probes cover Graph priority,
detailed Editor priority, toolbar overflow reflow, capture loss and Stop, without acceptance/history.

Existing automated configured Drum/Synth/PCM independence, mapped-zone identities/settings, shared
media, bidirectional edits, graph signal/order and portable history remain green. Independent copies
keep their explicit shared Mixer destination; no Mixer channel is manufactured. No additional small
Rack cloning/reorder defect was reproduced, so those production implementations are unchanged.

## Automatic verification

Windows 11 x64, .NET SDK 10.0.402/runtime 10.0.13, net10.0, MTP/xUnit v3, one test thread.
Unchanged locked dependencies/assets were reused.

```powershell
dotnet build Seqvium.sln -c Release --no-restore -m:1 -v:q
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --max-threads 1
```

Full Release build: **6.01 s**, zero warnings/errors. Nineteen new cases bring RackWorkflow to **38/38** (2.852 s CLI).
Related GraphRealtime/Rack/Mixer/Delay/Reverb/Automation pass **162/162**
(17.093 s CLI) before the final four-case integration extension. Final full Release suite:
**1839/1839**, zero skips, **72.783 s CLI** (72.642 s runner). No assertion or environment setting
was changed between the failed first full pass and this green repeat.
Final whitespace-only cleanup rebuild is also green (3.67 s); product metadata checks pass 31/31 (8.817 s CLI). Relative
destinations/anchors pass 903 checks and `git diff --check` is clean.

New evidence includes actual prepared PCM before release, cancel/commit/Undo/Redo at 44.1/48 kHz,
Pattern/Song with/without Mixer, unchanged execution/voices/position/preparation count, Save/Open
and independently frozen PCM isolation, stopped/no-op/selection ABA, unconsumed stale messages,
latest gesture ownership, zero measured warmed apply/cancel callback bytes with/without Mixer,
coalesced Gain/geometry/Mixer updates, and bit-identical continuing Delay/Reverb PCM against
separately prepared canonical parameter oracles. This is bounded verification, not pressure acceptance.

## Actual windows and physical output

`rack-gain ... native` passes guarded Windows input against the production window and **Line (Steinberg UR12), 44.1 kHz
stereo**. During captured drag, Gain zero produces DSP Peak/RMS zero at
frame **52,920**; the raised draft produces peak **0.021832082** at frame **101,430**. Canonical Gain
remains **0.25** in both observations, with two voices, epoch **2** and prepared identity **2**.
Escape restores nonzero accepted sound; release is one Undo, native Undo/Redo restores the respective
DSP level, and no Busy observation occurs. Playing row operations and routed cancellation follow.
Stop and window shutdown retain joined output ownership. No system-volume/device setting changes.

`rack-workflow ... native audio` independently passes existing native Rack/fader/exact actions and
physical accepted Gain/reorder. RU/EN × Dark/Light 1400/1000/640-DIP populated/empty/overflow windows,
long names, exact fractional values and legacy **1.1251** repair/Undo/Redo pass. All twelve actual field
center differences are **0 DIP**. Actual populated, empty, invalid and native-screen overflow images
were reviewed: the existing materials/hierarchy, contrast and reachability remain; no visual change
was justified. Setup, forced cancellation and preference/geometry changes are explicitly routed,
separate from native trajectories. Local reference images were inspected without distributing them.
The final `gestures` route additionally passes the Graph/Node Editor hierarchy, exact toolbar
Enter/Escape/focus/stale input, eight routed gesture groups and eleven guarded native graph checks
at zoom 0.5/1/2. Its twelve actual field pairs differ by at most 1 DIP. Shared styles are unchanged.

## Failed attempts and limits

- The initial implementation incorrectly advanced executing revision for a coalesced Gain/geometry
  update. The unchanged existing regression caught it; preserving audio/equivalent provenance fixes it.
- A new fixture initially performed its first Save while already playing. Added media roots legitimately
  invalidated preparation and obscured the Gain continuity assertion. Saving before Play isolates the
  intended parameter boundary; saving that same canonical project during preview still verifies isolation.
- Initial new-test/tool compilation failures were missing namespace/event arguments, analyzer requirements
  and an incorrect GraphHit enum name; all were corrected without weakening checks.
- The first full suite passes 1838/1839 but the unchanged EQ/Compressor allocation assertion records **6064 bytes**. Its
  isolated repeat passes unchanged (1.158 s CLI). The cause is not established;
  no DSP/GC/environment attribution or pressure claim is inferred. The subsequent full run passes all
  1839 unchanged tests. The failed run takes 61.834 s CLI; it is not counted as a successful L3.

Physical output activity plus DSP measurements establish delivery/continuity and numerical amplitude
changes, not subjective listening, DAC latency or acoustic click quality. High/mixed DPI, other OS,
maximum-load pressure and existing R4-F2 gaps remain outside this checkpoint. Recovery is not implemented;
no new recovery mechanism or persistence promise is introduced.

Ignored evidence lives under `.artifacts/rack-gain-audit/`: `native-final` retains the richer live
native/UR12 receipts, `rack-native` the localized windows/fields and existing native audit. Earlier
local attempts can be rotated by the existing retention policy. That policy rotated an older generated
Rack render during this task; protected native evidence and user/reference/library files remain intact.
The budget advisory for protected/unknown evidence remains. Product source, regression tests and reusable
VisualCheck route are permanent; generated projects/JSON/screenshots stay local. No index/history/remote
operation was performed. See [audio](../AUDIO_ENGINE.md#application-convergence-and-revision-observation),
[UX](../UX_CONTRACT.md#channel-duplication-order-and-pattern-gain-seq-rack-workflow-f1) and
[verification](../TEST_EXECUTION.md#rack-workflow-verification) for current owners.
