# SEQ-R11-MIXER — musical mixing and compact studio direction

Date: 2026-10-14. Environment: Windows 11 x64, .NET SDK 10.0.401/runtime 10.0.12,
repository-pinned Avalonia 12.1.3. This report owns observations, not product contracts or aesthetic approval.

## Scope and design

Implemented a working project Mixer, not a decorative pane: explicit shared-sound assignments,
channels/buses/Master, gain/trim/stereo balance, Mute/Solo, real peak/RMS/hold/reduction, acyclic output
and pre/post send routes, high-pass/three-band EQ, linked compression and optional Master sample-peak
protection. Canonical edit/history/format and shared prepared realtime/offline execution are reused.
The linear graph source projection preserves local gains; all Song occurrences aggregate before
stateful channel processing. Playlist lanes remain clip organization/audibility, not Mixer identity.

Private reference study covered the catalog/notes, mixers, EQ/dynamics and Light timelines, plus the
owner's FullHD FL Studio and Cubase workspace images. Their successful desktop density, channel rhythm,
contextual processing and tactile controls informed original implementation; no image/icon/texture
was extracted or distributed. After the owner rejected the initial oversized composition, strips and
sections were substantially reduced, processing moved to a side inspector, and shared chrome/Rack/
Playlist/Pattern navigation became denser. The default Mixer is 1008×408 DIP, about 19.8% of FullHD;
its channel body is 60 DIP, Master 72, without stretching them on resize.

Original vectors, sculpted fader gradients, quiet source bands, graduated compact dials and bounded
machined chassis grain now use shared theme resources. Light is independently steel/colored-ink,
Dark slate/jade. Tab templates use host typography/state colors instead of the Fluent accent defaults.
All exact controls and routes operate; plots are calibrated analytical curves, not fabricated spectra.
The compressor transfer includes makeup and distinguishes its input/output ranges.

[UI design](../UI_DESIGN.md#compact-studio-instrument-direction) owns accepted visual direction;
[design system](../DESIGN_SYSTEM.md#mixer-and-chassis-components) owns reusable components;
[audio](../AUDIO_ENGINE.md#project-mixer-execution) and [format](../PROJECT_FORMAT.md#mixer-persistence)
own execution and compatibility. [Roadmap](../ROADMAP.md#desktop-design-continuation) records the
remaining editor-design passes; a completed whole-interface radical redesign is not claimed.

## Native Neon Orchard workflow and safety

Original project/WAV/media/comparisons, private sample library and references were read-only.
A separate project **and its entire managed media folder** were copied into the owner-designated
`C:\Users\filed\Desktop\Neon Orchard - Mixer`. All musical edits used actual production GUI controls,
guarded Windows SendInput, keyboard and standard dialogs. Internal project/music observations were
read-only assertions; no musical JSON or API authoring created the delivered mix.

Explicit Mixer creation yielded 12 source groups across 19 shared sounds; a thirteenth Drums bus
received Kick/Snare/Clap/Closed Hat/Open Hat/Rim/Tom outputs. The original musical state compares
exactly after excluding only Mixer: notes, Patterns, clips, lanes, local graphs, sounds and resources
are unchanged. The source project remains format minor 2; the independent mixed copy uses minor 3
and minimum reader 3. SHA-256 audit of all **1255 protected original/library/reference files** finds
zero changed files. All **379 Ready library WAVs** pass actual decoder inspection; no library writes.

Native balance changes relative to the original local gains:

| Source                  |    Fader dB |      Balance |
|-------------------------|------------:|-------------:|
| Kick / Snare / Clap     | 0 / -1 / -4 | 0 / 0 / +.12 |
| Closed Hat / Open Hat   |     -2 / -3 |  -.32 / +.28 |
| Rim / Tom               |     -2 / -1 |  -.22 / +.20 |
| Bass / Keys             |     +1 / -1 |     0 / -.14 |
| Pizz forte / Flute      |     +4 / +2 |  +.27 / -.30 |
| FX / Drums bus / Master | -3 / 0 / +3 | +.38 / 0 / 0 |

Bass, Keys, Pizz forte, Flute and Snare use active EQ. HP values are 30/140/180/180/100 Hz;
low/mid/high gains are respectively +1/-2/-1, -2/-1.5/+1, -2/+2/+1, -1/+1/+1, -1/+1/0 dB.
Mid frequencies are 350/900/2500/1200/2000 Hz. Bass compression uses -24 dB, 3:1, 18/160 ms,
+1 dB makeup; Drums uses -22 dB, 2:1, 25/130 ms, +1 dB makeup. Both use 6-dB knees.
Master sample-peak protection is enabled. No send was needed in this mix; send semantics are
deterministically tested rather than falsely claimed as a native musical result.

Final deliverables outside Git:

- `C:\Users\filed\Desktop\Neon Orchard - Mixer\Neon Orchard.seqvium`
- sibling `Neon Orchard.seqvium.media` (all nine copied WAVs retained)
- `C:\Users\filed\Desktop\Neon Orchard - Mixer\Neon Orchard - Mixed.wav`

Native Save/reopen preserves Mixer intent, full Song reaches Ended on **Line (Steinberg UR12),
44.1 kHz stereo**, real meters/reduction are observed, and native GUI export produces the WAV.
Physical playback, submitted PCM capture and acoustic perception are separate evidence; no subjective
listening or musical-quality judgment is claimed.

## Signal and physical measurements

Native WAV: 48-kHz stereo PCM16, **3,291,429 frames / 68.5714375 s**, peak **0.8408203125 (approximately -1.51 dBFS)**,
zero saturated samples. RMS L/R **.06263115/.06475745**;
stereo correlation **.99171116**. Duration matches the original. RMS difference from the original
is **.02218978**; original peak **.7347107**, overall RMS **.04496157**. These prove changed
PCM/balance, not a subjective improvement or wide-stereo mastering claim.

Delivered WAV SHA-256: `5f60db63206f750710a3e31b63b94d8a1ff7c91f121d2a3625d6c8dd5bd070db`.

The separate read-only `DeviceCheck mixer-project` runs the saved Song on physical WASAPI, capturing
submitted float PCM only in bounded memory. At device rate **44100**, capacity **970 frames**,
the full Song ends at **3,024,000 frames**, maximum observed voices **13**, peak **.8283261**.
Maximum submitted-float versus same-partition/same-rate offline error is **0**. Native 48-kHz WAV
versus independent 48-kHz float execution has maximum error **.0000152587890625** (PCM16 rounding).
No cross-rate bit-equality assertion is made.

Processor/service/worker allocated bytes **0/0/0**; processor/service/packet misses **0/0/0**;
padding exhaustions **0**; nonfinite samples **0**; acknowledged Stop and joined teardown pass.
Each channel's full frame count and real held peaks are observed. This is one full-song physical run,
not proof of long-pressure stability or resolution of earlier R4 pressure failures.

## Verification and failures

- Full Release solution build: zero warnings/errors. Full MTP/xUnit suite **1196/1196**, zero skips,
  9.123 s CLI summary. New Mixer subset **17/17**; final affected presentation/version/Mixer subset **68/68**. Ordinary
  locked assets were reused; no dependency update.
- Production-window component catalog and shared field/Graph matrix pass. Native Mixer review checks
  FullHD populated Playlist/Rack/Mixer, Dark/Light normal bank/processing, RU/EN × Dark/Light compact
  420×300 inspector reachability/alignment, actual fader/pan/EQ single Undo and curve Redo/Undo.
  Musical state is restored and saved after gestures. Windows screen captures were visually inspected.
- Native GUI Save/Open/full physical Song/export pass; device read-only PCM comparison passes separately.
  Synthetic test fixtures never substitute for delivered native musical authoring.

Initial full-suite failures were outdated pane-count/density/reader-gate expectations and were updated
to the new contracts. One later full run failed an existing WAV-preview zero-allocation assertion (6128 bytes); the
isolated exact test passed and the subsequent full 1196-case run passed. No cause
or permanent resolution of that isolated allocation event is claimed.

Native attempts also exposed real UI defects: custom TextBox missing its standard StyleKey, owner
thread reading rename Text, redundant selector commits and an empty Expander clipping the compact
footer. These were fixed and successful runs retained. Some attempts refused foreground/occlusion:
the harness formerly rejected a correctly owned native picker opening during mouse release, or tried
a tab after scrolling its header out of view. Owned-picker allowance preserves the unrelated-window
guard; explicit scrolling fixes tab reachability. Failed attempts are not labelled passes.

Evidence remains in ignored `.artifacts/seq-mixer/{tests,gui,catalog,matrix,device,samples}` under
existing rolling retention. Private user files/captures are not redistributable product assets.
Permanent focused tests and reusable native/read-only checks remain source, not disposable screenshots.

Final metadata/presentation/Mixer subset also passes after the PATCH build. Static audit checks 1201
relative documentation links/anchors across 21 changed Markdown files, with no broken targets;
`git diff --check` is clean. Actual Dark/Light overview captures are retained outside Git in the
result folder's `Mixer Review` directory for owner inspection. The index is unchanged; all tracked
edits and new product/test/check/report files belong to this task, with no pre-existing dirty work.

## Limits and checkpoint

The Mixer has a fixed EQ/compressor order, no delay/reverb, arbitrary FX rack, sidechain, automation,
external plugins, true-peak/LUFS analysis or latency compensation. Source projection supports the
actual linear stateless graphs, not arbitrary future stateful nodes. Explicit seek/range wrap has
cold FX state; [debt](../TECH_DEBT.md#mixer-cold-seek-and-range-wrap) defines its bounded resolution.
Maximum-channel physical stress, high/mixed DPI, screen readers and other OS/device evidence remain
outside this run. English technical parameter abbreviations remain in the RU effects fields.
Further coherent editor styling is planned; owner aesthetic acceptance remains separate.

One substantial standalone Mixer capability qualifies for **PATCH 0.1.4.0001 → 0.1.5.0000**,
resetting BUILD only, under [versioning](../VERSIONING.md#seq-r11-mixer-checkpoint). This neither closes
R4/R5/R10's wider acceptance nor creates a release. No staging, commit, push, tag or history mutation.
