# Third-party provenance and evaluation

Role: Provenance ledger and evaluation boundary for external components and material.
Read when: Evaluating, introducing, upgrading, replacing, or removing a dependency, service, action, or asset.
Authoritative for: Project licensing/provenance boundary, introduced material, candidate status, obligations, and future entry fields.
Not authoritative for: Authoritative license text, final technical choices, subsystem design, or installed-version locks.

## Project license and material boundaries

Seqvium-authored work is licensed under **Apache License, Version 2.0** (`Apache-2.0`) unless a
file/component explicitly states another license. This covers Seqvium-authored code, documentation,
and assets. The root [LICENSE](../LICENSE) is the primary authoritative project license artifact,
downloaded unchanged from the [official Apache text](https://www.apache.org/licenses/LICENSE-2.0.txt).

Apache-2.0 permits commercial use, modification, redistribution, and derivative works under its terms,
includes an explicit contributor patent grant, and does not require derivative Seqvium code to remain
open source. It is a mature [OSI-approved license](https://opensource.org/license/apache-2.0) for
collaborative permissive FOSS; no extra use or redistribution restrictions are added.

**Seqvium's Apache-2.0 license does not relicense third-party material.**

Third-party executable plugins are independently authored software. Compatibility with Seqvium does
not make them Seqvium-authored or warrantied by the Seqvium project. [EXTENSIONS](EXTENSIONS.md) owns
the technical host/plugin responsibility boundary. Seqvium is provided on the `AS IS` basis, without
warranties, and with the limitation of liability as specified by Apache-2.0, subject to applicable law.
Root [LICENSE](../LICENSE) is authoritative; this summary adds no separate EULA or legal restrictions.

| Material | Licensing boundary |
| --- | --- |
| Seqvium-authored code/docs/assets | Apache-2.0 unless explicitly identified otherwise |
| Third-party dependencies | Their own licenses/terms, including relevant transitive and distribution obligations |
| Third-party assets/content | Their own licenses/terms and redistribution rights, independently of application code |
| Generated/derived material | Source/input obligations may remain; generation, transformation, or resampling does not clear them |

Evaluate each future dependency/asset for compatibility with the **distributed product** before
distribution. OSI approval alone does not imply all licenses can be combined with Apache-2.0.
Copyleft, SDK terms, codec/patent constraints, and redistribution requirements need concrete evaluation.

### Notices and future source identification

No current attribution content requires a root `NOTICE`; do not create one for ceremony. Apache-2.0
NOTICE handling becomes relevant when Seqvium has its own notices or redistributed components require
attribution treatment. Evaluate license/notice obligations before distribution when dependencies/assets
are introduced. This ledger owns provenance/evaluation and **does not substitute for legally required
bundled licenses or notices**.

When actual source files are created, prefer concise `SPDX-License-Identifier: Apache-2.0` where
file-level identification is useful. Do not add large per-file boilerplate by default unless later
tooling/legal policy establishes a concrete need. Do not add SPDX headers to all Markdown; root LICENSE
remains the primary license artifact.

## Introduced

R1 adopts the installed .NET 10 SDK/runtime for its framework-dependent canonical library and managed
tests. Production code has no external NuGet dependency. The test project uses the components below;
its [manifest](../tests/Seqvium.Tests/Seqvium.Tests.csproj) and
[lock file](../tests/Seqvium.Tests/packages.lock.json) own versions and the complete dependency graph.
No third-party source/assets are vendored. Local binaries are ignored and no product distribution is
created. Package licenses were checked in the restored package metadata, with official sources linked
below; redistribution of test binaries must preserve the applicable licenses/notices.

| Component / author | Form, purpose and source | License/distribution evaluation | Version authority |
| --- | --- | --- | --- |
| xUnit.net v3 and analyzers / .NET Foundation and contributors | Test-only managed framework, assertions, runners and compiler analyzers; [official source/license](https://github.com/xunit/xunit/blob/main/LICENSE), [MTP integration](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) | Apache-2.0 in the selected packages, compatible with Seqvium's license. Preserve license and any applicable notices if redistributing; no test package is a production-library dependency | Test manifest selects `xunit.v3.mtp-v1`; lock fixes xUnit components and analyzers |
| Microsoft.Testing.Platform, MSBuild, TrxReport.Abstractions and Telemetry / Microsoft | Test-only managed execution integration; [official source/license](https://github.com/microsoft/testfx/blob/main/LICENSE) | MIT; retain copyright/license on redistribution. Telemetry is a framework default, not a Seqvium application service; developers can opt out with `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` | Transitive test lock entries |
| Microsoft.ApplicationInsights, Microsoft.Bcl.AsyncInterfaces and Microsoft.Win32.Registry / Microsoft | Test-only transitive telemetry/BCL compatibility packages; [Application Insights source](https://github.com/microsoft/ApplicationInsights-dotnet), [runtime source/license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) | Installed package metadata declares MIT for all three. Preserve applicable copyright/license on redistribution; no application telemetry or Windows-only domain dependency is introduced | Transitive test lock entries |

SEQ-R0 separately uses installed tools/runtime and Windows system APIs, without NuGet audio packages,
downloaded audio libraries or assets. Compiled probe outputs/notices remain local ignored artifacts;
the following entries describe its independent provenance and obligations.

| Component / author | Form, purpose and source | License/distribution evaluation | Version authority |
| --- | --- | --- | --- |
| .NET / .NET Foundation and contributors | Installed SDK/runtime for R1 and framework-dependent experimental host; [official source/license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) | Runtime source is MIT; no runtime is bundled here. Future self-contained distribution must retain applicable runtime/transitive notices and review its actual contents | Production [global.json](../global.json), independent experiment-local [global.json](../experiments/seq-r0/global.json); actual experimental runtime in [report](experiments/SEQ-R0_REPORT.md) |
| GCC / Free Software Foundation, MSYS2 UCRT64 packaging | Existing developer C compiler; [GCC](https://gcc.gnu.org/), [MSYS2](https://www.msys2.org/) | Compiler GPLv3+, not redistributed. Eligible covered runtime code uses GPLv3 with GCC Runtime Library Exception 3.1; installed `COPYING.RUNTIME` and `COPYING3` are copied beside DLL output. No compiler/global installation performed | [build.ps1](../experiments/seq-r0/build.ps1) declares compiler/options; observed package identity in report |
| MinGW-w64 headers/CRT / MinGW-w64 contributors | Existing Windows API headers/import libraries and linked runtime; [official source/licensing](https://www.mingw-w64.org/support/#licensing) | Mixed runtime notices include permissive ZPL/BSD/public-domain terms and component-specific exceptions. Build copies the complete installed `COPYING.MinGW-w64-runtime.txt`, preserving its full obligations rather than assuming one umbrella license. Future distribution must audit actual linked contents | Build script uses installed MSYS2 UCRT64; exact tested headers/CRT packages in report |
| Windows WASAPI/COM/MMCSS/UCRT / Microsoft | Installed OS services, not redistributed DLLs; [official API](https://learn.microsoft.com/en-us/windows/win32/coreaudio/rendering-a-stream) | Probe calls OS APIs. System DLLs are not bundled or relicensed. Actual native import list, including a private CRT API-set import, is reported; clean supported distribution remains unvalidated | OS/API and import inspection in report |

The native probe's C scheduling/DSP and generated triangle/event sequence are Seqvium-authored
Apache-2.0 work. Local native loading succeeds without MSYS2 runtime DLL imports; that does not establish
clean-machine deployment or general legal/distribution acceptance. No root NOTICE is added in lieu of
the applicable runtime license files.

## Planned / under evaluation

These are candidates or product directions, not dependencies, adoption decisions, or promises.
SEQ-R0's bounded evaluation is recorded in its [protocol](experiments/SEQ-R0_PROTOCOL.md) and report;
it does not adopt a portable audio dependency or production engine.

| Candidate / direction | Intended evaluation boundary |
| --- | --- |
| Further .NET application delivery | Managed canonical foundation is adopted in R1; workstation executable, self-contained/native packaging and distribution remain unevaluated |
| Avalonia | Proposed UI framework; evaluate official package/API/platform/license evidence before adoption |
| miniaudio or comparable backend | Still a portable candidate. Official miniaudio license offers public-domain or MIT-0 terms; PortAudio has an MIT-style license. Neither is downloaded, built, redistributed or device-tested here; concrete revisions/transitive/backend/distribution checks remain necessary |
| Future ASIO integration | Desired later device capability, not an R0 requirement or adopted SDK/backend; evaluate concrete implementation source, licensing and redistribution when its stage approaches |
| CLAP / VST3 hosting | Later possibility; evaluate concrete SDK terms, bridges, distribution and product need when justified |
| WAV / FLAC support | Planned/candidate media capabilities; evaluate actual codec/library choices separately |

A native realtime engine, its implementation language (possibly C++), exact native ABI, and
package/plugin mechanisms remain open architectural choices. **C++ is a language choice, not a
third-party dependency.** Any chosen compiler/toolchain/runtime or library needs its own concrete
evaluation. [ARCHITECTURE](ARCHITECTURE.md), [AUDIO_ENGINE](AUDIO_ENGINE.md), and
[EXTENSIONS](EXTENSIONS.md) own those choices; this ledger does not accept them.

## Assets / borrowed material

No samples, sample packs, presets, impulse responses, demo projects/patterns, audio, icons, fonts,
or other shipped third-party assets have been introduced.

Musical content needs the same provenance discipline as code. Before material enters the official
repository or distribution, its record must identify:

- Source and author/organization, with a source URL or a documented project-authored creation method.
- License/terms and the right to redistribute the material in its intended form.
- Purpose, owning resource/subsystem, and repository paths where applicable.
- Whether it is redistributed; distinguish shipped content from developer-only evaluation/test material.
- Modifications or derivation, including source inputs and relevant generation/tool provenance.
- Introduced stage and relevant decision, if any.

This applies to samples/packs, presets, impulse responses, demo music/patterns, generated audio or
audio based on third-party material, icons, fonts, and all other shipped assets. Generation or resampling
does not erase the provenance or terms of input material. Recipe metadata can help explain derivation;
it is not evidence of redistribution rights by itself. Local test material does not become distributable
merely because it is available on disk. [SAMPLE_WORKFLOW](SAMPLE_WORKFLOW.md) separately owns accepted
audio durability, not rights clearance.

## Repository services and actions

No repository-configured hosted services or CI actions have been introduced. There are no workflows,
external service integrations, or runtime cloud-generation dependencies.

Future service/action entries need identity, organization, terms/license, purpose, official source,
introduction stage, and configuration/workflow version authority. Keep hosted services distinct from
redistributed runtime components; do not add CI just to populate this register.

## Future entry fields

Each introduced component/package, action, or service must record:

- Exact component/package/action identity and author/organization.
- License/terms, notices and redistribution/source obligations; concrete compatibility evaluation
  with Seqvium's Apache-2.0 work and the intended distributed product.
- Purpose and owning subsystem, official source, and introduced stage/decision when relevant.
- Form: managed, native, runtime, service, action, or asset; supported platforms where relevant.
- Version authority: the actual `.csproj`, central package/package manifest, native-version manifest,
  workflow, or other reproducible source of the installed revision. Link it when it exists.

An evaluation snapshot may record a checked version/date and official evidence, explicitly labeled
as research. It must not compete with build manifests. Native entries additionally need official
binary/source provenance, runtime/platform distribution implications, and material transitive obligations.
Evaluate codec/patent concerns where applicable. Asset entries use the additional fields above.

Record removal/replacement with status, stage and successor/evidence where relevant. This active
ledger retains current introduced material, materially evaluated candidates and current obligations;
history may move to the cold archive when no longer relevant to those obligations. Never remove
legally required current provenance or notices merely because material is old. Update this owner when
changed terms, upgrades, replacements or redistribution scope change obligations. Candidate popularity is not
sufficient adoption evidence.
