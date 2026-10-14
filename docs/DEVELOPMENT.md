# Development

Role: Developer environment, version authority, and local tooling policy.
Read when: Setting up development or introducing SDKs, local tools, or build/run entry points.
Authoritative for: Recommended environment, SDK/tool version authority, local versus global tooling, verification-output
organization, and build/run entry points.
Not authoritative for: Git safety, implementation style, package locks, test commands, CI triggers, or platform
acceptance.

## Current environment and entry points

Windows 11 is the current development and tested desktop environment. Rider is used for development,
but the CLI commands below work independently of an IDE. [PORTABILITY](PORTABILITY.md) distinguishes
intended Windows/Linux/macOS targets from demonstrated application support. For a first listening
session, use the [reader guide](README.md#try-sample-processing); for a code contribution, see
[Contributing](../CONTRIBUTING.md).

The application uses C#/.NET 10 and Avalonia. [Seqvium.sln](../Seqvium.sln) contains:

- [Seqvium.Core](../src/Seqvium.Core/Seqvium.Core.csproj): UI-independent musical documents, history,
  persistence, WAV resources and sampling/graph execution, using the .NET base libraries.
- [Seqvium.Audio.Windows](../src/Seqvium.Audio.Windows/Seqvium.Audio.Windows.csproj): the Windows audio adapter.
- [Seqvium.Desktop](../src/Seqvium.Desktop/Seqvium.Desktop.csproj): the Avalonia application, referencing
  Core and the Windows audio adapter.
- [Seqvium.Tests](../tests/Seqvium.Tests/Seqvium.Tests.csproj): the executable test suite.
- [Seqvium.DeviceCheck](../tools/Seqvium.DeviceCheck/Seqvium.DeviceCheck.csproj): explicitly invoked
  physical-device checks; it is not required to launch the desktop.
- [ProjectStats](../tools/Seqvium.Tools.ProjectStats/Seqvium.Tools.ProjectStats.csproj): independent
  repository diagnostics for developers.
- [VisualCheck](../tools/Seqvium.VisualCheck/README.md): isolated component/window/field-alignment and
  Desktop process verification using existing Avalonia dependencies; no production developer tab.

[Architecture](ARCHITECTURE.md) explains dependency and lifetime boundaries. The separate
[realtime probe](../experiments/seq-r0/README.md) is an experiment outside the solution, not the
production application. Default unnamed media is stored under LocalApplicationData; tests use
their own directories.

From the repository root with .NET SDK 10.0.401 (or a later patch in the 10.0.4xx band),
restore on setup or changed restore inputs/missing assets:

```text
dotnet restore Seqvium.sln --locked-mode
```

For an ordinary test/tooling change, build the affected test project once and then select classes:

```text
dotnet build tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-restore
dotnet test --project tests/Seqvium.Tests/Seqvium.Tests.csproj -c Release --no-build --no-restore --filter-class "*ArtifactRetentionTests"
```

The project build includes its references. For application launch without test changes, build
`src/Seqvium.Desktop/Seqvium.Desktop.csproj` instead. Use `--no-build` only with matching current
outputs. [TEST_EXECUTION](TEST_EXECUTION.md#required-verification-levels) owns required L0–L4 scope,
the 120-second focused target, runner filters and periodic full commands; ordinary handoffs do not
require a full suite. Reuse unchanged restored assets and sufficient successful checks.

Root [global.json](../global.json) pins the feature band with `latestPatch`, disables prerelease SDKs
and selects Microsoft.Testing.Platform. Project-local package lock files fix the restore graph.
Root [Directory.Build.props](../Directory.Build.props) enables nullable, implicit usings, warnings as
errors and package locks. The experiment has its own SDK/properties and is not in the solution.

The `dotnet` CLI provides the build/test path independently of Rider; test/filter/report commands are in
[TEST_EXECUTION](TEST_EXECUTION.md). PowerShell 7 is
an acceptable cross-platform repository scripting environment where a concrete need justifies it;
scripts are not introduced by this policy. Native compiler, CMake, Ninja, and native package managers
are not selected; document setup only when an implementation stage chooses them.

### Windows desktop launch

After locked restore and Release build, from the repository root:

```text
dotnet run --project src/Seqvium.Desktop/Seqvium.Desktop.csproj -c Release --no-build --no-restore
```

Alternatively launch `src/Seqvium.Desktop/bin/Release/net10.0/Seqvium.Desktop.exe` directly.
Use an ordinary, non-administrator desktop process for Explorer drag-and-drop. Windows UIPI rejects
medium-integrity Explorer drops into an elevated application. Do not disable OS security to test this.
Playlist MP3/FLAC decoding uses NAudio's Windows Media Foundation adapter on a worker; available
system codecs are required. WAV normalization and Core managed PCM remain separate from that backend.
No FFmpeg executable or OS codec DLL is bundled. See [provenance](THIRD_PARTY.md#audio-clip-codec-adapter).
Installed .NET 10 is needed; no global Avalonia template/tool/workload, server or browser is needed.
Launching creates an empty unnamed project and loads user preferences and workspace layout.
It stays silent until explicit Play and does not run DeviceCheck, open capture or change system
audio settings. English/Russian and theme changes persist to the
platform configuration path in [SETTINGS](SETTINGS.md#implemented-r3-f1-host-preferences).
The Panes toolbar menu opens Project Inspector, Appearance and Node Graph. A missing layout leaves
these panes hidden; importing a WAV and opening Node Graph selects the imported item's graph.
[Workspace](WORKSPACE.md#implemented-r3-f2-internal-panes) owns local keyboard paths and
[Settings](SETTINGS.md#implemented-r3-f2-workspace-layout-storage) owns the separate layout file.
The [graph workflow report](experiments/SEQ-R4-F3-B_REPORT.md) records actual desktop interaction
checks and their limitations. See [TEST_EXECUTION](TEST_EXECUTION.md) for verification commands.

### SEQ-R2-F2 physical output verification

After the solution Release build, explicitly run
`dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll measure 60` on Windows x64.
The [harness guide](../tools/Seqvium.DeviceCheck/README.md) lists smoke/lifetime/fault commands;
the [protocol](experiments/SEQ-R2-F2_PROTOCOL.md) declares workload/budgets and the
[report](experiments/SEQ-R2-F2_REPORT.md) records actual endpoint/environment and evidence gaps.
The framework-dependent adapter uses installed Windows system APIs, with no audio package, native
toolchain, global installation or future application/backend selection UI.

### SEQ-R2-F3 explicitly audible preview smoke

After the full Release build, explicitly run:

```text
dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll audition-smoke
```

This **plays sound** on the default output, warns on stderr and uses preview gain 0.05 with quiet
project-authored PCM. It changes no system volume/settings and imports nothing. Discovery/reuse tests
remain hardware-independent. The [harness guide](../tools/Seqvium.DeviceCheck/README.md) and
[F3 report](experiments/SEQ-R2-F3_REPORT.md) document opt-in diagnostics, observations and stage limits.
Default WasapiOutput has no large capture/observation arrays; detailed physical commands explicitly
enable diagnostics. No catalog, GUI, dependency or additional assembly/tool is installed.

### SEQ-R2-F4 endpoint discovery and selected output

After Release build, `dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll endpoints`
prints render/capture IDs, names, states and six defaults without opening streams or playing sound.
To explicitly test a known output, invoke `endpoint-session "<opaque output ID from discovery>"` on
the same harness. It warns before sound, uses authored PCM and gain 0.05, and checks three fresh
selected lifetimes, independent input selection, Stop, joined replacement and injected fault recovery.
Additional default-role opens do not Start. This opens no capture stream and changes no system setting.
The [harness guide](../tools/Seqvium.DeviceCheck/README.md) and [F4 report](experiments/SEQ-R2-F4_REPORT.md)
retain exact commands, endpoint observations and scope. No dependencies/tools/frameworks are added.

### SEQ-R0 experimental entry points

Run `./build.ps1` from `experiments/seq-r0/`, then `dotnet bin/Release/net10.0/Probe.dll verify`.
The [probe README](../experiments/seq-r0/README.md) lists device, pressure, controlled and lifetime
commands; the [report](experiments/SEQ-R0_REPORT.md) owns actual versions/environment/evidence.
The experiment-local `global.json` pins the SDK without affecting future application work;
`build.ps1` declares GCC/options and copies installed runtime notices. No global tools were installed,
NuGet packages restored, production assemblies created or CMake/Ninja selected. Experiment-local
`Directory.Build.props` enforces nullable/warnings-as-errors. These experimental facts do not select
the production audio SDK/topology.

## Local development resources

The home-machine setup keeps personal material outside the repository:

- `%USERPROFILE%/Music/Seqvium Sample Library/Ready`: categorized WAVs for the existing file picker or
  single-file Rack drop. `Originals` preserves downloaded members; `Sources` keeps official source
  pages/notices and archive indexes; JSON/CSV catalogs and receipts record exact paths, licenses,
  hashes and any preparation. Read the local `README.md` before sharing/exporting standalone content.
- `%USERPROFILE%/Documents/Seqvium Resources/Design References`: private owner images, stable IDs,
  searchable `index.html`, original provenance/identification confidence and `DESIGN-BRIEF.md`.
- `%USERPROFILE%/Documents/Seqvium Resources/Tools` and `Verification`: local preparation scripts,
  isolated preview tooling and read-only WAV audit evidence. Pillow is local to that Tools directory,
  not a product dependency or globally required Python package.

These are optional machine-local inputs, not bundled assets, automatic Browser roots or application
settings. Other contributors need no personal files to build/test/launch Seqvium. Do not add WAVs,
archives, images, source snapshots or private catalogs to Git or use them as required test fixtures.
Accepted project media still follows the existing durable import boundary; the external library is
not a managed project-media directory. Preserve original audio and source pitch/BPM/chord metadata;
sampler-root configuration is distinct from file format compatibility. SEQ-R9 imports optionally read
a nearby exact-path/hash-verified catalog root under [sample policy](SAMPLE_WORKFLOW.md#seq-r9-source-root-pitch);
absence never prevents build/import, and existing projects are not recalibrated on Open. User preferences and
projects are not replaced by resource preparation. GUI captures stay in ignored `.artifacts/` under
the existing retention policy. [UI design](UI_DESIGN.md#reference-library-and-next-editor-direction)
owns transferable decisions; private catalogs own per-image/source provenance, never public assets.

## Version authority

[VERSIONING](VERSIONING.md) owns product identity and checkpoint authority. Root
[Directory.Build.props](../Directory.Build.props) derives production display and numeric metadata
from four components; About reads the running Desktop's informational version without a Git suffix.
Tests/tools and the independent probe retain their own identity. Product checkpoints do not change
project serialization gates or imply a release. Work-package IDs belong in About/evidence, not the
ordinary workspace.

Prefer repository-declared versions/configuration over undocumented machine-global assumptions.
The R1 SDK pin/roll-forward policy is declared in root `global.json`; evolve it deliberately when
actual managed requirements change.
Language level must remain validated against that SDK. Package/dependency versions belong to actual
project/package/native manifests; [THIRD_PARTY](THIRD_PARTY.md) records provenance and obligations
with links to those authorities rather than maintaining a second version lock.

## Tool introduction

Before adding a persistent tool, identify the concrete problem, prefer repository-local/reproducible
configuration, document required version authority and setup, and record applicable third-party/license
implications. Avoid global machine mutation when a repository-scoped alternative exists. Do not add
tooling solely for aesthetics or metrics.

This includes analyzer packages, formatters, CMake/Ninja, native package managers, Python dependencies,
and code generation. An experimentally used developer tool does not automatically become a repository
requirement. Selection must follow task scope and actual project needs.

[Seqvium.Tools.ProjectStats](../tools/Seqvium.Tools.ProjectStats/Seqvium.Tools.ProjectStats.csproj)
is the implemented independent .NET 10 BCL-only repository diagnostics CLI. It belongs to the existing
solution, with an empty package lock and no production/analysis dependencies. The pinned SDK and
locked restore and risk-selected Release checks apply; no global tool or additional scripting environment is needed.
[PROJECT_STATS](PROJECT_STATS.md#cli-and-outputs) owns console/Markdown/JSON invocations, explicit
output, public corpus/privacy, count semantics and advisory limits. Its assembly metadata supplies
tool identity; it is developer tooling, separate from musical product capabilities and the R0 probe.

## Verification output organization

The repository is product source of truth, not a warehouse of generated test/research output. Use
ignored root `.artifacts/` for disposable captures/screenshots/logs and `.scratch/` for task-local
exploration scripts/work files. Keep user data and required runtime/build resources separate; never
globally ignore image/audio/JSON/Markdown extensions. Narrow generated-image ignores under
`docs/experiments/` do not hide intentional resources elsewhere. Git ignore rules do not untrack
existing files; any removal still needs scoped authorization and must not alter the staging index.

[Rolling retention v1](experiments/README.md#rolling-retention-v1) is executable in VisualCheck and
runs automatically at its GUI/process verification boundaries. Use its
[manual dry-run/apply commands](../tools/Seqvium.VisualCheck/README.md#artifact-retention) for a storage
review. A tool must mark its owned run and completed status before automatic rotation can consider it;
unknown artifact directories require specific review, never age-only deletion.

Small reproducible cases belong in the regular test suite. A genuinely reusable, adopted verification
tool belongs in an appropriate `tests/` or `tools/` location with documented setup/input/output and
existing tool-introduction/licensing review, not in a generated-artifact folder or a common utility.
An ad hoc local UI driver may remain ignored while needed for pending review; this does not adopt it
as supported permanent tooling. Do not delete reusable harnesses during artifact cleanup.
Use [evidence retention](experiments/README.md#evidence-retention) for observations/reports and
[coding guidelines](CODING_GUIDELINES.md#resource-and-shared-code-placement) for runtime assets/code.

## Developer diagnostics

Detailed developer Debug/Trace logging, if introduced, is activated only through developer-oriented
configuration, command-line/environment/config mechanisms rather than ordinary graphical settings.
No activation syntax, logging dependency, or executable configuration is selected here.
[SETTINGS](SETTINGS.md#production-diagnostics) owns quiet production logging, bounded file size,
rotation/count and retention, and the simple public enable/disable preference where useful.
Developer diagnostics do not change realtime execution constraints under [AUDIO_ENGINE](AUDIO_ENGINE.md).

## Machine enforcement and text consistency

If a rule can be enforced cheaply and deterministically by compiler/editor/build/CI, prefer enforcement
over relying on agent memory. Enforcement must remain proportionate; capability alone is not a reason
to introduce a tool.

| Rule                                                       | Authority / enforcement point                                                                                      |
|------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|
| UTF-8, LF, final newline, whitespace, basic C# indentation | Root [.editorconfig](../.editorconfig)                                                                             |
| Predictable tracked-text and working-tree newline policy   | Root [.gitattributes](../.gitattributes): automatic text detection with LF; explicit `.bat`/`.cmd` CRLF exceptions |
| Important correctness diagnostics                          | `.editorconfig`; rationale in [CODING_GUIDELINES](CODING_GUIDELINES.md)                                            |
| Nullable / warnings-as-errors                              | Inherited root `Directory.Build.props`; experiment-local policy remains separate                                   |
| SDK version                                                | Root `global.json` for R1; independent experiment-local pin                                                        |
| Formatting/analyzers                                       | Future repository configuration only for a concrete need                                                           |
| Portable build/test                                        | Future CI under [CI_CD](CI_CD.md)                                                                                  |

Do not rewrite unrelated files to normalize line endings. Inspect and report normalization impact
before changes; preserve user work and the staging index under AGENTS. No speculative binary asset
catalog or formatter/analyzer package is needed now.
