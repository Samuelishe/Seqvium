# Portability

Role: Cross-platform architecture, release-scope policy and portable engineering boundary.
Read when: Choosing project targets, platform/device APIs, native distribution, or platform evidence.
Authoritative for: OS target direction, portable/shared versus platform-specific rules, evidence-based release scope and
platform claim limits.
Not authoritative for: Release dates, runtime parity, selected RIDs/toolchains/backends, developer setup, or CI
triggers.

## Product target and current evidence

**Seqvium targets Windows, Linux, and macOS.** Windows is the primary early development/runtime
environment. Linux and macOS are first-class architectural targets from the start, not accidental
later ports. R3-F1 has a runnable desktop foundation, not a supported product release. The bounded Windows audio probe
has [experimental runtime/device evidence](experiments/SEQ-R0_REPORT.md). F2 adds a bounded production
Windows x64 WASAPI output adapter and [physical evidence](experiments/SEQ-R2-F2_REPORT.md), while Core's
musical/project/PCM contracts remain portable and contain no Windows API. Linux/macOS remain untested.
This direction promises neither release dates nor complete parity; CPU architectures and release RIDs
remain undecided.

Start hosted cross-platform build/test evidence early once executable source exists. Real UI/audio/device
acceptance may arrive later and must never be inferred from hosted success. Report evidence for its
actual OS/version, architecture, runtime/backend, environment, and bounded behavior; a hosted Windows
runner is not automatically Windows 11 desktop acceptance. [TEST_EXECUTION](TEST_EXECUTION.md) owns
evidence tiers; [CI_CD](CI_CD.md) owns hosted validation evolution.

F3 discovery/temporary PCM/reuse stays in portable Core and uses explicit platform filesystem paths.
Local tests exercise Windows paths and exclusive-file refusal; Linux/macOS permissions, links and
filesystem/device runtime remain unverified. Its [physical raw-preview smoke](experiments/SEQ-R2-F3_REPORT.md)
validates only the existing UR12 44.1 kHz stereo float32 endpoint, with no input stream/selection, MIDI,
new backend or additional support claim. Compilation alone does not establish other formats/platforms.

F4 adds portable session intent/snapshot/resolution and Windows x64 render/capture endpoint enumeration,
with independent logical selections and joined output replacement. The [F4 report](experiments/SEQ-R2-F4_REPORT.md)
records this machine's endpoints, role/default/status observations and explicitly selected UR12 checks.
Capture endpoints were enumerated/selected only; no input stream, microphone samples or recording test.
Other active outputs were listed without audible playback. Linux/macOS runtime/device support and
packaged distribution remain unverified. No OS preference was changed; endpoint IDs are session facts,
not portable project requirements.

## R3-F1 desktop feasibility and evidence

Desktop targets plain `net10.0`, not a Windows-only TFM; guarded user32 chrome is never used off Windows.
Avalonia supplies Win32, X11 and native macOS adapters plus Skia/HarfBuzz native RID assets via packages.
Linux defaults to X11/XWayland; native Wayland is not enabled. Linux needs a real display/session and
compatible X11/fontconfig/native libraries; macOS uses `libAvaloniaNative.dylib` and OS frameworks,
not the .NET macOS workload. Exact distributed prerequisites remain Q-041.
See official [Linux](https://docs.avaloniaui.net/docs/platform-specific-guides/linux) and
[macOS](https://docs.avaloniaui.net/docs/platform-specific-guides/macos) platform guidance.

The local Windows SDK builds the RID-independent executable assembly without apphost with zero warnings;
manifest/lock inspection confirms the cross-platform dependency shape. This supports hosted build/test
feasibility, not actual Linux/macOS execution or a cross-compiled packaged release. No hosted jobs ran.
Other desktops keep native window decorations pending OS-specific validation. Windows GUI evidence is
11 x64, 96 DPI, normal/minimum/maximized sizes, pointer and keyboard actions, both host languages/themes.
High/mixed DPI, cross-monitor behavior, Linux/macOS window/input/accessibility, screen readers, diverse
GPU/driver configurations, signing/installers and delivered clean-machine prerequisites remain untested.

R3-F2's [workspace evidence](experiments/SEQ-R3-F2_REPORT.md) adds 58 actual Windows 11 x64 checks and
six inspected pane screenshots on one 1920x1080 display at 96 DPI: 1100x750, reduced 640x511 and maximized
1920x1040 clients. State/persistence stay platform-independent and use the existing configuration paths;
native input automation is a local evidence method, not production pane code. Changed scaling, mixed DPI,
cross-monitor, screen-reader certification and Linux/macOS desktop/configuration runtime are untested.
No new OS support or packaged-distribution claim follows from F2.
SEQ-R3-CLOSE accepts this bounded foundation locally after owner F1/F2 acceptance and a further 29
physical Windows 11 x64/96-DPI checks at 1100x750. This closes no wider DPI/accessibility, Linux/macOS
runtime or delivered-distribution question. Windows local acceptance does not declare release support.

## Platform evidence and support scope

These are distinct, complementary dimensions. They can coexist; this table is neither a mandatory
sequence nor a readiness score. Evidence is required for the capabilities actually implemented and
claimed, not every future workstation feature.

| Dimension                                   | Meaning and limit                                                                                                                                                                                                             |
|---------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Architectural target                        | Shared domain/project contracts and platform boundaries deliberately accommodate the OS family; no functioning application or release is implied                                                                              |
| Source/build compatibility                  | Declared source restores/compiles for a particular environment; compilation alone proves no GUI, device or delivered-artifact behavior                                                                                        |
| Automated hosted verification               | Checks actually passed at a stated revision on identified runners; only their exercised contracts are established                                                                                                             |
| Native runtime/materialization verification | If native components are used, the correct materialized component loads and interoperates with the actual host for the declared environment; managed compilation or developer-installed libraries are insufficient            |
| Actual desktop GUI acceptance               | Implemented windowing, input, focus, scaling and workspace interaction work on the declared desktop; nominally portable types and hosted tests do not establish this                                                          |
| Actual audio-device/backend acceptance      | Claimed playback, and capture/monitoring where shipped, work with declared devices/backends and configurations; GUI availability and offline rendering are separate                                                           |
| Packaged distribution acceptance            | The delivered artifact installs/materializes as applicable, launches and resolves dependencies in a declared clean environment, with applicable delivery obligations met; source builds and development launches are separate |
| Publicly declared support scope             | A bounded product claim based on the necessary software, desktop, device and distribution evidence together, with supported environments/capabilities and limitations stated                                                  |

Record revision/artifact identity, OS/version, CPU architecture/runtime, desktop/integration environment,
native dependencies where applicable, and tested behavior/result/limits. Audio evidence additionally
identifies device/interface, backend/driver, sample rate, channel and buffer configuration and workload.
Storage evidence identifies filesystem/volume and tested failure conditions. None of these facts may
be invented before implementation. One device/configuration does not establish every device on an OS;
one Ubuntu environment does not establish every Linux distribution or desktop/audio environment.
An untested combination is not accepted merely because a nearby combination passed. The eventual
supported coverage and evidence sufficient for it must be justified explicitly, not assumed universal.

Conceptual descriptions may distinguish **architectural target**, **build/test-verified environment**, **experimentally
usable runtime**, **bounded desktop/device-validated environment**, and **distributable supported release target**. They
are not final marketing terms, enum values or a
universal ladder. Attach actual scope, missing evidence and known limitations; a hardware-validated
runtime can still lack packaging or serialization acceptance. No percentage-based readiness metric.
Build-only, hosted-only or experimental results must not be presented as full DAW support.

## Evidence-led release policy

Windows remains the primary early implementation and manual-validation environment. Windows-first
work is legitimate when shared domain, audio/plugin, resource and persistence contracts remain portable
and platform behavior stays at narrow adapters. This development priority neither selects Windows as
the sole release platform nor weakens Linux/macOS architectural ownership.

- Begin hosted Windows/Linux/macOS checks early when executable projects exist, under the existing
  [CI policy](CI_CD.md#current-boundary-and-initial-managed-ci). Hosted success is one evidence dimension.
- Before declaring a platform release supported, identify its intended environment/capability scope
  and establish the necessary software/project-integrity, actual desktop, audio and packaged-distribution
  evidence. Successful hardware use cannot replace serialization tests or delivery validation.
- Intended audio output must be verified and available within a supported DAW release scope. An editor
  that opens without audio remains useful but does not establish full DAW acceptance. Recording and
  monitoring readiness are separate claims when those capabilities are shipped; future recording is
  not made an early release prerequisite.
- State supported scope and limitations publicly for each distributed target. Windows acceptance,
  Linux hosted-only checks and macOS build-only verification must retain those different descriptions;
  common source or green matrix results do not establish release parity.
- Linux/macOS delivery need not share a Windows release date. Adequately evidenced targets may ship
  at the same milestone; no policy forbids earlier readiness or mandates deferral to R14+. Insufficient
  release evidence does not remove an OS family from first-class architecture or early verification.
- Expand distribution/backend/device/architecture coverage incrementally when evidence supports it,
  preserving portable project meaning. Keep unavailable capabilities honest and dependencies scoped.

At each release milestone, the remaining product choice is which adequately evidenced environments to
distribute: a bounded Windows-first scope or inclusion of ready Linux/macOS environments are sensible
options, not binding commitments. No exact OS/version, CPU/RID, device/backend list, prerequisites,
artifact layout, installation/signing/notarization method, distribution destination or timeline is
selected. Q-041 owns the concrete supported target matrix and prerequisites; Q-061 owns the complete
release scenario/dependency audit in [KNOWN_PROBLEMS](KNOWN_PROBLEMS.md).
[ROADMAP](ROADMAP.md#platform-release-acceptance-ownership) assigns bounded stage responsibility.

## Desktop and delivered-environment acceptance

For implemented first-party UI, actual desktop validation covers window lifecycle and custom chrome,
movement/resizing, keyboard actions and focus return, input/pointer interaction, high DPI/scaling,
pane/overlay/docking behavior, and accessibility integration for the capabilities claimed. External
plugin window behavior is validated when that hosting capability exists, not required before it.
[WORKSPACE](WORKSPACE.md) and [UX](UX_CONTRACT.md) retain observable behavior;
Avalonia adoption is established; Q-064 retains broader input/accessibility evidence. Hosted UI logic tests
supplement rather than replace actual interaction. No universal accessibility certification is claimed.

Linux desktop evidence must identify its actual graphical/audio integration and native dependencies.
Different environments may require separate bounded checks for windowing, input/focus, scaling, audio
output, device selection, filesystem behavior and dependency loading. X11/Wayland and ALSA/PipeWire
are examples of differences to investigate, not selected implementations or mandatory support lists.
macOS compilation likewise proves neither functioning desktop/audio behavior nor distribution,
signing/notarization readiness; actual applicable delivery requirements remain later choices.

WSLg evidence must distinguish Linux process/runtime execution, graphical interaction under WSLg,
and audio through its bridged environment. A success in one does not prove the others. Even success
in all three is bounded to that environment; it does not substitute for native Linux desktop,
device/backend or packaged-distribution acceptance. WSLg is useful additional evidence, not a proxy
for every Linux desktop or hardware configuration.

Distinguish source restore/build, direct launch of development output, packaging/materialization,
clean-environment installation where applicable, delivered launch/dependency loading, and eventual
update/removal behavior where applicable. A Rider/developer-CLI launch is not proof that the delivered
artifact works. Do not require an installer or updater implementation through this policy. Native
loading must work without undeclared developer-installed libraries; licenses/redistribution obligations
remain [THIRD_PARTY](THIRD_PARTY.md) and Q-014, not inferred from a successful build.

## Portable documents and environment availability

Case-sensitive and case-insensitive filesystem behavior must preserve project references, package
identity, extension resources and internal paths. Case-only filename differences must not silently
collide or break links when content moves. Filename/path spelling is not semantic identity. Future
formats/resource mechanisms must prevent incompatible collisions or explicitly detect and report them
before unsafe materialization; no case-folding, naming scheme or storage format is chosen here.

A Windows-created project moved to Linux/macOS must retain safely understandable canonical musical
meaning, relationships and accepted managed audio under [PROJECT_FORMAT](PROJECT_FORMAT.md).
Prior platform-specific paths, audio devices, monitor geometry, theme/language or user-layout settings
must not be required merely to edit it. Environment preferences adapt under [SETTINGS](SETTINGS.md)
and [WORKSPACE](WORKSPACE.md#layout-ownership-and-restoration); project intent must not be silently
rewritten to match negotiated device facts. Exact adaptation/migration remains Q-053/Q-069.

First-party modules and third-party plugins may be unavailable on another OS. Metadata/identity
availability proves no executable compatibility. Preserve stable identity, compatible opaque state
and relationships; safely interpretable projects use existing
[degraded access](EXTENSIONS.md#degraded-project-opening-and-operation-blockers) and scoped blockers.
No identical third-party plugin availability or early CLAP/VST3 hosting is promised (Q-016/Q-024).

Application/document availability, playback readiness, recording readiness and device/backend
availability are separate. If no suitable output/backend exists, retain safe document access and
available editing, while explaining unavailable playback and supported recovery direction through
[UX feedback](UX_CONTRACT.md#project-availability-and-dependency-blockers). Input failure does not
automatically prove output failure or vice versa. Offline render remains device-independent where
its canonical processing dependencies are available; device independence cannot bypass missing code.
Q-062/Q-069 retain device failure/recovery, clocks and runtime configuration mechanisms.
F2 supports float32 mono/stereo 44.1/48 kHz output with queried period/capacity and standard channel order;
the report distinguishes actually tested facts from merely supported adapter configurations. Faulted
playback stops and requires deliberate fresh initialization, without canonical/media mutation or a
seamless-recovery claim. F4's logical input selection is distinct from unevidenced input streaming,
MIDI/ASIO, broader endpoint playback, other platforms and delivered packaging.

Cross-platform offline tests can establish intended musical relationships, scheduling/DSP behavior,
valid rendering and declared numerical tolerances without a physical audio device, following
[AUDIO_ENGINE](AUDIO_ENGINE.md#offline-rendering-direction). They establish neither physical playback
quality nor bit-identical output across arbitrary processors, platforms or plugin versions.

Save/recovery/media integrity follows the accepted
[persistence contract](PROJECT_FORMAT.md#commit-evidence-and-ambiguous-completion). Windows success or
one rename does not prove another OS/filesystem/volume's atomic visibility, crash consistency or durable
persistence. Future adapters must demonstrate the guarantees actually claimed on declared environments,
with applicable failure/race evidence; Q-009/Q-058/Q-059 retain formats, protocols and validation.

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
native build -> package/materialize -> runtime loading -> managed/native interoperability -> bounded actual execution
```

A developer-installed library or a managed compile alone cannot prove the distributed runtime works.
This is a conceptual chain of evidence, not a selected build pipeline or three-platform R0 certification.
C++, CMake, MSVC, Clang, GCC, miniaudio, and any concrete RID matrix remain unselected.
[THIRD_PARTY](THIRD_PARTY.md) owns actual provenance;
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md) tracks choices when they become necessary.
