# Portability

Role: Cross-platform target and portable engineering boundary.
Read when: Choosing project targets, platform/device APIs, native distribution, or platform evidence.
Authoritative for: OS target direction, portable/shared versus platform-specific rules, and platform claim limits.
Not authoritative for: Release dates, runtime parity, selected RIDs/toolchains/backends, developer setup, or CI triggers.

## Product target and current evidence

**Seqvium targets Windows, Linux, and macOS.** Windows is the primary early development/runtime
environment. Linux and macOS are first-class architectural targets from the start, not accidental
later ports. No application/audio implementation or runtime acceptance exists on any platform yet.
This direction promises neither release dates nor complete parity; CPU architectures and release RIDs
remain undecided.

Start hosted cross-platform build/test evidence early once executable source exists. Real UI/audio/device
acceptance may arrive later and must never be inferred from hosted success. Report evidence for its
actual OS/version, architecture, runtime/backend, environment, and bounded behavior; a hosted Windows
runner is not automatically Windows 11 desktop acceptance. [TEST_EXECUTION](TEST_EXECUTION.md) owns
evidence tiers; [CI_CD](CI_CD.md) owns hosted validation evolution.

## Portable architecture rules

- Keep platform-specific APIs behind narrow platform/device adapters. Domain/project/plugin contracts
  must not expose Windows-specific types; audio plugin contracts must not expose WASAPI/ASIO or other
  backend implementation types. [ARCHITECTURE](ARCHITECTURE.md) and [AUDIO_ENGINE](AUDIO_ENGINE.md)
  own subsystem responsibilities.
- Use platform-safe path APIs instead of hard-coded separators or drive letters. Do not assume
  case-insensitive filesystems or Windows newline behavior. Portable tests must not depend on developer
  paths, installed devices, mutable machine state, or undocumented global tools.
- Do not assume one DPI, windowing, or device model. Localize platform-specific process/window/device
  behavior at its ownership boundary.
- Use a portable target framework where practical for portable/shared projects. Development on Windows
  alone is not a reason for a Windows-only target. OS-specific projects/adapters are permitted for
  genuine platform ownership; no project topology is prescribed now.

## Conditional native direction

If SEQ-R0 selects a native realtime component, its build/distribution strategy must explicitly consider
Windows/Linux/macOS. Managed and ordinary plugin contracts remain backend-independent. Native runtime
identity, materialization, packaging, and resource lifetime must be explicit.

Where practical, build/smoke on actual target runner families and validate managed/native interop
against the materialized native runtime, following the conceptual evidence chain:

```text
native build -> package/materialize -> smoke -> managed host build -> production interop test
```

A developer-installed library or a managed compile alone cannot prove the distributed runtime works.
This adopts an evidence pattern, not Fovium's library scripts. C++, CMake, MSVC, Clang, GCC, miniaudio,
and any concrete RID matrix remain unselected. [THIRD_PARTY](THIRD_PARTY.md) owns actual provenance;
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks choices when they become necessary.
