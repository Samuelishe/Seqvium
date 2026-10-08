# Test execution and verification

Role: Proportional verification, test quality, and evidence guide.
Read when: Planning, running, or interpreting verification and future tests.
Authoritative for: Test topology/quality, verification scope, future commands/platform discovery, Release gates, and evidence tiers.
Not authoritative for: Product/subsystem contracts, SDK selection, CI triggers, supported release platforms, or current progress.

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
Current local totals and evidence limits belong to [PROJECT_STATE](PROJECT_STATE.md).

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

| Category | Bounded scope / method |
| --- | --- |
| Pure/domain | Musical time, events, shared patterns/variations, IDs, undo/redo, serialization model, graph validation |
| DSP/offline | Generated impulse, sine, fixed-seed noise, bounded synthetic buffers; numerical behavior with declared tolerances, no physical device |
| Scheduler/transport | Virtual/offline time; block/loop boundaries, event ordering, note start/end, seek/start/stop, tempo transitions when introduced; avoid wall-clock sleeps |
| Graph contracts | Valid topology, branches/merges, invalid type connections, missing nodes, prepared publication, state preservation |
| Serialization compatibility | Load/edit/save/reload, migrations, stable identities, unknown/missing plugin state, graph relationships, shared resources versus placements |
| Plugin compatibility | Later host/plugin version combinations, missing optional capability, incompatible required capability, disabled plugin, opaque state preservation |
| UI logic | Pure geometry/state independent from UI framework/runtime where practical |
| Runtime/manual UI | Pointer feel, DPI, docking, animation, real windowing; unit tests alone do not establish these |
| Physical audio-device | Separate actual device/backend/sample-rate/buffer evidence; hosted/offline success does not establish device acceptance |

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

## Evidence tiers

These are complementary evidence classes, not a ranking in which a higher-looking tier replaces lower
semantic tests. Record source revision, environment, input/configuration, result, and material limits.

| Evidence | Proves within its declared scope |
| --- | --- |
| Unit / pure | Bounded deterministic logic |
| Integration | Controlled subsystem interaction |
| Hosted cross-platform CI | Build/test/runtime boundaries on hosted Windows/Linux/macOS environments |
| Native interop | Selected native build/materialization/loading and production interop for a declared platform/RID |
| Local/manual runtime | Actual application interaction on a declared real desktop/audio environment |
| Hardware/audio-device | Behavior with a declared real interface/device/backend and settings |
| Delivered distribution | Identified packaged artifact installation/materialization, launch/dependency loading and applicable update/removal behavior in a declared clean environment |

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

Useful optional status labels are **local accepted-ready** (declared local checks passed),
**hosted-confirmed** (hosted checks actually succeeded at a stated revision/platform scope),
**runtime/manual accepted** (bounded real application interaction accepted), and
**hardware/audio-device accepted** (bounded real device/backend/settings accepted).
Use them only with declared evidence and material limits, not as ceremony for trivial tasks.
`hosted-confirmed` never implies physical audio-device acceptance. Link detailed reports rather than
accumulating individual runs in [PROJECT_STATE](PROJECT_STATE.md).
