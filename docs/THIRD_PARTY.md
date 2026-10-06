# Third-party provenance and evaluation

Role: Provenance ledger and evaluation boundary for external components and material.
Read when: Evaluating, introducing, upgrading, replacing, or removing a dependency, service, action, or asset.
Authoritative for: Introduced material, candidate status, provenance, obligations, and future entry fields.
Not authoritative for: Final technical choices, subsystem design, installed-version locks, or project licensing selection.

Seqvium's project license has not been selected yet. Third-party components retain their own licenses
and terms. Do not create a final LICENSE or infer license compatibility from FOSS intent.

## Introduced

No executable third-party components, NuGet packages, native libraries, or vendored code have been
introduced. The pre-existing empty `Seqvium.sln` is repository scaffolding, not evidence of an installed
.NET/Avalonia dependency graph. Local Rider metadata is not shipped product material.

When actual components are introduced, record them here using the fields below. Exact installed
versions belong to build/package/native manifests once those exist; this ledger links that authority
and must not become a second package lock.

## Planned / under evaluation

These are candidates or product directions, not dependencies, adoption decisions, or promises.
No official version/license evaluation has been completed for them in SEQ-KB-R0.

| Candidate / direction | Intended evaluation boundary |
| --- | --- |
| C# / .NET 10 | User's development direction and proposed application platform; evaluate concrete SDK/runtime and distribution obligations when introduced |
| Avalonia | Proposed UI framework; evaluate official package/API/platform/license evidence before adoption |
| miniaudio or comparable backend | SEQ-R0 backend candidate; evaluate realtime/device behavior, official source/license, native binaries and packaging |
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
- License/terms, notices and redistribution/source obligations; compatibility with Seqvium's selected
  license when known. Until selection, record that compatibility remains unresolved.
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
  provenance/evaluation ledger with manifest-owned versions and unselected project licensing. Did
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

These are conceptual references, not dependencies or external sources of Seqvium truth. Branch links
may change; no continued access to MeasPilot is required to work on Seqvium. No code or documentation
text was imported from these repositories.
