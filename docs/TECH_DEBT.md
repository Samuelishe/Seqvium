# Technical debt

Role: Register of implementation compromises that actually exist.
Read when: Introducing, reviewing, or resolving an evidenced compromise.
Authoritative for: Existing implementation debt, impact, and cleanup conditions.
Not authoritative for: Hypothetical debt, open architecture choices, roadmap, or completed-work history.

## Continuous channel topology

The owner reports that adding/deleting a WAV channel interrupts music. DesktopWorkflow.Beat currently
issues explicit Stop for both operations; topology changes do not qualify for existing note-only
continuation, and deleting the last channel leaves an ineligible empty graph. Playlist introduces real
simultaneous playback without pretending this compromise is fixed.

Impact: channel management interrupts the continuous Rack, and Song using the affected source also
stops. Existing notes/graphs/media/history remain coherent. Exit condition: derive a bounded safe
topology/voice handoff (including silent/last-channel plans), map unchanged part/resource voice ages
and current musical time, retain Stop/Panic and preparation/retirement authority, and prove add/delete/
Undo/Redo across first/middle/last channels using deterministic PCM plus short physical output evidence.
Arbitrary topology or future stateful processors must retain explicit compatibility/refusal semantics.
This is an existing implementation debt, separate from new Mixer/audio-clip/automation capabilities.

Future entries should identify the existing compromise, affected files/evidence, impact, and a
bounded resolution or exit condition. Open uncertainties belong to [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md);
new capabilities belong to [ROADMAP](ROADMAP.md).

Keep only currently existing unresolved implementation debt here. Remove resolved entries and retain
useful resolution/evidence history in the cold archive under documentation governance.
