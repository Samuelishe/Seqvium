# Development

Role: Developer environment, version authority, and local tooling policy.
Read when: Setting up development or introducing SDKs, local tools, or build/run entry points.
Authoritative for: Recommended environment, SDK/tool version authority, local versus global tooling, and eventual entry
points.
Not authoritative for: Git safety, implementation style, package locks, test commands, CI triggers, or platform
acceptance.

## Current environment and entry points

Windows 11 is the primary development/runtime environment today; Rider is the repository owner's
primary IDE. Git is standard source control; [AGENTS](../AGENTS.md) owns safety and authorization.
[PORTABILITY](PORTABILITY.md) owns Windows/Linux/macOS target direction and evidence limits.

C#/.NET 10 is adopted for the R1 canonical foundation in [ARCHITECTURE](ARCHITECTURE.md#r1-canonical-foundation).
Avalonia remains proposed, not adopted. [Seqvium.sln](../Seqvium.sln) contains the portable
[Seqvium.Core](../src/Seqvium.Core/Seqvium.Core.csproj) library and one
[Seqvium.Tests](../tests/Seqvium.Tests/Seqvium.Tests.csproj) executable test project.
R2-F1 adds BCL-only WAV/media and offline sampler files within the same library; the
[architecture](ARCHITECTURE.md#r2-f1-managed-media-and-offline-execution) lists source responsibilities.
Default unnamed media ownership is under LocalApplicationData; tests always supply owned directories.
R2-F2 adds the narrow `Seqvium.Audio.Windows` platform library and explicit `Seqvium.DeviceCheck`
physical verification executable; [architecture](ARCHITECTURE.md#r2-f2-execution-and-platform-ownership)
owns their dependency boundary. There is no workstation executable or UI. The standalone
[SEQ-R0 probe](../experiments/seq-r0/README.md) remains independent.

From the repository root with .NET SDK 10.0.401 (or a later patch in the 10.0.4xx band):

```text
dotnet restore Seqvium.sln --locked-mode
dotnet build Seqvium.sln -c Release --no-restore
dotnet test --solution Seqvium.sln -c Release --no-build --no-restore
```

Root [global.json](../global.json) pins the feature band with `latestPatch`, disables prerelease SDKs
and selects Microsoft.Testing.Platform. Project-local package lock files fix the restore graph.
Root [Directory.Build.props](../Directory.Build.props) enables nullable, implicit usings, warnings as
errors and package locks. The experiment has its own SDK/properties and is not in the solution.

The `dotnet` CLI provides the build/test path independently of Rider; test/filter/report commands are in
[TEST_EXECUTION](TEST_EXECUTION.md). PowerShell 7 is
an acceptable cross-platform repository scripting environment where a concrete need justifies it;
scripts are not introduced by this policy. Native compiler, CMake, Ninja, and native package managers
are not selected; document setup only when an implementation stage chooses them.

### SEQ-R2-F2 physical output verification

After the solution Release build, explicitly run
`dotnet tools/Seqvium.DeviceCheck/bin/Release/net10.0/Seqvium.DeviceCheck.dll measure 60` on Windows x64.
The [harness guide](../tools/Seqvium.DeviceCheck/README.md) lists smoke/lifetime/fault commands;
the [protocol](experiments/SEQ-R2-F2_PROTOCOL.md) declares workload/budgets and the
[report](experiments/SEQ-R2-F2_REPORT.md) records actual endpoint/environment and evidence gaps.
The framework-dependent adapter uses installed Windows system APIs, with no audio package, native
toolchain, global installation or future application/backend selection UI.

### SEQ-R0 experimental entry points

Run `./build.ps1` from `experiments/seq-r0/`, then `dotnet bin/Release/net10.0/Probe.dll verify`.
The [probe README](../experiments/seq-r0/README.md) lists device, pressure, controlled and lifetime
commands; the [report](experiments/SEQ-R0_REPORT.md) owns actual versions/environment/evidence.
The experiment-local `global.json` pins the SDK without affecting future application work;
`build.ps1` declares GCC/options and copies installed runtime notices. No global tools were installed,
NuGet packages restored, production assemblies created or CMake/Ninja selected. Experiment-local
`Directory.Build.props` enforces nullable/warnings-as-errors. These experimental facts do not select
the production audio SDK/topology.

## Version authority

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

[PROJECT_STATS](PROJECT_STATS.md) defines the accepted future BCL-only/cross-platform diagnostics
direction. No CLI/project exists or is authorized by that contract alone; introduction waits for explicit
code authorization. It is independent repository tooling, not part of the SEQ-R0 audio probe.

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
