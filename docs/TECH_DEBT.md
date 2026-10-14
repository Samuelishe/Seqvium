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

The implemented Mixer resets IIR/detector/protection state on explicit seek and Song range wrap,
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

Keep only currently existing unresolved implementation debt here. Remove resolved entries and retain
useful resolution/evidence history in the cold archive under documentation governance.
