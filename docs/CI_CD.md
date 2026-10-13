# CI/CD policy

Role: Hosted automation and eventual acceptance-gate policy.
Read when: Introducing or changing CI, documentation automation, distribution, or release validation.
Authoritative for: CI evolution, feedback versus full acceptance, workflow principles, and trigger-policy ownership.
Not authoritative for: Exact test commands, physical audio acceptance, SDK/package versions, or current hosted results.

## Current boundary and initial managed CI

No GitHub Actions workflow is configured. R1 production/tests and the independent SEQ-R0 experiment
have local verification; no hosted evidence or persistent documentation checker exists. R1 does not
authorize CI implementation.
Exact workflow names, triggers, actions/versions, path filters, and branch-protection rules remain
implementation choices when real validation exists.

When CI is explicitly authorized, keep ordinary feedback focused on meaningful code/project changes
and affected contracts. A Windows/Ubuntu/macOS matrix remains a possible source of portable evidence;
choose its scope and frequency from actual platform risk and measured cost.
The exact commands follow actual projects and [TEST_EXECUTION](TEST_EXECUTION.md); the zero-warning
requirement follows [CODING_GUIDELINES](CODING_GUIDELINES.md). Begin portable evidence early under
[PORTABILITY](PORTABILITY.md), without manufacturing tests for an empty solution.

If native dependencies are selected, add proportionate native build, package/materialize, smoke,
managed host build, and production interop evidence on actual target runner families where practical.
Runtime identity must be explicit; portable contracts remain backend-independent. Do not copy
library-specific reference scripts or preselect a native toolchain/RID matrix.

## R3-F1 hosted feasibility assessment

At R3-F1 the desktop was plain `net10.0` without a Windows audio adapter reference; platform detection and
guarded chrome avoid Windows-only build requirements. Avalonia uses package-native assets, not global
templates or desktop SDK workloads. Its RID-independent/no-apphost build passed on the local Windows
SDK; pure shell tests need neither a display nor physical audio. Therefore a pinned .NET 10 Windows/
Ubuntu/macOS restore/Release-build/managed-test matrix is technically plausible, but no hosted run,
runner availability or three-platform result is claimed. Tests now reference Desktop and restore its
native assets, though pure tests do not initialize Avalonia platform/rendering services.

No workflow or external service was introduced incidentally. Existing exact locked commands remain the
candidate job steps; inspect pinned SDK availability and OS native prerequisites before introducing jobs.
GUI smoke on Linux needs display/session setup and macOS requires an actual interactive environment;
those are separate evidence tiers, not prerequisites for every pure test job. Current Desktop also
references the guarded Windows adapter since R4-F3-A; revisit target prerequisites when introducing CI.
Hosted compilation must
never be labeled desktop/device or distribution acceptance.

## Documentation validation and later growth

Once a real documentation checker exists, make docs validation separable from heavyweight application,
native, or audio builds. Docs-only edits should not needlessly trigger expensive full application CI.
Account for GitHub required-check semantics: a workflow skipped by path filtering may leave a required
check pending. Decide filters and required-check policy together when CI exists; no such settings are
selected here.

Future CI must preserve the accepted [L0–L4 policy](TEST_EXECUTION.md#required-verification-levels):
ordinary feedback uses L0/L1 plus risk-selected L2; full automated L3 is periodic or explicitly justified,
and L4 runtime/native/stress/device checks are separately selected. The local ten-task cadence belongs
to PROJECT_STATE; future hosted triggers can implement practical scheduled/manual/checkpoint full
verification without counting commits as tasks. BUILD increments and ordinary commits are not full-suite
triggers. This task authorizes policy only, not workflows or services.

| Purpose         | Possible scope / gates                                                                                                                  |
|-----------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| Fast feedback   | Ordinary push/PR: focused/fast Windows validation where useful, portable managed validation, docs                                       |
| Full automated | Periodic/manual or justified milestone/release checks: full Release build and automated suite (L3) |
| Runtime acceptance | Separately selected native GUI, visual matrix, stress, physical device and distribution checks; L3 does not trigger L4 automatically |

A small project should have a small CI system. Fast feedback must not silently become full acceptance;
record the revision and scope of acceptance evidence. Measure real costs before implementing a
collection of specialized workflows. Packaging/runtime validation is a separate evidence class,
not merely another successful source build.

## Workflow correctness

Future workflows must fail closed, use least required permissions, avoid hidden reliance on developer
secrets, and reproduce SDK/tool setup from declared authority. Record platform/environment identity and
preserve useful bounded diagnostics for difficult runtime/native failures. Do not claim unsupported
platform guarantees. [DEVELOPMENT](DEVELOPMENT.md) owns persistent tool/version policy;
[THIRD_PARTY](THIRD_PARTY.md) records introduced service/action provenance.

Use concurrency cancellation for obsolete ordinary push/PR feedback where useful. Do not cancel explicit
milestone/release certification without reason. Trigger/action details must follow real checks and costs,
not mature reference-repository complexity.

## Hosted CI and physical acceptance

[TEST_EXECUTION](TEST_EXECUTION.md#evidence-tiers) defines the evidence model. Hosted matrix success
may establish compilation, tests, offline DSP, serialization, graph scheduling, native loading/interop,
and packaging contracts that were actually checked. It does not establish real low-latency audio-device
quality, ASIO/USB behavior, playback under desktop load, input monitoring, or every desktop's perceived
UI quality. Declare separate local/manual runtime and hardware/audio-device evidence for those claims.
