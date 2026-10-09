# Test execution and verification

Role: Proportional verification, test quality, and evidence guide.
Read when: Planning, running, or interpreting verification and future tests.
Authoritative for: Test topology/quality, verification scope, future commands/platform discovery, Release gates, and
evidence tiers.
Not authoritative for: Product/subsystem contracts, SDK selection, CI triggers, supported release platforms, or current
progress.

## Current and future execution

The R1 [Seqvium.Tests](../tests/Seqvium.Tests/Seqvium.Tests.csproj) project uses xUnit v3 3.2.2 with
explicit MTP v1 support; its lock file resolves Microsoft.Testing.Platform 1.9.1. This suitable setup
was available in the installed package cache and verified against SDK 10.0.401 / runtime 10.0.12;
no VSTest adapter, coverage tool or additional runner is introduced. SDK 10 MTP mode is selected in
[global.json](../global.json). From the repository root:

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --filter-class "*MusicalTimeTests"
```

Use MTP arguments directly, without a `--` separator. xUnit filtering uses `--filter-class`,
`--filter-method` or `--filter-trait`, not VSTest's `--filter`. Optional built-in xUnit TRX output uses
`--report-xunit-trx --results-directory <directory>`; no generic TRX/coverage extension is installed.
The suite exercises document lifecycle, typed identities/sharing, variations, sound independence,
transaction rejection/deletion/history, processing relationships, exact time conversions, format
compatibility/unknown data (including known-field edits), net-zero history, degraded access and
normal-process save failures. Fixtures are synthetic
and files use owned temporary directories. The locked-file overwrite check is Windows-specific;
the directory-target failure and canonical checks are portable. There are no timing sleeps.
F1 adds `WavDecoderTests`, `ManagedMediaTests`, `OfflineSamplerTests` and the independent tiny
`WavFixtures` writer. They cover supported/extensible PCM/float WAV, all truncated prefixes/chunk bounds,
durable unnamed acceptance/source removal, Save As/reopen/relocation, actual integrity/degraded Save,
pending/stale/Undo/close/cancel gates, interrupted reads/storage blockers and partial transfer failures.
Async tests use explicit completion gates and cancellation, without sleeps. DSP tests use independent
linear-ramp/envelope/rational timing oracles (absolute float tolerance 2e-6), 1/4/8 voices, independent
release, equal-frame conflicts, zero-frame notes, partition/loop invariance, root/fractional pitch,
44.1/48 kHz and 120/137 BPM. Capacity/dependency failures must occur before output. This is local
functional evidence, not device timing, real disk-full/power-loss, process-crash or cross-platform proof.
Current local totals and evidence limits belong to [PROJECT_STATE](PROJECT_STATE.md).

F2 adds `RealtimeSamplerTests` for shared offline/packet PCM and independent ramp/release/rational oracles,
1/4/8 voices, Stop/Start command order, sticky Panic under gain bursts, stale/cancelled/closed/removed-target
acceptance, transition-generation rejection after Undo/Redo, pending/retired backpressure and preparation
candidate capacity, packet refusal, explicit termination and lease release. Per-thread allocation checks
surround warmed Process/control entry only; native/runtime-entry evidence is separate.
The [physical harness](../tools/Seqvium.DeviceCheck/README.md) runs explicitly in Release, never as an
ordinary test or a hardware-dependent skip. Its pre-output capture is compared to offline execution at
the same actual partitions and to an independent source/interpolation/envelope sum. Baseline and pressure
budgets/fault policy are predeclared in the [F2 protocol](experiments/SEQ-R2-F2_PROTOCOL.md);
results and exact allocation boundaries are in the [report](experiments/SEQ-R2-F2_REPORT.md).
No hardware removal, DAC measurement or hosted CI result follows from injected errors or PCM comparison.

F3 adds SourceAccessTests, WavAuditionTests and WavResourceReuseTests. Deterministic tests cover bounded
one-level discovery/truncation/diagnostics, Windows exclusive-file refusal, unavailable external versus
degraded accepted media, no canonical/storage preview side effects, independent temporary leases,
rate/interpolation/EOF, Stop/live cancellation/Close/fault/supersession, in-flight gates and capacity,
source-specific Start under retirement backpressure, explicit import/source deletion, one resource
with independent sound/part uses, Undo/Redo and Save As/reopen. No physical device opens during tests.
Warm preview allocation checks measure Process only, not general runtime/native costs.
The explicit `audition-smoke` Release command warns about sound and uses gain 0.05 with low-amplitude
authored PCM. It checks both diagnostics-off and opt-in capture, with a separate independent raw-source
oracle and joined release. [F3 evidence/audit](experiments/SEQ-R2-F3_REPORT.md) distinguishes short smoke,
post-change pressure, historical full F2 evidence and unmeasured acoustic/platform/device behavior.

F4 adds `AudioEndpointTests` with injected immutable discovery results: independent direction/intent,
duplicate names/distinct IDs, role-following versus fixed IDs, default disappearance/query failure,
missing/inactive/wrong-direction refusal, invalid selection, frozen observations and no project edit.
Real Core preparation/retirement is exercised with only a borrowing/join boundary stub, never fake WASAPI.
Changing output preserves processed Stop acknowledgments, does not invent unprocessed acknowledgments,
joins before state release and retains ownership after thrown or unconfirmed join. Fresh attachment
and input failure leave unrelated output valid. A discovery-call counter stays unchanged through
100 Process packets; source review additionally verifies native enumeration is outside the service loop.
`endpoints` is silent physical discovery. Explicit `endpoint-session <ID>` warns and uses gain 0.05;
it validates selected output/Stop/join/reopen and injected failure, not microphone capture. The
[F4 report and complete R2 audit](experiments/SEQ-R2-F4_REPORT.md) retains results and evidence limits.
The full F2 performance matrix is unnecessary for F4: scheduling/DSP/service processing did not change;
new enumeration is control-side and native validation precedes stream activation. Bounded selected
physical lifetimes and the full deterministic suite complement existing F2/F3 workload evidence.

The standalone [SEQ-R0 assertion harness](../experiments/seq-r0/README.md) remains independent:
`dotnet bin/Release/net10.0/Probe.dll verify` after `./build.ps1` in `experiments/seq-r0/`.
It is not the production test suite. Documentation verification checks links/anchors,
metadata, ownership/routing, decision status, scope, passive configuration, and Git diffs/status under
[AGENTS](../AGENTS.md) and [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md).

On setup changes, inspect SDK, platform, framework and manifests before changing commands.

SEQ-R0 device/timer/lifetime commands and exact scope are in the
[probe README](../experiments/seq-r0/README.md); measured results belong to the
[report](experiments/SEQ-R0_REPORT.md). Release builds treat warnings as errors. Its oracle checks
ordered absolute events, partition independence, generated float output, critical controls, skipped
time, prepared-state capacity/publication and ownership. Actual device callbacks and offline captures
are separate from timer-driven evidence. This does not establish plugin/graph/UI/recording or release
acceptance, and no hosted checks are introduced.

## R3-F1 host verification

`Desktop/HostPresentationTests` and `PreferenceStoreTests` add 34 cases to the 274-case R2 baseline.
They verify stable catalogs and per-entry/essential fallback, both semantic palettes and contrast,
canonical snapshot/generation/history/name/numeric integrity, acceptance of actual previously prepared
WAV work after preference changes, shell create/close, ordered preference commands, all four persisted
combinations, missing/corrupt/oversize/write-failed settings and preservation of unrelated user content.
They reference Desktop but never initialize the platform or open a physical window/audio stream.
Use the existing full locked Release commands; `--filter-class "*HostPresentationTests"` selects host
presentation cases when needed. The [F1 report](experiments/SEQ-R3-F1_REPORT.md) separately records actual
Windows UI Automation/pointer/keyboard smoke and visual inspection. Compilation/pure tests are not GUI
acceptance; high/mixed DPI, screen readers and Linux/macOS runtime remain missing evidence.

## R3-F2 workspace verification

`Desktop/WorkspaceStateTests` and `WorkspaceLayoutTests` initially added 38 deterministic cases.
The geometry/chrome correction adds 50 cases (396 at that checkpoint). The resize-target/boundary
correction adds 32 `WorkspaceBoundaryTests` cases, for **428 total**, retaining all 396 baseline cases.
The initial tests cover stable identity/localization, bounded front order, overlap
activation, floating/docked geometry and reflow, collapse/hide/reopen, traversal/disposal, per-pane docking
permission/conflicts, restored dock intent, early-close snapshots, bounded/partial/corrupt/future files,
roundtrip/backup/write failures and latest-pending shutdown. Actual canonical Save, prepared WAV acceptance
and preference changes verify ownership independence. These tests initialize no platform or physical audio.

`PaneGeometryTests` asserts exact edge/corner anchors, repeated reversibility, one-pixel reversal after
boundary/minimum saturation, dock width limits and retained floating bounds, rollback and compact hit
regions. `PaneChromeTests` checks actual Button/nested-visual source exclusion before capture, without
platform initialization. Additional layout tests verify independent dock width and legacy version-1 reads.
Localized close labels and keyboard hints are checked separately from actual control accessibility.
`WorkspaceBoundaryTests` exercises inward/outward tolerance, corner priority, distance/front-order ties,
touching visible sides, covered/invisible boundary exclusion, control sources, inactive direct resizing,
gesture stability after reordering and current independent dock seams. These display-independent cases
do not initialize Avalonia's platform or claim native cursor/capture correctness.
Use the same locked restore/full Release commands.
The [F2 correction](experiments/SEQ-R3-F2_REPORT.md#geometry-and-chrome-correction)
records 359 actual Windows input/cursor checks, retained content identity/focus and inspected local captures.
The original 58 checks missed the owner's reproducible resize/cursor defects; they do not establish
interactive acceptance. Pure tests do not establish control focus, pointer capture, visual or OS acceptance.
[Boundary correction evidence](experiments/SEQ-R3-F2_REPORT.md#resize-hit-targets-and-boundary-arbitration)
records real Windows Release tolerance, overlapping/meeting boundary, source-route cursor and focus checks.
High/mixed DPI, screen-reader certification, Linux/macOS and crash/power-loss guarantees remain untested.

## Intended test topology

Prefer **one main managed `Seqvium.Tests` project**, organized by domain/feature folders. Conceptual
examples are `Domain/`, `MusicalTime/`, `Patterns/`, `Serialization/`, `NodeGraph/`, `Resources/`,
`Extensions/`, `ProjectStats/`, and `Support/`. These are examples, not required empty folders;
the R1 suite uses cohesive feature files. Initial framework/platform selection Q-039 is resolved;
future materially different execution hosts need separate evidence.

Do not create a test project per production subsystem. Add a separate project only for a materially
different execution contract: native runtime/materialization, another target framework/platform, a UI
integration host, out-of-process plugin isolation, or materially different dependencies/lifecycle.
An architecture folder alone does not justify a project. This starting preference may evolve with evidence.

Future [ProjectStats](PROJECT_STATS.md#test-strategy-and-placement) tests initially belong in
`Seqvium.Tests/ProjectStats/`, using synthetic temporary repositories. `InternalsVisibleTo` may be used
if justified. A standalone tooling project does not itself justify a standalone tooling test project.

## Future test categories

Categories describe required kinds of evidence as their owning capabilities arrive, not installed suites.

| Category                    | Bounded scope / method                                                                                                                                   |
|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| Pure/domain                 | Musical time, events, shared patterns/variations, IDs, undo/redo, serialization model, graph validation                                                  |
| DSP/offline                 | Generated impulse, sine, fixed-seed noise, bounded synthetic buffers; numerical behavior with declared tolerances, no physical device                    |
| Scheduler/transport         | Virtual/offline time; block/loop boundaries, event ordering, note start/end, seek/start/stop, tempo transitions when introduced; avoid wall-clock sleeps |
| Graph contracts             | Valid topology, branches/merges, invalid type connections, missing nodes, prepared publication, state preservation                                       |
| Serialization compatibility | Load/edit/save/reload, migrations, stable identities, unknown/missing plugin state, graph relationships, shared resources versus placements              |
| Plugin compatibility        | Later host/plugin version combinations, missing optional capability, incompatible required capability, disabled plugin, opaque state preservation        |
| UI logic                    | Pure geometry/state independent from UI framework/runtime where practical                                                                                |
| Runtime/manual UI           | Pointer feel, DPI, docking, animation, real windowing; unit tests alone do not establish these                                                           |
| Physical audio-device       | Separate actual device/backend/sample-rate/buffer evidence; hosted/offline success does not establish device acceptance                                  |

Subsystem contracts remain in [AUDIO_ENGINE](AUDIO_ENGINE.md), [NODE_GRAPH](NODE_GRAPH.md),
[PROJECT_FORMAT](PROJECT_FORMAT.md), [EXTENSIONS](EXTENSIONS.md), and [WORKSPACE](WORKSPACE.md).

## Test quality

- Prefer generated audio, project-authored tiny fixtures, deterministic event streams, fixed seeds,
  and temporary directories. Explicitly own fixtures, tasks, resources, cleanup, and lifecycle.
  Do not depend on developer music libraries, personal samples, commercial songs, downloaded packs,
  or mutable machine configuration. Tracked audio/MIDI/image/font/test assets follow
  [THIRD_PARTY](THIRD_PARTY.md) provenance; document generated-fixture creation when useful.
- Async tests use `await`, meaningful cancellation, and deterministic synchronization; avoid
  `.Result`/`.Wait()`. Arbitrary `Thread.Sleep`, `Task.Delay`, polling delays, and timing luck are not
  the normal proof of concurrency. Prefer fake/virtual clocks, explicit schedulers,
  `TaskCompletionSource`, barriers/latches/channels, deterministic callback harnesses, or explicit
  publication signals. Tests of timing itself must declare tolerances/environment and must not
  masquerade as deterministic unit tests.
- Preserve failure semantics; do not weaken assertions for green output. Functional correctness and
  timing/platform acceptance are distinct claims.
- Prefer device-independent offline tests for deterministic DSP/signal/scheduling logic. Test declared
  numerical tolerances rather than assume bit identity across arbitrary processors/platforms/plugins.
  Physical-device latency/crackle acceptance remains separate.
- Automate pure UI layout/state logic where useful. Actual pointer interaction, DPI, windowing,
  rendering integration, and audio-device feel may require declared runtime/manual evidence.

For critical scheduling, serialization, compatibility, or lifetime regressions, optional negative-proof
evidence can deliberately inject a representative fault and confirm that the owning test fails.
The purpose is test sensitivity, not a mutation-score metric or universal gate. No mutation framework
is introduced now.

## Focused, full local, and hosted workflow

- **Focused:** during implementation, run affected tests and direct contract/lifecycle neighbours.
  Broaden for shared contracts/consumers and risky publication, serialization, interop, or lifetime boundaries.
- **Full local:** before meaningful milestone/task handoff while the suite remains reasonably fast,
  run a Release build and the normal full suite with zero warnings/errors under
  [CODING_GUIDELINES](CODING_GUIDELINES.md). Enforcement begins with actual projects.
- **Hosted:** after an authorized push, inspect the actual enabled Windows/Ubuntu/macOS checks and
  their revision/scope under [CI_CD](CI_CD.md); a push alone is not successful hosted evidence.

Do not repeat sufficient green checks without a new change, failure, or unresolved concern. Runtime/manual
evidence supplements tests where needed. If the suite becomes materially expensive, justify named routes
and fast/full separation from measured cost, preserving full acceptance scope. A single route registry
is retained as deferred [I-002](IDEAS.md#i-002--named-test-routes); no filters/route infrastructure exists now.

## R4-F1 graph verification

`Graphs/GraphEditingTests`, `GraphDiagnosticTests`, `GraphPersistenceTests` and the synthetic
`Support/GraphFixture` exercise canonical Core APIs in the existing Tests assembly. Cases cover typed
global identity collisions, atomic context/graph creation, Source coverage including silent parts,
same-name/sound/resource independence, item placement identity, supported ports/cardinality/fan-out,
deterministic topology diagnostics, all directed cycle classes and incompatible/unknown declarations.
Variation remapping, explicit ownership deletion, retained unresolved foreign dependencies and R1
referenced-owner refusal are checked together with Undo/Redo, net-zero collection/JSON reconstruction,
ended/reentrant/deleted-target operations and failure immutability.

Placement-level downstream routing blocks preparation for otherwise eligible single-part and mixed
item graphs. Real edits, Save/reopen and Undo/Redo preserve same-name distinct route identities;
the graph without its own downstream route remains eligible even when another placement is routed.

Persistence cases remove graph fields to reproduce graph-free 1.0 shape, retain precise Int64 ticks,
round-trip every graph boundary/coordinate and unknown/opaque state, and Save incomplete/cyclic graphs.
Compatibility fixtures freeze the actual baseline 1.0 envelope refusal predicate, verify graph-required
minor 1 and underdeclared refusal, higher compatible minor retention and nondecreasing requirements.
This is an envelope contract fixture, not a separately built historical reader. Existing version-refusal
cases now target required minor 2; all baseline test cases remain. Malformed identities/fields/ownership,
document/port/processing bounds, JSON depth/lifetime and failed Save retaining prior file/state/history
are deterministic and use owned temporary directories.

Run the unchanged locked restore, Release build and full solution test commands above. Optional focused
run: `dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "Seqvium.Tests.Graph*"`.
These tests open no GUI, audio device or network and establish intent eligibility only. Media decode,
prepared graph resources, PCM separation/oracles, realtime budgets/publication and actual canvas/device
behavior require separately authorized F2/F3 evidence. Current totals belong to PROJECT_STATE.

## R4-F2 execution verification

`GraphExecutionTests`, `GraphRealtimeTests` and `GraphLifecycleTests` add 42 cases without modifying
the 558 prior assertions. Focused execution after a Release build:

```text
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*GraphExecutionTests" --filter-class "*GraphRealtimeTests" --filter-class "*GraphLifecycleTests"
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*WavAuditionTests" --filter-method "*WarmPreviewProcessorAllocatesNothingAcrossVariablePackets*"
```

The authored WAV oracle reads canonical note intervals, absolute rational frame times and original PCM;
it calls neither prepared events nor production execution to derive expected samples. Tests compare
independent Kick/Snare, branch Gain, Mix and Output, release/EOF/hard Stop/repeats and stereo asymmetry
across rates/layouts/packet partitions. Branch swap, fan-out, same-resource independence, silent-source
media, unity Gain and geometry/node order are covered. A nonassociative three-input example asserts
exact port-UUID accumulation rather than toleranced/commutative equality. The largest numerical workload
uses 32 nodes, 8 sources/voices, 22 Gain, 8-input Mix and a 65,536-frame stereo packet. Five unique large
PCM16 silent dependencies verify actual decoded-budget refusal before execution.

An injected serialized owner context and explicit completion/cancellation gates exercise latest Edit/
Undo/Redo, ABA, target/Close, retained capacity retry without reprepare, invalid canonical/repair,
no last-valid resurrection after Save/reopen, Gain/release cursor continuity, finite topology ramp
including in-packet EOF, Stop/Panic, failed join retention and eventual PCM collection. Normal tests
open no physical output and use no timing sleeps. Warm allocation measures Process/status entry only,
after separate warm-up, with harness arrays/fixture setup outside the measurement. The existing preview
allocation case passes five isolated consecutive runs as well as full-suite runs; assertions are unchanged.

Explicit Windows evidence extends the existing DeviceCheck:

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll graph-measure 60
```

It warns before quiet authored output, uses the actual 32-node/8-source attachment and negotiated
format/capacity, bounded diagnostics/capture, independent arithmetic versus captured PCM and offline
packet parity, deadline/allocation/GC observations, owner convergence under pressure, hard Stop/Panic
and joined injected fault lifetimes. The [report](experiments/SEQ-R4-F2_REPORT.md) owns actual duration,
endpoint, packet distribution, limits and retained measurements. Pressure's dynamically edited capture
is not relabeled as steady-state oracle evidence; its oracle/parity fields are null. Diagnostic zeros
are evidence only when the corresponding diagnostics mode was enabled.
Full F2 pressure acceptance remains partial: passing runs coexist with the original two non-injected
starvation failures and further failures in the diagnostic continuation below. Callback allocation/
deadline counters alone do not cover missed service wakes.

Execution-identity correction adds nine deterministic cases while preserving all 600 existing tests:
two valid attachments prepared from one document revision, rejected pending target, repair and PCM/
provenance/convergence with 1/2/3 Gain versus A's two Gain, both playing and sticky stopped. Same-attachment
reprepare/Undo/Redo/ABA, pending target changes, explicit rejected-handoff retry without a canonical edit,
finite retry and both publication/consumer identity gates are covered. None uses a device or timing sleeps.
The [diagnostic variants](../tools/Seqvium.DeviceCheck/README.md#r4-f2-starvation-attribution-variants)
separate CPU, allocations, canonical edits, forced collections and graph-free legacy execution on the
same physical path. Their native GC/Suspend/Restart observations and bounded service windows explain
individual intervals; aggregate wake/GC totals alone remain insufficient for causal attribution.

## Evidence tiers

These are complementary evidence classes, not a ranking in which a higher-looking tier replaces lower
semantic tests. Record source revision, environment, input/configuration, result, and material limits.

| Evidence                 | Proves within its declared scope                                                                                                                            |
|--------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Unit / pure              | Bounded deterministic logic                                                                                                                                 |
| Integration              | Controlled subsystem interaction                                                                                                                            |
| Hosted cross-platform CI | Build/test/runtime boundaries on hosted Windows/Linux/macOS environments                                                                                    |
| Native interop           | Selected native build/materialization/loading and production interop for a declared platform/RID                                                            |
| Local/manual runtime     | Actual application interaction on a declared real desktop/audio environment                                                                                 |
| Hardware/audio-device    | Behavior with a declared real interface/device/backend and settings                                                                                         |
| Delivered distribution   | Identified packaged artifact installation/materialization, launch/dependency loading and applicable update/removal behavior in a declared clean environment |

Hosted CI can prove compilation, offline DSP, serialization, graph scheduling, native loading/interop,
and packaging contracts when those checks exist. It cannot alone prove stable real-device low latency,
ASIO quality, USB interface behavior, glitch-free playback under desktop load, microphone/input
monitoring quality, or user-perceived UI behavior on every desktop environment. Use actual runtime/device
evidence for those claims. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns audio constraints and probe acceptance;
[PORTABILITY](PORTABILITY.md) owns platform claim boundaries.

Interpret runner families separately: Windows restore/Release build/tests do not establish actual
Windows desktop/device operation; Ubuntu build/offline checks do not cover all Linux desktops/audio
environments; macOS managed/native compilation does not establish a release-ready desktop application.
Checks prove only behavior actually exercised. Record WSLg runtime/GUI/bridged-audio evidence separately
from native Linux acceptance under [Portability](PORTABILITY.md#desktop-and-delivered-environment-acceptance).
Release claims combine the required evidence dimensions under
[release policy](PORTABILITY.md#evidence-led-release-policy), rather than treating any one tier as sufficient.

Useful optional status labels are **local accepted-ready** (declared local checks passed), **hosted-confirmed** (hosted
checks actually succeeded at a stated revision/platform scope), **runtime/manual accepted** (bounded real application
interaction accepted), and **hardware/audio-device accepted** (bounded real device/backend/settings accepted).
Use them only with declared evidence and material limits, not as ceremony for trivial tasks.
`hosted-confirmed` never implies physical audio-device acceptance. Link detailed reports rather than
accumulating individual runs in [PROJECT_STATE](PROJECT_STATE.md).
