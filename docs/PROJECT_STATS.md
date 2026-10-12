# ProjectStats repository diagnostics

Role: Canonical contract and guide for implemented structural repository diagnostics.
Read when: Running, reviewing, implementing, or changing ProjectStats tooling.
Authoritative for: Metrics/count semantics, corpus/classification, reports/metadata, privacy, advisory diagnostics, and evolution limits.
Not authoritative for: Musical capability, semantic architecture, build/test acceptance, product version, or roadmap authorization.

## Implemented tool and scope

**ProjectStats is repository diagnostics, not repository authority.**
[Seqvium.Tools.ProjectStats](../tools/Seqvium.Tools.ProjectStats/Seqvium.Tools.ProjectStats.csproj) is an
independent .NET 10 executable in the existing solution. It uses only the BCL, with no package or
project references, Roslyn, Avalonia, Core, Desktop, audio adapter, or physical-device access.
Tool identity/version come from assembly metadata, without independent version constants.
[Development](DEVELOPMENT.md) owns SDK/setup; [versioning](VERSIONING.md) owns product identity.

The compact pipeline is options/application, path policy/file scanner, Git reader, models/collector,
and renderer. Internals are visible to the existing Tests project for synthetic repositories and
injected I/O failures. No semantic-analysis, smoke-coverage, indexing or historical database is added.
Scanning is read-only; only explicit `--output` writes a file, without creating directories.
This is not an MSBuild evaluator, compiler, test runner, coverage system, benchmark, quality gate or
automatic refactoring tool. This developer checkpoint starts no R5 and accepts no R4 visuals/audio.

## CLI and outputs

From the repository root:

```text
dotnet run --project tools/Seqvium.Tools.ProjectStats -- .
dotnet run --project tools/Seqvium.Tools.ProjectStats -- . --top 25
dotnet run --project tools/Seqvium.Tools.ProjectStats -- . --markdown
dotnet run --project tools/Seqvium.Tools.ProjectStats -- . --json
dotnet run --project tools/Seqvium.Tools.ProjectStats -- . --markdown --output PROJECT_STATS.md
dotnet run --project tools/Seqvium.Tools.ProjectStats -- . --json --output PROJECT_STATS.json
```

For existing Release binaries, add `-c Release --no-build --no-restore` before `--`, or invoke
`bin/Release/net10.0/Seqvium.Tools.ProjectStats.dll` with `dotnet`. SDK build/restore operations are
separate from the scanner's read-only execution guarantee.

Root defaults to the current directory. `--top` defaults to 10 and accepts integers in **1..100**.
Markdown/JSON are mutually exclusive; console is the default. Unknown/duplicate options, missing
values, extra roots and unavailable/reparse roots return 2. `--help`/`-h` alone prints usage.
Scan/output errors return 1, without printing exception content or input/output filenames.
Advisories, individual unreadable files/directories, and Git unavailability do not fail a report.

Output resolves relative to the scanned root; explicit external output is allowed. Parents must
already exist and have no reparse ancestors. Existing source files and `docs/PROJECT_STATS.md` are
protected. New custom targets are allowed; overwrite is limited to conventional generated names:
`PROJECT_STATS.md`, `PROJECT_STATS.json`, `project-stats.*`. Prefer the two root outputs, which are
ignored by [.gitignore](../.gitignore) and excluded on every scan. An explicit custom target is
excluded for that invocation; use a conventional name or excluded tree for stable later scans
without `--output`. Generated reports are local and must not be committed. This permanent owner is
a different file. No live metric snapshots belong in PROJECT_STATE or ordinary history.

## Public corpus, classification, and safety

The corpus includes `src/`, `tests/`, `tools/`, `eng/`, `experiments/`, `docs/`, root solutions, and
these root files: `AGENTS.md`, `README.md`, `LICENSE`, `NOTICE`, `global.json`, `NuGet.config`,
`Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `.gitignore`,
`.gitattributes`, `.editorconfig`. Other root files/trees are excluded. Git tracking/ignore rules do
not define the corpus; new public source is counted before staging. New topology needs an explicit
path-policy update and tests.

Ownership uses the first segment: `src` production; `tests` tests; `tools`/`eng` tooling; `experiments`
experiments; `docs` documentation; root files repository. Nested `tests` folders do not reclassify
tooling/experiments. Authored `src/<project>/Audio` and `tests/<project>/Audio` are included.

Before reading content or descending into directories, exclude:

- `.git`, `.idea`, `.vs`, `.vscode`, hidden/cache trees, `bin`, `obj`, `packages`, `publish`,
  `artifacts`, `.artifacts`, `.scratch`, `TestResults`, `build`, `out`, `dist`, `cmake-build-*`,
  `node_modules`, temporary/scratch/local/report directories.
- Private/secret/credentials/user/settings/personal/music/project trees, local sample/audio corpora,
  recordings, sensitive filename tokens, environment/appsettings files, user/IDE settings,
  keys/certificates, logs/backups, temporary/generated files, compiled binaries and audio/MIDI or
  personal musical project formats. Exact lists live in [PathPolicy.cs](../tools/Seqvium.Tools.ProjectStats/PathPolicy.cs).
- Generated report names, generated image captures below `docs/experiments`, and cold `docs/archive/`
  history. Canonical `docs/PROJECT_STATS.md` is the sole generated-name exception.
- Unsafe/non-ASCII/control/markup characters in filenames, segments over 160 characters,
  UUID-like identifiers and segments matching the current username.
- Symlink/junction/reparse files and trees, including root ancestors. Ancestors are checked again
  before enumeration/text opening; reparse targets are not intentionally followed.

Only admitted relative `/` paths are reported. Skips are aggregate reason counts, without excluded
paths. Unreadable files/directories are separate aggregate counts without exception text. A skipped
directory counts as one entry; descendants are not traversed to calculate hidden file totals.
Unavailable entry attributes count as skipped entries, because their file/directory type is unknown.

Admitted source/documentation namespaces must contain public authored material. V1 cannot infer
arbitrary confidential content/names in public paths, evaluate all Git ignore rules, or provide an
atomic boundary against hostile concurrent filesystem replacement. Private/local data belongs in
excluded trees. No source text, device IDs, environment variables, usernames, absolute root/output
paths or Git status filenames enter reports.

## Metrics and count semantics

- Total included/text/unreadable files and ordinal extension distribution. Unknown binary/resource
  extensions count as files without opening contents.
- C# files/lines/characters, isolated production/tests/tooling/experiments, plus documentation/root
  ownership. Separate AXAML, XAML, Markdown, build/config, C, C++ and headers.
- Build/config includes .NET manifests/solutions, props/targets, JSON/XML/config/manifest, YAML,
  PowerShell/shell, CMake/TOML and editor/Git config formats.
- .NET project/solution inventory without MSBuild evaluation. The closest containing project folder
  owns each file; ordinal project-path order breaks same-folder ties. Empty projects remain visible;
  files outside project folders are `[unassigned]`. Linked/conditional/generated Compile inputs are
  not inferred.
- All directories: direct/subtree files and subtree C# lines. Top-N density uses direct-file count
  with ordinal path ties; subtree counts intentionally overlap.
- Top-N largest readable text files for every file kind and C# ownership bucket, ordered by descending
  lines, characters, then ordinal path. Other structural lists use ordinal key/path order.
- Approximate Fact/Theory occurrences only in test-owned C#. Whitespace, `Xunit.`/`global::Xunit.` and
  `Attribute` suffixes are recognized. This is a **lexical estimate**, including matches in comments
  or strings; it does not resolve aliases, combined attribute lists, preprocessor conditions,
  custom/inherited attributes or expanded theory rows. It is never the number of executed tests.

Text defaults to strict UTF-8 with BOM detection. BOM is excluded; characters mean UTF-16 code units,
including original line terminators (supplementary Unicode characters use two units). Empty text
has zero lines; CRLF, CR and LF each terminate one line, with no extra empty line for a final
terminator. Unreadable/invalid-encoding files count as included files without text metrics. Text over
16 MiB is excluded before opening, with a separate reason/advisory bounding per-file allocation.

## Evidence metadata

All formats carry UTC timestamp, assembly-derived tool name/version, root `.`, Git availability,
branch/HEAD/nullable dirty state, scan and report aggregation durations, format, Top-N, output-to-file
flag, normalized parameters, and skipped/unreadable counts. Aggregation duration includes Git lookup,
excluding rendering/writing. Output paths become `[explicit output]` in parameters. Structural lists
are deterministic; time, durations, Git state and local skip counts may change.

Git queries are `rev-parse --verify HEAD`, `branch --show-current`, and
`status --porcelain=v1 --untracked-files=normal`. Optional locks are disabled through
`--no-optional-locks` and `GIT_OPTIONAL_LOCKS=0`; each command has a five-second timeout, no shell,
and concurrent stdout/stderr draining. Index/history are unchanged. Missing Git/non-repository/
invalid HEAD yields unavailable metadata and null branch/HEAD/dirty. Detached HEAD is explicit.
Common branches and bounded SEQ stage IDs are admitted; arbitrary branches become `[redacted]`.
Only validated hexadecimal HEAD and dirty boolean are reported, never raw status paths/stderr.

## Stable advisory diagnostics

Diagnostics have `Code`, `Severity`, `Message`, and nullable safe relative `Subject`. Summary counts
are grouped by code/severity. These signals never fail build/CI or require splitting/refactoring.

| Code | Severity | Signal |
| --- | --- | --- |
| PST001 | Warning | Production C# file has at least 1000 lines. |
| PST002 | Warning | Test C# file has at least 2000 lines. |
| PST003 | Info | Test file has at least 50 lexical Fact/Theory attributes. |
| PST004 | Warning | Unreadable files/directories leave metrics incomplete; aggregate subject. |
| PST005 | Info | Text over the 16 MiB limit was excluded; aggregate subject. |

Initial thresholds identify exceptional size/concentration, verified against the real corpus and
synthetic boundaries. No advisories is a valid result. No quality score, mass-refactoring advice or
static dependency analysis exists. Preserve code meanings when evolving thresholds, or add new codes.

## Test strategy and placement

Tests live in [Seqvium.Tests/ProjectStats](../tests/Seqvium.Tests/ProjectStats/) in the main project.
Synthetic repositories cover classification/projects/density, lexical counts, text/UTF-8/BOM/line
endings, deterministic ordering, privacy/exclusions, generated-output self-exclusion, read/write
failures, size bounds, real junctions/Unix symlinks, formats, invalid CLI, Git fallback, branch
sanitization, source protection and assembly isolation.
[TEST_EXECUTION](TEST_EXECUTION.md#projectstats-verification) owns commands/results/platform limits.

## Evolution boundary

Semantic analysis and compiler/ABI/plugin verification stay outside V1. A separately justified
`Seqvium.Tools.RepositoryAnalysis` may own semantic analysis
([I-003](IDEAS.md#i-003--separate-semantic-repository-analysis)); optional trend comparison remains
[I-004](IDEAS.md#i-004--optional-projectstats-trend-comparison). No such subsystem is selected or required.
