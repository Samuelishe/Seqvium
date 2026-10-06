# Coding guidelines

Role: Implementation conventions and engineering invariants.
Read when: Writing, reviewing, or refactoring C# production or test code.
Authoritative for: Naming/language, code structure, async, lifetime, errors, refactoring, and managed warning policy.
Not authoritative for: Product behavior, subsystem architecture, package versions, test commands, CI triggers, or platform support status.

These rules bind future implementation. No managed projects exist yet. Read [ARCHITECTURE](ARCHITECTURE.md)
and the affected subsystem owner alongside this document; coding policy applies their contracts in code.

## General C# and structure

- Use modern validated C# supported by the repository-selected .NET SDK; version authority belongs to
  [DEVELOPMENT](DEVELOPMENT.md). Enable nullable reference types in production and tests.
- Prefer clear domain terminology over framework jargon and unexplained abbreviations. Make invalid
  states difficult to represent where that remains simple.
- Prefer small cohesive types, visible dependencies, explicit ownership, and explicit state transitions.
  An abstraction needs a concrete ownership, substitution, platform, plugin, realtime, or testability
  reason. Do not create a service/interface/factory for every noun or hypothetical future need.
- Do not use Service Locator or hide dependencies in mutable global application state.
- Avoid God objects, ViewModels, and controllers. File/type size is a diagnostic signal, not an automatic
  refactoring command; do not split cohesive code into tiny files for line-count aesthetics.

## Language and documentation

Seqvium is a public FOSS project. Source identifiers, comments, XML documentation, log/diagnostic
identifiers, and repository documentation use English. User-facing localization is a separate product
concern; this rule does not select an interface language.

Comments explain non-obvious intent, invariants, ownership, platform/realtime constraints, surprising
algorithms, or compatibility decisions; they do not narrate obvious code line by line. Meaningful XML
documentation should explain reusable/public contract semantics when useful to consumers. Do not
require boilerplate for every private member or enable documentation metrics for their own sake.
After signature changes, reconcile `<param>` entries and remove orphaned documentation comments.

## Async, concurrency, resources, and errors

- Use `async`/`await` for asynchronous operations and propagate `CancellationToken` where cancellation
  is meaningful. Do not use `.Result`, `.Wait()`, or equivalent sync-over-async in ordinary async code
  or tests. Cancellation must respect the operation's ownership and publication contract.
- Avoid unobserved fire-and-forget work. Background tasks need explicit supervision, completion,
  cancellation, and shutdown ownership. Keep publication and thread-affinity points visible in code.
- Timers, subscriptions, streams, handles, buffers, and disposable resources need one clear lifetime
  owner, including acquisition, transfer where applicable, and cleanup on failure/cancellation.
- Keep long disk, decode, generation, network, and computation work off the UI thread.
- Catch exceptions only to recover, translate, add meaningful context, or guarantee cleanup. Broad
  catches must not hide failures. Separate technical diagnostics from user-facing error presentation;
  avoid unnecessarily exposing sensitive user paths or content.

## Realtime, DSP, and host boundaries

[AUDIO_ENGINE](AUDIO_ENGINE.md) owns realtime and host-context contracts; [NODE_GRAPH](NODE_GRAPH.md)
owns the editable/prepared graph boundary. Apply them during implementation:

- The callback/realtime path must not perform blocking filesystem/network I/O, UI work, unbounded
  waiting, uncontrolled allocation, avoidable GC-sensitive work, blocking locks with unbounded
  contention, arbitrary traversal of mutable UI/domain objects, potentially blocking logging, or
  expensive exception-driven control flow.
- Make realtime resource publication, use, and retirement ownership explicit. Editable node/project
  objects must not become the objects directly traversed by the callback. Publish through the accepted
  prepared/bounded execution boundary; no queue, snapshot layout, or native strategy is chosen here.
- First-party DSP/processors derive coefficients and relevant behavior from host processing context,
  including rate, frames/blocks, and channels. Do not hard-code `44.1 kHz` as a universal rate. Handle
  supported configurations deliberately; unsupported configurations yield compatibility/errors with
  diagnostics rather than unexplained crashes. Numeric ranges remain owned by future audio contracts.
- Host-owned device/backend integration stays below processing contracts. Ordinary plugin/node
  processing must not depend directly on WASAPI, ASIO, miniaudio, ALSA, PipeWire, or CoreAudio types.

Extensions are host guests under [EXTENSIONS](EXTENSIONS.md). APIs must preserve host-owned transport,
audio-device abstraction, node graph engine, project integrity, and resource lifetime boundaries.
Plugins may consume capabilities; accidental public APIs must not transfer fundamental ownership or
expose current backend types. Resolve declared incompatibility with compatibility diagnostics rather
than intentional host failure. Preserve unknown/missing plugin state during serialization under
[PROJECT_FORMAT](PROJECT_FORMAT.md); in-process hard-fault containment is not guaranteed.

## UI code

Avalonia remains a proposed/likely framework until explicit implementation adoption. Regardless of
framework, domain/business/audio logic must not live in pane/control code for convenience. UI owns
neither realtime execution nor project integrity; workspace panes host presentation/editing surfaces,
not domain ownership. Keep pure non-visual logic framework-independent where practical and localize
platform-specific UI operations at platform/UI boundaries.

MVVM is a tool. Genuine pointer interaction, rendering integration, control lifecycle, and focused
visual behavior may live in focused control/code-behind logic. Do not create a God ViewModel merely to
avoid all code-behind. [UI_DESIGN](UI_DESIGN.md), [UX_CONTRACT](UX_CONTRACT.md), and
[WORKSPACE](WORKSPACE.md) remain the visual and behavior owners.

## Mechanical versus behavioral change

A mechanical refactor preserves processing/statement order, timing, cancellation, ownership,
serialization, public contracts, plugin compatibility, user-visible behavior, routing, defaults, and
error semantics. A compile error caused by moving code is not permission for opportunistic redesign:
restore equivalence first. Behavioral change must be explicit in task scope and validation.

Do not weaken tests/assertions or failure semantics to make a refactor pass. Do not combine unrelated
cleanup with a focused task unless the change requires it. [TEST_EXECUTION](TEST_EXECUTION.md) owns
proportional verification and evidence limits.

## Zero-warning baseline and enforcement

**Supported builds of Seqvium production/test code must complete with zero warnings and zero errors.**
New production and test code starts from that baseline. Once managed projects exist, introduce
repository-wide inherited MSBuild policy, preferably `Directory.Build.props`, with the principle:

```xml
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

Exact properties may be refined with the first real projects. This policy is accepted but is not yet
compiler-enforced; do not create shared build files for the empty solution. Do not use broad `NoWarn`
lists to obtain green output. Suppressions must be narrow, reasoned/documented, and tested where relevant;
nullability should be established by guards/contracts, not unjustified `!` to silence diagnostics.

The root [.editorconfig](../.editorconfig) already marks particularly important failure classes as errors:

| Diagnostic | Guard |
| --- | --- |
| CS8602 | Possible null dereference |
| CS1572 / CS1573 / CS1587 | Stale parameter documentation, missing parameter documentation in a documented signature, or orphaned XML comments after refactors |
| xUnit1031 | Blocking async test behavior, when xUnit analyzers are present |

These explicit rules document recurring mistakes and influence IDE diagnostics even after compiler
warnings become globally fatal. They do not install analyzers, select xUnit, or require XML boilerplate.
Do not invent a style-analyzer package here. Cheap deterministic enforcement is preferred when justified;
[DEVELOPMENT](DEVELOPMENT.md#tool-introduction) owns persistent tool introduction.
