# SEQ-R2-F3 source access and transient audition evidence

Role: Reproducible bounded F3 verification and R2 acceptance audit.
Read when: Reproducing discovery/reuse tests or the explicitly audible preview smoke.
Authoritative for: This change's observations, declared smoke checks and evidence limits.
Not authoritative for: Product contracts, permanent backend choice, acoustic latency or release support.

## Baseline and declared checks

Initial clean `master`, HEAD `f9c04bb232e0966fb094a274276a8154381dfcc8`; index, tracked working
changes and untracked files were empty. No staging, commit, push or other Git history operation.
F3 is an authorized work package within R2, not a new official numbered stage.

Before physical collection: Windows x64, SDK 10.0.401/runtime 10.0.12; explicitly invoke
`dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll audition-smoke`.
The harness warns on stderr that sound will play. It authors one second of mono 44.1 kHz float WAV,
220 Hz sine at amplitude 0.02, with preview gain 0.05. No system volume/rate/settings changes.

Run two fresh sessions: default diagnostics-off and explicit diagnostics-on. Both must pass initial
silence, external decode without import, continued prepared preview after external deletion, natural
EOF, acknowledged Stop, replacement, live cancellation, project-close Stop and joined cleanup.
Canonical serialized state/Undo/resources must remain unchanged; all six prepared states must release.
Default mode must allocate zero diagnostic array payload and capture/observe nothing. Diagnostic mode
captures a bounded prefix including silence and the full first preview; an independent native-pitch
source/interpolation sum compares within absolute tolerance 2e-6. Require no observed completed-service/
packet misses, nonfinite samples or processor allocations in this short diagnostic smoke.
This is not the 60-second F2 pressure acceptance series; default-mode zero metrics mean not measured.

## Implementation and deterministic verification

SourceAccess is one-level/explicit, visiting 64 entries by default and at most 256, counting folders
without traversing them. Sequential reads/validation reuse the 16 MiB WAV bound and F1 decoder.
External path observations do not become resource identities; project results retain accepted
ResourceIds and captured lifecycle/revision. No hashes/names/paths collapse independent imports.
Availability is rechecked at preview/import/reuse, not guaranteed by discovery. No watcher, catalog,
library cache, filesystem mutation or new assembly/dependency.

Raw audition prepares one leased native-pitch source with a natural-EOF frame bound using the F1
OfflineSampler kernel. Empty prepared revision/occurrence denotes no canonical music; no fake saved
document is created. Dedicated RealtimeSampler publication retains bounded F2 ownership. Tagged Start
cannot resurrect an older source while retirement is blocked. Live cancellation is a nonblocking
prepared token check; Close notifications execute on the document owner, never the callback.
Stop/EOF/cancel retire temporary state only after borrowing ends. Await preparation and join output
before final Dispose; a join timeout never authorizes disposal. Device termination prevents later
publication/restart in that same consumer. No graph/contextual preview or redesigned musical transport.

External acceptance remains F1 BeginImport/Prepare/Accept. BeginReuse/Prepare/Accept validates existing
managed media and current authority, then creates a fresh independent SoundId over that ResourceId.
It writes no WAV and silently retargets no other sound/part. Part attachment uses explicit ordinary
ProjectEdit. Undo/Redo, resource/Pattern/placement IDs and Save As/reopen are preserved; a shared resource
is transferred once per Save As destination. Existing conservative physical retention is unchanged.
As with F1 import, Accept revalidates bounded media integrity on the control owner before editing;
no GUI throughput, filesystem race-proofing or broad storage performance is claimed.

New deterministic suites: SourceAccessTests, WavAuditionTests, WavResourceReuseTests. They cover all
declared source/preview/reuse acceptance cases, including gated in-flight cancel/supersession/Close,
completed candidate capacity, raw accepted-media audition, independent leases/owners, disposal, failure,
zero warmed Process allocation, sound sharing independence, one physical resource, Undo/Redo and Save.
Exclusive-file refusal was physically tested on Windows; no Linux/macOS permission claim follows.

## Actual physical results

Windows 11 Pro 10.0.26300 x64, SDK 10.0.401/runtime 10.0.12, existing Steinberg UR12 default endpoint
`{0.0.0.00000000}.{af417ee0-95bf-4faf-ac43-7739c2bb6304}`. Both preview sessions queried WASAPI shared
event-driven **44100 Hz stereo float32, 441-frame/10 ms period, 970-frame capacity**, IAudioClock
352800 units/s and successful MMCSS. Stream latency query 0 is not measured acoustic/DAC zero latency.

| Mode                     | Seconds / services | Diagnostic array payload | Capture samples | EOF / Stop / replacement / cancel / Close | Created / released / live states |
|--------------------------|--------------------|--------------------------|-----------------|-------------------------------------------|----------------------------------|
| Default, diagnostics off | 1.7564447 / 176    | 0 bytes                  | 0               | all passed                                | 6 / 6 / 0                        |
| Explicit diagnostics     | 1.7437699 / 175    | 1641004 bytes            | 110250          | all passed                                | 6 / 6 / 0                        |

Both joined, reported no native failure or padding exhaustion, kept identical canonical state with no
resource/Undo/storage acceptance, and reached a separately counted terminal boundary. Requested packet
Stop 5 was acknowledged before subsequent preview/Close; cleanup does not synthesize acknowledgments.
Natural EOF retired the first source before replacement, although its original file had been deleted.
The first complete diagnostic prefix includes silence before start and after EOF, verified by an
independent source/interpolation sum: maximum absolute error **5.5271454080241256e-11**, tolerance 2e-6.
Measured completed-service/packet misses and processor/service allocated bytes were zero in that short
diagnostic run. Default mode does not measure those timing/allocation counters; their zeros are not proof.

The first smoke failed in verification: the harness called canonical Encode after document Close,
correctly rejected by the existing document contract. It had already joined/released output. The
harness now compares serialized bytes before Close and unchanged immutable snapshot after Close;
the complete two-session smoke was rerun successfully, without altering production Close semantics
or loosening acceptance. Local ignored logs: `tools/Seqvium.DeviceCheck/bin/f3-audition.jsonl` and
`f3-audition-reviewed.jsonl`.

## Post-change F2 regression evidence

All F2 harness paths explicitly enable diagnostics, preserving full reporting rather than benefiting
from disabled checks. Same authored WAV, device, thresholds and final root-revision pressure workload
as the existing F2 harness/protocol; not a repeat of the full six-run historical series.

Units below: microseconds, median / p95 / p99 / worst.

| Run               | Processor                  | Full service                | Wake interval                         | Seconds / services | Service frames |
|-------------------|----------------------------|-----------------------------|---------------------------------------|--------------------|----------------|
| Baseline 1 voice  | 3.9 / 31.5 / 48.5 / 1101.7 | 9.9 / 46.7 / 137.3 / 1113.8 | 10157.1 / 10619.6 / 10902 / 10925.8   | 2.0176844 / 202    | 441–441        |
| Baseline 4 voices | 9.8 / 11.6 / 33.5 / 135.3  | 13.5 / 19 / 36.7 / 139.1    | 10130.4 / 10642.4 / 10878.9 / 10990.8 | 2.0154723 / 202    | 0–441          |
| Baseline 8 voices | 18.2 / 19.6 / 38.7 / 43    | 21.5 / 27.4 / 45.5 / 47.2   | 10135.3 / 10524.6 / 10845.5 / 11041.4 | 2.0188713 / 202    | 441–441        |
| Pressure 8 voices | 18.1 / 23.4 / 49.7 / 930.7 | 21.9 / 34.6 / 60 / 939.6    | 10173.1 / 10615.2 / 10994.4 / 13401.1 | 60.0194224 / 6003  | 441–441        |

All four passed with zero processor/service/packet misses, padding exhaustion, nonfinite samples and
processor/service/running-worker managed allocated bytes in F2's declared instrumentation boundaries.
Prefill is 970 frames, separately from services. Baseline captures: 88200 samples/99 partitions each,
offline error 0; independent maximum errors 4.637606e-10 / 5.189642e-9 / 1.606660e-8, below 2e-6.
No baseline GC; process-wide allocation totals 38304 / 33216 / 33192 bytes are control/setup, not callback.

Pressure: 4955575504 process-wide allocated bytes, **563/563/563 GC**, 5634 cycles, 1442304 gain controls,
142 accepted states, 149 preparation cancellations including 93 observed in flight, 47 observed in-flight
supersessions, 47 stale publications, 28 stale handoffs and 0 cancelled-publication/capacity refusals.
141 retirements and **142/142 released states, zero live**, eight observed voices, requested Stop 2
acknowledged before Close, zero active voices and two submitted silent packets after Stop; joined worker.
Cancellation/capacity refusal is separately covered deterministically. Local ignored logs:
`f3-f2-smoke.jsonl`, `f3-f2-pressure.jsonl` beneath the harness bin directory.

This supplement establishes this bounded eight-voice 60 s regression, not new six-run performance
acceptance, multi-hour stability, acoustic latency, hardware dropout counters/removal, lower periods,
other endpoint/formats/platforms, clean packaging or a permanent managed/native choice.

## Reproduction and artifact identity

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll audition-smoke
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll smoke 2
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll pressure 60
```

Locked restore and full Release build pass with **zero warnings/errors**; full MTP/xUnit suite passes **251 tests, zero
failures/skips**, preserving all 215 previous tests. New files use existing SDK default
source inclusion; solution/project manifests, lock files and warning policy require no changes.
No hosted CI or external/native dependency is introduced. [Harness guide](../../tools/Seqvium.DeviceCheck/README.md)
separates explicitly audible verification from ordinary tests.

Measured/final production artifact SHA-256:
Core `D15889C41D5F0E9CF1AB1DF3F0EE59FE9BEDF8D04B2DB7288009D422CF5E7A41`;
Windows adapter `56E1DA006D9C1A14E1446CA711E80C4612EB7F313FE422B93B62DF6F37B688C4`;
harness `D852449E5BC0E9388281A3C8A303F5077E891699E169AD4B3EBADD4A5F44C211`.

## R2 completion audit

F3 is **locally accepted-ready** in its bounded API scope. It is not a numbered roadmap stage.
Auditing actual [R2 roadmap requirements](../ROADMAP.md#seq-r2--audio-resource--device-foundation):

| Requirement                                                                        | Implemented/local evidence                                                                                                                  | Disposition                                                   |
|------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------|
| Supported WAV/import/managed media/source independence                             | F1 plus explicit F3 import/source deletion/reuse/history/Save tests                                                                         | Satisfied for declared formats/bounds                         |
| Pitched sampler/intensity/duration/release/overlap/scheduling/transport/output     | Shared F1/F2 kernel, preserved 215 tests, existing full F2 and fresh bounded regressions                                                    | Satisfied for declared execution/device scope                 |
| Minimal external/project find/audition/import/reuse                                | F3 bounded discovery, raw preview, explicit import and independent sound reuse; no GUI required                                             | Satisfied for API/controlled-device foundation                |
| Transient versus durable ownership                                                 | No preview edit/storage/Undo; independent leases and joined retirement; reuse does not duplicate WAV                                        | Satisfied within tested owner/candidate rules                 |
| Backend-independent input/output/device foundation and separate logical selections | Core PCM/output is independent; Windows output supports default/explicit endpoint. Input endpoint discovery/independent selection is absent | Genuine remaining bounded R2 completion gap                   |
| Plan MIDI/capture ownership without polished recording/all backends                | Owners, timestamps/clock separation, durable acceptance boundary and explicit remaining gaps in Audio                                       | Planning requirement met, no working input/MIDI/capture claim |

**Overall R2 remains in progress / partial**, not blocked on current F3 implementation and not complete.
The next bounded device-foundation scope should establish meaningful independent logical input/output
enumeration/selection/availability and session ownership, with hardware-independent failure tests and
actual endpoint-query evidence. It need not stream input, record, map MIDI controllers or adopt another
backend. This report does not authorize that implementation or R3.

Q-001–Q-007 retain wider engine/ABI/workload/period/clock/distribution evidence; Q-047 retains graph/
opaque-domain/routing scope; Q-062/Q-069 retain actual hardware loss, wider clocks/recovery/negotiation.
These wider questions can remain open without demanding a native engine or every device/backend before
R3. Q-065 retains broader Browser/catalog/library/UI/platform/scaling evidence; Q-011 contextual audition
is still future. Multi-hour, DAC, release packaging, Personal Library, recording and mature catalog are
not added R2 blockers. The current concrete blocker is the minimum separately selected logical endpoint
foundation, not those expanded workflows. No R3/UI, Mixer/graph, plugin, Sample Lab or recording started.

Current contracts remain in [Sample workflow](../SAMPLE_WORKFLOW.md#r2-f3-bounded-source-access-raw-preview-and-reuse),
[Audio](../AUDIO_ENGINE.md#r2-f3-transient-one-shot-execution) and
[Architecture](../ARCHITECTURE.md#r2-f3-source-access-and-audition-ownership); this report owns observations.
