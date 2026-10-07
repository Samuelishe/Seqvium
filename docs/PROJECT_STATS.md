# Future ProjectStats contract

Role: Contract for future structural repository diagnostics; no executable tool exists.
Read when: Authorizing, designing, implementing, or changing ProjectStats repository tooling.
Authoritative for: Purpose/scope, future metrics, report/output policy, privacy/exclusions, advisory diagnostics, evolution boundary.
Not authoritative for: Current executable capability, source topology, semantic architecture, build/test acceptance, or implementation authorization.

## Accepted direction and introduction boundary

Seqvium should introduce a repository diagnostics CLI, conceptually `Seqvium.Tools.ProjectStats`,
when executable repository tooling is explicitly authorized. This is accepted future direction,
not an existing project or permission to create one during a documentation stage.

It is an early repository-tooling foundation, separate from SEQ-R0's audio probe. It may precede the
probe after code authorization. Whether a dedicated pre-R0 tooling stage is useful remains Q-046 in
[KNOWN_PROBLEMS](KNOWN_PROBLEMS.md); no code stage is silently added to [ROADMAP](ROADMAP.md).
[DEVELOPMENT](DEVELOPMENT.md) owns tool introduction/setup and version authority.

## Purpose and scope boundary

**ProjectStats is repository diagnostics, not repository authority.** It should be cross-platform,
BCL-only initially, directly testable, and independent from production Seqvium assemblies, Avalonia,
audio, and the native engine. Structural data should be deterministic where possible.

Scanning is read-only and side-effect-free except an explicitly requested output file. It must be safe
around generated/private repository-local trees. It is not a semantic compiler, Roslyn architecture
analyzer, MSBuild evaluator, test runner, coverage system, benchmark, historical database, or quality gate.
It must not acquire authority to refactor code automatically.

## Bounded V1 structural metrics

Possible initial metrics; exact path rules wait for actual source topology (Q-044):

- Scanned/text file counts and files by extension; solution/project inventory without MSBuild evaluation.
- C# files/lines/characters; native C/C++/header equivalents only if native source exists.
- AXAML/XAML, Markdown, and build/config files.
- Production/test/tooling/experiment ownership categories.
- Approximate test attributes where meaningful; label textual approximations rather than claim test discovery.
- Largest files by category, folder structural density, and bounded skipped/unreadable paths.

Metrics are descriptive. A large file or concentration is a review signal, not proof of bad design.
V1 does not require semantic dependency or ABI inference. Define count semantics and classification
before relying on output; diagnostics/thresholds await real sizes and usefulness (Q-045).

## Reports, output, and evidence metadata

Plan human console, Markdown, and JSON reports. Generated reports are local/ignored diagnostics;
establish an ignored output location when the tool is introduced, and exclude it from scans.
Do not commit changing statistics snapshots into PROJECT_STATE or ordinary history.

Conceptual future invocations only; these commands are **not currently runnable**:

```text
dotnet run --project Seqvium.Tools.ProjectStats -- .
dotnet run --project Seqvium.Tools.ProjectStats -- . --top 25 --markdown
dotnet run --project Seqvium.Tools.ProjectStats -- . --json
```

The first useful implementation should include generated UTC timestamp, tool version, Git availability,
branch, HEAD SHA, dirty state, sanitized repository root (`.`), scan duration, and normalized
invocation/options. Git unavailability must yield an explicit fallback, not prevent structural reporting.
Sanitize options/metadata as well as subjects: no absolute user paths, usernames, private sample paths,
or device identities. Structural ordering is deterministic; timestamp/duration/Git dirty state are
intentionally live runtime metadata, not structural instability.

## Stable advisory diagnostics

Use structured diagnostics with `Code`, `Severity`, `Message`, and optional repository-relative `Subject`.
Codes should remain stable once introduced. Diagnostics are **advisory**: their count/severity must not
automatically fail build/CI. Possible signals include unusually large production/test files, unreadable
files, unexpected repository locations, and suspicious test concentration. Do not freeze thresholds now
or turn size advice into mandatory splits/refactoring.

## Privacy, exclusions, and traversal safety

The future scanner must avoid `.git`, `.idea`, `.vs`, `.vscode`, `bin`, `obj`, `packages`, `artifacts`,
`publish`, `TestResults`, generated report output, native build trees, local sample/audio scratch
corpora, and private/sensitive repository-local areas if introduced. Final corpus/path rules are explicit
and testable; source classification does not grant access to excluded material.

Do not follow reparse/symlink directory escapes outside the intended corpus. Report skipped/unreadable
paths in bounded sanitized form; private subjects must not reveal sensitive path names. Output targets
are self-excluded so reports do not inflate later scans. Preserve unreadable-file diagnostics without
making arbitrary machine access or permission changes.

## Test strategy and placement

When implemented, test the tool in the normal main `Seqvium.Tests/ProjectStats/` area, following
[TEST_EXECUTION](TEST_EXECUTION.md#intended-test-topology). Use synthetic temporary repositories;
running only against the live Seqvium repository is insufficient.

Cover exclusions, deterministic ordering, ownership classification, line/character counts, test-attribute
approximation, console/Markdown/JSON shape, output self-exclusion, locked/unreadable files, symlink/reparse
safety, sanitized paths, Git metadata fallback, and native/managed classification once relevant.
`InternalsVisibleTo` is allowed if justified. Do not create `Seqvium.Tools.ProjectStats.Tests` merely
because the CLI has its own project; split only for real execution/dependency constraints.

## Evolution boundary

If semantic analysis becomes useful, justify a separate concern such as
`Seqvium.Tools.RepositoryAnalysis` ([I-003](IDEAS.md#i-003--separate-semantic-repository-analysis)).
ProjectStats may later read its result if justified, but must not silently become a Roslyn architecture
oracle, dependency-policy engine, native ABI analyzer, or plugin verifier.
Optional historical trend comparison remains speculative [I-004](IDEAS.md#i-004--optional-projectstats-trend-comparison);
no historical metrics database or snapshot infrastructure is planned as V1 acceptance.
