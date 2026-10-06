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
Acceptance is recorded in [D-029](DECISIONS_LOG.md#d-029--seqvium-uses-apache-license-20).

Apache-2.0 permits commercial use, modification, redistribution, and derivative works under its terms,
includes an explicit contributor patent grant, and does not require derivative Seqvium code to remain
open source. It is a mature [OSI-approved license](https://opensource.org/license/apache-2.0) for
collaborative permissive FOSS; no extra use or redistribution restrictions are added.

**Seqvium's Apache-2.0 license does not relicense third-party material.**

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

No executable third-party components, NuGet packages, native libraries, or vendored code have been
introduced. The pre-existing empty `Seqvium.sln` is repository scaffolding, not evidence of an installed
.NET/Avalonia dependency graph. Local Rider metadata is not shipped product material.

When actual components are introduced, record them here using the fields below. Exact installed
versions belong to build/package/native manifests once those exist; this ledger links that authority
and must not become a second package lock.

## Planned / under evaluation

These are candidates or product directions, not dependencies, adoption decisions, or promises.
No official version/license evaluation has been completed for them in the knowledge-documentation stages.

| Candidate / direction | Intended evaluation boundary |
| --- | --- |
| C# / .NET 10 | User's development direction and proposed application platform; evaluate concrete SDK/runtime and distribution obligations when introduced |
| Avalonia | Proposed UI framework; evaluate official package/API/platform/license evidence before adoption |
| miniaudio or comparable backend | SEQ-R0 backend candidate; evaluate realtime/device behavior, official source/license, native binaries and packaging |
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
or other shipped third-party assets have been introduced. Documentation references are listed below
as conceptual provenance; their code or text has not been imported.

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
external service integrations, or runtime cloud-generation dependencies. Reading reference repositories
through GitHub and using Codex during this task do not install a product dependency or CI integration.

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

Record removal or replacement with status, stage, and a successor/evidence link where relevant; retain
historical provenance rather than silently deleting an entry. Update this owner when changed terms,
upgrades, replacements, or redistribution scope change obligations. Candidate popularity is not
sufficient adoption evidence.

## Documentation references studied

Reviewed on 2026-10-06 through read-only GitHub access, on each repository's default `master` branch:

- **[Fovium](https://github.com/Samuelishe/Fovium):**
  [AGENTS](https://github.com/Samuelishe/Fovium/blob/master/AGENTS.md),
  [INDEX](https://github.com/Samuelishe/Fovium/blob/master/docs/INDEX.md),
  [PROJECT-STATE](https://github.com/Samuelishe/Fovium/blob/master/docs/PROJECT-STATE.md),
  [PROJECT-VISION](https://github.com/Samuelishe/Fovium/blob/master/docs/PROJECT-VISION.md),
  [UX-CONTRACT](https://github.com/Samuelishe/Fovium/blob/master/docs/UX-CONTRACT.md),
  [ARCHITECTURE](https://github.com/Samuelishe/Fovium/blob/master/docs/ARCHITECTURE.md),
  [DOCUMENTATION-GOVERNANCE](https://github.com/Samuelishe/Fovium/blob/master/docs/DOCUMENTATION-GOVERNANCE.md),
  [THIRD-PARTY](https://github.com/Samuelishe/Fovium/blob/master/docs/THIRD-PARTY.md),
  [README License](https://github.com/Samuelishe/Fovium/blob/master/README.md#license),
  [DECISIONS-LOG](https://github.com/Samuelishe/Fovium/blob/master/docs/DECISIONS-LOG.md),
  [KNOWN-PROBLEMS](https://github.com/Samuelishe/Fovium/blob/master/docs/KNOWN-PROBLEMS.md).
  Applied the owner metadata pattern, subject-focused contracts, selective reading, and canonical
  provenance/evaluation ledger with manifest-owned versions. Project licensing was unselected at
  SEQ-KB-R0; [D-029](DECISIONS_LOG.md#d-029--seqvium-uses-apache-license-20) later selected Apache-2.0. Did
  not import photograph-specific UX rules or production architecture.
- **[MeasPilot](https://github.com/Samuelishe/MeasPilot)** (access-restricted reference):
  [AGENTS](https://github.com/Samuelishe/MeasPilot/blob/master/AGENTS.md),
  [PROJECT_STATE](https://github.com/Samuelishe/MeasPilot/blob/master/docs/PROJECT_STATE.md),
  [FILE_INDEX](https://github.com/Samuelishe/MeasPilot/blob/master/docs/FILE_INDEX.md),
  [DOCUMENTATION_GOVERNANCE](https://github.com/Samuelishe/MeasPilot/blob/master/docs/DOCUMENTATION_GOVERNANCE.md),
  [ROADMAP](https://github.com/Samuelishe/MeasPilot/blob/master/docs/ROADMAP.md),
  [TECH_DEBT](https://github.com/Samuelishe/MeasPilot/blob/master/docs/TECH_DEBT.md),
  [WORK_LOG](https://github.com/Samuelishe/MeasPilot/blob/master/docs/WORK_LOG.md),
  [repository workflow](https://github.com/Samuelishe/MeasPilot/blob/master/.agents/skills/measpilot-repository-workflow/SKILL.md).
  Applied separation of current truth, plans, history, debt, and contracts; bounded context selection;
  preservation of historical evidence. Did not copy private implementation/history or adopt its skill,
  AgentContext, budgets/manifests, map/planner, archive tooling, or ProjectStats system.

### SEQ-KB-R3 engineering reference study

Reviewed through read-only GitHub on 2026-10-06 at the then-current default `master` heads below.
These immutable snapshots record research, not introduced product/tool/CI dependencies or version locks.

- **Fovium — `83f1524272e76e02234a9dd2d6adb03d30ff3a47`:**
  [CODING-GUIDELINES](https://github.com/Samuelishe/Fovium/blob/83f1524272e76e02234a9dd2d6adb03d30ff3a47/docs/CODING-GUIDELINES.md),
  [TEST-EXECUTION](https://github.com/Samuelishe/Fovium/blob/83f1524272e76e02234a9dd2d6adb03d30ff3a47/docs/TEST-EXECUTION.md),
  [Directory.Build.props](https://github.com/Samuelishe/Fovium/blob/83f1524272e76e02234a9dd2d6adb03d30ff3a47/Directory.Build.props),
  [main project](https://github.com/Samuelishe/Fovium/blob/83f1524272e76e02234a9dd2d6adb03d30ff3a47/Fovium/Fovium.csproj),
  [test project](https://github.com/Samuelishe/Fovium/blob/83f1524272e76e02234a9dd2d6adb03d30ff3a47/Fovium.Tests/Fovium.Tests.csproj),
  [managed CI](https://github.com/Samuelishe/Fovium/blob/83f1524272e76e02234a9dd2d6adb03d30ff3a47/.github/workflows/ci.yml),
  [native-lcms2](https://github.com/Samuelishe/Fovium/blob/83f1524272e76e02234a9dd2d6adb03d30ff3a47/.github/workflows/native-lcms2.yml),
  [native-libheif](https://github.com/Samuelishe/Fovium/blob/83f1524272e76e02234a9dd2d6adb03d30ff3a47/.github/workflows/native-libheif.yml).
  Nullable/warnings-as-errors are set in its main/test projects; the inspected shared props owns version
  metadata. Applied proportional verification, the simple three-OS managed matrix, bounded hosted/manual
  evidence, and native build/materialize/smoke/production-interop pattern; no image/library scripts copied.
- **MeasPilot — `ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1`** (access-restricted):
  [.editorconfig](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/.editorconfig),
  [CODING_RULES](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/docs/CODING_RULES.md),
  [TEST_EXECUTION](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/docs/TEST_EXECUTION.md),
  [PORTABILITY](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/docs/PORTABILITY.md),
  [CI_CD](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/docs/CI_CD.md),
  [ci-docs](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/.github/workflows/ci-docs.yml),
  [ci-portable](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/.github/workflows/ci-portable.yml),
  [ci-windows-fast](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/.github/workflows/ci-windows-fast.yml),
  [ci-windows](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/.github/workflows/ci-windows.yml),
  [ci-linux](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/.github/workflows/ci-linux.yml),
  [ci-windows-distribution](https://github.com/Samuelishe/MeasPilot/blob/ad9cf7f41805f35b29eddcd3ac8f5547d1d235f1/.github/workflows/ci-windows-distribution.yml).
  Applied targeted nullable/XML/async-test guards, explicit lifetime, mechanical/behavioral separation,
  portable core/platform boundaries, and separate docs/distribution evidence. Fast/full separation is
  a future option for measured cost, not infrastructure imported into Seqvium. No SCPI/hardware rules,
  named-route tooling, private implementation, or language/localization policy imported.

These are conceptual references, not dependencies or external sources of Seqvium truth. Branch links
may change; no continued access to MeasPilot is required to work on Seqvium. No code or documentation
text was imported from these repositories, apart from the explicit minimal diagnostic configuration
requested for SEQ-KB-R3.
