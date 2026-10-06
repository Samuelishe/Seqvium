# Test execution and verification

Role: Proportional verification, test quality, and evidence guide.
Read when: Planning, running, or interpreting verification and future tests.
Authoritative for: Verification scope, future test commands/platform discovery, test quality, Release gates, and evidence tiers.
Not authoritative for: Product/subsystem contracts, SDK selection, CI triggers, supported release platforms, or current progress.

## Current and future execution

No test projects, test framework, or executable verifier exists. There are no test commands to run and
the empty `Seqvium.sln` must not be built for documentation verification. Check links/anchors,
metadata, ownership/routing, decision status, scope, passive configuration, and Git diffs/status under
[AGENTS](../AGENTS.md) and [DOCUMENTATION_GOVERNANCE](DOCUMENTATION_GOVERNANCE.md).

When tests exist, inspect the actual SDK, test platform (VSTest or Microsoft.Testing.Platform), framework,
and project setup before recording exact build/test/filter/report commands here. Do not invent commands
or select packages through this document.

Start with focused checks covering the affected behavior. Broaden for shared contracts, consumers, and
risky boundaries such as publication, serialization, native interop, or resource lifetime. Do not repeat
already sufficient green checks without a new change, failure, or unresolved concern. Once a suite exists,
full Release verification is required before claiming milestone-level completion. Supported production/test
builds must have zero warnings and errors under [CODING_GUIDELINES](CODING_GUIDELINES.md); enforcement
begins with actual projects. Integration/runtime/manual evidence supplements pure tests where needed.

## Test quality

- Use deterministic assertions and temporary/project-generated data where possible; avoid mutable
  developer-machine state. Explicitly own fixtures, tasks, resources, cleanup, and lifecycle.
- Async tests use `await`, meaningful cancellation, and deterministic synchronization. Do not use
  `.Result`/`.Wait()` or arbitrary sleeps when a deterministic signal/barrier is possible.
- Preserve failure semantics; do not weaken assertions for green output. Functional correctness and
  timing/platform acceptance are distinct claims.
- Prefer device-independent offline tests for deterministic DSP/signal/scheduling logic. Test declared
  numerical tolerances rather than assume bit identity across arbitrary processors/platforms/plugins.
  Physical-device latency/crackle acceptance remains separate.
- Automate pure UI layout/state logic where useful. Actual pointer interaction, DPI, windowing,
  rendering integration, and audio-device feel may require declared runtime/manual evidence.

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

Hosted CI can prove compilation, offline DSP, serialization, graph scheduling, native loading/interop,
and packaging contracts when those checks exist. It cannot alone prove stable real-device low latency,
ASIO quality, USB interface behavior, glitch-free playback under desktop load, microphone/input
monitoring quality, or user-perceived UI behavior on every desktop environment. Use actual runtime/device
evidence for those claims. [AUDIO_ENGINE](AUDIO_ENGINE.md) owns audio constraints and probe acceptance;
[PORTABILITY](PORTABILITY.md) owns platform claim boundaries.
