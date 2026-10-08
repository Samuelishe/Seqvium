# SEQ-R2-F4 independent endpoint evidence and R2 audit

Role: Reproducible local endpoint observations, selected-output checks and bounded R2 completion audit.
Read when: Reproducing endpoint/selection checks or assessing the R2 acceptance boundary.
Authoritative for: This machine's observed endpoints, verification results and their limitations.
Not authoritative for: Product contracts, permanent engine/backend, input streaming or release support.

## Scope and baseline

Date: 2026-10-08. Initial clean `master`, HEAD `9c4832260387fb58aa406e67f260b8fafe1dedca`;
staged/unstaged/untracked work absent. F4 is an authorized working subdivision of R2.
No staging, commit/push or other Git history operation; no Windows audio preference changes.
Smallest missing R2 foundation: independent logical input/output discovery, intent, availability and
joined output ownership. No new dependency/project, capture, MIDI enumeration, recording, ASIO or GUI.

Before physical collection, acceptance required role/default versus explicit identity, no fallback,
fresh worker-side validation, no canonical mutation, Stop acknowledgment integrity and join before
borrowed-state retirement. Ordinary tests use immutable injected discovery results and a join-only
boundary, not physical hardware or simulated WASAPI. Physical discovery must activate no capture;
audible output must require explicit ID invocation, warn and use conservative gain.

## Source and ownership

- Core: [AudioEndpoints.cs](../../src/Seqvium.Core/Audio/AudioEndpoints.cs) adds portable intent,
  immutable direction/state/default snapshots, explicit resolution and `AudioDeviceSession`.
  One `IAudioOutputLifetime` represents borrowing/join/release only. Core retains no COM/native code;
  native codes are opaque diagnostics interpreted only by Windows.
- Windows: [WindowsAudioEndpoints.cs](../../src/Seqvium.Audio.Windows/WindowsAudioEndpoints.cs)
  owns all-state MMDevice discovery, names and six role queries; `WasapiSelection` joins old output
  before discovery/open and creates a processor from actual negotiated facts.
  [WasapiNative.cs](../../src/Seqvium.Audio.Windows/WasapiNative.cs) adds property/QI/state helpers;
  [WasapiOutput.cs](../../src/Seqvium.Audio.Windows/WasapiOutput.cs) revalidates role/ID, state and
  render direction before activation. Existing service/PCM/fault processing is unchanged.
- Tests: [AudioEndpointTests.cs](../../tests/Seqvium.Tests/Audio/AudioEndpointTests.cs), 23 new cases.
  Full 274 tests preserve all 251 existing cases. Hardware-independent lifecycle checks use actual
  Core preparation/PCM state, confirmed versus failed join and release accounting.
- Harness: [EndpointCheck.cs](../../tools/Seqvium.DeviceCheck/EndpointCheck.cs) plus Program dispatch;
  existing physical modes remain explicit. No solution/project/lock changes; SDK default Compile
  inclusion supplies the new sources.

OS IDs are opaque stable OS identities, not display names or portable musical UUIDs. See Microsoft's
[device properties](https://learn.microsoft.com/en-us/windows/win32/coreaudio/device-properties)
and [default direction/role query](https://learn.microsoft.com/en-us/windows/win32/api/mmdeviceapi/nf-mmdeviceapi-immdeviceenumerator-getdefaultaudioendpoint).
Property stores and collection/device references release on every exit, ID strings use CoTaskMemFree,
PROPVARIANT uses PropVariantClear, COM initialization balances only this call's successful initialization.
Discovery reads properties; it never changes defaults or activates IAudioClient. Snapshot infrastructure
failures refuse the listing; default-query errors and optional-name failures stay explicit observations.

## Physical endpoint snapshot

Silent `endpoints` exit 0, snapshot UTC **2026-10-08T20:24:03.0717916+00:00**.
Environment: **Microsoft Windows 10.0.26300**, x64, SDK 10.0.401, **.NET 10.0.12**, Release.
Output: 25; NotPresent 17, Active 6, Unplugged 2. Input: 8; Unplugged 2, Disabled 2, Active 2, NotPresent 2. All 33
optional-name reads and six role queries returned code 0.

These are observations of this machine, not product defaults, universal supported hardware or a
recording capability claim. Names are exact OS display labels, including old disconnected instances.
Duplicate names have distinct IDs. Capture endpoints are device-level entries; no per-channel
microphone endpoints, channel count, clock facts or capture timestamps are inferred.

| Direction | Observed name                                       | OS endpoint ID                                            | State      |
|-----------|-----------------------------------------------------|-----------------------------------------------------------|------------|
| Output    | 2769M (NVIDIA High Definition Audio)                | `{0.0.0.00000000}.{03dc63e5-60e0-4f60-b88d-1a16948e5319}` | NotPresent |
| Output    | 2769M (NVIDIA High Definition Audio)                | `{0.0.0.00000000}.{08feac06-1f94-4880-85e6-6202904a363f}` | NotPresent |
| Output    | Q27G2SG4 (NVIDIA High Definition Audio)             | `{0.0.0.00000000}.{0aaf0c88-0f37-4789-8ea5-d11e33aa20c6}` | NotPresent |
| Output    | Q27G2SG4 (NVIDIA High Definition Audio)             | `{0.0.0.00000000}.{40ccbb34-49f8-44dc-ae4d-57da728912b5}` | NotPresent |
| Output    | MStar Demo (NVIDIA High Definition Audio)           | `{0.0.0.00000000}.{4772c9ce-1c83-4a00-9e0c-bc5cfcdec00f}` | NotPresent |
| Output    | HiTV (NVIDIA High Definition Audio)                 | `{0.0.0.00000000}.{4c502dc1-351f-4fe6-8ea1-9446be1ed354}` | Active     |
| Output    | Динамики (Steam Streaming Speakers)                 | `{0.0.0.00000000}.{4c64bfde-3072-4ba8-a360-d42b863b5fc4}` | Active     |
| Output    | 2769M (NVIDIA High Definition Audio)                | `{0.0.0.00000000}.{4d37b3de-acef-4ddd-94b3-ec13a6987877}` | Active     |
| Output    | NVIDIA Output (NVIDIA High Definition Audio)        | `{0.0.0.00000000}.{57a98c57-5956-4239-a738-f7609ce382ac}` | NotPresent |
| Output    | Digital Audio (HDMI) (High Definition Audio Device) | `{0.0.0.00000000}.{5f6ec870-2953-414d-a234-f487d91783f2}` | NotPresent |
| Output    | Digital Audio (HDMI) (High Definition Audio Device) | `{0.0.0.00000000}.{688b5cc5-b726-4f60-8e86-a89812daa33e}` | NotPresent |
| Output    | Digital Audio (HDMI) (High Definition Audio Device) | `{0.0.0.00000000}.{6dd66e72-f10b-4dcf-bc3f-a3e672e71b4c}` | NotPresent |
| Output    | Линия (2- Steinberg UR12 )                          | `{0.0.0.00000000}.{73080995-82b4-4735-9563-a3d787b96b08}` | NotPresent |
| Output    | MStar Demo (NVIDIA High Definition Audio)           | `{0.0.0.00000000}.{78eedf2c-c90a-44aa-91d0-e48aa1691902}` | NotPresent |
| Output    | Динамики (Realtek USB2.0 Audio)                     | `{0.0.0.00000000}.{9ad3af53-a74d-44a6-98cf-ec7ac4af748e}` | Unplugged  |
| Output    | MStar Demo (NVIDIA High Definition Audio)           | `{0.0.0.00000000}.{a0e90be1-a96f-4781-b39c-3a343262511f}` | NotPresent |
| Output    | NVIDIA Output (NVIDIA High Definition Audio)        | `{0.0.0.00000000}.{ab67bc67-ebe9-4689-920f-a8204798a8fb}` | NotPresent |
| Output    | Line (Steinberg UR12 )                              | `{0.0.0.00000000}.{af417ee0-95bf-4faf-ac43-7739c2bb6304}` | Active     |
| Output    | SPDIF Interface (Realtek USB2.0 Audio)              | `{0.0.0.00000000}.{b8dd6b10-4406-4464-8557-d379f04bd5d9}` | Active     |
| Output    | Динамики (Steam Streaming Microphone)               | `{0.0.0.00000000}.{bb76ddce-478c-48f1-8dfe-e8102dc0a331}` | Active     |
| Output    | Headphones (Realtek USB2.0 Audio)                   | `{0.0.0.00000000}.{c2577bcd-6023-40f6-a584-dbb710fc1b1b}` | Unplugged  |
| Output    | NVIDIA Output (NVIDIA High Definition Audio)        | `{0.0.0.00000000}.{c764100f-e11e-4000-9a46-0a30641ba5d5}` | NotPresent |
| Output    | Digital Audio (HDMI) (High Definition Audio Device) | `{0.0.0.00000000}.{c88a30b6-1d57-4e9d-b3db-c28d2377ba64}` | NotPresent |
| Output    | NVIDIA Output (NVIDIA High Definition Audio)        | `{0.0.0.00000000}.{ebb3af0b-0bb4-41e8-ba1a-3a4a85a74912}` | NotPresent |
| Output    | NVIDIA Output (NVIDIA High Definition Audio)        | `{0.0.0.00000000}.{fd9842db-e749-4cea-809d-b2e4453b272c}` | NotPresent |
| Input     | Line (Realtek USB2.0 Audio)                         | `{0.0.1.00000000}.{1a04772d-9b95-4d5f-b8df-1d5c6b40b3f5}` | Unplugged  |
| Input     | Внутренний разъем  AUX (Steam Streaming Speakers)   | `{0.0.1.00000000}.{21459ac4-5372-48c8-804c-f4869d9deccd}` | Disabled   |
| Input     | Microphone (Realtek USB2.0 Audio)                   | `{0.0.1.00000000}.{22fdf83f-97ed-47a9-be2e-bd4fbcfdd2e9}` | Unplugged  |
| Input     | Line (Steinberg UR12 )                              | `{0.0.1.00000000}.{5776a171-f29c-46fc-92a3-eb15bb809aa3}` | Active     |
| Input     | Analog Connector (Realtek USB2.0 Audio)             | `{0.0.1.00000000}.{6869d302-78aa-448a-8f3d-b467946a0ef2}` | Disabled   |
| Input     | Микрофон (Steam Streaming Microphone)               | `{0.0.1.00000000}.{726a4f6a-88a5-4b08-ae3b-266886ed100a}` | Active     |
| Input     | Линия (2- Steinberg UR12 )                          | `{0.0.1.00000000}.{a412701c-53c0-4d88-a7ac-f19763f05465}` | NotPresent |
| Input     | Микрофон (Steam Streaming Speakers)                 | `{0.0.1.00000000}.{fbfc688a-127a-40af-82c6-1cadaa053e94}` | NotPresent |

For **Console, Multimedia and Communications**, output default is
`{0.0.0.00000000}.{af417ee0-95bf-4faf-ac43-7739c2bb6304}` (Line (Steinberg UR12 )),
and input default is `{0.0.1.00000000}.{5776a171-f29c-46fc-92a3-eb15bb809aa3}`
(Line (Steinberg UR12 )); both Active. All six resolutions Available.
Other real active outputs were listed only; no unfamiliar device was audibly played.

## Selected output and failure observations

Explicit `endpoint-session` invocation used only the observed UR12 output ID. It warned before sound
and authored one second of mono 44.1 kHz float32 220 Hz sine (amplitude 0.02), sampler gain **0.05**.
The canonical fixture deliberately imports/creates its music before baseline capture; subsequent
selection, playback/fault/join/reinitialization preserve identical canonical JSON, generation and Undo.
Only the exclusively created UUID fixture directory is removed after join/release; no user files.

Actual queried output: **44,100 Hz, 2 channels, float32, 10 ms period, 970-frame capacity**,
clock frequency 352800, MMCSS true. Reported stream-latency field 0 is not measured DAC latency.
Each lifetime starts explicitly, observes Stop 2 acknowledged 2, changes input intent to the real UR12
capture ID while output remains open, changes output intent to default Multimedia (join/release),
then restores explicit UR12 intent before the next deliberate open. Iteration 1 injects device
invalidation, **0x88890004**, and iteration 2 proves fresh reinitialization after that failure.
Intent change affects an active stream lifetime; seamless cross-device playback is not attempted.

| Iteration | Native result       | Service callbacks | Stop/ack | States created/released | Render peak  |
|-----------|---------------------|-------------------|----------|-------------------------|--------------|
| 0         | None / 0x00000000   | 17                | 2/2      | 1/1                     | 0.0009999998 |
| 1         | Native / 0x88890004 | 17                | 2/2      | 1/1                     | 0.0009999998 |
| 2         | None / 0x00000000   | 17                | 2/2      | 1/1                     | 0.0009999998 |

All three pass with joined output, canonical integrity and zero live states, processor/service
allocated bytes, service/packet misses, padding exhaustion and nonfinite samples within enabled
instrumentation boundaries. This is short functional evidence, not new stress/latency guarantees.

Missing explicit output resolves Missing with no output open and no fallback. Direct native open of
NotPresent `2769M (NVIDIA High Definition Audio)` ID
`{0.0.0.00000000}.{03dc63e5-60e0-4f60-b88d-1a16948e5319}` returns EndpointUnavailable.
Attempting to render through capture `Line (Realtek USB2.0 Audio)` ID
`{0.0.1.00000000}.{1a04772d-9b95-4d5f-b8df-1d5c6b40b3f5}` returns InvalidEndpoint before
activation. Both join, have no Facts or callbacks, and start no stream.
Three additional default-role output opens pass without Start, each with the same observed UR12
facts and zero callbacks. Final harness Accepted true, exit 0, CaptureOpened false, RecordingTested false.

Physical Disabled/Unplugged input states are discovery observations, not manipulated OS preferences.
Default disappearance, disabled explicit output, direction independence and failed join are covered
deterministically. No actual device removal/default change, second-device playback, unsupported real
format, microphone stream, physical Stop click or clock synchronization test is claimed.

## Verification and reproduction

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll endpoints
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll endpoint-session "{0.0.0.00000000}.{af417ee0-95bf-4faf-ac43-7739c2bb6304}"
```

Locked restore and full Release solution build pass with **zero warnings/errors**.
MTP/xUnit suite: **274 passed, zero failed/skipped**, retaining all 251 baseline tests.
Tests cover independent input/output intent, duplicate names/distinct IDs, role changes/fixed IDs,
missing/disabled/not-present/unplugged/unknown/contradictory-state refusal, default absence/query failure,
invalid IDs/roles/snapshot identity, frozen results, no canonical changes, new processor after join,
input failure without output teardown, thrown/unconfirmed join retaining ownership and Stop integrity.
One hundred Process packets leave discovery-call count unchanged; source review confirms the native
enumerator/property calls occur only at control discovery or pre-activation, outside the service loop.

No 60-second F2 matrix rerun: scheduler/DSP/PCM and callback/service processing are unchanged.
New blocking enumeration is control-side; native direction/state validation happens before activation.
Existing [F2 full workload evidence](SEQ-R2-F2_REPORT.md), [F3 regressions](SEQ-R2-F3_REPORT.md),
full deterministic suite and focused F4 selected-device lifetimes cover this bounded change.
No lower-period, DAC latency, multi-hour stability, actual hardware loss, native-engine comparison,
Linux/macOS runtime, clean packaged distribution or full GUI/DAW acceptance follows.

Measured Release artifact SHA-256:
Core `29B00419900637CCFF4EC5BC3F905C4F66453B892353CD46C736898E58DC687D`;
Windows adapter `8D3BAC1D281CAE05710AC4C450C145379DF6E3DFA8382699C24851513FB18826`;
DeviceCheck `8E7084F4EC64E02C378F6921EFCCE0E08558E492E5369BB90F827D654DB3A91A`.

## Complete R2 requirement audit

The completed [R2 scope](../archive/ROADMAP.md#seq-r2--audio-resource--device-foundation) is audited
together with current Roadmap's execution, discovery, media-integrity and platform ownership:

| Actual R2 requirement                                                                                      | Implementation/evidence                                                                                                                                    | Disposition                                                                                                        |
|------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| Audio resources, supported WAV import, managed durable media and source independence                       | F1 import/Save As/reopen/integrity/history plus F3 reuse/source removal; original source disappearance does not break accepted bytes                       | Implemented, locally accepted-ready for declared PCM16/float32 mono/stereo 44.1/48 kHz bounds                      |
| Simple pitched sampler, intensity, note duration/release and overlap                                       | Shared F1/F2 kernel, independent numerical/timing/voice oracles, 1/4/8 voices                                                                              | Implemented, locally accepted-ready for declared source/Pattern scope                                              |
| Basic scheduler/transport and audible output sufficient to exercise sampler before R3                      | F2 variable packets, loop/event ordering, Stop/Panic, epochs, preparation/handoff/retirement, physical workload/pressure; F4 selected UR12 lifetimes       | Implemented, locally accepted-ready; broader device/period/clock evidence limited                                  |
| Minimum external/project find, transient audition, explicit import/use and accepted-resource reuse         | F3 one-level source discovery, raw preview without import/edit, independent sound reuse; physical preview checks                                           | Implemented, locally accepted-ready for API/controlled-device foundation; full Browser/catalog/contextual UI later |
| Resource lifetime for working/saved/history/pending/live ownership, durable acceptance and partial failure | F1 durable media and frozen leases; F2/F3/F4 bounded preparation/retirement/join and integrity tests                                                       | Implemented, locally accepted-ready; real crash/power-loss, recovery and GC/repair wider evidence remain           |
| Backend-independent logical input/output/device foundation informed by R0                                  | Portable Core intent/resolution/lifetime; narrow Windows all-state discovery/role defaults and actual worker validation                                    | Implemented, locally accepted-ready; no final backend or universal stream-capability claim                         |
| Separate logical input/output selection and availability                                                   | F4 independent default/explicit intent, no fallback, missing/disabled/default failure semantics, real capture listing/selection, joined output replacement | Implemented, locally accepted-ready; input selection proves no capture/monitoring support                          |
| Plan MIDI input and capture ownership without requiring recording/all backends                             | Audio owner separates MIDI messages/timestamps/lifetime, input buffers/channel/clock/lifetime and deliberate durable recording acceptance                  | Required ownership plan present; no MIDI/capture stream implementation claimed                                     |
| Validate implemented adapters honestly while keeping project contracts portable                            | Full Windows local checks and actual selected output/input observations; IDs absent from canonical state, one-way Windows-to-Core references               | Implemented within Windows x64 scope; Linux/macOS and delivered distribution not accepted                          |

**Decision: overall SEQ-R2 complete / local accepted-ready** for its current bounded foundation.
There are **no genuinely missing R2 prerequisites for scoping R3**. R3 remains not started and requires
its own authorization; no extra milestone is invented to defer acceptance. Source/backend replacement
remains possible; input/recording, seamless migration, ASIO/MIDI, graph/plugin, full catalog/UI/export/
recovery and release-platform acceptance are not promoted into implemented capabilities.

## Open questions after acceptance

Q-001/Q-002: portable control ownership and OS adapter remain replaceable; no permanent managed/native
engine, language/backend or distributed ABI choice.
Q-003: independent selection and confirmed-join replacement add bounded lifetime evidence; broader
races, real removal and graph/plugin ownership remain.
Q-004: existing Stop/Panic ordering and acknowledgment persist; general event admission/multi-producer
and recovery policies remain.
Q-005: no capture/output clock alignment, drift or tempo-map/recovery inference; faults require fresh lifetime.
Q-006: shared PCM/offline path unchanged; wider DSP/graphs/plugins/cross-platform parity remain.
Q-007: short selected-output checks add no lower-period/DAC/multi-hour or universal device guarantee.
Q-026 remains future ASIO/additional backend design and licensing.
Q-027 remains audio/MIDI capture placement, monitoring/alignment/compensation; enumeration supplies no timestamps.
Q-062's bounded independent discovery/selection gap is filled; actual hardware/default changes, notifications,
automatic recovery/reselection UX and future input/recording lifetime remain open.
Q-069 now explicitly separates session intent, snapshot facts and actual opened endpoint; wider negotiation,
input channels/clocks, synchronization, rate adaptation and other backend/platform evidence remain open.

Contracts stay in [Audio](../AUDIO_ENGINE.md#r2-f4-logical-endpoints-and-independent-selection),
[Architecture](../ARCHITECTURE.md#r2-f4-endpoint-and-session-ownership) and [Settings](../SETTINGS.md).
