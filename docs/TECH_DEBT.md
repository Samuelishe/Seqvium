# Technical debt

Role: Register of implementation compromises that actually exist.
Read when: Introducing, reviewing, or resolving an evidenced compromise.
Authoritative for: Existing implementation debt, impact, and cleanup conditions.
Not authoritative for: Hypothetical debt, open architecture choices, roadmap, or completed-work history.

No active bounded Rack channel-continuity debt remains. The stateless supported path and its limits
are owned by [Audio](AUDIO_ENGINE.md#playlist-song-preparation-and-execution); completed evidence is
in the [music workflow report](experiments/SEQ-PLAYLIST-MUSIC_REPORT.md). Arbitrary/stateful topology
continuity remains future capability, not an implemented promise.

## Mixer cold seek and range wrap

The implemented Mixer resets IIR/detector/Delay/protection state on explicit seek and Song range wrap,
while reconstructing musical voices. It does not preroll those stateful processors, so the first
transient after a jump can differ from uninterrupted streaming at that musical time. This is an
actual bounded compromise in `OfflineSampler.Seek`/`MixerRuntime.Reset`, not an export parity failure:
realtime and offline agree given the same reset context. Ordinary Pattern looping retains DSP state.
Resolve with bounded processor preroll/checkpoint policy when seamless processed seek/wrap is needed;
verify extreme settings, deterministic memory/time bounds and the shared transport/export boundary.
Do not substitute an unbounded render-from-start operation on the callback.

Future entries should identify the existing compromise, affected files/evidence, impact, and a
bounded resolution or exit condition. Open uncertainties belong to [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md);
new capabilities belong to [ROADMAP](ROADMAP.md).

## Browser selected-source decode cost

`InspectBrowserSourceAsync` and subsequent explicit/automatic `PreviewBrowserSourceAsync` each
normalize and read the complete external source. The inspection PCM is released before preview;
there is no persistent selected-source PCM cache. The worker path keeps this work off the UI, but
selection commands wait for cooperative cancellation of the previous worker. A stalled OS codec
cannot be forcibly cancelled. Large sources cause repeated disk/codec work and LOH allocation;
the 512 MiB source limit is not a process memory bound.

The [cross-stage audit](experiments/SEQ-CROSS-STAGE-AUDIT_REPORT.md#browser-cost-observation) records
one three-minute stereo MP3/FLAC observation: approximately 70 MB allocated per pass, with variable
latency up to 7.68 s. This is evidence of cost, not an attributed decoder/GC/UI hang. A follow-up
should first measure dispatcher progress and codec/GC time, then consider sharing decoded PCM only
within one inspection/auto-preview operation, retaining cancellation, source validation and joined
lease disposal. Avoid a general media cache or enlarged budgets without evidence.

Keep only currently existing unresolved implementation debt here. Remove resolved entries and retain
useful resolution/evidence history in the cold archive under documentation governance.

## Bypassed Delay storage and conservative tail reachability

R11 deliberately reserves a maximum one-second stereo ring plus Int64 generation tags on every
strip, including disabled strips. `MixerRuntime.Strip` allocates this on execution creation, not
on the callback. At 48 kHz a 64-channel bank plus Master adds 49,921,040 bytes (47.61 MiB) per
execution even in an old project without Delay intent. The recent audit exercises active/pending/
retired backpressure with three such banks: 149,763,120 bytes (142.83 MiB) of line payload alone.
PCM, graph storage, object overhead and deferred GC add more. The four-slot reservation is
conservative accounting, not proof of a 190.43-MiB simultaneous ring allocation or an OS memory cap.
This extra LOH/preparation/GC cost matters to unresolved R4-F2 pressure; no starvation attribution
to Delay has been established.

`MixerPreparation.Compile` also propagates Delay tail allowances across all strips/edges before
checking source reachability, Mute/Solo, zero sends/faders or Mix=0. An unused, muted or dry-only
maximum Delay each appends 34.05 seconds of exact zero PCM in the audit fixtures. Four such serial
strips exceed the 120-second gate and can refuse otherwise dry playback/export. The conservative
policy is documented in [Audio](AUDIO_ENGINE.md#musical-delay-v1-seq-r11-delay-f1); its cost is an
existing compromise, not a missing reverb/processor feature.

Follow up with measured disabled-bank preparation/retirement and GC/deadline behavior, then choose
bounded off-callback line admission for disabled strips if justified. Enabling must still publish
fully prepared exclusive storage and preserve Stop/Seek/history semantics. Separately derive a safe
audible source-to-Master tail bound, retaining serial/sends contributions, pre-fader routes and
already active live histories; static pruning must not truncate a fading bypass or a retained echo.
Exit evidence needs maximum-bank backpressure and PCM/late-echo/live-transition regressions, plus
pressure measurement before any R4 acceptance claim. [Audit](experiments/SEQ-RECENT-REGRESSION-AUDIT_REPORT.md)
owns the concrete scenarios; neither mechanism is redesigned by this checkpoint.
