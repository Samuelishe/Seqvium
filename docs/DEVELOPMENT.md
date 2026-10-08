# Development

Role: Developer environment, version authority, and local tooling policy.
Read when: Setting up development or introducing SDKs, local tools, or build/run entry points.
Authoritative for: Recommended environment, SDK/tool version authority, local versus global tooling, and eventual entry points.
Not authoritative for: Git safety, implementation style, package locks, test commands, CI triggers, or platform acceptance.

## Current environment and entry points

Windows 11 is the primary development/runtime environment today; Rider is the repository owner's
primary IDE. Git is standard source control; [AGENTS](../AGENTS.md) owns safety and authorization.
[PORTABILITY](PORTABILITY.md) owns Windows/Linux/macOS target direction and evidence limits.

C#/.NET 10 is the proposed managed application direction in [ARCHITECTURE](ARCHITECTURE.md).
Avalonia is proposed/likely, not installed or adopted. No production/test projects exist.
`Seqvium.sln` remains empty. The standalone [SEQ-R0 probe](../experiments/seq-r0/README.md) has actual
Release build/run entry points; it is not the application topology. Do not build the empty solution
as stage verification.

Once managed projects exist, the `dotnet` CLI must provide a reproducible build/test path independent
of Rider. Document actual restore/build/run entry points here and actual test commands in
[TEST_EXECUTION](TEST_EXECUTION.md), after inspecting the selected SDK/test platform. PowerShell 7 is
an acceptable cross-platform repository scripting environment where a concrete need justifies it;
scripts are not introduced by this policy. Native compiler, CMake, Ninja, and native package managers
are not selected; document setup only when an implementation stage chooses them.

### SEQ-R0 experimental entry points

Run `./build.ps1` from `experiments/seq-r0/`, then `dotnet bin/Release/net10.0/Probe.dll verify`.
The [probe README](../experiments/seq-r0/README.md) lists device, pressure, controlled and lifetime
commands; the [report](experiments/SEQ-R0_REPORT.md) owns actual versions/environment/evidence.
The experiment-local `global.json` pins the SDK without affecting future application work;
`build.ps1` declares GCC/options and copies installed runtime notices. No global tools were installed,
NuGet packages restored, production assemblies created or CMake/Ninja selected. Experiment-local
`Directory.Build.props` enforces nullable/warnings-as-errors. Production SDK/topology remains open.

## Version authority

Prefer repository-declared versions/configuration over undocumented machine-global assumptions.
With the first managed projects, decide SDK pin/roll-forward policy and introduce a suitable
`global.json` if reproducibility requires it. Do not add one to decorate an empty solution.
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

| Rule | Authority / enforcement point |
| --- | --- |
| UTF-8, LF, final newline, whitespace, basic C# indentation | Root [.editorconfig](../.editorconfig) |
| Predictable tracked-text and working-tree newline policy | Root [.gitattributes](../.gitattributes): automatic text detection with LF; explicit `.bat`/`.cmd` CRLF exceptions |
| Important correctness diagnostics | `.editorconfig`; rationale in [CODING_GUIDELINES](CODING_GUIDELINES.md) |
| Nullable / warnings-as-errors | Future inherited shared MSBuild properties when projects exist |
| SDK version | Future `global.json` if justified by selected SDK policy |
| Formatting/analyzers | Future repository configuration only for a concrete need |
| Portable build/test | Future CI under [CI_CD](CI_CD.md) |

Do not rewrite unrelated files to normalize line endings. Inspect and report normalization impact
before changes; preserve user work and the staging index under AGENTS. No speculative binary asset
catalog or formatter/analyzer package is needed now.
