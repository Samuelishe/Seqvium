# SEQ-HOME-RESOURCES — home resources and theme preparation

Role: Bounded evidence for private resources and existing-editor visual refinement.
Read when: Reproducing/interpreting this checkpoint's resource and visual checks.
Authoritative for: Observed setup, verification outcomes and limits.
Not authoritative for: Product contracts, redistribution rights, R9 implementation or stage acceptance.

## Scope and resources

On 2026-10-13 the owner authorized home resources, reference assessment and bounded UI improvements
before Piano Roll, then explicitly requested updating the old checkout from GitHub. Clean `master`
fast-forwarded from `6d29ebc` to `04f75be` without conflicts/user-file overwrites. Work then inspected
the current Rack/Playlist/Node Editor and expanded WAV decoder. No staging, commit, push or tag.

`%USERPROFILE%/Music/Seqvium Sample Library` contains **379 working WAVs / 332,907,502 bytes**, separate
originals, official pages/notices, member/download provenance, hashes and JSON/CSV catalogs/receipts.
Five MusicRadar packs provide stratified acoustic/electronic/vinyl drums, 808, analog/digital Bass,
acoustic/electric keyboard chords/phrases and FX. Bounded HTTP ranges retrieved selected ZIP members
with CRC checks instead of whole 267/372/156/60/240 MB archives. Sixteen VSCO 2 CE files add soft/loud
single-note Upright Piano, Organ, Flute and two pizzicato Violin examples; Git blob checks, pinned
content tree, CC0 notices and Piano root mapping are retained. MusicRadar allows musical use and
prohibits standalone sample redistribution; private receipts distinguish this from CC0.
No audio, source snapshots, notices, images or private catalogs are distributed in Git.

All **19** Desktop/screens images were inspected, including AVIF through isolated Pillow 12.0.0.
Originals were renamed/moved to `%USERPROFILE%/Documents/Seqvium Resources/Design References` with
SHA-256 verification. Original bytes/extensions/names/paths, dimensions and identification confidence
are preserved. PR/PL/MX/EQ IDs and searchable offline gallery provide retrieval. Publication URLs
were not supplied and redistribution rights remain unverified. Empty inbox folders remain.
Static screenshots establish composition/visible feedback, not tested gestures. Private catalogs
own detailed provenance; [UI direction](../UI_DESIGN.md#reference-library-and-next-editor-direction)
and [R9 planning](../ROADMAP.md#piano-roll-preparation-and-bounded-first-slice) own public conclusions.

## Implementation and checks

Only Light colors/material inputs change: richer channel/clip bodies, jade interaction surfaces,
boundaries and bevel/shadow depth. Dark values are identical. Existing contrast tests now cover
all eight note/body and checked-indicator pairs in both themes. Rack paint guidance moves to localized
step/position tooltips and accessibility help; populated Playlist reclaims its instruction row while
selection-tool/context help retains gestures. Empty Playlist guidance remains. No musical state,
DSP, scheduling, schema or parameter semantics change; existing vectors/materials suffice.

Environment: Windows 11 x64 **10.0.26300**, SDK **10.0.401**, runtime **10.0.12**. Locked restore passes (~1.04 s);
initial affected test-project build passes (~5.46 s), DeviceCheck build passes (~0.77 s),
and final affected Release build after BUILD metadata/whitespace normalization passes (~1.87 s),
all with zero warnings/errors. Initial Light fill/track contrast failed the existing 3:1 constraint (2.80:1); changing
its track restores **3.22:1** without lowering assertions. Focused 165 cases then
passed; final metadata-inclusive **196/196** pass (~8.73 s test phase). No tests were removed.

```text
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*DesignSystemTests" --filter-class "*HostPresentationTests" --filter-class "*StepSequencerTests" --filter-class "*ArrangementTests" --filter-class "*NodeEditorTests" --filter-class "*ProductVersionTests"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll catalog "<absolute .artifacts task directory>"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll arrangement "<absolute .artifacts task directory>"
dotnet tools/Seqvium.VisualCheck/bin/Release/net10.0/Seqvium.VisualCheck.dll matrix "<absolute .artifacts task directory>"
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll sample-check "<Ready>" "<verification directory outside Ready>"
```

Decoder initially accepted 367/377 files and rejected ten mono PCM24 terminal data chunks missing
their odd RIFF pad. Working copies append one container zero/update outer length; complete PCM is
verified bit-identical and originals are untouched. Final audit accepts **379/379**, zero refusals;
separate single-note Piano audit accepts 10/10. Existing offline Piano/808 boundary checks complete
with reduced gate discontinuity and silence after the gate. No normalization, resampling or trimming.
Source roots are catalogued, but current import root 60 does not calibrate recordings automatically.

Actual-window catalog and successful unchanged Playlist retry pass. RU/EN × Dark/Light Playlist
ordinary/reduced rows meet the one-DIP field-alignment bound. Full field/Graph/Node Editor matrix and
focused gestures pass, including eleven separately guarded native Graph/Workspace actions. Rendered
Light Rack, Playlist, controls and Gain editor were inspected. Dark catalog appearance/values remain
preserved. Selected images are private review artifacts, not shipped screenshots. Normal binaries
are built for the next task.

## Failures and claim limits

Standard `steps` fails **Reduced tempo action is unreachable** at reduced English Dark overflow,
on both unchanged `04f75be` and refined source. It stops before later themes; no complete Rack pass.
A task-local copy changes only theme iteration order (Light first), keeps every assertion and renders
ordinary/reduced Light Rack before the same Dark failure. Investigate actual overflow/verification
timing before treating this as a product regression or harmless harness defect. No assertion bypass.

One post-change Playlist attempt failed first routed placement (`Sequence contains no elements`);
an unchanged retry passed. Retain this as a verification-reliability gap, not a uniform interaction
claim. Catalog/matrix and bounded native graph passes do not establish native Rack/picker/Explorer
drop, physical audio, acoustic quality, high/mixed DPI, screen readers or other-OS acceptance.
Earlier R4 pressure failures remain separate.

This is L0 + focused L1/L2 and selected L4, **not L3**. Last full 1052-case baseline stays
SEQ-PLAYLIST-MUSIC; cadence advances from one to two tasks. BUILD becomes **0.1.1.0003** for compiled
UI refinement; resource preparation consumes no independent version. Owner aesthetic approval remains
separate. No Piano Roll/Mixer/EQ implementation or application-scale redesign occurred.

## Retention

Private resources, preparation/preview tools and WAV audits remain outside Git for reuse/provenance.
GUI runs stay ignored under `.artifacts/home-resources` with existing manifest/retention policy;
failures and bounded successful evidence are retained without pinning every run. Selected before/after
images are copied outside Git for owner review. The reordered Rack source/build is retained outside
the repository under local `Verification/RackLightRender` for the unresolved overflow check, with
all assertions intact. Temporary AVIF preview and formatting helpers are removed. Required production
outputs and existing user preferences/projects are preserved.
